import ai2thor.controller
import ai2thor
import random
import copy
import datetime

from helper_mover import get_reachable_positions, execute_command, ADITIONAL_ARM_ARGS, get_current_full_state, two_dict_equal


MAX_TESTS = 300
MAX_EP_LEN = 1000
SCREEN_SIZE = 224#900
scene_indices = [i + 1 for i in range(30)] +[i + 1 for i in range(200,230)] +[i + 1 for i in range(300,330)] +[i + 1 for i in range(400,430)]
scene_names = ['FloorPlan{}_physics'.format(i) for i in scene_indices]
set_of_actions = ['mm', 'rr', 'll', 'w', 'z', 'a', 's', 'u', 'j', '3', '4', 'p']

controller = ai2thor.controller.Controller(
    scene=scene_names[0], gridSize=0.25,
    width=SCREEN_SIZE, height=SCREEN_SIZE, agentMode='arm', fieldOfView=100,
    agentControllerType='mid-level',
    server_class=ai2thor.fifo_server.FifoServer,
)

def reset_the_scene_and_get_reachables(scene_name=None):
    if scene_name is None:
        scene_name = random.choice(scene_names)
    controller.reset(scene_name)
    return get_reachable_positions(controller)

all_timers = []

for i in range(MAX_TESTS):
    reachable_positions = reset_the_scene_and_get_reachables()

    initial_location = random.choice(reachable_positions)
    initial_rotation = random.choice([i for i in range(0, 360, 45)])
    event1 = controller.step(action='TeleportFull', x=initial_location['x'], y=initial_location['y'], z=initial_location['z'], rotation=dict(x=0, y=initial_rotation, z=0), horizon=10)
    initial_pose = dict(action='TeleportFull', x=initial_location['x'], y=initial_location['y'], z=initial_location['z'], rotation=dict(x=0, y=initial_rotation, z=0), horizon=10)
    controller.step('PausePhysicsAutoSim')
    all_commands = []
    before = datetime.datetime.now()
    while len(all_commands) <= (MAX_EP_LEN):
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

    after = datetime.datetime.now()
    time_diff = after - before
    seconds = time_diff.total_seconds()
    all_timers.append(len(all_commands) / seconds)
    print('FPS', sum(all_timers) / len(all_timers))

