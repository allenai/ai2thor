/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Facebook.WitAi.Data.Configuration;
using Facebook.WitAi.Lib;
using UnityEngine;

namespace Facebook.WitAi.Configuration
{
    [Serializable]
    public abstract class WitConfigurationData
    {
        [SerializeField] public WitConfiguration witConfiguration;

        #if UNITY_EDITOR
        public void UpdateData(Action onUpdateComplete = null)
        {
            if (!witConfiguration)
            {
                onUpdateComplete?.Invoke();
                return;
            }

            var request = OnCreateRequest();
            request.onResponse = (r) => OnUpdateData(r, onUpdateComplete);
            request.Request();
        }

        protected abstract WitRequest OnCreateRequest();

        private void OnUpdateData(WitRequest request, Action onUpdateComplete)
        {
            if (request.StatusCode == 200)
            {
                UpdateData(request.ResponseData);
            }
            else
            {
                Debug.LogError(request.StatusDescription);
            }

            onUpdateComplete?.Invoke();
        }

        public abstract void UpdateData(WitResponseNode data);
        #endif
    }
}
