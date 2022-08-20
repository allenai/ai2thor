/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Oculus.Interaction.Editor
{
    [InitializeOnLoad]
    public static class PackageCleanup
    {
        private enum DeleteResult
        {
            None,
            Success,
            Cancel,
            Incomplete,
        }

        public const string PACKAGE_VERSION = "0.42.0";
        public const string DEPRECATED_TAG = "oculus_interaction_deprecated";
        private const string MENU_NAME = "Oculus/Interaction/Remove Deprecated Assets";
        private const string AUTO_UPDATE_KEY = "Oculus_Interaction_AutoRemoveDeprecated_" + PACKAGE_VERSION;

        private static bool unityRunningInBatchmode = false;

        private static bool AutoCheckDeprecated
        {
            get => PlayerPrefs.GetInt(AUTO_UPDATE_KEY, 1) == 1;
            set => PlayerPrefs.SetInt(AUTO_UPDATE_KEY, value ? 1 : 0);
        }

        static PackageCleanup()
        {
            EditorApplication.delayCall += HandleDelayCall;
        }

        [MenuItem(MENU_NAME)]
        private static void AssetRemovalMenuCommand()
        {
            AutoCheckDeprecated = true;
            StartRemovalUserFlow(true);
        }

        private static void HandleDelayCall()
        {
            if (System.Environment.CommandLine.Contains("-batchmode"))
            {
                unityRunningInBatchmode = true;
            }

            bool startAutoDeprecation = !unityRunningInBatchmode &&
                                        AutoCheckDeprecated &&
                                        !Application.isPlaying;
            if (startAutoDeprecation)
            {
                StartRemovalUserFlow(false);
            }
        }

        /// <summary>
        /// Start the removal flow for removing deprecated assets.
        /// </summary>
        /// <param name="userTriggered">If true, the window will
        /// be non-modal, and a dialog will be shown if no assets found</param>
        public static void StartRemovalUserFlow(bool userTriggered)
        {
            var deprecatedGUIDs =
                AssetDatabase.FindAssets($"l:{DEPRECATED_TAG}", null);

            if (deprecatedGUIDs.Length == 0)
            {
                if (userTriggered)
                {
                    EditorUtility.DisplayDialog("Interaction SDK",
                        "No deprecated assets found in project.", "Close");
                }
                else
                {
                    return;
                }
            }
            else
            {
                int deletionPromptResult = EditorUtility.DisplayDialogComplex(
                    "Interaction SDK",
                    "Deprecated Interaction SDK stubs were included in the package " +
                    "for backwards compatibility during upgrade, and can be removed. " +
                    "Do you want to remove them?" +
                    "\n\n" +
                    "Click 'Show Assets' to view a list of these deprecated assets. " +
                    "You will then be given the option to delete them.",
                    "Show Assets (Recommended)", "No, Don't Ask Again", "No");

                switch (deletionPromptResult)
                {
                    case 0: // "Yes"
                        List<string> assetNames = new List<string>();
                        foreach (var GUID in deprecatedGUIDs)
                        {
                            assetNames.Add(AssetDatabase.GUIDToAssetPath(GUID));
                        }
                        bool modalWindow = !userTriggered;
                        ShowDeprecatedAssetRemovalWindow(assetNames, modalWindow);
                        break;
                    case 1: // "No, Don't Ask Again"
                        AutoCheckDeprecated = false;
                        ShowCancelDialog();
                        break;
                    default:
                    case 2: // "No"
                        AutoCheckDeprecated = true;
                        break;
                }
            }
        }

        private static void ShowDeprecatedAssetRemovalWindow(
            IEnumerable<string> assetPaths, bool modal)
        {
            void DrawHeader(AssetListWindow window)
            {
                EditorGUILayout.HelpBox(
                    "The following assets will be permanently deleted",
                    MessageType.Warning);
            }

            void DrawFooter(AssetListWindow window)
            {
                GUILayoutOption buttonHeight = GUILayout.Height(36);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Delete Assets (Recommended)", buttonHeight))
                {
                    DeleteResult result = DeleteAssets(window.AssetPaths);
                    switch (result)
                    {
                        default:
                        case DeleteResult.None:
                            break;
                        case DeleteResult.Success:
                            AutoCheckDeprecated = false;
                            window.Close();
                            break;
                        case DeleteResult.Cancel:
                            break;
                        case DeleteResult.Incomplete:
                            AutoCheckDeprecated = true;
                            window.Close();
                            break;
                    }
                }
                if (GUILayout.Button("Cancel", buttonHeight))
                {
                    ShowCancelDialog();
                }
                EditorGUILayout.EndHorizontal();
            }

            AssetListWindow assetListWindow = AssetListWindow.Show(
                "Interaction SDK - All Deprecated Assets In Project",
                assetPaths, modal, DrawHeader, DrawFooter);
        }

        private static void ShowCancelDialog()
        {
            AssetListWindow.CloseAll();
            EditorUtility.DisplayDialog("Interaction SDK",
                $"Deprecated assets were not removed. " +
                $"You can run this cleanup utility at any time " +
                $"using the '{MENU_NAME}' menu.",
                "Close");
        }

        private static DeleteResult DeleteAssets(IEnumerable<string> assetPaths)
        {
            bool Delete()
            {
                HashSet<string> filesToDelete = new HashSet<string>();
                HashSet<string> foldersToDelete = new HashSet<string>();
                HashSet<string> skippedFolders = new HashSet<string>();
                HashSet<string> failedPaths = new HashSet<string>();

                foreach (var path in assetPaths)
                {
                    if (File.Exists(path))
                    {
                        filesToDelete.Add(path);
                    }
                    else if (Directory.Exists(path))
                    {
                        foldersToDelete.Add(path);
                    }
                    else
                    {
                        failedPaths.Add(path);
                    }
                }

#if UNITY_2020_1_OR_NEWER
                List<string> failed = new List<string>();

                // Delete files
                AssetDatabase.DeleteAssets(filesToDelete.ToArray(), failed);
                failedPaths.UnionWith(failed);

                // Remove non-empty folders from delete list
                skippedFolders.UnionWith(foldersToDelete
                    .Where((path) => AssetDatabase.FindAssets("", new[] { path })
                    .Select((guid) => AssetDatabase.GUIDToAssetPath(guid))
                    .Any((path) => !AssetDatabase.IsValidFolder(path))));
                foldersToDelete.ExceptWith(skippedFolders);

                // Delete folders, removing longest paths (subfolders) first
                List<string> sortedFolders = new List<string>(foldersToDelete);
                sortedFolders.Sort((a, b) => b.Length.CompareTo(a.Length));
                AssetDatabase.DeleteAssets(sortedFolders.ToArray(), failed);
                failedPaths.UnionWith(failed);
#else
                // Delete files
                foreach (var path in filesToDelete)
                {
                    if (!AssetDatabase.DeleteAsset(path))
                    {
                        failedPaths.Add(path);
                    }
                }

                // Remove non-empty folders from delete list
                skippedFolders.UnionWith(foldersToDelete
                    .Where((path) => Directory.EnumerateFiles(path).Any()));
                foldersToDelete.ExceptWith(skippedFolders);

                // Delete folders
                foreach (var path in foldersToDelete)
                {
                    if (!AssetDatabase.DeleteAsset(path))
                    {
                        failedPaths.Add(path);
                    }
                }
#endif
                string logMessage;

                if (BuildLogMessage("Deprecated assets deleted:",
                    filesToDelete.Union(foldersToDelete), out logMessage))
                {
                    Debug.Log(logMessage);
                }
                if (BuildLogMessage("Skipped non-empty folders:",
                    skippedFolders, out logMessage))
                {
                    Debug.LogWarning(logMessage);
                }
                if (BuildLogMessage("Failed to delete assets:",
                    failedPaths, out logMessage))
                {
                    Debug.LogError(logMessage);
                }

                return failedPaths.Count == 0;
            }

            if (EditorUtility.DisplayDialog("Are you sure?",
               "Deprecated Interaction SDK assets will be permanently deleted." +
                "\n\n" +
                "It is strongly recommended that you back up your project before proceeding.",
                "Delete Assets", "Cancel"))
            {
                return Delete() ? DeleteResult.Success : DeleteResult.Incomplete;
            }
            else
            {
                return DeleteResult.Cancel;
            }
        }

        private static bool BuildLogMessage(
            string title,
            IEnumerable<string> paths,
            out string message)
        {
            int count = 0;
            StringBuilder sb = new StringBuilder();

            sb.Append(title);
            foreach (var path in paths)
            {
                sb.Append(System.Environment.NewLine);
                sb.Append(path);
                ++count;
            }
            message = sb.ToString();
            return count > 0;
        }
    }
}
