/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Facebook.WitAi;
using Facebook.WitAi.Configuration;
using Facebook.WitAi.Interfaces;
using UnityEngine;

namespace Facebook.WitAi
{
    public class Wit : VoiceService, IWitRuntimeConfigProvider
    {
        [SerializeField] private WitRuntimeConfiguration witRuntimeConfiguration;

        public WitRuntimeConfiguration RuntimeConfiguration
        {
            get => witRuntimeConfiguration;
            set => witRuntimeConfiguration = value;
        }

        private WitService witService;

        #region Voice Service Properties
        public override bool Active => null != witService && witService.Active;
        public override bool IsRequestActive => null != witService && witService.IsRequestActive;
        public override ITranscriptionProvider TranscriptionProvider
        {
            get => witService.TranscriptionProvider;
            set => witService.TranscriptionProvider = value;
        }
        public override bool MicActive => null != witService && witService.MicActive;
        protected override bool ShouldSendMicData => witRuntimeConfiguration.sendAudioToWit ||
                                                  null == TranscriptionProvider;
        #endregion

        #region Voice Service Methods

        public override void Activate()
        {
            witService.Activate();
        }

        public override void Activate(WitRequestOptions options)
        {
            witService.Activate(options);
        }

        public override void ActivateImmediately()
        {
            witService.ActivateImmediately();
        }

        public override void ActivateImmediately(WitRequestOptions options)
        {
            witService.ActivateImmediately(options);
        }

        public override void Deactivate()
        {
            witService.Deactivate();
        }

        public override void DeactivateAndAbortRequest()
        {
            witService.DeactivateAndAbortRequest();
        }

        public override void Activate(string text)
        {
            witService.Activate(text);
        }

        public override void Activate(string text, WitRequestOptions requestOptions)
        {
            witService.Activate(text, requestOptions);
        }

        #endregion

        protected override void Awake()
        {
            base.Awake();

            // WitService is 1:1 tied to a VoiceService. In the event there
            // are multiple voice services on a game object this will ensure
            // that this component has its own dedicated WitService
            witService = gameObject.AddComponent<WitService>();
            witService.VoiceEventProvider = this;
            witService.ConfigurationProvider = this;
        }
    }
}
