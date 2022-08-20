/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Facebook.WitAi.TTS.Data;
using Facebook.WitAi.TTS.Events;

namespace Facebook.WitAi.TTS.Interfaces
{
    public interface ITTSWebHandler
    {
        /// <summary>
        /// Streaming events
        /// </summary>
        TTSStreamEvents WebStreamEvents { get; set; }

        /// <summary>
        /// Method for determining if text to speak is valid
        /// </summary>
        /// <param name="textToSpeak">Text to be spoken by TTS</param>
        /// <returns>Invalid error</returns>
        string IsTextValid(string textToSpeak);

        /// <summary>
        /// Method for performing a web load request
        /// </summary>
        /// <param name="clipData">Clip request data</param>
        void RequestStreamFromWeb(TTSClipData clipData);

        /// <summary>
        /// Cancel web stream
        /// </summary>
        /// <param name="clipID">Clip unique identifier</param>
        bool CancelWebStream(TTSClipData clipData);

        /// <summary>
        /// Download events
        /// </summary>
        TTSDownloadEvents WebDownloadEvents { get; set; }

        /// <summary>
        /// Method for performing a web load request
        /// </summary>
        /// <param name="clipData">Clip request data</param>
        /// <param name="downloadPath">Path to save clip</param>
        void RequestDownloadFromWeb(TTSClipData clipData, string downloadPath);

        /// <summary>
        /// Cancel web download
        /// </summary>
        /// <param name="clipID">Clip unique identifier</param>
        /// <param name="downloadPath">Path to save clip</param>
        bool CancelWebDownload(TTSClipData clipData, string downloadPath);
    }
}
