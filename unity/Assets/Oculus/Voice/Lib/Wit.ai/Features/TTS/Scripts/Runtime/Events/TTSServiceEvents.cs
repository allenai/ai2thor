/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Facebook.WitAi.TTS.Data;
using UnityEngine;
using UnityEngine.Events;

namespace Facebook.WitAi.TTS.Events
{
    [Serializable]
    public class TTSServiceEvents
    {
        [Tooltip("Called when a audio clip has been added to the runtime cache")]
        public TTSClipEvent OnClipCreated  = new TTSClipEvent();

        [Tooltip("Called when a audio clip has been removed from the runtime cache")]
        public TTSClipEvent OnClipUnloaded  = new TTSClipEvent();

        // Streaming events
        public TTSStreamEvents Stream = new TTSStreamEvents();

        // Download events
        public TTSDownloadEvents Download = new TTSDownloadEvents();
    }
}
