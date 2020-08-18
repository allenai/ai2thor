# TODO: add setter to camera
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
            default_rotate_degrees: float = 90,
            default_move_meters: float = 0.25,
            nav_success_max_meter_dist: float = 1.5):
        self._reset_camera = False
        self.camera = camera
        self.default_rotate_degrees = default_rotate_degrees
        self.default_move_meters = default_move_meters

    @property
    def frame(self) -> np.ndarray:
        if self.camera.frame_as_bgr:
            return self._base_controller.last_event.cv2img
        else:
            return self._base_controller.last_event.frame

    @property
    def depth_frame(self) -> np.ndarray:
        if not self.camera.render_depth:
            raise ValueError(
                'Camera must render_depth.\n'
                'pass camera=Camera(render_depth=True) to your Agent'
            )
        else:
            return self._base_controller.last_event.depth_frame

    @property
    def class_segmentation_frame(self) -> np.ndarray:
        if not self.camera.render_class_segmentation:
            raise ValueError(
                'Camera must render_class_segmentation.\n'
                'pass camera=Camera(render_class_segmentation=True) '
                'to your Agent'
            )
        else:
            return self._base_controller.last_event.class_segmentation_frame

    @property
    def instance_segmantation_frame(self) -> np.ndarray:
        if not self.camera.render_instance_segmentation:
            raise ValueError(
                'Camera must render_instance_segmentation.\n'
                'pass camera=Camera(render_instance_segmentation=True) '
                'to your Agent'
            )
        else:
            return self._base_controller.last_event.instance_segmentation_frame

    @property
    def bounding_box_frame(self) -> np.ndarray:
        if not self.camera.render_instance_segmentation:
            raise ValueError(
                'Camera must render_instance_segmentation.\n'
                'pass camera=Camera(render_instance_segmentation=True) '
                'to your Agent'
            )
        else:
            raise NotImplementedError()

    def _connect_base_controller(
            self,
            base_controller,
            agent_idx: Union[None, int]):
        self._base_controller = base_controller
        self._agent_idx = agent_idx

    def _step(self, action: str, **action_kwargs) -> bool:
        # single agent
        if self._agent_idx is None:
            event = self._base_controller.step(action, **action_kwargs)
        # multi-agent
        else:
            event = self._base_controller.step(
                action,
                agentId=self._agent_idx,
                **action_kwargs)
        return event.metadata['lastActionSuccess']

    @property
    @abstractmethod
    def pose(self) -> Dict[str, float]:
        # use this format for teleporting with the agent
        raise NotImplementedError()

    def move(
            self,
            direction: np.ndarray = AHEAD,
            meters: Union[None, float] = None) -> bool:
        """Translates the agent in 'direction' by a distance of 'meters'"""
        # TODO: support vector direction with teleport

        kwargs = {
            'moveMagnitude':
                self.default_move_meters if meters is None else meters
        }
        if direction is AHEAD:
            return self._step(action='MoveAhead', **kwargs)
        elif direction is BACK:
            return self._step(action='MoveBack', **kwargs)
        elif direction is RIGHT:
            return self._step(action='MoveRight', **kwargs)
        elif direction is LEFT:
            return self._step(action='MoveLeft', **kwargs)
        else:
            raise ValueError(
                'Invalid direction!\n'
                'Please use ai2thor.utils.{AHEAD, BACK, RIGHT, LEFT}.')

    def rotate(
            self,
            direction: np.ndarray = RIGHT,
            degrees: Union[None, float] = None) -> bool:
        """Rotates the agent in 'direction' by 'degrees'"""
        degrees = self.default_rotate_degrees if degrees is None else degrees
        if direction is RIGHT:
            return self._step('RotateRight', degrees=degrees)
        elif direction is LEFT:
            return self._step('RotateLeft', degrees=degrees)
        else:
            raise ValueError(
                'Invalid direction!\n'
                'Please use ai2thor.utils.{RIGHT, LEFT}.')

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
