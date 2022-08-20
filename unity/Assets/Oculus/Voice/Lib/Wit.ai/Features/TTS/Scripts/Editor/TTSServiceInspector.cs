/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Facebook.WitAi.TTS.Data;
using Facebook.WitAi.TTS.Integrations;
using UnityEditor;
using UnityEngine;

namespace Facebook.WitAi.TTS.Editor
{
    [CustomEditor(typeof(TTSService), true)]
    public class TTSServiceInspector : UnityEditor.Editor
    {
        // Service
        private TTSService _service;
        // Dropdown
        private bool _clipFoldout = false;
        // Maximum text for abbreviated
        private const int MAX_DISPLAY_TEXT = 20;

        // Custom GUI when needed
        public static event Action<TTSService> onAdditionalGUI;

        // GUI
        public override void OnInspectorGUI()
        {
            // Display default ui
            base.OnInspectorGUI();

            // Get service
            if (_service == null)
            {
                _service = target as TTSService;
            }
            // Add additional gui
            onAdditionalGUI?.Invoke(_service);

            // Ignore if in editor
            if (!Application.isPlaying)
            {
                return;
            }

            // Add spaces
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Clip Cache", EditorStyles.boldLabel);

            // No clips
            TTSClipData[] clips = _service.GetAllRuntimeCachedClips();
            if (clips == null || clips.Length == 0)
            {
                WitEditorUI.LayoutErrorLabel("No clips found");
                return;
            }
            // Has clips
            _clipFoldout = WitEditorUI.LayoutFoldout(new GUIContent($"Clips: {clips.Length}"), _clipFoldout);
            if (_clipFoldout)
            {
                EditorGUI.indentLevel++;
                // Iterate clips
                foreach (TTSClipData clip in clips)
                {
                    // Get display name
                    string displayName = clip.textToSpeak;
                    // Crop if too long
                    if (displayName.Length > MAX_DISPLAY_TEXT)
                    {
                        displayName = displayName.Substring(0, MAX_DISPLAY_TEXT);
                    }
                    // Add voice setting id
                    if (clip.voiceSettings != null)
                    {
                        displayName = $"{clip.voiceSettings.settingsID} - {displayName}";
                    }
                    // Foldout if desired
                    bool foldout = WitEditorUI.LayoutFoldout(new GUIContent(displayName), clip);
                    if (foldout)
                    {
                        EditorGUI.indentLevel++;
                        OnClipGUI(clip);
                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUI.indentLevel--;
            }
        }
        // Clip data
        private void OnClipGUI(TTSClipData clip)
        {
            // Generation Settings
            WitEditorUI.LayoutKeyLabel("Text", clip.textToSpeak);
            WitEditorUI.LayoutKeyObjectLabels("Voice Settings", clip.voiceSettings);
            WitEditorUI.LayoutKeyObjectLabels("Cache Settings", clip.diskCacheSettings);
            // Clip Settings
            EditorGUILayout.TextField("Clip ID", clip.clipID);
            EditorGUILayout.ObjectField("Clip", clip.clip, typeof(AudioClip), true);
            // Load Settings
            WitEditorUI.LayoutKeyLabel("Load State", clip.loadState.ToString());
            WitEditorUI.LayoutKeyLabel("Load Progress", (clip.loadProgress * 100f).ToString() + "%");
        }
    }
}
