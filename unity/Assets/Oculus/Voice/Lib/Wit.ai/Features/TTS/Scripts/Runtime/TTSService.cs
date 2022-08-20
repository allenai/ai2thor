/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Facebook.WitAi.TTS.Data;
using Facebook.WitAi.TTS.Events;
using Facebook.WitAi.TTS.Interfaces;
using Facebook.WitAi.Utilities;

namespace Facebook.WitAi.TTS
{
    public abstract class TTSService : MonoBehaviour
    {
        #region SETUP
        // Accessor
        public static TTSService Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Get all services
                    TTSService[] services = Resources.FindObjectsOfTypeAll<TTSService>();
                    if (services != null)
                    {
                        // Set as first instance that isn't a prefab
                        _instance = Array.Find(services, (o) => o.gameObject.scene.rootCount != 0);
                    }
                    // Not found
                    if (Application.isPlaying && _instance == null)
                    {
                        Debug.LogError("TTS Service - No Service found in scene");
                    }
                }
                return _instance;
            }
        }
        private static TTSService _instance;

        // Handles TTS runtime cache
        public abstract ITTSRuntimeCacheHandler RuntimeCacheHandler { get; }
        // Handles TTS cache requests
        public abstract ITTSDiskCacheHandler DiskCacheHandler { get; }
        // Handles TTS web requests
        public abstract ITTSWebHandler WebHandler { get; }
        // Handles TTS voice presets
        public abstract ITTSVoiceProvider VoiceProvider { get; }

        // Handles TTS events
        public TTSServiceEvents Events => _events;
        [Header("Event Settings")]
        [SerializeField] private TTSServiceEvents _events = new TTSServiceEvents();

        // Set instance
        protected virtual void Awake()
        {
            // Set instance
            _instance = this;
            _delegates = false;
        }
        // Remove delegates
        protected virtual void OnDisable()
        {
            RemoveDelegates();
        }
        // Add delegates
        private bool _delegates = false;
        protected virtual void AddDelegates()
        {
            // Ignore if already added
            if (_delegates)
            {
                return;
            }
            _delegates = true;

            if (RuntimeCacheHandler != null)
            {
                RuntimeCacheHandler.OnClipAdded.AddListener(OnRuntimeClipAdded);
                RuntimeCacheHandler.OnClipRemoved.AddListener(OnRuntimeClipRemoved);
            }
            if (DiskCacheHandler != null)
            {
                DiskCacheHandler.DiskStreamEvents.OnStreamBegin.AddListener(OnStreamBegin);
                DiskCacheHandler.DiskStreamEvents.OnStreamCancel.AddListener(OnStreamCancel);
                DiskCacheHandler.DiskStreamEvents.OnStreamReady.AddListener(OnStreamReady);
                DiskCacheHandler.DiskStreamEvents.OnStreamError.AddListener(OnStreamError);
            }
            if (WebHandler != null)
            {
                WebHandler.WebStreamEvents.OnStreamBegin.AddListener(OnStreamBegin);
                WebHandler.WebStreamEvents.OnStreamCancel.AddListener(OnStreamCancel);
                WebHandler.WebStreamEvents.OnStreamReady.AddListener(OnStreamReady);
                WebHandler.WebStreamEvents.OnStreamError.AddListener(OnStreamError);
                WebHandler.WebDownloadEvents.OnDownloadBegin.AddListener(OnWebDownloadBegin);
                WebHandler.WebDownloadEvents.OnDownloadCancel.AddListener(OnWebDownloadCancel);
                WebHandler.WebDownloadEvents.OnDownloadSuccess.AddListener(OnWebDownloadSuccess);
                WebHandler.WebDownloadEvents.OnDownloadError.AddListener(OnWebDownloadError);
            }
        }
        // Remove delegates
        protected virtual void RemoveDelegates()
        {
            // Ignore if not yet added
            if (!_delegates)
            {
                return;
            }
            _delegates = false;

            if (RuntimeCacheHandler != null)
            {
                RuntimeCacheHandler.OnClipAdded.RemoveListener(OnRuntimeClipAdded);
                RuntimeCacheHandler.OnClipRemoved.RemoveListener(OnRuntimeClipRemoved);
            }
            if (DiskCacheHandler != null)
            {
                DiskCacheHandler.DiskStreamEvents.OnStreamBegin.RemoveListener(OnStreamBegin);
                DiskCacheHandler.DiskStreamEvents.OnStreamCancel.RemoveListener(OnStreamCancel);
                DiskCacheHandler.DiskStreamEvents.OnStreamReady.RemoveListener(OnStreamReady);
                DiskCacheHandler.DiskStreamEvents.OnStreamError.RemoveListener(OnStreamError);
            }
            if (WebHandler != null)
            {
                WebHandler.WebStreamEvents.OnStreamBegin.RemoveListener(OnStreamBegin);
                WebHandler.WebStreamEvents.OnStreamCancel.RemoveListener(OnStreamCancel);
                WebHandler.WebStreamEvents.OnStreamReady.RemoveListener(OnStreamReady);
                WebHandler.WebStreamEvents.OnStreamError.RemoveListener(OnStreamError);
                WebHandler.WebDownloadEvents.OnDownloadBegin.RemoveListener(OnWebDownloadBegin);
                WebHandler.WebDownloadEvents.OnDownloadCancel.RemoveListener(OnWebDownloadCancel);
                WebHandler.WebDownloadEvents.OnDownloadSuccess.RemoveListener(OnWebDownloadSuccess);
                WebHandler.WebDownloadEvents.OnDownloadError.RemoveListener(OnWebDownloadError);
            }
        }
        // Remove instance
        protected virtual void OnDestroy()
        {
            // Remove instance
            if (_instance == this)
            {
                _instance = null;
            }
            // Abort & unload all
            UnloadAll();
        }
        /// <summary>
        /// Logs for TTSService
        /// </summary>
        protected virtual void Log(string logMessage, LogType logType = LogType.Log)
        {
#if UNITY_EDITOR
            string logFinal = $"{GetType().Name} {logType.ToString()} - {logMessage}";
            if (logType == LogType.Error)
            {
                Debug.LogError(logFinal);
            }
            else if (logType == LogType.Warning)
            {
                Debug.LogWarning(logFinal);
            }
#endif
        }
        #endregion

        #region HELPERS
        /// <summary>
        /// Obtain unique id for clip data
        /// </summary>
        private const string CLIP_ID_DELIM = "|";
        public virtual string GetClipID(string textToSpeak, TTSVoiceSettings voiceSettings)
        {
            // Get a text string for a unique id
            StringBuilder uniqueID = new StringBuilder();
            // Add all data items
            if (VoiceProvider != null)
            {
                Dictionary<string, string> data = VoiceProvider.EncodeVoiceSettings(voiceSettings);
                foreach (var key in data.Keys)
                {
                    string keyClean = data[key].ToLower().Replace(CLIP_ID_DELIM, "");
                    uniqueID.Append(keyClean);
                    uniqueID.Append(CLIP_ID_DELIM);
                }
            }
            // Finally, add unique id
            uniqueID.Append(textToSpeak.ToLower());
            // Return id
            return GetSha256Hash(CLIP_HASH, uniqueID.ToString());
        }
        private readonly SHA256 CLIP_HASH = SHA256.Create();
        private string GetSha256Hash(SHA256 shaHash, string input)
        {
            // Convert the input string to a byte array and compute the hash.
            byte[] data = shaHash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
        /// <summary>
        /// Creates new clip data or returns existing cached clip
        /// </summary>
        /// <param name="textToSpeak">Text to speak</param>
        /// <param name="clipID">Unique clip id</param>
        /// <param name="voiceSettings">Voice settings</param>
        /// <param name="diskCacheSettings">Disk Cache settings</param>
        /// <returns>Clip data structure</returns>
        protected virtual TTSClipData CreateClipData(string textToSpeak, string clipID, TTSVoiceSettings voiceSettings,
            TTSDiskCacheSettings diskCacheSettings)
        {
            // Use default voice settings if none are set
            if (voiceSettings == null && VoiceProvider != null)
            {
                voiceSettings = VoiceProvider.VoiceDefaultSettings;
            }
            // Use default disk cache settings if none are set
            if (diskCacheSettings == null && DiskCacheHandler != null)
            {
                diskCacheSettings = DiskCacheHandler.DiskCacheDefaultSettings;
            }
            // Determine clip id if empty
            if (string.IsNullOrEmpty(clipID))
            {
                clipID = GetClipID(textToSpeak, voiceSettings);
            }

            // Get clip from runtime cache if applicable
            TTSClipData clipData = GetRuntimeCachedClip(clipID);
            if (clipData != null)
            {
                return clipData;
            }

            // Generate new clip data
            clipData = new TTSClipData()
            {
                clipID = clipID,
                textToSpeak = textToSpeak,
                voiceSettings = voiceSettings,
                diskCacheSettings = diskCacheSettings,
                loadState = TTSClipLoadState.Unloaded,
                loadProgress = 0f,
                queryParameters = VoiceProvider?.EncodeVoiceSettings(voiceSettings)
            };

            // Return generated clip
            return clipData;
        }
        // Set clip state
        protected virtual void SetClipLoadState(TTSClipData clipData, TTSClipLoadState loadState)
        {
            clipData.loadState = loadState;
            clipData.onStateChange?.Invoke(clipData, clipData.loadState);
        }
        #endregion

        #region LOAD
        // Cancel warning
        public const string CANCEL_WARNING = "Canceled";

        // TTS Request options
        public TTSClipData Load(string textToSpeak, Action<TTSClipData, string> onStreamReady = null) => Load(textToSpeak, null, null, null, onStreamReady);
        public TTSClipData Load(string textToSpeak, string presetVoiceId, Action<TTSClipData, string> onStreamReady = null) => Load(textToSpeak, null, GetPresetVoiceSettings(presetVoiceId), null, onStreamReady);
        public TTSClipData Load(string textToSpeak, string presetVoiceId, TTSDiskCacheSettings diskCacheSettings, Action<TTSClipData, string> onStreamReady = null) => Load(textToSpeak, null, GetPresetVoiceSettings(presetVoiceId), diskCacheSettings, onStreamReady);
        public TTSClipData Load(string textToSpeak, TTSVoiceSettings voiceSettings, TTSDiskCacheSettings diskCacheSettings, Action<TTSClipData, string> onStreamReady = null) => Load(textToSpeak, null, voiceSettings, diskCacheSettings, onStreamReady);

        /// <summary>
        /// Perform a request for a TTS audio clip
        /// </summary>
        /// <param name="textToSpeak">Text to be spoken in clip</param>
        /// <param name="clipID">Unique clip id</param>
        /// <param name="voiceSettings">Custom voice settings</param>
        /// <param name="diskCacheSettings">Custom cache settings</param>
        /// <returns>Generated TTS clip data</returns>
        public virtual TTSClipData Load(string textToSpeak, string clipID, TTSVoiceSettings voiceSettings,
            TTSDiskCacheSettings diskCacheSettings, Action<TTSClipData, string> onStreamReady)
        {
            // Add delegates if needed
            AddDelegates();

            // Get clip data
            TTSClipData clipData = CreateClipData(textToSpeak, clipID, voiceSettings, diskCacheSettings);
            if (clipData == null)
            {
                Log("No clip provided", LogType.Error);
                onStreamReady?.Invoke(clipData, "No clip provided");
                return null;
            }

            // From Runtime Cache
            if (clipData.loadState != TTSClipLoadState.Unloaded)
            {
                // Add callback
                if (onStreamReady != null)
                {
                    // Call once ready
                    if (clipData.loadState == TTSClipLoadState.Preparing)
                    {
                        clipData.onPlaybackReady += (e) => onStreamReady(clipData, e);
                    }
                    // Call after return
                    else
                    {
                        CoroutineUtility.StartCoroutine(CallAfterAMoment(() => onStreamReady(clipData,
                            clipData.loadState == TTSClipLoadState.Loaded ? string.Empty : "Error")));
                    }
                }

                // Return clip
                return clipData;
            }

            // Add to runtime cache if possible
            if (RuntimeCacheHandler != null)
            {
                RuntimeCacheHandler.AddClip(clipData);
            }
            // Load begin
            else
            {
                OnLoadBegin(clipData);
            }

            // Add on ready delegate
            clipData.onPlaybackReady += (error) => onStreamReady(clipData, error);

            // Wait a moment and load
            CoroutineUtility.StartCoroutine(CallAfterAMoment(() =>
            {
                // Check for invalid text
                string invalidError = WebHandler.IsTextValid(clipData.textToSpeak);
                if (!string.IsNullOrEmpty(invalidError))
                {
                    OnStreamError(clipData, invalidError);
                    return;
                }

                // If should cache to disk, attempt to do so
                if (ShouldCacheToDisk(clipData))
                {
                    // Download was canceled before starting
                    if (clipData.loadState != TTSClipLoadState.Preparing)
                    {
                        string downloadPath = DiskCacheHandler.GetDiskCachePath(clipData);
                        OnWebDownloadBegin(clipData, downloadPath);
                        OnWebDownloadCancel(clipData, downloadPath);
                        OnStreamBegin(clipData);
                        OnStreamCancel(clipData);
                        return;
                    }

                    // Download
                    DownloadToDiskCache(clipData, (clipData2, downloadPath, error) =>
                    {
                        // Download was canceled before starting
                        if (string.Equals(error, CANCEL_WARNING))
                        {
                            OnStreamBegin(clipData);
                            OnStreamCancel(clipData);
                            return;
                        }

                        // Success
                        if (string.IsNullOrEmpty(error))
                        {
                            DiskCacheHandler?.StreamFromDiskCache(clipData);
                        }
                        // Failed
                        else
                        {
                            WebHandler?.RequestStreamFromWeb(clipData);
                        }
                    });
                }
                // Simply stream from the web
                else
                {
                    // Stream was canceled before starting
                    if (clipData.loadState != TTSClipLoadState.Preparing)
                    {
                        OnStreamBegin(clipData);
                        OnStreamCancel(clipData);
                        return;
                    }

                    // Stream
                    WebHandler?.RequestStreamFromWeb(clipData);
                }
            }));

            // Return data
            return clipData;
        }
        // Wait a moment
        private IEnumerator CallAfterAMoment(Action call)
        {
            if (Application.isPlaying)
            {
                yield return new WaitForEndOfFrame();
            }
            else
            {
                yield return null;
            }
            call();
        }
        // Load begin
        private void OnLoadBegin(TTSClipData clipData)
        {
            // Now preparing
            SetClipLoadState(clipData, TTSClipLoadState.Preparing);

            // Begin load
            Log($"Load Clip\nText: {clipData?.textToSpeak}\nID: {clipData.clipID}");
            Events?.OnClipCreated?.Invoke(clipData);
        }
        // Handle begin of disk cache streaming
        private void OnStreamBegin(TTSClipData clipData)
        {
            // Callback delegate
            Log($"Stream Begin\nText: {clipData?.textToSpeak}\nID: {clipData.clipID}");
            Events?.Stream?.OnStreamBegin?.Invoke(clipData);
        }
        // Handle successful completion of disk cache streaming
        private void OnStreamReady(TTSClipData clipData)
        {
            // Refresh cache for file size
            RuntimeCacheHandler?.AddClip(clipData);

            // Now loaded
            SetClipLoadState(clipData, TTSClipLoadState.Loaded);

            // Invoke playback is ready
            clipData.onPlaybackReady?.Invoke(string.Empty);
            clipData.onPlaybackReady = null;

            // Callback delegate
            Log($"Stream Ready\nText: {clipData?.textToSpeak}\nID: {clipData.clipID}");
            Events?.Stream?.OnStreamReady?.Invoke(clipData);
        }
        // Handle cancel of disk cache streaming
        private void OnStreamCancel(TTSClipData clipData)
        {
            // Handled as an error
            SetClipLoadState(clipData, TTSClipLoadState.Error);

            // Invoke
            clipData.onPlaybackReady?.Invoke(CANCEL_WARNING);
            clipData.onPlaybackReady = null;

            // Callback delegate
            Log($"Stream Canceled\nText: {clipData?.textToSpeak}\nID: {clipData.clipID}");
            Events?.Stream?.OnStreamCancel?.Invoke(clipData);

            // Unload clip
            Unload(clipData);
        }
        // Handle disk cache streaming error
        private void OnStreamError(TTSClipData clipData, string error)
        {
            // Error
            SetClipLoadState(clipData, TTSClipLoadState.Error);

            // Invoke playback is ready
            clipData.onPlaybackReady?.Invoke(error);
            clipData.onPlaybackReady = null;

            // Stream error
            Log($"Stream Error\nText: {clipData?.textToSpeak}\nID: {clipData.clipID}\nError: {error}", LogType.Error);
            Events?.Stream?.OnStreamError?.Invoke(clipData, error);

            // Unload clip
            Unload(clipData);
        }
        #endregion

        #region UNLOAD
        /// <summary>
        /// Unload all audio clips from the runtime cache
        /// </summary>
        public void UnloadAll()
        {
            // Failed
            TTSClipData[] clips = RuntimeCacheHandler?.GetClips();
            if (clips == null)
            {
                return;
            }

            // Copy array
            TTSClipData[] copy = new TTSClipData[clips.Length];
            clips.CopyTo(copy, 0);

            // Unload all clips
            foreach (var clip in copy)
            {
                Unload(clip);
            }
        }
        /// <summary>
        /// Force a runtime cache unload
        /// </summary>
        public void Unload(TTSClipData clipData)
        {
            if (RuntimeCacheHandler != null)
            {
                RuntimeCacheHandler.RemoveClip(clipData.clipID);
            }
            else
            {
                OnUnloadBegin(clipData);
            }
        }
        /// <summary>
        /// Perform clip unload
        /// </summary>
        /// <param name="clipID"></param>
        private void OnUnloadBegin(TTSClipData clipData)
        {
            // Abort if currently preparing
            if (clipData.loadState == TTSClipLoadState.Preparing)
            {
                // Cancel web stream
                WebHandler?.CancelWebStream(clipData);
                // Cancel web download to cache
                WebHandler?.CancelWebDownload(clipData, GetDiskCachePath(clipData.textToSpeak, clipData.clipID, clipData.voiceSettings, clipData.diskCacheSettings));
                // Cancel disk cache stream
                DiskCacheHandler?.CancelDiskCacheStream(clipData);
            }
            // Destroy clip
            else if (clipData.clip != null)
            {
                MonoBehaviour.DestroyImmediate(clipData.clip);
            }

            // Clip is now unloaded
            SetClipLoadState(clipData, TTSClipLoadState.Unloaded);

            // Unload
            Log($"Unload Clip\nText: {clipData?.textToSpeak}\nID: {clipData.clipID}");
            Events?.OnClipUnloaded?.Invoke(clipData);
        }
        #endregion

        #region RUNTIME CACHE
        /// <summary>
        /// Obtain a clip from the runtime cache, if applicable
        /// </summary>
        public TTSClipData GetRuntimeCachedClip(string clipID) => RuntimeCacheHandler?.GetClip(clipID);
        /// <summary>
        /// Obtain all clips from the runtime cache, if applicable
        /// </summary>
        public TTSClipData[] GetAllRuntimeCachedClips() => RuntimeCacheHandler?.GetClips();

        /// <summary>
        /// Called when runtime cache adds a clip
        /// </summary>
        /// <param name="clipData"></param>
        protected virtual void OnRuntimeClipAdded(TTSClipData clipData) => OnLoadBegin(clipData);
        /// <summary>
        /// Called when runtime cache unloads a clip
        /// </summary>
        /// <param name="clipData">Clip to be unloaded</param>
        protected virtual void OnRuntimeClipRemoved(TTSClipData clipData) => OnUnloadBegin(clipData);
        #endregion

        #region DISK CACHE
        /// <summary>
        /// Whether a specific clip should be cached
        /// </summary>
        /// <param name="clipData">Clip data</param>
        /// <returns>True if should be cached</returns>
        public bool ShouldCacheToDisk(TTSClipData clipData) =>
            DiskCacheHandler != null && DiskCacheHandler.ShouldCacheToDisk(clipData);

        /// <summary>
        /// Get disk cache
        /// </summary>
        /// <param name="textToSpeak">Text to be spoken in clip</param>
        /// <param name="clipID">Unique clip id</param>
        /// <param name="voiceSettings">Custom voice settings</param>
        /// <param name="diskCacheSettings">Custom disk cache settings</param>
        /// <returns></returns>
        public string GetDiskCachePath(string textToSpeak, string clipID, TTSVoiceSettings voiceSettings,
            TTSDiskCacheSettings diskCacheSettings) =>
            DiskCacheHandler?.GetDiskCachePath(CreateClipData(textToSpeak, clipID, voiceSettings, diskCacheSettings));

        // Download options
        public TTSClipData DownloadToDiskCache(string textToSpeak,
            Action<TTSClipData, string, string> onDownloadComplete = null) =>
            DownloadToDiskCache(textToSpeak, null, null, null, onDownloadComplete);
        public TTSClipData DownloadToDiskCache(string textToSpeak, string presetVoiceId,
            Action<TTSClipData, string, string> onDownloadComplete = null) => DownloadToDiskCache(textToSpeak, null,
            GetPresetVoiceSettings(presetVoiceId), null, onDownloadComplete);
        public TTSClipData DownloadToDiskCache(string textToSpeak, string presetVoiceId,
            TTSDiskCacheSettings diskCacheSettings, Action<TTSClipData, string, string> onDownloadComplete = null) =>
            DownloadToDiskCache(textToSpeak, null, GetPresetVoiceSettings(presetVoiceId), diskCacheSettings,
                onDownloadComplete);
        public TTSClipData DownloadToDiskCache(string textToSpeak, TTSVoiceSettings voiceSettings,
            TTSDiskCacheSettings diskCacheSettings, Action<TTSClipData, string, string> onDownloadComplete = null) =>
            DownloadToDiskCache(textToSpeak, null, voiceSettings, diskCacheSettings, onDownloadComplete);

        /// <summary>
        /// Perform a download for a TTS audio clip
        /// </summary>
        /// <param name="textToSpeak">Text to be spoken in clip</param>
        /// <param name="clipID">Unique clip id</param>
        /// <param name="voiceSettings">Custom voice settings</param>
        /// <param name="diskCacheSettings">Custom disk cache settings</param>
        /// <param name="onDownloadComplete">Callback when file has finished downloading</param>
        /// <returns>Generated TTS clip data</returns>
        public TTSClipData DownloadToDiskCache(string textToSpeak, string clipID, TTSVoiceSettings voiceSettings,
            TTSDiskCacheSettings diskCacheSettings, Action<TTSClipData, string, string> onDownloadComplete = null)
        {
            TTSClipData clipData = CreateClipData(textToSpeak, clipID, voiceSettings, diskCacheSettings);
            DownloadToDiskCache(clipData, onDownloadComplete);
            return clipData;
        }

        // Performs download to disk cache
        protected virtual void DownloadToDiskCache(TTSClipData clipData, Action<TTSClipData, string, string> onDownloadComplete)
        {
            // Add delegates if needed
            AddDelegates();

            // Already in cache
            string downloadPath = DiskCacheHandler.GetDiskCachePath(clipData);
            if (File.Exists(downloadPath))
            {
                onDownloadComplete?.Invoke(clipData, downloadPath, string.Empty);
                return;
            }

            // Fail if not preloaded
            if (Application.isPlaying && clipData.diskCacheSettings.DiskCacheLocation == TTSDiskCacheLocation.Preload)
            {
                string warning = $"File is not preloaded\nText to Speak: {clipData.textToSpeak}\nVoice ID: {clipData.voiceSettings?.settingsID}";
                Log(warning, LogType.Warning);
                onDownloadComplete?.Invoke(clipData, downloadPath, warning);
                return;
            }

            // Return error
            clipData.onDownloadComplete += (error) => onDownloadComplete(clipData, downloadPath, error);

            // Download to cache & then stream
            WebHandler.RequestDownloadFromWeb(clipData, downloadPath);
        }
        // On web download begin
        private void OnWebDownloadBegin(TTSClipData clipData, string downloadPath)
        {
            Log($"Download Clip - Begin\nText: {clipData?.textToSpeak}\nID: {clipData.clipID}\nPath: {downloadPath}");
            Events?.Download?.OnDownloadBegin?.Invoke(clipData, downloadPath);
        }
        // On web download complete
        private void OnWebDownloadSuccess(TTSClipData clipData, string downloadPath)
        {
            // Invoke clip callback & clear
            clipData.onDownloadComplete?.Invoke(string.Empty);
            clipData.onDownloadComplete = null;

            // Log
            Log($"Download Clip - Success\nText: {clipData?.textToSpeak}\nID: {clipData.clipID}\nPath: {downloadPath}");
            Events?.Download?.OnDownloadSuccess?.Invoke(clipData, downloadPath);
        }
        // On web download complete
        private void OnWebDownloadCancel(TTSClipData clipData, string downloadPath)
        {
            // Invoke clip callback & clear
            clipData.onDownloadComplete?.Invoke(CANCEL_WARNING);
            clipData.onDownloadComplete = null;

            // Log
            Log($"Download Clip - Canceled\nText: {clipData?.textToSpeak}\nID: {clipData.clipID}\nPath: {downloadPath}");
            Events?.Download?.OnDownloadCancel?.Invoke(clipData, downloadPath);
        }
        // On web download complete
        private void OnWebDownloadError(TTSClipData clipData, string downloadPath, string error)
        {
            // Invoke clip callback & clear
            clipData.onDownloadComplete?.Invoke(error);
            clipData.onDownloadComplete = null;

            // Log
            Log($"Download Clip - Failed\nText: {clipData?.textToSpeak}\nID: {clipData.clipID}\nPath: {downloadPath}\nError: {error}", LogType.Error);
            Events?.Download?.OnDownloadError?.Invoke(clipData, downloadPath, error);
        }
        #endregion

        #region VOICES
        /// <summary>
        /// Return all preset voice settings
        /// </summary>
        /// <returns></returns>
        public TTSVoiceSettings[] GetAllPresetVoiceSettings() => VoiceProvider?.PresetVoiceSettings;

        /// <summary>
        /// Return preset voice settings for a specific id
        /// </summary>
        /// <param name="presetVoiceId"></param>
        /// <returns></returns>
        public TTSVoiceSettings GetPresetVoiceSettings(string presetVoiceId)
        {
            if (VoiceProvider == null || VoiceProvider.PresetVoiceSettings == null)
            {
                return null;
            }
            return Array.Find(VoiceProvider.PresetVoiceSettings, (v) => string.Equals(v.settingsID, presetVoiceId, StringComparison.CurrentCultureIgnoreCase));
        }
        #endregion
    }
}
