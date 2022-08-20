/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Linq;
using Facebook.WitAi.Data.Configuration;
using UnityEditor;
using UnityEngine;

namespace Facebook.WitAi.CallbackHandlers
{
    [CustomEditor(typeof(SimpleIntentHandler))]
    public class SimpleIntentHandlerEditor : Editor
    {
        private SimpleIntentHandler handler;
        private string[] intentNames;
        private int intentIndex;

        private void OnEnable()
        {
            handler = target as SimpleIntentHandler;
        }

        public override void OnInspectorGUI()
        {
            if (!handler.wit)
            {
                GUILayout.Label(
                    "Wit component is not present in the scene. Add wit to scene to get intent and entity suggestions.",
                    EditorStyles.helpBox);
            }

            if (handler && handler.wit && null == intentNames)
            {
                if (handler.wit is IWitRuntimeConfigProvider provider && null != provider.RuntimeConfiguration && provider.RuntimeConfiguration.witConfiguration)
                {
                    provider.RuntimeConfiguration.witConfiguration.RefreshData();
                    intentNames = provider.RuntimeConfiguration.witConfiguration.intents.Select(i => i.name).ToArray();
                    intentIndex = Array.IndexOf(intentNames, handler.intent);
                }
            }

            WitEditorUI.LayoutSerializedObjectPopup(serializedObject, "intent",
                intentNames, ref intentIndex);


            var confidenceProperty = serializedObject.FindProperty("confidence");
            EditorGUILayout.PropertyField(confidenceProperty);

            GUILayout.Space(16);

            var allowConfidenceOverlap = serializedObject.FindProperty("allowConfidenceOverlap");
            EditorGUILayout.PropertyField(allowConfidenceOverlap);

            var confidenceRanges = serializedObject.FindProperty("confidenceRanges");
            EditorGUILayout.PropertyField(confidenceRanges);

            GUILayout.Space(16);

            var eventProperty = serializedObject.FindProperty("onIntentTriggered");
            EditorGUILayout.PropertyField(eventProperty);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
