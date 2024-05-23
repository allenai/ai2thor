import json
import ai2thor.controller

def create_house(
    house_path, run_in_editor = False, platform=None
):
    if not run_in_editor:
            args = dict(
                commit_id="13ef2aba9d0228c30d775cbae0b674f0826a97f2",
                server_class=ai2thor.fifo_server.FifoServer,
            )
    else: 
        args = dict(
            start_unity=False,
            port=8200,
            server_class=ai2thor.wsgi_server.WsgiServer,
        )

    controller = ai2thor.controller.Controller(
            # local_executable_path="unity/builds/thor-OSXIntel64-local/thor-OSXIntel64-local.app/Contents/MacOS/AI2-THOR",
            # local_build=True,
            agentMode="stretch",
            platform=platform,
            scene="Procedural",
            gridSize=0.25,
            width=300,
            height=300,
            visibilityScheme="Distance",
            **args
        )
    
    with open(house_path, "r") as f:
        house = json.load(f)

    evt = controller.step(action="CreateHouse", house=house)

    print(
        f"Action {controller.last_action['action']} success: {evt.metadata['lastActionSuccess']}"
    )
    print(f'Error: {evt.metadata["errorMessage"]}')

    agent = house["metadata"]["agent"]
    evt = controller.step(
        action="TeleportFull",
        x=agent["position"]["x"],
        y=agent["position"]["y"],
        z=agent["position"]["z"],
        rotation=agent["rotation"],
        horizon=agent["horizon"],
        standing=agent["standing"]
    )
    print(
        f"Action {controller.last_action['action']} success: {evt.metadata['lastActionSuccess']}"
    )
    print(f'Error: {evt.metadata["errorMessage"]}')

if __name__ == "__main__":
    create_house("/Users/alvaroh/ai2/ai2thor/procthor_train_1.json", run_in_editor=False, platform=None)