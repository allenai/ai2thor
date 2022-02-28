import Xlib.display
import glob
import warnings
import os
import ctypes.util
import xml.etree.ElementTree


class Request:
    def __init__(self, system, width, height, x_display, headless):
        self.system = system
        self.width = width
        self.height = height
        self.x_display = x_display
        self.headless = headless


class BasePlatform:

    enabled = True

    @classmethod
    def validate(cls, r):
        return []

    @classmethod
    def dependency_instructions(cls, request):
        return None

    @classmethod
    def is_valid(cls, request):
        return len(cls.validate(request)) == 0

    @classmethod
    def name(cls):
        return cls.__name__

    @classmethod
    def launch_env(cls, width, height, x_display):
        return {}


class BaseLinuxPlatform(BasePlatform):
    @classmethod
    def executable_path(cls, base_dir, name):
        return os.path.join(base_dir, name)

    @classmethod
    def old_executable_path(cls, base_dir, name):
        return cls.executable_path(base_dir, name)


class Linux64(BaseLinuxPlatform):
    @classmethod
    def dependency_instructions(cls, request):
        message = "Linux64 requires a X11 server to be running with GLX. "

        displays = cls._valid_x_displays(request.width, request.height)
        if displays:
            message += "The following valid displays were found %s" % (
                ", ".join(displays)
            )
        else:
            message += "If you have a NVIDIA GPU, please run: sudo ai2thor-xorg start"

        return message

    @classmethod
    def _select_x_display(cls, width, height):

        valid_displays = cls._valid_x_displays(width, height)
        if valid_displays:
            return valid_displays[0]
        else:
            return None

    @classmethod
    def launch_env(cls, width, height, x_display):
        env = dict(DISPLAY=x_display)
        if env["DISPLAY"] is None:
            env["DISPLAY"] = cls._select_x_display(width, height)

        return env

    @classmethod
    def _validate_screen(cls, display_screen_str, width, height):
        errors = []
        try:
            disp_screen = Xlib.display.Display(
                display_screen_str
            )  # display_screen_str will have the format ":0.1"

            screen_parts = display_screen_str.split(".")
            if len(screen_parts) > 1:
                # this Xlib.display will find a valid screen if an
                # invalid screen was passed in (e.g. :0.9999999 -> :0.1)
                if screen_parts[1] != str(disp_screen.get_default_screen()):
                    errors.append(
                        "Invalid display, non-existent screen: %s" % display_screen_str
                    )

            if "GLX" not in disp_screen.list_extensions():
                errors.append(
                    "Display %s does not have the GLX extension loaded.  GLX is required by Unity3D."
                    % display_screen_str
                )

            if (
                disp_screen.screen()["width_in_pixels"] < width
                or disp_screen.screen()["height_in_pixels"] < height
            ):
                errors.append(
                    "Display %s does not have a large enough resolution for the target resolution: %sx%s vs. %sx%s"
                    % (
                        display_screen_str,
                        width,
                        height,
                        disp_screen["width_in_pixels"],
                        disp_screen["height_in_pixels"],
                    )
                )

            if disp_screen.screen()["root_depth"] != 24:
                errors.append(
                    "Display %s does not have a color depth of 24: %s"
                    % (display_screen_str, disp_screen.screen()["root_depth"])
                )
        except (Xlib.error.DisplayNameError, Xlib.error.DisplayConnectionError) as e:
            errors.append(
                "Invalid display: %s. Failed to connect %s " % (display_screen_str, e)
            )

        return errors

    @classmethod
    def _is_valid_screen(cls, display_screen_str, width, height):
        return len(cls._validate_screen(display_screen_str, width, height)) == 0

    @classmethod
    def _valid_x_displays(cls, width, height):
        open_display_strs = [
            int(os.path.basename(s)[1:]) for s in glob.glob("/tmp/.X11-unix/X[0-9]*")
        ]
        valid_displays = []
        for display_str in open_display_strs:
            try:
                disp = Xlib.display.Display(":%s" % display_str)
                for screen in range(0, disp.screen_count()):
                    disp_screen_str = ":%s.%s" % (display_str, screen)
                    if cls._is_valid_screen(disp_screen_str, width, height):
                        valid_displays.append(disp_screen_str)

            except Xlib.error.DisplayConnectionError as e:
                warnings.warn(
                    "could not connect to X Display: %s, %s" % (display_str, e)
                )

        return valid_displays

    @classmethod
    def validate(cls, request):
        if request.headless:
            return []
        elif request.x_display:
            return cls._validate_screen(
                request.x_display, request.width, request.height
            )
        elif cls._select_x_display(request.width, request.height) is None:
            return ["No valid X display found"]
        else:
            return []


class OSXIntel64(BasePlatform):
    @classmethod
    def old_executable_path(cls, base_dir, name):
        return os.path.join(base_dir, name + ".app", "Contents/MacOS", name)

    @classmethod
    def executable_path(cls, base_dir, name):
        plist = cls.parse_plist(base_dir, name)
        return os.path.join(
            base_dir, name + ".app", "Contents/MacOS", plist["CFBundleExecutable"]
        )

    @classmethod
    def parse_plist(cls, base_dir, name):

        plist_path = os.path.join(base_dir, name + ".app", "Contents/Info.plist")

        with open(plist_path) as f:
            plist = f.read()
        root = xml.etree.ElementTree.fromstring(plist)

        keys = [x.text for x in root.findall("dict/key")]
        values = [x.text for x in root.findall("dict/string")]
        return dict(zip(keys, values))


class CloudRendering(BaseLinuxPlatform):
    enabled = False

    @classmethod
    def dependency_instructions(cls, request):
        return "CloudRendering requires libvulkan1. Please install by running: sudo apt-get -y libvulkan1"

    @classmethod
    def failure_message(cls):
        pass

    @classmethod
    def validate(cls, request):
        if ctypes.util.find_library("vulkan") is not None:
            return []
        else:
            return ["Vulkan API driver missing."]


class WebGL(BasePlatform):
    pass


def select_platforms(request):
    candidates = []
    system_platform_map = dict(Linux=(CloudRendering, Linux64), Darwin=(OSXIntel64,))
    for p in system_platform_map.get(request.system, ()):
        if not p.enabled:
            continue
        # skip CloudRendering when a x_display is specified
        if p == CloudRendering and request.x_display is not None:
            continue
        candidates.append(p)
    return candidates


STR_PLATFORM_MAP = dict(
    CloudRendering=CloudRendering, Linux64=Linux64, OSXIntel64=OSXIntel64, WebGL=WebGL
)
