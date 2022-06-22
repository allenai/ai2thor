using System;

namespace UnityEngine.XR.Interaction.Toolkit.UI
{
    public partial class XRUIInputModule
    {
        /// <summary>
        /// The maximum distance to ray cast with tracked devices to find hit objects.
        /// </summary>
        /// <remarks>
        /// <c>maxRaycastDistance</c> has been deprecated. Its value was unused, calling this property is unnecessary and should be removed.
        /// </remarks>
        [Obsolete("maxRaycastDistance has been deprecated. Its value was unused, calling this property is unnecessary and should be removed.")]
        public float maxRaycastDistance
        {
            get => m_MaxTrackedDeviceRaycastDistance;
            set => m_MaxTrackedDeviceRaycastDistance = value;
        }
    }
}
