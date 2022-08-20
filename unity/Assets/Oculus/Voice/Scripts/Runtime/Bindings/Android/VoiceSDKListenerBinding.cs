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

using System;
using Facebook.WitAi;
using Facebook.WitAi.Events;
using Facebook.WitAi.Lib;
using UnityEngine;

namespace Oculus.Voice.Bindings.Android
{
    public class VoiceSDKListenerBinding : AndroidJavaProxy
    {
        private IVoiceService _voiceService;
        private readonly IVCBindingEvents _bindingEvents;

        public VoiceEvents VoiceEvents => _voiceService.VoiceEvents;

        public VoiceSDKListenerBinding(IVoiceService voiceService, IVCBindingEvents bindingEvents) : base(
            "com.oculus.assistant.api.voicesdk.immersivevoicecommands.IVCEventsListener")
        {
            _voiceService = voiceService;
            _bindingEvents = bindingEvents;
        }

        public void onResponse(string response)
        {
            var responseNode = WitResponseJson.Parse(response);
            var transcription = responseNode["text"];
            VoiceEvents.onFullTranscription?.Invoke(transcription);
            VoiceEvents.OnResponse?.Invoke(responseNode);
        }

        public void onPartialResponse(string response)
        {
            var responseNode = WitResponseJson.Parse(response);
            VoiceEvents.onPartialTranscription?.Invoke(responseNode["text"]);
        }

        public void onError(string error, string message, string errorBody)
        {
            VoiceEvents.OnError?.Invoke(error, message);
        }

        public void onAborted()
        {
            VoiceEvents.OnAborted?.Invoke();
        }

        public void onRequestCompleted()
        {
            VoiceEvents.OnRequestCompleted?.Invoke();
        }

        public void onMicLevelChanged(float level)
        {
            VoiceEvents.OnMicLevelChanged?.Invoke(level);
        }

        public void onRequestCreated()
        {
            VoiceEvents.OnRequestCreated?.Invoke(null);
        }

        public void onStartListening()
        {
            VoiceEvents.OnStartListening?.Invoke();
        }

        public void onStoppedListening(int reason)
        {
            VoiceEvents.OnStoppedListening?.Invoke();
        }

        public void onMicDataSent()
        {
            VoiceEvents.OnMicDataSent?.Invoke();
        }

        public void onMinimumWakeThresholdHit()
        {
            VoiceEvents.OnMinimumWakeThresholdHit?.Invoke();
        }

        public void onPartialTranscription(string transcription)
        {
            VoiceEvents.OnPartialTranscription?.Invoke(transcription);
        }

        public void onFullTranscription(string transcription)
        {
            VoiceEvents.OnFullTranscription?.Invoke(transcription);
        }

        public void onServiceNotAvailable(string error, string message)
        {
            Debug.LogWarning($"Platform service is not available: {error} - {message}");
            _bindingEvents.OnServiceNotAvailable(error, message);
        }
    }
}
