using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.SceneManagement;

public class StoveTurnOnManager : MonoBehaviour
{
    [SerializeField] private InputActionAsset actionAsset;
    [SerializeField] XRRayInteractor leftHand;
    [SerializeField] XRRayInteractor rightHand;


    // Start is called before the first frame update
    void Start()
    {
        var leftHand = actionAsset.FindActionMap("XRI LeftHand").FindAction("Select");//maybe activate?
        leftHand.Enable();
        leftHand.performed += checkIfTurningStoveOn;

        var rightHand = actionAsset.FindActionMap("XRI RightHand").FindAction("Select");//maybe activate?
        rightHand.Enable();
        rightHand.performed += checkIfTurningStoveOn;
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void turn_on_flames() {
        GameObject[] fires;
        fires = GameObject.FindGameObjectsWithTag("Fire");
        //Debug.Log("objects counted");
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject fire in allObjects) {
            if (fire.tag == "Fire") {
                //Debug.Log("objects found");
                fire.SetActive(true);
            }
        }
    }

    private void checkIfTurningStoveOn(InputAction.CallbackContext context)
    {
        RaycastHit hit;

        if (leftHand.GetCurrentRaycastHit(out hit))
        {

            if (hit.transform.GetComponent<StoveType>())
            {
                turn_on_flames();
            }
        }

        if (rightHand.GetCurrentRaycastHit(out hit)) {

            if (hit.transform.GetComponent<StoveType>()) {
                turn_on_flames();
            }
        }
    }
}
