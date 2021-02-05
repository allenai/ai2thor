
#import pytest
import os
import string
import random
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
    controller.stop()

# Issue #514 found that the thirdPartyCamera image code was causing multi-agents to end
# up with the same frame
def test_multi_agent_with_third_party_camera():
    controller = build_controller(server_class=FifoServer, agentCount=2)
    assert not np.all(controller.last_event.events[1].frame == controller.last_event.events[0].frame)
    event = controller.step(dict(action='AddThirdPartyCamera', rotation=dict(x=0, y=0, z=90), position=dict(x=-1.0, z=-2.0, y=1.0)))
    assert not np.all(controller.last_event.events[1].frame == controller.last_event.events[0].frame)
    controller.stop()

# Issue #526 thirdPartyCamera hanging without correct keys in FifoServer FormMap
def test_third_party_camera_with_image_synthesis():
    controller = build_controller(server_class=FifoServer, renderObjectImage=True, renderDepthImage=True, renderClassImage=True)
    event = controller.step(dict(action='AddThirdPartyCamera', rotation=dict(x=0, y=0, z=90), position=dict(x=-1.0, z=-2.0, y=1.0)))
    assert len(event.third_party_depth_frames) == 1
    assert len(event.third_party_class_segmentation_frames) == 1
    assert len(event.third_party_camera_frames) == 1
    assert len(event.third_party_instance_segmentation_frames) == 1
    controller.stop()


def test_rectangle_aspect():
    controller = build_controller(width=600, height=300)
    controller.reset('FloorPlan28')
    event = controller.step(dict(action='Initialize', gridSize=0.25))
    assert event.frame.shape == (300, 600, 3)
    controller.stop()

def test_small_aspect():
    controller = build_controller(width=128, height=64)
    controller.reset('FloorPlan28')
    event = controller.step(dict(action='Initialize', gridSize=0.25))
    assert event.frame.shape == (64, 128, 3)
    controller.stop()

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
    controller.stop()

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
    fast_controller.stop()

@pytest.mark.parametrize("controller", [fifo_controller])
def test_fifo_large_input(controller):
    random_string = ''.join(random.choice(string.ascii_letters) for i in range(1024 * 16))
    event = controller.step(dict(action='TestActionReflectParam', rvalue=random_string))
    assert event.metadata['actionReturn'] == random_string

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
def test_target_invocation_exception(controller):
    # TargetInvocationException is raised when short circuiting failures occur
    # on the Unity side. It often occurs when invalid arguments are used.
    event = controller.step('OpenObject', x=1.5, y=0.5)
    assert not event.metadata['lastActionSuccess'], 'OpenObject(x > 1) should fail.'
    assert event.metadata['errorMessage'], 'errorMessage should not be empty when OpenObject(x > 1).'

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

    # expects position to be a Vector3, should fail!
    event = controller.step(
        action="AddThirdPartyCamera",
        position=5,
        rotation=dict(x=0, y=0, z=0)
    )
    assert not event.metadata['lastActionSuccess'], 'position should not allow float input!'

    # orthographicSize expects float, not Vector3!
    error_message = None
    try:
        event = controller.step(
            action="AddThirdPartyCamera",
            position=dict(x=0, y=0, z=0),
            rotation=dict(x=0, y=0, z=0),
            orthographic=True,
            orthographicSize=dict(x=0, y=0, z=0)
        )
    except ValueError as e:
        error_message = str(e)
    assert error_message == "action: AddThirdPartyCamera has an invalid argument: orthographicSize. Cannot convert to: float"


def test_update_third_party_camera():
    controller = build_controller(server_class=FifoServer)

    # add a new camera
    expectedPosition = dict(x=1.2, y=2.3, z=3.4)
    expectedRotation = dict(x=30, y=40, z=50)
    expectedFieldOfView = 45.0
    e = controller.step(dict(action=Actions.AddThirdPartyCamera, position=expectedPosition, rotation=expectedRotation, fieldOfView=expectedFieldOfView))
    assert len(controller.last_event.metadata[MultiAgentMetadata.thirdPartyCameras]) == 1, 'there should be 1 camera'

    # update camera pose fully
    expectedPosition = dict(x=2.2, y=3.3, z=4.4)
    expectedRotation = dict(x=10, y=20, z=30)
    expectedInitialFieldOfView = 45.0
    e = controller.step(dict(action=Actions.UpdateThirdPartyCamera, thirdPartyCameraId=0, position=expectedPosition, rotation=expectedRotation))
    camera = e.metadata[MultiAgentMetadata.thirdPartyCameras][0]
    assert_near(camera[ThirdPartyCameraMetadata.position], expectedPosition, 'position should have been updated')
    assert_near(camera[ThirdPartyCameraMetadata.rotation], expectedRotation, 'rotation should have been updated')
    assert camera[ThirdPartyCameraMetadata.fieldOfView] == expectedInitialFieldOfView, 'fieldOfView should not have changed'

    # partially update the camera pose
    changeFOV = 55.0
    expectedPosition2 = dict(x=3.2, z=5)
    expectedRotation2 = dict(y=90)
    e = controller.step(
        action=Actions.UpdateThirdPartyCamera,
        thirdPartyCameraId=0,
        fieldOfView=changeFOV,
        position=expectedPosition2,
        rotation=expectedRotation2
    )
    camera = e.metadata[MultiAgentMetadata.thirdPartyCameras][0]
    assert camera[ThirdPartyCameraMetadata.fieldOfView] == changeFOV, 'fieldOfView should have been updated'

    expectedPosition.update(expectedPosition2)
    expectedRotation.update(expectedRotation2)
    assert_near(camera[ThirdPartyCameraMetadata.position], expectedPosition, 'position should been slightly updated')
    assert_near(camera[ThirdPartyCameraMetadata.rotation], expectedRotation, 'rotation should been slightly updated')

    for fov in [-1, 181, 0]:
        e = controller.step(dict(action=Actions.UpdateThirdPartyCamera, thirdPartyCameraId=0, fieldOfView=fov))
        assert not e.metadata['lastActionSuccess'], 'fieldOfView should fail outside of (0, 180)'
        assert_near(camera[ThirdPartyCameraMetadata.position], expectedPosition, 'position should not have updated')
        assert_near(camera[ThirdPartyCameraMetadata.rotation], expectedRotation, 'rotation should not have updated')
    controller.stop()


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

@pytest.mark.parametrize("controller", [wsgi_controller, fifo_controller])
def test_open(controller):
    objects = controller.last_event.metadata['objects']
    obj_to_open = next(obj for obj in objects if obj['objectType'] == 'Fridge')

    # helper that returns obj_to_open from a new event
    def get_object(event, object_id):
        return next(obj for obj in event.metadata['objects']
                    if obj['objectId'] == object_id)

    for openness in [0.5, 0.7, 0]:
        event = controller.step(
            action='OpenObject',
            objectId=obj_to_open['objectId'],
            openness=openness,
            forceAction=True,
            raise_for_failure=True)
        opened_obj = get_object(event, obj_to_open['objectId'])
        assert abs(opened_obj['openness'] - openness) < 1e-3, 'Incorrect openness!'
        assert opened_obj['isOpen'] == (openness != 0), 'isOpen incorrectly reported!'

    # test bad openness values
    for bad_openness in [-0.5, 1.5]:
        event = controller.step(
            action='OpenObject',
            objectId=obj_to_open['objectId'],
            openness=bad_openness,
            forceAction=True)
        assert not event.metadata['lastActionSuccess'], '0.0 > Openness > 1.0 should fail!'

    # test backwards compatibility on moveMagnitude, where moveMagnitude
    # is now `openness`, but when moveMagnitude = 0 that corresponds to openness = 1.
    event = controller.step(
        action='OpenObject',
        objectId=obj_to_open['objectId'],
        forceAction=True,
        moveMagnitude=0)
    opened_obj = get_object(event, obj_to_open['objectId'])
    assert abs(opened_obj['openness'] - 1) < 1e-3, 'moveMagnitude=0 must have openness=1'
    assert opened_obj['isOpen'], 'moveMagnitude isOpen incorrectly reported!'

    # another moveMagnitude check
    test_openness = 0.65
    event = controller.step(
        action='OpenObject',
        objectId=obj_to_open['objectId'],
        forceAction=True,
        moveMagnitude=test_openness)
    opened_obj = get_object(event, obj_to_open['objectId'])
    assert abs(opened_obj['openness'] - test_openness) < 1e-3, 'moveMagnitude is not working!'
    assert opened_obj['isOpen'], 'moveMagnitude isOpen incorrectly reported!'

    # a CloseObject specific check
    event = controller.step(action='CloseObject', objectId=obj_to_open['objectId'], forceAction=True)
    obj = get_object(event, obj_to_open['objectId'])
    assert abs(obj['openness'] - 0) < 1e-3, 'CloseObject openness should be 0'
    assert not obj['isOpen'], 'CloseObject should report isOpen==false!'

@pytest.mark.parametrize("controller", [fifo_controller])
def test_action_dispatch_find_ambiguous(controller):
    event = controller.step(dict(action='TestActionDispatchFindAmbiguous'), typeName='UnityStandardAssets.Characters.FirstPerson.PhysicsRemoteFPSAgentController')

    known_ambig = sorted(['TestActionDispatchSAAmbig', 'TestActionDispatchSAAmbig2', 'ProcessControlCommand'])
    assert sorted(event.metadata['actionReturn']) == known_ambig

@pytest.mark.parametrize("controller", [fifo_controller])
def test_action_dispatch_find_ambiguous_stochastic(controller):
    event = controller.step(dict(action='TestActionDispatchFindAmbiguous'), typeName='UnityStandardAssets.Characters.FirstPerson.StochasticRemoteFPSAgentController')

    known_ambig = sorted(['TestActionDispatchSAAmbig', 'TestActionDispatchSAAmbig2', 'ProcessControlCommand'])
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


@pytest.mark.parametrize("controller", [wsgi_controller, fifo_controller])
def test_get_interactable_poses(controller):
    controller.reset('FloorPlan28')
    fridgeId = next(obj['objectId'] for obj in controller.last_event.metadata['objects']
                    if obj['objectType'] == 'Fridge')
    event = controller.step('GetInteractablePoses', objectId=fridgeId)
    poses = event.metadata['actionReturn']
    assert len(poses) > 490, "Should have around 494 interactable poses next to the fridge!"

    # teleport to a random pose
    pose = poses[len(poses) // 2]
    event = controller.step('TeleportFull', **pose)

    # assumes 1 fridge in the scene
    fridge = next(obj for obj in controller.last_event.metadata['objects']
                  if obj['objectType'] == 'Fridge')
    assert fridge['visible'], "Object is not interactable!"

    # tests that teleport correctly works with **syntax
    assert abs(pose['x'] - event.metadata['agent']['position']['x']) < 1e-3, "Agent x position off!"
    assert abs(pose['z'] - event.metadata['agent']['position']['z']) < 1e-3, "Agent z position off!"
    assert abs(pose['rotation'] - event.metadata['agent']['rotation']['y']) < 1e-3, "Agent rotation off!"
    assert abs(pose['horizon'] - event.metadata['agent']['cameraHorizon']) < 1e-3, "Agent horizon off!"
    assert pose['standing'] == event.metadata['agent']['isStanding'], "Agent's isStanding is off!"

    # potato should be inside of the fridge (and, thus, non interactable)
    potatoId = next(obj['objectId'] for obj in controller.last_event.metadata['objects']
                    if obj['objectType'] == 'Potato')
    event = controller.step('GetInteractablePoses', objectId=potatoId)
    assert len(event.metadata['actionReturn']) == 0, "Potato is inside of fridge, and thus, shouldn't be interactable"
    assert event.metadata['lastActionSuccess'], "GetInteractablePoses with Potato shouldn't have failed!"

    # assertion for maxPoses
    event = controller.step('GetInteractablePoses', objectId=fridgeId, maxPoses=50)
    assert len(event.metadata['actionReturn']) == 50, "maxPoses should be capped at 50!"

    # assert only checking certain horizons and rotations is working correctly
    horizons = [0, 30]
    rotations = [0, 45]
    event = controller.step('GetInteractablePoses', objectId=fridgeId, horizons=horizons, rotations=rotations)
    for pose in event.metadata['actionReturn']:
        horizon_works = False
        for horizon in horizons:
            if abs(pose['horizon'] - horizon) < 1e-3:
                horizon_works = True
                break
        assert horizon_works, "Not expecting horizon: " + pose['horizon']

        rotation_works = False
        for rotation in rotations:
            if abs(pose['rotation'] - rotation) < 1e-3:
                rotation_works = True
                break
        assert rotation_works, "Not expecting rotation: " + pose['rotation']

    # assert only checking certain horizons and rotations is working correctly
    event = controller.step('GetInteractablePoses', objectId=fridgeId, rotations=[270])
    assert len(event.metadata['actionReturn']) == 0, "Fridge shouldn't be viewable from this rotation!"
    assert event.metadata['lastActionSuccess'], "GetInteractablePoses with Fridge shouldn't have failed!"

    # test maxDistance
    event = controller.step('GetInteractablePoses', objectId=fridgeId, maxDistance=5)
    assert 1300 > len(event.metadata['actionReturn']) > 1200, 'GetInteractablePoses with large maxDistance is off!'


@pytest.mark.parametrize("controller", [wsgi_controller, fifo_controller])
def test_2d_semantic_hulls(controller):
    controller.reset('FloorPlan28')

    object_types = ["Tomato", "Drawer", "Fridge"]
    object_ids = [
        "Mug|-03.15|+00.82|-03.47",
        "Faucet|-00.39|+00.93|-03.61",
        "StoveBurner|-00.22|+00.92|-01.85"
    ]

    def get_rounded_hulls(**kwargs):
        md = controller.step("Get2DSemanticHulls", **kwargs).metadata
        assert md["lastActionSuccess"] and md["errorMessage"] == ""
        hulls = md["actionReturn"]
        if isinstance(hulls, list):
            return np.array(hulls, dtype=float).round(2).tolist()
        else:
            return {
                k: np.array(v, dtype=float).round(2).tolist()
                for k, v in md["actionReturn"].items()
            }

    # All objects
    hulls_all = get_rounded_hulls()

    # Filtering by object types
    hulls_type_filtered = get_rounded_hulls(objectTypes=object_types)

    # Filtering by object ids
    hulls_id_filtered = get_rounded_hulls(objectIds=object_ids)

    # Single object id
    hulls_single_object = get_rounded_hulls(objectId=object_ids[0])

    # Used to save the ground truth values:
    # with open("ai2thor/tests/data/semantic-2d-hulls.json", "w") as f:
    #     json.dump(
    #         {
    #             "all": hulls_all,
    #             "type_filtered": hulls_type_filtered,
    #             "id_filtered": hulls_id_filtered,
    #             "single_object": hulls_single_object,
    #         },
    #         f
    #     )

    with open("ai2thor/tests/data/semantic-2d-hulls.json") as f:
        truth = json.load(f)

    assert truth["all"] == hulls_all
    assert truth["type_filtered"] == hulls_type_filtered
    assert truth["id_filtered"] == hulls_id_filtered
    assert truth["single_object"] == hulls_single_object

    # Should fail when given types and ids
    assert not controller.step(
        "Get2DSemanticHulls",
        objectTypes=object_types,
        objectIds=object_ids
    ).metadata["lastActionSuccess"]

