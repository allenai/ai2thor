# Copyright Allen Institute for Artificial Intelligence 2017
"""
ai2thor.server

Handles all communication with Unity through a Flask service.  Messages
are sent to the controller using a pair of request/response queues.
"""
import ai2thor.server
import json
import logging
import threading
import os

try:
    from queue import Queue, Empty
except ImportError:
    from Queue import Queue, Empty

import time

from flask import Flask, request, make_response, abort
import werkzeug
import werkzeug.serving
import werkzeug.http

logging.getLogger("werkzeug").setLevel(logging.ERROR)

werkzeug.serving.WSGIRequestHandler.protocol_version = "HTTP/1.1"

# get with timeout to allow quit
def queue_get(que, unity_proc=None):
    res = None
    attempts = 0
    max_attempts = 200
    while True:
        try:
            res = que.get(block=True, timeout=0.5)
            break
        except Empty:
            attempts += 1

            # we poll here for the unity proc in the event that it has
            # exited otherwise we would wait indefinetly for the queue
            if unity_proc:
                if unity_proc.poll() is not None:
                    raise Exception("Unity process exited %s" % unity_proc.returncode)

                # no Action should take > 100s to complete, so we assume that
                # something has gone wrong within Unity
                # max_attempts can also be triggered if an Exception is thrown from
                # within the thread used to run the wsgi server, in which case
                # Unity will receive a corrupted response
                if attempts >= max_attempts:
                    raise Exception(
                        "Could not get a message from the queue after %s attempts "
                        % attempts
                    )
    return res


class BufferedIO(object):
    def __init__(self, wfile):
        self.wfile = wfile
        self.data = []

    def write(self, output):
        self.data.append(output)

    def flush(self):
        self.wfile.write(b"".join(self.data))
        self.wfile.flush()

    def close(self):
        return self.wfile.close()

    @property
    def closed(self):
        return self.wfile.closed


class ThorRequestHandler(werkzeug.serving.WSGIRequestHandler):
    def run_wsgi(self):
        old_wfile = self.wfile
        self.wfile = BufferedIO(self.wfile)
        result = super(ThorRequestHandler, self).run_wsgi()
        self.wfile = old_wfile
        return result


class MultipartFormParser(object):
    @staticmethod
    def get_boundary(request_headers):
        for h, value in request_headers:
            if h == "Content-Type":
                ctype, ct_opts = werkzeug.http.parse_options_header(value)
                boundary = ct_opts["boundary"].encode("ascii")
                return boundary
        return None

    def __init__(self, data, boundary):

        self.form = {}
        self.files = {}

        full_boundary = b"--" + boundary
        mid_boundary = b"\r\n" + full_boundary
        view = memoryview(data)
        i = data.find(full_boundary) + len(full_boundary)
        while i >= 0:
            next_offset = data.find(mid_boundary, i)
            if next_offset < 0:
                break
            headers_offset = i + 2  # add 2 for CRLF
            body_offset = data.find(b"\r\n\r\n", headers_offset)
            raw_headers = view[headers_offset:body_offset]
            body = view[body_offset + 4 : next_offset]
            i = next_offset + len(mid_boundary)

            headers = {}
            for header in raw_headers.tobytes().decode("ascii").strip().split("\r\n"):

                k, v = header.split(":")
                headers[k.strip()] = v.strip()

            ctype, ct_opts = werkzeug.http.parse_options_header(headers["Content-Type"])
            cdisp, cd_opts = werkzeug.http.parse_options_header(
                headers["Content-disposition"]
            )
            assert cdisp == "form-data"

            if "filename" in cd_opts:
                if cd_opts["name"] not in self.files:
                    self.files[cd_opts["name"]] = []

                self.files[cd_opts["name"]].append(body)

            else:
                if ctype == "text/plain" and "charset" in ct_opts:
                    body = body.tobytes().decode(ct_opts["charset"])
                if cd_opts["name"] not in self.form:
                    self.form[cd_opts["name"]] = []

                self.form[cd_opts["name"]].append(body)


class WsgiServer(ai2thor.server.Server):

    server_type = "WSGI"

    def __init__(
        self,
        host,
        port=0,
        threaded=False,
        depth_format=ai2thor.server.DepthFormat.Meters,
        add_depth_noise=False,
        width=300,
        height=300,
    ):

        app = Flask(
            __name__,
            template_folder=os.path.realpath(
                os.path.join(
                    os.path.dirname(os.path.abspath(__file__)), "..", "templates"
                )
            ),
        )

        self.request_queue = Queue(maxsize=1)
        self.response_queue = Queue(maxsize=1)
        self.app = app
        self.app.config.update(
            PROPAGATE_EXCEPTIONS=False, JSONIFY_PRETTYPRINT_REGULAR=False
        )
        self.port = port
        self.last_rate_timestamp = time.time()
        self.frame_counter = 0
        self.debug_frames_per_interval = 50
        self.unity_proc = None
        self.wsgi_server = werkzeug.serving.make_server(
            host,
            self.port,
            self.app,
            threaded=threaded,
            request_handler=ThorRequestHandler,
        )
        # used to ensure that we are receiving frames for the action we sent
        super().__init__(width, height, depth_format, add_depth_noise)

        @app.route("/ping", methods=["get"])
        def ping():
            return "pong"

        @app.route("/train", methods=["post"])
        def train():

            action_returns = []

            if request.headers["Content-Type"].split(";")[0] == "multipart/form-data":
                form = MultipartFormParser(
                    request.get_data(),
                    MultipartFormParser.get_boundary(request.headers),
                )
                metadata = json.loads(form.form["metadata"][0])
                # backwards compatibility
                if (
                    "actionReturns" in form.form
                    and len(form.form["actionReturns"][0]) > 0
                ):
                    action_returns = json.loads(form.form["actionReturns"][0])
                token = form.form["token"][0]
            else:
                form = request
                metadata = json.loads(form.form["metadata"])
                # backwards compatibility
                if "actionReturns" in form.form and len(form.form["actionReturns"]) > 0:
                    action_returns = json.loads(form.form["actionReturns"])
                token = form.form["token"]

            if self.client_token and token != self.client_token:
                abort(403)

            if self.frame_counter % self.debug_frames_per_interval == 0:
                now = time.time()
                # rate = self.debug_frames_per_interval / float(now - self.last_rate_timestamp)
                self.last_rate_timestamp = now
                # import datetime
                # print("%s %s/s" % (datetime.datetime.now().isoformat(), rate))

            for i, a in enumerate(metadata["agents"]):
                if "actionReturn" not in a and i < len(action_returns):
                    a["actionReturn"] = action_returns[i]

            event = self.create_event(metadata, form.files)

            self.request_queue.put_nowait(event)

            self.frame_counter += 1

            next_action = queue_get(self.response_queue)
            if "sequenceId" not in next_action:
                self.sequence_id += 1
                next_action["sequenceId"] = self.sequence_id
            else:
                self.sequence_id = next_action["sequenceId"]

            resp = make_response(
                json.dumps(next_action, cls=ai2thor.server.NumpyAwareEncoder)
            )

            return resp

    def _start_server_thread(self):
        self.wsgi_server.serve_forever()

    def start(self):
        self.started = True
        self.server_thread = threading.Thread(target=self._start_server_thread)
        self.server_thread.daemon = True
        self.server_thread.start()

    def receive(self):
        return queue_get(self.request_queue, unity_proc=self.unity_proc)

    def send(self, action):
        assert self.request_queue.empty()
        self.response_queue.put_nowait(action)

    # params to pass up to unity
    def unity_params(self):
        host, port = self.wsgi_server.socket.getsockname()

        params = dict(host=host, port=str(port))
        return params

    def stop(self):
        self.send({})
        self.wsgi_server.shutdown()
