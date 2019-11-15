# WebGL Builds

Using Unity's support for WebGL builds AI2-THOR offers an easy way to create web interfaces that communicate with the game and viceversa.

This can be useful for getting data from human interactions in AI2-THOR and for large scale crowdsourcing.

## Requirements

* Tested on OS: Mac OS X 10.9+ and Windows 10
* WebGL Build Bupport Module in your Unity Installation (Re-run the Unity installer to add this module or select a Unity installation through Unity Hub to add Modules to it)

## Build

To create a WebGL build, close Unity and simply run the invoke task:

```
inv webgl-build -s=FloorPlan8_physics
```

The build should be written under `unity/builds/thor-local-WebGL`.

You can read the log file `thor-local-WebGL.log` under the root directory.

If it fails make sure Unity is closed, in Windows the process may be hanging so make sure Unity is not running. In Windows after opeing and closing Unity you may also need to restart you machine.

You can customize some of the build details with other invoke task parameters. Run `inv webgl-build --help`

## WebGL Templates

We include a demo template used for a crowdsourcing task, which is a great start if you want to build your own AI2-THOR interface. 

### Runing the demo Hide N Seek template

In Unity open the AI2-THOR project and select the WebGL Template called [HideNSeek](unity/Assets/WebGLTemplates/HideNSeek).

[How to do this.](https://docs.unity3d.com/Manual/webgl-templates.html)

From the build menu in Unity switch the target platform to WebGL/HTML5 and build. Unity launches a small server with the site. Make sure the address has the following get parameters:

`http://localhost:<change_to_your_build_port>?object=Tomato&variation=0`

The `object` parameter can be `Tomato`, `Bread`, `Plunger`, `Cup`, or `Knife` with the same casing, and leave `variation` to be 0.

* After the template is selected, you can close Unity and call the `webgl-build` command to make builds of different scenes that use the template.
* You can create your own template under `unity/Assets/WebGLTemplates`.

## Custom Interface

### Communication between Javascript and C#

You can call Unity functions from javascript, and javascript functions from C#. [Read more here](https://docs.unity3d.com/Manual/webgl-interactingwithbrowserscripting.html).

For Thor we have a simple callback interface for initialization, events, and event's metadata.

In your javascript code you can register the following callbacks:

```
window.onGameLoaded = function() {
    // When the Unity binary has loaded, you can load new scenes safely here
}
```

```
window.onUnityEvent = function(eventString) {
    // When an Action is called, eventString is a json string with the schema of the class a ServerAction
    let jsonEvent = JSON.parse(eventString);
    // ...
}
```

```
window.onUnityMetadata = function(metadataString) {
    // When an action is finished metadataString is a json string with the scheme of MultiAgentMetadata, just like the return to the step function on python
    let jsonEvent = JSON.parse(metadataString);
    // ...
}
```

See [AgentManager.cs](unity/Assets/Scripts/AgentManager.cs) for the json schemas.

You can look at [script.js](unity/Assets/WebGLTemplates/HideNSeek/TemplateData/script.js) to see an example of a metadata callback registered with different handlers.

### Controller step

We provide a step function you can call through javascript, that takes a stringified `ServerAction` just like the python controller:

```
gameInstance.SendMessage ('FPSController', 'Step', JSON.stringify({
          action: "MoveAhead",
          moveMagnitude:  0.25
        }));
```

Call on your `gameInstance` in javascript.


### Custom Controller

The easiest way to do this is to write a new Unity component `MonoBehaviour` that acts as a controller for the agent and handles user input through Unity's functions. See [DiscretePointClickAgentController.cs](unity/Assets/Scripts/DiscretePointClickAgentController.cs) as an example. This has the convenience of developing and testing this input system within Unity. 
When you're done register your component in the script [PlayerControllers.cs](https://github.com/allenai/ai2thor/blob/master/unity/Assets/Scripts/PlayerControllers.cs), adding it both to the `ControlMode` Enum and the `PlayerControllers` class dictionary; with the Enum value as key and your Script type as the value.

#### Activating Controller

In javascript call on your game instance after the scene has loaded:

```
  gameInstance.SendMessage('FPSController', 'SetController', 'DISCRETE_HIDE_N_SEEK');
```

The third argument in this case `"DISCRETE_HIDE_N_SEEK"`, should be the enum string name in the `ControlMode` enum, you mapped your controller to in [PlayerControllers.cs](https://github.com/allenai/ai2thor/blob/master/unity/Assets/Scripts/PlayerControllers.cs).

#### Alternate Controller

You can also handle user input in javascript and call `Step` on the controller to do certain actions. This is a more limited option and has some disadvantages in speed and customization but has the advantage of not having to rebuild when changing the controller.

### Deploying your Site to S3

An easy way to have access to your interface is to host it on AWS S3 service, we provide an invoke task that does this for you:

```
inv webgl-s3-deploy -b=<s3_bucket> -t=<target_dir_in_s3> -s=8 -v
```

The `-s` or `--scene` parameter is a comma separated list of numbers representing the scenes to build, `-v` is just to have verbose output.

This task, builds and deploys your interface to s3 (make sure you have your WebGL template selected as the one to use in Unity).
