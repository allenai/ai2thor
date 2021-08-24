using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stretch_Arm_Solver : MonoBehaviour {
    public Transform armTarget;
    Transform armRoot, arm2, arm3, arm4, arm5, wrist1;
    float armExtensionLength;

    #if UNITY_EDITOR
        void Update() {
            ManipulateStretchArm();
        }
    #endif

        void Awake() {
            armRoot = transform;
        }

        void ManipulateStretchArm() {
            arm2 = armRoot.GetChild(0).GetChild(0);
            arm3 = arm2.GetChild(0);
            arm4 = arm3.GetChild(0);
            arm5 = arm4.GetChild(0);
            wrist1 = arm5.GetChild(0);
            Debug.Log(wrist1 + " okay! " + armTarget);

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
        }
}