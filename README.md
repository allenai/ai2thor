<p align="center"><img width="50%" src="https://raw.githubusercontent.com/allenai/ai2thor/main/doc/static/logo.svg" /></p>
<h3 align="center"><i>A Near Photo-Realistic Interactable Framework for Embodied AI Agents</i></h3>

<p align="center">
    <a href="//travis-ci.org/allenai/ai2thor" target="_blank">
        <img src="https://travis-ci.org/allenai/ai2thor.svg?branch=main">
    </a>
    <a href="//github.com/allenai/ai2thor/releases">
        <img alt="GitHub release" src="https://img.shields.io/github/release/allenai/ai2thor.svg">
    </a>
    <a href="//ai2thor.allenai.org/" target="_blank">
        <img alt="Documentation" src="https://img.shields.io/website/https/ai2thor.allenai.org?down_color=red&down_message=offline&up_message=online">
    </a>
    <a href="//github.com/allenai/ai2thor/blob/main/LICENSE">
        <img alt="License" src="https://img.shields.io/github/license/allenai/ai2thor.svg?color=blue">
    </a>
    <a href="//arxiv.org/abs/1712.05474" target="_blank">
        <img src="https://img.shields.io/badge/arXiv-1712.05474-<COLOR>">
    </a>
    <a href="//www.youtube.com/watch?v=KcELPpdN770" target="_blank">
        <img src="https://img.shields.io/badge/video-YouTube-red">
    </a>
    <a href="//pepy.tech/project/ai2thor" target="_blank">
        <img alt="Downloads" src="https://pepy.tech/badge/ai2thor">
    </a>
</p>

## 🏡 Environments

<table>
    <tr>
        <td width="33%">
            <img src="https://user-images.githubusercontent.com/28768645/115940549-d0134a80-a456-11eb-8737-d89ad75a0f14.jpg" width="100%" />
        </td>
        <td width="33%">
            <img src="https://user-images.githubusercontent.com/28768645/115940534-c689e280-a456-11eb-985a-ca28e63715c7.jpg" width="100%" />
        </td>
        <td width="33%">
            <img src="https://user-images.githubusercontent.com/28768645/115940466-8b87af00-a456-11eb-92e5-25a9f28e7f6b.jpg" width="100%" />
        </td>
    </tr>
    <tr>
        <td align="center" width="33%">
            <code>iTHOR</code>
        </td>
        <td align="center" width="33%">
            <code>ManipulaTHOR</code>
        </td>
        <td align="center" width="33%">
            <code>RoboTHOR</code>
        </td>
    </tr>
    <tr>
        <td width="33%">
            A high-level interaction framework that facilitates research in embodied common sense reasoning.
        </td>
        <td width="33%">
            A mid-level interaction framework that facilitates visual manipulation of objects using a robotic arm.
        </td>
        <td width="33%">
            A framework that facilitates Sim2Real research with a collection of simlated scene counterparts in the physical world.
        </td>
    </tr>
</table>

## 🌍 Features

**🏡 Scenes.** 200+ custom built high-quality scenes. The scenes can be explored on our [demo](//ai2thor.allenai.org/demo) page. We are working on rapidly expanding the number of available scenes and domain randomization within each scene.

**🪑 Objects.** 2600+ custom designed household objects across 100+ object types. Each object is heavily annotated, which allows for near-realistic physics interaction.

**🤖 Agent Types.** Multi-agent support, a custom built LoCoBot agent, a Kinova 3 inspired robotic manipulation agent, and a drone agent.

**🦾 Actions.** 200+ actions that facilitate research in a wide range of interaction and navigation based embodied AI tasks.

**🖼 Images.** First-class support for many image modalities and camera adjustments. Some modalities include ego-centric RGB images, instance segmentation, semantic segmentation, depth frames, normals frames, top-down frames, orthographic projections, and third-person camera frames. User's can also easily change camera properties, such as the size of the images and field of view.

**🗺 Metadata.** After each step in the environment, there is a large amount of sensory data available about the state of the environment. This information can be used to build highly complex custom reward functions.

## 📰 Latest Announcements

<table>
    <tr>
        <td align="center" width="80">
            Date
        </td>
        <td align="center" colspan="2">
            Announcement
        </td>
    </tr>
    <tr>
        <td align="center">
            5/2021
        </td>
        <td width="50%">
            <video src="https://user-images.githubusercontent.com/28768645/118589971-58cf8e80-b756-11eb-82ea-a94f84f57353.mp4"/>
        </td>
        <td>
            <code>RandomizeMaterials</code> is now supported! It enables a massive amount of realistic looking domain randomization within each scene. Try it out on the <a href="https://ai2thor.allenai.org/demo">demo</a>
        </td>
    </tr>
    <tr>
        <td align="center">
            4/2021
        </td>
        <td colspan="2">
            We are excited to release <a href="https://ai2thor.allenai.org/manipulathor/">ManipulaTHOR</a>, an environment within the AI2-THOR framework that facilitates visual manipulation of objects using a robotic arm. Please see the <a href="https://github.com/allenai/ai2thor/blob/main/doc/static/ReleaseNotes/ReleaseNotes_3.0.md">full 3.0.0 release notes here</a>.
        </td>
    </tr>
    <tr>
        <td align="center">
            4/2021
        </td>
        <td width="50%">
            <video src="https://user-images.githubusercontent.com/28768645/118590040-7a307a80-b756-11eb-9b4c-c43373bea528.mp4"></video>
            <video src="https://user-images.githubusercontent.com/28768645/118589727-d1821b00-b755-11eb-9dff-9eda97fe0998.mp4"></video>
        </td>
        <td>
            <code>RandomizeLighting</code> is now supported! It includes many tunable parameters to allow for vast control over its effects. Try it out on the <a href="https://ai2thor.allenai.org">demo</a>!<br/><br/>
        </td>
    </tr>
    <tr>
        <td align="center">
            2/2021
        </td>
        <td colspan="2">
            We are excited to host the <a href="https://ai2thor.allenai.org/rearrangement/">AI2-THOR Rearrangement Challenge</a>, <a href="https://ai2thor.allenai.org/robothor/cvpr-2021-challenge/">RoboTHOR ObjectNav Challenge</a>, and <a href="https://askforalfred.com/EAI21/">ALFRED Challenge</a>, held in conjunction with the <a href="https://embodied-ai.org/">Embodied AI Workshop</a> at CVPR 2021.
        </td>
    </tr>
    <tr>
        <td align="center">
            2/2021
        </td>
        <td colspan="2">
            AI2-THOR v2.7.0 announces several massive speedups to AI2-THOR! Read more about it <a href="https://medium.com/ai2-blog/speed-up-your-training-with-ai2-thor-2-7-0-12a650b6ab5e">here</a>.
        </td>
    </tr>
    <tr>
        <td align="center">
            6/2020
        </td>
        <td colspan="2">
            We've released <a href="https://github.com/allenai/ai2thor-docker">🐳 AI2-THOR Docker</a> a mini-framework to simplify running AI2-THOR in Docker.
        </td>
    </tr>
    <tr>
        <td align="center">
            4/2020
        </td>
        <td colspan="2">
            Version 2.4.0 update of the framework is here. All sim objects that aren't explicitly part of the environmental structure are now moveable with physics interactions. New object types have been added, and many new actions have been added. Please see the <a href="https://github.com/allenai/ai2thor/blob/main/doc/static/ReleaseNotes/ReleaseNotes_2.4.md">full 2.4.0 release notes here</a>.
        </td>
    </tr>
    <tr>
        <td align="center">
            2/2020
        </td>
        <td colspan="2">
            AI2-THOR now includes two frameworks: <a href="https://ai2thor.allenai.org/ithor/">iTHOR</a> and <a href="https://ai2thor.allenai.org/robothor/">RoboTHOR</a>. iTHOR includes interactive objects and scenes and RoboTHOR consists of simulated scenes and their corresponding real world counterparts.
        </td>
    </tr>
    <tr>
        <td align="center">
            9/2019
        </td>
        <td colspan="2">
            Version 2.1.0 update of the framework has been added. New object types have been added. New Initialization actions have been added. Segmentation image generation has been improved in all scenes.
        </td>
    </tr>
    <tr>
        <td align="center">
            6/2019
        </td>
        <td colspan="2">
            Version 2.0 update of the AI2-THOR framework is now live! We have over quadrupled our action and object states, adding new actions that allow visually distinct state changes such as broken screens on electronics, shattered windows, breakable dishware, liquid fillable containers, cleanable dishware, messy and made beds and more! Along with these new state changes, objects have more physical properties like Temperature, Mass, and Salient Materials that are all reported back in object metadata. To combine all of these new properties and actions, new context sensitive interactions can now automatically change object states. This includes interactions like placing a dirty bowl under running sink water to clean it, placing a mug in a coffee machine to automatically fill it with coffee, putting out a lit candle by placing it in water, or placing an object over an active stove burner or in the fridge to change its temperature. Please see the <a href="https://github.com/allenai/ai2thor/blob/main/doc/static/ReleaseNotes/ReleaseNotes_2.0.md">full 2.0 release notes here</a> to view details on all the changes and new features.
        </td>
    </tr>
</table>

## 💻 Installation

### With Google Colab

[AI2-THOR Colab](https://github.com/allenai/ai2thor-colab) can be used to run AI2-THOR freely in the cloud with Google Colab. Running AI2-THOR in Google Colab makes it extremely easy to explore functionality without having to set AI2-THOR up locally.

### With pip

```bash
pip install ai2thor
```

### With conda

```bash
conda install -c conda-forge ai2thor
```

### With Docker

[🐳 AI2-THOR Docker](https://github.com/allenai/ai2thor-docker) can be used, which adds the configuration for running a X server to be used by Unity 3D to render scenes.

### Minimal Example

Once you've installed AI2-THOR, you can verify that everything is working correctly by running the following minimal example:

```python
from ai2thor.controller import Controller
controller = Controller(scene="FloorPlan10")
event = controller.step(action="RotateRight")
metadata = event.metadata
print(event, event.metadata.keys())
```

### Requirements

| Component | Requirement |
| :-- | :-- |
| OS | Mac OS X 10.9+, Ubuntu 14.04+ |
| Graphics Card | DX9 (shader model 3.0) or DX11 with feature level 9.3 capabilities. |
| CPU | SSE2 instruction set support. |
| Python | Versions 3.5+ |
| Linux | X server with GLX module enabled |

## 💬 Support

**Questions.** If you have any questions on AI2-THOR, please ask them on our [GitHub Discussions Page](https://github.com/allenai/ai2thor/discussions).

**Issues.** If you encounter any issues while using AI2-THOR, please open an [Issue on GitHub](https://github.com/allenai/ai2thor/issues).

## 🏫 Learn more

| Section | Description |
| :-- | :-- |
| [Demo](https://ai2thor.allenai.org/demo/) | Interact and play with AI2-THOR live in the browser. |
| [iTHOR Documentation](https://ai2thor.allenai.org/ithor/documentation/) | Documentation for the iTHOR environment. |
| [ManipulaTHOR Documentation](https://ai2thor.allenai.org/manipulathor/documentation/) | Documentation for the ManipulaTHOR environment. |
| [RoboTHOR Documentation](https://ai2thor.allenai.org/robothor/documentation/) | Documentation for the RoboTHOR environment. |
| [AI2-THOR Colab](https://github.com/allenai/ai2thor-colab) | A way to run AI2-THOR freely on the cloud using Google Colab. |
| [AllenAct](https://allenact.org/) | An Embodied AI Framework build at AI2 that provides first-class support for AI2-THOR. |
| [AI2-THOR Unity Development](https://github.com/allenai/ai2thor/tree/main/unity#readme) | A (sparse) collection of notes that may be useful if editing on the AI2-THOR backend. |
| [AI2-THOR WebGL Development](https://github.com/allenai/ai2thor/blob/main/WEBGL.md) | Documentation on packaging AI2-THOR for the web, which might be useful for annotation based tasks. |

## 📒 Citation

If you use AI2-THOR or iTHOR scenes, please cite the original AI2-THOR paper:

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

If you use 🏘️ ProcTHOR or procedurally generated scenes, please cite the following paper:

```bibtex
@inproceedings{procthor,
  author={Matt Deitke and Eli VanderBilt and Alvaro Herrasti and
          Luca Weihs and Jordi Salvador and Kiana Ehsani and
          Winson Han and Eric Kolve and Ali Farhadi and
          Aniruddha Kembhavi and Roozbeh Mottaghi},
  title={{ProcTHOR: Large-Scale Embodied AI Using Procedural Generation}},
  booktitle={NeurIPS},
  year={2022},
  note={Outstanding Paper Award}
}
```

If you use ManipulaTHOR agent, please cite the following paper:

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
  
If you use RoboTHOR scenes, please cite the following paper:

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

## 👋 Our Team

AI2-THOR is an open-source project built by the [PRIOR team](//prior.allenai.org) at the [Allen Institute for AI](//allenai.org) (AI2).
AI2 is a non-profit institute with the mission to contribute to humanity through high-impact AI research and engineering.

<br />

<a href="//prior.allenai.org">
<p align="center"><img width="100%" src="https://raw.githubusercontent.com/allenai/ai2thor/main/doc/static/ai2-prior.svg" /></p>
</a>
