import datetime
import json
import pdb
import os
import sys

root_dir = os.path.normpath(os.path.dirname(os.path.realpath(__file__)) + "/..")
sys.path.insert(0, root_dir)
import ai2thor.controller
import math
import ai2thor
import random
import copy

MAX_TESTS = 20
MAX_EP_LEN = 100
scene_names = ["FloorPlan{}_physics".format(i + 1) for i in range(30)]
set_of_actions = ["mm", "rr", "ll", "w", "z", "a", "s", "u", "j", "3", "4", "p"]


controller = ai2thor.controller.Controller(
    local_build=True,
    scene=scene_names[0],
    gridSize=0.25,
    width=900,
    height=900,
    agentMode="arm",
    fieldOfView=100,
    agentControllerType="mid-level",
    server_class=ai2thor.fifo_server.FifoServer,
)

ADITIONAL_ARM_ARGS = {
    "disableRendering": True,
    "restrictMovement": False,
    "waitForFixedUpdate": False,
    "eventCollisions": True,
    "returnToStart": True,
    "speed": 1,
    "move_constant": 0.05,
}


def get_reachable_positions(controller):
    event = controller.step("GetReachablePositions")
    reachable_positions = event.metadata["reachablePositions"]
    return reachable_positions


def execute_command(controller, command, action_dict_addition):

    base_position = get_current_arm_state(controller)
    change_height = action_dict_addition["move_constant"]
    change_value = change_height
    action_details = {}

    if command == "w":
        base_position["z"] += change_value
    elif command == "z":
        base_position["z"] -= change_value
    elif command == "s":
        base_position["x"] += change_value
    elif command == "a":
        base_position["x"] -= change_value
    elif command == "3":
        base_position["y"] += change_value
    elif command == "4":
        base_position["y"] -= change_value
    elif command == "u":
        base_position["h"] += change_height
    elif command == "j":
        base_position["h"] -= change_height
    elif command == "/":
        action_details = dict("")
        pickupable = controller.last_event.metadata["arm"]["pickupableObjects"]
        print(pickupable)
    elif command == "d":
        controller.step(action="DropMidLevelHand", **action_dict_addition)
        action_details = dict(action="DropMidLevelHand", **action_dict_addition)
    elif command == "mm":
        action_dict_addition = copy.copy(action_dict_addition)
        if "moveSpeed" in action_dict_addition:
            action_dict_addition["speed"] = action_dict_addition["moveSpeed"]
        controller.step(
            action="MoveContinuous",
            direction=dict(x=0.0, y=0.0, z=0.2),
            **action_dict_addition
        )
        action_details = dict(
            action="MoveContinuous",
            direction=dict(x=0.0, y=0.0, z=0.2),
            **action_dict_addition
        )

    elif command == "rr":
        action_dict_addition = copy.copy(action_dict_addition)

        if "moveSpeed" in action_dict_addition:
            action_dict_addition["speed"] = action_dict_addition["moveSpeed"]
        controller.step(
            action="RotateContinuous", degrees=45, **action_dict_addition
        )
        action_details = dict(
            action="RotateContinuous", degrees=45, **action_dict_addition
        )
    elif command == "ll":
        action_dict_addition = copy.copy(action_dict_addition)
        controller.step(
            action="RotateContinuous", degrees=-45, **action_dict_addition
        )
        action_details = dict(
            action="RotateContinuous", degrees=-45, **action_dict_addition
        )
    elif command == "m":
        controller.step(action="MoveAhead", **action_dict_addition)
        action_details = dict(action="MoveAhead", **action_dict_addition)

    elif command == "r":
        controller.step(
            action="RotateRight", degrees=45, **action_dict_addition
        )
        action_details = dict(action="RotateRight", degrees=45, **action_dict_addition)
    elif command == "l":
        controller.step(action="RotateLeft", degrees=45, **action_dict_addition)
        action_details = dict(action="RotateLeft", degrees=45, **action_dict_addition)
    elif command == "p":
        controller.step(action="PickUpMidLevelHand")
        action_details = dict(action="PickUpMidLevelHand")
    elif command == "q":
        action_details = {}
    else:
        action_details = {}

    if command in ["w", "z", "s", "a", "3", "4"]:

        controller.step(
            action="MoveMidLevelArm",
            position=dict(
                x=base_position["x"], y=base_position["y"], z=base_position["z"]
            ),
            handCameraSpace=False,
            **action_dict_addition
        )
        action_details = dict(
            action="MoveMidLevelArm",
            position=dict(
                x=base_position["x"], y=base_position["y"], z=base_position["z"]
            ),
            handCameraSpace=False,
            **action_dict_addition
        )

    elif command in ["u", "j"]:
        if base_position["h"] > 1:
            base_position["h"] = 1
        elif base_position["h"] < 0:
            base_position["h"] = 0

        controller.step(
            action="MoveArmBase", y=base_position["h"], **action_dict_addition
        )
        action_details = dict(
            action="MoveArmBase", y=base_position["h"], **action_dict_addition
        )


    return action_details


def get_current_arm_state(controller):
    h_min = 0.450998873
    h_max = 1.8009994
    event = controller.last_event
    joints = event.metadata["arm"]["joints"]
    arm = joints[-1]
    assert arm["name"] == "robot_arm_4_jnt"
    xyz_dict = arm["rootRelativePosition"]
    height_arm = joints[0]["position"]["y"]
    xyz_dict["h"] = (height_arm - h_min) / (h_max - h_min)
    #     print_error([x['position']['y'] for x in joints])
    return xyz_dict


def reset_the_scene_and_get_reachables(scene_name=None):
    if scene_name is None:
        scene_name = random.choice(scene_names)
    controller.reset(scene_name)
    return get_reachable_positions(controller)


def two_list_equal(l1, l2):
    dict1 = {i: v for (i, v) in enumerate(l1)}
    dict2 = {i: v for (i, v) in enumerate(l2)}
    return two_dict_equal(dict1, dict2)


def two_dict_equal(dict1, dict2):
    # removing calls to len to resolve https://lgtm.com/rules/7860092/
    dict_equal = len(dict1) == len(dict2)

    assert dict_equal, ("different len", dict1, dict2)
    equal = True
    for k in dict1:
        val1 = dict1[k]
        val2 = dict2[k]
        # https://lgtm.com/rules/7860092/
        type_equal = type(val1) == type(val2)
        assert type_equal, ("different type", dict1, dict2)
        if type(val1) == dict:
            equal = two_dict_equal(val1, val2)
        elif type(val1) == list:
            equal = two_list_equal(val1, val2)
        elif math.isnan(val1):
            equal = math.isnan(val2)
        elif type(val1) == float:
            equal = abs(val1 - val2) < 0.001
        else:
            equal = val1 == val2
        if not equal:
            print("not equal", val1, val2)
            return equal
    return equal


def get_current_full_state(controller):
    return {
        "agent_position": controller.last_event.metadata["agent"]["position"],
        "agent_rotation": controller.last_event.metadata["agent"]["rotation"],
        "arm_state": controller.last_event.metadata["arm"]["joints"],
        "held_object": controller.last_event.metadata["arm"]["heldObjects"],
    }


def random_tests():
    all_timers = []

    all_dict = {}

    for i in range(MAX_TESTS):
        print("test number", i)
        reachable_positions = reset_the_scene_and_get_reachables()

        initial_location = random.choice(reachable_positions)
        initial_rotation = random.choice([i for i in range(0, 360, 45)])
        controller.step(
            action="TeleportFull",
            x=initial_location["x"],
            y=initial_location["y"],
            z=initial_location["z"],
            rotation=dict(x=0, y=initial_rotation, z=0),
            horizon=10,
        )
        initial_pose = dict(
            action="TeleportFull",
            x=initial_location["x"],
            y=initial_location["y"],
            z=initial_location["z"],
            rotation=dict(x=0, y=initial_rotation, z=0),
            horizon=10,
        )
        controller.step("PausePhysicsAutoSim")
        all_commands = []
        before = datetime.datetime.now()
        for j in range(MAX_EP_LEN):
            command = random.choice(set_of_actions)
            execute_command(controller, command, ADITIONAL_ARM_ARGS)
            all_commands.append(command)

            pickupable = controller.last_event.metadata["arm"]["pickupableObjects"]
            picked_up_before = controller.last_event.metadata["arm"]["heldObjects"]
            if len(pickupable) > 0 and len(picked_up_before) == 0:
                cmd = "p"
                execute_command(controller, cmd, ADITIONAL_ARM_ARGS)
                all_commands.append(cmd)
                if controller.last_event.metadata["lastActionSuccess"] is False:
                    print("Failed to pick up ")
                    print("scene name", controller.last_event.metadata["sceneName"])
                    print("initial pose", initial_pose)
                    print("list of actions", all_commands)
                    break

        after = datetime.datetime.now()
        time_diff = after - before
        seconds = time_diff.total_seconds()
        all_timers.append(len(all_commands) / seconds)

        final_state = get_current_full_state(
            controller
        )  # made sure this does not require deep copy
        scene_name = controller.last_event.metadata["sceneName"]

        # TODO only when pick up has happened
        dict_to_add = {
            "initial_location": initial_location,
            "initial_rotation": initial_rotation,
            "all_commands": all_commands,
            "final_state": final_state,
            "initial_pose": initial_pose,
            "scene_name": scene_name,
        }
        all_dict[len(all_dict)] = dict_to_add
        # print('FPS', sum(all_timers) / len(all_timers))
    return all_dict


def determinism_test(all_tests):
    # Redo the actions 20 times:
    # only do this if an object is picked up
    for k, test_point in all_tests.items():
        initial_location = test_point["initial_location"]
        initial_rotation = test_point["initial_rotation"]
        all_commands = test_point["all_commands"]
        final_state = test_point["final_state"]
        initial_pose = test_point["initial_pose"]
        scene_name = test_point["scene_name"]

        controller.reset(scene_name)
        controller.step(
            action="TeleportFull",
            x=initial_location["x"],
            y=initial_location["y"],
            z=initial_location["z"],
            rotation=dict(x=0, y=initial_rotation, z=0),
            horizon=10,
        )
        controller.step("PausePhysicsAutoSim")
        for cmd in all_commands:
            execute_command(controller, cmd, ADITIONAL_ARM_ARGS)
        current_state = get_current_full_state(controller)
        if not two_dict_equal(final_state, current_state):
            print("not deterministic")
            print("scene name", controller.last_event.metadata["sceneName"])
            print("initial pose", initial_pose)
            print("list of actions", all_commands)
            pdb.set_trace()
        else:
            print("test {} passed".format(k))


if __name__ == "__main__":
    # all_dict = random_tests()
    # with open('determinism_json.json' ,'w') as f:
    #     json.dump(all_dict, f)

    with open("arm_test/determinism_json.json", "r") as f:
        all_dict = json.load(f)
    determinism_test(all_dict)
