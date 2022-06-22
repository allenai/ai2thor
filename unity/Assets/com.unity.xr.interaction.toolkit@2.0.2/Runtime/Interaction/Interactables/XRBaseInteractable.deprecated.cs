using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace UnityEngine.XR.Interaction.Toolkit
{
    public abstract partial class XRBaseInteractable
    {
#pragma warning disable 618
        [SerializeField, FormerlySerializedAs("m_OnFirstHoverEnter")]
        XRInteractableEvent m_OnFirstHoverEntered = new XRInteractableEvent();

        /// <summary>
        /// (Deprecated) Allows interaction with Interactors whose Interaction Layer Mask overlaps with any Layer in this Interaction Layer Mask.
        /// </summary>
        /// <seealso cref="XRBaseInteractor.interactionLayerMask"/>
        /// <remarks><c>interactionLayerMask</c> has been deprecated. Use <see cref="interactionLayers"/> instead.</remarks>
        [Obsolete("interactionLayerMask has been deprecated. Use interactionLayers instead.")]
        public LayerMask interactionLayerMask
        {
            get => m_InteractionLayerMask;
            set => m_InteractionLayerMask = value;
        }

        /// <summary>
        /// (Deprecated) Gets or sets the event that Unity calls only when the first Interactor begins hovering
        /// over this Interactable as the sole hovering Interactor. Subsequent Interactors that
        /// begin hovering over this Interactable will not cause this event to be invoked as
        /// long as any others are still hovering.
        /// </summary>
        /// <remarks><c>onFirstHoverEntered</c> has been deprecated. Use <see cref="firstHoverEntered"/> with updated signature instead.</remarks>
        [Obsolete("onFirstHoverEntered has been deprecated. Use firstHoverEntered with updated signature instead.")]
        public XRInteractableEvent onFirstHoverEntered
        {
            get => m_OnFirstHoverEntered;
            set => m_OnFirstHoverEntered = value;
        }

        [SerializeField, FormerlySerializedAs("m_OnLastHoverExit")]
        XRInteractableEvent m_OnLastHoverExited = new XRInteractableEvent();

        /// <summary>
        /// (Deprecated) Gets or sets the event that Unity calls only when the last remaining hovering Interactor
        /// ends hovering over this Interactable.
        /// </summary>
        /// <remarks><c>onLastHoverExited</c> has been deprecated. Use <see cref="lastHoverExited"/> with updated signature instead.</remarks>
        [Obsolete("onLastHoverExited has been deprecated. Use lastHoverExited with updated signature instead.")]
        public XRInteractableEvent onLastHoverExited
        {
            get => m_OnLastHoverExited;
            set => m_OnLastHoverExited = value;
        }

        [SerializeField, FormerlySerializedAs("m_OnHoverEnter")]
        XRInteractableEvent m_OnHoverEntered = new XRInteractableEvent();

        /// <summary>
        /// (Deprecated) Gets or sets the event that Unity calls when an Interactor begins hovering over this Interactable.
        /// </summary>
        /// <remarks><c>onHoverEntered</c> has been deprecated. Use <see cref="hoverEntered"/> instead.</remarks>
        [Obsolete("onHoverEntered has been deprecated. Use hoverEntered with updated signature instead.")]
        public XRInteractableEvent onHoverEntered
        {
            get => m_OnHoverEntered;
            set => m_OnHoverEntered = value;
        }

        [SerializeField, FormerlySerializedAs("m_OnHoverExit")]
        XRInteractableEvent m_OnHoverExited = new XRInteractableEvent();

        /// <summary>
        /// (Deprecated) Gets or sets the event that Unity calls when an Interactor ends hovering over this Interactable.
        /// </summary>
        /// <remarks><c>onHoverExited</c> has been deprecated. Use <see cref="hoverExited"/> hoverExited with updated signature instead.</remarks>
        [Obsolete("onHoverExited has been deprecated. Use hoverExited with updated signature instead.")]
        public XRInteractableEvent onHoverExited
        {
            get => m_OnHoverExited;
            set => m_OnHoverExited = value;
        }

        [SerializeField, FormerlySerializedAs("m_OnSelectEnter")]
        XRInteractableEvent m_OnSelectEntered = new XRInteractableEvent();

        /// <summary>
        /// (Deprecated) Gets or sets the event that Unity calls when an Interactor begins selecting this Interactable.
        /// </summary>
        /// <remarks><c>onSelectEntered</c> has been deprecated. Use <see cref="selectEntered"/> with updated signature instead.</remarks>
        [Obsolete("onSelectEntered has been deprecated. Use selectEntered with updated signature instead.")]
        public XRInteractableEvent onSelectEntered
        {
            get => m_OnSelectEntered;
            set => m_OnSelectEntered = value;
        }

        [SerializeField, FormerlySerializedAs("m_OnSelectExit")]
        XRInteractableEvent m_OnSelectExited = new XRInteractableEvent();

        /// <summary>
        /// (Deprecated) Gets or sets the event that Unity calls when an Interactor ends selecting this Interactable.
        /// </summary>
        /// <remarks><c>onSelectExited</c> has been deprecated. Use <see cref="selectExited"/> with updated signature and check for <c>!</c><see cref="SelectExitEventArgs.isCanceled"/> instead.</remarks>
        [Obsolete("onSelectExited has been deprecated. Use selectExited with updated signature and check for !args.isCanceled instead.")]
        public XRInteractableEvent onSelectExited
        {
            get => m_OnSelectExited;
            set => m_OnSelectExited = value;
        }

        [SerializeField, FormerlySerializedAs("m_OnSelectCancel")]
        XRInteractableEvent m_OnSelectCanceled = new XRInteractableEvent();

        /// <summary>
        /// (Deprecated) Gets or sets the event that Unity calls when this Interactable is selected by an Interactor and either is unregistered
        /// (such as from being disabled or destroyed).
        /// </summary>
        /// <remarks><c>onSelectCanceled</c> has been deprecated. Use <see cref="selectExited"/> with updated signature and check for <see cref="SelectExitEventArgs.isCanceled"/> instead.</remarks>
        [Obsolete("onSelectCanceled has been deprecated. Use selectExited with updated signature and check for args.isCanceled instead.")]
        public XRInteractableEvent onSelectCanceled
        {
            get => m_OnSelectCanceled;
            set => m_OnSelectCanceled = value;
        }

        [SerializeField]
        XRInteractableEvent m_OnActivate = new XRInteractableEvent();

        /// <summary>
        /// (Deprecated) Gets or sets the event that Unity calls when an Interactor activates this selected Interactable.
        /// </summary>
        /// <remarks><c>onActivate</c> has been deprecated. Use <see cref="activated"/> instead.</remarks>
        [Obsolete("onActivate has been deprecated. Use activated with updated signature instead.")]
        public XRInteractableEvent onActivate
        {
            get => m_OnActivate;
            set => m_OnActivate = value;
        }

        [SerializeField]
        XRInteractableEvent m_OnDeactivate = new XRInteractableEvent();

        /// <summary>
        /// (Deprecated) Gets or sets the event that Unity calls when an Interactor deactivates this selected Interactable.
        /// </summary>
        /// <remarks><c>onDeactivate</c> has been deprecated. Use <see cref="deactivated"/> instead.</remarks>
        [Obsolete("onDeactivate has been deprecated. Use deactivated with updated signature instead.")]
        public XRInteractableEvent onDeactivate
        {
            get => m_OnDeactivate;
            set => m_OnDeactivate = value;
        }

        /// <summary>
        /// (Deprecated) Unity calls this only when the first Interactor begins hovering over this Interactable.
        /// </summary>
        /// <remarks><c>onFirstHoverEnter</c> has been deprecated. Use <see cref="onFirstHoverEntered"/> instead.</remarks>
        [Obsolete("onFirstHoverEnter has been deprecated. Use onFirstHoverEntered instead. (UnityUpgradable) -> onFirstHoverEntered")]
        public XRInteractableEvent onFirstHoverEnter => onFirstHoverEntered;

        /// <summary>
        /// (Deprecated) Unity calls this every time an Interactor begins hovering over this Interactable.
        /// </summary>
        /// <remarks><c>onHoverEnter</c> has been deprecated. Use <see cref="onHoverEntered"/> instead.</remarks>
        [Obsolete("onHoverEnter has been deprecated. Use onHoverEntered instead. (UnityUpgradable) -> onHoverEntered")]
        public XRInteractableEvent onHoverEnter => onHoverEntered;

        /// <summary>
        /// (Deprecated) Unity calls this every time an Interactor ends hovering over this Interactable.
        /// </summary>
        /// <remarks><c>onHoverExit</c> has been deprecated. Use <see cref="onHoverExited"/> instead.</remarks>
        [Obsolete("onHoverExit has been deprecated. Use onHoverExited instead. (UnityUpgradable) -> onHoverExited")]
        public XRInteractableEvent onHoverExit => onHoverExited;

        /// <summary>
        /// (Deprecated) Unity calls this only when the last Interactor ends hovering over this Interactable.
        /// </summary>
        /// <remarks><c>onLastHoverExit</c> has been deprecated. Use <see cref="onLastHoverExited"/> instead.</remarks>
        [Obsolete("onLastHoverExit has been deprecated. Use onLastHoverExited instead. (UnityUpgradable) -> onLastHoverExited")]
        public XRInteractableEvent onLastHoverExit => onLastHoverExited;

        /// <summary>
        /// (Deprecated) Unity calls this when an Interactor begins selecting this Interactable.
        /// </summary>
        /// <remarks><c>onSelectEnter</c> has been deprecated. Use <see cref="onSelectEntered"/> instead.</remarks>
        [Obsolete("onSelectEnter has been deprecated. Use onSelectEntered instead. (UnityUpgradable) -> onSelectEntered")]
        public XRInteractableEvent onSelectEnter => onSelectEntered;

        /// <summary>
        /// (Deprecated) Unity calls this when an Interactor ends selecting this Interactable.
        /// </summary>
        /// <remarks><c>onSelectExit</c> has been deprecated. Use <see cref="onSelectExited"/> instead.</remarks>
        [Obsolete("onSelectExit has been deprecated. Use onSelectExited instead. (UnityUpgradable) -> onSelectExited")]
        public XRInteractableEvent onSelectExit => onSelectExited;

        /// <summary>
        /// (Deprecated) Unity calls this when the Interactor selecting this Interactable is disabled or destroyed.
        /// </summary>
        /// <remarks><c>onSelectCancel</c> has been deprecated. Use <see cref="onSelectCanceled"/> instead.</remarks>
        [Obsolete("onSelectCancel has been deprecated. Use onSelectCanceled instead. (UnityUpgradable) -> onSelectCanceled")]
        public XRInteractableEvent onSelectCancel => onSelectCanceled;

        /// <summary>
        /// (Deprecated) The <see cref="XRInteractionManager"/> calls this method
        /// right before the Interactor first initiates hovering over an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="interactor">Interactor that is initiating the hover.</param>
        /// <seealso cref="OnHoverEntered(XRBaseInteractor)"/>
        /// <remarks><c>OnHoverEntering(XRBaseInteractor)</c> has been deprecated. Use <see cref="OnHoverEntering(HoverEnterEventArgs)"/> instead.</remarks>
        [Obsolete("OnHoverEntering(XRBaseInteractor) has been deprecated. Use OnHoverEntering(HoverEnterEventArgs) instead.")]
        protected virtual void OnHoverEntering(XRBaseInteractor interactor)
        {
        }

        /// <summary>
        /// (Deprecated) The <see cref="XRInteractionManager"/> calls this method
        /// when the Interactor first initiates hovering over an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="interactor">Interactor that is initiating the hover.</param>
        /// <seealso cref="OnHoverExited(XRBaseInteractor)"/>
        /// <remarks><c>OnHoverEntered(XRBaseInteractor)</c> has been deprecated. Use <see cref="OnHoverEntered(HoverEnterEventArgs)"/> instead.</remarks>
        [Obsolete("OnHoverEntered(XRBaseInteractor) has been deprecated. Use OnHoverEntered(HoverEnterEventArgs) instead.")]
        protected virtual void OnHoverEntered(XRBaseInteractor interactor)
        {
            if (hoveringInteractors.Count == 1)
                m_OnFirstHoverEntered?.Invoke(interactor);

            m_OnHoverEntered?.Invoke(interactor);
        }

        /// <summary>
        /// (Deprecated) The <see cref="XRInteractionManager"/> calls this method
        /// right before the Interactor ends hovering over an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="interactor">Interactor that is ending the hover.</param>
        /// <seealso cref="OnHoverExited(XRBaseInteractor)"/>
        /// <remarks><c>OnHoverExiting(XRBaseInteractor)</c> has been deprecated. Use <see cref="OnHoverExiting(HoverExitEventArgs)"/> instead.</remarks>
        [Obsolete("OnHoverExiting(XRBaseInteractor) has been deprecated. Use OnHoverExiting(HoverExitEventArgs) instead.")]
        protected virtual void OnHoverExiting(XRBaseInteractor interactor)
        {
        }

        /// <summary>
        /// (Deprecated) The <see cref="XRInteractionManager"/> calls this method
        /// when the Interactor ends hovering over an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="interactor">Interactor that is ending the hover.</param>
        /// <seealso cref="OnHoverEntered(XRBaseInteractor)"/>
        /// <remarks><c>OnHoverExited(XRBaseInteractor)</c> has been deprecated. Use <see cref="OnHoverExited(HoverExitEventArgs)"/> instead.</remarks>
        [Obsolete("OnHoverExited(XRBaseInteractor) has been deprecated. Use OnHoverExited(HoverExitEventArgs) instead.")]
        protected virtual void OnHoverExited(XRBaseInteractor interactor)
        {
            if (hoveringInteractors.Count == 0)
                m_OnLastHoverExited?.Invoke(interactor);

            m_OnHoverExited?.Invoke(interactor);
        }

        /// <summary>
        /// (Deprecated) The <see cref="XRInteractionManager"/> calls this method
        /// right before the Interactor first initiates selection of an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="interactor">Interactor that is initiating the selection.</param>
        /// <seealso cref="OnSelectEntered(XRBaseInteractor)"/>
        /// <remarks><c>OnSelectEntering(XRBaseInteractor)</c> has been deprecated. Use <see cref="OnSelectEntering(SelectEnterEventArgs)"/> instead.</remarks>
        [Obsolete("OnSelectEntering(XRBaseInteractor) has been deprecated. Use OnSelectEntering(SelectEnterEventArgs) instead.")]
        protected virtual void OnSelectEntering(XRBaseInteractor interactor)
        {
        }

        /// <summary>
        /// (Deprecated) The <see cref="XRInteractionManager"/> calls this method
        /// when the Interactor first initiates selection of an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="interactor">Interactor that is initiating the selection.</param>
        /// <seealso cref="OnSelectExited(XRBaseInteractor)"/>
        /// <seealso cref="OnSelectCanceled"/>
        /// <remarks><c>OnSelectEntered(XRBaseInteractor)</c> has been deprecated. Use <see cref="OnSelectEntered(SelectEnterEventArgs)"/> instead.</remarks>
        [Obsolete("OnSelectEntered(XRBaseInteractor) has been deprecated. Use OnSelectEntered(SelectEnterEventArgs) instead.")]
        protected virtual void OnSelectEntered(XRBaseInteractor interactor)
        {
            m_OnSelectEntered?.Invoke(interactor);
        }

        /// <summary>
        /// (Deprecated) The <see cref="XRInteractionManager"/> calls this method
        /// right before the Interactor ends selection of an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="interactor">Interactor that is ending the selection.</param>
        /// <seealso cref="OnSelectExited(XRBaseInteractor)"/>
        /// <remarks><c>OnSelectExiting(XRBaseInteractor)</c> has been deprecated. Use <see cref="OnSelectExiting(SelectExitEventArgs)"/> instead.</remarks>
        [Obsolete("OnSelectExiting(XRBaseInteractor) has been deprecated. Use OnSelectExiting(SelectExitEventArgs) and check for !args.isCanceled instead.")]
        protected virtual void OnSelectExiting(XRBaseInteractor interactor)
        {
        }

        /// <summary>
        /// (Deprecated) The <see cref="XRInteractionManager"/> calls this method
        /// when the Interactor ends selection of an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="interactor">Interactor that is ending the selection.</param>
        /// <seealso cref="OnSelectEntered(XRBaseInteractor)"/>
        /// <seealso cref="OnSelectCanceled"/>
        /// <remarks><c>OnSelectExited(XRBaseInteractor)</c> has been deprecated. Use <see cref="OnSelectExited(SelectExitEventArgs)"/> instead.</remarks>
        [Obsolete("OnSelectExited(XRBaseInteractor) has been deprecated. Use OnSelectExited(SelectExitEventArgs) and check for !args.isCanceled instead.")]
        protected virtual void OnSelectExited(XRBaseInteractor interactor)
        {
            m_OnSelectExited?.Invoke(interactor);
        }

        /// <summary>
        /// (Deprecated) The <see cref="XRInteractionManager"/> calls this method
        /// while this Interactable is selected by an Interactor
        /// right before either is unregistered (such as from being disabled or destroyed)
        /// in a first pass.
        /// </summary>
        /// <param name="interactor">Interactor that is ending the selection.</param>
        /// <seealso cref="OnSelectEntered(XRBaseInteractor)"/>
        /// <seealso cref="OnSelectExited(XRBaseInteractor)"/>
        /// <seealso cref="OnSelectCanceled"/>
        /// <remarks><c>OnSelectCanceling(XRBaseInteractor)</c> has been deprecated. Use <see cref="OnSelectExiting(SelectExitEventArgs)"/> instead.</remarks>
        [Obsolete("OnSelectCanceling(XRBaseInteractor) has been deprecated. Use OnSelectExiting(SelectExitEventArgs) and check for args.isCanceled instead.")]
        protected virtual void OnSelectCanceling(XRBaseInteractor interactor)
        {
        }

        /// <summary>
        /// (Deprecated) The <see cref="XRInteractionManager"/> calls this method
        /// while an Interactor selects this Interactable when either
        /// is unregistered (such as from being disabled or destroyed)
        /// in a second pass.
        /// </summary>
        /// <param name="interactor">Interactor that is ending the selection.</param>
        /// <seealso cref="OnSelectEntered(XRBaseInteractor)"/>
        /// <seealso cref="OnSelectExited(XRBaseInteractor)"/>
        /// <seealso cref="OnSelectCanceling"/>
        /// <remarks><c>OnSelectCanceled(XRBaseInteractor)</c> has been deprecated. Use <see cref="OnSelectExited(SelectExitEventArgs)"/> instead.</remarks>
        [Obsolete("OnSelectCanceled(XRBaseInteractor) has been deprecated. Use OnSelectExited(SelectExitEventArgs) and check for args.isCanceled instead.")]
        protected virtual void OnSelectCanceled(XRBaseInteractor interactor)
        {
            m_OnSelectCanceled?.Invoke(interactor);
        }

        /// <summary>
        /// (Deprecated) The <see cref="XRBaseControllerInteractor"/> calls this method
        /// when the Interactor begins an activation event on this Interactable.
        /// </summary>
        /// <param name="interactor">Interactor that is sending the activate event.</param>
        /// <seealso cref="OnDeactivate"/>
        /// <remarks><c>OnActivate(XRBaseInteractor)</c> has been deprecated. Use <see cref="OnActivated(ActivateEventArgs)"/> instead.</remarks>
        [Obsolete("OnActivate(XRBaseInteractor) has been deprecated. Use OnActivated(ActivateEventArgs) instead.")]
        protected virtual void OnActivate(XRBaseInteractor interactor)
        {
            m_OnActivate?.Invoke(interactor);
        }

        /// <summary>
        /// (Deprecated) The <see cref="XRBaseControllerInteractor"/> calls this method
        /// when the Interactor ends an activation event on this Interactable.
        /// </summary>
        /// <param name="interactor">Interactor that is sending the deactivate event.</param>
        /// <seealso cref="OnActivate"/>
        /// <remarks><c>OnDeactivate(XRBaseInteractor)</c> has been deprecated. Use <see cref="OnDeactivated(DeactivateEventArgs)"/> instead.</remarks>
        [Obsolete("OnDeactivate(XRBaseInteractor) has been deprecated. Use OnDeactivated(DeactivateEventArgs) instead.")]
        protected virtual void OnDeactivate(XRBaseInteractor interactor)
        {
            m_OnDeactivate?.Invoke(interactor);
        }

        /// <summary>
        /// (Deprecated) Calculates distance squared to interactor (based on colliders).
        /// </summary>
        /// <param name="interactor">Interactor to calculate distance against.</param>
        /// <returns>Returns the minimum distance between the interactor and this interactable's colliders.</returns>
        /// <remarks><c>GetDistanceSqrToInteractor(XRBaseInteractor)</c> has been deprecated. Use <see cref="GetDistanceSqrToInteractor(IXRInteractor)"/> instead.</remarks>
        [Obsolete("GetDistanceSqrToInteractor(XRBaseInteractor) has been deprecated. Use GetDistanceSqrToInteractor(IXRInteractor) instead.")]
        public virtual float GetDistanceSqrToInteractor(XRBaseInteractor interactor) => GetDistanceSqrToInteractor(interactor as IXRInteractor);

        /// <summary>
        /// (Deprecated) Attaches the custom reticle to the Interactor.
        /// </summary>
        /// <param name="interactor">Interactor that is interacting with this Interactable.</param>
        /// <remarks><c>AttachCustomReticle(XRBaseInteractor)</c> has been deprecated. Use <see cref="AttachCustomReticle(IXRInteractor)"/> instead.</remarks>
        [Obsolete("AttachCustomReticle(XRBaseInteractor) has been deprecated. Use AttachCustomReticle(IXRInteractor) instead.")]
        public virtual void AttachCustomReticle(XRBaseInteractor interactor) => AttachCustomReticle(interactor as IXRInteractor);

        /// <summary>
        /// (Deprecated) Removes the custom reticle from the Interactor.
        /// </summary>
        /// <param name="interactor">Interactor that is no longer interacting with this Interactable.</param>
        /// <remarks><c>RemoveCustomReticle(XRBaseInteractor)</c> has been deprecated. Use <see cref="RemoveCustomReticle(IXRInteractor)"/> instead.</remarks>
        [Obsolete("RemoveCustomReticle(XRBaseInteractor) has been deprecated. Use RemoveCustomReticle(IXRInteractor) instead.")]
        public virtual void RemoveCustomReticle(XRBaseInteractor interactor) => RemoveCustomReticle(interactor as IXRInteractor);
#pragma warning restore 618

        /// <summary>
        /// (Deprecated) (Read Only) The list of interactors that are hovering on this interactable.
        /// </summary>
        /// <seealso cref="isHovered"/>
        /// <seealso cref="XRBaseInteractor.hoverTargets"/>
        [Obsolete("hoveringInteractors has been deprecated. Use interactorsHovering instead.")]
        public List<XRBaseInteractor> hoveringInteractors { get; } = new List<XRBaseInteractor>();

        /// <summary>
        /// (Deprecated) The Interactor selecting this Interactable (may be <see langword="null"/>).
        /// </summary>
        /// <remarks>
        /// Unity automatically sets this value during <see cref="OnSelectEntering(SelectEnterEventArgs)"/>
        /// and <see cref="OnSelectExiting(SelectExitEventArgs)"/> and should not typically need to be set
        /// by a user. The setter is <see langword="protected"/> to allow for rare scenarios where a derived
        /// class needs to control this value. Changing this value does not invoke select events.
        /// <br />
        /// <c>selectingInteractor</c> has been deprecated. Use <see cref="interactorsSelecting"/> or <see cref="isSelected"/> instead.
        /// </remarks>
        /// <seealso cref="isSelected"/>
        /// <seealso cref="XRBaseInteractor.selectTarget"/>
        [Obsolete("selectingInteractor has been deprecated. Use interactorsSelecting, GetOldestInteractorSelecting, or isSelected for similar functionality.")]
        public XRBaseInteractor selectingInteractor
        {
            get => isSelected ? interactorsSelecting[0] as XRBaseInteractor : null;
            protected set
            {
                if (isSelected)
                    interactorsSelecting[0] = value;
                else
                    interactorsSelecting.Add(value);
            }
        }

        /// <summary>
        /// (Deprecated) Determines if this interactable can be hovered by a given interactor.
        /// </summary>
        /// <param name="interactor">Interactor to check for a valid hover state with.</param>
        /// <returns>Returns <see langword="true"/> if hovering is valid this frame. Returns <see langword="false"/> if not.</returns>
        /// <seealso cref="XRBaseInteractor.CanHover(XRBaseInteractable)"/>
        /// <remarks>
        /// <c>IsHoverableBy(XRBaseInteractor)</c> has been deprecated. Use <see cref="IsHoverableBy(IXRHoverInteractor)"/> instead.
        /// </remarks>
        [Obsolete("IsHoverableBy(XRBaseInteractor) has been deprecated. Use IsHoverableBy(IXRHoverInteractor) instead.")]
        public virtual bool IsHoverableBy(XRBaseInteractor interactor) => IsHoverableBy((IXRHoverInteractor)interactor);

        /// <summary>
        /// (Deprecated) Determines if a given Interactor can select this Interactable.
        /// </summary>
        /// <param name="interactor">Interactor to check for a valid selection with.</param>
        /// <returns>Returns <see langword="true"/> if selection is valid this frame. Returns <see langword="false"/> if not.</returns>
        /// <seealso cref="XRBaseInteractor.CanSelect(XRBaseInteractable)"/>
        /// <remarks>
        /// <c>IsSelectableBy(XRBaseInteractor)</c> has been deprecated. Use <see cref="IsSelectableBy(IXRSelectInteractor)"/> instead.
        /// </remarks>
        [Obsolete("IsSelectableBy(XRBaseInteractor) has been deprecated. Use IsSelectableBy(IXRSelectInteractor) instead.")]
        public virtual bool IsSelectableBy(XRBaseInteractor interactor) => IsSelectableBy((IXRSelectInteractor)interactor);
    }
}
