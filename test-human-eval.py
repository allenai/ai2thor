import json
import matplotlib.pyplot as plt
import numpy as np
import math

def get_eval_metrics(filename):
    success_count = 0
    str_list = []
    with open(filename, 'r') as f:
        nav_hits = json.load(f)
        for nav_hit in nav_hits:
            it = (i for i, a in enumerate(nav_hit['actions']) if
                  a['lastAction'] == 'ObjectTypeToObjectIds')
            index = next(it)
            action_len = len(nav_hit['actions'][index:])
            real_success =nav_hit['success'] and action_len < 100
            str_list.append((nav_hit['id'], "id {} Success {} len {} success_actionlen {} {} n_move {} dist {} reset {}".format(nav_hit['id'], nav_hit['success'], action_len, nav_hit['id'], real_success, nav_hit['n_move_actions'], nav_hit['distance_moved'], nav_hit['reset_count'])))
            success_count += 1 if real_success else 0

    sorted_l = sorted(str_list, key=lambda x: x[0])
    for str_p in sorted_l:
        print(str_p)
    # print(sorted_l)
    print(success_count)
    print(success_count / len(nav_hits))


def plot_points():
    plt.axis('equal')
    target_scene = 'FloorPlan_RVal2_2'

    with open("start_points_val.json", 'r') as f:
        with open("start_points_val_hard.json", 'r') as fh:
            hard_points = json.load(fh)
            medium_points = json.load(f)
            all_points = medium_points['start_points'] + hard_points['start_points']
            json_points = [p for p in all_points if p['scene'] == target_scene]
            json_with_ids = [dict(id=i, **p) for i, p in enumerate(json_points)]
            print(json.dumps(json_with_ids, indent=4))
            grid_points = np.array([np.array([p['x'], p['y'], p['z']]) for p in json_with_ids])

            with open("points_with_id_RVal2_2.json", 'w') as fw:
                json.dump(json_with_ids, fw, indent=4, sort_keys=True)

            fig, ax = plt.subplots()
            # print(json.dumps(grid_points, indent=4))

            print(grid_points[:, 0])

            # N = 50
            # x = np.random.rand(N)
            # y = np.random.rand(N)
            # colors = np.random.rand(N)
            # area = (30 * np.random.rand(N)) ** 2  # 0 to 15 point radii
            #
            # plt.scatter(x, y, s=area, c=colors, alpha=0.5)
            # plt.scatter(grid_points[:, 0], grid_points[:, 2], color='blue')
            # plt.show()
            # plt.axis('equal')
            ax.scatter(grid_points[:, 0], grid_points[:, 2], color='red')

            for i, p in enumerate(grid_points):
                x = grid_points[i][0]
                z = grid_points[i][2]
                ax.annotate("{},{}".format(x, z), (x, z))

            fig, ax2 = plt.subplots()

            ax2.scatter(grid_points[:, 0], grid_points[:, 2], color='red')

            for i, p in enumerate(grid_points):
                x = grid_points[i][0]
                z = grid_points[i][2]
                ax2.annotate("{}".format(i), (x, z))

            plt.show()

def point_distance(p0, p1):
    x_diff = p0['x'] - p1['x']
    y_diff = p0['y'] - p1['y']
    z_diff = p0['z'] - p1['z']
    distance = math.sqrt(x_diff * x_diff + y_diff * y_diff + z_diff * z_diff)
    # print(distance)
    return distance

def closest_point():
    import sys
    with open("points_with_id_RVal2_2.json", 'r') as f:
        target_scene = 'FloorPlan_RVal2_2'
        points_with_ids = json.load(f)
        eps = 0.0001
        with open("nav_hits_raw_res.json", 'r') as f:
            nav_hits = [hit for hit in json.load(f) if hit["scene"] == "FloorPlan_RVal2_2"]
            for i, hit in enumerate(nav_hits):
                print("---- Hit {}".format(i))
                print("Loca {} ".format(json.dumps(hit['agent_start_location'])))
                sorted_points = sorted(points_with_ids,
                                       key=lambda p: point_distance(p, hit['trayectory'][0]))
                hit['id'] = sorted_points[0]['id']
                print("ID ", sorted_points[0]['id'])
                print("First {} ".format(sorted_points[0]))
                min_dist = sys.float_info.max
                id = -1
                # for point in points_with_ids:
                #     hit_x = hit['agent_start_location']['x']
                #     hit_y = hit['agent_start_location']['y']
                #     hit_z = hit['agent_start_location']['z']
                #     hit['id'] = 0
                #
                #     x_diff = point['x'] - hit_x
                #     y_diff = point['y'] - hit_y
                #     z_diff = point['z'] - hit_z
                #     distance = math.sqrt(x_diff*x_diff + y_diff*y_diff + z_diff*z_diff)
                #     print("Difference to point {}, {} ".format(point['id'], distance))
                #     if distance < min_dist:
                #         min_dist = distance
                #         id = point['id']
                #     # if (math.fabs(point['x'] - hit_x) < eps and
                #     #     math.fabs(point['y'] - hit_y) < eps and
                #     #     math.fabs(point['z'] - hit_z) < eps):
                #     #     print("ID true")
                #     #     hit['id'] = point['id']
                # hit['id'] = id
                print("--- ID {}".format(id))
        id_set = set([hit['id'] for hit in nav_hits])
        print(id_set)
        assert len(id_set) == len(nav_hits)

    with open("hits_with_ids_RVal2_2.json", 'w') as fw:
        json.dump(nav_hits, fw, indent=4, sort_keys=True)

if __name__== "__main__":
    get_eval_metrics("nav_hits_val_2_2.json")

