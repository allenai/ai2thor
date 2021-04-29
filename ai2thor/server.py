# Copyright Allen Institute for Artificial Intelligence 2017
"""
ai2thor.server

Handles all communication with Unity through a Flask service.  Messages
are sent to the controller using a pair of request/response queues.
"""

import warnings
import numpy as np
from enum import Enum
from ai2thor.util.depth import apply_real_noise, generate_noise_indices
import json
import sys


class NumpyAwareEncoder(json.JSONEncoder):
    def default(self, obj):
        if isinstance(obj, np.ndarray):
            return obj.tolist()
        if isinstance(obj, np.generic):
            return obj.item()
        return super(NumpyAwareEncoder, self).default(obj)


class MultiAgentEvent(object):
    def __init__(self, active_agent_id, events):
        self._active_event = events[active_agent_id]
        self.metadata = self._active_event.metadata
        self.screen_width = self._active_event.screen_width
        self.screen_height = self._active_event.screen_height
        self.events = events
        self.third_party_camera_frames = []
        # XXX add methods for depth,sem_seg

    def __bool__(self):
        return bool(self._active_event)

    @property
    def cv2img(self):
        return self._active_event.cv2img

    def add_third_party_camera_image(self, third_party_image_data):
        self.third_party_camera_frames.append(
            read_buffer_image(
                third_party_image_data, self.screen_width, self.screen_height
            )
        )


def read_buffer_image(
    buf, width, height, flip_y=True, flip_x=False, dtype=np.uint8, flip_rb_colors=False
):
    im_bytes = (
        np.frombuffer(buf.tobytes(), dtype=dtype)
        if sys.version_info.major < 3
        else np.frombuffer(buf, dtype=dtype)
    )
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


class MetadataWrapper(dict):
    def __getitem__(self, x):
        # alias deprecated functionality
        if x == "reachablePositions":
            last_action = super().__getitem__("lastAction")
            if last_action == "GetReachablePositions":
                warnings.warn(
                    'The key event.metadata["reachablePositions"] is deprecated and has been remapped to event.metadata["actionReturn"].'
                )
                x = "actionReturn"
            elif last_action == "GetSceneBounds":
                # Undocumented GetSceneBounds used to only populate reachablePositions,
                # and not actionReturn. This now maintains both sideways and
                # backwards compatibility in such a case.
                if "reachablePositions" in self:
                    return super().__getitem__(x)
                else:
                    warnings.warn(
                        'The key event.metadata["reachablePositions"] is deprecated and has been remapped to event.metadata["actionReturn"].'
                    )
                    x = "actionReturn"
            else:
                raise IndexError(
                    "You are trying to access event.metadata['reachablePositions'] without first "
                    + "calling controller.step(action='GetReachablePositions'). Also, "
                    + "the key 'reachablePositions' is deprecated in favor of event.metadata['actionReturn']."
                )
        return super().__getitem__(x)


class Event:
    """
    Object that is returned from a call to controller.step().
    This class wraps the screenshot that Unity captures as well
    as the metadata sent about each object
    """

    def __init__(self, metadata):
        self.metadata = MetadataWrapper(metadata)
        self.screen_width = metadata["screenWidth"]
        self.screen_height = metadata["screenHeight"]

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
        self.semantic_segmentation_frame = None

        self.class_detections2D = {}

        self.process_colors()
        self.process_visible_bounds2D()
        self.third_party_camera_frames = []
        self.third_party_semantic_segmentation_frames = []
        self.third_party_instance_segmentation_frames = []
        self.third_party_depth_frames = []
        self.third_party_normals_frames = []
        self.third_party_flows_frames = []

        self.events = [self]  # Ensure we have a similar API to MultiAgentEvent

    def __bool__(self):
        return self.metadata["lastActionSuccess"]

    def __repr__(self):
        """Summarizes the results from an Event."""
        action_return = str(self.metadata["actionReturn"])
        if len(action_return) > 100:
            action_return = action_return[:100] + "..."
        return (
            "<ai2thor.server.Event at "
            + str(hex(id(self)))
            + '\n    .metadata["lastAction"] = '
            + str(self.metadata["lastAction"])
            + '\n    .metadata["lastActionSuccess"] = '
            + str(self.metadata["lastActionSuccess"])
            + '\n    .metadata["errorMessage"] = "'
            + str(self.metadata["errorMessage"]).replace("\n", " ")
            + '\n    .metadata["actionReturn"] = '
            + action_return
            + "\n>"
        )

    def __str__(self):
        return self.__repr__()

    @property
    def image_data(self):
        warnings.warn(
            "Event.image_data has been removed - RGB data can be retrieved from event.frame and encoded to an image format"
        )
        return None

    @property
    def class_segmentation_frame(self):
        warnings.warn(
            "event.class_segmentation_frame has been renamed to event.semantic_segmentation_frame.",
            DeprecationWarning,
        )
        return self.semantic_segmentation_frame

    def process_visible_bounds2D(self):
        if self.instance_detections2D and len(self.instance_detections2D) > 0:
            for obj in self.metadata["objects"]:
                obj["visibleBounds2D"] = (
                    obj["visible"] and obj["objectId"] in self.instance_detections2D
                )

    def process_colors(self):
        if "colors" in self.metadata and self.metadata["colors"]:
            for color_data in self.metadata["colors"]:
                name = color_data["name"]
                c_key = tuple(color_data["color"])
                self.color_to_object_id[c_key] = name
                self.object_id_to_color[name] = c_key

    def objects_by_type(self, object_type):
        return [
            obj for obj in self.metadata["objects"] if obj["objectType"] == object_type
        ]

    def process_colors_ids(self):
        if self.instance_segmentation_frame is None:
            return

        MIN_DETECTION_LEN = 0

        self.instance_detections2D = {}
        unique_ids, unique_inverse = unique_rows(
            self.instance_segmentation_frame.reshape(-1, 3), return_inverse=True
        )
        unique_inverse = unique_inverse.reshape(
            self.instance_segmentation_frame.shape[:2]
        )
        unique_masks = (
            np.tile(unique_inverse[np.newaxis, :, :], (len(unique_ids), 1, 1))
            == np.arange(len(unique_ids))[:, np.newaxis, np.newaxis]
        )
        # for unique_color_ind, unique_color in enumerate(unique_ids):
        for color_bounds in self.metadata["colorBounds"]:
            color = np.array(color_bounds["color"])
            color_name = self.color_to_object_id.get(
                tuple(int(cc) for cc in color), "background"
            )
            cls = color_name
            simObj = False
            if "|" in cls:
                cls = cls.split("|")[0]
                simObj = True

            bb = np.array(color_bounds["bounds"])
            bb[[1, 3]] = self.metadata["screenHeight"] - bb[[3, 1]]
            if not (
                (bb[2] - bb[0]) < MIN_DETECTION_LEN
                or (bb[3] - bb[1]) < MIN_DETECTION_LEN
            ):
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
                    self.class_masks[cls] = np.logical_or(
                        self.class_masks[cls], unique_masks[color_ind, ...]
                    )

    def _image_depth(self, image_depth_data, **kwargs):
        image_depth = read_buffer_image(
            image_depth_data, self.screen_width, self.screen_height
        )
        depth_format = kwargs["depth_format"]
        image_depth_out = (
            image_depth[:, :, 0]
            + image_depth[:, :, 1] / np.float32(256)
            + image_depth[:, :, 2] / np.float32(256 ** 2)
        )
        multiplier = 1.0
        if depth_format != DepthFormat.Normalized:
            multiplier = kwargs["camera_far_plane"] - kwargs["camera_near_plane"]
        elif depth_format == DepthFormat.Millimeters:
            multiplier *= 1000
        image_depth_out *= multiplier / 256.0

        depth_image_float = image_depth_out.astype(np.float32)

        if "add_noise" in kwargs and kwargs["add_noise"]:
            depth_image_float = apply_real_noise(
                depth_image_float, self.screen_width, indices=kwargs["noise_indices"]
            )

        return depth_image_float

    def add_image_depth_robot(self, image_depth_data, depth_format, **kwargs):
        multiplier = 1.0
        camera_far_plane = kwargs.pop("camera_far_plane", 1)
        camera_near_plane = kwargs.pop("camera_near_plane", 0)
        if depth_format == DepthFormat.Normalized:
            multiplier = 1.0 / (camera_far_plane - camera_near_plane)
        elif depth_format == DepthFormat.Millimeters:
            multiplier = 1000.0

        image_depth = (
            read_buffer_image(
                image_depth_data, self.screen_width, self.screen_height, **kwargs
            ).reshape(self.screen_height, self.screen_width)
            * multiplier
        )
        self.depth_frame = image_depth.astype(np.float32)

    def add_image_depth(self, image_depth_data, **kwargs):
        self.depth_frame = self._image_depth(image_depth_data, **kwargs)

    def add_third_party_image_depth(self, image_depth_data, **kwargs):
        self.third_party_depth_frames.append(
            self._image_depth(image_depth_data, **kwargs)
        )

    def add_third_party_image_normals(self, normals_data):
        self.third_party_normals_frames.append(
            read_buffer_image(normals_data, self.screen_width, self.screen_height)
        )

    def add_image_normals(self, image_normals_data):
        self.normals_frame = read_buffer_image(
            image_normals_data, self.screen_width, self.screen_height
        )

    def add_third_party_image_flows(self, flows_data):
        self.third_party_flows_frames.append(
            read_buffer_image(flows_data, self.screen_width, self.screen_height)
        )

    def add_image_flows(self, image_flows_data):
        self.flows_frame = read_buffer_image(
            image_flows_data, self.screen_width, self.screen_height
        )

    def add_third_party_camera_image(self, third_party_image_data):
        self.third_party_camera_frames.append(
            read_buffer_image(
                third_party_image_data, self.screen_width, self.screen_height
            )
        )

    def add_image(self, image_data, **kwargs):
        self.frame = read_buffer_image(
            image_data, self.screen_width, self.screen_height, **kwargs
        )

    def add_image_ids(self, image_ids_data):
        self.instance_segmentation_frame = read_buffer_image(
            image_ids_data, self.screen_width, self.screen_height
        )
        self.process_colors_ids()

    def add_third_party_image_ids(self, image_ids_data):
        self.third_party_instance_segmentation_frames.append(
            read_buffer_image(image_ids_data, self.screen_width, self.screen_height)
        )

    def add_image_classes(self, image_classes_data):
        self.semantic_segmentation_frame = read_buffer_image(
            image_classes_data, self.screen_width, self.screen_height
        )

    def add_third_party_image_classes(self, image_classes_data):
        self.third_party_semantic_segmentation_frames.append(
            read_buffer_image(image_classes_data, self.screen_width, self.screen_height)
        )

    def cv2image(self):
        warnings.warn("Deprecated - please use event.cv2img")
        return self.cv2img

    @property
    def cv2img(self):
        return self.frame[..., ::-1]

    @property
    def pose(self):
        agent_meta = self.metadata["agent"]
        loc = agent_meta["position"]
        rotation = round(agent_meta["rotation"]["y"] * 1000)
        horizon = round(agent_meta["cameraHorizon"] * 1000)
        return (round(loc["x"] * 1000), round(loc["z"] * 1000), rotation, horizon)

    @property
    def pose_discrete(self):
        # XXX should have this as a parameter
        step_size = 0.25
        agent_meta = self.metadata["agent"]
        loc = agent_meta["position"]
        rotation = int(agent_meta["rotation"]["y"] / 90.0)
        horizon = int(round(agent_meta["cameraHorizon"]))
        return (int(loc["x"] / step_size), int(loc["z"] / step_size), rotation, horizon)

    def get_object(self, object_id):
        for obj in self.metadata["objects"]:
            if obj["objectId"] == object_id:
                return obj
        return None


class DepthFormat(Enum):
    Meters = (0,)
    Normalized = (1,)
    Millimeters = 2


class Server(object):
    def __init__(
        self, width, height, depth_format=DepthFormat.Meters, add_depth_noise=False
    ):
        self.depth_format = depth_format
        self.add_depth_noise = add_depth_noise
        self.noise_indices = None
        self.camera_near_plane = 0.1
        self.camera_far_plane = 20.0
        self.sequence_id = 0
        self.started = False
        self.client_token = None
        self.unity_proc = None

        if add_depth_noise:
            assert width == height, "Noise supported with square dimension images only."
            self.noise_indices = generate_noise_indices(width)

    def set_init_params(self, init_params):
        self.camera_near_plane = init_params["cameraNearPlane"]
        self.camera_far_plane = init_params["cameraFarPlane"]

    def create_event(self, metadata, files):
        if metadata["sequenceId"] != self.sequence_id:
            raise ValueError(
                "Sequence id mismatch: %s vs %s"
                % (metadata["sequenceId"], self.sequence_id)
            )

        events = []

        for i, a in enumerate(metadata["agents"]):
            e = Event(a)
            image_mapping = dict(
                image=e.add_image,
                image_depth=lambda x: e.add_image_depth(
                    x,
                    depth_format=self.depth_format,
                    camera_near_plane=self.camera_near_plane,
                    camera_far_plane=self.camera_far_plane,
                    add_noise=self.add_depth_noise,
                    noise_indices=self.noise_indices,
                ),
                image_ids=e.add_image_ids,
                image_classes=e.add_image_classes,
                image_normals=e.add_image_normals,
                image_flow=e.add_image_flows,
            )

            for key in image_mapping.keys():
                if key in files:
                    image_mapping[key](files[key][i])

            third_party_image_mapping = {
                # if we want to convert this param to underscores in Unity, we will need to
                # keep the mapping with the dash for backwards compatibility with older
                # Unity builds
                "image-thirdParty-camera": e.add_third_party_camera_image,
                "image_thirdParty_depth": lambda x: e.add_third_party_image_depth(
                    x,
                    depth_format=self.depth_format,
                    camera_near_plane=self.camera_near_plane,
                    camera_far_plane=self.camera_far_plane,
                ),
                "image_thirdParty_image_ids": e.add_third_party_image_ids,
                "image_thirdParty_classes": e.add_third_party_image_classes,
                "image_thirdParty_normals": e.add_third_party_image_normals,
                "image_thirdParty_flows": e.add_third_party_image_flows,
            }

            if a["thirdPartyCameras"] is not None:
                for ti, t in enumerate(a["thirdPartyCameras"]):
                    for key in third_party_image_mapping.keys():
                        if key in files:
                            third_party_image_mapping[key](files[key][ti])
            events.append(e)

        if len(events) > 1:
            self.last_event = MultiAgentEvent(metadata["activeAgentId"], events)
        else:
            self.last_event = events[0]

        return self.last_event
