using System;

namespace UnityEngine.XR.Interaction.Toolkit
{
    public partial class XRGrabInteractable
    {
        /// <summary>
        /// (Deprecated) Forces this object to have gravity when released
        /// (will still use pre-grab value if this is <see langword="false"/>).
        /// </summary>
        /// <remarks>
        /// <c>gravityOnDetach</c> has been deprecated. Use <see cref="forceGravityOnDetach"/> instead.
        /// </remarks>
        [Obsolete("gravityOnDetach has been deprecated. Use forceGravityOnDetach instead. (UnityUpgradable) -> forceGravityOnDetach")]
        public bool gravityOnDetach
        {
            get => forceGravityOnDetach;
            set => forceGravityOnDetach = value;
        }
    }
}
