/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Facebook.WitAi.Lib;
using UnityEngine;

namespace Facebook.WitAi.Data.Entities
{
    [Serializable]
    public class WitEntityRole
    {
        [SerializeField] public string id;
        [SerializeField] public string name;

#if UNITY_EDITOR
        public static WitEntityRole FromJson(WitResponseNode roleNode)
        {
            return new WitEntityRole()
            {
                id = roleNode["id"],
                name = roleNode["name"]
            };
        }
#endif
    }
}
