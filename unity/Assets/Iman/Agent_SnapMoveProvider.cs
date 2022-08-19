using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityStandardAssets.Characters.FirstPerson;

public class Agent_SnapMoveProvider : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The Input System Action that will be used to read Snap Move data from the left hand controller. Must be a Value Vector2 Control.")]
    InputActionProperty m_LeftHandSnapMoveAction;
    /// <summary>
    /// The Input System Action that Unity uses to read Snap Move data sent from the left hand controller. Must be a <see cref="InputActionType.Value"/> <see cref="Vector2Control"/> Control.
    /// </summary>
    public InputActionProperty leftHandSnapMoveAction {
        get => m_LeftHandSnapMoveAction;
        set => SetInputActionProperty(ref m_LeftHandSnapMoveAction, value);
    }

    [SerializeField]
    [Tooltip("The Input System Action that will be used to read Snap Move data from the right hand controller. Must be a Value Vector2 Control.")]
    InputActionProperty m_RightHandSnapMoveAction;
    /// <summary>
    /// The Input System Action that Unity uses to read Snap Move data sent from the right hand controller. Must be a <see cref="InputActionType.Value"/> <see cref="Vector2Control"/> Control.
    /// </summary>
    public InputActionProperty rightHandSnapMoveAction {
        get => m_RightHandSnapMoveAction;
        set => SetInputActionProperty(ref m_RightHandSnapMoveAction, value);
    }

    void SetInputActionProperty(ref InputActionProperty property, InputActionProperty value) {
        if (Application.isPlaying)
            property.DisableDirectAction();

        property = value;

        if (Application.isPlaying && isActiveAndEnabled)
            property.EnableDirectAction();
    }
    [SerializeField]
    [Tooltip("The amount of time that the system will wait before starting another snap move.")]
    float m_DebounceTime = 0.5f;
    /// <summary>
    /// The amount of time that Unity waits before starting another snap turn.
    /// </summary>
    public float debounceTime {
        get => m_DebounceTime;
        set => m_DebounceTime = value;
    }

    [SerializeField]
    [Tooltip("The number of degrees clockwise to rotate when snap turning clockwise.")]
    float m_MoveAmount = 0.25f;
    /// <summary>
    /// The number of degrees clockwise Unity rotates the rig when snap turning clockwise.
    /// </summary>
    public float moveAmount {
        get => m_MoveAmount;
        set => m_MoveAmount = value;
    }

    float m_TimeStarted;

    private AgentManager AManager = null;

    private void Start() {
        AManager = GameObject.Find("PhysicsSceneManager").GetComponentInChildren<AgentManager>();
    }

    /// <summary>
    /// See <see cref="MonoBehaviour"/>.
    /// </summary>
    protected void Update() {
        Move();

    }

    protected Vector2 ReadInput() {
        var leftHandValue = m_LeftHandSnapMoveAction.action?.ReadValue<Vector2>() ?? Vector2.zero;
        var rightHandValue = m_RightHandSnapMoveAction.action?.ReadValue<Vector2>() ?? Vector2.zero;

        return leftHandValue + rightHandValue;
    }

    /// <summary>
    /// Determines the turn amount in degrees for the given <paramref name="input"/> vector.
    /// </summary>
    /// <param name="input">Input vector, such as from a thumbstick.</param>
    /// <returns>Returns the turn amount in degrees for the given <paramref name="input"/> vector.</returns>
    protected void Move() {
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
        action["moveMagnitude"] = m_MoveAmount;
        var cardinal = CardinalUtility.GetNearestCardinal(input);
        switch (cardinal) {
            case Cardinal.North:
                action["action"] = "MoveAhead";
                break;
            case Cardinal.South:
                action["action"] = "MoveBack";
                break;
            case Cardinal.East:
                action["action"] = "MoveRight";
                break;
            case Cardinal.West:
                action["action"] = "MoveLeft";
                break;
            default:
                Assert.IsTrue(false, $"Unhandled {nameof(Cardinal)}={cardinal}");
                break;
        }

        if (action["action"] != null) {
            agent.ProcessControlCommand(action);
        }
    }
}
