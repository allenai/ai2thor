using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
using System.Linq;
public partial class Stretch_Robot_Arm_Controller : ArmController {
    
    [SerializeField]
    private Transform armBase, handCameraTransform, FirstJoint;

    private PhysicsRemoteFPSAgentController PhysicsController;

    //Distance from joint containing gripper camera to armTarget
    private Vector3 WristToManipulator = new Vector3 (0, -0.09872628f, 0);

    private Stretch_Arm_Solver solver;

    public override Transform pickupParent() {
        return magnetSphere.transform;
    }

    public override Vector3  wristSpaceOffsetToWorldPos(Vector3 offset) {
        return handCameraTransform.TransformPoint(offset) - handCameraTransform.position + WristToManipulator;
    }
    public override Vector3 armBaseSpaceOffsetToWorldPos(Vector3 offset) {
        return this.transform.TransformPoint(offset) - this.transform.position;
    }

    public override Vector3 pointToWristSpace(Vector3 point) {
        return handCameraTransform.TransformPoint(point) + WristToManipulator;
    }
    public override Vector3 pointToArmBaseSpace(Vector3 point) {
        return armBase.transform.TransformPoint(point);
    }

    public override void ContinuousUpdate(float fixedDeltaTime) {
        solver.ManipulateStretchArm();
    }

    public override GameObject GetArmTarget() {
        return armTarget.gameObject;
    }

     public override ActionFinished FinishContinuousMove(BaseFPSAgentController controller) {
        // TODO: does not do anything need to change Continuous Move to call this instead of continuousMoveFinish
        return ActionFinished.Success;
     }

    void Start() {
        this.collisionListener = this.GetComponentInParent<CollisionListener>();
        this.collisionListener.registerAllChildColliders();

        List<CapsuleCollider> armCaps = new List<CapsuleCollider>();
        List<BoxCollider> armBoxes = new List<BoxCollider>();

        // get references to all colliders in arm. Remove trigger colliders so there are no duplicates when using these as reference for
        // overlap casts since the trigger colliders are themselves duplicates of the nontrigger colliders.
        armCaps.AddRange(gameObject.GetComponentsInChildren<CapsuleCollider>());
        armBoxes.AddRange(gameObject.GetComponentsInChildren<BoxCollider>());

        // clean up arm colliders, removing triggers
        List<CapsuleCollider> cleanedCaps = new List<CapsuleCollider>();
        foreach (CapsuleCollider c in armCaps) {
            if (!c.isTrigger) {
                cleanedCaps.Add(c);
            }
        }

        ArmCapsuleColliders = cleanedCaps.ToArray();

        List<BoxCollider> cleanedBoxes = new List<BoxCollider>();
        foreach (BoxCollider b in armBoxes) {
            if (!b.isTrigger) {
                cleanedBoxes.Add(b);
            }
        }

        ArmBoxColliders = cleanedBoxes.ToArray();

        // TODO: Currently explicitly ignoring all arm self collisions (for efficiency)!
        var colliders = this.GetComponentsInChildren<Collider>();
        foreach (Collider c0 in colliders) {
            foreach (Collider c1 in colliders) {
                Physics.IgnoreCollision(c0, c1);
            }
        }

        solver = this.gameObject.GetComponentInChildren<Stretch_Arm_Solver>();
    }

    protected override void lastStepCallback() {
        Vector3 pos = handCameraTransform.transform.position + WristToManipulator;
        Quaternion rot = handCameraTransform.transform.rotation;
        armTarget.position = pos;
        armTarget.rotation = rot;
    }

    public override ArmMetadata GenerateMetadata() {
        ArmMetadata meta = new ArmMetadata();
        // meta.handTarget = armTarget.position;
        Transform joint = transform;
        List<JointMetadata> joints = new List<JointMetadata>();

        // Declare variables used for processing metadata
        Transform parentJoint;
        float angleRot;
        Vector3 vectorRot;
        Quaternion currentRotation;

        // Assign joint metadata to remaining joints, which all have identical hierarchies
        for (int i = 1; i <= 8; i++) {
            if (i == 1) {
                joint = joint.Find("stretch_robot_lift_jnt");
            }
            else if (i <= 6) {
                joint = joint.Find("stretch_robot_arm_" + (i-1) + "_jnt");
            }
            else {
                joint = joint.Find("stretch_robot_wrist_" + (i-6) + "_jnt");
            }

            JointMetadata jointMeta = new JointMetadata();

            // JOINT NAME
            jointMeta.name = joint.name;

            // POSITIONS //

            // WORLD RELATIVE POSITION
            jointMeta.position = joint.position;

            // ROOT-JOINT RELATIVE POSITION
            // Parent-relative position of joint is meaningless because it never changes relative to its parent joint, so we use rootRelative instead
            jointMeta.rootRelativePosition = armBase.InverseTransformPoint(joint.position);

            // ROTATIONS //

            // WORLD RELATIVE ROTATION
            currentRotation = joint.rotation;

            // Check that world-relative rotation is angle-axis-notation-compatible
            if (currentRotation != new Quaternion(0, 0, 0, -1)) {
                currentRotation.ToAngleAxis(angle: out angleRot, axis: out vectorRot);
                jointMeta.rotation = new Vector4(vectorRot.x, vectorRot.y, vectorRot.z, angleRot);
            } else {
                jointMeta.rotation = new Vector4(1, 0, 0, 0);
            }

            // ROOT-JOINT RELATIVE ROTATION
            // Grab rotation of current joint's angler relative to root joint
            currentRotation = Quaternion.Inverse(armBase.rotation) * joint.rotation;

            // Check that root-relative rotation is angle-axis-notation-compatible
            if (currentRotation != new Quaternion(0, 0, 0, -1)) {
                currentRotation.ToAngleAxis(angle: out angleRot, axis: out vectorRot);
                jointMeta.rootRelativeRotation = new Vector4(vectorRot.x, vectorRot.y, vectorRot.z, angleRot);
            } else {
                jointMeta.rootRelativeRotation = new Vector4(1, 0, 0, 0);
            }

            // PARENT-JOINT RELATIVE ROTATION
            if (i != 1) {
                parentJoint = joint.parent;

                // Grab rotation of current joint's angler relative to parent joint's angler
                currentRotation = Quaternion.Inverse(parentJoint.rotation) * joint.rotation;

                // Check that parent-relative rotation is angle-axis-notation-compatible
                if (currentRotation != new Quaternion(0, 0, 0, -1)) {
                    currentRotation.ToAngleAxis(angle: out angleRot, axis: out vectorRot);
                    jointMeta.localRotation = new Vector4(vectorRot.x, vectorRot.y, vectorRot.z, angleRot);
                } else {
                    jointMeta.localRotation = new Vector4(1, 0, 0, 0);
                }
            } else {
                // Special case for stretch_robot_lift_jnt because it has no parent-joint
                jointMeta.localRotation = jointMeta.rootRelativeRotation;
            }

            joints.Add(jointMeta);
        }

        meta.joints = joints.ToArray();

        // metadata for any objects currently held by the hand on the arm
        // note this is different from objects intersecting the hand's sphere,
        // there could be a case where an object is inside the sphere but not picked up by the hand
        List<string> heldObjectIDs = new List<string>();
        if (heldObjects != null) {
            foreach (SimObjPhysics sop in heldObjects.Keys) {
                heldObjectIDs.Add(sop.objectID);
            }
        }

        meta.heldObjects = heldObjectIDs;
        meta.handSphereCenter = magnetSphere.transform.TransformPoint(magnetSphere.center);
        meta.handSphereRadius = magnetSphere.radius;
        List<SimObjPhysics> objectsInMagnet = WhatObjectsAreInsideMagnetSphereAsSOP(false);
        meta.pickupableObjects = objectsInMagnet.Where(
            x => x.PrimaryProperty == SimObjPrimaryProperty.CanPickup
        ).Select(x => x.ObjectID).ToList();
        meta.touchedNotHeldObjects = objectsInMagnet.Select(x => x.ObjectID).ToList();
        return meta;
    }

#if UNITY_EDITOR
    public class GizmoDrawCapsule {
        public Vector3 p0;
        public Vector3 p1;
        public float radius;
    }

    List<GizmoDrawCapsule> debugCapsules = new List<GizmoDrawCapsule>();

    private void OnDrawGizmos() {
        if (debugCapsules.Count > 0) {
            foreach (GizmoDrawCapsule thing in debugCapsules) {
                Gizmos.DrawWireSphere(thing.p0, thing.radius);
                Gizmos.DrawWireSphere(thing.p1, thing.radius);
            }
        }
    }
#endif
}
