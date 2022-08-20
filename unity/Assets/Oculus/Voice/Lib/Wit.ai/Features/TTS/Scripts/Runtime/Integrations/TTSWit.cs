/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Facebook.WitAi.Data.Configuration;
using Facebook.WitAi.TTS.Data;
using Facebook.WitAi.TTS.Events;
using Facebook.WitAi.TTS.Interfaces;
using Facebook.WitAi.TTS.Utilities;
using UnityEngine.Serialization;

namespace Facebook.WitAi.TTS.Integrations
{
    [Serializable]
    public class TTSWitVoiceSettings : TTSVoiceSettings
    {
        // Attributes
        public string voice;
        public string style;
        [Range(50, 200)]
        public int speed = 100;
        [Range(25, 400)]
        public int pitch = 100;
        [Range(1, 100)]
        public int gain = 50;
    }
    [Serializable]
    public struct TTSWitRequestSettings
    {
        public WitConfiguration configuration;
    }

    public class TTSWit : TTSService, ITTSVoiceProvider, ITTSWebHandler
    {
        #region TTSService
        // Voice provider
        public override ITTSVoiceProvider VoiceProvider => this;
        // Request handler
        public override ITTSWebHandler WebHandler => this;
        // Runtime cache handler
        public override ITTSRuntimeCacheHandler RuntimeCacheHandler
        {
            get
            {
                if (_runtimeCache == null)
                {
                    _runtimeCache = gameObject.GetComponent<ITTSRuntimeCacheHandler>();
                }
                return _runtimeCache;
            }
        }
        private ITTSRuntimeCacheHandler _runtimeCache;
        // Cache handler
        public override ITTSDiskCacheHandler DiskCacheHandler
        {
            get
            {
                if (_diskCache == null)
                {
                    _diskCache = gameObject.GetComponent<ITTSDiskCacheHandler>();
                }
                return _diskCache;
            }
        }
        private ITTSDiskCacheHandler _diskCache;
        #endregion

        #region ITTSWebHandler Streams
        // Request settings
        [Header("Web Request Settings")]
        [FormerlySerializedAs("_settings")]
        public TTSWitRequestSettings RequestSettings;

        // Use settings web stream events
        public TTSStreamEvents WebStreamEvents { get; set; } = new TTSStreamEvents();

        // Requests bly clip id
        private Dictionary<string, WitUnityRequest> _webStreams = new Dictionary<string, WitUnityRequest>();

        // Ensures text can be sent to wit web service
        public string IsTextValid(string textToSpeak) => WitUnityRequest.IsTextValid(textToSpeak);

        /// <summary>
        /// Method for performing a web load request
        /// </summary>
        /// <param name="clipData">Clip request data</param>
        /// <param name="onStreamSetupComplete">Stream setup complete: returns clip and error if applicable</param>
        public void RequestStreamFromWeb(TTSClipData clipData)
        {
            // Stream begin
            WebStreamEvents?.OnStreamBegin?.Invoke(clipData);

            // Check if valid
            string validError = IsRequestValid(clipData, RequestSettings.configuration);
            if (!string.IsNullOrEmpty(validError))
            {
                WebStreamEvents?.OnStreamError?.Invoke(clipData, validError);
                return;
            }
            // Ignore if already performing
            if (_webStreams.ContainsKey(clipData.clipID))
            {
                CancelWebStream(clipData);
            }

            // Request tts
            _webStreams[clipData.clipID] = WitUnityRequest.RequestTTSStream(RequestSettings.configuration,
                clipData.textToSpeak, clipData.queryParameters,
                (progress) => clipData.loadProgress = progress,
                (clip, error) =>
                {
                    _webStreams.Remove(clipData.clipID);
                    clipData.clip = clip;
                    if (string.IsNullOrEmpty(error))
                    {
                        WebStreamEvents?.OnStreamReady?.Invoke(clipData);
                    }
                    else
                    {
                        WebStreamEvents?.OnStreamError?.Invoke(clipData, error);
                    }
                });
        }
        /// <summary>
        /// Cancel web stream
        /// </summary>
        /// <param name="clipID">Unique clip id</param>
        public bool CancelWebStream(TTSClipData clipData)
        {
            // Ignore without
            if (!_webStreams.ContainsKey(clipData.clipID))
            {
                return false;
            }

            // Get request
            WitUnityRequest request = _webStreams[clipData.clipID];
            _webStreams.Remove(clipData.clipID);

            // Destroy immediately
            request?.Unload();

            // Call delegate
            WebStreamEvents?.OnStreamCancel?.Invoke(clipData);

            // Success
            return true;
        }
        #endregion

        #region ITTSWebHandler Downloads
        // Use settings web download events
        public TTSDownloadEvents WebDownloadEvents { get; set; } = new TTSDownloadEvents();

        // Requests by clip id
        private Dictionary<string, WitUnityRequest> _webDownloads = new Dictionary<string, WitUnityRequest>();

        /// <summary>
        /// Method for performing a web load request
        /// </summary>
        /// <param name="clipData">Clip request data</param>
        /// <param name="downloadPath">Path to save clip</param>
        public void RequestDownloadFromWeb(TTSClipData clipData, string downloadPath)
        {
            // Begin
            WebDownloadEvents?.OnDownloadBegin?.Invoke(clipData, downloadPath);

            // Ensure valid
            string validError = IsRequestValid(clipData, RequestSettings.configuration);
            if (!string.IsNullOrEmpty(validError))
            {
                WebDownloadEvents?.OnDownloadError?.Invoke(clipData, downloadPath, validError);
                return;
            }
            // Abort if already performing
            if (_webDownloads.ContainsKey(clipData.clipID))
            {
                CancelWebDownload(clipData, downloadPath);
            }

            // Request tts
            _webDownloads[clipData.clipID] = WitUnityRequest.RequestTTSDownload(downloadPath,
                RequestSettings.configuration, clipData.textToSpeak, clipData.queryParameters,
                (progress) => clipData.loadProgress = progress,
                (error) =>
                {
                    _webDownloads.Remove(clipData.clipID);
                    if (string.IsNullOrEmpty(error))
                    {
                        WebDownloadEvents?.OnDownloadSuccess?.Invoke(clipData, downloadPath);
                    }
                    else
                    {
                        WebDownloadEvents?.OnDownloadError?.Invoke(clipData, downloadPath, error);
                    }
                });
        }
        /// <summary>
        /// Method for cancelling a running load request
        /// </summary>
        /// <param name="clipData">Clip request data</param>
        public bool CancelWebDownload(TTSClipData clipData, string downloadPath)
        {
            // Ignore if not performing
            if (!_webDownloads.ContainsKey(clipData.clipID))
            {
                return false;
            }

            // Get request
            WitUnityRequest request = _webDownloads[clipData.clipID];
            _webDownloads.Remove(clipData.clipID);

            // Destroy immediately
            request?.Unload();

            // Download cancelled
            WebDownloadEvents?.OnDownloadCancel?.Invoke(clipData, downloadPath);

            // Success
            return true;
        }
        #endregion

        #region ITTSVoiceProvider
        // Preset voice settings
        [Header("Voice Settings")]
        [SerializeField] private TTSWitVoiceSettings[] _presetVoiceSettings;
        public TTSWitVoiceSettings[] PresetWitVoiceSettings => _presetVoiceSettings;

        // Cast to voice array
        public TTSVoiceSettings[] PresetVoiceSettings
        {
            get
            {
                if (_presetVoiceSettings == null || _presetVoiceSettings.Length == 0)
                {
                    _presetVoiceSettings = new TTSWitVoiceSettings[1];
                    _presetVoiceSettings[0] = new TTSWitVoiceSettings
                    {
                        settingsID = "DEFAULT",
                        voice = "Charlie",
                        style = "default",
                        speed = 100,
                        pitch = 100,
                        gain = 50
                    };
                }
                return _presetVoiceSettings;
            }
        }
        // Default voice setting uses the first voice in the list
        public TTSVoiceSettings VoiceDefaultSettings => PresetVoiceSettings[0];

        // Convert voice settings into dictionary to be used with web requests
        public Dictionary<string, string> EncodeVoiceSettings(TTSVoiceSettings voiceSettings)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            if (voiceSettings != null)
            {
                foreach (FieldInfo field in voiceSettings.GetType().GetFields())
                {
                    if (!string.Equals(field.Name, "settingsID", StringComparison.CurrentCultureIgnoreCase))
                    {
                        // Get field value
                        object fieldVal = field.GetValue(voiceSettings);

                        // Clamp in between range
                        RangeAttribute range = field.GetCustomAttribute<RangeAttribute>();
                        if (range != null && field.FieldType == typeof(int))
                        {
                            int oldFloat = (int) fieldVal;
                            int newFloat = Mathf.Clamp(oldFloat, (int)range.min, (int)range.max);
                            if (oldFloat != newFloat)
                            {
                                fieldVal = newFloat;
                            }
                        }

                        // Apply
                        parameters[field.Name] = fieldVal.ToString();
                    }
                }
            }
            return parameters;
        }
        // Returns an error if request is not valid
        private string IsRequestValid(TTSClipData clipData, WitConfiguration configuration)
        {
            // Invalid clip
            if (clipData == null)
            {
                return "No clip data provided";
            }
            // Invalid configuration
            if (RequestSettings.configuration == null ||
                string.IsNullOrEmpty(RequestSettings.configuration.clientAccessToken))
            {
                return "No wit configuration provided";
            }
            // Success
            return string.Empty;
        }
        #endregion
    }
}
