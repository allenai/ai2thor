namespace UnityEngine.XR.Interaction.Toolkit
{
    /// <summary>
    /// An anchor is a teleportation destination which teleports the user to a pre-determined
    /// specific position and/or rotation.
    /// </summary>
    /// <seealso cref="TeleportationArea"/>
    [AddComponentMenu("XR/Teleportation Anchor", 11)]
    [HelpURL(XRHelpURLConstants.k_TeleportationAnchor)]
    public class TeleportationAnchor : BaseTeleportationInteractable
    {
        [SerializeField]
        [Tooltip("The Transform that represents the teleportation destination.")]
        Transform m_TeleportAnchorTransform;

        /// <summary>
        /// The <see cref="Transform"/> that represents the teleportation destination.
        /// </summary>
        public Transform teleportAnchorTransform
        {
            get => m_TeleportAnchorTransform;
            set => m_TeleportAnchorTransform = value;
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void OnValidate()
        {
            if (m_TeleportAnchorTransform == null)
                m_TeleportAnchorTransform = transform;
        }

        /// <inheritdoc />
        protected override void Reset()
        {
            base.Reset();
            m_TeleportAnchorTransform = transform;
        }

        /// <summary>
        /// Unity calls this when drawing gizmos.
        /// </summary>
        protected void OnDrawGizmos()
        {
            if (m_TeleportAnchorTransform == null)
                return;

            Gizmos.color = Color.blue;
            GizmoHelpers.DrawWireCubeOriented(m_TeleportAnchorTransform.position, m_TeleportAnchorTransform.rotation, 1f);

            GizmoHelpers.DrawAxisArrows(m_TeleportAnchorTransform, 1f);
        }

        /// <inheritdoc />
        protected override bool GenerateTeleportRequest(IXRInteractor interactor, RaycastHit raycastHit, ref TeleportRequest teleportRequest)
        {
            if (m_TeleportAnchorTransform == null)
                return false;

            teleportRequest.destinationPosition = m_TeleportAnchorTransform.position;
            teleportRequest.destinationRotation = m_TeleportAnchorTransform.rotation;
            return true;
        }
    }
}
