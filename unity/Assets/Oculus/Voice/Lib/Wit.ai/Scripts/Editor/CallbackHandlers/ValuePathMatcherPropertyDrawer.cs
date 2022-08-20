/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using Facebook.WitAi.Data;
using UnityEditor;
using UnityEngine;

namespace Facebook.WitAi.CallbackHandlers
{
    public class ValuePathMatcherPropertyDrawer : PropertyDrawer
    {
        private string currentEditPath;

        class Properties
        {
            public const string witValueRef = "witValueReference";
            public const string path = "path";
            public const string contentRequired = "contentRequired";
            public const string matchMethod = "matchMethod";
            public const string comparisonMethod = "comparisonMethod";
            public const string matchValue = "matchValue";

            public const string floatingPointComparisonTolerance =
                "floatingPointComparisonTolerance";
        }

        private Dictionary<string, bool> foldouts =
            new Dictionary<string, bool>();

        private string GetPropertyPath(SerializedProperty property)
        {
            var valueRefProp = property.FindPropertyRelative(Properties.witValueRef);
            if (valueRefProp.objectReferenceValue)
            {
                return ((WitValue) valueRefProp.objectReferenceValue).path;
            }
            return property.FindPropertyRelative(Properties.path).stringValue;
        }

        private bool IsEditingProperty(SerializedProperty property)
        {
            var path = GetPropertyPath(property);
            return path == currentEditPath || string.IsNullOrEmpty(path);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {

            float height = 0;

            // Path
            height += EditorGUIUtility.singleLineHeight;

            if (IsExpanded(property))
            {
                // Content Required
                height += EditorGUIUtility.singleLineHeight;
                // Match Method
                height += EditorGUIUtility.singleLineHeight;

                if (ComparisonMethodsVisible(property))
                {
                    // Comparison Method
                    height += EditorGUIUtility.singleLineHeight;
                }

                if (ComparisonValueVisible(property))
                {
                    // Comparison Value
                    height += EditorGUIUtility.singleLineHeight;
                }

                if (FloatingToleranceVisible(property))
                {
                    // Floating Point Tolerance
                    height += EditorGUIUtility.singleLineHeight;
                }

                height += 4;
            }

            return height;
        }

        private bool IsExpanded(SerializedProperty property)
        {
            return foldouts.TryGetValue(GetPropertyPath(property), out bool value) && value;
        }

        private bool Foldout(Rect rect, SerializedProperty property)
        {
            var path = GetPropertyPath(property);
            if (!foldouts.TryGetValue(path, out var value))
            {
                foldouts[path] = false;
            }

            foldouts[path] = EditorGUI.Foldout(rect, value, "");
            return foldouts[path];
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var rect = new Rect(position)
            {
                height = EditorGUIUtility.singleLineHeight
            };
            var path = property.FindPropertyRelative(Properties.path);

            var valueRefProp = property.FindPropertyRelative(Properties.witValueRef);
            var editIconWidth = 24;
            var pathRect = new Rect(rect);
            pathRect.width -= editIconWidth;
            var pathValue = GetPropertyPath(property);
            if (IsEditingProperty(property))
            {
                if (!valueRefProp.objectReferenceValue)
                {
                    pathRect.width -= WitStyles.IconButtonSize;
                    var value = EditorGUI.TextField(pathRect, path.stringValue);
                    if (value != path.stringValue)
                    {
                        path.stringValue = value;
                    }

                    pathRect.width += WitStyles.IconButtonSize;

                    var pickerRect = new Rect(pathRect)
                    {
                        x = pathRect.x + pathRect.width - 20,
                        width = 20
                    };
                    if (GUI.Button(pickerRect, WitStyles.ObjectPickerIcon, "Label"))
                    {
                        var id = EditorGUIUtility.GetControlID(FocusType.Passive) + 100;
                        EditorGUIUtility.ShowObjectPicker<WitValue>(
                            (WitValue) valueRefProp.objectReferenceValue, false, "", id);
                    }
                }
                else
                {
                    EditorGUI.PropertyField(pathRect, valueRefProp, new GUIContent());
                }

                if (Event.current.commandName == "ObjectSelectorClosed")
                {
                    valueRefProp.objectReferenceValue = EditorGUIUtility.GetObjectPickerObject();
                }

                pathValue = GetPropertyPath(property);
                if (pathValue != currentEditPath && null != currentEditPath)
                {
                    foldouts[currentEditPath] = false;
                    currentEditPath = GetPropertyPath(property);
                    foldouts[currentEditPath] = true;
                }
            }
            else
            {
                if (valueRefProp.objectReferenceValue)
                {
                    EditorGUI.LabelField(pathRect, valueRefProp.objectReferenceValue.name);
                }
                else
                {
                    EditorGUI.LabelField(pathRect, path.stringValue);
                }
            }

            var editRect = new Rect(rect)
            {
                x = pathRect.x + pathRect.width + 8
            };

            if (Foldout(rect, property))
            {
                if (GUI.Button(editRect, WitStyles.EditIcon, "Label"))
                {
                    if (currentEditPath == pathValue)
                    {
                        currentEditPath = null;
                    }
                    else
                    {
                        currentEditPath = pathValue;
                    }
                }

                rect.x += WitStyles.IconButtonSize;
                rect.width -= WitStyles.IconButtonSize;
                rect.y += rect.height;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(Properties.contentRequired));
                rect.y += rect.height;
                EditorGUI.PropertyField(rect, property.FindPropertyRelative(Properties.matchMethod));

                if (ComparisonMethodsVisible(property))
                {
                    rect.y += rect.height;
                    EditorGUI.PropertyField(rect,
                        property.FindPropertyRelative(Properties.comparisonMethod));
                }

                if (ComparisonValueVisible(property))
                {
                    rect.y += rect.height;
                    EditorGUI.PropertyField(rect,
                        property.FindPropertyRelative(Properties.matchValue));
                }

                if (FloatingToleranceVisible(property))
                {
                    rect.y += rect.height;
                    EditorGUI.PropertyField(rect,
                        property.FindPropertyRelative(Properties.floatingPointComparisonTolerance));
                }
            }
        }

        private bool ComparisonMethodsVisible(SerializedProperty property)
        {
            var matchedMethodProperty = property.FindPropertyRelative(Properties.matchMethod);
            return matchedMethodProperty.enumValueIndex > (int) MatchMethod.RegularExpression;
        }

        private bool ComparisonValueVisible(SerializedProperty property)
        {
            var matchedMethodProperty = property.FindPropertyRelative(Properties.matchMethod);
            return matchedMethodProperty.enumValueIndex > 0;
        }

        private bool FloatingToleranceVisible(SerializedProperty property)
        {
            var matchedMethodProperty = property.FindPropertyRelative(Properties.matchMethod);
            var comparisonMethodProperty =
                property.FindPropertyRelative(Properties.comparisonMethod);

            var comparisonMethod = comparisonMethodProperty.enumValueIndex;
            return matchedMethodProperty.enumValueIndex >= (int) MatchMethod.FloatComparison &&
                   (comparisonMethod == (int) ComparisonMethod.Equals || comparisonMethod == (int) ComparisonMethod.NotEquals);
        }
    }
}
