from typing import Union, Dict, List
import ai2thor.utils
from ai2thor.utils import AHEAD, BACK, LEFT, RIGHT
from abc import ABC, abstractmethod
import numpy as np


class Agent(ABC):
    def __init__(
            self,
            camera: ai2thor.utils.Camera = ai2thor.utils.Camera(),
            noise: None = None,
            default_rotate_degrees: float = 30,
            default_move_meters: float = 0.25,
            nav_success_max_meters_dist: float = 1.5):
        self._reset_camera = False
        self.camera = camera
        # TODO: add setter to camera

    def _connect_base_controller(
            self,
            base_controller,
            agent_idx: Union[None, int]):
        self._base_controller = base_controller
        self._agent_idx = agent_idx

    def _step(self, action: str, **action_kwargs) -> object:
        # single agent
        if self._agent_idx is None:
            return self._base_controller.step(action, **action_kwargs)
        # multi-agent
        else:
            return self._base_controller.step(
                action,
                agentId=self._agent_idx,
                **action_kwargs)

    @property
    @abstractmethod
    def pose(self) -> Dict[str, float]:
        # use this format for teleporting with the agent
        raise NotImplementedError()

    # def move(self, meters: float = 0.25, direction: str = 'ahead') -> None:
    def move(
            self,
            meters: float = 0.25,
            direction: np.ndarray = AHEAD) -> None:
        """Translates the agent in 'direction' by a distance of 'meters'"""
        # TODO: support vector direction with teleport

        kwargs = {'moveMagnitude': meters}
        if direction is AHEAD:
            self._step(action='MoveAhead', **kwargs)
        elif direction is BACK:
            self._step(action='MoveBack', **kwargs)
        elif direction is RIGHT:
            self._step(action='MoveRight', **kwargs)
        elif direction is LEFT:
            self._step(action='MoveLeft', **kwargs)
        else:
            raise ValueError('Invalid direction!')

    '''
    @property
    def _last_event(self):
        return self.controller.last_event

    @property
    def pose(self) -> Dict[str, float]:
        # use this format for teleporting
        raise NotImplementedError()

    @property
    def horizon(self) -> float:
        return self._last_event.metadata['agent']['cameraHorizon']

    @property
    def pos(self) -> Union[tuple, float]:
        # should only provide degrees of freedom that can change
        raise NotImplementedError()

    @property
    def reachable_positions(self) -> List[Dict[str, float]]:
        # caches the reachable positions for the current agent
        # in the current scene
        raise NotImplementedError()

    @property
    def rot(self) -> Union[tuple, float]:
        # should only provide degrees of freedom that can change
        raise NotImplementedError()


    def done(self) -> None:
        """Updates last_event without changing the environment"""
        self._step(action='done')

    def rotate(self, degrees: float = 90, direction: str = 'right') -> None:
        """Translates the agent in 'direction' by 'degrees'"""
        valid_directions = {'right', 'left'}
        if direction not in valid_directions:
            raise ValueError(f'direction must be in {valid_directions}')

        if direction == 'right':
            self._step(action='RotateRight', degrees=degrees)
        else:
            self._step(action='RotateLeft', degrees=degrees)


    def peak(self, degrees: float = 30, direction: str = 'up', ) -> None:
        """Rotates the agent's head in 'direction' by 'degrees' without
           rotating its body."""
        valid_directions = {'up', 'down'}
        if direction not in valid_directions:
            raise ValueError(f'direction must be in {valid_directions}')

        kwargs = {'degrees': degrees}
        if direction == 'up':
            self._step(action='LookUp', **kwargs)
        elif direction == 'down':
            self._step(action='LookDown', **kwargs)
    '''
