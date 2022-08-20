/*
 * Copyright (c) Facebook, Inc. and its affiliates.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Facebook.WitAi.TTS.Data;
using Facebook.WitAi.TTS.Integrations;
using TMPro;
using Facebook.WitAi.TTS.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace Facebook.WitAi.TTS.Samples
{
    public class TTSCacheToggle : MonoBehaviour
    {
        // UI references
        [SerializeField] private TTSDiskCache _diskCache;
        [SerializeField] private Button _button;

        // Add listeners
        private void Awake()
        {
            RefreshText();
            _button.onClick.AddListener(ToggleCache);
        }
        // Remove listeners
        private void OnDestroy()
        {
            _button.onClick.RemoveListener(ToggleCache);
        }
        // Toggle cache
        public void ToggleCache()
        {
            TTSDiskCacheLocation cacheLocation = _diskCache.DiskCacheDefaultSettings.DiskCacheLocation;
            switch (cacheLocation)
            {
                case TTSDiskCacheLocation.Stream:
                    cacheLocation = TTSDiskCacheLocation.Temporary;
                    break;
                case TTSDiskCacheLocation.Temporary:
                    cacheLocation = TTSDiskCacheLocation.Persistent;
                    break;
                case TTSDiskCacheLocation.Persistent:
                    cacheLocation = TTSDiskCacheLocation.Preload;
                    break;
                default:
                    cacheLocation = TTSDiskCacheLocation.Stream;
                    break;
            }
            _diskCache.DiskCacheDefaultSettings.DiskCacheLocation = cacheLocation;
            TTSService.Instance.UnloadAll();
            RefreshText();
        }
        // Refresh text
        private void RefreshText()
        {
            _button.GetComponentInChildren<TextMeshProUGUI>().text = "Disk Cache: " + (_diskCache == null ? "NULL" : _diskCache.DiskCacheDefaultSettings.DiskCacheLocation.ToString());
        }
    }
}
