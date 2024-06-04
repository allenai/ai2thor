import warnings
import os
from ai2thor.controller import Controller

class TestController(Controller):
    def __init__(self, **kwargs):
        self.force_opengl = os.environ.get("FORCE_OPENGL", False)
        if type(self.force_opengl) != bool or self.force_opengl != False:
            self.force_opengl = self.force_opengl.lower() in ("true", "1", "t")
        super().__init__(**kwargs)

    def unity_command(self, width, height, headless):
        command = super().unity_command(width, height, headless)
        # force OpenGLCore to get used so that the tests run in a consistent way
        # With low power graphics cards (such as those in the test environment)
        # Metal behaves in inconsistent ways causing test failures
        
        # This is not needed for cloudrendering
        if self.force_opengl:             
            command.append("-force-glcore")
        return command

def build_controller(**args):
    platform = os.environ.get("TEST_PLATFORM")
    if platform:
        default_args = dict(platform=platform)
    else:
        default_args = dict(local_build=True)

    default_args.update(args)
    # during a ci-build we will get a warning that we are using a commit_id for the
    # build instead of 'local'
    with warnings.catch_warnings():
        warnings.simplefilter("ignore")
        print("args test controller")
        print(default_args)
        c = TestController(**default_args)

    # used for resetting
    c._original_initialization_parameters = c.initialization_parameters
    return c