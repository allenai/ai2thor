using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.XR.Interaction.Toolkit.Utilities
{
    /// <summary>
    /// Class with methods to draw editor gui controls.
    /// </summary>
    static class XRInteractionEditorGUI
    {
        static class Contents
        {
            public static readonly GUIContent nothing = EditorGUIUtility.TrTextContent("Nothing");
            public static readonly GUIContent everything = EditorGUIUtility.TrTextContent("Everything");
            public static readonly GUIContent mixed = EditorGUIUtility.TrTextContent("Mixed...");
            public static readonly GUIContent addLayer = EditorGUIUtility.TrTextContent("Add layer...");
        }

        class SetPropertyMaskParameter
        {
            public readonly int MaskValue;
            public readonly SerializedProperty SerializedProperty;

            public SetPropertyMaskParameter(int maskValue, SerializedProperty serializedProperty)
            {
                MaskValue = maskValue;
                SerializedProperty = serializedProperty;
            }
        }

        static readonly int k_PropertyMaskField = nameof(k_PropertyMaskField).GetHashCode();
        
        static void SetPropertyMask(System.Object parameter)
        {
            if (!(parameter is SetPropertyMaskParameter setPropertyMaskParameter))
                return;

            var serializedProperty = setPropertyMaskParameter.SerializedProperty;
            serializedProperty.longValue = (uint)setPropertyMaskParameter.MaskValue;
            serializedProperty.serializedObject.ApplyModifiedProperties();
        }

        static GUIContent GetMaskContent(int mask, IList<string> displayedOptions, IList<int> valueOptions)
        {
            switch (mask)
            {
                case 0:
                    return Contents.nothing;
                case -1:
                    return Contents.everything;
            }

            var count = 0;
            var displayedMaskContent = Contents.mixed;
            var size = Mathf.Min(displayedOptions.Count, valueOptions.Count);
            for (var i = 0; i < size; i++)
            {
                if ((mask & 1 << valueOptions[i]) != 0)
                {
                    if (count == 0)
                        displayedMaskContent = EditorGUIUtility.TrTempContent(displayedOptions[i]);

                    ++count;
                    if (count >= 2)
                        return Contents.mixed;
                }
            }

            return displayedMaskContent;
        }
        
        /// <summary>
        /// Returns true if the event is a main keyboard action for the supplied control id.
        /// </summary>
        /// <param name="evt">The target gui event.</param>
        /// <param name="controlId">The target control id.</param>
        /// <returns>Returns whether the supplied event is a main keyboard action for the supplied control id.</returns>
        static bool IsMainActionKeyForControl(Event evt, int controlId)
        {
            if (GUIUtility.keyboardControl != controlId)
                return false;
            
            var modifier = evt.alt || evt.shift || evt.command || evt.control;
            return evt.type == EventType.KeyDown && (evt.keyCode == KeyCode.Space || evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter) && !modifier;
        }

        /// <summary>
        /// Makes a property field for masks.
        /// Inspired the internal Unity method <c>EditorGUI.DoPopup</c>.
        /// </summary>
        /// <param name="position">Rectangle on the screen to use for this control.</param>
        /// <param name="label">Label for the field.</param>
        /// <param name="property">The current SerializedProperty mask to display.</param>
        /// <param name="displayOptions">A string array containing the labels for each flag.</param>
        /// <param name="valueOptions">An integer list containing the value (or bit index) for each flag.</param>
        /// <param name="onAddLayerCallback">Optional callback called when users select the add layer option.</param>
        internal static void PropertyMaskField(Rect position, GUIContent label, SerializedProperty property, 
            IList<string> displayOptions, IList<int> valueOptions, GenericMenu.MenuFunction onAddLayerCallback = null)
        {
            // draw the property label
            var controlId = GUIUtility.GetControlID(k_PropertyMaskField, FocusType.Keyboard, position);
            position = EditorGUI.PrefixLabel(position, controlId, label);

            var mask = property.intValue;
            var currentEvent = Event.current;
            if (currentEvent.type == EventType.Repaint)
            {
                // draw the drop down button
                var content = GetMaskContent(mask, displayOptions, valueOptions);
                EditorStyles.popup.Draw(position, content, controlId, false, position.Contains(currentEvent.mousePosition));
            }
            else if (currentEvent.type == EventType.MouseDown && position.Contains(currentEvent.mousePosition) ||
                     IsMainActionKeyForControl(currentEvent, controlId))
            {
                currentEvent.Use();
             
                // show the interaction layer options menu
                var menu = new GenericMenu();
                menu.AddItem(Contents.nothing, mask == 0, SetPropertyMask, new SetPropertyMaskParameter(0, property));
                menu.AddItem(Contents.everything, mask == -1, SetPropertyMask, new SetPropertyMaskParameter(-1, property));
                
                var size = Mathf.Min(displayOptions.Count, valueOptions.Count);
                for (var i = 0; i < size; i++)
                {
                    var displayedOption = displayOptions[i];
                    var optionMaskValue = 1 << valueOptions[i];
                
                    menu.AddItem(new GUIContent(displayedOption), (mask & optionMaskValue) != 0, SetPropertyMask, new SetPropertyMaskParameter(mask ^ optionMaskValue, property));
                }
                
                if (onAddLayerCallback != null)
                {
                    menu.AddSeparator("");
                    menu.AddItem(Contents.addLayer, false, onAddLayerCallback);
                }
                
                menu.DropDown(position);
                GUIUtility.keyboardControl = controlId;
            }
        }

        internal static void EnumPropertyField(SerializedProperty property, GUIContent label, GUIContent[] displayedOptions, params GUILayoutOption[] options)
        {
            // This is similar to EditorGUILayout.PropertyField but allows the displayed options of the popup to be specified
            var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight, EditorStyles.popup, options);
            using (var scope = new EditorGUI.PropertyScope(rect, label, property))
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                var enumValueIndex = property.hasMultipleDifferentValues ? -1 : property.enumValueIndex;
                enumValueIndex = EditorGUI.Popup(rect, scope.content, enumValueIndex, displayedOptions);
                if (change.changed)
                {
                    property.enumValueIndex = enumValueIndex;
                }
            }
        }

        internal static void EnumPropertyField<T>(SerializedProperty property, GUIContent label, Func<Enum, bool> checkEnabled, params GUILayoutOption[] options) where T : Enum
        {
            // This is similar to EditorGUILayout.PropertyField but allows the displayed options of the popup to be disabled (grayed out)
            var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight, EditorStyles.popup, options);
            using (var scope = new EditorGUI.PropertyScope(rect, label, property))
            using (var change = new EditorGUI.ChangeCheckScope())
            {
                EditorGUI.showMixedValue = property.hasMultipleDifferentValues;

                var intValue = (T)EditorGUI.EnumPopup(rect, scope.content, (T)(object)property.intValue, checkEnabled);
                if (change.changed)
                {
                    property.intValue = Convert.ToInt32(intValue);
                }

                EditorGUI.showMixedValue = false;
            }
        }
    }
}