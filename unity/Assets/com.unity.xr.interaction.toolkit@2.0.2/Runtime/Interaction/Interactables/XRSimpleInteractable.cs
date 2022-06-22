namespace UnityEngine.XR.Interaction.Toolkit
{
    /// <summary>
    /// This is the simplest version of an Interactable object.
    /// It simply provides a concrete implementation of the <see cref="XRBaseInteractable"/>.
    /// It is intended to be used as a way to respond to <see cref="XRBaseInteractable.hoverEntered"/>/<see cref="XRBaseInteractable.hoverExited"/>
    /// and <see cref="XRBaseInteractable.selectEntered"/>/<see cref="XRBaseInteractable.selectExited"/>
    /// events with no underlying interaction behavior.
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    [AddComponentMenu("XR/XR Simple Interactable", 11)]
    [HelpURL(XRHelpURLConstants.k_XRSimpleInteractable)]
    public class XRSimpleInteractable : XRBaseInteractable
    {
    }
}
