﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;


public class AgentManager : MonoBehaviour
{
	private List<DiscreteRemoteFPSAgentController> agents = new List<DiscreteRemoteFPSAgentController>();

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
	private bool renderNormalsImage;
	private List<Camera> thirdPartyCameras = new List<Camera>();
	

	private bool readyToEmit;

	private Color[] agentColors = new Color[]{Color.blue, Color.cyan, Color.green, Color.red, Color.magenta, Color.yellow};

	public int actionDuration = 3;

	private DiscreteRemoteFPSAgentController primaryAgent;

	void Awake() {

		tex = new Texture2D(UnityEngine.Screen.width, UnityEngine.Screen.height, TextureFormat.RGB24, false);
		readPixelsRect = new Rect(0, 0, UnityEngine.Screen.width, UnityEngine.Screen.height);
	
		Application.targetFrameRate = 300;
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
		primaryAgent = GameObject.Find("FPSController").GetComponent<DiscreteRemoteFPSAgentController>();
        primaryAgent.actionDuration = this.actionDuration;
		readyToEmit = true;

		this.agents.Add (primaryAgent);
	}


	public void Initialize(ServerAction action)
	{
		
		primaryAgent.ProcessControlCommand (action);
		this.renderImage = action.renderImage;
		this.renderClassImage = action.renderClassImage;
		this.renderDepthImage = action.renderDepthImage;
		this.renderNormalsImage = action.renderNormalsImage;
		this.renderObjectImage = action.renderObjectImage;

		StartCoroutine (addAgents (action));

	}

	private IEnumerator addAgents(ServerAction action) {

		for (int i = 1; i < action.agentCount && this.agents.Count < Math.Min(agentColors.Length, action.agentCount); i++) {
			addAgent (action);
			yield return null; // must do this so we wait a frame so that when we CapsuleCast we see the most recently added agent
		}
		readyToEmit = true;
	}

	public void AddThirdPartyCamera(ServerAction action) {
		GameObject gameObject = new GameObject("ThirdPartyCamera" + thirdPartyCameras.Count);
		gameObject.AddComponent(typeof(Camera));
		Camera camera = gameObject.GetComponentInChildren<Camera>();
		this.thirdPartyCameras.Add(camera);
		gameObject.transform.eulerAngles = action.rotation;
		gameObject.transform.position = action.position;
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
		Vector3 clonePosition = agentStartPosition (primaryAgent);

		if (clonePosition.magnitude > 0) {
			GameObject visCapsule = primaryAgent.transform.Find ("VisibilityCapsule").gameObject;
			visCapsule.SetActive (true);


			DiscreteRemoteFPSAgentController clone = UnityEngine.Object.Instantiate (primaryAgent);
			clone.actionDuration = this.actionDuration;
			clone.m_Camera.targetDisplay = this.agents.Count;
			clone.transform.position = clonePosition;
			updateAgentColor(clone, agentColors[this.agents.Count]);
			clone.ProcessControlCommand (action);
			this.agents.Add (clone);
		} else {
			Debug.LogError ("couldn't find a valid start position for a new agent");
		}




	}

	private Vector3 agentStartPosition(DiscreteRemoteFPSAgentController agent) {

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

	private void updateAgentColor(DiscreteRemoteFPSAgentController agent, Color color) {
		foreach (MeshRenderer r in agent.gameObject.GetComponentsInChildren<MeshRenderer> () as MeshRenderer[]) {
			foreach (Material m in r.materials) {
				m.color = color;
			}

		}
	}

	public void Reset(ServerAction response) {
		if (string.IsNullOrEmpty(response.sceneName)){
			UnityEngine.SceneManagement.SceneManager.LoadScene (UnityEngine.SceneManagement.SceneManager.GetActiveScene ().name);
		} else {
			UnityEngine.SceneManagement.SceneManager.LoadScene (response.sceneName);
		}
	}

	// Decide whether agent has stopped actions
	// And if we need to capture a new frame


	
	private void LateUpdate() {

		int completeCount = 0;
		foreach (DiscreteRemoteFPSAgentController agent in this.agents) {
			if (agent.actionComplete) {
				completeCount++;
			}
		}


		if (completeCount == agents.Count && completeCount > 0 && readyToEmit) {
			readyToEmit = false;
			StartCoroutine (EmitFrame ());
		}

	}

	private byte[] captureScreen() {
		tex.ReadPixels(readPixelsRect, 0, 0);
		tex.Apply();
		return tex.GetRawTextureData ();
	}


	private void addThirdPartyCameraImageForm(WWWForm form, Camera camera) {
		RenderTexture.active = camera.activeTexture;
		camera.Render ();
		form.AddBinaryData("image-thirdParty-camera", captureScreen());
	}

	private void addImageForm(WWWForm form, DiscreteRemoteFPSAgentController agent) {
		if (this.renderImage) {

			if (this.agents.Count > 1 || this.thirdPartyCameras.Count > 0) {
				RenderTexture.active = agent.m_Camera.activeTexture;
				agent.m_Camera.Render ();
			}
			form.AddBinaryData("image", captureScreen());
		}
	}

	private void addObjectImageForm(WWWForm form, DiscreteRemoteFPSAgentController agent, ref MetadataWrapper metadata) {
		if (this.renderObjectImage) {
			if (!agent.imageSynthesis.hasCapturePass("_id")) {
				Debug.LogError("Object Image not available in imagesynthesis - returning empty image");
			}
			byte[] bytes = agent.imageSynthesis.Encode ("_id");
			form.AddBinaryData ("image_ids", bytes);

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
				bounds.color = new byte[] {
					(byte)Math.Round (key.r * 255),
					(byte)Math.Round (key.g * 255),
					(byte)Math.Round (key.b * 255)
				};
				bounds.bounds = colorBounds [key];
				boundsList.Add (bounds);
			}
			metadata.colorBounds = boundsList.ToArray ();

			List<ColorId> colors = new List<ColorId> ();
			foreach (Color key in agent.imageSynthesis.colorIds.Keys) {
				ColorId cid = new ColorId ();
				cid.color = new byte[] {
					(byte)Math.Round (key.r * 255),
					(byte)Math.Round (key.g * 255),
					(byte)Math.Round (key.b * 255)
				};

				cid.name = agent.imageSynthesis.colorIds [key];
				colors.Add (cid);
			}
			metadata.colors = colors.ToArray ();

		}
	}

	private void addImageSynthesisImageForm(WWWForm form, DiscreteRemoteFPSAgentController agent, bool flag, string captureName, string fieldName)
	{
		if (flag) {
			if (!agent.imageSynthesis.hasCapturePass (captureName)) {
				Debug.LogError (captureName + " not available - sending empty image");
			}
			byte[] bytes = agent.imageSynthesis.Encode (captureName);
			form.AddBinaryData (fieldName, bytes);


		}
	}

	private IEnumerator EmitFrame() {


		frameCounter += 1;

		// we should only read the screen buffer after rendering is complete
		yield return new WaitForEndOfFrame();


		WWWForm form = new WWWForm();

		MultiAgentMetadata multiMeta = new MultiAgentMetadata ();
		multiMeta.agents = new MetadataWrapper[this.agents.Count];
		ThirdPartyCameraMetadata[] cameraMetadata = new ThirdPartyCameraMetadata[this.thirdPartyCameras.Count];
		multiMeta.activeAgentId = this.activeAgentId;
		multiMeta.sequenceId = this.currentSequenceId;
		RenderTexture currentTexture = RenderTexture.active;

		for (int i = 0; i < this.thirdPartyCameras.Count; i++) {
			ThirdPartyCameraMetadata cMetadata = new ThirdPartyCameraMetadata();
			Camera camera = thirdPartyCameras.ToArray()[i];
			cMetadata.thirdPartyCameraId = i;
			cMetadata.position = camera.gameObject.transform.position;
			cMetadata.rotation = camera.gameObject.transform.eulerAngles;
			cameraMetadata[i] = cMetadata;
			addThirdPartyCameraImageForm (form, camera);
		}

		for (int i = 0; i < this.agents.Count; i++) {
			DiscreteRemoteFPSAgentController agent = this.agents.ToArray () [i];
			MetadataWrapper metadata = agent.generateMetadataWrapper ();
			metadata.agentId = i;
			// we don't need to render the agent's camera for the first agent
			addImageForm (form, agent);
			addImageSynthesisImageForm(form, agent, this.renderDepthImage, "_depth", "image_depth");
			addImageSynthesisImageForm(form, agent, this.renderNormalsImage, "_normals", "image_normals");
			addObjectImageForm (form, agent, ref metadata);
			addImageSynthesisImageForm(form, agent, this.renderClassImage, "_class", "image_classes");
			metadata.thirdPartyCameras = cameraMetadata;
			multiMeta.agents [i] = metadata;
		}

		RenderTexture.active = currentTexture;

		form.AddField("metadata", JsonUtility.ToJson(multiMeta));
		form.AddField("token", robosimsClientToken);
		WWW w = new WWW ("http://" + robosimsHost + ":" + robosimsPort + "/train", form);
		yield return w;

		if (!string.IsNullOrEmpty (w.error)) {
			Debug.Log ("Error: " + w.error);
			yield break;
		} else {
			ProcessControlCommand (w.text);
		}
	}

	private DiscreteRemoteFPSAgentController activeAgent() {
		return this.agents.ToArray () [activeAgentId];
	}


	private void ProcessControlCommand(string msg)
	{


		ServerAction controlCommand = JsonUtility.FromJson<ServerAction>(msg);
		this.currentSequenceId = controlCommand.sequenceId;
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
			readyToEmit = true;
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
	public int receptacleCount;
	public bool openable;
	public bool pickupable;
	public bool isopen;
	public string[] receptacleObjectIds;
	public PivotSimObj[] pivotSimObjs;
	public float distance;
	public String objectType;
	public string objectId;
	public float[] bounds3D;
	public string parentReceptacle;
}

[Serializable]
public class InventoryObject
{
	public string objectId;
	public string objectType;
}

[Serializable]
public class PivotSimObj
{
	public int pivotId;
	public string objectId;
}

[Serializable]
public class ColorId {
	public byte[] color;
	public string name;
}

[Serializable]
public class ColorBounds {
	public byte[] color;
	public int[] bounds;
}


[Serializable]
public struct MetadataWrapper
{
	public ObjectMetadata[] objects;
	public ObjectMetadata agent;
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
}



[Serializable]
public class ServerAction
{
	public string action;
	public int agentCount = 1;
	public string objectType;
	public string receptacleObjectType;
	public string receptacleObjectId;
	public float gridSize;
	public string[] excludeObjectIds;
	public string objectId;
	public int agentId;
	public int thirdPartyCameraId;
	public float y;
	public float x;
	public float z;
	public int horizon;
	public Vector3 rotation;
	public Vector3 position;
	public int sequenceId;
	public bool snapToGrid = true;
	public bool continuous;
	public string sceneName;
	public bool rotateOnTeleport;
	public bool forceVisible;
	public bool randomizeOpen;
	public int pivot;
	public int randomSeed;
	public float moveMagnitude;
	public float visibilityDistance;
	public bool continuousMode;
	public bool uniquePickupableObjectTypes; // only allow one of each object type to be visible
	public ReceptacleObjectList[] receptacleObjects;
	public ReceptacleObjectPair[] excludeReceptacleObjectPairs;
	public float removeProb;
	public int maxNumRepeats;
	public bool randomizeObjectAppearance;
	public bool renderImage = true;
	public bool renderDepthImage;
	public bool renderClassImage;
	public bool renderObjectImage;
	public bool renderNormalsImage;
	public float cameraY;

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

[Serializable]
public class ReceptacleObjectPair
{
	public string receptacleObjectId;
	public string objectId;
	public int pivot;
}


[Serializable]
public class ReceptacleObjectList
{
	public string receptacleObjectType;
	public string[] itemObjectTypes;
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
