
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;
using System.Collections;
using TMPro;
using System;
using UnityStandardAssets.Characters.FirstPerson;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Unity.XR.CoreUtils;

[RequireComponent(typeof(XROrigin))]
public class XRManager : MonoBehaviour
{
    /// <summary>
    /// The Input Manager assigns callback functions to certain actions that can be perfromed by the XR controllers.
    /// </summary>
    /// 

    [SerializeField] private TMP_Text _notificationText;
    [SerializeField] private CanvasGroup _notificationCanvasGroup;
    [SerializeField] private Canvas _armMenu;
    [SerializeField] private TMP_Text _locomotionText;
    [SerializeField] private TMP_Text _armText;
    [SerializeField] private TMP_Text _povText;
    [SerializeField] private float _fadeTime = 1.0f;

    [Header("Right Input Action References")]
    //[SerializeField] private InputActionReference _rightThumbstickPressReference = null;
    [SerializeField] private InputActionReference _rightPrimaryTapReference = null;
    //[SerializeField] private InputActionReference _rightSecondaryTapReference = null;
    [SerializeField] private InputActionReference _rightSecondaryHoldReference = null;
    //[SerializeField] private InputActionReference _rightGripPressReference = null;

    [Header("Left Input Action References")]
    [SerializeField] private InputActionReference _leftMenuPressReference = null;
    //[SerializeField] private InputActionReference _leftThumbstickClickReference = null;
    [SerializeField] private InputActionReference _leftPrimaryTapReference = null;
    [SerializeField] private InputActionReference _leftSecondaryTapReference = null;
    //[SerializeField] private InputActionReference _leftSecondaryHoldReference = null;
    //[SerializeField] private InputActionReference _leftGripPressReference = null;

    [Header("Events")]
    [SerializeField] private UnityEvent _onUserLocomotionEvent = new UnityEvent();
    [SerializeField] private UnityEvent _onAgentLocomotionEvent = new UnityEvent();

    [SerializeField] private UnityEvent<bool> _onTogglePOVEvent = new UnityEvent<bool>();

    [SerializeField] private UnityEvent<bool> _onArmOnEvent = new UnityEvent<bool>();
    [SerializeField] private UnityEvent<bool> _onArmOffEvent = new UnityEvent<bool>();

    [SerializeField] private UnityEvent _onResetArmEvent = new UnityEvent();
    [SerializeField] private UnityEvent _onDefaultArmEvent = new UnityEvent();

    [SerializeField] private UnityEvent _onPickUpObjectEvent = new UnityEvent();

    [SerializeField] private UnityEvent<bool> _onToggleGuideEvent = new UnityEvent<bool>();

    [SerializeField] private UnityEvent _onInitializedEvent = new UnityEvent();


    private enum ControllerMode {
        user = 0,
        agent = -1
    }

    private AgentManager _agentManager = null;
    private ControllerMode _locomotionMode = ControllerMode.user;
    private bool _isInitialized = false;
    private bool _isFPSMode = false;
    private bool _isArmMode = false;
    private bool _isCrouching = false;
    private bool _showingGuide = true;

    public bool IsFPSMode{
        get { return _isFPSMode; }
    }

    public TMP_Text ModeText {
        get { return _notificationText; }
    }

    public static XRManager Instance { get; private set; }

    BaseFPSAgentController CurrentActiveController() {
        return _agentManager.PrimaryAgent;
    }

    private void Awake() {
        // If there is an instance, and it's not me, delete myself.
        if (Instance != null && Instance != this) {
            Destroy(Instance.gameObject);
            Instance = this;
        } else {
            Instance = this;
        }

        _agentManager = GameObject.Find("PhysicsSceneManager").GetComponentInChildren<AgentManager>();

        _leftMenuPressReference.action.performed += (InputAction.CallbackContext context) => { ToggleMenu(); };

        _leftPrimaryTapReference.action.performed += (InputAction.CallbackContext context) => { ToggleCrouch(); };

        //_rightThumbstickPressReference.action.performed += (InputAction.CallbackContext context) => { ToggleMoveArmBase(); };
        //_leftThumbstickPressReference.action.performed += (InputAction.CallbackContext context) => { ToggleMoveArmBase(); };

        _rightPrimaryTapReference.action.performed += (InputAction.CallbackContext context) => { ToggleGrasp(); };
        //_leftPrimaryTapReference.action.performed += (InputAction.CallbackContext context) => { TogglePOV(); };

        _leftSecondaryTapReference.action.performed += (InputAction.CallbackContext context) => { ToggleGuide(); };

        //_rightSecondaryTapReference.action.performed += (InputAction.CallbackContext context) => { ToggleArm(); };
        //_leftSecondaryTapReference.action.performed += (InputAction.CallbackContext context) => { ToggleArm(); };

        _rightSecondaryHoldReference.action.performed += (InputAction.CallbackContext context) => { ResetArm(); };
        //_leftSecondaryHoldReference.action.performed += (InputAction.CallbackContext context) => { ResetArm(); };

        //_rightGripPressReference.action.performed += (InputAction.CallbackContext context) => { ToggleGrasp(); };
        //_leftGripPressReference.action.performed += (InputAction.CallbackContext context) => { ToggleGrasp(); };
    }

    private void OnDestroy() {
        StopAllCoroutines();

        _leftMenuPressReference.action.performed -= (InputAction.CallbackContext context) => { ToggleMenu(); };

        _leftPrimaryTapReference.action.performed -= (InputAction.CallbackContext context) => { ToggleCrouch(); };

        //_rightThumbstickPressReference.action.performed -= (InputAction.CallbackContext context) => { ToggleMoveArmBase(); };
        //_leftThumbstickPressReference.action.performed -= (InputAction.CallbackContext context) => { ToggleMoveArmBase(); };

        _rightPrimaryTapReference.action.performed -= (InputAction.CallbackContext context) => { ToggleGrasp(); };
        //_leftPrimaryTapReference.action.performed -= (InputAction.CallbackContext context) => { TogglePOV(); };

        _leftSecondaryTapReference.action.performed -= (InputAction.CallbackContext context) => { ToggleGuide(); };

        //_rightSecondaryTapReference.action.performed -= (InputAction.CallbackContext context) => { ToggleArm(); };
        //_leftSecondaryTapReference.action.performed -= (InputAction.CallbackContext context) => { ToggleArm(); };

        _rightSecondaryHoldReference.action.performed -= (InputAction.CallbackContext context) => { ResetArm(); };
        //_leftSecondaryHoldReference.action.performed -= (InputAction.CallbackContext context) => { ResetArm(); };

        //_rightGripPressReference.action.performed -= (InputAction.CallbackContext context) => { ToggleGrasp(); };
        //_leftGripPressReference.action.performed -= (InputAction.CallbackContext context) => { ToggleGrasp(); };
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
        action["autoSimulation"] = true;
        action["action"] = "Initialize";
        CurrentActiveController().ProcessControlCommand(new DynamicServerAction(action), _agentManager);

        _onInitializedEvent?.Invoke();

        _isInitialized = true;
    }

    private void Start() {
        // Set as user mode
        _onUserLocomotionEvent?.Invoke();
    }

    // Called when you want to toggle controller mode
    public void ToggleLocomotionMode() {
        if (!_isInitialized) {
            return;
        }

        _locomotionMode = ~_locomotionMode;
        bool value = Convert.ToBoolean((int)_locomotionMode);

        StopCoroutine("FadeNotificationCoroutine");

        if (value) {
            _onAgentLocomotionEvent?.Invoke();
            _notificationText.text = "Locomotion: <color=#0000FF>Agent</color>";
            _locomotionText.text = _notificationText.text;
            StartCoroutine("FadeNotificationCoroutine");
        } else {
            _onUserLocomotionEvent?.Invoke();
            _notificationText.text = "Locomotion: <color=#FF0000>User</color>";
            _locomotionText.text = _notificationText.text;
            StartCoroutine("FadeNotificationCoroutine");
        }
    }

    public void TogglePOV() {
        if (!_isInitialized) {
            return;
        }

        _isFPSMode = !_isFPSMode;

        StopCoroutine("FadeNotificationCoroutine");

        if (_isFPSMode) {
            _notificationText.text = "POV: <color=#0000FF>First</color>";
            _povText.text = _notificationText.text;

            // Set locomotion to agent
            _locomotionMode = ControllerMode.agent;
            _onAgentLocomotionEvent?.Invoke();
            _locomotionText.text = "Locomotion: <color=#0000FF>Agent</color>";

            StartCoroutine("FadeNotificationCoroutine");
        } else {
            _notificationText.text = "POV: <color=#FF0000>Third</color>";
            _povText.text = _notificationText.text;
            StartCoroutine("FadeNotificationCoroutine");
        }

        _onTogglePOVEvent?.Invoke(_isFPSMode);
    }

    public void ToggleArm() {
        if (!_isInitialized) {
            return;
        }
        _isArmMode = !_isArmMode;

        StopCoroutine("FadeNotificationCoroutine");

        if (_isArmMode) {
            _onArmOnEvent?.Invoke(_isArmMode);
            _notificationText.text = "Arm: <color=#0000FF>On</color>";
            _armText.text = _notificationText.text;
            StartCoroutine("FadeNotificationCoroutine");

        } else {
            _onArmOffEvent?.Invoke(_isArmMode);
            _notificationText.text = "Arm: <color=#FF0000>Off</color>";
            _armText.text = _notificationText.text;
            StartCoroutine("FadeNotificationCoroutine");
        }
    }

    private void ToggleCrouch() {
        if (!_isInitialized || _locomotionMode == ControllerMode.user) {
            return;
        }

        _isCrouching = !_isCrouching;
        Dictionary<string, object> action = new Dictionary<string, object>();
        
        if (_isCrouching) {
            action["action"] = "Crouch";
            
        } else {
            action["action"] = "Stand";
        }
        CurrentActiveController().ProcessControlCommand(action);
    }

    private void ToggleGuide() {
        _showingGuide = !_showingGuide;
        _onToggleGuideEvent?.Invoke(_showingGuide);
    }

    private void ToggleGrasp() {
        if (!_isInitialized || !_isArmMode) {
            return;
        }

        _onPickUpObjectEvent?.Invoke();
    }

    private void ResetArm() {
        if (!_isArmMode || !_isInitialized) {
            return;
        }

        StopCoroutine("FadeNotificationCoroutine");

        _notificationText.text = "Reset Arm";
        _notificationText.color = Color.white;


        if (_isFPSMode) {
            _notificationText.text = "Reset Arm";
            _onResetArmEvent?.Invoke();
        } else {
            _notificationText.text = "Default Arm";
            _onDefaultArmEvent?.Invoke();
        }
        StartCoroutine("FadeNotificationCoroutine");
    }

    private void ToggleMenu() {
        if (!_isInitialized) {
            return;
        }
        _armMenu.gameObject.SetActive(!_armMenu.gameObject.activeSelf);
    }

    private IEnumerator FadeNotificationCoroutine() {
        float timer = 0;
        while (timer < _fadeTime) {
            timer += Time.deltaTime;
            _notificationCanvasGroup.alpha =  timer / _fadeTime;
            yield return null;
        }
        _notificationCanvasGroup.alpha = 1;

        yield return new WaitForSeconds(_fadeTime);

        timer = _fadeTime;
        while (timer > 0) {
            timer -= Time.deltaTime;
            _notificationCanvasGroup.alpha = timer / _fadeTime;
            yield return null;
        }
        _notificationCanvasGroup.alpha = 0;
    }

    /*
     * Events Helper Functions
     */

    public void AddListenerToUserEvent(UnityAction action) {
        _onUserLocomotionEvent.AddListener(action);
    }
    public void RemoveListenerToUserEvent(UnityAction action) {
        _onUserLocomotionEvent.RemoveListener(action);
    }
    public void AddListenerToAgentEvent(UnityAction action) {
        _onAgentLocomotionEvent.AddListener(action);
    }
    public void RemoveListenerToAgentEvent(UnityAction action) {
        _onAgentLocomotionEvent.RemoveListener(action);
    }
    public void AddListenerToInitializeEvent(UnityAction action) {
        _onInitializedEvent.AddListener(action);
    }
    public void RemoveListenerToInitializeEvent(UnityAction action) {
        _onInitializedEvent.RemoveListener(action);
    }

}
