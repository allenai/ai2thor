using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Manipulator_Clamp : MonoBehaviour {
    Vector3[] currentCoordinates = new Vector3[2];
    Vector3[] prevCoordinates = new Vector3[2];
    Transform rootJoint;
    Transform shoulderJoint;
    Vector3 rootToWrist;
    Vector3 rootToShoulder;
    Vector3 shoulderToWrist;

    void Start() {
        currentCoordinates[0] = transform.position;
        currentCoordinates[1] = transform.eulerAngles;
        prevCoordinates = currentCoordinates;

        foreach (Transform transform in transform.parent.parent.parent) {
            if (transform.name == "robot_arm_1_jnt") {
                rootJoint = transform;
            }
        }

        foreach (Transform transform in rootJoint) {
            if (transform.name == "robot_arm_2_jnt") {
                shoulderJoint = transform;
            }
        }

    }

    void Update() {
        // Debug.Log("Checking for hemisphere change!");
        if (transform.position != prevCoordinates[0]) {
            // Clamp to front hemisphere
            rootToWrist = rootJoint.InverseTransformPoint(transform.position);

            // Debug.Log("Z from " + rootJoint.gameObject.name + " to " + transform.gameObject.name + ": " + rootToWrist);
            rootToShoulder = shoulderJoint.localPosition;
            // Debug.Log("Z from " + rootJoint.gameObject.name + " to shoulder: " + shoulderJoint.gameObject.name + ": " + rootToShoulder);
            if (rootToWrist.z - 0.01f <= rootToShoulder.z) {
                Debug.Log("Wrist is behind shoulder!");
                transform.position += rootJoint.TransformDirection(Vector3.forward * (rootToShoulder.z - rootToWrist.z + 0.01f));
            }

            rootToWrist = rootJoint.InverseTransformPoint(transform.position);
            // Clamp manipulator to radius
            // Maximum shoulder-to-wrist length is ~0.6335!!!
            shoulderToWrist = rootToWrist - rootToShoulder;
            // Debug.Log(shoulderToWrist.magnitude + " vs " + shoulderJoint.InverseTransformPoint(transform.position).magnitude);
            if (shoulderToWrist.sqrMagnitude >= Mathf.Pow(0.6335f, 2) && shoulderToWrist.z > 0) {
                // Debug.Log("Arm is overreaching!");
                transform.position += rootJoint.TransformDirection((shoulderToWrist.normalized * 0.6325f) - shoulderToWrist);
            }
        }

        // Clamp wrist not to bend back on itself....ooh, this might be tricky with the forearm constantly changing...
        // if (transform.eulerAngles != prevCoordinates[1])
        //{
        //
        //}

        prevCoordinates[0] = transform.position;
        prevCoordinates[1] = transform.localEulerAngles;
    }
}
