import json
import ai2thor.controller
import argparse
import sys
import math
import os
from pathlib import Path
import prior
from ai2thor.util.runtime_assets import get_existing_thor_asset_file_path, load_existing_thor_asset_file
# os.environ["OBJAVERSE_DATA_DIR"] = "/Users/kianae/git/vida/experiment_output/objaverse_assets/"
#  from utils.constants.objaverse_data_dirs import OBJAVERSE_ASSETS_DIR, OBJAVERSE_HOUSES_DIR
# from utils.constants.stretch_initialization_utils import ProceduralAssetHookRunnerResetOnNewHouse

from ai2thor.hooks.procedural_asset_hook import ProceduralAssetHookRunner

def get_nav_mesh_config_from_box(box_body_sides, nav_mesh_id, min_side_as_radius=False):
        # This can be changed to fit specific needs
        capsuleHeight = box_body_sides["y"]
        halfBoxX = box_body_sides["x"]
        halfBoxZ = box_body_sides["z"]

        capsuleOverEstimateRadius = math.sqrt(halfBoxX * halfBoxX + halfBoxZ * halfBoxZ)
        capsuleUnderEstimateRadius = min(halfBoxX, halfBoxZ)

        return {
            "id": nav_mesh_id,
            "agentRadius": capsuleUnderEstimateRadius if min_side_as_radius else capsuleOverEstimateRadius,
            "agentHeight": capsuleHeight,
        }

def reset_test(
    house_path, 
    run_in_editor = False, 
    platform=None, 
    local_build=False, 
    commit_id=None,
    objaverse_asset_id =None,
    objaverse_dir=None
):
    if not run_in_editor:
            build_args = dict(
                commit_id=commit_id,
                server_class=ai2thor.fifo_server.FifoServer,
            )
            if local_build:
                 del build_args["commit_id"]
                 build_args["local_build"] = True
    else: 
        build_args = dict(
            start_unity=False,
            port=8200,
            server_class=ai2thor.wsgi_server.WsgiServer,
        )

    if objaverse_asset_id != None and objaverse_dir != None:
        bodyAsset = dict(
                    dynamicAsset = {
                        "id": objaverse_asset_id,
                        "dir": os.path.abspath(objaverse_dir)
                    }
                )
    else:
        bodyAsset= {"assetId": "Toaster_5"}
    # Arguments to Fpin agent's Initialize 
    agentInitializationParams = dict(
        # bodyAsset= {"assetId": "Toaster_5"},
        bodyAsset = bodyAsset
        ,
        originOffsetX=0.0,
        originOffsetZ=0.0,
        colliderScaleRatio={"x":1, "y":1, "z": 1},
        useAbsoluteSize=False,
        useVisibleColliderBase=True
    )
    

    # Initialization params
    init_params = dict(
         # local_executable_path="unity/builds/thor-OSXIntel64-local/thor-OSXIntel64-local.app/Contents/MacOS/AI2-THOR",
        # local_build=True,
        
        platform=platform,
        scene="Procedural",
        gridSize=0.25,
        width=300,
        height=300,
            
        agentMode="fpin",
        visibilityScheme="Distance",
        renderInstanceSegmentation=True,
        renderDepth=True,
        
        # New parameter to pass to agent's initializer
        agentInitializationParams=agentInitializationParams,
        **build_args
    )

    # Load house
    with open(house_path, "r") as f:
        house = json.load(f)

    if objaverse_dir != None:
        init_params["action_hook_runner"]= ProceduralAssetHookRunner(
            asset_directory=objaverse_dir,
            asset_symlink=True, 
            verbose=True, 
            asset_limit=100
        )

    print('Controller args: ')
    print(init_params)

    # Build controller, Initialize will be called here
    controller = ai2thor.controller.Controller(
        **init_params
    )

    print(f"Init action success: {controller.last_event.metadata['lastActionSuccess']} error: {controller.last_event.metadata['errorMessage']}")
    # Get the fpin box bounds, vector3 with the box sides lenght
    box_body_sides = controller.last_event.metadata["agent"]["fpinColliderSize"]
    print(f"box_body_sides: {box_body_sides}")


    
     # Navmesh ids use integers, you can pass to GetShortestPath or actions of the type to compute on that particular navmesh
    navMeshOverEstimateId = 0
    navMeshUnderEstimateId = 1

    # build 2 nav meshes, you can build however many
    house["metadata"]["navMeshes"] = [
        #  The overestimated navmesh makes RandomlyPlaceAgentOnNavMesh
        # get_nav_mesh_config_from_box(box_body_sides= box_body_sides, nav_mesh_id= navMeshOverEstimateId, min_side_as_radius= False),
        get_nav_mesh_config_from_box(box_body_sides= box_body_sides, nav_mesh_id= navMeshUnderEstimateId, min_side_as_radius= True)
    ]

    #this works
    # house["metadata"]["navMeshes"] = [{'id': 1, 'agentRadius': 0.732177734, 'agentHeight': 1.800003}]

    print(f"agent computed navmeshes: { house['metadata']['navMeshes']}")

    # this works
    # house["metadata"]["navMeshes"] = [{'id': 1, 'agentRadius': 0.5, 'agentHeight': 1}]
    eps = 0.001

    # Radius experiments for 2ac18246873f4644af4f9f48361fc599 asset
    # house["metadata"]["navMeshes"]= [{'id': 1, 'agentRadius': 0.468841553+eps, 'agentHeight': 0.3492279+eps}]
    # house["metadata"]["navMeshes"]= [{'id': 1, 'agentRadius': 0.468841553+eps, 'agentHeight': 0.3492279}]
    # house["metadata"]["navMeshes"]= [{'id': 1, 'agentRadius': 0.468841553-eps, 'agentHeight': 0.3492279}]
    # house["metadata"]["navMeshes"]= [{'id': 1, 'agentRadius': 0.47, 'agentHeight': 0.3492279}]
    # house["metadata"]["navMeshes"][0]["agentRadius"]= house["metadata"]["navMeshes"][0]["agentRadius"] - eps
    # house["metadata"]["navMeshes"][0]["agentRadius"]= house["metadata"]["navMeshes"][0]["agentRadius"] = 0.46858
    # house["metadata"]["navMeshes"][0]["agentRadius"]= house["metadata"]["navMeshes"][0]["agentRadius"] = 0.4693


    # Unity does not like the interval 0.46858 >  x  < 0.4693 for the agent radius, even though it does generate a navmesh, 
    # The most basic function  UnityEngine.AI.NavMesh.SamplePosition(...) fails with no point in navmesh with this interval and perhaps others
    # adding an epsilon fixes it, tried tracing it on NavMeshSurface.cs but NavMeshBuilder.BuildNavMeshData is closed source...
    house["metadata"]["navMeshes"][0]["agentRadius"]= house["metadata"]["navMeshes"][0]["agentRadius"] + eps

    
    
    print(f"navmeshes: { house['metadata']['navMeshes']}")

    # Create the house, here the navmesh is created
    # evt = controller.step(action="CreateHouse", house=house)
    print("before 2nd reset")
    evt = controller.reset(scene=house)
    print(
        f"Action {controller.last_action['action']} success: {evt.metadata['lastActionSuccess']} Error: {evt.metadata['errorMessage']}"
    )

    print(evt.metadata["actionReturn"])

    
    

    # controller.step(
    #         action="OverwriteNavMeshes",
    #         #  action = "ReBakeNavMeshes",
    #         #  navMeshConfigs=[navmesh_config]
    #         navMeshConfigs = [house["metadata"]["navMeshes"]]

    #     )
    # print(
    #     f"Action {controller.last_action['action']} success: {evt.metadata['lastActionSuccess']} Error: {evt.metadata['errorMessage']}"
    # )

    evt = controller.step(
        action="RandomlyPlaceAgentOnNavMesh",
        n = 1000, # Number of sampled points in Navmesh defaults to 200
        # To be added in next build
        # maxDistance = 0.2
    )

    print(
        f"Action {controller.last_action['action']} success: {evt.metadata['lastActionSuccess']} Error: {evt.metadata['errorMessage']}"
    )

    position = {"x": 7.15, "y": 0.071, "z": 2.15}
    # position = {"x": 6, "y": 0.071, "z": 10}
    rotation = {"x": 0, "y": 90, "z": 0}
    position = {"x": 5.5, "y": 0.0608115, "z": 7}
    agent = house["metadata"]["agent"]

    evt = controller.step(
        action="TeleportFull",
        x=position['x'],
        y=position['y'],
        z=position['z'],
        rotation=rotation,
        horizon=agent["horizon"],
        standing=agent["standing"]
    )

    # This fails
    evt = controller.step(
        action="GetShortestPath",
        # objectType = "Dog Bed" # Number of sampled points in Navmesh defaults to 200
        objectId="FloorLamp|5|3"
    )

    # This works
    # evt = controller.step(
    #     action="GetShortestPathToPoint",
    #     # target = {"x": 7.608288, "y": 0.003244922, "z": 6.958992},
    #     # allowedError = 0.5
    #     target = {"x": 6.8, "y": 0.0608115, "z": 1.78}

        
    # )

    print(
        f"Action {controller.last_action['action']} success: {evt.metadata['lastActionSuccess']} Error: {evt.metadata['errorMessage']}"
    )
    

    # input()


    # evt = controller.step(
    #     action="RandomlyPlaceAgentOnNavMesh",
    #     n = 1000 # Number of sampled points in Navmesh defaults to 200
    # )
    # print(
    #     f"Action {controller.last_action['action']} success: {evt.metadata['lastActionSuccess']} Error: {evt.metadata['errorMessage']}"
    # )

    return

    position = {"x": 5, "y": 0.01, "z": 6}
    rotation = {"x": 0, "y": 90, "z": 0}
    agent = house["metadata"]["agent"]

    evt = controller.step(
        action="TeleportFull",
        x=position['x'],
        y=position['y'],
        z=position['z'],
        rotation=rotation,
        horizon=agent["horizon"],
        standing=agent["standing"]
    )

    print(
        f"Action {controller.last_action['action']} success: {evt.metadata['lastActionSuccess']} Error: {evt.metadata['errorMessage']}"
    )

    print("before 3rd Reset")
    evt = controller.reset(scene=house)
    print(
        f"Action {controller.last_action['action']} success: {evt.metadata['lastActionSuccess']} Error: {evt.metadata['errorMessage']}"
    )

    evt = controller.step(
        action="TeleportFull",
        x=position['x'],
        y=position['y'],
        z=position['z'],
        rotation=rotation,
        horizon=agent["horizon"],
        standing=agent["standing"]
    )

    print(
        f"Action {controller.last_action['action']} success: {evt.metadata['lastActionSuccess']} Error: {evt.metadata['errorMessage']}"
    )

    print("before 4th Reset")
    evt = controller.reset(scene=house)
    print(
        f"Action {controller.last_action['action']} success: {evt.metadata['lastActionSuccess']} Error: {evt.metadata['errorMessage']}"
    )

    evt = controller.step(
        action="TeleportFull",
        x=position['x'],
        y=position['y'],
        z=position['z'],
        rotation=rotation,
        horizon=agent["horizon"],
        standing=agent["standing"]
    )

    print(
        f"Action {controller.last_action['action']} success: {evt.metadata['lastActionSuccess']} Error: {evt.metadata['errorMessage']}"
    )



if __name__ == "__main__":
    parser = argparse.ArgumentParser()

    parser.add_argument("--house_path", type=str, default="procthor_train_1.json", required=True)
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
        "--local_build", action="store_true", help="Uses the local build."
    )

    parser.add_argument(
        "--editor", action="store_true", help="Runs in editor."
    )

    parser.add_argument(
        "--objaverse_dir",
        type=str,
        default=None,
    )

    parser.add_argument(
        "--objaverse_asset_id",
        type=str,
        default=None,
    )


    args = parser.parse_args(sys.argv[1:])
    reset_test(
         house_path=args.house_path, 
         run_in_editor=args.editor, 
         local_build=args.local_build,
         commit_id=args.commit_id,
         platform=args.platform,
         objaverse_asset_id=args.objaverse_asset_id,
         objaverse_dir=args.objaverse_dir
    ) #platform="CloudRendering")
    # input()