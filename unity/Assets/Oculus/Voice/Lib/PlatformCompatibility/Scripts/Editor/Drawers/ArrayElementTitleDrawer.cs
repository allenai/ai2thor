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

// Based off Unity forum post https://forum.unity.com/threads/how-to-change-the-name-of-list-elements-in-the-inspector.448910/

using UnityEditor;
using UnityEngine;

namespace Oculus.Voice.Core.Utilities
{
    [CustomPropertyDrawer(typeof(ArrayElementTitleAttribute))]
    public class ArrayElementTitleDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property,
            GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        protected virtual ArrayElementTitleAttribute Attribute
        {
            get { return (ArrayElementTitleAttribute) attribute; }
        }

        SerializedProperty titleNameProp;

        public override void OnGUI(Rect position,
            SerializedProperty property,
            GUIContent label)
        {
            string name = null;
            if (string.IsNullOrEmpty(Attribute.varname))
            {
                titleNameProp = property.serializedObject.FindProperty(property.propertyPath);
            }
            else
            {
                string fullPathName = property.propertyPath + "." + Attribute.varname;
                titleNameProp = property.serializedObject.FindProperty(fullPathName);
            }

            if (null != titleNameProp)
            {
                name = GetTitle();
            }

            if (string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(Attribute.fallbackName))
            {
                name = Attribute.fallbackName;
            }

            if (string.IsNullOrEmpty(name))
            {
                name = label.text;
            }

            EditorGUI.PropertyField(position, property, new GUIContent(name, label.tooltip), true);
        }

        private string GetTitle()
        {
            switch (titleNameProp.propertyType)
            {
                case SerializedPropertyType.Generic:
                    break;
                case SerializedPropertyType.Integer:
                    return titleNameProp.intValue.ToString();
                case SerializedPropertyType.Boolean:
                    return titleNameProp.boolValue.ToString();
                case SerializedPropertyType.Float:
                    return titleNameProp.floatValue.ToString();
                case SerializedPropertyType.String:
                    return titleNameProp.stringValue;
                case SerializedPropertyType.Color:
                    return titleNameProp.colorValue.ToString();
                case SerializedPropertyType.ObjectReference:
                    return titleNameProp.objectReferenceValue?.ToString();
                case SerializedPropertyType.LayerMask:
                    break;
                case SerializedPropertyType.Enum:
                    return titleNameProp.enumNames[titleNameProp.enumValueIndex];
                case SerializedPropertyType.Vector2:
                    return titleNameProp.vector2Value.ToString();
                case SerializedPropertyType.Vector3:
                    return titleNameProp.vector3Value.ToString();
                case SerializedPropertyType.Vector4:
                    return titleNameProp.vector4Value.ToString();
                case SerializedPropertyType.Rect:
                    break;
                case SerializedPropertyType.ArraySize:
                    break;
                case SerializedPropertyType.Character:
                    break;
                case SerializedPropertyType.AnimationCurve:
                    break;
                case SerializedPropertyType.Bounds:
                    break;
                case SerializedPropertyType.Gradient:
                    break;
                case SerializedPropertyType.Quaternion:
                    break;
                default:
                    break;
            }

            return "";
        }
    }
}
