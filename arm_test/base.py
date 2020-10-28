import os
import sys
import argparse
import random
import time
import boto3
import getpass
import ai2thor.controller
import uuid
import uuid
import cv2

parser = argparse.ArgumentParser(description="Thor Arm Tester")
parser.add_argument('--record-video', action='store_true')
args = parser.parse_args()

from pprint import pprint
controller = ai2thor.controller.Controller(
    scene='FloorPlan1_physics', gridSize=0.25,
    width=900, height=900, agentMode='arm', fieldOfView=100, agentControllerType='mid-level', targetFrameRate=30, fixedDeltaTime=0.02)

def upload_video(data):
    s3 = boto3.resource("s3")
    acl = 'public-read'
    key = os.path.join(sys.argv[0].split('/')[-1].split('.')[0], str(uuid.uuid4()) + ".webm")
    print("Video is available at: https://ai2-thor-exproom-arm-test.s3-us-west-2.amazonaws.com/%s" % key)
    metadata = dict(
            test=sys.argv[0].split('/')[-1].split('.')[0],
            build_url=controller.build_url()[0],
            user=getpass.getuser())
    for k,v in controller.initialization_parameters.items():
        metadata['aithorInit-%s' % k] = str(v)
    s3.Object('ai2-thor-exproom-arm-test', key).put(Body=data, ACL=acl, ContentType="video/mp4", Metadata=metadata)

def write_video(frames):
    if not args.record_video:
        return
    temp_file = str(time.time()) + "-" + str(random.randint(0, 2**32)) + "-" + str(os.getpid())  + ".webm"
    video = cv2.VideoWriter(
        temp_file,
        cv2.VideoWriter_fourcc(*'VP80'),
        30,
        (frames[0].shape[1], frames[0].shape[0])
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
    controller.step(action='TeleportFull', x=-1, y=0.9009995460510254, z=1, rotation=dict(x=0, y=180, z=0), horizon=0)
    controller.step('PausePhysicsAutoSim')
    controller.step(action='MoveMidLevelArm', disableRendering=False, position=dict(x=0.01, y=0, z=0.01), speed = 2, returnToStart = False, handCameraSpace = False)
    controller.step(action='MoveMidLevelArmHeight', disableRendering=False, y=0.9, speed = 2, returnToStart = False)

    pose = {'x': -1.0, 'y': 0.9009995460510254, 'z': 1, 'rotation': 135, 'horizon': 0}
    controller.step(action='TeleportFull', x=pose['x'], y=pose['y'], z=pose['z'], rotation=dict(x=0.0, y=pose['rotation'], z=0.0), horizon=pose['horizon'])


def execute_actions(actions, **kwargs):
    frames = []
    for a in actions:
        if a == {} or a=={'action':''}:
            continue
        for k,v in kwargs.items():
            a[k] = v

        controller.step(a)
        frames.append(controller.last_event.cv2img)
    write_video(frames)
