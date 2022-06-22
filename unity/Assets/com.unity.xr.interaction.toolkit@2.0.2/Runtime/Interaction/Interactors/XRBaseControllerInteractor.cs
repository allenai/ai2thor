using System;
using System.Collections.Generic;
using UnityEngine.Serialization;
using UnityEngine.XR.Interaction.Toolkit.Utilities.Pooling;

namespace UnityEngine.XR.Interaction.Toolkit
{
    /// <summary>
    /// Abstract base class from which all interactors that are controller-driven derive.
    /// This class hooks into the interaction system (via <see cref="XRInteractionManager"/>) and provides base virtual methods for handling
    /// hover and selection. Additionally, this class provides functionality for checking the controller's selection status
    /// and hiding the controller on selection.
    /// </summary>
    public abstract partial class XRBaseControllerInteractor : XRBaseInteractor, IXRActivateInteractor
    {
        /// <summary>
        /// This defines the type of input that triggers an interaction.
        /// </summary>
        /// <seealso cref="selectActionTrigger"/>
        public enum InputTriggerType
        {
            /// <summary>
            /// Unity will consider the input active while the button is pressed.
            /// A user can hold the button before the interaction is possible
            /// and still trigger the interaction when it is possible.
            /// </summary>
            /// <seealso cref="InteractionState.active"/>
            State,

            /// <summary>
            /// Unity will consider the input active only on the frame the button is pressed,
            /// and if successful remain engaged until the input is released.
            /// A user must press the button while the interaction is possible to trigger the interaction.
            /// They will not trigger the interaction if they started pressing the button before the interaction was possible.
            /// </summary>
            /// <seealso cref="InteractionState.activatedThisFrame"/>
            StateChange,

            /// <summary>
            /// The interaction starts on the frame the input is pressed
            /// and remains engaged until the second time the input is pressed.
            /// </summary>
            Toggle,

            /// <summary>
            /// The interaction starts on the frame the input is pressed
            /// and remains engaged until the second time the input is released.
            /// </summary>
            Sticky,
        }

        [SerializeField]
        InputTriggerType m_SelectActionTrigger = InputTriggerType.State;
        /// <summary>
        /// Choose how Unity interprets the select input action from the controller.
        /// Controls between different input styles for determining if this Interactor can select,
        /// such as whether the button is currently pressed or just toggles the active state.
        /// </summary>
        /// <seealso cref="InputTriggerType"/>
        /// <seealso cref="isSelectActive"/>
        public InputTriggerType selectActionTrigger
        {
            get => m_SelectActionTrigger;
            set => m_SelectActionTrigger = value;
        }

        [SerializeField]
        bool m_HideControllerOnSelect;
        /// <summary>
        /// Controls whether this Interactor should hide the controller model on selection.
        /// </summary>
        /// <seealso cref="XRBaseController.hideControllerModel"/>
        public bool hideControllerOnSelect
        {
            get => m_HideControllerOnSelect;
            set
            {
                m_HideControllerOnSelect = value;
                if (!m_HideControllerOnSelect && m_Controller != null)
                    m_Controller.hideControllerModel = false;
            }
        }

        [SerializeField]
        bool m_AllowHoveredActivate;
        /// <summary>
        /// Controls whether to send activate and deactivate events to interactables
        /// that this interactor is hovered over but not selected when there is no current selection.
        /// By default, the interactor will only send activate and deactivate events to interactables that it's selected.
        /// </summary>
        /// <seealso cref="allowActivate"/>
        /// <seealso cref="GetActivateTargets"/>
        public bool allowHoveredActivate
        {
            get => m_AllowHoveredActivate;
            set => m_AllowHoveredActivate = value;
        }

        [SerializeField, FormerlySerializedAs("m_PlayAudioClipOnSelectEnter")]
        bool m_PlayAudioClipOnSelectEntered;
        /// <summary>
        /// Controls whether Unity plays an <see cref="AudioClip"/> on Select Entered.
        /// </summary>
        /// <seealso cref="audioClipForOnSelectEntered"/>
        public bool playAudioClipOnSelectEntered
        {
            get => m_PlayAudioClipOnSelectEntered;
            set => m_PlayAudioClipOnSelectEntered = value;
        }

        [SerializeField, FormerlySerializedAs("m_AudioClipForOnSelectEnter")]
        AudioClip m_AudioClipForOnSelectEntered;
        /// <summary>
        /// The <see cref="AudioClip"/> Unity plays on Select Entered.
        /// </summary>
        /// <seealso cref="playAudioClipOnSelectEntered"/>
        public AudioClip audioClipForOnSelectEntered
        {
            get => m_AudioClipForOnSelectEntered;
            set => m_AudioClipForOnSelectEntered = value;
        }

        [SerializeField, FormerlySerializedAs("m_PlayAudioClipOnSelectExit")]
        bool m_PlayAudioClipOnSelectExited;
        /// <summary>
        /// Controls whether Unity plays an <see cref="AudioClip"/> on Select Exited.
        /// </summary>
        /// <seealso cref="audioClipForOnSelectExited"/>
        public bool playAudioClipOnSelectExited
        {
            get => m_PlayAudioClipOnSelectExited;
            set => m_PlayAudioClipOnSelectExited = value;
        }

        [SerializeField, FormerlySerializedAs("m_AudioClipForOnSelectExit")]
        AudioClip m_AudioClipForOnSelectExited;
        /// <summary>
        /// The <see cref="AudioClip"/> Unity plays on Select Exited.
        /// </summary>
        /// <seealso cref="playAudioClipOnSelectExited"/>
        public AudioClip audioClipForOnSelectExited
        {
            get => m_AudioClipForOnSelectExited;
            set => m_AudioClipForOnSelectExited = value;
        }

        [SerializeField]
        bool m_PlayAudioClipOnSelectCanceled;
        /// <summary>
        /// Controls whether Unity plays an <see cref="AudioClip"/> on Select Canceled.
        /// </summary>
        /// <seealso cref="audioClipForOnSelectCanceled"/>
        public bool playAudioClipOnSelectCanceled
        {
            get => m_PlayAudioClipOnSelectCanceled;
            set => m_PlayAudioClipOnSelectCanceled = value;
        }

        [SerializeField]
        AudioClip m_AudioClipForOnSelectCanceled;
        /// <summary>
        /// The <see cref="AudioClip"/> Unity plays on Select Canceled.
        /// </summary>
        /// <seealso cref="playAudioClipOnSelectCanceled"/>
        public AudioClip audioClipForOnSelectCanceled
        {
            get => m_AudioClipForOnSelectCanceled;
            set => m_AudioClipForOnSelectCanceled = value;
        }

        [SerializeField, FormerlySerializedAs("m_PlayAudioClipOnHoverEnter")]
        bool m_PlayAudioClipOnHoverEntered;
        /// <summary>
        /// Controls whether Unity plays an <see cref="AudioClip"/> on Hover Entered.
        /// </summary>
        /// <seealso cref="audioClipForOnHoverEntered"/>
        public bool playAudioClipOnHoverEntered
        {
            get => m_PlayAudioClipOnHoverEntered;
            set => m_PlayAudioClipOnHoverEntered = value;
        }

        [SerializeField, FormerlySerializedAs("m_AudioClipForOnHoverEnter")]
        AudioClip m_AudioClipForOnHoverEntered;
        /// <summary>
        /// The <see cref="AudioClip"/> Unity plays on Hover Entered.
        /// </summary>
        /// <seealso cref="playAudioClipOnHoverEntered"/>
        public AudioClip audioClipForOnHoverEntered
        {
            get => m_AudioClipForOnHoverEntered;
            set => m_AudioClipForOnHoverEntered = value;
        }

        [SerializeField, FormerlySerializedAs("m_PlayAudioClipOnHoverExit")]
        bool m_PlayAudioClipOnHoverExited;
        /// <summary>
        /// Controls whether Unity plays an <see cref="AudioClip"/> on Hover Exited.
        /// </summary>
        /// <seealso cref="audioClipForOnHoverExited"/>
        public bool playAudioClipOnHoverExited
        {
            get => m_PlayAudioClipOnHoverExited;
            set => m_PlayAudioClipOnHoverExited = value;
        }

        [SerializeField, FormerlySerializedAs("m_AudioClipForOnHoverExit")]
        AudioClip m_AudioClipForOnHoverExited;
        /// <summary>
        /// The <see cref="AudioClip"/> Unity plays on Hover Exited.
        /// </summary>
        /// <seealso cref="playAudioClipOnHoverExited"/>
        public AudioClip audioClipForOnHoverExited
        {
            get => m_AudioClipForOnHoverExited;
            set => m_AudioClipForOnHoverExited = value;
        }

        [SerializeField]
        bool m_PlayAudioClipOnHoverCanceled;
        /// <summary>
        /// Controls whether Unity plays an <see cref="AudioClip"/> on Hover Canceled.
        /// </summary>
        /// <seealso cref="audioClipForOnHoverCanceled"/>
        public bool playAudioClipOnHoverCanceled
        {
            get => m_PlayAudioClipOnHoverCanceled;
            set => m_PlayAudioClipOnHoverCanceled = value;
        }

        [SerializeField]
        AudioClip m_AudioClipForOnHoverCanceled;
        /// <summary>
        /// The <see cref="AudioClip"/> Unity plays on Hover Canceled.
        /// </summary>
        /// <seealso cref="playAudioClipOnHoverCanceled"/>
        public AudioClip audioClipForOnHoverCanceled
        {
            get => m_AudioClipForOnHoverCanceled;
            set => m_AudioClipForOnHoverCanceled = value;
        }

        [SerializeField, FormerlySerializedAs("m_PlayHapticsOnSelectEnter")]
        bool m_PlayHapticsOnSelectEntered;
        /// <summary>
        /// Controls whether Unity plays haptics on Select Entered.
        /// </summary>
        /// <seealso cref="hapticSelectEnterIntensity"/>
        /// <seealso cref="hapticSelectEnterDuration"/>
        public bool playHapticsOnSelectEntered
        {
            get => m_PlayHapticsOnSelectEntered;
            set => m_PlayHapticsOnSelectEntered = value;
        }

        [SerializeField]
        [Range(0,1)]
        float m_HapticSelectEnterIntensity;
        /// <summary>
        /// The Haptics intensity Unity plays on Select Entered.
        /// </summary>
        /// <seealso cref="hapticSelectEnterDuration"/>
        /// <seealso cref="playHapticsOnSelectEntered"/>
        public float hapticSelectEnterIntensity
        {
            get => m_HapticSelectEnterIntensity;
            set => m_HapticSelectEnterIntensity= value;
        }

        [SerializeField]
        float m_HapticSelectEnterDuration;
        /// <summary>
        /// The Haptics duration (in seconds) Unity plays on Select Entered.
        /// </summary>
        /// <seealso cref="hapticSelectEnterIntensity"/>
        /// <seealso cref="playHapticsOnSelectEntered"/>
        public float hapticSelectEnterDuration
        {
            get => m_HapticSelectEnterDuration;
            set => m_HapticSelectEnterDuration = value;
        }

        [SerializeField, FormerlySerializedAs("m_PlayHapticsOnSelectExit")]
        bool m_PlayHapticsOnSelectExited;
        /// <summary>
        /// Controls whether Unity plays haptics on Select Exited.
        /// </summary>
        /// <seealso cref="hapticSelectExitIntensity"/>
        /// <seealso cref="hapticSelectExitDuration"/>
        public bool playHapticsOnSelectExited
        {
            get => m_PlayHapticsOnSelectExited;
            set => m_PlayHapticsOnSelectExited = value;
        }

        [SerializeField]
        [Range(0,1)]
        float m_HapticSelectExitIntensity;
        /// <summary>
        /// The Haptics intensity Unity plays on Select Exited.
        /// </summary>
        /// <seealso cref="hapticSelectExitDuration"/>
        /// <seealso cref="playHapticsOnSelectExited"/>
        public float hapticSelectExitIntensity
        {
            get => m_HapticSelectExitIntensity;
            set => m_HapticSelectExitIntensity= value;
        }

        [SerializeField]
        float m_HapticSelectExitDuration;
        /// <summary>
        /// The Haptics duration (in seconds) Unity plays on Select Exited.
        /// </summary>
        /// <seealso cref="hapticSelectExitIntensity"/>
        /// <seealso cref="playHapticsOnSelectExited"/>
        public float hapticSelectExitDuration
        {
            get => m_HapticSelectExitDuration;
            set => m_HapticSelectExitDuration = value;
        }

        [SerializeField]
        bool m_PlayHapticsOnSelectCanceled;
        /// <summary>
        /// Controls whether Unity plays haptics on Select Canceled.
        /// </summary>
        /// <seealso cref="hapticSelectCancelIntensity"/>
        /// <seealso cref="hapticSelectCancelDuration"/>
        public bool playHapticsOnSelectCanceled
        {
            get => m_PlayHapticsOnSelectCanceled;
            set => m_PlayHapticsOnSelectCanceled = value;
        }

        [SerializeField]
        [Range(0,1)]
        float m_HapticSelectCancelIntensity;
        /// <summary>
        /// The Haptics intensity Unity plays on Select Canceled.
        /// </summary>
        /// <seealso cref="hapticSelectCancelDuration"/>
        /// <seealso cref="playHapticsOnSelectCanceled"/>
        public float hapticSelectCancelIntensity
        {
            get => m_HapticSelectCancelIntensity;
            set => m_HapticSelectCancelIntensity= value;
        }

        [SerializeField]
        float m_HapticSelectCancelDuration;
        /// <summary>
        /// The Haptics duration (in seconds) Unity plays on Select Canceled.
        /// </summary>
        /// <seealso cref="hapticSelectCancelIntensity"/>
        /// <seealso cref="playHapticsOnSelectCanceled"/>
        public float hapticSelectCancelDuration
        {
            get => m_HapticSelectCancelDuration;
            set => m_HapticSelectCancelDuration = value;
        }

        [SerializeField, FormerlySerializedAs("m_PlayHapticsOnHoverEnter")]
        bool m_PlayHapticsOnHoverEntered;
        /// <summary>
        /// Controls whether Unity plays haptics on Hover Entered.
        /// </summary>
        /// <seealso cref="hapticHoverEnterIntensity"/>
        /// <seealso cref="hapticHoverEnterDuration"/>
        public bool playHapticsOnHoverEntered
        {
            get => m_PlayHapticsOnHoverEntered;
            set => m_PlayHapticsOnHoverEntered = value;
        }

        [SerializeField]
        [Range(0,1)]
        float m_HapticHoverEnterIntensity;
        /// <summary>
        /// The Haptics intensity Unity plays on Hover Entered.
        /// </summary>
        /// <seealso cref="hapticHoverEnterDuration"/>
        /// <seealso cref="playHapticsOnHoverEntered"/>
        public float hapticHoverEnterIntensity
        {
            get => m_HapticHoverEnterIntensity;
            set => m_HapticHoverEnterIntensity = value;
        }

        [SerializeField]
        float m_HapticHoverEnterDuration;
        /// <summary>
        /// The Haptics duration (in seconds) Unity plays on Hover Entered.
        /// </summary>
        /// <seealso cref="hapticHoverEnterIntensity"/>
        /// <seealso cref="playHapticsOnHoverEntered"/>
        public float hapticHoverEnterDuration
        {
            get => m_HapticHoverEnterDuration;
            set => m_HapticHoverEnterDuration = value;
        }

        [SerializeField, FormerlySerializedAs("m_PlayHapticsOnHoverExit")]
        bool m_PlayHapticsOnHoverExited;
        /// <summary>
        /// Controls whether Unity plays haptics on Hover Exited.
        /// </summary>
        /// <seealso cref="hapticHoverExitIntensity"/>
        /// <seealso cref="hapticHoverExitDuration"/>
        public bool playHapticsOnHoverExited
        {
            get => m_PlayHapticsOnHoverExited;
            set => m_PlayHapticsOnHoverExited = value;
        }

        [SerializeField]
        [Range(0,1)]
        float m_HapticHoverExitIntensity;
        /// <summary>
        /// The Haptics intensity Unity plays on Hover Exited.
        /// </summary>
        /// <seealso cref="hapticHoverExitDuration"/>
        /// <seealso cref="playHapticsOnHoverExited"/>
        public float hapticHoverExitIntensity
        {
            get => m_HapticHoverExitIntensity;
            set => m_HapticHoverExitIntensity = value;
        }

        [SerializeField]
        float m_HapticHoverExitDuration;
        /// <summary>
        /// The Haptics duration (in seconds) Unity plays on Hover Exited.
        /// </summary>
        /// <seealso cref="hapticHoverExitIntensity"/>
        /// <seealso cref="playHapticsOnHoverExited"/>
        public float hapticHoverExitDuration
        {
            get => m_HapticHoverExitDuration;
            set => m_HapticHoverExitDuration = value;
        }

        [SerializeField]
        bool m_PlayHapticsOnHoverCanceled;
        /// <summary>
        /// Controls whether Unity plays haptics on Hover Canceled.
        /// </summary>
        /// <seealso cref="hapticHoverCancelIntensity"/>
        /// <seealso cref="hapticHoverCancelDuration"/>
        public bool playHapticsOnHoverCanceled
        {
            get => m_PlayHapticsOnHoverCanceled;
            set => m_PlayHapticsOnHoverCanceled = value;
        }

        [SerializeField]
        [Range(0,1)]
        float m_HapticHoverCancelIntensity;
        /// <summary>
        /// The Haptics intensity Unity plays on Hover Canceled.
        /// </summary>
        /// <seealso cref="hapticHoverCancelDuration"/>
        /// <seealso cref="playHapticsOnHoverCanceled"/>
        public float hapticHoverCancelIntensity
        {
            get => m_HapticHoverCancelIntensity;
            set => m_HapticHoverCancelIntensity= value;
        }

        [SerializeField]
        float m_HapticHoverCancelDuration;
        /// <summary>
        /// The Haptics duration (in seconds) Unity plays on Hover Canceled.
        /// </summary>
        /// <seealso cref="hapticHoverCancelIntensity"/>
        /// <seealso cref="playHapticsOnHoverCanceled"/>
        public float hapticHoverCancelDuration
        {
            get => m_HapticHoverCancelDuration;
            set => m_HapticHoverCancelDuration = value;
        }

        bool m_AllowActivate = true;
        /// <summary>
        /// Defines whether this interactor allows sending activate and deactivate events.
        /// </summary>
        /// <seealso cref="allowHoveredActivate"/>
        /// <seealso cref="shouldActivate"/>
        /// <seealso cref="shouldDeactivate"/>
        public bool allowActivate
        {
            get => m_AllowActivate;
            set => m_AllowActivate = value;
        }

        XRBaseController m_Controller;
        /// <summary>
        /// The controller instance that is queried for input.
        /// </summary>
        public XRBaseController xrController
        {
            get => m_Controller;
            set => m_Controller = value;
        }

        readonly LinkedPool<ActivateEventArgs> m_ActivateEventArgs = new LinkedPool<ActivateEventArgs>(() => new ActivateEventArgs(), collectionCheck: false);
        readonly LinkedPool<DeactivateEventArgs> m_DeactivateEventArgs = new LinkedPool<DeactivateEventArgs>(() => new DeactivateEventArgs(), collectionCheck: false);

        static readonly List<IXRActivateInteractable> s_ActivateTargets = new List<IXRActivateInteractable>();

        bool m_ToggleSelectActive;
        bool m_ToggleSelectDeactivatedThisFrame;
        bool m_WaitingForSelectDeactivate;
        AudioSource m_EffectsAudioSource;

        /// <inheritdoc />
        protected override void Awake()
        {
            base.Awake();

            // Setup interaction controller (for sending down selection state and input)
            m_Controller = GetComponentInParent<XRBaseController>();
            if (m_Controller == null)
                Debug.LogWarning($"Could not find {nameof(XRBaseController)} component on {gameObject} or any of its parents.", this);

            // If we are toggling selection and have a starting object, start out holding it
            if (m_SelectActionTrigger == InputTriggerType.Toggle && startingSelectedInteractable != null)
                m_ToggleSelectActive = true;

            if (m_PlayAudioClipOnSelectEntered || m_PlayAudioClipOnSelectExited || m_PlayAudioClipOnSelectCanceled ||
                m_PlayAudioClipOnHoverEntered || m_PlayAudioClipOnHoverExited || m_PlayAudioClipOnHoverCanceled)
            {
                CreateEffectsAudioSource();
            }
        }

        /// <inheritdoc />
        public override void PreprocessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.PreprocessInteractor(updatePhase);

            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                // Perform toggling of selection state for isSelectActive
                m_ToggleSelectDeactivatedThisFrame = false;
                if (m_SelectActionTrigger == InputTriggerType.Toggle ||
                    m_SelectActionTrigger == InputTriggerType.Sticky)
                {
                    if (m_Controller == null)
                        return;

                    if (m_ToggleSelectActive && m_Controller.selectInteractionState.activatedThisFrame)
                    {
                        m_ToggleSelectActive = false;
                        m_ToggleSelectDeactivatedThisFrame = true;
                        m_WaitingForSelectDeactivate = true;
                    }

                    if (m_Controller.selectInteractionState.deactivatedThisFrame)
                        m_WaitingForSelectDeactivate = false;
                }
            }
        }

        /// <inheritdoc />
        public override void ProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractor(updatePhase);

            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                // Send activate/deactivate events as necessary.
                if (m_AllowActivate)
                {
                    var sendActivate = shouldActivate;
                    var sendDeactivate = shouldDeactivate;
                    if (sendActivate || sendDeactivate)
                    {
                        GetActivateTargets(s_ActivateTargets);

                        if (sendActivate)
                            SendActivateEvent(s_ActivateTargets);

                        // Note that this makes it possible for an interactable to receive an OnDeactivated event
                        // but not the earlier OnActivated event if it was selected afterward.
                        if (sendDeactivate)
                            SendDeactivateEvent(s_ActivateTargets);
                    }
                }
            }
        }

        void SendActivateEvent(List<IXRActivateInteractable> targets)
        {
            foreach (var interactable in targets)
            {
                if (interactable == null || interactable as Object == null)
                    continue;

                using (m_ActivateEventArgs.Get(out var args))
                {
                    args.interactorObject = this;
                    args.interactableObject = interactable;
                    interactable.OnActivated(args);
                }
            }
        }

        void SendDeactivateEvent(List<IXRActivateInteractable> targets)
        {
            foreach (var interactable in targets)
            {
                if (interactable == null || interactable as Object == null)
                    continue;

                using (m_DeactivateEventArgs.Get(out var args))
                {
                    args.interactorObject = this;
                    args.interactableObject = interactable;
                    interactable.OnDeactivated(args);
                }
            }
        }

        /// <inheritdoc />
        public override bool isSelectActive
        {
            get
            {
                if (!base.isSelectActive)
                    return false;

                if (isPerformingManualInteraction)
                    return true;

                switch (m_SelectActionTrigger)
                {
                    case InputTriggerType.State:
                        return m_Controller != null && m_Controller.selectInteractionState.active;

                    case InputTriggerType.StateChange:
                        return (m_Controller != null && m_Controller.selectInteractionState.activatedThisFrame) ||
                            (hasSelection && m_Controller != null && !m_Controller.selectInteractionState.deactivatedThisFrame);

                    case InputTriggerType.Toggle:
                        return m_ToggleSelectActive ||
                            (m_Controller != null && m_Controller.selectInteractionState.activatedThisFrame && !m_ToggleSelectDeactivatedThisFrame);

                    case InputTriggerType.Sticky:
                        return m_ToggleSelectActive || m_WaitingForSelectDeactivate ||
                            (m_Controller != null && m_Controller.selectInteractionState.activatedThisFrame);

                    default:
                        return false;
                }
            }
        }

        /// <summary>
        /// (Read Only) Whether or not Unity considers the UI Press controller input pressed.
        /// </summary>
        /// <returns>Returns <see langword="true"/> if active. Otherwise, returns <see langword="false"/>.</returns>
        protected bool isUISelectActive => m_Controller != null && m_Controller.uiPressInteractionState.active;

        /// <inheritdoc />
        public virtual bool shouldActivate =>
            m_AllowActivate && (hasSelection || m_AllowHoveredActivate && hasHover) && m_Controller != null && m_Controller.activateInteractionState.activatedThisFrame;

        /// <inheritdoc />
        public virtual bool shouldDeactivate =>
            m_AllowActivate && (hasSelection || m_AllowHoveredActivate && hasHover) && m_Controller != null && m_Controller.activateInteractionState.deactivatedThisFrame;

        /// <inheritdoc />
        public virtual void GetActivateTargets(List<IXRActivateInteractable> targets)
        {
            targets.Clear();
            if (hasSelection)
            {
                foreach (var interactable in interactablesSelected)
                {
                    if (interactable is IXRActivateInteractable activateInteractable)
                    {
                        targets.Add(activateInteractable);
                    }
                }
            }
            else if (m_AllowHoveredActivate && hasHover)
            {
                foreach (var interactable in interactablesHovered)
                {
                    if (interactable is IXRActivateInteractable activateInteractable)
                    {
                        targets.Add(activateInteractable);
                    }
                }
            }
        }

        /// <inheritdoc />
        protected override void OnSelectEntering(SelectEnterEventArgs args)
        {
            base.OnSelectEntering(args);

            HandleSelecting();

            if (m_PlayHapticsOnSelectEntered)
                SendHapticImpulse(m_HapticSelectEnterIntensity, m_HapticSelectEnterDuration);

            if (m_PlayAudioClipOnSelectEntered)
                PlayAudio(m_AudioClipForOnSelectEntered);
        }

        /// <inheritdoc />
        protected override void OnSelectExiting(SelectExitEventArgs args)
        {
            base.OnSelectExiting(args);

            HandleDeselecting();

            if (args.isCanceled)
            {
                if (m_PlayHapticsOnSelectCanceled)
                    SendHapticImpulse(m_HapticSelectCancelIntensity, m_HapticSelectCancelDuration);

                if (m_PlayAudioClipOnSelectCanceled)
                    PlayAudio(m_AudioClipForOnSelectCanceled);
            }
            else
            {
                if (m_PlayHapticsOnSelectExited)
                    SendHapticImpulse(m_HapticSelectExitIntensity, m_HapticSelectExitDuration);

                if (m_PlayAudioClipOnSelectExited)
                    PlayAudio(m_AudioClipForOnSelectExited);
            }
        }

        /// <inheritdoc />
        protected override void OnHoverEntering(HoverEnterEventArgs args)
        {
            base.OnHoverEntering(args);

            if (m_PlayHapticsOnHoverEntered)
                SendHapticImpulse(m_HapticHoverEnterIntensity, m_HapticHoverEnterDuration);

            if (m_PlayAudioClipOnHoverEntered)
                PlayAudio(m_AudioClipForOnHoverEntered);
        }

        /// <inheritdoc />
        protected override void OnHoverExiting(HoverExitEventArgs args)
        {
            base.OnHoverExiting(args);

            if (args.isCanceled)
            {
                if (m_PlayHapticsOnHoverCanceled)
                    SendHapticImpulse(m_HapticHoverCancelIntensity, m_HapticHoverCancelDuration);

                if (m_PlayAudioClipOnHoverCanceled)
                    PlayAudio(m_AudioClipForOnHoverCanceled);
            }
            else
            {
                if (m_PlayHapticsOnHoverExited)
                    SendHapticImpulse(m_HapticHoverExitIntensity, m_HapticHoverExitDuration);

                if (m_PlayAudioClipOnHoverExited)
                    PlayAudio(m_AudioClipForOnHoverExited);
            }
        }

        /// <summary>
        /// Play a haptic impulse on the controller if one is available.
        /// </summary>
        /// <param name="amplitude">Amplitude (from 0.0 to 1.0) to play impulse at.</param>
        /// <param name="duration">Duration (in seconds) to play haptic impulse.</param>
        /// <returns>Returns <see langword="true"/> if successful. Otherwise, returns <see langword="false"/>.</returns>
        /// <seealso cref="XRBaseController.SendHapticImpulse"/>
        public bool SendHapticImpulse(float amplitude, float duration)
        {
            return m_Controller != null && m_Controller.SendHapticImpulse(amplitude, duration);
        }

        /// <summary>
        /// Play an <see cref="AudioClip"/>.
        /// </summary>
        /// <param name="audioClip">The clip to play.</param>
        protected virtual void PlayAudio(AudioClip audioClip)
        {
            if (audioClip == null)
                return;

            if (m_EffectsAudioSource == null)
                CreateEffectsAudioSource();

            m_EffectsAudioSource.PlayOneShot(audioClip);
        }

        void CreateEffectsAudioSource()
        {
            m_EffectsAudioSource = gameObject.AddComponent<AudioSource>();
            m_EffectsAudioSource.loop = false;
            m_EffectsAudioSource.playOnAwake = false;
        }

        /// <summary>
        /// Called automatically to handle entering select.
        /// </summary>
        /// <seealso cref="OnSelectEntering"/>
        void HandleSelecting()
        {
            m_ToggleSelectActive = true;
            m_WaitingForSelectDeactivate = false;

            if (m_HideControllerOnSelect && m_Controller != null)
                m_Controller.hideControllerModel = true;
        }

        /// <summary>
        /// Unity calls this automatically to handle exiting select.
        /// </summary>
        /// <seealso cref="OnSelectExiting"/>
        void HandleDeselecting()
        {
            if (hasSelection)
                return;

            // Reset toggle values when no longer selecting
            // (can happen by another Interactor taking the Interactable or through method calls).
            m_ToggleSelectActive = false;
            m_WaitingForSelectDeactivate = false;

            if (m_Controller != null)
                m_Controller.hideControllerModel = false;
        }
    }
}
