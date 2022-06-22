using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.AR;

namespace UnityEditor.XR.Interaction.Toolkit.AR
{
    /// <summary>
    /// Custom editor for an <see cref="ARTranslationInteractable"/>.
    /// </summary>
    [CustomEditor(typeof(ARTranslationInteractable), true), CanEditMultipleObjects]
    public class ARTranslationInteractableEditor : ARBaseGestureInteractableEditor
    {
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="ARTranslationInteractable.objectGestureTranslationMode"/>.</summary>
        protected SerializedProperty m_ObjectGestureTranslationMode;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="ARTranslationInteractable.maxTranslationDistance"/>.</summary>
        protected SerializedProperty m_MaxTranslationDistance;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="ARTranslationInteractable.fallbackLayerMask"/>.</summary>
        protected SerializedProperty m_FallbackLayerMask;

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();

            m_ObjectGestureTranslationMode = serializedObject.FindProperty("m_ObjectGestureTranslationMode");
            m_MaxTranslationDistance = serializedObject.FindProperty("m_MaxTranslationDistance");
            m_FallbackLayerMask = serializedObject.FindProperty("m_FallbackLayerMask");
        }

        /// <inheritdoc />
        protected override void DrawProperties()
        {
            base.DrawProperties();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_ObjectGestureTranslationMode);
            EditorGUILayout.PropertyField(m_MaxTranslationDistance);
            EditorGUILayout.PropertyField(m_FallbackLayerMask);
        }
    }
}
