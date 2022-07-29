using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityStandardAssets.Characters.FirstPerson;

public class ArmToggle : MonoBehaviour
{
    [SerializeField] private XRBaseController _xrController;
    [SerializeField] private int _maxResetCount = 100;
    [SerializeField] private float _hapticAmplitude = 0.7f;
    [SerializeField] private float _hapticDuration = 0.5f;

    private AgentManager _agentManager = null;
    private LinkedList<Vector3> _validResetPositions = new LinkedList<Vector3>();
    private LinkedList<Vector3> _validResetRotations = new LinkedList<Vector3>();
    private Vector3 _defaultPos;
    private Vector3 _defaultRot;
    private Vector3 _originPos;
    private Vector3 _armOffset;

    private void Start() {
        _agentManager = GameObject.Find("PhysicsSceneManager").GetComponentInChildren<AgentManager>();
        var armAgent = (ArmAgentController)_agentManager.PrimaryAgent;
        var arm = armAgent.getArm();

        _defaultPos = arm.armTarget.localPosition;
        _defaultRot = arm.armTarget.eulerAngles;
    }

    public void ToggleArm(bool isArmMode) {
        if (isArmMode) {
            // Turn on autoSimulate physics
            StartCoroutine("ArmCoroutine");
        }
        else {
            // Return to orinigal autoSimulate physics
            StopCoroutine("ArmCoroutine");
        }
    }

    private IEnumerator ArmCoroutine() {
        var armAgent = (ArmAgentController)_agentManager.PrimaryAgent;
        var arm = armAgent.getArm();
        CollisionListener collisionListener = arm.collisionListener;

        _originPos = _xrController.transform.localPosition;
        _armOffset = arm.armTarget.localPosition;
        _validResetPositions.AddLast(arm.armTarget.localPosition);
        _validResetRotations.AddLast(arm.armTarget.eulerAngles);

        while (true) {
            arm.armTarget.localPosition = _xrController.transform.localPosition - _originPos + _armOffset;
            arm.armTarget.eulerAngles = _xrController.transform.eulerAngles;

            PhysicsSceneManager.PhysicsSimulateTHOR(Time.deltaTime);

            if (collisionListener.ShouldHalt()) {
                // Set arm position to last valid position and remove from valid reset lists
                arm.armTarget.localPosition = _validResetPositions.Last();
                _validResetPositions.RemoveLast();
                arm.armTarget.eulerAngles = _validResetRotations.Last();
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
                    _validResetRotations.AddLast(arm.armTarget.eulerAngles);
                }
            }
            yield return null;
        }
    }

    public void ResetArm() {
        var armAgent = (ArmAgentController)_agentManager.PrimaryAgent;
        var arm = armAgent.getArm();
        arm.armTarget.localPosition = _defaultPos;
        arm.armTarget.eulerAngles = _defaultRot;

        _originPos = _xrController.transform.localPosition;
        _armOffset = arm.armTarget.localPosition;
    }
}
