using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.SceneManagement;

public class RoomChangeManager : MonoBehaviour
{
    [SerializeField] private InputActionAsset actionAsset;
    [SerializeField] XRRayInteractor leftHand;
    [SerializeField] XRRayInteractor rightHand;


    // Start is called before the first frame update
    void Start()
    {
        var leftHand = actionAsset.FindActionMap("XRI LeftHand").FindAction("Select");//maybe activate?
        leftHand.Enable();
        leftHand.performed += checkIfGrabbingDoor;

        var rightHand = actionAsset.FindActionMap("XRI RightHand").FindAction("Select");//maybe activate?
        rightHand.Enable();
        rightHand.performed += checkIfGrabbingDoor;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void checkIfGrabbingDoor(InputAction.CallbackContext context)
    {
        RaycastHit hit;

        if(leftHand.GetCurrentRaycastHit(out hit))
        {
            if(hit.transform.GetComponent<ChangeRoom>())
            {
                ChangeRoom c = hit.transform.GetComponent<ChangeRoom>();
                UnityEngine.SceneManagement.SceneManager.LoadScene(c.SceneToChangeTo, LoadSceneMode.Single);
            }
        }

        if(rightHand.GetCurrentRaycastHit(out hit))
        {
            if(hit.transform.GetComponent<ChangeRoom>())
            {
                ChangeRoom c = hit.transform.GetComponent<ChangeRoom>();
                UnityEngine.SceneManagement.SceneManager.LoadScene(c.SceneToChangeTo, LoadSceneMode.Single);
            }
        }
    }
}
