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

using System;
using UnityEngine;

namespace Oculus.Voice.Utility
{
    public static class VoiceSDKStyles
    {
        [Serializable]
        public struct VoiceSDKTexts
        {
            [Header("Setup Texts")]
            public string SetupTitleLabel;
            public string SetupHeaderLabel;
            public string SetupSubheaderLabel;
            public string SetupLanguageLabel;
            [Header("About Texts")]
            public string AboutTitleLabel;
            public string AboutCloseLabel;
            public string AboutVoiceSdkVersionLabel;
            public string AboutWitSdkVersionLabel;
            public string AboutWitApiVersionLabel;
            public string AboutTutorialButtonLabel;
            public string AboutTutorialButtonUrl;
            [Header("Settings Texts")]
            public string SettingsTitleLabel;
            [Header("Understanding Viewer Texts")]
            public string UnderstandingViewerTitleLabel;
            [Header("Built-In Texts")]
            public string BuiltInAppBtnLabel;
            public string BuiltInAppUrl;
        }
        public static VoiceSDKTexts Texts;

        public static Texture2D MainHeader;
        public static GUIContent SetupTitle;
        public static GUIContent AboutTitle;
        public static GUIContent SettingsTitle;
        public static GUIContent UnderstandingTitle;

        static VoiceSDKStyles()
        {
            // Load localization
            string languageID = "en-us";
            string textFilePath = $"voicesdk_texts_{languageID}";
            TextAsset textAsset = Resources.Load<TextAsset>(textFilePath);
            if (textAsset == null)
            {
                Debug.LogError($"VoiceSDK Texts - Add localization to Resources/{textFilePath}\nLanguage: {languageID}");
                return;
            }
            Texts = JsonUtility.FromJson<VoiceSDKTexts>(textAsset.text);

            MainHeader = (Texture2D) Resources.Load("voicesdk_heroart");
            SetupTitle = new GUIContent(Texts.SetupTitleLabel);
            AboutTitle = new GUIContent(Texts.AboutTitleLabel);
            SettingsTitle = new GUIContent(Texts.SettingsTitleLabel);
            UnderstandingTitle = new GUIContent(Texts.UnderstandingViewerTitleLabel);
        }
    }
}
