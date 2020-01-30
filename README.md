# Machine Common Sense Fork of AI2-THOR

Original documentation:  https://github.com/allenai/ai2thor

## Setup

The AI2-THOR documetation wants us to use Unity Editor version `2018.3.6` but that version was not available for the Linux Unity Editor.  However, version [`2018.3.0f2`](https://forum.unity.com/threads/unity-on-linux-release-notes-and-known-issues.350256/page-2#post-4009651) does work fine.  Do NOT use any version that starts with `2019` because its build files are not compatible with AI2-THOR.

## Run

If you want to run an MCS Scene in the Unity Editor:

- Copy a config file from the [AI2-THOR scenes folder in our MCS GitHub repository](https://github.com/NextCenturyCorporation/MCS/tree/master/ai2thor_wrapper/scenes) into the `unity/Assets/Resources/MCS/Scenes/` folder.
- In the "MCS" Game Object, enter the name of your scene file in the "Default Scene File" property.
- If you want to see the class/depth/object masks, enable "Image Synthesis" in the "FirstPersonCharacter" (camera) within the "FPSController" Game Object.
- While running, use WASD to move, arrow buttons to look, and escape to pass.

## Build

Open the Unity Editor and build the project.

Alternatively, if you want to build the Unity project via the command line, run the command below, replacing the path to your Unity executable file, log file name, `<cloned_repository>`, and the `executeMethod` as needed.  Please note that this command will build ALL the AI2-THOR scenes which will take a very long time (my only solution was to delete all the AI2-THOR scenes with `rm <cloned_repository>/unity/Assets/Scenes/FloorPlan*`).

```
./Unity-2018.3.0f2/Editor/Unity -quit -batchmode -logfile MCS-Unity-Build.log -projectpath <cloned_repository>/unity/ -executeMethod Build.Linux64
```

## Important Files and Folders

- [`unity/`](./unity)  The MCS Unity project.  Add this folder as a project in your Unity Hub.
- `unity/Assets/Scenes/MCS.unity`  The MCS Unity Scene.  You can load and edit this in the Unity Editor.
- [`unity/Assets/Scripts/MachineCommonSenseMain.cs`](./unity/Assets/Scripts/MachineCommonSenseMain.cs)  The main MCS Unity script that is imported into and runs within the Scene.
- [`unity/Assets/Scripts/MachineCommonSensePerformerAgent.cs`](./unity/Assets/Scripts/MachineCommonSensePerformerAgent.cs)  A custom subclass extending AI2-THOR's [AgentManager](./unity/Assets/Scripts/AgentManager.cs) that handles all the communication between the Python API and the Unity Scene.
- [`unity/Assets/Resources/MCS/`](./unity/Assets/Resources/MCS)  Folder containing all MCS runtime resources.
- [`unity/Assets/Resources/MCS/object_registry.json`](./unity/Assets/Resources/MCS/object_registry.json)  Config file containing the MCS Scene's Game Objects that may be loaded at runtime. 
- [`unity/Assets/Resources/MCS/Materials/`](./unity/Assets/Resources/MCS/Materials)  Copy of AI2-THOR's [`unity/Assets/QuickMaterials/`](./unity/Assets/QuickMaterials).  Must be in the `Resources` folder to access at runtime.
- [`unity/Assets/Resources/MCS/Scenes/`](./unity/Assets/Resources/MCS/Scenes)  Folder containing sample scene config files (see [Run](#run)).

## Differences from AI2-THOR Scenes

- The "FPSController" object is mostly the same, but I made it smaller to simulate a baby.  This also allowed me to downscale the room which not only improves performance but also was necessary to get the depth masks to work (while standing at one end of the room, you still want to see the far wall in the depth masks).  Changes affected the "Transform", "Character Controller", and "Capsule Collider" in the "FPSController" and the "Transform" in the "FirstPersonCharacter" (camera) nested inside the "FPSController".
- In the "PhysicsSceneManager" object, I replaced the "AgentManager" script with our "MachineCommonSensePerformerManager" script.
- Added structural objects (walls, floor, ceiling).
- Added the invisible "MCS" object containing our "MachineCommonSenseMain" script that runs in the background.

## Lessons Learned

- Adding AI2-THOR's custom Tags and Layers to your Game Objects is needed for their scripts to work properly.  For example, if you don't tag the walls as "Structure", then the player can walk fully into them.
- Fast moving objects that use Unity physics, as well as all structural objects, should have their "Collision Detection" (in their "Rigidbody") set to "Continuous".  With these changes, a fast moving object that tries to move from one side of a wall to the other side in a single frame will be stopped as expected.

## Changelog of AI2-THOR Classes

- Scripts/AgentManager:
  - Added the `logs` and `sceneConfig` properties to `ServerAction`
  - Added `virtual` to `ResetCoroutine` and `setReadyToEmit`
- Scripts/BaseFPSAgentController:
  - Removed the hard-coded camera properties in `SetAgentMode`
  - Replaced the call to `checkInitializeAgentLocationAction` in `Initialize` with calls to `snapToGrid` and `actionFinished` so re-initialization doesn't cause the player to move for a few steps
- Scripts/DebugDiscreteAgentController:
  - Added a way to "Pass" (with the "Escape" button) or "Initialize" (with the "Backspace" button) on a step while playing the game in the Unity Editor
- Shaders/DepthBW:
  - Changed the divisor to increase the effective depth of field for the depth masks.

