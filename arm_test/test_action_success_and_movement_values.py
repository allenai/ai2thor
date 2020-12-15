import datetime
import pdb

import ai2thor.controller
import ai2thor
import random
import copy
import time

MAX_TESTS = 300
MAX_EP_LEN = 1000
# scene_indices = [i + 1 for i in range(30)] #Only kitchens
scene_indices = [i + 1 for i in range(30)] +[i + 1 for i in range(200,230)] +[i + 1 for i in range(300,330)] +[i + 1 for i in range(400,430)]
scene_names = ['FloorPlan{}_physics'.format(i) for i in scene_indices]
set_of_actions = ['mm', 'rr', 'll', 'w', 'z', 'a', 's', 'u', 'j', '3', '4', 'p']

controller = ai2thor.controller.Controller(
    scene=scene_names[0], gridSize=0.25,
    width=900, height=900, agentMode='arm', fieldOfView=100,
    agentControllerType='mid-level',
    server_class=ai2thor.fifo_server.FifoServer,
)

ADITIONAL_ARM_ARGS = {
    'disableRendering': True,
    'restrictMovement': False,
    'waitForFixedUpdate': False,
    'returnToStart': True,
    'speed': 1,
    'move_constant': 0.05,
}

def get_reachable_positions(controller):
    event = controller.step('GetReachablePositions')
    reachable_positions = event.metadata['reachablePositions']
    return reachable_positions


def execute_command(controller, command,action_dict_addition):

    base_position = get_current_arm_state(controller)
    change_height = action_dict_addition['move_constant']
    change_value = change_height
    action_details = {}

    if command == 'w':
        base_position['z'] += change_value
    elif command == 'z':
        base_position['z'] -= change_value
    elif command == 's':
        base_position['x'] += change_value
    elif command == 'a':
        base_position['x'] -= change_value
    elif command == '3':
        base_position['y'] += change_value
    elif command == '4':
        base_position['y'] -= change_value
    elif command == 'u':
        base_position['h'] += change_height
    elif command == 'j':
        base_position['h'] -= change_height
    elif command == '/':
        action_details = dict('')
        pickupable = controller.last_event.metadata['arm']['PickupableObjectsInsideHandSphere']
        print(pickupable)
    elif command == 'd':
        event = controller.step(action='DropMidLevelHand',**action_dict_addition)
        action_details = dict(action='DropMidLevelHand',**action_dict_addition)
    elif command == 'mm':
        action_dict_addition = copy.copy(action_dict_addition)
        if 'moveSpeed' in action_dict_addition:
            action_dict_addition['speed'] = action_dict_addition['moveSpeed']
        event = controller.step(action='MoveContinuous', direction=dict(x=0.0, y=0.0, z=.2),**action_dict_addition)
        action_details = dict(action='MoveContinuous', direction=dict(x=0.0, y=0.0, z=.2),**action_dict_addition)

    elif command == 'rr':
        action_dict_addition = copy.copy(action_dict_addition)

        if 'moveSpeed' in action_dict_addition:
            action_dict_addition['speed'] = action_dict_addition['moveSpeed']
        event = controller.step(action='RotateContinuous', degrees = 45,**action_dict_addition)
        action_details = dict(action='RotateContinuous', degrees = 45,**action_dict_addition)
    elif command == 'll':
        action_dict_addition = copy.copy(action_dict_addition)
        event = controller.step(action='RotateContinuous', degrees = -45,**action_dict_addition)
        action_details = dict(action='RotateContinuous', degrees = -45,**action_dict_addition)
    elif command == 'm':
        event = controller.step(action='MoveAhead',**action_dict_addition)
        action_details = dict(action='MoveAhead',**action_dict_addition)

    elif command == 'r':
        event = controller.step(action='RotateRight',degrees=45,**action_dict_addition)
        action_details = dict(action='RotateRight',degrees=45,**action_dict_addition)
    elif command == 'l':
        event = controller.step(action='RotateLeft',degrees=45,**action_dict_addition)
        action_details = dict(action='RotateLeft',degrees=45,**action_dict_addition)
    elif command == 'p':
        event = controller.step(action='PickUpMidLevelHand')
        action_details = dict(action='PickUpMidLevelHand')
    elif command == 'q':
        action_details = {}
    else:
        action_details = {}

    if command in ['w', 'z', 's', 'a', '3', '4']:

        event = controller.step(action='MoveMidLevelArm', position=dict(x=base_position['x'], y=base_position['y'], z=base_position['z']), handCameraSpace = False,**action_dict_addition)
        action_details=dict(action='MoveMidLevelArm', position=dict(x=base_position['x'], y=base_position['y'], z=base_position['z']), handCameraSpace = False,**action_dict_addition)
        success = event.metadata['lastActionSuccess']


    elif command in ['u', 'j']:
        #TODO change this everywhere
        # if base_position['h'] > 1:
        #     base_position['h'] = 1
        # elif base_position['h'] < 0:
        #     base_position['h'] = 0


        event = controller.step(action='MoveMidLevelArmHeight', y=base_position['h'],**action_dict_addition)
        action_details=dict(action='MoveMidLevelArmHeight', y=base_position['h'],**action_dict_addition)

        success = event.metadata['lastActionSuccess']

    return action_details

def get_current_arm_state(controller):
    h_min = 0.450998873
    h_max = 1.8009994
    event = controller.last_event
    joints=(event.metadata['arm']['joints'])
    arm=joints[-1]
    assert arm['name'] == 'robot_arm_4_jnt'
    xyz_dict = arm['rootRelativePosition'].copy()
    height_arm = joints[0]['position']['y']
    xyz_dict['h'] = (height_arm - h_min) / (h_max - h_min)
    #     print_error([x['position']['y'] for x in joints])
    return xyz_dict

def two_list_equal(l1, l2):
    dict1 = {i: v for (i,v) in enumerate(l1)}
    dict2 = {i: v for (i,v) in enumerate(l2)}
    return two_dict_equal(dict1, dict2)


def two_dict_equal(dict1, dict2, threshold):
    assert len(dict1) == len(dict2), print('different len', dict1, dict2)
    equal = True
    for k in dict1:
        val1 = dict1[k]
        val2 = dict2[k]
        assert type(val1) == type(val2) or (type(val1) in [int, float] and type(val2) in [int, float]), (print('different type', dict1, dict2))
        if type(val1) == dict:
            equal = two_dict_equal(val1, val2)
        elif type(val1) == list:
            equal = two_list_equal(val1, val2)
        elif val1 != val1: # Either nan or -inf
            equal = val2 != val2
        elif type(val1) == float:
            equal = abs(val1 - val2) < threshold
        else:
            equal = (val1 == val2)
        if not equal:
            print('not equal', val1, val2)
            return equal
    return equal

def reset_the_scene_and_get_reachables(scene_name=None):
    if scene_name is None:
        scene_name = random.choice(scene_names)
    controller.reset(scene_name)
    return get_reachable_positions(controller)

all_timers = []

for i in range(MAX_TESTS):
    reachable_positions = reset_the_scene_and_get_reachables()

    failed_action_pool = []

    initial_location = random.choice(reachable_positions)
    initial_rotation = random.choice([i for i in range(0, 360, 45)])
    event1 = controller.step(action='TeleportFull', x=initial_location['x'], y=initial_location['y'], z=initial_location['z'], rotation=dict(x=0, y=initial_rotation, z=0), horizon=10)
    initial_pose = dict(action='TeleportFull', x=initial_location['x'], y=initial_location['y'], z=initial_location['z'], rotation=dict(x=0, y=initial_rotation, z=0), horizon=10)
    controller.step('PausePhysicsAutoSim')
    all_commands = []
    before = datetime.datetime.now()
    for j in range(MAX_EP_LEN):
        command = random.choice(set_of_actions)
        before_action_arm_value = get_current_arm_state(controller)#.copy() #TODO this is important
        execute_command(controller, command, ADITIONAL_ARM_ARGS)
        all_commands.append(command)
        last_event_success = controller.last_event.metadata['lastActionSuccess']
        after_action_arm_value = get_current_arm_state(controller)

        if last_event_success and command in ['w','z', 'a', 's', '3', '4', 'u', 'j']:
            expected_arm_position = before_action_arm_value.copy()
            move_arm_value = ADITIONAL_ARM_ARGS['move_constant']
            if command == 'w':
                expected_arm_position['z'] += move_arm_value
            elif command == 'z':
                expected_arm_position['z'] -= move_arm_value
            elif command == 's':
                expected_arm_position['x'] += move_arm_value
            elif command == 'a':
                expected_arm_position['x'] -= move_arm_value
            elif command == '3':
                expected_arm_position['y'] += move_arm_value
            elif command == '4':
                expected_arm_position['y'] -= move_arm_value
            elif command == 'u':
                expected_arm_position['h'] += move_arm_value
            elif command == 'j':
                expected_arm_position['h'] -= move_arm_value
            # expected_arm_position['h'] = max(min(expected_arm_position['h'], 1), 0) # remove this, we want the action to fail
        else:
            expected_arm_position = before_action_arm_value.copy()

        # this means the result value is closer to the expected pose with an arm movement margin
        if not two_dict_equal(expected_arm_position, after_action_arm_value, threshold=ADITIONAL_ARM_ARGS['move_constant'] / 2):
            print('before', before_action_arm_value, '\n after', after_action_arm_value, '\n expected', expected_arm_position, '\n action', command, 'success', last_event_success)
            pdb.set_trace()

        pickupable = controller.last_event.metadata['arm']['PickupableObjectsInsideHandSphere']
        picked_up_before = controller.last_event.metadata['arm']['HeldObjects']
        if len(pickupable) > 0 and len(picked_up_before) == 0:
            cmd = 'p'
            execute_command(controller, cmd, ADITIONAL_ARM_ARGS)
            all_commands.append(cmd)
            if controller.last_event.metadata['lastActionSuccess'] is False:
                print('Failed to pick up ')
                print('scene name', controller.last_event.metadata['sceneName'])
                print('initial pose', initial_pose)
                print('list of actions', all_commands)
                break





    after = datetime.datetime.now()
    time_diff = after - before
    seconds = time_diff.total_seconds()
    all_timers.append(len(all_commands) / seconds)
    print('FPS', sum(all_timers) / len(all_timers))


