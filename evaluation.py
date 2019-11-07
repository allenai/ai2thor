import ai2thor.controller
from pprint import pprint
import json
import math
import copy


def vector_distance(v0, v1):
    dx = v0['x'] - v1['x']
    dy = v0['y'] - v1['y']
    dz = v0['z'] - v1['z']
    return math.sqrt(dx * dx + dy * dy + dz * dz)

def path_distance(path):
    distance = 0
    for i in range(0, len(path) - 1):
        distance += vector_distance(path[i], path[i+1])
    return distance


def SPL(episodes_with_golden):
    N = len(episodes_with_golden)
    eval_sum = 0.0
    for i, episode in enumerate(episodes_with_golden):
        path = episode['path']
        shortest_path = episode['shortest_path']
        Si = 1 if episode['success'] == True or episode['success'] == 1 else 0
        li = path_distance(shortest_path)
        pi = path_distance(path)
        eval_sum += Si * (li / (max(pi, li)))
    return eval_sum / N


def get_shortest_path_to_object_in_current_context(
        controller,
        object_id,
        initial_position,
        initial_rotation):
    event = controller.step(
        dict(
            action='GetShortestPath',
            objectId=object_id,
            position=initial_position,
            roatation=initial_rotation
        )
    )
    if event.metadata['lastActionSuccess']:
        return event.metadata['actionReturn']['corners']
    else:
        raise ValueError(
            "Unable to find shortest path for objectId '{}'".format(
                object_id
            )
        )


def get_episodes_with_shortest_paths(controller, initialize_func, episodes):
    episodes_with_golden = copy.deepcopy(episodes)
    for i, episode in enumerate(episodes_with_golden):
        event = controller.reset(episode['scene'])
        initialize_func()

        try:
            episode['shortest_path'] = get_shortest_path_to_object_in_current_context(
                controller,
                episode['target_object_id'],
                {
                    'x': episode['initial_position']['x'],
                    'y': episode['initial_position']['y'],
                    'z': episode['initial_position']['z']
                },
                {
                    'x': episode['initial_rotation']['x'],
                    'y': episode['initial_rotation']['y'],
                    'z': episode['initial_rotation']['z']
                }
            )
        except ValueError:
            raise ValueError(
                "Unable to find shortest path for objectId '{}' in episode '{}'".format(
                    episode['target_object_id'], json.dumps(episode, sort_keys=True, indent=4)
                )
            )
    return episodes_with_golden

def demo_SPL():
    pprint("demo")
    episodes = [
        {
            'path': [
                 {'x': 4.0, 'y': 0.02901104, 'z': -1.5},
                 {'x': 2.275, 'y': 0.02901104, 'z': -1.750001},
                 {'x': 2.1583333, 'y': 0.02901104, 'z': -1.86666679},
                 {'x': 1.8083334, 'y': 0.02901104, 'z': -3.208334},
                 {'x': 1.8083334, 'y': 0.02901104, 'z': -3.90833378},
                 {'x': 1.925, 'y': 0.02901104, 'z': -4.02500057},
                 {'x': 2.275, 'y': 0.02901104, 'z': -4.14166737},
                 {'x': 5.425, 'y': 0.02901104, 'z': -4.14166737},
                 {'x': 5.75, 'y': 0.02901104, 'z': -4.0}
            ],
            'target_object_id': 'Television|+07.63|+00.49|-03.77',
            'initial_position': {'x': 4.17, 'y': 1.02, 'z': -1.328},
            'initial_rotation': {'x': 0, 'y': 0, 'z': 0},
            'success': True,
            'scene': 'FloorPlan_Train9_2'
        }
    ]

    controller = ai2thor.controller.Controller()
    start_unity = True
    angle = 45
    controller.local_executable_path = "/Users/alvaroh/ai2/vision/ai2thor/unity/builds/thor-local-OSXIntel64.app/Contents/MacOS/thor-local-OSXIntel64"
    event = controller.start(start_unity=start_unity)
    initialize_func = lambda: controller.step(dict(action='Initialize', gridSize=0.25, fieldOfView=90, rotateStepDegrees=angle))

    episodes_with_gold = get_episodes_with_shortest_paths(controller, initialize_func, episodes)
    pprint("SPL: {}".format(episodes_with_gold))
    spl_val = SPL(episodes_with_gold)
    pprint("SPL: {}".format(spl_val))


if __name__== "__main__":
    demo_SPL()