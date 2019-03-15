[![Build Status](https://travis-ci.org/allenai/ai2thor.svg?branch=master)](https://travis-ci.org/allenai/ai2thor)
<p align="center"><img width="40%" src="doc/static/thor-logo-main_1.0_thick.png" /></p>

--------------------------------------------------------------------------------

AI2-THOR (The House Of inteRactions) is a photo-realistic interactable framework for AI agents.

## Documentation

Please refer to the [Documentation Page](http://ai2thor.allenai.org/documentation/) on the AI2-THOR website for more detailed information.

## News
* (3/2019) Introducing Version 1.0 of the AI2-THOR framework! This release includes a full rework of all Sim Objects and Scenes to have additional physics functionality and improved fidelity. Physics based interactions can now be modeled in the THOR environment in realistic ways like never before! Object collision when placed in receptacles, moveable receptacles that contain other objects, collision based object position randomization, Multi-Agent support— these are a few of the many exciting new features that come with this update. Please check the [full release notes here](doc/static/ReleaseNotes/ReleaseNotes_1.0.md) to view details on all the changes and new features.
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

* **Sim Object:** Objects that can be interacted with by the Agent. Objects have a range of interactions based on affordanced defined by the Object Type.

* **Object Visibility:** An object is considered Visible when it satisfies three conditions: It must be within the Camera's viewport, it must be within a threshold of distance from the Agent's center (default: 1.5 meters), and a ray emitted from the camera must hit the object without first hitting another obstruction. Note that an object rendered in an image will not always be Visible to the Agent. For example, an object outside the 1.5 meter threshold could be seen in an image, but will be reported as not-visible to the Agent.

* **Object Interactability:** An object is said to be Interactable if it is flagged as Visible and if it is unobstructed by any other objects. Most objects will be Interactable as long as they are also Visible, but some objects have transparency which can cause objects to be reported as Visible through them. An example is a Glass Shower Door with a Sponge object behind it. The glass door will be flagged as Visible and Interactable, but the sponge will only be Visible. Attempting to interact with the sponge will throw an error as it cannot be reached through the glass door, only seen.


* **Receptacle:** A type of object that can contain another object. Some examples of receptacles are: TableTop, Cup, Sofa, Bed, Desk, Bowl, etc. Some Receptacles cannot be moved within the scene, and are mostly large objects that do not make sense to move (Countertop, Sink, etc). Some Receptacles can also open and close (Microwave, Cabinet, Drawer, etc.) while others can also be picked up and moved around by the Agent (Plate, Bowl, Box, etc).

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
Each call to ```controller.step()``` returns an instance of an Event.  Detailed descriptions of each field can be found on the [Documentation Page](http://ai2thor.allenai.org/documentation/event-metadata).  The Event object contains a screen capture from the point the last action completed as well as metadata about each object within the scene.

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

<hr>

## Object Position Randomization

After initializing the scene, pickupable objects can have their default positions randomized to any valid receptacle they could be placed in within the scene.

```python
controller.reset('FloorPlan28')
controller.step(dict(action='Initialize', gridSize=0.25))
controller.step(dict(action = 'InitialRandomSpawn', randomSeed = 0, forceVisible = false, maxNumRepeats = 5))
```
Remember to reset and initiialize the scene before using the Position Randomizer, otherwise seeded values will be innacurate. 

| Parameter | Type | Description | Default |
|-----------|------|-------------|---------|
|randomSeed| int | Used to seed the randomization for duplicatable scene states. Because this seed depends on the current state of the scene, remember to reset the scene with controller.reset() before running InitialRandomSpawn(), otherwise the seeded randomization will not be accurate | 0 |
|forceVisible | bool | When enabled, the scene will attempt to randomize all moveable objects outside of receptacles in plain view. Use this if you want to avoid objects spawning inside closed drawers, cabinets, etc.| False|
|maxNumRepeats | int | how many times each object in the scene attempts to randomly spawn. Setting this value higher will lead to fewer spawn failures at the cost of performance | 5|

<hr>

## Agent Navigation

The agent can use these actions to navigate through the environment.
<hr>

### RotateRight
Rotate the agent by 90 degrees to the right of its current facing
```python
event = controller.step(dict(action='RotateRight'))
```
<hr>

### RotateLeft
Rotate the agent by 90 degrees to the left of its current facing
```python
event = controller.step(dict(action='RotateLeft'))
```
<hr>

### LookUp
Angle the agent's view up in 30 degree increments (max upward angle is 30 degrees above the forward horizon)
```python
event = controller.step(dict(action='LookUp'))
```
<hr>

### LookDown
Angle the agent's view down in 30 degree increments (max downward angle is 60 degrees below the forward horizon)
```python
event = controller.step(dict(action='LookDown'))
```
<hr>

### MoveAhead
Move the agent forward by `gridSize`.
```python
event = controller.step(dict(action='MoveAhead'))
```
<hr>

### MoveRight
Move the agent right by `gridSize` (without changing view direction).
```python
event = controller.step(dict(action='MoveRight'))
```
<hr>

### MoveLeft
Move the agent left by `gridSize` (without changing view direction).
```python
event = controller.step(dict(action='MoveLeft'))
```
<hr>

### MoveBack
Move the agent backward by `gridSize` (without changing view direction).
```python
event = controller.step(dict(action='MoveBack'))
```
<hr>


## Agent Teleportation
Use these actions to move the agent through the scene by warping to specified points rather than having to navigate to positions step by step.
<hr>

### Teleport
Move the agent to any location in the scene. Using this command it is possible to put the agent into places that would not normally be possible to navigate to, but it can be useful if you need to place an agent in the exact same spot for a task.
```python
controller.step(dict(action='Teleport', x=0.999, y=1.01, z=-0.3541))
```

| Parameter | Type | Description | Default |
|-----------|------|-------------|---------|
|x| float | x coordinate in 3D scene space | 0.0 |
|y| float | y coordinate in 3D scene space | 0.0 |
|z| float | z coordinate in 3D scene space | 0.0 |

<hr>

### TeleportFull
Move the agent to any location in the scene. Using this command it is possible to put the agent into places that would not normally be possible to navigate to, but it can be useful if you need to place an agent in the exact same spot for a task. Identical to Telport, but also allows rotation and horizon to be passed in.
```python
event = controller.step(dict(action='TeleportFull', x=0.999, y=1.01, z=-0.3541, rotation=90.0, horizon=30.0))
```

| Parameter | Type | Description | Default |
|-----------|------|-------------|---------|
|x| float | x coordinate in 3D scene space | 0.0 |
|y| float | y coordinate in 3D scene space | 0.0 |
|z| float | z coordinate in 3D scene space | 0.0 |
|rotation| float | Forward orientation of the Agent relative to world x/z axes | 0.0 |
|horizon| float | Up/Down look angle of the Agent. Valid values are -60 to +30 degrees in 30 degree increments | 0.0 |

<hr>

### Get Reachable Positions
Returns valid coordinates that the Agent can reach without colliding with the environment or Sim Objects. This can be used in tandem with `Teleport` to warp the Agent as needed. This is useful for things like randomizing the initial position of the agent without clipping into the environment.
```python
event = controller.step(dict(action='GetReachablePositions'))
``` 
<hr>

## Object Interaction
These actions allow the agent to interact with Sim Objects in the scene in various ways.
<hr>

### Pickup Object

Pick up an Interactable object specified by `objectID` and move it to the Agent's Hand. Note that the agent's hand must be clear of obstruction- if the target object being in the Agent's Hand would cause it to clip into the environment, this will fail.

Picked up objects can also obstruct the Agent's view of the environment since the Agent's hand is always in camera view, so know that picking up larger objects will obstruct the field of vision.

**Moveable Receptacles:** Note that certain objects are Receptacles that can themselves be picked up. If a moveable receptacle is picked up while other Sim Objects are inside of it, the contained objects will be picked up with the moveable receptacle. This allows for sequences like "Place Egg on Plate -> Pick Up Plate" to move both the Plate and Egg.

```python
event = controller.step(dict(action='PickupObject', objectId="Mug|0.25|-0.27"))
```

| Parameter | Type | Description | Default |
|-----------|------|-------------|---------|
|objectId| string | the string unique id of the target object | null |

<hr>

### Put Object
Attempt to Put an object the Agent is holding onto/in the target Receptacle.
```python
event = controller.step(dict(action='PutObject', objectId = "Tomato|0.1|3.2|0.43", receptacleObjectId = "TableTop|0.25|-0.27|0.95"))
 ```

 | Parameter | Type | Description | Default |
|-----------|------|-------------|---------|
|objectId| string| The string unique id of the object in the Agent's hand that it is trying to put down. Note that an error will be thrown if trying to put an object the Agent is not holding|null|
|receptacleObjectId| string| string unique id of target receptacle to attempt putting the object in/on.|null|
|forceAction | bool | Enable to ignore any Receptacle Restrictions when attempting to place objects. Normally objects will fail to be put on a receptacle if that receptacle is not valid for the object. See the [Receptacle Object Types](receptacle-object-types) page for more details.|False|
|placeStationary| bool | If `placeStationary = False` is passed in, a placed object will use the physics engine to resolve the final position. This means placing an object on an uneven surface may cause inconsistent results due to the object rolling around or even falling off of the target receptacle. Note that because of variances in physics resolution, this placement mode is non-determanistic! If `placeStationary = True`, the object will be placed in/on the valid receptacle without using physics to resolve the final position. This means that the object will be placed so that it will not roll around. For determanistic placement make sure to set to true! | True|

<hr>
 
### Drop Object
Drop a held object and let Physics resolve where it lands. Note that this is different from the Put Object function, as this does not guarantee the held object will be put into a specified receptacle. This is meant to be used in tandem with the Move/Rotate Hand functions to maneuver a held object to a target area, and the let it drop.

Additionally, this Drop action will fail if the held object is not clear from all collisions. Most importantly, the Agent's collision will prevent Drop, as dropping an object if it is "inside" the agent will lead to unintended behavior.
```python
event = controller.step(dict(action='DropHandObject')))
```
<hr>

### Throw Object
An extention of the Drop function-throw a held object in the current forward direction of the Agent at a force specified by `moveMagnitude`. Because objects can have different Mass properties, certain objects will require more or less force to push the same distance. 
```python
event = controller.step(dict(action='ThrowObject', moveMagnitude= 150.0 )))
```

| Parameter | Type | Description | Default |
|-----------|------|-------------|---------|
|moveMagnitude| float | The amount of force used to throw the object. Note that objects of different masses will have different throw distances if this magnitude is not changed | 0.0 |

<hr>

### OpenObject 
Open an object specified by `objectID`. 

The target object must be within range of the Agent and Interactable in order for this action to succeed. An object can fail to open if it hits another object as it is opening. In this case the action will fail and the target object will reset to the position it was last in.
```python
event = controller.step(dict(action='OpenObject', objectId="Fridge|0.25|0.75"))
```
Here is an example of opening the Fridge halfway:
```python
event = controller.step(dict(action='OpenObject', objectId="Fridge|0.25|0.75", moveMagnitude = 0.5))
```

| Parameter | Type | Description | Default |
|-----------|------|-------------|---------|
|objectId| string | the string unique id of the target object | null |
|moveMagnitude| float | Pass a magnitude between 0.0 and 1.0 to open by the corresponding percentage. For example, a magnitude of 0.5 will cause the object to open halfway, a value of 0.25 will open the object a quarter of it's full open position| 0.0 |

<hr>

### CloseObject
Close an object specified by `objectID`.

The target object must be within range of the Agent and Interactable in order for this action to succeed. An object can fail to open if it hits another object as it is closing. In this case the action will fail and the target object will reset to the position it was last in.

```python
event = controller.step(dict(action='CloseObject', objectId="Fridge|0.25|0.75"))
```

| Parameter | Type | Description | Default |
|-----------|------|-------------|---------|
|objectId| string | the string unique id of the target object | null |

<hr>

### Toggle On
Toggles an object specified by `objectID` into the On state if applicable. Noteable examples are Lamps, Light Switches, and Laptops.

```python
event = controller.step(dict(action='ToggleObjectOn', objectId= "LightSwitch|0.25|-0.27|0.95")))
```

| Parameter | Type | Description | Default |
|-----------|------|-------------|---------|
|objectId| string | the string unique id of the target object | null |

<hr>

### Toggle Off
Toggles an object specified by `objectID` into the Off state if applicable. Noteable examples are Lamps, Light Switches, and Laptops.

```python
event = controller.step(dict(action='ToggleObjectOff', objectId= "LightSwitch|0.25|-0.27|0.95")))
```

| Parameter | Type | Description | Default |
|-----------|------|-------------|---------|
|objectId| string | the string unique id of the target object | null |

<hr>

## Object Manipulation
After the agent has picked up a Sim Object that is pickupable, these actions can be used to manipulate held items in various ways.
<hr>

### Move Hand Forward
Moves the Agent's hand and held object forward relative to the agent's current facing. The hand can only be moved if it is holding an object.
```python
event = controller.step(dict(action='MoveHandAhead', moveMagnitude = 0.1))
```

| Parameter | Type | Description | Default |
|-----------|------|-------------|---------|
|moveMagnitude| float | The distance, in meters, to move the hand in this direction | 0.0 |

<hr>

### Move Hand Back
Moves the Agent's hand and held object backward relative to the agent's current facing. The hand can only be moved if it is holding an object.
```python
event = controller.step(dict(action='MoveHandBack', moveMagnitude = 0.1))
```

| Parameter | Type | Description | Default |
|-----------|------|-------------|---------|
|moveMagnitude| float | The distance, in meters, to move the hand in this direction | 0.0 |

<hr>

### Move Hand Left
Moves the Agent's hand and held object left relative to the agent's current facing. The hand can only be moved if it is holding an object.
```python
event = controller.step(dict(action='MoveHandLeft', moveMagnitude = 0.1))
```

| Parameter | Type | Description | Default |
|-----------|------|-------------|---------|
|moveMagnitude| float | The distance, in meters, to move the hand in this direction | 0.0 |

<hr>

### Move Hand Right
Moves the Agent's hand and held object right relative to the agent's current facing. The hand can only be moved if it is holding an object.
```python
event = controller.step(dict(action='MoveHandRight', moveMagnitude = 0.1))
```

| Parameter | Type | Description | Default |
|-----------|------|-------------|---------|
|moveMagnitude| float | The distance, in meters, to move the hand in this direction | 0.0 |

<hr>

### Move Hand Up
Moves the Agent's hand and held object up relative to the agent's current facing. The hand can only be moved if it is holding an object.
```python
event = controller.step(dict(action='MoveHandUp', moveMagnitude = 0.1))
```

| Parameter | Type | Description | Default |
|-----------|------|-------------|---------|
|moveMagnitude| float | The distance, in meters, to move the hand in this direction | 0.0 |

<hr>

### Move Hand Down
Moves the Agent's hand and held object down relative to the agent's current facing. The hand can only be moved if it is holding an object.
```python
event = controller.step(dict(action='MoveHandDown', moveMagnitude = 0.1))
```

| Parameter | Type | Description | Default |
|-----------|------|-------------|---------|
|moveMagnitude| float | The distance, in meters, to move the hand in this direction | 0.0 |

<hr>

### Rotate Hand
Rotates the hand and held object about the specified axes (x, y, z) to the specified degree. These examples rotate a held object to 90 degrees about each axis. 
```python
event = controller.step(dict(action='RotateHand', x = 90))
event = controller.step(dict(action='RotateHand', y = 90))
event = controller.step(dict(action='RotateHand', z = 90))
```
Multiple Axes can be specified at once as well. 
```python
event = controller.step(dict(action='RotateHand', x = 90, y = -15, z = 28))
```

| Parameter | Type | Description | Default |
|-----------|------|-------------|---------|
|x| float | rotation about the object's x axis | 0.0 |
|y| float | rotation about the object's y axis | 0.0 |
|z| float | rotation about the object's z axis | 0.0 |

<hr>

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
        Author = {Eric Kolve and Roozbeh Mottaghi 
                  and Winson Han and Eli VanderBilt 
                  and Luca Weihs and Alvaro Herrasti 
                  and Daniel Gordon and Yuke Zhu 
                  and Abhinav Gupta and Ali Farhadi},
        Title = {{AI2-THOR: An Interactive 3D Environment for Visual AI}},
        Journal = {arXiv},
        Year = {2017}
    }
    

## Support

We have done our best to fix all bugs and issues. However, you might still encounter some bugs during navigation and interaction. We will be glad to fix the bugs. Please open issues for these and include the scene name as well as the event.metadata from the moment that the bug can be identified.


## Team

AI2-THOR is an open-source project backed by [the Allen Institute for Artificial Intelligence (AI2)](http://www.allenai.org).
AI2 is a non-profit institute with the mission to contribute to humanity through high-impact AI research and engineering.
