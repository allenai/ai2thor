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
    public interface ITTSDiskCacheHandler
    {
        /// <summary>
        /// All events for streaming from the disk cache
        /// </summary>
        TTSStreamEvents DiskStreamEvents { get; set; }

        /// <summary>
        /// The default cache settings
        /// </summary>
        TTSDiskCacheSettings DiskCacheDefaultSettings { get; }

        /// <summary>
        /// A method for obtaining the path to a specific cache clip
        /// </summary>
        /// <param name="clipData">Clip request data</param>
        /// <returns>Returns the clip's cache path</returns>
        string GetDiskCachePath(TTSClipData clipData);

        /// <summary>
        /// Whether or not the clip data should be cached on disk
        /// </summary>
        /// <param name="clipData">Clip request data</param>
        /// <returns>Returns true if should cache</returns>
        bool ShouldCacheToDisk(TTSClipData clipData);

        /// <summary>
        /// Method for streaming from disk cache
        /// </summary>
        void StreamFromDiskCache(TTSClipData clipData);

        /// <summary>
        /// Method for cancelling a running cache load request
        /// </summary>
        /// <param name="clipID">Clip unique identifier</param>
        void CancelDiskCacheStream(TTSClipData clipData);
    }
}
