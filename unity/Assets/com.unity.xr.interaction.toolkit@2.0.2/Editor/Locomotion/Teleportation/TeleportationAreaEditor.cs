using UnityEngine.XR.Interaction.Toolkit;

namespace UnityEditor.XR.Interaction.Toolkit
{
    /// <summary>
    /// Custom editor for an <see cref="TeleportationArea"/>.
    /// </summary>
    [CustomEditor(typeof(TeleportationArea), true), CanEditMultipleObjects]
    public class TeleportationAreaEditor : BaseTeleportationInteractableEditor
    {
    }
}
