# Copyright Allen Institute for Artificial Intelligence 2017
"""
ai2thor.controller

Primary entrypoint into the Thor API. Provides all the high-level functions
needed to control the in-game agent through ai2thor.server.

"""
import atexit
from collections import deque, defaultdict
from itertools import product
import io
import json
import copy
import logging
import fcntl
import math
import time
import random
import shlex
import signal
import subprocess
import shutil
import threading
import os
import platform
import uuid
import tty
import sys
import termios
try:
    from queue import Queue
except ImportError:
    from Queue import Queue

import zipfile

import numpy as np

import ai2thor.docker
import ai2thor.downloader
import ai2thor.server
from ai2thor.server import queue_get
from ai2thor._builds import BUILDS
from ai2thor._quality_settings import QUALITY_SETTINGS, DEFAULT_QUALITY

logger = logging.getLogger(__name__)

RECEPTACLE_OBJECTS = {
    'Box': {'Candle',
            'CellPhone',
            'Cloth',
            'CreditCard',
            'Dirt',
            'KeyChain',
            'Newspaper',
            'ScrubBrush',
            'SoapBar',
            'SoapBottle',
            'ToiletPaper'},
    'Cabinet': {'Bowl',
                'BowlDirty',
                'Box',
                'Bread',
                'BreadSliced',
                'ButterKnife',
                'Candle',
                'CellPhone',
                'Cloth',
                'CoffeeMachine',
                'Container',
                'ContainerFull',
                'CreditCard',
                'Cup',
                'Fork',
                'KeyChain',
                'Knife',
                'Laptop',
                'Mug',
                'Newspaper',
                'Pan',
                'Plate',
                'Plunger',
                'Pot',
                'Potato',
                'Sandwich',
                'ScrubBrush',
                'SoapBar',
                'SoapBottle',
                'Spoon',
                'SprayBottle',
                'Statue',
                'TissueBox',
                'Toaster',
                'ToiletPaper',
                'WateringCan'},
    'CoffeeMachine': {'MugFilled', 'Mug'},
    'CounterTop': {'Apple',
                   'AppleSlice',
                   'Bowl',
                   'BowlDirty',
                   'BowlFilled',
                   'Box',
                   'Bread',
                   'BreadSliced',
                   'ButterKnife',
                   'Candle',
                   'CellPhone',
                   'CoffeeMachine',
                   'Container',
                   'ContainerFull',
                   'CreditCard',
                   'Cup',
                   'Egg',
                   'EggFried',
                   'EggShell',
                   'Fork',
                   'HousePlant',
                   'KeyChain',
                   'Knife',
                   'Laptop',
                   'Lettuce',
                   'LettuceSliced',
                   'Microwave',
                   'Mug',
                   'MugFilled',
                   'Newspaper',
                   'Omelette',
                   'Pan',
                   'Plate',
                   'Plunger',
                   'Pot',
                   'Potato',
                   'PotatoSliced',
                   'RemoteControl',
                   'Sandwich',
                   'ScrubBrush',
                   'SoapBar',
                   'SoapBottle',
                   'Spoon',
                   'SprayBottle',
                   'Statue',
                   'Television',
                   'TissueBox',
                   'Toaster',
                   'ToiletPaper',
                   'Tomato',
                   'TomatoSliced',
                   'WateringCan'},
    'Fridge': {'Apple',
               'AppleSlice',
               'Bowl',
               'BowlDirty',
               'BowlFilled',
               'Bread',
               'BreadSliced',
               'Container',
               'ContainerFull',
               'Cup',
               'Egg',
               'EggFried',
               'EggShell',
               'Lettuce',
               'LettuceSliced',
               'Mug',
               'MugFilled',
               'Omelette',
               'Pan',
               'Plate',
               'Pot',
               'Potato',
               'PotatoSliced',
               'Sandwich',
               'Tomato',
               'TomatoSliced'},
    'GarbageCan': {'Apple',
                   'AppleSlice',
                   'Box',
                   'Bread',
                   'BreadSliced',
                   'Candle',
                   'CellPhone',
                   'CreditCard',
                   'Egg',
                   'EggFried',
                   'EggShell',
                   'LettuceSliced',
                   'Newspaper',
                   'Omelette',
                   'Plunger',
                   'Potato',
                   'PotatoSliced',
                   'Sandwich',
                   'ScrubBrush',
                   'SoapBar',
                   'SoapBottle',
                   'SprayBottle',
                   'Statue',
                   'ToiletPaper',
                   'Tomato',
                   'TomatoSliced'},
    'Microwave': {'Bowl',
                  'BowlDirty',
                  'BowlFilled',
                  'Bread',
                  'BreadSliced',
                  'Container',
                  'ContainerFull',
                  'Cup',
                  'Egg',
                  'EggFried',
                  'Mug',
                  'MugFilled',
                  'Omelette',
                  'Plate',
                  'Potato',
                  'PotatoSliced',
                  'Sandwich'},
    'PaintingHanger': {'Painting'},
    'Pan': {'Apple',
            'AppleSlice',
            'EggFried',
            'Lettuce',
            'LettuceSliced',
            'Omelette',
            'Potato',
            'PotatoSliced',
            'Tomato',
            'TomatoSliced'},
    'Pot': {'Apple',
            'AppleSlice',
            'EggFried',
            'Lettuce',
            'LettuceSliced',
            'Omelette',
            'Potato',
            'PotatoSliced',
            'Tomato',
            'TomatoSliced'},
    'Sink': {'Apple',
             'AppleSlice',
             'Bowl',
             'BowlDirty',
             'BowlFilled',
             'ButterKnife',
             'Container',
             'ContainerFull',
             'Cup',
             'Egg',
             'EggFried',
             'EggShell',
             'Fork',
             'Knife',
             'Lettuce',
             'LettuceSliced',
             'Mug',
             'MugFilled',
             'Omelette',
             'Pan',
             'Plate',
             'Pot',
             'Potato',
             'PotatoSliced',
             'Sandwich',
             'ScrubBrush',
             'SoapBottle',
             'Spoon',
             'Tomato',
             'TomatoSliced',
             'WateringCan'},
    'StoveBurner': {'Omelette', 'Pot', 'Pan', 'EggFried'},
    'TableTop': {'Apple',
                 'AppleSlice',
                 'Bowl',
                 'BowlDirty',
                 'BowlFilled',
                 'Box',
                 'Bread',
                 'BreadSliced',
                 'ButterKnife',
                 'Candle',
                 'CellPhone',
                 'CoffeeMachine',
                 'Container',
                 'ContainerFull',
                 'CreditCard',
                 'Cup',
                 'Egg',
                 'EggFried',
                 'EggShell',
                 'Fork',
                 'HousePlant',
                 'KeyChain',
                 'Knife',
                 'Laptop',
                 'Lettuce',
                 'LettuceSliced',
                 'Microwave',
                 'Mug',
                 'MugFilled',
                 'Newspaper',
                 'Omelette',
                 'Pan',
                 'Plate',
                 'Plunger',
                 'Pot',
                 'Potato',
                 'PotatoSliced',
                 'RemoteControl',
                 'Sandwich',
                 'ScrubBrush',
                 'SoapBar',
                 'SoapBottle',
                 'Spoon',
                 'SprayBottle',
                 'Statue',
                 'Television',
                 'TissueBox',
                 'Toaster',
                 'ToiletPaper',
                 'Tomato',
                 'TomatoSliced',
                 'WateringCan'},
    'ToiletPaperHanger': {'ToiletPaper'},
    'TowelHolder': {'Cloth'}}


def get_term_character():
    fd = sys.stdin.fileno()
    old_settings = termios.tcgetattr(fd)
    try:
        tty.setraw(sys.stdin.fileno())
        ch = sys.stdin.read(1)
    finally:
        termios.tcsetattr(fd, termios.TCSADRAIN, old_settings)
    return ch

def process_alive(pid):
    """
    Use kill(0) to determine if pid is alive
    :param pid: process id
    :rtype: bool
    """
    try:
        os.kill(pid, 0)
    except OSError:
        return False

    return True

# python2.7 compatible makedirs
def makedirs(directory):
    if not os.path.isdir(directory):
        os.makedirs(directory)

def distance(point1, point2):
    x_diff = (point1['x'] - point2['x']) ** 2
    z_diff = (point1['z'] - point2['z']) ** 2
    return math.sqrt(x_diff + z_diff)


def key_for_point(x, z):
    return "%0.1f %0.1f" % (x, z)

class Controller(object):

    def __init__(self, quality=DEFAULT_QUALITY, fullscreen=False):
        self.request_queue = Queue(maxsize=1)
        self.response_queue = Queue(maxsize=1)
        self.receptacle_nearest_pivot_points = {}
        self.server = None
        self.unity_pid = None
        self.docker_enabled = False
        self.container_id = None
        self.local_executable_path = None
        self.last_event = None
        self.server_thread = None
        self.killing_unity = False
        self.quality = quality
        self.lock_file = None
        self.fullscreen = fullscreen

    def reset(self, scene_name=None):
        self.response_queue.put_nowait(dict(action='Reset', sceneName=scene_name, sequenceId=0))
        self.last_event = queue_get(self.request_queue)

        return self.last_event

    def random_initialize(
            self,
            random_seed=None,
            randomize_open=False,
            unique_object_types=False,
            exclude_receptacle_object_pairs=[],
            max_num_repeats=1,
            remove_prob=0.5):

        receptacle_objects = []

        for rec_obj_type, object_types in RECEPTACLE_OBJECTS.items():
            receptacle_objects.append(
                dict(receptacleObjectType=rec_obj_type, itemObjectTypes=list(object_types))
            )
        if random_seed is None:
            random_seed = random.randint(0, 2**32)

        exclude_object_ids = []

        for obj in self.last_event.metadata['objects']:
            pivot_points = self.receptacle_nearest_pivot_points
            # don't put things in pot or pan currently
            if (pivot_points and obj['receptacle'] and
                    pivot_points[obj['objectId']].keys()) or obj['objectType'] in ['Pot', 'Pan']:

                #print("no visible pivots for receptacle %s" % o['objectId'])
                exclude_object_ids.append(obj['objectId'])

        return self.step(dict(
            action='RandomInitialize',
            receptacleObjects=receptacle_objects,
            randomizeOpen=randomize_open,
            uniquePickupableObjectTypes=unique_object_types,
            excludeObjectIds=exclude_object_ids,
            excludeReceptacleObjectPairs=exclude_receptacle_object_pairs,
            maxNumRepeats=max_num_repeats,
            removeProb=remove_prob,
            randomSeed=random_seed))

    def scene_names(self):
        scenes = []
        for low, high in [(1,31), (201, 231), (301, 331), (401, 431)]:
            for i in range(low, high):
                scenes.append('FloorPlan%s' % i)

        return scenes

    def unlock_release(self):
        if self.lock_file:
            fcntl.flock(self.lock_file, fcntl.LOCK_UN)

    def lock_release(self):
        build_dir = os.path.join(self.releases_dir(), self.build_name())
        if os.path.isdir(build_dir):
            self.lock_file = open(os.path.join(build_dir, ".lock"), "w")
            fcntl.flock(self.lock_file, fcntl.LOCK_SH)

    def prune_releases(self):
        current_exec_path = self.executable_path()
        for d in os.listdir(self.releases_dir()):
            release = os.path.join(self.releases_dir(), d)

            if current_exec_path.startswith(release):
                continue

            if os.path.isdir(release):
                try:
                    with open(os.path.join(release, ".lock"), "w") as f:
                        fcntl.flock(f, fcntl.LOCK_EX | fcntl.LOCK_NB)
                        shutil.rmtree(release)
                except Exception as e:
                    pass

    def next_interact_command(self):
        current_buffer = ''
        while True:
            commands = self._interact_commands
            current_buffer += get_term_character()
            if current_buffer == 'q' or current_buffer == '\x03':
                break

            if current_buffer in commands:
                yield commands[current_buffer]
                current_buffer = ''
            else:
                match = False
                for k,v in commands.items():
                    if k.startswith(current_buffer):
                        match = True
                        break

                if not match:
                    current_buffer = ''

    def interact(self):
        default_interact_commands = {
            '\x1b[C': dict(action='MoveRight', moveMagnitude=0.25),
            '\x1b[D': dict(action='MoveLeft', moveMagnitude=0.25),
            '\x1b[A': dict(action='MoveAhead', moveMagnitude=0.25),
            '\x1b[B': dict(action='MoveBack', moveMagnitude=0.25),
            '\x1b[1;2A': dict(action='LookUp'),
            '\x1b[1;2B': dict(action='LookDown'),
            '\x1b[1;2C': dict(action='RotateRight'),
            '\x1b[1;2D': dict(action='RotateLeft')
        }

        self._interact_commands = default_interact_commands.copy()

        command_message = u"Enter a Command: Move \u2190\u2191\u2192\u2193, Rotate/Look Shift + \u2190\u2191\u2192\u2193, Quit 'q' or Ctrl-C"
        print(command_message)
        for a in self.next_interact_command():
            new_commands = {}
            command_counter = dict(counter=1)

            def add_command(cc, action, **args):
                if cc['counter'] < 10:
                    com = dict(action=action)
                    com.update(args)
                    new_commands[str(cc['counter'])] = com
                    cc['counter'] += 1

            event = self.step(a)
            # check inventory
            visible_objects = []
            for o in event.metadata['objects']:
                if o['visible']:
                    visible_objects.append(o['objectId'])
                    if o['openable']:
                        if o['isopen']:
                            add_command(command_counter, 'CloseObject', objectId=o['objectId'])
                        else:
                            add_command(command_counter, 'OpenObject', objectId=o['objectId'])
                    if len(event.metadata['inventoryObjects']) > 0:
                        if o['receptacle'] and (not o['openable'] or o['isopen']):
                            inventoryObjectId = event.metadata['inventoryObjects'][0]['objectId']
                            add_command(command_counter, 'PutObject', objectId=inventoryObjectId, receptacleObjectId=o['objectId'])

                    elif o['pickupable']:
                        add_command(command_counter, 'PickupObject', objectId=o['objectId'])

            self._interact_commands = default_interact_commands.copy()
            self._interact_commands.update(new_commands)

            print(command_message)
            print("Visible Objects:\n" + "\n".join(sorted(visible_objects)))

            skip_keys = ['action', 'objectId']
            for k in sorted(new_commands.keys()):
                v = new_commands[k]
                command_info = [k + ")", v['action']]
                if 'objectId' in v:
                    command_info.append(v['objectId'])

                for a, av in v.items():
                    if a in skip_keys:
                        continue
                    command_info.append("%s: %s" % (a, av))

                print(' '.join(command_info))

    def step(self, action, raise_for_failure=False):

        # prevent changes to the action from leaking
        action = copy.deepcopy(action)

        # XXX should be able to get rid of this with some sort of deprecation warning
        if 'AI2THOR_VISIBILITY_DISTANCE' in os.environ:
            action['visibilityDistance'] = float(os.environ['AI2THOR_VISIBILITY_DISTANCE'])

        should_fail = False
        self.last_action = action

        if ('objectId' in action and (action['action'] == 'OpenObject' or action['action'] == 'CloseObject')):

            force_visible = action.get('forceVisible', False)
            if not force_visible and self.last_event.instance_detections2D and action['objectId'] not in self.last_event.instance_detections2D:
                should_fail = True

            obj_metadata = self.last_event.get_object(action['objectId'])
            if obj_metadata is None or obj_metadata['isopen'] == (action['action'] == 'OpenObject'):
                should_fail = True

        elif action['action'] == 'PutObject':
            receptacle_type = action['receptacleObjectId'].split('|')[0]
            object_type = action['objectId'].split('|')[0]
            if object_type not in RECEPTACLE_OBJECTS[receptacle_type]:
                should_fail = True

        rotation = action.get('rotation')
        if rotation is not None and type(rotation) != dict:
            action['rotation'] = {}
            action['rotation']['y'] = rotation

        if should_fail:
            new_event = copy.deepcopy(self.last_event)
            new_event.metadata['lastActionSuccess'] = False
            self.last_event = new_event
            return new_event

        assert self.request_queue.empty()

        self.response_queue.put_nowait(action)
        self.last_event = queue_get(self.request_queue)

        if not self.last_event.metadata['lastActionSuccess'] and self.last_event.metadata['errorCode'] == 'InvalidAction':
            raise ValueError(self.last_event.metadata['errorMessage'])

        if raise_for_failure:
            assert self.last_event.metadata['lastActionSuccess']

        return self.last_event

    def unity_command(self, width, height):

        command = self.executable_path()
        fullscreen = 1 if self.fullscreen else 0
        command += " -screen-fullscreen %s -screen-quality %s -screen-width %s -screen-height %s" % (fullscreen, QUALITY_SETTINGS[self.quality], width, height)
        return shlex.split(command)

    def _start_unity_thread(self, env, width, height, host, port, image_name):
        # get environment variables

        env['AI2THOR_CLIENT_TOKEN'] = self.server.client_token = str(uuid.uuid4())
        env['AI2THOR_HOST'] = host
        env['AI2THOR_PORT'] = str(port)

        # env['AI2THOR_SERVER_SIDE_SCREENSHOT'] = 'True'

        # print("Viewer: http://%s:%s/viewer" % (host, port))
        command = self.unity_command(width, height)

        if image_name is not None:
            self.container_id = ai2thor.docker.run(image_name, self.base_dir(), ' '.join(command), env)
            atexit.register(lambda: ai2thor.docker.kill_container(self.container_id))
        else:
            proc = subprocess.Popen(command, env=env)
            self.unity_pid = proc.pid
            atexit.register(lambda: proc.poll() is None and proc.kill())
            returncode = proc.wait()
            if returncode != 0 and not self.killing_unity:
                raise Exception("command: %s exited with %s" % (command, returncode))

    def check_docker(self):
        if self.docker_enabled:
            assert ai2thor.docker.has_docker(), "Docker enabled, but could not find docker binary in path"
            assert ai2thor.docker.nvidia_version() is not None,\
                "No nvidia driver version found at /proc/driver/nvidia/version - Dockerized THOR is only \
                    compatible with hosts with Nvidia cards with a driver installed"

    def check_x_display(self, x_display):
        with open(os.devnull, "w") as dn:
            # copying the environment so that we pickup
            # XAUTHORITY values
            env = os.environ.copy()
            env['DISPLAY'] = x_display

            if subprocess.call(['which', 'xdpyinfo'], stdout=dn) == 0:
                assert subprocess.call("xdpyinfo", stdout=dn, env=env, shell=True) == 0, \
                    ("Invalid DISPLAY %s - cannot find X server with xdpyinfo" % x_display)

    def _start_server_thread(self):
        self.server.start()

    def releases_dir(self):
        return os.path.join(self.base_dir(), 'releases')

    def base_dir(self):
        return os.path.join(os.path.expanduser('~'), '.ai2thor')

    def build_name(self):
        return os.path.splitext(os.path.basename(BUILDS[platform.system()]['url']))[0]

    def executable_path(self):

        if self.local_executable_path is not None:
            return self.local_executable_path

        target_arch = platform.system()

        if target_arch == 'Linux':
            return os.path.join(self.releases_dir(), self.build_name(), self.build_name())
        elif target_arch == 'Darwin':
            return os.path.join(
                self.releases_dir(),
                self.build_name(),
                self.build_name() + ".app",
                "Contents/MacOS",
                self.build_name())
        else:
            raise Exception('unable to handle target arch %s' % target_arch)

    def download_binary(self):

        if platform.architecture()[0] != '64bit':
            raise Exception("Only 64bit currently supported")

        url = BUILDS[platform.system()]['url']
        tmp_dir = os.path.join(self.base_dir(), 'tmp')
        makedirs(self.releases_dir())
        makedirs(tmp_dir)

        if not os.path.isfile(self.executable_path()):
            zip_data = ai2thor.downloader.download(
                url,
                self.build_name(),
                BUILDS[platform.system()]['sha256'])

            z = zipfile.ZipFile(io.BytesIO(zip_data))
            # use tmpdir instead or a random number
            extract_dir = os.path.join(tmp_dir, self.build_name())
            logger.debug("Extracting zipfile %s" % os.path.basename(url))
            z.extractall(extract_dir)
            os.rename(extract_dir, os.path.join(self.releases_dir(), self.build_name()))
            # we can lose the executable permission when unzipping a build
            os.chmod(self.executable_path(), 0o755)
        else:
            logger.debug("%s exists - skipping download" % self.executable_path())

    def start(
            self,
            port=0,
            start_unity=True,
            player_screen_width=300,
            player_screen_height=300,
            x_display=None):

        if 'AI2THOR_VISIBILITY_DISTANCE' in os.environ:
            import warnings
            warnings.warn("AI2THOR_VISIBILITY_DISTANCE environment variable is deprecated, use \
                the parameter visibilityDistance parameter with the Initialize action instead")

        if player_screen_height < 300 or player_screen_width < 300:
            raise Exception("Screen resolution must be >= 300x300")

        if self.server_thread is not None:
            raise Exception("server has already been started - cannot start more than once")

        env = os.environ.copy()

        image_name = None
        host = '127.0.0.1'

        if self.docker_enabled:
            self.check_docker()
            host = ai2thor.docker.bridge_gateway()

        self.server = ai2thor.server.Server(
            self.request_queue,
            self.response_queue,
            host,
            port=port)

        _, port = self.server.wsgi_server.socket.getsockname()

        self.server_thread = threading.Thread(target=self._start_server_thread)

        self.server_thread.daemon = True
        self.server_thread.start()

        if start_unity:
            if platform.system() == 'Linux':

                if self.docker_enabled:
                    image_name = ai2thor.docker.build_image()
                else:

                    if x_display:
                        env['DISPLAY'] = ':' + x_display
                    elif 'DISPLAY' not in env:
                        env['DISPLAY'] = ':0.0'

                    self.check_x_display(env['DISPLAY'])

            self.download_binary()
            self.lock_release()
            self.prune_releases()

            unity_thread = threading.Thread(
                target=self._start_unity_thread,
                args=(env, player_screen_width, player_screen_height, host, port, image_name))
            unity_thread.daemon = True
            unity_thread.start()

        # receive the first request
        self.last_event = queue_get(self.request_queue)

        return self.last_event

    def stop(self):
        self.response_queue.put_nowait({})
        self.server.wsgi_server.shutdown()
        self.stop_container()
        self.stop_unity()
        self.unlock_release()

    def stop_container(self):
        if self.container_id:
            ai2thor.docker.kill_container(self.container_id)
            self.container_id = None

    def stop_unity(self):
        if self.unity_pid and process_alive(self.unity_pid):
            self.killing_unity = True
            os.kill(self.unity_pid, signal.SIGTERM)
            for i in range(10):
                if not process_alive(self.unity_pid):
                    break
                time.sleep(0.1)
            if process_alive(self.unity_pid):
                os.kill(self.unity_pid, signal.SIGKILL)

class BFSSearchPoint:
    def __init__(self, start_position, move_vector, heading_angle=0.0, horizon_angle=0.0):
        self.start_position = start_position
        self.move_vector = defaultdict(lambda: 0.0)
        self.move_vector.update(move_vector)
        self.heading_angle = heading_angle
        self.horizon_angle = horizon_angle

    def target_point(self):
        x = self.start_position['x'] + self.move_vector['x']
        z = self.start_position['z'] + self.move_vector['z']
        return dict(x=x, z=z)

class BFSController(Controller):

    def __init__(self, grid_size=0.25):
        super(BFSController, self).__init__()
        self.rotations = [0, 90, 180, 270]
        self.horizons = [330, 0, 30]
        self.allow_enqueue = True
        self.queue = deque()
        self.seen_points = []
        self.visited_seen_points = []
        self.grid_points = []
        self.grid_size = grid_size
        self._check_visited = False
        self.distance_threshold = self.grid_size / 5.0

    def visualize_points(self, scene_name, wait_key=10):
        import cv2
        points = set()
        xs = []
        zs = []

        # Follow the file as it grows
        for point in self.grid_points:
            xs.append(point['x'])
            zs.append(point['z'])
            points.add(str(point['x']) + "," + str(point['z']))

        image_width = 470
        image_height = 530
        image = np.zeros((image_height, image_width, 3), np.uint8)
        if not xs:
            return

        min_x = min(xs) - 1
        max_x = max(xs) + 1
        min_z = min(zs) - 1
        max_z = max(zs) + 1

        for point in list(points):
            x, z = map(float, point.split(','))
            circle_x = round(((x - min_x) / float(max_x - min_x)) * image_width)
            z = (max_z - z) + min_z
            circle_y = round(((z - min_z) / float(max_z - min_z)) * image_height)
            cv2.circle(image, (circle_x, circle_y), 5, (0, 255, 0), -1)

        cv2.imshow(scene_name, image)
        cv2.waitKey(wait_key)

    def has_islands(self):
        queue = []
        seen_points = set()
        mag = self.grid_size

        def enqueue_island_points(p):
            if json.dumps(p) in seen_points:
                return
            queue.append(dict(z=p['z'] + mag, x=p['x']))
            queue.append(dict(z=p['z'] - mag, x=p['x']))
            queue.append(dict(z=p['z'], x=p['x'] + mag))
            queue.append(dict(z=p['z'], x=p['x'] - mag))
            seen_points.add(json.dumps(p))

        enqueue_island_points(self.grid_points[0])

        while queue:
            point_to_find = queue.pop()
            for p in self.grid_points:
                dist = math.sqrt(
                    ((point_to_find['x'] - p['x']) ** 2) +
                    ((point_to_find['z'] - p['z']) ** 2))

                if dist < 0.05:
                    enqueue_island_points(p)

        return len(seen_points) != len(self.grid_points)

    def build_graph(self):
        import networkx as nx
        graph = nx.Graph()
        for point in self.grid_points:
            self._build_graph_point(graph, point)

        return graph

    def key_for_point(self, point):
        return "{x:0.3f}|{z:0.3f}".format(**point)

    def _build_graph_point(self, graph, point):
        for p in self.grid_points:
            dist = math.sqrt(((point['x'] - p['x']) ** 2) + ((point['z'] - p['z']) ** 2))
            if dist <= (self.grid_size + 0.01) and dist > 0:
                graph.add_edge(self.key_for_point(point), self.key_for_point(p))

    def move_relative_points(self, all_points, graph, position, rotation):

        action_orientation = {
            0:dict(x=0, z=1, action='MoveAhead'),
            90:dict(x=1, z=0, action='MoveRight'),
            180:dict(x=0, z=-1, action='MoveBack'),
            270:dict(x=-1, z=0, action='MoveLeft')
        }

        move_points = dict()

        for n in graph.neighbors(self.key_for_point(position)):
            point = all_points[n]
            x_o = round((point['x'] - position['x']) / self.grid_size)
            z_o = round((point['z'] - position['z']) / self.grid_size)
            for target_rotation, offsets in action_orientation.items():
                delta = round(rotation + target_rotation) % 360
                ao = action_orientation[delta]
                action_name = action_orientation[target_rotation]['action']
                if x_o == ao['x'] and z_o == ao['z']:
                    move_points[action_name] = point
                    break

        return move_points

    def plan_horizons(self, agent_horizon, target_horizon):
        actions = []
        horizon_step_map = {330:3, 0:2, 30:1, 60:0}
        look_diff = horizon_step_map[int(agent_horizon)] - horizon_step_map[int(target_horizon)]
        if look_diff > 0:
            for i in range(look_diff):
                actions.append(dict(action='LookDown'))
        else:
            for i in range(abs(look_diff)):
                actions.append(dict(action='LookUp'))

        return actions

    def plan_rotations(self, agent_rotation, target_rotation):
        right_diff = target_rotation - agent_rotation
        if right_diff < 0:
            right_diff += 360
        right_steps = right_diff / 90

        left_diff = agent_rotation - target_rotation
        if left_diff < 0:
            left_diff += 360
        left_steps = left_diff / 90

        actions = []
        if right_steps < left_steps:
            for i in range(int(right_steps)):
                actions.append(dict(action='RotateRight'))
        else:
            for i in range(int(left_steps)):
                actions.append(dict(action='RotateLeft'))

        return actions

    def shortest_plan(self, graph, agent, target):
        import networkx as nx
        path = nx.shortest_path(graph, self.key_for_point(agent['position']), self.key_for_point(target['position']))
        actions = []
        all_points = {}

        for point in self.grid_points:
            all_points[self.key_for_point(point)] = point

        #assert all_points[path[0]] == agent['position']

        current_position = agent['position']
        current_rotation = agent['rotation']['y']

        for p in path[1:]:
            inv_pms = {self.key_for_point(v): k for k, v in self.move_relative_points(all_points, graph, current_position, current_rotation).items()}
            actions.append(dict(action=inv_pms[p]))
            current_position = all_points[p]

        actions += self.plan_horizons(agent['cameraHorizon'], target['cameraHorizon'])
        actions += self.plan_rotations(agent['rotation']['y'], target['rotation']['y'])
        # self.visualize_points(path)

        return actions

    def enqueue_point(self, point):

        # ensure there are no points near the new point
        if self._check_visited or not any(map(lambda p: distance(p, point.target_point()) < self.distance_threshold, self.seen_points)):
            self.seen_points.append(point.target_point())
            self.queue.append(point)

    def enqueue_points(self, agent_position):

        if not self.allow_enqueue:
            return

        if not self._check_visited or not any(map(lambda p: distance(p, agent_position) < self.distance_threshold, self.visited_seen_points)):
            self.enqueue_point(BFSSearchPoint(agent_position, dict(x=-1 * self.grid_size)))
            self.enqueue_point(BFSSearchPoint(agent_position, dict(x=self.grid_size)))
            self.enqueue_point(BFSSearchPoint(agent_position, dict(z=-1 * self.grid_size)))
            self.enqueue_point(BFSSearchPoint(agent_position, dict(z=1 * self.grid_size)))
            self.visited_seen_points.append(agent_position)

    def search_all_closed(self, scene_name):
        self.allow_enqueue = True
        self.queue = deque()
        self.seen_points = []
        self.visited_seen_points = []
        self.grid_points = []
        event = self.reset(scene_name)
        event = self.step(dict(action='Initialize', gridSize=self.grid_size))
        self.enqueue_points(event.metadata['agent']['position'])
        while self.queue:
            self.queue_step()
            # self.visualize_points(scene_name)

    def start_search(
            self,
            scene_name,
            random_seed,
            full_grid,
            current_receptacle_object_pairs,
            randomize=True):

        self.seen_points = []
        self.visited_seen_points = []
        self.queue = deque()
        self.grid_points = []

        # we only search a pre-defined grid with all the cabinets/fridges closed
        # then keep the points that can still be reached
        self.allow_enqueue = True

        for gp in full_grid:
            self.enqueue_points(gp)

        self.allow_enqueue = False

        self.reset(scene_name)
        receptacle_object_pairs = []
        for op in current_receptacle_object_pairs:
            object_id, receptacle_object_id = op.split('||')
            receptacle_object_pairs.append(
                dict(receptacleObjectId=receptacle_object_id,
                     objectId=object_id))

        if randomize:
            self.random_initialize(
                random_seed=random_seed,
                unique_object_types=True,
                exclude_receptacle_object_pairs=receptacle_object_pairs)

        # there is some randomization in initialize scene
        # and if a seed is passed in this will keep it
        # deterministic
        if random_seed is not None:
            random.seed(random_seed)

        self.initialize_scene()
        while self.queue:
            self.queue_step()
            # self.visualize_points(scene_name)

        self.prune_points()
        #self.visualize_points(scene_name)

    # get rid of unreachable points
    def prune_points(self):
        final_grid_points = set()

        for gp in self.grid_points:
            final_grid_points.add(key_for_point(gp['x'], gp['z']))

        pruned_grid_points = []

        for gp in self.grid_points:
            found = False
            for x in [1, -1]:
                found |= key_for_point(gp['x'] + (self.grid_size * x), gp['z']) in final_grid_points

            for z in [1, -1]:
                found |= key_for_point(
                    gp['x'],
                    (self.grid_size * z) + gp['z']) in final_grid_points

            if found:
                pruned_grid_points.append(gp)

        self.grid_points = pruned_grid_points

    def is_object_visible(self, object_id):
        for obj in self.last_event.metadata['objects']:
            if obj['objectId'] == object_id and obj['visible']:
                return True
        return False

    def find_visible_receptacles(self):
        receptacle_points = []
        receptacle_pivot_points = []

        # pickup all objects
        visibility_object_id = None
        visibility_object_types = ['Mug', 'CellPhone']
        for obj in self.last_event.metadata['objects']:
            if obj['pickupable']:
                self.step(action=dict(
                    action='PickupObject',
                    objectId=obj['objectId'],
                    forceVisible=True))
            if visibility_object_id is None and obj['objectType'] in visibility_object_types:
                visibility_object_id = obj['objectId']

        for point in self.grid_points:
            self.step(dict(
                action='Teleport',
                x=point['x'],
                y=point['y'],
                z=point['z']), raise_for_failure=True)

            for rot, hor in product(self.rotations, self.horizons):
                event = self.step(
                    dict(action='RotateLook', rotation=rot, horizon=hor),
                    raise_for_failure=True)
                for j in event.metadata['objects']:
                    if j['receptacle'] and j['visible']:
                        receptacle_points.append(dict(
                            distance=j['distance'],
                            pivotId=0,
                            receptacleObjectId=j['objectId'],
                            searchNode=dict(
                                horizon=hor,
                                rotation=rot,
                                openReceptacle=False,
                                pivotId=0,
                                receptacleObjectId='',
                                x=point['x'],
                                y=point['y'],
                                z=point['z'])))

                        if j['openable']:
                            self.step(action=dict(
                                action='OpenObject',
                                forceVisible=True,
                                objectId=j['objectId']),
                                      raise_for_failure=True)
                        for pivot_id in range(j['receptacleCount']):
                            self.step(
                                action=dict(
                                    action='Replace',
                                    forceVisible=True,
                                    receptacleObjectId=j['objectId'],
                                    objectId=visibility_object_id,
                                    pivot=pivot_id), raise_for_failure=True)
                            if self.is_object_visible(visibility_object_id):
                                receptacle_pivot_points.append(dict(
                                    distance=j['distance'],
                                    pivotId=pivot_id,
                                    receptacleObjectId=j['objectId'],
                                    searchNode=dict(
                                        horizon=hor,
                                        rotation=rot,
                                        openReceptacle=j['openable'],
                                        pivotId=pivot_id,
                                        receptacleObjectId=j['objectId'],
                                        x=point['x'],
                                        y=point['y'],
                                        z=point['z'])))

                        if j['openable']:
                            self.step(action=dict(
                                action='CloseObject',
                                forceVisible=True,
                                objectId=j['objectId']),
                                      raise_for_failure=True)

        return receptacle_pivot_points, receptacle_points

    def find_visible_objects(self):

        seen_target_objects = defaultdict(list)

        for point in self.grid_points:
            self.step(dict(
                action='Teleport',
                x=point['x'],
                y=point['y'],
                z=point['z']), raise_for_failure=True)

            for rot, hor in product(self.rotations, self.horizons):
                event = self.step(dict(
                    action='RotateLook',
                    rotation=rot,
                    horizon=hor), raise_for_failure=True)

                object_receptacle = dict()
                for obj in event.metadata['objects']:
                    if obj['receptacle']:
                        for pso in obj['pivotSimObjs']:
                            object_receptacle[pso['objectId']] = obj

                for obj in filter(
                        lambda x: x['visible'] and x['pickupable'],
                        event.metadata['objects']):

                    #if obj['objectId'] in object_receptacle and\
                    #        object_receptacle[obj['objectId']]['openable'] and not \
                    #        object_receptacle[obj['objectId']]['isopen']:
                    #    continue

                    seen_target_objects[obj['objectId']].append(dict(
                        distance=obj['distance'],
                        agent=event.metadata['agent']))

        return seen_target_objects

    def initialize_scene(self):
        self.target_objects = []
        self.object_receptacle = defaultdict(
            lambda: dict(objectId='StartupPosition', pivotSimObjs=[]))

        self.open_receptacles = []
        open_pickupable = {}
        pickupable = {}
        is_open = {}

        for obj in filter(lambda x: x['receptacle'], self.last_event.metadata['objects']):
            for oid in obj['receptacleObjectIds']:
                self.object_receptacle[oid] = obj

            is_open[obj['objectId']] = (obj['openable'] and obj['isopen'])

        for obj in filter(lambda x: x['receptacle'], self.last_event.metadata['objects']):
            for oid in obj['receptacleObjectIds']:
                if obj['openable'] or (obj['objectId'] in self.object_receptacle and self.object_receptacle[obj['objectId']]['openable']):

                    open_pickupable[oid] = obj['objectId']
                else:
                    pickupable[oid] = obj['objectId']

        if open_pickupable.keys():
            self.target_objects = random.sample(open_pickupable.keys(), k=1)
            shuffled_keys = list(open_pickupable.keys())
            random.shuffle(shuffled_keys)
            for oid in shuffled_keys:
                position_target = self.object_receptacle[self.target_objects[0]]['position']
                position_candidate = self.object_receptacle[oid]['position']
                dist = math.sqrt(
                    (position_target['x'] - position_candidate['x']) ** 2 +
                    (position_target['y'] - position_candidate['y']) ** 2)
                # try to find something that is far to avoid having the doors collide
                if dist > 1.25:
                    self.target_objects.append(oid)
                    break

        for roid in set(map(lambda x: open_pickupable[x], self.target_objects)):
            if roid in is_open:
                continue
            self.open_receptacles.append(roid)
            self.step(dict(
                action='OpenObject',
                objectId=roid,
                forceVisible=True), raise_for_failure=True)

    def queue_step(self):
        search_point = self.queue.popleft()
        event = self.step(dict(
            action='Teleport',
            x=search_point.start_position['x'],
            y=search_point.start_position['y'],
            z=search_point.start_position['z']))

        assert event.metadata['lastActionSuccess']
        move_vec = search_point.move_vector
        move_vec['moveMagnitude'] = self.grid_size
        event = self.step(dict(action='Move', **move_vec))

        if event.metadata['lastActionSuccess']:
            if event.metadata['agent']['position']['y'] > 1.3:
                #pprint(search_point.start_position)
                #pprint(search_point.move_vector)
                #pprint(event.metadata['agent']['position'])
                raise Exception("**** got big point ")

            self.enqueue_points(event.metadata['agent']['position'])

            if not any(map(lambda p: distance(p, event.metadata['agent']['position']) < self.distance_threshold, self.grid_points)):
                self.grid_points.append(event.metadata['agent']['position'])

        return event


### ExhaustiveBFSController and OfflineController.
import importlib

class ThorAgentState:
    """ Representation of a simple state of a Thor Agent which includes
        the position, horizon and rotation. """
    def __init__(self, x, y, z, rotation, horizon):
        self.x = x
        self.y = y
        self.z = z
        self.rotation = rotation
        self.horizon = horizon

    @classmethod
    def get_state_from_evenet(cls, event):
        """ Extracts a state from an event. """
        return cls(
            x=event.metadata['agent']['position']['x'],
            y=event.metadata['agent']['position']['y'],
            z=event.metadata['agent']['position']['z'],
            rotation=event.metadata['agent']['rotation']['y'],
            horizon=event.metadata['agent']['cameraHorizon']
        )

    def __eq__(self, other):
        """ If we check for exact equality then we get issues.
            For now we consider this 'close enough'. """
        if isinstance(other, ThorAgentState):
            return (
                abs(self.x - other.x) <= 0.00001 and
                abs(self.y - other.y) <= 0.1 and
                abs(self.z - other.z) <= 0.00001 and
                self.rotation == other.rotation and
                abs(self.horizon - other.horizon) <= 1
            )
        return NotImplemented

    def __str__(self):
        """ Get the string representation of a state. """
        return '{:0.2f}|{:0.2f}|{:0.2f}|{:d}|{:d}'.format(
            self.x,
            self.y,
            self.z,
            round(self.rotation),
            round(self.horizon)
        )

    def key(self):
        """ Similar to __str__ but excludes the y value.
            which we cannot predict. """
        return '{:0.2f}|{:0.2f}|{:d}|{:d}'.format(
            self.x,
            self.z,
            round(self.rotation),
            round(self.horizon)
        )

    def position(self):
        """ Returns just the position. """
        return dict(x=self.x, y=self.y, z=self.z)


class ExhaustiveBFSController(Controller):
    """ A much slower and more exhaustive version of the BFSController.
        This may be helpful if you wish to find the shortest path to an object.
        The usual BFSController does not consider things like rotate or look down
        when you are navigating towards an object. Additionally, there is some
        rare occurances of positions which you can only get to in a certain way.
        This ExhaustiveBFSController introduces the safe_teleport method which
        ensures that all states will be covered. 
        Strongly recomend having a seperate directory for each scene. See 
        OfflineController for more information on how the generated data may be used. """
    def __init__(
            self,
            grid_size=0.25,
            grid_file='grid.json',
            graph_file='graph.json',
            metadata_file='metadata.json',
            images_file='images.hdf5',
            debug_mode=True,
            grid_assumption=False,
            actions=[
                'MoveAhead',
                'RotateLeft',
                'RotateRight',
                'LookUp',
                'LookDown'
            ]):

        super(ExhaustiveBFSController, self).__init__()
        # Allowed rotations.
        self.rotations = [0, 90, 180, 270]
        # Allowed horizons.
        self.horizons = [-30, 0, 30]

        self.allow_enqueue = True
        self.queue = deque()
        self.seen_points = []
        self.grid_points = []
        self.seen_states = []
        self.visited_seen_states = []
        self.grid_states = []
        self.grid_size = grid_size
        self._check_visited = False

        # distance_threshold to be consistent with BFSController in generating grid.
        self.distance_threshold = self.grid_size / 5.0
        self.debug_mode = debug_mode
        self.actions = actions
        self.grid_assumption = grid_assumption

        self.grid_file = grid_file
        self.metadata_file = metadata_file
        self.graph_file = graph_file
        self.images_file = images_file

        # Optionally make a gird (including x,y,z points that are reachable)
        self.make_grid = (grid_file is not None)

        # Optionally store the metadata of each state.
        self.make_metadata = (metadata_file is not None)

        # Optionally make a directed of (s,t) where exists a in self.actions
        # such that t is reachable via s via a.
        self.make_graph = (graph_file is not None)

        # Optionally store an hdf5 file which contains the frame for each state.
        self.make_images = (images_file is not None)

        self.metadata = {}

        self.graph = None
        if self.make_graph:
            import networkx as nx
            self.graph = nx.DiGraph()

        if self.make_images:
            import h5py
            self.images = h5py.File(self.images_file, 'w')

    def safe_teleport(self, state):
        """ Approach a state from all possible directions if the usual teleport fails. """
        event = self.step(dict(action='Teleport', x=state.x, y=state.y, z=state.z))
        if event.metadata['lastActionSuccess']:
            return event

        # Approach from the left.
        event = self.step(dict(action='Teleport', x=(state.x-self.grid_size), y=state.y, z=state.z))
        if event.metadata['lastActionSuccess']:
            self.step(dict(action='Rotate', rotation=90))
            event = self.step(dict(action='MoveAhead'))
            if event.metadata['lastActionSuccess']:
                return event

        # Approach from the right.
        event = self.step(dict(action='Teleport', x=(state.x+self.grid_size), y=state.y, z=state.z))
        if event.metadata['lastActionSuccess']:
            self.step(dict(action='Rotate', rotation=270))
            event = self.step(dict(action='MoveAhead'))
            if event.metadata['lastActionSuccess']:
                return event

        # Approach from the back.
        event = self.step(dict(action='Teleport', x=state.x, y=state.y, z=state.z-self.grid_size))
        if event.metadata['lastActionSuccess']:
            self.step(dict(action='Rotate', rotation=0))
            event = self.step(dict(action='MoveAhead'))
            if event.metadata['lastActionSuccess']:
                return event

        # Approach from the front.
        event = self.step(dict(action='Teleport', x=state.x, y=state.y, z=state.z+self.grid_size))
        if event.metadata['lastActionSuccess']:
            self.step(dict(action='Rotate', rotation=180))
            event = self.step(dict(action='MoveAhead'))
            if event.metadata['lastActionSuccess']:
                return event

        raise Exception('Safe Teleport Failed')


    def teleport_to_state(self, state):
        """ Only use this method when we know the state is valid. """
        event = self.safe_teleport(state)
        assert event.metadata['lastActionSuccess']
        event = self.step(dict(action='Rotate', rotation=state.rotation))
        assert event.metadata['lastActionSuccess']
        event = self.step(dict(action="Look", horizon=state.horizon))
        assert event.metadata['lastActionSuccess']

        if self.debug_mode:
            # Sanity check that we have teleported to the correct state.
            new_state = self.get_state_from_event(event)
            assert state == new_state
        return event

    def get_state_from_event(self, event):
        return ThorAgentState.get_state_from_evenet(event)

    def get_point_from_event(self, event):
        return event.metadata['agent']['position']

    def get_next_state(self, state, action, copy_state=False):
        """ Guess the next state when action is taken. Note that
            this will not predict the correct y value. """
        if copy_state:
            next_state = copy.deepcopy(state)
        else:
            next_state = state
        if action == 'MoveAhead':
            if next_state.rotation == 0:
                next_state.z += self.grid_size
            elif next_state.rotation == 90:
                next_state.x += self.grid_size
            elif next_state.rotation == 180:
                next_state.z -= self.grid_size
            elif next_state.rotation == 270:
                next_state.x -= self.grid_size
            else:
                raise Exception('Unknown Rotation')
        elif action == 'RotateRight':
            next_state.rotation = (next_state.rotation + 90) % 360
        elif action == 'RotateLeft':
            next_state.rotation = (next_state.rotation - 90) % 360
        elif action == 'LookUp':
            if abs(next_state.horizon + 30) <= 1:
                return None
            next_state.horizon = next_state.horizon - 30
        elif action == 'LookDown':
            if abs(next_state.horizon - 60) <= 1:
                return None
            next_state.horizon = next_state.horizon + 30
        return next_state

    def add_edge(self, curr_state, next_state):
        self.graph.add_edge(str(curr_state), str(next_state))

    def enqueue_state(self, state):
        """ Returns true if state is valid. """
        # ensure there are no dup states.
        if state in self.seen_states:
            return True
        
        # ensure state is a legal rotation and horizon.
        if round(state.horizon) not in self.horizons or round(state.rotation) not in self.rotations:
            return False

        self.seen_states.append(state)
        self.queue.append(state)
        return True

    def enqueue_states(self, agent_state):

        if not self.allow_enqueue:
            return

        # Take all action in self.action and enqueue if they are valid.
        for action in self.actions:

            # Grid assumption is meant to make things faster and should not
            # be used in practice. In general it does not work when the y
            # values fluctuate in a scene. It circumvents using the actual controller.
            if self.grid_assumption:
                next_state_guess = self.get_next_state(agent_state, action, True)
                if next_state_guess is None:
                    continue
                if next_state_guess in self.seen_states:
                    if self.make_graph:
                        self.add_edge(agent_state, next_state_guess)
                    continue


            event = self.step(dict(action=action))
            if event.metadata['lastActionSuccess']:
                next_state = self.get_state_from_event(event)

                if self.grid_assumption and self.debug_mode:
                    if next_state != next_state_guess:
                        print(next_state)
                        print(next_state_guess)
                    assert next_state == next_state_guess
                
                if self.enqueue_state(next_state) and self.make_graph:
                    self.add_edge(agent_state, next_state)

            # Return back to agents initial location.
            self.teleport_to_state(agent_state)


    def search_all_closed(self, scene_name):
        """ Runs the ExhaustiveBFSController on scene_name. """
        self.allow_enqueue = True
        self.queue = deque()
        self.seen_points = []
        self.visited_seen_points = []
        self.grid_points = []
        self.seen_states = []
        self.visited_seen_states = []
        event = self.reset(scene_name)

        event = self.step(dict(action='Initialize', gridSize=self.grid_size))

        self.enqueue_state(
            self.get_state_from_event(event)
        )

        while self.queue:
            self.queue_step()

        if self.make_grid:
            with open(self.grid_file, 'w') as outfile:
                json.dump(self.grid_points, outfile)
        if self.make_graph:
            from networkx.readwrite import json_graph
            with open(self.graph_file, 'w') as outfile:
                data = json_graph.node_link_data(self.graph)
                json.dump(data, outfile)
        if self.make_metadata:
            with open(self.metadata_file, 'w') as outfile:
                json.dump(self.metadata, outfile)
        if self.make_images:
            self.images.close()

    def queue_step(self):

        search_state = self.queue.popleft()
        event = self.teleport_to_state(search_state)

        if search_state.y > 1.3:
            raise Exception("**** got big point ")

        self.enqueue_states(search_state)
        self.visited_seen_states.append(search_state)

        if self.make_grid and not any(map(lambda p: distance(p, search_state.position()) < self.distance_threshold, self.grid_points)):
            self.grid_points.append(search_state.position())

        if self.make_metadata:
            self.metadata[str(search_state)] = event.metadata

        if self.make_images:
            self.images.create_dataset(str(search_state), data=event.frame)


class OfflineControllerEvent:
    """ A stripped down version of an event. Only contains lastActionSuccess, sceneName,
        and optionally state and frame. Does not contain the rest of the metadata. """
    def __init__(self, last_action_success, scene_name, state=None, frame=None):
        self.metadata = {
            'lastActionSuccess': last_action_success,
            'sceneName': scene_name
        }
        if state is not None:
            self.metadata['agent'] = {}
            self.metadata['agent']['position'] = state.position()
            self.metadata['agent']['rotation'] = {'x': 0., 'y': state.rotation, 'z':  0.}
            self.metadata['agent']['cameraHorizon'] = state.horizon
        self.frame = frame

class OfflineController:
    """ A stripped down version of the controller for non-interactive settings.
        Only allows for a few given actions. Note that you must use the
        ExhaustiveBFSController to first generate the data used by OfflineController.
        Data is stored in offline_data_dir/<scene_name>/.

        Can swap the metadata.json for a visible_object_map.json. A script for generating
        this is coming soon. If the swap is made then the OfflineController is faster and
        self.using_raw_metadata will be set to false.

        Additionally, images.hdf5 may be swapped out with ResNet features or anything
        that you want to be returned for event.frame. """
        
    def __init__(
            self,
            grid_size=0.25,
            offline_data_dir='./resources/offline_data/',
            grid_file_name='grid.json',
            graph_file_name='graph.json',
            metadata_file_name='visible_object_map.json',
            # metadata_file_name='metadata.json',
            images_file_name='images.hdf5',
            debug_mode=True,
            actions=[
                'MoveAhead',
                'RotateLeft',
                'RotateRight',
                'LookUp',
                'LookDown'
            ],
            visualize=True):

        super(OfflineController, self).__init__()
        self.grid_size = grid_size
        self.offline_data_dir = offline_data_dir
        self.grid_file_name = grid_file_name
        self.graph_file_name = graph_file_name
        self.metadata_file_name = metadata_file_name
        self.images_file_name = images_file_name
        self.grid = None
        self.graph = None
        self.metadata = None
        self.images = None
        self.controller = None
        self.using_raw_metadata = True
        self.actions = actions
        self.rotations = [0, 90, 180, 270]
        self.horizons = [-30, 0, 30]
        self.debug_mode = debug_mode

        self.last_event = None

        self.controller = ExhaustiveBFSController()
        self.visualize = visualize

        self.scene_name = None
        self.state = None
        self.last_action_success = True

        self.h5py = importlib.import_module('h5py')
        self.nx = importlib.import_module('networkx')
        self.json_graph_loader = importlib.import_module('networkx.readwrite')

    def start(self):
        if self.visualize:
            self.controller.start()
            self.controller.step(dict(action='Initialize', gridSize=self.grid_size))

    def get_state(self, x, y, z, rotation=0., horizon=0.):
        return ThorAgentState(x, y, z, rotation, horizon)

    def reset(self, scene_name=None):

        if scene_name is None:
            scene_name = 'FloorPlan28'

        if scene_name != self.scene_name:
            self.scene_name = scene_name
            with open(os.path.join(self.offline_data_dir, self.scene_name, self.grid_file_name), 'r') as f:
                self.grid = json.load(f)
            with open(os.path.join(self.offline_data_dir, self.scene_name, self.graph_file_name), 'r') as f:
                graph_json = json.load(f)
            self.graph = self.json_graph_loader.node_link_graph(graph_json).to_directed()
            with open(os.path.join(self.offline_data_dir, self.scene_name, self.metadata_file_name), 'r') as f:
                self.metadata = json.load(f)
                # Determine if using the raw metadata, which is structured as a dictionary of
                # state -> metatdata. The alternative is a map of obj -> states where object is visible.
                key = next(iter(self.metadata.keys()))
                try:
                    float(key.split('|')[0])
                    self.using_raw_metadata = True
                except ValueError:
                    self.using_raw_metadata = False

            if self.images is not None:
                self.images.close()
            self.images = self.h5py.File(os.path.join(
                self.offline_data_dir, self.scene_name, self.images_file_name), 'r')

        self.state = self.get_state(**self.grid[0], rotation=random.choice(self.rotations))
        self.last_action_success = True
        self.last_event = self._successful_event()

        if self.visualize:
            self.controller.reset(scene_name)
            self.controller.teleport_to_state(self.state)

    def randomize_state(self):

        self.state = self.get_state(**random.choice(self.grid), rotation=random.choice(self.rotations))
        self.last_action_success = True
        self.last_event = self._successful_event()
        
        if self.visualize:
            self.controller.teleport_to_state(self.state)

    def step(self, action, raise_for_failure=False):
        
        if 'action' not in action or action['action'] not in self.actions:
            if action['action'] == 'Initialize':
                return
            raise Exception('Unsupported action.')

        if self.visualize:
            viz_event = self.controller.step(action, raise_for_failure)
            viz_next_state = self.controller.get_state_from_event(viz_event)
            if (round(viz_next_state.horizon) not in self.horizons or
                round(viz_next_state.rotation) not in self.rotations):
                # return back to original state.
                self.controller.teleport_to_state(self.state)

        action = action["action"]

        next_state = self.controller.get_next_state(self.state, action, True)
        if next_state is not None:
            next_state_key = next_state.key()
            neighbors = self.graph.neighbors(str(self.state))

            for neighbor in neighbors:
                split = neighbor.split('|')
                neighbor_key = '|'.join([split[0]] + split[2:])

                if next_state_key == neighbor_key:
                    self.state = self.get_state(*[float(x) for x in split])
                    self.last_action_success = True
                    event = self._successful_event()
                    if self.debug_mode and self.visualize:
                        if (self.controller.get_state_from_event(viz_event) !=
                                self.controller.get_state_from_event(event)):
                            print(str(self.controller.get_state_from_event(viz_event)))
                            print(str(self.controller.get_state_from_event(event)))

                        assert (self.controller.get_state_from_event(viz_event) ==
                                self.controller.get_state_from_event(event))
                        assert viz_event.metadata['lastActionSuccess']

                        # Uncomment if you want to view the frames side by side to 
                        # ensure that they are duplicated.
                        # from matplotlib import pyplot as plt
                        # fig = plt.figure()
                        # fig.add_subplot(2,1,1)
                        # plt.imshow(self.get_image())
                        # fig.add_subplot(2,1,2)
                        # plt.imshow(viz_event.frame)
                        # plt.show()

                    self.last_event = event
                    return event
                     
        self.last_action_success = False
        self.last_event.metadata['lastActionSuccess'] = False
        return self.last_event


    def shortest_path(self, source_state, target_state):
        return self.nx.shortest_path(self.graph, str(source_state), str(target_state))
    
    def optimal_plan(self, source_state, target_state):
        """ This is for debugging. It modifies the state. """
        path = self.shortest_path(source_state, target_state)

        self.state = source_state
        actions = []
        i = 1
        while i < len(path):
            for a in self.actions:
                next_state = self.controller.get_next_state(self.state, a, True)
                if str(next_state) == path[i]:
                    actions.append(a)
                    i += 1
                    self.state = next_state
                    break
        
        return actions

    def shortest_path_to_target(self, source_state, objId, get_plan=False):
        """ Many ways to reach objId, which one is best? """
        states_where_visible = []
        if self.using_raw_metadata:
            for s in self.metadata:
                objects = self.metadata[s]['objects']
                visible_objects = [o['objectId'] for o in objects if o['visible']]
                if objId in visible_objects:
                    states_where_visible.append(s)
        else:
            states_where_visible = self.metadata[objId]
        
        # transform from strings into states
        states_where_visible = [
            self.get_state(*[float(x) for x in str_.split('|')]) for str_ in states_where_visible
        ]

        best_path = None
        best_path_len = 0

        for t in states_where_visible:
            path = self.shortest_path(source_state, t)
            if len(path) < best_path_len or best_path is None:
                best_path = path
                best_path_len = len(path)

        best_plan = []
        if get_plan:
            best_plan = self.optimal_plan(source_state, t)
       
        return best_path, best_path_len, best_plan

    def visualize_plan(self, source, plan):
        """ Visualize the best path from source to plan. """
        assert self.visualize
        self.controller.teleport_to_state(source)
        time.sleep(0.5)
        for a in plan:
            print(a)
            self.controller.step(dict(action=a))
            time.sleep(0.5)


    def object_is_visible(self, objId):
        if self.using_raw_metadata:
            objects = self.metadata[str(self.state)]['objects']
            visible_objects = [o['objectId'] for o in objects if o['visible']]
            return objId in visible_objects
        else:
            return str(self.state) in self.metadata[objId]

    def _successful_event(self):
        return OfflineControllerEvent(
            self.last_action_success,
            self.scene_name,
            self.state,
            self.get_image(),
        )

    def get_image(self):
        return self.images[str(self.state)][:]

    def all_objects(self):
        if self.using_raw_metadata:
            return [o['objectId'] for o in self.metadata[str(self.state)]['objects']]
        else:
            return self.metadata.keys()
        