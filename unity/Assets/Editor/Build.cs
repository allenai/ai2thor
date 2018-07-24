using UnityEditor;
using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;

public class Build
{
    static void OSXIntel64() {
#if UNITY_2017_3_OR_NEWER
		var buildTarget  = BuildTarget.StandaloneOSX;
#else
		var buildTarget = BuildTarget.StandaloneOSXIntel64;
#endif
		build(GetBuildName(), buildTarget);
    }

    static string GetBuildName() {
		return Environment.GetEnvironmentVariable ("UNITY_BUILD_NAME");
	}

    static void Linux64(){
		build(GetBuildName(), BuildTarget.StandaloneLinux64);
    }

    static void build(string buildName, BuildTarget target)	{		
        List<string> scenes = new List<string>();
        foreach (string f in Directory.GetFiles("Assets/Scenes/")) {
            if (f.EndsWith(".unity")) {
                Debug.Log ("Adding Scene " + f);
				scenes.Add (f);
            }
        }

		BuildPipeline.BuildPlayer(scenes.ToArray(), buildName, target, BuildOptions.StrictMode);
    }
}
