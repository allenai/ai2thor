using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public partial class ArticulatedArmController : ArmController {
    public ArticulatedArmJointSolver[] joints;

    [SerializeField]
    //this wrist placeholder represents the posrot manipulator's position on the IK Stretch so we can match the distance magnitudes
    //it is weird because the origin point of the last joint in the AB is in a different offset, so we have to account for that for benchmarking
    private Transform armBase,
        handCameraTransform,
        FirstJoint,
        wristPlaceholderTransform;

    private float wristPlaceholderForwardOffset;

    private PhysicsRemoteFPSAgentController PhysicsController;

    //Distance from joint containing gripper camera to armTarget
    private Vector3 WristToManipulator = new Vector3(0, -0.09872628f, 0);

    //held objects, don't need a reference to colliders since we are "attaching" via fixed joints instead of cloning
    public new List<SimObjPhysics> heldObjects;

    // TODO: Possibly reimplement this fucntions, if AB read of transform is ok then may not need to reimplement
    public override Transform pickupParent() {
        return magnetSphere.transform;
    }

    public override Vector3 wristSpaceOffsetToWorldPos(Vector3 offset) {
        return handCameraTransform.TransformPoint(offset)
            - handCameraTransform.position
            + WristToManipulator;
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
        //so assume each joint that needs to move has had its `currentArmMoveParams` set
        //now we call `ControlJointFromAction` on all joints each physics update to get it to move...
        //Debug.Log("starting ArticulatedArmController.manipulateArm");
        foreach (ArticulatedArmJointSolver j in joints) {
            j.ControlJointFromAction(fixedDeltaTime);
        }
    }

    public override bool ShouldHalt() {
        //Debug.Log("checking ArticulatedArmController shouldHalt");
        bool shouldHalt = false;
        foreach (ArticulatedArmJointSolver j in joints) {
            //only halt if all joints report back that shouldHalt = true
            //joints that are idle and not moving will return shouldHalt = true by default
            //Debug.Log($"checking joint: {j.transform.name}");
            //Debug.Log($"distance moved so far for this joint is: {j.distanceTransformedSoFar}");

            //check all joints that have had movement params set to see if they have halted or not
            if (j.currentArmMoveParams != null) {
                if (
                    !j.shouldHalt(
                        distanceTransformedSoFar: j.distanceTransformedSoFar,
                        cachedPositions: j.currentArmMoveParams.cachedPositions,
                        cachedFixedTimeDeltas: j.currentArmMoveParams.cachedFixedDeltaTimes,
                        minMovementPerSecond: j.currentArmMoveParams.minMovementPerSecond,
                        haltCheckTimeWindow: j.currentArmMoveParams.haltCheckTimeWindow
                    )
                ) {
                    //if any single joint is still not halting, return false
                    //Debug.Log("still not done, don't halt yet");
                    shouldHalt = false;
                    return shouldHalt;
                }
                //this joint returns that it should stop! Now we must wait to see if there rest
                else {
                    //Debug.Log($"halted! Distance moved: {j.distanceTransformedSoFar}");
                    shouldHalt = true;
                    continue;
                }
            }
        }

        //Debug.Log("halted, return true!");
        return shouldHalt;
    }

    public override ActionFinished FinishContinuousMove(BaseFPSAgentController controller) {
        Debug.Log("starting continuousMoveFinishAB");
        string debugMessage = "I guess everything is fine?";

        // TODO inherit both solvers from common code
        // bool actionSuccess = IsFingerTransformCorrect();
        bool actionSuccess = true;
        if (!actionSuccess) {
            controller.agentManager.SetCriticalErrorState();
        }

        debugMessage = actionSuccess
            ? debugMessage
            : "Articulated agent is broken, fingers dislodges all actions will fail from this point. Must call 'reset' and reload scene and re-initialize agent.";
        // controller.actionFinished(actionSuccess, debugMessage);

        return new ActionFinished { success = actionSuccess, errorMessage = debugMessage };
    }

    public override GameObject GetArmTarget() {
        return armTarget.gameObject;
    }

    void Start() {
        wristPlaceholderForwardOffset = wristPlaceholderTransform.transform.localPosition.z;
        agentCapsuleCollider = PhysicsController.GetComponent<CapsuleCollider>();
        //Debug.Log($"wrist offset is: {wristPlaceholderForwardOffset}");

        // standingLocalCameraPosition = m_Camera.transform.localPosition;
        // Debug.Log($"------ AWAKE {standingLocalCameraPosition}");
        // this.collisionListener = this.GetComponentInParent<CollisionListener>();

        //TODO: Initialization

        // TODO: Replace Solver
    }

    //TODO: main functions to reimplement, use continuousMovement.moveAB/rotateAB
    public override IEnumerator moveArmRelative(
        PhysicsRemoteFPSAgentController controller,
        Vector3 offset,
        float unitsPerSecond,
        float fixedDeltaTime,
        bool returnToStart,
        string coordinateSpace,
        bool restrictTargetPosition
    ) {
        yield return new ActionFinished() { success = false, errorMessage = "Not implemented" };
        //not doing this one yet soooo uhhhhh ignore for now
    }

    public IEnumerator moveArmTarget(
        ArticulatedAgentController controller,
        Vector3 target, //distance + direction
        float unitsPerSecond,
        float fixedDeltaTime,
        bool useLimits = false
    ) {
        float minMovementPerSecond = 1e-3f;
        float maxTimePassed = 10.0f;
        float haltCheckTimeWindow = 0.2f;

        float distance = Vector3.Distance(target, Vector3.zero);
        //Debug.Log($"raw distance value: {distance}");
        //calculate distance to move offset by the wristPlaceholderTransform local z value
        //add the -z offset each time to actually move the same "distance" as the IK arm
        distance = distance + wristPlaceholderForwardOffset;
        //Debug.Log($"actual distance to move: {distance}");

        int direction = 0;

        //this is sort of a wonky way to detect direction but it'll work for noooooow
        if (target.z < 0) {
            direction = -1;
        }
        if (target.z > 0) {
            direction = 1;
        }

        Dictionary<ArticulatedArmJointSolver, float> jointToArmDistanceRatios =
            new Dictionary<ArticulatedArmJointSolver, float>();

        ArmMoveParams amp = new ArmMoveParams {
            distance = distance,
            speed = unitsPerSecond,
            minMovementPerSecond = minMovementPerSecond,
            maxTimePassed = maxTimePassed,
            haltCheckTimeWindow = haltCheckTimeWindow,
            direction = direction,
            useLimits = useLimits,
            maxForce = 40f
        };

        prepAllTheThingsBeforeJointMoves(joints[1], amp);

        //now need to do move call here I think
        return withLastStepCallback(
            ContinuousMovement.moveAB(
                movable: this,
                controller: controller,
                fixedDeltaTime: fixedDeltaTime
            )
        );
    }

    public float GetDriveUpperLimit(
        ArticulatedArmJointSolver joint,
        JointAxisType jointAxisType = JointAxisType.Extend
    ) {
        float upperLimit = 0.0f;

        if (jointAxisType == JointAxisType.Extend) {
            //z drive
            upperLimit = joint.myAB.zDrive.upperLimit;
        }

        if (jointAxisType == JointAxisType.Lift) {
            //y drive
            upperLimit = joint.myAB.yDrive.upperLimit;
        }

        //no revolute limit because it revolves in a circle forever

        return upperLimit;
    }

    public IEnumerator moveArmBase(
        ArticulatedAgentController controller,
        float distance,
        float unitsPerSecond,
        float fixedDeltaTime,
        bool returnToStartPositionIfFailed,
        bool normalizedY,
        bool useLimits
    ) {
        Debug.Log("starting moveArmBase in ArticulatedArmController");
        float minMovementPerSecond = 1e-3f;
        float maxTimePassed = 10.0f;
        float haltCheckTimeWindow = 0.2f;

        int direction = 0;
        if (distance < 0) {
            direction = -1;
        }
        if (distance > 0) {
            direction = 1;
        }

        ArmMoveParams amp = new ArmMoveParams {
            distance = Mathf.Abs(distance),
            speed = unitsPerSecond,
            minMovementPerSecond = minMovementPerSecond,
            maxTimePassed = maxTimePassed,
            haltCheckTimeWindow = haltCheckTimeWindow,
            direction = direction,
            useLimits = useLimits,
            maxForce = 40f
        };

        ArticulatedArmJointSolver liftJoint = joints[0];
        //preset the joint's movement parameters ahead of time
        prepAllTheThingsBeforeJointMoves(liftJoint, amp);
        //liftJoint.PrepToControlJointFromAction(amp);

        //Vector3 target = new Vector3(this.transform.position.x, distance, this.transform.position.z);

        //now need to do move call here I think
        return withLastStepCallback(
            ContinuousMovement.moveAB(
                movable: this,
                controller: controller,
                fixedDeltaTime: fixedDeltaTime
            )
        );
    }

    public IEnumerator moveArmBaseUp(
        ArticulatedAgentController controller,
        float distance,
        float unitsPerSecond,
        float fixedDeltaTime,
        bool returnToStartPositionIfFailed,
        bool useLimits
    ) {
        return moveArmBase(
            controller: controller,
            distance: distance,
            unitsPerSecond: unitsPerSecond,
            fixedDeltaTime: fixedDeltaTime,
            returnToStartPositionIfFailed: returnToStartPositionIfFailed,
            normalizedY: false,
            useLimits: useLimits
        );
    }

    private void prepAllTheThingsBeforeJointMoves(
        ArticulatedArmJointSolver joint,
        ArmMoveParams armMoveParams
    ) {
        //FloorCollider.material = sticky;
        joint.PrepToControlJointFromAction(armMoveParams);
    }

    public IEnumerator rotateWrist(
        ArticulatedAgentController controller,
        float distance,
        float degreesPerSecond,
        float fixedDeltaTime,
        bool returnToStartPositionIfFailed
    ) {
        Debug.Log("starting rotateWrist in ArticulatedArmController");
        float minMovementPerSecond = 1f * Mathf.Deg2Rad;
        float maxTimePassed = 10.0f;
        float haltCheckTimeWindow = 0.2f;

        int direction = 0;
        if (distance < 0) {
            direction = -1;
        }
        if (distance > 0) {
            direction = 1;
        }

        ArmMoveParams amp = new ArmMoveParams {
            distance = Mathf.Abs(distance),
            speed = degreesPerSecond,
            minMovementPerSecond = minMovementPerSecond,
            maxTimePassed = maxTimePassed,
            haltCheckTimeWindow = haltCheckTimeWindow,
            direction = direction,
            maxForce = 40f
        };

        ArticulatedArmJointSolver wristJoint = joints[2];
        //preset the joint's movement parameters ahead of time
        prepAllTheThingsBeforeJointMoves(wristJoint, amp);

        //now need to do move call here I think
        return withLastStepCallback(
            ContinuousMovement.moveAB(
                movable: this,
                controller: controller,
                fixedDeltaTime: fixedDeltaTime
            )
        );
    }

    public IEnumerable PickupObject(List<string> objectIds) {
        Debug.Log("calling PickupObject from ArticulatedArmController");
        bool pickedUp = false;

        foreach (SimObjPhysics sop in WhatObjectsAreInsideMagnetSphereAsSOP(onlyPickupable: true)) {
            Debug.Log($"sop named: {sop.objectID} found inside sphere");
            if (objectIds != null) {
                //only grab objects specified by objectIds
                if (!objectIds.Contains(sop.objectID)) {
                    continue;
                }
            }

            sop.BeingPickedUpByArticulatedAgent(this);

            // Rigidbody rb = sop.GetComponent<Rigidbody>();

            // //make sure rigidbody of object is not kinematic
            // rb.isKinematic = false;

            // //add a fixed joint to this picked up object
            // FixedJoint ultraHand = sop.transform.gameObject.AddComponent<FixedJoint>();
            // //add reference to the wrist joint as connected articulated body
            // ultraHand.connectedArticulationBody = FinalJoint.GetComponent<ArticulationBody>();
            // ultraHand.enableCollision = true;
            //add to heldObjects list so we know when to drop

            pickedUp = true;
            heldObjects.Add(sop);
        }
        var errorMessage = "";
        if (!pickedUp) {
            errorMessage = (
                objectIds != null
                    ? "No objects (specified by objectId) were valid to be picked up by the arm"
                    : "No objects were valid to be picked up by the arm"
            );
        }

        yield return new ActionFinished() { success = pickedUp, errorMessage = errorMessage };
    }

    //called by ArmAgentController ReleaseObject
    public override IEnumerator DropObject() {
        foreach (SimObjPhysics sop in heldObjects) {
            //remove the joint component
            //may need a null check for if we decide to break joints via force at some poine.
            //look into the OnJointBreak callback if needed
            Destroy(sop.transform.GetComponent<FixedJoint>());

            sop.BeingDropped();
        }

        heldObjects.Clear();
        yield return ActionFinished.Success;
    }

    protected override void lastStepCallback() {
        foreach (ArticulatedArmJointSolver joint in joints) {
            ArticulationBody myAB = joint.myAB;

            if (myAB == null) {
                Debug.LogWarning("Articulated body is null, skipping.");
                continue;
            }

            // Check the joint type and get the current joint position and velocity
            if (myAB.jointType == ArticulationJointType.PrismaticJoint) {
                //                Debug.Log($"joint {joint.gameObject}");
                //                Debug.Log($"joint {myAB.jointType}");
                //                Debug.Log($"solverIterations {myAB.solverIterations}");
                //                Debug.Log($"solverVelocityIterations {myAB.solverVelocityIterations}");

                if (myAB.dofCount != 1) {
                    throw new NotImplementedException(
                        "Prismatic joint must have 1 degree of freedom"
                    );
                }
                float currentPosition = myAB.jointPosition[0];

                ArticulationDrive xDrive = myAB.xDrive;
                ArticulationDrive yDrive = myAB.yDrive;
                ArticulationDrive zDrive = myAB.zDrive;

                // Super hacky way to get which drive is active
                string whichDrive = "x";
                ArticulationDrive activeDrive = xDrive;
                if (yDrive.target != 0.0f) {
                    activeDrive = yDrive;
                    whichDrive = "y";
                }
                if (zDrive.target != 0.0f) {
                    activeDrive = zDrive;
                    whichDrive = "z";
                }

                Debug.Log(currentPosition);
                Debug.Log(whichDrive);

                activeDrive.target = currentPosition;
                activeDrive.targetVelocity = 0f;

                if (whichDrive == "x") {
                    myAB.xDrive = activeDrive;
                }
                if (whichDrive == "y") {
                    myAB.yDrive = activeDrive;
                }
                if (whichDrive == "z") {
                    myAB.zDrive = activeDrive;
                }
            } else if (myAB.jointType == ArticulationJointType.RevoluteJoint) {
                // For revolute joints
                if (myAB.dofCount != 1) {
                    throw new NotImplementedException(
                        "Revolute joint must have 1 degree of freedom"
                    );
                }
                float currentPosition = Mathf.Rad2Deg * myAB.jointPosition[0]; // Weirdly not in degrees

                // TODO: We just assume that the joint is on the x axis, we don't have a good way to check
                //       for otherwise atm.
                ArticulationDrive xDrive = myAB.xDrive;

                xDrive.target = currentPosition;
                xDrive.targetVelocity = 0f;

                myAB.xDrive = xDrive;
            } else {
                throw new NotImplementedException($"Unsupported joint type {myAB.jointType}");
            }
        }
    }

    //ignore this, we need new metadata that makes more sense for the articulation heirarchy soooooo
    public override ArmMetadata GenerateMetadata() {
        // TODO: Reimplement, low prio for benchmark
        ArmMetadata meta = new ArmMetadata();
        return meta;
    }

    //actual metadata implementation for articulation heirarchy
    public ArticulationArmMetadata GenerateArticulationMetadata() {
        ArticulationArmMetadata meta = new ArticulationArmMetadata();

        List<ArticulationJointMetadata> metaJoints = new List<ArticulationJointMetadata>();

        //declaring some stuff for processing metadata
        Quaternion currentRotation;
        float angleRot;
        Vector3 vectorRot;

        for (int i = 0; i < joints.Count(); i++) {
            ArticulationJointMetadata jMeta = new ArticulationJointMetadata();

            jMeta.name = joints[i].name;

            jMeta.position = joints[i].transform.position;

            jMeta.rootRelativePosition = joints[0]
                .transform.InverseTransformPoint(joints[i].transform.position);

            jMeta.jointHeirarchyPosition = i;

            // WORLD RELATIVE ROTATION
            currentRotation = joints[i].transform.rotation;

            // Check that world-relative rotation is angle-axis-notation-compatible
            if (currentRotation != new Quaternion(0, 0, 0, -1)) {
                currentRotation.ToAngleAxis(angle: out angleRot, axis: out vectorRot);
                jMeta.rotation = new Vector4(vectorRot.x, vectorRot.y, vectorRot.z, angleRot);
            } else {
                jMeta.rotation = new Vector4(1, 0, 0, 0);
            }

            // ROOT-JOINT RELATIVE ROTATION
            // Grab rotation of current joint's angler relative to root joint
            currentRotation =
                Quaternion.Inverse(joints[0].transform.rotation) * joints[i].transform.rotation;
            if (currentRotation != new Quaternion(0, 0, 0, -1)) {
                currentRotation.ToAngleAxis(angle: out angleRot, axis: out vectorRot);
                jMeta.rootRelativeRotation = new Vector4(
                    vectorRot.x,
                    vectorRot.y,
                    vectorRot.z,
                    angleRot
                );
            } else {
                jMeta.rootRelativeRotation = new Vector4(1, 0, 0, 0);
            }

            // LOCAL POSITION AND LOCAL ROTATION
            //get local position and local rotation relative to immediate parent in heirarchy
            if (i != 0) {
                jMeta.localPosition = joints[i - 1]
                    .transform.InverseTransformPoint(joints[i].transform.position);

                var currentLocalRotation =
                    Quaternion.Inverse(joints[i - 1].transform.rotation)
                    * joints[i].transform.rotation;
                if (currentLocalRotation != new Quaternion(0, 0, 0, -1)) {
                    currentLocalRotation.ToAngleAxis(angle: out angleRot, axis: out vectorRot);
                    jMeta.localRotation = new Vector4(
                        vectorRot.x,
                        vectorRot.y,
                        vectorRot.z,
                        angleRot
                    );
                } else {
                    jMeta.localRotation = new Vector4(1, 0, 0, 0);
                }
            } else {
                //special case for the lift since its the base of the arm
                jMeta.localPosition = jMeta.position;
                jMeta.localRotation = jMeta.rootRelativeRotation;
            }

            metaJoints.Add(jMeta);
        }

        meta.joints = metaJoints.ToArray();

        // metadata for any objects currently held by the hand on the arm
        // note this is different from objects intersecting the hand's sphere,
        // there could be a case where an object is inside the sphere but not picked up by the hand
        List<string> heldObjectIDs = new List<string>();
        if (heldObjects != null) {
            foreach (SimObjPhysics sop in heldObjects) {
                heldObjectIDs.Add(sop.objectID);
            }
        }

        meta.heldObjects = heldObjectIDs;
        meta.handSphereCenter = magnetSphere.transform.TransformPoint(magnetSphere.center);
        meta.handSphereRadius = magnetSphere.radius;
        List<SimObjPhysics> objectsInMagnet = WhatObjectsAreInsideMagnetSphereAsSOP(false);
        meta.pickupableObjects = objectsInMagnet
            .Where(x => x.PrimaryProperty == SimObjPrimaryProperty.CanPickup)
            .Select(x => x.ObjectID)
            .ToList();
        meta.objectsInsideHandSphereRadius = objectsInMagnet.Select(x => x.ObjectID).ToList();

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
