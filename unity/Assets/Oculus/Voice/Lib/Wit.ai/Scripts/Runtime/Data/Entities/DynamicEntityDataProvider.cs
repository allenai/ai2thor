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
    public class DynamicEntityDataProvider : MonoBehaviour, IDynamicEntitiesProvider
    {
        [SerializeField] internal WitDynamicEntitiesData[] entitiesDefinition;
        public WitDynamicEntities GetDynamicEntities()
        {
            WitDynamicEntities entities = new WitDynamicEntities();
            foreach (var entity in entitiesDefinition)
            {
                entities.Merge(entity);
            }

            return entities;
        }
    }
}
