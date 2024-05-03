import glob
import json
import os
import sys
import time

from objathor.asset_conversion.util import (
    save_thor_asset_file,
    add_default_annotations,
    change_asset_paths,
    load_existing_thor_asset_file,
    create_runtime_asset_file,
)

import ai2thor.controller
import ai2thor.wsgi_server


def make_single_object_house(
    asset_id,
    house_path,
    instance_id="asset_0",
    skybox_color=(0, 0, 0),
):
    with open(house_path, "r") as f:
        house = json.load(f)

    house["objects"] = [
        {
            "assetId": asset_id,
            "id": instance_id,
            "kinematic": True,
            "position": {"x": 0, "y": 0, "z": 0},
            "rotation": {"x": 0, "y": 0, "z": 0},
            "layer": "Procedural2",
            "material": None,
        }
    ]
    return house


def view_asset_in_thor(
    asset_id,
    controller,
    output_dir,
    house_path,
    rotations=[],
    instance_id="asset_0",
    skybox_color=(0, 0, 0),
):
    from PIL import Image

    house = make_single_object_house(
        asset_id=asset_id, house_path=house_path, instance_id=instance_id, skybox_color=skybox_color
    )

    start = time.perf_counter()
    evt = controller.step(action="CreateHouse", house=house)
    end = time.perf_counter()

    if not evt.metadata["lastActionSuccess"]:
        print(f"Action success: {evt.metadata['lastActionSuccess']}")
        print(f'Error: {evt.metadata["errorMessage"]}')
        return evt
    evt = controller.step(action="LookAtObjectCenter", objectId=instance_id)

    evt = controller.step(
        action="SetSkybox",
        color={
            "r": skybox_color[0],
            "g": skybox_color[1],
            "b": skybox_color[2],
        },
    )

    if not evt.metadata["lastActionSuccess"]:
        print(f"Action success: {evt.metadata['lastActionSuccess']}")
        print(f'Error: {evt.metadata["errorMessage"]}')

    if evt.frame is not None:
        im = Image.fromarray(evt.frame)
        # os.makedirs(output_dir, exist_ok=True)

        if not os.path.exists(output_dir):
            os.makedirs(output_dir)
        for rotation in rotations:
            evt = controller.step(
                action="RotateObject",
                angleAxisRotation={
                    "axis": {
                        "x": rotation[0],
                        "y": rotation[1],
                        "z": rotation[2],
                    },
                    "degrees": rotation[3],
                },
            )
            im = Image.fromarray(evt.frame)
            im.save(
                os.path.join(
                    output_dir, f"{rotation[0]}_{rotation[1]}_{rotation[2]}_{rotation[3]}.jpg"
                )
            )
    return evt, (end - start)


if __name__ == "__main__":
    width = 300
    height = 300
    output_dir = "./images"
    empty_house = "empty_house.json"

    extension = sys.argv[1] if len(sys.argv) > 1 else ".json"

    asset_id = "Apple_1"

    extensions = [".json", ".msgpack", ".msgpack.gz", "gz", ".pkl.gz", ""]

    controller = ai2thor.controller.Controller(
        port=8200,
        start_unity=False,
        server_class=ai2thor.wsgi_server.WsgiServer,
        # start_unity=True, local_build=True, server_class=ai2thor.fifo_server.FifoServer,
        scene="Procedural",
        gridSize=0.25,
        width=width,
        height=height,
    )
    objsverse_root = "./objaverse"
    ids = []

    dirs = glob.glob(f"{objsverse_root}/*")
    dirs = dirs[:1]

    extensions = [".pkl.gz"]
    # extensions = extensions[:1]

    for g in dirs:
        asset_id = os.path.basename(g)
        ids.append(asset_id)
        evt = controller.step(action="GetPersistentDataPath")
        print(f"return  : {evt.metadata['actionReturn']}")
        print(f"success: {evt.metadata['lastActionSuccess']}")
        print(evt.metadata["errorMessage"])

        # copy_to_dir = os.path.join(controller._build.base_dir, "processed_models")
        copy_to_dir = evt.metadata["actionReturn"]
        build_target_dir = os.path.join(copy_to_dir, asset_id)
        asset_dir = os.path.abspath(os.path.join(objsverse_root, asset_id))
        for extension in extensions:
            print(f"---- extension {extension}")
            extension = extension if extension != "" else ".json"
            load_file_in_unity = extension != "" and extension != ".pkl.gz"
            print(
                f"---- running {asset_id} wit extension {extension}, load_in_unity {load_file_in_unity}"
            )

            create_runtime_asset_file(
                asset_id=asset_id,
                asset_directory=asset_dir,
                save_dir=copy_to_dir,
                verbose=True,
                load_file_in_unity=load_file_in_unity,
                use_extension=extension,
            )

            asset = load_existing_thor_asset_file(
                build_target_dir, asset_id, force_extension=".pkl.gz"
            )
            asset = add_default_annotations(
                asset=asset, asset_directory=build_target_dir, verbose=True
            )
            asset = change_asset_paths(asset=asset, save_dir=build_target_dir)
            print(f" -- saving asset dir {build_target_dir} name {asset_id}{extension}")
            save_thor_asset_file(asset, os.path.join(build_target_dir, f"{asset_id}{extension}"))

            args = {}
            if load_file_in_unity:
                args = dict(
                    action="CreateRuntimeAsset", id=asset_id, dir=copy_to_dir, extension=extension
                )
            else:
                args = asset

            start = time.perf_counter()
            print(args)
            evt = controller.step(**args)
            end = time.perf_counter()
            frame_time = end - start
            print(f"return  : {controller.last_action}")
            print(f"return  : {evt.metadata['actionReturn']}")
            print(f"success: {evt.metadata['lastActionSuccess']}")
            print(evt.metadata["errorMessage"])

            angle_increment = 45
            angles = [n * angle_increment for n in range(0, round(360 / angle_increment))]
            axes = [(0, 1, 0), (1, 0, 0)]
            rotations = [(x, y, z, degrees) for degrees in angles for (x, y, z) in axes]

            evt, time_create_H = view_asset_in_thor(
                asset_id=asset_id,
                controller=controller,
                house_path="empty_house.json",
                rotations=rotations,
                output_dir="./out_msg",
            )

            print(f"return  : {evt.metadata['actionReturn']}")
            print(f"success: {evt.metadata['lastActionSuccess']}")
            print(evt.metadata["errorMessage"])

            print(f"{asset_id}: time Create Prefab: {frame_time} create_hoyse {time_create_H}")
            print("---reset")
            # controller.reset()

            break
        break

    input()
    # evt = controller.step(action="CreateRuntimeAsset",
    #         filepath="/Users/alvaroh/ai2/ai2thor/objaverse/b8d24c146a6844788c0ba6f7b135e99e/b8d24c146a6844788c0ba6f7b135e99e.msgpack.gz",
    #         outpath="/Users/alvaroh/ai2/ai2thor/objaverse/b8d24c146a6844788c0ba6f7b135e99e/out"
    #     )

    # print(f"error: {evt.metadata['lastActionSuccess']}")
    # print(evt.metadata["errorMessage"])
