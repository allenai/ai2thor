
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Events;

public class XRInputManager : MonoBehaviour
{
    /// <summary>
    /// The Input Manager assigns callback functions to certain actions that can be perfromed by the XR controllers.
    /// </summary>
    
    [Header("Right Input Action References")]
    [SerializeField] private InputActionReference rightPrimaryButtonReference = null;
    [SerializeField] private InputActionReference rightSecondaryButtonReference = null;
    [SerializeField] private InputActionReference rightJoystickReference = null;

    [Header("Left Input Action References")]
    [SerializeField] private InputActionReference leftPrimaryButtonReference = null;
    [SerializeField] private InputActionReference leftSecondaryButtonReference = null;
    [SerializeField] private InputActionReference leftJoystickReference = null;

    [Header("Right Input CallBacks")]
    [SerializeField] private Event rightPrimaryButton;
    [SerializeField] private Event rightSecondaryButton;
    [SerializeField] private Event rightJoystick;

    [Header("Left Input CallBacks")]
    [SerializeField] private Event leftPrimaryButton;
    [SerializeField] private Event leftSecondaryButton;
    [SerializeField] private Event leftJoystick;
    private bool isInputActivated = true;

    [System.Serializable]
    public class Event : UnityEvent<InputAction.CallbackContext> {}

    // Right Callback Functions
    private void rightPrimaryButtonFunction(InputAction.CallbackContext context)
    {
        if (isInputActivated)
            rightPrimaryButton.Invoke(context);
    }

    private void rightSecondaryButtonFunction(InputAction.CallbackContext context)
    {
        if (isInputActivated)
            rightSecondaryButton.Invoke(context);
    }

    private void rightJoystickFunction(InputAction.CallbackContext context)
    {
        if (isInputActivated)
            rightJoystick.Invoke(context);
    }

    // Left Callback Functions
    private void leftPrimaryButtonFunction(InputAction.CallbackContext context)
    {
        if (isInputActivated)
            leftPrimaryButton.Invoke(context);
    }

    private void leftSecondaryButtonFunction(InputAction.CallbackContext context)
    {
        if (isInputActivated)
            leftSecondaryButton.Invoke(context);
    }

    private void leftJoystickFunction(InputAction.CallbackContext context)
    {
        if (isInputActivated)
            leftJoystick.Invoke(context);
    }

    // Start is called before the first frame update
    void Awake()
    {
        // Assign callback functions
        rightPrimaryButtonReference.action.performed += rightPrimaryButtonFunction;

        rightSecondaryButtonReference.action.performed += rightSecondaryButtonFunction;

        rightJoystickReference.action.performed += rightJoystickFunction;

        leftPrimaryButtonReference.action.performed += leftPrimaryButtonFunction;

        leftSecondaryButtonReference.action.performed += leftSecondaryButtonFunction;

        leftJoystickReference.action.performed += leftJoystickFunction;

    }

    private void OnDestroy()
    {
        rightPrimaryButtonReference.action.performed -= rightPrimaryButtonFunction;

        rightSecondaryButtonReference.action.performed -= rightSecondaryButtonFunction;

        rightJoystickReference.action.performed -= rightJoystickFunction;

        leftPrimaryButtonReference.action.performed -= leftPrimaryButtonFunction;

        leftSecondaryButtonReference.action.performed -= leftSecondaryButtonFunction;

        leftJoystickReference.action.performed -= leftJoystickFunction;

    }

    public void Deactivate()
    {
        isInputActivated = false;
    }

    public void Activate()
    {
        isInputActivated = true;
    }
}
