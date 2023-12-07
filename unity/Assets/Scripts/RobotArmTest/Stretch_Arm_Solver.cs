using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stretch_Arm_Solver : MonoBehaviour {
    public Transform armRoot, armTarget;
    Transform arm1, arm2, arm3, arm4, arm5, wrist1;
    float liftInitialLocalHeightOffset = 0f, armHeight, armExtensionLength; 

        void Update() {
            ManipulateStretchArm();
        }

    public void ManipulateStretchArm() {
        arm1 = armRoot.GetChild(0);
        arm2 = arm1.GetChild(0);
        arm3 = arm2.GetChild(0);
        arm4 = arm3.GetChild(0);
        arm5 = arm4.GetChild(0);
        wrist1 = arm5.GetChild(0);

        //Set height from target input, checking for overextension
        if (armTarget.localPosition.y < -0.056) {
            armHeight = -0.056f;
        }

        else if (armTarget.localPosition.y > 1.045f) {
            armHeight = 1.045f;
        }

        else {
            armHeight = armTarget.localPosition.y;
        }
        
        //Set arm extension from target input, checking for overextension
        if (armTarget.localPosition.z < 0) {
            armExtensionLength = 0f;
        }
        
        else if (armTarget.localPosition.z > 0.8065f) {
            armExtensionLength = 0.8065f;
        }
        
        else {
            armExtensionLength = armTarget.localPosition.z;
        }

        //Move Arm Base height
        armRoot.localPosition = new Vector3(armRoot.localPosition.x, armHeight + liftInitialLocalHeightOffset, armRoot.localPosition.z);

        //Extend each part of arm by one-quarter of extension length, in local z-direction
        arm2.localPosition = new Vector3 (0, 0, armExtensionLength / 4 + 0.01300028f);
        arm3.localPosition = new Vector3 (0, 0, armExtensionLength / 4 + 0.01300049f);
        arm4.localPosition = new Vector3 (0, 0, armExtensionLength / 4 + 0.01300025f);
        arm5.localPosition = new Vector3 (0, 0, armExtensionLength / 4 + 0.0117463f);

        //Adjust rotation
        wrist1.eulerAngles = new Vector3 (0, armTarget.eulerAngles.y, 0);
    }
}