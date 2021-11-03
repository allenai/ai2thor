using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;

/// <summary>
/// Class for building through Jenkins with Addressables support
/// </summary>
public class JenkinsBuild
{
    static string[] EnabledScenes = FindEnabledEditorScenes();
    static string DEFAULT_BUILD_NAME = "MCS-AI2-THOR";
    static string DEFAULT_BUILD_DIR = "~/Desktop";
    static string DEFAULT_ADDRESSABLES_PROFILE = "Development";

    /// <summary>
    /// 
    /// </summary>
    public static void BuildMacOS()
    {
        StartBuild(BuildTarget.StandaloneOSX, ".app");
    }

    /// <summary>
    /// 
    /// </summary>
    public static void BuildLinux()
    {
        StartBuild(BuildTarget.StandaloneLinux64, ".x86_64");
    }

    /// <summary>
    /// 
    /// </summary>
    public static void BuildWindows()
    {
        StartBuild(BuildTarget.StandaloneWindows, ".exe");
    }

    private static void StartBuild(BuildTarget buildTarget, string extension)
    {
        string[] args = GetExecuteMethodArguments();

        // Do not build if arguments were set incorrectly
        if (args == null)
            return;

        string buildName = args[0];
        string buildPath = args[1];
        string addressesProfile = args[2];

        SwitchPlatform(BuildTargetGroup.Standalone, buildTarget);
        SwitchAddressablesProfile(addressesProfile);
        BuildAddressablesContent();
        string fullPathAndName = buildPath + System.IO.Path.DirectorySeparatorChar + buildName + extension;
        BuildProject(EnabledScenes, fullPathAndName, buildTarget, BuildOptions.None);
    }

    /// <summary>
    /// Example:
    /// -executeMethod JenkinsBuild.BuildLinux
    /// MCS-AI2-THOR
    /// ai2thor/unity/Builds/Linux
    /// dev
    /// </summary>
    /// <returns></returns>
    private static string[] GetExecuteMethodArguments()
    {
        string[] returnedArgs = new string[3];
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-executeMethod")
            {
                if (i + 4 <= args.Length)
                {
                    returnedArgs[0] = args[i + 2];
                    returnedArgs[1] = args[i + 3];
                    returnedArgs[2] = args[i + 4];
                    
                    return returnedArgs;
                }
                else
                {
                    System.Console.WriteLine("[JenkinsBuild] Incorrect Parameters for -executeMethod Format: -executeMethod Build[PLATFORM] <app name> <output dir> [dev/prod]");
                    System.Console.WriteLine("[JenkinsBuild] Parameter 1: " + args[i + 2]);
                    System.Console.WriteLine("[JenkinsBuild] Parameter 2: " + args[i + 3]);
                    System.Console.WriteLine("[JenkinsBuild] Parameter 3: " + args[i + 4]);
                    return null;
                }
            }
        }

        return returnedArgs;
    }

    private static string[] FindEnabledEditorScenes()
    {
        List<string> EditorScenes = new List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                EditorScenes.Add(scene.path);
            }
        }
        return EditorScenes.ToArray();
    }

    private static void SwitchPlatform(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget)
    {
        System.Console.WriteLine("[JenkinsBuild] Switching Platform: " + buildTarget.ToString());

        bool switchResult = EditorUserBuildSettings.SwitchActiveBuildTarget(buildTargetGroup, buildTarget);
        if (switchResult)
        {
            System.Console.WriteLine("[JenkinsBuild] Successfully changed Build Target to: " + buildTarget.ToString());
        }
        else
        {
            System.Console.WriteLine("[JenkinsBuild] Unable to change Build Target to: " + buildTarget.ToString() + " Exiting...");
            return;
        }
    }

    private static void SwitchAddressablesProfile(string addressablesProfileName) {
        string profileName = addressablesProfileName;
        if(profileName.ToLower() == "prod" || profileName.ToLower() == "production" || profileName.ToLower() == "remote") {
            Debug.Log("Switching profile " + profileName + " to Remote");
            profileName = "Remote";
        }
        if(profileName.ToLower() == "dev" || profileName == "development") {
            Debug.Log("Switching profile " + profileName + " to Development");
            profileName = "Development";
        }
        if(profileName == "default") {
            Debug.Log("Switching profile " + profileName + " to Default");
            profileName = "Default";
        }
        if(profileName != "Default" && profileName != "Development" && profileName != "Remote") {
            Debug.Log("Switching profile " + profileName + " to Development because it doesn't exist");
            profileName = DEFAULT_ADDRESSABLES_PROFILE;
        }
        Debug.Log("Loading profile " + profileName + " from the Addressable Asset Settings");
        AddressableAssetSettings addressableAssetSettings = AddressableAssetSettingsDefaultObject.Settings;
        string id = addressableAssetSettings.profileSettings.GetProfileId(addressablesProfileName);
        addressableAssetSettings.activeProfileId = id;
        System.Console.WriteLine("[JenkinsBuild] Setting Addressable Profile To: " + addressableAssetSettings.activeProfileId);
    }

    private static void BuildAddressablesContent()
    {
        System.Console.WriteLine("[JenkinsBuild] Building Addressables");
        // Clean current settings for new build
        AddressableAssetSettings.CleanPlayerContent(
            AddressableAssetSettingsDefaultObject.Settings.ActivePlayerDataBuilder);

        // Log build and load paths
        AddressableAssetSettings addressableAssetSettings = AddressableAssetSettingsDefaultObject.Settings;
        System.Console.WriteLine("[JenkinsBuild] Addressables Build Path: " + 
            addressableAssetSettings.RemoteCatalogBuildPath.GetValue(addressableAssetSettings));
        System.Console.WriteLine("[JenkinsBuild] Addressables Active Data Builder: " +
            AddressableAssetSettingsDefaultObject.Settings.DataBuilders[AddressableAssetSettingsDefaultObject.Settings.ActivePlayerDataBuilderIndex].name);

        // Build addressables 
        AddressableAssetSettings.BuildPlayerContent();
        System.Console.WriteLine("[JenkinsBuild] Addressables Built!");
    }

    private static void BuildProject(string[] scenes, string buildDir, BuildTarget buildTarget, BuildOptions buildOptions)
    {
        System.Console.WriteLine("[JenkinsBuild] Building:" + buildDir + " buildTarget:" + buildTarget.ToString());
        BuildReport buildReport = BuildPipeline.BuildPlayer(scenes, buildDir, buildTarget, buildOptions);
        BuildSummary buildSummary = buildReport.summary;
        if (buildSummary.result == BuildResult.Succeeded)
        {
            System.Console.WriteLine("[JenkinsBuild] Build Success: Time:" + buildSummary.totalTime + " Size:" + buildSummary.totalSize + " bytes");
        }
        else
        {
            System.Console.WriteLine("[JenkinsBuild] Build Failed: Time:" + buildSummary.totalTime + " Total Errors:" + buildSummary.totalErrors);
        }
    }

    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        var bucketEnv = "";
        bool dirty = false;
        string[] args=GetExecuteMethodArguments();
        if (args !=null && args.Length > 2){
            bucketEnv = args[2];
        }

        if (string.IsNullOrEmpty(bucketEnv) == false) {
            var prodBucket = "https://ai2thor-mcs-addressables.s3.amazonaws.com";
            var devBucket = "https://ai2thor-mcs-addressables-dev.s3.amazonaws.com";

            var (_, targetDir) = AddressablesEditor.GetBuildAssetsDirectories(target, pathToBuiltProject);
            if (targetDir == null) throw new NullReferenceException("The target path for the streaming asset settings was not found.");
            var configFileText = File.ReadAllText(targetDir + "/settings.json");
            var selectedBucket = devBucket;
        
            if(bucketEnv.ToLower() == "prod" || bucketEnv.ToLower() == "production" || bucketEnv.ToLower() == "remote") {
                selectedBucket = prodBucket;
                configFileText = configFileText.Replace(devBucket, selectedBucket);
                dirty = true;
            }
            else if(bucketEnv.ToLower() == "dev" || bucketEnv.ToLower() == "development") {
                selectedBucket = devBucket;
                configFileText = configFileText.Replace(prodBucket, selectedBucket);
                dirty = true;
            }

            if (dirty) {
                File.WriteAllText(targetDir + "/settings.json", configFileText);
                System.Console.WriteLine("[JenkinsBuild] Addressables Load Path: " + selectedBucket);
            }
        }
    }
}