using System;
using System.Collections.Generic;

namespace UnityEngine.XR.Interaction.Toolkit.Utilities
{
    /// <summary>
    /// Use this class to maintain a list of Colliders being touched in order to determine the set of
    /// Interactables that are being touched.
    /// </summary>
    /// <remarks>
    /// This class is useful for Interactors that utilize a trigger Collider to determine which objects
    /// it is coming in contact with. For Interactables with multiple Colliders, this will help handle the
    /// bookkeeping to know if any of the colliders are still being touched.
    /// </remarks>
    class TriggerContactMonitor
    {
        /// <summary>
        /// Calls the methods in its invocation list when an Interactable is being touched.
        /// </summary>
        /// <remarks>
        /// Will only be fired for an Interactable once when any of the colliders associated with it are touched.
        /// In other words, touching more of its colliders does not cause this to fire again until all of its colliders
        /// are no longer being touched.
        /// </remarks>
        public event Action<IXRInteractable> contactAdded;

        /// <summary>
        /// Calls the methods in its invocation list when an Interactable is no longer being touched.
        /// </summary>
        /// <remarks>
        /// Will only be fired for an Interactable once all of the colliders associated with it are no longer touched.
        /// In other words, leaving just one of its colliders when another one of it is still being touched
        /// will not fire the event.
        /// </remarks>
        public event Action<IXRInteractable> contactRemoved;

        /// <summary>
        /// The Interaction Manager used to fetch the Interactable associated with a Collider.
        /// </summary>
        /// <seealso cref="XRInteractionManager.GetInteractableForCollider"/>
        public XRInteractionManager interactionManager { get; set; }

        readonly Dictionary<Collider, IXRInteractable> m_EnteredColliders = new Dictionary<Collider, IXRInteractable>();
        readonly HashSet<IXRInteractable> m_UnorderedInteractables = new HashSet<IXRInteractable>();
        readonly HashSet<Collider> m_EnteredUnassociatedColliders = new HashSet<Collider>();

        /// <summary>
        /// Reusable temporary list of Collider objects for resolving unassociated colliders.
        /// </summary>
        static readonly List<Collider> s_ScratchColliders = new List<Collider>();

        /// <summary>
        /// Reusable temporary list of Collider objects for removing colliders that did not stay during the frame
        /// but previously entered.
        /// </summary>
        static readonly List<Collider> s_ExitedColliders = new List<Collider>();

        /// <summary>
        /// Adds <paramref name="collider"/> to contact list.
        /// </summary>
        /// <param name="collider">The Collider to add.</param>
        /// <seealso cref="RemoveCollider"/>
        public void AddCollider(Collider collider)
        {
            if (interactionManager == null)
                return;

            if (!interactionManager.TryGetInteractableForCollider(collider, out var interactable))
            {
                m_EnteredUnassociatedColliders.Add(collider);
                return;
            }

            m_EnteredColliders[collider] = interactable;

            if (m_UnorderedInteractables.Add(interactable))
                contactAdded?.Invoke(interactable);
        }

        /// <summary>
        /// Removes <paramref name="collider"/> from contact list.
        /// </summary>
        /// <param name="collider">The Collider to remove.</param>
        /// <seealso cref="AddCollider"/>
        public void RemoveCollider(Collider collider)
        {
            if (m_EnteredUnassociatedColliders.Remove(collider))
                return;

            if (m_EnteredColliders.TryGetValue(collider, out var interactable))
            {
                m_EnteredColliders.Remove(collider);

                if (interactable == null)
                    return;

                // Don't remove the Interactable if there are still
                // any of its colliders touching this trigger.
                // Treat destroyed colliders as no longer touching.
                foreach (var kvp in m_EnteredColliders)
                {
                    if (kvp.Value == interactable && kvp.Key != null)
                        return;
                }

                if (m_UnorderedInteractables.Remove(interactable))
                    contactRemoved?.Invoke(interactable);
            }
        }

        /// <summary>
        /// Resolves all unassociated colliders to Interactables if possible.
        /// </summary>
        /// <remarks>
        /// This process is done automatically when Colliders are added,
        /// but this method can be used to force a refresh.
        /// </remarks>
        public void ResolveUnassociatedColliders()
        {
            // Cull destroyed colliders from the set to keep it tidy
            // since there would be no reason to monitor it anymore.
            m_EnteredUnassociatedColliders.RemoveWhere(IsDestroyed);

            if (m_EnteredUnassociatedColliders.Count == 0 || interactionManager == null)
                return;

            s_ScratchColliders.Clear();
            foreach (var col in m_EnteredUnassociatedColliders)
            {
                if (interactionManager.TryGetInteractableForCollider(col, out var interactable))
                {
                    // Add to temporary list to remove in a second pass to avoid modifying
                    // the collection being iterated.
                    s_ScratchColliders.Add(col);
                    m_EnteredColliders[col] = interactable;

                    if (m_UnorderedInteractables.Add(interactable))
                        contactAdded?.Invoke(interactable);
                }
            }

            foreach (var col in s_ScratchColliders)
            {
                m_EnteredUnassociatedColliders.Remove(col);
            }

            s_ScratchColliders.Clear();
        }

        /// <summary>
        /// Resolves the unassociated colliders to <paramref name="interactable"/> if they match.
        /// </summary>
        /// <param name="interactable">The Interactable to try to associate with the unassociated colliders.</param>
        /// <remarks>
        /// This process is done automatically when Colliders are added,
        /// but this method can be used to force a refresh.
        /// </remarks>
        public void ResolveUnassociatedColliders(IXRInteractable interactable)
        {
            // Cull destroyed colliders from the set to keep it tidy
            // since there would be no reason to monitor it anymore.
            m_EnteredUnassociatedColliders.RemoveWhere(IsDestroyed);

            if (m_EnteredUnassociatedColliders.Count == 0 || interactionManager == null)
                return;

            foreach (var col in interactable.colliders)
            {
                if (col == null)
                    continue;

                if (m_EnteredUnassociatedColliders.Contains(col) &&
                    interactionManager.TryGetInteractableForCollider(col, out var associatedInteractable) &&
                    associatedInteractable == interactable)
                {
                    m_EnteredUnassociatedColliders.Remove(col);
                    m_EnteredColliders[col] = interactable;

                    if (m_UnorderedInteractables.Add(interactable))
                        contactAdded?.Invoke(interactable);
                }
            }
        }

        /// <summary>
        /// Remove colliders that no longer stay during this frame but previously entered.
        /// </summary>
        /// <param name="stayedColliders">Colliders that stayed during the fixed update.</param>
        /// <remarks>
        /// Can be called in the fixed update phase by interactors after OnTriggerStay.
        /// </remarks>
        public void UpdateStayedColliders(List<Collider> stayedColliders)
        {
            if (m_EnteredColliders.Count == 0)
                return;

            s_ExitedColliders.Clear();

            foreach (var collider in m_EnteredColliders.Keys)
            {
                if (!stayedColliders.Contains(collider))
                    // Add to temporary list to remove in a second pass to avoid modifying
                    // the collection being iterated.
                    s_ExitedColliders.Add(collider);
            }

            foreach (var collider in s_ExitedColliders)
            {
                RemoveCollider(collider);
            }

            s_ExitedColliders.Clear();
        }

        /// <summary>
        /// Checks whether the Interactable is being touched.
        /// </summary>
        /// <param name="interactable">The Interactable to check if touching.</param>
        /// <returns>Returns <see langword="true"/> if the Interactable is being touched. Otherwise, returns <see langword="false"/>.</returns>
        public bool IsContacting(IXRInteractable interactable)
        {
            return m_UnorderedInteractables.Contains(interactable);
        }

        static bool IsDestroyed(Collider col) => col == null;
    }
}