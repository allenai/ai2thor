using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using RandomExtensions;
using UnityEngine.AI;
using UnityEngine.Rendering.PostProcessing;

namespace UnityStandardAssets.Characters.FirstPerson {
        
    public partial class StretchAgentController : PhysicsRemoteFPSAgentController {
        public StretchAgentController(BaseAgentComponent baseAgentComponent, AgentManager agentManager) : base(baseAgentComponent, agentManager) {
        }

        public override void updateImageSynthesis(bool status) {
            base.updateImageSynthesis(status);

            // updateImageSynthesis is run in BaseFPSController's Initialize method after the
            // Stretch Agent's unique secondary camera has been added to the list of third party
            // cameras in InitializeBody, so a third-party camera image synthesis update is
            // necessary if we want the secondary camera's image synthesis componenent to match
            // the primary camera's
            agentManager.updateThirdPartyCameraImageSynthesis(status);
        }

        public override void InitializeBody() {
            VisibilityCapsule = StretchVisCap;
            m_CharacterController.center = new Vector3(0, -0.1821353f, -0.1092373f);
            m_CharacterController.radius = 0.1854628f;
            m_CharacterController.height = 1.435714f;

            CapsuleCollider cc = this.GetComponent<CapsuleCollider>();
            cc.center = m_CharacterController.center;
            cc.radius = m_CharacterController.radius;
            cc.height = m_CharacterController.height;

            m_Camera.GetComponent<PostProcessVolume>().enabled = true;
            m_Camera.GetComponent<PostProcessLayer>().enabled = true;

            // camera position
            m_Camera.transform.localPosition = new Vector3(0, 0.378f, 0.0453f);

            // camera FOV
            m_Camera.fieldOfView = 69f;

            // set camera stand/crouch local positions for Tall mode
            standingLocalCameraPosition = m_Camera.transform.localPosition;
            crouchingLocalCameraPosition = m_Camera.transform.localPosition;

            // set secondary arm-camera
            Camera fp_camera_2 = m_CharacterController.transform.Find("SecondaryCamera").GetComponent<Camera>();
            fp_camera_2.gameObject.SetActive(true);
            fp_camera_2.transform.localPosition = new Vector3(0.0353f, 0.5088f, -0.076f);
            fp_camera_2.transform.localEulerAngles = new Vector3(45f, 90f, 0f);
            fp_camera_2.fieldOfView = 90f;
//            fp_camera_2.fieldOfView = 75f;
            agentManager.registerAsThirdPartyCamera(fp_camera_2);

            // limit camera from looking too far down
            this.maxDownwardLookAngle = 90f;
            this.maxUpwardLookAngle = 25f;

            // enable stretch arm component
            Debug.Log("initializing stretch arm");
            StretchArm.SetActive(true);
            SArm = this.GetComponentInChildren<Stretch_Robot_Arm_Controller>();
            var armTarget = SArm.transform.Find("stretch_robot_arm_rig").Find("stretch_robot_pos_rot_manipulator");
            Vector3 pos = armTarget.transform.localPosition;
            pos.z = 0.0f; // pulls the arm in to be fully contracted
            armTarget.transform.localPosition = pos;
            var StretchSolver = this.GetComponentInChildren<Stretch_Arm_Solver>();
            Debug.Log("running manipulate stretch arm");
            StretchSolver.ManipulateStretchArm();
        }

        protected Stretch_Robot_Arm_Controller getArm() {
            Stretch_Robot_Arm_Controller arm = GetComponentInChildren<Stretch_Robot_Arm_Controller>();
            if (arm == null) {
                throw new InvalidOperationException(
                    "Agent does not have Stretch arm or is not enabled.\n" +
                    $"Make sure there is a '{typeof(Stretch_Robot_Arm_Controller).Name}' component as a child of this agent."
                );
            }
            return arm;
        }

        /*
        Toggles the visibility of the magnet sphere at the end of the arm.
        */
        public void ToggleMagnetVisibility(bool? visible = null) {
            MeshRenderer mr = GameObject.Find("MagnetRenderer").GetComponentInChildren<MeshRenderer>();
            if (visible.HasValue) {
                mr.enabled = visible.Value;
            } else {
                mr.enabled = !mr.enabled;
            }
            actionFinished(true);
        }

        public void TeleportArm(
            Vector3? position = null,
            float? rotation = null,
            bool worldRelative = false,
            bool forceAction = false
        ) {
            GameObject posRotManip = this.GetComponent<BaseAgentComponent>().StretchArm.GetComponent<Stretch_Robot_Arm_Controller>().GetArmTarget();

            // cache old values in case there's a failure
            Vector3 oldLocalPosition = posRotManip.transform.localPosition;
            float oldLocalRotationAngle = posRotManip.transform.localEulerAngles.y;

            // establish defaults in the absence of inputs
            if (position == null) {
                position = new Vector3(0f, 0.1f, 0f);
            }

            if (rotation == null) {
                rotation = -180f;
            }

            // teleport arm!
            if (!worldRelative) {
                posRotManip.transform.localPosition = (Vector3)position;
                posRotManip.transform.localEulerAngles = new Vector3 (0, (float)rotation % 360, 0);
            } else {
                posRotManip.transform.position = (Vector3)position;
                posRotManip.transform.eulerAngles = new Vector3 (0, (float)rotation % 360, 0);
            }

            if (SArm.IsArmColliding() && !forceAction) {
                errorMessage = "collision detected at desired transform, cannot teleport";
                posRotManip.transform.localPosition = oldLocalPosition;
                posRotManip.transform.localEulerAngles = new Vector3(0, oldLocalRotationAngle, 0);
                actionFinished(false);
            } else {
                actionFinished(true);
            }
        }

        /*
        This function is identical to `MoveArm` except that rather than
        giving a target position you instead give an "offset" w.r.t.
        the arm's (i.e. wrist's) current location.

        Thus if you want to increase the
        arms x position (in world coordinates) by 0.1m you should
        pass in `offset=Vector3(0.1f, 0f, 0f)` and `coordinateSpace="world"`.
        If you wanted to move the arm 0.1m to the "right" from the agent's
        perspective then you would pass in the same offset but set
        `coordinateSpace="armBase"`. Note that this last movement is **not**
        the same as passing `position=Vector3(0.1f, 0f, 0f)` to the `MoveArm`
        action with `coordinateSpace="wrist"` as, if the wrist has been rotated,
        right need not mean the same thing to the arm base as it does to the wrist.

        Finally note that when `coordinateSpace="wrist"` then both `MoveArm` and
        `MoveArmRelative` are identical.
        */
        public void MoveArmRelative(
            Vector3 offset,
            float speed = 1,
            float? fixedDeltaTime = null,
            bool returnToStart = true,
            string coordinateSpace = "armBase",
            bool restrictMovement = false,
            bool disableRendering = true
        ) {
            Stretch_Robot_Arm_Controller arm = getArm();
            arm.moveArmRelative(
                controller: this,
                offset: offset,
                unitsPerSecond: speed,
                fixedDeltaTime: fixedDeltaTime.GetValueOrDefault(Time.fixedDeltaTime),
                returnToStart: returnToStart,
                coordinateSpace: coordinateSpace,
                restrictTargetPosition: restrictMovement,
                disableRendering: disableRendering
            );
        }

        public void MoveArm(
            Vector3 position,
            float speed = 1,
            float? fixedDeltaTime = null,
            bool returnToStart = true,
            string coordinateSpace = "armBase",
            bool restrictMovement = false,
            bool disableRendering = true
        ) {
            Stretch_Robot_Arm_Controller arm = getArm();
            arm.moveArmTarget(
                controller: this,
                target: position,
                unitsPerSecond: speed,
                fixedDeltaTime: fixedDeltaTime.GetValueOrDefault(Time.fixedDeltaTime),
                returnToStart: returnToStart,
                coordinateSpace: coordinateSpace,
                restrictTargetPosition: restrictMovement,
                disableRendering: disableRendering
            );
        }

//         /*
//         Let's say you wanted the agent to be able to rotate the object it's
//         holding so that it could get multiple views of the object. You
//         could do this by using the RotateWristRelative action but the downside
//         of using that function is that object will be translated as the
//         wrist rotates. This RotateWristAroundHeldObject action gets around this
//         problem by allowing you to specify how much you'd like the object to
//         rotate by and then figuring out how to translate/rotate the wrist so
//         that the object rotates while staying fixed in space.

//         Note that the object may stil be translated if the specified rotation
//         of the object is not feasible given the arm's DOF and length/joint
//         constraints.
//         */
//         public void RotateWristAroundHeldObject(
//             float pitch = 0f,
//             float yaw = 0f,
//             float roll = 0f,
//             float speed = 10f,
//             float? fixedDeltaTime = null,
//             bool returnToStart = true,
//             bool disableRendering = true
//         ) {
//             IK_Robot_Arm_Controller arm = getArm();

//             if (arm.heldObjects.Count == 1) {
//                 SimObjPhysics sop = arm.heldObjects.Keys.ToArray()[0];
//                 RotateWristAroundPoint(
//                     point: sop.gameObject.transform.position,
//                     pitch: pitch,
//                     yaw: yaw,
//                     roll: roll,
//                     speed: speed,
//                     fixedDeltaTime: fixedDeltaTime,
//                     returnToStart: returnToStart,
//                     disableRendering: disableRendering
//                 );
//             } else {
//                 actionFinished(
//                     success: false,
//                     errorMessage: $"Cannot RotateWristAroundHeldObject when holding" +
//                         $" != 1 objects, currently holding {arm.heldObjects.Count} objects."
//                 );
//             }

//         }

//         /*
//         Rotates and translates the wrist so that its position
//         stays fixed relative some given point as that point
//         rotates some given amount.
//         */
//         public void RotateWristAroundPoint(
//             Vector3 point,
//             float pitch = 0f,
//             float yaw = 0f,
//             float roll = 0f,
//             float speed = 10f,
//             float? fixedDeltaTime = null,
//             bool returnToStart = true,
//             bool disableRendering = true
//         ) {
//             IK_Robot_Arm_Controller arm = getArm();

//             arm.rotateWristAroundPoint(
//                 controller: this,
//                 rotatePoint: point,
//                 rotation: Quaternion.Euler(pitch, yaw, -roll),
//                 degreesPerSecond: speed,
//                 disableRendering: disableRendering,
//                 fixedDeltaTime: fixedDeltaTime.GetValueOrDefault(Time.fixedDeltaTime),
//                 returnToStartPositionIfFailed: returnToStart
//             );
//         }

//         // perhaps this should fail if no object is picked up?
//         // currently action success happens as long as the arm is
//         // enabled because it is a successful "attempt" to pickup something
        public void PickupObject(List<string> objectIdCandidates = null) {
            Stretch_Robot_Arm_Controller arm = getArm();
            actionFinished(arm.PickupObject(objectIdCandidates, ref errorMessage), errorMessage);
        }

        public override void PickupObject(float x, float y, bool forceAction = false, bool manualInteract = false) {
            throw new InvalidOperationException(
                "You are passing in iTHOR PickupObject parameters (x, y) to the arm agent!"
            );
        }

        public override void PickupObject(string objectId, bool forceAction = false, bool manualInteract = false) {
            throw new InvalidOperationException(
                "You are passing in iTHOR PickupObject parameters (objectId) to the arm agent!"
            );
        }

        public void ReleaseObject() {
            Stretch_Robot_Arm_Controller arm = getArm();
            arm.DropObject();

            // TODO: only return after object(s) dropped have finished moving
            // currently this will return the frame the object is released
            actionFinished(true);
        }

        // note this does not reposition the center point of the magnet orb
        // so expanding the radius too much will cause it to clip backward into the wrist joint
        public void SetHandSphereRadius(float radius) {
            if (radius < 0.04f || radius > 0.5f) {
                throw new ArgumentOutOfRangeException(
                    $"radius={radius} of hand cannot be less than 0.04m nor greater than 0.5m"
                );
            }

            Stretch_Robot_Arm_Controller arm = getArm();
            arm.SetHandSphereRadius(radius);
            actionFinished(true);
        }

        public void MoveAgent(
            float ahead = 0,
            float right = 0,
            float speed = 1,
            float? fixedDeltaTime = null,
            bool returnToStart = true,
            bool disableRendering = true
        ) {
            if (ahead == 0 && right == 0) {
                throw new ArgumentException("Must specify ahead or right!");
            }
            Vector3 direction = new Vector3(x: right, y: 0, z: ahead);
            float fixedDeltaTimeFloat = fixedDeltaTime.GetValueOrDefault(Time.fixedDeltaTime);

            CollisionListener collisionListener = this.GetComponentInParent<CollisionListener>();

            Vector3 directionWorld = transform.TransformDirection(direction);
            Vector3 targetPosition = transform.position + directionWorld;

            collisionListener.Reset();

            IEnumerator move = ContinuousMovement.move(
                controller: this,
                collisionListener: collisionListener,
                moveTransform: this.transform,
                targetPosition: targetPosition,
                fixedDeltaTime: fixedDeltaTimeFloat,
                unitsPerSecond: speed,
                returnToStartPropIfFailed: returnToStart,
                localPosition: false
            );

            if (disableRendering) {
                unrollSimulatePhysics(
                    enumerator: move,
                    fixedDeltaTime: fixedDeltaTimeFloat
                );
            } else {
                StartCoroutine(move);
            }
        }

        public override void MoveAhead(
            float? moveMagnitude = null,
            string objectId = "",                // TODO: Unused, remove when refactoring the controllers
            float maxAgentsDistance = -1f,       // TODO: Unused, remove when refactoring the controllers
            bool forceAction = false,            // TODO: Unused, remove when refactoring the controllers
            bool manualInteract = false,         // TODO: Unused, remove when refactoring the controllers
            bool allowAgentsToIntersect = false, // TODO: Unused, remove when refactoring the controllers
            float speed = 1,
            float? fixedDeltaTime = null,
            bool returnToStart = true,
            bool disableRendering = true
        ) {
            MoveAgent(
                ahead: moveMagnitude.GetValueOrDefault(gridSize),
                speed: speed,
                fixedDeltaTime: fixedDeltaTime,
                returnToStart: returnToStart,
                disableRendering: disableRendering
            );
        }

        public override void MoveBack(
            float? moveMagnitude = null,
            string objectId = "",                // TODO: Unused, remove when refactoring the controllers
            float maxAgentsDistance = -1f,       // TODO: Unused, remove when refactoring the controllers
            bool forceAction = false,            // TODO: Unused, remove when refactoring the controllers
            bool manualInteract = false,         // TODO: Unused, remove when refactoring the controllers
            bool allowAgentsToIntersect = false, // TODO: Unused, remove when refactoring the controllers
            float speed = 1,
            float? fixedDeltaTime = null,
            bool returnToStart = true,
            bool disableRendering = true
        ) {
            MoveAgent(
                ahead: -moveMagnitude.GetValueOrDefault(gridSize),
                speed: speed,
                fixedDeltaTime: fixedDeltaTime,
                returnToStart: returnToStart,
                disableRendering: disableRendering
            );
        }

        public override void MoveRight(
            float? moveMagnitude = null,
            string objectId = "",                // TODO: Unused, remove when refactoring the controllers
            float maxAgentsDistance = -1f,       // TODO: Unused, remove when refactoring the controllers
            bool forceAction = false,            // TODO: Unused, remove when refactoring the controllers
            bool manualInteract = false,         // TODO: Unused, remove when refactoring the controllers
            bool allowAgentsToIntersect = false, // TODO: Unused, remove when refactoring the controllers
            float speed = 1,
            float? fixedDeltaTime = null,
            bool returnToStart = true,
            bool disableRendering = true
        ) {
            MoveAgent(
                right: moveMagnitude.GetValueOrDefault(gridSize),
                speed: speed,
                fixedDeltaTime: fixedDeltaTime,
                returnToStart: returnToStart,
                disableRendering: disableRendering
            );
        }

        public override void MoveLeft(
            float? moveMagnitude = null,
            string objectId = "",                // TODO: Unused, remove when refactoring the controllers
            float maxAgentsDistance = -1f,       // TODO: Unused, remove when refactoring the controllers
            bool forceAction = false,            // TODO: Unused, remove when refactoring the controllers
            bool manualInteract = false,         // TODO: Unused, remove when refactoring the controllers
            bool allowAgentsToIntersect = false, // TODO: Unused, remove when refactoring the controllers
            float speed = 1,
            float? fixedDeltaTime = null,
            bool returnToStart = true,
            bool disableRendering = true
        ) {
            MoveAgent(
                right: -moveMagnitude.GetValueOrDefault(gridSize),
                speed: speed,
                fixedDeltaTime: fixedDeltaTime,
                returnToStart: returnToStart,
                disableRendering: disableRendering
            );
        }

        public override void RotateRight(
            float? degrees = null,
            bool manualInteract = false, // TODO: Unused, remove when refactoring the controllers
            bool forceAction = false,    // TODO: Unused, remove when refactoring the controllers
            float speed = 1.0f,
            bool waitForFixedUpdate = false,
            bool returnToStart = true,
            bool disableRendering = true,
            float fixedDeltaTime = 0.02f
        ) {
            RotateAgent(
                degrees: degrees.GetValueOrDefault(rotateStepDegrees),
                speed: speed,
                waitForFixedUpdate: waitForFixedUpdate,
                returnToStart: returnToStart,
                disableRendering: disableRendering,
                fixedDeltaTime: fixedDeltaTime
            );
        }

        public override void RotateLeft(
            float? degrees = null,
            bool manualInteract = false, // TODO: Unused, remove when refactoring the controllers
            bool forceAction = false,    // TODO: Unused, remove when refactoring the controllers
            float speed = 1.0f,
            bool waitForFixedUpdate = false,
            bool returnToStart = true,
            bool disableRendering = true,
            float fixedDeltaTime = 0.02f
        ) {
            RotateAgent(
                degrees: -degrees.GetValueOrDefault(rotateStepDegrees),
                speed: speed,
                waitForFixedUpdate: waitForFixedUpdate,
                returnToStart: returnToStart,
                disableRendering: disableRendering,
                fixedDeltaTime: fixedDeltaTime
            );
        }

        public void RotateAgent(
            float degrees,
            float speed = 1.0f,
            bool waitForFixedUpdate = false,
            bool returnToStart = true,
            bool disableRendering = true,
            float fixedDeltaTime = 0.02f
        ) {
            CollisionListener collisionListener = this.GetComponentInParent<CollisionListener>();
            collisionListener.Reset();

            // this.transform.Rotate()
            IEnumerator rotate = ContinuousMovement.rotate(
                controller: this,
                collisionListener: this.GetComponentInParent<CollisionListener>(),
                moveTransform: this.transform,
                targetRotation: this.transform.rotation * Quaternion.Euler(0.0f, degrees, 0.0f),
                fixedDeltaTime: disableRendering ? fixedDeltaTime : Time.fixedDeltaTime,
                radiansPerSecond: speed,
                returnToStartPropIfFailed: returnToStart
            );

            if (disableRendering) {
                unrollSimulatePhysics(
                    enumerator: rotate,
                    fixedDeltaTime: fixedDeltaTime
                );
            } else {
                StartCoroutine(rotate);
            }
        }

        /*
        Rotates the wrist (in a relative fashion) given some input
        pitch, yaw, and roll offsets. Easiest to see how this works by
        using the editor debugging and shift+alt+(arrow keys or s/w).

        Currently not a completely finished action. New logic is needed
        to prevent self-collisions. In particular we need to
        account for the hierarchy of rigidbodies of each arm joint and
        determine how to detect collision between a given arm joint and other arm joints.
        */
        public void RotateWristRelative(
            float pitch = 0f,
            float yaw = 0f,
            float roll = 0f,
            float speed = 10f,
            float? fixedDeltaTime = null,
            bool returnToStart = true,
            bool disableRendering = true
        ) {
            // pitch and roll are not supported for the stretch and so we throw an error
            if (pitch != 0f || roll != 0f) {
                throw new System.NotImplementedException("Pitch and roll are not supported for the stretch agent.");
            }

            Stretch_Robot_Arm_Controller arm = getArm();

            arm.rotateWrist(
                controller: this,
                rotation: Quaternion.Euler(0, yaw, 0),
                degreesPerSecond: speed,
                disableRendering: disableRendering,
                fixedDeltaTime: fixedDeltaTime.GetValueOrDefault(Time.fixedDeltaTime),
                returnToStartPositionIfFailed: returnToStart
            );
        }

        // /*
        // Rotates the elbow (in a relative fashion) by some given
        // number of degrees. Easiest to see how this works by
        // using the editor debugging and shift+alt+(q/e).

        // Currently not a completely finished action. New logic is needed
        // to prevent self-collisions. In particular we need to
        // account for the hierarchy of rigidbodies of each arm joint and
        // determine how to detect collision between a given arm joint and other arm joints.
        // */
        // public void RotateElbowRelative(
        //     float degrees,
        //     float speed = 10f,
        //     float? fixedDeltaTime = null,
        //     bool returnToStart = true,
        //     bool disableRendering = true
        // ) {
        //     Stretch_Robot_Arm_Controller arm = getArm();

        //     arm.rotateElbowRelative(
        //         controller: this,
        //         degrees: degrees,
        //         degreesPerSecond: speed,
        //         disableRendering: disableRendering,
        //         fixedDeltaTime: fixedDeltaTime.GetValueOrDefault(Time.fixedDeltaTime),
        //         returnToStartPositionIfFailed: returnToStart
        //     );
        // }

        // /*
        // Same as RotateElbowRelative but rotates the elbow to a given angle directly.
        // */
        // public void RotateElbow(
        //     float degrees,
        //     float speed = 10f,
        //     float? fixedDeltaTime = null,
        //     bool returnToStart = true,
        //     bool disableRendering = true
        // ) {
        //     Stretch_Robot_Arm_Controller arm = getArm();

        //     arm.rotateElbow(
        //         controller: this,
        //         degrees: degrees,
        //         degreesPerSecond: speed,
        //         disableRendering: disableRendering,
        //         fixedDeltaTime: fixedDeltaTime.GetValueOrDefault(Time.fixedDeltaTime),
        //         returnToStartPositionIfFailed: returnToStart
        //     );
        // }


        // constrain arm's y position based on the agent's current capsule collider center and extents
        // valid Y height from action.y is [0, 1.0] to represent the relative min and max heights of the
        // arm constrained by the agent's capsule
        public void MoveArmBase(
            float y,
            float speed = 1,
            float? fixedDeltaTime = null,
            bool returnToStart = true,
            bool disableRendering = true,
            bool normalizedY = true
        ) {
            if (normalizedY && (y < 0f || y > 1f)) {
                // Checking for bounds when normalizedY == false is handled by arm.moveArmBase
                throw new ArgumentOutOfRangeException($"y={y} value must be in [0, 1] when normalizedY=true.");
            }

            Stretch_Robot_Arm_Controller arm = getArm();
            arm.moveArmBase(
                controller: this,
                height: y,
                unitsPerSecond: speed,
                fixedDeltaTime: fixedDeltaTime.GetValueOrDefault(Time.fixedDeltaTime),
                returnToStartPositionIfFailed: returnToStart,
                disableRendering: disableRendering,
                normalizedY: normalizedY
            );
        }

        public void MoveArmBaseUp(
            float distance,
            float speed = 1,
            float? fixedDeltaTime = null,
            bool returnToStart = true,
            bool disableRendering = true
        ) {
            Stretch_Robot_Arm_Controller arm = getArm();
            arm.moveArmBaseUp(
                controller: this,
                distance: distance,
                unitsPerSecond: speed,
                fixedDeltaTime: fixedDeltaTime.GetValueOrDefault(Time.fixedDeltaTime),
                returnToStartPositionIfFailed: returnToStart,
                disableRendering: disableRendering
            );
        }

        public void MoveArmBaseDown(
            float distance,
            float speed = 1,
            float? fixedDeltaTime = null,
            bool returnToStart = true,
            bool disableRendering = true
        ) {
            MoveArmBaseUp(
                distance: -distance,
                speed: speed,
                fixedDeltaTime: fixedDeltaTime,
                returnToStart: returnToStart,
                disableRendering: disableRendering
            );
        }

#if UNITY_EDITOR
        // debug for static arm collisions from collision listener
        public void GetMidLevelArmCollisions() {
            Stretch_Robot_Arm_Controller arm = getArm();
            CollisionListener collisionListener = arm.GetComponentInChildren<CollisionListener>();
            if (collisionListener != null) {
                List<Dictionary<string, string>> collisions = new List<Dictionary<string, string>>();
                foreach (var sc in collisionListener.StaticCollisions()) {
                    Dictionary<string, string> element = new Dictionary<string, string>();
                    if (sc.simObjPhysics != null) {
                        element["objectType"] = "simObjPhysics";
                        element["name"] = sc.simObjPhysics.objectID;
                    } else {
                        element["objectType"] = "gameObject";
                        element["name"] = sc.gameObject.name;
                    }
                    collisions.Add(element);
                }
                actionFinished(true, collisions);
            }
        }

        // debug for static arm collisions from collision listener
        public void DebugMidLevelArmCollisions() {
            Stretch_Robot_Arm_Controller arm = getArm();
            List<CollisionListener.StaticCollision> scs = arm.collisionListener.StaticCollisions().ToList();
            Debug.Log("Total current active static arm collisions: " + scs.Count);
            foreach (CollisionListener.StaticCollision sc in scs) {
                Debug.Log("Arm static collision: " + sc.name);
            }
            actionFinished(true);
        }
#endif

    }
}
