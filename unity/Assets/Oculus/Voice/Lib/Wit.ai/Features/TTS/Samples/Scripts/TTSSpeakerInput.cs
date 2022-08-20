/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEngine;
using TMPro;
using Facebook.WitAi.TTS.Utilities;

namespace Facebook.WitAi.TTS.Samples
{
    public class TTSSpeakerInput : MonoBehaviour
    {
        [SerializeField] private TTSSpeaker _speaker;
        [SerializeField] private TMP_InputField _input;

        // Either say the current phrase or stop talking/loading
        public void SayPhrase()
        {
            if (_speaker.IsLoading || _speaker.IsSpeaking)
            {
                _speaker.Stop();
            }
            else
            {
                _speaker.Speak(_input.text);
            }
        }
    }
}
