using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
using System.Net.Sockets;
using System.Net;
using Newtonsoft.Json;
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
	public bool renderImage = true;
	protected bool renderDepthImage;
	protected bool renderClassImage;
	protected bool renderObjectImage;
	protected bool renderNormalsImage;
    protected bool renderFlowImage;
	private bool synchronousHttp = true;
	private Socket sock = null;
	private List<Camera> thirdPartyCameras = new List<Camera>();
	private bool readyToEmit;

	private Color[] agentColors = new Color[]{Color.blue, Color.yellow, Color.green, Color.red, Color.magenta, Color.grey};

	public int actionDuration = 3;

	private BaseFPSAgentController primaryAgent;

    private JavaScriptInterface jsInterface;

    protected PhysicsSceneManager physicsSceneManager;
    public int AdvancePhysicsStepCount = 0;

	void Awake() {

        tex = new Texture2D(UnityEngine.Screen.width, UnityEngine.Screen.height, TextureFormat.RGB24, false);
		readPixelsRect = new Rect(0, 0, UnityEngine.Screen.width, UnityEngine.Screen.height);

        #if !UNITY_WEBGL
            // Creates warning for WebGL
            // https://forum.unity.com/threads/rendering-without-using-requestanimationframe-for-the-main-loop.373331/
            Application.targetFrameRate = 300;
        #else
            Debug.unityLogger.logEnabled = false;
        #endif

        QualitySettings.vSyncCount = 0;
		robosimsPort = LoadIntVariable (robosimsPort, "PORT");
		robosimsHost = LoadStringVariable(robosimsHost, "HOST");
		serverSideScreenshot = LoadBoolVariable (serverSideScreenshot, "SERVER_SIDE_SCREENSHOT");
		robosimsClientToken = LoadStringVariable (robosimsClientToken, "CLIENT_TOKEN");
		bool trainPhase = true;
		trainPhase = LoadBoolVariable(trainPhase, "TRAIN_PHASE");

		// read additional configurations for model
		// agent speed and action length
		string prefix = trainPhase ? "TRAIN_" : "TEST_";


		

		actionDuration = LoadIntVariable(actionDuration, prefix + "ACTION_LENGTH");

	}

	void Start() {
		initializePrimaryAgent();
        primaryAgent.actionDuration = this.actionDuration;
        this.setReadyToEmit(true);
		Debug.Log("Graphics Tier: " + Graphics.activeTier);
		this.agents.Add (primaryAgent);

        physicsSceneManager = GameObject.Find("PhysicsSceneManager").GetComponent<PhysicsSceneManager>();
	}

	private void initializePrimaryAgent() {

		GameObject fpsController = GameObject.FindObjectOfType<BaseFPSAgentController>().gameObject;
		primaryAgent = fpsController.GetComponent<PhysicsRemoteFPSAgentController>();
		primaryAgent.enabled = true;
		primaryAgent.agentManager = this;
		primaryAgent.actionComplete = true;

	}
	
	public void Initialize(ServerAction action)
	{
        if (action.agentType != null && action.agentType.ToLower() == "stochastic") {
            this.agents.Clear();

            // stochastic must have these set to work properly
            action.continuous = true;
            action.snapToGrid = false;

            GameObject fpsController = GameObject.FindObjectOfType<BaseFPSAgentController>().gameObject;
            primaryAgent.enabled = false;

            primaryAgent = fpsController.GetComponent<StochasticRemoteFPSAgentController>();
            primaryAgent.agentManager = this;
            primaryAgent.enabled = true;
            // must manually call start here since it this only gets called before Update() is called
            primaryAgent.Start();
            this.agents.Add(primaryAgent);
        }
        
		primaryAgent.ProcessControlCommand (action);
		primaryAgent.IsVisible = action.makeAgentsVisible;
		this.renderClassImage = action.renderClassImage;
		this.renderDepthImage = action.renderDepthImage;
		this.renderNormalsImage = action.renderNormalsImage;
		this.renderObjectImage = action.renderObjectImage;
        this.renderFlowImage = action.renderFlowImage;
		if (action.alwaysReturnVisibleRange) {
			((PhysicsRemoteFPSAgentController) primaryAgent).alwaysReturnVisibleRange = action.alwaysReturnVisibleRange;
		}
		StartCoroutine (addAgents (action));
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

		this.setReadyToEmit(true);
	}

	public void AddThirdPartyCamera(ServerAction action) {
		GameObject gameObject = new GameObject("ThirdPartyCamera" + thirdPartyCameras.Count);
		gameObject.AddComponent(typeof(Camera));
		Camera camera = gameObject.GetComponentInChildren<Camera>();

		if (this.renderDepthImage || this.renderClassImage || this.renderObjectImage || this.renderNormalsImage || this.renderFlowImage) 
		{
			gameObject.AddComponent(typeof(ImageSynthesis));
		}

		this.thirdPartyCameras.Add(camera);
		gameObject.transform.eulerAngles = action.rotation;
		gameObject.transform.position = action.position;

        float fov;

        if(action.fieldOfView <= 0 || action.fieldOfView > 180)
        {
            //default to 90 fov on third party camera if nothing passed in, or if value is too large
            fov = 90f;
        }
        
        else
        {
            fov = action.fieldOfView;
        }

        camera.fieldOfView = fov;

		readyToEmit = true;
	}

	public void UpdateThirdPartyCamera(ServerAction action) {
		if (action.thirdPartyCameraId <= thirdPartyCameras.Count) {
			Camera thirdPartyCamera = thirdPartyCameras.ToArray()[action.thirdPartyCameraId];
			thirdPartyCamera.gameObject.transform.eulerAngles = action.rotation;
			thirdPartyCamera.gameObject.transform.position = action.position;
		} 
		readyToEmit = true;
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

	public virtual void setReadyToEmit(bool readyToEmit) {
		this.readyToEmit = readyToEmit;
	}

    // Decide whether agent has stopped actions
    // And if we need to capture a new frame

    public virtual void Update()
    {
        physicsSceneManager.isSceneAtRest = true;//assume the scene is at rest by default
    }

    private void LateUpdate() {
		int completeCount = 0;
		foreach (BaseFPSAgentController agent in this.agents) {
			if (agent.actionComplete) {
				completeCount++;
			}
		}

        //check what objects in the scene are currently in motion
        Rigidbody[] rbs = FindObjectsOfType(typeof(Rigidbody)) as Rigidbody[];
        foreach(Rigidbody rb in rbs)
        {
            //if this rigidbody is part of a SimObject, calculate rest using lastVelocity/currentVelocity comparisons
            if(rb.GetComponentInParent<SimObjPhysics>())
            {
                
                SimObjPhysics sop = rb.GetComponentInParent<SimObjPhysics>();
                
                float currentVelocity = Math.Abs(rb.angularVelocity.sqrMagnitude + rb.velocity.sqrMagnitude);
                float accel = (currentVelocity - sop.lastVelocity) / Time.fixedDeltaTime;

                if(accel == 0)
                {
                    sop.inMotion = false;
                }

                else
                {
                    //the rb's velocities are not 0, so it is in motion and the scene is not at rest
                    rb.GetComponentInParent<SimObjPhysics>().inMotion = true;
                    physicsSceneManager.isSceneAtRest = false;
                }

            }

            //this rigidbody is not a SimOBject, and might be a piece of a shattered sim object spawned in, or something
            else
            {
                //is the rigidbody at non zero velocity? then the scene is not at rest
                if(!(Math.Abs(rb.angularVelocity.sqrMagnitude + 
                rb.velocity.sqrMagnitude) < 0.001))
                {
                    physicsSceneManager.isSceneAtRest = false;
                }
            }
        }

		if (completeCount == agents.Count && completeCount > 0 && readyToEmit) {
			readyToEmit = false;
			StartCoroutine (EmitFrame ());
		}

        //ok now if the scene is at rest, turn back on physics autosimulation automatically
        //note: you can do this earlier by manually using the UnpausePhysicsAutoSim() action found in PhysicsRemoteFPSAgentController
        if(physicsSceneManager.isSceneAtRest && 
        physicsSceneManager.physicsSimulationPaused && AdvancePhysicsStepCount > 0)
        {
            //print("soshite toki wa ugoki desu");
            Physics.autoSimulation = true;
            physicsSceneManager.physicsSimulationPaused = false;
            AdvancePhysicsStepCount = 0;
        }

	}

	public byte[] captureScreen() {
		if (tex.height != UnityEngine.Screen.height || 
			tex.width != UnityEngine.Screen.width) {
			tex = new Texture2D(UnityEngine.Screen.width, UnityEngine.Screen.height, TextureFormat.RGB24, false);
			readPixelsRect = new Rect(0, 0, UnityEngine.Screen.width, UnityEngine.Screen.height);
		}
		tex.ReadPixels(readPixelsRect, 0, 0);
		tex.Apply();
		return tex.GetRawTextureData ();
	}


	private void addThirdPartyCameraImageForm(WWWForm form, Camera camera) {
		RenderTexture.active = camera.activeTexture;
		camera.Render ();
		form.AddBinaryData("image-thirdParty-camera", captureScreen());
	}

	private void addImageForm(WWWForm form, BaseFPSAgentController agent) {
		if (this.renderImage) {

			if (this.agents.Count > 1 || this.thirdPartyCameras.Count > 0) {
				RenderTexture.active = agent.m_Camera.activeTexture;
				agent.m_Camera.Render ();
			}
			form.AddBinaryData("image", captureScreen());
		}
	}

	private void addDepthImageForm(WWWForm form, BaseFPSAgentController agent) {
		if (this.renderDepthImage) {
			if (!agent.imageSynthesis.hasCapturePass ("_depth")) {
				Debug.LogError ("Depth image not available - returning empty image");
			}

			byte[] bytes = agent.imageSynthesis.Encode ("_depth");
			form.AddBinaryData ("image_depth", bytes);
		}
	}

	private void addObjectImageForm(WWWForm form, BaseFPSAgentController agent, ref MetadataWrapper metadata) {
		if (this.renderObjectImage) {
			if (!agent.imageSynthesis.hasCapturePass("_id")) {
				Debug.LogError("Object Image not available in imagesynthesis - returning empty image");
			}
			byte[] bytes = agent.imageSynthesis.Encode ("_id");
			form.AddBinaryData ("image_ids", bytes);
            metadata = this.UpdateMetadataColors(agent, metadata);
		}
    }

    public MetadataWrapper UpdateMetadataColors(BaseFPSAgentController agent, MetadataWrapper metadata) {
		if (this.renderObjectImage) {
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
        return metadata;
	}

	private void addImageSynthesisImageForm(WWWForm form, ImageSynthesis synth, bool flag, string captureName, string fieldName)
	{
		if (flag) {
			if (!synth.hasCapturePass (captureName)) {
				Debug.LogError (captureName + " not available - sending empty image");
			}
			byte[] bytes = synth.Encode (captureName);
			form.AddBinaryData (fieldName, bytes);


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
			if (synchronousHttp) {
				// must wait an additional frame when in synchronous mode otherwise the frame lags
				yield return new WaitForEndOfFrame();
			}
		}

		string msg = "{\"action\": \"RotateRight\"}";
		ProcessControlCommand(msg);
	}

    protected virtual WWWForm InitializeForm(WWWForm form) {
        return form;
    }

    protected virtual MultiAgentMetadata FinalizeMultiAgentMetadata(MultiAgentMetadata metadata) {
        return metadata;
    }

	public virtual IEnumerator EmitFrame() {


		frameCounter += 1;

		bool shouldRender = this.renderImage && serverSideScreenshot;

		if (shouldRender) {
			// we should only read the screen buffer after rendering is complete
			yield return new WaitForEndOfFrame();
		}

		WWWForm form = new WWWForm();

        form = this.InitializeForm(form);

        MultiAgentMetadata multiMeta = new MultiAgentMetadata ();
        multiMeta.agents = new MetadataWrapper[this.agents.Count];
        multiMeta.activeAgentId = this.activeAgentId;
        multiMeta.sequenceId = this.currentSequenceId;
		

		ThirdPartyCameraMetadata[] cameraMetadata = new ThirdPartyCameraMetadata[this.thirdPartyCameras.Count];
		RenderTexture currentTexture = null;
        JavaScriptInterface jsInterface = null;
        if (shouldRender) {
            currentTexture = RenderTexture.active;
            for (int i = 0; i < this.thirdPartyCameras.Count; i++) {
                ThirdPartyCameraMetadata cMetadata = new ThirdPartyCameraMetadata();
                Camera camera = thirdPartyCameras.ToArray()[i];
                cMetadata.thirdPartyCameraId = i;
                cMetadata.position = camera.gameObject.transform.position;
                cMetadata.rotation = camera.gameObject.transform.eulerAngles;
                cameraMetadata[i] = cMetadata;
                ImageSynthesis imageSynthesis = camera.gameObject.GetComponentInChildren<ImageSynthesis> () as ImageSynthesis;
                addThirdPartyCameraImageForm (form, camera);
                addImageSynthesisImageForm(form, imageSynthesis, this.renderDepthImage, "_depth", "image_thirdParty_depth");
                addImageSynthesisImageForm(form, imageSynthesis, this.renderNormalsImage, "_normals", "image_thirdParty_normals");
                addImageSynthesisImageForm(form, imageSynthesis, this.renderObjectImage, "_id", "image_thirdParty_image_ids");
                addImageSynthesisImageForm(form, imageSynthesis, this.renderClassImage, "_class", "image_thirdParty_classes");
                addImageSynthesisImageForm(form, imageSynthesis, this.renderClassImage, "_flow", "image_thirdParty_flow");//XXX fix this in a bit
            }
        }

        for (int i = 0; i < this.agents.Count; i++) {
            BaseFPSAgentController agent = this.agents.ToArray () [i];
            jsInterface = agent.GetComponent<JavaScriptInterface>();
            MetadataWrapper metadata = agent.generateMetadataWrapper ();
            metadata.agentId = i;
            // we don't need to render the agent's camera for the first agent
            if (shouldRender) {
                addImageForm (form, agent);
                addImageSynthesisImageForm(form, agent.imageSynthesis, this.renderDepthImage, "_depth", "image_depth");
                addImageSynthesisImageForm(form, agent.imageSynthesis, this.renderNormalsImage, "_normals", "image_normals");
                addObjectImageForm (form, agent, ref metadata);
                addImageSynthesisImageForm(form, agent.imageSynthesis, this.renderClassImage, "_class", "image_classes");
                addImageSynthesisImageForm(form, agent.imageSynthesis, this.renderFlowImage, "_flow", "image_flow");

                metadata.thirdPartyCameras = cameraMetadata;
            }
            multiMeta.agents [i] = metadata;
        }

        if (shouldRender) {
            RenderTexture.active = currentTexture;
        }

        multiMeta = this.FinalizeMultiAgentMetadata(multiMeta);

        var serializedMetadata = Newtonsoft.Json.JsonConvert.SerializeObject(multiMeta);
		#if UNITY_WEBGL

				// JavaScriptInterface jsI =  FindObjectOfType<JavaScriptInterface>();
				// jsInterface.SendAction(new ServerAction(){action = "Test"});
                if (jsInterface != null) {
					jsInterface.SendActionMetadata(serializedMetadata);
				}
        #endif

        //form.AddField("metadata", JsonUtility.ToJson(multiMeta));
        form.AddField("metadata", serializedMetadata);
        form.AddField("token", robosimsClientToken);

        #if !UNITY_WEBGL 
		if (synchronousHttp) {


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

                int sent = this.sock.Send(Encoding.ASCII.GetBytes(request));
                sent = this.sock.Send(rawData);

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
		} else {

			using (var www = UnityWebRequest.Post("http://" + robosimsHost + ":" + robosimsPort + "/train", form))
			{
				yield return www.SendWebRequest();

				if (www.isNetworkError || www.isHttpError)
				{
					Debug.Log("Error: " + www.error);
					yield break;
				}
				ProcessControlCommand(www.downloadHandler.text);
			}
		}
        #endif
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
		return this.agents.ToArray () [activeAgentId];
	}

	private void ProcessControlCommand(string msg)
	{

		ServerAction controlCommand = new ServerAction();

		JsonUtility.FromJsonOverwrite(msg, controlCommand);

		this.currentSequenceId = controlCommand.sequenceId;
		this.renderImage = controlCommand.renderImage;
		activeAgentId = controlCommand.agentId;
		if (controlCommand.action == "Reset") {
			this.Reset (controlCommand);
		} else if (controlCommand.action == "Initialize") {
			this.Initialize(controlCommand);
		} else if (controlCommand.action == "AddThirdPartyCamera") {
			this.AddThirdPartyCamera(controlCommand);
		} else if (controlCommand.action == "UpdateThirdPartyCamera") {
			this.UpdateThirdPartyCamera(controlCommand);
		} else {
			this.activeAgent().ProcessControlCommand (controlCommand);
            this.setReadyToEmit(true);
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
}

[Serializable]
public class ObjectMetadata
{
	public string name;
	public Vector3 position;
	public Vector3 rotation;
	public float cameraHorizon;
	public bool visible;
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
	///
	public bool pickupable;
	public bool isPickedUp;//if the pickupable object is actively being held by the agent

	public float mass;//mass is only for moveable and pickupable objects

	//salient materials are only for pickupable and moveable objects, for now static only objects do not report material back since we have to assign them manually
	public enum ObjectSalientMaterial {Metal, Wood, Plastic, Glass, Ceramic, Stone, Fabric, Rubber, Food, Paper, Wax, Soap, Sponge, Organic, Hollow} //salient materials that make up an object (ie: cell phone - metal, glass)

	public string [] salientMaterials; //salient materials that this object is made of as strings (see enum above). This is only for objects that are Pickupable or Moveable
	///
	public string[] receptacleObjectIds;
	public float distance;
	public String objectType;
	public string objectId;
	public string parentReceptacle;
	public string[] parentReceptacles;
	public float currentTime;
    public bool isMoving;//true if this game object currently has a non-zero velocity

    public WorldSpaceBounds objectBounds;

    // MCS Additions
    public string[] colorsFromMaterials;
    public Vector3 direction;
    public float distanceXZ;
    public Vector3 heading;
    public Vector3[] points;
    public string shape;
    public bool visibleInCamera;

	public ObjectMetadata() { }
}

[Serializable]
public class WorldSpaceBounds
{
    //8 corners of the box that bounds a sim object
    public Vector3[] objectBoundsCorners;
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
    public string objectType;
    public int count;
}

[Serializable]
public class ObjectPose
{
    public string objectName;
    public Vector3 position;
    public Vector3 rotation;
}

[Serializable]
public class ObjectToggle
{
    public string objectType;
    public bool isOn;
}

[Serializable]
public struct MetadataWrapper
{
	public ObjectMetadata[] objects;
	public ObjectMetadata[] structuralObjects;
    public bool isSceneAtRest;//set true if all objects in the scene are at rest (or very very close to 0 velocity)
	public ObjectMetadata agent;
	public HandMetadata hand;
	public float fov;
	public float clippingPlaneFar;
	public float clippingPlaneNear;
	public bool isStanding;
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

    // MCS Additions
    public string lastActionStatus;
    public float reachDistance;
}


[Serializable]
public class ServerAction
{
	public string action;
    public string agentMode = "tall"; //default to Tall version of Agent
	public int agentCount = 1;
	public string quality;
	public bool makeAgentsVisible = true;
	public float timeScale = 1.0f;
	public string objectType;
	public int objectVariation;
	public string receptacleObjectType;
	public string receptacleObjectId;
	public float gridSize;
	public string[] excludeObjectIds;
	public string objectId;
	public int agentId;
	public int thirdPartyCameraId;
	public float y;
	public float fieldOfView;
	public float x;
	public float z;
    public float pushAngle;
	public float horizon;
	public Vector3 rotation;
	public Vector3 position;
    public Vector3 direction;
    public float handDistance;//used for max distance agent's hand can move
	public List<Vector3> positions = null;
	public bool standing = true;
	public bool forceAction;
    public bool applyActionNoise = true;
    public float movementGaussianMu;
    public float movementGaussianSigma;
    public float rotateGaussianMu;
    public float rotateGaussianSigma;


	public bool forceKinematic;

	public float maxAgentsDistance = -1.0f;

	public bool alwaysReturnVisibleRange = false;
	public int sequenceId;
	public bool snapToGrid = true;
	public bool continuous;
	public string sceneName;
	public bool rotateOnTeleport;
	public bool forceVisible;
	public bool randomizeOpen;
	public int randomSeed;
	public float moveMagnitude;
	public bool autoSimulation = true;
	public float visibilityDistance;
	public bool continuousMode; //i don't think this is used right now? also how is this different from the continuous bool above?
	public bool uniquePickupableObjectTypes; // only allow one of each object type to be visible
	public float removeProb;
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
    public ObjectTypeCount[] numRepeats;
    public ObjectTypeCount[] minFreePerReceptacleType;
    public ObjectPose[] objectPoses;
    public ObjectToggle[] objectToggles;
    public float noise;
    public ControllerInitialization controllerInitialization = null;
    public string agentType;
    public float agentRadius = 2.0f;
    public int maxStepCount;

    public float rotateStepDegrees = 90.0f;

    public bool useAgentTransform = false;

    // MCS Additions
    public bool logs = false;
    public Vector3 objectDirection;
    public Vector3 receptacleObjectDirection;
    public MCSConfigScene sceneConfig;

    public SimObjType ReceptableSimObjType()
	{
		if (string.IsNullOrEmpty(receptacleObjectType))
		{
			return SimObjType.Undefined;
		}
		return (SimObjType)Enum.Parse(typeof(SimObjType), receptacleObjectType);
	}

	public SimObjType GetSimObjType()
	{

		if (string.IsNullOrEmpty(objectType))
		{
			return SimObjType.Undefined;
		}
		return (SimObjType)Enum.Parse(typeof(SimObjType), objectType);
	}
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
	InvalidAction
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
