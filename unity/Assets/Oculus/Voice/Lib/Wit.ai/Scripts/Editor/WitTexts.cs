/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Facebook.WitAi
{
    public static class WitTexts
    {
        // Localized text
        public static WitText Texts;
        [System.Serializable]
        public struct WitText
        {
            [Header("Shared Settings Texts")]
            public string LanguageID;
            public string WitAppsUrl;
            public string WitAppSettingsEndpoint;
            public string WitAppUnderstandingEndpoint;
            public string WitOpenButtonLabel;
            public string ConfigurationFileManagerLabel;
            public string ConfigurationFileNameLabel;
            public string ConfigurationSelectLabel;
            public string ConfigurationSelectMissingLabel;
            [Header("Setup Settings Texts")]
            public string SetupTitleLabel;
            public string SetupSubheaderLabel;
            public string SetupServerTokenLabel;
            public string SetupSubmitButtonLabel;
            public string SetupSubmitFailLabel;
            [Header("Understanding Viewer Texts")]
            public string UnderstandingViewerLabel;
            public string UnderstandingViewerMissingConfigLabel;
            public string UnderstandingViewerMissingClientTokenLabel;
            public string UnderstandingViewerServicesLabel;
            public string UnderstandingViewerMissingServicesLabel;
            public string UnderstandingViewerSettingsButtonLabel;
            public string UnderstandingViewerUtteranceLabel;
            public string UnderstandingViewerPromptLabel;
            public string UnderstandingViewerSubmitButtonLabel;
            public string UnderstandingViewerActivateButtonLabel;
            public string UnderstandingViewerDeactivateButtonLabel;
            public string UnderstandingViewerAbortButtonLabel;
            public string UnderstandingViewerListeningLabel;
            public string UnderstandingViewerLoadingLabel;
            [Header("Settings Texts")]
            public string SettingsTitleLabel;
            public string SettingsServerTokenLabel;
            public string SettingsServerTokenTooltip;
            public string SettingsRelinkButtonLabel;
            public string SettingsAddButtonLabel;
            [Header("Configuration Texts")]
            public string ConfigurationHeaderLabel;
            public string ConfigurationRefreshButtonLabel;
            public string ConfigurationRefreshingButtonLabel;
            public string ConfigurationServerTokenLabel;
            public string ConfigurationClientTokenLabel;
            public string ConfigurationRequestTimeoutLabel;

            [Header("Configuration Endpoint Texts")]
            public string ConfigurationEndpointTitleLabel;
            public string ConfigurationEndpointUriLabel;
            public string ConfigurationEndpointAuthLabel;
            public string ConfigurationEndpointPortLabel;
            public string ConfigurationEndpointApiLabel;
            public string ConfigurationEndpointSpeechLabel;
            [Header("Configuration Application Texts")]
            public string ConfigurationApplicationTabLabel;
            public string ConfigurationApplicationMissingLabel;
            public string ConfigurationApplicationNameLabel;
            public string ConfigurationApplicationIdLabel;
            public string ConfigurationApplicationLanguageLabel;
            public string ConfigurationApplicationPrivateLabel;
            public string ConfigurationApplicationCreatedLabel;

            [Header("Configuration Intent Texts")]
            public string ConfigurationIntentsTabLabel;
            public string ConfigurationIntentsMissingLabel;
            public string ConfigurationIntentsIdLabel;
            public string ConfigurationIntentsEntitiesLabel;
            [Header("Configuration Entity Texts")]
            public string ConfigurationEntitiesTabLabel;
            public string ConfigurationEntitiesMissingLabel;
            public string ConfigurationEntitiesIdLabel;
            public string ConfigurationEntitiesLookupsLabel;
            public string ConfigurationEntitiesRolesLabel;
            [Header("Configuration Trait Texts")]
            public string ConfigurationTraitsTabLabel;
            public string ConfigurationTraitsMissingLabel;
            public string ConfigurationTraitsIdLabel;
            public string ConfigurationTraitsValuesLabel;
        }

        // Wit
        public const string WitUrl = "https://wit.ai";
        // Endpoint
        public enum WitAppEndpointType
        {
            Settings,
            Understanding
        }
        // Title Contents
        public static Texture2D HeaderIcon;
        public static Texture2D TitleIcon;
        public static GUIContent SetupTitleContent;
        public static GUIContent UnderstandingTitleContent;
        public static GUIContent SettingsTitleContent;
        public static GUIContent SettingsServerTokenContent;
        public static GUIContent ConfigurationServerTokenContent;
        public static GUIContent ConfigurationClientTokenContent;
        public static GUIContent ConfigurationRequestTimeoutContent;

        // Init
        static WitTexts()
        {
            // Get text
            string languageID = "en-us";
            string textFilePath = $"witai_texts_{languageID}";
            TextAsset textAsset = Resources.Load<TextAsset>(textFilePath);
            if (textAsset == null)
            {
                Debug.LogError($"WitStyles - Add localization to Resources/{textFilePath}\nLanguage: {languageID}");
                return;
            }
            Texts = JsonUtility.FromJson<WitText>(textAsset.text);

            // Setup titles
            HeaderIcon = (Texture2D) Resources.Load("wit-ai-title");
            TitleIcon = (Texture2D) Resources.Load("witai");
            SetupTitleContent = new GUIContent(WitTexts.Texts.SetupTitleLabel, TitleIcon);
            SettingsTitleContent = new GUIContent(WitTexts.Texts.SettingsTitleLabel, TitleIcon);
            SettingsServerTokenContent = new GUIContent(WitTexts.Texts.SettingsServerTokenLabel, WitTexts.Texts.SettingsServerTokenTooltip);
            UnderstandingTitleContent = new GUIContent(WitTexts.Texts.UnderstandingViewerLabel, TitleIcon);
            ConfigurationServerTokenContent = new GUIContent(WitTexts.Texts.ConfigurationServerTokenLabel);
            ConfigurationClientTokenContent = new GUIContent(WitTexts.Texts.ConfigurationClientTokenLabel);
            ConfigurationRequestTimeoutContent = new GUIContent(WitTexts.Texts.ConfigurationRequestTimeoutLabel);
        }
        // Get urls
        public static string GetAppURL(string appId, WitAppEndpointType endpointType)
        {
            // Return apps url without id
            string url = WitUrl + Texts.WitAppsUrl;
            if (string.IsNullOrEmpty(appId))
            {
                return url;
            }
            // Determine endpoint
            string endpoint;
            switch (endpointType)
            {
                case WitAppEndpointType.Understanding:
                    endpoint = Texts.WitAppUnderstandingEndpoint;
                    break;
                case WitAppEndpointType.Settings:
                    endpoint = Texts.WitAppSettingsEndpoint;
                    break;
                default:
                    endpoint = Texts.WitAppSettingsEndpoint;
                    break;
            }
            // Ensure endpoint is set
            if (string.IsNullOrEmpty(endpoint))
            {
                return url;
            }
            // Replace app id key with desired app id
            endpoint = endpoint.Replace("[APP_ID]", appId);
            // Return full url
            return url + endpoint;
        }
    }
}
