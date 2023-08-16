from stretch_controller.stretch_initialization_utils import (
    WRIST_ROTATION,
    ADDITIONAL_ARM_ARGS,
    STRETCH_ENV_ARGS
)
from stretch_controller.stretch_controller import StretchController
from door_parsing import filter_dataset

import prior

def rotate_ee(stretch_controller):
    wrist_target_angle = 90
    current_wrist_angle = stretch_controller.get_arm_wrist_position()
    count = 0
    while abs(current_wrist_angle - wrist_target_angle) > 1 and count < 2 * int(360 / WRIST_ROTATION):
        print("Before Operation: ", current_wrist_angle, wrist_target_angle)
        action_dict = dict(action="RotateWristRelative", yaw=-WRIST_ROTATION)
        action_dict = {**action_dict, **ADDITIONAL_ARM_ARGS}
        event = stretch_controller.step(**action_dict)
        count += 1
        print(action_dict)
        print(event.metadata["actionReturn"])
        current_wrist_angle = stretch_controller.get_arm_wrist_position()
        print("After Operation: ", current_wrist_angle, wrist_target_angle)
    return count < 2 * int(360 / WRIST_ROTATION)


# from srestretch_initialization_utils import STRETCH_ENV_ARGS

dataset = prior.load_dataset("procthor-10k")['train']
dataset = filter_dataset(dataset)

stretch_controller = StretchController(**STRETCH_ENV_ARGS, scene=dataset[0])

id1 = 410 + 10
id2 = 410 + 12

stretch_controller.reset(dataset[id1])

rotate_ee(stretch_controller)
pose = {} 
pose["fieldOfView"] = 50
pose["position"] = {}
pose["position"]["y"] = 3
pose["position"]["x"] = stretch_controller.controller.last_event.metadata["agent"]["position"]["x"]
pose["position"]["z"] = stretch_controller.controller.last_event.metadata["agent"]["position"]["z"]
pose["orthographic"] = False
pose["farClippingPlane"] = 50
pose["rotation"] = {'x': 90.0, 'y': 0.0, 'z': 0.0}
# add the camera to the scene
event = stretch_controller.step(
    action="AddThirdPartyCamera",
    **pose,
    skyboxColor="white",
    raise_for_failure=True,
)

topview = stretch_controller.controller.last_event.third_party_camera_frames[1]
import matplotlib.pyplot as plt
plt.imsave(f"room_filter/after_reori_{id1}.jpg", topview)


stretch_controller.reset(dataset[id2])

rotate_ee(stretch_controller)
pose = {} 
pose["fieldOfView"] = 50
pose["position"] = {}
pose["position"]["y"] = 3
pose["position"]["x"] = stretch_controller.controller.last_event.metadata["agent"]["position"]["x"]
pose["position"]["z"] = stretch_controller.controller.last_event.metadata["agent"]["position"]["z"]
pose["orthographic"] = False
pose["farClippingPlane"] = 50
pose["rotation"] = {'x': 90.0, 'y': 0.0, 'z': 0.0}
# add the camera to the scene
event = stretch_controller.step(
    action="AddThirdPartyCamera",
    **pose,
    skyboxColor="white",
    raise_for_failure=True,
)

topview = stretch_controller.controller.last_event.third_party_camera_frames[1]
import matplotlib.pyplot as plt
plt.imsave(f"room_filter/after_reori_{id2}.jpg", topview)

