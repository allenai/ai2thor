using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
using System.Linq;
public partial class StretchABArmController : ArmController {
    
    [SerializeField]
    private Transform armBase, handCameraTransform, FirstJoint;

    private PhysicsRemoteFPSAgentController PhysicsController;

    //Distance from joint containing gripper camera to armTarget
    private Vector3 WristToManipulator = new Vector3 (0, -0.09872628f, 0);

    // private Stretch_Arm_Solver solver;
    

    // TODO: Possibly reimplement this fucntions, if AB read of transform is ok then may not need to reimplement
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

    public override void manipulateArm() {
        // TODO: this is called after every physics update loop so solver update funcion should go here

        // Arm target class member is used to calculate distance

    }

    public override GameObject GetArmTarget() {
        return armTarget.gameObject;
    }

    void Start() {
        // this.collisionListener = this.GetComponentInParent<CollisionListener>();

        //TODO: Initialization

        

        // TODO: Replace Solver 
    }

    public override bool shouldHalt() {
        // TODO: Reimplement halting condition 
        /// This is the halting condition in ContinuousMove, used to be the collision listener now can be the 
        // new solver or whatever
        // return collisionListener.ShouldHalt();

        return false;
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
        

    }

    public override void moveArmTarget(
        PhysicsRemoteFPSAgentController controller,
        Vector3 target,
        float unitsPerSecond,
        float fixedDeltaTime,
        bool returnToStart,
        string coordinateSpace,
        bool restrictTargetPosition,
        bool disableRendering
    ) {

    }
    
    public override void moveArmBase(
        PhysicsRemoteFPSAgentController controller,
        float height,
        float unitsPerSecond,
        float fixedDeltaTime,
        bool returnToStartPositionIfFailed,
        bool disableRendering,
        bool normalizedY
    ) { 

        // Vector3 target = new Vector3(this.transform.position.x, height, this.transform.position.z);
        // IEnumerator moveCall = resetArmTargetPositionRotationAsLastStep(
        //         ContinuousMovement.moveAB(
        //         controller: controller,
        //         collisionListener: collisionListener,
        //         moveTransform: this.transform,
        //         targetPosition: target,
        //         fixedDeltaTime: disableRendering ? fixedDeltaTime : Time.fixedDeltaTime,
        //         unitsPerSecond: unitsPerSecond,
        //         returnToStartPropIfFailed: returnToStartPositionIfFailed,
        //         localPosition: false
        //     )
        // );

        // if (disableRendering) {
        //     controller.unrollSimulatePhysics(
        //         enumerator: moveCall,
        //         fixedDeltaTime: fixedDeltaTime
        //     );
        // } else {
        //     StartCoroutine(moveCall);
        // }

    }

    public override void moveArmBaseUp(
        PhysicsRemoteFPSAgentController controller,
        float distance,
        float unitsPerSecond,
        float fixedDeltaTime,
        bool returnToStartPositionIfFailed,
        bool disableRendering
    ) {
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
        Vector3 pos = handCameraTransform.transform.position + WristToManipulator;
        Quaternion rot = handCameraTransform.transform.rotation;
        armTarget.position = pos;
        armTarget.rotation = rot;
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
