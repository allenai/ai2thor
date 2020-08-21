import ai2thor.fifo_server
import pytest
import numpy as np
import msgpack
from ai2thor.tests.test_event import metadata_simple
from ai2thor.tests.fifo_client import FifoClient
from io import BytesIO
import copy




def test_multi_agent_train():


    s = ai2thor.fifo_server.FifoServer(width=300, height=300)
    s.send(dict(action='RotateRight'))
    c = FifoClient(s.server_pipe_path, s.client_pipe_path)
    msg = c.recv()
    c.send(ai2thor.fifo_server.FieldType.METADATA, generate_multi_agent_metadata_payload(metadata_simple, s.sequence_id))
    c.send_eom()
    event = s.receive()
    assert len(event.events) == 2
    assert event.events[1].metadata == metadata_simple



def test_train_numpy_action():

    s = ai2thor.fifo_server.FifoServer(width=300, height=300)
    s.send(dict(
        action='Teleport', 
        rotation=dict(y=np.array([24])[0]),
        moveMagnitude=np.array([55.5])[0],
    ))
    c = FifoClient(s.server_pipe_path, s.client_pipe_path)
    msg = c.recv()

    assert msg == {'action': 'Teleport', 'rotation': {'y': 24}, 'sequenceId': 1, 'moveMagnitude': 55.5}

def generate_metadata_payload(metadata, sequence_id):
    return msgpack.dumps(dict(agents=[metadata], sequenceId=sequence_id))

def generate_multi_agent_metadata_payload(metadata, sequence_id):
    return msgpack.dumps(dict(agents=[metadata, metadata], activeAgentId=1, sequenceId=sequence_id))

def test_simple():

    s = ai2thor.fifo_server.FifoServer(width=300, height=300)
    s.send(dict(action='RotateRight'))
    c = FifoClient(s.server_pipe_path, s.client_pipe_path)
    msg = c.recv()
    assert msg == dict(action='RotateRight', sequenceId=s.sequence_id)
    c.send(ai2thor.fifo_server.FieldType.METADATA, generate_metadata_payload(metadata_simple, s.sequence_id))
    c.send_eom()
    event = s.receive()
    assert event.metadata == metadata_simple

def test_sequence_id_mismatch():
    s = ai2thor.fifo_server.FifoServer(width=300, height=300)
    s.send(dict(action='RotateRight'))
    c = FifoClient(s.server_pipe_path, s.client_pipe_path)
    msg = c.recv()
    assert msg == dict(action='RotateRight', sequenceId=s.sequence_id)
    c.send(ai2thor.fifo_server.FieldType.METADATA, generate_metadata_payload(metadata_simple, s.sequence_id + 1))
    c.send_eom()
    exception_caught = False
    try:
        event = s.receive()
    except ValueError as e:
        exception_caught = True
    assert exception_caught
        

