using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Align_to_Joint_Normal : MonoBehaviour {
    public Transform root;
    public Transform mid;
    public Transform tip;
    public bool isMidJointAngler;
    Vector3 joint1;
    Vector3 joint2;
    Vector3 jointNormal, jointTangent;
    Transform positionAlignedJoint;

    void Update() {
        joint1 = mid.position - root.position;
        joint2 = tip.position - mid.position;
        jointNormal = Vector3.Cross(joint1, joint2);

        if (isMidJointAngler == true) {
            positionAlignedJoint = tip;
            jointTangent = Vector3.Cross(joint2, jointNormal);
        } else {
            positionAlignedJoint = mid;
            jointTangent = Vector3.Cross(joint1, jointNormal);
        }

        transform.LookAt(positionAlignedJoint, jointTangent);
    }
}