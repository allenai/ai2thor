using System.Collections.Generic;

namespace UnityEngine.XR.Interaction.Toolkit
{
    /// <summary>
    /// Locomotion provider that allows the user to smoothly rotate their rig continuously over time
    /// using a specified 2D axis input.
    /// </summary>
    /// <seealso cref="LocomotionProvider"/>
    /// <seealso cref="DeviceBasedSnapTurnProvider"/>
    [AddComponentMenu("XR/Locomotion/Continuous Turn Provider (Device-based)", 11)]
    [HelpURL(XRHelpURLConstants.k_DeviceBasedContinuousTurnProvider)]
    public class DeviceBasedContinuousTurnProvider : ContinuousTurnProviderBase
    {
        /// <summary>
        /// Sets which input axis to use when reading from controller input.
        /// </summary>
        /// <seealso cref="inputBinding"/>
        public enum InputAxes
        {
            /// <summary>
            /// Use the primary touchpad or joystick on a device.
            /// </summary>
            Primary2DAxis = 0,
            /// <summary>
            /// Use the secondary touchpad or joystick on a device.
            /// </summary>
            Secondary2DAxis = 1,
        }

        [SerializeField]
        [Tooltip("The 2D Input Axis on the controller devices that will be used to trigger a turn.")]
        InputAxes m_InputBinding = InputAxes.Primary2DAxis;
        /// <summary>
        /// The 2D Input Axis on the controller devices that will be used to trigger a turn.
        /// </summary>
        public InputAxes inputBinding
        {
            get => m_InputBinding;
            set => m_InputBinding = value;
        }

        [SerializeField]
        [Tooltip("A list of controllers that allow Turn.  If an XRController is not enabled, or does not have input actions enabled, turn will not work.")]
        List<XRBaseController> m_Controllers = new List<XRBaseController>();
        /// <summary>
        /// The XRControllers that allow turning. An XRController must be enabled in order to turn.
        /// </summary>
        public List<XRBaseController> controllers
        {
            get => m_Controllers;
            set => m_Controllers = value;
        }

        [SerializeField]
        [Tooltip("Value below which input values will be clamped. After clamping, values will be renormalized to [0, 1] between min and max.")]
        float m_DeadzoneMin = 0.125f;
        /// <summary>
        /// Value below which input values will be clamped. After clamping, values will be renormalized to [0, 1] between min and max.
        /// </summary>
        public float deadzoneMin
        {
            get => m_DeadzoneMin;
            set => m_DeadzoneMin = value;
        }

        [SerializeField]
        [Tooltip("Value above which input values will be clamped. After clamping, values will be renormalized to [0, 1] between min and max.")]
        float m_DeadzoneMax = 0.925f;
        /// <summary>
        /// Value above which input values will be clamped. After clamping, values will be renormalized to [0, 1] between min and max.
        /// </summary>
        public float deadzoneMax
        {
            get => m_DeadzoneMax;
            set => m_DeadzoneMax = value;
        }

        /// <summary>
        /// Mapping of <see cref="InputAxes"/> to actual common usage values.
        /// </summary>
        static readonly InputFeatureUsage<Vector2>[] k_Vec2UsageList =
        {
            CommonUsages.primary2DAxis,
            CommonUsages.secondary2DAxis,
        };

        /// <inheritdoc />
        protected override Vector2 ReadInput()
        {
            if (m_Controllers.Count == 0)
                return Vector2.zero;

            // Accumulate all the controller inputs
            var input = Vector2.zero;
            var feature = k_Vec2UsageList[(int)m_InputBinding];
            for (var i = 0; i < m_Controllers.Count; ++i)
            {
                var controller = m_Controllers[i] as XRController;
                if (controller != null &&
                    controller.enableInputActions &&
                    controller.inputDevice.TryGetFeatureValue(feature, out var controllerInput))
                {
                    input += GetDeadzoneAdjustedValue(controllerInput);
                }
            }

            return input;
        }

        /// <summary>
        /// Gets value adjusted based on deadzone thresholds.
        /// </summary>
        /// <param name="value">The value to be adjusted.</param>
        /// <returns>Returns adjusted 2D vector.</returns>
        protected Vector2 GetDeadzoneAdjustedValue(Vector2 value)
        {
            var magnitude = value.magnitude;
            var newMagnitude = GetDeadzoneAdjustedValue(magnitude);
            if (Mathf.Approximately(newMagnitude, 0f))
                value = Vector2.zero;
            else
                value *= newMagnitude / magnitude;
            return value;
        }

        /// <summary>
        /// Gets value adjusted based on deadzone thresholds.
        /// </summary>
        /// <param name="value">The value to be adjusted.</param>
        /// <returns>Returns adjusted value.</returns>
        protected float GetDeadzoneAdjustedValue(float value)
        {
            var min = m_DeadzoneMin;
            var max = m_DeadzoneMax;

            var absValue = Mathf.Abs(value);
            if (absValue < min)
                return 0f;
            if (absValue > max)
                return Mathf.Sign(value);

            return Mathf.Sign(value) * ((absValue - min) / (max - min));
        }
    }
}
