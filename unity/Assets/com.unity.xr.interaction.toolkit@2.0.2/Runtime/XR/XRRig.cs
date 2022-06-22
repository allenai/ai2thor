using System;
using Unity.XR.CoreUtils;

namespace UnityEngine.XR.Interaction.Toolkit
{
    /// <summary>
    /// (Deprecated) The XR Rig component is typically attached to the base object of the XR Rig,
    /// and stores the <see cref="GameObject"/> that will be manipulated via locomotion.
    /// It is also used for offsetting the camera.
    /// </summary>
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    [Obsolete("XRRig has been deprecated. Use the XROrigin component instead.")]
    [HelpURL(XRHelpURLConstants.k_XRRig)]
    public partial class XRRig : XROrigin
    {
        /// <summary>
        /// See <c>MonoBehaviour.Awake</c>.
        /// </summary>
        protected new void Awake()
        {
            Debug.LogWarning("XRRig has been deprecated. Use the XROrigin component instead.", this);

            // Attempt to migrate
            if (Camera == null && m_CameraGameObject != null)
            {
                Camera = m_CameraGameObject.GetComponent<Camera>();
            }

            base.Awake();
        }
    }
}
