using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine.XR.Interaction.Toolkit.Utilities.Pooling;
#if AR_FOUNDATION_PRESENT
using UnityEngine.XR.Interaction.Toolkit.AR;
#endif

namespace UnityEngine.XR.Interaction.Toolkit
{
    /// <summary>
    /// The Interaction Manager acts as an intermediary between Interactors and Interactables.
    /// It is possible to have multiple Interaction Managers, each with their own valid set of Interactors and Interactables.
    /// Upon being enabled, both Interactors and Interactables register themselves with a valid Interaction Manager
    /// (if a specific one has not already been assigned in the inspector). The loaded scenes must have at least one Interaction Manager
    /// for Interactors and Interactables to be able to communicate.
    /// </summary>
    /// <remarks>
    /// Many of the methods on the Interactors and Interactables are designed to be called by this Interaction Manager
    /// rather than being called directly in order to maintain consistency between both targets of an interaction event.
    /// </remarks>
    /// <seealso cref="IXRInteractor"/>
    /// <seealso cref="IXRInteractable"/>
    [AddComponentMenu("XR/XR Interaction Manager", 11)]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_InteractionManager)]
    [HelpURL(XRHelpURLConstants.k_XRInteractionManager)]
    public partial class XRInteractionManager : MonoBehaviour
    {
        /// <summary>
        /// Calls the methods in its invocation list when an <see cref="IXRInteractor"/> is registered.
        /// </summary>
        /// <remarks>
        /// The <see cref="InteractorRegisteredEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="RegisterInteractor(IXRInteractor)"/>
        /// <seealso cref="IXRInteractor.registered"/>
        public event Action<InteractorRegisteredEventArgs> interactorRegistered;

        /// <summary>
        /// Calls the methods in its invocation list when an <see cref="IXRInteractor"/> is unregistered.
        /// </summary>
        /// <remarks>
        /// The <see cref="InteractorUnregisteredEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="UnregisterInteractor(IXRInteractor)"/>
        /// <seealso cref="IXRInteractor.unregistered"/>
        public event Action<InteractorUnregisteredEventArgs> interactorUnregistered;

        /// <summary>
        /// Calls the methods in its invocation list when an <see cref="IXRInteractable"/> is registered.
        /// </summary>
        /// <remarks>
        /// The <see cref="InteractableRegisteredEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="RegisterInteractable(IXRInteractable)"/>
        /// <seealso cref="IXRInteractable.registered"/>
        public event Action<InteractableRegisteredEventArgs> interactableRegistered;

        /// <summary>
        /// Calls the methods in its invocation list when an <see cref="IXRInteractable"/> is unregistered.
        /// </summary>
        /// <remarks>
        /// The <see cref="InteractableUnregisteredEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="UnregisterInteractable(IXRInteractable)"/>
        /// <seealso cref="IXRInteractable.unregistered"/>
        public event Action<InteractableUnregisteredEventArgs> interactableUnregistered;

        /// <summary>
        /// (Read Only) List of enabled Interaction Manager instances.
        /// </summary>
        /// <remarks>
        /// Intended to be used by XR Interaction Debugger.
        /// </remarks>
        internal static List<XRInteractionManager> activeInteractionManagers { get; } = new List<XRInteractionManager>();

        /// <summary>
        /// Map of all registered objects to test for colliding.
        /// </summary>
        readonly Dictionary<Collider, IXRInteractable> m_ColliderToInteractableMap = new Dictionary<Collider, IXRInteractable>();

        /// <summary>
        /// List of registered Interactors.
        /// </summary>
        readonly RegistrationList<IXRInteractor> m_Interactors = new RegistrationList<IXRInteractor>();

        /// <summary>
        /// List of registered Interactables.
        /// </summary>
        readonly RegistrationList<IXRInteractable> m_Interactables = new RegistrationList<IXRInteractable>();

        /// <summary>
        /// Reusable list of Interactables for retrieving the current hovered Interactables of an Interactor.
        /// </summary>
        readonly List<IXRHoverInteractable> m_CurrentHovered = new List<IXRHoverInteractable>();

        /// <summary>
        /// Reusable list of Interactables for retrieving the current selected Interactables of an Interactor.
        /// </summary>
        readonly List<IXRSelectInteractable> m_CurrentSelected = new List<IXRSelectInteractable>();

        /// <summary>
        /// Reusable list of valid targets for an Interactor.
        /// </summary>
        readonly List<IXRInteractable> m_ValidTargets = new List<IXRInteractable>();

        /// <summary>
        /// Reusable set of valid targets for an Interactor.
        /// </summary>
        readonly HashSet<IXRInteractable> m_UnorderedValidTargets = new HashSet<IXRInteractable>();

        readonly List<XRBaseInteractable> m_DeprecatedValidTargets = new List<XRBaseInteractable>();
        readonly List<IXRInteractor> m_ScratchInteractors = new List<IXRInteractor>();
        readonly List<IXRInteractable> m_ScratchInteractables = new List<IXRInteractable>();

        // Reusable event args
        readonly LinkedPool<SelectEnterEventArgs> m_SelectEnterEventArgs = new LinkedPool<SelectEnterEventArgs>(() => new SelectEnterEventArgs(), collectionCheck: false);
        readonly LinkedPool<SelectExitEventArgs> m_SelectExitEventArgs = new LinkedPool<SelectExitEventArgs>(() => new SelectExitEventArgs(), collectionCheck: false);
        readonly LinkedPool<HoverEnterEventArgs> m_HoverEnterEventArgs = new LinkedPool<HoverEnterEventArgs>(() => new HoverEnterEventArgs(), collectionCheck: false);
        readonly LinkedPool<HoverExitEventArgs> m_HoverExitEventArgs = new LinkedPool<HoverExitEventArgs>(() => new HoverExitEventArgs(), collectionCheck: false);
        readonly LinkedPool<InteractorRegisteredEventArgs> m_InteractorRegisteredEventArgs = new LinkedPool<InteractorRegisteredEventArgs>(() => new InteractorRegisteredEventArgs(), collectionCheck: false);
        readonly LinkedPool<InteractorUnregisteredEventArgs> m_InteractorUnregisteredEventArgs = new LinkedPool<InteractorUnregisteredEventArgs>(() => new InteractorUnregisteredEventArgs(), collectionCheck: false);
        readonly LinkedPool<InteractableRegisteredEventArgs> m_InteractableRegisteredEventArgs = new LinkedPool<InteractableRegisteredEventArgs>(() => new InteractableRegisteredEventArgs(), collectionCheck: false);
        readonly LinkedPool<InteractableUnregisteredEventArgs> m_InteractableUnregisteredEventArgs = new LinkedPool<InteractableUnregisteredEventArgs>(() => new InteractableUnregisteredEventArgs(), collectionCheck: false);

        static readonly ProfilerMarker s_PreprocessInteractorsMarker = new ProfilerMarker("XRI.PreprocessInteractors");
        static readonly ProfilerMarker s_ProcessInteractorsMarker = new ProfilerMarker("XRI.ProcessInteractors");
        static readonly ProfilerMarker s_ProcessInteractablesMarker = new ProfilerMarker("XRI.ProcessInteractables");
        static readonly ProfilerMarker s_GetValidTargetsMarker = new ProfilerMarker("XRI.GetValidTargets");
        static readonly ProfilerMarker s_FilterRegisteredValidTargetsMarker = new ProfilerMarker("XRI.FilterRegisteredValidTargets");
        static readonly ProfilerMarker s_EvaluateInvalidSelectionsMarker = new ProfilerMarker("XRI.EvaluateInvalidSelections");
        static readonly ProfilerMarker s_EvaluateInvalidHoversMarker = new ProfilerMarker("XRI.EvaluateInvalidHovers");
        static readonly ProfilerMarker s_EvaluateValidSelectionsMarker = new ProfilerMarker("XRI.EvaluateValidSelections");
        static readonly ProfilerMarker s_EvaluateValidHoversMarker = new ProfilerMarker("XRI.EvaluateValidHovers");
        static readonly ProfilerMarker s_SelectEnterMarker = new ProfilerMarker("XRI.SelectEnter");
        static readonly ProfilerMarker s_SelectExitMarker = new ProfilerMarker("XRI.SelectExit");
        static readonly ProfilerMarker s_HoverEnterMarker = new ProfilerMarker("XRI.HoverEnter");
        static readonly ProfilerMarker s_HoverExitMarker = new ProfilerMarker("XRI.HoverExit");

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void OnEnable()
        {
            activeInteractionManagers.Add(this);
            Application.onBeforeRender += OnBeforeRender;
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void OnDisable()
        {
            Application.onBeforeRender -= OnBeforeRender;
            activeInteractionManagers.Remove(this);
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        // ReSharper disable PossiblyImpureMethodCallOnReadonlyVariable -- ProfilerMarker.Begin with context object does not have Pure attribute
        protected virtual void Update()
        {
            FlushRegistration();

            using (s_PreprocessInteractorsMarker.Auto())
                PreprocessInteractors(XRInteractionUpdateOrder.UpdatePhase.Dynamic);

            foreach (var interactor in m_Interactors.registeredSnapshot)
            {
                if (!m_Interactors.IsStillRegistered(interactor))
                    continue;

                using (s_GetValidTargetsMarker.Auto())
                    GetValidTargets(interactor, m_ValidTargets);

                // Cast to the abstract base classes to assist with backwards compatibility with existing user code.
                GetOfType(m_ValidTargets, m_DeprecatedValidTargets);

                var selectInteractor = interactor as IXRSelectInteractor;
                var hoverInteractor = interactor as IXRHoverInteractor;

                if (selectInteractor != null)
                {
                    using (s_EvaluateInvalidSelectionsMarker.Auto())
                        ClearInteractorSelectionInternal(selectInteractor, m_ValidTargets);
                }

                if (hoverInteractor != null)
                {
                    using (s_EvaluateInvalidHoversMarker.Auto())
                        ClearInteractorHoverInternal(hoverInteractor, m_ValidTargets, m_DeprecatedValidTargets);
                }

                if (selectInteractor != null)
                {
                    using (s_EvaluateValidSelectionsMarker.Auto())
                        InteractorSelectValidTargetsInternal(selectInteractor, m_ValidTargets, m_DeprecatedValidTargets);
                }

                if (hoverInteractor != null)
                {
                    using (s_EvaluateValidHoversMarker.Auto())
                        InteractorHoverValidTargetsInternal(hoverInteractor, m_ValidTargets, m_DeprecatedValidTargets);
                }
            }

            using (s_ProcessInteractorsMarker.Auto())
                ProcessInteractors(XRInteractionUpdateOrder.UpdatePhase.Dynamic);
            using (s_ProcessInteractablesMarker.Auto())
                ProcessInteractables(XRInteractionUpdateOrder.UpdatePhase.Dynamic);
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void LateUpdate()
        {
            FlushRegistration();

            using (s_ProcessInteractorsMarker.Auto())
                ProcessInteractors(XRInteractionUpdateOrder.UpdatePhase.Late);
            using (s_ProcessInteractablesMarker.Auto())
                ProcessInteractables(XRInteractionUpdateOrder.UpdatePhase.Late);
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void FixedUpdate()
        {
            FlushRegistration();

            using (s_ProcessInteractorsMarker.Auto())
                ProcessInteractors(XRInteractionUpdateOrder.UpdatePhase.Fixed);
            using (s_ProcessInteractablesMarker.Auto())
                ProcessInteractables(XRInteractionUpdateOrder.UpdatePhase.Fixed);
        }

        /// <summary>
        /// Delegate method used to register for "Just Before Render" input updates for VR devices.
        /// </summary>
        /// <seealso cref="Application"/>
        [BeforeRenderOrder(XRInteractionUpdateOrder.k_BeforeRenderOrder)]
        protected virtual void OnBeforeRender()
        {
            FlushRegistration();

            using (s_ProcessInteractorsMarker.Auto())
                ProcessInteractors(XRInteractionUpdateOrder.UpdatePhase.OnBeforeRender);
            using (s_ProcessInteractablesMarker.Auto())
                ProcessInteractables(XRInteractionUpdateOrder.UpdatePhase.OnBeforeRender);
        }
        // ReSharper restore PossiblyImpureMethodCallOnReadonlyVariable

        /// <summary>
        /// Automatically called each frame to preprocess all interactors registered with this manager.
        /// </summary>
        /// <param name="updatePhase">The update phase.</param>
        /// <remarks>
        /// Please see the <see cref="XRInteractionUpdateOrder.UpdatePhase"/> documentation for more details on update order.
        /// </remarks>
        /// <seealso cref="IXRInteractor.PreprocessInteractor"/>
        /// <seealso cref="XRInteractionUpdateOrder.UpdatePhase"/>
        protected virtual void PreprocessInteractors(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            foreach (var interactor in m_Interactors.registeredSnapshot)
            {
                if (!m_Interactors.IsStillRegistered(interactor))
                    continue;

                interactor.PreprocessInteractor(updatePhase);
            }
        }

        /// <summary>
        /// Automatically called each frame to process all interactors registered with this manager.
        /// </summary>
        /// <param name="updatePhase">The update phase.</param>
        /// <remarks>
        /// Please see the <see cref="XRInteractionUpdateOrder.UpdatePhase"/> documentation for more details on update order.
        /// </remarks>
        /// <seealso cref="IXRInteractor.PreprocessInteractor"/>
        /// <seealso cref="XRInteractionUpdateOrder.UpdatePhase"/>
        protected virtual void ProcessInteractors(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            foreach (var interactor in m_Interactors.registeredSnapshot)
            {
                if (!m_Interactors.IsStillRegistered(interactor))
                    continue;

                interactor.ProcessInteractor(updatePhase);
            }
        }

        /// <summary>
        /// Automatically called each frame to process all interactables registered with this manager.
        /// </summary>
        /// <param name="updatePhase">The update phase.</param>
        /// <remarks>
        /// Please see the <see cref="XRInteractionUpdateOrder.UpdatePhase"/> documentation for more details on update order.
        /// </remarks>
        /// <seealso cref="IXRInteractable.ProcessInteractable"/>
        /// <seealso cref="XRInteractionUpdateOrder.UpdatePhase"/>
        protected virtual void ProcessInteractables(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            foreach (var interactable in m_Interactables.registeredSnapshot)
            {
                if (!m_Interactables.IsStillRegistered(interactable))
                    continue;

                interactable.ProcessInteractable(updatePhase);
            }
        }

        /// <summary>
        /// Registers a new Interactor to be processed.
        /// </summary>
        /// <param name="interactor">The Interactor to be registered.</param>
        public virtual void RegisterInteractor(IXRInteractor interactor)
        {
            if (m_Interactors.Register(interactor))
            {
                using (m_InteractorRegisteredEventArgs.Get(out var args))
                {
                    args.manager = this;
                    args.interactorObject = interactor;
                    OnRegistered(args);
                }
            }
        }

        /// <summary>
        /// Automatically called when an Interactor is registered with this Interaction Manager.
        /// Notifies the Interactor, passing the given <paramref name="args"/>.
        /// </summary>
        /// <param name="args">Event data containing the registered Interactor.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="RegisterInteractor(IXRInteractor)"/>
        protected virtual void OnRegistered(InteractorRegisteredEventArgs args)
        {
            Debug.Assert(args.manager == this, this);

            args.interactorObject.OnRegistered(args);
            interactorRegistered?.Invoke(args);
        }

        /// <summary>
        /// Unregister an Interactor so it is no longer processed.
        /// </summary>
        /// <param name="interactor">The Interactor to be unregistered.</param>
        public virtual void UnregisterInteractor(IXRInteractor interactor)
        {
            if (!IsRegistered(interactor))
                return;

            if (interactor is IXRSelectInteractor selectInteractor)
                CancelInteractorSelectionInternal(selectInteractor);

            if (interactor is IXRHoverInteractor hoverInteractor)
                CancelInteractorHoverInternal(hoverInteractor);

            if (m_Interactors.Unregister(interactor))
            {
                using (m_InteractorUnregisteredEventArgs.Get(out var args))
                {
                    args.manager = this;
                    args.interactorObject = interactor;
                    OnUnregistered(args);
                }
            }
        }

        /// <summary>
        /// Automatically called when an Interactor is unregistered from this Interaction Manager.
        /// Notifies the Interactor, passing the given <paramref name="args"/>.
        /// </summary>
        /// <param name="args">Event data containing the unregistered Interactor.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="UnregisterInteractor(IXRInteractor)"/>
        protected virtual void OnUnregistered(InteractorUnregisteredEventArgs args)
        {
            Debug.Assert(args.manager == this, this);

            args.interactorObject.OnUnregistered(args);
            interactorUnregistered?.Invoke(args);
        }

        /// <summary>
        /// Registers a new Interactable to be processed.
        /// </summary>
        /// <param name="interactable">The Interactable to be registered.</param>
        public virtual void RegisterInteractable(IXRInteractable interactable)
        {
            if (m_Interactables.Register(interactable))
            {
                foreach (var interactableCollider in interactable.colliders)
                {
                    if (interactableCollider == null)
                        continue;

                    // Add the association for a fast lookup which maps from Collider to Interactable.
                    // Warn if the same Collider is already used by another registered Interactable
                    // since the lookup will only return the earliest registered rather than a list of all.
                    // The warning is suppressed in the case of gesture interactables since it's common
                    // to compose multiple on the same GameObject.
                    if (!m_ColliderToInteractableMap.TryGetValue(interactableCollider, out var associatedInteractable))
                    {
                        m_ColliderToInteractableMap.Add(interactableCollider, interactable);
                    }
#if AR_FOUNDATION_PRESENT
                    else if (!(interactable is ARBaseGestureInteractable && associatedInteractable is ARBaseGestureInteractable))
#else
                    else
#endif
                    {
                        Debug.LogWarning("A Collider used by an Interactable object is already registered with another Interactable object." +
                            $" The {interactableCollider} will remain associated with {associatedInteractable}, which was registered before {interactable}." +
                            $" The value returned by {nameof(XRInteractionManager)}.{nameof(TryGetInteractableForCollider)} will be the first association.",
                            interactable as Object);
                    }
                }

                using (m_InteractableRegisteredEventArgs.Get(out var args))
                {
                    args.manager = this;
                    args.interactableObject = interactable;
                    OnRegistered(args);
                }
            }
        }

        /// <summary>
        /// Automatically called when an Interactable is registered with this Interaction Manager.
        /// Notifies the Interactable, passing the given <paramref name="args"/>.
        /// </summary>
        /// <param name="args">Event data containing the registered Interactable.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="RegisterInteractable(IXRInteractable)"/>
        protected virtual void OnRegistered(InteractableRegisteredEventArgs args)
        {
            Debug.Assert(args.manager == this, this);

            args.interactableObject.OnRegistered(args);
            interactableRegistered?.Invoke(args);
        }

        /// <summary>
        /// Unregister an Interactable so it is no longer processed.
        /// </summary>
        /// <param name="interactable">The Interactable to be unregistered.</param>
        public virtual void UnregisterInteractable(IXRInteractable interactable)
        {
            if (!IsRegistered(interactable))
                return;

            if (interactable is IXRSelectInteractable selectable)
                CancelInteractableSelectionInternal(selectable);

            if (interactable is IXRHoverInteractable hoverable)
                CancelInteractableHoverInternal(hoverable);

            if (m_Interactables.Unregister(interactable))
            {
                // This makes the assumption that the list of Colliders has not been changed after
                // the Interactable is registered. If any were removed afterward, those would remain
                // in the dictionary.
                foreach (var interactableCollider in interactable.colliders)
                {
                    if (interactableCollider == null)
                        continue;

                    if (m_ColliderToInteractableMap.TryGetValue(interactableCollider, out var associatedInteractable) && associatedInteractable == interactable)
                        m_ColliderToInteractableMap.Remove(interactableCollider);
                }

                using (m_InteractableUnregisteredEventArgs.Get(out var args))
                {
                    args.manager = this;
                    args.interactableObject = interactable;
                    OnUnregistered(args);
                }
            }
        }

        /// <summary>
        /// Automatically called when an Interactable is unregistered from this Interaction Manager.
        /// Notifies the Interactable, passing the given <paramref name="args"/>.
        /// </summary>
        /// <param name="args">Event data containing the unregistered Interactable.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="UnregisterInteractable(IXRInteractable)"/>
        protected virtual void OnUnregistered(InteractableUnregisteredEventArgs args)
        {
            Debug.Assert(args.manager == this, this);

            args.interactableObject.OnUnregistered(args);
            interactableUnregistered?.Invoke(args);
        }

        /// <summary>
        /// Returns all registered Interactors into List <paramref name="results"/>.
        /// </summary>
        /// <param name="results">List to receive registered Interactors.</param>
        /// <remarks>
        /// This method populates the list with the registered Interactors at the time the
        /// method is called. It is not a live view, meaning Interactors
        /// registered or unregistered afterward will not be reflected in the
        /// results of this method.
        /// Clears <paramref name="results"/> before adding to it.
        /// </remarks>
        /// <seealso cref="GetRegisteredInteractables(List{IXRInteractable})"/>
        public void GetRegisteredInteractors(List<IXRInteractor> results)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            m_Interactors.GetRegisteredItems(results);
        }

        /// <summary>
        /// Returns all registered Interactables into List <paramref name="results"/>.
        /// </summary>
        /// <param name="results">List to receive registered Interactables.</param>
        /// <remarks>
        /// This method populates the list with the registered Interactables at the time the
        /// method is called. It is not a live view, meaning Interactables
        /// registered or unregistered afterward will not be reflected in the
        /// results of this method.
        /// Clears <paramref name="results"/> before adding to it.
        /// </remarks>
        /// <seealso cref="GetRegisteredInteractors(List{IXRInteractor})"/>
        public void GetRegisteredInteractables(List<IXRInteractable> results)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));

            m_Interactables.GetRegisteredItems(results);
        }

        /// <summary>
        /// Checks whether the <paramref name="interactor"/> is registered with this Interaction Manager.
        /// </summary>
        /// <param name="interactor">The Interactor to check.</param>
        /// <returns>Returns <see langword="true"/> if registered. Otherwise, returns <see langword="false"/>.</returns>
        /// <seealso cref="RegisterInteractor(IXRInteractor)"/>
        public bool IsRegistered(IXRInteractor interactor)
        {
            return m_Interactors.IsRegistered(interactor);
        }

        /// <summary>
        /// Checks whether the <paramref name="interactable"/> is registered with this Interaction Manager.
        /// </summary>
        /// <param name="interactable">The Interactable to check.</param>
        /// <returns>Returns <see langword="true"/> if registered. Otherwise, returns <see langword="false"/>.</returns>
        /// <seealso cref="RegisterInteractable(IXRInteractable)"/>
        public bool IsRegistered(IXRInteractable interactable)
        {
            return m_Interactables.IsRegistered(interactable);
        }

        /// <summary>
        /// Gets the Interactable a specific <see cref="Collider"/> is attached to.
        /// </summary>
        /// <param name="interactableCollider">The collider of the Interactable to retrieve.</param>
        /// <param name="interactable">The returned Interactable associated with the collider.</param>
        /// <returns>Returns <see langword="true"/> if an Interactable was associated with the collider. Otherwise, returns <see langword="false"/>.</returns>
        public bool TryGetInteractableForCollider(Collider interactableCollider, out IXRInteractable interactable)
        {
            interactable = null;
            return interactableCollider != null && m_ColliderToInteractableMap.TryGetValue(interactableCollider, out interactable) && interactable != null;
        }

        /// <summary>
        /// Retrieves the list of Interactables that the given Interactor could possibly interact with this frame.
        /// This list is sorted by priority (with highest priority first), and will only contain Interactables
        /// that are registered with this Interaction Manager.
        /// </summary>
        /// <param name="interactor">The Interactor to get valid targets for.</param>
        /// <param name="targets">The results list to populate with Interactables that are valid for selection or hover.</param>
        /// <remarks>
        /// Unity expects the <paramref name="interactor"/>'s implementation of <see cref="IXRInteractor.GetValidTargets"/> to clear <paramref name="targets"/> before adding to it.
        /// </remarks>
        /// <seealso cref="IXRInteractor.GetValidTargets"/>
        public void GetValidTargets(IXRInteractor interactor, List<IXRInteractable> targets)
        {
            interactor.GetValidTargets(targets);

            // To attempt to be backwards compatible with user scripts that have not been upgraded to use the interfaces,
            // call the old method to let existing code modify the list.
            if (interactor is XRBaseInteractor baseInteractor)
            {
                m_DeprecatedValidTargets.Clear();
                GetOfType(targets, m_DeprecatedValidTargets);
                if (targets.Count == m_DeprecatedValidTargets.Count)
                {
#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
                    baseInteractor.GetValidTargets(m_DeprecatedValidTargets);
#pragma warning restore 618

                    GetOfType(m_DeprecatedValidTargets, targets);
                }
            }

            // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable -- ProfilerMarker.Begin with context object does not have Pure attribute
            using (s_FilterRegisteredValidTargetsMarker.Auto())
                RemoveAllUnregistered(this, targets);
        }

        /// <summary>
        /// Removes all the Interactables from the given list that are not being handled by the manager.
        /// </summary>
        /// <param name="manager">The Interaction Manager to check registration against.</param>
        /// <param name="interactables">List of elements that will be filtered to exclude those not registered.</param>
        /// <returns>Returns the number of elements removed from the list.</returns>
        /// <remarks>
        /// Does not modify the manager at all, just the list.
        /// </remarks>
        internal static int RemoveAllUnregistered(XRInteractionManager manager, List<IXRInteractable> interactables)
        {
            var numRemoved = 0;
            for (var i = interactables.Count - 1; i >= 0; --i)
            {
                if (!manager.m_Interactables.IsRegistered(interactables[i]))
                {
                    interactables.RemoveAt(i);
                    ++numRemoved;
                }
            }

            return numRemoved;
        }

        /// <summary>
        /// Automatically called each frame during Update to clear the selection of the Interactor if necessary due to current conditions.
        /// </summary>
        /// <param name="interactor">The Interactor to potentially exit its selection state.</param>
        /// <param name="validTargets">The list of interactables that this Interactor could possibly interact with this frame.</param>
        /// <seealso cref="ClearInteractorHover(IXRHoverInteractor, List{IXRInteractable})"/>
        protected virtual void ClearInteractorSelection(IXRSelectInteractor interactor, List<IXRInteractable> validTargets)
        {
            if (interactor.interactablesSelected.Count == 0)
                return;

            m_CurrentSelected.Clear();
            m_CurrentSelected.AddRange(interactor.interactablesSelected);

            // Performance optimization of the Contains checks by putting the valid targets into a HashSet.
            // Some Interactors like ARGestureInteractor can have hundreds of valid Interactables
            // since they will add most ARBaseGestureInteractable instances.
            m_UnorderedValidTargets.Clear();
            if (validTargets.Count > 0)
            {
                foreach (var target in validTargets)
                {
                    m_UnorderedValidTargets.Add(target);
                }
            }

            for (var i = m_CurrentSelected.Count - 1; i >= 0; --i)
            {
                var interactable = m_CurrentSelected[i];
                // Selection, unlike hover, can control whether the interactable has to continue being a valid target
                // to automatically cause it to be deselected.
                if (!interactor.isSelectActive || !HasInteractionLayerOverlap(interactor, interactable) || !interactor.CanSelect(interactable) || !interactable.IsSelectableBy(interactor) || (!interactor.keepSelectedTargetValid && !m_UnorderedValidTargets.Contains(interactable)))
                    SelectExitInternal(interactor, interactable);
            }
        }

        void ClearInteractorSelectionInternal(IXRSelectInteractor interactor, List<IXRInteractable> validTargets)
        {
            ClearInteractorSelection(interactor, validTargets);
            if (interactor is XRBaseInteractor baseInteractor)
#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
                ClearInteractorSelection(baseInteractor);
#pragma warning restore 618
        }

        /// <summary>
        /// Automatically called when an Interactor is unregistered to cancel the selection of the Interactor if necessary.
        /// </summary>
        /// <param name="interactor">The Interactor to potentially exit its selection state due to cancellation.</param>
        public virtual void CancelInteractorSelection(IXRSelectInteractor interactor)
        {
            for (var i = interactor.interactablesSelected.Count - 1; i >= 0; --i)
            {
                SelectCancelInternal(interactor, interactor.interactablesSelected[i]);
            }
        }

        void CancelInteractorSelectionInternal(IXRSelectInteractor interactor)
        {
            if (interactor is XRBaseInteractor baseInteractor)
#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
                CancelInteractorSelection(baseInteractor);
#pragma warning restore 618
            else
                CancelInteractorSelection(interactor);
        }

        /// <summary>
        /// Automatically called when an Interactable is unregistered to cancel the selection of the Interactable if necessary.
        /// </summary>
        /// <param name="interactable">The Interactable to potentially exit its selection state due to cancellation.</param>
        public virtual void CancelInteractableSelection(IXRSelectInteractable interactable)
        {
            for (var i = interactable.interactorsSelecting.Count - 1; i >= 0; --i)
            {
                SelectCancelInternal(interactable.interactorsSelecting[i], interactable);
            }
        }

        void CancelInteractableSelectionInternal(IXRSelectInteractable interactable)
        {
#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            if (interactable is XRBaseInteractable baseInteractable)
                CancelInteractableSelection(baseInteractable);
#pragma warning restore 618
            else
                CancelInteractableSelection(interactable);
        }

        /// <summary>
        /// Automatically called each frame during Update to clear the hover state of the Interactor if necessary due to current conditions.
        /// </summary>
        /// <param name="interactor">The Interactor to potentially exit its hover state.</param>
        /// <param name="validTargets">The list of interactables that this Interactor could possibly interact with this frame.</param>
        /// <seealso cref="ClearInteractorSelection(IXRSelectInteractor, List{IXRInteractable})"/>
        protected virtual void ClearInteractorHover(IXRHoverInteractor interactor, List<IXRInteractable> validTargets)
        {
            if (interactor.interactablesHovered.Count == 0)
                return;

            m_CurrentHovered.Clear();
            m_CurrentHovered.AddRange(interactor.interactablesHovered);

            // Performance optimization of the Contains checks by putting the valid targets into a HashSet.
            // Some Interactors like ARGestureInteractor can have hundreds of valid Interactables
            // since they will add most ARBaseGestureInteractable instances.
            m_UnorderedValidTargets.Clear();
            if (validTargets.Count > 0)
            {
                foreach (var target in validTargets)
                {
                    m_UnorderedValidTargets.Add(target);
                }
            }

            for (var i = m_CurrentHovered.Count - 1; i >= 0; --i)
            {
                var interactable = m_CurrentHovered[i];
                if (!interactor.isHoverActive || !HasInteractionLayerOverlap(interactor, interactable) || !interactor.CanHover(interactable) || !interactable.IsHoverableBy(interactor) || !m_UnorderedValidTargets.Contains(interactable))
                    HoverExitInternal(interactor, interactable);
            }
        }

        void ClearInteractorHoverInternal(IXRHoverInteractor interactor, List<IXRInteractable> validTargets, List<XRBaseInteractable> deprecatedValidTargets)
        {
            ClearInteractorHover(interactor, validTargets);
#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            if (interactor is XRBaseInteractor baseInteractor)
                ClearInteractorHover(baseInteractor, deprecatedValidTargets);
#pragma warning restore 618
        }

        /// <summary>
        /// Automatically called when an Interactor is unregistered to cancel the hover state of the Interactor if necessary.
        /// </summary>
        /// <param name="interactor">The Interactor to potentially exit its hover state due to cancellation.</param>
        public virtual void CancelInteractorHover(IXRHoverInteractor interactor)
        {
            for (var i = interactor.interactablesHovered.Count - 1; i >= 0; --i)
            {
                HoverCancelInternal(interactor, interactor.interactablesHovered[i]);
            }
        }

        void CancelInteractorHoverInternal(IXRHoverInteractor interactor)
        {
#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            if (interactor is XRBaseInteractor baseInteractor)
                CancelInteractorHover(baseInteractor);
#pragma warning restore 618
            else
                CancelInteractorHover(interactor);
        }

        /// <summary>
        /// Automatically called when an Interactable is unregistered to cancel the hover state of the Interactable if necessary.
        /// </summary>
        /// <param name="interactable">The Interactable to potentially exit its hover state due to cancellation.</param>
        public virtual void CancelInteractableHover(IXRHoverInteractable interactable)
        {
            for (var i = interactable.interactorsHovering.Count - 1; i >= 0; --i)
            {
                HoverCancelInternal(interactable.interactorsHovering[i], interactable);
            }
        }

        void CancelInteractableHoverInternal(IXRHoverInteractable interactable)
        {
#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            if (interactable is XRBaseInteractable baseInteractable)
                CancelInteractableHover(baseInteractable);
#pragma warning restore 618
            else
                CancelInteractableHover(interactable);
        }

        /// <summary>
        /// Initiates selection of an Interactable by an Interactor. This method may first result in other interaction events
        /// such as causing the Interactable to first exit being selected.
        /// </summary>
        /// <param name="interactor">The Interactor that is selecting.</param>
        /// <param name="interactable">The Interactable being selected.</param>
        /// <remarks>
        /// This attempt may be ignored depending on the selection policy of the Interactor and/or the Interactable.
        /// </remarks>
        public virtual void SelectEnter(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            if (interactable.isSelected && !ResolveExistingSelect(interactor, interactable))
                return;

            using (m_SelectEnterEventArgs.Get(out var args))
            {
                args.manager = this;
                args.interactorObject = interactor;
                args.interactableObject = interactable;
                SelectEnterInternal(interactor, interactable, args);
            }
        }

        void SelectEnterInternal(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            if (interactor is XRBaseInteractor baseInteractor && interactable is XRBaseInteractable baseInteractable)
                SelectEnter(baseInteractor, baseInteractable);
#pragma warning restore 618
            else
                SelectEnter(interactor, interactable);
        }

        /// <summary>
        /// Initiates ending selection of an Interactable by an Interactor.
        /// </summary>
        /// <param name="interactor">The Interactor that is no longer selecting.</param>
        /// <param name="interactable">The Interactable that is no longer being selected.</param>
        public virtual void SelectExit(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            using (m_SelectExitEventArgs.Get(out var args))
            {
                args.manager = this;
                args.interactorObject = interactor;
                args.interactableObject = interactable;
                args.isCanceled = false;
                SelectExitInternal(interactor, interactable, args);
            }
        }

        void SelectExitInternal(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            if (interactor is XRBaseInteractor baseInteractor && interactable is XRBaseInteractable baseInteractable)
                SelectExit(baseInteractor, baseInteractable);
#pragma warning restore 618
            else
                SelectExit(interactor, interactable);
        }

        /// <summary>
        /// Initiates ending selection of an Interactable by an Interactor due to cancellation,
        /// such as from either being unregistered due to being disabled or destroyed.
        /// </summary>
        /// <param name="interactor">The Interactor that is no longer selecting.</param>
        /// <param name="interactable">The Interactable that is no longer being selected.</param>
        public virtual void SelectCancel(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            using (m_SelectExitEventArgs.Get(out var args))
            {
                args.manager = this;
                args.interactorObject = interactor;
                args.interactableObject = interactable;
                args.isCanceled = true;
                SelectExitInternal(interactor, interactable, args);
            }
        }

        void SelectCancelInternal(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            if (interactor is XRBaseInteractor baseInteractor && interactable is XRBaseInteractable baseInteractable)
                SelectCancel(baseInteractor, baseInteractable);
#pragma warning restore 618
            else
                SelectCancel(interactor, interactable);
        }

        /// <summary>
        /// Initiates hovering of an Interactable by an Interactor.
        /// </summary>
        /// <param name="interactor">The Interactor that is hovering.</param>
        /// <param name="interactable">The Interactable being hovered over.</param>
        public virtual void HoverEnter(IXRHoverInteractor interactor, IXRHoverInteractable interactable)
        {
            using (m_HoverEnterEventArgs.Get(out var args))
            {
                args.manager = this;
                args.interactorObject = interactor;
                args.interactableObject = interactable;
                HoverEnterInternal(interactor, interactable, args);
            }
        }

        void HoverEnterInternal(IXRHoverInteractor interactor, IXRHoverInteractable interactable)
        {
#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            if (interactor is XRBaseInteractor baseInteractor && interactable is XRBaseInteractable baseInteractable)
                HoverEnter(baseInteractor, baseInteractable);
#pragma warning restore 618
            else
                HoverEnter(interactor, interactable);
        }

        /// <summary>
        /// Initiates ending hovering of an Interactable by an Interactor.
        /// </summary>
        /// <param name="interactor">The Interactor that is no longer hovering.</param>
        /// <param name="interactable">The Interactable that is no longer being hovered over.</param>
        public virtual void HoverExit(IXRHoverInteractor interactor, IXRHoverInteractable interactable)
        {
            using (m_HoverExitEventArgs.Get(out var args))
            {
                args.manager = this;
                args.interactorObject = interactor;
                args.interactableObject = interactable;
                args.isCanceled = false;
                HoverExitInternal(interactor, interactable, args);
            }
        }

        void HoverExitInternal(IXRHoverInteractor interactor, IXRHoverInteractable interactable)
        {
#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            if (interactor is XRBaseInteractor baseInteractor && interactable is XRBaseInteractable baseInteractable)
                HoverExit(baseInteractor, baseInteractable);
#pragma warning restore 618
            else
                HoverExit(interactor, interactable);
        }

        /// <summary>
        /// Initiates ending hovering of an Interactable by an Interactor due to cancellation,
        /// such as from either being unregistered due to being disabled or destroyed.
        /// </summary>
        /// <param name="interactor">The Interactor that is no longer hovering.</param>
        /// <param name="interactable">The Interactable that is no longer being hovered over.</param>
        public virtual void HoverCancel(IXRHoverInteractor interactor, IXRHoverInteractable interactable)
        {
            using (m_HoverExitEventArgs.Get(out var args))
            {
                args.manager = this;
                args.interactorObject = interactor;
                args.interactableObject = interactable;
                args.isCanceled = true;
                HoverExitInternal(interactor, interactable, args);
            }
        }

        void HoverCancelInternal(IXRHoverInteractor interactor, IXRHoverInteractable interactable)
        {
#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            if (interactor is XRBaseInteractor baseInteractor && interactable is XRBaseInteractable baseInteractable)
                HoverCancel(baseInteractor, baseInteractable);
#pragma warning restore 618
            else
                HoverCancel(interactor, interactable);
        }

        /// <summary>
        /// Initiates selection of an Interactable by an Interactor, passing the given <paramref name="args"/>.
        /// </summary>
        /// <param name="interactor">The Interactor that is selecting.</param>
        /// <param name="interactable">The Interactable being selected.</param>
        /// <param name="args">Event data containing the Interactor and Interactable involved in the event.</param>
        /// <remarks>
        /// The interactor and interactable are notified immediately without waiting for a previous call to finish
        /// in the case when this method is called again in a nested way. This means that if this method is
        /// called during the handling of the first event, the second will start and finish before the first
        /// event finishes calling all methods in the sequence to notify of the first event.
        /// </remarks>
        // ReSharper disable PossiblyImpureMethodCallOnReadonlyVariable -- ProfilerMarker.Begin with context object does not have Pure attribute
        protected virtual void SelectEnter(IXRSelectInteractor interactor, IXRSelectInteractable interactable, SelectEnterEventArgs args)
        {
            Debug.Assert(args.interactorObject == interactor, this);
            Debug.Assert(args.interactableObject == interactable, this);
            Debug.Assert(args.manager == this || args.manager == null, this);
            args.manager = this;

            using (s_SelectEnterMarker.Auto())
            {
                interactor.OnSelectEntering(args);
                interactable.OnSelectEntering(args);
                interactor.OnSelectEntered(args);
                interactable.OnSelectEntered(args);
            }
        }

        void SelectEnterInternal(IXRSelectInteractor interactor, IXRSelectInteractable interactable, SelectEnterEventArgs args)
        {
#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            if (interactor is XRBaseInteractor baseInteractor && interactable is XRBaseInteractable baseInteractable)
                SelectEnter(baseInteractor, baseInteractable, args);
#pragma warning restore 618
            else
                SelectEnter(interactor, interactable, args);
        }

        /// <summary>
        /// Initiates ending selection of an Interactable by an Interactor, passing the given <paramref name="args"/>.
        /// </summary>
        /// <param name="interactor">The Interactor that is no longer selecting.</param>
        /// <param name="interactable">The Interactable that is no longer being selected.</param>
        /// <param name="args">Event data containing the Interactor and Interactable involved in the event.</param>
        /// <remarks>
        /// The interactor and interactable are notified immediately without waiting for a previous call to finish
        /// in the case when this method is called again in a nested way. This means that if this method is
        /// called during the handling of the first event, the second will start and finish before the first
        /// event finishes calling all methods in the sequence to notify of the first event.
        /// </remarks>
        protected virtual void SelectExit(IXRSelectInteractor interactor, IXRSelectInteractable interactable, SelectExitEventArgs args)
        {
            Debug.Assert(args.interactorObject == interactor, this);
            Debug.Assert(args.interactableObject == interactable, this);
            Debug.Assert(args.manager == this || args.manager == null, this);
            args.manager = this;

            using (s_SelectExitMarker.Auto())
            {
                interactor.OnSelectExiting(args);
                interactable.OnSelectExiting(args);
                interactor.OnSelectExited(args);
                interactable.OnSelectExited(args);
            }
        }

        void SelectExitInternal(IXRSelectInteractor interactor, IXRSelectInteractable interactable, SelectExitEventArgs args)
        {
#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            if (interactor is XRBaseInteractor baseInteractor && interactable is XRBaseInteractable baseInteractable)
                SelectExit(baseInteractor, baseInteractable, args);
#pragma warning restore 618
            else
                SelectExit(interactor, interactable, args);
        }

        /// <summary>
        /// Initiates hovering of an Interactable by an Interactor, passing the given <paramref name="args"/>.
        /// </summary>
        /// <param name="interactor">The Interactor that is hovering.</param>
        /// <param name="interactable">The Interactable being hovered over.</param>
        /// <param name="args">Event data containing the Interactor and Interactable involved in the event.</param>
        /// <remarks>
        /// The interactor and interactable are notified immediately without waiting for a previous call to finish
        /// in the case when this method is called again in a nested way. This means that if this method is
        /// called during the handling of the first event, the second will start and finish before the first
        /// event finishes calling all methods in the sequence to notify of the first event.
        /// </remarks>
        protected virtual void HoverEnter(IXRHoverInteractor interactor, IXRHoverInteractable interactable, HoverEnterEventArgs args)
        {
            Debug.Assert(args.interactorObject == interactor, this);
            Debug.Assert(args.interactableObject == interactable, this);
            Debug.Assert(args.manager == this || args.manager == null, this);
            args.manager = this;

            using (s_HoverEnterMarker.Auto())
            {
                interactor.OnHoverEntering(args);
                interactable.OnHoverEntering(args);
                interactor.OnHoverEntered(args);
                interactable.OnHoverEntered(args);
            }
        }

        void HoverEnterInternal(IXRHoverInteractor interactor, IXRHoverInteractable interactable, HoverEnterEventArgs args)
        {
#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            if (interactor is XRBaseInteractor baseInteractor && interactable is XRBaseInteractable baseInteractable)
                HoverEnter(baseInteractor, baseInteractable, args);
#pragma warning restore 618
            else
                HoverEnter(interactor, interactable, args);
        }

        /// <summary>
        /// Initiates ending hovering of an Interactable by an Interactor, passing the given <paramref name="args"/>.
        /// </summary>
        /// <param name="interactor">The Interactor that is no longer hovering.</param>
        /// <param name="interactable">The Interactable that is no longer being hovered over.</param>
        /// <param name="args">Event data containing the Interactor and Interactable involved in the event.</param>
        /// <remarks>
        /// The interactor and interactable are notified immediately without waiting for a previous call to finish
        /// in the case when this method is called again in a nested way. This means that if this method is
        /// called during the handling of the first event, the second will start and finish before the first
        /// event finishes calling all methods in the sequence to notify of the first event.
        /// </remarks>
        protected virtual void HoverExit(IXRHoverInteractor interactor, IXRHoverInteractable interactable, HoverExitEventArgs args)
        {
            Debug.Assert(args.interactorObject == interactor, this);
            Debug.Assert(args.interactableObject == interactable, this);
            Debug.Assert(args.manager == this || args.manager == null, this);
            args.manager = this;

            using (s_HoverExitMarker.Auto())
            {
                interactor.OnHoverExiting(args);
                interactable.OnHoverExiting(args);
                interactor.OnHoverExited(args);
                interactable.OnHoverExited(args);
            }
        }
        // ReSharper restore PossiblyImpureMethodCallOnReadonlyVariable

        void HoverExitInternal(IXRHoverInteractor interactor, IXRHoverInteractable interactable, HoverExitEventArgs args)
        {
#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            if (interactor is XRBaseInteractor baseInteractor && interactable is XRBaseInteractable baseInteractable)
                HoverExit(baseInteractor, baseInteractable, args);
#pragma warning restore 618
            else
                HoverExit(interactor, interactable, args);
        }

        /// <summary>
        /// Automatically called each frame during Update to enter the selection state of the Interactor if necessary due to current conditions.
        /// </summary>
        /// <param name="interactor">The Interactor to potentially enter its selection state.</param>
        /// <param name="validTargets">The list of interactables that this Interactor could possibly interact with this frame.</param>
        /// <seealso cref="InteractorHoverValidTargets(IXRHoverInteractor, List{IXRInteractable})"/>
        protected virtual void InteractorSelectValidTargets(IXRSelectInteractor interactor, List<IXRInteractable> validTargets)
        {
            foreach (var target in validTargets)
            {
                if (target is IXRSelectInteractable interactable)
                {
                    if (interactor.isSelectActive && HasInteractionLayerOverlap(interactor, interactable) && interactor.CanSelect(interactable) && interactable.IsSelectableBy(interactor) && !interactor.IsSelecting(interactable))
                    {
                        SelectEnterInternal(interactor, interactable);
                    }
                }
            }
        }

        void InteractorSelectValidTargetsInternal(IXRSelectInteractor interactor, List<IXRInteractable> validTargets, List<XRBaseInteractable> deprecatedValidTargets)
        {
            InteractorSelectValidTargets(interactor, validTargets);
#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            if (interactor is XRBaseInteractor baseInteractor)
                InteractorSelectValidTargets(baseInteractor, deprecatedValidTargets);
#pragma warning restore 618
        }

        /// <summary>
        /// Automatically called each frame during Update to enter the hover state of the Interactor if necessary due to current conditions.
        /// </summary>
        /// <param name="interactor">The Interactor to potentially enter its hover state.</param>
        /// <param name="validTargets">The list of interactables that this Interactor could possibly interact with this frame.</param>
        /// <seealso cref="InteractorSelectValidTargets(IXRSelectInteractor, List{IXRInteractable})"/>
        protected virtual void InteractorHoverValidTargets(IXRHoverInteractor interactor, List<IXRInteractable> validTargets)
        {
            foreach (var target in validTargets)
            {
                if (target is IXRHoverInteractable interactable)
                {
                    if (interactor.isHoverActive && HasInteractionLayerOverlap(interactor, interactable) && interactor.CanHover(interactable) && interactable.IsHoverableBy(interactor) && !interactor.IsHovering(interactable))
                    {
                        HoverEnterInternal(interactor, interactable);
                    }
                }
            }
        }

        void InteractorHoverValidTargetsInternal(IXRHoverInteractor interactor, List<IXRInteractable> validTargets, List<XRBaseInteractable> deprecatedValidTargets)
        {
            InteractorHoverValidTargets(interactor, validTargets);
#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            if (interactor is XRBaseInteractor baseInteractor)
                InteractorHoverValidTargets(baseInteractor, deprecatedValidTargets);
#pragma warning restore 618
        }

        /// <summary>
        /// Automatically called when selection of an Interactable by an Interactor is initiated
        /// and the Interactable is already selected.
        /// </summary>
        /// <param name="interactor">The Interactor that is selecting.</param>
        /// <param name="interactable">The Interactable being selected.</param>
        /// <returns>Returns <see langword="true"/> if the existing selection was successfully resolved and selection should continue.
        /// Otherwise, returns <see langword="false"/> if the select should be ignored.</returns>
        /// <seealso cref="SelectEnter(IXRSelectInteractor, IXRSelectInteractable)"/>
        protected virtual bool ResolveExistingSelect(IXRSelectInteractor interactor, IXRSelectInteractable interactable)
        {
            Debug.Assert(interactable.isSelected, this);

            if (interactor.IsSelecting(interactable))
                return false;

#pragma warning disable 618 // Calling deprecated method to help with backwards compatibility with existing user code.
            if (interactor is XRBaseInteractor baseInteractor && baseInteractor.requireSelectExclusive)
                return false;
#pragma warning restore 618

            switch (interactable.selectMode)
            {
                case InteractableSelectMode.Single:
                    ExitInteractableSelection(interactable);
                    break;
                case InteractableSelectMode.Multiple:
                    break;
                default:
                    Debug.Assert(false, $"Unhandled {nameof(InteractableSelectMode)}={interactable.selectMode}", this);
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Determines whether the Interactor and Interactable share at least one interaction layer
        /// between their Interaction Layer Masks.
        /// </summary>
        /// <param name="interactor">The Interactor to check.</param>
        /// <param name="interactable">The Interactable to check.</param>
        /// <returns>Returns <see langword="true"/> if the Interactor and Interactable share at least one interaction layer. Otherwise, returns <see langword="false"/>.</returns>
        /// <seealso cref="IXRInteractor.interactionLayers"/>
        /// <seealso cref="IXRInteractable.interactionLayers"/>
        protected static bool HasInteractionLayerOverlap(IXRInteractor interactor, IXRInteractable interactable)
        {
            return (interactor.interactionLayers & interactable.interactionLayers) != 0;
        }

        void ExitInteractableSelection(IXRSelectInteractable interactable)
        {
            for (var i = interactable.interactorsSelecting.Count - 1; i >= 0; --i)
            {
                SelectExitInternal(interactable.interactorsSelecting[i], interactable);
            }
        }

        void FlushRegistration()
        {
            m_Interactors.Flush();
            m_Interactables.Flush();
        }

        static void GetOfType<TSource, TDestination>(List<TSource> source, List<TDestination> destination)
        {
            destination.Clear();
            foreach (var item in source)
            {
                if (item is TDestination destinationItem)
                {
                    destination.Add(destinationItem);
                }
            }
        }

        /// <summary>
        /// Use this class to maintain a registration of Interactors or Interactables. This maintains
        /// a synchronized list that stays constant until buffered registration status changes are
        /// explicitly committed.
        /// </summary>
        /// <typeparam name="T">The type of object to register, i.e. <see cref="XRBaseInteractor"/> or <see cref="XRBaseInteractable"/>.</typeparam>
        /// <remarks>
        /// Objects may be registered or unregistered from an Interaction Manager
        /// at any time, including when processing objects.
        /// For consistency with the functionality of Unity components which do not have
        /// Update called the same frame in which they are enabled, disabled, or destroyed,
        /// this class will maintain multiple lists to achieve that desired result with processing
        /// Interactors and Interactables.
        /// </remarks>
        internal class RegistrationList<T>
        {
            /// <summary>
            /// A snapshot of registered items that should potentially be processed this update phase of the current frame.
            /// The count of items shall only change upon a call to <see cref="Flush"/>.
            /// </summary>
            /// <remarks>
            /// Items being in this collection does not imply that the item is currently registered.
            /// <br />
            /// Logically this should be a <see cref="IReadOnlyList{T}"/> but is kept as a <see cref="List{T}"/>
            /// to avoid allocations when iterating. Use <see cref="Register"/> and <see cref="Unregister"/>
            /// instead of directly changing this list.
            /// </remarks>
            public List<T> registeredSnapshot { get; } = new List<T>();

            readonly List<T> m_BufferedAdd = new List<T>();
            readonly List<T> m_BufferedRemove = new List<T>();

            readonly HashSet<T> m_UnorderedBufferedAdd = new HashSet<T>();
            readonly HashSet<T> m_UnorderedBufferedRemove = new HashSet<T>();
            readonly HashSet<T> m_UnorderedRegisteredSnapshot = new HashSet<T>();
            readonly HashSet<T> m_UnorderedRegisteredItems = new HashSet<T>();

            /// <summary>
            /// Checks the registration status of <paramref name="item"/>.
            /// </summary>
            /// <param name="item">The item to query.</param>
            /// <returns>Returns <see langword="true"/> if registered. Otherwise, returns <see langword="false"/>.</returns>
            /// <remarks>
            /// This includes pending changes that have not yet been pushed to <see cref="registeredSnapshot"/>.
            /// </remarks>
            /// <seealso cref="IsStillRegistered"/>

            public bool IsRegistered(T item) => m_UnorderedRegisteredItems.Contains(item);

            /// <summary>
            /// Faster variant of <see cref="IsRegistered"/> that assumes that the <paramref name="item"/> is in the snapshot.
            /// It short circuits the check when there are no pending changes to unregister, which is usually the case.
            /// </summary>
            /// <param name="item">The item to query.</param>
            /// <returns>Returns <see langword="true"/> if registered</returns>
            /// <remarks>
            /// This includes pending changes that have not yet been pushed to <see cref="registeredSnapshot"/>.
            /// Use this method instead of <see cref="IsRegistered"/> when iterating over <see cref="registeredSnapshot"/>
            /// for improved performance.
            /// </remarks>
            /// <seealso cref="IsRegistered"/>
            public bool IsStillRegistered(T item) => m_UnorderedBufferedRemove.Count == 0 || !m_UnorderedBufferedRemove.Contains(item);

            /// <summary>
            /// Register <paramref name="item"/>.
            /// </summary>
            /// <param name="item">The item to register.</param>
            /// <returns>Returns <see langword="true"/> if a change in registration status occurred. Otherwise, returns <see langword="false"/>.</returns>
            public bool Register(T item)
            {
                if (m_UnorderedBufferedAdd.Count > 0 && m_UnorderedBufferedAdd.Contains(item))
                    return false;

                if ((m_UnorderedBufferedRemove.Count > 0 && m_UnorderedBufferedRemove.Remove(item)) || !m_UnorderedRegisteredSnapshot.Contains(item))
                {
                    m_BufferedRemove.Remove(item);
                    m_BufferedAdd.Add(item);
                    m_UnorderedBufferedAdd.Add(item);
                    m_UnorderedRegisteredItems.Add(item);
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Unregister <paramref name="item"/>.
            /// </summary>
            /// <param name="item">The item to unregister.</param>
            /// <returns>Returns <see langword="true"/> if a change in registration status occurred. Otherwise, returns <see langword="false"/>.</returns>
            public bool Unregister(T item)
            {
                if (m_UnorderedBufferedRemove.Count > 0 && m_BufferedRemove.Contains(item))
                    return false;

                if ((m_UnorderedBufferedAdd.Count > 0 && m_UnorderedBufferedAdd.Remove(item)) || m_UnorderedRegisteredSnapshot.Contains(item))
                {
                    m_BufferedAdd.Remove(item);
                    m_BufferedRemove.Add(item);
                    m_UnorderedBufferedRemove.Add(item);
                    m_UnorderedRegisteredItems.Remove(item);
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Flush pending registration changes into <see cref="registeredSnapshot"/>.
            /// </summary>
            public void Flush()
            {
                // This method is called multiple times each frame,
                // so additional explicit Count checks are done for
                // performance.
                if (m_BufferedRemove.Count > 0)
                {
                    foreach (var item in m_BufferedRemove)
                    {
                        registeredSnapshot.Remove(item);
                        m_UnorderedRegisteredSnapshot.Remove(item);
                    }

                    m_BufferedRemove.Clear();
                    m_UnorderedBufferedRemove.Clear();
                }

                if (m_BufferedAdd.Count > 0)
                {
                    foreach (var item in m_BufferedAdd)
                    {
                        if (!m_UnorderedRegisteredSnapshot.Contains(item))
                        {
                            registeredSnapshot.Add(item);
                            m_UnorderedRegisteredSnapshot.Add(item);
                        }
                    }

                    m_BufferedAdd.Clear();
                    m_UnorderedBufferedAdd.Clear();
                }
            }

            /// <summary>
            /// Return all registered items into List <paramref name="results"/> in the order they were registered.
            /// </summary>
            /// <param name="results">List to receive registered items.</param>
            /// <remarks>
            /// Clears <paramref name="results"/> before adding to it.
            /// </remarks>
            public void GetRegisteredItems(List<T> results)
            {
                if (results == null)
                    throw new ArgumentNullException(nameof(results));

                results.Clear();
                EnsureCapacity(results, registeredSnapshot.Count - m_BufferedRemove.Count + m_BufferedAdd.Count);
                foreach (var item in registeredSnapshot)
                {
                    if (m_UnorderedBufferedRemove.Count > 0 && m_UnorderedBufferedRemove.Contains(item))
                        continue;

                    results.Add(item);
                }

                results.AddRange(m_BufferedAdd);
            }

            static void EnsureCapacity(List<T> list, int capacity)
            {
                if (list.Capacity < capacity)
                    list.Capacity = capacity;
            }
        }
    }
}
