/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Facebook.WitAi.Lib;

namespace Facebook.WitAi.Data.Entities
{
    [Serializable]
    public class WitEntityKeyword
    {
        public string keyword;
        public List<string> synonyms = new List<string>();

        public WitEntityKeyword() {}

        public WitEntityKeyword(string keyword)
        {
            this.keyword = keyword;
        }

        public WitEntityKeyword(string keyword, params string[] synonyms)
        {
            this.keyword = keyword;
            this.synonyms.AddRange(synonyms);
        }

        public WitEntityKeyword(string keyword, IEnumerable<string> synonyms)
        {
            this.keyword = keyword;
            this.synonyms.AddRange(synonyms);
        }

        public WitResponseClass AsJson
        {
            get
            {
                var synonymArray = new WitResponseArray();

                foreach (var synonym in synonyms)
                {
                    synonymArray.Add(synonym);
                }

                return new WitResponseClass
                {
                    {"keyword", new WitResponseData(keyword)},
                    {"synonyms", synonymArray}
                };
            }
        }

        public static WitEntityKeyword FromJson(WitResponseNode keywordNode)
        {
            return new WitEntityKeyword()
            {
                keyword = keywordNode["keyword"],
                synonyms = keywordNode["synonyms"].AsStringArray.ToList()
            };
        }
    }
}
