using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stretch_Arm_Solver : MonoBehaviour
{
    public Transform armRoot,
        armTarget;
    Transform arm1,
        arm2,
        arm3,
        arm4,
        arm5,
        wrist1;
    float liftInitialLocalHeightOffset = 0f,
        armHeight,
        armExtensionLength;
    private float lowerArmHeightLimit = -0.056f,
        upperArmHeightLimit = 1.045f,
        backArmExtensionLimit = 0f,
        frontArmExtensionLimit = 0.516f;

#if UNITY_EDITOR
    void Update()
    {
        ManipulateStretchArm();
    }
#endif

    public void ManipulateStretchArm()
    {
        arm1 = armRoot.GetChild(0);
        arm2 = arm1.GetChild(0);
        arm3 = arm2.GetChild(0);
        arm4 = arm3.GetChild(0);
        arm5 = arm4.GetChild(0);
        wrist1 = arm5.GetChild(0);

        // Set height from target input, checking for overextension
        armHeight = Mathf.Clamp(
            armTarget.localPosition.y,
            lowerArmHeightLimit,
            upperArmHeightLimit
        );

        // Set arm extension from target input, checking for overextension
        armExtensionLength = Mathf.Clamp(
            armTarget.localPosition.z,
            backArmExtensionLimit,
            frontArmExtensionLimit
        );

        // Move arm base height
        armRoot.localPosition = new Vector3(
            armRoot.localPosition.x,
            armHeight + liftInitialLocalHeightOffset,
            armRoot.localPosition.z
        );

        // Extend each part of arm by one-quarter of extension length, in local z-direction
        arm2.localPosition = new Vector3(0, 0, armExtensionLength / 4 + 0.01300028f);
        arm3.localPosition = new Vector3(0, 0, armExtensionLength / 4 + 0.01300049f);
        arm4.localPosition = new Vector3(0, 0, armExtensionLength / 4 + 0.01300025f);
        arm5.localPosition = new Vector3(0, 0, armExtensionLength / 4 + 0.0117463f);

        // Adjust rotation
        wrist1.eulerAngles = new Vector3(0, armTarget.eulerAngles.y, 0);
    }
}
