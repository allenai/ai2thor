using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Handles collider stretching and arm extension animation
public class ArticulatedArmExtender : MonoBehaviour {

    public Transform arm2, arm3, arm4;
    public Transform[] myColliders;
    public float initialZPos;
    public float arm2InitialZPos;
    public float arm3InitialZPos;
    public float arm4InitialZPos;

    public float scaleMultiplier = 1f;

    public void Start() {
        initialZPos = this.gameObject.transform.localPosition.z;
        arm2InitialZPos = arm2.gameObject.transform.localPosition.z;
        arm3InitialZPos = arm3.gameObject.transform.localPosition.z;
        arm4InitialZPos = arm4.gameObject.transform.localPosition.z;
    }

    private void scaleColliders() {
        var currentZPos = this.gameObject.transform.localPosition.z;
        foreach (Transform go in myColliders) {
            go.localScale = new Vector3(go.localScale.x, go.localScale.y, 1 + (currentZPos - initialZPos) * scaleMultiplier);
        }
    }

    private void animate(float currentZPos) {
        //Extend each part of arm by one-quarter of extension length, in local z-direction, also adds offset from origin of arm_1 to each arm position
        float armExtensionLength = currentZPos - initialZPos;
        arm2.localPosition = new Vector3(0, 0, 1 * (armExtensionLength / 4) + arm2InitialZPos); //+ 0.01300028f);
        arm3.localPosition = new Vector3(0, 0, 2 * (armExtensionLength / 4) + arm3InitialZPos); //0.01300049f);
        arm4.localPosition = new Vector3(0, 0, 3 * (armExtensionLength / 4) + arm4InitialZPos); //0.01300025f);
        // arm5.localPosition = new Vector3 (0, 0, 4 * (armExtensionLength / 4) + 0.0117463f);
    }

    //move these to update because there are cases where even though the arm target has stopped updating
    //from `ControlJointFromAction`, there is still arm joint movement due to residual momentum
    //and the colliders would start to be offset because the initial z position no longer matched
    public void Update() {
        Extend();
    }
    public void Extend() {
        scaleColliders();
        var currentZPos = this.gameObject.transform.localPosition.z;
        animate(currentZPos);
    }
}
