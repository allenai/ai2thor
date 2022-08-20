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
using Facebook.WitAi.Data.Traits;

namespace Facebook.WitAi.Windows
{
    public class WitTraitPropertyDrawer : WitPropertyDrawer
    {
        // Use name value for title if possible
        protected override string GetLocalizedText(SerializedProperty property, string key)
        {
            // Determine by ids
            switch (key)
            {
                case LocalizedTitleKey:
                    string title = GetFieldStringValue(property, "name");
                    if (!string.IsNullOrEmpty(title))
                    {
                        return title;
                    }
                    break;
                case "id":
                    return WitTexts.Texts.ConfigurationTraitsIdLabel;
                case "values":
                    return WitTexts.Texts.ConfigurationTraitsValuesLabel;
            }

            // Default to base
            return base.GetLocalizedText(property, key);
        }
        // Determine if should layout field
        protected override bool ShouldLayoutField(SerializedProperty property, FieldInfo subfield)
        {
            switch (subfield.Name)
            {
                case "name":
                    return false;
            }
            return base.ShouldLayoutField(property, subfield);
        }
    }
}
