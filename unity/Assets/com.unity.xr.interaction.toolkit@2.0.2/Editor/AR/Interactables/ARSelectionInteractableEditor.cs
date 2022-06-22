using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.AR;

namespace UnityEditor.XR.Interaction.Toolkit.AR
{
    /// <summary>
    /// Custom editor for an <see cref="ARSelectionInteractable"/>.
    /// </summary>
    [CustomEditor(typeof(ARSelectionInteractable), true), CanEditMultipleObjects]
    public class ARSelectionInteractableEditor : ARBaseGestureInteractableEditor
    {
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="ARSelectionInteractable.selectionVisualization"/>.</summary>
        protected SerializedProperty m_SelectionVisualization;

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();

            m_SelectionVisualization = serializedObject.FindProperty("m_SelectionVisualization");
        }

        /// <inheritdoc />
        protected override void DrawProperties()
        {
            base.DrawProperties();
            EditorGUILayout.PropertyField(m_SelectionVisualization);
        }
    }
}
