using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation
{
    /// <summary>
    /// Button indices for <see cref="XRSimulatedControllerState.buttons"/>
    /// </summary>
    public enum ControllerButton
    {
        /// <summary>
        /// The primary face button being pressed on a device, or sole button if only one is available.
        /// </summary>
        PrimaryButton,

         /// <summary>
        /// The primary face button being touched on a device.
        /// </summary>
        PrimaryTouch,

        /// <summary>
        /// The secondary face button being pressed on a device.
        /// </summary>
        SecondaryButton,

        /// <summary>
        /// The secondary face button being touched on a device.
        /// </summary>
        SecondaryTouch,

        /// <summary>
        /// A binary measure of whether the device is being gripped.
        /// </summary>
        GripButton,

        /// <summary>
        /// A binary measure of whether the index finger is activating the trigger.
        /// </summary>
        TriggerButton,

        /// <summary>
        /// Represents a menu button, used to pause, go back, or otherwise exit gameplay.
        /// </summary>
        MenuButton,

        /// <summary>
        /// Represents the primary 2D axis being clicked or otherwise depressed.
        /// </summary>
        Primary2DAxisClick,

        /// <summary>
        /// Represents the primary 2D axis being touched.
        /// </summary>
        Primary2DAxisTouch,

        /// <summary>
        /// Represents the secondary 2D axis being clicked or otherwise depressed.
        /// </summary>
        Secondary2DAxisClick,

        /// <summary>
        /// Represents the secondary 2D axis being touched.
        /// </summary>
        Secondary2DAxisTouch,

        /// <summary>
        /// Indicates whether the user is present and interacting with the device.
        /// </summary>
        UserPresence,
    }

    /// <summary>
    /// State for input device representing a simulated XR handed controller.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 63)]
    public struct XRSimulatedControllerState : IInputStateTypeInfo
    {
        /// <summary>
        /// Memory format identifier for <see cref="XRSimulatedControllerState"/>.
        /// </summary>
        /// <seealso cref="InputStateBlock.format"/>
        public static FourCC formatId => new FourCC('X', 'R', 'S', 'C');

        /// <summary>
        /// See <a href="https://docs.unity3d.com/Packages/com.unity.inputsystem@1.2/api/UnityEngine.InputSystem.LowLevel.IInputStateTypeInfo.html">IInputStateTypeInfo</a>.format.
        /// </summary>
        public FourCC format => formatId;

        /// <summary>
        /// The primary touchpad or joystick on a device.
        /// </summary>
        [InputControl(usage = "Primary2DAxis", aliases = new[] { "thumbstick", "joystick" }, offset = 0)]
        [FieldOffset(0)]
        public Vector2 primary2DAxis;

        /// <summary>
        /// A trigger-like control, pressed with the index finger.
        /// </summary>
        [InputControl(usage = "Trigger", layout = "Axis", offset = 8)]
        [FieldOffset(8)]
        public float trigger;

        /// <summary>
        /// Represents the user's grip on the controller.
        /// </summary>
        [InputControl(usage = "Grip", layout = "Axis", offset = 12)]
        [FieldOffset(12)]
        public float grip;

        /// <summary>
        /// A secondary touchpad or joystick on a device.
        /// </summary>
        [InputControl(usage = "Secondary2DAxis", offset = 16)]
        [FieldOffset(16)]
        public Vector2 secondary2DAxis;

        /// <summary>
        /// All the buttons on this device.
        /// </summary>
        [InputControl(name = nameof(XRSimulatedController.primaryButton), usage = "PrimaryButton", layout = "Button", bit = (uint)ControllerButton.PrimaryButton, offset = 24)]
        [InputControl(name = nameof(XRSimulatedController.primaryTouch), usage = "PrimaryTouch", layout = "Button", bit = (uint)ControllerButton.PrimaryTouch, offset = 24)]
        [InputControl(name = nameof(XRSimulatedController.secondaryButton), usage = "SecondaryButton", layout = "Button", bit = (uint)ControllerButton.SecondaryButton, offset = 24)]
        [InputControl(name = nameof(XRSimulatedController.secondaryTouch), usage = "SecondaryTouch", layout = "Button", bit = (uint)ControllerButton.SecondaryTouch, offset = 24)]
        [InputControl(name = nameof(XRSimulatedController.gripButton), usage = "GripButton", layout = "Button", bit = (uint)ControllerButton.GripButton, offset = 24, alias = "gripPressed")]
        [InputControl(name = nameof(XRSimulatedController.triggerButton), usage = "TriggerButton", layout = "Button", bit = (uint)ControllerButton.TriggerButton, offset = 24, alias = "triggerPressed")]
        [InputControl(name = nameof(XRSimulatedController.menuButton), usage = "MenuButton", layout = "Button", bit = (uint)ControllerButton.MenuButton, offset = 24)]
        [InputControl(name = nameof(XRSimulatedController.primary2DAxisClick), usage = "Primary2DAxisClick", layout = "Button", bit = (uint)ControllerButton.Primary2DAxisClick, offset = 24)]
        [InputControl(name = nameof(XRSimulatedController.primary2DAxisTouch), usage = "Primary2DAxisTouch", layout = "Button", bit = (uint)ControllerButton.Primary2DAxisTouch, offset = 24)]
        [InputControl(name = nameof(XRSimulatedController.secondary2DAxisClick), usage = "Secondary2DAxisClick", layout = "Button", bit = (uint)ControllerButton.Secondary2DAxisClick, offset = 24)]
        [InputControl(name = nameof(XRSimulatedController.secondary2DAxisTouch), usage = "Secondary2DAxisTouch", layout = "Button", bit = (uint)ControllerButton.Secondary2DAxisTouch, offset = 24)]
        [InputControl(name = nameof(XRSimulatedController.userPresence), usage = "UserPresence", layout = "Button", bit = (uint)ControllerButton.UserPresence, offset = 24)]
        [FieldOffset(24)]
        public ushort buttons;

        /// <summary>
        /// Value representing the current battery life of this device.
        /// </summary>
        [InputControl(usage = "BatteryLevel", layout = "Axis", offset = 26)]
        [FieldOffset(26)]
        public float batteryLevel;

        /// <summary>
        /// Represents the values being tracked for this device.
        /// </summary>
        [InputControl(usage = "TrackingState", layout = "Integer", offset = 30)]
        [FieldOffset(30)]
        public int trackingState;

        /// <summary>
        /// Informs to the developer whether the device is currently being tracked.
        /// </summary>
        [InputControl(usage = "IsTracked", layout = "Button", offset = 34)]
        [FieldOffset(34)]
        public bool isTracked;

        /// <summary>
        /// The position of the device.
        /// </summary>
        [InputControl(usage = "DevicePosition", offset = 35)]
        [FieldOffset(35)]
        public Vector3 devicePosition;

        /// <summary>
        /// The rotation of this device.
        /// </summary>
        [InputControl(usage = "DeviceRotation", offset = 47)]
        [FieldOffset(47)]
        public Quaternion deviceRotation;

        /// <summary>
        /// Set the button mask for the given <paramref name="button"/>.
        /// </summary>
        /// <param name="button">Button whose state to set.</param>
        /// <param name="state">Whether to set the bit on or off.</param>
        /// <returns>The same <see cref="XRSimulatedControllerState"/> with the change applied.</returns>
        /// <seealso cref="buttons"/>
        public XRSimulatedControllerState WithButton(ControllerButton button, bool state = true)
        {
            var bit = 1 << (int)button;
            if (state)
                buttons |= (ushort)bit;
            else
                buttons &= (ushort)~bit;
            return this;
        }

        /// <summary>
        /// Resets the value of all fields to default or the identity rotation.
        /// </summary>
        public void Reset()
        {
            primary2DAxis = default;
            trigger = default;
            grip = default;
            secondary2DAxis = default;
            buttons = default;
            batteryLevel = default;
            trackingState = default;
            isTracked = default;
            devicePosition = default;
            deviceRotation = Quaternion.identity;
        }
    }
}
