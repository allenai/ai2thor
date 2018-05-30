<p align="center"><img width="30%" src="doc/static/thor-logo-main.png" /></p>

--------------------------------------------------------------------------------


AI2-THOR is a photo-realistic interactable framework for AI agents.

## Tutorial

Please refer to the [tutorial page](http://ai2thor.allenai.org/tutorials/) for a detailed walkthrough.

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

* Agent: A capsule shaped entity that can navigate within scenes and interact with objects.
* Scene: A scene within AI2-THOR represents a virtual room that an agent can navigate in and interact with.
* Action: A discrete command for the Agent to perform within a scene (e.g. MoveAhead, RotateRight, PickupObject)
* Object Visibility: An object is said to be visible when it is in camera view and within a threshold of distance (default: 1 meter) when measured from the Agent’s camera to the centerpoint of the target object. This determines whether the agent can interact with the object or not.
* Receptacle: A type of object that can contain another object. These types of objects include: sinks, refrigerators, cabinets and tabletops. 

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

We currently provide the following API controlled actions. New actions such as turning on faucet or slicing a loaf of bread can be easily added to the API. Actions are defined in ```unity/Assets/Scripts/DiscreteRemoteFPSAgentController.cs```. Please refer to [this page](http://ai2thor.allenai.org/tutorials/object-types) to check which objects are actionable.

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
Close an object (assuming object is visible to the agent). In the case of the Refrigerator, the door will close.
```python
event = controller.step(dict(action='CloseObject', objectId="Fridge|0.25|0.75"))
```

#### PickupObject

Pick up a visible object and place it into the Agent’s inventory. Currently the Agent can only have a single object in its inventory. 

```python
event = controller.step(dict(action='PickupObject', objectId="Mug|0.25|-0.27"))
```

#### PutObject
Put an object in the Agent’s inventory into a visible receptacle. In the following example, it is assumed that the agent holds a Mug in its inventory, and there is an open visible Fridge. 

```python
event = controller.step(dict(
    objectId="Mug|0.25|-0.27",
    receptacleObjectId="Fridge|0.05|0.75"))
 ```
#### Teleport
Move the agent to any location in the scene. Using this command it is possible to put the agent into places that would not normally be possible to navigate to, but it can be useful if you need to place an agent in the exact same spot for a task.
```python
event = controller.step(dict(action='Teleport', x=0.999, y=1.01, z=-0.3541))
``` 

## Architecture

AI2-THOR is made up of two components: a set of scenes built for the Unity game engine located in ```unity``` folder, a lightweight Python API that interacts with the game engine located in ```ai2thor``` folder.

On the Python side there is a Flask service that listens for HTTP requests from the Unity game engine. After an action is executed within the game engine, a screen capture is taken and a JSON metadata object is constructed from the state of all the objects of the scene and POST'd to the Python Flask service.  This payload is then used to construct an Event object comprised of a numpy array (the screen capture) and metadata (dictionary containing the current state of every object including the agent).  At this point the game engine waits for a response from the Python service, which it receives when the next ```controller.step()``` call is made.  Once the response is received within Unity, the requested action is taken and the process repeats.


## Unity Development

If you wish to make changes to the Unity scenes/assets you will need to install Unity Editor version 2017.3.1f1 for OSX (Linux Editor is currently in Beta) from [Unity Download Archive](https://unity3d.com/get-unity/download/archive).  After making your desired changes using the Unity Editor you will need to build.  To do this you must first exit the editor, then run the following commands from the ai2thor base directory. Individual scenes (the 3D models) can be found beneath the unity/Assets/Scenes directory - scenes are named FloorPlan###.

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
