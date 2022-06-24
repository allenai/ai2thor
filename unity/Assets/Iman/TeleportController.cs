using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class TeleportController : MonoBehaviour
{
    /// <summary>
    /// The <c>TeleportController<c> class manages teleportation. 
    /// When the primary button is pressed down activates teleporation mode.
    /// When primary button let go reverts back to base controllers
    /// </summary>

    [SerializeField] private InputActionReference teleportActivationReference;

    [Space]
    [SerializeField] private UnityEvent OnTeleportActivate;
    [SerializeField] private UnityEvent OnTeleportCancel;

    private void Start()
    {
        // Assign call back fucntions
        teleportActivationReference.action.performed += TeleportModeActivate;
        teleportActivationReference.action.canceled += TeleportModeCancel;
    }

    private void OnDestroy()
    {
        teleportActivationReference.action.performed -= TeleportModeActivate;
        teleportActivationReference.action.canceled -= TeleportModeCancel;
    }
    private void OnEnable()
    {
        DeactivateTeleporter();
    }

    private void OnDisable()
    {
        OnTeleportActivate?.Invoke();
    }

    // Called when you want to teleport and revert back to base controllers
    private void TeleportModeCancel(InputAction.CallbackContext context)
    {
        // Wait 0.1 seconds before reverting
        Invoke("DeactivateTeleporter", .1f);
    }

    private void DeactivateTeleporter()
    {
        OnTeleportCancel?.Invoke();
    }

    // Called when you want to activate teleport mode
    private void TeleportModeActivate(InputAction.CallbackContext context)
    {
        OnTeleportActivate?.Invoke();
    }
}
