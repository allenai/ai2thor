/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using Facebook.WitAi.Interfaces;
using Facebook.WitAi.Lib;

namespace Facebook.WitAi.Data.Entities
{
    [Serializable]
    public class WitDynamicEntity : IDynamicEntitiesProvider
    {
        public string entity;
        public List<WitEntityKeyword> keywords = new List<WitEntityKeyword>();

        public WitDynamicEntity()
        {
        }

        public WitDynamicEntity(string entity, WitEntityKeyword keyword)
        {
            this.entity = entity;
            this.keywords.Add(keyword);
        }

        public WitDynamicEntity(string entity, params string[] keywords)
        {
            this.entity = entity;
            foreach (var keyword in keywords)
            {
                this.keywords.Add(new WitEntityKeyword(keyword));
            }
        }

        public WitDynamicEntity(string entity, Dictionary<string, List<string>> keywordsToSynonyms)
        {
            this.entity = entity;

            foreach (var synonym in keywordsToSynonyms)
            {
                keywords.Add(new WitEntityKeyword()
                {
                    keyword = synonym.Key,
                    synonyms = synonym.Value
                });

            }
        }

        public WitResponseArray AsJson
        {
            get
            {
                WitResponseArray synonymArray = new WitResponseArray();
                foreach (var keyword in keywords)
                {
                    synonymArray.Add(keyword.AsJson);
                }

                return synonymArray;
            }
        }

        public WitDynamicEntities GetDynamicEntities()
        {
            return new WitDynamicEntities()
            {
                entities = new List<WitDynamicEntity>
                {
                    this
                }
            };
        }
    }
}
