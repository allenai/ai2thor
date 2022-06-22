using System;

namespace UnityEngine.XR.Interaction.Toolkit
{
    public partial class XRSocketInteractor
    {
        /// <inheritdoc />
        /// <remarks>
        /// <c>CanHover(XRBaseInteractable)</c> has been deprecated. Use <see cref="CanHover(IXRHoverInteractable)"/> instead.
        /// </remarks>
        [Obsolete("CanHover(XRBaseInteractable) has been deprecated. Use CanHover(IXRHoverInteractable) instead.")]
        public override bool CanHover(XRBaseInteractable interactable) => CanHover((IXRHoverInteractable)interactable);

        /// <inheritdoc />
        /// <remarks>
        /// <c>CanSelect(XRBaseInteractable)</c> has been deprecated. Use <see cref="CanSelect(IXRSelectInteractable)"/> instead.
        /// </remarks>
        [Obsolete("CanSelect(XRBaseInteractable) has been deprecated. Use CanSelect(IXRSelectInteractable) instead.")]
        public override bool CanSelect(XRBaseInteractable interactable) => CanSelect((IXRSelectInteractable)interactable);
    }
}
