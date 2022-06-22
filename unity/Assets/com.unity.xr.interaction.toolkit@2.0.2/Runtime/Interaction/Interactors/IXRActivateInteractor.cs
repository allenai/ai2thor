using System.Collections.Generic;

namespace UnityEngine.XR.Interaction.Toolkit
{
    /// <summary>
    /// An interface that represents an Interactor component that can activate
    /// an Interactable component. Not to be confused with the active state of a GameObject,
    /// an activate event in this context refers to a contextual command action, such as
    /// toggling a flashlight on and off.
    /// </summary>
    /// <seealso cref="IXRActivateInteractable"/>
    public interface IXRActivateInteractor : IXRInteractor
    {
        /// <summary>
        /// (Read Only) Indicates whether this interactor is in a state where it should send the activate event this frame.
        /// </summary>
        bool shouldActivate { get; }

        /// <summary>
        /// (Read Only) Indicates whether this interactor is in a state where it should send the deactivate event this frame.
        /// </summary>
        bool shouldDeactivate { get; }

        /// <summary>
        /// Retrieve the list of Interactables that this Interactor could possibly activate or deactivate this frame.
        /// </summary>
        /// <param name="targets">The results list to populate with Interactables that are valid for activate or deactivate.</param>
        /// <remarks>
        /// When implementing this method, Unity expects you to clear <paramref name="targets"/> before adding to it.
        /// </remarks>
        void GetActivateTargets(List<IXRActivateInteractable> targets);
    }
}