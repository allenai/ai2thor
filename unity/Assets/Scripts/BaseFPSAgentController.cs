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

		protected static float gridSize = 0.25f;
		protected float moveMagnitude;

		protected bool continuousMode;

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

		protected SimObjType[] OpenableTypes = new SimObjType[] { SimObjType.Fridge, SimObjType.Cabinet, SimObjType.Microwave, SimObjType.LightSwitch, SimObjType.Blinds, SimObjType.Book, SimObjType.Toilet };
		protected SimObjType[] ImmobileTypes = new SimObjType[] { SimObjType.Chair, SimObjType.Toaster, SimObjType.CoffeeMachine, SimObjType.Television, SimObjType.StoveKnob };

		protected float[] headingAngles = new float[] { 0.0f, 90.0f, 180.0f, 270.0f };
		protected float[] horizonAngles = new float[] { 60.0f, 30.0f, 0.0f, 330.0f };

        //allow agent to push sim objects that can move, for physics
		protected bool PushMode = false;

		protected Dictionary<SimObjType, Dictionary<string, int>> OPEN_CLOSE_STATES = new Dictionary<SimObjType, Dictionary<string, int>>{
			{SimObjType.Microwave, new Dictionary<string, int>{{"open", 2}, {"close", 1}}},
			{SimObjType.Laptop, new Dictionary<string, int>{{"open", 2}, {"close", 1}}},
			{SimObjType.Book, new Dictionary<string, int>{{"open", 1}, {"close", 2}}},
			{SimObjType.Toilet, new Dictionary<string, int>{{"open", 2}, {"close", 3}}},
			{SimObjType.Sink, new Dictionary<string, int>{{"open", 2}, {"close", 1}}}
		};



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


		// Vector3 m_OriginalCameraPosition;


		protected float maxVisibleDistance = 1.0f;

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

		public Quaternion TargetRotation
		{
			get { return targetRotation; }
		}

		// Initialize parameters from environment variables
		protected virtual void Awake()
		{         
			// whether it's in training or test phase         
			// character controller parameters
			m_CharacterController = GetComponent<CharacterController>();
			//float radius = m_CharacterController.radius;
			m_CharacterController.radius = 0.2f;
			// using default for now to remain consistent with generated points
			//m_CharacterController.height = LoadFloatVariable (height, "AGENT_HEIGHT");
			this.m_WalkSpeed = 2;
			this.m_RunSpeed = 10;
			this.m_GravityMultiplier = 2;
			//this.m_UseFovKick = true;
			//this.m_StepInterval = 5;
		}


		// Use this for initialization
		protected virtual void Start()
		{
			m_Camera = this.gameObject.GetComponentInChildren<Camera> ();
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

			//allowNodes = false;
		}



		protected virtual void actionFinished(bool success) { }


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

		public virtual MetadataWrapper generateMetadataWrapper()
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

		public void ProcessControlCommand(ServerAction controlCommand)
		{
			errorMessage = "";
			errorCode = ServerActionErrorCode.Undefined;
			collisionsInAction = new List<string>();

			lastAction = controlCommand.action;
			lastActionSuccess = false;
			lastPosition = new Vector3 (transform.position.x, transform.position.y, transform.position.z);
			System.Reflection.MethodInfo method = this.GetType ().GetMethod (controlCommand.action);
			this.actionComplete = false;
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
              
		// Handle collisions - CharacterControllers don't apply physics innately, see "PushMode" check below
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


			if (!collisionsInAction.Contains (hit.gameObject.name)) 
			{
				Debug.Log ("Agent Collided with " + hit.gameObject.name);
				collisionsInAction.Add (hit.gameObject.name);
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
            if(PushMode)
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
            StartCoroutine(checkMoveAction(action));

        }

		virtual protected IEnumerator checkMoveAction(ServerAction action)
        {

            yield return null;

            if (continuousMode)
            {
                actionFinished(true);
                yield break;
            }

            bool result = false;         

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
                    }

                    break;
                }

                else
                {
                    yield return null;
                }
            }
         
            // Debug.Log(this.transform.position.z.ToString("F3", CultureInfo.InvariantCulture));

            // if for some reason we moved in the Y space too much, then we assume that something is wrong
            // In FloorPlan 223 @ x=-1, z=2.0 its possible to move through the wall using move=0.5

            if (Math.Abs((this.transform.position - lastPosition).y) > 0.2)
            {
                result = false;
            }


            if (!result)
            {
                Debug.Log("check move failed");
                transform.position = lastPosition;
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

        private int currentHorizonAngleIndex()
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
        public void Rotate(ServerAction response)
        {
            transform.rotation = Quaternion.Euler(new Vector3(0.0f, response.rotation, 0.0f));
            actionFinished(true);
        }

		//looks like thisfree rotates AND free changes camera look angle?
        public void RotateLook(ServerAction response)
        {
            transform.rotation = Quaternion.Euler(new Vector3(0.0f, response.rotation, 0.0f));
            m_Camera.transform.localEulerAngles = new Vector3(response.horizon, 0.0f, 0.0f);
            actionFinished(true);

        }

        //rotates 90 degrees left w/ respect to current forward
		public void RotateLeft(ServerAction controlCommand)
        {
            int index = currentHeadingAngleIndex() - 1;
            if (index < 0)
            {
                index = headingAngles.Length - 1;
            }
            float targetRotation = headingAngles[index];
            transform.rotation = Quaternion.Euler(new Vector3(0.0f, targetRotation, 0.0f));
            actionFinished(true);

        }
      
        //rotates 90 degrees right w/ respect to current forward
        public void RotateRight(ServerAction controlCommand)
        {

            int index = currentHeadingAngleIndex() + 1;
            if (index == headingAngles.Length)
            {
                index = 0;
            }

            float targetRotation = headingAngles[index];
            transform.rotation = Quaternion.Euler(new Vector3(0.0f, targetRotation, 0.0f));
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
                errorCode = ServerActionErrorCode.LookUpCantExceedMax;
                actionFinished(false);
            }
        }
	}
}
