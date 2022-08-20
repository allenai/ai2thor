/************************************************************************************
Filename    :   ONSPPropagationSerializationManager.cs
Content     :   Functionality for serializing Oculus Audio geometry
Copyright   :   Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Licensed under the Oculus SDK Version 3.5 (the "License"); 
you may not use the Oculus SDK except in compliance with the License, 
which is provided at the time of installation or download, or which 
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

https://developer.oculus.com/licenses/sdk-3.5/

Unless required by applicable law or agreed to in writing, the Oculus SDK 
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
************************************************************************************/
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public enum PlayModeState
{
    Stopped,
    Playing,
    Paused
}

class ONSPPropagationSerializationManager
{
    static ONSPPropagationSerializationManager()
    {
        EditorSceneManager.sceneSaving += OnSceneSaving;
    }
    public int callbackOrder { get { return 0; } }
    public void OnPreprocessBuild(BuildTarget target, string path)
    {
        Debug.Log("ONSPPropagationSerializationManager.OnPreprocessBuild for target " + target + " at path " + path);
    }

    [MenuItem("Oculus/Spatializer/Build audio geometry for current scene")]
    public static void BuildAudioGeometryForCurrentScene()
    {
        BuildAudioGeometryForScene(EditorSceneManager.GetActiveScene());
    }

    [MenuItem("Oculus/Spatializer/Rebuild audio geometry all scenes")]
    public static void RebuildAudioGeometryForAllScenes()
    {
        Debug.Log("Rebuilding geometry for all scenes");

        System.IO.Directory.Delete(ONSPPropagationGeometry.GeometryAssetPath, true);

        for (int i = 0; i < EditorSceneManager.sceneCount; ++i)
        {
            BuildAudioGeometryForScene(EditorSceneManager.GetSceneAt(i));
        }
    }

    public static void OnSceneSaving(Scene scene, string path)
    {
        BuildAudioGeometryForScene(scene);
    }

    private static void BuildAudioGeometryForScene(Scene scene)
    {
        Debug.Log("Building audio geometry for scene " + scene.name);

        List<GameObject> rootObjects = new List<GameObject>();
        scene.GetRootGameObjects(rootObjects);

        HashSet<string> fileNames = new HashSet<string>();
        foreach (GameObject go in rootObjects)
        {
            var geometryComponents = go.GetComponentsInChildren<ONSPPropagationGeometry>();
            foreach (ONSPPropagationGeometry geo in geometryComponents)
            {
                if (geo.fileEnabled)
                {
                    if (!geo.WriteFile())
                    {
                        Debug.LogError("Failed writing geometry for " + geo.gameObject.name);
                    }
                    else
                    {
                        if (!fileNames.Add(geo.filePathRelative))
                        {
                            Debug.LogWarning("Duplicate file name detected: " + geo.filePathRelative);
                        }
                    }
                }
            }
        }

        Debug.Log("Successfully built " + fileNames.Count + " geometry objects");
    }
}
