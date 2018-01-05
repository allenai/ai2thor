# Copyright Allen Institute for Artificial Intelligence 2017
"""
ai2thor.server

Handles all communication with Unity through a Flask service.  Messages
are sent to the controller using a pair of request/response queues.
"""


import datetime
import io
import json
import logging
import os
import os.path

try:
    from queue import Empty
except ImportError:
    from Queue import Empty

import time
import uuid

from flask import Flask, request, make_response, abort
import werkzeug.serving
from PIL import Image
import numpy as np

LOG = logging.getLogger('werkzeug')
LOG.setLevel(logging.ERROR)


# get with timeout to allow quit
def queue_get(que):
    res = None
    while True:
        try:
            res = que.get(block=True, timeout=0.5)
            break
        except Empty:
            pass
    return res


class Event(object):
    """
    Object that is returned from a call to  controller.step().
    This class wraps the screenshot that Unity captures as well
    as the metadata sent about each object
    """

    def __init__(self, metadata, image_data):
        self.metadata = metadata
        self.image = image_data
        # decode image from string encoding
        self.frame = np.asarray(Image.open(io.BytesIO(image_data)))

class Server(object):

    def __init__(self, request_queue, response_queue, host, port=0):

        app = Flask(__name__,
                    template_folder=os.path.realpath(
                        os.path.join(
                            os.path.dirname(os.path.abspath(__file__)), '..', 'templates')))

        self.image_buffer = None

        self.app = app
        self.client_token = str(uuid.uuid4())
        self.subscriptions = []
        self.app.config.update(PROPAGATE_EXCEPTIONS=True, JSONIFY_PRETTYPRINT_REGULAR=False)
        self.port = port
        self.last_rate_timestamp = time.time()
        self.frame_counter = 0
        self.debug_frames_per_interval = 50
        self.xwindow_id = None
        self.wsgi_server = werkzeug.serving.BaseWSGIServer(host, self.port, self.app)
        # used to ensure that we are receiving frames for the action we sent
        self.sequence_id = 0

        @app.route('/ping', methods=['get'])
        def ping():
            return 'pong'

        @app.route('/train', methods=['post'])
        def train():

            if self.client_token:
                token = request.form['token']
                if token is None or token != self.client_token:
                    abort(403)

            if self.frame_counter % self.debug_frames_per_interval == 0:
                now = time.time()
                rate = self.debug_frames_per_interval / float(now - self.last_rate_timestamp)
                # print("%s %s/s" % (datetime.datetime.now().isoformat(), rate))
                self.last_rate_timestamp = now

            metadata = json.loads(request.form['metadata'])
            if metadata['sequenceId'] != self.sequence_id:
                raise Exception("Sequence id mismatch: %s vs %s" % (
                    metadata['sequenceId'], self.sequence_id))

            image = request.files['image']
            image_data = image.read()

            event = Event(metadata, image_data)
            request_queue.put_nowait(event)
            self.frame_counter += 1

            next_action = queue_get(response_queue)
            if 'sequenceId' not in next_action:
                self.sequence_id += 1
                next_action['sequenceId'] = self.sequence_id
            else:
                self.sequence_id = next_action['sequenceId']

            resp = make_response(json.dumps(next_action))

            return resp

    def start(self):
        self.wsgi_server.serve_forever()
