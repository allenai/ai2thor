"""
TODO: Give Warning on action fail!
"""
import ai2thor
from typing import Union


class Agent:
    def __init__(
            self,
            controller: ai2thor.Controller,
            agent_idx: Union[int, None] = None):
        self.controller = controller
        self.agent_idx = agent_idx

    def _step(self, **action_kwargs) -> None:
        pass

    def done(self) -> None:
        self._step(action='done')
        pass

    def rotate(self, degrees: float = 90, direction: str = 'right') -> None:
        pass

    def move(self, meters: float = 0.25, direction: str = 'ahead') -> None:
        pass

    def teleport(
            self,
            x: Union[None, float] = None,
            z: Union[None, float] = None,
            rot_y: Union[None, float] = None,
            horizon: Union[None, float] = None,
            y: Union[None, float] = None,
            rot_x: Union[None, float] = None,
            rot_z: Union[None, float] = None) -> None:
        pass
