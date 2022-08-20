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
    // Edit Type
    public enum WitPropertyEditType
    {
        NoEdit,
        FreeEdit,
        LockEdit
    }

    // Handles layout of of property sub properties
    public abstract class WitPropertyDrawer : PropertyDrawer
    {
        // Whether editing
        private int editIndex = -1;
        // Whether to use a foldout
        protected virtual bool FoldoutEnabled => true;
        // Determine edit type for this drawer
        protected virtual WitPropertyEditType EditType => WitPropertyEditType.NoEdit;

        // Remove padding
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 0;
        }

        // Handles gui layout
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Return error
            if (property.serializedObject == null)
            {
                string missingText = GetLocalizedText(property, LocalizedMissingKey);
                WitEditorUI.LayoutErrorLabel(missingText);
                return;
            }

            // Show foldout if desired
            string titleText = GetLocalizedText(property, LocalizedTitleKey);
            if (FoldoutEnabled)
            {
                property.isExpanded = WitEditorUI.LayoutFoldout(new GUIContent(titleText), property.isExpanded);
                if (!property.isExpanded)
                {
                    return;
                }
            }
            // Show title only
            else
            {
                WitEditorUI.LayoutLabel(titleText);
            }

            // Indent
            GUILayout.BeginVertical();
            EditorGUI.indentLevel++;

            // Pre fields
            OnGUIPreFields(position, property, label);

            // Iterate all subfields
            WitPropertyEditType editType = EditType;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
            Type fieldType = fieldInfo.FieldType;
            if (fieldType.IsArray)
            {
                fieldType = fieldType.GetElementType();
            }
            FieldInfo[] subfields = fieldType.GetFields(flags);
            for (int s = 0; s < subfields.Length; s++)
            {
                FieldInfo subfield = subfields[s];
                if (ShouldLayoutField(property, subfield))
                {
                    LayoutField(s, property, subfield, editType);
                }
            }

            // Post fields
            OnGUIPostFields(position, property, label);

            // Undent
            EditorGUI.indentLevel--;
            GUILayout.EndVertical();
        }
        // Override pre fields
        protected virtual void OnGUIPreFields(Rect position, SerializedProperty property, GUIContent label)
        {

        }
        // Draw a specific property
        protected virtual void LayoutField(int index, SerializedProperty property, FieldInfo subfield, WitPropertyEditType editType)
        {
            // Begin layout
            GUILayout.BeginHorizontal();

            // Get label content
            string labelText = GetLocalizedText(property, subfield.Name);
            GUIContent labelContent = new GUIContent(labelText);

            // Determine if can edit
            bool canEdit = editType == WitPropertyEditType.FreeEdit || (editType == WitPropertyEditType.LockEdit && editIndex == index);
            bool couldEdit = GUI.enabled;
            GUI.enabled = canEdit;

            // Cannot edit, just show field
            SerializedProperty subfieldProperty = property.FindPropertyRelative(subfield.Name);
            if (!canEdit && subfieldProperty.type == "string")
            {
                // Get value text
                string valText = subfieldProperty.stringValue;
                if (string.IsNullOrEmpty(valText))
                {
                    valText = GetDefaultFieldValue(property, subfield);
                }

                // Layout key
                WitEditorUI.LayoutKeyLabel(labelText, valText);
            }
            // Can edit, allow edit
            else
            {
                GUILayout.BeginVertical();
                LayoutPropertyField(subfield, subfieldProperty, labelContent, canEdit);
                GUILayout.EndVertical();
            }

            // Reset
            GUI.enabled = couldEdit;

            // Lock Settings
            if (editType == WitPropertyEditType.LockEdit)
            {
                // Is Editing
                if (editIndex == index)
                {
                    // Clear Edit
                    if (WitEditorUI.LayoutIconButton(WitStyles.ResetIcon))
                    {
                        editIndex = -1;
                        string clearVal = "";
                        if (subfieldProperty.type != "string")
                        {
                            clearVal = GetDefaultFieldValue(property, subfield);
                        }
                        SetFieldStringValue(subfieldProperty, clearVal);
                        GUI.FocusControl(null);
                    }
                    // Accept Edit
                    if (WitEditorUI.LayoutIconButton(WitStyles.AcceptIcon))
                    {
                        editIndex = -1;
                        GUI.FocusControl(null);
                    }
                }
                // Not Editing
                else
                {
                    // Begin Editing
                    if (WitEditorUI.LayoutIconButton(WitStyles.EditIcon))
                    {
                        editIndex = index;
                        GUI.FocusControl(null);
                    }
                }
            }

            // End layout
            GUILayout.EndHorizontal();
        }
        // Layout property field
        protected virtual void LayoutPropertyField(FieldInfo subfield, SerializedProperty subfieldProperty,  GUIContent labelContent, bool canEdit)
        {
            // If can edit or not array default layout
            if (canEdit || !subfield.FieldType.IsArray || subfieldProperty.arraySize <= 0)
            {
                EditorGUILayout.PropertyField(subfieldProperty, labelContent);
                return;
            }

            // If cannot edit, handle here
            subfieldProperty.isExpanded = WitEditorUI.LayoutFoldout(labelContent, subfieldProperty.isExpanded);
            if (subfieldProperty.isExpanded)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < subfieldProperty.arraySize; i++)
                {
                    SerializedProperty p = subfieldProperty.GetArrayElementAtIndex(i);
                    EditorGUILayout.PropertyField(p);
                }
                EditorGUI.indentLevel--;
            }
        }
        // Override post fields
        protected virtual void OnGUIPostFields(Rect position, SerializedProperty property, GUIContent label)
        {

        }
        // Get text for specified key
        public const string LocalizedTitleKey = "title";
        public const string LocalizedMissingKey = "missing";
        protected virtual string GetLocalizedText(SerializedProperty property, string key)
        {
            return property.displayName;
        }
        // Way to ignore certain properties
        protected virtual bool ShouldLayoutField(SerializedProperty property, FieldInfo subfield)
        {
            switch (subfield.Name)
            {
                case "witConfiguration":
                    return false;
            }
            return true;
        }
        // Get field default value if applicable
        protected virtual string GetDefaultFieldValue(SerializedProperty property, FieldInfo subfield)
        {
            return string.Empty;
        }
        // Get subfield value
        protected virtual string GetFieldStringValue(SerializedProperty property, string fieldName)
        {
            SerializedProperty subfieldProperty = property.FindPropertyRelative(fieldName);
            return GetFieldStringValue(subfieldProperty);
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
        // Set subfield value
        protected virtual void SetFieldStringValue(SerializedProperty subfieldProperty, string newFieldValue)
        {
            // Supported types
            switch (subfieldProperty.type)
            {
                case "string":
                    subfieldProperty.stringValue = newFieldValue;
                    break;
                case "int":
                    int rI;
                    if (int.TryParse(newFieldValue, out rI))
                    {
                        subfieldProperty.intValue = rI;
                    }
                    break;
                case "bool":
                    bool rB;
                    if (bool.TryParse(newFieldValue, out rB))
                    {
                        subfieldProperty.boolValue = rB;
                    }
                    break;
            }
        }
    }
}
