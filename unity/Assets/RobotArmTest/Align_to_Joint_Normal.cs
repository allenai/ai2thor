using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Align_to_Joint_Normal : MonoBehaviour
{
    public Transform root;
    public Transform mid;
    public Transform tip;
    public bool isMidJointAngler;
    Vector3 joint1;
    Vector3 joint2;
    Vector3 jointNormal;
    Transform positionAlignedJoint;

    void Update()
    {
        joint1 = mid.position - root.position;
        joint2 = tip.position - mid.position;
        jointNormal = Vector3.Cross(joint1, joint2);

        if (isMidJointAngler == true)
        {
            positionAlignedJoint = tip;
        }

        else
        {
            positionAlignedJoint = mid;
        }

        transform.LookAt(positionAlignedJoint, jointNormal);
    }
}