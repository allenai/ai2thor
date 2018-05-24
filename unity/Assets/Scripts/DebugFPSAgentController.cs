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
		public float MaxDistance = 1.0f;

		[SerializeField] private bool m_IsWalking;
		[SerializeField] private float m_WalkSpeed;
		[SerializeField] private float m_RunSpeed;
		[SerializeField] [Range(0f, 1f)] private float m_RunstepLenghten;
		//[SerializeField] private float m_JumpSpeed;
		//[SerializeField] private float m_StickToGroundForce;
		[SerializeField] private float m_GravityMultiplier;
		[SerializeField] private MouseLook m_MouseLook;
		//[SerializeField] private bool m_UseFovKick;
		//[SerializeField] private FOVKick m_FovKick = new FOVKick();
		//[SerializeField] private bool m_UseHeadBob;
		//[SerializeField] private CurveControlledBob m_HeadBob = new CurveControlledBob();
		//[SerializeField] private LerpControlledBob m_JumpBob = new LerpControlledBob();
		//[SerializeField] private float m_StepInterval;
		//[SerializeField] private AudioClip[] m_FootstepSounds;    // an array of footstep sounds that will be randomly selected from.
		//[SerializeField] private AudioClip m_JumpSound;           // the sound played when character leaves the ground.
		//[SerializeField] private AudioClip m_LandSound;           // the sound played when character touches back on ground.


	
        //[SerializeField] private GameObject Target_Text = null;
        [SerializeField] private GameObject Debug_Canvas = null;
       //[SerializeField] private bool isReceptacle = false;
       //[SerializeField] private bool isPickup = false;

       // [SerializeField] private string current_Object_In_Inventory = null;
        [SerializeField] private GameObject Inventory_Text = null;
		[SerializeField] private GameObject InputMode_Text = null;
        [SerializeField] private GameObject AgentHand = null;
        [SerializeField] private GameObject ItemInHand = null;

		private Camera m_Camera;
		//private bool m_Jump;
		//private float m_YRotation;
		public bool rotateMouseLook;
		private Vector2 m_Input;
		private Vector3 m_MoveDir = Vector3.zero;
		private CharacterController m_CharacterController;
		//private CollisionFlags m_CollisionFlags;
		//private bool m_PreviouslyGrounded;
		//private Vector3 m_OriginalCameraPosition;
		//private float m_StepCycle;
		//private float m_NextStep;
		//private bool m_Jumping;

        //this is true if FPScontrol mode using Mouse and Keyboard is active
        public bool TextInputMode = false;

		//private AudioSource m_AudioSource;
              
		//public Collider[] TestcollidersHit = null;

		//public GameObject TestBall = null;

		public Transform DefaultHandPosition = null;
		public GameObject HandSweepPosition = null;
		public GameObject RotationSweepTestPivot = null;

		public SimObjPhysics[] VisibleObjects; //these objects are within the camera viewport and in range of the agent

		//public GameObject TestObject = null;

		public bool IsHandDefault = true;

		public GameObject InputFieldObj = null;

        private void Start()
        {
            m_CharacterController = GetComponent<CharacterController>();
            m_Camera = Camera.main;
            //m_OriginalCameraPosition = m_Camera.transform.localPosition;
            //m_FovKick.Setup(m_Camera);
            //m_HeadBob.Setup(m_Camera, m_StepInterval);
            //m_StepCycle = 0f;
            //m_NextStep = m_StepCycle / 2f;
            //m_Jumping = false;
            //m_AudioSource = GetComponent<AudioSource>();
            m_MouseLook.Init(transform, m_Camera.transform);
            
            //grab text object on canvas to update with what is currently targeted by reticle
            //Target_Text = GameObject.Find("DebugCanvas/TargetText"); ;
            Debug_Canvas = GameObject.Find("DebugCanvas");
			Inventory_Text = GameObject.Find("DebugCanvas/InventoryText");
			InputMode_Text = GameObject.Find("DebugCanvas/InputModeText");

            //if this component is enabled, turn on the targeting reticle and target text
            if (this.isActiveAndEnabled)
            {
                Debug_Canvas.SetActive(true);
                //Target_Text.SetActive(true);

                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }


			//Time.timeScale = 10.0f;
        }
        
        public void MoveForward()
		{
			Vector3 motion = new Vector3(0f, 0f, 0.5f);
			motion.y = Physics.gravity.y * m_GravityMultiplier;
			m_CharacterController.Move(motion);
		}

        public void MoveLeft()
		{
			
		}

        public void MoveRight()
		{
			
		}

        public void LookUp()
		{
			
		}

        public void LookDown()
		{
			
		}

        public SimObjPhysics[] GetAllVisibleSimObjPhysics(Camera agentCamera, float maxDistance)
        {
            List<SimObjPhysics> currentlyVisibleItems = new List<SimObjPhysics>();

            Vector3 agentCameraPos = agentCamera.transform.position;
            
			//get all sim objects in range around us
            Collider[] colliders_in_view = Physics.OverlapSphere(agentCameraPos, maxDistance, 
                                                         1 << 8 , QueryTriggerInteraction.Collide); //layermask is 8

            if(colliders_in_view != null)
            {
                foreach (Collider item in colliders_in_view)
                {
					if(item.tag == "SimObjPhysics")
					{
						//get the SimObjPhysics component from the collider that was found
                        SimObjPhysics sop = item.GetComponent<SimObjPhysics>();

                        //is the sim object in range, check if it is bounds of the camera's viewport
                        if (CheckIfInViewport(sop, agentCamera, maxDistance))
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
        public bool CheckIfInViewport(SimObjPhysics item, Camera agentCamera, float maxDistance)
        {
			//return true result if object is within the Viewport, false if not in viewport or the viewport doesn't care about the object
			bool result = false;

			SimObjProperty[] itemProperty = item.GetComponent<SimObjPhysics>().Properties;

            bool DoWeCareABoutThisObjectsVisibility = false;

			foreach (SimObjProperty prop in itemProperty)
            {
				//check for sim objects that can be picked up and/or interacted with via Hand
				if (prop != SimObjProperty.Static && prop != SimObjProperty.Moveable)
                {
                    DoWeCareABoutThisObjectsVisibility = true;
                }
            }

            if (DoWeCareABoutThisObjectsVisibility == true)
            {
                Vector3 viewPoint = agentCamera.WorldToViewportPoint(item.transform.position);

                //move these two up top as serialized variables later, or maybe not? values between 0 and 1 will cause "tunnel vision"
                float ViewPointRangeHigh = 1.0f;
                float ViewPointRangeLow = 0.0f;

				//note: Viewport space normalized as bottom left (0,0) and top right(1, 1)
                if (viewPoint.z > 0 && viewPoint.z < maxDistance //is in front of camera and within range of visibility sphere
                   && viewPoint.x < ViewPointRangeHigh && viewPoint.x > ViewPointRangeLow//within x bounds of viewport
                    && viewPoint.y < ViewPointRangeHigh && viewPoint.y > ViewPointRangeLow)//within y bounds of viewport
                {
                    result = true;
                }

                else
                    result = false;
            }
            else 
				result = false;

			return result;
        }
      
        //if the agent were to rotate Left/Right/up/down, would the hand hit anything that should prevent the agent from rotating
        public bool CheckIfAgentCanRotate(string rotation)
		{

			if (ItemInHand == null)
            {
                Debug.Log("No need to check rotation if empty handed");
                return false;
            }

			//returns true if Rotation is allowed
			bool result = false;

			//zero out the pivot
   			RotationSweepTestPivot.transform.localRotation = Quaternion.Euler(Vector3.zero);

			//first move position to wheever the Agent's hand is
			HandSweepPosition.transform.position = AgentHand.transform.position;

            if(rotation == "right")
   				//next rotate the RotationSweepTestPivot to simulate the Agent rotating 90 degrees
                RotationSweepTestPivot.transform.localRotation = Quaternion.Euler(new Vector3(0, 90, 0));
			if(rotation == "left")
				RotationSweepTestPivot.transform.localRotation = Quaternion.Euler(new Vector3(0, -90, 0));
			if (rotation == "up")
				RotationSweepTestPivot.transform.localRotation = Quaternion.Euler(new Vector3(-30, 0, 0));
			if (rotation == "down")
				RotationSweepTestPivot.transform.localRotation = Quaternion.Euler(new Vector3(45, 0, 0));

         

            //now perform a sweeptest from AgentHand's current position to HandSweepPosition after the rotation

            RaycastHit hit;


			//if (ItemInHand == null)
			//{
			//	Rigidbody HandRB = AgentHand.GetComponent<Rigidbody>();
			//	if (HandRB.SweepTest(HandSweepPosition.transform.position - AgentHand.transform.position, out hit,
   //                  Vector3.Distance(HandSweepPosition.transform.position, AgentHand.transform.position)))
   //             {
   //                 //ignore hits if it is the Agent itself or the Agent's Hand
   //                 if (hit.transform != AgentHand.transform && hit.transform != gameObject.transform)
   //                 {
			//			if(rotation == "right")
			//			{
			//				Debug.Log(hit.transform.name + " is in Agent Hand's Path! Can't rotate Agent RIGHT");
   //                         result = false;
			//			}

			//			else
			//			{
			//				Debug.Log(hit.transform.name + " is in Agent Hand's Path! Can't rotate Agent LEFT");
   //                         result = false;
			//			}

   //                 }               
   //             }

			//	else
   //             {
   //                 if (direction == 1)
   //                 {
   //                     Debug.Log("Rotation to the RIGHT 90 degrees is possible!");
   //                     result = true;
   //                 }

   //                 else
   //                 {
   //                     Debug.Log("Rotation to the LEFT 90 degrees is possible!");
   //                     result = true;
   //                 }
   //             }
			//}
         

            //for i there is an Item in the agent's hand right now
			if (ItemInHand != null)
			{
				Rigidbody ItemRB = ItemInHand.GetComponent<Rigidbody>();
				if (ItemRB.SweepTest(HandSweepPosition.transform.position - AgentHand.transform.position, out hit,
                     Vector3.Distance(HandSweepPosition.transform.position, AgentHand.transform.position)))
                {
					print(hit.transform.name);
                    //ignore hits if it is the Agent itself or the Agent's Hand
					if (hit.transform != AgentHand.transform && hit.transform != gameObject.transform && hit.transform != ItemInHand.transform)
                    {
						if(rotation == "right")
                        {
                            Debug.Log(hit.transform.name + " is in Agent Hand's Path! Can't rotate Agent RIGHT");
                            result = false;
                        }

						if(rotation == "left")                       
						{
                            Debug.Log(hit.transform.name + " is in Agent Hand's Path! Can't rotate Agent LEFT");
                            result = false;
                        }

                        if(rotation == "up")
						{
							Debug.Log(hit.transform.name + " is in Agent Hand's Path! Can't rotate View UP");
						}

                        if(rotation == "down")
						{
							Debug.Log(hit.transform.name + " is in Agent Hand's Path! Can't rotate View DOWN");

						}
                    }
     
                }

				else
                {
                    if (rotation == "right")
                    {
                        Debug.Log("Rotation to the RIGHT 90 degrees is possible!");
                        result = true;
                    }

					else
                    {
                        Debug.Log("Rotation to the LEFT 90 degrees is possible!");
                        result = true;
                    }
                }
			}

			return result;
		}
        
        //If an object is in the agent's hand, sweeptest desired move distance to check for blocking objects
		public bool CheckIfHandBlocksMovement(float moveMagnitude, string direction)
		{
			bool result = false;

            //if there is nothing in our hand, we are good, return!
			if(ItemInHand == null)
			{
				result = true;
				Debug.Log("Agent has nothing in hand blocking movement");
				return result;
   			}

            //otherwise we are holding an object and need to do a sweep using that object's rb
			else
			{
				Vector3 dir = new Vector3();

                //use the agent's forward as reference
				switch (direction)
                {
                    case "forward":
                        dir = gameObject.transform.forward;
                        break;

                    case "left":
                        dir = -gameObject.transform.right;
                        break;

                    case "right":
                        dir = gameObject.transform.right;
                        break;

                    default:
                        Debug.Log("Incorrect direction input! Allowed Directions: forward, left, right");
                        break;
                }
				//otherwise we haev an item in our hand, so sweep using it's rigid body.
                RaycastHit hit;
                
                Rigidbody rb = ItemInHand.GetComponent<Rigidbody>();
				if(rb.SweepTest(dir, out hit, moveMagnitude, QueryTriggerInteraction.Ignore))
				{
					Debug.Log(hit.transform.name + " is blocking Agent Hand holding " + ItemInHand.name + " from moving " + direction);
                    result = false;
				}
				//nothing was hit, we good
                else
                {
					Debug.Log("Agent hand holding " + ItemInHand.name + " can move " + direction + " " + moveMagnitude + " units");
                }
			}
         
			return result;
		}
              
        //
        public bool CheckIfAgentCanMove(float moveMagnitude, string direction)
		{
			bool result = false;
			//RaycastHit hit;

			Vector3 dir = new Vector3();

            switch(direction)
			{
				case "forward":
					dir = gameObject.transform.forward;
					break;

				case "left":
					dir = -gameObject.transform.right;
					break;

				case "right":
					dir = gameObject.transform.right;
					break;
                
				default:
					Debug.Log("Incorrect direction input! Allowed Directions: forward, left, right");
					break;
			}

			Rigidbody rb = gameObject.GetComponent<Rigidbody>();

            //might need to sweep test all, check for static..... want to be able to try and move through sim objects that can pickup and move yes
			RaycastHit[] sweepResults = rb.SweepTestAll(dir, moveMagnitude, QueryTriggerInteraction.Ignore);

            //check each of the hit results, check if its tag is a "structure" and if so, we can't move, otherwise clear to move?
            //i guess also check if it is a sim object, make sure it isn't static?
			foreach(RaycastHit res in sweepResults)
			{
				if(res.transform.tag == "Structure")
				{
					result = false;
					Debug.Log(res.transform.name + " is blocking the Agent from moving " + direction);
					return result;
				}

				if(res.transform.GetComponent<SimObjPhysics>())
				{
					SimObjProperty[] resProperties = res.transform.GetComponent<SimObjPhysics>().Properties;
                    //now check if any of the manip types are Static, if so we can't move through them either
					foreach(SimObjProperty rmt in resProperties)
					{
						if(rmt == SimObjProperty.Static)
						{
							result = false;
							Debug.Log(res.transform.name + " is blocking the Agent from moving " + direction);
							return result;
						}
					}
				}
			}
         
			//if(rb.SweepTest(dir, out hit, moveMagnitude, QueryTriggerInteraction.Ignore))
			//{
			//	Debug.Log(hit.transform.name + " is blocking the Agent Body's " + direction + " movement");
			//	result = false;
			//}

			//else
			//{
				Debug.Log("Agent Body can move " + direction);
				result = true;
			//}
            
            return result;
		}
        
        //returns true if the Hand Movement was succesful
        //false if blocked by something or out of range
        public bool MoveHand(Vector3 targetPosition)
        {
			bool result = false;

			//can only move hand if there is an object in it.
			if(ItemInHand == null)
			{
				Debug.Log("Agent can only move hand if holding an item");
				result = false;
				return result;
			}
			//result if movement was succesful or not

         
			//first check if passed in targetPosition is in range or not           
			if(Vector3.Distance(gameObject.transform.position, targetPosition) > MaxDistance + 0.3)
			{           
				Debug.Log("The target position is out of range");
				result = false;
				return result;
			}

			//get viewport point of target position
            Vector3 vp = m_Camera.WorldToViewportPoint(targetPosition);

            //Note: Viewport normalizes to (0,0) bottom left, (1, 0) top right of screen
            //now make sure the targetPosition is actually within the Camera Bounds       
			if (vp.z < 0 || vp.x > 1.0f || vp.x < 0.0f || vp.y > 1.0f || vp.y < 0.0f)
            {
				Debug.Log("The target position is not in the Agent's Viewport!");
				result = false;
				return result;
            }
         
			Rigidbody ItemRB = ItemInHand.GetComponent<Rigidbody>();
			RaycastHit hit;

            //put the Hand position update inside this, soince the object will always hit the agent Hand once, which we ignore
            if(ItemRB.SweepTest(targetPosition - AgentHand.transform.position, out hit, Vector3.Distance(targetPosition, AgentHand.transform.position)))
			{
				//return error if anything but the Agent Hand or the Agent are hit
				if(hit.transform != AgentHand.transform && hit.transform != gameObject.transform)
				{
				Debug.Log(hit.transform.name + " is in Object In Hand's Path! Can't Move Hand holding " + ItemInHand.name);
					result = false;
				}


				else
                {
					Debug.Log("Movement of Agent Hand holding " + ItemInHand.name + " succesful!");
                    AgentHand.transform.position = targetPosition;
				    IsHandDefault = false;
					result = true;
                }
			}

			else
            {
                AgentHand.transform.position = targetPosition;
				result = true;
            }

		return result;
         
        }
        
		//for DebugController use only: cast ray from camera point to world, attempt to move hand to that position + a Y offset
		public Vector3 ScreenPointMoveHand(float yOffset)
		{
			RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			//shoot a ray out based on mouse position
			Physics.Raycast(ray, out hit);

				//TestBall.transform.position = hit.point + new Vector3(0, 0.3f, 0);
				return hit.point + new Vector3(0, yOffset, 0);
		}

        public void ResetAgentHandPosition()
		{
			AgentHand.transform.position = DefaultHandPosition.position;
		}

		public void ResetAgentHandRotation()
		{
			AgentHand.transform.localRotation = Quaternion.Euler(Vector3.zero);
		}

		public void DefaultAgentHand()
		{
			ResetAgentHandPosition();
			ResetAgentHandRotation();
			IsHandDefault = true;
		}

        //pickup a sim object
        public bool PickUpSimObjPhysics(Transform target)
        {
            //make sure hand is empty, turn off the target object's collision and physics properties
            //and make the object kinematic
            if (ItemInHand == null)
            {
				if(IsHandDefault == false)
				{
					Debug.Log("Reset Hand to default position before attempting to Pick Up objects");
					return false;
				}

				//default hand rotation for further rotation manipulation
				ResetAgentHandRotation();
                //move the object to the hand's default position.
                target.GetComponent<Rigidbody>().isKinematic = true;
                //target.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;
                target.position = AgentHand.transform.position;
				//AgentHand.transform.parent = target;
				target.SetParent(AgentHand.transform);
                //target.parent = AgentHand.transform;
                //update "inventory"

                //this is only in debug mode - probs delete this part when porting to BaseFPSAgent
                
                ItemInHand = target.gameObject;
				Text txt = Inventory_Text.GetComponent<Text>();
				txt.text = "In Inventory: " + target.name + " " + target.GetComponent<SimObjPhysics>().UniqueID;

                /////////////////

				return true;
            }

            else
			{
				Debug.Log("Your hand has something in it already!");
				return false;
			}
         
        }

        public bool DropSimObjPhysics()
		{
			//make sure something is actually in our hands
			if (ItemInHand != null)
			{
				ItemInHand.GetComponent<Rigidbody>().isKinematic = false;
				ItemInHand.transform.parent = null;
				ItemInHand = null;

                //take this out later when moving to BaseFPS agent controller
				Text txt = Inventory_Text.GetComponent<Text>();
				txt.text = "In Inventory: Nothing!";
                ///////
            
				return true;
			}

			else
			{
				Debug.Log("nothing in hand to drop!");
				return false;
			}

		}
        
        //used by RotateSimObjPhysicsInHand for compound collider object comparison
        private bool CheckForMatches(IEnumerable<Transform> objects, Transform toCompare )
		{
			foreach (Transform t in objects)
			{
				if(toCompare == t)
				{
					return true;
				}
			}     
			return false;
		}

        public bool RotateSimObjPhysicsInHand(Vector3 vec)
        {
			//based on the collider type of the item in the Agent's Hand, set the radius of the OverlapSphere to check if there is room for rotation
            if(ItemInHand != null)
            {
                //for items that use box colliders
                if(ItemInHand.GetComponent<BoxCollider>())
                {
                    Vector3 sizeOfBox = ItemInHand.GetComponent<BoxCollider>().size;
					//do an overlapshere around the agent with radius based on max size of xyz of object in hand's collider

                    //find the radius of the overlap sphere based on max length of dimensions of box collider
					float overlapRadius = Math.Max(Math.Max(sizeOfBox.x, sizeOfBox.y), sizeOfBox.z) / 2;
     					//since the sim objects have wonky scales, find the percent increase or decrease to multiply the radius by to match the scale of the sim object
					Vector3 itemInHandScale = ItemInHand.transform.lossyScale;
					//take the average of each axis scale, even though they should all be THE SAME but just in case
					float avgScale = (itemInHandScale.x + itemInHandScale.y + itemInHandScale.z) / 3;
                    //adjust radius according to scale of item in hand
					overlapRadius = overlapRadius * avgScale;

                    Collider[] hitColliders = Physics.OverlapSphere(AgentHand.transform.position, 
					                                                overlapRadius);

					//TestcollidersHit = hitColliders;

                    //for objects that might have compound colliders, make sure we track them here for comparison below
					//NOTE: Make sure any objects with compound colliders have an "isTrigger" Collider on the highest object in the 
					//Heirarchy. The check for "Box" or "Sphere" Collider will use that trigger collider for radius calculations, since
                    //getting the dimensions of a compound collider wouldn't make any sense due to irregular shapes
					Transform[] anyChildren = ItemInHand.GetComponentsInChildren<Transform>();
               
                    foreach(Collider col in hitColliders)
                    {
                        //check if the thing collided with by the OverlapSphere is the agent, the hand, or the object itself
                        if(col.name != "TextInputModeler" && col.name != "TheHand" && col.name != ItemInHand.name)
                        {
							//also check against any children the ItemInHand has for prefabs with compound colliders                     
							//set to true if there is a match between this collider among ANY of the children of ItemInHand
                            
							if(CheckForMatches(anyChildren, col.transform) == false)
							{
								Debug.Log(col.name + " blocking rotation");
                                Debug.Log("Not Enough Room to Rotate");
                                return false;
							}
                     
                        }
                  
						else
						{
							AgentHand.transform.localRotation = Quaternion.Euler(vec);
							return true;
						}
                    }               
                }


                //for items with sphere collider
                if (ItemInHand.GetComponent<SphereCollider>())
                {
                    float radiusOfSphere = ItemInHand.GetComponent<SphereCollider>().radius;

					Vector3 itemInHandScale = ItemInHand.transform.lossyScale;

					float avgScale = (itemInHandScale.x + itemInHandScale.y + itemInHandScale.z) / 3;

					radiusOfSphere = radiusOfSphere * avgScale;

                    Collider[] hitColliders = Physics.OverlapSphere(AgentHand.transform.position, radiusOfSphere);

                    foreach (Collider col in hitColliders)
                    {
                        //print(col.name);
                        if (col.name != "TextInputModeler" && col.name != "TheHand" && col.name != ItemInHand.name)
                        {
                            Debug.Log("Not Enough Room to Rotate");
                            return false;
                        }

						else
						{
							AgentHand.transform.localRotation = Quaternion.Euler(vec);
							return true;
						}
                    }
               
                }            
            }
         
            //if nothing is in your hand, nothing to rotate so don't!
            Debug.Log("Nothing In Hand to rotate!");
            return false;

        }

		#if UNITY_EDITOR
        //used to show what's currently visible on the top left of the screen
        void OnGUI()
        {
            if (VisibleObjects != null)
            {
                if (VisibleObjects.Length > 10)
                {
                    int horzIndex = -1;
                    GUILayout.BeginHorizontal();
                    foreach (SimObjPhysics o in VisibleObjects)
                    {
                        horzIndex++;
                        if (horzIndex >= 3)
                        {
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            horzIndex = 0;
                        }
                        GUILayout.Button(o.UniqueID, UnityEditor.EditorStyles.miniButton, GUILayout.MaxWidth(200f));
                    }

                    GUILayout.EndHorizontal();
                }

                else
                {
                    Plane[] planes = GeometryUtility.CalculateFrustumPlanes(m_Camera);

                    //int position_number = 0;
                    foreach (SimObjPhysics o in VisibleObjects)
                    {
                        string suffix = "";
                        Bounds bounds = new Bounds(o.gameObject.transform.position, new Vector3(0.05f, 0.05f, 0.05f));
                        if (GeometryUtility.TestPlanesAABB(planes, bounds))
                        {
                            //position_number += 1;

                            //if (o.GetComponent<SimObj>().Manipulation == SimObjManipProperty.Inventory)
                            //    suffix += " VISIBLE: " + "Press '" + position_number + "' to pick up";

                            //else
                            //suffix += " VISIBLE";
                            if (o.isInteractable == true)
                            {
                                suffix += " INTERACTABLE";
                            }
                        }


                        GUILayout.Button(o.UniqueID + suffix, UnityEditor.EditorStyles.miniButton, GUILayout.MinWidth(100f));
                    }
                }
            }
        }
        #endif

        private void DebugKeyboardControls()
		{
			//swap between text input and not
			if (Input.GetKeyDown(KeyCode.BackQuote))
            {
				//Switch to Text Mode
                if (TextInputMode == false)
                {
					InputMode_Text.GetComponent<Text>().text = "Input Mode: Text";
                    TextInputMode = true;
                    return;
                }

                //Switch to Mouse and Keyboard Mode
                if (TextInputMode == true)
                {               
					InputMode_Text.GetComponent<Text>().text = "Input Mode: Keyboard/Mouse";
                    TextInputMode = false;
                    return;
                }

            }

            //allow actions only via text input
            if (TextInputMode == true)
            {
                //if we press enter, select the input field
                if (Input.GetKeyDown(KeyCode.Return))
                {
                    UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(InputFieldObj);
                    print("input field");
                }
            }

            //no text input, we are in fps mode
            if (TextInputMode == false)
            {
				if(Input.GetKey(KeyCode.Space))
				{
					Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
				}

                if(Input.GetKeyUp(KeyCode.Space))
				{
					Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;

				}

                if (Input.GetMouseButtonDown(0))
                {
                    RaycastHit hit;
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    //shoot a ray out to select an object
                    if (Physics.Raycast(ray, out hit))
                    {
                        //check if the hit object is a SimObj in our array of Accessible sim objects
                        if (hit.transform.tag == "SimObjPhysics")
                        {
                            //if an interaction point is accessible by the hand, proceed to try and pick it up
                            if (hit.transform.GetComponent<SimObjPhysics>().isInteractable == true)
                            {
                                //print(hit.transform.name + " is pickupable!");

                                //pickup the object here
                                PickUpSimObjPhysics(hit.transform);

                            }
                        }
                    }
                }


                //on right mouse click
                if (Input.GetMouseButtonDown(1))
                {
                    DropSimObjPhysics();
                }

                if (Input.GetKeyDown(KeyCode.E))
                {
                    MoveHand(ScreenPointMoveHand(0.1f));

                    //MoveHand(TestObject.transform.position);
                    //print(TestObject.transform.position);
                }

                if (Input.GetKeyDown(KeyCode.R))
                {
                    MoveHand(ScreenPointMoveHand(0.3f));
                }

                //default position and rotation
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    DefaultAgentHand();
                }

                //check if the agent can rotate left or right, return errors if the agent hand would hit anything
                if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    CheckIfAgentCanRotate("right");
                }

                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    CheckIfAgentCanRotate("left");
                }

				if (Input.GetKeyDown(KeyCode.Alpha3))
                {
                    CheckIfAgentCanRotate("up");
                }

				if (Input.GetKeyDown(KeyCode.Alpha4))
                {
                    CheckIfAgentCanRotate("down");
                }
                //

                //Rotate tests for objects in agent hand
                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    RotateSimObjPhysicsInHand(new Vector3(0, 0, 0));
                }

                if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    RotateSimObjPhysicsInHand(new Vector3(180, 0, 0));
                }

                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    RotateSimObjPhysicsInHand(new Vector3(0, 0, 90));
                }

                if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    RotateSimObjPhysicsInHand(new Vector3(0, 0, -90));
                }
                ////////////
                /// 

                //Check Forward Movement
                if (Input.GetKeyDown(KeyCode.I))
                {
                    CheckIfHandBlocksMovement(1.0f, "forward");
                    //CheckIfAgentCanMove(1.0f, "forward");
                 
                }

                if (Input.GetKeyDown(KeyCode.J))
                {
					CheckIfAgentCanRotate("left");
                   // CheckIfHandBlocksMovement(1.0f, "left");
                    //CheckIfAgentCanMove(1.0f, "left");
                }

                if (Input.GetKeyDown(KeyCode.L))
                {
                    CheckIfHandBlocksMovement(1.0f, "right");
                    //CheckIfAgentCanMove(1.0f, "right");
                }

            }
		}

		private void Update()	
        {
			//constantly check for visible objects in front of agent
			VisibleObjects = GetAllVisibleSimObjPhysics(m_Camera, MaxDistance);

			DebugKeyboardControls();
         
            ///////////////////////////////////////////////////////////////////////////
			//we are not in focus mode, so use WASD and mouse to move around
			if(TextInputMode == false)
			{
				//this is the mouselook in first person mode

				FPSInput();

				if(Cursor.visible == false)
				{
					RotateView();
				}
			}

            //we are in focus mode, this should be the default - can toggle fps control from here
            //by default we can only use enter to execute commands in the text field
			if(TextInputMode == true)
			{
				
			}
	
		}

		private void FixedUpdate()
		{


		}
      
		private void GetInput(out float speed)
		{
			// Read input
			float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
			float vertical = CrossPlatformInputManager.GetAxis("Vertical");

			//bool waswalking = m_IsWalking;

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
			//if (m_IsWalking != waswalking && m_UseFovKick && m_CharacterController.velocity.sqrMagnitude > 0)
			//{
			//	StopAllCoroutines();
			//	StartCoroutine(!m_IsWalking ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
			//}
		}

		private void RotateView()
		{
			if (/*!captureScreenshot &&*/ rotateMouseLook) {
				m_MouseLook.LookRotation (transform, m_Camera.transform);
			}
		}

        private void FPSInput()
		{
			//// the jump state needs to read here to make sure it is not missed
            //if (!m_Jump)
            //{
            //  //m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");
            //}

            //if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
            //{
            //  StartCoroutine(m_JumpBob.DoBobCycle());
            //  m_MoveDir.y = 0f;
            //  m_Jumping = false;
            //}
            //if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded)
            //{
            //  m_MoveDir.y = 0f;
            //}

            //m_PreviouslyGrounded = m_CharacterController.isGrounded;


            //take WASD input and do magic, turning it into movement!
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

            //if (m_CharacterController.isGrounded)
            //{
            //    m_MoveDir.y = -m_StickToGroundForce;

            //    if (m_Jump)
            //    {
            //        m_MoveDir.y = m_JumpSpeed;
            //        //PlayJumpSound();
            //        m_Jump = false;
            //        m_Jumping = true;
            //    }
            //}

            //else
            //{
                m_MoveDir += Physics.gravity * m_GravityMultiplier * Time.fixedDeltaTime;
            //}

            m_CharacterController.Move(m_MoveDir * Time.fixedDeltaTime);

            //ProgressStepCycle(speed);
            //UpdateCameraPosition(speed);
		}





        //private void ProgressStepCycle(float speed)
        //{
        //  if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
        //  {
        //      m_StepCycle += (m_CharacterController.velocity.magnitude + (speed*(m_IsWalking ? 1f : m_RunstepLenghten)))*
        //          Time.fixedDeltaTime;
        //  }

        //  if (!(m_StepCycle > m_NextStep))
        //  {
        //      return;
        //  }

        //  m_NextStep = m_StepCycle + m_StepInterval;

        //}


        //this is only for headbob
        //private void UpdateCameraPosition(float speed)
        //{
        //  Vector3 newCameraPosition;
        //  if (!m_UseHeadBob)
        //  {
        //      return;
        //  }
        //  if (m_CharacterController.velocity.magnitude > 0 && m_CharacterController.isGrounded)
        //  {
        //      m_Camera.transform.localPosition =
        //          m_HeadBob.DoHeadBob(m_CharacterController.velocity.magnitude +
        //              (speed*(m_IsWalking ? 1f : m_RunstepLenghten)));
        //      newCameraPosition = m_Camera.transform.localPosition;
        //      newCameraPosition.y = m_Camera.transform.localPosition.y - m_JumpBob.Offset();
        //  }
        //  else
        //  {
        //      newCameraPosition = m_Camera.transform.localPosition;
        //      newCameraPosition.y = m_OriginalCameraPosition.y - m_JumpBob.Offset();
        //  }
        //  m_Camera.transform.localPosition = newCameraPosition;
        //}

        //      //check if if the agent's HAND would hit anything in front/left/right of it, if the agent moved/strafed
        //      public bool CheckIfHandCanMoveForward(float moveMagnitude)
        //{
        //  //Note: sweeptest forward using TheHandDefaultPosition since its forward is constant

        //  bool result = false;

        //  RaycastHit hit;

        //          //for empty handed
        //  if(ItemInHand == null)
        //  {
        //      Rigidbody rb = AgentHand.GetComponent<Rigidbody>();

        //              //yo we hit something
        //              if (rb.SweepTest(DefaultHandPosition.forward, out hit, moveMagnitude))
        //              {
        //          Debug.Log(hit.transform.name + " is blocking Agent Hand FORWARD movement");
        //          result = false;
        //              }

        //              //nothing hit, we are clear!
        //      else
        //      {
        //          Debug.Log("Agent hand can move FORWARD " + moveMagnitude + " units");
        //          result = true;
        //      }
        //  }

        //          //oh we are holding something 
        //  else
        //  {
        //      Rigidbody rb = ItemInHand.GetComponent<Rigidbody>();

        //              //the item rb hit something
        //              if(rb.SweepTest(DefaultHandPosition.forward, out hit, moveMagnitude))
        //      {
        //          Debug.Log(hit.transform.name + " is blocking Agent Hand holding " + ItemInHand.name + " from moving FORWARD");
        //          result = false;
        //      }

        //              //nothing was hit, we good
        //      else
        //      {
        //          Debug.Log("Agent hand holding " + ItemInHand.name + " can move FORWARD " + moveMagnitude + " units");
        //      }
        //  }

        //  return result;
        //}

        //      public bool CheckIfHandCanMoveLeft(float moveMagnitude)
        //{
        //  //Note: sweeptest forward using TheHandDefaultPosition since its forward is constant

        //          bool result = false;

        //          RaycastHit hit;

        //          //for empty handed
        //          if (ItemInHand == null)
        //          {
        //              Rigidbody rb = AgentHand.GetComponent<Rigidbody>();

        //              //yo we hit something
        //              if (rb.SweepTest(-DefaultHandPosition.right, out hit, moveMagnitude))
        //              {
        //                  Debug.Log(hit.transform.name + " is blocking Agent Hand LEFT movement");
        //                  result = false;
        //              }

        //              //nothing hit, we are clear!
        //              else
        //              {
        //                  Debug.Log("Agent hand can move LEFT " + moveMagnitude + " units");
        //                  result = true;
        //              }
        //          }

        //          //oh we are holding something 
        //          else
        //          {
        //              Rigidbody rb = ItemInHand.GetComponent<Rigidbody>();

        //              //the item rb hit something
        //              if (rb.SweepTest(-DefaultHandPosition.right, out hit, moveMagnitude))
        //              {
        //                  Debug.Log(hit.transform.name + " is blocking Agent Hand holding " + ItemInHand.name + " from moving LEFT");
        //                  result = false;
        //              }

        //              //nothing was hit, we good
        //              else
        //              {
        //                  Debug.Log("Agent hand holding " + ItemInHand.name + " can move LEFT " + moveMagnitude + " units");
        //              }
        //          }

        //          return result;
        //}

        //public bool CheckIfHandCanMoveRight(float moveMagnitude)
        //{
        //  //Note: sweeptest forward using TheHandDefaultPosition since its forward is constant

        //          bool result = false;

        //          RaycastHit hit;

        //          //for empty handed
        //          if (ItemInHand == null)
        //          {
        //              Rigidbody rb = AgentHand.GetComponent<Rigidbody>();

        //              //yo we hit something
        //              if (rb.SweepTest(DefaultHandPosition.right, out hit, moveMagnitude))
        //              {
        //                  Debug.Log(hit.transform.name + " is blocking Agent Hand RIGHT movement");
        //                  result = false;
        //              }

        //              //nothing hit, we are clear!
        //              else
        //              {
        //                  Debug.Log("Agent hand can move RIGHT " + moveMagnitude + " units");
        //                  result = true;
        //              }
        //          }

        //          //oh we are holding something 
        //          else
        //          {
        //              Rigidbody rb = ItemInHand.GetComponent<Rigidbody>();

        //              //the item rb hit something
        //              if (rb.SweepTest(DefaultHandPosition.right, out hit, moveMagnitude))
        //              {
        //                  Debug.Log(hit.transform.name + " is blocking Agent Hand holding " + ItemInHand.name + " from moving RIGHT");
        //                  result = false;
        //              }

        //              //nothing was hit, we good
        //              else
        //              {
        //                  Debug.Log("Agent hand holding " + ItemInHand.name + " can move RIGHT " + moveMagnitude + " units");
        //              }
        //          }

        //          return result;
        //}

        //      //check if the AGENT ITSELF would hit anything if it were to move/strafe forward, left, or right, no check for rotation since it doesn't matter with capsule
        //      public bool CheckIfAgentCanMoveForward(float moveMagnitude)
        //{
        //  bool result = false;
        //  RaycastHit hit;

        //  Rigidbody rb = gameObject.GetComponent<Rigidbody>();

        //  if(rb.SweepTest(DefaultHandPosition.forward, out hit, moveMagnitude))
        //  {
        //      Debug.Log(hit.transform.name + " is blocking the Agent Body's forward movement");
        //      result = false;
        //  }

        //  else
        //  {
        //      Debug.Log("Agent Body can move forward");
        //      result = true;
        //  }

        //  return result;
        //}

        //      public void CheckIfAgentCanMoveLeft(float moveMagnitude)
        //{

        //}

        //      public void CheckIfAgentCanMoveRight(float moveMagnitude)
        //{

        //}

	}
}

