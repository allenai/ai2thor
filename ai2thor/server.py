# Copyright Allen Institute for Artificial Intelligence 2017
"""
ai2thor.server

Handles all communication with Unity through a Flask service.  Messages
are sent to the controller using a pair of request/response queues.
"""
import json
import subprocess
import warnings
from abc import abstractmethod, ABC
from collections.abc import Mapping
from enum import Enum
from typing import Optional, Tuple, Dict, cast, List, Set

import numpy as np

from ai2thor.util.depth import apply_real_noise, generate_noise_indices


class NumpyAwareEncoder(json.JSONEncoder):
    def default(self, obj):
        if isinstance(obj, np.ndarray):
            return obj.tolist()
        if isinstance(obj, np.generic):
            return obj.item()
        return super(NumpyAwareEncoder, self).default(obj)


class LazyMask(Mapping):

    def __contains__(self, key: object) -> bool:
        return self.mask(cast(str, key)) is not None

    def __getitem__(self, key: str):

        m = self.mask(key)

        if m is None:
            raise KeyError(key)

        return m

    @abstractmethod
    def mask(self, key: str, default: Optional[np.ndarray]=None) -> Optional[np.ndarray]:
        pass

    @abstractmethod
    def _load_all(self):
        pass

    def __iter__(self):
        self._load_all()
        return iter(self._masks)

    def __len__(self):
        self._load_all()
        return len(self._masks)



class LazyInstanceSegmentationMasks(LazyMask):

    def __init__(self, image_ids_data: bytes, metadata: dict):
        self._masks: Dict[str, np.ndarray] = {}
        self._loaded = False
        screen_width = metadata["screenWidth"]
        screen_height = metadata["screenHeight"]
        item_size = int(len(image_ids_data)/(screen_width * screen_height))
        self._unique_integer_keys: Optional[Set[np.uint32]] = None
        self._empty_mask: Optional[np.ndarray] = None

        if item_size == 4:
            self.instance_segmentation_frame_uint32 = read_buffer_image(
                image_ids_data, screen_width, screen_height, dtype=np.uint32
            ).squeeze()
            # within ImageSynthesis.cs a RGBA32 frame is read, the alpha channel
            # is always equal to 255
            self._alpha_channel_value = 255

        elif item_size == 3: # 3 byte per pixel for backwards compatibility, RGB24 texture
            # this is more expensive than the 4 byte variant since copying is required
            frame = read_buffer_image(image_ids_data, screen_width, screen_height)
            self.instance_segmentation_frame_uint32 = np.concatenate(
                (
                    frame,
                    np.zeros((frame.shape[0], frame.shape[1], 1), dtype=np.uint8),
                ),
                axis=2,
            )
            self.instance_segmentation_frame_uint32.dtype = np.uint32
            self.instance_segmentation_frame_uint32 = self.instance_segmentation_frame_uint32.squeeze()
            self._alpha_channel_value = 0

        # At this point we should have a 2d matrix of shape (height, width)
        # with a 32bit uint as the value

        self.instance_colors: Dict[str, List[int]]= {}
        self.class_colors: Dict[str, List[List[int]]] = {}
        for c in metadata["colors"]:
            cls = c["name"]

            if "|" in c["name"]:
                self.instance_colors[c["name"]] = c["color"]
                cls = c["name"].split("|")[0]

            if cls not in self.class_colors:
                self.class_colors[cls] = []

            self.class_colors[cls].append(c["color"])


    @property
    def empty_mask(self) -> np.ndarray:
        if self._empty_mask is None:
            self._empty_mask = np.zeros(self.instance_segmentation_frame_uint32.shape, dtype=bool)
            self._empty_mask.flags["WRITEABLE"] = False

        return self._empty_mask

    @property
    def unique_integer_keys(self) -> Set[np.uint32]:
        if self._unique_integer_keys is None:
            self._unique_integer_keys = set(np.unique(self.instance_segmentation_frame_uint32))

        return self._unique_integer_keys

    def _integer_color_key(self, color: List[int]) -> np.uint32:
        a = np.array(color + [self._alpha_channel_value], dtype=np.uint8)
        # mypy complains, but it is safe to modify the dtype on an ndarray
        a.dtype = np.uint32 # type: ignore
        return a[0]

    def _load_all(self):
        if not self._loaded:
            all_integer_keys = self.unique_integer_keys
            for color_name, color in self.instance_colors.items():
                if self._integer_color_key(color) in all_integer_keys:
                    self.__getitem__(color_name)

        self._loaded = True


    def mask(self, key: str, default: Optional[np.ndarray]=None) -> Optional[np.ndarray]:
        if key not in self.instance_colors:
            return default
        elif key in self._masks:
            return self._masks[key]

        m = self.instance_segmentation_frame_uint32 == self._integer_color_key(
            self.instance_colors[key]
        )

        if m.any():
            self._masks[key] = m
            return m
        else:
            return default


class LazyClassSegmentationMasks(LazyMask):
    def __init__(self, instance_masks: LazyInstanceSegmentationMasks):
        self.instance_masks = instance_masks
        self._loaded = False
        self._masks: Dict[str, np.ndarray] = {}

    def _load_all(self):
        if not self._loaded:
            all_integer_keys = self.instance_masks.unique_integer_keys
            for cls, colors in self.instance_masks.class_colors.items():
                for color in colors:
                    if self.instance_masks._integer_color_key(color) in all_integer_keys:
                        self.__getitem__(cls)
                        break
        self._loaded = True

    def mask(self, key: str, default: Optional[np.ndarray]=None) -> Optional[np.ndarray]:
        if key in self._masks:
            return self._masks[key]

        class_mask = np.zeros(self.instance_masks.instance_segmentation_frame_uint32.shape, dtype=bool)

        if key == "background":
            # "background" is a special name for any color that wasn't included in the metadata
            # this is mainly done for backwards compatibility since we only have a handful of instances
            # of this across all scenes (e.g. FloorPlan412 - thin strip above the doorway)
            all_integer_keys = self.instance_masks.unique_integer_keys
            metadata_color_keys = set()
            for cls, colors in self.instance_masks.class_colors.items():
                for color in colors:
                    metadata_color_keys.add(self.instance_masks._integer_color_key(color))

            background_keys = all_integer_keys - metadata_color_keys
            for ik in background_keys:
                mask = self.instance_masks.instance_segmentation_frame_uint32 == ik
                class_mask = np.logical_or(class_mask, mask)

        elif "|" not in key:
            for color in self.instance_masks.class_colors.get(key, []):
                mask = self.instance_masks.instance_segmentation_frame_uint32 == self.instance_masks._integer_color_key(color)
                class_mask = np.logical_or(class_mask, mask)

        if class_mask.any():
            self._masks[key] = class_mask
            return class_mask
        else:
            return default

class LazyDetections2D(Mapping):
    def __init__(self, instance_masks: LazyInstanceSegmentationMasks):

        self.instance_masks = instance_masks

    def mask_bounding_box(self, mask: np.ndarray) -> Optional[Tuple[int, int, int, int]]:
        rows = np.any(mask, axis=1)
        cols = np.any(mask, axis=0)
        rw = np.where(rows)
        if len(rw[0]) == 0:
            return None

        rmin, rmax = map(int, rw[0][[0, -1]])
        cmin, cmax = map(int, np.where(cols)[0][[0, -1]])

        return cmin, rmin, cmax, rmax

    def __contains__(self, key: object) -> bool:
        return key in self.instance_masks

    def __eq__(self, other: object):
        if isinstance(other, self.__class__):
            return self.instance_masks == other.instance_masks
        else:
            return False

class LazyInstanceDetections2D(LazyDetections2D):

    def __init__(self, instance_masks: LazyInstanceSegmentationMasks):
        super().__init__(instance_masks)
        self._detections2d : Dict[str, Optional[Tuple[int, int, int, int]]] = {}

    def __eq__(self, other: object):
        if isinstance(other, self.__class__):
            return self.instance_masks == other.instance_masks
        else:
            return False

    def mask_bounding_box(self, mask: np.ndarray) -> Optional[Tuple[int, int, int, int]]:
        rows = np.any(mask, axis=1)
        cols = np.any(mask, axis=0)
        rw = np.where(rows)
        if len(rw[0]) == 0:
            return None

        rmin, rmax = map(int, rw[0][[0, -1]])
        cmin, cmax = map(int, np.where(cols)[0][[0, -1]])

        return cmin, rmin, cmax, rmax

    def __contains__(self, key: object) -> bool:
        return key in self.instance_masks

    def __getitem__(self, key: str) -> Optional[Tuple[int, int, int, int]]:
        if key in self._detections2d:
            return self._detections2d[key]

        mask = self.instance_masks[key]
        self._detections2d[key] = self.mask_bounding_box(mask)

        return self._detections2d[key]

    def __len__(self) -> int:
        return len(self.instance_masks)

    def __iter__(self):
        return iter(self.instance_masks.keys())


class LazyClassDetections2D(LazyDetections2D):

    def __init__(self, instance_masks: LazyInstanceSegmentationMasks):

        super().__init__(instance_masks)
        self._loaded = False
        self._detections2d : Dict[str, Optional[Tuple[Tuple[int, int, int, int], ...]]] = {}

    def __eq__(self, other: object):
        if isinstance(other, self.__class__):
            return self.instance_masks == other.instance_masks
        else:
            return False

    def __len__(self) -> int:
        self._load_all()
        return len(self._detections2d)

    def _load_all(self):
        if not self._loaded:
            all_integer_keys = self.instance_masks.unique_integer_keys
            for cls, colors in self.instance_masks.class_colors.items():
                for color in colors:
                    if self.instance_masks._integer_color_key(color) in all_integer_keys:
                        self.__getitem__(cls)
                        break

        self._loaded = True

    def __iter__(self):
        self._load_all()

        return iter(self._detections2d)

    def __getitem__(self, cls: str) -> Optional[Tuple[Tuple[int, int, int, int], ...]]:
        if cls in self._detections2d:
            return self._detections2d[cls]

        detections = []

        for color in self.instance_masks.class_colors.get(cls, []):
            mask = self.instance_masks.instance_segmentation_frame_uint32 == self.instance_masks._integer_color_key(color)
            bb = self.mask_bounding_box(mask)
            if bb:
                detections.append(bb)
        if detections:
            self._detections2d[cls] = tuple(detections)
        else:
            raise KeyError(cls)

        return self._detections2d[cls]


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
    im_bytes = np.frombuffer(buf, dtype=dtype)

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
        elif x == "hand":
            if "hand" not in self:
                # maintains sideways compatibility
                warnings.warn(
                    'The key event.metadata["hand"] is deprecated and has been remapped to event.metadata["heldObjectPose"].'
                )
                x = "heldObjectPose"
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
        self.third_party_instance_masks = []
        self.third_party_class_masks = []
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

    def process_colors_ids(self, image_ids_data):

        self.instance_masks = LazyInstanceSegmentationMasks(image_ids_data, self.metadata)
        self.class_masks = LazyClassSegmentationMasks(self.instance_masks)
        self.class_detections2D = LazyClassDetections2D(self.instance_masks)
        self.instance_detections2D = LazyInstanceDetections2D(self.instance_masks)


    def _image_depth(self, image_depth_data, **kwargs):
        item_size = int(len(image_depth_data)/(self.screen_width * self.screen_height))

        multipliers = {
            DepthFormat.Normalized: 1.0,
            DepthFormat.Meters: (kwargs["camera_far_plane"] - kwargs["camera_near_plane"]),
            DepthFormat.Millimeters: (kwargs["camera_far_plane"] - kwargs["camera_near_plane"]) * 1000.0
        }

        target_depth_format = kwargs["depth_format"]
        # assume Normalized for backwards compatibility
        source_depth_format = DepthFormat[self.metadata.get("depthFormat", "Normalized")]
        multiplier = multipliers[target_depth_format]/multipliers[source_depth_format]

        if item_size == 4: # float32
            image_depth_out = read_buffer_image(
                image_depth_data, self.screen_width, self.screen_height, dtype=np.float32
            ).squeeze()

        elif item_size  == 3: # 3 byte 1/256.0 precision, legacy depth binary format
            image_depth = read_buffer_image(
                image_depth_data, self.screen_width, self.screen_height
            )
            image_depth_out = (
                image_depth[:, :, 0]
                + image_depth[:, :, 1] / np.float32(256)
                + image_depth[:, :, 2] / np.float32(256 ** 2)
            )

            multiplier /= 256.0
        else:
            raise Exception("invalid shape for depth image %s" % (image_depth.shape,))

        if multiplier != 1.0:
            if not image_depth_out.flags["WRITEABLE"]:
                image_depth_out = np.copy(image_depth_out)

            image_depth_out *= multiplier

        if "add_noise" in kwargs and kwargs["add_noise"]:
            image_depth_out = apply_real_noise(
                image_depth_out, self.screen_width, indices=kwargs["noise_indices"]
            )

        return image_depth_out

    def add_third_party_camera_image_robot(self, third_party_image_data, width, height):
        self.third_party_camera_frames.append(
            read_buffer_image(third_party_image_data, width, height)
        )

    def add_third_party_image_depth_robot(
        self, image_depth_data, depth_format, **kwargs
    ):
        multiplier = 1.0
        camera_far_plane = kwargs.pop("camera_far_plane", 1)
        camera_near_plane = kwargs.pop("camera_near_plane", 0)
        depth_width = kwargs.pop("depth_width", self.screen_width)
        depth_height = kwargs.pop("depth_height", self.screen_height)
        if depth_format == DepthFormat.Normalized:
            multiplier = 1.0 / (camera_far_plane - camera_near_plane)
        elif depth_format == DepthFormat.Millimeters:
            multiplier = 1000.0

        image_depth = (
            read_buffer_image(
                image_depth_data, depth_width, depth_height, **kwargs
            ).reshape(depth_height, depth_width)
            * multiplier
        )
        self.third_party_depth_frames.append(image_depth.astype(np.float32))

    def add_image_depth_robot(self, image_depth_data, depth_format, **kwargs):
        multiplier = 1.0
        camera_far_plane = kwargs.pop("camera_far_plane", 1)
        camera_near_plane = kwargs.pop("camera_near_plane", 0)
        depth_width = kwargs.pop("depth_width", self.screen_width)
        depth_height = kwargs.pop("depth_height", self.screen_height)

        if depth_format == DepthFormat.Normalized:
            multiplier = 1.0 / (camera_far_plane - camera_near_plane)
        elif depth_format == DepthFormat.Millimeters:
            multiplier = 1000.0

        image_depth = (
            read_buffer_image(
                image_depth_data, depth_width, depth_height, **kwargs
            ).reshape(depth_height, depth_width)
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
        )[
            :, :, :3
        ]  # CloudRendering returns 4 channels instead of 3

    def add_image_ids(self, image_ids_data):
        self.instance_segmentation_frame = read_buffer_image(
            image_ids_data, self.screen_width, self.screen_height
            )[:, :, :3]

        self.process_colors_ids(image_ids_data)

    def add_third_party_image_ids(self, image_ids_data):
        instance_segmentation_frame = read_buffer_image(
            image_ids_data, self.screen_width, self.screen_height
        )[:, :, :3]

        self.third_party_instance_segmentation_frames.append(
            instance_segmentation_frame
        )
        instance_masks = LazyInstanceSegmentationMasks(image_ids_data, self.metadata)
        self.third_party_instance_masks.append(instance_masks)
        self.third_party_class_masks.append(LazyClassSegmentationMasks(instance_masks))

    def add_image_classes(self, image_classes_data):
        self.semantic_segmentation_frame = read_buffer_image(
            image_classes_data, self.screen_width, self.screen_height
            )[:, :, :3]

    def add_third_party_image_classes(self, image_classes_data):
        self.third_party_semantic_segmentation_frames.append(
            read_buffer_image(image_classes_data, self.screen_width, self.screen_height)[:, :, :3]
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


class Server(ABC):
    def __init__(
        self,
        width,
        height,
        timeout: Optional[float],
        depth_format=DepthFormat.Meters,
        add_depth_noise=False,
    ):
        self.depth_format = depth_format
        self.add_depth_noise = add_depth_noise
        self.timeout = timeout

        self.noise_indices = None
        self.camera_near_plane = 0.1
        self.camera_far_plane = 20.0
        self.sequence_id = 0
        self.started = False
        self.client_token = None
        self.unity_proc: Optional[subprocess.Popen] = None

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

    @abstractmethod
    def start(self):
        raise NotImplementedError

    @abstractmethod
    def stop(self):
        raise NotImplementedError

    @abstractmethod
    def send(self, action):
        raise NotImplementedError

    @abstractmethod
    def receive(self, timeout: Optional[float] = None):
        raise NotImplementedError
