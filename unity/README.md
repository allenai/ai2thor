# Unity Development

If you wish to make changes to the Unity scenes/assets you will need to install Unity Editor version 2020.3.25f1 LTS for OSX (Linux Editor is currently in Beta) from [Unity Download Archive](https://unity3d.com/get-unity/download/archive).  After making your desired changes using the Unity Editor you will need to build.  To do this you must first exit the editor, then run the following commands from the ai2thor base directory. Individual scenes (the 3D models) can be found beneath the unity/Assets/Scenes directory - scenes are named FloorPlan###.

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

# Install PIP based on a SHA

The continuous integration system generates a pip on each push to the repo.  These can be installed by using the ai2thor pypi index url.  For example, to install the pip associated with the commit `d26bb0ef75d95074c39718cf9f1a0890ac2c974f` the following can be run from the shell

```bash
pip install --extra-index-url https://ai2thor-pypi.allenai.org ai2thor==0+d26bb0ef75d95074c39718cf9f1a0890ac2c974f
```

To install the latest production release (and also uninstall the pip based on the specific commit), run the following:
```bash
pip install ai2thor --upgrade
```

It is also possible to install a commit based pip by adding the following to a `requirements.txt`:
```text
--extra-index-url https://ai2thor-pypi.allenai.org

ai2thor==0+d26bb0ef75d95074c39718cf9f1a0890ac2c974f
```

This will force the installation of the pip associated with the commit `d26bb0ef75d95074c39718cf9f1a0890ac2c974f` when running `pip install -r requirements.txt`.


## Add Third-Party Plugin

AI2-THOR uses [Assembly definitions](https://docs.unity3d.com/Manual/ScriptCompilationAssemblyDefinitionFiles.html) to declare dependencies between libraries within the project.  To add a third-party package to the project:
1. unpack the package to a newly created directory beneath the `unity/Assets/` directory.
2. Within the Unity Editor, find the new folder in the Project tab.
3. Select the folder and right-click in the window that displays the contents of the folder and select Create -> Assembly Definition.  This will create a new file in the folder - rename the Assembly definition to match the name of the plugin you are adding (e.g. 'Priority Queue' or 'iTween'). This allows the Assembly definition to be easily found during creation of the reference.  One thing to be aware of is that if you don't rename the newly created file immediately the name in the Inspector within the Editor will not match the filename and you will have to manually update the definition name after renaming the file.

To reference the plugin, a reference must be created in the Assembly Definition file located at `unity/Assets/Scripts/AI2-THOR-Base`. Locate the `AI2-THOR-Base` Assembly Definition file under `unity/Assets/Scripts` within the Project tab.  Click on the file and locate the section titled "Assembly Definition References".  Click the `+` sign to add a new entry, then click on the circle to the right of the newly created entry.  That will bring up a menu with all the Assembly definitions in the project.  Select the name of the new plugin/Assembly Definition that was just created.  Scroll to the bottom and click the "Apply" button.  The plugin should now be available to use.  The AI2-THOR-Base Assembly definition and the new plugin folder will need to be commited.

Additional information about Assembly definitions can be found at [Unity's Working with Assembly Definitions Tutorial](https://learn.unity.com/tutorial/working-with-assembly-definitions?uv=2019.4) and at [Unity's Assembly Definitions Documentation Page](https://docs.unity3d.com/Manual/ScriptCompilationAssemblyDefinitionFiles.html).

