using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem.XR;

public class POVToggle : MonoBehaviour
{
    [SerializeField] private XROrigin _xrOrigin;
    [SerializeField] private Transform _cameraOffset;
    [SerializeField] private TrackedPoseDriver _trackedPoseDriver;
    [SerializeField] private Transform _camera;
    [SerializeField] private ScreenFader _screenFader;
    [SerializeField] private FirstPersonCharacterCull _firstPersonCharacterCull;

    private AgentManager _agentManager = null;
    private Vector3 _orignalCameraOffsetPos = Vector3.zero;
    private Quaternion _orignalCameraOffsetRot = Quaternion.identity;
    private Vector3 _orignalXROriginPos = Vector3.zero;
    private Quaternion _orignalXROriginRot = Quaternion.identity;
    private Vector3 _lastAgentCameraPos = Vector3.zero;
    private Quaternion _lastAgentCameratRot = Quaternion.identity;
    private bool _isFPSMode = false;
    private bool _isInitialized = false;

    public void Initialize() {
        _agentManager = GameObject.Find("PhysicsSceneManager").GetComponentInChildren<AgentManager>();

        _firstPersonCharacterCull.FPSController = _agentManager.PrimaryAgent.baseAgentComponent;
        _firstPersonCharacterCull.SwitchRenderersToHide(_agentManager.PrimaryAgent.baseAgentComponent.HeadVisCap);
        _firstPersonCharacterCull.enabled = false;
        _isInitialized = true;
    }

    private void OnDestroy() {
        StopAllCoroutines();
    }

    public void TogglePOV(bool isFPSMode) {
        StartCoroutine(TogglePOVCoroutine(isFPSMode));
    }

    private IEnumerator TogglePOVCoroutine(bool isFPSMode) {
        if (_isInitialized) {
            this._isFPSMode = isFPSMode;
            yield return _screenFader.StartFadeOut();
            if (isFPSMode) {
                // Only track rotation
                //_trackedPoseDriver.trackingType = TrackedPoseDriver.TrackingType.RotationOnly;

                // Save original position
                _orignalCameraOffsetPos = _cameraOffset.localPosition;
                _orignalCameraOffsetRot = _cameraOffset.localRotation;
                _orignalXROriginPos = _xrOrigin.transform.position;
                _orignalXROriginRot = _xrOrigin.transform.rotation;

                SetFPSCameraTransform();
                StartCoroutine("UpdateFPSCamera");

                if (_firstPersonCharacterCull != null) {
                    _firstPersonCharacterCull.enabled = true;
                }

            } else {
                StopCoroutine("UpdateFPSCamera");
                var angleDegrees = _xrOrigin.transform.rotation.eulerAngles.y - _orignalXROriginRot.eulerAngles.y;
                _xrOrigin.transform.position = _orignalXROriginPos;
                _xrOrigin.transform.rotation = _orignalXROriginRot;
                _cameraOffset.localPosition = _orignalCameraOffsetPos;
                _cameraOffset.localRotation = _orignalCameraOffsetRot;

                _xrOrigin.RotateAroundCameraUsingOriginUp(angleDegrees);
                if (_firstPersonCharacterCull != null) {
                    _firstPersonCharacterCull.enabled = false;
                }

                // Track both rotation and position
                //_trackedPoseDriver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
            }
            yield return _screenFader.StartFadeIn();
        }
    }

    private IEnumerator UpdateFPSCamera() {
        while (_isFPSMode) {
            var agent = _agentManager.PrimaryAgent;
            var agentCamera = agent.m_Camera;
            if (!agentCamera.transform.position.Equals(_lastAgentCameraPos) ||
                agentCamera.transform.eulerAngles.y != _lastAgentCameratRot.eulerAngles.y) {
                SetFPSCameraTransform();
            }

            yield return null;
        }
    }

    private void SetFPSCameraTransform() {
        if (_isFPSMode) {
            _trackedPoseDriver.enabled = false;
            var agent = _agentManager.PrimaryAgent;
            var agentCamera = agent.m_Camera;

            _lastAgentCameraPos = agentCamera.transform.position;
            _lastAgentCameratRot = agentCamera.transform.rotation;

            // Reset cameraOffset transfrom
            _cameraOffset.localPosition = _orignalCameraOffsetPos;
            _cameraOffset.localRotation = _orignalCameraOffsetRot;

            _cameraOffset.localPosition = _cameraOffset.InverseTransformPoint(agentCamera.transform.position);

            // Remove other rotations besides y
            Vector3 rot = new Vector3(0, agentCamera.transform.eulerAngles.y, 0);

            _cameraOffset.localRotation = Quaternion.Inverse(_cameraOffset.rotation) * Quaternion.Euler(rot);

            // Remove offset from camera to cameraOffset
            _cameraOffset.position += _cameraOffset.position - _camera.position;
            _trackedPoseDriver.enabled = true;
        }
    }
}
