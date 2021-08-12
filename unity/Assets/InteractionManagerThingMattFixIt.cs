using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class InteractionManagerThingMattFixIt : MonoBehaviour {

    [SerializeField] private InputActionAsset actionAsset;
    [SerializeField] XRRayInteractor leftHand;
    [SerializeField] XRRayInteractor rightHand;

    // Start is called before the first frame update
    void Start() {
        var leftHand = actionAsset.FindActionMap("XRI LeftHand").FindAction("Select");//maybe activate?
        leftHand.Enable();
        leftHand.performed += doTheThing;

        var rightHand = actionAsset.FindActionMap("XRI RightHand").FindAction("Select");//maybe activate?
        rightHand.Enable();
        rightHand.performed += doTheThing;
    }

    // Update is called once per frame
    void Update() { }

    private void toggleOpen(ThingToMove ttm) {
        if (ttm.shouldMove) {
            ttm.thingToMove.transform.position = ttm.isOpen ? ttm.closePosition : ttm.openPosition;
        }
        if (ttm.shouldRotate) {
            ttm.thingToMove.transform.rotation = Quaternion.Euler(
                ttm.shouldRotate ? ttm.closeRotation : ttm.openRotation
            );
        }
        ttm.isOpen = !ttm.isOpen;
    }

    private void doTheThing(InputAction.CallbackContext context) {
        RaycastHit hit;

        if (
            leftHand.GetCurrentRaycastHit(out hit)
            && hit.transform.GetComponent<ThingToMove>()
        ) {
            ThingToMove ttm = hit.transform.GetComponent<ThingToMove>();
            toggleOpen(ttm: ttm);
            ttm.thingToMove.SetActive(false);
        }

        if (
            rightHand.GetCurrentRaycastHit(out hit)
            && hit.transform.GetComponent<ThingToMove>()
        ) {
            ThingToMove ttm = hit.transform.GetComponent<ThingToMove>();
            toggleOpen(ttm: ttm);
            ttm.thingToMove.SetActive(false);
        }
    }
}
