/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Facebook.WitAi.TTS.Data;
using Facebook.WitAi.TTS.Interfaces;
using Facebook.WitAi.TTS.Events;

namespace Facebook.WitAi.TTS.Integrations
{
    // A simple LRU Cache
    public class TTSRuntimeCache : MonoBehaviour, ITTSRuntimeCacheHandler
    {
        /// <summary>
        /// Whether or not to unload clip data after the clip capacity is hit
        /// </summary>
        [Header("Runtime Cache Settings")]
        [Tooltip("Whether or not to unload clip data after the clip capacity is hit")]
        [FormerlySerializedAs("_clipLimit")]
        public bool ClipLimit = true;

        /// <summary>
        /// The maximum clips allowed in the runtime cache
        /// </summary>
        [Tooltip("The maximum clips allowed in the runtime cache")]
        [FormerlySerializedAs("_clipCapacity")]
        [Min(1)] public int ClipCapacity = 20;

        /// <summary>
        /// Whether or not to unload clip data after the ram capacity is hit
        /// </summary>
        [Tooltip("Whether or not to unload clip data after the ram capacity is hit")]
        [FormerlySerializedAs("_ramLimit")]
        public bool RamLimit = true;

        /// <summary>
        /// The maximum amount of RAM allowed in the runtime cache.  In KBs
        /// </summary>
        [Tooltip("The maximum amount of RAM allowed in the runtime cache.  In KBs")]
        [FormerlySerializedAs("_ramCapacity")]
        [Min(1)] public int RamCapacity = 32768;

        /// <summary>
        /// On clip added callback
        /// </summary>
        public TTSClipEvent OnClipAdded { get; set; } = new TTSClipEvent();
        /// <summary>
        /// On clip removed callback
        /// </summary>
        public TTSClipEvent OnClipRemoved { get; set; } = new TTSClipEvent();

        // Clips & their ids
        private Dictionary<string, TTSClipData> _clips = new Dictionary<string, TTSClipData>();
        private List<string> _clipOrder = new List<string>();

        /// <summary>
        /// Simple getter for all clips
        /// </summary>
        public TTSClipData[] GetClips() => _clips.Values.ToArray();

        /// <summary>
        /// Getter for a clip that also moves clip to the back of the queue
        /// </summary>
        public TTSClipData GetClip(string clipID)
        {
            // Id not found
            if (!_clips.ContainsKey(clipID))
            {
                return null;
            }

            // Sort to end
            int clipIndex = _clipOrder.IndexOf(clipID);
            _clipOrder.RemoveAt(clipIndex);
            _clipOrder.Add(clipID);

            // Return clip
            return _clips[clipID];
        }

        /// <summary>
        /// Add clip to cache and ensure it is most recently referenced
        /// </summary>
        /// <param name="clipData"></param>
        public void AddClip(TTSClipData clipData)
        {
            // Do not add null
            if (clipData == null)
            {
                return;
            }
            // Remove from order
            bool wasAdded = true;
            int clipIndex = _clipOrder.IndexOf(clipData.clipID);
            if (clipIndex != -1)
            {
                wasAdded = false;
                _clipOrder.RemoveAt(clipIndex);
            }

            // Add clip
            _clips[clipData.clipID] = clipData;
            // Add to end of order
            _clipOrder.Add(clipData.clipID);

            // Evict least recently used clips
            while (IsCacheFull() && _clipOrder.Count > 0)
            {
                RemoveClip(_clipOrder[0]);
            }

            // Call add delegate
            if (wasAdded && _clips.Keys.Count > 0)
            {
                OnClipAdded?.Invoke(clipData);
            }
        }

        /// <summary>
        /// Remove clip from cache immediately
        /// </summary>
        /// <param name="clipID"></param>
        public void RemoveClip(string clipID)
        {
            // Id not found
            if (!_clips.ContainsKey(clipID))
            {
                return;
            }

            // Remove from dictionary
            TTSClipData clipData = _clips[clipID];
            _clips.Remove(clipID);

            // Remove from order list
            int clipIndex = _clipOrder.IndexOf(clipID);
            _clipOrder.RemoveAt(clipIndex);

            // Call remove delegate
            OnClipRemoved?.Invoke(clipData);
        }

        /// <summary>
        /// Check if cache is full
        /// </summary>
        protected bool IsCacheFull()
        {
            // Capacity full
            if (ClipLimit && _clipOrder.Count > ClipCapacity)
            {
                return true;
            }
            // Ram full
            if (RamLimit && GetCacheDiskSize() > RamCapacity)
            {
                return true;
            }
            // Free
            return false;
        }
        /// <summary>
        /// Get RAM size of cache in KBs
        /// </summary>
        /// <returns>Returns size in KBs rounded up</returns>
        public int GetCacheDiskSize()
        {
            long total = 0;
            foreach (var key in _clips.Keys)
            {
                total += GetClipBytes(_clips[key].clip);
            }
            return (int)(total / (long)1024) + 1;
        }
        // Return bytes occupied by clip
        public static long GetClipBytes(AudioClip clip)
        {
            if (clip == null)
            {
                return 0;
            }
            return ((clip.samples * clip.channels) * 2);
        }
    }
}
