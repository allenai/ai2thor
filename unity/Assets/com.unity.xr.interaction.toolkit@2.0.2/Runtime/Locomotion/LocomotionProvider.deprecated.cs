using System;

namespace UnityEngine.XR.Interaction.Toolkit
{
    public abstract partial class LocomotionProvider
    {
        /// <summary>
        /// (Deprecated) The <see cref="startLocomotion"/> action will be called when a <see cref="LocomotionProvider"/> successfully begins a locomotion event.
        /// </summary>
        /// <seealso cref="beginLocomotion"/>
        /// <remarks>
        /// <c>startLocomotion</c> has been deprecated. Use <see cref="beginLocomotion"/> instead.
        /// </remarks>
        [Obsolete("startLocomotion has been deprecated. Use beginLocomotion instead. (UnityUpgradable) -> beginLocomotion", true)]
#pragma warning disable 67 // Never invoked, kept for API Updater
        public event Action<LocomotionSystem> startLocomotion;
#pragma warning restore 67
    }
}
