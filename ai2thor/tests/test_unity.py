
#import pytest
import os
import ai2thor.controller
import glob
import re

class UnityTestController(ai2thor.controller.Controller):

    def __init__(self,**kwargs):
        base_path = os.path.normpath(os.path.join(os.path.abspath(__file__), "..", "..", "..", "unity", "builds"))
        build_path = None
        local_build_path = 'thor-local-OSXIntel64.app/Contents/MacOS/AI2-Thor'
        newest_build_time = 0
        if os.path.isfile(os.path.join(base_path, local_build_path)):
            newest_build_time = os.stat(os.path.join(base_path, 'thor-local-OSXIntel64.app')).st_mtime
            build_path = local_build_path

        for g in glob.glob(base_path + "/*"):
            if os.path.isdir(g):
                base = os.path.basename(g)
                if re.match(r'^thor-OSXIntel64-[0-9a-z]+$', base):
                    mtime = os.stat(g).st_mtime
                    if mtime > newest_build_time:
                        newest_build_time = mtime
                        build_path = os.path.join(base, base + '.app', 'Contents/MacOS/AI2-Thor')

        print("selected executable %s" % build_path)
        kwargs['local_executable_path'] = os.path.join(base_path, build_path)
        kwargs['scene'] = 'FloorPlan28'
        super().__init__(**kwargs)

    def prune_releases(self):
        pass

# Defining const classes to lessen the possibility of a misspelled key
# Review: This pattern is inconsistent with this file, but I think its a better direction
# let me know if I should revert to match the rest of the file
class Actions:
    AddThirdPartyCamera = 'AddThirdPartyCamera'
    UpdateThirdPartyCamera = 'UpdateThirdPartyCamera'

class MultiAgentMetadata:
    thirdPartyCameras = 'thirdPartyCameras'

class ThirdPartyCameraMetadata:
    position = 'position'
    rotation = 'rotation'
    fieldOfView = 'fieldOfView'

controller = UnityTestController()
controller.reset('FloorPlan28')
controller.step(dict(action='Initialize', gridSize=0.25))

def teardown_module(module):
    controller.stop()

#@pytest.fixture
#def controller():
#    return c

def assert_near(point1, point2, error_message=''):
    assert point1.keys() == point2.keys(), error_message
    for k in point1.keys():
        assert round(point1[k], 3) == round(point2[k], 3), error_message

def test_rectangle_aspect():
    controller = UnityTestController(width=600, height=300)
    controller.reset('FloorPlan28')
    event = controller.step(dict(action='Initialize', gridSize=0.25))
    assert event.frame.shape == (300, 600, 3)

def test_small_aspect():
    controller = UnityTestController(width=128, height=64)
    controller.reset('FloorPlan28')
    event = controller.step(dict(action='Initialize', gridSize=0.25))
    assert event.frame.shape == (64, 128, 3)

def test_lookdown():

    e = controller.step(dict(action='RotateLook', rotation=0, horizon=0))
    position = controller.last_event.metadata['agent']['position']
    horizon = controller.last_event.metadata['agent']['cameraHorizon']
    assert horizon == 0.0
    e = controller.step(dict(action='LookDown'))
    assert e.metadata['agent']['position'] == position
    assert round(e.metadata['agent']['cameraHorizon']) == 30
    assert e.metadata['agent']['rotation'] == dict(x=0, y=0, z=0)
    e = controller.step(dict(action='LookDown'))
    assert round(e.metadata['agent']['cameraHorizon']) == 60
    e = controller.step(dict(action='LookDown'))
    assert round(e.metadata['agent']['cameraHorizon']) == 60

def test_no_leak_params():

    action = dict(action='RotateLook', rotation=0, horizon=0)
    e = controller.step(action)
    assert 'sequenceId' not in action

def test_lookup():

    e = controller.step(dict(action='RotateLook', rotation=0, horizon=0))
    position = controller.last_event.metadata['agent']['position']
    horizon = controller.last_event.metadata['agent']['cameraHorizon']
    assert horizon == 0.0
    e = controller.step(dict(action='LookUp'))
    assert e.metadata['agent']['position'] == position
    assert e.metadata['agent']['cameraHorizon'] == -30.0
    assert e.metadata['agent']['rotation'] == dict(x=0, y=0, z=0)
    e = controller.step(dict(action='LookUp'))
    assert e.metadata['agent']['cameraHorizon'] == -30.0

def test_rotate_left():

    e = controller.step(dict(action='RotateLook', rotation=0, horizon=0))
    position = controller.last_event.metadata['agent']['position']
    rotation = controller.last_event.metadata['agent']['rotation']
    assert rotation == dict(x=0, y=0, z=0)
    horizon = controller.last_event.metadata['agent']['cameraHorizon']
    e = controller.step(dict(action='RotateLeft'))
    assert e.metadata['agent']['position'] == position
    assert e.metadata['agent']['cameraHorizon'] == horizon
    assert e.metadata['agent']['rotation']['y'] == 270.0
    assert e.metadata['agent']['rotation']['x'] == 0.0
    assert e.metadata['agent']['rotation']['z'] == 0.0


def test_add_third_party_camera():

    expectedPosition = dict(x=1.2, y=2.3, z=3.4)
    expectedRotation = dict(x=30, y=40, z=50)
    expectedFieldOfView = 45.0
    assert len(controller.last_event.metadata[MultiAgentMetadata.thirdPartyCameras]) == 0, 'there should be 0 cameras'

    e = controller.step(dict(action=Actions.AddThirdPartyCamera, position=expectedPosition, rotation=expectedRotation, fieldOfView=expectedFieldOfView))
    assert len(e.metadata[MultiAgentMetadata.thirdPartyCameras]) == 1, 'there should be 1 camera'
    camera = e.metadata[MultiAgentMetadata.thirdPartyCameras][0]
    assert_near(camera[ThirdPartyCameraMetadata.position], expectedPosition, 'initial position should have been set')
    assert_near(camera[ThirdPartyCameraMetadata.rotation], expectedRotation, 'initial rotation should have been set')
    assert camera[ThirdPartyCameraMetadata.fieldOfView] == expectedFieldOfView, 'initial fieldOfView should have been set'


def test_update_third_party_camera():

    expectedPosition = dict(x=2.2, y=3.3, z=4.4)
    expectedRotation = dict(x=10, y=20, z=30)
    expectedInitialFieldOfView = 45.0
    expectedFieldOfView2 = 55.0
    expectedFieldOfViewDefault = 90.0
    assert len(controller.last_event.metadata[MultiAgentMetadata.thirdPartyCameras]) == 1, 'there should be 1 camera'

    e = controller.step(dict(action=Actions.UpdateThirdPartyCamera, thirdPartyCameraId=0, position=expectedPosition, rotation=expectedRotation))
    camera = e.metadata[MultiAgentMetadata.thirdPartyCameras][0]
    assert_near(camera[ThirdPartyCameraMetadata.position], expectedPosition, 'position should have been updated')
    assert_near(camera[ThirdPartyCameraMetadata.rotation], expectedRotation, 'rotation should have been updated')
    assert camera[ThirdPartyCameraMetadata.fieldOfView] == expectedInitialFieldOfView, 'fieldOfView should not have changed'

    # 0 is a special case, since nullable float does not get encoded properly, we need to pass 0 as null
    e = controller.step(dict(action=Actions.UpdateThirdPartyCamera, thirdPartyCameraId=0, fieldOfView=0))
    camera = e.metadata[MultiAgentMetadata.thirdPartyCameras][0]
    assert camera[ThirdPartyCameraMetadata.fieldOfView] == expectedInitialFieldOfView, 'fieldOfView should have been updated'

    e = controller.step(dict(action=Actions.UpdateThirdPartyCamera, thirdPartyCameraId=0, fieldOfView=expectedFieldOfView2))
    camera = e.metadata[MultiAgentMetadata.thirdPartyCameras][0]
    assert camera[ThirdPartyCameraMetadata.fieldOfView] == expectedFieldOfView2, 'fieldOfView should have been updated'

    e = controller.step(dict(action=Actions.UpdateThirdPartyCamera, thirdPartyCameraId=0, fieldOfView=-1))
    camera = e.metadata[MultiAgentMetadata.thirdPartyCameras][0]
    assert camera[ThirdPartyCameraMetadata.fieldOfView] == expectedFieldOfViewDefault, 'fieldOfView should have been updated to default'

    e = controller.step(dict(action=Actions.UpdateThirdPartyCamera, thirdPartyCameraId=0, fieldOfView=181))
    camera = e.metadata[MultiAgentMetadata.thirdPartyCameras][0]
    assert camera[ThirdPartyCameraMetadata.fieldOfView] == expectedFieldOfViewDefault, 'fieldOfView should have been updated to default'


def test_rotate_look():

    e = controller.step(dict(action='RotateLook', rotation=0, horizon=0))
    position = controller.last_event.metadata['agent']['position']
    rotation = controller.last_event.metadata['agent']['rotation']
    assert rotation == dict(x=0, y=0, z=0)
    e = controller.step(dict(action='RotateLook', rotation=90, horizon=31))
    assert e.metadata['agent']['position'] == position
    assert int(e.metadata['agent']['cameraHorizon']) == 31
    assert e.metadata['agent']['rotation']['y'] == 90.0
    assert e.metadata['agent']['rotation']['x'] == 0.0
    assert e.metadata['agent']['rotation']['z'] == 0.0


def test_rotate_right():

    e = controller.step(dict(action='RotateLook', rotation=0, horizon=0))
    position = controller.last_event.metadata['agent']['position']
    rotation = controller.last_event.metadata['agent']['rotation']
    assert rotation == dict(x=0, y=0, z=0)
    horizon = controller.last_event.metadata['agent']['cameraHorizon']
    e = controller.step(dict(action='RotateRight'))
    assert e.metadata['agent']['position'] == position
    assert e.metadata['agent']['cameraHorizon'] == horizon
    assert e.metadata['agent']['rotation']['y'] == 90.0
    assert e.metadata['agent']['rotation']['x'] == 0.0
    assert e.metadata['agent']['rotation']['z'] == 0.0


def test_teleport():
    controller.step(dict(action='Teleport', x=-1.5, z=-1.5, y=1.0), raise_for_failure=True)
    position = controller.last_event.metadata['agent']['position']

    assert_near(position, dict(x=-1.5, z=-1.5, y=0.901))

    controller.step(dict(action='Teleport', x=-2.0, z=-2.5, y=1.0), raise_for_failure=True)
    position = controller.last_event.metadata['agent']['position']
    assert_near(position, dict(x=-2.0, z=-2.5, y=0.901))

def test_moveahead():
    controller.step(dict(action='Teleport', x=-1.5, z=-1.5, y=1.0), raise_for_failure=True)
    controller.step(dict(action='MoveAhead'), raise_for_failure=True)
    position = controller.last_event.metadata['agent']['position']
    assert_near(position, dict(x=-1.25, z=-1.5, y=0.901))

def test_moveback():
    controller.step(dict(action='Teleport', x=-1.5, z=-1.5, y=1.0), raise_for_failure=True)
    controller.step(dict(action='MoveBack'), raise_for_failure=True)
    position = controller.last_event.metadata['agent']['position']
    assert_near(position, dict(x=-1.75, z=-1.5, y=0.900998652))

def test_moveleft():
    controller.step(dict(action='Teleport', x=-1.5, z=-1.5, y=1.0), raise_for_failure=True)
    controller.step(dict(action='MoveLeft'), raise_for_failure=True)
    position = controller.last_event.metadata['agent']['position']
    assert_near(position, dict(x=-1.5, z=-1.25, y=0.901))

def test_moveright():
    controller.step(dict(action='Teleport', x=-1.5, z=-1.5, y=1.0), raise_for_failure=True)
    controller.step(dict(action='MoveRight'), raise_for_failure=True)
    position = controller.last_event.metadata['agent']['position']
    assert_near(position, dict(x=-1.5, z=-1.75, y=0.901))


def test_moveahead_mag():
    controller.step(dict(action='Teleport', x=-1.5, z=-1.5, y=1.1), raise_for_failure=True)
    controller.step(dict(action='MoveAhead', moveMagnitude=0.5), raise_for_failure=True)
    position = controller.last_event.metadata['agent']['position']
    assert_near(position, dict(x=-1.0, z=-1.5, y=0.9009983))


def test_moveahead_fail():
    controller.step(dict(action='Teleport', x=-1.5, z=-1.5, y=1.0), raise_for_failure=True)
    controller.step(dict(action='MoveAhead', moveMagnitude=5.0))
    assert not controller.last_event.metadata['lastActionSuccess']


def test_get_scenes_in_build():
    scenes = set()
    for g in glob.glob('unity/Assets/Scenes/*.unity'):
        scenes.add(os.path.splitext(os.path.basename(g))[0])



    event = controller.step(dict(action='GetScenesInBuild'), raise_for_failure=True)
    return_scenes = set(event.metadata['actionReturn'])
    # not testing for private scenes
    diff = scenes - return_scenes
    assert len(diff) == 0, "scenes in build diff: %s" % diff


def test_change_resolution():
    event = controller.step(dict(action='Pass'), raise_for_failure=True)
    assert event.frame.shape == (300,300,3)
    event = controller.step(dict(action='ChangeResolution', x=400, y=400), raise_for_failure=True)
    assert event.frame.shape == (400,400,3)
    assert event.screen_width == 400
    assert event.screen_height == 400
    event = controller.step(dict(action='ChangeResolution', x=300, y=300), raise_for_failure=True)

