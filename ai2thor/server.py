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
import sys
import os
import os.path

try:
    from queue import Empty
except ImportError:
    from Queue import Empty

import time

from flask import Flask, request, make_response, abort, Response
import werkzeug
import werkzeug.serving
import werkzeug.http
import numpy as np
from PIL import Image

logging.getLogger('werkzeug').setLevel(logging.ERROR)

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


class MultiAgentEvent(object):

    def __init__(self, active_agent_id, events):
        self._active_event = events[active_agent_id]
        self.cv2image = self._active_event.cv2image
        self.metadata = self._active_event.metadata
        self.events = events

class Event(object):
    """
    Object that is returned from a call to  controller.step().
    This class wraps the screenshot that Unity captures as well
    as the metadata sent about each object
    """

    def __init__(self, metadata, image_data):
        self.metadata = metadata
        width = metadata['screenWidth']
        height = metadata['screenHeight']
        self.image_data = image_data
        if sys.version_info.major < 3:
            # support for Python 2.7 - can't handle memoryview in Python2.7 and Numpy frombuffer
            self.frame = np.flip(np.frombuffer(image_data.tobytes(), dtype=np.uint8).reshape(width, height, 3), axis=0)
        else:
            self.frame = np.flip(np.frombuffer(image_data, dtype=np.uint8).reshape(width, height, 3), axis=0)

    def cv2image(self):
        return self.frame[...,::-1]


class MultipartFormParser(object):

    @staticmethod
    def get_boundary(request_headers):
        for h, value in request_headers:
            if h == 'Content-Type':
                ctype, ct_opts = werkzeug.http.parse_options_header(value)
                boundary = ct_opts['boundary'].encode('ascii')
                return boundary
        return None

    def __init__(self, data, boundary):

        self.form = {}
        self.files = {}

        full_boundary = b'\r\n--' + boundary
        view = memoryview(data)
        i = data.find(full_boundary)
        while i >= 0:
            next_offset = data.find(full_boundary, i + len(full_boundary))
            if next_offset < 0:
                break
            headers_offset = i + len(full_boundary) + 2
            body_offset = data.find(b'\r\n\r\n', headers_offset)
            raw_headers = view[headers_offset: body_offset]
            body = view[body_offset + 4: next_offset]
            i = next_offset

            headers = {}
            for header in raw_headers.tobytes().decode('ascii').strip().split("\r\n"):

                k,v = header.split(':')
                headers[k.strip()] = v.strip()

            ctype, ct_opts = werkzeug.http.parse_options_header(headers['Content-Type'])
            cdisp, cd_opts = werkzeug.http.parse_options_header(headers['Content-disposition'])
            assert cdisp == 'form-data'

            if 'filename' in cd_opts:
                if cd_opts['name'] not in self.files:
                    self.files[cd_opts['name']] = []

                self.files[cd_opts['name']].append(body)

            else:
                if ctype == 'text/plain' and 'charset' in ct_opts:
                    body = body.tobytes().decode(ct_opts['charset'])
                if cd_opts['name'] not in self.form:
                    self.form[cd_opts['name']] = []

                self.form[cd_opts['name']].append(body)


class Server(object):

    def __init__(self, request_queue, response_queue, host, port=0, threaded=False):

        app = Flask(__name__,
                    template_folder=os.path.realpath(
                        os.path.join(
                            os.path.dirname(os.path.abspath(__file__)), '..', 'templates')))

        self.image_buffer = None

        self.app = app
        self.client_token = None
        self.subscriptions = []
        self.app.config.update(PROPAGATE_EXCEPTIONS=True, JSONIFY_PRETTYPRINT_REGULAR=False)
        self.port = port
        self.last_rate_timestamp = time.time()
        self.frame_counter = 0
        self.debug_frames_per_interval = 50
        self.xwindow_id = None
        self.wsgi_server = werkzeug.serving.make_server(host, self.port, self.app, threaded=threaded)
        # used to ensure that we are receiving frames for the action we sent
        self.sequence_id = 0
        self.last_event = None

        def stream_gen():
            while True:
                event = self.last_event
                if event:
                    b = io.BytesIO()
                    i = Image.fromarray(event.frame)
                    i.save(b, format='PNG')
                    b.seek(0)

                    time.sleep(0.02)
                    yield (b'--frame\r\n' + b'Content-Type: image/png\r\n\r\n' + b.read() + b'\r\n')

        @app.route('/stream', methods=['get'])
        def stream():
            if threaded:
                return Response(stream_gen(),
                    mimetype='multipart/x-mixed-replace; boundary=frame')
            else:
                return abort(404)

        @app.route('/viewer', methods=['get'])
        def viewer():
            html = """
<html>
  <head>
    <title></title>
    <script>
        var image_url = "/stream";
        var image_data = [];
        function checkDiff() {
            var image = document.getElementById('image');
            var canvas = document.getElementById('canvas');
            var count = 0;
            try {
                canvas.width = image.width;
                canvas.height = image.height;
                var context = canvas.getContext('2d');
                context.drawImage(image, 0, 0);
                var current_image_data = context.getImageData(0, 0, image.width, image.height).data;
                for (var i = 0; i < image_data.length; i++) {
                    if (image_data[i] != current_image_data[i]) {
                        count++;
                        break;
                    }
                }
            } catch (err) {
               console.log("caught err: " + err)
            }
            if (count === 0) {
                console.log("trying to reload");
                image.src = image_url + "?" + (new Date().getTime());
            }

            image_data = current_image_data;
            setTimeout(checkDiff, 5000);
        }
        setTimeout(checkDiff, 5000);
    </script
  </head>
  <body>
    <img id="image" src="/stream">
    <canvas id="canvas" style="display: none"></canvas>
  </body>
</html>
<html
"""
            return make_response(html)



        @app.route('/ping', methods=['get'])
        def ping():
            return 'pong'

        @app.route('/train', methods=['post'])
        def train():

            if request.headers['Content-Type'].split(';')[0] == 'multipart/form-data':
                form = MultipartFormParser(request.get_data(), MultipartFormParser.get_boundary(request.headers))
            else:
                form = request

            if self.client_token:
                token = form.form['token'][0]
                if token is None or token != self.client_token:
                    abort(403)

            if self.frame_counter % self.debug_frames_per_interval == 0:
                now = time.time()
                rate = self.debug_frames_per_interval / float(now - self.last_rate_timestamp)
                # print("%s %s/s" % (datetime.datetime.now().isoformat(), rate))
                self.last_rate_timestamp = now

            metadata = json.loads(form.form['metadata'][0])

            if len(metadata['agents']) > 1:
                events = []
                for a in metadata['agents']:
                    events.append(Event(a, form.files['image'][len(events)]))

                self.last_event = event = MultiAgentEvent(metadata['activeAgentId'], events)
            else:
                self.last_event = event = Event(metadata['agents'][0], form.files['image'][0])

            if metadata['sequenceId'] != self.sequence_id:
                raise Exception("Sequence id mismatch: %s vs %s" % (
                    metadata['sequenceId'], self.sequence_id))

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
