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

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction.PoseDetection.Editor
{
    public abstract class FeatureStateThresholdsEditor<TFeature> : UnityEditor.Editor
        where TFeature : unmanaged, Enum
    {
#region static helpers
        public static readonly TFeature[] FeatureEnumValues = (TFeature[])Enum.GetValues(typeof(TFeature));
        public static TFeature IntToFeature(int value)
        {
            return FeatureEnumValues[value];
        }

        public static int FeatureToInt(TFeature feature)
        {
            for (int i = 0; i < FeatureEnumValues.Length; i++)
            {
                TFeature enumVal = FeatureEnumValues[i];
                if (enumVal.Equals(feature))
                {
                    return i;
                }
            }

            throw new ArgumentOutOfRangeException();
        }
#endregion

#region Model Classes
        public class FeatureStateThresholdsModel
        {
            private readonly SerializedProperty _thresholdsProp;
            private readonly SerializedProperty _featureProp;
            public FeatureStateThresholdsModel(SerializedProperty self)
            {
                _featureProp = self.FindPropertyRelative("_feature");
                _thresholdsProp = self.FindPropertyRelative("_thresholds");
                Assert.IsNotNull(_featureProp);
                Assert.IsNotNull(_thresholdsProp);
            }

            public TFeature Feature
            {
                get => IntToFeature(_featureProp.enumValueIndex);
                set => _featureProp.enumValueIndex = FeatureToInt(value);
            }

            public SerializedProperty ThresholdsProp => _thresholdsProp;
        }

        public class FeatureStateThresholdModel
        {
            private readonly SerializedProperty _thresholdMidpointProp;
            private readonly SerializedProperty _thresholdWidthProp;
            private readonly SerializedProperty _firstStateProp;
            private readonly SerializedProperty _secondStateProp;

            public FeatureStateThresholdModel(SerializedProperty self)
            {
                _thresholdMidpointProp = self.FindPropertyRelative("_thresholdMidpoint");
                _thresholdWidthProp = self.FindPropertyRelative("_thresholdWidth");
                _firstStateProp = self.FindPropertyRelative("_firstState");
                _secondStateProp = self.FindPropertyRelative("_secondState");
                Assert.IsNotNull(_thresholdMidpointProp);
                Assert.IsNotNull(_thresholdWidthProp);
                Assert.IsNotNull(_firstStateProp);
                Assert.IsNotNull(_secondStateProp);
            }

            public float ThresholdMidpoint{
                get => _thresholdMidpointProp.floatValue;
                set { _thresholdMidpointProp.floatValue = value; }
            }
            public float ThresholdWidth {
                get => _thresholdWidthProp.floatValue;
                set { _thresholdWidthProp.floatValue = value; }
            }
            public string FirstStateId {
                get => _firstStateProp.stringValue;
                set => _firstStateProp.stringValue = value;
            }
            public string SecondStateId {
                get => _secondStateProp.stringValue;
                set => _secondStateProp.stringValue = value;
            }

            public float ToFirstWhenBelow => ThresholdMidpoint - ThresholdWidth * 0.5f;
            public float ToSecondWhenAbove => ThresholdMidpoint + ThresholdWidth * 0.5f;
        }
#endregion

        SerializedProperty _rootProperty;
        SerializedProperty _minTimeInStateProp;

        private readonly bool[] _featureVisible = new bool [FeatureEnumValues.Length];

        private readonly Color _visStateColorPro = new Color32(194, 194, 194, 255);
        private readonly Color _visStateColorLight = new Color32(56, 56, 56, 255);
        private readonly Color _visTransitionColorPro = new Color32(80, 80, 80, 255);
        private readonly Color _visTransitionColorLight = new Color32(160, 160, 160, 255);
        private readonly Color _visBorderColor = new Color32(0,0,0,255);
        private const float _visHeight = 20.0f;
        private const float _visMargin = 10.0f;

        private IReadOnlyDictionary<TFeature, FeatureDescription> _featureDescriptions;

        protected abstract IReadOnlyDictionary<TFeature, FeatureDescription> CreateFeatureDescriptions();

        void OnEnable()
        {
            if (_featureDescriptions == null)
            {
                _featureDescriptions = CreateFeatureDescriptions();
            }
            if (_featureDescriptions.Count != FeatureEnumValues.Length)
            {
                throw new InvalidOperationException(
                    "CreateFeatureDescriptions() must return one key for each enum value.");
            }

            _rootProperty = serializedObject.FindProperty("_featureThresholds");
            _minTimeInStateProp = serializedObject.FindProperty("_minTimeInState");

            for (var index = 0; index < _featureVisible.Length; index++)
            {
                _featureVisible[index] = true;
            }
        }

        public override void OnInspectorGUI()
        {
            if (_rootProperty == null || !_rootProperty.isArray || _minTimeInStateProp == null)
            {
                return;
            }

            EditorGUILayout.LabelField("All Features", EditorStyles.whiteLargeLabel);
            EditorGUILayout.PropertyField(_minTimeInStateProp);

            GUILayout.Space(10);
            EditorGUILayout.LabelField("Per Feature", EditorStyles.whiteLargeLabel);
            foreach (TFeature feature in FeatureEnumValues)
            {
                FeatureStateThresholdsModel foundFeatureProp = null;
                for (int i = 0; i < _rootProperty.arraySize; ++i)
                {
                    var featureThresholdsProp =
                        new FeatureStateThresholdsModel(
                            _rootProperty.GetArrayElementAtIndex(i));

                    if (featureThresholdsProp.Feature.Equals(feature))
                    {
                        foundFeatureProp = featureThresholdsProp;
                        break;
                    }
                }

                ref bool isVisible = ref _featureVisible[FeatureToInt(feature)];
                isVisible = EditorGUILayout.BeginFoldoutHeaderGroup(isVisible, $"{feature} Thresholds");
                if (!isVisible)
                {
                    EditorGUILayout.EndFoldoutHeaderGroup();
                    continue;
                }

                if (!IsFeatureThresholdsValid(foundFeatureProp))
                {
                    if (GUILayout.Button("Create Config"))
                    {
                        foundFeatureProp = CreateFeatureStateConfig(feature);
                    }
                    else
                    {
                        foundFeatureProp = null;
                    }
                }

                if (foundFeatureProp != null)
                {
                    RenderFeatureStates(feature, foundFeatureProp);
                }

                EditorGUILayout.EndFoldoutHeaderGroup();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private FeatureStateThresholdsModel CreateFeatureStateConfig(
            TFeature feature)
        {
            // Delete any old invalid configs for this feature.
            for (int i = 0; i < _rootProperty.arraySize;)
            {
                var model =
                    new FeatureStateThresholdsModel(
                        _rootProperty.GetArrayElementAtIndex(i));
                if (model.Feature.Equals(feature))
                {
                    _rootProperty.DeleteArrayElementAtIndex(i);
                }
                else
                {
                    i++;
                }
            }

            // Create a new config
            int insertIndex = _rootProperty.arraySize;
            _rootProperty.InsertArrayElementAtIndex(insertIndex);
            var featureStateThresholds = new FeatureStateThresholdsModel(
                _rootProperty.GetArrayElementAtIndex(insertIndex))
            {
                Feature = feature
            };

            // Set initial state
            ResetFeatureStates(featureStateThresholds);

            return featureStateThresholds;
        }

        private void ResetFeatureStates(FeatureStateThresholdsModel foundFeatureProp)
        {
            var states = _featureDescriptions[foundFeatureProp.Feature].FeatureStates;

            var thresholdsArrayProp = foundFeatureProp.ThresholdsProp;
            foundFeatureProp.ThresholdsProp.arraySize = states.Length - 1;
            var featureDescription = _featureDescriptions[foundFeatureProp.Feature];
            float minExpectedValue = featureDescription.MinValueHint;
            float maxExpectedValue = featureDescription.MaxValueHint;

            float range = maxExpectedValue - minExpectedValue;
            float initialWidth = range * 0.075f;
            float numStatesMultiplier = range / (states.Length);
            for (int stateIdx = 0; stateIdx < states.Length - 1; ++stateIdx)
            {
                var featureState = states[stateIdx];
                FeatureStateThresholdModel model = new FeatureStateThresholdModel(
                    thresholdsArrayProp.GetArrayElementAtIndex(stateIdx));
                model.ThresholdMidpoint = minExpectedValue + (stateIdx + 1) * numStatesMultiplier;
                model.ThresholdWidth = initialWidth;
                model.FirstStateId = featureState.Id;
                model.SecondStateId = states[stateIdx + 1].Id;
            }
        }

        private bool IsFeatureThresholdsValid(FeatureStateThresholdsModel foundFeatureModel)
        {
            if (foundFeatureModel == null)
            {
                return false;
            }

            var states = _featureDescriptions[foundFeatureModel.Feature].FeatureStates;
            if (foundFeatureModel.ThresholdsProp.arraySize != states.Length - 1)
            {
                return false;
            }

            for (var firstStateIdx = 0; firstStateIdx < states.Length - 1; firstStateIdx++)
            {
                var model = new FeatureStateThresholdModel(
                    foundFeatureModel.ThresholdsProp.GetArrayElementAtIndex(firstStateIdx));
                if (states[firstStateIdx].Id != model.FirstStateId ||
                    states[firstStateIdx + 1].Id != model.SecondStateId)
                {
                    return false;
                }
            }

            return true;
        }

        private void RenderFeatureStates(TFeature feature, FeatureStateThresholdsModel featureStateThresholdsModel)
        {
            FeatureDescription featureDescription = _featureDescriptions[feature];

            // Indent block
            using (new EditorGUI.IndentLevelScope())
            {
                RenderFeatureDescription(featureDescription);

                var states = _featureDescriptions[feature].FeatureStates;
                float minVal = float.MaxValue;
                float maxVal = float.MinValue;
                bool overlappingValues = false;
                float thresholdMaxWidth =
                    featureDescription.MaxValueHint - featureDescription.MinValueHint;
                for (var firstStateIdx = 0; firstStateIdx < states.Length - 1; firstStateIdx++)
                {
                    var firstState = states[firstStateIdx];
                    var secondState = states[firstStateIdx + 1];
                    EditorGUILayout.LabelField($"{firstState.Name} ⟷ {secondState.Name}", EditorStyles.label);

                    // Indent block
                    using (new EditorGUI.IndentLevelScope())
                    {
                        var model = new FeatureStateThresholdModel(
                            featureStateThresholdsModel.ThresholdsProp.GetArrayElementAtIndex(firstStateIdx));

                        if (model.ToFirstWhenBelow <= maxVal || model.ToSecondWhenAbove <= maxVal)
                        {
                            overlappingValues = true;
                        }

                        float thresholdMidpoint = model.ThresholdMidpoint;
                        float thresholdWidth = model.ThresholdWidth;

                        float newMidpoint = EditorGUILayout.FloatField("Midpoint", thresholdMidpoint);
                        float newWidth = EditorGUILayout.Slider("Width", thresholdWidth, 0.0f,
                            thresholdMaxWidth);

                        if (Math.Abs(newMidpoint - thresholdMidpoint) > float.Epsilon ||
                            Math.Abs(newWidth - thresholdWidth) > float.Epsilon)
                        {
                            // save new values.
                            model.ThresholdMidpoint = newMidpoint;
                            model.ThresholdWidth = newWidth;
                        }

                        minVal = Mathf.Min(minVal, model.ToFirstWhenBelow);
                        maxVal = Mathf.Max(maxVal, model.ToSecondWhenAbove);
                    }
                }

                float range = maxVal - minVal;
                if (range <= 0.0f)
                {
                    EditorGUILayout.HelpBox("Invalid threshold values", MessageType.Warning);
                }
                else
                {
                    if (overlappingValues)
                    {
                        EditorGUILayout.HelpBox("Overlapping threshold values",
                            MessageType.Warning);
                    }

                    RenderFeatureStateGraphic(featureStateThresholdsModel,
                        Mathf.Min(featureDescription.MinValueHint, minVal),
                        Mathf.Max(featureDescription.MaxValueHint, maxVal));
                }
            }
        }

        private void RenderFeatureDescription(FeatureDescription featureDescription)
        {
            if (!String.IsNullOrWhiteSpace(featureDescription.ShortDescription))
            {
                EditorGUILayout.HelpBox(featureDescription.ShortDescription, MessageType.Info);
            }

            EditorGUILayout.LabelField(
                new GUIContent("Expected value range", featureDescription.Description),
                new GUIContent($"[{featureDescription.MinValueHint}, {featureDescription.MaxValueHint}]"));
        }

        private void RenderFeatureStateGraphic(FeatureStateThresholdsModel prop, float minVal,
            float maxVal)
        {
            var lastRect = GUILayoutUtility.GetLastRect();
            float xOffset = lastRect.xMin + _visMargin;
            float widgetWidth = lastRect.width - _visMargin;

            GUILayout.Space(_visHeight + _visMargin * 2);

            EditorGUI.DrawRect(
                new Rect(xOffset - 1, lastRect.yMax + _visMargin - 1, widgetWidth + 2.0f,
                    _visHeight + 2.0f), _visBorderColor);

            float range = maxVal - minVal;
            Color stateColor = EditorGUIUtility.isProSkin
                ? _visStateColorPro
                : _visStateColorLight;
            Color transitionColor = EditorGUIUtility.isProSkin
                ? _visTransitionColorPro
                : _visTransitionColorLight;
            for (var firstStateIdx = 0;
                firstStateIdx < prop.ThresholdsProp.arraySize;
                firstStateIdx++)
            {
                var model = new FeatureStateThresholdModel(
                    prop.ThresholdsProp.GetArrayElementAtIndex(firstStateIdx));

                float firstPc = ((model.ToFirstWhenBelow - minVal)) / range;
                EditorGUI.DrawRect(
                    new Rect(Mathf.Floor(xOffset), lastRect.yMax + _visMargin,
                        Mathf.Ceil(widgetWidth * firstPc), _visHeight), stateColor);
                xOffset += widgetWidth * firstPc;
                minVal = model.ToFirstWhenBelow;

                float secondPc = ((model.ToSecondWhenAbove - minVal)) / range;
                EditorGUI.DrawRect(
                    new Rect(Mathf.Floor(xOffset), lastRect.yMax + _visMargin,
                        Mathf.Ceil(widgetWidth * secondPc), _visHeight), transitionColor);
                xOffset += widgetWidth * secondPc;
                minVal = model.ToSecondWhenAbove;
            }

            float lastPc = ((maxVal - minVal)) / range;
            EditorGUI.DrawRect(
                new Rect(Mathf.Floor(xOffset), lastRect.yMax + _visMargin,
                    Mathf.Ceil(widgetWidth * lastPc), _visHeight), stateColor);
        }
    }
}
