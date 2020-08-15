import ai2thor.server
import pytest
import numpy as np
import json
from ai2thor.server import Queue
from ai2thor.tests.test_event import metadata_simple
from ai2thor.tests.fifo_client import FifoClient
from io import BytesIO
import copy


def generate_multi_agent_form(metadata, sequence_id=1):
    agent2 = copy.deepcopy(metadata)
    agent2['agentId'] = 1
    agent1 = metadata
    agents = [agent1, agent2]
    boundary = b'--OVCo05I3SVXLPeTvCgJjHl1EOleL4u9TDx5raRVt'
    data = b'\r\n' + boundary + b'\r\nContent-Type: text/plain; charset="utf-8"\r\nContent-disposition: form-data; name="metadata"\r\n\r\n'
    data += json.dumps(dict(agents=agents, sequenceId=sequence_id, activeAgentId=1)).encode('utf8')
    data += b'\r\n' + boundary + b'\r\nContent-Type: text/plain; charset="utf-8"\r\nContent-disposition: form-data; name="actionReturns"\r\n\r\n'
    data += b'\r\n' + boundary + b'\r\nContent-Type: text/plain; charset="utf-8"\r\nContent-disposition: form-data; name="token"\r\n\r\n'
    data += b'12cb40b5-3a70-4316-8ae2-82cbff6c9902'
    data += b'\r\n' + boundary + b'--\r\n'
    return data



def test_multi_agent_train():


    s = ai2thor.server.FifoServer(width=300, height=300)
    s.send(dict(action='RotateRight'))
    c = FifoClient(s.server_pipe_path, s.client_pipe_path)
    msg = c.recv()
    c.send(ai2thor.server.FieldType.METADATA, generate_multi_agent_metadata_payload(metadata_simple, s.sequence_id))
    c.send_eom()
    event = s.receive()
    assert len(event.events) == 2
    assert event.events[1].metadata == metadata_simple



def test_train_numpy_action():

    s = ai2thor.server.FifoServer(width=300, height=300)
    s.send(dict(
        action='Teleport', 
        rotation=dict(y=np.array([24])[0]),
        moveMagnitude=np.array([55.5])[0],
    ))
    c = FifoClient(s.server_pipe_path, s.client_pipe_path)
    msg = c.recv()

    assert msg == {'action': 'Teleport', 'rotation': {'y': 24}, 'sequenceId': 1, 'moveMagnitude': 55.5}

def generate_metadata_payload(metadata, sequence_id):
    return json.dumps(dict(agents=[metadata], sequenceId=sequence_id)).encode('utf8')

def generate_multi_agent_metadata_payload(metadata, sequence_id):
    return json.dumps(dict(agents=[metadata, metadata], activeAgentId=1, sequenceId=sequence_id)).encode('utf8')

def test_simple():

    s = ai2thor.server.FifoServer(width=300, height=300)
    s.send(dict(action='RotateRight'))
    c = FifoClient(s.server_pipe_path, s.client_pipe_path)
    msg = c.recv()
    assert msg == dict(action='RotateRight', sequenceId=s.sequence_id)
    c.send(ai2thor.server.FieldType.METADATA, generate_metadata_payload(metadata_simple, s.sequence_id))
    c.send_eom()
    event = s.receive()
    assert event.metadata == metadata_simple

def test_sequence_id_mismatch():
    s = ai2thor.server.FifoServer(width=300, height=300)
    s.send(dict(action='RotateRight'))
    c = FifoClient(s.server_pipe_path, s.client_pipe_path)
    msg = c.recv()
    assert msg == dict(action='RotateRight', sequenceId=s.sequence_id)
    c.send(ai2thor.server.FieldType.METADATA, generate_metadata_payload(metadata_simple, s.sequence_id + 1))
    c.send_eom()
    exception_caught = False
    try:
        event = s.receive()
    except ValueError as e:
        exception_caught = True
    assert exception_caught
        

