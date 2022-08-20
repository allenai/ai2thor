/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEditor;
using UnityEngine;
using System.Reflection;
using Facebook.WitAi.Data.Entities;

namespace Facebook.WitAi.Windows
{
    [CustomPropertyDrawer(typeof(WitEntityRole))]
    public class WitEntityRolePropertyDrawer : WitSimplePropertyDrawer
    {
        // Key = Name
        protected override string GetKeyFieldName()
        {
            return "name";
        }
        // Value = ID
        protected override string GetValueFieldName()
        {
            return "id";
        }
    }
}
