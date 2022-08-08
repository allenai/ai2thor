using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityStandardAssets.Characters.FirstPerson;

public class MoveArmBaseProvider : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The Input System Action that will be used to read Snap Move data from the left hand controller. Must be a Value Vector2 Control.")]
    InputActionProperty m_LeftHandMoveArmBaseAction;
    /// <summary>
    /// The Input System Action that Unity uses to read Snap Move data sent from the left hand controller. Must be a <see cref="InputActionType.Value"/> <see cref="Vector2Control"/> Control.
    /// </summary>
    public InputActionProperty leftHandMoveArmBaseAction {
        get => m_LeftHandMoveArmBaseAction;
        set => SetInputActionProperty(ref m_LeftHandMoveArmBaseAction, value);
    }

    [SerializeField]
    [Tooltip("The Input System Action that will be used to read Snap Move data from the right hand controller. Must be a Value Vector2 Control.")]
    InputActionProperty m_RightHandMoveArmBaseAction;
    /// <summary>
    /// The Input System Action that Unity uses to read Snap Move data sent from the right hand controller. Must be a <see cref="InputActionType.Value"/> <see cref="Vector2Control"/> Control.
    /// </summary>
    public InputActionProperty rightHandMoveArmBaseAction {
        get => m_RightHandMoveArmBaseAction;
        set => SetInputActionProperty(ref m_RightHandMoveArmBaseAction, value);
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
    [Tooltip("The amount that the arm moves.")]
    float m_MoveArmAmount = 0.01f;
    /// <summary>
    /// The number of degrees clockwise Unity rotates the rig when snap turning clockwise.
    /// </summary>
    public float moveArmAmount {
        get => m_MoveArmAmount;
        set => m_MoveArmAmount = value;
    }

    [SerializeField] private ArmManager _armManager;

    float m_TimeStarted;

    private AgentManager AManager = null;
    

    private void Start() {
        AManager = GameObject.Find("PhysicsSceneManager").GetComponentInChildren<AgentManager>();
    }

    /// <summary>
    /// See <see cref="MonoBehaviour"/>.
    /// </summary>
    protected void Update() {
        MoveArmBase();
    }

    protected Vector2 ReadInput() {
        var leftHandValue = m_LeftHandMoveArmBaseAction.action?.ReadValue<Vector2>() ?? Vector2.zero;
        var rightHandValue = m_RightHandMoveArmBaseAction.action?.ReadValue<Vector2>() ?? Vector2.zero;

        return new Vector2(0, (leftHandValue + rightHandValue).y);
    }

    /// <summary>
    /// Determines the turn amount in degrees for the given <paramref name="input"/> vector.
    /// </summary>
    /// <param name="input">Input vector, such as from a thumbstick.</param>
    /// <returns>Returns the turn amount in degrees for the given <paramref name="input"/> vector.</returns>
    protected void MoveArmBase() {
        // Wait for a certain amount of time before allowing another turn.
        if (m_TimeStarted > 0f && (m_TimeStarted + m_DebounceTime < Time.time)) {
            m_TimeStarted = 0f;
            return;
        }

        var input = ReadInput();
        if (input == Vector2.zero)
            return;

        PhysicsRemoteFPSAgentController agent = (PhysicsRemoteFPSAgentController)AManager.PrimaryAgent;
        if (m_TimeStarted > 0f || agent.agentState == AgentState.Emit)
            return;

        var cardinal = CardinalUtility.GetNearestCardinal(input);
        switch (cardinal) {
            case Cardinal.North:
                _armManager.ArmHeight += moveArmAmount;
                break;
            case Cardinal.South:
                _armManager.ArmHeight -= moveArmAmount;
                break;
            default:
                Assert.IsTrue(false, $"Unhandled {nameof(Cardinal)}={cardinal}");
                break;
        }
    }
}
