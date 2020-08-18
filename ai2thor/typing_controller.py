from ai2thor.controller import Controller as BaseController
from typing import List
import ai2thor
from ai2thor.agents import Jarvis, Agent


class _DefaultController:
    def foo(self):
        return 'def'

    @property
    def last_event(self):
        pass


def Controller(
        scene: str = 'FloorPlan_Train1_1',
        grid_size: float = 0.25,
        agents: List[ai2thor.agents.Agent] = []) -> _DefaultController:
    # Decides which controller to provide based on the agent.
    # This helps with mypy find functions specific to certain controllers.
    if len(agents) == 0:
        raise ValueError('You must specify an agent')

    agent_type = type(agents[0])
    for i in range(1, len(agents)):
        if type(agents[i]) != agent_type:
            raise ValueError('For now, all agents must be the same type')

    return _DefaultController()

    """
    if foo == 'a':
        return DefaultController()
    else:
        return JarvisController()
    """


class JarvisController:
    def foo(self):
        return 'jarvis'

    def jarvis(self):
        return 'jarvisss'



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