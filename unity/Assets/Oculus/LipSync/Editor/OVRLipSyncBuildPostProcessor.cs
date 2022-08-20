/************************************************************************************
Filename    :   OVRLipSyncBuildPostProcessor.cs
Content     :   Editor extension to generate LipSync-powered iOS apps
Created     :   Feb 11th, 2019
Copyright   :   Copyright Facebook Technologies, LLC and its affiliates.
                All rights reserved.

Licensed under the Oculus Audio SDK License Version 3.3 (the "License");
you may not use the Oculus Audio SDK except in compliance with the License,
which is provided at the time of installation or download, or which
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

https://developer.oculus.com/licenses/audio-3.3/

Unless required by applicable law or agreed to in writing, the Oculus Audio SDK
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
************************************************************************************/
#if UNITY_IOS
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;
using System.Text.RegularExpressions;

class OVRLipSyncBuildPostProcessor : MonoBehaviour
{
    [PostProcessBuildAttribute(1)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target == BuildTarget.iOS)
        {
            AddMicrophoneAccess(Path.Combine(pathToBuiltProject, "Info.plist"));
            AddEmbeddedBinary(PBXProject.GetPBXProjectPath(pathToBuiltProject));
        }
    }

    private static void AddMicrophoneAccess(string infoPlistPath)
    {
        const string micUsageProperty = "NSMicrophoneUsageDescription";

        var plist = new PlistDocument();
        plist.ReadFromFile(infoPlistPath);
        var rootDict = plist.root.AsDict();
        // Don't override the description other might have edited already
        if (rootDict.values.ContainsKey(micUsageProperty))
        {
            return;
        }
        rootDict.SetString(micUsageProperty, "To lipsync you");
        plist.WriteToFile(infoPlistPath);
    }

    private static void AddEmbeddedBinary(string projectPath)
    {
        const string buildPhaseName = "Embed Libraries";
        const string dylibName = "libOVRLipSync.dylib";

        var project = new PBXProject();
        project.ReadFromFile(projectPath);

        // Don't add the same library twice
        if (project.FindFileGuidByProjectPath(dylibName) != null)
        {
            return;
        }

        var targetGUID = project.TargetGuidByName(PBXProject.GetUnityTargetName());
        // Limit the target to ARM64
        project.SetBuildProperty(targetGUID, "ARCHS", "arm64");

        // Add dylib to the project
        var dylibGUID = project.AddFile(
            Path.Combine(Application.dataPath, "Oculus/LipSync/Plugins/iOS/" + dylibName),
            dylibName);
        // Copy it to the same folder as executable
        var embedPhaseGuid = project.AddCopyFilesBuildPhase(targetGUID, buildPhaseName, "", "6");
        project.AddFileToBuildSection(targetGUID, embedPhaseGuid, dylibGUID);
        var content = project.WriteToString();

        // Add CodeSignOnCopy attribute ot the library using an ugly regex
        content = Regex.Replace(content,
            "(?<="+ buildPhaseName + ")(?:.*)(\\/\\* " + Regex.Escape(dylibName) + " \\*\\/)(?=; };)",
            m => m.Value.Replace(
                    "/* " + dylibName + " */",
                    "/* " + dylibName + " */; settings = {ATTRIBUTES = (CodeSignOnCopy, );}"
                    )
                    );
        File.WriteAllText(projectPath, content);
    }

}

#endif
