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

namespace UnityStandardAssets.Characters.FirstPerson
{
	[RequireComponent(typeof(CharacterController))]

	abstract public class BaseFPSAgentController : MonoBehaviour
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

		protected static float gridSize = 0.25f;
		protected float moveMagnitude;

		protected bool continuousMode;
		public ImageSynthesis imageSynthesis;

		private List<Renderer> capsuleRenderers = null;

        private bool isVisible = true;

        public bool IsVisible
        {
			get { return isVisible; }
			set {
				if (capsuleRenderers == null) {
					GameObject visCapsule = this.transform.Find ("VisibilityCapsule").gameObject;
					capsuleRenderers = new List<Renderer>();
					foreach (Renderer r in visCapsule.GetComponentsInChildren<Renderer>()) {
						if (r.enabled) {
							capsuleRenderers.Add(r);
						}
					}
				}
				// DO NOT DISABLE THE VIS CAPSULE, instead disable the renderers below.
				foreach (Renderer r in capsuleRenderers) {
					r.enabled = value;
				}
				isVisible = value;
			}
        }

		//[SerializeField]
		//protected bool m_UseFovKick;
		//[SerializeField]
		//protected FOVKick m_FovKick = new FOVKick();
		//[SerializeField]
		//private bool m_UseHeadBob;
		//[SerializeField]
		//private CurveControlledBob m_HeadBob = new CurveControlledBob();
		//[SerializeField]
		//protected LerpControlledBob m_JumpBob = new LerpControlledBob();
		//[SerializeField]
		//private float m_StepInterval;


		protected float[] headingAngles = new float[] { 0.0f, 90.0f, 180.0f, 270.0f };
		protected float[] horizonAngles = new float[] { 60.0f, 30.0f, 0.0f, 330.0f };

		//allow agent to push sim objects that can move, for physics
		protected bool PushMode = false;
		protected int actionCounter;
		protected Vector3 targetTeleport;
        public AgentManager agentManager;




		public string[] excludeObjectIds = new string[0];
		public Camera m_Camera;
		//protected bool m_Jump;
		protected float m_XRotation;
		protected float m_ZRotation;
		protected Vector2 m_Input;
		protected Vector3 m_MoveDir = Vector3.zero;
		public CharacterController m_CharacterController;
		protected CollisionFlags m_CollisionFlags;
		//protected bool m_PreviouslyGrounded;
		//protected bool m_Jumping;
		protected Vector3 lastPosition;
		protected string lastAction;
		protected bool lastActionSuccess;
		protected string errorMessage;
		protected ServerActionErrorCode errorCode;
		public bool actionComplete;
		public System.Object actionReturn;


        // Vector3 m_OriginalCameraPosition;


        public float maxVisibleDistance = 1.5f; //changed from 1.0f to account for objects randomly spawned far away on tables/countertops, which would be not visible at 1.0f

		// initial states
		protected Vector3 init_position;
		protected Quaternion init_rotation;

		// server controls
		// agent movement parameters
		//public float forwardVelocity = 2.0f;
		//public float rotateVelocity = 2.0f;
		public int actionDuration = 3;

		// internal state variables
		private float lastEmitTime;
		protected List<string> collisionsInAction; // tracking collided objects
		protected string[] collidedObjects;      // container for collided objects


		private Quaternion targetRotation;

        // Javascript communication
        private JavaScriptInterface jsInterface;
        private ServerAction currentServerAction;

        protected float angleStepDegrees = 90.0f;
		public Quaternion TargetRotation
		{
			get { return targetRotation; }
		}

		// Initialize parameters from environment variables
		protected virtual void Awake()
		{
            #if UNITY_WEBGL
                this.jsInterface = this.GetComponent<JavaScriptInterface>();
                this.jsInterface.enabled = true;
            #endif
            // whether it's in training or test phase         
            // character controller parameters
            m_CharacterController = GetComponent<CharacterController>();
			//float radius = m_CharacterController.radius;
			//m_CharacterController.radius = 0.2f;
			// using default for now to remain consistent with generated points
			//m_CharacterController.height = LoadFloatVariable (height, "AGENT_HEIGHT");
			this.m_WalkSpeed = 2;
			this.m_RunSpeed = 10;
			this.m_GravityMultiplier = 2;
			//this.m_UseFovKick = true;
			//this.m_StepInterval = 5;
		}


		// Use this for initialization
		public virtual void Start()
		{
			m_Camera = this.gameObject.GetComponentInChildren<Camera>();
			//m_OriginalCameraPosition = m_Camera.transform.localPosition;
			//m_FovKick.Setup(m_Camera);
			//m_HeadBob.Setup(m_Camera, m_StepInterval);
			//m_Jumping = false;

			// set agent initial states
			targetRotation = transform.rotation;
			collidedObjects = new string[0];
			collisionsInAction = new List<string>();

			// record initial positions and rotations
			init_position = transform.position;
			init_rotation = transform.rotation;

			agentManager = GameObject.Find("PhysicsSceneManager").GetComponentInChildren<AgentManager>();

			//disabling in editor by default so performance in editor isn't garbage all the time. Enable this from the DebugInputField -InitSynth
            // #if UNITY_EDITOR
            //     this.enableImageSynthesis();
            // #endif
			//allowNodes = false;
		}

		public void actionFinished(bool success, System.Object actionReturn=null) 
		{
			
			if (actionComplete) 
			{
				Debug.LogError ("ActionFinished called with actionComplete already set to true");
			}

            if (this.jsInterface)
            {
                // TODO: Check if the reflection method call was successfull add that to the sent event data
                this.jsInterface.SendAction(currentServerAction);
            }

            lastActionSuccess = success;
			this.actionComplete = true;
			this.actionReturn = actionReturn;
			actionCounter = 0;
			targetTeleport = Vector3.zero;
		}

		abstract public Vector3[] getReachablePositions(float gridMultiplier=1.0f, int maxStepCount = 10000);

		public void Initialize(ServerAction action)
        {
            if (action.gridSize == 0)
            {
                action.gridSize = 0.25f;
            }


			// make fov backwards compatible
			if (action.fov != 60f && action.fieldOfView == 60f) {
				action.fieldOfView = action.fov;
			}

			if (action.fieldOfView > 0 && action.fieldOfView < 180) {
				m_Camera.fieldOfView = action.fieldOfView;
			} else {
				errorMessage = "fov must be in (0, 180) noninclusive.";
                Debug.Log(errorMessage);
                actionFinished(false);
			}

			if (action.timeScale > 0) {
				if (Time.timeScale != action.timeScale) {
                	Time.timeScale = action.timeScale;
				}
            } else {
                errorMessage = "Time scale must be >0";
                Debug.Log(errorMessage);
                actionFinished(false);
            }

			this.continuousMode = action.continuous;

            if (action.renderDepthImage || action.renderClassImage || action.renderObjectImage || action.renderNormalsImage) 
            {
    			this.enableImageSynthesis ();
    		}

			if (action.visibilityDistance > 0.0f) {
				this.maxVisibleDistance = action.visibilityDistance;
			}

			if (action.cameraY > 0.0) 
            {
				Vector3 pos = m_Camera.transform.localPosition;
				m_Camera.transform.localPosition = new Vector3 (pos.x, action.cameraY, pos.z);
			}


            if (action.gridSize <= 0 || action.gridSize > 5)
            {
                errorMessage = "grid size must be in the range (0,5]";
                Debug.Log(errorMessage);
                actionFinished(false);
            }
            else
            {
                gridSize = action.gridSize;
                StartCoroutine(checkInitializeAgentLocationAction());
            }

            // Rotation
            var epsilon = 1e-4;
            var epsilonBig = 1e-3;
            if (Mathf.Abs(action.rotateStepDegrees - 90.0f) > epsilonBig) {
                var ratio = 360.0f / action.rotateStepDegrees;
                var angleStepNumber = Mathf.RoundToInt(ratio);
                
                if (Mathf.Abs(ratio - angleStepNumber) > epsilon) {
                    errorMessage = "Invalid argument 'rotateStepDegrees': 360 should be divisible by 'rotateStepDegrees'.";
                    Debug.Log(errorMessage);
                    actionFinished(false);
                }
                else {
                    Debug.Log("Setting heading angles with " + action.rotateStepDegrees);
                    this.headingAngles = new float[angleStepNumber];
                    for (int i = 0; i < angleStepNumber; i++) {
                        headingAngles[i] = i * action.rotateStepDegrees;
                    }

                    this.angleStepDegrees = action.rotateStepDegrees;

                    Debug.Log("Total "  + string.Join(",", headingAngles.Select(x => x.ToString()).ToArray()));
                }
            }

			//override default ssao settings when using init
			string ssao = action.ssao.ToLower().Trim();
			if (ssao == "on") {
				m_Camera.GetComponent<ScreenSpaceAmbientOcclusion>().enabled = true;
			} else if (ssao == "off") {
				m_Camera.GetComponent<ScreenSpaceAmbientOcclusion>().enabled = false;
			} else if (ssao == "default") {
				// Do nothing
			} else {
				throw new NotImplementedException("ssao must be one of 'on', 'off' or 'default'.");
			}	
            	

            // Debug.Log("Object " + action.controllerInitialization.ToString() + " dict "  + (action.controllerInitialization.variableInitializations == null));//+ string.Join(";", action.controllerInitialization.variableInitializations.Select(x => x.Key + "=" + x.Value).ToArray()));

            if (action.controllerInitialization != null && action.controllerInitialization.variableInitializations != null) {
                foreach (KeyValuePair<string, TypedVariable> entry in action.controllerInitialization.variableInitializations) {
                    Debug.Log(" Key " + entry.Value.type + " field " + entry.Key);
                    Type t = Type.GetType(entry.Value.type);
                    FieldInfo field = t.GetField(entry.Key, BindingFlags.Public | BindingFlags.Instance);
                    field.SetValue(this, entry.Value);
                }
                InitializeController(action);
            }
        }

        public bool SetHeadingAngles(float rotateStepDegrees) {
            var epsilon = 1e-4;
            var epsilonBig = 1e-3;
            if (Mathf.Abs(rotateStepDegrees - 90.0f) > epsilonBig) {
                var ratio = 360.0f / rotateStepDegrees;
                var angleStepNumber = Mathf.RoundToInt(ratio);
                
                if (Mathf.Abs(ratio - angleStepNumber) > epsilon) {
                    errorMessage = "Invalid argument 'rotateStepDegrees': 360 should be divisible by 'rotateStepDegrees'.";
                    Debug.Log(errorMessage);
                    return false;
                }
                else {
                    // Debug.Log("Setting heading angles with " + rotateStepDegrees);
                    this.headingAngles = new float[angleStepNumber];
                    for (int i = 0; i < angleStepNumber; i++) {
                        headingAngles[i] = i * rotateStepDegrees;
                    }

                    this.angleStepDegrees = rotateStepDegrees;

                    // Debug.Log("Total "  + string.Join(",", headingAngles.Select(x => x.ToString()).ToArray()));
                    return true;
                }
            }
            return true;
        }

        public virtual void InitializeController(ServerAction action) {
            
        }

        public IEnumerator checkInitializeAgentLocationAction()
        {
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

            foreach (float x in xs)
            {
                foreach (float z in zs)
                {
                    this.transform.position = startingPosition;
                    yield return null;

                    Vector3 target = new Vector3(x, this.transform.position.y, z);
                    Vector3 dir = target - this.transform.position;
                    Vector3 movement = dir.normalized * 100.0f;
                    if (movement.magnitude > dir.magnitude)
                    {
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
            if (validMovements.Count > 0)
            {
                Debug.Log("Initialize: got total valid initial targets: " + validMovements.Count);
                Vector3 firstMove = validMovements[0];
                firstMove.y = Physics.gravity.y * this.m_GravityMultiplier;

                m_CharacterController.Move(firstMove);
                snapToGrid();
                actionFinished(true);
            }

            else
            {
                Debug.Log("Initialize: no valid starting positions found");
                actionFinished(false);
            }
        }

		public bool excludeObject(string uniqueId)
		{
			return Array.IndexOf(this.excludeObjectIds, uniqueId) >= 0;
		}

		public bool excludeObject(SimpleSimObj so)
		{
			return excludeObject(so.UniqueID);
		}

		protected bool closeSimObj(SimpleSimObj so)
		{
			return so.Close();
		}

		protected bool openSimObj(SimpleSimObj so)
		{
			return so.Open();
		}


		// rotate view with respect to mouse or server controls - I'm not sure when this is actually used
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

        public virtual SimpleSimObj[] allSceneObjects() {
			return GameObject.FindObjectsOfType<SimObj>();
        }

        public abstract ObjectMetadata[] generateObjectMetadata();

		public virtual MetadataWrapper generateMetadataWrapper()
		{
			ObjectMetadata agentMeta = new ObjectMetadata();
			agentMeta.name = "agent";
			agentMeta.position = transform.position;
			agentMeta.rotation = transform.eulerAngles;
			agentMeta.cameraHorizon = m_Camera.transform.rotation.eulerAngles.x;

			if (agentMeta.cameraHorizon > 180) {
				agentMeta.cameraHorizon -= 360;
			}

			MetadataWrapper metaMessage = new MetadataWrapper();

			metaMessage.agent = agentMeta;
			metaMessage.sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
			metaMessage.objects = generateObjectMetadata();
			metaMessage.collided = collidedObjects.Length > 0;
			metaMessage.collidedObjects = collidedObjects;
			metaMessage.screenWidth = Screen.width;
			metaMessage.screenHeight = Screen.height;
			metaMessage.lastAction = lastAction;
			metaMessage.lastActionSuccess = lastActionSuccess;
			metaMessage.errorMessage = errorMessage;

			if (errorCode != ServerActionErrorCode.Undefined) 
			{
				metaMessage.errorCode = Enum.GetName(typeof(ServerActionErrorCode), errorCode);
			}


			return metaMessage;
		}

		public virtual SimpleSimObj[] VisibleSimObjs() {
			return new SimObj[]{} as SimpleSimObj[];
		}

		private void enableImageSynthesis() {
			imageSynthesis = this.gameObject.GetComponentInChildren<ImageSynthesis> () as ImageSynthesis;
			imageSynthesis.enabled = true;			
		}


		public void ProcessControlCommand(ServerAction controlCommand)
		{
            currentServerAction = controlCommand;
			
	        errorMessage = "";
			errorCode = ServerActionErrorCode.Undefined;
			collisionsInAction = new List<string>();

			lastAction = controlCommand.action;
			lastActionSuccess = false;
			lastPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z);
            // this is always a reference to PhysicsFPSAgentController even when trying to use the Stochastic, is null if destroyed in initialize see AgentManager Initialize, Fix This
			System.Reflection.MethodInfo method = this.GetType().GetMethod(controlCommand.action);
			
            // TODO Remove hack, for some reason  heading angles are reset after initialize to default values for SochasticAgent, it must be being destroyed and constructed again
            var agentManagerStepDegrees = GameObject.FindObjectOfType<AgentManager>().rotateStepDegrees;
            if (Mathf.Abs(agentManagerStepDegrees - this.angleStepDegrees) > 1e-5) {
                 Debug.Log("Setting angle step to " + agentManagerStepDegrees);
                this.SetHeadingAngles(agentManagerStepDegrees);
            }
			this.actionComplete = false;
			try
			{
				if (method == null) {
					errorMessage = "Invalid action: " + controlCommand.action;
					errorCode = ServerActionErrorCode.InvalidAction;
					Debug.LogError(errorMessage);
					actionFinished(false);
				} else {
					method.Invoke(this, new object[] { controlCommand });
				}
			}
			catch (Exception e)
			{
				Debug.LogError("caught error with invoke");
				Debug.LogError(e);

				errorMessage += e.ToString();
				actionFinished(false);
			}

#if UNITY_EDITOR
			if (errorMessage != "") {
				Debug.Log(errorMessage);
			}
#endif

			agentManager.setReadyToEmit(true);
		}

		// Handle collisions - CharacterControllers don't apply physics innately, see "PushMode" check below
		protected void OnControllerColliderHit(ControllerColliderHit hit)
		{
			if (!enabled)
			{
				return;
			}

			if (hit.gameObject.GetComponent<StructureObject>())
			{
                if(hit.gameObject.GetComponent<StructureObject>().WhatIsMyStructureObjectTag == StructureObjectTag.Floor)
				return;
			}


			if (!collisionsInAction.Contains(hit.gameObject.name))
			{

                //XXX - Yeh so we will need to search up the gameobject's parentint Heirarchy for a SimObj or SimObjPhysics component, 
				//otherwise
                //compound colliders will report back nonsense names
				Debug.Log("Agent Collided with " + hit.gameObject.name);
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

			//push objects out of the way if moving through them and they are Moveable or CanPickup (Physics)
			if (PushMode)
			{
				float pushPower = 2.0f;
				Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
				body.velocity = pushDir * pushPower;
			}
			//if we touched something with a rigidbody that needs to simulate physics, generate a force at the impact point
			//body.AddForce(m_CharacterController.velocity * 15f, ForceMode.Force);
			//body.AddForceAtPosition (m_CharacterController.velocity * 15f, hit.point, ForceMode.Acceleration);//might have to adjust the force vector scalar later
		}

		protected void snapToGrid()
		{
			float mult = 1 / gridSize;
			float gridX = Convert.ToSingle(Math.Round(this.transform.position.x * mult) / mult);
			float gridZ = Convert.ToSingle(Math.Round(this.transform.position.z * mult) / mult);

			this.transform.position = new Vector3(gridX, transform.position.y, gridZ);
		}

		//move in cardinal directions
		virtual protected void moveCharacter(ServerAction action, int targetOrientation)
		{
            // TODO: Simplify this???
			//resetHand(); when I looked at this resetHand in DiscreteRemoteFPSAgent was just commented out doing nothing so...
			moveMagnitude = gridSize;
			if (action.moveMagnitude > 0)
			{
				moveMagnitude = action.moveMagnitude;
			}
			int currentRotation = (int)Math.Round(transform.rotation.eulerAngles.y, 0);
			Dictionary<int, Vector3> actionOrientation = new Dictionary<int, Vector3>();
			actionOrientation.Add(0, new Vector3(0f, 0f, 1.0f));
			actionOrientation.Add(90, new Vector3(1.0f, 0.0f, 0.0f));
			actionOrientation.Add(180, new Vector3(0f, 0f, -1.0f));
			actionOrientation.Add(270, new Vector3(-1.0f, 0.0f, 0.0f));
			int delta = (currentRotation + targetOrientation) % 360;

			Vector3 m;
			if (actionOrientation.ContainsKey(delta))
			{
				m = actionOrientation[delta];

			}

			else
			{
				actionOrientation = new Dictionary<int, Vector3>();
				actionOrientation.Add(0, transform.forward);
				actionOrientation.Add(90, transform.right);
				actionOrientation.Add(180, transform.forward * -1);
				actionOrientation.Add(270, transform.right * -1);
				m = actionOrientation[targetOrientation];
			}

			m *= moveMagnitude;

			m.y = Physics.gravity.y * this.m_GravityMultiplier;
			m_CharacterController.Move(m);
			actionFinished(true);
			// StartCoroutine(checkMoveAction(action));
		}

        //this is not currently used by the Physics agent. Physics agent now checks the final movement position first before moving, so collision checks after trying to move are no longer needed
		virtual protected IEnumerator checkMoveAction(ServerAction action)
		{

			yield return null;

    
			if (continuousMode)
			{
				actionFinished(true);
				yield break;
			}

			bool result = false;

			errorMessage = "Agent did not settle during move.";
			for (int i = 0; i < actionDuration; i++)
			{
				Vector3 currentPosition = this.transform.position;
				Vector3 zeroY = new Vector3(1.0f, 0.0f, 1.0f);
				float distance = Vector3.Distance(Vector3.Scale(lastPosition, zeroY), Vector3.Scale(currentPosition, zeroY));
				if (Math.Abs(moveMagnitude - distance) < 0.005)
				{
					currentPosition = this.transform.position;

					if (action.snapToGrid)
					{
						//print("jere");
						this.snapToGrid();
					}


					yield return null;
					if (this.IsCollided())
					{
						for (int j = 0; j < actionDuration; j++)
						{
							yield return null;
						}

					}

					if ((currentPosition - this.transform.position).magnitude <= 0.001f)
					{
						result = true;
						//lastPosition = transform.position;//debugging
					}

					break;
				}

				else
				{
				//	print("here?");

					yield return null;
				}
			}

			// Debug.Log(this.transform.position.z.ToString("F3", CultureInfo.InvariantCulture));

			// if for some reason we moved in the Y space too much, then we assume that something is wrong
			// In FloorPlan 223 @ x=-1, z=2.0 its possible to move through the wall using move=0.5

			if (Math.Abs((this.transform.position - lastPosition).y) > 0.2)
			{
				errorMessage = "Move resulted in too large a change in y coordinate.";
				result = false;
			}


			if (!result)
			{
				Debug.Log(errorMessage);
				transform.position = lastPosition;
			} else {
				errorMessage = "";
			}

			actionFinished(result);
		}




		public virtual void MoveLeft(ServerAction action)
		{
			moveCharacter(action, 270);
		}

		public virtual void MoveRight(ServerAction action)
		{
			moveCharacter(action, 90);
		}

		public virtual void MoveAhead(ServerAction action)
		{
			moveCharacter(action, 0);
		}

		public virtual void MoveBack(ServerAction action)
		{
			moveCharacter(action, 180);
		}

        public virtual void MoveRelative(ServerAction action) {
            var moveLocal = new Vector3(action.x, 0, action.z);
            Vector3 moveWorldSpace = transform.rotation * moveLocal;
            moveWorldSpace.y = Physics.gravity.y * this.m_GravityMultiplier;
			m_CharacterController.Move(moveWorldSpace);
			actionFinished(true);
        }

		//free move
		public virtual void Move(ServerAction action)
		{
			//resetHand(); again, reset hand was commented out so was doing nothing
			if (Math.Abs(action.x) > 0)
			{
				moveMagnitude = Math.Abs(action.x);
			}

			else
			{
				moveMagnitude = Math.Abs(action.z);
			}

			action.y = Physics.gravity.y * this.m_GravityMultiplier;
			m_CharacterController.Move(new Vector3(action.x, action.y, action.z));
			StartCoroutine(checkMoveAction(action));
		}


		private int nearestAngleIndex(float angle, float[] array)
		{

			for (int i = 0; i < array.Length; i++)
			{
				if (Math.Abs(angle - array[i]) < 2.0f)
				{
					return i;
				}
			}
			return 0;
		}

		protected int currentHorizonAngleIndex()
		{
			return nearestAngleIndex(Quaternion.LookRotation(m_Camera.transform.forward).eulerAngles.x, horizonAngles);
		}

		private int currentHeadingAngleIndex()
		{
			return nearestAngleIndex(Quaternion.LookRotation(transform.forward).eulerAngles.y, headingAngles);
		}

		//free look, change up/down angle of camera view
		public void Look(ServerAction response)
		{
			m_Camera.transform.localEulerAngles = new Vector3(response.horizon, 0.0f, 0.0f);
			actionFinished(true);
		}

		//free rotate, change forward facing of Agent
		public virtual void Rotate(ServerAction response)
		{
			transform.rotation = Quaternion.Euler(new Vector3(0.0f, response.rotation.y, 0.0f));
			actionFinished(true);
		}

		//looks like thisfree rotates AND free changes camera look angle?
		public void RotateLook(ServerAction response)
		{
			transform.rotation = Quaternion.Euler(new Vector3(0.0f, response.rotation.y, 0.0f));
			m_Camera.transform.localEulerAngles = new Vector3(response.horizon, 0.0f, 0.0f);
			actionFinished(true);

		}

        public virtual Quaternion GetRotateQuaternion(int headIndex)
		{
			int index = (headingAngles.Length + (currentHeadingAngleIndex() + headIndex)) % headingAngles.Length;
			float targetRotation = headingAngles[index];
            // Debug.Log("Target rot " + targetRotation + " from " + string.Join(",", headingAngles.Select(x => x.ToString()).ToArray()) + " Step " + angleStepDegrees);
			return Quaternion.Euler(new Vector3(0.0f, targetRotation, 0.0f));
		}

		//rotates 90 degrees left w/ respect to current forward
		public virtual void RotateLeft(ServerAction controlCommand)
		{
			transform.rotation = GetRotateQuaternion(-1);
			actionFinished(true);
		}

		//rotates 90 degrees right w/ respect to current forward
		public virtual void RotateRight(ServerAction controlCommand)
		{
			transform.rotation = transform.rotation = GetRotateQuaternion(1);
			actionFinished(true);
		}

		//iterates to next allowed downward horizon angle for AgentCamera (max 60 degrees down)
		public virtual void LookDown(ServerAction response)
		{
			if (currentHorizonAngleIndex() > 0)
			{
				float targetHorizon = horizonAngles[currentHorizonAngleIndex() - 1];
				m_Camera.transform.localEulerAngles = new Vector3(targetHorizon, 0.0f, 0.0f);
				actionFinished(true);

			}
			else
			{
				errorMessage = "can't LookDown below the min horizon angle";
				Debug.Log(errorMessage);
				errorCode = ServerActionErrorCode.LookDownCantExceedMin;
				actionFinished(false);
			}
		}

		//iterates to next allowed upward horizon angle for agent camera (max 30 degrees up)
		public virtual void LookUp(ServerAction controlCommand)
		{

			if (currentHorizonAngleIndex() < horizonAngles.Length - 1)
			{
				float targetHorizon = horizonAngles[currentHorizonAngleIndex() + 1];
				m_Camera.transform.localEulerAngles = new Vector3(targetHorizon, 0.0f, 0.0f);
				actionFinished(true);
			}

			else
			{
				errorMessage = "can't LookUp beyond the max horizon angle";
				Debug.Log(errorMessage);
				errorCode = ServerActionErrorCode.LookUpCantExceedMax;
				actionFinished(false);
			}
		}

		//public virtual void P(ServerAction action)//use
		//{

		//}
	}
}
