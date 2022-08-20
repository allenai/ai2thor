/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Oculus.Interaction.Editor;
using Oculus.Interaction.Input;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction.GrabAPI
{
    [CustomPropertyDrawer(typeof(GrabbingRule))]
    public class GrabbingRuleEditor : PropertyDrawer
    {
        private static Dictionary<string, bool> _unfolds = new Dictionary<string, bool>();

        private static readonly string[] FINGER_PROPERTY_NAMES = new string[]
        {
            "_thumbRequirement",
            "_indexRequirement",
            "_middleRequirement",
            "_ringRequirement",
            "_pinkyRequirement",
        };

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            InitializeUnfold(property);
            if (_unfolds[property.propertyPath])
            {
                return EditorConstants.ROW_HEIGHT * (Constants.NUM_FINGERS + 2);
            }
            else
            {
                return EditorConstants.ROW_HEIGHT * 1;
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            InitializeUnfold(property);
            Rect rowRect = new Rect(position.x, position.y, position.width, EditorConstants.ROW_HEIGHT);
            _unfolds[property.propertyPath] = EditorGUI.Foldout(rowRect, _unfolds[property.propertyPath], label, true);

            if (_unfolds[property.propertyPath])
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < Constants.NUM_FINGERS; i++)
                {
                    rowRect.y += EditorConstants.ROW_HEIGHT;
                    SerializedProperty finger = property.FindPropertyRelative(FINGER_PROPERTY_NAMES[i]);
                    HandFinger fingerID = (HandFinger)i;
                    FingerRequirement current = (FingerRequirement)finger.intValue;
                    FingerRequirement selected = (FingerRequirement)EditorGUI.EnumPopup(rowRect, $"{fingerID}: ", current);
                    finger.intValue = (int)selected;
                }

                rowRect.y += EditorConstants.ROW_HEIGHT;
                DrawFlagProperty<FingerUnselectMode>(property, rowRect, "Unselect Mode", "_unselectMode", false);
                EditorGUI.indentLevel--;
            }
            EditorGUI.EndProperty();
        }

        private void InitializeUnfold(SerializedProperty property)
        {
            if (!_unfolds.ContainsKey(property.propertyPath))
            {
                _unfolds.Add(property.propertyPath, false);
            }
        }

        private void DrawFlagProperty<TEnum>(SerializedProperty parentProperty, Rect position, string title, string fieldName, bool isFlags) where TEnum : Enum
        {
            SerializedProperty fieldProperty = parentProperty.FindPropertyRelative(fieldName);
            TEnum value = (TEnum)Enum.ToObject(typeof(TEnum), fieldProperty.intValue);
            Enum selectedValue = isFlags ?
                EditorGUI.EnumFlagsField(position, title, value)
                : EditorGUI.EnumPopup(position, title, value);
            fieldProperty.intValue = (int)Enum.ToObject(typeof(TEnum), selectedValue);
        }
    }
}
