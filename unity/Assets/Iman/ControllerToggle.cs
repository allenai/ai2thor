using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;
using System.Collections;

public class ControllerToggle : MonoBehaviour {
    /// <summary>
    /// The <c>TeleportController<c> class manages teleportation. 
    /// When the primary button is pressed down activates teleporation mode.
    /// When primary button let go reverts back to base controllers
    /// </summary>

    [SerializeField] private InputActionReference _leftToggleControllerActivationReference;
    [SerializeField] private InputActionReference _rightToggleControllerActivationReference;

    [SerializeField] private TMP_Text _controllerTypeText;
    [SerializeField] private float _fadeTime = 1.0f;

    [Header("Left Hand")]
    [SerializeField] private ActionBasedController _leftAgentXRController;
    [SerializeField] private XRRayInteractor _leftAgentXRRayInteractor;
    [SerializeField] private ActionBasedController _leftUserXRController;
    [SerializeField] private XRRayInteractor _leftUserXRRayInteractor;


    [Header("Right Hand")]
    [SerializeField] private ActionBasedController _rightAgentXRController;
    [SerializeField] private XRRayInteractor _rightAgentXRRayInteractor;
    [SerializeField] private ActionBasedController _rightUserXRController;
    [SerializeField] private XRRayInteractor _rightUserXRRayInteractor;

    [SerializeField] private UnityEvent _onUserControllerEvent = new UnityEvent();
    [SerializeField] private UnityEvent _onAgentControllerEvent = new UnityEvent();

    private enum ControllerType {
        user = 0,
        agent = 1
    }

    private ControllerType _controllerType = ControllerType.user;

    private void Start() {
        // Assign call back fucntions
        _leftToggleControllerActivationReference.action.performed += ToggleController;
        _rightToggleControllerActivationReference.action.canceled += ToggleController;

        _leftAgentXRController.enableInputActions = false;
        _leftUserXRController.enableInputActions = true;
        _leftAgentXRRayInteractor.enabled = false;
        _leftUserXRRayInteractor.enabled = true;
        if (_leftUserXRController.model != null) {
            _leftUserXRController.model.gameObject.SetActive(true);
        }


        _rightAgentXRController.enableInputActions = false;
        _rightUserXRController.enableInputActions = true;
        _rightAgentXRRayInteractor.enabled = false;
        _rightUserXRRayInteractor.enabled = true;
        if (_rightUserXRController.model != null) {
            _rightUserXRController.model.gameObject.SetActive(true);
        }


        _onUserControllerEvent?.Invoke();
    }

    private void OnDestroy() {
        _leftToggleControllerActivationReference.action.performed -= ToggleController;
        _rightToggleControllerActivationReference.action.canceled -= ToggleController;
    }

    // Called when you want to activate teleport mode
    private void ToggleController(InputAction.CallbackContext context) {
        _controllerType = ~_controllerType;
        bool value = Convert.ToBoolean((int)_controllerType);

        StopAllCoroutines();

        if (value) {
            _onAgentControllerEvent?.Invoke();
            _controllerTypeText.text = "Agent Mode";
            _controllerTypeText.color = Color.blue;
            StartCoroutine(FadeControllerTypeText());
        }
        else {
            _onUserControllerEvent?.Invoke();
            _controllerTypeText.text = "User Mode";
            _controllerTypeText.color = Color.red;
            StartCoroutine(FadeControllerTypeText());
        }

        
        _leftAgentXRController.enableInputActions = value;
        _leftUserXRController.enableInputActions = !value;
        _leftAgentXRRayInteractor.enabled = value;
        _leftUserXRRayInteractor.enabled = !value;
        _leftUserXRController.model?.gameObject.SetActive(!value);

        _rightAgentXRController.enableInputActions = value;
        _rightUserXRController.enableInputActions = !value;
        _rightAgentXRRayInteractor.enabled = value;
        _rightUserXRRayInteractor.enabled = !value;
        _rightUserXRController.model?.gameObject.SetActive(!value);
    }

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

    private IEnumerator FadeControllerTypeText() {
        float timer = 0;
        while (timer < _fadeTime) {
            timer += Time.deltaTime;
            _controllerTypeText.color = new Color(_controllerTypeText.color.r, _controllerTypeText.color.g, _controllerTypeText.color.b, timer / _fadeTime);
            yield return null;
        }
        _controllerTypeText.color = new Color(_controllerTypeText.color.r, _controllerTypeText.color.g, _controllerTypeText.color.b, 1);

        yield return new WaitForSeconds(_fadeTime);

        timer = _fadeTime;
        while (timer > 0) {
            timer -= Time.deltaTime;
            _controllerTypeText.color = new Color(_controllerTypeText.color.r, _controllerTypeText.color.g, _controllerTypeText.color.b, timer / _fadeTime);
            yield return null;
        }
        _controllerTypeText.color *= new Color(_controllerTypeText.color.r, _controllerTypeText.color.g, _controllerTypeText.color.b, 0);
    }
}
