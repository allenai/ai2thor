from types import SimpleNamespace

from ai2thor.util import metrics

INITIAL_POSITION = dict(x=1.0, y=0.9, z=2.0)
TARGET_POSITION = dict(x=0.25, y=0.9, z=3.75)
CORNERS = [dict(x=1.0, y=0.9, z=2.0), dict(x=0.25, y=0.9, z=3.75)]


class RecordingController:
    """Stands in for a Controller, recording the action sent to `step`."""

    def __init__(self):
        self.step_args = None

    def step(self, args):
        self.step_args = args
        return SimpleNamespace(
            metadata=dict(lastActionSuccess=True, actionReturn=dict(corners=CORNERS))
        )


def test_get_shortest_path_to_point_sends_target_vector3():
    # Regression test for
    # https://github.com/allenai/ai2thor/issues/1155 - the action was sent with
    # top-level `x`/`y`/`z` keys, but the Unity action signature is
    # `GetShortestPathToPoint(Vector3 position, Vector3 target, float allowedError)`,
    # so dispatch rejected the action with
    # "called with invalid arguments: 'x', 'y', 'z'".
    controller = RecordingController()

    corners = metrics.get_shortest_path_to_point(controller, INITIAL_POSITION, TARGET_POSITION)

    assert corners == CORNERS
    assert controller.step_args["action"] == "GetShortestPathToPoint"
    assert controller.step_args["position"] == INITIAL_POSITION
    assert controller.step_args["target"] == TARGET_POSITION
    assert "x" not in controller.step_args
    assert "y" not in controller.step_args
    assert "z" not in controller.step_args


def test_get_shortest_path_to_point_sends_allowed_error():
    controller = RecordingController()

    metrics.get_shortest_path_to_point(
        controller, INITIAL_POSITION, TARGET_POSITION, allowed_error=0.03
    )

    assert controller.step_args["allowedError"] == 0.03
