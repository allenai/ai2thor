import ai2thor.controller
import ai2thor
import random
import copy

MAX_TESTS = 300
MAX_EP_LEN = 1000
scene_names = ['FloorPlan{}_physics'.format(i + 1) for i in range(30)]
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
        if base_position['h'] > 1:
            base_position['h'] = 1
        elif base_position['h'] < 0:
            base_position['h'] = 0


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
    xyz_dict = arm['rootRelativePosition']
    height_arm = joints[0]['position']['y']
    xyz_dict['h'] = (height_arm - h_min) / (h_max - h_min)
    #     print_error([x['position']['y'] for x in joints])
    return xyz_dict

def reset_the_scene_and_get_reachables(scene_name=None):
    if scene_name is None:
        scene_name = random.choice(scene_names)
    controller.reset(scene_name)
    return get_reachable_positions(controller)



for i in range(MAX_TESTS):
    reachable_positions = reset_the_scene_and_get_reachables()

    initial_location = random.choice(reachable_positions)
    initial_rotation = random.choice([i for i in range(0, 360, 45)])
    event1 = controller.step(action='TeleportFull', x=initial_location['x'], y=initial_location['y'], z=initial_location['z'], rotation=dict(x=0, y=initial_rotation, z=0), horizon=10)
    initial_pose = dict(action='TeleportFull', x=initial_location['x'], y=initial_location['y'], z=initial_location['z'], rotation=dict(x=0, y=initial_rotation, z=0), horizon=10)
    controller.step('PausePhysicsAutoSim')
    all_commands = []
    for j in range(MAX_EP_LEN):
        command = random.choice(set_of_actions)
        execute_command(controller, command, ADITIONAL_ARM_ARGS)
        all_commands.append(command)
        last_event_success = controller.last_event.metadata['lastActionSuccess']

        if not last_event_success:
            # If last action failed Make sure it is not stuck
            all_actions = copy.copy(set_of_actions)
            random.shuffle(all_actions)
            for a in all_actions:
                execute_command(controller, a, ADITIONAL_ARM_ARGS)
                all_commands.append(a)
                if controller.last_event.metadata['lastActionSuccess']:
                    break
            if not controller.last_event.metadata['lastActionSuccess']:
                print('This means we are stuck')
                print('scene name', controller.last_event.metadata['sceneName'])
                print('initial pose', initial_pose)
                print('list of actions', all_commands)
                break



