import copy
import math
import random
import ai2thor
import pdb

ADITIONAL_ARM_ARGS = {
    'disableRendering': True,
    'restrictMovement': False,
    'waitForFixedUpdate': False,
    'returnToStart': True,
    'speed': 1,
    'move_constant': 0.05,
}

SCENE_INDICES = [i + 1 for i in range(30)] +[i + 1 for i in range(200,230)] +[i + 1 for i in range(300,330)] +[i + 1 for i in range(400,430)]
SCENE_NAMES = ['FloorPlan{}_physics'.format(i) for i in SCENE_INDICES if i != 12] #TODO put 12 back

ENV_ARGS = dict(gridSize=0.25,
                width=224, height=224, agentMode='arm', fieldOfView=100,
                agentControllerType='mid-level',
                server_class=ai2thor.fifo_server.FifoServer,
                useMassThreshold = True, massThreshold = 10,
                autoSimulation=False, autoSyncTransforms=False #TODO Are you sure? change everywhere?
                )

def make_all_objects_unbreakable(controller):
    all_breakable_objects = [o['objectType'] for o in controller.last_event.metadata['objects'] if o['breakable'] is True]
    all_breakable_objects = set(all_breakable_objects)
    for obj_type in all_breakable_objects:
        controller.step(action='MakeObjectsOfTypeUnbreakable', objectType=obj_type)


def reset_the_scene_and_get_reachables(controller, scene_name=None):
    if scene_name is None:
        scene_name = random.choice(SCENE_NAMES)
    controller.reset(scene_name)
    #TODO change these everywhere
    controller.step('PausePhysicsAutoSim')
    controller.step(action='MakeAllObjectsMoveable')
    make_all_objects_unbreakable(controller)
    return get_reachable_positions(controller)

def get_reachable_positions(controller):
    event = controller.step('GetReachablePositions')
    reachable_positions = event.metadata['reachablePositions']
    if reachable_positions is None or len(reachable_positions) == 0:
        reachable_positions = event.metadata['actionReturn'] #TODO we have to change this everywhere
    if reachable_positions is None or len(reachable_positions) == 0:
        print('Scene name', controller.last_event.metadata['sceneName'])
        pdb.set_trace()
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
    agent_base_location = 0.9009995460510254
    event = controller.last_event
    offset = event.metadata['agent']['position']['y'] - agent_base_location
    h_max += offset
    h_min += offset
    joints=(event.metadata['arm']['joints'])
    arm=joints[-1]
    assert arm['name'] == 'robot_arm_4_jnt'
    xyz_dict = copy.deepcopy(arm['rootRelativePosition'])
    height_arm = joints[0]['position']['y']
    xyz_dict['h'] = (height_arm - h_min) / (h_max - h_min)
    #     print_error([x['position']['y'] for x in joints])
    return xyz_dict

def two_list_equal(l1, l2):
    dict1 = {i: v for (i,v) in enumerate(l1)}
    dict2 = {i: v for (i,v) in enumerate(l2)}
    return two_dict_equal(dict1, dict2)


def get_current_full_state(controller):
    return {'agent_position':controller.last_event.metadata['agent']['position'], 'agent_rotation':controller.last_event.metadata['agent']['rotation'], 'arm_state': controller.last_event.metadata['arm']['joints'], 'held_object': controller.last_event.metadata['arm']['HeldObjects']}


def two_dict_equal(dict1, dict2, threshold=0.001):
    if len(dict1) != len(dict2):
        print('different len', dict1, dict2)
        return False
    # assert len(dict1) == len(dict2), print('different len', dict1, dict2)
    equal = True
    for k in dict1:
        val1 = dict1[k]
        val2 = dict2[k]
        if not (type(val1) == type(val2) or (type(val1) in [int, float] and type(val2) in [int, float])):
            print('different type', dict1, dict2)
            return False
        # assert type(val1) == type(val2) or (type(val1) in [int, float] and type(val2) in [int, float]), ()
        if type(val1) == dict:
            equal = two_dict_equal(val1, val2)
        elif type(val1) == list:
            equal = two_list_equal(val1, val2)
        # elif val1 != val1: # Either nan or -inf
        #     equal = val2 != val2
        elif type(val1) == float:
            equal = abs(val1 - val2) < threshold
        else:
            equal = (val1 == val2)
        if not equal:
            print('not equal', val1, val2)
            return equal
    return equal

def dict_recursive_nan_check(arm_dict):
    for (k, v) in arm_dict.items():
        if type(v) == dict:
            this_item_nan = dict_recursive_nan_check(v)
        elif type(v) == float:
            this_item_nan = (v != v) or (math.isinf(v))
        elif type(v) == str or type(v) == bool:
            this_item_nan = False
        elif type(v) == list:
            this_item_nan = dict_recursive_nan_check({i:v for (i,v) in enumerate(v)})
        elif v is None:
            this_item_nan = True
            # print('nan', v)
        else:
            print(v)
            raise Exception('Not implemented')
        if this_item_nan:
            return True
    return False