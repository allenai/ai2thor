/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEditor;
using UnityEngine;

namespace Facebook.WitAi.Inspectors
{
    public class WitInspector : Editor
    {
        private string activationMessage;
        private VoiceService wit;
        private float micMin;
        private float micMax;
        private string lastTranscription;
        private float micCurrent;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (Application.isPlaying)
            {
                wit = (VoiceService) target;

                if (wit.Active)
                {
                    if (GUILayout.Button("Deactivate"))
                    {
                        wit.Deactivate();
                    }

                    if (wit.MicActive)
                    {
                        GUILayout.Label("Listening...");
                    }
                    else
                    {
                        GUILayout.Label("Processing...");
                    }
                }
                else
                {
                    if (GUILayout.Button("Activate"))
                    {
                        InitializeActivationLogging();
                        wit.Activate();
                    }

                    GUILayout.BeginHorizontal();
                    activationMessage = GUILayout.TextField(activationMessage);
                    if (GUILayout.Button("Send", GUILayout.Width(50)))
                    {
                        InitializeActivationLogging();
                        wit.Activate(activationMessage);
                    }

                    GUILayout.EndHorizontal();
                }

                GUILayout.Label("Last Transcription", EditorStyles.boldLabel);
                GUILayout.TextArea(lastTranscription);

                GUILayout.Label("Mic Status", EditorStyles.boldLabel);
                GUILayout.Label($"Mic range: {micMin.ToString("F5")} - {micMax.ToString("F5")}");
                GUILayout.Label($"Mic current: {micCurrent.ToString("F5")}");
            }
        }

        private void InitializeActivationLogging()
        {
            wit.events.OnFullTranscription.AddListener(UpdateTranscription);
            wit.events.OnPartialTranscription.AddListener(UpdateTranscription);
            wit.events.OnMicLevelChanged.AddListener(OnMicLevelChanged);
            micMin = Mathf.Infinity;
            micMax = Mathf.NegativeInfinity;
            EditorApplication.update += UpdateWhileActive;
        }

        private void OnMicLevelChanged(float volume)
        {
            micCurrent = volume;
            micMin = Mathf.Min(volume, micMin);
            micMax = Mathf.Max(volume, micMax);
        }

        private void UpdateTranscription(string transcription)
        {
            lastTranscription = transcription;
        }

        private void UpdateWhileActive()
        {
            Repaint();
            if (!wit.Active)
            {
                EditorApplication.update -= UpdateWhileActive;
                wit.events.OnFullTranscription.RemoveListener(UpdateTranscription);
                wit.events.OnPartialTranscription.RemoveListener(UpdateTranscription);
                wit.events.OnMicLevelChanged.RemoveListener(OnMicLevelChanged);
            }
        }
    }
}
