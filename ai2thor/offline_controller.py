import copy
import glob
import shutil
import json
import pickle
import numpy as np
import cv2
from ai2thor.controller import key_for_point
import ai2thor.controller
from ai2thor.server import Event
from collections import defaultdict
import os

MOVE_AHEAD_MAP = {
    0: dict(x=0, z=1),
    90: dict(x=1, z=0),
    180: dict(x=0, z=-1),
    270: dict(x=-1, z=0),
    }

class Controller(object):

    def __init__(self, base_dir, grid_size=0.25):

        self.grid_size = grid_size
        self.base_dir = base_dir
        if grid_size < 0.25:
            raise Exception("must adjust granularity of key_for_point for smaller grid sizes")


    def reset(self, scene_name):
        self.scene_name = scene_name
        self.positions = defaultdict(list)
        for g in glob.glob('%s/%s/metadata/*.json' % (self.base_dir,self.scene_name)):
            with open(g) as f:
                j = json.loads(f.read())
                pos = j['agent']['position']
                key = key_for_point(pos['x'], pos['z'])
                pos_id = os.path.splitext(os.path.basename(g))[0]
                event_path = os.path.join('%s/%s/events/%s.pickle' % (self.base_dir, self.scene_name, pos_id))
                self.positions[key].append({'event': event_path, 'metadata': j})

        p = self.positions[list(self.positions.keys())[50]][0]
        self.last_event = self.load_event(p)

    @property
    def position_x(self):
        return self.last_event.metadata['agent']['position']['x']

    @property
    def position_z(self):
        return self.last_event.metadata['agent']['position']['z']

    @property
    def rotation(self):
        return self.last_event.metadata['agent']['rotation']['y']

    @property
    def camera_horizon(self):
        return self.last_event.metadata['agent']['cameraHorizon']

    def start(self):
        pass
    
    def load_event(self, pos):
        with open(pos['event'], 'rb') as f:
            e = pickle.load(f)
        return e

    def find_position(self, x, z, rotation, camera_horizon):
        for p in self.positions[key_for_point(x, z)]:
            met = p['metadata']
            if abs(met['agent']['rotation']['y'] - rotation) < 1.0 and abs(met['agent']['cameraHorizon'] - camera_horizon) < 1.0:

                event = self.load_event(p)
                return event

        return None

    def move(self, x, z):
        return self.find_position(x, z, self.rotation, self.camera_horizon)

    def move_back(self):
        m = MOVE_AHEAD_MAP[self.rotation]
        new_x = (-m['x'] * self.grid_size) + self.position_x
        new_z = (-m['z'] * self.grid_size) + self.position_z
        return self.move(new_x, new_z)

    def move_ahead(self):
        m = MOVE_AHEAD_MAP[self.rotation]
        new_x = (m['x'] * self.grid_size) + self.position_x
        new_z = (m['z'] * self.grid_size) + self.position_z
        return self.move(new_x, new_z)
    
    def look(self, new_horizon):
        if new_horizon < -30 or new_horizon > 30:
            return None
        else:
            return self.find_position(self.position_x, self.position_z, self.rotation, new_horizon)
            


    def look_up(self):
        return self.look(self.camera_horizon - 30)

    def look_down(self):
        return self.look(self.camera_horizon + 30)
    
    def rotate_right(self):
        new_rotation = (self.rotation + 90) % 360
        return self.rotate(new_rotation)

    def rotate(self, new_rotation):
        return self.find_position(self.position_x, self.position_z, new_rotation, self.camera_horizon)

    def rotate_left(self):
        new_rotation = (self.rotation - 90) % 360
        return self.rotate(new_rotation)

    def step(self, action=None, **action_args):
        if type(action) is dict:
            action = copy.deepcopy(action) # prevent changes from leaking
        else:
            action = dict(action=action)
       
        actions = dict(
            RotateRight=self.rotate_right,
            RotateLeft=self.rotate_left,
            MoveAhead=self.move_ahead,
            MoveBack=self.move_back,
            LookUp=self.look_up,
            LookDown=self.look_down
        )
         
        event = actions[action['action']]()
        if event is None:
            event = copy.deepcopy(self.last_event)
            event.metadata['lastActionSuccess'] = False
            event.metadata['lastAction'] = action['action']

        self.last_event = event
        return event

class FrameCounter:

    def __init__(self):
        self.counter = 0

    def inc(self):
        self.counter += 1

def write_frame(event, base_dir, scene_name, frame_name):
    import cv2
    import os
    import json
    events_dir = "%s/%s/events" % (base_dir, scene_name)
    met_dir = "%s/%s/metadata" % (base_dir, scene_name)
    os.makedirs(met_dir, exist_ok=True)
    os.makedirs(events_dir, exist_ok=True)

    #cv2.imwrite(images_dir + "/%03d.png" % frame_name, event.cv2img)
    #np.save(depth_images_dir + "/%03d.npy" % frame_name, event.depth_frame)



    with open(events_dir + "/%03d.pickle" % frame_name, "wb") as f:
        pickle.dump(event, f)

    with open(met_dir + "/%03d.json" % frame_name, "w") as f:
        f.write(json.dumps(event.metadata))

def look_up_down_write(controller, base_dir, fc, scene_name):

    fc.inc()
    write_frame(controller.step(action='LookUp'), base_dir, scene_name, fc.counter)
    fc.inc()
    write_frame(controller.step(action='LookDown'), base_dir, scene_name, fc.counter)
    fc.inc()
    write_frame(controller.step(action='LookDown'), base_dir, scene_name, fc.counter)
    controller.step(action='LookUp')


def dump_scene(scene_name, base_dir, renderObjectImage=False, renderDepthImage=False, renderClassImage=False):
    controller = ai2thor.controller.Controller()
    controller.start(player_screen_height=448, player_screen_width=448)
    fc = FrameCounter()

    shutil.rmtree("%s/%s" % (base_dir, scene_name), ignore_errors=True)

    controller.reset(scene_name) 
    event = controller.step(dict(action='Initialize', gridSize=0.25, fieldOfView=90, renderDepthImage=renderDepthImage, renderObjectImage=renderObjectImage, renderClassImage=renderClassImage))
    event = controller.step(action='GetReachablePositions')
    for p in event.metadata['reachablePositions']:
        action = copy.deepcopy(p)
        action['action'] = 'TeleportFull'
        action['horizon'] = 0.0
        action['forceAction'] = True
        action['rotation'] = dict(y=0.0)
        event = controller.step(action)
        print(fc.counter)
        if event.metadata['lastActionSuccess']:
            look_up_down_write(controller, base_dir, fc, scene_name)
            for i in range(3):
                event = controller.step(action='RotateRight')
                look_up_down_write(controller, base_dir, fc, scene_name)

