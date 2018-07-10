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
        //for use with mouse/keyboard input
		[SerializeField] private bool m_IsWalking;
		[SerializeField] private float m_WalkSpeed;
		[SerializeField] private float m_RunSpeed;


		[SerializeField] private float m_GravityMultiplier;
		[SerializeField] private MouseLook m_MouseLook;

        [SerializeField] private GameObject Debug_Canvas = null;
//        [SerializeField] private GameObject Inventory_Text = null;
		[SerializeField] private GameObject InputMode_Text = null;
//        [SerializeField] private GameObject AgentHand = null;
//        [SerializeField] private GameObject ItemInHand = null;

		private Camera m_Camera;
		//public bool rotateMouseLook;
		private Vector2 m_Input;
		private Vector3 m_MoveDir = Vector3.zero;
		private CharacterController m_CharacterController;

        //this is true if FPScontrol mode using Mouse and Keyboard is active
        public bool TextInputMode = false;
             
		//public Transform DefaultHandPosition = null;
        
  //      //for turning and look Sweeptests
		//public GameObject LookSweepPosition = null;
		//public GameObject LookSweepTestPivot = null; //if the Camera position ever moves, make sure this is set to the same local position as FirstPersonCharacter
		//public GameObject TurnSweepPosition = null;
		//public GameObject TurnSweepTestPivot = null;

		//public SimObjPhysics[] VisibleObjects; //these objects are within the camera viewport and in range of the agent

		//public GameObject TestObject = null;
        
		//public bool IsHandDefault = true;

		public GameObject InputFieldObj = null;

		//protected float[] LookAngles = { 60.0f, 30.0f, 0.0f, -30.0f };//make sure LookAngleIndex defaults to 0.0f's index
		//protected int LookAngleIndex = 2; //default to index 2, since agent defaults looking forward

  //      //set turn angles to prevent floating point error on rotation
		//protected float[] TurnAngles = { 0.0f, 90.0f, 180.0f, 270.0f };
		//protected int TurnAngleIndex = 0; 

        private void Start()
        {
            m_CharacterController = GetComponent<CharacterController>();
            m_Camera = Camera.main;
            m_MouseLook.Init(transform, m_Camera.transform);
            
            //find debug canvas related objects 
            Debug_Canvas = GameObject.Find("DebugCanvasPhysics");
			//Inventory_Text = GameObject.Find("DebugCanvas/InventoryText");
			InputMode_Text = GameObject.Find("DebugCanvasPhysics/InputModeText");

			InputFieldObj = GameObject.Find("DebugCanvasPhysics/InputField");

            //if this component is enabled, turn on the targeting reticle and target text
            if (this.isActiveAndEnabled)
            {
				Debug_Canvas.GetComponent<Canvas>().enabled = true;            
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;

				//ServerAction action = new ServerAction();
				//action.action = "test";
				//this.GetComponent<PhysicsRemoteFPSAgentController>().ProcessControlCommand(action);
            }

        }

  //      public void MoveAgent(string direction, float magnitude)
		//{
		//	//checks for if any object is obstructing agent/agent hand movement before executing move

		//	if(direction == "forward" && CheckIfHandBlocksAgentMovement(magnitude, "forward")
		//	   && CheckIfAgentCanMove(magnitude, "forward"))
		//	{
		//		Vector3 motion = transform.forward * magnitude;
  //              motion.y = Physics.gravity.y * m_GravityMultiplier;
  //              m_CharacterController.Move(motion);
		//	}

		//	if (direction == "backward" && CheckIfAgentCanMove(magnitude, "backward"))
  //          {            
		//		Vector3 motion = -transform.forward * magnitude;
  //              motion.y = Physics.gravity.y * m_GravityMultiplier;
  //              m_CharacterController.Move(motion);
  //          }

		//	if (direction == "left" && CheckIfHandBlocksAgentMovement(magnitude, "left")
		//	    && CheckIfAgentCanMove(magnitude, "left"))
  //          {
		//		CheckIfHandBlocksAgentMovement(magnitude, "left");

		//		Vector3 motion = -transform.right * magnitude;
  //              motion.y = Physics.gravity.y * m_GravityMultiplier;
  //              m_CharacterController.Move(motion);
  //          }

		//	if (direction == "right" && CheckIfHandBlocksAgentMovement(magnitude, "right")
		//	    && CheckIfAgentCanMove(magnitude, "right"))
  //          {
		//		CheckIfHandBlocksAgentMovement(magnitude, "right");
		//		Vector3 motion = transform.right * magnitude; 
		//		motion.y = Physics.gravity.y * m_GravityMultiplier;
  //              m_CharacterController.Move(motion);
  //          }
		//}

   
  //      public SimObjPhysics[] GetAllVisibleSimObjPhysics(Camera agentCamera, float maxDistance)
  //      {
  //          List<SimObjPhysics> currentlyVisibleItems = new List<SimObjPhysics>();

  //          Vector3 agentCameraPos = agentCamera.transform.position;
            
		//	//get all sim objects in range around us
  //          Collider[] colliders_in_view = Physics.OverlapSphere(agentCameraPos, maxDistance, 
  //                                                       1 << 8 , QueryTriggerInteraction.Collide); //layermask is 8

  //          if(colliders_in_view != null)
  //          {
  //              foreach (Collider item in colliders_in_view)
  //              {
		//			if(item.tag == "SimObjPhysics")
		//			{
		//				SimObjPhysics sop;

  //                      //if the object has no compound trigger colliders
		//				if (item.GetComponent<SimObjPhysics>())
		//				{
		//					sop = item.GetComponent<SimObjPhysics>();
  //     					}

  //                      //if the object does have compount trigger colliders, get the SimObjPhysics component from the parent
		//				else
		//				{
		//					sop = item.GetComponentInParent<SimObjPhysics>();
		//				}

		//				if (sop.VisibilityPoints.Length > 0)
		//				{
		//					Transform[] visPoints = sop.VisibilityPoints;
		//					int visPointCount = 0;

		//					foreach (Transform point in visPoints)
		//					{
		//						//if this particular point is in view...
		//						if (CheckIfVisibilityPointInViewport(point, agentCamera, maxDistance))
		//						{
		//							visPointCount++;
		//						}
		//					}

		//					if (visPointCount > 0)
		//					{
		//						sop.isVisible = true;
		//						if (!currentlyVisibleItems.Contains(sop))
		//							currentlyVisibleItems.Add(sop);
		//					}
		//				}

		//				else
		//					Debug.Log("Error! Set at least 1 visibility point on SimObjPhysics prefab!");
                  
		//			}
                  
  //              }

  //              //now that we have a list of currently visible items, let's see which ones are interactable!
  //              Rigidbody HandRB = AgentHand.GetComponent<Rigidbody>();
  //              //RaycastHit hit = new RaycastHit();

  //              foreach (SimObjPhysics visibleSimObjP in currentlyVisibleItems)
  //              {

  //                  //get all interaction points on the visible sim object we are checking here
  //                  Transform[] InteractionPoints = visibleSimObjP.InteractionPoints;

  //                  int ReachableInteractionPointCount = 0;
  //                  foreach (Transform ip in InteractionPoints)
  //                  {
  //                      //sweep test from agent's hand to each Interaction point
  //                      RaycastHit hit;
  //                      if(HandRB.SweepTest(ip.position - AgentHand.transform.position, out hit, maxDistance))
  //                      {
  //                          //if the object only has one interaction point to check
  //                          if(visibleSimObjP.InteractionPoints.Length == 1)
  //                          {
  //                              if (hit.transform == visibleSimObjP.transform)
  //                              {
  //                                  #if UNITY_EDITOR
  //                                  Debug.DrawLine(AgentHand.transform.position, ip.transform.position, Color.magenta);
  //                                  #endif

  //                                  visibleSimObjP.isInteractable = true;
  //                              }

  //                              else
  //                                  visibleSimObjP.isInteractable = false;
  //                          }

  //                          //this object has 2 or more interaction points
  //                          //if any one of them can be accessed by the Agent's hand, this object is interactable
  //                          if(visibleSimObjP.InteractionPoints.Length > 1)
  //                          {
                                
  //                              if(hit.transform == visibleSimObjP.transform)
  //                              {
  //                                  #if UNITY_EDITOR
  //                                  Debug.DrawLine(AgentHand.transform.position, ip.transform.position, Color.magenta);
  //                                  #endif
  //                                  ReachableInteractionPointCount++;
  //                              }

  //                              //check if at least one of the interaction points on this multi interaction point object
  //                              //is accessible to the agent Hand
  //                              if (ReachableInteractionPointCount > 0)
  //                              {
  //                                  visibleSimObjP.isInteractable = true;
  //                              }

  //                              else
  //                                  visibleSimObjP.isInteractable = false;
  //                          }
  //                      }                  
  //                  }               
  //              }
  //          }

  //          //populate array of visible items in order by distance
  //          currentlyVisibleItems.Sort((x, y) => Vector3.Distance(x.transform.position, agentCameraPos).CompareTo(Vector3.Distance(y.transform.position, agentCameraPos)));
  //          return currentlyVisibleItems.ToArray();
  //      }

  //      public bool CheckIfVisibilityPointInViewport(Transform point, Camera agentCamera, float maxDistance)
		//{
		//	bool result = false;

		//	Vector3 viewPoint = agentCamera.WorldToViewportPoint(point.position);

		//	float ViewPointRangeHigh = 1.0f;
		//	float ViewPointRangeLow = 0.0f;

		//	if (viewPoint.z > 0 && viewPoint.z < maxDistance //is in front of camera and within range of visibility sphere
  //                 && viewPoint.x < ViewPointRangeHigh && viewPoint.x > ViewPointRangeLow//within x bounds of viewport
  //                  && viewPoint.y < ViewPointRangeHigh && viewPoint.y > ViewPointRangeLow)//within y bounds of viewport
  //          {
  //              result = true;
		//		#if UNITY_EDITOR
		//		Debug.DrawLine(agentCamera.transform.position, point.position, Color.yellow);
  //              #endif
  //          }

  //          else
  //              result = false;
         
  //          return result;

		//}
   //     //see if a given SimObjPhysics is within the camera's range and field of view
   //     public bool CheckIfInViewport(SimObjPhysics item, Camera agentCamera, float maxDistance)
   //     {
			////return true result if object is within the Viewport, false if not in viewport or the viewport doesn't care about the object
			//bool result = false;

   //             Vector3 viewPoint = agentCamera.WorldToViewportPoint(item.transform.position);

   //             //move these two up top as serialized variables later, or maybe not? values between 0 and 1 will cause "tunnel vision"
   //             float ViewPointRangeHigh = 1.0f;
   //             float ViewPointRangeLow = 0.0f;

			//	//note: Viewport space normalized as bottom left (0,0) and top right(1, 1)
   //             if (viewPoint.z > 0 && viewPoint.z < maxDistance //is in front of camera and within range of visibility sphere
   //                && viewPoint.x < ViewPointRangeHigh && viewPoint.x > ViewPointRangeLow//within x bounds of viewport
   //                 && viewPoint.y < ViewPointRangeHigh && viewPoint.y > ViewPointRangeLow)//within y bounds of viewport
   //             {
   //                 result = true;
   //             }

   //             else
   //                 result = false;
   //         //}
   //         //else 
			//	//result = false;

			//return result;
        //}
      
        //changes agent's rotation, turn left or right
  //      public void Turn(string dir)
		//{
		//	if( dir == "left")
		//	{
		//		if(CheckIfAgentCanRotate("left"))
		//		{
		//			//transform.rotation = Quaternion.Euler(new Vector3(0, -90, 0));
		//			transform.Rotate(transform.rotation.x, transform.rotation.y - 90, transform.rotation.z);
		//		}
		//	}

  //          if(dir == "right")
		//	{
		//		if (CheckIfAgentCanRotate("right"))
  //              {
		//			transform.Rotate(transform.rotation.x, transform.rotation.y + 90, transform.rotation.z);
  //              }
		//	}
		//}
        
        //for turning agent left/right
  //      public void Turn(int direction)
		//{
		//	//currently restricting turning to 90 degrees left or right
		//	if(direction != 90 && direction != -90)
		//	{
		//		Debug.Log("Please give -90(left) or 90(right) as direction parameter");
		//		return;
		//	}

		//	if(CheckIfAgentCanTurn(direction))
		//	transform.Rotate(transform.rotation.x, transform.rotation.y + direction, transform.rotation.z);
		//	//transform.rotation = Quaternion.Euler(new Vector3(transform.rotation.x, transform.rotation.y + direction, transform.rotation.z));
		//	//transform.rotation = Quaternion.AngleAxis(transform.rotation.y + direction, Vector3.up);
			
		//}

  //      private int nearestAngleIndex(float angle, float[] arrayOfAngles)
		//{
		//	for (int i = 0; i < arrayOfAngles.Length; i++)
		//	{
		//		if(Math.Abs(angle - arrayOfAngles[i]) < 2.0f)
		//		{
		//			return i;
		//		}
		//	}

		//	return 0;
		//}

  //      private int CurrentTurnAngleIndex()
		//{
		//	return nearestAngleIndex(Quaternion.LookRotation (transform.forward).eulerAngles.y, TurnAngles);
		//}

  //      public void TurnLeft()
		//{
		//	if(CheckIfAgentCanTurn(-90))
		//	{
		//		int index = CurrentTurnAngleIndex() - 1;
  //              if (index < 0)
  //              {
  //                  index = TurnAngles.Length - 1;
  //              }

  //              float targetRotation = TurnAngles[index];
  //              transform.rotation = Quaternion.Euler(new Vector3(0.0f, targetRotation, 0.0f));
		//	}
		//}

  //      public void TurnRight()
		//{
		//	if(CheckIfAgentCanTurn(90))
		//	{
		//		int index = CurrentTurnAngleIndex() + 1;
  //              if (index == TurnAngles.Length)
  //              {
  //                  index = 0;
  //              }

  //              float targetRotation = TurnAngles[index];
  //              transform.rotation = Quaternion.Euler(new Vector3(0.0f, targetRotation, 0.0f));
		//	}
		//}

  //      public bool CheckIfAgentCanTurn(int direction)
		//{
		//	bool result = false;

		//	if (ItemInHand == null)
  //          {
  //              Debug.Log("Rotation check passed: nothing in Agent Hand");
  //              return true;
  //          }

		//	if (direction != 90 && direction != -90)
  //          {
  //              Debug.Log("Please give -90(left) or 90(right) as direction parameter");
  //              return false;
  //          }

		//	//zero out the pivot and default to hand's current position
		//	TurnSweepTestPivot.transform.localRotation = Quaternion.Euler(Vector3.zero);
  // 			TurnSweepPosition.transform.position = AgentHand.transform.position;         


		//	TurnSweepTestPivot.transform.localRotation = Quaternion.Euler(new Vector3(0, direction, 0));
            
		//	RaycastHit hit;
            
		//	Rigidbody ItemRB = ItemInHand.GetComponent<Rigidbody>();

		//	//check if the sweep hits anything at all
		//	if (ItemRB.SweepTest(TurnSweepPosition.transform.position - AgentHand.transform.position, out hit,
		//						 Vector3.Distance(TurnSweepPosition.transform.position, AgentHand.transform.position),
		//						 QueryTriggerInteraction.Ignore))
		//	{
		//		//print(hit.transform.name);
		//		//If the thing hit was anything except the object itself, the agent, or the agent's hand - it's blocking
		//		if (hit.transform != AgentHand.transform && hit.transform != gameObject.transform && hit.transform != ItemInHand.transform)
		//		{
		//			if (direction == 90)
		//			{
		//				Debug.Log(hit.transform.name + " is in Agent Hand's Path! Can't rotate Agent RIGHT");
		//				result = false;
		//			}

		//			else
		//			{
		//				Debug.Log(hit.transform.name + " is in Agent Hand's Path! Can't rotate Agent LEFT");
		//				result = false;
		//			}
		//		} 

		//		else
  //              {
  //                  //the sweep hit something that it is ok to hit (agent itself, agent's hand, object itself somehow)
  //                  result = true;
  //              }
		//	}

		//	else
  //          {
  //              //oh we didn't hit anything, good to go
  //              result = true;
  //          }
           
		//	return result;         
		//}

  //      public void LookUp()
		//{
		//	if(LookAngleIndex < LookAngles.Length - 1)
		//	{
		//		//look up here, iterate to next upward angle
		//		float targetAngle = LookAngles[LookAngleIndex + 1];
		//		//print("looking up " + targetAngle);

		//		if(CheckIfAgentCanLook(targetAngle))
		//		{
		//			m_Camera.transform.localRotation = Quaternion.AngleAxis(targetAngle, Vector3.right);
  //                  LookAngleIndex++;
		//		}

		//	}

		//	else
		//	{
		//		Debug.Log("Can't LookUp() beyond maximum angle!");
		//	}
		//}

  //      public void LookDown()
		//{
		//	if(LookAngleIndex > 0)
		//	{
		//		float targetAngle = LookAngles[LookAngleIndex - 1];
		//		//print("looking down " + targetAngle);
		//		if(CheckIfAgentCanLook(targetAngle))
		//		{
		//			m_Camera.transform.localRotation = Quaternion.AngleAxis(targetAngle, Vector3.right);
  //                  LookAngleIndex--;
		//		}            
		//	}

		//	else
		//	{
		//		Debug.Log("Can't LookDown() below minimum angle!");
		//	}
		//}

  //      public bool CheckIfAgentCanLook(float targetAngle)
		//{
		//	//print(targetAngle);
		//	if (ItemInHand == null)
  //          {
  //              Debug.Log("Look check passed: nothing in Agent Hand to prevent Angle change");
  //              return true;
  //          }

  //          //returns true if Rotation is allowed
  //          bool result = false;

		//	//zero out the pivot and default to hand's current position AND rotation
		//	LookSweepTestPivot.transform.localRotation = m_Camera.transform.localRotation;
		//	LookSweepPosition.transform.position = AgentHand.transform.position;

		//	//rotate pivot to target location, then sweep for obstacles
		//	LookSweepTestPivot.transform.localRotation = Quaternion.AngleAxis(targetAngle, Vector3.right);
		//	//RotationSweepTestPivot.transform.localRotation = Quaternion.Euler(new Vector3(targetAngle, 0, 0));
            
		//	RaycastHit hit;

  //          Rigidbody ItemRB = ItemInHand.GetComponent<Rigidbody>();

  //          //check if the sweep hits anything at all
  //          if (ItemRB.SweepTest(LookSweepPosition.transform.position - AgentHand.transform.position, out hit,
  //                               Vector3.Distance(LookSweepPosition.transform.position, AgentHand.transform.position),
  //                               QueryTriggerInteraction.Ignore))
  //          {
  //              //If the thing hit was anything except the object itself, the agent, or the agent's hand - it's blocking
		//		if (hit.transform != AgentHand.transform && hit.transform != ItemInHand.transform && hit.transform != gameObject.transform) //&& hit.transform != gameObject.transform
  //              {
		//			Debug.Log("Can't change view to " + targetAngle + ", " + hit.transform.name + "is blocking the way");
		//			result = false;
  //              }

  //              else
  //              {
  //                  //the sweep hit something that it is ok to hit (agent itself, agent's hand, object itself somehow)
  //                  result = true;

  //              }

  //          }

  //          else
  //          {
  //              //oh we didn't hit anything, good to go
  //              result = true;
  //          }
         
  //          return result;
		//}
        
        
  //      //If an object is in the agent's hand, sweeptest desired move distance to check for blocking objects
		//public bool CheckIfHandBlocksAgentMovement(float moveMagnitude, string direction)
		//{
		//	bool result = false;

  //          //if there is nothing in our hand, we are good, return!
		//	if(ItemInHand == null)
		//	{
		//		result = true;
		//		Debug.Log("Agent has nothing in hand blocking movement");
		//		return result;
  // 			}

  //          //otherwise we are holding an object and need to do a sweep using that object's rb
		//	else
		//	{
		//		Vector3 dir = new Vector3();

  //              //use the agent's forward as reference
		//		switch (direction)
  //              {
  //                  case "forward":
  //                      dir = gameObject.transform.forward;
  //                      break;

  //                  case "left":
  //                      dir = -gameObject.transform.right;
  //                      break;

  //                  case "right":
  //                      dir = gameObject.transform.right;
  //                      break;

  //                  default:
  //                      Debug.Log("Incorrect direction input! Allowed Directions: forward, left, right");
  //                      break;
  //              }
		//		//otherwise we haev an item in our hand, so sweep using it's rigid body.
  //              RaycastHit hit;
                
  //              Rigidbody rb = ItemInHand.GetComponent<Rigidbody>();
		//		if(rb.SweepTest(dir, out hit, moveMagnitude, QueryTriggerInteraction.Ignore))
		//		{
		//			Debug.Log(hit.transform.name + " is blocking Agent Hand holding " + ItemInHand.name + " from moving " + direction);
  //                  result = false;
		//		}
		//		//nothing was hit, we good
  //              else
  //              {
		//			Debug.Log("Agent hand holding " + ItemInHand.name + " can move " + direction + " " + moveMagnitude + " units");
		//			result = true;
  //              }
		//	}
         
		//	return result;
		//}
              
  //      //
  //      public bool CheckIfAgentCanMove(float moveMagnitude, string direction)
		//{
		//	bool result = false;
		//	//RaycastHit hit;

		//	Vector3 dir = new Vector3();

  //          switch(direction)
		//	{
		//		case "forward":
		//			dir = gameObject.transform.forward;
		//			break;

		//		case "backward":
		//			dir = -gameObject.transform.forward;
		//			break;

		//		case "left":
		//			dir = -gameObject.transform.right;
		//			break;

		//		case "right":
		//			dir = gameObject.transform.right;
		//			break;
                
		//		default:
		//			Debug.Log("Incorrect direction input! Allowed Directions: forward, backward, left, right");
		//			break;
		//	}

		//	Rigidbody rb = gameObject.GetComponent<Rigidbody>();

  //          //might need to sweep test all, check for static..... want to be able to try and move through sim objects that can pickup and move yes
		//	RaycastHit[] sweepResults = rb.SweepTestAll(dir, moveMagnitude, QueryTriggerInteraction.Ignore);

  //          //check if we hit an environmental structure or a sim object that we aren't actively holding. If so we can't move
		//	if(sweepResults.Length > 0)
		//	{
		//		foreach(RaycastHit res in sweepResults)
  //              {
  //                  //if(res.transform.tag == "Structure")
  //                  //{
  //                  //    print("hit a structure");
  //                  //    result = false;
  //                  //    Debug.Log(res.transform.name + " is blocking the Agent from moving " + direction);
  //                  //    return result;
  //                  //}

  //                  //nothing in our hand, so nothing to ignore
  //                  if(ItemInHand == null)
  //                  {
  //                      if(res.transform.GetComponent<SimObjPhysics>())
  //                      {
  //                          result = false;
  //                          Debug.Log(res.transform.name + " is blocking the Agent from moving " + direction);
		//					break;
  //                      }
                  
  //                  }               
  //                  //oh if there is something in our hand, ignore it if that is what we hit
  //                  if(ItemInHand != null)
  //                  {
  //                      if(ItemInHand.transform == res.transform)
  //                      {
		//					result = true;
  //                          break;
  //                      }
  //                  }

		//			Debug.Log(res.transform.name + " is blocking the Agent from moving " + direction);

  //              }
		//	}


  //          //if the array is empty, nothing was hit by the sweeptest so we are clear to move
		//	else
		//	{
		//		Debug.Log("Agent Body can move " + direction);
  //              result = true;	
		//	}

  //          return result;
		//}
        
        //returns true if the Hand Movement was succesful
        //false if blocked by something or out of range
  //      public bool MoveHand(Vector3 targetPosition)
  //      {
		//	bool result = false;

		//	//can only move hand if there is an object in it.
		//	if(ItemInHand == null)
		//	{
		//		Debug.Log("Agent can only move hand if holding an item");
		//		result = false;
		//		return result;
		//	}
		//	//result if movement was succesful or not

         
		//	//first check if passed in targetPosition is in range or not           
		//	if(Vector3.Distance(gameObject.transform.position, targetPosition) > MaxDistance + 0.3)
		//	{           
		//		Debug.Log("The target position is out of range");
		//		result = false;
		//		return result;
		//	}

		//	//get viewport point of target position
  //          Vector3 vp = m_Camera.WorldToViewportPoint(targetPosition);

  //          //Note: Viewport normalizes to (0,0) bottom left, (1, 0) top right of screen
  //          //now make sure the targetPosition is actually within the Camera Bounds       
		//	if (vp.z < 0 || vp.x > 1.0f || vp.x < 0.0f || vp.y > 1.0f || vp.y < 0.0f)
  //          {
		//		Debug.Log("The target position is not in the Agent's Viewport!");
		//		result = false;
		//		return result;
  //          }
         
		//	Rigidbody ItemRB = ItemInHand.GetComponent<Rigidbody>();
		//	RaycastHit hit;

  //          //put the Hand position update inside this, soince the object will always hit the agent Hand once, which we ignore
  //          if(ItemRB.SweepTest(targetPosition - AgentHand.transform.position, out hit, Vector3.Distance(targetPosition, AgentHand.transform.position)))
		//	{
		//		//return error if anything but the Agent Hand or the Agent are hit
		//		if(hit.transform != AgentHand.transform && hit.transform != gameObject.transform)
		//		{
		//		    Debug.Log(hit.transform.name + " is in Object In Hand's Path! Can't Move Hand holding " + ItemInHand.name);
		//			result = false;
		//		}
            
		//		else
  //              {
		//			Debug.Log("Movement of Agent Hand holding " + ItemInHand.name + " succesful!");
  //                  AgentHand.transform.position = targetPosition;
		//		    IsHandDefault = false;
		//			result = true;
  //              }
		//	}

		//	else
  //          {
  //              AgentHand.transform.position = targetPosition;
		//		IsHandDefault = false;            
		//		result = true;
  //          }

		//return result;
         
        //}
        
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

  //      public void ResetAgentHandPosition()
		//{
		//	AgentHand.transform.position = DefaultHandPosition.position;
		//}

		//public void ResetAgentHandRotation()
		//{
		//	AgentHand.transform.localRotation = Quaternion.Euler(Vector3.zero);
		//}

		//public void DefaultAgentHand()
		//{
		//	ResetAgentHandPosition();
		//	ResetAgentHandRotation();
		//	IsHandDefault = true;
		//}

  //      //pickup a sim object
  //      //hand must be in defualt position, then does a sweep to see if the hand can get to the object's interaction point
  //      public bool PickUpSimObjPhysics(Transform target)
  //      {
		//	if(target.GetComponent<SimObjPhysics>().PrimaryProperty!= SimObjPrimaryProperty.CanPickup)
		//	{
		//		Debug.Log("Only SimObjPhysics that have the property CanPickup can be picked up");
		//		return false;
		//	}
  //          //make sure hand is empty, turn off the target object's collision and physics properties
  //          //and make the object kinematic
  //          if (ItemInHand == null)
  //          {
		//		if(IsHandDefault == false)
		//		{
		//			Debug.Log("Reset Hand to default position before attempting to Pick Up objects");
		//			return false;
		//		}

		//		//default hand rotation for further rotation manipulation
		//		ResetAgentHandRotation();
  //              //move the object to the hand's default position.
  //              target.GetComponent<Rigidbody>().isKinematic = true;
  //              //target.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;
  //              target.position = AgentHand.transform.position;
		//		//AgentHand.transform.parent = target;
		//		target.SetParent(AgentHand.transform);
  //              //target.parent = AgentHand.transform;
  //              //update "inventory"

  //              //this is only in debug mode - probs delete this part when porting to BaseFPSAgent
                
  //              ItemInHand = target.gameObject;
		//		Text txt = Inventory_Text.GetComponent<Text>();
		//		txt.text = "In Inventory: " + target.name + " " + target.GetComponent<SimObjPhysics>().UniqueID;

  //              /////////////////

		//		return true;
  //          }

  //          else
		//	{
		//		Debug.Log("Your hand has something in it already!");
		//		return false;
		//	}
         
  //      }
        
  //      public bool DropSimObjPhysics()
		//{
		//	//make sure something is actually in our hands
		//	if (ItemInHand != null)
		//	{

		//		if(ItemInHand.GetComponent<SimObjPhysics>().isColliding)
		//		{
		//			Debug.Log(ItemInHand.transform.name + " can't be dropped. It must be clear of all other objects first");
		//			return false;
		//		}

		//		else
		//		{
		//			ItemInHand.GetComponent<Rigidbody>().isKinematic = false;
  //                  ItemInHand.transform.parent = null;
  //                  ItemInHand = null;

  //                  //take this out later when moving to BaseFPS agent controller
  //                  Text txt = Inventory_Text.GetComponent<Text>();
  //                  txt.text = "In Inventory: Nothing!";
  //                  ///////

  //                  return true;
		//		}

		//	}

		//	else
		//	{
		//		Debug.Log("nothing in hand to drop!");
		//		return false;
		//	}

		//}
        
  //      //used by RotateSimObjPhysicsInHand for compound collider object comparison
  //      private bool CheckForMatches(IEnumerable<Transform> objects, Transform toCompare )
		//{
		//	foreach (Transform t in objects)
		//	{
		//		if(toCompare == t)
		//		{
		//			return true;
		//		}
		//	}     
		//	return false;
		//}

   //     public bool RotateSimObjPhysicsInHand(Vector3 vec)
   //     {
			////based on the collider type of the item in the Agent's Hand, set the radius of the OverlapSphere to check if there is room for rotation
     //       if(ItemInHand != null)
     //       {
     //           //for items that use box colliders
     //           if(ItemInHand.GetComponent<BoxCollider>())
     //           {
     //               Vector3 sizeOfBox = ItemInHand.GetComponent<BoxCollider>().size;
					////do an overlapshere around the agent with radius based on max size of xyz of object in hand's collider

     //               //find the radius of the overlap sphere based on max length of dimensions of box collider
					//float overlapRadius = Math.Max(Math.Max(sizeOfBox.x, sizeOfBox.y), sizeOfBox.z) / 2;
     //					//since the sim objects have wonky scales, find the percent increase or decrease to multiply the radius by to match the scale of the sim object
					//Vector3 itemInHandScale = ItemInHand.transform.lossyScale;
					////take the average of each axis scale, even though they should all be THE SAME but just in case
					//float avgScale = (itemInHandScale.x + itemInHandScale.y + itemInHandScale.z) / 3;
     //               //adjust radius according to scale of item in hand
					//overlapRadius = overlapRadius * avgScale;

     //               Collider[] hitColliders = Physics.OverlapSphere(AgentHand.transform.position, 
					//                                                overlapRadius);
               
     //               //for objects that might have compound colliders, make sure we track them here for comparison below
					////NOTE: Make sure any objects with compound colliders have an "isTrigger" Collider on the highest object in the 
					////Heirarchy. The check for "Box" or "Sphere" Collider will use that trigger collider for radius calculations, since
     //               //getting the dimensions of a compound collider wouldn't make any sense due to irregular shapes
					//Transform[] anyChildren = ItemInHand.GetComponentsInChildren<Transform>();
               
     //               foreach(Collider col in hitColliders)
     //               {
     //                   //check if the thing collided with by the OverlapSphere is the agent, the hand, or the object itself
     //                   if(col.name != "TextInputModeler" && col.name != "TheHand" && col.name != ItemInHand.name)
     //                   {
					//		//also check against any children the ItemInHand has for prefabs with compound colliders                     
					//		//set to true if there is a match between this collider among ANY of the children of ItemInHand
                            
					//		if(CheckForMatches(anyChildren, col.transform) == false)
					//		{
					//			Debug.Log(col.name + " blocking rotation");
     //                           Debug.Log("Not Enough Room to Rotate");
     //                           return false;
					//		}
                     
     //                   }
                  
					//	else
					//	{
					//		AgentHand.transform.localRotation = Quaternion.Euler(vec);
					//		return true;
					//	}
     //               }               
     //           }


     //           //for items with sphere collider
     //           if (ItemInHand.GetComponent<SphereCollider>())
     //           {
     //               float radiusOfSphere = ItemInHand.GetComponent<SphereCollider>().radius;

					//Vector3 itemInHandScale = ItemInHand.transform.lossyScale;

					//float avgScale = (itemInHandScale.x + itemInHandScale.y + itemInHandScale.z) / 3;

					//radiusOfSphere = radiusOfSphere * avgScale;

      //              Collider[] hitColliders = Physics.OverlapSphere(AgentHand.transform.position, radiusOfSphere);

      //              foreach (Collider col in hitColliders)
      //              {
      //                  //print(col.name);
      //                  if (col.name != "TextInputModeler" && col.name != "TheHand" && col.name != ItemInHand.name)
      //                  {
      //                      Debug.Log("Not Enough Room to Rotate");
      //                      return false;
      //                  }

						//else
						//{
						//	AgentHand.transform.localRotation = Quaternion.Euler(vec);
						//	return true;
						//}
        //            }
               
        //        }            
        //    }
         
        //    //if nothing is in your hand, nothing to rotate so don't!
        //    Debug.Log("Nothing In Hand to rotate!");
        //    return false;

        //}

		//#if UNITY_EDITOR
        ////used to show what's currently visible on the top left of the screen
        //void OnGUI()
        //{
        //    if (VisibleObjects != null)
        //    {
        //        if (VisibleObjects.Length > 10)
        //        {
        //            int horzIndex = -1;
        //            GUILayout.BeginHorizontal();
        //            foreach (SimObjPhysics o in VisibleObjects)
        //            {
        //                horzIndex++;
        //                if (horzIndex >= 3)
        //                {
        //                    GUILayout.EndHorizontal();
        //                    GUILayout.BeginHorizontal();
        //                    horzIndex = 0;
        //                }
        //                GUILayout.Button(o.UniqueID, UnityEditor.EditorStyles.miniButton, GUILayout.MaxWidth(200f));
        //            }

        //            GUILayout.EndHorizontal();
        //        }

        //        else
        //        {
        //            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(m_Camera);

        //            //int position_number = 0;
        //            foreach (SimObjPhysics o in VisibleObjects)
        //            {
        //                string suffix = "";
        //                Bounds bounds = new Bounds(o.gameObject.transform.position, new Vector3(0.05f, 0.05f, 0.05f));
        //                if (GeometryUtility.TestPlanesAABB(planes, bounds))
        //                {
        //                    //position_number += 1;

        //                    //if (o.GetComponent<SimObj>().Manipulation == SimObjManipProperty.Inventory)
        //                    //    suffix += " VISIBLE: " + "Press '" + position_number + "' to pick up";

        //                    //else
        //                    //suffix += " VISIBLE";
        //                    if (o.isInteractable == true)
        //                    {
        //                        suffix += " INTERACTABLE";
        //                    }
        //                }


        //                GUILayout.Button(o.UniqueID + suffix, UnityEditor.EditorStyles.miniButton, GUILayout.MinWidth(100f));
        //            }
        //        }
        //    }
        //}
        //#endif

        private void DebugKeyboardControls()
		{
			//swap between text input and not
			if (Input.GetKeyDown(KeyCode.BackQuote))
            {
				//Switch to Text Mode
                if (TextInputMode == false)
                {
					InputMode_Text.GetComponent<Text>().text = "Text Input Mode";
                    TextInputMode = true;
                    return;
                }

                //Switch to Mouse and Keyboard Mode
                if (TextInputMode == true)
                {               
					InputMode_Text.GetComponent<Text>().text = "Free Mode";
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

       //         if (Input.GetMouseButtonDown(0))
       //         {
       //             RaycastHit hit;
       //             Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
       //             //shoot a ray out to select an object
       //             if (Physics.Raycast(ray, out hit))
       //             {
       //                 //check if the hit object is a SimObj in our array of Accessible sim objects
       //                 if (hit.transform.tag == "SimObjPhysics")
       //                 {
							////print(hit.transform.name);
                //            //if an interaction point is accessible by the hand, proceed to try and pick it up
                //            if (hit.transform.GetComponent<SimObjPhysics>().isInteractable == true)
                //            {
                //                //print(hit.transform.name + " is pickupable!");

                //                //pickup the object here
                //                PickUpSimObjPhysics(hit.transform);

                //            }
                //        }
                //    }
                //}


                ////on right mouse click
                //if (Input.GetMouseButtonDown(1))
                //{
                //    //DropSimObjPhysics();
                //}

                if (Input.GetKeyDown(KeyCode.E))
                {
                    //MoveHand(ScreenPointMoveHand(0.1f));

                    //MoveHand(TestObject.transform.position);
                    //print(TestObject.transform.position);
                }

                if (Input.GetKeyDown(KeyCode.R))
                {
                    //MoveHand(ScreenPointMoveHand(0.3f));
                }

                //default position and rotation
                if (Input.GetKeyDown(KeyCode.Q))
                {
                    //DefaultAgentHand();
                }

                //Rotate tests for objects in agent hand
                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    //RotateSimObjPhysicsInHand(new Vector3(0, 0, 0));
                }

                if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    //RotateSimObjPhysicsInHand(new Vector3(180, 0, 0));
                }

                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    //RotateSimObjPhysicsInHand(new Vector3(0, 0, 90));
                }

                if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    //RotateSimObjPhysicsInHand(new Vector3(0, 0, -90));
                }

            }
		}

		private void Update()	
        {
			//constantly check for visible objects in front of agent
			//VisibleObjects = GetAllVisibleSimObjPhysics(m_Camera, MaxDistance);

			DebugKeyboardControls();
         
            ///////////////////////////////////////////////////////////////////////////
			//we are not in focus mode, so use WASD and mouse to move around
			if(TextInputMode == false)
			{
				//this is the mouselook in first person mode

				FPSInput();

				if(Cursor.visible == false)
				{
					//accept input to update view based on mouse input
					MouseRotateView();
				}
			}

            //we are in focus mode, this should be the default - can toggle fps control from here
            //by default we can only use enter to execute commands in the text field
			if(TextInputMode == true)
			{
				
			}
	
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
            
		}

		private void MouseRotateView()
		{
   			m_MouseLook.LookRotation (transform, m_Camera.transform);         
		}

        private void FPSInput()
		{                  
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
            m_MoveDir += Physics.gravity * m_GravityMultiplier * Time.fixedDeltaTime;         
            m_CharacterController.Move(m_MoveDir * Time.fixedDeltaTime);
		}

  
	}
}

