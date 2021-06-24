using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Class for building through Jenkins with Addressables support
/// </summary>
public class JenkinsBuild
{
    static string[] EnabledScenes = FindEnabledEditorScenes();
    static string DEFAULT_BUILD_NAME = "MCS-AI2-THOR";
    static string DEFAULT_BUILD_DIR = "~/Desktop";

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

        SwitchPlatform(BuildTargetGroup.Standalone, buildTarget);
        BuildAddressablesContent();
        string fullPathAndName = buildPath + System.IO.Path.DirectorySeparatorChar + buildName + extension;
        BuildProject(EnabledScenes, fullPathAndName, buildTarget, BuildOptions.None);
    }

    /// <summary>
    /// Example:
    /// -executeMethod JenkinsBuild.BuildLinux
    /// MCS-AI2-THOR
    /// ai2thor/unity/Builds/Linux
    /// </summary>
    /// <returns></returns>
    private static string[] GetExecuteMethodArguments()
    {
        string[] returnedArgs = new string[] { DEFAULT_BUILD_NAME, DEFAULT_BUILD_DIR};
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-executeMethod")
            {
                if (i + 3 <= args.Length)
                {
                    returnedArgs[0] = args[i + 2];
                    returnedArgs[1] = args[i + 3];
                    return returnedArgs;
                }
                else
                {
                    System.Console.WriteLine("[JenkinsBuild] Incorrect Parameters for -executeMethod Format: -executeMethod Build[PLATFORM] <app name> <output dir>");
                    System.Console.WriteLine("[JenkinsBuild] Parameter 1: " + args[i + 2]);
                    System.Console.WriteLine("[JenkinsBuild] Parameter 2: " + args[i + 3]);
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
        System.Console.WriteLine("[JenkinsBuild] Addressables Load Path: " + 
            addressableAssetSettings.RemoteCatalogLoadPath.GetValue(addressableAssetSettings));

        // Build addressables 
        AddressableAssetSettings.BuildPlayerContent();
        System.Console.WriteLine("[JenkinsBuild] Addressables Built!");

        FileWriteTest();
    }

    private static void FileWriteTest()
    {
        // Check ServerData path
        var serverDataPath = Path.GetDirectoryName(Application.dataPath) + Path.DirectorySeparatorChar + "ServerData";
        System.Console.WriteLine("[JenkinsBuild] ServerData path: " + serverDataPath);
        var serverDataPathExists = Directory.Exists(serverDataPath);
        System.Console.WriteLine("[JenkinsBuild] ServerData exists? : " + serverDataPathExists);

        // Write to path if it does not exist
        if (!serverDataPathExists)
        {
            Directory.CreateDirectory(serverDataPath);

            var filePath = serverDataPath + Path.DirectorySeparatorChar + "didthiswork.txt";
            try
            {
                if (!File.Exists(filePath))
                {
                    // Create a file to write to.
                    using (StreamWriter sw = File.CreateText(filePath))
                    {
                        sw.WriteLine("yes?");
                    }
                }
            }
            catch (System.Exception Ex)
            {
                // Catch any error which could be causing addressables write failure
                System.Console.WriteLine("[JenkinsBuild] ServerData write failure: " + Ex.ToString());
            }
        }

        // Does it exist yet?
        System.Console.WriteLine("[JenkinsBuild] ServerData exists? : " + Directory.Exists(serverDataPath));
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
}