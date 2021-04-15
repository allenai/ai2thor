from pprint import pprint
import json
import math
import copy


def vector_distance(v0, v1):
    dx = v0["x"] - v1["x"]
    dy = v0["y"] - v1["y"]
    dz = v0["z"] - v1["z"]
    return math.sqrt(dx * dx + dy * dy + dz * dz)


def path_distance(path):
    distance = 0
    for i in range(0, len(path) - 1):
        distance += vector_distance(path[i], path[i + 1])
    return distance


def compute_spl(episodes_with_golden):
    """
    Computes batch SPL from episode list
    :param episodes_with_golden:
        sequence of episode object, an episode should include the following keys:
                                 'path'
                                    the path to evaluate and 'shortest_path' the shortest path
                                    as returned by 'get_shortest_path_to_object'.
                                    Both as sequences with values of the form
                                    dict(x=float, y=float, z=float)
                                 'success' boolean, a 0 for a failed path 1 for a successful one
    :return: returns a float representing the spl
    """
    N = len(episodes_with_golden)
    eval_sum = 0.0
    for i, episode in enumerate(episodes_with_golden):
        path = episode["path"]
        shortest_path = episode["shortest_path"]
        eval_sum += compute_single_spl(path, shortest_path, episode["success"])
    return eval_sum / N


def compute_single_spl(path, shortest_path, successful_path):
    """
    Computes SPL for a path dict(x=float, y=float, z=float)
    :param path: Sequence of dict(x=float, y=float, z=float) representing the path to evaluate
    :param shortest_path: Sequence of dict(x=float, y=float, z=float) representing the shortest oath
    :param successful_path: boolean indicating if the path was successful, 0 for a failed path or 1 for a successful one
    :return:
    """
    Si = 1 if successful_path == True or successful_path == 1 else 0
    li = path_distance(shortest_path)
    pi = path_distance(path)
    if max(pi, li) > 0:
        pl_ratio = li / max(pi, li)
    else:
        pl_ratio = 1.0
    spl = Si * pl_ratio
    return spl


def get_shortest_path_to_object(
    controller, object_id, initial_position, initial_rotation=None
):
    """
    Computes the shortest path to an object from an initial position using a controller
    :param controller: agent controller
    :param object_id: string with id of the object
    :param initial_position: dict(x=float, y=float, z=float) with the desired initial rotation
    :param initial_rotation: dict(x=float, y=float, z=float) representing rotation around axes or None
    :return:
    """
    args = dict(
        action="GetShortestPath",
        objectId=object_id,
        position=initial_position,
    )
    if initial_rotation is not None:
        args["rotation"] = initial_rotation
    event = controller.step(args)
    if event.metadata["lastActionSuccess"]:
        return event.metadata["actionReturn"]["corners"]
    else:
        raise ValueError(
            "Unable to find shortest path for objectId '{}'".format(object_id)
        )


def get_shortest_path_to_object_type(
    controller, object_type, initial_position, initial_rotation=None, allowed_error=None
):
    """
    Computes the shortest path to an object from an initial position using a controller
    :param controller: agent controller
    :param object_type: string that represents the type of the object
    :param initial_position: dict(x=float, y=float, z=float) with the desired initial rotation
    :param initial_rotation: dict(x=float, y=float, z=float) representing rotation around axes or None
    :param allowed_error: a floating point number describing the total amount of error allowed in specifying
        the start and end point in the shortest path computation. This number should be non-zero to allow for
        floating point issues and can be made larger to make this method more robust to edge cases where
        ai2thor "thinks" no path exists (this comes at the cost of some added noise to the start/end positions of the
        path). Passing `None` to this argument (the default) will result in THOR choosing it to be some small value,
        note that this value will not be exactly 0 to be robust to floating point inaccuracies.

    """
    kwargs = dict(
        action="GetShortestPath",
        objectType=object_type,
        position=initial_position,
    )
    if initial_rotation is not None:
        kwargs["rotation"] = initial_rotation
    if allowed_error is not None:
        kwargs["allowedError"] = allowed_error

    event = controller.step(kwargs)
    if event.metadata["lastActionSuccess"]:
        return event.metadata["actionReturn"]["corners"]
    else:
        raise ValueError(
            "Unable to find shortest path for object type '{}' due to error '{}'.".format(
                object_type, event.metadata["errorMessage"]
            )
        )


def get_shortest_path_to_point(
    controller, initial_position, target_position, allowed_error=None
):
    """
    Computes the shortest path to a point from an initial position using an agent controller
    :param controller: agent controller
    :param initial_position: dict(x=float, y=float, z=float) with the desired initial rotation
    :param target_position: dict(x=float, y=float, z=float) representing target position
    :param allowed_error: See documentation of the `get_shortest_path_to_object_type` method.
    :return:
    """
    kwargs = dict(
        action="GetShortestPathToPoint",
        position=initial_position,
        x=target_position["x"],
        y=target_position["y"],
        z=target_position["z"],
    )
    if allowed_error is not None:
        kwargs["allowedError"] = allowed_error

    event = controller.step(kwargs)
    if event.metadata["lastActionSuccess"]:
        return event.metadata["actionReturn"]["corners"]
    else:
        raise ValueError(
            "Unable to find shortest path to point '{}'  due to error '{}'.".format(
                target_position, event.metadata["errorMessage"]
            )
        )


def get_episodes_with_shortest_paths(controller, episodes):
    """
    Computes shortest path for an episode sequence
    :param controller: agent controller
    :param episodes: sequence of episode object required fields:
                        'target_object_id' string representing the object to look for
                        'initial_position' dict(x=float, y=float, z=float) of starting position
                        'initial_rotation' dict(x=float, y=float, z=float) representing rotation
                                           around axes
    :return:
    """
    episodes_with_golden = copy.deepcopy(episodes)
    for _, episode in enumerate(episodes_with_golden):
        controller.reset(episode["scene"])

        try:
            if "target_object_id" in episode:
                episode["shortest_path"] = get_shortest_path_to_object(
                    controller,
                    episode["target_object_id"],
                    {
                        "x": episode["initial_position"]["x"],
                        "y": episode["initial_position"]["y"],
                        "z": episode["initial_position"]["z"],
                    },
                    {
                        "x": episode["initial_rotation"]["x"],
                        "y": episode["initial_rotation"]["y"],
                        "z": episode["initial_rotation"]["z"],
                    },
                )
            else:
                episode["shortest_path"] = get_shortest_path_to_object_type(
                    controller,
                    episode["target_object_type"],
                    {
                        "x": episode["initial_position"]["x"],
                        "y": episode["initial_position"]["y"],
                        "z": episode["initial_position"]["z"],
                    },
                    {
                        "x": episode["initial_rotation"]["x"],
                        "y": episode["initial_rotation"]["y"],
                        "z": episode["initial_rotation"]["z"],
                    },
                )
        except ValueError:
            raise ValueError(
                "Unable to find shortest path for objectId '{}' in episode '{}'".format(
                    episode["target_object_id"],
                    json.dumps(episode, sort_keys=True, indent=4),
                )
            )
    return episodes_with_golden
