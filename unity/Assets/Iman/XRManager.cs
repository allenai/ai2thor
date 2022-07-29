
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using TMPro;
using System;
using UnityEngine.InputSystem.XR;
using UnityStandardAssets.Characters.FirstPerson;
using System.Collections.Generic;

public class XRManager : MonoBehaviour
{
    /// <summary>
    /// The Input Manager assigns callback functions to certain actions that can be perfromed by the XR controllers.
    /// </summary>
    /// 

    [SerializeField] private TMP_Text _modeText;
    [SerializeField] private float _fadeTime = 1.0f;
    [SerializeField] private ArmToggle _armToggle;
    [SerializeField] private POVToggle _povToggle;
    [SerializeField] private LocomotionToggle _locomotionToggle;

    [Header("Right Input Action References")]
    [SerializeField] private InputActionReference _rightThumbstickPressReference = null;
    [SerializeField] private InputActionReference _rightPrimaryTapReference = null;
    [SerializeField] private InputActionReference _rightSecondaryTapReference = null;
    [SerializeField] private InputActionReference _rightSecondaryHoldReference = null;

    [Header("Left Input Action References")]
    [SerializeField] private InputActionReference _leftThumbstickPressReference = null;
    [SerializeField] private InputActionReference _leftPrimaryTapReference = null;
    [SerializeField] private InputActionReference _leftSecondaryTapReference = null;
    [SerializeField] private InputActionReference _leftSecondaryHoldReference = null;

    [Header("Events")]
    [SerializeField] private UnityEvent _onUserControllerEvent = new UnityEvent();
    [SerializeField] private UnityEvent _onAgentControllerEvent = new UnityEvent();

    [SerializeField] private UnityEvent<bool> _onTogglePOVEvent = new UnityEvent<bool>();

    [SerializeField] private UnityEvent<bool> _onToggleArmEvent = new UnityEvent<bool>();

    [SerializeField] private UnityEvent _onResetArmEvent = new UnityEvent();

    private enum ControllerMode {
        user = 0,
        agent = 1
    }

    private AgentManager _agentManager = null;
    private ControllerMode _controllerType = ControllerMode.user;
    private bool _isInitialized = false;
    private bool _isFPSMode = false;
    private bool _isArmMode = false;

    public bool IsFPSMode{
        get { return _isFPSMode; }
    }

    public TMP_Text ModeText {
        get { return _modeText; }
    }

    public static XRManager Instance { get; private set; }

    BaseFPSAgentController CurrentActiveController() {
        return _agentManager.PrimaryAgent;
    }

    private void Awake() {
        // If there is an instance, and it's not me, delete myself.
        if (Instance != null && Instance != this) {
            Destroy(this.gameObject);
        } else {
            Instance = this;
        }

        _agentManager = GameObject.Find("PhysicsSceneManager").GetComponentInChildren<AgentManager>();

        _rightThumbstickPressReference.action.performed += ToggleLocomotionMode;
        _leftThumbstickPressReference.action.performed += ToggleLocomotionMode;

        _rightPrimaryTapReference.action.performed += TogglePOV;
        _leftPrimaryTapReference.action.performed += TogglePOV;

        _rightSecondaryTapReference.action.performed += ToggleArm;
        _leftSecondaryTapReference.action.performed += ToggleArm;

        _rightSecondaryHoldReference.action.performed += ResetArm;
        _leftSecondaryHoldReference.action.performed += ResetArm;
    }

    public void Initialize() {
        Dictionary<string, object> action = new Dictionary<string, object>();
        // if you want to use smaller grid size step increments, initialize with a smaller/larger gridsize here
        // by default the gridsize is 0.25, so only moving in increments of .25 will work
        // so the MoveAhead action will only take, by default, 0.25, .5, .75 etc magnitude with the default
        // grid size!
        // action.renderNormalsImage = true;
        // action.renderDepthImage = true;
        // action.renderSemanticSegmentation = true;
        // action.renderInstanceSegmentation = true;
        // action.renderFlowImage = true;
        // action.rotateStepDegrees = 30;
        // action.ssao = "default";
        // action.snapToGrid = true;
        // action.makeAgentsVisible = false;
        action["agentMode"] = "vr";
        action["fieldOfView"] = 90f;
        // action.cameraY = 2.0f;
        action["snapToGrid"] = true;
        // action.rotateStepDegrees = 45;
        action["action"] = "Initialize";
        CurrentActiveController().ProcessControlCommand(new DynamicServerAction(action), _agentManager);

        _armToggle.enabled = true;
        _povToggle.enabled = true;

        _isInitialized = true;
    }

    private void Start() {
        // Set as user mode
        _onUserControllerEvent?.Invoke();
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Space)) {
            Initialize();
        }
    }

    // Called when you want to toggle controller mode
    private void ToggleLocomotionMode(InputAction.CallbackContext context) {
        if (!_isInitialized) {
            return;
        }

        _controllerType = ~_controllerType;
        bool value = Convert.ToBoolean((int)_controllerType);

        StopCoroutine("FadeText");

        if (value) {
            _onAgentControllerEvent?.Invoke();
            _modeText.text = "Locomotion: Agent";
            _modeText.color = Color.blue;
            StartCoroutine("FadeText");
        } else {
            _onUserControllerEvent?.Invoke();
            _modeText.text = "Locomotion: User";
            _modeText.color = Color.red;
            StartCoroutine("FadeText");
        }
    }

    private void TogglePOV(InputAction.CallbackContext context) {
        if (!_isInitialized) {
            return;
        }

        _isFPSMode = !_isFPSMode;

        StopCoroutine("FadeText");

        if (_isFPSMode) {
            _modeText.text = "POV: First";
            _modeText.color = Color.white;
            StartCoroutine("FadeText");
        } else {
            _modeText.text = "POV: Third";
            _modeText.color = Color.white;
            StartCoroutine("FadeText");
        }

        _onTogglePOVEvent?.Invoke(_isFPSMode);
    }

    private void ToggleArm(InputAction.CallbackContext context) {
        if (!_isInitialized) {
            return;
        }
        _isArmMode = !_isArmMode;

        StopCoroutine("FadeText");

        if (_isArmMode) {
            _modeText.text = "Arm: On";
            _modeText.color = Color.white;
            StartCoroutine("FadeText");
        } else {
            _modeText.text = "Arm: Off";
            _modeText.color = Color.white;
            StartCoroutine("FadeText");
        }

        _onToggleArmEvent?.Invoke(_isArmMode);
    }

    private void ResetArm(InputAction.CallbackContext context) {
        if (!_isArmMode || !_isInitialized) {
            return;
        }

        StopCoroutine("FadeText");

        _modeText.text = "Reset Arm";
        _modeText.color = Color.white;
        StartCoroutine("FadeText");

        _onResetArmEvent?.Invoke();
    }

    public void FadeText() {
        StopCoroutine("FadeTextCoroutine");
        StartCoroutine("FadeTextCoroutine");
    }

    private IEnumerator FadeTextCoroutine() {
        float timer = 0;
        while (timer < _fadeTime) {
            timer += Time.deltaTime;
            _modeText.color = new Color(_modeText.color.r, _modeText.color.g, _modeText.color.b, timer / _fadeTime);
            yield return null;
        }
        _modeText.color = new Color(_modeText.color.r, _modeText.color.g, _modeText.color.b, 1);

        yield return new WaitForSeconds(_fadeTime);

        timer = _fadeTime;
        while (timer > 0) {
            timer -= Time.deltaTime;
            _modeText.color = new Color(_modeText.color.r, _modeText.color.g, _modeText.color.b, timer / _fadeTime);
            yield return null;
        }
        _modeText.color *= new Color(_modeText.color.r, _modeText.color.g, _modeText.color.b, 0);
    }


    /*
     * Events Helper Functions
     */

    public void AddListenerToUserEvent(UnityAction action) {
        _onUserControllerEvent.AddListener(action);
    }
    public void RemoveListenerToUserEvent(UnityAction action) {
        _onUserControllerEvent.RemoveListener(action);
    }
    public void AddListenerToAgentEvent(UnityAction action) {
        _onAgentControllerEvent.AddListener(action);
    }
    public void RemoveListenerToAgentEvent(UnityAction action) {
        _onAgentControllerEvent.RemoveListener(action);
    }

}
