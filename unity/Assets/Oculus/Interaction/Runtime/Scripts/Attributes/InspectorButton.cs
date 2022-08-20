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

using UnityEngine;
using System.Reflection;
using System;
using UnityEditor;

namespace Oculus.Interaction
{
    [AttributeUsage(AttributeTargets.Field)]
    public class InspectorButtonAttribute : PropertyAttribute
    {
        private const float BUTTON_WIDTH = 80;
        private const float BUTTON_HEIGHT = 20;

        public float ButtonWidth { get; set; } = BUTTON_WIDTH;

        public readonly string methodName;
        public readonly float buttonHeight;

        public InspectorButtonAttribute(string methodName)
        {
            this.methodName = methodName;
            this.buttonHeight = BUTTON_HEIGHT;
        }
        public InspectorButtonAttribute(string methodName, float buttonHeight)
        {
            this.methodName = methodName;
            this.buttonHeight = buttonHeight;
        }
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(InspectorButtonAttribute))]
    public class InspectorButtonPropertyDrawer : PropertyDrawer
    {
        private MethodInfo _method = null;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            InspectorButtonAttribute inspectorButtonAttribute = (InspectorButtonAttribute)attribute;
            return inspectorButtonAttribute.buttonHeight;
        }

        public override void OnGUI(Rect positionRect, SerializedProperty prop, GUIContent label)
        {
            InspectorButtonAttribute inspectorButtonAttribute = (InspectorButtonAttribute)attribute;
            Rect rect = positionRect;
            rect.height = inspectorButtonAttribute.buttonHeight;
            if (GUI.Button(rect, label.text))
            {
                Type eventType = prop.serializedObject.targetObject.GetType();
                string eventName = inspectorButtonAttribute.methodName;
                if (_method == null)
                {
                    _method = eventType.GetMethod(eventName,
                        BindingFlags.Public
                        | BindingFlags.NonPublic
                        | BindingFlags.Instance
                        | BindingFlags.Static);
                }
                _method?.Invoke(prop.serializedObject.targetObject, null);
            }
        }
    }
#endif
}
