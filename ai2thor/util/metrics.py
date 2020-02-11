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


def compute_spl(episodes_with_golden):
    """
    Computes SPL from episode list
    :param episodes_with_golden:
    :return:
    """
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


def get_shortest_path_to_object(
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


def get_episodes_with_shortest_paths(controller, episodes, initialize_func=None):
    episodes_with_golden = copy.deepcopy(episodes)
    for i, episode in enumerate(episodes_with_golden):
        event = controller.reset(episode['scene'])
        if initialize_func is not None:
            initialize_func()

        try:
            episode['shortest_path'] = get_shortest_path_to_object(
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
