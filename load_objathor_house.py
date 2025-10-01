import os
import sys
from argparse import ArgumentParser
import ai2thor.fifo_server
from ai2thor.interact import InteractiveControllerPrompt

import ai2thor
from ai2thor.controller import Controller
from ai2thor.hooks.procedural_asset_hook import WebProceduralAssetHookRunner
import json
import prior

import argparse
from random import randrange

import time
import gzip
from pathlib import Path
from timeit import default_timer as timer

import psutil
from pynvml import nvmlInit, nvmlDeviceGetHandleByIndex, nvmlDeviceGetMemoryInfo

def get_ram_usage():
    process = psutil.Process(os.getpid())
    mem_info = process.memory_info()
    return mem_info.rss / 1024 ** 2  # in MB

def get_gpu_usage(device_index=0):
    nvmlInit()
    handle = nvmlDeviceGetHandleByIndex(device_index)
    mem_info = nvmlDeviceGetMemoryInfo(handle)
    return mem_info.used / 1024 ** 2  # in MB

def measure_usage(code_block, *args, **kwargs):
    print("Measuring before code...")
    ram_before = get_ram_usage()
    try:
        gpu_before = get_gpu_usage()
    except Exception as e:
        gpu_before = None
        print("GPU measurement failed:", e)

    start_time = time.time()

    result = code_block(*args, **kwargs)

    duration = time.time() - start_time
    ram_after = get_ram_usage()
    try:
        gpu_after = get_gpu_usage()
    except:
        gpu_after = None

    print("\n=== Resource Usage ===")
    print(f"RAM used: {ram_after - ram_before:.2f} MB")
    if gpu_before is not None and gpu_after is not None:
        print(f"GPU memory used: {gpu_after - gpu_before:.2f} MB")
    print(f"Execution time: {duration:.2f} seconds\n")

    return result


parser = ArgumentParser()


# OBJAVERSE_HOUSES_DIR = os.path.abspath("./objaverse_houses/houses_2023_07_28")

def load_objaverse_houses(house_dataset_path, dataset="procthor-objaverse-internal", subset_to_load="val"):
    # max_houses_per_split = {"train": int(1e9), "val": int(1e9), "test": int(1e9)}
    max_houses_per_split = {"train": 0, "val": 0, "test": 0}
    print(house_dataset_path)

    max_houses_per_split[subset_to_load] = int(1e9)

    dataset_args = {}

    if dataset == "procthor-objaverse-internal":
        dataset_args = dict(
            revision="local",
            path_to_splits=None,
            split_to_path={k: os.path.abspath(os.path.join(house_dataset_path, f"{k}.jsonl.gz")) for k in ["train", "val", "test"]},
            max_houses_per_split=max_houses_per_split
        )
    # elif dataset == "procthor-10k":

    return prior.load_dataset(
        dataset,
        **dataset_args
    )[subset_to_load]

def save_image(name, image, flip_br=False):
    from PIL import Image
    im = Image.fromarray(image)
    im.save("{}.png".format(name))

def get_top_down_path_view(controller):
    event = controller.step({"action": "GetMapViewCameraProperties"})
    cam = event.metadata["actionReturn"].copy()
    bounds = event.metadata["sceneBounds"]["size"]
    max_bound = max(bounds["x"], bounds["z"])

    cam["fieldOfView"] = 50
    cam["position"]["y"] += 1.1 * max_bound
    cam["orthographic"] = True
    cam["farClippingPlane"] = 50
    # del cam["orthographicSize"]
    event = controller.step({"action": "AddThirdPartyCamera", "skyboxColor": "white", **cam})

    print(f" event.third_party_camera_frames len {len(event.third_party_camera_frames)}")
    return event.third_party_camera_frames[-1]


def objathor_houses(
        assets_url,
        assets_output_path,
        assets_version,
        house_path=None, 
        house_dataset="",
        house_dataset_path=None,
        house_set="val", 
        house_id=None, 
        commit_id=None, 
        editor=False, 
        local_build=False,
        output_asset_dir_house_suffix=False,
        save_house=False,
        width=600,
        height=600,
        procedural_scene="Procedural",
        download_assets_in_unity=False,
        unload_unused_assets_after_creation=False,
        texture_replace_energy_threshold=None,
        texture_scale=1.0,
        unity_log_path=None
    ):
    houses = None
    house_name = os.path.basename(house_path) if house_path else f"house_{house_set}_{house_id}"
    
    assets_dir_name = assets_version if not output_asset_dir_house_suffix else f"{house_dataset}_{assets_version}_{house_name}" 

    final_asset_output_path = os.path.join(assets_output_path, assets_dir_name)
    if house_id != None and not house_path:
        houses = load_objaverse_houses(house_dataset_path=house_dataset_path, dataset=house_dataset, subset_to_load=house_set)
        print(houses)
        if not houses[house_id]:
            print(f"Error, missing house: {house_id} in dataset: {house_dataset_path}, split: {house_set}, make sure a house for that index in that dataset split exists.")
            exit(1)

    print(f"--- texture_replace_energy_threshold {texture_replace_energy_threshold}")
    action_hook_runner=WebProceduralAssetHookRunner(
        target_dir=os.path.abspath(os.path.join(final_asset_output_path, "target")), # os.path.abspath(final_asset_output_path),
        asset_directory=os.path.abspath(final_asset_output_path),
        base_url=assets_url,
        load_file_in_unity=True,
        verbose=True,
        extension=".json",
        texture_replace_energy_threshold=texture_replace_energy_threshold,
        resize_texture_settings=dict(
            albedoTextureScale= texture_scale,
            metallicTextureScale= texture_scale,
            normalTextureScale= texture_scale,
            emissionTextureScale= texture_scale
        ),
        save_material_to_asset_db=True,
        download_assets_in_unity=download_assets_in_unity,
        unload_unused_assets_after_creation=unload_unused_assets_after_creation,
        asset_limit=0
    )

    all_args = dict(
            # local_executable_path="unity/builds/thor-OSXIntel64-local/thor-OSXIntel64-local.app/Contents/MacOS/AI2-THOR",
            agentMode="stretch",
            commit_id=commit_id,
            scene=procedural_scene,
            gridSize=0.25,
            width=width,
            height=height,
            visibilityScheme="Distance",
            renderDepthImage=True,
            renderSemanticSegmentation=True,
            renderInstanceSegmentation=True,
            fieldOfView=120,
            fastActionEmit=False,
            server_class=ai2thor.fifo_server.FifoServer,
            action_hook_runner=action_hook_runner,
            unityLogFilePath=unity_log_path
        )
    if commit_id:
        all_args["commit_id"] = commit_id
        all_args["server_class"] = ai2thor.fifo_server.FifoServer
    elif local_build:
        del all_args["commit_id"]
        all_args["local_build"] = True
    elif editor:
        all_args["start_unity"]=False
        all_args["port"]=8200
        all_args["server_class"]=ai2thor.wsgi_server.WsgiServer

    print(f"Controller Args: {all_args}")

    controller = Controller(
            **all_args,
        )
    

    def run_house_load():
        start = timer()
        for idx in range(1):
            i = 0
            house = None
            if house_id != None and not house_path:
                i = int(house_id)
                print(f"{idx}: house id {i}")
                house = houses[i]
                if save_house:
                    if not os.path.exists(final_asset_output_path):
                        os.makedirs(final_asset_output_path)
                    with open(os.path.join(final_asset_output_path, f"{house_name}.json"), "w") as f:
                        json.dump(house, f)
            else:

                suffixes = Path(house_path).suffixes
                if suffixes[-1] == ".gz":
                    with gzip.open(house_path, 'rb') as f:
                        house = json.load(f)
                else:
                    with open(house_path, "r") as f:
                        house = json.load(f)
                # with open(house_path, "r") as f:
                #     house = json.load(f)
            
            print("---- called reset with house")
            controller.reset(house, procedural_scene=procedural_scene)

            controller.step(dict(
                action = "UnloadUnusedAssets"
            ))

            if "agent" in house["metadata"] and house["metadata"]["agent"] != None:
                agent = house["metadata"]["agent"]

                evt = controller.step(
                    action="TeleportFull",
                    x=agent["position"]["x"],
                    y=agent["position"]["y"],
                    z=agent["position"]["z"],
                    rotation=agent["rotation"],
                    horizon=agent["horizon"],
                    standing=agent["standing"],
                    forceAction=True,
                )
            if download_assets_in_unity:
                os.makedirs(final_asset_output_path, exist_ok=True)

            save_image(os.path.join(final_asset_output_path, house_name), get_top_down_path_view(controller=controller))
            # InteractiveControllerPrompt.write_image(controller.last_event, image_dir, suffix=f"{i}", image_per_frame=True, semantic_segmentation_frame=True, depth_frame=True, color_frame=True)
        # evt = controller.step(
        #             action="DeleteLRUFromProceduralCache", 
        #             assetLimit=0,
        #             physicsSimulationParams=dict(
        #                     autoSimulation=True
        #             )
        #         )
        
        end = timer()
        print(f"Time: {'{:.2f}'.format(end - start)} s")

        # controller.step(
        #     action="SpawnAsset",
        #     assetId="Apple_1",
        #     generatedId="fc197ac1e49043a185e064cdb7952d84_1",
        #     position=dict(x=0, y=0.0, z=0),
        #     rotation=dict(x=0, y=0, z=0),
        # )

        # house_path2 = "./simple_obja5.json"
        # suffixes = Path(house_path2).suffixes
        # if suffixes[-1] == ".gz":
        #     with gzip.open(house_path2, 'rb') as f:
        #         house = json.load(f)
        # else:
        #     with open(house_path2, "r") as f:
        #         house = json.load(f)
        # controller.reset(scene=house)

    measure_usage(run_house_load)
    # run_house_load()


if __name__ == "__main__":

    versions_to_url = {
        "2023_07_28": "https://pub-daedd7738a984186a00f2ab264d06a07.r2.dev/2023_07_28/assets/",
        "2024_03_01": "https://pub-daedd7738a984186a00f2ab264d06a07.r2.dev/2024_03_01/assets/",
        "2024-08-16": "https://pub-2619544d52bd4f35927b08d301d2aba0.r2.dev/assets/",
        "2025_06_10": "https://pub-ddc5ca49fcee4247b552f4217e910a0f.r2.dev/assets"
    }

    procedural_scenes = ['Procedural', 'Procedural_lazy']
    parser = argparse.ArgumentParser()

    # ./objaverse_houses/houses_2023_07_28
    parser.add_argument("--houses_dataset_path", type=str, default=None, help="Directory where house dataset is loaded from.")

    parser.add_argument("--house_path", type=str, default=None, help="Optional loading of a single house.")
    parser.add_argument(
        "--platform",
        type=str,
        default=None,
        help='Platform "CloudRendering", "OSXIntel64"',
    )
    parser.add_argument(
        "--commit_id",
        type=str,
        default=None,
        help="commit_id parameter to pass to Thor controller."
    )
    parser.add_argument("--local_build", action="store_true", help="Uses the local build.")

    parser.add_argument("--house_id", type=int, default=None, help="Integer index/id of house in set")

    parser.add_argument("--width", type=int, default=600, help="Pixel width of thor player")
    parser.add_argument("--height", type=int, default=600, help="Pixel height of thor player")

    parser.add_argument("--house_set", type=str, choices=['test', 'val', 'train'], default=None, help="Name of dataset set. Valid ['test', 'val', 'train']")

    parser.add_argument("--editor", action="store_true", help="Runs in editor.")

    parser.add_argument("--assets_version", type=str,choices=list(versions_to_url.keys()), default="2024_03_01", help=f"Version name of assets to use for house. Valid: {list(versions_to_url.keys())}")

    parser.add_argument("--output_assets_path", type=str, default="assets", help="Location where assets will be downloaded.")

    parser.add_argument("--save_house", action="store_true", help="Saves house in output_assets_path")

    parser.add_argument("--add_house_suffix_to_assets", action="store_true", help="Whether to add the house name to output_assets_path, to keep asset folders separate.")


    parser.add_argument("--dataset", type=str, choices=['procthor-objaverse-internal', 'procthor-10k'], default='procthor-objaverse-internal', help="Name of dataset. Valid 'procthor-objaverse-internal', 'procthor-10k'")

    parser.add_argument("--procedural_scene", type=str, choices=procedural_scenes, default=procedural_scenes[0], help=f"Procedural scene to use. Valid: {procedural_scenes}")

    parser.add_argument("--download_assets_in_unity", action="store_true", help="Whether download of assets happens in unity, this disables saving assets in `output_assets_path`.")

    parser.add_argument("--unload_unused_assets_after_creation", action="store_true", help="Whether to call UnloadUnused assets after downloading and creating them, only applies if `download_assets_in_unity` is True.")

    parser.add_argument("--texture_replace_energy_threshold", type=float, default=None, help="Energy threshold which if `'texture_replace_energy_threshold' <= '*TextureEnergyNormalized'`  will replace respective texture with respective `*RGBA` values in asset if it has them.")
    
    parser.add_argument("--texture_scale", type=float, default=1.0, help="Scale of textures, 1.0 default and meaning keep them the same, 0.5 cut them in half.")
    
    parser.add_argument("--unity_log_path", type=str, default=None, help=f"Location to save unity log file")


    args = parser.parse_args(sys.argv[1:])
    valid = True
    if args.house_id:
        valid = True
    elif args.houses_dataset_path and args.house_id and args.house_set:
        valid = True
    else:
        valid = False

    if not valid:
        print("Invalid arguments to decide which house to load, you can either provide path to house with `--house_id` or load a house index from dataset by providing"
              +"`--houses_dataset_path` `--house_set` and `--house_id`")

    objathor_houses(
        house_dataset_path=args.houses_dataset_path,
        house_dataset=args.dataset,
        assets_url=versions_to_url[args.assets_version],
        assets_output_path=args.output_assets_path,
        assets_version=args.assets_version,
        house_path=args.house_path, 
        house_id=args.house_id, 
        house_set=args.house_set, 
        editor=args.editor, 
        local_build=args.local_build, 
        output_asset_dir_house_suffix=args.add_house_suffix_to_assets,
        save_house=args.save_house,
        width=args.width,
        height=args.height,
        commit_id=args.commit_id,
        procedural_scene=args.procedural_scene,
        download_assets_in_unity=args.download_assets_in_unity,
        unload_unused_assets_after_creation=args.unload_unused_assets_after_creation,
        texture_replace_energy_threshold=args.texture_replace_energy_threshold,
        texture_scale=args.texture_scale,
        unity_log_path=args.unity_log_path
    )