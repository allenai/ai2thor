using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class InteractionManagerThingMattFixIt : MonoBehaviour
{
    [SerializeField] private InputActionAsset actionAsset;
    [SerializeField] XRRayInteractor leftHand;
    [SerializeField] XRRayInteractor rightHand;
    // Start is called before the first frame update
    void Start()
    {
        var leftHand = actionAsset.FindActionMap("XRI LeftHand").FindAction("Select");//maybe activate?
        leftHand.Enable();
        leftHand.performed += doTheThing;

        var rightHand = actionAsset.FindActionMap("XRI RightHand").FindAction("Select");//maybe activate?
        rightHand.Enable();
        rightHand.performed += doTheThing;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void doTheThing(InputAction.CallbackContext context)
    {
        RaycastHit hit;

        if(leftHand.GetCurrentRaycastHit(out hit))
        {
            if(hit.transform.GetComponent<ThingToMove>())
            {
                ThingToMove ttm = hit.transform.GetComponent<ThingToMove>();
                //ttm.thingToMove.transform.rotate..do something
            }
        }

        if(rightHand.GetCurrentRaycastHit(out hit))
        {
            if(hit.transform.GetComponent<ThingToMove>())
            {
                ThingToMove ttm = hit.transform.GetComponent<ThingToMove>();
                //do the thing
            }
        }
    }
}
