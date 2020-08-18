# TODO: add setter to camera
# TODO: Raise Exception for not building a controller
from typing import Union, Dict, List
from ai2thor.controller import Controller as BaseController
import ai2thor.utils
from ai2thor.utils import AHEAD, BACK, LEFT, RIGHT, UP, DOWN
from abc import ABC, abstractmethod
import numpy as np


class Agent(ABC):
    def __init__(
            self,
            camera: ai2thor.utils.Camera = ai2thor.utils.Camera(),
            noise: None = None,
            default_rotate_degrees: float = 90,
            default_move_meters: float = 0.25,
            default_peak_degrees: float = 30,
            nav_success_max_meter_dist: float = 1.5):
        self._reset_camera = False
        self.camera = camera
        self.default_rotate_degrees = default_rotate_degrees
        self.default_move_meters = default_move_meters
        self.default_peak_degrees = default_peak_degrees
        self._base_controllers: List[BaseController] = []

    def _extract_sensors(self, event_fn):
        if len(self._base_controllers) == 1:
            # one to one mapping between agent and controller
            return event_fn(self, self._base_controllers[0].last_event)
        else:
            return [
                event_fn(self, contrl.last_event)
                for contrl in self._base_controllers]

    @property
    def frame(self) -> np.ndarray:
        return self._extract_sensors(
            lambda e: e.ev2img if self.camera.frame_as_bgr else e.frame
        )

    @property
    def depth_frame(self) -> np.ndarray:
        if not self.camera.render_depth:
            raise ValueError(
                'Camera must render_depth.\n'
                'pass camera=Camera(render_depth=True) to your Agent'
            )
        else:
            return self._extract_sensors(lambda e: e.depth_frame)

    @property
    def class_segmentation_frame(self) -> np.ndarray:
        if not self.camera.render_class_segmentation:
            raise ValueError(
                'Camera must render_class_segmentation.\n'
                'pass camera=Camera(render_class_segmentation=True) '
                'to your Agent'
            )
        else:
            return self._extract_sensors(
                lambda e: e.class_segmentation_frame)

    @property
    def instance_segmantation_frame(
            self) -> Union[np.ndarray, List[np.ndarray]]:
        if not self.camera.render_instance_segmentation:
            raise ValueError(
                'Camera must render_instance_segmentation.\n'
                'pass camera=Camera(render_instance_segmentation=True) '
                'to your Agent'
            )
        else:
            return self._extract_sensors(
                lambda e: e.instance_segmentation_frame)

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
        # TODO: Connect to multiple base controllers!
        self._base_controllers.append(base_controller)
        self._agent_idx = agent_idx

    def _step(self, action: str, **action_kwargs) -> bool:
        out = []

        # works for single agent and multi-agent
        if self._agent_idx is not None:
            action_kwargs['agentId'] = self._agent_idx

        for ctrl in self._base_controllers:
            event = ctrl.step(action, **action_kwargs)
            out.append(event.metadata['lastActionSuccess'])
        return out[0] if len(out) == 1 else out

    @property
    @abstractmethod
    def pose(self) -> Union[List[Dict[str, float]], Dict[str, float]]:
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

    @property
    def horizon(self) -> float:
        return self._extract_sensors(
            lambda event: event.metadata['agent']['cameraHorizon'])

    def done(self) -> None:
        """Updates last_event without changing the environment"""
        self._step(action='done')

    def peak(
            self,
            direction: np.ndarray = UP,
            degrees: Union[None, float] = None) -> None:
        """Rotates the agent's head in 'direction' by 'degrees' without
           rotating its body."""
        degrees = self.default_peak_degrees if degrees is None else degrees
        kwargs = {'degrees': degrees}

        if direction is UP:
            self._step('LookUp', **kwargs)
        elif direction is DOWN:
            self._step('LookDown', **kwargs)
        else:
            raise ValueError(
                'Invalid direction!\n'
                'Please use ai2thor.utils.{UP, DOWN}.')
