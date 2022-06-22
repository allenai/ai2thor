using System;

namespace UnityEngine.XR.Interaction.Toolkit
{
    /// <summary>
    /// The <see cref="LocomotionProvider"/> is the base class for various locomotion implementations.
    /// This class provides simple ways to interrogate the <see cref="LocomotionSystem"/> for whether a locomotion can begin
    /// and simple events for hooking into a start/end locomotion.
    /// </summary>
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_LocomotionProviders)]
    public abstract partial class LocomotionProvider : MonoBehaviour
    {
        /// <summary>
        /// Unity calls the <see cref="beginLocomotion"/> action when a <see cref="LocomotionProvider"/> successfully begins a locomotion event.
        /// </summary>
        public event Action<LocomotionSystem> beginLocomotion;

        /// <summary>
        /// Unity calls the <see cref="endLocomotion"/> action when a <see cref="LocomotionProvider"/> successfully ends a locomotion event.
        /// </summary>
        public event Action<LocomotionSystem> endLocomotion;

        [SerializeField]
        [Tooltip("The Locomotion System that this locomotion provider communicates with for exclusive access to an XR Origin." +
            " If one is not provided, the behavior will attempt to locate one during its Awake call.")]
        LocomotionSystem m_System;

        /// <summary>
        /// The <see cref="LocomotionSystem"/> that this <see cref="LocomotionProvider"/> communicates with for exclusive access to an XR Origin.
        /// If one is not provided, the behavior will attempt to locate one during its Awake call.
        /// </summary>
        public LocomotionSystem system
        {
            get => m_System;
            set => m_System = value;
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void Awake()
        {
            if (m_System == null)
                m_System = FindObjectOfType<LocomotionSystem>();
        }

        /// <summary>
        /// Checks if locomotion can begin.
        /// </summary>
        /// <returns>Returns <see langword="true"/> if locomotion can start. Otherwise, returns <see langword="false"/>.</returns>
        protected bool CanBeginLocomotion()
        {
            if (m_System == null)
                return false;

            return !m_System.busy;
        }

        /// <summary>
        /// Invokes begin locomotion events.
        /// </summary>
        /// <returns>Returns <see langword="true"/> if successful. Otherwise, returns <see langword="false"/>.</returns>
        protected bool BeginLocomotion()
        {
            if (m_System == null)
                return false;

            var success = m_System.RequestExclusiveOperation(this) == RequestResult.Success;
            if (success)
                beginLocomotion?.Invoke(m_System);

            return success;
        }

        /// <summary>
        /// Invokes end locomotion events.
        /// </summary>
        /// <returns>Returns <see langword="true"/> if successful. Otherwise, returns <see langword="false"/>.</returns>
        protected bool EndLocomotion()
        {
            if (m_System == null)
                return false;

            var success = m_System.FinishExclusiveOperation(this) == RequestResult.Success;
            if (success)
                endLocomotion?.Invoke(m_System);

            return success;
        }
    }
}
