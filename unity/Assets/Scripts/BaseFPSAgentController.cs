// Copyright Allen Institute for Artificial Intelligence 2017
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using UnityEngine;
using Random = UnityEngine.Random;


namespace UnityStandardAssets.Characters.FirstPerson
{
	[RequireComponent(typeof(CharacterController))]

	public class BaseFPSAgentController : MonoBehaviour
	{
		// first person controller parameters
		[SerializeField]
		protected bool m_IsWalking;
		[SerializeField]
		protected float m_WalkSpeed;
		[SerializeField]
		protected float m_RunSpeed;
		[SerializeField]
		protected float m_GravityMultiplier;
		[SerializeField]
		protected bool m_UseFovKick;
		[SerializeField]
		protected FOVKick m_FovKick = new FOVKick();
		[SerializeField]
		private bool m_UseHeadBob;
		[SerializeField]
		private CurveControlledBob m_HeadBob = new CurveControlledBob();
		[SerializeField]
		protected LerpControlledBob m_JumpBob = new LerpControlledBob();
		[SerializeField]
		private float m_StepInterval;
		protected SimObjType[] OpenableTypes = new SimObjType[] { SimObjType.Fridge, SimObjType.Cabinet, SimObjType.Microwave, SimObjType.LightSwitch, SimObjType.Blinds, SimObjType.Book, SimObjType.Toilet };
		protected SimObjType[] ImmobileTypes = new SimObjType[] { SimObjType.Chair, SimObjType.Toaster, SimObjType.CoffeeMachine, SimObjType.Television, SimObjType.StoveKnob };

		protected float[] headingAngles = new float[] { 0.0f, 90.0f, 180.0f, 270.0f };
		protected float[] horizonAngles = new float[] { 60.0f, 30.0f, 0.0f, 330.0f };

		protected Dictionary<SimObjType, Dictionary<string, int>> OPEN_CLOSE_STATES = new Dictionary<SimObjType, Dictionary<string, int>>{
			{SimObjType.Microwave, new Dictionary<string, int>{{"open", 2}, {"close", 1}}},
			{SimObjType.Laptop, new Dictionary<string, int>{{"open", 2}, {"close", 1}}},
			{SimObjType.Book, new Dictionary<string, int>{{"open", 1}, {"close", 2}}},
			{SimObjType.Toilet, new Dictionary<string, int>{{"open", 2}, {"close", 3}}},
			{SimObjType.Sink, new Dictionary<string, int>{{"open", 2}, {"close", 1}}}
		};



		public string[] excludeObjectIds = new string[0];
		protected Camera m_Camera;
		protected string ENVIRONMENT_PREFIX = "AI2THOR_";
		protected bool m_Jump;
		protected float m_XRotation;
		protected float m_ZRotation;
		protected Vector2 m_Input;
		protected Vector3 m_MoveDir = Vector3.zero;
		protected CharacterController m_CharacterController;
		protected CollisionFlags m_CollisionFlags;
		protected bool m_PreviouslyGrounded;
		protected bool m_Jumping;
		protected Vector3 lastPosition;
		// Vector3 m_OriginalCameraPosition;
		protected string lastAction;
		protected bool lastActionSuccess;
		protected string errorMessage;
		protected ServerActionErrorCode errorCode;
		protected int currentSequenceId;



		float defaultMaxVisibleDistance = 1.0f;
		float maxVisibleDistance;

		// initial states
		protected Vector3 init_position;
		protected Quaternion init_rotation;

		// server controls
		// agent movement parameters
		public float forwardVelocity = 2.0f;
		public float rotateVelocity = 2.0f;
		public int actionDuration = 3;

		// internal state variables
		private float lastEmitTime;
		protected List<string> collisionsInAction; // tracking collided objects
		protected string[] collidedObjects;      // container for collided objects


		private Quaternion targetRotation;
		public Quaternion TargetRotation
		{
			get { return targetRotation; }
		}

		// Initialize parameters from environment variables
		protected virtual void Awake()
		{
			Application.targetFrameRate = 300;
            QualitySettings.vSyncCount = 0;

			// whether it's in training or test phase
			bool trainPhase = true;
			trainPhase = LoadBoolVariable(trainPhase, "TRAIN_PHASE");

			// read additional configurations for model
			// agent speed and action length
			string prefix = trainPhase ? "TRAIN_" : "TEST_";
			//forwardVelocity = LoadFloatVariable (forwardVelocity, prefix + "WALK_VELOCITY");
			//rotateVelocity = LoadFloatVariable (rotateVelocity, prefix + "TURN_VELOCITY");
			actionDuration = LoadIntVariable(actionDuration, prefix + "ACTION_LENGTH");
			maxVisibleDistance = LoadFloatVariable(defaultMaxVisibleDistance, prefix + "VISIBILITY_DISTANCE");


			// character controller parameters
			m_CharacterController = GetComponent<CharacterController>();
			//float radius = m_CharacterController.radius;
			m_CharacterController.radius = 0.2f;
			// using default for now to remain consistent with generated points
			//m_CharacterController.height = LoadFloatVariable (height, "AGENT_HEIGHT");
			this.m_WalkSpeed = 2;
			this.m_RunSpeed = 10;
			this.m_GravityMultiplier = 2;
			this.m_UseFovKick = true;
			this.m_StepInterval = 5;
		}


		// Use this for initialization
		protected virtual void Start()
		{
			m_Camera = Camera.main;
			//m_OriginalCameraPosition = m_Camera.transform.localPosition;
			m_FovKick.Setup(m_Camera);
			m_HeadBob.Setup(m_Camera, m_StepInterval);
			m_Jumping = false;

			// set agent initial states
			targetRotation = transform.rotation;
			collidedObjects = new string[0];
			collisionsInAction = new List<string>();

			// record initial positions and rotations
			init_position = transform.position;
			init_rotation = transform.rotation;

			//allowNodes = false;
		}

		protected virtual byte[] captureScreen()
		{
			int width = Screen.width;
			int height = Screen.height;

			Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);


			// read screen contents into the texture
			tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
			tex.Apply();

			// encode texture into JPG - XXX SHOULD SET QUALITY
			byte[] bytes = tex.EncodeToJPG();
			Destroy(tex);
			return bytes;
		}


		protected virtual void actionFinished(bool success) { }

		protected bool closeSimObj(SimObj so)
		{
			bool res = false;
			if (OPEN_CLOSE_STATES.ContainsKey(so.Type))
			{
				res = updateAnimState(so.Animator, OPEN_CLOSE_STATES[so.Type]["close"]);
			}
			else if (so.IsAnimated)
			{
				res = updateAnimState(so.Animator, false);
			}

			return res;
		}

		protected bool openSimObj(SimObj so)
		{

			bool res = false;
			if (OPEN_CLOSE_STATES.ContainsKey(so.Type))
			{
				res = updateAnimState(so.Animator, OPEN_CLOSE_STATES[so.Type]["open"]);

			}
			else if (so.IsAnimated)
			{
				res = updateAnimState(so.Animator, true);
			}

			return res;
        }


		private bool updateAnimState(Animator anim, int value)
		{
			AnimatorControllerParameter param = anim.parameters[0];

			if (anim.GetInteger(param.name) == value)
			{
				return false;
			}
			else
			{
				anim.SetInteger(param.name, value);
				return true;
			}
		}

		private bool updateAnimState(Animator anim, bool value)
		{
			AnimatorControllerParameter param = anim.parameters[0];

			if (anim.GetBool(param.name) == value)
			{
				return false;
			}
			else
			{
				anim.SetBool(param.name, value);
				return true;
			}
		}

		// rotate view with respect to mouse or server controls
		protected virtual void RotateView()
		{
			// turn up & down
			if (Mathf.Abs(m_XRotation) > Mathf.Epsilon)
			{
				transform.Rotate(Vector3.right * m_XRotation, Space.Self);
			}

			// turn left & right
			if (Mathf.Abs(m_ZRotation) > Mathf.Epsilon)
			{
				transform.Rotate(Vector3.up * m_ZRotation, Space.Self);
			}

			// heading
			float eulerX = Mathf.Round(transform.eulerAngles.x);

			// rotating
			float eulerY = Mathf.Round(transform.eulerAngles.y);

			// TODO: make this as a precondition
			// move this out of Unity
			// constrain vertical turns in safe range
			float X_SAFE_RANGE = 30.0f;
			if (eulerX < 180.0f)
			{
				eulerX = Mathf.Min(X_SAFE_RANGE, eulerX);
			}
			else
			{
				eulerX = 360.0f - Mathf.Min(X_SAFE_RANGE, 360.0f - eulerX);
			}

			// freeze y-axis
			transform.rotation = Quaternion.Euler(eulerX, eulerY, 0);

		}




		// Check if agent is collided with other objects
		protected bool IsCollided()
		{
			return collisionsInAction.Count > 0;
		}

		protected virtual MetadataWrapper generateMetadataWrapper()
		{
			ObjectMetadata agentMeta = new ObjectMetadata();
			agentMeta.name = "agent";
			agentMeta.position = transform.position;
			agentMeta.rotation = transform.eulerAngles;
			agentMeta.cameraHorizon = m_Camera.transform.rotation.eulerAngles.x;

			MetadataWrapper metaMessage = new MetadataWrapper();
			metaMessage.agent = agentMeta;
			metaMessage.sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
			metaMessage.objects = generateObjectMetadataForTag("SimObj", false);
			metaMessage.collided = collidedObjects.Length > 0;
			metaMessage.collidedObjects = collidedObjects;
			metaMessage.screenWidth = Screen.width;
			metaMessage.screenHeight = Screen.height;

			return metaMessage;
		}


		public SimObj[] VisibleSimObjs()
		{
			return SimUtil.GetAllVisibleSimObjs(m_Camera, maxVisibleDistance);
		}


		public SimObj[] VisibleSimObjs(bool forceVisible)
		{
			if (forceVisible)
			{
				return GameObject.FindObjectsOfType(typeof(SimObj)) as SimObj[];
			}
			else
			{
				return VisibleSimObjs();

			}
		}

		public bool IsOpen(SimObj simobj)
		{
			Animator anim = simobj.Animator;
			AnimatorControllerParameter param = anim.parameters[0];
			if (OPEN_CLOSE_STATES.ContainsKey(simobj.Type))
			{
				return anim.GetInteger(param.name) == OPEN_CLOSE_STATES[simobj.Type]["open"];
			}
			else
			{
				return anim.GetBool(param.name);
			}

		}

		public bool IsOpenable(SimObj so)
		{

			return Array.IndexOf(OpenableTypes, so.Type) >= 0 && so.IsAnimated;
		}


		public bool IsPickupable(SimObj so)
		{
			return !IsOpenable(so) && !so.IsReceptacle && !(Array.IndexOf(ImmobileTypes, so.Type) >= 0);
		}

		public bool excludeObject(SimObj so)
		{
			return Array.IndexOf(this.excludeObjectIds, so.UniqueID) >= 0;
		}

		private ObjectMetadata[] generateObjectMetadataForTag(string tag, bool isAnimated)
		{
			// Encode these in a json string and send it to the server
			SimObj[] simObjects = GameObject.FindObjectsOfType(typeof(SimObj)) as SimObj[];

			HashSet<SimObj> visibleObjectIds = new HashSet<SimObj>();
			foreach (SimObj so in VisibleSimObjs())
			{
				visibleObjectIds.Add(so);
			}
			int numObj = simObjects.Length;
			List<ObjectMetadata> metadata = new List<ObjectMetadata>();
			for (int k = 0; k < numObj; k++)
			{
				ObjectMetadata meta = new ObjectMetadata();
				SimObj simObj = simObjects[k];
				if (this.excludeObject(simObj))
				{
					continue;
				}
				GameObject o = simObj.gameObject;


				meta.name = o.name;
				meta.position = o.transform.position;
				meta.rotation = o.transform.eulerAngles;

				meta.objectType = Enum.GetName(typeof(SimObjType), simObj.Type);
				meta.receptacle = simObj.IsReceptacle;
				meta.openable = IsOpenable(simObj);
				if (meta.openable)
				{
					meta.isopen = IsOpen(simObj);
				}
				meta.pickupable = IsPickupable(simObj);

				if (meta.receptacle)
				{
					List<string> receptacleObjectIds = new List<string>();
					foreach (SimObj cso in SimUtil.GetItemsFromReceptacle(simObj.Receptacle))
					{
						receptacleObjectIds.Add(cso.UniqueID);

					}
					List<PivotSimObj> pivotSimObjs = new List<PivotSimObj>();
					for (int i = 0; i < simObj.Receptacle.Pivots.Length; i++)
					{
						Transform t = simObj.Receptacle.Pivots[i];
						if (t.childCount > 0)
						{
							SimObj psimobj = t.GetChild(0).GetComponent<SimObj>();
							PivotSimObj pso = new PivotSimObj();
							pso.objectId = psimobj.UniqueID;
							pso.pivotId = i;
							pivotSimObjs.Add(pso);
						}
					}
					meta.pivotSimObjs = pivotSimObjs.ToArray();
					meta.receptacleObjectIds = receptacleObjectIds.ToArray();
					meta.receptacleCount = simObj.Receptacle.Pivots.Length;

				}

				meta.objectId = simObj.UniqueID;


				meta.visible = (visibleObjectIds.Contains(simObj));
				meta.distance = Vector3.Distance(transform.position, o.transform.position);

				metadata.Add(meta);
			}
			return metadata.ToArray();

		}






		// Handle collisions
		protected void OnControllerColliderHit(ControllerColliderHit hit)
		{
			if (!enabled)
			{
				return;
			}

			if (hit.gameObject.name.Equals("Floor"))
			{
				return;
			}


			if (!collisionsInAction.Contains(hit.gameObject.name))
			{
				Debug.Log("Collided with " + hit.gameObject.name);
				collisionsInAction.Add(hit.gameObject.name);
			}

			Rigidbody body = hit.collider.attachedRigidbody;
			// don't move the rigidbody if the character is on top of it
			if (m_CollisionFlags == CollisionFlags.Below)
			{
				return;
			}

			if (body == null || body.isKinematic)
			{
				return;
			}
			body.AddForceAtPosition(m_CharacterController.velocity * 0.1f, hit.point, ForceMode.Impulse);
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



		protected void ProcessControlCommand(string msg)
		{
			errorMessage = "";
			errorCode = ServerActionErrorCode.Undefined;
			collisionsInAction = new List<string>();

			ServerAction controlCommand = JsonUtility.FromJson<ServerAction>(msg);
			this.currentSequenceId = controlCommand.sequenceId;
			lastAction = controlCommand.action;
			lastActionSuccess = false;
			lastPosition = new Vector3 (transform.position.x, transform.position.y, transform.position.z);
			System.Reflection.MethodInfo method = this.GetType ().GetMethod (controlCommand.action);
			try
			{
				method.Invoke(this, new object[] { controlCommand });
			}
			catch (Exception e)
			{
				Debug.LogError("caught error with invoke");
				Debug.LogError(e);
				errorMessage += e.ToString();
				actionFinished(false);
			}
		}


	}
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
	public int sequenceId;
}



[Serializable]
public class ServerAction
{
	public string action;
	public string objectType;
	public string receptacleObjectType;
	public string receptacleObjectId;
	public float gridSize;
	public string[] excludeObjectIds;
	public string objectId;
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
