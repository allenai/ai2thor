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
    [CustomPropertyDrawer(typeof(JointVelocityActiveState.JointVelocityFeatureConfigList))]
    public class JointVelocityConfigListEditor : PropertyDrawer
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

    [CustomPropertyDrawer(typeof(JointVelocityActiveState.JointVelocityFeatureConfig))]
    public class JointVelocityFeatureConfigEditor : PropertyDrawer
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
            var headAxis = property.FindPropertyRelative("_headAxis");

            DrawControl(joint, "Joint", ref pos);
            DrawControl(relativeTo, "Relative To", ref pos);

            if ((JointVelocityActiveState.RelativeTo)relativeTo.enumValueIndex ==
                JointVelocityActiveState.RelativeTo.Hand)
            {
                DrawControl(handAxis, "Hand Axis", ref pos);
            }
            else if ((JointVelocityActiveState.RelativeTo)relativeTo.enumValueIndex ==
                     JointVelocityActiveState.RelativeTo.World)
            {
                DrawControl(worldAxis, "World Axis", ref pos);
            }
            else if ((JointVelocityActiveState.RelativeTo)relativeTo.enumValueIndex ==
                     JointVelocityActiveState.RelativeTo.Head)
            {
                DrawControl(headAxis, "Head Axis", ref pos);
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
