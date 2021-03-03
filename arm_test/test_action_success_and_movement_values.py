import argparse
import datetime
import pdb

import ai2thor.controller
import ai2thor
import random
import copy
import time
from helper_mover import get_reachable_positions, execute_command, ADITIONAL_ARM_ARGS, get_current_full_state, two_dict_equal, get_current_arm_state, reset_the_scene_and_get_reachables

# RESOLUTION = 900
from save_bug_sequence_util import save_failed_sequence

from helper_mover import ENV_ARGS

RESOLUTION = 224
MAX_TESTS = 300
MAX_EP_LEN = 2000
MAX_CONSECUTIVE_FAILURE = 10
# scene_indices = [i + 1 for i in range(30)] #Only kitchens
scene_indices = [i + 1 for i in range(30)] +[i + 1 for i in range(200,230)] +[i + 1 for i in range(300,330)] +[i + 1 for i in range(400,430)]
scene_names = ['FloorPlan{}_physics'.format(i) for i in scene_indices]
set_of_actions = ['mm', 'rr', 'll', 'w', 'z', 'a', 's', 'u', 'j', '3', '4', 'p']




def main(controller):

    all_timers = []

    for i in range(MAX_TESTS):
        reachable_positions = reset_the_scene_and_get_reachables(controller)

        failed_action_pool = []

        all_commands = []
        all_exact_command = []

        initial_location = random.choice(reachable_positions)
        initial_rotation = random.choice([i for i in range(0, 360, 45)])
        event1 = controller.step(action='TeleportFull', x=initial_location['x'], y=initial_location['y'], z=initial_location['z'], rotation=dict(x=0, y=initial_rotation, z=0), horizon=10, standing=True)
        initial_pose = dict(action='TeleportFull', x=initial_location['x'], y=initial_location['y'], z=initial_location['z'], rotation=dict(x=0, y=initial_rotation, z=0), horizon=10, standing=True)
        all_exact_command.append(initial_pose)


        # all_exact_command.append(dict(action='PausePhysicsAutoSim'))
        # all_exact_command.append(dict(action='MakeAllObjectsMoveable'))

        before = datetime.datetime.now()
        for j in range(MAX_EP_LEN):
            command = random.choice(set_of_actions)
            before_action_arm_value = get_current_arm_state(controller)#.copy() #TODO this is important
            before_full = copy.deepcopy(controller.last_event.metadata['arm'])
            output_of_command = execute_command(controller, command, ADITIONAL_ARM_ARGS)
            all_exact_command.append(output_of_command)
            all_commands.append(command)
            last_event_success = controller.last_event.metadata['lastActionSuccess']
            after_action_arm_value = get_current_arm_state(controller)
            after_full = copy.deepcopy(controller.last_event.metadata['arm'])


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
                # this means the result value is closer to the expected pose with an arm movement margin
                # if not two_dict_equal(expected_arm_position, after_action_arm_value, threshold=ADITIONAL_ARM_ARGS['move_constant']):

                    # print('Arm movement error')
                    # print('before', before_action_arm_value, '\n after', after_action_arm_value, '\n expected', expected_arm_position, '\n action', command, 'success', last_event_success)
                    # pdb.set_trace()

                if command in ['u', 'j'] and not two_dict_equal(expected_arm_position, after_action_arm_value, threshold=0.01):

                    print('Arm height movement error')
                    print('before', before_action_arm_value, '\n after', after_action_arm_value, '\n expected', expected_arm_position, '\n action', command, 'success', last_event_success)
                    pdb.set_trace()
            else:
                expected_arm_position = before_action_arm_value.copy()
                if not two_dict_equal(expected_arm_position, after_action_arm_value, threshold=0.001):# ADITIONAL_ARM_ARGS['move_constant'] / 2):
                    print('Failed action or non-arm movement errors')
                    print('before', before_action_arm_value, '\n after', after_action_arm_value, '\n expected', expected_arm_position, '\n action', command, 'success', last_event_success)
                    pdb.set_trace()


            pickupable = controller.last_event.metadata['arm']['PickupableObjectsInsideHandSphere']
            picked_up_before = controller.last_event.metadata['arm']['HeldObjects']
            if len(pickupable) > 0 and len(picked_up_before) == 0:
                cmd = 'p'
                output_of_command = execute_command(controller, cmd, ADITIONAL_ARM_ARGS)
                all_exact_command.append(output_of_command)
                all_commands.append(cmd)
                if controller.last_event.metadata['lastActionSuccess'] is False:
                    print('Failed to pick up ')
                    print('scene name', controller.last_event.metadata['sceneName'])
                    print('initial pose', initial_pose)
                    print('list of actions', all_commands)
                    break

            if last_event_success:
                failed_action_pool  = []
            if not last_event_success:
                failed_action_pool.append(1)
                if len(failed_action_pool) > MAX_CONSECUTIVE_FAILURE:
                    # If last action failed Make sure it is not stuck
                    all_actions = copy.copy(set_of_actions)
                    random.shuffle(all_actions)
                    for a in all_actions:
                        output_of_command = execute_command(controller, a, ADITIONAL_ARM_ARGS)
                        all_exact_command.append(output_of_command)
                        all_commands.append(a)

                        if controller.last_event.metadata['lastActionSuccess']:
                            break
                    if not controller.last_event.metadata['lastActionSuccess']:
                        print('This means we are stuck')
                        print('scene name', controller.last_event.metadata['sceneName'])
                        print('initial pose', initial_pose)
                        print('list of actions', all_commands)
                        save_failed_sequence(controller, sequence = all_exact_command, scene_name=controller.last_event.metadata['sceneName'])
                        break





        after = datetime.datetime.now()
        time_diff = after - before
        seconds = time_diff.total_seconds()
        all_timers.append(len(all_commands) / seconds)
        print('FPS', sum(all_timers) / len(all_timers))

def parse_args():
    parser = argparse.ArgumentParser(
        description="allenact", formatter_class=argparse.ArgumentDefaultsHelpFormatter,
    )

    parser.add_argument(
        "--commit_id", type=str, default=None,
    )
    return parser.parse_args()

if __name__ == '__main__':
    args = parse_args()
    if args.commit_id is not None:
        ENV_ARGS['commit_id'] = args.commit_id
        ENV_ARGS['scene'] = 'FloorPlan1_physics'
    controller = ai2thor.controller.Controller(
         **ENV_ARGS
    )
    print('controller build', controller._build.url)
    main(controller)

