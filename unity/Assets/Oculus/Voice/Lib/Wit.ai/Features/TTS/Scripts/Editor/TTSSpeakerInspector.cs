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
using Facebook.WitAi.TTS.Utilities;
using Facebook.WitAi.TTS.Data;

namespace Facebook.WitAi.TTS.Editor
{
    [CustomEditor(typeof(TTSSpeaker), true)]
    public class TTSSpeakerInspector : UnityEditor.Editor
    {
        // Speaker
        private TTSSpeaker _speaker;

        // Voices
        private int _voiceIndex = -1;
        private string[] _voices = null;

        // Voice text
        private const string UI_VOICE_HEADER = "Voice Settings";
        private const string UI_VOICE_KEY = "Voice Preset";

        // GUI
        public override void OnInspectorGUI()
        {
            // Get speaker
            if (_speaker == null)
            {
                _speaker = target as TTSSpeaker;
            }
            // Get voices
            if (_voices == null || (_voiceIndex >= 0 && _voiceIndex < _voices.Length && !string.Equals(_speaker.VoiceSettings.settingsID, _voices[_voiceIndex])))
            {
                RefreshVoices();
            }

            // Voice select
            EditorGUILayout.LabelField(UI_VOICE_HEADER, EditorStyles.boldLabel);
            // No voices found
            if (_voices == null || _voices.Length == 0)
            {
                EditorGUILayout.TextField(UI_VOICE_KEY, _speaker.presetVoiceID);
            }
            // Voice dropdown
            else
            {
                bool updated = false;
                WitEditorUI.LayoutPopup(UI_VOICE_KEY, _voices, ref _voiceIndex, ref updated);
                if (updated)
                {
                    string newVoiceID = _voiceIndex >= 0 && _voiceIndex < _voices.Length
                        ? _voices[_voiceIndex]
                        : string.Empty;
                    _speaker.presetVoiceID = newVoiceID;
                    EditorUtility.SetDirty(_speaker);
                }
            }

            // Display default ui
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            base.OnInspectorGUI();
        }

        // Refresh voices
        private void RefreshVoices()
        {
            // Reset voice data
            _voiceIndex = -1;
            _voices = null;

            // Get settings
            TTSService tts = TTSService.Instance;
            TTSVoiceSettings[] settings = tts?.GetAllPresetVoiceSettings();
            if (settings == null)
            {
                Debug.LogError("No Preset Voice Settings Found!");
                return;
            }

            // Apply all settings
            _voices = new string[settings.Length];
            for (int i = 0; i < settings.Length; i++)
            {
                _voices[i] = settings[i].settingsID;
                if (string.Equals(_speaker.presetVoiceID, _voices[i], StringComparison.CurrentCultureIgnoreCase))
                {
                    _voiceIndex = i;
                }
            }
        }
    }
}
