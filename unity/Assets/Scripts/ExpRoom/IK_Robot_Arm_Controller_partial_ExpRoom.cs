using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Priority_Queue;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.ImageEffects;
using UnityStandardAssets.Utility;
using RandomExtensions;

public partial class IK_Robot_Arm_Controller : MonoBehaviour {

    public bool AttachObjectToArmWithFixedJoint(
        SimObjPhysics target,
        float breakForce,
        float breakTorque
    ) {
        foreach (FixedJoint fj in magnetSphere.gameObject.GetComponents<FixedJoint>()) {
            if (fj.connectedBody.gameObject == target.gameObject) {
                return true;
            }
        }

        FixedJoint newFj = magnetSphere.gameObject.AddComponent(typeof(FixedJoint)) as FixedJoint;

        newFj.connectedBody = target.GetComponent<Rigidbody>();
        newFj.breakForce = breakForce;
        newFj.breakTorque = breakTorque;
        newFj.enableCollision = false;
        return true;
    }

    public bool IsObjectAttachedWithFixedJoint(
        SimObjPhysics target
    ) {
        foreach (FixedJoint fj in magnetSphere.gameObject.GetComponents<FixedJoint>()) {
            if (fj.connectedBody.gameObject == target.gameObject) {
                return true;
            }
        }
        return false;
    }

    public bool BreakFixedJoints() {
        foreach (FixedJoint fj in magnetSphere.gameObject.GetComponents<FixedJoint>()) {
            Destroy(fj);
        }
        return true;
    }

}