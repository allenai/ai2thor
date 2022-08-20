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

namespace Facebook.WitAi.Data.Traits
{
    [Serializable]
    public class WitTrait : WitConfigurationData
    {
        [SerializeField] public string id;
        [SerializeField] public string name;
        [SerializeField] public WitTraitValue[] values;

        #if UNITY_EDITOR
        protected override WitRequest OnCreateRequest()
        {
            return witConfiguration.GetTraitRequest(name);
        }

        public override void UpdateData(WitResponseNode traitWitResponse)
        {
            id = traitWitResponse["id"].Value;
            name = traitWitResponse["name"].Value;
            var valueArray = traitWitResponse["values"].AsArray;
            var n = valueArray.Count;
            values = new WitTraitValue[n];
            for (int i = 0; i < n; i++) {
                values[i] = WitTraitValue.FromJson(valueArray[i]);
            }
        }

        public static WitTrait FromJson(WitResponseNode traitWitResponse)
        {
            var trait = new WitTrait();
            trait.UpdateData(traitWitResponse);
            return trait;
        }
        #endif
    }
}
