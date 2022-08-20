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
    public class TTSClipDownloadEvent : UnityEvent<TTSClipData, string>
    {
    }
    [Serializable]
    public class TTSClipDownloadErrorEvent : UnityEvent<TTSClipData, string, string>
    {
    }

    [Serializable]
    public class TTSDownloadEvents
    {
        [Tooltip("Called when a audio clip download begins")]
        public TTSClipDownloadEvent OnDownloadBegin = new TTSClipDownloadEvent();

        [Tooltip("Called when a audio clip is downloaded successfully")]
        public TTSClipDownloadEvent OnDownloadSuccess = new TTSClipDownloadEvent();

        [Tooltip("Called when a audio clip downloaded has been cancelled")]
        public TTSClipDownloadEvent OnDownloadCancel = new TTSClipDownloadEvent();

        [Tooltip("Called when a audio clip downloaded has failed")]
        public TTSClipDownloadErrorEvent OnDownloadError = new TTSClipDownloadErrorEvent();
    }
}
