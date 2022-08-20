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
using Facebook.WitAi.Configuration;
using Facebook.WitAi.Events;
using Facebook.WitAi.Interfaces;
using Oculus.Voice.Core.Bindings.Android;
using Oculus.Voice.Interfaces;
using Debug = UnityEngine.Debug;

namespace Oculus.Voice.Bindings.Android
{
    public class VoiceSDKImpl : BaseAndroidConnectionImpl<VoiceSDKBinding>,
        IPlatformVoiceService, IVCBindingEvents
    {
        private bool _isServiceAvailable = true;
        public Action OnServiceNotAvailableEvent;
        private IVoiceService _baseVoiceService;

        private bool _isActive;

        public VoiceSDKImpl(IVoiceService baseVoiceService) : base(
            "com.oculus.assistant.api.unity.immersivevoicecommands.UnityIVCServiceFragment")
        {
            _baseVoiceService = baseVoiceService;
        }

        public bool PlatformSupportsWit => service.PlatformSupportsWit && _isServiceAvailable;

        public bool Active => service.Active && _isActive;
        public bool IsRequestActive => service.IsRequestActive;
        public bool MicActive => service.MicActive;
        public void SetRuntimeConfiguration(WitRuntimeConfiguration configuration)
        {
            service.SetRuntimeConfiguration(configuration);
        }

        private VoiceSDKListenerBinding eventBinding;

        public ITranscriptionProvider TranscriptionProvider { get; set; }

        public override void Connect()
        {
            base.Connect();
            eventBinding = new VoiceSDKListenerBinding(this, this);
            eventBinding.VoiceEvents.OnStoppedListening.AddListener(OnStoppedListening);
            service.SetListener(eventBinding);
            service.Connect();
            Debug.Log(
                $"Platform integration initialization complete. Platform integrations are {(PlatformSupportsWit ? "active" : "inactive")}");
        }

        public override void Disconnect()
        {
            base.Disconnect();
            if (null != eventBinding)
            {
                eventBinding.VoiceEvents.OnStoppedListening.RemoveListener(OnStoppedListening);
            }
        }

        private void OnStoppedListening()
        {
            _isActive = false;
        }

        public void Activate(string text)
        {
            service.Activate(text);
        }

        public void Activate(string text, WitRequestOptions requestOptions)
        {
            service.Activate(text, requestOptions);
        }

        public void Activate()
        {
            if (_isActive) return;

            _isActive = true;
            service.Activate();
        }

        public void Activate(WitRequestOptions requestOptions)
        {
            if (_isActive) return;

            _isActive = true;
            service.Activate(requestOptions);
        }

        public void ActivateImmediately()
        {
            if (_isActive) return;

            _isActive = true;
            service.ActivateImmediately();
        }

        public void ActivateImmediately(WitRequestOptions requestOptions)
        {
            if (_isActive) return;

            _isActive = true;
            service.ActivateImmediately(requestOptions);
        }

        public void Deactivate()
        {
            _isActive = false;
            service.Deactivate();
        }

        public void DeactivateAndAbortRequest()
        {
            _isActive = false;
            service.Deactivate();
        }

        public void OnServiceNotAvailable(string error, string message)
        {
            _isActive = false;
            _isServiceAvailable = false;
            OnServiceNotAvailableEvent?.Invoke();
        }

        public VoiceEvents VoiceEvents
        {
            get => _baseVoiceService.VoiceEvents;
            set => _baseVoiceService.VoiceEvents = value;
        }
    }
}
