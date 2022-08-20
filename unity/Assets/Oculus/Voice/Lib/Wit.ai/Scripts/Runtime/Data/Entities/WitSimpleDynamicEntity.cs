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
    public class WitSimpleDynamicEntity : MonoBehaviour, IDynamicEntitiesProvider
    {
        [SerializeField] private string entityName;
        [SerializeField] private string[] keywords;

        public WitDynamicEntities GetDynamicEntities()
        {
            var entity = new WitDynamicEntity(entityName, keywords);
            var entities = new WitDynamicEntities(entity);
            return entities;
        }
    }
}
