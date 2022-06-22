using System;

namespace UnityEngine.XR.Interaction.Toolkit
{
    public partial class CharacterControllerDriver
    {
        /// <summary>
        /// (Read Only) The <see cref="XRRig"/> used for driving the <see cref="CharacterController"/>.
        /// </summary>
        [Obsolete("xrRig has been deprecated. Use xrOrigin instead.")]
        protected XRRig xrRig => xrOrigin as XRRig;
    }
}
