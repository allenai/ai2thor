# Copyright Allen Institute for Artificial Intelligence 2017
"""
ai2thor.controller

Primary entrypoint into the Thor API. Provides all the high-level functions
needed to control the in-game agent through ai2thor.server.

"""
import atexit
from collections import deque, defaultdict
from itertools import product
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
import re
import os
import platform
import uuid
import tty
import sys
import termios


import numpy as np
import ai2thor.docker
import ai2thor.wsgi_server
import ai2thor.fifo_server
from ai2thor.interact import InteractiveControllerPrompt, DefaultActions
from ai2thor.server import DepthFormat
from ai2thor.build import COMMIT_ID
import ai2thor.build
from ai2thor._quality_settings import QUALITY_SETTINGS, DEFAULT_QUALITY
from ai2thor.util import makedirs

import warnings

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

def distance(point1, point2):
    x_diff = (point1['x'] - point2['x']) ** 2
    z_diff = (point1['z'] - point2['z']) ** 2
    return math.sqrt(x_diff + z_diff)


def key_for_point(x, z):
    return "%0.1f %0.1f" % (x, z)

class Controller(object):

    def __init__(
            self,
            quality=DEFAULT_QUALITY,
            fullscreen=False,
            headless=False,
            port=0,
            start_unity=True,
            local_executable_path=None,
            local_build=False,
            width=300,
            height=300,
            x_display=None,
            host='127.0.0.1',
            scene='FloorPlan_Train1_1',
            image_dir='.',
            save_image_per_frame=False,
            docker_enabled=False,
            depth_format=DepthFormat.Meters,
            add_depth_noise=False,
            download_only=False,
            include_private_scenes=False,
            server_class=ai2thor.fifo_server.FifoServer,
            **unity_initialization_parameters
    ):
        self.receptacle_nearest_pivot_points = {}
        self.server = None
        self.unity_pid = None
        self.docker_enabled = docker_enabled
        self.container_id = None
        self.width = width
        self.height = height

        self.last_event = None
        self._scenes_in_build = None
        self.killing_unity = False
        self.quality = quality
        self.lock_file = None
        self.fullscreen = fullscreen
        self.headless = headless
        self.depth_format = depth_format
        self.add_depth_noise = add_depth_noise
        self.include_private_scenes = include_private_scenes
        self.server_class = server_class
        self._build = None

        self.interactive_controller = InteractiveControllerPrompt(
            list(DefaultActions),
            has_object_actions=True,
            image_dir=image_dir,
            image_per_frame=save_image_per_frame
        )

        if local_executable_path:
            self._build = ai2thor.build.ExternalBuild(local_executable_path)
        else:
            self._build = self.find_build(local_build)

        if self._build is None:
            raise Exception("Couldn't find a suitable build for platform: %s" % platform.system())

        if download_only:
            self._build.download()
        else:

            self.start(
                port=port,
                start_unity=start_unity,
                width=width,
                height=height,
                x_display=x_display,
                host=host
            )

            self.initialization_parameters = unity_initialization_parameters

            if 'continuous' in self.initialization_parameters:
                warnings.warn(
                    "Warning: 'continuous' is deprecated and will be ignored,"
                    " use 'snapToGrid={}' instead."
                    .format(not self.initialization_parameters['continuous']),
                    DeprecationWarning
                )

            if 'fastActionEmit' in self.initialization_parameters and self.server_class != ai2thor.fifo_server.FifoServer:
                warnings.warn("fastAtionEmit is only available with the FifoServer");

            if 'continuousMode' in self.initialization_parameters:
                warnings.warn(
                    "Warning: 'continuousMode' is deprecated and will be ignored,"
                    " use 'snapToGrid={}' instead."
                        .format(not self.initialization_parameters['continuousMode']),
                    DeprecationWarning
                )

            event = self.reset(scene)
            if event.metadata['lastActionSuccess']:
                init_return = event.metadata['actionReturn']
                self.server.set_init_params(init_return)

                logging.info("Initialize return: {}".format(init_return))
            else:
                raise RuntimeError('Initialize action failure: {}'.format(
                    event.metadata['errorMessage'])
                )

    def _build_server(self, host, port, width, height):

        if self.server is not None:
            return

        if self.server_class == ai2thor.wsgi_server.WsgiServer:
            self.server = ai2thor.wsgi_server.WsgiServer(
                host,
                port=port,
                depth_format=self.depth_format,
                add_depth_noise=self.add_depth_noise,
                width=width,
                height=height
            )
        elif self.server_class == ai2thor.fifo_server.FifoServer:
            self.server = ai2thor.fifo_server.FifoServer(
                depth_format=self.depth_format,
                add_depth_noise=self.add_depth_noise,
                width=width,
                height=height)
        else:
            raise ValueError("Invalid server class: %s" % self.server_class)

    def __enter__(self):
        return self

    def __exit__(self, *args):
        self.stop()

    @property
    def scenes_in_build(self):
        if self._scenes_in_build:
            return self._scenes_in_build

        event = self.step(action='GetScenesInBuild')

        self._scenes_in_build = set(event.metadata['actionReturn'])

        return self._scenes_in_build

    def reset(self, scene='FloorPlan_Train1_1', **init_params):
        if re.match(r'^FloorPlan[0-9]+$', scene):
            scene = scene + "_physics"

        if scene not in self.scenes_in_build:
            def key_sort_func(scene_name):
                m = re.search('FloorPlan[_]?([a-zA-Z\-]*)([0-9]+)_?([0-9]+)?.*$', scene_name)
                last_val = m.group(3) if m.group(3) is not None else -1
                return m.group(1), int(m.group(2)), int(last_val)
            raise ValueError(
                "\nScene '{}' not contained in build (scene names are case sensitive)."
                "\nPlease choose one of the following scene names:\n\n{}".format(
                    scene,
                    ", ".join(sorted(list(self.scenes_in_build), key=key_sort_func))
                )
            )

        self.server.send(dict(action='Reset', sceneName=scene, sequenceId=0))
        self.last_event = self.server.receive()

        # update the initialization parameters
        init_params = init_params.copy()

        # width and height are updates in 'ChangeResolution', not 'Initialize'
        if ('width' in init_params and  init_params['width'] != self.width) or (
            'height' in init_params and init_params['height'] != self.height
        ):
            if 'width' in init_params:
                self.width = init_params['width']
                del init_params['width']
            if 'height' in init_params:
                self.height = init_params['height']
                del init_params['height']
            self.step(action='ChangeResolution', x=self.width, y=self.height)

        # updates the initialization parameters
        self.initialization_parameters.update(init_params)
        self.last_event = self.step(action='Initialize', **self.initialization_parameters)

        return self.last_event

    def random_initialize(
            self,
            random_seed=None,
            randomize_open=False,
            unique_object_types=False,
            exclude_receptacle_object_pairs=[],
            max_num_repeats=1,
            remove_prob=0.5):

        raise Exception("RandomInitialize has been removed.  Use InitialRandomSpawn - https://ai2thor.allenai.org/ithor/documentation/actions/initialization/#object-position-randomization")

    def scene_names(self):
        scenes = []
        for low, high in [(1,31), (201, 231), (301, 331), (401, 431)]:
            for i in range(low, high):
                scenes.append('FloorPlan%s_physics' % i)

        return scenes

    def prune_releases(self):
        current_exec_path = self._build.executable_path
        rdir = self.releases_dir
        makedirs(self.tmp_dir)
        makedirs(self.releases_dir)

        # sort my mtime ascending, keeping the 3 most recent, attempt to prune anything older
        all_dirs = list(filter(os.path.isdir, map(lambda x: os.path.join(rdir, x), os.listdir(rdir))))
        sorted_dirs = sorted(all_dirs, key=lambda x: os.stat(x).st_mtime)[:-3]
        for release in sorted_dirs:
            if current_exec_path.startswith(release):
                continue
            try:
                lf_path = release + ".lock"
                lf = os.open(lf_path, os.O_RDWR | os.O_CREAT)
                # we must try to get a lock here since its possible that a process could still
                # be running with this release
                fcntl.lockf(lf, fcntl.LOCK_EX | fcntl.LOCK_NB)

                tmp_prune_dir = os.path.join(self.tmp_dir, '-'.join([self.build_name(release), str(time.time()), str(random.random()), 'prune']))
                os.rename(release, tmp_prune_dir)
                shutil.rmtree(tmp_prune_dir)

                fcntl.lockf(lf, fcntl.LOCK_UN)
                os.close(lf)
                os.unlink(lf_path)
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

    def interact(self,
                 class_segmentation_frame=False,
                 instance_segmentation_frame=False,
                 depth_frame=False,
                 color_frame=False,
                 metadata=False
                 ):
        self.interactive_controller.interact(
            self,
            class_segmentation_frame,
            instance_segmentation_frame,
            depth_frame,
            color_frame,
            metadata
        )

    def multi_step_physics(self, action, timeStep=0.05, max_steps=20):
        events = []
        self.step(action=dict(action='PausePhysicsAutoSim'), raise_for_failure=True)
        events.append(self.step(action))
        while not self.last_event.metadata['isSceneAtRest']:
            events.append(
                self.step(action=dict(
                    action='AdvancePhysicsStep',
                    timeStep=timeStep), raise_for_failure=True))

            if len(events) == (max_steps - 1):
                events.append(self.step(action=dict(action='UnpausePhysicsAutoSim'), raise_for_failure=True))
                break

        return events

    def step(self, action=None, **action_args):

        if type(action) is dict:
            action = copy.deepcopy(action) # prevent changes from leaking
        else:
            action = dict(action=action)

        raise_for_failure = action_args.pop('raise_for_failure', False)
        action.update(action_args)

        if self.headless:
            action["renderImage"] = False

        # prevent changes to the action from leaking
        action = copy.deepcopy(action)

        # XXX should be able to get rid of this with some sort of deprecation warning
        if 'AI2THOR_VISIBILITY_DISTANCE' in os.environ:
            action['visibilityDistance'] = float(os.environ['AI2THOR_VISIBILITY_DISTANCE'])

        should_fail = False
        self.last_action = action

        if ('objectId' in action and (action['action'] == 'OpenObject' or action['action'] == 'CloseObject')):

            force_visible = action.get('forceVisible', False)
            agent_id = action.get('agentId', 0)
            instance_detections2D = self.last_event.events[agent_id].instance_detections2D
            if not force_visible and instance_detections2D and action['objectId'] not in instance_detections2D:
                should_fail = True

            obj_metadata = self.last_event.events[agent_id].get_object(action['objectId'])
            if obj_metadata is None or obj_metadata['isOpen'] == (action['action'] == 'OpenObject'):
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


        self.server.send(action)
        self.last_event = self.server.receive()

        if not self.last_event.metadata['lastActionSuccess'] and self.last_event.metadata['errorCode'] in ['InvalidAction', 'MissingArguments', 'AmbiguousAction']:
            raise ValueError(self.last_event.metadata['errorMessage'])

        if raise_for_failure:
            assert self.last_event.metadata['lastActionSuccess']

        return self.last_event

    def unity_command(self, width, height, headless):
        command = self._build.executable_path
        if headless:
            command += " -batchmode"
        else:
            fullscreen = 1 if self.fullscreen else 0
            if QUALITY_SETTINGS[self.quality] == 0:
                raise RuntimeError("Quality {} is associated with an index of 0. "
                                   "Due to a bug in unity, this quality setting would be ignored.".format(self.quality))
            command += " -screen-fullscreen %s -screen-quality %s -screen-width %s -screen-height %s" % (fullscreen, QUALITY_SETTINGS[self.quality], width, height)
        return shlex.split(command)

    def _start_unity_thread(self, env, width, height, server_params, image_name):
        # get environment variables

        env['AI2THOR_CLIENT_TOKEN'] = self.server.client_token = str(uuid.uuid4())
        env['AI2THOR_SERVER_SIDE_SCREENSHOT'] = 'False' if self.headless else 'True'
        for k,v in server_params.items():
            env['AI2THOR_' + k.upper()] = v

        # print("Viewer: http://%s:%s/viewer" % (host, port))
        command = self.unity_command(width, height, headless=self.headless)

        if image_name is not None:
            self.container_id = ai2thor.docker.run(image_name, self.base_dir(), ' '.join(command), env)
            atexit.register(lambda: ai2thor.docker.kill_container(self.container_id))
        else:
            self.server.unity_proc = proc = subprocess.Popen(command, env=env)
            self.unity_pid = proc.pid
            atexit.register(lambda: proc.poll() is None and proc.kill())

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

    @property
    def tmp_dir(self):
        return os.path.join(self.base_dir, 'tmp')

    @property
    def releases_dir(self):
        return os.path.join(self.base_dir, 'releases')

    @property
    def base_dir(self):
        return os.path.join(os.path.expanduser('~'), '.ai2thor')

    def find_build(self, local_build):
        from ai2thor.build import arch_platform_map
        import ai2thor.build
        arch = arch_platform_map[platform.system()]

        if COMMIT_ID:
            ver_build = ai2thor.build.Build(arch, COMMIT_ID, self.include_private_scenes, self.releases_dir)
            return ver_build
        else:
            git_dir = os.path.normpath(os.path.dirname(os.path.realpath(__file__)) + "/../.git")
            commits = subprocess.check_output('git --git-dir=' + git_dir + ' log -n 10 --format=%H', shell=True).decode('ascii').strip().split("\n")

            rdir = self.releases_dir
            found_build = None

            if local_build:
                rdir = os.path.normpath(os.path.dirname(os.path.realpath(__file__)) + "/../unity/builds")
                commits = ['local'] + commits # we add the commits to the list to allow the ci_build to succeed

            for commit_id in commits:
                commit_build = ai2thor.build.Build(arch, commit_id, self.include_private_scenes, rdir)

                try:
                    if os.path.isfile(commit_build.executable_path) or (not local_build and commit_build.exists()):
                        found_build = commit_build
                        break
                except Exception:
                    pass

            # print("Got build for %s: " % (url))

            return found_build

    def start(
            self,
            port=0,
            start_unity=True,
            width=300,
            height=300,
            x_display=None,
            host='127.0.0.1',
            player_screen_width=None,
            player_screen_height=None
    ):
        self._build_server(host, port, width, height)

        if 'AI2THOR_VISIBILITY_DISTANCE' in os.environ:
            import warnings
            warnings.warn("AI2THOR_VISIBILITY_DISTANCE environment variable is deprecated, use \
                the parameter visibilityDistance parameter with the Initialize action instead")

        if player_screen_width is not None:
            warnings.warn("'player_screen_width' parameter is deprecated, use the 'width'"
                          " parameter instead.")
            width = player_screen_width

        if player_screen_height is not None:
            warnings.warn("'player_screen_height' parameter is deprecated, use the 'height'"
                          " parameter instead.")
            height = player_screen_height

        if height <= 0 or width <= 0:
            raise Exception("Screen resolution must be > 0x0")

        if self.server.started:
            import warnings
            warnings.warn('start method depreciated. The server started when the Controller was initialized.')

            # Stops the current server and creates a new one. This is done so
            # that the arguments passed in will be used on the server.
            self.stop()

        env = os.environ.copy()

        image_name = None

        if self.docker_enabled:
            self.check_docker()
            host = ai2thor.docker.bridge_gateway()


        self.server.start()

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

            self._build.download()
            self._build.lock_sh()
            self.prune_releases()

            unity_params = self.server.unity_params()
            self._start_unity_thread(env, width, height, unity_params, image_name)

        # receive the first request
        self.last_event = self.server.receive()

        if height < 300 or width < 300:
            self.last_event = self.step('ChangeResolution', x=width, y=height)

        return self.last_event

    def stop(self):
        self.stop_unity()
        self.server.stop()
        self.stop_container()
        self._build.unlock()

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
            #self.visualize_points(scene_name)

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

            is_open[obj['objectId']] = (obj['openable'] and obj['isOpen'])

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

        if event.metadata['lastActionSuccess']:
            move_vec = search_point.move_vector
            move_vec['moveMagnitude'] = self.grid_size
            event = self.step(dict(action='Move', **move_vec))
            if event.metadata['agent']['position']['y'] > 1.3:
                #pprint(search_point.start_position)
                #pprint(search_point.move_vector)
                #pprint(event.metadata['agent']['position'])
                raise Exception("**** got big point ")

            self.enqueue_points(event.metadata['agent']['position'])

            if not any(map(lambda p: distance(p, event.metadata['agent']['position']) < self.distance_threshold, self.grid_points)):
                self.grid_points.append(event.metadata['agent']['position'])

        return event

