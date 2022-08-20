/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEditor;
using UnityEngine;
using Facebook.WitAi.Data.Configuration;
using System.Reflection;

namespace Facebook.WitAi.Windows
{
    public class WitApplicationPropertyDrawer : WitPropertyDrawer
    {
        // Whether to use a foldout
        protected override bool FoldoutEnabled => false;
        // Use name value for title if possible
        protected override string GetLocalizedText(SerializedProperty property, string key)
        {
            // Determine by ids
            switch (key)
            {
                case LocalizedTitleKey:
                    return WitTexts.Texts.ConfigurationApplicationTabLabel;
                case LocalizedMissingKey:
                    return WitTexts.Texts.ConfigurationApplicationMissingLabel;
                case "name":
                    return WitTexts.Texts.ConfigurationApplicationNameLabel;
                case "id":
                    return WitTexts.Texts.ConfigurationApplicationIdLabel;
                case "lang":
                    return WitTexts.Texts.ConfigurationApplicationLanguageLabel;
                case "isPrivate":
                    return WitTexts.Texts.ConfigurationApplicationPrivateLabel;
                case "createdAt":
                    return WitTexts.Texts.ConfigurationApplicationCreatedLabel;
            }

            // Default to base
            return base.GetLocalizedText(property, key);
        }
        // Skip wit configuration field
        protected override bool ShouldLayoutField(SerializedProperty property, FieldInfo subfield)
        {
            switch (subfield.Name)
            {
                case "witConfiguration":
                    return false;
            }
            return base.ShouldLayoutField(property, subfield);
        }
    }
}
