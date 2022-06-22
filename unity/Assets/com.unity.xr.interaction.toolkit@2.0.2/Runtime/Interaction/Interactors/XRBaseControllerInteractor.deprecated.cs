using System;
using System.Collections.Generic;

namespace UnityEngine.XR.Interaction.Toolkit
{
    public abstract partial class XRBaseControllerInteractor
    {
        /// <summary>
        /// (Deprecated) Controls whether Unity plays an <see cref="AudioClip"/> on Select Entered.
        /// </summary>
        /// <seealso cref="audioClipForOnSelectEntered"/>
        /// <remarks><c>playAudioClipOnSelectEnter</c> has been deprecated. Use <see cref="playAudioClipOnSelectEntered"/> instead.</remarks>
        [Obsolete("playAudioClipOnSelectEnter has been deprecated. Use playAudioClipOnSelectEntered instead. (UnityUpgradable) -> playAudioClipOnSelectEntered")]
        public bool playAudioClipOnSelectEnter => playAudioClipOnSelectEntered;

        /// <summary>
        /// (Deprecated) The <see cref="AudioClip"/> Unity plays on Select Entered.
        /// </summary>
        /// <seealso cref="playAudioClipOnSelectEntered"/>
        /// <remarks><c>audioClipForOnSelectEnter</c> has been deprecated. Use <see cref="audioClipForOnSelectEntered"/> instead.</remarks>
        [Obsolete("audioClipForOnSelectEnter has been deprecated. Use audioClipForOnSelectEntered instead. (UnityUpgradable) -> audioClipForOnSelectEntered")]
        public AudioClip audioClipForOnSelectEnter => audioClipForOnSelectEntered;

        /// <summary>
        /// (Deprecated) The <see cref="AudioClip"/> Unity plays on Select Entered.
        /// </summary>
        /// <seealso cref="playAudioClipOnSelectEntered"/>
        /// <remarks><c>AudioClipForOnSelectEnter</c> has been deprecated. Use <see cref="audioClipForOnSelectEntered"/> instead.</remarks>
#pragma warning disable IDE1006 // Naming Styles
        [Obsolete("AudioClipForOnSelectEnter has been deprecated. Use audioClipForOnSelectEntered instead. (UnityUpgradable) -> audioClipForOnSelectEntered")]
        public AudioClip AudioClipForOnSelectEnter
        {
            get => audioClipForOnSelectEntered;
            set => audioClipForOnSelectEntered = value;
        }
#pragma warning restore IDE1006

        /// <summary>
        /// (Deprecated) Controls whether Unity plays an <see cref="AudioClip"/> on Select Exited.
        /// </summary>
        /// <seealso cref="audioClipForOnSelectExited"/>
        /// <remarks><c>playAudioClipOnSelectExit</c> has been deprecated. Use <see cref="playAudioClipOnSelectExited"/> instead.</remarks>
        [Obsolete("playAudioClipOnSelectExit has been deprecated. Use playAudioClipOnSelectExited instead. (UnityUpgradable) -> playAudioClipOnSelectExited")]
        public bool playAudioClipOnSelectExit => playAudioClipOnSelectExited;

        /// <summary>
        /// (Deprecated) The <see cref="AudioClip"/> Unity plays on Select Exited.
        /// </summary>
        /// <seealso cref="playAudioClipOnSelectExited"/>
        /// <remarks><c>audioClipForOnSelectExit</c> has been deprecated. Use <see cref="audioClipForOnSelectExited"/> instead.</remarks>
        [Obsolete("audioClipForOnSelectExit has been deprecated. Use audioClipForOnSelectExited instead. (UnityUpgradable) -> audioClipForOnSelectExited")]
        public AudioClip audioClipForOnSelectExit => audioClipForOnSelectExited;

        /// <summary>
        /// (Deprecated) The <see cref="AudioClip"/> Unity plays on Select Exited.
        /// </summary>
        /// <seealso cref="playAudioClipOnSelectExited"/>
        /// <remarks><c>AudioClipForOnSelectExit</c> has been deprecated. Use <see cref="audioClipForOnSelectExited"/> instead.</remarks>
#pragma warning disable IDE1006 // Naming Styles
        [Obsolete("AudioClipForOnSelectExit has been deprecated. Use audioClipForOnSelectExited instead. (UnityUpgradable) -> audioClipForOnSelectExited")]
        public AudioClip AudioClipForOnSelectExit
        {
            get => audioClipForOnSelectExited;
            set => audioClipForOnSelectExited = value;
        }
#pragma warning restore IDE1006

        /// <summary>
        /// (Deprecated) Controls whether Unity plays an <see cref="AudioClip"/> on Hover Entered.
        /// </summary>
        /// <seealso cref="audioClipForOnHoverEntered"/>
        /// <remarks><c>playAudioClipOnHoverEnter</c> has been deprecated. Use <see cref="playAudioClipOnHoverEntered"/> instead.</remarks>
        [Obsolete("playAudioClipOnHoverEnter has been deprecated. Use playAudioClipOnHoverEntered instead. (UnityUpgradable) -> playAudioClipOnHoverEntered")]
        public bool playAudioClipOnHoverEnter => playAudioClipOnHoverEntered;

        /// <summary>
        /// (Deprecated) The <see cref="AudioClip"/> Unity plays on Hover Entered.
        /// </summary>
        /// <seealso cref="playAudioClipOnHoverEntered"/>
        /// <remarks><c>audioClipForOnHoverEnter</c> has been deprecated. Use <see cref="audioClipForOnHoverEntered"/> instead.</remarks>
        [Obsolete("audioClipForOnHoverEnter has been deprecated. Use audioClipForOnHoverEntered instead. (UnityUpgradable) -> audioClipForOnHoverEntered")]
        public AudioClip audioClipForOnHoverEnter => audioClipForOnHoverEntered;

        /// <summary>
        /// (Deprecated) The <see cref="AudioClip"/> Unity plays on Hover Entered.
        /// </summary>
        /// <seealso cref="playAudioClipOnHoverEntered"/>
        /// <remarks><c>AudioClipForOnHoverEnter</c> has been deprecated. Use <see cref="audioClipForOnHoverEntered"/> instead.</remarks>
#pragma warning disable IDE1006 // Naming Styles
        [Obsolete("AudioClipForOnHoverEnter has been deprecated. Use audioClipForOnHoverEntered instead. (UnityUpgradable) -> audioClipForOnHoverEntered")]
        public AudioClip AudioClipForOnHoverEnter
        {
            get => audioClipForOnHoverEntered;
            set => audioClipForOnHoverEntered = value;
        }
#pragma warning restore IDE1006

        /// <summary>
        /// (Deprecated) Controls whether Unity plays an <see cref="AudioClip"/> on Hover Exited.
        /// </summary>
        /// <seealso cref="audioClipForOnHoverExited"/>
        /// <remarks><c>playAudioClipOnHoverExit</c> has been deprecated. Use <see cref="playAudioClipOnHoverExited"/> instead.</remarks>
        [Obsolete("playAudioClipOnHoverExit has been deprecated. Use playAudioClipOnHoverExited instead. (UnityUpgradable) -> playAudioClipOnHoverExited")]
        public bool playAudioClipOnHoverExit => playAudioClipOnHoverExited;

        /// <summary>
        /// (Deprecated) The <see cref="AudioClip"/> Unity plays on Hover Exited.
        /// </summary>
        /// <seealso cref="playAudioClipOnHoverExited"/>
        /// <remarks><c>audioClipForOnHoverExit</c> has been deprecated. Use <see cref="audioClipForOnHoverExited"/> instead.</remarks>
        [Obsolete("audioClipForOnHoverExit has been deprecated. Use audioClipForOnHoverExited instead. (UnityUpgradable) -> audioClipForOnHoverExited")]
        public AudioClip audioClipForOnHoverExit => audioClipForOnHoverExited;

        /// <summary>
        /// (Deprecated) The <see cref="AudioClip"/> Unity plays on Hover Exited.
        /// </summary>
        /// <seealso cref="playAudioClipOnHoverExited"/>
        /// <remarks><c>AudioClipForOnHoverExit</c> has been deprecated. Use <see cref="audioClipForOnHoverExited"/> instead.</remarks>
#pragma warning disable IDE1006 // Naming Styles
        [Obsolete("AudioClipForOnHoverExit has been deprecated. Use audioClipForOnHoverExited instead. (UnityUpgradable) -> audioClipForOnHoverExited")]
        public AudioClip AudioClipForOnHoverExit
        {
            get => audioClipForOnHoverExited;
            set => audioClipForOnHoverExited = value;
        }
#pragma warning restore IDE1006

        /// <summary>
        /// (Deprecated) Controls whether Unity plays haptics on Select Entered.
        /// </summary>
        /// <seealso cref="hapticSelectEnterIntensity"/>
        /// <seealso cref="hapticSelectEnterDuration"/>
        /// <remarks><c>playHapticsOnSelectEnter</c> has been deprecated. Use <see cref="playHapticsOnSelectEntered"/> instead.</remarks>
        [Obsolete("playHapticsOnSelectEnter has been deprecated. Use playHapticsOnSelectEntered instead. (UnityUpgradable) -> playHapticsOnSelectEntered")]
        public bool playHapticsOnSelectEnter => playHapticsOnSelectEntered;

        /// <summary>
        /// (Deprecated) Controls whether Unity plays haptics on Select Exited.
        /// </summary>
        /// <seealso cref="hapticSelectExitIntensity"/>
        /// <seealso cref="hapticSelectExitDuration"/>
        /// <remarks><c>playHapticsOnSelectExit</c> has been deprecated. Use <see cref="playHapticsOnSelectExited"/> instead.</remarks>
        [Obsolete("playHapticsOnSelectExit has been deprecated. Use playHapticsOnSelectExited instead. (UnityUpgradable) -> playHapticsOnSelectExited")]
        public bool playHapticsOnSelectExit => playHapticsOnSelectExited;

        /// <summary>
        /// (Deprecated) Controls whether Unity plays haptics on Hover Entered.
        /// </summary>
        /// <seealso cref="hapticHoverEnterIntensity"/>
        /// <seealso cref="hapticHoverEnterDuration"/>
        /// <remarks><c>playHapticsOnHoverEnter</c> has been deprecated. Use <see cref="playHapticsOnHoverEntered"/> instead.</remarks>
        [Obsolete("playHapticsOnHoverEnter has been deprecated. Use playHapticsOnHoverEntered instead. (UnityUpgradable) -> playHapticsOnHoverEntered")]
        public bool playHapticsOnHoverEnter => playHapticsOnHoverEntered;

        /// <summary>
        /// (Deprecated) (Read Only) A list of Interactables that this Interactor could possibly interact with this frame.
        /// </summary>
        [Obsolete("validTargets has been deprecated. Use a property of type List<IXRInteractable> instead.")]
        protected virtual List<XRBaseInteractable> validTargets { get; } = new List<XRBaseInteractable>();
    }
}