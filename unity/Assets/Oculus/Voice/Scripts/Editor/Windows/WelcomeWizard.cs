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
using System.Collections.Generic;
using Facebook.WitAi;
using Facebook.WitAi.Data.Configuration;
using Facebook.WitAi.Windows;
using Oculus.Voice.Utility;
using UnityEditor;
using UnityEngine;

namespace Oculus.Voice.Windows
{
    public class WelcomeWizard : WitWelcomeWizard
    {
        private int witBuiltInIndex;
        private string[] builtinAppNames;

        protected override Texture2D HeaderIcon => VoiceSDKStyles.MainHeader;
        protected override GUIContent Title => VoiceSDKStyles.SetupTitle;
        protected override string ContentHeaderLabel => VoiceSDKStyles.Texts.SetupHeaderLabel;
        protected override string ContentSubheaderLabel => VoiceSDKStyles.Texts.SetupSubheaderLabel;

        protected override void OnEnable()
        {
            WitAuthUtility.tokenValidator = new VoiceSDKTokenValidatorProvider();
            base.OnEnable();
            witBuiltInIndex = 0;
            var names = AppBuiltIns.appNames;
            builtinAppNames = new string[names.Length + 1];
            builtinAppNames[0] = "Custom App";
            for (int i = 0; i < names.Length; i++)
            {
                builtinAppNames[i + 1] = names[i];
            }
        }

        protected override void LayoutFields()
        {
            // Prebuilt language app
            bool updated = false;
            WitEditorUI.LayoutLabel(VoiceSDKStyles.Texts.SetupLanguageLabel);
            WitEditorUI.LayoutPopup("", builtinAppNames, ref witBuiltInIndex, ref updated);
            if (updated)
            {
                if (witBuiltInIndex == 0)
                {
                    serverToken = WitAuthUtility.ServerToken;
                }
                else
                {
                    serverToken = AppBuiltIns.builtInPrefix + builtinAppNames[witBuiltInIndex];
                }
            }

            // Base fields
            if (witBuiltInIndex == 0)
            {
                GUILayout.Space(WitStyles.HeaderPaddingBottom);
                base.LayoutFields();
            }
        }

        // Customize configuration if voice app was selected
        protected override int CreateConfiguration(string newToken)
        {
            // Do base for custom app
            if (witBuiltInIndex <= 0)
            {
                return base.CreateConfiguration(newToken);
            }

            // Get built in app data
            string languageName = builtinAppNames[witBuiltInIndex];
            Dictionary<string, string> appData = AppBuiltIns.apps[languageName];

            // Generate asset using app data
            WitConfiguration configuration = ScriptableObject.CreateInstance<WitConfiguration>();
            configuration.clientAccessToken = appData["clientToken"];
            WitApplication application = new WitApplication();
            application.name = appData["name"];
            application.id = appData["id"];
            application.lang = appData["lang"];
            configuration.application = application;
            configuration.name = application.id;

            // Save configuration to asset
            return WitConfigurationUtility.SaveConfiguration(newToken, configuration);
        }
    }

    public class VoiceSDKTokenValidatorProvider : WitAuthUtility.ITokenValidationProvider
    {
        public bool IsTokenValid(string appId, string token)
        {
            return IsServerTokenValid(token);
        }

        public bool IsServerTokenValid(string serverToken)
        {
            return null != serverToken && (serverToken.Length == 32 || serverToken.StartsWith(AppBuiltIns.builtInPrefix));
        }
    }
}
