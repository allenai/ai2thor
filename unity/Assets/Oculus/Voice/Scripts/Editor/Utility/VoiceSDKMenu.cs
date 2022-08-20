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
using Facebook.WitAi.Windows;
using Facebook.WitAi.Configuration;
using Facebook.WitAi.Data.Configuration;
using Facebook.WitAi.Data.Intents;
using Facebook.WitAi.Data.Entities;
using Facebook.WitAi.Data.Traits;
using Facebook.WitAi.TTS.Editor.Preload;
using Oculus.Voice.Windows;

namespace Oculus.Voice.Utility
{
    public static class VoiceSDKMenu
    {
        #region WINDOWS
        [MenuItem("Oculus/Voice SDK/Settings", false, 100)]
        private static void OpenConfigurationWindow()
        {
            WitWindowUtility.OpenConfigurationWindow();
        }
        [MenuItem("Oculus/Voice SDK/Understanding Viewer", false, 100)]
        private static void OpenUnderstandingWindow()
        {
            WitWindowUtility.OpenUnderstandingWindow();
        }
        [MenuItem("Oculus/Voice SDK/About", false, 200)]
        private static void OpenAboutWindow()
        {
            ScriptableWizard.DisplayWizard<AboutWindow>(VoiceSDKStyles.Texts.AboutTitleLabel, VoiceSDKStyles.Texts.AboutCloseLabel);
        }
        #endregion

        #region DRAWERS
        [CustomPropertyDrawer(typeof(WitEndpointConfig))]
        public class VoiceCustomEndpointPropertyDrawer : WitEndpointConfigDrawer
        {

        }
        [CustomPropertyDrawer(typeof(WitApplication))]
        public class VoiceCustomApplicationPropertyDrawer : VoiceApplicationDetailProvider
        {

        }
        [CustomPropertyDrawer(typeof(WitIntent))]
        public class VoiceCustomIntentPropertyDrawer : WitIntentPropertyDrawer
        {

        }
        [CustomPropertyDrawer(typeof(WitEntity))]
        public class VoiceCustomEntityPropertyDrawer : WitEntityPropertyDrawer
        {

        }
        [CustomPropertyDrawer(typeof(WitTrait))]
        public class VoiceCustomTraitPropertyDrawer : WitTraitPropertyDrawer
        {

        }
        #endregion

        #region Scriptable Objects
        [MenuItem("Assets/Create/Voice SDK/Dynamic Entities")]
        public static void CreateDynamicEntities()
        {
            WitDynamicEntitiesData asset =
                ScriptableObject.CreateInstance<WitDynamicEntitiesData>();

            var path = EditorUtility.SaveFilePanel("Save Dynamic Entity", Application.dataPath,
                "DynamicEntities", "asset");

            if (!string.IsNullOrEmpty(path))
            {
                path = "Assets/" + path.Replace(Application.dataPath, "");
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.SaveAssets();

                EditorUtility.FocusProjectWindow();

                Selection.activeObject = asset;
            }
        }
        [MenuItem("Assets/Create/Voice SDK/TTS Preload Settings")]
        public static void CreateTTSPreloadSettings()
        {
            TTSPreloadUtility.CreatePreloadSettings();
        }
        #endregion
    }
}
