using System;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using Object = UnityEngine.Object;

namespace UnityEditor.XR.Interaction.Toolkit
{
    public partial class XRBaseInteractorEditor
    {
        /// <summary>
        /// Get whether deprecated events are in use.
        /// </summary>
        /// <returns>Returns <see langword="true"/> if deprecated events are in use. Otherwise, returns <see langword="false"/>.</returns>
        [Obsolete("IsDeprecatedEventsInUse is marked for deprecation and will be removed in a future version. It is only used for migrating deprecated events.")]
        protected virtual bool IsDeprecatedEventsInUse()
        {
            return m_OnHoverEnteredCalls.arraySize > 0 || m_OnHoverEnteredCalls.hasMultipleDifferentValues ||
                m_OnHoverExitedCalls.arraySize > 0 || m_OnHoverExitedCalls.hasMultipleDifferentValues ||
                m_OnSelectEnteredCalls.arraySize > 0 || m_OnSelectEnteredCalls.hasMultipleDifferentValues ||
                m_OnSelectExitedCalls.arraySize > 0 || m_OnSelectExitedCalls.hasMultipleDifferentValues;
        }

        /// <summary>
        /// Migrate the persistent listeners from the deprecated <see cref="UnityEvent"/>
        /// properties to the new events on an <see cref="XRBaseInteractor"/>.
        /// </summary>
        /// <param name="serializedObject">The object to upgrade.</param>
        [Obsolete("MigrateEvents is marked for deprecation and will be removed in a future version. It is only used for migrating deprecated events.")]
        protected virtual void MigrateEvents(SerializedObject serializedObject)
        {
#pragma warning disable 618 // One-time migration of deprecated events.
            EventMigrationUtility.MigrateEvent(serializedObject.FindProperty("m_OnHoverEntered"), serializedObject.FindProperty("m_HoverEntered"));
            EventMigrationUtility.MigrateEvent(serializedObject.FindProperty("m_OnHoverExited"), serializedObject.FindProperty("m_HoverExited"));
            EventMigrationUtility.MigrateEvent(serializedObject.FindProperty("m_OnSelectEntered"), serializedObject.FindProperty("m_SelectEntered"));
            EventMigrationUtility.MigrateEvent(serializedObject.FindProperty("m_OnSelectExited"), serializedObject.FindProperty("m_SelectExited"));
#pragma warning restore 618
        }

        /// <summary>
        /// Migrate the persistent listeners from the deprecated <see cref="UnityEvent"/>
        /// properties to the new events on an <see cref="XRBaseInteractor"/>.
        /// </summary>
        /// <param name="targets">An array of all the objects to upgrade.</param>
        [Obsolete("MigrateEvents is marked for deprecation and will be removed in a future version. It is only used for migrating deprecated events.")]
        public void MigrateEvents(Object[] targets)
        {
            foreach (var target in targets)
            {
                var serializedObject = new SerializedObject(target);
                MigrateEvents(serializedObject);
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
