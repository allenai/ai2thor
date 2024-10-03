import json
import ai2thor.controller
import argparse
import sys
import os
from ai2thor.interact import InteractiveControllerPrompt


def load_scene(scene_name, house_path=None, run_in_editor=False, platform=None, local_build=False, commit_id=None, fov=120, distortion=False, image_dir=None, width=300, height=300):
    if image_dir is not None:
        os.makedirs(image_dir, exist_ok=True)
    if not run_in_editor:
        args = dict(
            # commit_id="13ef2aba9d0228c30d775cbae0b674f0826a97f2",
            #  commit_id="b9a854e9b7fdde450460fe054fb75ba6572e022e",
            commit_id=commit_id,
            server_class=ai2thor.fifo_server.FifoServer,
        )
        if local_build:
            del args["commit_id"]
            args["local_build"] = True
    else:
        args = dict(
            start_unity=False,
            port=8200,
            server_class=ai2thor.wsgi_server.WsgiServer,
        )

    all_args = dict(
        # local_executable_path="unity/builds/thor-OSXIntel64-local/thor-OSXIntel64-local.app/Contents/MacOS/AI2-THOR",
        # local_build=True,
        # agentMode="stretch",
        platform=platform,
        scene=scene_name,
        gridSize=0.25,
        width=width,
        height=height,
        visibilityScheme="Distance",
        renderDepthImage=True,
        renderDistortionImage=distortion,
        renderSemanticSegmentation=True,
        renderInstanceSegmentation=True,
        **args,
    )

    print("Controller args: ")
    print(all_args)

    controller = ai2thor.controller.Controller(**all_args)
    if house_path != None:
        with open(house_path, "r") as f:
            house = json.load(f)

        evt = controller.step(action="CreateHouse", house=house)

        print(f"Action {controller.last_action['action']} success: {evt.metadata['lastActionSuccess']}")
        print(f'Error: {evt.metadata["errorMessage"]}')

        agent = house["metadata"]["agent"]
        evt = controller.step(
            action="TeleportFull",
            x=agent["position"]["x"],
            y=agent["position"]["y"],
            z=agent["position"]["z"],
            rotation=agent["rotation"],
            horizon=agent["horizon"],
            standing=agent["standing"],
        )
        print(f"Action {controller.last_action['action']} success: {evt.metadata['lastActionSuccess']}")
        print(f'Error: {evt.metadata["errorMessage"]}')
    if distortion:
        controller.step(
            action="SetDistortionShaderParams",
            zoomPercent=0.46,
            k1=1.09,
            k2=1.92,
            k3=3.1,
            k4=1.8,
            intensityX=0.9,
            intensityY=0.97
        )

    xpos = dict(x=0.0, y=0.900992214679718, z=0.0786)
    sr = controller.step(
        action="Teleport", position=xpos, rotation=dict(x=0, y=0, z=0), forceAction=True
    )
    if not sr.metadata["lastActionSuccess"]:
        print(f"Error teleporting to {xpos}")
        current_pos = controller.last_event.metadata["agent"]["position"]
        print(f"Current position: {current_pos}")
    # print("Teleported to calibration room")
    # print(f"Current position: {thor_controller.last_event.metadata['agent']['position']}")
    # controller.step(
    #     {"action": "RotateCameraMount", "degrees": 13, "secondary": False}
    # )
    cam_pos = {"x": -0.1211464, "y": 0.561659, "z": 0.03892733}
    cam_rot = {"x": 13.0, "y": 0.0, "z": 0.0}
    event = controller.step(
        action="UpdateMainCamera",
        position=cam_pos,
        rotation=cam_rot,
        fieldOfView=120,
        agentId=0,
    )

    InteractiveControllerPrompt.write_image(controller.last_event, image_dir, "", semantic_segmentation_frame=True, depth_frame=True, color_frame=True, distortion_frame=distortion)
    # input()


if __name__ == "__main__":
    parser = argparse.ArgumentParser()

    parser.add_argument("--house_path", type=str, default=None)
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
    )

    parser.add_argument(
        "--output",
        type=str,
        default=None,
    )

    parser.add_argument(
        "--scene",
        type=str,
        default="FloorPlan227_physics",
    )

    parser.add_argument(
        "--width",
        type=str,
        default=300,
    )

    parser.add_argument(
        "--height",
        type=str,
        default=300,
    )

    parser.add_argument("--local_build", action="store_true", help="Uses the local build.")

    parser.add_argument("--editor", action="store_true", help="Runs in editor.")


    parser.add_argument("--distortion", action="store_true", help="Distortion.")

    parser.add_argument(
        "--fov",
        type=str,
        default="120",
        help='fov',
    )

    args = parser.parse_args(sys.argv[1:])
    print(args.distortion)
    load_scene(
        scene_name=args.scene,
        house_path=args.house_path,
        run_in_editor=args.editor,
        local_build=args.local_build,
        commit_id=args.commit_id,
        platform=args.platform,
        fov=float(args.fov),
        distortion=args.distortion,
        image_dir=args.output,
        width=float(args.width),
        height=float(args.height)
    )  # platform="CloudRendering")
