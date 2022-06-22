using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace UnityEditor.XR.Interaction.Toolkit
{
    /// <summary>
    /// Custom editor for an <see cref="TeleportationAnchor"/>.
    /// </summary>
    [CustomEditor(typeof(TeleportationAnchor), true), CanEditMultipleObjects]
    public class TeleportationAnchorEditor : BaseTeleportationInteractableEditor
    {
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="TeleportationAnchor.teleportAnchorTransform"/>.</summary>
        protected SerializedProperty m_TeleportAnchorTransform;

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();

            m_TeleportAnchorTransform = serializedObject.FindProperty("m_TeleportAnchorTransform");
        }

        /// <inheritdoc />
        protected override void DrawCoreConfiguration()
        {
            base.DrawCoreConfiguration();
            EditorGUILayout.PropertyField(m_TeleportAnchorTransform);
        }
    }
}
