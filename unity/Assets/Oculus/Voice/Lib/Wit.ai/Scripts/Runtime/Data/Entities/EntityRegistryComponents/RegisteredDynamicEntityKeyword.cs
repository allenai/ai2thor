/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEngine;

namespace Facebook.WitAi.Data.Entities
{
    /// <summary>
    /// A configured dynamic entity meant to be placed on dynamic objects.
    /// when the object is enabled this entity will be registered with active
    /// voice services on activation.
    /// </summary>
    public class RegisteredDynamicEntityKeyword : MonoBehaviour
    {
        [SerializeField] private string entity;
        [SerializeField] private WitEntityKeyword keyword;

        private void OnEnable()
        {
            if (null == keyword) return;
            if (string.IsNullOrEmpty(entity)) return;

            if (DynamicEntityKeywordRegistry.HasDynamicEntityRegistry)
            {
                DynamicEntityKeywordRegistry.Instance.RegisterDynamicEntity(entity, keyword);
            }
            else
            {
                Debug.LogWarning($"No dynamic entity registry in the scene. Cannot register {name}.");
            }
        }

        private void OnDisable()
        {
            if (null == keyword) return;
            if (string.IsNullOrEmpty(entity)) return;

            if (DynamicEntityKeywordRegistry.HasDynamicEntityRegistry && null != keyword)
            {
                DynamicEntityKeywordRegistry.Instance.UnregisterDynamicEntity(entity, keyword);
            }
        }
    }
}
