/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Facebook.WitAi;
using Facebook.WitAi.Lib;
using UnityEngine;
using UnityEngine.UI;

namespace Oculus.Voice.Demo
{
    public class InteractionHandler : MonoBehaviour
    {
        [Header("Default States"), Multiline]
        [SerializeField] private string freshStateText = "Try pressing the Activate button and saying \"Make the cube red\"";

        [Header("UI")]
        [SerializeField] private Text textArea;
        [SerializeField] private bool showJson;

        [Header("Voice")]
        [SerializeField] private AppVoiceExperience appVoiceExperience;

        // Whether voice is activated
        public bool IsActive => _active;
        private bool _active = false;

        // Add delegates
        private void OnEnable()
        {
            appVoiceExperience.events.OnRequestCreated.AddListener(OnRequestStarted);
            appVoiceExperience.events.OnPartialTranscription.AddListener(OnRequestTranscript);
            appVoiceExperience.events.OnFullTranscription.AddListener(OnRequestTranscript);
            appVoiceExperience.events.OnStartListening.AddListener(OnListenStart);
            appVoiceExperience.events.OnStoppedListening.AddListener(OnListenStop);
            appVoiceExperience.events.OnStoppedListeningDueToDeactivation.AddListener(OnListenForcedStop);
            appVoiceExperience.events.OnResponse.AddListener(OnRequestResponse);
            appVoiceExperience.events.OnError.AddListener(OnRequestError);
        }
        // Remove delegates
        private void OnDisable()
        {
            appVoiceExperience.events.OnRequestCreated.RemoveListener(OnRequestStarted);
            appVoiceExperience.events.OnPartialTranscription.RemoveListener(OnRequestTranscript);
            appVoiceExperience.events.OnFullTranscription.RemoveListener(OnRequestTranscript);
            appVoiceExperience.events.OnStartListening.RemoveListener(OnListenStart);
            appVoiceExperience.events.OnStoppedListening.RemoveListener(OnListenStop);
            appVoiceExperience.events.OnStoppedListeningDueToDeactivation.RemoveListener(OnListenForcedStop);
            appVoiceExperience.events.OnResponse.RemoveListener(OnRequestResponse);
            appVoiceExperience.events.OnError.RemoveListener(OnRequestError);
        }

        // Request began
        private void OnRequestStarted(WitRequest r)
        {
            // Store json on completion
            if (showJson) r.onRawResponse = (response) => textArea.text = response;
            // Begin
            _active = true;
        }
        // Request transcript
        private void OnRequestTranscript(string transcript)
        {
            textArea.text = transcript;
        }
        // Listen start
        private void OnListenStart()
        {
            textArea.text = "Listening...";
        }
        // Listen stop
        private void OnListenStop()
        {
            textArea.text = "Processing...";
        }
        // Listen stop
        private void OnListenForcedStop()
        {
            if (!showJson)
            {
                textArea.text = freshStateText;
            }
            OnRequestComplete();
        }
        // Request response
        private void OnRequestResponse(WitResponseNode response)
        {
            if (!showJson)
            {
                if (!string.IsNullOrEmpty(response["text"]))
                {
                    textArea.text = "I heard: " + response["text"];
                }
                else
                {
                    textArea.text = freshStateText;
                }
            }
            OnRequestComplete();
        }
        // Request error
        private void OnRequestError(string error, string message)
        {
            if (!showJson)
            {
                textArea.text = $"<color=\"red\">Error: {error}\n\n{message}</color>";
            }
            OnRequestComplete();
        }
        // Deactivate
        private void OnRequestComplete()
        {
            _active = false;
        }

        // Toggle activation
        public void ToggleActivation()
        {
            SetActivation(!_active);
        }
        // Set activation
        public void SetActivation(bool toActivated)
        {
            if (_active != toActivated)
            {
                _active = toActivated;
                if (_active)
                {
                    appVoiceExperience.Activate();
                }
                else
                {
                    appVoiceExperience.Deactivate();
                }
            }
        }
    }
}
