using System;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.XR;

namespace UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation
{
    /// <summary>
    /// A component which handles mouse and keyboard input from the user and uses it to
    /// drive simulated XR controllers and an XR head mounted display (HMD).
    /// </summary>
    /// <remarks>
    /// This class does not directly manipulate the camera or controllers which are part of
    /// the XR Origin, but rather drives them indirectly through simulated input devices.
    /// <br /><br />
    /// Use the Package Manager window to install the <i>XR Device Simulator</i> sample into
    /// your project to get sample mouse and keyboard bindings for Input System actions that
    /// this component expects. The sample also includes a prefab of a <see cref="GameObject"/>
    /// with this component attached that has references to those sample actions already set.
    /// To make use of this simulator, add the prefab to your scene (the prefab makes use
    /// of <see cref="InputActionManager"/> to ensure the Input System actions are enabled).
    /// <br /><br />
    /// Note that the XR Origin must read the position and rotation of the HMD and controllers
    /// by using Input System actions (such as by using <see cref="ActionBasedController"/>
    /// and <see cref="TrackedPoseDriver"/>) for this simulator to work as expected.
    /// Attempting to use XR input subsystem device methods (such as by using <see cref="XRController"/>
    /// and <see cref="SpatialTracking.TrackedPoseDriver"/>) will not work as expected
    /// since this simulator depends on the Input System to drive the simulated devices.
    /// </remarks>
    /// <seealso cref="XRSimulatedController"/>
    /// <seealso cref="XRSimulatedHMD"/>
    /// <seealso cref="SimulatedInputLayoutLoader"/>
    [AddComponentMenu("XR/Debug/XR Device Simulator", 11)]
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_DeviceSimulator)]
    [HelpURL(XRHelpURLConstants.k_XRDeviceSimulator)]
    public class XRDeviceSimulator : MonoBehaviour
    {
        /// <summary>
        /// The coordinate space in which to operate.
        /// </summary>
        /// <seealso cref="keyboardTranslateSpace"/>
        /// <seealso cref="mouseTranslateSpace"/>
        public enum Space
        {
            /// <summary>
            /// Applies translations of a controller or HMD relative to its own coordinate space, considering its own rotations.
            /// Will translate a controller relative to itself, independent of the camera.
            /// </summary>
            Local,

            /// <summary>
            /// Applies translations of a controller or HMD relative to its parent. If the object does not have a parent, meaning
            /// it is a root object, the parent coordinate space is the same as the world coordinate space. This is the same
            /// as <see cref="Local"/> but without considering its own rotations.
            /// </summary>
            Parent,

            /// <summary>
            /// Applies translations of a controller or HMD relative to the screen.
            /// Will translate a controller relative to the camera, independent of the controller's orientation.
            /// </summary>
            Screen,
        }

        /// <summary>
        /// The transformation mode in which to operate.
        /// </summary>
        /// <seealso cref="mouseTransformationMode"/>
        public enum TransformationMode
        {
            /// <summary>
            /// Applies translations from input.
            /// </summary>
            Translate,

            /// <summary>
            /// Applies rotations from input.
            /// </summary>
            Rotate,
        }

        /// <summary>
        /// The target device control(s) to update from input.
        /// </summary>
        /// <remarks>
        /// <see cref="FlagsAttribute"/> to support updating multiple controls from input
        /// (e.g. to drive the primary and secondary 2D axis on a controller from the same input).
        /// </remarks>
        /// <seealso cref="axis2DTargets"/>
        [Flags]
        public enum Axis2DTargets
        {
            /// <summary>
            /// Do not update device state from input.
            /// </summary>
            None = 0,

            /// <summary>
            /// Update device position from input.
            /// </summary>
            Position  = 1 << 0,

            /// <summary>
            /// Update the primary touchpad or joystick on a controller device from input.
            /// </summary>
            Primary2DAxis   = 1 << 1,

            /// <summary>
            /// Update the secondary touchpad or joystick on a controller device from input.
            /// </summary>
            Secondary2DAxis = 1 << 2,
        }

        [SerializeField]
        [Tooltip("The Input System Action used to translate in the x-axis (left/right) while held. Must be a Value Axis Control.")]
        InputActionReference m_KeyboardXTranslateAction;
        /// <summary>
        /// The Input System Action used to translate in the x-axis (left/right) while held.
        /// Must be a <see cref="InputActionType.Value"/> <see cref="AxisControl"/>.
        /// </summary>
        public InputActionReference keyboardXTranslateAction
        {
            get => m_KeyboardXTranslateAction;
            set
            {
                UnsubscribeKeyboardXTranslateAction();
                m_KeyboardXTranslateAction = value;
                SubscribeKeyboardXTranslateAction();
            }
        }

        [SerializeField]
        [Tooltip("The Input System Action used to translate in the y-axis (up/down) while held. Must be a Value Axis Control.")]
        InputActionReference m_KeyboardYTranslateAction;
        /// <summary>
        /// The Input System Action used to translate in the y-axis (up/down) while held.
        /// Must be a <see cref="InputActionType.Value"/> <see cref="AxisControl"/>.
        /// </summary>
        public InputActionReference keyboardYTranslateAction
        {
            get => m_KeyboardYTranslateAction;
            set
            {
                UnsubscribeKeyboardYTranslateAction();
                m_KeyboardYTranslateAction = value;
                SubscribeKeyboardYTranslateAction();
            }
        }

        [SerializeField]
        [Tooltip("The Input System Action used to translate in the z-axis (forward/back) while held. Must be a Value Axis Control.")]
        InputActionReference m_KeyboardZTranslateAction;
        /// <summary>
        /// The Input System Action used to translate in the z-axis (forward/back) while held.
        /// Must be a <see cref="InputActionType.Value"/> <see cref="AxisControl"/>.
        /// </summary>
        public InputActionReference keyboardZTranslateAction
        {
            get => m_KeyboardZTranslateAction;
            set
            {
                UnsubscribeKeyboardZTranslateAction();
                m_KeyboardZTranslateAction = value;
                SubscribeKeyboardZTranslateAction();
            }
        }

        [SerializeField]
        [Tooltip("The Input System Action used to enable manipulation of the left-hand controller while held. Must be a Button Control.")]
        InputActionReference m_ManipulateLeftAction;
        /// <summary>
        /// The Input System Action used to enable manipulation of the left-hand controller while held.
        /// Must be a <see cref="ButtonControl"/>.
        /// </summary>
        /// <remarks>
        /// Note that if controls on the left-hand controller are actuated when this action is released,
        /// those controls will continue to remain actuated. This is to allow for multi-hand interactions
        /// without needing to have dedicated bindings for manipulating each controller separately and concurrently.
        /// </remarks>
        /// <seealso cref="manipulateRightAction"/>
        /// <seealso cref="toggleManipulateLeftAction"/>
        public InputActionReference manipulateLeftAction
        {
            get => m_ManipulateLeftAction;
            set
            {
                UnsubscribeManipulateLeftAction();
                m_ManipulateLeftAction = value;
                SubscribeManipulateLeftAction();
            }
        }

        [SerializeField]
        [Tooltip("The Input System Action used to enable manipulation of the right-hand controller while held. Must be a Button Control.")]
        InputActionReference m_ManipulateRightAction;
        /// <summary>
        /// The Input System Action used to enable manipulation of the right-hand controller while held.
        /// Must be a <see cref="ButtonControl"/>.
        /// </summary>
        /// <remarks>
        /// Note that if controls on the right-hand controller are actuated when this action is released,
        /// those controls will continue to remain actuated. This is to allow for multi-hand interactions
        /// without needing to have dedicated bindings for manipulating each controller separately and concurrently.
        /// </remarks>
        /// <seealso cref="manipulateLeftAction"/>
        public InputActionReference manipulateRightAction
        {
            get => m_ManipulateRightAction;
            set
            {
                UnsubscribeManipulateRightAction();
                m_ManipulateRightAction = value;
                SubscribeManipulateRightAction();
            }
        }

        [SerializeField]
        [Tooltip("The Input System Action used to toggle enable manipulation of the left-hand controller when pressed. Must be a Button Control.")]
        InputActionReference m_ToggleManipulateLeftAction;
        /// <summary>
        /// The Input System Action used to toggle enable manipulation of the left-hand controller when pressed.
        /// Must be a <see cref="ButtonControl"/>.
        /// </summary>
        /// <seealso cref="manipulateLeftAction"/>
        /// <seealso cref="toggleManipulateRightAction"/>
        public InputActionReference toggleManipulateLeftAction
        {
            get => m_ToggleManipulateLeftAction;
            set
            {
                UnsubscribeToggleManipulateLeftAction();
                m_ToggleManipulateLeftAction = value;
                SubscribeToggleManipulateLeftAction();
            }
        }

        [SerializeField]
        [Tooltip("The Input System Action used to toggle enable manipulation of the right-hand controller when pressed. Must be a Button Control.")]
        InputActionReference m_ToggleManipulateRightAction;
        /// <summary>
        /// The Input System Action used to toggle enable manipulation of the right-hand controller when pressed.
        /// Must be a <see cref="ButtonControl"/>.
        /// </summary>
        /// <seealso cref="manipulateRightAction"/>
        /// <seealso cref="toggleManipulateLeftAction"/>
        public InputActionReference toggleManipulateRightAction
        {
            get => m_ToggleManipulateRightAction;
            set
            {
                UnsubscribeToggleManipulateRightAction();
                m_ToggleManipulateRightAction = value;
                SubscribeToggleManipulateRightAction();
            }
        }

        [SerializeField]
        [Tooltip("The Input System Action used to enable manipulation of the HMD while held. Must be a Button Control.")]
        InputActionReference m_ManipulateHeadAction;
        /// <summary>
        /// The Input System Action used to enable manipulation of the HMD while held.
        /// Must be a <see cref="ButtonControl"/>.
        /// </summary>
        public InputActionReference manipulateHeadAction
        {
            get => m_ManipulateHeadAction;
            set
            {
                UnsubscribeManipulateHeadAction();
                m_ManipulateHeadAction = value;
                SubscribeManipulateHeadAction();
            }
        }

        [SerializeField]
        [Tooltip("The Input System Action used to translate or rotate by a scaled amount along or about the x- and y-axes. Must be a Value Vector2 Control.")]
        InputActionReference m_MouseDeltaAction;
        /// <summary>
        /// The Input System Action used to translate or rotate by a scaled amount along or about the x- and y-axes.
        /// Must be a <see cref="InputActionType.Value"/> <see cref="Vector2Control"/>.
        /// </summary>
        /// <remarks>
        /// Typically bound to the screen-space motion delta of the mouse in pixels.
        /// </remarks>
        /// <seealso cref="mouseScrollAction"/>
        public InputActionReference mouseDeltaAction
        {
            get => m_MouseDeltaAction;
            set
            {
                UnsubscribeMouseDeltaAction();
                m_MouseDeltaAction = value;
                SubscribeMouseDeltaAction();
            }
        }

        [SerializeField]
        [Tooltip("The Input System Action used to translate or rotate by a scaled amount along or about the z-axis. Must be a Value Vector2 Control.")]
        InputActionReference m_MouseScrollAction;
        /// <summary>
        /// The Input System Action used to translate or rotate by a scaled amount along or about the z-axis.
        /// Must be a <see cref="InputActionType.Value"/> <see cref="Vector2Control"/>.
        /// </summary>
        /// <remarks>
        /// Typically bound to the horizontal and vertical scroll wheels, though only the vertical is used.
        /// </remarks>
        /// <seealso cref="mouseDeltaAction"/>
        public InputActionReference mouseScrollAction
        {
            get => m_MouseScrollAction;
            set
            {
                UnsubscribeMouseScrollAction();
                m_MouseScrollAction = value;
                SubscribeMouseScrollAction();
            }
        }

        [SerializeField]
        [Tooltip("The Input System Action used to cause the manipulated device(s) to rotate when moving the mouse when held. Must be a Button Control.")]
        InputActionReference m_RotateModeOverrideAction;
        /// <summary>
        /// The Input System Action used to cause the manipulated device(s) to rotate when moving the mouse when held.
        /// Must be a <see cref="ButtonControl"/>.
        /// </summary>
        /// <remarks>
        /// Forces rotation mode when held, no matter what the current mouse transformation mode is.
        /// </remarks>
        /// <seealso cref="negateModeAction"/>
        public InputActionReference rotateModeOverrideAction
        {
            get => m_RotateModeOverrideAction;
            set
            {
                UnsubscribeRotateModeOverrideAction();
                m_RotateModeOverrideAction = value;
                SubscribeRotateModeOverrideAction();
            }
        }

        [SerializeField]
        [Tooltip("The Input System Action used to toggle between translating or rotating the manipulated device(s) when moving the mouse when pressed. Must be a Button Control.")]
        InputActionReference m_ToggleMouseTransformationModeAction;
        /// <summary>
        /// The Input System Action used to toggle between translating or rotating the manipulated device(s)
        /// when moving the mouse when pressed.
        /// Must be a <see cref="ButtonControl"/>.
        /// </summary>
        public InputActionReference toggleMouseTransformationModeAction
        {
            get => m_ToggleMouseTransformationModeAction;
            set
            {
                UnsubscribeToggleMouseTransformationModeAction();
                m_ToggleMouseTransformationModeAction = value;
                SubscribeToggleMouseTransformationModeAction();
            }
        }

        [SerializeField]
        [Tooltip("The Input System Action used to cause the manipulated device(s) to rotate when moving the mouse while held when it would normally translate, and vice-versa. Must be a Button Control.")]
        InputActionReference m_NegateModeAction;
        /// <summary>
        /// The Input System Action used to cause the manipulated device(s) to rotate when moving the mouse
        /// while held when it would normally translate, and vice-versa.
        /// Must be a <see cref="ButtonControl"/>.
        /// </summary>
        /// <remarks>
        /// Can be used to temporarily change the mouse transformation mode to the other mode while held
        /// for making quick adjustments.
        /// </remarks>
        /// <seealso cref="toggleMouseTransformationModeAction"/>
        public InputActionReference negateModeAction
        {
            get => m_NegateModeAction;
            set
            {
                UnsubscribeNegateModeAction();
                m_NegateModeAction = value;
                SubscribeNegateModeAction();
            }
        }

        [SerializeField]
        [Tooltip("The Input System Action used to constrain the translation or rotation to the x-axis when moving the mouse or resetting. May be combined with another axis constraint to constrain to a plane. Must be a Button Control.")]
        InputActionReference m_XConstraintAction;
        /// <summary>
        /// The Input System Action used to constrain the translation or rotation to the x-axis when moving the mouse or resetting.
        /// May be combined with another axis constraint to constrain to a plane.
        /// Must be a <see cref="ButtonControl"/>.
        /// </summary>
        /// <seealso cref="yConstraintAction"/>
        /// <seealso cref="zConstraintAction"/>
        public InputActionReference xConstraintAction
        {
            get => m_XConstraintAction;
            set
            {
                UnsubscribeXConstraintAction();
                m_XConstraintAction = value;
                SubscribeXConstraintAction();
            }
        }

        [SerializeField]
        [Tooltip("The Input System Action used to constrain the translation or rotation to the y-axis when moving the mouse or resetting. May be combined with another axis constraint to constrain to a plane. Must be a Button Control.")]
        InputActionReference m_YConstraintAction;
        /// <summary>
        /// The Input System Action used to constrain the translation or rotation to the y-axis when moving the mouse or resetting.
        /// May be combined with another axis constraint to constrain to a plane.
        /// Must be a <see cref="ButtonControl"/>.
        /// </summary>
        /// <seealso cref="xConstraintAction"/>
        /// <seealso cref="zConstraintAction"/>
        public InputActionReference yConstraintAction
        {
            get => m_YConstraintAction;
            set
            {
                UnsubscribeYConstraintAction();
                m_YConstraintAction = value;
                SubscribeYConstraintAction();
            }
        }

        [SerializeField]
        [Tooltip("The Input System Action used to constrain the translation or rotation to the z-axis when moving the mouse or resetting. May be combined with another axis constraint to constrain to a plane. Must be a Button Control.")]
        InputActionReference m_ZConstraintAction;
        /// <summary>
        /// The Input System Action used to constrain the translation or rotation to the z-axis when moving the mouse or resetting.
        /// May be combined with another axis constraint to constrain to a plane.
        /// Must be a <see cref="ButtonControl"/>.
        /// </summary>
        /// <seealso cref="xConstraintAction"/>
        /// <seealso cref="yConstraintAction"/>
        public InputActionReference zConstraintAction
        {
            get => m_ZConstraintAction;
            set
            {
                UnsubscribeZConstraintAction();
                m_ZConstraintAction = value;
                SubscribeZConstraintAction();
            }
        }

        [SerializeField]
        [Tooltip("The Input System Action used to cause the manipulated device(s) to reset position or rotation (depending on the effective manipulation mode). Must be a Button Control.")]
        InputActionReference m_ResetAction;
        /// <summary>
        /// The Input System Action used to cause the manipulated device(s) to reset position or rotation
        /// (depending on the effective manipulation mode).
        /// Must be a <see cref="ButtonControl"/>.
        /// </summary>
        /// <remarks>
        /// Resets position to <see cref="Vector3.zero"/> and rotation to <see cref="Quaternion.identity"/>.
        /// May be combined with axis constraints (<see cref="xConstraintAction"/>, <see cref="yConstraintAction"/>, and <see cref="zConstraintAction"/>).
        /// </remarks>
        public InputActionReference resetAction
        {
            get => m_ResetAction;
            set
            {
                UnsubscribeResetAction();
                m_ResetAction = value;
                SubscribeResetAction();
            }
        }

        [SerializeField]
        [Tooltip("The Input System Action used to toggle the cursor lock mode for the game window when pressed. Must be a Button Control.")]
        InputActionReference m_ToggleCursorLockAction;
        /// <summary>
        /// The Input System Action used to toggle the cursor lock mode for the game window when pressed.
        /// Must be a <see cref="ButtonControl"/>.
        /// </summary>
        /// <seealso cref="Cursor.lockState"/>
        /// <seealso cref="desiredCursorLockMode"/>
        public InputActionReference toggleCursorLockAction
        {
            get => m_ToggleCursorLockAction;
            set
            {
                UnsubscribeToggleCursorLockAction();
                m_ToggleCursorLockAction = value;
                SubscribeToggleCursorLockAction();
            }
        }

        [SerializeField]
        [Tooltip("The Input System Action used to toggle enable translation from keyboard inputs when pressed. Must be a Button Control.")]
        InputActionReference m_ToggleDevicePositionTargetAction;
        /// <summary>
        /// The Input System Action used to toggle enable translation from keyboard inputs when pressed.
        /// Must be a <see cref="ButtonControl"/>.
        /// </summary>
        /// <seealso cref="keyboardXTranslateAction"/>
        /// <seealso cref="keyboardYTranslateAction"/>
        /// <seealso cref="keyboardZTranslateAction"/>
        public InputActionReference toggleDevicePositionTargetAction
        {
            get => m_ToggleDevicePositionTargetAction;
            set
            {
                UnsubscribeToggleDevicePositionTargetAction();
                m_ToggleDevicePositionTargetAction = value;
                SubscribeToggleDevicePositionTargetAction();
            }
        }

        [SerializeField]
        [Tooltip("The Input System Action used to toggle enable manipulation of the Primary2DAxis of the controllers when pressed. Must be a Button Control.")]
        InputActionReference m_TogglePrimary2DAxisTargetAction;
        /// <summary>
        /// The Input System action used to toggle enable manipulation of the <see cref="Axis2DTargets.Primary2DAxis"/> of the controllers when pressed.
        /// Must be a <see cref="ButtonControl"/>.
        /// </summary>
        /// <seealso cref="toggleSecondary2DAxisTargetAction"/>
        /// <seealso cref="toggleDevicePositionTargetAction"/>
        /// <seealso cref="axis2DAction"/>
        public InputActionReference togglePrimary2DAxisTargetAction
        {
            get => m_TogglePrimary2DAxisTargetAction;
            set
            {
                UnsubscribeTogglePrimary2DAxisTargetAction();
                m_TogglePrimary2DAxisTargetAction = value;
                SubscribeTogglePrimary2DAxisTargetAction();
            }
        }

        [SerializeField]
        [Tooltip("The Input System Action used to toggle enable manipulation of the Secondary2DAxis of the controllers when pressed. Must be a Button Control.")]
        InputActionReference m_ToggleSecondary2DAxisTargetAction;
        /// <summary>
        /// The Input System action used to toggle enable manipulation of the <see cref="Axis2DTargets.Secondary2DAxis"/> of the controllers when pressed.
        /// Must be a <see cref="ButtonControl"/>.
        /// </summary>
        /// <seealso cref="togglePrimary2DAxisTargetAction"/>
        /// <seealso cref="toggleDevicePositionTargetAction"/>
        /// <seealso cref="axis2DAction"/>
        public InputActionReference toggleSecondary2DAxisTargetAction
        {
            get => m_ToggleSecondary2DAxisTargetAction;
            set
            {
                UnsubscribeToggleSecondary2DAxisTargetAction();
                m_ToggleSecondary2DAxisTargetAction = value;
                SubscribeToggleSecondary2DAxisTargetAction();
            }
        }

        [SerializeField]
        [Tooltip("The Input System Action used to control the value of one or more 2D Axis controls on the manipulated controller device(s). Must be a Value Vector2 Control.")]
        InputActionReference m_Axis2DAction;
        /// <summary>
        /// The Input System Action used to control the value of one or more 2D Axis controls on the manipulated controller device(s).
        /// Must be a <see cref="InputActionType.Value"/> <see cref="Vector2Control"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="togglePrimary2DAxisTargetAction"/> and <see cref="toggleSecondary2DAxisTargetAction"/> toggle enables
        /// the ability to manipulate 2D Axis controls on the simulated controllers, and this <see cref="axis2DAction"/>
        /// actually controls the value of them while those controller devices are being manipulated.
        /// <br />
        /// Typically bound to WASD on a keyboard, and controls the primary and/or secondary 2D Axis controls on them.
        /// </remarks>
        public InputActionReference axis2DAction
        {
            get => m_Axis2DAction;
            set
            {
                UnsubscribeAxis2DAction();
                m_Axis2DAction = value;
                SubscribeAxis2DAction();
            }
        }

        [SerializeField]
        [Tooltip("The Input System Action used to control one or more 2D Axis controls on the opposite hand of the exclusively manipulated controller device. Must be a Value Vector2 Control.")]
        InputActionReference m_RestingHandAxis2DAction;
        /// <summary>
        /// The Input System Action used to control one or more 2D Axis controls on the opposite hand
        /// of the exclusively manipulated controller device.
        /// Must be a <see cref="InputActionType.Value"/> <see cref="Vector2Control"/>.
        /// </summary>
        /// <remarks>
        /// Typically bound to Q and E on a keyboard for the horizontal component, and controls the opposite hand's
        /// 2D Axis controls when manipulating one (and only one) controller. Can be used to quickly and simultaneously
        /// control the 2D Axis on the other hand's controller. In a typical setup of continuous movement bound on the left-hand
        /// controller stick, and turning bound on the right-hand controller stick, while exclusively manipulating the left-hand
        /// controller to move, this action can be used to trigger turning.
        /// </remarks>
        /// <seealso cref="axis2DAction"/>
        public InputActionReference restingHandAxis2DAction
        {
            get => m_RestingHandAxis2DAction;
            set
            {
                UnsubscribeRestingHandAxis2DAction();
                m_RestingHandAxis2DAction = value;
                SubscribeRestingHandAxis2DAction();
            }
        }

        [SerializeField]
        [Tooltip("The Input System Action used to control the Grip control of the manipulated controller device(s). Must be a Button Control.")]
        InputActionReference m_GripAction;
        /// <summary>
        /// The Input System Action used to control the Grip control of the manipulated controller device(s).
        /// Must be a <see cref="ButtonControl"/>.
        /// </summary>
        public InputActionReference gripAction
        {
            get => m_GripAction;
            set
            {
                UnsubscribeGripAction();
                m_GripAction = value;
                SubscribeGripAction();
            }
        }

        [SerializeField]
        [Tooltip("The Input System Action used to control the Trigger control of the manipulated controller device(s). Must be a Button Control.")]
        InputActionReference m_TriggerAction;
        /// <summary>
        /// The Input System Action used to control the Trigger control of the manipulated controller device(s).
        /// Must be a <see cref="ButtonControl"/>.
        /// </summary>
        public InputActionReference triggerAction
        {
            get => m_TriggerAction;
            set
            {
                UnsubscribeTriggerAction();
                m_TriggerAction = value;
                SubscribeTriggerAction();
            }
        }

        [SerializeField]
        [Tooltip("The Input System Action used to control the PrimaryButton control of the manipulated controller device(s). Must be a Button Control.")]
        InputActionReference m_PrimaryButtonAction;
        /// <summary>
        /// The Input System Action used to control the PrimaryButton control of the manipulated controller device(s).
        /// Must be a <see cref="ButtonControl"/>.
        /// </summary>
        public InputActionReference primaryButtonAction
        {
            get => m_PrimaryButtonAction;
            set
            {
                UnsubscribePrimaryButtonAction();
                m_PrimaryButtonAction = value;
                SubscribePrimaryButtonAction();
            }
        }

        [SerializeField]
        [Tooltip("The Input System Action used to control the SecondaryButton control of the manipulated controller device(s). Must be a Button Control.")]
        InputActionReference m_SecondaryButtonAction;
        /// <summary>
        /// The Input System Action used to control the SecondaryButton control of the manipulated controller device(s).
        /// Must be a <see cref="ButtonControl"/>.
        /// </summary>
        public InputActionReference secondaryButtonAction
        {
            get => m_SecondaryButtonAction;
            set
            {
                UnsubscribeSecondaryButtonAction();
                m_SecondaryButtonAction = value;
                SubscribeSecondaryButtonAction();
            }
        }

        [SerializeField]
        [Tooltip("The Input System Action used to control the Menu control of the manipulated controller device(s). Must be a Button Control.")]
        InputActionReference m_MenuAction;
        /// <summary>
        /// The Input System Action used to control the Menu control of the manipulated controller device(s).
        /// Must be a <see cref="ButtonControl"/>.
        /// </summary>
        public InputActionReference menuAction
        {
            get => m_MenuAction;
            set
            {
                UnsubscribeMenuAction();
                m_MenuAction = value;
                SubscribeMenuAction();
            }
        }

        [SerializeField]
        [Tooltip("The Input System Action used to control the Primary2DAxisClick control of the manipulated controller device(s). Must be a Button Control.")]
        InputActionReference m_Primary2DAxisClickAction;
        /// <summary>
        /// The Input System Action used to control the Primary2DAxisClick control of the manipulated controller device(s).
        /// Must be a <see cref="ButtonControl"/>.
        /// </summary>
        public InputActionReference primary2DAxisClickAction
        {
            get => m_Primary2DAxisClickAction;
            set
            {
                UnsubscribePrimary2DAxisClickAction();
                m_Primary2DAxisClickAction = value;
                SubscribePrimary2DAxisClickAction();
            }
        }

        [SerializeField]
        [Tooltip("The Input System Action used to control the Secondary2DAxisClick control of the manipulated controller device(s). Must be a Button Control.")]
        InputActionReference m_Secondary2DAxisClickAction;
        /// <summary>
        /// The Input System Action used to control the Secondary2DAxisClick control of the manipulated controller device(s).
        /// Must be a <see cref="ButtonControl"/>.
        /// </summary>
        public InputActionReference secondary2DAxisClickAction
        {
            get => m_Secondary2DAxisClickAction;
            set
            {
                UnsubscribeSecondary2DAxisClickAction();
                m_Secondary2DAxisClickAction = value;
                SubscribeSecondary2DAxisClickAction();
            }
        }

        [SerializeField]
        [Tooltip("The Input System Action used to control the Primary2DAxisTouch control of the manipulated controller device(s). Must be a Button Control.")]
        InputActionReference m_Primary2DAxisTouchAction;
        /// <summary>
        /// The Input System Action used to control the Primary2DAxisTouch control of the manipulated controller device(s).
        /// Must be a <see cref="ButtonControl"/>.
        /// </summary>
        public InputActionReference primary2DAxisTouchAction
        {
            get => m_Primary2DAxisTouchAction;
            set
            {
                UnsubscribePrimary2DAxisTouchAction();
                m_Primary2DAxisTouchAction = value;
                SubscribePrimary2DAxisTouchAction();
            }
        }

        [SerializeField]
        [Tooltip("The Input System Action used to control the Secondary2DAxisTouch control of the manipulated controller device(s). Must be a Button Control.")]
        InputActionReference m_Secondary2DAxisTouchAction;
        /// <summary>
        /// The Input System Action used to control the Secondary2DAxisTouch control of the manipulated controller device(s).
        /// Must be a <see cref="ButtonControl"/>.
        /// </summary>
        public InputActionReference secondary2DAxisTouchAction
        {
            get => m_Secondary2DAxisTouchAction;
            set
            {
                UnsubscribeSecondary2DAxisTouchAction();
                m_Secondary2DAxisTouchAction = value;
                SubscribeSecondary2DAxisTouchAction();
            }
        }

        [SerializeField]
        [Tooltip("The Input System Action used to control the PrimaryTouch control of the manipulated controller device(s). Must be a Button Control.")]
        InputActionReference m_PrimaryTouchAction;
        /// <summary>
        /// The Input System Action used to control the PrimaryTouch control of the manipulated controller device(s).
        /// Must be a <see cref="ButtonControl"/>.
        /// </summary>
        public InputActionReference primaryTouchAction
        {
            get => m_PrimaryTouchAction;
            set
            {
                UnsubscribePrimaryTouchAction();
                m_PrimaryTouchAction = value;
                SubscribePrimaryTouchAction();
            }
        }

        [SerializeField]
        [Tooltip("The Input System Action used to control the SecondaryTouch control of the manipulated controller device(s). Must be a Button Control.")]
        InputActionReference m_SecondaryTouchAction;
        /// <summary>
        /// The Input System Action used to control the SecondaryTouch control of the manipulated controller device(s).
        /// Must be a <see cref="ButtonControl"/>.
        /// </summary>
        public InputActionReference secondaryTouchAction
        {
            get => m_SecondaryTouchAction;
            set
            {
                UnsubscribeSecondaryTouchAction();
                m_SecondaryTouchAction = value;
                SubscribeSecondaryTouchAction();
            }
        }

        [SerializeField]
        [Tooltip("The Transform that contains the Camera. This is usually the \"Head\" of XR Origins. Automatically set to the first enabled camera tagged MainCamera if unset.")]
        Transform m_CameraTransform;
        /// <summary>
        /// The <see cref="Transform"/> that contains the <see cref="Camera"/>. This is usually the "Head" of XR Origins.
        /// Automatically set to <see cref="Camera.main"/> if unset.
        /// </summary>
        public Transform cameraTransform
        {
            get => m_CameraTransform;
            set => m_CameraTransform = value;
        }

        [SerializeField]
        [Tooltip("The coordinate space in which keyboard translation should operate.")]
        Space m_KeyboardTranslateSpace = Space.Local;
        /// <summary>
        /// The coordinate space in which keyboard translation should operate.
        /// </summary>
        /// <seealso cref="Space"/>
        /// <seealso cref="mouseTranslateSpace"/>
        /// <seealso cref="keyboardXTranslateAction"/>
        /// <seealso cref="keyboardYTranslateAction"/>
        /// <seealso cref="keyboardZTranslateAction"/>
        public Space keyboardTranslateSpace
        {
            get => m_KeyboardTranslateSpace;
            set => m_KeyboardTranslateSpace = value;
        }

        [SerializeField]
        [Tooltip("The coordinate space in which mouse translation should operate.")]
        Space m_MouseTranslateSpace = Space.Screen;
        /// <summary>
        /// The coordinate space in which mouse translation should operate.
        /// </summary>
        /// <seealso cref="Space"/>
        /// <seealso cref="keyboardTranslateSpace"/>
        public Space mouseTranslateSpace
        {
            get => m_MouseTranslateSpace;
            set => m_MouseTranslateSpace = value;
        }

        [SerializeField]
        [Tooltip("Speed of translation in the x-axis (left/right) when triggered by keyboard input.")]
        float m_KeyboardXTranslateSpeed = 0.2f;
        /// <summary>
        /// Speed of translation in the x-axis (left/right) when triggered by keyboard input.
        /// </summary>
        /// <seealso cref="keyboardXTranslateAction"/>
        /// <seealso cref="keyboardYTranslateSpeed"/>
        /// <seealso cref="keyboardZTranslateSpeed"/>
        public float keyboardXTranslateSpeed
        {
            get => m_KeyboardXTranslateSpeed;
            set => m_KeyboardXTranslateSpeed = value;
        }

        [SerializeField]
        [Tooltip("Speed of translation in the y-axis (up/down) when triggered by keyboard input.")]
        float m_KeyboardYTranslateSpeed = 0.2f;
        /// <summary>
        /// Speed of translation in the y-axis (up/down) when triggered by keyboard input.
        /// </summary>
        /// <seealso cref="keyboardYTranslateAction"/>
        /// <seealso cref="keyboardXTranslateSpeed"/>
        /// <seealso cref="keyboardZTranslateSpeed"/>
        public float keyboardYTranslateSpeed
        {
            get => m_KeyboardYTranslateSpeed;
            set => m_KeyboardYTranslateSpeed = value;
        }

        [SerializeField]
        [Tooltip("Speed of translation in the z-axis (forward/back) when triggered by keyboard input.")]
        float m_KeyboardZTranslateSpeed = 0.2f;
        /// <summary>
        /// Speed of translation in the z-axis (forward/back) when triggered by keyboard input.
        /// </summary>
        /// <seealso cref="keyboardZTranslateAction"/>
        /// <seealso cref="keyboardXTranslateSpeed"/>
        /// <seealso cref="keyboardYTranslateSpeed"/>
        public float keyboardZTranslateSpeed
        {
            get => m_KeyboardZTranslateSpeed;
            set => m_KeyboardZTranslateSpeed = value;
        }

        [SerializeField]
        [Tooltip("Sensitivity of translation in the x-axis (left/right) when triggered by mouse input.")]
        float m_MouseXTranslateSensitivity = 0.0004f;
        /// <summary>
        /// Sensitivity of translation in the x-axis (left/right) when triggered by mouse input.
        /// </summary>
        /// <seealso cref="mouseDeltaAction"/>
        /// <seealso cref="mouseYTranslateSensitivity"/>
        /// <seealso cref="mouseScrollTranslateSensitivity"/>
        public float mouseXTranslateSensitivity
        {
            get => m_MouseXTranslateSensitivity;
            set => m_MouseXTranslateSensitivity = value;
        }

        [SerializeField]
        [Tooltip("Sensitivity of translation in the y-axis (up/down) when triggered by mouse input.")]
        float m_MouseYTranslateSensitivity = 0.0004f;
        /// <summary>
        /// Sensitivity of translation in the y-axis (up/down) when triggered by mouse input.
        /// </summary>
        /// <seealso cref="mouseDeltaAction"/>
        /// <seealso cref="mouseXTranslateSensitivity"/>
        /// <seealso cref="mouseScrollTranslateSensitivity"/>
        public float mouseYTranslateSensitivity
        {
            get => m_MouseYTranslateSensitivity;
            set => m_MouseYTranslateSensitivity = value;
        }

        [SerializeField]
        [Tooltip("Sensitivity of translation in the z-axis (forward/back) when triggered by mouse scroll input.")]
        float m_MouseScrollTranslateSensitivity = 0.0002f;
        /// <summary>
        /// Sensitivity of translation in the z-axis (forward/back) when triggered by mouse scroll input.
        /// </summary>
        /// <seealso cref="mouseScrollAction"/>
        /// <seealso cref="mouseXTranslateSensitivity"/>
        /// <seealso cref="mouseYTranslateSensitivity"/>
        public float mouseScrollTranslateSensitivity
        {
            get => m_MouseScrollTranslateSensitivity;
            set => m_MouseScrollTranslateSensitivity = value;
        }

        [SerializeField]
        [Tooltip("Sensitivity of rotation along the x-axis (pitch) when triggered by mouse input.")]
        float m_MouseXRotateSensitivity = 0.1f;
        /// <summary>
        /// Sensitivity of rotation along the x-axis (pitch) when triggered by mouse input.
        /// </summary>
        /// <seealso cref="mouseDeltaAction"/>
        /// <seealso cref="mouseYRotateSensitivity"/>
        /// <seealso cref="mouseScrollRotateSensitivity"/>
        public float mouseXRotateSensitivity
        {
            get => m_MouseXRotateSensitivity;
            set => m_MouseXRotateSensitivity = value;
        }

        [SerializeField]
        [Tooltip("Sensitivity of rotation along the y-axis (yaw) when triggered by mouse input.")]
        float m_MouseYRotateSensitivity = 0.1f;
        /// <summary>
        /// Sensitivity of rotation along the y-axis (yaw) when triggered by mouse input.
        /// </summary>
        /// <seealso cref="mouseDeltaAction"/>
        /// <seealso cref="mouseXRotateSensitivity"/>
        /// <seealso cref="mouseScrollRotateSensitivity"/>
        public float mouseYRotateSensitivity
        {
            get => m_MouseYRotateSensitivity;
            set => m_MouseYRotateSensitivity = value;
        }

        [SerializeField]
        [Tooltip("Sensitivity of rotation along the z-axis (roll) when triggered by mouse scroll input.")]
        float m_MouseScrollRotateSensitivity = 0.05f;
        /// <summary>
        /// Sensitivity of rotation along the z-axis (roll) when triggered by mouse scroll input.
        /// </summary>
        /// <seealso cref="mouseScrollAction"/>
        /// <seealso cref="mouseXRotateSensitivity"/>
        /// <seealso cref="mouseYRotateSensitivity"/>
        public float mouseScrollRotateSensitivity
        {
            get => m_MouseScrollRotateSensitivity;
            set => m_MouseScrollRotateSensitivity = value;
        }

        [SerializeField]
        [Tooltip("A boolean value of whether to invert the y-axis of mouse input when rotating by mouse input." +
            "\nA false value (default) means typical FPS style where moving the mouse up/down pitches up/down." +
            "\nA true value means flight control style where moving the mouse up/down pitches down/up.")]
        bool m_MouseYRotateInvert;
        /// <summary>
        /// A boolean value of whether to invert the y-axis of mouse input when rotating by mouse input.
        /// A <see langword="false"/> value (default) means typical FPS style where moving the mouse up/down pitches up/down.
        /// A <see langword="true"/> value means flight control style where moving the mouse up/down pitches down/up.
        /// </summary>
        public bool mouseYRotateInvert
        {
            get => m_MouseYRotateInvert;
            set => m_MouseYRotateInvert = value;
        }

        [SerializeField]
        [Tooltip("The desired cursor lock mode to toggle to from None (either Locked or Confined).")]
        CursorLockMode m_DesiredCursorLockMode = CursorLockMode.Locked;
        /// <summary>
        /// The desired cursor lock mode to toggle to from <see cref="CursorLockMode.None"/>
        /// (either <see cref="CursorLockMode.Locked"/> or <see cref="CursorLockMode.Confined"/>).
        /// </summary>
        /// <seealso cref="toggleCursorLockAction"/>
        public CursorLockMode desiredCursorLockMode
        {
            get => m_DesiredCursorLockMode;
            set => m_DesiredCursorLockMode = value;
        }

        /// <summary>
        /// The transformation mode in which the mouse should operate.
        /// </summary>
        public TransformationMode mouseTransformationMode { get; set; } = TransformationMode.Translate;

        /// <summary>
        /// One or more 2D Axis controls that keyboard input should apply to (or none).
        /// </summary>
        /// <remarks>
        /// Used to control a combination of the position (<see cref="Axis2DTargets.Position"/>),
        /// primary 2D axis (<see cref="Axis2DTargets.Primary2DAxis"/>), or
        /// secondary 2D axis (<see cref="Axis2DTargets.Secondary2DAxis"/>) of manipulated device(s).
        /// </remarks>
        /// <seealso cref="keyboardXTranslateAction"/>
        /// <seealso cref="keyboardYTranslateAction"/>
        /// <seealso cref="keyboardZTranslateAction"/>
        /// <seealso cref="axis2DAction"/>
        /// <seealso cref="restingHandAxis2DAction"/>
        public Axis2DTargets axis2DTargets { get; set; } = Axis2DTargets.Primary2DAxis;

        float m_KeyboardXTranslateInput;
        float m_KeyboardYTranslateInput;
        float m_KeyboardZTranslateInput;

        bool m_ManipulateLeftInput;
        bool m_ManipulateRightInput;
        bool m_ManipulateHeadInput;

        Vector2 m_MouseDeltaInput;
        Vector2 m_MouseScrollInput;

        bool m_RotateModeOverrideInput;
        bool m_NegateModeInput;

        bool m_XConstraintInput;
        bool m_YConstraintInput;
        bool m_ZConstraintInput;

        bool m_ResetInput;

        Vector2 m_Axis2DInput;
        Vector2 m_RestingHandAxis2DInput;

        bool m_GripInput;
        bool m_TriggerInput;
        bool m_PrimaryButtonInput;
        bool m_SecondaryButtonInput;
        bool m_MenuInput;
        bool m_Primary2DAxisClickInput;
        bool m_Secondary2DAxisClickInput;
        bool m_Primary2DAxisTouchInput;
        bool m_Secondary2DAxisTouchInput;
        bool m_PrimaryTouchInput;
        bool m_SecondaryTouchInput;

        bool m_ManipulatedRestingHandAxis2D;

        Vector3 m_LeftControllerEuler;
        Vector3 m_RightControllerEuler;
        Vector3 m_CenterEyeEuler;

        XRSimulatedHMDState m_HMDState;
        XRSimulatedControllerState m_LeftControllerState;
        XRSimulatedControllerState m_RightControllerState;

        XRSimulatedHMD m_HMDDevice;
        XRSimulatedController m_LeftControllerDevice;
        XRSimulatedController m_RightControllerDevice;

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void Awake()
        {
            m_HMDState.Reset();
            m_LeftControllerState.Reset();
            m_RightControllerState.Reset();
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void OnEnable()
        {
            // Find the Camera if necessary
            if (m_CameraTransform == null)
            {
                var mainCamera = Camera.main;
                if (mainCamera != null)
                    m_CameraTransform = mainCamera.transform;
            }

            AddDevices();

            SubscribeKeyboardXTranslateAction();
            SubscribeKeyboardYTranslateAction();
            SubscribeKeyboardZTranslateAction();
            SubscribeManipulateLeftAction();
            SubscribeManipulateRightAction();
            SubscribeToggleManipulateLeftAction();
            SubscribeToggleManipulateRightAction();
            SubscribeManipulateHeadAction();
            SubscribeMouseDeltaAction();
            SubscribeMouseScrollAction();
            SubscribeRotateModeOverrideAction();
            SubscribeToggleMouseTransformationModeAction();
            SubscribeNegateModeAction();
            SubscribeXConstraintAction();
            SubscribeYConstraintAction();
            SubscribeZConstraintAction();
            SubscribeResetAction();
            SubscribeToggleCursorLockAction();
            SubscribeToggleDevicePositionTargetAction();
            SubscribeTogglePrimary2DAxisTargetAction();
            SubscribeToggleSecondary2DAxisTargetAction();
            SubscribeAxis2DAction();
            SubscribeRestingHandAxis2DAction();
            SubscribeGripAction();
            SubscribeTriggerAction();
            SubscribePrimaryButtonAction();
            SubscribeSecondaryButtonAction();
            SubscribeMenuAction();
            SubscribePrimary2DAxisClickAction();
            SubscribeSecondary2DAxisClickAction();
            SubscribePrimary2DAxisTouchAction();
            SubscribeSecondary2DAxisTouchAction();
            SubscribePrimaryTouchAction();
            SubscribeSecondaryTouchAction();
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void OnDisable()
        {
            RemoveDevices();

            UnsubscribeKeyboardXTranslateAction();
            UnsubscribeKeyboardYTranslateAction();
            UnsubscribeKeyboardZTranslateAction();
            UnsubscribeManipulateLeftAction();
            UnsubscribeManipulateRightAction();
            UnsubscribeToggleManipulateLeftAction();
            UnsubscribeToggleManipulateRightAction();
            UnsubscribeManipulateHeadAction();
            UnsubscribeMouseDeltaAction();
            UnsubscribeMouseScrollAction();
            UnsubscribeRotateModeOverrideAction();
            UnsubscribeToggleMouseTransformationModeAction();
            UnsubscribeNegateModeAction();
            UnsubscribeXConstraintAction();
            UnsubscribeYConstraintAction();
            UnsubscribeZConstraintAction();
            UnsubscribeResetAction();
            UnsubscribeToggleCursorLockAction();
            UnsubscribeToggleDevicePositionTargetAction();
            UnsubscribeTogglePrimary2DAxisTargetAction();
            UnsubscribeToggleSecondary2DAxisTargetAction();
            UnsubscribeAxis2DAction();
            UnsubscribeRestingHandAxis2DAction();
            UnsubscribeGripAction();
            UnsubscribeTriggerAction();
            UnsubscribePrimaryButtonAction();
            UnsubscribeSecondaryButtonAction();
            UnsubscribeMenuAction();
            UnsubscribePrimary2DAxisClickAction();
            UnsubscribeSecondary2DAxisClickAction();
            UnsubscribePrimary2DAxisTouchAction();
            UnsubscribeSecondary2DAxisTouchAction();
            UnsubscribePrimaryTouchAction();
            UnsubscribeSecondaryTouchAction();
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void Update()
        {
            ProcessPoseInput();
            ProcessControlInput();

            if (m_HMDDevice != null)
            {
                InputState.Change(m_HMDDevice, m_HMDState);
            }

            if (m_LeftControllerDevice != null)
            {
                InputState.Change(m_LeftControllerDevice, m_LeftControllerState);
            }

            if (m_RightControllerDevice != null)
            {
                InputState.Change(m_RightControllerDevice, m_RightControllerState);
            }
        }

        /// <summary>
        /// Process input from the user and update the state of manipulated device(s)
        /// related to position and rotation.
        /// </summary>
        protected virtual void ProcessPoseInput()
        {
            // Set tracked states
            m_LeftControllerState.isTracked = true;
            m_RightControllerState.isTracked = true;
            m_HMDState.isTracked = true;
            m_LeftControllerState.trackingState = (int)(InputTrackingState.Position | InputTrackingState.Rotation);
            m_RightControllerState.trackingState = (int)(InputTrackingState.Position | InputTrackingState.Rotation);
            m_HMDState.trackingState = (int)(InputTrackingState.Position | InputTrackingState.Rotation);

            if (!m_ManipulateLeftInput && !m_ManipulateRightInput && !m_ManipulateHeadInput)
                return;

            if (m_CameraTransform == null)
                return;

            var cameraParent = m_CameraTransform.parent;
            var cameraParentRotation = cameraParent != null ? cameraParent.rotation : Quaternion.identity;
            var inverseCameraParentRotation = Quaternion.Inverse(cameraParentRotation);

            if ((axis2DTargets & Axis2DTargets.Position) != 0)
            {
                // Determine frame of reference
                GetAxes(m_KeyboardTranslateSpace, m_CameraTransform, out var right, out var up, out var forward);

                // Keyboard translation
                var deltaPosition =
                    right * (m_KeyboardXTranslateInput * m_KeyboardXTranslateSpeed * Time.deltaTime) +
                    up * (m_KeyboardYTranslateInput * m_KeyboardYTranslateSpeed * Time.deltaTime) +
                    forward * (m_KeyboardZTranslateInput * m_KeyboardZTranslateSpeed * Time.deltaTime);

                if (m_ManipulateLeftInput)
                {
                    var deltaRotation = GetDeltaRotation(m_KeyboardTranslateSpace, m_LeftControllerState, inverseCameraParentRotation);
                    m_LeftControllerState.devicePosition += deltaRotation * deltaPosition;
                }

                if (m_ManipulateRightInput)
                {
                    var deltaRotation = GetDeltaRotation(m_KeyboardTranslateSpace, m_RightControllerState, inverseCameraParentRotation);
                    m_RightControllerState.devicePosition += deltaRotation * deltaPosition;
                }

                if (m_ManipulateHeadInput)
                {
                    var deltaRotation = GetDeltaRotation(m_KeyboardTranslateSpace, m_HMDState, inverseCameraParentRotation);
                    m_HMDState.centerEyePosition += deltaRotation * deltaPosition;
                    m_HMDState.devicePosition = m_HMDState.centerEyePosition;
                }
            }

            if ((mouseTransformationMode == TransformationMode.Translate && !m_RotateModeOverrideInput && !m_NegateModeInput) ||
                (mouseTransformationMode == TransformationMode.Rotate || m_RotateModeOverrideInput) && m_NegateModeInput)
            {
                // Determine frame of reference
                GetAxes(m_MouseTranslateSpace, m_CameraTransform, out var right, out var up, out var forward);

                // Mouse translation
                var scaledMouseDeltaInput =
                    new Vector3(m_MouseDeltaInput.x * m_MouseXTranslateSensitivity,
                        m_MouseDeltaInput.y * m_MouseYTranslateSensitivity,
                        m_MouseScrollInput.y * m_MouseScrollTranslateSensitivity);

                Vector3 deltaPosition;
                if (m_XConstraintInput && !m_YConstraintInput && m_ZConstraintInput) // XZ
                {
                    deltaPosition =
                        right * scaledMouseDeltaInput.x +
                        forward * scaledMouseDeltaInput.y;
                }
                else if (!m_XConstraintInput && m_YConstraintInput && m_ZConstraintInput) // YZ
                {
                    deltaPosition =
                        up * scaledMouseDeltaInput.y +
                        forward * scaledMouseDeltaInput.x;
                }
                else if (m_XConstraintInput && !m_YConstraintInput && !m_ZConstraintInput) // X
                {
                    deltaPosition =
                        right * (scaledMouseDeltaInput.x + scaledMouseDeltaInput.y);
                }
                else if (!m_XConstraintInput && m_YConstraintInput && !m_ZConstraintInput) // Y
                {
                    deltaPosition =
                        up * (scaledMouseDeltaInput.x + scaledMouseDeltaInput.y);
                }
                else if (!m_XConstraintInput && !m_YConstraintInput && m_ZConstraintInput) // Z
                {
                    deltaPosition =
                        forward * (scaledMouseDeltaInput.x + scaledMouseDeltaInput.y);
                }
                else
                {
                    deltaPosition =
                        right * scaledMouseDeltaInput.x +
                        up * scaledMouseDeltaInput.y;
                }

                // Scroll contribution
                deltaPosition +=
                    forward * scaledMouseDeltaInput.z;

                if (m_ManipulateLeftInput)
                {
                    var deltaRotation = GetDeltaRotation(m_MouseTranslateSpace, m_LeftControllerState, inverseCameraParentRotation);
                    m_LeftControllerState.devicePosition += deltaRotation * deltaPosition;
                }

                if (m_ManipulateRightInput)
                {
                    var deltaRotation = GetDeltaRotation(m_MouseTranslateSpace, m_RightControllerState, inverseCameraParentRotation);
                    m_RightControllerState.devicePosition += deltaRotation * deltaPosition;
                }

                if (m_ManipulateHeadInput)
                {
                    var deltaRotation = GetDeltaRotation(m_MouseTranslateSpace, m_HMDState, inverseCameraParentRotation);
                    m_HMDState.centerEyePosition += deltaRotation * deltaPosition;
                    m_HMDState.devicePosition = m_HMDState.centerEyePosition;
                }

                // Reset
                if (m_ResetInput)
                {
                    var resetScale = GetResetScale();

                    if (m_ManipulateLeftInput)
                    {
                        var devicePosition = Vector3.Scale(m_LeftControllerState.devicePosition, resetScale);
                        // The active control for the InputAction will be null while the Action is in waiting at (0, 0, 0)
                        // so use a small value to reset the position to near origin.
                        if (devicePosition.magnitude <= 0f)
                            devicePosition = new Vector3(Mathf.Epsilon, Mathf.Epsilon, Mathf.Epsilon);

                        m_LeftControllerState.devicePosition = devicePosition;
                    }

                    if (m_ManipulateRightInput)
                    {
                        var devicePosition = Vector3.Scale(m_RightControllerState.devicePosition, resetScale);
                        // The active control for the InputAction will be null while the Action is in waiting at (0, 0, 0)
                        // so use a small value to reset the position to near origin.
                        if (devicePosition.magnitude <= 0f)
                            devicePosition = new Vector3(Mathf.Epsilon, Mathf.Epsilon, Mathf.Epsilon);

                        m_RightControllerState.devicePosition = devicePosition;
                    }

                    if (m_ManipulateHeadInput)
                    {
                        // TODO: Tracked Pose Driver (New Input System) has a bug where it only subscribes to
                        // performed and not canceled, so the Transform will not be updated until the magnitude
                        // is considered actuated to trigger a performed event. As a workaround, set to
                        // a small value (enough to be considered actuated) instead of Vector3.zero.
                        var centerEyePosition = Vector3.Scale(m_HMDState.centerEyePosition, resetScale);
                        if (centerEyePosition.magnitude <= 0f)
                            centerEyePosition = new Vector3(Mathf.Epsilon, Mathf.Epsilon, Mathf.Epsilon);

                        m_HMDState.centerEyePosition = centerEyePosition;
                        m_HMDState.devicePosition = m_HMDState.centerEyePosition;
                    }
                }
            }
            else
            {
                // Mouse rotation
                var scaledMouseDeltaInput =
                    new Vector3(m_MouseDeltaInput.x * m_MouseXRotateSensitivity,
                        m_MouseDeltaInput.y * m_MouseYRotateSensitivity * (m_MouseYRotateInvert ? 1f : -1f),
                        m_MouseScrollInput.y * m_MouseScrollRotateSensitivity);

                Vector3 anglesDelta;
                if (m_XConstraintInput && !m_YConstraintInput && m_ZConstraintInput) // XZ
                {
                    anglesDelta = new Vector3(scaledMouseDeltaInput.y, 0f, -scaledMouseDeltaInput.x);
                }
                else if (!m_XConstraintInput && m_YConstraintInput && m_ZConstraintInput) // YZ
                {
                    anglesDelta = new Vector3(0f, scaledMouseDeltaInput.x, -scaledMouseDeltaInput.y);
                }
                else if (m_XConstraintInput && !m_YConstraintInput && !m_ZConstraintInput) // X
                {
                    anglesDelta = new Vector3(-scaledMouseDeltaInput.x + scaledMouseDeltaInput.y, 0f, 0f);
                }
                else if (!m_XConstraintInput && m_YConstraintInput && !m_ZConstraintInput) // Y
                {
                    anglesDelta = new Vector3(0f, scaledMouseDeltaInput.x + -scaledMouseDeltaInput.y, 0f);
                }
                else if (!m_XConstraintInput && !m_YConstraintInput && m_ZConstraintInput) // Z
                {
                    anglesDelta = new Vector3(0f, 0f, -scaledMouseDeltaInput.x + -scaledMouseDeltaInput.y);
                }
                else
                {
                    anglesDelta = new Vector3(scaledMouseDeltaInput.y, scaledMouseDeltaInput.x, 0f);
                }

                // Scroll contribution
                anglesDelta += new Vector3(0f, 0f, scaledMouseDeltaInput.z);

                if (m_ManipulateLeftInput)
                {
                    m_LeftControllerEuler += anglesDelta;
                    m_LeftControllerState.deviceRotation = Quaternion.Euler(m_LeftControllerEuler);
                }

                if (m_ManipulateRightInput)
                {
                    m_RightControllerEuler += anglesDelta;
                    m_RightControllerState.deviceRotation = Quaternion.Euler(m_RightControllerEuler);
                }

                if (m_ManipulateHeadInput)
                {
                    m_CenterEyeEuler += anglesDelta;
                    m_HMDState.centerEyeRotation = Quaternion.Euler(m_CenterEyeEuler);
                    m_HMDState.deviceRotation = m_HMDState.centerEyeRotation;
                }

                // Reset
                if (m_ResetInput)
                {
                    var resetScale = GetResetScale();

                    if (m_ManipulateLeftInput)
                    {
                        m_LeftControllerEuler = Vector3.Scale(m_LeftControllerEuler, resetScale);
                        m_LeftControllerState.deviceRotation = Quaternion.Euler(m_LeftControllerEuler);
                    }

                    if (m_ManipulateRightInput)
                    {
                        m_RightControllerEuler = Vector3.Scale(m_RightControllerEuler, resetScale);
                        m_RightControllerState.deviceRotation = Quaternion.Euler(m_RightControllerEuler);
                    }

                    if (m_ManipulateHeadInput)
                    {
                        m_CenterEyeEuler = Vector3.Scale(m_CenterEyeEuler, resetScale);
                        m_HMDState.centerEyeRotation = Quaternion.Euler(m_CenterEyeEuler);
                        m_HMDState.deviceRotation = m_HMDState.centerEyeRotation;
                    }
                }
            }
        }

        /// <summary>
        /// Process input from the user and update the state of manipulated controller device(s)
        /// related to input controls.
        /// </summary>
        protected virtual void ProcessControlInput()
        {
            ProcessAxis2DControlInput();

            if (m_ManipulateLeftInput)
            {
                ProcessButtonControlInput(ref m_LeftControllerState);
            }

            if (m_ManipulateRightInput)
            {
                ProcessButtonControlInput(ref m_RightControllerState);
            }
        }

        /// <summary>
        /// Process input from the user and update the state of manipulated controller device(s)
        /// related to 2D Axis input controls.
        /// </summary>
        protected virtual void ProcessAxis2DControlInput()
        {
            if (!m_ManipulateLeftInput && !m_ManipulateRightInput)
                return;

            if ((axis2DTargets & Axis2DTargets.Primary2DAxis) != 0)
            {
                if (m_ManipulateLeftInput)
                    m_LeftControllerState.primary2DAxis = m_Axis2DInput;

                if (m_ManipulateRightInput)
                    m_RightControllerState.primary2DAxis = m_Axis2DInput;

                if (m_ManipulateLeftInput ^ m_ManipulateRightInput)
                {
                    if (m_RestingHandAxis2DInput != Vector2.zero || m_ManipulatedRestingHandAxis2D)
                    {
                        if (m_ManipulateLeftInput)
                            m_RightControllerState.primary2DAxis = m_RestingHandAxis2DInput;

                        if (m_ManipulateRightInput)
                            m_LeftControllerState.primary2DAxis = m_RestingHandAxis2DInput;

                        m_ManipulatedRestingHandAxis2D = m_RestingHandAxis2DInput != Vector2.zero;
                    }
                    else
                    {
                        m_ManipulatedRestingHandAxis2D = false;
                    }
                }
            }

            if ((axis2DTargets & Axis2DTargets.Secondary2DAxis) != 0)
            {
                if (m_ManipulateLeftInput)
                    m_LeftControllerState.secondary2DAxis = m_Axis2DInput;

                if (m_ManipulateRightInput)
                    m_RightControllerState.secondary2DAxis = m_Axis2DInput;

                if (m_ManipulateLeftInput ^ m_ManipulateRightInput)
                {
                    if (m_RestingHandAxis2DInput != Vector2.zero || m_ManipulatedRestingHandAxis2D)
                    {
                        if (m_ManipulateLeftInput)
                            m_RightControllerState.secondary2DAxis = m_RestingHandAxis2DInput;

                        if (m_ManipulateRightInput)
                            m_LeftControllerState.secondary2DAxis = m_RestingHandAxis2DInput;

                        m_ManipulatedRestingHandAxis2D = m_RestingHandAxis2DInput != Vector2.zero;
                    }
                    else
                    {
                        m_ManipulatedRestingHandAxis2D = false;
                    }
                }
            }
        }

        /// <summary>
        /// Process input from the user and update the state of manipulated controller device(s)
        /// related to button input controls.
        /// </summary>
        /// <param name="controllerState">The controller state that will be processed.</param>
        protected virtual void ProcessButtonControlInput(ref XRSimulatedControllerState controllerState)
        {
            controllerState.grip = m_GripInput ? 1f : 0f;
            controllerState.WithButton(ControllerButton.GripButton, m_GripInput);
            controllerState.trigger = m_TriggerInput ? 1f : 0f;
            controllerState.WithButton(ControllerButton.TriggerButton, m_TriggerInput);
            controllerState.WithButton(ControllerButton.PrimaryButton, m_PrimaryButtonInput);
            controllerState.WithButton(ControllerButton.SecondaryButton, m_SecondaryButtonInput);
            controllerState.WithButton(ControllerButton.MenuButton, m_MenuInput);
            controllerState.WithButton(ControllerButton.Primary2DAxisClick, m_Primary2DAxisClickInput);
            controllerState.WithButton(ControllerButton.Secondary2DAxisClick, m_Secondary2DAxisClickInput);
            controllerState.WithButton(ControllerButton.Primary2DAxisTouch, m_Primary2DAxisTouchInput);
            controllerState.WithButton(ControllerButton.Secondary2DAxisTouch, m_Secondary2DAxisTouchInput);
            controllerState.WithButton(ControllerButton.PrimaryTouch, m_PrimaryTouchInput);
            controllerState.WithButton(ControllerButton.SecondaryTouch, m_SecondaryTouchInput);
        }

        /// <summary>
        /// Add simulated XR devices to the Input System.
        /// </summary>
        /// <see href="https://docs.unity3d.com/Packages/com.unity.inputsystem@1.2/api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_AddDevice__1_System_String_"/>
        protected virtual void AddDevices()
        {
            m_HMDDevice = InputSystem.InputSystem.AddDevice<XRSimulatedHMD>();
            if (m_HMDDevice == null)
            {
                Debug.LogError($"Failed to create {nameof(XRSimulatedHMD)}.");
            }

            m_LeftControllerDevice = InputSystem.InputSystem.AddDevice<XRSimulatedController>($"{nameof(XRSimulatedController)} - {InputSystem.CommonUsages.LeftHand}");
            if (m_LeftControllerDevice != null)
            {
                InputSystem.InputSystem.SetDeviceUsage(m_LeftControllerDevice, InputSystem.CommonUsages.LeftHand);
            }
            else
            {
                Debug.LogError($"Failed to create {nameof(XRSimulatedController)} for {InputSystem.CommonUsages.LeftHand}.", this);
            }

            m_RightControllerDevice = InputSystem.InputSystem.AddDevice<XRSimulatedController>($"{nameof(XRSimulatedController)} - {InputSystem.CommonUsages.RightHand}");
            if (m_RightControllerDevice != null)
            {
                InputSystem.InputSystem.SetDeviceUsage(m_RightControllerDevice, InputSystem.CommonUsages.RightHand);
            }
            else
            {
                Debug.LogError($"Failed to create {nameof(XRSimulatedController)} for {InputSystem.CommonUsages.RightHand}.", this);
            }
        }

        /// <summary>
        /// Remove simulated XR devices from the Input System.
        /// </summary>
        /// <see href="https://docs.unity3d.com/Packages/com.unity.inputsystem@1.2/api/UnityEngine.InputSystem.InputSystem.html#UnityEngine_InputSystem_InputSystem_RemoveDevice_UnityEngine_InputSystem_InputDevice_"/>
        protected virtual void RemoveDevices()
        {
            if (m_HMDDevice != null && m_HMDDevice.added)
                InputSystem.InputSystem.RemoveDevice(m_HMDDevice);

            if (m_LeftControllerDevice != null && m_LeftControllerDevice.added)
                InputSystem.InputSystem.RemoveDevice(m_LeftControllerDevice);

            if (m_RightControllerDevice != null && m_RightControllerDevice.added)
                InputSystem.InputSystem.RemoveDevice(m_RightControllerDevice);
        }

        /// <summary>
        /// Gets a <see cref="Vector3"/> that can be multiplied component-wise with another <see cref="Vector3"/>
        /// to reset components of the <see cref="Vector3"/>, based on axis constraint inputs.
        /// </summary>
        /// <returns></returns>
        /// <seealso cref="resetAction"/>
        /// <seealso cref="xConstraintAction"/>
        /// <seealso cref="yConstraintAction"/>
        /// <seealso cref="zConstraintAction"/>
        protected Vector3 GetResetScale()
        {
            return m_XConstraintInput || m_YConstraintInput || m_ZConstraintInput
                ? new Vector3(m_XConstraintInput ? 0f : 1f, m_YConstraintInput ? 0f : 1f, m_ZConstraintInput ? 0f : 1f)
                : Vector3.zero;
        }

        static void GetAxes(Space translateSpace, Transform cameraTransform, out Vector3 right, out Vector3 up, out Vector3 forward)
        {
            if (cameraTransform == null)
                throw new ArgumentNullException(nameof(cameraTransform));

            switch (translateSpace)
            {
                case Space.Local:
                    // Makes the assumption that the Camera and the Controllers are siblings
                    // (meaning they share a parent GameObject).
                    var cameraParent = cameraTransform.parent;
                    if (cameraParent != null)
                    {
                        right = cameraParent.TransformDirection(Vector3.right);
                        up = cameraParent.TransformDirection(Vector3.up);
                        forward = cameraParent.TransformDirection(Vector3.forward);
                    }
                    else
                    {
                        right = Vector3.right;
                        up = Vector3.up;
                        forward = Vector3.forward;
                    }

                    break;
                case Space.Parent:
                    right = Vector3.right;
                    up = Vector3.up;
                    forward = Vector3.forward;
                    break;
                case Space.Screen:
                    right = cameraTransform.TransformDirection(Vector3.right);
                    up = cameraTransform.TransformDirection(Vector3.up);
                    forward = cameraTransform.TransformDirection(Vector3.forward);
                    break;
                default:
                    right = Vector3.right;
                    up = Vector3.up;
                    forward = Vector3.forward;
                    Assert.IsTrue(false, $"Unhandled {nameof(translateSpace)}={translateSpace}.");
                    return;
            }
        }

        static Quaternion GetDeltaRotation(Space translateSpace, in XRSimulatedControllerState state, in Quaternion inverseCameraParentRotation)
        {
            switch (translateSpace)
            {
                case Space.Local:
                    return state.deviceRotation * inverseCameraParentRotation;
                case Space.Parent:
                    return Quaternion.identity;
                case Space.Screen:
                    return inverseCameraParentRotation;
                default:
                    Assert.IsTrue(false, $"Unhandled {nameof(translateSpace)}={translateSpace}.");
                    return Quaternion.identity;
            }
        }

        static Quaternion GetDeltaRotation(Space translateSpace, in XRSimulatedHMDState state, in Quaternion inverseCameraParentRotation)
        {
            switch (translateSpace)
            {
                case Space.Local:
                    return state.centerEyeRotation * inverseCameraParentRotation;
                case Space.Parent:
                    return Quaternion.identity;
                case Space.Screen:
                    return inverseCameraParentRotation;
                default:
                    Assert.IsTrue(false, $"Unhandled {nameof(translateSpace)}={translateSpace}.");
                    return Quaternion.identity;
            }
        }

        static void Subscribe(InputActionReference reference, Action<InputAction.CallbackContext> performed = null, Action<InputAction.CallbackContext> canceled = null)
        {
            var action = GetInputAction(reference);
            if (action != null)
            {
                if (performed != null)
                    action.performed += performed;
                if (canceled != null)
                    action.canceled += canceled;
            }
        }

        static void Unsubscribe(InputActionReference reference, Action<InputAction.CallbackContext> performed = null, Action<InputAction.CallbackContext> canceled = null)
        {
            var action = GetInputAction(reference);
            if (action != null)
            {
                if (performed != null)
                    action.performed -= performed;
                if (canceled != null)
                    action.canceled -= canceled;
            }
        }

        static TransformationMode Negate(TransformationMode mode)
        {
            switch (mode)
            {
                case TransformationMode.Rotate:
                    return TransformationMode.Translate;
                case TransformationMode.Translate:
                    return TransformationMode.Rotate;
                default:
                    Assert.IsTrue(false, $"Unhandled {nameof(mode)}={mode}.");
                    return TransformationMode.Rotate;
            }
        }

        CursorLockMode Negate(CursorLockMode mode)
        {
            switch (mode)
            {
                case CursorLockMode.None:
                    return m_DesiredCursorLockMode;
                case CursorLockMode.Locked:
                case CursorLockMode.Confined:
                    return CursorLockMode.None;
                default:
                    Assert.IsTrue(false, $"Unhandled {nameof(mode)}={mode}.");
                    return CursorLockMode.None;
            }
        }

        void SubscribeKeyboardXTranslateAction() => Subscribe(m_KeyboardXTranslateAction, OnKeyboardXTranslatePerformed, OnKeyboardXTranslateCanceled);
        void UnsubscribeKeyboardXTranslateAction() => Unsubscribe(m_KeyboardXTranslateAction, OnKeyboardXTranslatePerformed, OnKeyboardXTranslateCanceled);

        void SubscribeKeyboardYTranslateAction() => Subscribe(m_KeyboardYTranslateAction, OnKeyboardYTranslatePerformed, OnKeyboardYTranslateCanceled);
        void UnsubscribeKeyboardYTranslateAction() => Unsubscribe(m_KeyboardYTranslateAction, OnKeyboardYTranslatePerformed, OnKeyboardYTranslateCanceled);

        void SubscribeKeyboardZTranslateAction() => Subscribe(m_KeyboardZTranslateAction, OnKeyboardZTranslatePerformed, OnKeyboardZTranslateCanceled);
        void UnsubscribeKeyboardZTranslateAction() => Unsubscribe(m_KeyboardZTranslateAction, OnKeyboardZTranslatePerformed, OnKeyboardZTranslateCanceled);

        void SubscribeManipulateLeftAction() => Subscribe(m_ManipulateLeftAction, OnManipulateLeftPerformed, OnManipulateLeftCanceled);
        void UnsubscribeManipulateLeftAction() => Unsubscribe(m_ManipulateLeftAction, OnManipulateLeftPerformed, OnManipulateLeftCanceled);

        void SubscribeManipulateRightAction() => Subscribe(m_ManipulateRightAction, OnManipulateRightPerformed, OnManipulateRightCanceled);
        void UnsubscribeManipulateRightAction() => Unsubscribe(m_ManipulateRightAction, OnManipulateRightPerformed, OnManipulateRightCanceled);

        void SubscribeToggleManipulateLeftAction() => Subscribe(m_ToggleManipulateLeftAction, OnToggleManipulateLeftPerformed);
        void UnsubscribeToggleManipulateLeftAction() => Unsubscribe(m_ToggleManipulateLeftAction, OnToggleManipulateLeftPerformed);

        void SubscribeToggleManipulateRightAction() => Subscribe(m_ToggleManipulateRightAction, OnToggleManipulateRightPerformed);
        void UnsubscribeToggleManipulateRightAction() => Unsubscribe(m_ToggleManipulateRightAction, OnToggleManipulateRightPerformed);

        void SubscribeManipulateHeadAction() => Subscribe(m_ManipulateHeadAction, OnManipulateHeadPerformed, OnManipulateHeadCanceled);
        void UnsubscribeManipulateHeadAction() => Unsubscribe(m_ManipulateHeadAction, OnManipulateHeadPerformed, OnManipulateHeadCanceled);

        void SubscribeMouseDeltaAction() => Subscribe(m_MouseDeltaAction, OnMouseDeltaPerformed, OnMouseDeltaCanceled);
        void UnsubscribeMouseDeltaAction() => Unsubscribe(m_MouseDeltaAction, OnMouseDeltaPerformed, OnMouseDeltaCanceled);

        void SubscribeMouseScrollAction() => Subscribe(m_MouseScrollAction, OnMouseScrollPerformed, OnMouseScrollCanceled);
        void UnsubscribeMouseScrollAction() => Unsubscribe(m_MouseScrollAction, OnMouseScrollPerformed, OnMouseScrollCanceled);

        void SubscribeRotateModeOverrideAction() => Subscribe(m_RotateModeOverrideAction, OnRotateModeOverridePerformed, OnRotateModeOverrideCanceled);
        void UnsubscribeRotateModeOverrideAction() => Unsubscribe(m_RotateModeOverrideAction, OnRotateModeOverridePerformed, OnRotateModeOverrideCanceled);

        void SubscribeToggleMouseTransformationModeAction() => Subscribe(m_ToggleMouseTransformationModeAction, OnToggleMouseTransformationModePerformed);
        void UnsubscribeToggleMouseTransformationModeAction() => Unsubscribe(m_ToggleMouseTransformationModeAction, OnToggleMouseTransformationModePerformed);

        void SubscribeNegateModeAction() => Subscribe(m_NegateModeAction, OnNegateModePerformed, OnNegateModeCanceled);
        void UnsubscribeNegateModeAction() => Unsubscribe(m_NegateModeAction, OnNegateModePerformed, OnNegateModeCanceled);

        void SubscribeXConstraintAction() => Subscribe(m_XConstraintAction, OnXConstraintPerformed, OnXConstraintCanceled);
        void UnsubscribeXConstraintAction() => Unsubscribe(m_XConstraintAction, OnXConstraintPerformed, OnXConstraintCanceled);

        void SubscribeYConstraintAction() => Subscribe(m_YConstraintAction, OnYConstraintPerformed, OnYConstraintCanceled);
        void UnsubscribeYConstraintAction() => Unsubscribe(m_YConstraintAction, OnYConstraintPerformed, OnYConstraintCanceled);

        void SubscribeZConstraintAction() => Subscribe(m_ZConstraintAction, OnZConstraintPerformed, OnZConstraintCanceled);
        void UnsubscribeZConstraintAction() => Unsubscribe(m_ZConstraintAction, OnZConstraintPerformed, OnZConstraintCanceled);

        void SubscribeResetAction() => Subscribe(m_ResetAction, OnResetPerformed, OnResetCanceled);
        void UnsubscribeResetAction() => Unsubscribe(m_ResetAction, OnResetPerformed, OnResetCanceled);

        void SubscribeToggleCursorLockAction() => Subscribe(m_ToggleCursorLockAction, OnToggleCursorLockPerformed);
        void UnsubscribeToggleCursorLockAction() => Unsubscribe(m_ToggleCursorLockAction, OnToggleCursorLockPerformed);

        void SubscribeToggleDevicePositionTargetAction() => Subscribe(m_ToggleDevicePositionTargetAction, OnToggleDevicePositionTargetPerformed);
        void UnsubscribeToggleDevicePositionTargetAction() => Unsubscribe(m_ToggleDevicePositionTargetAction, OnToggleDevicePositionTargetPerformed);

        void SubscribeTogglePrimary2DAxisTargetAction() => Subscribe(m_TogglePrimary2DAxisTargetAction, OnTogglePrimary2DAxisTargetPerformed);
        void UnsubscribeTogglePrimary2DAxisTargetAction() => Unsubscribe(m_TogglePrimary2DAxisTargetAction, OnTogglePrimary2DAxisTargetPerformed);

        void SubscribeToggleSecondary2DAxisTargetAction() => Subscribe(m_ToggleSecondary2DAxisTargetAction, OnToggleSecondary2DAxisTargetPerformed);
        void UnsubscribeToggleSecondary2DAxisTargetAction() => Unsubscribe(m_ToggleSecondary2DAxisTargetAction, OnToggleSecondary2DAxisTargetPerformed);

        void SubscribeAxis2DAction() => Subscribe(m_Axis2DAction, OnAxis2DPerformed, OnAxis2DCanceled);
        void UnsubscribeAxis2DAction() => Unsubscribe(m_Axis2DAction, OnAxis2DPerformed, OnAxis2DCanceled);

        void SubscribeRestingHandAxis2DAction() => Subscribe(m_RestingHandAxis2DAction, OnRestingHandAxis2DPerformed, OnRestingHandAxis2DCanceled);
        void UnsubscribeRestingHandAxis2DAction() => Unsubscribe(m_RestingHandAxis2DAction, OnRestingHandAxis2DPerformed, OnRestingHandAxis2DCanceled);

        void SubscribeGripAction() => Subscribe(m_GripAction, OnGripPerformed, OnGripCanceled);
        void UnsubscribeGripAction() => Unsubscribe(m_GripAction, OnGripPerformed, OnGripCanceled);

        void SubscribeTriggerAction() => Subscribe(m_TriggerAction, OnTriggerPerformed, OnTriggerCanceled);
        void UnsubscribeTriggerAction() => Unsubscribe(m_TriggerAction, OnTriggerPerformed, OnTriggerCanceled);

        void SubscribePrimaryButtonAction() => Subscribe(m_PrimaryButtonAction, OnPrimaryButtonPerformed, OnPrimaryButtonCanceled);
        void UnsubscribePrimaryButtonAction() => Unsubscribe(m_PrimaryButtonAction, OnPrimaryButtonPerformed, OnPrimaryButtonCanceled);

        void SubscribeSecondaryButtonAction() => Subscribe(m_SecondaryButtonAction, OnSecondaryButtonPerformed, OnSecondaryButtonCanceled);
        void UnsubscribeSecondaryButtonAction() => Unsubscribe(m_SecondaryButtonAction, OnSecondaryButtonPerformed, OnSecondaryButtonCanceled);

        void SubscribeMenuAction() => Subscribe(m_MenuAction, OnMenuPerformed, OnMenuCanceled);
        void UnsubscribeMenuAction() => Unsubscribe(m_MenuAction, OnMenuPerformed, OnMenuCanceled);

        void SubscribePrimary2DAxisClickAction() => Subscribe(m_Primary2DAxisClickAction, OnPrimary2DAxisClickPerformed, OnPrimary2DAxisClickCanceled);
        void UnsubscribePrimary2DAxisClickAction() => Unsubscribe(m_Primary2DAxisClickAction, OnPrimary2DAxisClickPerformed, OnPrimary2DAxisClickCanceled);

        void SubscribeSecondary2DAxisClickAction() => Subscribe(m_Secondary2DAxisClickAction, OnSecondary2DAxisClickPerformed, OnSecondary2DAxisClickCanceled);
        void UnsubscribeSecondary2DAxisClickAction() => Unsubscribe(m_Secondary2DAxisClickAction, OnSecondary2DAxisClickPerformed, OnSecondary2DAxisClickCanceled);

        void SubscribePrimary2DAxisTouchAction() => Subscribe(m_Primary2DAxisTouchAction, OnPrimary2DAxisTouchPerformed, OnPrimary2DAxisTouchCanceled);
        void UnsubscribePrimary2DAxisTouchAction() => Unsubscribe(m_Primary2DAxisTouchAction, OnPrimary2DAxisTouchPerformed, OnPrimary2DAxisTouchCanceled);

        void SubscribeSecondary2DAxisTouchAction() => Subscribe(m_Secondary2DAxisTouchAction, OnSecondary2DAxisTouchPerformed, OnSecondary2DAxisTouchCanceled);
        void UnsubscribeSecondary2DAxisTouchAction() => Unsubscribe(m_Secondary2DAxisTouchAction, OnSecondary2DAxisTouchPerformed, OnSecondary2DAxisTouchCanceled);

        void SubscribePrimaryTouchAction() => Subscribe(m_PrimaryTouchAction, OnPrimaryTouchPerformed, OnPrimaryTouchCanceled);
        void UnsubscribePrimaryTouchAction() => Unsubscribe(m_PrimaryTouchAction, OnPrimaryTouchPerformed, OnPrimaryTouchCanceled);

        void SubscribeSecondaryTouchAction() => Subscribe(m_SecondaryTouchAction, OnSecondaryTouchPerformed, OnSecondaryTouchCanceled);
        void UnsubscribeSecondaryTouchAction() => Unsubscribe(m_SecondaryTouchAction, OnSecondaryTouchPerformed, OnSecondaryTouchCanceled);

        void OnKeyboardXTranslatePerformed(InputAction.CallbackContext context) => m_KeyboardXTranslateInput = context.ReadValue<float>();
        void OnKeyboardXTranslateCanceled(InputAction.CallbackContext context) => m_KeyboardXTranslateInput = 0f;

        void OnKeyboardYTranslatePerformed(InputAction.CallbackContext context) => m_KeyboardYTranslateInput = context.ReadValue<float>();
        void OnKeyboardYTranslateCanceled(InputAction.CallbackContext context) => m_KeyboardYTranslateInput = 0f;

        void OnKeyboardZTranslatePerformed(InputAction.CallbackContext context) => m_KeyboardZTranslateInput = context.ReadValue<float>();
        void OnKeyboardZTranslateCanceled(InputAction.CallbackContext context) => m_KeyboardZTranslateInput = 0f;

        void OnManipulateLeftPerformed(InputAction.CallbackContext context) => m_ManipulateLeftInput = true;
        void OnManipulateLeftCanceled(InputAction.CallbackContext context) => m_ManipulateLeftInput = false;

        void OnManipulateRightPerformed(InputAction.CallbackContext context) => m_ManipulateRightInput = true;
        void OnManipulateRightCanceled(InputAction.CallbackContext context) => m_ManipulateRightInput = false;

        void OnToggleManipulateLeftPerformed(InputAction.CallbackContext context) => m_ManipulateLeftInput = !m_ManipulateLeftInput;
        void OnToggleManipulateRightPerformed(InputAction.CallbackContext context) => m_ManipulateRightInput = !m_ManipulateRightInput;

        void OnManipulateHeadPerformed(InputAction.CallbackContext context) => m_ManipulateHeadInput = true;
        void OnManipulateHeadCanceled(InputAction.CallbackContext context) => m_ManipulateHeadInput = false;

        void OnMouseDeltaPerformed(InputAction.CallbackContext context) => m_MouseDeltaInput = context.ReadValue<Vector2>();
        void OnMouseDeltaCanceled(InputAction.CallbackContext context) => m_MouseDeltaInput = Vector2.zero;

        void OnMouseScrollPerformed(InputAction.CallbackContext context) => m_MouseScrollInput = context.ReadValue<Vector2>();
        void OnMouseScrollCanceled(InputAction.CallbackContext context) => m_MouseScrollInput = Vector2.zero;

        void OnRotateModeOverridePerformed(InputAction.CallbackContext context) => m_RotateModeOverrideInput = true;
        void OnRotateModeOverrideCanceled(InputAction.CallbackContext context) => m_RotateModeOverrideInput = false;

        void OnToggleMouseTransformationModePerformed(InputAction.CallbackContext context) => mouseTransformationMode = Negate(mouseTransformationMode);

        void OnNegateModePerformed(InputAction.CallbackContext context) => m_NegateModeInput = true;
        void OnNegateModeCanceled(InputAction.CallbackContext context) => m_NegateModeInput = false;

        void OnXConstraintPerformed(InputAction.CallbackContext context) => m_XConstraintInput = true;
        void OnXConstraintCanceled(InputAction.CallbackContext context) => m_XConstraintInput = false;

        void OnYConstraintPerformed(InputAction.CallbackContext context) => m_YConstraintInput = true;
        void OnYConstraintCanceled(InputAction.CallbackContext context) => m_YConstraintInput = false;

        void OnZConstraintPerformed(InputAction.CallbackContext context) => m_ZConstraintInput = true;
        void OnZConstraintCanceled(InputAction.CallbackContext context) => m_ZConstraintInput = false;

        void OnResetPerformed(InputAction.CallbackContext context) => m_ResetInput = true;
        void OnResetCanceled(InputAction.CallbackContext context) => m_ResetInput = false;

        void OnToggleCursorLockPerformed(InputAction.CallbackContext context) => Cursor.lockState = Negate(Cursor.lockState);

        void OnToggleDevicePositionTargetPerformed(InputAction.CallbackContext context) => axis2DTargets ^= Axis2DTargets.Position;

        void OnTogglePrimary2DAxisTargetPerformed(InputAction.CallbackContext context) => axis2DTargets ^= Axis2DTargets.Primary2DAxis;

        void OnToggleSecondary2DAxisTargetPerformed(InputAction.CallbackContext context) => axis2DTargets ^= Axis2DTargets.Secondary2DAxis;

        void OnAxis2DPerformed(InputAction.CallbackContext context) => m_Axis2DInput = Vector2.ClampMagnitude(context.ReadValue<Vector2>(), 1f);
        void OnAxis2DCanceled(InputAction.CallbackContext context) => m_Axis2DInput = Vector2.zero;

        void OnRestingHandAxis2DPerformed(InputAction.CallbackContext context) => m_RestingHandAxis2DInput = Vector2.ClampMagnitude(context.ReadValue<Vector2>(), 1f);
        void OnRestingHandAxis2DCanceled(InputAction.CallbackContext context) => m_RestingHandAxis2DInput = Vector2.zero;

        void OnGripPerformed(InputAction.CallbackContext context) => m_GripInput = true;
        void OnGripCanceled(InputAction.CallbackContext context) => m_GripInput = false;

        void OnTriggerPerformed(InputAction.CallbackContext context) => m_TriggerInput = true;
        void OnTriggerCanceled(InputAction.CallbackContext context) => m_TriggerInput = false;

        void OnPrimaryButtonPerformed(InputAction.CallbackContext context) => m_PrimaryButtonInput = true;
        void OnPrimaryButtonCanceled(InputAction.CallbackContext context) => m_PrimaryButtonInput = false;

        void OnSecondaryButtonPerformed(InputAction.CallbackContext context) => m_SecondaryButtonInput = true;
        void OnSecondaryButtonCanceled(InputAction.CallbackContext context) => m_SecondaryButtonInput = false;

        void OnMenuPerformed(InputAction.CallbackContext context) => m_MenuInput = true;
        void OnMenuCanceled(InputAction.CallbackContext context) => m_MenuInput = false;

        void OnPrimary2DAxisClickPerformed(InputAction.CallbackContext context) => m_Primary2DAxisClickInput = true;
        void OnPrimary2DAxisClickCanceled(InputAction.CallbackContext context) => m_Primary2DAxisClickInput = false;

        void OnSecondary2DAxisClickPerformed(InputAction.CallbackContext context) => m_Secondary2DAxisClickInput = true;
        void OnSecondary2DAxisClickCanceled(InputAction.CallbackContext context) => m_Secondary2DAxisClickInput = false;

        void OnPrimary2DAxisTouchPerformed(InputAction.CallbackContext context) => m_Primary2DAxisTouchInput = true;
        void OnPrimary2DAxisTouchCanceled(InputAction.CallbackContext context) => m_Primary2DAxisTouchInput = false;

        void OnSecondary2DAxisTouchPerformed(InputAction.CallbackContext context) => m_Secondary2DAxisTouchInput = true;
        void OnSecondary2DAxisTouchCanceled(InputAction.CallbackContext context) => m_Secondary2DAxisTouchInput = false;

        void OnPrimaryTouchPerformed(InputAction.CallbackContext context) => m_PrimaryTouchInput = true;
        void OnPrimaryTouchCanceled(InputAction.CallbackContext context) => m_PrimaryTouchInput = false;

        void OnSecondaryTouchPerformed(InputAction.CallbackContext context) => m_SecondaryTouchInput = true;
        void OnSecondaryTouchCanceled(InputAction.CallbackContext context) => m_SecondaryTouchInput = false;

        static InputAction GetInputAction(InputActionReference actionReference)
        {
#pragma warning disable IDE0031 // Use null propagation -- Do not use for UnityEngine.Object types
            return actionReference != null ? actionReference.action : null;
#pragma warning restore IDE0031
        }
    }
}
