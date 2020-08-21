import ai2thor.fifo_server
import struct
import json
class FifoClient:


    def __init__(self, server_pipe_path, client_pipe_path):
        self.server_pipe = None
        self.client_pipe = None
        self.server_pipe_path = server_pipe_path
        self.client_pipe_path = client_pipe_path

    def send(self, field_type, body):
        if self.server_pipe is None:
            self.server_pipe = open(self.server_pipe_path, "wb" )
        
        header = struct.pack(ai2thor.fifo_server.FifoServer.header_format, field_type, len(body))

        self.server_pipe.write(header + body)

    def send_eom(self):
        header = struct.pack(ai2thor.fifo_server.FifoServer.header_format, ai2thor.fifo_server.FieldType.END_OF_MESSAGE, 0)
        self.server_pipe.write(header)
        self.server_pipe.flush()

    def recv(self):
        #print("trying to receive")
        if self.client_pipe is None:
            self.client_pipe = open(self.client_pipe_path, "rb" )
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

