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
using Facebook.WitAi.Configuration;
using Facebook.WitAi.Interfaces;
#if UNITY_ANDROID
using Oculus.Voice.Bindings.Android;
#endif
using Oculus.Voice.Interfaces;
using Oculus.VoiceSDK.Utilities;
using UnityEngine;

namespace Oculus.Voice
{
    [HelpURL("https://developer.oculus.com/experimental/voice-sdk/tutorial-overview/")]
    public class AppVoiceExperience : VoiceService, IWitRuntimeConfigProvider
    {
        [SerializeField] private WitRuntimeConfiguration witRuntimeConfiguration;
        [Tooltip("Uses platform services to access wit.ai instead of accessing wit directly from within the application.")]
        [SerializeField] private bool usePlatformServices;

        public WitRuntimeConfiguration RuntimeConfiguration
        {
            get => witRuntimeConfiguration;
            set => witRuntimeConfiguration = value;
        }

        private IPlatformVoiceService platformService;
        private IVoiceService voiceServiceImpl;

        private bool Initialized => null != voiceServiceImpl;

        #region Voice Service Properties
        public override bool Active => null != voiceServiceImpl && voiceServiceImpl.Active;
        public override bool IsRequestActive => null != voiceServiceImpl && voiceServiceImpl.IsRequestActive;
        public override ITranscriptionProvider TranscriptionProvider
        {
            get => voiceServiceImpl.TranscriptionProvider;
            set => voiceServiceImpl.TranscriptionProvider = value;

        }
        public override bool MicActive => null != voiceServiceImpl && voiceServiceImpl.MicActive;
        protected override bool ShouldSendMicData => witRuntimeConfiguration.sendAudioToWit ||
                                                  null == TranscriptionProvider;
        #endregion

        #if UNITY_ANDROID && !UNITY_EDITOR
        public bool HasPlatformIntegrations => usePlatformServices;
        #else
        public bool HasPlatformIntegrations => false;
        #endif

        #region Voice Service Methods

        public override void Activate()
        {
            voiceServiceImpl.Activate();
        }

        public override void Activate(WitRequestOptions options)
        {
            voiceServiceImpl.Activate(options);
        }

        public override void ActivateImmediately()
        {
            voiceServiceImpl.ActivateImmediately();
        }

        public override void ActivateImmediately(WitRequestOptions options)
        {
            voiceServiceImpl.ActivateImmediately(options);
        }

        public override void Deactivate()
        {
            voiceServiceImpl.Deactivate();
        }

        public override void DeactivateAndAbortRequest()
        {
            voiceServiceImpl.DeactivateAndAbortRequest();
        }

        public override void Activate(string text)
        {
            voiceServiceImpl.Activate(text);
        }

        public override void Activate(string text, WitRequestOptions requestOptions)
        {
            voiceServiceImpl.Activate(text, requestOptions);
        }

        #endregion

        private void InitVoiceSDK()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (HasPlatformIntegrations)
            {
                Debug.Log("Checking platform capabilities...");
                var platformImpl = new VoiceSDKImpl(this);
                platformImpl.OnServiceNotAvailableEvent += () => RevertToWitUnity();
                platformImpl.Connect();
                platformImpl.SetRuntimeConfiguration(RuntimeConfiguration);
                if (platformImpl.PlatformSupportsWit)
                {
                    voiceServiceImpl = platformImpl;

                    if (voiceServiceImpl is Wit wit)
                    {
                        wit.RuntimeConfiguration = witRuntimeConfiguration;
                    }

                    voiceServiceImpl.VoiceEvents = VoiceEvents;
                }
                else
                {
                    Debug.Log("Platform registration indicated platform support is not currently available.");
                    RevertToWitUnity();
                }
            }
            else
            {
                RevertToWitUnity();
            }
#else
            RevertToWitUnity();
#endif
        }

        private void RevertToWitUnity()
        {
            Wit w = GetComponent<Wit>();
            if (null == w)
            {
                w = gameObject.AddComponent<Wit>();
                w.hideFlags = HideFlags.HideInInspector;
            }
            voiceServiceImpl = w;

            if (voiceServiceImpl is Wit wit)
            {
                wit.RuntimeConfiguration = witRuntimeConfiguration;
            }

            voiceServiceImpl.VoiceEvents = VoiceEvents;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (MicPermissionsManager.HasMicPermission())
            {
                InitVoiceSDK();
            }
            else
            {
                MicPermissionsManager.RequestMicPermission();
            }

            #if UNITY_ANDROID && !UNITY_EDITOR
            platformService?.SetRuntimeConfiguration(witRuntimeConfiguration);
            #endif
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            #if UNITY_ANDROID
            if (voiceServiceImpl is VoiceSDKImpl platformImpl)
            {
                platformImpl.Disconnect();
            }
            #endif
            voiceServiceImpl = null;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && !Initialized)
            {
                if (MicPermissionsManager.HasMicPermission())
                {
                    InitVoiceSDK();
                }
            }
        }
    }
}
