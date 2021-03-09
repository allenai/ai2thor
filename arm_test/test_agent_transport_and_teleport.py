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

from helper_mover import ENV_ARGS, transport_wrapper, is_object_at_position, is_agent_at_position, get_object_details

RESOLUTION = 224
MAX_TESTS = 300
MAX_EP_LEN = 200
MAX_CONSECUTIVE_FAILURE = 10
kitchen_indices = [i + 1 for i in range(30)]
# controller.step('AdvancePhysicsStep', syncTransforms=True)
# set_of_actions = ['mm', 'rr', 'll', 'w', 'z', 'a', 's', 'u', 'j', '3', '4', 'p'] + ['teleport', 'transport'] * 5
set_of_actions = ['transport', 'teleport']
kitchens = ['FloorPlan{}_physics'.format(i) for i in kitchen_indices]



def main(controller):

    transport_check = 0
    transport_fail = 0
    teleport_check = 0
    teleport_fail = 0

    all_timers = []

    for i in range(MAX_TESTS):
        reachable_positions = reset_the_scene_and_get_reachables(controller, scene_options=kitchens)

        all_commands = []
        all_exact_command = []

        # initial_location = random.choice(reachable_positions)
        # initial_rotation = random.choice([i for i in range(0, 360, 45)])
        # event1 = controller.step(action='TeleportFull', x=initial_location['x'], y=initial_location['y'], z=initial_location['z'], rotation=dict(x=0, y=initial_rotation, z=0), horizon=10, standing=True)
        # initial_pose = dict(action='TeleportFull', x=initial_location['x'], y=initial_location['y'], z=initial_location['z'], rotation=dict(x=0, y=initial_rotation, z=0), horizon=10, standing=True)
        # if event1.metadata['lastActionSuccess']:
        #     if not is_agent_at_position(controller, initial_pose):
        #         print('Agent is not at correct position')
        #         print('scene', controller.last_event.metadata['sceneName'])
        #         print('teleport', initial_pose)
        #         print('agent pose', controller.last_event.metadata['agent']['position'], controller.last_event.metadata['agent']['rotation'])
        # all_exact_command.append(initial_pose)


        # all_exact_command.append(dict(action='PausePhysicsAutoSim'))
        # all_exact_command.append(dict(action='MakeAllObjectsMoveable'))

        before = datetime.datetime.now()
        for j in range(MAX_EP_LEN):
            command = random.choice(set_of_actions)
            all_commands.append(command)
            if command in ['transport', 'teleport']:

                if command == 'transport':

                    transport_check += 1
                    #get all moveable objects
                    moveables = [o['objectId'] for o in controller.last_event.metadata['objects'] if o['pickupable']] #because moveable was only garbage can in room 411
                    target_obj = random.choice(moveables)
                    #check spawn on a counter
                    all_receptacles = [o for o in controller.last_event.metadata['objects'] if (o['objectType'] in GOOD_COUNTERTOPS and not o['openable'] and o['receptacle'])]
                    random.shuffle(all_receptacles)
                    index = 0
                    possible_xyz = []
                    while len(possible_xyz) == 0:

                        target_receptacle = (all_receptacles[index])['objectId']
                        index += 1

                        event = controller.step('GetSpawnCoordinatesAboveReceptacle', objectId=target_receptacle, anywhere=True)
                        possible_xyz = event.metadata['actionReturn']
                    target_location = random.choice(possible_xyz)
                    #try to transport object with additional step or whatever
                    # action_detail = dict(action = 'PlaceObjectAtPoint', objectId=target_obj, position=target_location)

                    event, action_detail = transport_wrapper(controller, target_obj, target_location) #this returns the event before the additional step
                    #if success check the object pose
                    #if object pose not similar report

                    if event.metadata['lastActionSuccess']:
                        #This is to check the object being at the parent receptacle in the beginning
                        if not is_object_in_receptacle(event,target_obj,target_receptacle):
                            #This is to check the object being in the parent receptacle in the end if it is not there in the beginning.
                            if not is_object_in_receptacle(controller.last_event,target_obj,target_receptacle):
                                print('Object is not at correct position')
                                print('scene', event.metadata['sceneName'])
                                print('transport', action_detail)
                                print('target receptacle', target_receptacle)
                                print('begin receptacle', event.get_object(target_obj)['parentReceptacles'])
                                print('end receptacle', controller.last_event.get_object(target_obj)['parentReceptacles'])
                                print('target obj', target_obj, 'target location', target_location, 'current_obj_location', event.get_object(target_obj)['position'])
                                pdb.set_trace()
                    else:
                        transport_fail += 1
                elif command == 'teleport':

                    teleport_check += 1
                    reachable_positions = get_reachable_positions(controller)
                    initial_location = random.choice(reachable_positions)
                    horizon = 10
                    initial_rotation = random.choice([i for i in range(0,360,45)])
                    action_detail = dict(action='TeleportFull', x=initial_location['x'], y=initial_location['y'], z=initial_location['z'], rotation=dict(x=0, y=initial_rotation, z=0), horizon=horizon, standing=True)
                    event = controller.step(action_detail)
                    if event.metadata['lastActionSuccess']:
                        if not is_agent_at_position(controller, action_detail):
                            print('Agent is not at correct position')
                            print('scene', controller.last_event.metadata['sceneName'])
                            print('teleport', action_detail)
                            print('agent pose', controller.last_event.metadata['agent']['position'], controller.last_event.metadata['agent']['rotation'])
                            pdb.set_trace()
                    else:
                        teleport_fail += 1

                all_exact_command.append(action_detail)

            else:
                output_of_command = execute_command(controller, command, ADITIONAL_ARM_ARGS)
                all_exact_command.append(output_of_command)

            last_event_success = controller.last_event.metadata['lastActionSuccess']
            after_action_arm_value = get_current_arm_state(controller)
            after_full = copy.deepcopy(controller.last_event.metadata['arm'])

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
                    print('list of actions', all_commands)
                    pdb.set_trace()
                    break



        after = datetime.datetime.now()
        time_diff = after - before
        seconds = time_diff.total_seconds()
        all_timers.append(len(all_commands) / seconds)
        print('FPS', sum(all_timers) / len(all_timers))
        print('teleport_check',teleport_check, 'teleport_fail', teleport_fail, 'transport_check', transport_check, 'transport_fail', transport_fail)

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

