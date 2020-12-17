[![Build Status](https://travis-ci.org/allenai/ai2thor.svg?branch=master)](https://travis-ci.org/allenai/ai2thor)
<p align="center"><img width="50%" src="doc/static/logo.svg" /></p>

--------------------------------------------------------------------------------

[AI2-THOR (The House Of inteRactions)](https://ai2thor.allenai.org/) is a near photo-realistic interactable framework for AI agents.

## News

* (6/2020) We provide a mini-framework to simplify running AI2-THOR in Docker. It can be accssed at: [https://github.com/allenai/ai2thor-docker](https://github.com/allenai/ai2thor-docker).

* (4/2020) Version 2.4.0 update of the framework is here. All sim objects that aren't explicitly part of the environmental structure are now moveable with physics interactions. New object types have been added, and many new actions have been added. Please see the [full 2.4.0 release notes here](doc/static/ReleaseNotes/ReleaseNotes_2.4.md)

* (2/2020) AI2-THOR now includes two frameworks: [iTHOR](https://ai2thor.allenai.org/ithor/) and [RoboTHOR](https://ai2thor.allenai.org/robothor/). iTHOR includes interactive objects and scenes and RoboTHOR consists of simulated scenes and their corresponding real world counterparts.  

* (9/2019) Version 2.1.0 update of the framework has been added. New object types have been added. New Initialization actions have been added. Segmentation image generation has been improved in all scenes. 

* (6/2019) Version 2.0 update of the AI2-THOR framework is now live! We have over quadrupled our action and object states, adding new actions that allow visually distinct state changes such as broken screens on electronics, shattered windows, breakable dishware, liquid fillable containers, cleanable dishware, messy and made beds and more! Along with these new state changes, objects have more physical properties like Temperature, Mass, and Salient Materials that are all reported back in object metadata. To combine all of these new properties and actions, new context sensitive interactions can now automatically change object states. This includes interactions like placing a dirty bowl under running sink water to clean it, placing a mug in a coffee machine to automatically fill it with coffee, putting out a lit candle by placing it in water, or placing an object over an active stove burner or in the fridge to change its temperature. Please see the [full 2.0 release notes here](doc/static/ReleaseNotes/ReleaseNotes_2.0.md) to view details on all the changes and new features.


## Requirements

* OS: Mac OS X 10.9+, Ubuntu 14.04+
* Graphics Card: DX9 (shader model 3.0) or DX11 with feature level 9.3 capabilities.
* CPU: SSE2 instruction set support.
* Python 2.7 or Python 3.5+
* Linux: X server with GLX module enabled

## Documentation

Please refer to the [Documentation Page on the AI2-THOR website](hhttps://ai2thor.allenai.org/ithor/documentation/) for information on Installation, API, Metadata, actions, object properties and other important framework information.
## Unity Development

If you wish to make changes to the Unity scenes/assets you will need to install Unity Editor version 2019.4.2 LTS for OSX (Linux Editor is currently in Beta) from [Unity Download Archive](https://unity3d.com/get-unity/download/archive).  After making your desired changes using the Unity Editor you will need to build.  To do this you must first exit the editor, then run the following commands from the ai2thor base directory. Individual scenes (the 3D models) can be found beneath the unity/Assets/Scenes directory - scenes are named FloorPlan###.

```python
pip install invoke
invoke local-build
```

This will create a build beneath the directory 'unity/builds/thor-local-OSXIntel64.app'. To use this build in your code, make the following change:

```python
controller = ai2thor.controller.Controller(
    local_executable_path="<BASE_DIR>/unity/builds/thor-OSXIntel64-local/thor-OSXIntel64-local.app/Contents/MacOS/AI2-Thor"
)
```

## Browser Build

For building for browser, please refer to [this page](WEBGL.md).

## Citation
If you use iTHOR, please cite the original AI2-THOR paper:

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
  
If you use RoboTHOR, please cite the following paper:

    @inproceedings{robothor,
        Author = {Matt Deitke and Winson Han and Alvaro Herrasti and
                  Aniruddha Kembhavi and Eric Kolve and Roozbeh Mottaghi and
                  Jordi Salvador and Dustin Schwenk and Eli VanderBilt and
                  Matthew Wallingford and Luca Weihs and Mark Yatskar and
                  Ali Farhadi},
        Title = {{RoboTHOR: An Open Simulation-to-Real Embodied AI Platform}},
        Booktitle = {CVPR},
        Year = {2020}
    }


## Support

We have done our best to fix all bugs and issues. However, you might still encounter some bugs during navigation and interaction. We will be glad to fix the bugs. Please open issues for these and include the scene name as well as the event.metadata from the moment that the bug can be identified.


## Team

AI2-THOR is an open-source project backed by [the Allen Institute for Artificial Intelligence (AI2)](http://www.allenai.org).
AI2 is a non-profit institute with the mission to contribute to humanity through high-impact AI research and engineering.

