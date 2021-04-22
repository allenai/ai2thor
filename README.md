<p align="center"><img width="50%" src="doc/static/logo.svg" /></p>
<p align="center">
    <a href="//github.com/allenai/ai2thor/releases">
        <img alt="GitHub release" src="https://img.shields.io/github/release/allenai/ai2thor.svg">
    </a>
    <a href="//github.com/allenai/ai2thor/blob/main/LICENSE">
        <img alt="License" src="https://img.shields.io/github/license/allenai/ai2thor.svg?color=blue">
    </a>
    <a href="//ai2thor.allenai.org/" target="_blank">
        <img alt="Documentation" src="https://img.shields.io/website/https/ai2thor.allenai.org?down_color=red&down_message=offline&up_message=online">
    </a>
    <a href="//arxiv.org/abs/1712.05474" target="_blank">
        <img src="https://img.shields.io/badge/arXiv-1712.05474-<COLOR>">
    </a>
    <a href="//www.youtube.com/watch?v=KcELPpdN770" target="_blank">
        <img src="https://img.shields.io/badge/video-YouTube-red">
    </a>
    <a href="//travis-ci.org/allenai/ai2thor" target="_blank">
        <img src="https://travis-ci.org/allenai/ai2thor.svg?branch=main">
    </a>
</p>

[AI2-THOR (The House Of inteRactions)](https://ai2thor.allenai.org/) is a near photo-realistic interactable framework for AI agents.

## News

* (4/2021) We are excited to release [ManipulaTHOR](https://ai2thor.allenai.org/manipulathor/), an environment within the AI2-THOR framework that facilitates visual manipulation of objects using a robotic arm. Please see the [full 3.0.0 release notes here](doc/static/ReleaseNotes/ReleaseNotes_3.0.md)

* (2/2021) We are excited to host the [AI2-THOR Rearrangement Challenge](https://ai2thor.allenai.org/rearrangement/), [RoboTHOR ObjectNav Challenge](https://ai2thor.allenai.org/robothor/cvpr-2021-challenge/), and [ALFRED Challenge](https://askforalfred.com/EAI21/), held in conjunction with the [Embodied AI Workshop](https://embodied-ai.org/) at CVPR 2021.

* (2/2021) AI2-THOR v2.7.0 announces several massive speedups to AI2-THOR! Read more about it [here](https://medium.com/ai2-blog/speed-up-your-training-with-ai2-thor-2-7-0-12a650b6ab5e).

* (6/2020) We provide a mini-framework to simplify running AI2-THOR in Docker. It can be accssed at: [github.com/allenai/ai2thor-docker](https://github.com/allenai/ai2thor-docker).

* (4/2020) Version 2.4.0 update of the framework is here. All sim objects that aren't explicitly part of the environmental structure are now moveable with physics interactions. New object types have been added, and many new actions have been added. Please see the [full 2.4.0 release notes here](doc/static/ReleaseNotes/ReleaseNotes_2.4.md)

* (2/2020) AI2-THOR now includes two frameworks: [iTHOR](https://ai2thor.allenai.org/ithor/) and [RoboTHOR](https://ai2thor.allenai.org/robothor/). iTHOR includes interactive objects and scenes and RoboTHOR consists of simulated scenes and their corresponding real world counterparts.  

* (9/2019) Version 2.1.0 update of the framework has been added. New object types have been added. New Initialization actions have been added. Segmentation image generation has been improved in all scenes. 

* (6/2019) Version 2.0 update of the AI2-THOR framework is now live! We have over quadrupled our action and object states, adding new actions that allow visually distinct state changes such as broken screens on electronics, shattered windows, breakable dishware, liquid fillable containers, cleanable dishware, messy and made beds and more! Along with these new state changes, objects have more physical properties like Temperature, Mass, and Salient Materials that are all reported back in object metadata. To combine all of these new properties and actions, new context sensitive interactions can now automatically change object states. This includes interactions like placing a dirty bowl under running sink water to clean it, placing a mug in a coffee machine to automatically fill it with coffee, putting out a lit candle by placing it in water, or placing an object over an active stove burner or in the fridge to change its temperature. Please see the [full 2.0 release notes here](doc/static/ReleaseNotes/ReleaseNotes_2.0.md) to view details on all the changes and new features.


## Requirements

* OS: Mac OS X 10.9+, Ubuntu 14.04+
* Graphics Card: DX9 (shader model 3.0) or DX11 with feature level 9.3 capabilities.
* CPU: SSE2 instruction set support.
* Python 3.5+
* Linux: X server with GLX module enabled

## Documentation

Please refer to the [Documentation Page on the AI2-THOR website](hhttps://ai2thor.allenai.org/ithor/documentation/) for information on Installation, API, Metadata, actions, object properties and other important framework information.
## Unity Development

If you wish to make changes to the Unity scenes/assets you will need to install Unity Editor version 2019.4.20 LTS for OSX (Linux Editor is currently in Beta) from [Unity Download Archive](https://unity3d.com/get-unity/download/archive).  After making your desired changes using the Unity Editor you will need to build.  To do this you must first exit the editor, then run the following commands from the ai2thor base directory. Individual scenes (the 3D models) can be found beneath the unity/Assets/Scenes directory - scenes are named FloorPlan###.

```python
pip install invoke
invoke local-build
```

This will create a build beneath the directory 'unity/builds/thor-local-OSXIntel64.app'. To use this build in your code, make the following change:

```python
controller = ai2thor.controller.Controller(
    local_executable_path="<BASE_DIR>/unity/builds/thor-OSXIntel64-local/thor-OSXIntel64-local.app/Contents/MacOS/AI2-THOR"
)
```

## Add Third-Party Plugin

AI2-THOR uses [Assembly definitions](https://docs.unity3d.com/Manual/ScriptCompilationAssemblyDefinitionFiles.html) to declare dependencies between libraries within the project.  To add a third-party package to the project, unpack the package to a newly created directory beneath the `unity/Assets/` directory.  Within the Unity Editor, find the new folder in the Project tab.  Select the folder and right-click in the window that displays the contents of the folder and select Create -> Assembly Definition.  This will create a new file in the folder - rename the Assembly definition to match the name of the plugin you are adding (e.g. 'Priority Queue' or 'iTween'). This allows the Assembly definition to be easily found during creation of the reference.  One thing to be aware of is that if you don't rename the newly created file immediately the name in the Inspector within the Editor will not match the filename and you will have to manually update the definition name after renaming the file.  To reference the plugin, a reference must be created in the Assembly Definition file located at `unity/Assets/Scripts/AI2-THOR-Base`.  Locate the `AI2-THOR-Base` Assembly Definition file under `unity/Assets/Scripts` within the Project tab.  Click on the file and locate the section titled "Assembly Definition References".  Click the `+` sign to add a new entry, then click on the circle to the right of the newly created entry.  That will bring up a menu with all the Assembly definitions in the project.  Select the name of the new plugin/Assembly Definition that was just created.  Scroll to the bottom and click the "Apply" button.  The plugin should now be available to use.  The AI2-THOR-Base Assembly definition and the new plugin folder will need to be commited. Additional information about Assembly definitions can be found at:
* https://learn.unity.com/tutorial/working-with-assembly-definitions?uv=2019.4#
* https://docs.unity3d.com/Manual/ScriptCompilationAssemblyDefinitionFiles.html

## Browser Build

For building for browser, please refer to [this page](WEBGL.md).

## Citation
If you use iTHOR, please cite the original AI2-THOR paper:

```bibtex
@article{ai2thor,
  author={Eric Kolve and Roozbeh Mottaghi and Winson Han and
          Eli VanderBilt and Luca Weihs and Alvaro Herrasti and
          Daniel Gordon and Yuke Zhu and Abhinav Gupta and
          Ali Farhadi},
  title={{AI2-THOR: An Interactive 3D Environment for Visual AI}},
  journal={arXiv},
  year={2017}
}
```

If you use ManipulaTHOR, please cite the following paper:

```bibtex
@inproceedings{manipulathor,
  title={{ManipulaTHOR: A Framework for Visual Object Manipulation}},
  author={Kiana Ehsani and Winson Han and Alvaro Herrasti and
          Eli VanderBilt and Luca Weihs and Eric Kolve and
          Aniruddha Kembhavi and Roozbeh Mottaghi},
  booktitle={CVPR},
  year={2021}
}
```
  
If you use RoboTHOR, please cite the following paper:

```bibtex
@inproceedings{robothor,
  author={Matt Deitke and Winson Han and Alvaro Herrasti and
          Aniruddha Kembhavi and Eric Kolve and Roozbeh Mottaghi and
          Jordi Salvador and Dustin Schwenk and Eli VanderBilt and
          Matthew Wallingford and Luca Weihs and Mark Yatskar and
          Ali Farhadi},
  title={{RoboTHOR: An Open Simulation-to-Real Embodied AI Platform}},
  booktitle={CVPR},
  year={2020}
}
```


## Support

We have done our best to fix all bugs and issues. However, you might still encounter some bugs during navigation and interaction. We will be glad to fix the bugs. Please open issues for these and include the scene name as well as the event.metadata from the moment that the bug can be identified.


## Team

AI2-THOR is an open-source project backed by [the Allen Institute for Artificial Intelligence (AI2)](http://www.allenai.org).
AI2 is a non-profit institute with the mission to contribute to humanity through high-impact AI research and engineering.
