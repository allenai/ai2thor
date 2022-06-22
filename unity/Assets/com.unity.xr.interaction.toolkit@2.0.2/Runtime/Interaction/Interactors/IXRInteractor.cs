using System;
using System.Collections.Generic;

namespace UnityEngine.XR.Interaction.Toolkit
{
    /// <summary>
    /// An interface that represents an Interactor component that controls how a GameObject
    /// interacts with an Interactable component. An example is a Ray Interactor which
    /// uses ray casting to find valid Interactable objects to manipulate.
    /// </summary>
    /// <remarks>
    /// When scripting, you can typically write custom behaviors that derive from <see cref="XRBaseInteractor"/>
    /// or one of its derived classes rather than implementing this interface directly.
    /// </remarks>
    /// <seealso cref="XRBaseInteractor"/>
    /// <seealso cref="IXRActivateInteractor"/>
    /// <seealso cref="IXRHoverInteractor"/>
    /// <seealso cref="IXRSelectInteractor"/>
    /// <seealso cref="IXRInteractable"/>
    public interface IXRInteractor
    {
        /// <summary>
        /// Calls the methods in its invocation list when this Interactor is registered with an Interaction Manager.
        /// </summary>
        /// <remarks>
        /// The <see cref="InteractorRegisteredEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="XRInteractionManager.interactorRegistered"/>
        event Action<InteractorRegisteredEventArgs> registered;

        /// <summary>
        /// Calls the methods in its invocation list when this Interactor is unregistered from an Interaction Manager.
        /// </summary>
        /// <remarks>
        /// The <see cref="InteractorUnregisteredEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="XRInteractionManager.interactorUnregistered"/>
        event Action<InteractorUnregisteredEventArgs> unregistered;

        /// <summary>
        /// (Read Only) Allows interaction with Interactables whose Interaction Layer Mask overlaps with any Layer in this Interaction Layer Mask.
        /// </summary>
        /// <seealso cref="IXRInteractable.interactionLayers"/>
        InteractionLayerMask interactionLayers { get; }

        /// <summary>
        /// (Read Only) The <see cref="Transform"/> associated with the Interactor.
        /// </summary>
        /// <remarks>
        /// When this Interactor is a component, this property is the Transform of the GameObject the component is attached to.
        /// </remarks>
        Transform transform { get; }

        /// <summary>
        /// Gets the <see cref="Transform"/> that is used as the attachment point for a given Interactable.
        /// </summary>
        /// <param name="interactable">The specific Interactable as context to get the attachment point for.</param>
        /// <returns>Returns the attachment point <see cref="Transform"/>.</returns>
        /// <seealso cref="IXRInteractable.GetAttachTransform"/>
        /// <remarks>
        /// This should typically return the Transform of a child GameObject or the <see cref="transform"/> itself.
        /// </remarks>
        Transform GetAttachTransform(IXRInteractable interactable);

        /// <summary>
        /// Retrieve the list of Interactables that this Interactor could possibly interact with this frame.
        /// This list is sorted by priority (with highest priority first).
        /// </summary>
        /// <param name="targets">The results list to populate with Interactables that are valid for selection or hover.</param>
        /// <remarks>
        /// When implementing this method, Unity expects you to clear <paramref name="targets"/> before adding to it.
        /// </remarks>
        void GetValidTargets(List<IXRInteractable> targets);

        /// <summary>
        /// The <see cref="XRInteractionManager"/> calls this method
        /// when this Interactor is registered with it.
        /// </summary>
        /// <param name="args">Event data containing the Interaction Manager that registered this Interactor.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="XRInteractionManager.RegisterInteractor(IXRInteractor)"/>
        void OnRegistered(InteractorRegisteredEventArgs args);

        /// <summary>
        /// The <see cref="XRInteractionManager"/> calls this method
        /// when this Interactor is unregistered from it.
        /// </summary>
        /// <param name="args">Event data containing the Interaction Manager that unregistered this Interactor.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="XRInteractionManager.UnregisterInteractor(IXRInteractor)"/>
        void OnUnregistered(InteractorUnregisteredEventArgs args);

        /// <summary>
        /// The <see cref="XRInteractionManager"/> calls this method to update the Interactor
        /// before interaction events occur. Interactors should use this method to
        /// do tasks like determine their valid targets.
        /// </summary>
        /// <param name="updatePhase">The update phase this is called during.</param>
        /// <remarks>
        /// Please see the <see cref="XRInteractionManager"/> and <see cref="XRInteractionUpdateOrder.UpdatePhase"/> documentation for more
        /// details on update order.
        /// </remarks>
        /// <seealso cref="XRInteractionUpdateOrder.UpdatePhase"/>
        void PreprocessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase);

        /// <summary>
        /// The <see cref="XRInteractionManager"/> calls this method to update the Interactor
        /// after interaction events occur.
        /// </summary>
        /// <param name="updatePhase">The update phase this is called during.</param>
        /// <remarks>
        /// Please see the <see cref="XRInteractionManager"/> and <see cref="XRInteractionUpdateOrder.UpdatePhase"/> documentation for more
        /// details on update order.
        /// </remarks>
        /// <seealso cref="XRInteractionUpdateOrder.UpdatePhase"/>
        /// <seealso cref="IXRInteractable.ProcessInteractable"/>
        void ProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase);
    }
}