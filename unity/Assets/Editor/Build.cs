using UnityEditor;
using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Rendering;
using UnityEngine.Rendering;
using UnityEditor.Build.Reporting;

public class Build {

    // Since CloudRendering uses a different version of Unity (2020.2) vs production (2019.4), GraphicsSettings and ProjectSettings
    // must be copied over from the Standalone platform.  As well, continuing to use this ensures that settings made for 
    // the Standalone platform get used for CloudRendering
    static void InitializeCloudRendering() {
        PlayerSettings.SetApiCompatibilityLevel(BuildTargetGroup.CloudRendering, PlayerSettings.GetApiCompatibilityLevel(BuildTargetGroup.Standalone));
        var graphicsTiers = new List<GraphicsTier>(){GraphicsTier.Tier1, GraphicsTier.Tier2, GraphicsTier.Tier3};
        foreach (var graphicsTier in graphicsTiers) {
            EditorGraphicsSettings.SetTierSettings(
                BuildTargetGroup.CloudRendering,
                graphicsTier,
                EditorGraphicsSettings.GetTierSettings(BuildTargetGroup.Standalone, graphicsTier)
            );
        }
    }

    static void OSXIntel64() {
        build(GetBuildName(),  BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX);
    }

    static string GetBuildName() {
        return Environment.GetEnvironmentVariable("UNITY_BUILD_NAME");
    }

    static void Linux64() {
        build(GetBuildName(), BuildTargetGroup.Standalone, BuildTarget.StandaloneLinux64);
    }

    static void CloudRendering() {
        InitializeCloudRendering();
        build(GetBuildName(), BuildTargetGroup.CloudRendering, BuildTarget.CloudRendering);
    }

    static void WebGL() {
        // USIM_USE_... scripting defines are required
        // to disable the native PNG and JPEG encoders present in the simulation capture package
        // if these defines are not provide the WebGL build will fail
        build(GetBuildName(), BuildTargetGroup.WebGL, BuildTarget.WebGL, new string[]{"USIM_USE_BUILTIN_JPG_ENCODER", "USIM_USE_BUILTIN_PNG_ENCODER"});
    }

    static void buildResourceAssetJson() {
        ResourceAssetManager manager = new ResourceAssetManager();
        manager.BuildCatalog();
    }

    static void build(string buildName, BuildTargetGroup targetGroup, BuildTarget target, string[] extraScriptingDefines=null) {
        buildResourceAssetJson();

        var defines = GetDefineSymbolsFromEnv();
        if (defines != "") {
            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, GetDefineSymbolsFromEnv());
        }
        List<string> scenes = GetScenes();
        foreach (string scene in scenes) {
            Debug.Log("Adding Scene " + scene);
        }

        // zip + compresslevel=1 && LZ4 is faster by about 30 seconds 
        // (and results in smaller .zip files) than
        // zip + compresslevel=6 (default) && uncomprsesed asset bundles 
        BuildOptions options = BuildOptions.StrictMode | BuildOptions.CompressWithLz4;

        if (ScriptsOnly()) {
            options |= BuildOptions.Development | BuildOptions.BuildScriptsOnly;
        }
        Debug.Log("Build options " + options);
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        if (extraScriptingDefines != null) {
            buildPlayerOptions.extraScriptingDefines = extraScriptingDefines;
        }
        buildPlayerOptions.scenes = scenes.ToArray();
        buildPlayerOptions.locationPathName = buildName;
        buildPlayerOptions.target = target;
        buildPlayerOptions.options = options;
        EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup, target);
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

    }

    private static List<string> GetScenes() {
        List<string> envScenes = GetScenesFromEnv();
        if (envScenes.Count > 0) {
            return envScenes;
        } else {
            return GetAllScenePaths();
        }
    }

    private static List<string> GetAllScenePaths() {
        List<string> files = new List<string>();
        List<string> scenes = new List<string>();
        files.AddRange(Directory.GetFiles("Assets/Scenes/"));

        if (IncludePrivateScenes()) {
            files.AddRange(Directory.GetFiles("Assets/Private/Scenes/"));
        }

        files.AddRange(Directory.GetFiles("Assets/Scenes/Procedural"));
        files.AddRange(Directory.GetFiles("Assets/Scenes/Procedural/ArchitecTHOR"));

        foreach (string f in files) {
            // ignore entryway scenes in build since these are not yet complete
            if (f.Contains("FloorPlan5") && !f.EndsWith("FloorPlan5_physics.unity")) {
                continue;
            }

            if (f.EndsWith(".unity")) {
                scenes.Add(f);
            }
        }

        // uncomment for faster builds for testing
        return scenes; //.Where(x => x.Contains("FloorPlan") || x.Contains("Procedural.unity")).ToList(); //.Where(x => x.Contains("FloorPlan1_") || x.Contains("Procedural")).ToList();
    }

    private static List<string> GetScenesFromEnv() {
        if (Environment.GetEnvironmentVariables().Contains(("BUILD_SCENES"))) {
            return Environment.GetEnvironmentVariable("BUILD_SCENES").Split(',').Select(
                x => "Assets/Scenes/" + x + ".unity"
            ).ToList();
        } else {
            return new List<string>();
        }
    }

    private static bool GetBoolEnvVariable(string key, bool defaultValue = false) {
        string value = Environment.GetEnvironmentVariable(key);
        if (value != null) {
            return value.ToLower() == "true";
        } else {
            return defaultValue;
        }
    }

    private static bool ScriptsOnly() {
        return GetBoolEnvVariable("BUILD_SCRIPTS_ONLY");
    }

    private static bool IncludePrivateScenes() {
        return GetBoolEnvVariable("INCLUDE_PRIVATE_SCENES");
    }

    private static string GetDefineSymbolsFromEnv() {
        return Environment.GetEnvironmentVariable("DEFINES");
    }
}
