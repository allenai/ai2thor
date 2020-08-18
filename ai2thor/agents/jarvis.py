"""
Default AI2-THOR Blue Physics Based Agent. It can ONLY move in x/z directions
and can only change it's yaw.
"""

# TODO: raise exception for robothor scenes

from typing import Union, Tuple, Dict, List, Optional
from .agent import Agent
import ai2thor.utils


# outside for typing
def _get_interact_type_kwargs(x, y, object_id):
    """Determines if the pixel was used to find the object."""
    used_pixel = not (x is None or y is None)
    used_id = object_id is not None

    if used_pixel != used_id:
        raise ValueError('Must supply (x and y) xor object_id')

    return {'x': x, 'y': y} if used_pixel else {'objectid': object_id}


class Jarvis(Agent):
    def __init__(
            self,
            camera: ai2thor.utils.Camera = ai2thor.utils.Camera(),
            noise: None = None,
            default_rotate_degrees: float = 90,
            default_move_meters: float = 0.25,
            nav_success_max_meter_dist: float = 1.5):
        Agent.__init__(
            self,
            camera=camera,
            noise=noise,
            default_rotate_degrees=default_rotate_degrees,
            default_move_meters=default_move_meters,
            nav_success_max_meter_dist=nav_success_max_meter_dist)
        # step here to set up the agent

    @property
    def pos(self) -> Tuple[float, float]:
        """Returns the tuple(x, z) position of the agent"""
        event = self._base_controller.last_event
        pos = event.metadata['agent']['position']
        return pos['x'], pos['z']

    @property
    def rot(self) -> float:
        """Returns the yaw (or heading direction) of the agent"""
        event = self._base_controller.last_event
        return event.metadata['agent']['rotation']['y']

    @property
    def pose(self) -> Dict[str, float]:
        # use this format for teleporting
        event = self._base_controller.last_event
        horizon = event.metadata['agent']['cameraHorizon']
        pos = self.pos
        rot = self.rot
        return {
            'x': pos[0],
            'z': pos[1],
            'rot_y': rot,
            'horizon': horizon
        }

    @property
    def reachable_positions(self) -> List[Dict[str, float]]:
        # TODO: Cache per scene
        self._step('GetReachablePositions', renderImage=False)
        event = self._base_controller.last_event
        positions = event.metadata['reachablePositions']
        return [{
            'x': pos['x'],
            'z': pos['z']
        } for pos in positions]

    def teleport(
            self,
            x: Optional[float] = None,
            z: Optional[float] = None,
            rot_y: Optional[float] = None,
            horizon: Optional[float] = None) -> bool:
        # uses default values if they're not specified
        event = self._base_controller.last_event
        pos = event.metadata['agent']['position']
        y = pos['y']
        x = pos['x'] if x is None else x
        z = pos['z'] if z is None else z

        rot = event.metadata['agent']['rotation']
        rot_x = rot['x']
        rot_z = rot['z']
        rot_y = rot['y'] if rot_y is None else rot_y

        horizon = self.horizon if horizon is None else horizon

        return self._step(
            'TeleportFull',
            x=x, y=y, z=z,
            rotation={'x': rot_x, 'y': rot_y, 'z': rot_z},
            horizon=horizon
        )

    def open(
            self,
            x: Optional[float] = None,
            y: Optional[float] = None,
            object_id: Optional[str] = None,
            openness: Optional[float] = None,
            force_action: bool = False) -> bool:
        kwargs = _get_interact_type_kwargs(x, y, object_id)
        if openness is not None:
            if openness > 1 or openness < 0:
                raise ValueError('openness must be between [0:1]')
            kwargs['moveMagnitude'] = openness
        kwargs['forceAction'] = force_action
        return self._step('OpenObject', **kwargs)

    def close(
            self,
            x: Optional[float] = None,
            y: Optional[float] = None,
            object_id: Optional[str] = None,
            force_action: bool = False) -> bool:
        kwargs = _get_interact_type_kwargs(x, y, object_id)
        kwargs['forceAction'] = force_action
        return self._step('CloseObject', **kwargs)

    def turn_on(self):
        # NOTE: Renamed from Toggle On
        raise NotImplementedError()

    def turn_off(self):
        # NOTE: Renamed from Toggle Off
        raise NotImplementedError()

    def cook(
            self,
            x: Optional[float] = None,
            y: Optional[float] = None,
            object_id: Optional[str] = None,
            force_action: bool = False) -> bool:
        kwargs = _get_interact_type_kwargs(x, y, object_id)
        kwargs['forceAction'] = force_action
        return self._step('CookObject', **kwargs)

    def cut(
            self,
            x: Optional[float] = None,
            y: Optional[float] = None,
            object_id: Optional[str] = None,
            force_action: bool = False) -> bool:
        # NOTE: Renamed from Slice
        pass

    def destroy(
            self,
            x: Optional[float] = None,
            y: Optional[float] = None,
            object_id: Optional[str] = None,
            force_action: bool = False) -> bool:
        # NOTE: Renamed from Break
        kwargs = _get_interact_type_kwargs(x, y, object_id)
        kwargs['forceAction'] = force_action
        return self._step('BreakObject', **kwargs)

    def dirty(
            self,
            x: Optional[float] = None,
            y: Optional[float] = None,
            object_id: Optional[str] = None,
            force_action: bool = False) -> bool:
        kwargs = _get_interact_type_kwargs(x, y, object_id)
        kwargs['forceAction'] = force_action
        return self._step('DirtyObject', **kwargs)

    def clean(
            self,
            x: Optional[float] = None,
            y: Optional[float] = None,
            object_id: Optional[str] = None,
            force_action: bool = False) -> bool:
        kwargs = _get_interact_type_kwargs(x, y, object_id)
        kwargs['forceAction'] = force_action
        return self._step('CleanObject', **kwargs)

    def liquid_fill(
            self,
            liquid_type: str,
            x: Optional[float] = None,
            y: Optional[float] = None,
            object_id: Optional[str] = None,
            force_action: bool = False) -> bool:
        valid_liquid_types = {'coffee', 'wine', 'water'}
        if liquid_type not in valid_liquid_types:
            raise ValueError(f'Liquid must be in {valid_liquid_types}.')
        kwargs = _get_interact_type_kwargs(x, y, object_id)
        kwargs['forceAction'] = force_action
        kwargs['fillLiquid'] = liquid_type
        return self._step('FillObjectWithLiquid', **kwargs)

    def liquid_empty(
            self,
            x: Optional[float] = None,
            y: Optional[float] = None,
            object_id: Optional[str] = None,
            force_action: bool = False) -> bool:
        kwargs = _get_interact_type_kwargs(x, y, object_id)
        kwargs['forceAction'] = force_action
        return self._step('EmptyLiquidFromObject', **kwargs)

    def use_up(
            self,
            x: Optional[float] = None,
            y: Optional[float] = None,
            object_id: Optional[str] = None,
            force_action: bool = False) -> bool:
        kwargs = _get_interact_type_kwargs(x, y, object_id)
        kwargs['forceAction'] = force_action
        return self._step('UseUpObject', **kwargs)

    def pickup(
            self,
            x: Optional[float] = None,
            y: Optional[float] = None,
            object_id: Optional[str] = None,
            teleport_hand_to_object: bool = False,
            force_action: bool = False) -> bool:
        kwargs = _get_interact_type_kwargs(x, y, object_id)
        kwargs['forceAction'] = force_action
        kwargs['manualInteract'] = teleport_hand_to_object
        return self._step('PickupObject', **kwargs)

    def put(
            self,
            x: Optional[float] = None,
            y: Optional[float] = None,
            object_id: Optional[str] = None,
            let_object_settle: bool = False,
            force_action: bool = False) -> bool:
        kwargs = _get_interact_type_kwargs(x, y, object_id)
        kwargs['forceAction'] = force_action
        kwargs['placeStationary'] = let_object_settle
        return self._step('PutObject', **kwargs)

    def drop(
            self,
            force_action: bool = False) -> bool:
        kwargs = {}
        kwargs['forceAction'] = force_action
        return self._step('DropHandObject', **kwargs)

    def throw(
            self,
            newtons: Optional[float] = None) -> bool:
        if newtons is None:
            return self._step('ThrowObject')
        else:
            return self._step('ThrowObject', moveMagnitude=newtons)

    def push(
            self,
            x: Optional[float] = None,
            y: Optional[float] = None,
            object_id: Optional[str] = None,
            angled_degrees: Optional[float] = None,
            force_action: bool = False) -> bool:
        # Merges directional push
        kwargs = _get_interact_type_kwargs(x, y, object_id)
        kwargs['forceAction'] = force_action
        if angled_degrees is None:
            return self._step(
                'DirectionalPush', pushAngle=angled_degrees, **kwargs)
        return self._step('PushObject', **kwargs)

    def pull(
            self,
            x: Optional[float] = None,
            y: Optional[float] = None,
            object_id: Optional[str] = None,
            force_action: bool = False) -> bool:
        kwargs = _get_interact_type_kwargs(x, y, object_id)
        kwargs['forceAction'] = force_action
        return self._step('PullObject', **kwargs)
