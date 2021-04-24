using UnityEditor;
using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Build.Reporting;

public class Build {
    static void OSXIntel64() {
        build(GetBuildName(), GetAllScenePaths(), BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX);
    }

    static string GetBuildName() {
        return Environment.GetEnvironmentVariable("UNITY_BUILD_NAME");
    }

    static void Linux64() {
        build(GetBuildName(), GetAllScenePaths(), BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64);
    }

    static void CloudRendering() {
        build(GetBuildName(), GetAllScenePaths(), BuildTargetGroup.CloudRendering, BuildTarget.CloudRendering);
    }

    static void WebGL() {
        build(GetBuildName(), GetSceneFromEnv().ToList(), BuildTargetGroup.WebGL, BuildTarget.WebGL);
    }

    static void build(string buildName, List<string> scenes, BuildTargetGroup targetGroup, BuildTarget target) {
        Debug.Log("build name: " + buildName);
        var defines = GetDefineSymbolsFromEnv();
        if (defines != "") {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, GetDefineSymbolsFromEnv());
        }
   BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
    buildPlayerOptions.scenes = scenes.ToArray();
    buildPlayerOptions.locationPathName = buildName;
    buildPlayerOptions.target = target;
    buildPlayerOptions.options = BuildOptions.None; //BuildOptions.StrictMode | BuildOptions.UncompressedAssetBundle;
    EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup, target);
    BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
    BuildSummary summary = report.summary;
    }

    private static List<string> GetAllScenePaths() {
        List<string> files = new List<string>();
        List<string> scenes = new List<string>();
        files.AddRange(Directory.GetFiles("Assets/Scenes/"));

        if (IncludePrivateScenes()) {
            files.AddRange(Directory.GetFiles("Assets/Private/Scenes/"));

        }

        foreach (string f in files) {
            if (f.EndsWith("FloorPlan28_physics.unity")) {
                Debug.Log("Adding Scene " + f);
                scenes.Add(f);
            }
        }

        // uncomment for faster builds for testing
        return scenes;//.Where(x => x.Contains("FloorPlan1_")).ToList();
    }

    private static IEnumerable<string> GetSceneFromEnv() {
        return Environment.GetEnvironmentVariable("SCENE").Split(',').Select(
            x => "Assets/Scenes/" + x + ".unity"
        );
    }

    private static bool IncludePrivateScenes() {
        string privateScenes = Environment.GetEnvironmentVariable("INCLUDE_PRIVATE_SCENES");

        return privateScenes != null && privateScenes.ToLower() == "true";
    }

    private static string GetDefineSymbolsFromEnv() {
        return Environment.GetEnvironmentVariable("DEFINES");
    }
}
