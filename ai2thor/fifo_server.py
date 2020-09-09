# Copyright Allen Institute for Artificial Intelligence 2017
"""
ai2thor.server

Handles all communication with Unity through a Flask service.  Messages
are sent to the controller using a pair of request/response queues.
"""
import ai2thor.server
import json
import msgpack
import os
import tempfile
from enum import IntEnum, unique
from collections import defaultdict
import struct

# FifoFields
@unique
class FieldType(IntEnum):
    METADATA = 1
    ACTION = 2
    ACTION_RESULT = 3
    RGB_IMAGE = 4
    DEPTH_IMAGE = 5
    NORMALS_IMAGE = 6
    FLOWS_IMAGE = 7
    CLASSES_IMAGE = 8
    IDS_IMAGE = 9
    THIRD_PARTY_IMAGE = 10
    END_OF_MESSAGE = 255


class FifoServer(ai2thor.server.Server):
    header_format = '!BI'
    header_size = struct.calcsize(header_format)
    field_types = {f.value:f for f in FieldType}

    def __init__(self, width, height, depth_format=ai2thor.server.DepthFormat.Meters, add_depth_noise=False):

        self.tmp_dir = tempfile.TemporaryDirectory()
        self.server_pipe_path = os.path.join(self.tmp_dir.name, 'server.pipe')
        self.client_pipe_path = os.path.join(self.tmp_dir.name, 'client.pipe')
        self.server_pipe = None
        self.client_pipe = None
        # allows us to map the enum to form field names
        # for backwards compatibility
        # this can be removed when the wsgi server is removed
        self.form_field_map = {
            FieldType.RGB_IMAGE:'image',
            FieldType.DEPTH_IMAGE:'image_depth',
            FieldType.IDS_IMAGE:'image_ids',
            FieldType.NORMALS_IMAGE:'image_normals',
            FieldType.FLOWS_IMAGE:'image_flows',
            FieldType.THIRD_PARTY_IMAGE:"image-thirdParty-camera"
        }

        self.image_fields = {
            FieldType.IDS_IMAGE,
            FieldType.CLASSES_IMAGE,
            FieldType.FLOWS_IMAGE,
            FieldType.NORMALS_IMAGE,
            FieldType.DEPTH_IMAGE,
            FieldType.RGB_IMAGE,
            FieldType.THIRD_PARTY_IMAGE
            }

        self.eom_header = self._create_header(FieldType.END_OF_MESSAGE, b'')
        super().__init__(width, height, depth_format, add_depth_noise)

    def _create_header(self, message_type, body):
        return struct.pack(self.header_format, message_type, len(body))

    def _recv_message(self):
        if self.server_pipe is None:
            self.server_pipe = open(self.server_pipe_path, "rb" )

        metadata = None
        files = defaultdict(list)
        while True:
            header = self.server_pipe.read(self.header_size) # message type + length
            # XXX
            # check if len == 0 to disconnect
            # check if pid is dead
            
            if header[0] == FieldType.END_OF_MESSAGE.value:
                #print("GOT EOM")
                break

            #print("got header %s" % header)
            field_type_int, message_length = struct.unpack(self.header_format, header)
            field_type = self.field_types[field_type_int]
            body = self.server_pipe.read(message_length)
            #print("field type")
            #print(field_type)
            if field_type is FieldType.METADATA:
                #print("body length %s" % len(body))
                #print(body)
                metadata = msgpack.loads(body, raw=False)
                #print(metadata)
            elif field_type in self.image_fields:
                files[self.form_field_map[field_type]].append(body)
            else:
                raise ValueError("Invalid field type: %s" % field_type )

        return metadata, files

    def _send_message(self, message_type, body):
        #print("trying to write to ")
        if self.client_pipe is None:
            self.client_pipe = open(self.client_pipe_path, "wb" )

        header = self._create_header(message_type, body)
        #print("len header %s" % len(header))
        #print("sending body %s" % body)

        self.client_pipe.write(header + body + self.eom_header)
        self.client_pipe.flush()

    def receive(self):
        metadata, files = self._recv_message()
        return self.create_event(metadata, files)

    def send(self, action):
        #print("got action to send")
        if 'sequenceId' in action:
            self.sequence_id = action['sequenceId']
        else:
            self.sequence_id += 1
            action['sequenceId'] = self.sequence_id
        #print(action)

        # need to switch this to msgpack
        self._send_message(FieldType.ACTION, json.dumps(action, cls=ai2thor.server.NumpyAwareEncoder).encode('utf8'))
    

    def start(self):
        os.mkfifo(self.server_pipe_path)
        os.mkfifo(self.client_pipe_path)
        self.started = True

    # params to pass up to unity
    def unity_params(self):
        params = dict(
            server_type='FIFO',
            fifo_server_pipe_path=self.server_pipe_path,
            fifo_client_pipe_path=self.client_pipe_path
        )
        return params

    def stop(self):
        self.client_pipe.close()
        self.server_pipe.close()

