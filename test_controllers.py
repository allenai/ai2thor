import ai2thor.controller
import ai2thor.robot_controller
import random
import time
import numpy as np
from pprint import pprint

fps = ["FloorPlan311"]

runs = [
    {'id': 'unity', 'port': 8200, 'controller': ai2thor.controller.Controller()},
    {'id': 'robot', 'port': 9200, 'controller': ai2thor.robot_controller.Controller()}
    #{'id': 'robot', 'port': 9000, 'controller': ai2thor.robot_controller.Controller()}
]

for run_config in runs:
    port = run_config['port']
    controller = run_config['controller']
    event = controller.start(start_unity=False, host='127.0.0.1', port=port)
    # event = controller.step({'action': 'ChangeQuality', 'quality': 'High'})
    # event = controller.step({"action": "ChangeResolution", "x": 300, "y": 300})

    for fp in fps:
        print(fp)
        for i in range(1):
            event = controller.reset(fp)
            # event = controller.step(dict(action='Initialize', gridSize=0.25, fieldOfView=90, renderObjectImage=True))
            # event = controller.step(dict(action='InitialRandomSpawn', forceVisible=True, maxNumRepeats=10, randomSeed=1))
            # event = controller.step(dict(action='MoveAhead', noise=0.02))
            event = controller.step(dict(action='RotateLeft'))
            print("event for '{}':".format(run_config['id']))
            pprint(event.metadata)
            time.sleep(1)
