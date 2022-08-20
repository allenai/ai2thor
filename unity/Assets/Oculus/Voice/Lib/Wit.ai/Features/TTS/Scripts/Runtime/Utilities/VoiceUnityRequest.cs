/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Facebook.WitAi.Utilities;

namespace Facebook.WitAi.TTS.Utilities
{
    public class VoiceUnityRequest
    {
        #region AUDIO
        // Request audio clip with url & ready delegate
        public static VoiceUnityRequest RequestAudioClip(string audioUrl, Action<string, AudioClip, string> onAudioClipReady) =>
            RequestAudioClip(audioUrl, AudioType.UNKNOWN, null, onAudioClipReady);
        // Request audio clip with url, progress delegate & ready delegate
        public static VoiceUnityRequest RequestAudioClip(string audioUrl, Action<string, float> onAudioClipProgress,
            Action<string, AudioClip, string> onAudioClipReady) => RequestAudioClip(audioUrl, AudioType.UNKNOWN,
            onAudioClipProgress, onAudioClipReady);
        // Request audio clip with url, type & ready delegate
        public static VoiceUnityRequest RequestAudioClip(string audioUrl, AudioType audioType,
            Action<string, AudioClip, string> onAudioClipReady) =>
            RequestAudioClip(audioUrl, audioType, null, onAudioClipReady);
        // Request audio clip with url, type, progress delegate & ready delegate
        public static VoiceUnityRequest RequestAudioClip(string audioUrl, AudioType audioType, Action<string, float> onAudioClipProgress, Action<string, AudioClip, string> onAudioClipReady)
        {
            // Attempt to determine audio type
            if (audioType == AudioType.UNKNOWN)
            {
                // Determine audio type from extension
                string audioExt = Path.GetExtension(audioUrl).Replace(".", "");
                if (!Enum.TryParse(audioExt, true, out audioType))
                {
                    onAudioClipReady?.Invoke(audioUrl, null, $"Unknown audio type\nExtension: {audioExt}");
                    return null;
                }
            }

            // Get url
            string finalUrl = audioUrl;
            // Add file:// if needed
            if (!audioUrl.StartsWith("http") && !audioUrl.StartsWith("file://"))
            {
                finalUrl = $"file://{audioUrl}";
            }

            // Audio clip request
            UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(finalUrl, audioType);

            // Stream audio
            ((DownloadHandlerAudioClip)request.downloadHandler).streamAudio = true;

            // Perform request
            return Request(request, (p) => onAudioClipProgress?.Invoke(audioUrl, p), (r) =>
            {
                // Error
                #if UNITY_2020_1_OR_NEWER
                if (r.result != UnityWebRequest.Result.Success)
                #else
                if (r.isHttpError)
                #endif
                {
                    onAudioClipReady?.Invoke(audioUrl, null, r.error);
                }
                // Handler
                else
                {
                    // Get clip
                    AudioClip clip = null;
                    try
                    {
                        clip = DownloadHandlerAudioClip.GetContent(r);
                    }
                    catch (Exception exception)
                    {
                        onAudioClipReady?.Invoke(audioUrl, null, $"Failed to decode audio clip\n{exception.ToString()}");
                        return;
                    }
                    // Still missing
                    if (clip == null)
                    {
                        onAudioClipReady?.Invoke(audioUrl, null, "Failed to decode audio clip");
                    }
                    // Success
                    else
                    {
                        clip.name = Path.GetFileNameWithoutExtension(audioUrl);
                        onAudioClipReady?.Invoke(audioUrl, clip, string.Empty);
                    }
                }
            });
        }
        #endregion

        #region FILE
        // Request a file
        public static VoiceUnityRequest RequestFile(string fileUrl, Action<string, UnityWebRequest> onFileLoaded) => RequestFile(fileUrl, null, onFileLoaded);
        public static VoiceUnityRequest RequestFile(string fileUrl, Action<string, float> onFileProgress, Action<string, UnityWebRequest> onFileLoaded)
        {
            // Generate get request
            UnityWebRequest request = UnityWebRequest.Get(fileUrl);
            // Perform request
            return Request(request, (p) => onFileProgress?.Invoke(fileUrl, p), (r) => onFileLoaded?.Invoke(fileUrl, r));
        }
        #endregion

        #region REQUESTS
        // Perform a request with a complete delegate
        public static VoiceUnityRequest Request(UnityWebRequest request, Action<UnityWebRequest> onComplete) => Request(request, null, onComplete);
        // Perform a request with a progress delegate & a complete delegate
        public static VoiceUnityRequest Request(UnityWebRequest unityRequest, Action<float> onProgress, Action<UnityWebRequest> onComplete)
        {
            // Get request
            VoiceUnityRequest request = new VoiceUnityRequest();

            // Load
            request.Setup(unityRequest, onProgress, onComplete);

            // Return request
            return request;
        }
        #endregion

        #region INSTANCE
        // Max requests
        private const int REQUEST_MAX = 2;
        // Currently transmitting requests
        private static int _requestCount = 0;

        /// <summary>
        /// Timeout in seconds
        /// </summary>
        public static int Timeout = 5;

        // Access for state
        public bool IsTransmitting => _transmitting;
        private bool _transmitting = false;
        public float Progress => _progress;
        private float _progress;

        // Internal refs & delegates
        private UnityWebRequest _request;
        private Action<float> _onProgress;
        private Action<UnityWebRequest> _onComplete;
        private CoroutineUtility.CoroutinePerformer _coroutine;

        // Request setup
        public virtual void Setup(UnityWebRequest newRequest, Action<float> newProgress, Action<UnityWebRequest> newComplete)
        {
            // Already setup
            if (_request != null)
            {
                return;
            }

            // Setup
            _request = newRequest;
            _onProgress = newProgress;
            _onComplete = newComplete;
            _transmitting = false;
            _progress = 0f;

            // Use default timeout
            if (newRequest.timeout <= 0)
            {
                newRequest.timeout = Timeout;
            }

            // Begin
            _coroutine = CoroutineUtility.StartCoroutine(PerformUpdate());
        }
        // Perform update
        protected virtual IEnumerator PerformUpdate()
        {
            // Continue while request exists
            while (_request != null && !_request.isDone)
            {
                // Wait
                yield return null;

                // Waiting to begin
                if (!_transmitting)
                {
                    // Can start
                    if (_requestCount < REQUEST_MAX)
                    {
                        _requestCount++;
                        Begin();
                    }
                }
                // Update progress
                else
                {
                    float newProgress = Mathf.Max(_request.downloadProgress, _request.uploadProgress);
                    if (_progress != newProgress)
                    {
                        _progress = newProgress;
                        _onProgress?.Invoke(_progress);
                    }
                }
            }
            // Complete
            Complete();
        }
        // Begin request
        protected virtual void Begin()
        {
            _transmitting = true;
            _progress = 0f;
            _onProgress?.Invoke(_progress);
            _request.SendWebRequest();
        }
        // Request complete
        protected virtual void Complete()
        {
            // Perform callback
            if (_transmitting && _request != null && _request.isDone)
            {
                _progress = 1f;
                _onProgress?.Invoke(_progress);
                _onComplete?.Invoke(_request);
            }

            // Unload
            Unload();
        }
        // Request destroy
        public virtual void Unload()
        {
            // Cancel coroutine
            if (_coroutine != null)
            {
                _coroutine.CoroutineCancel();
            }

            // Complete
            if (_transmitting)
            {
                _transmitting = false;
                _requestCount--;
            }

            // Remove delegates
            _onProgress = null;
            _onComplete = null;

            // Dispose
            if (_request != null)
            {
                _request.Dispose();
                _request = null;
            }
        }
        #endregion
    }
}
