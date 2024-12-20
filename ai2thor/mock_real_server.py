from flask import Flask, request, make_response
import numpy as np
import random
import msgpack
import werkzeug
import werkzeug.serving
import werkzeug.http


def random_image(height, width):
    img = np.zeros(height * width * 3, dtype=np.uint8).reshape(height, width, 3)
    img[:, :, 0] = random.randint(0, 255)
    img[:, :, 1] = random.randint(0, 255)
    img[:, :, 2] = random.randint(0, 255)

    return img


class MockServer(object):
    def __init__(self, height, width):
        app = Flask(__name__)

        self.height = height
        self.width = width

        @app.route("/ping", methods=["GET"])
        def ping():
            return make_response("PONG")

        @app.route("/start", methods=["POST"])
        def start():
            return make_response(msgpack.packb({"status": 200}, use_bin_type=True))

        @app.route("/reset", methods=["POST"])
        def reset():
            return make_response(msgpack.packb({"status": 200}, use_bin_type=True))

        @app.route("/step", methods=["POST"])
        def step():
            content = request.json

            metadata = {
                "sequenceId": content["sequenceId"],
                "agents": [
                    {
                        "agentId": 0,
                        "screenHeight": self.height,
                        "screenWidth": self.width,
                        "lastAction": content["action"],
                        "lastActionSuccess": True,
                    }
                ],
            }

            result = {
                "image": [random_image(self.height, self.width).tostring()],
                "image_depth": [],
                "metadata": metadata,
            }
            out = msgpack.packb(result, use_bin_type=True)

            return make_response(out)

        self.host = "127.0.0.1"
        self.port = 9200
        app.config.update(PROPAGATE_EXCEPTIONS=True, JSONIFY_PRETTYPRINT_REGULAR=False)
        self.wsgi_server = werkzeug.serving.make_server(self.host, self.port, app)

    def start(self):
        self.wsgi_server.serve_forever()
