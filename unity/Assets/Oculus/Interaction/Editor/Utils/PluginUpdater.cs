/************************************************************************************
Copyright : Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Your use of this SDK or tool is subject to the Oculus SDK License Agreement, available at
https://developer.oculus.com/licenses/oculussdk/

Unless required by applicable law or agreed to in writing, the Utilities SDK distributed
under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
ANY KIND, either express or implied. See the License for the specific language governing
permissions and limitations under the License.
************************************************************************************/

using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;

using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using Assert = NUnit.Framework.Assert;
using Debug = UnityEngine.Debug;
using System.Diagnostics;
using System.Collections.Generic;

namespace Oculus.Interaction
{
    [InitializeOnLoad]
    public class PluginUpdater
    {
        private static bool _isRestarting = false;
        private static string _keyBase = "Oculus_Interaction_PluginUpdater";
        private static string _keyDontAsk = _keyBase + "_DontAsk";
        private static string _keyIsInstalling = _keyBase + "_IsRestarting";
        private static BuildTarget[] buildTargets = { BuildTarget.Android, BuildTarget.StandaloneWindows, BuildTarget.StandaloneWindows64 };
        private static string _baseDllName = "InteractionSdk";

        static PluginUpdater()
        {
            EditorApplication.delayCall += HandleDelayCall;
        }

        [MenuItem("Oculus/Interaction/Update Interaction SDK Plugin")]
        public static void UpdatePlugin()
        {
            PerformUpdate(verbose: true);
        }

        private static string GetProjectPath()
        {
            string path = Application.dataPath;
            string token = "/Assets";
            if (path.EndsWith(token))
            {
                path = path.Substring(0, path.Length - token.Length);
            }

            return path;
        }

        public static void HandleDelayCall()
        {
            if (!EditorApplication.isPlaying && !_isRestarting)
            {
                // first off, check to see if there's NO current dll for the build targets, and if so just copy the new one over right now since there won't be a chance of write blocking
                foreach (BuildTarget t in buildTargets)
                {
                    if (IsNewLibraryPresent(t) && !IsCurrentLibraryPresent(t))
                    {
                        MoveNewLibary(t);
                    }
                }

                // only do the moving when not running and not called from command line in batch mode
                if (!Application.isBatchMode)
                {
                    // has an install been started and Unity just got restarted?
                    // If so, start moving the new libraries into the names where the old ones were located
                    if (PlayerPrefs.GetInt(_keyIsInstalling, 0) > 0)
                    {
                        foreach (BuildTarget t in buildTargets)
                        {
                            if (IsNewLibraryPresent(t))
                            {
                                MoveNewLibary(t);
                            }
                        }

                        // mark as done for next time Unity starts
                        PlayerPrefs.SetInt(_keyIsInstalling, 0);
                    }
                    else if (PlayerPrefs.GetInt(_keyDontAsk, 0) == 0)
                    {
                        // if user hasn't asked us to stop bugging them, go on and ask 'em
                        PerformUpdate(verbose: false);
                    }
                }
            }
        }

        private static string GetTargetFolderName(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.Android:
                    return "Android";
                case BuildTarget.StandaloneOSX:
                    return "OSX";
                case BuildTarget.StandaloneWindows:
                    return "Win32";
                case BuildTarget.StandaloneWindows64:
                    return "Win64";
                default:
                    throw new ArgumentException("Attempted GetTargetFolderName() for unsupported BuildTarget: " + target);
            }
        }

        private static string GetTargetDllSuffix(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.Android:
                    return ".aar";
                case BuildTarget.StandaloneOSX:
                    return ".bundle";
                case BuildTarget.StandaloneWindows:
                    return ".dll";
                case BuildTarget.StandaloneWindows64:
                    return ".dll";
                default:
                    throw new ArgumentException("Attempted GetTargetDllSuffix() for unsupported BuildTarget: " + target);
            }
        }

        private static string[] FindAllSdkDlls(BuildTarget target)
        {
            char[] slashes = new char[] { '/', '\\' };

            List<string> dllPaths = new List<string>();
            string[] dlls = AssetDatabase.FindAssets(_baseDllName);
            string targetPath = GetTargetFolderName(target);
            foreach (string dll in dlls)
            {
                string path = AssetDatabase.GUIDToAssetPath(dll);
                string suffix = GetTargetDllSuffix(target);

                if (path.Contains(_baseDllName + suffix) && !path.Contains(_baseDllName + suffix + ".disabled"))
                {
                    // see if the path contains the build target name (e.g. Win64) we're looking for
                    string[] subPaths = path.Split(slashes);
                    foreach(string subpath in subPaths)
                    {
                        if (subpath == targetPath)
                        {
                            dllPaths.Add(path);
                            break;
                        }
                    }


                }
            }
            return dllPaths.ToArray();
        }

        private static void PerformUpdate(bool verbose)
        {
            int newLibsFound = 0;

            foreach (BuildTarget t in buildTargets)
            {
                if (IsNewLibraryPresent(t))
                {
                    newLibsFound++;
                }
            }

            if (newLibsFound > 0)
            {
                // the ordering of the dialog responses is to keep consistency with the OVRPluginUpdater dialog responses
                int response = EditorUtility.DisplayDialogComplex("Update Interaction SDK libraries?", "New versions of Interaction SDK libraries were found, it is recommended you update them now.", "Ok", "No, Don't ask again", "Not right now");
                switch (response)
                {
                    case 0: // yes
                        if (MoveAllNewLibraries() > 0)
                        {
                            PlayerPrefs.SetInt(_keyIsInstalling, 1);
                            response = EditorUtility.DisplayDialogComplex("Restart Unity?", "Unity needs to be restarted in order for the Interaction SDK installation to complete", "Ok", "Not right now", "Cancel");

                            if (response == 0)
                                RestartUnity();
                        }
                        else
                        {
                            PlayerPrefs.SetInt(_keyIsInstalling, 0);
                            EditorUtility.DisplayDialog("Library update error", "There was an issue updating the new Interaction SDK libraries", "Ok");
                            return;
                        }
                        break;

                    case 1: // dont ask again
                        PlayerPrefs.SetInt(_keyDontAsk, 1);
                        break;

                    case 2: // no
                        break;
                }
            }
            else
            {
                if (verbose)
                {
                    EditorUtility.DisplayDialog("Interaction SDK Plugin Updater", "No new libraries were found to update", "Ok");
                }
            }
        }

        private static int MoveAllNewLibraries()
        {
            int found = 0;
            foreach (BuildTarget t in buildTargets)
            {
                if (IsNewLibraryPresent(t))
                {
                    found++;
                    if (IsCurrentLibraryPresent(t))
                    {
                        MoveCurrentLibrary(t);
                    }
                }
            }

            return found;
        }

        private static string GetCurrentLibraryAssetPath(BuildTarget target, bool newVersion)
        {
            string[] dlls = FindAllSdkDlls(target);
            foreach (var dllName in dlls)
            {
                string suffix = GetTargetDllSuffix(target);

                if (dllName.Contains(suffix))
                {
                    bool isNew = dllName.Contains(suffix + ".new");
                    if (newVersion == isNew)
                        return dllName;
                }
            }

            return null;
        }

        private static bool IsCurrentLibraryPresent(BuildTarget target)
        {
            return GetCurrentLibraryAssetPath(target, false) != null;
        }

        private static bool IsNewLibraryPresent(BuildTarget target)
        {
            return GetCurrentLibraryAssetPath(target, true) != null;
        }

        private static bool MoveCurrentLibrary(BuildTarget target)
        {
            string srcPath = GetCurrentLibraryAssetPath(target, false);
            bool success = MoveAsset(srcPath, srcPath + ".disabled", true);
            return success;
        }

        private static bool MoveNewLibary(BuildTarget target)
        {
            string src = GetCurrentLibraryAssetPath(target, true);
            string dest = src;
            if (dest.EndsWith(".new"))
            {
                dest = dest.Substring(0, dest.Length - 4);
            }
            bool success = MoveAsset(src, dest, false);
            if (success)
            {
                PluginImporter pi = PluginImporter.GetAtPath(dest) as PluginImporter;
                pi.SetCompatibleWithEditor(false);
                pi.SetCompatibleWithAnyPlatform(false);
                pi.SetCompatibleWithPlatform(BuildTarget.StandaloneOSX, false);
                pi.SetCompatibleWithPlatform(BuildTarget.Android, false);
                pi.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows, false);
                pi.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64, false);
                pi.SetCompatibleWithPlatform(target, true);
                pi.SetCompatibleWithEditor(true);

                switch (target)
                {
                    case BuildTarget.StandaloneOSX:
                        pi.SetCompatibleWithEditor(true);
                        pi.SetEditorData("CPU", "AnyCPU");
                        pi.SetEditorData("OS", "OSX");
                        pi.SetPlatformData("Editor", "CPU", "AnyCPU");
                        pi.SetPlatformData("Editor", "OS", "OSX");
                        break;
                    case BuildTarget.StandaloneWindows:
                        pi.SetEditorData("CPU", "X86");
                        pi.SetEditorData("OS", "Windows");
                        pi.SetPlatformData("Editor", "CPU", "X86");
                        pi.SetPlatformData("Editor", "OS", "Windows");
                        break;
                    case BuildTarget.Android:
                        break;
                    case BuildTarget.StandaloneWindows64:
                        pi.SetCompatibleWithPlatform(BuildTarget.StandaloneWindows64, true);
                        pi.SetCompatibleWithEditor(true);
                        pi.SetEditorData("CPU", "X86_64");
                        pi.SetEditorData("OS", "Windows");
                        pi.SetPlatformData("Editor", "CPU", "X86_64");
                        pi.SetPlatformData("Editor", "OS", "Windows");
                        break;
                }

                ReimportAsset(dest);
            }
            else
            {
                Debug.LogErrorFormat("ISDK PluginUpdater: Error copying {0} to {1}", src, dest);
            }

            return success;
        }

        private static void ReimportAsset(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }

        private static bool MoveAsset(string srcPath, string destPath, bool reImport)
        {
            string fullDest = GetProjectPath() + "/" + destPath;
            if (File.Exists(fullDest))
            {
                File.Delete(fullDest);
                File.Delete(fullDest + ".meta");
            }

            string errMsg = AssetDatabase.MoveAsset(srcPath, destPath);
            if (errMsg.Length > 0)
            {
                UnityEngine.Debug.LogError(errMsg);
                return false;
            }

            if (reImport)
            {
                ReimportAsset(destPath);
            }

            return true;
        }

        private static void RestartUnity()
        {
            _isRestarting = true;
            EditorApplication.OpenProject(GetProjectPath());
        }
    }
}
