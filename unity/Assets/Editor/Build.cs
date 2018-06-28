using UnityEditor;
using System;
using System.IO;
using UnityEngine;
using System.Collections.Generic;

public class Build
{
    static void OSXIntel64() {
		build(GetBuildName(), BuildTarget.StandaloneOSX); //was BuildTarget.StandaloneOSXIntel64
    }

    static string GetBuildName() {
		return Environment.GetEnvironmentVariable ("UNITY_BUILD_NAME");
	}

    static void Linux64(){
		build(GetBuildName(), BuildTarget.StandaloneLinux64);
    }

    static void build(string buildName, BuildTarget target)	{		
        List<string> files = new List<string>();
        List<string> scenes = new List<string>();
        files.AddRange(Directory.GetFiles("Assets/Scenes/"));
        files.AddRange(Directory.GetFiles("Assets/Physics/Physics Scenes/"));
        foreach (string f in files) {
            if (f.EndsWith(".unity")) {
                Debug.Log ("Adding Scene " + f);
				scenes.Add (f);
            }
        }

		BuildPipeline.BuildPlayer(scenes.ToArray(), buildName, target, BuildOptions.StrictMode | BuildOptions.CompressWithLz4);
    }
}
