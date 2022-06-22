using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.XR.Interaction.Toolkit
{
    /// <summary>
    /// Helper class for different kinds of input.
    /// </summary>
    [MovedFrom("")]
    public static class InputHelpers
    {
        /// <summary>
        /// A list of buttons that can be bound to.
        /// </summary>
        public enum Button
        {
            /// <summary>
            /// Represents and invalid button.
            /// </summary>
            None = 0,

            /// <summary>
            /// Represents a menu button, used to pause, go back, or otherwise exit gameplay.
            /// </summary>
            MenuButton,

            /// <summary>
            /// A binary measure of whether the index finger is activating the trigger.
            /// </summary>
            Trigger,

            /// <summary>
            /// Represents the user's grip on the controller.
            /// </summary>
            Grip,

            /// <summary>
            /// A binary measure of whether the index finger is activating the trigger.
            /// </summary>
            TriggerButton,

            /// <summary>
            /// A binary measure of whether the device is being gripped.
            /// </summary>
            GripButton,

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
            /// Represents the primary 2D axis being touched.
            /// </summary>
            Primary2DAxisTouch,

            /// <summary>
            /// Represents the primary 2D axis being clicked or otherwise depressed.
            /// </summary>
            Primary2DAxisClick,

            /// <summary>
            /// Represents the primary 2D axis being touched.
            /// </summary>
            Secondary2DAxisTouch,

            /// <summary>
            /// Represents the secondary 2D axis being clicked or otherwise depressed.
            /// </summary>
            Secondary2DAxisClick,

            /// <summary>
            /// Represents an upwards motion on the primary touchpad or joystick on a device.
            /// </summary>
            PrimaryAxis2DUp,

            /// <summary>
            /// Represents a downwards motion on the primary touchpad or joystick on a device.
            /// </summary>
            PrimaryAxis2DDown,

            /// <summary>
            /// Represents a leftwards motion on the primary touchpad or joystick on a device.
            /// </summary>
            PrimaryAxis2DLeft,

            /// <summary>
            /// Represents a rightwards motion on the primary touchpad or joystick on a device.
            /// </summary>
            PrimaryAxis2DRight,

            /// <summary>
            /// Represents an upwards motion on the secondary touchpad or joystick on a device.
            /// </summary>
            SecondaryAxis2DUp,

            /// <summary>
            /// Represents a downwards motion on the secondary touchpad or joystick on a device.
            /// </summary>
            SecondaryAxis2DDown,

            /// <summary>
            /// Represents a leftwards motion on the secondary touchpad or joystick on a device.
            /// </summary>
            SecondaryAxis2DLeft,

            /// <summary>
            /// Represents a rightwards motion on the secondary touchpad or joystick on a device.
            /// </summary>
            SecondaryAxis2DRight,

            /// <summary>
            /// A binary measure of whether the index finger is activating the trigger.
            /// </summary>
            /// <remarks>
            /// <c>TriggerPressed</c> has been deprecated. Use <see cref="TriggerButton"/> instead.
            /// </remarks>
            [Obsolete("TriggerPressed has been deprecated. Use TriggerButton instead. (UnityUpgradable) -> TriggerButton")]
            TriggerPressed = TriggerButton,

            /// <summary>
            /// A binary measure of whether the device is being gripped.
            /// </summary>
            /// <remarks>
            /// <c>GripPressed</c> has been deprecated. Use <see cref="GripButton"/> instead.
            /// </remarks>
            [Obsolete("GripPressed has been deprecated. Use GripButton instead. (UnityUpgradable) -> GripButton")]
            GripPressed = GripButton,
        }

        enum ButtonReadType
        {
            None = 0,
            Binary,
            Axis1D,
            Axis2DUp,
            Axis2DDown,
            Axis2DLeft,
            Axis2DRight,
        }

        struct ButtonInfo
        {
            public ButtonInfo(string name, ButtonReadType type)
            {
                this.name = name;
                this.type = type;
            }

            public string name;
            public ButtonReadType type;
        }

        static readonly ButtonInfo[] s_ButtonData =
        {
            new ButtonInfo("", ButtonReadType.None),
            new ButtonInfo("MenuButton", ButtonReadType.Binary),
            new ButtonInfo("Trigger", ButtonReadType.Axis1D),
            new ButtonInfo("Grip", ButtonReadType.Axis1D),
            new ButtonInfo("TriggerButton", ButtonReadType.Binary),
            new ButtonInfo("GripButton", ButtonReadType.Binary),
            new ButtonInfo("PrimaryButton", ButtonReadType.Binary),
            new ButtonInfo("PrimaryTouch", ButtonReadType.Binary),
            new ButtonInfo("SecondaryButton", ButtonReadType.Binary),
            new ButtonInfo("SecondaryTouch", ButtonReadType.Binary),
            new ButtonInfo("Primary2DAxisTouch", ButtonReadType.Binary),
            new ButtonInfo("Primary2DAxisClick", ButtonReadType.Binary),
            new ButtonInfo("Secondary2DAxisTouch", ButtonReadType.Binary),
            new ButtonInfo("Secondary2DAxisClick", ButtonReadType.Binary),
            new ButtonInfo("Primary2DAxis", ButtonReadType.Axis2DUp),
            new ButtonInfo("Primary2DAxis", ButtonReadType.Axis2DDown),
            new ButtonInfo("Primary2DAxis", ButtonReadType.Axis2DLeft),
            new ButtonInfo("Primary2DAxis", ButtonReadType.Axis2DRight),
            new ButtonInfo("Secondary2DAxis", ButtonReadType.Axis2DUp),
            new ButtonInfo("Secondary2DAxis", ButtonReadType.Axis2DDown),
            new ButtonInfo("Secondary2DAxis", ButtonReadType.Axis2DLeft),
            new ButtonInfo("Secondary2DAxis", ButtonReadType.Axis2DRight),
        };

        const float k_DefaultPressThreshold = 0.1f;

        /// <summary>
        /// Checks whether button is pressed or not.
        /// </summary>
        /// <param name="device"> The input device.</param>
        /// <param name="button"> The button that is being checked.</param>
        /// <param name="isPressed"> A boolean that will be true if button is pressed and false if not.</param>
        /// <param name="pressThreshold"> The threshold of what defines a press.</param>
        /// <returns>Returns <see langword="true"/> if device and button are valid. Otherwise, returns <see langword="false"/>.</returns>
        public static bool IsPressed(this InputDevice device, Button button, out bool isPressed, float pressThreshold = -1.0f)
        {
            if ((int)button >= s_ButtonData.Length)
            {
                throw new ArgumentException("[InputHelpers.IsPressed] The value of <button> is out of the supported range.");
            }

            if (!device.isValid)
            {
                isPressed = false;
                return false;
            }

            var info = s_ButtonData[(int)button];
            switch (info.type)
            {
                case ButtonReadType.Binary:
                {
                    if (device.TryGetFeatureValue(new InputFeatureUsage<bool>(info.name), out var value))
                    {
                        isPressed = value;
                        return true;
                    }
                }
                    break;
                case ButtonReadType.Axis1D:
                {
                    if (device.TryGetFeatureValue(new InputFeatureUsage<float>(info.name), out var value))
                    {
                        var threshold = (pressThreshold >= 0f) ? pressThreshold : k_DefaultPressThreshold;
                        isPressed = value >= threshold;
                        return true;
                    }
                }
                    break;
                case ButtonReadType.Axis2DUp:
                {
                    if (device.TryGetFeatureValue(new InputFeatureUsage<Vector2>(info.name), out var value))
                    {
                        var threshold = (pressThreshold >= 0f) ? pressThreshold : k_DefaultPressThreshold;
                        isPressed = value.y >= threshold;
                        return true;
                    }
                }
                    break;
                case ButtonReadType.Axis2DDown:
                {
                    if (device.TryGetFeatureValue(new InputFeatureUsage<Vector2>(info.name), out var value))
                    {
                        var threshold = (pressThreshold >= 0f) ? pressThreshold : k_DefaultPressThreshold;
                        isPressed = value.y <= -threshold;
                        return true;
                    }
                }
                    break;
                case ButtonReadType.Axis2DLeft:
                {
                    if (device.TryGetFeatureValue(new InputFeatureUsage<Vector2>(info.name), out var value))
                    {
                        var threshold = (pressThreshold >= 0f) ? pressThreshold : k_DefaultPressThreshold;
                        isPressed = value.x <= -threshold;
                        return true;
                    }
                }
                    break;
                case ButtonReadType.Axis2DRight:
                {
                    if (device.TryGetFeatureValue(new InputFeatureUsage<Vector2>(info.name), out var value))
                    {
                        var threshold = (pressThreshold >= 0f) ? pressThreshold : k_DefaultPressThreshold;
                        isPressed = value.x >= threshold;
                        return true;
                    }
                }
                    break;
            }

            isPressed = false;
            return false;
        }

        /// <summary>
        /// Tries to read and return the supplied button value.
        /// </summary>
        /// <param name="device">The input device.</param>
        /// <param name="button">The button to read the value.</param>
        /// <param name="singleValue">The button value; it will have the default <see langword="float"/> value (<c>0f</c>) if the button value can't be read.</param>
        /// <returns>Returns <see langword="true"/> if the button value can be read. Otherwise, returns <see langword="false"/></returns>
        public static bool TryReadSingleValue(this InputDevice device, Button button, out float singleValue)
        {
            if ((int)button >= s_ButtonData.Length)
            {
                throw new ArgumentException("[InputHelpers.TryReadSingleValue] The value of <button> is out of the supported range.");
            }

            if (!device.isValid)
            {
                singleValue = default;
                return false;
            }

            var info = s_ButtonData[(int)button];
            switch (info.type)
            {
                case ButtonReadType.Binary:
                {
                    if (device.TryGetFeatureValue(new InputFeatureUsage<bool>(info.name), out var value))
                    {
                        singleValue = value ? 1f : 0f;
                        return true;
                    }
                }
                    break;
                case ButtonReadType.Axis1D:
                {
                    if (device.TryGetFeatureValue(new InputFeatureUsage<float>(info.name), out var value))
                    {
                        singleValue = value;
                        return true;
                    }
                }
                    break;
                case ButtonReadType.Axis2DUp:
                {
                    if (device.TryGetFeatureValue(new InputFeatureUsage<Vector2>(info.name), out var value))
                    {
                        singleValue = value.y;
                        return true;
                    }
                }
                    break;
                case ButtonReadType.Axis2DDown:
                {
                    if (device.TryGetFeatureValue(new InputFeatureUsage<Vector2>(info.name), out var value))
                    {
                        singleValue = -value.y;
                        return true;
                    }
                }
                    break;
                case ButtonReadType.Axis2DLeft:
                {
                    if (device.TryGetFeatureValue(new InputFeatureUsage<Vector2>(info.name), out var value))
                    {
                        singleValue = -value.x;
                        return true;
                    }
                }
                    break;
                case ButtonReadType.Axis2DRight:
                {
                    if (device.TryGetFeatureValue(new InputFeatureUsage<Vector2>(info.name), out var value))
                    {
                        singleValue = value.x;
                        return true;
                    }
                }
                    break;
            }

            singleValue = default;
            return false;
        }
    }
}