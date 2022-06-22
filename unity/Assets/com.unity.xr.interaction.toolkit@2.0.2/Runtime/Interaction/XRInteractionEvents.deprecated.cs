using System;
using UnityEngine.Events;

namespace UnityEngine.XR.Interaction.Toolkit
{
    /// <summary>
    /// <see cref="UnityEvent"/> that responds to changes of hover, selection, and activation by this Interactable.
    /// </summary>
    [Serializable, Obsolete("XRInteractableEvent has been deprecated. Use events specific to each state change instead.")]
    public class XRInteractableEvent : UnityEvent<XRBaseInteractor>
    {
    }

    /// <summary>
    /// <see cref="UnityEvent"/> that responds to changes of hover and selection by this Interactor.
    /// </summary>
    [Serializable, Obsolete("XRInteractorEvent has been deprecated. Use events specific to each state change instead.")]
    public class XRInteractorEvent : UnityEvent<XRBaseInteractable>
    {
    }

    public abstract partial class BaseInteractionEventArgs
    {
        /// <summary>
        /// (Deprecated) The Interactor associated with the interaction event.
        /// </summary>
        /// <remarks>
        /// <c>interactor</c> has been deprecated. Use <see cref="interactorObject"/> instead.
        /// </remarks>
        [Obsolete("interactor has been deprecated. Use interactorObject instead.")]
        public XRBaseInteractor interactor
        {
            get => interactorObject as XRBaseInteractor;
            set => interactorObject = value;
        }

        /// <summary>
        /// (Deprecated) The Interactable associated with the interaction event.
        /// </summary>
        /// <remarks>
        /// <c>interactable</c> has been deprecated. Use <see cref="interactableObject"/> instead.
        /// </remarks>
        [Obsolete("interactable has been deprecated. Use interactableObject instead.")]
        public XRBaseInteractable interactable
        {
            get => interactableObject as XRBaseInteractable;
            set => interactableObject = value;
        }
    }

    #region Registration

    public partial class InteractorRegisteredEventArgs
    {
        /// <summary>
        /// (Deprecated) The Interactor that was registered.
        /// </summary>
        /// <remarks>
        /// <c>interactor</c> has been deprecated. Use <see cref="interactorObject"/> instead.
        /// </remarks>
        [Obsolete("interactor has been deprecated. Use interactorObject instead.")]
        public XRBaseInteractor interactor
        {
            get => interactorObject as XRBaseInteractor;
            set => interactorObject = value;
        }
    }

    public partial class InteractableRegisteredEventArgs
    {
        /// <summary>
        /// (Deprecated) The Interactable that was registered.
        /// </summary>
        /// <remarks>
        /// <c>interactable</c> has been deprecated. Use <see cref="interactableObject"/> instead.
        /// </remarks>
        [Obsolete("interactable has been deprecated. Use interactableObject instead.")]
        public XRBaseInteractable interactable
        {
            get => interactableObject as XRBaseInteractable;
            set => interactableObject = value;
        }
    }

    public partial class InteractorUnregisteredEventArgs
    {
        /// <summary>
        /// (Deprecated) The Interactor that was unregistered.
        /// </summary>
        /// <remarks>
        /// <c>interactor</c> has been deprecated. Use <see cref="interactorObject"/> instead.
        /// </remarks>
        [Obsolete("interactor has been deprecated. Use interactorObject instead.")]
        public XRBaseInteractor interactor
        {
            get => interactorObject as XRBaseInteractor;
            set => interactorObject = value;
        }
    }

    public partial class InteractableUnregisteredEventArgs
    {
        /// <summary>
        /// (Deprecated) The Interactable that was unregistered.
        /// </summary>
        /// <remarks>
        /// <c>interactable</c> has been deprecated. Use <see cref="interactableObject"/> instead.
        /// </remarks>
        [Obsolete("interactable has been deprecated. Use interactableObject instead.")]
        public XRBaseInteractable interactable
        {
            get => interactableObject as XRBaseInteractable;
            set => interactableObject = value;
        }
    }

    #endregion
}