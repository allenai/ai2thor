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
from functools import lru_cache


import numpy as np
import ai2thor.wsgi_server
import ai2thor.fifo_server
from ai2thor.exceptions import UnityCrashException, RestartError
from ai2thor.interact import InteractiveControllerPrompt, DefaultActions
from ai2thor.server import DepthFormat
import ai2thor.build
from ai2thor._quality_settings import QUALITY_SETTINGS, DEFAULT_QUALITY
from ai2thor.util import makedirs, atomic_write
from ai2thor.util.lock import LockEx

import warnings

logger = logging.getLogger(__name__)

RECEPTACLE_OBJECTS = {
    "Box": {
        "Candle",
        "CellPhone",
        "Cloth",
        "CreditCard",
        "Dirt",
        "KeyChain",
        "Newspaper",
        "ScrubBrush",
        "SoapBar",
        "SoapBottle",
        "ToiletPaper",
    },
    "Cabinet": {
        "Bowl",
        "BowlDirty",
        "Box",
        "Bread",
        "BreadSliced",
        "ButterKnife",
        "Candle",
        "CellPhone",
        "Cloth",
        "CoffeeMachine",
        "Container",
        "ContainerFull",
        "CreditCard",
        "Cup",
        "Fork",
        "KeyChain",
        "Knife",
        "Laptop",
        "Mug",
        "Newspaper",
        "Pan",
        "Plate",
        "Plunger",
        "Pot",
        "Potato",
        "Sandwich",
        "ScrubBrush",
        "SoapBar",
        "SoapBottle",
        "Spoon",
        "SprayBottle",
        "Statue",
        "TissueBox",
        "Toaster",
        "ToiletPaper",
        "WateringCan",
    },
    "CoffeeMachine": {"MugFilled", "Mug"},
    "CounterTop": {
        "Apple",
        "AppleSlice",
        "Bowl",
        "BowlDirty",
        "BowlFilled",
        "Box",
        "Bread",
        "BreadSliced",
        "ButterKnife",
        "Candle",
        "CellPhone",
        "CoffeeMachine",
        "Container",
        "ContainerFull",
        "CreditCard",
        "Cup",
        "Egg",
        "EggFried",
        "EggShell",
        "Fork",
        "HousePlant",
        "KeyChain",
        "Knife",
        "Laptop",
        "Lettuce",
        "LettuceSliced",
        "Microwave",
        "Mug",
        "MugFilled",
        "Newspaper",
        "Omelette",
        "Pan",
        "Plate",
        "Plunger",
        "Pot",
        "Potato",
        "PotatoSliced",
        "RemoteControl",
        "Sandwich",
        "ScrubBrush",
        "SoapBar",
        "SoapBottle",
        "Spoon",
        "SprayBottle",
        "Statue",
        "Television",
        "TissueBox",
        "Toaster",
        "ToiletPaper",
        "Tomato",
        "TomatoSliced",
        "WateringCan",
    },
    "Fridge": {
        "Apple",
        "AppleSlice",
        "Bowl",
        "BowlDirty",
        "BowlFilled",
        "Bread",
        "BreadSliced",
        "Container",
        "ContainerFull",
        "Cup",
        "Egg",
        "EggFried",
        "EggShell",
        "Lettuce",
        "LettuceSliced",
        "Mug",
        "MugFilled",
        "Omelette",
        "Pan",
        "Plate",
        "Pot",
        "Potato",
        "PotatoSliced",
        "Sandwich",
        "Tomato",
        "TomatoSliced",
    },
    "GarbageCan": {
        "Apple",
        "AppleSlice",
        "Box",
        "Bread",
        "BreadSliced",
        "Candle",
        "CellPhone",
        "CreditCard",
        "Egg",
        "EggFried",
        "EggShell",
        "LettuceSliced",
        "Newspaper",
        "Omelette",
        "Plunger",
        "Potato",
        "PotatoSliced",
        "Sandwich",
        "ScrubBrush",
        "SoapBar",
        "SoapBottle",
        "SprayBottle",
        "Statue",
        "ToiletPaper",
        "Tomato",
        "TomatoSliced",
    },
    "Microwave": {
        "Bowl",
        "BowlDirty",
        "BowlFilled",
        "Bread",
        "BreadSliced",
        "Container",
        "ContainerFull",
        "Cup",
        "Egg",
        "EggFried",
        "Mug",
        "MugFilled",
        "Omelette",
        "Plate",
        "Potato",
        "PotatoSliced",
        "Sandwich",
    },
    "PaintingHanger": {"Painting"},
    "Pan": {
        "Apple",
        "AppleSlice",
        "EggFried",
        "Lettuce",
        "LettuceSliced",
        "Omelette",
        "Potato",
        "PotatoSliced",
        "Tomato",
        "TomatoSliced",
    },
    "Pot": {
        "Apple",
        "AppleSlice",
        "EggFried",
        "Lettuce",
        "LettuceSliced",
        "Omelette",
        "Potato",
        "PotatoSliced",
        "Tomato",
        "TomatoSliced",
    },
    "Sink": {
        "Apple",
        "AppleSlice",
        "Bowl",
        "BowlDirty",
        "BowlFilled",
        "ButterKnife",
        "Container",
        "ContainerFull",
        "Cup",
        "Egg",
        "EggFried",
        "EggShell",
        "Fork",
        "Knife",
        "Lettuce",
        "LettuceSliced",
        "Mug",
        "MugFilled",
        "Omelette",
        "Pan",
        "Plate",
        "Pot",
        "Potato",
        "PotatoSliced",
        "Sandwich",
        "ScrubBrush",
        "SoapBottle",
        "Spoon",
        "Tomato",
        "TomatoSliced",
        "WateringCan",
    },
    "StoveBurner": {"Omelette", "Pot", "Pan", "EggFried"},
    "TableTop": {
        "Apple",
        "AppleSlice",
        "Bowl",
        "BowlDirty",
        "BowlFilled",
        "Box",
        "Bread",
        "BreadSliced",
        "ButterKnife",
        "Candle",
        "CellPhone",
        "CoffeeMachine",
        "Container",
        "ContainerFull",
        "CreditCard",
        "Cup",
        "Egg",
        "EggFried",
        "EggShell",
        "Fork",
        "HousePlant",
        "KeyChain",
        "Knife",
        "Laptop",
        "Lettuce",
        "LettuceSliced",
        "Microwave",
        "Mug",
        "MugFilled",
        "Newspaper",
        "Omelette",
        "Pan",
        "Plate",
        "Plunger",
        "Pot",
        "Potato",
        "PotatoSliced",
        "RemoteControl",
        "Sandwich",
        "ScrubBrush",
        "SoapBar",
        "SoapBottle",
        "Spoon",
        "SprayBottle",
        "Statue",
        "Television",
        "TissueBox",
        "Toaster",
        "ToiletPaper",
        "Tomato",
        "TomatoSliced",
        "WateringCan",
    },
    "ToiletPaperHanger": {"ToiletPaper"},
    "TowelHolder": {"Cloth"},
}


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
    x_diff = (point1["x"] - point2["x"]) ** 2
    z_diff = (point1["z"] - point2["z"]) ** 2
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
        commit_id=ai2thor.build.COMMIT_ID,
        branch=None,
        width=300,
        height=300,
        x_display=None,
        host="127.0.0.1",
        scene=None,
        image_dir=".",
        save_image_per_frame=False,
        depth_format=DepthFormat.Meters,
        add_depth_noise=False,
        download_only=False,
        include_private_scenes=False,
        server_class=None,
        **unity_initialization_parameters,
    ):
        self.receptacle_nearest_pivot_points = {}
        self.server = None
        self.unity_pid = None
        self.container_id = None
        self.width = width
        self.height = height
        self.x_display = x_display

        self.last_event = None
        self.scene = None
        self._scenes_in_build = None
        self.killing_unity = False
        self.quality = quality
        self.lock_file = None
        self.fullscreen = fullscreen
        self.headless = headless
        self.depth_format = depth_format
        self.add_depth_noise = add_depth_noise
        self.include_private_scenes = include_private_scenes

        if server_class is None and platform.system() == "Windows":
            self.server_class = ai2thor.wsgi_server.WsgiServer
        elif (
            isinstance(server_class, ai2thor.fifo_server.FifoServer)
            and platform.system() == "Windows"
        ):
            raise ValueError("server_class=FifoServer cannot be used on Windows.")
        elif server_class is None:
            self.server_class = ai2thor.fifo_server.FifoServer
        else:
            self.server_class = server_class

        self._build = None

        self.interactive_controller = InteractiveControllerPrompt(
            list(DefaultActions),
            has_object_actions=True,
            image_dir=image_dir,
            image_per_frame=save_image_per_frame,
        )

        if not start_unity:
            self._build = ai2thor.build.EditorBuild()
        elif local_executable_path:
            self._build = ai2thor.build.ExternalBuild(local_executable_path)
        else:
            self._build = self.find_build(local_build, commit_id, branch)

        if self._build is None:
            raise Exception(
                "Couldn't find a suitable build for platform: %s" % platform.system()
            )

        self._build.download()

        if not download_only:
            self.start(
                port=port,
                start_unity=start_unity,
                width=width,
                height=height,
                x_display=x_display,
                host=host,
            )

            self.initialization_parameters = unity_initialization_parameters

            if "continuous" in self.initialization_parameters:
                warnings.warn(
                    "Warning: 'continuous' is deprecated and will be ignored,"
                    " use 'snapToGrid={}' instead.".format(
                        not self.initialization_parameters["continuous"]
                    ),
                    DeprecationWarning,
                )

            if (
                "fastActionEmit" in self.initialization_parameters
                and self.server_class != ai2thor.fifo_server.FifoServer
            ):
                warnings.warn("fastAtionEmit is only available with the FifoServer")

            if "continuousMode" in self.initialization_parameters:
                warnings.warn(
                    "Warning: 'continuousMode' is deprecated and will be ignored,"
                    " use 'snapToGrid={}' instead.".format(
                        not self.initialization_parameters["continuousMode"]
                    ),
                    DeprecationWarning,
                )

            # Let's set the scene for them!
            if scene is None:
                scenes_in_build = self.scenes_in_build
                if not scenes_in_build:
                    raise RuntimeError("No scenes are in your build of AI2-THOR!")

                # use a robothor scene
                robothor_scenes = set(self.robothor_scenes())

                # prioritize robothor if locobot is being used
                robothor_scenes_in_build = robothor_scenes.intersection(scenes_in_build)

                # check for bot as well, for backwards compatibility support
                if (
                    unity_initialization_parameters.get("agentMode", "default").lower()
                    in {"locobot", "bot"}
                    and robothor_scenes_in_build
                ):
                    # get the first robothor scene
                    scene = sorted(list(robothor_scenes_in_build))[0]
                else:
                    ithor_scenes = set(self.ithor_scenes())
                    ithor_scenes_in_build = ithor_scenes.intersection(scenes_in_build)
                    if ithor_scenes_in_build:
                        # prioritize iTHOR because that's what the default agent best uses
                        scene = sorted(list(ithor_scenes_in_build))[0]
                    else:
                        # perhaps only using RoboTHOR or using only custom scenes
                        scene = sorted(list(scenes_in_build))[0]

            event = self.reset(scene)

            # older builds don't send actionReturn on Initialize
            init_return = event.metadata["actionReturn"]
            if init_return:
                self.server.set_init_params(init_return)
                logging.info("Initialize return: {}".format(init_return))

    def _build_server(self, host, port, width, height):

        if self.server is not None:
            return

        if self.server_class.server_type not in self._build.server_types:
            warnings.warn(
                "server_type: %s not available in build: %s, defaulting to WSGI"
                % (self.server_class.server_type, self._build.url)
            )
            self.server_class = ai2thor.wsgi_server.WsgiServer

        if self.server_class == ai2thor.wsgi_server.WsgiServer:
            self.server = ai2thor.wsgi_server.WsgiServer(
                host,
                port=port,
                depth_format=self.depth_format,
                add_depth_noise=self.add_depth_noise,
                width=width,
                height=height,
            )

        elif self.server_class == ai2thor.fifo_server.FifoServer:
            self.server = ai2thor.fifo_server.FifoServer(
                depth_format=self.depth_format,
                add_depth_noise=self.add_depth_noise,
                width=width,
                height=height,
            )

    def __enter__(self):
        return self

    def __exit__(self, *args):
        self.stop()

    @property
    def scenes_in_build(self):
        if self._scenes_in_build is not None:
            return self._scenes_in_build

        try:
            event = self.step(action="GetScenesInBuild")
            self._scenes_in_build = set(event.metadata["actionReturn"])
        except ValueError as e:
            # will happen for old builds without GetScenesInBuild
            self._scenes_in_build = set()

        return self._scenes_in_build

    @staticmethod
    def normalize_scene(scene):

        if re.match(r"^FloorPlan[0-9]+$", scene):
            scene = scene + "_physics"

        return scene

    def reset(self, scene=None, **init_params):
        if scene is None:
            scene = self.scene

        scene = Controller.normalize_scene(scene)

        # scenes in build can be an empty set when GetScenesInBuild doesn't exist as an action
        # for old builds
        if self.scenes_in_build and scene not in self.scenes_in_build:

            def key_sort_func(scene_name):
                m = re.search(
                    r"FloorPlan[_]?([a-zA-Z\-]*)([0-9]+)_?([0-9]+)?.*$", scene_name
                )
                last_val = m.group(3) if m.group(3) is not None else -1
                return m.group(1), int(m.group(2)), int(last_val)

            raise ValueError(
                "\nScene '{}' not contained in build (scene names are case sensitive)."
                "\nPlease choose one of the following scene names:\n\n{}".format(
                    scene,
                    ", ".join(sorted(list(self.scenes_in_build), key=key_sort_func)),
                )
            )

        self.server.send(dict(action="Reset", sceneName=scene, sequenceId=0))
        self.last_event = self.server.receive()

        # update the initialization parameters
        init_params = init_params.copy()

        # width and height are updates in 'ChangeResolution', not 'Initialize'
        if ("width" in init_params and init_params["width"] != self.width) or (
            "height" in init_params and init_params["height"] != self.height
        ):
            if "width" in init_params:
                self.width = init_params["width"]
                del init_params["width"]
            if "height" in init_params:
                self.height = init_params["height"]
                del init_params["height"]
            self.step(
                action="ChangeResolution",
                x=self.width,
                y=self.height,
                raise_for_failure=True,
            )

        # updates the initialization parameters
        self.initialization_parameters.update(init_params)

        # RoboTHOR checks
        agent_mode = self.initialization_parameters.get("agentMode", "default")
        if agent_mode.lower() == "bot":
            self.initialization_parameters["agentMode"] = "locobot"
            warnings.warn(
                "On reset and upon initialization, agentMode='bot' has been renamed to agentMode='locobot'."
            )

        if (
            scene in self.robothor_scenes()
            and self.initialization_parameters.get("agentMode", "default").lower()
            != "locobot"
        ):
            warnings.warn(
                "You are using a RoboTHOR scene without using the standard LoCoBot.\n"
                + "Did you mean to mean to set agentMode='locobot' upon initialization or within controller.reset(...)?"
            )

        self.last_event = self.step(
            action="Initialize",
            raise_for_failure=True,
            **self.initialization_parameters,
        )

        self.scene = scene
        return self.last_event

    def random_initialize(
        self,
        random_seed=None,
        randomize_open=False,
        unique_object_types=False,
        exclude_receptacle_object_pairs=[],
        max_num_repeats=1,
        remove_prob=0.5,
    ):

        raise Exception(
            "RandomInitialize has been removed.  Use InitialRandomSpawn - https://ai2thor.allenai.org/ithor/documentation/actions/initialization/#object-position-randomization"
        )

    @lru_cache()
    def ithor_scenes(
        self,
        include_kitchens=True,
        include_living_rooms=True,
        include_bedrooms=True,
        include_bathrooms=True,
    ):
        types = []
        if include_kitchens:
            types.append((1, 31))
        if include_living_rooms:
            types.append((201, 231))
        if include_bedrooms:
            types.append((301, 331))
        if include_bathrooms:
            types.append((401, 431))

        # keep this as a list because the order may look weird otherwise
        scenes = []
        for low, high in types:
            for i in range(low, high):
                scenes.append("FloorPlan%s_physics" % i)
        return scenes

    @lru_cache()
    def robothor_scenes(self, include_train=True, include_val=True):
        # keep this as a list because the order may look weird otherwise
        scenes = []
        stages = dict()

        # from FloorPlan_Train[1:12]_[1:5]
        if include_train:
            stages["Train"] = range(1, 13)
        if include_val:
            # from FloorPlan_Val[1:12]_[1:5]
            stages["Val"] = range(1, 4)

        for stage, wall_configs in stages.items():
            for wall_config_i in wall_configs:
                for object_config_i in range(1, 6):
                    scenes.append(
                        "FloorPlan_{stage}{wall_config}_{object_config}".format(
                            stage=stage,
                            wall_config=wall_config_i,
                            object_config=object_config_i,
                        )
                    )
        return scenes

    @lru_cache()
    def scene_names(self):
        return self.ithor_scenes() + self.robothor_scenes()

    def _prune_release(self, release):
        try:
            # we must try to get a lock here since its possible that a process could still
            # be running with this release
            lock = LockEx(release, blocking=False)
            lock.lock()
            # its possible that another process could prune
            # out a release when running multiple procs
            # that all race to prune the same release
            if os.path.isdir(release):
                tmp_prune_dir = os.path.join(
                    self.tmp_dir,
                    "-".join(
                        [
                            os.path.basename(release),
                            str(time.time()),
                            str(random.random()),
                            "prune",
                        ]
                    ),
                )
                os.rename(release, tmp_prune_dir)
                shutil.rmtree(tmp_prune_dir)

            lock.unlock()
            lock.unlink()
            return True
        except BlockingIOError:
            return False

    def prune_releases(self):
        current_exec_path = self._build.executable_path
        rdir = self.releases_dir
        makedirs(self.tmp_dir)
        makedirs(self.releases_dir)

        # sort my mtime ascending, keeping the 3 most recent, attempt to prune anything older
        all_dirs = list(
            filter(
                os.path.isdir, map(lambda x: os.path.join(rdir, x), os.listdir(rdir))
            )
        )
        dir_stats = defaultdict(lambda: 0)
        for d in all_dirs:
            try:
                dir_stats[d] = os.stat(d).st_mtime
            # its possible for multiple procs to race to stat/prune
            # creating the possibility that between the listdir/stat the directory was
            # pruned
            except FileNotFoundError:
                pass

        sorted_dirs = sorted(all_dirs, key=lambda x: dir_stats[x])[:-3]
        for release in sorted_dirs:
            if current_exec_path.startswith(release):
                continue
            self._prune_release(release)

    def next_interact_command(self):
        # NOTE: Leave this here because it is incompatible with Windows.
        from ai2thor.interact import get_term_character

        current_buffer = ""
        while True:
            commands = self._interact_commands
            current_buffer += get_term_character()
            if current_buffer == "q" or current_buffer == "\x03":
                break

            if current_buffer in commands:
                yield commands[current_buffer]
                current_buffer = ""
            else:
                match = False
                for k, v in commands.items():
                    if k.startswith(current_buffer):
                        match = True
                        break

                if not match:
                    current_buffer = ""

    def interact(
        self,
        semantic_segmentation_frame=False,
        instance_segmentation_frame=False,
        depth_frame=False,
        color_frame=False,
        metadata=False,
    ):
        self.interactive_controller.interact(
            self,
            semantic_segmentation_frame,
            instance_segmentation_frame,
            depth_frame,
            color_frame,
            metadata,
        )

    def multi_step_physics(self, action, timeStep=0.05, max_steps=20):
        events = []
        self.step(action=dict(action="PausePhysicsAutoSim"), raise_for_failure=True)
        events.append(self.step(action))
        while not self.last_event.metadata["isSceneAtRest"]:
            events.append(
                self.step(
                    action=dict(action="AdvancePhysicsStep", timeStep=timeStep),
                    raise_for_failure=True,
                )
            )

            if len(events) == (max_steps - 1):
                events.append(
                    self.step(
                        action=dict(action="UnpausePhysicsAutoSim"),
                        raise_for_failure=True,
                    )
                )
                break

        return events

    def step(self, action=None, **action_args):

        if type(action) is dict:
            action = copy.deepcopy(action)  # prevent changes from leaking
        else:
            action = dict(action=action)

        raise_for_failure = action_args.pop("raise_for_failure", False)
        action.update(action_args)

        if self.headless:
            action["renderImage"] = False

        # prevent changes to the action from leaking
        action = copy.deepcopy(action)

        # XXX should be able to get rid of this with some sort of deprecation warning
        if "AI2THOR_VISIBILITY_DISTANCE" in os.environ:
            action["visibilityDistance"] = float(
                os.environ["AI2THOR_VISIBILITY_DISTANCE"]
            )

        self.last_action = action

        # dangerously converts rotation(float) to rotation(dict(x=0, y=float, z=0))
        # this should be removed when ServerActions have been removed from Unity
        # for all relevant actions.
        rotation = action.get("rotation")
        if rotation is not None and not isinstance(rotation, dict):
            action["rotation"] = dict(y=rotation)

        # Support for deprecated parameter names (old: new)
        # Note that these parameters used to be applicable to ANY action.
        changed_parameter_names = {
            "renderClassImage": "renderSemanticSegmentation",
            "renderObjectImage": "renderInstanceSegmentation",
        }
        for old, new in changed_parameter_names.items():
            if old in action:
                # warnings.warn(old + " has been renamed to " + new)
                action[new] = action[old]
                # not deleting to allow for older builds to continue to work
                # del action[old]

        self.server.send(action)
        try:
            self.last_event = self.server.receive()
        except UnityCrashException as e:
            self.server.stop()
            self.server = None
            # we don't need to pass port or host, since this Exception
            # is only thrown from the FifoServer, start_unity is also
            # not passed since Unity would have to have been started
            # for this to be thrown
            message = "Restarting unity due to crash: %s" % e
            warnings.warn(message)
            self.start(width=self.width, height=self.height, x_display=self.x_display)
            self.reset()
            raise RestartError(message)

        if not self.last_event.metadata["lastActionSuccess"]:
            if self.last_event.metadata["errorCode"] in [
                "InvalidAction",
                "MissingArguments",
                "AmbiguousAction",
                "InvalidArgument",
            ]:
                raise ValueError(self.last_event.metadata["errorMessage"])
            elif raise_for_failure:
                raise RuntimeError(
                    self.last_event.metadata.get("errorMessage", f"{action} failed")
                )

        return self.last_event

    def unity_command(self, width, height, headless):
        command = self._build.executable_path
        if headless:
            command += " -batchmode -nographics"
        else:
            fullscreen = 1 if self.fullscreen else 0
            if QUALITY_SETTINGS[self.quality] == 0:
                raise RuntimeError(
                    "Quality {} is associated with an index of 0. "
                    "Due to a bug in unity, this quality setting would be ignored.".format(
                        self.quality
                    )
                )
            command += (
                " -screen-fullscreen %s -screen-quality %s -screen-width %s -screen-height %s"
                % (fullscreen, QUALITY_SETTINGS[self.quality], width, height)
            )
        return shlex.split(command)

    def _start_unity_thread(self, env, width, height, server_params, image_name):
        # get environment variables

        env["AI2THOR_CLIENT_TOKEN"] = self.server.client_token = str(uuid.uuid4())
        env["AI2THOR_SERVER_TYPE"] = self.server.server_type
        env["AI2THOR_SERVER_SIDE_SCREENSHOT"] = "False" if self.headless else "True"
        for k, v in server_params.items():
            env["AI2THOR_" + k.upper()] = v

        # print("Viewer: http://%s:%s/viewer" % (host, port))
        command = self.unity_command(width, height, headless=self.headless)
        makedirs(self.log_dir)
        self.server.unity_proc = proc = subprocess.Popen(
            command,
            env=env,
            stdout=open(os.path.join(self.log_dir, "unity.log"), "a"),
            stderr=open(os.path.join(self.log_dir, "unity.log"), "a"),
        )
        self.unity_pid = proc.pid
        atexit.register(lambda: proc.poll() is None and proc.kill())

    def check_x_display(self, x_display):
        if not self.headless:
            with open(os.devnull, "w") as dn:
                # copying the environment so that we pickup
                # XAUTHORITY values
                env = os.environ.copy()
                env["DISPLAY"] = x_display

                if subprocess.call(["which", "xdpyinfo"], stdout=dn) == 0:
                    assert (
                        subprocess.call("xdpyinfo", stdout=dn, env=env, shell=True) == 0
                    ), (
                        "Invalid DISPLAY %s - cannot find X server with xdpyinfo"
                        % x_display
                    )

    @property
    def tmp_dir(self):
        return os.path.join(self.base_dir, "tmp")

    @property
    def releases_dir(self):
        return os.path.join(self.base_dir, "releases")

    @property
    def cache_dir(self):
        return os.path.join(self.base_dir, "cache")

    @property
    def commits_cache_dir(self):
        return os.path.join(self.cache_dir, "commits")

    @property
    def base_dir(self):
        return os.path.join(os.path.expanduser("~"), ".ai2thor")

    @property
    def log_dir(self):
        return os.path.join(self.base_dir, "log")

    def _cache_commit_filename(self, branch):
        encoded_branch = re.sub(r"[^a-zA-Z0-9_\-.]", "_", re.sub("_", "__", branch))
        return os.path.join(self.commits_cache_dir, encoded_branch + ".json")

    def _cache_commit_history(self, branch, payload):
        makedirs(self.commits_cache_dir)
        cache_filename = self._cache_commit_filename(branch)
        atomic_write(cache_filename, json.dumps(payload))

    def _get_cache_commit_history(self, branch):
        cache_filename = self._cache_commit_filename(branch)
        payload = None
        if os.path.exists(cache_filename):
            with open(cache_filename, "r") as f:
                payload = json.loads(f.read())

        return payload

    def _branch_commits(self, branch):
        import requests

        payload = []
        try:
            res = requests.get(
                "https://api.github.com/repos/allenai/ai2thor/commits?sha=%s" % branch
            )
            if res.status_code == 404:
                raise ValueError("Invalid branch name: %s" % branch)
            elif res.status_code == 200:
                payload = res.json()
                self._cache_commit_history(branch, payload)
            else:
                res.raise_for_status()
        except requests.exceptions.ConnectionError as e:
            payload = self._get_cache_commit_history(branch)
            if payload:
                warnings.warn(
                    "Unable to connect to github.com: %s - using cached commit history for %s"
                    % (e, branch)
                )
            else:
                raise Exception(
                    "Unable to get commit history for branch %s and no cached history exists: %s"
                    % (branch, e)
                )

        return [c["sha"] for c in payload]

    def find_build(self, local_build, commit_id, branch):

        if platform.architecture()[0] != "64bit":
            raise Exception("Only 64bit currently supported")

        arch = ai2thor.build.arch_platform_map[platform.system()]

        if branch:
            commits = self._branch_commits(branch)
        elif commit_id:
            ver_build = ai2thor.build.Build(
                arch, commit_id, self.include_private_scenes, self.releases_dir
            )

            if not ver_build.exists():
                raise ValueError(
                    "Invalid commit_id: %s - no build exists for arch=%s"
                    % (commit_id, arch)
                )

            return ver_build
        else:
            git_dir = os.path.normpath(
                os.path.dirname(os.path.realpath(__file__)) + "/../.git"
            )
            commits = (
                subprocess.check_output(
                    "git --git-dir=" + git_dir + " log -n 10 --format=%H", shell=True
                )
                .decode("ascii")
                .strip()
                .split("\n")
            )

        rdir = self.releases_dir
        found_build = None

        if local_build:
            rdir = os.path.normpath(
                os.path.dirname(os.path.realpath(__file__)) + "/../unity/builds"
            )
            commits = [
                "local"
            ] + commits  # we add the commits to the list to allow the ci_build to succeed

        for commit_id in commits:
            commit_build = ai2thor.build.Build(
                arch, commit_id, self.include_private_scenes, rdir
            )

            try:
                if os.path.isdir(commit_build.base_dir) or (
                    not local_build and commit_build.exists()
                ):
                    found_build = commit_build
                    break
            except Exception:
                pass

        if commit_build and commit_build.commit_id != commits[0]:
            warnings.warn(
                "Build for the most recent commit: %s is not available.  Using commit build %s"
                % (commits[0], commit_build.commit_id)
            )

        # print("Got build for %s: " % (found_build.url))
        return found_build

    def start(
        self,
        port=0,
        start_unity=True,
        width=300,
        height=300,
        x_display=None,
        host="127.0.0.1",
        player_screen_width=None,
        player_screen_height=None,
    ):
        self._build_server(host, port, width, height)

        if "AI2THOR_VISIBILITY_DISTANCE" in os.environ:

            warnings.warn(
                "AI2THOR_VISIBILITY_DISTANCE environment variable is deprecated, use \
                the parameter visibilityDistance parameter with the Initialize action instead"
            )

        if player_screen_width is not None:
            warnings.warn(
                "'player_screen_width' parameter is deprecated, use the 'width'"
                " parameter instead."
            )
            width = player_screen_width

        if player_screen_height is not None:
            warnings.warn(
                "'player_screen_height' parameter is deprecated, use the 'height'"
                " parameter instead."
            )
            height = player_screen_height

        if height <= 0 or width <= 0:
            raise Exception("Screen resolution must be > 0x0")

        if self.server.started:

            warnings.warn(
                "start method depreciated. The server started when the Controller was initialized."
            )

            # Stops the current server and creates a new one. This is done so
            # that the arguments passed in will be used on the server.
            self.stop()

        env = os.environ.copy()

        image_name = None

        self.server.start()
        if platform.system() == "Linux":
            if x_display:
                env["DISPLAY"] = ":" + x_display
            elif "DISPLAY" not in env:
                env["DISPLAY"] = ":0.0"

            self.check_x_display(env["DISPLAY"])

        if start_unity:

            self._build.lock_sh()
            self.prune_releases()

            unity_params = self.server.unity_params()
            self._start_unity_thread(env, width, height, unity_params, image_name)

        # receive the first request
        self.last_event = self.server.receive()

        if height < 300 or width < 300:
            self.last_event = self.step("ChangeResolution", x=width, y=height)

        return self.last_event

    def stop(self):
        self.stop_unity()
        self.server.stop()
        self._build.unlock()

    def stop_unity(self):
        if self.unity_pid and process_alive(self.unity_pid):
            self.killing_unity = True
            proc = self.server.unity_proc
            for i in range(4):
                if not process_alive(proc.pid):
                    break
                try:
                    proc.kill()
                    proc.wait(1)
                except subprocess.TimeoutExpired:
                    pass


class BFSSearchPoint:
    def __init__(
        self, start_position, move_vector, heading_angle=0.0, horizon_angle=0.0
    ):
        self.start_position = start_position
        self.move_vector = defaultdict(lambda: 0.0)
        self.move_vector.update(move_vector)
        self.heading_angle = heading_angle
        self.horizon_angle = horizon_angle

    def target_point(self):
        x = self.start_position["x"] + self.move_vector["x"]
        z = self.start_position["z"] + self.move_vector["z"]
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
            xs.append(point["x"])
            zs.append(point["z"])
            points.add(str(point["x"]) + "," + str(point["z"]))

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
            x, z = map(float, point.split(","))
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
            queue.append(dict(z=p["z"] + mag, x=p["x"]))
            queue.append(dict(z=p["z"] - mag, x=p["x"]))
            queue.append(dict(z=p["z"], x=p["x"] + mag))
            queue.append(dict(z=p["z"], x=p["x"] - mag))
            seen_points.add(json.dumps(p))

        enqueue_island_points(self.grid_points[0])

        while queue:
            point_to_find = queue.pop()
            for p in self.grid_points:
                dist = math.sqrt(
                    ((point_to_find["x"] - p["x"]) ** 2)
                    + ((point_to_find["z"] - p["z"]) ** 2)
                )

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
            dist = math.sqrt(
                ((point["x"] - p["x"]) ** 2) + ((point["z"] - p["z"]) ** 2)
            )
            if dist <= (self.grid_size + 0.01) and dist > 0:
                graph.add_edge(self.key_for_point(point), self.key_for_point(p))

    def move_relative_points(self, all_points, graph, position, rotation):

        action_orientation = {
            0: dict(x=0, z=1, action="MoveAhead"),
            90: dict(x=1, z=0, action="MoveRight"),
            180: dict(x=0, z=-1, action="MoveBack"),
            270: dict(x=-1, z=0, action="MoveLeft"),
        }

        move_points = dict()

        for n in graph.neighbors(self.key_for_point(position)):
            point = all_points[n]
            x_o = round((point["x"] - position["x"]) / self.grid_size)
            z_o = round((point["z"] - position["z"]) / self.grid_size)
            for target_rotation, offsets in action_orientation.items():
                delta = round(rotation + target_rotation) % 360
                ao = action_orientation[delta]
                action_name = action_orientation[target_rotation]["action"]
                if x_o == ao["x"] and z_o == ao["z"]:
                    move_points[action_name] = point
                    break

        return move_points

    def plan_horizons(self, agent_horizon, target_horizon):
        actions = []
        horizon_step_map = {330: 3, 0: 2, 30: 1, 60: 0}
        look_diff = (
            horizon_step_map[int(agent_horizon)] - horizon_step_map[int(target_horizon)]
        )
        if look_diff > 0:
            for i in range(look_diff):
                actions.append(dict(action="LookDown"))
        else:
            for i in range(abs(look_diff)):
                actions.append(dict(action="LookUp"))

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
                actions.append(dict(action="RotateRight"))
        else:
            for i in range(int(left_steps)):
                actions.append(dict(action="RotateLeft"))

        return actions

    def shortest_plan(self, graph, agent, target):
        import networkx as nx

        path = nx.shortest_path(
            graph,
            self.key_for_point(agent["position"]),
            self.key_for_point(target["position"]),
        )
        actions = []
        all_points = {}

        for point in self.grid_points:
            all_points[self.key_for_point(point)] = point

        # assert all_points[path[0]] == agent['position']

        current_position = agent["position"]
        current_rotation = agent["rotation"]["y"]

        for p in path[1:]:
            inv_pms = {
                self.key_for_point(v): k
                for k, v in self.move_relative_points(
                    all_points, graph, current_position, current_rotation
                ).items()
            }
            actions.append(dict(action=inv_pms[p]))
            current_position = all_points[p]

        actions += self.plan_horizons(agent["cameraHorizon"], target["cameraHorizon"])
        actions += self.plan_rotations(agent["rotation"]["y"], target["rotation"]["y"])
        # self.visualize_points(path)

        return actions

    def enqueue_point(self, point):

        # ensure there are no points near the new point
        if self._check_visited or not any(
            map(
                lambda p: distance(p, point.target_point()) < self.distance_threshold,
                self.seen_points,
            )
        ):
            self.seen_points.append(point.target_point())
            self.queue.append(point)

    def enqueue_points(self, agent_position):

        if not self.allow_enqueue:
            return

        if not self._check_visited or not any(
            map(
                lambda p: distance(p, agent_position) < self.distance_threshold,
                self.visited_seen_points,
            )
        ):
            self.enqueue_point(
                BFSSearchPoint(agent_position, dict(x=-1 * self.grid_size))
            )
            self.enqueue_point(BFSSearchPoint(agent_position, dict(x=self.grid_size)))
            self.enqueue_point(
                BFSSearchPoint(agent_position, dict(z=-1 * self.grid_size))
            )
            self.enqueue_point(
                BFSSearchPoint(agent_position, dict(z=1 * self.grid_size))
            )
            self.visited_seen_points.append(agent_position)

    def search_all_closed(self, scene_name):
        self.allow_enqueue = True
        self.queue = deque()
        self.seen_points = []
        self.visited_seen_points = []
        self.grid_points = []
        self.reset(scene_name)
        event = self.step(dict(action="Initialize", gridSize=self.grid_size))
        self.enqueue_points(event.metadata["agent"]["position"])
        while self.queue:
            self.queue_step()
            # self.visualize_points(scene_name)

    def start_search(
        self,
        scene_name,
        random_seed,
        full_grid,
        current_receptacle_object_pairs,
        randomize=True,
    ):

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
            object_id, receptacle_object_id = op.split("||")
            receptacle_object_pairs.append(
                dict(receptacleObjectId=receptacle_object_id, objectId=object_id)
            )

        if randomize:
            self.random_initialize(
                random_seed=random_seed,
                unique_object_types=True,
                exclude_receptacle_object_pairs=receptacle_object_pairs,
            )

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
        # self.visualize_points(scene_name)

    # get rid of unreachable points
    def prune_points(self):
        final_grid_points = set()

        for gp in self.grid_points:
            final_grid_points.add(key_for_point(gp["x"], gp["z"]))

        pruned_grid_points = []

        for gp in self.grid_points:
            found = False
            for x in [1, -1]:
                found |= (
                    key_for_point(gp["x"] + (self.grid_size * x), gp["z"])
                    in final_grid_points
                )

            for z in [1, -1]:
                found |= (
                    key_for_point(gp["x"], (self.grid_size * z) + gp["z"])
                    in final_grid_points
                )

            if found:
                pruned_grid_points.append(gp)

        self.grid_points = pruned_grid_points

    def is_object_visible(self, object_id):
        for obj in self.last_event.metadata["objects"]:
            if obj["objectId"] == object_id and obj["visible"]:
                return True
        return False

    def find_visible_receptacles(self):
        receptacle_points = []
        receptacle_pivot_points = []

        # pickup all objects
        visibility_object_id = None
        visibility_object_types = ["Mug", "CellPhone"]
        for obj in self.last_event.metadata["objects"]:
            if obj["pickupable"]:
                self.step(
                    action=dict(
                        action="PickupObject",
                        objectId=obj["objectId"],
                        forceVisible=True,
                    )
                )
            if (
                visibility_object_id is None
                and obj["objectType"] in visibility_object_types
            ):
                visibility_object_id = obj["objectId"]

        for point in self.grid_points:
            self.step(
                dict(action="Teleport", x=point["x"], y=point["y"], z=point["z"]),
                raise_for_failure=True,
            )

            for rot, hor in product(self.rotations, self.horizons):
                event = self.step(
                    dict(action="RotateLook", rotation=rot, horizon=hor),
                    raise_for_failure=True,
                )
                for j in event.metadata["objects"]:
                    if j["receptacle"] and j["visible"]:
                        receptacle_points.append(
                            dict(
                                distance=j["distance"],
                                pivotId=0,
                                receptacleObjectId=j["objectId"],
                                searchNode=dict(
                                    horizon=hor,
                                    rotation=rot,
                                    openReceptacle=False,
                                    pivotId=0,
                                    receptacleObjectId="",
                                    x=point["x"],
                                    y=point["y"],
                                    z=point["z"],
                                ),
                            )
                        )

                        if j["openable"]:
                            self.step(
                                action=dict(
                                    action="OpenObject",
                                    forceVisible=True,
                                    objectId=j["objectId"],
                                ),
                                raise_for_failure=True,
                            )
                        for pivot_id in range(j["receptacleCount"]):
                            self.step(
                                action=dict(
                                    action="Replace",
                                    forceVisible=True,
                                    receptacleObjectId=j["objectId"],
                                    objectId=visibility_object_id,
                                    pivot=pivot_id,
                                ),
                                raise_for_failure=True,
                            )
                            if self.is_object_visible(visibility_object_id):
                                receptacle_pivot_points.append(
                                    dict(
                                        distance=j["distance"],
                                        pivotId=pivot_id,
                                        receptacleObjectId=j["objectId"],
                                        searchNode=dict(
                                            horizon=hor,
                                            rotation=rot,
                                            openReceptacle=j["openable"],
                                            pivotId=pivot_id,
                                            receptacleObjectId=j["objectId"],
                                            x=point["x"],
                                            y=point["y"],
                                            z=point["z"],
                                        ),
                                    )
                                )

                        if j["openable"]:
                            self.step(
                                action=dict(
                                    action="CloseObject",
                                    forceVisible=True,
                                    objectId=j["objectId"],
                                ),
                                raise_for_failure=True,
                            )

        return receptacle_pivot_points, receptacle_points

    def find_visible_objects(self):

        seen_target_objects = defaultdict(list)

        for point in self.grid_points:
            self.step(
                dict(action="Teleport", x=point["x"], y=point["y"], z=point["z"]),
                raise_for_failure=True,
            )

            for rot, hor in product(self.rotations, self.horizons):
                event = self.step(
                    dict(action="RotateLook", rotation=rot, horizon=hor),
                    raise_for_failure=True,
                )

                object_receptacle = dict()
                for obj in event.metadata["objects"]:
                    if obj["receptacle"]:
                        for pso in obj["pivotSimObjs"]:
                            object_receptacle[pso["objectId"]] = obj

                for obj in filter(
                    lambda x: x["visible"] and x["pickupable"],
                    event.metadata["objects"],
                ):

                    # if obj['objectId'] in object_receptacle and\
                    #        object_receptacle[obj['objectId']]['openable'] and not \
                    #        object_receptacle[obj['objectId']]['isopen']:
                    #    continue

                    seen_target_objects[obj["objectId"]].append(
                        dict(distance=obj["distance"], agent=event.metadata["agent"])
                    )

        return seen_target_objects

    def initialize_scene(self):
        self.target_objects = []
        self.object_receptacle = defaultdict(
            lambda: dict(objectId="StartupPosition", pivotSimObjs=[])
        )

        self.open_receptacles = []
        open_pickupable = {}
        pickupable = {}
        is_open = {}

        for obj in filter(
            lambda x: x["receptacle"], self.last_event.metadata["objects"]
        ):
            for oid in obj["receptacleObjectIds"]:
                self.object_receptacle[oid] = obj

            is_open[obj["objectId"]] = obj["openable"] and obj["isOpen"]

        for obj in filter(
            lambda x: x["receptacle"], self.last_event.metadata["objects"]
        ):
            for oid in obj["receptacleObjectIds"]:
                if obj["openable"] or (
                    obj["objectId"] in self.object_receptacle
                    and self.object_receptacle[obj["objectId"]]["openable"]
                ):

                    open_pickupable[oid] = obj["objectId"]
                else:
                    pickupable[oid] = obj["objectId"]

        if open_pickupable.keys():
            self.target_objects = random.sample(open_pickupable.keys(), k=1)
            shuffled_keys = list(open_pickupable.keys())
            random.shuffle(shuffled_keys)
            for oid in shuffled_keys:
                position_target = self.object_receptacle[self.target_objects[0]][
                    "position"
                ]
                position_candidate = self.object_receptacle[oid]["position"]
                dist = math.sqrt(
                    (position_target["x"] - position_candidate["x"]) ** 2
                    + (position_target["y"] - position_candidate["y"]) ** 2
                )
                # try to find something that is far to avoid having the doors collide
                if dist > 1.25:
                    self.target_objects.append(oid)
                    break

        for roid in set(map(lambda x: open_pickupable[x], self.target_objects)):
            if roid in is_open:
                continue
            self.open_receptacles.append(roid)
            self.step(
                dict(action="OpenObject", objectId=roid, forceVisible=True),
                raise_for_failure=True,
            )

    def queue_step(self):
        search_point = self.queue.popleft()
        event = self.step(
            dict(
                action="Teleport",
                x=search_point.start_position["x"],
                y=search_point.start_position["y"],
                z=search_point.start_position["z"],
            )
        )

        if event.metadata["lastActionSuccess"]:
            move_vec = search_point.move_vector
            move_vec["moveMagnitude"] = self.grid_size
            event = self.step(dict(action="Move", **move_vec))
            if event.metadata["agent"]["position"]["y"] > 1.3:
                # pprint(search_point.start_position)
                # pprint(search_point.move_vector)
                # pprint(event.metadata['agent']['position'])
                raise Exception("**** got big point ")

            self.enqueue_points(event.metadata["agent"]["position"])

            if not any(
                map(
                    lambda p: distance(p, event.metadata["agent"]["position"])
                    < self.distance_threshold,
                    self.grid_points,
                )
            ):
                self.grid_points.append(event.metadata["agent"]["position"])

        return event
