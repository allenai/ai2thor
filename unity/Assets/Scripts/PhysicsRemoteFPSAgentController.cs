// Copyright Allen Institute for Artificial Intelligence 2017

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Priority_Queue;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Animations;
using UnityEngine.SceneManagement;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.ImageEffects;
using UnityStandardAssets.Utility;
using RandomExtensions;
using Thor.Procedural;
using Thor.Procedural.Data;
using UnityEngine.Rendering.PostProcessing;

namespace UnityStandardAssets.Characters.FirstPerson {
    public class OrientedPoint {
        public Vector3 position = new Vector3();
        public Quaternion orientation = new Quaternion();
    }

    public partial class PhysicsRemoteFPSAgentController : BaseFPSAgentController {
        protected Dictionary<string, Dictionary<int, Material[]>> maskedObjects = new Dictionary<string, Dictionary<int, Material[]>>();
        bool transparentStructureObjectsHidden = false;
        // face swap stuff here

        public PhysicsRemoteFPSAgentController(BaseAgentComponent baseAgentComponent, AgentManager agentManager) : base(baseAgentComponent, agentManager) {
        }

        public override void InitializeBody() {
            VisibilityCapsule = TallVisCap;
            m_CharacterController.center = new Vector3(0, 0, 0);
            m_CharacterController.radius = 0.2f;
            m_CharacterController.height = 1.8f;

            CapsuleCollider cc = this.GetComponent<CapsuleCollider>();
            cc.center = m_CharacterController.center;
            cc.radius = m_CharacterController.radius;
            cc.height = m_CharacterController.height;

            m_Camera.GetComponent<PostProcessVolume>().enabled = false;
            m_Camera.GetComponent<PostProcessLayer>().enabled = false;

            // camera position
            m_Camera.transform.localPosition = new Vector3(0, 0.675f, 0);

            // camera FOV
            m_Camera.fieldOfView = 90f;

            // set camera stand/crouch local positions for Tall mode
            standingLocalCameraPosition = m_Camera.transform.localPosition;
            crouchingLocalCameraPosition = m_Camera.transform.localPosition + new Vector3(0, -0.675f, 0); // bigger y offset if tall
        }

        // change visibility check to use this distance when looking down
        // protected float DownwardViewDistance = 2.0f;

        // forceVisible is true to activate, false to deactivate
        public void ToggleHideAndSeekObjects(bool forceVisible = false) {
            if (physicsSceneManager.ToggleHideAndSeek(forceVisible)) {
                physicsSceneManager.ResetObjectIdToSimObjPhysics();
                actionFinished(true);
            } else {
                errorMessage = "No HideAndSeek object found";
                actionFinished(false);
            }
        }

        public Vector3 AgentHandLocation() {
            return AgentHand.transform.position;
        }

        public GameObject WhatAmIHolding() {
            return ItemInHand;
        }

        public void EnableTemperatureDecay() {
            if (!physicsSceneManager.GetComponent<PhysicsSceneManager>().AllowDecayTemperature) {
                physicsSceneManager.GetComponent<PhysicsSceneManager>().AllowDecayTemperature = true;
            }
            actionFinished(true);
        }

        public void DisableTemperatureDecay() {
            if (physicsSceneManager.GetComponent<PhysicsSceneManager>().AllowDecayTemperature) {
                physicsSceneManager.GetComponent<PhysicsSceneManager>().AllowDecayTemperature = false;
            }
            actionFinished(true);
        }

        // sets temperature decay for a single object.
        public void SetTemperatureDecayTime(string objectId, float decayTime) {
            if (decayTime < 0) {
                throw new ArgumentOutOfRangeException("decayTime must be >= 0. You gave " + decayTime);
            }
            SimObjPhysics sop = getInteractableSimObjectFromId(objectId: objectId, forceAction: true);
            sop.SetHowManySecondsUntilRoomTemp(decayTime);
            actionFinished(true);
        }

        // globally sets temperature decay for all objects.
        public void SetTemperatureDecayTime(float decayTime) {
            if (decayTime < 0) {
                throw new ArgumentOutOfRangeException("decayTime must be >= 0. You gave " + decayTime);
            }
            SimObjPhysics[] simObjects = GameObject.FindObjectsOfType<SimObjPhysics>();
            foreach (SimObjPhysics sop in simObjects) {
                sop.SetHowManySecondsUntilRoomTemp(decayTime);
            }
            actionFinished(true);
        }

        // change the mass/drag/angular drag values of a simobjphys that is pickupable or moveable
        public void SetMassProperties(string objectId, float mass, float drag, float angularDrag) {
            if (objectId == null) {
                errorMessage = "please give valid ObjectID for SetMassProperties() action";
                actionFinished(false);
                return;
            }

            SimObjPhysics[] simObjects = GameObject.FindObjectsOfType<SimObjPhysics>();
            foreach (SimObjPhysics sop in simObjects) {
                if (sop.objectID == objectId) {
                    if (sop.PrimaryProperty == SimObjPrimaryProperty.Moveable || sop.PrimaryProperty == SimObjPrimaryProperty.CanPickup) {
                        Rigidbody rb = sop.GetComponent<Rigidbody>();
                        rb.mass = mass;
                        rb.drag = drag;
                        rb.angularDrag = angularDrag;

                        actionFinished(true);
                        return;
                    }

                    errorMessage = "object with ObjectID: " + objectId + ", is not Moveable or Pickupable, and the Mass Properties cannot be changed";
                    actionFinished(false);
                    return;
                }
            }

            errorMessage = "object with ObjectID: " + objectId + ", could not be found in this scene";
            actionFinished(false);
            return;
        }

        public bool isStanding() {
            return (m_Camera.transform.localPosition - standingLocalCameraPosition).magnitude < 0.1f;
        }

        public override MetadataWrapper generateMetadataWrapper() {
            MetadataWrapper metaWrapper = base.generateMetadataWrapper();
            metaWrapper.agent.isStanding = isStanding();
            return metaWrapper;
        }

        // change the radius of the agent's capsule on the char controller component, and the capsule collider component
        public void SetAgentRadius(float agentRadius = 2.0f) {
            m_CharacterController.radius = agentRadius;
            CapsuleCollider cap = GetComponent<CapsuleCollider>();
            cap.radius = agentRadius;
            actionFinished(true);
        }

        // EDITOR DEBUG SCRIPTS:
        //////////////////////////////////////////////////////////////////////
#if UNITY_EDITOR

        // return ID of closest CanPickup object by distance
        public string ObjectIdOfClosestVisibleObject() {
            string objectID = null;

            foreach (SimObjPhysics o in VisibleSimObjPhysics) {
                if (o.PrimaryProperty == SimObjPrimaryProperty.CanPickup) {
                    objectID = o.ObjectID;
                    //  print(objectID);
                    break;
                }
            }

            return objectID;
        }

        public string ObjectIdOfClosestPickupableOrMoveableObject() {
            string objectID = null;

            foreach (SimObjPhysics o in VisibleSimObjPhysics) {
                if (o.PrimaryProperty == SimObjPrimaryProperty.CanPickup || o.PrimaryProperty == SimObjPrimaryProperty.Moveable) {
                    objectID = o.ObjectID;
                    //  print(objectID);
                    break;
                }
            }

            return objectID;
        }

        // return ID of closest CanOpen or CanOpen_Fridge object by distance
        public string ObjectIdOfClosestVisibleOpenableObject() {
            string objectID = null;

            foreach (SimObjPhysics o in VisibleSimObjPhysics) {
                if (o.GetComponent<CanOpen_Object>()) {
                    objectID = o.ObjectID;
                    break;
                }
            }

            return objectID;
        }

        // return ID of closes toggleable object by distance
        public string ObjectIdOfClosestToggleObject() {
            string objectID = null;

            foreach (SimObjPhysics o in VisibleSimObjPhysics) {
                if (o.GetComponent<CanToggleOnOff>()) {
                    objectID = o.ObjectID;
                    break;
                }
            }

            return objectID;
        }

        public string ObjectIdOfClosestReceptacleObject() {
            string objectID = null;

            foreach (SimObjPhysics o in VisibleSimObjPhysics) {
                if (o.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.Receptacle)) {
                    objectID = o.ObjectID;
                    break;
                }
            }
            return objectID;
        }
#endif
        /////////////////////////////////////////////////////////
        // return a reference to a SimObj that is Visible (in the VisibleSimObjPhysics array) and
        // matches the passed in objectID
        public GameObject FindObjectInVisibleSimObjPhysics(string objectId) {
            foreach (SimObjPhysics sop in VisibleSimObjs(false)) {
                if (sop.ObjectID == objectId) {
                    return sop.gameObject;
                }
            }
            return null;
        }

        protected Collider[] collidersWithinCapsuleCastOfAgent(float maxDistance) {
            CapsuleCollider agentCapsuleCollider = GetComponent<CapsuleCollider>();
            Vector3 point0, point1;
            float radius;
            agentCapsuleCollider.ToWorldSpaceCapsule(out point0, out point1, out radius);
            if (point0.y <= point1.y) {
                point1.y += maxDistance;
            } else {
                point0.y += maxDistance;
            }
            return Physics.OverlapCapsule(point0, point1, maxDistance, LayerMask.GetMask("SimObjVisible", "Procedural1", "Procedural2", "Procedural3", "Procedural0"), QueryTriggerInteraction.Collide);
        }


        // checks if a float is a multiple of 0.1f
        private bool CheckIfFloatIsMultipleOfOneTenth(float f) {
            if (((decimal)f % 0.1M == 0) == false) {
                return false;
            } else {
                return true;
            }
        }

        public override void LookDown(ServerAction action) {
            if (action.degrees < 0) {
                errorMessage = "LookDown action requires positive degree value. Invalid value used: " + action.degrees;
                actionFinished(false);
                return;
            }

            if (!CheckIfFloatIsMultipleOfOneTenth(action.degrees)) {
                errorMessage = "LookDown action requires degree value to be a multiple of 0.1f";
                actionFinished(false);
                return;
            }

            // default degree increment to 30
            if (action.degrees == 0) {
                action.degrees = 30f;
            }

            // force the degree increment to the nearest tenths place
            // this is to prevent too small of a degree increment change that could cause float imprecision
            action.degrees = Mathf.Round(action.degrees * 10.0f) / 10.0f;

            if (!checkForUpDownAngleLimit("down", action.degrees)) {
                errorMessage = "can't look down beyond " + maxDownwardLookAngle + " degrees below the forward horizon";
                errorCode = ServerActionErrorCode.LookDownCantExceedMin;
                actionFinished(false);
                return;
            }

            if (CheckIfAgentCanRotate("down", action.degrees)) {
                base.LookDown(action);

                // only default hand if not manually Interacting with things
                if (!action.manualInteract) {
                    DefaultAgentHand();
                }
            } else {
                errorMessage = "a held item: " + ItemInHand.transform.GetComponent<SimObjPhysics>().objectID + " will collide with something if agent rotates down " + action.degrees + " degrees";
                actionFinished(false);
            }

        }

        public override void LookUp(ServerAction action) {

            if (action.degrees < 0) {
                errorMessage = "LookUp action requires positive degree value. Invalid value used: " + action.degrees;
                actionFinished(false);
                return;
            }

            if (!CheckIfFloatIsMultipleOfOneTenth(action.degrees)) {
                errorMessage = "LookUp action requires degree value to be a multiple of 0.1f";
                actionFinished(false);
                return;
            }

            // default degree increment to 30
            if (action.degrees == 0) {
                action.degrees = 30f;
            }

            // force the degree increment to the nearest tenths place
            // this is to prevent too small of a degree increment change that could cause float imprecision
            action.degrees = Mathf.Round(action.degrees * 10.0f) / 10.0f;

            if (!checkForUpDownAngleLimit("up", action.degrees)) {
                errorMessage = "can't look up beyond " + maxUpwardLookAngle + " degrees above the forward horizon";
                errorCode = ServerActionErrorCode.LookDownCantExceedMin;
                actionFinished(false);
                return;
            }

            if (CheckIfAgentCanRotate("up", action.degrees)) {
                base.LookUp(action);

                // only default hand if not manually Interacting with things
                if (!action.manualInteract) {
                    DefaultAgentHand();
                }
            } else {
                errorMessage = "a held item: " + ItemInHand.transform.GetComponent<SimObjPhysics>().objectID + " will collide with something if agent rotates up " + action.degrees + " degrees";
                actionFinished(false);
            }
        }

        public virtual void RotateRight(
            float? degrees = null,
            bool manualInteract = false,
            bool forceAction = false,
            float speed = 1.0f,              // TODO: Unused, remove when refactoring the controllers
            bool waitForFixedUpdate = false, // TODO: Unused, remove when refactoring the controllers
            bool returnToStart = true,       // TODO: Unused, remove when refactoring the controllers
            bool disableRendering = true,    // TODO: Unused, remove when refactoring the controllers
            float fixedDeltaTime = 0.02f     // TODO: Unused, remove when refactoring the controllers
        ) {
            if (!degrees.HasValue) {
                degrees = rotateStepDegrees;
            } else if (degrees == 0f) {
                throw new InvalidOperationException(
                    "Cannot rotate by 0 degrees as this previously defaulted to rotating by a diferent amount."
                );
            }

            if (CheckIfAgentCanRotate("right", degrees.Value) || forceAction) {

                transform.Rotate(0, degrees.Value, 0);

                // only default hand if not manually Interacting with things
                if (!manualInteract) {
                    DefaultAgentHand();
                }

                actionFinished(true);
            } else {
                errorMessage = $"a held item: {ItemInHand.transform.name} with something if agent rotates Right {degrees} degrees";
                actionFinished(false);
            }
        }

        public virtual void RotateLeft(
            float? degrees = null,
            bool manualInteract = false,
            bool forceAction = false,
            float speed = 1.0f,              // TODO: Unused, remove when refactoring the controllers
            bool waitForFixedUpdate = false, // TODO: Unused, remove when refactoring the controllers
            bool returnToStart = true,       // TODO: Unused, remove when refactoring the controllers
            bool disableRendering = true,    // TODO: Unused, remove when refactoring the controllers
            float fixedDeltaTime = 0.02f     // TODO: Unused, remove when refactoring the controllers
        ) {
            if (!degrees.HasValue) {
                degrees = rotateStepDegrees;
            } else if (degrees == 0f) {
                throw new InvalidOperationException(
                    "Cannot rotate by 0 degrees as this previously defaulted to rotating by a diferent amount."
                );
            }

            if (CheckIfAgentCanRotate("left", degrees.Value) || forceAction) {
                transform.Rotate(0, -degrees.Value, 0);

                // only default hand if not manually Interacting with things
                if (!manualInteract) {
                    DefaultAgentHand();
                }

                actionFinished(true);
            } else {
                errorMessage = $"a held item: {ItemInHand.transform.name} with something if agent rotates Left {degrees} degrees";
                actionFinished(false);
            }
        }

        private bool checkArcForCollisions(BoxCollider bb, Vector3 origin, float degrees, int dirSign, Vector3 dirAxis) {
            bool result = true;
            float arcIncrementDistance;
            Vector3 bbWorldCenter = bb.transform.TransformPoint(bb.center);
            Vector3 bbHalfExtents = bb.size / 2.0f;

            // Generate arc points in the positive y-axis rotation
            OrientedPoint[] pointsOnArc = GenerateArcPoints(bbWorldCenter, bb.transform.rotation, origin, degrees, dirSign, dirAxis);

            // Save the arc-distance to a value to reduce computation in the for-loop, since it's the same between every OrientedPoint
            arcIncrementDistance = (pointsOnArc[1].position - pointsOnArc[0].position).magnitude;

            // Raycast from first point in pointsOnArc, stepwise to last point. If any collisions are hit, immediately return 
            for (int i = 0; i < pointsOnArc.Length - 1; i++) {
                RaycastHit hit;
                // do boxcasts from the first point, sequentially, to the last

                // Debug.DrawLine(pointsOnArc[i].position, pointsOnArc[i+1].position, Color.magenta, 500.0f);

                if (
                    Physics.BoxCast(
                        pointsOnArc[i].position,
                        bbHalfExtents,
                        pointsOnArc[i + 1].position - pointsOnArc[i].position,
                        out hit,
                        Quaternion.Lerp(
                            pointsOnArc[i].orientation,
                            pointsOnArc[i + 1].orientation,
                            0.5f
                        ),
                        arcIncrementDistance,
                        LayerMask.GetMask("SimObjVisible", "Procedural1", "Procedural2", "Procedural3", "Procedural0", "Agent"),
                        QueryTriggerInteraction.Ignore
                    )
                ) {
                    // did we hit a sim obj?
                    if (
                        (
                            hit.transform.GetComponentInParent<SimObjPhysics>()
                            && hit.transform.GetComponentInParent<SimObjPhysics>().transform == ItemInHand.transform
                        ) || (
                            hit.transform == this.transform
                        )
                    ) {
                        // if the sim obj we hit is what we are holding, skip
                        // don't worry about clipping the object into this agent
                        continue;
                    }

                    result = false;
                    break;
                }
            }

            // OverlapBox check for final rotated state of GameObject, since arc-points are using averaged orientations, and the final state of orientation is high-priority
            rotPoint.transform.position = bbWorldCenter;
            rotPoint.transform.rotation = bb.transform.rotation;
            // Rotate the rotPoint around the origin by the current increment's angle, relative to the correct axis
            rotPoint.transform.RotateAround(origin, dirAxis, dirSign * degrees);
            Collider[] WhatDidWeHit = Physics.OverlapBox(
                rotPoint.position,
                bbHalfExtents,
                rotPoint.transform.rotation,
                LayerMask.GetMask("SimObjVisible", "Procedural1", "Procedural2", "Procedural3", "Procedural0", "Agent"),
                QueryTriggerInteraction.Ignore
            );

            foreach (Collider col in WhatDidWeHit) {
                if (col.transform.GetComponentInParent<SimObjPhysics>()) {
                    if (col.transform.GetComponentInParent<SimObjPhysics>().transform == ItemInHand.transform) {
                        continue;
                    }
                }

                if (col.transform == this.transform) {
                    continue;
                }

                result = false;
                break;
            }

            return result;
        }

        // Returns an array of OrientedPoints along the arc of the rotation for a given starting point about an origin point for a total given angle
        private OrientedPoint[] GenerateArcPoints(Vector3 startingPoint, Quaternion startingRotation, Vector3 origin, float angle, int dirSign, Vector3 dirAxis) {
            float incrementAngle = angle / 10f; // divide the total amount we are rotating by 10 to get 10 points on the arc for positions
            OrientedPoint[] arcPoints = new OrientedPoint[11]; // we just always want 10 points in addition to our starting corner position (11 total) to check against per corner
            float currentIncrementAngle;

            // Calculate positions of all 10 OrientedPoints
            for (int i = 0; i < arcPoints.Length; i++) {
                currentIncrementAngle = i * dirSign * incrementAngle;
                // Move and orient the rotPoint to the bb's position and orientation
                rotPoint.transform.position = startingPoint;
                rotPoint.transform.rotation = startingRotation;
                // Rotate the rotPoint around the origin by the current increment's angle, relative to the correct axis
                rotPoint.transform.RotateAround(origin, dirAxis, currentIncrementAngle);

                arcPoints[i] = new OrientedPoint();
                // set the current arcPoint's position to the rotated point
                arcPoints[i].position = rotPoint.transform.position;
                arcPoints[i].orientation = rotPoint.transform.rotation;
                // arcPoints[i] = RotatePointAroundPivot(startingPoint, origin, new Vector3(0, currentIncrementAngle, 0));
                // arcPoints[(i - 1) / 2].orientation = rotPoint.transform.rotation;

                //// Visualize box volumes
                // GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                // cube.transform.position = arcPoints[i].position;
                // cube.transform.rotation = arcPoints[i].orientation;
                // cube.transform.localScale = new Vector3(size.x, size.y, size.z);
                // cube.GetComponent<Renderer>().material.color = UnityEngine.Random.ColorHSV();
            }

            return arcPoints;
        }


        // TODO: I dunno who was using this or for what, but it doesn't play nice with the new rotate functions so please add back functionality later
        //  public void RotateRightSmooth(ServerAction controlCommand) {
        //     if (CheckIfAgentCanTurn(90)) {
        //         DefaultAgentHand(controlCommand);
        //         StartCoroutine(InterpolateRotation(this.GetRotateQuaternion(1), controlCommand.timeStep));
        //     } else {
        //         actionFinished(false);
        //     }
        // }

        // public void RotateLeftSmooth(ServerAction controlCommand) {
        //     if (CheckIfAgentCanTurn(-90)) {
        //         DefaultAgentHand(controlCommand);
        //         StartCoroutine(InterpolateRotation(this.GetRotateQuaternion(-1), controlCommand.timeStep));
        //     } else {
        //         actionFinished(false);
        //     }
        // }

        // checks if agent is clear to rotate left/right/up/down some number of degrees while holding an object
        public bool CheckIfAgentCanRotate(string direction, float degrees) {

            if (ItemInHand == null) {
                // Debug.Log("Rotation check passed: nothing in Agent Hand");
                return true;
            }

            bool result = true;

            // Get held object's bounding box
            BoxCollider bb = ItemInHand.GetComponent<SimObjPhysics>().BoundingBox.GetComponent<BoxCollider>();

            // Establish the directionality of specified rotation
            int dirSign = -1;
            Vector3 dirAxis = transform.up;
            Vector3 origin = m_CharacterController.transform.position;

            // Yawing left (Rotating negatively across XZ plane around CharacterController)
            if (direction == "left") {
                dirSign = -1;
                dirAxis = transform.up;
                origin = m_CharacterController.transform.position;
            }

            // Yawing right (Rotating positively across XZ plane around CharacterController)
            else if (direction == "right") {
                dirSign = 1;
                dirAxis = transform.up;
                origin = m_CharacterController.transform.position;
            }

            // Pitching up (Rotating negatively across YZ plane around camera)
            else if (direction == "up") {
                dirSign = -1;
                dirAxis = transform.right;
                origin = m_Camera.transform.position;
            }

            // Pitching down (Rotating positively across YZ plane around camera)
            else if (direction == "down") {
                dirSign = 1;
                dirAxis = transform.right;
                origin = m_Camera.transform.position;
            }

            result = checkArcForCollisions(bb, origin, degrees, dirSign, dirAxis);

            // no checks flagged anything, good to go, return true i guess
            return result;
        }

        public void TeleportObject(
            string objectId,
            Vector3 position,
            Vector3 rotation,
            bool forceAction = false,
            bool forceKinematic = false,
            bool allowTeleportOutOfHand = false,
            bool makeUnbreakable = false
        ) {
            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(objectId)) {
                errorMessage = $"Cannot find object with id {objectId}";
                actionFinished(false);
                return;
            }

            SimObjPhysics sop = physicsSceneManager.ObjectIdToSimObjPhysics[objectId];
            bool teleportSuccess = TeleportObject(
                sop: sop,
                position: position,
                rotation: rotation,
                forceAction: forceAction,
                forceKinematic: forceKinematic,
                allowTeleportOutOfHand: allowTeleportOutOfHand,
                makeUnbreakable: makeUnbreakable,
                includeErrorMessage: true
            );

            if (teleportSuccess) {
                if (!forceKinematic) {
                    StartCoroutine(checkIfObjectHasStoppedMoving(sop, 0, true));
                    return;
                } else {
                    actionFinished(true);
                    return;
                }
            } else {
                actionFinished(false);
                return;
            }
        }

        public void TeleportObject(
            string objectId,
            Vector3[] positions,
            Vector3 rotation,
            bool forceAction = false,
            bool forceKinematic = false,
            bool allowTeleportOutOfHand = false,
            bool makeUnbreakable = false
        ) {
            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(objectId)) {
                errorMessage = $"Cannot find object with id {objectId}";
                actionFinished(false);
                return;
            }
            SimObjPhysics sop = physicsSceneManager.ObjectIdToSimObjPhysics[objectId];

            bool teleportSuccess = false;
            foreach (Vector3 position in positions) {
                teleportSuccess = TeleportObject(
                    sop: sop,
                    position: position,
                    rotation: rotation,
                    forceAction: forceAction,
                    forceKinematic: forceKinematic,
                    allowTeleportOutOfHand: allowTeleportOutOfHand,
                    makeUnbreakable: makeUnbreakable,
                    includeErrorMessage: true
                );
                if (teleportSuccess) {
                    errorMessage = "";
                    break;
                }
            }

            if (teleportSuccess) {
                // TODO: Do we want to wait for objects to stop moving when teleported?
                // if (!forceKinematic) {
                //     StartCoroutine(checkIfObjectHasStoppedMoving(sop, 0, true));
                //     return;
                // }
                actionFinished(true);
                return;
            } else {
                actionFinished(false);
                return;
            }
        }

        public bool TeleportObject(
            SimObjPhysics sop,
            Vector3 position,
            Vector3 rotation,
            bool forceAction,
            bool forceKinematic,
            bool allowTeleportOutOfHand,
            bool makeUnbreakable,
            bool includeErrorMessage = false
        ) {
            bool sopInHand = ItemInHand != null && sop == ItemInHand.GetComponent<SimObjPhysics>();
            if (sopInHand && !allowTeleportOutOfHand) {
                if (includeErrorMessage) {
                    errorMessage = "Cannot teleport object in hand.";
                }
                return false;
            }
            Vector3 oldPosition = sop.transform.position;
            Quaternion oldRotation = sop.transform.rotation;

            sop.transform.position = position;
            sop.transform.rotation = Quaternion.Euler(rotation);
            if (forceKinematic) {
                sop.GetComponent<Rigidbody>().isKinematic = true;
            }
            if (!forceAction) {
                Collider colliderHitIfTeleported = UtilityFunctions.firstColliderObjectCollidingWith(sop.gameObject);
                if (colliderHitIfTeleported != null) {
                    sop.transform.position = oldPosition;
                    sop.transform.rotation = oldRotation;
                    SimObjPhysics hitSop = ancestorSimObjPhysics(colliderHitIfTeleported.gameObject);
                    if (includeErrorMessage) {
                        errorMessage = $"{sop.ObjectID} is colliding with {(hitSop != null ? hitSop.ObjectID : colliderHitIfTeleported.name)} after teleport.";
                    }
                    return false;
                }
            }

            if (makeUnbreakable) {
                if (sop.GetComponent<Break>()) {
                    sop.GetComponent<Break>().Unbreakable = true;
                }
            }

            if (sopInHand) {
                if (!forceKinematic) {
                    Rigidbody rb = ItemInHand.GetComponent<Rigidbody>();
                    rb.constraints = RigidbodyConstraints.None;
                    rb.useGravity = true;
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                }
                GameObject topObject = GameObject.Find("Objects");
                if (topObject != null) {
                    ItemInHand.transform.parent = topObject.transform;
                } else {
                    ItemInHand.transform.parent = null;
                }

                sop.DropContainedObjects(reparentContainedObjects: true, forceKinematic: forceKinematic);
                sop.isInAgentHand = false;
                ItemInHand = null;
            }

            return true;
        }

        public void TeleportObject(
            string objectId,
            float x,
            float y,
            float z,
            Vector3 rotation,
            bool forceAction = false,
            bool forceKinematic = false,
            bool allowTeleportOutOfHand = false,
            bool makeUnbreakable = false
        ) {
            TeleportObject(
                objectId: objectId,
                position: new Vector3(x, y, z),
                rotation: rotation,
                forceAction: forceAction,
                forceKinematic: forceKinematic,
                allowTeleportOutOfHand: allowTeleportOutOfHand,
                makeUnbreakable: makeUnbreakable
            );
        }

        // params are named x,y,z due to the action orignally using ServerAction.x,y,z
        public void ChangeAgentColor(float x, float y, float z) {
            agentManager.UpdateAgentColor(this, new Color(x, y, z, 1.0f));
            actionFinished(true);
        }

        protected Vector3 closestPointToObject(SimObjPhysics sop) {
            float closestDist = 10000.0f;
            Vector3 closestPoint = new Vector3(0f, 0f, 0f);

            foreach (Collider c in sop.GetComponentsInChildren<Collider>()) {
                Vector3 point = c.ClosestPointOnBounds(transform.position);
                float dist = Vector3.Distance(
                    transform.position, c.ClosestPointOnBounds(transform.position)
                );
                if (dist < closestDist) {
                    closestDist = dist;
                    closestPoint = point;
                }
            }
            return closestPoint;
        }

        public void PointsOverTableWhereHandCanBe(string objectId, float x, float z) {
            // Assumes InitializeTableSetting has been run before calling this

            string tableId = objectId;

            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(objectId)) {
                errorMessage = $"Cannot find object with id {objectId}.";
                actionFinishedEmit(false);
                return;
            }

            int xSteps = Convert.ToInt32(Math.Abs(x / 0.1f));
            int zStart = Convert.ToInt32(Math.Abs(z / 0.1f));

            DefaultAgentHand();

            AgentHand.transform.position = AgentHand.transform.position;

            if (ItemInHand != null) {
                ItemInHand.SetActive(false);
            }
            List<Vector3> goodPositions = new List<Vector3>();
            for (int i = -xSteps; i <= xSteps; i++) {
                for (int j = zStart; j < 11; j++) {
                    DefaultAgentHand();

                    Vector3 testPosition = AgentHand.transform.position + 0.1f * i * transform.right + 0.1f * j * transform.forward;

                    RaycastHit hit;
                    if (Physics.Raycast(testPosition, -transform.up, out hit, 1f, LayerMask.GetMask("SimObjVisible", "Procedural1", "Procedural2", "Procedural3", "Procedural0"))) {
                        Vector3 viewportPoint = m_Camera.WorldToViewportPoint(hit.point);
                        if (viewportPoint.x >= 0f && viewportPoint.x <= 1f && viewportPoint.y >= 0f && viewportPoint.y <= 1f) {
                            SimObjPhysics hitSop = hit.transform.gameObject.GetComponent<SimObjPhysics>();
                            if (hitSop && hitSop.ObjectID == tableId) {
                                goodPositions.Add(hit.point);
#if UNITY_EDITOR
                                Debug.Log("Point");
                                Debug.Log(hit.point.x);
                                Debug.Log(hit.point.y);
                                Debug.Log(hit.point.z);
                                Debug.DrawLine(
                                    m_Camera.transform.position,
                                    hit.point,
                                    Color.red,
                                    20f,
                                    true
                                );
#endif
                            }
                        }
                    }
                }
            }

            if (ItemInHand != null) {
                ItemInHand.SetActive(true);
            }

            DefaultAgentHand();
            actionFinished(true, goodPositions);
        }

        public void PlaceFixedReceptacleAtLocation(int objectVariation, float x, float y, float z) {
            if (objectVariation < 0 || objectVariation > 4) {
                errorMessage = "Invalid receptacle variation.";
                actionFinished(false);
                return;
            }

            if (
                physicsSceneManager.ManipulatorReceptacles == null ||
                physicsSceneManager.ManipulatorReceptacles.Length == 0
            ) {
                errorMessage = "Scene does not have manipulator receptacles set.";
                actionFinished(false);
                return;
            }

            // float[] yoffsets = {-0.1049f, -0.1329f, -0.1009f, -0.0969f, -0.0971f};
            float[] yoffsets = { 0f, -0.0277601f, 0f, 0f, 0f };

            string receptId = "";
            for (int i = 0; i < 5; i++) {
                GameObject recept = physicsSceneManager.ManipulatorReceptacles[i];
                SimObjPhysics receptSop = recept.GetComponent<SimObjPhysics>();

                if (objectVariation == i) {
                    recept.SetActive(true);
                    recept.GetComponent<Rigidbody>().isKinematic = true;
                    recept.transform.position = new Vector3(x, y + yoffsets[i], z);
                    recept.transform.rotation = transform.rotation;
                    physicsSceneManager.AddToObjectsInScene(receptSop);
                    receptId = receptSop.ObjectID;
                } else if (recept.activeInHierarchy) {
                    physicsSceneManager.RemoveFromObjectsInScene(receptSop);
                    recept.SetActive(false);
                }
            }

            actionFinished(true, receptId);
        }

        public void PlaceBookWallAtLocation(int objectVariation, float x, float y, float z, Vector3 rotation) {
            if (
                physicsSceneManager.ManipulatorBooks == null ||
                physicsSceneManager.ManipulatorBooks.Length == 0
            ) {
                errorMessage = "Scene does not have manipulator books set.";
                actionFinished(false);
                return;
            }

            if (objectVariation < 0) {
                errorMessage = "objectVariation must be >= 0";
                actionFinished(false);
                return;
            }

            float yoffset = 0.19f;

            // uint which = (uint) Convert.ToUInt32(action.objectVariation);
            // List<bool> whichIncluded = new List<bool>();
            for (int i = 0; i < 5; i++) {
                if (((objectVariation >> i) % 2) == 1) {
                    physicsSceneManager.ManipulatorBooks[i].transform.gameObject.SetActive(true);
                } else {
                    physicsSceneManager.ManipulatorBooks[i].transform.gameObject.SetActive(false);
                }
                // whichIncluded.Add(
                //     ((action.objectVariation >> i) % 2) == 1
                // );
            }

            GameObject allBooksObject = physicsSceneManager.ManipulatorBooks[0].transform.parent.gameObject;

            allBooksObject.transform.position = new Vector3(x, y + yoffset, z);
            allBooksObject.transform.localRotation = Quaternion.Euler(
                rotation.x,
                rotation.y,
                rotation.z
            );

            actionFinished(true);
        }

        public void InitializeTableSetting(int objectVariation) {
            string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

            Vector3 newPosition = transform.position;
            Quaternion newRotation = transform.rotation;

            if (scene == "FloorPlan501_physics") {
                newPosition = new Vector3(0f, transform.position.y, 0.75f);
                newRotation = Quaternion.Euler(0f, 180f, 0f);
            } else if (scene == "FloorPlan502_physics") {
                newPosition = new Vector3(-0.5f, transform.position.y, 0.75f);
                newRotation = Quaternion.Euler(0f, 90f, 0f);
            } else if (scene == "FloorPlan503_physics") {
                newPosition = new Vector3(-0.5f, transform.position.y, -0.25f);
                newRotation = Quaternion.Euler(0f, 0f, 0f);
            } else if (scene == "FloorPlan504_physics") {
                newPosition = new Vector3(0f, transform.position.y, 0.5f);
                newRotation = Quaternion.Euler(0f, 180f, 0f);
            } else if (scene == "FloorPlan505_physics") {
                newPosition = new Vector3(0f, transform.position.y, 1.25f);
                newRotation = Quaternion.Euler(0f, 180f, 0f);
            } else {
                errorMessage = "Cannot initialize table in scene " + scene;
                actionFinished(false);
                return;
            }

            if (objectVariation < 0 || objectVariation > 4) {
                errorMessage = "Invalid table variation.";
                actionFinished(false);
                return;
            }

            transform.position = newPosition;
            transform.rotation = newRotation;

            if (m_Camera.fieldOfView != 90f) {
                m_Camera.fieldOfView = 90f;
            }
            m_Camera.transform.localEulerAngles = new Vector3(30f, 0.0f, 0.0f);

            string tableId = "";
            for (int i = 0; i < 5; i++) {
                GameObject table = physicsSceneManager.ManipulatorTables[i];
                SimObjPhysics tableSop = table.GetComponent<SimObjPhysics>();

                if (objectVariation == i) {
                    table.SetActive(true);
                    physicsSceneManager.AddToObjectsInScene(tableSop);
                    tableId = tableSop.ObjectID;
                } else if (table.activeInHierarchy) {
                    physicsSceneManager.RemoveFromObjectsInScene(tableSop);
                    table.SetActive(false);
                }

                GameObject recept = physicsSceneManager.ManipulatorReceptacles[i];
                SimObjPhysics receptSop = recept.GetComponent<SimObjPhysics>();
                if (recept.activeInHierarchy) {
                    physicsSceneManager.RemoveFromObjectsInScene(receptSop);
                    recept.SetActive(false);
                }
            }

            if (physicsSceneManager.ManipulatorBooks != null) {
                foreach (GameObject book in physicsSceneManager.ManipulatorBooks) {
                    book.SetActive(false);
                }
            }

            actionFinished(true, tableId);
        }

        public float GetXZRadiusOfObject(SimObjPhysics sop) {
            BoxCollider bc = sop.BoundingBox.GetComponent<BoxCollider>();
            return (new Vector3(bc.size.x, 0f, bc.size.z) * 0.5f).magnitude;
        }

        public void GetUnreachableSilhouetteForObject(string objectId, float z) {
            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(objectId)) {
                errorMessage = $"Cannot find object with id {objectId}.";
                actionFinishedEmit(false);
                return;
            }
            if (z <= 0.0f) {
                errorMessage = "Interactable distance (z) must be > 0";
                actionFinished(false);
                return;
            }
            SimObjPhysics targetObject = physicsSceneManager.ObjectIdToSimObjPhysics[objectId];

            Vector3 savedObjectPosition = targetObject.transform.position;
            Quaternion savedObjectRotation = targetObject.transform.rotation;
            Vector3 savedAgentPosition = transform.position;
            Quaternion savedAgentRotation = transform.rotation;

            targetObject.transform.rotation = Quaternion.identity;
            transform.rotation = Quaternion.identity;

            float objectRad = GetXZRadiusOfObject(targetObject);

            var sb = new System.Text.StringBuilder();
            int halfWidth = 1 + ((int)Math.Round((objectRad + z + m_CharacterController.radius) / gridSize));
            for (int i = 2 * halfWidth; i >= 0; i--) {
                float zOffset = ((i - halfWidth) * gridSize);

                for (int j = 0; j < 2 * halfWidth + 1; j++) {

                    float xOffset = ((j - halfWidth) * gridSize);
                    if (j != 0) {
                        sb.Append(" ");
                    }
                    transform.position = targetObject.transform.position + new Vector3(xOffset, 0f, zOffset);
                    if (isAgentCapsuleCollidingWith(targetObject.gameObject)) {
                        sb.Append("1");
                    } else if (distanceToObject(targetObject) <= z) {
                        sb.Append("2");
                    } else {
                        sb.Append("0");
                    }
                }
                sb.Append("\n");
            }
            string mat = sb.ToString();
#if UNITY_EDITOR
            Debug.Log(mat);
#endif

            targetObject.transform.position = savedObjectPosition;
            targetObject.transform.rotation = savedObjectRotation;
            transform.position = savedAgentPosition;
            transform.rotation = savedAgentRotation;

            actionFinished(true, mat);
        }

        public void RandomlyCreateLiftedFurniture(ServerAction action) {
            if (action.z < 0.25f) {
                errorMessage = "z must be at least 0.25";
                actionFinished(false);
                return;
            }
            if (action.y == 0.0f) {
                errorMessage = "y must be non-zero";
                actionFinished(false);
                return;
            }
            Vector3[] reachablePositions = getReachablePositions();

            List<Vector3> oldAgentPositions = new List<Vector3>();
            List<Quaternion> oldAgentRotations = new List<Quaternion>();
            foreach (BaseFPSAgentController agent in this.agentManager.agents) {
                oldAgentPositions.Add(agent.transform.position);
                agent.transform.position = new Vector3(50f, 50f, 50f);
                oldAgentRotations.Add(agent.transform.rotation);
            }
            SimObjPhysics objectCreated = null;
            try {
                objectCreated = randomlyCreateAndPlaceObjectOnFloor(
                    action.objectType, action.objectVariation, reachablePositions
                );
            } catch (Exception) { }
            if (objectCreated == null) {
                for (int i = 0; i < this.agentManager.agents.Count; i++) {
                    var agent = this.agentManager.agents[i];
                    agent.transform.position = oldAgentPositions[i];
                    agent.transform.rotation = oldAgentRotations[i];
                }
                errorMessage = "Failed to create object of type " + action.objectType + " . " + errorMessage;
                actionFinished(false);
                return;
            }
            objectCreated.GetComponent<Rigidbody>().isKinematic = true;
            bool objectFloating = moveObject(
                objectCreated,
                objectCreated.transform.position + new Vector3(0f, action.y, 0f)
            );

            float[] rotationsArr = { 0f, 90f, 180f, 270f };
            List<float> rotations = rotationsArr.ToList();

            bool placementSuccess = false;
            for (int i = 0; i < 10; i++) {
                if (objectFloating) {
                    List<Vector3> candidatePositionsList = new List<Vector3>();
                    foreach (Vector3 p in reachablePositions) {
                        transform.position = p;
                        if (isAgentCapsuleColliding(collidersToIgnoreDuringMovement)) {
                            continue;
                        }
                        if (distanceToObject(objectCreated) <= action.z) {
                            candidatePositionsList.Add(p);
                        }
                    }
                    transform.position = new Vector3(50f, 50f, 50f);

                    if (candidatePositionsList.Count >= agentManager.agents.Count) {
                        candidatePositionsList.Shuffle_();
                        foreach (Vector3[] candidatePositions in UtilityFunctions.Combinations(
                            candidatePositionsList.ToArray(), agentManager.agents.Count)) {
                            bool candidatesBad = false;
                            for (int j = 0; j < candidatePositions.Length - 1; j++) {
                                Vector3 p0 = candidatePositions[j];
                                for (int k = j + 1; k < candidatePositions.Length; k++) {
                                    Vector3 p1 = candidatePositions[k];
                                    if (Math.Abs(p1.x - p0.x) < 0.4999f && Math.Abs(p1.z - p0.z) < 0.4999f) {
                                        candidatesBad = true;
                                    }
                                    if (candidatesBad) {
                                        break;
                                    }
                                }
                                if (candidatesBad) {
                                    break;
                                }
                            }
                            if (candidatesBad) {
                                continue;
                            }
                            placementSuccess = true;

                            for (int j = 0; j < agentManager.agents.Count; j++) {
                                var agent = (PhysicsRemoteFPSAgentController)agentManager.agents[j];
                                agent.transform.position = candidatePositions[j];

                                foreach (float r in rotations.Shuffle_()) {
                                    agent.transform.rotation = Quaternion.Euler(new Vector3(0f, r, 0f));
                                    if (agent.objectIsCurrentlyVisible(objectCreated, 100f)) {
                                        break;
                                    }
                                }
                            }
                            break;
                        }
                        if (placementSuccess) {
                            break;
                        }
                    }
                }

                if (placementSuccess) {
                    break;
                } else {
                    foreach (BaseFPSAgentController agent in this.agentManager.agents) {
                        agent.transform.position = new Vector3(50f, 50f, 50f);
                    }
                    randomlyPlaceObjectOnFloor(objectCreated, reachablePositions);
                    objectFloating = moveObject(
                        objectCreated,
                        objectCreated.transform.position + new Vector3(0f, action.y, 0f)
                    );
                }
            }

            if (!placementSuccess) {
                for (int i = 0; i < this.agentManager.agents.Count; i++) {
                    var agent = this.agentManager.agents[i];
                    agent.transform.position = oldAgentPositions[i];
                    agent.transform.rotation = oldAgentRotations[i];
                }
                objectCreated.gameObject.SetActive(false);
                errorMessage = "Could not find a place to put the object after 10 iterations. " + errorMessage;
                actionFinished(false);
                return;
            }
            actionFinished(true, objectCreated.ObjectID);
        }

        protected bool moveObject(
            SimObjPhysics sop,
            Vector3 targetPosition,
            bool snapToGrid = false,
            HashSet<Transform> ignoreCollisionWithTransforms = null
        ) {
            Vector3 lastPosition = sop.transform.position;
            // Rigidbody ItemRB = sop.gameObject.GetComponent<Rigidbody>(); no longer needs rb reference

            if (snapToGrid) {
                float mult = 1.0f / gridSize;
                float gridX = Convert.ToSingle(Math.Round(targetPosition.x * mult) / mult);
                float gridZ = Convert.ToSingle(Math.Round(targetPosition.z * mult) / mult);
                targetPosition = new Vector3(gridX, targetPosition.y, gridZ);
            }

            Vector3 dir = targetPosition - sop.transform.position;
            RaycastHit[] sweepResults = UtilityFunctions.CastAllPrimitiveColliders(
                sop.gameObject,
                targetPosition - sop.transform.position,
                dir.magnitude,
                LayerMask.GetMask("SimObjVisible", "Procedural1", "Procedural2", "Procedural3", "Procedural0", "Agent"),
                QueryTriggerInteraction.Ignore
            );

            if (sweepResults.Length > 0) {
                foreach (RaycastHit hit in sweepResults) {
                    if (ignoreCollisionWithTransforms == null || !ignoreCollisionWithTransforms.Contains(hit.transform)) {
                        errorMessage = hit.transform.name + " is in the way of moving " + sop.ObjectID;
                        return false;
                    }
                }
            }
            sop.transform.position = targetPosition;
            return true;
        }

        protected bool moveLiftedObjectHelper(string objectId, Vector3 relativeDir, float maxAgentsDistance = -1.0f) {
            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(objectId)) {
                errorMessage = $"Cannot find object with id {objectId}.";
                return false;
            }
            SimObjPhysics objectToMove = physicsSceneManager.ObjectIdToSimObjPhysics[objectId];
            Vector3 oldPosition = objectToMove.transform.position;
            if (moveObject(objectToMove, objectToMove.transform.position + relativeDir, true)) {
                if (maxAgentsDistance > 0.0f) {
                    for (int i = 0; i < agentManager.agents.Count; i++) {
                        if (((PhysicsRemoteFPSAgentController)agentManager.agents[i]).distanceToObject(objectToMove) > maxAgentsDistance) {
                            objectToMove.transform.position = oldPosition;
                            errorMessage = "Would move object beyond max distance from agent " + i.ToString();
                            return false;
                        }
                    }
                }
                return true;
            } else {
                return false;
            }
        }

        public void CollidersObjectCollidingWith(string objectId) {
            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(objectId)) {
                errorMessage = $"Cannot find object with id {objectId}.";
                actionFinishedEmit(false);
                return;
            }
            List<string> collidingWithNames = new List<string>();
            GameObject go = physicsSceneManager.ObjectIdToSimObjPhysics[objectId].gameObject;
            foreach (Collider c in UtilityFunctions.collidersObjectCollidingWith(go)) {
                collidingWithNames.Add(c.name);
#if UNITY_EDITOR
                Debug.Log(c.name);
#endif
            }
            actionFinished(true, collidingWithNames);
        }
        protected bool moveObjectWithTeleport(SimObjPhysics sop, Vector3 targetPosition, bool snapToGrid = false) {
            Vector3 lastPosition = sop.transform.position;

            if (snapToGrid) {
                float mult = 1.0f / gridSize;
                float gridX = Convert.ToSingle(Math.Round(targetPosition.x * mult) / mult);
                float gridZ = Convert.ToSingle(Math.Round(targetPosition.z * mult) / mult);
                targetPosition = new Vector3(gridX, targetPosition.y, gridZ);
            }

            Vector3 oldPosition = sop.transform.position;
            sop.transform.position = targetPosition;

            if (UtilityFunctions.isObjectColliding(sop.gameObject)) {
                sop.transform.position = oldPosition;
                errorMessage = sop.ObjectID + " is colliding after teleport.";
                return false;
            }

            foreach (BaseFPSAgentController agent in agentManager.agents) {
                // This check is stupid but seems necessary to appease the unity gods
                // as unity doesn't realize the object collides with the agents in
                // the above checks in some cases.
                if (((PhysicsRemoteFPSAgentController)agent).isAgentCapsuleCollidingWith(sop.gameObject)) {
                    sop.transform.position = oldPosition;
                    errorMessage = sop.ObjectID + " is colliding with an agent after movement.";
                    return false;
                }
            }

            return true;
        }

        public void MoveLiftedObjectAhead(ServerAction action) {
            float mag = action.moveMagnitude > 0 ? action.moveMagnitude : gridSize;
            actionFinished(
                moveLiftedObjectHelper(
                    action.objectId,
                    mag * transform.forward,
                    action.maxAgentsDistance
                )
            );
        }

        public void MoveLiftedObjectRight(ServerAction action) {
            float mag = action.moveMagnitude > 0 ? action.moveMagnitude : gridSize;
            actionFinished(
                moveLiftedObjectHelper(
                    action.objectId,
                    mag * transform.right,
                    action.maxAgentsDistance
                )
            );
        }

        public void MoveLiftedObjectLeft(ServerAction action) {
            float mag = action.moveMagnitude > 0 ? action.moveMagnitude : gridSize;
            actionFinished(
                moveLiftedObjectHelper(
                    action.objectId,
                    -mag * transform.right,
                    action.maxAgentsDistance
                )
            );
        }

        public void MoveLiftedObjectBack(ServerAction action) {
            float mag = action.moveMagnitude > 0 ? action.moveMagnitude : gridSize;
            actionFinished(
                moveLiftedObjectHelper(
                    action.objectId,
                    -mag * transform.forward,
                    action.maxAgentsDistance
                )
            );
        }

        public void RotateLiftedObjectRight(ServerAction action) {
            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(action.objectId)) {
                errorMessage = "Cannot find object with id " + action.objectId;
                Debug.Log(errorMessage);
                actionFinished(false);
                return;
            } else {
                SimObjPhysics sop = physicsSceneManager.ObjectIdToSimObjPhysics[action.objectId];
                if (ItemInHand != null && sop == ItemInHand.GetComponent<SimObjPhysics>()) {
                    errorMessage = "Cannot rotate lifted object in hand.";
                    Debug.Log(errorMessage);
                    actionFinished(false);
                    return;
                }
                Quaternion oldRotation = sop.transform.rotation;
                sop.transform.rotation = Quaternion.Euler(new Vector3(0.0f, (float)Math.Round((sop.transform.eulerAngles.y + 90f) % 360), 0.0f)); ;
                if (!action.forceAction) {
                    if (action.maxAgentsDistance > 0.0f) {
                        for (int i = 0; i < agentManager.agents.Count; i++) {
                            if (((PhysicsRemoteFPSAgentController)agentManager.agents[i]).distanceToObject(sop) > action.maxAgentsDistance) {
                                sop.transform.rotation = oldRotation;
                                errorMessage = "Would move object beyond max distance from agent " + i.ToString();
                                actionFinished(false);
                                return;
                            }
                        }
                    }
                    if (UtilityFunctions.isObjectColliding(sop.gameObject, null, 0.0f)) {
                        sop.transform.rotation = oldRotation;
                        errorMessage = sop.ObjectID + " is colliding after teleport.";
                        actionFinished(false);
                        return;
                    }
                    foreach (BaseFPSAgentController agent in agentManager.agents) {
                        // This check is silly but seems necessary to appease unity
                        // as unity doesn't realize the object collides with the agents in
                        // the above checks in some cases.
                        if (((PhysicsRemoteFPSAgentController)agent).isAgentCapsuleCollidingWith(sop.gameObject)) {
                            sop.transform.rotation = oldRotation;
                            errorMessage = sop.ObjectID + " is colliding with an agent after rotation.";
                            actionFinished(false);
                            return;
                        }
                    }
                }
                actionFinished(true);
            }
        }

        public bool moveAgentsWithObject(SimObjPhysics objectToMove, Vector3 d, bool snapToGrid = true) {
            List<Vector3> startAgentPositions = new List<Vector3>();
            var agentMovePQ = new SimplePriorityQueue<BaseFPSAgentController>();
            foreach (BaseFPSAgentController agent in agentManager.agents) {
                var p = agent.transform.position;
                startAgentPositions.Add(p);
                agentMovePQ.Enqueue(agent, -(d.x * p.x + d.z * p.z));
            }
            Vector3 startObjectPosition = objectToMove.transform.position;
            float objectPriority = d.x * startObjectPosition.x + d.z * startObjectPosition.z;
            bool objectMoved = false;

            HashSet<Collider> agentsAndObjColliders = new HashSet<Collider>();
            foreach (BaseFPSAgentController agent in agentManager.agents) {
                foreach (Collider c in agent.GetComponentsInChildren<Collider>()) {
                    agentsAndObjColliders.Add(c);
                }
            }
            foreach (Collider c in objectToMove.GetComponentsInChildren<Collider>()) {
                agentsAndObjColliders.Add(c);
            }

            bool success = true;
            Physics.autoSimulation = false;
            while (agentMovePQ.Count > 0 || !objectMoved) {
                if (agentMovePQ.Count == 0) {
                    success = moveObjectWithTeleport(objectToMove, objectToMove.transform.position + d, snapToGrid);
                    PhysicsSceneManager.PhysicsSimulateTHOR(0.04f);
                    break;
                } else {
                    PhysicsRemoteFPSAgentController nextAgent = (PhysicsRemoteFPSAgentController)agentMovePQ.First;
                    float agentPriority = -agentMovePQ.GetPriority(nextAgent);

                    if (!objectMoved && agentPriority < objectPriority) {
                        // Debug.Log("Object");
                        success = moveObjectWithTeleport(objectToMove, objectToMove.transform.position + d, snapToGrid);
                        PhysicsSceneManager.PhysicsSimulateTHOR(0.04f);
                        objectMoved = true;
                    } else {
                        // Debug.Log(nextAgent);
                        agentMovePQ.Dequeue();
                        success = nextAgent.moveInDirection(d, "", -1, false, false, agentsAndObjColliders);
                        PhysicsSceneManager.PhysicsSimulateTHOR(0.04f);
                    }
                }
                if (!success) {
                    break;
                }
            }
            Physics.autoSimulation = true;
            if (!success) {
                for (int i = 0; i < agentManager.agents.Count; i++) {
                    agentManager.agents[i].transform.position = startAgentPositions[i];
                }
                objectToMove.transform.position = startObjectPosition;
            }
            return success;
        }

        public void MoveAgentsAheadWithObject(ServerAction action) {
            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(action.objectId)) {
                errorMessage = "Cannot find object with id " + action.objectId;
                actionFinished(false);
                return;
            }
            SimObjPhysics objectToMove = physicsSceneManager.ObjectIdToSimObjPhysics[action.objectId];
            action.moveMagnitude = action.moveMagnitude > 0 ? action.moveMagnitude : gridSize;
            actionFinished(moveAgentsWithObject(objectToMove, transform.forward * action.moveMagnitude));
        }

        public void MoveAgentsLeftWithObject(ServerAction action) {
            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(action.objectId)) {
                errorMessage = "Cannot find object with id " + action.objectId;
                actionFinished(false);
                return;
            }
            SimObjPhysics objectToMove = physicsSceneManager.ObjectIdToSimObjPhysics[action.objectId];
            action.moveMagnitude = action.moveMagnitude > 0 ? action.moveMagnitude : gridSize;
            actionFinished(moveAgentsWithObject(objectToMove, -transform.right * action.moveMagnitude));
        }

        public void MoveAgentsRightWithObject(ServerAction action) {
            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(action.objectId)) {
                errorMessage = "Cannot find object with id " + action.objectId;
                actionFinished(false);
                return;
            }
            SimObjPhysics objectToMove = physicsSceneManager.ObjectIdToSimObjPhysics[action.objectId];
            action.moveMagnitude = action.moveMagnitude > 0 ? action.moveMagnitude : gridSize;
            actionFinished(moveAgentsWithObject(objectToMove, transform.right * action.moveMagnitude));
        }

        public void MoveAgentsBackWithObject(ServerAction action) {
            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(action.objectId)) {
                errorMessage = "Cannot find object with id " + action.objectId;
                actionFinished(false);
                return;
            }
            SimObjPhysics objectToMove = physicsSceneManager.ObjectIdToSimObjPhysics[action.objectId];
            action.moveMagnitude = action.moveMagnitude > 0 ? action.moveMagnitude : gridSize;
            actionFinished(moveAgentsWithObject(objectToMove, -transform.forward * action.moveMagnitude));
        }

        public void TeleportObjectToFloor(ServerAction action) {
            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(action.objectId)) {
                errorMessage = "Cannot find object with id " + action.objectId;
                actionFinished(false);
                return;
            } else {
                SimObjPhysics sop = physicsSceneManager.ObjectIdToSimObjPhysics[action.objectId];
                if (ItemInHand != null && sop == ItemInHand.GetComponent<SimObjPhysics>()) {
                    errorMessage = "Cannot teleport object in hand.";
                    actionFinished(false);
                    return;
                }

                Bounds objBounds = UtilityFunctions.CreateEmptyBounds();
                foreach (Renderer r in sop.GetComponentsInChildren<Renderer>()) {
                    if (r.enabled) {
                        objBounds.Encapsulate(r.bounds);
                    }
                }
                if (objBounds.min.x == float.PositiveInfinity) {
                    errorMessage = "Could not get bounds of " + action.objectId + ".";
                    actionFinished(false);
                    return;
                }
                float y = getFloorY(action.x, action.z);
                if (errorMessage != "") {
                    actionFinished(false);
                    return;
                }
                sop.transform.position = new Vector3(
                    action.x,
                    objBounds.extents.y + y + 0.1f,
                    action.z
                );
                sop.transform.rotation = Quaternion.Euler(action.rotation);
                actionFinished(true);
            }
        }

        ///////////////////////////////////////////
        ////////////// TELEPORT FULL //////////////
        ///////////////////////////////////////////

        // [ObsoleteAttribute(message: "This action is deprecated. Call TeleportFull(position, ...) instead.", error: false)] 
        // public void TeleportFull(
        //     float x, float y, float z,
        //     float rotation,
        //     float horizon,
        //     bool standing,
        //     bool forceAction = false
        // ) {
        //     TeleportFull(
        //         position: new Vector3(x, y, z),
        //         rotation: new Vector3(0, rotation, 0),
        //         horizon: horizon,
        //         standing: standing,
        //         forceAction: forceAction
        //     );
        // }

        [ObsoleteAttribute(message: "This action is deprecated. Call TeleportFull(position, ...) instead.", error: false)]
        public void TeleportFull(
            float x, float y, float z,
            Vector3 rotation,
            float horizon,
            bool standing,
            bool forceAction = false
        ) {
            TeleportFull(
                position: new Vector3(x, y, z),
                rotation: rotation,
                horizon: horizon,
                standing: standing,
                forceAction: forceAction
            );
        }

        // keep undocumented until float: rotation is added to Stochastic
        // public void TeleportFull(
        //     Vector3 position,
        //     float rotation,
        //     float horizon,
        //     bool standing,
        //     bool forceAction = false
        // ) {
        //     TeleportFull(
        //         position: position,
        //         rotation: new Vector3(0, rotation, 0),
        //         horizon: horizon,
        //         standing: standing,
        //         forceAction: forceAction
        //     );
        // }

        // has to consider both the arm and standing
        public void TeleportFull(
            Vector3 position,
            Vector3 rotation,
            float horizon,
            bool standing,
            bool forceAction = false
        ) {
            // cache old values in case there's a failure
            bool wasStanding = isStanding();
            Vector3 oldPosition = transform.position;
            Quaternion oldRotation = transform.rotation;
            Vector3 oldCameraLocalEulerAngle = m_Camera.transform.localEulerAngles;

            Vector3 oldLocalHandPosition = new Vector3();
            Quaternion oldLocalHandRotation = new Quaternion();
            if (ItemInHand != null) {
                oldLocalHandPosition = ItemInHand.transform.localPosition;
                oldLocalHandRotation = ItemInHand.transform.localRotation;
            }

            try {
                // default high level hand when teleporting
                DefaultAgentHand();
                base.teleportFull(
                    position: position,
                    rotation: rotation,
                    horizon: horizon,
                    forceAction: forceAction
                );

                if (standing) {
                    stand();
                } else {
                    crouch();
                }

                // add arm value cases
                if (!forceAction) {
                    if (isHandObjectColliding(ignoreAgent: true)) {
                        throw new InvalidOperationException("Cannot teleport due to hand object collision.");
                    }
                    if (Arm != null && Arm.IsArmColliding()) {
                        throw new InvalidOperationException(
                            "Mid Level Arm is actively clipping with some geometry in the environment. TeleportFull fails in this position."
                        );
                    }
                    else if (SArm != null && SArm.IsArmColliding()) {
                        throw new InvalidOperationException(
                            "Stretch Arm is actively clipping with some geometry in the environment. TeleportFull fails in this position."
                        );
                    }
                    base.assertTeleportedNearGround(targetPosition: position);
                }
            } catch (InvalidOperationException e) {
                if (wasStanding) {
                    stand();
                } else {
                    crouch();
                }
                if (ItemInHand != null) {
                    ItemInHand.transform.localPosition = oldLocalHandPosition;
                    ItemInHand.transform.localRotation = oldLocalHandRotation;
                }

                transform.position = oldPosition;
                transform.rotation = oldRotation;
                m_Camera.transform.localEulerAngles = oldCameraLocalEulerAngle;

                throw new InvalidOperationException(e.Message);
            }
            actionFinished(success: true);
        }

        ///////////////////////////////////////////
        //////////////// TELEPORT /////////////////
        ///////////////////////////////////////////

        // [ObsoleteAttribute(message: "This action is deprecated. Call Teleport(position, ...) instead.", error: false)] 
        // public void Teleport(
        //     float x, float y, float z,
        //     float? rotation = null,
        //     float? horizon = null,
        //     bool? standing = null,
        //     bool forceAction = false
        // ) {
        //     Teleport(
        //         position: new Vector3(x, y, z),
        //         rotation: rotation,
        //         horizon: horizon,
        //         standing: standing,
        //         forceAction: forceAction
        //     );
        // }

        [ObsoleteAttribute(message: "This action is deprecated. Call Teleport(position, ...) instead.", error: false)]
        public void Teleport(
            float x, float y, float z,
            Vector3? rotation = null,
            float? horizon = null,
            bool? standing = null,
            bool forceAction = false
        ) {
            Teleport(
                position: new Vector3(x, y, z),
                rotation: rotation,
                horizon: horizon,
                standing: standing,
                forceAction: forceAction
            );
        }

        // keep undocumented until float: rotation is added to Stochastic
        // DO NOT add float: rotation to base.
        // public void Teleport(
        //     Vector3? position = null,
        //     float? rotation = null,
        //     float? horizon = null,
        //     bool? standing = null,
        //     bool forceAction = false
        // ) {
        //     Teleport(
        //         position: position,
        //         rotation: rotation == null ? m_Camera.transform.localEulerAngles : new Vector3(0, (float) rotation, 0),
        //         horizon: horizon,
        //         standing: standing,
        //         forceAction: forceAction
        //     );
        // }

        public void Teleport(
            Vector3? position = null,
            Vector3? rotation = null,
            float? horizon = null,
            bool? standing = null,
            bool forceAction = false
        ) {
            TeleportFull(
                position: position == null ? transform.position : (Vector3)position,
                rotation: rotation == null ? transform.eulerAngles : (Vector3)rotation,
                horizon: horizon == null ? m_Camera.transform.localEulerAngles.x : (float)horizon,
                standing: standing == null ? isStanding() : (bool)standing,
                forceAction: forceAction
            );
        }

        public void ToggleArmColliders(IK_Robot_Arm_Controller arm, bool value) {
            if (arm != null) {
                foreach (CapsuleCollider c in arm.ArmCapsuleColliders) {
                    c.isTrigger = value;
                }
                foreach (BoxCollider b in arm.ArmBoxColliders) {
                    b.isTrigger = value;
                }
            }
        }

        protected HashSet<Collider> allAgentColliders() {
            HashSet<Collider> colliders = null;
            colliders = new HashSet<Collider>();
            foreach (BaseFPSAgentController agent in agentManager.agents) {
                foreach (Collider c in agent.GetComponentsInChildren<Collider>()) {
                    colliders.Add(c);
                }
            }
            return colliders;
        }

        public virtual void MoveLeft(
            float? moveMagnitude = null,
            string objectId = "",
            float maxAgentsDistance = -1f,
            bool forceAction = false,
            bool manualInteract = false,
            bool allowAgentsToIntersect = false,
            float speed = 1,              // TODO: Unused, remove when refactoring the controllers
            float? fixedDeltaTime = null, // TODO: Unused, remove when refactoring the controllers
            bool returnToStart = true,    // TODO: Unused, remove when refactoring the controllers
            bool disableRendering = true  // TODO: Unused, remove when refactoring the controllers
        ) {
            if (!moveMagnitude.HasValue) {
                moveMagnitude = gridSize;
            } else if (moveMagnitude <= 0f) {
                throw new InvalidOperationException("moveMagnitude must be > 0");
            }

            actionFinished(moveInDirection(
                direction: -transform.right * moveMagnitude.Value,
                objectId: objectId,
                maxDistanceToObject: maxAgentsDistance,
                forceAction: forceAction,
                manualInteract: manualInteract,
                ignoreColliders: allowAgentsToIntersect ? allAgentColliders() : null
            ));
        }

        public virtual void MoveRight(
            float? moveMagnitude = null,
            string objectId = "",
            float maxAgentsDistance = -1f,
            bool forceAction = false,
            bool manualInteract = false,
            bool allowAgentsToIntersect = false,
            float speed = 1,              // TODO: Unused, remove when refactoring the controllers
            float? fixedDeltaTime = null, // TODO: Unused, remove when refactoring the controllers
            bool returnToStart = true,    // TODO: Unused, remove when refactoring the controllers
            bool disableRendering = true  // TODO: Unused, remove when refactoring the controllers
        ) {
            if (!moveMagnitude.HasValue) {
                moveMagnitude = gridSize;
            } else if (moveMagnitude <= 0f) {
                throw new InvalidOperationException("moveMagnitude must be > 0");
            }

            actionFinished(moveInDirection(
                direction: transform.right * moveMagnitude.Value,
                objectId: objectId,
                maxDistanceToObject: maxAgentsDistance,
                forceAction: forceAction,
                manualInteract: manualInteract,
                ignoreColliders: allowAgentsToIntersect ? allAgentColliders() : null
            ));
        }

        public virtual void MoveAhead(
            float? moveMagnitude = null,
            string objectId = "",
            float maxAgentsDistance = -1f,
            bool forceAction = false,
            bool manualInteract = false,
            bool allowAgentsToIntersect = false,
            float speed = 1,              // TODO: Unused, remove when refactoring the controllers
            float? fixedDeltaTime = null, // TODO: Unused, remove when refactoring the controllers
            bool returnToStart = true,    // TODO: Unused, remove when refactoring the controllers
            bool disableRendering = true  // TODO: Unused, remove when refactoring the controllers
        ) {
            if (!moveMagnitude.HasValue) {
                moveMagnitude = gridSize;
            } else if (moveMagnitude <= 0f) {
                throw new InvalidOperationException("moveMagnitude must be > 0");
            }

            actionFinished(moveInDirection(
                direction: transform.forward * moveMagnitude.Value,
                objectId: objectId,
                maxDistanceToObject: maxAgentsDistance,
                forceAction: forceAction,
                manualInteract: manualInteract,
                ignoreColliders: allowAgentsToIntersect ? allAgentColliders() : null
            ));
        }

        public virtual void MoveBack(
            float? moveMagnitude = null,
            string objectId = "",
            float maxAgentsDistance = -1f,
            bool forceAction = false,
            bool manualInteract = false,
            bool allowAgentsToIntersect = false,
            float speed = 1,              // TODO: Unused, remove when refactoring the controllers
            float? fixedDeltaTime = null, // TODO: Unused, remove when refactoring the controllers
            bool returnToStart = true,    // TODO: Unused, remove when refactoring the controllers
            bool disableRendering = true  // TODO: Unused, remove when refactoring the controllers
        ) {
            if (!moveMagnitude.HasValue) {
                moveMagnitude = gridSize;
            } else if (moveMagnitude <= 0f) {
                throw new InvalidOperationException("moveMagnitude must be > 0");
            }

            actionFinished(moveInDirection(
                direction: -transform.forward * moveMagnitude.Value,
                objectId: objectId,
                maxDistanceToObject: maxAgentsDistance,
                forceAction: forceAction,
                manualInteract: manualInteract,
                ignoreColliders: allowAgentsToIntersect ? allAgentColliders() : null
            ));
        }

        // a no op action used to return metadata via actionFinished call, but not actually doing anything to interact with the scene or manipulate the Agent
        public void NoOp() {
            actionFinished(true);
        }

        public void PushObject(ServerAction action) {
            if (ItemInHand != null && action.objectId == ItemInHand.GetComponent<SimObjPhysics>().objectID) {
                errorMessage = "Please use Throw for an item in the Agent's Hand";
                Debug.Log(errorMessage);
                actionFinished(false);
                return;
            }

            action.z = 1;

            // if (action.moveMagnitude == 0f) {
            //     action.moveMagnitude = 200f;
            // }

            ApplyForceObject(action);
        }

        public void PullObject(ServerAction action) {
            if (ItemInHand != null && action.objectId == ItemInHand.GetComponent<SimObjPhysics>().objectID) {
                errorMessage = "Please use Throw for an item in the Agent's Hand";
                Debug.Log(errorMessage);
                actionFinished(false);
                return;
            }

            action.z = -1;

            // if (action.moveMagnitude == 0f) {
            //     action.moveMagnitude = 200f;
            // }

            ApplyForceObject(action);
        }

        // pass in a magnitude and an angle offset to push an object relative to agent forward
        public void DirectionalPush(ServerAction action) {
            if (ItemInHand != null && action.objectId == ItemInHand.GetComponent<SimObjPhysics>().objectID) {
                errorMessage = "Please use Throw for an item in the Agent's Hand";
                Debug.Log(errorMessage);
                actionFinished(false);
                return;
            }

            // The direction vector to push the target object defined by action.pushAngle
            // degrees clockwise from the agent's forward.
            action.pushAngle %= 360;

            // converts negative rotations to be positive
            if (action.pushAngle < 360) {
                action.pushAngle += 360;
            }

            if (action.forceAction) {
                action.forceVisible = true;
            }

            SimObjPhysics target = null;
            if (action.objectId == null) {
                target = getInteractableSimObjectFromXY(x: action.x, y: action.y, forceAction: action.forceAction);
            } else {
                target = getInteractableSimObjectFromId(objectId: action.objectId, forceAction: action.forceAction);
            }

            // SimObjPhysics[] simObjPhysicsArray = VisibleSimObjs(action);

            // foreach (SimObjPhysics sop in simObjPhysicsArray) {
            //     if (action.objectId == sop.ObjectID) {
            //         target = sop;
            //     }
            // }

            // print(target.name);

            if (
                target.PrimaryProperty != SimObjPrimaryProperty.CanPickup
                && target.PrimaryProperty != SimObjPrimaryProperty.Moveable
            ) {
                throw new InvalidOperationException(
                    "Target Primary Property type incompatible with push/pull"
                );
            }

            if (!action.forceAction && !IsInteractable(target)) {
                errorMessage = "Target is not interactable and is probably occluded by something!";
                actionFinished(false);
                return;
            }

            // find the Direction to push the object basec on action.PushAngle
            Vector3 agentForward = transform.forward;
            float pushAngleInRadians = action.pushAngle * Mathf.PI / -180; // using -180 so positive PushAngle values go clockwise

            Vector3 direction = new Vector3((agentForward.x * Mathf.Cos(pushAngleInRadians) - agentForward.z * Mathf.Sin(pushAngleInRadians)), 0,
            agentForward.x * Mathf.Sin(pushAngleInRadians) + agentForward.z * Mathf.Cos(pushAngleInRadians));

            ServerAction pushAction = new ServerAction();
            pushAction.x = direction.x;
            pushAction.y = direction.y;
            pushAction.z = direction.z;

            pushAction.moveMagnitude = action.moveMagnitude;

            target.GetComponent<Rigidbody>().isKinematic = false;
            sopApplyForce(pushAction, target);

            // target.GetComponent<SimObjPhysics>().ApplyForce(pushAction);
            // actionFinished(true);
        }

        public void ApplyForceObject(ServerAction action) {
            if (action.forceAction) {
                action.forceVisible = true;
            }

            SimObjPhysics target = null;
            if (action.objectId == null) {
                target = getInteractableSimObjectFromXY(x: action.x, y: action.y, forceAction: action.forceAction);
            } else {
                target = getInteractableSimObjectFromId(objectId: action.objectId, forceAction: action.forceAction);
            }

            bool canbepushed = false;

            if (target.PrimaryProperty == SimObjPrimaryProperty.CanPickup ||
                target.PrimaryProperty == SimObjPrimaryProperty.Moveable) {
                canbepushed = true;
            }

            if (!canbepushed) {
                errorMessage = "Target Sim Object cannot be moved. It's primary property must be Pickupable or Moveable";
                actionFinished(false);
                return;
            }

            if (!action.forceAction && !IsInteractable(target)) {
                errorMessage = "Target:" + target.objectID + "is not interactable and is probably occluded by something!";
                actionFinished(false);
                return;
            }

            target.GetComponent<Rigidbody>().isKinematic = false;

            ServerAction apply = new ServerAction();
            apply.moveMagnitude = action.moveMagnitude;

            Vector3 dir = Vector3.zero;

            if (action.z == 1) {
                dir = gameObject.transform.forward;
            }

            if (action.z == -1) {
                dir = -gameObject.transform.forward;
            }
            // Vector3 dir = gameObject.transform.forward;
            // print(dir);
            apply.x = dir.x;
            apply.y = dir.y;
            apply.z = dir.z;

            sopApplyForce(apply, target);
            // target.GetComponent<SimObjPhysics>().ApplyForce(apply);
            // actionFinished(true);
        }
        public void PhysicsSyncTransforms() {
            Physics.SyncTransforms();
            actionFinished(true);
        }

        // pause physics autosimulation! Automatic physics simulation can be resumed using the UnpausePhysicsAutoSim() action.
        // additionally, auto simulation will automatically resume from the LateUpdate() check on AgentManager.cs - if the scene has come to rest, physics autosimulation will resume
        public void PausePhysicsAutoSim() {
            physicsSceneManager.PausePhysicsAutoSim();
            actionFinished(true);
        }

        // if physics AutoSimulation is paused, manually advance the physics timestep by action.timeStep's value. Only use values for timeStep no less than zero and no greater than 0.05
        public void AdvancePhysicsStep(
            float timeStep = 0.02f,
            float? simSeconds = null,
            bool allowAutoSimulation = false
        ) {
            if ((!allowAutoSimulation) && Physics.autoSimulation) {
                errorMessage = (
                    "AdvancePhysicsStep can only be called if Physics AutoSimulation is currently " +
                    "paused or if you have passed allowAutoSimulation=true! Either use the" +
                    " PausePhysicsAutoSim() action first, or if you already used it, Physics" +
                    " AutoSimulation has been turned back on already."
                );
                actionFinished(false);
                return;
            }

            if (timeStep <= 0.0f || timeStep > 0.05f) {
                errorMessage = "Please use a timeStep between 0.0f and 0.05f. Larger timeSteps produce inconsistent simulation results.";
                actionFinished(false);
                return;
            }

            if (!simSeconds.HasValue) {
                simSeconds = timeStep;
            }
            if (simSeconds.Value < 0.0f) {
                errorMessage = $"simSeconds must be non-negative (simSeconds=={simSeconds}).";
                actionFinished(false);
                return;
            }

            physicsSceneManager.AdvancePhysicsStep(timeStep, simSeconds, allowAutoSimulation);
            actionFinished(true);
        }

        // Use this to immediately unpause physics autosimulation and allow physics to resolve automatically like normal
        public void UnpausePhysicsAutoSim() {
            physicsSceneManager.UnpausePhysicsAutoSim();
            actionFinished(true);
        }

        protected void sopApplyForce(ServerAction action, SimObjPhysics sop, float length) {
            // print("running sopApplyForce");
            // apply force, return action finished immediately
            if (physicsSceneManager.physicsSimulationPaused) {
                // print("autosimulation off");
                sop.ApplyForce(action);
                if (length >= 0.00001f) {
                    WhatDidITouch feedback = new WhatDidITouch() { didHandTouchSomething = true, objectId = sop.objectID, armsLength = length };
#if UNITY_EDITOR
                    print("didHandTouchSomething: " + feedback.didHandTouchSomething);
                    print("object id: " + feedback.objectId);
                    print("armslength: " + feedback.armsLength);
#endif
                    actionFinished(true, feedback);
                }

                // why is this here?
                else {
                    actionFinished(true);
                }
            }

            // if physics is automatically being simulated, use coroutine rather than returning actionFinished immediately
            else {
                // print("autosimulation true");
                sop.ApplyForce(action);
                StartCoroutine(checkIfObjectHasStoppedMoving(sop, length));
            }
        }

        // wrapping the SimObjPhysics.ApplyForce function since lots of things use it....
        protected void sopApplyForce(ServerAction action, SimObjPhysics sop) {
            sopApplyForce(action, sop, 0.0f);
        }

        // used to check if an specified sim object has come to rest
        // set useTimeout bool to use a faster time out
        private IEnumerator checkIfObjectHasStoppedMoving(
            SimObjPhysics sop,
            float length,
            bool useTimeout = false) {
            // yield for the physics update to make sure this yield is consistent regardless of framerate
            yield return new WaitForFixedUpdate();

            float startTime = Time.time;
            float waitTime = TimeToWaitForObjectsToComeToRest;

            if (useTimeout) {
                waitTime = 1.0f;
            }

            if (sop != null) {
                Rigidbody rb = sop.GetComponentInChildren<Rigidbody>();
                bool stoppedMoving = false;

                while (Time.time - startTime < waitTime) {
                    if (sop == null) {
                        break;
                    }

                    float currentVelocity = Math.Abs(rb.angularVelocity.sqrMagnitude + rb.velocity.sqrMagnitude);
                    float accel = (currentVelocity - sop.lastVelocity) / Time.fixedDeltaTime;

                    // ok the accel is basically zero, so it has stopped moving
                    if (Mathf.Abs(accel) <= 0.001f) {
                        // force the rb to stop moving just to be safe
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                        rb.Sleep();
                        stoppedMoving = true;
                        break;
                    } else {
                        yield return new WaitForFixedUpdate();
                    }
                }

                // so we never stopped moving and we are using the timeout
                if (!stoppedMoving && useTimeout) {
                    errorMessage = "object couldn't come to rest";
                    // print(errorMessage);
                    actionFinished(false);
                    yield break;
                }

                // we are past the wait time threshold, so force object to stop moving before
                // rb.velocity = Vector3.zero;
                // rb.angularVelocity = Vector3.zero;
                // rb.Sleep();

                // return to metadatawrapper.actionReturn if an object was touched during this interaction
                if (length != 0.0f) {
                    WhatDidITouch feedback = new WhatDidITouch() { didHandTouchSomething = true, objectId = sop.objectID, armsLength = length };

#if UNITY_EDITOR
                    print("yield timed out");
                    print("didHandTouchSomething: " + feedback.didHandTouchSomething);
                    print("object id: " + feedback.objectId);
                    print("armslength: " + feedback.armsLength);
#endif

                    // force objec to stop moving 
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                    rb.Sleep();

                    actionFinished(true, feedback);
                }

                // if passed in length is 0, don't return feedback cause not all actions need that
                else {
                    DefaultAgentHand();
                    actionFinished(true, sop.transform.position);
                }
            } else {
                errorMessage = "null reference sim obj in checkIfObjectHasStoppedMoving call";
                actionFinished(false);
            }

        }

        // Sweeptest to see if the object Agent is holding will prohibit movement
        public bool CheckIfItemBlocksAgentStandOrCrouch() {
            bool result = false;

            // if there is nothing in our hand, we are good, return!
            if (ItemInHand == null) {
                result = true;
                return result;
            }

            // otherwise we are holding an object and need to do a sweep using that object's rb
            else {
                Vector3 dir = new Vector3();

                if (isStanding()) {
                    dir = new Vector3(0.0f, -1f, 0.0f);
                } else {
                    dir = new Vector3(0.0f, 1f, 0.0f);
                }

                Rigidbody rb = ItemInHand.GetComponent<Rigidbody>();

                RaycastHit[] sweepResults = rb.SweepTestAll(dir, standingLocalCameraPosition.y, QueryTriggerInteraction.Ignore);
                if (sweepResults.Length > 0) {
                    foreach (RaycastHit res in sweepResults) {
                        // did the item in the hand touch the agent? if so, ignore it's fine
                        // also ignore Untagged because the Transparent_RB of transparent objects need to be ignored for movement
                        // the actual rigidbody of the SimObjPhysics parent object of the transparent_rb should block correctly by having the
                        // checkMoveAction() in the BaseFPSAgentController fail when the agent collides and gets shoved back
                        if (res.transform.tag == "Player" || res.transform.tag == "Untagged") {
                            result = true;
                            break;
                        } else {
                            errorMessage = res.transform.name + " is blocking the Agent from moving " + dir + " with " + ItemInHand.name;
                            result = false;
                            Debug.Log(errorMessage);
                            return result;
                        }

                    }
                }
                // if the array is empty, nothing was hit by the sweeptest so we are clear to move
                else {
                    result = true;
                }

                return result;
            }
        }

        ///// AGENT HAND STUFF////
        protected IEnumerator moveHandToTowardsXYZWithForce(float x, float y, float z, float maxDistance) {
            if (ItemInHand == null) {
                errorMessage = "Agent can only move hand if holding an item";
                actionFinished(false);
                yield break;
            }
            SimObjPhysics simObjInHand = ItemInHand.GetComponent<SimObjPhysics>();
            simObjInHand.ResetContactPointsDictionary();
            Rigidbody rb = ItemInHand.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = false;

            Vector3 targetPosition = new Vector3(x, y, z);

            Vector3 initialPosition = rb.transform.position;
            Quaternion initialRotation = rb.transform.rotation;
            Vector3 forceDirection = targetPosition - rb.transform.position;
            forceDirection.Normalize();

            Vector3 lastPosition = initialPosition;
            Quaternion lastRotation = initialRotation;
            bool hitMaxDistance = false;
            bool beyondVisibleDistance = false;
            bool leavingViewport = false;
            CollisionDetectionMode oldCollisionDetectionMode = rb.collisionDetectionMode;

            List<Vector3> positions = new List<Vector3>();
            List<Quaternion> rotations = new List<Quaternion>();
            positions.Add(initialPosition);
            rotations.Add(initialRotation);

            Physics.autoSimulation = false;
            List<Vector3> seenPositions = new List<Vector3>();
            List<Quaternion> seenRotations = new List<Quaternion>();
            for (int i = 0; i < 100; i++) {
                seenPositions.Add(rb.transform.position);
                seenRotations.Add(rb.transform.rotation);
                if (rb.velocity.magnitude < 1) {
                    rb.AddForce(forceDirection, ForceMode.Force);
                }
                rb.angularVelocity = rb.angularVelocity * 0.96f;

                PhysicsSceneManager.PhysicsSimulateTHOR(0.04f);
#if UNITY_EDITOR
                yield return null;
#endif

                if (i >= 5) {
                    bool repeatedPosition = false;
                    for (int j = seenPositions.Count - 4; j >= Math.Max(seenPositions.Count - 8, 0); j--) {
                        float distance = Vector3.Distance(rb.transform.position, seenPositions[i]);
                        float angle = Quaternion.Angle(rb.transform.rotation, seenRotations[i]);
                        if (distance <= 0.001f && angle <= 3f) {
                            repeatedPosition = true;
                            break;
                        }
                    }
                    if (repeatedPosition) {
                        break;
                    }
                }

                hitMaxDistance = beyondVisibleDistance = leavingViewport = false;

                Vector3 newPosition = rb.transform.position;
                Vector3 delta = newPosition - initialPosition;
                Vector3 forceDir = Vector3.Project(newPosition - initialPosition, forceDirection);
                Vector3 perpDir = delta - forceDir;
                float perpNorm = perpDir.magnitude;
                if (perpNorm > 0.1f * maxDistance) {
                    newPosition = initialPosition + forceDir + (0.1f * maxDistance) * perpDir / perpNorm;
                    rb.transform.position = newPosition;
                }

                Vector3 tmpForCamera = newPosition;
                tmpForCamera.y = m_Camera.transform.position.y;

                hitMaxDistance = Vector3.Distance(initialPosition, newPosition) > maxDistance;
                beyondVisibleDistance = Vector3.Distance(m_Camera.transform.position, tmpForCamera) > maxVisibleDistance;
                leavingViewport = !objectIsWithinViewport(simObjInHand);
                // leavingViewport = !objectIsCurrentlyVisible(simObjInHand, 1000f);

                if (hitMaxDistance) {
                    rb.velocity = new Vector3(0f, 0f, 0f);
                    rb.angularVelocity = 0.0f * rb.angularVelocity;
                    break;
                }

                if (beyondVisibleDistance || leavingViewport) {
                    break;
                } else {
                    positions.Add(rb.transform.position);
                    rotations.Add(rb.transform.rotation);
                    lastPosition = rb.transform.position;
                    lastRotation = rb.transform.rotation;
                }
            }

            Vector3 normalSum = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 aveCollisionsNormal = new Vector3(0.0f, 0.0f, 0.0f);
            int count = 0;
            foreach (KeyValuePair<Collider, ContactPoint[]> pair in simObjInHand.contactPointsDictionary) {
                foreach (ContactPoint cp in pair.Value) {
                    normalSum += cp.normal;
                    count += 1;
                }
            }

            if (count != 0) {
                aveCollisionsNormal = normalSum / count;
                aveCollisionsNormal.Normalize();
            }

            AgentHand.transform.position = lastPosition;
            rb.transform.localPosition = new Vector3(0f, 0f, 0f);
            rb.transform.rotation = lastRotation;
            rb.velocity = new Vector3(0f, 0f, 0f);
            rb.angularVelocity = new Vector3(0f, 0f, 0f);

            // SetUpRotationBoxChecks();
            IsHandDefault = false;

            PhysicsSceneManager.PhysicsSimulateTHOR(0.1f);
            bool handObjectIsColliding = isHandObjectColliding(true);
            if (count != 0) {
                for (int j = 0; handObjectIsColliding && j < 5; j++) {
                    AgentHand.transform.position = AgentHand.transform.position + 0.01f * aveCollisionsNormal;
                    PhysicsSceneManager.PhysicsSimulateTHOR(0.1f);
                    handObjectIsColliding = isHandObjectColliding(true);
                }
            }

            Physics.autoSimulation = true;

            // This has to be after the above as the contactPointsDictionary is only
            // updated while rb is not kinematic.
            rb.isKinematic = true;
            rb.collisionDetectionMode = oldCollisionDetectionMode;

            if (handObjectIsColliding) {
                AgentHand.transform.position = initialPosition;
                rb.transform.rotation = initialRotation;
                errorMessage = "Hand object was colliding with: ";
                foreach (KeyValuePair<Collider, ContactPoint[]> pair in simObjInHand.contactPointsDictionary) {
                    SimObjPhysics sop = ancestorSimObjPhysics(pair.Key.gameObject);
                    if (sop != null) {
                        errorMessage += "" + sop.ObjectID + ", ";
                    } else {
                        errorMessage += "" + pair.Key.gameObject.name + ", ";
                    }
                }
                errorMessage += " object(s) after movement.";
                actionFinished(false);
            } else if (Vector3.Distance(initialPosition, lastPosition) < 0.001f &&
                Quaternion.Angle(initialRotation, lastRotation) < 0.001f) {
                if (beyondVisibleDistance) {
                    errorMessage = "Hand already at max distance.";
                } else if (leavingViewport) {
                    errorMessage = "Hand at viewport constraints.";
                } else {
                    errorMessage = "Hand object did not move, perhaps its being blocked.";
                }
                actionFinished(false);
            } else {
                actionFinished(true);
            }
        }

        public void OpenWithHand(ServerAction action) {
            Vector3 direction = transform.forward * action.z +
                transform.right * action.x +
                transform.up * action.y;
            direction.Normalize();
            if (ItemInHand != null) {
                ItemInHand.SetActive(false);
            }
            RaycastHit hit;
            int layerMask = LayerMask.GetMask("SimObjVisible", "Procedural1", "Procedural2", "Procedural3", "Procedural0", "SimObjInvisible");
            bool raycastDidHit = Physics.Raycast(
                AgentHand.transform.position, direction, out hit, 10f, layerMask);
            if (ItemInHand != null) {
                ItemInHand.SetActive(true);
            }

            if (!raycastDidHit) {
                errorMessage = "No openable objects in direction.";
                actionFinished(false);
                return;
            }
            SimObjPhysics so = ancestorSimObjPhysics(hit.transform.gameObject);
            if (so != null) {
                OpenObject(objectId: so.ObjectID, forceAction: true);
            } else {
                errorMessage = hit.transform.gameObject.name + " is not interactable.";
                actionFinished(false);
            }
        }

        public void MoveHandForce(float x, float y, float z) {
            Vector3 direction = transform.forward * z +
                transform.right * x +
                transform.up * y;
            Vector3 target = AgentHand.transform.position +
                direction;
            if (ItemInHand == null) {
                Debug.Log("Agent can only move hand if holding an item");
                actionFinished(false);
            } else if (moveHandToXYZ(target.x, target.y, target.z)) {
                actionFinished(true);
            } else {
                errorMessage = "";
                StartCoroutine(
                    moveHandToTowardsXYZWithForce(target.x, target.y, target.z, direction.magnitude)
                );
            }
        }

        public void TouchThenApplyForce(ServerAction action) {
            float x = action.x;
            float y = 1.0f - action.y; // reverse the y so that the origin (0, 0) can be passed in as the top left of the screen

            // cast ray from screen coordinate into world space. If it hits an object
            Ray ray = m_Camera.ViewportPointToRay(new Vector3(x, y, 0.0f));
            RaycastHit hit;

            // if something was touched, actionFinished(true) always
            if (
                Physics.Raycast(
                    ray,
                    out hit,
                    action.handDistance,
                    LayerMask.GetMask("Default", "SimObjVisible", "Procedural1", "Procedural2", "Procedural3", "Procedural0", "Agent"),
                    QueryTriggerInteraction.Ignore
                )
            ) {
                if (hit.transform.GetComponent<SimObjPhysics>()) {
                    // wait! First check if the point hit is withing visibility bounds (camera viewport, max distance etc)
                    // this should basically only happen if the handDistance value is too big
                    try {
                        assertPosInView(targetPosition: hit.point, inMaxVisibleDistance: true, inViewport: true);
                    } catch (InvalidOperationException e) {
                        actionFinished(
                            success: false,
                            errorMessage: "Object successfully hit, but it is outside of the Agent's interaction range",
                            actionReturn: new WhatDidITouch() {
                                didHandTouchSomething = false,
                                objectId = "",
                                armsLength = action.handDistance
                            }
                        );
                        return;
                    }

                    // if the object is a sim object, apply force now!
                    SimObjPhysics target = hit.transform.GetComponent<SimObjPhysics>();
                    bool canbepushed = false;

                    if (target.PrimaryProperty == SimObjPrimaryProperty.CanPickup ||
                        target.PrimaryProperty == SimObjPrimaryProperty.Moveable) {
                        canbepushed = true;
                    }

                    if (!canbepushed) {
                        // the sim object hit was not moveable or pickupable
                        WhatDidITouch feedback = new WhatDidITouch() { didHandTouchSomething = true, objectId = target.objectID, armsLength = hit.distance };
#if UNITY_EDITOR
                        print("object touched was not moveable or pickupable");
                        print("didHandTouchSomething: " + feedback.didHandTouchSomething);
                        print("object id: " + feedback.objectId);
                        print("armslength: " + feedback.armsLength);
#endif
                        actionFinished(true, feedback);
                        return;
                    }

                    ServerAction apply = new ServerAction();
                    apply.moveMagnitude = action.moveMagnitude;

                    // translate action.direction from Agent's local space to world space - note: do not use camera local space, keep it on agent
                    Vector3 forceDir = this.transform.TransformDirection(action.direction);

                    apply.x = forceDir.x;
                    apply.y = forceDir.y;
                    apply.z = forceDir.z;

                    sopApplyForce(apply, target, hit.distance);
                }

                // raycast hit something but it wasn't a sim object
                else {
                    WhatDidITouch feedback = new WhatDidITouch() { didHandTouchSomething = true, objectId = "not a sim object, a structure was touched", armsLength = hit.distance };
#if UNITY_EDITOR
                    print("object touched was not a sim object at all");
                    print("didHandTouchSomething: " + feedback.didHandTouchSomething);
                    print("object id: " + feedback.objectId);
                    print("armslength: " + feedback.armsLength);
#endif
                    actionFinished(true, feedback);
                    return;
                }
            }

            // raycast didn't hit anything
            else {
                // get ray.origin, multiply handDistance with ray.direction, add to origin to get the final point
                // if the final point was out of range, return actionFinished false, otherwise return actionFinished true with feedback
                Vector3 testPosition = ((action.handDistance * ray.direction) + ray.origin);
                try {
                    assertPosInView(targetPosition: testPosition, inMaxVisibleDistance: true, inViewport: true);
                } catch (InvalidOperationException e) {
                    actionFinished(
                        success: false,
                        errorMessage: "Object successfully hit, but it is outside of the Agent's interaction range",
                        actionReturn: new WhatDidITouch() {
                            didHandTouchSomething = false,
                            objectId = "",
                            armsLength = action.handDistance
                        }
                    );
                    return;
                }

                // the nothing hit was not out of range, but still nothing was hit
                WhatDidITouch feedback = new WhatDidITouch() { didHandTouchSomething = false, objectId = "", armsLength = action.handDistance };
#if UNITY_EDITOR
                print("raycast did not hit anything, it only hit empty space");
                print("didHandTouchSomething: " + feedback.didHandTouchSomething);
                print("object id: " + feedback.objectId);
                print("armslength: " + feedback.armsLength);
#endif
                actionFinished(true, feedback);
            }

        }

        // for use with TouchThenApplyForce feedback return
        public struct WhatDidITouch {
            public bool didHandTouchSomething;// did the hand touch something or did it hit nothing?
            public string objectId;// id of object touched, if it is a sim object
            public float armsLength;// the amount the hand moved from it's starting position to hit the object touched
        }

        // checks if agent hand that is holding an object can move to a target location. Returns false if any obstructions
        public bool CheckIfAgentCanMoveHand(Vector3 targetPosition, bool mustBeVisible = false) {
            bool result = false;

            // first check if we have anything in our hand, if not then no reason to move hand
            if (ItemInHand == null) {
                errorMessage = "Agent can only move hand if currently holding an item";
                result = false;
                return result;
            }

            // now check if the target position is within bounds of the Agent's forward (z) view
            Vector3 tmp = m_Camera.transform.position;
            tmp.y = targetPosition.y;

            if (Vector3.Distance(tmp, targetPosition) > maxVisibleDistance) {
                errorMessage = "The target position is out of range- object cannot move outside of max visibility distance.";
                result = false;
                return result;
            }

            // Note: Viewport normalizes to (0,0) bottom left, (1, 0) top right of screen
            // now make sure the targetPosition is actually within the Camera Bounds 

            Vector3 lastPosition = AgentHand.transform.position;
            AgentHand.transform.position = targetPosition;
            // now make sure that the targetPosition is within the Agent's x/y view, restricted by camera
            if (!objectIsWithinViewport(ItemInHand.GetComponent<SimObjPhysics>())) {
                AgentHand.transform.position = lastPosition;
                errorMessage = "Target position is outside of the agent's viewport. The target position must be within the frustrum of the viewport.";
                result = false;
                return result;
            }

            // reset for mustBeVisible test so the direction from agent hand to target is correct
            AgentHand.transform.position = lastPosition;

            // by default this is ignored, but pass this as true to force hand manipulation
            // such that objects will always remain visible to the agent and not occluded
            if (mustBeVisible) {
                // quickly move object to proposed target position and see if target is still visible
                lastPosition = AgentHand.transform.position;
                AgentHand.transform.position = targetPosition;
                if (!objectIsCurrentlyVisible(ItemInHand.GetComponent<SimObjPhysics>(), 1000f)) {
                    errorMessage = "The target position is not in the Area of the Agent's Viewport!";
                    result = false;
                    AgentHand.transform.position = lastPosition;
                    return result;
                }
                AgentHand.transform.position = lastPosition;
            }


            // ok now actually check if the Agent Hand holding ItemInHand can move to the target position without
            // being obstructed by anything
            Rigidbody ItemRB = ItemInHand.GetComponent<Rigidbody>();

            RaycastHit[] sweepResults = ItemRB.SweepTestAll(targetPosition - AgentHand.transform.position,
                Vector3.Distance(targetPosition, AgentHand.transform.position),
                QueryTriggerInteraction.Ignore);

            // did we hit anything?
            if (sweepResults.Length > 0) {

                foreach (RaycastHit hit in sweepResults) {
                    // hit the player? it's cool, no problem
                    if (hit.transform.tag == "Player") {
                        result = true;
                        break;
                    }

                    // oh we hit something else? oh boy, that's blocking!
                    else {
                        errorMessage = hit.transform.name + " is in Object In Hand's Path! Can't Move Hand holding " + ItemInHand.name;
                        result = false;
                        return result;
                    }
                }

            }

            // didnt hit anything in sweep, we are good to go
            else {
                result = true;
            }

            return result;
        }

        // moves hand to the x, y, z coordinate, not constrained by any axis, if within range
        protected bool moveHandToXYZ(float x, float y, float z, bool mustBeVisible = false) {
            Vector3 targetPosition = new Vector3(x, y, z);
            if (CheckIfAgentCanMoveHand(targetPosition, mustBeVisible)) {
                // Debug.Log("Movement of Agent Hand holding " + ItemInHand.name + " succesful!");
                Vector3 oldPosition = AgentHand.transform.position;
                AgentHand.transform.position = targetPosition;
                IsHandDefault = false;
                return true;
            } else {
                // error messages are set up in CheckIfAgentCanMoveHand
                return false;
            }
        }

        // coroutine to yield n frames before returning
        protected IEnumerator waitForNFramesAndReturn(int n, bool actionSuccess) {
            for (int i = 0; i < n; i++) {
                yield return null;
            }
            actionFinished(actionSuccess);
        }

        // Moves hand relative the agent (but not relative the camera, i.e. up is up)
        // x, y, z coordinates should specify how far to move in that direction, so
        // x=.1, y=.1, z=0 will move the hand .1 in both the x and y coordinates.
        public void MoveHand(float x, float y, float z) {
            // get new direction relative to Agent forward facing direction (not the camera)
            Vector3 newPos = AgentHand.transform.position +
                transform.forward * z +
                transform.right * x +
                transform.up * y;
            StartCoroutine(waitForNFramesAndReturn(1, moveHandToXYZ(newPos.x, newPos.y, newPos.z)));
        }

        [ObsoleteAttribute(message: "This action is deprecated. Call MoveHeldObject(right, up, ahead) instead.", error: false)]
        public void MoveHandDelta(float x, float y, float z, bool forceVisible = false) {
            MoveHeldObject(right: x, up: y, ahead: z);
        }

        // moves hand constrained to x, y, z axes a given magnitude- x y z describe the magnitude in this case
        public void MoveHeldObject(float right = 0, float up = 0, float ahead = 0, bool forceVisible = false) {
            Vector3 newPos = AgentHand.transform.position;
            newPos += (
                m_Camera.transform.forward * ahead
                + m_Camera.transform.up * up
                + m_Camera.transform.right * right
            );
            actionFinished(moveHandToXYZ(newPos.x, newPos.y, newPos.z, forceVisible));
        }

        [ObsoleteAttribute(message: "This action is deprecated. Call MoveHeldObjectAhead() instead.", error: false)]
        public void MoveHandAhead(float moveMagnitude, bool forceVisible = false) {
            MoveHeldObjectAhead(moveMagnitude: moveMagnitude, forceVisible: forceVisible);
        }

        public void MoveHeldObjectAhead(float moveMagnitude, bool forceVisible = false) {
            Vector3 newPos = AgentHand.transform.position;
            newPos = newPos + (m_Camera.transform.forward * moveMagnitude);
            actionFinished(moveHandToXYZ(newPos.x, newPos.y, newPos.z, forceVisible));
        }

        [ObsoleteAttribute(message: "This action is deprecated. Call MoveHeldObjectLeft() instead.", error: false)]
        public void MoveHandLeft(float moveMagnitude, bool forceVisible = false) {
            MoveHeldObjectLeft(moveMagnitude: moveMagnitude, forceVisible: forceVisible);
        }

        public void MoveHeldObjectLeft(float moveMagnitude, bool forceVisible = false) {
            Vector3 newPos = AgentHand.transform.position;
            newPos = newPos + (-m_Camera.transform.right * moveMagnitude);
            actionFinished(moveHandToXYZ(newPos.x, newPos.y, newPos.z, forceVisible));
        }

        [ObsoleteAttribute(message: "This action is deprecated. Call MoveHeldObjectDown() instead.", error: false)]
        public void MoveHandDown(float moveMagnitude, bool forceVisible = false) {
            MoveHeldObjectDown(moveMagnitude: moveMagnitude, forceVisible: forceVisible);
        }

        public void MoveHeldObjectDown(float moveMagnitude, bool forceVisible = false) {
            Vector3 newPos = AgentHand.transform.position;
            newPos = newPos + (-m_Camera.transform.up * moveMagnitude);
            actionFinished(moveHandToXYZ(newPos.x, newPos.y, newPos.z, forceVisible));
        }

        [ObsoleteAttribute(message: "This action is deprecated. Call MoveHeldObjectUp() instead.", error: false)]
        public void MoveHandUp(float moveMagnitude, bool forceVisible = false) {
            MoveHeldObjectUp(moveMagnitude: moveMagnitude, forceVisible: forceVisible);
        }

        public void MoveHeldObjectUp(float moveMagnitude, bool forceVisible = false) {
            Vector3 newPos = AgentHand.transform.position;
            newPos = newPos + (m_Camera.transform.up * moveMagnitude);
            actionFinished(moveHandToXYZ(newPos.x, newPos.y, newPos.z, forceVisible));
        }

        [ObsoleteAttribute(message: "This action is deprecated. Call MoveHeldObjectRight() instead.", error: false)]
        public void MoveHandRight(float moveMagnitude, bool forceVisible = false) {
            MoveHeldObjectRight(moveMagnitude: moveMagnitude, forceVisible: forceVisible);
        }

        public void MoveHeldObjectRight(float moveMagnitude, bool forceVisible = false) {
            Vector3 newPos = AgentHand.transform.position;
            newPos = newPos + (m_Camera.transform.right * moveMagnitude);
            actionFinished(moveHandToXYZ(newPos.x, newPos.y, newPos.z, forceVisible));
        }

        [ObsoleteAttribute(message: "This action is deprecated. Call MoveHeldObjectBack() instead.", error: false)]
        public void MoveHandBack(float moveMagnitude, bool forceVisible = false) {
            MoveHeldObjectBack(moveMagnitude: moveMagnitude, forceVisible: forceVisible);
        }

        public void MoveHeldObjectBack(float moveMagnitude, bool forceVisible = false) {
            Vector3 newPos = AgentHand.transform.position;
            newPos = newPos + (-m_Camera.transform.forward * moveMagnitude);
            actionFinished(moveHandToXYZ(newPos.x, newPos.y, newPos.z, forceVisible));
        }

        // uh this kinda does what MoveHandDelta does but in more steps, splitting direction and magnitude into
        // two separate params in case someone wants it that way
        public void MoveHandMagnitude(float moveMagnitude, float x = 0.0f, float y = 0.0f, float z = 0.0f) {
            Vector3 newPos = AgentHand.transform.position;

            // get new direction relative to Agent's (camera's) forward facing 
            if (x > 0) {
                newPos = newPos + (m_Camera.transform.right * moveMagnitude);
            }

            if (x < 0) {
                newPos = newPos + (-m_Camera.transform.right * moveMagnitude);
            }

            if (y > 0) {
                newPos = newPos + (m_Camera.transform.up * moveMagnitude);
            }

            if (y < 0) {
                newPos = newPos + (-m_Camera.transform.up * moveMagnitude);
            }

            if (z > 0) {
                newPos = newPos + (m_Camera.transform.forward * moveMagnitude);
            }

            if (z < 0) {
                newPos = newPos + (-m_Camera.transform.forward * moveMagnitude);
            }

            actionFinished(moveHandToXYZ(newPos.x, newPos.y, newPos.z));
        }

        public bool IsInArray(Collider collider, GameObject[] arrayOfCol) {
            for (int i = 0; i < arrayOfCol.Length; i++) {
                if (collider == arrayOfCol[i].GetComponent<Collider>()) {
                    return true;
                }
            }
            return false;
        }

        public bool CheckIfAgentCanRotateHand() {
            bool result = false;

            // make sure there is a box collider
            if (ItemInHand.GetComponent<SimObjPhysics>().BoundingBox.GetComponent<BoxCollider>()) {
                Vector3 sizeOfBox = ItemInHand.GetComponent<SimObjPhysics>().BoundingBox.GetComponent<BoxCollider>().size;
                float overlapRadius = Math.Max(Math.Max(sizeOfBox.x, sizeOfBox.y), sizeOfBox.z);

                // all colliders hit by overlapsphere
                Collider[] hitColliders = Physics.OverlapSphere(
                    AgentHand.transform.position,
                    overlapRadius,
                    LayerMask.GetMask("SimObjVisible", "Procedural1", "Procedural2", "Procedural3", "Procedural0"),
                    QueryTriggerInteraction.Ignore
                );

                // did we even hit anything?
                if (hitColliders.Length > 0) {
                    foreach (Collider col in hitColliders) {
                        // is this a sim object?
                        if (col.GetComponentInParent<SimObjPhysics>()) {
                            // is it not the item we are holding? then it's blocking
                            if (col.GetComponentInParent<SimObjPhysics>().transform != ItemInHand.transform) {
                                errorMessage = "Rotating the object results in it colliding with " + col.gameObject.name;
                                return false;
                            }

                            // oh it is the item we are holding, it's fine
                            else {
                                result = true;
                            }
                        }

                        // ok it's not a sim obj and it's not the player, so it must be a structure or something else that would block
                        else if (col.tag != "Player") {
                            errorMessage = "Rotating the object results in it colliding with an agent.";
                            return false;
                        }
                    }
                }

                // nothing hit by sphere, so we are safe to rotate
                else {
                    result = true;
                }
            } else {
                Debug.Log("item in hand is missing a collider box for some reason! Oh nooo!");
            }

            return result;
        }

        [ObsoleteAttribute(message: "This action is deprecated. Call RotateHeldObject() instead.", error: false)]
        public void RotateHand(float x, float y, float z) {
            RotateHeldObject(rotation: new Vector3(x, y, z));
        }

        // rotate the held object if there is an object in it
        public void RotateHeldObject(Vector3 rotation) {
            if (ItemInHand == null) {
                throw new InvalidOperationException("Can't rotate held object since no object is being held.");
            }

            if (!CheckIfAgentCanRotateHand()) {
                throw new InvalidOperationException("Object is unable to rotate in its current state!");
            }

            AgentHand.transform.localRotation = Quaternion.Euler(rotation);
            // SetUpRotationBoxChecks();

            // if this is rotated too much, drop any contained object if held item is a receptacle
            if (Vector3.Angle(ItemInHand.transform.up, Vector3.up) > 95) {
                ItemInHand.GetComponent<SimObjPhysics>().DropContainedObjects(reparentContainedObjects: true, forceKinematic: false);
            }

            actionFinished(true);
        }

        // rotate the hand if there is an object in it
        public void RotateHeldObject(float pitch = 0, float yaw = 0, float roll = 0) {
            if (ItemInHand == null) {
                throw new InvalidOperationException("Can't rotate held object since no object is being held.");
            }

            Quaternion agentRot = transform.rotation;
            Quaternion agentHandStartRot = AgentHand.transform.rotation;

            transform.rotation = Quaternion.identity;

            // NOTE: -roll is used so that rotating an object rightward is positive
            AgentHand.transform.Rotate(
                new Vector3(pitch, yaw, -roll), Space.World
            );
            transform.rotation = agentRot;

            if (isHandObjectColliding(true)) {
                AgentHand.transform.rotation = agentHandStartRot;
                throw new InvalidOperationException("Held object is coliding after rotation.");
            }

            actionFinished(true);
        }

        // rotate the hand if there is an object in it
        public void RotateHandRelative(float x = 0, float y = 0, float z = 0) {
            // NOTE: -z is used for backwards compatibility.
            RotateHeldObject(pitch: x, yaw: y, roll: -z);
        }

        //utility function, spawn prefab at random position in receptacle
        //Object will fall into place with physics resolution
        public void SpawnObjectInReceptacleRandomly(string objectId, string prefabName, string targetReceptacle, FlexibleRotation rotation) {
            SimObjPhysics target = getInteractableSimObjectFromId(targetReceptacle, true);
            var spawnedObj = ProceduralTools.spawnObjectInReceptacleRandomly(this, ProceduralTools.getAssetMap(), prefabName, objectId, target, rotation);
            bool result = false;
            //object succesfully spawned, wait for it to settle, then actionReturn success and the object's position
            if (spawnedObj != null) {
                result = true;
                StartCoroutine(checkIfObjectHasStoppedMoving(sop: spawnedObj.GetComponent<SimObjPhysics>(), length: 0, useTimeout: false));
            } else {
                errorMessage = $"object ({prefabName}) could not find free space to spawn in ({targetReceptacle})";
                //if spawnedObj null, that means the random spawn failed because it couldn't find a free position
                actionFinished(result, errorMessage);
            }
        }

        //spawn prefab in a receptacle at target position. Object will fall into place with physics resolution
        public void SpawnObjectInReceptacle(string objectId, string prefabName, string targetReceptacle, Vector3 position, FlexibleRotation rotation) {
            SimObjPhysics target = getInteractableSimObjectFromId(targetReceptacle, true);
            var spawnedObj = ProceduralTools.spawnObjectInReceptacle(this, ProceduralTools.getAssetMap(), prefabName, objectId, target, position, rotation);
            bool result = false;
            //object succesfully spawned, wait for it to settle, then actionReturn success and the object's position
            if (spawnedObj != null) {
                result = true;
                StartCoroutine(checkIfObjectHasStoppedMoving(sop: spawnedObj.GetComponent<SimObjPhysics>(), length: 0, useTimeout: false));
            } else {
                errorMessage = $"object ({prefabName}) could not find free space to spawn in ({targetReceptacle}) at position ({position})";
                //if spawnedObj null, that means the random spawn failed because it couldn't find a free position
                actionFinished(result, errorMessage);
            }
        }

        //generic spawn object in scene, no physics resoultion or bounds/collision check
        public void SpawnObjectInScene(HouseObject ho) {
            var spawnedObj = ProceduralTools.spawnHouseObject(ProceduralTools.getAssetMap(), ho);
            actionFinished(true);
        }
        //used to spawn in a new object at a given position, used with ProceduralTools.spawnObjectAtReceptacle
        //places an object on the surface directly below the `position` value, with slight offset
        public bool placeNewObjectAtPoint(SimObjPhysics t, Vector3 position) {
            SimObjPhysics target = t;

            // make sure point we are moving the object to is valid
            if (!agentManager.SceneBounds.Contains(position)) {
                return false;
            }

            //ok let's get the distance from the simObj to the bottom most part of its colliders
            Vector3 targetNegY = target.transform.position + new Vector3(0, -1, 0);
            BoxCollider b = target.BoundingBox.GetComponent<BoxCollider>();
            b.enabled = true;

            Vector3 bottomPoint = b.ClosestPoint(targetNegY);
            b.enabled = false;

            float distFromSopToBottomPoint = Vector3.Distance(bottomPoint, target.transform.position);

            //adding slight offset so it doesn't clip with the floor
            float offset = distFromSopToBottomPoint + 0.00001f;
            //final position to place on surface
            Vector3 finalPos = GetSurfacePointBelowPosition(position) + new Vector3(0, offset, 0);

            //check spawn area, if its clear, then place object at finalPos
            InstantiatePrefabTest ipt = physicsSceneManager.GetComponent<InstantiatePrefabTest>();
            if (ipt.CheckSpawnArea(target, finalPos, target.transform.rotation, false)) {
                target.transform.position = finalPos;
                return true;
            }

            return false;
        }

        public void ScaleObject(
            string objectId,
            float scale,
            float scaleOverSeconds = 1.0f,
            bool forceAction = false
        ) {

            // if object is in the scene and visible, assign it to 'target'
            SimObjPhysics target = getInteractableSimObjectFromId(objectId: objectId, forceAction: forceAction);

            // neither objectId nor coordinates found an object
            if (target == null) {
                errorMessage = $"Object with ID {objectId} does not appear to be interactable.";
                actionFinished(false);
                return;
            } else {
                StartCoroutine(scaleObject(gameObject.transform.localScale * scale, target, scaleOverSeconds));
            }
        }

        public void ScaleObject(
            float x,
            float y,
            float scale,
            float scaleOverSeconds = 1.0f,
            bool forceAction = false
        ) {
            SimObjPhysics target = getInteractableSimObjectFromXY(
                x: x,
                y: y,
                forceAction: forceAction
            );
            StartCoroutine(scaleObject(gameObject.transform.localScale * scale, target, scaleOverSeconds));
        }

        private IEnumerator scaleObject(
            float scale,
            SimObjPhysics target,
            float scaleOverSeconds,
            bool skipActionFinished = false
        ) {
            return scaleObject(
                targetScale: this.transform.localScale * scale,
                target: target,
                scaleOverSeconds: scaleOverSeconds,
                skipActionFinished: skipActionFinished
            );
        }

        private IEnumerator scaleObject(
            Vector3 targetScale,
            SimObjPhysics target,
            float scaleOverSeconds,
            bool skipActionFinished = false
        ) {
            Vector3 originalScale = target.transform.localScale;
            float currentTime = 0.0f;

            if (scaleOverSeconds <= 0f) {
                target.transform.localScale = targetScale;
            } else {
                yield return new WaitForFixedUpdate();
                do {
                    target.transform.localScale = Vector3.Lerp(
                        originalScale,
                        targetScale,
                        currentTime / scaleOverSeconds
                    );
                    currentTime += Time.deltaTime;
                    yield return null;
                } while (currentTime <= scaleOverSeconds);
            }

            // store reference to all children
            Transform[] children = new Transform[target.transform.childCount];

            for (int i = 0; i < target.transform.childCount; i++) {
                children[i] = target.transform.GetChild(i);
            }

            // detach all children
            target.transform.DetachChildren();
            // zero out object transform to be 1, 1, 1
            target.transform.transform.localScale = Vector3.one;
            // reparent all children
            foreach (Transform t in children) {
                t.SetParent(target.transform);
            }

            autoSyncTransforms();

            target.ContextSetUpBoundingBox(forceCacheReset: true);

            if (!skipActionFinished) {
                actionFinished(true);
            }
        }

        // pass in a Vector3, presumably from GetReachablePositions, and try to place a specific Sim Object there
        // unlike PlaceHeldObject or InitialRandomSpawn, this won't be limited by a Receptacle, but only
        // limited by collision
        public void PlaceObjectAtPoint(
            string objectId,
            Vector3 position,
            Vector3? rotation = null,
            bool forceKinematic = false
        ) {
            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(objectId)) {
                errorMessage = $"Cannot find object with id {objectId}.";
                actionFinishedEmit(false);
                return;
            }

            // find the object in the scene, disregard visibility
            SimObjPhysics target = physicsSceneManager.ObjectIdToSimObjPhysics[objectId];

            bool placeObjectSuccess = PlaceObjectAtPoint(
                target: target,
                position: position,
                rotation: rotation,
                forceKinematic: forceKinematic,
                includeErrorMessage: true
            );

            if (placeObjectSuccess) {
                if (!forceKinematic) {
                    StartCoroutine(checkIfObjectHasStoppedMoving(target, 0, true));
                    return;
                } else {
                    actionFinished(true);
                    return;
                }
            } else {
                actionFinished(false);
                return;
            }
        }

        public bool PlaceObjectAtPoint(
            SimObjPhysics target,
            Vector3 position,
            Vector3? rotation,
            bool forceKinematic,
            bool includeErrorMessage = false
        ) {
            // make sure point we are moving the object to is valid
            if (!agentManager.sceneBounds.Contains(position)) {
                if (includeErrorMessage) {
                    errorMessage = $"Position coordinate ({position}) is not within scene bounds ({agentManager.sceneBounds})";
                }
                return false;
            }

            Quaternion originalRotation = target.transform.rotation;
            if (rotation.HasValue) {
                target.transform.rotation = Quaternion.Euler(rotation.Value);
            }
            Vector3 originalPos = target.transform.position;
            target.transform.position = agentManager.SceneBounds.min - new Vector3(-100f, -100f, -100f);

            if (!Physics.autoSyncTransforms) {
                Physics.SyncTransforms();
            }

            bool wasInHand = false;
            if (ItemInHand) {
                if (ItemInHand.transform.gameObject == target.transform.gameObject) {
                    wasInHand = true;
                }
            }

            // ok let's get the distance from the simObj to the bottom most part of its colliders
            Vector3 targetNegY = target.transform.position + new Vector3(0, -1, 0);
            BoxCollider b = target.BoundingBox.GetComponent<BoxCollider>();

            b.enabled = true;
            Vector3 bottomPoint = b.ClosestPoint(targetNegY);
            b.enabled = false;

            float distFromSopToBottomPoint = Vector3.Distance(bottomPoint, target.transform.position);

            float offset = distFromSopToBottomPoint + 0.005f; // Offset in case the surface below isn't completely flat

            Vector3 finalPos = GetSurfacePointBelowPosition(position) + new Vector3(0, offset, 0);

            // Check spawn area here            
            target.transform.position = finalPos;

            if (!Physics.autoSyncTransforms) {
                Physics.SyncTransforms();
            }

            Collider colliderHitIfSpawned = UtilityFunctions.firstColliderObjectCollidingWith(
                target.gameObject
            );

            if (colliderHitIfSpawned == null) {
                target.transform.position = finalPos;

                Rigidbody rb = target.GetComponent<Rigidbody>();

                if (forceKinematic) {
                    rb.isKinematic = forceKinematic;
                }

                // Additional stuff we need to do if placing item that was in hand
                if (wasInHand) {

                    rb.constraints = RigidbodyConstraints.None;
                    rb.useGravity = true;

                    // change collision detection mode while falling so that obejcts don't phase through colliders.
                    // this is reset to discrete on SimObjPhysics.cs's update 
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

                    GameObject topObject = GameObject.Find("Objects");
                    if (topObject != null) {
                        ItemInHand.transform.parent = topObject.transform;
                    } else {
                        ItemInHand.transform.parent = null;
                    }

                    target.DropContainedObjects(reparentContainedObjects: true, forceKinematic: forceKinematic);
                    target.isInAgentHand = false;
                    ItemInHand = null;

                }
                return true;
            }

            target.transform.position = originalPos;
            target.transform.rotation = originalRotation;

            // if the original position was in agent hand, reparent object to agent hand
            if (wasInHand) {
                target.transform.SetParent(AgentHand.transform);
                ItemInHand = target.gameObject;
                target.isInAgentHand = true;
                target.GetComponent<Rigidbody>().isKinematic = true;
            }

            if (includeErrorMessage) {
                SimObjPhysics hitSop = ancestorSimObjPhysics(colliderHitIfSpawned.gameObject);
                errorMessage = (
                    $"Spawn area not clear ({(hitSop != null ? hitSop.ObjectID : colliderHitIfSpawned.name)})"
                    + " is in the way), can't place object at that point"
                );
            }
            return false;
        }

        public void PlaceObjectAtPoint(
            string objectId,
            Vector3[] positions,
            Vector3? rotation = null,
            bool forceKinematic = false
        ) {
            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(objectId)) {
                errorMessage = $"Cannot find object with id {objectId}.";
                actionFinishedEmit(false);
                return;
            }

            // find the object in the scene, disregard visibility
            SimObjPhysics target = physicsSceneManager.ObjectIdToSimObjPhysics[objectId];

            bool placeObjectSuccess = false;

            foreach (Vector3 position in positions) {
                placeObjectSuccess = PlaceObjectAtPoint(
                    target: target,
                    position: position,
                    rotation: rotation,
                    forceKinematic: forceKinematic,
                    includeErrorMessage: true
                );
                if (placeObjectSuccess) {
                    errorMessage = "";
                    break;
                }
            }

            if (placeObjectSuccess) {
                if (!forceKinematic) {
                    StartCoroutine(checkIfObjectHasStoppedMoving(target, 0, true));
                    return;
                } else {
                    actionFinished(true);
                    return;
                }
            } else {
                actionFinished(false);
                return;
            }
        }

        // Similar to PlaceObjectAtPoint(...) above but returns a bool if successful
        public bool placeObjectAtPoint(SimObjPhysics t, Vector3 position) {
            SimObjPhysics target = null;
            // find the object in the scene, disregard visibility
            foreach (SimObjPhysics sop in VisibleSimObjs(true)) {
                if (sop.objectID == t.objectID) {
                    target = sop;
                }
            }

            if (target == null) {
                return false;
            }

            // make sure point we are moving the object to is valid
            if (!agentManager.sceneBounds.Contains(position)) {
                return false;
            }

            // ok let's get the distance from the simObj to the bottom most part of its colliders
            Vector3 targetNegY = target.transform.position + new Vector3(0, -1, 0);
            BoxCollider b = target.BoundingBox.GetComponent<BoxCollider>();

            b.enabled = true;
            Vector3 bottomPoint = b.ClosestPoint(targetNegY);
            b.enabled = false;

            float distFromSopToBottomPoint = Vector3.Distance(bottomPoint, target.transform.position);

            float offset = distFromSopToBottomPoint;

            // final position to place on surface
            Vector3 finalPos = GetSurfacePointBelowPosition(position) + new Vector3(0, offset, 0);


            // check spawn area, if its clear, then place object at finalPos
            InstantiatePrefabTest ipt = physicsSceneManager.GetComponent<InstantiatePrefabTest>();
            if (ipt.CheckSpawnArea(target, finalPos, target.transform.rotation, false)) {
                target.transform.position = finalPos;
                return true;
            }

            return false;
        }
        // uncomment this to debug draw valid points
        // private List<Vector3> validpointlist = new List<Vector3>();

        // return a bunch of vector3 points above a target receptacle
        // if forceVisible = true, return points regardless of where receptacle is
        // if forceVisible = false, only return points that are also within view of the Agent camera
        public void GetSpawnCoordinatesAboveReceptacle(ServerAction action) {
            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(action.objectId)) {
                errorMessage = "Object ID appears to be invalid.";
                actionFinished(false);
                return;
            }

            SimObjPhysics target = null;
            // find our target receptacle
            // if action.anywhere False (default) this should only return objects that are visible
            // if action.anywhere true, return for any object no matter where it is
            foreach (SimObjPhysics sop in VisibleSimObjs(action.anywhere)) {
                if (action.objectId == sop.ObjectID) {
                    target = sop;
                }
            }

            if (target == null) {
                if (action.anywhere) {
                    errorMessage = "No valid Receptacle found in scene";
                } else {
                    errorMessage = "No valid Receptacle found in view";
                }

                actionFinished(false);
                return;
            }

            // ok now get spawn points from target
            List<Vector3> targetPoints = new List<Vector3>();
            targetPoints = target.FindMySpawnPointsFromTopOfTriggerBox();

            // by default, action.anywhere = false, so remove all targetPoints that are outside of agent's view
            // if anywhere true, don't do this and just return all points we got from above
            if (!action.anywhere) {
                List<Vector3> filteredTargetPoints = new List<Vector3>();
                foreach (Vector3 v in targetPoints) {
                    try {
                        assertPosInView(targetPosition: v, inMaxVisibleDistance: true, inViewport: true);
                        filteredTargetPoints.Add(v);
                    } catch (Exception e) {
                        continue;
                    }
                }

                targetPoints = filteredTargetPoints;
            }

            // uncomment to debug draw valid points
            // #if UNITY_EDITOR
            // validpointlist = targetPoints;
            // #endif

            actionFinished(true, targetPoints);
        }

        // same as GetSpawnCoordinatesAboveReceptacle(Server Action) but takes a sim obj phys instead
        // returns a list of vector3 coordinates above a receptacle. These coordinates will make up a grid above the receptacle
        public List<Vector3> getSpawnCoordinatesAboveReceptacle(SimObjPhysics t) {
            SimObjPhysics target = t;
            // ok now get spawn points from target
            List<Vector3> targetPoints = new List<Vector3>();
            targetPoints = target.FindMySpawnPointsFromTopOfTriggerBox();
            return targetPoints;
        }

        // instantiate a target circle, and then place it in a "SpawnOnlyOUtsideReceptacle" that is also within camera view
        // If fails, return actionFinished(false) and despawn target circle
        public void SpawnTargetCircle(ServerAction action) {
            if (action.objectVariation > 2 || action.objectVariation < 0) {
                errorMessage = "Please use valid int for SpawnTargetCircleAction. Valid ints are: 0, 1, 2 for small, medium, large circles";
                actionFinished(false);
                return;
            }
            // instantiate a target circle
            GameObject targetCircle = Instantiate(
                original: TargetCircles[action.objectVariation],
                position: new Vector3(0, 100, 0),
                rotation: Quaternion.identity
            ) as GameObject;
            targetCircle.transform.parent = GameObject.Find("Objects").transform;
            List<SimObjPhysics> targetReceptacles = new List<SimObjPhysics>();
            InstantiatePrefabTest ipt = physicsSceneManager.GetComponent<InstantiatePrefabTest>();

            // this is the default, only spawn circles in objects that are in view
            if (!action.anywhere) {
                // check every sim object and see if it is within the viewport
                foreach (SimObjPhysics sop in VisibleSimObjs(true)) {
                    if (sop.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.Receptacle)) {
                        /// one more check, make sure this receptacle
                        if (ReceptacleRestrictions.SpawnOnlyOutsideReceptacles.Contains(sop.ObjType)) {
                            // ok now check if the object is for real in the viewport
                            if (objectIsWithinViewport(sop)) {
                                targetReceptacles.Add(sop);
                            }
                        }
                    }
                }
            }

            // spawn target circle in any valid "outside" receptacle in the scene even if not in veiw
            else {
                // targetReceptacles.AddRange(physicsSceneManager.ReceptaclesInScene); 
                foreach (SimObjPhysics sop in physicsSceneManager.GatherAllReceptaclesInScene()) {
                    if (ReceptacleRestrictions.SpawnOnlyOutsideReceptacles.Contains(sop.ObjType)) {
                        targetReceptacles.Add(sop);
                    }
                }
            }


            // if we passed in a objectId, see if it is in the list of targetReceptacles found so far
            if (action.objectId != null) {
                List<SimObjPhysics> filteredTargetReceptacleList = new List<SimObjPhysics>();
                foreach (SimObjPhysics sop in targetReceptacles) {
                    if (sop.objectID == action.objectId) {
                        filteredTargetReceptacleList.Add(sop);
                    }
                }

                targetReceptacles = filteredTargetReceptacleList;
            }

            // if(action.randomSeed != 0)
            // {
            //     targetReceptacles.Shuffle_(action.randomSeed);
            // }

            bool succesfulSpawn = false;

            if (targetReceptacles.Count <= 0) {
                errorMessage = "for some reason, no receptacles were found in the scene!";
                Destroy(targetCircle);
                actionFinished(false);
                return;
            }

            // ok we have a shuffled list of receptacles that is picked based on the seed....
            foreach (SimObjPhysics sop in targetReceptacles) {
                // for every receptacle, we will get a returned list of receptacle spawn points, and then try placeObjectReceptacle
                List<ReceptacleSpawnPoint> rsps = new List<ReceptacleSpawnPoint>();

                rsps = sop.ReturnMySpawnPoints();
                List<ReceptacleSpawnPoint> editedRsps = new List<ReceptacleSpawnPoint>();
                bool constraintsUsed = false;// only set rsps to editedRsps if constraints were passed in

                // only do further constraint checks if defaults are overwritten
                if (!(action.minDistance == 0 && action.maxDistance == 0)) {
                    foreach (ReceptacleSpawnPoint p in rsps) {
                        // get rid of differences in y values for points
                        Vector3 normalizedPosition = new Vector3(transform.position.x, 0, transform.position.z);
                        Vector3 normalizedPoint = new Vector3(p.Point.x, 0, p.Point.z);

                        if (action.minDistance == 0 && action.maxDistance > 0) {
                            // check distance from agent's transform to spawnpoint
                            if ((Vector3.Distance(normalizedPoint, normalizedPosition) <= action.maxDistance)) {
                                editedRsps.Add(p);
                            }

                            constraintsUsed = true;
                        }

                        // min distance passed in, no max distance
                        if (action.maxDistance == 0 && action.minDistance > 0) {
                            // check distance from agent's transform to spawnpoint
                            if ((Vector3.Distance(normalizedPoint, normalizedPosition) >= action.minDistance)) {
                                editedRsps.Add(p);
                            }

                            constraintsUsed = true;
                        } else {
                            // these are default so don't filter by distance
                            // check distance from agent's transform to spawnpoint
                            if ((Vector3.Distance(normalizedPoint, normalizedPosition) >= action.minDistance
                            && Vector3.Distance(normalizedPoint, normalizedPosition) <= action.maxDistance)) {
                                editedRsps.Add(p);
                            }

                            constraintsUsed = true;
                        }
                    }
                }

                if (constraintsUsed) {
                    rsps = editedRsps;
                }

                rsps.Shuffle_(action.randomSeed);

                // only place in viewport
                if (!action.anywhere) {
                    if (ipt.PlaceObjectReceptacleInViewport(this, rsps, targetCircle.GetComponent<SimObjPhysics>(), true, 500, 90, true)) {
                        // make sure target circle is within viewport
                        succesfulSpawn = true;
                        break;
                    }
                }
                // place anywhere
                else {
                    if (ipt.PlaceObjectReceptacle(rsps, targetCircle.GetComponent<SimObjPhysics>(), true, 500, 90, true)) {
                        // make sure target circle is within viewport
                        succesfulSpawn = true;
                        break;
                    }
                }
            }

            if (succesfulSpawn) {
                // if image synthesis is active, make sure to update the renderers for image synthesis since now there are new objects with renderes in the scene
                BaseFPSAgentController primaryAgent = GameObject.Find("PhysicsSceneManager").GetComponent<AgentManager>().PrimaryAgent;
                if (primaryAgent.imageSynthesis) {
                    if (primaryAgent.imageSynthesis.enabled) {
                        primaryAgent.imageSynthesis.OnSceneChange();
                    }
                }

                SimObjPhysics targetSOP = targetCircle.GetComponent<SimObjPhysics>();
                physicsSceneManager.Generate_ObjectID(targetSOP);
                physicsSceneManager.AddToObjectsInScene(targetSOP);
                actionFinished(true, targetSOP.objectID);// return the objectID of circle spawned for easy reference
            } else {
                Destroy(targetCircle);
                errorMessage = "circle failed to spawn";
                actionFinished(false);
            }
        }

        public void MakeObjectsOfTypeUnbreakable(string objectType) {
            if (objectType == null) {
                errorMessage = "no object type specified for MakeOBjectsOfTypeUnbreakable()";
                actionFinished(false);
            }

            SimObjPhysics[] simObjs = GameObject.FindObjectsOfType(typeof(SimObjPhysics)) as SimObjPhysics[];
            if (simObjs != null) {
                foreach (SimObjPhysics sop in simObjs) {
                    if (sop.Type.ToString() == objectType) {
                        if (sop.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanBreak)) {
                            //look both in this object and children so things like Windows don't FREAK OUT
                            sop.GetComponentInChildren<Break>().Unbreakable = true;
                        }
                    }
                }
            }

            actionFinished(true);
        }

        public void MakeAllObjectsUnbreakable() {
            UpdateBreakabilityOfAllObjects(true);

        }

        public void MakeAllObjectsBreakable() {
            UpdateBreakabilityOfAllObjects(false);
        }

        private void UpdateBreakabilityOfAllObjects(bool isUnbreakable) {
            SimObjPhysics[] simObjs = GameObject.FindObjectsOfType(typeof(SimObjPhysics)) as SimObjPhysics[];
            if (simObjs != null) {
                foreach (SimObjPhysics sop in simObjs) {
                    if (sop.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanBreak)) {
                        sop.GetComponentInChildren<Break>().Unbreakable = isUnbreakable;
                    }
                }
            }
            actionFinished(true);
        }

        public void MakeObjectBreakable(string objectId) {
            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(objectId)) {
                throw new ArgumentException($"Object ID {objectId} appears to be invalid.");
            }
            SimObjPhysics sop = physicsSceneManager.ObjectIdToSimObjPhysics[objectId];
            if (sop.GetComponent<Break>()) {
                sop.GetComponent<Break>().Unbreakable = false;
            }
            actionFinishedEmit(true);
        }

        public void SetObjectPoses(ServerAction action) {
            // make sure objectPoses and also the Object Pose elements inside are initialized correctly
            if (action.objectPoses == null || action.objectPoses[0] == null) {
                errorMessage = "objectPoses was not initialized correctly. Please make sure each element in the objectPoses list is initialized.";
                actionFinished(false);
                return;
            }
            StartCoroutine(setObjectPoses(action.objectPoses, action.placeStationary));
        }

        // SetObjectPoses is performed in a coroutine otherwise if
        // a frame does not pass prior to this AND the imageSynthesis
        // is enabled for say depth or normals, Unity will crash on 
        // a subsequent scene reset()
        protected IEnumerator setObjectPoses(ObjectPose[] objectPoses, bool placeStationary) {
            yield return new WaitForEndOfFrame();
            bool success = physicsSceneManager.SetObjectPoses(objectPoses, out errorMessage, placeStationary);

            //update image synthesis since scene has changed
            if (this.imageSynthesis && this.imageSynthesis.enabled) {
                this.imageSynthesis.OnSceneChange();
            }

            actionFinished(success, errorMessage);
        }

        // set all objects objects of a given type to a specific state, if that object has that state
        // ie: All objects of type Bowl that have the state property breakable, set isBroken = true
        public void SetObjectStates(ServerAction action) {
            if (action.SetObjectStates == null) {
                errorMessage = "action.SetObjectStates is null or not initialized!";
                actionFinished(false);
                return;
            }

            // if both the objectType and stateChange members are null, params not set correctly
            if (action.SetObjectStates.objectType == null && action.SetObjectStates.stateChange == null) {
                errorMessage = "action.SetObjectStates has objectType and stateChange strings null. Please pass in valid strings.";
                actionFinished(false);
                return;
            }

            // if you pass in an ObjectType, you must also pass in which stateChange of that object you are trying to Set
            if (action.SetObjectStates.objectType != null && action.SetObjectStates.stateChange == null) {
                errorMessage = "action.SetObjectStates is missing stateChange string. If setting objects by objectType, Please specify both an objectType and a stateChange compatible with that objectType to set.";
                actionFinished(false);
                return;
            }

            // call a coroutine to return actionFinished for all objects that have animation time
            if (action.SetObjectStates.stateChange == "toggleable" || action.SetObjectStates.stateChange == "openable") {
                StartCoroutine(SetStateOfAnimatedObjects(action.SetObjectStates));
            }

            // these object change states instantly, so no need for coroutine
            // the function called will handle the actionFinished() return;
            else {
                SetStateOfObjectsThatDontHaveAnimationTime(action.SetObjectStates);
            }
        }

        // find all objects in scene of type specified by SetObjectStates.objectType
        // toggle them to the bool if applicable: isOpen, isToggled, isBroken etc.
        protected IEnumerator SetStateOfAnimatedObjects(SetObjectStates SetObjectStates) {
            List<SimObjPhysics> animating = new List<SimObjPhysics>();
            Dictionary<SimObjPhysics, string> animatingType = new Dictionary<SimObjPhysics, string>();

            // in this case, we will try and set isToggled for all toggleable objects in the entire scene
            if (SetObjectStates.objectType == null) {
                foreach (SimObjPhysics sop in VisibleSimObjs(true)) {
                    if (SetObjectStates.stateChange == "toggleable") {
                        if (sop.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanToggleOnOff) && sop.GetComponent<CanToggleOnOff>()) {
                            StartCoroutine(toggleObject(sop, SetObjectStates.isToggled));
                            animating.Add(sop);
                            animatingType[sop] = "toggleable";
                        }
                    }

                    if (SetObjectStates.stateChange == "openable") {
                        if (sop.GetComponent<CanOpen_Object>()) {
                            openObject(
                                target: sop,
                                openness: SetObjectStates.isOpen ? 1 : 0,
                                forceAction: true,
                                markActionFinished: false
                            );
                            animatingType[sop] = "openable";
                            animating.Add(sop);
                        }
                    }
                }
            } else {
                // in this case, we will only try and set states for objects of the specified objectType
                SimObjType sot = (SimObjType)System.Enum.Parse(typeof(SimObjType), SetObjectStates.objectType);

                // for every sim obj in scene, find objects of type specified first
                foreach (SimObjPhysics sop in VisibleSimObjs(true)) {
                    // ok we found an object with type specified, now toggle it 
                    if (sop.ObjType == sot) {
                        if (SetObjectStates.stateChange == "toggleable") {
                            if (sop.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanToggleOnOff) && sop.GetComponent<CanToggleOnOff>()) {
                                StartCoroutine(toggleObject(sop, SetObjectStates.isToggled));
                                animating.Add(sop);
                                animatingType[sop] = "toggleable";
                            }
                        }

                        if (SetObjectStates.stateChange == "openable") {
                            if (sop.GetComponent<CanOpen_Object>()) {
                                openObject(
                                    target: sop,
                                    openness: SetObjectStates.isOpen ? 1 : 0,
                                    forceAction: true,
                                    markActionFinished: false);
                                animating.Add(sop);
                                animatingType[sop] = "openable";
                            }
                        }
                    }
                }
            }

            if (animating.Count > 0) {
                // we have now started the toggle for all objects in the ObjectStates array
                int numStillGoing = animating.Count;
                while (numStillGoing > 0) {
                    foreach (SimObjPhysics sop in animating) {
                        if (animatingType.ContainsKey(sop) &&
                            (animatingType[sop] == "toggleable" && sop.GetComponent<CanToggleOnOff>().GetIsCurrentlyLerping() == false || animatingType[sop] == "openable") &&
                            sop.GetComponent<CanOpen_Object>().GetIsCurrentlyLerping() == false
                        ) {
                            numStillGoing--;
                        }
                    }
                    // someone is still animating
                    if (numStillGoing > 0) {
                        numStillGoing = animating.Count;
                    }

                    // hold your horses, wait a frame so we don't miss the timing
                    yield return null;
                }
            }

            // ok none of the objects that were actively toggling have any lerps going, so we are done!
            actionFinished(true);
        }

        // for setting object states that don't have an animation time, which means they don't require coroutines yeah!
        protected void SetStateOfObjectsThatDontHaveAnimationTime(SetObjectStates SetObjectStates) {

            // ok what state are we lookin at here
            switch (SetObjectStates.stateChange) {
                case "breakable": {
                        foreach (SimObjPhysics sop in VisibleSimObjs(true)) {
                            if (sop.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanBreak)) {
                                // only break objects that are not already broken
                                Break b = sop.GetComponent<Break>();

                                // only actually do stuff is the object is not broken and we are trying to break it
                                if (!b.isBroken() && SetObjectStates.isBroken) {

                                    // oh we have a specific object type?
                                    if (SetObjectStates.objectType != null) {
                                        if (sop.Type == (SimObjType)System.Enum.Parse(typeof(SimObjType), SetObjectStates.objectType)) {
                                            b.BreakObject(null);
                                        } else {
                                            continue;
                                        }
                                    } else {
                                        b.BreakObject(null);
                                    }
                                }
                            }
                        }

                        break;
                    }

                case "canFillWithLiquid": {
                        foreach (SimObjPhysics sop in VisibleSimObjs(true)) {
                            // only proceed if the sop is fillable
                            if (sop.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanBeFilled)) {
                                Fill fil = sop.GetComponent<Fill>();

                                // object is empty and trying to fill it
                                if (!fil.IsFilled() && SetObjectStates.isFilledWithLiquid) {
                                    // oh, we have a specific object type?
                                    if (SetObjectStates.objectType != null) {
                                        // we found an object of the type we want to set
                                        if (sop.Type == (SimObjType)System.Enum.Parse(typeof(SimObjType), SetObjectStates.objectType)) {
                                            fil.FillObject("water");
                                        }

                                        // doesn't match objectType, continue to next object
                                        else {
                                            continue;
                                        }
                                    } else {
                                        fil.FillObject("water");
                                    }
                                }

                                // object is full of some liquid, and trying to empty it
                                else if (fil.IsFilled() && !SetObjectStates.isFilledWithLiquid) {
                                    // oh, we have a specific object type?
                                    if (SetObjectStates.objectType != null) {
                                        // we found an object of the type we want to set
                                        if (sop.Type == (SimObjType)System.Enum.Parse(typeof(SimObjType), SetObjectStates.objectType)) {
                                            fil.EmptyObject();
                                        }

                                        // doesn't match objectType, continue to next object
                                        else {
                                            continue;
                                        }
                                    } else {
                                        fil.EmptyObject();
                                    }
                                }
                            }
                        }

                        break;
                    }

                case "dirtyable": {
                        foreach (SimObjPhysics sop in VisibleSimObjs(true)) {
                            // only proceed if the sop is dirtyable
                            if (sop.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanBeDirty)) {
                                Dirty deedsDoneDirtCheap = sop.GetComponent<Dirty>();

                                // object is clean and we are trying to dirty it
                                if (!deedsDoneDirtCheap.IsDirty() && SetObjectStates.isDirty) {
                                    // oh, we have a specific object type?
                                    if (SetObjectStates.objectType != null) {
                                        // we found an object of the type we want to set
                                        if (sop.Type == (SimObjType)System.Enum.Parse(typeof(SimObjType), SetObjectStates.objectType)) {
                                            deedsDoneDirtCheap.ToggleCleanOrDirty();
                                        }

                                        // doesn't match objectType, continue to next object
                                        else {
                                            continue;
                                        }
                                    } else {
                                        deedsDoneDirtCheap.ToggleCleanOrDirty();
                                    }
                                }

                                // object is dirty and we are trying to clean it
                                else if (deedsDoneDirtCheap.IsDirty() && !SetObjectStates.isDirty) {
                                    // oh, we have a specific object type?
                                    if (SetObjectStates.objectType != null) {
                                        // we found an object of the type we want to set
                                        if (sop.Type == (SimObjType)System.Enum.Parse(typeof(SimObjType), SetObjectStates.objectType)) {
                                            deedsDoneDirtCheap.ToggleCleanOrDirty();
                                        }

                                        // doesn't match objectType, continue to next object
                                        else {
                                            continue;
                                        }
                                    } else {
                                        deedsDoneDirtCheap.ToggleCleanOrDirty();
                                    }
                                }
                            }
                        }

                        break;
                    }

                // one way
                case "cookable": {
                        foreach (SimObjPhysics sop in VisibleSimObjs(true)) {
                            if (sop.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanBeCooked)) {
                                CookObject c = sop.GetComponent<CookObject>();

                                // only do stuff if object is not cooked and we are trying to cook it
                                if (!c.IsCooked() && SetObjectStates.isCooked) {
                                    if (SetObjectStates.objectType != null) {
                                        if (sop.Type == (SimObjType)System.Enum.Parse(typeof(SimObjType), SetObjectStates.objectType)) {
                                            c.Cook();
                                        } else {
                                            continue;
                                        }
                                    } else {
                                        c.Cook();
                                    }
                                }
                            }
                        }

                        break;
                    }

                // one way
                case "sliceable": {
                        foreach (SimObjPhysics sop in VisibleSimObjs(true)) {
                            if (sop.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanBeSliced)) {
                                SliceObject s = sop.GetComponent<SliceObject>();

                                // only do stuff if object is unsliced and we are trying to slice it
                                if (!s.IsSliced() && SetObjectStates.isSliced) {
                                    if (SetObjectStates.objectType != null) {
                                        if (sop.Type == (SimObjType)System.Enum.Parse(typeof(SimObjType), SetObjectStates.objectType)) {
                                            s.Slice();
                                        } else {
                                            continue;
                                        }
                                    } else {
                                        s.Slice();
                                    }
                                }
                            }
                        }

                        break;
                    }

                // one way
                case "canBeUsedUp": {
                        foreach (SimObjPhysics sop in VisibleSimObjs(true)) {
                            if (sop.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanBeUsedUp)) {
                                UsedUp u = sop.GetComponent<UsedUp>();

                                // only do stuff if object is not used up and we are trying to use it up
                                if (!u.isUsedUp && SetObjectStates.isUsedUp) {
                                    if (SetObjectStates.objectType != null) {
                                        if (sop.Type == (SimObjType)System.Enum.Parse(typeof(SimObjType), SetObjectStates.objectType)) {
                                            u.UseUp();
                                        } else {
                                            continue;
                                        }
                                    } else {
                                        u.UseUp();
                                    }
                                }
                            }
                        }

                        break;
                    }
            }

            actionFinished(true);
        }


        public void PutObject(float x, float y, bool forceAction = false, bool placeStationary = true, int randomSeed = 0, bool putNearXY = false) {
            PlaceHeldObject(
                x: x,
                y: y,
                forceAction: forceAction,
                placeStationary: placeStationary,
                randomSeed: randomSeed,
                putNearXY: putNearXY,
                maxDistance: maxVisibleDistance);
        }

        public void PutObject(string objectId, bool forceAction = false, bool placeStationary = true, int randomSeed = 0) {
            PlaceHeldObject(
                objectId: objectId,
                forceAction: forceAction,
                placeStationary: placeStationary,
                randomSeed: randomSeed,
                maxDistance: maxVisibleDistance);
        }

        // if you are holding an object, place it on a valid Receptacle 
        // used for placing objects on receptacles without enclosed restrictions (drawers, cabinets, etc)
        // only checks if the object can be placed on top of the target receptacle via the receptacle trigger box 
        public void PlaceHeldObject(float x, float y, float maxDistance, bool forceAction = false, bool placeStationary = true, int randomSeed = 0, bool putNearXY = false) {
            SimObjPhysics targetReceptacle = null;

            RaycastHit hit = new RaycastHit();

            screenToWorldTarget(
                x: x,
                y: y,
                target: ref targetReceptacle,
                forceAction: forceAction,
                hit: out hit
            );

            placeHeldObject(
                targetReceptacle: targetReceptacle,
                forceAction: forceAction,
                placeStationary: placeStationary,
                randomSeed: randomSeed,
                maxDistance: maxDistance,
                putNearXY: putNearXY,
                hit: hit
            );
        }


        // overload of PlaceHeldObject that takes a target receptacle by objectId instead of screenspace coordinate raycast
        public void PlaceHeldObject(string objectId, float maxDistance, bool forceAction = false, bool placeStationary = true, int randomSeed = 0) {
            // get the target receptacle based on the action object ID
            SimObjPhysics targetReceptacle = null;

            // if object is in the scene and visible, assign it to 'target'
            targetReceptacle = getInteractableSimObjectFromId(objectId: objectId, forceAction: forceAction);

            placeHeldObject(
                targetReceptacle: targetReceptacle,
                forceAction: forceAction,
                placeStationary: placeStationary,
                randomSeed: randomSeed,
                maxDistance: maxDistance);

        }

        private void placeHeldObject(
            SimObjPhysics targetReceptacle,
            bool forceAction,
            bool placeStationary,
            int randomSeed,
            float maxDistance) {

            RaycastHit hit = new RaycastHit();

            placeHeldObject(
                targetReceptacle: targetReceptacle,
                forceAction: forceAction,
                placeStationary: placeStationary,
                randomSeed: randomSeed,
                maxDistance: maxDistance,
                putNearXY: false,
                hit: hit);
        }

        private void placeHeldObject(
            SimObjPhysics targetReceptacle,
            bool forceAction,
            bool placeStationary,
            int randomSeed,
            float maxDistance,
            bool putNearXY,
            RaycastHit hit) {
            // #if UNITY_EDITOR
            // var watch = System.Diagnostics.Stopwatch.StartNew();
            // #endif

            // check if we are even holding anything
            if (ItemInHand == null) {
                errorMessage = "Can't place an object if Agent isn't holding anything";
                actionFinished(false);
                return;
            }


            if (targetReceptacle == null) {
                errorMessage = "No valid Receptacle found";
                Debug.Log(errorMessage);
                actionFinished(false);
                return;
            }

            if (!targetReceptacle.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.Receptacle)) {
                errorMessage = "This target object is NOT a receptacle!";
                Debug.Log(errorMessage);
                actionFinished(false);
                return;
            }

            // if receptacle can open, check that it's open before placing. Can't place objects in something that is closed!
            if (targetReceptacle.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanOpen)) {
                if (ReceptacleRestrictions.MustBeOpenToPlaceObjectsIn.Contains(targetReceptacle.ObjType)) {
                    if (!targetReceptacle.GetComponent<CanOpen_Object>().isOpen) {
                        errorMessage = "Target openable Receptacle is CLOSED, can't place if target is not open!";
                        Debug.Log(errorMessage);
                        actionFinished(false);
                        return;
                    }
                }
            }

            // if this receptacle only receives specific objects, check that the ItemInHand is compatible and
            // check if the receptacle is currently full with another valid object or not
            if (targetReceptacle.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.ObjectSpecificReceptacle)) {
                ObjectSpecificReceptacle osr = targetReceptacle.GetComponent<ObjectSpecificReceptacle>();
                if (osr.HasSpecificType(ItemInHand.GetComponent<SimObjPhysics>().ObjType) && !osr.isFull()) {
                    // check spawn area specifically if it's a stove top we are trying to place something in because
                    // they are close together and can overlap and are weird
                    if (osr.GetComponent<SimObjPhysics>().Type == SimObjType.StoveBurner) {
                        // PhysicsSceneManager psm = GameObject.Find("PhysicsSceneManager").GetComponent<PhysicsSceneManager>();
                        if (physicsSceneManager.StoveTopCheckSpawnArea(ItemInHand.GetComponent<SimObjPhysics>(), osr.attachPoint.transform.position,
                                osr.attachPoint.transform.rotation, false) == false) {
                            errorMessage = "another object's collision is blocking held object from being placed";
                            actionFinished(false);
                            return;
                        }

                    }

                    ItemInHand.transform.position = osr.attachPoint.position;
                    ItemInHand.transform.SetParent(osr.attachPoint.transform);
                    ItemInHand.transform.localRotation = Quaternion.identity;
                    ItemInHand.GetComponent<Rigidbody>().isKinematic = true;
                    ItemInHand.GetComponent<SimObjPhysics>().isInAgentHand = false;// remove in agent hand flag
                    ItemInHand = null;
                    DefaultAgentHand();
                    actionFinished(true);
                    return;
                } else {

                    if (osr.attachPoint.transform.childCount > 0 || osr.isFull()) {
                        errorMessage = targetReceptacle.name + " is full right now";
                    } else {
                        errorMessage = ItemInHand.name + " is not a valid Object Type to be placed in " + targetReceptacle.name;
                    }

                    actionFinished(false);
                    return;
                }
            }

            SimObjPhysics handSOP = ItemInHand.GetComponent<SimObjPhysics>();

            if (!forceAction) {
                // check if the item we are holding can even be placed in the action.ObjectID target at all
                foreach (KeyValuePair<SimObjType, List<SimObjType>> res in ReceptacleRestrictions.PlacementRestrictions) {
                    // find the Object Type in the PlacementRestrictions dictionary
                    if (res.Key == handSOP.ObjType) {
                        if (!res.Value.Contains(targetReceptacle.ObjType)) {
                            errorMessage = ItemInHand.name + " cannot be placed in " + targetReceptacle.transform.name;
                            Debug.Log(errorMessage);
                            actionFinished(false);
                            return;
                        }
                    }
                }
            }

            bool onlyPointsCloseToAgent = !forceAction;

            // if the target is something like a pot or bowl on a table, return all valid points instead of ONLY visible points since
            // the Agent can't see the bottom of the receptacle if it's placed too high on a table
            if (ReceptacleRestrictions.ReturnAllPoints.Contains(targetReceptacle.ObjType)) {
                onlyPointsCloseToAgent = false;
            }

            bool placeUpright = false;
            // check if the object should be forced to only check upright placement angles (this prevents things like Pots being placed sideways)
            if (ReceptacleRestrictions.AlwaysPlaceUpright.Contains(handSOP.ObjType)) {
                placeUpright = true;
            }

            // ok we are holding something, time to try and place it
            InstantiatePrefabTest script = physicsSceneManager.GetComponent<InstantiatePrefabTest>();
            // set degreeIncrement to 90 for placing held objects to check for vertical angles
            List<ReceptacleSpawnPoint> spawnPoints = targetReceptacle.ReturnMySpawnPoints(onlyPointsCloseToAgent ? this : null);
            if (randomSeed != 0 || putNearXY) {
                List<KeyValuePair<ReceptacleSpawnPoint, float>> distSpawnPoints = new List<KeyValuePair<ReceptacleSpawnPoint, float>>();

                foreach (ReceptacleSpawnPoint sp in spawnPoints) {
                    // calculate distance from potential spawn point to the agent's current x/z coordinate. Compare using the spawn point's y value so
                    // we compare distance as a flat plane parallel to the agent's x/z plane. This keeps things consistent regardless of agent camera
                    // position if the agent is crouching or standing
                    Vector3 tmp = new Vector3(transform.position.x, sp.Point.y, transform.position.z);
                    if (Vector3.Distance(sp.Point, tmp) < maxDistance) {
                        float dist = 0;
                        if (putNearXY) {
                            dist = Vector3.Distance(sp.Point, hit.point);
                        }
                        distSpawnPoints.Add(new KeyValuePair<ReceptacleSpawnPoint, float>(sp, dist));
                    }
                }

                // actually sort by distance closest to raycast hit if needed here, otherwise leave random
                if (putNearXY) {
                    distSpawnPoints.Sort((x, y) => (x.Value.CompareTo(y.Value)));
                } else {
                    distSpawnPoints.Shuffle_(randomSeed);
                }

                spawnPoints = new List<ReceptacleSpawnPoint>();  // populate a new spawnPoints list with sorted keys
                foreach (KeyValuePair<ReceptacleSpawnPoint, float> pair in distSpawnPoints) {
                    spawnPoints.Add(pair.Key);
                }
            }

            if (script.PlaceObjectReceptacle(spawnPoints, ItemInHand.GetComponent<SimObjPhysics>(), placeStationary, -1, 90, placeUpright)) {
                ItemInHand = null;
                DefaultAgentHand();
                actionFinished(true);
            } else {
                errorMessage = "No valid positions to place object found";
                actionFinished(false);
            }

            // #if UNITY_EDITOR
            // watch.Stop();
            // var elapsed = watch.ElapsedMilliseconds;
            // print("place object took: " + elapsed + "ms");
            // #endif
        }


        protected void pickupObject(
            SimObjPhysics target,
            bool forceAction,
            bool manualInteract,
            bool markActionFinished
        ) {
            if (target == null) {
                throw new ArgumentNullException();
            }

            // found non-pickupable object
            if (target.PrimaryProperty != SimObjPrimaryProperty.CanPickup) {

                throw new InvalidOperationException(target.objectID + " must have the property CanPickup to be picked up.");
            }

            // agent is holding something
            if (ItemInHand != null) {
                throw new InvalidOperationException("Agent hand has something in it already! Can't pick up anything else");
            }
            if (!IsHandDefault) {
                throw new InvalidOperationException("Must reset Hand to default position before attempting to Pick Up objects");
            }

            // save all initial values in case we need to reset on action fail
            Vector3 savedPos = target.transform.position;
            Quaternion savedRot = target.transform.rotation;
            Transform savedParent = target.transform.parent;

            // oh also save kinematic values in case we need to reset
            Rigidbody rb = target.GetComponent<Rigidbody>();
            bool wasKinematic = rb.isKinematic;

            // in preparation for object being held, force collision detection to discrete and make sure kinematic = true
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rb.isKinematic = true;

            // run this to pickup any contained objects if object is a receptacle
            // if the target is rotated too much, don't try to pick up any contained objects since they would fall out
            if (Vector3.Angle(target.transform.up, Vector3.up) < 60) {
                PickupContainedObjects(target);
            }

            if (!manualInteract) {
                // by default, abstract agent hand pickup so that object teleports to hand and changes orientation to match agent

                // agent's hand is in default position in front of camera, teleport object into agent's hand
                target.transform.position = AgentHand.transform.position;
                // target.transform.rotation = AgentHand.transform.rotation; - keep this line if we ever want to change the pickup position to be constant relative to the Agent Hand and Agent Camera rather than aligned by world axis
                target.transform.rotation = transform.rotation;
            } else {
                // in manualInteract mode, move the hand to the object, and require agent hand manipulation to move object around
                // or move closer to agent

                AgentHand.transform.position = target.transform.position;
                // don't rotate target at all as we are moving the hand to the object in manualInteract = True mode
            }

            target.transform.SetParent(AgentHand.transform);
            ItemInHand = target.gameObject;

            if (!forceAction && isHandObjectColliding(true) && !manualInteract) {
                // Undo picking up the object if the object is colliding with something after picking it up
                target.GetComponent<Rigidbody>().isKinematic = wasKinematic;
                target.transform.position = savedPos;
                target.transform.rotation = savedRot;
                target.transform.SetParent(savedParent);
                ItemInHand = null;
                target.DropContainedObjects(reparentContainedObjects: true, forceKinematic: false);
                throw new InvalidOperationException("Picking up object would cause it to collide and clip into something!");
            }

            // we have successfully picked up something!
            target.isInAgentHand = true;
            if (markActionFinished) {
                actionFinished(success: true, actionReturn: target.ObjectID);
            }
        }

        public virtual void PickupObject(float x, float y, bool forceAction = false, bool manualInteract = false) {
            SimObjPhysics target = getInteractableSimObjectFromXY(x: x, y: y, forceAction: forceAction);
            pickupObject(target: target, forceAction: forceAction, manualInteract: manualInteract, markActionFinished: true);
        }

        public virtual void PickupObject(string objectId, bool forceAction = false, bool manualInteract = false) {
            SimObjPhysics target = getInteractableSimObjectFromId(objectId: objectId, forceAction: forceAction);
            pickupObject(target: target, forceAction: forceAction, manualInteract: manualInteract, markActionFinished: true);
        }

        // make sure not to pick up any sliced objects because those should remain uninteractable i they have been sliced
        public void PickupContainedObjects(SimObjPhysics target) {
            if (target.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.Receptacle)) {
                foreach (SimObjPhysics sop in target.SimObjectsContainedByReceptacle) {
                    // for every object that is contained by this object...first make sure it's pickupable so we don't like, grab a Chair if it happened to be in the receptacle box or something
                    // turn off the colliders (so contained object doesn't block movement), leaving Trigger Colliders active (this is important to maintain visibility!)
                    if (sop.PrimaryProperty == SimObjPrimaryProperty.CanPickup) {
                        // wait! check if this object is sliceable and is sliced, if so SKIP!
                        if (sop.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanBeSliced)) {
                            // if this object is sliced, don't pick it up because it is effectively disabled
                            if (sop.GetComponent<SliceObject>().IsSliced()) {
                                target.RemoveFromContainedObjectReferences(sop);
                                break;
                            }
                        }

                        sop.transform.Find("Colliders").gameObject.SetActive(false);
                        Rigidbody soprb = sop.GetComponent<Rigidbody>();
                        soprb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                        soprb.isKinematic = true;
                        sop.transform.SetParent(target.transform);

                        // used to reference objects in the receptacle that is being picked up without having to search through all children
                        target.AddToContainedObjectReferences(sop);

                        target.GetComponent<SimObjPhysics>().isInAgentHand = true;// agent hand flag

                    }
                }
            }
        }



        // private IEnumerator checkDropHeldObjectAction(SimObjPhysics currentHandSimObj) 
        // {
        //     yield return null; // wait for two frames to pass
        //     yield return null;
        //     float startTime = Time.time;

        //     // if we can't find the currentHandSimObj's rigidbody because the object was destroyed, bypass this check
        //     if (currentHandSimObj != null)
        //     {
        //         Rigidbody rb = currentHandSimObj.GetComponentInChildren<Rigidbody>();
        //         while (Time.time - startTime < 2) 
        //         {
        //             if(currentHandSimObj == null)
        //             break;

        //             if (Math.Abs(rb.angularVelocity.sqrMagnitude + rb.velocity.sqrMagnitude) < 0.00001) 
        //             {
        //                 // Debug.Log ("object is now at rest");
        //                 break;
        //             } 

        //             else 
        //             {
        //                 // Debug.Log ("object is still moving");
        //                 yield return null;
        //             }
        //         }
        //     }

        //     DefaultAgentHand();
        //     actionFinished(true);
        // }

        private IEnumerator checkDropHeldObjectActionFast(SimObjPhysics currentHandSimObj) {
            if (currentHandSimObj != null) {
                Rigidbody rb = currentHandSimObj.GetComponentInChildren<Rigidbody>();
                Physics.autoSimulation = false;
                yield return null;

                for (int i = 0; i < 100; i++) {
                    PhysicsSceneManager.PhysicsSimulateTHOR(0.04f);
#if UNITY_EDITOR
                    yield return null;
#endif
                    if (Math.Abs(rb.angularVelocity.sqrMagnitude + rb.velocity.sqrMagnitude) < 0.00001) {
                        break;
                    }
                }
                Physics.autoSimulation = true;
            }

            DefaultAgentHand();
            actionFinished(true);
        }

        public void DropHeldObject(bool forceAction = false, bool autoSimulation = true) {
            // make sure something is actually in our hands
            if (ItemInHand == null) {
                throw new InvalidOperationException("Nothing is in the agent's hand to drop!");
            }

            // we do need this to check if the item is currently colliding with the agent, otherwise
            // dropping an object while it is inside the agent will cause it to shoot out weirdly
            if (!forceAction && isHandObjectColliding(false)) {
                throw new InvalidOperationException(
                    $"{ItemInHand.transform.name} can't be dropped. " +
                    "It must be clear of all other collision first, including the Agent."
                );
            }

            Rigidbody rb = ItemInHand.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.None;
            rb.useGravity = true;

            // change collision detection mode while falling so that obejcts don't phase through colliders.
            // this is reset to discrete on SimObjPhysics.cs's update 
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            GameObject topObject = GameObject.Find("Objects");
            if (topObject != null) {
                ItemInHand.transform.parent = topObject.transform;
            } else {
                ItemInHand.transform.parent = null;
            }
            ItemInHand.GetComponent<SimObjPhysics>().DropContainedObjects(reparentContainedObjects: true, forceKinematic: false);

            // if physics simulation has been paused by the PausePhysicsAutoSim() action,
            // don't do any coroutine checks
            if (!physicsSceneManager.physicsSimulationPaused) {
                // this is true by default
                if (autoSimulation) {
                    StartCoroutine(checkIfObjectHasStoppedMoving(ItemInHand.GetComponent<SimObjPhysics>(), 0));
                } else {
                    StartCoroutine(checkDropHeldObjectActionFast(ItemInHand.GetComponent<SimObjPhysics>()));
                }
            } else {
                DefaultAgentHand();
                actionFinished(true);
            }
            ItemInHand.GetComponent<SimObjPhysics>().isInAgentHand = false;
            ItemInHand = null;
        }

        [ObsoleteAttribute(message: "This action is deprecated. Call DropHeldObject instead.", error: false)]
        public void DropHandObject(bool forceAction = false, bool autoSimulation = true) {
            DropHeldObject(forceAction: forceAction, autoSimulation: autoSimulation);
        }

        // by default will throw in the forward direction relative to the Agent's Camera
        // moveMagnitude, strength of throw, good values for an average throw are around 150-250
        public void ThrowObject(float moveMagnitude, bool forceAction = false, bool autoSimulation = true) {
            if (ItemInHand == null) {
                throw new InvalidOperationException("Nothing in Hand to Throw!");
            }

            GameObject go = ItemInHand;
            DropHeldObject(forceAction: forceAction, autoSimulation: autoSimulation);

            // Force is not applied because action success from DropObject
            // starts a coroutine that waits for the object to be stationary
            // to return lastActionSuccess == true that is not what we want
            // for throwing an object, review why this was that way
            Vector3 dir = m_Camera.transform.forward;
            go.GetComponent<SimObjPhysics>().ApplyForce(dir, moveMagnitude);
        }

        // Hide and Seek helper function, makes overlap box at x,z coordinates
        protected HashSet<SimObjPhysics> objectsInBox(float x, float z) {
            Collider[] colliders = Physics.OverlapBox(
                new Vector3(x, 0f, z),
                new Vector3(0.125f, 10f, 0.125f),
                Quaternion.identity
            );
            HashSet<SimObjPhysics> toReturn = new HashSet<SimObjPhysics>();
            foreach (Collider c in colliders) {
                SimObjPhysics so = ancestorSimObjPhysics(c.transform.gameObject);
                if (so != null) {
                    toReturn.Add(so);
                }
            }
            return toReturn;
        }

        public void ObjectsInBox(float x, float z) {
            HashSet<SimObjPhysics> objects = objectsInBox(x, z);
            objectIdsInBox = new string[objects.Count];
            int i = 0;
            foreach (SimObjPhysics so in objects) {
                objectIdsInBox[i] = so.ObjectID;
                i++;
#if UNITY_EDITOR
                Debug.Log(so.ObjectID);
#endif
            }
            actionFinished(true);
        }

        // try and close all visible objects
        public void CloseVisibleObjects(bool simplifyPhysics = false) {
            OpenVisibleObjects(simplifyPhysics: simplifyPhysics, openness: 0);
        }

        // try and open all visible objects
        public void OpenVisibleObjects(bool simplifyPhysics = false, float openness = 1) {
            foreach (SimObjPhysics so in GetAllVisibleSimObjPhysics(m_Camera, maxVisibleDistance)) {
                CanOpen_Object coo = so.GetComponent<CanOpen_Object>();
                if (coo) {
                    // if object is open, add it to be closed.
                    if (!coo.isOpen) {
                        openObject(
                            target: so,
                            openness: openness,
                            forceAction: true,
                            markActionFinished: false,
                            simplifyPhysics: simplifyPhysics
                        );
                    }
                }
            }

            // While there are no objects to open, it was technically successful at opening all 0 objects.
            actionFinished(true);
        }

        // syntactic sugar for open object with openness = 0.
        public void CloseObject(
            string objectId,
            bool forceAction = false,
            bool returnToStart = true,
            bool ignoreAgentInTransition = false,
            bool stopAtNonStaticCol = false,
            float? physicsInterval = null,
            bool useGripper = false
            ) {
            OpenObject(
                objectId: objectId,
                openness: 0,
                forceAction: forceAction,
                returnToStart: returnToStart,
                ignoreAgentInTransition: ignoreAgentInTransition,
                stopAtNonStaticCol: stopAtNonStaticCol,
                physicsInterval: physicsInterval,
                useGripper: useGripper
                );
        }

        // syntactic sugar for open object with openness = 0.
        public void CloseObject(
            float x,
            float y,
            bool forceAction = false
        ) {
            OpenObject(x: x, y: y, forceAction: forceAction, openness: 0);
        }

        protected SimObjPhysics getOpenableOrCloseableObjectNearLocation(
            bool open, float x, float y, float radius, bool forceAction
        ) {
            y = 1.0f - y;

            RaycastHit hit;
            int layerMask = LayerMask.GetMask("SimObjVisible", "Procedural1", "Procedural2", "Procedural3", "Procedural0", "SimObjInvisible");
            if (ItemInHand != null) {
                foreach (Collider c in ItemInHand.GetComponentsInChildren<Collider>()) {
                    c.enabled = false;
                }
            }
            for (int i = 0; i < 10; i++) {
                float r = radius * (i / 9.0f);
                int n = 2 * i + 1;
                for (int j = 0; j < n; j++) {
                    float thetak = 2 * j * ((float)Math.PI) / n;

                    float newX = x + (float)(r * Math.Cos(thetak));
                    float newY = y + (float)(r * Math.Sin(thetak));
                    if (x < 0 || x > 1.0 || y < 0 || y > 1.0) {
                        continue;
                    }

                    Ray ray = m_Camera.ViewportPointToRay(new Vector3(newX, newY, 0.0f));
                    bool raycastDidHit = Physics.Raycast(ray, out hit, 10f, layerMask);

#if UNITY_EDITOR
                    if (raycastDidHit) {
                        Debug.DrawLine(ray.origin, hit.point, Color.red, 10f);
                    }
#endif

                    if (raycastDidHit) {
                        SimObjPhysics sop = ancestorSimObjPhysics(hit.transform.gameObject);
                        if (sop != null && sop.GetComponent<CanOpen_Object>() && (
                                forceAction || objectIsCurrentlyVisible(sop, maxVisibleDistance)
                            )) {
                            CanOpen_Object coo = sop.GetComponent<CanOpen_Object>();

                            if (open != coo.isOpen) {
                                if (ItemInHand != null) {
                                    foreach (Collider c in ItemInHand.GetComponentsInChildren<Collider>()) {
                                        c.enabled = true;
                                    }
                                }
                                return sop;
                            }
                        }
                    }
                }
            }
            if (ItemInHand != null) {
                foreach (Collider c in ItemInHand.GetComponentsInChildren<Collider>()) {
                    c.enabled = true;
                }
            }
            return null;
        }

        // H&S action
        private void OpenOrCloseObjectAtLocation(bool open, ServerAction action) {
            float x = action.x;
            float y = 1.0f - action.y;
            Ray ray = m_Camera.ViewportPointToRay(new Vector3(x, y, 0.0f));
            RaycastHit hit;
            int layerMask = LayerMask.GetMask("SimObjVisible", "Procedural1", "Procedural2", "Procedural3", "Procedural0", "SimObjInvisible");
            if (ItemInHand != null) {
                foreach (Collider c in ItemInHand.GetComponentsInChildren<Collider>()) {
                    c.enabled = false;
                }
            }
            bool raycastDidHit = Physics.Raycast(ray, out hit, 10f, layerMask);
            if (ItemInHand != null) {
                foreach (Collider c in ItemInHand.GetComponentsInChildren<Collider>()) {
                    c.enabled = true;
                }
            }
            if (!raycastDidHit) {
                Debug.Log("There don't seem to be any objects in that area.");
                errorMessage = "No openable object at location.";
                actionFinished(false);
                return;
            }
            SimObjPhysics so = ancestorSimObjPhysics(hit.transform.gameObject);
            if (so != null && (
                    action.forceAction || objectIsCurrentlyVisible(so, maxVisibleDistance)
                )) {
                if (open) {
                    OpenObject(objectId: so.ObjectID, forceAction: true);
                } else {
                    CloseObject(objectId: so.ObjectID, forceAction: true);
                }
            } else if (so == null) {
                errorMessage = "Object at location is not interactable.";
                actionFinished(false);
            } else {
                errorMessage = so.ObjectID + " is too far away.";
                actionFinished(false);
            }
        }

        // H&S action
        public void OpenObjectAtLocation(ServerAction action) {
            if (action.z > 0) {
                SimObjPhysics sop = getOpenableOrCloseableObjectNearLocation(
                    true, action.x, action.y, action.z, false
                );
                if (sop != null) {
                    OpenObject(objectId: sop.ObjectID, forceAction: true);
                } else {
                    errorMessage = "No openable object found within a radius about given point.";
                    actionFinished(false);
                }
            } else {
                OpenOrCloseObjectAtLocation(true, action);
            }
        }

        // H&S action
        public void CloseObjectAtLocation(ServerAction action) {
            OpenOrCloseObjectAtLocation(false, action);
        }

        // Helper used with OpenObject commands, which controls the physics
        // of actually opening an object. Instead of calling this directly,
        // one is recommended to call openObject, which runs more checks.
        // Previously named InteractAndWait.
        private protected IEnumerator openAnimation(
            CanOpen_Object openableObject,
            bool markActionFinished,
            float openness = 1.0f,
            bool forceAction = false,
            bool returnToStart = true,
            bool ignoreAgentInTransition = false,
            bool stopAtNonStaticCol = false,
            bool useGripper = false,
            float? physicsInterval = null,
            bool freezeContained = false,
            GameObject posRotManip = null,
            GameObject posRotRef = null
        ) {
            if (openableObject == null) {
                if (markActionFinished) {
                    errorMessage = "Must pass in openable object!";
                    actionFinished(false);
                }
                yield break;
            }

            // stores the object id of each object within this openableObject
            Dictionary<string, Transform> objectIdToOldParent = null;
            if (freezeContained) {
                SimObjPhysics target = ancestorSimObjPhysics(openableObject.gameObject);
                objectIdToOldParent = new Dictionary<string, Transform>();

                foreach (string objectId in target.GetAllSimObjectsInReceptacleTriggersByObjectID()) {
                    SimObjPhysics toReParent = physicsSceneManager.ObjectIdToSimObjPhysics[objectId];
                    objectIdToOldParent[objectId] = toReParent.transform.parent;
                    toReParent.transform.parent = openableObject.transform;
                    toReParent.GetComponent<Rigidbody>().isKinematic = true;
                }
            }
            
            // set conditions for ignoring certain fail-conditions or not
            // (must be stored on CanOpen_Object component for OnTriggerEnter event to work)
            openableObject.SetForceAction(forceAction);
            openableObject.SetIgnoreAgentInTransition(ignoreAgentInTransition);
            openableObject.SetStopAtNonStaticCol(stopAtNonStaticCol);

            // open the object to openness
            openableObject.SetIsCurrentlyLerping(true);
            openableObject.Interact(
                targetOpenness: openness,
                physicsInterval: physicsInterval,
                returnToStart: returnToStart,
                useGripper: useGripper,
                returnToStartMode: false,
                posRotManip: posRotManip,
                posRotRef: posRotRef);
            // Wait until all animating is done
            yield return new WaitUntil(() => (openableObject.GetIsCurrentlyLerping() == false));
            yield return null;
            bool succeeded = true;
            
            // if failure occurred, revert back to backup state (either start or lastSuccessful), and then report failure
            if (openableObject.GetFailState() != CanOpen_Object.failState.none) {
                succeeded = false;

                openableObject.SetIsCurrentlyLerping(true);
                openableObject.Interact(
                    targetOpenness: openableObject.GetStartOpenness(),
                    physicsInterval: physicsInterval,
                    returnToStart: returnToStart,
                    useGripper: useGripper,
                    returnToStartMode: true,
                    posRotManip: posRotManip,
                    posRotRef: posRotRef);
                yield return new WaitUntil(() => (openableObject.GetIsCurrentlyLerping() == false));
                yield return null;
                if (openableObject.GetFailState() == CanOpen_Object.failState.collision) {
                    errorMessage = "Openable object collided with " + openableObject.GetFailureCollision().name;
                }
                else if (openableObject.GetFailState() == CanOpen_Object.failState.hyperextension) {
                    errorMessage = "Agent hyperextended arm while opening object";
                }
            }

            // stops any object located within this openableObject from moving
            if (freezeContained) {
                foreach (string objectId in objectIdToOldParent.Keys) {
                    SimObjPhysics toReParent = physicsSceneManager.ObjectIdToSimObjPhysics[objectId];
                    toReParent.transform.parent = objectIdToOldParent[toReParent.ObjectID];
                    Rigidbody rb = toReParent.GetComponent<Rigidbody>();
                    rb.velocity = new Vector3(0f, 0f, 0f);
                    rb.angularVelocity = new Vector3(0f, 0f, 0f);
                    rb.isKinematic = false;
                }
            }

            // Reset conditions for next interaction
            openableObject.SetFailState(CanOpen_Object.failState.none);
            openableObject.SetFailureCollision(null);
            
            openableObject.SetForceAction(false);
            openableObject.SetIgnoreAgentInTransition(false);
            openableObject.SetStopAtNonStaticCol(false);

            // Remove PosRotRef from MovingPart
            if (posRotRef != null) {
                UnityEngine.Object.Destroy(posRotRef);
            }

            if (markActionFinished) {
                actionFinished(succeeded);
            }
        }

        // swap an object's materials out to the cooked version of the object
        public void CookObject(ServerAction action) {
            // specify target to pickup via objectId or coordinates
            if (action.forceAction) {
                action.forceVisible = true;
            }

            SimObjPhysics target = null;
            if (action.objectId == null) {
                target = getInteractableSimObjectFromXY(x: action.x, y: action.y, forceAction: action.forceAction);
            } else {
                target = getInteractableSimObjectFromId(objectId: action.objectId, forceAction: action.forceAction);
            }

            if (target) {
                if (!action.forceAction && !IsInteractable(target)) {
                    actionFinished(false);
                    errorMessage = "object is visible but occluded by something: " + action.objectId;
                    return;
                }

                if (target.GetComponent<CookObject>()) {
                    CookObject to = target.GetComponent<CookObject>();

                    // is this toasted already? if not, good to go
                    if (to.IsCooked()) {
                        actionFinished(false);
                        errorMessage = action.objectId + " is already Toasted!";
                        return;
                    }

                    to.Cook();

                    actionFinished(true);
                } else {
                    errorMessage = "target object is not cookable";
                    actionFinished(false);
                    return;
                }
            }

            // target not found in currently visible objects, report not found
            else {
                actionFinished(false);
                errorMessage = "object not found: " + action.objectId;
            }
        }

        // face change the agent's face screen to demonstrate different "emotion" states
        // for use with multi agent implicit communication
        public void ChangeAgentFaceToNeutral() {
            Material[] currentmats = MyFaceMesh.materials;

            currentmats[2] = ScreenFaces[0];

            MyFaceMesh.materials = currentmats;

            actionFinished(true);
        }

        public void ChangeAgentFaceToHappy() {
            Material[] currentmats = MyFaceMesh.materials;

            currentmats[2] = ScreenFaces[1];

            MyFaceMesh.materials = currentmats;

            actionFinished(true);
        }

        public void ChangeAgentFaceToMad() {
            Material[] currentmats = MyFaceMesh.materials;

            currentmats[2] = ScreenFaces[2];

            MyFaceMesh.materials = currentmats;

            actionFinished(true);
        }

        public void ChangeAgentFaceToSuperMad() {
            Material[] currentmats = MyFaceMesh.materials;

            currentmats[2] = ScreenFaces[3];

            MyFaceMesh.materials = currentmats;

            actionFinished(true);
        }

        public void ToggleObjectOn(string objectId, bool forceAction = false) {
            toggleObject(objectId, true, forceAction);
        }

        public void ToggleObjectOff(string objectId, bool forceAction = false) {
            toggleObject(objectId, false, forceAction);
        }

        public void ToggleObjectOn(float x, float y, bool forceAction = false) {
            toggleObject(x, y, true, forceAction);
        }

        public void ToggleObjectOff(float x, float y, bool forceAction = false) {
            toggleObject(x, y, false, forceAction);
        }

        private void toggleObject(float x, float y, bool toggleOn, bool forceAction) {
            SimObjPhysics target = getInteractableSimObjectFromXY(
                x: x, y: y, forceAction: forceAction
            );
            toggleObject(target, toggleOn, forceAction);
        }

        private void toggleObject(string objectId, bool toggleOn, bool forceAction) {
            SimObjPhysics target = null;

            // if object is in the scene and visible, assign it to 'target'
            target = getInteractableSimObjectFromId(objectId: objectId, forceAction: forceAction);

            if (!target) {

                // target not found in currently visible objects, report not found
                errorMessage = "object not found: " + objectId;
                actionFinished(false);
                return;
            }

            toggleObject(target, toggleOn, forceAction);
        }

        // specific ToggleObject that is used for SetObjectStatesForLotsOfObjects
        private IEnumerator toggleObject(SimObjPhysics target, bool toggleOn) {
            if (target.GetComponent<CanToggleOnOff>()) {
                // get CanToggleOnOff component from target
                CanToggleOnOff ctof = target.GetComponent<CanToggleOnOff>();

                if (!ctof.ReturnSelfControlled()) {
                    yield break;
                }

                // if the object is already in the state specified by the toggleOn bool, do nothing
                if (ctof.isOn == toggleOn) {
                    yield break;
                }

                // if object needs to be closed to turn on...
                if (toggleOn && ctof.ReturnMustBeClosedToTurnOn().Contains(target.Type)) {
                    // if the object is open and we are trying to turn it on, do nothing because it can't
                    if (target.GetComponent<CanOpen_Object>().isOpen) {
                        yield break;
                    }
                }

                ctof.Toggle();
            }
        }

        private bool toggleObject(SimObjPhysics target, bool toggleOn, bool forceAction) {
            if (!forceAction && !IsInteractable(target)) {
                errorMessage = "object is visible but occluded by something: " + target.ObjectID;
                actionFinished(false);
                return false;
            }

            if (target.GetComponent<CanToggleOnOff>()) {
                CanToggleOnOff ctof = target.GetComponent<CanToggleOnOff>();

                if (!ctof.ReturnSelfControlled()) {
                    errorMessage = "target object is controlled by another sim object. target object cannot be turned on/off directly";
                    actionFinished(false);
                    return false;
                }

                // check to make sure object is in other state
                if (ctof.isOn == toggleOn) {
                    if (ctof.isOn) {
                        errorMessage = "can't toggle object on if it's already on!";
                    } else {
                        errorMessage = "can't toggle object off if it's already off!";
                    }

                    actionFinished(false);
                    return false;
                }
                // check if this object needs to be closed in order to turn on
                if (toggleOn && ctof.ReturnMustBeClosedToTurnOn().Contains(target.Type)) {
                    if (target.GetComponent<CanOpen_Object>().isOpen) {
                        errorMessage = "Target must be closed to Toggle On!";
                        actionFinished(false);
                        return false;
                    }
                }

                // check if this object is broken, it should not be able to be turned on
                if (toggleOn && target.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanBreak)) {
                    // if this breakable object is broken, we can't turn it on
                    if (target.IsBroken) {
                        errorMessage = "Target is broken and cannot be Toggled On!";
                        actionFinished(false);
                        return false;
                    }

                }

                // interact then wait
                StartCoroutine(ToggleAndWait(ctof));
                return true;

            } else {
                errorMessage = "object is not toggleable.";
                actionFinished(false);
                return false;
            }
        }

        protected IEnumerator ToggleAndWait(CanToggleOnOff ctof) {
            bool ctofInitialState = ctof.isOn;

            if (ctof != null) {
                ctof.Toggle();
            }

            bool success = false;


            yield return new WaitUntil(() => (ctof != null && ctof.GetIsCurrentlyLerping() == false && ctof.isOn == !ctofInitialState));
            success = true;

            if (!success) {
                errorMessage = "object could not be toggled on/off succesfully";
            }

            // only return ActionFinished once the object is completely done animating.
            // print(ctof.isOn);
            actionFinished(success);
        }

        // private helper used with OpenObject commands
        private void openObject(
            SimObjPhysics target,
            float openness,
            bool forceAction,
            bool markActionFinished,
            bool returnToStart = true,
            bool ignoreAgentInTransition = false,
            bool stopAtNonStaticCol = false,
            bool useGripper = false,
            float? physicsInterval = null,
            bool simplifyPhysics = false,
            float? moveMagnitude = null // moveMagnitude is supported for backwards compatibility. It's new name is 'openness'.
        ) {
            // backwards compatibility support
            if (moveMagnitude != null) {
                // Previously, when moveMagnitude==0, that meant full openness, since the default float was 0.
                openness = ((float)moveMagnitude) == 0 ? 1 : (float)moveMagnitude;
            }

            if (openness > 1 || openness < 0) {
                errorMessage = "openness must be in [0:1]";
                if (markActionFinished) {
                    actionFinished(false);
                }
                return;
            }

            if (target == null) {
                errorMessage = "Object not found!";
                if (markActionFinished) {
                    actionFinished(false);
                }
                return;
            }

            if (!forceAction && !IsInteractable(target)) {
                errorMessage = "object is visible but occluded by something: " + target.ObjectID;
                if (markActionFinished) {
                    actionFinished(false);
                }
                return;
            }

            if (!target.GetComponent<CanOpen_Object>()) {
                errorMessage = $"{target.ObjectID} is not an Openable object";
                if (markActionFinished) {
                    actionFinished(false);
                }
                return;
            }

            CanOpen_Object codd = target.GetComponent<CanOpen_Object>();

            // This is a style choice that applies to Microwaves and Laptops,
            // where it doesn't make a ton of sense to open them, while they are in use.
            if (codd.WhatReceptaclesMustBeOffToOpen().Contains(target.Type) && target.GetComponent<CanToggleOnOff>().isOn) {
                errorMessage = "Target must be OFF to open!";
                if (markActionFinished) {
                    actionFinished(false);
                }
                return;
            }

            if (useGripper == true) {
                // Opening objects with the gripper only works with the IK-Arm
                // (it'd be preferable to reference the agentMode directly, but no such universal metadata exists)
                if (this.GetComponent<BaseAgentComponent>().IKArm.activeInHierarchy == false) {
                    errorMessage = "IK-Arm is required to open objects with gripper";
                    if (markActionFinished) {
                        actionFinished(false);
                    }
                    return;
                }
                
                // Opening objects with the gripper only works with an empty hand
                if (ItemInHand != null) {
                    errorMessage = "An empty hand is required to open objects with gripper";
                    if (markActionFinished) {
                        actionFinished(false);
                    }
                    return;
                }

                // Opening objects with the gripper currently requires the gripper-sphere to have some overlap with the moving parts' collision geometry
                GameObject parentMovingPart = FindOverlappingMovingPart(codd);
                if (parentMovingPart == null) {
                    errorMessage = "Gripper must be making contact with at least one moving part's surface to open it!";
                    if (markActionFinished) {
                        actionFinished(false);
                    }
                    return;
                }

                GameObject posRotManip = this.GetComponent<BaseAgentComponent>().IKArm.GetComponent<IK_Robot_Arm_Controller>().GetArmTarget();
                GameObject posRotRef = new GameObject("PosRotReference");
                posRotRef.transform.parent = parentMovingPart.transform;
                posRotRef.transform.position = posRotManip.transform.position;
                posRotRef.transform.rotation = posRotManip.transform.rotation;
                StartCoroutine(openAnimation(
                    openableObject: codd,
                    markActionFinished: markActionFinished,
                    openness: openness,
                    forceAction: forceAction,
                    returnToStart: returnToStart,
                    ignoreAgentInTransition: ignoreAgentInTransition,
                    stopAtNonStaticCol: stopAtNonStaticCol,
                    useGripper: useGripper,
                    physicsInterval: physicsInterval,
                    freezeContained: simplifyPhysics,
                    posRotManip: posRotManip,
                    posRotRef: posRotRef
                ));
                return;
            }

            StartCoroutine(openAnimation(
                openableObject: codd,
                markActionFinished: markActionFinished,
                openness: openness,
                forceAction: forceAction,
                returnToStart: returnToStart,
                ignoreAgentInTransition: ignoreAgentInTransition,
                stopAtNonStaticCol: stopAtNonStaticCol,
                useGripper: useGripper,
                physicsInterval: physicsInterval,
                freezeContained: simplifyPhysics
            ));
        }

        //helper action to set the openness of a "rotate" typed open/close object immediately (no tween over time)
        public void OpenObjectImmediate(
            string objectId,
            float openness = 1.0f
        ) {
            SimObjPhysics target = getInteractableSimObjectFromId(objectId: objectId, forceAction: true);
            target.GetComponent<CanOpen_Object>().SetOpennessImmediate(openness);
            actionFinished(true);
        }
        
        //helper function to confirm if GripperSphere overlaps with openable object's moving part(s), and which one
    
        public GameObject FindOverlappingMovingPart(CanOpen_Object codd)
        {
            int layerMask = 1 << 8;
            GameObject magnetSphere = this.GetComponent<BaseAgentComponent>().IKArm.GetComponent<IK_Robot_Arm_Controller>().GetMagnetSphere();
            foreach (GameObject movingPart in codd.GetComponent<CanOpen_Object>().MovingParts) {
                foreach(Collider col in movingPart.GetComponentsInChildren<Collider>()) {
                    // Checking for matches between moving parts' colliders and colliders inside of gripper-sphere
                    foreach(Collider containedCol in Physics.OverlapSphere(
                        magnetSphere.transform.TransformPoint(magnetSphere.GetComponent<SphereCollider>().center),
                        magnetSphere.transform.GetComponent<SphereCollider>().radius,
                        layerMask)) {
                            if (col == containedCol)
                            {
                                return movingPart;
                            }
                    }
                }
            }
            return null;
        }

        public void OpenObject(
            string objectId,
            float openness = 1,
            bool forceAction = false,
            bool returnToStart = true,
            bool ignoreAgentInTransition = false,
            bool stopAtNonStaticCol = false,
            float? physicsInterval = null,
            bool useGripper = false,
            float? moveMagnitude = null // moveMagnitude is supported for backwards compatibility. Its new name is 'openness'.
        ) {
            SimObjPhysics target = getInteractableSimObjectFromId(objectId: objectId, forceAction: forceAction);
            openObject(
                target: target,
                openness: openness,
                forceAction: forceAction,
                returnToStart: returnToStart,
                ignoreAgentInTransition: ignoreAgentInTransition,
                stopAtNonStaticCol: stopAtNonStaticCol,
                useGripper: useGripper,
                physicsInterval: physicsInterval,
                moveMagnitude: moveMagnitude,
                markActionFinished: true
            );
        }

        public void OpenObject(
            float x,
            float y,
            bool forceAction = false,
            float openness = 1,
            float? moveMagnitude = null // moveMagnitude is supported for backwards compatibility. It's new name is 'openness'.
        ) {
            SimObjPhysics target = getInteractableSimObjectFromXY(
                x: x, y: y, forceAction: forceAction
            );
            openObject(
                target: target,
                openness: openness,
                forceAction: forceAction,
                moveMagnitude: moveMagnitude,
                markActionFinished: true
            );
        }

        // XXX: This will return contained objects, but should not be used. Likely depracate this later
        public void Contains(ServerAction action) {
            if (action.objectId == null) {
                errorMessage = "Hey, actually give me an object ID check containment for, yeah?";
                actionFinished(false);
                return;
            }

            SimObjPhysics target = getInteractableSimObjectFromId(objectId: action.objectId, forceAction: action.forceAction);

            if (target) {
                List<string> ids = target.GetAllSimObjectsInReceptacleTriggersByObjectID();

#if UNITY_EDITOR
                foreach (string s in ids) {
                    Debug.Log(s);
                }
#endif

                actionFinished(true, ids.ToArray());
            } else {
                errorMessage = "object not found: " + action.objectId;
                actionFinished(false);
            }
        }

        // override for SimpleSimObj?
        // public override SimpleSimObj[] VisibleSimObjs() 
        // {
        //     return GetAllVisibleSimObjPhysics(m_Camera, maxVisibleDistance);
        // }

        ////////////////////////////////////////
        ////// HIDING AND MASKING OBJECTS //////
        ////////////////////////////////////////

        private Dictionary<int, Material[]> maskedGameObjectDict = new Dictionary<int, Material[]>();
        private void maskGameObject(GameObject go, Material mat) {
            if (go.name == "Objects" || go.name == "Structure") {
                return;
            }
            foreach (MeshRenderer r in go.GetComponentsInChildren<MeshRenderer>() as MeshRenderer[]) {
                int id = r.GetInstanceID();
                if (!maskedGameObjectDict.ContainsKey(id)) {
                    maskedGameObjectDict[id] = r.materials;
                }

                Material[] newMaterials = new Material[r.materials.Length];
                for (int i = 0; i < newMaterials.Length; i++) {
                    newMaterials[i] = new Material(mat);
                }
                r.materials = newMaterials;
            }
        }

        private void unmaskGameObject(GameObject go) {
            foreach (MeshRenderer r in go.GetComponentsInChildren<MeshRenderer>() as MeshRenderer[]) {
                int id = r.GetInstanceID();
                if (maskedGameObjectDict.ContainsKey(id)) {
                    r.materials = maskedGameObjectDict[id];
                    maskedGameObjectDict.Remove(id);
                }
            }
        }

        public void MaskMovingParts() {
            Material openMaterial = new Material(Shader.Find("Unlit/Color"));
            openMaterial.color = Color.magenta;
            Material closedMaterial = new Material(Shader.Find("Unlit/Color"));
            closedMaterial.color = Color.blue;
            Material otherMaterial = new Material(Shader.Find("Unlit/Color"));
            otherMaterial.color = Color.green;

            foreach (GameObject go in GameObject.FindObjectsOfType<GameObject>()) {
                maskGameObject(go, otherMaterial);
            }

            foreach (CanOpen_Object coo in GameObject.FindObjectsOfType<CanOpen_Object>()) {
                Material m;
                if (coo.isOpen) {
                    m = openMaterial;
                } else {
                    m = closedMaterial;
                }
                foreach (GameObject go in coo.MovingParts) {
                    maskGameObject(go, m);
                }
            }
            actionFinished(true);
        }

        public void UnmaskMovingParts() {
            foreach (GameObject go in GameObject.FindObjectsOfType<GameObject>()) {
                unmaskGameObject(go);
            }
            // foreach (CanOpen_Object coo in GameObject.FindObjectsOfType<CanOpen_Object>()) {
            //     foreach (GameObject go in coo.MovingParts) {
            //         unmaskGameObject(go);
            //     }
            // }
            actionFinished(true);
        }

        private void HideAll() {
            foreach (GameObject go in GameObject.FindObjectsOfType<GameObject>()) {
                UpdateDisplayGameObject(go, false);
            }
        }

        private void UnhideAll() {
            foreach (GameObject go in GameObject.FindObjectsOfType<GameObject>()) {
                UpdateDisplayGameObject(go, true);
            }
            // Making sure the agents visibility capsules are not incorrectly unhidden
            foreach (BaseFPSAgentController agent in this.agentManager.agents) {
                agent.IsVisible = agent.IsVisible;
            }
        }

        public void HideAllObjectsExcept(ServerAction action) {
            foreach (GameObject go in UnityEngine.Object.FindObjectsOfType<GameObject>()) {
                UpdateDisplayGameObject(go, false);
            }
            if (physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(action.objectId)) {
                UpdateDisplayGameObject(physicsSceneManager.ObjectIdToSimObjPhysics[action.objectId].gameObject, true);
            }
            actionFinished(true);
        }

        public void HideTranslucentObjects() {
            foreach (SimObjPhysics sop in GameObject.FindObjectsOfType<SimObjPhysics>()) {
                if (sop.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanSeeThrough)) {
                    UpdateDisplayGameObject(sop.gameObject, false);
                }
            }
            actionFinished(true);
        }

        public void HideTransparentStructureObjects() {
            transparentStructureObjectsHidden = true;

            GameObject structObj = GameObject.Find("Structure");
            GameObject lightObj = GameObject.Find("Lighting");

            List<Renderer> renderers = new List<Renderer>();
            if (structObj != null) {
                renderers.AddRange(structObj.GetComponentsInChildren<Renderer>());
            }
            if (lightObj != null) {
                renderers.AddRange(lightObj.GetComponentsInChildren<Renderer>());
            }
            // renderers.AddRange(GameObject.FindObjectsOfType<Renderer>());

            foreach (Renderer r in renderers) {
                bool transparent = true;
                foreach (Material m in r.materials) {
                    if (
                        !(m.IsKeywordEnabled("_ALPHATEST_ON") ||
                          m.IsKeywordEnabled("_ALPHABLEND_ON") ||
                          m.IsKeywordEnabled("_ALPHAPREMULTIPLY_ON")
                        ) || m.color.a == 1.0f
                        ) {
                        transparent = false;
                        break;
                    }
                }
                if (transparent) {
                    UpdateDisplayGameObject(r.gameObject, false);
                }
            }
        }

        public void UnhideStructureObjects() {
            transparentStructureObjectsHidden = false;

            GameObject structObj = GameObject.Find("Structure");
            GameObject lightObj = GameObject.Find("Lighting");

            List<Transform> transforms = new List<Transform>();
            if (structObj != null) {
                transforms.AddRange(structObj.GetComponentsInChildren<Transform>());
            }
            if (lightObj != null) {
                transforms.AddRange(lightObj.GetComponentsInChildren<Transform>());
            }

            foreach (Transform transform in transforms) {
                UpdateDisplayGameObject(transform.gameObject, true);
            }
        }

        public void HideBlueObjects(ServerAction action) {
            foreach (Renderer r in UnityEngine.Object.FindObjectsOfType<Renderer>()) {
                foreach (Material m in r.materials) {
                    if (m.name.Contains("BLUE")) {
                        r.enabled = false;
                        break;
                    }
                }
            }

            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>()) {
                if (go.name.Contains("BlueCube")) {
                    UpdateDisplayGameObject(go, true);
                }
            }
            actionFinished(true);
        }

        public void GetAwayFromObject(ServerAction action) {
            if (physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(action.objectId)) {
                SimObjPhysics sop = physicsSceneManager.ObjectIdToSimObjPhysics[action.objectId];
                int k = 0;
                while (isAgentCapsuleCollidingWith(sop.gameObject) && k < 20) {
                    k++;
                    Vector3[] dirs = {
                        transform.forward, -transform.forward, transform.right, -transform.right
                    };
                    dirs.Shuffle_(action.randomSeed);

                    sop.gameObject.SetActive(false);
                    moveInDirection(dirs[0] * gridSize);
                    sop.gameObject.SetActive(true);
                }
                if (isAgentCapsuleCollidingWith(sop.gameObject)) {
                    errorMessage = "Could not get away from " + sop.ObjectID;
                    actionFinished(false);
                    return;
                }
                actionFinished(true);
            } else {
                errorMessage = "No object with given id could be found to disable collisions with.";
                actionFinished(false);
            }
        }


        public void DisableObjectCollisionWithAgent(ServerAction action) {
            if (physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(action.objectId)) {
                SimObjPhysics sop = physicsSceneManager.ObjectIdToSimObjPhysics[action.objectId];
                foreach (Collider c0 in this.GetComponentsInChildren<Collider>()) {
                    foreach (Collider c1 in sop.GetComponentsInChildren<Collider>()) {
                        Physics.IgnoreCollision(c0, c1);
                    }
                }
                foreach (Collider c1 in sop.GetComponentsInChildren<Collider>()) {
                    collidersToIgnoreDuringMovement.Add(c1);
                }
                actionFinished(true);
            } else {
                errorMessage = "No object with given id could be found to disable collisions with.";
                actionFinished(false);
            }
        }

        public void HideObject(string objectId, bool hideContained = true) {
            if (physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(objectId)) {
                SimObjPhysics sop = physicsSceneManager.ObjectIdToSimObjPhysics[objectId];
                if (hideContained && !ReceptacleRestrictions.SpawnOnlyOutsideReceptacles.Contains(sop.ObjType)) {
                    foreach (SimObjPhysics containedSop in sop.SimObjectsContainedByReceptacle) {
                        UpdateDisplayGameObject(containedSop.gameObject, false);
                    }
                }
                UpdateDisplayGameObject(sop.gameObject, false);
                sop.GetAllSimObjectsInReceptacleTriggersByObjectID();

                actionFinished(true);
            } else {
                errorMessage = "No object with given id could be found to hide.";
                actionFinished(false);
            }
        }

        public void UnhideObject(string objectId, bool unhideContained = true) {
            if (physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(objectId)) {
                SimObjPhysics sop = physicsSceneManager.ObjectIdToSimObjPhysics[objectId];
                if (unhideContained && !ReceptacleRestrictions.SpawnOnlyOutsideReceptacles.Contains(sop.ObjType)) {
                    foreach (SimObjPhysics containedSop in sop.SimObjectsContainedByReceptacle) {
                        UpdateDisplayGameObject(containedSop.gameObject, true);
                    }
                }
                UpdateDisplayGameObject(sop.gameObject, true);
                actionFinished(true);
            } else {
                errorMessage = "No object with given id could be found to unhide.";
                actionFinished(false);
            }
        }

        public void HideAllObjects(ServerAction action) {
            HideAll();
            actionFinished(true);
        }

        public void UnhideAllObjects(ServerAction action) {
            transparentStructureObjectsHidden = false;
            UnhideAll();
            actionFinished(true);
        }

        protected void MaskSimObj(SimObjPhysics so, Material mat) {
            if (!transparentStructureObjectsHidden) {
                HideTransparentStructureObjects();
            }
            HashSet<MeshRenderer> renderersToSkip = new HashSet<MeshRenderer>();
            foreach (SimObjPhysics childSo in so.GetComponentsInChildren<SimObjPhysics>()) {
                if (so.ObjectID != childSo.ObjectID) {
                    foreach (MeshRenderer mr in childSo.GetComponentsInChildren<MeshRenderer>()) {
                        renderersToSkip.Add(mr);
                    }
                }
            }
            Dictionary<int, Material[]> dict = new Dictionary<int, Material[]>();
            foreach (MeshRenderer r in so.gameObject.GetComponentsInChildren<MeshRenderer>() as MeshRenderer[]) {
                if (!renderersToSkip.Contains(r)) {
                    dict[r.GetInstanceID()] = r.materials;
                    Material[] newMaterials = new Material[r.materials.Length];
                    for (int i = 0; i < newMaterials.Length; i++) {
                        newMaterials[i] = new Material(mat);
                    }
                    r.materials = newMaterials;
                }
            }
            if (!maskedObjects.ContainsKey(so.ObjectID)) {
                maskedObjects[so.ObjectID] = dict;
            }
        }

        protected void MaskSimObj(SimObjPhysics so, Color color) {
            if (!transparentStructureObjectsHidden) {
                HideTransparentStructureObjects();
            }
            Material material = new Material(Shader.Find("Unlit/Color"));
            material.color = color;
            MaskSimObj(so, material);
        }

        protected void UnmaskSimObj(SimObjPhysics so) {
            if (transparentStructureObjectsHidden) {
                UnhideStructureObjects();
            }

            if (maskedObjects.ContainsKey(so.ObjectID)) {
                foreach (MeshRenderer r in so.gameObject.GetComponentsInChildren<MeshRenderer>() as MeshRenderer[]) {
                    if (r != null) {
                        if (maskedObjects[so.ObjectID].ContainsKey(r.GetInstanceID())) {
                            r.materials = maskedObjects[so.ObjectID][r.GetInstanceID()];
                        }
                    }
                }
                maskedObjects.Remove(so.ObjectID);
            }
        }

        public void EmphasizeObject(ServerAction action) {
#if UNITY_EDITOR
            foreach (KeyValuePair<string, SimObjPhysics> entry in physicsSceneManager.ObjectIdToSimObjPhysics) {
                Debug.Log(entry.Key);
                Debug.Log(entry.Key == action.objectId);
            }
#endif

            if (physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(action.objectId)) {
                HideAll();
                UpdateDisplayGameObject(physicsSceneManager.ObjectIdToSimObjPhysics[action.objectId].gameObject, true);
                MaskSimObj(physicsSceneManager.ObjectIdToSimObjPhysics[action.objectId], Color.magenta);
                actionFinished(true);
            } else {
                errorMessage = "No object with id: " + action.objectId;
                actionFinished(false);
            }
        }

        public void UnemphasizeAll() {
            UnhideAll();
            foreach (SimObjPhysics so in GameObject.FindObjectsOfType<SimObjPhysics>()) {
                UnmaskSimObj(so);
            }
            actionFinished(true);
        }

        public void MaskObject(ServerAction action) {
            if (physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(action.objectId)) {
                MaskSimObj(physicsSceneManager.ObjectIdToSimObjPhysics[action.objectId], Color.magenta);
                actionFinished(true);
            } else {
                errorMessage = "No such object with id: " + action.objectId;
                actionFinished(false);
            }
        }

        public void UnmaskObject(ServerAction action) {
            if (physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(action.objectId)) {
                UnmaskSimObj(physicsSceneManager.ObjectIdToSimObjPhysics[action.objectId]);
                actionFinished(true);
            } else {
                errorMessage = "No such object with id: " + action.objectId;
                actionFinished(false);
            }
        }

        ///////////////////////////////////////////
        ///// GETTING DISTANCES, NORMALS, ETC /////
        ///////////////////////////////////////////

        private bool NormalIsApproximatelyUp(Vector3 normal, float tol = 10f) {
            return Vector3.Angle(transform.up, normal) < tol;
        }

        private bool AnythingAbovePosition(Vector3 position, float distance) {
            Vector3 up = new Vector3(0.0f, 1.0f, 0.0f);
            RaycastHit hit;
            return Physics.Raycast(position, up, out hit, distance);
        }

        private bool AnythingAbovePositionIgnoreObject(
            Vector3 position,
            float distance,
            int layerMask,
            GameObject toIgnore) {
            Vector3 up = new Vector3(0.0f, 1.0f, 0.0f);
            RaycastHit[] hits = Physics.RaycastAll(position, up, distance, layerMask);
            foreach (RaycastHit hit in hits) {
                if (hit.collider.transform.gameObject != toIgnore) {
                    return true;
                }
            }
            return false;
        }

        private float[,,] initializeFlatSurfacesOnGrid(int yGridSize, int xGridSize) {
            float[,,] flatSurfacesOnGrid = new float[2, yGridSize, xGridSize];
            for (int i = 0; i < 2; i++) {
                for (int j = 0; j < yGridSize; j++) {
                    for (int k = 0; k < xGridSize; k++) {
                        flatSurfacesOnGrid[i, j, k] = float.PositiveInfinity;
                    }
                }
            }
            return flatSurfacesOnGrid;
        }

        private void toggleColliders(IEnumerable<Collider> colliders) {
            foreach (Collider c in colliders) {
                c.enabled = !c.enabled;
            }
        }

        public void FlatSurfacesOnGrid(ServerAction action) {
            int xGridSize = (int)Math.Round(action.x, 0);
            int yGridSize = (int)Math.Round(action.y, 0);
            flatSurfacesOnGrid = initializeFlatSurfacesOnGrid(yGridSize, xGridSize);

            if (ItemInHand != null) {
                toggleColliders(ItemInHand.GetComponentsInChildren<Collider>());
            }

            int layerMask = LayerMask.GetMask("SimObjVisible", "Procedural1", "Procedural2", "Procedural3", "Procedural0");
            for (int i = 0; i < yGridSize; i++) {
                for (int j = 0; j < xGridSize; j++) {
                    float x = j * (1.0f / xGridSize) + (0.5f / xGridSize);
                    float y = (1.0f - (0.5f / yGridSize)) - i * (1.0f / yGridSize);
                    Ray ray = m_Camera.ViewportPointToRay(new Vector3(x, y, 0));
                    RaycastHit[] hits = Physics.RaycastAll(ray, 10f, layerMask);
                    float minHitDistance = float.PositiveInfinity;
                    foreach (RaycastHit hit in hits) {
                        if (hit.distance < minHitDistance) {
                            minHitDistance = hit.distance;
                        }
                    }
                    foreach (RaycastHit hit in hits) {
                        if (NormalIsApproximatelyUp(hit.normal) &&
                            !AnythingAbovePosition(hit.point, 0.1f)) {
                            if (hit.distance == minHitDistance) {
                                flatSurfacesOnGrid[0, i, j] = minHitDistance;
                            } else {
                                flatSurfacesOnGrid[1, i, j] = Math.Min(
                                    flatSurfacesOnGrid[1, i, j], hit.distance
                                );
                            }
                        }
                    }
                }
            }
            if (ItemInHand != null) {
                toggleColliders(ItemInHand.GetComponentsInChildren<Collider>());
            }
            actionFinished(true);
        }

        public void GetMetadataOnGrid(ServerAction action) {
            int xGridSize = (int)Math.Round(action.x, 0);
            int yGridSize = (int)Math.Round(action.y, 0);
            distances = new float[yGridSize, xGridSize];
            normals = new float[3, yGridSize, xGridSize];
            isOpenableGrid = new bool[yGridSize, xGridSize];

            if (ItemInHand != null) {
                toggleColliders(ItemInHand.GetComponentsInChildren<Collider>());
            }

            int layerMask = LayerMask.GetMask("SimObjVisible", "Procedural1", "Procedural2", "Procedural3", "Procedural0");
            for (int i = 0; i < yGridSize; i++) {
                for (int j = 0; j < xGridSize; j++) {
                    float x = j * (1.0f / xGridSize) + (0.5f / xGridSize);
                    float y = (1.0f - (0.5f / yGridSize)) - i * (1.0f / yGridSize);
                    Ray ray = m_Camera.ViewportPointToRay(new Vector3(x, y, 0));
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, 10f, layerMask)) {
                        distances[i, j] = hit.distance;
                        normals[0, i, j] = Vector3.Dot(transform.right, hit.normal);
                        normals[1, i, j] = Vector3.Dot(transform.up, hit.normal);
                        normals[2, i, j] = Vector3.Dot(transform.forward, hit.normal);
                        SimObjPhysics so = hit.transform.gameObject.GetComponent<SimObjPhysics>();
                        isOpenableGrid[i, j] = so != null && (so.GetComponent<CanOpen_Object>());
                    } else {
                        distances[i, j] = float.PositiveInfinity;
                        normals[0, i, j] = float.NaN;
                        normals[1, i, j] = float.NaN;
                        normals[2, i, j] = float.NaN;
                        isOpenableGrid[i, j] = false;
                    }
                }
            }

            if (ItemInHand != null) {
                toggleColliders(ItemInHand.GetComponentsInChildren<Collider>());
            }
            actionFinished(true);
        }

        public void SegmentVisibleObjects() {
            if (ItemInHand != null) {
                toggleColliders(ItemInHand.GetComponentsInChildren<Collider>());
            }

            int k = 0;
            List<string> objectIds = new List<string>();
            foreach (SimObjPhysics so in GetAllVisibleSimObjPhysics(m_Camera, 100f)) {
                int i = (10 * k) / 256;
                int j = (10 * k) % 256;
                MaskSimObj(so, new Color32(Convert.ToByte(i), Convert.ToByte(j), 255, 255));
                objectIds.Add(so.ObjectID);
                k++;
            }
            segmentedObjectIds = objectIds.ToArray();

            if (ItemInHand != null) {
                toggleColliders(ItemInHand.GetComponentsInChildren<Collider>());
            }
            actionFinished(true);
        }

        ////////////////////////////
        ///// Crouch and Stand /////
        ////////////////////////////
        public void crouch() {
            m_Camera.transform.localPosition = new Vector3(
                standingLocalCameraPosition.x,
                crouchingLocalCameraPosition.y,
                standingLocalCameraPosition.z
            );
        }

        public void stand() {
            m_Camera.transform.localPosition = standingLocalCameraPosition;
        }

        public void Crouch() {
            if (!isStanding()) {
                errorMessage = "Already crouching.";
                actionFinished(false);
            } else if (!CheckIfItemBlocksAgentStandOrCrouch()) {
                actionFinished(false);
            } else {
                m_Camera.transform.localPosition = new Vector3(
                    standingLocalCameraPosition.x,
                    crouchingLocalCameraPosition.y,
                    standingLocalCameraPosition.z
                );
                actionFinished(true);
            }
        }

        public void Stand() {
            if (isStanding()) {
                errorMessage = "Already standing.";
                actionFinished(false);
            } else if (!CheckIfItemBlocksAgentStandOrCrouch()) {
                actionFinished(false);
            } else {
                m_Camera.transform.localPosition = standingLocalCameraPosition;
                actionFinished(true);
            }
        }

        ////////////////
        ///// MISC /////
        ////////////////

        public void RotateUniverseAroundAgent(ServerAction action) {
            agentManager.RotateAgentsByRotatingUniverse(action.rotation.y);
            actionFinished(true);
        }

        public void ChangeFOV(ServerAction action) {

            if (action.fieldOfView > 0 && action.fieldOfView < 180) {
                m_Camera.fieldOfView = action.fieldOfView;
                actionFinished(true);
            } else {
                errorMessage = "fov must be in (0, 180) noninclusive.";
                Debug.Log(errorMessage);
                actionFinished(false);
            }

        }

        // in case you want to change the timescale
        public void ChangeTimeScale(ServerAction action) {
            if (action.timeScale > 0) {
                Time.timeScale = action.timeScale;
                actionFinished(true);
            } else {
                errorMessage = "Time scale must be >0";
                actionFinished(false);
            }
        }

        ///////////////////////////////////
        ///// DATA GENERATION HELPERS /////
        ///////////////////////////////////

        // this is a combination of objectIsWithinViewport and objectIsCurrentlyVisible, specifically to check
        // if a single sim object is on screen regardless of agent visibility maxDistance
        // DO NOT USE THIS FOR ALL OBJECTS cause it's going to be soooo expensive
        public bool objectIsOnScreen(SimObjPhysics sop) {
            bool result = false;
            if (sop.VisibilityPoints.Length > 0) {
                Transform[] visPoints = sop.VisibilityPoints;
                foreach (Transform point in visPoints) {
                    Vector3 viewPoint = m_Camera.WorldToViewportPoint(point.position);
                    float ViewPointRangeHigh = 1.0f;
                    float ViewPointRangeLow = 0.0f;

                    // first make sure the vis point is within the viewport at all
                    if (viewPoint.z > 0 &&
                        viewPoint.x < ViewPointRangeHigh && viewPoint.x > ViewPointRangeLow && // within x bounds of viewport
                        viewPoint.y < ViewPointRangeHigh && viewPoint.y > ViewPointRangeLow // within y bounds of viewport
                    ) {
                        // ok so it is within the viewport, not lets do a raycast to see if we can see the vis point
                        updateAllAgentCollidersForVisibilityCheck(false);
                        // raycast from agentcamera to point, ignore triggers, use layers 8 and 10
                        RaycastHit hit;

                        if (
                            Physics.Raycast(
                                m_Camera.transform.position,
                                point.position - m_Camera.transform.position,
                                out hit,
                                Mathf.Infinity,
                                LayerMask.GetMask("SimObjVisible", "Procedural1", "Procedural2", "Procedural3", "Procedural0", "Agent")
                            )
                        ) {
                            if (hit.transform != sop.transform) {
                                result = false;
                            } else {
                                result = true;
                                break;
                            }
                        }
                    }
                }

                updateAllAgentCollidersForVisibilityCheck(true);
                return result;
            } else {
                Debug.Log("Error! Set at least 1 visibility point on SimObjPhysics prefab!");
            }

            return false;
        }

        public bool objectIsCurrentlyVisible(SimObjPhysics sop, float maxDistance) {
            if (sop.VisibilityPoints.Length > 0) {
                Transform[] visPoints = sop.VisibilityPoints;
                updateAllAgentCollidersForVisibilityCheck(false);
                foreach (Transform point in visPoints) {
                    Vector3 tmp = point.position;
                    tmp.y = transform.position.y;
                    // Debug.Log(Vector3.Distance(tmp, transform.position));
                    if (Vector3.Distance(tmp, transform.position) < maxDistance) {
                        // if this particular point is in view...
                        if ((CheckIfVisibilityPointInViewport(sop, point, m_Camera, false).visible ||
                            CheckIfVisibilityPointInViewport(sop, point, m_Camera, true).visible)) {
                            updateAllAgentCollidersForVisibilityCheck(true);
                            return true;
                        }
                    }
                }
            } else {
                Debug.Log("Error! Set at least 1 visibility point on SimObjPhysics prefab!");
            }
            updateAllAgentCollidersForVisibilityCheck(true);
            return false;
        }

        protected int xzManhattanDistance(Vector3 p0, Vector3 p1, float gridSize) {
            return (Math.Abs(Convert.ToInt32((p0.x - p1.x) / gridSize)) +
                Math.Abs(Convert.ToInt32((p0.z - p1.z) / gridSize)));
        }

        // hide and seek action.
        public void ExhaustiveSearchForItem(ServerAction action) {
            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(action.objectId)) {
                errorMessage = "Object ID appears to be invalid.";
                actionFinished(false);
                return;
            }
            SimObjPhysics theObject = physicsSceneManager.ObjectIdToSimObjPhysics[action.objectId];

            Vector3[] positions = null;
            if (action.positions != null && action.positions.Count != 0) {
                positions = action.positions.ToArray();
            } else {
                positions = getReachablePositions();
            }

            bool wasStanding = isStanding();
            Vector3 oldPosition = transform.position;
            Quaternion oldRotation = transform.rotation;
            if (ItemInHand != null) {
                ItemInHand.gameObject.SetActive(false);
            }

            // NOTE: this doesn't use systemRandom
            positions.Shuffle_(seed: action.randomSeed);

            SimplePriorityQueue<Vector3> pq = new SimplePriorityQueue<Vector3>();
            Vector3 agentPos = transform.position;
            foreach (Vector3 p in positions) {
                pq.Enqueue(p, xzManhattanDistance(p, agentPos, gridSize));
            }

#if UNITY_EDITOR
            Vector3 visiblePosition = new Vector3(0.0f, 0.0f, 0.0f);
#endif
            bool objectSeen = false;
            int positionsTried = 0;
            while (pq.Count != 0 && !objectSeen) {
                positionsTried += 1;
                Vector3 p = pq.Dequeue();
                transform.position = p;
                Collider[] colliders = collidersWithinCapsuleCastOfAgent(maxVisibleDistance);

                HashSet<SimObjPhysics> openableObjectsNearby = new HashSet<SimObjPhysics>();
                foreach (Collider c in colliders) {
                    SimObjPhysics sop = ancestorSimObjPhysics(c.gameObject);
                    if (sop != null && sop.GetComponent<CanOpen_Object>() != null) {
                        openableObjectsNearby.Add(sop);
                    }
                }

                foreach (SimObjPhysics openable in openableObjectsNearby) {
                    foreach (GameObject go in openable.GetComponent<CanOpen_Object>().MovingParts) {
                        go.SetActive(false);
                    }
                }

                for (int j = 0; j < 2; j++) { // Standing / Crouching
                    if (j == 0) {
                        stand();
                    } else {
                        crouch();
                    }
                    for (int i = 0; i < 4; i++) { // 4 rotations
                        transform.rotation = Quaternion.Euler(new Vector3(0.0f, 90.0f * i, 0.0f));
                        if (objectIsCurrentlyVisible(theObject, 1000f)) {
                            objectSeen = true;
#if UNITY_EDITOR
                            visiblePosition = p;
#endif
                            break;
                        }
                    }
                    if (objectSeen) {
                        break;
                    }
                }

                foreach (SimObjPhysics openable in openableObjectsNearby) {
                    foreach (GameObject go in openable.GetComponent<CanOpen_Object>().MovingParts) {
                        go.SetActive(true);
                    }
                }
            }

#if UNITY_EDITOR
            if (objectSeen) {
                Debug.Log("Object found.");
                Debug.Log("Manhattan distance:");
                Debug.Log(xzManhattanDistance(visiblePosition, oldPosition, gridSize));
            } else {
                Debug.Log("Object not found.");
            }
            Debug.Log("BFS steps taken:");
            Debug.Log(positionsTried);
#endif

            actionIntReturn = positionsTried;

            Dictionary<string, int> toReturn = new Dictionary<string, int>();
            toReturn["objectSeen"] = objectSeen ? 1 : 0;
            toReturn["positionsTried"] = positionsTried;

            actionFinished(true, toReturn);
        }

        protected HashSet<SimObjPhysics> getAllItemsVisibleFromPositions(Vector3[] positions) {
            bool wasStanding = isStanding();
            Vector3 oldPosition = transform.position;
            Quaternion oldRotation = transform.rotation;
            if (ItemInHand != null) {
                ItemInHand.gameObject.SetActive(false);
            }

            List<GameObject> movingPartsDisabled = new List<GameObject>();
            foreach (SimObjPhysics sop in physicsSceneManager.ObjectIdToSimObjPhysics.Values) {
                if (sop.GetComponent<CanOpen_Object>() != null) {
                    foreach (GameObject go in sop.GetComponent<CanOpen_Object>().MovingParts) {
                        movingPartsDisabled.Add(go);
                        go.SetActive(false);
                    }
                }
            }

            HashSet<SimObjPhysics> allVisible = new HashSet<SimObjPhysics>();
            float[] rotations = { 0f, 90f, 180f, 270f };
            foreach (Vector3 p in positions) {
                transform.position = p;
                foreach (float rotation in rotations) {
                    transform.rotation = Quaternion.Euler(new Vector3(0f, rotation, 0f));
                    for (int i = 0; i < 2; i++) {
                        if (i == 0) {
                            stand();
                        } else {
                            crouch();
                        }
                        foreach (SimObjPhysics sop in GetAllVisibleSimObjPhysics(m_Camera, 1.0f + maxVisibleDistance)) {
                            allVisible.Add(sop);
                        }
                    }
                }
            }

            foreach (GameObject go in movingPartsDisabled) {
                go.SetActive(true);
            }

            if (wasStanding) {
                stand();
            } else {
                crouch();
            }
            transform.position = oldPosition;
            transform.rotation = oldRotation;
            if (ItemInHand != null) {
                ItemInHand.gameObject.SetActive(true);
            }

            return allVisible;
        }

        // @positions/@rotations/@horizons/@standings are used to override all possible values the agent
        // may encounter with basic agent navigation commands (excluding teleport).
        private List<Dictionary<string, object>> getInteractablePoses(
            string objectId,
            bool markActionFinished,
            Vector3[] positions = null,
            float[] rotations = null,
            float[] horizons = null,
            bool[] standings = null,
            float? maxDistance = null,
            int maxPoses = int.MaxValue  // works like infinity
        ) {
            if (360 % rotateStepDegrees != 0 && rotations != null) {
                throw new InvalidOperationException($"360 % rotateStepDegrees (360 % {rotateStepDegrees} != 0) must be 0, unless 'rotations: float[]' is overwritten.");
            }

            if (maxPoses <= 0) {
                throw new ArgumentOutOfRangeException("maxPoses must be > 0.");
            }

            // default "visibility" distance
            float maxDistanceFloat;
            if (maxDistance == null) {
                maxDistanceFloat = maxVisibleDistance;
            } else if ((float)maxDistance <= 0) {
                throw new ArgumentOutOfRangeException("maxDistance must be >= 0 meters from the object.");
            } else {
                maxDistanceFloat = (float)maxDistance;
            }

            SimObjPhysics theObject = getInteractableSimObjectFromId(objectId: objectId, forceAction: true);

            // Populate default standings. Note that these are boolean because that's
            // the most natural integration with Teleport
            if (standings == null) {
                standings = new bool[] { false, true };
            }

            // populate default horizons
            if (horizons == null) {
                horizons = new float[] { -30, 0, 30, 60 };
            } else {
                foreach (float horizon in horizons) {
                    // recall that horizon=60 is look down 60 degrees and horizon=-30 is look up 30 degrees
                    if (horizon > maxDownwardLookAngle || horizon < -maxUpwardLookAngle) {
                        throw new ArgumentException(
                            $"Each horizon must be in [{-maxUpwardLookAngle}:{maxDownwardLookAngle}]. You gave {horizon}."
                        );
                    }
                }
            }

            // populate the positions by those that are reachable
            if (positions == null) {
                positions = getReachablePositions();
            }

            // populate the rotations based on rotateStepDegrees
            if (rotations == null) {
                // Consider the case where one does not want to move on a perfect grid, and is currently moving
                // with an offsetted set of rotations like {10, 100, 190, 280} instead of the default {0, 90, 180, 270}.
                // This may happen if the agent starts by teleports with the rotation of 10 degrees.
                int offset = (int)Math.Round(transform.eulerAngles.y % rotateStepDegrees);

                // Examples:
                // if rotateStepDegrees=10 and offset=70, then the paths would be [70, 80, ..., 400, 410, 420].
                // if rotateStepDegrees=90 and offset=10, then the paths would be [10, 100, 190, 280]
                rotations = new float[(int)Math.Round(360 / rotateStepDegrees)];
                int i = 0;
                for (float rotation = offset; rotation < 360 + offset; rotation += rotateStepDegrees) {
                    rotations[i++] = rotation;
                }
            }

            if (horizons.Length == 0 || rotations.Length == 0 || positions.Length == 0 || standings.Length == 0) {
                throw new InvalidOperationException("Every degree of freedom must have at least 1 valid value.");
            }

            // save current agent pose
            bool wasStanding = isStanding();
            Vector3 oldPosition = transform.position;
            Quaternion oldRotation = transform.rotation;
            Vector3 oldHorizon = m_Camera.transform.localEulerAngles;
            if (ItemInHand != null) {
                ItemInHand.gameObject.SetActive(false);
            }

            // Don't want to consider all positions in the scene, just those from which the object
            // is plausibly visible. The following computes a "fudgeFactor" (radius of the object)
            // which is then used to filter the set of all reachable positions to just those plausible positions.
            Bounds objectBounds = UtilityFunctions.CreateEmptyBounds();
            objectBounds.Encapsulate(theObject.transform.position);
            foreach (Transform vp in theObject.VisibilityPoints) {
                objectBounds.Encapsulate(vp.position);
            }
            float fudgeFactor = objectBounds.extents.magnitude;
            List<Vector3> filteredPositions = positions.Where(
                p => (Vector3.Distance(a: p, b: theObject.transform.position) <= maxDistanceFloat + fudgeFactor + gridSize)
            ).ToList();

            // set each key to store a list
            List<Dictionary<string, object>> validAgentPoses = new List<Dictionary<string, object>>();
            string[] keys = { "x", "y", "z", "rotation", "standing", "horizon" };

            // iterate over each reasonable agent pose
            bool stopEarly = false;
            foreach (float horizon in horizons) {
                m_Camera.transform.localEulerAngles = new Vector3(horizon, 0f, 0f);

                foreach (bool standing in standings) {
                    if (standing) {
                        stand();
                    } else {
                        crouch();
                    }

                    foreach (float rotation in rotations) {
                        Vector3 rotationVector = new Vector3(x: 0, y: rotation, z: 0);
                        transform.rotation = Quaternion.Euler(rotationVector);

                        foreach (Vector3 position in filteredPositions) {
                            transform.position = position;

                            // Each of these values is directly compatible with TeleportFull
                            // and should be used with .step(action='TeleportFull', **interactable_positions[0])
                            if (objectIsCurrentlyVisible(theObject, maxDistanceFloat)) {
                                validAgentPoses.Add(new Dictionary<string, object> {
                                    ["x"] = position.x,
                                    ["y"] = position.y,
                                    ["z"] = position.z,
                                    ["rotation"] = rotation,
                                    ["standing"] = standing,
                                    ["horizon"] = horizon
                                });

                                if (validAgentPoses.Count >= maxPoses) {
                                    stopEarly = true;
                                    break;
                                }

#if UNITY_EDITOR
                                // In the editor, draw lines indicating from where the object was visible.
                                Debug.DrawLine(position, position + transform.forward * (gridSize * 0.5f), Color.red, 20f);
#endif
                            }
                        }
                        if (stopEarly) {
                            break;
                        }
                    }
                    if (stopEarly) {
                        break;
                    }
                }
                if (stopEarly) {
                    break;
                }
            }

            // restore old agent pose
            if (wasStanding) {
                stand();
            } else {
                crouch();
            }
            transform.position = oldPosition;
            transform.rotation = oldRotation;
            m_Camera.transform.localEulerAngles = oldHorizon;
            if (ItemInHand != null) {
                ItemInHand.gameObject.SetActive(true);
            }

#if UNITY_EDITOR
            Debug.Log(validAgentPoses.Count);
            Debug.Log(validAgentPoses);
#endif

            if (markActionFinished) {
                actionFinishedEmit(success: true, actionReturn: validAgentPoses);
            }

            return validAgentPoses;
        }

        // Get the poses with which the agent can interact with 'objectId'
        // @rotations: if rotation is not specified, we use rotateStepDegrees, which results in [0, 90, 180, 270] by default.
        public void GetInteractablePoses(
            string objectId,
            Vector3[] positions = null,
            float[] rotations = null,
            float[] horizons = null,
            bool[] standings = null,
            float? maxDistance = null,
            int maxPoses = int.MaxValue  // works like infinity
        ) {
            getInteractablePoses(
                objectId: objectId,
                markActionFinished: true,
                positions: positions, rotations: rotations, horizons: horizons, standings: standings,
                maxDistance: maxDistance,
                maxPoses: maxPoses
            );
        }

        [ObsoleteAttribute(message: "This action is deprecated. Call GetInteractablePoses instead.", error: false)]
        public void PositionsFromWhichItemIsInteractable(string objectId, float horizon = 30, Vector3[] positions = null) {
            // set horizons using the horizon as an increment
            List<float> horizons = new List<float>();
            for (float h = -maxUpwardLookAngle; h <= maxDownwardLookAngle; h += horizon) {
                horizons.Add(h);
            }
            List<Dictionary<string, object>> interactablePoses = getInteractablePoses(
                objectId: objectId,
                markActionFinished: false,
                positions: positions,
                horizons: horizons.ToArray()
            );

            // for backwards compatibility, PositionsFromWhichItemIsInteractable returns
            // Dictionary<string, float> instead of List<Dictionary<string, object>>,
            // where the latter is cleaner in python.
            Dictionary<string, List<float>> d = new Dictionary<string, List<float>>();
            string[] keys = { "x", "y", "z", "rotation", "standing", "horizon" };
            foreach (string key in keys) {
                d[key] = new List<float>();
            }
            foreach (Dictionary<string, object> pose in interactablePoses) {
                foreach (string key in keys) {
                    if (key == "standing") {
                        // standing is converted from true => 1 to false => 0, for backwards compatibility
                        d[key].Add((bool)pose[key] ? 1 : 0);
                    } else {
                        // all other keys have float outputs
                        d[key].Add((float)pose[key]);
                    }
                }
            }
            actionFinishedEmit(true, d);
        }

        // private helper for NumberOfPositionsFromWhichItemIsVisible
        private int numVisiblePositions(string objectId, bool markActionFinished, Vector3[] positions = null, int maxPoses = int.MaxValue) {
            List<Dictionary<string, object>> interactablePoses = getInteractablePoses(
                objectId: objectId,
                positions: positions,
                maxDistance: 1e5f,  // super large number for maximum distance!
                horizons: new float[] { m_Camera.transform.localEulerAngles.x },  // don't care about every horizon here, just horizon={current horizon}
                markActionFinished: false,
                maxPoses: maxPoses
            );

            // object id might have been invalid, causing failure
            if (markActionFinished) {
                actionFinishedEmit(success: true, actionReturn: interactablePoses.Count);
            }
            return interactablePoses.Count;
        }

        // Similar to GetInteractablePositions, but with horizon=0 and maxDistance like infinity
        public void NumberOfPositionsFromWhichItemIsVisible(string objectId, Vector3[] positions = null) {
            numVisiblePositions(objectId: objectId, positions: positions, markActionFinished: true);
        }

        public void TogglePhysics() {
            Physics.autoSimulation = !Physics.autoSimulation;
            actionFinished(true);
        }

        public void ChangeOpenSpeed(ServerAction action) {
            foreach (CanOpen_Object coo in GameObject.FindObjectsOfType<CanOpen_Object>()) {
                coo.animationTime = action.x;
            }
            actionFinished(true);
        }

        // to ignore the agent in this collision check, set ignoreAgent to true
        protected bool isHandObjectColliding(bool ignoreAgent = false, float expandBy = 0.0f) {
            if (ItemInHand == null) {
                return false;
            }
            List<GameObject> ignoreGameObjects = new List<GameObject>();
            // Ignore the agent when determining if the hand object is colliding
            if (ignoreAgent) {
                ignoreGameObjects.Add(this.gameObject);
            }
            return UtilityFunctions.isObjectColliding(ItemInHand, ignoreGameObjects, expandBy);
        }

        protected bool isAgentCapsuleCollidingWith(GameObject otherGameObject) {
            int layerMask = LayerMask.GetMask("SimObjVisible", "Procedural1", "Procedural2", "Procedural3", "Procedural0");
            foreach (Collider c in PhysicsExtensions.OverlapCapsule(GetComponent<CapsuleCollider>(), layerMask, QueryTriggerInteraction.Ignore)) {
                if (hasAncestor(c.transform.gameObject, otherGameObject)) {
                    return true;
                }
            }
            return false;
        }

        // protected GameObject isUnmoveableSurfaceCollidingWith(GameObject openableObject) {
        //     // Okay, this is the part where I begrudgingly add an entire check of every moving parts' collider
        //     // against every contained collider to see if it belongs to a structure or static SimObject...ugh...
        //     int layerMask = 1 << 8;
        //     foreach (GameObject MovingPart in openableObject.GetComponent<CanOpen_Object>().MovingParts) {
        //         foreach (Collider col in MovingPart.transform.Find("Colliders").GetComponentsInChildren<Collider>()) {
        //             if (col.GetType() == typeof(BoxCollider)) {
        //                 BoxCollider bcol = col as BoxCollider;
        //                 foreach (Collider containedCol in Physics.OverlapBox(
        //                     bcol.transform.TransformPoint(bcol.center),
        //                     bcol.size / 2f,
        //                     bcol.transform.rotation,
        //                     layerMask)) {
        //                     return IsContainedColliderADealbreaker(openableObject, containedCol);
        //                 }
        //             }

        //             else if (col.GetType() == typeof(CapsuleCollider)) {
        //                 CapsuleCollider ccol = col as CapsuleCollider;
        //                 foreach (Collider containedCol in Physics.OverlapCapsule(
        //                     ccol.transform.TransformPoint(new Vector3 (ccol.center.x, ccol.center.y + ccol.height / 2, ccol.center.y)),
        //                     ccol.transform.TransformPoint(new Vector3 (ccol.center.x, ccol.center.y - ccol.height / 2, ccol.center.y)),
        //                     ccol.radius,
        //                     layerMask)) {
        //                     return IsContainedColliderADealbreaker(openableObject, containedCol);
        //                 }
        //             }

        //             else if (col.GetType() == typeof(SphereCollider)) {
        //                 SphereCollider scol = col as SphereCollider;
        //                 foreach (Collider containedCol in Physics.OverlapSphere(
        //                     scol.transform.TransformPoint(scol.center),
        //                     scol.radius,
        //                     layerMask)) {
        //                     return IsContainedColliderADealbreaker(openableObject, containedCol);
        //                 }
        //             }
        //         }
        //     }
        //     return null;
        // }
        //
        // protected GameObject IsContainedColliderADealbreaker(GameObject openableObject, Collider containedCol) {
        //     // Edge Case 1: There should be no need to compare static gameobjects against other static gameobjects and structures,
        //     // or else we have a larger problem upstream with object placement
        //     if (containedCol.GetType() == typeof(MeshCollider)) {
        //         // if (openableObject) == // Blah blah blah working on it...
        //         return containedCol.gameObject;
        //     }
        //     // collider shouldn't be 
        //     else if (ancestorSimObjPhysics(containedCol.gameObject) != null &&
        //         ancestorSimObjPhysics(containedCol.gameObject).gameObject != openableObject &&
        //         ancestorSimObjPhysics(containedCol.gameObject).PrimaryProperty == SimObjPrimaryProperty.Static) {
        //         return ancestorSimObjPhysics(containedCol.gameObject).gameObject;
        //     }
        //     else {
        //         return ancestorSimObjPhysics(containedCol.gameObject).gameObject;
        //     }
        // }
        //
        // protected bool isHandObjectCollidingWith(GameObject otherGameObject) {
        //     if (ItemInHand == null) {
        //         return false;
        //     }
        //     int layerMask = 1 << 8;
        //     foreach (CapsuleCollider cc in ItemInHand.GetComponentsInChildren<CapsuleCollider>()) {
        //         foreach (Collider c in PhysicsExtensions.OverlapCapsule(cc, layerMask, QueryTriggerInteraction.Ignore)) {
        //             if (hasAncestor(c.transform.gameObject, otherGameObject)) {
        //                 return true;
        //             }
        //         }
        //     }
        //     foreach (BoxCollider bc in ItemInHand.GetComponentsInChildren<BoxCollider>()) {
        //         foreach (Collider c in PhysicsExtensions.OverlapBox(bc, layerMask, QueryTriggerInteraction.Ignore)) {
        //             if (!hasAncestor(c.transform.gameObject, otherGameObject)) {
        //                 return true;
        //             }
        //         }
        //     }
        //     foreach (SphereCollider sc in ItemInHand.GetComponentsInChildren<SphereCollider>()) {
        //         foreach (Collider c in PhysicsExtensions.OverlapSphere(sc, layerMask, QueryTriggerInteraction.Ignore)) {
        //             if (!hasAncestor(c.transform.gameObject, otherGameObject)) {
        //                 return true;
        //             }
        //         }
        //     }
        //     return false;
        // }

        public float roundToGridSize(float x, float gridSize, bool roundUp) {
            int mFactor = Convert.ToInt32(1.0f / gridSize);
            if (Math.Abs(mFactor - 1.0f / gridSize) > 1e-3) {
                throw new Exception("1.0 / gridSize should be an integer.");
            }
            if (roundUp) {
                return (float)Math.Ceiling(mFactor * x) / mFactor;
            } else {
                return (float)Math.Floor(mFactor * x) / mFactor;
            }
        }

        public void RandomlyMoveAgent(int randomSeed = 0) {
#if UNITY_EDITOR
            randomSeed = UnityEngine.Random.Range(0, 1000000);
#endif
            Vector3[] reachablePositions = getReachablePositions();
            var orientations = new float[]{
                0,
                90,
                180,
                270
            };
            orientations.Shuffle_(randomSeed);
            reachablePositions.Shuffle_(randomSeed);

            bool success = false;
            foreach (Vector3 position in reachablePositions) {
                foreach (float rotation in orientations) {
                    if (handObjectCanFitInPosition(position, rotation)) {
                        this.transform.position = position;
                        this.transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
                        success = true;
                        break;
                    }
                }
            }

            if (errorMessage != "") {
                actionFinished(false);
            } else if (!success) {
                errorMessage = "Could not find a position in which the agent and object fit.";
                actionFinished(false);
            } else {
                actionFinished(true, reachablePositions);
            }
        }

        public void GetReachablePositionsForObject(ServerAction action) {
            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(action.objectId)) {
                errorMessage = "Object " + action.objectId + " does not seem to exist.";
                actionFinished(false);
                return;
            }
            SimObjPhysics sop = physicsSceneManager.ObjectIdToSimObjPhysics[action.objectId];

            Vector3 startPos = sop.transform.position;
            Quaternion startRot = sop.transform.rotation;

            Vector3[] positions = null;
            if (action.positions != null && action.positions.Count != 0) {
                positions = action.positions.ToArray();
            } else {
                positions = getReachablePositions();
            }

            Bounds b = UtilityFunctions.CreateEmptyBounds();
            foreach (Vector3 p in positions) {
                b.Encapsulate(p);
            }

            float xMin = roundToGridSize(b.min.x - gridSize * 3, gridSize, true);
            float xMax = roundToGridSize(b.max.x + gridSize * 3, gridSize, false);
            float zMin = roundToGridSize(b.min.z - gridSize * 3, gridSize, true);
            float zMax = roundToGridSize(b.max.z + gridSize * 3, gridSize, false);
            // Debug.Log(xMin);
            // Debug.Log(xMax);
            // Debug.Log(zMin);
            // Debug.Log(zMax);

            List<GameObject> agentGameObjects = new List<GameObject>();
            foreach (BaseFPSAgentController agent in agentManager.agents) {
                agentGameObjects.Add(agent.gameObject);
            }
            // List<Vector3> reachable = new List<Vector3>();

            List<Collider> enabledColliders = new List<Collider>();
            foreach (Collider c in sop.GetComponentsInChildren<Collider>()) {
                if (c.enabled) {
                    c.enabled = false;
                    enabledColliders.Add(c);
                }
            }
            sop.BoundingBox.GetComponent<BoxCollider>().enabled = true;

            Dictionary<int, List<Vector3>> reachablePerRotation = new Dictionary<int, List<Vector3>>();
            for (int k = 0; k < 4; k++) {
                reachablePerRotation[90 * k] = new List<Vector3>();
                sop.transform.rotation = Quaternion.Euler(new Vector3(0f, k * 90f, 0f));

                for (int i = 0; i <= (int)((xMax - xMin) / gridSize); i++) {
                    for (int j = 0; j <= (int)((zMax - zMin) / gridSize); j++) {
                        Vector3 p = new Vector3(xMin + gridSize * i, startPos.y, zMin + j * gridSize);
                        sop.transform.position = p;
                        if (!UtilityFunctions.isObjectColliding(
                                sop.BoundingBox.gameObject,
                                agentGameObjects,
                                0.0f,
                                true
                            )) {
                            // #if UNITY_EDITOR
                            // Debug.Log(p);
                            // #endif
#if UNITY_EDITOR
                            Debug.DrawLine(p, new Vector3(p.x, p.y + 0.3f, p.z) + sop.transform.forward * 0.3f, Color.red, 60f);
#endif
                            reachablePerRotation[90 * k].Add(p);
                        }
                    }
                }
            }
            sop.BoundingBox.GetComponent<BoxCollider>().enabled = false;
            foreach (Collider c in enabledColliders) {
                c.enabled = true;
            }

            sop.transform.position = startPos;
            sop.transform.rotation = startRot;

#if UNITY_EDITOR
            Debug.Log(reachablePerRotation[0].Count);
            Debug.Log(reachablePerRotation[90].Count);
            Debug.Log(reachablePerRotation[180].Count);
            Debug.Log(reachablePerRotation[270].Count);
#endif
            actionFinished(true, reachablePerRotation);
        }

        // from given position in worldspace, raycast straight down and return a point of any surface hit
        // useful for getting a worldspace coordinate on the floor given any point in space.
        public Vector3 GetSurfacePointBelowPosition(Vector3 position) {
            Vector3 point = Vector3.zero;

            // raycast down from the position like 10m and see if you hit anything. If nothing hit, return the original position and an error message?
            RaycastHit hit;
            if (
                Physics.Raycast(
                    position,
                    Vector3.down,
                    out hit,
                    10f,
                    LayerMask.GetMask("Default", "SimObjVisible", "Procedural1", "Procedural2", "Procedural3", "Procedural0", "Agent"),
                    QueryTriggerInteraction.Ignore
                )
            ) {
                point = hit.point;
                return point;
            }

            // nothing hit, return the original position?
            else {
                return position;
            }
        }

        private bool stringInSomeAncestorName(GameObject go, string[] strs) {
            foreach (string str in strs) {
                if (go.name.Contains(str)) {
                    return true;
                }
            }
            if (go.transform.parent != null) {
                return stringInSomeAncestorName(go.transform.parent.gameObject, strs);
            } else {
                return false;
            }
        }

        public void HideObscuringObjects(ServerAction action) {
            string objType = "";
            if (action.objectId != null && action.objectId != "") {
                string[] split = action.objectId.Split('|');
                if (split.Length != 0) {
                    objType = action.objectId.Split('|')[0];
                }
            }
            int xGridSize = 100;
            int yGridSize = 100;
            int layerMask = LayerMask.GetMask("SimObjVisible", "Procedural1", "Procedural2", "Procedural3", "Procedural0");
            for (int i = 0; i < yGridSize; i++) {
                for (int j = 0; j < xGridSize; j++) {
                    float x = j * (1.0f / xGridSize) + (0.5f / xGridSize);
                    float y = (1.0f - (0.5f / yGridSize)) - i * (1.0f / yGridSize);
                    Ray ray = m_Camera.ViewportPointToRay(new Vector3(x, y, 0));
                    RaycastHit hit;
                    while (true) {
                        if (Physics.Raycast(ray, out hit, 10f, layerMask)) {
                            UpdateDisplayGameObject(hit.transform.gameObject, false);
                            SimObjPhysics hitObj = hit.transform.gameObject.GetComponentInChildren<SimObjPhysics>();
                            if (hitObj != null && objType != "" && hitObj.ObjectID.Contains(objType)) {
                                ray.origin = hit.point + ray.direction / 100f;
                            } else {
                                break;
                            }
                        } else {
                            break;
                        }
                    }
                }
            }
            actionFinished(true);
        }

        // spawns object in agent's hand with the same orientation as the agent's hand
        public void CreateObject(ServerAction action) {
            if (ItemInHand != null) {
                errorMessage = "Already have an object in hand, can't create a new one to put there.";
                actionFinished(false);
                return;
            }

            if (action.objectType == null) {
                errorMessage = "Please give valid Object Type from SimObjType enum list";
                actionFinished(false);
                return;
            }

            // spawn the object at the agent's hand position
            InstantiatePrefabTest script = physicsSceneManager.GetComponent<InstantiatePrefabTest>();
            SimObjPhysics so = script.SpawnObject(
                action.objectType,
                action.randomizeObjectAppearance,
                action.objectVariation,
                AgentHand.transform.position,
                AgentHand.transform.rotation.eulerAngles,
                true,
                action.forceAction
            );

            if (so == null) {
                throw new InvalidOperationException(
                    "Failed to create object, are you sure it can be spawned?"
                );
            }

            // update the Physics Scene Manager with this new object
            physicsSceneManager.AddToObjectsInScene(so);

            PickupObject(
                objectId: so.objectID,
                forceAction: true
            );
        }

        public void CreateObjectAtLocation(ServerAction action) {
            Vector3 targetPosition = action.position;
            Vector3 targetRotation = action.rotation;

            if (!action.forceAction && !agentManager.SceneBounds.Contains(targetPosition)) {
                errorMessage = "Target position is out of bounds!";
                actionFinished(false);
                return;
            }

            if (action.objectType == null) {
                errorMessage = "Please give valid Object Type from SimObjType enum list";
                actionFinished(false);
                return;
            }

            // spawn the object at the agent's hand position
            InstantiatePrefabTest script = physicsSceneManager.GetComponent<InstantiatePrefabTest>();
            SimObjPhysics so = script.SpawnObject(action.objectType, action.randomizeObjectAppearance, action.objectVariation,
                targetPosition, targetRotation, false, action.forceAction);

            if (so == null) {
                errorMessage = "Failed to create object, are you sure it can be spawned?";
                actionFinished(false);
                return;
            } else {
                // also update the PHysics Scene Manager with this new object
                physicsSceneManager.AddToObjectsInScene(so);
            }

            actionFinished(true, so.ObjectID);
        }

        protected SimObjPhysics createObjectAtLocation(string objectType, Vector3 targetPosition, Vector3 targetRotation, int objectVariation = 1) {
            if (!agentManager.SceneBounds.Contains(targetPosition)) {
                errorMessage = "Target position is out of bounds!";
                return null;
            }

            if (objectType == null) {
                errorMessage = "Please give valid Object Type from SimObjType enum list";
                return null;
            }

            // spawn the object at the agent's hand position
            InstantiatePrefabTest script = physicsSceneManager.GetComponent<InstantiatePrefabTest>();
            SimObjPhysics so = script.SpawnObject(objectType, false, objectVariation,
                targetPosition, targetRotation, false);

            if (so == null) {
                errorMessage = "Failed to create object, are you sure it can be spawned?";
                return null;
            } else {
                // also update the PHysics Scene Manager with this new object
                physicsSceneManager.AddToObjectsInScene(so);
            }

            return so;
        }


        public void CreateObjectOnFloor(ServerAction action) {
            InstantiatePrefabTest script = physicsSceneManager.GetComponent<InstantiatePrefabTest>();
            Bounds b = script.BoundsOfObject(action.objectType, 1);
            if (b.min.x == float.PositiveInfinity) {
                errorMessage = "Could not get bounds for the object to be created on the floor";
                actionFinished(false);
            } else {
                action.y = b.extents.y + getFloorY(action.x, action.z) + 0.1f;
                action.position = new Vector3(action.x, action.y, action.z);
                CreateObjectAtLocation(action);
            }
        }

        protected bool randomlyPlaceObjectOnFloor(SimObjPhysics sop, Vector3[] candidatePositions) {
            var oldPosition = sop.transform.position;
            var oldRotation = sop.transform.rotation;

            sop.transform.rotation = Quaternion.identity;
            Bounds b = UtilityFunctions.CreateEmptyBounds();
            foreach (Renderer r in sop.GetComponentsInChildren<Renderer>()) {
                if (r.enabled) {
                    b.Encapsulate(r.bounds);
                }
            }

            List<Vector3> shuffledCurrentlyReachable = (List<Vector3>)candidatePositions.ToList().Shuffle_();
            float[] rotations = { 0f, 90f, 180f, 270f };
            List<float> shuffledRotations = (List<float>)rotations.ToList().Shuffle_();
            bool objectColliding = true;
            foreach (Vector3 position in shuffledCurrentlyReachable) {
                float y = b.extents.y + getFloorY(position.x, position.y, position.z) + 0.1f;
                foreach (float r in shuffledRotations) {
                    sop.transform.position = new Vector3(position.x, y, position.z);
                    sop.transform.rotation = Quaternion.Euler(new Vector3(0.0f, r, 0.0f));
                    objectColliding = UtilityFunctions.isObjectColliding(sop.gameObject);
                    if (!objectColliding) {
                        break;
                    }
                }
                if (!objectColliding) {
                    break;
                }
            }
            if (objectColliding) {
                sop.transform.position = oldPosition;
                sop.transform.rotation = oldRotation;
            }
            return objectColliding;
        }

        protected SimObjPhysics randomlyCreateAndPlaceObjectOnFloor(string objectType, int objectVariation, Vector3[] candidatePositions) {
            InstantiatePrefabTest script = physicsSceneManager.GetComponent<InstantiatePrefabTest>();
            Bounds b = script.BoundsOfObject(objectType, 1);
            if (b.min.x != float.PositiveInfinity) {
                errorMessage = "Could not get bounds of object with type " + objectType;
            }

            Vector3[] shuffledCurrentlyReachable = candidatePositions.OrderBy(x => systemRandom.Next()).ToArray();
            float[] rotations = { 0f, 90f, 180f, 270f };
            float[] shuffledRotations = rotations.OrderBy(x => systemRandom.Next()).ToArray();
            SimObjPhysics objectCreated = null;
            foreach (Vector3 position in shuffledCurrentlyReachable) {
                float y = b.extents.y + getFloorY(position.x, position.y, position.z) + 0.01f;
                foreach (float r in shuffledRotations) {
                    objectCreated = createObjectAtLocation(
                        objectType,
                        new Vector3(position.x, y, position.z),
                        new Vector3(0.0f, r, 0.0f),
                        objectVariation);
                    if (objectCreated) {
                        break;
                    }
                }
                if (objectCreated) {
                    errorMessage = "";
                    break;
                }
            }
            return objectCreated;
        }

        protected SimObjPhysics randomlyCreateAndPlaceObjectOnFloor(string objectType, int objectVariation) {
            return randomlyCreateAndPlaceObjectOnFloor(objectType, objectVariation, getReachablePositions());
        }

        public void RandomlyCreateAndPlaceObjectOnFloor(string objectType, int objectVariation = 0) {
            SimObjPhysics objectCreated = randomlyCreateAndPlaceObjectOnFloor(objectType, objectVariation);
            if (!objectCreated) {
                errorMessage = "Failed to randomly create object. " + errorMessage;
                actionFinished(false);
            } else {
                errorMessage = "";
                actionFinished(true, objectCreated.ObjectID);
            }
        }

        public void GetPositionsObjectVisibleFrom(ServerAction action) {
            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(action.objectId)) {
                errorMessage = "Object " + action.objectId + " does not seem to exist.";
                actionFinished(false);
                return;
            }

            SimObjPhysics sop = physicsSceneManager.ObjectIdToSimObjPhysics[action.objectId];

            Vector3 savedPosition = transform.position;
            Quaternion savedRotation = transform.rotation;
            float[] rotations = { 0f, 90f, 180f, 270f };

            List<Vector3> goodPositions = new List<Vector3>();
            List<float> goodRotations = new List<float>();

            Vector3[] positions = null;
            if (action.positions != null && action.positions.Count != 0) {
                positions = action.positions.ToArray();
            } else {
                positions = getReachablePositions();
            }

            foreach (Vector3 position in positions) {
                Vector3 tmp = position;
                tmp.y = sop.transform.position.y;
                if (Vector3.Distance(tmp, sop.transform.position) <= 2 * maxVisibleDistance) {
                    foreach (float r in rotations) {
                        transform.position = position;
                        transform.rotation = Quaternion.Euler(new Vector3(0f, r, 0f));
                        if (objectIsCurrentlyVisible(sop, maxVisibleDistance)) {
#if UNITY_EDITOR
                            Debug.Log(position);
                            Debug.Log(r);
#endif
                            goodPositions.Add(position);
                            goodRotations.Add(r);
                        }
                    }
                }
            }

            actionVector3sReturn = goodPositions.ToArray();
            actionFloatsReturn = goodRotations.ToArray();

            transform.position = savedPosition;
            transform.rotation = savedRotation;

            actionFinished(true);
        }

        public void WorldToViewportPoint(Vector3 position) {
            Vector3 point = m_Camera.WorldToViewportPoint(position);
            if (point.x < 0f || point.x > 1.0f || point.y < 0f || point.y > 1.0f) {
                errorMessage = "Point not in viewport.";
                actionFinished(false);
                return;
            }

            // Translate to coordinates from top left of screen
            actionFinished(true, new Vector3(point.x, 1.0f - point.y, point.z));
        }

        protected float approxPercentScreenObjectOccupies(SimObjPhysics sop, bool updateVisibilityColliders = true) {
            float percent = 0.0f;
            if (sop.VisibilityPoints != null && sop.VisibilityPoints.Length > 0) {
                float minX = 1.0f;
                float maxX = 0.0f;
                float minY = 1.0f;
                float maxY = 0.0f;

                if (updateVisibilityColliders) {
                    updateAllAgentCollidersForVisibilityCheck(false);
                }
                foreach (Transform point in sop.VisibilityPoints) {
                    Vector3 viewPoint = m_Camera.WorldToViewportPoint(point.position);

                    if (CheckIfVisibilityPointInViewport(sop, point, m_Camera, false).visible) {
                        minX = Math.Min(viewPoint.x, minX);
                        maxX = Math.Max(viewPoint.x, maxX);
                        minY = Math.Min(viewPoint.y, minY);
                        maxY = Math.Max(viewPoint.y, maxY);
                    }
                }
                percent = Math.Max(0f, maxX - minX) * Math.Max(0f, maxY - minY);
                if (updateVisibilityColliders) {
                    updateAllAgentCollidersForVisibilityCheck(true);
                }
            }
#if UNITY_EDITOR
            Debug.Log(percent);
#endif
            return percent;
        }

        public void ApproxPercentScreenObjectOccupies(string objectId) {
            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(objectId)) {
                errorMessage = $"Cannot find object with id {objectId}.";
                actionFinishedEmit(false);
                return;
            }
            SimObjPhysics sop = physicsSceneManager.ObjectIdToSimObjPhysics[objectId];
            actionFinished(true, approxPercentScreenObjectOccupies(sop));
        }

        public void ApproxPercentScreenObjectFromPositions(ServerAction action) {
            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(action.objectId)) {
                errorMessage = "Cannot find object with id " + action.objectId;
                actionFinished(false);
                return;
            }
            SimObjPhysics sop = physicsSceneManager.ObjectIdToSimObjPhysics[action.objectId];

            Vector3[] positions = null;
            if (action.positions != null && action.positions.Count != 0) {
                positions = action.positions.ToArray();
            } else {
                positions = getReachablePositions();
            }

            Vector3 oldPosition = transform.position;
            Quaternion oldRotation = transform.rotation;
            float[] rotations = { 0f, 90f, 180f, 270f };

            List<float[]> positionAndApproxAmountVisible = new List<float[]>();

            updateAllAgentCollidersForVisibilityCheck(false);
            foreach (Vector3 position in positions) {
                transform.position = position;
                foreach (float rotation in rotations) {
                    transform.rotation = Quaternion.Euler(0f, rotation, 0f);
                    float approxVisible = approxPercentScreenObjectOccupies(sop, false);
                    if (approxVisible > 0.0f) {
                        float[] tuple = { position.x, position.y, position.z, transform.eulerAngles.y };
                        positionAndApproxAmountVisible.Add(tuple);
                    }
                }
            }
            updateAllAgentCollidersForVisibilityCheck(true);

            transform.position = oldPosition;
            transform.rotation = oldRotation;
            actionFinished(true, positionAndApproxAmountVisible);
        }

        public void GetVisibilityPointsOfObjects() {
            Dictionary<string, List<Vector3>> objectIdToVisibilityPoints = new Dictionary<string, List<Vector3>>();
            foreach (SimObjPhysics sop in physicsSceneManager.ObjectIdToSimObjPhysics.Values) {
                objectIdToVisibilityPoints[sop.ObjectID] = new List<Vector3>();
                if (sop.VisibilityPoints != null) {
                    foreach (Transform t in sop.VisibilityPoints) {
                        objectIdToVisibilityPoints[sop.ObjectID].Add(t.position);
                    }
                }
            }
            actionFinished(true, objectIdToVisibilityPoints);
        }

        public void ObjectsVisibleFromPositions(ServerAction action) {
            Vector3[] positions = null;
            if (action.positions != null && action.positions.Count != 0) {
                positions = action.positions.ToArray();
            } else {
                positions = getReachablePositions();
            }

            Vector3 oldPosition = transform.position;
            Quaternion oldRotation = transform.rotation;
            float[] rotations = { 0f, 90f, 180f, 270f };

            Dictionary<string, List<float[]>> objectIdToVisiblePositions = new Dictionary<string, List<float[]>>();

            foreach (Vector3 position in positions) {
                transform.position = position;
                foreach (float rotation in rotations) {
                    transform.rotation = Quaternion.Euler(0f, rotation, 0f);
                    foreach (SimObjPhysics sop in GetAllVisibleSimObjPhysics(m_Camera, maxVisibleDistance)) {
                        if (!objectIdToVisiblePositions.ContainsKey(sop.ObjectID)) {
                            objectIdToVisiblePositions[sop.ObjectID] = new List<float[]>();
                        }
                        List<float[]> l = objectIdToVisiblePositions[sop.ObjectID];
                        float[] tuple = { position.x, position.y, position.z, transform.eulerAngles.y };
                        l.Add(tuple);
                    }
                }
            }

            transform.position = oldPosition;
            transform.rotation = oldRotation;

            actionFinished(true, objectIdToVisiblePositions);
        }

        public void StackBooks() {
            GameObject topLevelObject = GameObject.Find("HideAndSeek");
            SimObjPhysics[] hideSeekObjects = topLevelObject.GetComponentsInChildren<SimObjPhysics>();

            HashSet<string> seenBooks = new HashSet<string>();
            List<HashSet<SimObjPhysics>> groups = new List<HashSet<SimObjPhysics>>();
            foreach (SimObjPhysics sop in hideSeekObjects) {
                HashSet<SimObjPhysics> group = new HashSet<SimObjPhysics>();
                if (sop.ObjectID.StartsWith("Book|")) {
                    if (!seenBooks.Contains(sop.ObjectID)) {
                        HashSet<SimObjPhysics> objectsNearBook = objectsInBox(
                            sop.transform.position.x, sop.transform.position.z);
                        group.Add(sop);
                        seenBooks.Add(sop.ObjectID);
                        foreach (SimObjPhysics possibleBook in objectsNearBook) {
                            if (possibleBook.ObjectID.StartsWith("Book|") &&
                                !seenBooks.Contains(possibleBook.ObjectID)) {
                                group.Add(possibleBook);
                                seenBooks.Add(possibleBook.ObjectID);
                            }
                        }
                        groups.Add(group);
                    }
                }
            }

            foreach (HashSet<SimObjPhysics> group in groups) {
                SimObjPhysics topBook = null;
                GameObject topMesh = null;
                GameObject topColliders = null;
                GameObject topTrigColliders = null;
                GameObject topVisPoints = null;
                foreach (SimObjPhysics so in group) {
                    if (topBook == null) {
                        topBook = so;
                        topMesh = so.gameObject.transform.Find("mesh").gameObject;
                        topColliders = so.gameObject.transform.Find("Colliders").gameObject;
                        topTrigColliders = so.gameObject.transform.Find("TriggerColliders").gameObject;
                        topVisPoints = so.gameObject.transform.Find("VisibilityPoints").gameObject;
                    } else {
                        GameObject mesh = so.gameObject.transform.Find("mesh").gameObject;
                        mesh.transform.parent = topMesh.transform;

                        GameObject colliders = so.gameObject.transform.Find("Colliders").gameObject;
                        foreach (Transform t in colliders.GetComponentsInChildren<Transform>()) {
                            if (t != colliders.transform) {
                                t.parent = topColliders.transform;
                            }
                        }

                        GameObject trigColliders = so.gameObject.transform.Find("TriggerColliders").gameObject;
                        foreach (Transform t in trigColliders.GetComponentsInChildren<Transform>()) {
                            if (t != colliders.transform) {
                                t.parent = topTrigColliders.transform;
                            }
                        }

                        GameObject visPoints = so.gameObject.transform.Find("VisibilityPoints").gameObject;
                        foreach (Transform t in visPoints.GetComponentsInChildren<Transform>()) {
                            if (t != visPoints.transform) {
                                t.parent = topVisPoints.transform;
                            }
                        }

                        physicsSceneManager.ObjectIdToSimObjPhysics.Remove(so.ObjectID);
                        so.gameObject.SetActive(false);
                    }
                }
            }
            actionFinished(true);
        }

        // hide and seek action
        public void RandomizeHideSeekObjects(int randomSeed, float removeProb) {
            // NOTE: this does not use systemRandom
            var rnd = new System.Random(randomSeed);

            if (!physicsSceneManager.ToggleHideAndSeek(true)) {
                errorMessage = "Hide and Seek object reference not set, nothing to randomize.";
                actionFinished(false);
                return;
            }

            foreach (Transform child in physicsSceneManager.HideAndSeek.transform) {
                child.gameObject.SetActive(rnd.NextDouble() > removeProb);
            }
            physicsSceneManager.SetupScene();
            physicsSceneManager.ResetObjectIdToSimObjPhysics();

            snapAgentToGrid(); // This snapping seems necessary for some reason, really doesn't make any sense.
            actionFinished(true);
        }

        // Following code for calculating the volume of a mesh taken from
        // https://answers.unity.com/questions/52664/how-would-one-calculate-a-3d-mesh-volume-in-unity.html
        // and https://answers.unity.com/questions/52664/how-would-one-calculate-a-3d-mesh-volume-in-unity.html
        //  protected float SignedVolumeOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3) {
        //     float v321 = p3.x * p2.y * p1.z;
        //     float v231 = p2.x * p3.y * p1.z;
        //     float v312 = p3.x * p1.y * p2.z;
        //     float v132 = p1.x * p3.y * p2.z;
        //     float v213 = p2.x * p1.y * p3.z;
        //     float v123 = p1.x * p2.y * p3.z;
        //     return (1.0f / 6.0f) * (-v321 + v231 + v312 - v132 - v213 + v123);
        // }
        // protected float VolumeOfMesh(Mesh mesh) {
        //     float volume = 0;
        //     Vector3[] vertices = mesh.vertices;
        //     int[] triangles = mesh.triangles;
        //     Debug.Log(vertices);
        //     Debug.Log(triangles);
        //     for (int i = 0; i < mesh.triangles.Length; i += 3)
        //     {
        //         Vector3 p1 = vertices[triangles[i + 0]];
        //         Vector3 p2 = vertices[triangles[i + 1]];
        //         Vector3 p3 = vertices[triangles[i + 2]];
        //         volume += SignedVolumeOfTriangle(p1, p2, p3);
        //     }
        //     return Mathf.Abs(volume);
        // }

        // public void VolumeOfObject(ServerAction action) {
        //     SimObjPhysics so = physicsSceneManager.ObjectIdToSimObjPhysics[action.objectId];
        //     foreach (MeshFilter meshFilter in so.GetComponentsInChildren<MeshFilter>()) {
        //         Mesh mesh = meshFilter.sharedMesh;
        //         float volume = VolumeOfMesh(mesh);
        //         string msg = "The volume of the mesh is " + volume + " cube units.";
        //         Debug.Log(msg);
        //     }
        // }

        // End code for calculating the volume of a mesh

        // @pOpen is the probability of opening an openable object.
        // @randOpenness specifies if the openness for each opened object should be random, between 0% : 100%, or always 100%.
        public void RandomlyOpenCloseObjects(
                bool simplifyPhysics = false,
                float pOpen = 0.5f,
                bool randOpenness = true
        ) {
            foreach (SimObjPhysics so in GameObject.FindObjectsOfType<SimObjPhysics>()) {
                if (so.GetComponent<CanOpen_Object>()) {
                    // randomly opens an object to a random openness
                    if (UnityEngine.Random.value < pOpen) {
                        openObject(
                            target: so,
                            openness: randOpenness ? UnityEngine.Random.value : 1,
                            forceAction: true,
                            simplifyPhysics: simplifyPhysics,
                            markActionFinished: false
                        );
                    }
                }
            }
            actionFinished(true);
        }

        public void GetApproximateVolume(string objectId) {
            if (physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(objectId)) {
                SimObjPhysics so = physicsSceneManager.ObjectIdToSimObjPhysics[objectId];
                Quaternion oldRotation = so.transform.rotation;
                so.transform.rotation = Quaternion.identity;
                Bounds objBounds = UtilityFunctions.CreateEmptyBounds();
                bool hasActiveRenderer = false;
                foreach (Renderer r in so.GetComponentsInChildren<Renderer>()) {
                    if (r.enabled) {
                        hasActiveRenderer = true;
                        objBounds.Encapsulate(r.bounds);
                    }
                }
                if (!hasActiveRenderer) {
                    errorMessage = "Cannot get bounds for " + objectId + " as it has no attached (and active) renderers.";
                    actionFinished(false);
                    return;
                }
                so.transform.rotation = oldRotation;
                Vector3 diffs = objBounds.max - objBounds.min;
                actionFloatReturn = diffs.x * diffs.y * diffs.z;
#if UNITY_EDITOR
                Debug.Log("Volume is " + actionFloatReturn);
#endif
                actionFinished(true);
            } else {
                errorMessage = "Invalid objectId " + objectId;
                actionFinished(false);
            }
        }

        public void GetVolumeOfAllObjects() {
            List<string> objectIds = new List<string>();
            List<float> volumes = new List<float>();
            foreach (SimObjPhysics so in GameObject.FindObjectsOfType<SimObjPhysics>()) {
                Quaternion oldRotation = so.transform.rotation;
                so.transform.rotation = Quaternion.identity;
                Bounds objBounds = UtilityFunctions.CreateEmptyBounds();
                bool hasActiveRenderer = false;
                foreach (Renderer r in so.GetComponentsInChildren<Renderer>()) {
                    if (r.enabled) {
                        hasActiveRenderer = true;
                        objBounds.Encapsulate(r.bounds);
                    }
                }
                if (!hasActiveRenderer) {
                    continue;
                }
                so.transform.rotation = oldRotation;
                Vector3 diffs = objBounds.max - objBounds.min;

                objectIds.Add(so.ObjectID);
                volumes.Add(diffs.x * diffs.y * diffs.z);
            }
            actionStringsReturn = objectIds.ToArray();
            actionFloatsReturn = volumes.ToArray();
            actionFinished(true);
        }

        protected void changeObjectBlendMode(SimObjPhysics so, StandardShaderUtils.BlendMode bm, float alpha) {
            HashSet<MeshRenderer> renderersToSkip = new HashSet<MeshRenderer>();
            foreach (SimObjPhysics childSo in so.GetComponentsInChildren<SimObjPhysics>()) {
                if (!childSo.ObjectID.StartsWith("Drawer") &&
                    !childSo.ObjectID.Split('|')[0].EndsWith("Door") &&
                    so.ObjectID != childSo.ObjectID) {
                    foreach (MeshRenderer mr in childSo.GetComponentsInChildren<MeshRenderer>()) {
                        renderersToSkip.Add(mr);
                    }
                }
            }

            foreach (MeshRenderer r in so.gameObject.GetComponentsInChildren<MeshRenderer>() as MeshRenderer[]) {
                if (!renderersToSkip.Contains(r)) {
                    Material[] newMaterials = new Material[r.materials.Length];
                    for (int i = 0; i < newMaterials.Length; i++) {
                        newMaterials[i] = new Material(r.materials[i]);
                        StandardShaderUtils.ChangeRenderMode(newMaterials[i], bm);
                        Color color = newMaterials[i].color;
                        color.a = alpha;
                        newMaterials[i].color = color;
                    }
                    r.materials = newMaterials;
                }
            }
        }

        public void MakeObjectTransparent(string objectId) {
            if (physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(objectId)) {
                changeObjectBlendMode(
                    physicsSceneManager.ObjectIdToSimObjPhysics[objectId],
                    StandardShaderUtils.BlendMode.Fade,
                    0.4f
                );
                actionFinished(true);
            } else {
                errorMessage = "Invalid objectId " + objectId;
                actionFinished(false);
            }
        }

        public void MakeObjectOpaque(string objectId) {
            if (physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(objectId)) {
                changeObjectBlendMode(
                    physicsSceneManager.ObjectIdToSimObjPhysics[objectId],
                    StandardShaderUtils.BlendMode.Opaque,
                    1.0f
                );
                actionFinished(true);
            } else {
                errorMessage = "Invalid objectId " + objectId;
                actionFinished(false);
            }
        }

        public void UnmaskWalkable() {
            GameObject walkableParent = GameObject.Find("WalkablePlanes");
            if (walkableParent != null) {
                foreach (GameObject go in GameObject.FindObjectsOfType<GameObject>()) {
                    unmaskGameObject(go);
                }
                foreach (Renderer r in walkableParent.GetComponentsInChildren<Renderer>()) {
                    r.enabled = false;
                }
            }
            actionFinished(true);
        }

        public void MaskWalkable() {
            Material backgroundMaterial = new Material(Shader.Find("Unlit/Color"));
            backgroundMaterial.color = Color.green;

            foreach (GameObject go in GameObject.FindObjectsOfType<GameObject>()) {
                if (!ancestorHasName(go, "WalkablePlanes")) {
                    maskGameObject(go, backgroundMaterial);
                }
            }

            GameObject walkableParent = GameObject.Find("WalkablePlanes");
            if (walkableParent != null) {
                foreach (Renderer r in walkableParent.GetComponentsInChildren<Renderer>()) {
                    r.enabled = true;
                }
                actionFinished(true);
                return;
            }

            Vector3[] reachablePositions = getReachablePositions();
            walkableParent = new GameObject();
            walkableParent.name = "WalkablePlanes";
            GameObject topLevelObject = GameObject.Find("Objects");
            if (topLevelObject != null) {
                walkableParent.transform.parent = topLevelObject.transform;
            }

            int layerMask = LayerMask.GetMask("SimObjVisible", "Procedural1", "Procedural2", "Procedural3", "Procedural0");
            foreach (Vector3 p in reachablePositions) {
                RaycastHit hit;
                bool somethingHit = false;
                float y = 0f;
                for (int i = -1; i <= 1; i++) {
                    for (int j = -1; j <= 1; j++) {
                        Vector3 offset = new Vector3(i * 0.41f * gridSize, 0f, i * 0.41f * gridSize);
                        if (Physics.Raycast(p + offset, -transform.up, out hit, 10f, layerMask)) {
                            if (!somethingHit) {
                                y = hit.point.y;
                            } else {
                                y = Math.Max(y, hit.point.y);
                            }
                            somethingHit = true;
                        }
                    }
                }
                if (somethingHit) {
                    y += 0.01f;
                    y = Math.Max(y, 0.05f);
                    GameObject plane = Instantiate(
                        Resources.Load("BluePlane") as GameObject,
                        new Vector3(p.x, y, p.z),
                        Quaternion.identity
                    ) as GameObject;
                    plane.name = "WalkablePlane";
                    plane.transform.parent = walkableParent.transform;
                    plane.transform.localScale = new Vector3(gridSize * 0.1f, 0.1f, gridSize * 0.1f);
                }
            }
            actionFinished(true);
        }

        private IEnumerator CoverSurfacesWithHelper(
            int n,
            List<SimObjPhysics> newObjects,
            Vector3[] reachablePositions
        ) {
            Vector3[] initialPositions = new Vector3[newObjects.Count];
            int k = 0;
            bool[] deleted = new bool[newObjects.Count];
            foreach (SimObjPhysics so in newObjects) {
                initialPositions[k] = so.transform.position;
                deleted[k] = false;
                k++;
            }
            for (int i = 0; i < n; i++) {
                k = 0;
                foreach (SimObjPhysics so in newObjects) {
                    if (!deleted[k]) {
                        float dist = Vector3.Distance(initialPositions[k], so.transform.position);
                        if (dist > 0.5f) {
                            deleted[k] = true;
                            so.gameObject.SetActive(false);
                        }
                    }
                    k++;
                }
                yield return null;
            }

            HashSet<string> objectIdsContained = new HashSet<string>();
            foreach (SimObjPhysics so in physicsSceneManager.ObjectIdToSimObjPhysics.Values) {
                if (objectIsOfIntoType(so)) {
                    foreach (string id in so.GetAllSimObjectsInReceptacleTriggersByObjectID()) {
                        objectIdsContained.Add(id);
                    }
                }
            }

            Material redMaterial = (Material)Resources.Load("RED", typeof(Material));
            Material greenMaterial = (Material)Resources.Load("GREEN", typeof(Material));
            Collider[] fpsControllerColliders = GameObject.Find("FPSController").GetComponentsInChildren<Collider>();
            k = 0;
            foreach (SimObjPhysics so in newObjects) {
                if (!deleted[k]) {
                    so.GetComponentInChildren<Rigidbody>().isKinematic = true;
                    foreach (Collider c1 in so.GetComponentsInChildren<Collider>()) {
                        foreach (Collider c in fpsControllerColliders) {
                            Physics.IgnoreCollision(c, c1);
                        }
                    }
                    if (objectIdsContained.Contains(so.ObjectID)) {
                        MaskSimObj(so, greenMaterial);
                    } else {
                        MaskSimObj(so, redMaterial);
                    }
                    physicsSceneManager.AddToObjectsInScene(so);
                }
                k++;
            }

            HashSet<SimObjPhysics> visibleObjects = getAllItemsVisibleFromPositions(reachablePositions);
            foreach (SimObjPhysics so in newObjects) {
                if (so.gameObject.activeSelf && !visibleObjects.Contains(so)) {
                    so.gameObject.SetActive(false);
                    physicsSceneManager.ObjectIdToSimObjPhysics.Remove(so.ObjectID);
                }
            }

            actionFinished(true);
        }

        private void createCubeSurrounding(Bounds bounds) {
            Vector3 center = bounds.center;
            Vector3 max = bounds.max;
            Vector3 min = bounds.min;
            float size = 0.001f;
            float offset = 0.0f;
            min.y = Math.Max(-1.0f, min.y);
            center.y = (max.y + min.y) / 2;
            float xLen = max.x - min.x;
            float yLen = max.y - min.y;
            float zLen = max.z - min.z;

            // Top
            GameObject cube = Instantiate(
                original: Resources.Load("BlueCube") as GameObject,
                position: new Vector3(center.x, max.y + offset + size / 2, center.z),
                rotation: Quaternion.identity
            ) as GameObject;
            cube.transform.parent = GameObject.Find("Objects").transform;
            cube.transform.localScale = new Vector3(xLen + 2 * (size + offset), size, zLen + 2 * (size + offset));

            // Bottom
            cube = Instantiate(
                original: Resources.Load("BlueCube") as GameObject,
                position: new Vector3(center.x, min.y - offset - size / 2, center.z),
                rotation: Quaternion.identity
            ) as GameObject;
            cube.transform.parent = GameObject.Find("Objects").transform;
            cube.transform.localScale = new Vector3(xLen + 2 * (size + offset), size, zLen + 2 * (size + offset));

            // z min
            cube = Instantiate(
                original: Resources.Load("BlueCube") as GameObject,
                position: new Vector3(center.x, center.y, min.z - offset - size / 2),
                rotation: Quaternion.identity
            ) as GameObject;
            cube.transform.parent = GameObject.Find("Objects").transform;
            cube.transform.localScale = new Vector3(xLen + 2 * (size + offset), yLen + 2 * offset, size);

            // z max
            cube = Instantiate(
                original: Resources.Load("BlueCube") as GameObject,
                position: new Vector3(center.x, center.y, max.z + offset + size / 2),
                rotation: Quaternion.identity
            ) as GameObject;
            cube.transform.parent = GameObject.Find("Objects").transform;
            cube.transform.localScale = new Vector3(xLen + 2 * (size + offset), yLen + 2 * offset, size);

            // x min
            cube = Instantiate(
                original: Resources.Load("BlueCube") as GameObject,
                position: new Vector3(min.x - offset - size / 2, center.y, center.z),
                rotation: Quaternion.identity
            ) as GameObject;
            cube.transform.parent = GameObject.Find("Objects").transform;
            cube.transform.localScale = new Vector3(size, yLen + 2 * offset, zLen + 2 * offset);

            // x max
            cube = Instantiate(
                original: Resources.Load("BlueCube") as GameObject,
                position: new Vector3(max.x + offset + size / 2, center.y, center.z),
                rotation: Quaternion.identity
            ) as GameObject;
            cube.transform.parent = GameObject.Find("Objects").transform;
            cube.transform.localScale = new Vector3(size, yLen + 2 * offset, zLen + 2 * offset);
        }

        private List<RaycastHit> RaycastWithRepeatHits(
            Vector3 origin, Vector3 direction, float maxDistance, int layerMask
        ) {
            List<RaycastHit> hits = new List<RaycastHit>();
            RaycastHit hit;
            bool didHit = Physics.Raycast(origin, direction, out hit, maxDistance, layerMask);
            while (didHit) {
                hits.Add(hit);
                origin = hit.point + direction / 100f;
                hit = new RaycastHit();
                didHit = Physics.Raycast(origin, direction, out hit, maxDistance, layerMask);
            }
            return hits;
        }

        public void setAllObjectsToMaterial(Material material) {
            GameObject go = GameObject.Find("Lighting");
            if (go != null) {
                go.SetActive(false);
            }
            foreach (Renderer r in GameObject.FindObjectsOfType<Renderer>()) {
                bool disableRenderer = false;
                foreach (Material m in r.materials) {
                    if (m.name.Contains("LightRay")) {
                        disableRenderer = true;
                        break;
                    }
                }
                if (disableRenderer) {
                    r.enabled = false;
                } else {
                    Material[] newMaterials = new Material[r.materials.Length];
                    for (int i = 0; i < newMaterials.Length; i++) {
                        newMaterials[i] = material;
                    }
                    r.materials = newMaterials;
                }
            }
            foreach (Light l in GameObject.FindObjectsOfType<Light>()) {
                l.enabled = false;
            }
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Color.white;
        }

        public void SetAllObjectsToBlueUnlit() {
            setAllObjectsToMaterial((Material)Resources.Load("BLUE", typeof(Material)));
            actionFinished(true);
        }
        public void SetAllObjectsToBlueStandard() {
            setAllObjectsToMaterial((Material)Resources.Load("BLUE_standard", typeof(Material)));
            actionFinished(true);
        }

        public void EnableFog(float z) {
            GlobalFog gf = m_Camera.GetComponent<GlobalFog>();
            gf.enabled = true;
            gf.heightFog = false;
            gf.useRadialDistance = true;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 0.0f;
            RenderSettings.fogEndDistance = z;
            RenderSettings.fogColor = Color.white;
            actionFinished(true);
        }

        public void DisableFog() {
            m_Camera.GetComponent<GlobalFog>().enabled = false;
            RenderSettings.fog = false;
            actionFinished(true);
        }

        public void ColorSurfaceColorObjectsByDistance(float z) {
            GameObject surfaceCoverObjects = GameObject.Find("SurfaceCoverObjects");

            HashSet<string> objectIdsContained = new HashSet<string>();
            foreach (SimObjPhysics so in physicsSceneManager.ObjectIdToSimObjPhysics.Values) {
                if (objectIsOfIntoType(so)) {
                    foreach (string id in so.GetAllSimObjectsInReceptacleTriggersByObjectID()) {
                        objectIdsContained.Add(id);
                    }
                }
            }

            foreach (SimObjPhysics sop in surfaceCoverObjects.GetComponentsInChildren<SimObjPhysics>()) {
                Material newMaterial;
                float minRed = 0.0f;
                float minGreen = 0.0f;
                newMaterial = new Material(Shader.Find("Unlit/Color"));
                if (objectIdsContained.Contains(sop.ObjectID)) {
                    minGreen = 1.0f;
                } else {
                    minRed = 1.0f;
                }

                Vector3 closestPoint = closestPointToObject(sop);
                closestPoint = new Vector3(closestPoint.x, 0f, closestPoint.z);
                Vector3 tmp = new Vector3(transform.position.x, 0f, transform.position.z);

                float min = Math.Min(Vector3.Distance(closestPoint, tmp) / z, 1.0f);
                newMaterial.color = new Color(
                    Math.Max(minRed, min),
                    Math.Max(minGreen, min),
                    min,
                    1.0f
                );
                MaskSimObj(sop, newMaterial);
            }

            actionFinished(true);
        }

        public void CoverSurfacesWith(ServerAction action) {
            string prefab = action.objectType;
            int objectVariation = action.objectVariation;
            Vector3[] reachablePositions = getReachablePositions();

            Bounds b = UtilityFunctions.CreateEmptyBounds();
            b.min = agentManager.SceneBounds.min;
            b.max = agentManager.SceneBounds.max;
            b.min = new Vector3(
                Math.Max(b.min.x, transform.position.x - 7),
                Math.Max(b.min.y, transform.position.y - 1.3f),
                Math.Max(b.min.z, transform.position.z - 7)
            );
            b.max = new Vector3(
                Math.Min(b.max.x, transform.position.x + 7),
                Math.Min(b.max.y, transform.position.y + 3),
                Math.Min(b.max.z, transform.position.z + 7)
            );
            createCubeSurrounding(b);

            float yMax = b.max.y - 0.2f;
            float xRoomSize = b.max.x - b.min.x;
            float zRoomSize = b.max.z - b.min.z;
            InstantiatePrefabTest script = physicsSceneManager.GetComponent<InstantiatePrefabTest>();
            SimObjPhysics objForBounds = script.SpawnObject(prefab, false, objectVariation, new Vector3(0.0f, b.max.y + 10.0f, 0.0f), transform.eulerAngles, false, true);

            Bounds objBounds = UtilityFunctions.CreateEmptyBounds();
            foreach (Renderer r in objForBounds.GetComponentsInChildren<Renderer>()) {
                objBounds.Encapsulate(r.bounds);
            }
            Vector3 objCenterRelPos = objBounds.center - objForBounds.transform.position;
            Vector3 yOffset = new Vector3(
                0f,
                0.01f + objForBounds.transform.position.y - objBounds.min.y,
                0f
            );
            objForBounds.gameObject.SetActive(false);

            float xExtent = objBounds.max.x - objBounds.min.x;
            float yExtent = objBounds.max.y - objBounds.min.y;
            float zExtent = objBounds.max.z - objBounds.min.z;
            float xStepSize = Math.Max(Math.Max(xExtent, 0.1f), action.x);
            float zStepSize = Math.Max(Math.Max(zExtent, 0.1f), action.z);
            int numXSteps = (int)(xRoomSize / xStepSize);
            int numZSteps = (int)(zRoomSize / zStepSize);
            // float xTmp = -0.153f;
            // float zTmp = -3f;
            List<SimObjPhysics> newObjects = new List<SimObjPhysics>();

            var xsToTry = new List<float>();
            var zsToTry = new List<float>();
            // xsToTry.Add(-0.1253266f);
            // zsToTry.Add(1.159979f);
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>()) {
                if (go.name == "ReceptacleTriggerBox") {
                    Vector3 receptCenter = go.transform.position;
                    xsToTry.Add(receptCenter.x);
                    zsToTry.Add(receptCenter.z);
                }
            }
            for (int i = 0; i < numXSteps; i++) {
                float x = b.min.x + (0.5f + i) * xStepSize;
                for (int j = 0; j < numZSteps; j++) {
                    float z = b.min.z + (0.5f + j) * zStepSize;
                    xsToTry.Add(x);
                    zsToTry.Add(z);
                }
            }
            var xsToTryArray = xsToTry.ToArray();
            var zsToTryArray = zsToTry.ToArray();

            int layerMask = LayerMask.GetMask("SimObjVisible", "Procedural1", "Procedural2", "Procedural3", "Procedural0");
            for (int i = 0; i < xsToTryArray.Length; i++) {
                float xPos = xsToTryArray[i];
                float zPos = zsToTryArray[i];

                List<RaycastHit> hits = RaycastWithRepeatHits(
                    new Vector3(xPos, yMax, zPos),
                    new Vector3(0.0f, -1.0f, 0.0f),
                    10f,
                    layerMask
                );
                int k = -1;
                foreach (RaycastHit hit in hits) {
                    if (b.Contains(hit.point) &&
                        hit.point.y < transform.position.y + 1.2f &&
                        hit.point.y >= transform.position.y - 1.1f &&
                        !AnythingAbovePositionIgnoreObject(
                            hit.point + new Vector3(0f, -0.01f, 0f),
                            0.02f,
                            layerMask,
                            hit.collider.transform.gameObject)
                    ) {
                        SimObjPhysics hitSimObj = hit.transform.gameObject.GetComponent<SimObjPhysics>();
                        if (hitSimObj == null || hitSimObj.ObjectID.Split('|')[0] != prefab) {
                            Vector3 halfExtents = new Vector3(xExtent / 2.1f, yExtent / 2.1f, zExtent / 2.1f);
                            Vector3 center = hit.point + objCenterRelPos + yOffset;
                            Collider[] colliders = Physics.OverlapBox(center, halfExtents, Quaternion.identity, layerMask);
                            if (colliders.Length == 0) {
                                k++;
                                SimObjPhysics newObj = script.SpawnObject(prefab, false, objectVariation, center - objCenterRelPos, transform.eulerAngles, false, true);
                                if (prefab == "Cup") {
                                    foreach (Collider c in newObj.GetComponentsInChildren<Collider>()) {
                                        c.enabled = false;
                                    }
                                    newObj.GetComponentInChildren<Renderer>().gameObject.AddComponent<BoxCollider>();
                                }
                                newObjects.Add(newObj);
                            }
                        }
                    }
                }
            }
            GameObject topLevelObject = GameObject.Find("Objects");
            GameObject newTopLevelObject = new GameObject("SurfaceCoverObjects");
            newTopLevelObject.transform.parent = topLevelObject.transform;
            foreach (SimObjPhysics sop in newObjects) {
                sop.gameObject.transform.parent = newTopLevelObject.transform;
            }
            StartCoroutine(CoverSurfacesWithHelper(100, newObjects, reachablePositions));
        }


        public void NumberOfPositionsObjectsOfTypeAreVisibleFrom(
            string objectType,
            Vector3[] positions
        ) {
#if UNITY_EDITOR
            if (positions == null || positions.Length == 0) {
                List<SimObjPhysics> toReEnable = new List<SimObjPhysics>();
                foreach (SimObjPhysics sop in GameObject.FindObjectsOfType<SimObjPhysics>()) {
                    if (sop.Type.ToString().ToLower() == objectType.ToLower()) {
                        toReEnable.Add(sop);
                        sop.gameObject.SetActive(false);
                    }
                }
                foreach (SimObjPhysics sop in toReEnable) {
                    sop.gameObject.SetActive(true);
                }
            }
#endif


            List<SimObjPhysics> objectsOfType = new List<SimObjPhysics>();
            foreach (SimObjPhysics sop in GameObject.FindObjectsOfType<SimObjPhysics>()) {
                if (sop.Type.ToString().ToLower() == objectType.ToLower()) {
                    objectsOfType.Add(sop);
                    sop.gameObject.SetActive(false);
                }
            }

            Dictionary<String, int> objectIdToPositionsVisibleFrom = new Dictionary<String, int>();
            foreach (SimObjPhysics sop in objectsOfType) {
                sop.gameObject.SetActive(true);
                objectIdToPositionsVisibleFrom.Add(
                    sop.ObjectID,
                    numVisiblePositions(objectId: sop.ObjectID, markActionFinished: false, positions: positions)
                );

#if UNITY_EDITOR
                Debug.Log(sop.ObjectID);
                Debug.Log(objectIdToPositionsVisibleFrom[sop.ObjectID]);
#endif
                sop.gameObject.SetActive(false);
            }

            foreach (SimObjPhysics sop in objectsOfType) {
                sop.gameObject.SetActive(true);
            }

            actionFinished(true, objectIdToPositionsVisibleFrom);
        }

        private IEnumerator SpamObjectsInRoomHelper(int n, List<SimObjPhysics> newObjects) {
            for (int i = 0; i < n; i++) {
                yield return null;
            }

            Collider[] fpsControllerColliders = GameObject.Find("FPSController").GetComponentsInChildren<Collider>();
            foreach (SimObjPhysics so in newObjects) {
                so.GetComponentInChildren<Rigidbody>().isKinematic = true;
                foreach (Collider c1 in so.GetComponentsInChildren<Collider>()) {
                    foreach (Collider c in fpsControllerColliders) {
                        Physics.IgnoreCollision(c, c1);
                    }
                }
                physicsSceneManager.ObjectIdToSimObjPhysics[so.ObjectID] = so;
            }

            actionFinished(true);
        }
        public void SpamObjectsInRoom(int randomSeed = 0) {
            UnityEngine.Random.InitState(randomSeed);

            string[] objectTypes = {
                "Bread",
                "Cup",
                "Footstool",
                "Knife",
                "Plunger",
                "Tomato",
            };
            int numObjectVariations = 3;

            Bounds b = UtilityFunctions.CreateEmptyBounds();
            b.min = agentManager.SceneBounds.min;
            b.max = agentManager.SceneBounds.max;
            b.min = new Vector3(
                Math.Max(b.min.x, transform.position.x - 7),
                Math.Max(b.min.y, transform.position.y - 1.3f),
                Math.Max(b.min.z, transform.position.z - 7)
            );
            b.max = new Vector3(
                Math.Min(b.max.x, transform.position.x + 7),
                Math.Min(b.max.y, transform.position.y + 3),
                Math.Min(b.max.z, transform.position.z + 7)
            );

            float yMax = b.max.y - 0.2f;
            InstantiatePrefabTest script = physicsSceneManager.GetComponent<InstantiatePrefabTest>();

            List<Bounds> objsBounds = new List<Bounds>();
            List<Vector3> objsCenterRelPos = new List<Vector3>();
            List<Vector3> yOffsets = new List<Vector3>();
            float offset = 10f;
            foreach (string objType in objectTypes) {
                for (int i = 1; i < numObjectVariations; i++) {
                    SimObjPhysics objForBounds = script.SpawnObject(
                        objType,
                        false,
                        i,
                        new Vector3(0.0f, b.max.y + offset, 0.0f),
                        transform.eulerAngles,
                        false,
                        true
                    );
                    offset += 1.0f;

                    Bounds objBounds = UtilityFunctions.CreateEmptyBounds();
                    foreach (Renderer r in objForBounds.GetComponentsInChildren<Renderer>()) {
                        objBounds.Encapsulate(r.bounds);
                    }

                    objsBounds.Add(objBounds);
                    objsCenterRelPos.Add(objBounds.center - objForBounds.transform.position);
                    yOffsets.Add(
                        new Vector3(
                            0f,
                            0.01f + objForBounds.transform.position.y - objBounds.min.y,
                            0f
                        )
                    );
                    objForBounds.gameObject.SetActive(false);
                }
            }

            var xsToTry = new List<float>();
            var zsToTry = new List<float>();
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>()) {
                if (go.name == "ReceptacleTriggerBox") {
                    BoxCollider bc = go.GetComponent<BoxCollider>();
                    Bounds bcb = bc.bounds;
                    xsToTry.Add(bcb.center.x);
                    zsToTry.Add(bcb.center.z);
                    for (int i = 0; i < 5; i++) {
                        xsToTry.Add((bcb.max.x - bcb.min.x) * UnityEngine.Random.value + bcb.min.x);
                        zsToTry.Add((bcb.max.z - bcb.min.z) * UnityEngine.Random.value + bcb.min.z);
                    }
                }
            }
            for (int i = 0; i < 1000; i++) {
                xsToTry.Add((b.max.x - b.min.x) * UnityEngine.Random.value + b.min.x);
                zsToTry.Add((b.max.z - b.min.z) * UnityEngine.Random.value + b.min.z);
            }
            var xsToTryArray = xsToTry.ToArray();
            // var zsToTryArray = zsToTry.ToArray();

            List<SimObjPhysics> newObjects = new List<SimObjPhysics>();
            int layerMask = LayerMask.GetMask("SimObjVisible", "Procedural1", "Procedural2", "Procedural3", "Procedural0");
            // int attempts = 0;
            for (int i = 0; i < xsToTryArray.Length; i++) {
                if (newObjects.Count >= 100) {
                    break;
                }
                float xPos = (b.max.x - b.min.x) * UnityEngine.Random.value + b.min.x;
                float zPos = (b.max.z - b.min.z) * UnityEngine.Random.value + b.min.z;

                int objectInd = UnityEngine.Random.Range(0, objectTypes.Length);
                int objectVar = UnityEngine.Random.Range(1, numObjectVariations);

                List<RaycastHit> hits = RaycastWithRepeatHits(
                    new Vector3(xPos, yMax, zPos),
                    new Vector3(0.0f, -1.0f, 0.0f),
                    10f,
                    layerMask
                );

                foreach (RaycastHit hit in hits) {
                    Bounds ob = objsBounds[objectInd];
                    Vector3 randRotation = new Vector3(0.0f, 0.0f, 0.0f);
                    if (UnityEngine.Random.value < 0.5f) {
                        randRotation = new Vector3(UnityEngine.Random.value * 360f, UnityEngine.Random.value * 360f, UnityEngine.Random.value * 360f);
                    }

                    // Debug.Log(UnityEngine.Random.rotationUniform.ToEulerAngles());
                    SimObjPhysics newObj = script.SpawnObject(
                        objectTypes[objectInd],
                        false,
                        objectVar,
                        hit.point + new Vector3(0f, ob.extents.y + 0.05f, 0f) - objsCenterRelPos[objectInd],
                        randRotation,
                        // UnityEngine.Random.rotationUniform.ToEulerAngles(),
                        // transform.eulerAngles, 
                        false,
                        false
                    );
                    if (newObj == null) {
                        newObj = script.SpawnObject(
                            objectTypes[objectInd],
                            false,
                            objectVar,
                            hit.point + new Vector3(0f, Math.Max(ob.extents.z, Math.Max(ob.extents.x, ob.extents.y)) + 0.05f, 0f) - objsCenterRelPos[objectInd],
                            randRotation,
                            // UnityEngine.Random.rotationUniform.ToEulerAngles(),
                            // transform.eulerAngles, 
                            false,
                            false
                        );
                    }
                    if (newObj != null) {
                        newObjects.Add(newObj);
                    }
                    if (newObj != null && objectTypes[objectInd] == "Cup") {
                        foreach (Collider c in newObj.GetComponentsInChildren<Collider>()) {
                            c.enabled = false;
                        }
                        newObj.GetComponentInChildren<Renderer>().gameObject.AddComponent<BoxCollider>();
                    }
                }
            }

            StartCoroutine(SpamObjectsInRoomHelper(100, newObjects));
        }

        public void ChangeLightSet(ServerAction action) {
            if (action.objectVariation > 10 || action.objectVariation < 1) {
                errorMessage = "Please use value between 1 and 10";
                actionFinished(false);
                return;
            }

            GameObject lightTransform = GameObject.Find("Lighting");
            lightTransform.GetComponent<ChangeLighting>().SetLights(action.objectVariation);
            actionFinished(true);
        }

        public void SliceObject(ServerAction action) {
            if (action.forceAction) {
                action.forceVisible = true;
            }

            SimObjPhysics target = null;
            if (action.objectId == null) {
                target = getInteractableSimObjectFromXY(x: action.x, y: action.y, forceAction: action.forceAction);
            } else {
                target = getInteractableSimObjectFromId(objectId: action.objectId, forceAction: action.forceAction);
            }

            // we found it!
            if (target) {

                if (ItemInHand != null) {
                    if (target.transform == ItemInHand.transform) {
                        errorMessage = "target object cannot be sliced if it is in the agent's hand";
                        actionFinished(false);
                        return;
                    }
                }

                if (target.GetComponent<SimObjPhysics>().DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanBeSliced)) {
                    target.GetComponent<SliceObject>().Slice();
                    actionFinished(true);
                    return;
                } else {
                    errorMessage = target.transform.name + " Does not have the CanBeSliced property!";
                    actionFinished(false);
                    return;
                }
            }

            // target not found in currently visible objects, report not found
            else {
                errorMessage = "object not found: " + action.objectId;
                actionFinished(false);
            }
        }

        public void BreakObject(ServerAction action) {
            if (action.forceAction) {
                action.forceVisible = true;
            }

            SimObjPhysics target = null;
            if (action.objectId == null) {
                target = getInteractableSimObjectFromXY(x: action.x, y: action.y, forceAction: action.forceAction);
            } else {
                target = getInteractableSimObjectFromId(objectId: action.objectId, forceAction: action.forceAction);
            }

            // we found it!
            if (target) {
                if (target.GetComponent<SimObjPhysics>().DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanBreak)) {
                    SimObjPhysics targetsop = target.GetComponent<SimObjPhysics>();
                    // if the object is in the agent's hand, we need to reset the agent hand booleans and other cleanup as well
                    if (targetsop.isInAgentHand) {
                        // if the target is also a Receptacle, drop contained objects first
                        if (targetsop.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.Receptacle)) {
                            // drop contained objects as well
                            targetsop.DropContainedObjects(reparentContainedObjects: true, forceKinematic: false);
                        }

                        targetsop.isInAgentHand = false;
                        ItemInHand = null;
                        DefaultAgentHand();
                        // ok now we are ready to break go go go
                    }

                    target.GetComponentInChildren<Break>().BreakObject(null);
                    actionFinished(true);
                    return;
                } else {
                    errorMessage = target.transform.name + " does not have the CanBreak property!!";
                    actionFinished(false);
                    return;
                }
            }

            // target not found in currently visible objects, report not found
            else {
                errorMessage = "object not found: " + action.objectId;
                actionFinished(false);
            }
        }

        public void DirtyObject(ServerAction action) {
            if (action.forceAction) {
                action.forceVisible = true;
            }

            SimObjPhysics target = null;
            if (action.objectId == null) {
                target = getInteractableSimObjectFromXY(x: action.x, y: action.y, forceAction: action.forceAction);
            } else {
                target = getInteractableSimObjectFromId(objectId: action.objectId, forceAction: action.forceAction);
            }

            if (target) {
                if (target.GetComponent<SimObjPhysics>().DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanBeDirty)) {
                    Dirty dirt = target.GetComponent<Dirty>();
                    if (dirt.IsDirty() == false) {
                        dirt.ToggleCleanOrDirty();
                        actionFinished(true);
                        return;
                    } else {
                        errorMessage = target.transform.name + " is already dirty!";
                        actionFinished(false);
                        return;
                    }
                } else {
                    errorMessage = target.transform.name + " does not have CanBeDirty property!";
                    actionFinished(false);
                    return;
                }
            } else {
                errorMessage = "object not found: " + action.objectId;
                actionFinished(false);
            }
        }

        public void CleanObject(ServerAction action) {
            if (action.forceAction) {
                action.forceVisible = true;
            }

            SimObjPhysics target = null;
            if (action.objectId == null) {
                target = getInteractableSimObjectFromXY(x: action.x, y: action.y, forceAction: action.forceAction);
            } else {
                target = getInteractableSimObjectFromId(objectId: action.objectId, forceAction: action.forceAction);
            }

            if (target) {
                if (target.GetComponent<SimObjPhysics>().DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanBeDirty)) {
                    Dirty dirt = target.GetComponent<Dirty>();
                    if (dirt.IsDirty()) {
                        dirt.ToggleCleanOrDirty();
                        actionFinished(true);
                        return;
                    } else {
                        errorMessage = target.transform.name + " is already Clean!";
                        actionFinished(false);
                        return;
                    }
                } else {
                    errorMessage = target.transform.name + " does not have dirtyable property!";
                    actionFinished(false);
                    return;
                }
            } else {
                errorMessage = "object not found: " + action.objectId;
                actionFinished(false);
            }
        }

        // fill an object with a liquid specified by action.fillLiquid - coffee, water, soap, wine, etc
        public void FillObjectWithLiquid(ServerAction action) {
            if (action.forceAction) {
                action.forceVisible = true;
            }

            SimObjPhysics target = null;
            if (action.objectId == null) {
                target = getInteractableSimObjectFromXY(x: action.x, y: action.y, forceAction: action.forceAction);
            } else {
                target = getInteractableSimObjectFromId(objectId: action.objectId, forceAction: action.forceAction);
            }

            if (action.fillLiquid == null) {
                throw new InvalidOperationException("Missing Liquid string for FillObject action");
            }

            // ignore casing
            action.fillLiquid = action.fillLiquid.ToLower();

            if (!target) {
                throw new ArgumentException("object not found: " + action.objectId);
            }

            if (!target.GetComponent<SimObjPhysics>().DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanBeFilled)) {
                throw new ArgumentException(target.transform.name + " does not have CanBeFilled property!");
            }

            Fill fil = target.GetComponent<Fill>();

            // if the passed in liquid string is not valid
            if (!fil.Liquids.ContainsKey(action.fillLiquid)) {
                throw new ArgumentException(action.fillLiquid + " is not a valid Liquid Type");
            }

            if (fil.IsFilled()) {
                throw new InvalidOperationException(target.transform.name + " is already Filled!");
            }

            fil.FillObject(action.fillLiquid);
            actionFinished(true);
        }

        public void EmptyLiquidFromObject(ServerAction action) {
            if (action.forceAction) {
                action.forceVisible = true;
            }

            SimObjPhysics target = null;
            if (action.objectId == null) {
                target = getInteractableSimObjectFromXY(x: action.x, y: action.y, forceAction: action.forceAction);
            } else {
                target = getInteractableSimObjectFromId(objectId: action.objectId, forceAction: action.forceAction);
            }

            if (target) {
                if (target.GetComponent<SimObjPhysics>().DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanBeFilled)) {
                    Fill fil = target.GetComponent<Fill>();

                    if (fil.IsFilled()) {
                        fil.EmptyObject();
                        actionFinished(true);
                        return;
                    } else {
                        errorMessage = "object already empty";
                        actionFinished(false);
                        return;
                    }
                } else {
                    errorMessage = target.transform.name + " does not have CanBeFilled property!";
                    actionFinished(false);
                    return;
                }
            } else {
                errorMessage = "object not found: " + action.objectId;
                actionFinished(false);
            }
        }

        // use up the contents of this object (toilet paper, paper towel, tissue box, etc).
        public void UseUpObject(ServerAction action) {
            if (action.forceAction) {
                action.forceVisible = true;
            }

            SimObjPhysics target = null;
            if (action.objectId == null) {
                target = getInteractableSimObjectFromXY(x: action.x, y: action.y, forceAction: action.forceAction);
            } else {
                target = getInteractableSimObjectFromId(objectId: action.objectId, forceAction: action.forceAction);
            }

            if (target) {
                if (target.GetComponent<SimObjPhysics>().DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanBeUsedUp)) {
                    UsedUp u = target.GetComponent<UsedUp>();

                    // make sure object is not already used up
                    if (!u.isUsedUp) {
                        u.UseUp();
                        actionFinished(true);
                        return;
                    } else {
                        errorMessage = "object already used up!";
                        // Debug.Log(errorMessage);
                        actionFinished(false);
                        return;
                    }
                } else {
                    errorMessage = target.transform.name + " does not have CanBeUsedUp property!";
                    actionFinished(false);
                    return;
                }
            } else {
                errorMessage = "object not found: " + action.objectId;
                actionFinished(false);
            }
        }

        public void GetScenesInBuild() {
            int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
            string[] scenes = new string[sceneCount];
            for (int i = 0; i < sceneCount; i++) {
                scenes[i] = System.IO.Path.GetFileNameWithoutExtension(UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i));
            }
            actionFinished(true, scenes);
        }

        protected bool objectIsOfIntoType(SimObjPhysics so) {
            return so.ReceptacleTriggerBoxes != null &&
                so.ReceptacleTriggerBoxes.Length != 0 &&
                !so.ObjectID.Contains("Table") && // Don't include table tops, counter tops, etc.
                !so.ObjectID.Contains("Counter") &&
                !so.ObjectID.Contains("Top") &&
                !so.ObjectID.Contains("Burner") &&
                !so.ObjectID.Contains("Chair") &&
                !so.ObjectID.Contains("Sofa") &&
                !so.ObjectID.Contains("Shelf") &&
                !so.ObjectID.Contains("Ottoman");
        }

        public void ToggleColorIntoTypeReceptacleFloors() {
            GameObject go = GameObject.Find("IntoObjectFloorPlanes");
            if (go != null) {
                foreach (Renderer r in go.GetComponentsInChildren<Renderer>()) {
                    r.enabled = !r.enabled;
                }
                actionFinished(true);
                return;
            }

            GameObject newParent = new GameObject();
            newParent.name = "IntoObjectFloorPlanes";
            GameObject topLevelObject = GameObject.Find("Objects");
            if (topLevelObject != null) {
                newParent.transform.parent = topLevelObject.transform;
            }

            int layerMask = LayerMask.GetMask("SimObjVisible", "Procedural1", "Procedural2", "Procedural3", "Procedural0");
            foreach (SimObjPhysics so in physicsSceneManager.ObjectIdToSimObjPhysics.Values) {
                if (objectIsOfIntoType(so)) {
                    foreach (GameObject rtb in so.ReceptacleTriggerBoxes) {
                        Quaternion oldRotation = rtb.transform.rotation;
                        Vector3 euler = oldRotation.eulerAngles;

                        rtb.transform.rotation = Quaternion.Euler(new Vector3(euler.x, 0f, euler.z));
                        BoxCollider bc = rtb.GetComponent<BoxCollider>();
                        Bounds b = bc.bounds;
                        rtb.transform.rotation = oldRotation;

                        HashSet<float> yOffsets = new HashSet<float>();
                        yOffsets.Add(b.extents.y - 0.01f);
                        for (int i = -1; i <= 1; i++) {
                            for (int j = -1; j <= 1; j++) {
                                Vector3 start = b.center + new Vector3(i * b.extents.x / 3f, b.extents.y - 0.001f, j * b.extents.z / 3f);
                                foreach (RaycastHit hit in Physics.RaycastAll(start, -transform.up, 10f, layerMask)) {
                                    if (NormalIsApproximatelyUp(hit.normal) &&
                                        ancestorSimObjPhysics(hit.transform.gameObject) == so) {
                                        yOffsets.Add((float)Math.Round(hit.distance - b.extents.y - 0.005f, 3));
                                    }
                                }
                            }
                        }

                        foreach (float yOffset in yOffsets) {
                            GameObject plane = Instantiate(
                                Resources.Load("BluePlane") as GameObject,
                                new Vector3(0f, 0f, 0f),
                                Quaternion.identity
                            ) as GameObject;
                            plane.transform.parent = newParent.transform;
                            plane.transform.localScale = 0.1f * 2f * b.extents;
                            plane.transform.rotation = Quaternion.Euler(new Vector3(0f, euler.y, 0f)); // oldRotation;
                            plane.transform.position = bc.bounds.center + new Vector3(0f, -yOffset, 0f);
                        }
                    }
                }
            }
            actionFinished(true);
        }
    }
}