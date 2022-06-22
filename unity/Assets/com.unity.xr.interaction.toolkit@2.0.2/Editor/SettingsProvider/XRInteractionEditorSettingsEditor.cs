using UnityEngine;

namespace UnityEditor.XR.Interaction.Toolkit
{
    /// <summary>
    /// Editor inspector for <see cref="XRInteractionEditorSettings"/>.
    /// </summary>
    [CustomEditor(typeof(XRInteractionEditorSettings))]
    class XRInteractionEditorSettingsEditor : Editor
    {
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRInteractionEditorSettings.m_ShowOldInteractionLayerMaskInInspector"/>.</summary>
        SerializedProperty m_ShowOldInteractionLayerMaskInInspector;

        /// <summary>
        /// Contents of GUI elements used by this editor.
        /// </summary>
        protected static class Contents
        {
            /// <summary><see cref="GUIContent"/> for <see cref="XRInteractionEditorSettings.showOldInteractionLayerMaskInInspector"/>.</summary>
            public static readonly GUIContent showOldInteractionLayerMaskInInspector = EditorGUIUtility.TrTextContent("Show Old Layer Mask In Inspector", "Enable this to show the \'Deprecated Interaction Layer Mask\' property in the Inspector window.");
        }

        void OnEnable()
        {
            m_ShowOldInteractionLayerMaskInInspector = serializedObject.FindProperty(nameof(m_ShowOldInteractionLayerMaskInInspector));
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            var labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 250f;
            EditorGUILayout.PropertyField(m_ShowOldInteractionLayerMaskInInspector, Contents.showOldInteractionLayerMaskInInspector);
            EditorGUIUtility.labelWidth = labelWidth;
            if (EditorGUI.EndChangeCheck())
                Repaint();
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}
