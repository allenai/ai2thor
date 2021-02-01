using UnityEditor;
using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Build
{
    static void OSXIntel64() {
#if UNITY_2017_3_OR_NEWER
		var buildTarget  = BuildTarget.StandaloneOSX;
#else
		var buildTarget = BuildTarget.StandaloneOSXIntel64;
#endif
		build(GetBuildName(), GetAllScenePaths(), buildTarget);
    }

    static string GetBuildName() {
		return Environment.GetEnvironmentVariable ("UNITY_BUILD_NAME");
	}

    static void Linux64(){
		build(GetBuildName(), GetAllScenePaths(), BuildTarget.StandaloneLinux64);
    }

    static void WebGL()
    {
        build(GetBuildName(), GetSceneFromEnv().ToList(), BuildTarget.WebGL);
    }

    static void build(string buildName, List<string> scenes, BuildTarget target)	{
        var defines = GetDefineSymbolsFromEnv();
        if (defines != "") {
            var targetGroup = BuildPipeline.GetBuildTargetGroup(target);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, GetDefineSymbolsFromEnv());
        }
        BuildPipeline.BuildPlayer(scenes.ToArray(), buildName, target, BuildOptions.StrictMode | BuildOptions.UncompressedAssetBundle);
    }

    private static List<string> GetAllScenePaths()
    {
        List<string> files = new List<string>();
        List<string> scenes = new List<string>();
        files.AddRange(Directory.GetFiles("Assets/Scenes/"));

        if (IncludePrivateScenes()) {
            files.AddRange(Directory.GetFiles("Assets/Private/Scenes/"));

        }

        foreach (string f in files) {
            if (f.EndsWith(".unity")) {
                Debug.Log ("Adding Scene " + f);
				scenes.Add (f);
            }
        }

        return scenes;
    }

    private static IEnumerable<string> GetSceneFromEnv()
    {
        return Environment.GetEnvironmentVariable("SCENE").Split(',').Select(
            x => "Assets/Scenes/" + x + ".unity"
        );
    }

    private static bool IncludePrivateScenes()
    {
        string privateScenes = Environment.GetEnvironmentVariable("INCLUDE_PRIVATE_SCENES");
        
        return privateScenes != null && privateScenes.ToLower() == "true";
    }

    private static string GetDefineSymbolsFromEnv()
    {
        return Environment.GetEnvironmentVariable("DEFINES");
    }
}
