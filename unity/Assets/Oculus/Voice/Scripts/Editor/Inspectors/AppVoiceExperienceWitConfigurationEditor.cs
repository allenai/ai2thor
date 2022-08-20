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

using Facebook.WitAi.Data.Configuration;
using Facebook.WitAi.Windows;
using Facebook.WitAi;
using Oculus.Voice.Utility;
using UnityEditor;
using UnityEngine;

namespace Oculus.Voice.Inspectors
{
    [CustomEditor(typeof(WitConfiguration))]
    public class AppVoiceExperienceWitConfigurationEditor : WitConfigurationEditor
    {
        // Override with voice sdk header
        public override Texture2D HeaderIcon => VoiceSDKStyles.MainHeader;
        public override string HeaderUrl => GetSafeAppUrl(configuration, WitTexts.WitAppEndpointType.Settings);
        public override string OpenButtonLabel => IsBuiltInConfiguration(configuration) ? VoiceSDKStyles.Texts.BuiltInAppBtnLabel : base.OpenButtonLabel;

        // Dont allow built-in configurations to refresh
        protected override bool CanConfigurationRefresh(WitConfiguration configuration)
        {
            return base.CanConfigurationRefresh(configuration) && !IsBuiltInConfiguration(configuration);
        }
        // Dont show certain tabs for built in configurations
        protected override bool ShouldTabShow(WitConfiguration configuration, string tabID)
        {
            return base.ShouldTabShow(configuration, tabID) && (!IsBuiltInConfiguration(configuration) || IsBuiltInTabID(tabID));
        }

        // Use to determine if built in configuration
        public static bool IsBuiltInConfiguration(WitConfiguration witConfiguration)
        {
            string applicationID = WitConfigurationUtility.GetAppID(witConfiguration);
            return IsBuiltInConfiguration(applicationID);
        }
        public static bool IsBuiltInConfiguration(string applicationID)
        {
            return !string.IsNullOrEmpty(applicationID) && applicationID.StartsWith("voice");
        }
        // Tabs that should show for built in configurations
        private bool IsBuiltInTabID(string tabID)
        {
            return string.Equals(TAB_APPLICATION_ID, tabID);
        }

        // Get safe app url
        public static string GetSafeAppUrl(WitConfiguration witConfiguration, WitTexts.WitAppEndpointType endpointType)
        {
            // Use built in app url
            if (IsBuiltInConfiguration(witConfiguration))
            {
                return VoiceSDKStyles.Texts.BuiltInAppUrl;
            }
            // Return wit app id
            return WitTexts.GetAppURL(WitConfigurationUtility.GetAppID(witConfiguration), endpointType);
        }
    }
}
