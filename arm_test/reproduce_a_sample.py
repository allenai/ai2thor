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
    # Test agent transport:
    # scene_name = 'FloorPlan15_physics'
    # target_obj = 'SoapBottle|-02.04|+00.90|+03.95'
    # target_location = {'x': -1.5253443717956543, 'y': 1.0084999799728394, 'z': 3.4976742267608643}
    # target_recep = 'CounterTop|-03.38|+00.95|+03.34'
    # sink_recep = 'Sink|-01.80|+00.92|+03.66|SinkBasin'
    #
    # for i in range(100):
    #     reset_the_scene_and_get_reachables(controller, scene_name=scene_name)
    #     pdb.set_trace()
    #     event = transport_wrapper(controller, target_obj, target_location)
    #     print(controller.last_event.get_object(target_obj)['parentReceptacles'])
    #     [o for o in controller.last_event.metadata['objects'] if o['objectType'] == 'Floor']
    #     floor_name = 'Floor_f6707506'
    #     controller.last_event.get_object(target_obj)
    #     controller.last_event.get_object('Drawer|+00.71|+00.77|-00.13')
    #     controller.last_event.get_object(target_obj)
    #     event[0].get_object('Drawer|+00.71|+00.77|-00.13')

    # Test non-determinism with arm
    scene_name = 'FloorPlan221_physics'
    initial_pose = {'action': 'TeleportFull', 'x': -0.5, 'y': 0.9984914064407349, 'z': -1.5, 'rotation': {'x': 0, 'y': 180, 'z': 0}, 'horizon': 10, 'standing': True}
    list_of_actions = ['3', 'u', 'll', 'a', 'u', 'j', 'll', 's', '3', 's', 'j', 'a', '4', 'z', 's', '4', 'w', 'w', 'j', 'p', 'rr', 's', 'mm', 'u', 'a', 'p', 's', 'll', '4', 'mm', 'mm', 'u', 'j', '4', 's', 'z', 's', 'mm', 'u', 'p', 'j', 'j', 'j', 'll', '3', 'p', 'u', 'a', 'rr', 'w', 'j', 'p', 's', 'p', 'j', 'z', '3', 'u', 'w', 'z', 'll', 'z', 'w', 'w', 'll', 'll', 'j', 'rr', 'a', 'z', 'u', 'z', 'rr', '3', 'mm', 'rr', '3', 'u', 'z', 's', 'p', 'll', 'w', 'j', 'z', 'p', 'a', 'rr', 'j', 'z', '3', 'u', 's', 'rr', 'w', 'u', '3', '4', 'z', '3', '3']

    final_state = None

    for i in range(10):
        print('Try', i)
        reset_the_scene_and_get_reachables(controller, scene_name)
        event1 = controller.step(**initial_pose)
        for cmd in list_of_actions:
            execute_command(controller, cmd, ADITIONAL_ARM_ARGS)
            last_event_success = controller.last_event.metadata['lastActionSuccess']
        current_state = get_current_full_state(controller)
        if final_state is None:
            final_state = current_state

        if not two_dict_equal(final_state, current_state, ignore_keys='inHighFrictionAreas'):
            print('not deterministic')
            print('scene name', controller.last_event.metadata['sceneName'])
            print('initial pose', initial_pose)
            print('list of actions', list_of_actions)
            pdb.set_trace()
            break








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

