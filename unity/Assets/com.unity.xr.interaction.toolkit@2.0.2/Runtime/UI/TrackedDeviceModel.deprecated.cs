using System;

namespace UnityEngine.XR.Interaction.Toolkit.UI
{
    public partial struct TrackedDeviceModel
    {
        /// <summary>
        /// The maximum distance to ray cast to check for UI.
        /// </summary>
        /// <remarks>
        /// <c>maxRaycastDistance</c> has been deprecated. Its value was unused, calling this property is unnecessary and should be removed.
        /// </remarks>
        [Obsolete("maxRaycastDistance has been deprecated. Its value was unused, calling this property is unnecessary and should be removed.")]
        public float maxRaycastDistance
        {
            // m_MaxRaycastDistance only exists to clean up a warning - when removing this property, remove that field
            get => m_MaxRaycastDistance;
            set => m_MaxRaycastDistance = value;
        }
    }
}
