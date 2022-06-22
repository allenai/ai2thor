using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace UnityEngine.XR.Interaction.Toolkit
{
    public abstract partial class XRBaseInteractor
    {
#pragma warning disable 618
        /// <summary>
        /// (Deprecated) Allows interaction with Interactables whose Interaction Layer Mask overlaps with any Layer in this Interaction Layer Mask.
        /// </summary>
        /// <seealso cref="XRBaseInteractable.interactionLayerMask"/>
        /// <remarks><c>interactionLayerMask</c> has been deprecated. Use <see cref="interactionLayers"/> instead.</remarks>
        [Obsolete("interactionLayerMask has been deprecated. Use interactionLayers instead.")]
        public LayerMask interactionLayerMask
        {
            get => m_InteractionLayerMask;
            set => m_InteractionLayerMask = value;
        }

        /// <summary>
        /// (Deprecated) Defines whether interactions are enabled or not.
        /// </summary>
        /// <remarks>
        /// <example>
        /// <c>enableInteractions = value;</c> is a convenience property for:
        /// <code>
        /// allowHover = value;
        /// allowSelect = value;
        /// </code>
        /// </example>
        /// <c>enableInteractions</c> has been deprecated. Use <see cref="allowHover"/> and <see cref="allowSelect"/> instead.
        /// </remarks>
        [Obsolete("enableInteractions has been deprecated. Use allowHover and allowSelect instead.")]
        public bool enableInteractions
        {
            get => m_AllowHover && m_AllowSelect;
            set
            {
                m_AllowHover = value;
                m_AllowSelect = value;
            }
        }

        [SerializeField, FormerlySerializedAs("m_OnHoverEnter")]
        XRInteractorEvent m_OnHoverEntered = new XRInteractorEvent();
        /// <summary>
        /// (Deprecated) Gets or sets the event that Unity calls when this Interactor begins hovering over an Interactable.
        /// </summary>
        /// <remarks><c>onHoverEntered</c> has been deprecated. Use <see cref="hoverEntered"/> with updated signature instead.</remarks>
        [Obsolete("onHoverEntered has been deprecated. Use hoverEntered with updated signature instead.")]
        public XRInteractorEvent onHoverEntered
        {
            get => m_OnHoverEntered;
            set => m_OnHoverEntered = value;
        }

        [SerializeField, FormerlySerializedAs("m_OnHoverExit")]
        XRInteractorEvent m_OnHoverExited = new XRInteractorEvent();
        /// <summary>
        /// (Deprecated) Gets or sets the event that Unity calls when this Interactor ends hovering over an Interactable.
        /// </summary>
        /// <remarks><c>onHoverExited</c> has been deprecated. Use <see cref="hoverExited"/> with updated signature instead.</remarks>
        [Obsolete("onHoverExited has been deprecated. Use hoverExited with updated signature instead.")]
        public XRInteractorEvent onHoverExited
        {
            get => m_OnHoverExited;
            set => m_OnHoverExited = value;
        }

        [SerializeField, FormerlySerializedAs("m_OnSelectEnter")]
        XRInteractorEvent m_OnSelectEntered = new XRInteractorEvent();
        /// <summary>
        /// (Deprecated) Gets or sets the event that Unity calls when this Interactor begins selecting an Interactable.
        /// </summary>
        /// <remarks><c>onSelectEntered</c> has been deprecated. Use <see cref="selectEntered"/> with updated signature instead.</remarks>
        [Obsolete("onSelectEntered has been deprecated. Use selectEntered with updated signature instead.")]
        public XRInteractorEvent onSelectEntered
        {
            get => m_OnSelectEntered;
            set => m_OnSelectEntered = value;
        }

        [SerializeField, FormerlySerializedAs("m_OnSelectExit")]
        XRInteractorEvent m_OnSelectExited = new XRInteractorEvent();
        /// <summary>
        /// (Deprecated) Gets or sets the event that Unity calls when this Interactor ends selecting an Interactable.
        /// </summary>
        /// <remarks><c>onSelectExited</c> has been deprecated. Use <see cref="selectExited"/> with updated signature instead.</remarks>
        [Obsolete("onSelectExited has been deprecated. Use selectExited with updated signature instead.")]
        public XRInteractorEvent onSelectExited
        {
            get => m_OnSelectExited;
            set => m_OnSelectExited = value;
        }

        /// <summary>
        /// (Deprecated) Gets or sets the event that Unity calls when this Interactor begins hovering over an Interactable.
        /// </summary>
        /// <remarks><c>onHoverEnter</c> has been deprecated. Use <see cref="onHoverEntered"/> instead.</remarks>
        [Obsolete("onHoverEnter has been deprecated. Use onHoverEntered instead. (UnityUpgradable) -> onHoverEntered")]
        public XRInteractorEvent onHoverEnter => onHoverEntered;

        /// <summary>
        /// (Deprecated) Gets or sets the event that Unity calls when this Interactor ends hovering over an Interactable.
        /// </summary>
        /// <remarks><c>onHoverExit</c> has been deprecated. Use <see cref="onHoverExited"/> instead.</remarks>
        [Obsolete("onHoverExit has been deprecated. Use onHoverExited instead. (UnityUpgradable) -> onHoverExited")]
        public XRInteractorEvent onHoverExit => onHoverExited;

        /// <summary>
        /// (Deprecated) Gets or sets the event that Unity calls when this Interactor begins selecting an Interactable.
        /// </summary>
        /// <remarks><c>onSelectEnter</c> has been deprecated. Use <see cref="onSelectEntered"/> instead.</remarks>
        [Obsolete("onSelectEnter has been deprecated. Use onSelectEntered instead. (UnityUpgradable) -> onSelectEntered")]
        public XRInteractorEvent onSelectEnter => onSelectEntered;

        /// <summary>
        /// (Deprecated) Gets or sets the event that Unity calls when this Interactor ends selecting an Interactable.
        /// </summary>
        /// <remarks><c>onSelectExit></c> has been deprecated. Use <see cref="onSelectExited"/> instead.</remarks>
        [Obsolete("onSelectExit has been deprecated. Use onSelectExited instead. (UnityUpgradable) -> onSelectExited")]
        public XRInteractorEvent onSelectExit => onSelectExited;

        /// <summary>
        /// (Deprecated) The <see cref="XRInteractionManager"/> calls this method
        /// right before the Interactor first initiates hovering over an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="interactable">Interactable that is being hovered over.</param>
        /// <seealso cref="OnHoverEntered(XRBaseInteractable)"/>
        /// <remarks><c>OnHoverEntering(XRBaseInteractable)</c> has been deprecated. Use <see cref="OnHoverEntering(HoverEnterEventArgs)"/> instead.</remarks>
        [Obsolete("OnHoverEntering(XRBaseInteractable) has been deprecated. Use OnHoverEntering(HoverEnterEventArgs) instead.")]
        protected virtual void OnHoverEntering(XRBaseInteractable interactable)
        {
        }

        /// <summary>
        /// (Deprecated) The <see cref="XRInteractionManager"/> calls this method
        /// when the Interactor first initiates hovering over an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="interactable">Interactable that is being hovered over.</param>
        /// <seealso cref="OnHoverExited(XRBaseInteractable)"/>
        /// <remarks><c>OnHoverEntered(XRBaseInteractable)</c> has been deprecated. Use <see cref="OnHoverEntered(HoverEnterEventArgs)"/> instead.</remarks>
        [Obsolete("OnHoverEntered(XRBaseInteractable) has been deprecated. Use OnHoverEntered(HoverEnterEventArgs) instead.")]
        protected virtual void OnHoverEntered(XRBaseInteractable interactable)
        {
            m_OnHoverEntered?.Invoke(interactable);
        }

        /// <summary>
        /// (Deprecated) The <see cref="XRInteractionManager"/> calls this method
        /// right before the Interactor ends hovering over an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="interactable">Interactable that is no longer hovered over.</param>
        /// <seealso cref="OnHoverExited(XRBaseInteractable)"/>
        /// <remarks><c>OnHoverExiting(XRBaseInteractable)</c> has been deprecated. Use <see cref="OnHoverExiting(HoverExitEventArgs)"/> instead.</remarks>
        [Obsolete("OnHoverExiting(XRBaseInteractable) has been deprecated. Use OnHoverExiting(HoverExitEventArgs) instead.")]
        protected virtual void OnHoverExiting(XRBaseInteractable interactable)
        {
        }

        /// <summary>
        /// (Deprecated) The <see cref="XRInteractionManager"/> calls this method
        /// when the Interactor ends hovering over an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="interactable">Interactable that is no longer hovered over.</param>
        /// <seealso cref="OnHoverEntered(XRBaseInteractable)"/>
        /// <remarks><c>OnHoverExited(XRBaseInteractable)</c> has been deprecated. Use <see cref="OnHoverExited(HoverExitEventArgs)"/> instead.</remarks>
        [Obsolete("OnHoverExited(XRBaseInteractable) has been deprecated. Use OnHoverExited(HoverExitEventArgs) instead.")]
        protected virtual void OnHoverExited(XRBaseInteractable interactable)
        {
            m_OnHoverExited?.Invoke(interactable);
        }

        /// <summary>
        /// (Deprecated) The <see cref="XRInteractionManager"/> calls this method
        /// right before the Interactor first initiates selection of an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="interactable">Interactable that is being selected.</param>
        /// <seealso cref="OnSelectEntered(XRBaseInteractable)"/>
        /// <remarks><c>OnSelectEntering(XRBaseInteractable)</c> has been deprecated. Use <see cref="OnSelectEntering(SelectEnterEventArgs)"/> instead.</remarks>
        [Obsolete("OnSelectEntering(XRBaseInteractable) has been deprecated. Use OnSelectEntering(SelectEnterEventArgs) instead.")]
        protected virtual void OnSelectEntering(XRBaseInteractable interactable)
        {
        }

        /// <summary>
        /// (Deprecated) The <see cref="XRInteractionManager"/> calls this method
        /// when the Interactor first initiates selection of an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="interactable">Interactable that is being selected.</param>
        /// <seealso cref="OnSelectExited(XRBaseInteractable)"/>
        /// <remarks><c>OnSelectEntered(XRBaseInteractable)</c> has been deprecated. Use <see cref="OnSelectEntered(SelectEnterEventArgs)"/> instead.</remarks>
        [Obsolete("OnSelectEntered(XRBaseInteractable) has been deprecated. Use OnSelectEntered(SelectEnterEventArgs) instead.")]
        protected virtual void OnSelectEntered(XRBaseInteractable interactable)
        {
            m_OnSelectEntered?.Invoke(interactable);
        }

        /// <summary>
        /// (Deprecated) The <see cref="XRInteractionManager"/> calls this method
        /// right before the Interactor ends selection of an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="interactable">Interactable that is no longer selected.</param>
        /// <seealso cref="OnSelectExited(XRBaseInteractable)"/>
        /// <remarks><c>OnSelectExiting(XRBaseInteractable)</c> has been deprecated. Use <see cref="OnSelectExiting(SelectExitEventArgs)"/> instead.</remarks>
        [Obsolete("OnSelectExiting(XRBaseInteractable) has been deprecated. Use OnSelectExiting(SelectExitEventArgs) instead.")]
        protected virtual void OnSelectExiting(XRBaseInteractable interactable)
        {
        }

        /// <summary>
        /// (Deprecated) The <see cref="XRInteractionManager"/> calls this method
        /// when the Interactor ends selection of an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="interactable">Interactable that is no longer selected.</param>
        /// <seealso cref="OnSelectEntered(XRBaseInteractable)"/>
        /// <remarks><c>OnSelectExited(XRBaseInteractable)</c> has been deprecated. Use <see cref="OnSelectExited(SelectExitEventArgs)"/> instead.</remarks>
        [Obsolete("OnSelectExited(XRBaseInteractable) has been deprecated. Use OnSelectExited(SelectExitEventArgs) instead.")]
        protected virtual void OnSelectExited(XRBaseInteractable interactable)
        {
            m_OnSelectExited?.Invoke(interactable);
        }
#pragma warning restore 618

        /// <summary>
        /// Selected Interactable for this Interactor (may be <see langword="null"/>).
        /// </summary>
        /// <seealso cref="XRBaseInteractable.selectingInteractor"/>
        /// <remarks>
        /// <c>selectTarget</c> has been deprecated. Use <see cref="interactablesSelected"/>, <see cref="XRSelectInteractorExtensions.GetOldestInteractableSelected(IXRSelectInteractor)"/>, <see cref="hasSelection"/>, or <see cref="XRBaseInteractor.IsSelecting(IXRSelectInteractable)"/> instead.
        /// </remarks>
        [Obsolete("selectTarget has been deprecated. Use interactablesSelected, GetOldestInteractableSelected, hasSelection, or IsSelecting for similar functionality.")]
        public XRBaseInteractable selectTarget
        {
            get => hasSelection ? interactablesSelected[0] as XRBaseInteractable : null;
            protected set => interactablesSelected[0] = value;
        }

        /// <summary>
        /// Target Interactables that are currently being hovered over (may by empty).
        /// </summary>
        /// <seealso cref="XRBaseInteractable.hoveringInteractors"/>
        /// <remarks>
        /// <c>hoverTargets</c> has been deprecated. Use <see cref="interactablesHovered"/> instead.
        /// </remarks>
        [Obsolete("hoverTargets has been deprecated. Use interactablesHovered instead.")]
        protected List<XRBaseInteractable> hoverTargets { get; } = new List<XRBaseInteractable>();

        /// <summary>
        /// Retrieves a copy of the list of target Interactables that are currently being hovered over.
        /// </summary>
        /// <param name="targets">The results list to store hover targets into.</param>
        /// <remarks>
        /// Clears <paramref name="targets"/> before adding to it.
        /// <br />
        /// <c>GetHoverTargets</c> has been deprecated. Use <see cref="interactablesHovered"/> instead.
        /// </remarks>
        [Obsolete("GetHoverTargets has been deprecated. Use interactablesHovered instead.")]
        public void GetHoverTargets(List<XRBaseInteractable> targets)
        {
            targets.Clear();
            targets.AddRange(hoverTargets);
        }

        /// <summary>
        /// Retrieve the list of Interactables that this Interactor could possibly interact with this frame.
        /// This list is sorted by priority (with highest priority first).
        /// </summary>
        /// <param name="targets">The results list to populate with Interactables that are valid for selection or hover.</param>
        /// <remarks>
        /// <c>GetValidTargets(List&lt;XRBaseInteractable&gt;)</c> has been deprecated. Use <see cref="GetValidTargets(List{IXRInteractable})"/> instead.
        /// <see cref="XRInteractionManager.GetValidTargets(IXRInteractor, List{IXRInteractable})"/> will stitch the results together with <c>GetValidTargets(List&lt;IXRInteractable&gt;)</c>,
        /// but by default this method now does nothing.
        /// </remarks>
        [Obsolete("GetValidTargets(List<XRBaseInteractable>) has been deprecated. Override GetValidTargets(List<IXRInteractable>) instead." +
            " XRInteractionManager.GetValidTargets will stitch the results together with GetValidTargets(List<IXRInteractable>), but by default" +
            " this method now does nothing.")]
        public virtual void GetValidTargets(List<XRBaseInteractable> targets)
        {
        }

        /// <summary>
        /// Determines if the Interactable is valid for hover this frame.
        /// </summary>
        /// <param name="interactable">Interactable to check.</param>
        /// <returns>Returns <see langword="true"/> if the interactable can be hovered over this frame.</returns>
        /// <seealso cref="XRBaseInteractable.IsHoverableBy(XRBaseInteractor)"/>
        /// <remarks>
        /// <c>CanHover(XRBaseInteractable)</c> has been deprecated. Use <see cref="CanHover(IXRHoverInteractable)"/> instead.
        /// </remarks>
        [Obsolete("CanHover(XRBaseInteractable) has been deprecated. Use CanHover(IXRHoverInteractable) instead.")]
        public virtual bool CanHover(XRBaseInteractable interactable) => CanHover((IXRHoverInteractable)interactable);

        /// <summary>
        /// Determines if the Interactable is valid for selection this frame.
        /// </summary>
        /// <param name="interactable">Interactable to check.</param>
        /// <returns>Returns <see langword="true"/> if the Interactable can be selected this frame.</returns>
        /// <seealso cref="XRBaseInteractable.IsSelectableBy(XRBaseInteractor)"/>
        /// <remarks>
        /// <c>CanSelect(XRBaseInteractable)</c> has been deprecated. Use <see cref="CanSelect(IXRSelectInteractable)"/> instead.
        /// </remarks>
        [Obsolete("CanSelect(XRBaseInteractable) has been deprecated. Use CanSelect(IXRSelectInteractable) instead.")]
        public virtual bool CanSelect(XRBaseInteractable interactable) => CanSelect((IXRSelectInteractable)interactable);

        /// <summary>
        /// (Deprecated) (Read Only) Indicates whether this Interactor requires that an Interactable is not currently selected to begin selecting it.
        /// </summary>
        /// <remarks>
        /// When <see langword="true"/>, the Interaction Manager will only begin a selection when the Interactable is not currently selected.
        /// </remarks>
        /// <remarks>
        /// <c>requireSelectExclusive</c> has been deprecated. Put logic in <see cref="CanSelect(IXRSelectInteractable)"/> instead.
        /// </remarks>
        [Obsolete("requireSelectExclusive has been deprecated. Put logic in CanSelect instead.")]
        public virtual bool requireSelectExclusive => false;

        /// <summary>
        /// Manually initiate selection of an Interactable.
        /// </summary>
        /// <param name="interactable">Interactable that is being selected.</param>
        /// <seealso cref="EndManualInteraction"/>
        /// <remarks>
        /// <c>StartManualInteraction(XRBaseInteractable)</c> has been deprecated. Use <see cref="StartManualInteraction(IXRSelectInteractable)"/> instead.
        /// </remarks>
        [Obsolete("StartManualInteraction(XRBaseInteractable) has been deprecated. Use StartManualInteraction(IXRSelectInteractable) instead.")]
        public virtual void StartManualInteraction(XRBaseInteractable interactable) => StartManualInteraction((IXRSelectInteractable)interactable);
    }
}
