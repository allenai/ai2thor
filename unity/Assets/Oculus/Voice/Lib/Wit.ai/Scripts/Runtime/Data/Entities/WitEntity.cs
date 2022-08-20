/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Facebook.WitAi.Configuration;
using Facebook.WitAi.Lib;
using UnityEngine;

namespace Facebook.WitAi.Data.Entities
{

    [Serializable]
    public class WitEntity : WitConfigurationData
    {
        [SerializeField] public string id;
        [SerializeField] public string name;
        [SerializeField] public string[] lookups;
        [SerializeField] public WitEntityRole[] roles;
        [SerializeField] public WitEntityKeyword[] keywords;

        public static class Fields
        {
            public const string ID = "id";
            public const string NAME = "name";
            public const string ROLE= "role";

            public const string START = "start";
            public const string END = "end";

            public const string TYPE = "type";

            public const string BODY = "body";
            public const string VALUE = "value";
            public const string CONFIDENCE = "confidence";

            public const string ENTITIES = "entities";

            public const string LOOKUPS = "lookups";
            public const string ROLES = "roles";
            public const string KEYWORDS = "keywords";
        }

#if UNITY_EDITOR
        protected override WitRequest OnCreateRequest()
        {
            return witConfiguration.GetEntityRequest(name);
        }

        public override void UpdateData(WitResponseNode entityWitResponse)
        {
            id = entityWitResponse[Fields.ID].Value;
            name = entityWitResponse[Fields.NAME].Value;
            lookups = entityWitResponse[Fields.LOOKUPS].AsStringArray;
            var roleArray = entityWitResponse[Fields.ROLES].AsArray;
            roles = new WitEntityRole[roleArray.Count];
            for (int i = 0; i < roleArray.Count; i++)
            {
                roles[i] = WitEntityRole.FromJson(roleArray[i]);
            }
            var keywordArray = entityWitResponse[Fields.KEYWORDS].AsArray;
            keywords = new WitEntityKeyword[keywordArray.Count];
            for (int i = 0; i < keywordArray.Count; i++)
            {
                keywords[i] = WitEntityKeyword.FromJson(keywordArray[i]);
            }
        }

        public static WitEntity FromJson(WitResponseNode entityWitResponse)
        {
            var entity = new WitEntity();
            entity.UpdateData(entityWitResponse);
            return entity;
        }
        #endif
    }
}
