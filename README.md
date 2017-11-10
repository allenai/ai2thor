
## AI2Thor

An [Apache 2.0](https://github.com/allenai/allennlp/blob/master/LICENSE) Visual AI Platform based on the Unity game engine. Please read the [tutorial](http://ai2thor.allenai.org/tutorials/installation) for a more detailed walkthrough.

## PIP Installation

AI2Thor will run on OSX and Linux platforms with Python 2.7+/Python 3.5+.
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

## What is AI2Thor?

THOR is an interactive 3D Visual AI platform that allows an agent to be controlled via an API through Python. The framework advances AI by providing capabilities for learning by interaction in near-photo-realistic scenes. The current state-of-the-art AI models are trained using still images or videos or trained in non-realistic settings such as ATARI games, which are very different from how humans learn. AI2Thor is a step towards learning like humans.

## Architecture

AI2Thor is made up of two components: a set of scenes built within the Unity Game engine, a lightweight Python API that interacts with the game engine.

On the Python side there is a Flask service that listens for HTTP requests from the Unity Game engine. After an action is executed within the game engine, a screen capture is taken and a JSON metadata object is constructed from the state of all the objects of the scene and POST'd to the Python Flask service.  This payload is then used to construct an Event object comprised of a numpy array (the screen capture) and metadata (dictionary containing the current state of every object including the agent).  At this point the game engine waits for a response from the Python service, which it receives when the next ```controller.step()``` call is made.  Once the response is received within Unity, the requested action is taken and the process repeats.


## Team

AI2Thor is an open-source project backed by [the Allen Institute for Artificial Intelligence (AI2)](http://www.allenai.org).
AI2 is a non-profit institute with the mission to contribute to humanity through high-impact AI research and engineering.
