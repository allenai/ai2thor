/*
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using Facebook.WitAi.Utilities;
using UnityEditor;
using UnityEngine;

namespace Facebook.WitAi.TTS.Editor.Preload
{
    [Serializable]
    public class TTSPreloadPhraseData
    {
        /// <summary>
        /// ID used to identify this phrase
        /// </summary>
        public string clipID;
        /// <summary>
        /// Actual phrase to be spoken
        /// </summary>
        public string textToSpeak;

        /// <summary>
        /// Meta data for whether clip is downloaded or not
        /// </summary>
        public bool downloaded;
        /// <summary>
        /// Meta data for clip download progress
        /// </summary>
        public float downloadProgress;
    }

    [Serializable]
    public class TTSPreloadVoiceData
    {
        /// <summary>
        /// Specific preset voice settings id to be used with TTSService
        /// </summary>
        public string presetVoiceID;
        /// <summary>
        /// All data corresponding to text to speak
        /// </summary>
        public TTSPreloadPhraseData[] phrases;
    }

    [Serializable]
    public class TTSPreloadData
    {
        public TTSPreloadVoiceData[] voices;
    }

    public class TTSPreloadSettings : ScriptableObject
    {
        [SerializeField] public TTSPreloadData data = new TTSPreloadData();
    }
}
