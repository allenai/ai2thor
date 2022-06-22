using System.Collections.Generic;
using UnityEditor.XR.Interaction.Toolkit.Utilities;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace UnityEditor.XR.Interaction.Toolkit
{
    /// <summary>
    /// Class used to draw an <see cref="InteractionLayerMask"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(InteractionLayerMask))]
    class InteractionLayerMaskDrawer : PropertyDrawer
    {
        static readonly List<string> s_DisplayOptions = new List<string>();
        static readonly List<int> s_ValueOptions = new List<int>();

        static void SelectInteractionLayerSettings()
        {
            Selection.activeObject = InteractionLayerSettings.instance;
        }

        /// <inheritdoc />
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var maskProperty = property.FindPropertyRelative("m_Bits");
            label = EditorGUI.BeginProperty(position, label, maskProperty);

            s_DisplayOptions.Clear();
            s_ValueOptions.Clear();
            InteractionLayerSettings.instance.GetLayerNamesAndValues(s_DisplayOptions, s_ValueOptions);
            XRInteractionEditorGUI.PropertyMaskField(position, label, maskProperty, s_DisplayOptions, s_ValueOptions, SelectInteractionLayerSettings);

            EditorGUI.EndProperty();
        }
    }
}
