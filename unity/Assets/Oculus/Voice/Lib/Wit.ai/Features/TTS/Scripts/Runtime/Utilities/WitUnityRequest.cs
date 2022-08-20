/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Facebook.WitAi.Configuration;
using Facebook.WitAi.Data.Configuration;

namespace Facebook.WitAi.TTS.Utilities
{
    public class WitUnityRequest : VoiceUnityRequest
    {
        #region TTS
        // Audio type for tts
        public static AudioType TTSAudioType = AudioType.WAV;
        // TTS End point
        public const string WIT_ENDPOINT_TTS = "synthesize";
        // TTS End point
        public const int TTS_MAX_SIZE = 140;

        // Return error with text to speak if any are found
        public static string IsTextValid(string textToSpeak)
        {
            // Ensure text exists
            if (string.IsNullOrEmpty(textToSpeak))
            {
                return "No text provided";
            }
            // Ensure text is not too long
            if (textToSpeak.Length > WitUnityRequest.TTS_MAX_SIZE)
            {
                return $"Text must be less than {WitUnityRequest.TTS_MAX_SIZE} characters";
            }
            // Success
            return string.Empty;
        }

        // Request a TTS service stream
        public static WitUnityRequest RequestTTSStream(WitConfiguration configuration, string textToSpeak,
            Dictionary<string, string> data, Action<float> onProgress, Action<AudioClip, string> onClipReady)
        {
            return RequestTTS(configuration, textToSpeak, data, (response, uri) =>
            {
                DownloadHandlerAudioClip streamHandler = new DownloadHandlerAudioClip(uri, TTSAudioType);
                streamHandler.compressed = true;
                streamHandler.streamAudio = true;
                response.downloadHandler = streamHandler;
                response.disposeDownloadHandlerOnDispose = true;
            }, onProgress, (response, error) =>
            {
                // Failed
                if (!string.IsNullOrEmpty(error))
                {
                    onClipReady?.Invoke(null, error);
                }
                // Success
                else
                {
                    // Get clip
                    AudioClip clip = null;
                    try
                    {
                        clip = DownloadHandlerAudioClip.GetContent(response);
                    }
                    catch (Exception exception)
                    {
                        // Exception
                        onClipReady?.Invoke(null, $"Failed to decode audio clip\n{exception.ToString()}");
                        return;
                    }

                    // Not found
                    if (clip == null)
                    {
                        onClipReady?.Invoke(null, "Failed to decode audio clip");
                    }
                    // Success
                    else
                    {
                        clip.name = "TTS_CLIP";
                        onClipReady?.Invoke(clip, string.Empty);
                    }
                }
            });
        }

        // Request a TTS service download
        public static WitUnityRequest RequestTTSDownload(string downloadPath, WitConfiguration configuration, string textToSpeak,
            Dictionary<string, string> data, Action<float> onProgress, Action<string> onDownloadComplete)
        {
            // Download to temp path
            string tempDownloadPath = downloadPath + ".tmp";
            if (File.Exists(tempDownloadPath))
            {
                File.Delete(tempDownloadPath);
            }

            // Request file
            return RequestTTS(configuration, textToSpeak, data, (response, uri) =>
            {
                DownloadHandlerFile fileHandler = new DownloadHandlerFile(tempDownloadPath, true);
                response.downloadHandler = fileHandler;
                response.disposeDownloadHandlerOnDispose = true;
            }, onProgress, (response, error) =>
            {
                // If file found
                try
                {
                    if (System.IO.File.Exists(tempDownloadPath))
                    {
                        // For error, remove
                        if (!string.IsNullOrEmpty(error))
                        {
                            System.IO.File.Delete(tempDownloadPath);
                        }
                        // For success, move to final path
                        else
                        {
                            System.IO.File.Move(tempDownloadPath, downloadPath);
                        }
                    }
                }
                catch (Exception exception)
                {
                    error = exception.ToString();
                    Debug.LogError($"Moving File Failed\nFrom: {tempDownloadPath}\nTo: {downloadPath}\nError: {error}");
                }
                onDownloadComplete?.Invoke(error);
            });
        }

        // Request tts
        private static WitUnityRequest RequestTTS(WitConfiguration configuration, string textToSpeak, Dictionary<string, string> data, Action<UnityWebRequest, Uri> onSetup, Action<float> onProgress, Action<UnityWebRequest, string> onComplete)
        {
            // Failure
            if (configuration == null)
            {
                onComplete?.Invoke(null, "TTS Request Failed\nNo wit configuration provided");
                return null;
            }
            // Check text
            string textError = IsTextValid(textToSpeak);
            if (!string.IsNullOrEmpty(textError))
            {
                onComplete?.Invoke(null, $"TTS Request Failed\n{textError}");
                return null;
            }

            // Get uri & data
            Uri ttsUri = GetUri(configuration, WIT_ENDPOINT_TTS, null);
            data["q"] = textToSpeak;

            // Parse into json
            string ttsJson = string.Empty;
            foreach (var key in data.Keys)
            {
                if (!string.IsNullOrEmpty(ttsJson))
                {
                    ttsJson += ",";
                }
                ttsJson += $"\"{key}\":\"{data[key].Replace("\"", "\\\"")}\"";
            }
            ttsJson = "{" + ttsJson + "}";
            byte[] jsonBytes = Encoding.UTF8.GetBytes(ttsJson);

            // Generate post
            UnityWebRequest request = new UnityWebRequest(ttsUri, UnityWebRequest.kHttpVerbPOST);
            request.SetRequestHeader("Content-type", "application/json");
            request.SetRequestHeader("Accept", $"audio/{TTSAudioType.ToString().ToLower()}");
            request.uploadHandler = new UploadHandlerRaw(jsonBytes);

            // Handle setup
            onSetup(request, ttsUri);

            // Perform request
            return RequestWit(configuration, request, onProgress, (r) =>
            {
                // Error
                if (!string.IsNullOrEmpty(r.error))
                {
                    onComplete?.Invoke(null, r.error);
                }
                // Handler
                else
                {
                    onComplete.Invoke(r, r.error);
                }
            });
        }

        // TTS End point
        public const string WIT_ENDPOINT_TTS_VOICES = "voices";
        // Request tts voices
        public static WitUnityRequest RequestTTSVoices(WitConfiguration configuration, Action<float> onProgress, Action<string, string> onComplete)
        {
            // Failure
            if (configuration == null)
            {
                onComplete?.Invoke(null, "TTS Request Failed\nNo wit configuration provided");
                return null;
            }

            // Get uri & data
            Uri ttsUri = GetUri(configuration, WIT_ENDPOINT_TTS_VOICES, null);

            // Generate post
            UnityWebRequest request = UnityWebRequest.Get(ttsUri);
            request.SetRequestHeader("Content-type", "application/json");

            // Perform request
            return RequestWit(configuration, request, onProgress, (r) =>
            {
                // Error
                if (!string.IsNullOrEmpty(r.error))
                {
                    onComplete?.Invoke(null, r.error);
                }
                // Handler
                else
                {
                    onComplete.Invoke(r.downloadHandler.text, r.error);
                }
            });
        }
        #endregion

        #region SHARED
        // Provide custom headers
        public static event Func<UriBuilder,Uri> OnProvideCustomUri;
        public static event Func<Dictionary<string, string>> OnProvideCustomHeaders;

        // Get uri
        private static Uri GetUri(WitConfiguration configuration, string path, Dictionary<string, string> queryParams)
        {
            // Uri builder
            UriBuilder uriBuilder = new UriBuilder();

            // Append endpoint data
            var endpointConfig = WitEndpointConfig.GetEndpointConfig(configuration);
            uriBuilder.Scheme = endpointConfig.UriScheme;
            uriBuilder.Host = endpointConfig.Authority;
            if (endpointConfig.Port > 0)
            {
                uriBuilder.Port = endpointConfig.Port;
            }

            // Set path
            uriBuilder.Path = path;

            // Build query
            uriBuilder.Query = $"v={endpointConfig.WitApiVersion}";
            if (queryParams != null)
            {
                foreach (string key in queryParams.Keys)
                {
                    string val = Uri.EscapeDataString(queryParams[key]);
                    uriBuilder.Query += $"&{key}={val}";
                }
            }

            // Return custom uri
            if (OnProvideCustomUri != null)
            {
                return OnProvideCustomUri(uriBuilder);
            }

            // Return uri
            return uriBuilder.Uri;
        }
        // Apply headers
        private static void ApplyHeaders(WitConfiguration configuration, UnityWebRequest unityRequest)
        {
            // Set authorization
            unityRequest.SetRequestHeader("Authorization", GetAuthorization(configuration));
            // Set user agent
            unityRequest.SetRequestHeader("User-Agent", WitRequest.GetUserAgent(configuration));
            // Set timeout
            unityRequest.timeout = configuration ? Mathf.CeilToInt(configuration.timeoutMS / 1000f) : 10;
            // Set custom headers
            if (OnProvideCustomHeaders != null)
            {
                foreach (Func<Dictionary<string, string>> del in OnProvideCustomHeaders.GetInvocationList())
                {
                    Dictionary<string, string> customHeaders = del();
                    if (customHeaders != null)
                    {
                        foreach (var key in customHeaders.Keys)
                        {
                            unityRequest.SetRequestHeader(key, customHeaders[key]);
                        }
                    }
                }
            }
        }
        // Get config authorization
        private static string GetAuthorization(WitConfiguration configuration)
        {
            return $"Bearer {configuration.clientAccessToken.Trim()}";
        }
        // Generate wit request
        protected static WitUnityRequest RequestWit(WitConfiguration configuration, UnityWebRequest unityRequest, Action<float> onProgress,
            Action<UnityWebRequest> onComplete)
        {
            // Get request
            WitUnityRequest request = new WitUnityRequest();

            // Add all headers
            ApplyHeaders(configuration, unityRequest);

            // Setup request
            request.Setup(unityRequest, onProgress, onComplete);

            // Return request
            return request;
        }
        #endregion
    }
}
