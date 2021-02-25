using System;
using System.Collections;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
using System.Linq;
using System.Collections.Generic;

public class MCSController : PhysicsRemoteFPSAgentController {
    public static float STANDING_POSITION_Y = 0.4625f;
    public static float CRAWLING_POSITION_Y = STANDING_POSITION_Y/2;
    public static float LYING_POSITION_Y = 0.1f;
    public static float MOVE_MAX = 0.1f;

    public static float DISTANCE_HELD_OBJECT_Y = 0.1f;
    public static float DISTANCE_HELD_OBJECT_Z = 0.3f;

    // TODO MCS-95 Make the room size configurable in the scene configuration file.
    // The room dimensions are always 10x10 so the distance from corner to corner is around 14.15
    public static float MAX_DISTANCE_ACROSS_ROOM = 14.15f;

    // The number of times to run Physics.Simulate after each action from the player is LOOPS * STEPS.
    public static int PHYSICS_SIMULATION_LOOPS = 1;
    public static int PHYSICS_SIMULATION_STEPS = 5;

    public static int ROTATION_DEGREES = 10;

    //this is not the capsule radius, this is the radius of the x and z bounds of the agent.
    public static float AGENT_RADIUS = 0.12f;

    public int substeps = MCSController.PHYSICS_SIMULATION_LOOPS;

    public int step = 0;

    protected int minHorizon = -90;
    protected int maxHorizon = 90;
    protected float minRotation = -360f;
    protected float maxRotation = 360f;

    public GameObject fpsAgent;

    public enum PlayerPose {
        STANDING,
        CRAWLING,
        LYING
    }

    PlayerPose pose = PlayerPose.STANDING;

    private int cameraCullingMask = -1;

    private bool movementActionFinished = false;
    private MCSMovementActionData movementActionData; //stores movement direction
    private bool inputWasMovement = false;

    private MCSRotationData bodyRotationActionData; //stores body rotation direction
    private MCSRotationData lookRotationActionData; //stores look rotation direction
    private bool inputWasRotateLook = false;

    private int actionFrameCount;

    public override void CloseObject(ServerAction action) {
        bool continueAction = TryConvertingEachScreenPointToId(action);

        if (!continueAction) {
            return;
        }

        base.CloseObject(action);
    }

    private string ConvertScreenPointToId(Vector3 screenPoint, string previousObjectId) {
        // If the objectId was set or the screen point vector was not set, return the previous objectId.
        if ((previousObjectId != null && !previousObjectId.Equals("")) ||
            (screenPoint.x <= 0 && screenPoint.y <= 0)) {
            return previousObjectId;
        }

        int layerMask = (1 << 8); // Only look at objects on the SimObjVisible layer.
        Ray screenPointRay = m_Camera.ScreenPointToRay(screenPoint);
        List<RaycastHit> hits = Physics.RaycastAll(screenPointRay.origin, screenPointRay.direction,
            MCSController.MAX_DISTANCE_ACROSS_ROOM, layerMask).ToList();
        if (hits.Count == 0) {
            this.errorMessage = "Cannot find any object on the screen point vector.";
            this.lastActionStatus = Enum.GetName(typeof(ActionStatus), ActionStatus.NOT_OBJECT);
            this.actionFinished(false);
            return previousObjectId;
        } else {
            hits.Sort(delegate (RaycastHit one, RaycastHit two) {
                return one.distance.CompareTo(two.distance);
            });
            SimObjPhysics simObjPhysics = hits.First().transform.gameObject
                .GetComponentInParent<SimObjPhysics>();
            if (simObjPhysics == null) {
                this.errorMessage = "The closest object on the screen point is not interactable.";
                this.lastActionStatus = Enum.GetName(typeof(ActionStatus), ActionStatus.NOT_INTERACTABLE);
                this.actionFinished(false);
                return previousObjectId;
            } else {
                return simObjPhysics.ObjectID;
            }
        }
    }

    public override bool DropHandObject(ServerAction action) {
        // Use held object instead of direction for objectId (if needed)
        bool continueAction = TryObjectIdFromHeldObject(action);

        if (!continueAction) {
            return false;
        }

        SimObjPhysics target = null;

        if (physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(action.objectId)) {
            target = physicsSceneManager.ObjectIdToSimObjPhysics[action.objectId];
        }

        // Reactivate the object BEFORE trying to drop it so that we can see if it's obstructed.
        // TODO MCS-77 This object will always be active, so we won't need to reactivate this object.
        if (target != null && target.transform.parent == this.AgentHand.transform) {
            target.gameObject.SetActive(true);
        }

        bool status = base.DropHandObject(action);

        // Deactivate the object again if the drop failed.
        // TODO MCS-77 We should never need to deactivate this object again (see PickupObject).
        if (target != null && target.transform.parent == this.AgentHand.transform) {
            target.gameObject.SetActive(false);
        }

        return status;
    }

    public void EndHabituation(ServerAction action) {
        this.GetComponentInChildren<Camera>().cullingMask = 0;
        foreach (Transform child in GameObject.Find("Objects").transform) {
            child.gameObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
        }
        this.lastActionStatus = Enum.GetName(typeof(ActionStatus), ActionStatus.SUCCESSFUL);
        this.actionFinished(false);
    }

    public override ObjectMetadata[] generateObjectMetadata() {
        // TODO MCS-77 The held objects will always be active, so we won't need to reactivate this object again.
        bool deactivate = false;
        if (this.ItemInHand != null && !this.ItemInHand.activeSelf) {
            deactivate = true;
            this.ItemInHand.SetActive(true);
        }

        List<string> visibleObjectIds = this.GetAllVisibleSimObjPhysics(this.m_Camera,
            MCSController.MAX_DISTANCE_ACROSS_ROOM).Select((obj) => obj.ObjectID).ToList();

        ObjectMetadata[] objectMetadata = base.generateObjectMetadata().ToList().Select((metadata) => {
            // The "visible" property in the ObjectMetadata really describes if the object is within reach.
            // We also want to know if we can currently see the object in our camera view.
            // Additionally, verify its renderer is enabled (it may be shrouded; see MCSMain).
            // Note that interactable child objects like shelves, drawers, and cabinents may not have their own Renderer.
            Renderer renderer = GameObject.Find(metadata.name).GetComponentInChildren<Renderer>();
            metadata.visibleInCamera = visibleObjectIds.Contains(metadata.objectId) && renderer != null && renderer.enabled;
            return metadata;
        }).ToArray();

        // TODO MCS-77 The held objects will always be active, so we shouldn't deactivate this object again.
        if (deactivate) {
            this.ItemInHand.SetActive(false);
        }

        return objectMetadata;
    }

    public override MetadataWrapper generateMetadataWrapper() {
        MetadataWrapper metadata = base.generateMetadataWrapper();
        metadata.lastActionStatus = this.lastActionStatus;
        metadata.reachDistance = this.maxVisibleDistance;
        metadata.clippingPlaneFar = this.m_Camera.farClipPlane;
        metadata.clippingPlaneNear = this.m_Camera.nearClipPlane;
        metadata.pose = this.pose.ToString();
        metadata.structuralObjects = metadata.objects.ToList().Where(objectMetadata => {
            GameObject gameObject = GameObject.Find(objectMetadata.name);
            // The object may be null if it is being held.
            return gameObject != null && gameObject.GetComponent<StructureObject>() != null;
        }).Select(objectMetadata => {
            // Performance optimization: Just say that all of the structural objects are visible all of the time so we
            // don't have to add a lot of visibility points to each of them that will cause excessive raycasting.
            objectMetadata.visible = true;
            objectMetadata.visibleInCamera = true;
            return objectMetadata;
        }).ToArray();
        metadata.objects = metadata.objects.ToList().Where(objectMetadata => {
            GameObject gameObject = GameObject.Find(objectMetadata.name);
            // The object may be null if it is being held.
            return gameObject == null || gameObject.GetComponent<StructureObject>() == null;
        }).ToArray();
        return this.agentManager.UpdateMetadataColors(this, metadata);
    }

    /**
     * For actions where there is an object in the agent's hand, check the held object
     * for an object ID if one isn't given.
     *
     * Note: This may need to change later when the held object is visible
     * or the agent has two hands to use.
     */
    private string GetHeldObjectId(string previousObjectId) {
        if ((previousObjectId != null) && (!previousObjectId.Equals(""))) {
            return previousObjectId;
        } else {
            if (ItemInHand != null) {
                return ItemInHand.GetComponent<SimObjPhysics>().objectID;
            } else {
                errorMessage = "No object found in hand.";
                Debug.Log(errorMessage);
                this.lastActionStatus = Enum.GetName(typeof(ActionStatus), ActionStatus.NOT_HELD);
                actionFinished(false);
                return previousObjectId;
            }
        }
    }

    public override void Initialize(ServerAction action) {
        // Set consistentColors to randomize segmentation mask colors if required
        this.agentManager.consistentColors = action.consistentColors;
        base.Initialize(action);

        this.step = 0;
        MCSMain main = GameObject.Find("MCS").GetComponent<MCSMain>();
        main.enableVerboseLog = main.enableVerboseLog || action.logs;
        // Reset the MCS scene configuration data and player.
        main.ChangeCurrentScene(action.sceneConfig);
    }

    public void MCSCloseObject(ServerAction action) {
        // The AI2-THOR Python library has buggy error checking specifically for the CloseObject function,
        // so create our own function and call it from the Python API.
        this.CloseObject(action);
    }

    public void MCSOpenObject(ServerAction action) {
        // The AI2-THOR Python library has buggy error checking specifically for the OpenObject function,
        // so create our own function and call it from the Python API.
        this.OpenObject(action);
    }

    public override ObjectMetadata ObjectMetadataFromSimObjPhysics(SimObjPhysics simObj, bool isVisible) {
        ObjectMetadata objectMetadata = base.ObjectMetadataFromSimObjPhysics(simObj, isVisible);

        objectMetadata = this.UpdatePositionDistanceAndDirectionInObjectMetadata(simObj.gameObject, objectMetadata);

        Transform recBox = simObj.BoundingBox == null && simObj.transform.parent != null &&
            simObj.transform.parent.name != "Objects" ? simObj.transform.Find("ReceptacleTriggerBox") : null;
        if ((objectMetadata.objectBounds == null && (simObj.BoundingBox != null || recBox != null))) {
            objectMetadata.objectBounds = this.WorldCoordinatesOfBoundingBox(simObj);
        }
        if (objectMetadata.objectBounds != null) {
            MCSMain main = GameObject.Find("MCS").GetComponent<MCSMain>();
            if (main != null && main.enableVerboseLog) {
                Debug.Log("MCS: " + objectMetadata.objectId + " CENTER = " +
                    (recBox != null ? recBox : simObj.BoundingBox.transform).position.ToString("F4"));
                Debug.Log("MCS: " + objectMetadata.objectId + " BOUNDS = " + String.Join(", ",
                    objectMetadata.objectBounds.objectBoundsCorners.Select(point => point.ToString("F4")).ToArray()));
            }
        }

        HashSet<string> colors = new HashSet<string>();
        simObj.gameObject.GetComponentsInChildren<Renderer>().ToList().ForEach((renderer) => {
            renderer.materials.ToList().ForEach((material) => {
                // Object material names sometimes end with " (Instance)" during runtime though I'm not sure why.
                string materialName = material.name.Replace(" (Instance)", "");
                if (MCSConfig.MATERIAL_COLORS.ContainsKey(materialName)) {
                    MCSConfig.MATERIAL_COLORS[materialName].ToList().ForEach((color) => {
                        colors.Add(color);
                    });
                }
            });
        });
        objectMetadata.colorsFromMaterials = colors.ToArray();

        objectMetadata.shape = simObj.shape;

        return objectMetadata;
    }

    public override void OpenObject(ServerAction action) {
        bool continueAction = TryConvertingEachScreenPointToId(action);

        if (!continueAction) {
            return;
        }

        base.OpenObject(action);
    }

    public override void PickupObject(ServerAction action) {
        bool continueAction = TryConvertingEachScreenPointToId(action);

        if (!continueAction) {
            return;
        }

        SimObjPhysics target = null;
        SimObjPhysics containerObject = null;

        if (physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(action.objectId)) {
            target = physicsSceneManager.ObjectIdToSimObjPhysics[action.objectId];

            // Update our hand's position so that the object we want to hold doesn't clip our body.
            // TODO MCS-77 We may want to change how this function is used.
            this.UpdateHandPositionToHoldObject(target);

            // Check if the object to be picked up is currently inside a receptacle.
            // If so, we'll need to manually update that receptacle's list of contained objects since the
            // previous onTriggerExit method that handled this will not get called once the
            // picked up object is deactivated.
            // TODO MCS-77 Remove when object is no longer deactivated when picked up
            int layerMask = 1 << 8;

            List<RaycastHit> hits = Physics.RaycastAll(target.transform.position, Vector3.down,
                1f, layerMask).ToList();
            if (hits.Count > 0) {
                hits.Sort(delegate (RaycastHit one, RaycastHit two) {
                    return one.distance.CompareTo(two.distance);
                });
                SimObjPhysics firstObject = hits.First().transform.gameObject
                    .GetComponentInParent<SimObjPhysics>();

                if(firstObject != null && firstObject.IsReceptacle && firstObject.SimObjectsContainedByReceptacle.Contains(target)) {
                    containerObject = firstObject;
                }
            }
        }


        base.PickupObject(action);

        // TODO MCS-77 Find a way to handle held object collisions so we don't have to deactivate this object
        // and update CurrentlyContains list.
        if (target != null && target.transform.parent == this.AgentHand.transform) {
            target.gameObject.SetActive(false);

            if(containerObject != null) {
                foreach(GameObject rtb in containerObject.ReceptacleTriggerBoxes) {
                    Contains containsScript = rtb.GetComponent<Contains>();
                    containsScript.RemoveFromCurrentlyContains(target);
                }
            }
        }
    }

    public override void ProcessControlCommand(ServerAction controlCommand) {
        if (this.cameraCullingMask < 0) {
            this.cameraCullingMask = this.GetComponentInChildren<Camera>().cullingMask;
        }
        this.GetComponentInChildren<Camera>().cullingMask = this.cameraCullingMask;

        this.inputWasMovement =
                controlCommand.action.Equals("MoveAhead") ||
                controlCommand.action.Equals("MoveBack") ||
                controlCommand.action.Equals("MoveLeft") ||
                controlCommand.action.Equals("MoveRight");
        this.inputWasRotateLook =
                controlCommand.action.Equals("RotateLook") ||
                controlCommand.action.Equals("RotateLeft") ||
                controlCommand.action.Equals("RotateRight") ||
                controlCommand.action.Equals("LookUp") ||
                controlCommand.action.Equals("LookDown");

        this.actionFrameCount = 0; //for movement and rotation

        // Never let the placeable objects ignore the physics simulation (they should always be affected by it).
        controlCommand.placeStationary = false;

        Debug.Log("MCS: Action = " + controlCommand.action);

        base.ProcessControlCommand(controlCommand);

        // Clear the saved images from the previous step.
        ((MCSPerformerManager)this.agentManager).ClearSavedImages();

        if (!controlCommand.action.Equals("Initialize")) {
            this.step++;
        }
    }

    public override void PullObject(ServerAction action) {
        bool continueAction = TryConvertingEachScreenPointToId(action);

        if (!continueAction) {
            return;
        }

        if (physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(action.objectId) &&
            ItemInHand != null && action.objectId == ItemInHand.GetComponent<SimObjPhysics>().objectID) {
            Debug.Log("Cannot pull. Object " + action.objectId + " is in agent's hand. Calling ThrowObject instead.");
            ThrowObject(action);
        } else {
            base.PullObject(action);
        }

    }

    public override void PushObject(ServerAction action) {
        bool continueAction = TryConvertingEachScreenPointToId(action);

        if (!continueAction) {
            return;
        }

        if (physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(action.objectId) &&
            ItemInHand != null && action.objectId == ItemInHand.GetComponent<SimObjPhysics>().objectID) {
            Debug.Log("Cannot push. Object " + action.objectId + " is in agent's hand. Calling ThrowObject instead.");
            ThrowObject(action);
        } else {
            base.PushObject(action);
        }
    }

    public override void PutObject(ServerAction action) {
        // Use held object instead of direction for objectId (if needed)
        bool continueAction = TryObjectIdFromHeldObject(action);

        if (!continueAction) {
            return;
        }

        if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(action.objectId)) {
            errorMessage = "Object ID appears to be invalid.";
            Debug.Log(errorMessage);
            this.lastActionStatus = Enum.GetName(typeof(ActionStatus), ActionStatus.NOT_OBJECT);
            actionFinished(false);
            return;
        }

        if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(action.receptacleObjectId)) {
            errorMessage = "Receptacle Object ID appears to be invalid.";
            Debug.Log(errorMessage);
            this.lastActionStatus = Enum.GetName(typeof(ActionStatus), ActionStatus.NOT_OBJECT);
            actionFinished(false);
            return;
        }

        SimObjPhysics target = physicsSceneManager.ObjectIdToSimObjPhysics[action.objectId];

        // Reactivate the object BEFORE trying to place it so that we can see if it's obstructed.
        // TODO MCS-77 This object will always be active, so we won't need to reactivate this object.
        if (target && target.transform.parent == this.AgentHand.transform) {
            target.gameObject.SetActive(true);
        }

        base.PutObject(action);

        // Deactivate the object again if the placement failed.
        // TODO MCS-77 We should never need to deactivate this object (see PickupObject).
        if (target.transform.parent == this.AgentHand.transform) {
            target.gameObject.SetActive(false);
        }
    }

    // [REVIEW] This is worth noting that in 2.5 this function does not take a serveraction
    // I am keeping this here in case there is any reflection code passing in as it does
    // not seem to be referenced directly in code.
    public void ResetAgentHandPosition(ServerAction action) {
        // Don't reset the player's hand position if the player is just moving or rotating.
        // Use this.lastAction here because this function's ServerAction argument is sometimes null.
        if (this.lastAction.StartsWith("Move") || this.lastAction.StartsWith("Rotate") ||
            this.lastAction.StartsWith("Look") || this.lastAction.StartsWith("Teleport")) {
            return;
        }
        base.ResetAgentHandPosition();
    }

    // [REVIEW] This is worth noting that in 2.5 this function does not take a serveraction
    // I am keeping this here in case there is any reflection code passing in as it does
    // not seem to be referenced directly in code.
    public void ResetAgentHandRotation(ServerAction action) {
        // Don't reset the player's hand rotation if the player is just moving or rotating.
        // Use this.lastAction here because this function's ServerAction argument is sometimes null.
        if (this.lastAction.StartsWith("Move") || this.lastAction.StartsWith("Rotate") ||
            this.lastAction.StartsWith("Look") || this.lastAction.StartsWith("Teleport")) {
            return;
        }
        base.ResetAgentHandRotation();
    }

    public override void RotateLook(ServerAction response)
    {
        // Need to calculate current rotation/horizon and increment by inputs given
        float currentHorizonValue = m_Camera.transform.localEulerAngles.x;
        // The horizon should always be either between 0 and 90 (looking down) or 270 and 360 (looking up).
        // If looking up, we must change the horizon from [360, 270] to [0, -90].
        currentHorizonValue = (currentHorizonValue >= 270 ? (currentHorizonValue - 360) : currentHorizonValue);

        // Limiting where to look based on realistic expectation (for instance, a person can't turn
        // their head 180 degrees)
        bool reset = false;
        if ((Mathf.Round(currentHorizonValue) + response.horizon > maxHorizon) ||
            (Mathf.Round(currentHorizonValue) + response.horizon < minHorizon))
        {
            Debug.Log("Value of horizon needs to be between " + minHorizon + " and " + maxHorizon +
                ". Setting value to 0.");
            reset = true;
        }

        this.bodyRotationActionData = //left right
            new MCSRotationData(transform.rotation, Quaternion.Euler(new Vector3(0.0f, response.rotation.y, 0.0f)));
        this.lookRotationActionData = !reset ? //if not reseting then free look up down
            new MCSRotationData(m_Camera.transform.rotation, Quaternion.Euler(new Vector3(response.horizon, 0.0f, 0.0f))) :
            new MCSRotationData(m_Camera.transform.rotation, Quaternion.Euler(Vector3.zero));

        this.lastActionStatus = Enum.GetName(typeof(ActionStatus), reset ? ActionStatus.CANNOT_ROTATE : ActionStatus.SUCCESSFUL);
    }

    public void SimulatePhysics() {
        if (this.agentManager.renderImage) {
            // We only need to save ONE image of the scene after initialization.
            StartCoroutine(this.SimulatePhysicsSaveImagesIncreaseStep(this.step == 0 ? 1 : this.substeps));
        }

        else {
            // (Also simulate the physics after initialization so that the objects can settle down onto the floor.)
            this.SimulatePhysicsCompletely();
            GameObject.Find("MCS").GetComponent<MCSMain>().UpdateOnPhysicsSubstep(1);
            // Notify the AgentManager to send the action output metadata and images to the Python API.
            ((MCSPerformerManager)this.agentManager).FinalizeEmit();
        }
    }

    private void SimulatePhysicsCompletely() {
        for (int i = 0; i < this.substeps; ++i) {
            this.SimulatePhysicsOnce();
        }
    }

    private void SimulatePhysicsOnce() {
        //for movement
        if (this.inputWasMovement) {
            MatchAgentHeightToStructureBelow(false);
            this.movementActionFinished = moveInDirection((this.movementActionData.direction / this.substeps),
                    this.movementActionData.UniqueID,
                    this.movementActionData.maxDistanceToObject,
                    this.movementActionData.forceAction);
            this.actionFrameCount++;
            if (this.actionFrameCount == this.substeps) {
                actionFinished(this.movementActionFinished);
            }
        } //for rotation
        else if (this.inputWasRotateLook) {
            RotateLookAcrossFrames(this.lookRotationActionData);
            RotateLookBodyAcrossFrames(this.bodyRotationActionData);
            this.actionFrameCount++;
            if (this.actionFrameCount == this.substeps) {
                actionFinished(true);
            }
        }
        // Call Physics.Simulate multiple times with a small step value because a large step
        // value causes collision errors.  From the Unity Physics.Simulate documentation:
        // "Using step values greater than 0.03 is likely to produce inaccurate results."
        for (int i = 0; i < MCSController.PHYSICS_SIMULATION_STEPS; ++i) {
            Physics.Simulate(0.01f);
        }
    }

    private IEnumerator SimulatePhysicsSaveImagesIncreaseStep(int thisLoop) {
        // Run the physics simulation for a little bit, then pause and save the images for the current scene.
        this.SimulatePhysicsOnce();

        GameObject.Find("MCS").GetComponent<MCSMain>().UpdateOnPhysicsSubstep(this.substeps);

        // Wait for the end of frame after we run the physics simulation but before we save the images.
        yield return new WaitForEndOfFrame(); // Required for coroutine functions

        ((MCSPerformerManager)this.agentManager).SaveImages(this.imageSynthesis);

        int nextLoop = thisLoop - 1;

        if (nextLoop > 0) {
            // Continue the next loop: run the physics simulation, then save more images.
            StartCoroutine(this.SimulatePhysicsSaveImagesIncreaseStep(nextLoop));
        }
        else {
            // Once finished, notify the AgentManager to send the action output metadata and images to the Python API.
            ((MCSPerformerManager)this.agentManager).FinalizeEmit();
        }
    }

    public override void ThrowObject(ServerAction action) {
        // Use held object instead of direction for objectId (if needed)
        bool continueAction = TryObjectIdFromHeldObject(action);

        if (!continueAction) {
            return;
        }

        if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(action.objectId)) {
            errorMessage = "Object ID appears to be invalid.";
            Debug.Log(errorMessage);
            this.lastActionStatus = Enum.GetName(typeof(ActionStatus), ActionStatus.NOT_OBJECT);
            actionFinished(false);
            return;
        }

        SimObjPhysics target = physicsSceneManager.ObjectIdToSimObjPhysics[action.objectId];

        // Reactivate the object BEFORE trying to throw it so that we can see if it's obstructed.
        // TODO MCS-77 This object will always be active, so we won't need to reactivate this object.
        if (target && target.transform.parent == this.AgentHand.transform) {
            target.gameObject.SetActive(true);
        }

        GameObject gameObj = ItemInHand;

        if(base.DropHandObject(action)) {
            if (action.objectImageCoords.x > 0 && action.objectImageCoords.y > 0) {
                // Need to calculate z for where to throw towards based on a raycast, since now
                // we are using screen points instead of directional vectors as input, which
                // will only give us (x,y).
                int layerMask = (1 << 8); // Only look at objects on the SimObjVisible layer.
                Ray screenPointRay = m_Camera.ScreenPointToRay(action.objectImageCoords);
                List<RaycastHit> hits = Physics.RaycastAll(screenPointRay.origin, screenPointRay.direction,
                    MCSController.MAX_DISTANCE_ACROSS_ROOM, layerMask).ToList();
                if (hits.Count == 0) {
                    this.errorMessage = "Cannot convert screen point to directional vector.";
                    Debug.Log(errorMessage);
                    this.lastActionStatus = Enum.GetName(typeof(ActionStatus), ActionStatus.FAILED);
                    this.actionFinished(false);
                } else {
                    hits.Sort(delegate (RaycastHit one, RaycastHit two) {
                        return one.distance.CompareTo(two.distance);
                    });

                    Vector3 directionToThrowTowards = hits.First().point;
                    gameObj.GetComponent<SimObjPhysics>().ApplyForce(directionToThrowTowards, action.moveMagnitude);
                }
            } else {
                // throw object forward if no direction input is given
                gameObj.GetComponent<SimObjPhysics>().ApplyRelativeForce(Vector3.forward, action.moveMagnitude);
            }
        }

        // Deactivate the object again if the throw failed.
        // TODO MCS-77 We should never need to deactivate this object (see PickupObject).
        if (target.transform.parent == this.AgentHand.transform) {
            target.gameObject.SetActive(false);
        }
    }

    public override void ToggleObject(ServerAction action, bool toggleOn, bool forceAction) {
        bool continueAction = TryConvertingEachScreenPointToId(action);

        if (!continueAction) {
            return;
        }

        base.ToggleObject(action, toggleOn, forceAction);
    }

    // Note that for screen points, (0,0) would be the bottom left of your
    // screen, and the top is the top right.
    private bool TryConvertingEachScreenPointToId(ServerAction action) {

        action.objectId = this.ConvertScreenPointToId(action.objectImageCoords,
            action.objectId);

        return TryReceptacleObjectIdFromScreenPoint(action);

    }

    private bool TryObjectIdFromHeldObject(ServerAction action) {
        // Can't currently use direction for objects in player's hand, since held objects are currently invisible
        action.objectId = this.GetHeldObjectId(action.objectId);

        // For receptacleObjectId (if needed), still using screen point
        return TryReceptacleObjectIdFromScreenPoint(action);
    }

    private bool TryReceptacleObjectIdFromScreenPoint(ServerAction action) {
        if (agentState != AgentState.ActionComplete) {
            action.receptacleObjectId = this.ConvertScreenPointToId(action.receptacleObjectImageCoords,
                action.receptacleObjectId);
        }
        // If we haven't yet called actionFinished then actionComplete will be false; continue the action.
        return agentState != AgentState.ActionComplete;
    }

    private ObjectMetadata UpdatePositionDistanceAndDirectionInObjectMetadata(GameObject gameObject, ObjectMetadata objectMetadata) {
        // From https://docs.unity3d.com/Manual/DirectionDistanceFromOneObjectToAnother.html
        objectMetadata.heading = objectMetadata.position - this.transform.position;
        objectMetadata.direction = (objectMetadata.heading / objectMetadata.heading.magnitude);

        // Calculate a distance with only the X and Z coordinates for our Python API.
        objectMetadata.distanceXZ = Vector3.Distance(new Vector3(this.transform.position.x, 0, this.transform.position.z),
            new Vector3(gameObject.transform.position.x, 0, gameObject.transform.position.z));

        return objectMetadata;
    }

    private void UpdateHandPositionToHoldObject(SimObjPhysics target) {
        float positionY = target.BoundingBox.transform.localPosition.y;
        float positionZ = target.BoundingBox.transform.localPosition.z;
        float sizeY = target.BoundingBox.transform.localScale.y;
        float sizeZ = target.BoundingBox.transform.localScale.z;

        BoxCollider boundingBoxCollider = target.BoundingBox.GetComponent<BoxCollider>();
        positionY = positionY + (boundingBoxCollider.center.y * target.BoundingBox.transform.localScale.y);
        positionZ = positionZ + (boundingBoxCollider.center.z * target.BoundingBox.transform.localScale.z);
        sizeY = sizeY * boundingBoxCollider.size.y;
        sizeZ = sizeZ * boundingBoxCollider.size.z;

        float multiplierY = (target.BoundingBox.transform.parent.localScale.y / this.transform.localScale.y);
        float multiplierZ = (target.BoundingBox.transform.parent.localScale.z / this.transform.localScale.z);
        positionY = positionY * multiplierY;
        positionZ = positionZ * multiplierZ;
        sizeY = sizeY * multiplierY;
        sizeZ = sizeZ * multiplierZ;

        float handY = -1 * ((sizeY / 2.0f) + positionY + MCSController.DISTANCE_HELD_OBJECT_Y);
        float handZ = ((sizeZ / 2.0f) - positionZ + MCSController.DISTANCE_HELD_OBJECT_Z);

        // Ensure that a tall held object is not positioned inside of the floor below.
        float minY = (sizeY / 2.0f) - positionY - (this.transform.position.y / this.transform.localScale.y) +
            MCSController.DISTANCE_HELD_OBJECT_Y;

        this.AgentHand.transform.localPosition = new Vector3(this.AgentHand.transform.localPosition.x, Math.Max(handY, minY), handZ);
    }

    public void Crawl(ServerAction action) {
        if (this.pose == PlayerPose.CRAWLING) {
                this.lastActionStatus = Enum.GetName(typeof(ActionStatus), ActionStatus.SUCCESSFUL);
                actionFinished(true);
                Debug.Log("Agent is already Crawling");
        } else {
            float startHeight = (this.pose == PlayerPose.LYING ? LYING_POSITION_Y : STANDING_POSITION_Y);
            startHeight += MatchAgentHeightToStructureBelow(true);
            Vector3 direction = (this.pose == PlayerPose.LYING ? Vector3.up : Vector3.down);
            CheckIfAgentCanCrawlLieOrStand(direction, startHeight, CRAWLING_POSITION_Y, PlayerPose.CRAWLING);
        }
    }

    public void LieDown(ServerAction action) {
        if (this.pose == PlayerPose.LYING) {
            this.lastActionStatus = Enum.GetName(typeof(ActionStatus), ActionStatus.SUCCESSFUL);
            actionFinished(true);
            Debug.Log("Agent is already Lying Down");
        } else {
            float startHeight = (this.pose == PlayerPose.CRAWLING ? CRAWLING_POSITION_Y : STANDING_POSITION_Y);
            startHeight += MatchAgentHeightToStructureBelow(true);
            CheckIfAgentCanCrawlLieOrStand(Vector3.down, startHeight, LYING_POSITION_Y, PlayerPose.LYING);
        }
    }

    public override void Stand(ServerAction action) {
        if (this.pose == PlayerPose.STANDING) {
            this.lastActionStatus = Enum.GetName(typeof(ActionStatus), ActionStatus.SUCCESSFUL);
            actionFinished(true);
            Debug.Log("Agent is already Standing");
        } else if (this.pose == PlayerPose.LYING) {
            this.lastActionStatus = Enum.GetName(typeof(ActionStatus), ActionStatus.WRONG_POSE);
            actionFinished(false);
            Debug.Log("Agent cannot Stand when lying down");
        } else {
            float startHeight = (this.pose == PlayerPose.CRAWLING ? CRAWLING_POSITION_Y : LYING_POSITION_Y);
            startHeight += MatchAgentHeightToStructureBelow(true);
            CheckIfAgentCanCrawlLieOrStand(Vector3.up, startHeight, STANDING_POSITION_Y, PlayerPose.STANDING);
        }
    }

    public void CheckIfAgentCanCrawlLieOrStand(Vector3 direction, float startHeight, float endHeight, PlayerPose pose) {
        //Raycast up or down the distance of shifting on y-axis
        Vector3 origin = new Vector3(transform.position.x, startHeight, transform.position.z);
        Vector3 end = new Vector3(transform.position.x, endHeight, transform.position.z);
        RaycastHit hit;
        LayerMask layerMask = ~(1 << 10);

        //if raycast hits an object, the agent does not move on y-axis
        if (Physics.SphereCast(origin, AGENT_RADIUS, direction, out hit, Mathf.Abs(startHeight-endHeight), layerMask) &&
            hit.collider.tag == "SimObjPhysics" &&
            hit.transform.GetComponent<StructureObject>() == null)
        {
            this.lastActionStatus = Enum.GetName(typeof(ActionStatus), ActionStatus.OBSTRUCTED);
            actionFinished(false);
            Debug.Log("Agent is Obstructed");
        } else {
            this.transform.position = new Vector3(this.transform.position.x, MatchAgentHeightToStructureBelow(true)+endHeight, this.transform.position.z);
            this.pose = pose;
            this.transform.position = new Vector3(this.transform.position.x, MatchAgentHeightToStructureBelow(true)+endHeight, this.transform.position.z);
            //SetUpRotationBoxChecks();
            this.lastActionStatus = Enum.GetName(typeof(ActionStatus), ActionStatus.SUCCESSFUL);
            actionFinished(true);
        }
    }


    //overrides from PhysicsRemoteFPSAgentController which enable agent/object collisions
    public override void MoveLeft(ServerAction action) {
        action.moveMagnitude = action.moveMagnitude > 0 ? action.moveMagnitude : gridSize;
        action.moveMagnitude = Mathf.Min(action.moveMagnitude, MCSController.MOVE_MAX);
        this.movementActionData = new MCSMovementActionData(-1 * transform.right * action.moveMagnitude,
            action.objectId,
            action.maxAgentsDistance, action.forceAction);
        this.inputDirection = "left";
        this.serverActionMoveMagnitude = action.moveMagnitude;
    }

    public override void MoveRight(ServerAction action) {
        action.moveMagnitude = action.moveMagnitude > 0 ? action.moveMagnitude : gridSize;
        action.moveMagnitude = Mathf.Min(action.moveMagnitude, MCSController.MOVE_MAX);
        this.movementActionData = new MCSMovementActionData(transform.right * action.moveMagnitude,
            action.objectId,
            action.maxAgentsDistance, action.forceAction);
        this.inputDirection = "right";
        this.serverActionMoveMagnitude = action.moveMagnitude;
    }

    public override void MoveAhead(ServerAction action) {
        action.moveMagnitude = action.moveMagnitude > 0 ? action.moveMagnitude : gridSize;
        action.moveMagnitude = Mathf.Min(action.moveMagnitude, MCSController.MOVE_MAX);
        this.movementActionData = new MCSMovementActionData(transform.forward * action.moveMagnitude,
            action.objectId,
            action.maxAgentsDistance, action.forceAction);
        this.inputDirection = "forward";
        this.serverActionMoveMagnitude = action.moveMagnitude;
    }

    public override void MoveBack(ServerAction action) {
        action.moveMagnitude = action.moveMagnitude > 0 ? action.moveMagnitude : gridSize;
        action.moveMagnitude = Mathf.Min(action.moveMagnitude, MCSController.MOVE_MAX);
        this.movementActionData = new MCSMovementActionData(-1 * transform.forward * action.moveMagnitude,
            action.objectId,
            action.maxAgentsDistance, action.forceAction);
        this.inputDirection = "back";
        this.serverActionMoveMagnitude = action.moveMagnitude;
    }

    private float MatchAgentHeightToStructureBelow(bool poseChange) {
        float heightDifference;
        heightDifference = pose == PlayerPose.STANDING ? STANDING_POSITION_Y :
            pose == PlayerPose.CRAWLING ? CRAWLING_POSITION_Y : LYING_POSITION_Y;

        //Raycast down
        Vector3 origin = new Vector3(transform.position.x, this.GetComponent<CapsuleCollider>().bounds.max.y, transform.position.z);
        RaycastHit hit;
        LayerMask layerMask = ~(1 << 10);

        //raycast to traverse structures at anything <= 45 degree angle incline
        if (Physics.Raycast(origin, Vector3.down, out hit, Mathf.Infinity, layerMask, QueryTriggerInteraction.Ignore) &&
            (hit.transform.GetComponent<StructureObject>()!=null)) {
            //for pose changes on structures only
            if (poseChange)
                return hit.point.y;
            else
            {
                Vector3 newHeight = new Vector3(transform.position.x, (hit.point.y + heightDifference), transform.position.z);
                this.transform.position = newHeight;
            }
        }
        //method needs a return value
        return 0;
    }

    public override void RotateLeft(ServerAction controlCommand) {
        ServerAction rotate = new ServerAction();
        rotate.rotation.y = -ROTATION_DEGREES;
        RotateLook(rotate);
    }

    public override void RotateRight(ServerAction controlCommand) {
        ServerAction rotate = new ServerAction();
        rotate.rotation.y = ROTATION_DEGREES;
        RotateLook(rotate);
    }

    public override void LookUp(ServerAction controlCommand)
    {
        ServerAction rotate = new ServerAction();
        rotate.horizon = -ROTATION_DEGREES;
        RotateLook(rotate);
    }

    public override void LookDown(ServerAction controlCommand)
    {
        ServerAction rotate = new ServerAction();
        rotate.horizon = ROTATION_DEGREES;
        RotateLook(rotate);
    }

    public void RotateLookAcrossFrames(MCSRotationData rotationActionData)
    {
        Quaternion currentAngle = rotationActionData.startingRotation;
        Quaternion distance = rotationActionData.endRotation;

        bool upReset = distance.eulerAngles.x + currentAngle.eulerAngles.x > maxHorizon;
        bool downReset = distance.eulerAngles.x + currentAngle.eulerAngles.x < minHorizon;

        float horizonChange = //this is because unity switches angles after 180 degrees. It sometimes switches to -180-0 or to 180-360.
            distance.eulerAngles.x > maxHorizon ? distance.eulerAngles.x - 360 :
            distance.eulerAngles.x < minHorizon ? distance.eulerAngles.x + 360 : distance.eulerAngles.x;

        Vector3 updatedRotation = new Vector3(horizonChange / this.substeps, 0, 0);
        m_Camera.transform.rotation = Quaternion.Euler(m_Camera.transform.rotation.eulerAngles + updatedRotation);
    }

    public void RotateLookBodyAcrossFrames(MCSRotationData rotationActionData)
    {
        Quaternion currentAngle = rotationActionData.startingRotation;
        Quaternion distance = rotationActionData.endRotation;

        float rotationChange =
            distance.eulerAngles.y > 90 ? distance.eulerAngles.y - 360 :
            distance.eulerAngles.y < -90 ? distance.eulerAngles.y + 360 : distance.eulerAngles.y;

        Vector3 updatedRotation = new Vector3(0, rotationChange / this.substeps, 0);
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + updatedRotation);
    }
}

/* class for contatining movement data */
public class MCSMovementActionData {
    public Vector3 direction;
    public string UniqueID;
    public float maxDistanceToObject;
    public bool forceAction;

    public MCSMovementActionData(Vector3 direction, string UniqueID, float maxDistanceToObject, bool forceAction) {
        this.direction = direction;
        this.UniqueID = UniqueID;
        this.maxDistanceToObject = maxDistanceToObject;
        this.forceAction = forceAction;
    }
}

/* class for contatining rotation data */
public class MCSRotationData {
    public Quaternion startingRotation;
    public Quaternion endRotation;

    public MCSRotationData(Quaternion startingRotation, Quaternion endRotation) {
        this.startingRotation = startingRotation;
        this.endRotation = endRotation;
    }
}
