import argparse
import datetime
import json
import pdb

import ai2thor.controller
import ai2thor
import random
import copy
import time

from helper_mover import get_reachable_positions, execute_command, ADITIONAL_ARM_ARGS, get_current_full_state, two_dict_equal

MAX_TESTS = 20
MAX_EP_LEN = 100
scene_indices = [i + 1 for i in range(30)] +[i + 1 for i in range(200,230)] +[i + 1 for i in range(300,330)] +[i + 1 for i in range(400,430)]
scene_names = ['FloorPlan{}_physics'.format(i) for i in scene_indices]
set_of_actions = ['mm', 'rr', 'll', 'w', 'z', 'a', 's', 'u', 'j', '3', '4', 'p']

controller = ai2thor.controller.Controller(
    scene=scene_names[0], gridSize=0.25,
    width=224, height=224, agentMode='arm', fieldOfView=100,
    agentControllerType='mid-level',
    server_class=ai2thor.fifo_server.FifoServer,
    useMassThreshold = True, massThreshold = 10,
)


def parse_args():
    parser = argparse.ArgumentParser(description='Data loader')
    parser.add_argument('--generate_test', default=False, action='store_true')
    args = parser.parse_args()
    return args

def reset_the_scene_and_get_reachables(scene_name=None):
    if scene_name is None:
        scene_name = random.choice(scene_names)
    controller.reset(scene_name)
    return get_reachable_positions(controller)


def random_tests():
    all_timers = []

    all_dict = {}

    for i in range(MAX_TESTS):
        print('test number', i)
        reachable_positions = reset_the_scene_and_get_reachables()

        initial_location = random.choice(reachable_positions)
        initial_rotation = random.choice([i for i in range(0, 360, 45)])
        event1 = controller.step(action='TeleportFull', x=initial_location['x'], y=initial_location['y'], z=initial_location['z'], rotation=dict(x=0, y=initial_rotation, z=0), horizon=10)
        initial_pose = dict(action='TeleportFull', x=initial_location['x'], y=initial_location['y'], z=initial_location['z'], rotation=dict(x=0, y=initial_rotation, z=0), horizon=10)
        controller.step('PausePhysicsAutoSim')
        all_commands = []
        before = datetime.datetime.now()
        for j in range(MAX_EP_LEN):
            command = random.choice(set_of_actions)
            execute_command(controller, command, ADITIONAL_ARM_ARGS)
            all_commands.append(command)
            last_event_success = controller.last_event.metadata['lastActionSuccess']

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

        final_state = get_current_full_state(controller) # made sure this does not require deep copy
        scene_name = controller.last_event.metadata['sceneName']

        #TODO only when pick up has happened
        dict_to_add = ({'initial_location': initial_location,
               'initial_rotation': initial_rotation,
               'all_commands': all_commands,
               'final_state': final_state,
               'initial_pose': initial_pose,
               'scene_name': scene_name
               })
        all_dict[len(all_dict)] = dict_to_add
        # print('FPS', sum(all_timers) / len(all_timers))
    return all_dict
def determinism_test(all_tests):
    # Redo the actions 20 times:
    # only do this if an object is picked up
    for k, test_point in all_tests.items():
        initial_location = test_point['initial_location']
        initial_rotation = test_point['initial_rotation']
        all_commands = test_point['all_commands']
        final_state = test_point['final_state']
        initial_pose = test_point['initial_pose']
        scene_name = test_point['scene_name']

        controller.reset(scene_name)
        event1 = controller.step(action='TeleportFull', x=initial_location['x'], y=initial_location['y'], z=initial_location['z'], rotation=dict(x=0, y=initial_rotation, z=0), horizon=10)
        controller.step('PausePhysicsAutoSim')
        for cmd in all_commands:
            execute_command(controller, cmd, ADITIONAL_ARM_ARGS)
            last_event_success = controller.last_event.metadata['lastActionSuccess']
        current_state = get_current_full_state(controller)
        if not two_dict_equal(final_state, current_state):
            print('not deterministic')
            print('scene name', controller.last_event.metadata['sceneName'])
            print('initial pose', initial_pose)
            print('list of actions', all_commands)
            pdb.set_trace()
        else:
            print('test {} passed'.format(k))

def test_generator():
    all_dict = random_tests()
    with open('determinism_json.json' ,'w') as f:
        json.dump(all_dict, f)

def test_from_file():
    with open('determinism_json.json' ,'r') as f:
        all_dict = json.load(f)
    determinism_test(all_dict)

if __name__ == '__main__':
    args = parse_args()
    if args.generate_test:
        test_generator()
    else:
        test_from_file()




