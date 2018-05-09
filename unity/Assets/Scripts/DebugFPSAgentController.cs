// Copyright Allen Institute for Artificial Intelligence 2017

//Check Assets/Prefabs/DebugController for ReadMe on how to use this Debug Controller
using UnityEngine;

using Random = UnityEngine.Random;

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UnityStandardAssets.Characters.FirstPerson
{
	[RequireComponent(typeof (CharacterController))]
    public class DebugFPSAgentController : MonoBehaviour
	{
		public float MaxDistance = 1f;

		[SerializeField] private bool m_IsWalking;
		[SerializeField] private float m_WalkSpeed;
		[SerializeField] private float m_RunSpeed;
		[SerializeField] [Range(0f, 1f)] private float m_RunstepLenghten;
		[SerializeField] private float m_JumpSpeed;
		[SerializeField] private float m_StickToGroundForce;
		[SerializeField] private float m_GravityMultiplier;
		[SerializeField] private MouseLook m_MouseLook;
		[SerializeField] private bool m_UseFovKick;
		[SerializeField] private FOVKick m_FovKick = new FOVKick();
		[SerializeField] private bool m_UseHeadBob;
		[SerializeField] private CurveControlledBob m_HeadBob = new CurveControlledBob();
		[SerializeField] private LerpControlledBob m_JumpBob = new LerpControlledBob();
		[SerializeField] private float m_StepInterval;
		[SerializeField] private AudioClip[] m_FootstepSounds;    // an array of footstep sounds that will be randomly selected from.
		[SerializeField] private AudioClip m_JumpSound;           // the sound played when character leaves the ground.
		[SerializeField] private AudioClip m_LandSound;           // the sound played when character touches back on ground.
	
        [SerializeField] private GameObject Target_Text = null;
        [SerializeField] private GameObject Debug_Canvas = null;
       // [SerializeField] private bool isReceptacle = false;
      //  [SerializeField] private bool isPickup = false;

       // [SerializeField] private string current_Object_In_Inventory = null;
      //  [SerializeField] private GameObject Inventory_Text = null;

        [SerializeField] private GameObject AgentHand = null;
        [SerializeField] private GameObject ItemInHand = null;

		private Camera m_Camera;
		private bool m_Jump;
		private float m_YRotation;
		public bool rotateMouseLook;
		private Vector2 m_Input;
		private Vector3 m_MoveDir = Vector3.zero;
		private CharacterController m_CharacterController;
		//private CollisionFlags m_CollisionFlags;
		private bool m_PreviouslyGrounded;
		private Vector3 m_OriginalCameraPosition;
		private float m_StepCycle;
		private float m_NextStep;
		private bool m_Jumping;

        public bool looking = false;
		//private AudioSource m_AudioSource;

		public SimObjPhysics[] VisibleObjects; //these objects are within the camera viewport and in range of the agent

        private void Start()
        {
            m_CharacterController = GetComponent<CharacterController>();
            m_Camera = Camera.main;
            m_OriginalCameraPosition = m_Camera.transform.localPosition;
            m_FovKick.Setup(m_Camera);
            m_HeadBob.Setup(m_Camera, m_StepInterval);
            m_StepCycle = 0f;
            m_NextStep = m_StepCycle / 2f;
            m_Jumping = false;
            //m_AudioSource = GetComponent<AudioSource>();
            m_MouseLook.Init(transform, m_Camera.transform);

            //grab text object on canvas to update with what is currently targeted by reticle
            Target_Text = GameObject.Find("DebugCanvas/TargetText"); ;
            Debug_Canvas = GameObject.Find("DebugCanvas");
            //Inventory_Text = GameObject.Find("DebugCanvas/InventoryText");

            //if this component is enabled, turn on the targeting reticle and target text
            if (this.isActiveAndEnabled)
            {
                Debug_Canvas.SetActive(true);
                Target_Text.SetActive(true);

                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        public SimObjPhysics[] GetAllVisibleSimObjPhysics(Camera agentCamera, float maxDistance)
        {
            List<SimObjPhysics> currentlyVisibleItems = new List<SimObjPhysics>();

            Vector3 agentCameraPos = agentCamera.transform.position;

            //sphere from the camera center with radius max distance * downward range extension
            //check what objects are in range by looking for their colliders

            //testcolliders_in_view = Physics.OverlapSphere(agentCameraPos, maxDistance,
                                                    //1 << 8, QueryTriggerInteraction.Collide);

            //get all sim objects in range around us
            //the range is the maxDistance + offset to make sure sim objects that leave the sphere have time to properly update physics
            //if the OverlapSphere is exactly at the max Distance, the physics update can be missed
            Collider[] colliders_in_view = Physics.OverlapSphere(agentCameraPos, maxDistance + 0.5f, 
                                                         1 << 8 , QueryTriggerInteraction.Collide); //layermask is 8

            if(colliders_in_view != null)
            {
                foreach (Collider item in colliders_in_view)
                {
                    //get the SimObjPhysics component from the collider that was found
                    SimObjPhysics sop = item.GetComponent<SimObjPhysics>();

                    //is the sim object in range, check if it is bounds of the camera's viewport
                    if (CheckIfInViewport(sop, agentCamera, maxDistance - 0.01f))
                    {
                        sop.isVisible = true;
                        currentlyVisibleItems.Add(sop);

                        //draw a debug line to the object's transform
                        #if UNITY_EDITOR
                        Debug.DrawLine(agentCameraPos, sop.transform.position, Color.yellow);
                        #endif
                    }

                    else
                    {
                        //print("out of range");
                        sop.isVisible = false;
                        currentlyVisibleItems.Remove(sop);
                    }
                }

                //now that we have a list of currently visible items, let's see which ones are interactable!
                Rigidbody HandRB = AgentHand.GetComponent<Rigidbody>();
                //RaycastHit hit = new RaycastHit();

                foreach (SimObjPhysics visibleSimObjP in currentlyVisibleItems)
                {

                    //get all interaction points on the visible sim object we are checking here
                    Transform[] InteractionPoints = visibleSimObjP.InteractionPoints;

                    int ReachableInteractionPointCount = 0;
                    foreach (Transform ip in InteractionPoints)
                    {
                        //sweep test from agent's hand to each Interaction point
                        RaycastHit hit;
                        if(HandRB.SweepTest(ip.position - AgentHand.transform.position, out hit, maxDistance))
                        {
                            //if the object only has one interaction point to check
                            if(visibleSimObjP.InteractionPoints.Length == 1)
                            {
                                if (hit.transform == visibleSimObjP.transform)
                                {
                                    #if UNITY_EDITOR
                                    Debug.DrawLine(AgentHand.transform.position, ip.transform.position, Color.magenta);
                                    #endif

                                    visibleSimObjP.isInteractable = true;
                                }

                                else
                                    visibleSimObjP.isInteractable = false;
                            }

                            //this object has 2 or more interaction points
                            //if any one of them can be accessed by the Agent's hand, this object is interactable
                            if(visibleSimObjP.InteractionPoints.Length > 1)
                            {
                                
                                if(hit.transform == visibleSimObjP.transform)
                                {
                                    #if UNITY_EDITOR
                                    Debug.DrawLine(AgentHand.transform.position, ip.transform.position, Color.magenta);
                                    #endif
                                    ReachableInteractionPointCount++;
                                }

                                //check if at least one of the interaction points on this multi interaction point object
                                //is accessible to the agent Hand
                                if (ReachableInteractionPointCount > 0)
                                {
                                    visibleSimObjP.isInteractable = true;
                                }

                                else
                                    visibleSimObjP.isInteractable = false;
                            }
                        }

                    }

                }
            }

            //populate array of visible items in order by distance
            currentlyVisibleItems.Sort((x, y) => Vector3.Distance(x.transform.position, agentCameraPos).CompareTo(Vector3.Distance(y.transform.position, agentCameraPos)));
            return currentlyVisibleItems.ToArray();
        }

        //see if a given SimObjPhysics is within the camera's range and field of view
        static bool CheckIfInViewport(SimObjPhysics item, Camera agentCamera, float maxDistance)
        {
            SimObjManipTypePhysics[] itemManipType = item.GetComponent<SimObjPhysics>().ManipTypes;

            bool DoWeCareABoutThisObjectsVisibility = false;

            foreach (SimObjManipTypePhysics type in itemManipType)
            {
                if (type != SimObjManipTypePhysics.Static && type != SimObjManipTypePhysics.Moveable)
                {
                    DoWeCareABoutThisObjectsVisibility = true;
                }
            }

            if (DoWeCareABoutThisObjectsVisibility == true)
            {
                Vector3 viewPoint = agentCamera.WorldToViewportPoint(item.transform.position);

                //move these two up to variables later
                float ViewPointRangeHigh = 1.0f;
                float ViewPointRangeLow = 0.0f;

                if (viewPoint.z > 0 && viewPoint.z < maxDistance //is in front of camera and within range of visibility sphere
                   && viewPoint.x < ViewPointRangeHigh && viewPoint.x > ViewPointRangeLow//within x bounds
                    && viewPoint.y < ViewPointRangeHigh && viewPoint.y > ViewPointRangeLow)//within y bounds
                {
                    return true;
                }

                else
                    return false;
            }
            else return false;
        }
      
		#if UNITY_EDITOR
		//used to show what's currently visible on the top left of the screen
		void OnGUI () 
        {
			if (VisibleObjects != null) 
            {
				if (VisibleObjects.Length > 10) 
                {
					int horzIndex = -1;
					GUILayout.BeginHorizontal ();
					foreach (SimObjPhysics o in VisibleObjects) 
                    {
						horzIndex++;
						if (horzIndex >= 3) 
                        {
							GUILayout.EndHorizontal ();
							GUILayout.BeginHorizontal ();
							horzIndex = 0;
						}
						GUILayout.Button (o.UniqueID, UnityEditor.EditorStyles.miniButton, GUILayout.MaxWidth (200f));
					}

					GUILayout.EndHorizontal ();
				}

                else 
                {
					Plane[] planes = GeometryUtility.CalculateFrustumPlanes(m_Camera);

                    //int position_number = 0;
					foreach (SimObjPhysics o in VisibleObjects) 
                    {
						string suffix = "";
						Bounds bounds = new Bounds (o.gameObject.transform.position, new Vector3 (0.05f, 0.05f, 0.05f));
						if (GeometryUtility.TestPlanesAABB (planes, bounds)) 
                        {
                            //position_number += 1;

                            //if (o.GetComponent<SimObj>().Manipulation == SimObjManipType.Inventory)
                            //    suffix += " VISIBLE: " + "Press '" + position_number + "' to pick up";

                            //else
                                //suffix += " VISIBLE";
                            if(o.isInteractable == true)
                            {
                                suffix += " INTERACTABLE";
                            }
						}
							
							
						GUILayout.Button (o.UniqueID + suffix, UnityEditor.EditorStyles.miniButton, GUILayout.MinWidth (100f));
					}
				}
			}
		}
		#endif

        public void MoveHand(Vector3 targetPosition)
        {
            //two versions, one for if an object is in the hand, one for if there is not an object in the hand. 

         
        }

        //pickup a sim object
        public void PickUpSimObjPhysics(Transform target)
        {
            //turn off the hand's collision and physics properties
            //make the object kinematic
            if (ItemInHand == null)
            {
                //move the object to the hand's default position.
                target.GetComponent<Rigidbody>().isKinematic = true;
                //target.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;
                target.position = AgentHand.transform.position;
                //AgentHand.transform.parent = target;
                target.parent = AgentHand.transform;
                //update "inventory"
                ItemInHand = target.gameObject;
            }

            else
                Debug.Log("Your hand has something in it already!");

        }

        public void RotateSimObjPhysicsInHand(Vector3 vec)
        {
            if(ItemInHand != null)
            {
                

                //get rigidbody of object in hand
                Rigidbody objInHandRB = ItemInHand.GetComponent<Rigidbody>();
                //OverlapSphere to see if there is enough room to completely rotate this object.
                //center hand
                //radius - length of the rigidbody /2? get the rigidbody's longest point?
                //layermask to ignore colliders we don't care about... but i think we care about all of them so yes

                //for items that use box colliders
                if(ItemInHand.GetComponent<BoxCollider>())
                {
                    Vector3 sizeOfBox = ItemInHand.GetComponent<BoxCollider>().size;
                    //do an overlapshere around the agent with radius based on max size of xyz of object in hand's collider
                    Collider[] hitColliders = Physics.OverlapSphere(AgentHand.transform.position, 
                                                                    Math.Max(Math.Max(sizeOfBox.x, sizeOfBox.y), sizeOfBox.z) / 2);

                    foreach(Collider col in hitColliders)
                    {
                        //if anything other than the agent, the agent's hand, or the object currently being held is touched, no room to rotate
                        if(col.name != "FPSController" && col.name != "TheHand" && col.name != ItemInHand.name)
                        {
                            print(col.name);
                            print("Not Enough Room to Rotate");
                            return;
                        }
                    }
                }

                if (ItemInHand.GetComponent<SphereCollider>())
                {
                    float radiusOfSphere = ItemInHand.GetComponent<SphereCollider>().radius;
                    Collider[] hitColliders = Physics.OverlapSphere(AgentHand.transform.position, radiusOfSphere);

                    foreach (Collider col in hitColliders)
                    {
                        //print(col.name);
                        if (col.name != "FPSController" && col.name != "TheHand" && col.name != ItemInHand.name)
                        {
                            print("Not Enough Room to Rotate");
                            return;
                        }
                    }
                }

                //rotate agent hand's local rotation. This way it is consistant regardless of the default rotation of an object
                //picked up. Some objects (because of the model imported at weird rotations) must default to weird rotations
                AgentHand.transform.localRotation = Quaternion.Euler(vec);

            }

            else
            {
                Debug.Log("Nothing In Hand to rotate!");
                return;
            }

        }

		private void Update()	
        {
            VisibleObjects = GetAllVisibleSimObjPhysics(m_Camera, 2.0f);

            //////MOUSE AND KEYBAORD INPUT///////////////////////////////////////////////////
            if(Input.GetKey(KeyCode.Space))
            {
                //turn on cursor targeting mode
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                looking = true;
            }

            if (Input.GetKeyUp(KeyCode.Space))
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                looking = false;
            }

            //on left mouse click
            if(Input.GetMouseButtonDown(0))
            {
                //check if we are in cursor targeting mode
                if(Cursor.visible == true)
                {
                    RaycastHit hit;
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    //shoot a ray out to select an object
                    if(Physics.Raycast(ray, out hit))
                    {
                        //check if the hit object is a SimObj in our array of Accessible sim objects
                        if(hit.transform.tag == "SimObjPhysics")
                        {
                            //if an interaction point is accessible by the hand, proceed to try and pick it up
                            if(hit.transform.GetComponent<SimObjPhysics>().isInteractable == true)
                            {
                                //print(hit.transform.name + " is pickupable!");

                                //pickup the object here
                                PickUpSimObjPhysics(hit.transform);

                            }
                        }
                    }
                }
            }

            //on right mouse click
            if(Input.GetMouseButtonDown(1))
            {
                //RotateObjectInHand(new Vector3(90, 0, 0));
            }

            if(Input.GetKeyDown(KeyCode.UpArrow))
            {
                RotateSimObjPhysicsInHand(new Vector3(0, 0, 0));
            }

            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                RotateSimObjPhysicsInHand(new Vector3(180, 0, 0));
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                RotateSimObjPhysicsInHand(new Vector3(90, 0, 0));
            }

            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                RotateSimObjPhysicsInHand(new Vector3(-90, 0, 0));
            }
            ///////////////////////////////////////////////////////////////////////////
			if(looking == false)
            RotateView();

			// the jump state needs to read here to make sure it is not missed
			if (!m_Jump)
			{
				//m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");
			}

			if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
			{
				StartCoroutine(m_JumpBob.DoBobCycle());
				m_MoveDir.y = 0f;
				m_Jumping = false;
			}
			if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded)
			{
				m_MoveDir.y = 0f;
			}

			m_PreviouslyGrounded = m_CharacterController.isGrounded;

            float speed;
            GetInput(out speed);
            // always move along the camera forward as it is the direction that it being aimed at
            Vector3 desiredMove = transform.forward * m_Input.y + transform.right * m_Input.x;

            // get a normal for the surface that is being touched to move along it
            RaycastHit hitInfo;
            Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
                m_CharacterController.height / 2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

            m_MoveDir.x = desiredMove.x * speed;
            m_MoveDir.z = desiredMove.z * speed;


            if (m_CharacterController.isGrounded)
            {
                m_MoveDir.y = -m_StickToGroundForce;

                if (m_Jump)
                {
                    m_MoveDir.y = m_JumpSpeed;
                    //PlayJumpSound();
                    m_Jump = false;
                    m_Jumping = true;
                }
            }

            else
            {
                m_MoveDir += Physics.gravity * m_GravityMultiplier * Time.fixedDeltaTime;
            }

            m_CharacterController.Move(m_MoveDir * Time.fixedDeltaTime);

            ProgressStepCycle(speed);
            UpdateCameraPosition(speed);
		}

		private void FixedUpdate()
		{


		}




		private void ProgressStepCycle(float speed)
		{
			if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
			{
				m_StepCycle += (m_CharacterController.velocity.magnitude + (speed*(m_IsWalking ? 1f : m_RunstepLenghten)))*
					Time.fixedDeltaTime;
			}

			if (!(m_StepCycle > m_NextStep))
			{
				return;
			}

			m_NextStep = m_StepCycle + m_StepInterval;

		}



		private void UpdateCameraPosition(float speed)
		{
			Vector3 newCameraPosition;
			if (!m_UseHeadBob)
			{
				return;
			}
			if (m_CharacterController.velocity.magnitude > 0 && m_CharacterController.isGrounded)
			{
				m_Camera.transform.localPosition =
					m_HeadBob.DoHeadBob(m_CharacterController.velocity.magnitude +
						(speed*(m_IsWalking ? 1f : m_RunstepLenghten)));
				newCameraPosition = m_Camera.transform.localPosition;
				newCameraPosition.y = m_Camera.transform.localPosition.y - m_JumpBob.Offset();
			}
			else
			{
				newCameraPosition = m_Camera.transform.localPosition;
				newCameraPosition.y = m_OriginalCameraPosition.y - m_JumpBob.Offset();
			}
			m_Camera.transform.localPosition = newCameraPosition;
		}


		private void GetInput(out float speed)
		{
			// Read input
			float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
			float vertical = CrossPlatformInputManager.GetAxis("Vertical");

			bool waswalking = m_IsWalking;

			#if !MOBILE_INPUT
			// On standalone builds, walk/run speed is modified by a key press.
			// keep track of whether or not the character is walking or running
			m_IsWalking = !Input.GetKey(KeyCode.LeftShift);
			#endif
			// set the desired speed to be walking or running
			speed = m_IsWalking ? m_WalkSpeed : m_RunSpeed;
			m_Input = new Vector2(horizontal, vertical);

			// normalize input if it exceeds 1 in combined length:
			if (m_Input.sqrMagnitude > 1)
			{
				m_Input.Normalize();
			}

			// handle speed change to give an fov kick
			// only if the player is going to a run, is running and the fovkick is to be used
			if (m_IsWalking != waswalking && m_UseFovKick && m_CharacterController.velocity.sqrMagnitude > 0)
			{
				StopAllCoroutines();
				StartCoroutine(!m_IsWalking ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
			}
		}

		private void RotateView()
		{
			if (/*!captureScreenshot &&*/ rotateMouseLook) {
				m_MouseLook.LookRotation (transform, m_Camera.transform);
			}
		}
	}
}

