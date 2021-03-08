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
# controller.step('AdvancePhysicsStep', syncTransforms=True)
set_of_actions = ['mm', 'rr', 'll'] * 3 + ['w', 'z', 'a', 's', 'u', 'j', '3', '4', 'p']




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
            before_action_arm_value = get_current_arm_state(controller)#.copy()
            agent_before_full = copy.deepcopy(controller.last_event.metadata['agent'])
            output_of_command = execute_command(controller, command, ADITIONAL_ARM_ARGS)
            all_exact_command.append(output_of_command)
            all_commands.append(command)
            last_event_success = controller.last_event.metadata['lastActionSuccess']
            after_action_arm_value = get_current_arm_state(controller)
            agent_after_full = copy.deepcopy(controller.last_event.metadata['agent'])

            if last_event_success and command in ['mm', 'rr', 'll']:
                expected_agent_position = copy.deepcopy(agent_before_full)
                if command == 'mm':
                    #TODO This does not count the rotation, a simpler check than it should be
                    current_position = agent_after_full['position']
                    prev_position = agent_before_full['position']
                    distance = (sum([(current_position[k] - prev_position[k])**2 for k in ['x','y','z']]))**.5
                    threshold = 0.001
                    if abs(distance- 0.2) > threshold:
                        print('Agent move was different than expected')
                        print('distance', distance)
                        pdb.set_trace()

                elif command == 'rr':
                    expected_agent_position['rotation']['y'] += 45
                    expected_agent_position['rotation']['y'] = float(round(expected_agent_position['rotation']['y']))
                    expected_agent_position['rotation']['y'] %= 360
                    agent_after_full['rotation']['y'] = float(round(agent_after_full['rotation']['y']))
                    agent_after_full['rotation']['y'] %= 360
                    if not two_dict_equal(expected_agent_position, agent_after_full, threshold=0.001, ignore_keys='inHighFrictionArea'):# ADITIONAL_ARM_ARGS['move_constant'] / 2):
                        print('Agent did not rr as expected')
                        pdb.set_trace()

                elif command == 'll':
                    expected_agent_position['rotation']['y'] -= 45
                    expected_agent_position['rotation']['y'] = float(round(expected_agent_position['rotation']['y']))
                    expected_agent_position['rotation']['y'] %= 360
                    agent_after_full['rotation']['y'] = float(round(agent_after_full['rotation']['y']))
                    agent_after_full['rotation']['y'] %= 360
                    if not two_dict_equal(expected_agent_position, agent_after_full, threshold=0.001, ignore_keys='inHighFrictionArea'):# ADITIONAL_ARM_ARGS['move_constant'] / 2):
                        print('Agent did not ll as expected')
                        pdb.set_trace()


            else:
                expected_agent_position = copy.deepcopy(agent_before_full)
                if not two_dict_equal(expected_agent_position, agent_after_full, threshold=0.001, ignore_keys='inHighFrictionArea'):# ADITIONAL_ARM_ARGS['move_constant'] / 2):
                    print('Agent moved even though it should not have')
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

