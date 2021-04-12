using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using RandomExtensions;
using UnityEngine.AI;

namespace UnityStandardAssets.Characters.FirstPerson {
    [RequireComponent(typeof(CharacterController))]
    public class ArmAgentController : PhysicsRemoteFPSAgentController {
        protected IK_Robot_Arm_Controller getArm() {
            IK_Robot_Arm_Controller arm = GetComponentInChildren<IK_Robot_Arm_Controller>();
            if (arm == null) {
                throw new InvalidOperationException(
                    "Agent does not have kinematic arm or is not enabled.\n" +
                    $"Make sure there is a '{typeof(IK_Robot_Arm_Controller).Name}' component as a child of this agent."
                );
            }
            return arm;
        }

        public void MoveArm(
            Vector3 position,
            float speed = 1,
            float? fixedDeltaTime = null,
            bool returnToStart = false,
            string coordinateSpace = "armBase",
            bool restrictMovement = false,
            bool disableRendering = false
        ) {
            IK_Robot_Arm_Controller arm = getArm();
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

        // perhaps this should fail if no object is picked up?
        // currently action success happens as long as the arm is
        // enabled because it is a successful "attempt" to pickup something
        public void PickupObject(List<string> objectIdCandidates = null) {
            IK_Robot_Arm_Controller arm = getArm();
            actionFinished(arm.PickupObject(objectIdCandidates, ref errorMessage), errorMessage);
        }

        public void ReleaseObject() {
            IK_Robot_Arm_Controller arm = getArm();
            arm.DropObject();

            // TODO: only return after object(s) dropped have finished moving
            // currently this will return the frame the object is released
            actionFinished(true);
        }

        public void WhatObjectsCanHandPickUp() {
            IK_Robot_Arm_Controller arm = getArm();
            StartCoroutine(arm.ReturnObjectsInMagnetAfterPhysicsUpdate(this));
        }

        // note this does not reposition the center point of the magnet orb
        // so expanding the radius too much will cause it to clip backward into the wrist joint
        public void SetHandSphereRadius(float radius) {
            if (radius < 0.04f || radius > 0.5f) {
                throw new ArgumentOutOfRangeException(
                    $"radius={radius} of hand cannot be less than 0.04m nor greater than 0.5m"
                );
            }

            IK_Robot_Arm_Controller arm = getArm();
            arm.SetHandSphereRadius(radius);
            actionFinished(true);
        }

        // Signature does not work with debuginput field(?)
        public void MoveContinuous(
            float ahead = 0,
            float right = 0,
            float speed = 1,
            float? fixedDeltaTime = null,
            bool returnToStart = false,
            bool disableRendering = false
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

        public void RotateContinuous(
            float degrees,
            float speed = 1.0f,
            bool waitForFixedUpdate = false,
            bool returnToStart = false,
            bool disableRendering = false,
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

        // currently not finished action. New logic needs to account for the
        // hierarchy of rigidbodies of each arm joint and how to detect collision
        // between a given arm joint an other arm joints.
        public void RotateMidLevelHand(ServerAction action) {
            IK_Robot_Arm_Controller arm = getArm();
            Quaternion target = new Quaternion();

            // rotate around axis aliged x, y, z with magnitude based on vector3
            if (action.degrees == 0) {
                // use euler angles
                target = Quaternion.Euler(action.rotation);
            } else {
                // rotate action.degrees about axis
                target = Quaternion.AngleAxis(action.degrees, action.rotation);
            }

            arm.rotateHand(
                this,
                target,
                action.speed,
                action.disableRendering,
                action.fixedDeltaTime.GetValueOrDefault(Time.fixedDeltaTime),
                action.returnToStart
            );
        }

        // constrain arm's y position based on the agent's current capsule collider center and extents
        // valid Y height from action.y is [0, 1.0] to represent the relative min and max heights of the
        // arm constrained by the agent's capsule
        public void MoveArmBase(
            float y,
            float speed = 1,
            float? fixedDeltaTime = null,
            bool returnToStart = false,
            bool disableRendering = false
        ) {
            if (y < 0 || y > 1) {
                throw new ArgumentOutOfRangeException($"y={y} value must be [0, 1.0].");
            }

            IK_Robot_Arm_Controller arm = getArm();
            arm.moveArmBase(
                controller: this,
                height: y,
                unitsPerSecond: speed,
                fixedDeltaTime: fixedDeltaTime.GetValueOrDefault(Time.fixedDeltaTime),
                returnToStartPositionIfFailed: returnToStart,
                disableRendering: disableRendering
            );
        }

        #if UNITY_EDITOR
            // debug for static arm collisions from collision listener
            public void GetMidLevelArmCollisions() {
                IK_Robot_Arm_Controller arm = getArm();
                CollisionListener collisionListener = arm.GetComponentInChildren<CollisionListener>();
                if (collisionListener != null) {
                    List<Dictionary<string, string>> collisions = new List<Dictionary<string, string>>();
                    foreach (var sc in collisionListener.StaticCollisions()){
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
                IK_Robot_Arm_Controller arm = getArm();
                List<CollisionListener.StaticCollision> scs = arm.collisionListener.StaticCollisions();
                Debug.Log("Total current active static arm collisions: " + scs.Count);
                foreach (CollisionListener.StaticCollision sc  in scs) {
                    Debug.Log("Arm static collision: " + sc.name);
                }
                actionFinished(true);
            }
        #endif
    }
}
