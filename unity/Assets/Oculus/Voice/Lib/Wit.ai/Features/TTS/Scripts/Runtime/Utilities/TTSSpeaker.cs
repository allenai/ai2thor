/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Facebook.WitAi.TTS.Data;

namespace Facebook.WitAi.TTS.Utilities
{
    [Serializable]
    public class TTSSpeakerEvent : UnityEvent<TTSSpeaker, string>
    {
    }
    [Serializable]
    public class TTSSpeakerEvents
    {
        [Tooltip("Called when TTS audio clip load begins")]
        public TTSSpeakerEvent OnClipLoadBegin;
        [Tooltip("Called when TTS audio clip load fails")]
        public TTSSpeakerEvent OnClipLoadFailed;
        [Tooltip("Called when TTS audio clip load successfully")]
        public TTSSpeakerEvent OnClipLoadSuccess;
        [Tooltip("Called when TTS audio clip load is cancelled")]
        public TTSSpeakerEvent OnClipLoadAbort;

        [Tooltip("Called when a audio clip playback begins")]
        public TTSSpeakerEvent OnStartSpeaking;
        [Tooltip("Called when a audio clip playback completes or is cancelled")]
        public TTSSpeakerEvent OnFinishedSpeaking;
        [Tooltip("Called when a audio clip playback completes or is cancelled")]
        public TTSSpeakerEvent OnCancelledSpeaking;
    }

    public class TTSSpeaker : MonoBehaviour
    {
        #region SETUP
        // Preset voice id
        [HideInInspector] [SerializeField] public string presetVoiceID;
        public TTSVoiceSettings VoiceSettings => TTSService.Instance.GetPresetVoiceSettings(presetVoiceID);
        // Audio source
        [SerializeField] private AudioSource _source;
        public AudioSource AudioSource => _source;
        // Events
        [SerializeField] private TTSSpeakerEvents _events;
        public TTSSpeakerEvents Events => _events;

        // Automatically generate source if needed
        protected virtual void Awake()
        {
            if (_source == null)
            {
                _source = gameObject.GetComponentInChildren<AudioSource>();
                if (_source == null)
                {
                    _source = gameObject.AddComponent<AudioSource>();
                }
            }
            _source.playOnAwake = false;
            TTSService.Instance.Events.OnClipUnloaded.AddListener(OnClipUnload);
        }
        // Stop speaking
        protected virtual void OnDestroy()
        {
            Stop();
            if (TTSService.Instance != null)
            {
                TTSService.Instance.Events.OnClipUnloaded.RemoveListener(OnClipUnload);
            }
        }
        // Clip unloaded externally
        protected virtual void OnClipUnload(TTSClipData clipData)
        {
            // Handle abort for loading
            if (clipData == _loadingClip)
            {
                OnLoadAbort();
            }
            // Cancel playback & remove clip
            else if (clipData == _lastClip)
            {
                // Cancel playback
                OnPlaybackCancel();
                // Remove reference
                _lastClip = null;
            }
        }
        // Compare clip with new text
        protected virtual bool IsClipSame(string clipID, TTSClipData clipData)
        {
            // Not same
            if (clipData == null)
            {
                return false;
            }
            // Not same text
            if (!string.Equals(clipID, clipData.clipID, StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }
            // Success
            return true;
        }
        #endregion

        #region SPEAK
        // Whether speaking
        public bool IsSpeaking => _speaking;
        private bool _speaking = false;

        /// <summary>
        /// Speaks a format delimited phrase
        /// </summary>
        /// <param name="format">Format string to be filled in with texts</param>
        /// <param name="textsToSpeak">Parts of text to be loaded into the format string</param>
        public virtual void Speak(string format, params string[] textsToSpeak)
        {
            // Ensure texts exist
            if (textsToSpeak == null)
            {
                return;
            }
            // Format
            object[] objects = new object[textsToSpeak.Length];
            textsToSpeak.CopyTo(objects, 0);
            Speak(string.Format(format, objects));
        }
        // Speak from this audio source
        public virtual void Speak(string textToSpeak) => Speak(textToSpeak, (TTSDiskCacheSettings)null);
        // Speak from this audio source
        public virtual void Speak(string textToSpeak, TTSDiskCacheSettings diskCacheSettings)
        {
            // Ensure voice settings exist
            TTSVoiceSettings voiceSettings = VoiceSettings;
            if (voiceSettings == null)
            {
                Debug.LogError($"TTS Speaker - No voice found with preset id: {presetVoiceID}");
                return;
            }
            // Log if empty text
            if (string.IsNullOrEmpty(textToSpeak))
            {
                Debug.LogError("TTS Speaker - No text to speak provided");
                return;
            }
            // Get clip id
            string newClipID = TTSService.Instance.GetClipID(textToSpeak, voiceSettings);
            // Currently loading
            if (_loadingClip != null)
            {
                // Already loading text
                if (IsClipSame(newClipID, _loadingClip))
                {
                    // Refresh clip use time
                    TTSService.Instance.GetRuntimeCachedClip(_loadingClip.clipID);
                    return;
                }
                // Abort previous load
                OnLoadAbort();
            }
            // Currently playing
            if (IsClipSame(newClipID, _lastClip))
            {
                // Refresh clip use time
                TTSService.Instance.GetRuntimeCachedClip(_lastClip.clipID);
                // Play clip
                OnPlaybackBegin(_lastClip);
                return;
            }

            // Load new clip
            OnLoadBegin(textToSpeak, newClipID, voiceSettings, diskCacheSettings);
        }
        // Stops loading & speaking immediately
        public virtual void Stop()
        {
            // Abort if loading
            if (_loadingClip != null)
            {
                TTSService.Instance.Unload(_loadingClip);
            }
            // Cancel if playing
            if (IsSpeaking)
            {
                // Stop source
                if (_lastClip != null)
                {
                    _source.Stop();
                }

                // Cancel calls
                OnPlaybackCancel();
            }
        }
        #endregion

        #region LOAD
        // Whether currently loading or not
        public bool IsLoading => _loadingClip != null;
        // Loading clip
        protected TTSClipData _loadingClip;

        // Begin a load
        protected virtual void OnLoadBegin(string textToSpeak, string clipID, TTSVoiceSettings voiceSettings, TTSDiskCacheSettings diskCacheSettings)
        {
            // Load begin
            Events?.OnClipLoadBegin?.Invoke(this, textToSpeak);

            // Perform load request
            _loadingClip = TTSService.Instance.Load(textToSpeak, clipID, voiceSettings, diskCacheSettings, OnLoadComplete);
        }
        // Load complete
        protected virtual void OnLoadComplete(TTSClipData clipData, string error)
        {
            // Incorrect clip, ignore
            if (clipData != _loadingClip)
            {
                return;
            }

            // Loading complete
            _loadingClip = null;

            // Load failed
            if (clipData.clip == null)
            {
                Debug.LogError($"TTS Speaker - Load Clip - Failed\n{error}");
                Events?.OnClipLoadFailed?.Invoke(this, clipData.textToSpeak);
                return;
            }

            // Load success
            Events?.OnClipLoadSuccess?.Invoke(this, clipData.textToSpeak);

            // Play clip
            OnPlaybackBegin(clipData);
        }
        // Cancel load
        protected virtual void OnLoadAbort()
        {
            // Abort any loading
            if (_loadingClip != null)
            {
                Events?.OnClipLoadAbort?.Invoke(this, _loadingClip.textToSpeak);
                _loadingClip = null;
            }
        }
        #endregion

        #region PLAY
        // Play coroutine
        private Coroutine _player;
        // Most recently loaded clip
        private TTSClipData _lastClip;

        // Play begin
        protected virtual void OnPlaybackBegin(TTSClipData clipData)
        {
            // If already speaking, stop doing so
            OnPlaybackCancel();

            // Remove previous clip
            if (clipData != _lastClip && _lastClip != null)
            {
                _lastClip = null;
            }

            // Apply clip
            _lastClip = clipData;

            // If clip missing
            if (_lastClip == null || _lastClip.clip == null)
            {
                Debug.LogError("TTS Speaker - Clip destroyed prior to playback");
                return;
            }

            // Started speaking
            _speaking = true;
            Events?.OnStartSpeaking?.Invoke(this, _lastClip.textToSpeak);

            // Play clip & wait
            _source.PlayOneShot(_lastClip.clip);
            _player = StartCoroutine(OnPlaybackWait());
        }
        // Wait for clip completion
        protected virtual IEnumerator OnPlaybackWait()
        {
            // Wait for completion
            yield return new WaitForSeconds(_lastClip.clip.length);

            // Complete
            OnPlaybackComplete();
        }
        // Play complete
        protected virtual void OnPlaybackComplete()
        {
            // Not speaking
            if (!_speaking)
            {
                return;
            }

            // Done
            _speaking = false;
            _player = null;

            // Cancelled
            if (_lastClip != null)
            {
                Events?.OnFinishedSpeaking?.Invoke(this, _lastClip.textToSpeak);
            }
        }
        // Play cancel
        protected virtual void OnPlaybackCancel()
        {
            // Not speaking
            if (!_speaking)
            {
                return;
            }

            // Done
            _speaking = false;
            if (_player != null)
            {
                StopCoroutine(_player);
                _player = null;
            }

            // Cancelled
            if (_lastClip != null)
            {
                Events?.OnCancelledSpeaking?.Invoke(this, _lastClip.textToSpeak);
            }
        }
        #endregion
    }
}
