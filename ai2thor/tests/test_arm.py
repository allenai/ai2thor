# import pytest
import json
import os
import warnings
import pytest

import copy

from ai2thor.controller import Controller

from ai2thor.fifo_server import FifoServer
from ai2thor.wsgi_server import WsgiServer

from deepdiff import DeepDiff

from .build_controller import build_controller

DATA_PATH = "ai2thor/tests/data/"
ARM_TEST_DATA_PATH = os.path.join(DATA_PATH, "arm")
SCENE = "Procedural"

shared_args = dict(
    scene="Procedural",
    gridSize=0.25,
    port=8200,
    width=300,
    height=300,
    fieldOfView=45,
    agentCount=1,
    agentMode="stretch"
)

_wsgi_controller = dict(server_class=WsgiServer, **shared_args)
_fifo_controller = dict(server_class=FifoServer, **shared_args)

fifo_wsgi = [_fifo_controller, _wsgi_controller]
wsgi = [_wsgi_controller]
fifo = [_fifo_controller]


def load_house(filename):
    house = None
    with open(os.path.join(ARM_TEST_DATA_PATH, filename)) as f:
        house = json.load(f)
    return house

def run(controller, actions_json_filename):
    actions = None
    with open(os.path.join(ARM_TEST_DATA_PATH, actions_json_filename)) as f:
        actions = json.load(f)
    metadata_returns = []
    last_event = None
    for a in actions:
        last_event = controller.step(**a)
        # metadata_returns.append(copy.deepcopy(evt.metadata))
    return last_event

@pytest.mark.parametrize("controller_args", fifo)
def test_arm_pickup_object(controller_args):
    controller = build_controller(**controller_args)
    
    house = load_house("procthor_train_1.json")

    evt = controller.step(action="CreateHouse", house=house)

    print(
        "Action success {0}, message {1}".format(
            evt.metadata["lastActionSuccess"], evt.metadata["errorMessage"]
        )
    )
    assert evt.metadata["lastActionSuccess"]

    evt = run(controller, "pickup_object.json")

    assert evt.metadata["lastActionSuccess"]

    controller.stop()

    assert ["Plate|surface|5|45"] == evt.metadata['arm']['heldObjects']


@pytest.mark.parametrize("controller_args", fifo)
def test_arm_object_intersect(controller_args):
    controller = build_controller(**controller_args)
    
    house = load_house("procthor_train_1_laptop.json")

    evt = controller.step(action="CreateHouse", house=house)

    print(
        "Action success {0}, message {1}".format(
            evt.metadata["lastActionSuccess"], evt.metadata["errorMessage"]
        )
    )
    assert evt.metadata["lastActionSuccess"]

    prev_event = run(controller, "pickup_object.json")

    evt = controller.step(**{
        "action": "RotateWristRelative",
        "yaw": 200, 
        "physicsSimulationParams": {"autoSimulation": False},
        "returnToStart": True, 
        "speed": 1
    })

    assert not evt.metadata["lastActionSuccess"]

    controller.stop()

    diff = DeepDiff(evt.metadata['arm'], prev_event.metadata['arm'], significant_digits=2, ignore_numeric_type_changes=True)
    print(diff)
    assert diff == {}


@pytest.mark.parametrize("controller_args", fifo)
def test_arm_body_object_intersect(controller_args):
    controller = build_controller(**controller_args)
    
    house = load_house("procthor_train_1.json")

    evt = controller.step(action="CreateHouse", house=house)

    print(
        "Action success {0}, message {1}".format(
            evt.metadata["lastActionSuccess"], evt.metadata["errorMessage"]
        )
    )
    assert evt.metadata["lastActionSuccess"]

    prev_event = run(controller, "pickup_plate_before_intersect.json")

    evt = controller.step(**
        {"action": "RotateWristRelative", 
         "yaw": 10, 
         "physicsSimulationParams": {"autoSimulation": False},
         "returnToStart": True, 
         "speed": 1
         }               
    )

    assert not evt.metadata["lastActionSuccess"]

    controller.stop()

    diff = DeepDiff(evt.metadata['arm'], prev_event.metadata['arm'], significant_digits=2, ignore_numeric_type_changes=True)
    print(diff)
    assert diff == {}


@pytest.mark.parametrize("controller_args", fifo)
def test_arm_pickup_drop_sequence(controller_args):
    controller = build_controller(**controller_args)
    
    house = load_house("procthor_train_1.json")

    evt = controller.step(action="CreateHouse", house=house)

    print(
        "Action success {0}, message {1}".format(
            evt.metadata["lastActionSuccess"], evt.metadata["errorMessage"]
        )
    )
    assert evt.metadata["lastActionSuccess"]

    evt = run(controller, "pickup_plate_before_intersect.json")

    assert ["Plate|surface|5|45"] == evt.metadata['arm']['heldObjects']
    
    evt = run(controller, "object_drop.json")

    assert evt.metadata['arm']['heldObjects'] == []


    object_dict = {o['name']: o for o in evt.metadata['objects']}

    plate_metadata = object_dict["Plate|surface|5|45"]

    object_target = {
        'position': {'x': 9.562429428100586, 'y': 1.0261509418487549, 'z': 0.37188154458999634}, 
        'rotation': {'x': 359.0494384765625, 'y': 28.759014129638672, 'z': 0.06256783753633499},
        'parentReceptacles': ['CounterTop|2|0']
    }

    assert {} == DeepDiff(object_target['position'], plate_metadata['position'], significant_digits=4, ignore_numeric_type_changes=True)
    assert {} == DeepDiff(object_target['rotation'], plate_metadata['rotation'], significant_digits=4, ignore_numeric_type_changes=True)
    assert plate_metadata['parentReceptacles'] == object_target['parentReceptacles']