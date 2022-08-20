/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using Facebook.WitAi.Interfaces;
using Facebook.WitAi.Lib;
using UnityEngine;

namespace Facebook.WitAi.Data.Entities
{
    [Serializable]
    public class WitDynamicEntities : IDynamicEntitiesProvider, IEnumerable<WitDynamicEntity>
    {
        public List<WitDynamicEntity> entities = new List<WitDynamicEntity>();

        public WitDynamicEntities()
        {

        }

        public WitDynamicEntities(IEnumerable<WitDynamicEntity> entity)
        {
            entities.AddRange(entity);
        }

        public WitDynamicEntities(params WitDynamicEntity[] entity)
        {
            entities.AddRange(entity);
        }

        public WitResponseClass AsJson
        {
            get
            {
                WitResponseClass json = new WitResponseClass();
                foreach (var entity in entities)
                {
                    json.Add(entity.entity, entity.AsJson);
                }

                return json;
            }
        }

        public override string ToString()
        {
            return AsJson.ToString();
        }

        public IEnumerator<WitDynamicEntity> GetEnumerator() => entities.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public WitDynamicEntities GetDynamicEntities()
        {
            return this;
        }

        public void Merge(IDynamicEntitiesProvider provider)
        {
            if (null == provider) return;

            entities.AddRange(provider.GetDynamicEntities());
        }

        public void Merge(IEnumerable<WitDynamicEntity> mergeEntities)
        {
            if (null == mergeEntities) return;

            entities.AddRange(mergeEntities);
        }

        public void Add(WitDynamicEntity dynamicEntity)
        {
            int index = entities.FindIndex(e => e.entity == dynamicEntity.entity);
            if(index < 0) entities.Add(dynamicEntity);
            else Debug.LogWarning($"Cannot add entity, registry already has an entry for {dynamicEntity.entity}");
        }

        public void Remove(WitDynamicEntity dynamicEntity)
        {
            entities.Remove(dynamicEntity);
        }

        public void AddKeyword(string entityName, WitEntityKeyword keyword)
        {
            var entity = entities.Find(e => entityName == e.entity);
            if (null == entity)
            {
                entity = new WitDynamicEntity(entityName);
                entities.Add(entity);
            }
            entity.keywords.Add(keyword);
        }

        public void RemoveKeyword(string entityName, WitEntityKeyword keyword)
        {
            int index = entities.FindIndex(e => e.entity == entityName);
            if (index >= 0)
            {
                entities[index].keywords.Remove(keyword);
                if(entities[index].keywords.Count == 0) entities.RemoveAt(index);
            }
        }
    }
}
