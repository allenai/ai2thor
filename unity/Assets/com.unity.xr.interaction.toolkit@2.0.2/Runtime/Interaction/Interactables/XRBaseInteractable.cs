using System;
using System.Collections.Generic;
using System.Diagnostics;
#if UNITY_EDITOR
using UnityEditor.XR.Interaction.Toolkit.Utilities;
#endif

namespace UnityEngine.XR.Interaction.Toolkit
{
    /// <summary>
    /// Abstract base class from which all interactable behaviours derive.
    /// This class hooks into the interaction system (via <see cref="XRInteractionManager"/>) and provides base virtual methods for handling
    /// hover and selection.
    /// </summary>
    [SelectionBase]
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_Interactables)]
    public abstract partial class XRBaseInteractable : MonoBehaviour, IXRActivateInteractable, IXRHoverInteractable, IXRSelectInteractable
    {
        /// <summary>
        /// Options for how to process and perform movement of an Interactable.
        /// </summary>
        /// <remarks>
        /// Each method of movement has tradeoffs, and different values may be more appropriate
        /// for each type of Interactable object in a project.
        /// </remarks>
        /// <seealso cref="XRGrabInteractable.movementType"/>
        public enum MovementType
        {
            /// <summary>
            /// Move the Interactable object by setting the velocity and angular velocity of the Rigidbody.
            /// Use this if you don't want the object to be able to move through other Colliders without a Rigidbody
            /// as it follows the Interactor, however with the tradeoff that it can appear to lag behind
            /// and not move as smoothly as <see cref="Instantaneous"/>.
            /// </summary>
            /// <remarks>
            /// Unity sets the velocity values during the FixedUpdate function. This Interactable will move at the
            /// framerate-independent interval of the Physics update, which may be slower than the Update rate.
            /// If the Rigidbody is not set to use interpolation or extrapolation, as the Interactable
            /// follows the Interactor, it may not visually update position each frame and be a slight distance
            /// behind the Interactor or controller due to the difference between the Physics update rate
            /// and the render update rate.
            /// </remarks>
            /// <seealso cref="Rigidbody.velocity"/>
            /// <seealso cref="Rigidbody.angularVelocity"/>
            VelocityTracking,

            /// <summary>
            /// Move the Interactable object by moving the kinematic Rigidbody towards the target position and orientation.
            /// Use this if you want to keep the visual representation synchronized to match its Physics state,
            /// and if you want to allow the object to be able to move through other Colliders without a Rigidbody
            /// as it follows the Interactor.
            /// </summary>
            /// <remarks>
            /// Unity will call the movement methods during the FixedUpdate function. This Interactable will move at the
            /// framerate-independent interval of the Physics update, which may be slower than the Update rate.
            /// If the Rigidbody is not set to use interpolation or extrapolation, as the Interactable
            /// follows the Interactor, it may not visually update position each frame and be a slight distance
            /// behind the Interactor or controller due to the difference between the Physics update rate
            /// and the render update rate. Collisions will be more accurate as compared to <see cref="Instantaneous"/>
            /// since with this method, the Rigidbody will be moved by settings its internal velocity rather than
            /// instantly teleporting to match the Transform pose.
            /// </remarks>
            /// <seealso cref="Rigidbody.MovePosition"/>
            /// <seealso cref="Rigidbody.MoveRotation"/>
            Kinematic,

            /// <summary>
            /// Move the Interactable object by setting the position and rotation of the Transform every frame.
            /// Use this if you want the visual representation to be updated each frame, minimizing latency,
            /// however with the tradeoff that it will be able to move through other Colliders without a Rigidbody
            /// as it follows the Interactor.
            /// </summary>
            /// <remarks>
            /// Unity will set the Transform values each frame, which may be faster than the framerate-independent
            /// interval of the Physics update. The Collider of the Interactable object may be a slight distance
            /// behind the visual as it follows the Interactor due to the difference between the Physics update rate
            /// and the render update rate. Collisions will not be computed as accurately as <see cref="Kinematic"/>
            /// since with this method, the Rigidbody will be forced to instantly teleport poses to match the Transform pose
            /// rather than moving the Rigidbody through setting its internal velocity.
            /// </remarks>
            /// <seealso cref="Transform.position"/>
            /// <seealso cref="Transform.rotation"/>
            Instantaneous,
        }

        /// <inheritdoc />
        public event Action<InteractableRegisteredEventArgs> registered;

        /// <inheritdoc />
        public event Action<InteractableUnregisteredEventArgs> unregistered;

        [SerializeField]
        XRInteractionManager m_InteractionManager;

        /// <summary>
        /// The <see cref="XRInteractionManager"/> that this Interactable will communicate with (will find one if <see langword="null"/>).
        /// </summary>
        public XRInteractionManager interactionManager
        {
            get => m_InteractionManager;
            set
            {
                m_InteractionManager = value;
                if (Application.isPlaying && isActiveAndEnabled)
                    RegisterWithInteractionManager();
            }
        }

        [SerializeField]
#pragma warning disable IDE0044 // Add readonly modifier -- readonly fields cannot be serialized by Unity
        List<Collider> m_Colliders = new List<Collider>();
#pragma warning restore IDE0044

        /// <summary>
        /// (Read Only) Colliders to use for interaction with this Interactable (if empty, will use any child Colliders).
        /// </summary>
        public List<Collider> colliders => m_Colliders;

        [SerializeField]
        LayerMask m_InteractionLayerMask = -1;
        
        [SerializeField]
        InteractionLayerMask m_InteractionLayers = 1;

        /// <summary>
        /// Allows interaction with Interactors whose Interaction Layer Mask overlaps with any Layer in this Interaction Layer Mask.
        /// </summary>
        /// <seealso cref="IXRInteractor.interactionLayers"/>
        /// <seealso cref="IsHoverableBy(IXRHoverInteractor)"/>
        /// <seealso cref="IsSelectableBy(IXRSelectInteractor)"/>
        /// <inheritdoc />
        public InteractionLayerMask interactionLayers
        {
            get => m_InteractionLayers;
            set => m_InteractionLayers = value;
        }

        [SerializeField]
        InteractableSelectMode m_SelectMode = InteractableSelectMode.Single;

        /// <inheritdoc />
        public InteractableSelectMode selectMode
        {
            get => m_SelectMode;
            set => m_SelectMode = value;
        }

        [SerializeField]
        GameObject m_CustomReticle;

        /// <summary>
        /// The reticle that appears at the end of the line when valid.
        /// </summary>
        public GameObject customReticle
        {
            get => m_CustomReticle;
            set => m_CustomReticle = value;
        }

        [SerializeField]
        HoverEnterEvent m_FirstHoverEntered = new HoverEnterEvent();

        /// <inheritdoc />
        public HoverEnterEvent firstHoverEntered
        {
            get => m_FirstHoverEntered;
            set => m_FirstHoverEntered = value;
        }

        [SerializeField]
        HoverExitEvent m_LastHoverExited = new HoverExitEvent();

        /// <inheritdoc />
        public HoverExitEvent lastHoverExited
        {
            get => m_LastHoverExited;
            set => m_LastHoverExited = value;
        }

        [SerializeField]
        HoverEnterEvent m_HoverEntered = new HoverEnterEvent();

        /// <inheritdoc />
        public HoverEnterEvent hoverEntered
        {
            get => m_HoverEntered;
            set => m_HoverEntered = value;
        }

        [SerializeField]
        HoverExitEvent m_HoverExited = new HoverExitEvent();

        /// <inheritdoc />
        public HoverExitEvent hoverExited
        {
            get => m_HoverExited;
            set => m_HoverExited = value;
        }

        [SerializeField]
        SelectEnterEvent m_FirstSelectEntered = new SelectEnterEvent();

        /// <inheritdoc />
        public SelectEnterEvent firstSelectEntered
        {
            get => m_FirstSelectEntered;
            set => m_FirstSelectEntered = value;
        }

        [SerializeField]
        SelectExitEvent m_LastSelectExited = new SelectExitEvent();

        /// <inheritdoc />
        public SelectExitEvent lastSelectExited
        {
            get => m_LastSelectExited;
            set => m_LastSelectExited = value;
        }

        [SerializeField]
        SelectEnterEvent m_SelectEntered = new SelectEnterEvent();

        /// <inheritdoc />
        public SelectEnterEvent selectEntered
        {
            get => m_SelectEntered;
            set => m_SelectEntered = value;
        }

        [SerializeField]
        SelectExitEvent m_SelectExited = new SelectExitEvent();

        /// <inheritdoc />
        public SelectExitEvent selectExited
        {
            get => m_SelectExited;
            set => m_SelectExited = value;
        }

        [SerializeField]
        ActivateEvent m_Activated = new ActivateEvent();

        /// <inheritdoc />
        public ActivateEvent activated
        {
            get => m_Activated;
            set => m_Activated = value;
        }

        [SerializeField]
        DeactivateEvent m_Deactivated = new DeactivateEvent();

        /// <inheritdoc />
        public DeactivateEvent deactivated
        {
            get => m_Deactivated;
            set => m_Deactivated = value;
        }

        /// <inheritdoc />
        public List<IXRHoverInteractor> interactorsHovering { get; } = new List<IXRHoverInteractor>();

        /// <inheritdoc />
        public bool isHovered => interactorsHovering.Count > 0;

        /// <inheritdoc />
        public List<IXRSelectInteractor> interactorsSelecting { get; } = new List<IXRSelectInteractor>();

        /// <inheritdoc />
        public IXRSelectInteractor firstInteractorSelecting { get; private set; }

        /// <inheritdoc />
        public bool isSelected => interactorsSelecting.Count > 0;

        readonly Dictionary<IXRSelectInteractor, Pose> m_AttachPoseOnSelect = new Dictionary<IXRSelectInteractor, Pose>();

        readonly Dictionary<IXRSelectInteractor, Pose> m_LocalAttachPoseOnSelect = new Dictionary<IXRSelectInteractor, Pose>();

        readonly Dictionary<IXRInteractor, GameObject> m_ReticleCache = new Dictionary<IXRInteractor, GameObject>();

        XRInteractionManager m_RegisteredInteractionManager;

        /// <summary>
        /// Cached reference to an <see cref="XRInteractionManager"/> found with <see cref="Object.FindObjectOfType{Type}()"/>.
        /// </summary>
        static XRInteractionManager s_InteractionManagerCache;

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        protected virtual void Reset()
        {
#if UNITY_EDITOR
            m_InteractionManager = EditorComponentLocatorUtility.FindSceneComponentOfType<XRInteractionManager>(gameObject);
#endif
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void Awake()
        {
            // If no colliders were set, populate with children colliders
            if (m_Colliders.Count == 0)
                GetComponentsInChildren(m_Colliders);

            // Setup Interaction Manager
            FindCreateInteractionManager();

            // Warn about use of deprecated events
            if (m_OnFirstHoverEntered.GetPersistentEventCount() > 0 ||
                m_OnLastHoverExited.GetPersistentEventCount() > 0 ||
                m_OnHoverEntered.GetPersistentEventCount() > 0 ||
                m_OnHoverExited.GetPersistentEventCount() > 0 ||
                m_OnSelectEntered.GetPersistentEventCount() > 0 ||
                m_OnSelectExited.GetPersistentEventCount() > 0 ||
                m_OnSelectCanceled.GetPersistentEventCount() > 0 ||
                m_OnActivate.GetPersistentEventCount() > 0 ||
                m_OnDeactivate.GetPersistentEventCount() > 0)
            {
                Debug.LogWarning("Some deprecated Interactable Events are being used. These deprecated events will be removed in a future version." +
                    " Please convert these to use the newer events, and update script method signatures for Dynamic listeners.", this);
            }
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void OnEnable()
        {
            FindCreateInteractionManager();
            RegisterWithInteractionManager();
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void OnDisable()
        {
            UnregisterWithInteractionManager();
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void OnDestroy()
        {
            // Don't need to do anything; method kept for backwards compatibility.
        }

        void FindCreateInteractionManager()
        {
            if (m_InteractionManager != null)
                return;

            if (s_InteractionManagerCache == null)
                s_InteractionManagerCache = FindObjectOfType<XRInteractionManager>();

            if (s_InteractionManagerCache == null)
            {
                var interactionManagerGO = new GameObject("XR Interaction Manager", typeof(XRInteractionManager));
                s_InteractionManagerCache = interactionManagerGO.GetComponent<XRInteractionManager>();
            }

            m_InteractionManager = s_InteractionManagerCache;
        }

        void RegisterWithInteractionManager()
        {
            if (m_RegisteredInteractionManager == m_InteractionManager)
                return;

            UnregisterWithInteractionManager();

            if (m_InteractionManager != null)
            {
#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
                m_InteractionManager.RegisterInteractable(this);
#pragma warning restore 618
                m_RegisteredInteractionManager = m_InteractionManager;
            }
        }

        void UnregisterWithInteractionManager()
        {
            if (m_RegisteredInteractionManager == null)
                return;

#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            m_RegisteredInteractionManager.UnregisterInteractable(this);
#pragma warning restore 618
            m_RegisteredInteractionManager = null;
        }

        /// <inheritdoc />
        public virtual Transform GetAttachTransform(IXRInteractor interactor)
        {
            return transform;
        }

        /// <inheritdoc />
        public Pose GetAttachPoseOnSelect(IXRSelectInteractor interactor)
        {
            return m_AttachPoseOnSelect.TryGetValue(interactor, out var pose) ? pose : Pose.identity;
        }

        /// <inheritdoc />
        public Pose GetLocalAttachPoseOnSelect(IXRSelectInteractor interactor)
        {
            return m_LocalAttachPoseOnSelect.TryGetValue(interactor, out var pose) ? pose : Pose.identity;
        }

        /// <inheritdoc />
        public virtual float GetDistanceSqrToInteractor(IXRInteractor interactor)
        {
            var interactorAttachTransform = interactor?.GetAttachTransform(this);
            if (interactorAttachTransform == null)
                return float.MaxValue;

            var interactorPosition = interactorAttachTransform.position;
            var minDistanceSqr = float.MaxValue;
            foreach (var col in m_Colliders)
            {
                if (col == null || !col.gameObject.activeInHierarchy || !col.enabled)
                    continue;

                var offset = interactorPosition - col.transform.position;
                minDistanceSqr = Mathf.Min(offset.sqrMagnitude, minDistanceSqr);
            }

            return minDistanceSqr;
        }

        /// <summary>
        /// Determines if a given Interactor can hover over this Interactable.
        /// </summary>
        /// <param name="interactor">Interactor to check for a valid hover state with.</param>
        /// <returns>Returns <see langword="true"/> if hovering is valid this frame. Returns <see langword="false"/> if not.</returns>
        /// <seealso cref="IXRHoverInteractor.CanHover"/>
        public virtual bool IsHoverableBy(IXRHoverInteractor interactor) => true;

        /// <summary>
        /// Determines if a given Interactor can select this Interactable.
        /// </summary>
        /// <param name="interactor">Interactor to check for a valid selection with.</param>
        /// <returns>Returns <see langword="true"/> if selection is valid this frame. Returns <see langword="false"/> if not.</returns>
        /// <seealso cref="IXRSelectInteractor.CanSelect"/>
        public virtual bool IsSelectableBy(IXRSelectInteractor interactor) => true;

        /// <summary>
        /// Attaches the custom reticle to the Interactor.
        /// </summary>
        /// <param name="interactor">Interactor that is interacting with this Interactable.</param>
        public virtual void AttachCustomReticle(IXRInteractor interactor)
        {
            var interactorTransform = interactor?.transform;
            if (interactorTransform == null)
                return;

            // Try and find any attached reticle and swap it
            var reticleProvider = interactorTransform.GetComponent<IXRCustomReticleProvider>();
            if (reticleProvider != null)
            {
                if (m_ReticleCache.TryGetValue(interactor, out var prevReticle))
                {
                    Destroy(prevReticle);
                    m_ReticleCache.Remove(interactor);
                }

                if (m_CustomReticle != null)
                {
                    var reticleInstance = Instantiate(m_CustomReticle);
                    m_ReticleCache.Add(interactor, reticleInstance);
                    reticleProvider.AttachCustomReticle(reticleInstance);
                }
            }
        }

        /// <summary>
        /// Removes the custom reticle from the Interactor.
        /// </summary>
        /// <param name="interactor">Interactor that is no longer interacting with this Interactable.</param>
        public virtual void RemoveCustomReticle(IXRInteractor interactor)
        {
            var interactorTransform = interactor?.transform;
            if (interactorTransform == null)
                return;

            // Try and find any attached reticle and swap it
            var reticleProvider = interactorTransform.GetComponent<IXRCustomReticleProvider>();
            if (reticleProvider != null)
            {
                if (m_ReticleCache.TryGetValue(interactor, out var reticleInstance))
                {
                    Destroy(reticleInstance);
                    m_ReticleCache.Remove(interactor);
                    reticleProvider.RemoveCustomReticle();
                }
            }
        }

        /// <summary>
        /// Capture the current Attach Transform pose.
        /// This method is automatically called by Unity to capture the pose during the moment of selection.
        /// </summary>
        /// <param name="interactor">The specific Interactor as context to get the attachment point for.</param>
        /// <remarks>
        /// Unity automatically calls this method during <see cref="OnSelectEntering(SelectEnterEventArgs)"/>
        /// and should not typically need to be called by a user.
        /// </remarks>
        /// <seealso cref="GetAttachPoseOnSelect"/>
        /// <seealso cref="GetLocalAttachPoseOnSelect"/>
        /// <seealso cref="XRBaseInteractor.CaptureAttachPose"/>
        protected void CaptureAttachPose(IXRSelectInteractor interactor)
        {
            var thisAttachTransform = GetAttachTransform(interactor);
            if (thisAttachTransform != null)
            {
                m_AttachPoseOnSelect[interactor] =
                    new Pose(thisAttachTransform.position, thisAttachTransform.rotation);
                m_LocalAttachPoseOnSelect[interactor] =
                    new Pose(thisAttachTransform.localPosition, thisAttachTransform.localRotation);
            }
            else
            {
                m_AttachPoseOnSelect.Remove(interactor);
                m_LocalAttachPoseOnSelect.Remove(interactor);
            }
        }

        /// <inheritdoc />
        public virtual void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
        }

        /// <inheritdoc />
        void IXRInteractable.OnRegistered(InteractableRegisteredEventArgs args) => OnRegistered(args);

        /// <inheritdoc />
        void IXRInteractable.OnUnregistered(InteractableUnregisteredEventArgs args) => OnUnregistered(args);

        /// <inheritdoc />
        void IXRActivateInteractable.OnActivated(ActivateEventArgs args) => OnActivated(args);

        /// <inheritdoc />
        void IXRActivateInteractable.OnDeactivated(DeactivateEventArgs args) => OnDeactivated(args);

        /// <inheritdoc />
        bool IXRHoverInteractable.IsHoverableBy(IXRHoverInteractor interactor)
        {
            if (interactor is XRBaseInteractor baseInteractor)
#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
                return IsHoverableBy(baseInteractor);
#pragma warning restore 618
            return IsHoverableBy(interactor);
        }

        /// <inheritdoc />
        void IXRHoverInteractable.OnHoverEntering(HoverEnterEventArgs args) => OnHoverEntering(args);

        /// <inheritdoc />
        void IXRHoverInteractable.OnHoverEntered(HoverEnterEventArgs args) => OnHoverEntered(args);

        /// <inheritdoc />
        void IXRHoverInteractable.OnHoverExiting(HoverExitEventArgs args) => OnHoverExiting(args);

        /// <inheritdoc />
        void IXRHoverInteractable.OnHoverExited(HoverExitEventArgs args) => OnHoverExited(args);

        /// <inheritdoc />
        bool IXRSelectInteractable.IsSelectableBy(IXRSelectInteractor interactor)
        {
            if (interactor is XRBaseInteractor baseInteractor)
#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
                return IsSelectableBy(baseInteractor);
#pragma warning restore 618
            return IsSelectableBy(interactor);
        }

        /// <inheritdoc />
        void IXRSelectInteractable.OnSelectEntering(SelectEnterEventArgs args) => OnSelectEntering(args);

        /// <inheritdoc />
        void IXRSelectInteractable.OnSelectEntered(SelectEnterEventArgs args) => OnSelectEntered(args);

        /// <inheritdoc />
        void IXRSelectInteractable.OnSelectExiting(SelectExitEventArgs args) => OnSelectExiting(args);

        /// <inheritdoc />
        void IXRSelectInteractable.OnSelectExited(SelectExitEventArgs args) => OnSelectExited(args);

        /// <summary>
        /// The <see cref="XRInteractionManager"/> calls this method
        /// when this Interactable is registered with it.
        /// </summary>
        /// <param name="args">Event data containing the Interaction Manager that registered this Interactable.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="XRInteractionManager.RegisterInteractable(IXRInteractable)"/>
        protected virtual void OnRegistered(InteractableRegisteredEventArgs args)
        {
            if (args.manager != m_InteractionManager)
                Debug.LogWarning($"An Interactable was registered with an unexpected {nameof(XRInteractionManager)}." +
                    $" {this} was expecting to communicate with \"{m_InteractionManager}\" but was registered with \"{args.manager}\".", this);

            registered?.Invoke(args);
        }

        /// <summary>
        /// The <see cref="XRInteractionManager"/> calls this method
        /// when this Interactable is unregistered from it.
        /// </summary>
        /// <param name="args">Event data containing the Interaction Manager that unregistered this Interactable.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="XRInteractionManager.UnregisterInteractable(IXRInteractable)"/>
        protected virtual void OnUnregistered(InteractableUnregisteredEventArgs args)
        {
            if (args.manager != m_RegisteredInteractionManager)
                Debug.LogWarning($"An Interactable was unregistered from an unexpected {nameof(XRInteractionManager)}." +
                    $" {this} was expecting to communicate with \"{m_RegisteredInteractionManager}\" but was unregistered from \"{args.manager}\".", this);

            unregistered?.Invoke(args);
        }

        /// <summary>
        /// The <see cref="XRInteractionManager"/> calls this method
        /// right before the Interactor first initiates hovering over an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is initiating the hover.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnHoverEntered(HoverEnterEventArgs)"/>
        protected virtual void OnHoverEntering(HoverEnterEventArgs args)
        {
            if (m_CustomReticle != null)
            {
                if (args.interactorObject is XRBaseInteractor baseInteractor)
#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
                    AttachCustomReticle(baseInteractor);
#pragma warning restore 618
                else
                    AttachCustomReticle(args.interactorObject);
            }

            interactorsHovering.Add(args.interactorObject);

#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            hoveringInteractors.Add(args.interactor);
            OnHoverEntering(args.interactor);
#pragma warning restore 618
        }

        /// <summary>
        /// The <see cref="XRInteractionManager"/> calls this method
        /// when the Interactor first initiates hovering over an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is initiating the hover.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnHoverExited(HoverExitEventArgs)"/>
        protected virtual void OnHoverEntered(HoverEnterEventArgs args)
        {
            if (interactorsHovering.Count == 1)
                m_FirstHoverEntered?.Invoke(args);

            m_HoverEntered?.Invoke(args);

#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            OnHoverEntered(args.interactor);
#pragma warning restore 618
        }

        /// <summary>
        /// The <see cref="XRInteractionManager"/> calls this method
        /// right before the Interactor ends hovering over an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is ending the hover.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnHoverExited(HoverExitEventArgs)"/>
        protected virtual void OnHoverExiting(HoverExitEventArgs args)
        {
            if (m_CustomReticle != null)
            {
                if (args.interactorObject is XRBaseInteractor baseInteractor)
#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
                    RemoveCustomReticle(baseInteractor);
#pragma warning restore 618
                else
                    RemoveCustomReticle(args.interactorObject);
            }

            interactorsHovering.Remove(args.interactorObject);

#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            hoveringInteractors.Remove(args.interactor);
            OnHoverExiting(args.interactor);
#pragma warning restore 618
        }

        /// <summary>
        /// The <see cref="XRInteractionManager"/> calls this method
        /// when the Interactor ends hovering over an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is ending the hover.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnHoverEntered(HoverEnterEventArgs)"/>
        protected virtual void OnHoverExited(HoverExitEventArgs args)
        {
            if (interactorsHovering.Count == 0)
                m_LastHoverExited?.Invoke(args);

            m_HoverExited?.Invoke(args);

#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            OnHoverExited(args.interactor);
#pragma warning restore 618
        }

        /// <summary>
        /// The <see cref="XRInteractionManager"/> calls this method right
        /// before the Interactor first initiates selection of an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is initiating the selection.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnSelectEntered(SelectEnterEventArgs)"/>
        protected virtual void OnSelectEntering(SelectEnterEventArgs args)
        {
            interactorsSelecting.Add(args.interactorObject);

            if (interactorsSelecting.Count == 1)
                firstInteractorSelecting = args.interactorObject;

            CaptureAttachPose(args.interactorObject);

#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            OnSelectEntering(args.interactor);
#pragma warning restore 618
        }

        /// <summary>
        /// The <see cref="XRInteractionManager"/> calls this method
        /// when the Interactor first initiates selection of an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is initiating the selection.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnSelectExited(SelectExitEventArgs)"/>
        protected virtual void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (interactorsSelecting.Count == 1)
                m_FirstSelectEntered?.Invoke(args);

            m_SelectEntered?.Invoke(args);

#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            OnSelectEntered(args.interactor);
#pragma warning restore 618
        }

        /// <summary>
        /// The <see cref="XRInteractionManager"/> calls this method
        /// right before the Interactor ends selection of an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is ending the selection.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnSelectExited(SelectExitEventArgs)"/>
        protected virtual void OnSelectExiting(SelectExitEventArgs args)
        {
            interactorsSelecting.Remove(args.interactorObject);

#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            if (args.isCanceled)
                OnSelectCanceling(args.interactor);
            else
                OnSelectExiting(args.interactor);
#pragma warning restore 618
        }

        /// <summary>
        /// The <see cref="XRInteractionManager"/> calls this method
        /// when the Interactor ends selection of an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is ending the selection.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnSelectEntered(SelectEnterEventArgs)"/>
        protected virtual void OnSelectExited(SelectExitEventArgs args)
        {
            if (interactorsSelecting.Count == 0)
                m_LastSelectExited?.Invoke(args);

            m_SelectExited?.Invoke(args);

#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            if (args.isCanceled)
                OnSelectCanceled(args.interactor);
            else
                OnSelectExited(args.interactor);
#pragma warning restore 618

            // The dictionaries are pruned so that they don't infinitely grow in size as selections are made.
            if (interactorsSelecting.Count == 0)
            {
                firstInteractorSelecting = null;
                m_AttachPoseOnSelect.Clear();
                m_LocalAttachPoseOnSelect.Clear();
            }
        }

        /// <summary>
        /// <see cref="XRBaseControllerInteractor"/> calls this method when the
        /// Interactor begins an activation event on this Interactable.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is sending the activate event.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnDeactivated"/>
        protected virtual void OnActivated(ActivateEventArgs args)
        {
            m_Activated?.Invoke(args);

#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            OnActivate(args.interactor);
#pragma warning restore 618
        }

        /// <summary>
        /// <see cref="XRBaseControllerInteractor"/> calls this method when the
        /// Interactor ends an activation event on this Interactable.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is sending the deactivate event.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnActivated"/>
        protected virtual void OnDeactivated(DeactivateEventArgs args)
        {
            m_Deactivated?.Invoke(args);

#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            OnDeactivate(args.interactor);
#pragma warning restore 618
        }
    }
}
