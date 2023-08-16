from shapely.geometry import Point, Polygon

def calc_arm_movement(arm_1, arm_2):
    total_dist = 0
    for k in ["x", "y", "z"]:
        total_dist += (arm_1[k] - arm_2[k]) ** 2
    return total_dist**0.5

def get_rooms_polymap_and_type(house):
    room_poly_map = {}
    room_type_dict = {}
    # NOTE: Map the rooms
    for i, room in enumerate(house["rooms"]):
        room_poly_map[room["id"]] = Polygon([(p["x"], p["z"]) for p in room["floorPolygon"]])
        room_type_dict[room["id"]] = room["roomType"]
    return room_poly_map, room_type_dict

def get_room_id_from_location(room_polymap, position):
    point = Point(position["x"], position["z"])
    for room_id, poly in room_polymap.items():
        if poly.contains(point):
            return room_id
    print(position, "is out of house")
    return None

def sum_dist_path(path):
    total_dist = 0
    for i in range(len(path) - 1):
        total_dist += dist(path[i], path[i + 1])
    return total_dist

def dist(loc_1, loc_2):
    return (
        (loc_1["x"] - loc_2["x"]) ** 2
        + (loc_1["y"] - loc_2["y"]) ** 2
        + (loc_1["z"] - loc_2["z"]) ** 2
    ) ** 0.5

