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
#from collections import defaultdict

try:
    from queue import Empty
except ImportError:
    from Queue import Empty

import time
import uuid

from flask import Flask, request, make_response, abort
import werkzeug.serving
import numpy as np
#import scipy.misc

from utils import game_util

import constants

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


def read_image(buf, metadata):
    # Copy makes sure array is in readable order
    return np.flipud(np.frombuffer(buf, dtype=np.uint8).reshape(
            (metadata['screenHeight'], metadata['screenWidth'], -1))).copy()


class Event(object):
    """
    Object that is returned from a call to  controller.step().
    This class wraps the screenshot that Unity captures as well
    as the metadata sent about each object
    """

    def __init__(self, pose, metadata, image_data,
            frame_depth=None, frame_classes=None, frame_ids=None,
            color_to_object_id=None, detected_objects=None,
            detected_object_masks=None, bounds2D=None, masks=None):
        self.pose = pose
        self.metadata = metadata
        #self.image = image_data
        # decode image from string encoding
        self.frame = image_data
        self.frame_depth = frame_depth
        self.frame_classes = frame_classes
        self.frame_ids = frame_ids
        self.color_to_object_id = color_to_object_id
        self.detected_objects = detected_objects
        self.detected_object_masks = detected_object_masks # XXX deprecate/remove, no longer needed
        self.bounds2D = bounds2D
        self.masks = masks

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
        self.train_timer = 0
        self.debug_frames_per_interval = 50
        self.xwindow_id = None
        self.wsgi_server = werkzeug.serving.BaseWSGIServer(host, self.port, self.app)
        # used to ensure that we are receiving frames for the action we sent
        self.sequence_id = 0
        self.color_arr = None
        self.color_names = None
        self.color_to_object_id = None

        @app.route('/ping', methods=['get'])
        def ping():
            return 'pong'

        @app.route('/train', methods=['post'])
        def train():
            server_process_start = time.time()
            import pdb
            start_time = time.time()

            if self.client_token and False:
                token = request.form['token']
                if token is None or token != self.client_token:
                    abort(403)

            if self.frame_counter % self.debug_frames_per_interval == 0:
                now = time.time()
                rate = self.debug_frames_per_interval / float(now - self.last_rate_timestamp)
                print("%s %.3f/s" % (datetime.datetime.now().isoformat(), rate))
                self.last_rate_timestamp = now

            metadata = json.loads(request.form['metadata'])
            if metadata['sequenceId'] != self.sequence_id:
                raise Exception("Sequence id mismatch: %s vs %s" % (
                    metadata['sequenceId'], self.sequence_id))

            print(list(request.files.keys()))

            #    image = request.files['image']
            #    image_data = image.read()
            #    #image_data = np.asarray(Image.open(io.BytesIO(image_data)))
            #    #image_data = scipy.misc.imread(io.BytesIO(image_data))
            #    image_data = read_image(image_data, metadata)
            #else:
            #    image_data = np.zeros((constants.SCREEN_HEIGHT, constants.SCREEN_WIDTH, 3), dtype=np.uint8)

            #if constants.PROCESS_DEPTH and 'image_depth' in request.files:
            #    image_depth = request.files['image_depth']
            #    image_depth_data = image_depth.read()
            #    '''
            #    image_depth = np.asarray(Image.open(io.BytesIO(image_depth_data)), dtype=np.int16)  # decode image from string encoding
            #    image_depth_out = image_depth[:,:,0] * 2
            #    image_depth_out[image_depth_out == 510] = image_depth[image_depth_out == 510, 1] * 4
            #    image_depth_out[image_depth_out == 1020] = image_depth[image_depth_out == 1020, 2] * 8
            #    image_depth_out = image_depth_out.astype(np.float32)
            #    image_depth_out *= (1000 / 255.0)
            #    image_depth = image_depth_out
            #    '''
            #    #image_depth = np.asarray(Image.open(io.BytesIO(image_depth_data)), dtype=np.uint8)
            #    image_depth = read_image(image_depth_data, metadata).astype(np.float32)
            #    max_spots = image_depth[:,:,0] == 255
            #    image_depth_out = image_depth[:,:,0] + image_depth[:,:,1] / 256 + image_depth[:,:,2] / 256 ** 2
            #    image_depth_out[max_spots] = 256
            #    image_depth_out *= 10.0 / 256.0 * 1000 # converts to meters then to mm
            #    image_depth_out[image_depth_out > constants.MAX_DEPTH] = constants.MAX_DEPTH
            #    image_depth = image_depth_out.astype(np.float32)
            #else:
            #    image_depth = None
            detections = {} #defaultdict(list)
            detection_images = {} #defaultdict(list)
            # XXX
            # semantic_seg = np.zeros((image_data.shape[0], image_data.shape[1], constants.NUM_CLASSES), dtype=np.uint8)

            #if 'image_ids' in request.files and constants.PROCESS_SEGMENTATION:
            #    image_ids = request.files['image_ids']
            #    image_ids_data = image_ids.read()
            #    image_ids = read_image(image_ids_data, metadata)

            #    if 'image_classes' in request.files and constants.PROCESS_CLASS_SEGMENTATION:
            #        image_classes = request.files['image_classes']
            #        image_classes_data = image_classes.read()
            #        image_classes = read_image(image_classes_data, metadata)

            #   if len(metadata['colors']) > 0:
            #       self.color_arr = []
            #       self.color_names = []
            #       self.color_to_object_id = {}
            #       for color_data in metadata['colors']:
            #           color = np.array([color_data['color']['r'], color_data['color']['g'], color_data['color']['b']])
            #           color *= 255
            #           color = np.round(color) # deals with very close round off errors
            #           #name = ''.join([x for x in color_data['name'] if x.isalpha()]).lower() # Keep only alpha chars
            #           name = color_data['name']
            #           self.color_arr.append(color)
            #           self.color_names.append(name)
            #           self.color_to_object_id[tuple(int(cc) for cc in color)] = name
            #       self.color_arr = np.array(self.color_arr, dtype=np.uint8)

            #    bounds2D = {}
            #    masks = {}
            #    if self.color_arr is not None and len(self.color_arr) > 0:
            #        object_id_image = image_ids
            #        unique_ids, unique_inverse = game_util.unique_rows(object_id_image.reshape(-1, 3), return_inverse=True)
            #        unique_inverse = unique_inverse.reshape(object_id_image.shape[:2])
            #        unique_masks = (np.tile(unique_inverse[np.newaxis, :, :], (len(unique_ids), 1, 1)) == np.arange(len(unique_ids))[:, np.newaxis, np.newaxis])
            #        #for unique_color_ind, unique_color in enumerate(unique_ids):
            #        for color_bounds in metadata['colorBounds']:
            #            color = np.array([color_bounds['color']['r'], color_bounds['color']['g'], color_bounds['color']['b']])
            #            color *= 255
            #            color = np.round(color) # deals with very close round off errors
            #            '''
            #            try:
            #                color_name = self.color_to_object_id[tuple(int(cc) for cc in unique_color)]
            #            '''
            #            try:
            #                color_name = self.color_to_object_id[tuple(int(cc) for cc in color)]
            #            except:
            #                color_name = 'background'
            #            cls = color_name.lower()
            #            simObj = False
            #            if '|' in cls:
            #                cls = cls.split('|')[0]
            #                simObj = True
            #            elif 'sink' in cls:
            #                cls = 'sink'
            #                simObj = True
            #                color_name = game_util.get_objects_of_type('Sink', metadata)[0]['objectId']

            #            if cls not in constants.COCO_CLASS_TO_OBJECT_CLASS:
            #                continue
            #            cls = constants.COCO_CLASS_TO_OBJECT_CLASS[cls]
            #            '''
            #            coords = np.where(unique_masks[unique_color_ind, :, :])
            #            semantic_seg[coords[0],coords[1],constants.OBJECT_CLASS_TO_ID[cls]] += 1
            #            bb = np.array([
            #                coords[1].min(),
            #                coords[0].min(),
            #                coords[1].max(),
            #                coords[0].max(),
            #                ])
            #            '''
            #            bb = np.array(color_bounds['bounds'])
            #            bb[[1,3]] = metadata['screenHeight'] - bb[[3,1]]
            #            if not((bb[2] - bb[0]) < constants.MIN_DETECTION_LEN or (bb[3] - bb[1]) < constants.MIN_DETECTION_LEN):
            #                if cls not in detections:
            #                    detections[cls] = []
            #                    detection_images[cls] = []
            #                detections[cls].append(bb)
            #                #detection_images[cls].append(unique_masks[unique_color_ind, :, :])
            #                if simObj:
            #                    bounds2D[color_name] = bb
            #                    color_ind = np.argmin(np.sum(np.abs(unique_ids - color), axis=1))
            #                    masks[color_name] = unique_masks[color_ind, ...]
            #else:
            #    image_ids = None
            #    bounds2D = {}
            #    masks = {}
            #    image_classes = np.zeros_like(image_data)

            #'''
            #metadata_detections = [obj for obj in metadata['objects'] if len(obj['bounds2D']) > 0]
            #for obj in metadata_detections:
            #    bb = np.array(obj['bounds2D'])
            #    if not(bb[2] - bb[0] < 5 or bb[3] - bb[1] < 5):
            #        if obj['objectType'] in constants.OBJECT_CLASS_TO_COCO_CLASS and constants.OBJECT_CLASS_TO_COCO_CLASS[obj['objectType']] in detections:
            #            curr_boxes = np.array(detections[constants.OBJECT_CLASS_TO_COCO_CLASS[obj['objectType']]])
            #            if np.max(IOU.IOU_numpy(curr_boxes, bb)) < 0.5 and np.min(np.max(np.abs(curr_boxes - bb), axis=1)) > 10:
            #                detections[constants.OBJECT_CLASS_TO_COCO_CLASS[obj['objectType']]].append(np.array(obj['bounds2D']))
            #'''

            # pose = game_util.get_pose(metadata)

            if image_ids is not None:
                for obj in metadata['objects']:
                    if obj['visible'] and obj['objectId'] not in bounds2D:
                        obj['visible'] = False
            event = Event(pose, metadata, image_data, image_depth, semantic_seg, image_ids, self.color_to_object_id, detections, detection_images, bounds2D, masks)
            print('process time %.3FPS' % (1.0 / (time.time() - start_time)))

            request_queue.put_nowait(event)

            self.train_timer += time.time() - server_process_start
            if self.frame_counter % self.debug_frames_per_interval == 0:
                print('server_process_time %.3f FPS' % (self.debug_frames_per_interval / self.train_timer))
                self.train_timer = 0

            self.frame_counter += 1
            t0 = time.time()
            next_action = queue_get(response_queue)
            #print('response queue', time.time() - t0)
            if 'sequenceId' not in next_action:
                self.sequence_id += 1
                next_action['sequenceId'] = self.sequence_id
            else:
                self.sequence_id = next_action['sequenceId']

            resp = make_response(json.dumps(next_action))

            return resp

    def start(self):
        self.wsgi_server.serve_forever()
