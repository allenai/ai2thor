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
using UnityEngine;
using Facebook.WitAi.TTS.Data;
using Facebook.WitAi.TTS.Events;
using Facebook.WitAi.TTS.Interfaces;
using Facebook.WitAi.TTS.Utilities;

namespace Facebook.WitAi.TTS.Integrations
{
    public class TTSDiskCache : MonoBehaviour, ITTSDiskCacheHandler
    {
        [Header("Disk Cache Settings")]
        /// <summary>
        /// The relative path from the DiskCacheLocation in TTSDiskCacheSettings
        /// </summary>
        [SerializeField] private string _diskPath = "TTS/";
        public string DiskPath => _diskPath;

        /// <summary>
        /// The cache default settings
        /// </summary>
        [SerializeField] private TTSDiskCacheSettings _defaultSettings = new TTSDiskCacheSettings();
        public TTSDiskCacheSettings DiskCacheDefaultSettings => _defaultSettings;

        /// <summary>
        /// The cache streaming events
        /// </summary>
        [SerializeField] private TTSStreamEvents _events = new TTSStreamEvents();
        public TTSStreamEvents DiskStreamEvents
        {
            get => _events;
            set { _events = value; }
        }

        // All currently performing stream requests
        private Dictionary<string, VoiceUnityRequest> _streamRequests = new Dictionary<string, VoiceUnityRequest>();

        /// <summary>
        /// Builds full cache path
        /// </summary>
        /// <param name="clipData"></param>
        /// <returns></returns>
        public string GetDiskCachePath(TTSClipData clipData)
        {
            // Disabled
            if (!ShouldCacheToDisk(clipData))
            {
                return string.Empty;
            }

            // Get directory path
            string directory = string.Empty;
            switch (clipData.diskCacheSettings.DiskCacheLocation)
            {
                case TTSDiskCacheLocation.Persistent:
                    directory = Application.persistentDataPath;
                    break;
                case TTSDiskCacheLocation.Temporary:
                    directory = Application.temporaryCachePath;
                    break;
                case TTSDiskCacheLocation.Preload:
                    directory = Application.streamingAssetsPath;
                    break;
            }
            if (string.IsNullOrEmpty(directory))
            {
                return string.Empty;
            }

            // Generate root directory if needed
            if (!Directory.Exists(directory))
            {
                try
                {
                    Directory.CreateDirectory(directory);
                }
                catch (Exception e)
                {
                    Debug.LogError($"TTS Cache - Failed to create root directory\nPath: {directory}\n{e}");
                    return string.Empty;
                }
            }

            // Add tts cache path & clean
            directory += "/" + DiskPath;
            directory = directory.Replace("\\", "/");
            if (!directory.EndsWith("/"))
            {
                directory += "/";
            }

            // Generate tts directory if possible
            if (!Directory.Exists(directory))
            {
                try
                {
                    Directory.CreateDirectory(directory);
                }
                catch (Exception e)
                {
                    Debug.LogError($"TTS Cache - Failed to create tts directory\nPath: {directory}\n{e}");
                    return string.Empty;
                }
            }

            // Return clip path
            return $"{directory}{clipData.clipID}.{clipData.audioType.ToString().ToLower()}";
        }

        /// <summary>
        /// Determine if should cache to disk or not
        /// </summary>
        /// <param name="clipData">All clip data</param>
        /// <returns>Returns true if should cache to disk</returns>
        public bool ShouldCacheToDisk(TTSClipData clipData)
        {
            return clipData != null && clipData.diskCacheSettings.DiskCacheLocation != TTSDiskCacheLocation.Stream && !string.IsNullOrEmpty(clipData.clipID);
        }

        /// <summary>
        /// Determines if file is cached on disk
        /// </summary>
        /// <param name="clipData">Request data</param>
        /// <returns>True if file is on disk</returns>
        public bool IsCachedToDisk(TTSClipData clipData)
        {
            // Get path
            string cachePath = GetDiskCachePath(clipData);
            if (string.IsNullOrEmpty(cachePath))
            {
                return false;
            }
            // Check if file exists
            return File.Exists(cachePath);
        }

        /// <summary>
        /// Performs async load request
        /// </summary>
        public void StreamFromDiskCache(TTSClipData clipData)
        {
            // Invoke begin
            DiskStreamEvents?.OnStreamBegin?.Invoke(clipData);

            // Get file path
            string filePath = GetDiskCachePath(clipData);
            if (!File.Exists(filePath))
            {
                string e = $"Clip not found\nPath: {filePath}";
                OnStreamComplete(clipData, e);
                return;
            }

            // Load clip async
            _streamRequests[clipData.clipID] = VoiceUnityRequest.RequestAudioClip(filePath, (path, progress) => clipData.loadProgress = progress, (path, clip, error) =>
            {
                // Apply clip
                clipData.clip = clip;
                // Call on complete
                OnStreamComplete(clipData, error);
            });
        }
        /// <summary>
        /// Cancels unity request
        /// </summary>
        public void CancelDiskCacheStream(TTSClipData clipData)
        {
            // Ignore if not currently streaming
            if (!_streamRequests.ContainsKey(clipData.clipID))
            {
                return;
            }

            // Get request
            VoiceUnityRequest request = _streamRequests[clipData.clipID];
            _streamRequests.Remove(clipData.clipID);

            // Destroy immediately
            if (request != null)
            {
                request.Unload();
            }

            // Call cancel
            DiskStreamEvents?.OnStreamCancel?.Invoke(clipData);
        }
        // On stream completion
        protected virtual void OnStreamComplete(TTSClipData clipData, string error)
        {
            // Ignore if not currently streaming
            if (!_streamRequests.ContainsKey(clipData.clipID))
            {
                return;
            }

            // Remove from list
            _streamRequests.Remove(clipData.clipID);

            // Error
            if (!string.IsNullOrEmpty(error))
            {
                DiskStreamEvents?.OnStreamError?.Invoke(clipData, error);
            }
            // Success
            else
            {
                DiskStreamEvents?.OnStreamReady?.Invoke(clipData);
            }
        }
    }
}
