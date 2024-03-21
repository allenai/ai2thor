import json
import ai2thor.controller
import argparse
import sys
import math


def fpin_tutorial(
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


    # Arguments to Fpin agent's Initialize 
    agentInitializationParams = dict(
        bodyAsset= {"assetId": "Toaster_5"},
        originOffsetX=0.0,
        originOffsetZ=0.0,
        colliderScaleRatio={"x":1, "y":1, "z": 1},
        useAbsoluteSize=False,
        useVisibleColliderBase=True
    )

    # Initialization params
    init_args = dict(
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

    print('Controller args: ')
    print(init_args)

    # Build controller, Initialize will be called here
    controller = ai2thor.controller.Controller(
        **init_args
    )

    print(f"Init action success: {controller.last_event.metadata['lastActionSuccess']} error: {controller.last_event.metadata['errorMessage']}")
    # Get the fpin box bounds, vector3 with the box sides lenght
    bodyBoxSides = controller.last_event.metadata["agent"]["fpinColliderSize"]
    print(bodyBoxSides)


    # Compute the desired capsule for the navmesh
    capsuleHeight = bodyBoxSides["y"]
    halfBoxX = bodyBoxSides["x"]/2.0
    halfBoxZ = bodyBoxSides["z"]/2.0

    # Navmesh ids use integers
    navMeshOverEstimateId = 0
    capsuleOverEstimateRadius = math.sqrt(halfBoxX * halfBoxX + halfBoxZ * halfBoxZ)

    navMeshUnderEstimateId = 1
    capsuleUnderEstimateRadius = min(halfBoxX, halfBoxZ)

    # build 2 nav meshes, you can build however many
    house["metadata"]["navMeshes"] = [
      {
        "id": navMeshOverEstimateId,
        "agentRadius": capsuleOverEstimateRadius,
        "agentHeight": capsuleHeight,
      },
      {
        "id": navMeshUnderEstimateId,
        "agentRadius": capsuleUnderEstimateRadius,
        "agentHeight": capsuleHeight,
      }
    ]

    # Create the house, here the navmesh is created
    evt = controller.step(action="CreateHouse", house=house)

    # Can also call reset to reset scene and create house, which builds navmesh
    # controller.reset(house)

    print(
        f"Action {controller.last_action['action']} success: {evt.metadata['lastActionSuccess']}"
    )
    print(f'Error: {evt.metadata["errorMessage"]}')

    # Teleport using new RandomlyPlaceAgentOnNavMesh
    evt = controller.step(
        action="RandomlyPlaceAgentOnNavMesh",
        n = 200 # Number of sampled points in Navmesh defaults to 200
    )
    
    
    # Teleport agent using Teleport full

    # Get a valid position, for house procthor_train_1.json this is a valid one
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

    # Move 
    controller.step(
         action = "MoveAhead",
         moveMagnitude = 0.25
    )
    controller.step(
         action = "MoveRight",
         moveMagnitude = 0.25
    )
    # Move Diagonally
    controller.step(
         action = "MoveAgent",
         ahead = 0.25,
         right = 0.25,
         speed = 1
    )

    # Moves diagonally 0.25
    controller.step(
         action = "MoveAgent",
         ahead = 0.15,
         right = 0.2,
         speed = 1
    )

    # Whatever rotateStepDegrees is from initialize, defualt 90
    controller.step(
        action = "RotateRight"
    )
    
    # + clockwise
    controller.step(
        action = "RotateAgent",
        degrees = 35
    )
     # - counter-clockwise
    controller.step(
        action = "RotateAgent",
        degrees = -35
    )
    
    print(
        f"Action {controller.last_action['action']} success: {evt.metadata['lastActionSuccess']} Error: {evt.metadata['errorMessage']}"
    )

    
    ## Change Body!
    # default change to apple
    body = {"assetId": "StretchBotSimObj"}
    # For objaverse assets loaded in unity
    if objaverse_asset_id != None and objaverse_dir != None:
        body = dict(
            dynamicAsset = {
                "id": objaverse_asset_id,
                "dir": objaverse_dir
            }
        )

    # Also alternative if you load the asset data from python of a json model you can load via
    # bodyAsset = dict(asset = <asset_json_data>),
        

    # Uses exact same parameters as agentInitializationParams sent to Initialize  
    bodyParams = dict(
        bodyAsset=body,
        originOffsetX=0.0,
        originOffsetY=-0.25,
        originOffsetZ=0.0,
        colliderScaleRatio={"x":1, "y":1, "z": 1},
        useAbsoluteSize=False,
        useVisibleColliderBase=False
    )

    # Call InitializeBody with flattened parameters
    evt = controller.step(
        action = "InitializeBody",
        **bodyParams
    )
    print(
        f"Action {controller.last_action['action']} success: {evt.metadata['lastActionSuccess']} Error: {evt.metadata['errorMessage']}"
    )
    # input()

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
    fpin_tutorial(
         house_path=args.house_path, 
         run_in_editor=args.editor, 
         local_build=args.local_build,
         commit_id=args.commit_id,
         platform=args.platform,
         objaverse_asset_id=args.objaverse_asset_id,
         objaverse_dir=args.objaverse_dir
    ) #platform="CloudRendering")
    # input()