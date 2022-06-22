using System;
using System.Collections.Generic;

namespace UnityEngine.XR.Interaction.Toolkit
{
    /// <summary>
    /// An interface that represents an Interactable component that controls how a GameObject
    /// interacts with an Interactor component. An example is a Grab Interactable which
    /// can be picked up and moved by an Interactor.
    /// </summary>
    /// <remarks>
    /// When scripting, you can typically write custom behaviors that derive from <see cref="XRBaseInteractable"/>
    /// or one of its derived classes rather than implementing this interface directly.
    /// </remarks>
    /// <seealso cref="XRBaseInteractable"/>
    /// <seealso cref="IXRActivateInteractable"/>
    /// <seealso cref="IXRHoverInteractable"/>
    /// <seealso cref="IXRSelectInteractable"/>
    /// <seealso cref="IXRInteractor"/>
    public interface IXRInteractable
    {
        /// <summary>
        /// Unity calls the methods in this invocation list when this Interactable is registered with an <see cref="XRInteractionManager"/>.
        /// </summary>
        /// <remarks>
        /// The <see cref="InteractableRegisteredEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="XRInteractionManager.interactableRegistered"/>
        event Action<InteractableRegisteredEventArgs> registered;

        /// <summary>
        /// Unity calls the methods in this invocation list when this Interactable is unregistered from an <see cref="XRInteractionManager"/>.
        /// </summary>
        /// <remarks>
        /// The <see cref="InteractableUnregisteredEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="XRInteractionManager.interactableUnregistered"/>
        event Action<InteractableUnregisteredEventArgs> unregistered;

        /// <summary>
        /// (Read Only) Allows interaction with Interactors whose Interaction Layer Mask overlaps with any Layer in this Interaction Layer Mask.
        /// </summary>
        /// <seealso cref="IXRInteractor.interactionLayers"/>
        InteractionLayerMask interactionLayers { get; }

        /// <summary>
        /// (Read Only) Colliders to use for interaction with this Interactable.
        /// </summary>
        List<Collider> colliders { get; }

        /// <summary>
        /// (Read Only) The <see cref="Transform"/> associated with the Interactable.
        /// </summary>
        /// <remarks>
        /// When this Interactable is a component, this property is the Transform of the GameObject the component is attached to.
        /// </remarks>
        Transform transform { get; }

        /// <summary>
        /// Gets the <see cref="Transform"/> that serves as the attachment point for a given Interactor.
        /// </summary>
        /// <param name="interactor">The specific Interactor as context to get the attachment point for.</param>
        /// <returns>Returns the attachment point <see cref="Transform"/>.</returns>
        /// <seealso cref="IXRInteractor.GetAttachTransform"/>
        /// <remarks>
        /// This should typically return the Transform of a child GameObject or the <see cref="transform"/> itself.
        /// </remarks>
        Transform GetAttachTransform(IXRInteractor interactor);

        /// <summary>
        /// The <see cref="XRInteractionManager"/> calls this method
        /// when this Interactable is registered with it.
        /// </summary>
        /// <param name="args">Event data containing the Interaction Manager that registered this Interactable.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="XRInteractionManager.RegisterInteractable(IXRInteractable)"/>
        void OnRegistered(InteractableRegisteredEventArgs args);

        /// <summary>
        /// The <see cref="XRInteractionManager"/> calls this method
        /// when this Interactable is unregistered from it.
        /// </summary>
        /// <param name="args">Event data containing the Interaction Manager that unregistered this Interactable.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="XRInteractionManager.UnregisterInteractable(IXRInteractable)"/>
        void OnUnregistered(InteractableUnregisteredEventArgs args);

        /// <summary>
        /// The <see cref="XRInteractionManager"/> calls this method to update the Interactable.
        /// </summary>
        /// <param name="updatePhase">The update phase this is called during.</param>
        /// <remarks>
        /// Please see the <see cref="XRInteractionManager"/> and <see cref="XRInteractionUpdateOrder.UpdatePhase"/> documentation for more
        /// details on update order.
        /// </remarks>
        /// <seealso cref="XRInteractionUpdateOrder.UpdatePhase"/>
        /// <seealso cref="IXRInteractor.ProcessInteractor"/>
        void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase);

        /// <summary>
        /// Calculates squared distance to an Interactor (based on colliders).
        /// </summary>
        /// <param name="interactor">Interactor to calculate distance against.</param>
        /// <returns>Returns the minimum squared distance between the Interactor and this Interactable's colliders.</returns>
        float GetDistanceSqrToInteractor(IXRInteractor interactor);
    }
}