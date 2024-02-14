using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
using System.Linq;
public partial class Stretch_Robot_Arm_Controller : ArmController {
    
    [SerializeField]
    private Transform armBase, handCameraTransform, FirstJoint;

    [SerializeField]
    public PhysicsRemoteFPSAgentController PhysicsController;

    // Distance from joint containing gripper camera to armTarget
    private Vector3 WristToManipulator = new Vector3 (0, -0.09872628f, 0);

    private Stretch_Arm_Solver solver;

    public float wristClockwiseLocalRotationLimit = 77.5f;
    public float wristCounterClockwiseLocalRotationLimit = 102.5f;

    private bool deadZoneCheck;

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

    private bool DeadZoneCheck() {
        if (deadZoneCheck) {
        float currentYaw = armTarget.rotation.eulerAngles.y;
            float cLimit = wristClockwiseLocalRotationLimit;
            float ccLimit = wristCounterClockwiseLocalRotationLimit;
            
            // Consolidate reachable euler-rotations (which are normally bounded by [0, 360)) into a continuous number line,
            // bounded instead by [continuousCounterClockwiseLocalRotationLimit, continuousClockwiseLocalRotationLimit + 360)
            if (cLimit < ccLimit) {
                cLimit += 360;
                if (currentYaw < ccLimit) {
                    currentYaw += 360;
                }
            }

            if (currentYaw < ccLimit || currentYaw > cLimit) {
                return true;
            } else {
                return false;
            }
        }
        else {
            return false;
        }
    }

     public override bool ShouldHalt() {
        return base.ShouldHalt() || DeadZoneCheck();
    }

    public override string GetHaltMessage() {
        var errorMessage = base.GetHaltMessage();
        if (errorMessage == "") {
            if (DeadZoneCheck()) {
                errorMessage = "Rotated up against Stretch arm wrist's dead-zone, could not reach target: '" + armTarget + "'.";
            }
        }
        return errorMessage;
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

    
    
    public IEnumerator rotateWrist(
        PhysicsRemoteFPSAgentController controller,
        float rotation,
        float degreesPerSecond,
        bool returnToStartPositionIfFailed = false,
        bool isRelativeRotation = true
    ) {

        // float clockwiseLocalRotationLimit = 77.5f;
        // float counterClockwiseLocalRotationLimit = 102.5f;
        float currentContinuousRotation, targetRelativeRotation, targetContinuousRotation;
        float continuousClockwiseLocalRotationLimit = wristClockwiseLocalRotationLimit;
        float continuousCounterClockwiseLocalRotationLimit = wristCounterClockwiseLocalRotationLimit;

        currentContinuousRotation = armTarget.transform.localEulerAngles.y;
        Quaternion targetRotation;
        Quaternion? secTargetRotation = null;

        // currentContinuousRotation is the start-rotation state on the bounds number-range
        // targetContinuousRotation is the end-rotation state on the bounds number-range
        // (which allows acute and obtuse rotation end-states to be distinct)
        // targetRelativeRotation is simply the final relative-rotation
        if (isRelativeRotation) {
            if (Mathf.Abs(rotation) <= 180) {
                targetRotation = armTarget.transform.rotation * Quaternion.Euler(0,rotation,0);
            } else {
                // Calculate target and secTargetRotation
                targetRelativeRotation = rotation / 2;
                targetRotation = armTarget.transform.rotation * Quaternion.Euler(0,targetRelativeRotation,0);
                secTargetRotation = targetRotation * Quaternion.Euler(0,targetRelativeRotation,0);
            }
        } else {
            // Consolidate reachable euler-rotations (which are normally bounded by [0, 360)) into a continuous number line,
            // bounded instead by [continuousCounterClockwiseLocalRotationLimit, continuousClockwiseLocalRotationLimit + 360)
            if (continuousClockwiseLocalRotationLimit < continuousCounterClockwiseLocalRotationLimit) {
                continuousClockwiseLocalRotationLimit += 360;
                if (currentContinuousRotation < continuousCounterClockwiseLocalRotationLimit) {
                    currentContinuousRotation += 360;
                }
            }

            targetContinuousRotation = currentContinuousRotation + rotation;

            // if angle is reachable via non-reflex rotation
            if (targetContinuousRotation > continuousCounterClockwiseLocalRotationLimit
                && targetContinuousRotation < continuousClockwiseLocalRotationLimit) {
                targetRotation = armTarget.transform.rotation * Quaternion.Euler(0,rotation,0);
            
            // if angle is NOT reachable, find how close it can get from that direction
            } else {
                float nonReflexAngularDistance, reflexAngularDistance;

                // Calculate proximity of non-reflex angle extreme to target
                if (targetContinuousRotation < continuousCounterClockwiseLocalRotationLimit) {
                    nonReflexAngularDistance = continuousCounterClockwiseLocalRotationLimit - targetContinuousRotation;
                } else {
                    nonReflexAngularDistance = targetContinuousRotation - continuousClockwiseLocalRotationLimit;
                }

                // Reflex targetContinuousRotation calculation
                targetRelativeRotation = (Mathf.Abs(rotation) - 360) * Mathf.Sign(rotation) / 2;
                float secTargetContinuousRotation = currentContinuousRotation + 2 * targetRelativeRotation;

                // If angle is reachable via reflex rotation
                if (secTargetContinuousRotation > continuousCounterClockwiseLocalRotationLimit
                    && secTargetContinuousRotation < continuousClockwiseLocalRotationLimit)
                {
                    targetRotation = armTarget.transform.rotation * Quaternion.Euler(0,targetRelativeRotation,0);
                    secTargetRotation = targetRotation * Quaternion.Euler(0,targetRelativeRotation,0);
                } else {
                    // Calculate proximity of reflex angle extreme to target
                    if (secTargetContinuousRotation < continuousCounterClockwiseLocalRotationLimit) {
                        reflexAngularDistance = continuousCounterClockwiseLocalRotationLimit - secTargetContinuousRotation;
                    } else {// if (secTargetContinuousRotation > continuousClockwiseLocalRotationLimit) {
                        reflexAngularDistance = secTargetContinuousRotation - continuousClockwiseLocalRotationLimit;
                    }

                    // Calculate which distance gets wrist closer to target
                    if (nonReflexAngularDistance <= reflexAngularDistance) {
                        targetRotation = armTarget.transform.rotation * Quaternion.Euler(0,rotation,0);
                    } else {
                        targetRotation = armTarget.transform.rotation * Quaternion.Euler(0,targetRelativeRotation,0);
                        secTargetRotation = targetRotation * Quaternion.Euler(0,targetRelativeRotation,0);
                    }
                }
            }
        }

        // view target rotation
        // Debug.Log("Rotating to " + targetRotation.eulerAngles + " degrees");
        // if (secTargetRotation is Quaternion currentSecTargetRotation) {
        //     Debug.Log("Rotating to " + targetRotation.eulerAngles + " degrees, and then to " + currentSecTargetRotation.eulerAngles);
        // }

        // Rotate wrist
        collisionListener.Reset();

        // Activate check for dead-zone encroachment inside of CollisionListener
        collisionListener.enableDeadZoneCheck();

        yield return withLastStepCallback(
            ContinuousMovement.rotate(
                controller: controller,
                moveTransform: armTarget.transform,
                targetRotation: targetRotation,
                fixedDeltaTime: PhysicsSceneManager.fixedDeltaTime,
                radiansPerSecond: degreesPerSecond,
                returnToStartPropIfFailed: returnToStartPositionIfFailed,
                secTargetRotation: secTargetRotation
            )
        );
        // IEnumerator rotate = resetArmTargetPositionRotationAsLastStep(
        //     ContinuousMovement.rotate(
        //         controller,
        //         collisionListener,
        //         armTarget.transform,
        //         targetRotation,
        //         disableRendering ? fixedDeltaTime : Time.fixedDeltaTime,
        //         degreesPerSecond,
        //         returnToStartPositionIfFailed,
        //         secTargetRotation
        //     )
        // );

        // if (disableRendering) {
        //     controller.unrollSimulatePhysics(
        //         rotate,
        //         fixedDeltaTime
        //     );

        // } else {
        //     StartCoroutine(rotate);
        // }
        
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
                jointMeta.rotation = new Vector4(vectorRot.x, Mathf.Abs(vectorRot.y), vectorRot.z, ConvertAngleToZeroCentricRange(angleRot * Mathf.Sign(vectorRot.y)));
            } else {
                jointMeta.rotation = new Vector4(1, 0, 0, 0);
            }

            // if (joint.name =="stretch_robot_wrist_1_jnt") {
            //     Debug.Log("stretch_robot_wrist_1_jnt's world-relative rotation: (" + jointMeta.rotation.x + ", " + jointMeta.rotation.y + ", " + jointMeta.rotation.z + ", " + jointMeta.rotation.w + ")");
            // }

            // ROOT-JOINT RELATIVE ROTATION
            // Grab rotation of current joint's angler relative to root joint
            currentRotation = Quaternion.Inverse(armBase.rotation) * joint.rotation;

            // Check that root-relative rotation is angle-axis-notation-compatible
            if (currentRotation != new Quaternion(0, 0, 0, -1)) {
                currentRotation.ToAngleAxis(angle: out angleRot, axis: out vectorRot);
                jointMeta.rootRelativeRotation = new Vector4(vectorRot.x, Mathf.Abs(vectorRot.y), vectorRot.z, ConvertAngleToZeroCentricRange(angleRot * Mathf.Sign(vectorRot.y)));
            } else {
                jointMeta.rootRelativeRotation = new Vector4(1, 0, 0, 0);
            }

            // if (joint.name =="stretch_robot_wrist_1_jnt") {
            //     Debug.Log("stretch_robot_wrist_1_jnt's root-relative rotation: (" + jointMeta.rootRelativeRotation.x + ", " + jointMeta.rootRelativeRotation.y + ", " + jointMeta.rootRelativeRotation.z + ", " + jointMeta.rootRelativeRotation.w + ")");
            // }

            // PARENT-JOINT RELATIVE ROTATION
            if (i != 1) {
                parentJoint = joint.parent;

                // Grab rotation of current joint's angler relative to parent joint's angler
                currentRotation = Quaternion.Inverse(parentJoint.rotation) * joint.rotation;

                // Check that parent-relative rotation is angle-axis-notation-compatible
                if (currentRotation != new Quaternion(0, 0, 0, -1)) {
                    currentRotation.ToAngleAxis(angle: out angleRot, axis: out vectorRot);
                    jointMeta.localRotation = new Vector4(vectorRot.x, Mathf.Abs(vectorRot.y), vectorRot.z, ConvertAngleToZeroCentricRange(angleRot * Mathf.Sign(vectorRot.y)));
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

        meta.rootRelativeHandSphereCenter = armBase.InverseTransformPoint(meta.handSphereCenter);

        meta.handSphereRadius = magnetSphere.radius;
        List<SimObjPhysics> objectsInMagnet = WhatObjectsAreInsideMagnetSphereAsSOP(false);
        meta.pickupableObjects = objectsInMagnet.Where(
            x => x.PrimaryProperty == SimObjPrimaryProperty.CanPickup
        ).Select(x => x.ObjectID).ToList();
        meta.touchedNotHeldObjects = objectsInMagnet.Select(x => x.ObjectID).ToList();
        meta.gripperOpennessState = ((StretchAgentController) PhysicsController).gripperOpennessState;
        
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
