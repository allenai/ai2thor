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
using Newtonsoft.Json.Serialization;
using System.Reflection;
using System.Text;
using UnityEngine.Networking;

public class AgentManager : MonoBehaviour
{
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
	private bool renderClassImage;
	private bool renderObjectImage;
    private bool defaultRenderObjectImage;
	private bool renderNormalsImage;
    private bool renderFlowImage;
	private Socket sock = null;
	private List<Camera> thirdPartyCameras = new List<Camera>();
	private Color[] agentColors = new Color[]{Color.blue, Color.yellow, Color.green, Color.red, Color.magenta, Color.grey};
	public int actionDuration = 3;
	private BaseFPSAgentController primaryAgent;
    private PhysicsSceneManager physicsSceneManager;
    private FifoServer.Client fifoClient = null;
	private enum serverTypes { WSGI, FIFO};
    private serverTypes serverType;
    private AgentState agentManagerState = AgentState.Emit;
    private bool fastActionEmit;
    private HashSet<string> agentManagerActions = new HashSet<string>{"Reset", "Initialize", "AddThirdPartyCamera", "UpdateThirdPartyCamera"};


	public Bounds sceneBounds = new Bounds(
		new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity),
		new Vector3(-float.PositiveInfinity, -float.PositiveInfinity, -float.PositiveInfinity)
	);
	public Bounds SceneBounds
    {
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
            Debug.unityLogger.logEnabled = false;
        #endif

        QualitySettings.vSyncCount = 0;
		robosimsPort = LoadIntVariable (robosimsPort, "PORT");
		robosimsHost = LoadStringVariable(robosimsHost, "HOST");
		serverSideScreenshot = LoadBoolVariable (serverSideScreenshot, "SERVER_SIDE_SCREENSHOT");
		robosimsClientToken = LoadStringVariable (robosimsClientToken, "CLIENT_TOKEN");
        serverType = (serverTypes)Enum.Parse(typeof(serverTypes), LoadStringVariable (serverTypes.WSGI.ToString(), "SERVER_TYPE").ToUpper());
        if (serverType == serverTypes.FIFO) {
            string  serverPipePath = LoadStringVariable (null, "FIFO_SERVER_PIPE_PATH");
            string  clientPipePath = LoadStringVariable (null, "FIFO_CLIENT_PIPE_PATH");
            
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

	void Start()
	{
        //default primary agent's agentController type to "PhysicsRemoteFPSAgentController"
		initializePrimaryAgent();

        //auto set agentMode to default for the web demo
        #if UNITY_WEBGL
        primaryAgent.SetAgentMode("default");
        #endif

        primaryAgent.actionDuration = this.actionDuration;
		// this.agents.Add (primaryAgent);
        physicsSceneManager = GameObject.Find("PhysicsSceneManager").GetComponent<PhysicsSceneManager>();
		#if !UNITY_EDITOR
        StartCoroutine (EmitFrame());
		#endif
	}

	private void initializePrimaryAgent()
    {
        SetUpPhysicsController();
	}

	public void Initialize(ServerAction action)
	{
        //first parse agentMode and agentControllerType
        //"default" agentMode can use either default or "stochastic" agentControllerType
        //"bot" agentMode can use either default or "stochastic" agentControllerType
        //"drone" agentMode can ONLY use "drone" agentControllerType, and NOTHING ELSE (for now?)
        if(action.agentMode.ToLower() == "default")
        {
            if(action.agentControllerType.ToLower() != "physics" || action.agentControllerType.ToLower() != "stochastic")
            {
                Debug.Log("default mode must use either physics or stochastic controller. Defaulting to physics");
                SetUpPhysicsController();
            }

            //if not stochastic, default to physics controller
            if(action.agentControllerType.ToLower() == "physics")
            {
                //set up physics controller
                SetUpPhysicsController();
            }

            //if stochastic, set up stochastic controller
            else if(action.agentControllerType.ToLower() == "stochastic")
            {
                //set up stochastic controller
                SetUpStochasticController(action);
            }
        }

        else if(action.agentMode.ToLower() == "bot")
        {
            //if not stochastic, default to stochastic
            if(action.agentControllerType.ToLower() != "stochastic")
            {
                Debug.Log("'bot' mode only fully supports the 'stochastic' controller type at the moment. Forcing agentControllerType to 'stochastic'");
                action.agentControllerType = "stochastic";
                //set up stochastic controller
                SetUpStochasticController(action);
            }

            else
            SetUpStochasticController(action);
        }

        else if(action.agentMode.ToLower() == "drone")
        {
            if(action.agentControllerType.ToLower() != "drone")
            {
                Debug.Log("'drone' agentMode is only compatible with 'drone' agentControllerType, forcing agentControllerType to 'drone'");
                action.agentControllerType = "drone";

                //ok now set up drone controller
                SetUpDroneController(action);
            }

            else
            SetUpDroneController(action);

        }

		primaryAgent.ProcessControlCommand (action);
		primaryAgent.IsVisible = action.makeAgentsVisible;
		this.renderClassImage = action.renderClassImage;
		this.renderDepthImage = action.renderDepthImage;
		this.renderNormalsImage = action.renderNormalsImage;
		this.renderObjectImage = this.defaultRenderObjectImage = action.renderObjectImage;
        this.renderFlowImage = action.renderFlowImage;
        this.fastActionEmit = action.fastActionEmit;

		if (action.alwaysReturnVisibleRange) {
			((PhysicsRemoteFPSAgentController) primaryAgent).alwaysReturnVisibleRange = action.alwaysReturnVisibleRange;
		}
        print("start addAgents");
		StartCoroutine (addAgents (action));

	}

    private void SetUpStochasticController(ServerAction action)
    {
        this.agents.Clear();
        //force snapToGrid to be false since we are stochastic
        action.snapToGrid = false;
        GameObject fpsController = GameObject.FindObjectOfType<BaseFPSAgentController>().gameObject;
        primaryAgent.enabled = false;
        primaryAgent = fpsController.GetComponent<StochasticRemoteFPSAgentController>();
        primaryAgent.agentManager = this;
        primaryAgent.enabled = true;
        // primaryAgent.Start();
        this.agents.Add(primaryAgent);
    }

    private void SetUpDroneController (ServerAction action)
    {
        this.agents.Clear();
        //force snapToGrid to be false
        action.snapToGrid = false;
        GameObject fpsController = GameObject.FindObjectOfType<BaseFPSAgentController>().gameObject;
        primaryAgent.enabled = false;
        primaryAgent = fpsController.GetComponent<DroneFPSAgentController>();
        primaryAgent.agentManager = this;
        primaryAgent.enabled = true;
        this.agents.Add(primaryAgent);
    }

    //note: this doesn't take a ServerAction because we don't have to force the snpToGrid bool
    //to be false like in other controller types.
    private void SetUpPhysicsController ()
    {
        this.agents.Clear();
		GameObject fpsController = GameObject.FindObjectOfType<BaseFPSAgentController>().gameObject;
		primaryAgent = fpsController.GetComponent<PhysicsRemoteFPSAgentController>();
		primaryAgent.enabled = true;
		primaryAgent.agentManager = this;
        this.agents.Add(primaryAgent);
    }

    //return reference to primary agent in case we need a reference to the primary
    public BaseFPSAgentController ReturnPrimaryAgent()
    {
        return primaryAgent;
    }

	private IEnumerator addAgents(ServerAction action) {
		yield return null;
		Vector3[] reachablePositions = primaryAgent.getReachablePositions(2.0f);
		for (int i = 1; i < action.agentCount && this.agents.Count < Math.Min(agentColors.Length, action.agentCount); i++) {
			action.x = reachablePositions[i + 4].x;
			action.y = reachablePositions[i + 4].y;
			action.z = reachablePositions[i + 4].z;
			addAgent (action);
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
		// Recordining initially disabled renderers and scene bounds
		sceneBounds = new Bounds(
			new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity),
			new Vector3(-float.PositiveInfinity, -float.PositiveInfinity, -float.PositiveInfinity)
		);
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

	// If fov is <= min or > max, return defaultVal, else return fov
	private float ClampFieldOfView(float fov, float defaultVal = 90f, float min = 0f, float max = 180f) {
		return (fov <= min || fov > max) ? defaultVal : fov;
	}

    private void updateImageSynthesis(bool status) {
        foreach(var agent in this.agents) {
            agent.updateImageSynthesis(status);
        }
    }

    private void updateThirdPartyCameraImageSynthesis(bool status) {
        if (status) 
        {
            foreach(var camera in this.thirdPartyCameras)
            {
                GameObject gameObject = camera.gameObject;
                var imageSynthesis = gameObject.GetComponentInChildren<ImageSynthesis> () as ImageSynthesis;
                if (imageSynthesis == null){
                    gameObject.AddComponent(typeof(ImageSynthesis));
                }
                imageSynthesis = gameObject.GetComponentInChildren<ImageSynthesis> () as ImageSynthesis;
                imageSynthesis.enabled = status;
            }
        }
    }

	public void AddThirdPartyCamera(ServerAction action) {
		GameObject gameObject = new GameObject("ThirdPartyCamera" + thirdPartyCameras.Count);
		gameObject.AddComponent(typeof(Camera));
		Camera camera = gameObject.GetComponentInChildren<Camera>();

		camera.cullingMask = ~(1 << 11);

		if (this.renderDepthImage || this.renderClassImage || this.renderObjectImage || this.renderNormalsImage || this.renderFlowImage)
		{
			gameObject.AddComponent(typeof(ImageSynthesis));
		}

		this.thirdPartyCameras.Add(camera);
		gameObject.transform.eulerAngles = action.rotation;
		gameObject.transform.position = action.position;

		// default to 90 fov on third party camera if value is too small or large
		camera.fieldOfView = ClampFieldOfView(action.fieldOfView);

        if (action.orthographic) {
			// NOTE: orthographicSize uses the original fieldOfView value passed in
			// TODO: this is an odd overload of the argument, consider adding another parameter
            camera.orthographicSize = action.fieldOfView;
        }
        camera.orthographic = action.orthographic;

		this.agentManagerState = AgentState.ActionComplete;
	}

	public void UpdateThirdPartyCamera(ServerAction action) {
		if (action.thirdPartyCameraId <= thirdPartyCameras.Count) {
			Camera thirdPartyCamera = thirdPartyCameras.ToArray()[action.thirdPartyCameraId];
			thirdPartyCamera.gameObject.transform.eulerAngles = action.rotation;
			thirdPartyCamera.gameObject.transform.position = action.position;

			// keep existing fieldOfView if we have one and are not passed a new one
			// 0 gets passed for null
			if(thirdPartyCamera.fieldOfView == 0 || action.fieldOfView != 0) {
				// default to 90 fov on third party camera if value passed in is too small or large
				thirdPartyCamera.fieldOfView = ClampFieldOfView(action.fieldOfView);
			}

			// set the skybox to default, white, or black (other colors can be added as needed)
			if (action.skyboxColor == null) {
				// default blue-ish skybox that shows the ground
				thirdPartyCamera.clearFlags = CameraClearFlags.Skybox;
			} else {
				thirdPartyCamera.clearFlags = CameraClearFlags.SolidColor;
				if (action.skyboxColor == "white") {
					thirdPartyCamera.backgroundColor = Color.white;
				} else {
					thirdPartyCamera.backgroundColor = Color.black;
				}
			}
		}
		this.agentManagerState = AgentState.ActionComplete;
	}

	private void addAgent(ServerAction action) {
		Vector3 clonePosition = new Vector3(action.x, action.y, action.z);

		//disable ambient occlusion on primary agetn because it causes issues with multiple main cameras
		//primaryAgent.GetComponent<PhysicsRemoteFPSAgentController>().DisableScreenSpaceAmbientOcclusion();

		BaseFPSAgentController clone = UnityEngine.Object.Instantiate (primaryAgent);
		clone.IsVisible = action.makeAgentsVisible;
		clone.actionDuration = this.actionDuration;
		// clone.m_Camera.targetDisplay = this.agents.Count;
		clone.transform.position = clonePosition;
		UpdateAgentColor(clone, agentColors[this.agents.Count]);
		clone.ProcessControlCommand (action);
		this.agents.Add (clone);
	}

	private Vector3 agentStartPosition(BaseFPSAgentController agent) {

		Transform t = agent.transform;
		Vector3[] castDirections = new Vector3[]{ t.forward, t.forward * -1, t.right, t.right * -1 };

		RaycastHit maxHit = new RaycastHit ();
		Vector3 maxDirection = Vector3.zero;

		RaycastHit hit;
		CharacterController charContr = agent.m_CharacterController;
		Vector3 p1 = t.position + charContr.center + Vector3.up * -charContr.height * 0.5f;
		Vector3 p2 = p1 + Vector3.up * charContr.height;
		foreach (Vector3 d in castDirections) {

			if (Physics.CapsuleCast (p1, p2, charContr.radius, d, out hit)) {
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
		foreach (MeshRenderer r in agent.gameObject.GetComponentsInChildren<MeshRenderer> () as MeshRenderer[]) {
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
			Destroy(agents[i]);
		}
		yield return null;

		if (string.IsNullOrEmpty(response.sceneName)){
			UnityEngine.SceneManagement.SceneManager.LoadScene (UnityEngine.SceneManagement.SceneManager.GetActiveScene ().name);
		} else {
			UnityEngine.SceneManagement.SceneManager.LoadScene (response.sceneName);
		}
	}

	public void Reset(ServerAction response) {
		StartCoroutine(ResetCoroutine(response));
	}

    public bool SwitchScene(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            return true;
        }
        return false;
    }

    // Decide whether agent has stopped actions
    // And if we need to capture a new frame

    private void Update()
    {
        physicsSceneManager.isSceneAtRest = true;//assume the scene is at rest by default
    }

    private void LateUpdate() {

/*
		if (readyToEmit)
        {
            //start emit frame for physics and stochastic controllers
			if(!droneMode)
            {
				//readyToEmit = false;
				//StartCoroutine (EmitFrame());
			}

            //start emit frame for flying drone controller
            if(droneMode)
            {
                //make sure each agent in flightMode has updated at least once
				if (hasDroneAgentUpdatedCount == agents.Count && hasDroneAgentUpdatedCount > 0)
                {
					readyToEmit = false;
					//StartCoroutine (EmitFrame());
				}
			}
		}
 */

        //ok now if the scene is at rest, turn back on physics autosimulation automatically
        //note: you can do this earlier by manually using the UnpausePhysicsAutoSim() action found in PhysicsRemoteFPSAgentController
        // if(physicsSceneManager.isSceneAtRest && !droneMode &&
        // physicsSceneManager.physicsSimulationPaused && AdvancePhysicsStepCount > 0)
        // {
        //     //print("soshite toki wa ugoki desu");
        //     Physics.autoSimulation = true;
        //     physicsSceneManager.physicsSimulationPaused = false;
        //     AdvancePhysicsStepCount = 0;
        // }

	}

	private byte[] captureScreen() {
		if (tex.height != UnityEngine.Screen.height ||
			tex.width != UnityEngine.Screen.width) {
			tex = new Texture2D(UnityEngine.Screen.width, UnityEngine.Screen.height, TextureFormat.RGB24, false);
			readPixelsRect = new Rect(0, 0, UnityEngine.Screen.width, UnityEngine.Screen.height);
		}
		tex.ReadPixels(readPixelsRect, 0, 0);
		tex.Apply();
		return tex.GetRawTextureData ();
	}


	private void addThirdPartyCameraImage(List<KeyValuePair<string, byte[]>> payload, Camera camera) {
		RenderTexture.active = camera.activeTexture;
		camera.Render ();
        payload.Add(new KeyValuePair<string, byte[]>("image-thirdParty-camera", captureScreen()));
	}

	private void addImage(List<KeyValuePair<string, byte[]>> payload, BaseFPSAgentController agent) {
		if (this.renderImage) {

			if (this.agents.Count > 1 || this.thirdPartyCameras.Count > 0) {
				RenderTexture.active = agent.m_Camera.activeTexture;
				agent.m_Camera.Render ();
			}
            payload.Add(new KeyValuePair<string, byte[]>("image", captureScreen()));
		}
	}

	private void addObjectImage(List<KeyValuePair<string, byte[]>> payload, BaseFPSAgentController agent, ref MetadataWrapper metadata) {
		if (this.renderObjectImage) {
			if (!agent.imageSynthesis.hasCapturePass("_id")) {
				Debug.LogError("Object Image not available in imagesynthesis - returning empty image");
			}
			byte[] bytes = agent.imageSynthesis.Encode ("_id");
            payload.Add(new KeyValuePair<string, byte[]>("image_ids", bytes));

			Color[] id_image = agent.imageSynthesis.tex.GetPixels();
			Dictionary<Color, int[]> colorBounds = new Dictionary<Color, int[]> ();
			for (int yy = 0; yy < tex.height; yy++) {
				for (int xx = 0; xx < tex.width; xx++) {
					Color colorOn = id_image [yy * tex.width + xx];
					if (!colorBounds.ContainsKey (colorOn)) {
						colorBounds [colorOn] = new int[]{xx, yy, xx, yy};
					} else {
						int[] oldPoint = colorBounds [colorOn];
						if (xx < oldPoint [0]) {
							oldPoint [0] = xx;
						}
						if (yy < oldPoint [1]) {
							oldPoint [1] = yy;
						}
						if (xx > oldPoint [2]) {
							oldPoint [2] = xx;
						}
						if (yy > oldPoint [3]) {
							oldPoint [3] = yy;
						}
					}
				}
			}
			List<ColorBounds> boundsList = new List<ColorBounds> ();
			foreach (Color key in colorBounds.Keys) {
				ColorBounds bounds = new ColorBounds ();
				bounds.color = new ushort[] {
					(ushort)Math.Round (key.r * 255),
					(ushort)Math.Round (key.g * 255),
					(ushort)Math.Round (key.b * 255)
				};
				bounds.bounds = colorBounds [key];
				boundsList.Add (bounds);
			}
			metadata.colorBounds = boundsList.ToArray ();

			List<ColorId> colors = new List<ColorId> ();
			foreach (Color key in agent.imageSynthesis.colorIds.Keys) {
				ColorId cid = new ColorId ();
				cid.color = new ushort[] {
					(ushort)Math.Round (key.r * 255),
					(ushort)Math.Round (key.g * 255),
					(ushort)Math.Round (key.b * 255)
				};

				cid.name = agent.imageSynthesis.colorIds [key];
				colors.Add (cid);
			}
			metadata.colors = colors.ToArray ();

		}
	}

	private void addImageSynthesisImage(List<KeyValuePair<string, byte[]>> payload, ImageSynthesis synth, bool flag, string captureName, string fieldName)
	{
		if (flag) {
			if (!synth.hasCapturePass (captureName)) {
				Debug.LogError (captureName + " not available - sending empty image");
			}
			byte[] bytes = synth.Encode (captureName);
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

		string msg = "{\"action\": \"RotateRight\", \"timeScale\": 90.0}";
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
                ImageSynthesis imageSynthesis = camera.gameObject.GetComponentInChildren<ImageSynthesis> () as ImageSynthesis;
                addThirdPartyCameraImage (renderPayload, camera);
                addImageSynthesisImage(renderPayload, imageSynthesis, this.renderDepthImage, "_depth", "image_thirdParty_depth");
                addImageSynthesisImage(renderPayload, imageSynthesis, this.renderNormalsImage, "_normals", "image_thirdParty_normals");
                addImageSynthesisImage(renderPayload, imageSynthesis, this.renderObjectImage, "_id", "image_thirdParty_image_ids");
                addImageSynthesisImage(renderPayload, imageSynthesis, this.renderClassImage, "_class", "image_thirdParty_classes");
                addImageSynthesisImage(renderPayload, imageSynthesis, this.renderClassImage, "_flow", "image_thirdParty_flow");//XXX fix this in a bit
            }
        }
        for (int i = 0; i < this.agents.Count; i++) {
            BaseFPSAgentController agent = this.agents[i];
            MetadataWrapper metadata = agent.generateMetadataWrapper ();
            metadata.agentId = i;

            // we don't need to render the agent's camera for the first agent
            if (shouldRender) {
                addImage (renderPayload, agent);
                addImageSynthesisImage(renderPayload, agent.imageSynthesis, this.renderDepthImage, "_depth", "image_depth");
                addImageSynthesisImage(renderPayload, agent.imageSynthesis, this.renderNormalsImage, "_normals", "image_normals");
                addObjectImage (renderPayload, agent, ref metadata);
                addImageSynthesisImage(renderPayload, agent.imageSynthesis, this.renderClassImage, "_class", "image_classes");
                addImageSynthesisImage(renderPayload, agent.imageSynthesis, this.renderFlowImage, "_flow", "image_flow");

                metadata.thirdPartyCameras = cameraMetadata;
            }
            multiMeta.agents [i] = metadata;
        }

        if (shouldRender) {
            RenderTexture.active = currentTexture;
        }

        
    }

    private string serializeMetadataJson(MultiAgentMetadata multiMeta) {
            var jsonResolver = new ShouldSerializeContractResolver();
            return Newtonsoft.Json.JsonConvert.SerializeObject(multiMeta, Newtonsoft.Json.Formatting.None,
                        new Newtonsoft.Json.JsonSerializerSettings()
                            {
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


	public IEnumerator EmitFrame() {
        while (true) 
        {
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

            if (!this.canEmit()) 
            {
                continue;
            }

            MultiAgentMetadata multiMeta = new MultiAgentMetadata ();

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
                foreach(var item in renderPayload) {
                    form.AddBinaryData(item.Key, item.Value);
                }
                form.AddField("metadata", serializeMetadataJson(multiMeta));
                form.AddField("token", robosimsClientToken);


                if (this.sock == null) {
                    // Debug.Log("connecting to host: " + robosimsHost);
                    IPAddress host = IPAddress.Parse(robosimsHost);
                    IPEndPoint hostep = new IPEndPoint(host, robosimsPort);
                    this.sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    try {
                        this.sock.Connect(hostep);
                    }
                    catch (SocketException e) {
                        Debug.Log("Socket exception: " + e.ToString());
                    }
                }

                if (this.sock != null && this.sock.Connected) {
                    byte[] rawData = form.data;

                    string request = "POST /train HTTP/1.1\r\n" +
                    "Content-Length: " + rawData.Length.ToString() + "\r\n";

                    foreach(KeyValuePair<string, string> entry in form.headers) {
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

                        bytesReceived += received;;
                        string headerMsg = Encoding.ASCII.GetString(headerBuffer, 0, bytesReceived);
                        int offset = headerMsg.IndexOf("\r\n\r\n");
                        if (offset > 0){
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
                        //Debug.Log("total bytes received: " + bodyBytesReceived);
                    }

                    string msg = Encoding.ASCII.GetString(bodyBuffer, 0, bodyBytesReceived);
                    ProcessControlCommand(msg);
                }
            } else if (serverType == serverTypes.FIFO){
                byte[] msgPackMetadata = MessagePack.MessagePackSerializer.Serialize(multiMeta, 
                    MessagePack.Resolvers.ThorContractlessStandardResolver.Options);
                this.fifoClient.SendMessage(FifoServer.FieldType.Metadata, msgPackMetadata);
                foreach(var item in renderPayload) {
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

            //if(droneMode)
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

	private int parseContentLength(string header) {
		// Debug.Log("got header: " + header);
		string[] fields = header.Split(new char[]{'\r','\n'});
		foreach(string field in fields) {
			string[] elements = field.Split(new char[]{':'});
			if (elements[0].ToLower() == "content-length") {
				return Int32.Parse(elements[1].Trim());
			}
		}

		return 0;
	}

	private BaseFPSAgentController activeAgent() {
		return this.agents[activeAgentId];
	}

	private void ProcessControlCommand(string msg)
	{

        this.renderObjectImage = this.defaultRenderObjectImage;
        #if UNITY_WEBGL
		ServerAction controlCommand = new ServerAction();
		JsonUtility.FromJsonOverwrite(msg, controlCommand);
        #else
        dynamic controlCommand = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(msg);
        #endif

		this.currentSequenceId = controlCommand.sequenceId;
        // the following are handled this way since they can be null
        this.renderImage = controlCommand.renderImage == null ? true : controlCommand.renderImage;
        this.activeAgentId = controlCommand.agentId == null ? 0 : controlCommand.agentId;

		if (agentManagerActions.Contains(controlCommand.action.ToString())) {
            this.agentManagerState = AgentState.Processing;
            ActionDispatcher.Dispatch(this, controlCommand);
		} else {
            //we only allow renderObjectImage to be flipped on
            //on a per step() basis, since by default the param is null
            //so we don't know if a request is meant to turn the param off
            //or if it is just the value by default
            if (controlCommand.renderObjectImage == true) {
                this.renderObjectImage = true;
            }

            if (this.renderDepthImage || this.renderClassImage || this.renderObjectImage || this.renderNormalsImage) 
            {
                updateImageSynthesis(true);
                updateThirdPartyCameraImageSynthesis(true);
            }
			this.activeAgent().ProcessControlCommand (controlCommand);
		}
	}

	// Extra helper functions
	protected string LoadStringVariable(string variable, string name)
	{
		string envVarName = ENVIRONMENT_PREFIX + name.ToUpper();
		string envVarValue = Environment.GetEnvironmentVariable(envVarName);
		return envVarValue == null ? variable : envVarValue;
	}

	protected int LoadIntVariable(int variable, string name)
	{
		string envVarName = ENVIRONMENT_PREFIX + name.ToUpper();
		string envVarValue = Environment.GetEnvironmentVariable(envVarName);
		return envVarValue == null ? variable : int.Parse(envVarValue);
	}

	protected float LoadFloatVariable(float variable, string name)
	{
		string envVarName = ENVIRONMENT_PREFIX + name.ToUpper();
		string envVarValue = Environment.GetEnvironmentVariable(envVarName);
		return envVarValue == null ? variable : float.Parse(envVarValue);
	}

	protected bool LoadBoolVariable(bool variable, string name)
	{
		string envVarName = ENVIRONMENT_PREFIX + name.ToUpper();
		string envVarValue = Environment.GetEnvironmentVariable(envVarName);
		return envVarValue == null ? variable : bool.Parse(envVarValue);
	}

}

[Serializable]
public class MultiAgentMetadata {

	public MetadataWrapper[] agents;
	public ThirdPartyCameraMetadata[] thirdPartyCameras;
	public int activeAgentId;
	public int sequenceId;
}

[Serializable]
public class ThirdPartyCameraMetadata
{
	public int thirdPartyCameraId;
	public Vector3 position;
	public Vector3 rotation;
	public float fieldOfView;
}

[Serializable]
public class MetadataPatch
{
	public string lastAction;
	public string errorMessage;
	public string errorCode;
	public bool lastActionSuccess;
    public int agentId;
    public System.Object actionReturn;
}

//adding AgentMetdata class so there is less confusing
//overlap between ObjectMetadata and AgentMetadata
[Serializable]
public class AgentMetadata
{
    public string name;
    public Vector3 position;
    public Vector3 rotation;
    public float cameraHorizon;
	public bool isStanding;
	public bool inHighFrictionArea;
    public AgentMetadata() {}
}

[Serializable]
public class DroneAgentMetadata : AgentMetadata
{
    public float droneCurrentTime;
    public Vector3 LauncherPosition;
}

//additional metadata for drone objects (only use with Drone controller)
[Serializable]
public class DroneObjectMetadata : ObjectMetadata
{
    // Drone Related Metadata
    public int numSimObjHits;
    public int numFloorHits;
    public int numStructureHits;
    public float lastVelocity;
    public Vector3 LauncherPosition;
	public bool isCaught;
    public DroneObjectMetadata() {}
}

[Serializable]
public class ObjectMetadata
{
	public string name;
	public Vector3 position;
	public Vector3 rotation;
	//public float cameraHorizon; moved to AgentMetadata, objects don't have a camerahorizon
	public bool visible;
	public bool obstructed; //if true, object is obstructed by something and actions cannot be performed on it. This means an object behind glass will be obstructed=True and visible=True
	public bool receptacle;
	///
	//note: some objects are not themselves toggleable, because they must be toggled on/off via another sim object (stove knob -> stove burner)
	public bool toggleable;//is this object able to be toggled on/off directly?

	//note some objects can still return the istoggle value even if they cannot directly be toggled on off (stove burner -> stove knob)
	public bool isToggled;//is this object currently on or off? true is on
	///
	public bool breakable;
	public bool isBroken;//is this object broken?
	///
	public bool canFillWithLiquid;//objects filled with liquids
	public bool isFilledWithLiquid;//is this object filled with some liquid? - similar to 'depletable' but this is for liquids
	///
	public bool dirtyable;//can toggle object state dirty/clean
	public bool isDirty;//is this object in a dirty or clean state?
	///
	public bool canBeUsedUp;//for objects that can be emptied or depleted (toilet paper, paper towels, tissue box etc) - specifically not for liquids
	public bool isUsedUp;
	///
	public bool cookable;//can this object be turned to a cooked state? object should not be able to toggle back to uncooked state with contextual interactions, only a direct action
	public bool isCooked;//is it cooked right now? - context sensitive objects might set this automatically like Toaster/Microwave/ Pots/Pans if isHeated = true
	// ///
	// public bool abletocook;//can this object be heated up by a "fire" tagged source? -  use this for Pots/Pans
	// public bool isabletocook;//object is in contact with a "fire" tagged source (stove burner), if this is heated any object cookable object touching it will be switched to cooked - again use for Pots/Pans
	//
	//temperature placeholder values, might get more specific later with degrees but for now just track these three states
	public enum Temperature { RoomTemp, Hot, Cold};
	public string ObjectTemperature;//return current abstracted temperature of object as a string (RoomTemp, Hot, Cold)
	//
	public bool canChangeTempToHot;//can change other object temp to hot
	public bool canChangeTempToCold;//can change other object temp to cool
	//
	public bool sliceable;//can this be sliced in some way?
	public bool isSliced;//currently sliced?
	///
	public bool openable;
	public bool isOpen;
	public float openPercent;//if the object is openable, what is the current open percent? value from 0 to 1.0
	///
	public bool pickupable;
	public bool isPickedUp;//if the pickupable object is actively being held by the agent
    public bool moveable;//if the object is moveable, able to be pushed/affected by physics but is too big to pick up

	public float mass;//mass is only for moveable and pickupable objects

	//salient materials are only for pickupable and moveable objects, for now static only objects do not report material back since we have to assign them manually
	public enum ObjectSalientMaterial {Metal, Wood, Plastic, Glass, Ceramic, Stone, Fabric, Rubber, Food, Paper, Wax, Soap, Sponge, Organic, Leather} //salient materials that make up an object (ie: cell phone - metal, glass)

	public string [] salientMaterials; //salient materials that this object is made of as strings (see enum above). This is only for objects that are Pickupable or Moveable
	///
	public string[] receptacleObjectIds;
	public float distance;//dintance fromm object's transform to agent transform
	public String objectType;
	public string objectId;
	//public string parentReceptacle;
	public string[] parentReceptacles;
	//public float currentTime;
    public bool isMoving;//true if this game object currently has a non-zero velocity
    public AxisAlignedBoundingBox axisAlignedBoundingBox;
    public ObjectOrientedBoundingBox objectOrientedBoundingBox;

	public ObjectMetadata() { }
}

[Serializable]
public class SceneBounds
{
    //8 corners of the world axis aligned box that bounds a sim object
    //8 rows - 8 corners, one per row
    //3 columns - x, y, z of each corner respectively
    public float[][] cornerPoints;

    //center of the bounding box of the scene in worldspace coordinates
    public Vector3 center;

    //the size of the bounding box of the scene in worldspace coordinates (world x, y, z)
    public Vector3 size;
}

//for returning a world axis aligned bounding box
//if an object is rotated, the dimensions of this box are subject to change
[Serializable]
public class AxisAlignedBoundingBox
{
    //8 corners of the world axis aligned box that bounds a sim object
    //8 rows - 8 corners, one per row
    //3 columns - x, y, z of each corner respectively
    public float[][] cornerPoints;

    //center of the bounding box of this object in worldspace coordinates
    public Vector3 center;

    //the size of the bounding box in worldspace coordinates (world x, y, z)
    public Vector3 size;
}

//for returning an object oriented bounds not locked to world axes
//if an object is rotated, this object oriented box will not change dimensions
[Serializable]
public class ObjectOrientedBoundingBox
{
    //probably return these from the BoundingBox component of the object for now?
    //this means that it will only work for Pickupable objects at the moment
    public float[][] cornerPoints;
}

[Serializable]
public class InventoryObject
{
	public string objectId;
	public string objectType;
}

[Serializable]
public class ColorId {
	public ushort[] color;
	public string name;
}

[Serializable]
public class ColorBounds {
	public ushort[] color;
	public int[] bounds;
}

[Serializable]
public class HandMetadata {
	public Vector3 position;
	public Vector3 rotation;
	public Vector3 localPosition;
	public Vector3 localRotation;
}

[Serializable]
public class ObjectTypeCount
{
    public string objectType; //specify object by type in scene
    public int count; //the total count of objects of type objectType that we will try to make exist in the scene
}

[Serializable]
public class ObjectPose
{
    public string objectName;
    public Vector3 position;
    public Vector3 rotation;
}

//set object states either by Type or by compatible State
//"slice all objects of type Apple"
//"slice all objects that have the sliceable property"
//also used to randomly do this ie: randomly slice all objects of type apple, randomly slice all objects that have sliceable property
[Serializable]
public class SetObjectStates
{
    public string objectType = null; //valid strings are any Object Type listed in documentation (ie: AlarmClock, Apple, etc)
    public string stateChange = null; //valid strings are: openable, toggleable, breakable, canFillWithLiquid, dirtyable, cookable, sliceable, canBeUsedUp
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
public struct MetadataWrapper
{
	public ObjectMetadata[] objects;
    public bool isSceneAtRest;//set true if all objects in the scene are at rest (or very very close to 0 velocity)
	public AgentMetadata agent;
	public HandMetadata hand;
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
	public ColorId [] colors;
	public ColorBounds[] colorBounds;

	// Extras
	public Vector3[] reachablePositions;
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
    public System.Object actionReturn;
	public float currentTime;
    public SceneBounds sceneBounds;//return coordinates of the scene's bounds (center, size, extents)
}

[Serializable]
public class ServerAction
{
	public string action;
	public int agentCount = 1;
	public string quality;
	public bool makeAgentsVisible = true;
	public float timeScale = 1.0f;
	public float fixedDeltaTime = 0.02f;
	public string objectType;
	public int objectVariation;
	public string receptacleObjectType;
	public string receptacleObjectId;
	public float gridSize;
	public string[] excludeObjectIds;
    public string [] objectIds;
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
    public float handDistance;//used for max distance agent's hand can move
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
    public bool anywhere;//used for SpawnTargetCircle, GetSpawnCoordinatesAboveObject for if anywhere or only in agent view
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
	public bool renderClassImage;
	public bool renderObjectImage;
	public bool renderNormalsImage;
    public bool renderFlowImage;
	public float cameraY = 0.675f;
	public bool placeStationary = true; //when placing/spawning an object, do we spawn it stationary (kinematic true) or spawn and let physics resolve final position
	//public string ssao = "default";
	public string fillLiquid; //string to indicate what kind of liquid this object should be filled with. Water, Coffee, Wine etc.
	public float TimeUntilRoomTemp;
	public bool allowDecayTemperature = true; //set to true if temperature should decay over time, set to false if temp changes should not decay, defaulted true
	public string StateChange;//a string that specifies which state change to randomly toggle
    public float timeStep = 0.01f;
    public float mass;
    public float drag;
    public float angularDrag;
    public ObjectTypeCount[] numDuplicatesOfType; //specify, by object Type, how many duplicates of that given object type to try and spawn
    //use only the objectType class member to specify which receptacle objects should be excluded from the valid receptacles to spawn objects in
    public String[] excludedReceptacles;
    public ObjectPose[] objectPoses;
    public SetObjectStates SetObjectStates;
    public float minDistance;//used in target circle spawning function
    public float maxDistance;//used in target circle spawning function
    public float noise;
    public ControllerInitialization controllerInitialization = null;
    public string agentControllerType = "physics";//default to physics controller
    public string agentMode = "default"; //mode of Agent, valid values are "default" "bot" "drone", note certain modes are only compatible with certain controller types

    public float agentRadius = 2.0f;
    public int maxStepCount;
    public float rotateStepDegrees = 90.0f; //default rotation amount for RotateRight/RotateLeft actions

    public float degrees;//for overriding the default degree amount in look up/lookdown/rotaterRight/rotateLeft

    public bool topView = false;

    public bool orthographic = false;

    public bool grid = false;

    public Color? gridColor;

    public Gradient pathGradient;

	//should actions like pickup and moveHand have more manual, less abstracted behavior?
	public bool manualInteract = false;

	//color 0-255
	public float r;
	public float g;
	public float b;

	//default time for objects to wait before returning actionFinished() if an action put them in motion
	public float TimeToWaitForObjectsToComeToRest = 10.0f;
	public float scale;
    public string visibilityScheme = VisibilityScheme.Collider.ToString();
    public bool fastActionEmit;

    public SimObjType ReceptableSimObjType()
	{
		if (string.IsNullOrEmpty(receptacleObjectType))
		{
			return SimObjType.Undefined;
		}
		return (SimObjType)Enum.Parse(typeof(SimObjType), receptacleObjectType);
	}

    public VisibilityScheme GetVisibilityScheme() {
        VisibilityScheme result = VisibilityScheme.Collider;
        try 
        {
            result = (VisibilityScheme)Enum.Parse(typeof(VisibilityScheme), visibilityScheme, true);
        } 
        catch (ArgumentException) { 
            Debug.LogError("Error parsing visibilityScheme: '" + visibilityScheme + "' defaulting to Collider");
        }

		return result;
    }

	public SimObjType GetSimObjType()
	{
		if (string.IsNullOrEmpty(objectType))
		{
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
public class InitializeReturn
{
	public float cameraNearPlane;
    public float cameraFarPlane;
}

public enum ServerActionErrorCode  {
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
	AmbiguousAction
}

public enum VisibilityScheme {
    Collider,
    Distance
}



[Serializable]
public class ControllerInitialization {
    public Dictionary<string, TypedVariable> variableInitializations;
}


[Serializable]
public class TypedVariable {
    public string type;
    public object value;
}



public class ShouldSerializeContractResolver : DefaultContractResolver
{
   public static readonly ShouldSerializeContractResolver Instance = new ShouldSerializeContractResolver();

   protected override JsonProperty CreateProperty( MemberInfo member,
                                    MemberSerialization memberSerialization )
   {
      JsonProperty property = base.CreateProperty( member, memberSerialization );

      // exclude these properties to make serialization match JsonUtility
      if( property.DeclaringType == typeof(Vector3) &&
            (property.PropertyName == "sqrMagnitude" || 
            property.PropertyName == "magnitude"  ||
            property.PropertyName == "normalized" 
            ))
      {
         property.ShouldSerialize = instance => { return false; };
         return property;
      } else {
          return base.CreateProperty(member, memberSerialization);
      }

   }
}

public enum AgentState  {
	Processing,
    ActionComplete,
    PendingFixedUpdate,
	Emit
}
