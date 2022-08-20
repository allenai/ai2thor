/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Facebook.WitAi.Interfaces;
using UnityEngine;

namespace Facebook.WitAi.Data.Entities
{
    /// <summary>
    /// Singleton registry for tracking any objects owned defined in entities in
    /// a scene
    /// </summary>
    public class DynamicEntityKeywordRegistry : MonoBehaviour, IDynamicEntitiesProvider
    {
        private static DynamicEntityKeywordRegistry instance;

        private WitDynamicEntities entities = new WitDynamicEntities();

        public static bool HasDynamicEntityRegistry => Instance;

        /// <summary>
        /// Gets the instance in the scene if there is one
        /// </summary>
        public static DynamicEntityKeywordRegistry Instance
        {
            get
            {
                if (!instance)
                {
                    instance = FindObjectOfType<DynamicEntityKeywordRegistry>();
                }

                return instance;
            }
        }

        private void OnEnable()
        {
            instance = this;
        }

        private void OnDisable()
        {
            instance = null;
        }

        public void RegisterDynamicEntity(string entity, WitEntityKeyword keyword)
        {
            entities.AddKeyword(entity, keyword);
        }

        public void UnregisterDynamicEntity(string entity, WitEntityKeyword keyword)
        {
            entities.RemoveKeyword(entity, keyword);
        }

        public WitDynamicEntities GetDynamicEntities()
        {
            return entities;
        }
    }
}
