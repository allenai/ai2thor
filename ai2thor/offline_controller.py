import copy

import glob
import shutil
import json
import pickle
import ai2thor.controller
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
            raise Exception(
                "must adjust granularity of key_for_point for smaller grid sizes"
            )

    def reset(self, scene_name):
        self.scene_name = scene_name
        with open("%s/%s/index.json" % (self.base_dir, self.scene_name)) as f:
            self.positions = json.loads(f.read())

        # for g in glob.glob('%s/%s/metadata/*.json' % (self.base_dir,self.scene_name)):
        #    with open(g) as f:
        #        j = json.loads(f.read())
        #        pos = j['agent']['position']
        #        key = key_for_point(pos['x'], pos['z'])
        #        pos_id = os.path.splitext(os.path.basename(g))[0]
        #        event_path = os.path.join('%s/%s/events/%s.pickle' % (self.base_dir, self.scene_name, pos_id))
        #        self.positions[key].append({'event': event_path, 'metadata': j})

        # import random
        # total = len(self.positions)

        # p = self.positions[list(self.positions.keys())[random.randint(0, total - 1)]][3]
        # self.last_event = self.load_event(p)

    @property
    def position_x(self):
        return self.last_event.metadata["agent"]["position"]["x"]

    @property
    def position_z(self):
        return self.last_event.metadata["agent"]["position"]["z"]

    @property
    def rotation(self):
        return self.last_event.metadata["agent"]["rotation"]["y"]

    @property
    def camera_horizon(self):
        return self.last_event.metadata["agent"]["cameraHorizon"]

    def start(self):
        pass

    def load_event(self, pos):
        with open(pos["event"], "rb") as f:
            e = pickle.load(f)
        return e

    def find_position(self, x, z, rotation, camera_horizon):
        for p in self.positions.get(ai2thor.controller.key_for_point(x, z), []):
            if (
                abs(p["rotation"] - rotation) < 1.0
                and abs(p["cameraHorizon"] - camera_horizon) < 1.0
            ):
                event = self.load_event(p)
                return event

        return None

    def move(self, x, z):
        return self.find_position(x, z, self.rotation, self.camera_horizon)

    def move_back(self):
        m = MOVE_AHEAD_MAP[self.rotation]
        new_x = (-m["x"] * self.grid_size) + self.position_x
        new_z = (-m["z"] * self.grid_size) + self.position_z
        return self.move(new_x, new_z)

    def move_ahead(self):
        m = MOVE_AHEAD_MAP[self.rotation]
        new_x = (m["x"] * self.grid_size) + self.position_x
        new_z = (m["z"] * self.grid_size) + self.position_z
        return self.move(new_x, new_z)

    def look(self, new_horizon):
        if new_horizon < -30 or new_horizon > 30:
            return None
        else:
            return self.find_position(
                self.position_x, self.position_z, self.rotation, new_horizon
            )

    def look_up(self):
        return self.look(self.camera_horizon - 30)

    def look_down(self):
        return self.look(self.camera_horizon + 30)

    def rotate_right(self):
        new_rotation = (self.rotation + 90) % 360
        return self.rotate(new_rotation)

    def rotate(self, new_rotation):
        return self.find_position(
            self.position_x, self.position_z, new_rotation, self.camera_horizon
        )

    def rotate_left(self):
        new_rotation = (self.rotation - 90) % 360
        return self.rotate(new_rotation)

    def step(self, action=None, **action_args):
        if type(action) is dict:
            action = copy.deepcopy(action)  # prevent changes from leaking
        else:
            action = dict(action=action)

        actions = dict(
            RotateRight=self.rotate_right,
            RotateLeft=self.rotate_left,
            MoveAhead=self.move_ahead,
            MoveBack=self.move_back,
            LookUp=self.look_up,
            LookDown=self.look_down,
        )

        event = actions[action["action"]]()
        if event is None:
            event = copy.deepcopy(self.last_event)
            event.metadata["lastActionSuccess"] = False
            event.metadata["lastAction"] = action["action"]

        self.last_event = event
        return event


class FrameCounter:
    def __init__(self):
        self.counter = 0

    def inc(self):
        self.counter += 1


def write_frame(event, base_dir, scene_name, frame_name):

    events_dir = "%s/%s/events" % (base_dir, scene_name)
    met_dir = "%s/%s/metadata" % (base_dir, scene_name)
    os.makedirs(met_dir, exist_ok=True)
    os.makedirs(events_dir, exist_ok=True)

    with open(events_dir + "/%03d.pickle" % frame_name, "wb") as f:
        pickle.dump(event, f)

    with open(met_dir + "/%03d.json" % frame_name, "w") as f:
        f.write(json.dumps(event.metadata))


def look_up_down_write(controller, base_dir, fc, scene_name):

    fc.inc()
    write_frame(controller.step(action="LookUp"), base_dir, scene_name, fc.counter)
    fc.inc()
    write_frame(controller.step(action="LookDown"), base_dir, scene_name, fc.counter)
    fc.inc()
    write_frame(controller.step(action="LookDown"), base_dir, scene_name, fc.counter)
    controller.step(action="LookUp")


def index_metadata(base_dir, scene_name):
    positions_index = defaultdict(list)
    for g in glob.glob("%s/%s/metadata/*.json" % (base_dir, scene_name)):
        with open(g) as f:
            j = json.loads(f.read())
            agent = j["agent"]
            pos = agent["position"]
            key = ai2thor.controller.key_for_point(pos["x"], pos["z"])
            pos_id = os.path.splitext(os.path.basename(g))[0]
            event_path = os.path.join(
                "%s/%s/events/%s.pickle" % (base_dir, scene_name, pos_id)
            )
            positions_index[key].append(
                {
                    "event": event_path,
                    "rotation": agent["rotation"]["y"],
                    "cameraHorizon": agent["cameraHorizon"],
                }
            )
    with open("%s/%s/index.json" % (base_dir, scene_name), "w") as f:
        f.write(json.dumps(positions_index))


def dump_scene_controller(base_dir, controller):
    if controller.last_event is None:
        raise Exception("Controller must be reset and intialized to a scene")

    scene_name = controller.last_event.metadata["sceneName"]
    fc = FrameCounter()

    shutil.rmtree("%s/%s" % (base_dir, scene_name), ignore_errors=True)

    event = controller.step(action="GetReachablePositions")
    for p in event.metadata["actionReturn"]:
        action = copy.deepcopy(p)
        action["action"] = "TeleportFull"
        action["horizon"] = 0.0
        action["forceAction"] = True
        action["rotation"] = dict(y=0.0)
        event = controller.step(action)
        print(fc.counter)
        if event.metadata["lastActionSuccess"]:
            look_up_down_write(controller, base_dir, fc, scene_name)
            for i in range(3):
                controller.step(action="RotateRight")
                look_up_down_write(controller, base_dir, fc, scene_name)

    index_metadata(base_dir, scene_name)


def dump_scene(
    scene_name,
    base_dir,
    renderInstanceSegmentation=False,
    renderDepthImage=False,
    renderSemanticSegmentation=False,
):
    controller = ai2thor.controller.Controller()
    controller.start(height=448, width=448)
    controller.reset(scene_name)
    controller.step(
        dict(
            action="Initialize",
            fieldOfView=90,
            gridSize=0.25,
            renderDepthImage=renderDepthImage,
            renderInstanceSegmentation=renderInstanceSegmentation,
            renderSemanticSegmentation=renderSemanticSegmentation,
        )
    )
    dump_scene_controller(base_dir, controller)
    controller.stop()
