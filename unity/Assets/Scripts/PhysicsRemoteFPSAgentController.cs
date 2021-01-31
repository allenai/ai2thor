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
using UnityEngine.SceneManagement;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.ImageEffects;
using UnityStandardAssets.Utility;
using RandomExtensions;

namespace UnityStandardAssets.Characters.FirstPerson {
    [RequireComponent(typeof(CharacterController))]
    public class PhysicsRemoteFPSAgentController : BaseFPSAgentController {
        [SerializeField] protected GameObject[] ToSetActive = null;
        protected Dictionary<string, Dictionary<int, Material[]>> maskedObjects = new Dictionary<string, Dictionary<int, Material[]>>();
        bool transparentStructureObjectsHidden = false;

        // face swap stuff here
        public Material[] ScreenFaces; //0 - neutral, 1 - Happy, 2 - Mad, 3 - Angriest
        public MeshRenderer MyFaceMesh;
        public int AdvancePhysicsStepCount;
        public GameObject[] TargetCircles = null;

        // these object types can have a placeable surface mesh associated ith it
        protected List<SimObjType> hasPlaceableSurface = new List<SimObjType>() {
            SimObjType.Bathtub, SimObjType.Sink, SimObjType.Drawer, SimObjType.Cabinet,
            SimObjType.CounterTop, SimObjType.Shelf
        };

        // change visibility check to use this distance when looking down
        // protected float DownwardViewDistance = 2.0f;

        // Use this for initialization
        public override void Start() {
            base.Start();
        }

        // forceVisible is true to activate, false to deactivate
        public void ToggleHideAndSeekObjects(bool forceVisible = false) {
            if (!physicsSceneManager.ToggleHideAndSeek(forceVisible)) {
                throw new InvalidOperationException("No HideAndSeek object found");
            }
            physicsSceneManager.ResetObjectIdToSimObjPhysics();
            actionFinished(true);
        }

        public Vector3 AgentHandLocation() {
            return AgentHand.transform.position;
        }

        public float WhatIsAgentsMaxVisibleDistance() {
            return maxVisibleDistance;
        }

        public GameObject WhatAmIHolding() {
            return ItemInHand;
        }

        // get all sim objects of type, then sets their temperature decay timers to value
        public void SetRoomTempDecayTimeForType(string objectType, float TimeUntilRoomTemp = 0.0f) {
            // get all objects of type passed by action
            SimObjPhysics[] simObjects = GameObject.FindObjectsOfType<SimObjPhysics>();

            List<SimObjPhysics> simObjectsOfType = new List<SimObjPhysics>();

            foreach (SimObjPhysics sop in simObjects) {
                if (sop.Type.ToString() == objectType) {
                    simObjectsOfType.Add(sop);
                }
            }
            // use SetHowManySecondsUntilRoomTemp to set them all
            foreach (SimObjPhysics sop in simObjectsOfType) {
                sop.SetHowManySecondsUntilRoomTemp(TimeUntilRoomTemp);
            }

            actionFinished(true);
        }

        // get all sim objects and globally set the room temp decay time for all of them
        public void SetGlobalRoomTempDecayTime(float TimeUntilRoomTemp=0.0f) {
            // get all objects
            SimObjPhysics[] simObjects = GameObject.FindObjectsOfType<SimObjPhysics>();

            // use SetHowManySecondsUntilRoomTemp to set them all
            foreach (SimObjPhysics sop in simObjects) {
                sop.SetHowManySecondsUntilRoomTemp(TimeUntilRoomTemp);
            }

            actionFinished(true);
        }

        // change the mass/drag/angular drag values of a simobjphys that is pickupable or moveable
        public void SetMassProperties(string objectId, float mass, float drag, float angularDrag) {
            if (objectId == null) {
                throw new ArgumentNullException();
            }

            SimObjPhysics[] simObjects = GameObject.FindObjectsOfType<SimObjPhysics>();
            SimObjPhysics sop = getTargetObject(objectId: objectId, foraceAction: true);

            if (sop.PrimaryProperty != SimObjPrimaryProperty.Moveable && sop.PrimaryProperty != SimObjPrimaryProperty.CanPickup) {
                throw new InvalidOperationException("object with ObjectID: " + objectId + ", is not Moveable or Pickupable, and the Mass Properties cannot be changed");
            }

            Rigidbody rb = sop.GetComponent<Rigidbody>();
            rb.mass = mass;
            rb.drag = drag;
            rb.angularDrag = angularDrag;

            actionFinished(true);
        }

        // sets whether this scene should allow objects to decay temperature to room temp over time or not
        public void SetDecayTemperatureBool(bool allowDecayTemperature) {
            physicsSceneManager.GetComponent<PhysicsSceneManager>().AllowDecayTemperature = allowDecayTemperature;
            actionFinished(true);
        }

        protected void LateUpdate() {
            // make sure this happens in late update so all physics related checks are done ahead of time
            // this is also mostly for in editor, the array of visible sim objects is found via server actions
            // using VisibleSimObjs(action), so be aware of that

            #if UNITY_EDITOR || UNITY_WEBGL
                if (this.agentState == AgentState.ActionComplete) {
                    VisibleSimObjPhysics = VisibleSimObjs(forceVisible: false);
                }
            #endif
        }

        public override ObjectMetadata ObjectMetadataFromSimObjPhysics(SimObjPhysics simObj, bool isVisible) {
            return base.ObjectMetadataFromSimObjPhysics(simObj, isVisible);
        }

        // change the radius of the agent's capsule on the char controller component, and the capsule collider component
        public void SetAgentRadius(float agentRadius = 2.0f) {
            m_CharacterController.radius = agentRadius;
            CapsuleCollider cap = GetComponent<CapsuleCollider>();
            cap.radius = agentRadius;
            actionFinished(true);
        }

        ///////////////////////////////////////////
        /////////// UNITY DEBUG SCRIPTS ///////////
        ///////////////////////////////////////////

        #if UNITY_EDITOR
            // for use in Editor to test the Reset function.
            public void Reset(string sceneName) {
                physicsSceneManager.GetComponent<AgentManager>().Reset(sceneName: sceneName);
            }

            // return ID of closest CanPickup object by distance
            public string ObjectIdOfClosestVisibleObject() {
                string objectID = null;
                foreach (SimObjPhysics o in VisibleSimObjPhysics) {
                    if (o.PrimaryProperty == SimObjPrimaryProperty.CanPickup) {
                        objectID = o.ObjectID;
                        // TODO: why is it breaking? what if it's not the closest!
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
                        // TODO: why is it breaking? what if it's not the closest!
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
                        // TODO: why is it breaking? what if it's not the closest!
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
                        // TODO: why is it breaking? what if it's not the closest!
                        objectID = o.ObjectID;
                        break;
                    }
                }

                return objectID;
            }

            public string ObjectIdOfClosestReceptacleObject() {
                string objectID = null;

                foreach (SimObjPhysics o in VisibleSimObjPhysics) {
                    if (o.hasSecondaryProperty(SimObjSecondaryProperty.Receptacle)) {
                        // TODO: why is it breaking? what if it's not the closest!
                        objectID = o.ObjectID;
                        break;
                    }
                }
                return objectID;
            }
        #endif

        // return a reference to a SimObj that is Visible (in the VisibleSimObjPhysics array) and
        // matches the passed in objectID
        public GameObject FindObjectInVisibleSimObjPhysics(string objectID) {
            GameObject target = null;

            foreach (SimObjPhysics o in VisibleSimObjPhysics) {
                if (o.objectID == objectID) {
                    target = o.gameObject;
                }
            }

            return target;
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
            return Physics.OverlapCapsule(point0, point1, maxDistance, 1 << 8, QueryTriggerInteraction.Collide);
        }

        // use this to check if any given Vector3 coordinate is within the agent's viewport and also not obstructed
        // TODO: Isn't this repeated somewhere?
        public bool CheckIfPointIsInViewport(Vector3 point) {
            Vector3 viewPoint = m_Camera.WorldToViewportPoint(point);

            float ViewPointRangeHigh = 1.0f;
            float ViewPointRangeLow = 0.0f;

            if (viewPoint.z > 0 //&& viewPoint.z < maxDistance * DownwardViewDistance // is in front of camera and within range of visibility sphere
                && viewPoint.x < ViewPointRangeHigh && viewPoint.x > ViewPointRangeLow // within x bounds of viewport
                && viewPoint.y < ViewPointRangeHigh && viewPoint.y > ViewPointRangeLow
            ) {
                RaycastHit hit;

                updateAllAgentCollidersForVisibilityCheck(false);

                if (Physics.Raycast(m_Camera.transform.position, point - m_Camera.transform.position, out hit,
                        Vector3.Distance(m_Camera.transform.position, point) - 0.01f, (1 << 8) | (1 << 10))) // reduce distance by slight offset
                {
                    updateAllAgentCollidersForVisibilityCheck(true);
                    return false;
                } else {
                    updateAllAgentCollidersForVisibilityCheck(true);
                    return true;
                }
            }
            return false;
        }

        ///////////////////////////////////////////
        ///////////////// ROTATE //////////////////
        ///////////////////////////////////////////

        // checks if agent is clear to rotate left/right/up/down some number of degrees while holding an object
        public void assertAgentCanRotate(string direction, float degrees) {
            if (direction != "left" && direction != "right" && direction != "up" && direction != "down") {
                throw new ArgumentException("direction must be in {up, right, down, left}");
            }

            if (ItemInHand == null) {
                return;
            }

            BoxCollider bb = ItemInHand.GetComponent<SimObjPhysics>().BoundingBox.GetComponent<BoxCollider>();

            // get world coordinates of object in hand's bounding box corners
            Vector3[] corners = UtilityFunctions.CornerCoordinatesOfBoxColliderToWorld(bb);

            // ok now we have each corner, let's rotate them the specified direction
            Vector3 origin = (direction == "right" || direction == "left" ? m_CharacterController : m_Camera).transform.position;

            // for use with each of the 8 corners of a picked up object's bounding box - returns an array of Vector3 points along the arc of the rotation for a given starting point
            // given a starting Vector3, rotate about an origin point for a total given angle. maxIncrementAngle is the maximum value of the increment between points on the arc.
            // if leftOrRight is true - rotate around Y (rotate left/right), false - rotate around X (look up/down)
            Vector3[] GenerateArcPoints(Vector3 startingPoint) {
                float incrementAngle = degrees / 10f; // divide the total amount we are rotating by 10 to get 10 points on the arc
                Vector3[] arcPoints = new Vector3[11]; // we just always want 10 points in addition to our starting corner position (11 total) to check against per corner
                float currentIncrementAngle;

                // Yawing left (Rotating across XZ plane around Y-pivot)
                for (int i = 0; i < arcPoints.Length; i++) {
                    // move the rotPoint to the current corner's position
                    rotPoint.transform.position = startingPoint;

                    switch (dir) {
                        case "up":
                            currentIncrementAngle = i * -incrementAngle;
                            goto case "look";
                        case "down":
                            currentIncrementAngle = i * incrementAngle;
                            goto case "look";
                        case "left":
                            currentIncrementAngle = i * -incrementAngle;
                            goto case "rotate";
                        case "right":
                            currentIncrementAngle = i * incrementAngle;
                            goto case "rotate";

                        // rotate the rotPoint around the origin the current increment's angle, relative to the correct axis
                        case "look":
                            rotPoint.transform.RotateAround(origin, transform.right, currentIncrementAngle);
                            break;
                        case "rotate":
                            rotPoint.transform.RotateAround(origin, transform.up, currentIncrementAngle);
                            break;
                    }

                    // set the current arcPoint's vector3 to the rotated point
                    arcPoints[i] = rotPoint.transform.position;
                }
                return arcPoints;
            }

            // generate arc points in the positive y axis rotation
            foreach (Vector3 v in corners) {
                Vector3[] pointsOnArc = GenerateArcPoints(v, degrees, dir);

                // raycast from first point in pointsOnArc, stepwise to the last point. If any collisions are hit, immediately return
                for (int i = 0; i < pointsOnArc.Length; i++) {
                    // debug draw spheres to show path of arc
                    // GameObject Sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    // Sphere.transform.position = pointsOnArc[i];
                    // Sphere.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
                    // Sphere.GetComponent<SphereCollider>().enabled = false;

                    RaycastHit hit;

                    // do linecasts from the first point, sequentially, to the last
                    if (i < pointsOnArc.Length - 1) {
                        if (Physics.Linecast(pointsOnArc[i], pointsOnArc[i+1], out hit, 1 << 8 | 1 << 10, QueryTriggerInteraction.Ignore)) {
                            if (hit.transform.GetComponent<SimObjPhysics>()) {
                                // if we hit the item in our hand, skip
                                if (hit.transform.GetComponent<SimObjPhysics>().transform == ItemInHand.transform) {
                                    continue;
                                }
                            }

                            if (hit.transform == this.transform) {
                                // don't worry about clipping the object into this agent
                                continue;
                            }
                            throw new InvalidOperationException(
                                $"a held item: {ItemInHand.transform.GetComponent<SimObjPhysics>().objectID} will collide with something if agent rotates down {degrees}degrees"
                            );
                        }
                    }
                }
            }
        }

        // helper used with rotate right/left.
        // note: this sets actionFinished(true) if successful.
        protected void rotate(float? degrees, string direction, bool manualInteract) {
            assertAgentCanRotate(direction: direction, degrees: degreesFloat);
            if (!manualInteract) {
                DefaultAgentHand();
            }
            switch (direction) {
                case "right":
                    base.RotateRight(degrees: degrees);
                    break;
                case "left":
                    base.RotateLeft(degrees: degrees);
                    break;
            }
        }

        public void RotateRight(float? degrees = null, bool manualInteract = false) {
            rotate(degrees: degrees, direction: "right", manualInteract: manualInteract);
        }

        public void RotateLeft(float? degrees = null, bool manualInteract = false) {
            rotate(degrees: degrees, direction: "left", manualInteract: manualInteract);
        }

        // helper used with look up/down.
        // note: this sets actionFinished(true) if successful.
        protected void look(float degrees, string direction, bool manualInteract) {
            // force the degree increment to the nearest tenths place
            // this is to prevent too small of a degree increment change that could cause float imprecision
            degrees = Mathf.Round(degrees * 10.0f) / 10.0f;

            assertAgentCanRotate(direction: direction, degrees: degrees);

            // let's check if the rotation would put us out of a valid horizon
            // first move the rotPoint to the camera
            rotPoint.transform.position = m_Camera.transform.position;
            rotPoint.transform.rotation = m_Camera.transform.rotation;

            if (direction == "down") {
                rotPoint.Rotate(new Vector3(degrees, 0, 0));
                // note: maxDownwardLookAngle is negative because SignedAngle() returns a... signed angle... so even though the input is LookDown(degrees) with
                // degrees being positive, it still needs to check against this negatively signed direction.
                if (Mathf.Round(Vector3.SignedAngle(rotPoint.transform.forward, m_CharacterController.transform.forward, m_CharacterController.transform.right) * 10.0f) / 10.0f < -maxDownwardLookAngle) {
                    throw new InvalidOperationException("can't look down beyond " + maxDownwardLookAngle + " degrees below the forward horizon");
                }
            } else if (direction == "up") {
                rotPoint.Rotate(new Vector3(-degrees, 0, 0));
                if (Mathf.Round(Vector3.SignedAngle(rotPoint.transform.forward, m_CharacterController.transform.forward, m_CharacterController.transform.right) * 10.0f) / 10.0f > maxUpwardLookAngle) {
                    throw new InvalidOperationException("can't look up beyond " + maxUpwardLookAngle + " degrees above the forward horizon");
                }
            }

            // only default hand if not manually Interacting with things
            if (!manualInteract) {
                DefaultAgentHand();
            }

            // execute the standard LookUp/LookDown
            switch (direction) {
                case "up":
                    base.LookUp(degrees: degrees);
                    break;
                case "down":
                    base.LookDown(degrees: degrees);
                    break;
                default:
                    throw new InvalidOperationException("direction must be either up or down.");
            }
        }

        public void LookDown(float degrees = 30, bool manualInteract = false) {
            look(degrees: degrees, direction: "down");
        }

        public void LookUp(float degrees = 30, bool manualInteract = false) {
            look(degrees: degrees, direction: "up", manualInteract: manualInteract);
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

        //  protected IEnumerator InterpolateRotation(Quaternion targetRotation, float seconds) {
        //     var time = Time.time;
        //     var newTime = time;
        //     while (newTime - time < seconds) {
        //         yield return null;
        //         newTime = Time.time;
        //         var diffSeconds = newTime - time;
        //         var alpha = Mathf.Min(diffSeconds / seconds, 1.0f);
        //         this.transform.rotation = Quaternion.Lerp(this.transform.rotation, targetRotation, alpha);

        //     }
        //     Debug.Log("Rotate action finished! " + (newTime - time) );
        //     //  this.transform.rotation = targetRotation;
        //     actionFinished(true);
        // }

        ///////////////////////////////////////////
        //////////// TELEPORT OBJECT //////////////
        ///////////////////////////////////////////

        // TODO: why aren't these in the base?
        public void TeleportObject(
            string objectId,
            Vector3 position,
            Vector3 rotation,
            bool forceAction = false,
            bool forceKinematic = false,
            bool allowTeleportOutOfHand = false,
            bool makeUnbreakable = false
        ) {
            SimObjPhysics sop = getTargetObject(objectId: objectId, forceAction: true);
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
                } else {
                    actionFinished(true);
                }
            } else {
                actionFinished(false);
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
            SimObjPhysics sop = getTargetObject(objectId: objectId, forceAction: forceAction);

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

                dropContainedObjects(
                    target: sop,
                    reparentContainedObjects: true,
                    forceKinematic: forceKinematic
                );
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

        public void TeleportObjectToFloor(string objectId, float x, float y, float z, Vector3 rotation) {
            SimObjPhysics sop = getTargetObject(objectId: objectId);
            if (ItemInHand != null && sop == ItemInHand.GetComponent<SimObjPhysics>()) {
                throw new InvalidOperationException("Cannot teleport object in hand.");
            }

            Bounds objBounds = new Bounds(
                center: new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity),
                size: new Vector3(-float.PositiveInfinity, -float.PositiveInfinity, -float.PositiveInfinity)
            );
            foreach (Renderer r in sop.GetComponentsInChildren<Renderer>()) {
                if (r.enabled) {
                    objBounds.Encapsulate(r.bounds);
                }
            }

            if (objBounds.min.x == float.PositiveInfinity) {
                throw new InvalidOperationException($"Could not get bounds of {objectId}.");
            }

            float y = getFloorY(x, z);
            sop.transform.position = new Vector3(x, objBounds.extents.y + y + 0.1f, z);
            sop.transform.rotation = Quaternion.Euler(rotation);
            actionFinished(true);
        }

        [ObsoleteAttribute(message: "This action is deprecated. Call ChangeAgentColor(string color) instead.", error: false)]
        public void ChangeAgentColor(float x, float y, float z) {
            ChangeAgentColor(color: $"rgb({x}, {y}, {z})");
        }

        // accepts any HTML string color as input
        public void ChangeAgentColor(string htmlColor) {
            Color targetColor;
            bool successfullyParsed = ColorUtility.TryParseHtmlString(htmlString: htmlColor, color: out targetColor);
            if (!successfullyParsed) {
                throw new ArgumentException("Invalid color! It cannot be parsed as an HTML color.");
            }
            agentManager.UpdateAgentColor(this, targetColor);
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
            if (objectId == null) {
                throw new ArgumentNullException();
            }

            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(objectId)) {
                throw new ArgumentException("Cannot find object with id " + objectId);
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
                    if (Physics.Raycast(testPosition, -transform.up, out hit, 1f, 1 << 8)) {
                        Vector3 viewportPoint = m_Camera.WorldToViewportPoint(hit.point);
                        if (viewportPoint.x >= 0f && viewportPoint.x <= 1f && viewportPoint.y >= 0f && viewportPoint.y <= 1f) {
                            SimObjPhysics hitSop = hit.transform.gameObject.GetComponent<SimObjPhysics>();
                            if (hitSop && hitSop.ObjectID == objectId) {
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
                throw new ArgumentOutOfRangeException("objectVariation (int) must be in [0:4]");
            }

            if (physicsSceneManager.ManipulatorReceptacles == null ||
                physicsSceneManager.ManipulatorReceptacles.Length == 0
            ) {
                throw new InvalidOperationException("Scene does not have manipulator receptacles set.");
            }

            // float[] yoffsets = {-0.1049f, -0.1329f, -0.1009f, -0.0969f, -0.0971f};
            float[] yoffsets = {0f, -0.0277601f, 0f, 0f, 0f};

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
            if (physicsSceneManager.ManipulatorBooks == null ||
                physicsSceneManager.ManipulatorBooks.Length == 0
            ) {
                throw new InvalidOperationException("Scene does not have manipulator books set.");
            }

            if (objectVariation < 0) {
                throw new ArgumentOutOfRangeException("objectVariation must be >= 0");
            }

            float yoffset = 0.19f;

            for (int i = 0; i < 5; i++) {
                physicsSceneManager.ManipulatorBooks[i].transform.gameObject.SetActive(
                    ((objectVariation >> i) % 2) == 1
                );
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
                throw new ArgumentException("Cannot initialize table in scene " + scene);
            }

            if (objectVariation < 0 || objectVariation > 4) {
                throw new ArgumentOutOfRangeException("Invalid table variation.");
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
            if (objectId == null) {
                throw new ArgumentNullException();
            }

            if (z <= 0.0f) {
                throw ArgumentOutOfRangeException("Interactable distance (z) must be > 0");
            }

            SimObjPhysics targetObject = getTargetObject(objectId: objectId, forceAction: true);

            Vector3 savedObjectPosition = targetObject.transform.position;
            Quaternion savedObjectRotation = targetObject.transform.rotation;
            Vector3 savedAgentPosition = transform.position;
            Quaternion savedAgentRotation = transform.rotation;

            targetObject.transform.rotation = Quaternion.identity;
            transform.rotation = Quaternion.identity;

            float objectRad = GetXZRadiusOfObject(targetObject);

            var sb = new System.Text.StringBuilder();
            int halfWidth = 1 + ((int) Math.Round((objectRad + z + m_CharacterController.radius) / gridSize));
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

        public void RandomlyCreateLiftedFurniture(float z, float y, string objectType, int objectVariation = 0) {
            if (z < 0.25f) {
                throw new ArgumentOutOfRangeException("z must be at least 0.25");
            }
            if (y == 0.0f) {
                throw new ArgumentOutOfRangeException("y must be non-zero");
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
                    objectType, objectVariation, reachablePositions
                );
            } catch (Exception e) {
                for (int i = 0; i < this.agentManager.agents.Count; i++) {
                    var agent = this.agentManager.agents[i];
                    agent.transform.position = oldAgentPositions[i];
                    agent.transform.rotation = oldAgentRotations[i];
                }
                throw new InvalidOperationException($"Failed to create object of type {objectType}.");
            }
            objectCreated.GetComponent<Rigidbody>().isKinematic = true;

            // TODO: fix moveObject to try() catch()
            bool objectFloating = moveObject(
                objectCreated,
                objectCreated.transform.position + new Vector3(0f, y, 0f)
            );

            float[] rotationsArr = { 0f, 90f, 180f, 270f };
            List<float> rotations = rotationsArr.ToList();

            bool placementSuccess = false;
            for (int i = 0; i < 10; i++) {
                if (objectFloating) {
                    List<Vector3> candidatePositionsList = new List<Vector3>();
                    foreach (Vector3 p in reachablePositions) {
                        transform.position = p;
                        if (!isAgentCapsuleColliding(collidersToIgnoreDuringMovement) && distanceToObject(objectCreated) <= z) {
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
                                var agent = (PhysicsRemoteFPSAgentController) agentManager.agents[j];
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
                    // TODO: fix with try catch.
                    objectFloating = moveObject(
                        objectCreated,
                        objectCreated.transform.position + new Vector3(0f, y, 0f)
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
                throw new InvalidOperationException("Could not find a place to put the object after 10 iterations. " + errorMessage);
            }
            actionFinished(true, objectCreated.ObjectID);
        }

        public void CollidersObjectCollidingWith(string objectId) {
            GameObject go = getTargetObject(objectId: objectId, forceAction: true).gameObject;
            List<string> collidingWithNames = new List<string>();
            foreach (Collider c in UtilityFunctions.collidersObjectCollidingWith(go)) {
                collidingWithNames.Add(c.name);
                #if UNITY_EDITOR
                    Debug.Log(c.name);
                #endif
            }
            actionFinished(true, collidingWithNames);
        }

        ///////////////////////////////////////////
        //////// MOVE LIFTED OBJECT AHEAD /////////
        ///////////////////////////////////////////

        protected void moveObject(
            SimObjPhysics sop,
            Vector3 targetPosition,
            bool snapToGrid=false,
            HashSet<Transform> ignoreCollisionWithTransforms=null
        ) {
            Vector3 lastPosition = sop.transform.position;
            if (snapToGrid) {
                float mult = 1.0f / gridSize;
                float gridX = Convert.ToSingle(Math.Round(targetPosition.x * mult) / mult);
                float gridZ = Convert.ToSingle(Math.Round(targetPosition.z * mult) / mult);
                targetPosition = new Vector3(gridX, targetPosition.y, gridZ);
            }

            Vector3 dir = targetPosition - sop.transform.position;
            RaycastHit[] sweepResults = UtilityFunctions.CastAllPrimitiveColliders(
                sop.gameObject, targetPosition - sop.transform.position, dir.magnitude,
                1 << 8 | 1 << 10, QueryTriggerInteraction.Ignore
            );

            if (sweepResults.Length > 0) {
                foreach (RaycastHit hit in sweepResults) {
                    if (ignoreCollisionWithTransforms == null || !ignoreCollisionWithTransforms.Contains(hit.transform)) {
                        throw new InvalidOperationException(hit.transform.name + " is in the way of moving " + sop.ObjectID);
                    }
                }
            }
            sop.transform.position = targetPosition;
        }

        protected bool moveLiftedObjectHelper(string objectId, Vector3 relativeDir, float maxAgentsDistance, bool markActionFinished) {
            SimObjPhysics objectToMove = getTargetObject(objectId: objectId, forceAction: true);
            Vector3 oldPosition = objectToMove.transform.position;
            moveObject(objectToMove, objectToMove.transform.position + relativeDir, true);
            if (maxAgentsDistance > 0.0f) {
                for (int i = 0; i < agentManager.agents.Count; i++) {
                    if (((PhysicsRemoteFPSAgentController) agentManager.agents[i]).distanceToObject(objectToMove) > maxAgentsDistance) {
                        objectToMove.transform.position = oldPosition;
                        throw new InvalidOperationException("Would move object beyond max distance from agent " + i.ToString());
                    }
                }
            }

            if (markActionFinished) {
                actionFinished(true);
            }
        }

        public void MoveLiftedObjectAhead(string objectId, float maxAgentsDistance = -1, float? moveMagnitude = null) {
            float mag = moveMagnitude == null || (float) moveMagnitude == 0 ? gridSize : (float) moveMagnitude;
            moveLiftedObjectHelper(
                objectId: objectId,
                relativeDir: mag * transform.forward,
                maxAgentsDistance: maxAgentsDistance,
                markActionFinished: true
            );
        }

        public void MoveLiftedObjectRight(string objectId, float maxAgentsDistance = -1, float? moveMagnitude = null) {
            float mag = moveMagnitude == null ? gridSize : moveMagnitude;
            moveLiftedObjectHelper(
                objectId: objectId,
                relativeDir: mag * transform.right,
                maxAgentsDistance: maxAgentsDistance,
                markActionFinished: true
            );
        }

        public void MoveLiftedObjectLeft(string objectId, float maxAgentsDistance = -1, float? moveMagnitude = null) {
            float mag = moveMagnitude == null ? gridSize : moveMagnitude;
            moveLiftedObjectHelper(
                objectId: objectId,
                relativeDir: -mag * transform.right,
                maxAgentsDistance: maxAgentsDistance,
                markActionFinished: true
            );
            actionFinished(true);
        }

        public void MoveLiftedObjectBack(string objectId, float maxAgentsDistance = -1, float? moveMagnitude = null) {
            float mag = moveMagnitude == null ? gridSize : moveMagnitude;
            moveLiftedObjectHelper(
                objectId: objectId,
                relativeDir: - mag * transform.forward,
                maxAgentsDistance: maxAgentsDistance,
                markActionFinished: true
            );
        }

        ///////////////////////////////////////////
        /////// ROTATE LIFTED OBJECT AHEAD ////////
        ///////////////////////////////////////////

        public void RotateLiftedObjectRight(string objectId, bool forceAction = false, float maxAgentsDistance = 0) {
            SimObjPhysics sop = getTargetObject(objectId: objectId, forceAction: forceAction);
            if (ItemInHand != null && sop == ItemInHand.GetComponent<SimObjPhysics>()) {
                throw new InvalidOperationException("Cannot rotate lifted object in hand.");
            }

            Quaternion oldRotation = sop.transform.rotation;
            sop.transform.rotation = Quaternion.Euler(new Vector3(0.0f, (float) Math.Round((sop.transform.eulerAngles.y + 90f) % 360), 0.0f));;
            if (!forceAction) {
                if (maxAgentsDistance > 0.0f) {
                    for (int i = 0; i < agentManager.agents.Count; i++) {
                        if (((PhysicsRemoteFPSAgentController) agentManager.agents[i]).distanceToObject(sop) > maxAgentsDistance) {
                            sop.transform.rotation = oldRotation;
                            throw new InvalidOperationException("Would move object beyond max distance from agent " + i.ToString());
                        }
                    }
                }
                if (UtilityFunctions.isObjectColliding(sop.gameObject, null, 0.0f)) {
                    sop.transform.rotation = oldRotation;
                    throw new InvalidOperationException(sop.ObjectID + " is colliding after teleport.");
                }
                foreach (BaseFPSAgentController agent in agentManager.agents) {
                    // This check is silly but seems necessary to appease unity
                    // as unity doesn't realize the object collides with the agents in
                    // the above checks in some cases.
                    if (((PhysicsRemoteFPSAgentController) agent).isAgentCapsuleCollidingWith(sop.gameObject)) {
                        sop.transform.rotation = oldRotation;
                        throw new InvalidOperationException(sop.ObjectID + " is colliding with an agent after rotation.");
                    }
                }
            }
            actionFinished(true);
        }

        ///////////////////////////////////////////
        ///////// MOVE AGENTS WITH OBJECT /////////
        ///////////////////////////////////////////

        public void moveAgentsWithObject(SimObjPhysics objectToMove, Vector3 d, bool snapToGrid = true) {
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

            void moveObjectWithTeleport(Vector3 targetPosition) {
                Vector3 lastPosition = objectToMove.transform.position;

                if (snapToGrid) {
                    float mult = 1.0f / gridSize;
                    float gridX = Convert.ToSingle(Math.Round(targetPosition.x * mult) / mult);
                    float gridZ = Convert.ToSingle(Math.Round(targetPosition.z * mult) / mult);
                    targetPosition = new Vector3(gridX, targetPosition.y, gridZ);
                }

                Vector3 oldPosition = objectToMove.transform.position;
                objectToMove.transform.position = targetPosition;

                if (UtilityFunctions.isObjectColliding(objectToMove.gameObject)) {
                    objectToMove.transform.position = oldPosition;
                    throw new InvalidOperationException(objectToMove.ObjectID + " is colliding after teleport.");
                }

                foreach (BaseFPSAgentController agent in agentManager.agents) {
                    // This check is stupid but seems necessary to appease the unity gods
                    // as unity doesn't realize the object collides with the agents in
                    // the above checks in some cases.
                    if (((PhysicsRemoteFPSAgentController)agent).isAgentCapsuleCollidingWith(objectToMove.gameObject)) {
                        objectToMove.transform.position = oldPosition;
                        throw new InvalidOperationException(objectToMove.ObjectID + " is colliding with an agent after movement.");
                    }
                }
            }

            try {
                Physics.autoSimulation = false;
                while (agentMovePQ.Count > 0 || !objectMoved) {
                    if (agentMovePQ.Count == 0) {
                        moveObjectWithTeleport(targetPosition: objectToMove.transform.position + d);
                        Physics.Simulate(0.04f);
                        break;
                    } else {
                        PhysicsRemoteFPSAgentController nextAgent = (PhysicsRemoteFPSAgentController) agentMovePQ.First;
                        float agentPriority = -agentMovePQ.GetPriority(nextAgent);

                        if (!objectMoved && agentPriority < objectPriority) {
                            moveObjectWithTeleport(targetPosition: objectToMove.transform.position + d);
                            Physics.Simulate(0.04f);
                            objectMoved = true;
                        } else {
                            agentMovePQ.Dequeue();
                            nextAgent.moveInDirection(d, "", -1, false, false, agentsAndObjColliders);
                            Physics.Simulate(0.04f);
                        }
                    }
                }
            } catch (InvalidOperationException e) {
                Physics.autoSimulation = true;
                for (int i = 0; i < agentManager.agents.Count; i++) {
                    agentManager.agents[i].transform.position = startAgentPositions[i];
                }
                objectToMove.transform.position = startObjectPosition;
                throw new InvalidOperationException(e.Message);
            }
        }

        public void MoveAgentsAheadWithObject(string objectId, float moveMagnitude = 0) {
            SimObjPhysics objectToMove = getTargetObject(objectId: objectId, forceAction: true);
            moveMagnitude = moveMagnitude > 0 ? moveMagnitude : gridSize;
            actionFinished(moveAgentsWithObject(objectToMove, transform.forward * moveMagnitude));
        }

        public void MoveAgentsLeftWithObject(string objectId, float moveMagnitude = 0) {
            SimObjPhysics objectToMove = getTargetObject(objectId: objectId, forceAction: true);
            moveMagnitude = moveMagnitude > 0 ? moveMagnitude : gridSize;
            actionFinished(moveAgentsWithObject(objectToMove, -transform.right * moveMagnitude));
        }

        public void MoveAgentsRightWithObject(string objectId, float moveMagnitude = 0) {
            SimObjPhysics objectToMove = getTargetObject(objectId: objectId, forceAction: true);
            moveMagnitude = moveMagnitude > 0 ? moveMagnitude : gridSize;
            actionFinished(moveAgentsWithObject(objectToMove, transform.right * moveMagnitude));
        }

        public void MoveAgentsBackWithObject(string objectId, float moveMagnitude = 0) {
            SimObjPhysics objectToMove = getTargetObject(objectId: objectId, forceAction: true);
            moveMagnitude = moveMagnitude > 0 ? moveMagnitude : gridSize;
            actionFinished(moveAgentsWithObject(objectToMove, -transform.forward * moveMagnitude));
        }

        ///////////////////////////////////////////
        //////////////// TELEPORT /////////////////
        ///////////////////////////////////////////

        public override void TeleportFull(ServerAction action) {
            targetTeleport = new Vector3(action.x, action.y, action.z);

            if (action.forceAction) {
                DefaultAgentHand();
                transform.position = targetTeleport;
                transform.rotation = Quaternion.Euler(new Vector3(0.0f, action.rotation.y, 0.0f));
                if (action.standing) {
                    m_Camera.transform.localPosition = standingLocalCameraPosition;
                } else {
                    m_Camera.transform.localPosition = crouchingLocalCameraPosition;
                }
                m_Camera.transform.localEulerAngles = new Vector3(action.horizon, 0.0f, 0.0f);
            } else {
                if (!agentManager.SceneBounds.Contains(targetTeleport)) {
                    errorMessage = "Teleport target out of scene bounds.";
                    actionFinished(false);
                    return;
                }

                Vector3 oldPosition = transform.position;
                Quaternion oldRotation = transform.rotation;
                Vector3 oldLocalHandPosition = new Vector3();
                Quaternion oldLocalHandRotation = new Quaternion();
                if (ItemInHand != null) {
                    oldLocalHandPosition = ItemInHand.transform.localPosition;
                    oldLocalHandRotation = ItemInHand.transform.localRotation;
                }
                Vector3 oldCameraLocalEulerAngle = m_Camera.transform.localEulerAngles;
                Vector3 oldCameraLocalPosition = m_Camera.transform.localPosition;

                DefaultAgentHand();
                transform.position = targetTeleport;

                // apply gravity after teleport so we aren't floating in the air
                Vector3 m = new Vector3();
                m.y = Physics.gravity.y * this.m_GravityMultiplier;
                m_CharacterController.Move(m);

                transform.rotation = Quaternion.Euler(new Vector3(0.0f, action.rotation.y, 0.0f));
                if (action.standing) {
                    m_Camera.transform.localPosition = standingLocalCameraPosition;
                } else {
                    m_Camera.transform.localPosition = crouchingLocalCameraPosition;
                }
                m_Camera.transform.localEulerAngles = new Vector3(action.horizon, 0.0f, 0.0f);

                bool agentCollides = isAgentCapsuleColliding(
                    collidersToIgnore: collidersToIgnoreDuringMovement,
                    includeErrorMessage: true
                );

                bool handObjectCollides = isHandObjectColliding(true);
                if (handObjectCollides && !agentCollides) {
                    errorMessage = "Cannot teleport due to hand object collision.";
                }

                if (agentCollides || handObjectCollides) {
                    if (ItemInHand != null) {
                        ItemInHand.transform.localPosition = oldLocalHandPosition;
                        ItemInHand.transform.localRotation = oldLocalHandRotation;
                    }
                    transform.position = oldPosition;
                    transform.rotation = oldRotation;
                    m_Camera.transform.localPosition = oldCameraLocalPosition;
                    m_Camera.transform.localEulerAngles = oldCameraLocalEulerAngle;
                    actionFinished(false);
                    return;
                }
            }

            Vector3 v = new Vector3();
            v.y = Physics.gravity.y * this.m_GravityMultiplier;
            m_CharacterController.Move(v);

            snapAgentToGrid();
            actionFinished(true);
        }

        public override void Teleport(ServerAction action) {
            action.horizon = Convert.ToInt32(m_Camera.transform.localEulerAngles.x);
            action.standing = isStanding();
            if (!action.rotateOnTeleport) {
                action.rotation = transform.eulerAngles;
            }
            TeleportFull(action);
        }

        ///////////////////////////////////////////
        /////////////// MOVE AGENT ////////////////
        ///////////////////////////////////////////

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


        // for all translational movement, check if the item the player is holding will hit anything, or if the agent will hit anything
        // NOTE: (XXX) All four movements below no longer use base character controller Move() due to doing initial collision blocking
        // checks before actually moving. Previously we would moveCharacter() first and if we hit anything reset, but now to match
        // Luca's movement grid and valid position generation, simple transform setting is used for movement instead.

        // XXX revisit what movement means when we more clearly define what "continuous" movement is
        protected void moveInDirection(
            Vector3 direction,
            float? moveMagnitude,
            bool forceAction,
            bool manualInteract,
            HashSet<Collider> ignoreColliders
        ) {
            float mag = moveMagnitude == null ? gridSize : (float) moveMagnitude;
            if (mag <= 0) {
                throw new ArgumentOutOfRangeException("moveMagnitude must be >= 0");
            }

            if (snapToGrid && Mathf.Abs(mag % gridSize) >= 1e-3f) {
                throw new ArgumentOutOfRangeException($"moveMagnitude % gridSize != 0 (moveMagnitude: {moveMagnitude}, gridSize: {gridSize}). Use controller.reset(..., snapToGrid=False to set this off.).");
            }

            Vector3 targetPosition = transform.position + direction * mag;
            float angle = Vector3.Angle(transform.forward, Vector3.Normalize(direction));

            float right = Vector3.Dot(transform.right, direction);
            if (right < 0) {
                angle = 360f - angle;
            }
            int angleInt = Mathf.RoundToInt(angle) % 360;

            // forceAction = true allows ignoring movement restrictions caused by held objects
            if (!checkIfSceneBoundsContainTargetPosition(targetPosition) ||
                !CheckIfItemBlocksAgentMovement(direction.magnitude, angleInt, forceAction) ||
                !CheckIfAgentCanMove(direction.magnitude, angleInt, ignoreColliders)
            ) {
                throw new InvalidOperationException("Unable to move agent in direction.");
            }

            // only default hand if not manually interacting with things
            if (!manualInteract) {
                DefaultAgentHand();
            }

            transform.position = targetPosition;
            snapAgentToGrid();
        }

        // TODO: set allowAgentsToIntersect should be set upon Initialization.. (ask Luca)
        // seem to only be used in cordial-sync.
        public void MoveRight(
            float? moveMagnitude = null,
            bool forceAction = false,
            bool manualInteract = false,
            bool allowAgentsToIntersect = false
        ) {
            moveInDirection(
                direction: transform.right,
                moveMagnitude: moveMagnitude,
                forceAction: forceAction,
                manualInteract: manualInteract,
                ignoreColliders: allowAgentsToIntersect ? allAgentColliders() : null
            );
            actionFinished(true);
        }

        public void MoveLeft(
            float? moveMagnitude = null,
            bool forceAction = false,
            bool manualInteract = false,
            bool allowAgentsToIntersect = false
        ) {
            moveInDirection(
                direction: -transform.right,
                moveMagnitude: moveMagnitude,
                forceAction: forceAction,
                manualInteract: manualInteract,
                ignoreColliders: allowAgentsToIntersect ? allAgentColliders() : null
            );
            actionFinished(true);
        }

        public void MoveBack(
            float? moveMagnitude = null,
            bool forceAction = false,
            bool manualInteract = false,
            bool allowAgentsToIntersect = false
        ) {
            moveInDirection(
                direction: -transform.forward,
                moveMagnitude: moveMagnitude,
                forceAction: forceAction,
                manualInteract: manualInteract,
                ignoreColliders: allowAgentsToIntersect ? allAgentColliders() : null
            );
            actionFinished(true);
        }

        public void MoveAhead(
            float? moveMagnitude = null,
            bool forceAction = false,
            bool manualInteract = false,
            bool allowAgentsToIntersect = false
        ) {
            moveInDirection(
                direction: transform.forward,
                moveMagnitude: moveMagnitude,
                forceAction: forceAction,
                manualInteract: manualInteract,
                ignoreColliders: allowAgentsToIntersect ? allAgentColliders() : null
            );
            actionFinished(true);
        }

        ///////////////////////////////////////////
        ////////////// PUSH OBJECT ////////////////
        ///////////////////////////////////////////

        // syntactic sugar for DirectionalPush(pushAngle: 0)
        public void PushObject(string objectId, float moveMagnitude, bool forceAction = false) {
            DirectionalPush(objectId: objectId, moveMagnitude: moveMagnitude, pushAngle: 0, forceAction: forceAction);
        }

        // syntactic sugar for DirectionalPush(pushAngle: 0)
        public void PushObject(float x, float y, float moveMagnitude, bool forceAction = false) {
            DirectionalPush(x: x, y: y, moveMagnitude: moveMagnitude, pushAngle: 0, forceAction: forceAction);
        }

        ///////////////////////////////////////////
        ////////////// PULL OBJECT ////////////////
        ///////////////////////////////////////////

        // syntactic sugar for DirectionalPush(pushAngle: 180)
        public void PullObject(string objectId, float moveMagnitude, bool forceAction = false) {
            DirectionalPush(objectId: objectId, moveMagnitude: moveMagnitude, pushAngle: 180, forceAction: forceAction);
        }

        // syntactic sugar for DirectionalPush(pushAngle: 180)
        public void PullObject(float x, float y, float moveMagnitude, bool forceAction = false) {
            DirectionalPush(x: x, y: y, moveMagnitude: moveMagnitude, pushAngle: 180, forceAction: forceAction);
        }

        ///////////////////////////////////////////
        /////////// DIRECTIONAL PUSH //////////////
        ///////////////////////////////////////////

        protected bool canBePushed(SimObjPhysics target) {
            return (target.PrimaryProperty == SimObjPrimaryProperty.CanPickup ||
                    target.PrimaryProperty == SimObjPrimaryProperty.Moveable
            );
        }

        // pass in a magnitude and an angle offset to push an object relative to agent forward
        protected void directionalPush(
            SimObjPhysics target,
            float moveMagnitude,
            float pushAngle,
            bool markActionFinished
        ) {
            if (target == null) {
                throw new ArgumentNullException();
            }

            if (ItemInHand != null && target.ObjectId == ItemInHand.GetComponent<SimObjPhysics>().objectID) {
                throw new InvalidOperationException("Please use Throw for an item in the Agent's Hand");
            }

            if (!canBePushed(target)) {
                throw new InvalidOperationException("Target Primary Property type incompatible with push/pull");
            }

            // The direction vector to push the target object defined by pushAngle
            // degrees clockwise from the agent's forward. Note: pushAngle % 360 < 0 iff pushAngle < 0.
            pushAngle = pushAngle < 0 ? 360 + pushAngle % 360 : pushAngle % 360;

            // find the Direction to push the object based on PushAngle
            Vector3 agentForward = transform.forward;
            float pushAngleInRadians = pushAngle * Mathf.PI / -180; // using -180 so positive PushAngle values go clockwise

            Vector3 direction = new Vector3(
                x: agentForward.x * Mathf.Cos(pushAngleInRadians) - agentForward.z * Mathf.Sin(pushAngleInRadians),
                y: 0,
                z: agentForward.x * Mathf.Sin(pushAngleInRadians) + agentForward.z * Mathf.Cos(pushAngleInRadians)
            );

            target.GetComponent<Rigidbody>().isKinematic = false;
            sopApplyForce(dir: direction, magnitude: moveMagnitude, sop: target, markActionFinished: true);
        }

        public void DirectionalPush(string objectId, float moveMagnitude, bool forceAction = false) {
            SimObjPhysics target = getTargetObject(objectId: objectId, forceAction: forceAction);
            directionalPush(target: target, moveMagnitude: moveMagnitude, pushAngle: pushAngle, markActionFinished: true);
        }

        public void DirectionalPush(float x, float y, float moveMagnitude, float pushAngle, bool forceAction = false) {
            SimObjPhysics target = getTargetObject(x: x, y: y, forceAction: forceAction);
            directionalPush(target: target, moveMagnitude: moveMagnitude, pushAngle: pushAngle, markActionFinished: true);
        }

        ///////////////////////////////////////////
        ////// (DEPRECATED) ApplyForceObject //////
        ///////////////////////////////////////////

        [ObsoleteAttribute(message: "This action is deprecated. Call DirectionalPush, PushObject, or PullObject instead.", error: false)]
        public void ApplyForceObject(string objectId, float moveMagnitude, int z, bool forceAction = false) {
            switch (z) {
                case 1:
                    PushObject(objectId: objectId, moveMagnitude: moveMagnitude, forceAction: forceAction);
                    break;
                case -1:
                    PushObject(objectId: objectId, moveMagnitude: moveMagnitude, forceAction: forceAction);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("z must be in {1, -1}. Also, this is obsolete! Use PushObject, PullObject, or DirectionalPush");
                    break;
            }
        }

        [ObsoleteAttribute(message: "This action is deprecated. Call DirectionalPush, PushObject, or PullObject instead.", error: false)]
        public void ApplyForceObject(float x, float y, float moveMagnitude, int z, bool forceAction = false) {
            switch (z) {
                case 1:
                    PushObject(x: x, y: y, moveMagnitude: moveMagnitude, forceAction: forceAction);
                    break;
                case -1:
                    PushObject(x: x, y: y, moveMagnitude: moveMagnitude, forceAction: forceAction);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("z must be in {1, -1}. Also, this is obsolete! Use PushObject, PullObject, or DirectionalPush");
                    break;
            }
        }

        ///////////////////////////////////////////
        /////////////  PHYSICS TIMING /////////////
        ///////////////////////////////////////////

        // pause physics autosimulation! Automatic physics simulation can be resumed using the UnpausePhysicsAutoSim() action.
        // additionally, auto simulation will automatically resume from the LateUpdate() check on AgentManager.cs - if the scene has come to rest, physics autosimulation will resume
        public void PausePhysicsAutoSim() {
            Physics.autoSimulation = false;
            physicsSceneManager.physicsSimulationPaused = true;
            actionFinished(true);
        }

        public void AdvancePhysicsStep(
            float timeStep = 0.02f,
            float? simSeconds = null,
            bool allowAutoSimulation = false
        ) {
            if ((!allowAutoSimulation) && Physics.autoSimulation) {
                throw new InvalidOperationException(
                    "AdvancePhysicsStep can only be called if Physics AutoSimulation is currently " +
                    "paused or if you have passed allowAutoSimulation=true! Either use the" +
                    " PausePhysicsAutoSim() action first, or if you already used it, Physics" +
                    " AutoSimulation has been turned back on already."
                );
            }

            if (timeStep <= 0.0f || timeStep > 0.05f) {
                throw new ArgumentOutOfRangeException(
                    "Please use a timeStep between 0.0f and 0.05f. Larger timeSteps produce inconsistent simulation results."
                );
            }

            if (!simSeconds.HasValue) {
                simSeconds = timeStep;
            }

            if (simSeconds.Value < 0.0f) {
                throw new ArgumentOutOfRangeException($"simSeconds must be non-negative (simSeconds=={simSeconds}).");
            }

            bool oldPhysicsAutoSim = Physics.autoSimulation;
            Physics.autoSimulation = false;

            while (simSeconds.Value > 0.0f) {
                simSeconds = simSeconds.Value - timeStep;
                if (simSeconds.Value <= 0) {
                    // This is necessary to keep lastVelocity up-to-date for all sim objects and is
                    // called just before the last physics simulation step.
                    Rigidbody[] rbs = FindObjectsOfType(typeof(Rigidbody)) as Rigidbody[];
                    foreach (Rigidbody rb in rbs) {
                        if (rb.GetComponentInParent<SimObjPhysics>()) {
                            SimObjPhysics sop = rb.GetComponentInParent<SimObjPhysics>();
                            sop.lastVelocity = Math.Abs(rb.angularVelocity.sqrMagnitude + rb.velocity.sqrMagnitude);
                        }
                    }
                }

                // pass in the timeStep to advance the physics simulation
                Physics.Simulate(timeStep);
                this.AdvancePhysicsStepCount++;
            }

            Physics.autoSimulation = oldPhysicsAutoSim;
            actionFinished(true);
        }

        // Use this to immediately un-pause physics autosimulation and allow physics to resolve automatically like normal
        public void UnpausePhysicsAutoSim() {
            Physics.autoSimulation = true;
            physicsSceneManager.physicsSimulationPaused = false;
            actionFinished(true);
        }

        protected void sopApplyForce(Vector3 dir, float magnitude, SimObjPhysics sop, bool markActionFinished, float length = 0) {
            // apply force, return action finished immediately
            sop.ApplyForce(dir: dir, magnitude: magnitude);
            if (physicsSceneManager.physicsSimulationPaused && length > 0) {
                giveTouchFeedback(didHandTouchSomething: true, objectId: sop.ObjectID, armsLength: length);
                if (markActionFinished) {
                    actionFinished(true, feedback);
                }
            } else if (physicsSceneManager.physicsSimulationPaused) {
                if (markActionFinished) {
                    actionFinished(true);
                }
            } else {
                // if physics is automatically being simulated, use coroutine rather than returning actionFinished immediately
                StartCoroutine(checkIfObjectHasStoppedMoving(sop: sop, length: length));
            }
        }

        // used to check if an specified sim object has come to rest
        // set useTimeout bool to use a faster time out
        protected IEnumerator checkIfObjectHasStoppedMoving(
            SimObjPhysics sop,
            float length,
            bool markActionFinished,
            bool useTimeout = false
        ) {
            if (sop == null) {
                throw new ArgumentNullException("SimObject must be non-null!");
            }

            // yield for the physics update to make sure this yield is consistent regardless of framerate
            yield return new WaitForFixedUpdate();

            float startTime = Time.time;
            float waitTime = timeToWaitForObjectsToComeToRest;

            if (useTimeout) {
                waitTime = 1.0f;
            }

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
                throw new InvalidOperationException("object couldn't come to rest");
            }

            // we are past the wait time threshold, so force object to stop moving before
            // rb.velocity = Vector3.zero;
            // rb.angularVelocity = Vector3.zero;
            // rb.Sleep();

            // return to metadatawrapper.actionReturn if an object was touched during this interaction
            if (length != 0.0f) {
                // force object to stop moving
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.Sleep();

                if (markActionFinished) {
                    TouchFeedback feedback = giveTouchFeedback(didHandTouchSomething: true, objectId: sop.objectID, armsLength: length);
                    actionFinished(true, feedback);
                }
            } else {
                // if passed in length is 0, don't return feedback cause not all actions need that
                DefaultAgentHand();
                if (markActionFinished) {
                    actionFinished(true, "object settled after: " + (Time.time - startTime));
                }
            }
        }

        ///////////////////////////////////////////
        /////////  TOUCH THEN APPLY FORCE /////////
        ///////////////////////////////////////////

        // TODO: can we simplify this actionReturn setup? Seems super complicated and uniquely different..
        // Specifically, looking at using getTargetObject.
        public void TouchThenApplyForce(
            float x,
            float y,
            float moveMagnitude,
            Vector3 direction,
            float? handDistance = null
        ) {
            if (x < 0 || x > 1 || y < 0 || y > 1) {
                throw new ArgumentOutOfRangeException("x/y must be in [0:1]");
            }

            float handDistanceFloat = maxVisibleDistance;
            if (handDistance != null) {
                handDistanceFloat = (float) handDistance;
            }
            if (handDistanceFloat <= 0) {
                throw new ArgumentOutOfRangeException("handDistance must be > 0.");
            }

            // NOTE: this does nearly the same thing as getTargetObject, but we care a lot
            // more about the returns from the intermediate steps here. See getTargetObject
            // for more on what's going on.
            float x = x;
            float y = 1.0f - y; 
            Ray ray = m_Camera.ViewportPointToRay(new Vector3(x, y, 0.0f));
            RaycastHit hit;
            bool hitSomething = Physics.Raycast(
                origin: ray,
                hit: out hit,
                maxDistance: handDistanceFloat,
                layerMask: 1 << 0 | 1 << 8 | 1 << 10,
                queryTriggerInteraction: QueryTriggerInteraction.Ignore
            );

            if (!hitSomething) {
                // get ray.origin, multiply handDistance with ray.direction, add to origin to get the final point
                // if the final point was out of range, return actionFinished false, otherwise return actionFinished true with feedback
                Vector3 testPosition = ((handDistanceFloat * ray.direction) + ray.origin);
                if (!isPositionInteractable(position: testPosition)) {
                    giveTouchFeedback(didHandTouchSomething: false, armsLength: handDistanceFloat);
                    throw new InvalidOperationException("The position the hand would have moved to is outside the agent's max interaction range.");
                }

                // the nothing hit was not out of range, but still nothing was hit
                TouchFeedback feedback = giveTouchFeedback(didHandTouchSomething: false, armsLength: handDistanceFloat);
                actionFinished(success: true, actionReturn: feedback);
                return;
            }

            // if something was touched, actionFinished(true) always
            if (!hit.transform.GetComponent<SimObjPhysics>()) {
                // raycast hit something but it wasn't a sim object
                TouchFeedback feedback = giveTouchFeedback(didHandTouchSomething: false, armsLength: handDistanceFloat);
                actionFinished(success: true, actionReturn: feedback, errorMessage: "Structure touched, not SimObject");
                return;
            }

            // wait! First check if the point hit is withing visibility bounds (camera viewport, max distance etc)
            // this should basically only happen if the handDistance value is too big
            if (!isPositionInteractable(position: hit.point)) {
                giveTouchFeedback(didHandTouchSomething: false, armsLength: handDistanceFloat);
                throw new InvalidOperationException("Object successfully hit, but it is outside of the Agent's interaction range");
            }

            // if the object is a sim object, apply force now!
            SimObjPhysics target = hit.transform.GetComponent<SimObjPhysics>();
            if (!canBePushed(target)) {
                TouchFeedback feedback = giveTouchFeedback(didHandTouchSomething: false, armsLength: handDistanceFloat);
                actionFinished(success: false, actionReturn: feedback);
                return;
            }

            sopApplyForce(
                dir: transform.TransformDirection(direction),
                magnitude: moveMagnitude,
                sop: target,
                markActionFinished: true,
                length: hit.distance
            );
        }

        // for use with TouchThenApplyForce feedback return
        public struct TouchFeedback {
            public bool didHandTouchSomething; // did the hand touch something or did it hit nothing?
            public string objectId; // id of object touched, if it is a sim object
            public float armsLength; // the amount the hand moved from it's starting position to hit the object touched
        }

        protected void giveTouchFeedback(bool didHandTouchSomething, float armsLength, string objectId = null) {
            TouchFeedback feedback = new TouchFeedback() {
                didHandTouchSomething = didHandTouchSomething,
                objectId = objectId,
                armsLength = armsLength
            };

            #if UNITY_EDITOR
                print("didHandTouchSomething: " + feedback.didHandTouchSomething);
                print("object id: " + feedback.objectId);
                print("armslength: " + feedback.armsLength);
            #endif

            actionReturn = feedback;
        }

        ///////////////////////////////////////////
        ///////////// OPEN WITH HAND //////////////
        ///////////////////////////////////////////

        // H&S action
        [ObsoleteAttribute(message: "This action is deprecated. Call OpenObject(x, y, forceAction=true) instead.", error: false)]
        public void OpenWithHand(float x, float y, float z) {
            // z direction specifies the forward distance that you want to aim at.
            Vector3 direction = (
                transform.forward * z +
                transform.right * x +
                transform.up * y
            );
            direction.Normalize();

            SimObjPhysics target = getTargetObject(
                origin: AgentHand.transform.position,
                direction: direction,
                forceAction: z > 10f // originally, the raycast capped at 10f
            );

            openObject(target: target, openness: 1, markActionFinished: true);
        }

        ///////////////////////////////////////////
        //////////////// MOVE HAND ////////////////
        ///////////////////////////////////////////

        protected IEnumerator moveHandToTowardsXYZWithForce(float x, float y, float z, float maxDistance) {
            if (ItemInHand == null) {
                throw new InvalidOperationException("Agent can only move hand if holding an item");
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

                Physics.Simulate(0.04f);
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

            Physics.Simulate(0.1f);
            bool handObjectIsColliding = isHandObjectColliding(true);
            if (count != 0) {
                for (int j = 0; handObjectIsColliding && j < 5; j++) {
                    AgentHand.transform.position = AgentHand.transform.position + 0.01f * aveCollisionsNormal;
                    Physics.Simulate(0.1f);
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
                Quaternion.Angle(initialRotation, lastRotation) < 0.001f
            ) {
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

        public void MoveHandForce(float x, float y, float z) {
            if (ItemInHand == null) {
                throw new InvalidOperationException("Agent can only move hand if holding an item");
            }

            Vector3 direction = (
                transform.forward * z +
                transform.right * x +
                transform.up * y
            );
            Vector3 target = AgentHand.transform.position + direction;
            if (moveHandToXYZ(target.x, target.y, target.z)) {
                actionFinished(true);
            }

            StartCoroutine(moveHandToTowardsXYZWithForce(target.x, target.y, target.z, direction.magnitude));
        }

        // checks if agent hand that is holding an object can move to a target location. Returns false if any obstructions
        public void CheckIfAgentCanMoveHand(Vector3 targetPosition, bool mustBeVisible = false) {
            // first check if we have anything in our hand, if not then no reason to move hand
            if (ItemInHand == null) {
                throw new InvalidOperationException("Agent can only move hand if currently holding an item");
            }

            // now check if the target position is within bounds of the Agent's forward (z) view
            Vector3 tmp = m_Camera.transform.position;
            tmp.y = targetPosition.y;

            if (Vector3.Distance(tmp, targetPosition) > maxVisibleDistance) {
                throw new InvalidOperationException("The target position is out of range: object cannot move outside of max visibility distance.");
            }

            // Note: Viewport normalizes to (0,0) bottom left, (1, 0) top right of screen
            // now make sure the targetPosition is actually within the Camera Bounds
            Vector3 lastPosition = AgentHand.transform.position;
            AgentHand.transform.position = targetPosition;

            // now make sure that the targetPosition is within the Agent's x/y view, restricted by camera
            if (!objectIsWithinViewport(ItemInHand.GetComponent<SimObjPhysics>())) {
                AgentHand.transform.position = lastPosition;
                throw new InvalidOperationException("Target position is outside of the agent's viewport.");
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
                    AgentHand.transform.position = lastPosition;
                    throw new InvalidOperationException("The target position is not in the area of the Agent's Viewport!");
                }
                AgentHand.transform.position = lastPosition;
            }

            // ok now actually check if the Agent Hand holding ItemInHand can move to the target position without
            // being obstructed by anything
            Rigidbody ItemRB = ItemInHand.GetComponent<Rigidbody>();
            RaycastHit[] sweepResults = ItemRB.SweepTestAll(
                targetPosition - AgentHand.transform.position,
                Vector3.Distance(targetPosition, AgentHand.transform.position),
                QueryTriggerInteraction.Ignore
            );

            // did we hit anything?
            if (sweepResults.Length > 0) {
                foreach (RaycastHit hit in sweepResults) {
                    // hit the player? it's cool, no problem
                    if (hit.transform.tag != "Player") {
                        // oh we hit something else? oh boy, that's blocking!
                        throw new InvalidOperationException(hit.transform.name + " is in Object In Hand's Path! Can't Move Hand holding " + ItemInHand.name);
                    }
                }
            }
        }

        // moves hand to the x, y, z coordinate, not constrained by any axis, if within range
        protected void moveHandToXYZ(float x, float y, float z, bool mustBeVisible = false) {
            Vector3 targetPosition = new Vector3(x, y, z);
            CheckIfAgentCanMoveHand(targetPosition, mustBeVisible);

            Vector3 oldPosition = AgentHand.transform.position;
            AgentHand.transform.position = targetPosition;
            IsHandDefault = false;
        }

        // Moves hand relative the agent (but not relative the camera, i.e. up is up)
        // x, y, z coordinates should specify how far to move in that direction, so
        // x=.1, y=.1, z=0 will move the hand .1 in both the x and y coordinates.
        public void MoveHand(float x, float y, float z) {
            // get new direction relative to Agent forward facing direction (not the camera)
            Vector3 newPos = (
                AgentHand.transform.position +
                transform.forward * z +
                transform.right * x +
                transform.up * y
            );
            moveHandToXYZ(newPos.x, newPos.y, newPos.z);

            IEnumerator waitForNFramesAndReturn(int n) {
                for (int i = 0; i < n; i++) {
                    yield return null;
                }
                actionFinished(true);
            }
            StartCoroutine(waitForNFramesAndReturn(n: 1));
        }

        // moves hand constrained to x, y, z axes a given magnitude- x y z describe the magnitude in this case
        // pass in x,y,z of 0 if no movement is desired on that axis
        // pass in x,y,z of + for positive movement along that axis
        // pass in x,y,z of - for negative movement along that axis
        public void MoveHandDelta(float x, float y, float z, bool forceVisible = false) {
            Vector3 newPos = AgentHand.transform.position;
            newPos = newPos + (m_Camera.transform.forward * z) + (m_Camera.transform.up * y) + (m_Camera.transform.right * x);
            moveHandToXYZ(newPos.x, newPos.y, newPos.z, forceVisible);
            actionFinished(success: true);
        }

        public void MoveHandAhead(float moveMagnitude, bool forceVisible = false) {
            Vector3 newPos = AgentHand.transform.position;
            newPos = newPos + (m_Camera.transform.forward * moveMagnitude);
            moveHandToXYZ(newPos.x, newPos.y, newPos.z, forceVisible);
            actionFinished(success: true);
        }

        public void MoveHandLeft(float moveMagnitude, bool forceVisible = false) {
            Vector3 newPos = AgentHand.transform.position;
            newPos = newPos + (-m_Camera.transform.right * moveMagnitude);
            moveHandToXYZ(newPos.x, newPos.y, newPos.z, forceVisible);
            actionFinished(success: true);
        }

        public void MoveHandDown(float moveMagnitude, bool forceVisible = false) {
            Vector3 newPos = AgentHand.transform.position;
            newPos = newPos + (-m_Camera.transform.up * moveMagnitude);
            moveHandToXYZ(newPos.x, newPos.y, newPos.z, forceVisible);
            actionFinished(success: true);
        }

        public void MoveHandUp(float moveMagnitude, bool forceVisible = false) {
            Vector3 newPos = AgentHand.transform.position;
            newPos = newPos + (m_Camera.transform.up * moveMagnitude);
            moveHandToXYZ(newPos.x, newPos.y, newPos.z, forceVisible);
            actionFinished(success: true);
        }

        public void MoveHandRight(float moveMagnitude, bool forceVisible = false) {
            Vector3 newPos = AgentHand.transform.position;
            newPos = newPos + (m_Camera.transform.right * moveMagnitude);
            moveHandToXYZ(newPos.x, newPos.y, newPos.z, forceVisible);
            actionFinished(success: true);
        }

        public void MoveHandBack(float moveMagnitude, bool forceVisible = false) {
            Vector3 newPos = AgentHand.transform.position;
            newPos = newPos + (-m_Camera.transform.forward * moveMagnitude);
            moveHandToXYZ(newPos.x, newPos.y, newPos.z, forceVisible);
            actionFinished(success: true);
        }

        // uh this kinda does what MoveHandDelta does but in more steps, splitting direction and magnitude into
        // two separate params in case someone wants it that way
        public void MoveHandMagnitude(float moveMagnitude, float x = 0, float y = 0, float z = 0) {
            Vector3 newPos = AgentHand.transform.position;

            // get new direction relative to Agent's (camera's) forward facing
            if (x > 0) {
                newPos = newPos + (m_Camera.transform.right * moveMagnitude);
            } else if (x < 0) {
                newPos = newPos + (-m_Camera.transform.right * moveMagnitude);
            }

            if (y > 0) {
                newPos = newPos + (m_Camera.transform.up * moveMagnitude);
            } else if (y < 0) {
                newPos = newPos + (-m_Camera.transform.up * moveMagnitude);
            }

            if (z > 0) {
                newPos = newPos + (m_Camera.transform.forward * moveMagnitude);
            } else if (z < 0) {
                newPos = newPos + (-m_Camera.transform.forward * moveMagnitude);
            }

            actionFinished(moveHandToXYZ(newPos.x, newPos.y, newPos.z));
        }

        ///////////////////////////////////////////
        /////////////// ROTATE HAND ///////////////
        ///////////////////////////////////////////

        public bool CheckIfAgentCanRotateHand() {
            bool result = false;

            // make sure there is a box collider
            if (ItemInHand.GetComponent<SimObjPhysics>().BoundingBox.GetComponent<BoxCollider>()) {
                Vector3 sizeOfBox = ItemInHand.GetComponent<SimObjPhysics>().BoundingBox.GetComponent<BoxCollider>().size;
                float overlapRadius = Math.Max(Math.Max(sizeOfBox.x, sizeOfBox.y), sizeOfBox.z);

                // all colliders hit by overlapsphere
                Collider[] hitColliders = Physics.OverlapSphere(AgentHand.transform.position,
                    overlapRadius, 1 << 8, QueryTriggerInteraction.Ignore);

                // did we even hit enything?
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
                            else
                                result = true;
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

        // rotate the hand if there is an object in it
        public void RotateHand(float x, float y, float z) {
            if (ItemInHand == null) {
                throw new InvalidOperationException("Can't rotate hand unless holding object");
            }

            if (!CheckIfAgentCanRotateHand()) {
                throw new InvalidOperationException("Agent hand cannot rotate!");
            }

            Vector3 vec = new Vector3(x, y, z);
            AgentHand.transform.localRotation = Quaternion.Euler(vec);

            // if this is rotated too much, drop any contained object if held item is a receptacle
            if (Vector3.Angle(ItemInHand.transform.up, Vector3.up) > 95) {
                dropContainedObjects(
                    target: ItemInHand.GetComponent<SimObjPhysics>(),
                    reparentContainedObjects: true,
                    forceKinematic: false
                );
            }

            actionFinished(true);
        }

        // rotate the hand if there is an object in it
        public void RotateHandRelative(float x, float y, float z) {
            if (ItemInHand == null) {
                throw new InvalidOperationException("Can't rotate hand unless holding object");
            }

            Quaternion agentRot = transform.rotation;
            Quaternion agentHandStartRot = AgentHand.transform.rotation;

            transform.rotation = Quaternion.identity;

            AgentHand.transform.Rotate(new Vector3(x, y, z), Space.World);
            transform.rotation = agentRot;

            if (isHandObjectColliding(true)) {
                AgentHand.transform.rotation = agentHandStartRot;
                throw new InvalidOperationException("Hand object is coliding after rotation.");
            }

            actionFinished(success: true);
        }

        ///////////////////////////////////////////
        ////////// ExpRoom CHANGE COLOR ///////////
        ///////////////////////////////////////////

        protected void changeColor(int r, int g, int b, string materialType, SimObjPhysics target = null) {
            if (r < 0 || r > 255 || g < 0 || g > 255 || b < 0 || b > 255) {
                throw new ArgumentOutOfRangeException("rgb values must be [0-255]");
            }

            if (materialType == null) {
                throw new ArgumentNullException();
            }

            ExperimentRoomSceneManager ersm = physicsSceneManager.GetComponent<ExperimentRoomSceneManager>();
            switch (materialType) {
                case "tableTop":
                    ersm.ChangeTableTopColor(r, g, b);
                    break;
                case "tableLeg":
                    ersm.ChangeTableLegColor(r, g, b);
                    break;
                case "light":
                    ersm.ChangeLightColor(r, g, b);
                    break;
                case "wall":
                    ersm.ChangeWallColor(r, g, b);
                    break;
                case "floor":
                    ersm.ChangeFloorColor(r, g, b);
                    break;
                case "screen":
                    if (target == null) {
                        throw new ArgumentNullException();
                    }
                    ersm.ChangeScreenColor(target, r, g, b);
                    break;
                default:
                    throw new ArgumentException("Invalid materialType!");
            }
            actionFinished(success: true);
        }

        public void ChangeWallColorExpRoom(int r, int g, int b) {
            changeColor(r: r, g: g, b: b, materialType: "wall");
        }

        public void ChangeFloorColorExpRoom(int r, int g, int b) {
            changeColor(r: r, g: g, b: b, materialType: "floor");
        }

        public void ChangeLightColorExpRoom(int r, int g, int b) {
            changeColor(r: r, g: g, b: b, materialType: "light");
        }

        public void ChangeTableTopColorExpRoom(int r, int g, int b) {
            changeColor(r: r, g: g, b: b, materialType: "tableTop");
        }

        public void ChangeTableLegColorExpRoom(int r, int g, int b) {
            changeColor(r: r, g: g, b: b, materialType: "tableLeg");
        }

        // specify a screen in exp room by objectId and change material color to rgb
        public void ChangeScreenColorExpRoom(string objectId, float r, float g, float b, bool forceAction = false) {
            SimObjPhysics target = getTargetObject(objectId: objectId, forceAction: forceAction);
            changeColor(r: r, g: g, b: b, materialType: "screen", target: target);
        }

        ///////////////////////////////////////////
        ///////// ExpRoom CHANGE LIGHTING /////////
        ///////////////////////////////////////////

        // change intensity of lights in exp room [0-5] these aren't in like... lumens or anything
        // just a relative intensity value
        public void ChangeLightIntensityExpRoom(float intensity) {
            if (intensity < 0 || intensity > 5) {
                throw new ArgumentOutOfRangeException("light intensity must be [0.0 , 5.0] inclusive");
            }

            ExperimentRoomSceneManager ersm = physicsSceneManager.GetComponent<ExperimentRoomSceneManager>();
            ersm.ChangeLightIntensity(intensity);
            actionFinished(true);
        }

        ///////////////////////////////////////////
        //////// ExpRoom CHANGE MATERIALS /////////
        ///////////////////////////////////////////

        protected void changeMaterial(int objectVariation, string materialType, SimObjPhysics target = null) {
            if (materialType == null) {
                throw new ArgumentNullException();
            }

            // todo: get this dynamically
            if (objectVariation < 0 || objectVariation > 4) {
                throw new ArgumentOutOfRangeException("Please use objectVariation [0, 4] inclusive");
            }

            ExperimentRoomSceneManager ersm = physicsSceneManager.GetComponent<ExperimentRoomSceneManager>();
            switch (materialType) {
                case "tableTop":
                    ersm.ChangeTableTopMaterial(objectVariation);
                    break;
                case "tableLeg":
                    ersm.ChangeTableLegMaterial(objectVariation);
                    break;
                case "wall":
                    ersm.ChangeWallMaterial(objectVariation);
                    break;
                case "floor":
                    ersm.ChangeFloorMaterial(objectVariation);
                    break;
                case "screen":
                    if (target == null) {
                        throw new ArgumentNullException();
                    }
                    ersm.ChangeScreenMaterial(target, objectVariation);
                    break;
                default:
                    throw new ArgumentException("Invalid materialType!");
            }
            actionFinished(success: true);
        }

        public void ChangeTableTopMaterialExpRoom(int objectVariation) {
            changeMaterial(objectVariation: objectVariation, materialType: "tableTop");
        }

        public void ChangeTableLegMaterialExpRoom(int objectVariation) {
            changeMaterial(objectVariation: objectVariation, materialType: "tableLeg");
        }

        public void ChangeWallMaterialExpRoom(int objectVariation) {
            changeMaterial(objectVariation: objectVariation, materialType: "wall");
        }

        public void ChangeFloorMaterialExpRoom(int objectVariation) {
            changeMaterial(objectVariation: objectVariation, materialType: "floor");
        }

        // specify a screen by objectId in exp room and change material to objectVariation
        public void ChangeScreenMaterialExpRoom(string objectId, int objectVariation, bool forceAction = false) {
            SimObjPhysics target = getTargetObject(objectId: objectId, forceAction: forceAction);
            changeMaterial(objectVariation: objectVariation, materialType: "screen", target: target);
        }

        ///////////////////////////////////////////
        //////////// ExpRoom SPAWNING /////////////
        ///////////////////////////////////////////

        // returns valid spawn points for spawning an object on a receptacle in the experiment room
        // checks if <objectId> at <y> rotation can spawn without falling off
        // table <receptacleObjectId>
        public void ReturnValidSpawnsExpRoom(string receptacleObjectId, string objectType, float y) {
            if (receptacleObjectId == null || objectType == null) {
                throw new ArgumentNullException();
            }

            // return all valid spawn coordinates
            ExperimentRoomSceneManager ersm = physicsSceneManager.GetComponent<ExperimentRoomSceneManager>();
            SimObjPhysics target = getTargetObject(objectId: receptacleObjectId, forceAction: false);
            actionFinished(
                success: true,
                actionReturn: ersm.ReturnValidSpawns(
                    objType: objectType,
                    variation: objectVariation,
                    targetReceptacle: target,
                    yRot: y
                )
            );
        }

        // action to return points from a grid that have an experiment receptacle below it
        // creates a grid starting from the agent's current hand position and projects that grid
        // forward relative to the agent
        // grid will be a 2n+1 by n grid in the orientation of agent right/left by agent forward
        public void GetReceptacleCoordinatesExpRoom(float gridSize, int maxStepCount) {
            var agent = agentManager.agents[0];
            ExperimentRoomSceneManager ersm = physicsSceneManager.GetComponent<ExperimentRoomSceneManager>();

            // good defaults would be gridSize 0.1m, maxStepCount 20 to cover the room
            var ret = ersm.ValidGrid(agent.AgentHand.transform.position, gridSize, maxStepCount, agent);

            // var ret = ersm.ValidGrid(agent.AgentHand.transform.position, gridSize, maxStepCount, agent);
            actionFinished(true, ret);
        }

        // spawn receptacle object at array index <objectVariation> rotated to <y>
        // on <receptacleObjectId> using position <position>
        public void SpawnExperimentObjAtPoint(string receptacleObjectId, string objectType, int objectVariation, float y, Vector3 position) {
            if (receptacleObjectId == null || objectType == null) {
                throw new ArgumentNullException();
            }

            ExperimentRoomSceneManager ersm = physicsSceneManager.GetComponent<ExperimentRoomSceneManager>();
            SimObjPhysics target = getTargetObject(objectId: receptacleObjectId, forceAction: false);
            if (!ersm.SpawnExperimentObjAtPoint(objectType, objectVariation, target, position, y)) {
                throw new InvalidOperationException("Experiment object could not be placed on " + receptacleObjectId);
            }
            actionFinished(true);
        }

        // spawn receptacle object at array index <objectVariation> rotated to <y>
        // on <receptacleObjectId> using random seed <randomSeed>
        public void SpawnExperimentObjAtRandom(string receptacleObjectId, string objectType, float y, int objectVariation, int randomSeed = 0) {
            if (receptacleObjectId == null || objectType == null) {
                throw new ArgumentNullException();
            }

            ExperimentRoomSceneManager ersm = physicsSceneManager.GetComponent<ExperimentRoomSceneManager>();
            SimObjPhysics target = getTargetObject(objectId: receptacleObjectId);
            if (!ersm.SpawnExperimentObjAtRandom(objectType, objectVariation, randomSeed, target, y)) {
                throw new InvalidOperationException("Experiment object could not be placed on " + receptacleObjectId);
            }
            actionFinished(true);
        }

        ///////////////////////////////////////////
        ////////////// SCALE OBJECT ///////////////
        ///////////////////////////////////////////

        // Change the scale of a sim object. This only works with sim objects not structures
        // scale should be something like 0.3 to shrink or 1.5 to grow
        protected void scaleObject(SimObjPhysics target, float scale, bool markActionFinished) {
            if (target == null) {
                throw new ArgumentNullException();
            }

            IEnumerator scaleObject() {
                Vector3 targetScale = gameObject.transform.localScale * scale;
                yield return new WaitForFixedUpdate();

                Vector3 originalScale = target.transform.localScale;
                float currentTime = 0.0f;

                do {
                    target.transform.localScale = Vector3.Lerp(originalScale, targetScale, currentTime / 1.0f);
                    currentTime += Time.deltaTime;
                    yield return null;
                } while (currentTime <= 1.0f);

                // store reference to all children
                Transform[] children = new Transform[target.transform.childCount];

                for (int i = 0; i < target.transform.childCount; i++) {
                    children[i] = target.transform.GetChild(i);
                }

                // detach all children
                target.transform.DetachChildren();

                // zero out object transform to be 1, 1, 1
                target.transform.transform.localScale = Vector3.one;

                // re-parent all children
                foreach (Transform t in children) {
                    t.SetParent(target.transform);
                }

                target.ContextSetUpBoundingBox();
                if (markActionFinished) {
                    actionFinished(true);
                }
            }
            StartCoroutine(scaleObject());
        }

        public void ScaleObject(float x, float y, float scale, bool forceAction = false) {
            SimObjPhysics target = getTargetObject(x: x, y: y, forceAction: forceAction);
            scaleObject(target: target, scale: scale, markActionFinished: true);
        }

        public void ScaleObject(string objectId, float scale, bool forceAction = false) {
            SimObjPhysics target = getTargetObject(objectId: objectId, forceAction: forceAction);
            scaleObject(target: target, scale: scale, markActionFinished: true);
        }

        ///////////////////////////////////////////
        ////////// PLACE OBJECT AT POINT //////////
        ///////////////////////////////////////////

        // pass in a Vector3, presumably from GetReachablePositions, and try to place a specific Sim Object there
        // unlike PlaceHeldObject or InitialRandomSpawn, this won't be limited by a Receptacle, but only
        // limited by collision
        public void PlaceObjectAtPoint(
            string objectId,
            Vector3 position,
            Vector3? rotation = null,
            bool forceKinematic = false
        ) {
            // find the object in the scene, disregard visibility
            SimObjPhysics target = getTargetObject(objectId: objectId, forceAction: true);

            placeObjectAtPoint(
                target: target,
                position: position,
                rotation: rotation,
                forceKinematic: forceKinematic,
                includeErrorMessage: true
            );

            if (placeObjectSuccess) {
                if (!forceKinematic) {
                    StartCoroutine(checkIfObjectHasStoppedMoving(target, 0, true));
                } else {
                    actionFinished(true);
                }
            } else {
                actionFinished(false);
            }
        }

        protected bool placeObjectAtPoint(
            SimObjPhysics target,
            Vector3 position,
            Vector3? rotation,
            bool forceKinematic,
            bool includeErrorMessage = false
        ) {
            // make sure point we are moving the object to is valid
            if (!agentManager.sceneBounds.Contains(position)) {
                throw new ArgumentOutOfRangeException($"Position coordinate ({position}) is not within scene bounds ({agentManager.sceneBounds})");
            }

            Quaternion originalRotation = target.transform.rotation;
            if (rotation.HasValue) {
                target.transform.rotation = Quaternion.Euler(rotation.Value);
            }
            Vector3 originalPos = target.transform.position;
            target.transform.position = agentManager.SceneBounds.min - new Vector3(-100f, -100f, -100f);

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

            Vector3 finalPos = GetSurfacePointBelowPosition(position) +  new Vector3(0, offset, 0);

            // Check spawn area here
            target.transform.position = finalPos;
            Collider colliderHitIfSpawned = UtilityFunctions.firstColliderObjectCollidingWith(
                target.gameObject
            );

            if (colliderHitIfSpawned == null) {
                target.transform.position = finalPos;

                // Additional stuff we need to do if placing item that was in hand
                if (wasInHand) {

                    Rigidbody rb = ItemInHand.GetComponent<Rigidbody>();
                    rb.isKinematic = forceKinematic;
                    rb.constraints = RigidbodyConstraints.None;
                    rb.useGravity = true;

                    // change collision detection mode while falling so that objects don't phase through colliders.
                    // this is reset to discrete on SimObjPhysics.cs's update
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

                    GameObject topObject = GameObject.Find("Objects");
                    if (topObject != null) {
                        ItemInHand.transform.parent = topObject.transform;
                    } else {
                        ItemInHand.transform.parent = null;
                    }

                    dropContainedObjects(
                        target: target,
                        reparentContainedObjects: true,
                        forceKinematic: forceKinematic
                    );
                    target.isInAgentHand = false;
                    ItemInHand = null;

                }
                return true;
            }

            target.transform.position = originalPos;
            target.transform.rotation = originalRotation;

            // if the original position was in agent hand, re-parent object to agent hand
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
            SimObjPhysics target = getTargetObject(objectId: objectId, forceAction: true);

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
                } else {
                    actionFinished(true);
                }
            } else {
                actionFinished(false);
            }
        }

        // return a bunch of vector3 points above a target receptacle
        // if forceVisible = true, return points regardless of where receptacle is
        // if forceVisible = false, only return points that are also within view of the Agent camera
        public void GetSpawnCoordinatesAboveReceptacle(string objectId, bool anywhere = false) {
            SimObjPhysics target = getTargetObject(objectId: objectId, forceAction: anywhere);

            // ok now get spawn points from target
            List<Vector3> targetPoints = new List<Vector3>();
            targetPoints = target.FindMySpawnPointsFromTopOfTriggerBox();

            // by default, anywhere = false, so remove all targetPoints that are outside of agent's view
            // if anywhere true, don't do this and just return all points we got from above
            if (!anywhere) {
                List<Vector3> filteredTargetPoints = new List<Vector3>();
                foreach (Vector3 v in targetPoints) {
                    if (isPositionInteractable(position: v)) {
                        filteredTargetPoints.Add(v);
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
        public List<Vector3> getSpawnCoordinatesAboveReceptacle(SimObjPhysics target) {
            // ok now get spawn points from target
            List<Vector3> targetPoints = new List<Vector3>();
            targetPoints = target.FindMySpawnPointsFromTopOfTriggerBox();
            return targetPoints;
        }

        // instantiate a target circle, and then place it in a "SpawnOnlyOUtsideReceptacle" that is also within camera view
        // If fails, return actionFinished(false) and despawn target circle
        public void SpawnTargetCircle(
            string objectId,
            int randomSeed = 0,
            float minDistance = 0,
            float maxDistance = 0,
            bool anywhere = false,
            int objectVariation = 0
        ) {
            if (objectId == null) {
                throw new ArgumentNullException();
            }

            if (objectVariation > 2 || objectVariation < 0) {
                throw new ArgumentOutOfRangeException("objectVariation must be in {0: small, 1: medium, 2: large}.");
            }

            // instantiate a target circle
            GameObject targetCircle = Instantiate(TargetCircles[objectVariation], new Vector3(0, 100, 0), Quaternion.identity);
            List<SimObjPhysics> targetReceptacles = new List<SimObjPhysics>();
            InstantiatePrefabTest ipt = physicsSceneManager.GetComponent<InstantiatePrefabTest>();

            // this is the default, only spawn circles in objects that are in view
            if (!anywhere) {
                // check every sim object and see if it is within the viewport
                foreach (SimObjPhysics sop in VisibleSimObjs(true)) {
                    if (sop.hasSecondaryProperty(SimObjSecondaryProperty.Receptacle)) {
                        /// one more check, make sure this receptacle
                        if (ReceptacleRestrictions.SpawnOnlyOutsideReceptacles.Contains(sop.ObjType)) {
                            // ok now check if the object is for real in the viewport
                            if (objectIsWithinViewport(sop)) {
                                targetReceptacles.Add(sop);
                            }
                        }
                    }
                }
            } else {
                // spawn target circle in any valid "outside" receptacle in the scene even if not in veiw
                foreach (SimObjPhysics sop in physicsSceneManager.GatherAllReceptaclesInScene()) {
                    if (ReceptacleRestrictions.SpawnOnlyOutsideReceptacles.Contains(sop.ObjType)) {
                        targetReceptacles.Add(sop);
                    }
                }
            }


            // if we passed in a objectId, see if it is in the list of targetReceptacles found so far
            if (objectId != null) {
                List<SimObjPhysics> filteredTargetReceptacleList = new List<SimObjPhysics>();
                foreach (SimObjPhysics sop in targetReceptacles) {
                    if (sop.objectID == objectId) {
                        filteredTargetReceptacleList.Add(sop);
                    }
                }
                targetReceptacles = filteredTargetReceptacleList;
            }

            if (targetReceptacles.Count <= 0) {
                throw new InvalidOperationException("for some reason, no receptacles were found in the scene!");
            }

            // ok we have a shuffled list of receptacles that is picked based on the seed....
            bool successfulSpawn = false;
            foreach (SimObjPhysics sop in targetReceptacles) {
                // for every receptacle, we will get a returned list of receptacle spawn points, and then try placeObjectReceptacle
                List<ReceptacleSpawnPoint> rsps = new List<ReceptacleSpawnPoint>();

                rsps = sop.ReturnMySpawnPoints(false);
                List<ReceptacleSpawnPoint> editedRsps = new List<ReceptacleSpawnPoint>();

                // only set rsps to editedRsps if constraints were passed in
                bool constraintsUsed = false;

                // only do further constraint checks if defaults are overwritten
                if (minDistance != 0 || maxDistance != 0) {
                    foreach (ReceptacleSpawnPoint p in rsps) {
                        // get rid of differences in y values for points
                        Vector3 normalizedPosition = new Vector3(transform.position.x, 0, transform.position.z);
                        Vector3 normalizedPoint = new Vector3(p.Point.x, 0, p.Point.z);

                        if (minDistance == 0 && maxDistance > 0) {
                            // check distance from agent's transform to spawnpoint
                            if ((Vector3.Distance(normalizedPoint, normalizedPosition) <= maxDistance)) {
                                editedRsps.Add(p);
                            }
                        }

                        // min distance passed in, no max distance
                        if (maxDistance == 0 && minDistance > 0) {
                            // check distance from agent's transform to spawnpoint
                            if ((Vector3.Distance(normalizedPoint, normalizedPosition) >= minDistance)) {
                                editedRsps.Add(p);
                            }
                        } else {
                            // these are default so don't filter by distance
                            // check distance from agent's transform to spawnpoint
                            if (Vector3.Distance(normalizedPoint, normalizedPosition) >= minDistance &&
                                Vector3.Distance(normalizedPoint, normalizedPosition) <= maxDistance
                            ) {
                                editedRsps.Add(p);
                            }
                        }
                        constraintsUsed = true;
                    }
                }

                if (constraintsUsed) {
                    rsps = editedRsps;
                }

                rsps.Shuffle_(randomSeed);

                // only place in viewport
                if (!anywhere) {
                    if (ipt.PlaceObjectReceptacleInViewport(rsps, targetCircle.GetComponent<SimObjPhysics>(), true, 500, 90, true)) {
                        // make sure target circle is within viewport
                        successfulSpawn = true;
                        break;
                    }
                } else if (ipt.PlaceObjectReceptacle(rsps, targetCircle.GetComponent<SimObjPhysics>(), true, 500, 90, true)) {
                    // make sure target circle is within viewport
                    successfulSpawn = true;
                    break;
                }
            }

            if (!successfulSpawn) {
                Destroy(targetCircle);
                throw new InvalidOperationException("circle failed to spawn");
            }

            // if image synthesis is active, make sure to update the renderers for image synthesis since now there are new objects with renderes in the scene
            BaseFPSAgentController primaryAgent = GameObject.Find("PhysicsSceneManager").GetComponent<AgentManager>().ReturnPrimaryAgent();
            if (primaryAgent.imageSynthesis && primaryAgent.imageSynthesis.enabled) {
                primaryAgent.imageSynthesis.OnSceneChange();
            }

            SimObjPhysics targetSOP = targetCircle.GetComponent<SimObjPhysics>();
            physicsSceneManager.Generate_ObjectID(targetSOP);
            physicsSceneManager.AddToObjectsInScene(targetSOP);
            actionFinished(true, targetSOP.objectID);// return the objectID of circle spawned for easy reference
        }

        public void MakeObjectsOfTypeUnbreakable(string objectType) {
            if (objectType == null) {
                throw new ArgumentNullException();
            }

            SimObjPhysics[] simObjs= GameObject.FindObjectsOfType(typeof(SimObjPhysics)) as SimObjPhysics[];
            foreach (SimObjPhysics sop in simObjs) {
                if (sop.Type.ToString() == objectType) {
                    if (sop.hasSecondaryProperty(SimObjSecondaryProperty.CanBreak)) {
                        sop.GetComponent<Break>().Unbreakable = true;
                    }
                }
            }
            actionFinished(true);
        }

        ///////////////////////////////////////////
        /////////////// PUT OBJECT ////////////////
        ///////////////////////////////////////////

        // if you are holding an object, place it on a valid Receptacle
        // used for placing objects on receptacles without enclosed restrictions (drawers, cabinets, etc)
        // only checks if the object can be placed on top of the target receptacle
        public void PutObject(float x, float y, bool forceAction=false, bool placeStationary=true) {
            PlaceHeldObject(x, y, forceAction, placeStationary);
        }

        public void PutObject(string objectId, bool forceAction=false, bool placeStationary=true) {
            PlaceHeldObject(objectId, forceAction, placeStationary);
        }

        [ObsoleteAttribute(message: "This action is deprecated. Call PutObject instead.", error: false)]
        public void PlaceHeldObject(float x, float y, bool forceAction = false, bool placeStationary = true, int randomSeed = 0, float z = 0.0f) {
            // TODO: what is z?
            SimObjPhysics targetReceptacle = getTargetObject(x: x, y: y, forceAction: forceAction);
            putObject(targetReceptacle, forceAction, placeStationary, randomSeed, z);
        }

        [ObsoleteAttribute(message: "This action is deprecated. Call PutObject instead.", error: false)]
        public void PlaceHeldObject(string objectId, bool forceAction=false, bool placeStationary=true, int randomSeed = 0, float z = 0.0f) {
            // TODO: what is z?
            SimObjPhysics targetReceptacle = getTargetObject(objectId: objectId, forceAction: forceAction);
            putObject(targetReceptacle, forceAction, placeStationary, randomSeed, z);
        }

        protected void putObject(SimObjPhysics targetReceptacle, bool forceAction, bool placeStationary, int randomSeed, float z) {
            if (targetReceptacle == null) {
                throw new ArgumentNullException();
            }

            // check if we are even holding anything
            if (ItemInHand == null) {
                throw new InvalidOperationException("Can't place an object if Agent isn't holding anything");
            }

            if (!targetReceptacle.hasSecondaryProperty(SimObjSecondaryProperty.Receptacle)) {
                throw new ArgumentException("This target object is NOT a receptacle!");
            }

            // if receptacle can open, check that it's open before placing. Can't place objects in something that is closed!
            if (targetReceptacle.hasSecondaryProperty(SimObjSecondaryProperty.CanOpen) &&
                ReceptacleRestrictions.MustBeOpenToPlaceObjectsIn.Contains(targetReceptacle.ObjType) &&
                !targetReceptacle.GetComponent<CanOpen_Object>().isOpen
            ) {
                throw new InvalidOperationException("Target openable Receptacle is CLOSED, can't place if target is not open!");
            }

            // if this receptacle only receives specific objects, check that the ItemInHand is compatible and
            // check if the receptacle is currently full with another valid object or not
            if (targetReceptacle.hasSecondaryProperty(SimObjSecondaryProperty.ObjectSpecificReceptacle)) {
                ObjectSpecificReceptacle osr = targetReceptacle.GetComponent<ObjectSpecificReceptacle>();
                if (osr.attachPoint.transform.childCount > 0 || osr.isFull()) {
                    throw new InvalidOperationException(targetReceptacle.name + " is currently full!");
                }

                if (!osr.HasSpecificType(ItemInHand.GetComponent<SimObjPhysics>().ObjType)) {
                    throw new InvalidOperationException(ItemInHand.name + " is not a valid Object Type to be placed in " + targetReceptacle.name);
                }

                // check spawn area specifically if it's a stove top we are trying to place something in because
                // they are close together and can overlap and are weird
                if (osr.GetComponent<SimObjPhysics>().Type == SimObjType.StoveBurner && !physicsSceneManager.StoveTopCheckSpawnArea(
                        simObj: ItemInHand.GetComponent<SimObjPhysics>(),
                        position: osr.attachPoint.transform.position,
                        rotation: osr.attachPoint.transform.rotation,
                        spawningInHand: false
                    )
                ) {
                    throw new InvalidOperationException("another object's collision is blocking held object from being placed");
                }

                ItemInHand.transform.position = osr.attachPoint.position;
                ItemInHand.transform.SetParent(osr.attachPoint.transform);
                ItemInHand.transform.localRotation = Quaternion.identity;
                ItemInHand.GetComponent<Rigidbody>().isKinematic = true;
                ItemInHand.GetComponent<SimObjPhysics>().isInAgentHand = false; // remove in agent hand flag
                ItemInHand = null;
                DefaultAgentHand();
                actionFinished(true);
            }

            SimObjPhysics handSOP = ItemInHand.GetComponent<SimObjPhysics>();

            if (!forceAction) {
                // check if the item we are holding can even be placed in the ObjectID target at all
                foreach (KeyValuePair<SimObjType, List<SimObjType>> res in ReceptacleRestrictions.PlacementRestrictions) {
                    // find the Object Type in the PlacementRestrictions dictionary
                    if (res.Key == handSOP.ObjType && !res.Value.Contains(targetReceptacle.ObjType)) {
                        throw new InvalidOperationException(ItemInHand.name + " cannot be placed in " + targetReceptacle.transform.name);
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
            List<ReceptacleSpawnPoint> spawnPoints = targetReceptacle.ReturnMySpawnPoints(onlyPointsCloseToAgent);
            if (randomSeed != 0) {
                List<ReceptacleSpawnPoint> randomizedSpawnPoints = new List<ReceptacleSpawnPoint>();
                float maxDistance = z;
                if (maxDistance == 0.0f) {
                    maxDistance = maxVisibleDistance;
                }
                foreach (ReceptacleSpawnPoint sp in spawnPoints) {
                    Vector3 tmp = new Vector3(transform.position.x, sp.Point.y, transform.position.z);
                    if (Vector3.Distance(sp.Point, tmp) < maxDistance) {
                        randomizedSpawnPoints.Add(sp);
                    }
                }
                randomizedSpawnPoints.Shuffle_(randomSeed);
                spawnPoints = randomizedSpawnPoints;
            }
            if (!script.PlaceObjectReceptacle(spawnPoints, ItemInHand.GetComponent<SimObjPhysics>(), placeStationary, -1, 90, placeUpright)) {
                throw new InvalidOperationException("No valid positions to place object found");
            }
            ItemInHand = null;
            DefaultAgentHand();
            actionFinished(true);
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
                dropContainedObjects(
                    target: target,
                    reparentContainedObjects: true,
                    forceKinematic: false
                );
                throw new InvalidOperationException("Picking up object would cause it to collide and clip into something!");
            }

            // we have successfully picked up something!
            target.isInAgentHand = true;
            if (markActionFinished) {
                actionFinished(success: true, actionReturn: target.ObjectID);
            }
        }

        public void PickupObject(float x, float y, bool forceAction = false, bool manualInteract = false) {
            SimObjPhysics target = getTargetObject(x: x, y: y, forceAction: forceAction);
            pickupObject(target: target, forceAction: forceAction, manualInteract: manualInteract, markActionFinished: true);
        }

        public void PickupObject(string objectId, bool forceAction = false, bool manualInteract = false) {
            SimObjPhysics target = getTargetObject(objectId: objectId, forceAction: forceAction);
            pickupObject(target: target, forceAction: forceAction, manualInteract: manualInteract, markActionFinished: true);
        }

        // make sure not to pick up any sliced objects because those should remain un-interactable
        public void PickupContainedObjects(SimObjPhysics target) {
            if (target.hasSecondaryProperty(SimObjSecondaryProperty.Receptacle)) {
                foreach (SimObjPhysics sop in target.SimObjectsContainedByReceptacle) {
                    // for every object that is contained by this object...first make sure it's pickupable so we don't like, grab a Chair if it happened to be in the receptacle box or something
                    // turn off the colliders (so contained object doesn't block movement), leaving Trigger Colliders active (this is important to maintain visibility!)
                    if (sop.PrimaryProperty == SimObjPrimaryProperty.CanPickup) {
                        // wait! check if this object is sliceable and is sliced, if so SKIP!
                        if (sop.hasSecondaryProperty(SimObjSecondaryProperty.CanBeSliced) &&
                            sop.GetComponent<SliceObject>().IsSliced()
                        ) {
                            // if this object is sliced, don't pick it up because it is effectively disabled
                            target.RemoveFromContainedObjectReferences(sop);
                            // XXX: Should this be continue? Should it raise an exception? I don't think it should be break..
                            break;
                        }

                        sop.transform.Find("Colliders").gameObject.SetActive(false);
                        Rigidbody soprb = sop.GetComponent<Rigidbody>();
                        soprb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                        soprb.isKinematic = true;
                        sop.transform.SetParent(target.transform);

                        // used to reference objects in the receptacle that is being picked up without having to search through all children
                        target.AddToContainedObjectReferences(sop);

                        // agent hand flag
                        target.isInAgentHand = true;
                    }
                }
            }
        }

        // note: this is accessed by InstantiatePrefabTest, too.
        public void dropContainedObjects(
            SimObjPhysics target,
            bool reparentContainedObjects,
            bool forceKinematic
        ) {
            if (target.hasSecondaryProperty(SimObjSecondaryProperty.Receptacle)) {
                // print("dropping contained objects");
                GameObject topObject = null;

                foreach (SimObjPhysics sop in target.ContainedObjectReferences) {
                    // for every object that is contained by this object turn off
                    // the colliders, leaving Trigger Colliders active (this is important to maintain visibility!)
                    sop.transform.Find("Colliders").gameObject.SetActive(true);
                    sop.isInAgentHand = false; // Agent hand flag

                    if (reparentContainedObjects) {
                        if (topObject == null) {
                            topObject = GameObject.Find("Objects");
                        }
                        sop.transform.SetParent(topObject.transform);
                    }

                    Rigidbody rb = sop.GetComponent<Rigidbody>();
                    rb.isKinematic = forceKinematic;
                    if (!forceKinematic) {
                        rb.useGravity = true;
                        rb.constraints = RigidbodyConstraints.None;
                        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                    }

                }
                target.ClearContainedObjectReferences();
            }
        }

        // note: this is accessed by InstantiatePrefabTest, too.
        public void dropContainedObjectsStationary(SimObjPhysics target) {
            dropContainedObjects(target: target, reparentContainedObjects: false, forceKinematic: true);
        }

        protected IEnumerator checkDropHandObjectActionFast(SimObjPhysics currentHandSimObj) {
            if (currentHandSimObj != null) {
                Rigidbody rb = currentHandSimObj.GetComponentInChildren<Rigidbody>();
                Physics.autoSimulation = false;
                yield return null;

                for (int i = 0; i < 100; i++) {
                    Physics.Simulate(0.04f);
                    #if UNITY_EDITOR
                        yield return null;
                    #endif
                    if (Math.Abs(rb.angularVelocity.sqrMagnitude + rb.velocity.sqrMagnitude) < 1e-5f) {
                        break;
                    }
                }
                Physics.autoSimulation = true;
            }

            DefaultAgentHand();
            actionFinished(true);
        }

        public void DropHandObject(bool forceAction = false, bool autoSimulation = true) {
            if (ItemInHand == null) {
                throw new InvalidOperationException("Nothing in hand to drop!");
            }

            // we do need this to check if the item is currently colliding with the agent, otherwise
            // dropping an object while it is inside the agent will cause it to shoot out weirdly
            if (!forceAction && isHandObjectColliding(false)) {
                throw new InvalidOperationException($"{ItemInHand.transform.name} can't be dropped. It must be clear of all other collision first, including the Agent");
            }

            Rigidbody rb = ItemInHand.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.None;
            rb.useGravity = true;

            // change collision detection mode while falling so that objects don't phase through colliders.
            // this is reset to discrete on SimObjPhysics.cs's update
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            GameObject topObject = GameObject.Find("Objects");
            ItemInHand.transform.parent = topObject == null ? null : topObject.transform;

            // Add some random rotational momentum to the dropped object to make things
            // less deterministic.
            // TODO: Need a parameter to control how much randomness we introduce.
            rb.angularVelocity = UnityEngine.Random.insideUnitSphere;

            dropContainedObjects(
                target: ItemInHand.GetComponent<SimObjPhysics>(),
                reparentContainedObjects: true,
                forceKinematic: false
            );

            // if physics simulation has been paused by the PausePhysicsAutoSim() action, don't do any coroutine checks
            if (!physicsSceneManager.physicsSimulationPaused) {
                if (autoSimulation) {
                    StartCoroutine(checkIfObjectHasStoppedMoving(ItemInHand.GetComponent<SimObjPhysics>(), 0));
                } else {
                    StartCoroutine(checkDropHandObjectActionFast(ItemInHand.GetComponent<SimObjPhysics>()));
                }
            } else {
                actionFinished(true);
            }

            ItemInHand.GetComponent<SimObjPhysics>().isInAgentHand = false;
            ItemInHand = null;
        }

        // by default will throw in the forward direction relative to the Agent's Camera
        // moveMagnitude, strength of throw, good values for an average throw are around 150-250
        public void ThrowObject(float moveMagnitude, bool forceAction = false) {
            if (ItemInHand == null) {
                throw new InvalidOperationException("Nothing in Hand to Throw!");
            }

            GameObject go = ItemInHand;
            DropHandObject(forceAction: forceAction);
            // XXX: won't this execute after actionFinished has been called?
            if (lastActionSuccess) {
                Vector3 dir = m_Camera.transform.forward;
                go.GetComponent<SimObjPhysics>().ApplyForce(dir, moveMagnitude);
            }
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
            foreach (SimObjPhysics so in objects)
            {
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
                if (coo && !coo.isOpen) {
                    // if object is open, add it to be closed.
                    openObject(
                        target: so,
                        openness: openness,
                        markActionFinished: false,
                        simplifyPhysics: simplifyPhysics
                    );
                }
            }

            // While there are no objects to open, it was technically successful at opening all 0 objects.
            actionFinished(true);
        }

        // H&S action
        [ObsoleteAttribute(message: "This action is deprecated. Call OpenObject instead.", error: false)]
        public void OpenObjectAtLocation(float x, float y, float z = 0, bool forceAction = false) {
            // NOTE: z specifies the radius... for some reason...
            if (z > 0) {
                for (int i = 0; i < 10; i++) {
                    // for 10 different radii
                    float r = z * (i / 9.0f);

                    // for n different points along that radii
                    int n = 2 * i + 1;
                    for (int j = 0; j < n; j++) {
                        float theta_k = 2 * j * ((float) Math.PI) / n;

                        // update the z x/y target
                        float newX = x + (float) (r * Math.Cos(theta_k));
                        float newY = y + (float) (r * Math.Sin(theta_k));
                        if (x < 0 || x > 1.0 || y < 0 || y > 1.0) {
                            continue;
                        }

                        // try getting the sim object
                        SimObjPhysics sop;
                        try {
                            sop = getTargetObject(x: newX, y: newY, forceAction: forceAction);
                        } catch (InvalidOperationException) {
                            // no object at x/y
                            continue;
                        }

                        // try opening that object
                        try {
                            openObject(target: sop, openness: 1, markActionFinished: true);
                            return;
                        } catch (InvalidOperationException) {
                            // object at x/y is not openable, keep looking.
                            continue;
                        }
                    }
                }

                // never was able to open the object :(
                throw new InvalidOperationException("No openable object found within a radius about given point.");
            }

            OpenObject(x: x, y: y, forceAction: forceAction);
        }

        // H&S action
        [ObsoleteAttribute(message: "This action is deprecated. Call CloseObject instead.", error: false)]
        public void CloseObjectAtLocation(float x, float y, bool forceAction = false) {
            CloseObject(x: x, y: y, forceAction: forceAction);
        }

        protected bool anyInteractionsStillRunning(List<CanOpen_Object> coos) {
            bool anyStillRunning = false;
            if (!anyStillRunning) {
                foreach (CanOpen_Object coo in coos) {
                    if (coo.GetiTweenCount() != 0) {
                        anyStillRunning = true;
                        break;
                    }
                }
            }
            return anyStillRunning;
        }

        ///////////////////////////////////////////
        //////// CHANGE FACIAL EXPRESSION /////////
        ///////////////////////////////////////////

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
            actionFinished(success: true);
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
            actionFinished(success: true);
        }

        // XXX: To get all objects contained in a receptacle, target it with this Function and it will return a list of strings, each being the
        // object ID of an object in this receptacle
        public void Contains(string objectId) {
            if (objectId == null) {
                throw new ArgumentNullException();
            }
            SimObjPhysics target = getTargetObject(objectId: objectId, forceAction: true);

            List<string> ids = target.GetAllSimObjectsInReceptacleTriggersByObjectID();
            #if UNITY_EDITOR
                foreach (string s in ids) {
                    Debug.Log(s);
                }
            #endif

            actionFinished(true, ids.ToArray());
        }

        ////////////////////////////////////////
        ////// HIDING AND MASKING OBJECTS //////
        ////////////////////////////////////////

        protected Dictionary<int, Material[]> maskedGameObjectDict = new Dictionary<int, Material[]>();
        protected void maskGameObject(GameObject go, Material mat) {
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

        protected void unmaskGameObject(GameObject go) {
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

        public void HideAllObjectsExcept(string objectId) {
            GameObject target = getTargetObject(objectId: objectId, forceAction: true).gameObject;
            foreach (GameObject go in UnityEngine.Object.FindObjectsOfType<GameObject>()) {
                updateDisplayGameObject(target: go, enabled: false);
            }
            updateDisplayGameObject(target: target, enabled: true);
            actionFinished(true);
        }

        public void HideTranslucentObjects() {
            foreach (SimObjPhysics sop in GameObject.FindObjectsOfType<SimObjPhysics>()) {
                if (sop.hasSecondaryProperty(SimObjSecondaryProperty.CanSeeThrough)) {
                    updateDisplayGameObject(target: sop.gameObject, enabled: false);
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
                    updateDisplayGameObject(target: r.gameObject, enabled: false);
                }
            }
        }

        public void unhideStructureObjects() {
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
                updateDisplayGameObject(target: transform.gameObject, enabled: true);
            }
        }

        public void HideBlueObjects() {
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
                    updateDisplayGameObject(target: go, enabled: true);
                }
            }
            actionFinished(true);
        }

        public void GetAwayFromObject(string objectId, int randomSeed = 0) {
            SimObjPhysics sop = getTargetObject(objectId: objectId, forceAction: true);
            int k = 0;
            while (isAgentCapsuleCollidingWith(sop.gameObject) && k < 20) {
                k++;
                Vector3[] dirs = {
                    transform.forward, -transform.forward, transform.right, -transform.right
                };
                dirs.Shuffle_(randomSeed);

                sop.gameObject.SetActive(false);
                moveInDirection(dirs[0] * gridSize);
                sop.gameObject.SetActive(true);
            }
            if (isAgentCapsuleCollidingWith(sop.gameObject)) {
                throw new InvalidOperationException("Could not get away from " + sop.ObjectID);
            }
            actionFinished(true);
        }

        public void DisableObjectCollisionWithAgent(string objectId) {
            SimObjPhysics sop = getTargetObject(objectId: objectId, forceAction: true);
            foreach (Collider c0 in this.GetComponentsInChildren<Collider>()) {
                foreach (Collider c1 in sop.GetComponentsInChildren<Collider>()) {
                    Physics.IgnoreCollision(c0, c1);
                }
            }
            foreach (Collider c1 in sop.GetComponentsInChildren<Collider>()) {
                collidersToIgnoreDuringMovement.Add(c1);
            }
            actionFinished(true);
        }

        protected void maskSimObj(SimObjPhysics so, Material mat) {
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

        protected void maskSimObj(SimObjPhysics so, Color color) {
            if (!transparentStructureObjectsHidden) {
                HideTransparentStructureObjects();
            }
            Material material = new Material(Shader.Find("Unlit/Color"));
            material.color = color;
            MaskSimObj(so, material);
        }

        protected void unmaskSimObj(SimObjPhysics so) {
            if (transparentStructureObjectsHidden) {
                unhideStructureObjects();
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

        public void EmphasizeObject(string objectId) {
            SimObjPhysics target = getTargetObject(objectId: objectId, forceAction: true);
            HideAll();
            updateDisplayGameObject(target: sop.gameObject, enabled: true);
            maskSimObj(target, Color.magenta);
            actionFinished(true);
        }

        public void UnemphasizeAll() {
            unhideAll();
            foreach (SimObjPhysics so in GameObject.FindObjectsOfType<SimObjPhysics>()) {
                unmaskSimObj(so);
            }
            actionFinished(true);
        }

        public void MaskObject(string objectId) {
            SimObjPhysics target = getTargetObject(objectId: objectId, forceAction: true);
            maskSimObj(so: target, mat: Color.magenta);
            actionFinished(true);
        }

        public void UnmaskObject(string objectId) {
            SimObjPhysics target = getTargetObject(objectId: objectId, forceAction: true);
            unmaskSimObj(target);
            actionFinished(true);
        }

        ///////////////////////////////////////////
        ///// GETTING DISTANCES, NORMALS, ETC /////
        ///////////////////////////////////////////

        protected bool NormalIsApproximatelyUp(Vector3 normal, float tol = 10f) {
            return Vector3.Angle(transform.up, normal) < tol;
        }

        protected bool AnythingAbovePosition(Vector3 position, float distance) {
            Vector3 up = new Vector3(0.0f, 1.0f, 0.0f);
            RaycastHit hit;
            return Physics.Raycast(position, up, out hit, distance);
        }

        protected bool AnythingAbovePositionIgnoreObject(
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

        protected float[, , ] initializeFlatSurfacesOnGrid(int yGridSize, int xGridSize) {
            float[, , ] flatSurfacesOnGrid = new float[2, yGridSize, xGridSize];
            for (int i = 0; i < 2; i++) {
                for (int j = 0; j < yGridSize; j++) {
                    for (int k = 0; k < xGridSize; k++) {
                        flatSurfacesOnGrid[i, j, k] = float.PositiveInfinity;
                    }
                }
            }
            return flatSurfacesOnGrid;
        }

        protected void toggleColliders(IEnumerable<Collider> colliders) {
            foreach (Collider c in colliders) {
                c.enabled = !c.enabled;
            }
        }

        public void FlatSurfacesOnGrid(float x, float y) {
            int xGridSize = (int) Math.Round(x, 0);
            int yGridSize = (int) Math.Round(y, 0);
            flatSurfacesOnGrid = initializeFlatSurfacesOnGrid(yGridSize, xGridSize);

            if (ItemInHand != null) {
                toggleColliders(ItemInHand.GetComponentsInChildren<Collider>());
            }

            int layerMask = 1 << 8;
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

        public void GetMetadataOnGrid(float x, float y) {
            int xGridSize = (int) Math.Round(x, 0);
            int yGridSize = (int) Math.Round(y, 0);
            distances = new float[yGridSize, xGridSize];
            normals = new float[3, yGridSize, xGridSize];
            isOpenableGrid = new bool[yGridSize, xGridSize];

            if (ItemInHand != null) {
                toggleColliders(ItemInHand.GetComponentsInChildren<Collider>());
            }

            int layerMask = 1 << 8;
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


        // Sweeptest to see if the object Agent is holding will prohibit movement
        public void assertAgentCanStandOrCrouch() {
            // if there is nothing in our hand, we are good, return!
            if (ItemInHand == null) {
                return;
            }

            // otherwise we are holding an object and need to do a sweep using that object's rb
            Vector3 dir = new Vector3();

            if (isStanding()) {
                dir = new Vector3(0.0f, -1f, 0.0f);
            } else {
                dir = new Vector3(0.0f, 1f, 0.0f);
            }

            Rigidbody rb = ItemInHand.GetComponent<Rigidbody>();

            RaycastHit[] sweepResults = rb.SweepTestAll(dir, standingLocalCameraPosition.y, QueryTriggerInteraction.Ignore);
            foreach (RaycastHit res in sweepResults) {
                // did the item in the hand touch the agent? if so, ignore it's fine
                // also ignore Untagged because the Transparent_RB of transparent objects need to be ignored for movement
                // the actual rigidbody of the SimObjPhysics parent object of the transparent_rb should block correctly by having the
                // checkMoveAction() in the BaseFPSAgentController fail when the agent collides and gets shoved back
                if (res.transform.tag == "Player" || res.transform.tag == "Untagged") {
                    break;
                } else {
                    throw new InvalidOperationException($"{res.transform.name} is blocking the Agent from moving {dir} with {ItemInHand.name}");
                }
            }
        }

        public bool isStanding() {
            return standingLocalCameraPosition == m_Camera.transform.localPosition;
        }

        protected void crouch() {
            m_Camera.transform.localPosition = new Vector3(
                standingLocalCameraPosition.x,
                crouchingLocalCameraPosition.y,
                standingLocalCameraPosition.z
            );
        }

        protected void stand() {
            m_Camera.transform.localPosition = standingLocalCameraPosition;
        }

        public void Crouch() {
            if (!isStanding()) {
                actionFinished(true);
                return;
            }

            assertAgentCanStandOrCrouch();
            crouch();
            actionFinished(true);
        }

        public void Stand() {
            if (isStanding()) {
                actionFinished(true);
                return;
            }

            assertAgentCanStandOrCrouch();
            stand();
            actionFinished(true);
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

                        if (Physics.Raycast(
                            m_Camera.transform.position,
                            point.position - m_Camera.transform.position,
                            out hit, Mathf.Infinity, (1 << 8) | (1 << 10))
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
                #if UNITY_EDITOR
                    Debug.Log("Error! Set at least 1 visibility point on SimObjPhysics prefab!");
                #endif
            }

            return false;
        }

        public bool objectIsWithinViewport(SimObjPhysics sop) {
            if (sop.VisibilityPoints.Length > 0) {
                Transform[] visPoints = sop.VisibilityPoints;
                foreach (Transform point in visPoints) {
                    Vector3 viewPoint = m_Camera.WorldToViewportPoint(point.position);
                    float ViewPointRangeHigh = 1.0f;
                    float ViewPointRangeLow = 0.0f;

                    if (viewPoint.z > 0 &&
                        viewPoint.x < ViewPointRangeHigh && viewPoint.x > ViewPointRangeLow && // within x bounds of viewport
                        viewPoint.y < ViewPointRangeHigh && viewPoint.y > ViewPointRangeLow // within y bounds of viewport
                    ) {
                            return true;
                    }
                }
            } else {
                #if UNITY_EDITOR
                Debug.Log("Error! Set at least 1 visibility point on SimObjPhysics prefab!");
                #endif
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
                        if (CheckIfVisibilityPointInViewport(sop, point, m_Camera, false) ||
                            CheckIfVisibilityPointInViewport(sop, point, m_Camera, true)
                        ) {
                            updateAllAgentCollidersForVisibilityCheck(true);
                            return true;
                        }
                    }
                }
            } else {
                #if UNITY_EDITOR
                    Debug.Log("Error! Set at least 1 visibility point on SimObjPhysics prefab!");
                #endif
            }
            updateAllAgentCollidersForVisibilityCheck(true);
            return false;
        }

        protected static void Shuffle<T>(System.Random rng, T[] array) {
            // Taken from https:// stackoverflow.com/questions/108819/best-way-to-randomize-an-array-with-net
            int n = array.Length;
            while (n > 1) {
                int k = rng.Next(n--);
                T temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
        }

        protected int xzManhattanDistance(Vector3 p0, Vector3 p1, float gridSize) {
            return (Math.Abs(Convert.ToInt32((p0.x - p1.x) / gridSize)) +
                Math.Abs(Convert.ToInt32((p0.z - p1.z) / gridSize)));
        }

        public void ExhaustiveSearchForItem(string objectId, Vector3[] positions = null, float randomSeed = 0) {
            SimObjPhysics theObject = getTargetObject(objectId: objectId, forceAction: true);

            if (positions == null) {
                positions = getReachablePositions();
            }

            bool wasStanding = isStanding();
            Vector3 oldPosition = transform.position;
            Quaternion oldRotation = transform.rotation;
            if (ItemInHand != null) {
                ItemInHand.gameObject.SetActive(false);
            }

            Shuffle(new System.Random(randomSeed), positions);

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

                // Standing / Crouching
                for (int j = 0; j < 2; j++) { 
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
        protected List<Dictionary<string, object>> getInteractablePoses(
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
            } else if ((float) maxDistance <= 0) {
                throw new ArgumentOutOfRangeException("maxDistance must be >= 0 meters from the object.");
            } else {
                maxDistanceFloat = (float) maxDistance;
            }

            SimObjPhysics theObject = getTargetObject(objectId: objectId, forceAction: true);

            // Populate default standings. Note that these are boolean because that's
            // the most natural integration with Teleport
            if (standings == null) {
                standings = new bool[] {false, true};
            }

            // populate default horizons
            if (horizons == null) {
                horizons = new float[] {-30, 0, 30, 60};
            } else {
                foreach (float horizon in horizons) {
                    // recall that horizon=60 is look down 60 degrees and horizon=-30 is look up 30 degrees
                    if (horizon > maxDownwardLookAngle || horizon < -maxUpwardLookAngle) {
                        throw new ArgumentException($"Each horizon must be in [{-maxUpwardLookAngle}:{maxDownwardLookAngle}]");
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
                int offset = (int) Math.Round(transform.eulerAngles.y % rotateStepDegrees);

                // Examples:
                // if rotateStepDegrees=10 and offset=70, then the paths would be [70, 80, ..., 400, 410, 420].
                // if rotateStepDegrees=90 and offset=10, then the paths would be [10, 100, 190, 280]
                rotations = new float[(int) Math.Round(360 / rotateStepDegrees)];
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
            Bounds objectBounds = new Bounds(
                center: new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity),
                size: new Vector3(-float.PositiveInfinity, -float.PositiveInfinity, -float.PositiveInfinity)
            );
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
            string[] keys = {"x", "y", "z", "rotation", "standing", "horizon"};

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
                actionFinished(success: true, actionReturn: validAgentPoses);
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
            string[] keys = {"x", "y", "z", "rotation", "standing", "horizon"};
            foreach (string key in keys) {
                d[key] = new List<float>();
            }
            foreach (Dictionary<string, object> pose in interactablePoses) {
                foreach (string key in keys) {
                    if (key == "standing") {
                        // standing is converted from true => 1 to false => 0, for backwards compatibility
                        d[key].Add((bool) pose[key] ? 1 : 0);
                    } else {
                        // all other keys have float outputs
                        d[key].Add((float) pose[key]);
                    }
                }
            }
            actionFinished(true, d);
        }

        // protected helper for NumberOfPositionsFromWhichItemIsVisible
        protected int numVisiblePositions(string objectId, bool markActionFinished, Vector3[] positions = null, int maxPoses = int.MaxValue) {
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
                actionFinished(success: interactablePoses != null);
            }
            return interactablePoses == null ? 0 : interactablePoses.Count;
        }

        // Similar to GetInteractablePositions, but with horizon=0 and maxDistance like infinity
        public void NumberOfPositionsFromWhichItemIsVisible(string objectId, Vector3[] positions = null) {
            numVisiblePositions(objectId: objectId, positions: positions, markActionFinished: true);
        }

        public void TogglePhysics() {
            Physics.autoSimulation = !Physics.autoSimulation;
            actionFinished(true);
        }

        // Why are we using x?
        public void ChangeOpenSpeed(float x) {
            foreach (CanOpen_Object coo in GameObject.FindObjectsOfType<CanOpen_Object>()) {
                coo.animationTime = x;
            }
            actionFinished(true);
        }

        public void GetSceneBounds() {
            reachablePositions = new Vector3[2];
            reachablePositions[0] = agentManager.SceneBounds.min;
            reachablePositions[1] = agentManager.SceneBounds.max;
            #if UNITY_EDITOR
                Debug.Log(reachablePositions[0]);
                Debug.Log(reachablePositions[1]);
            #endif
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
            int layerMask = 1 << 8;
            foreach (Collider c in PhysicsExtensions.OverlapCapsule(GetComponent<CapsuleCollider>(), layerMask, QueryTriggerInteraction.Ignore)) {
                if (hasAncestor(c.transform.gameObject, otherGameObject)) {
                    return true;
                }
            }
            return false;
        }

        protected bool isHandObjectCollidingWith(GameObject otherGameObject) {
            if (ItemInHand == null) {
                return false;
            }
            int layerMask = 1 << 8;
            foreach (CapsuleCollider cc in ItemInHand.GetComponentsInChildren<CapsuleCollider>()) {
                foreach (Collider c in PhysicsExtensions.OverlapCapsule(cc, layerMask, QueryTriggerInteraction.Ignore)) {
                    if (hasAncestor(c.transform.gameObject, otherGameObject)) {
                        return true;
                    }
                }
            }
            foreach (BoxCollider bc in ItemInHand.GetComponentsInChildren<BoxCollider>()) {
                foreach (Collider c in PhysicsExtensions.OverlapBox(bc, layerMask, QueryTriggerInteraction.Ignore)) {
                    if (!hasAncestor(c.transform.gameObject, otherGameObject)) {
                        return true;
                    }
                }
            }
            foreach (SphereCollider sc in ItemInHand.GetComponentsInChildren<SphereCollider>()) {
                foreach (Collider c in PhysicsExtensions.OverlapSphere(sc, layerMask, QueryTriggerInteraction.Ignore)) {
                    if (!hasAncestor(c.transform.gameObject, otherGameObject)) {
                        return true;
                    }
                }
            }
            return false;
        }

        public float roundToGridSize(float x, float gridSize, bool roundUp) {
            int mFactor = (int) (1.0f / gridSize);
            if (Math.Abs(mFactor - 1.0f / gridSize) > 1e-3) {
                throw new Exception("1.0 / gridSize should be an integer.");
            }
            if (roundUp) {
                return (float) Math.Ceiling(mFactor * x) / mFactor;
            } else {
                return (float) Math.Floor(mFactor * x) / mFactor;
            }
        }

        public void RandomlyMoveAgent(int randomSeed = 0) {
            #if UNITY_EDITOR
                randomSeed = UnityEngine.Random.Range(0, 1000000);
            #endif
            reachablePositions = getReachablePositions();
            var orientations = new float[] {0, 90, 180, 270};
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

            if (!success) {
                throw new InvalidOperationException("Could not find a position in which the agent and object fit.");
            }
            actionFinished(true, reachablePositions);
        }

        public void GetReachablePositionsForObject(
            string objectId,
            Vector3[] positions = null
        ) {
            SimObjPhysics sop = getTargetObject(objectId: objectId, forceAction: true);
            Vector3 startPos = sop.transform.position;
            Quaternion startRot = sop.transform.rotation;

            if (positions == null || positions.Count != 0) {
                positions = getReachablePositions();
            }

            Bounds b = new Bounds(
                center: new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity),
                size: new Vector3(-float.PositiveInfinity, -float.PositiveInfinity, -float.PositiveInfinity)
            );
            foreach (Vector3 p in positions) {
                b.Encapsulate(p);
            }

            float xMin = roundToGridSize(b.min.x - gridSize * 3, gridSize, true);
            float xMax = roundToGridSize(b.max.x + gridSize * 3, gridSize, false);
            float zMin = roundToGridSize(b.min.z - gridSize * 3, gridSize, true);
            float zMax = roundToGridSize(b.max.z + gridSize * 3, gridSize, false);

            List<GameObject> agentGameObjects = new List<GameObject>();
            foreach (BaseFPSAgentController agent in agentManager.agents) {
                agentGameObjects.Add(agent.gameObject);
            }

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

                for (int i = 0; i <= (int) ((xMax - xMin) / gridSize); i++) {
                    for (int j = 0; j <= (int) ((zMax - zMin) / gridSize); j++) {
                        Vector3 p = new Vector3(xMin + gridSize * i, startPos.y, zMin + j * gridSize);
                        sop.transform.position = p;
                        if (!UtilityFunctions.isObjectColliding(
                                sop.BoundingBox.gameObject,
                                agentGameObjects,
                                0.0f,
                                true
                            )) {
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
            if (Physics.Raycast(position, Vector3.down, out hit, 10f, (1<<8 | 1<<10), QueryTriggerInteraction.Ignore)) {
                point = hit.point;
                return point;
            } else {
                // nothing hit, return the original position?
                return position;
            }
        }

        protected bool stringInSomeAncestorName(GameObject go, string[] strs) {
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

        // objectId works as a string[], basically...
        public void HideObscuringObjects(string objectId) {
            string objType = "";
            if (objectId != null && objectId != "") {
                string[] split = objectId.Split('|');
                if (split.Length != 0) {
                    objType = objectId.Split('|') [0];
                }
            }
            int xGridSize = 100;
            int yGridSize = 100;
            int layerMask = 1 << 8;
            for (int i = 0; i < yGridSize; i++) {
                for (int j = 0; j < xGridSize; j++) {
                    float x = j * (1.0f / xGridSize) + (0.5f / xGridSize);
                    float y = (1.0f - (0.5f / yGridSize)) - i * (1.0f / yGridSize);
                    Ray ray = m_Camera.ViewportPointToRay(new Vector3(x, y, 0));
                    RaycastHit hit;
                    while (true) {
                        if (Physics.Raycast(ray, out hit, 10f, layerMask)) {
                            updateDisplayGameObject(target: hit.transform.gameObject, enabled: false);
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

        ///////////////////////////////////////////
        ///////////// CREATE OBJECT ///////////////
        ///////////////////////////////////////////

        protected SimObjPhysics createObject(
            string objectType,
            Vector3 position,
            Vector3 rotation,
            bool randomizeObjectAppearance = false,
            int objectVariation = 0,
            bool forceAction = false
        ) {
            if (objectType == null) {
                throw new ArgumentNullException("Please give valid Object Type from SimObjType enum list");
            }

            // spawn the object at the agent's hand position
            InstantiatePrefabTest script = physicsSceneManager.GetComponent<InstantiatePrefabTest>();
            SimObjPhysics sop = script.SpawnObject(
                objectType: objectType,
                randomize: randomizeObjectAppearance,
                variation: objectVariation,
                position: AgentHand.transform.position,
                rotation: AgentHand.transform.rotation.eulerAngles,
                spawningInHand: true,
                ignoreChecks: forceAction
            );

            if (sop == null) {
                throw new NullReferenceException("Failed to create object, are you sure it can be spawned?");
            }

            // also update the Physics Scene Manager with this new object
            physicsSceneManager.AddToObjectsInScene(sop);
            return sop;
        }

        // spawns object in agent's hand with the same orientation as the agent's hand
        public void CreateObject(
            string objectType,
            bool randomizeObjectAppearance = false,
            int objectVariation = 0,
            bool forceAction = false
        ) {
            if (ItemInHand != null) {
                throw new InvalidOperationException("Already have an object in hand, can't create a new one to put there.");
            }
            SimObjPhysics sop = createObject(
                objectType: objectType,
                position: AgentHand.transform.position,
                rotation: AgentHand.transform.rotation.eulerAngles,
                randomizeObjectAppearance: randomizeObjectAppearance,
                objectVariation: objectVariation,
                forceAction: forceAction
            );
            PickupObject(objectId: sop.objectId, forceAction: true);
        }

        [ObsoleteAttribute(message: "This action is deprecated. Call CreateObject instead.", error: false)]
        public void CreateObjectAtLocation(
            string objectType,
            Vector3 position,
            Vector3 rotation,
            bool randomizeObjectAppearance = false,
            int objectVariation = 0,
            bool forceAction = false
        ) {
            SimObjPhysics sop = CreateObject(
                objectType: objectType,
                position: position,
                rotation: rotation,
                randomizeObjectAppearance: randomizeObjectAppearance,
                objectVariation: objectVariation,
                forceAction: forceAction
            );
        }

        public void CreateObject(
            string objectType,
            Vector3 position,
            Vector3 rotation,
            bool randomizeObjectAppearance = false,
            int objectVariation = 0,
            bool forceAction = false
        ) {
            if (objectType == null || position == null || rotation == null) {
                throw new ArgumentNullException();
            }
            SimObjPhysics sop = createObject(
                objectType: objectType,
                position: position,
                rotation: rotation,
                randomizeObjectAppearance: randomizeObjectAppearance,
                objectVariation: objectVariation,
                forceAction: forceAction
            );
            actionFinished(true, sop.ObjectID);
        }

        public void CreateObjectOnFloor(string objectType, float x, float z, Vector3? rotation = null) {
            InstantiatePrefabTest script = physicsSceneManager.GetComponent<InstantiatePrefabTest>();
            Bounds b = script.BoundsOfObject(objectType, 1);
            if (b.min.x == float.PositiveInfinity) {
                throw new InvalidOperationException("Could not get bounds for the object to be created on the floor");
            }
            float y = b.extents.y + getFloorY(x, z) + 0.1f;
            Vector3 position = new Vector3(x, y, z);
            CreateObjectAtLocation(
                objectType: objectType,
                position: position,
                rotation: rotation == null ? new Vector3(x: 0, y: 0, z: 0) : rotation
            );
        }

        protected bool randomlyPlaceObjectOnFloor(SimObjPhysics sop, Vector3[] candidatePositions) {
            var oldPosition = sop.transform.position;
            var oldRotation = sop.transform.rotation;

            sop.transform.rotation = Quaternion.identity;
            Bounds b = new Bounds(
                center: new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity),
                size: new Vector3(-float.PositiveInfinity, -float.PositiveInfinity, -float.PositiveInfinity)
            );
            foreach (Renderer r in sop.GetComponentsInChildren<Renderer>()) {
                if (r.enabled) {
                    b.Encapsulate(r.bounds);
                }
            }

            List<Vector3> shuffledCurrentlyReachable = (List<Vector3>) candidatePositions.ToList().Shuffle_();
            float[] rotations = { 0f, 90f, 180f, 270f };
            List<float> shuffledRotations = (List<float>) rotations.ToList().Shuffle_();
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
                throw InvalidOperationException("Could not get bounds of object with type " + objectType);
            }

            System.Random rnd = new System.Random();
            Vector3[] shuffledCurrentlyReachable = candidatePositions.OrderBy(x => rnd.Next()).ToArray();
            float[] rotations = { 0f, 90f, 180f, 270f };
            float[] shuffledRotations = rotations.OrderBy(x => rnd.Next()).ToArray();
            foreach (Vector3 position in shuffledCurrentlyReachable) {
                float y = b.extents.y + getFloorY(position.x, position.y, position.z) + 0.01f;
                foreach (float r in shuffledRotations) {
                    return createObject(
                        objectType: objectType,
                        position: new Vector3(position.x, y, position.z),
                        rotation: new Vector3(0.0f, r, 0.0f),
                        objectVariation: objectVariation
                    );
                }
            }
            return null;
        }

        protected SimObjPhysics randomlyCreateAndPlaceObjectOnFloor(string objectType, int objectVariation) {
            return randomlyCreateAndPlaceObjectOnFloor(objectType, objectVariation, getReachablePositions());
        }

        public void RandomlyCreateAndPlaceObjectOnFloor(string objectType, int objectVariation = 0) {
            SimObjPhysics objectCreated = randomlyCreateAndPlaceObjectOnFloor(objectType, objectVariation);
            if (!objectCreated) {
                throw new InvalidOperationException("Failed to randomly create object. " + errorMessage);
            }
            actionFinished(true, objectCreated.ObjectID);
        }

        public void GetPositionsObjectVisibleFrom(string objectId, Vector3[] positions) {
            // TODO: rewrite with GetInteractablePoses
            SimObjPhysics sop = getTargetObject(objectId: objectId, forceAction: true);

            Vector3 savedPosition = transform.position;
            Quaternion savedRotation = transform.rotation;

            float[] rotations = { 0f, 90f, 180f, 270f };
            if (positions == null) {
                positions = getReachablePositions();
            }

            List<Vector3> goodPositions = new List<Vector3>();
            List<float> goodRotations = new List<float>();

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
                throw new InvalidOperationException("Point not in viewport.");
            }

            // Translate to coordinates from top left of screen
            actionFinished(true, new Vector3(point.x, 1.0f - point.y, point.z));
        }

        protected float approxPercentScreenObjectOccupies(SimObjPhysics sop, bool updateVisibilityColliders=true) {
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

                    if (CheckIfVisibilityPointInViewport(sop, point, m_Camera, false)) {
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
            SimObjPhysics sop = getTargetObject(objectId: objectId, forceAction: true);
            actionFinished(true, approxPercentScreenObjectOccupies(sop));
        }

        public void ApproxPercentScreenObjectFromPositions(string objectId, Vector3[] positions = null) {
            SimObjPhysics sop = getTargetObject(objectId: objectId, forceAction: true);
            Vector3[] positions = null;
            if (positions != null && positions.Count != 0) {
                positions = positions.ToArray();
            } else {
                positions = getReachablePositions();
            }

            Vector3 oldPosition = transform.position;
            Quaternion oldRotation = transform.rotation;
            float[] rotations = {0f, 90f, 180f, 270f};

            List<float[]> positionAndApproxAmountVisible = new List<float[]>();

            updateAllAgentCollidersForVisibilityCheck(false);
            foreach (Vector3 position in positions) {
                transform.position = position;
                foreach (float rotation in rotations) {
                    transform.rotation = Quaternion.Euler(0f, rotation, 0f);
                    float approxVisible = approxPercentScreenObjectOccupies(sop, false);
                    if (approxVisible > 0.0f) {
                        float[] tuple = {position.x, position.y, position.z, transform.eulerAngles.y};
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

        public void ObjectsVisibleFromPositions(Vector3[] positions = null) {
            if (positions == null) {
                positions = getReachablePositions();
            }

            Vector3 oldPosition = transform.position;
            Quaternion oldRotation = transform.rotation;
            float[] rotations = {0f, 90f, 180f, 270f};

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
                        float[] tuple = {position.x, position.y, position.z, transform.eulerAngles.y};
                        l.Add(tuple);
                    }
                }
            }

            transform.position = oldPosition;
            transform.rotation = oldRotation;

            actionFinished(true, objectIdToVisiblePositions);
        }

        // Remains for cordial-sync backwards compatibility.
        [ObsoleteAttribute(message: "This action is deprecated. Please use DisableObjectsOfType.", error: false)]
        public void DisableAllObjectsOfType(string objectType = null, string objectId = "") {
            base.DisableObjectsOfType(objectType: objectType == null ? objectId : objectType);
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

        public void RandomizeHideSeekObjects(int randomSeed, float removeProb) {
            System.Random rnd = new System.Random(randomSeed);

            if (!physicsSceneManager.ToggleHideAndSeek(true)) {
                throw new InvalidOperationException("Hide and Seek object reference not set, nothing to randomize.");
            }

            foreach (Transform child in physicsSceneManager.HideAndSeek.transform) {
                child.gameObject.SetActive(rnd.NextDouble() > removeProb);
            }
            physicsSceneManager.SetupScene();
            physicsSceneManager.ResetObjectIdToSimObjPhysics();

            snapAgentToGrid(); // This snapping seems necessary for some reason, really doesn't make any sense.
            actionFinished(true);
        }

        // @pOpen is the probability of opening an openable object.
        // @randOpenness specifies if the openness for each opened object should be random, between 0% : 100%, or always 100%.
        public void RandomlyOpenCloseObjects(
            int? randomSeed = null,
            bool simplifyPhysics = false,
            float pOpen = 0.5f,
            bool randOpenness = true
        ) {
            System.Random rnd;
            System.Random rndOpenness;
            if (randomSeed == null) {
                // truly random!
                rnd = new System.Random();
                rndOpenness = new System.Random();
            } else {
                rnd = new System.Random((int) randomSeed);
                rndOpenness = new System.Random(((int) randomSeed) + 42);
            }

            foreach (SimObjPhysics so in GameObject.FindObjectsOfType<SimObjPhysics>()) {
                if (so.GetComponent<CanOpen_Object>()) {
                    // randomly opens an object to a random openness
                    if (rnd.NextDouble() < pOpen) {
                        openObject(
                            target: so,
                            openness: randOpenness ? (float) rndOpenness.NextDouble() : 1,
                            simplifyPhysics: simplifyPhysics,
                            markActionFinished: false
                        );
                    }
                }
            }
            actionFinished(true);
        }

        public void GetApproximateVolume(string objectId) {
            SimObjPhysics sop = getTargetObject(objectId: objectId, forceAction: true);
            Quaternion oldRotation = sop.transform.rotation;
            sop.transform.rotation = Quaternion.identity;
            Bounds objBounds = new Bounds(
                center: new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity),
                size: new Vector3(-float.PositiveInfinity, -float.PositiveInfinity, -float.PositiveInfinity)
            );

            bool hasActiveRenderer = false;
            foreach (Renderer r in sop.GetComponentsInChildren<Renderer>()) {
                if (r.enabled) {
                    hasActiveRenderer = true;
                    objBounds.Encapsulate(r.bounds);
                }
            }

            if (!hasActiveRenderer) {
                throw new InvalidOperationException("Cannot get bounds for " + objectId + " as it has no attached (and active) renderers.");
            }

            sop.transform.rotation = oldRotation;
            Vector3 diffs = objBounds.max - objBounds.min;
            actionFloatReturn = diffs.x * diffs.y * diffs.z;

            #if UNITY_EDITOR
                Debug.Log("Volume is " + actionFloatReturn);
            #endif

            actionFinished(true);
        }

        public void GetVolumeOfAllObjects() {
            List<string> objectIds = new List<string>();
            List<float> volumes = new List<float>();
            foreach (SimObjPhysics so in FindObjectsOfType<SimObjPhysics>()) {
                Quaternion oldRotation = so.transform.rotation;
                so.transform.rotation = Quaternion.identity;
                Bounds objBounds = new Bounds(
                    center: new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity),
                    size: new Vector3(-float.PositiveInfinity, -float.PositiveInfinity, -float.PositiveInfinity)
                );
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

            int layerMask = 1 << 8;
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

        protected IEnumerator CoverSurfacesWithHelper(
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

            Material redMaterial = (Material) Resources.Load("RED", typeof(Material));
            Material greenMaterial = (Material) Resources.Load("GREEN", typeof(Material));
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

        protected void createCubeSurrounding(Bounds bounds) {
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
            cube.transform.localScale = new Vector3(xLen + 2 * (size + offset), size, zLen + 2 * (size + offset));

            // Bottom
            cube = Instantiate(
                original: Resources.Load("BlueCube") as GameObject,
                position: new Vector3(center.x, min.y - offset - size / 2, center.z),
                rotation: Quaternion.identity
            ) as GameObject;
            cube.transform.localScale = new Vector3(xLen + 2 * (size + offset), size, zLen + 2 * (size + offset));

            // z min
            cube = Instantiate(
                original: Resources.Load("BlueCube") as GameObject,
                position: new Vector3(center.x, center.y, min.z - offset - size / 2),
                rotation: Quaternion.identity
            ) as GameObject;
            cube.transform.localScale = new Vector3(xLen + 2 * (size + offset), yLen + 2 * offset, size);

            // z max
            cube = Instantiate(
                original: Resources.Load("BlueCube") as GameObject,
                position: new Vector3(center.x, center.y, max.z + offset + size / 2),
                rotation: Quaternion.identity
            ) as GameObject;
            cube.transform.localScale = new Vector3(xLen + 2 * (size + offset), yLen + 2 * offset, size);

            // x min
            cube = Instantiate(
                original: Resources.Load("BlueCube") as GameObject,
                position: new Vector3(min.x - offset - size / 2, center.y, center.z),
                rotation: Quaternion.identity
            ) as GameObject;
            cube.transform.localScale = new Vector3(size, yLen + 2 * offset, zLen + 2 * offset);

            // x max
            cube = Instantiate(
                original: Resources.Load("BlueCube") as GameObject,
                position: new Vector3(max.x + offset + size / 2, center.y, center.z),
                rotation: Quaternion.identity
            ) as GameObject;
            cube.transform.localScale = new Vector3(size, yLen + 2 * offset, zLen + 2 * offset);
        }

        protected List<RaycastHit> RaycastWithRepeatHits(
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

        public void CoverSurfacesWith(string objectType, int objectVariation, float x = 0, float z = 0) {
            Vector3[] reachablePositions = getReachablePositions();

            Bounds b = new Bounds();
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
            SimObjPhysics objForBounds = script.SpawnObject(objectType, false, objectVariation, new Vector3(0.0f, b.max.y + 10.0f, 0.0f), transform.eulerAngles, false, true);

            Bounds objBounds = new Bounds(
                center: new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity),
                size: new Vector3(-float.PositiveInfinity, -float.PositiveInfinity, -float.PositiveInfinity)
            );
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
            float xStepSize = Math.Max(Math.Max(xExtent, 0.1f), x);
            float zStepSize = Math.Max(Math.Max(zExtent, 0.1f), z);
            int numXSteps = (int) (xRoomSize / xStepSize);
            int numZSteps = (int) (zRoomSize / zStepSize);
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

            int layerMask = 1 << 8;
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
                        if (hitSimObj == null || hitSimObj.ObjectID.Split('|') [0] != objectType) {
                            Vector3 halfExtents = new Vector3(xExtent / 2.1f, yExtent / 2.1f, zExtent / 2.1f);
                            Vector3 center = hit.point + objCenterRelPos + yOffset;
                            Collider[] colliders = Physics.OverlapBox(center, halfExtents, Quaternion.identity, layerMask);
                            if (colliders.Length == 0) {
                                k++;
                                SimObjPhysics newObj = script.SpawnObject(objectType, false, objectVariation, center - objCenterRelPos, transform.eulerAngles, false, true);
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
                    foreach (SimObjPhysics sop in FindObjectsOfType<SimObjPhysics>()) {
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
            foreach (SimObjPhysics sop in FindObjectsOfType<SimObjPhysics>()) {
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

        protected IEnumerator SpamObjectsInRoomHelper(int n, List<SimObjPhysics> newObjects) {
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

            Bounds b = new Bounds();
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

                    Bounds objBounds = new Bounds(
                        center: new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity),
                        size: new Vector3(-float.PositiveInfinity, -float.PositiveInfinity, -float.PositiveInfinity)
                    );
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
            int layerMask = 1 << 8;
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

        public void ChangeLightSet(int objectVariation) {
            if (objectVariation > 10 || objectVariation < 1) {
                throw new ArgumentOutOfRangeException("objectVariation must be an integer in [1:10]");
            }

            GameObject lightTransform = GameObject.Find("Lighting");
            lightTransform.GetComponent<ChangeLighting>().SetLights(objectVariation);
            actionFinished(success: true);
        }

        ///////////////////////////////////////////
        ////////////// COOK OBJECT ////////////////
        ///////////////////////////////////////////

        // swap an object's materials out to the cooked version of the object :)
        protected void cookObject(SimObjPhysics target, bool markActionFinished) {
            if (target == null) {
                throw new ArgumentNullException();
            }

            CookObject cookComponent = target.GetComponent<CookObject>();
            if (cookComponent == null) {
                throw new InvalidOperationException("Target object is not cookable!");
            }
            if (!cookComponent.IsCooked()) {
                cookComponent.Cook();
            }

            if (markActionFinished) {
                actionFinished(success: true);
            }
        }

        public void CookObject(string objectId, bool forceAction = false) {
            SimObjPhysics target = getTargetObject(objectId: objectId, forceAction: forceAction);
            cookObject(target: target, markActionFinished: true);
        }

        public void CookObject(float x, float y, bool forceAction = false) {
            SimObjPhysics target = getTargetObject(x: x, y: y, forceAction: forceAction);
            cookObject(target: target, markActionFinished: true);
        }

        ///////////////////////////////////////////
        ///////////// SLICE OBJECT ////////////////
        ///////////////////////////////////////////

        protected void sliceObject(SimObjPhysics target, bool markActionFinished) {
            if (target == null) {
                throw new ArgumentNullException();
            }

            if (ItemInHand != null && target.transform == ItemInHand.transform) {
                throw new InvalidOperationException("target object cannot be sliced if it is in the agent's hand");
            }

            SliceObject sliceComponent = !target.GetComponent<SliceObject>();
            if (sliceComponent == null) {
                throw new InvalidOperationException($"{target.transform.name} Does not have the CanBeSliced property!");
            }
            if (!sliceComponent.IsSliced()) {
                sliceComponent.Slice();
            }

            if (markActionFinished) {
                actionFinished(success: true);
            }
        }

        public void SliceObject(float x, float y, bool forceAction = false) {
            SimObjPhysics target = getTargetObject(x: x, y: y, forceAction: forceAction);
            sliceObject(target: target, markActionFinished: true);
        }

        public void SliceObject(string objectId, bool forceAction = false) {
            SimObjPhysics target = getTargetObject(objectId: objectId, forceAction: forceAction);
            sliceObject(target: target, markActionFinished: true);
        }

        ///////////////////////////////////////////
        ///////////// BREAK OBJECT ////////////////
        ///////////////////////////////////////////

        protected void breakObject(SimObjPhysics target, bool markActionFinished) {
            if (target == null) {
                throw new ArgumentNullException();
            }

            Break breakComponent = target.GetComponentInChildren<Break>();
            if (breakComponent == null) {
                throw new InvalidOperationException($"{target.transform.name} does not have the CanBreak property!");
            }

            if (!breakComponent.isBroken()) {
                // if the object is in the agent's hand, we need to reset the agent hand booleans and other cleanup as well
                if (target.isInAgentHand) {
                    // if the target is also a Receptacle, drop contained objects first
                    if (target.hasSecondaryProperty(SimObjSecondaryProperty.Receptacle)) {
                        // drop contained objects as well
                        dropContainedObjects(target: targetsop, reparentContainedObjects: true, forceKinematic: false);
                    }

                    targetsop.isInAgentHand = false;
                    ItemInHand = null;
                    DefaultAgentHand();
                }

                // ok now we are ready to break go go go
                breakComponent.BreakObject(collision: null);
            }

            if (markActionFinished) {
                actionFinished(success: true);
            }
        }

        public void BreakObject(float x, float y, bool forceAction = false) {
            SimObjPhysics target = getTargetObject(x: x, y: y, forceAction: forceAction);
            breakObject(target: target, markActionFinished: true);
        }

        public void BreakObject(string objectId, bool forceAction = false) {
            SimObjPhysics target = getTargetObject(objectId: objectId, forceAction: forceAction);
            breakObject(target: target, markActionFinished: true);
        }

        ///////////////////////////////////////////
        ////////////// DIRTY OBJECT ///////////////
        ///////////////////////////////////////////

        protected void dirtyObject(SimObjPhysics target, bool markActionFinished) {
            if (target == null) {
                throw new ArgumentNullException();
            }

            Dirty dirtyComponent = target.GetComponent<Dirty>();
            if (dirtyComponent == null) {
                throw new InvalidOperationException($"{target.transform.name} does not have CanBeDirty property!");
            }
            if (!dirtyComponent.IsDirty()) {
                dirt.ToggleCleanOrDirty();
            }

            if (markActionFinished) {
                actionFinished(success: true);
            }
        }

        public void DirtyObject(float x, float y, bool forceAction = false) {
            SimObjPhysics target = getTargetObject(x: x, y: y, forceAction: forceAction);
            dirtyObject(target: target, markActionFinished: true);
        }

        public void DirtyObject(string objectId, bool forceAction = false) {
            SimObjPhysics target = getTargetObject(objectId: objectId, forceAction: forceAction);
            dirtyObject(target: target, markActionFinished: true);
        }

        ///////////////////////////////////////////
        ///////////// CLEAN OBJECT ////////////////
        ///////////////////////////////////////////

        protected void cleanObject(SimObjPhysics target, bool markActionFinished) {
            if (target == null) {
                throw new ArgumentNullException();
            }

            Dirty dirtyComponent = target.GetComponent<Dirty>();
            if (dirtyComponent == null) {
                throw InvalidOperationException($"{target.transform.name} does not have dirtyable property!");
            }
            if (dirtyComponent.IsDirty()) {
                dirt.ToggleCleanOrDirty();
            }

            if (markActionFinished) {
                actionFinished(success: true);
            }
        }

        public void CleanObject(float x, float y, bool forceAction = false) {
            SimObjPhysics target = getTargetObject(x: x, y: y, forceAction: forceAction);
            cleanObject(target: target, markActionFinished: true);
        }

        public void CleanObject(string objectId, bool forceAction = false) {
            SimObjPhysics target = getTargetObject(objectId: objectId, forceAction: forceAction);
            cleanObject(target: target, markActionFinished: true);
        }

        ///////////////////////////////////////////
        ///////// FILL OBJECT WITH LIQUID /////////
        ///////////////////////////////////////////

        // fill an object with a liquid specified by fillLiquid - coffee, water, soap, wine, etc.
        protected void fillObjectWithLiquid(SimObjPhysics target, string fillLiquid, bool markActionFinished) {
            if (target == null || fillLiquid == null) {
                throw new ArgumentNullException();
            }

            Fill fillComponent = target.GetComponent<Fill>();
            if (fillComponent == null) {
                throw new InvalidOperationException($"{target.transform.name} does not have CanBeFilled property!");
            }

            fillComponent.FillObject(fillLiquid: fillLiquid);

            if (markActionFinished) {
                actionFinished(success: true);
            }
        }

        public void FillObjectWithLiquid(float x, float y, bool forceAction = false) {
            SimObjPhysics target = getTargetObject(x: x, y: y, forceAction: forceAction);
            fillObjectWithLiquid(target: target, markActionFinished: true);
        }

        public void FillObjectWithLiquid(string objectId, bool forceAction = false) {
            SimObjPhysics target = getTargetObject(objectId: objectId, forceAction: forceAction);
            fillObjectWithLiquid(target: target, markActionFinished: true);
        }

        ///////////////////////////////////////////
        //////// EMPTY LIQUID FROM OBJECT /////////
        ///////////////////////////////////////////

        protected void emptyLiquidFromObject(SimObjPhysics target, bool markActionFinished) {
            if (target == null) {
                throw new ArgumentNullException();
            }

            Fill fillComponent = target.GetComponent<Fill>();
            if (fillComponent == null) {
                throw new InvalidOperationException($"{target.transform.name} does not have CanBeFilled property!");
            }

            fillComponent.EmptyObject();

            if (markActionFinished) {
                actionFinished(success: true);
            }
        }

        public void EmptyLiquidFromObject(float x, float y, bool forceAction = false) {
            SimObjPhysics target = getTargetObject(x: x, y: y, forceAction: forceAction);
            emptyLiquidFromObject(target: target, markActionFinished: true);
        }

        public void EmptyLiquidFromObject(string objectId, bool forceAction = false) {
            SimObjPhysics target = getTargetObject(objectId: objectId, forceAction: forceAction);
            emptyLiquidFromObject(target: target, markActionFinished: true);
        }

        ///////////////////////////////////////////
        ///////////// USE UP OBJECT ///////////////
        ///////////////////////////////////////////

        // use up the contents of this object (toilet paper, paper towel, tissue box, etc).
        protected void useObjectUp(SimObjPhysics target, bool markActionFinished) {
            if (target == null) {
                throw new ArgumentNullException();
            }

            UsedUp useUpComponent = target.GetComponent<UsedUp>();
            if (useUpComponent == null) {
                throw new InvalidOperationException($"{target.transform.name} does not have CanBeUsedUp property!");
            }

            if (!useUpComponent.isUsedUp) {
                useUpComponent.UseUp();
            }

            if (markActionFinished) {
                actionFinished(success: true);
            }
        }

        public void UseObjectUp(float x, float y, bool forceAction = false) {
            SimObjPhysics target = getTargetObject(x: x, y: y, forceAction: forceAction);
            useObjectUp(target: target, markActionFinished: true);
        }

        public void UseObjectUp(string objectId, bool forceAction = false) {
            SimObjPhysics target = getTargetObject(objectId: objectId, forceAction: forceAction);
            useObjectUp(target: target, markActionFinished: true);
        }

        ///////////////////////////////////////////
        ///////////// TOGGLE OBJECT ///////////////
        ///////////////////////////////////////////

        protected void toggleObject(SimObjPhysics target, bool toggleOn, bool markActionFinished) {
            if (target == null) {
                throw new ArgumentNullException();
            }

            CanToggleOnOff toggleComponent = target.GetComponent<CanToggleOnOff>();
            if (toggleComponent == null) {
                throw new InvalidOperationException($"{target.objectId} is not toggleable!");
            }
            if (!toggleComponent.ReturnSelfControlled()) {
                throw new InvalidOperationException("target object is controlled by another sim object. target object cannot be turned on/off directly");
            }

            // already in this state.
            if (toggleComponent.isOn == toggleOn) {
                if (markActionFinished) {
                    actionFinished(success: true);
                }
                return;
            }

            // Matt TODO: I don't think this is necessary anymore, since Open is not treated as a toggle..:
            // check if this object needs to be closed in order to turn on
            // if (toggleOn && toggleComponent.ReturnMustBeClosedToTurnOn().Contains(target.Type) && target.GetComponent<CanOpen_Object>().isOpen) {
            //     throw new InvalidOperationException("Target must be closed to Toggle On!");
            // }

            // check if this object is broken, it should not be able to be turned on
            if (toggleOn && target.hasSecondaryProperty(SimObjSecondaryProperty.CanBreak) && target.IsBroken) {
                throw new InvalidOperationException("Target is broken and cannot be Toggled On!");
            }

            IEnumerator ToggleAndWait() {
                toggleComponent.Toggle();
                yield return new WaitUntil(() => toggleComponent.GetiTweenCount() == 0);
                if (markActionFinished) {
                    actionFinished(success: true);
                }
            }
            StartCoroutine(ToggleAndWait());
        }

        public void ToggleObjectOn(string objectId, bool forceAction = false) {
            SimObjPhysics target = getTargetObject(objectId: objectId, forceAction: forceAction);
            toggleObject(target: target, toggleOn: true, markActionFinished: true);
        }

        public void ToggleObjectOff(string objectId, bool forceAction = false) {
            SimObjPhysics target = getTargetObject(objectId: objectId, forceAction: forceAction);
            toggleObject(target: target, toggleOn: false, markActionFinished: true);
        }

        public void ToggleObjectOn(float x, float y, bool forceAction = false) {
            SimObjPhysics target = getTargetObject(x: x, y: y, forceAction: forceAction);
            toggleObject(target: target, toggleOn: true, markActionFinished: true);
        }

        public void ToggleObjectOff(float x, float y, bool forceAction = false) {
            SimObjPhysics target = getTargetObject(x: x, y: y, forceAction: forceAction);
            toggleObject(target: target, toggleOn: false, markActionFinished: true);
        }

        ///////////////////////////////////////////
        ////////////// OPEN OBJECT ////////////////
        ///////////////////////////////////////////

        // protected helper used with OpenObject commands
        protected void openObject(
            SimObjPhysics target,
            float openness,
            bool markActionFinished,
            bool simplifyPhysics = false,
        ) {
            if (target == null) {
                throw new ArgumentNullException();
            }

            if (openness > 1 || openness < 0) {
                throw new InvalidOperationException("Openness must be in [0:1]");
            }

            CanOpen_Object openComponent = target.GetComponent<CanOpen_Object>();
            if (openComponent == null) {
                throw new InvalidOperationException($"{target.ObjectID} is not an Openable object");
            }

            // Matt TODO: I don't think this is necessary anymore, since open is not treated as a toggle.
            // This is a style choice that applies to Microwaves and Laptops,
            // where it doesn't make a ton of sense to open them, while they are in use.
            // if (openComponent.WhatReceptaclesMustBeOffToOpen().Contains(target.Type) && target.GetComponent<CanToggleOnOff>().isOn) {
                // throw new InvalidOperationException("Target must be toggled OFF to open!");
            // }

            IEnumerator openAnimation() {
                // disables all colliders in the scene
                List<Collider> collidersDisabled = new List<Collider>();
                foreach (Collider c in GetComponentsInChildren<Collider>()) {
                    if (c.enabled) {
                        collidersDisabled.Add(c);
                        c.enabled = false;
                    }
                }

                // stores the object id of each object within this openComponent
                Dictionary<string, Transform> objectIdToOldParent = null;

                // freeze contained objects
                if (simplifyPhysics) {
                    SimObjPhysics target = ancestorSimObjPhysics(openComponent.gameObject);
                    objectIdToOldParent = new Dictionary<string, Transform>();

                    foreach (string objectId in target.GetAllSimObjectsInReceptacleTriggersByObjectID()) {
                        SimObjPhysics toReParent = physicsSceneManager.ObjectIdToSimObjPhysics[objectId];
                        objectIdToOldParent[objectId] = toReParent.transform.parent;
                        toReParent.transform.parent = openComponent.transform;
                        toReParent.GetComponent<Rigidbody>().isKinematic = true;
                    }
                }

                // just incase there's a failure, we can undo it
                float startOpenness = openComponent.currentOpenness;

                // open the object to openness
                openComponent.Interact(openness);
                yield return new WaitUntil(() => (openComponent.GetiTweenCount() == 0));
                yield return null;

                GameObject openableGameObj = openComponent.GetComponentInParent<SimObjPhysics>().gameObject;

                // check for collision failure
                bool failed = isAgentCapsuleCollidingWith(openableGameObj) || isHandObjectCollidingWith(openableGameObj);
                if (failed) {
                    // failure: reset the openness!
                    openComponent.Interact(openness: startOpenness);
                    yield return new WaitUntil(() => (openComponent.GetiTweenCount() == 0));
                    yield return null;
                }

                // re-enables all previously disabled colliders
                foreach (Collider c in collidersDisabled) {
                    c.enabled = true;
                }

                // stops any object located within this openComponent from moving
                // unfreeze contained objects
                if (simplifyPhysics) {
                    foreach (string objectId in objectIdToOldParent.Keys) {
                        SimObjPhysics toReParent = physicsSceneManager.ObjectIdToSimObjPhysics[objectId];
                        toReParent.transform.parent = objectIdToOldParent[toReParent.ObjectID];
                        Rigidbody rb = toReParent.GetComponent<Rigidbody>();
                        rb.velocity = new Vector3(0f, 0f, 0f);
                        rb.angularVelocity = new Vector3(0f, 0f, 0f);
                        rb.isKinematic = false;
                    }
                }

                if (failed) {
                    throw new InvalidOperationException("Object failed to open/close successfully.");
                }
                if (markActionFinished) {
                    actionFinished(true);
                }
            }
            StartCoroutine(openAnimation());
        }

        public void OpenObject(string objectId, float openness = 1, bool forceAction = false) {
            SimObjPhysics target = getTargetObject(objectId: objectId, forceAction: forceAction);
            openObject(target: target, openness: openness, markActionFinished: true);
        }

        public void OpenObject(float x, float y, float openness = 1, bool forceAction = false) {
            SimObjPhysics target = getTargetObject(x: x, y: y, forceAction: forceAction);
            openObject(target: target, openness: openness, markActionFinished: true);
        }

        [ObsoleteAttribute(message: "This action is deprecated. Call OpenObject(openness) instead.", error: false)]
        public void OpenObject(string objectId, bool forceAction = false, float? moveMagnitude = null) {
            // backwards compatibility support
            // Previously, when moveMagnitude==0, that meant full openness, since the default float was 0.
            float openness = 1;
            if (moveMagnitude != null) {
                openness = ((float) moveMagnitude) == 0 ? 1 : (float) moveMagnitude;
            }
            OpenObject(objectId: objectId, openness: openness, forceAction: forceAction);
        }

        [ObsoleteAttribute(message: "This action is deprecated. Call OpenObject(openness) instead.", error: false)]
        public void OpenObject(float x, float y, bool forceAction = false, float? moveMagnitude = null) {
            // backwards compatibility support
            // Previously, when moveMagnitude==0, that meant full openness, since the default float was 0.
            float openness = 1;
            if (moveMagnitude != null) {
                openness = ((float) moveMagnitude) == 0 ? 1 : (float) moveMagnitude;
            }
            OpenObject(x: x, y: y, openness: openness, forceAction: forceAction);
        }

        ///////////////////////////////////////////
        ///////////// CLOSE OBJECT ////////////////
        ///////////////////////////////////////////

        // syntactic sugar for open object with openness = 0.
        public void CloseObject(string objectId, bool forceAction = false) {
            OpenObject(objectId: objectId, forceAction: forceAction, openness: 0);
        }

        // syntactic sugar for open object with openness = 0.
        public void CloseObject(float x, float y, bool forceAction = false) {
            OpenObject(x: x, y: y, forceAction: forceAction, openness: 0);
        }

        public void GetScenesInBuild() {
            int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
            string[] scenes = new string[sceneCount];
            for (int i = 0; i < sceneCount; i++) {
                scenes[i] = System.IO.Path.GetFileNameWithoutExtension(
                    UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i)
                );
            }
            actionFinished(success: true, actionReturn: scenes);
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

            int layerMask = 1 << 8;
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
                                        yOffsets.Add((float) Math.Round(hit.distance - b.extents.y - 0.005f, 3));
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
