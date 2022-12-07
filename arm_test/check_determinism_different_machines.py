import datetime
import json
import pdb
import math
import os
import sys

root_dir = os.path.normpath(os.path.dirname(os.path.realpath(__file__)) + "/..")
sys.path.insert(0, root_dir)
import ai2thor.controller
import ai2thor
import random
import copy
import time
import argparse

MAX_TESTS = 20
MAX_EP_LEN = 100
scene_names = ["FloorPlan{}_physics".format(i + 1) for i in range(30)]
set_of_actions = ["mm", "rr", "ll", "w", "z", "a", "s", "u", "j", "3", "4", "p"]

new_actions = True
new_action_suffix = "New"

controller = ai2thor.controller.Controller(
    local_build=True,
    scene=scene_names[0],
    gridSize=0.25,
    width=900,
    height=900,
    agentMode="arm",
    fieldOfView=100,
    start_unity=False,
    port=8200,
    host="127.0.0.1",
    # server_class=ai2thor.wsgi_server.WsgiServer
    server_class=ai2thor.fifo_server.FifoServer,
)

ADITIONAL_ARM_ARGS = {
    "disableRendering": True,
    "restrictMovement": False,
    # "waitForFixedUpdate": False,  // deprecated
    "returnToStart": True,
    "speed": 1
}

ADDITONAL_MOVEMENT_ARGS = {
        "disableRendering": True,
        "returnToStart": True,
        "speed": 1
    }

ADITIONAL_NEW_MOVEMENT_ARGS= {
        "returnToStart": True,
        "speed": 1,
        "physicsSimulationParams": {
            "autoSimulation": False
        }
    }

MoveArm = "MoveArm"

MoveArmBase = "MoveArmBase"
RotateAgent = "RotateAgent"
MoveAhead = "MoveAhead"
MoveAgent = "MoveAgent"


ADITIONAL_ARM_ARGS_BY_ACTION = {

    "MoveArmNew": {
        "restrictMovement": False,
        **ADITIONAL_NEW_MOVEMENT_ARGS
    },
    "MoveArmBaseNew": ADITIONAL_NEW_MOVEMENT_ARGS,
    "RotateAgentNew": ADITIONAL_NEW_MOVEMENT_ARGS,
    "MoveAheadNew": ADITIONAL_NEW_MOVEMENT_ARGS,
    "MoveAgentNew": ADITIONAL_NEW_MOVEMENT_ARGS,

    "MoveArm": {
        "restrictMovement": False,
        **ADDITONAL_MOVEMENT_ARGS
    },
    "MoveArmBase": ADDITONAL_MOVEMENT_ARGS,
    "RotateAgent": ADDITONAL_MOVEMENT_ARGS,
    "MoveAhead": ADDITONAL_MOVEMENT_ARGS,
    "MoveAgent": ADDITONAL_MOVEMENT_ARGS

}

def actionName(action):
    # return action
    if not new_actions:
        return action
    else:
        return f"{action}{new_action_suffix}"

MOVE_CONSTANT = 0.05


def get_reachable_positions(controller):
    event = controller.step("GetReachablePositions")
    reachable_positions = event.metadata["reachablePositions"]
    return reachable_positions


def execute_command(controller, command, action_dict_addition_by_action):

    base_position = get_current_arm_state(controller)
    change_height = MOVE_CONSTANT
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
        action_details = dict(action="DropMidLevelHand", add_extra_args=True)
    elif command == "mm":
        action_details = dict(
            action=actionName(MoveAgent),
            ahead=0.2,
            add_extra_args=True
        )

    elif command == "rr":
        action_details = dict(
            action= actionName(RotateAgent), degrees=45, add_extra_args=True
        )

    elif command == "ll":
        action_details = dict(
            action=actionName(RotateAgent), degrees=-45, add_extra_args=True
        )

    elif command == "m":
        action_details = dict(action=actionName(MoveAhead), add_extra_args=True)

    elif command == "r":
        # action_details = dict(action="RotateRight", degrees=45, add_extra_args=True)
        action_details = dict(action=actionName(RotateAgent), degrees=45, add_extra_args=True)
    elif command == "l":
        # action_details = dict(action="RotateLeft", degrees=45, add_extra_args=True)
        action_details = dict(action=actionName(RotateAgent), degrees=-45, add_extra_args=True)

    elif command == "p":
        action_details = dict(action="PickupObject")
    elif command == "q":
        action_details = {}
    else:
        action_details = {}

    if command in ["w", "z", "s", "a", "3", "4"]:
        action_details = dict(
            action=actionName(MoveArm),
            position=dict(
                x=base_position["x"], y=base_position["y"], z=base_position["z"]
            ),
            add_extra_args=True
        )

    elif command in ["u", "j"]:
        if base_position["h"] > 1:
            base_position["h"] = 1
        elif base_position["h"] < 0:
            base_position["h"] = 0

        action_details = dict(
            action=actionName(MoveArmBase),
            y=base_position["h"],
            add_extra_args=True
        )

    if 'add_extra_args' in action_details and action_details['add_extra_args']:
        del action_details['add_extra_args']
        action_dict_addition = action_dict_addition_by_action[action_details['action']]
        action_details = dict(**action_details, **action_dict_addition)
    if 'action' in action_details:
        # print(f"Calling action: {action_details['action']} with {action_details}")
        controller.step(
            **action_details
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
    # https://lgtm.com/rules/7860092/
    dict_equal = len(dict1) == len(dict2)
    assert dict_equal, ("different len", dict1, dict2)
    equal = True
    for k in dict1:
        val1 = dict1[k]
        val2 = dict2[k]
        # https://lgtm.com/rules/7860092/
        type_equal = type(val1) == type(val2)
        assert type_equal, ("different type", dict1, dict2)
        try:
            if type(val1) == dict:
                equal = two_dict_equal(val1, val2)
            elif type(val1) == list:
                equal = two_list_equal(val1, val2)
            elif type(val1) == str:
                equal = val1 == val2
            elif val1 is None:
                equal = val1 is None and val2 is None
            elif math.isnan(val1):
                equal = math.isnan(val2)
            elif type(val1) == float:
                equal = abs(val1 - val2) < 0.001
            else:
                equal = val1 == val2
            if not equal:
                print("not equal", val1, val2)
                return equal
        except Exception as err:
            print(f"Unexpected {err}, {type(err)}")
            print(f"val1 {val1} val2 {val2}")
            equal = False

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
            standing=True
        )
        initial_pose = dict(
            action="TeleportFull",
            x=initial_location["x"],
            y=initial_location["y"],
            z=initial_location["z"],
            rotation=dict(x=0, y=initial_rotation, z=0),
            horizon=10,
            standing=True
        )
        controller.step("PausePhysicsAutoSim")
        all_commands = []
        before = datetime.datetime.now()
        for j in range(MAX_EP_LEN):
            command = random.choice(set_of_actions)

            execute_command(controller, command, ADITIONAL_ARM_ARGS_BY_ACTION)
            all_commands.append(command)

            pickupable = controller.last_event.metadata["arm"]["pickupableObjects"]
            picked_up_before = controller.last_event.metadata["arm"]["heldObjects"]
            if len(pickupable) > 0 and len(picked_up_before) == 0:
                cmd = "p"
                execute_command(controller, cmd, ADITIONAL_ARM_ARGS_BY_ACTION)
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


def determinism_test(all_tests, test_index=None):
    # Redo the actions 20 times:
    # only do this if an object is picked up
    passed_count = 0
    tests = all_tests.items()
    if test_index is not  None:
        tests = [list(tests)[test_index]]
    for k, test_point in tests:
        start = time.time()
        print(f"Test {k}")
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
            standing=True
        )
        controller.step("PausePhysicsAutoSim")
        for cmd in all_commands:
            execute_command(controller, cmd, ADITIONAL_ARM_ARGS_BY_ACTION)
            # if controller.last_action['action'] == MoveArm:
            #     break
        current_state = get_current_full_state(controller)
        if not two_dict_equal(final_state, current_state):
            print("not deterministic")
            print("scene name", controller.last_event.metadata["sceneName"])
            print("initial pose", initial_pose)
            print("list of actions", all_commands)
            # pdb.set_trace()
        else:
            print("test {} passed".format(k))
            passed_count += 1
        end = time.time()
        print(f"Elapsed: {end - start}")
        print(f"Passed: {passed_count}/{len(all_tests.items())}")


if __name__ == "__main__":
    # all_dict = random_tests()
    # with open('determinism_json.json' ,'w') as f:
    #     json.dump(all_dict, f)

    with open("arm_test/determinism_json_2.json", "r") as f:
        all_dict = json.load(f)

    parser = argparse.ArgumentParser(
        prog='Arm Determinism Tests',
        description='Testing arm determinism')
    parser.add_argument('-i', '--index', type=int)

    args = parser.parse_args()

    print(args.index)

    determinism_test(all_dict, args.index)
