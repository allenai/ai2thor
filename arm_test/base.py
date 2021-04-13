import os
import sys
import argparse
import random
import time
import boto3
import getpass
import ai2thor.controller
import ai2thor.fifo_server
import uuid
import uuid
import cv2
from tasks import _local_build_path

# ai2thor.controller.COMMIT_ID ='fd7cf8d59c5a01f5aadc7f9379b0f579e9139ace'

parser = argparse.ArgumentParser(description="Thor Arm Tester")
parser.add_argument("--record-video", action="store_true")
args = parser.parse_args()

from pprint import pprint

controller = ai2thor.controller.Controller(
    #        port=8200, start_unity=False,
    local_executable_path=_local_build_path(),
    server_class=ai2thor.fifo_server.FifoServer,
    scene="FloorPlan1_physics",
    gridSize=0.25,
    width=900,
    height=900,
    agentMode="arm",
    # fastActionEmit=True,
    # fieldOfView=100,
    agentControllerType="mid-level",
)


def upload_video(data):
    s3 = boto3.resource("s3")
    acl = "public-read"
    key = os.path.join(
        sys.argv[0].split("/")[-1].split(".")[0], str(uuid.uuid4()) + ".webm"
    )
    print(
        "Video is available at: https://ai2-thor-exproom-arm-test.s3-us-west-2.amazonaws.com/%s"
        % key
    )
    metadata = dict(
        test=sys.argv[0].split("/")[-1].split(".")[0],
        build_url=controller.build_url()[0],
        user=getpass.getuser(),
    )
    for k, v in controller.initialization_parameters.items():
        metadata["aithorInit-%s" % k] = str(v)
    s3.Object("ai2-thor-exproom-arm-test", key).put(
        Body=data, ACL=acl, ContentType="video/mp4", Metadata=metadata
    )


def write_video(frames):
    if not args.record_video:
        return
    temp_file = (
        str(time.time())
        + "-"
        + str(random.randint(0, 2 ** 32))
        + "-"
        + str(os.getpid())
        + ".webm"
    )
    video = cv2.VideoWriter(
        temp_file,
        cv2.VideoWriter_fourcc(*"VP80"),
        30,
        (frames[0].shape[1], frames[0].shape[0]),
    )
    for frame in frames:
        # assumes that the frames are RGB images. CV2 uses BGR.
        for i in range(10):
            video.write(frame)
    cv2.destroyAllWindows()
    with open(temp_file, "rb") as f:
        data = f.read()
    os.unlink(temp_file)
    upload_video(data)


def standard_pose():
    controller.step(
        action="TeleportFull",
        x=-1,
        y=0.9009995460510254,
        z=1,
        rotation=dict(x=0, y=180, z=0),
        horizon=10,
    )
    controller.step("PausePhysicsAutoSim")

    pose = {"x": -1.0, "y": 0.9009995460510254, "z": 1.0, "rotation": 0, "horizon": 10}
    controller.step(
        action="TeleportFull",
        x=pose["x"],
        y=pose["y"],
        z=pose["z"],
        rotation=dict(x=0.0, y=pose["rotation"], z=0.0),
        horizon=pose["horizon"],
    )
    controller.step(
        action="MoveMidLevelArm",
        disableRendering=False,
        position=dict(x=0.00, y=0, z=0.35),
        speed=2,
        returnToStart=False,
        handCameraSpace=False,
    )
    controller.step(
        action="MoveArmBase",
        disableRendering=False,
        y=0.8,
        speed=2,
        returnToStart=False,
    )


def execute_actions(actions, **kwargs):
    frames = []
    for a in actions:
        if a == {} or a == {"action": ""}:
            continue
        for k, v in kwargs.items():
            a[k] = v

        controller.step(a)
        print("success: %s" % controller.last_event.metadata["lastActionSuccess"])
        print("return: %s" % controller.last_event.metadata["actionReturn"])
        print(
            "position: %s" % (controller.last_event.metadata["arm"]["HandSphereCenter"])
        )
        for j in controller.last_event.metadata["arm"]["joints"]:
            rot = " ".join(map(lambda x: str(j["rotation"][x]), ["x", "y", "z", "w"]))
            print("%s %s" % (j["name"], rot))
            # print("%s %s" % (j['name'], j['position']))
        print(controller.last_event.metadata["arm"]["PickupableObjects"])
        # frames.append(controller.last_event.cv2img)


#    write_video(frames)
