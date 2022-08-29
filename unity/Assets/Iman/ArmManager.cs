using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityStandardAssets.Characters.FirstPerson;

public class ArmManager : MonoBehaviour
{
    [SerializeField] private XRBaseController _xrController;
    [SerializeField] private int _maxResetCount = 100;
    [SerializeField] private float _hapticAmplitude = 0.7f;
    [SerializeField] private float _hapticDuration = 0.5f;

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
    [Tooltip("The amount that the arm moves.")]
    float m_MoveArmAmount = 0.1f;
    /// <summary>
    /// The number of degrees clockwise Unity rotates the rig when snap turning clockwise.
    /// </summary>
    public float moveArmAmount {
        get => m_MoveArmAmount;
        set => m_MoveArmAmount = value;
    }

    private AgentManager _agentManager = null;
    private LinkedList<Vector3> _validResetPositions = new LinkedList<Vector3>();
    private LinkedList<Vector3> _validResetRotations = new LinkedList<Vector3>();
    private Vector3 _defaultPos;
    private Vector3 _defaultRot;
    private float _defaultArmHeight;
    private Vector3 _originPos;
    private Vector3 _armOffset;
    private bool _isInitialized = false;
    private bool _isArmMode = false;
    private float _armHeight;
    private ArmAgentController armAgent;
    private IK_Robot_Arm_Controller arm;

    public float ArmHeight {
        get { return _armHeight; }
        set { _armHeight = Mathf.Clamp(value, 0, 1); }
    }

    public void Initialize() {
        _agentManager = GameObject.Find("PhysicsSceneManager").GetComponentInChildren<AgentManager>();
        var armAgent = (ArmAgentController)_agentManager.PrimaryAgent;
        var arm = armAgent.getArm();

        _defaultPos = arm.armTarget.localPosition;
        _defaultRot = arm.armTarget.eulerAngles;
        _defaultArmHeight = arm.transform.localPosition.y;
        _isInitialized = true;
    }

    private void OnDestroy() {
        StopAllCoroutines();
    }

    public void ToggleArm(bool isArmMode) {
        if (!_isInitialized) {
            return;
        }

        _isArmMode = isArmMode;

        if (isArmMode) {
            armAgent = (ArmAgentController)_agentManager.PrimaryAgent;
            arm = armAgent.getArm();
            armAgent.agentState = AgentState.Processing;
            StartCoroutine("ArmCoroutine");
        }
        else {
            // Return to orinigal autoSimulate physics
            StopCoroutine("ArmCoroutine");
            armAgent.actionFinished(true);
            
        }
    }

    private IEnumerator ArmCoroutine() {
        CollisionListener collisionListener = arm.collisionListener;

        _originPos = _xrController.transform.localPosition;
        _armOffset = arm.armTarget.localPosition;
        _validResetPositions.AddLast(arm.armTarget.localPosition);
        _validResetRotations.AddLast(arm.armTarget.eulerAngles);

        while (true) {
            arm.armTarget.localPosition = _xrController.transform.localPosition - _originPos + _armOffset;
            arm.armTarget.localEulerAngles = _xrController.transform.localEulerAngles;

            ArmHeight += ReadInput();

            MoveArmBase(armAgent);

            PhysicsSceneManager.PhysicsSimulateTHOR(Time.deltaTime);

            if (collisionListener.ShouldHalt()) {
                // Set arm position to last valid position and remove from valid reset lists
                arm.armTarget.localPosition = _validResetPositions.Last();
                _validResetPositions.RemoveLast();
                arm.armTarget.localEulerAngles = _validResetRotations.Last();
                _validResetRotations.RemoveLast();

                // Set originPos and armOffset to new  track hand position
                _originPos = _xrController.transform.localPosition;
                _armOffset = arm.armTarget.localPosition;

                _xrController.SendHapticImpulse(_hapticAmplitude, _hapticDuration);
            } else {
                if (!_validResetPositions.Contains(arm.armTarget.localPosition)) {
                    if (_validResetPositions.Count > _maxResetCount) { // Too many reset positions stored
                        _validResetPositions.RemoveFirst();
                        _validResetRotations.RemoveFirst();
                    }

                    _validResetPositions.AddLast(arm.armTarget.localPosition);
                    _validResetRotations.AddLast(arm.armTarget.localEulerAngles);
                }
            }
            arm.AppendArmMetadataVR();
            yield return null;
        }
    }

    private void MoveArmBase(ArmAgentController controller) {
        CapsuleCollider cc = controller.GetComponent<CapsuleCollider>();
        Vector3 capsuleWorldCenter = cc.transform.TransformPoint(cc.center);

        float maxY = capsuleWorldCenter.y + cc.height / 2f;
        float minY = capsuleWorldCenter.y + (-cc.height / 2f) / 2f;

        // Normalized
        float height = (maxY - minY) * _armHeight + minY;

        var arm = controller.getArm();
        if (arm.transform.localPosition.y == height) {
            return;
        }

        if (height < minY || height > maxY) {
            throw new ArgumentOutOfRangeException($"height={height} value must be in [{minY}, {maxY}].");
        }

        arm.transform.localPosition = new Vector3(arm.transform.localPosition.x, _armHeight, arm.transform.localPosition.z);
    }

    protected float ReadInput() {
        var leftHandValue = m_LeftHandMoveArmBaseAction.action?.ReadValue<float>() ?? 0;
        var rightHandValue = m_RightHandMoveArmBaseAction.action?.ReadValue<float>() ?? 0;

        return rightHandValue - leftHandValue;
    }

    public void ToggleGrasp() {
        var armAgent = (ArmAgentController)_agentManager.PrimaryAgent;
        var arm = armAgent.getArm();

        if (arm.heldObjects.Count == 0) {
            List<string> grabbableObjects = arm.WhatObjectsAreInsideMagnetSphereAsObjectID();
            string errorMessage = "";
            if (!arm.PickupObject(grabbableObjects, ref errorMessage)) {
                print(errorMessage);
            }
        }
        else {
            arm.DropObject();
        }
    }

    public void ResetArm() {
        var armAgent = (ArmAgentController)_agentManager.PrimaryAgent;
        var arm = armAgent.getArm();

        arm.armTarget.position = _xrController.transform.position;
        arm.armTarget.eulerAngles = _xrController.transform.eulerAngles;

        _originPos = _xrController.transform.localPosition;
        _armOffset = arm.armTarget.localPosition;
    }

    public void DefaultArm() {
        var armAgent = (ArmAgentController)_agentManager.PrimaryAgent;
        var arm = armAgent.getArm();

        arm.armTarget.localPosition = _defaultPos;
        arm.armTarget.eulerAngles = _defaultRot;

        _originPos = _xrController.transform.localPosition;
        _armOffset = arm.armTarget.localPosition;
    }
}
