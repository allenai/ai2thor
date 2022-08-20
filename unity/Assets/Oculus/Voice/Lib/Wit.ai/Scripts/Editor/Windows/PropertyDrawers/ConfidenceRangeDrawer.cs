/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Facebook.WitAi.CallbackHandlers;

namespace Facebook.WitAi.Windows
{
    [CustomPropertyDrawer(typeof(ConfidenceRange))]
    public class ConfidenceRangeDrawer : WitPropertyDrawer
    {
        private Vector2 fieldScroll;
        private bool showOutsideConfidence;

        private Dictionary<SerializedProperty, bool> eventFoldouts =
            new Dictionary<SerializedProperty, bool>();

        private float GetEventContentsHeight(SerializedProperty property)
        {
            var height = EditorGUIUtility.singleLineHeight;
            var trigger = property.FindPropertyRelative("onWithinConfidenceRange");
            if (trigger.isExpanded)
            {
                height += EditorGUI.GetPropertyHeight(trigger);
                if (showOutsideConfidence)
                {
                    trigger = property.FindPropertyRelative("onOutsideConfidenceRange");
                    height += EditorGUI.GetPropertyHeight(trigger);
                }

                height += EditorGUIUtility.singleLineHeight * 1.5f;
            }

            return height;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = EditorGUIUtility.singleLineHeight * 4;
            height += GetEventContentsHeight(property);

            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var rect = position;
            rect.height = EditorGUIUtility.singleLineHeight;
            var minConfidence = property.FindPropertyRelative("minConfidence");
            var maxConfidence = property.FindPropertyRelative("maxConfidence");
            var minVal = minConfidence.floatValue;
            var maxVal = maxConfidence.floatValue;
            EditorGUI.MinMaxSlider(rect, ref minVal, ref maxVal, 0, 1);
            rect.y += EditorGUIUtility.singleLineHeight;


            var minRect = new Rect(rect);
            minRect.width = Mathf.Min(position.width / 2.0f - 4, 75f);

            EditorGUI.TextField(minRect, minVal.ToString());

            var maxRect = new Rect(minRect);
            maxRect.xMin = position.xMax - maxRect.width;
            maxRect.xMax = position.xMax;
            EditorGUI.TextField(maxRect, maxVal.ToString());

            rect.y += EditorGUIUtility.singleLineHeight * 1.5f;

            minConfidence.floatValue = minVal;
            maxConfidence.floatValue = maxVal;

            var eventRect = new Rect(rect);
            eventRect.height = GetEventContentsHeight(property);
            EditorGUI.DrawRect(eventRect, Color.gray);
            rect.xMin += 16;
            rect.width = rect.xMax - rect.xMin - 16;
            var trigger = property.FindPropertyRelative("onWithinConfidenceRange");
            trigger.isExpanded = EditorGUI.Foldout(rect, trigger.isExpanded, "Events");
            rect.y += EditorGUIUtility.singleLineHeight;
            if (trigger.isExpanded)
            {
                rect.height = EditorGUI.GetPropertyHeight(trigger);
                EditorGUI.PropertyField(rect, trigger);
                rect.y += rect.height;
                rect.height = EditorGUIUtility.singleLineHeight;
                showOutsideConfidence = EditorGUI.Foldout(rect, showOutsideConfidence,
                    "Outside Confidence Range Triggers");
                rect.y += EditorGUIUtility.singleLineHeight;
                if (showOutsideConfidence)
                {
                    trigger = property.FindPropertyRelative("onOutsideConfidenceRange");
                    rect.height = EditorGUI.GetPropertyHeight(trigger);
                    EditorGUI.PropertyField(rect, trigger);
                }
            }

            EditorGUI.EndProperty();
        }
    }
}
