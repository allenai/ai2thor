using System;
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
		StartCoroutine (addAgents (action));

	}

	private IEnumerator addAgents(ServerAction action) {

		for (int i = 1; i < action.agentCount && this.agents.Count < Math.Min(agentColors.Length, action.agentCount); i++) {
			addAgent (action);
			yield return null; // must do this so we wait a frame so that when we CapsuleCast we see the most recently added agent
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
			// XXX add to errorMessage
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


	private IEnumerator EmitFrame() {


		frameCounter += 1;

		// we should only read the screen buffer after rendering is complete
		yield return new WaitForEndOfFrame();


		WWWForm form = new WWWForm();

		if (!serverSideScreenshot) {
			// read screen contents into the texture

			RenderTexture currentTexture = RenderTexture.active;
			form.AddBinaryData("image", captureScreen(), "frame-" + frameCounter.ToString().PadLeft(7, '0') + ".rgb", "image/raw-rgb");

			DiscreteRemoteFPSAgentController[] agentsAr = this.agents.ToArray ();
			for(int i = 1; i < agentsAr.Length; i++) {
				DiscreteRemoteFPSAgentController agent = agentsAr [i];
				RenderTexture.active = agent.m_Camera.activeTexture;
				agent.m_Camera.Render ();
				form.AddBinaryData("image", captureScreen(), "frame-" + frameCounter.ToString().PadLeft(7, '0') + ".rgb", "image/raw-rgb");
			}

			RenderTexture.active = currentTexture;
		}

		MultiAgentMetadata multiMeta = new MultiAgentMetadata ();
		multiMeta.agents = new MetadataWrapper[this.agents.Count];
		multiMeta.activeAgentId = this.activeAgentId;
		multiMeta.sequenceId = this.currentSequenceId;

		for (int i = 0; i < this.agents.Count; i++) {
			multiMeta.agents [i] = this.agents.ToArray () [i].generateMetadataWrapper ();
			multiMeta.agents [i].agentId = i;
		}

		// for testing purposes, also write to a file in the project folder
		// File.WriteAllBytes(Application.dataPath + "/Screenshots/SavedScreen" + frameCounter.ToString() + ".png", bytes);
		// Debug.Log ("Frame Bytes: " + bytes.Length.ToString());
		//string img_str = System.Convert.ToBase64String (bytes);
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
	public int activeAgentId;
	public int sequenceId;
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
public struct MetadataWrapper
{
	public ObjectMetadata[] objects;
	public ObjectMetadata agent;
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
	public float y;
	public float x;
	public float z;
	public int sequenceId;
	public bool snapToGrid = true;
	public bool continuous;
	public string sceneName;
	public int sceneConfigIndex;
	public int agentPositionIndex;
	public bool rotateOnTeleport;
	public bool forceVisible;
	public int rotation;
	public int horizon;
	public bool randomizeOpen;
	public int pivot;
	public int randomSeed;
	public float moveMagnitude;
	public float visibilityDistance;
	public bool continuousMode;
	public bool uniquePickupableObjectTypes; // only allow one of each object type to be visible
	public ReceptacleObjectList[] receptacleObjects;
	public ReceptacleObjectPair[] excludeReceptacleObjectPairs;

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
	LookDownCantExceedMin
}
