/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Facebook.WitAi.Configuration;
using Facebook.WitAi.Data.Configuration;
using Facebook.WitAi.TTS.Data;
using Facebook.WitAi.TTS.Editor.Preload;
using UnityEditor;
using UnityEngine;

namespace Facebook.WitAi.TTS.Editor
{
    [CustomEditor(typeof(TTSPreloadSettings), true)]
    public class TTSPreloadSettingsInspector : UnityEditor.Editor
    {
        // TTS Settings
        public TTSPreloadSettings Settings { get; private set; }

        // TTS Service
        public TTSService TtsService { get; private set; }
        private List<string> _ttsVoiceIDs;

        // Layout items
        public const float ACTION_BTN_INDENT = 15f;
        public virtual Texture2D HeaderIcon => WitTexts.HeaderIcon;
        public virtual string HeaderUrl => WitTexts.GetAppURL(WitConfigurationUtility.GetAppID(null), WitTexts.WitAppEndpointType.Settings);

        // Layout
        public override void OnInspectorGUI()
        {
            // Get settings
            if (Settings != target)
            {
                Settings = target as TTSPreloadSettings;
            }

            // Draw header
            WitEditorUI.LayoutHeaderButton(HeaderIcon, HeaderUrl);
            GUILayout.Space(WitStyles.HeaderPaddingBottom);

            // Layout actions
            LayoutPreloadActions();
            // Layout data
            LayoutPreloadData();
        }
        // Layout Preload Data
        protected virtual void LayoutPreloadActions()
        {
            // Layout preload actions
            EditorGUILayout.Space();
            WitEditorUI.LayoutSubheaderLabel("TTS Preload Actions");

            // Indent
            EditorGUI.indentLevel++;

            // Get TTS Service if needed
            EditorGUILayout.Space();
            TtsService = EditorGUILayout.ObjectField("TTS Service", TtsService, typeof(TTSService), true) as TTSService;
            if (TtsService == null)
            {
                if (!Application.isPlaying)
                {
                    EditorUtility.ClearProgressBar();
                    TtsService = GameObject.FindObjectOfType<TTSService>();
                }
                WitEditorUI.LayoutErrorLabel("You must add a TTS Service to the loaded scene in order perform TTS actions.");
                EditorGUI.indentLevel--;
                return;
            }
            if (TtsService != null && _ttsVoiceIDs == null)
            {
                _ttsVoiceIDs = GetVoiceIDs(TtsService);
            }

            // Begin buttons
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();

            // Import JSON
            GUILayout.Space(ACTION_BTN_INDENT * EditorGUI.indentLevel);
            if (WitEditorUI.LayoutTextButton("Refresh Data"))
            {
                RefreshData();
            }
            GUILayout.Space(ACTION_BTN_INDENT);
            if (WitEditorUI.LayoutTextButton("Import JSON"))
            {
                EditorUtility.ClearProgressBar();
                if (TTSPreloadUtility.ImportData(Settings))
                {
                    RefreshData();
                }
            }
            // Clear disk cache
            GUI.enabled = TtsService != null;
            EditorGUILayout.Space();
            Color col = GUI.color;
            GUI.color = Color.red;
            if (WitEditorUI.LayoutTextButton("Delete Cache"))
            {
                EditorUtility.ClearProgressBar();
                TTSPreloadUtility.DeleteData(TtsService);
                RefreshData();
            }
            // Preload disk cache
            GUILayout.Space(ACTION_BTN_INDENT);
            GUI.color = Color.green;
            if (WitEditorUI.LayoutTextButton("Preload Cache"))
            {
                DownloadClips();
            }
            GUI.color = col;
            GUI.enabled = true;

            // End buttons
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            // Indent
            EditorGUI.indentLevel--;
        }
        // Refresh
        private void RefreshData()
        {
            TTSPreloadUtility.RefreshPreloadData(TtsService, Settings.data, (p) =>
            {
                EditorUtility.DisplayProgressBar("TTS Preload Utility", "Refreshing Data", p);
            }, (d, l) =>
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.SetDirty(Settings);
                Debug.Log($"TTS Preload Utility - Refresh Complete{l}");
            });
        }
        // Download
        private void DownloadClips()
        {
            TTSPreloadUtility.PreloadData(TtsService, Settings.data, (p) =>
            {
                EditorUtility.DisplayProgressBar("TTS Preload Utility", "Downloading Clips", p);
            }, (d, l) =>
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.SetDirty(Settings);
                AssetDatabase.Refresh();
                Debug.Log($"TTS Preload Utility - Preload Complete{l}");
            });
        }
        // Layout Preload Data
        protected virtual void LayoutPreloadData()
        {
            // For updates
            bool updated = false;

            // Layout preload items
            GUILayout.Space(WitStyles.WindowPaddingBottom);
            GUILayout.BeginHorizontal();
            WitEditorUI.LayoutSubheaderLabel("TTS Preload Data");
            if (WitEditorUI.LayoutTextButton("Add Voice"))
            {
                AddVoice();
                updated = true;
            }
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();

            // Indent
            EditorGUI.indentLevel++;

            // Generate
            if (Settings.data == null)
            {
                Settings.data = new TTSPreloadData();
            }
            if (Settings.data.voices == null)
            {
                Settings.data.voices = new TTSPreloadVoiceData[] {new TTSPreloadVoiceData()};
            }

            // Begin scroll
            for (int v = 0; v < Settings.data.voices.Length; v++)
            {
                if (!LayoutVoiceData(Settings.data, v, ref updated))
                {
                    break;
                }
            }

            // Set dirty
            if (updated)
            {
                EditorUtility.SetDirty(Settings);
            }

            // Indent
            EditorGUI.indentLevel--;
        }
        // Layout
        private bool LayoutVoiceData(TTSPreloadData preloadData, int voiceIndex, ref bool updated)
        {
            // Indent
            EditorGUI.indentLevel++;

            // Get data
            TTSPreloadVoiceData voiceData = preloadData.voices[voiceIndex];
            string voiceID = voiceData.presetVoiceID;
            if (string.IsNullOrEmpty(voiceID))
            {
                voiceID = "No Voice Selected";
            }
            voiceID = $"{(voiceIndex+1)} - {voiceID}";

            // Foldout
            GUILayout.BeginHorizontal();
            bool show = WitEditorUI.LayoutFoldout(new GUIContent(voiceID), voiceData);
            if (!show)
            {
                GUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
                return true;
            }

            // Delete
            if (WitEditorUI.LayoutTextButton("Delete Voice"))
            {
                DeleteVoice(voiceIndex);
                GUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
                updated = true;
                return false;
            }

            // Begin Voice Data
            GUILayout.EndHorizontal();
            EditorGUI.indentLevel++;

            // Voice Text Field
            if (TtsService == null || _ttsVoiceIDs == null || _ttsVoiceIDs.Count == 0)
            {
                WitEditorUI.LayoutTextField(new GUIContent("Voice ID"), ref voiceData.presetVoiceID, ref updated);
            }
            // Voice Preset Select
            else
            {
                int presetIndex = _ttsVoiceIDs.IndexOf(voiceData.presetVoiceID);
                bool presetUpdated = false;
                WitEditorUI.LayoutPopup("Voice ID", _ttsVoiceIDs.ToArray(), ref presetIndex, ref presetUpdated);
                if (presetUpdated)
                {
                    voiceData.presetVoiceID = _ttsVoiceIDs[presetIndex];
                    string l = string.Empty;
                    TTSPreloadUtility.RefreshVoiceData(TtsService, voiceData, null, ref l);
                    updated = true;
                }
            }

            // Ensure phrases exist
            if (voiceData.phrases == null)
            {
                voiceData.phrases = new TTSPreloadPhraseData[] { };
            }

            // Phrase Foldout
            EditorGUILayout.BeginHorizontal();
            bool isLayout = WitEditorUI.LayoutFoldout(new GUIContent($"Phrases ({voiceData.phrases.Length})"),
                voiceData.phrases);
            if (WitEditorUI.LayoutTextButton("Add Phrase"))
            {
                TTSPreloadPhraseData lastPhrase = voiceData.phrases.Length == 0 ? null : voiceData.phrases[voiceData.phrases.Length - 1];
                voiceData.phrases = AddArrayItem<TTSPreloadPhraseData>(voiceData.phrases, new TTSPreloadPhraseData()
                {
                    textToSpeak = lastPhrase?.textToSpeak,
                    clipID = lastPhrase?.clipID
                });
                GUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
                updated = true;
                return false;
            }
            EditorGUILayout.EndHorizontal();
            if (isLayout)
            {
                for (int p = 0; p < voiceData.phrases.Length; p++)
                {
                    if (!LayoutPhraseData(voiceData, p, ref updated))
                    {
                        break;
                    }
                }
            }

            // End Voice Data
            EditorGUILayout.Space();
            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;
            return true;
        }
        // Layout phrase data
        private bool LayoutPhraseData(TTSPreloadVoiceData voiceData, int phraseIndex, ref bool updated)
        {
            // Begin Phrase
            EditorGUI.indentLevel++;

            // Get data
            TTSPreloadPhraseData phraseData = voiceData.phrases[phraseIndex];
            string title = $"{(phraseIndex+1)} - {phraseData.textToSpeak}";

            // Foldout
            GUILayout.BeginHorizontal();
            bool show = WitEditorUI.LayoutFoldout(new GUIContent(title), phraseData);
            if (!show)
            {
                GUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
                return true;
            }

            // Delete
            if (WitEditorUI.LayoutTextButton("Delete Phrase"))
            {
                voiceData.phrases = DeleteArrayItem<TTSPreloadPhraseData>(voiceData.phrases, phraseIndex);
                GUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
                updated = true;
                return false;
            }

            // Begin phrase Data
            GUILayout.EndHorizontal();
            EditorGUI.indentLevel++;

            // Phrase
            bool phraseChange = false;
            WitEditorUI.LayoutTextField(new GUIContent("Phrase"), ref phraseData.textToSpeak, ref phraseChange);
            if (phraseChange)
            {
                TTSPreloadUtility.RefreshPhraseData(TtsService, new TTSDiskCacheSettings()
                {
                    DiskCacheLocation = TTSDiskCacheLocation.Preload
                }, TtsService?.GetPresetVoiceSettings(voiceData.presetVoiceID), phraseData);
                updated = true;
            }

            // Clip
            string clipID = phraseData.clipID;
            WitEditorUI.LayoutTextField(new GUIContent("Clip ID"), ref clipID, ref phraseChange);

            // State
            Color col = GUI.color;
            Color stateColor = Color.green;
            string stateValue = "Downloaded";
            if (!phraseData.downloaded)
            {
                if (phraseData.downloadProgress <= 0f)
                {
                    stateColor = Color.red;
                    stateValue = "Missing";
                }
                else
                {
                    stateColor = Color.yellow;
                    stateValue = $"Downloading {(phraseData.downloadProgress * 100f):00.0}%";
                }
            }
            GUI.color = stateColor;
            WitEditorUI.LayoutKeyLabel("State", stateValue);
            GUI.color = col;

            // End Phrase
            EditorGUILayout.Space();
            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;
            return true;
        }
        // Add
        private T[] AddArrayItem<T>(T[] array, T item) => EditArray<T>(array, (l) => l.Add(item));
        // Delete
        private T[] DeleteArrayItem<T>(T[] array, int index) => EditArray<T>(array, (l) => l.RemoveAt(index));
        // Edit array
        private T[] EditArray<T>(T[] array, Action<List<T>> edit)
        {
            // Generate list
            List<T> list = new List<T>();

            // Add array to list
            if (array != null)
            {
                list.AddRange(array);
            }

            // Call edit action
            edit(list);

            // Set to array
            T[] result = list.ToArray();

            // Refresh foldout value
            WitEditorUI.SetFoldoutValue(result, WitEditorUI.GetFoldoutValue(array));

            // Return array
            return result;
        }
        //
        private void AddVoice()
        {
            List<TTSPreloadVoiceData> voices = new List<TTSPreloadVoiceData>();
            if (Settings?.data?.voices != null)
            {
                voices.AddRange(Settings.data.voices);
            }
            voices.Add(new TTSPreloadVoiceData()
            {
                presetVoiceID = _ttsVoiceIDs == null || _ttsVoiceIDs.Count == 0 ? "" : _ttsVoiceIDs[0],
                phrases = new TTSPreloadPhraseData[] { new TTSPreloadPhraseData() }
            });
            Settings.data.voices = voices.ToArray();
        }
        // Delete voice
        private void DeleteVoice(int index)
        {
            // Invalid
            if (Settings?.data?.voices == null || index < 0 || index >= Settings.data.voices.Length)
            {
                return;
            }
            // Cancelled
            if (!EditorUtility.DisplayDialog("Delete Voice?",
                $"Are you sure you would like to remove voice data:\n#{(index + 1)} - {Settings.data.voices[index].presetVoiceID}?",
                "Okay", "Cancel"))
            {
                return;
            }

            // Remove
            List<TTSPreloadVoiceData> voices = new List<TTSPreloadVoiceData>(Settings.data.voices);
            voices.RemoveAt(index);
            Settings.data.voices = voices.ToArray();
        }
        // Get voice ids
        private List<string> GetVoiceIDs(TTSService service)
        {
            List<string> results = new List<string>();
            if (service != null)
            {
                foreach (var voiceSetting in service.GetAllPresetVoiceSettings())
                {
                    if (voiceSetting != null && !string.IsNullOrEmpty(voiceSetting.settingsID) &&
                        !results.Contains(voiceSetting.settingsID))
                    {
                        results.Add(voiceSetting.settingsID);
                    }
                }
            }
            return results;
        }
    }
}
