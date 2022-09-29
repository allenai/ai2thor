# import pytest
import json
import os

import cv2
import numpy as np
import pytest

from ai2thor.fifo_server import FifoServer
from ai2thor.wsgi_server import WsgiServer

from .test_unity import build_controller, depth_images_near, images_near

DATA_PATH = "ai2thor/tests/data/"
IMAGE_FOLDER_PATH = os.path.join(DATA_PATH, "procedural")
SCENE = "Procedural"

shared_args = dict(
    scene="Procedural",
    gridSize=0.25,
    port=8200,
    width=300,
    height=300,
    fieldOfView=45,
    agentCount=1,
)

_wsgi_controller = dict(server_class=WsgiServer, **shared_args)
_fifo_controller = dict(server_class=FifoServer, **shared_args)

fifo_wsgi = [_fifo_controller, _wsgi_controller]
wsgi = [_wsgi_controller]
fifo = [_fifo_controller]


def create_pixel_diff_image(img, g_truth):
    dx = np.where(~np.all(g_truth == img, axis=-1))
    copy = img.copy()
    copy[dx] = (255, 0, 255)
    return copy


house_template = {
    "id": "house_0",
    "layout": """
           0 0 0 0 0 0
           0 2 2 2 2 0
           0 2 2 2 2 0
           0 1 1 1 1 0
           0 1 1 1 1 0
           0 0 0 0 0 0
        """,
    "objectsLayouts": [
        """
          0 0 0 0 0 0
          0 2 2 2 2 0
          0 2 2 2 = 0
          0 1 1 1 = 0
          0 1 1 1 + 0
          0 0 0 0 0 0
        """
    ],
    "rooms": {
        "1": {
            "wallTemplate": {
                "material": {
                    "unlit": False,
                    "color": {"r": 1.0, "g": 0.0, "b": 0.0, "a": 1.0},
                }
            },
            "floorTemplate": {
                "roomType": "Bedroom",
                "floorMaterial": {"name": "DarkWoodFloors"},
            },
            "floorYPosition": 0.0,
            "wallHeight": 3.0,
        },
        "2": {
            "wallTemplate": {
                "material": {
                    "unlit": False,
                    "color": {"r": 0.0, "g": 0.0, "b": 1.0, "a": 1.0},
                }
            },
            "floorTemplate": {
                "roomType": "LivingRoom",
                "floorMaterial": {"name": "RedBrick"},
            },
            "floorYPosition": 0.0,
            "wallHeight": 3.0,
        },
    },
    "holes": {"=": {"room0": "1", "openness": 1.0, "assetId": "Doorway_1"}},
    "objects": {"+": {"kinematic": True, "assetId": "Chair_007_1"}},
    "proceduralParameters": {
        "floorColliderThickness": 1.0,
        "receptacleHeight": 0.7,
        "skyboxId": "Sky1",
        "ceilingMaterial": {"name": "ps_mat"},
    },
    "metadata": {"schema": "1.0.0"},
}

# TODO rendering is different for fifo and wsgi server
@pytest.mark.parametrize("controller_args", fifo)
def test_render_lit(controller_args):
    print("Args")
    print(controller_args)
    controller = build_controller(**controller_args)

    rgb_filename = "proc_rgb_lit_fifo.png"
    ground_truth = cv2.imread(os.path.join(IMAGE_FOLDER_PATH, rgb_filename))

    evt = controller.step(action="GetHouseFromTemplate", template=house_template)

    print(
        "Action success {0}, message {1}".format(
            evt.metadata["lastActionSuccess"], evt.metadata["errorMessage"]
        )
    )
    assert evt.metadata["lastActionSuccess"], evt.metadata["errorMessage"]
    house = evt.metadata["actionReturn"]

    with open("test_render_lit.json", "w") as f:
        print(house)
        json.dump(house, f)

    evt = controller.step(action="CreateHouse", house=house)

    print(
        "Action success {0}, message {1}".format(
            evt.metadata["lastActionSuccess"], evt.metadata["errorMessage"]
        )
    )
    assert evt.metadata["lastActionSuccess"]

    evt = controller.step(
        action="TeleportFull",
        x=3.0,
        y=0.9010001,
        z=1.0,
        rotation=dict(x=0, y=0, z=0),
        horizon=0,
        standing=True,
        forceAction=True,
    )

    controller.stop()

    assert images_near(
        evt.cv2img, ground_truth, max_mean_pixel_diff=52, debug_save=True
    )


#
# @pytest.mark.parametrize("controller_args", wsgi)
# def test_render_lit_2(controller_args):
#     rgb_filename = "proc_rgb_lit.png"
#     ground_truth = cv2.imread(os.path.join(IMAGE_FOLDER_PATH, rgb_filename))
#     rgb_filename = "proc_rgb_lit_server.png"
#     server_image = cv2.imread(os.path.join(IMAGE_FOLDER_PATH, rgb_filename))
#     assert images_near(server_image, ground_truth, max_mean_pixel_diff=8, debug_save=True)
#
#
# @pytest.mark.parametrize("controller_args", wsgi)
# def test_render_depth_2(controller_args):
#     depth_filename = "proc_depth.npy"
#     raw_depth = np.load(os.path.join(IMAGE_FOLDER_PATH, depth_filename))
#     depth_filename = "proc_depth_server.npy"
#     server_image = np.load(os.path.join(IMAGE_FOLDER_PATH, depth_filename))
#     print("HIIII")
#     assert depth_images_near(server_image, raw_depth, epsilon=2e-1, debug_save=True)


@pytest.mark.parametrize("controller_args", fifo)
def test_depth(controller_args):
    controller_args.update(
        renderDepthImage=True,
    )

    controller = build_controller(**controller_args)

    depth_filename = "proc_depth.npy"
    raw_depth = np.load(os.path.join(IMAGE_FOLDER_PATH, depth_filename))

    evt = controller.step(action="GetHouseFromTemplate", template=house_template)

    assert evt.metadata["lastActionSuccess"], evt.metadata["errorMessage"]
    house = evt.metadata["actionReturn"]

    evt = controller.step(action="CreateHouse", house=house)

    print(
        "Action success {0}, message {1}".format(
            evt.metadata["lastActionSuccess"], evt.metadata["errorMessage"]
        )
    )
    assert evt.metadata["lastActionSuccess"]

    evt = controller.step(
        action="TeleportFull",
        x=3.0,
        y=0.9010001,
        z=1.0,
        rotation=dict(x=0, y=0, z=0),
        horizon=0,
        standing=True,
        forceAction=True,
    )

    controller.stop()
    assert depth_images_near(evt.depth_frame, raw_depth, epsilon=1e-1, debug_save=True)
