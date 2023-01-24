import copy
from collections import defaultdict
import numpy as np
import logging
import functools
import operator
from operator import itemgetter

class BenchmarkConfig:
    def __init__(
            self,
            init_params,

            scenes=None,
            procedural_houses=None,

            action_sample_count=100,
            experiment_sample_count = 100,
            filter_object_types="",
            teleport_random_before_actions=False,

            verbose=False,
            local_build=False,
            editor_mode=False,
            title = "",
            output_file="benchmark.json",

        ):
        self.init_params = init_params
        self.action_sample_count = action_sample_count
        self.experiment_sample_count = experiment_sample_count
        self.scenes = scenes
        self.procedural_houses = procedural_houses
        self.editor_mode = editor_mode
        self.output_file = output_file

        self.verbose = verbose
        self.local_build = local_build

        self.filter_object_types = filter_object_types
        self.teleport_random_before_actions = teleport_random_before_actions
        self.title = title

class ActionsPerSecondBenchmarker():
    def __init__(self):
        self.__aggregate_key = "average_frametime"
        self.__atomic_key = "frametime"
        self.__transformed_key = "actions_per_second"
        pass

    def name(self):
        return "Actions Per Second"

    def benchmark(self, env, action_config, report_add_key_values={}):
        start = time.time()
        env.step(dict(action=action_config["action"], **action_config["args"]))
        end = time.time()
        frame_time = end - start

        record = {
                "action": action_config["action"],
                "count": 1,
                [self.__aggregate_key]: frame_time
            }
        record = {**record, **add_key_values}

        return record

    # def benchmark(self, env, action_group_name, action_group, add_key_values={}):
    #     average_frametime = 0
    #
    #     # per_action_benchmark =  defaultdict(lambda: defaultdict(lambda: {"frametime": 0.0, "count": 0}))
    #     record_per_action = defaultdict(list)
    #     b = []
    #
    #     for i in range(action_group["sample_count"]):
    #         action_config = action_group["selector"](action_group["actions"])
    #         start = time.time()
    #         env.step(dict(action=action_config["action"], **action_config["args"]))
    #         end = time.time()
    #         frame_time = end - start
    #         average_frame_time += frame_time
    #         # b.append({"group": action_group_name, "action": action_config["action"], [self.__atomic_key]: frame_time})
    #
    #         record = {
    #                 "group": action_group_name,
    #                 "action": action_config["action"],
    #                 "count": 1,
    #                 [self.__aggregate_key]: frame_time
    #             }
    #         record = {**record, **add_key_values}
    #         record_per_action[action_config["action"]] += [record]
    #
    #
    #         # per_action_benchmark[action_config["action"]]["frametime"] = frame_time
    #         # per_action_benchmark[action_config["action"]]["count"] += 1
    #
    #     aggregates = [transform_aggregate(aggregate(record_per_action, a)) for a in action_group["actions"]] +\
    #                  [transform_aggregate(aggregate(record_per_action, "group"))]
    #
    #     return aggregates

    def aggregate_by(self, records, dimensions, include_dimensions=True, transform=True, aggregate_out_key=None):
        # aggregate = [report[k] for k in keys ]
        if not isinstance(dimensions, list):
            dimensions = [dimensions]

        if aggregate_out_key is None:
            aggregate_out_key = self.__aggregate_key

        grouper = itemgetter(*dimensions)

        transform = lambda x: self.transform_aggregate(x) if transform else lambda x: x

        groups = itertools.groupby(records, grouper)

        aggregated_groups = {
            [dimension]: transform({
                "count": len(slice),
                [aggregate_out_key]: np.sum(slice, lambda x: x[self.__aggregate_key]/len(slice)),
            })
            for dimension, slice in groups
        }
        # dimensions =
        # return dict(**aggregated_groups,

    def transform_aggregate(self, report):
        report[self.__transformed_key] = 1/report[self.__aggregate_key]
        return report

class UnityActionBenchmarkRunner(BenchmarkConfig):
    def __init__(self, benchmarkers, **benchmarker_kwargs):
        self.benchmarkers = benchmarkers
        super().__init__(**benchmarker_kwargs)

    def benchmark(
            self,
            action_map = {}
    ):
        import ai2thor.controller
        import random
        import platform
        import time
        from functools import reduce
        from pprint import pprint

        import os
        curr = os.path.dirname(os.path.abspath(__file__))

        def get_complete_action_dict(action_group):
            group_copy = copy.deepcopy(action_group)
            actions_copy = group_copy["actions"]

            for i in range(len(actions_copy)):
                a = actions_copy[i]
                if isinstance(a, str):
                    actions_copy[i] = {"action": a}
                if isinstance(a, dict):
                    if "action" not in a:
                        actions_copy[i] = None
                    elif "args" not in act_copy:
                        actions_copy[i]["args"] = {}

            group_copy["actions"] = [a for a in actions_copy if a is not None]
            if "sample_count" not in group_copy:
                group_copy["sample_count"] = self.action_sample_count

            default_selector = lambda x: random.choice(x)
            if isinstance(group_copy["selector"], str):
                if group_copy["selector"] == "random":
                    group_copy["selector"] = default_selector
                # TODO: potentially add more selectors
            if group_copy["selector"] is None:
                group_copy["selector"] = default_selector

            # TODO: Arg selector for potentially sending different values as arguments


            return group_copy

        if len(action_map) == 0:
            action_map = {
                "move": {"actions": ["MoveAhead", "MoveBack", "MoveLeft", "MoveRight"]},
                "rotate": {"actions": ["RotateRight", "RotateLeft"]},
                "look": {"actions": ["LookUp", "LookDown"]}
            }

        action_map = {k: get_complete_action_dict(group) for k,group in action_map.items()}

        action_map["all"] = [v for k,v in action_map.items()]

        def test_routine(env, test_actions, n=self.ex):
            average_frame_time = 0
            for i in range(n):
                action = random.choice(test_actions)
                start = time.time()
                env.step(dict(action=action))
                end = time.time()
                frame_time = end - start
                average_frame_time += frame_time

            average_frame_time = average_frame_time / float(n)
            return average_frame_time

        def benchmark_actions(env, action_name, actions):
            if self.verbose:
                print("--- Actions {}".format(actions))
            frame_time = test_routine(env, actions)
            if self.verbose:
                print("{} average: {}".format(action_name, 1 / frame_time))
            return 1 / frame_time

        procedural = False
        if self.procedural_houses:
            procedural = True

        def create_procedural_house(procedural_house):
            if procedural_house:
                if self.verbose:
                    print("Creating procedural house: ".format(procedural_house_path['id']))

                evt =env.step(
                    action="CreateHouse",
                    house=procedural_house
                )
                return evt.metadata["lastActionSuccess"]
            else:
                return False

            if self.filter_object_types != "":
                if self.filter_object_types == "*":
                    if self.verbose:
                        print("-- Filter All Objects From Metadata")
                    env.step(action="SetObjectFilter", objectIds=[])
                else:
                    types = filter_object_types.split(",")
                    evt = env.step(action="SetObjectFilterForType", objectTypes=types)
                    if self.verbose:
                        print("Filter action, Success: {}, error: {}".format(evt.metadata["lastActionSuccess"],
                                                                             evt.metadata["errorMessage"]))
            return house

        def telerport_to_random_reachable(env, house=None):

            evt = env.step(action="GetReachablePositions")

            if (
                    not evt.metadata["lastActionSuccess"] or
                    evt.metadata["actionReturn"] is None or
                    len(evt.metadata["actionReturn"]) == 0
            ) and house is not None:
                # teleport within scene for reachable positions to work
                def centroid(poly):
                    n = len(poly)
                    total = reduce(
                        lambda acc, e: {'x': acc['x'] + e['x'], 'y': acc['y'] + e['y'], 'z': acc['z'] + e['z']},
                        poly, {'x': 0, 'y': 2, 'z': 0})
                    return {'x': total['x'] / n, 'y': total['y'] / n, 'z': total['z'] / n}

                pos = {'x': 0, 'y': 2, 'z': 0}

                if house['rooms'] and len(house['rooms']) > 0:
                    poly = house['rooms'][0]['floorPolygon']
                    pos = centroid(poly)

                    print("poly center: {0}".format(pos))
                evt = env.step(
                    dict(
                        action="TeleportFull",
                        x=pos['x'],
                        y=pos['y'],
                        z=pos['z'],
                        rotation=dict(x=0, y=0, z=0),
                        horizon=0.0,
                        standing=True,
                        forceAction=True
                    )
                )
                if self.verbose:
                    print("--Teleport, " + " err: " + evt.metadata["errorMessage"])

                evt = env.step(action="GetReachablePositions")

            # print("After GetReachable AgentPos: {}".format(evt.metadata["agent"]["position"]))
            if self.verbose:
                print("-- GetReachablePositions success: {}, message: {}".format(evt.metadata["lastActionSuccess"],
                                                                                 evt.metadata["errorMessage"]))

            reachable_pos = evt.metadata["actionReturn"]

            # print(evt.metadata["actionReturn"])
            pos = random.choice(reachable_pos)
            rot = random.choice([0, 90, 180, 270])

            evt = env.step(
                dict(
                    action="TeleportFull",
                    x=pos['x'],
                    y=pos['y'],
                    z=pos['z'],
                    rotation=dict(x=0, y=rot, z=0),
                    horizon=0.0,
                    standing=True
                )
            )

        args = self.init_params
        # if self.editor_mode:
        #     args["port"] = 8200
        #     args["start_unity"] = False
        #     del args["commit_id"]
        # elif local_build:
        #     args["local_build"] = self.local_build
        #     del args["commit_id"]

        env = ai2thor.controller.Controller(
            **args
        )

        # Kitchens:       FloorPlan1 - FloorPlan30
        # Living rooms:   FloorPlan201 - FloorPlan230
        # Bedrooms:       FloorPlan301 - FloorPlan330
        # Bathrooms:      FloorPLan401 - FloorPlan430

        room_ranges = [(1, 30), (201, 230), (301, 330), (401, 430)]

        if self.scenes:
            if isinstance(scenes, list):
                scene_list = scenes
            elif isinstance(scenes, list):
                scene_list = [scenes]
        else:
            scene_list = []
            # scene_list = [["FloorPlan{}_physics".format(i) for i in range(room_range[0], room_range[1])] for room_range
            #               in room_ranges]

        procedural_json_filenames = None
        if self.procedural_houses:
            scene_list = scene_list + [("Procedural", house) for house in self.procedural_houses]


        experiment_list = [
            [[(scene, procedural_house, benchmarker, i) for benchmarker in self.benchmarkers]
                    for i in range(self.experiment_sample_count)]
                           for (scene, procedural_house) in scene_list
        ]

        experiment_list = functools.reduce(
            operator.iconcat,
            functools.reduce(operator.iconcat, experiment_list, []),
            []
        )

        benchmark_map = {"benchmarks": defaultdict(lambda: defaultdict(lambda: {})), "title": self.title,
                         "controller_params": {**args},
                         "benchmark_params": {"platform": platform.system(),
                                              "filter_object_types": self.filter_object_types,
                                              "action_sample_number": self.action_sample_count}}
        total_average_ft = 0
        scene_count = 0
        benchmarks = defaultdict(list)

        scene_benchmark = defaultdict(lambda: {})
        records = []
        for (scene, procedural_house, benchmarker, experiment_index) in scene_list:
            if self.verbose:
                print("Loading scene {}".format(scene))
            env.reset(scene)

            if self.verbose:
                print("------ {}".format(scene))

            # initial_teleport(env)
            sample_number = number_samples

            scene_average_fr = 0

            house = procedural_house
            house_id = ""
            if house != None:
                success = create_procedural_house(house)
                if not success:

                    if self.verbose:
                        print(f"Procedural house creation failed for house {house['id']}")
                    continue
                house_id = house["id"]

            record_per_action = defaultdict(list)
            # records = []
            for action_group_name, action_group in action_map.items():

                telerport_to_random_reachable(env, house)

                for i in range(action_group["sample_count"]):
                    action_config = action_group["selector"](action_group["actions"])
                    record = benchmarker.benchmark(
                        env,
                        action_config,
                        {
                            "action_group": action_group_name,
                            "house": house_id,
                            "scene": scene,
                            "experiment_index": experiment_index,
                            "benchmarker": benchmarker.name(),
                        }
                    )
                    records.append(record)

            if self.verbose:
                print("Total average frametime: {}".format(scene_average_fr))

        env.stop()

        by_benchmarker = bencharker.aggregate_by(records, "benchmarker")
        by_scene = bencharker.aggregate_by(records, ["scene", "house", "benchmarker"])
        by_action = bencharker.aggregate_by(records, ["scene", "house", "benchmarker", "action"])
        by_action_group = bencharker.aggregate_by(records, ["scene", "house", "benchmarker", "action_group"])


        house_or_scene = lambda house,scene: scene if house == "" else house
        # [{[f"{scene if house == '' else house}"]: aggregate} for ((scene, house_id, benchmarker_name, action_group), aggregate) in by_action_group.items()]


        for ((scene, house_id, benchmarker_name, action_or_group), aggregate) in dict(**by_action_group, **by_action).items():
            benchmark_map["benchmarks"][benchmarker_name][house_or_scene(scene, house_id)][action_name] = aggregate

        for ((scene, house_id, benchmarker_name), aggregate) in by_scene.items():
            for k, v in aggregate:
                benchmark_map["benchmarks"][benchmarker_name][house_or_scene(scene, house_id)][k] = v

        for benchmarker_name,aggregate in by_benchmarker.items():
            for k, v in aggregate:
                benchmark_map["benchmarks"][benchmarker_name][k] = v

        benchmark_map["average_framerate_seconds"] = total_average_ft / scene_count
        with open(out, "w") as f:
            f.write(json.dumps(benchmark_map, indent=4, sort_keys=True))

