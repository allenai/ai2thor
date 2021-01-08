
#import pytest
import os
import json
import pytest
import jsonschema
import numpy as np
from ai2thor.controller import Controller
from ai2thor.wsgi_server import WsgiServer
from ai2thor.fifo_server import FifoServer
import glob
import re

# Defining const classes to lessen the possibility of a misspelled key
class Actions:
    AddThirdPartyCamera = 'AddThirdPartyCamera'
    UpdateThirdPartyCamera = 'UpdateThirdPartyCamera'

class MultiAgentMetadata:
    thirdPartyCameras = 'thirdPartyCameras'

class ThirdPartyCameraMetadata:
    position = 'position'
    rotation = 'rotation'
    fieldOfView = 'fieldOfView'


def build_controller(**args):
    default_args = dict(scene='FloorPlan28', local_build=True)
    default_args.update(args)
    return Controller(**default_args)

wsgi_controller = build_controller(server_class=WsgiServer)
fifo_controller = build_controller(server_class=FifoServer)

def teardown_module(module):
    wsgi_controller.stop()
    fifo_controller.stop()

def assert_near(point1, point2, error_message=''):
    assert point1.keys() == point2.keys(), error_message
    for k in point1.keys():
        assert round(point1[k], 3) == round(point2[k], 3), error_message

def test_stochastic_controller():
    controller = build_controller(agentControllerType='stochastic')
    controller.reset('FloorPlan28')
    assert controller.last_event.metadata['lastActionSuccess']

# Issue #514 found that the thirdPartyCamera image code was causing multi-agents to end
# up with the same frame
def test_multi_agent_with_third_party_camera():
    controller = build_controller(server_class=FifoServer, agentCount=2)
    assert not np.all(controller.last_event.events[1].frame == controller.last_event.events[0].frame)
    event = controller.step(dict(action='AddThirdPartyCamera', rotation=dict(x=0, y=0, z=90), position=dict(x=-1.0, z=-2.0, y=1.0)))
    assert not np.all(controller.last_event.events[1].frame == controller.last_event.events[0].frame)

# Issue #526 thirdPartyCamera hanging without correct keys in FifoServer FormMap
def test_third_party_camera_with_image_synthesis():
    controller = build_controller(server_class=FifoServer, renderObjectImage=True, renderDepthImage=True, renderClassImage=True)
    event = controller.step(dict(action='AddThirdPartyCamera', rotation=dict(x=0, y=0, z=90), position=dict(x=-1.0, z=-2.0, y=1.0)))
    assert len(event.third_party_depth_frames) == 1
    assert len(event.third_party_class_segmentation_frames) == 1
    assert len(event.third_party_camera_frames) == 1
    assert len(event.third_party_instance_segmentation_frames) == 1


def test_rectangle_aspect():
    controller = build_controller(width=600, height=300)
    controller.reset('FloorPlan28')
    event = controller.step(dict(action='Initialize', gridSize=0.25))
    assert event.frame.shape == (300, 600, 3)

def test_small_aspect():
    controller = build_controller(width=128, height=64)
    controller.reset('FloorPlan28')
    event = controller.step(dict(action='Initialize', gridSize=0.25))
    assert event.frame.shape == (64, 128, 3)

def test_reset():
    controller = build_controller()
    width = 520
    height = 310
    event = controller.reset(scene='FloorPlan28', width=width, height=height, renderDepthImage=True)
    assert event.frame.shape == (height, width, 3), "RGB frame dimensions are wrong!"
    assert event.depth_frame is not None, 'depth frame should have rendered!'
    assert event.depth_frame.shape == (height, width), "depth frame dimensions are wrong!"

    width = 300
    height = 300
    event = controller.reset(scene='FloorPlan28', width=width, height=height, renderDepthImage=False)
    assert event.depth_frame is None, "depth frame shouldn't have rendered!"
    assert event.frame.shape == (height, width, 3), "RGB frame dimensions are wrong!"

def test_fast_emit():
    fast_controller = build_controller(server_class=FifoServer, fastActionEmit=True)
    event = fast_controller.step(dict(action='RotateRight'))
    event_fast_emit = fast_controller.step(dict(action='TestFastEmit', rvalue='foo'))
    event_no_fast_emit = fast_controller.step(dict(action='LookUp'))
    event_no_fast_emit_2 = fast_controller.step(dict(action='RotateRight'))

    assert event.metadata['actionReturn'] is None
    assert event_fast_emit.metadata['actionReturn'] == 'foo' 
    assert id(event.metadata['objects']) ==  id(event_fast_emit.metadata['objects'])
    assert id(event.metadata['objects']) !=  id(event_no_fast_emit.metadata['objects'])
    assert id(event_no_fast_emit_2.metadata['objects']) !=  id(event_no_fast_emit.metadata['objects'])

@pytest.mark.parametrize("controller", [fifo_controller])
def test_fast_emit_disabled(controller):
    event = controller.step(dict(action='RotateRight'))
    event_fast_emit = controller.step(dict(action='TestFastEmit', rvalue='foo'))
    # assert that when actionFastEmit is off that the objects are different
    assert id(event.metadata['objects']) !=  id(event_fast_emit.metadata['objects'])


@pytest.mark.parametrize("controller", [wsgi_controller, fifo_controller])
def test_lookdown(controller):
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

@pytest.mark.parametrize("controller", [wsgi_controller, fifo_controller])
def test_no_leak_params(controller):

    action = dict(action='RotateLook', rotation=0, horizon=0)
    e = controller.step(action)
    assert 'sequenceId' not in action

@pytest.mark.parametrize("controller", [wsgi_controller, fifo_controller])
def test_lookup(controller):

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

@pytest.mark.parametrize("controller", [wsgi_controller, fifo_controller])
def test_rotate_left(controller):

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

@pytest.mark.parametrize("controller", [wsgi_controller, fifo_controller])
def test_simobj_filter(controller):

    objects = controller.last_event.metadata['objects']
    unfiltered_object_ids = sorted([o['objectId'] for o in objects])
    filter_object_ids = sorted([o['objectId'] for o in objects[0:3]])
    e = controller.step(dict(action='SetObjectFilter', objectIds=filter_object_ids))
    assert len(e.metadata['objects']) == len(filter_object_ids)
    filtered_object_ids =sorted([o['objectId'] for o in e.metadata['objects']])
    assert filtered_object_ids == filter_object_ids

    e = controller.step(dict(action='SetObjectFilter', objectIds=[]))
    assert len(e.metadata['objects']) == 0

    e = controller.step(dict(action='ResetObjectFilter'))
    reset_filtered_object_ids =sorted([o['objectId'] for o in e.metadata['objects']])
    assert unfiltered_object_ids == reset_filtered_object_ids


@pytest.mark.parametrize("controller", [wsgi_controller, fifo_controller])
def test_add_third_party_camera(controller):


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


@pytest.mark.parametrize("controller", [wsgi_controller, fifo_controller])
def test_update_third_party_camera(controller):

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


@pytest.mark.parametrize("controller", [wsgi_controller, fifo_controller])
def test_rotate_look(controller):

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


@pytest.mark.parametrize("controller", [wsgi_controller, fifo_controller])
def test_rotate_right(controller):

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


@pytest.mark.parametrize("controller", [wsgi_controller, fifo_controller])
def test_teleport(controller):
    controller.step(dict(action='Teleport', x=-1.5, z=-1.5, y=1.0), raise_for_failure=True)
    position = controller.last_event.metadata['agent']['position']

    assert_near(position, dict(x=-1.5, z=-1.5, y=0.901))

    controller.step(dict(action='Teleport', x=-2.0, z=-2.5, y=1.0), raise_for_failure=True)
    position = controller.last_event.metadata['agent']['position']
    assert_near(position, dict(x=-2.0, z=-2.5, y=0.901))

@pytest.mark.parametrize("controller", [fifo_controller])
def test_action_dispatch_find_ambiguous(controller):
    event = controller.step(dict(action='TestActionDispatchFindAmbiguous'), typeName='UnityStandardAssets.Characters.FirstPerson.PhysicsRemoteFPSAgentController')

    known_ambig = sorted(['TestActionDispatchSAAmbig', 'TestActionDispatchSAAmbig2'])
    assert sorted(event.metadata['actionReturn']) == known_ambig

@pytest.mark.parametrize("controller", [fifo_controller])
def test_action_dispatch_find_ambiguous_stochastic(controller):
    event = controller.step(dict(action='TestActionDispatchFindAmbiguous'), typeName='UnityStandardAssets.Characters.FirstPerson.StochasticRemoteFPSAgentController')

    known_ambig = sorted(['TestActionDispatchSAAmbig', 'TestActionDispatchSAAmbig2'])
    assert sorted(event.metadata['actionReturn']) == known_ambig

@pytest.mark.parametrize("controller", [fifo_controller])
def test_action_dispatch_server_action_ambiguous2(controller):
    exception_thrown = False
    exception_message = None
    try:
        controller.step('TestActionDispatchSAAmbig2')
    except ValueError as e:
        exception_thrown = True
        exception_message = str(e)

    assert exception_thrown
    assert 'Ambiguous action: TestActionDispatchSAAmbig2 Signature match found in the same class' == exception_message

@pytest.mark.parametrize("controller", [fifo_controller])
def test_action_dispatch_server_action_ambiguous(controller):
    exception_thrown = False
    exception_message = None
    try:
        controller.step('TestActionDispatchSAAmbig')
    except ValueError as e:
        exception_thrown = True
        exception_message = str(e)

    assert exception_thrown
    assert exception_message == 'Ambiguous action: TestActionDispatchSAAmbig Mixing a ServerAction method with overloaded methods is not permitted'

@pytest.mark.parametrize("controller", [fifo_controller])
def test_action_dispatch_find_conflicts_stochastic(controller):
    event = controller.step(dict(action='TestActionDispatchFindConflicts'), typeName='UnityStandardAssets.Characters.FirstPerson.StochasticRemoteFPSAgentController')
    known_conflicts = {
        'GetComponent': ['type'],
        'StopCoroutine': ['routine'],
        'TestActionDispatchConflict': ['param22']
    }
    assert event.metadata['actionReturn'] == known_conflicts
    
@pytest.mark.parametrize("controller", [fifo_controller])
def test_action_dispatch_find_conflicts_physics(controller):
    event = controller.step(dict(action='TestActionDispatchFindConflicts'), typeName='UnityStandardAssets.Characters.FirstPerson.PhysicsRemoteFPSAgentController')
    known_conflicts = {
        'GetComponent': ['type'],
        'StopCoroutine': ['routine'],
        'TestActionDispatchConflict': ['param22']
    }
    assert event.metadata['actionReturn'] == known_conflicts

@pytest.mark.parametrize("controller", [wsgi_controller, fifo_controller])
def test_action_dispatch_missing_args(controller):
    caught_exception = False
    try:
        event = controller.step(dict(action='TestActionDispatchNoop', param6='foo'))
        print(event.metadata['actionReturn'])
    except ValueError as e:
        caught_exception = True
    assert caught_exception
    assert controller.last_event.metadata['errorCode'] == 'MissingArguments'
    
@pytest.mark.parametrize("controller", [wsgi_controller, fifo_controller])
def test_action_dispatch_invalid_action(controller):
    caught_exception = False
    try:
        event = controller.step(dict(action='TestActionDispatchNoopFoo'))
    except ValueError as e:
        caught_exception = True
    assert caught_exception
    assert controller.last_event.metadata['errorCode'] == 'InvalidAction'

@pytest.mark.parametrize("controller", [wsgi_controller, fifo_controller])
def test_action_dispatch_empty(controller):
    event = controller.step(dict(action='TestActionDispatchNoop'))
    assert event.metadata['actionReturn'] == 'emptyargs'

@pytest.mark.parametrize("controller", [wsgi_controller, fifo_controller])
def test_action_disptatch_one_param(controller):
    event = controller.step(dict(action='TestActionDispatchNoop', param1=True))
    assert event.metadata['actionReturn'] == 'param1'

@pytest.mark.parametrize("controller", [wsgi_controller, fifo_controller])
def test_action_disptatch_two_param(controller):
    event = controller.step(dict(action='TestActionDispatchNoop', param1=True, param2=False))
    assert event.metadata['actionReturn'] == 'param1 param2'

@pytest.mark.parametrize("controller", [wsgi_controller, fifo_controller])
def test_action_disptatch_two_param_with_default(controller):
    event = controller.step(dict(action='TestActionDispatchNoop2', param3=True, param4='foobar'))
    assert event.metadata['actionReturn'] == 'param3 param4/default foobar'

@pytest.mark.parametrize("controller", [wsgi_controller, fifo_controller])
def test_action_disptatch_two_param_with_default_empty(controller):
    event = controller.step(dict(action='TestActionDispatchNoop2', param3=True))
    assert event.metadata['actionReturn'] == 'param3 param4/default foo'

@pytest.mark.parametrize("controller", [wsgi_controller, fifo_controller])
def test_action_disptatch_serveraction_default(controller):
    event = controller.step(dict(action='TestActionDispatchNoopServerAction'))
    assert event.metadata['actionReturn'] == 'serveraction'

@pytest.mark.parametrize("controller", [wsgi_controller, fifo_controller])
def test_action_disptatch_serveraction_with_object_id(controller):
    event = controller.step(dict(action='TestActionDispatchNoopServerAction', objectId='candle|1|2|3'))
    assert event.metadata['actionReturn'] == 'serveraction'

@pytest.mark.parametrize("controller", [wsgi_controller, fifo_controller])
def test_action_disptatch_all_default(controller):
    event = controller.step(dict(action='TestActionDispatchNoopAllDefault'))
    assert event.metadata['actionReturn'] == 'alldefault'

@pytest.mark.parametrize("controller", [wsgi_controller, fifo_controller])
def test_action_disptatch_some_default(controller):
    event = controller.step(dict(action='TestActionDispatchNoopAllDefault2', param12=9.0))
    assert event.metadata['actionReturn'] == 'somedefault'

@pytest.mark.parametrize("controller", [wsgi_controller, fifo_controller])
def test_moveahead(controller):
    controller.step(dict(action='Teleport', x=-1.5, z=-1.5, y=1.0), raise_for_failure=True)
    controller.step(dict(action='MoveAhead'), raise_for_failure=True)
    position = controller.last_event.metadata['agent']['position']
    assert_near(position, dict(x=-1.25, z=-1.5, y=0.901))

@pytest.mark.parametrize("controller", [wsgi_controller, fifo_controller])
def test_moveback(controller):
    controller.step(dict(action='Teleport', x=-1.5, z=-1.5, y=1.0), raise_for_failure=True)
    controller.step(dict(action='MoveBack'), raise_for_failure=True)
    position = controller.last_event.metadata['agent']['position']
    assert_near(position, dict(x=-1.75, z=-1.5, y=0.900998652))

@pytest.mark.parametrize("controller", [wsgi_controller, fifo_controller])
def test_moveleft(controller):
    controller.step(dict(action='Teleport', x=-1.5, z=-1.5, y=1.0), raise_for_failure=True)
    controller.step(dict(action='MoveLeft'), raise_for_failure=True)
    position = controller.last_event.metadata['agent']['position']
    assert_near(position, dict(x=-1.5, z=-1.25, y=0.901))

@pytest.mark.parametrize("controller", [wsgi_controller, fifo_controller])
def test_moveright(controller):
    controller.step(dict(action='Teleport', x=-1.5, z=-1.5, y=1.0), raise_for_failure=True)
    controller.step(dict(action='MoveRight'), raise_for_failure=True)
    position = controller.last_event.metadata['agent']['position']
    assert_near(position, dict(x=-1.5, z=-1.75, y=0.901))


@pytest.mark.parametrize("controller", [wsgi_controller, fifo_controller])
def test_moveahead_mag(controller):
    controller.step(dict(action='Teleport', x=-1.5, z=-1.5, y=1.1), raise_for_failure=True)
    controller.step(dict(action='MoveAhead', moveMagnitude=0.5), raise_for_failure=True)
    position = controller.last_event.metadata['agent']['position']
    assert_near(position, dict(x=-1.0, z=-1.5, y=0.9009983))


@pytest.mark.parametrize("controller", [wsgi_controller, fifo_controller])
def test_moveahead_fail(controller):
    controller.step(dict(action='Teleport', x=-1.5, z=-1.5, y=1.0), raise_for_failure=True)
    controller.step(dict(action='MoveAhead', moveMagnitude=5.0))
    assert not controller.last_event.metadata['lastActionSuccess']


@pytest.mark.parametrize("controller", [wsgi_controller, fifo_controller])
def test_jsonschema_metadata(controller):
    event = controller.step(dict(action='Pass'))
    with open("ai2thor/tests/data/metadata-schema.json") as f:
        schema = json.loads(f.read())

    jsonschema.validate(instance=event.metadata, schema=schema)


@pytest.mark.parametrize("controller", [wsgi_controller, fifo_controller])
def test_get_scenes_in_build(controller):
    scenes = set()
    for g in glob.glob('unity/Assets/Scenes/*.unity'):
        scenes.add(os.path.splitext(os.path.basename(g))[0])



    event = controller.step(dict(action='GetScenesInBuild'), raise_for_failure=True)
    return_scenes = set(event.metadata['actionReturn'])
    # not testing for private scenes
    diff = scenes - return_scenes
    assert len(diff) == 0, "scenes in build diff: %s" % diff


@pytest.mark.parametrize("controller", [wsgi_controller, fifo_controller])
def test_change_resolution(controller):
    event = controller.step(dict(action='Pass'), raise_for_failure=True)
    assert event.frame.shape == (300,300,3)
    event = controller.step(dict(action='ChangeResolution', x=400, y=400), raise_for_failure=True)
    assert event.frame.shape == (400,400,3)
    assert event.screen_width == 400
    assert event.screen_height == 400
    event = controller.step(dict(action='ChangeResolution', x=300, y=300), raise_for_failure=True)

