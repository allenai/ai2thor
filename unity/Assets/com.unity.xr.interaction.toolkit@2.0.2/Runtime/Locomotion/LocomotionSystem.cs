using UnityEngine.Serialization;
using Unity.XR.CoreUtils;

namespace UnityEngine.XR.Interaction.Toolkit
{
    /// <summary>
    /// The result of a locomotion request.
    /// </summary>
    /// <seealso cref="LocomotionSystem.RequestExclusiveOperation"/>
    /// <seealso cref="LocomotionSystem.FinishExclusiveOperation"/>
    public enum RequestResult
    {
        /// <summary>
        /// The locomotion request was successful.
        /// </summary>
        Success,

        /// <summary>
        /// The locomotion request failed due to the system being currently busy.
        /// </summary>
        Busy,

        /// <summary>
        /// The locomotion request failed due to an unknown error.
        /// </summary>
        Error,
    }

    /// <summary>
    /// The <see cref="LocomotionSystem"/> object is used to control access to the XR Origin. This system enforces that only one
    /// Locomotion Provider can move the XR Origin at one time. This is the only place that access to an XR Origin is controlled,
    /// having multiple instances of a <see cref="LocomotionSystem"/> drive a single XR Origin is not recommended.
    /// </summary>
    [AddComponentMenu("XR/Locomotion/Locomotion System", 11)]
    [HelpURL(XRHelpURLConstants.k_LocomotionSystem)]
    public partial class LocomotionSystem : MonoBehaviour
    {
        LocomotionProvider m_CurrentExclusiveProvider;
        float m_TimeMadeExclusive;

        [SerializeField]
        [Tooltip("The timeout (in seconds) for exclusive access to the XR Origin.")]
        float m_Timeout = 10f;

        /// <summary>
        /// The timeout (in seconds) for exclusive access to the XR Origin.
        /// </summary>
        public float timeout
        {
            get => m_Timeout;
            set => m_Timeout = value;
        }

        [SerializeField, FormerlySerializedAs("m_XRRig")]
        [Tooltip("The XR Origin object to provide access control to.")]
        XROrigin m_XROrigin;

        /// <summary>
        /// The XR Origin object to provide access control to.
        /// </summary>
        public XROrigin xrOrigin
        {
            get => m_XROrigin;
            set => m_XROrigin = value;
        }

        /// <summary>
        /// (Read Only) If this value is true, the XR Origin's position should not be modified until this false.
        /// </summary>
        public bool busy => m_CurrentExclusiveProvider != null;

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void Awake()
        {
            if (m_XROrigin == null)
                m_XROrigin = FindObjectOfType<XROrigin>();
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void Update()
        {
            if (m_CurrentExclusiveProvider != null && Time.time > m_TimeMadeExclusive + m_Timeout)
            {
                ResetExclusivity();
            }
        }

        /// <summary>
        /// Attempt to "lock" access to the XR Origin for the <paramref name="provider"/>.
        /// </summary>
        /// <param name="provider">The locomotion provider that is requesting access.</param>
        /// <returns>Returns a <see cref="RequestResult"/> that reflects the status of the request.</returns>
        public RequestResult RequestExclusiveOperation(LocomotionProvider provider)
        {
            if (provider == null)
                return RequestResult.Error;

            if (m_CurrentExclusiveProvider == null)
            {
                m_CurrentExclusiveProvider = provider;
                m_TimeMadeExclusive = Time.time;
                return RequestResult.Success;
            }

            return m_CurrentExclusiveProvider != provider ? RequestResult.Busy : RequestResult.Error;
        }

        void ResetExclusivity()
        {
            m_CurrentExclusiveProvider = null;
            m_TimeMadeExclusive = 0f;
        }

        /// <summary>
        /// Informs the <see cref="LocomotionSystem"/> that exclusive access to the XR Origin is no longer required.
        /// </summary>
        /// <param name="provider">The locomotion provider that is relinquishing access.</param>
        /// <returns>Returns a <see cref="RequestResult"/> that reflects the status of the request.</returns>
        public RequestResult FinishExclusiveOperation(LocomotionProvider provider)
        {
            if(provider == null || m_CurrentExclusiveProvider == null)
                return RequestResult.Error;

            if (m_CurrentExclusiveProvider == provider)
            {
                ResetExclusivity();
                return RequestResult.Success;
            }

            return RequestResult.Error;
        }
    }
}
