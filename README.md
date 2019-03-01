<p align="center"><img width="30%" src="doc/static/thor-logo-main.png" /></p>

--------------------------------------------------------------------------------



AI2-THOR (The House Of inteRactions) is a photo-realistic interactable framework for AI agents.

## News
* (4/2018) We have released version 0.0.25 of AI2-THOR. The main changes include: upgrade to Unity 2017, performance optimization to improve frame rate, and various bug fixes. We have also added some physics functionalities. Please contact us for instructions. 
* (1/2018) If you need a docker version, please contact us so we provide you with the instructions. Our docker version is in beta mode.

## Requirements

* OS: Mac OS X 10.9+, Ubuntu 14.04+
* Graphics Card: DX9 (shader model 3.0) or DX11 with feature level 9.3 capabilities.
* CPU: SSE2 instruction set support.
* Python 2.7 or Python 3.5+
* Linux: X server with GLX module enabled

## Concepts

* **Agent:** A capsule shaped entity that can navigate within scenes and interact with objects.

* **Scene:** A scene within AI2-THOR represents a virtual room that an agent can navigate in and interact with. There are 4 scene Categories, each with 30 unique Scenes within them: **Kitchen, Living Room, Bedroom, Bathroom**.

* **Action:** A discrete command for the Agent to perform within a scene (e.g. MoveAhead, RotateRight, PickupObject)

* **Sim Object:** Objects that can be interacted with by the Agent. Please refer to [the Sim Object Info Table Spreadsheet](https://docs.google.com/spreadsheets/d/1wx8vWgmFSi-4Gknkwl2fUMG8oRedu-tUklGSvU0oh4U/edit?usp=sharing) and check the **Object Locations** sheet to see which Object types can be found in which Scene Category. Note that objects marked **_All_** are guaranteed to be found in all scenes of a given Category, objects marked **_Some_** can be found in some but not all scenes of a given Category, and objects marked **_None_** will not be found in a scene of the given Category.

* **Object Interactability:** An object is said to be interactable when it is in camera view, within a threshold of distance (default: 1.5 meters), and not obstructed by any other objects. This means objects behind glass are flagged as Visible to the agent but not Interactable, since they can only be seen and not reached.

* **Receptacle:** A type of object that can contain another object. These types of objects include: ArmChair, Bathtub, Bed, Bowl, Box, Cabinet, Cart, CoffeeMachine, CounterTop, Cup, Desk, Drawer, Dresser, Fridge, GarbageCan, HandTowelHolder, LaundryHamper, Microwave, Mug, NightStand, Ottoman, Pan, Plate, Pot, Safe, Shelf, Sink, Sofa, Stove Burner, TableTop, Toilet, ToiletPaperHanger, and TowelHolder. For more info about Sim Objects and their properties, refer to [the Sim Object Info Table Spreadsheet](https://docs.google.com/spreadsheets/d/1wx8vWgmFSi-4Gknkwl2fUMG8oRedu-tUklGSvU0oh4U/edit?usp=sharing)

## PIP Installation

```bash
pip install ai2thor
```
Once installed you can launch the framework. **Make sure X server with OpenGL extensions is running before running the following commands. You can check by running ```glxinfo``` or ```glxgears```.**

```python
import ai2thor.controller
controller = ai2thor.controller.Controller()
controller.start()

# Kitchens:       FloorPlan1 - FloorPlan30
# Living rooms:   FloorPlan201 - FloorPlan230
# Bedrooms:       FloorPlan301 - FloorPlan330
# Bathrooms:      FloorPLan401 - FloorPlan430
controller.reset('FloorPlan28')

# gridSize specifies the coarseness of the grid that the agent navigates on
controller.step(dict(action='Initialize', gridSize=0.25))
event = controller.step(dict(action='MoveAhead'))
```
Upon executing the ```controller.start()``` a window should appear on screen with a view of the room FloorPlan28.

## Event/Metadata
Each call to ```controller.step()``` returns an instance of an Event.  Detailed descriptions of each field can be found within the [tutorial](http://ai2thor.allenai.org/tutorials/event-metadata).  The Event object contains a screen capture from the point the last action completed as well as metadata about each object within the scene.

```python
event = controller.step(dict(action=MoveAhead))

# Numpy Array - shape (width, height, channels), channels are in RGB order
event.frame

# byte[] PNG image
event.image

# current metadata dictionary that includes the state of the scene
event.metadata
```

## Actions

We currently provide the following API controlled actions. Actions are defined in ```unity/Assets/Scripts/PhysicsRemoteFPSAgentController.cs```. 

### Agent Movement and Orientation

When moving or rotating the agent, do note that if the agent is holding a Sim Object in its hand it could prevent moving or turning from succeeding. This is to prevent held objects from clipping with the environment.

#### RotateRight
Rotate the agent by 90 degrees to the right of its current facing
```python
event = controller.step(dict(action='RotateRight'))
```
#### RotateLeft
Rotate the agent by 90 degrees to the left of its current facing
```python
event = controller.step(dict(action='RotateLeft'))
```
#### LookUp
Angle the agent's view up in 30 degree increments (max upward angle is 30 degrees above the forward horizon)
```python
event = controller.step(dict(action='LookUp'))
```

#### LookDown
Angle the agent's view down in 30 degree increments (max downward angle is 60 degrees below the forward horizon)
```python
event = controller.step(dict(action='LookDown'))
```
#### Crouch
Lowers the Camera and Hand position of the Agent to observe the scene from a lower perspective. Do note that this does not actually make the Agent shorter, but only moves the Camera and Hand. The total height of the Agent is unaffected so this cannot be used to do something like move underneath a table.
```python
event = controller.step(dict(action='Crouch'))
```
#### Stand
If crouching, the Agent will "stand up"- resetting the Camera and Hand to the default upright position.
```python
event = controller.step(dict(action='Stand'))
```
#### MoveAhead
Move Ahead the given `moveMagnitude` in meters. If no `moveMagnitude` specified, it defaults to the initialized grid size
```python
event = controller.step(dict(action='MoveAhead'))
event = controller.step(dict(action='MoveAhead', moveMagnitude = 0.1))
```
#### MoveRight
Move Right the given `moveMagnitude` in meters. If no `moveMagnitude` specified, it defaults to the initialized grid size
```python
event = controller.step(dict(action='MoveRight'))
event = controller.step(dict(action='MoveRight', moveMagnitude = 0.1))
```
#### MoveLeft
Move Left the given `moveMagnitude` in meters. If no `moveMagnitude` specified, it defaults to the initialized grid size
```python
event = controller.step(dict(action='MoveLeft'))
event = controller.step(dict(action='MoveLeft', moveMagnitude = 0.1))
```
#### MoveBack
Move Backwards the given `moveMagnitude` in meters. If no `moveMagnitude` specified, it defaults to the initialized grid size
```python
event = controller.step(dict(action='MoveBack'))
event = controller.step(dict(action='MoveBack', moveMagnitude = 0.1))
```
#### Teleport 
Move the agent to any location in the scene (within scene bounds). Using this command it is possible to put the agent into places that would not normally be possible to navigate to, but it can be useful if you need to place an agent in the exact same spot for a task.
```python
event = controller.step(dict(action='Teleport', x=0.999, y=1.01, z=-0.3541))
``` 

### Sim Object Interaction
Check [The Sim Object Info Table](https://docs.google.com/spreadsheets/d/1wx8vWgmFSi-4Gknkwl2fUMG8oRedu-tUklGSvU0oh4U/edit?usp=sharing) and open the **Object Interactions** sheet for an info table on what Sim Object types can be interacted with in which ways.

#### Pickup Object

Pick up an Interactable object specified by `objectID` and move it to the Agent's Hand. Note that the agent's hand must be clear of obstruction- if the target object being in the Agent's Hand would cause it to clip into the environment, this will fail.

Picked up objects can also obstruct the Agent's view of the environment since the Agent's hand is always in camera view, so know that picking up larger objects will obstruct the field of vision.

**Moveable Receptacles:** Note that certain objects are Receptacles that can themselves be picked up. If a moveable receptacle is picked up while other Sim Objects are inside of it, the contained objects will be picked up with the moveable receptacle. This allows for sequences like "Place Egg on Plate -> Pick Up Plate" to move both the Plate and Egg.

```python
event = controller.step(dict(action='PickupObject', objectId="Mug|0.25|-0.27"))
```
#### Place Object
If a sim object is in the Agent's Hand, this will place it in/on a target receptacle specified by `objectID`. 

**Receptacle Restrictions:** By default, objects are restricted as to what type of receptacle they can be placed in. Please refer to [the Sim Object Info Table](https://docs.google.com/spreadsheets/d/1wx8vWgmFSi-4Gknkwl2fUMG8oRedu-tUklGSvU0oh4U/edit?usp=sharing), check the **_"Pickupable Object Restrictions"_** sheet, and use the dropdown menu under **_"Select Object Type"_** to see which object types can validly be placed in which receptacle types. 

If `forceAction = true` is passed in to this command, object placement will ignore the beforementioned placement restrictions.

Additionally, there are 2 "modes" that can be used when placing an object:

**Physics Resolution Mode:**
If `placeStationary = false` is passed in, a placed object will use the physics engine to resolve the final position. This means placing an object on an uneven surface may cause inconsistent results due to the object rolling around or even falling off of the target receptacle.

**Stationary Mode:**
If `placeStationary = true` is passed in, the object will be placed in/on the valid receptacle without using physics to resolve the final position. This means that the object will be placed so that it will not roll around.

Note that regardless of which placement mode is used, if another moving object hits a placed object, or if the Push/Pull actions are used on a placed object, the placed object will react with physics.

Place a held item on a Table in Stationary Mode.
```python
event = controller.step(dict(action='PlaceHeldObject', objectId = "TableTop|0.25|-0.27|0.95", placeStationary = true))
 ```
Place a held item in a Fridge, ignoring receptacle restrictions
```python
event = controller.step(dict(action='PlaceHeldObject', objectId = "Fridge|0.45|0.23|0.94", forceAction = true))
```
Place a held item on a Table, in Physics Resolution Mode, and ignoring placement restrictions.
```python
event = controller.step(dict(action='PlaceHeldObject', objectId = "Table|0.25|-0.27|0.95", forceAction = true, placeStationary = false))
```
#### Drop Object
Drop a held object and let Physics resolve where it lands. Note that this is different from the "Place Object" function, as this does not guarantee the held object will be put into a specified receptacle. This is meant to be used in tandem with the Move/Rotate Hand functions to maneuver a held object to a target area, and the let it drop.

Additionally, this Drop action will fail if the held object is not clear from all collisions. Most importantly, the Agent's collision will prevent Drop, as dropping an object if it is "inside" the agent will lead to unintended behavior.
```python
event = controller.step(dict(action='DropHandObject')))
```
#### Throw Object
Throw a held object in the current forward direction of the Agent at a force specified by `moveMagnitude`. Because objects can have different Mass properties, certain objects will require more or less force to push the same distance. 
```python
event = controller.step(dict(action='ThrowObject', moveMagnitude= 150 )))
```
#### OpenObject 
Open an object specified by `objectID`. 

If a `moveMagnitude` value is passed in as well, it can be used to open an object a percentage of its full "open" position. To do this, pass a value between 0 and 1 (1 being fully open). The second example here would open the Fridge halfway.

The target object must be within range of the Agent and Interactable in order for this action to succeed. An object can fail to open if it hits another object as it is opening. In this case the action will fail and the target object will reset to the position it was last in.
```python
event = controller.step(dict(action='OpenObject', objectId="Fridge|0.25|0.75"))
event = controller.step(dict(action='OpenObject', objectId="Fridge|0.25|0.75", moveMagnitude = 0.5))
```
#### CloseObject
Close an object specified by `objectID`.

The target object must be within range of the Agent and Interactable in order for this action to succeed. An object can fail to open if it hits another object as it is closing. In this case the action will fail and the target object will reset to the position it was last in.
```python
event = controller.step(dict(action='CloseObject', objectId="Fridge|0.25|0.75"))
```
#### Push Object
Push a target object specified by `objectID` away from the Agent (relative to the Agent's forward orientation) with a given force. Force of the push is set with `moveMagnitude`. 

Sim Objects can have different Mass properties, so certain objects will require more or less force to push the same distance. A heavy Chair object with 20kg of mass might require 4000 force to push, while a Tomato that is only 0.2kg would only require 10 units of force.

Note that this cannot be used to Push an object held by the Agent in its hand. Refer to the "Throw" action for that.
```python
event = controller.step(dict(action='PushObject', objectID = "Statue|0.25|-0.27|0.95", moveMagnitude= 20 )))
```
#### Pull Object
Pull a target object specified by `objectID` towards the Agent (relative to the Agent's forward orientation) with a given force. Force of the pull is set with `moveMagnitude`. 

Sim Objects can have different Mass properties, so certain objects will require more or less force to pull the same distance. A heavy Chair object with 20kg of mass might require 4000 force to push, while a Tomato that is only 0.2kg would only require 10 units of force.

Note that this cannot be used to Pull an object held by the Agent in its hand. 
```python
event = controller.step(dict(action='PullObject', objectID = "Statue|0.25|-0.27|0.95", moveMagnitude= 20 )))
```
#### Toggle On
Toggles an object specified by `objectID` into the On state if applicable. Noteable examples are Lamps, Light Switches, and Laptops.
```python
event = controller.step(dict(action='ToggleObjectOn', objectId= "LightSwitch|0.25|-0.27|0.95")))
```
#### Toggle Off
Toggles an object specified by `objectID` into the Off state if applicable. Noteable examples are Lamps, Light Switches, and Laptops.
```python
event = controller.step(dict(action='ToggleObjectOff', objectId= "LightSwitch|0.25|-0.27|0.95")))
```

### Agent Hand/Object Manipulation
If the agent has picked up an object, it can manipulate the position and rotation of the Hand and the object held. The position of the Agent's hand is constrained by the Camera viewport and cannot be moved outside of the viewport.

#### Move Hand Forward
Moves the hand forward relative to the agent's current facing the given moveMagnitude in meters.
```python
event = controller.step(dict(action='MoveHandAhead', moveMagnitude = 0.1))
```
#### Move Hand Back
Moves the hand back relative to the agent's current facing the given moveMagnitude in meters.
```python
event = controller.step(dict(action='MoveHandBack', moveMagnitude = 0.1))
```
#### Move Hand Left
Moves the hand left relative to the agent's current facing the given moveMagnitude in meters.
```python
event = controller.step(dict(action='MoveHandLeft', moveMagnitude = 0.1))
```
#### Move Hand Right
Moves the hand right relative to the agent's current facing the given moveMagnitude in meters.
```python
event = controller.step(dict(action='MoveHandRight', moveMagnitude = 0.1))
```
#### Move Hand Up
Moves the hand up relative to the agent's current facing the given moveMagnitude in meters.
```python
event =controller.step(dict(action='MoveHandUp', moveMagnitude = 0.1))
```
#### Move Hand Down
Moves the hand down relative to the agent's current facing the given moveMagnitude in meters.
```python
event = controller.step(dict(action='MoveHandDown', moveMagnitude = 0.1))
```
#### Rotate Hand
Rotates the hand and held object about the specified axes (x, y, z) the specified number of degrees. These examples rotate a held object 90 degrees about the each axis. 
```python
event = controller.step(dict(action='RotateHand', x = 90))
event = controller.step(dict(action='RotateHand', y = 90))
event = controller.step(dict(action='RotateHand', z = 90))
```
Multiple Axes can be specified at once as well.
```python
event = controller.step(dict(action='RotateHand', x = 90, y = 15, z = 28))
```

## Architecture

AI2-THOR is made up of two components: a set of scenes built for the Unity game engine located in ```unity``` folder, a lightweight Python API that interacts with the game engine located in ```ai2thor``` folder.

On the Python side there is a Flask service that listens for HTTP requests from the Unity game engine. After an action is executed within the game engine, a screen capture is taken and a JSON metadata object is constructed from the state of all the objects of the scene and POST'd to the Python Flask service.  This payload is then used to construct an Event object comprised of a numpy array (the screen capture) and metadata (dictionary containing the current state of every object including the agent).  At this point the game engine waits for a response from the Python service, which it receives when the next ```controller.step()``` call is made.  Once the response is received within Unity, the requested action is taken and the process repeats.


## Unity Development

If you wish to make changes to the Unity scenes/assets you will need to install Unity Editor version 2018.3.6 for OSX (Linux Editor is currently in Beta) from [Unity Download Archive](https://unity3d.com/get-unity/download/archive).  After making your desired changes using the Unity Editor you will need to build.  To do this you must first exit the editor, then run the following commands from the ai2thor base directory. Individual scenes (the 3D models) can be found beneath the unity/Assets/Scenes directory - scenes are named FloorPlan###.

```python
pip install invoke
invoke local-build
```

This will create a build beneath the directory 'unity/builds/local-build/thor-local-OSXIntel64.app'. To use this build in your code, make the following change:

```python
controller = ai2thor.controller.Controller()
controller.local_executable_path = "<BASE_DIR>/unity/builds/local-build/thor-local-OSXIntel64.app/Contents/MacOS/thor-local-OSXIntel64"
controller.start()
```

## Citation

    @article{ai2thor,
        Author = {Eric Kolve and 
                  Roozbeh Mottaghi and 
                  Daniel Gordon and 
                  Yuke Zhu and 
                  Abhinav Gupta and 
                  Ali Farhadi},
        Title = {{AI2-THOR: An Interactive 3D Environment for Visual AI}},
        Journal = {arXiv},
        Year = {2017}
    }
    

## Support

We have done our best to fix all bugs and issues. However, you might still encounter some bugs during navigation and interaction. We will be glad to fix the bugs. Please open issues for these and include the scene name as well as the event.metadata from the moment that the bug can be identified.


## Team

AI2-THOR is an open-source project backed by [the Allen Institute for Artificial Intelligence (AI2)](http://www.allenai.org).
AI2 is a non-profit institute with the mission to contribute to humanity through high-impact AI research and engineering.
