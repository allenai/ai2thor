// Copyright Allen Institute for Artificial Intelligence 2017
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityStandardAssets.ImageEffects;
using System.Linq;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.AI;
using Newtonsoft.Json.Linq;

namespace UnityStandardAssets.Characters.FirstPerson {
	[RequireComponent(typeof(CharacterController))]

	abstract public class BaseFPSAgentController : MonoBehaviour {
		//debug draw bounds of objects in editor
        #if UNITY_EDITOR
            protected List<Bounds> gizmobounds = new List<Bounds>();
        #endif
        [SerializeField] public SimObjPhysics[] VisibleSimObjPhysics 
        {
            get;
            protected set;
        }
        [SerializeField] protected bool IsHandDefault = true;
        [SerializeField] public GameObject ItemInHand = null; //current object in inventory
        [SerializeField] public GameObject AgentHand = null;
        [SerializeField] protected GameObject DefaultHandPosition = null;
        [SerializeField] protected Transform rotPoint;
        [SerializeField] protected GameObject DebugPointPrefab;
        [SerializeField] private GameObject GridRenderer = null;
        [SerializeField] protected GameObject DebugTargetPointPrefab;
        [SerializeField] protected bool inTopLevelView = false;
        [SerializeField] protected Vector3 lastLocalCameraPosition;
        [SerializeField] protected Quaternion lastLocalCameraRotation;
        public float autoResetTimeScale = 1.0f;

        public Vector3[] reachablePositions = new Vector3[0];
        protected float gridVisualizeY = 0.005f; //used to visualize reachable position grid, offset from floor
        protected HashSet<int> initiallyDisabledRenderers = new HashSet<int>();
		// first person controller parameters
		[SerializeField]
		protected bool m_IsWalking;
		[SerializeField]
		protected float m_WalkSpeed;
		[SerializeField]
		protected float m_RunSpeed;
		[SerializeField]
		protected float m_GravityMultiplier;
		protected static float gridSize = 0.25f;
        //time the checkIfObjectHasStoppedMoving coroutine waits for objects to stop moving
        protected float timeToWaitForObjectsToComeToRest = 0.0f;
        //determins default move distance for move actions
		protected float moveMagnitude;
        //determines rotation increment of rotate functions
        protected float rotateStepDegrees = 90.0f;
        protected bool snapToGrid;
		protected bool continuousMode;//deprecated, use snapToGrid instead
		public ImageSynthesis imageSynthesis;
        public GameObject VisibilityCapsule = null;//used to keep track of currently active VisCap: see different vis caps for modes below
        public GameObject TallVisCap;//meshes used for Tall mode
        public GameObject BotVisCap;//meshes used for Bot mode
        public GameObject DroneVisCap;//meshes used for Drone mode
        public GameObject DroneBasket;//reference to the drone's basket object
        private bool isVisible = true;
        public bool inHighFrictionArea = false;
        // outbound object filter
        private SimObjPhysics[] simObjFilter = null;
        private VisibilityScheme visibilityScheme = VisibilityScheme.Collider;
        public AgentState agentState = AgentState.Emit;

        public const float DefaultAllowedErrorInShortestPath = 0.0001f;

        public bool IsVisible {
			get { return isVisible; }
			set {
                // first default all Vis capsules of all modes to not enabled
                HideAllAgentRenderers();

                // The VisibilityCapsule will be set to either Tall or Bot 
                // from the SetAgentMode call in BaseFPSAgentController's Initialize()
                foreach (Renderer r in VisibilityCapsule.GetComponentsInChildren<Renderer>()) {
                    r.enabled = value;
                }
				isVisible = value;
			}
        }

        public bool IsProcessing {
            get {
                return this.agentState == AgentState.Processing;
            }
        }

        public bool ReadyForCommand {
            get {
                return this.agentState == AgentState.Emit;
            }

        }

		protected float maxDownwardLookAngle = 60f;
		protected float maxUpwardLookAngle = 30f;
		//allow agent to push sim objects that can move, for physics
		protected bool PushMode = false;
		protected int actionCounter;
		protected Vector3 targetTeleport;
        public AgentManager agentManager;
		public Camera m_Camera;
        [SerializeField] protected float cameraOrthSize;
		protected float m_XRotation;
		protected float m_ZRotation;
		protected Vector2 m_Input;
		protected Vector3 m_MoveDir = Vector3.zero;
		public CharacterController m_CharacterController;
		protected CollisionFlags m_CollisionFlags;
		protected Vector3 lastPosition;

        // These are public for actions outside of the agent context (e.g., AddThirdPartyCamera)
        public string lastAction;
        public bool lastActionSuccess;
        public string errorMessage;
        public ServerActionErrorCode errorCode;

		public System.Object actionReturn;
        [SerializeField] protected Vector3 standingLocalCameraPosition;
        [SerializeField] protected Vector3 crouchingLocalCameraPosition;
        public float maxVisibleDistance = 1.5f; //changed from 1.0f to account for objects randomly spawned far away on tables/countertops, which would be not visible at 1.0f
        protected float[, , ] flatSurfacesOnGrid = new float[0, 0, 0];
        protected float[, ] distances = new float[0, 0];
        protected float[, , ] normals = new float[0, 0, 0];
		protected bool[, ] isOpenableGrid = new bool[0, 0];
        protected string[] segmentedObjectIds = new string[0];
        [SerializeField] public string[] objectIdsInBox = new string[0];
        protected int actionIntReturn;
        protected float actionFloatReturn;
        protected float[] actionFloatsReturn;
        protected Vector3[] actionVector3sReturn;
        protected string[] actionStringsReturn;
        public bool alwaysReturnVisibleRange = false;
		// initial states
		protected Vector3 init_position;
		protected Quaternion init_rotation;
		public int actionDuration = 3;

		// internal state variables
		private float lastEmitTime;
		protected List<string> collisionsInAction;// tracking collided objects
		protected string[] collidedObjects;// container for collided objects
        protected HashSet<Collider> collidersToIgnoreDuringMovement = new HashSet<Collider>();
		protected Quaternion targetRotation;
        // Javascript communication
        private JavaScriptInterface jsInterface = null;
		public Quaternion TargetRotation
		{
			get { return targetRotation; }
		}

        private PhysicsSceneManager _physicsSceneManager = null;
        //use as reference to the PhysicsSceneManager object
        protected PhysicsSceneManager physicsSceneManager
        {
            get {
                if (_physicsSceneManager == null) {
                    _physicsSceneManager = GameObject.Find("PhysicsSceneManager").GetComponent<PhysicsSceneManager>();
                }
                return _physicsSceneManager;
            }
        }

        //reference to prefab for activiting the cracked camera effect via CameraCrack()
        [SerializeField] GameObject CrackedCameraCanvas = null;

		// Initialize parameters from environment variables
		protected virtual void Awake() {
            #if UNITY_WEBGL
                this.jsInterface = this.GetComponent<JavaScriptInterface>();
                this.jsInterface.enabled = true;
            #endif

            // character controller parameters
            m_CharacterController = GetComponent<CharacterController>();
			this.m_WalkSpeed = 2;
			this.m_RunSpeed = 10;
			this.m_GravityMultiplier = 2;
		}

		// Use this for initialization
		public virtual void Start() {
			m_Camera = this.gameObject.GetComponentInChildren<Camera>();

			// set agent initial states
			targetRotation = transform.rotation;
			collidedObjects = new string[0];
			collisionsInAction = new List<string>();

            // setting default renderer settings
            // this hides renderers not used in tall mode, and also sets renderer
            // culling in FirstPersonCharacterCull.cs to ignore tall mode renderers
            HideAllAgentRenderers();

			// record initial positions and rotations
			init_position = transform.position;
			init_rotation = transform.rotation;

			agentManager = GameObject.Find("PhysicsSceneManager").GetComponentInChildren<AgentManager>();

            // default nav mesh agent to false cause WHY DOES THIS BREAK THINGS I GUESS IT DOESN TLIKE TELEPORTING
            this.GetComponent<NavMeshAgent>().enabled = false;

            // Recording initially disabled renderers and scene bounds 
            // then setting up sceneBounds based on encapsulating all renderers
            foreach (Renderer r in GameObject.FindObjectsOfType<Renderer>()) {
                if (!r.enabled) {
                    initiallyDisabledRenderers.Add(r.GetInstanceID());
                } else {
                    agentManager.SceneBounds.Encapsulate(r.bounds);
                }
            }

            //On start, activate gravity
            Vector3 movement = Vector3.zero;
            movement.y = Physics.gravity.y * m_GravityMultiplier;
            m_CharacterController.Move(movement);
		}

        //defaults all agent renderers, from all modes (tall, bot, drone), to hidden for initialization default
        protected void HideAllAgentRenderers() {
            foreach (Renderer r in TallVisCap.GetComponentsInChildren<Renderer>()) {
                if (r.enabled) {
                    r.enabled = false;
                }
            }

            foreach (Renderer r in BotVisCap.GetComponentsInChildren<Renderer>()) {
                if (r.enabled) {
                    r.enabled = false;
                }
            }

            foreach (Renderer r in DroneVisCap.GetComponentsInChildren<Renderer>())
            {
                if (r.enabled)
                {
                    r.enabled = false;
                }
            }
        }

		public void actionFinishedEmit(bool success, object actionReturn = null) {
            actionFinished(success: success, newState: AgentState.Emit, actionReturn: actionReturn);
		}

		protected virtual void actionFinished(bool success, AgentState newState, object actionReturn = null) {
			if (!this.IsProcessing) {
				Debug.LogError ("ActionFinished called with agentState not in processing ");
			}

            lastActionSuccess = success;
			this.agentState = newState;
			this.actionReturn = actionReturn;
			actionCounter = 0;
			targetTeleport = Vector3.zero;
        }

		public virtual void actionFinished(bool success, object actionReturn = null, string errorMessage = null) {
            if (errorMessage != null) {
                this.errorMessage = errorMessage;
            }
            actionFinished(success: success, newState: AgentState.ActionComplete, actionReturn: actionReturn);
            this.resumePhysics();
		}

        protected virtual void resumePhysics() {}


        // max step count represents a 100m * 100m room. Adjust this value later if we end up making bigger rooms?
        public Vector3[] getReachablePositions(
            float gridMultiplier = 1.0f,
            int maxStepCount = 10000,
            bool visualize = false,
            Color? gridColor = null
        ) {
            CapsuleCollider cc = GetComponent<CapsuleCollider>();

            float sw = m_CharacterController.skinWidth;
            Queue<Vector3> pointsQueue = new Queue<Vector3>();
            pointsQueue.Enqueue(transform.position);

            //float dirSkinWidthMultiplier = 1.0f + sw;
            Vector3[] directions = {
                new Vector3(1.0f, 0.0f, 0.0f),
                new Vector3(0.0f, 0.0f, 1.0f),
                new Vector3(-1.0f, 0.0f, 0.0f),
                new Vector3(0.0f, 0.0f, -1.0f)
            };

            HashSet<Vector3> goodPoints = new HashSet<Vector3>();
            HashSet<Vector3> seenPoints = new HashSet<Vector3>();
            int layerMask = 1 << 8;
            int stepsTaken = 0;
            while (pointsQueue.Count != 0) {
                stepsTaken += 1;
                Vector3 p = pointsQueue.Dequeue();
                if (!goodPoints.Contains(p)) {
                    goodPoints.Add(p);
                    HashSet<Collider> objectsAlreadyColliding = new HashSet<Collider>(objectsCollidingWithAgent());
                    foreach (Vector3 d in directions) {
                        Vector3 newPosition = p + d * gridSize * gridMultiplier;
                        if (seenPoints.Contains(newPosition)) {
                            continue;
                        }
                        seenPoints.Add(newPosition);

                        RaycastHit[] hits = capsuleCastAllForAgent(
                            cc,
                            sw,
                            p,
                            d,
                            (gridSize * gridMultiplier),
                            layerMask
                        );

                        bool shouldEnqueue = true;
                        foreach (RaycastHit hit in hits) {
                            if (hit.transform.gameObject.name != "Floor" &&
                                !ancestorHasName(hit.transform.gameObject, "FPSController") &&
                                !objectsAlreadyColliding.Contains(hit.collider)
                            ) {
                                shouldEnqueue = false;
                                break;
                            }
                        }
                        bool inBounds = agentManager.SceneBounds.Contains(newPosition);
                        if (errorMessage == "" && !inBounds) {
                            errorMessage = "In " +
                                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name +
                                ", position " + newPosition.ToString() +
                                " can be reached via capsule cast but is beyond the scene bounds.";
                        }

                        shouldEnqueue = shouldEnqueue && inBounds && (
                            handObjectCanFitInPosition(newPosition, 0.0f) ||
                            handObjectCanFitInPosition(newPosition, 90.0f) ||
                            handObjectCanFitInPosition(newPosition, 180.0f) ||
                            handObjectCanFitInPosition(newPosition, 270.0f)
                        );
                        if (shouldEnqueue) {
                            pointsQueue.Enqueue(newPosition);

                            if (visualize) {
                                var gridRenderer = Instantiate(GridRenderer, Vector3.zero, Quaternion.identity);
                                var gridLineRenderer = gridRenderer.GetComponentInChildren<LineRenderer>();
                                if (gridColor.HasValue) {
                                    gridLineRenderer.startColor = gridColor.Value;
                                    gridLineRenderer.endColor =  gridColor.Value;
                                }
                                // gridLineRenderer.startColor = ;
                                // gridLineRenderer.endColor = ;
                                gridLineRenderer.positionCount = 2;
                                // gridLineRenderer.startWidth = 0.01f;
                                // gridLineRenderer.endWidth = 0.01f;
                                gridLineRenderer.SetPositions(new Vector3[] { 
                                    new Vector3(p.x, gridVisualizeY, p.z),
                                    new Vector3(newPosition.x, gridVisualizeY, newPosition.z)
                                });
                            }
                            #if UNITY_EDITOR
                            Debug.DrawLine(p, newPosition, Color.cyan, 100000f);
                            #endif
                        }
                    }
                }
                //default maxStepCount to scale based on gridSize
                if (stepsTaken > Math.Floor(maxStepCount/(gridSize * gridSize))) {
                    errorMessage = "Too many steps taken in GetReachablePositions.";
                    break;
                }
            }

            Vector3[] reachablePos = new Vector3[goodPoints.Count];
            goodPoints.CopyTo(reachablePos);

            #if UNITY_EDITOR
            Debug.Log("count of reachable positions: " + reachablePos.Length);
            #endif

            return reachablePos;
        }

        public void GetReachablePositions(int maxStepCount = 0) {
            if (maxStepCount != 0) {
                reachablePositions = getReachablePositions(1.0f, maxStepCount);
            } else {
                reachablePositions = getReachablePositions();
            }

            if (errorMessage != "") {
                actionFinishedEmit(false);
            } else {
                actionFinishedEmit(true, reachablePositions);
            }
        }

        ///////////////////////////////////////////
        ////////////// Initialize /////////////////
        ///////////////////////////////////////////

		public void Initialize(
            string agentMode = "default",
            float? fieldOfView = null,
            float gridSize = 0.25f,
            float timeScale = 1,
            float rotateStepDegrees = 90,
            bool snapToGrid = true,
            float visibilityDistance = 1.5f,
            float TimeToWaitForObjectsToComeToRest = 10.0f,
            bool renderDepthImage = false,
            bool renderClassImage = false,
            bool renderObjectImage = false,
            bool renderNormalsImage = false,
            string visibilityScheme = "Collider"
        ) {
            // set agent mode to default, bot or drone
            const HashSet<string> VALID_AGENT_MODES = new HashSet<string> {"default", "bot", "drone"};
            if (!VALID_AGENT_MODES.Contains(agentMode)) {
                throw new ArgumentException("agentMode be be in {'default', 'bot', 'drone'}.");
            }
            SetAgentMode(agentMode);

            // set up the gridSize
            if (this.gridSize <= 0 || this.gridSize > 5) {
                throw new ArgumentOutOfRangeException("grid size must be in the range (0, 5]");
            }
            this.gridSize = gridSize;
            StartCoroutine(checkInitializeAgentLocationAction());

            // fieldOfView is set to its defaults in SetAgentMode. But, here, we can overwrite it.
            if (fieldOfView != null) {
                if ((float)fieldOfView <= 0 || (float)fieldOfView >= 180) {
                    throw new ArgumentOutOfRangeException("fov must be set to (0, 180) noninclusive.");
                }
                m_Camera.fieldOfView = action.fieldOfView;
            }

            // set the time scale
            if (timeScale <= 0) {
                throw new ArgumentOutOfRangeException("Time scale must be > 0");
            }
            Time.timeScale = timeScale;

            // set rotateStepDegrees
            if (rotateStepDegrees <= 0.0) {
                throw new ArgumentOutOfRangeException("rotateStepDegrees must be a non-zero, non-negative float");
            }
            this.rotateStepDegrees = rotateStepDegrees;

            this.snapToGrid = snapToGrid;

            // set up image synthesis
            if (renderDepthImage || renderClassImage || renderObjectImage || renderNormalsImage) {
    			updateImageSynthesis(true);
    		}

            if (visibilityDistance <= 0) {
                throw new ArgumentOutOfRangeException("Visibility Distance must be > 0.");
            }

            this.maxVisibleDistance = action.visibilityDistance;

            NavMeshAgent navmeshAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            CapsuleCollider collider = GetComponent<CapsuleCollider>();
            if (collider != null && navmeshAgent != null) {
                navmeshAgent.radius = collider.radius;
                navmeshAgent.height = collider.height;
            }

            // initialize how long the default wait time for objects to stop moving is
            if (timeToWaitForObjectsToComeToRest < 0) {
                throw new ArgumentOutOfRangeException("timeToWaitForObjectsToComeToRest must be > 0.");
            }
            this.timeToWaitForObjectsToComeToRest = timeToWaitForObjectsToComeToRest;

            // visibilityScheme
            try {
                this.visibilityScheme = (VisibilityScheme) Enum.Parse(typeof(VisibilityScheme), visibilityScheme, ignoreCase: true);
            } catch (ArgumentException) {
                throw new ArgumentException($"Error parsing visibilityScheme: {visibilityScheme}");
            }
        }

        public void SetAgentMode(string mode) {
            string whichMode;
            whichMode = mode.ToLower();

            //null check for camera, used to ensure no missing references on initialization
            if (m_Camera == null) {
                m_Camera = gameObject.GetComponentInChildren<Camera>();
            }

            FirstPersonCharacterCull fpcc = m_Camera.GetComponent<FirstPersonCharacterCull>();

            //determine if we are in Tall or Bot mode (or other modes as we go on)
            if (whichMode == "default") {   
                //toggle FirstPersonCharacterCull
                fpcc.SwitchRenderersToHide(whichMode);

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

                //camera position
                m_Camera.transform.localPosition = new Vector3(0, 0.675f, 0);

                //camera FOV
                m_Camera.fieldOfView = 90f;

                //set camera stand/crouch local positions for Tall mode
                standingLocalCameraPosition = m_Camera.transform.localPosition;
                crouchingLocalCameraPosition = m_Camera.transform.localPosition + new Vector3(0, -0.675f, 0);// bigger y offset if tall
            } else if (whichMode == "bot") {
                //toggle FirstPersonCharacterCull
                fpcc.SwitchRenderersToHide(whichMode);

                VisibilityCapsule = BotVisCap;
                m_CharacterController.center = new Vector3(0, -0.45f, 0);
                m_CharacterController.radius = 0.175f;
                m_CharacterController.height = 0.9f;

                CapsuleCollider cc = this.GetComponent<CapsuleCollider>();
                cc.center = m_CharacterController.center;
                cc.radius = m_CharacterController.radius;
                cc.height = m_CharacterController.height;

                m_Camera.GetComponent<PostProcessVolume>().enabled = true;
                m_Camera.GetComponent<PostProcessLayer>().enabled = true;

                //camera position
                m_Camera.transform.localPosition = new Vector3(0, -0.0312f, 0);

                //camera FOV
                m_Camera.fieldOfView = 60f;

                //set camera stand/crouch local positions for Tall mode
                standingLocalCameraPosition = m_Camera.transform.localPosition;
                crouchingLocalCameraPosition = m_Camera.transform.localPosition + new Vector3(0, -0.2206f, 0);//smaller y offset if Bot

                // limit camera from looking too far down
				this.maxDownwardLookAngle = 30f;
				this.maxUpwardLookAngle = 30f;
                //this.horizonAngles = new float[] { 30.0f, 0.0f, 330.0f };
            } else if (whichMode == "drone") {
                //toggle first person character cull
                fpcc.SwitchRenderersToHide(whichMode);

                VisibilityCapsule = DroneVisCap;
                m_CharacterController.center = new Vector3(0,0,0);
                m_CharacterController.radius = 0.2f;
                m_CharacterController.height = 0.0f;

                CapsuleCollider cc = this.GetComponent<CapsuleCollider>();
                cc.center = m_CharacterController.center;
                cc.radius = m_CharacterController.radius;
                cc.height = m_CharacterController.height;

                m_Camera.GetComponent<PostProcessVolume>().enabled = false;
                m_Camera.GetComponent<PostProcessLayer>().enabled = false;

                //camera position set forward a bit for drone
                m_Camera.transform.localPosition = new Vector3(0, 0, 0.2f);

                //camera FOV for drone
                m_Camera.fieldOfView = 150f;

                //default camera stand/crouch for drone mode since drone doesn't stand or crouch
                standingLocalCameraPosition = m_Camera.transform.localPosition;
                crouchingLocalCameraPosition = m_Camera.transform.localPosition;

                //drone also needs to toggle on the drone basket
                DroneBasket.SetActive(true);
            }
        }

        ///////////////////////////////////////////
        //////////// getTargetObject //////////////
        ///////////////////////////////////////////

        // Helper method that parses objectId parameter to return the sim object that it target.
        // The action is halted if the objectId does not appear in the scene.
        private SimObjPhysics getTargetObject(string objectId, bool forceAction) {
            // an objectId was given, so find that target in the scene if it exists
            if (!physicsSceneManager.ObjectIdToSimObjPhysics.ContainsKey(objectId)) {
                throw new ArgumentException($"objectId: {objectId} is not the objectId on any object in the scene!");
            }

            // if object is in the scene and visible, assign it to 'target'
            SimObjPhysics target = null;
            foreach (SimObjPhysics sop in VisibleSimObjs(objectId: objectId, forceVisible: forceAction)) {
                target = sop;
            }

            // target not found!
            if (target == null) {
                throw new NullReferenceException("Target object not found within the specified visibility.");
            }

            return target;
        }

        // Helper method that parses (x and y) parameters to return the
        // sim object that they target.
        private SimObjPhysics getTargetObject(float x, float y, bool forceAction) {
            if (x < 0 || x > 1 || y < 0 || y > 1) {
                throw new ArgumentOutOfRangeException("x/y must be in [0:1]");
            }

            // let's try picking up the object!
            SimObjPhysics target = null;

            // reverse the y so that the origin (0, 0) can be passed in as the top left of the screen
            y = 1.0f - y;

            // cast ray from screen coordinate into world space. If it hits an object
            Ray ray = m_Camera.ViewportPointToRay(new Vector3(x, y, 0.0f));
            RaycastHit hit;

            // if something was touched, actionFinished(true) always
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, 1 << 0 | 1 << 8 | 1 << 10, QueryTriggerInteraction.Ignore)) {
                if (hit.transform.GetComponent<SimObjPhysics>()) {
                    // wait! First check if the point hit is withing visibility bounds (camera viewport, max distance etc)
                    // this should basically only happen if the handDistance value is too big
                    if (forceAction || CheckIfTargetPositionIsInViewportRange(hit.point)) {
                        throw new InvalidOperationException($"Target sim object at screen coordinate: ({x}, {y}) is not within the viewport");
                    }

                    // it is within viewport, so we are good, assign as target
                    target = hit.transform.GetComponent<SimObjPhysics>();
                }
            }

            // try again, this time cast for placeable surface for things like countertops or interior of cabinets
            // if no target was found in the layers above, try the SimObjInvisible layer. 
            // additionally, if a target was found above, but that target was one of the SimObjPhysics Types that can have
            // PlaceableSurfaces on it, also make sure to check again
            if (target == null || hasPlaceableSurface.Contains(target.Type)) {
                if (Physics.Raycast(ray, out hit, Mathf.Infinity, 1 << 11, QueryTriggerInteraction.Ignore)) {
                    if (hit.transform.GetComponentInParent<SimObjPhysics>()) {
                        // wait! First check if the point hit is withing visibility bounds (camera viewport, max distance etc)
                        // this should basically only happen if the handDistance value is too big
                        if (forceAction || CheckIfTargetPositionIsInViewportRange(hit.point)) {
                            throw new InvalidOperationException($"Target sim object at screen coordinate: ({x}, {y}) is not within the viewport");
                        }
                        // it is within viewport, so we are good, assign as target
                        target = hit.transform.GetComponentInParent<SimObjPhysics>();
                    }
                }
            }

            // force update objects to be visible/interactable correctly
            VisibleSimObjs(forceVisible: false);
            return target;
        }

        public IEnumerator checkInitializeAgentLocationAction() {
            yield return null;

            Vector3 startingPosition = this.transform.position;
            // move ahead
            // move back

            float mult = 1 / gridSize;
            float grid_x1 = Convert.ToSingle(Math.Floor(this.transform.position.x * mult) / mult);
            float grid_z1 = Convert.ToSingle(Math.Floor(this.transform.position.z * mult) / mult);

            float[] xs = new float[] { grid_x1, grid_x1 + gridSize };
            float[] zs = new float[] { grid_z1, grid_z1 + gridSize };
            List<Vector3> validMovements = new List<Vector3>();

            foreach (float x in xs) {
                foreach (float z in zs) {
                    this.transform.position = startingPosition;
                    yield return null;

                    Vector3 target = new Vector3(x, this.transform.position.y, z);
                    Vector3 dir = target - this.transform.position;
                    Vector3 movement = dir.normalized * 100.0f;
                    if (movement.magnitude > dir.magnitude) {
                        movement = dir;
                    }

                    movement.y = Physics.gravity.y * this.m_GravityMultiplier;

                    m_CharacterController.Move(movement);

                    for (int i = 0; i < actionDuration; i++)
                    {
                        yield return null;
                        Vector3 diff = this.transform.position - target;


                        if ((Math.Abs(diff.x) < 0.005) && (Math.Abs(diff.z) < 0.005))
                        {
                            validMovements.Add(movement);
                            break;
                        }
                    }

                }
            }

            this.transform.position = startingPosition;
            yield return null;
            if (validMovements.Count > 0) {
                Debug.Log("Initialize: got total valid initial targets: " + validMovements.Count);
                Vector3 firstMove = validMovements[0];
                firstMove.y = Physics.gravity.y * this.m_GravityMultiplier;

                m_CharacterController.Move(firstMove);
                snapAgentToGrid();
                actionFinished(true, new InitializeReturn{
                    cameraNearPlane = m_Camera.nearClipPlane,
                    cameraFarPlane = m_Camera.farClipPlane
                });
            } else {
                Debug.Log("Initialize: no valid starting positions found");
                actionFinished(false);
            }
        }

        ///////////////////////////////////////////
        ////////// COLOR RANDOMIZATION ////////////
        ///////////////////////////////////////////

        [ObsoleteAttribute(message: "This action is deprecated. Call RandomizeColors instead.", error: false)] 
        public void ChangeColorOfMaterials() {
            RandomizeColors();
        }

        public void RandomizeColors() {
            ColorChanger colorChangeComponent = physicsSceneManager.GetComponent<ColorChanger>();
            colorChangeComponent.RandomizeColor();
            actionFinished(true);
        }

        public void ResetColors() {
            ColorChanger colorChangeComponent = physicsSceneManager.GetComponent<ColorChanger>();
            colorChangeComponent.ResetColors();
            actionFinished(true);
        }

        protected float distanceToObject(SimObjPhysics sop) {
            float dist = 10000.0f;
            foreach (Collider c in sop.GetComponentsInChildren<Collider>()) {
                Vector3 closestPoint = c.ClosestPointOnBounds(transform.position);
                Vector3 p0 = new Vector3(transform.position.x, 0f, transform.position.z);
                Vector3 p1 = new Vector3(closestPoint.x, 0f, closestPoint.z);
                dist = Math.Min(Vector3.Distance(p0, p1), dist);
            }
            return dist;
        }

        public void DistanceToObject(string objectId) {
            float dist = distanceToObject(physicsSceneManager.ObjectIdToSimObjPhysics[objectId]);
            #if UNITY_EDITOR
            Debug.Log(dist);
            #endif
            actionFinished(true, dist);
        }

        public bool CheckIfAgentCanMove(
            float moveMagnitude,
            int orientation,
            HashSet<Collider> ignoreColliders = null
        ) {
            Vector3 dir = new Vector3();

            switch (orientation) {
                case 0: //forward
                    dir = gameObject.transform.forward;
                    break;
                case 180: //backward
                    dir = -gameObject.transform.forward;
                    break;
                case 270: //left
                    dir = -gameObject.transform.right;
                    break;
                case 90: //right
                    dir = gameObject.transform.right;
                    break;
                default:
                    throw new ArgumentException("Incorrect orientation! Allowed orientations (0: forward, 90: right, 180: backward, 270: left).");
            }

            RaycastHit[] sweepResults = capsuleCastAllForAgent(
                GetComponent<CapsuleCollider>(),
                m_CharacterController.skinWidth,
                transform.position,
                dir,
                moveMagnitude,
                1 << 8 | 1 << 10
            );
            //check if we hit an environmental structure or a sim object that we aren't actively holding. If so we can't move
            if (sweepResults.Length > 0) {
                foreach (RaycastHit res in sweepResults) {
                    if (ignoreColliders != null && ignoreColliders.Contains(res.collider)) {
                        continue;
                    }

                    // Don't worry if we hit something thats in our hand.
                    if (ItemInHand != null && ItemInHand.transform == res.transform) {
                        continue;
                    }

                    if (res.transform.gameObject != this.gameObject && res.transform.GetComponent<PhysicsRemoteFPSAgentController>()) {

                        PhysicsRemoteFPSAgentController maybeOtherAgent = res.transform.GetComponent<PhysicsRemoteFPSAgentController>();
                        int thisAgentNum = agentManager.agents.IndexOf(this);
                        int otherAgentNum = agentManager.agents.IndexOf(maybeOtherAgent);
                        errorMessage = "Agent " + otherAgentNum.ToString() + " is blocking Agent " + thisAgentNum.ToString() + " from moving " + orientation;
                        return false;
                    }

                    //including "Untagged" tag here so that the agent can't move through objects that are transparent
                    if ((!collidersToIgnoreDuringMovement.Contains(res.collider)) && (
                            res.transform.GetComponent<SimObjPhysics>() ||
                            res.transform.tag == "Structure" ||
                            res.transform.tag == "Untagged"
                        )) {
                        int thisAgentNum = agentManager.agents.IndexOf(this);
                        errorMessage = res.transform.name + " is blocking Agent " + thisAgentNum.ToString() + " from moving " + orientation;
                        //the moment we find a result that is blocking, return false here
                        return false;
                    }
                }
            }
            return true;
        }

        ///////////////////////////////////////////
        //////////// HIDING OBJECTS ///////////////
        ///////////////////////////////////////////

        public void DisableObject(string objectId) {
            SimObjPhysics target = getTargetObject(objectId: objectId, forceAction: true);
            target.gameObject.SetActive(false);
            actionFinished(true);
        }

        public void EnableObject(string objectId) {
            SimObjPhysics target = getTargetObject(objectId: objectId, forceAction: true);
            target.gameObject.SetActive(true);
            actionFinished(true);
        }
        
        // remove a given sim object from the scene. Pass in the object's objectID string to remove it.
        public void RemoveFromScene(string objectId) {
            SimObjPhysics target = getTargetObject(objectId: objectId, forceAction: true);
            target.transform.gameObject.SetActive(false);
            physicsSceneManager.SetupScene();
            actionFinished(true);
        }

        // remove an array of sim objects from the scene.
        public void RemoveFromScene(string[] objectIds) {
            List<SimObjPhysics> objects = new List<SimObjPhysics>();

            // Make sure all the objectIds are valid!
            foreach (string objectId in objectIds) {
                SimObjPhysics target = getTargetObject(objectId: objectId, forceAction: true);
                objects.Add(target);
            }

            foreach (SimObjPhysics target in objects) {
                target.transform.gameObject.SetActive(false);
            }
            physicsSceneManager.SetupScene();
            actionFinished(true);
        }

        public void HideObject(string objectId) {
            SimObjPhysics sop = getTargetObject(objectId: objectId, forceAction: true);
            if (!ReceptacleRestrictions.SpawnOnlyOutsideReceptacles.Contains(sop.ObjType)) {
                foreach (SimObjPhysics containedSop in sop.SimObjectsContainedByReceptacle) {
                    updateDisplayGameObject(target: containedSop.gameObject, enabled: false);
                }
            }
            updateDisplayGameObject(target: sop.gameObject, enabled: false);
            sop.GetAllSimObjectsInReceptacleTriggersByObjectID();

            actionFinished(true);
        }

        public void UnhideObject(string objectId) {
            SimObjPhysics sop = getTargetObject(objectId: objectId, forceAction: true);
            if (!ReceptacleRestrictions.SpawnOnlyOutsideReceptacles.Contains(sop.ObjType)) {
                foreach (SimObjPhysics containedSop in sop.SimObjectsContainedByReceptacle) {
                    updateDisplayGameObject(target: containedSop.gameObject, enabled: true);
                }
            }
            updateDisplayGameObject(target: sop.gameObject, enabled: true);
            actionFinished(true);
        }

        protected void HideAll() {
            foreach (GameObject target in GameObject.FindObjectsOfType<GameObject>()) {
                updateDisplayGameObject(target: target, enabled: false);
            }
        }

        public void HideAllObjects() {
            HideAll();
            actionFinished(true);
        }

        protected void UnhideAll() {
            foreach (GameObject target in GameObject.FindObjectsOfType<GameObject>()) {
                updateDisplayGameObject(target: target, enabled: true);
            }
            // Making sure the agents visibility capsules are not incorrectly unhidden
            foreach (BaseFPSAgentController agent in this.agentManager.agents) {
                agent.IsVisible = agent.IsVisible;
            }
        }

        public void UnhideAllObjects() {
            transparentStructureObjectsHidden = false;
            UnhideAll();
            actionFinished(true);
        }

        [ObsoleteAttribute(message: "This action is deprecated. Call RemoveFromScene(string[] objectIds) instead.", error: false)] 
        public void RemoveObjsFromScene(string[] objectIds) {
            RemoveFromScene(objectIds: objectIds);
        }

        ///////////////////////////////////////////
        ////////// OBJECT TRANSPARENCY ////////////
        ///////////////////////////////////////////
        
        protected void changeObjectBlendMode(SimObjPhysics so, StandardShaderUtils.BlendMode bm, float alpha) {
            HashSet<MeshRenderer> renderersToSkip = new HashSet<MeshRenderer>();
            foreach (SimObjPhysics childSo in so.GetComponentsInChildren<SimObjPhysics>()) {
                if (!childSo.ObjectID.StartsWith("Drawer") &&
                    !childSo.ObjectID.Split('|') [0].EndsWith("Door") &&
                    so.ObjectID != childSo.ObjectID
                ) {
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

        public void MakeObjectTransparent(string objectId, float alpha = 0.4f) {
            changeObjectBlendMode(
                so: getTargetObject(objectId: objectId, forceAction: true),
                bm: StandardShaderUtils.BlendMode.Fade,
                alpha: alpha
            );
            actionFinished(true);
        }

        public void MakeObjectOpaque(string objectId) {
            changeObjectBlendMode(
                so: getTargetObject(objectId: objectId, forceAction: true),
                bm: StandardShaderUtils.BlendMode.Opaque,
                alpha: 1
            );
            actionFinished(true);
        }

        ///////////////////////////////////////////
        //////////// OBJECT MATERIALS /////////////
        ///////////////////////////////////////////

        private void setAllObjectsToMaterial(Material material, bool markActionFinished) {
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

            if (markActionFinished) {
                actionFinished(success: true);
            }
        }

        public void SetAllObjectsToBlueUnlit() {
            setAllObjectsToMaterial(material: (Material) Resources.Load("BLUE", typeof(Material)), markActionFinished: true);
        }

        public void SetAllObjectsToBlueStandard() {
            setAllObjectsToMaterial(material: (Material) Resources.Load("BLUE_standard", typeof(Material)), markActionFinished: true);
        }

        //Sweeptest to see if the object Agent is holding will prohibit movement
        public bool CheckIfItemBlocksAgentMovement(float moveMagnitude, int orientation, bool forceAction = false) {
            bool result = false;

            //if forceAction true, ignore collision restrictions caused by held objects
            if (forceAction)
            {
                return true;
            }
            //if there is nothing in our hand, we are good, return!
            if (ItemInHand == null) {
                result = true;
                //  Debug.Log("Agent has nothing in hand blocking movement");
                return result;
            }

            //otherwise we are holding an object and need to do a sweep using that object's rb
            else {
                Vector3 dir = new Vector3();

                //use the agent's forward as reference
                switch (orientation) {
                    case 0: //forward
                        dir = gameObject.transform.forward;
                        break;

                    case 180: //backward
                        dir = -gameObject.transform.forward;
                        break;

                    case 270: //left
                        dir = -gameObject.transform.right;
                        break;

                    case 90: //right
                        dir = gameObject.transform.right;
                        break;

                    default:
                        Debug.Log("Incorrect orientation input! Allowed orientations (0 - forward, 90 - right, 180 - backward, 270 - left) ");
                        break;
                }
                //otherwise we haev an item in our hand, so sweep using it's rigid body.
                //RaycastHit hit;

                Rigidbody rb = ItemInHand.GetComponent<Rigidbody>();

                RaycastHit[] sweepResults = rb.SweepTestAll(dir, moveMagnitude, QueryTriggerInteraction.Ignore);
                if (sweepResults.Length > 0) {
                    foreach (RaycastHit res in sweepResults) {
                        //did the item in the hand touch the agent? if so, ignore it's fine
                        if (res.transform.tag == "Player") {
                            result = true;
                            break;
                        } else {
                            errorMessage = res.transform.name + " is blocking the Agent from moving " + orientation + " with " + ItemInHand.name;
                            result = false;
                            return result;
                        }

                    }
                }

                //if the array is empty, nothing was hit by the sweeptest so we are clear to move
                else {
                    //Debug.Log("Agent Body can move " + orientation);
                    result = true;
                }

                return result;
            }
        }

        protected bool checkIfSceneBoundsContainTargetPosition(Vector3 position) {
            if (!agentManager.SceneBounds.Contains(position)) {
                errorMessage = "Scene bounds do not contain target position: " + position;
                return false;
            } else {
                return true;
            }
        }

        // if you want to do something like throw objects to knock over other objects, use this action to set all objects to Kinematic false
        // otherwise objects will need to be hit multiple times in order to ensure kinematic false toggle
        // use this by initializing the scene, then calling randomize if desired, and then call this action to prepare the scene so all objects will react to others upon collision.
        // note that SOMETIMES rigidbodies will continue to jitter or wiggle, especially if they are stacked against other rigidbodies.
        // this means that the isSceneAtRest bool will always be false
        public void MakeAllObjectsMoveable() {
            foreach (SimObjPhysics sop in GameObject.FindObjectsOfType<SimObjPhysics>()) {
                //check if the sopType is something that can be hung
                if (sop.Type == SimObjType.Towel || sop.Type == SimObjType.HandTowel || sop.Type == SimObjType.ToiletPaper) {
                    //if this object is actively hung on its corresponding object specific receptacle... skip it so it doesn't fall on the floor
                    if (sop.GetComponentInParent<ObjectSpecificReceptacle>()) {
                        continue;
                    }
                }

                if (sop.PrimaryProperty == SimObjPrimaryProperty.CanPickup || sop.PrimaryProperty == SimObjPrimaryProperty.Moveable) {
                    Rigidbody rb = sop.GetComponent<Rigidbody>();
                    rb.isKinematic = false;
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                }
            }
            actionFinished(true);
        }

        // This does not appear to be used except for by the Python unit test
        [ObsoleteAttribute(message: "This action is deprecated. Call RotateRight/Left and LookUp/Down instead.", error: false)] 
        public void RotateLook(Vector3 rotation, float horizon) {
            // TODO: why is the base setting x=0.0f and z=0.0f?
            transform.rotation = Quaternion.Euler(new Vector3(0.0f, rotation.y, 0.0f));
            m_Camera.transform.localEulerAngles = new Vector3(horizon, 0.0f, 0.0f);
            actionFinished(true);
        }

        // Check if agent is collided with other objects
        protected bool IsCollided() {
            return collisionsInAction.Count > 0;
        }

        public virtual SimpleSimObj[] allSceneObjects() {
            return GameObject.FindObjectsOfType<SimObj>();
        }

        ///////////////////////////////////////////
        //////////////// METADATA /////////////////
        ///////////////////////////////////////////

        public void ResetObjectFilter() {
            this.simObjFilter = null;
            actionFinishedEmit(true);
        }
        public void SetObjectFilter(string[] objectIds) {
            SimObjPhysics[] simObjects = GameObject.FindObjectsOfType<SimObjPhysics>();
            HashSet<SimObjPhysics> filter = new HashSet<SimObjPhysics>();
            HashSet<string> filterObjectIds = new HashSet<string>(objectIds);
            foreach (var simObj in simObjects) {
                if (filterObjectIds.Contains(simObj.ObjectID)) {
                    filter.Add(simObj);
                }
            }
            simObjFilter = filter.ToArray();
            actionFinishedEmit(true);
        }

        public virtual ObjectMetadata[] generateObjectMetadata() {
            SimObjPhysics[] simObjects = null;
            if (this.simObjFilter != null) {
                simObjects = this.simObjFilter;
            } else {
                simObjects = GameObject.FindObjectsOfType<SimObjPhysics>();
            }

            HashSet<SimObjPhysics> visibleSimObjsHash = new HashSet<SimObjPhysics>(GetAllVisibleSimObjPhysics(
                this.m_Camera,
                this.maxVisibleDistance));

            int numObj = simObjects.Length;
            List<ObjectMetadata> metadata = new List<ObjectMetadata>();
            Dictionary<string, List<string>> parentReceptacles = new Dictionary<string, List<string>>();

            #if UNITY_EDITOR
                //debug draw bounds reset list
                gizmobounds.Clear();
            #endif

            for (int k = 0; k < numObj; k++) {
                SimObjPhysics simObj = simObjects[k];
                ObjectMetadata meta = ObjectMetadataFromSimObjPhysics(simObj, visibleSimObjsHash.Contains(simObj));
                if (meta.receptacle) {
                    
                    List<string> containedObjectsAsID = new List<String>();
                    foreach (GameObject go in simObj.ContainedGameObjects())
                    {
                        containedObjectsAsID.Add(go.GetComponent<SimObjPhysics>().ObjectID);
                    }
                    List<string> roid = containedObjectsAsID;//simObj.Contains();

                    foreach (string oid in roid) {
                        if (!parentReceptacles.ContainsKey(oid)) {
                            parentReceptacles[oid] = new List<string>();
                        }
                        parentReceptacles[oid].Add(simObj.ObjectID);
                    }
                    meta.receptacleObjectIds = roid.ToArray();
                }
                meta.distance = Vector3.Distance(transform.position, simObj.gameObject.transform.position);
                metadata.Add(meta);
            }
            foreach (ObjectMetadata meta in metadata) {
                if (parentReceptacles.ContainsKey(meta.objectId)) {
                    meta.parentReceptacles = parentReceptacles[meta.objectId].ToArray();
                }
            }
            return metadata.ToArray();
		}

        // generates object metadata based on sim object's properties
        public virtual ObjectMetadata ObjectMetadataFromSimObjPhysics(SimObjPhysics simObj, bool isVisible) {            
            ObjectMetadata objMeta = new ObjectMetadata();
            GameObject o = simObj.gameObject;
            objMeta.name = o.name;
            objMeta.position = o.transform.position;
            objMeta.rotation = o.transform.eulerAngles;
            objMeta.objectType = Enum.GetName(typeof(SimObjType), simObj.Type);
            objMeta.receptacle = simObj.IsReceptacle;

            objMeta.openable = simObj.IsOpenable;
            if (objMeta.openable) {
                objMeta.isOpen = simObj.IsOpen;
                objMeta.openness = simObj.openness;
            }

            objMeta.toggleable = simObj.IsToggleable;
            if (objMeta.toggleable) {
                objMeta.isToggled = simObj.IsToggled;
            }

            objMeta.breakable = simObj.IsBreakable;
            if (objMeta.breakable) {
                objMeta.isBroken = simObj.IsBroken;
            }

            objMeta.canFillWithLiquid = simObj.IsFillable;
            if (objMeta.canFillWithLiquid) {
                objMeta.isFilledWithLiquid = simObj.IsFilled;
            }

            objMeta.dirtyable = simObj.IsDirtyable;
            if (objMeta.dirtyable) {
                objMeta.isDirty = simObj.IsDirty;
            }

            objMeta.cookable = simObj.IsCookable;
            if (objMeta.cookable) {
                objMeta.isCooked = simObj.IsCooked;
            }

            //if the sim object is moveable or pickupable
            if (simObj.IsPickupable || simObj.IsMoveable || simObj.salientMaterials.Length > 0) {
                // this object should report back mass and salient materials
                string [] salientMaterialsToString = new string [simObj.salientMaterials.Length];

                for (int i = 0; i < simObj.salientMaterials.Length; i++) {
                    salientMaterialsToString[i] = simObj.salientMaterials[i].ToString();
                }

                objMeta.salientMaterials = salientMaterialsToString;

                //this object should also report back mass since it is moveable/pickupable
                objMeta.mass = simObj.Mass;
            }

            // can this object change others to hot?
            objMeta.canChangeTempToHot = simObj.canChangeTempToHot;

            // can this object change others to cold?
            objMeta.canChangeTempToCold = simObj.canChangeTempToCold;

            // placeholder for heatable objects -kettle, pot, pan
            // objMeta.abletocook = simObj.abletocook;
            // if (objMeta.abletocook) {
            //     objMeta.isReadyToCook = simObj.IsHeated;
            // }

            objMeta.sliceable = simObj.IsSliceable;
            if (objMeta.sliceable) {
                objMeta.isSliced = simObj.IsSliced;
            }

            objMeta.canBeUsedUp = simObj.CanBeUsedUp;
            if (objMeta.canBeUsedUp) {
                objMeta.isUsedUp = simObj.IsUsedUp;
            }

            //object temperature to string
            objMeta.ObjectTemperature = simObj.CurrentObjTemp.ToString();

            objMeta.pickupable = simObj.IsPickupable;
            objMeta.isPickedUp = simObj.isPickedUp;//returns true for if this object is currently being held by the agent

            objMeta.moveable = simObj.IsMoveable;

            objMeta.objectId = simObj.ObjectID;

            // TODO: using the isVisible flag on the object causes weird problems
            // in the multiagent setting, explicitly giving this information for now.
            objMeta.visible = isVisible; //simObj.isVisible;

            objMeta.obstructed = !simObj.isInteractable;//if object is not interactable, it means it is obstructed

            objMeta.isMoving = simObj.inMotion;//keep track of if this object is actively moving


            objMeta.objectOrientedBoundingBox = simObj.ObjectOrientedBoundingBox;
            
            objMeta.axisAlignedBoundingBox = simObj.AxisAlignedBoundingBox;

            return objMeta;
        }

        public SceneBounds GenerateSceneBounds(Bounds bounding) {
            SceneBounds b = new SceneBounds();
            List<float[]> cornerPoints = new List<float[]>();
            float[] xs = new float[]{
                bounding.center.x + bounding.size.x/2f,
                bounding.center.x - bounding.size.x/2f
            };
            float[] ys = new float[]{
                bounding.center.y + bounding.size.y/2f,
                bounding.center.y - bounding.size.y/2f
            };
            float[] zs = new float[]{
                bounding.center.z + bounding.size.z/2f,
                bounding.center.z - bounding.size.z/2f
            };
            foreach (float x in xs) {
                foreach (float y in ys) {
                    foreach (float z in zs) {
                        cornerPoints.Add(new float[]{x, y, z});
                    }
                }
            }
            b.cornerPoints = cornerPoints.ToArray();

            b.center = bounding.center;
            b.size = bounding.size;
            
            return b;
        }

		public virtual  MetadataPatch generateMetadataPatch() {
            MetadataPatch patch = new MetadataPatch();
            patch.lastAction = this.lastAction;
            patch.lastActionSuccess = this.lastActionSuccess;
            patch.actionReturn = this.actionReturn;
            if (errorCode != ServerActionErrorCode.Undefined) {
                patch.errorCode = Enum.GetName(typeof(ServerActionErrorCode), errorCode);
            }
            patch.errorMessage = this.errorMessage;
            return patch;
        }

		public virtual MetadataWrapper generateMetadataWrapper() {
            // AGENT METADATA
            AgentMetadata agentMeta = new AgentMetadata();
            agentMeta.name = "agent";
            agentMeta.position = transform.position;
            agentMeta.rotation = transform.eulerAngles;
            agentMeta.cameraHorizon = m_Camera.transform.rotation.eulerAngles.x;
            if (agentMeta.cameraHorizon > 180) 
            {
                agentMeta.cameraHorizon -= 360;
            }
	        agentMeta.isStanding = (m_Camera.transform.localPosition - standingLocalCameraPosition).magnitude < 0.1f;
            agentMeta.inHighFrictionArea = inHighFrictionArea;

            // OTHER METADATA
            MetadataWrapper metaMessage = new MetadataWrapper();
            metaMessage.agent = agentMeta;
            metaMessage.sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            metaMessage.objects = this.generateObjectMetadata();
            metaMessage.isSceneAtRest = physicsSceneManager.isSceneAtRest;
            metaMessage.sceneBounds = GenerateSceneBounds(agentManager.SceneBounds);
            metaMessage.collided = collidedObjects.Length > 0;
            metaMessage.collidedObjects = collidedObjects;
            metaMessage.screenWidth = Screen.width;
            metaMessage.screenHeight = Screen.height;
            metaMessage.cameraPosition = m_Camera.transform.position;
            metaMessage.cameraOrthSize = cameraOrthSize;
            cameraOrthSize = -1f;
            metaMessage.fov = m_Camera.fieldOfView;
            metaMessage.lastAction = lastAction;
            metaMessage.lastActionSuccess = lastActionSuccess;
            metaMessage.errorMessage = errorMessage;
            metaMessage.actionReturn = this.actionReturn;

            if (errorCode != ServerActionErrorCode.Undefined) {
                metaMessage.errorCode = Enum.GetName(typeof(ServerActionErrorCode), errorCode);
            }

            List<InventoryObject> ios = new List<InventoryObject>();

            if (ItemInHand != null) {
                SimObjPhysics so = ItemInHand.GetComponent<SimObjPhysics>();
                InventoryObject io = new InventoryObject();
                io.objectId = so.ObjectID;
                io.objectType = Enum.GetName(typeof(SimObjType), so.Type);
                ios.Add(io);
            }

            metaMessage.inventoryObjects = ios.ToArray();

            // HAND
            metaMessage.hand = new HandMetadata();
            metaMessage.hand.position = AgentHand.transform.position;
            metaMessage.hand.localPosition = AgentHand.transform.localPosition;
            metaMessage.hand.rotation = AgentHand.transform.eulerAngles;
            metaMessage.hand.localRotation = AgentHand.transform.localEulerAngles;

            // EXTRAS
            metaMessage.reachablePositions = reachablePositions;
            metaMessage.flatSurfacesOnGrid = UtilityFunctions.flatten3DimArray(flatSurfacesOnGrid);
            metaMessage.distances = UtilityFunctions.flatten2DimArray(distances);
            metaMessage.normals = UtilityFunctions.flatten3DimArray(normals);
            metaMessage.isOpenableGrid = UtilityFunctions.flatten2DimArray(isOpenableGrid);
            metaMessage.segmentedObjectIds = segmentedObjectIds;
            metaMessage.objectIdsInBox = objectIdsInBox;
            metaMessage.actionIntReturn = actionIntReturn;
            metaMessage.actionFloatReturn = actionFloatReturn;
            metaMessage.actionFloatsReturn = actionFloatsReturn;
            metaMessage.actionStringsReturn = actionStringsReturn;
            metaMessage.actionVector3sReturn = actionVector3sReturn;

            if (alwaysReturnVisibleRange) {
                metaMessage.visibleRange = visibleRange();
            }

            //test time
            metaMessage.currentTime = Time.time;

            // Resetting things
            reachablePositions = new Vector3[0];
            flatSurfacesOnGrid = new float[0, 0, 0];
            distances = new float[0, 0];
            normals = new float[0, 0, 0];
            isOpenableGrid = new bool[0, 0];
            segmentedObjectIds = new string[0];
            objectIdsInBox = new string[0];
            actionIntReturn = 0;
            actionFloatReturn = 0.0f;
            actionFloatsReturn = new float[0];
            actionStringsReturn = new string[0];
            actionVector3sReturn = new Vector3[0];

            return metaMessage;
		}

		public void updateImageSynthesis(bool status) {
            if (this.imageSynthesis == null) {
                imageSynthesis = this.gameObject.GetComponentInChildren<ImageSynthesis> () as ImageSynthesis;
            }
			imageSynthesis.enabled = status;
		}

        ///////////////////////////////////////////
        //////// PROCESS CONTROL COMMAND //////////
        ///////////////////////////////////////////

        // This should only be used by DebugInputField and HideNSeekController
        // Once all those invocations have been converted to Dictionary<string, object>
        // this can be removed
        public void ProcessControlCommand(ServerAction controlCommand) {
            errorMessage = "";
            errorCode = ServerActionErrorCode.Undefined;
            collisionsInAction = new List<string>();

            lastAction = controlCommand.action;
            lastActionSuccess = false;
            lastPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
			System.Reflection.MethodInfo method = this.GetType().GetMethod(controlCommand.action);
			
            this.agentState = AgentState.Processing;
			try {
				if (method == null) {
					errorMessage = "Invalid action: " + controlCommand.action;
					errorCode = ServerActionErrorCode.InvalidAction;
					Debug.LogError(errorMessage);
					actionFinished(false);
				} else {
					method.Invoke(this, new object[] { controlCommand });
				}
			} catch (Exception e) {
				Debug.LogError("Caught error with invoke for action: " + controlCommand.action);
                Debug.LogError("Action error message: " + errorMessage);
				Debug.LogError(e);

				errorMessage += e.ToString();
				actionFinished(false);
			}
        }

        // the parameter name is different to avoid failing a test
        // that looks for methods with identical param names, since
        // we dispatch using method + param names
        public void ProcessControlCommand(Dictionary<string, object> actionDict) {
            ProcessControlCommand(new DynamicServerAction(actionDict));
        }

        public void ProcessControlCommand(DynamicServerAction controlCommand) {
            errorMessage = "";
            errorCode = ServerActionErrorCode.Undefined;
            collisionsInAction = new List<string>();

            lastAction = controlCommand.action;
            lastActionSuccess = false;
            lastPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
            this.agentState = AgentState.Processing;

            try {
                ActionDispatcher.Dispatch(this, controlCommand);
            } catch (MissingArgumentsActionException e) {
                errorMessage = "action: " + controlCommand.action + " is missing the following arguments: " + string.Join(",", e.ArgumentNames.ToArray());
                errorCode = ServerActionErrorCode.MissingArguments;
                actionFinished(false);
            } catch (AmbiguousActionException e) {
                errorMessage = "Ambiguous action: " + controlCommand.action + " " + e.Message;
                errorCode = ServerActionErrorCode.AmbiguousAction;
                actionFinished(false);
            } catch (InvalidActionException) {
                errorCode = ServerActionErrorCode.InvalidAction;
                actionFinished(success: false, errorMessage: "Invalid action: " + controlCommand.action);
            } catch (TargetInvocationException e) {
                // TargetInvocationException is called whenever an action
                // throws an exception. It is used to short circuit errors,
                // which terminates the action immediately.
                actionFinished(
                    success: false,
                    errorMessage: $"{e.InnerException.GetType().Name}: {e.InnerException.Message}"
                );
            } catch (Exception e) {
                Debug.LogError("Caught error with invoke for action: " + controlCommand.action);
                Debug.LogError("Action error message: " + errorMessage);
                errorMessage += e.ToString();
                actionFinished(false);
            }

            #if UNITY_EDITOR
                if (errorMessage != "") {
                    Debug.LogError(errorMessage);
                }
            #endif
        }

        ///////////////////////////////////////////
        ////////////// DO NOTHING /////////////////
        ///////////////////////////////////////////

        // a no op action used to return metadata via actionFinished call,
        // but not actually doing anything to interact with the scene or manipulate the Agent
        public void NoOp() {
            actionFinished(true);
        }

        public void Pass() {
            NoOp();
        }

        public void Done() {
            NoOp();
        }

		// Handle collisions - CharacterControllers don't apply physics innately, see "PushMode" check below
        // XXX: this will be used for truly continuous movement over time, for now this is unused
		protected void OnControllerColliderHit(ControllerColliderHit hit) {
			if (!enabled) {
				return;
			}

			if (hit.gameObject.GetComponent<StructureObject>() &&
                hit.gameObject.GetComponent<StructureObject>().WhatIsMyStructureObjectTag == StructureObjectTag.Floor
            ) {
				return;
			}


			if (!collisionsInAction.Contains(hit.gameObject.name)) {
				collisionsInAction.Add(hit.gameObject.name);
			}

			Rigidbody body = hit.collider.attachedRigidbody;
			// don't move the rigidbody if the character is on top of it
			if (m_CollisionFlags == CollisionFlags.Below) {
				return;
			}

			if (body == null || body.isKinematic) {
				return;
			}

			// push objects out of the way if moving through them and they are Moveable or CanPickup (Physics)
			if (PushMode) {
				float pushPower = 2.0f;
				Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
				body.velocity = pushDir * pushPower;
			}

			//if we touched something with a rigidbody that needs to simulate physics, generate a force at the impact point
			//body.AddForce(m_CharacterController.velocity * 15f, ForceMode.Force);
			//body.AddForceAtPosition (m_CharacterController.velocity * 15f, hit.point, ForceMode.Acceleration);//might have to adjust the force vector scalar later
		}

        ///////////////////////////////////////////
        ////////////////// MOVE ///////////////////
        ///////////////////////////////////////////
        
        // for all translational movement, check if the item the player is holding will hit anything, or if the agent will hit anything
        // NOTE: (XXX) All four movements below no longer use base character controller Move() due to doing initial collision blocking
        // checks before actually moving. Previously we would moveCharacter() first and if we hit anything reset, but now to match
        // Luca's movement grid and valid position generation, simple transform setting is used for movement instead.

        // XXX revisit what movement means when we more clearly define what "continuous" movement is
        protected void moveInDirection(
            Vector3 direction,
            string objectId = "",
            float maxDistanceToObject = -1.0f,
            bool forceAction = false,
            bool manualInteract = false,
            HashSet<Collider> ignoreColliders = null
        ) {
            Vector3 targetPosition = transform.position + direction;
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

            //only default hand if not manually interacting with things    
            if (!manualInteract) {
                DefaultAgentHand();
            }

            // TODO: these things should go on their own method...
            Vector3 oldPosition = transform.position;
            transform.position = targetPosition;
            this.snapAgentToGrid();

            if (objectId != "" && maxDistanceToObject > 0.0f) {
                SimObjPhysics sop;
                try {
                    sop = getTargetObject(objectId: objectId, forceAction: true);
                } catch (Exception e) {
                    transform.position = oldPosition; 
                    throw new ArgumentException(e.Message);
                }

                if (distanceToObject(sop) > maxDistanceToObject) {
                    transform.position = oldPosition;
                    throw new InvalidOperationException("Agent movement would bring it beyond the max distance of " + objectId);
                }
            }
        }

		protected void snapAgentToGrid() {
            if (this.snapToGrid) {
                float mult = 1 / gridSize;
                float gridX = Convert.ToSingle(Math.Round(this.transform.position.x * mult) / mult);
                float gridZ = Convert.ToSingle(Math.Round(this.transform.position.z * mult) / mult);

                this.transform.position = new Vector3(gridX, transform.position.y, gridZ);
            }
		}

        // Allows you to move in both the x/z directions at once, instead of calling
        // MoveAhead and MoveRight.
        public virtual void MoveRelative(float x, float z) {
            var moveLocal = new Vector3(x, 0, z);
            Vector3 moveWorldSpace = transform.rotation * moveLocal;
            moveWorldSpace.y = Physics.gravity.y * this.m_GravityMultiplier;
			m_CharacterController.Move(moveWorldSpace);
			actionFinished(true);
        }

        ///////////////////////////////////////////
        ///////////////// ROTATE //////////////////
        ///////////////////////////////////////////

		// free rotate, change forward facing of Agent
        // this is currently overwritten by Rotate in Stochastic Controller
		public virtual void Rotate(Vector3 rotation) {
            // TODO: why is x=0 and z=0 set in the base?
			transform.rotation = Quaternion.Euler(new Vector3(0.0f, response.rotation.y, 0.0f));
			actionFinished(true);
		}

		// rotates controlCommand.degrees degrees left w/ respect to current forward
		public virtual void RotateLeft(float degrees) {
            transform.Rotate(0, degrees == null ? -rotateStepDegrees : -1 * (float) degrees, 0);
			actionFinished(true);
		}

		// rotates controlCommand.degrees degrees right w/ respect to current forward
		public virtual void RotateRight(float? degrees = null) {
            transform.Rotate(0, degrees == null ? rotateStepDegrees : (float) degrees, 0);
			actionFinished(true);
		}

        ///////////////////////////////////////////
        ////////////// LOOK UP/DOWN ///////////////
        ///////////////////////////////////////////

		// iterates to next allowed downward horizon angle for AgentCamera (max 60 degrees down)
		public virtual void LookDown(float degrees) {
			m_Camera.transform.Rotate(degrees, 0, 0);
			actionFinished(true);
		}

		// iterates to next allowed upward horizon angle for agent camera (max 30 degrees up)
		public virtual void LookUp(float degrees) {
			m_Camera.transform.Rotate(degrees, 0, 0);
			actionFinished(true);
		}

        ///////////////////////////////////////////
        //////////////// TELEPORT /////////////////
        ///////////////////////////////////////////

        // teleport full, base version does not consider being able to hold objects
        // XXX: does teleport full even make sense in the base? what about the drone
        // or some other robot that has 10 degrees of freedom. When will this ever
        // be used?
        public virtual void TeleportFull(ServerAction action) {
            targetTeleport = new Vector3(action.x, action.y, action.z);

            if (action.forceAction) {
                DefaultAgentHand();
                transform.position = targetTeleport;
                // TODO: why are X/Z set to 0 in the base?
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
                Vector3 oldCameraLocalEulerAngle = m_Camera.transform.localEulerAngles;
                Vector3 oldCameraLocalPosition = m_Camera.transform.localPosition;

                //DefaultAgentHand(action);
                transform.position = targetTeleport;

                //apply gravity after teleport so we aren't floating in the air
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

                if (agentCollides) {
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

        public virtual void Teleport(ServerAction action) {
            action.horizon = Convert.ToInt32(m_Camera.transform.localEulerAngles.x);
            if (!action.rotateOnTeleport) {
                action.rotation = transform.eulerAngles;
            }
            TeleportFull(action);
        }

        protected List<Vector3> visibleRange() {
            int n = 5;
            List<Vector3> points = new List<Vector3>();
            points.Add(transform.position);
            updateAllAgentCollidersForVisibilityCheck(false);
            for (int i = 0; i < n; i++) {
                for (int j = 0; j < n; j++) {
                    RaycastHit hit;
                    Ray ray = m_Camera.ViewportPointToRay(new Vector3(
                        (i + 0.5f) / n, (j + 0.5f) / n, 0.0f));
                    if (Physics.Raycast(ray, out hit, 100f, (1 << 8) | (1 << 10))) {
                        points.Add(hit.point);
                    }
                }
            }
            updateAllAgentCollidersForVisibilityCheck(true);
            return points;
        }

        //*** Maybe make this better */
        // This function should be called before and after doing a visibility check (before with
        // enableColliders == false and after with it equaling true). It, in particular, will
        // turn off/on all the colliders on agents which should not block visibility for the current agent
        // (invisible agents for example).
        protected void updateAllAgentCollidersForVisibilityCheck(bool enableColliders) {
            foreach (BaseFPSAgentController agent in this.agentManager.agents) {
                bool overlapping = (transform.position - agent.transform.position).magnitude < 0.001f;
                if (overlapping || agent == this || !agent.IsVisible) {
                    foreach (Collider c in agent.GetComponentsInChildren<Collider>()) {
                        if (ItemInHand == null || !hasAncestor(c.transform.gameObject, ItemInHand)) {
                            c.enabled = enableColliders;
                        }
                    }
                }
            }
        }

        ///////////////////////////////////////////
        /////////// HIERARCHY HELPERS /////////////
        ///////////////////////////////////////////

        protected bool hasAncestor(GameObject child, GameObject potentialAncestor) {
            if (child == potentialAncestor) {
                return true;
            } else if (child.transform.parent != null) {
                return hasAncestor(child.transform.parent.gameObject, potentialAncestor);
            } else {
                return false;
            }
        }

        protected bool ancestorHasName(GameObject go, string name) {
            if (go.name == name) {
                return true;
            } else if (go.transform.parent != null) {
                return ancestorHasName(go.transform.parent.gameObject, name);
            } else {
                return false;
            }
        }

        protected static SimObjPhysics ancestorSimObjPhysics(GameObject go) {
            if (go == null) {
                return null;
            }
            SimObjPhysics so = go.GetComponent<SimObjPhysics>();
            if (so != null) {
                return so;
            } else if (go.transform.parent != null) {
                return ancestorSimObjPhysics(go.transform.parent.gameObject);
            } else {
                return null;
            }
        }

        ///////////////////////////////////////////
        /////////////// VISIBILITY ////////////////
        ///////////////////////////////////////////

        public void VisibleRange() {
            actionFinished(true, visibleRange());
        }

        private bool isSimObjVisible(Camera agentCamera, SimObjPhysics sop, float maxDistance, Plane[] planes) {
            bool visible = false;
            //check against all visibility points, accumulate count. If at least one point is visible, set object to visible
            if (sop.VisibilityPoints != null && sop.VisibilityPoints.Length > 0) {
                Transform[] visPoints = sop.VisibilityPoints;
                int visPointCount = 0;

                foreach (Transform point in visPoints) {
                    bool outsidePlane = false;
                    for (int i = 0; i < planes.Length; i++) {
                        if (!planes[i].GetSide(point.position)) {
                            outsidePlane = true;
                            break;
                        }
                    }

                    if (outsidePlane) {
                        continue;
                    }

                    float xdelta = Math.Abs(this.transform.position.x - point.position.x);
                    if (xdelta > maxDistance) {
                        continue;
                    }

                    float zdelta = Math.Abs(this.transform.position.z - point.position.z);
                    if (zdelta > maxDistance) {
                        continue;
                    }

                    // if the object is too far above the Agent, skip
                    float ydelta =  point.position.y - this.transform.position.y;
                    if (ydelta > maxDistance) {
                        continue;
                    }

                    double distance = Math.Sqrt((xdelta * xdelta) + (zdelta * zdelta));
                    if (distance > maxDistance) {
                        continue;
                    }

                    //if this particular point is in view...
                    if (CheckIfVisibilityPointRaycast(sop, point, agentCamera, false) ||
                        CheckIfVisibilityPointRaycast(sop, point, agentCamera, true))
                    {
                        visPointCount++;
                        #if !UNITY_EDITOR
                            // If we're in the unity editor then don't break on finding a visible
                            // point as we want to draw lines to each visible point.
                            break;
                        #endif
                    }
                }

                //if we see at least one vis point, the object is "visible"
                if (visPointCount > 0) {
                    #if UNITY_EDITOR
                        sop.isVisible = true;
                    #endif
                    visible = true;
                }
            } else {
                Debug.Log("Error! Set at least 1 visibility point on SimObjPhysics " + sop + ".");
            }
            return visible;
        }

        public SimObjPhysics[] VisibleSimObjs(string objectId, bool forceVisible = false) {
            ServerAction action = new ServerAction();
            action.objectId = objectId;
            action.forceVisible = forceVisible;
            return VisibleSimObjs(action);

            List<SimObjPhysics> simObjs = new List<SimObjPhysics>();

            // go through array of sim objects visible to the camera
            foreach (SimObjPhysics so in VisibleSimObjs(forceVisible)) {
                if (!string.IsNullOrEmpty(objectId) && objectId != so.ObjectID) {
                    continue;
                }
                simObjs.Add(so);
            }

            return simObjs.ToArray();
        }

        //pass in forceVisible bool to force grab all objects of type sim obj
        //if not, gather all visible sim objects maxVisibleDistance away from camera view
        public SimObjPhysics[] VisibleSimObjs(bool forceVisible) {
            if (forceVisible) {
                return GameObject.FindObjectsOfType(typeof(SimObjPhysics)) as SimObjPhysics[];
            } else {
                return GetAllVisibleSimObjPhysics(m_Camera, maxVisibleDistance);
            }
        }

        protected SimObjPhysics[] GetAllVisibleSimObjPhysics(Camera agentCamera, float maxDistance) {
            if (this.visibilityScheme == VisibilityScheme.Collider) {
                return GetAllVisibleSimObjPhysicsCollider(agentCamera, maxDistance);
            } else {
                return GetAllVisibleSimObjPhysicsDistance(agentCamera, maxDistance);
            }
        }

        // this is a faster version of the visibility check, but is not entirely
        // consistent with the collider based method.  In particular, if an object
        // is within range of the maxVisibleDistance, but obscurred only within this
        // range and is visibile outside of the range, it will get reported as invisible
        // by the new scheme, but visible in the current scheme.
        private SimObjPhysics[] GetAllVisibleSimObjPhysicsDistance(Camera agentCamera, float maxDistance) {
            List<SimObjPhysics> visible = new List<SimObjPhysics>();
            IEnumerable<SimObjPhysics> simObjs = null;
            if (this.simObjFilter != null) {
                simObjs = this.simObjFilter;
            } else {
                // this is faster than doing GameObject.FindObjectsOfType and is kept consistent when new objects are added
                simObjs = physicsSceneManager.ObjectIdToSimObjPhysics.Values;
            }

            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(agentCamera);
            foreach (var sop in simObjs) {
                if (isSimObjVisible(agentCamera, sop, this.maxVisibleDistance, planes)) {
                    visible.Add(sop);
                }
            }
            return visible.ToArray();
        }

        private SimObjPhysics[] GetAllVisibleSimObjPhysicsCollider(Camera agentCamera, float maxDistance) {
            List<SimObjPhysics> currentlyVisibleItems = new List<SimObjPhysics>();

            #if UNITY_EDITOR
                foreach (KeyValuePair<string, SimObjPhysics> pair in physicsSceneManager.ObjectIdToSimObjPhysics) {
                    // Set all objects to not be visible
                    pair.Value.isVisible = false;
                }
            #endif

            HashSet<SimObjPhysics> filter = null;
            if (this.simObjFilter != null) {
                filter = new HashSet<SimObjPhysics>(this.simObjFilter);
                if (filter.Count == 0) {
                    return currentlyVisibleItems.ToArray();
                }
            }

            Vector3 agentCameraPos = agentCamera.transform.position;

            //get all sim objects in range around us that have colliders in layer 8 (visible), ignoring objects in the SimObjInvisible layer
            //this will make it so the receptacle trigger boxes don't occlude the objects within them.
            CapsuleCollider agentCapsuleCollider = GetComponent<CapsuleCollider>();
            Vector3 point0, point1;
            float radius;
            agentCapsuleCollider.ToWorldSpaceCapsule(out point0, out point1, out radius);
            if (point0.y <= point1.y) {
                point1.y += maxDistance;
            } else {
                point0.y += maxDistance;
            }

            // Turn off the colliders corresponding to this agent
            // and any invisible agents.
            updateAllAgentCollidersForVisibilityCheck(false);

            Collider[] colliders_in_view = Physics.OverlapCapsule(point0, point1, maxDistance, 1 << 8, QueryTriggerInteraction.Collide);

            if (colliders_in_view != null) {
                HashSet<SimObjPhysics> testedSops = new HashSet<SimObjPhysics>();
                foreach (Collider item in colliders_in_view) {
                    SimObjPhysics sop = ancestorSimObjPhysics(item.gameObject);
                    //now we have a reference to our sim object
                    if ((sop != null && !testedSops.Contains(sop)) && (filter == null || filter.Contains(sop))) {
                        testedSops.Add(sop);
                        //check against all visibility points, accumulate count. If at least one point is visible, set object to visible
                        if (sop.VisibilityPoints != null && sop.VisibilityPoints.Length > 0) {
                            Transform[] visPoints = sop.VisibilityPoints;
                            int visPointCount = 0;

                            foreach (Transform point in visPoints) {
                                //if this particular point is in view...
                                if (CheckIfVisibilityPointInViewport(sop, point, agentCamera, false)) {
                                    visPointCount++;
                                    #if !UNITY_EDITOR
                                        // If we're in the unity editor then don't break on finding a visible
                                        // point as we want to draw lines to each visible point.
                                        break;
                                    #endif
                                }
                            }

                            //if we see at least one vis point, the object is "visible"
                            if (visPointCount > 0) {
                                #if UNITY_EDITOR
                                    sop.isVisible = true;
                                #endif
                                if (!currentlyVisibleItems.Contains(sop)) {
                                    currentlyVisibleItems.Add(sop);
                                }
                            }
                        } else {
                            Debug.Log("Error! Set at least 1 visibility point on SimObjPhysics " + sop + ".");
                        }
                    }
                }
            }

            //check against anything in the invisible layers that we actually want to have occlude things in this round.
            //normally receptacle trigger boxes must be ignored from the visibility check otherwise objects inside them will be occluded, but
            //this additional check will allow us to see inside of receptacle objects like cabinets/fridges by checking for that interior
            //receptacle trigger box. Oh boy!
            Collider[] invisible_colliders_in_view = Physics.OverlapCapsule(point0, point1, maxDistance, 1 << 9, QueryTriggerInteraction.Collide);

            if (invisible_colliders_in_view != null) {
                foreach (Collider item in invisible_colliders_in_view) {
                    if (item.tag == "Receptacle") {
                        SimObjPhysics sop;

                        sop = item.GetComponentInParent<SimObjPhysics>();

                        // now we have a reference to our sim object
                        if (sop && (filter == null || filter.Contains(sop))) {
                            // check against all visibility points, accumulate count. If at least one point is visible, set object to visible
                            if (sop.VisibilityPoints.Length > 0) {
                                Transform[] visPoints = sop.VisibilityPoints;
                                int visPointCount = 0;

                                foreach (Transform point in visPoints) {
                                    //if this particular point is in view...
                                    if (CheckIfVisibilityPointInViewport(sop, point, agentCamera, true)) {
                                        visPointCount++;
                                    }
                                }

                                //if we see at least one vis point, the object is "visible"
                                if (visPointCount > 0) {
                                    #if UNITY_EDITOR
                                    sop.isVisible = true;
                                    #endif
                                    if (!currentlyVisibleItems.Contains(sop)) {
                                        currentlyVisibleItems.Add(sop);
                                    }
                                }
                            } else {
                                Debug.Log("Error! Set at least 1 visibility point on SimObjPhysics prefab!");
                            }
                        }
                    }
                }
            }

            // Turn back on the colliders corresponding to this agent and invisible agents.
            updateAllAgentCollidersForVisibilityCheck(true);

            //populate array of visible items in order by distance
            currentlyVisibleItems.Sort((x, y) => Vector3.Distance(x.transform.position, agentCameraPos).CompareTo(Vector3.Distance(y.transform.position, agentCameraPos)));
            return currentlyVisibleItems.ToArray();
        }

        //check if the visibility point on a sim object, sop, is within the viewport
        //has a include Invisible bool to check against triggerboxes as well, to check for visibility with things like Cabinets/Drawers
        protected bool CheckIfVisibilityPointRaycast(
            SimObjPhysics sop,
            Transform point,
            Camera agentCamera,
            bool includeInvisible
        ) {
            bool result = false;
            // now cast a ray out toward the point, if anything occludes this point, that point is not visible
            RaycastHit hit;

            float distFromPointToCamera = Vector3.Distance(point.position, m_Camera.transform.position);

            // adding slight buffer to this distance to ensure the ray goes all the way to the collider of the object being cast to
            float raycastDistance = distFromPointToCamera + 0.5f;

            LayerMask mask = (1 << 8) | (1 << 9) | (1 << 10);

            //change mask if its a floor so it ignores the receptacle trigger boxes on the floor
            if (sop.Type == SimObjType.Floor)
            mask = (1 << 8) | (1 << 10);

            // check raycast against both visible and invisible layers, to check against ReceptacleTriggerBoxes which are normally
            // ignored by the other raycast
            if (includeInvisible && Physics.Raycast(agentCamera.transform.position, point.position - agentCamera.transform.position, out hit, raycastDistance, mask)) {
                if (hit.transform != sop.transform) {
                    return false;
                } else {
                    // if this line is drawn, then this visibility point is in camera frame and not occluded
                    // might want to use this for a targeting check as well at some point....
                    sop.isInteractable = true;

                    #if UNITY_EDITOR
                        Debug.DrawLine(agentCamera.transform.position, point.position, Color.cyan);
                    #endif
                    return true;
                }
            }

            // only check against the visible layer, ignore the invisible layer
            // so if an object ONLY has colliders on it that are not on layer 8, this raycast will go through them
            if (Physics.Raycast(agentCamera.transform.position, point.position - agentCamera.transform.position, out hit, raycastDistance, (1 << 8) | (1 << 10))) {
                if (hit.transform == sop.transform) {
                    // if this line is drawn, then this visibility point is in camera frame and not occluded
                    // might want to use this for a targeting check as well at some point....
                    sop.isInteractable = true;
                    return true;
                }

                // we didn't directly hit the sop we are checking for with this cast,
                // check if it's because we hit something see-through
                SimObjPhysics hitSop = hit.transform.GetComponent<SimObjPhysics>();
                if (hitSop != null && hitSop.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanSeeThrough)) {
                    //we hit something see through, so now find all objects in the path between
                    //the sop and the camera
                    RaycastHit[] hits;
                    hits = Physics.RaycastAll(agentCamera.transform.position, point.position - agentCamera.transform.position,
                        raycastDistance, (1 << 8), QueryTriggerInteraction.Ignore);

                    float[] hitDistances = new float[hits.Length];
                    for (int i = 0; i < hitDistances.Length; i++) {
                        hitDistances[i] = hits[i].distance; //Vector3.Distance(hits[i].transform.position, m_Camera.transform.position);
                    }

                    Array.Sort(hitDistances, hits);

                    foreach (RaycastHit h in hits) {
                        if (h.transform == sop.transform) {
                            // found the object we are looking for, great!
                            return true;
                        } else {
                            // Didn't find it, continue on only if the hit object was translucent
                            SimObjPhysics sopHitOnPath = null;
                            sopHitOnPath = h.transform.GetComponentInParent<SimObjPhysics>();
                            if (sopHitOnPath == null || !sopHitOnPath.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanSeeThrough)) {
                                return false;
                            }
                        }
                    }
                }
            }
            return false;
        }

        protected bool CheckIfVisibilityPointInViewport(
            SimObjPhysics sop,
            Transform point,
            Camera agentCamera,
            bool includeInvisible
        ) {
            bool result = false;

            Vector3 viewPoint = agentCamera.WorldToViewportPoint(point.position);

            float ViewPointRangeHigh = 1.0f;
            float ViewPointRangeLow = 0.0f;

            if (viewPoint.z > 0 && //&& viewPoint.z < maxDistance * DownwardViewDistance //is in front of camera and within range of visibility sphere
                viewPoint.x < ViewPointRangeHigh && viewPoint.x > ViewPointRangeLow && // within x bounds of viewport
                viewPoint.y < ViewPointRangeHigh && viewPoint.y > ViewPointRangeLow  // within y bounds of viewport
            ) {
                result = CheckIfVisibilityPointRaycast(sop, point, agentCamera, includeInvisible);
            }

            #if UNITY_EDITOR
                if (result) {
                    Debug.DrawLine(agentCamera.transform.position, point.position, Color.cyan);
                }
            #endif

            return result;
        }

        public void DefaultAgentHand() {
            ResetAgentHandPosition();
            ResetAgentHandRotation();
            IsHandDefault = true;
        }

        public void ResetAgentHandPosition() {
            AgentHand.transform.position = DefaultHandPosition.transform.position;
            SimObjPhysics sop = AgentHand.GetComponentInChildren<SimObjPhysics>();
            if (sop != null) {
                sop.gameObject.transform.localPosition = Vector3.zero;
            }
        }

        public void ResetAgentHandRotation() {
            AgentHand.transform.localRotation = Quaternion.Euler(Vector3.zero);
            SimObjPhysics sop = AgentHand.GetComponentInChildren<SimObjPhysics>();
            if (sop != null) {
                sop.gameObject.transform.rotation = transform.rotation;
            }
        }

        //randomly repositions sim objects in the current scene
        public void InitialRandomSpawn(
            int randomSeed = 0,
            bool forceVisible = false,
            bool placeStationary = true,
            ObjectTypeCount[] numDuplicatesOfType = null,
            String[] excludedReceptacles = null,
            String[] excludedObjectIds = null,
            int numPlacementAttempts = 5,
            bool allowFloor = false
        ) {
            if (numPlacementAttempts <= 0) {
                throw ArgumentOutOfRangeException("numPlacementAttempts must be a positive integer.");
            }

            //something is in our hand AND we are trying to spawn it. Quick drop the object
            if (ItemInHand != null) {
                Rigidbody rb = ItemInHand.GetComponent<Rigidbody>();
                rb.isKinematic = false;
                rb.constraints = RigidbodyConstraints.None;
                rb.useGravity = true;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                GameObject topObject = GameObject.Find("Objects");
                if (topObject != null) {
                    ItemInHand.transform.parent = topObject.transform;
                } else {
                    ItemInHand.transform.parent = null;
                }

                rb.angularVelocity = UnityEngine.Random.insideUnitSphere;

                ItemInHand.GetComponent<SimObjPhysics>().isInAgentHand = false; //agent hand flag
                DefaultAgentHand();//also default agent hand
                ItemInHand = null;
            }

            // default excludedReceptacles if null
            if (excludedReceptacles == null) {
                excludedReceptacles = new String[0];
            }

            List<SimObjType> listOfExcludedReceptacleTypes = new List<SimObjType>();

            // check if strings used for excludedReceptacles are valid object types
            foreach (string receptacleType in excludedReceptacles) {
                try {
                    SimObjType objType = (SimObjType)Enum.Parse(typeof(SimObjType), receptacleType);
                    listOfExcludedReceptacleTypes.Add(objType);
                } catch (ArgumentException) {
                    throw new ArgumentException("invalid Object Type used in excludedReceptacles array: " + receptacleType);
                }
            }
            if (!allowFloor) {
                listOfExcludedReceptacleTypes.Add(SimObjType.Floor);
            }

            if (excludedObjectIds == null)  {
                excludedObjectIds = new String[0];
            }

            HashSet<SimObjPhysics> excludedSimObjects = new HashSet<SimObjPhysics>();
            foreach (String objectId in excludedObjectIds) {
                excludedSimObjects.Add(getTargetObject(objectId: objectId, forceAction: true));
            }

            bool success = physicsSceneManager.RandomSpawnRequiredSceneObjects(
                seed: randomSeed,
                spawnOnlyOutside: forceVisible,
                maxPlacementAttempts: numPlacementAttempts,
                staticPlacement: placeStationary,
                excludedSimObjects: excludedSimObjects,
                numDuplicatesOfType: numDuplicatesOfType,
                excludedReceptacleTypes: listOfExcludedReceptacleTypes
            );

            // Let things come to rest for 2 seconds.
            if (success && !placeStationary) {
                bool autoSim = Physics.autoSimulation;
                Physics.autoSimulation = false;
                for (int i = 0; i < 100; i++) {
                    Physics.Simulate(0.02f);
                }
                Physics.autoSimulation = Physics.autoSimulation;
            }
            physicsSceneManager.ResetObjectIdToSimObjPhysics();

            actionFinished(success: success);
        }

        // On demand public function for getting what sim objects are visible at that moment 
        public List<SimObjPhysics> GetAllVisibleSimObjPhysics(float maxDistance) {
            var camera = this.GetComponentInChildren<Camera>();
            return new List<SimObjPhysics>(GetAllVisibleSimObjPhysics(camera, maxDistance));
        }

        //not sure what this does, maybe delete?
        public void SetTopLevelView(bool topView = false) {
            inTopLevelView = topView;
            actionFinished(true);
        }

        public void ToggleMapView() {

            SyncTransform[] syncInChildren;

            List<StructureObject> structureObjsList = new List<StructureObject>();
            StructureObject[] structureObjs = FindObjectsOfType(typeof(StructureObject)) as StructureObject[];

            foreach (StructureObject so in structureObjs)
            {
                if ((so.WhatIsMyStructureObjectTag == StructureObjectTag.Ceiling) ||
                    (so.WhatIsMyStructureObjectTag == StructureObjectTag.LightFixture) ||
                    (so.WhatIsMyStructureObjectTag == StructureObjectTag.CeilingLight)
                ) {
                    structureObjsList.Add(so);
                }
            }

            if (inTopLevelView) {
                inTopLevelView = false;
                m_Camera.orthographic = false;
                m_Camera.transform.localPosition = lastLocalCameraPosition;
                m_Camera.transform.localRotation = lastLocalCameraRotation;

                //restore agent body culling
                m_Camera.transform.GetComponent<FirstPersonCharacterCull>().StopCullingThingsForASecond = false;
                syncInChildren = gameObject.GetComponentsInChildren<SyncTransform>();
                foreach (SyncTransform sync in syncInChildren)
                {
                    sync.StopSyncingForASecond = false;
                }

                foreach (StructureObject so in structureObjsList) {
                    updateDisplayGameObject(target: so.gameObject, enabled: true);
                }
            }

            else {

                //stop culling the agent's body so it's visible from the top?
                m_Camera.transform.GetComponent<FirstPersonCharacterCull>().StopCullingThingsForASecond = true;
                syncInChildren = gameObject.GetComponentsInChildren<SyncTransform>();
                foreach (SyncTransform sync in syncInChildren)
                {
                    sync.StopSyncingForASecond = true;
                }

                inTopLevelView = true;
                lastLocalCameraPosition = m_Camera.transform.localPosition;
                lastLocalCameraRotation = m_Camera.transform.localRotation;

                Bounds b = new Bounds();
                b.min = agentManager.SceneBounds.min;
                b.max = agentManager.SceneBounds.max;
                float midX = (b.max.x + b.min.x) / 2.0f;
                float midZ = (b.max.z + b.min.z) / 2.0f;
                m_Camera.transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
                m_Camera.transform.position = new Vector3(midX, b.max.y + 5, midZ);
                m_Camera.orthographic = true;

                m_Camera.orthographicSize = Math.Max((b.max.x - b.min.x) / 2f, (b.max.z - b.min.z) / 2f);

                cameraOrthSize = m_Camera.orthographicSize;
                foreach (StructureObject so in structureObjsList) {
                    updateDisplayGameObject(target: so.gameObject, enabled: false);
                }            }
            actionFinished(true);
        }

        public void updateDisplayGameObject(GameObject target, bool enabled) {
            if (target == null) {
                throw new ArgumentNullException("target must be specified");
            }
            foreach (MeshRenderer mr in target.GetComponentsInChildren<MeshRenderer>() as MeshRenderer[]) {
                if (!initiallyDisabledRenderers.Contains(mr.GetInstanceID())) {
                    mr.enabled = enabled;
                }
            }
        }

        ///////////////////////////////////////////
        ///////////// VISUALIZE PATH //////////////
        ///////////////////////////////////////////

        // still experimental?
        public void VisualizePath(Vector3[] positions, string text = "", bool showGrid = false) {
            if (positions == null || text == null) {
                throw new ArgumentNullException("text and positions must be non-null.");
            }

            if (positions.Count == 0) {
                throw new ArgumentOutOfRangeException("Positions must have at least 1 position!");
            }

            if (showGrid) {
                getReachablePositions(visualize: true);
            }

            // set start and target positions
            Instantiate(DebugTargetPointPrefab, positions[positions.Count - 1], Quaternion.identity);
            GameObject start = Instantiate(DebugPointPrefab, positions[0], Quaternion.identity) as GameObject;

            // set the text
            TextMesh textMesh = start.GetComponentInChildren<TextMesh>();
            textMesh.text = text;

            // connect the dots
            LineRenderer lineRenderer = start.GetComponentInChildren<LineRenderer>();
            lineRenderer.startWidth = 0.015f;
            lineRenderer.endWidth = 0.015f;
            lineRenderer.positionCount = positions.Count;
            lineRenderer.SetPositions(positions);

            actionFinished(success: true);
        }

        // TODO: use VisualizePath
        public void VisualizeShortestPaths(ServerAction action) {
            SimObjPhysics sop = getSimObjectFromTypeOrId(action.objectType, action.objectId);

            getReachablePositions(1.0f, 10000, action.grid, action.gridColor);

            Instantiate(DebugTargetPointPrefab, sop.transform.position, Quaternion.identity);
            var results = new List<bool>();
            for (var i = 0; i < action.positions.Count; i++) {
                var pos = action.positions[i];
                var go = Instantiate(DebugPointPrefab, pos, Quaternion.identity);
                var textMesh = go.GetComponentInChildren<TextMesh>();
                textMesh.text = i.ToString();

                var path = GetSimObjectNavMeshTarget(sop, pos, Quaternion.identity, 0.1f);

                var lineRenderer = go.GetComponentInChildren<LineRenderer>();

                if (action.pathGradient != null && action.pathGradient.colorKeys.Length > 0){
                    lineRenderer.colorGradient = action.pathGradient;
                }
                lineRenderer.startWidth = 0.015f;
                lineRenderer.endWidth = 0.015f;

                results.Add(path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete);
               
                if (path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete) { 
                    lineRenderer.positionCount = path.corners.Length;
                    lineRenderer.SetPositions(path.corners.Select(c => new Vector3(c.x, gridVisualizeY + 0.005f, c.z)).ToArray());
                }
            }
            actionFinished(true, results.ToArray());
        }


        #if UNITY_EDITOR
            // this one is used for in-editor debug draw, currently calls to this are commented out
            private void VisualizePath(Vector3 startPosition, NavMeshPath path) {
                var pathDistance = 0.0;

                for (int i = 0; i < path.corners.Length - 1; i++) {
                    Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.red, 10.0f);
                    Debug.Log("P i:" + i + " : " + path.corners[i] + " i+1:" + i + 1 + " : " + path.corners[i]);
                    pathDistance += Vector3.Distance(path.corners[i], path.corners[i + 1]);
                }

                if (pathDistance > 0.0001 ) {
                    // Better way to draw spheres
                    var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    go.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                    go.GetComponent<Collider>().enabled = false;
                    go.transform.position = startPosition;
                }
            }
        #endif

        ///////////////////////////////////////////
        ///////////// VISUALIZE GRID //////////////
        ///////////////////////////////////////////

        public void VisualizeGrid() {
            var reachablePositions = getReachablePositions(visualize: true);
            actionFinished(true, reachablePositions);
        }

        ///////////////////////////////////////////
        ///////////// SHORTEST PATH ///////////////
        ///////////////////////////////////////////

        private void getShortestPath(
            string objectType,
            string objectId,
            Vector3 startPosition,
            Quaternion startRotation,
            float allowedError
        ) {
            SimObjPhysics sop = getSimObjectFromTypeOrId(objectType, objectId);
            var path = GetSimObjectNavMeshTarget(sop, startPosition, startRotation, allowedError);
            if (path.status != UnityEngine.AI.NavMeshPathStatus.PathComplete) {
                throw InvalidOperationException("Path to target could not be found");
            }
            actionFinishedEmit(true, path);
        }

        public void GetShortestPath(
            Vector3 position,
            Vector3 rotation,
            string objectType = null,
            string objectId = null,
            float allowedError = DefaultAllowedErrorInShortestPath
        ) {
            getShortestPath(objectType, objectId, position, Quaternion.Euler(rotation), allowedError);
        }

        public void GetShortestPath(
            Vector3 position,
            string objectType = null,
            string objectId = null,
            float allowedError = DefaultAllowedErrorInShortestPath
        ) {
            getShortestPath(objectType, objectId, position, Quaternion.Euler(Vector3.zero), allowedError);
        }

        public void GetShortestPath(
            string objectType = null,
            string objectId = null,
            float allowedError = DefaultAllowedErrorInShortestPath
        ) {
            getShortestPath(objectType, objectId, this.transform.position, this.transform.rotation, allowedError);
        }

        private bool GetPathFromReachablePositions(
            IEnumerable<Vector3> sortedPositions,
            Vector3 targetPosition,
            Transform agentTransform,
            string targetSimObjectId,
            UnityEngine.AI.NavMeshPath path) {
                
            Vector3 fixedPosition = new Vector3(float.MinValue, float.MinValue, float.MinValue);
            //bool success = false;
            var PhysicsController = this;
            foreach (var pos in sortedPositions) {
                agentTransform.position = pos;
                agentTransform.LookAt(targetPosition);

                var visibleSimObjects = PhysicsController.GetAllVisibleSimObjPhysics(PhysicsController.maxVisibleDistance);
                if (visibleSimObjects.Any(sop => sop.objectID == targetSimObjectId)) {
                    fixedPosition = pos;
                    //success = true;
                    break;
                }
            }

            var pathSuccess =  UnityEngine.AI.NavMesh.CalculatePath(agentTransform.position, fixedPosition,  UnityEngine.AI.NavMesh.AllAreas, path);
            return pathSuccess;
        }

        private string[] objectTypeToObjectIds(string objectTypeString) {
            List<string> objectIds = new List<string>();
            // TODO: why are we using try/catch here?
            try {
                SimObjType objectType = (SimObjType) Enum.Parse(typeof(SimObjType), objectTypeString.Replace(" ", String.Empty), true);
                foreach (var s in physicsSceneManager.ObjectIdToSimObjPhysics) {
                    if (s.Value.ObjType == objectType) {
                        objectIds.Add(s.Value.objectID);
                    }
                }
            }
            catch (ArgumentException exception) {
                throw new ArgumentException($"Invalid object type '{objectTypeString}'.");
            }
            return objectIds.ToArray();
        }

        public void ObjectTypeToObjectIds(string objectType) {
            try {
                string[] objectIds = objectTypeToObjectIds(objectType);
            } catch (ArgumentException exception) {
                throw new ArgumentException($"Invalid object type '{objectTypeString}'.");
            }
            actionFinished(true, objectIds.ToArray());
        }

        // TODO: remove
        private SimObjPhysics getSimObjectFromTypeOrId(string objectType, string objectId) {
            if (!String.IsNullOrEmpty(objectType) && String.IsNullOrEmpty(objectId)) {
                var ids = objectTypeToObjectIds(objectType);
                if (ids.Length == 0) {
                    errorMessage = "Object type '" + objectType + "' was not found in the scene.";
                    return null;
                }
                else if (ids.Length > 1) {
                    errorMessage = "Multiple objects of type '" + objectType + "' were found in the scene, cannot disambiguate.";
                    return null;
                }
                
                objectId = ids[0];
            }

            SimObjPhysics sop = getTargetObject(objectId: objectId, forceAction: true);
            return sop;
        }

        protected Collider[] overlapCollider(BoxCollider box, Vector3 newCenter, float rotateBy, int layerMask) {
            Vector3 center, halfExtents;
            Quaternion orientation;
            box.ToWorldSpaceBox(out center, out halfExtents, out orientation);
            orientation = Quaternion.Euler(0f, rotateBy, 0f) * orientation;

            return Physics.OverlapBox(newCenter, halfExtents, orientation, layerMask, QueryTriggerInteraction.Ignore);
        }

        protected Collider[] overlapCollider(SphereCollider sphere, Vector3 newCenter, int layerMask) {
            Vector3 center;
            float radius;
            sphere.ToWorldSpaceSphere(out center, out radius);
            return Physics.OverlapSphere(newCenter, radius, layerMask, QueryTriggerInteraction.Ignore);
        }

        protected Collider[] overlapCollider(CapsuleCollider capsule, Vector3 newCenter, float rotateBy, int layerMask) {
            Vector3 point0, point1;
            float radius;
            capsule.ToWorldSpaceCapsule(out point0, out point1, out radius);

            // Normalizing
            Vector3 oldCenter = (point0 + point1) / 2.0f;
            point0 = point0 - oldCenter;
            point1 = point1 - oldCenter;

            // Rotating and recentering
            var rotator = Quaternion.Euler(0f, rotateBy, 0f);
            point0 = rotator * point0 + newCenter;
            point1 = rotator * point1 + newCenter;

            return Physics.OverlapCapsule(point0, point1, radius, layerMask, QueryTriggerInteraction.Ignore);
        }

        protected bool handObjectCanFitInPosition(Vector3 newAgentPosition, float rotation) {
            if (ItemInHand == null) {
                return true;
            }

            SimObjPhysics soInHand = ItemInHand.GetComponent<SimObjPhysics>();

            Vector3 handObjPosRelAgent =
                Quaternion.Euler(0, rotation - transform.eulerAngles.y, 0) *
                (transform.position - ItemInHand.transform.position);

            Vector3 newHandPosition = handObjPosRelAgent + newAgentPosition;

            int layerMask = 1 << 8;
            foreach (CapsuleCollider cc in soInHand.GetComponentsInChildren<CapsuleCollider>()) {
                foreach (Collider c in overlapCollider(cc, newHandPosition, rotation, layerMask)) {
                    if (!hasAncestor(c.transform.gameObject, gameObject)) {
                        return false;
                    }
                }
            }

            foreach (BoxCollider bc in soInHand.GetComponentsInChildren<BoxCollider>()) {
                foreach (Collider c in overlapCollider(bc, newHandPosition, rotation, layerMask)) {
                    if (!hasAncestor(c.transform.gameObject, gameObject)) {
                        return false;
                    }
                }
            }

            foreach (SphereCollider sc in soInHand.GetComponentsInChildren<SphereCollider>()) {
                foreach (Collider c in overlapCollider(sc, newHandPosition, layerMask)) {
                    if (!hasAncestor(c.transform.gameObject, gameObject)) {
                        return false;
                    }
                }
            }

            return true;
        }

        // cast a capsule the same size as the agent
        // used to check for collisions
        public RaycastHit[] capsuleCastAllForAgent(
            CapsuleCollider cc,
            float skinWidth,
            Vector3 startPosition,
            Vector3 dir,
            float moveMagnitude,
            int layerMask
        ) {
            Vector3 center = cc.transform.position + cc.center;//make sure to offset this by cc.center since we shrank the capsule size
            float radius = cc.radius + skinWidth;
            float innerHeight = cc.height / 2.0f - radius;
            Vector3 point1 = new Vector3(startPosition.x, center.y + innerHeight, startPosition.z);
            Vector3 point2 = new Vector3(startPosition.x, center.y - innerHeight + skinWidth, startPosition.z);
            return Physics.CapsuleCastAll(
                point1,
                point2,
                radius,
                dir,
                moveMagnitude,
                layerMask,
                QueryTriggerInteraction.Ignore
            );
        }

        protected bool isAgentCapsuleColliding(
            HashSet<Collider> collidersToIgnore = null,
            bool includeErrorMessage = false
        ) {
            int layerMask = 1 << 8;
            foreach (Collider c in PhysicsExtensions.OverlapCapsule(GetComponent<CapsuleCollider>(), layerMask, QueryTriggerInteraction.Ignore)) {
                if ((!hasAncestor(c.transform.gameObject, gameObject)) && (
                    collidersToIgnore == null || !collidersToIgnoreDuringMovement.Contains(c))
                ) {
                    if (includeErrorMessage) {
                        SimObjPhysics sop = ancestorSimObjPhysics(c.gameObject);
                        String collidedWithName;
                        if (sop != null) {
                            collidedWithName = sop.ObjectID;
                        } else {
                            collidedWithName = sop.gameObject.name;
                        }
                        errorMessage = $"Collided with: {collidedWithName}.";
                    }
                    #if UNITY_EDITOR
                        Debug.Log("Collided with: ");
                        Debug.Log(c);
                        Debug.Log(c.enabled);
                    #endif
                    return true;
                }
            }
            return false;
        }

        protected Collider[] objectsCollidingWithAgent() {
            int layerMask = 1 << 8;
            return PhysicsExtensions.OverlapCapsule(GetComponent<CapsuleCollider>(), layerMask, QueryTriggerInteraction.Ignore);
        }

        public bool  getReachablePositionToObjectVisible(SimObjPhysics targetSOP, out Vector3 pos, float gridMultiplier = 1.0f, int maxStepCount = 10000) {
            CapsuleCollider cc = GetComponent<CapsuleCollider>();
            float sw = m_CharacterController.skinWidth;
            Queue<Vector3> pointsQueue = new Queue<Vector3>();
            pointsQueue.Enqueue(transform.position);
            Vector3[] directions = {
                new Vector3(1.0f, 0.0f, 0.0f),
                new Vector3(0.0f, 0.0f, 1.0f),
                new Vector3(-1.0f, 0.0f, 0.0f),
                new Vector3(0.0f, 0.0f, -1.0f)
            };
            Quaternion originalRot = transform.rotation;

            HashSet<Vector3> goodPoints = new HashSet<Vector3>();
            HashSet<Vector3> seenPoints = new HashSet<Vector3>();
            int layerMask = 1 << 8;
            int stepsTaken = 0;
            pos = Vector3.negativeInfinity;
            while (pointsQueue.Count != 0) {
                stepsTaken += 1;
                Vector3 p = pointsQueue.Dequeue();
                if (!goodPoints.Contains(p)) {
                    goodPoints.Add(p);
                    transform.position = p;
                    var rot = transform.rotation;
                    //make sure to rotate just the Camera, not the whole agent
                    m_Camera.transform.LookAt(targetSOP.transform, transform.up);

                    bool isVisible = false;
                    if (this.visibilityScheme == VisibilityScheme.Distance) {
                        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(this.m_Camera);
                        isVisible = isSimObjVisible(this.m_Camera, targetSOP, this.maxVisibleDistance, planes);
                    } else {
                        var visibleSimObjects = this.GetAllVisibleSimObjPhysics(this.maxVisibleDistance);
                        isVisible = visibleSimObjects.Any(sop => sop.objectID == targetSOP.objectID);
                    }

                    transform.rotation = rot;

                    if (isVisible){
                        pos = p;
                        return true;
                    }

                    
                    HashSet<Collider> objectsAlreadyColliding = new HashSet<Collider>(objectsCollidingWithAgent());
                    foreach (Vector3 d in directions) {
                        Vector3 newPosition = p + d * gridSize * gridMultiplier;
                        if (seenPoints.Contains(newPosition)) {
                            continue;
                        }
                        seenPoints.Add(newPosition);

                        RaycastHit[] hits = capsuleCastAllForAgent(
                            cc,
                            sw,
                            p,
                            d,
                            (gridSize * gridMultiplier),
                            layerMask
                        );

                        bool shouldEnqueue = true;
                        foreach (RaycastHit hit in hits) {
                            if (hit.transform.gameObject.name != "Floor" &&
                                !ancestorHasName(hit.transform.gameObject, "FPSController") &&
                                !objectsAlreadyColliding.Contains(hit.collider)
                            ) {
                                shouldEnqueue = false;
                                break;
                            }
                        }
                        bool inBounds = agentManager.SceneBounds.Contains(newPosition);
                        if (errorMessage == "" && !inBounds) {
                            errorMessage = "In " +
                                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name +
                                ", position " + newPosition.ToString() +
                                " can be reached via capsule cast but is beyond the scene bounds.";
                        }

                        shouldEnqueue = shouldEnqueue && inBounds && (
                            handObjectCanFitInPosition(newPosition, 0.0f) ||
                            handObjectCanFitInPosition(newPosition, 90.0f) ||
                            handObjectCanFitInPosition(newPosition, 180.0f) ||
                            handObjectCanFitInPosition(newPosition, 270.0f)
                        );
                        if (shouldEnqueue) {
                            pointsQueue.Enqueue(newPosition);
                            #if UNITY_EDITOR
                                Debug.DrawLine(p, newPosition, Color.cyan, 100000f);
                            #endif
                        }
                    }
                }
                if (stepsTaken > Math.Floor(maxStepCount/(gridSize * gridSize))) {
                    errorMessage = "Too many steps taken in GetReachablePositions.";
                    break;
                }
            }

            Vector3[] reachablePos = new Vector3[goodPoints.Count];
            goodPoints.CopyTo(reachablePos);
            #if UNITY_EDITOR
                Debug.Log(reachablePos.Length);
            #endif
            return false;
        }

        private UnityEngine.AI.NavMeshPath GetSimObjectNavMeshTarget(
            SimObjPhysics targetSOP,
            Vector3 initialPosition,
            Quaternion initialRotation,
            float allowedError,
            bool visualize = false
        ) {
            var targetTransform = targetSOP.transform;
            var targetSimObject = targetTransform.GetComponentInChildren<SimObjPhysics>();
            var PhysicsController = this;
            var agentTransform = PhysicsController.transform;

            var originalAgentPosition = agentTransform.position;
            var orignalAgentRotation = agentTransform.rotation;
            var originalCameraRotation = m_Camera.transform.rotation;

            var fixedPosition = Vector3.negativeInfinity;

            agentTransform.position = initialPosition;
            agentTransform.rotation = initialRotation;
            getReachablePositionToObjectVisible(targetSimObject, out fixedPosition);
            agentTransform.position = originalAgentPosition;
            agentTransform.rotation = orignalAgentRotation;
            m_Camera.transform.rotation = originalCameraRotation;

            var path = new UnityEngine.AI.NavMeshPath();
            var sopPos = targetSOP.transform.position;
            //var target = new Vector3(sopPos.x, initialPosition.y, sopPos.z);

            bool pathSuccess = SafelyComputeNavMeshPath(initialPosition, fixedPosition, path, allowedError);
            
            var pathDistance = 0.0f;
            for (int i = 0; i < path.corners.Length - 1; i++) {
                #if UNITY_EDITOR
                    //Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.red, 10.0f);
                    Debug.Log("Corner " + i + ": " + path.corners[i]);
                #endif
                pathDistance += Vector3.Distance(path.corners[i], path.corners[i + 1]);
            }

            return path;
        }

        protected float getFloorY(float x, float start_y, float z) {
            int layerMask = ~(LayerMask.GetMask("Agent") | LayerMask.GetMask("SimObjInvisible"));

            float y = start_y;
            RaycastHit hit;
            Ray ray = new Ray(new Vector3(x, y, z), -transform.up);
            if (!Physics.Raycast(ray, out hit, 100f, layerMask)) {
                errorMessage = "Could not find the floor";
                return float.NegativeInfinity;
            }
            return hit.point.y;
        }
        
        protected float getFloorY(float x, float z) {
            int layerMask = ~LayerMask.GetMask("Agent");

            Ray ray = new Ray(transform.position, -transform.up);
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, 10f, layerMask)) {
                errorMessage = "Could not find the floor";
                return float.NegativeInfinity;
            }
            return getFloorY(x, hit.point.y + 0.1f, z);
        }

        protected bool SafelyComputeNavMeshPath(
            Vector3 start,
            Vector3 target,
            UnityEngine.AI.NavMeshPath path,
            float allowedError
        ) {
            float floorY = Math.Min(
                getFloorY(start.x, start.y, start.z),
                getFloorY(target.x, target.y, target.z)
            );
            Vector3 startPosition = new Vector3(start.x, floorY, start.z);
            Vector3 targetPosition = new Vector3(target.x, floorY, target.z);

            this.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = true;

            NavMeshHit startHit;
            bool startWasHit = UnityEngine.AI.NavMesh.SamplePosition(
                startPosition, out startHit, Math.Max(0.2f, allowedError), UnityEngine.AI.NavMesh.AllAreas
            );

            NavMeshHit targetHit;
            bool targetWasHit = UnityEngine.AI.NavMesh.SamplePosition(
                targetPosition, out targetHit, Math.Max(0.2f, allowedError), UnityEngine.AI.NavMesh.AllAreas
            );

            if (!startWasHit || !targetWasHit) {
                if (!startWasHit) {
                    errorMessage = $"No point on NavMesh near {startPosition}.";
                }
                if (!targetWasHit) {
                    errorMessage = $"No point on NavMesh near {targetPosition}.";
                }
                this.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
                return false;
            }

            float startOffset = Vector3.Distance(
                startHit.position, 
                new Vector3(startPosition.x, startHit.position.y, startPosition.z)
            );
            float targetOffset = Vector3.Distance(
                targetHit.position, 
                new Vector3(targetPosition.x, targetHit.position.y, targetPosition.z)
            );
            if (startOffset > allowedError && targetOffset > allowedError) {
                errorMessage = $"Closest point on NavMesh was too far from the agent: " + 
                    $" (startPosition={startPosition.ToString("F3")}," +
                    $" closest navmesh position {startHit.position.ToString("F3")}) and" +
                    $" (targetPosition={targetPosition.ToString("F3")}," +
                    $" closest navmesh position {targetHit.position.ToString("F3")}).";
                this.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
                return false;
            }

            #if UNITY_EDITOR
            Debug.Log($"Attempting to find path from {startHit.position} to {targetHit.position}.");
            #endif
            bool pathSuccess = UnityEngine.AI.NavMesh.CalculatePath(
                startHit.position, targetHit.position,  UnityEngine.AI.NavMesh.AllAreas, path
            );
            if (path.status == UnityEngine.AI.NavMeshPathStatus.PathComplete) {
                #if UNITY_EDITOR
                VisualizePath(startHit.position, path);
                #endif
                this.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
                return true;
            }
            else {
                errorMessage = $"Could not find path between {startHit.position.ToString("F3")}" +
                    $" and {targetHit.position.ToString("F3")} using the NavMesh.";
                this.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
                return false;
            }
        }
        public void GetShortestPathToPoint(
            Vector3 position, float x, float y, float z, float allowedError = DefaultAllowedErrorInShortestPath
        ) {
            var path = new UnityEngine.AI.NavMeshPath();
            if (SafelyComputeNavMeshPath(position, new Vector3(x, y, z), path, allowedError)) {
                actionFinished(true, path);
            } else {
                actionFinished(false);
            }
        }

        public void GetShortestPathToPoint(
            float x,
            float y,
            float z,
            float allowedError = DefaultAllowedErrorInShortestPath
        ) {
            var startPosition = this.transform.position;
            GetShortestPathToPoint(startPosition, x, y, z, allowedError);
        }

        public void CameraCrack(int randomSeed = 0) {
            GameObject canvas = Instantiate(CrackedCameraCanvas);
            CrackedCameraManager camMan = canvas.GetComponent<CrackedCameraManager>();

            camMan.SpawnCrack(randomSeed);
            actionFinished(true);
        }

        public void OnTriggerStay(Collider other) {
            inHighFrictionArea = other.CompareTag("HighFriction");
        }

        public void DisableObjectsOfType(string objectType) {
            foreach (SimObjPhysics target in GameObject.FindObjectsOfType<SimObjPhysics>()) {
                if (Enum.GetName(typeof(SimObjType), target.Type) == type) {
                    target.gameObject.SetActive(false);
                }
            }
            actionFinished(success: true);
        }

        ///////////////////////////////////////////
        ///////// ACTION DISPATCH TESTING /////////
        ///////////////////////////////////////////

        public void TestActionDispatchSAAmbig2(float foo, bool def=false) {
            actionFinished(true);
        }

        public void TestActionDispatchSAAmbig2(float foo) {
            actionFinished(true);
        }

        public void TestActionDispatchSAAmbig(ServerAction action) {
            actionFinished(true);
        }

        public void TestActionDispatchSAAmbig(float foo) {
            actionFinished(true);
        }

        public void TestActionDispatchNoopServerAction(ServerAction action) {
            actionFinished(true, "serveraction");
        }

        public void TestFastEmit(string rvalue) {
            actionFinishedEmit(true, rvalue);
        }

        public void TestActionDispatchNoopAllDefault2(float param12, float param10=0.0f, float param11=1.0f) {
            actionFinished(true, "somedefault");
        }

        public void TestActionDispatchNoopAllDefault(float param10=0.0f, float param11=1.0f) {
            actionFinished(true, "alldefault");
        }

        public void TestActionDispatchNoop2(bool param3,  string param4="foo") {
            actionFinished(true, "param3 param4/default " + param4);
        }

        public void TestActionDispatchNoop(string param6, string param7) {
            actionFinished(true, "param6 param7");
        }

        public void TestActionDispatchNoop(bool param1, bool param2) {
            actionFinished(true, "param1 param2");
        }

        public void TestActionDispatchConflict(string param22) {
            actionFinished(true);
        }

        public void TestActionDispatchConflict(bool param22) {
            actionFinished(true);
        }

        public void TestActionDispatchNoop(bool param1) {
            actionFinished(true, "param1");
        }

        public void TestActionDispatchNoop() {
            actionFinished(true, "emptyargs");
        }

        public void TestActionDispatchFindAmbiguous(string typeName) {
            List<string> actions = ActionDispatcher.FindAmbiguousActions(Type.GetType(typeName));
            actionFinished(true, actions);
        }

        public void TestActionDispatchFindConflicts(string typeName) {
            Dictionary<string, List<string>> conflicts = ActionDispatcher.FindMethodVariableNameConflicts(Type.GetType(typeName));
            actionFinished(true, conflicts);
        }
	}
}
