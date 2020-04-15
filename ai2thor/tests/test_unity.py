
#import pytest
import os
import ai2thor.controller
import glob

class UnityTestController(ai2thor.controller.Controller):

    def __init__(self,**kwargs):
        base_path = os.path.normpath(os.path.join(os.path.abspath(__file__), "..", "..", "..", "unity", "builds"))
        build_path = 'thor-local-OSXIntel64.app/Contents/MacOS/thor-local-OSXIntel64'
        

        kwargs['local_executable_path'] = os.path.join(base_path, build_path)
        kwargs['scene'] = 'FloorPlan28'
        super().__init__(**kwargs)

    def prune_releases(self):
        pass


controller = UnityTestController()
controller.reset('FloorPlan28')
controller.step(dict(action='Initialize', gridSize=0.25))

#@pytest.fixture
#def controller():
#    return c

def assert_near(point1, point2):
    assert point1.keys() == point2.keys()
    for k in point1.keys():
        assert round(point1[k], 3) == round(point2[k], 3)

def test_rectangle_aspect():
    controller = UnityTestController(width=600, height=300)
    controller.reset('FloorPlan28')
    event = controller.step(dict(action='Initialize', gridSize=0.25))
    assert event.frame.shape == (300, 600, 3)

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

    assert len(controller.last_event.metadata['thirdPartyCameras']) == 0
    e = controller.step(dict(action='AddThirdPartyCamera', position=dict(x=1.2, y=2.3, z=3.4), rotation=dict(x=30, y=40,z=50)))
    assert len(e.metadata['thirdPartyCameras']) == 1
    assert_near(e.metadata['thirdPartyCameras'][0]['position'], dict(x=1.2, y=2.3, z=3.4))
    assert_near(e.metadata['thirdPartyCameras'][0]['rotation'], dict(x=30, y=40, z=50))
    assert len(e.third_party_camera_frames) == 1
    assert e.third_party_camera_frames[0].shape == (300,300,3)
    e = controller.step(dict(action='UpdateThirdPartyCamera', thirdPartyCameraId=0, position=dict(x=2.2, y=3.3, z=4.4), rotation=dict(x=10, y=20,z=30)))
    assert_near(e.metadata['thirdPartyCameras'][0]['position'], dict(x=2.2, y=3.3, z=4.4))
    assert_near(e.metadata['thirdPartyCameras'][0]['rotation'], dict(x=10, y=20, z=30))

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

