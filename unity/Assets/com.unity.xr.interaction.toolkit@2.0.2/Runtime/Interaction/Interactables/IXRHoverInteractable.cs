using System.Collections.Generic;

namespace UnityEngine.XR.Interaction.Toolkit
{
    /// <summary>
    /// An interface that represents an Interactable component which
    /// Interactor components can hover over.
    /// </summary>
    /// <seealso cref="IXRHoverInteractor"/>
    public interface IXRHoverInteractable : IXRInteractable
    {
        /// <summary>
        /// The event that is called only when the first Interactor begins hovering
        /// over this Interactable as the sole hovering Interactor. Subsequent Interactors that
        /// begin hovering over this Interactable will not cause this event to be invoked as
        /// long as any others are still hovering.
        /// </summary>
        /// <remarks>
        /// The <see cref="HoverEnterEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="lastHoverExited"/>
        /// <seealso cref="hoverEntered"/>
        HoverEnterEvent firstHoverEntered { get; }

        /// <summary>
        /// The event that is called only when the last remaining hovering Interactor
        /// ends hovering over this Interactable.
        /// </summary>
        /// <remarks>
        /// The <see cref="HoverExitEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="firstHoverEntered"/>
        /// <seealso cref="hoverExited"/>
        HoverExitEvent lastHoverExited { get; }

        /// <summary>
        /// The event that is called when an Interactor begins hovering over this Interactable.
        /// </summary>
        /// <remarks>
        /// The <see cref="HoverEnterEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="hoverExited"/>
        HoverEnterEvent hoverEntered { get; }

        /// <summary>
        /// The event that is called when an Interactor ends hovering over this Interactable.
        /// </summary>
        /// <remarks>
        /// The <see cref="HoverExitEventArgs"/> passed to each listener is only valid while the event is invoked,
        /// do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="hoverEntered"/>
        HoverExitEvent hoverExited { get; }

        /// <summary>
        /// (Read Only) The list of Interactors that are hovering on this Interactable (may by empty).
        /// </summary>
        /// <remarks>
        /// You should treat this as a read only view of the list and should not modify it.
        /// Unity exposes this as a <see cref="List{T}"/> rather than an <see cref="IReadOnlyList{T}"/> to avoid
        /// GC Allocations when enumerating the list.
        /// </remarks>
        /// <seealso cref="isHovered"/>
        /// <seealso cref="IXRHoverInteractor.interactablesHovered"/>
        List<IXRHoverInteractor> interactorsHovering { get; }

        /// <summary>
        /// (Read Only) Indicates whether an Interactor currently hovers over this Interactable.
        /// </summary>
        /// <remarks>
        /// In other words, returns whether <see cref="interactorsHovering"/> contains any interactors.
        /// <example>
        /// <code>interactorsHovering.Count > 0</code>
        /// </example>
        /// </remarks>
        /// <seealso cref="interactorsHovering"/>
        /// <seealso cref="IXRHoverInteractor.hasHover"/>
        bool isHovered { get; }

        /// <summary>
        /// Determines if a given Interactor can hover over this Interactable.
        /// </summary>
        /// <param name="interactor">Interactor to check for a valid hover state with.</param>
        /// <returns>Returns <see langword="true"/> if hovering is valid this frame. Returns <see langword="false"/> if not.</returns>
        /// <seealso cref="IXRHoverInteractor.CanHover"/>
        bool IsHoverableBy(IXRHoverInteractor interactor);

        /// <summary>
        /// The <see cref="XRInteractionManager"/> calls this method right
        /// before the Interactor first initiates hovering over an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is initiating the hover.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnHoverEntered(HoverEnterEventArgs)"/>
        void OnHoverEntering(HoverEnterEventArgs args);

        /// <summary>
        /// The <see cref="XRInteractionManager"/> calls this method
        /// when the Interactor first initiates hovering over an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is initiating the hover.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnHoverExited(HoverExitEventArgs)"/>
        void OnHoverEntered(HoverEnterEventArgs args);

        /// <summary>
        /// The <see cref="XRInteractionManager"/> calls this method
        /// right before the Interactor ends hovering over an Interactable
        /// in a first pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is ending the hover.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnHoverExited(HoverExitEventArgs)"/>
        void OnHoverExiting(HoverExitEventArgs args);

        /// <summary>
        /// The <see cref="XRInteractionManager"/> calls this method
        /// when the Interactor ends hovering over an Interactable
        /// in a second pass.
        /// </summary>
        /// <param name="args">Event data containing the Interactor that is ending the hover.</param>
        /// <remarks>
        /// <paramref name="args"/> is only valid during this method call, do not hold a reference to it.
        /// </remarks>
        /// <seealso cref="OnHoverEntered(HoverEnterEventArgs)"/>
        void OnHoverExited(HoverExitEventArgs args);
    }

    /// <summary>
    /// Extension methods for <see cref="IXRHoverInteractable"/>.
    /// </summary>
    /// <seealso cref="IXRHoverInteractable"/>
    public static class XRHoverInteractableExtensions
    {
        /// <summary>
        /// Gets the oldest interactor currently hovering on this interactable.
        /// This is a convenience method for when the interactable does not support being hovered by multiple interactors at a time.
        /// </summary>
        /// <param name="interactable">The interactable to operate on.</param>
        /// <returns>Returns the oldest interactor currently hovering on this interactable.</returns>
        /// <remarks>
        /// Equivalent to <code>interactorsHovering.Count > 0 ? interactorsHovering[0] : null</code>
        /// </remarks>
        /// <seealso cref="IXRHoverInteractable.interactorsHovering"/>
        public static IXRHoverInteractor GetOldestInteractorHovering(this IXRHoverInteractable interactable) =>
            interactable?.interactorsHovering.Count > 0 ? interactable.interactorsHovering[0] : null;
    }
}