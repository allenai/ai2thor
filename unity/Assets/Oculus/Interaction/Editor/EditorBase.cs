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
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;

namespace Oculus.Interaction.Editor
{
    /// <summary>
    /// A utility class for building custom editors with less work required.
    /// </summary>
    public class EditorBase : UnityEditor.Editor
    {

        #region API

        protected virtual void OnEnable() { }

        protected virtual void OnDisable() { }

        /// <summary>
        /// You must put all of the editor specifications into OnInit
        /// </summary>
        protected virtual void OnInit() { }

        /// <summary>
        /// Call in OnInit with one or more property names to hide them from the inspector.
        ///
        /// This is preferable to using [HideInInspector] because it still allows the property to
        /// be viewed when using the Inspector debug mode.
        /// </summary>
        protected void Hide(params string[] properties)
        {
            Assert.IsTrue(properties.Length > 0, "Should always hide at least one property.");
            if (!ValidateProperties(properties))
            {
                return;
            }

            _hiddenProperties.UnionWith(properties);
        }

        /// <summary>
        /// Call in OnInit with one or more property names to defer drawing them until after all
        /// non-deferred properties have been drawn.  All deferred properties will be drawn in the order
        /// they are passed in to calls to Defer.
        /// </summary>
        protected void Defer(params string[] properties)
        {
            Assert.IsTrue(properties.Length > 0, "Should always defer at least one property.");
            if (!ValidateProperties(properties))
            {
                return;
            }

            foreach (var property in properties)
            {
                if (_deferredProperties.Contains(property))
                {
                    continue;
                }

                _deferredProperties.Add(property);
                _deferredActions.Add(() =>
                {
                    DrawProperty(serializedObject.FindProperty(property));
                });
            }
        }

        /// <summary>
        /// Call in OnInit with a single property name and a custom property drawer.  Equivalent
        /// to calling Draw and then Defer for the property.
        /// </summary>
        protected void Defer(string property, Action<SerializedProperty> customDrawer)
        {
            Draw(property, customDrawer);
            Defer(property);
        }

        /// <summary>
        /// Call in OnInit with a single delegate to have it be called after all other non-deferred
        /// properties have been drawn.
        /// </summary>
        protected void Defer(Action deferredAction)
        {
            _deferredActions.Add(deferredAction);
        }

        /// <summary>
        /// Call in OnInit to specify a custom drawer for a single property.  Whenever the property is drawn,
        /// it will use the provided property drawer instead of the default one.
        /// </summary>
        protected void Draw(string property, Action<SerializedProperty> drawer)
        {
            if (!ValidateProperties(property))
            {
                return;
            }

            _customDrawers.Add(property, drawer);
        }

        /// <summary>
        /// Call in OnInit to specify a custom drawer for a single property.  Include an extra property that gets
        /// lumped in with the primary property.  The extra property is not drawn normally, and is instead grouped in
        /// with the primary property.  Can be used in situations where a collection of properties need to be drawn together.
        /// </summary>
        protected void Draw(string property,
            string withExtra0,
            Action<SerializedProperty, SerializedProperty> drawer)
        {
            if (!ValidateProperties(property, withExtra0))
            {
                return;
            }

            Hide(withExtra0);
            Draw(property, p =>
            {
                drawer(p,
                    serializedObject.FindProperty(withExtra0));
            });
        }

        protected void Draw(string property,
            string withExtra0,
            string withExtra1,
            Action<SerializedProperty, SerializedProperty, SerializedProperty> drawer)
        {
            if (!ValidateProperties(property, withExtra0, withExtra1))
            {
                return;
            }

            Hide(withExtra0);
            Hide(withExtra1);
            Draw(property, p =>
            {
                drawer(p,
                    serializedObject.FindProperty(withExtra0),
                    serializedObject.FindProperty(withExtra1));
            });
        }

        protected void Draw(string property,
            string withExtra0,
            string withExtra1,
            string withExtra2,
            Action<SerializedProperty, SerializedProperty, SerializedProperty, SerializedProperty>
                drawer)
        {
            if (!ValidateProperties(property, withExtra0, withExtra1, withExtra2))
            {
                return;
            }

            Hide(withExtra0);
            Hide(withExtra1);
            Hide(withExtra2);
            Draw(property, p =>
            {
                drawer(p,
                    serializedObject.FindProperty(withExtra0),
                    serializedObject.FindProperty(withExtra1),
                    serializedObject.FindProperty(withExtra2));
            });
        }

        protected void Draw(string property,
            string withExtra0,
            string withExtra1,
            string withExtra2,
            string withExtra3,
            Action<SerializedProperty, SerializedProperty, SerializedProperty, SerializedProperty,
                SerializedProperty> drawer)
        {
            if (!ValidateProperties(property, withExtra0, withExtra1, withExtra2, withExtra3))
            {
                return;
            }

            Hide(withExtra0);
            Hide(withExtra1);
            Hide(withExtra2);
            Hide(withExtra3);
            Draw(property, p =>
            {
                drawer(p,
                    serializedObject.FindProperty(withExtra0),
                    serializedObject.FindProperty(withExtra1),
                    serializedObject.FindProperty(withExtra2),
                    serializedObject.FindProperty(withExtra3));
            });
        }

        protected void Conditional(string boolPropName, bool showIf, params string[] toHide)
        {
            if (!ValidateProperties(boolPropName) || !ValidateProperties(toHide))
            {
                return;
            }

            var boolProp = serializedObject.FindProperty(boolPropName);
            if (boolProp.propertyType != SerializedPropertyType.Boolean)
            {
                Debug.LogError(
                    $"Must provide a Boolean property to this Conditional method, but the property {boolPropName} had a type of {boolProp.propertyType}");
                return;
            }

            List<Func<bool>> conditions;
            foreach (var prop in toHide)
            {
                if (!_propertyDrawConditions.TryGetValue(prop, out conditions))
                {
                    conditions = new List<Func<bool>>();
                    _propertyDrawConditions[prop] = conditions;
                }

                conditions.Add(() =>
                {
                    if (boolProp.hasMultipleDifferentValues)
                    {
                        return false;
                    }
                    else
                    {
                        return boolProp.boolValue == showIf;
                    }
                });
            }
        }

        protected void Conditional<T>(string enumPropName, T showIf, params string[] toHide)
            where T : Enum
        {
            if (!ValidateProperties(enumPropName) || !ValidateProperties(toHide))
            {
                return;
            }

            var enumProp = serializedObject.FindProperty(enumPropName);
            if (enumProp.propertyType != SerializedPropertyType.Enum)
            {
                Debug.LogError(
                    $"Must provide a Boolean property to this Conditional method, but the property {enumPropName} had a type of {enumProp.propertyType}");
                return;
            }

            List<Func<bool>> conditions;
            foreach (var prop in toHide)
            {
                if (!_propertyDrawConditions.TryGetValue(prop, out conditions))
                {
                    conditions = new List<Func<bool>>();
                    _propertyDrawConditions[prop] = conditions;
                }

                conditions.Add(() =>
                {
                    if (enumProp.hasMultipleDifferentValues)
                    {
                        return false;
                    }
                    else
                    {
                        return enumProp.intValue == showIf.GetHashCode();
                    }
                });
            }
        }

        /// <summary>
        /// Call in OnInit to specify a custom decorator for a single property.  Before a property is drawn,
        /// all of the decorators will be drawn first.
        /// </summary>
        protected void Decorate(string property, Action<SerializedProperty> decorator)
        {
            if (!ValidateProperties(property))
            {
                return;
            }

            List<Action<SerializedProperty>> decorators;
            if (!_customDecorators.TryGetValue(property, out decorators))
            {
                decorators = new List<Action<SerializedProperty>>();
                _customDecorators[property] = decorators;
            }

            decorators.Add(decorator);
        }

        /// <summary>
        /// Call in OnInit to specify a custom grouping behaviour for a range of properties.  Specify the first
        /// and last property (inclusive) and the action to take BEFORE the first property is drawn, and the action
        /// to take AFTER the last property is drawn.
        /// </summary>
        protected void Group(string firstProperty, string lastProperty, Action beginGroup,
            Action endGroup)
        {
            if (!ValidateProperties(firstProperty) || !ValidateProperties(lastProperty))
            {
                return;
            }

            _groupBegins.Add(firstProperty, beginGroup);
            _groupEnds.Add(lastProperty, endGroup);
        }

        /// <summary>
        /// A utility version of the more generic Group method.
        /// Call in OnInit to specify a range of properties that should be grouped within a styled vertical
        /// layout group.
        /// </summary>
        protected void Group(string firstProperty, string lastProperty, GUIStyle style)
        {
            if (style == null)
            {
                Debug.LogError(
                    "Cannot provide a null style to EditorBase.Group.  If you are acquiring a " +
                    "Style from the EditorStyles class, try calling Group from with on OnInit instead " +
                    "of from within OnEnable.");
                return;
            }

            Group(firstProperty,
                lastProperty,
                () => EditorGUILayout.BeginVertical(style),
                () => EditorGUILayout.EndVertical());
        }

        /// <summary>
        /// Groups the given properties into a foldout with a given name.
        /// </summary>
        protected void Foldout(string firstProperty, string lastProperty, string foldoutName,
            bool showByDefault = false)
        {
            Group(firstProperty,
                lastProperty,
                () =>
                {
                    bool shouldShow;
                    if (!_foldouts.TryGetValue(foldoutName, out shouldShow))
                    {
                        shouldShow = showByDefault;
                    }

                    shouldShow = EditorGUILayout.Foldout(shouldShow, foldoutName);

                    _foldouts[foldoutName] = shouldShow;
                    EditorGUI.indentLevel++;

                    _currentStates.Push(shouldShow);
                },
                () =>
                {
                    EditorGUI.indentLevel--;
                    _currentStates.Pop();
                });
        }

        protected virtual void OnBeforeInspector() { }
        protected virtual void OnAfterInspector(bool anyPropertiesModified) { }

        #endregion

        #region IMPLEMENTATION

        [NonSerialized]
        private bool _hasInitBeenCalled = false;

        private HashSet<string> _hiddenProperties = new HashSet<string>();
        private HashSet<string> _deferredProperties = new HashSet<string>();
        private List<Action> _deferredActions = new List<Action>();

        private Dictionary<string, bool> _foldouts = new Dictionary<string, bool>();
        private Stack<bool> _currentStates = new Stack<bool>();

        private Dictionary<string, Action<SerializedProperty>> _customDrawers =
            new Dictionary<string, Action<SerializedProperty>>();

        private Dictionary<string, List<Action<SerializedProperty>>> _customDecorators =
            new Dictionary<string, List<Action<SerializedProperty>>>();

        private Dictionary<string, Action> _groupBegins = new Dictionary<string, Action>();
        private Dictionary<string, Action> _groupEnds = new Dictionary<string, Action>();

        private Dictionary<string, List<Func<bool>>> _propertyDrawConditions =
            new Dictionary<string, List<Func<bool>>>();

        public override void OnInspectorGUI()
        {
            if (!_hasInitBeenCalled)
            {
                OnInit();
                _hasInitBeenCalled = true;
            }

            SerializedProperty it = serializedObject.GetIterator();
            it.NextVisible(enterChildren: true);

            //Draw script header
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.PropertyField(it);
            EditorGUI.EndDisabledGroup();

            OnBeforeInspector();

            EditorGUI.BeginChangeCheck();

            while (it.NextVisible(enterChildren: false))
            {
                //Don't draw deferred properties in this pass, we will draw them after everything else
                if (_deferredProperties.Contains(it.name))
                {
                    continue;
                }

                DrawProperty(it);
            }

            foreach (var deferredAction in _deferredActions)
            {
                deferredAction();
            }

            bool anyModified = EditorGUI.EndChangeCheck();

            OnAfterInspector(anyModified);

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawProperty(SerializedProperty property)
        {
            Action groupBeginAction;
            if (_groupBegins.TryGetValue(property.name, out groupBeginAction))
            {
                groupBeginAction();
            }

            try
            {
                //Don't draw if we are in a property that is currently hidden by a foldout
                if (_currentStates.Any(s => s == false))
                {
                    return;
                }

                //Don't draw hidden properties
                if (_hiddenProperties.Contains(property.name))
                {
                    return;
                }

                List<Func<bool>> conditions;
                if (_propertyDrawConditions.TryGetValue(property.name, out conditions))
                {
                    foreach (var condition in conditions)
                    {
                        if (!condition())
                        {
                            return;
                        }
                    }
                }

                //First draw all decorators for the property
                List<Action<SerializedProperty>> decorators;
                if (_customDecorators.TryGetValue(property.name, out decorators))
                {
                    foreach (var decorator in decorators)
                    {
                        decorator(property);
                    }
                }

                //Then draw the property itself, using a custom drawer if needed
                Action<SerializedProperty> customDrawer;
                if (_customDrawers.TryGetValue(property.name, out customDrawer))
                {
                    customDrawer(property);
                }
                else
                {
                    EditorGUILayout.PropertyField(property, includeChildren: true);
                }

            }
            finally
            {
                Action groupEndAction;
                if (_groupEnds.TryGetValue(property.name, out groupEndAction))
                {
                    groupEndAction();
                }
            }
        }

        private bool ValidateProperties(params string[] properties)
        {
            foreach (var property in properties)
            {
                if (serializedObject.FindProperty(property) == null)
                {
                    Debug.LogWarning(
                        $"Could not find property {property}, maybe it was deleted or renamed?");
                    return false;
                }
            }

            return true;
        }

        #endregion
    }
}
