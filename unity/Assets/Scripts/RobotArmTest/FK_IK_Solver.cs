using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FK_IK_Solver : MonoBehaviour {
    public bool isIKDriven;
    public Transform armRoot, armShoulder, armElbow, armWrist, armHand;
    float bone1Length, bone2Length, bone3Length;
    public Transform FKRootTarget, FKShoulderTarget, FKElbowTarget, FKWristTarget;
    public Transform IKTarget, IKPole;
    Transform IKHint;
    // public Transform centerTest, hintProjectionTest;
    // public Transform bone1Extents, bone3Extents;
    float p1x, p1y, p1z, p2x, p2y, p2z, p3x, p3y, p3z, overlapA, overlapB, overlapC, overlapD, overlapParameter, overlapRadius;
    Vector3 overlapCenter, hintProjection, elbowPosition;

    // this must be Awake vs Start since when the Arm is activated, Start() will not have been called
    void Awake() {
        bone1Length = (armShoulder.position - armRoot.position).magnitude;
        bone2Length = (armElbow.position - armShoulder.position).magnitude;
        bone3Length = (armWrist.position - armElbow.position).magnitude;
        IKHint = IKPole.GetChild(0);
    }

#if UNITY_EDITOR || UNITY_ANDROID
    // Uncomment this when testing in Unity
    void Update() {
        ManipulateArm();
    }
#endif

    public void ManipulateArm() {
        // Check if arm is driven by IK or FK
        if (isIKDriven == true) {
            // Adjust pole position 
            IKPole.parent.position = IKTarget.position;
            IKPole.parent.forward = IKTarget.position - armShoulder.position;

            // Check if manipulator location is reachable by arm, with 1e-5 bias towards hyperextension when comparing values, to account for rounding errors
            if ((IKTarget.position - armShoulder.position).magnitude + 1e-5 < bone2Length + bone3Length) {
                // Define variables to optimize logic
                p1x = armShoulder.position.x;
                p1y = armShoulder.position.y;
                p1z = armShoulder.position.z;
                p2x = IKTarget.position.x;
                p2y = IKTarget.position.y;
                p2z = IKTarget.position.z;
                p3x = IKHint.position.x;
                p3y = IKHint.position.y;
                p3z = IKHint.position.z;

                // Define plane created by ring of overlap between spheres of extent for both shoulder-to-elbow and wrist-to-elbow bone-lengths at shoulder's and wrist's current positions, respectively
                overlapA = 2 * p2x - 2 * p1x;
                overlapB = 2 * p2y - 2 * p1y;
                overlapC = 2 * p2z - 2 * p1z;
                overlapD = Mathf.Pow(bone2Length, 2) - Mathf.Pow(bone3Length, 2) - Mathf.Pow(p1x, 2) - Mathf.Pow(p1y, 2) - Mathf.Pow(p1z, 2) + Mathf.Pow(p2x, 2) + Mathf.Pow(p2y, 2) + Mathf.Pow(p2z, 2);

                // Find center of ring of overlap by projecting shoulder position onto overlap-plane, since the center will always be on the direct line between shoulder and wrist, which has the same direction vector as the overlap-plane normal
                overlapParameter = FindParameter(p1x, p1y, p1z, overlapA, overlapB, overlapC, overlapD);
                overlapCenter = new Vector3(p1x + overlapA * overlapParameter, p1y + overlapB * overlapParameter, p1z + overlapC * overlapParameter);

                // Find radius of ring of overlap via Pythagorean Theorem using shoulder-to-elbow bone length as hypotenuse, and shoulder-to-overlap-center distance as adjacent
                overlapRadius = Mathf.Sqrt(Mathf.Pow(bone2Length, 2) - (overlapCenter - armShoulder.position).sqrMagnitude);

                // Find elbow position by projecting IK_Hint position onto overlap-plane, and then moving the ring of overlap's center-point in the ring-center-to-projected-IK_Hint direction vector by a magnitude of the ring's radius
                overlapParameter = FindParameter(p3x, p3y, p3z, overlapA, overlapB, overlapC, overlapD);
                hintProjection = new Vector3(p3x + overlapA * overlapParameter, p3y + overlapB * overlapParameter, p3z + overlapC * overlapParameter);
                elbowPosition = overlapCenter + overlapRadius * (hintProjection - overlapCenter).normalized;

                // Move joint transforms to calculated positions
                armElbow.position = elbowPosition;
                armWrist.position = IKTarget.position;
                armWrist.rotation = IKTarget.rotation;
            } else {
                Vector3 armDirectionVector = (IKTarget.position - armShoulder.position).normalized;
                armElbow.position = armShoulder.position + armDirectionVector * bone1Length;
                armWrist.position = armElbow.position + armDirectionVector * bone2Length;
                armWrist.rotation = IKTarget.rotation;
            }
        } else {
            armRoot.position = FKRootTarget.position;
            armRoot.rotation = FKRootTarget.rotation;
            armShoulder.position = FKShoulderTarget.position;
            armShoulder.rotation = FKShoulderTarget.rotation;
            armElbow.position = FKElbowTarget.position;
            armElbow.rotation = FKElbowTarget.rotation;
            armWrist.position = FKWristTarget.position;
            armWrist.rotation = FKWristTarget.rotation;
        }

        // Align individual arm components to their correct joint-angles 
        AlignToJointNormal(armRoot.GetChild(1), armRoot, armShoulder, armElbow, false);
        AlignToJointNormal(armShoulder.GetChild(0), armRoot, armShoulder, armElbow, true);
        AlignToJointNormal(armShoulder.GetChild(1), armShoulder, armElbow, armWrist, false);
        AlignToJointNormal(armElbow.GetChild(0), armShoulder, armElbow, armWrist, true);
        AlignToJointNormal(armElbow.GetChild(1), armElbow, armWrist, armHand, false);
        AlignToJointNormal(armWrist.GetChild(0), armElbow, armWrist, armHand, true);
    }

    float FindParameter(float p0x, float p0y, float p0z, float a, float b, float c, float d) {
        float parameter = (d - a * p0x - b * p0y - c * p0z) / (Mathf.Pow(a, 2) + Mathf.Pow(b, 2) + Mathf.Pow(c, 2));
        return parameter;
    }

    void AlignToJointNormal(Transform armComponent, Transform root, Transform mid, Transform tip, bool isMidJointAngled) {
        Vector3 bone1 = mid.position - root.position;
        Vector3 bone2 = tip.position - mid.position;
        Vector3 jointNormal = Vector3.Cross(bone1, bone2);
        Vector3 jointTangent;

        Transform positionAlignedJoint;

        if (isMidJointAngled == true) {
            positionAlignedJoint = tip;
            jointTangent = Vector3.Cross(bone2, jointNormal);
        } else {
            positionAlignedJoint = mid;
            jointTangent = Vector3.Cross(bone1, jointNormal);
        }

        armComponent.LookAt(positionAlignedJoint, jointTangent);
    }
}
