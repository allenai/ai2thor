import argparse
import datetime
import pdb

import ai2thor.controller
import ai2thor
import random
import copy
import time
from helper_mover import get_reachable_positions, execute_command, ADITIONAL_ARM_ARGS, get_current_full_state, two_dict_equal, get_current_arm_state, reset_the_scene_and_get_reachables, GOOD_COUNTERTOPS, is_object_in_receptacle

# RESOLUTION = 900
from save_bug_sequence_util import save_failed_sequence

from helper_mover import ENV_ARGS, action_wrapper, is_object_at_position, is_agent_at_position, get_object_details

RESOLUTION = 224
MAX_TESTS = 300
MAX_EP_LEN = 20
kitchen_indices = [i + 1 for i in range(30)]
# controller.step('AdvancePhysicsStep', syncTransforms=True)
set_of_actions = ['transport', 'teleport']
kitchens = ['FloorPlan{}_physics'.format(i) for i in kitchen_indices]


def main(controller):

    all_timers = []

    for i in range(MAX_TESTS):
        print('run', i)
        successful_transport = 0
        reachable_positions = reset_the_scene_and_get_reachables(controller, scene_options=kitchens)

        all_commands = []
        all_exact_command = []

        before = datetime.datetime.now()
        for j in range(MAX_EP_LEN):
            command = random.choice(set_of_actions)
            all_commands.append(command)

            if command == 'transport':
                #get all moveable objects
                moveables = [o['objectId'] for o in controller.last_event.metadata['objects'] if o['pickupable']] #because moveable was only garbage can in room 411
                target_obj = random.choice(moveables)
                #check spawn on a counter
                possible_xyz = []
                while len(possible_xyz) == 0:
                    all_receptacles = [o for o in controller.last_event.metadata['objects'] if (o['objectType'] in GOOD_COUNTERTOPS and not o['openable'] and o['receptacle'])] #TODO change this everywhere
                    target_receptacle = random.choice(all_receptacles)['objectId']
                    event = controller.step('GetSpawnCoordinatesAboveReceptacle', objectId=target_receptacle, anywhere=True)
                    possible_xyz = event.metadata['actionReturn']
                target_location = random.choice(possible_xyz)
                #try to transport object with additional step or whatever
                transport_detail = dict(action = 'PlaceObjectAtPoint', objectId=target_obj, position=target_location)
                event, action_detail = action_wrapper(controller, transport_detail) #this returns the event before the additional step
                all_exact_command += action_detail
                if event.metadata['lastActionSuccess']:
                    successful_transport += 1
                #if success check the object pose
                #if object pose not similar report

            elif command == 'teleport':
                reachable_positions = get_reachable_positions(controller)
                initial_location = random.choice(reachable_positions)
                horizon = 10
                initial_rotation = random.choice([i for i in range(0,360,45)])
                action_detail = dict(action='TeleportFull', x=initial_location['x'], y=initial_location['y'], z=initial_location['z'], rotation=dict(x=0, y=initial_rotation, z=0), horizon=horizon, standing=True)
                event = controller.step(action_detail)
                all_exact_command.append(action_detail)

        total_result = {
            'agent': controller.last_event.metadata['agent'],
            'objects': controller.last_event.metadata['objects']
        }
        total_result = copy.deepcopy(total_result) #TODO we have to do this whenever we are working with a dict?
        for det_ind in range(10):
            reset_the_scene_and_get_reachables(controller, scene_name=controller.last_event.metadata['sceneName'])
            for action in all_exact_command:
                controller.step(action)

            final_result ={
                'agent': controller.last_event.metadata['agent'],
                'objects': controller.last_event.metadata['objects']
            }
            final_result = copy.deepcopy(final_result)
            if not two_dict_equal(final_result, total_result):
                print('Two runs are different')
                pdb.set_trace()

        after = datetime.datetime.now()
        time_diff = after - before
        seconds = time_diff.total_seconds()
        all_timers.append(len(all_commands) / seconds)
        print(successful_transport)

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

