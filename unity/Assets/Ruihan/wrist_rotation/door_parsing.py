import pdb

import numpy as np
import prior
# import ai2thor.controller
import random
import imageio

# from stretch_initialization_utils import STRETCH_ENV_ARGS
from shapely.geometry import Point, Polygon

# dataset = prior.load_dataset("procthor-10k")['train']

def filter_dataset(dataset, close_door=False):
    single_room_list = []
    for idx, data_sample in enumerate(dataset):
        if len(data_sample["rooms"]) == 2:
            for door in data_sample["doors"]:
                if "openable" not in door:
                    continue
                if "Double" in door["assetId"]:
                    continue
                if door["openable"]:
                    single_room_list.append(idx)
                    break

    single_door_dataset = []
    import copy
    for idx in single_room_list:
        data_sample = copy.deepcopy(dataset[idx])
        for i in range(len(data_sample["doors"])):
            if "openable" not in data_sample["doors"][i] or \
                not data_sample["doors"][i]["openable"]:
                data_sample["doors"].remove(data_sample["doors"][i])
                continue
            if close_door:
                data_sample["doors"][i]["openness"] = 0
        single_door_dataset.append(data_sample)
    return single_door_dataset

def get_corresponding_wall(scene, door):
    walls = []
    for wall in scene["walls"]:
        if wall["id"] == door["wall0"]:
            walls.append(wall)
            break

    for wall in scene["walls"]:
        if wall["id"] == door["wall1"]:
            walls.append(wall)
            break
    return walls

def get_corresponding_room(scene, door):
    rooms = []
    for room in scene["rooms"]:
        if room["id"] == door["room0"]:
            rooms.append(room)
            break

    for room in scene["rooms"]:
        if room["id"] == door["room1"]:
            rooms.append(room)
            break

    return rooms

def get_door_property(scene):
    door = scene["doors"][0]
    walls = get_corresponding_wall(scene, door)
    rooms = get_corresponding_room(scene, door)

    room0_polygon = Polygon([(p['x'], p['z']) for p in rooms[0]['floorPolygon']])
    room1_polygon = Polygon([(p['x'], p['z']) for p in rooms[1]['floorPolygon']])

    wall_corner = np.array((
        walls[0]["polygon"][0]['x'],
        walls[0]["polygon"][0]['z']
    ))
    wall_corner_sec = np.array((
        walls[0]["polygon"][1]['x'],
        walls[0]["polygon"][1]['z']
    ))

    direction = (wall_corner_sec - wall_corner)
    direction = direction / np.linalg.norm(direction)

    vec = np.squeeze(np.array(room1_polygon.centroid.xy) - np.array(room0_polygon.centroid.xy))

    vec_along_dir = np.sum(vec * direction) / np.linalg.norm(direction) * direction

    per_vec = (vec - vec_along_dir)
    per_vec = per_vec / np.linalg.norm(per_vec)

    door_corner = wall_corner + door["holePolygon"][0]['x'] * direction

    return {
        "door_id": door["id"],
        "door_corner": door_corner,
        "door_width": door["holePolygon"][1]["x"] - door["holePolygon"][0]["x"],
        "closed_direction": direction,
        "opened_direction": per_vec
    }
