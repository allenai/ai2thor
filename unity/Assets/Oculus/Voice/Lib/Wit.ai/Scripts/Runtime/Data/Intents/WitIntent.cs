/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Facebook.WitAi.Configuration;
using Facebook.WitAi.Data.Entities;
using Facebook.WitAi.Lib;
using UnityEngine;

namespace Facebook.WitAi.Data.Intents
{

    [Serializable]
    public class WitIntent : WitConfigurationData
    {
        [SerializeField] public string id;
        [SerializeField] public string name;
        [SerializeField] public WitEntity[] entities;

        public static class Fields
        {
            public const string ID = "id";
            public const string NAME = "name";
            public const string CONFIDENCE = "confidence";
        }

        #if UNITY_EDITOR
        public static class EditorFields
        {
            public const string ENTITIES = "entities";
        }

        protected override WitRequest OnCreateRequest()
        {
            return witConfiguration.GetIntentRequest(name);
        }

        public override void UpdateData(WitResponseNode intentWitResponse)
        {
            id = intentWitResponse[Fields.ID].Value;
            name = intentWitResponse[Fields.NAME].Value;
            var entityArray = intentWitResponse[EditorFields.ENTITIES].AsArray;
            var n = entityArray.Count;
            entities = new WitEntity[n];
            for (int i = 0; i < n; i++)
            {
                entities[i] = WitEntity.FromJson(entityArray[i]);
                entities[i].witConfiguration = witConfiguration;
            }
        }

        public static WitIntent FromJson(WitResponseNode intentWitResponse)
        {
            var intent = new WitIntent();
            intent.UpdateData(intentWitResponse);
            return intent;
        }
        #endif
    }
}
