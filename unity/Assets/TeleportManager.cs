using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class TeleportManager : MonoBehaviour
{
    [SerializeField] private InputActionAsset actionAsset;
    [SerializeField] XRRayInteractor teleportRay;
    [SerializeField] XRRayInteractor grabRay;
    [SerializeField] TeleportationProvider provider;
    private InputAction _thumbstick;
    private bool _teleRayActive;


    // Start is called before the first frame update
    void Start()
    {
        teleportRay.enabled = false;

        var activate = actionAsset.FindActionMap("XRI LeftHand").FindAction("Teleport Mode Activate");
        activate.Enable();
        activate.performed += OnTeleportActivate;

        var cancel = actionAsset.FindActionMap("XRI LeftHand").FindAction("Teleport Mode Cancel");
        cancel.Enable();
        cancel.performed += OnTeleportCancel;

        _thumbstick = actionAsset.FindActionMap("XRI LeftHand").FindAction("Move");
        _thumbstick.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        //do nothing if trying to pick tele spot
        if (!_teleRayActive)
            return;

        //do nothing if thumb active
        if(_thumbstick.triggered)
            return;
        
        if(!teleportRay.GetCurrentRaycastHit(out RaycastHit hit))
        {
            teleportRay.enabled = false;
            _teleRayActive = false;
            return;
        }

        TeleportRequest req = new TeleportRequest()
        {
            destinationPosition = hit.point,
        };

        provider.QueueTeleportRequest(req);
        teleportRay.enabled = false;
        _teleRayActive = false;
        grabRay.enabled = true;

    }

    private void OnTeleportActivate(InputAction.CallbackContext context)
    {
        teleportRay.enabled = true;
        grabRay.enabled = false;
        _teleRayActive = true;
    }

    private void OnTeleportCancel(InputAction.CallbackContext context)
    {
        teleportRay.enabled = false;
        grabRay.enabled = true;
        _teleRayActive = false;
    }
}
