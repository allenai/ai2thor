from typing import Union, Dict, List
import ai2thor


class Agent:
    def __init__(
            self,
            camera,
            noise,
            default_rotate_degrees,
            default_move_meters,
            nav_success_max_distance):
        self._next_action_kwargs = None

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

    def _step(self, action: str, **action_kwargs) -> ai2thor.server.Event:
        # single agent
        if self.agent_idx is None:
            return self.controller.step(action, **action_kwargs)

        # multi-agent
        else:
            return self.controller.step(
                action,
                agentId=self.agent_idx,
                **action_kwargs)

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

    def move(self, meters: float = 0.25, direction: str = 'ahead') -> None:
        """Translates the agent in 'direction' by a distance of 'meters'"""
        # TODO: support vector direction with teleport
        valid_directions = {'ahead', 'right', 'left', 'back'}
        if direction not in valid_directions:
            raise ValueError(f'direction must be in {valid_directions}')

        kwargs = {'moveMagnitude': meters}
        if direction == 'ahead':
            self._step(action='MoveAhead', **kwargs)
        elif direction == 'right':
            self._step(action='MoveRight', **kwargs)
        elif direction == 'left':
            self._step(action='MoveLeft', **kwargs)
        elif direction == 'back':
            self._step(action='MoveBack', **kwargs)

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
