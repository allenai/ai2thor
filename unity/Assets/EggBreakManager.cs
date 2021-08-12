using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.SceneManagement;

public class EggBreakManager : MonoBehaviour {
    [SerializeField] private InputActionAsset actionAsset;
    [SerializeField] XRRayInteractor leftHand;
    [SerializeField] XRRayInteractor rightHand;
    [SerializeField]
    private GameObject PrefabToSwapTo = null;
    private bool broken = false;

    // Start is called before the first frame update
    void Start() {
        //TODO remove
        //GameObject egg = GameObject.Find("Egg_cf1753df");
        //break_egg(egg);

    }

    void break_egg(GameObject egg) {
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        foreach (Transform t in gameObject.transform) {
            t.gameObject.SetActive(false);
        }
        Vector3 position = egg.transform.position;
        //Quaternion rotation = egg.transform.rotation;
        Quaternion rotation = Quaternion.Euler(0, 0, 0);
        GameObject resultObject = Instantiate(PrefabToSwapTo, position, rotation);
        broken = true;
        //TODO do we need these?
        //foreach (Rigidbody subRb in resultObject.GetComponentsInChildren<Rigidbody>()) {
        //    subRb.velocity = rb.velocity * 0.4f;
        //    subRb.angularVelocity = rb.angularVelocity * 0.4f;
        //}

        //rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        //rb.isKinematic = true;
    }
    
    // Update is called once per frame
    void Update() {
        GameObject egg = GameObject.Find("Egg_cf1753df");
        Vector3 egg_position = egg.transform.position;
        GameObject pan = GameObject.Find("Pan_da081f05");
        Vector3 pan_position = pan.transform.position;
        float distance = Vector3.Distance(egg_position, pan_position);
        //Debug.Log(distance);
        if (distance < 0.1) {
            break_egg(egg);
        }

    }
}