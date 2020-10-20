import ai2thor.fifo_server
import struct
import json
import numpy as np
import ctypes
from ai2thor.fifo_server import shmat, shmget, shmdt, memcpy

class FifoClient:


    def __init__(self, server_pipe_path, client_pipe_path, shm_key=None, shm_size=None):
        self.server_pipe = None
        self.client_pipe = None
        self.server_pipe_path = server_pipe_path
        self.client_pipe_path = client_pipe_path
        self.has_shm = False
        self.shm_size = 0
        self.shm_offset = 0
        self._shm_init(shm_key, shm_size)
        self.shm_header = struct.pack(ai2thor.fifo_server.FifoServer.header_format, ai2thor.fifo_server.FieldType.SHARED_MEMORY_FIELD, self.shm_offset)

    def _shm_init(self, shm_key, shm_size):
        shmid = shmget(shm_key, shm_size,  0o777) # use ipc_creat
        if shmid != -1:
            self.shmaddr = shmat(shmid, None, 0)
            self.data = (ctypes.c_byte * shm_size).from_address(self.shmaddr)
            self.shm_size = shm_size
            self.has_shm = True

    def shm_free(self):
        return self.shm_size - self.shm_offset
    
    def send(self, field_type, body):
        if self.server_pipe is None:
            self.server_pipe = open(self.server_pipe_path, "wb")

        header = struct.pack(ai2thor.fifo_server.FifoServer.header_format, field_type, len(body))
        if self.has_shm: # and (len(header) + len(body)) < self.shm_free():
            self.data[self.shm_offset: self.shm_offset + len(header)] = header
            self.shm_offset += len(self.shm_header)
            self.data[self.shm_offset: self.shm_offset + 10] = body[:10]
            #print("trying to copy")
            memcpy(self.shmaddr, np.zeros(300000, dtype=np.uint8).ctypes.data, 300000)
            #self.data[self.shm_offset: self.shm_offset + len(body)] = body
            #self.shm_offset += len(body)
            self.server_pipe.write(self.shm_header)
        else:
            print("sending not shared")
            self.server_pipe.write(header + body)

    def send_eom(self):
        if self.server_pipe is None:
            self.server_pipe = open(self.server_pipe_path, "wb")
        header = struct.pack(ai2thor.fifo_server.FifoServer.header_format, ai2thor.fifo_server.FieldType.END_OF_MESSAGE, 0)
        self.server_pipe.write(header)
        self.server_pipe.flush()
        self.shm_offset = 0

    def recv(self):
        #print("trying to receive")
        if self.client_pipe is None:
            self.client_pipe = open(self.client_pipe_path, "rb")
        #print("trying to read")
        j = None
        while True:
            header = self.client_pipe.read(ai2thor.fifo_server.FifoServer.header_size) # field_type + length
            if len(header) == 0:
                print("Read 0 - server closed")
                break
            #print("got header %s" % header)
            #print("header length %s" % len(header))
            field_type_int, field_length = struct.unpack(ai2thor.fifo_server.FifoServer.header_format, header)
            field_type = ai2thor.fifo_server.FifoServer.field_types[field_type_int]
            if field_length > 0: # EOM has length == 0
                body = self.client_pipe.read(field_length)

            if field_type is ai2thor.fifo_server.FieldType.ACTION:
                j = json.loads(body)
            elif field_type is ai2thor.fifo_server.FieldType.END_OF_MESSAGE:
            #    #print("got eom")
                break
            else:
                raise Exception("invalid field %s" % field_type)
        return j

