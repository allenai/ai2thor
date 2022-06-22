using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.Scripting;

namespace UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation
{
    /// <summary>
    /// An input device representing a simulated XR handed controller.
    /// </summary>
    [InputControlLayout(stateType = typeof(XRSimulatedControllerState), commonUsages = new[] { "LeftHand", "RightHand" }, isGenericTypeOfDevice = false, displayName = "XR Simulated Controller"), Preserve]
    public class XRSimulatedController : InputSystem.XR.XRController
    {
        /// <summary>
        /// The primary touchpad or joystick on a device.
        /// </summary>
        public Vector2Control primary2DAxis { get; private set; }

        /// <summary>
        /// A trigger-like control, pressed with the index finger.
        /// </summary>
        public AxisControl trigger { get; private set; }

        /// <summary>
        /// Represents the users grip on the controller.
        /// </summary>
        public AxisControl grip { get; private set; }

        /// <summary>
        /// A secondary touchpad or joystick on a device.
        /// </summary>
        public Vector2Control secondary2DAxis { get; private set; }

        /// <summary>
        /// The primary face button being pressed on a device, or sole button if only one is available.
        /// </summary>
        public ButtonControl primaryButton { get; private set; }

        /// <summary>
        /// The primary face button being touched on a device.
        /// </summary>
        public ButtonControl primaryTouch { get; private set; }

        /// <summary>
        /// The secondary face button being pressed on a device.
        /// </summary>
        public ButtonControl secondaryButton { get; private set; }

        /// <summary>
        /// The secondary face button being touched on a device.
        /// </summary>
        public ButtonControl secondaryTouch { get; private set; }

        /// <summary>
        /// A binary measure of whether the device is being gripped.
        /// </summary>
        public ButtonControl gripButton { get; private set; }

        /// <summary>
        /// A binary measure of whether the index finger is activating the trigger.
        /// </summary>
        public ButtonControl triggerButton { get; private set; }

        /// <summary>
        /// Represents a menu button, used to pause, go back, or otherwise exit gameplay.
        /// </summary>
        public ButtonControl menuButton { get; private set; }

        /// <summary>
        /// Represents the primary 2D axis being clicked or otherwise depressed.
        /// </summary>
        public ButtonControl primary2DAxisClick { get; private set; }

        /// <summary>
        /// Represents the primary 2D axis being touched.
        /// </summary>
        public ButtonControl primary2DAxisTouch { get; private set; }

        /// <summary>
        /// Represents the secondary 2D axis being clicked or otherwise depressed.
        /// </summary>
        public ButtonControl secondary2DAxisClick { get; private set; }

        /// <summary>
        /// Represents the secondary 2D axis being touched.
        /// </summary>
        public ButtonControl secondary2DAxisTouch { get; private set; }

        /// <summary>
        /// Value representing the current battery life of this device.
        /// </summary>
        public AxisControl batteryLevel { get; private set; }

        /// <summary>
        /// Indicates whether the user is present and interacting with the device.
        /// </summary>
        public ButtonControl userPresence { get; private set; }

        /// <summary>
        /// Finishes setting up all the input values for the controller.
        /// </summary>
        protected override void FinishSetup()
        {
            base.FinishSetup();

            primary2DAxis = GetChildControl<Vector2Control>(nameof(primary2DAxis));
            trigger = GetChildControl<AxisControl>(nameof(trigger));
            grip = GetChildControl<AxisControl>(nameof(grip));
            secondary2DAxis = GetChildControl<Vector2Control>(nameof(secondary2DAxis));
            primaryButton = GetChildControl<ButtonControl>(nameof(primaryButton));
            primaryTouch = GetChildControl<ButtonControl>(nameof(primaryTouch));
            secondaryButton = GetChildControl<ButtonControl>(nameof(secondaryButton));
            secondaryTouch = GetChildControl<ButtonControl>(nameof(secondaryTouch));
            gripButton = GetChildControl<ButtonControl>(nameof(gripButton));
            triggerButton = GetChildControl<ButtonControl>(nameof(triggerButton));
            menuButton = GetChildControl<ButtonControl>(nameof(menuButton));
            primary2DAxisClick = GetChildControl<ButtonControl>(nameof(primary2DAxisClick));
            primary2DAxisTouch = GetChildControl<ButtonControl>(nameof(primary2DAxisTouch));
            secondary2DAxisClick = GetChildControl<ButtonControl>(nameof(secondary2DAxisClick));
            secondary2DAxisTouch = GetChildControl<ButtonControl>(nameof(secondary2DAxisTouch));
            batteryLevel = GetChildControl<AxisControl>(nameof(batteryLevel));
            userPresence = GetChildControl<ButtonControl>(nameof(userPresence));
        }
    }
}
