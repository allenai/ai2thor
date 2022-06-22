using System.Collections.Generic;

namespace UnityEngine.XR.Interaction.Toolkit
{
    /// <summary>
    /// An interface that represents an Interactor component that can select
    /// an Interactable component.
    /// </summary>
    /// <seealso cref="IXRSelectInteractable"/>
    public interface IXRSelectInteractor : IXRInteractor
    {
        /// <summary>
        /// The event that is called when this Interactor begins selecting an Interactable.
        /// </summary>
        /// <remarks>
        /// The <see cref="SelectEnterEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="selectExited"/>
        SelectEnterEvent selectEntered { get; }

        /// <summary>
        /// The event that is called when this Interactor ends selecting an Interactable.
        /// </summary>
        /// <remarks>
        /// The <see cref="SelectEnterEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="selectEntered"/>
        SelectExitEvent selectExited { get; }

        /// <summary>
        /// (Read Only) The list of Interactables that are currently being selected (may by empty).
        /// </summary>
        /// <remarks>
        /// This should be treated as a read only view of the list and should not be modified by external callers.
        /// It is exposed as a <see cref="List{T}"/> rather than an <see cref="IReadOnlyList{T}"/> to avoid GC Allocations
        /// when enumerating the list.
        /// </remarks>
        /// <seealso cref="hasSelection"/>
        /// <seealso cref="IXRSelectInteractable.interactorsSelecting"/>
        List<IXRSelectInteractable> interactablesSelected { get; }

        /// <summary>
        /// (Read Only) The first Interactable selected since not having any selection.
        /// This Interactor may not currently be selecting the Interactable, which would be the case
        /// when it was released while multiple Interactables were selected.
        /// </summary>
        /// <seealso cref="IXRSelectInteractable.firstInteractorSelecting"/>
        IXRSelectInteractable firstInteractableSelected { get; }

        /// <summary>
        /// (Read Only) Indicates whether this Interactor is currently selecting an Interactable.
        /// </summary>
        /// <remarks>
        /// In other words, returns whether <see cref="interactablesSelected"/> contains any Interactables.
        /// <example>
        /// <code>interactablesSelected.Count > 0</code>
        /// </example>
        /// </remarks>
        /// <seealso cref="interactablesSelected"/>
        /// <seealso cref="IXRSelectInteractable.isSelected"/>
        bool hasSelection { get; }

        /// <summary>
        /// (Read Only) Indicates whether this Interactor is in a state where it could select.
        /// </summary>
        bool isSelectActive { get; }

        /// <summary>
        /// Whether to keep selecting an Interactable after initially selecting it even when it is no longer a valid target.
        /// </summary>
        /// <remarks>
        /// Return <see langword="true"/> to make the <see cref="XRInteractionManager"/> retain the selection even if the
        /// Interactable is not contained within the list of valid targets. Return <see langword="false"/> to make
        /// the Interaction Manager clear the selection if it isn't within the list of valid targets.
        /// <br/>
        /// A common use for disabling this is for Ray Interactors used for teleportation to make the teleportation Interactable
        /// no longer selected when not currently pointing at it.
        /// </remarks>
        bool keepSelectedTargetValid { get; }

        /// <summary>
        /// Determines if the Interactable is valid for selection this frame.
        /// </summary>
        /// <param name="interactable">Interactable to check.</param>
        /// <returns>Returns <see langword="true"/> if the interactable can be selected this frame.</returns>
        /// <seealso cref="IXRSelectInteractable.IsSelectableBy"/>
        bool CanSelect(IXRSelectInteractable interactable);

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
        bool IsSelecting(IXRSelectInteractable interactable);

        /// <summary>
        /// Gets the world position and rotation of the Attach Transform captured during the moment of selection.
        /// </summary>
        /// <param name="interactable">The specific Interactable as context to get the attachment point for.</param>
        /// <returns>Returns the world pose of the attachment point during the moment of selection,
        /// and otherwise the identity <see cref="Pose"/> if it was not selected during the current selection stack.</returns>
        /// <seealso cref="GetLocalAttachPoseOnSelect"/>
        /// <seealso cref="IXRInteractor.GetAttachTransform"/>
        /// <seealso cref="IXRSelectInteractable.GetAttachPoseOnSelect"/>
        Pose GetAttachPoseOnSelect(IXRSelectInteractable interactable);

        /// <summary>
        /// Gets the local position and rotation of the Attach Transform captured during the moment of selection.
        /// </summary>
        /// <param name="interactable">The specific Interactable as context to get the attachment point for.</param>
        /// <returns>Returns the local pose of the attachment point during the moment of selection,
        /// and otherwise the identity <see cref="Pose"/> if it was not selected during the current selection stack.</returns>
        /// <seealso cref="GetAttachPoseOnSelect"/>
        /// <seealso cref="IXRInteractor.GetAttachTransform"/>
        /// <seealso cref="IXRSelectInteractable.GetLocalAttachPoseOnSelect"/>
        Pose GetLocalAttachPoseOnSelect(IXRSelectInteractable interactable);

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
        void OnSelectEntering(SelectEnterEventArgs args);

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
        void OnSelectEntered(SelectEnterEventArgs args);

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
        void OnSelectExiting(SelectExitEventArgs args);

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
        void OnSelectExited(SelectExitEventArgs args);
    }

    /// <summary>
    /// Extension methods for <see cref="IXRSelectInteractor"/>.
    /// </summary>
    /// <seealso cref="IXRSelectInteractor"/>
    public static class XRSelectInteractorExtensions
    {
        /// <summary>
        /// Gets the oldest Interactable currently selected.
        /// This is a convenience method for when the Interactor does not support selecting multiple interactables at a time.
        /// </summary>
        /// <param name="interactor">The Interactor to operate on.</param>
        /// <returns>Returns the oldest Interactable currently selected.</returns>
        /// <remarks>
        /// Equivalent to <code>interactablesSelected.Count > 0 ? interactablesSelected[0] : null</code>
        /// </remarks>
        /// <seealso cref="IXRSelectInteractor.interactablesSelected"/>
        public static IXRSelectInteractable GetOldestInteractableSelected(this IXRSelectInteractor interactor) =>
            interactor?.interactablesSelected.Count > 0 ? interactor.interactablesSelected[0] : null;
    }
}