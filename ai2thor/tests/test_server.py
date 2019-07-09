import ai2thor.server
import pytest
import numpy as np
import json
from ai2thor.controller import Queue
from ai2thor.tests.test_event import metadata_simple
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
    data += b'\r\n' + boundary + b'\r\nContent-Type: text/plain; charset="utf-8"\r\nContent-disposition: form-data; name="token"\r\n\r\n'
    data += b'12cb40b5-3a70-4316-8ae2-82cbff6c9902'
    data += b'\r\n' + boundary + b'--\r\n'
    return data

def generate_form(metadata, sequence_id=1):
    boundary = b'--OVCo05I3SVXLPeTvCgJjHl1EOleL4u9TDx5raRVt'
    data = b'\r\n' + boundary + b'\r\nContent-Type: text/plain; charset="utf-8"\r\nContent-disposition: form-data; name="metadata"\r\n\r\n'
    data += json.dumps(dict(agents=[metadata], sequenceId=sequence_id)).encode('utf8')
    data += b'\r\n' + boundary + b'\r\nContent-Type: text/plain; charset="utf-8"\r\nContent-disposition: form-data; name="token"\r\n\r\n'
    data += b'12cb40b5-3a70-4316-8ae2-82cbff6c9902'
    data += b'\r\n' + boundary + b'--\r\n'
    return data

@pytest.fixture
def server():
    request_queue = Queue(maxsize=1)
    response_queue = Queue(maxsize=1)

    return ai2thor.server.Server(request_queue, response_queue, '127.0.0.1')

@pytest.fixture
def client(server):
    return server.app.test_client()

def test_ping(client):
    res = client.get('/ping')
    assert res.data == b'pong'

def test_multi_agent_train():
    request_queue = Queue(maxsize=1)
    response_queue = Queue(maxsize=1)

    response_queue.put_nowait(dict(action='RotateRight'))

    s = ai2thor.server.Server(request_queue, response_queue, '127.0.0.1')
    c = s.app.test_client()
    res = c.post(
        '/train', 
        buffered=True,
        content_type='multipart/form-data; boundary=OVCo05I3SVXLPeTvCgJjHl1EOleL4u9TDx5raRVt',
        input_stream=BytesIO(generate_multi_agent_form(metadata_simple, s.sequence_id)))
    assert res.status_code == 200

def test_train_numpy_action():
    request_queue = Queue(maxsize=1)
    response_queue = Queue(maxsize=1)

    response_queue.put_nowait(dict(
        action='Teleport', 
        rotation=dict(y=np.array([24])[0]),
        moveMagnitude=np.array([55.5])[0],
    ))

    s = ai2thor.server.Server(request_queue, response_queue, '127.0.0.1')
    c = s.app.test_client()
    res = c.post(
        '/train', 
        buffered=True,
        content_type='multipart/form-data; boundary=OVCo05I3SVXLPeTvCgJjHl1EOleL4u9TDx5raRVt',
        input_stream=BytesIO(generate_form(metadata_simple, s.sequence_id)))
    j = json.loads(res.get_data())
    assert j == {'action': 'Teleport', 'rotation': {'y': 24}, 'sequenceId': 1, 'moveMagnitude': 55.5}
    assert res.status_code == 200

def test_train():
    request_queue = Queue(maxsize=1)
    response_queue = Queue(maxsize=1)

    response_queue.put_nowait(dict(action='RotateRight'))

    s = ai2thor.server.Server(request_queue, response_queue, '127.0.0.1')
    c = s.app.test_client()
    res = c.post(
        '/train',
        buffered=True,
        content_type='multipart/form-data; boundary=OVCo05I3SVXLPeTvCgJjHl1EOleL4u9TDx5raRVt',
        input_stream=BytesIO(generate_form(metadata_simple, s.sequence_id)))
    assert res.status_code == 200

def test_client_token_mismatch():
    request_queue = Queue(maxsize=1)
    response_queue = Queue(maxsize=1)

    response_queue.put_nowait(dict(action='RotateRight'))

    s = ai2thor.server.Server(request_queue, response_queue, '127.0.0.1')
    s.client_token = '123456'
    c = s.app.test_client()

    res = c.post(
        '/train', 
        buffered=True,
        content_type='multipart/form-data; boundary=OVCo05I3SVXLPeTvCgJjHl1EOleL4u9TDx5raRVt',
        input_stream=BytesIO(generate_form(metadata_simple, s.sequence_id + 1)))
    assert res.status_code == 403

def test_non_multipart():
    request_queue = Queue(maxsize=1)
    response_queue = Queue(maxsize=1)

    response_queue.put_nowait(dict(action='RotateRight'))

    s = ai2thor.server.Server(request_queue, response_queue, '127.0.0.1')
    c = s.app.test_client()
    s.client_token = '1234567'

    m = dict(agents=[metadata_simple], sequenceId=s.sequence_id)
    res = c.post(
        '/train', 
        data=dict(metadata=json.dumps(m), token=s.client_token))
    assert res.status_code == 200

def test_sequence_id_mismatch():
    request_queue = Queue(maxsize=1)
    response_queue = Queue(maxsize=1)

    response_queue.put_nowait(dict(action='RotateRight'))

    s = ai2thor.server.Server(request_queue, response_queue, '127.0.0.1')
    c = s.app.test_client()

    res = c.post(
        '/train', 
        buffered=True,
        content_type='multipart/form-data; boundary=OVCo05I3SVXLPeTvCgJjHl1EOleL4u9TDx5raRVt',
        input_stream=BytesIO(generate_form(metadata_simple, s.sequence_id + 1)))
    assert res.status_code == 500
