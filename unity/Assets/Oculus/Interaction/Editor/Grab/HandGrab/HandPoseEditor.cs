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
using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction.HandGrab.Editor
{
    [CustomPropertyDrawer(typeof(HandPose))]
    public class HandPoseEditor : PropertyDrawer
    {
        private bool _foldedFreedom = true;
        private bool _foldedRotations = false;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float multiplier = 4;

            if (_foldedFreedom)
            {
                multiplier += Constants.NUM_FINGERS;
            }
            if (_foldedRotations)
            {
                multiplier += FingersMetadata.HAND_JOINT_IDS.Length;
            }

            return EditorConstants.ROW_HEIGHT * multiplier;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            Rect labelPos = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            EditorGUI.indentLevel++;

            Rect rowRect = new Rect(position.x, labelPos.y + EditorConstants.ROW_HEIGHT, position.width, EditorConstants.ROW_HEIGHT);
            DrawFlagProperty<Handedness>(property, rowRect, "Handedness:", "_handedness", false);
            rowRect.y += EditorConstants.ROW_HEIGHT;
            rowRect = DrawFingersFreedomMenu(property, rowRect);
            rowRect = DrawJointAngles(property, rowRect);
            EditorGUI.indentLevel--;
            EditorGUI.EndProperty();
        }

        private Rect DrawFingersFreedomMenu(SerializedProperty property, Rect position)
        {
            _foldedFreedom = EditorGUI.Foldout(position, _foldedFreedom, "Fingers Freedom", true);
            position.y += EditorConstants.ROW_HEIGHT;
            if (_foldedFreedom)
            {
                SerializedProperty fingersFreedom = property.FindPropertyRelative("_fingersFreedom");
                EditorGUI.indentLevel++;
                for (int i = 0; i < Constants.NUM_FINGERS; i++)
                {
                    SerializedProperty finger = fingersFreedom.GetArrayElementAtIndex(i);
                    HandFinger fingerID = (HandFinger)i;
                    JointFreedom current = (JointFreedom)finger.intValue;
                    JointFreedom selected = (JointFreedom)EditorGUI.EnumPopup(position, $"{fingerID}", current);
                    finger.intValue = (int)selected;
                    position.y += EditorConstants.ROW_HEIGHT;
                }
                EditorGUI.indentLevel--;
            }
            return position;
        }

        private Rect DrawJointAngles(SerializedProperty property, Rect position)
        {
            _foldedRotations = EditorGUI.Foldout(position, _foldedRotations, "Joint Angles", true);
            position.y += EditorConstants.ROW_HEIGHT;
            if (_foldedRotations)
            {
                SerializedProperty jointRotations = property.FindPropertyRelative("_jointRotations");
                EditorGUI.indentLevel++;
                for (int i = 0; i < FingersMetadata.HAND_JOINT_IDS.Length; i++)
                {
                    SerializedProperty finger = jointRotations.GetArrayElementAtIndex(i);
                    HandJointId jointID = FingersMetadata.HAND_JOINT_IDS[i];
                    Vector3 current = finger.quaternionValue.eulerAngles;
                    Vector3 rotation = EditorGUI.Vector3Field(position, $"{jointID}", current);
                    finger.quaternionValue = Quaternion.Euler(rotation);
                    position.y += EditorConstants.ROW_HEIGHT;
                }
                EditorGUI.indentLevel--;
            }

            return position;
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
