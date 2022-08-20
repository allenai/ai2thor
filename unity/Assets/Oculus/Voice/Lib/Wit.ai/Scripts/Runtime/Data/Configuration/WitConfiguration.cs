/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Facebook.WitAi.Configuration;
using Facebook.WitAi.Data.Entities;
using Facebook.WitAi.Data.Intents;
using Facebook.WitAi.Data.Traits;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Facebook.WitAi.Data.Configuration
{
    public class WitConfiguration : ScriptableObject
    {
        [HideInInspector]
        [SerializeField] public WitApplication application;
        [HideInInspector] [SerializeField] public string configId;

        /// <summary>
        /// Access token used in builds to make requests for data from Wit.ai
        /// </summary>
        [Tooltip("Access token used in builds to make requests for data from Wit.ai")]
        [SerializeField] public string clientAccessToken;

        [Tooltip("The number of milliseconds to wait before requests to Wit.ai will timeout")]
        [SerializeField] public int timeoutMS = 10000;

        /// <summary>
        /// Configuration parameters to set up a custom endpoint for testing purposes and request forwarding. The default values here will work for most.
        /// </summary>
        [Tooltip("Configuration parameters to set up a custom endpoint for testing purposes and request forwarding. The default values here will work for most.")]
        [SerializeField] public WitEndpointConfig endpointConfiguration = new WitEndpointConfig();

        [SerializeField] public WitEntity[] entities;
        [SerializeField] public WitIntent[] intents;
        [SerializeField] public WitTrait[] traits;

        [SerializeField] public bool isDemoOnly;

        public WitApplication Application => application;
        private void OnEnable()
        {
            #if UNITY_EDITOR
            if (string.IsNullOrEmpty(configId))
            {
                configId = GUID.Generate().ToString();
                EditorUtility.SetDirty(this);
            }
            #endif
        }
    }
}
