using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.AR;

namespace UnityEditor.XR.Interaction.Toolkit.AR
{
    /// <summary>
    /// Custom editor for an <see cref="ARAnnotationInteractable"/>.
    /// </summary>
    [CustomEditor(typeof(ARAnnotationInteractable), true), CanEditMultipleObjects]
    public class ARAnnotationInteractableEditor : ARBaseGestureInteractableEditor
    {
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="ARAnnotationInteractable.annotations"/>.</summary>
        protected SerializedProperty m_Annotations;

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();

            m_Annotations = serializedObject.FindProperty("m_Annotations");
        }

        /// <inheritdoc />
        protected override void DrawProperties()
        {
            base.DrawProperties();
            EditorGUILayout.PropertyField(m_Annotations);
        }
    }
}
