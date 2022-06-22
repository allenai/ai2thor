using System.Collections.Generic;
using UnityEditor.XR.Interaction.Toolkit.Utilities;
using UnityEngine;

namespace UnityEditor.XR.Interaction.Toolkit
{
    /// <summary>
    /// Base class for custom editors in the XR Interaction Toolkit.
    /// </summary>
    public abstract class BaseInteractionEditor : Editor
    {
        /// <summary>
        /// The <see cref="SerializeField"/> names of all <see cref="SerializedProperty"/> fields
        /// defined in the <see cref="Editor"/> (including derived types).
        /// </summary>
        /// <seealso cref="InitializeKnownSerializedPropertyNames"/>
        protected string[] knownSerializedPropertyNames { get; set; }

        bool m_InitializedKnownSerializedPropertyNames;

        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            InitializeKnownSerializedPropertyNames();

            serializedObject.Update();

            DrawInspector();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// This method is automatically called by <see cref="OnInspectorGUI"/> to
        /// draw the custom inspector. Override this method to customize the
        /// inspector as a whole.
        /// </summary>
        protected abstract void DrawInspector();

        /// <summary>
        /// This method is automatically called by <see cref="OnInspectorGUI"/> to
        /// initialize <see cref="knownSerializedPropertyNames"/> if necessary.
        /// This is used together with <see cref="DrawDerivedProperties"/> to draw all unknown
        /// serialized fields from derived classes.
        /// </summary>
        /// <seealso cref="DrawDerivedProperties"/>
        protected virtual void InitializeKnownSerializedPropertyNames()
        {
            if (m_InitializedKnownSerializedPropertyNames)
                return;

            knownSerializedPropertyNames = GetDerivedSerializedPropertyNames().ToArray();
            m_InitializedKnownSerializedPropertyNames = true;
        }

        /// <summary>
        /// Returns a list containing the <see cref="SerializeField"/> names of all <see cref="SerializedProperty"/> fields
        /// defined in the <see cref="Editor"/> (including derived types).
        /// </summary>
        /// <returns>Returns a list of strings with property names.</returns>
        protected virtual List<string> GetDerivedSerializedPropertyNames()
        {
            return XRInteractionEditorUtility.GetDerivedSerializedPropertyNames(this);
        }

        /// <summary>
        /// This method is automatically called by <see cref="DrawInspector"/> to
        /// draw the property fields of derived classes that are not explicitly defined in the <see cref="Editor"/>.
        /// </summary>
        /// <remarks>
        /// This method is used to allow users to add a <see cref="SerializeField"/> to a derived behavior
        /// and have it automatically appear in the Inspector while still having the custom Editor
        /// apply to that derived class.
        /// <br />
        /// When a derived <see cref="Editor"/> class adds a <see cref="SerializedProperty"/>,
        /// it will no longer automatically be drawn by this method. This is to allow users to customize
        /// where the property is drawn in the Inspector window. The derived <see cref="Editor"/> class
        /// does not need to explicitly add the <see cref="SerializedProperty"/> if the user is fine with
        /// the default location of where it will be drawn.
        /// </remarks>
        /// <seealso cref="InitializeKnownSerializedPropertyNames"/>
        protected virtual void DrawDerivedProperties()
        {
            DrawPropertiesExcluding(serializedObject, knownSerializedPropertyNames);
        }

        /// <summary>
        /// Draw the standard read-only Script property.
        /// </summary>
        protected virtual void DrawScript()
        {
            using (new EditorGUI.DisabledScope(true))
            {
                if (target is MonoBehaviour behaviour)
                    EditorGUILayout.ObjectField(EditorGUIUtility.TrTempContent("Script"), MonoScript.FromMonoBehaviour(behaviour), typeof(MonoBehaviour), false);
                else if (target is ScriptableObject scriptableObject)
                    EditorGUILayout.ObjectField(EditorGUIUtility.TrTempContent("Script"), MonoScript.FromScriptableObject(scriptableObject), typeof(ScriptableObject), false);
            }
        }
    }
}