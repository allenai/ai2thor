/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;

namespace Facebook.WitAi.Data
{
    [Serializable]
    public class AudioEncoding
    {
        public enum Endian
        {
            Big,
            Little
        }

        /// <summary>
        /// The expected encoding of the mic pcm data
        /// </summary>
        public string encoding = "signed-integer";

        /// <summary>
        /// The number of bits per sample
        /// </summary>
        public int bits = 16;

        /// <summary>
        /// The sample rate used to capture audio
        /// </summary>
        public int samplerate = 16000;

        /// <summary>
        /// The endianess of the data
        /// </summary>
        public Endian endian = Endian.Little;

        public override string ToString()
        {
            return $"audio/raw;bits={bits};rate={samplerate / 1000}k;encoding={encoding};endian={endian.ToString().ToLower()}";
            ;
        }
    }
}
