/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace Facebook.WitAi.Windows
{
    // Handles layout of very simple property drawer
    public abstract class WitSimplePropertyDrawer : PropertyDrawer
    {
        // Get field names
        protected abstract string GetKeyFieldName();
        protected abstract string GetValueFieldName();

        // Remove padding
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 0;
        }
        // Handles gui layout
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            string keyText = GetFieldStringValue(property, GetKeyFieldName());
            string valueText = GetFieldStringValue(property, GetValueFieldName());
            WitEditorUI.LayoutKeyLabel(keyText, valueText);
        }
        // Get subfield value
        protected virtual string GetFieldStringValue(SerializedProperty property, string fieldName)
        {
            SerializedProperty subfieldProperty = property.FindPropertyRelative(fieldName);
            string result = GetFieldStringValue(subfieldProperty);
            if (string.IsNullOrEmpty(result))
            {
                result = fieldName;
            }
            return result;
        }
        // Get subfield value
        protected virtual string GetFieldStringValue(SerializedProperty subfieldProperty)
        {
            // Supported types
            switch (subfieldProperty.type)
            {
                case "string":
                    return subfieldProperty.stringValue;
                case "int":
                    return subfieldProperty.intValue.ToString();
                case "bool":
                    return subfieldProperty.boolValue.ToString();
            }
            // No others are currently supported
            return string.Empty;
        }
    }
}
