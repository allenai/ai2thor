from ai2thor.controller import Controller as BaseController
from typing import Sequence, Union, Iterable, List, Dict, Any
from ai2thor.agents import Jarvis, Agent
import numpy as np
import ai2thor
import warnings


# NOTE: These functions cannot be inherited from a base class as
# it makes typing less flexible.

# TODO: Log trajectory image

def _base_init(scene: str, agents: Sequence[ai2thor.Agent]) -> BaseController:
    agent_count = {'agentCount': len(agents)} if len(agents) != 1 else {}
    base_controller = BaseController(
        scene=scene,
        fov=agents[0].camera.fov,
        width=agents[0].camera.width,
        height=agents[0].camera.height,
        renderDepthImage=agents[0].camera.render_depth,
        renderClassimage=agents[0].camera.render_class_segmentation,
        renderObjectImage=agents[0].camera.render_instance_segmentation,
        **agent_count)

    # link each of the agents with the controller
    for i, agent in enumerate(agents):
        agent._connect_base_controller(
            base_controller=base_controller,
            agent_idx=i
        )
    return base_controller


def _get_map_frame(self):
    event = self._base_controller.step(action='ToggleMapView')
    frame = event.frame
    self._base_controller.step(action='ToggleMapView')
    return frame


"""
def objects(
        self,
        types: Union[None, str, Iterable[str]] = None,
        chopable: Union[None, bool] = None,  # TODO: Change this name
        openable: Union[None, bool] = None,
        pickupable: Union[None, bool] = None,
        moveable: Union[None, bool] = None,
        is_moving: Union[None, bool] = None,

        ) -> List[Dict[str, Any]]:
    raise NotImplementedError()
"""


class _JarvisController:
    def __init__(self, agents: Sequence[Jarvis], scene: str):
        self._base_controller = _base_init(scene, agents)
        self.agents = agents

    @property
    def agent(self) -> Jarvis:
        if len(self.agents) != 1:
            raise ValueError('Use .agents[i] to access the ith multi-agent')
        return self.agents[0]

    @property
    def map_frame(self) -> np.ndarray:
        return _get_map_frame(self)

    def stop(self):
        self._base_controller.stop()

    def reset(self, scene: str = 'FloorPlan28') -> None:
        self._base_controller.reset(scene)


def Controller(
        agents: Union[ai2thor.Agent, Sequence[ai2thor.Agent]] = Jarvis(),
        scene: str = 'FloorPlan28'):
    # Decides which controller to provide based on the agent.
    # This helps with mypy find functions specific to certain controllers.

    # only using a single agent
    if issubclass(type(agents), ai2thor.Agent):
        agent_list: Sequence[ai2thor.Agent] = [agents]
    else:
        agent_list = agents

    if len(agent_list) == 0:
        raise ValueError('You must specify an agent')

    agent_type = type(agent_list[0])
    for i in range(1, len(agent_list)):
        if type(agent_list[i]) != agent_type:
            raise ValueError('For now, all agents must be the same type')

    if len(agent_list) > 1:
        warnings.warn('Only 1 identical agent can currently be used.')

    return _JarvisController(agent_list, scene)


"""
class DefaultController:
    def __init__(
            self,
            foo='a',
        if foo == 'a':
            setattr(self, 'testing', lambda x: x * 2)
            # self.__dict__['testing'] = lambda x: x * 2
        '''
        self.base_controller = BaseController(
            scene=scene,
            gridSize=grid_size)
        '''
        # self.base_controller.stop()
        pass

    def foo(self):
        return 'Controller'

    def destroy_a(self):
        if 'a' in self.__dict__:
            del self.__dict__['a']
"""


# test = 'yo'
'''
from ai2thor.agents import Jarvis
from ai2thor.agents import Agent

# use custom_kwargs


class Noise:
    def __init__(
            self,
            move_normal_mean: float = 0,
            move_normal_sigma: float = 0.005,
            rotate_normal_mu: float = 0,
            rotate_normal_sigma: float = 0.5):
        pass


class Camera:
    def __init__(
            self,
            fov: float = 90,
            width: int = 256,
            height: int = 256,
            render_rgb: bool = True,
            render_depth: bool = False,
            render_class_segmentation: bool = False,
            render_instance_segmentation: bool = False
            ) -> None:
        pass


class Controller:  # (BaseController):
    def __init__(
            self,
            scene: str = 'FloorPlan_Train1_1',
            grid_size: float = 0.25,
            agents: List[Agent] = [Jarvis(None, 0)]):


    @property
    def third_person_cameras(self):
        pass

    def add_agent(self, agent):
        pass

    def make_all_objects_moveable(self) -> None:
        pass

    def make_object_type_unbreakable(self, object_type: str):
        pass

    def randomize_objects(self, seed: int = 42):
        pass

    def set_object_poses(self):
        pass

    def set_object_states(self, metadata='todo'):
        pass

    def place_object(
            self,
            object_id: str,
            x: float,
            y: float,
            z: float):
        pass

'''