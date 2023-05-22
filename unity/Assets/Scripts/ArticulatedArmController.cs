using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
using System.Linq;
public partial class ArticulatedArmController : ArmController {
    public ArticulatedArmJointSolver[] joints;

    [SerializeField]
    //this wrist placeholder represents the posrot manipulator's position on the IK Stretch so we can match the distance magnitudes
    //it is weird because the origin point of the last joint in the AB is in a different offset, so we have to account for that for benchmarking
    private Transform armBase, handCameraTransform, FirstJoint, wristPlaceholderTransform;

    private float wristPlaceholderForwardOffset;
 
    private PhysicsRemoteFPSAgentController PhysicsController;

    //Distance from joint containing gripper camera to armTarget
    private Vector3 WristToManipulator = new Vector3(0, -0.09872628f, 0);

    // private Stretch_Arm_Solver solver;


    // TODO: Possibly reimplement this fucntions, if AB read of transform is ok then may not need to reimplement
    public override Transform pickupParent() {
        return magnetSphere.transform;
    }

    public override Vector3 wristSpaceOffsetToWorldPos(Vector3 offset) {
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

    public override void manipulateArm() {
        //so assume each joint that needs to move has had its `currentArmMoveParams` set
        //now we call `ControlJointFromAction` on all joints each physics update to get it to move...
        Debug.Log("starting ArticulatedArmController.manipulateArm");
        foreach (ArticulatedArmJointSolver j in joints) {
            j.ControlJointFromAction();
        }
    }

    public override bool shouldHalt() {
        Debug.Log("checking ArticulatedArmController shouldHalt");
        bool ZaWarudo = false;
        foreach (ArticulatedArmJointSolver j in joints) {
            //only halt if all joints report back that shouldHalt = true
            //joints that are idle and not moving will return shouldHalt = true by default
            Debug.Log($"distance moved so far for this joint is: {j.distanceMovedSoFar}");

            //if shouldHalt == false, continue moving
            if (!j.shouldHalt(
                distanceMovedSoFar: j.distanceMovedSoFar,
                cachedPositions: j.currentArmMoveParams.cachedPositions,
                tolerance: j.currentArmMoveParams.tolerance
            )) {
                //if any single joint is still not halting, return false
                Debug.Log("still not done, don't halt yet");
                ZaWarudo = false;
                return ZaWarudo;
            }

            //this joint returns that it should stop! Now we must wait to see if there rest
            else
            {
                Debug.Log($"halted! Distance moved: {j.distanceMovedSoFar}");
                ZaWarudo = true;
                continue;
            }
        }

        Debug.Log("halted, return true!");
        return ZaWarudo;
    }

    public override GameObject GetArmTarget() {
        return armTarget.gameObject;
    }

    void Start() {
        wristPlaceholderForwardOffset = wristPlaceholderTransform.transform.localPosition.z;
        Debug.Log($"wrist offset is: {wristPlaceholderForwardOffset}");
        // this.collisionListener = this.GetComponentInParent<CollisionListener>();

        //TODO: Initialization

        // TODO: Replace Solver 
    }

    //TODO: main functions to reimplement, use continuousMovement.moveAB/rotateAB
    public override void moveArmRelative(
        PhysicsRemoteFPSAgentController controller,
        Vector3 offset,
        float unitsPerSecond,
        float fixedDeltaTime,
        bool returnToStart,
        string coordinateSpace,
        bool restrictTargetPosition,
        bool disableRendering
    ) {
        //not doing this one yet soooo uhhhhh ignore for now        
    }

    public override void moveArmTarget(
        PhysicsRemoteFPSAgentController controller,
        Vector3 target, //distance + direction
        float unitsPerSecond,
        float fixedDeltaTime,
        bool returnToStart,
        string coordinateSpace,
        bool restrictTargetPosition,
        bool disableRendering
    ) {
        Debug.Log("starting moveArmTarget in ArticulatedArmController");
        float tolerance = 1e-3f;
        float maxTimePassed = 10.0f;
        int positionCacheSize = 10;

        float distance = Vector3.Distance(target, Vector3.zero);
        Debug.Log($"raw distance value: {distance}");
        //calculate distance to move offset by the wristPlaceholderTransform local z value
        //add the -z offset each time to actually move the same "distance" as the IK arm
        distance = distance + wristPlaceholderForwardOffset;
        Debug.Log($"actual distance to move: {distance}");

        int direction = 0;
        if (distance < 0) {
            direction = -1;
        }
        if (distance > 0) {
            direction = 1;
        }

        Dictionary<ArticulatedArmJointSolver, float> jointToArmDistanceRatios = new Dictionary<ArticulatedArmJointSolver, float>();

        //get the total distance each joint can move based on the upper limits
        float totalExtendDistance = 0.0f;

        //loop through all extending joints to get the total distance each joint can move
        for (int i = 1; i <= 4; i++) {
            totalExtendDistance += GetDriveUpperLimit(joints[i]);
        }

        //loop through all extending joints and get the ratio of movement each joint is responsible for
        for (int i = 1; i <= 4; i++) {
            ArticulatedArmJointSolver thisJoint = joints[i];
            jointToArmDistanceRatios.Add(thisJoint, GetDriveUpperLimit(thisJoint) / totalExtendDistance);
        }

        //set each joint to move its specific distance
        foreach (ArticulatedArmJointSolver joint in jointToArmDistanceRatios.Keys) {

        //assign each joint the distance it needs to move to have the entire arm
        //this means the distance each joint moves may be slightly different due to proportion of movement this joint is responsible for
        float myDistance = distance * jointToArmDistanceRatios[joint];

        ArmMoveParams amp = new ArmMoveParams {
            distance = myDistance,
            speed = unitsPerSecond,
            tolerance = tolerance,
            maxTimePassed = maxTimePassed,
            positionCacheSize = positionCacheSize,
            direction = direction
        };

            //assign movement params to this joint
            joint.PrepToControlJointFromAction(amp);
        }

        //now need to do move call here I think
        IEnumerator moveCall = resetArmTargetPositionRotationAsLastStep(
                ContinuousMovement.moveAB(
                controller: controller,
                fixedDeltaTime: disableRendering ? fixedDeltaTime : Time.fixedDeltaTime
            )
        );

        StartCoroutine(moveCall);
    }

    public float GetDriveUpperLimit(ArticulatedArmJointSolver joint, JointAxisType jointAxisType = JointAxisType.Extend) {
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

    // private IEnumerator AreAllTheJointsBackToIdle(List<ArticulatedArmJointSolver> jointsThatAreMoving, PhysicsRemoteFPSAgentController controller) {
    //     bool hasEveryoneStoppedYet = false;

    //     //keep checking if things are all idle yet
    //     //all individual joints should have a max timeout so this won't hang infinitely (i hope)
    //     while (hasEveryoneStoppedYet == false) {
    //         yield return new WaitForFixedUpdate();

    //         foreach (ArticulatedArmJointSolver joint in jointsThatAreMoving) {
    //             if (joint.extendState == ArmExtendState.Idle) {
    //                 hasEveryoneStoppedYet = true;
    //             } else {
    //                 hasEveryoneStoppedYet = false;
    //             }
    //         }
    //     }

    //     //done!
    //     controller.actionFinished(true);
    //     yield return null;
    // }

    public override void moveArmBase(
        PhysicsRemoteFPSAgentController controller,
        float distance,
        float unitsPerSecond,
        float fixedDeltaTime,
        bool returnToStartPositionIfFailed,
        bool disableRendering,
        bool normalizedY
    ) {
        Debug.Log("starting moveArmBase in ArticulatedArmController");
        float tolerance = 1e-3f;
        float maxTimePassed = 10.0f;
        int positionCacheSize = 10;

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
            tolerance = tolerance,
            maxTimePassed = maxTimePassed,
            positionCacheSize = positionCacheSize,
            direction = direction
        };

        ArticulatedArmJointSolver liftJoint = joints[0];
        //preset the joint's movement parameters ahead of time
        liftJoint.PrepToControlJointFromAction(amp);

        //Vector3 target = new Vector3(this.transform.position.x, distance, this.transform.position.z);

        //now need to do move call here I think
        IEnumerator moveCall = resetArmTargetPositionRotationAsLastStep(
                ContinuousMovement.moveAB(
                controller: controller,
                fixedDeltaTime: disableRendering ? fixedDeltaTime : Time.fixedDeltaTime
            )
        );

        StartCoroutine(moveCall);
    }

    public override void moveArmBaseUp(
        PhysicsRemoteFPSAgentController controller,
        float distance,
        float unitsPerSecond,
        float fixedDeltaTime,
        bool returnToStartPositionIfFailed,
        bool disableRendering
    ) {
        moveArmBase(
            controller: controller,
            distance: distance,
            unitsPerSecond: unitsPerSecond,
            fixedDeltaTime: fixedDeltaTime,
            returnToStartPositionIfFailed: returnToStartPositionIfFailed,
            disableRendering: disableRendering,
            normalizedY: false
        );

    }


    // TODO??? not sure if ready
    // public override void rotateWrist(
    //     PhysicsRemoteFPSAgentController controller,
    //     Quaternion rotation,
    //     float degreesPerSecond,
    //     bool disableRendering,
    //     float fixedDeltaTime,
    //     bool returnToStartPositionIfFailed
    // ) {}

    // public override bool PickupObject(List<string> objectIds, ref string errorMessage) {}

    //public override void DropObject() { }

    protected override void resetArmTarget() {

        // TODO: Reimplement
        // Vector3 pos = handCameraTransform.transform.position + WristToManipulator;
        // Quaternion rot = handCameraTransform.transform.rotation;
        // armTarget.position = pos;
        // armTarget.rotation = rot;
    }

    public override ArmMetadata GenerateMetadata() {

        // TODO: Reimplement, low prio for benchmark
        ArmMetadata meta = new ArmMetadata();

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
