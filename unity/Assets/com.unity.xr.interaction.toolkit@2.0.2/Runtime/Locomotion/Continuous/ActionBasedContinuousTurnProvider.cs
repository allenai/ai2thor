using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

namespace UnityEngine.XR.Interaction.Toolkit
{
    /// <summary>
    /// Locomotion provider that allows the user to smoothly rotate their rig continuously over time
    /// using a specified input action.
    /// </summary>
    /// <seealso cref="LocomotionProvider"/>
    /// <seealso cref="ActionBasedSnapTurnProvider"/>
    [AddComponentMenu("XR/Locomotion/Continuous Turn Provider (Action-based)", 11)]
    [HelpURL(XRHelpURLConstants.k_ActionBasedContinuousTurnProvider)]
    public class ActionBasedContinuousTurnProvider : ContinuousTurnProviderBase
    {
        [SerializeField]
        [Tooltip("The Input System Action that will be used to read Turn data from the left hand controller. Must be a Value Vector2 Control.")]
        InputActionProperty m_LeftHandTurnAction;
        /// <summary>
        /// The Input System Action that Unity uses to read Turn data from the left hand controller. Must be a <see cref="InputActionType.Value"/> <see cref="Vector2Control"/> Control.
        /// </summary>
        public InputActionProperty leftHandTurnAction
        {
            get => m_LeftHandTurnAction;
            set => SetInputActionProperty(ref m_LeftHandTurnAction, value);
        }

        [SerializeField]
        [Tooltip("The Input System Action that will be used to read Turn data from the right hand controller. Must be a Value Vector2 Control.")]
        InputActionProperty m_RightHandTurnAction;
        /// <summary>
        /// The Input System Action that Unity uses to read Turn data from the right hand controller. Must be a <see cref="InputActionType.Value"/> <see cref="Vector2Control"/> Control.
        /// </summary>
        public InputActionProperty rightHandTurnAction
        {
            get => m_RightHandTurnAction;
            set => SetInputActionProperty(ref m_RightHandTurnAction, value);
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void OnEnable()
        {
            m_LeftHandTurnAction.EnableDirectAction();
            m_RightHandTurnAction.EnableDirectAction();
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void OnDisable()
        {
            m_LeftHandTurnAction.DisableDirectAction();
            m_RightHandTurnAction.DisableDirectAction();
        }

        /// <inheritdoc />
        protected override Vector2 ReadInput()
        {
            var leftHandValue = m_LeftHandTurnAction.action?.ReadValue<Vector2>() ?? Vector2.zero;
            var rightHandValue = m_RightHandTurnAction.action?.ReadValue<Vector2>() ?? Vector2.zero;

            return leftHandValue + rightHandValue;
        }

        void SetInputActionProperty(ref InputActionProperty property, InputActionProperty value)
        {
            if (Application.isPlaying)
                property.DisableDirectAction();

            property = value;

            if (Application.isPlaying && isActiveAndEnabled)
                property.EnableDirectAction();
        }
    }
}
