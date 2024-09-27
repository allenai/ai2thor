import json
import ai2thor.controller
import argparse
import sys
from ai2thor.interact import InteractiveControllerPrompt

def load_scene(scene_name, house_path=None, run_in_editor=False, platform=None, local_build=False, commit_id=None):
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
        width=300,
        height=300,
        visibilityScheme="Distance",
        renderDepthImage=True,
        renderDistortionImage=True,
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
    InteractiveControllerPrompt.write_image(controller.last_event, "distortion", "", semantic_segmentation_frame=True, depth_frame=True, color_frame=True, distortion_frame=True)
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
        "--scene",
        type=str,
        default="FloorPlan227_physics",
    )

    parser.add_argument("--local_build", action="store_true", help="Uses the local build.")

    parser.add_argument("--editor", action="store_true", help="Runs in editor.")

    args = parser.parse_args(sys.argv[1:])
    load_scene(
        scene_name=args.scene,
        house_path=args.house_path,
        run_in_editor=args.editor,
        local_build=args.local_build,
        commit_id=args.commit_id,
        platform=args.platform,
    )  # platform="CloudRendering")
