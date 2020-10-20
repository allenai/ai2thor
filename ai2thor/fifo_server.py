# Copyright Allen Institute for Artificial Intelligence 2017
"""
ai2thor.server

Handles all communication with Unity through a Flask service.  Messages
are sent to the controller using a pair of request/response queues.
"""
import random
import time
import sys
import ctypes
import ai2thor.server
import json
import warnings
import ujson
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
    SHARED_MEMORY_FIELD = 200
    END_OF_MESSAGE = 255

libc_so = {"darwin": "libc.dylib", "linux": "libc.so.6"}[sys.platform]
libc = ctypes.CDLL(libc_so, use_errno=True, use_last_error=True)

IPC_RMID = 0
IPC_EXCL = 0o2000
IPC_CREAT = 0o1000

PyBUF_READ = 0x100
PyBUF_WRITE = 0x200

shmget = libc.shmget
shmget.restype = ctypes.c_int
shmget.argtypes = (ctypes.c_int, ctypes.c_size_t, ctypes.c_int)

shmat = libc.shmat
shmat.restype = ctypes.c_void_p
shmat.argtypes = (ctypes.c_int, ctypes.c_void_p, ctypes.c_int)

shmdt = libc.shmdt
shmdt.restype = ctypes.c_int
shmdt.argtypes = (ctypes.c_void_p,)

shmctl = libc.shmctl
shmctl.restype = ctypes.c_int
shmctl.argtypes = (ctypes.c_int, ctypes.c_int, ctypes.c_void_p)

memcpy = libc.memcpy
memcpy.restype = ctypes.c_void_p
memcpy.argtypes = (ctypes.c_void_p, ctypes.c_void_p, ctypes.c_size_t)



class FifoServer(ai2thor.server.Server):
    header_format = '!BI'
    header_size = struct.calcsize(header_format)
    field_types = {f.value:f for f in FieldType}

    def __init__(self, width, height, shm_size, depth_format=ai2thor.server.DepthFormat.Meters, add_depth_noise=False):

        self.tmp_dir = tempfile.TemporaryDirectory()
        self.server_pipe_path = os.path.join(self.tmp_dir.name, 'server.pipe')
        self.client_pipe_path = os.path.join(self.tmp_dir.name, 'client.pipe')
        self.server_pipe = None
        self.client_pipe = None
        # allows us to map the enum to form field names
        # for backwards compatibility
        # this can be removed when the wsgi server is removed
        self.shmkey = None
        self.shmaddr = -1
        self.shmid = -1
        self.shm_size = shm_size
        #self._shm_init()

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
            #self.server_pipe = os.open(self.server_pipe_path, os.O_RDONLY | os.O_SYNC)
            self.server_pipe = open(self.server_pipe_path, "rb")

        metadata = None
        files = defaultdict(list)
        while True:
            #header = os.read(self.server_pipe, self.header_size) # message type + length
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
            if field_type is FieldType.SHARED_MEMORY_FIELD:
                offset = message_length
                #print("got shared memory field")
                #print(field_type)
                #print("got shared memory offset" + str(offset))
                #print(self.data[0])
                shm_header = self.mview[offset: offset + self.header_size]
                #print("got shm header")
                #print(shm_header.tobytes())
                field_type_int, message_length = struct.unpack(self.header_format, shm_header)
                field_type = self.field_types[field_type_int]
                offset += self.header_size
                #s = time.time()
                body = self.mview[offset: offset + message_length]
                #e = time.time() - s
                #print("elapsed %s" % e)
                #print("read body from mview")
            else:
                #body = os.read(self.server_pipe, message_length)
                body = self.server_pipe.read(message_length)

            if field_type is FieldType.METADATA:
                #print(type(body))
                metadata = msgpack.loads(body, raw=False)
            elif field_type in self.image_fields:
                files[self.form_field_map[field_type]].append(body)
            else:
                raise ValueError("Invalid field type: %s" % field_type )

        return metadata, files

    def _send_message(self, message_type, body):
        #print("trying to write to ")
        if self.client_pipe is None:
            #self.client_pipe = os.open(self.client_pipe_path, os.O_WRONLY | os.O_SYNC)
            self.client_pipe = open(self.client_pipe_path, "wb")

        header = self._create_header(message_type, body)
        #print("len header %s" % len(header))
        #print("sending body %s" % body)

        self.client_pipe.write(header + body + self.eom_header)
        #self.client_pipe.write(header + body + self.eom_header)
        #os.fsync(self.client_pipe)
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
        #self._send_message(FieldType.ACTION, ujson.dumps(action).encode('utf8'))
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
            fifo_client_pipe_path=self.client_pipe_path,
        )

        if self.shmkey:
            params['fifo_shm_key'] = self.shmkey
            params['fifo_shm_size'] = self.shm_size

        return params

    def _shm_init(self):
        limit = 2 ** (ctypes.sizeof(ctypes.c_int) * 8 - 1)
        low_range = -limit
        high_range = limit - 1
        for _ in range(10):
            k = random.randint(low_range, high_range)
            if k == 0: # 0 == IPC_PRIVATE
                continue
    

            print("k %s" % k)
            shmid = shmget(k, self.shm_size,  0o777 | IPC_CREAT | IPC_EXCL)
            # on OSX, this call can fail if kern.sysv.shmmax and/or kern.sysv.shmseg is set too low 
            # by default it is set to 4MB, on linux it is set to a number higher than
            # sysctl -w kern.sysv.shmmax=134217728 increases to 128MB on osx
            # the system memory
            print("shmid %s" % shmid)
            print(ctypes.get_errno())
            #shmid = -1
            if shmid != -1:
                self.shmaddr = shmat(shmid, None, 0)
                self.shmkey = k
                self.shmid = shmid
                self.data = (ctypes.c_byte * self.shm_size).from_address(self.shmaddr)
                self.mview = memoryview(self.data)
                break
        if self.shmkey is None:
            warnings.warn("couldn't obtain a shmkey - shared memory won't be used")

    def _free_shm(self):
        if self.shmid != -1:
            print("removing shm")
            shmdt(self.shmaddr)
            shmctl(self.shmid, IPC_RMID, None)
            self.shmid = -1
            self.shmkey = None
    
    def __del__(self):
        self._free_shm()

    def stop(self):
        self.client_pipe.close()
        self.server_pipe.close()
        self._free_shm()
