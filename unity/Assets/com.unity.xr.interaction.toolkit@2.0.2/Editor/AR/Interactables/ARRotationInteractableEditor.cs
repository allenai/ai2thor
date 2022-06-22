using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.AR;

namespace UnityEditor.XR.Interaction.Toolkit.AR
{
    /// <summary>
    /// Custom editor for an <see cref="ARRotationInteractable"/>.
    /// </summary>
    [CustomEditor(typeof(ARRotationInteractable), true), CanEditMultipleObjects]
    public class ARRotationInteractableEditor : ARBaseGestureInteractableEditor
    {
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="ARRotationInteractable.rotationRateDegreesDrag"/>.</summary>
        protected SerializedProperty m_RotationRateDegreesDrag;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="ARRotationInteractable.rotationRateDegreesTwist"/>.</summary>
        protected SerializedProperty m_RotationRateDegreesTwist;

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();

            m_RotationRateDegreesDrag = serializedObject.FindProperty("m_RotationRateDegreesDrag");
            m_RotationRateDegreesTwist = serializedObject.FindProperty("m_RotationRateDegreesTwist");
        }

        /// <inheritdoc />
        protected override void DrawProperties()
        {
            base.DrawProperties();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_RotationRateDegreesDrag);
            EditorGUILayout.PropertyField(m_RotationRateDegreesTwist);
        }
    }
}
