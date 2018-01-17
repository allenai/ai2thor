using UnityEditor;
using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;

public class Build
{
    static void OSXIntel64() {
		build(GetBuildName(), BuildTarget.StandaloneOSXIntel64);
    }

    static string GetBuildName() {
		return Environment.GetEnvironmentVariable ("UNITY_BUILD_NAME");
	}

    static void Linux64(){
		build(GetBuildName(), BuildTarget.StandaloneLinux64);
    }

    static void build(string buildName, BuildTarget target)	{		
        List<string> scenes = new List<string>();
        foreach (string f in Directory.GetFiles("Assets/Scenes/Physics_enabled/")) {
            if (f.EndsWith(".unity")) {
				scenes.Add (f);
            }
        }

		BuildPipeline.BuildPlayer(scenes.ToArray(), buildName, target, BuildOptions.None);
		//BuildPipeline.BuildPlayer(scenes.ToArray(), buildName, target, BuildOptions.StrictMode);
    }
}
