import os
import sys

root_dir = os.path.normpath(os.path.dirname(os.path.realpath(__file__)) + "/..")
sys.path.insert(0, root_dir)
from arm_test.base import standard_pose, execute_actions
import arm_test.base

# pp
# inita
# rr
# mmlah 1 1
# telefull
# mmlah 0.5203709825292535 2 True
# pac
# mmla 0.01000303 -1.63912773e-06 0.558107364 2 armBase True
# pac
# mmlah 0.49074136446614885 2 True


actions = [
    {
        "action": "MoveArmBase",
        "y": 0.5203709825292535,
        "speed": 2,
        "disableRendering": True,
        "returnToStart": True,
    },
    {
        "action": "MoveMidLevelArm",
        "position": {"x": 0.01000303, "y": -1.63912773e-06, "z": 0.558107364},
        "speed": 2,
        "handCameraSpace": False,
        "disableRendering": True,
        "returnToStart": True,
    },
]

standard_pose()
execute_actions(actions, disableRendering=True, returnToStart=True)
event = arm_test.base.controller.step(
    "MoveArmBase",
    y=0.49074136446614885,
    disableRendering=True,
    returnToStart=True,
    speed=2.0,
)
assert event.metadata["lastActionSuccess"], "MoveArmBase failed; arm is stuck"
