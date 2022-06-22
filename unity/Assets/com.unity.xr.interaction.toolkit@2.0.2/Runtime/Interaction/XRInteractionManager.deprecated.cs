using System;
using System.Collections.Generic;

namespace UnityEngine.XR.Interaction.Toolkit
{
    public partial class XRInteractionManager
    {
        /// <inheritdoc cref="RegisterInteractor(IXRInteractor)"/>
        /// <remarks>
        /// <c>RegisterInteractor(XRBaseInteractor)</c> has been deprecated. Use <see cref="RegisterInteractor(IXRInteractor)"/> instead.
        /// </remarks>
        [Obsolete("RegisterInteractor(XRBaseInteractor) has been deprecated. Use RegisterInteractor(IXRInteractor) instead.")]
        public virtual void RegisterInteractor(XRBaseInteractor interactor) => RegisterInteractor((IXRInteractor)interactor);

        /// <inheritdoc cref="UnregisterInteractor(IXRInteractor)"/>
        /// <remarks>
        /// <c>UnregisterInteractor(XRBaseInteractor)</c> has been deprecated. Use <see cref="UnregisterInteractor(IXRInteractor)"/> instead.
        /// </remarks>
        [Obsolete("UnregisterInteractor(XRBaseInteractor) has been deprecated. Use UnregisterInteractor(IXRInteractor) instead.")]
        public virtual void UnregisterInteractor(XRBaseInteractor interactor) => UnregisterInteractor((IXRInteractor)interactor);

        /// <inheritdoc cref="RegisterInteractable(IXRInteractable)"/>
        /// <remarks>
        /// <c>RegisterInteractable(XRBaseInteractable)</c> has been deprecated. Use <see cref="RegisterInteractable(IXRInteractable)"/> instead.
        /// </remarks>
        [Obsolete("RegisterInteractable(XRBaseInteractable) has been deprecated. Use RegisterInteractable(IXRInteractable) instead.")]
        public virtual void RegisterInteractable(XRBaseInteractable interactable) => RegisterInteractable((IXRInteractable)interactable);

        /// <inheritdoc cref="UnregisterInteractable(IXRInteractable)"/>
        /// <remarks>
        /// <c>UnregisterInteractable(XRBaseInteractable)</c> has been deprecated. Use <see cref="UnregisterInteractable(IXRInteractable)"/> instead.
        /// </remarks>
        [Obsolete("UnregisterInteractable(XRBaseInteractable) has been deprecated. Use UnregisterInteractable(IXRInteractable) instead.")]
        public virtual void UnregisterInteractable(XRBaseInteractable interactable) => UnregisterInteractable((IXRInteractable)interactable);

        /// <inheritdoc cref="GetRegisteredInteractors(List{IXRInteractor})"/>
        /// <remarks>
        /// <c>GetRegisteredInteractors(List&lt;XRBaseInteractor&gt;)</c> has been deprecated. Use <see cref="GetRegisteredInteractors(List{IXRInteractor})"/> instead.
        /// </remarks>
        [Obsolete("GetRegisteredInteractors(List<XRBaseInteractor>) has been deprecated. Use GetRegisteredInteractors(List<IXRInteractor>) instead.")]
        public void GetRegisteredInteractors(List<XRBaseInteractor> results)
        {
            GetRegisteredInteractors(m_ScratchInteractors);
            GetOfType(m_ScratchInteractors, results);
        }

        /// <inheritdoc cref="GetRegisteredInteractables(List{IXRInteractable})"/>
        /// <remarks>
        /// <c>GetRegisteredInteractables(List&lt;XRBaseInteractable&gt;)</c> has been deprecated. Use <see cref="GetRegisteredInteractables(List{IXRInteractable})"/> instead.
        /// </remarks>
        [Obsolete("GetRegisteredInteractables(List<XRBaseInteractable>) has been deprecated. Use GetRegisteredInteractables(List<IXRInteractable>) instead.")]
        public void GetRegisteredInteractables(List<XRBaseInteractable> results)
        {
            GetRegisteredInteractables(m_ScratchInteractables);
            GetOfType(m_ScratchInteractables, results);
        }

        /// <inheritdoc cref="IsRegistered(IXRInteractor)"/>
        /// <remarks>
        /// <c>IsRegistered(XRBaseInteractor)</c> has been deprecated. Use <see cref="IsRegistered(IXRInteractor)"/> instead.
        /// </remarks>
        [Obsolete("IsRegistered(XRBaseInteractor) has been deprecated. Use IsRegistered(IXRInteractor) instead.")]
        public bool IsRegistered(XRBaseInteractor interactor) => IsRegistered((IXRInteractor)interactor);

        /// <inheritdoc cref="IsRegistered(IXRInteractable)"/>
        /// <remarks>
        /// <c>IsRegistered(XRBaseInteractable)</c> has been deprecated. Use <see cref="IsRegistered(IXRInteractable)"/> instead.
        /// </remarks>
        [Obsolete("IsRegistered(XRBaseInteractable) has been deprecated. Use IsRegistered(IXRInteractable) instead.")]
        public bool IsRegistered(XRBaseInteractable interactable) => IsRegistered((IXRInteractable)interactable);

        /// <inheritdoc cref="GetInteractableForCollider"/>
        /// <remarks>
        /// <c>TryGetInteractableForCollider</c> has been deprecated. Use <see cref="GetInteractableForCollider"/> instead.
        /// </remarks>
        [Obsolete("TryGetInteractableForCollider has been deprecated. Use GetInteractableForCollider instead. (UnityUpgradable) -> GetInteractableForCollider(*)")]
        public XRBaseInteractable TryGetInteractableForCollider(Collider interactableCollider)
        {
            return GetInteractableForCollider(interactableCollider);
        }

        /// <summary>
        /// Gets the Interactable a specific collider is attached to.
        /// </summary>
        /// <param name="interactableCollider">The collider of the Interactable to retrieve.</param>
        /// <returns>Returns the Interactable that the collider is attached to. Otherwise returns <see langword="null"/> if no such Interactable exists.</returns>
        /// <remarks>
        /// <c>GetInteractableForCollider</c> has been deprecated. Use <see cref="TryGetInteractableForCollider(Collider, out IXRInteractable)"/> instead.
        /// </remarks>
        [Obsolete("GetInteractableForCollider has been deprecated. Use TryGetInteractableForCollider(Collider, out IXRInteractable) instead.")]
        public XRBaseInteractable GetInteractableForCollider(Collider interactableCollider)
        {
            if (TryGetInteractableForCollider(interactableCollider, out var interactable))
                return interactable as XRBaseInteractable;

            return null;
        }

        /// <summary>
        /// Gets the dictionary that has all the registered colliders and their associated Interactable.
        /// </summary>
        /// <param name="map">When this method returns, contains the dictionary that has all the registered colliders and their associated Interactable.</param>
        /// <remarks>
        /// Clears <paramref name="map"/> before adding to it.
        /// <br />
        /// <c>GetColliderToInteractableMap</c> has been deprecated. GetColliderToInteractableMap has been deprecated. The signature no longer matches the field used by the XRInteractionManager, so a copy is returned instead of a ref. Changes to the returned Dictionary will not be observed by the XRInteractionManager.
        /// </remarks>
        [Obsolete("GetColliderToInteractableMap has been deprecated. The signature no longer matches the field used by the XRInteractionManager, so a copy is returned instead of a ref. Changes to the returned Dictionary will not be observed by the XRInteractionManager.", true)]
        public void GetColliderToInteractableMap(ref Dictionary<Collider, XRBaseInteractable> map)
        {
            if (map != null)
            {
                map.Clear();
                foreach (var kvp in m_ColliderToInteractableMap)
                {
                    if (kvp.Value is XRBaseInteractable baseInteractable)
                        map.Add(kvp.Key, baseInteractable);
                }
            }
        }

        /// <summary>
        /// For the provided <paramref name="interactor"/>, returns a list of the valid Interactables that can be hovered over or selected.
        /// </summary>
        /// <param name="interactor">The Interactor whose valid targets we want to find.</param>
        /// <param name="validTargets">List to be filled with valid targets of the Interactor.</param>
        /// <returns>The list of valid targets of the Interactor.</returns>
        /// <seealso cref="IXRInteractor.GetValidTargets"/>
        /// <remarks>
        /// <c>GetValidTargets(XRBaseInteractor, List&lt;XRBaseInteractable&gt;)</c> has been deprecated. Use <see cref="GetValidTargets(IXRInteractor, List{IXRInteractable})"/> instead.
        /// </remarks>
        [Obsolete("GetValidTargets(XRBaseInteractor, List<XRBaseInteractable>) has been deprecated. Use GetValidTargets(IXRInteractor, List<IXRInteractable>) instead.")]
        public List<XRBaseInteractable> GetValidTargets(XRBaseInteractor interactor, List<XRBaseInteractable> validTargets)
        {
            GetValidTargets(interactor, m_ScratchInteractables);
            GetOfType(m_ScratchInteractables, validTargets);

            return validTargets;
        }

        /// <summary>
        /// Manually forces selection of an Interactable. This is different than starting manual interaction.
        /// </summary>
        /// <param name="interactor">The Interactor that will select the Interactable.</param>
        /// <param name="interactable">The Interactable to be selected.</param>
        /// <remarks>
        /// <c>ForceSelect(XRBaseInteractor, XRBaseInteractable)</c> has been deprecated. Use <see cref="SelectEnter(IXRSelectInteractor, IXRSelectInteractable)"/> instead.
        /// </remarks>
        [Obsolete("ForceSelect(XRBaseInteractor, XRBaseInteractable) has been deprecated. Use SelectEnter(IXRSelectInteractor, IXRSelectInteractable) instead.")]
        public void ForceSelect(XRBaseInteractor interactor, XRBaseInteractable interactable) => SelectEnter(interactor, interactable);

        /// <summary>
        /// Automatically called each frame during Update to clear the selection of the Interactor if necessary due to current conditions.
        /// </summary>
        /// <param name="interactor">The Interactor to potentially exit its selection state.</param>
        /// <remarks>
        /// <c>ClearInteractorSelection(XRBaseInteractor)</c> has been deprecated. Use <see cref="ClearInteractorSelection(IXRSelectInteractor, List{IXRInteractable})"/> instead.
        /// </remarks>
        [Obsolete("ClearInteractorSelection(XRBaseInteractor) has been deprecated. Use ClearInteractorSelection(IXRSelectInteractor, List<IXRInteractable>) instead.")]
        public virtual void ClearInteractorSelection(XRBaseInteractor interactor)
        {
        }

        /// <summary>
        /// Automatically called when an Interactor is unregistered to cancel the selection of the Interactor if necessary.
        /// </summary>
        /// <param name="interactor">The Interactor to potentially exit its selection state due to cancellation.</param>
        /// <remarks>
        /// <c>CancelInteractorSelection(XRBaseInteractor)</c> has been deprecated. Use <see cref="CancelInteractorSelection(IXRSelectInteractor)"/> instead.
        /// </remarks>
        [Obsolete("CancelInteractorSelection(XRBaseInteractor) has been deprecated. Use CancelInteractorSelection(IXRSelectInteractor) instead.")]
        public virtual void CancelInteractorSelection(XRBaseInteractor interactor) => CancelInteractorSelection((IXRSelectInteractor)interactor);

        /// <summary>
        /// Automatically called when an Interactable is unregistered to cancel the selection of the Interactable if necessary.
        /// </summary>
        /// <param name="interactable">The Interactable to potentially exit its selection state due to cancellation.</param>
        /// <remarks>
        /// <c>CancelInteractableSelection(XRBaseInteractable)</c> has been deprecated. Use <see cref="CancelInteractableSelection(IXRSelectInteractable)"/> instead.
        /// </remarks>
        [Obsolete("CancelInteractableSelection(XRBaseInteractable) has been deprecated. Use CancelInteractableSelection(IXRSelectInteractable) instead.")]
        public virtual void CancelInteractableSelection(XRBaseInteractable interactable) => CancelInteractableSelection((IXRSelectInteractable)interactable);

        /// <summary>
        /// Automatically called each frame during Update to clear the hover state of the Interactor if necessary due to current conditions.
        /// </summary>
        /// <param name="interactor">The Interactor to potentially exit its hover state.</param>
        /// <param name="validTargets">The list of interactables that this Interactor could possibly interact with this frame.</param>
        /// <remarks>
        /// <c>ClearInteractorHover(XRBaseInteractor, List&lt;XRBaseInteractable&gt;)</c> has been deprecated. Use <see cref="ClearInteractorHover(IXRHoverInteractor, List{IXRInteractable})"/> instead.
        /// </remarks>
        [Obsolete("ClearInteractorHover(XRBaseInteractor, List<XRBaseInteractable>) has been deprecated. Use ClearInteractorHover(IXRHoverInteractor, List<IXRInteractable>) instead.")]
        public virtual void ClearInteractorHover(XRBaseInteractor interactor, List<XRBaseInteractable> validTargets)
        {
        }

        /// <summary>
        /// Automatically called when an Interactor is unregistered to cancel the hover state of the Interactor if necessary.
        /// </summary>
        /// <param name="interactor">The Interactor to potentially exit its hover state due to cancellation.</param>
        /// <remarks>
        /// <c>CancelInteractorHover(XRBaseInteractor)</c> has been deprecated. Use <see cref="CancelInteractorHover(IXRHoverInteractor)"/> instead.
        /// </remarks>
        [Obsolete("CancelInteractorHover(XRBaseInteractor) has been deprecated. Use CancelInteractorHover(IXRHoverInteractor) instead.")]
        public virtual void CancelInteractorHover(XRBaseInteractor interactor) => CancelInteractorHover((IXRHoverInteractor)interactor);

        /// <summary>
        /// Automatically called when an Interactable is unregistered to cancel the hover state of the Interactable if necessary.
        /// </summary>
        /// <param name="interactable">The Interactable to potentially exit its hover state due to cancellation.</param>
        /// <remarks>
        /// <c>CancelInteractableHover(XRBaseInteractable)</c> has been deprecated. Use <see cref="CancelInteractableHover(IXRHoverInteractable)"/> instead.
        /// </remarks>
        [Obsolete("CancelInteractableHover(XRBaseInteractable) has been deprecated. Use CancelInteractableHover(IXRHoverInteractable) instead.")]
        public virtual void CancelInteractableHover(XRBaseInteractable interactable) => CancelInteractableHover((IXRHoverInteractable)interactable);

        /// <summary>
        /// Initiates selection of an Interactable by an Interactor. This method may first result in other interaction events
        /// such as causing the Interactable to first exit being selected.
        /// </summary>
        /// <param name="interactor">The Interactor that is selecting.</param>
        /// <param name="interactable">The Interactable being selected.</param>
        /// <remarks>
        /// This attempt may be ignored depending on the selection policy of the Interactor and/or the Interactable.
        /// <br />
        /// <c>SelectEnter(XRBaseInteractor, XRBaseInteractable)</c> has been deprecated. Use <see cref="SelectEnter(IXRSelectInteractor, IXRSelectInteractable)"/> instead.
        /// </remarks>
        [Obsolete("SelectEnter(XRBaseInteractor, XRBaseInteractable) has been deprecated. Use SelectEnter(IXRSelectInteractor, IXRSelectInteractable) instead.")]
        public virtual void SelectEnter(XRBaseInteractor interactor, XRBaseInteractable interactable)
#pragma warning disable IDE0004 // ReSharper disable twice RedundantCast
            => SelectEnter((IXRSelectInteractor)interactor, (IXRSelectInteractable)interactable);
#pragma warning restore IDE0004

        /// <summary>
        /// Initiates ending selection of an Interactable by an Interactor.
        /// </summary>
        /// <param name="interactor">The Interactor that is no longer selecting.</param>
        /// <param name="interactable">The Interactable that is no longer being selected.</param>
        /// <remarks>
        /// <c>SelectExit(XRBaseInteractor, XRBaseInteractable)</c> has been deprecated. Use <see cref="SelectExit(IXRSelectInteractor, IXRSelectInteractable)"/> instead.
        /// </remarks>
        [Obsolete("SelectExit(XRBaseInteractor, XRBaseInteractable) has been deprecated. Use SelectExit(IXRSelectInteractor, IXRSelectInteractable) instead.")]
        public virtual void SelectExit(XRBaseInteractor interactor, XRBaseInteractable interactable)
            => SelectExit((IXRSelectInteractor)interactor, (IXRSelectInteractable)interactable);

        /// <summary>
        /// Initiates ending selection of an Interactable by an Interactor due to cancellation,
        /// such as from either being unregistered due to being disabled or destroyed.
        /// </summary>
        /// <param name="interactor">The Interactor that is no longer selecting.</param>
        /// <param name="interactable">The Interactable that is no longer being selected.</param>
        /// <remarks>
        /// <c>SelectCancel(XRBaseInteractor, XRBaseInteractable)</c> has been deprecated. Use <see cref="SelectCancel(IXRSelectInteractor, IXRSelectInteractable)"/> instead.
        /// </remarks>
        [Obsolete("SelectCancel(XRBaseInteractor, XRBaseInteractable) has been deprecated. Use SelectCancel(IXRSelectInteractor, IXRSelectInteractable) instead.")]
        public virtual void SelectCancel(XRBaseInteractor interactor, XRBaseInteractable interactable)
            => SelectCancel((IXRSelectInteractor)interactor, (IXRSelectInteractable)interactable);

        /// <summary>
        /// Initiates hovering of an Interactable by an Interactor.
        /// </summary>
        /// <param name="interactor">The Interactor that is hovering.</param>
        /// <param name="interactable">The Interactable being hovered over.</param>
        /// <remarks>
        /// <c>HoverEnter(XRBaseInteractor, XRBaseInteractable)</c> has been deprecated. Use <see cref="HoverEnter(IXRHoverInteractor, IXRHoverInteractable)"/> instead.
        /// </remarks>
        [Obsolete("HoverEnter(XRBaseInteractor, XRBaseInteractable) has been deprecated. Use HoverEnter(IXRHoverInteractor, IXRHoverInteractable) instead.")]
        public virtual void HoverEnter(XRBaseInteractor interactor, XRBaseInteractable interactable)
#pragma warning disable IDE0004 // ReSharper disable twice RedundantCast
            => HoverEnter((IXRHoverInteractor)interactor, (IXRHoverInteractable)interactable);
#pragma warning restore IDE0004

        /// <summary>
        /// Initiates ending hovering of an Interactable by an Interactor.
        /// </summary>
        /// <param name="interactor">The Interactor that is no longer hovering.</param>
        /// <param name="interactable">The Interactable that is no longer being hovered over.</param>
        /// <remarks>
        /// <c>HoverExit(XRBaseInteractor, XRBaseInteractable)</c> has been deprecated. Use <see cref="HoverExit(IXRHoverInteractor, IXRHoverInteractable)"/> instead.
        /// </remarks>
        [Obsolete("HoverExit(XRBaseInteractor, XRBaseInteractable) has been deprecated. Use HoverExit(IXRHoverInteractor, IXRHoverInteractable) instead.")]
        public virtual void HoverExit(XRBaseInteractor interactor, XRBaseInteractable interactable)
#pragma warning disable IDE0004 // ReSharper disable twice RedundantCast
            => HoverExit((IXRHoverInteractor)interactor, (IXRHoverInteractable)interactable);
#pragma warning restore IDE0004

        /// <summary>
        /// Initiates ending hovering of an Interactable by an Interactor due to cancellation,
        /// such as from either being unregistered due to being disabled or destroyed.
        /// </summary>
        /// <param name="interactor">The Interactor that is no longer hovering.</param>
        /// <param name="interactable">The Interactable that is no longer being hovered over.</param>
        /// <remarks>
        /// <c>HoverCancel(XRBaseInteractor, XRBaseInteractable)</c> has been deprecated. Use <see cref="HoverCancel(IXRHoverInteractor, IXRHoverInteractable)"/> instead.
        /// </remarks>
        [Obsolete("HoverCancel(XRBaseInteractor, XRBaseInteractable) has been deprecated. Use HoverCancel(IXRHoverInteractor, IXRHoverInteractable) instead.")]
        public virtual void HoverCancel(XRBaseInteractor interactor, XRBaseInteractable interactable)
#pragma warning disable IDE0004 // ReSharper disable twice RedundantCast
            => HoverCancel((IXRHoverInteractor)interactor, (IXRHoverInteractable)interactable);
#pragma warning restore IDE0004

        /// <summary>
        /// Initiates selection of an Interactable by an Interactor, passing the given <paramref name="args"/>.
        /// </summary>
        /// <param name="interactor">The Interactor that is selecting.</param>
        /// <param name="interactable">The Interactable being selected.</param>
        /// <param name="args">Event data containing the Interactor and Interactable involved in the event.</param>
        /// <remarks>
        /// <c>SelectExit(XRBaseInteractor, XRBaseInteractable, SelectExitEventArgs)</c> has been deprecated. Use <see cref="SelectExit(IXRSelectInteractor, IXRSelectInteractable, SelectExitEventArgs)"/> instead.
        /// </remarks>
        [Obsolete("SelectExit(XRBaseInteractor, XRBaseInteractable, SelectExitEventArgs) has been deprecated. Use SelectExit(IXRSelectInteractor, IXRSelectInteractable, SelectExitEventArgs) instead.")]
        protected virtual void SelectEnter(XRBaseInteractor interactor, XRBaseInteractable interactable, SelectEnterEventArgs args)
#pragma warning disable IDE0004 // ReSharper disable twice RedundantCast
            => SelectEnter((IXRSelectInteractor)interactor, (IXRSelectInteractable)interactable, args);
#pragma warning restore IDE0004

        /// <summary>
        /// Initiates ending selection of an Interactable by an Interactor, passing the given <paramref name="args"/>.
        /// </summary>
        /// <param name="interactor">The Interactor that is no longer selecting.</param>
        /// <param name="interactable">The Interactable that is no longer being selected.</param>
        /// <param name="args">Event data containing the Interactor and Interactable involved in the event.</param>
        /// <remarks>
        /// <c>SelectExit(XRBaseInteractor, XRBaseInteractable, SelectExitEventArgs)</c> has been deprecated. Use <see cref="SelectExit(IXRSelectInteractor, IXRSelectInteractable, SelectExitEventArgs)"/> instead.
        /// </remarks>
        [Obsolete("SelectExit(XRBaseInteractor, XRBaseInteractable, SelectExitEventArgs) has been deprecated. Use SelectExit(IXRSelectInteractor, IXRSelectInteractable, SelectExitEventArgs) instead.")]
        protected virtual void SelectExit(XRBaseInteractor interactor, XRBaseInteractable interactable, SelectExitEventArgs args)
#pragma warning disable IDE0004 // ReSharper disable twice RedundantCast
            => SelectExit((IXRSelectInteractor)interactor, (IXRSelectInteractable)interactable, args);
#pragma warning restore IDE0004

        /// <summary>
        /// Initiates hovering of an Interactable by an Interactor, passing the given <paramref name="args"/>.
        /// </summary>
        /// <param name="interactor">The Interactor that is hovering.</param>
        /// <param name="interactable">The Interactable being hovered over.</param>
        /// <param name="args">Event data containing the Interactor and Interactable involved in the event.</param>
        /// <remarks>
        /// <c>HoverEnter(XRBaseInteractor, XRBaseInteractable, HoverEnterEventArgs)</c> has been deprecated. Use <see cref="HoverEnter(IXRHoverInteractor, IXRHoverInteractable, HoverEnterEventArgs)"/> instead.
        /// </remarks>
        [Obsolete("HoverEnter(XRBaseInteractor, XRBaseInteractable, HoverEnterEventArgs) has been deprecated. Use HoverEnter(IXRHoverInteractor, IXRHoverInteractable, HoverEnterEventArgs) instead.")]
        protected virtual void HoverEnter(XRBaseInteractor interactor, XRBaseInteractable interactable, HoverEnterEventArgs args)
#pragma warning disable IDE0004 // ReSharper disable twice RedundantCast
            => HoverEnter((IXRHoverInteractor)interactor, (IXRHoverInteractable)interactable, args);
#pragma warning restore IDE0004

        /// <summary>
        /// Initiates ending hovering of an Interactable by an Interactor, passing the given <paramref name="args"/>.
        /// </summary>
        /// <param name="interactor">The Interactor that is no longer hovering.</param>
        /// <param name="interactable">The Interactable that is no longer being hovered over.</param>
        /// <param name="args">Event data containing the Interactor and Interactable involved in the event.</param>
        /// <remarks>
        /// <c>HoverExit(XRBaseInteractor, XRBaseInteractable, HoverExitEventArgs)</c> has been deprecated. Use <see cref="HoverExit(IXRHoverInteractor, IXRHoverInteractable, HoverExitEventArgs)"/> instead.
        /// </remarks>
        [Obsolete("HoverExit(XRBaseInteractor, XRBaseInteractable, HoverExitEventArgs) has been deprecated. Use HoverExit(IXRHoverInteractor, IXRHoverInteractable, HoverExitEventArgs) instead.")]
        protected virtual void HoverExit(XRBaseInteractor interactor, XRBaseInteractable interactable, HoverExitEventArgs args)
#pragma warning disable IDE0004 // ReSharper disable twice RedundantCast
            => HoverExit((IXRHoverInteractor)interactor, (IXRHoverInteractable)interactable, args);
#pragma warning restore IDE0004

        /// <summary>
        /// Automatically called each frame during Update to enter the selection state of the Interactor if necessary due to current conditions.
        /// </summary>
        /// <param name="interactor">The Interactor to potentially enter its selection state.</param>
        /// <param name="validTargets">The list of interactables that this Interactor could possibly interact with this frame.</param>
        /// <remarks>
        /// <c>InteractorSelectValidTargets(XRBaseInteractor, List&lt;XRBaseInteractable&gt;)</c> has been deprecated. Use <see cref="InteractorSelectValidTargets(IXRSelectInteractor, List{IXRInteractable})"/> instead.
        /// </remarks>
        [Obsolete("InteractorSelectValidTargets(XRBaseInteractor, List<XRBaseInteractable>) has been deprecated. Use InteractorSelectValidTargets(IXRSelectInteractor, List<IXRInteractable>) instead.")]
        protected virtual void InteractorSelectValidTargets(XRBaseInteractor interactor, List<XRBaseInteractable> validTargets)
        {
        }

        /// <summary>
        /// Automatically called each frame during Update to enter the hover state of the Interactor if necessary due to current conditions.
        /// </summary>
        /// <param name="interactor">The Interactor to potentially enter its hover state.</param>
        /// <param name="validTargets">The list of interactables that this Interactor could possibly interact with this frame.</param>
        /// <remarks>
        /// <c>InteractorHoverValidTargets(XRBaseInteractor, List&lt;XRBaseInteractable&gt;)</c> has been deprecated. Use <see cref="InteractorHoverValidTargets(IXRHoverInteractor, List{IXRInteractable})"/> instead.
        /// </remarks>
        [Obsolete("InteractorHoverValidTargets(XRBaseInteractor, List<XRBaseInteractable>) has been deprecated. Use InteractorHoverValidTargets(IXRHoverInteractor, List<IXRInteractable>) instead.")]
        protected virtual void InteractorHoverValidTargets(XRBaseInteractor interactor, List<XRBaseInteractable> validTargets)
        {
        }
    }
}
