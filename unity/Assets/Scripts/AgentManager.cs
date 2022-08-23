using System;
using System.Collections;

using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
using System.IO;
using System.Net.Sockets;
using System.Net;
using MessagePack.Resolvers;
using MessagePack.Formatters;
using MessagePack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Reflection;
using System.Text;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
#if PLATFORM_CLOUD_RENDERING
using Unity.Simulation;
using UnityEditor;
using UnityEngine.CloudRendering;
#endif
using UnityEngine.Networking;
using System.Linq;
using UnityEngine.Rendering.PostProcessing;
using UnityStandardAssets.ImageEffects;


public class AgentManager : MonoBehaviour {
    public List<BaseFPSAgentController> agents = new List<BaseFPSAgentController>();
    protected int frameCounter;
    protected bool serverSideScreenshot;
    protected string robosimsClientToken = "";
    protected int robosimsPort = 8200;
    protected string robosimsHost = "127.0.0.1";
    protected string ENVIRONMENT_PREFIX = "AI2THOR_";
    private Texture2D tex;
    private Rect readPixelsRect;
    private int currentSequenceId;
    private int activeAgentId;
    private bool renderImage = true;
    private bool renderDepthImage;
    private bool renderSemanticSegmentation;
    private bool renderInstanceSegmentation;
    private bool initializedInstanceSeg;
    private bool renderNormalsImage;
    private bool renderFlowImage;
    private Socket sock = null;
    [SerializeField]
    public List<Camera> thirdPartyCameras = new List<Camera>();
    private Color[] agentColors = new Color[] { Color.blue, Color.yellow, Color.green, Color.red, Color.magenta, Color.grey };
    public int actionDuration = 3;
    private BaseFPSAgentController primaryAgent;
    private PhysicsSceneManager physicsSceneManager;
    private FifoServer.Client fifoClient = null;
    private enum serverTypes { WSGI, FIFO };
    private serverTypes serverType;
    private AgentState agentManagerState = AgentState.Emit;
    private bool fastActionEmit = true;

    // it is public to be accessible from the debug input field.
    public HashSet<string> agentManagerActions = new HashSet<string> { "Reset", "Initialize", "AddThirdPartyCamera", "UpdateThirdPartyCamera", "ChangeResolution", "CoordinateFromRaycastThirdPartyCamera", "ChangeQuality" };

    public bool doResetMaterials = false;
    public bool doResetColors = false;

    public const float DEFAULT_FOV = 90;
    public const float MAX_FOV = 180;
    public const float MIN_FOV = 0;


    public Bounds sceneBounds = UtilityFunctions.CreateEmptyBounds();
    public Bounds SceneBounds {
        get {
            if (sceneBounds.min.x == float.PositiveInfinity) {
                ResetSceneBounds();
            }
            return sceneBounds;
        }
        set {
            sceneBounds = value;
        }
    }

    void Awake() {

        tex = new Texture2D(UnityEngine.Screen.width, UnityEngine.Screen.height, TextureFormat.RGB24, false);
        readPixelsRect = new Rect(0, 0, UnityEngine.Screen.width, UnityEngine.Screen.height);

#if !UNITY_WEBGL
        // Creates warning for WebGL
        // https://forum.unity.com/threads/rendering-without-using-requestanimationframe-for-the-main-loop.373331/
        Application.targetFrameRate = 3000;
#else
            Debug.unityLogger.logEnabled = Debug.isDebugBuild;
#endif

        QualitySettings.vSyncCount = 0;
        robosimsPort = LoadIntVariable(robosimsPort, "PORT");
        robosimsHost = LoadStringVariable(robosimsHost, "HOST");
        serverSideScreenshot = LoadBoolVariable(serverSideScreenshot, "SERVER_SIDE_SCREENSHOT");
        robosimsClientToken = LoadStringVariable(robosimsClientToken, "CLIENT_TOKEN");
        serverType = (serverTypes)Enum.Parse(typeof(serverTypes), LoadStringVariable(serverTypes.WSGI.ToString(), "SERVER_TYPE").ToUpper());
        if (serverType == serverTypes.FIFO) {
            string serverPipePath = LoadStringVariable(null, "FIFO_SERVER_PIPE_PATH");
            string clientPipePath = LoadStringVariable(null, "FIFO_CLIENT_PIPE_PATH");

            Debug.Log("creating fifo server: " + serverPipePath);
            Debug.Log("client fifo path: " + clientPipePath);
            this.fifoClient = FifoServer.Client.GetInstance(serverPipePath, clientPipePath);

        }

        bool trainPhase = true;
        trainPhase = LoadBoolVariable(trainPhase, "TRAIN_PHASE");

        // read additional configurations for model
        // agent speed and action length
        string prefix = trainPhase ? "TRAIN_" : "TEST_";

        actionDuration = LoadIntVariable(actionDuration, prefix + "ACTION_LENGTH");

    }

    void Start() {
        // default primary agent's agentController type to "PhysicsRemoteFPSAgentController"
        initializePrimaryAgent();
        #if PLATFORM_CLOUD_RENDERING    
        // must wrap this in PLATFORM_CLOUDRENDERING
        // needed to ensure that the com.unity.simulation.capture package
        // gets initialized
        var instance = Manager.Instance;
        Camera camera = this.primaryAgent.gameObject.GetComponentInChildren<Camera>();
        camera.targetTexture = createRenderTexture(Screen.width, Screen.height);
        #endif

        primaryAgent.actionDuration = this.actionDuration;
        // this.agents.Add (primaryAgent);
        physicsSceneManager = GameObject.Find("PhysicsSceneManager").GetComponent<PhysicsSceneManager>();

        // auto set agentMode to default for the web demo
#if UNITY_WEBGL
        physicsSceneManager.UnpausePhysicsAutoSim();
        primaryAgent.InitializeBody();
        JavaScriptInterface jsInterface = primaryAgent.GetComponent<JavaScriptInterface>();
        if (jsInterface != null) {
            jsInterface.enabled = true;
        }
#endif

        StartCoroutine(EmitFrame());
    }

    private void initializePrimaryAgent() {
        if (this.PrimaryAgent == null) {
            SetUpPhysicsController();
        }
    }

    public void Initialize(ServerAction action) {
        // first parse agentMode and agentControllerType
        //"default" agentMode can use either default or "stochastic" agentControllerType
        //"locobot" agentMode can use either default or "stochastic" agentControllerType
        //"drone" agentMode can ONLY use "drone" agentControllerType, and NOTHING ELSE (for now?)
        if (action.agentMode.ToLower() == "default") {
            if (action.agentControllerType.ToLower() != "physics" && action.agentControllerType.ToLower() != "stochastic") {
                Debug.Log("default mode must use either physics or stochastic controller. Defaulting to physics");
                action.agentControllerType = "";
                SetUpPhysicsController();
            }

            // if not stochastic, default to physics controller
            if (action.agentControllerType.ToLower() == "physics") {
                // set up physics controller
                SetUpPhysicsController();
            }

            // if stochastic, set up stochastic controller
            else if (action.agentControllerType.ToLower() == "stochastic") {
                // set up stochastic controller
                primaryAgent.actionFinished(success: false, errorMessage: "Invalid combination of agentControllerType=stochastic and agentMode=default. In order to use agentControllerType=stochastic, agentMode must be set to stochastic");
		return;
            }
        } else if (action.agentMode.ToLower() == "locobot") {
            // if not stochastic, default to stochastic
            if (action.agentControllerType.ToLower() != "stochastic") {
                Debug.Log("'bot' mode only fully supports the 'stochastic' controller type at the moment. Forcing agentControllerType to 'stochastic'");
                action.agentControllerType = "stochastic";
            } 
            // LocobotController is a subclass of Stochastic which just the agentMode (VisibilityCapsule) changed
            SetUpLocobotController(action);
            
        } else if (action.agentMode.ToLower() == "drone") {
            if (action.agentControllerType.ToLower() != "drone") {
                Debug.Log("'drone' agentMode is only compatible with 'drone' agentControllerType, forcing agentControllerType to 'drone'");
                action.agentControllerType = "drone";
            }
            SetUpDroneController(action);
        } else if (action.agentMode.ToLower() == "stretch") {
                SetUpStretchController(action);

                action.autoSimulation = false;
                physicsSceneManager.MakeAllObjectsMoveable();

                if (action.massThreshold.HasValue) {
                    if (action.massThreshold.Value > 0.0) {
                        SetUpMassThreshold(action.massThreshold.Value);
                    } else {
                        var error = "massThreshold must have nonzero value - invalid value: " + action.massThreshold.Value;
                        Debug.Log(error);
                        primaryAgent.actionFinished(false, error);
                        return;
                    }
                }
        } else if (action.agentMode.ToLower() == "arm") {

            if (action.agentControllerType == "") {
                action.agentControllerType = "mid-level";
                Debug.Log("Defaulting to mid-level.");
            }

            if (action.agentControllerType.ToLower() != "low-level" && action.agentControllerType.ToLower() != "mid-level") {
                var error = "'arm' mode must use either low-level or mid-level controller.";
                Debug.Log(error);
                primaryAgent.actionFinished(success: false, errorMessage: error);
                return;
            } else if (action.agentControllerType.ToLower() == "mid-level") {
                // set up physics controller
                SetUpArmController(true);
                // the arm should currently be used only with autoSimulation off
                // as we manually control Physics during its movement
                action.autoSimulation = false;
                physicsSceneManager.MakeAllObjectsMoveable();

                if (action.massThreshold.HasValue) {
                    if (action.massThreshold.Value > 0.0) {
                        SetUpMassThreshold(action.massThreshold.Value);
                    } else {
                        var error = "massThreshold must have nonzero value - invalid value: " + action.massThreshold.Value;
                        Debug.Log(error);
                        primaryAgent.actionFinished(success: false, errorMessage: error);
                        return;
                    }
                }

            } else {
                var error = "unsupported";
                Debug.Log(error);
                primaryAgent.actionFinished(success: false, errorMessage: error);
                return;
            }
        }

        primaryAgent.ProcessControlCommand(action.dynamicServerAction);
        Time.fixedDeltaTime = action.fixedDeltaTime.GetValueOrDefault(Time.fixedDeltaTime);
        if (action.targetFrameRate > 0) {
            Application.targetFrameRate = action.targetFrameRate;
        }

        primaryAgent.IsVisible = action.makeAgentsVisible;
        this.renderSemanticSegmentation = action.renderSemanticSegmentation;
        this.renderDepthImage = action.renderDepthImage;
        this.renderNormalsImage = action.renderNormalsImage;
        this.renderInstanceSegmentation = this.initializedInstanceSeg = action.renderInstanceSegmentation;
        this.renderFlowImage = action.renderFlowImage;
        this.fastActionEmit = action.fastActionEmit;
        // we default Physics.autoSimulation to False in the built Player, but
        // set ServerAction.autoSimulation = True for backwards compatibility. Keeping
        // this value False allows the user complete control of all Physics Simulation
        // if they need deterministic simulations.
        Physics.autoSimulation = action.autoSimulation;
        Physics.autoSyncTransforms = Physics.autoSimulation;

        if (action.alwaysReturnVisibleRange) {
            ((PhysicsRemoteFPSAgentController)primaryAgent).alwaysReturnVisibleRange = action.alwaysReturnVisibleRange;
        }
        print("start addAgents");
        StartCoroutine(addAgents(action));

    }
    
    private void SetUpLocobotController(ServerAction action) {
        this.agents.Clear();
        // force snapToGrid to be false since we are stochastic
        action.snapToGrid = false;
        BaseAgentComponent baseAgentComponent = GameObject.FindObjectOfType<BaseAgentComponent>();
        primaryAgent = createAgentType(typeof(LocobotFPSAgentController), baseAgentComponent);
    }

    private void SetUpDroneController(ServerAction action) {
        this.agents.Clear();
        // force snapToGrid to be false
        action.snapToGrid = false;
        BaseAgentComponent baseAgentComponent = GameObject.FindObjectOfType<BaseAgentComponent>();
        primaryAgent = createAgentType(typeof(DroneFPSAgentController), baseAgentComponent);
    }

    private void SetUpStretchController(ServerAction action) {
        this.agents.Clear();
        // force snapToGrid to be false
        action.snapToGrid = false;
        BaseAgentComponent baseAgentComponent = GameObject.FindObjectOfType<BaseAgentComponent>();
        primaryAgent = createAgentType(typeof(StretchAgentController), baseAgentComponent);
    }

    // note: this doesn't take a ServerAction because we don't have to force the snpToGrid bool
    // to be false like in other controller types.
    public void SetUpPhysicsController() {
        this.agents.Clear();
        BaseAgentComponent baseAgentComponent = GameObject.FindObjectOfType<BaseAgentComponent>();
        primaryAgent = createAgentType(typeof(PhysicsRemoteFPSAgentController), baseAgentComponent);
    }

    private BaseFPSAgentController createAgentType(Type agentType, BaseAgentComponent agentComponent) {
        BaseFPSAgentController agent = Activator.CreateInstance(agentType, new object[]{agentComponent, this}) as BaseFPSAgentController;
        this.agents.Add(agent);
        return agent;
    }

    private void SetUpArmController(bool midLevelArm) {
        this.agents.Clear();
        BaseAgentComponent baseAgentComponent = GameObject.FindObjectOfType<BaseAgentComponent>();
        primaryAgent = createAgentType(typeof(ArmAgentController), baseAgentComponent);
        var handObj = primaryAgent.transform.FirstChildOrDefault((x) => x.name == "robot_arm_rig_gripper");
        handObj.gameObject.SetActive(true);
    }

    // on initialization of agentMode = "arm" and agentControllerType = "mid-level"
    // if mass threshold should be used to prevent arm from knocking over objects that
    // are too big (table, sofa, shelf, etc) use this
    private void SetUpMassThreshold(float massThreshold) {
        CollisionListener.useMassThreshold = true;
        CollisionListener.massThreshold = massThreshold;
        primaryAgent.MakeObjectsStaticKinematicMassThreshold();
    }

    // return reference to primary agent in case we need a reference to the primary
    public BaseFPSAgentController PrimaryAgent {
        get => this.primaryAgent;
    }

    private IEnumerator addAgents(ServerAction action) {
        yield return null;
        Vector3[] reachablePositions = primaryAgent.getReachablePositions(2.0f);
        for (int i = 1; i < action.agentCount && this.agents.Count < Math.Min(agentColors.Length, action.agentCount); i++) {
            action.x = reachablePositions[i + 4].x;
            action.y = reachablePositions[i + 4].y;
            action.z = reachablePositions[i + 4].z;
            addAgent(action);
            yield return null; // must do this so we wait a frame so that when we CapsuleCast we see the most recently added agent
        }
        for (int i = 0; i < this.agents.Count; i++) {
            this.agents[i].m_Camera.depth = 1;
        }
        this.agents[0].m_Camera.depth = 9999;

        if (action.startAgentsRotatedBy != 0f) {
            RotateAgentsByRotatingUniverse(action.startAgentsRotatedBy);
        } else {
            ResetSceneBounds();
        }

        this.agentManagerState = AgentState.ActionComplete;
    }

    public void ResetSceneBounds() {
        // Recording initially disabled renderers and scene bounds
        sceneBounds = UtilityFunctions.CreateEmptyBounds();
        foreach (Renderer r in GameObject.FindObjectsOfType<Renderer>()) {
            if (r.enabled) {
                sceneBounds.Encapsulate(r.bounds);
            }
        }
    }

    public void RotateAgentsByRotatingUniverse(float rotation) {
        List<Quaternion> startAgentRots = new List<Quaternion>();

        foreach (BaseFPSAgentController agent in this.agents) {
            startAgentRots.Add(agent.transform.rotation);
        }

        GameObject superObject = GameObject.Find("SuperTopLevel");
        if (superObject == null) {
            superObject = new GameObject("SuperTopLevel");
        }

        superObject.transform.position = this.agents[0].transform.position;

        List<GameObject> topLevelObjects = new List<GameObject>();
        foreach (GameObject go in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()) {
            topLevelObjects.Add(go);
            go.transform.SetParent(superObject.transform);
        }

        superObject.transform.rotation = Quaternion.Euler(0f, rotation, 0f);
        foreach (GameObject go in topLevelObjects) {
            go.transform.SetParent(null);
        }

        for (int i = 0; i < this.agents.Count; i++) {
            agents[i].transform.rotation = startAgentRots[i];
        }

        ResetSceneBounds();
    }


    public void registerAsThirdPartyCamera(Camera camera) {
        this.thirdPartyCameras.Add(camera);
        // camera.gameObject.AddComponent(typeof(ImageSynthesis));
    }

    // If fov is <= min or > max, return defaultVal, else return fov
    private float ClampFieldOfView(float fov, float defaultVal = 90f, float min = 0f, float max = 180f) {
        return (fov <= min || fov > max) ? defaultVal : fov;
    }

    public void updateImageSynthesis(bool status) {
        foreach (var agent in this.agents) {
            agent.updateImageSynthesis(status);
        }
    }

    public void updateThirdPartyCameraImageSynthesis(bool status) {
        if (status) {
            foreach (var camera in this.thirdPartyCameras) {
                GameObject gameObject = camera.gameObject;
                var imageSynthesis = gameObject.GetComponentInChildren<ImageSynthesis>() as ImageSynthesis;
                if (imageSynthesis == null) {
                    gameObject.AddComponent(typeof(ImageSynthesis));
                }
                imageSynthesis = gameObject.GetComponentInChildren<ImageSynthesis>() as ImageSynthesis;
                imageSynthesis.enabled = status;
            }
        }
    }

    private void updateCameraProperties(
        Camera camera,
        Vector3 position,
        Vector3 rotation,
        float fieldOfView,
        string skyboxColor,
        bool? orthographic,
        float? orthographicSize,
        float? nearClippingPlane,
        float? farClippingPlane
    ) {
        if (orthographic != true && orthographicSize != null) {
            throw new InvalidOperationException(
                $"orthographicSize(: {orthographicSize}) can only be set when orthographic=True.\n" +
                "Otherwise, we use assume perspective camera setting." +
                "Hint: call .step(..., orthographic=True)."
            );
        }

        // update the position and rotation
        camera.gameObject.transform.position = position;
        camera.gameObject.transform.eulerAngles = rotation;

        // updates the camera's perspective
        camera.fieldOfView = fieldOfView;
        if (orthographic != null) {
            camera.orthographic = (bool)orthographic;
            if (orthographic == true && orthographicSize != null) {
                camera.orthographicSize = (float)orthographicSize;
            }
        }

        //updates camera near and far clipping planes
        //default to near and far clipping planes of agent camera, which are currently
        //static values and are not exposed in anything like Initialize
        if (nearClippingPlane != null) {
            camera.nearClipPlane = (float)nearClippingPlane;
        }

        //default to primary agent's near clip plane value
        else {
            camera.nearClipPlane = this.primaryAgent.m_Camera.nearClipPlane;
        }

        if (farClippingPlane != null) {
            camera.farClipPlane = (float)farClippingPlane;
        }

        //default to primary agent's far clip plane value
        else {
            camera.farClipPlane = this.primaryAgent.m_Camera.farClipPlane;
        }

        // supports a solid color skybox, which work well with videos and images (i.e., white/black/orange/blue backgrounds)
        if (skyboxColor == "default") {
            camera.clearFlags = CameraClearFlags.Skybox;
        } else if (skyboxColor != null) {
            Color color;
            bool successfullyParsed = ColorUtility.TryParseHtmlString(skyboxColor, out color);
            if (successfullyParsed) {
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = color;
            } else {
                throw new ArgumentException($"Invalid skyboxColor: {skyboxColor}! Cannot be parsed as an HTML color.");
            }
        }
        this.activeAgent().actionFinished(success: true);
    }

    private void assertFovInBounds(float fov) {
        if (fov <= MIN_FOV || fov >= MAX_FOV) {
            throw new ArgumentOutOfRangeException($"fieldOfView: {fov} must be in {MIN_FOV} < fieldOfView > {MIN_FOV}.");
        }
    }

    public void AddThirdPartyCamera(
        Vector3 position,
        Vector3 rotation,
        float fieldOfView = DEFAULT_FOV,
        string skyboxColor = null,
        bool orthographic = false,
        float? orthographicSize = null,
        float? nearClippingPlane = null,
        float? farClippingPlane = null,
        string antiAliasing = "none"
    ) {
        // adds error if fieldOfView is out of bounds
        assertFovInBounds(fov: fieldOfView);

        GameObject gameObject = GameObject.Instantiate(Resources.Load("ThirdPartyCameraTemplate")) as GameObject;
        gameObject.name = "ThirdPartyCamera" + thirdPartyCameras.Count;
        Camera camera = gameObject.GetComponentInChildren<Camera>();

        // set up returned image
        camera.cullingMask = ~(1 << 11);
        if (renderDepthImage || renderSemanticSegmentation || renderInstanceSegmentation || renderNormalsImage || renderFlowImage) {
            gameObject.AddComponent(typeof(ImageSynthesis));
        }
        
        #if PLATFORM_CLOUD_RENDERING
        camera.targetTexture = createRenderTexture(this.primaryAgent.m_Camera.pixelWidth, this.primaryAgent.m_Camera.targetTexture.height);
        #endif

        antiAliasing = antiAliasing.ToLower();
        PostProcessLayer postProcessLayer = gameObject.GetComponentInChildren<PostProcessLayer>();
        if (antiAliasing == "none") {
            postProcessLayer.enabled = false;
        } else {
            postProcessLayer.enabled = true;
            switch (antiAliasing) {
                case "fxaa":
                    postProcessLayer.antialiasingMode = PostProcessLayer.Antialiasing.FastApproximateAntialiasing;
                    break;
                case "smaa":
                    postProcessLayer.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
                    break;
                case "taa":
                    postProcessLayer.antialiasingMode = PostProcessLayer.Antialiasing.TemporalAntialiasing;
                    break;
                default:
                    break;
            }
        }

        thirdPartyCameras.Add(camera);
        updateCameraProperties(
            camera: camera,
            position: position,
            rotation: rotation,
            fieldOfView: fieldOfView,
            skyboxColor: skyboxColor,
            orthographic: orthographic,
            orthographicSize: orthographicSize,
            nearClippingPlane: nearClippingPlane,
            farClippingPlane: farClippingPlane
        );
    }

    // helper that can be used when converting Dictionary<string, float> to a Vector3.
    private Vector3 parseOptionalVector3(
        OptionalVector3 optionalVector3,
        Vector3 defaultsOnNull
    ) {
        if (optionalVector3 == null) {
            return defaultsOnNull;
        }

        return new Vector3(
            x: optionalVector3.x == null ? defaultsOnNull.x : (float)optionalVector3.x,
            y: optionalVector3.y == null ? defaultsOnNull.y : (float)optionalVector3.y,
            z: optionalVector3.z == null ? defaultsOnNull.z : (float)optionalVector3.z
        );
    }

    // Here, we don't want some dimensions set. For instance, set x, but not y.
    public class OptionalVector3 {
        public float? x = null;
        public float? y = null;
        public float? z = null;
    }

    // note that using a using a Dictionary<string, float> allows for only x, y, or z
    // to be passed in, individually, whereas using Vector3 would require each of x/y/z.
    public void UpdateThirdPartyCamera(
        int thirdPartyCameraId = 0,
        OptionalVector3 position = null,
        OptionalVector3 rotation = null,
        float? fieldOfView = null,
        string skyboxColor = null,
        bool? orthographic = null,
        float? orthographicSize = null,
        float? nearClippingPlane = null,
        float? farClippingPlane = null
    ) {
        // adds error if fieldOfView is out of bounds
        if (fieldOfView != null) {
            assertFovInBounds(fov: (float)fieldOfView);
        }

        // count is out of bounds
        if (thirdPartyCameraId >= thirdPartyCameras.Count || thirdPartyCameraId < 0) {
            throw new ArgumentOutOfRangeException(
                $"thirdPartyCameraId: {thirdPartyCameraId} (int: default=0) must in 0 <= thirdPartyCameraId < len(thirdPartyCameras)={thirdPartyCameras.Count}."
            );
        }

        Camera thirdPartyCamera = thirdPartyCameras[thirdPartyCameraId];

        // keeps positions at default values, if unspecified.
        Vector3 oldPosition = thirdPartyCamera.gameObject.transform.position;
        Vector3 targetPosition = parseOptionalVector3(optionalVector3: position, defaultsOnNull: oldPosition);

        // keeps rotations at default values, if unspecified.
        Vector3 oldRotation = thirdPartyCamera.gameObject.transform.localEulerAngles;
        Vector3 targetRotation = parseOptionalVector3(optionalVector3: rotation, defaultsOnNull: oldRotation);

        updateCameraProperties(
            camera: thirdPartyCamera,
            position: targetPosition,
            rotation: targetRotation,
            fieldOfView: fieldOfView == null ? thirdPartyCamera.fieldOfView : (float)fieldOfView,
            skyboxColor: skyboxColor,
            orthographic: orthographic,
            orthographicSize: orthographicSize,
            nearClippingPlane: nearClippingPlane,
            farClippingPlane: farClippingPlane
        );
    }

    private void addAgent(ServerAction action) {
        Vector3 clonePosition = new Vector3(action.x, action.y, action.z);
        BaseAgentComponent componentClone = UnityEngine.Object.Instantiate(primaryAgent.baseAgentComponent);
        var agent = createAgentType(primaryAgent.GetType(), componentClone);
        agent.IsVisible = action.makeAgentsVisible;
        agent.actionDuration = this.actionDuration;
        // clone.m_Camera.targetDisplay = this.agents.Count;
        componentClone.transform.position = clonePosition;
        UpdateAgentColor(agent, agentColors[this.agents.Count]);
        
#if PLATFORM_CLOUD_RENDERING
        agent.m_Camera.targetTexture = createRenderTexture(this.primaryAgent.m_Camera.targetTexture.width, this.primaryAgent.m_Camera.targetTexture.height);
#endif 
        agent.ProcessControlCommand(action.dynamicServerAction);
    }

    private Vector3 agentStartPosition(BaseFPSAgentController agent) {

        Transform t = agent.transform;
        Vector3[] castDirections = new Vector3[] { t.forward, t.forward * -1, t.right, t.right * -1 };

        RaycastHit maxHit = new RaycastHit();
        Vector3 maxDirection = Vector3.zero;

        RaycastHit hit;
        CharacterController charContr = agent.m_CharacterController;
        Vector3 p1 = t.position + charContr.center + Vector3.up * -charContr.height * 0.5f;
        Vector3 p2 = p1 + Vector3.up * charContr.height;
        foreach (Vector3 d in castDirections) {

            if (Physics.CapsuleCast(p1, p2, charContr.radius, d, out hit)) {
                if (hit.distance > maxHit.distance) {
                    maxHit = hit;
                    maxDirection = d;
                }

            }
        }

        if (maxHit.distance > (charContr.radius * 5)) {
            return t.position + (maxDirection * (charContr.radius * 4));

        }

        return Vector3.zero;
    }

    public void UpdateAgentColor(BaseFPSAgentController agent, Color color) {
        foreach (MeshRenderer r in agent.gameObject.GetComponentsInChildren<MeshRenderer>() as MeshRenderer[]) {
            foreach (Material m in r.materials) {
                if (m.name.Contains("Agent_Color_Mat")) {
                    m.color = color;
                }
            }

        }
    }

    public IEnumerator ResetCoroutine(ServerAction response) {
        // Setting all the agents invisible here is silly but necessary
        // as otherwise the FirstPersonCharacterCull.cs script will
        // try to disable renderers that are invalid (but not null)
        // as the scene they existed in has changed.
        for (int i = 0; i < agents.Count; i++) {
            Destroy(agents[i].baseAgentComponent);
        }
        yield return null;

        if (string.IsNullOrEmpty(response.sceneName)) {
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        } else {
            UnityEngine.SceneManagement.SceneManager.LoadScene(response.sceneName);
        }
    }

    public void resetMaterials() {
        ColorChanger colorChangeComponent = physicsSceneManager.GetComponent<ColorChanger>();
        colorChangeComponent.ResetMaterials();
        doResetMaterials = false;
        doResetColors = false;
    }

    public void resetColors() {
        ColorChanger colorChangeComponent = physicsSceneManager.GetComponent<ColorChanger>();
        colorChangeComponent.ResetColors();
        doResetColors = false;
    }

    public void Reset(ServerAction response) {
        if (doResetMaterials) {
            resetMaterials();
        } else if (doResetColors) {
            resetColors();
        }
        StartCoroutine(ResetCoroutine(response));
    }

    public bool SwitchScene(string sceneName) {
        if (!string.IsNullOrEmpty(sceneName)) {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            return true;
        }
        return false;
    }

    // Decide whether agent has stopped actions
    // And if we need to capture a new frame

    private void Update() {
        physicsSceneManager.isSceneAtRest = true;// assume the scene is at rest by default
    }


    private void captureScreenAsync(List<KeyValuePair<string, byte[]>> payload, string key, Camera camera) {
        RenderTexture tt = camera.targetTexture;
        RenderTexture.active = tt;
        camera.Render();
        AsyncGPUReadback.Request(tt, 0, (request) =>
        {
            if (!request.hasError) {
                var data = request.GetData<byte>().ToArray();
                payload.Add(new KeyValuePair<string, byte[]>(key, data));
            }
            else
            {
                Debug.Log("Request error: " + request.hasError);
            }
        });
    }

    private byte[] captureScreen() {
        if (tex.height != UnityEngine.Screen.height ||
            tex.width != UnityEngine.Screen.width) {
            tex = new Texture2D(UnityEngine.Screen.width, UnityEngine.Screen.height, TextureFormat.RGB24, false);
            readPixelsRect = new Rect(0, 0, UnityEngine.Screen.width, UnityEngine.Screen.height);
        }
        tex.ReadPixels(readPixelsRect, 0, 0);
        tex.Apply();
        return tex.GetRawTextureData();
    }


    private void addThirdPartyCameraImage(List<KeyValuePair<string, byte[]>> payload, Camera camera) {
#if PLATFORM_CLOUD_RENDERING
        captureScreenAsync(payload, "image-thirdParty-camera", camera);
#else
        RenderTexture.active = camera.activeTexture;
        camera.Render();
        payload.Add(new KeyValuePair<string, byte[]>("image-thirdParty-camera", captureScreen()));
#endif
    }

    private void addImage(List<KeyValuePair<string, byte[]>> payload, BaseFPSAgentController agent) {
        if (this.renderImage) {

#if PLATFORM_CLOUD_RENDERING
            captureScreenAsync(payload, "image", agent.m_Camera);
#else
            // XXX may not need this since we call render in captureScreenAsync
            if (this.agents.Count > 1 || this.thirdPartyCameras.Count > 0) {
                RenderTexture.active = agent.m_Camera.activeTexture;
                agent.m_Camera.Render();
            }
            payload.Add(new KeyValuePair<string, byte[]>("image", captureScreen()));
#endif
        }
    }

    private void resetImageSynthesis(Camera camera) {
        ImageSynthesis imageSynthesis = camera.gameObject.GetComponentInChildren<ImageSynthesis>();
        if (imageSynthesis != null && imageSynthesis.enabled) {
            imageSynthesis.OnCameraChange();
            imageSynthesis.OnSceneChange();
        }
    }

    private void resetAllImageSynthesis() {
        foreach (var agent in this.agents) {
            resetImageSynthesis(agent.m_Camera);
        }

        foreach (var camera in this.thirdPartyCameras) {
            resetImageSynthesis(camera);
        }
    }

    public IEnumerator WaitOnResolutionChange(int width, int height) {
        while (Screen.width != width || Screen.height != height) {
            yield return null;
        }
        this.resetAllImageSynthesis();
        this.primaryAgent.actionFinished(true);
    }

    public void ChangeQuality(string quality) {
        string[] names = QualitySettings.names;
        for (int i = 0; i < names.Length; i++) {
            if (names[i] == quality) {
                QualitySettings.SetQualityLevel(i, true);
                break;
            }
        }
        
        this.primaryAgent.actionFinished(true);
    }


    public void ChangeResolution(int x, int y) {
        Screen.SetResolution(width: x, height: y, false);
        Debug.Log("current screen resolution pre change: " + Screen.width + " height" + Screen.height);
#if PLATFORM_CLOUD_RENDERING
        foreach (var agent in this.agents) {
            var rt = agent.m_Camera.targetTexture;
            rt.Release();
            Destroy(rt);
           agent.m_Camera.targetTexture = createRenderTexture(x, y); 
        }
        
        foreach (var camera in this.thirdPartyCameras) {
            var rt = camera.targetTexture;
            rt.Release();
            Destroy(rt);
           camera.targetTexture = createRenderTexture(x, y); 
        }
#endif
        StartCoroutine(WaitOnResolutionChange(width: x, height: y));
    }

    private void addObjectImage(List<KeyValuePair<string, byte[]>> payload, BaseFPSAgentController agent, ref MetadataWrapper metadata) {
        if (this.renderInstanceSegmentation || this.renderSemanticSegmentation) {
            if (!agent.imageSynthesis.hasCapturePass("_id")) {
                Debug.LogError("Object Image not available in imagesynthesis - returning empty image");
            }
            byte[] bytes = agent.imageSynthesis.Encode("_id");
            payload.Add(new KeyValuePair<string, byte[]>("image_ids", bytes));

            List<ColorId> colors = new List<ColorId>();
            foreach (Color key in agent.imageSynthesis.colorIds.Keys) {
                ColorId cid = new ColorId();
                cid.color = new ushort[] {
                    (ushort)Math.Round (key.r * 255),
                    (ushort)Math.Round (key.g * 255),
                    (ushort)Math.Round (key.b * 255)
                };

                cid.name = agent.imageSynthesis.colorIds[key];
                colors.Add(cid);
            }
            metadata.colors = colors.ToArray();

        }
    }

    private void addImageSynthesisImage(List<KeyValuePair<string, byte[]>> payload, ImageSynthesis synth, bool flag, string captureName, string fieldName) {
        if (flag) {
            if (!synth.hasCapturePass(captureName)) {
                Debug.LogError(captureName + " not available - sending empty image");
            }
            byte[] bytes = synth.Encode(captureName);
            payload.Add(new KeyValuePair<string, byte[]>(fieldName, bytes));


        }
    }

    // Used for benchmarking only the server-side
    // no call is made to the Python side
    private IEnumerator EmitFrameNoClient() {
        frameCounter += 1;

        bool shouldRender = this.renderImage;

        if (shouldRender) {
            // we should only read the screen buffer after rendering is complete
            yield return new WaitForEndOfFrame();
            // must wait an additional frame when in synchronous mode otherwise the frame lags
            yield return new WaitForEndOfFrame();
        }

        // NOTE: sequenceId is required in DynamicServerAction.
        string msg = "{\"action\": \"RotateRight\", \"timeScale\": 90.0, \"sequenceId\": 0}";
        ProcessControlCommand(msg);
    }

    private void createPayload(MultiAgentMetadata multiMeta, ThirdPartyCameraMetadata[] cameraMetadata, List<KeyValuePair<string, byte[]>> renderPayload, bool shouldRender) {

        multiMeta.agents = new MetadataWrapper[this.agents.Count];
        multiMeta.activeAgentId = this.activeAgentId;
        multiMeta.sequenceId = this.currentSequenceId;

        RenderTexture currentTexture = null;

        if (shouldRender) {
            currentTexture = RenderTexture.active;
            for (int i = 0; i < this.thirdPartyCameras.Count; i++) {
                ThirdPartyCameraMetadata cMetadata = new ThirdPartyCameraMetadata();
                Camera camera = thirdPartyCameras.ToArray()[i];
                cMetadata.thirdPartyCameraId = i;
                cMetadata.position = camera.gameObject.transform.position;
                cMetadata.rotation = camera.gameObject.transform.eulerAngles;
                cMetadata.fieldOfView = camera.fieldOfView;
                cameraMetadata[i] = cMetadata;
                ImageSynthesis imageSynthesis = camera.gameObject.GetComponentInChildren<ImageSynthesis>() as ImageSynthesis;
                addThirdPartyCameraImage(renderPayload, camera);
                addImageSynthesisImage(renderPayload, imageSynthesis, this.renderDepthImage, "_depth", "image_thirdParty_depth");
                addImageSynthesisImage(renderPayload, imageSynthesis, this.renderNormalsImage, "_normals", "image_thirdParty_normals");
                addImageSynthesisImage(renderPayload, imageSynthesis, this.renderInstanceSegmentation, "_id", "image_thirdParty_image_ids");
                addImageSynthesisImage(renderPayload, imageSynthesis, this.renderSemanticSegmentation, "_class", "image_thirdParty_classes");
                addImageSynthesisImage(renderPayload, imageSynthesis, this.renderSemanticSegmentation, "_flow", "image_thirdParty_flow");// XXX fix this in a bit
            }
        }
        for (int i = 0; i < this.agents.Count; i++) {
            BaseFPSAgentController agent = this.agents[i];
            MetadataWrapper metadata = agent.generateMetadataWrapper();
            // This value may never change, but the purpose is to provide a way
            //  to be backwards compatible in the future by knowing the output format
            //  so that it can be converted if necessary on the Python side
            metadata.depthFormat = DepthFormat.Meters.ToString();
            metadata.agentId = i;

            // we don't need to render the agent's camera for the first agent
            
            if (shouldRender) {
                addImage(renderPayload, agent);
                addImageSynthesisImage(renderPayload, agent.imageSynthesis, this.renderDepthImage, "_depth", "image_depth");
                addImageSynthesisImage(renderPayload, agent.imageSynthesis, this.renderNormalsImage, "_normals", "image_normals");
                addObjectImage(renderPayload, agent, ref metadata);
                addImageSynthesisImage(renderPayload, agent.imageSynthesis, this.renderSemanticSegmentation, "_class", "image_classes");
                addImageSynthesisImage(renderPayload, agent.imageSynthesis, this.renderFlowImage, "_flow", "image_flow");
                metadata.thirdPartyCameras = cameraMetadata;
            }

            multiMeta.agents[i] = metadata;
        }

        if (shouldRender) {
            RenderTexture.active = currentTexture;
        }


    }

    private string serializeMetadataJson(MultiAgentMetadata multiMeta) {
        var jsonResolver = new ShouldSerializeContractResolver();
        return Newtonsoft.Json.JsonConvert.SerializeObject(
            multiMeta,
            Newtonsoft.Json.Formatting.None,
            new Newtonsoft.Json.JsonSerializerSettings() {
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                ContractResolver = jsonResolver
            }
        );
    }

    private bool canEmit() {
        bool emit = true;
        foreach (BaseFPSAgentController agent in this.agents) {
            if (agent.agentState != AgentState.Emit) {
                emit = false;
                break;
            }
        }

        return this.agentManagerState == AgentState.Emit && emit;
    }

    private RenderTexture createRenderTexture(int width, int height) {
        RenderTexture rt = new RenderTexture(width: width, height: height,depth:0, GraphicsFormat.R8G8B8A8_UNorm);
        rt.antiAliasing = 4;
        if (rt.Create()) {
            Debug.Log(" created render texture with width= " + width + " height=" + height);
            return rt;
        } else {
            // throw exception ?
            Debug.LogError("Could not create a renderTexture");
            return null;
        }

    }

    public IEnumerator EmitFrame() {
        while (true) {
            bool shouldRender = this.renderImage && serverSideScreenshot;
            yield return new WaitForEndOfFrame();

            frameCounter += 1;
            if (this.agentManagerState == AgentState.ActionComplete) {
                this.agentManagerState = AgentState.Emit;
            }

            foreach (BaseFPSAgentController agent in this.agents) {
                if (agent.agentState == AgentState.ActionComplete) {
                    agent.agentState = AgentState.Emit;
                }
            }

            if (!this.canEmit()) {
                continue;
            }
            MultiAgentMetadata multiMeta = new MultiAgentMetadata();

            ThirdPartyCameraMetadata[] cameraMetadata = new ThirdPartyCameraMetadata[this.thirdPartyCameras.Count];
            List<KeyValuePair<string, byte[]>> renderPayload = new List<KeyValuePair<string, byte[]>>();
            createPayload(multiMeta, cameraMetadata, renderPayload, shouldRender);

#if UNITY_WEBGL
                JavaScriptInterface jsInterface = this.primaryAgent.GetComponent<JavaScriptInterface>();
                if (jsInterface != null) {
                    jsInterface.SendActionMetadata(serializeMetadataJson(multiMeta));
                }
#endif



#if !UNITY_WEBGL
            if (serverType == serverTypes.WSGI) {
                WWWForm form = new WWWForm();
                form.AddField("metadata", serializeMetadataJson(multiMeta));
                AsyncGPUReadback.WaitAllRequests();
                foreach (var item in renderPayload) {
                    form.AddBinaryData(item.Key, item.Value);
                }
                form.AddField("token", robosimsClientToken);


                if (this.sock == null) {
                    // Debug.Log("connecting to host: " + robosimsHost);
                    IPAddress host = IPAddress.Parse(robosimsHost);
                    IPEndPoint hostep = new IPEndPoint(host, robosimsPort);
                    this.sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    try {
                        this.sock.Connect(hostep);
                    } catch (SocketException e) {
                        var msg = e.ToString();
#if UNITY_EDITOR
                        break;
#endif
                        // wrapping the message in !UNITY_EDITOR to avoid unreachable code warning
#if !UNITY_EDITOR
                        Debug.Log("Socket exception: " + msg);
#endif
                    }
                }

                if (this.sock != null && this.sock.Connected) {
                    byte[] rawData = form.data;

                    string request = "POST /train HTTP/1.1\r\n" +
                    "Content-Length: " + rawData.Length.ToString() + "\r\n";

                    foreach (KeyValuePair<string, string> entry in form.headers) {
                        request += entry.Key + ": " + entry.Value + "\r\n";
                    }
                    request += "\r\n";

                    this.sock.Send(Encoding.ASCII.GetBytes(request));
                    this.sock.Send(rawData);

                    // waiting for a frame here keeps the Unity window in sync visually
                    // its not strictly necessary, but allows the interact() command to work properly
                    // and does not reduce the overall FPS
                    yield return new WaitForEndOfFrame();

                    byte[] headerBuffer = new byte[1024];
                    int bytesReceived = 0;
                    byte[] bodyBuffer = null;
                    int bodyBytesReceived = 0;
                    int contentLength = 0;

                    // read header
                    while (true) {
                        int received = this.sock.Receive(headerBuffer, bytesReceived, headerBuffer.Length - bytesReceived, SocketFlags.None);
                        if (received == 0) {
                            Debug.LogError("0 bytes received attempting to read header - connection closed");
                            break;
                        }

                        bytesReceived += received; ;
                        string headerMsg = Encoding.ASCII.GetString(headerBuffer, 0, bytesReceived);
                        int offset = headerMsg.IndexOf("\r\n\r\n");
                        if (offset > 0) {
                            contentLength = parseContentLength(headerMsg.Substring(0, offset));
                            bodyBuffer = new byte[contentLength];
                            bodyBytesReceived = bytesReceived - (offset + 4);
                            Array.Copy(headerBuffer, offset + 4, bodyBuffer, 0, bodyBytesReceived);
                            break;
                        }
                    }

                    // read body
                    while (bodyBytesReceived < contentLength) {
                        // check for 0 bytes received
                        int received = this.sock.Receive(bodyBuffer, bodyBytesReceived, bodyBuffer.Length - bodyBytesReceived, SocketFlags.None);
                        if (received == 0) {
                            Debug.LogError("0 bytes received attempting to read body - connection closed");
                            break;
                        }

                        bodyBytesReceived += received;
                        // Debug.Log("total bytes received: " + bodyBytesReceived);
                    }

                    string msg = Encoding.ASCII.GetString(bodyBuffer, 0, bodyBytesReceived);
                    ProcessControlCommand(msg);
                }
            } else if (serverType == serverTypes.FIFO) {

                byte[] msgPackMetadata = MessagePack.MessagePackSerializer.Serialize<MultiAgentMetadata>(multiMeta,
                    MessagePack.Resolvers.ThorContractlessStandardResolver.Options);

                this.fifoClient.SendMessage(FifoServer.FieldType.Metadata, msgPackMetadata);
                AsyncGPUReadback.WaitAllRequests();
                foreach (var item in renderPayload) {
                    this.fifoClient.SendMessage(FifoServer.Client.FormMap[item.Key], item.Value);
                }
                this.fifoClient.SendEOM();
                string msg = this.fifoClient.ReceiveMessage();
                ProcessControlCommand(msg);

                while (canEmit() && this.fastActionEmit) {
                    MetadataPatch patch = this.activeAgent().generateMetadataPatch();
                    patch.agentId = this.activeAgentId;
                    msgPackMetadata = MessagePack.MessagePackSerializer.Serialize(patch,
                    MessagePack.Resolvers.ThorContractlessStandardResolver.Options);
                    this.fifoClient.SendMessage(FifoServer.FieldType.MetadataPatch, msgPackMetadata);
                    this.fifoClient.SendEOM();
                    msg = this.fifoClient.ReceiveMessage();
                    ProcessControlCommand(msg);
                }
            }

            // if(droneMode)
            //{
            //    if (Time.timeScale == 0 && !Physics.autoSimulation && physicsSceneManager.physicsSimulationPaused)
            //    {
            //        DroneFPSAgentController agent_tmp = this.agents[0].GetComponent<DroneFPSAgentController>();
            //        Time.timeScale = agent_tmp.autoResetTimeScale;
            //        Physics.autoSimulation = true;
            //        physicsSceneManager.physicsSimulationPaused = false;
            //        agent_tmp.hasFixedUpdateHappened = false;
            //    }
            //}

#endif




        }


    }


    // Uniform entry point for both the test runner and the python server for step dispatch calls
    public void ProcessControlCommand(DynamicServerAction controlCommand) {
        this.renderInstanceSegmentation = this.initializedInstanceSeg;

        this.currentSequenceId = controlCommand.sequenceId;
        // the following are handled this way since they can be null
        this.renderImage = controlCommand.renderImage;
        this.activeAgentId = controlCommand.agentId;

        if (agentManagerActions.Contains(controlCommand.action)) {
            // let's look in this class for the action
            this.activeAgent().ProcessControlCommand(controlCommand: controlCommand, target: this);
        } else {
            // we only allow renderInstanceSegmentation to be flipped on
            // on a per step() basis, since by default the param is null
            // so we don't know if a request is meant to turn the param off
            // or if it is just the value by default
            // We only assign if its true to handle the case when renderInstanceSegmentation
            // was initialized to be true, but any particular step() may not set it
            // so we don't want to disable it inadvertently

            if (controlCommand.renderInstanceSegmentation) {
                this.renderInstanceSegmentation = true;
            }

            if (this.renderDepthImage ||
                this.renderSemanticSegmentation ||
                this.renderInstanceSegmentation ||
                this.renderNormalsImage
            ) {
                updateImageSynthesis(true);
                updateThirdPartyCameraImageSynthesis(true);
            }

            // let's look in the agent's set of actions for the action
            this.activeAgent().ProcessControlCommand(controlCommand: controlCommand);
        }
    }

    public BaseFPSAgentController GetActiveAgent() {
        return this.agents[activeAgentId];
    }

    private int parseContentLength(string header) {
        // Debug.Log("got header: " + header);
        string[] fields = header.Split(new char[] { '\r', '\n' });
        foreach (string field in fields) {
            string[] elements = field.Split(new char[] { ':' });
            if (elements[0].ToLower() == "content-length") {
                return Int32.Parse(elements[1].Trim());
            }
        }

        return 0;
    }

    private BaseFPSAgentController activeAgent() {
        return this.agents[activeAgentId];
    }

    private void ProcessControlCommand(string msg) {
        DynamicServerAction controlCommand = new DynamicServerAction(jsonMessage: msg);
        this.ProcessControlCommand(controlCommand);
    }

    // Extra helper functions
    protected string LoadStringVariable(string variable, string name) {
        string envVarName = ENVIRONMENT_PREFIX + name.ToUpper();
        string envVarValue = Environment.GetEnvironmentVariable(envVarName);
        return envVarValue == null ? variable : envVarValue;
    }

    protected int LoadIntVariable(int variable, string name) {
        string envVarName = ENVIRONMENT_PREFIX + name.ToUpper();
        string envVarValue = Environment.GetEnvironmentVariable(envVarName);
        return envVarValue == null ? variable : int.Parse(envVarValue);
    }

    protected float LoadFloatVariable(float variable, string name) {
        string envVarName = ENVIRONMENT_PREFIX + name.ToUpper();
        string envVarValue = Environment.GetEnvironmentVariable(envVarName);
        return envVarValue == null ? variable : float.Parse(envVarValue);
    }

    protected bool LoadBoolVariable(bool variable, string name) {
        string envVarName = ENVIRONMENT_PREFIX + name.ToUpper();
        string envVarValue = Environment.GetEnvironmentVariable(envVarName);
        return envVarValue == null ? variable : bool.Parse(envVarValue);
    }

}

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class MultiAgentMetadata {

    public MetadataWrapper[] agents;
    public ThirdPartyCameraMetadata[] thirdPartyCameras;
    public int activeAgentId;
    public int sequenceId;
}

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class ThirdPartyCameraMetadata {
    public int thirdPartyCameraId;
    public Vector3 position;
    public Vector3 rotation;
    public float fieldOfView;
}

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class MetadataPatch {
    public string lastAction;
    public string errorMessage;
    public string errorCode;
    public bool lastActionSuccess;
    public int agentId;
    public object actionReturn;
}

// adding AgentMetdata class so there is less confusing
// overlap between ObjectMetadata and AgentMetadata
[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class AgentMetadata {
    public string name;
    public Vector3 position;
    public Vector3 rotation;
    public float cameraHorizon;

    // TODO: this should be removed from base.
    // some agents cannot stand (e.g., drone, locobot)
    public bool? isStanding = null;

    public bool inHighFrictionArea;
    public AgentMetadata() { }
}

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class DroneAgentMetadata : AgentMetadata {
    // why is the launcher position even attached to the agent's metadata
    // and not the generic metdata?
    public Vector3 launcherPosition;
}

// additional metadata for drone objects (only use with Drone controller)
[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class DroneObjectMetadata : ObjectMetadata {
    // Drone Related Metadata
    public int numSimObjHits;
    public int numFloorHits;
    public int numStructureHits;
    public float lastVelocity;
    public Vector3 launcherPosition;
    public bool isCaught;
    public DroneObjectMetadata() { }
}

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class ObjectMetadata {
    public string name;
    public Vector3 position;
    public Vector3 rotation;

    // public float cameraHorizon; moved to AgentMetadata, objects don't have a camerahorizon
    public bool visible;

    // If true, object is obstructed by something and actions cannot be performed on it unless forced.
    // This means an object behind glass will be obstructed=True and visible=True
    public bool isInteractable;

    // is this object a receptacle?
    public bool receptacle;

    // note: some objects are not themselves toggleable, because they must be toggled
    // on/off via another sim object (stove knob -> stove burner)
    // is this object able to be toggled on/off directly?
    public bool toggleable;

    // note some objects can still return the istoggle value even if they cannot directly
    // be toggled on off (stove burner -> stove knob)
    // is this object currently on or off? true is on
    public bool isToggled;

    // can this object be broken?
    public bool breakable;

    // is this object broken?
    public bool isBroken;

    // objects filled with liquids
    public bool canFillWithLiquid;

    // is this object filled with some liquid? - similar to 'depletable' but this is for liquids
    public bool isFilledWithLiquid;

    // coffee, wine, water
    public string fillLiquid;

    // can toggle object state dirty/clean
    public bool dirtyable;

    // is this object in a dirty or clean state?
    public bool isDirty;

    // for objects that can be emptied or depleted (toilet paper, paper towels, tissue box etc)
    // - specifically not for liquids.
    public bool canBeUsedUp;

    // is this object currently used up?
    public bool isUsedUp;

    // can this object be turned to a cooked state? object should not be able to toggle
    // back to uncooked state with contextual interactions, only a direct action
    public bool cookable;

    // is it cooked right now? - context sensitive objects might set this
    // automatically like Toaster/Microwave/Pots/Pans if isHeated = true
    // temperature placeholder values, might get more specific later
    // with degrees but for now just track these three states
    public bool isCooked;

    // return current abstracted temperature of object as a string (RoomTemp, Hot, Cold)
    public string temperature;

    // can change other object temp to hot
    public bool isHeatSource;

    // can change other object temp to cool
    public bool isColdSource;

    // can this be sliced in some way?
    public bool sliceable;

    // currently sliced?
    public bool isSliced;

    // can this object be opened?
    public bool openable;

    // is this object currently opened?
    public bool isOpen;

    // if the object is openable, what is the current openness? It's a normalized value from [0:1]
    public float openness;

    // can this object be picked up?
    public bool pickupable;

    // if the pickupable object is actively being held by the agent
    public bool isPickedUp;

    // if the object is moveable, able to be pushed/affected by physics but is too big to pick up
    public bool moveable;

    // mass is only for moveable and pickupable objects
    public float mass;

    // Salient materials that this object is made of as strings (see enum above).
    // This is only for objects that are Pickupable or Moveable
    public string[] salientMaterials;

    public string[] receptacleObjectIds;

    // distance from object's transform to agent transform
    public float distance;

    // what type of object is this?
    public string objectType;

    // uuid of the object
    public string objectId;

    //name of this game object's prefab asset if it has one
    public string assetId;

    //report back what receptacles contain this object
    public string[] parentReceptacles;

    //if this is a toggleable object, report back what objects this also toggles (light switch, stove, etc.)
    public string[] controlledObjects;

    // true if this game object currently has a non-zero velocity
    public bool isMoving;

    public AxisAlignedBoundingBox axisAlignedBoundingBox;
    public ObjectOrientedBoundingBox objectOrientedBoundingBox;

    public ObjectMetadata() { }
}

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class SceneBounds {
    // 8 corners of the world axis aligned box that bounds a sim object
    // 8 rows - 8 corners, one per row
    // 3 columns - x, y, z of each corner respectively
    public float[][] cornerPoints;

    // center of the bounding box of the scene in worldspace coordinates
    public Vector3 center;

    // the size of the bounding box of the scene in worldspace coordinates (world x, y, z)
    public Vector3 size;
}

// for returning a world axis aligned bounding box
// if an object is rotated, the dimensions of this box are subject to change
[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class AxisAlignedBoundingBox {
    // 8 corners of the world axis aligned box that bounds a sim object
    // 8 rows - 8 corners, one per row
    // 3 columns - x, y, z of each corner respectively
    public float[][] cornerPoints;

    // center of the bounding box of this object in worldspace coordinates
    public Vector3 center;

    // the size of the bounding box in worldspace coordinates (world x, y, z)
    public Vector3 size;
}

// for returning an object oriented bounds not locked to world axes
// if an object is rotated, this object oriented box will not change dimensions
[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class ObjectOrientedBoundingBox {
    // probably return these from the BoundingBox component of the object for now?
    // this means that it will only work for Pickupable objects at the moment
    public float[][] cornerPoints;
}

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class InventoryObject {
    public string objectId;
    public string objectType;
}

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class ColorId {
    public ushort[] color;
    public string name;
}

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class ColorBounds {
    public ushort[] color;
    public int[] bounds;
}

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class HandMetadata {
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 localPosition;
    public Vector3 localRotation;
}

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class JointMetadata {
    public string name;
    public Vector3 position;
    public Vector3 rootRelativePosition;
    public Vector4 rotation;
    public Vector4 rootRelativeRotation;
    public Vector4 localRotation;
}

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class ArmMetadata {
    // public Vector3 handTarget;
    // joints 1 to 4, joint 4 is the wrist and joint 1 is the base that never moves
    public JointMetadata[] joints;

    // all objects currently held by the hand sphere
    public List<String> heldObjects;

    // all sim objects that are both pickupable and inside the hand sphere
    public List<String> pickupableObjects;

    // world coordinates of the center of the hand's sphere
    public Vector3 handSphereCenter;

    // current radius of the hand sphere
    public float handSphereRadius;
}

[Serializable]
public class ObjectTypeCount {
    public string objectType; // specify object by type in scene
    public int count; // the total count of objects of type objectType that we will try to make exist in the scene
}

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class ObjectPose {

    public ObjectPose() : this("", new Vector3(), new Vector3()) { }
    public ObjectPose(string objectName, Vector3 position, Vector3 rotation) {
        this.objectName = objectName;
        this.position = position;
        this.rotation = rotation;
    }
    public string objectName;
    public Vector3 position;
    public Vector3 rotation;
}

// set object states either by Type or by compatible State
//"slice all objects of type Apple"
//"slice all objects that have the sliceable property"
// also used to randomly do this ie: randomly slice all objects of type apple, randomly slice all objects that have sliceable property
[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class SetObjectStates {
    public string objectType = null; // valid strings are any Object Type listed in documentation (ie: AlarmClock, Apple, etc)
    public string stateChange = null; // valid strings are: openable, toggleable, breakable, canFillWithLiquid, dirtyable, cookable, sliceable, canBeUsedUp
    public bool isOpen;
    public bool isToggled;
    public bool isBroken;
    public bool isFilledWithLiquid;
    public bool isDirty;
    public bool isCooked;
    public bool isSliced;
    public bool isUsedUp;
}

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public struct MetadataWrapper {
    public ObjectMetadata[] objects;
    public bool isSceneAtRest;// set true if all objects in the scene are at rest (or very very close to 0 velocity)
    public AgentMetadata agent;
    public HandMetadata heldObjectPose;
    public ArmMetadata arm;
    public float fov;
    public Vector3 cameraPosition;
    public float cameraOrthSize;
    public ThirdPartyCameraMetadata[] thirdPartyCameras;
    public bool collided;
    public string[] collidedObjects;
    public InventoryObject[] inventoryObjects;
    public string sceneName;
    public string lastAction;
    public string errorMessage;
    public string errorCode; // comes from ServerActionErrorCode
    public bool lastActionSuccess;
    public int screenWidth;
    public int screenHeight;
    public int agentId;
    public string depthFormat;
    public ColorId[] colors;

    // Extras
    public float[] flatSurfacesOnGrid;
    public float[] distances;
    public float[] normals;
    public bool[] isOpenableGrid;
    public string[] segmentedObjectIds;
    public string[] objectIdsInBox;

    public int actionIntReturn;
    public float actionFloatReturn;
    public string[] actionStringsReturn;

    public float[] actionFloatsReturn;
    public Vector3[] actionVector3sReturn;
    public List<Vector3> visibleRange;
    public float currentTime;
    public SceneBounds sceneBounds;// return coordinates of the scene's bounds (center, size, extents)

    public object actionReturn;

}

/*
Wraps the JObject created by JSON.net and used by the ActionDispatcher
to dispatch to the appropriate action based on the passed in params.
The properties(agentId, sequenceId, action) exist to encapsulate the key names.
*/
public class DynamicServerAction {

    // These parameters are allowed to exist as both parameters to an Action and as global
    // paramaters.  This also excludes them from the InvalidArgument logic used in the ActionDispatcher
    public static readonly IReadOnlyCollection<string> AllowedExtraneousParameters = new HashSet<string>(){
        "sequenceId",
        "renderImage",
        "agentId",
        "renderObjectImage",
        "renderClassImage",
        "renderNormalsImage",
        "renderInstanceSegmentation",
        "action"
    };

    public JObject jObject {
        get;
        private set;
    }

    public int agentId {
        get {
            return this.GetValue("agentId", 0);
        }
    }

    public int sequenceId {
        get {
            return (int)this.GetValue("sequenceId", 0);
        }
    }

    public string action {
        get {
            return this.jObject["action"].ToString();
        }
    }

    public int GetValue(string name, int defaultValue) {
        if (this.ContainsKey(name)) {
            return (int)this.GetValue(name);
        } else {
            return defaultValue;
        }
    }

    public bool GetValue(string name, bool defaultValue) {
        if (this.ContainsKey(name)) {
            return (bool)this.GetValue(name);
        } else {
            return defaultValue;
        }
    }

    public JToken GetValue(string name) {
        return this.jObject.GetValue(name);
    }

    public bool ContainsKey(string name) {
        return this.jObject.ContainsKey(name);
    }

    public bool renderInstanceSegmentation {
        get {
            return this.GetValue("renderInstanceSegmentation", false);
        }
    }

    public bool renderImage {
        get {
            return this.GetValue("renderImage", true);
        }
    }

    public DynamicServerAction(Dictionary<string, object> action) {
        var jsonResolver = new ShouldSerializeContractResolver();
        this.jObject = JObject.FromObject(action,
                    new Newtonsoft.Json.JsonSerializer() {
                        ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                        ContractResolver = jsonResolver
                    });
    }

    public DynamicServerAction(JObject action) {
        this.jObject = action;
    }

    public DynamicServerAction(string jsonMessage) {
        this.jObject = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(jsonMessage);
    }

    public System.Object ToObject(Type objectType) {
        return this.jObject.ToObject(objectType);
    }

    public T ToObject<T>() {
        return this.jObject.ToObject<T>();
    }

    // this is primarily used when detecting invalid arguments
    // if Initialize is ever changed we should refactor this since renderInstanceSegmentation is a 
    // valid argument for Initialize as well as a global parameter
    public IEnumerable<string> ArgumentKeys() {
        return this.jObject.Properties().Select(p => p.Name).Where(argName => !AllowedExtraneousParameters.Contains(argName)).ToList();
    }

    public IEnumerable<string> Keys() {
        return this.jObject.Properties().Select(p => p.Name).ToList();
    }

    public int Count() {
        return this.jObject.Count;
    }

}

[Serializable]
public class ServerAction {
    public string action;
    public int agentCount = 1;
    public string quality;
    public bool makeAgentsVisible = true;
    public float timeScale = 1.0f;
    public float? fixedDeltaTime;
    public int targetFrameRate;
    public float dronePositionRandomNoiseSigma = 0.00f;
    public string objectType;
    public int objectVariation;
    public string receptacleObjectType;
    public string receptacleObjectId;
    public float gridSize;
    public string[] excludeObjectIds;
    public string[] objectIds;
    public string objectId;
    public int agentId;
    public int thirdPartyCameraId;
    public float y;
    public float fieldOfView;
    public float x;
    public float z;
    public float pushAngle;
    public int horizon;
    public Vector3 rotation;
    public Vector3 position;
    public Vector3 direction;

    public bool allowAgentsToIntersect = false;
    public float handDistance;// used for max distance agent's hand can move
    public List<Vector3> positions = null;
    public bool standing = true;
    public bool forceAction;
    public bool applyActionNoise = true;
    public float movementGaussianMu;
    public float movementGaussianSigma;
    public float rotateGaussianMu;
    public float rotateGaussianSigma;
    public string skyboxColor = null;
    public bool forceKinematic;
    public float maxAgentsDistance = -1.0f;
    public bool alwaysReturnVisibleRange = false;
    public int sequenceId;
    public bool snapToGrid = true;
    public string sceneName;
    public bool rotateOnTeleport;
    public bool forceVisible;
    public bool anywhere;// used for SpawnTargetCircle, GetSpawnCoordinatesAboveObject for if anywhere or only in agent view
    public bool randomizeOpen;
    public int randomSeed;
    public float moveMagnitude;
    public bool autoSimulation = true;
    public bool simplifyPhysics = false;
    public float startAgentsRotatedBy = 0f;
    public float visibilityDistance;
    public bool uniquePickupableObjectTypes; // only allow one of each object type to be visible
    public int numPlacementAttempts;
    public bool randomizeObjectAppearance;
    public bool renderImage = true;
    public bool renderDepthImage;
    public bool renderSemanticSegmentation;
    public bool renderInstanceSegmentation;
    public bool renderNormalsImage;
    public bool renderFlowImage;
    public float cameraY = 0.675f;
    public bool placeStationary = true; // when placing/spawning an object, do we spawn it stationary (kinematic true) or spawn and let physics resolve final position
                                        // public string ssao = "default";
    public string fillLiquid; // string to indicate what kind of liquid this object should be filled with. Water, Coffee, Wine etc.
    public float TimeUntilRoomTemp;
    public bool allowDecayTemperature = true; // set to true if temperature should decay over time, set to false if temp changes should not decay, defaulted true
    public string StateChange;// a string that specifies which state change to randomly toggle
    public float timeStep = 0.01f;
    public float mass;
    public float drag;
    public float angularDrag;
    public ObjectTypeCount[] numDuplicatesOfType; // specify, by object Type, how many duplicates of that given object type to try and spawn
    // use only the objectType class member to specify which receptacle objects should be excluded from the valid receptacles to spawn objects in
    public String[] excludedReceptacles;
    public ObjectPose[] objectPoses;
    public SetObjectStates SetObjectStates;
    public float minDistance;// used in target circle spawning function
    public float maxDistance;// used in target circle spawning function
    public float noise;
    public ControllerInitialization controllerInitialization = null;
    public string agentControllerType = "";
    public string agentMode = "default"; // mode of Agent, valid values are "default" "locobot" "drone", note certain modes are only compatible with certain controller types

    public float agentRadius = 2.0f;
    public int maxStepCount;
    public float rotateStepDegrees = 90.0f; // default rotation amount for RotateRight/RotateLeft actions

    public float degrees;// for overriding the default degree amount in look up/lookdown/rotaterRight/rotateLeft

    public bool topView = false;

    public bool orthographic = false;

    public bool grid = false;

    public Color? gridColor;

    public Gradient pathGradient;

    // should actions like pickup and moveHand have more manual, less abstracted behavior?
    public bool manualInteract = false;

    // color 0-255
    public float r;
    public float g;
    public float b;

    // default time for objects to wait before returning actionFinished() if an action put them in motion
    public float TimeToWaitForObjectsToComeToRest = 10.0f;
    public float scale;
    public string visibilityScheme = VisibilityScheme.Collider.ToString();
    public bool fastActionEmit = true;
    // this allows us to chain the dispatch between two separate
    // legacy action (e.g. AgentManager.Initialize and BaseFPSAgentController.Initialize)
    public DynamicServerAction dynamicServerAction;

    public bool returnToStart = false;

    public float speed = 1.0f;

    public bool handCameraSpace = false;

    public float radius;

    public bool stopArmMovementOnContact = false;

    public bool disableRendering = false;

    // this restricts arm position to the hemisphere in front of the agent
    public bool restrictMovement = false;

    // used to determine which coordinate space is used in Mid Level Arm actions
    // valid options are relative to: world, wrist, armBase
    public string coordinateSpace = "armBase";

    // if agent is using arm mode, determines if a mass threshold should be used
    // for when the arm hits heavy objects. If threshold is used, the arm will
    // collide and stop moving when hitting a heavy enough sim object rather than
    // move through it (this is for when colliding with pickupable and moveable sim objs)
    // the mass threshold for how massive a pickupable/moveable sim object needs to be
    // for the arm to detect collisions and stop moving
    public float? massThreshold;


    public SimObjType ReceptableSimObjType() {
        if (string.IsNullOrEmpty(receptacleObjectType)) {
            return SimObjType.Undefined;
        }
        return (SimObjType)Enum.Parse(typeof(SimObjType), receptacleObjectType);
    }

    public VisibilityScheme GetVisibilityScheme() {
        VisibilityScheme result = VisibilityScheme.Collider;
        try {
            result = (VisibilityScheme)Enum.Parse(typeof(VisibilityScheme), visibilityScheme, true);
        }
        // including this pragma so the "ex variable declared but not used" warning stops yelling
#pragma warning disable 0168
        catch (ArgumentException ex) {
#pragma warning restore 0168
            Debug.LogError("Error parsing visibilityScheme: '" + visibilityScheme + "' defaulting to Collider");
        }

        return result;
    }

    public SimObjType GetSimObjType() {
        if (string.IsNullOrEmpty(objectType)) {
            return SimObjType.Undefined;
        }
        return (SimObjType)Enum.Parse(typeof(SimObjType), objectType);
    }
    // allows this to be passed in as a dynamic which we then
    // cast back to itself
    public ServerAction ToObject<T>() {
        return this;
    }

    public ServerAction ToObject(Type t) {
        return this;
    }


}

[Serializable]
[MessagePackObject(keyAsPropertyName: true)]
public class InitializeReturn {
    public float cameraNearPlane;
    public float cameraFarPlane;
}

public enum ServerActionErrorCode {
    Undefined,
    ReceptacleNotVisible,
    ReceptacleNotOpen,
    ObjectNotInInventory,
    ReceptacleFull,
    ReceptaclePivotNotVisible,
    ObjectNotAllowedInReceptacle,
    ObjectNotVisible,
    InventoryFull,
    ObjectNotPickupable,
    LookUpCantExceedMax,
    LookDownCantExceedMin,
    InvalidAction,
    MissingArguments,
    AmbiguousAction,
    InvalidArgument
}

public enum VisibilityScheme {
    Collider,
    Distance
}

public enum Temperature {
    RoomTemp,
    Hot,
    Cold
};

// Salient materials are only for pickupable and moveable objects,
// for now static only objects do not report material back since we have to assign them manually
// They are the materials that make up an object (ie: cell phone - metal, glass).
public enum SalientObjectMaterial {
    Metal,
    Wood,
    Plastic,
    Glass,
    Ceramic,
    Stone,
    Fabric,
    Rubber,
    Food,
    Paper,
    Wax,
    Soap,
    Sponge,
    Organic,
    Leather
};

[Serializable]
public class ControllerInitialization {
    public Dictionary<string, TypedVariable> variableInitializations;
}

[Serializable]
public class TypedVariable {
    public string type;
    public object value;
}

public class ShouldSerializeContractResolver : DefaultContractResolver {
    public static readonly ShouldSerializeContractResolver Instance = new ShouldSerializeContractResolver();

    protected override JsonProperty CreateProperty(
        MemberInfo member,
        MemberSerialization memberSerialization
    ) {
        JsonProperty property = base.CreateProperty(member, memberSerialization);

        // exclude these properties to make serialization match JsonUtility
        if (property.DeclaringType == typeof(Vector3) &&
            (property.PropertyName == "sqrMagnitude" ||
            property.PropertyName == "magnitude" ||
            property.PropertyName == "normalized")
        ) {
            property.ShouldSerialize = instance => { return false; };
            return property;
        } else {
            return base.CreateProperty(member, memberSerialization);
        }
    }
}

public enum DepthFormat {
    Normalized,
    Meters,
    Millimeters
}

public enum AgentState {
    Processing,
    ActionComplete,
    PendingFixedUpdate,
    Emit
}
