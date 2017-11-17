<p align="center"><img width="30%" src="doc/static/thor-logo-main.png" /></p>

--------------------------------------------------------------------------------


THOR is an interactive 3D Visual AI platform that allows an agent to be controlled via an API through Python.

## Tutorial

Please refer to the [tutorial page](http://ai2thor.allenai.org/tutorials/installation) for a detailed walkthrough.

## Requirements

* OS: Mac OS X 10.9+, Ubuntu 14.04+
* Graphics Card: DX9 (shader model 3.0) or DX11 with feature level 9.3 capabilities.
* CPU: SSE2 instruction set support.
* Python 2.7 or Python 3.5+
* Linux: X server with GLX module enabled

## Concepts

* Agent: A capsule shaped entity that can navigate within scenes and interact with objects.
* Scene: A scene within THOR represents a virtual room that an agent can navigate in and interact with.
* Action: A discrete command for the Agent to perform within a scene (e.g. MoveAhead, RotateRight, PickupObject)
* Object Visibility: An object is said to be visible when it is within a threshold of distance (default: 1 meter) when measured from the Agent’s camera to the centerpoint of the target object. This determines whether the agent can interact with the object or not.
* Receptacle: A type of object that can contain another object. These types of objects include: sinks, refrigerators, cabinets and tabletops. A receptacle cannot be picked up.


## Scene Initialization

Before performing any actions a scene must be loaded.

```python
import ai2thor.controller
controller = ai2thor.controller.Controller()
controller.start()

# The scene can be any of the following:
# FloorPlan1 - FloorPlan30, FloorPlan201 - FloorPlan230, FloorPlan301 - FloorPlan330, FloorPLan401 - FloorPlan430
controller.reset('FloorPlan28')

# gridSize determines the step size the agent moves 
controller.step(dict(action='Initialize', gridSize=0.25))
```

## Actions

We currently provide the following API controlled actions. New actions can be easily added to the API.

#### MoveAhead
Move ahead in the amount of the grid size
```python
event = controller.step(dict(action='MoveAhead'))
```

#### MoveRight
Move right in the amount of the grid size
```python
event = controller.step(dict(action='MoveRight'))
```
#### MoveLeft
Move left in the amount of the grid size
```python
event = controller.step(dict(action='MoveLeft'))
```
#### MoveBack
Move back in the amount of the grid size
```python
event = controller.step(dict(action='MoveBack'))
```
#### RotateRight
Rotate the agent by 90 degrees to the right
```python
event = controller.step(dict(action='RotateRight'))
```

#### RotateLeft
Rotate the agent by 90 degrees to the left
```python
event = controller.step(dict(action='RotateLeft'))
```

#### OpenObject
Open an object (assuming the object is visible to the agent). In the case of the Refrigerator, the door will open.
```python
event = controller.step(dict(action='OpenObject', objectId="Fridge|0.25|0.75"))
```

#### CloseObject
Close an object (assuming object is visible to the agent). In the case of the Refrigerator, the door will fridge.
```python
event = controller.step(dict(action='CloseObject', objectId="Fridge|0.25|0.75"))
```

#### PickupObject

Pick a visible object up that is in a scene and place it into the Agent’s inventory. Currently the Agent can only have a single object in its inventory. See below for a more complex example.

```python
event = controller.step(dict(action='PickupObject', objectId="Mug|0.25|-0.27"))
```


#### PutObject
Put an object in the Agent’s inventory into a visible receptacle. In order for this to work, the agent must pick up a visible Mug and open a visible Fridge. See below for a more complete example.

```python
event = controller.step(dict(
    objectId="Mug|0.25|-0.27",
    receptacleObjectId="Fridge|0.05|0.75"))
 ```
 
#### Event/Metadata
Each call to ```controller.step()``` returns an instance of an Event.  Detailed descriptions of each field can be found within the [tutorial](http://ai2thor.allenai.org/tutorials/event-metadata).  The Event object contains a screen capture from the point the last action completed as well as metadata about each object within the scene.

```python
event = controller.step(dict(action=MoveAhead))

# Numpy Array - shape (width, height, channels), channels are in RGB order
event.frame

# byte[] PNG image
event.image()

# Metadata dictionary
event.metadata
```


## PIP Installation

```bash
pip install ai2thor
```
Once installed you can now launch the framework.

```python
import ai2thor.controller
controller = ai2thor.controller.Controller()
controller.start()
# can be any one of the scenes FloorPlan###
controller.reset('FloorPlan28')
controller.step(dict(action='Initialize', gridSize=0.25))
event = controller.step(dict(action='MoveAhead'))

# current frame (numpy array)
event.image

# current metadata about the state of the scene
event.metadata

```
Upon executing the ```controller.start()``` a window should appear on screen with a view of the room FloorPlan28.


## Architecture

AI2Thor is made up of two components: a set of scenes built within the Unity Game engine, a lightweight Python API that interacts with the game engine.

On the Python side there is a Flask service that listens for HTTP requests from the Unity Game engine. After an action is executed within the game engine, a screen capture is taken and a JSON metadata object is constructed from the state of all the objects of the scene and POST'd to the Python Flask service.  This payload is then used to construct an Event object comprised of a numpy array (the screen capture) and metadata (dictionary containing the current state of every object including the agent).  At this point the game engine waits for a response from the Python service, which it receives when the next ```controller.step()``` call is made.  Once the response is received within Unity, the requested action is taken and the process repeats.

## Support

We have done our best to remove all bugs and issues. However, you might still encounter some bugs during navigation and interaction. We will be glad to fix the bugs. Please open issues for these and include the scene name as well as the event.metadata from the moment that the bug can be identified.


## Team

AI2Thor is an open-source project backed by [the Allen Institute for Artificial Intelligence (AI2)](http://www.allenai.org).
AI2 is a non-profit institute with the mission to contribute to humanity through high-impact AI research and engineering.
