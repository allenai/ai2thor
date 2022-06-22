using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UnityEditor.XR.Interaction.Toolkit.Utilities
{
    /// <summary>
    /// Utility class for Inspector <see cref="Editor"/> classes in the XR Interaction Toolkit.
    /// </summary>
    public static class XRInteractionEditorUtility
    {
        /// <summary>
        /// Returns a list containing the <see cref="SerializeField"/> names of all <see cref="SerializedProperty"/> fields
        /// defined in the Editor (including derived types).
        /// </summary>
        /// <param name="editor">The <see cref="Editor"/> instance to reflect.</param>
        /// <returns>Returns a list of strings with property names.</returns>
        /// <seealso cref="Editor.DrawPropertiesExcluding"/>
        public static List<string> GetDerivedSerializedPropertyNames(Editor editor)
        {
            if (editor == null)
                throw new ArgumentNullException(nameof(editor));

            var fields = editor.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var propertyNames = new List<string> { "m_Script" };
            foreach (var field in fields)
            {
                var value = field.GetValue(editor);
                if (value is SerializedProperty serializedProperty)
                {
                    propertyNames.Add(serializedProperty.name);
                }
            }

            return propertyNames;
        }
    }
}