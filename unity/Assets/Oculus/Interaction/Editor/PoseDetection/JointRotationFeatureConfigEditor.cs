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

using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction.PoseDetection.Editor
{
    [CustomPropertyDrawer(typeof(JointRotationActiveState.JointRotationFeatureConfigList))]
    public class JointRotationConfigListEditor : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property.FindPropertyRelative("_values"));
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.PropertyField(position, property.FindPropertyRelative("_values"), new GUIContent("Joints"), true);
            EditorGUI.EndProperty();
        }
    }

    [CustomPropertyDrawer(typeof(JointRotationActiveState.JointRotationFeatureConfig))]
    public class JointRotationFeatureConfigEditor : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return GetLineHeight() * 3;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Rect pos = new Rect(position.x, position.y, position.width,
                EditorGUIUtility.singleLineHeight);

            EditorGUI.BeginProperty(position, label, property);
            var joint = property.FindPropertyRelative("_feature");
            var relativeTo = property.FindPropertyRelative("_relativeTo");
            var handAxis = property.FindPropertyRelative("_handAxis");
            var worldAxis = property.FindPropertyRelative("_worldAxis");

            DrawControl(joint, "Joint", ref pos);
            DrawControl(relativeTo, "Relative To", ref pos);

            if ((JointRotationActiveState.RelativeTo)relativeTo.enumValueIndex ==
                JointRotationActiveState.RelativeTo.Hand)
            {
                DrawControl(handAxis, "Hand Axis", ref pos);
            }
            else
            {
                DrawControl(worldAxis, "World Axis", ref pos);
            }

            EditorGUI.EndProperty();
        }

        private void DrawControl(SerializedProperty property, string name, ref Rect position)
        {
            EditorGUI.PropertyField(position, property, new GUIContent(name));
            position.y += GetLineHeight();
        }

        private float GetLineHeight()
        {
            return EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        }
    }
}
