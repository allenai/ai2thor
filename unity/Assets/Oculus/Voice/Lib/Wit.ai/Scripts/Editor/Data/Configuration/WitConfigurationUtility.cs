/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

#if UNITY_EDITOR
//#define VERBOSE_LOG
#endif

using System;
using System.Collections.Generic;
using Facebook.WitAi.Data.Entities;
using Facebook.WitAi.Data.Intents;
using Facebook.WitAi.Data.Traits;
using Facebook.WitAi.Lib;
using Facebook.WitAi.Configuration;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Facebook.WitAi.Data.Configuration
{
    public static class WitConfigurationUtility
    {
        #region ACCESS
        // Return wit configs
        public static WitConfiguration[] WitConfigs
        {
            get
            {
                // Reload if not setup
                if (_witConfigs == null)
                {
                    ReloadConfigurationData();
                }
                // Force reload
                if (_needsConfigReload)
                {
                    ReloadConfigurationData();
                }
                // Return config data
                return _witConfigs;
            }
        }
        // Wit configuration assets
        private static WitConfiguration[] _witConfigs = null;

        // Wit configuration asset names
        public static string[] WitConfigNames => _witConfigNames;
        private static string[] _witConfigNames = Array.Empty<string>();

        // Config reload
        private static bool _needsConfigReload = false;

        // Has configuration
        public static bool HasValidCustomConfig()
        {
            // Find a valid custom configuration
            return Array.Exists(WitConfigs, (c) => !c.isDemoOnly);
        }
        // Enable config reload on next access
        public static void NeedsConfigReload()
        {
            _needsConfigReload = true;
        }
        // Refresh configuration asset list
        public static void ReloadConfigurationData()
        {
            // Reloaded
            _needsConfigReload = false;

            // Find all Wit Configurations
            List<WitConfiguration> found = new List<WitConfiguration>();
            string[] guids = AssetDatabase.FindAssets("t:WitConfiguration");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                WitConfiguration config = AssetDatabase.LoadAssetAtPath<WitConfiguration>(path);
                if (!config.isDemoOnly)
                {
                    found.Add(config);
                }
            }

            // Store wit configuration data
            _witConfigs = found.ToArray();
            // Obtain all names
            _witConfigNames = new string[_witConfigs.Length];
            for (int i = 0; i < _witConfigs.Length; i++)
            {
                _witConfigNames[i] = _witConfigs[i].name;
            }
        }
        // Get configuration index
        public static int GetConfigurationIndex(WitConfiguration configuration)
        {
            // Search through configs
            return Array.FindIndex(WitConfigs, (checkConfig) => checkConfig == configuration );
        }
        // Get configuration index
        public static int GetConfigurationIndex(string configurationName)
        {
            // Search through configs
            return Array.FindIndex(WitConfigs, (checkConfig) => string.Equals(checkConfig.name, configurationName));
        }
        // Get application id
        public static string GetAppID(WitConfiguration configuration)
        {
            if (configuration != null && configuration.application != null)
            {
                return configuration.application.id;
            }
            return string.Empty;
        }
        #endregion

        #region MANAGEMENT
        // Create configuration for token with blank configuration
        public static int CreateConfiguration(string serverToken)
        {
            // Generate blank asset
            WitConfiguration configurationAsset = ScriptableObject.CreateInstance<WitConfiguration>();
            configurationAsset.name = WitTexts.Texts.ConfigurationFileNameLabel;
            configurationAsset.clientAccessToken = string.Empty;
            // Create
            int index = SaveConfiguration(serverToken, configurationAsset);
            if (index == -1)
            {
                MonoBehaviour.DestroyImmediate(configurationAsset);
            }
            // Return new index
            return index;
        }
        // Get asset save directory
        public static string GetFileSaveDirectory(string title, string fileName, string fileExt)
        {
            // Determine root directory with selection if possible
            string rootDirectory = Application.dataPath;
            if (Selection.activeObject)
            {
                // Get asset path
                string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                // Only allow if in assets
                if (selectedPath.StartsWith("Assets"))
                {
                    if (AssetDatabase.IsValidFolder(selectedPath))
                    {
                        rootDirectory = selectedPath;
                    }
                    else if (!string.IsNullOrEmpty(selectedPath))
                    {
                        rootDirectory = new System.IO.FileInfo(selectedPath).DirectoryName;
                    }
                }
            }
            // Save panel
            return EditorUtility.SaveFilePanel(title, rootDirectory, fileName, fileExt);
        }
        // Save configuration after determining path
        public static int SaveConfiguration(string serverToken, WitConfiguration configurationAsset)
        {
            string savePath = GetFileSaveDirectory(WitTexts.Texts.ConfigurationFileManagerLabel, WitTexts.Texts.ConfigurationFileNameLabel, "asset");
            return SaveConfiguration(savePath, serverToken, configurationAsset);
        }
        // Save configuration to selected location
        public static int SaveConfiguration(string savePath, string serverToken, WitConfiguration configurationAsset)
        {
            // Ensure valid save path
            if (string.IsNullOrEmpty(savePath))
            {
                return -1;
            }
            // Must be in assets
            string unityPath = savePath.Replace("\\", "/");
            if (!unityPath.StartsWith(Application.dataPath))
            {
                Debug.LogError($"Configuration Utility - Cannot Create Configuration Outside of Assets Directory\nPath: {unityPath}");
                return -1;
            }

            // Determine local unity path
            unityPath = unityPath.Replace(Application.dataPath, "Assets");
            AssetDatabase.CreateAsset(configurationAsset, unityPath);
            AssetDatabase.SaveAssets();

            // Refresh configurations
            ReloadConfigurationData();

            // Get new index following reload
            string name = System.IO.Path.GetFileNameWithoutExtension(unityPath);
            int index = GetConfigurationIndex(name);
            _witConfigs[index].SetServerToken(serverToken);

            // Return index
            return index;
        }
        #endregion

        #region TOKENS
        // Token valid check
        public static bool IsServerTokenValid(string serverToken)
        {
            return !string.IsNullOrEmpty(serverToken) && WitAuthUtility.IsServerTokenValid(serverToken);
        }
        // Token valid check
        public static bool IsClientTokenValid(string clientToken)
        {
            return !string.IsNullOrEmpty(clientToken) && clientToken.Length == 32;
        }
        // Sets server token for all configurations if possible
        public static void SetServerToken(string serverToken, Action<string> onSetComplete = null)
        {
            // Invalid token
            if (!IsServerTokenValid(serverToken))
            {
                SetServerTokenComplete(string.Empty, "", onSetComplete);
                return;
            }
            // Perform a list app request to get app for token
            var listRequest = WitRequestFactory.ListAppsRequest(serverToken, 10000);
            PerformRequest(listRequest, (r, o) => ApplyAllApplicationData(serverToken, r, o), (error) =>
            {
                SetServerTokenComplete(serverToken, error, onSetComplete);
            });
        }
        // Set server token complete
        private static void SetServerTokenComplete(string serverToken, string error, Action<string> onSetComplete)
        {
            // Failed
            if (!string.IsNullOrEmpty(error))
            {
                error = $"Set Server Token Failed\n{error}";
                Log(error, true);
                WitAuthUtility.ServerToken = "";
            }
            // Success
            else
            {
                // Log Success
                Log("Set Server Token Success", false);
                // Apply token
                WitAuthUtility.ServerToken = serverToken;
                // Refresh configurations
                ReloadConfigurationData();
            }
            // On complete
            onSetComplete?.Invoke(error);
        }
        // Sets server token for specified configuration by updating it's application data
        public static void SetServerToken(this WitConfiguration configuration, string serverToken, Action<string> onSetComplete = null)
        {
            // Invalid
            if (!IsServerTokenValid(serverToken))
            {
                SetConfigServerTokenComplete(configuration, serverToken, "Invalid Token", onSetComplete);
                return;
            }
            // Refresh app data
            SetApplicationData(configuration, serverToken, onSetComplete);
        }
        // Refresh client data
        private static void SetApplicationData(WitConfiguration configuration, string serverToken, Action<string> onSetComplete)
        {
            // Already set in app server data
            string appID = GetAppID(configuration);
            if (!string.IsNullOrEmpty(appID))
            {
                string curToken = WitAuthUtility.GetAppServerToken(appID);
                if (string.Equals(curToken, serverToken))
                {
                    SetClientData(configuration, serverToken, onSetComplete);
                    return;
                }
            }
            // Perform a list app request to get app for token
            var listRequest = WitRequestFactory.ListAppsRequest(serverToken, 10000);
            PerformConfigRequest(configuration, listRequest, ApplyApplicationData, (error) =>
            {
                // Failed
                if (!string.IsNullOrEmpty(error))
                {
                    SetConfigServerTokenComplete(configuration, serverToken, error, onSetComplete);
                }
                // Find client token
                else
                {
                    SetClientData(configuration, serverToken, onSetComplete);
                }
            });
        }
        // Refresh client data
        private static void SetClientData(WitConfiguration configuration, string serverToken, Action<string> onSetComplete)
        {
            // Invalid app ID
            string appID = GetAppID(configuration);
            if (string.IsNullOrEmpty(appID))
            {
                SetConfigServerTokenComplete(configuration, serverToken, "Invalid App ID", onSetComplete);
                return;
            }
            // Set server token
            WitAuthUtility.SetAppServerToken(appID, serverToken);
            // Clear client token
            ApplyClientToken(configuration, string.Empty, null);
            // Find client id
            PerformConfigRequest(configuration, configuration.GetClientToken(appID), ApplyClientToken, (error) =>
            {
                SetConfigServerTokenComplete(configuration, serverToken, error, onSetComplete);
            });
        }
        // Complete
        private static void SetConfigServerTokenComplete(WitConfiguration configuration, string serverToken, string error, Action<string> onSetComplete)
        {
            // Failed
            if (!string.IsNullOrEmpty(error))
            {
                error = "Set Configuration Server Token Failed\n" + error;
                Log(error, true);
            }
            // Success
            else
            {
                // Log success
                Log("Set Configuration Server Token Success", false);
                // Refresh data
                configuration.RefreshData(onSetComplete);
            }
            // On complete
            onSetComplete?.Invoke(error);
        }
        #endregion

        #region REFRESH
        // Refresh if possible & return true if still refreshing
        private static List<string> refreshAppIDs = new List<string>();
        // Check if refreshing
        private static bool IsRefreshing(string appID)
        {
            return !string.IsNullOrEmpty(appID) && refreshAppIDs.Contains(appID);
        }
        // Check if refreshing
        public static bool IsRefreshingData(this WitConfiguration configuration)
        {
            string appID = GetAppID(configuration);
            return IsRefreshing(appID);
        }
        // Refreshes configuration data
        public static void RefreshData(this WitConfiguration configuration, Action<string> onRefreshComplete = null)
        {
            // Get refresh id
            string appID = GetAppID(configuration);
            if (string.IsNullOrEmpty(appID))
            {
                RefreshDataComplete(configuration, "Cannot refresh without application data", onRefreshComplete);
                return;
            }
            if (Application.isPlaying)
            {
                RefreshDataComplete(configuration, "Cannot refresh while playing", onRefreshComplete);
                return;
            }
            if (IsRefreshing(appID))
            {
                RefreshDataComplete(configuration, "Already Refreshing", onRefreshComplete);
                return;
            }
            if (!IsClientTokenValid(configuration.clientAccessToken))
            {
                RefreshDataComplete(configuration, "Invalid client token set", onRefreshComplete);
                return;
            }
            // Begin refresh
            refreshAppIDs.Add(appID);
            // Refresh application data
            configuration.application.witConfiguration = configuration;
            configuration.application.UpdateData(() =>
            {
                if (configuration != null)
                {
                    EditorUtility.SetDirty(configuration);
                    RefreshIntentsData(configuration, onRefreshComplete);
                }
            });
        }
        // Refresh intents data
        private static void RefreshIntentsData(WitConfiguration configuration, Action<string> onRefreshComplete)
        {
            PerformConfigRequest(configuration, configuration.ListIntentsRequest(), ApplyIntentList, (error) =>
            {
                if (!string.IsNullOrEmpty(error))
                {
                    RefreshDataComplete(configuration, error, onRefreshComplete);
                }
                else
                {
                    RefreshEntitiesData(configuration, onRefreshComplete);
                }
            });
        }
        // Refresh entities data
        private static void RefreshEntitiesData(WitConfiguration configuration, Action<string> onRefreshComplete)
        {
            PerformConfigRequest(configuration, configuration.ListEntitiesRequest(), ApplyEntityList, (error) =>
            {
                if (!string.IsNullOrEmpty(error))
                {
                    RefreshDataComplete(configuration, error, onRefreshComplete);
                }
                else
                {
                    RefreshTraitsData(configuration, onRefreshComplete);
                }
            });
        }
        // Refresh traits data
        private static void RefreshTraitsData(WitConfiguration configuration, Action<string> onRefreshComplete)
        {
            PerformConfigRequest(configuration, configuration.ListTraitsRequest(), ApplyTraitList, (error) =>
            {
                RefreshDataComplete(configuration, error, onRefreshComplete);
            });
        }
        // Refresh data complete
        private static void RefreshDataComplete(WitConfiguration configuration, string error, Action<string> onRefreshComplete)
        {
            // Get refresh id
            string appID = GetAppID(configuration);
            if (IsRefreshing(appID))
            {
                refreshAppIDs.Remove(appID);
            }
            // Failed
            if (!string.IsNullOrEmpty(error))
            {
                error = $"Refresh Configuration Failed\n{error}";
                Log(error, true);
            }
            // Success
            else
            {
                Log("Refresh Configuration Success", false);
            }
            // Invoke complete
            onRefreshComplete?.Invoke(error);
        }
        #endregion

        #region APPLICATION
        // Perform a configuration wit request and then apply configuration data
        private static void PerformConfigRequest(WitConfiguration configuration, WitRequest request, Action<WitConfiguration, WitResponseNode, Action<string>> onApply, Action<string> onComplete)
        {
            PerformRequest(request, (response, onRequestComplete) =>
            {
                onApply(configuration, response, onRequestComplete);
            }, onComplete);
        }
        // Perform a wit request and then apply data
        private static void PerformRequest(WitRequest request, Action<WitResponseNode, Action<string>> onApply, Action<string> onComplete)
        {
            // Add response delegate
            request.onResponse = (response) =>
            {
                // Get status
                int status = response.StatusCode;
                // Failed
                if (status != 200)
                {
                    onComplete($"Request Failed [{status}]: {response.StatusDescription}\nPath: {request}");
                }
                // Success
                else
                {
                    // Apply
                    onApply(response.ResponseData, (error) =>
                    {
                        // Apply failed
                        if (!string.IsNullOrEmpty(error))
                        {
                            onComplete?.Invoke($"Request Set Failed: {status}\nPath: {request}\nError: {error}");
                        }
                        // Complete
                        else
                        {
                            Log($"Request Success\nType: {request}", false);
                            onComplete?.Invoke("");
                        }
                    });
                }
            };

            // Perform
            Log($"Request Begin\nType: {request}", false);
            request.Request();
        }
        // Apply all application data
        private static void ApplyAllApplicationData(string serverToken, WitResponseNode witResponse, Action<string> onComplete)
        {
            var applications = witResponse.AsArray;
            for (int i = 0; i < applications.Count; i++)
            {
                // Get application
                var application = WitApplication.FromJson(applications[i]);
                string appID = application?.id;
                // Apply app server token if applicable
                if (applications[i]["is_app_for_token"].AsBool)
                {
                    WitAuthUtility.SetAppServerToken(appID, serverToken);
                }
                // Apply to configuration
                int witConfigIndex = Array.FindIndex(WitConfigs, (configuration) => string.Equals(appID, configuration?.application?.id));
                if (witConfigIndex != -1)
                {
                    WitConfiguration configuration = _witConfigs[witConfigIndex];
                    configuration.application = application;
                    EditorUtility.SetDirty(configuration);
                    configuration.RefreshData();
                }
            }
            onComplete("");
        }
        // Apply application data
        private static void ApplyApplicationData(WitConfiguration configuration, WitResponseNode witResponse, Action<string> onComplete)
        {
            var applications = witResponse.AsArray;
            for (int i = 0; i < applications.Count; i++)
            {
                if (applications[i]["is_app_for_token"].AsBool)
                {
                    if (configuration.application == null)
                    {
                        configuration.application = WitApplication.FromJson(applications[i]);
                    }
                    else
                    {
                        configuration.application.UpdateData(applications[i]);
                    }
                    configuration.application.witConfiguration = configuration;
                    EditorUtility.SetDirty(configuration);
                    onComplete?.Invoke("");
                    return;
                }
            }
            onComplete?.Invoke("No applicable configuration application found");
        }
        // Apply client id
        private static void ApplyClientToken(WitConfiguration configuration, WitResponseNode witResponse, Action<string> onComplete)
        {
            var token = witResponse?["client_token"];
            if (!string.IsNullOrEmpty(token))
            {
                configuration.clientAccessToken = token;
                EditorUtility.SetDirty(configuration);
            }
            onComplete?.Invoke("");
        }
        // Apply intents
        private static void ApplyIntentList(WitConfiguration configuration, WitResponseNode witResponse, Action<string> onComplete)
        {
            // Generate intent list
            var intentList = witResponse.AsArray;
            var n = intentList.Count;
            configuration.intents = new WitIntent[n];
            for (int i = 0; i < n; i++)
            {
                var intent = WitIntent.FromJson(intentList[i]);
                intent.witConfiguration = configuration;
                configuration.intents[i] = intent;
            }
            EditorUtility.SetDirty(configuration);
            // Update intents
            UpdateConfigItem(0, configuration.intents, configuration, onComplete);
        }
        // Apply entities
        private static void ApplyEntityList(WitConfiguration configuration, WitResponseNode witResponse, Action<string> onComplete)
        {
            // Generate entities list
            var entityList = witResponse.AsArray;
            var n = entityList.Count;
            configuration.entities = new WitEntity[n];
            for (int i = 0; i < n; i++)
            {
                var entity = WitEntity.FromJson(entityList[i]);
                entity.witConfiguration = configuration;
                configuration.entities[i] = entity;
            }
            EditorUtility.SetDirty(configuration);
            // Update entities
            UpdateConfigItem(0, configuration.entities, configuration, onComplete);
        }
        // Apply traits
        private static void ApplyTraitList(WitConfiguration configuration, WitResponseNode witResponse, Action<string> onComplete)
        {
            // Generate traits list
            var traitList = witResponse.AsArray;
            var n = traitList.Count;
            configuration.traits = new WitTrait[n];
            for (int i = 0; i < n; i++)
            {
                var trait = WitTrait.FromJson(traitList[i]);
                trait.witConfiguration = configuration;
                configuration.traits[i] = trait;
            }
            EditorUtility.SetDirty(configuration);
            // Update traits
            UpdateConfigItem(0, configuration.traits, configuration, onComplete);
        }
        // Update all
        private static void UpdateConfigItem(int index, WitConfigurationData[] items, WitConfiguration configuration, Action<string> onComplete)
        {
            // Complete
            if (index < 0 || index >= items.Length)
            {
                onComplete?.Invoke("");
                return;
            }
            // Update item
            WitConfigurationData item = items[index];
            item.UpdateData(() =>
            {
                Log($"{item.GetType()} {index} Updated", false);
                EditorUtility.SetDirty(configuration);
                UpdateConfigItem(index + 1, items, configuration, onComplete);
            });
        }
        // Log
        private static void Log(string comment, bool error)
        {
            #if VERBOSE_LOG
            string l = "Wit Configuration Utility - " + comment;
            if (error)
            {
                Debug.LogError(l);
            }
            else
            {
                Debug.Log(l);
            }
            #endif
        }
        #endregion
    }
}
