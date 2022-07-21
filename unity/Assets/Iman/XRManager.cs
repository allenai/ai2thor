
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;
using TMPro;
using System;
using UnityEngine.InputSystem.XR;
using UnityStandardAssets.Characters.FirstPerson;

public class XRManager : MonoBehaviour
{
    /// <summary>
    /// The Input Manager assigns callback functions to certain actions that can be perfromed by the XR controllers.
    /// </summary>
    /// 

    [SerializeField] private TMP_Text _modeText;
    [SerializeField] private float _fadeTime = 1.0f;

    [Header("Right Input Action References")]
    [SerializeField] private InputActionReference _rightThumbstickPressReference = null;
    [SerializeField] private InputActionReference _rightPrimaryPressReference = null;

    [Header("Left Input Action References")]
    [SerializeField] private InputActionReference _leftThumbstickPressReference = null;
    [SerializeField] private InputActionReference _leftPrimaryPressReference = null;

    [Header("Events")]
    [SerializeField] private UnityEvent _onUserControllerEvent = new UnityEvent();
    [SerializeField] private UnityEvent _onAgentControllerEvent = new UnityEvent();

    [SerializeField] private UnityEvent<bool> _onTogglePOVEvent = new UnityEvent<bool>();

    private enum ControllerMode {
        user = 0,
        agent = 1
    }

    private AgentManager _agentManager = null;
    private ControllerMode _controllerType = ControllerMode.user;
    private bool _isFPSMode = false;

    public bool IsFPSMode{
        get { return _isFPSMode; }
    }

    public TMP_Text ModeText {
        get { return _modeText; }
    }

    public static XRManager Instance { get; private set; }

    private void Awake() {
        // If there is an instance, and it's not me, delete myself.

        if (Instance != null && Instance != this) {
            Destroy(this);
        } else {
            Instance = this;
        }

        //Initialize
        _agentManager = GameObject.Find("PhysicsSceneManager").GetComponentInChildren<AgentManager>();

        _rightThumbstickPressReference.action.performed += ToggleLocomotionMode;
        _leftThumbstickPressReference.action.performed += ToggleLocomotionMode;

        _rightPrimaryPressReference.action.performed += TogglePOV;
        _leftPrimaryPressReference.action.performed += TogglePOV;
    }

    private void Start() {
        // Set as user mode
        _onUserControllerEvent?.Invoke();
    }

    private void Update() {
        //if (_isFPSMode) {
        //    IK_Robot_Arm_Controller arm = _AManager.PrimaryAgent.baseAgentComponent.IKArm.GetComponent<IK_Robot_Arm_Controller>();
        //    //arm.moveArmTarget((PhysicsRemoteFPSAgentController)agent, hand.position, float.PositiveInfinity, 0, false, "world");
        //    arm.armTarget.position = hand.position;
        //    arm.armTarget.rotation = hand.rotation;
        //}
    }

    // Called when you want to toggle controller mode
    private void ToggleLocomotionMode(InputAction.CallbackContext context) {
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
