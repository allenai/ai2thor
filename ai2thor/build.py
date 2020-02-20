try:
    from ai2thor._builds import BUILDS
except ImportError:
    BUILDS = {}

platform_map = dict(Linux64="Linux", OSXIntel64="Darwin")

arch_platform_map = {v: k for k, v in platform_map.items()}

