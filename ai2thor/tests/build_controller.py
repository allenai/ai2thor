import warnings
from ai2thor.controller import Controller

class TestController(Controller):
    def unity_command(self, width, height, headless):
        command = super().unity_command(width, height, headless)
        # force OpenGLCore to get used so that the tests run in a consistent way
        # With low power graphics cards (such as those in the test environment)
        # Metal behaves in inconsistent ways causing test failures
        command.append("-force-glcore")
        return command

def build_controller(**args):
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