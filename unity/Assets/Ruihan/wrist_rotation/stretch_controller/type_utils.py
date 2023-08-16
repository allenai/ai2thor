import sys
from typing import Any, Dict, List, Optional

try:
    from typing import Literal, TypedDict
except ImportError:
    from typing_extensions import Literal, TypedDict

from allenact.base_abstractions.sensor import Sensor
from attrs import define


class Vector3(TypedDict):
    x: float
    y: float
    z: float


@define
class TaskSamplerArgs:
    process_ind: int
    """The process index number."""

    mode: Literal["train", "eval"]
    """Whether we are in training or evaluation mode."""

    house_inds: List[int]
    """Which houses to use for each process."""

    houses: Any
    """The hugging face Dataset of all the houses in the split."""

    sensors: List[Sensor]
    """The sensors to use for each task."""

    controller_args: Dict[str, Any]
    """The arguments to pass to the AI2-THOR controller."""

    reward_config: Dict[str, Any]
    """The reward configuration to use."""

    target_object_types: List[str]
    """The object types to use as targets."""

    action_names: List[str]
    """The object types to use as targets."""

    max_steps: int
    """The maximum number of steps to run each task."""

    max_tasks: int
    """The maximum number of tasks to run."""

    distance_type: str
    """The type of distance computation to use ("l2" or "geo")."""

    resample_same_scene_freq: int
    """
    Number of times to sample a scene/house before moving to the next one.

    If <1 then will never 
        sample a new scene (unless `force_advance_scene=True` is passed to `next_task(...)`.
    ."""

    # Can we remove?
    deterministic_cudnn: bool = False
    loop_dataset: bool = True
    seed: Optional[int] = None
    allow_flipping: bool = False


@define
class RewardConfig:
    step_penalty: float
    goal_success_reward: float
    failed_stop_reward: float
    shaping_weight: float
    reached_horizon_reward: float
    positive_only_reward: bool


class AgentPose(TypedDict):
    position: Vector3
    rotation: Vector3
    horizon: int
    standing: bool


class AbstractTaskArgs(TypedDict):
    sensors: List[Sensor]
    max_steps: int
    action_names: List[str]
    reward_config: Optional[RewardConfig]


class ObjectNavTaskArgs(AbstractTaskArgs):
    pass


class ObjectExploreTaskArgs(AbstractTaskArgs):
    pass


class Fetch2RoomTaskArgs(AbstractTaskArgs):
    pass


class Fetch2SurfaceTaskArgs(AbstractTaskArgs):
    pass


class FetchTaskArgs(AbstractTaskArgs):
    pass


class THORActions:
    move_ahead = "m"
    move_back = "b"
    rotate_right = "r"
    rotate_left = "l"
    rotate_right_small = "rs"
    rotate_left_small = "ls"
    done = "end"
    move_arm_up = "yp"
    move_arm_up_small = "yps"
    move_arm_down = "ym"
    move_arm_down_small = "yms"
    move_arm_out = "zp"
    move_arm_out_small = "zps"
    move_arm_in = "zm"
    move_arm_in_small = "zms"
    wrist_open = "wp"
    wrist_close = "wm"
    pickup = "p"
    dropoff = "d"
    ARM_ACTIONS = [
        move_arm_in,
        move_arm_out,
        move_arm_up,
        move_arm_down,
        move_arm_in_small,
        move_arm_out_small,
        move_arm_up_small,
        move_arm_down_small,
    ]
    sub_done = "sub_done"


REGISTERED_TASK_PARAMS: Dict[str, List[str]] = {}

if sys.version_info >= (3, 9):

    def get_required_keys(cls):
        return getattr(cls, "__required_keys__", [])

else:

    def get_required_keys(cls):
        return list(cls.__annotations__.keys())


def register_task_specific_params(cls):
    REGISTERED_TASK_PARAMS[cls.__name__] = get_required_keys(cls)
    return cls


@register_task_specific_params
class ObjectNavType(TypedDict):
    target_object_type: str
    target_object_ids: List[str]


@register_task_specific_params
class ObjectNavRoom(TypedDict):
    target_object_type: str
    target_object_ids: List[str]
    source_room_type: str


@register_task_specific_params
class ObjectNavRelAttribute(TypedDict):
    target_object_type: str
    target_object_ids: List[str]
    source_room_id: str
    source_room_type: str
    rel_attribute: str


@register_task_specific_params
class Fetch2RoomType(TypedDict):
    target_object_type: str
    target_object_ids: List[str]
    source_room_types: List[str]
    target_room_type: str
    target_room_ids: List[str]


@register_task_specific_params
class Fetch2RoomObjRoom(TypedDict):
    target_object_type: str
    target_object_ids: List[str]
    source_room_type: str
    target_room_type: str
    target_room_ids: List[str]


@register_task_specific_params
class Fetch2RoomRelAttribute(TypedDict):
    target_object_type: str
    target_object_id: str
    source_room_type: str
    target_room_type: str
    target_room_ids: List[str]
    rel_attribute: str


@register_task_specific_params
class Fetch2SurfaceType(TypedDict):
    target_object_type: str
    target_object_ids: List[str]
    source_receptacle_types: List[str]
    target_receptacle_type: str
    target_receptacle_ids: List[str]


@register_task_specific_params
class Fetch2SurfaceObjRoom(TypedDict):
    target_object_type: str
    target_object_ids: List[str]
    source_room_type: str
    source_receptacle_types: List[str]
    target_receptacle_type: str
    target_receptacle_ids: List[str]


@register_task_specific_params
class Fetch2SurfaceRelAttribute(TypedDict):
    target_object_type: str
    target_object_id: str
    source_room_type: str
    source_receptacle_types: List[str]
    target_receptacle_type: str
    target_receptacle_ids: List[str]
    rel_attribute: str


@register_task_specific_params
class FetchType(TypedDict):
    target_object_type: str
    target_object_ids: List[str]


@register_task_specific_params
class FetchObjRoom(TypedDict):
    target_object_type: str
    target_object_ids: List[str]
    source_room_type: str


@register_task_specific_params
class FetchRelAttribute(TypedDict):
    target_object_type: str
    target_object_ids: List[str]
    source_room_id: str
    source_room_type: str
    rel_attribute: str


@register_task_specific_params
class ExploreObjects(TypedDict):
    pass


@register_task_specific_params
class ExploreHouse(TypedDict):
    pass


@register_task_specific_params
class RoomNav(TypedDict):
    target_room_type: str
    target_room_ids: List[str]


@register_task_specific_params
class ObjectNavMulti(TypedDict):
    target_object_types: List[str]
    target_object_ids: List[List[str]]


@register_task_specific_params
class ObjectNavMofN(TypedDict):
    target_object_types: List[str]
    target_object_ids: List[List[str]]
    num_required: int


@register_task_specific_params
class Fetch2RefType(TypedDict):
    source_object_type: str
    source_object_ids: List[str]
    target_object_type: str
    target_object_ids: List[str]


@register_task_specific_params
class Fetch2RefObjRoom(TypedDict):
    source_object_type: str
    source_object_ids: List[str]
    source_room_type: str
    target_object_type: str
    target_object_ids: List[str]


@register_task_specific_params
class Fetch2RefRelAttribute(TypedDict):
    source_object_type: str
    source_object_ids: List[str]
    source_room_type: str
    target_object_type: str
    target_object_ids: List[str]
    rel_attribute: str


@register_task_specific_params
class Fetch2SurfaceMultiObject(TypedDict):
    source_object_types: List[str]
    source_object_ids: List[List[str]]
    source_receptacle_types: List[List[str]]
    target_receptacle_type: str
    target_receptacle_ids: List[str]
