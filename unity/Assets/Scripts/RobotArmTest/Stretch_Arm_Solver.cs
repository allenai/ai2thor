using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stretch_Arm_Solver : MonoBehaviour {
    public Transform armRoot, armTarget;
    Transform arm1, arm2, arm3, arm4, arm5, wrist1;
    float armExtensionLength;

    #if UNITY_EDITOR
        void Update() {
            ManipulateStretchArm();
        }
    #endif

    public void ManipulateStretchArm() {
        arm1 = armRoot.GetChild(0);
        arm2 = arm1.GetChild(0);
        arm3 = arm2.GetChild(0);
        arm4 = arm3.GetChild(0);
        arm5 = arm4.GetChild(0);
        wrist1 = arm5.GetChild(0);

        //Adjust height
        armRoot.position = new Vector3(armRoot.position.x, armTarget.position.y + 0.1643845f, armRoot.position.z);

        //Adjust extension
        armExtensionLength = armRoot.InverseTransformPoint(armTarget.position).x - 0.2787063f;

        //Extend each part of arm by one-quarter of extension length, in z-direction
        arm2.localPosition = new Vector3 (arm2.localPosition.x, arm2.localPosition.y, armExtensionLength / 4 + 0.01300028f);
        arm3.localPosition = new Vector3 (arm3.localPosition.x, arm2.localPosition.y, armExtensionLength / 4 + 0.01300049f);
        arm4.localPosition = new Vector3 (arm4.localPosition.x, arm2.localPosition.y, armExtensionLength / 4 + 0.01300025f);
        arm5.localPosition = new Vector3 (arm5.localPosition.x, arm2.localPosition.y, armExtensionLength / 4 - 0.0150049f);

        //Adjust rotation
        wrist1.eulerAngles = new Vector3 (0, armTarget.eulerAngles.y, 0);

        //Reign in target if it's out-of-range of robot's extension
        if (armTarget.localPosition.x != 0)
            armTarget.localPosition = new Vector3(0, armTarget.localPosition.y, armTarget.localPosition.z);

        else if (armTarget.localPosition.y < 0) {
            armTarget.localPosition = new Vector3(armTarget.localPosition.x, 0f, armTarget.localPosition.z);
        }

        else if (armTarget.localPosition.y > 1.045f) {
            armTarget.localPosition = new Vector3(armTarget.localPosition.x, 1.045f, armTarget.localPosition.z);
        }

        else if (armTarget.localPosition.z < 0) {
            armTarget.localPosition = new Vector3(armTarget.localPosition.x, armTarget.localPosition.y, 0f);
        }

        else if (armTarget.localPosition.z > 0.8065f) {
            armTarget.localPosition = new Vector3(armTarget.localPosition.x, armTarget.localPosition.y, 0.8065f);
        }

        // ONLY INCLUDE THIS IF THERE'S SOME WAY FOR A USER TO EVEN ROTATE STRETCH MANIPULATOR ALONG X AND Z AXES
        else if (Mathf.Abs(armTarget.localEulerAngles.x) + Mathf.Abs(armTarget.localEulerAngles.z) != 0) {
            armTarget.localEulerAngles = new Vector3(0, armTarget.localEulerAngles.y, 0);
        }
    }
}