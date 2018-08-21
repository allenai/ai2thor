import ai2thor.controller
from ai2thor.server import Event
import pytest
import numpy as np
import os
import math

class FakeQueue(object):

    def __init__(self):
        self.value = None

    def put_nowait(self, v):
        assert self.value is None
        self.value = v

    def get(self, block, timeout):
        v = self.value
        self.value = None
        return v

    # always return empty so that we pass
    def empty(self):
        return True

def test_contstruct():
    c = ai2thor.controller.Controller()
    assert True


def test_distance():
    point1 = dict(x=1.5, z=2.5)
    point2 = dict(x=4.33, z=7.5)
    point3 = dict(x=2.5, z=3.5)
    assert ai2thor.controller.distance(point1, point2) == 5.745337239884183 
    assert ai2thor.controller.distance(point1, point1) == 0.0
    assert ai2thor.controller.distance(point1, point3) == math.sqrt(2.0)

def test_key_for_point():
    assert ai2thor.controller.key_for_point(2.567, -3.43) == "2.6 -3.4"

def test_scene_names():
    c = ai2thor.controller.Controller()
    assert len(c.scene_names()) == 120

def test_local_executable_path():
    c = ai2thor.controller.Controller()
    c.local_executable_path = 'FOO'
    assert c.executable_path() == 'FOO'

def test_step_put_object_in_wrong_receptacle():
    fake_event = Event(dict(screenWidth=300, screenHeight=300, colors=[], lastActionSuccess=True))
    c = ai2thor.controller.Controller()
    c.last_event = fake_event
    e = c.step(dict(action='PutObject', objectId='Mug|1.2|3.4|5.6', receptacleObjectId="Box|5.6|4.3|2.1"))
    assert not e.metadata['lastActionSuccess']

def test_invalid_action():
    fake_event = Event(dict(screenWidth=300, screenHeight=300, colors=[], lastActionSuccess=False, errorCode='InvalidAction', errorMessage='Invalid method: moveaheadbadmethod'))
    c = ai2thor.controller.Controller()
    c.last_event = fake_event
    action1 = dict(action='MoveaheadbadMethod')
    c.request_queue = FakeQueue()
    c.request_queue.put_nowait(fake_event)

    with pytest.raises(ValueError) as excinfo:
        c.step(action1, raise_for_failure=True)
    assert excinfo.value.args == ('Invalid method: moveaheadbadmethod',)

def test_fix_visibility_distance_env():
    os.environ['AI2THOR_VISIBILITY_DISTANCE'] = '2.0'
    fake_event = Event(dict(screenWidth=300, screenHeight=300, colors=[], lastActionSuccess=True))
    c = ai2thor.controller.Controller()
    c.last_event = fake_event
    action1 = dict(action='Initialize', gridSize=0.25)
    c.request_queue = FakeQueue()
    c.request_queue.put_nowait(fake_event)
    c.step(action1)
    filtered_action = c.response_queue.get()
    print(filtered_action)
    assert filtered_action == {'action': 'Initialize', 'gridSize': 0.25, 'visibilityDistance':2.0}
    del(os.environ['AI2THOR_VISIBILITY_DISTANCE'])


def test_raise_for_failure():
    fake_event = Event(dict(screenWidth=300, screenHeight=300, colors=[], lastActionSuccess=False, errorCode='NotOpen'))
    c = ai2thor.controller.Controller()
    c.last_event = fake_event
    action1 = dict(action='MoveAhead')
    c.request_queue = FakeQueue()
    c.request_queue.put_nowait(fake_event)

    with pytest.raises(AssertionError):
        c.step(action1, raise_for_failure=True)

def test_failure():
    fake_event = Event(dict(screenWidth=300, screenHeight=300, colors=[], lastActionSuccess=False, errorCode='NotOpen'))
    c = ai2thor.controller.Controller()
    c.last_event = fake_event
    action1 = dict(action='MoveAhead')
    c.request_queue = FakeQueue()
    c.request_queue.put_nowait(fake_event)
    e = c.step(action1)
    assert c.last_action == action1
    assert not e.metadata['lastActionSuccess']

def test_last_action():
    fake_event = Event(dict(screenWidth=300, screenHeight=300, colors=[], lastActionSuccess=True))
    c = ai2thor.controller.Controller()
    c.last_event = fake_event
    action1 = dict(action='RotateRight')
    c.request_queue = FakeQueue()
    c.request_queue.put_nowait(fake_event)
    e = c.step(action1)
    assert c.last_action == action1
    assert e.metadata['lastActionSuccess']

    c = ai2thor.controller.Controller()
    c.last_event = fake_event
    action2 = dict(action='RotateLeft')
    c.request_queue = FakeQueue()
    c.request_queue.put_nowait(fake_event)
    e = c.step(action2)
    assert c.last_action == action2
    assert e.metadata['lastActionSuccess']


def test_unity_command():
    c = ai2thor.controller.Controller()
    assert c.unity_command(650, 550) == [
        c.executable_path(),
        '-screen-fullscreen', 
        '0',
        '-screen-quality', 
        '6', 
        '-screen-width', 
        '650', 
        '-screen-height', 
        '550'] 

    c = ai2thor.controller.Controller(quality='Low', fullscreen=True)
    assert c.unity_command(650, 550) == [
        c.executable_path(),
        '-screen-fullscreen', 
        '1',
        '-screen-quality', 
        '1', 
        '-screen-width', 
        '650', 
        '-screen-height', 
        '550'] 
