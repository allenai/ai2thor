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

MAX_DEPTH = 5000

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
        # XXX add methods for depth,sem_seg

def read_buffer_image(buf, width, height):

    if sys.version_info.major < 3:
        # support for Python 2.7 - can't handle memoryview in Python2.7 and Numpy frombuffer
        return np.flip(np.frombuffer(
            buf.tobytes(), dtype=np.uint8).reshape(width, height, -1), axis=0)
    else:
        return np.flip(np.frombuffer(buf, dtype=np.uint8).reshape(width, height, -1), axis=0)

def unique_rows(arr, return_index=False, return_inverse=False):
        arr = np.ascontiguousarray(arr).copy()
        b = arr.view(np.dtype((np.void, arr.dtype.itemsize * arr.shape[1])))
        if return_inverse:
            _, idx, inv = np.unique(b, return_index=True, return_inverse=True)
        else:
            _, idx = np.unique(b, return_index=True)
        unique = arr[idx]
        if return_index and return_inverse:
            return unique, idx, inv
        elif return_index:
            return unique, idx
        elif return_inverse:
            return unique, inv
        else:
            return unique

class Event(object):
    """
    Object that is returned from a call to  controller.step().
    This class wraps the screenshot that Unity captures as well
    as the metadata sent about each object
    """

    def __init__(self, metadata):
        self.metadata = metadata
        self.screen_width = metadata['screenWidth']
        self.screen_height = metadata['screenHeight']

        self.frame = None
        self.frame_depth = None

        self.color_to_object_id = {}
        self.object_id_to_color = {}

        self.bounds2D = {}
        self.instance_masks = {}
        self.class_masks = {}

        self.instance_segmentation_image = None
        self.class_segmentation_image = None

        self.detections = {}

        self.class_detections2D = {}
        self.instance_detections2D = {}

        self.process_colors()
        self.process_visible_bounds2D()

    def process_visible_bounds2D(self):
        if len(self.bounds2D) > 0:
            for obj in self.metadata['objects']:
                obj['visibleBounds2D'] = (obj['visible'] and obj['objectId'] in self.bounds2D)

    def process_colors(self):
        for color_data in self.metadata['colors']:
            name = ''.join([x for x in color_data['name'] if x.isalpha()]).lower()  # Keep only alpha chars
            name = color_data['name']
            c_key = tuple(color_data['color'])
            self.color_to_object_id[c_key] = name
            self.object_id_to_color[name] = c_key

    def objects_by_type(self, object_type):
        return [obj for obj in self.metadata['objects'] if obj['objectType'] == object_type]

    def process_colors_ids(self):
        if self.instance_segmentation_image is None:
            return

        MIN_DETECTION_LEN = 10

        unique_ids, unique_inverse = unique_rows(self.instance_segmentation_image.reshape(-1, 3), return_inverse=True)
        unique_inverse = unique_inverse.reshape(self.instance_segmentation_image.shape[:2])
        unique_masks = (np.tile(unique_inverse[np.newaxis, :, :], (len(unique_ids), 1, 1)) == np.arange(len(unique_ids))[:, np.newaxis, np.newaxis])
        #for unique_color_ind, unique_color in enumerate(unique_ids):
        for color_bounds in self.metadata['colorBounds']:
            color = np.array(color_bounds['color'])
            color_name = self.color_to_object_id.get(tuple(int(cc) for cc in color), 'background')
            cls = color_name.lower()
            simObj = False
            if '|' in cls:
                cls = cls.split('|')[0]
                simObj = True
            elif 'sink' in cls:
                cls = 'sink'
                simObj = True
                color_name = self.objects_by_type('Sink')[0]['objectId']

            bb = np.array(color_bounds['bounds'])
            bb[[1,3]] = self.metadata['screenHeight'] - bb[[3,1]]
            if not((bb[2] - bb[0]) < MIN_DETECTION_LEN or (bb[3] - bb[1]) < MIN_DETECTION_LEN):
                if cls not in self.detections:
                    self.detections[cls] = []
                self.detections[cls].append(bb)
                if simObj:
                    self.bounds2D[color_name] = bb
                    color_ind = np.argmin(np.sum(np.abs(unique_ids - color), axis=1))
                    self.instance_masks[color_name] = unique_masks[color_ind, ...]

                    if cls not in self.class_masks:
                        self.class_masks[cls] = unique_masks[color_ind, ...]
                    else:
                        self.class_masks[cls] = np.logical_or(self.class_masks[cls], unique_masks[color_ind, ...])

            self.instance_detections2D = self.bounds2D
            self.class_detections2D = self.detections

    def add_image_depth(self, image_depth_data):

        image_depth = read_buffer_image(image_depth_data, self.screen_width, self.screen_height).astype(np.float32)
        max_spots = image_depth[:,:,0] == 255
        image_depth_out = image_depth[:,:,0] + image_depth[:,:,1] / 256 + image_depth[:,:,2] / 256 ** 2
        image_depth_out[max_spots] = 256
        image_depth_out *= 10.0 / 256.0 * 1000  # converts to meters then to mm
        image_depth_out[image_depth_out > MAX_DEPTH] = MAX_DEPTH
        self.frame_depth = image_depth_out.astype(np.float32)

    def add_image(self, image_data):
        self.image_data = image_data
        self.frame = read_buffer_image(image_data, self.screen_width, self.screen_height)

    def add_image_ids(self, image_ids_data):
        self.instance_segmentation_image = read_buffer_image(image_ids_data, self.screen_width, self.screen_height)

    def add_image_classes(self, image_classes_data):
        self.class_segmentation_image = read_buffer_image(image_classes_data, self.screen_width, self.screen_height)

    def cv2image(self):
        return self.frame[...,::-1]

    @property
    def pose_continuous(self):
        agent_meta = self.metadata['agent']
        loc = agent_meta['position']
        rotation = round(agent_meta['rotation']['y'] * 1000)
        horizon = round(agent_meta['cameraHorizon'] * 1000)
        return (round(loc['x'] * 1000), round(loc['z'] * 1000), rotation, horizon)

    @property
    def pose(self):
        step_size = 0.25
        agent_meta = self.metadata['agent']
        loc = agent_meta['position']
        rotation = int(agent_meta['rotation']['y'] / 90.0)
        horizon = int(round(agent_meta['cameraHorizon']))
        return (int(loc['x'] / step_size), int(loc['z'] / step_size, rotation, horizon))


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
                self.last_event = event = Event(metadata['agents'][0])

            if metadata['sequenceId'] != self.sequence_id:
                raise Exception("Sequence id mismatch: %s vs %s" % (
                    metadata['sequenceId'], self.sequence_id))

            #print(list(form.files.keys()))

            image_mapping = dict(
                image=event.add_image,
                image_depth=event.add_image_depth,
                image_ids=event.add_image_ids,
                image_classes=event.add_image_classes
            )

            for key in image_mapping.keys():
                if key in form.files:
                    image_mapping[key](form.files[key][0])

            # XXX restore
            event.process_colors_ids()

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
