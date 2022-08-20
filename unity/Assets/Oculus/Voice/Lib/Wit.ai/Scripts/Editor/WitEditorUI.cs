/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Facebook.WitAi.Data.Configuration;
using UnityEditor.PackageManager.UI;

namespace Facebook.WitAi
{
    public static class WitEditorUI
    {
        #region LABELS
        public static void LayoutLabel(string text)
        {
            LayoutLabel(text, WitStyles.Label);
        }
        public static void LayoutWrapLabel(string text)
        {
            LayoutLabel(text, WitStyles.LabelWrap);
        }
        public static void LayoutHeaderLabel(string text)
        {
            LayoutLabel(text, WitStyles.LabelHeader);
        }
        public static void LayoutSubheaderLabel(string text)
        {
            LayoutLabel(text, WitStyles.LabelSubheader);
        }
        public static void LayoutErrorLabel(string text)
        {
            LayoutLabel(text, WitStyles.LabelError);
        }
        public static void LayoutStatusLabel(string text)
        {
            EditorGUILayout.BeginVertical(WitStyles.LabelStatusBackground,
                GUILayout.ExpandWidth(true), GUILayout.Height(24));
            EditorGUILayout.LabelField(text, WitStyles.LabelStatus, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndVertical();
        }
        private static void LayoutLabel(string text, GUIStyle style)
        {
            EditorGUILayout.LabelField(text, style);
        }
        public static void LayoutKeyLabel(string key, string text)
        {
            EditorGUILayout.LabelField(key, text, WitStyles.TextField);
        }
        public static void LayoutKeyObjectLabels(string key, object obj)
        {
            // Null
            if (obj == null)
            {
                LayoutKeyLabel(key, "NULL");
                return;
            }
            // Foldout
            bool foldoutVoice = WitEditorUI.LayoutFoldout(new GUIContent(key), obj);
            if (!foldoutVoice)
            {
                return;
            }
            // Iterate fields
            EditorGUI.indentLevel++;
            foreach (var field in obj.GetType().GetFields())
            {
                LayoutKeyLabel(field.Name, field.GetValue(obj).ToString());
            }
            EditorGUI.indentLevel--;
        }
        #endregion

        #region BUTTONS
        public static bool LayoutTextButton(string text)
        {
            GUIContent content = new GUIContent(text);
            float width = WitStyles.TextButton.CalcSize(content).x + WitStyles.TextButtonPadding * 2f;
            return LayoutButton(content, WitStyles.TextButton, new GUILayoutOption[] { GUILayout.Width(width) });
        }
        public static bool LayoutIconButton(GUIContent icon)
        {
            return LayoutButton(icon, WitStyles.IconButton, null);
        }
        public static void LayoutTabButtons(string[] tabTitles, ref int selection)
        {
            if (tabTitles != null)
            {
                GUILayout.BeginHorizontal();
                for (int i = 0; i < tabTitles.Length; i++)
                {
                    GUI.enabled = selection != i;
                    if (LayoutTabButton(tabTitles[i]))
                    {
                        selection = i;
                    }
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
        }
        public static bool LayoutTabButton(string tabTitle)
        {
            return LayoutButton(new GUIContent(tabTitle), WitStyles.TabButton, null);
        }
        private static bool LayoutButton(GUIContent content, GUIStyle style, GUILayoutOption[] options)
        {
            return GUILayout.Button(content, style, options);
        }
        // Layout header button
        public static void LayoutHeaderButton(Texture2D headerTexture, string headerURL)
        {
            if (headerTexture != null)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                float maxWidth = EditorGUIUtility.currentViewWidth - WitStyles.WindowPaddingLeft - WitStyles.WindowPaddingRight - WitStyles.IconButton.CalcSize(WitStyles.HelpIcon).x;
                float headerWidth = Mathf.Min(WitStyles.HeaderWidth, maxWidth);
                float headerHeight = headerWidth * (float)headerTexture.height / (float)headerTexture.width;
                if (GUILayout.Button(headerTexture, WitStyles.HeaderButton, GUILayout.Width(headerWidth), GUILayout.Height(headerHeight)) && !string.IsNullOrEmpty(headerURL))
                {
                    Application.OpenURL(headerURL);
                }
                GUILayout.FlexibleSpace();
                if (LayoutIconButton(WitStyles.HelpIcon))
                {
                    Application.OpenURL(headerURL);
                }
                GUILayout.EndHorizontal();
            }
        }
        #endregion

        #region FOLDOUT
        // Foldout storage for bind objects
        private static Dictionary<string, bool> foldouts = new Dictionary<string, bool>();
        private static string GetFoldoutID(object bindObject)
        {
            return bindObject == null ? "" : bindObject.GetHashCode().ToString();
        }
        public static bool GetFoldoutValue(object bindObject)
        {
            string foldoutID = GetFoldoutID(bindObject);
            if (!string.IsNullOrEmpty(foldoutID) && foldouts.ContainsKey(foldoutID))
            {
                return foldouts[foldoutID];
            }
            return false;
        }
        public static void SetFoldoutValue(object bindObject, bool toFoldout)
        {
            string foldoutID = GetFoldoutID(bindObject);
            if (!string.IsNullOrEmpty(foldoutID))
            {
                foldouts[foldoutID] = toFoldout;
            }
        }
        public static bool LayoutFoldout(GUIContent key, object bindObject)
        {
            bool wasFoldout = GetFoldoutValue(bindObject);
            bool isFoldout = LayoutFoldout(key, wasFoldout);
            if (isFoldout != wasFoldout)
            {
                SetFoldoutValue(bindObject, isFoldout);
            }
            return isFoldout;
        }
        // GUI Layout handled without bind objects
        public static bool LayoutFoldout(GUIContent key, bool wasFoldout)
        {
            return EditorGUILayout.Foldout(wasFoldout, key, true, WitStyles.Foldout);
        }
        #endregion

        #region FIELDS
        public static void LayoutTextField(GUIContent key, ref string fieldValue, ref bool isUpdated)
        {
            // Ensure not null
            if (fieldValue == null)
            {
                fieldValue = string.Empty;
            }

            // Simple layout
            string newFieldValue;
            if (key == null)
            {
                newFieldValue = EditorGUILayout.TextField(fieldValue, WitStyles.TextField);
            }
            else
            {
                newFieldValue = EditorGUILayout.TextField(key, fieldValue, WitStyles.TextField);
            }

            // Update if changed
            if (!string.Equals(fieldValue, newFieldValue))
            {
                fieldValue = newFieldValue;
                isUpdated = true;
            }
        }
        public static void LayoutPasswordField(GUIContent key, ref string fieldValue, ref bool isUpdated)
        {
            // Ensure not null
            if (fieldValue == null)
            {
                fieldValue = string.Empty;
            }

            // Simple layout
            GUILayout.BeginHorizontal();
            string newFieldValue;
            if (key == null)
            {
                newFieldValue = EditorGUILayout.PasswordField(fieldValue, WitStyles.PasswordField);
            }
            else
            {
                newFieldValue = EditorGUILayout.PasswordField(key, fieldValue, WitStyles.PasswordField);
            }

            // Layout icon
            if (LayoutIconButton(WitStyles.PasteIcon))
            {
                newFieldValue = EditorGUIUtility.systemCopyBuffer;
                GUI.FocusControl(null);
            }
            GUILayout.EndHorizontal();

            // Update if changed
            if (!string.Equals(fieldValue, newFieldValue))
            {
                fieldValue = newFieldValue;
                isUpdated = true;
            }
        }
        public static void LayoutIntField(GUIContent key, ref int fieldValue, ref bool isUpdated)
        {
            // Simple layout
            int newFieldValue = EditorGUILayout.IntField(key, fieldValue, WitStyles.IntField);

            // Update
            if (fieldValue != newFieldValue)
            {
                fieldValue = newFieldValue;
                isUpdated = true;
            }
        }
        // Locked text field
        private static string unlockFieldId = "";
        private static string unlockFieldText = "";
        public static void LayoutLockedTextField(GUIContent key, ref string fieldValue, ref bool isUpdated)
        {
            // Determine if locked
            string fieldId = GetFoldoutID(key.text);
            bool isEditing = unlockFieldId.Equals(fieldId);

            // Begin Horizontal
            GUILayout.BeginHorizontal();

            // Hide if not editing
            GUI.enabled = isEditing;
            // Layout
            string newFieldValue = isEditing ? unlockFieldText : fieldValue;
            bool newUpdate = false;
            LayoutTextField(key, ref newFieldValue, ref newUpdate);
            // Allow next item
            GUI.enabled = true;

            // Updated
            if (newUpdate && isEditing)
            {
                unlockFieldText = newFieldValue;
            }

            // Cancel vs Apply Buttons
            if (isEditing)
            {
                if (LayoutIconButton(WitStyles.ResetIcon))
                {
                    unlockFieldId = string.Empty;
                    unlockFieldText = string.Empty;
                    GUI.FocusControl(null);
                }
                if (LayoutIconButton(WitStyles.AcceptIcon))
                {
                    if (!string.Equals(fieldValue, unlockFieldText))
                    {
                        fieldValue = unlockFieldText;
                        isUpdated = true;
                    }
                    unlockFieldId = string.Empty;
                    unlockFieldText = string.Empty;
                    GUI.FocusControl(null);
                }
            }
            // Edit button
            else
            {
                if (LayoutIconButton(WitStyles.EditIcon))
                {
                    unlockFieldId = fieldId;
                    unlockFieldText = fieldValue;
                }
            }

            // End Horizontal
            GUILayout.EndHorizontal();
        }
        #endregion

        #region MISCELANEOUS
        public static void LayoutToggle(GUIContent key, ref bool toggleValue, ref bool isUpdated)
        {
            // Simple layout
            bool newToggleValue = EditorGUILayout.Toggle(key, toggleValue, WitStyles.Toggle);

            // Update
            if (toggleValue != newToggleValue)
            {
                toggleValue = newToggleValue;
                isUpdated = true;
            }
        }
        public static void LayoutPopup(string key, string[] options, ref int selectionValue, ref bool isUpdated)
        {
            // Default
            int newSelectionValue = selectionValue;

            // No options
            if (options == null || options.Length == 0)
            {
                newSelectionValue = -1;
                EditorGUILayout.LabelField(key, "<color=FF0000>No Options</color>", WitStyles.Label);
            }
            // Single Option
            else if (options.Length == 1)
            {
                newSelectionValue = 0;
                EditorGUILayout.LabelField(key, options[0], WitStyles.Label);
            }
            // Popup Options
            else
            {
                newSelectionValue = EditorGUILayout.Popup(key, selectionValue, options, WitStyles.Popup);
            }

            // Update
            if (selectionValue != newSelectionValue)
            {
                selectionValue = newSelectionValue;
                isUpdated = true;
            }
        }
        public static bool LayoutSerializedObjectPopup(SerializedObject serializedObject, string propertyName, string[] names, ref int index)
        {
            // Get property
            var property = serializedObject.FindProperty(propertyName);
            // Get intent
            string intent;
            bool updated = false;
            if (names != null && names.Length > 0)
            {
                index = Mathf.Clamp(index, 0, names.Length);
                LayoutPopup(property.displayName, names, ref index, ref updated);
                intent = names[index];
            }
            else
            {
                intent = property.stringValue;
                LayoutTextField(new GUIContent(property.displayName), ref intent, ref updated);
            }

            // Success
            if (intent != property.stringValue)
            {
                property.stringValue = intent;
                return true;
            }

            // Failed
            return false;
        }
        #endregion

        #region WINDOW
        public static void LayoutWindow(string windowTitle, Texture2D windowHeader, string windowHeaderUrl, Action windowContentLayout, ref Vector2 offset, out Vector2 size)
        {
            // Get minimum width
            float minWidth = WitStyles.WindowMinWidth;

            // Begin Header
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Space(WitStyles.WindowPaddingLeft);
            GUILayout.BeginVertical();
            GUILayout.Space(WitStyles.WindowPaddingTop);
            // Layout header image
            if (windowHeader != null)
            {
                LayoutHeaderButton(windowHeader, windowHeaderUrl);
            }
            // Layout header label
            if (!string.IsNullOrEmpty(windowTitle))
            {
                LayoutHeaderLabel(windowTitle);
            }
            // End Header
            GUILayout.EndVertical();
            GUILayout.Space(WitStyles.WindowPaddingRight);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            // Begin Content
            GUILayout.BeginVertical();
            offset = GUILayout.BeginScrollView(offset);
            GUILayout.BeginHorizontal();
            GUILayout.Space(WitStyles.WindowPaddingLeft);
            GUILayout.BeginVertical(GUILayout.MinWidth(minWidth), GUILayout.MaxWidth(WitStyles.WindowMaxSize));
            // Layout content
            windowContentLayout?.Invoke();
            // End Content
            GUILayout.EndVertical();
            GUILayout.Space(WitStyles.WindowPaddingRight);
            GUILayout.EndHorizontal();
            GUILayout.Space(WitStyles.WindowPaddingBottom);
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            // Return size
            size = new Vector2(minWidth + WitStyles.WindowPaddingLeft + WitStyles.WindowPaddingRight + WitStyles.WindowScrollBarSize, WitStyles.WindowMinHeight);
        }
        #endregion
    }
}
