namespace UnityEngine.XR.Interaction.Toolkit
{
    /// <summary>
    /// An interface that represents an Interactable component which Interactor
    /// components can activate. Not to be confused with the active state of a GameObject,
    /// an activate event in this context refers to a contextual command action, such as
    /// toggling a flashlight on and off.
    /// </summary>
    /// <seealso cref="IXRActivateInteractor"/>
    public interface IXRActivateInteractable : IXRInteractable
    {
        /// <summary>
        /// The event that is called when the selecting Interactor activates this Interactable.
        /// </summary>
        /// <remarks>
        /// Not to be confused with activating or deactivating a <see cref="GameObject"/> with <see cref="GameObject.SetActive"/>.
        /// This is a generic event when an Interactor wants to activate an Interactable,
        /// such as from a trigger pull on a controller.
        /// <br />
        /// The <see cref="ActivateEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="deactivated"/>
        ActivateEvent activated { get; }

        /// <summary>
        /// The event that is called when an Interactor deactivates this Interactable.
        /// </summary>
        /// <remarks>
        /// Not to be confused with activating or deactivating a <see cref="GameObject"/> with <see cref="GameObject.SetActive"/>.
        /// This is a generic event when an Interactor wants to deactivate an Interactable,
        /// such as from a trigger release on a controller.
        /// <br />
        /// The <see cref="DeactivateEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="activated"/>
        DeactivateEvent deactivated { get; }

        /// <summary>
        /// This method is called when the Interactor begins an activation event on this Interactable.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is sending the activate event.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnDeactivated"/>
        void OnActivated(ActivateEventArgs args);

        /// <summary>
        /// This method is called when the Interactor ends an activation event on this Interactable.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is sending the deactivate event.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnActivated"/>
        void OnDeactivated(DeactivateEventArgs args);
    }
}