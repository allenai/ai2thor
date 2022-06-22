using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.AR;

namespace UnityEditor.XR.Interaction.Toolkit.AR
{
    /// <summary>
    /// Custom editor for an <see cref="ARScaleInteractable"/>.
    /// </summary>
    [CustomEditor(typeof(ARScaleInteractable), true), CanEditMultipleObjects]
    public class ARScaleInteractableEditor : ARBaseGestureInteractableEditor
    {
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="ARScaleInteractable.minScale"/>.</summary>
        protected SerializedProperty m_MinScale;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="ARScaleInteractable.maxScale"/>.</summary>
        protected SerializedProperty m_MaxScale;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="ARScaleInteractable.elasticRatioLimit"/>.</summary>
        protected SerializedProperty m_ElasticRatioLimit;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="ARScaleInteractable.sensitivity"/>.</summary>
        protected SerializedProperty m_Sensitivity;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="ARScaleInteractable.elasticity"/>.</summary>
        protected SerializedProperty m_Elasticity;

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();

            m_MinScale = serializedObject.FindProperty("m_MinScale");
            m_MaxScale = serializedObject.FindProperty("m_MaxScale");
            m_ElasticRatioLimit = serializedObject.FindProperty("m_ElasticRatioLimit");
            m_Sensitivity = serializedObject.FindProperty("m_Sensitivity");
            m_Elasticity = serializedObject.FindProperty("m_Elasticity");
        }

        /// <inheritdoc />
        protected override void DrawProperties()
        {
            base.DrawProperties();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_MinScale);
            EditorGUILayout.PropertyField(m_MaxScale);
            EditorGUILayout.PropertyField(m_ElasticRatioLimit);
            EditorGUILayout.PropertyField(m_Sensitivity);
            EditorGUILayout.PropertyField(m_Elasticity);
        }
    }
}
