using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
using System.Linq;

public partial class IK_Robot_Arm_Controller : ArmController {
    [SerializeField]
    private Transform armBase, elbowTarget, handCameraTransform, FirstJoint;

    private PhysicsRemoteFPSAgentController PhysicsController;

    // dict to track which picked up object has which set of trigger colliders
    // which we have to parent and reparent in order for arm collision to detect
    [SerializeField]
    public new Dictionary<SimObjPhysics, HashSet<Collider>> heldObjects = new Dictionary<SimObjPhysics, HashSet<Collider>>();

    // private bool StopMotionOnContact = false;
    // Start is called before the first frame update

    private FK_IK_Solver solver;

    private float originToShoulderLength = 0f;

    private const float extendedArmLength = 0.6325f;

    public override GameObject GetArmTarget() {
        return armTarget.gameObject;
    }

     public override Transform pickupParent() {
        return magnetSphere.transform;
    }

    public override Vector3  wristSpaceOffsetToWorldPos(Vector3 offset) {
        return handCameraTransform.TransformPoint(offset) - handCameraTransform.TransformPoint(Vector3.zero);
    }
    public override Vector3 armBaseSpaceOffsetToWorldPos(Vector3 offset) {
        return this.transform.TransformPoint(offset) - this.transform.TransformPoint(Vector3.zero);
    }

    public override Vector3 pointToWristSpace(Vector3 point) {
        return handCameraTransform.TransformPoint(point);
    }
    public override Vector3 pointToArmBaseSpace(Vector3 point) {
        return this.transform.Find("robot_arm_FK_IK_rig").transform.TransformPoint(point);
    }

    public override void ContinuousUpdate(float fixedDeltaTime) {
        Debug.Log("manipulate Arm called from IK_Robot_Arm_Controller");
        solver.ManipulateArm();
    }

    public GameObject GetArmBase() {
        return armBase.gameObject;
    }

    
    public GameObject GetElbowTarget() {
        return elbowTarget.gameObject;
    }

    public GameObject GetMagnetSphere() {
        return magnetSphere.gameObject;
    }

    protected override void lastStepCallback() {
        Vector3 pos = handCameraTransform.transform.position;
        Quaternion rot = handCameraTransform.transform.rotation;
        armTarget.position = pos;
        armTarget.rotation = rot;
    }

    public override ActionFinished FinishContinuousMove(BaseFPSAgentController controller) {
        // TODO: does not do anything need to change Continuous Move to call this instead of continuousMoveFinish
        return ActionFinished.Success;
     }

   
    void Start() {
        // calculating based on distance from origin of arm to the 2nd joint, which will always be constant
        this.originToShoulderLength = Vector3.Distance(
            this.transform.FirstChildOrDefault(
                x => x.name == "robot_arm_2_jnt"
            ).position,
            this.transform.position
        );

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
        solver = this.gameObject.GetComponentInChildren<FK_IK_Solver>();
    }

    // Restricts front hemisphere for arm movement
    protected override bool validArmTargetPosition(Vector3 targetWorldPosition) {
        Vector3 targetShoulderSpace = (
            this.transform.InverseTransformPoint(targetWorldPosition)
            - new Vector3(0, 0, originToShoulderLength)
        );

        // check if not behind, check if not hyper extended
        return targetShoulderSpace.z >= 0.0f && targetShoulderSpace.magnitude <= extendedArmLength;
    }

    public IEnumerator rotateWristAroundPoint(
        PhysicsRemoteFPSAgentController controller,
        Vector3 rotatePoint,
        Quaternion rotation,
        float degreesPerSecond,
         float fixedDeltaTime,
        bool returnToStartPositionIfFailed = false
    ) {
        collisionListener.Reset();
        return withLastStepCallback(
            ContinuousMovement.rotateAroundPoint(
                controller: controller,
                updateTransform: armTarget.transform,
                rotatePoint: rotatePoint,
                targetRotation: rotation,
                fixedDeltaTime: fixedDeltaTime,
                degreesPerSecond: degreesPerSecond,
                returnToStartPropIfFailed: returnToStartPositionIfFailed
            )
        );
    }

    public IEnumerator rotateElbowRelative(
        PhysicsRemoteFPSAgentController controller,
        float degrees,
        float degreesPerSecond,
        float fixedDeltaTime,
        bool returnToStartPositionIfFailed = false
    ) {
        collisionListener.Reset();
        GameObject poleManipulator = GameObject.Find("IK_pole_manipulator");
        Quaternion rotation = Quaternion.Euler(0f, 0f, degrees);
        return withLastStepCallback(
            ContinuousMovement.rotate(
                controller: controller,
                moveTransform: poleManipulator.transform,
                targetRotation: poleManipulator.transform.rotation * rotation,
                fixedDeltaTime,
                degreesPerSecond,
                returnToStartPositionIfFailed
            )
        );
    }

    public IEnumerator rotateElbow(
        PhysicsRemoteFPSAgentController controller,
        float degrees,
        float degreesPerSecond,
        float fixedDeltaTime,
        bool returnToStartPositionIfFailed = false
    ) {
        GameObject poleManipulator = GameObject.Find("IK_pole_manipulator");
        return rotateElbowRelative(
            controller: controller,
            degrees: (degrees - poleManipulator.transform.eulerAngles.z),
            degreesPerSecond: degreesPerSecond,
            fixedDeltaTime: fixedDeltaTime,
            returnToStartPositionIfFailed: returnToStartPositionIfFailed
        );
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
        for (int i = 1; i <= 4; i++) {
            joint = joint.Find("robot_arm_" + i + "_jnt");

            JointMetadata jointMeta = new JointMetadata();

            // JOINT NAME
            jointMeta.name = joint.name;

            // POSITIONS //

            // WORLD RELATIVE POSITION
            jointMeta.position = joint.position;

            // ROOT-JOINT RELATIVE POSITION
            // Parent-relative position of joint is meaningless because it never changes relative to its parent joint, so we use rootRelative instead
            jointMeta.rootRelativePosition = FirstJoint.InverseTransformPoint(joint.position);

            // ROTATIONS //

            // WORLD RELATIVE ROTATION
            // Angler is grabbed since that is what actually changes the geometry angle
            currentRotation = joint.GetChild(0).rotation;

            // Check that world-relative rotation is angle-axis-notation-compatible
            if (currentRotation != new Quaternion(0, 0, 0, -1)) {
                currentRotation.ToAngleAxis(angle: out angleRot, axis: out vectorRot);

                jointMeta.rotation = new Vector4(vectorRot.x, vectorRot.y, vectorRot.z, ConvertAngleToZeroCentricRange(angleRot));
            } else {
                jointMeta.rotation = new Vector4(1, 0, 0, 0);
            }

            // ROOT-JOINT RELATIVE ROTATION
            // Root-forward and agent-forward are always the same

            // Grab rotation of current joint's angler relative to root joint
            currentRotation = Quaternion.Inverse(armBase.rotation) * joint.GetChild(0).rotation;

            // Check that root-relative rotation is angle-axis-notation-compatible
            if (currentRotation != new Quaternion(0, 0, 0, -1)) {
                currentRotation.ToAngleAxis(angle: out angleRot, axis: out vectorRot);
                jointMeta.rootRelativeRotation = new Vector4(vectorRot.x, vectorRot.y, vectorRot.z, ConvertAngleToZeroCentricRange(angleRot));
            } else {
                jointMeta.rootRelativeRotation = new Vector4(1, 0, 0, 0);
            }

            // PARENT-JOINT RELATIVE ROTATION
            if (i != 1) {
                parentJoint = joint.parent;

                // Grab rotation of current joint's angler relative to parent joint's angler
                currentRotation = Quaternion.Inverse(parentJoint.GetChild(0).rotation) * joint.GetChild(0).rotation;

                // Check that parent-relative rotation is angle-axis-notation-compatible
                if (currentRotation != new Quaternion(0, 0, 0, -1)) {
                    currentRotation.ToAngleAxis(angle: out angleRot, axis: out vectorRot);
                    jointMeta.localRotation = new Vector4(vectorRot.x, vectorRot.y, vectorRot.z, ConvertAngleToZeroCentricRange(angleRot));
                } else {
                    jointMeta.localRotation = new Vector4(1, 0, 0, 0);
                }
            } else {
                // Special case for robot_arm_1_jnt because it has no parent-joint
                jointMeta.localRotation = jointMeta.rootRelativeRotation;

                jointMeta.armBaseHeight = this.transform.localPosition.y;
                jointMeta.elbowOrientation = elbowTarget.localEulerAngles.z;
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

float ConvertAngleToZeroCentricRange(float degrees) {
    if (degrees < 0) {
        degrees = (degrees % 360f) + 360f;
    }
    if (degrees > 180f) {
        degrees = (degrees % 360f) - 360f;
    }
    return degrees;
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
