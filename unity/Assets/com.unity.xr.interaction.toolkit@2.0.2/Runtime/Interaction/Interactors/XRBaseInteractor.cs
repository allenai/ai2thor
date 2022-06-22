using System;
using System.Collections.Generic;
using System.Diagnostics;
#if UNITY_EDITOR
using UnityEditor.XR.Interaction.Toolkit.Utilities;
#endif

namespace UnityEngine.XR.Interaction.Toolkit
{
    /// <summary>
    /// Abstract base class from which all interactor behaviours derive.
    /// This class hooks into the interaction system (via <see cref="XRInteractionManager"/>) and provides base virtual methods for handling
    /// hover and selection.
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_Interactors)]
    public abstract partial class XRBaseInteractor : MonoBehaviour, IXRHoverInteractor, IXRSelectInteractor
    {
        /// <inheritdoc />
        public event Action<InteractorRegisteredEventArgs> registered;

        /// <inheritdoc />
        public event Action<InteractorUnregisteredEventArgs> unregistered;

        [SerializeField]
        XRInteractionManager m_InteractionManager;

        /// <summary>
        /// The <see cref="XRInteractionManager"/> that this Interactor will communicate with (will find one if <see langword="null"/>).
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
        LayerMask m_InteractionLayerMask = -1;

        [SerializeField]
        InteractionLayerMask m_InteractionLayers = -1;

        /// <summary>
        /// Allows interaction with Interactables whose Interaction Layer Mask overlaps with any Layer in this Interaction Layer Mask.
        /// </summary>
        /// <seealso cref="IXRInteractable.interactionLayers"/>
        /// <seealso cref="CanHover(IXRHoverInteractable)"/>
        /// <seealso cref="CanSelect(IXRSelectInteractable)"/>
        /// <inheritdoc />
        public InteractionLayerMask interactionLayers
        {
            get => m_InteractionLayers;
            set => m_InteractionLayers = value;
        }

        [SerializeField]
        Transform m_AttachTransform;

        /// <summary>
        /// The <see cref="Transform"/> that is used as the attach point for Interactables.
        /// </summary>
        /// <remarks>
        /// Automatically instantiated and set in <see cref="Awake"/> if <see langword="null"/>.
        /// Setting this will not automatically destroy the previous object.
        /// </remarks>
        public Transform attachTransform
        {
            get => m_AttachTransform;
            set => m_AttachTransform = value;
        }

        [SerializeField]
        bool m_KeepSelectedTargetValid = true;

        /// <inheritdoc />
        public bool keepSelectedTargetValid
        {
            get => m_KeepSelectedTargetValid;
            set => m_KeepSelectedTargetValid = value;
        }

        [SerializeField]
        XRBaseInteractable m_StartingSelectedInteractable;

        /// <summary>
        /// The Interactable that this Interactor automatically selects at startup (optional, may be <see langword="null"/>).
        /// </summary>
        public XRBaseInteractable startingSelectedInteractable
        {
            get => m_StartingSelectedInteractable;
            set => m_StartingSelectedInteractable = value;
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

        bool m_AllowHover = true;

        /// <summary>
        /// Defines whether this interactor allows hover events.
        /// </summary>
        /// <remarks>
        /// A hover exit event will still occur if this value is disabled while hovering.
        /// </remarks>
        public bool allowHover
        {
            get => m_AllowHover;
            set => m_AllowHover = value;
        }

        bool m_AllowSelect = true;

        /// <summary>
        /// Defines whether this interactor allows select events.
        /// </summary>
        /// <remarks>
        /// A select exit event will still occur if this value is disabled while selecting.
        /// </remarks>
        public bool allowSelect
        {
            get => m_AllowSelect;
            set => m_AllowSelect = value;
        }

        bool m_IsPerformingManualInteraction;

        /// <summary>
        /// Defines whether this interactor is performing a manual interaction or not.
        /// </summary>
        /// <seealso cref="StartManualInteraction(IXRSelectInteractable)"/>
        /// <seealso cref="EndManualInteraction"/>
        public bool isPerformingManualInteraction => m_IsPerformingManualInteraction;

        /// <inheritdoc />
        public List<IXRHoverInteractable> interactablesHovered { get; } = new List<IXRHoverInteractable>();

        /// <inheritdoc />
        public bool hasHover => interactablesHovered.Count > 0;

        /// <inheritdoc />
        public List<IXRSelectInteractable> interactablesSelected { get; } = new List<IXRSelectInteractable>();

        /// <inheritdoc />
        public IXRSelectInteractable firstInteractableSelected { get; private set; }

        /// <inheritdoc />
        public bool hasSelection => interactablesSelected.Count > 0;

        readonly Dictionary<IXRSelectInteractable, Pose> m_AttachPoseOnSelect = new Dictionary<IXRSelectInteractable, Pose>();

        readonly Dictionary<IXRSelectInteractable, Pose> m_LocalAttachPoseOnSelect = new Dictionary<IXRSelectInteractable, Pose>();

        readonly HashSet<IXRHoverInteractable> m_UnorderedInteractablesHovered = new HashSet<IXRHoverInteractable>();

        readonly HashSet<IXRSelectInteractable> m_UnorderedInteractablesSelected = new HashSet<IXRSelectInteractable>();

        IXRSelectInteractable m_ManualInteractionInteractable;

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
            // Create empty attach transform if none specified
            if (m_AttachTransform == null)
            {
                var attachGO = new GameObject($"[{gameObject.name}] Attach");
                m_AttachTransform = attachGO.transform;
                m_AttachTransform.SetParent(transform);
                m_AttachTransform.localPosition = Vector3.zero;
                m_AttachTransform.localRotation = Quaternion.identity;
            }

            // Setup Interaction Manager
            FindCreateInteractionManager();

            // Warn about use of deprecated events
            if (m_OnHoverEntered.GetPersistentEventCount() > 0 ||
                m_OnHoverExited.GetPersistentEventCount() > 0 ||
                m_OnSelectEntered.GetPersistentEventCount() > 0 ||
                m_OnSelectExited.GetPersistentEventCount() > 0)
            {
                Debug.LogWarning("Some deprecated Interactor Events are being used. These deprecated events will be removed in a future version." +
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
        protected virtual void Start()
        {
            if (m_InteractionManager != null && m_StartingSelectedInteractable != null)
                m_InteractionManager.SelectEnter(this, (IXRSelectInteractable)m_StartingSelectedInteractable);
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void OnDestroy()
        {
            // Don't need to do anything; method kept for backwards compatibility.
        }

        /// <inheritdoc />
        public virtual Transform GetAttachTransform(IXRInteractable interactable)
        {
            return m_AttachTransform != null ? m_AttachTransform : transform;
        }

        /// <inheritdoc />
        public Pose GetAttachPoseOnSelect(IXRSelectInteractable interactable)
        {
            return m_AttachPoseOnSelect.TryGetValue(interactable, out var pose) ? pose : Pose.identity;
        }

        /// <inheritdoc />
        public Pose GetLocalAttachPoseOnSelect(IXRSelectInteractable interactable)
        {
            return m_LocalAttachPoseOnSelect.TryGetValue(interactable, out var pose) ? pose : Pose.identity;
        }

        /// <inheritdoc />
        public virtual void GetValidTargets(List<IXRInteractable> targets)
        {
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
                m_InteractionManager.RegisterInteractor(this);
#pragma warning restore 618
                m_RegisteredInteractionManager = m_InteractionManager;
            }
        }

        void UnregisterWithInteractionManager()
        {
            if (m_RegisteredInteractionManager == null)
                return;

#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            m_RegisteredInteractionManager.UnregisterInteractor(this);
#pragma warning restore 618
            m_RegisteredInteractionManager = null;
        }

        /// <inheritdoc />
        public virtual bool isHoverActive => m_AllowHover;

        /// <inheritdoc />
        public virtual bool isSelectActive => m_AllowSelect;

        /// <summary>
        /// Determines if the Interactable is valid for hover this frame.
        /// </summary>
        /// <param name="interactable">Interactable to check.</param>
        /// <returns>Returns <see langword="true"/> if the Interactable can be hovered over this frame.</returns>
        /// <seealso cref="IXRHoverInteractable.IsHoverableBy"/>
        public virtual bool CanHover(IXRHoverInteractable interactable) => true;

        /// <summary>
        /// Determines if the Interactable is valid for selection this frame.
        /// </summary>
        /// <param name="interactable">Interactable to check.</param>
        /// <returns>Returns <see langword="true"/> if the Interactable can be selected this frame.</returns>
        /// <seealso cref="IXRSelectInteractable.IsSelectableBy"/>
        public virtual bool CanSelect(IXRSelectInteractable interactable) => true;

        /// <inheritdoc />
        public bool IsHovering(IXRHoverInteractable interactable)
        {
            Debug.Assert(m_UnorderedInteractablesHovered.Count == interactablesHovered.Count, this);
            return m_UnorderedInteractablesHovered.Contains(interactable);
        }

        /// <inheritdoc />
        public bool IsSelecting(IXRSelectInteractable interactable)
        {
            Debug.Assert(m_UnorderedInteractablesSelected.Count == interactablesSelected.Count, this);
            return m_UnorderedInteractablesSelected.Contains(interactable);
        }

        /// <summary>
        /// Determines whether this Interactor is currently hovering the Interactable.
        /// </summary>
        /// <param name="interactable">Interactable to check.</param>
        /// <returns>Returns <see langword="true"/> if this Interactor is currently hovering the Interactable.
        /// Otherwise, returns <seealso langword="false"/>.</returns>
        /// <remarks>
        /// In other words, returns whether <see cref="interactablesHovered"/> contains <paramref name="interactable"/>.
        /// </remarks>
        /// <seealso cref="interactablesHovered"/>
        /// <seealso cref="IXRHoverInteractor.IsHovering"/>
        protected bool IsHovering(IXRInteractable interactable) => interactable is IXRHoverInteractable hoverable && IsHovering(hoverable);

        /// <summary>
        /// Determines whether this Interactor is currently selecting the Interactable.
        /// </summary>
        /// <param name="interactable">Interactable to check.</param>
        /// <returns>Returns <see langword="true"/> if this Interactor is currently selecting the Interactable.
        /// Otherwise, returns <seealso langword="false"/>.</returns>
        /// <remarks>
        /// In other words, returns whether <see cref="interactablesSelected"/> contains <paramref name="interactable"/>.
        /// </remarks>
        /// <seealso cref="interactablesSelected"/>
        /// <seealso cref="IXRSelectInteractor.IsSelecting"/>
        protected bool IsSelecting(IXRInteractable interactable) => interactable is IXRSelectInteractable selectable && IsSelecting(selectable);

        /// <summary>
        /// (Read Only) Overriding movement type of the selected Interactable's movement.
        /// By default, this does not override the movement type.
        /// </summary>
        /// <remarks>
        /// You can use this to change the effective movement type of an Interactable for different
        /// Interactors. An example would be having an Interactable use <see cref="XRBaseInteractable.MovementType.VelocityTracking"/>
        /// so it does not move through geometry with a Collider when interacting with it using a Ray or Direct Interactor,
        /// but have a Socket Interactor override the movement type to be <see cref="XRBaseInteractable.MovementType.Instantaneous"/>
        /// for reduced movement latency.
        /// </remarks>
        /// <seealso cref="XRGrabInteractable.movementType"/>
        public virtual XRBaseInteractable.MovementType? selectedInteractableMovementTypeOverride => null;

        /// <summary>
        /// Capture the current Attach Transform pose.
        /// This method is automatically called by Unity to capture the pose during the moment of selection.
        /// </summary>
        /// <param name="interactable">The specific Interactable as context to get the attachment point for.</param>
        /// <remarks>
        /// Unity automatically calls this method during <see cref="OnSelectEntering(SelectEnterEventArgs)"/>
        /// and should not typically need to be called by a user.
        /// </remarks>
        /// <seealso cref="GetAttachPoseOnSelect"/>
        /// <seealso cref="GetLocalAttachPoseOnSelect"/>
        /// <seealso cref="XRBaseInteractable.CaptureAttachPose"/>
        protected void CaptureAttachPose(IXRSelectInteractable interactable)
        {
            var thisAttachTransform = GetAttachTransform(interactable);
            if (thisAttachTransform != null)
            {
                m_AttachPoseOnSelect[interactable] =
                    new Pose(thisAttachTransform.position, thisAttachTransform.rotation);
                m_LocalAttachPoseOnSelect[interactable] =
                    new Pose(thisAttachTransform.localPosition, thisAttachTransform.localRotation);
            }
            else
            {
                m_AttachPoseOnSelect.Remove(interactable);
                m_LocalAttachPoseOnSelect.Remove(interactable);
            }
        }

        /// <inheritdoc />
        public virtual void PreprocessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
        }

        /// <inheritdoc />
        public virtual void ProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
        }

        /// <inheritdoc />
        void IXRInteractor.OnRegistered(InteractorRegisteredEventArgs args) => OnRegistered(args);

        /// <inheritdoc />
        void IXRInteractor.OnUnregistered(InteractorUnregisteredEventArgs args) => OnUnregistered(args);

        /// <inheritdoc />
        bool IXRHoverInteractor.CanHover(IXRHoverInteractable interactable)
        {
            if (interactable is XRBaseInteractable baseInteractable)
#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
                return CanHover(baseInteractable);
#pragma warning restore 618
            return CanHover(interactable);
        }

        /// <inheritdoc />
        void IXRHoverInteractor.OnHoverEntering(HoverEnterEventArgs args) => OnHoverEntering(args);

        /// <inheritdoc />
        void IXRHoverInteractor.OnHoverEntered(HoverEnterEventArgs args) => OnHoverEntered(args);

        /// <inheritdoc />
        void IXRHoverInteractor.OnHoverExiting(HoverExitEventArgs args) => OnHoverExiting(args);

        /// <inheritdoc />
        void IXRHoverInteractor.OnHoverExited(HoverExitEventArgs args) => OnHoverExited(args);

        /// <inheritdoc />
        bool IXRSelectInteractor.CanSelect(IXRSelectInteractable interactable)
        {
            if (interactable is XRBaseInteractable baseInteractable)
#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
                return CanSelect(baseInteractable);
#pragma warning restore 618
            return CanSelect(interactable);
        }

        /// <inheritdoc />
        void IXRSelectInteractor.OnSelectEntering(SelectEnterEventArgs args) => OnSelectEntering(args);

        /// <inheritdoc />
        void IXRSelectInteractor.OnSelectEntered(SelectEnterEventArgs args) => OnSelectEntered(args);

        /// <inheritdoc />
        void IXRSelectInteractor.OnSelectExiting(SelectExitEventArgs args) => OnSelectExiting(args);

        /// <inheritdoc />
        void IXRSelectInteractor.OnSelectExited(SelectExitEventArgs args) => OnSelectExited(args);

        /// <summary>
        /// The <see cref="XRInteractionManager"/> calls this method
        /// when this Interactor is registered with it.
        /// </summary>
        /// <param name="args">Event data containing the Interaction Manager that registered this Interactor.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="XRInteractionManager.RegisterInteractor(IXRInteractor)"/>
        protected virtual void OnRegistered(InteractorRegisteredEventArgs args)
        {
            if (args.manager != m_InteractionManager)
                Debug.LogWarning($"An Interactor was registered with an unexpected {nameof(XRInteractionManager)}." +
                    $" {this} was expecting to communicate with \"{m_InteractionManager}\" but was registered with \"{args.manager}\".", this);

            registered?.Invoke(args);
        }

        /// <summary>
        /// The <see cref="XRInteractionManager"/> calls this method
        /// when this Interactor is unregistered from it.
        /// </summary>
        /// <param name="args">Event data containing the Interaction Manager that unregistered this Interactor.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="XRInteractionManager.UnregisterInteractor(IXRInteractor)"/>
        protected virtual void OnUnregistered(InteractorUnregisteredEventArgs args)
        {
            if (args.manager != m_RegisteredInteractionManager)
                Debug.LogWarning($"An Interactor was unregistered from an unexpected {nameof(XRInteractionManager)}." +
                    $" {this} was expecting to communicate with \"{m_RegisteredInteractionManager}\" but was unregistered from \"{args.manager}\".", this);

            unregistered?.Invoke(args);
        }

        /// <summary>
        /// The <see cref="XRInteractionManager"/> calls this method
        /// right before the Interactor first initiates hovering over an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactable that is being hovered over.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnHoverEntered(HoverEnterEventArgs)"/>
        protected virtual void OnHoverEntering(HoverEnterEventArgs args)
        {
            interactablesHovered.Add(args.interactableObject);
            var added = m_UnorderedInteractablesHovered.Add(args.interactableObject);
            Debug.Assert(added, "An Interactor received a Hover Enter event for an Interactable that it was already hovering over.", this);

#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            hoverTargets.Add(args.interactable);
            OnHoverEntering(args.interactable);
#pragma warning restore 618
        }

        /// <summary>
        /// The <see cref="XRInteractionManager"/> calls this method
        /// when the Interactor first initiates hovering over an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactable that is being hovered over.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnHoverExited(HoverExitEventArgs)"/>
        protected virtual void OnHoverEntered(HoverEnterEventArgs args)
        {
            m_HoverEntered?.Invoke(args);

#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            OnHoverEntered(args.interactable);
#pragma warning restore 618
        }

        /// <summary>
        /// The <see cref="XRInteractionManager"/> calls this method
        /// right before the Interactor ends hovering over an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactable that is no longer hovered over.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnHoverExited(HoverExitEventArgs)"/>
        protected virtual void OnHoverExiting(HoverExitEventArgs args)
        {
            interactablesHovered.Remove(args.interactableObject);
            var removed = m_UnorderedInteractablesHovered.Remove(args.interactableObject);
            Debug.Assert(removed, "An Interactor received a Hover Exit event for an Interactable that it was not hovering over.", this);

#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            hoverTargets.Remove(args.interactable);
            OnHoverExiting(args.interactable);
#pragma warning restore 618
        }

        /// <summary>
        /// The <see cref="XRInteractionManager"/> calls this method
        /// when the Interactor ends hovering over an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactable that is no longer hovered over.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnHoverEntered(HoverEnterEventArgs)"/>
        protected virtual void OnHoverExited(HoverExitEventArgs args)
        {
            m_HoverExited?.Invoke(args);

#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            OnHoverExited(args.interactable);
#pragma warning restore 618
        }

        /// <summary>
        /// The <see cref="XRInteractionManager"/> calls this method
        /// right before the Interactor first initiates selection of an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactable that is being selected.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnSelectEntered(SelectEnterEventArgs)"/>
        protected virtual void OnSelectEntering(SelectEnterEventArgs args)
        {
            interactablesSelected.Add(args.interactableObject);
            var added = m_UnorderedInteractablesSelected.Add(args.interactableObject);
            Debug.Assert(added, "An Interactor received a Select Enter event for an Interactable that it was already selecting.", this);

            if (interactablesSelected.Count == 1)
                firstInteractableSelected = args.interactableObject;

            CaptureAttachPose(args.interactableObject);

#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            OnSelectEntering(args.interactable);
#pragma warning restore 618
        }

        /// <summary>
        /// The <see cref="XRInteractionManager"/> calls this method
        /// when the Interactor first initiates selection of an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactable that is being selected.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnSelectExited(SelectExitEventArgs)"/>
        protected virtual void OnSelectEntered(SelectEnterEventArgs args)
        {
            m_SelectEntered?.Invoke(args);

#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            OnSelectEntered(args.interactable);
#pragma warning restore 618
        }

        /// <summary>
        /// The <see cref="XRInteractionManager"/> calls this method
        /// right before the Interactor ends selection of an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactable that is no longer selected.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnSelectExited(SelectExitEventArgs)"/>
        protected virtual void OnSelectExiting(SelectExitEventArgs args)
        {
            interactablesSelected.Remove(args.interactableObject);
            var removed = m_UnorderedInteractablesSelected.Remove(args.interactableObject);
            Debug.Assert(removed, "An Interactor received a Select Exit event for an Interactable that it was not selecting.", this);

#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            OnSelectExiting(args.interactable);
#pragma warning restore 618
        }

        /// <summary>
        /// The <see cref="XRInteractionManager"/> calls this method
        /// when the Interactor ends selection of an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactable that is no longer selected.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnSelectEntered(SelectEnterEventArgs)"/>
        protected virtual void OnSelectExited(SelectExitEventArgs args)
        {
            m_SelectExited?.Invoke(args);

#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            OnSelectExited(args.interactable);
#pragma warning restore 618

            // The dictionaries are pruned so that they don't infinitely grow in size as selections are made.
            if (interactablesSelected.Count == 0)
            {
                firstInteractableSelected = null;
                m_AttachPoseOnSelect.Clear();
                m_LocalAttachPoseOnSelect.Clear();
            }
        }

        /// <summary>
        /// Manually initiate selection of an Interactable.
        /// </summary>
        /// <param name="interactable">Interactable that is being selected.</param>
        /// <seealso cref="EndManualInteraction"/>
        public virtual void StartManualInteraction(IXRSelectInteractable interactable)
        {
            if (interactionManager == null)
            {
                Debug.LogWarning("Cannot start manual interaction without an Interaction Manager set.", this);
                return;
            }

            interactionManager.SelectEnter(this, interactable);
            m_IsPerformingManualInteraction = true;
            m_ManualInteractionInteractable = interactable;
        }

        /// <summary>
        /// Ends the manually initiated selection of an Interactable.
        /// </summary>
        /// <seealso cref="StartManualInteraction(IXRSelectInteractable)"/>
        public virtual void EndManualInteraction()
        {
            if (interactionManager == null)
            {
                Debug.LogWarning("Cannot end manual interaction without an Interaction Manager set.", this);
                return;
            }

            if (!m_IsPerformingManualInteraction)
            {
                Debug.LogWarning("Tried to end manual interaction but was not performing manual interaction. Ignoring request.", this);
                return;
            }

            interactionManager.SelectExit(this, m_ManualInteractionInteractable);
            m_IsPerformingManualInteraction = false;
            m_ManualInteractionInteractable = null;
        }
    }
}
