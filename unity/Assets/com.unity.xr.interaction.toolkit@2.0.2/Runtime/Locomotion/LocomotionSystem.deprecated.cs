using System;

namespace UnityEngine.XR.Interaction.Toolkit
{
    public partial class LocomotionSystem
    {
        /// <summary>
        /// (Deprecated) The XR Rig object to provide access control to.
        /// </summary>
        [Obsolete("xrRig is marked for deprecation and will be removed in a future version. Please use xrOrigin instead.")]
        public XRRig xrRig
        {
            get => m_XROrigin as XRRig;
            set => m_XROrigin = value;
        }

        /// <summary>
        /// (Read Only) If this value is true, the XR Origin's position should not be modified until this false.
        /// </summary>
        /// <remarks>
        /// <c>Busy</c> has been deprecated. Use <see cref="busy"/> instead.
        /// </remarks>
#pragma warning disable IDE1006 // Naming Styles
        [Obsolete("Busy has been deprecated. Use busy instead. (UnityUpgradable) -> busy")]
        public bool Busy => busy;
#pragma warning restore IDE1006
    }
}
