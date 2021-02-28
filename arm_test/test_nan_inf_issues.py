import argparse
import datetime
import pdb

import ai2thor.controller
import ai2thor
import random
import copy
import time
from helper_mover import get_reachable_positions, execute_command, ADITIONAL_ARM_ARGS, get_current_full_state, two_dict_equal, dict_recursive_nan_check, ENV_ARGS, reset_the_scene_and_get_reachables

MAX_TESTS = 3000
MAX_EP_LEN = 1000
MAX_CONSECUTIVE_FAILURE = 10
RESOLUTION=224
# scene_indices = [i + 1 for i in range(30)] #Only kitchens
scene_indices = [i + 1 for i in range(30)] +[i + 1 for i in range(200,230)] +[i + 1 for i in range(300,330)] +[i + 1 for i in range(400,430)]
scene_names = ['FloorPlan{}_physics'.format(i) for i in scene_indices]
set_of_actions = ['mm', 'rr', 'll', 'w', 'z', 'a', 's', 'u', 'j', '3', '4', 'p']

# visualization_args=dict(
#     scene=scene_names[0], gridSize=0.25,
#     width=RESOLUTION, height=RESOLUTION, agentMode='arm', fieldOfView=100,
#     agentControllerType='mid-level',
#     server_class=ai2thor.fifo_server.FifoServer,
#     useMassThreshold = True, massThreshold = 10,
# )
visualization_args = {'width': 224, 'height': 224, 'visibilityDistance': 1.0, 'fieldOfView':100, 'gridSize': 0.25, 'snapToGrid': False, 'agentMode': 'arm', 'agentControllerType': 'mid-level', 'useMassThreshold': True, 'massThreshold': 10, 'include_private_scenes': False, 'server_class': ai2thor.fifo_server.FifoServer}#, 'x_display': '0.-1'}

# controller = ai2thor.controller.Controller(**visualization_args)

def main(controller):
    all_timers = []

    for i in range(MAX_TESTS):
        reachable_positions = reset_the_scene_and_get_reachables(controller)

        failed_action_pool = []
        all_action_details = []

        initial_location = random.choice(reachable_positions)
        initial_rotation = random.choice([i for i in range(0, 360, 45)])
        event1 = controller.step(action='TeleportFull', x=initial_location['x'], y=initial_location['y'], z=initial_location['z'], rotation=dict(x=0, y=initial_rotation, z=0), horizon=10, standing=True)
        initial_pose = dict(action='TeleportFull', x=initial_location['x'], y=initial_location['y'], z=initial_location['z'], rotation=dict(x=0, y=initial_rotation, z=0), horizon=10, standing=True)

        all_commands = []
        before = datetime.datetime.now()
        for j in range(MAX_EP_LEN):

            #check if arm values are nan:
            arm_values = controller.last_event.metadata['arm']['joints']
            arm_values_dict = {i:val for (i,val) in enumerate(arm_values)}
            if dict_recursive_nan_check(arm_values_dict):
                print('Arm is nan inf', arm_values)
                print('scene name', controller.last_event.metadata['sceneName'])
                print('initial pose', initial_pose)
                print('list of actions', all_commands)
                break

            #check if agent values are nan:
            agent_values_dict = controller.last_event.metadata['agent']
            if dict_recursive_nan_check(agent_values_dict):
                print('Agent is nan inf', agent_values_dict)
                print('scene name', controller.last_event.metadata['sceneName'])
                print('initial pose', initial_pose)
                print('list of actions', all_commands)
                break


            command = random.choice(set_of_actions)
            command_detail = execute_command(controller, command, ADITIONAL_ARM_ARGS)
            all_action_details.append(command_detail)
            all_commands.append(command)
            last_event_success = controller.last_event.metadata['lastActionSuccess']

            pickupable = controller.last_event.metadata['arm']['PickupableObjectsInsideHandSphere']
            picked_up_before = controller.last_event.metadata['arm']['HeldObjects']

            #check if obj in hand values are nan:
            if len(picked_up_before) > 0:
                obj_to_look_at = picked_up_before[0]
                obj_in_hand_values_dict = [o for o in controller.last_event.metadata['objects'] if o['objectId'] == obj_to_look_at][0]
                obj_in_hand_values_dict = dict(position=obj_in_hand_values_dict['position'],rotation=obj_in_hand_values_dict['rotation'])
                if dict_recursive_nan_check(obj_in_hand_values_dict):
                    print('ObjInHand is nan inf', obj_in_hand_values_dict)
                    print('scene name', controller.last_event.metadata['sceneName'])
                    print('initial pose', initial_pose)
                    print('list of actions', all_commands)
                    break


            if len(pickupable) > 0 and len(picked_up_before) == 0:
                cmd = 'p'
                command_detail = execute_command(controller, cmd, ADITIONAL_ARM_ARGS)
                all_action_details.append(command_detail)
                all_commands.append(cmd)
                if controller.last_event.metadata['lastActionSuccess'] is False:
                    print('Failed to pick up ', pickupable)
                    print('scene name', controller.last_event.metadata['sceneName'])
                    print('initial pose', initial_pose)
                    print('list of actions', all_commands)
                    break

            if last_event_success:
                failed_action_pool = []
            if not last_event_success:
                failed_action_pool.append(1)
                if len(failed_action_pool) > MAX_CONSECUTIVE_FAILURE:
                    # If last action failed Make sure it is not stuck
                    all_actions = copy.copy(set_of_actions)
                    random.shuffle(all_actions)
                    for a in all_actions:
                        command_detail = execute_command(controller, a, ADITIONAL_ARM_ARGS)
                        all_action_details.append(command_detail)
                        all_commands.append(a)
                        if controller.last_event.metadata['lastActionSuccess']:
                            failed_action_pool = []
                            break

                    if not controller.last_event.metadata['lastActionSuccess']:
                        print('This means we are stuck')
                        print('scene name', controller.last_event.metadata['sceneName'])
                        print('initial pose', initial_pose)
                        print('list of actions', all_commands)
                        # print('action details', all_action_details)
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

