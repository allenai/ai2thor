# Copyright Allen Institute for Artificial Intelligence 2017
"""
ai2thor.server

Handles all communication with Unity through a Flask service.  Messages
are sent to the controller using a pair of request/response queues.
"""


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
import warnings

from flask import Flask, request, make_response, abort
import werkzeug
import werkzeug.serving
import werkzeug.http
import numpy as np
from enum import Enum

from ai2thor.util.depth import apply_real_noise, generate_noise_indices

logging.getLogger('werkzeug').setLevel(logging.ERROR)

werkzeug.serving.WSGIRequestHandler.protocol_version = 'HTTP/1.1'

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

class NumpyAwareEncoder(json.JSONEncoder):

    def default(self, obj):
        if isinstance(obj, np.generic):
            return np.asscalar(obj)
        return super(NumpyAwareEncoder, self).default(obj)

class BufferedIO(object):
    def __init__(self, wfile):
        self.wfile = wfile
        self.data = []

    def write(self, output):
        self.data.append(output)

    def flush(self):
        self.wfile.write(b"".join(self.data))
        self.wfile.flush()

    def close(self):
        return self.wfile.close()

    @property
    def closed(self):
        return self.wfile.closed

class ThorRequestHandler(werkzeug.serving.WSGIRequestHandler):
    def run_wsgi(self):
        old_wfile = self.wfile
        self.wfile = BufferedIO(self.wfile)
        result = super(ThorRequestHandler, self).run_wsgi()
        self.wfile = old_wfile
        return result

class MultiAgentEvent(object):

    def __init__(self, active_agent_id, events):
        self._active_event = events[active_agent_id]
        self.metadata = self._active_event.metadata
        self.screen_width = self._active_event.screen_width
        self.screen_height = self._active_event.screen_height
        self.events = events
        self.third_party_camera_frames = []
        # XXX add methods for depth,sem_seg

    @property
    def cv2img(self):
        return self._active_event.cv2img

    def add_third_party_camera_image(self, third_party_image_data):
        self.third_party_camera_frames.append(read_buffer_image(third_party_image_data, self.screen_width, self.screen_height))


def read_buffer_image(buf, width, height, flip_y=True, flip_x=False, dtype=np.uint8,
                      flip_rb_colors=False):
    im_bytes = np.frombuffer(buf.tobytes(), dtype=dtype) if sys.version_info.major < 3 \
        else np.frombuffer(buf, dtype=dtype)
    im = im_bytes.reshape(height, width, -1)
    if flip_y:
        im = np.flip(im, axis=0)
    if flip_x:
        im = np.flip(im, axis=1)
    if flip_rb_colors:
        im = im[..., ::-1]

    return im


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
        self.depth_frame = None
        self.normals_frame = None
        self.flow_frame = None

        self.color_to_object_id = {}
        self.object_id_to_color = {}

        self.instance_detections2D = None
        self.instance_masks = {}
        self.class_masks = {}

        self.instance_segmentation_frame = None
        self.class_segmentation_frame = None

        self.class_detections2D = {}

        self.process_colors()
        self.process_visible_bounds2D()
        self.third_party_camera_frames = []
        self.third_party_class_segmentation_frames = []
        self.third_party_instance_segmentation_frames = []
        self.third_party_depth_frames = []
        self.third_party_normals_frames = []
        self.third_party_flows_frames = []

        self.events = [self] # Ensure we have a similar API to MultiAgentEvent

    @property
    def image_data(self):
        warnings.warn("Event.image_data has been removed - RGB data can be retrieved from event.frame and encoded to an image format")
        return None

    def process_visible_bounds2D(self):
        if self.instance_detections2D and len(self.instance_detections2D) > 0:
            for obj in self.metadata['objects']:
                obj['visibleBounds2D'] = (obj['visible'] and obj['objectId'] in self.instance_detections2D)

    def process_colors(self):
        if 'colors' in self.metadata and self.metadata['colors']:
            for color_data in self.metadata['colors']:
                name = color_data['name']
                c_key = tuple(color_data['color'])
                self.color_to_object_id[c_key] = name
                self.object_id_to_color[name] = c_key

    def objects_by_type(self, object_type):
        return [obj for obj in self.metadata['objects'] if obj['objectType'] == object_type]

    def process_colors_ids(self):
        if self.instance_segmentation_frame is None:
            return

        MIN_DETECTION_LEN = 0

        self.instance_detections2D = {}
        unique_ids, unique_inverse = unique_rows(self.instance_segmentation_frame.reshape(-1, 3), return_inverse=True)
        unique_inverse = unique_inverse.reshape(self.instance_segmentation_frame.shape[:2])
        unique_masks = (np.tile(unique_inverse[np.newaxis, :, :], (len(unique_ids), 1, 1)) == np.arange(len(unique_ids))[:, np.newaxis, np.newaxis])
        #for unique_color_ind, unique_color in enumerate(unique_ids):
        for color_bounds in self.metadata['colorBounds']:
            color = np.array(color_bounds['color'])
            color_name = self.color_to_object_id.get(tuple(int(cc) for cc in color), 'background')
            cls = color_name
            simObj = False
            if '|' in cls:
                cls = cls.split('|')[0]
                simObj = True

            bb = np.array(color_bounds['bounds'])
            bb[[1,3]] = self.metadata['screenHeight'] - bb[[3,1]]
            if not((bb[2] - bb[0]) < MIN_DETECTION_LEN or (bb[3] - bb[1]) < MIN_DETECTION_LEN):
                if cls not in self.class_detections2D:
                    self.class_detections2D[cls] = []

                self.class_detections2D[cls].append(bb)

                color_ind = np.argmin(np.sum(np.abs(unique_ids - color), axis=1))

                if simObj:
                    self.instance_detections2D[color_name] = bb
                    self.instance_masks[color_name] = unique_masks[color_ind, ...]

                if cls not in self.class_masks:
                    self.class_masks[cls] = unique_masks[color_ind, ...]
                else:
                    self.class_masks[cls] = np.logical_or(self.class_masks[cls], unique_masks[color_ind, ...])

    def _image_depth(self, image_depth_data, **kwargs):
        image_depth = read_buffer_image(image_depth_data, self.screen_width, self.screen_height)
        depth_format = kwargs['depth_format']
        image_depth_out = image_depth[:,:,0] + image_depth[:,:,1] / np.float32(256) + image_depth[:,:,2] / np.float32(256 ** 2)
        multiplier = 1.0
        if depth_format != DepthFormat.Normalized:
            multiplier = kwargs['camera_far_plane'] - kwargs['camera_near_plane']
        elif depth_format == DepthFormat.Millimeters:
            multiplier *= 1000
        image_depth_out *= multiplier / 256.0

        depth_image_float = image_depth_out.astype(np.float32)

        if 'add_noise' in kwargs and kwargs['add_noise']:
            depth_image_float = apply_real_noise(
                depth_image_float,
                self.screen_width,
                indices=kwargs['noise_indices']
            )

        return depth_image_float

    def add_image_depth_robot(self, image_depth_data, depth_format, **kwargs):
        multiplier = 1.0
        camera_far_plane = kwargs.pop('camera_far_plane', 1)
        camera_near_plane = kwargs.pop('camera_near_plane', 0)
        if depth_format == DepthFormat.Normalized:
            multiplier = 1.0 / (camera_far_plane - camera_near_plane)
        elif depth_format == DepthFormat.Millimeters:
            multiplier = 1000.0

        image_depth = read_buffer_image(
            image_depth_data, self.screen_width, self.screen_height, **kwargs
        ).reshape(self.screen_height, self.screen_width) * multiplier
        self.depth_frame = image_depth.astype(np.float32)

    def add_image_depth(self, image_depth_data, **kwargs):
        self.depth_frame = self._image_depth(image_depth_data, **kwargs)

    def add_third_party_image_depth(self, image_depth_data, **kwargs):
        self.third_party_depth_frames.append(self._image_depth(image_depth_data, **kwargs))

    def add_third_party_image_normals(self, normals_data):
        self.third_party_normals_frames.append(read_buffer_image(normals_data, self.screen_width, self.screen_height))

    def add_image_normals(self, image_normals_data):
        self.normals_frame = read_buffer_image(image_normals_data, self.screen_width, self.screen_height)

    def add_third_party_image_flows(self, flows_data):
        self.third_party_flows_frames.append(read_buffer_image(flows_data, self.screen_width, self.screen_height))

    def add_image_flows(self, image_flows_data):
        self.flows_frame = read_buffer_image(image_flows_data, self.screen_width, self.screen_height)

    def add_third_party_camera_image(self, third_party_image_data):
        self.third_party_camera_frames.append(read_buffer_image(third_party_image_data, self.screen_width, self.screen_height))

    def add_image(self, image_data, **kwargs):
        self.frame = read_buffer_image(image_data, self.screen_width, self.screen_height, **kwargs)

    def add_image_ids(self, image_ids_data):
        self.instance_segmentation_frame = read_buffer_image(image_ids_data, self.screen_width, self.screen_height)
        self.process_colors_ids()

    def add_third_party_image_ids(self, image_ids_data):
        self.third_party_instance_segmentation_frames.append(read_buffer_image(image_ids_data, self.screen_width, self.screen_height))

    def add_image_classes(self, image_classes_data):
        self.class_segmentation_frame = read_buffer_image(image_classes_data, self.screen_width, self.screen_height)

    def add_third_party_image_classes(self, image_classes_data):
        self.third_party_class_segmentation_frames.append(read_buffer_image(image_classes_data, self.screen_width, self.screen_height))

    def cv2image(self):
        warnings.warn("Deprecated - please use event.cv2img")
        return self.cv2img

    @property
    def cv2img(self):
        return self.frame[...,::-1]

    @property
    def pose(self):
        agent_meta = self.metadata['agent']
        loc = agent_meta['position']
        rotation = round(agent_meta['rotation']['y'] * 1000)
        horizon = round(agent_meta['cameraHorizon'] * 1000)
        return (round(loc['x'] * 1000), round(loc['z'] * 1000), rotation, horizon)

    @property
    def pose_discrete(self):
        # XXX should have this as a parameter
        step_size = 0.25
        agent_meta = self.metadata['agent']
        loc = agent_meta['position']
        rotation = int(agent_meta['rotation']['y'] / 90.0)
        horizon = int(round(agent_meta['cameraHorizon']))
        return (int(loc['x'] / step_size), int(loc['z'] / step_size), rotation, horizon)

    def get_object(self, object_id):
        for obj in self.metadata['objects']:
            if obj['objectId'] == object_id:
                return obj
        return None


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

        full_boundary = b'--' + boundary 
        mid_boundary = b'\r\n' + full_boundary
        view = memoryview(data)
        i = data.find(full_boundary) + len(full_boundary)
        while i >= 0:
            next_offset = data.find(mid_boundary, i)
            if next_offset < 0:
                break
            headers_offset = i + 2 # add 2 for CRLF
            body_offset = data.find(b'\r\n\r\n', headers_offset)
            raw_headers = view[headers_offset: body_offset]
            body = view[body_offset + 4: next_offset]
            i = next_offset + len(mid_boundary)

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


class DepthFormat(Enum):
    Meters = 0,
    Normalized = 1,
    Millimeters = 2

class Server(object):

    def __init__(
            self,
            request_queue,
            response_queue,
            host,
            port=0,
            threaded=False,
            depth_format=DepthFormat.Meters,
            add_depth_noise=False,
            width=300,
            height=300
    ):

        app = Flask(__name__,
                    template_folder=os.path.realpath(
                        os.path.join(
                            os.path.dirname(os.path.abspath(__file__)), '..', 'templates')))

        self.image_buffer = None

        self.app = app
        self.client_token = None
        self.subscriptions = []
        self.app.config.update(PROPAGATE_EXCEPTIONS=False, JSONIFY_PRETTYPRINT_REGULAR=False)
        self.port = port
        self.last_rate_timestamp = time.time()
        self.frame_counter = 0
        self.debug_frames_per_interval = 50
        self.xwindow_id = None
        self.wsgi_server = werkzeug.serving.make_server(host, self.port, self.app, threaded=threaded, request_handler=ThorRequestHandler)
        # used to ensure that we are receiving frames for the action we sent
        self.sequence_id = 0
        self.last_event = None
        self.camera_near_plane = 0.1
        self.camera_far_plane = 20.0
        self.depth_format = depth_format
        self.add_depth_noise = add_depth_noise
        self.noise_indices = None

        if add_depth_noise:
            assert width == height,\
                "Noise supported with square dimension images only."
            self.noise_indices = generate_noise_indices(width)

        @app.route('/ping', methods=['get'])
        def ping():
            return 'pong'

        @app.route('/train', methods=['post'])
        def train():

            if request.headers['Content-Type'].split(';')[0] == 'multipart/form-data':
                form = MultipartFormParser(request.get_data(), MultipartFormParser.get_boundary(request.headers))
                metadata = json.loads(form.form['metadata'][0])
                token = form.form['token'][0]
            else:
                form = request
                metadata = json.loads(form.form['metadata'])
                token = form.form['token']

            if self.client_token and token != self.client_token:
                abort(403)

            if self.frame_counter % self.debug_frames_per_interval == 0:
                now = time.time()
                # rate = self.debug_frames_per_interval / float(now - self.last_rate_timestamp)
                self.last_rate_timestamp = now
                # import datetime
                # print("%s %s/s" % (datetime.datetime.now().isoformat(), rate))

            if metadata['sequenceId'] != self.sequence_id:
                raise ValueError("Sequence id mismatch: %s vs %s" % (
                    metadata['sequenceId'], self.sequence_id))

            events = []
            for i, a in enumerate(metadata['agents']):
                e = Event(a)
                image_mapping = dict(
                    image=e.add_image,
                    image_depth=lambda x: e.add_image_depth(
                        x,
                        depth_format=self.depth_format,
                        camera_near_plane=self.camera_near_plane,
                        camera_far_plane=self.camera_far_plane,
                        add_noise=self.add_depth_noise,
                        noise_indices=self.noise_indices
                    ),
                    image_ids=e.add_image_ids,
                    image_classes=e.add_image_classes,
                    image_normals=e.add_image_normals,
                    image_flows=e.add_image_flows
                )

                for key in image_mapping.keys():
                    if key in form.files:
                        image_mapping[key](form.files[key][i])

                third_party_image_mapping = dict(
                    image=e.add_image,
                    image_thirdParty_depth=lambda x: e.add_third_party_image_depth(
                        x,
                        depth_format=self.depth_format,
                        camera_near_plane=self.camera_near_plane,
                        camera_far_plane=self.camera_far_plane
                    ),
                    image_thirdParty_image_ids=e.add_third_party_image_ids,
                    image_thirdParty_classes=e.add_third_party_image_classes,
                    image_thirdParty_normals=e.add_third_party_image_normals,
                    image_thirdParty_flows=e.add_third_party_image_flows
                )

                if a['thirdPartyCameras'] is not None:
                    for ti, t in enumerate(a['thirdPartyCameras']):
                        for key in third_party_image_mapping.keys():
                            if key in form.files:
                                third_party_image_mapping[key](form.files[key][ti])
                events.append(e)

            if len(events) > 1:
                self.last_event = event = MultiAgentEvent(metadata['activeAgentId'], events)
            else:
                self.last_event = event = events[0]

            for img in form.files.get('image-thirdParty-camera', []):
                self.last_event.add_third_party_camera_image(img)

            request_queue.put_nowait(event)

            self.frame_counter += 1

            next_action = queue_get(response_queue)
            if 'sequenceId' not in next_action:
                self.sequence_id += 1
                next_action['sequenceId'] = self.sequence_id
            else:
                self.sequence_id = next_action['sequenceId']

            resp = make_response(json.dumps(next_action, cls=NumpyAwareEncoder))

            return resp

    def start(self):
        self.wsgi_server.serve_forever()

    def set_init_params(self, init_params):
        self.camera_near_plane = init_params['cameraNearPlane']
        self.camera_far_plane = init_params['cameraFarPlane']
