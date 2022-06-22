using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Utilities;

namespace UnityEngine.XR.Interaction.Toolkit
{
    /// <summary>
    /// Interactor used for directly interacting with interactables that are touching. This is handled via trigger volumes
    /// that update the current set of valid targets for this interactor. This component must have a collision volume that is
    /// set to be a trigger to work.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("XR/XR Direct Interactor", 11)]
    [HelpURL(XRHelpURLConstants.k_XRDirectInteractor)]
    public partial class XRDirectInteractor : XRBaseControllerInteractor
    {
        /// <summary>
        /// The set of Interactables that this Interactor could possibly interact with this frame.
        /// This list is not sorted by priority.
        /// </summary>
        /// <seealso cref="IXRInteractor.GetValidTargets"/>
        protected List<IXRInteractable> unsortedValidTargets { get; } = new List<IXRInteractable>();

        /// <summary>
        /// The set of Colliders that stayed in touch with this Interactor on fixed updated.
        /// This list will be populated by colliders in OnTriggerStay.
        /// </summary>
        readonly List<Collider> m_StayedColliders = new List<Collider>();

        readonly TriggerContactMonitor m_TriggerContactMonitor = new TriggerContactMonitor();

        /// <summary>
        /// Reusable value of <see cref="WaitForFixedUpdate"/> to reduce allocations.
        /// </summary>
        static readonly WaitForFixedUpdate s_WaitForFixedUpdate = new WaitForFixedUpdate();

        /// <inheritdoc />
        protected override void Awake()
        {
            base.Awake();

            m_TriggerContactMonitor.interactionManager = interactionManager;
            m_TriggerContactMonitor.contactAdded += OnContactAdded;
            m_TriggerContactMonitor.contactRemoved += OnContactRemoved;

            ValidateTriggerCollider();
        }

        /// <inheritdoc />
        protected override void Start()
        {
            base.Start();

            StartCoroutine(UpdateCollidersAfterOnTriggerStay());
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        /// <param name="other">The other <see cref="Collider"/> involved in this collision.</param>
        protected void OnTriggerEnter(Collider other)
        {
            m_TriggerContactMonitor.AddCollider(other);
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        /// <param name="other">The other <see cref="Collider"/> involved in this collision.</param>
        protected void OnTriggerStay(Collider other)
        {
            m_StayedColliders.Add(other);
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        /// <param name="other">The other <see cref="Collider"/> involved in this collision.</param>
        protected void OnTriggerExit(Collider other)
        {
            m_TriggerContactMonitor.RemoveCollider(other);
        }

        /// <summary>
        /// This coroutine functions like a LateFixedUpdate method that executes after OnTriggerXXX.
        /// </summary>
        /// <returns>Returns enumerator for coroutine.</returns>
        IEnumerator UpdateCollidersAfterOnTriggerStay()
        {
            while (true)
            {
                // Wait until the end of the physics cycle so that OnTriggerXXX can get called.
                // See https://docs.unity3d.com/Manual/ExecutionOrder.html
                yield return s_WaitForFixedUpdate;

                m_TriggerContactMonitor.UpdateStayedColliders(m_StayedColliders);
            }
            // ReSharper disable once IteratorNeverReturns -- stopped when behavior is destroyed.
        }

        /// <inheritdoc />
        public override void ProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractor(updatePhase);

            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Fixed)
            {
                // Clear stayed Colliders at the beginning of the physics cycle before
                // the OnTriggerStay method populates this list.
                // Then the UpdateCollidersAfterOnTriggerStay coroutine will use this list to remove Colliders
                // that no longer stay in this frame after previously entered.
                m_StayedColliders.Clear();
            }
        }

        void ValidateTriggerCollider()
        {
            // If there isn't a Rigidbody on the same GameObject, a Trigger Collider has to be on this GameObject
            // for OnTriggerEnter, OnTriggerStay, and OnTriggerExit to be called by Unity. When this has a Rigidbody, Colliders can be
            // on child GameObjects and they don't necessarily have to be Trigger Colliders.
            // See Collision action matrix https://docs.unity3d.com/Manual/CollidersOverview.html
            if (!TryGetComponent(out Rigidbody _))
            {
                var hasTriggerCollider = false;
                foreach (var col in GetComponents<Collider>())
                {
                    if (col.isTrigger)
                    {
                        hasTriggerCollider = true;
                        break;
                    }
                }

                if (!hasTriggerCollider)
                    Debug.LogWarning("Direct Interactor does not have required Collider set as a trigger.", this);
            }
        }

        /// <inheritdoc />
        public override void GetValidTargets(List<IXRInteractable> targets)
        {
            SortingHelpers.SortByDistanceToInteractor(this, unsortedValidTargets, targets);
        }

        /// <inheritdoc />
        public override bool CanHover(IXRHoverInteractable interactable)
        {
            return base.CanHover(interactable) && (!hasSelection || IsSelecting(interactable));
        }

        /// <inheritdoc />
        public override bool CanSelect(IXRSelectInteractable interactable)
        {
            return base.CanSelect(interactable) && (!hasSelection || IsSelecting(interactable));
        }

        /// <inheritdoc />
        protected override void OnRegistered(InteractorRegisteredEventArgs args)
        {
            base.OnRegistered(args);
            args.manager.interactableRegistered += OnInteractableRegistered;
            args.manager.interactableUnregistered += OnInteractableUnregistered;

            // Attempt to resolve any colliders that entered this trigger while this was not subscribed,
            // and filter out any targets that were unregistered while this was not subscribed.
            m_TriggerContactMonitor.interactionManager = args.manager;
            m_TriggerContactMonitor.ResolveUnassociatedColliders();
            XRInteractionManager.RemoveAllUnregistered(args.manager, unsortedValidTargets);
        }

        /// <inheritdoc />
        protected override void OnUnregistered(InteractorUnregisteredEventArgs args)
        {
            base.OnUnregistered(args);
            args.manager.interactableRegistered -= OnInteractableRegistered;
            args.manager.interactableUnregistered -= OnInteractableUnregistered;
        }

        void OnInteractableRegistered(InteractableRegisteredEventArgs args)
        {
            m_TriggerContactMonitor.ResolveUnassociatedColliders(args.interactableObject);
            if (m_TriggerContactMonitor.IsContacting(args.interactableObject) && !unsortedValidTargets.Contains(args.interactableObject))
                unsortedValidTargets.Add(args.interactableObject);
        }

        void OnInteractableUnregistered(InteractableUnregisteredEventArgs args)
        {
            unsortedValidTargets.Remove(args.interactableObject);
        }

        void OnContactAdded(IXRInteractable interactable)
        {
            if (!unsortedValidTargets.Contains(interactable))
                unsortedValidTargets.Add(interactable);
        }

        void OnContactRemoved(IXRInteractable interactable)
        {
            unsortedValidTargets.Remove(interactable);
        }
    }
}