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
    public class WitDynamicEntitiesData : ScriptableObject, IDynamicEntitiesProvider
    {
        public WitDynamicEntities entities;
        public WitDynamicEntities GetDynamicEntities()
        {
            return entities;
        }
    }
}
