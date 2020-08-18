"""
Default AI2-THOR Blue Physics Based Agent. It can ONLY move in x/z directions
and can only change it's yaw.
"""

# TODO: raise exception for robothor scenes

from typing import Union, Tuple, Dict, List
from .agent import Agent


class Jarvis(Agent):
    def __init__(self):
        Agent.__init__(self)
        # step here to set up the agent

    def pose(self):
        raise NotImplementedError()

    def jarvis_method(self):
        return 'yo'



'''

class Jarvis(ai2thor.agents.Agent):
    def __init__(
            self,
            controller: ai2thor.typing_controller.Controller,
            agent_idx: Union[int, None] = None):
        ai2thor.agents.Agent.__init__(self, controller, agent_idx)

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
        event = self._step('GetReachablePositions', renderImage=False)
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

    def open(
            self,
            x: Union[float, None] = None,
            y: Union[float, None] = None,
            object_id: Union[None, str] = None,
            openness: Union[None, float] = None,
            force_action: bool = False) -> None:
        pass

    def close(
            self,
            x: Union[float, None] = None,
            y: Union[float, None] = None,
            object_id: Union[None, str] = None,
            force_action: bool = False) -> None:
        pass

    def turn_on(self):
        pass

    def turn_off(self):
        pass

    def cook(
            self,
            x: Union[float, None] = None,
            y: Union[float, None] = None,
            object_id: Union[None, str] = None,
            force_action: bool = False) -> None:
        pass

    def cut(
            self,
            x: Union[float, None] = None,
            y: Union[float, None] = None,
            object_id: Union[None, str] = None,
            force_action: bool = False) -> None:
        pass

    def destroy(
            self,
            x: Union[float, None] = None,
            y: Union[float, None] = None,
            object_id: Union[None, str] = None,
            force_action: bool = False) -> None:
        pass

    def dirty(
            self,
            x: Union[float, None] = None,
            y: Union[float, None] = None,
            object_id: Union[None, str] = None,
            force_action: bool = False) -> None:
        pass

    def clean(
            self,
            x: Union[float, None] = None,
            y: Union[float, None] = None,
            object_id: Union[None, str] = None,
            force_action: bool = False) -> None:
        pass

    def liquid_fill(
            self,
            liquid_type: str,
            x: Union[float, None] = None,
            y: Union[float, None] = None,
            object_id: Union[None, str] = None,
            force_action: bool = False) -> None:
        pass

    def liquid_empty(
            self,
            x: Union[float, None] = None,
            y: Union[float, None] = None,
            object_id: Union[None, str] = None,
            force_action: bool = False) -> None:
        pass

    def use_up(
            self,
            x: Union[float, None] = None,
            y: Union[float, None] = None,
            object_id: Union[None, str] = None,
            force_action: bool = False) -> None:
        pass

'''