using System;
using System.Collections;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

public class MCSController : PhysicsRemoteFPSAgentController {
    public const float PHYSICS_SIMULATION_STEP_SECONDS = 0.01f;

    public static float AGENT_STARTING_HEIGHT = 0.762f;
    public static float COLLIDER_HEIGHT = 1.23f;
    public static float COLLIDER_CENTER = -0.33f;
    public static float COLLIDER_RADIUS = 0.251f;

    //This is an extra collider that slightly clips into the ground to ensure collision with objects of any size.
    public CapsuleCollider groundObjectsCollider;
    public static float GROUND_OBJECTS_COLLIDER_RADIUS = 0.5f;


    public static float DISTANCE_HELD_OBJECT_Y = 0.1f;
    public static float DISTANCE_HELD_OBJECT_Z = 0.05f;

    // TODO MCS-95 Make the room size configurable in the scene configuration file.
    // The room dimensions are always 10x10 so the distance from corner to corner is around 14.15
    public static float MAX_DISTANCE_ACROSS_ROOM = 14.15f;

    // The number of times to run Physics.Simulate after each action from the player is LOOPS * STEPS.
    public static int PHYSICS_SIMULATION_STEPS = 5;

    public static int ROTATION_DEGREES = 10;

    //this is not the capsule radius, this is the radius of the x and z bounds of the agent.
    public static float AGENT_RADIUS = 0.12f;

    public int step = 0;
    public AsyncOperationHandle<SceneInstance> asyncOperationHandle;

    protected int minHorizon = -90;
    protected int maxHorizon = 90;
    protected float minRotation = -360f;
    protected float maxRotation = 360f;

    public GameObject fpsAgent;

    private int cameraCullingMask = -1;

    private enum InputAction {
        MOVEMENT,
        ROTATE,
        PASS,
        OTHER
    }

    private InputAction lastInputAction = InputAction.PASS;
    private bool movementActionFinished = false;
    private MCSMovementActionData movementActionData; //stores movement direction

    private MCSRotationData bodyRotationActionData; //stores body rotation direction
    private MCSRotationData lookRotationActionData; //stores look rotation direction

    private enum HapticFeedback {
        ON_LAVA,
    }
  
    private Dictionary<string, bool> hapticFeedback = new Dictionary<string, bool>();
    private int stepsOnLava;

    [SerializeField] private string resolvedObject;
    [SerializeField] private string resolvedReceptacle;
    public List<MCSSimulationAgent> simulationAgents = new List<MCSSimulationAgent>();
    public static int SIMULATION_AGENT_ANIMATION_FRAMES_PER_PHYSICS_STEPS = 1;
    public Dictionary<string, SimObjPhysics> agentObjectAssociations = new Dictionary<string, SimObjPhysics>();
    public bool targetIsVisibleAtStart = false;
    public GameObject retrievalTargetGameObject = null;
    private MCSMain mcsMain;


    public override void Awake() {
        mcsMain = FindObjectOfType<MCSMain>();
        base.Awake();
    }

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

        // We may need to add additional validation logic for teleport later.
        MCSTeleportFull(action);

        this.lastActionStatus = Enum.GetName(typeof(ActionStatus), ActionStatus.SUCCESSFUL);
        this.actionFinished(false);
    }

    private void MCSTeleportFull(ServerAction action) {
        if((!action.teleportPosition.HasValue) && (!action.teleportRotation.HasValue)) {
            return;
        }

        if(action.teleportPosition.HasValue) {
            // X/Z positions are passed in. Y position should always be standing height for now,
            // but this logic may need to change later if there's potential for the y position
            // to change (ramps, etc).
            targetTeleport = new Vector3(action.teleportPosition.Value.x, AGENT_STARTING_HEIGHT, action.teleportPosition.Value.z);
            transform.position = targetTeleport;
        }

        if(action.teleportRotation.HasValue) {
            transform.rotation = Quaternion.Euler(new Vector3(0.0f, action.teleportRotation.Value.y, 0.0f));

            // reset camera as well
            m_Camera.transform.localEulerAngles = new Vector3(0.0f, 0.0f, 0.0f);

        }
    }

///////////////////////////////
    public override ObjectMetadata[] generateObjectMetadata() {
        // TODO MCS-77 The held objects will always be active, so we won't need to reactivate this object again.
        bool deactivate = false;
        if (this.ItemInHand != null && !this.ItemInHand.activeSelf) {
            deactivate = true;
            this.ItemInHand.SetActive(true);
        }

        List<GameObject> heldAgentObjects = new List<GameObject>();
        foreach(MCSSimulationAgent agent in this.simulationAgents) {
            if (agent.isHoldingHeldObject && 
                (agent.rotatingToFacePerformer || (agent.simAgentActionState != MCSSimulationAgent.SimAgentActionState.InteractingHoldingHeldObject && 
                agent.simAgentActionState != MCSSimulationAgent.SimAgentActionState.HoldingOutHeldObject))) {
                
                agent.heldObject.gameObject.SetActive(true);
                heldAgentObjects.Add(agent.heldObject.gameObject);
            }
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
        
        foreach(GameObject heldAgentObject in heldAgentObjects) {
            heldAgentObject.SetActive(false);
        }

        return objectMetadata;
    }

    public override MetadataWrapper generateMetadataWrapper() {
        MetadataWrapper metadata = base.generateMetadataWrapper();
        metadata.lastActionStatus = this.lastActionStatus;
        metadata.performerReach = this.maxVisibleDistance;
        metadata.clippingPlaneFar = this.m_Camera.farClipPlane;
        metadata.clippingPlaneNear = this.m_Camera.nearClipPlane;
        metadata.performerRadius = this.GetComponent<CapsuleCollider>().radius;
        metadata.stepsOnLava = this.stepsOnLava;
        metadata.hapticFeedback = this.hapticFeedback;
        metadata.resolvedObject = this.resolvedObject;
        metadata.resolvedReceptacle = this.resolvedReceptacle;
        metadata.targetIsVisibleAtStart = this.targetIsVisibleAtStart;
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
        asyncOperationHandle.WaitForCompletion();

        // Set consistentColors to randomize segmentation mask colors if required
        this.agentManager.consistentColors = action.consistentColors;
        base.Initialize(action);

        this.step = 0;
        this.stepsOnLava = 0;
        foreach(HapticFeedback hf in Enum.GetValues(typeof(HapticFeedback))) {
            if (!hapticFeedback.ContainsKey(hf.ToString().ToLower()))
                hapticFeedback.Add(hf.ToString().ToLower(), false);
        }
      
        mcsMain.enableVerboseLog = mcsMain.enableVerboseLog || action.logs;
        // Reset the MCS scene configuration data and player.
        mcsMain.ChangeCurrentScene(action.sceneConfig);
    }

    public void OnSceneChange() {
        CapsuleCollider cc = GetComponent<CapsuleCollider>();
        cc.height = COLLIDER_HEIGHT;
        cc.center = new Vector3(0,COLLIDER_CENTER,0);
        cc.radius = COLLIDER_RADIUS;
        groundObjectsCollider.radius = GROUND_OBJECTS_COLLIDER_RADIUS;
        this.stepsOnLava = 0;
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
        objectMetadata.associatedWithAgent = simObj.associatedWithAgent == null ? "" : simObj.associatedWithAgent;

        MCSSimulationAgent simulationAgent = simObj.GetComponent<MCSSimulationAgent>();
        objectMetadata.simulationAgentHeldObject = simulationAgent == null ? "" : simulationAgent.heldObject == null ? "" : simulationAgent.heldObject.objectID;
        objectMetadata.simulationAgentIsHoldingHeldObject = simulationAgent == null ? false : simulationAgent.isHoldingHeldObject;

        return objectMetadata;
    }

    public override void OpenObject(ServerAction action) {
        bool continueAction = TryConvertingEachScreenPointToId(action);

        if (!continueAction) {
            return;
        }

        action.restrictOpenDoors = mcsMain.currentScene.restrictOpenDoors;

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
            if (!ItemInHand) 
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
        this.resolvedObject = "";
        this.resolvedReceptacle = "";
        if (this.cameraCullingMask < 0) {
            this.cameraCullingMask = this.GetComponentInChildren<Camera>().cullingMask;
        }
        this.GetComponentInChildren<Camera>().cullingMask = this.cameraCullingMask;

        this.lastInputAction = 
                controlCommand.action.Equals("Pass") ? InputAction.PASS :
                controlCommand.action.Equals("MoveAhead") ||
                controlCommand.action.Equals("MoveBack") ||
                controlCommand.action.Equals("MoveLeft") ||
                controlCommand.action.Equals("MoveRight") ? InputAction.MOVEMENT :
                controlCommand.action.Equals("RotateLook") ||
                controlCommand.action.Equals("RotateLeft") ||
                controlCommand.action.Equals("RotateRight") ||
                controlCommand.action.Equals("LookUp") ||
                controlCommand.action.Equals("LookDown") ? InputAction.ROTATE :
                InputAction.OTHER;

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

    public override void ResetAgentHandPosition() {
        // Don't reset the player's hand position if the player is just moving or rotating.
        // Use this.lastAction here because this function's ServerAction argument is sometimes null.
        if (this.lastAction.StartsWith("Move") || this.lastAction.StartsWith("Rotate") ||
            this.lastAction.StartsWith("Look") || this.lastAction.StartsWith("Teleport")) {
            return;
        }
        base.ResetAgentHandPosition();
    }

    public override void ResetAgentHandRotation() {
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
            StartCoroutine(this.SimulatePhysicsSaveImagesIncreaseStep());
        }

        else {
            // (Also simulate the physics after initialization so that the objects can settle down onto the floor.)
            this.SimulatePhysicsOnce();
            mcsMain.UpdateOnPhysicsSubstep();
            // Notify the AgentManager to send the action output metadata and images to the Python API.
            ((MCSPerformerManager)this.agentManager).FinalizeEmit();
        }
    }

    private void SimulatePhysicsOnce() {
        if(lastInputAction == InputAction.PASS || lastInputAction == InputAction.OTHER) {
            MatchAgentHeightToStructureBelow();
        } //for movement
        else if (lastInputAction == InputAction.MOVEMENT) {
            MatchAgentHeightToStructureBelow();
            this.movementActionFinished = moveInDirection((this.movementActionData.direction),
                    this.movementActionData.UniqueID,
                    this.movementActionData.maxDistanceToObject,
                    this.movementActionData.forceAction);
            actionFinished(this.movementActionFinished);
        } //for rotation
        else if (lastInputAction == InputAction.ROTATE) {
            MatchAgentHeightToStructureBelow();
            RotateLookAcrossFrames(this.lookRotationActionData);
            RotateLookBodyAcrossFrames(this.bodyRotationActionData);
            actionFinished(true);
        }
        //haptic feedback checks
        foreach(string hf in hapticFeedback.Keys.ToList()) {
            hapticFeedback[hf] = false;
        }
        CheckIfInLava();

        //Simulation Agent Animations
        List<MCSSimulationAgent> simulationAgents =  this.simulationAgents;
        foreach(MCSSimulationAgent simAgent in simulationAgents) {
            for(int i = 0; i<MCSController.SIMULATION_AGENT_ANIMATION_FRAMES_PER_PHYSICS_STEPS; i++) {
                simAgent.IncrementAnimationFrame();
            }
        }

        // Call Physics.Simulate multiple times with a small step value because a large step
        // value causes collision errors.  From the Unity Physics.Simulate documentation:
        // "Using step values greater than 0.03 is likely to produce inaccurate results."   
        for (int i = 0; i < MCSController.PHYSICS_SIMULATION_STEPS; ++i) {
            Physics.Simulate(MCSController.PHYSICS_SIMULATION_STEP_SECONDS);
        }
        physicsFramesPerSecond = 1.0f / (MCSController.PHYSICS_SIMULATION_STEP_SECONDS * MCSController.PHYSICS_SIMULATION_STEPS);
    }

    private void CheckIfInLava() {
        if (mcsMain.isPassiveScene) {
            return;
        }
        Ray ray = new Ray(transform.position, Vector3.down);
        RaycastHit hit;
        Physics.SphereCast(transform.position, AGENT_RADIUS, Vector3.down, out hit, AGENT_STARTING_HEIGHT + 0.01f, 1<<8, QueryTriggerInteraction.Ignore);
        Renderer renderer = hit.transform.GetComponent<Renderer>();
        if(renderer!=null) {
            Material material = renderer.material;
            //this is at the end of every material name
            string materialInstanceString = " (Instance)";
            string materialName = material.name.Substring(0, material.name.Length - materialInstanceString.Length);

            if(material != null && MCSConfig.LAVA_MATERIAL_REGISTRY.Any(key=>key.Key.Contains(materialName))) {
                stepsOnLava++;
                hapticFeedback[HapticFeedback.ON_LAVA.ToString().ToLower()] = true;
            }
        }
    }

    private IEnumerator SimulatePhysicsSaveImagesIncreaseStep() {
        // Run the physics simulation for a little bit, then pause and save the images for the current scene.
        this.SimulatePhysicsOnce();

        mcsMain.UpdateOnPhysicsSubstep();

        // Wait for the end of frame after we run the physics simulation but before we save the images.
        yield return new WaitForEndOfFrame(); // Required for coroutine functions

        ((MCSPerformerManager)this.agentManager).SaveImages(this.imageSynthesis);
        ((MCSPerformerManager)this.agentManager).FinalizeEmit();
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

        this.resolvedObject = action.objectId != null ? action.objectId : "";
        return agentState != AgentState.ActionComplete;

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
            this.resolvedReceptacle = action.receptacleObjectId != null ? action.receptacleObjectId : "";
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
        float sizeY = target.BoundingBox.transform.localScale.y;

        BoxCollider boundingBoxCollider = target.BoundingBox.GetComponent<BoxCollider>();
        positionY = positionY + (boundingBoxCollider.center.y * target.BoundingBox.transform.localScale.y);
        sizeY = sizeY * boundingBoxCollider.size.y;

        float multiplierY = (target.BoundingBox.transform.parent.localScale.y / this.transform.localScale.y);
        positionY = positionY * multiplierY;
        sizeY = sizeY * multiplierY;
        float handY = -1 * ((sizeY / 2.0f) + positionY + MCSController.DISTANCE_HELD_OBJECT_Y);

        // Ensure that a tall held object is not positioned inside of the floor below.
        float minY = (sizeY / 2.0f) - positionY - (this.transform.position.y / this.transform.localScale.y) +
            MCSController.DISTANCE_HELD_OBJECT_Y;

        // Find the largest side of the object's bounding box and then set the hand position to half the largest side of the object plus the agent's radius 
        // distance away to ensure the held object is not clipping inside the agent's collider
        // This first check is if either the bounding box transform or collider size was changed to adjust the size of the bounding box
        Vector3 boundingBoxSize = 
            boundingBoxCollider.transform.localScale != Vector3.one ? boundingBoxCollider.transform.localScale : boundingBoxCollider.size;
        float largestSide = Mathf.Max(target.transform.localScale.x * boundingBoxSize.x, target.transform.localScale.z * boundingBoxSize.z) / 2;

        // Set the rotation of the object to its original rotation
        this.AgentHand.transform.localPosition = 
            new Vector3(0, Math.Max(handY, minY), ((largestSide + (COLLIDER_RADIUS * transform.localScale.x)) / transform.localScale.x) + DISTANCE_HELD_OBJECT_Z);
    }

    //overrides from PhysicsRemoteFPSAgentController which enable agent/object collisions
    public override void MoveLeft(ServerAction action) {
        action.moveMagnitude = action.moveMagnitude > 0 ? action.moveMagnitude : gridSize;
        this.movementActionData = new MCSMovementActionData(-1 * transform.right * action.moveMagnitude,
            action.objectId,
            action.maxAgentsDistance, action.forceAction);
        this.inputDirection = "left";
        this.serverActionMoveMagnitude = action.moveMagnitude;
    }

    public override void MoveRight(ServerAction action) {
        action.moveMagnitude = action.moveMagnitude > 0 ? action.moveMagnitude : gridSize;
        this.movementActionData = new MCSMovementActionData(transform.right * action.moveMagnitude,
            action.objectId,
            action.maxAgentsDistance, action.forceAction);
        this.inputDirection = "right";
        this.serverActionMoveMagnitude = action.moveMagnitude;
    }

    public override void MoveAhead(ServerAction action) {
        action.moveMagnitude = action.moveMagnitude > 0 ? action.moveMagnitude : gridSize;
        this.movementActionData = new MCSMovementActionData(transform.forward * action.moveMagnitude,
            action.objectId,
            action.maxAgentsDistance, action.forceAction);
        this.inputDirection = "forward";
        this.serverActionMoveMagnitude = action.moveMagnitude;
    }

    public override void MoveBack(ServerAction action) {
        action.moveMagnitude = action.moveMagnitude > 0 ? action.moveMagnitude : gridSize;
        this.movementActionData = new MCSMovementActionData(-1 * transform.forward * action.moveMagnitude,
            action.objectId,
            action.maxAgentsDistance, action.forceAction);
        this.inputDirection = "back";
        this.serverActionMoveMagnitude = action.moveMagnitude;
    }

    public float MatchAgentHeightToStructureBelow() {
        if (mcsMain.currentScene == null || mcsMain.isPassiveScene) {
            return 0;
        }

        //Raycast down
        Vector3 origin = new Vector3(transform.position.x, this.GetComponent<CapsuleCollider>().bounds.max.y, transform.position.z);
        RaycastHit hit;
        LayerMask layerMask = ~(1 << 10);

        //raycast to traverse structures at anything <= 45 degree angle incline
        bool isAnythingBelowAgent = Physics.SphereCast(origin, AGENT_RADIUS, Vector3.down, out hit,
                Mathf.Infinity, layerMask, QueryTriggerInteraction.Ignore);
        if (isAnythingBelowAgent) {
            // Note that the floor is a structure that's always below the agent, so this hit is usually just the floor.
            StructureObject structureObjectScript = hit.transform.GetComponent<StructureObject>();
            SimObjPhysics simObjPhysicsScript = hit.transform.GetComponent<SimObjPhysics>();
            if ((structureObjectScript != null) || (simObjPhysicsScript != null && simObjPhysicsScript.IsSeesaw)) {
                hit.rigidbody.AddForceAtPosition(Physics.gravity * GetComponent<Rigidbody>().mass, hit.point);
                //for pose changes on structures only
                float oldHeight = this.transform.position.y;
                Vector3 newHeight = new Vector3(transform.position.x, (hit.point.y + AGENT_STARTING_HEIGHT), transform.position.z);
                this.transform.position = newHeight;
                if (oldHeight != this.transform.position.y) {
                    AdjustLocationAfterHeightAdjustment();
                }
            }
        }
        //method needs a return value
        return 0;
    }

    private void AdjustLocationAfterHeightAdjustment() {
        CapsuleCollider myCollider = GetComponent<CapsuleCollider>();
        float radius;
        Vector3 point1, point2;

        //This method shoots a ray down from the agents head to find the highest point below the agent that is touching the ground and uses that point as the base of the overlap check
        CapsuleCastInfoByShootingRayToFloor(out point1, out point2, out radius);
        
        //Determine if we are colliding (or within skin width) of another object
        Collider[] overlapColliders = Physics.OverlapCapsule(point1, point2, radius + (radius*0.5f), 1 << 8);

        //we divide by scale here because we are going to expand the scaled collider by this value
        float obstructionVsCollisionDifference = m_CharacterController.skinWidth / Mathf.Max(transform.localScale.x, transform.localScale.z);

        //if we are colliding, we need to move a bit
        if (overlapColliders.Length > 0) {
            foreach (Collider c in overlapColliders) {
                // Don't avoid lightweight objects if the agent can simply move into their space and "shove" them out of the way.
                SimObjPhysics simObjPhysicsScript = c.gameObject.GetComponentInParent<SimObjPhysics>();
                bool isSeesaw = (simObjPhysicsScript != null && simObjPhysicsScript.IsSeesaw);
                Rigidbody rigidbody = c.gameObject.GetComponentInParent<Rigidbody>();
                if (!isSeesaw && rigidbody != null && AgentCanMoveIntoObject(rigidbody)) {
                    continue;
                }
                Vector3 direction;
                float distance;
                //Need to increase the collider radius temporarily to ensure we collide with something just outside but in our "skin"
                myCollider.radius += obstructionVsCollisionDifference;
                //This function determines the distance and direct we need to move to no longer be colliding.
                bool overlap = Physics.ComputePenetration(myCollider, transform.position, transform.rotation, c, c.transform.position,
                    c.transform.rotation, out direction, out distance);
                myCollider.radius -= obstructionVsCollisionDifference;
                Vector3 newPos = transform.position;
                if (overlap) {
                    Vector3 shift = direction * distance;
                    newPos += shift;
                    transform.position = newPos;
                }
            }
        }
    }

    protected override void SubPositionAdjustment() {
        MatchAgentHeightToStructureBelow();
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

        Vector3 updatedRotation = new Vector3(horizonChange, 0, 0);
        m_Camera.transform.rotation = Quaternion.Euler(m_Camera.transform.rotation.eulerAngles + updatedRotation);
    }

    public void RotateLookBodyAcrossFrames(MCSRotationData rotationActionData)
    {
        Quaternion currentAngle = rotationActionData.startingRotation;
        Quaternion distance = rotationActionData.endRotation;

        float rotationChange =
            distance.eulerAngles.y > 90 ? distance.eulerAngles.y - 360 :
            distance.eulerAngles.y < -90 ? distance.eulerAngles.y + 360 : distance.eulerAngles.y;

        Vector3 updatedRotation = new Vector3(0, rotationChange, 0);
        transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + updatedRotation);
    }

    public void TorqueObject(ServerAction action) {
        bool continueAction = TryConvertingEachScreenPointToId(action);

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

        if (physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(action.objectId) &&
            ItemInHand != null && action.objectId == ItemInHand.GetComponent<SimObjPhysics>().objectID) {
            Debug.Log("Cannot Torque object. Object " + action.objectId + " is in agent's hand. Calling ThrowObject instead.");
            ThrowObject(action);
        } else {
            //Add Torque
            ApplyForceObject(action);
        }
    }

    public void RotateObject(ServerAction action) {
        bool continueAction = TryConvertingEachScreenPointToId(action);

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

        if (physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(action.objectId) &&
            ItemInHand != null && action.objectId == ItemInHand.GetComponent<SimObjPhysics>().objectID) {
            Debug.Log("Cannot Rotate object. Object " + action.objectId + " is in agent's hand. Calling ThrowObject instead.");
            ThrowObject(action);
        } else {
            //Add Rotation
            ApplyForceObject(action);
        }
    }

    public void MoveObject(ServerAction action) {
        bool continueAction = TryConvertingEachScreenPointToId(action);

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

        if (physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(action.objectId) &&
            ItemInHand != null && action.objectId == ItemInHand.GetComponent<SimObjPhysics>().objectID) {
            Debug.Log("Cannot Move object. Object " + action.objectId + " is in agent's hand. Calling ThrowObject instead.");
            ThrowObject(action);
        } else {
            //Move Object
            action.agentTransform = transform;
            ApplyForceObject(action);
        }
    }

    public override void UpdateAgentObjectAssociations(SimObjPhysics sop) {
        if(this.simulationAgents.Count > 0) {
            if (sop.associatedWithAgent != "" && this.agentObjectAssociations.ContainsKey(sop.associatedWithAgent)) {
                foreach (MCSSimulationAgent agent in this.simulationAgents) {
                    if (sop.associatedWithAgent == agent.name) {
                        agent.isHoldingHeldObject = false;
                        if (agent.SetPrevious())
                            return;
                        agent.SetDefaultAnimation(usePreviousClip: agent.previousClip != "", interactionComplete: true);
                    }
                }
            }
        }
    }

    public void InteractWithAgent(ServerAction action) {
        bool continueInteraction = IsVisableAndInDistance(action, "Simulation Agent");
        if(!continueInteraction) {
            actionFinished(false);
            return;
        }  

        MCSSimulationAgent simulationAgent = GameObject.Find(action.objectId).GetComponent<MCSSimulationAgent>();
        if(simulationAgent == null) {
            this.lastActionStatus = Enum.GetName(typeof(ActionStatus), ActionStatus.NOT_AGENT);
            string outputMessage = "The object being interacted with is NOT an agent.";
            Debug.Log(outputMessage);
            actionFinished(false);
            return;
        }

        if(simulationAgent.IsDoingAnyInteractions()) {

            string outputMessage = "Simulation Agent is currently interacting with performer.";
            this.lastActionStatus = Enum.GetName(typeof(ActionStatus), ActionStatus.AGENT_CURRENTLY_INTERACTING_WTIH_PERFORMER);
            Debug.Log(outputMessage);
            actionFinished(false);
            return;
        }

        this.lastActionStatus = Enum.GetName(typeof(ActionStatus), ActionStatus.SUCCESSFUL);
        if (simulationAgent.isHoldingHeldObject && 
            simulationAgent.simAgentActionState != MCSSimulationAgent.SimAgentActionState.InteractingHoldingHeldObject && 
            simulationAgent.simAgentActionState != MCSSimulationAgent.SimAgentActionState.HoldingOutHeldObject) {

            simulationAgent.simAgentActionState = MCSSimulationAgent.SimAgentActionState.InteractingHoldingHeldObject;
            simulationAgent.RotateAgentToLookAtPerformer();
        }

        else {
            simulationAgent.simAgentActionState = MCSSimulationAgent.SimAgentActionState.InteractingNotHoldingHeldObject;
            simulationAgent.RotateAgentToLookAtPerformer();
        }
        actionFinished(true);
    }

    public bool IsVisableAndInDistance(ServerAction action, string obstructedObject) {
        bool continueAction = TryConvertingEachScreenPointToId(action);

        if (!continueAction) {
            return false;
        }

        if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(action.objectId)) {
            errorMessage = "Object ID appears to be invalid.";
            Debug.Log(errorMessage);
            this.lastActionStatus = Enum.GetName(typeof(ActionStatus), ActionStatus.NOT_OBJECT);
            return false;
        }

        if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(action.objectId)) {
                errorMessage = "Object ID " + action.objectId + " appears to be invalid.";
                Debug.Log(errorMessage);
                this.lastActionStatus = Enum.GetName(typeof(ActionStatus), ActionStatus.NOT_OBJECT);
                return false;
            }
            
        SimObjPhysics target = physicsSceneManager.ObjectIdToSimObjPhysics[action.objectId];

        if (target == null || !target.GetComponent<SimObjPhysics>() || !target.isInteractable) {
            errorMessage = action.objectId + " is not interactable.";
            this.lastActionStatus = Enum.GetName(typeof(ActionStatus), ActionStatus.NOT_INTERACTABLE);
            return false;
        }

        // Must call this now because it will set target.isInteractable
        bool isNotVisible = !objectIsCurrentlyVisible(target, maxVisibleDistance);

        if (isNotVisible || !target.isInteractable) {
            if (Vector3.Distance(transform.position, FindClosestPoint(transform.position, target)) < maxVisibleDistance) {
                errorMessage = obstructedObject + " " + action.objectId + " is obstructed.";
                Debug.Log(errorMessage);
                this.lastActionStatus = Enum.GetName(typeof(ActionStatus), ActionStatus.OBSTRUCTED);
                return false;
            }
            
        }

        if (!action.forceAction && (isNotVisible || !target.isInteractable)) {
            errorMessage = action.objectId + " is not visible.";
            this.lastActionStatus = Enum.GetName(typeof(ActionStatus), ActionStatus.OUT_OF_REACH);
            return false;
        }
        return true;
    }

    public void CheckIfTargetIsVisibleAtStart()
    {
        Renderer r = retrievalTargetGameObject.GetComponent<Renderer>();
        Vector3 center = r.bounds.center;
        Vector3 right = new Vector3(r.bounds.max.x, r.bounds.center.y, r.bounds.center.z);
        Vector3 left = new Vector3(r.bounds.min.x, r.bounds.center.y, r.bounds.center.z);
        Vector3 front = new Vector3(r.bounds.center.x, r.bounds.center.y, r.bounds.max.z);
        Vector3 back = new Vector3(r.bounds.center.x, r.bounds.center.y, r.bounds.min.z);
        Vector3 top = new Vector3(r.bounds.center.x, r.bounds.max.y, r.bounds.center.z);
        Vector3 bottom = new Vector3(r.bounds.center.x, r.bounds.min.y, r.bounds.center.z);

        RaycastHit hit;
        List<Vector3> points = new List<Vector3>{center,right,left,front,back,top,bottom};
        foreach (Vector3 p in points) {
            Vector3 direction = p - transform.position;
            if (Physics.Raycast(transform.position, direction, out hit, Vector3.Distance(p, transform.position), 1 << 8))
            {
                //Debug red lines to show lines that dont hit the target, debug green for the raycast that does
                
                //Debug.DrawLine(transform.position, hit.point, Color.red, 10f);
                if (hit.transform.name == retrievalTargetGameObject.name) {
                    //Debug.DrawLine(transform.position, hit.point, Color.green, 10f);
                    this.targetIsVisibleAtStart = true;
                    return;
                }
            }
        }
        this.targetIsVisibleAtStart = false;
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
