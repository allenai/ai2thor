using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RandomExtensions;
using UnityEngine;
using UnityEngine.AI;

namespace UnityStandardAssets.Characters.FirstPerson {
    public abstract class ArmAgentController : PhysicsRemoteFPSAgentController {
        public ArmAgentController(BaseAgentComponent baseAgentComponent, AgentManager agentManager)
            : base(baseAgentComponent, agentManager) { }

        protected abstract ArmController getArm();

        /*
        Toggles the visibility of the magnet sphere at the end of the arm.
        */
        public ActionFinished ToggleMagnetVisibility(bool? visible = null) {
            MeshRenderer mr = GameObject
                .Find("MagnetRenderer")
                .GetComponentInChildren<MeshRenderer>();
            if (visible.HasValue) {
                mr.enabled = visible.Value;
            } else {
                mr.enabled = !mr.enabled;
            }
            return ActionFinished.Success;
        }

        public override void updateImageSynthesis(bool status, IEnumerable<string> activePassList = null) {
            base.updateImageSynthesis(status);

            // updateImageSynthesis is run in BaseFPSController's Initialize method after the
            // Stretch Agent's unique secondary camera has been added to the list of third party
            // cameras in InitializeBody, so a third-party camera image synthesis update is
            // necessary if we want the secondary camera's image synthesis componenent to match
            // the primary camera's
            agentManager.updateThirdPartyCameraImageSynthesis(status);
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
        public IEnumerator MoveArmRelative(
            Vector3 offset,
            float speed = 1,
            bool returnToStart = true,
            string coordinateSpace = "armBase",
            bool restrictMovement = false
        ) {
            var arm = getArm();
            return arm.moveArmRelative(
                controller: this,
                offset: offset,
                unitsPerSecond: speed,
                fixedDeltaTime: PhysicsSceneManager.fixedDeltaTime,
                returnToStart: returnToStart,
                coordinateSpace: coordinateSpace,
                restrictTargetPosition: restrictMovement
            );
        }

        public virtual IEnumerator MoveArm(
            Vector3 position,
            float speed = 1,
            bool returnToStart = true,
            string coordinateSpace = "armBase",
            bool restrictMovement = false
        ) {
            var arm = getArm();
            return arm.moveArmTarget(
                controller: this,
                target: position,
                unitsPerSecond: speed,
                fixedDeltaTime: PhysicsSceneManager.fixedDeltaTime,
                returnToStart: returnToStart,
                coordinateSpace: coordinateSpace,
                restrictTargetPosition: restrictMovement
            );
        }

        // perhaps this should fail if no object is picked up?
        // currently action success happens as long as the arm is
        // enabled because it is a successful "attempt" to pickup something
        public IEnumerator PickupObject(List<string> objectIdCandidates = null) {
            var arm = getArm();
            return arm.PickupObject(objectIdCandidates);
        }

        public override void PickupObject(
            float x,
            float y,
            bool forceAction = false,
            bool manualInteract = false
        ) {
            throw new InvalidOperationException(
                "You are passing in iTHOR PickupObject parameters (x, y) to the arm agent!"
            );
        }

        public override void PickupObject(
            string objectId,
            bool forceAction = false,
            bool manualInteract = false
        ) {
            throw new InvalidOperationException(
                "You are passing in iTHOR PickupObject parameters (objectId) to the arm agent!"
            );
        }

        public IEnumerator ReleaseObject() {
            var arm = getArm();
            return arm.DropObject();
        }

        // note this does not reposition the center point of the magnet orb
        // so expanding the radius too much will cause it to clip backward into the wrist joint
        public ActionFinished SetHandSphereRadius(float radius) {
            if (radius < 0.04f || radius > 0.5f) {
                throw new ArgumentOutOfRangeException(
                    $"radius={radius} of hand cannot be less than 0.04m nor greater than 0.5m"
                );
            }

            var arm = getArm();
            arm.SetHandSphereRadius(radius);
            return ActionFinished.Success;
        }

        public IEnumerator MoveAgent(
            bool returnToStart = true,
            float ahead = 0,
            float right = 0,
            float speed = 1
        ) {
            if (ahead == 0 && right == 0) {
                throw new ArgumentException("Must specify ahead or right!");
            }
            Vector3 direction = new Vector3(x: right, y: 0, z: ahead);

            CollisionListener collisionListener = this.GetComponentInParent<CollisionListener>();

            Vector3 directionWorld = transform.TransformDirection(direction);
            Vector3 targetPosition = transform.position + directionWorld;

            collisionListener.Reset();

            return ContinuousMovement.move(
                movable: this.getArm(),
                controller: this,
                moveTransform: this.transform,
                targetPosition: targetPosition,
                fixedDeltaTime: PhysicsSceneManager.fixedDeltaTime,
                unitsPerSecond: speed,
                returnToStartPropIfFailed: returnToStart,
                localPosition: false
            );
        }

        public IEnumerator MoveAhead(
            float? moveMagnitude = null,
            float speed = 1,
            bool returnToStart = true
        ) {
            return MoveAgent(
                ahead: moveMagnitude.GetValueOrDefault(gridSize),
                speed: speed,
                returnToStart: returnToStart
            );
        }

        public IEnumerator MoveBack(
            float? moveMagnitude = null,
            float speed = 1,
            bool returnToStart = true
        ) {
            return MoveAgent(
                ahead: -moveMagnitude.GetValueOrDefault(gridSize),
                speed: speed,
                returnToStart: returnToStart
            );
        }

        public IEnumerator MoveRight(
            float? moveMagnitude = null,
            float speed = 1,
            bool returnToStart = true
        ) {
            return MoveAgent(
                right: moveMagnitude.GetValueOrDefault(gridSize),
                speed: speed,
                returnToStart: returnToStart
            );
        }

        public IEnumerator MoveLeft(
            float? moveMagnitude = null,
            float speed = 1,
            bool returnToStart = true
        ) {
            return MoveAgent(
                right: -moveMagnitude.GetValueOrDefault(gridSize),
                speed: speed,
                returnToStart: returnToStart
            );
        }

        public IEnumerator RotateRight(
            float? degrees = null,
            float speed = 1.0f,
            bool returnToStart = true
        ) {
            return RotateAgent(
                degrees: degrees.GetValueOrDefault(rotateStepDegrees),
                speed: speed,
                returnToStart: returnToStart
            );
        }

        public IEnumerator RotateLeft(
            float? degrees = null,
            float speed = 1.0f,
            bool returnToStart = true
        ) {
            return RotateAgent(
                degrees: -degrees.GetValueOrDefault(rotateStepDegrees),
                speed: speed,
                returnToStart: returnToStart
            );
        }

        public virtual IEnumerator RotateAgent(
            float degrees,
            float speed = 1.0f,
            bool returnToStart = true
        ) {
            CollisionListener collisionListener = this.GetComponentInParent<CollisionListener>();
            collisionListener.Reset();

            // this.transform.Rotate()
            return ContinuousMovement.rotate(
                movable: this.getArm(),
                controller: this,
                moveTransform: this.transform,
                targetRotation: this.transform.rotation * Quaternion.Euler(0.0f, degrees, 0.0f),
                fixedDeltaTime: PhysicsSceneManager.fixedDeltaTime,
                radiansPerSecond: speed,
                returnToStartPropIfFailed: returnToStart
            );
        }

        public override void Teleport(
            Vector3? position = null,
            Vector3? rotation = null,
            float? horizon = null,
            bool? standing = null,
            bool forceAction = false
        ) {
            //non-high level agents cannot set standing
            if (standing != null) {
                errorMessage = "Cannot set standing for arm/stretch agent";
                actionFinishedEmit(success: false, actionReturn: null, errorMessage: errorMessage);
                return;
            }

            TeleportFull(
                position: position,
                rotation: rotation,
                horizon: horizon,
                standing: standing,
                forceAction: forceAction
            );
        }

        public override void TeleportFull(
            Vector3? position = null,
            Vector3? rotation = null,
            float? horizon = null,
            bool? standing = null,
            bool forceAction = false
        ) {
            //non-high level agents cannot set standing
            if (standing != null) {
                errorMessage = "Cannot set standing for arm/stretch agent";
                actionFinishedEmit(success: false, actionReturn: null, errorMessage: errorMessage);
                return;
            }

            //cache old values in case there is a failure
            Vector3 oldPosition = transform.position;
            Quaternion oldRotation = transform.rotation;
            Quaternion oldCameraRotation = m_Camera.transform.localRotation;

            try {
                base.teleportFull(
                    position: position,
                    rotation: rotation,
                    horizon: horizon,
                    forceAction: forceAction
                );

                // add arm value cases
                if (!forceAction) {
                    if (Arm != null && Arm.IsArmColliding()) {
                        throw new InvalidOperationException(
                            "Mid Level Arm is actively clipping with some geometry in the environment. TeleportFull fails in this position."
                        );
                    } else if (SArm != null && SArm.IsArmColliding()) {
                        throw new InvalidOperationException(
                            "Stretch Arm is actively clipping with some geometry in the environment. TeleportFull fails in this position."
                        );
                    }
                    base.assertTeleportedNearGround(targetPosition: position);
                }
            } catch (InvalidOperationException e) {
                transform.position = oldPosition;
                transform.rotation = oldRotation;
                m_Camera.transform.localRotation = oldCameraRotation;

                throw new InvalidOperationException(e.Message);
            }

            actionFinished(success: true);
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
        public virtual IEnumerator RotateWristRelative(
            float pitch = 0f,
            float yaw = 0f,
            float roll = 0f,
            float speed = 10f,
            bool returnToStart = true
        ) {
            var arm = getArm();
            return arm.rotateWrist(
                controller: this,
                rotation: Quaternion.Euler(pitch, yaw, -roll),
                degreesPerSecond: speed,
                fixedDeltaTime: PhysicsSceneManager.fixedDeltaTime,
                returnToStartPositionIfFailed: returnToStart
            );
        }

        // constrain arm's y position based on the agent's current capsule collider center and extents
        // valid Y height from action.y is [0, 1.0] to represent the relative min and max heights of the
        // arm constrained by the agent's capsule
        public virtual IEnumerator MoveArmBase(
            float y,
            float speed = 1,
            bool returnToStart = true,
            bool normalizedY = true
        ) {
            if (normalizedY && (y < 0f || y > 1f)) {
                // Checking for bounds when normalizedY == false is handled by arm.moveArmBase
                throw new ArgumentOutOfRangeException(
                    $"y={y} value must be in [0, 1] when normalizedY=true."
                );
            }

            var arm = getArm();
            return arm.moveArmBase(
                controller: this,
                height: y,
                unitsPerSecond: speed,
                fixedDeltaTime: PhysicsSceneManager.fixedDeltaTime,
                returnToStartPositionIfFailed: returnToStart,
                normalizedY: normalizedY
            );
        }

        public virtual IEnumerator MoveArmBaseUp(
            float distance,
            float speed = 1,
            bool returnToStart = true
        ) {
            var arm = getArm();
            return arm.moveArmBaseUp(
                controller: this,
                distance: distance,
                unitsPerSecond: speed,
                fixedDeltaTime: PhysicsSceneManager.fixedDeltaTime,
                returnToStartPositionIfFailed: returnToStart
            );
        }

        public virtual IEnumerator MoveArmBaseDown(
            float distance,
            float speed = 1,
            bool returnToStart = true
        ) {
            return MoveArmBaseUp(distance: -distance, speed: speed, returnToStart: returnToStart);
        }

#if UNITY_EDITOR
        // debug for static arm collisions from collision listener
        public void GetMidLevelArmCollisions() {
            var arm = getArm();
            CollisionListener collisionListener = arm.GetComponentInChildren<CollisionListener>();
            if (collisionListener != null) {
                List<Dictionary<string, string>> collisions =
                    new List<Dictionary<string, string>>();
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
            var arm = getArm();
            List<StaticCollision> scs = arm.collisionListener.StaticCollisions().ToList();
            Debug.Log("Total current active static arm collisions: " + scs.Count);
            foreach (StaticCollision sc in scs) {
                Debug.Log("Arm static collision: " + sc.name);
            }
            actionFinished(true);
        }
#endif
    }
}
