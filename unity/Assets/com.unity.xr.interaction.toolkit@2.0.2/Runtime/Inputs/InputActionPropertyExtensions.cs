using UnityEngine.InputSystem;

namespace UnityEngine.XR.Interaction.Toolkit.Inputs
{
    /// <summary>
    /// Extension methods for <see cref="InputActionProperty"/>.
    /// </summary>
    public static class InputActionPropertyExtensions
    {
        /// <summary>
        /// Enable the action held on to by the <paramref name="property"/> only if it represents
        /// an <see cref="InputAction"/> directly. In other words, function will do nothing if the action
        /// has a non-<see langword="null"/> <see cref="InputActionProperty.reference"/> property.
        /// </summary>
        /// <param name="property">The property to operate on.</param>
        /// <remarks>
        /// This can make it easier to allow the enabled state of the <see cref="InputAction"/> serialized with
        /// a <see cref="MonoBehaviour"/> to be owned by the behavior itself, but let a reference type be managed
        /// elsewhere.
        /// </remarks>
        public static void EnableDirectAction(this InputActionProperty property)
        {
            if (property.reference != null)
                return;

            property.action?.Enable();
        }

        /// <summary>
        /// Disable the action held on to by the <paramref name="property"/> only if it represents
        /// an <see cref="InputAction"/> directly. In other words, function will do nothing if the action
        /// has a non-<see langword="null"/> <see cref="InputActionProperty.reference"/> property.
        /// </summary>
        /// <param name="property">The property to operate on.</param>
        /// <remarks>
        /// This can make it easier to allow the enabled state of the <see cref="InputAction"/> serialized with
        /// a <see cref="MonoBehaviour"/> to be owned by the behavior itself, but let a reference type be managed
        /// elsewhere.
        /// </remarks>
        public static void DisableDirectAction(this InputActionProperty property)
        {
            if (property.reference != null)
                return;

            property.action?.Disable();
        }
    }
}
