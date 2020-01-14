# Machine Common Sense Fork of AI2-THOR

Original documentation:  https://github.com/NextCenturyCorporation/ai2thor

## Setup

The AI2-THOR documetation wants us to use Unity Editor version `2018.3.6` but that version was not available for the Linux Unity Editor.  However, version [`2018.3.0f2`](https://forum.unity.com/threads/unity-on-linux-release-notes-and-known-issues.350256/page-2#post-4009651) does work fine.  Do NOT use version `2019.1.0f2` because its build files are not compatible with AI2-THOR.

## Build

Open the Unity Editor and build the project.

To build using the command line (replace the path to your Unity executable file, log file name, `path_to_ai2thor_unity_folder`, and the `executeMethod` as needed):

```
./Unity-2018.3.0f2/Editor/Unity -quit -batchmode -logfile MCS-Unity-Build.log -projectpath <path_to_ai2thor_unity_folder> -executeMethod Build.Linux64
```

## Content

- [`unity/`](./unity)  The MCS Unity project.  Add this folder as a project in your Unity Hub.
- `unity/Assets/Scenes/MCS.unity`  The MCS Unity Scene.  You can load and edit this in the Unity Editor.
- [`unity/Assets/Scripts/MachineCommonSenseMain.cs`](./unity/Assets/Scripts/MachineCommonSenseMain.cs)  The main MCS Unity script that is imported into and runs within the Scene.
- [`unity/Assets/Scripts/MachineCommonSensePerformerAgent.cs`](./unity/Assets/Scripts/MachineCommonSensePerformerAgent.cs)  A custom subclass extending AI2-THOR's [AgentManager](./unity/Assets/Scripts/AgentManager.cs) that handles all the communication between the Python API and the Unity Scene.
- [`unity/Assets/Resources/MCS/`](./unity/Assets/Resources/MCS)  Folder containing all MCS runtime resources.
- [`unity/Assets/Resources/MCS/config.json`](./unity/Assets/Resources/MCS/config.json)  Main MCS config file.  Choose 
- [`unity/Assets/Resources/MCS/Games/`](./unity/Assets/Resources/MCS/Games)  MCS "game" config files (individual evaluation test configurations).
- [`unity/Assets/Resources/MCS/Materials/`](./unity/Assets/Resources/MCS/Materials)  Copy of AI2-THOR's [`unity/Assets/QuickMaterials/`](./unity/Assets/QuickMaterials).  Must be in the `Resources` folder to access at runtime.

## Changelog

- Scripts/AgentManager:
  - Added `virtual` to `setReadyToEmit`
- Scripts/BaseFPSAgentController:
  - Removed the hard-coded camera properties in `SetAgentMode`
- Shaders/DepthBW:
  - Changed the divisor to increase the final depth of field.
  
