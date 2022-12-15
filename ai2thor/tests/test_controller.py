import ai2thor.controller
from ai2thor.server import Event
from ai2thor.platform import CloudRendering, Linux64
import pytest
import numpy as np
import warnings
import os
import math


def fake_linux64_exists(self):
    if self.platform.name() == "Linux64":
        return True
    else:
        return False


@classmethod
def fake_invalid_cr_validate(cls, request):
    return ["Missing libvulkan1."]


@classmethod
def fake_invalid_linux64_validate(cls, request):
    return ["No display found. "]


def fake_cr_exists(self):
    if self.platform.name() == "CloudRendering":
        return True
    else:
        return False


def fake_not_exists(self):
    return False

def fake_find_platform_builds(self, canditate_platorms, request, commits, releases_dir, local_build):
    return []

def fake_exists(self):
    return True


def fake_linux_system():
    return "Linux"


def fake_darwin_system():
    return "Darwin"


def noop_download(self):
    pass

def select_platforms_linux_cr(request):
    return (Linux64, CloudRendering)

def select_platforms_cr(request):
    return (CloudRendering, )

@classmethod
def fake_validate(cls, request):
    return []


class FakeServer(object):
    def __init__(self):
        self.request_queue = FakeQueue()
        self.response_queue = FakeQueue()

    def send(self, action):
        assert self.request_queue.empty()
        self.response_queue.put_nowait(action)

    def receive(self):
        return self.request_queue.get(False, 0)


class FakeQueue(object):
    def __init__(self):
        self.value = None

    def put_nowait(self, v):
        assert self.value is None
        self.value = v

    def get(self, block=False, timeout=0):
        v = self.value
        self.value = None
        return v

    # always return empty so that we pass
    def empty(self):
        return True


def controller(**args):

    # delete display so the tests can run on Linux
    if "DISPLAY" in os.environ:
        del os.environ["DISPLAY"]

    # during a ci-build we will get a warning that we are using a commit_id for the
    # build instead of 'local'
    default_args = dict(download_only=True)
    default_args.update(args)
    with warnings.catch_warnings():
        warnings.simplefilter("ignore")
        c = ai2thor.controller.Controller(**default_args)
        c.server = FakeServer()

    return c


def test_osx_build_missing(mocker):
    mocker.patch("ai2thor.controller.platform_system", fake_darwin_system)
    mocker.patch("ai2thor.controller.Controller.find_platform_builds", fake_find_platform_builds)

    with pytest.raises(Exception) as ex:
        c = controller()

    assert str(ex.value).startswith("No build exists for arch=Darwin platforms=OSXIntel64 and commits:")


def test_osx_build_invalid_commit_id(mocker):
    mocker.patch("ai2thor.controller.platform_system", fake_darwin_system)
    mocker.patch("ai2thor.controller.ai2thor.build.Build.exists", fake_not_exists)

    fake_commit_id = "1234567TEST"
    with pytest.raises(ValueError) as ex:
        c = controller(commit_id=fake_commit_id)

    assert (
        str(ex.value)
        == "Invalid commit_id: %s - no build exists for arch=Darwin platforms=OSXIntel64" % fake_commit_id
    )


def test_osx_build(mocker):
    mocker.patch("ai2thor.controller.platform_system", fake_darwin_system)
    mocker.patch("ai2thor.controller.ai2thor.build.Build.exists", fake_exists)
    mocker.patch("ai2thor.controller.ai2thor.build.Build.download", noop_download)

    fake_commit_id = "1234567TEST"
    c = controller(commit_id=fake_commit_id)
    assert c._build.platform.name() == "OSXIntel64"
    assert c._build.commit_id == fake_commit_id


def test_linux_explicit_xdisplay(mocker):
    mocker.patch("ai2thor.controller.platform_system", fake_linux_system)
    mocker.patch("ai2thor.controller.ai2thor.build.Build.exists", fake_exists)
    mocker.patch("ai2thor.controller.ai2thor.build.Build.download", noop_download)
    mocker.patch("ai2thor.controller.ai2thor.platform.Linux64.validate", fake_validate)

    fake_commit_id = "1234567TEST"
    c = controller(commit_id=fake_commit_id, x_display="75.9")
    assert c._build.platform.name() == "Linux64"
    assert c._build.commit_id == fake_commit_id


def test_linux_invalid_linux64_invalid_cr(mocker):

    mocker.patch("ai2thor.controller.platform_system", fake_linux_system)
    mocker.patch("ai2thor.controller.ai2thor.build.Build.exists", fake_exists)
    mocker.patch("ai2thor.controller.ai2thor.build.Build.download", noop_download)
    mocker.patch("ai2thor.controller.ai2thor.platform.select_platforms", select_platforms_linux_cr)
    mocker.patch(
        "ai2thor.controller.ai2thor.platform.CloudRendering.validate",
        fake_invalid_cr_validate,
    )
    mocker.patch(
        "ai2thor.controller.ai2thor.platform.Linux64.validate",
        fake_invalid_linux64_validate,
    )

    fake_commit_id = "1234567TEST"
    with pytest.raises(Exception) as excinfo:
        c = controller(commit_id=fake_commit_id)

    assert str(excinfo.value).startswith(
        "The following builds were found, but had missing dependencies. Only one valid platform is required to run AI2-THOR."
    )


def test_linux_invalid_linux64_valid_cr(mocker):

    mocker.patch("ai2thor.controller.platform_system", fake_linux_system)
    mocker.patch("ai2thor.controller.ai2thor.build.Build.exists", fake_exists)
    mocker.patch("ai2thor.controller.ai2thor.build.Build.download", noop_download)
    mocker.patch("ai2thor.controller.ai2thor.platform.select_platforms", select_platforms_linux_cr)
    mocker.patch(
        "ai2thor.controller.ai2thor.platform.CloudRendering.validate", fake_validate
    )
    mocker.patch(
        "ai2thor.controller.ai2thor.platform.Linux64.validate",
        fake_invalid_linux64_validate,
    )

    mocker.patch("ai2thor.platform.CloudRendering.enabled", True)

    fake_commit_id = "1234567TEST"
    c = controller(commit_id=fake_commit_id)
    assert c._build.platform.name() == "CloudRendering"
    assert c._build.commit_id == fake_commit_id


def test_linux_valid_linux64_valid_cloudrendering(mocker):

    mocker.patch("ai2thor.controller.platform_system", fake_linux_system)
    mocker.patch("ai2thor.controller.ai2thor.build.Build.exists", fake_exists)
    mocker.patch("ai2thor.controller.ai2thor.build.Build.download", noop_download)
    mocker.patch(
        "ai2thor.controller.ai2thor.platform.CloudRendering.validate", fake_validate
    )
    mocker.patch("ai2thor.controller.ai2thor.platform.Linux64.validate", fake_validate)

    fake_commit_id = "1234567TEST"
    c = controller(commit_id=fake_commit_id)
    assert c._build.platform.name() == "Linux64"
    assert c._build.commit_id == fake_commit_id


def test_linux_valid_linux64_valid_cloudrendering_enabled_cr(mocker):

    mocker.patch("ai2thor.controller.platform_system", fake_linux_system)
    mocker.patch("ai2thor.controller.ai2thor.build.Build.exists", fake_exists)
    mocker.patch("ai2thor.controller.ai2thor.build.Build.download", noop_download)
    mocker.patch("ai2thor.controller.ai2thor.platform.select_platforms", select_platforms_cr)
    mocker.patch(
        "ai2thor.controller.ai2thor.platform.CloudRendering.validate", fake_validate
    )
    mocker.patch("ai2thor.controller.ai2thor.platform.Linux64.validate", fake_validate)
    mocker.patch("ai2thor.platform.CloudRendering.enabled", True)
    mocker.patch("ai2thor.platform.Linux64.enabled", False)

    fake_commit_id = "1234567TEST"
    c = controller(commit_id=fake_commit_id)
    assert c._build.platform.name() == "CloudRendering"
    assert c._build.commit_id == fake_commit_id


def test_linux_valid_linux64_invalid_cloudrendering(mocker):

    mocker.patch("ai2thor.controller.platform_system", fake_linux_system)
    mocker.patch("ai2thor.controller.ai2thor.build.Build.exists", fake_exists)
    mocker.patch("ai2thor.controller.ai2thor.build.Build.download", noop_download)
    mocker.patch(
        "ai2thor.controller.ai2thor.platform.CloudRendering.validate",
        fake_invalid_cr_validate,
    )
    mocker.patch("ai2thor.controller.ai2thor.platform.Linux64.validate", fake_validate)

    fake_commit_id = "1234567TEST"
    c = controller(commit_id=fake_commit_id)
    assert c._build.platform.name() == "Linux64"
    assert c._build.commit_id == fake_commit_id


def test_linux_missing_linux64(mocker):

    mocker.patch("ai2thor.controller.platform_system", fake_linux_system)
    mocker.patch("ai2thor.controller.ai2thor.build.Build.exists", fake_cr_exists)
    mocker.patch("ai2thor.controller.ai2thor.build.Build.download", noop_download)
    mocker.patch(
        "ai2thor.controller.ai2thor.platform.CloudRendering.validate", fake_validate
    )
    mocker.patch("ai2thor.platform.CloudRendering.enabled", True)
    mocker.patch("ai2thor.controller.ai2thor.platform.select_platforms", select_platforms_linux_cr)

    fake_commit_id = "1234567TEST"
    c = controller(commit_id=fake_commit_id)
    assert c._build.platform.name() == "CloudRendering"
    assert c._build.commit_id == fake_commit_id


def test_linux_missing_cloudrendering(mocker):

    mocker.patch("ai2thor.controller.platform_system", fake_linux_system)
    mocker.patch("ai2thor.controller.ai2thor.build.Build.exists", fake_linux64_exists)
    mocker.patch("ai2thor.controller.ai2thor.build.Build.download", noop_download)
    mocker.patch("ai2thor.controller.ai2thor.platform.Linux64.validate", fake_validate)

    fake_commit_id = "1234567TEST"
    c = controller(commit_id=fake_commit_id)
    assert c._build.platform.name() == "Linux64"
    assert c._build.commit_id == fake_commit_id


def test_distance():
    point1 = dict(x=1.5, z=2.5)
    point2 = dict(x=4.33, z=7.5)
    point3 = dict(x=2.5, z=3.5)
    assert ai2thor.controller.distance(point1, point2) == 5.745337239884183
    assert ai2thor.controller.distance(point1, point1) == 0.0
    assert ai2thor.controller.distance(point1, point3) == math.sqrt(2.0)


def test_key_for_point():
    assert ai2thor.controller.key_for_point(2.567, -3.43) == "2.6 -3.4"


def test_invalid_commit(mocker):
    caught_exception = False
    try:
        c = ai2thor.controller.Controller(commit_id="1234567x")
    except ValueError as e:
        caught_exception = True

    assert caught_exception, "invalid commit id should throw ValueError"


def test_scene_names(mocker):
    mocker.patch("ai2thor.controller.platform_system", fake_darwin_system)
    mocker.patch("ai2thor.controller.ai2thor.build.Build.exists", fake_exists)
    mocker.patch("ai2thor.controller.ai2thor.build.Build.download", noop_download)
    c = controller()
    assert len(c.scene_names()) == 195
    assert len(c.ithor_scenes()) == 120
    assert len(c.robothor_scenes()) == 195 - 120


def test_invalid_action(mocker):
    mocker.patch("ai2thor.controller.platform_system", fake_darwin_system)
    mocker.patch("ai2thor.controller.ai2thor.build.Build.exists", fake_exists)
    mocker.patch("ai2thor.controller.ai2thor.build.Build.download", noop_download)
    fake_event = Event(
        dict(
            screenWidth=300,
            screenHeight=300,
            colors=[],
            lastActionSuccess=False,
            errorCode="InvalidAction",
            errorMessage="Invalid method: moveaheadbadmethod",
        )
    )
    c = controller()
    c.last_event = fake_event
    action1 = dict(action="MoveaheadbadMethod")
    c.server.request_queue.put_nowait(fake_event)

    with pytest.raises(ValueError) as excinfo:
        c.step(action1, raise_for_failure=True)
    assert excinfo.value.args == ("Invalid method: moveaheadbadmethod",)


def test_fix_visibility_distance_env(mocker):
    mocker.patch("ai2thor.controller.platform_system", fake_darwin_system)
    mocker.patch("ai2thor.controller.ai2thor.build.Build.exists", fake_exists)
    mocker.patch("ai2thor.controller.ai2thor.build.Build.download", noop_download)
    try:
        os.environ["AI2THOR_VISIBILITY_DISTANCE"] = "2.0"
        fake_event = Event(
            dict(screenWidth=300, screenHeight=300, colors=[], lastActionSuccess=True)
        )
        c = controller()
        c.last_event = fake_event
        action1 = dict(action="Initialize", gridSize=0.25)
        c.server.request_queue.put_nowait(fake_event)
        c.step(action1)
        filtered_action = c.server.response_queue.get()
        assert filtered_action == {
            "action": "Initialize",
            "gridSize": 0.25,
            "visibilityDistance": 2.0,
        }
    finally:
        del os.environ["AI2THOR_VISIBILITY_DISTANCE"]


def test_raise_for_failure(mocker):
    mocker.patch("ai2thor.controller.platform_system", fake_darwin_system)
    mocker.patch("ai2thor.controller.ai2thor.build.Build.exists", fake_exists)
    mocker.patch("ai2thor.controller.ai2thor.build.Build.download", noop_download)

    fake_event = Event(
        dict(
            screenWidth=300,
            screenHeight=300,
            colors=[],
            lastActionSuccess=False,
            errorCode="NotOpen",
        )
    )
    c = controller()
    c.last_event = fake_event
    action1 = dict(action="MoveAhead")
    c.server.request_queue.put_nowait(fake_event)

    with pytest.raises(RuntimeError):
        c.step(action1, raise_for_failure=True)


def test_failure(mocker):
    mocker.patch("ai2thor.controller.platform_system", fake_darwin_system)
    mocker.patch("ai2thor.controller.ai2thor.build.Build.exists", fake_exists)
    mocker.patch("ai2thor.controller.ai2thor.build.Build.download", noop_download)
    fake_event = Event(
        dict(
            screenWidth=300,
            screenHeight=300,
            colors=[],
            lastActionSuccess=False,
            errorCode="NotOpen",
        )
    )
    c = controller()
    c.last_event = fake_event
    action1 = dict(action="MoveAhead")
    c.server.request_queue.put_nowait(fake_event)
    e = c.step(action1)
    assert c.last_action == action1
    assert not e.metadata["lastActionSuccess"]


def test_last_action(mocker):
    mocker.patch("ai2thor.controller.platform_system", fake_darwin_system)
    mocker.patch("ai2thor.controller.ai2thor.build.Build.exists", fake_exists)
    mocker.patch("ai2thor.controller.ai2thor.build.Build.download", noop_download)
    fake_event = Event(
        dict(screenWidth=300, screenHeight=300, colors=[], lastActionSuccess=True)
    )
    c = controller()
    c.last_event = fake_event
    action1 = dict(action="RotateRight")
    c.server.request_queue.put_nowait(fake_event)
    e = c.step(action1)
    assert c.last_action == action1
    assert e.metadata["lastActionSuccess"]

    c = controller()
    c.last_event = fake_event
    action2 = dict(action="RotateLeft")
    c.server.request_queue.put_nowait(fake_event)
    e = c.step(action2)
    assert c.last_action == action2
    assert e.metadata["lastActionSuccess"]

def test_unity_command_force_device_index(mocker):
    pass

    # TODO: this test is no longer valid as the mapping between CUDA/Vulkan
    #   devices and CUDA devices is more arbitrary than we first believed.
    #   We should find a way to test this in a more robust way.

    # mocker.patch("ai2thor.controller.platform_system", fake_linux_system)
    # mocker.patch("ai2thor.controller.ai2thor.build.Build.exists", fake_exists)
    # mocker.patch("ai2thor.controller.ai2thor.build.Build.download", noop_download)
    # mocker.patch("ai2thor.controller.ai2thor.platform.select_platforms", select_platforms_linux_cr)
    # mocker.patch(
    #     "ai2thor.controller.ai2thor.platform.CloudRendering.validate", fake_validate
    # )
    # mocker.patch(
    #     "ai2thor.controller.ai2thor.platform.Linux64.validate",
    #     fake_invalid_linux64_validate,
    # )
    #
    # mocker.patch("ai2thor.platform.CloudRendering.enabled", True)
    # original_visible_devices = os.environ.get("CUDA_VISIBLE_DEVICES")
    # try:
    #     os.environ["CUDA_VISIBLE_DEVICES"] = "2,3,4"
    #
    #     c = controller(platform=CloudRendering, gpu_device=1)
    #     assert c.unity_command(650, 550, False) == [
    #         c._build.executable_path,
    #         "-screen-fullscreen",
    #         "0",
    #         "-screen-quality",
    #         "7",
    #         "-screen-width",
    #         "650",
    #         "-screen-height",
    #         "550",
    #         '-force-device-index',
    #         '4'
    #     ]
    # finally:
    #     if original_visible_devices:
    #         os.environ["CUDA_VISIBLE_DEVICES"] = original_visible_devices
    #     else:
    #         del os.environ["CUDA_VISIBLE_DEVICES"]
    #
    # c = controller(platform=CloudRendering, gpu_device=5)
    # assert c.unity_command(650, 550, False) == [
    #     c._build.executable_path,
    #     "-screen-fullscreen",
    #     "0",
    #     "-screen-quality",
    #     "7",
    #     "-screen-width",
    #     "650",
    #     "-screen-height",
    #     "550",
    #     '-force-device-index',
    #     '6'
    # ]
    #
    # c = controller(platform=CloudRendering, gpu_device=0)
    # assert c.unity_command(650, 550, False) == [
    #     c._build.executable_path,
    #     "-screen-fullscreen",
    #     "0",
    #     "-screen-quality",
    #     "7",
    #     "-screen-width",
    #     "650",
    #     "-screen-height",
    #     "550",
    #     '-force-device-index',
    #     '0'
    # ]



def test_unity_command(mocker):
    mocker.patch("ai2thor.controller.platform_system", fake_linux_system)
    mocker.patch("ai2thor.controller.ai2thor.build.Build.exists", fake_exists)
    mocker.patch("ai2thor.controller.ai2thor.build.Build.download", noop_download)
    mocker.patch("ai2thor.controller.ai2thor.platform.Linux64.validate", fake_validate)
    c = controller()
    assert c.unity_command(650, 550, False) == [
        c._build.executable_path,
        "-screen-fullscreen",
        "0",
        "-screen-quality",
        "7",
        "-screen-width",
        "650",
        "-screen-height",
        "550",
    ]

    c = controller(fullscreen=True, quality="Low")
    assert c.unity_command(650, 550, False) == [
        c._build.executable_path,
        "-screen-fullscreen",
        "1",
        "-screen-quality",
        "2",
        "-screen-width",
        "650",
        "-screen-height",
        "550",
    ]
