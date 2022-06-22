using UnityEngine.XR.Interaction.Toolkit;

namespace UnityEditor.XR.Interaction.Toolkit
{
    /// <summary>
    /// Custom editor for an <see cref="XRDirectInteractor"/>.
    /// </summary>
    [CustomEditor(typeof(XRDirectInteractor), true), CanEditMultipleObjects]
    public class XRDirectInteractorEditor : XRBaseControllerInteractorEditor
    {
        /// <inheritdoc />
        protected override void DrawProperties()
        {
            // Not calling base method to completely override drawn properties

            DrawInteractionManagement();
            DrawInteractionConfiguration();

            EditorGUILayout.Space();

            DrawSelectionConfiguration();
        }

        /// <summary>
        /// Draw the property fields related to interaction configuration.
        /// </summary>
        protected virtual void DrawInteractionConfiguration()
        {
            EditorGUILayout.PropertyField(m_AttachTransform, BaseContents.attachTransform);
        }

        /// <summary>
        /// Draw the property fields related to selection configuration.
        /// </summary>
        protected virtual void DrawSelectionConfiguration()
        {
            DrawSelectActionTrigger();
            EditorGUILayout.PropertyField(m_KeepSelectedTargetValid, BaseContents.keepSelectedTargetValid);
            EditorGUILayout.PropertyField(m_HideControllerOnSelect, BaseControllerContents.hideControllerOnSelect);
            EditorGUILayout.PropertyField(m_AllowHoveredActivate, BaseControllerContents.allowHoveredActivate);
            EditorGUILayout.PropertyField(m_StartingSelectedInteractable, BaseContents.startingSelectedInteractable);
        }
    }
}
