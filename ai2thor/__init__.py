# Copyright Allen Institute for Artificial Intelligence

from ai2thor.controller import Controller
from ai2thor.agents import Agent
from ai2thor.server import Event


__version__ = None
try:
    from ai2thor._version import __version__
except ImportError:
    pass
