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

using Oculus.Interaction.PoseDetection.Editor.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Oculus.Interaction.PoseDetection.Editor
{
    namespace Model
    {
        public class FeatureConfigList
        {
            private readonly SerializedProperty _root;
            private uint _flags;
            private readonly IReadOnlyDictionary<int, FeatureDescription> _featureDescriptions;

            public FeatureConfigList(SerializedProperty root,
                IReadOnlyDictionary<int, FeatureDescription> featureDescriptions)
            {
                Assert.IsNotNull(root);

                _root = root;
                _flags = GetFlags(_root);
                _featureDescriptions = featureDescriptions;
            }

            private uint GetFlags(SerializedProperty root)
            {
                if (!root.isArray)
                {
                    return 0U;
                }

                uint flags = 0U;
                for (int i = 0; i < root.arraySize; i++)
                {
                    var elemProp = root.GetArrayElementAtIndex(i);
                    var featureProp = elemProp.FindPropertyRelative("_feature");
                    flags |= 1U << featureProp.enumValueIndex;
                }

                return flags;
            }

            public uint Flags
            {
                get => _flags;
                set
                {
                    uint flagsToCreate = value;
                    for (int i = 0; i < _root.arraySize;)
                    {
                        var elemProp = _root.GetArrayElementAtIndex(i);
                        var featureProp = elemProp.FindPropertyRelative("_feature");
                        uint propFlags = (1U << featureProp.enumValueIndex);
                        if ((flagsToCreate & propFlags) == 0U)
                        {
                            // Feature is in list, but not flags... delete list entry.
                            _root.DeleteArrayElementAtIndex(i);
                        }
                        else
                        {
                            // Feature is in list, and in flags; remove from list of things we need to create.
                            flagsToCreate &= ~propFlags;
                            i++;
                        }
                    }

                    // Create missing elements.
                    foreach (var feature in _featureDescriptions.Keys)
                    {
                        uint flags = 1U << feature;
                        if ((flagsToCreate & flags) == 0U)
                        {
                            continue;
                        }

                        var lastIndex = _root.arraySize;
                        _root.InsertArrayElementAtIndex(lastIndex);
                        var model = new FeatureConfig(_root.GetArrayElementAtIndex(lastIndex));
                        model.Feature = feature;
                        model.FeatureState = _featureDescriptions[feature].FeatureStates[0].Id;
                    }

                    _flags = value;
                }
            }

            public IEnumerable<FeatureConfig> ConfigModels
            {
                get
                {
                    List<FeatureConfig> models = new List<FeatureConfig>(_root.arraySize);
                    for (int i = 0; i < _root.arraySize; i++)
                    {
                        models.Add(new FeatureConfig(_root.GetArrayElementAtIndex(i)));
                    }

                    return models;
                }
            }
        }

        public class FeatureConfig
        {
            SerializedProperty _modeProp;
            SerializedProperty _featureProp;
            SerializedProperty _stateProp;

            public FeatureConfig(SerializedProperty root)
            {
                _modeProp = root.FindPropertyRelative("_mode");
                _featureProp = root.FindPropertyRelative("_feature");
                _stateProp = root.FindPropertyRelative("_state");
            }

            public FeatureStateActiveMode Mode
            {
                get => (FeatureStateActiveMode)_modeProp.enumValueIndex;
                set
                {
                    if (value != (FeatureStateActiveMode)_modeProp.enumValueIndex)
                    {
                        _modeProp.enumValueIndex = (int)value;
                    }
                }
            }

            public int Feature
            {
                get => _featureProp.enumValueIndex;
                set
                {
                    if (value != _featureProp.enumValueIndex)
                    {
                        _featureProp.enumValueIndex = value;
                    }
                }
            }

            public string FeatureState
            {
                get => _stateProp.stringValue;
                set
                {
                    if (value != _stateProp.stringValue)
                    {
                        _stateProp.stringValue = value;
                    }
                }
            }
        }
    }

    public abstract class FeatureListPropertyDrawer : PropertyDrawer
    {
        public float ControlLineHeight => EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing; // = 18 + 2
        public float BottomMargin => EditorGUIUtility.standardVerticalSpacing * 2;
        private const float IndentSize = 16;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var height = base.GetPropertyHeight(property, label); // includes height of first line

            var model = CreateModel(property);
            if (property.isExpanded)
            {
                var controlCount = model.ConfigModels.Count();
                height += controlCount * ControlLineHeight;
                height += BottomMargin;
            }

            return height;
        }

        public override bool CanCacheInspectorGUI(SerializedProperty property)
        {
            return true;
        }

        public override void OnGUI(Rect drawerPos, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(drawerPos, label, property);

            var oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            var labelPos = new Rect(drawerPos)
            {
                width = EditorGUIUtility.labelWidth - drawerPos.x,
                height = EditorGUIUtility.singleLineHeight
            };

            var model = CreateModel(property);
            property.isExpanded = EditorGUI.Foldout(labelPos, property.isExpanded, label, true);
            if (property.isExpanded)
            {
                RenderExpanded(drawerPos, model);
            }
            else
            {
                RenderCollapsed(drawerPos, model);
            }

            EditorGUI.indentLevel = oldIndent;
            EditorGUI.EndProperty();
        }

        private void RenderExpanded(Rect drawerPos, FeatureConfigList model) {
            Rect controlPos = new Rect(drawerPos.x, drawerPos.y, drawerPos.width,
                EditorGUIUtility.singleLineHeight);

            var flagsPos = Indent(controlPos, EditorGUIUtility.labelWidth);
            var newFlags = EnumToFlags(EditorGUI.EnumFlagsField(flagsPos, FlagsToEnum(model.Flags)));
            if (newFlags != model.Flags)
            {
                model.Flags = newFlags;
            }

            controlPos = Indent(controlPos, IndentSize);
            foreach (var configModel in model.ConfigModels)
            {
                controlPos.y += ControlLineHeight;

                // Render the label
                float indent = 0f;
                var labelPos = Indent(controlPos, indent);
                labelPos.width = EditorGUIUtility.labelWidth - IndentSize;
                var featureName = FeatureToString(configModel.Feature);
                featureName = ObjectNames.NicifyVariableName(featureName);

                EditorGUI.PrefixLabel(labelPos, new GUIContent(featureName));

                // Render the mode dropdown
                indent += labelPos.width;
                var modePos = Indent(controlPos, indent);
                var allowedModes = GetAllowedModes(configModel);
                if (allowedModes.Length > 1)
                {
                    modePos.width = 70;
                    configModel.Mode =
                        (FeatureStateActiveMode)EditorGUI.EnumPopup(modePos, configModel.Mode);
                }
                else if (allowedModes.Length == 1)
                {
                    configModel.Mode = allowedModes[0];
                    modePos.width = 15;
                    EditorGUI.SelectableLabel(modePos,
                        ObjectNames.NicifyVariableName(allowedModes[0] + ": "));
                }
                else
                {
                    modePos.width = -2;
                }

                // Render the state dropdown
                indent += modePos.width + 2;
                var statePos = Indent(controlPos, indent);
                var featureStates = GetStatesForFeature(configModel.Feature);
                string[] options = featureStates.Select(fs => fs.Name).ToArray();
                int selectedIndex = Array.FindIndex(featureStates, fs => fs.Id == configModel.FeatureState);
                int newSelectedIndex = EditorGUI.Popup(statePos, selectedIndex, options);
                if (newSelectedIndex != selectedIndex) {
                    configModel.FeatureState = featureStates[newSelectedIndex].Id;
                }
            }
        }

        private void RenderCollapsed(Rect drawerPos, FeatureConfigList model)
        {
            Rect controlPos = drawerPos;
            controlPos.height = EditorGUIUtility.singleLineHeight;

            var valuePos = Indent(controlPos, EditorGUIUtility.labelWidth + 2);
            valuePos.width = drawerPos.width - valuePos.x;
            var flagsEnum = FlagsToEnum(model.Flags);
            var values = Enum.GetValues(flagsEnum.GetType())
                .Cast<Enum>()
                .Where(e => flagsEnum.HasFlag(e))
                .Select(e => e.ToString());
            EditorGUI.SelectableLabel(valuePos, String.Join(", ", values));
        }

        private static Rect Indent(Rect position, float indentWidth)
        {
            return new Rect(position)
            {
                x = position.x + indentWidth,
                width = position.width - indentWidth
            };
        }

        protected virtual FeatureStateActiveMode[] GetAllowedModes(FeatureConfig model)
        {
            var statesForFeature = GetStatesForFeature(model.Feature);
            if (statesForFeature.Length > 2)
                return (FeatureStateActiveMode[])Enum.GetValues(typeof(FeatureStateActiveMode));
            else
                return new[] { FeatureStateActiveMode.Is };
        }

        protected abstract Enum FlagsToEnum(uint flags);
        protected abstract uint EnumToFlags(Enum flags);
        protected abstract string FeatureToString(int featureIdx);
        protected abstract FeatureStateDescription[] GetStatesForFeature(int featureIdx);
        protected abstract Model.FeatureConfigList CreateModel(SerializedProperty property);
    }
}
