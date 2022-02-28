import os
import msgpack
import numpy as np
import requests
import cv2
from pprint import pprint
import shutil
import copy

from ai2thor.server import Event, MultiAgentEvent, DepthFormat
from ai2thor.interact import InteractiveControllerPrompt, DefaultActions


class Controller(object):
    def __init__(
        self,
        headless=False,
        host="127.0.0.1",
        port=0,
        width=300,
        height=300,
        agent_id=0,
        image_dir=".",
        save_image_per_frame=False,
        depth_format=DepthFormat.Meters,
        camera_near_plane=0.1,
        camera_far_plane=20,
    ):
        self.host = host
        self.port = port
        self.headless = headless
        self.last_event = {}
        self.last_action = {}
        self.sequence_id = 0
        self.agent_id = agent_id
        self.screen_width = width
        self.screen_height = height
        self.depth_format = depth_format
        self.camera_near_plane = (camera_near_plane,)
        self.camera_far_plane = camera_far_plane

        if image_dir != ".":
            if os.path.exists(image_dir):
                shutil.rmtree(image_dir)
            os.makedirs(image_dir)

        self.interactive_controller = InteractiveControllerPrompt(
            [
                DefaultActions.MoveAhead,
                DefaultActions.MoveBack,
                DefaultActions.RotateLeft,
                DefaultActions.RotateRight,
                DefaultActions.LookUp,
                DefaultActions.LookDown,
            ],
            image_dir=image_dir,
            image_per_frame=save_image_per_frame,
        )

        self.start(port, host, agent_id=agent_id)

    def stop(self):
        pass

    def start(self, port=9200, host="127.0.0.1", agent_id=0, **kwargs):
        self.host = host
        self.port = port
        self.agent_id = agent_id

        # response_payload = self._post_event('start')
        pprint("-- Start:")
        # pprint(response_payload)

    def reset(self, scene_name=None):
        self.sequence_id = 0
        # response_payload = self._post_event(
        #     'reset', dict(action='Reset', sceneName=scene_name, sequenceId=self.sequence_id)
        # )
        pprint("-- Reset:")
        # pprint(response_payload)

        return self.last_event

    def step(self, action=None, **action_args):

        if type(action) is dict:
            action = copy.deepcopy(action)  # prevent changes from leaking
        else:
            action = dict(action=action)

        raise_for_failure = action_args.pop("raise_for_failure", False)

        action.update(action_args)

        if self.headless:
            action["renderImage"] = False

        action["sequenceId"] = self.sequence_id
        action["agentId"] = self.agent_id

        self.last_action = action

        rotation = action.get("rotation")
        if rotation is not None and type(rotation) != dict:
            action["rotation"] = {}
            action["rotation"]["y"] = rotation

        payload = self._post_event("step", action)
        events = []
        for i, agent_metadata in enumerate(payload["metadata"]["agents"]):
            event = Event(agent_metadata)
            image_mapping = dict(
                image=lambda x: event.add_image(x, flip_y=False, flip_rb_colors=False),
                image_depth=lambda x: event.add_image_depth_robot(
                    x,
                    self.depth_format,
                    camera_near_plane=self.camera_near_plane,
                    camera_far_plane=self.camera_far_plane,
                    flip_y=False,
                    dtype=np.float64,
                ),
            )
            for key in image_mapping.keys():
                if key in payload and len(payload[key]) > i:
                    image_mapping[key](payload[key][i])
            events.append(event)

        if len(events) > 1:
            self.last_event = MultiAgentEvent(self.agent_id, events)
        else:
            self.last_event = events[0]

        if (
            not self.last_event.metadata["lastActionSuccess"]
            and self.last_event.metadata["errorCode"] == "InvalidAction"
        ):
            raise ValueError(self.last_event.metadata["errorMessage"])

        if raise_for_failure:
            assert self.last_event.metadata["lastActionSuccess"]

        # pprint("Display event:")
        # Controller._display_step_event(self.last_event)

        return self.last_event

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
            semantic_segmentation_frame=semantic_segmentation_frame,
            instance_segmentation_frame=instance_segmentation_frame,
            depth_frame=depth_frame,
            color_frame=color_frame,
            metadata=metadata,
        )

    def _post_event(self, route="", data=None):
        r = requests.post(self._get_url(route), json=data)
        pprint('ACTION "{}"'.format(data["action"]))
        pprint("POST")
        # pprint(r.content)
        pprint(r.status_code)
        return msgpack.unpackb(r.content, raw=False)

    def _get_url(self, route=""):
        return "http://{}:{}{}".format(
            self.host, self.port, "/{}".format(route) if route != "" else ""
        )

    @staticmethod
    def _display_step_event(event):
        metadata = event.metadata
        pprint(metadata)
        cv2.imshow("aoeu", event.cv2img)
        cv2.waitKey(1000)
