import ai2thor
from ai2thor.utils import AHEAD, RIGHT, LEFT, BACK, UP, DOWN, Camera
from ai2thor.agents import Jarvis


agent = Jarvis(Camera(render_depth=True))
c1 = ai2thor.Env(agent)
c2 = ai2thor.Env(agent)

c2.reset('FloorPlan7')
agent.move(BACK)


class scenes:
    floorplan1 = Scene('FloorPlan1')
    flooplan2 = Scene('FloorPlan2')
    flooplan3 = Scene('FloorPlan3')
    flooplan4 = Scene('FloorPlan4')
    flooplan5 = Scene('FloorPlan5')
    flooplan6 = Scene('FloorPlan6')
    flooplan7 = Scene('FloorPlan7')
    flooplan8 = Scene('FloorPlan8')
    flooplan9 = Scene('FloorPlan9')
    flooplan10 = Scene('FloorPlan10')
    flooplan11 = Scene('FloorPlan11')
    flooplan12 = Scene('FloorPlan12')
    flooplan13 = Scene('FloorPlan13')
    flooplan14 = Scene('FloorPlan14')
    flooplan15 = Scene('FloorPlan15')
    flooplan16 = Scene('FloorPlan16')
    flooplan17 = Scene('FloorPlan17')
    flooplan18 = Scene('FloorPlan18')
    flooplan19 = Scene('FloorPlan19')
    flooplan20 = Scene('FloorPlan20')
    flooplan21 = Scene('FloorPlan21')
    flooplan22 = Scene('FloorPlan22')
    flooplan23 = Scene('FloorPlan23')
    flooplan24 = Scene('FloorPlan24')
    flooplan25 = Scene('FloorPlan25')
    flooplan26 = Scene('FloorPlan26')
    flooplan27 = Scene('FloorPlan27')
    flooplan28 = Scene('FloorPlan28')
    flooplan29 = Scene('FloorPlan29')
    flooplan30 = Scene('FloorPlan30')

    flooplan201 = Scene('FloorPlan201')
    flooplan202 = Scene('FloorPlan202')
    flooplan203 = Scene('FloorPlan203')
    flooplan204 = Scene('FloorPlan204')
    flooplan205 = Scene('FloorPlan205')
    flooplan206 = Scene('FloorPlan206')
    flooplan207 = Scene('FloorPlan207')
    flooplan208 = Scene('FloorPlan208')
    flooplan209 = Scene('FloorPlan209')
    flooplan210 = Scene('FloorPlan210')
    flooplan211 = Scene('FloorPlan211')
    flooplan212 = Scene('FloorPlan212')
    flooplan213 = Scene('FloorPlan213')
    flooplan214 = Scene('FloorPlan214')
    flooplan215 = Scene('FloorPlan215')
    flooplan216 = Scene('FloorPlan216')
    flooplan217 = Scene('FloorPlan217')
    flooplan218 = Scene('FloorPlan218')
    flooplan219 = Scene('FloorPlan219')
    flooplan220 = Scene('FloorPlan220')
    flooplan221 = Scene('FloorPlan221')
    flooplan222 = Scene('FloorPlan222')
    flooplan223 = Scene('FloorPlan223')
    flooplan224 = Scene('FloorPlan224')
    flooplan225 = Scene('FloorPlan225')
    flooplan226 = Scene('FloorPlan226')
    flooplan227 = Scene('FloorPlan227')
    flooplan228 = Scene('FloorPlan228')
    flooplan229 = Scene('FloorPlan229')
    flooplan230 = Scene('FloorPlan230')

    flooplan301 = Scene('FloorPlan301')
    flooplan302 = Scene('FloorPlan302')
    flooplan303 = Scene('FloorPlan303')
    flooplan304 = Scene('FloorPlan304')
    flooplan305 = Scene('FloorPlan305')
    flooplan306 = Scene('FloorPlan306')
    flooplan307 = Scene('FloorPlan307')
    flooplan308 = Scene('FloorPlan308')
    flooplan309 = Scene('FloorPlan309')
    flooplan310 = Scene('FloorPlan310')
    flooplan311 = Scene('FloorPlan311')
    flooplan312 = Scene('FloorPlan312')
    flooplan313 = Scene('FloorPlan313')
    flooplan314 = Scene('FloorPlan314')
    flooplan315 = Scene('FloorPlan315')
    flooplan316 = Scene('FloorPlan316')
    flooplan317 = Scene('FloorPlan317')
    flooplan318 = Scene('FloorPlan318')
    flooplan319 = Scene('FloorPlan319')
    flooplan320 = Scene('FloorPlan320')
    flooplan321 = Scene('FloorPlan321')
    flooplan322 = Scene('FloorPlan322')
    flooplan323 = Scene('FloorPlan323')
    flooplan324 = Scene('FloorPlan324')
    flooplan325 = Scene('FloorPlan325')
    flooplan326 = Scene('FloorPlan326')
    flooplan327 = Scene('FloorPlan327')
    flooplan328 = Scene('FloorPlan328')
    flooplan329 = Scene('FloorPlan329')
    flooplan330 = Scene('FloorPlan330')

    flooplan401 = Scene('FloorPlan401')
    flooplan402 = Scene('FloorPlan402')
    flooplan403 = Scene('FloorPlan403')
    flooplan404 = Scene('FloorPlan404')
    flooplan405 = Scene('FloorPlan405')
    flooplan406 = Scene('FloorPlan406')
    flooplan407 = Scene('FloorPlan407')
    flooplan408 = Scene('FloorPlan408')
    flooplan409 = Scene('FloorPlan409')
    flooplan410 = Scene('FloorPlan410')
    flooplan411 = Scene('FloorPlan411')
    flooplan412 = Scene('FloorPlan412')
    flooplan413 = Scene('FloorPlan413')
    flooplan414 = Scene('FloorPlan414')
    flooplan415 = Scene('FloorPlan415')
    flooplan416 = Scene('FloorPlan416')
    flooplan417 = Scene('FloorPlan417')
    flooplan418 = Scene('FloorPlan418')
    flooplan419 = Scene('FloorPlan419')
    flooplan420 = Scene('FloorPlan420')
    flooplan421 = Scene('FloorPlan421')
    flooplan422 = Scene('FloorPlan422')
    flooplan423 = Scene('FloorPlan423')
    flooplan424 = Scene('FloorPlan424')
    flooplan425 = Scene('FloorPlan425')
    flooplan426 = Scene('FloorPlan426')
    flooplan427 = Scene('FloorPlan427')
    flooplan428 = Scene('FloorPlan428')
    flooplan429 = Scene('FloorPlan429')
    flooplan430 = Scene('FloorPlan430')


scenes.floorplan1

for j in (0, 200, 300, 400):
    for i in range(1, 31):
        print(f"    flooplan{i+j} = Scene('FloorPlan{i+j}')")
    print()



class SceneGenerator:
    floorplan1 = _Scene('FloorPlan1')
    floorplan2 = Scene('FloorPlan2')

    def __init__(self):
        pass

    def __repr__(self):
        pass


Scenes



scenes.floorplan1

    @property
    @staticmethod
    def scenes(self):
        return ['yo']


    class scenes:
        def __repr__
    scenes = {'FloorPlan1'}


class ithor:
    scenes = 
    def __init__(self):
        pass


ithor.scenes.f

agent.move(BACK)
agent

agent.reachable_positions

agent.teleport(x=-4.0, z=-2.0)

controller.agent.horizon

controller.agent.reachable_positions

controller.agent.depth_frame
controller.agent.move(BACK)

controller.agent.rotate(RIGHT)
controller.agent.rot

controller.agent.pose

Image.fromarray(controller.map_frame)

controller


controller.agent.move()

controller.agent.camera

controller._base_controller.last_event.cv2img


print(ai2thor.utils.Camera())

controller.agent

controller.ag

controller.ag

controller.agents[0].move

controller.ag

controller.ag

controller.agent.jarvis_method()

controller.agents[0].


controller.agent.jarvis_method()

controller.agent.jarvis_method()


controller.agent.jarvis_method()

controller.agents[0].jar

controller.agents[0].mov

controller.agents[0].m

controller.agents[0].move(direction='back')

controller.agents[0].pose()



controller.agents[0]

controller.agents[0]._co

ai2thor.Controller(
    scene='FloorPlan_Train1_1',
    
)

from ai2thor.utils import Camera

from ai2thor.agents import Agent


Agent()

c = Camera(fov=60)

c.fov = 50

c.f


c = Camera(fov=60)

Camera()


Camera(fov=)

Camera()
Camera()

from ai2thor.utils import Camera

from ai2thor import utils

from ai2thor import utils
from ai2thor.utils import camera

ai2thor.util.Camera

from ai2thor.utils import Camera



from ai2thor.agents import Agent

a = Agent()

ai2thor.types.Vector

ai2thor.types.Controller(5)

ai2thor.typing.Controller


from ai2thor import Controller

from ai2thor.agents import Jarvis

ca = Controller(
    scene='FloorPlan_Train1_1',
    agents=[])
cb = Controller(foo='b')

ca.foo()
cb.foo()
cb.jarvis()
ca.f

ca.testing('2')


cb.testing(2)

ca.testing


c.testing()
c.t

c.

from types import SimpleNamespace

types = SimpleNamespace(**{'key1': 'v2'})
types.key1


c = Controller()
c.foo()
