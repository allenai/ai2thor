"""
Default AI2-THOR Blue Physics Based Agent. It can ONLY move in x/z directions
and can only change it's yaw.
"""
import ai2thor
from typing import Union, Tuple, Dict, List


class BlueBot(ai2thor.Agent):
    def __init__(
            self,
            controller: ai2thor.Controller,
            agent_idx: Union[int, None] = None):
        ai2thor.Agent.__init__(self, controller, agent_idx)

    @property
    def pose(self) -> Dict[str, float]:
        # use this format for teleporting
        horizon = self.last_event.metadata['agent']['cameraHorizon']
        pos = self.pos
        rot = self.rot
        return {
            'x': pos[0],
            'z': pos[1],
            'rot_y': rot,
            'horizon': horizon
        }

    @property
    def pos(self) -> Tuple[float, float]:
        """Returns the tuple(x, z) position of the agent"""
        pos = self.last_event.metadata['agent']['position']
        return pos['x'], pos['z']

    @property
    def rot(self) -> float:
        """Returns the yaw (or heading direction) of the agent"""
        return self.last_event.metadata['agent']['rotation']['y']

    @property
    def reachable_positions(self) -> List[Dict[str, float]]:
        # TODO: Cache per scene
        event = self._step('GetReachablePositions')
        positions = event.metadata['reachablePositions']
        return [{
            'x': pos['x'],
            'z': pos['z']
        } for pos in positions]

    def teleport(
            self,
            x: Union[None, float] = None,
            z: Union[None, float] = None,
            rot_y: Union[None, float] = None,
            horizon: Union[None, float] = None) -> None:
        # uses default values if they're not specified
        pos = self.last_event.metadata['agent']['position']
        y = pos['y']
        x = pos['x'] if x is None else x
        z = pos['z'] if z is None else z

        rot = self.last_event.metadata['agent']['rotation']
        rot_x = rot['x']
        rot_z = rot['z']
        rot_y = rot['y'] if rot_y is None else rot_y

        horizon = self.horizon if horizon is None else horizon

        self._step(
            'TeleportFull',
            x=x, y=y, z=y,
            rotation={'x': rot_x, 'y': rot_y, 'z': rot_z},
            horizon=horizon
        )
