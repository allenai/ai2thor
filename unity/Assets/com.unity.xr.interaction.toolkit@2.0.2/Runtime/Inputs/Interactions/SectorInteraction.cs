using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
#endif
using UnityEngine.Scripting;

namespace UnityEngine.XR.Interaction.Toolkit.Inputs.Interactions
{
    /// <summary>
    /// Interaction that performs the action if the user pushes the control into a slice of a circle along cardinal directions,
    /// with a deadzone center magnitude. Typically used to define actions for North, South, East, or West for a thumbstick.
    /// </summary>
    /// <remarks>
    /// The interaction performs the action if the control crosses the center threshold defined by <see cref="pressPoint"/>
    /// and it is in a direction matching <see cref="directions"/>. Whether the action cancels or performs when leaving or
    /// re-entering each direction sector depends on the configured <see cref="sweepBehavior"/>. Once the control returns
    /// to center as defined by the center threshold, the action cancels.
    /// </remarks>
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    [Preserve]
    public class SectorInteraction : IInputInteraction<Vector2>
    {
        /// <summary>
        /// Sets which cardinal directions to use when determining valid directions to perform the action.
        /// </summary>
        /// <seealso cref="directions"/>
        [Flags]
        public enum Directions
        {
            /// <summary>
            /// Do not include any direction, e.g. the center deadzone region of a thumbstick.
            /// The action will never perform.
            /// </summary>
            None  = 0,

            /// <summary>
            /// Include North direction, e.g. forward on a thumbstick.
            /// </summary>
            North = 1 << 0,

            /// <summary>
            /// Include South direction, e.g. back on a thumbstick.
            /// </summary>
            South = 1 << 1,

            /// <summary>
            /// Include East direction, e.g. right on a thumbstick.
            /// </summary>
            East  = 1 << 2,

            /// <summary>
            /// Include West direction, e.g. left on a thumbstick.
            /// </summary>
            West  = 1 << 3,
        }

        /// <summary>
        /// Sets which strategy to use when sweeping the stick around the cardinal directions
        /// without returning to center for whether the action should perform or cancel.
        /// </summary>
        /// <seealso cref="sweepBehavior"/>
        public enum SweepBehavior
        {
            /// <summary>
            /// Perform if initially actuated in a configured valid direction, and will remain so
            /// (i.e. even if no longer actuating in a valid direction)
            /// until returning to center, at which point it will cancel.
            /// </summary>
            Locked,

            /// <summary>
            /// Perform if initially actuated in a configured valid direction. Cancels when
            /// no longer actuating in a valid direction, and performs again when re-entering a valid sector
            /// even when not returning to center.
            /// </summary>
            AllowReentry,

            /// <summary>
            /// Perform if initially actuated in a configured valid direction. Cancels when
            /// no longer actuating in a valid direction, and remains so when re-entering a valid sector
            /// without returning to center.
            /// </summary>
            DisallowReentry,

            /// <summary>
            /// Perform if actuated in a configured valid direction, no matter the initial actuation direction.
            /// Cancels when not actuated in a valid direction.
            /// </summary>
            HistoryIndependent,
        }

        /// <summary>
        /// Sets which state this <see cref="SectorInteraction"/> is in.
        /// </summary>
        /// <seealso cref="m_State"/>
        enum State
        {
            /// <summary>
            /// Input control is in the center deadzone region.
            /// </summary>
            Centered,

            /// <summary>
            /// The initial latched direction was one of the configured valid directions.
            /// </summary>
            StartedValidDirection,

            /// <summary>
            /// The initial latched direction was not one of the configured valid directions.
            /// </summary>
            StartedInvalidDirection,
        }

        /// <summary>
        /// Specifies cardinal direction(s) that, if the user moves the control in
        /// any of those directions enough to cross the press threshold, the action
        /// should be performed.
        /// </summary>
        public Directions directions;

        /// <summary>
        /// Determines when the action should perform or cancel when sweeping the stick around
        /// the cardinal directions without returning to center.
        /// </summary>
        public SweepBehavior sweepBehavior;

        /// <summary>
        /// Magnitude threshold that an actuated control must cross for the control to
        /// be considered pressed.
        /// </summary>
        /// <remarks>
        /// If this is less than or equal to 0 (the default), <see cref="defaultPressPoint"/> is used instead.
        /// </remarks>
        /// <seealso cref="InputControl.EvaluateMagnitude()"/>
        public float pressPoint = -1f;

        internal float pressPointOrDefault => pressPoint >= 0f ? pressPoint : defaultPressPoint;

        /// <summary>
        /// The default magnitude threshold that an actuated control must cross for the control to
        /// be considered pressed.
        /// </summary>
        public static float defaultPressPoint { get; set; } = 0.5f;

        State m_State;

        bool m_WasValidDirection;

        /// <summary>
        /// See <see cref="IInputInteraction.Process(ref InputInteractionContext)"/>
        /// </summary>
        /// <param name="context">Context for an interaction occuring that the input system passed here for interaction operations.</param>
        public void Process(ref InputInteractionContext context)
        {
            var isActuated = context.ControlIsActuated(pressPointOrDefault);

            if (!isActuated)
            {
                switch (m_State)
                {
                    case State.Centered:
                        return;
                    case State.StartedInvalidDirection:
                    case State.StartedValidDirection:
                        m_State = State.Centered;
                        context.Canceled();
                        return;
                    default:
                        Assert.IsTrue(false, $"Unhandled {nameof(State)}={m_State}");
                        return;
                }
            }

            var isValidDirection = IsValidDirection(ref context);

            if (m_State == State.Centered)
            {
                m_State = isValidDirection ? State.StartedValidDirection : State.StartedInvalidDirection;
                if (isValidDirection)
                {
                    context.PerformedAndStayPerformed();
                }
                m_WasValidDirection = isValidDirection;

                return;
            }

            switch (sweepBehavior)
            {
                case SweepBehavior.Locked:
                    break;
                case SweepBehavior.AllowReentry:
                    if (m_WasValidDirection && !isValidDirection && m_State == State.StartedValidDirection)
                    {
                        context.Canceled();
                    }
                    else if (!m_WasValidDirection && isValidDirection && m_State == State.StartedValidDirection)
                    {
                        context.PerformedAndStayPerformed();
                    }

                    break;
                case SweepBehavior.DisallowReentry:
                    if (m_WasValidDirection && !isValidDirection && m_State == State.StartedValidDirection)
                    {
                        context.Canceled();
                    }

                    break;
                case SweepBehavior.HistoryIndependent:
                    if (m_WasValidDirection && !isValidDirection)
                    {
                        context.Canceled();
                    }
                    else if (!m_WasValidDirection && isValidDirection)
                    {
                        context.PerformedAndStayPerformed();
                    }

                    break;
                default:
                    Assert.IsTrue(false, $"Unhandled {nameof(SweepBehavior)}={sweepBehavior}");
                    break;
            }

            m_WasValidDirection = isValidDirection;
        }

        /// <summary>
        /// Determines whether the control is pointing towards the configured North, South, East, or West direction(s).
        /// </summary>
        /// <param name="context">The input control information context.</param>
        /// <returns>Returns <see langword="true"/> if pointing in "this" direction. Otherwise, returns <see langword="false"/>.</returns>
        bool IsValidDirection(ref InputInteractionContext context)
        {
            var value = context.ReadValue<Vector2>();
            var cardinal = CardinalUtility.GetNearestCardinal(value);
            var nearestDirection = GetNearestDirection(cardinal);

            return (nearestDirection & directions) != 0;
        }

        static Directions GetNearestDirection(Cardinal value)
        {
            switch (value)
            {
                case Cardinal.North:
                    return Directions.North;
                case Cardinal.South:
                    return Directions.South;
                case Cardinal.East:
                    return Directions.East;
                case Cardinal.West:
                    return Directions.West;
                default:
                    Assert.IsTrue(false, $"Unhandled {nameof(Cardinal)}={value}");
                    return Directions.None;
            }
        }

        /// <summary>
        /// See [IInputInteraction.Reset](xref:UnityEngine.InputSystem.IInputInteraction.Reset).
        /// </summary>
        public void Reset()
        {
            // Do not reset, only do so when no longer actuating.
            // We cancel when the stick moves outside the sector, but the latched sector
            // needs to still be set to achieve the desired sweep behavior, so it can't
            // be reset to default.
        }

        [Preserve]
        static SectorInteraction()
        {
            InputSystem.InputSystem.RegisterInteraction<SectorInteraction>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad), Preserve]
#pragma warning disable IDE0051 // Remove unused private members
        static void Initialize()
#pragma warning restore IDE0051 // Remove unused private members
        {
            // Will execute the static constructor as a side effect.
        }
    }

#if UNITY_EDITOR
    class SectorInteractionEditor : InputParameterEditor<SectorInteraction>
    {
        readonly GUIContent m_DirectionsLabel = EditorGUIUtility.TrTextContent("Directions",
            "Sets which cardinal directions to use when determining valid directions to perform the action.");
        readonly GUIContent m_SweepBehaviorLabel = EditorGUIUtility.TrTextContent("Sweep Behavior",
            "Determines when the action should perform or cancel when sweeping the stick around the cardinal directions without returning to center.");
        readonly GUIContent m_PressPointLabel = EditorGUIUtility.TrTextContent("Press Point",
            "Magnitude threshold that must be crossed by an actuated control for the control to be considered pressed.");
        readonly GUIContent m_DefaultToggleLabel = EditorGUIUtility.TrTextContent("Default",
            "If enabled, the default value is used.");

        public override void OnGUI()
        {
            target.directions = (SectorInteraction.Directions)EditorGUILayout.EnumFlagsField(m_DirectionsLabel, target.directions);

            target.sweepBehavior = (SectorInteraction.SweepBehavior)EditorGUILayout.EnumPopup(m_SweepBehaviorLabel, target.sweepBehavior);

            var useDefaultValue = target.pressPoint < 0f;

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(useDefaultValue);

            var newPressPoint = EditorGUILayout.Slider(m_PressPointLabel, target.pressPointOrDefault, 0f, 1f, GUILayout.ExpandWidth(false));
            if (!useDefaultValue)
            {
                target.pressPoint = newPressPoint;
            }

            EditorGUI.EndDisabledGroup();

            var newUseDefault = GUILayout.Toggle(useDefaultValue, m_DefaultToggleLabel, GUILayout.ExpandWidth(false));
            if (newUseDefault != useDefaultValue)
            {
                target.pressPoint = newUseDefault ? -1f : SectorInteraction.defaultPressPoint;
            }

            EditorGUILayout.EndHorizontal();
        }
    }
#endif
}
