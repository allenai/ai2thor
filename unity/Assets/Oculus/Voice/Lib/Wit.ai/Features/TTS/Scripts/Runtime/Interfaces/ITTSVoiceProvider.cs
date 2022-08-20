/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using Facebook.WitAi.TTS.Data;

namespace Facebook.WitAi.TTS.Interfaces
{
    public interface ITTSVoiceProvider
    {
        /// <summary>
        /// Returns preset voice data if no voice data is selected.
        /// Useful for menu ai, etc.
        /// </summary>
        TTSVoiceSettings VoiceDefaultSettings { get; }

        /// <summary>
        /// Returns all preset voice settings
        /// </summary>
        TTSVoiceSettings[] PresetVoiceSettings { get; }

        /// <summary>
        /// Encode voice data to be transmitted
        /// </summary>
        /// <param name="voiceSettings">The voice settings class</param>
        /// <returns>Returns a dictionary with all variables</returns>
        Dictionary<string, string> EncodeVoiceSettings(TTSVoiceSettings voiceSettings);
    }
}
