# Copyright Allen Institute for Artificial Intelligence 2017
"""
ai2thor.server

Handles all communication with Unity through a Flask service.  Messages
are sent to the controller using a pair of request/response queues.
"""
import json
import os
import select
import struct
import tempfile
import time
from collections import defaultdict
from enum import IntEnum, unique
from io import TextIOWrapper
from typing import Optional

import msgpack

import ai2thor.server
from ai2thor.exceptions import UnityCrashException


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
    METADATA_PATCH = 11
    THIRD_PARTY_DEPTH = 12
    THIRD_PARTY_NORMALS = 13
    THIRD_PARTY_IMAGE_IDS = 14
    THIRD_PARTY_CLASSES = 15
    THIRD_PARTY_FLOW = 16
    END_OF_MESSAGE = 255


class FifoServer(ai2thor.server.Server):
    header_format = "!BI"
    header_size = struct.calcsize(header_format)
    field_types = {f.value: f for f in FieldType}
    server_type = "FIFO"

    def __init__(
        self,
        width: int,
        height: int,
        timeout: Optional[float] = 100.0,
        depth_format=ai2thor.server.DepthFormat.Meters,
        add_depth_noise: bool = False,
    ):

        self.tmp_dir = tempfile.TemporaryDirectory()
        self.server_pipe_path = os.path.join(self.tmp_dir.name, "server.pipe")
        self.client_pipe_path = os.path.join(self.tmp_dir.name, "client.pipe")
        self.server_pipe: Optional[TextIOWrapper] = None
        self.client_pipe: Optional[TextIOWrapper] = None
        self.raw_metadata = None
        self.raw_files = None
        self._last_action_message = None
        # allows us to map the enum to form field names
        # for backwards compatibility
        # this can be removed when the wsgi server is removed
        self.form_field_map = {
            FieldType.RGB_IMAGE: "image",
            FieldType.DEPTH_IMAGE: "image_depth",
            FieldType.CLASSES_IMAGE: "image_classes",
            FieldType.IDS_IMAGE: "image_ids",
            FieldType.NORMALS_IMAGE: "image_normals",
            FieldType.FLOWS_IMAGE: "image_flow",
            FieldType.THIRD_PARTY_IMAGE: "image-thirdParty-camera",
            FieldType.THIRD_PARTY_DEPTH: "image_thirdParty_depth",
            FieldType.THIRD_PARTY_NORMALS: "image_thirdParty_normals",
            FieldType.THIRD_PARTY_IMAGE_IDS: "image_thirdParty_image_ids",
            FieldType.THIRD_PARTY_CLASSES: "image_thirdParty_classes",
            FieldType.THIRD_PARTY_FLOW: "image_thirdParty_flow",
        }

        self.image_fields = {
            FieldType.IDS_IMAGE,
            FieldType.CLASSES_IMAGE,
            FieldType.FLOWS_IMAGE,
            FieldType.NORMALS_IMAGE,
            FieldType.DEPTH_IMAGE,
            FieldType.RGB_IMAGE,
            FieldType.THIRD_PARTY_IMAGE,
            FieldType.THIRD_PARTY_DEPTH,
            FieldType.THIRD_PARTY_NORMALS,
            FieldType.THIRD_PARTY_IMAGE_IDS,
            FieldType.THIRD_PARTY_CLASSES,
            FieldType.THIRD_PARTY_FLOW,
        }

        self.eom_header = self._create_header(FieldType.END_OF_MESSAGE, b"")
        super().__init__(
            width=width,
            height=height,
            timeout=timeout,
            depth_format=depth_format,
            add_depth_noise=add_depth_noise
        )

    def _create_header(self, message_type, body):
        return struct.pack(self.header_format, message_type, len(body))

    def _read_with_timeout(self, server_pipe, message_size: int, timeout: Optional[float]):
        if timeout is None:
            return server_pipe.read(message_size)

        start_t = time.time()
        message = b""

        while message_size > 0:
            r, w, e = select.select([server_pipe], [], [], timeout)
            if server_pipe in r:
                part = os.read(server_pipe.fileno(), message_size)
                message_size -= len(part)
                message = message + part

            cur_t = time.time()
            if timeout is not None and cur_t - start_t > timeout:
                break

        if message_size != 0:
            raise TimeoutError(f"Reading from AI2-THOR backend timed out (using {timeout}s) timeout.")

        return message

    def _recv_message(self, timeout: Optional[float]):
        if self.server_pipe is None:
            self.server_pipe = open(self.server_pipe_path, "rb")

        metadata = None
        files = defaultdict(list)
        while True:
            header = self._read_with_timeout(
                server_pipe=self.server_pipe,
                message_size=self.header_size,
                timeout=self.timeout if timeout is None else timeout
            )  # message type + length
            if len(header) == 0:
                self.unity_proc.wait(timeout=5)
                returncode = self.unity_proc.returncode
                message = (
                    "Unity process has exited - check "
                    "~/.config/unity3d/Allen\ Institute\ for\ "
                    "Artificial\ Intelligence/AI2-THOR/Player.log for errors. "
                    f"Last action message: %s, returncode=%s"
                    % (self._last_action_message, self.unity_proc.returncode)
                )
                # we don't want to restart all process exits since its possible that a user
                # kills off a Unity process with SIGTERM to end a training run
                # SIGABRT is the returncode for when Unity crashes due to a segfault
                if returncode in [-6, -11]:  # SIGABRT, SIGSEGV
                    raise UnityCrashException(message)
                else:
                    raise Exception(message)

            if header[0] == FieldType.END_OF_MESSAGE.value:
                # print("GOT EOM")
                break

            # print("got header %s" % header)
            field_type_int, message_length = struct.unpack(self.header_format, header)
            field_type = self.field_types[field_type_int]

            body = self._read_with_timeout(
                server_pipe=self.server_pipe,
                message_size=message_length,
                timeout=self.timeout if timeout is None else timeout
            )

            # print("field type")
            # print(field_type)
            if field_type is FieldType.METADATA:
                # print("body length %s" % len(body))
                # print(body)
                metadata = msgpack.loads(body, raw=False, strict_map_key=False)
            elif field_type is FieldType.METADATA_PATCH:
                metadata_patch = msgpack.loads(body, raw=False, strict_map_key=False)
                agents = self.raw_metadata["agents"]
                metadata = dict(
                    agents=[{} for i in range(len(agents))],
                    thirdPartyCameras=self.raw_metadata["thirdPartyCameras"],
                    sequenceId=self.sequence_id,
                    activeAgentId=metadata_patch["agentId"],
                )
                for i in range(len(agents)):
                    metadata["agents"][i].update(agents[i])

                metadata["agents"][metadata_patch["agentId"]].update(metadata_patch)
                files = self.raw_files
            elif field_type in self.image_fields:
                files[self.form_field_map[field_type]].append(body)
            else:
                raise ValueError("Invalid field type: %s" % field_type)

        self.raw_metadata = metadata
        self.raw_files = files

        return metadata, files

    def _send_message(self, message_type, body):
        # print("trying to write to ")
        if self.client_pipe is None:
            self.client_pipe = open(self.client_pipe_path, "wb")

        header = self._create_header(message_type, body)
        # print("len header %s" % len(header))
        # print("sending body %s" % body)

        # used for debugging in case of an error
        self._last_action_message = body

        self.client_pipe.write(header + body + self.eom_header)
        self.client_pipe.flush()

    def receive(self, timeout: Optional[float] = None):

        metadata, files = self._recv_message(
            timeout=self.timeout if timeout is None else timeout
        )

        if metadata is None:
            raise ValueError("no metadata received from recv_message")

        return self.create_event(metadata, files)

    def send(self, action):
        # print("got action to send")
        if "sequenceId" in action:
            self.sequence_id = action["sequenceId"]
        else:
            self.sequence_id += 1
            action["sequenceId"] = self.sequence_id
        # print(action)

        # need to switch this to msgpack
        self._send_message(
            FieldType.ACTION,
            json.dumps(action, cls=ai2thor.server.NumpyAwareEncoder).encode("utf8"),
        )

    def start(self):
        os.mkfifo(self.server_pipe_path)
        os.mkfifo(self.client_pipe_path)
        self.started = True

    # params to pass up to unity
    def unity_params(self):
        params = dict(
            fifo_server_pipe_path=self.server_pipe_path,
            fifo_client_pipe_path=self.client_pipe_path,
        )
        return params

    def stop(self):
        if self.client_pipe is not None:
            self.client_pipe.close()

        if self.server_pipe is not None:
            self.server_pipe.close()

        if self.unity_proc is not None and self.unity_proc.poll() is None:
            self.unity_proc.kill()
