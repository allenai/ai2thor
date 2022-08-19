using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityStandardAssets.Characters.FirstPerson;

/// <summary>
/// A locomotion provider that allows the user to rotate the agent using a 2D axis input.
/// </summary>
public class Agent_SnapTurnProvider : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The Input System Action that will be used to read Snap Turn data from the left hand controller. Must be a Value Vector2 Control.")]
    InputActionProperty m_LeftHandSnapTurnAction;
    /// <summary>
    /// The Input System Action that Unity uses to read Snap Turn data sent from the left hand controller. Must be a <see cref="InputActionType.Value"/> <see cref="Vector2Control"/> Control.
    /// </summary>
    public InputActionProperty leftHandSnapTurnAction {
        get => m_LeftHandSnapTurnAction;
        set => SetInputActionProperty(ref m_LeftHandSnapTurnAction, value);
    }

    [SerializeField]
    [Tooltip("The Input System Action that will be used to read Snap Turn data from the right hand controller. Must be a Value Vector2 Control.")]
    InputActionProperty m_RightHandSnapTurnAction;
    /// <summary>
    /// The Input System Action that Unity uses to read Snap Turn data sent from the right hand controller. Must be a <see cref="InputActionType.Value"/> <see cref="Vector2Control"/> Control.
    /// </summary>
    public InputActionProperty rightHandSnapTurnAction {
        get => m_RightHandSnapTurnAction;
        set => SetInputActionProperty(ref m_RightHandSnapTurnAction, value);
    }


    [SerializeField]
    [Tooltip("The number of degrees clockwise to rotate when snap turning clockwise.")]
    float m_TurnAmount = 45f;
    /// <summary>
    /// The number of degrees clockwise Unity rotates the rig when snap turning clockwise.
    /// </summary>
    public float turnAmount {
        get => m_TurnAmount;
        set => m_TurnAmount = value;
    }

    [SerializeField]
    [Tooltip("The amount of time that the system will wait before starting another snap turn.")]
    float m_DebounceTime = 0.5f;
    /// <summary>
    /// The amount of time that Unity waits before starting another snap turn.
    /// </summary>
    public float debounceTime {
        get => m_DebounceTime;
        set => m_DebounceTime = value;
    }

    [SerializeField]
    [Tooltip("Controls whether to enable left & right snap turns.")]
    bool m_EnableTurnLeftRight = true;
    /// <summary>
    /// Controls whether to enable left and right snap turns.
    /// </summary>
    /// <seealso cref="enableTurnAround"/>
    public bool enableTurnLeftRight {
        get => m_EnableTurnLeftRight;
        set => m_EnableTurnLeftRight = value;
    }

    float m_CurrentTurnAmount;
    float m_TimeStarted;

    private AgentManager AManager = null;

    /// <summary>
    /// See <see cref="MonoBehaviour"/>.
    /// </summary>
    protected void OnEnable() {
        m_LeftHandSnapTurnAction.EnableDirectAction();
        m_RightHandSnapTurnAction.EnableDirectAction();
    }

    /// <summary>
    /// See <see cref="MonoBehaviour"/>.
    /// </summary>
    protected void OnDisable() {
        m_LeftHandSnapTurnAction.DisableDirectAction();
        m_RightHandSnapTurnAction.DisableDirectAction();
    }

    private void Start() {
        AManager = GameObject.Find("PhysicsSceneManager").GetComponentInChildren<AgentManager>();
    }

    /// <summary>
    /// See <see cref="MonoBehaviour"/>.
    /// </summary>
    protected void Update() {
        Turn();
    }

    protected Vector2 ReadInput() {
        var leftHandValue = m_LeftHandSnapTurnAction.action?.ReadValue<Vector2>() ?? Vector2.zero;
        var rightHandValue = m_RightHandSnapTurnAction.action?.ReadValue<Vector2>() ?? Vector2.zero;

        return leftHandValue + rightHandValue;
    }

    
    /// <summary>
    /// Determines the turn amount in degrees for the given <paramref name="input"/> vector.
    /// </summary>
    /// <param name="input">Input vector, such as from a thumbstick.</param>
    /// <returns>Returns the turn amount in degrees for the given <paramref name="input"/> vector.</returns>
    protected void Turn() {
        // Wait for a certain amount of time before allowing another turn.
        if (m_TimeStarted > 0f && (m_TimeStarted + m_DebounceTime < Time.time)) {
            m_TimeStarted = 0f;
            return;
        }

        var input = ReadInput();

        if (input == Vector2.zero)
            return;

        PhysicsRemoteFPSAgentController agent = (PhysicsRemoteFPSAgentController)AManager.PrimaryAgent;
        if (m_TimeStarted > 0f || agent.agentState == AgentState.ActionComplete)
            return;

        Dictionary<string, object> action = new Dictionary<string, object>();
        m_TimeStarted = Time.time;
        action["degrees"] = m_TurnAmount;
        var cardinal = CardinalUtility.GetNearestCardinal(input);
        switch (cardinal) {
            case Cardinal.North:
                action["action"] = "LookUp";
                break;
            case Cardinal.South:
                action["action"] = "LookDown";
                break;
            case Cardinal.East:
                if (m_EnableTurnLeftRight)
                    action["action"] = "RotateRight";
                break;
            case Cardinal.West:
                if (m_EnableTurnLeftRight)
                    action["action"] = "RotateLeft";
                break;
            default:
                Assert.IsTrue(false, $"Unhandled {nameof(Cardinal)}={cardinal}");
                break;
        }
        if (action["action"] != null) {
            agent.ProcessControlCommand(action);
        }

    }

    void SetInputActionProperty(ref InputActionProperty property, InputActionProperty value) {
        if (Application.isPlaying)
            property.DisableDirectAction();

        property = value;

        if (Application.isPlaying && isActiveAndEnabled)
            property.EnableDirectAction();
    }
}
