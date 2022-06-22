using System;
using UnityEngine.XR.Interaction.Toolkit.Utilities.Pooling;

namespace UnityEngine.XR.Interaction.Toolkit
{
    /// <summary>
    /// The option of which object's orientation in the rig Unity matches with the destination after teleporting.
    /// </summary>
    public enum MatchOrientation
    {
        /// <summary>
        /// After teleporting the XR Origin will be positioned such that its up vector matches world space up.
        /// </summary>
        WorldSpaceUp,

        /// <summary>
        /// After teleporting the XR Origin will be positioned such that its up vector matches target up.
        /// </summary>
        TargetUp,

        /// <summary>
        /// After teleporting the XR Origin will be positioned such that its up and forward vectors match target up and forward, respectively.
        /// </summary>
        TargetUpAndForward,

        /// <summary>
        /// After teleporting the XR Origin will not attempt to match any orientation.
        /// </summary>
        None,
    }

    /// <summary>
    /// The Teleport Request that describes the result of the teleportation action. Each Teleportation Interactable must fill out a Teleport Request
    /// for each teleport action.
    /// </summary>
    public struct TeleportRequest
    {
        /// <summary>
        /// The position in world space of the Teleportation Destination.
        /// </summary>
        public Vector3 destinationPosition;
        /// <summary>
        /// The rotation in world space of the Teleportation Destination. This is used primarily for matching world rotations directly.
        /// </summary>
        public Quaternion destinationRotation;
        /// <summary>
        ///  The Time (in unix epoch) of the request.
        /// </summary>
        public float requestTime;
        /// <summary>
        /// The option of how to orient the rig after teleportation.
        /// </summary>
        public MatchOrientation matchOrientation;
    }

    /// <summary>
    /// This is intended to be the base class for all Teleportation Interactables. This abstracts the teleport request process for specializations of this class.
    /// </summary>
    public abstract partial class BaseTeleportationInteractable : XRBaseInteractable
    {
        /// <summary>
        /// Indicates when the teleportation action happens.
        /// </summary>
        public enum TeleportTrigger
        {
            /// <summary>
            /// Teleportation occurs once selection is released without being canceled.
            /// </summary>
            OnSelectExited,

            /// <summary>
            /// Teleportation occurs right when area is selected.
            /// </summary>
            OnSelectEntered,

            /// <summary>
            /// Teleportation occurs on activate.
            /// </summary>
            OnActivated,

            /// <summary>
            /// Teleportation occurs on deactivate.
            /// </summary>
            OnDeactivated,

            /// <summary>
            /// (Deprecated) OnSelectExit has been deprecated. Use OnSelectExited instead.
            /// </summary>
            [Obsolete("OnSelectExit has been deprecated. Use OnSelectExited instead. (UnityUpgradable) -> OnSelectExited")]
            OnSelectExit = OnSelectExited,

            /// <summary>
            /// (Deprecated) OnSelectEnter has been deprecated. Use OnSelectEntered instead.
            /// </summary>
            [Obsolete("OnSelectEnter has been deprecated. Use OnSelectEntered instead. (UnityUpgradable) -> OnSelectEntered")]
            OnSelectEnter = OnSelectEntered,

            /// <summary>
            /// (Deprecated) OnSelectEnter has been deprecated. Use OnSelectEntered instead.
            /// </summary>
            [Obsolete("OnActivate has been deprecated. Use OnActivated instead. (UnityUpgradable) -> OnActivated")]
            OnActivate = OnActivated,

            /// <summary>
            /// (Deprecated) OnDeactivate has been deprecated. Use OnDeactivated instead.
            /// </summary>
            [Obsolete("OnDeactivate has been deprecated. Use OnDeactivated instead. (UnityUpgradable) -> OnDeactivated")]
            OnDeactivate = OnDeactivated,
        }

        [SerializeField]
        [Tooltip("The teleportation provider that this teleportation interactable will communicate teleport requests to." +
            " If no teleportation provider is configured, will attempt to find a teleportation provider during Awake.")]
        TeleportationProvider m_TeleportationProvider;

        /// <summary>
        /// The teleportation provider that this teleportation interactable communicates teleport requests to.
        /// If no teleportation provider is configured, will attempt to find a teleportation provider during Awake.
        /// </summary>
        public TeleportationProvider teleportationProvider
        {
            get => m_TeleportationProvider;
            set => m_TeleportationProvider = value;
        }

        [SerializeField]
        [Tooltip("How to orient the rig after teleportation." +
            "\nSet to:" +
            "\n\nWorld Space Up to stay oriented according to the world space up vector." +
            "\n\nSet to Target Up to orient according to the target BaseTeleportationInteractable Transform's up vector." +
            "\n\nSet to Target Up And Forward to orient according to the target BaseTeleportationInteractable Transform's rotation." +
            "\n\nSet to None to maintain the same orientation before and after teleporting.")]
        MatchOrientation m_MatchOrientation = MatchOrientation.WorldSpaceUp;

        /// <summary>
        /// How to orient the rig after teleportation.
        /// </summary>
        /// <remarks>
        /// Set to:
        /// <list type="bullet">
        /// <item>
        /// <term><see cref="MatchOrientation.WorldSpaceUp"/></term>
        /// <description> to stay oriented according to the world space up vector.</description>
        /// </item>
        /// <item>
        /// <term><see cref="MatchOrientation.TargetUp"/></term>
        /// <description> to orient according to the target <see cref="BaseTeleportationInteractable"/> Transform's up vector.</description>
        /// </item>
        /// <item>
        /// <term><see cref="MatchOrientation.TargetUpAndForward"/></term>
        /// <description> to orient according to the target <see cref="BaseTeleportationInteractable"/> Transform's rotation.</description>
        /// </item>
        /// <item>
        /// <term><see cref="MatchOrientation.None"/></term>
        /// <description> to maintain the same orientation before and after teleporting.</description>
        /// </item>
        /// </list>
        /// </remarks>
        public MatchOrientation matchOrientation
        {
            get => m_MatchOrientation;
            set => m_MatchOrientation = value;
        }

        [SerializeField]
        [Tooltip("Specify when the teleportation will be triggered. Options map to when the trigger is pressed or when it is released.")]
        TeleportTrigger m_TeleportTrigger = TeleportTrigger.OnSelectExited;

        /// <summary>
        /// Specifies when the teleportation triggers.
        /// </summary>
        public TeleportTrigger teleportTrigger
        {
            get => m_TeleportTrigger;
            set => m_TeleportTrigger = value;
        }

        [SerializeField]
        TeleportingEvent m_Teleporting = new TeleportingEvent();

        /// <summary>
        /// Gets or sets the event that Unity calls when queuing to teleport via
        /// the <see cref="TeleportationProvider"/>.
        /// </summary>
        /// <remarks>
        /// The <see cref="TeleportingEventArgs"/> passed to each listener is only valid
        /// while the event is invoked, do not hold a reference to it.
        /// </remarks>
        public TeleportingEvent teleporting
        {
            get => m_Teleporting;
            set => m_Teleporting = value;
        }

        // Reusable event args
        readonly LinkedPool<TeleportingEventArgs> m_TeleportingEventArgs = new LinkedPool<TeleportingEventArgs>(() => new TeleportingEventArgs(), collectionCheck: false);

        /// <inheritdoc />
        protected override void Awake()
        {
            base.Awake();
            if (m_TeleportationProvider == null)
            {
                m_TeleportationProvider = FindObjectOfType<TeleportationProvider>();
            }
        }

        /// <inheritdoc />
        protected override void Reset()
        {
            base.Reset();
            selectMode = InteractableSelectMode.Multiple;
        }

        /// <summary>
        /// Automatically called upon the teleport trigger event occurring to generate the teleport request.
        /// The teleportation destination pose should be filled out.
        /// </summary>
        /// <param name="interactor">The interactor that initiated the teleport trigger.</param>
        /// <param name="raycastHit">The ray cast hit information from the interactor.</param>
        /// <param name="teleportRequest">The teleport request that should be filled out during this method call.</param>
        /// <returns>Returns <see langword="true"/> if the teleport request was successfully updated and should be queued. Otherwise, returns <see langword="false"/>.</returns>
        /// <seealso cref="TeleportationProvider.QueueTeleportRequest"/>
        protected virtual bool GenerateTeleportRequest(IXRInteractor interactor, RaycastHit raycastHit, ref TeleportRequest teleportRequest) => false;

        void SendTeleportRequest(IXRInteractor interactor)
        {
            if (interactor == null || m_TeleportationProvider == null)
                return;

            RaycastHit raycastHit = default;
            if (interactor is XRRayInteractor rayInteractor && rayInteractor != null)
            {
                if (rayInteractor.TryGetCurrent3DRaycastHit(out raycastHit))
                {
                    // Are we still selecting this object?
                    var found = false;
                    foreach (var interactionCollider in colliders)
                    {
                        if (interactionCollider == raycastHit.collider)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        return;
                    }
                }
            }

            var teleportRequest = new TeleportRequest
            {
                matchOrientation = m_MatchOrientation,
                requestTime = Time.time,
            };

            bool success;
            if (interactor is XRBaseInteractor baseInteractor)
#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
                success = GenerateTeleportRequest(baseInteractor, raycastHit, ref teleportRequest);
#pragma warning restore 618
            else
                success = GenerateTeleportRequest(interactor, raycastHit, ref teleportRequest);

            if (success)
            {
                success = m_TeleportationProvider.QueueTeleportRequest(teleportRequest);

                if (success && m_Teleporting != null)
                {
                    using (m_TeleportingEventArgs.Get(out var args))
                    {
                        args.interactorObject = interactor;
                        args.interactableObject = this;
                        args.teleportRequest = teleportRequest;
                        m_Teleporting.Invoke(args);
                    }
                }
            }
        }

        /// <inheritdoc />
        protected override void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (m_TeleportTrigger == TeleportTrigger.OnSelectEntered)
                SendTeleportRequest(args.interactorObject);

            base.OnSelectEntered(args);
        }

        /// <inheritdoc />
        protected override void OnSelectExited(SelectExitEventArgs args)
        {
            if (m_TeleportTrigger == TeleportTrigger.OnSelectExited && !args.isCanceled)
                SendTeleportRequest(args.interactorObject);

            base.OnSelectExited(args);
        }

        /// <inheritdoc />
        protected override void OnActivated(ActivateEventArgs args)
        {
            if (m_TeleportTrigger == TeleportTrigger.OnActivated)
                SendTeleportRequest(args.interactorObject);

            base.OnActivated(args);
        }

        /// <inheritdoc />
        protected override void OnDeactivated(DeactivateEventArgs args)
        {
            if (m_TeleportTrigger == TeleportTrigger.OnDeactivated)
                SendTeleportRequest(args.interactorObject);

            base.OnDeactivated(args);
        }
    }
}
