/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace Facebook.WitAi
{
    public static class WitSettingsUtility
    {
        #region SHARED
        // Whether settings have been loaded or not
        public static bool IsLoaded { get; private set; }

        // Current settings
        public static WitSettings Settings
        {
            get
            {
                if (_settings.configSettings == null)
                {
                    LoadSettings();
                }
                return _settings;
            }
        }
        private static WitSettings _settings;

        // Settings save path
        private const string SETTINGS_PATH = "ProjectSettings/wit.config";
        // Server token dictionary path
        private static string GetSettingsFilePath() => Application.dataPath.Replace("Assets", SETTINGS_PATH).Replace("\\", "/");
        // Load Settings
        public static void LoadSettings()
        {
            // Ignore
            if (IsLoaded)
            {
                return;
            }

            // Loaded
            IsLoaded = true;

            // Get file path
            string settingsFilePath = GetSettingsFilePath();
            if (!File.Exists(settingsFilePath))
            {
                Debug.LogWarning($"Wit Settings Utility - Generating new settings file\nPath{settingsFilePath}");
                _settings = new WitSettings();
                return;
            }

            // Read file
            string settingsContents = string.Empty;
            try
            {
                settingsContents = File.ReadAllText(settingsFilePath);
            }
            // Catch error
            catch (Exception e)
            {
                Debug.LogError($"Wit Settings Utility - Failed to load settings file\nPath{settingsFilePath}\nError: {e}");
                _settings = new WitSettings();
                return;
            }

            // Decode file
            try
            {
                _settings = JsonUtility.FromJson<WitSettings>(settingsContents);
            }
            // Catch error
            catch (Exception e)
            {
                Debug.LogError($"Wit Settings Utility - Failed to decode settings file\nPath{settingsFilePath}\nError: {e}");
                _settings = new WitSettings();
                return;
            }
        }
        // Save Settings
        public static void SaveSettings()
        {
            // Get path
            string settingsFilePath = GetSettingsFilePath();

            // Encode file
            string settingsContents = string.Empty;
            try
            {
                settingsContents = JsonUtility.ToJson(_settings);
            }
            // Catch error
            catch (Exception e)
            {
                Debug.LogError($"Wit Settings Utility - Failed to encode settings file\nPath{settingsFilePath}\nError: {e}");
                return;
            }

            // Write file
            try
            {
                File.WriteAllText(settingsFilePath, settingsContents);
            }
            // Catch error
            catch (Exception e)
            {
                Debug.LogError($"Wit Settings Utility - Failed to save settings file\nPath{settingsFilePath}\nError: {e}");
            }
        }
        #endregion

        #region TOKENS
        // Get index for app id
        private static int GetConfigIndexWithAppID(string appID) => Settings.configSettings == null ? -1 : Array.FindIndex(Settings.configSettings, (c) => string.Equals(appID, c.appID));
        // Get index for server token
        private static int GetConfigIndexWithServerToken(string serverToken) => Settings.configSettings == null ? -1 : Array.FindIndex(Settings.configSettings, (c) => string.Equals(serverToken, c.serverToken));

        // Get server token
        public static string GetServerToken(string appID, string defaultServerToken = "")
        {
            // Invalid
            if (string.IsNullOrEmpty(appID))
            {
                return string.Empty;
            }

            // Add if missing
            int index = GetConfigIndexWithAppID(appID);
            if (index == -1)
            {
                AddNewConfigSetting(appID, defaultServerToken);
                index = _settings.configSettings.Length - 1;
            }

            // Success
            return Settings.configSettings[index].serverToken;
        }
        // Get app id from server token
        public static string GetServerTokenAppID(string serverToken, string defaultAppID = "")
        {
            // Invalid
            if (string.IsNullOrEmpty(serverToken))
            {
                return string.Empty;
            }

            // Add if missing
            int index = GetConfigIndexWithServerToken(serverToken);
            if (index == -1)
            {
                AddNewConfigSetting(defaultAppID, serverToken);
                index = _settings.configSettings.Length - 1;
            }

            // Success
            return Settings.configSettings[index].appID;
        }
        // Add setting
        private static void AddNewConfigSetting(string newAppID, string newServerToken)
        {
            // Generate config
            WitConfigSettings config = new WitConfigSettings();
            config.appID = newAppID;
            config.serverToken = newServerToken;

            // Add config
            List<WitConfigSettings> all = new List<WitConfigSettings>();
            if (_settings.configSettings != null)
            {
                all.AddRange(_settings.configSettings);
            }
            all.Add(config);
            _settings.configSettings = all.ToArray();

            // Save settings
            SaveSettings();
        }
        // Set server token
        public static void SetServerToken(string appID, string newServerToken)
        {
            // Invalid
            if (string.IsNullOrEmpty(appID))
            {
                return;
            }

            // Add if missing
            int index = GetConfigIndexWithAppID(appID);
            if (index == -1)
            {
                AddNewConfigSetting(appID, newServerToken);
            }
            // If token changed, adjust
            else if (!string.Equals(newServerToken, _settings.configSettings[index].serverToken))
            {
                WitConfigSettings config = _settings.configSettings[index];
                config.serverToken = newServerToken;
                _settings.configSettings[index] = config;
                SaveSettings();
            }
        }
        #endregion
    }
}
#endif
