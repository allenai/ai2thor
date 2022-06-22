using System;

namespace UnityEngine.XR.Interaction.Toolkit
{
    public abstract partial class XRBaseController
    {
#pragma warning disable 618
        /// <summary>
        /// (Deprecated) Gets the state of the controller.
        /// </summary>
        /// <param name="controllerState">When this method returns, contains the <see cref="XRControllerState"/> object representing the state of the controller.</param>
        /// <returns>Returns <see langword="false"/>.</returns>
        /// <seealso cref="currentControllerState"/>
        [Obsolete("GetControllerState has been deprecated. Use currentControllerState instead.")]
        public virtual bool GetControllerState(out XRControllerState controllerState)
        {
            controllerState = currentControllerState;
            return false;
        }

        /// <summary>
        /// (Deprecated) Sets the state of the controller.
        /// </summary>
        /// <param name="controllerState">The state of the controller to set.</param>
        /// <seealso cref="currentControllerState"/>
        [Obsolete("SetControllerState has been deprecated. Use currentControllerState instead.")]
        public virtual void SetControllerState(XRControllerState controllerState)
        {
            currentControllerState = controllerState;
        }
#pragma warning restore 618

        /// <inheritdoc cref="modelParent"/>
        /// <remarks><c>modelTransform</c> has been deprecated due to being renamed. Use <see cref="modelParent"/> instead.</remarks>
        [Obsolete("modelTransform has been deprecated due to being renamed. Use modelParent instead. (UnityUpgradable) -> modelParent")]
        public Transform modelTransform
        {
            get => modelParent;
            set => modelParent = value;
        }

        /// <summary>
        /// (Deprecated) Defines the deadzone values for device-based input when performing translate or rotate anchor actions.
        /// </summary>
        /// <seealso cref="XRRayInteractor.TranslateAnchor"/>
        /// <seealso cref="XRRayInteractor.RotateAnchor"/>
        /// <remarks>
        /// <c>anchorControlDeadzone</c> has been deprecated. Please configure deadzone on the Rotate Anchor and Translate Anchor Actions.
        /// </remarks>
        [Obsolete("anchorControlDeadzone is obsolete. Please configure deadzone on the Rotate Anchor and Translate Anchor Actions.", true)]
        public float anchorControlDeadzone { get; set; }

        /// <summary>
        /// (Deprecated) Defines the off-axis deadzone values for device-based input when performing translate or rotate anchor actions.
        /// </summary>
        /// <seealso cref="Application.onBeforeRender"/>
        /// <remarks>
        /// <c>anchorControlOffAxisDeadzone</c> has been deprecated. Please configure deadzone on the Rotate Anchor and Translate Anchor Actions.
        /// </remarks>
        [Obsolete("anchorControlOffAxisDeadzone is obsolete. Please configure deadzone on the Rotate Anchor and Translate Anchor Actions.", true)]
        public float anchorControlOffAxisDeadzone { get; set; }
    }
}
