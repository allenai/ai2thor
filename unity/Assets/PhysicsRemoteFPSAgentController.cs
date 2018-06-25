// Copyright Allen Institute for Artificial Intelligence 2017

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using System.Globalization;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace UnityStandardAssets.Characters.FirstPerson
{
	[RequireComponent(typeof (CharacterController))]   
	public class PhysicsRemoteFPSAgentController : BaseFPSAgentController
    {
		[SerializeField] protected GameObject[] ToSetActive = null;

		[SerializeField] protected float MaxViewDistancePhysics = 1.7f; //change MaxVisibleDistance of BaseAgent to this value to account for Physics
		[SerializeField] protected float PhysicsAgentSkinWidth = 0.04f; //change agent's skin width so that it collides directly with ground - otherwise sweeptests will fail for flat objects on floor

		[SerializeField] protected GameObject AgentHand = null;
		[SerializeField] protected GameObject DefaultHandPosition = null;
        [SerializeField] protected GameObject ItemInHand = null;//current object in inventory

		////for turning and look Sweeptests
		//[SerializeField] protected GameObject LookSweepPosition = null;
		//[SerializeField] protected GameObject LookSweepTestPivot = null; //if the Camera position ever moves, make sure this is set to the same local position as FirstPersonCharacter

        [SerializeField] protected GameObject[] RotateRLPivots = null;
		[SerializeField] protected GameObject[] RotateRLTriggerBoxes = null;

		[SerializeField] protected GameObject[] LookUDPivots = null;
		[SerializeField] protected GameObject[] LookUDTriggerBoxes = null;

        
		[SerializeField] protected SimObjPhysics[] VisibleSimObjPhysics; //all SimObjPhysics that are within camera viewport and range dictated by MaxViewDistancePhysics

		[SerializeField] public bool IsHandDefault = true;

        // Use this for initialization
        protected override void Start()
        {
			base.Start();

			ServerAction action = new ServerAction();
			Initialize(action);
			//enable all the GameObjects on the Agent that Physics Mode requires

            //physics requires max distance to be extended to be able to see objects on ground
			maxVisibleDistance = MaxViewDistancePhysics;
			gameObject.GetComponent<CharacterController>().skinWidth = PhysicsAgentSkinWidth;

			foreach (GameObject go in ToSetActive)
			{
				go.SetActive(true);
			}

			//On start, activate gravity
            Vector3 movement = Vector3.zero;
            movement.y = Physics.gravity.y * m_GravityMultiplier;
            m_CharacterController.Move(movement);
        }

  //      public void DebugInitialize(Vector3 pos)
		//{
		//	lastPosition = pos;
		//}
		public GameObject WhatAmIHolding()
		{
			return ItemInHand;
		}

        // Update is called once per frame
        void Update()
        {
			
        }

		private void LateUpdate()
		{
			//make sure this happens in late update so all physics related checks are done ahead of time
			VisibleSimObjPhysics = GetAllVisibleSimObjPhysics(m_Camera, maxVisibleDistance);
   		}

        public string UniqueIDOfClosestInteractableObject()
		{
			string objectID = null;

			foreach (SimObjPhysics o in VisibleSimObjPhysics)
			{
				if(o.isInteractable == true)
				{
					objectID = o.UniqueID;
				//	print(objectID);
					break;
				}
			}

			return objectID;
		}

		protected SimObjPhysics[] GetAllVisibleSimObjPhysics(Camera agentCamera, float maxDistance)
        {
            List<SimObjPhysics> currentlyVisibleItems = new List<SimObjPhysics>();

            Vector3 agentCameraPos = agentCamera.transform.position;

            //get all sim objects in range around us
            Collider[] colliders_in_view = Physics.OverlapSphere(agentCameraPos, maxDistance,
                                                         1 << 8, QueryTriggerInteraction.Collide); //layermask is 8

            if (colliders_in_view != null)
            {
                foreach (Collider item in colliders_in_view)
                {
                    if (item.tag == "SimObjPhysics")
                    {
                        SimObjPhysics sop;

                        //if the object has no compound trigger colliders
                        if (item.GetComponent<SimObjPhysics>())
                        {
                            sop = item.GetComponent<SimObjPhysics>();
                        }

                        //if the object does have compound trigger colliders, get the SimObjPhysics component from the parent
                        else
                        {
                            sop = item.GetComponentInParent<SimObjPhysics>();
                        }

						if(sop)
						{
							if (sop.VisibilityPoints.Length > 0)
                            {
                                Transform[] visPoints = sop.VisibilityPoints;
                                int visPointCount = 0;

                                foreach (Transform point in visPoints)
                                {
                                    //if this particular point is in view...
                                    if (CheckIfVisibilityPointInViewport(sop, point, agentCamera, maxDistance))
                                    {
                                        visPointCount++;
                                    }
                                }

                                //if we see at least one vis point, the object is "visible"
                                if (visPointCount > 0)
                                {
                                    sop.isVisible = true;
                                    if (!currentlyVisibleItems.Contains(sop))
                                        currentlyVisibleItems.Add(sop);
                                }
                            }

                            else
                                Debug.Log("Error! Set at least 1 visibility point on SimObjPhysics prefab!");                     
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
						Vector3 viewPoint = agentCamera.WorldToViewportPoint(ip.position);

                        float ViewPointRangeHigh = 1.0f;
                        float ViewPointRangeLow = 0.0f;

                        //check if the interaction point is within the viewport
                        if (viewPoint.z > 0 && viewPoint.z < maxDistance //is in front of camera and within range of visibility sphere
                            && viewPoint.x < ViewPointRangeHigh && viewPoint.x > ViewPointRangeLow//within x bounds of viewport
                            && viewPoint.y < ViewPointRangeHigh && viewPoint.y > ViewPointRangeLow)//within y bounds of viewport
						{                        
							//sweep test from agent's hand to each Interaction point
                            RaycastHit hit;
                            if (HandRB.SweepTest(ip.position - AgentHand.transform.position, out hit, maxDistance))
                            {
                                //if the object only has one interaction point to check
                                if (visibleSimObjP.InteractionPoints.Length == 1)
                                {
                                    if (hit.transform == visibleSimObjP.transform)
                                    {
                                        #if UNITY_EDITOR
                                        Debug.DrawLine(AgentHand.transform.position, ip.transform.position, Color.magenta);
                                        #endif

                                        //print(hit.transform.name);
                                        visibleSimObjP.isInteractable = true;
                                    }

                                    else
                                        visibleSimObjP.isInteractable = false;
                                }

                                //this object has 2 or more interaction points
                                //if any one of them can be accessed by the Agent's hand, this object is interactable
                                if (visibleSimObjP.InteractionPoints.Length > 1)
                                {

                                    if (hit.transform == visibleSimObjP.transform)
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

							else
                                visibleSimObjP.isInteractable = false;
						}                    
                    }
                }
            }
            
            //populate array of visible items in order by distance
            currentlyVisibleItems.Sort((x, y) => Vector3.Distance(x.transform.position, agentCameraPos).CompareTo(Vector3.Distance(y.transform.position, agentCameraPos)));
            return currentlyVisibleItems.ToArray();
        }

        
		protected bool CheckIfVisibilityPointInViewport(SimObjPhysics sop, Transform point, Camera agentCamera, float maxDistance)
        {
            bool result = false;

            Vector3 viewPoint = agentCamera.WorldToViewportPoint(point.position);

            float ViewPointRangeHigh = 1.0f;
            float ViewPointRangeLow = 0.0f;
            
            if (viewPoint.z > 0 && viewPoint.z < maxDistance //is in front of camera and within range of visibility sphere
                && viewPoint.x < ViewPointRangeHigh && viewPoint.x > ViewPointRangeLow//within x bounds of viewport
                && viewPoint.y < ViewPointRangeHigh && viewPoint.y > ViewPointRangeLow)//within y bounds of viewport
            {
                
				//now cast a ray out toward the point, if anything occludes this point, that point is not visible
				RaycastHit hit;
				if(Physics.Raycast(agentCamera.transform.position, point.position - agentCamera.transform.position, out hit, 
                   Vector3.Distance(point.position,agentCamera.transform.position), 1<<8))//layer mask automatically excludes Agent from this check
				{
					//print(hit.transform.name);
                    if(hit.transform.name != sop.name)
					{
						result = false;
					}

					else
					{
						result = true;
                        #if UNITY_EDITOR
                        Debug.DrawLine(agentCamera.transform.position, point.position, Color.yellow);
                        #endif
					}               
				}          
            }

            else
                result = false;

            return result;

        }

		//old check if in viewport for SimObjPhysics without multiple visibility points
		//public bool CheckIfInViewport(SimObjPhysics item, Camera agentCamera, float maxDistance)
		//{
		//    //return true result if object is within the Viewport, false if not in viewport or the viewport doesn't care about the object
		//    bool result = false;

		//    Vector3 viewPoint = agentCamera.WorldToViewportPoint(item.transform.position);

		//    //move these two up top as serialized variables later, or maybe not? values between 0 and 1 will cause "tunnel vision"
		//    float ViewPointRangeHigh = 1.0f;
		//    float ViewPointRangeLow = 0.0f;

		//    //note: Viewport space normalized as bottom left (0,0) and top right(1, 1)
		//    if (viewPoint.z > 0 && viewPoint.z < maxDistance //is in front of camera and within range of visibility sphere
		//       && viewPoint.x < ViewPointRangeHigh && viewPoint.x > ViewPointRangeLow//within x bounds of viewport
		//        && viewPoint.y < ViewPointRangeHigh && viewPoint.y > ViewPointRangeLow)//within y bounds of viewport
		//    {
		//        result = true;
		//    }

		//    else
		//        result = false;

		//    return result;
		//}

		public override void LookDown(ServerAction response)
		{
			float targetHorizon = 0.0f;

			if (currentHorizonAngleIndex() > 0)
			{
				targetHorizon = horizonAngles[currentHorizonAngleIndex() - 1];
			}

			int down = -1;

			if(CheckIfAgentCanLook(targetHorizon, down))            
			base.LookDown(response);
			SetUpRotationBoxChecks();
		}

		public override void LookUp(ServerAction controlCommand)
		{
			float targetHorizon = 0.0f;

			if (currentHorizonAngleIndex() < horizonAngles.Length - 1)
			{
				targetHorizon = horizonAngles[currentHorizonAngleIndex() + 1];
			}

			int up = 1;

			if(CheckIfAgentCanLook(targetHorizon, up))                        
			base.LookUp(controlCommand);
			SetUpRotationBoxChecks();
		}

		public bool CheckIfAgentCanLook(float targetAngle, int updown)
        {
            //print(targetAngle);
            if (ItemInHand == null)
            {
                //Debug.Log("Look check passed: nothing in Agent Hand to prevent Angle change");
                return true;
            }
            
            //returns true if Rotation is allowed
            bool result = true;

            //check if we can look up without hitting something
            if(updown > 0)
			{
				for (int i = 0; i < 3; i++)
                {
					if (LookUDTriggerBoxes[i].GetComponent<RotationTriggerCheck>().isColliding == true)
                    {
						Debug.Log("Object In way, Can't Look Up");
                        return false;
                    }
                }
			}

            //check if we can look down without hitting something
            if(updown <0)
			{
				for (int i = 3; i < 6; i++)
                {
                    if (LookUDTriggerBoxes[i].GetComponent<RotationTriggerCheck>().isColliding == true)
                    {
						Debug.Log("Object in way, Can't Look down");
                        return false;
                    }
                }
			}
         
            ////zero out the pivot and default to hand's current position AND rotation
            //LookSweepTestPivot.transform.localRotation = m_Camera.transform.localRotation;
            //LookSweepPosition.transform.position = AgentHand.transform.position;

            ////rotate pivot to target location, then sweep for obstacles
            //LookSweepTestPivot.transform.localRotation = Quaternion.AngleAxis(targetAngle, Vector3.right);
            ////RotationSweepTestPivot.transform.localRotation = Quaternion.Euler(new Vector3(targetAngle, 0, 0));
            
            //RaycastHit hit;

            //Rigidbody ItemRB = ItemInHand.GetComponent<Rigidbody>();

            ////check if the sweep hits anything at all
            //if (ItemRB.SweepTest(LookSweepPosition.transform.position - AgentHand.transform.position, out hit,
            //                     Vector3.Distance(LookSweepPosition.transform.position, AgentHand.transform.position),
            //                     QueryTriggerInteraction.Ignore))
            //{
            //    //If the thing hit was anything except the object itself, the agent, or the agent's hand - it's blocking
            //    if (hit.transform != AgentHand.transform && hit.transform != ItemInHand.transform && hit.transform != gameObject.transform) //&& hit.transform != gameObject.transform
            //    {
            //        Debug.Log("Can't change view to " + targetAngle + ", " + hit.transform.name + "is blocking the way");
            //        result = false;
            //    }

            //    else
            //    {
            //        //the sweep hit something that it is ok to hit (agent itself, agent's hand, object itself somehow)
            //        result = true;

            //    }

            //}

            //else
            //{
            //    //oh we didn't hit anything, good to go
            //    result = true;
            //}
         
            return result;
        }
        
        //

		public override void RotateRight(ServerAction controlCommand)
		{
			if(CheckIfAgentCanTurn(90))
			base.RotateRight(controlCommand);
		}

		public override void RotateLeft(ServerAction controlCommand)
		{
			if(CheckIfAgentCanTurn(-90))
			base.RotateLeft(controlCommand);
		}

        //checks if agent is clear to rotate left/right without object in hand hitting anything
		public bool CheckIfAgentCanTurn(int direction)
        {
            bool result = true;

            if (ItemInHand == null)
            {
                //Debug.Log("Rotation check passed: nothing in Agent Hand");
                return true;
            }

            if (direction != 90 && direction != -90)
            {
                Debug.Log("Please give -90(left) or 90(right) as direction parameter");
                return false;
            }

			//if turning right, check first 3 in array (30R, 60R, 90R)
            if(direction > 0)
			{
				for (int i = 0; i < 6; i++)
				{
					if(RotateRLTriggerBoxes[i].GetComponent<RotationTriggerCheck>().isColliding == true)
					{
						Debug.Log("Can't rotate right");
						return false;
					}
				}
			}

			//if turning left, check last 3 in array (30L, 60L, 90L)
			else
			{
				for (int i = 6; i < 11; i++)
                {
                    if (RotateRLTriggerBoxes[i].GetComponent<RotationTriggerCheck>().isColliding == true)
                    {
						Debug.Log("Can't rotate left");
                        return false;
                    }
                }
			}

			//TurnSweepPosition.GetComponent<BoxCollider>().enabled = true;
            
			//zero out the pivot to prep for rotation
		 //   TurnSweepTestPivot.transform.localRotation = Quaternion.Euler(Vector3.zero);


   //         GameObject RotateCol = ItemInHand.GetComponent<SimObjPhysics>().RotateAgentCollider;

   //         //move the sweep position to where the rotation collider of the item in hand is
			//TurnSweepPosition.transform.position = RotateCol.transform.position;//AgentHand.transform.position;
			//TurnSweepPosition.transform.rotation = RotateCol.transform.rotation;
            
			//BoxCollider RotTestBox = TurnSweepPosition.GetComponent<BoxCollider>();

   //         //set the collision checking box on TurnSweepPosition to the same size and center of item in hand's rotation box
			//RotTestBox.center = RotateCol.GetComponent<BoxCollider>().center;
			//RotTestBox.size = RotateCol.GetComponent<BoxCollider>().size;

			////prep to check in 30 degree increments, default right check
			//int thirty = 30;
			//int sixty = 60;

   //         //swap signs for checking left
   //         if(direction < 0)
			//{
			//	thirty *= -1;
			//	sixty *= -1;
			//}

			//bool firstcheck = true;
			//bool secondcheck = true;
			//bool thirdcheck = true;
			////check first 30 degree increment to see if the object in hand would collide with anything
			//TurnSweepTestPivot.transform.localRotation = Quaternion.Euler(new Vector3(0, direction - sixty, 0));
			//if (RotTestBox.GetComponent<RotationTriggerCheck>().isColliding)
			//{
			//	Debug.Log("rotate failed 30 degrees");
			//	firstcheck = false;
			//}
            

   //         //check next 30 degree increment
			//TurnSweepTestPivot.transform.localRotation = Quaternion.Euler(new Vector3(0, direction - thirty, 0));
    //        if (RotTestBox.GetComponent<RotationTriggerCheck>().isColliding)
    //        {
				//Debug.Log("rotate failed 60 degrees");
                
				//secondcheck = false;
            //}

   //         //check final position
   //         TurnSweepTestPivot.transform.localRotation = Quaternion.Euler(new Vector3(0, direction, 0));
			//if (RotTestBox.GetComponent<RotationTriggerCheck>().isColliding)
    //        {
				//Debug.Log("rotate failed 90 degrees");

            //    thirdcheck = false;
            //}
            
            //RaycastHit hit;

            //Rigidbody ItemRB = ItemInHand.GetComponent<Rigidbody>();

            ////check if the sweep hits anything at all
            //if (ItemRB.SweepTest(TurnSweepPosition.transform.position - AgentHand.transform.position, out hit,
            //                     Vector3.Distance(TurnSweepPosition.transform.position, AgentHand.transform.position),
            //                     QueryTriggerInteraction.Ignore))
            //{
            //    //print(hit.transform.name);
            //    //If the thing hit was anything except the object itself, the agent, or the agent's hand - it's blocking
            //    if (hit.transform != AgentHand.transform && hit.transform != gameObject.transform && hit.transform != ItemInHand.transform)
            //    {
            //        if (direction == 90)
            //        {
            //            Debug.Log(hit.transform.name + " is in Agent Hand's Path! Can't rotate Agent RIGHT");
            //            result = false;
            //        }

            //        else
            //        {
            //            Debug.Log(hit.transform.name + " is in Agent Hand's Path! Can't rotate Agent LEFT");
            //            result = false;
            //        }
            //    }

            //    else
            //    {
            //        //the sweep hit something that it is ok to hit (agent itself, agent's hand, object itself somehow)
            //        result = true;
            //    }
            //}

            //else
            //{
            //    //oh we didn't hit anything, good to go
            //    result = true;
            //}
			//TurnSweepPosition.GetComponent<BoxCollider>().enabled = false;
			//if(firstcheck == false || secondcheck == false || thirdcheck == false)
			//{
			//	result = false;
			//}

			//else
			//{
			//	result = true;
			//}

            return result;
        }

        //for all translational movement, check if the item the player is holding will hit anything, or if the agent will hit anything
		public override void MoveLeft(ServerAction action)
		{
			if(CheckIfItemBlocksAgentMovement(action.moveMagnitude, 270) && CheckIfAgentCanMove(action.moveMagnitude, 270))
			base.MoveLeft(action);
		}

		public override void MoveRight(ServerAction action)
		{
			if (CheckIfItemBlocksAgentMovement(action.moveMagnitude, 90) && CheckIfAgentCanMove(action.moveMagnitude, 90))            
			base.MoveRight(action);
		}

		public override void MoveAhead(ServerAction action)
		{
			if (CheckIfItemBlocksAgentMovement(action.moveMagnitude, 0) && CheckIfAgentCanMove(action.moveMagnitude, 0))
			base.MoveAhead(action);
			
		}
        
		public override void MoveBack(ServerAction action)
		{
			if (CheckIfItemBlocksAgentMovement(action.moveMagnitude, 180) && CheckIfAgentCanMove(action.moveMagnitude, 180))
			base.MoveBack(action);
		}

		//Sweeptest to see if the object Agent is holding will prohibit movement
        public bool CheckIfItemBlocksAgentMovement(float moveMagnitude, int orientation)
        {
            bool result = false;

            //if there is nothing in our hand, we are good, return!
            if (ItemInHand == null)
            {
                result = true;
              //  Debug.Log("Agent has nothing in hand blocking movement");
                return result;
            }

            //otherwise we are holding an object and need to do a sweep using that object's rb
            else
            {
                Vector3 dir = new Vector3();

                //use the agent's forward as reference
                switch (orientation)
                {
					case 0: //forward
                        dir = gameObject.transform.forward;
                        break;

                    case 180: //backward
                        dir = -gameObject.transform.forward;
                        break;

                    case 270: //left
                        dir = -gameObject.transform.right;
                        break;

                    case 90: //right
                        dir = gameObject.transform.right;
                        break;

                    default:
						Debug.Log("Incorrect orientation input! Allowed orientations (0 - forward, 90 - right, 180 - backward, 270 - left) ");
                        break;
                }
                //otherwise we haev an item in our hand, so sweep using it's rigid body.
                //RaycastHit hit;

                Rigidbody rb = ItemInHand.GetComponent<Rigidbody>();

				RaycastHit[] sweepResults = rb.SweepTestAll(dir, moveMagnitude, QueryTriggerInteraction.Ignore);
				if(sweepResults.Length > 0)
				{
					foreach (RaycastHit res in sweepResults)
					{
                        //did the item in the hand touch the agent? if so, ignore it's fine
						if (res.transform.tag == "Player")
                        {
                            result = true;
                            break;
                        }

						else
						{
							result = false;
							Debug.Log(res.transform.name + " is blocking the Agent from moving " + orientation + " with " + ItemInHand.name);
							return result;
						}
                                          
					}
				}

				//if the array is empty, nothing was hit by the sweeptest so we are clear to move
                else
                {
                    //Debug.Log("Agent Body can move " + orientation);
                    result = true;
                }

                return result;
            }
        }

        //
        public bool CheckIfAgentCanMove(float moveMagnitude, int orientation)
        {
            bool result = false;
            //RaycastHit hit;

            Vector3 dir = new Vector3();

            switch (orientation)
            {
                case 0: //forward
                    dir = gameObject.transform.forward;
                    break;

                case 180: //backward
                    dir = -gameObject.transform.forward;
                    break;

                case 270: //left
                    dir = -gameObject.transform.right;
                    break;

                case 90: //right
                    dir = gameObject.transform.right;
                    break;

                default:
					Debug.Log("Incorrect orientation input! Allowed orientations (0 - forward, 90 - right, 180 - backward, 270 - left) ");
                    break;
            }

            Rigidbody rb = gameObject.GetComponent<Rigidbody>();
			//print(rb.name);
			//print(dir);
            //might need to sweep test all, check for static..... want to be able to try and move through sim objects that can pickup and move yes
            RaycastHit[] sweepResults = rb.SweepTestAll(dir, moveMagnitude, QueryTriggerInteraction.Ignore);
			//print(sweepResults[0]);
            //check if we hit an environmental structure or a sim object that we aren't actively holding. If so we can't move
            if (sweepResults.Length > 0)
            {
                foreach (RaycastHit res in sweepResults)
                {
                    //if(res.transform.tag == "Structure")
                    //{
                    //    print("hit a structure");
                    //    result = false;
                    //    Debug.Log(res.transform.name + " is blocking the Agent from moving " + direction);
                    //    return result;
                    //}

                    //nothing in our hand, so nothing to ignore
                    if (ItemInHand == null)
                    {
						if (res.transform.GetComponent<SimObjPhysics>() || res.transform.tag == "Structure")
                        {
                            result = false;
                            Debug.Log(res.transform.name + " is blocking the Agent from moving " + orientation);
                            break;
                        }

                    }
                    //oh if there is something in our hand, ignore it if that is what we hit
                    if (ItemInHand != null)
                    {
                        if (ItemInHand.transform == res.transform)
                        {
                            result = true;
                            break;
                        }
                    }

                    //Debug.Log(res.transform.name + " is blocking the Agent from moving " + orientation);

                }
            }
         
            //if the array is empty, nothing was hit by the sweeptest so we are clear to move
            else
            {
                //Debug.Log("Agent Body can move " + orientation);
                result = true;
            }

            return result;
        }
        

        /////AGENT HAND STUFF////

		public void ResetAgentHandPosition(ServerAction action)
        {
            AgentHand.transform.position = DefaultHandPosition.transform.position;
        }

        public void ResetAgentHandRotation(ServerAction action)
        {
            AgentHand.transform.localRotation = Quaternion.Euler(Vector3.zero);
        }
        
        public void DefaultAgentHand(ServerAction action)
        {
            ResetAgentHandPosition(action);
            ResetAgentHandRotation(action);
			SetUpRotationBoxChecks();
            IsHandDefault = true;
        }

        //checks if agent hand can move to a target location. Returns false if any obstructions
        public bool CheckIfAgentCanMoveHand(ServerAction action)
		{
			bool result = false;

            //first check if we have anything in our hand, if not then no reason to move hand
			if (ItemInHand == null)
            {
                Debug.Log("Agent can only move hand if holding an item");
                result = false;
                return result;
            }

			Vector3 targetPosition = new Vector3(action.x, action.y, action.z);
           
			//now check if the target position is within bounds of the Agent's forward (z) view
			if (Vector3.Distance(gameObject.transform.position, targetPosition) > maxVisibleDistance)// + 0.3)
            {
                Debug.Log("The target position is out of range");
                result = false;
                return result;
            }

            //now make sure that the targetPosition is within the Agent's x/y view, restricted by camera
			Vector3 vp = m_Camera.WorldToViewportPoint(targetPosition);

            //Note: Viewport normalizes to (0,0) bottom left, (1, 0) top right of screen
            //now make sure the targetPosition is actually within the Camera Bounds       
            if (vp.z < 0 || vp.x > 1.0f || vp.x < 0.0f || vp.y > 1.0f || vp.y < 0.0f)
            {
                Debug.Log("The target position is not in the Agent's Viewport!");
                result = false;
                return result;
            }

            //ok now actually check if the Agent Hand holding ItemInHand can move to the target position without
            //being obstructed by anything
			Rigidbody ItemRB = ItemInHand.GetComponent<Rigidbody>();

			RaycastHit[] sweepResults = ItemRB.SweepTestAll(targetPosition - AgentHand.transform.position, 
			                                                Vector3.Distance(targetPosition, AgentHand.transform.position),
															QueryTriggerInteraction.Ignore);

            //did we hit anything?
			if (sweepResults.Length > 0)
			{

				foreach (RaycastHit hit in sweepResults)
				{
					//hit the player? it's cool, no problem
					if (hit.transform.tag == "Player")
                    {
                        result = true;
						break;
                    }

                    //oh we hit something else? oh boy, that's blocking!
                    else
                    {
                        //  print("sweep didn't hit anything?");
                        Debug.Log(hit.transform.name + " is in Object In Hand's Path! Can't Move Hand holding " + ItemInHand.name);
                        result = false;
						return result;
                    }
				}

			}

            //didnt hit anything in sweep, we are good to go
			else
			{
				result = true;
			}

			return result;
		}


        //moves hand to the x, y, z coordinate, not constrained by any axis, if within range
        public void MoveHand(ServerAction action) //uses server action.x,y,z to create target position
        {
			if(CheckIfAgentCanMoveHand(action))
			{
				Vector3 targetPosition = new Vector3(action.x, action.y, action.z);

				//Debug.Log("Movement of Agent Hand holding " + ItemInHand.name + " succesful!");
                AgentHand.transform.position = targetPosition;
				SetUpRotationBoxChecks();
                IsHandDefault = false;
			}
         
        }

        //moves hand constrained to x, y, z axes a given magnitude
        //pass in x,y,z of 0 if no movement is desired on that axis
        //pass in x,y,z of 1 for positive movement along that axis
        //pass in x,y,z of -1 for negative movement along that axis
        public void MoveHandMagnitude(ServerAction action)
		{         
			Vector3 newPos = AgentHand.transform.position;

			//get new direction relative to Agent's (camera's) forward facing 
            if(action.x > 0)
			{
				newPos = newPos + (m_Camera.transform.right * action.moveMagnitude);    
			}
            
            if (action.x < 0)
			{
				newPos = newPos + (-m_Camera.transform.right * action.moveMagnitude);      
			}

			if(action.y > 0)
			{
				newPos = newPos + (m_Camera.transform.up * action.moveMagnitude);                           
			}

			if (action.y < 0)
            {
				newPos = newPos + (-m_Camera.transform.up * action.moveMagnitude);                
            }

			if (action.z > 0)
            {
				newPos = newPos + (m_Camera.transform.forward * action.moveMagnitude);            
            }

			if (action.z < 0)
            {
				newPos = newPos + (-m_Camera.transform.forward * action.moveMagnitude);    
            }

            ServerAction newAction = new ServerAction();
            newAction.x = newPos.x;
            newAction.y = newPos.y;
            newAction.z = newPos.z;

            MoveHand(newAction);
		}

		public bool IsInArray(Collider collider, GameObject[] arrayOfCol)
		{
			for (int i = 0; i < arrayOfCol.Length; i++)
			{
				if (collider == arrayOfCol[i].GetComponent<Collider>())
					return true;
			}
			return false;
		}

        public bool CheckIfAgentCanRotateHand()
		{
			bool result = false;
                     
            //make sure there is a box collider
			if (ItemInHand.GetComponent<SimObjPhysics>().RotateAgentCollider.GetComponent<BoxCollider>())
			{
				//print("yes yes yes");
				Vector3 sizeOfBox = ItemInHand.GetComponent<SimObjPhysics>().RotateAgentCollider.GetComponent<BoxCollider>().size;
				float overlapRadius = Math.Max(Math.Max(sizeOfBox.x, sizeOfBox.y), sizeOfBox.z);

                //all colliders hit by overlapsphere
                Collider[] hitColliders = Physics.OverlapSphere(AgentHand.transform.position,
                                                                overlapRadius);

                //did we even hit enything?
				if(hitColliders.Length > 0)
				{
					GameObject[] ItemInHandColliders = ItemInHand.GetComponent<SimObjPhysics>().MyColliders;
                    GameObject[] ItemInHandTriggerColliders = ItemInHand.GetComponent<SimObjPhysics>().MyTriggerColliders;

					foreach (Collider col in hitColliders)
                    {
						//check each collider hit

                        //if it's the player, ignore it
                        if(col.tag != "Player")
						{
							if(IsInArray(col, ItemInHandColliders) || IsInArray(col, ItemInHandTriggerColliders))
							{
								result = true;
							}

							else
							{
								Debug.Log(col.name + "  is blocking hand from rotating");
								result = false;
							}
						}
                    }
				}

                //nothing hit by sphere, so we are safe to rotate
				else
				{
					result = true;
				}
			}

			else
			{
				Debug.Log("item in hand is missing a collider box for some reason! Oh nooo!");
			}

			return result;
		}

        //rotat ethe hand if there is an object in it
		public void RotateHand(ServerAction action)
        {

			if(ItemInHand == null)
			{
				Debug.Log("Can't rotate hand unless holding object");
				return;
			}

			if(CheckIfAgentCanRotateHand())
			{
				Vector3 vec = new Vector3(action.x, action.y, action.z);
                AgentHand.transform.localRotation = Quaternion.Euler(vec);
				SetUpRotationBoxChecks();
                actionFinished(true);
			}         
        }
        
		public void PickupObject(ServerAction action)//use serveraction objectid
        {
			if(ItemInHand != null)
			{
				Debug.Log("Agent hand has something in it already! Can't pick up anything else");
                actionFinished(false);
                return;
			}

            //else our hand is empty, commence other checks
			else
			{
				if (IsHandDefault == false)
                {
                    Debug.Log("Reset Hand to default position before attempting to Pick Up objects");
                    actionFinished(false);
                    //return false;
                }

                SimObjPhysics target = null;

                foreach (SimObjPhysics sop in VisibleSimObjPhysics)
                {
                    if (action.objectId == sop.UniqueID)
                    {
                        //print("found it");

                        target = sop;
                    }
                }

                //GameObject target = GameObject.Find(action.objectId);
                if (target == null)
                {
                    Debug.Log("No valid target to pickup");
                    actionFinished(false);
                    return;
                }

                if (!target.GetComponent<SimObjPhysics>())
                {
                    Debug.Log("Target must be SimObjPhysics to pickup");
                    actionFinished(false);
                    return;
                }

                if (target.PrimaryProperty != SimObjPrimaryProperty.CanPickup)
                {
                    Debug.Log("Only SimObjPhysics that have the property CanPickup can be picked up");
                    actionFinished(false);
                    return;
                    //return false;
                }

				if(target.isInteractable != true)
				{
					Debug.Log("Target not in Interactable range of Agent Hand");
                    actionFinished(false);
                    return;
				}
                
                //move the object to the hand's default position.
                target.GetComponent<Rigidbody>().isKinematic = true;
                //target.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;
                target.transform.position = AgentHand.transform.position;
                //AgentHand.transform.parent = target;
                target.transform.SetParent(AgentHand.transform);
                //target.parent = AgentHand.transform;
                //update "inventory"            
                ItemInHand = target.gameObject;

				SetUpRotationBoxChecks();

                //return true;
                actionFinished(true);
                return;
			}      
        }

		public void DropHandObject(ServerAction action)
        {
            //make sure something is actually in our hands
            if (ItemInHand != null)
            {
                if (ItemInHand.GetComponent<SimObjPhysics>().isColliding)
                {
                    Debug.Log(ItemInHand.transform.name + " can't be dropped. It must be clear of all other objects first");
					actionFinished(false);
					return;
                }

                else
                {
                    ItemInHand.GetComponent<Rigidbody>().isKinematic = false;
                    ItemInHand.transform.parent = null;
                    ItemInHand = null;

					ServerAction a = new ServerAction();
					DefaultAgentHand(a);

					actionFinished(true);
					return;
                }
            }

            else
            {
                Debug.Log("nothing in hand to drop!");
				actionFinished(false);
				return;
            }         
        }  

        //x, y, z direction of throw
        //moveMagnitude, strength of throw
        public void ThrowObject(ServerAction action)
		{
			if(ItemInHand == null)
			{
				Debug.Log("can't throw nothing!");            
				return;
			}

			GameObject go = ItemInHand;

			DropHandObject(action);

			ServerAction apply = new ServerAction();
			apply.moveMagnitude = action.moveMagnitude;

			Vector3 dir = m_Camera.transform.forward;
			apply.x = dir.x;
			apply.y = dir.y;
			apply.z = dir.z;

			go.GetComponent<SimObjPhysics>().ApplyForce(apply);         
		}

        public void SetUpRotationBoxChecks()
		{
			if (ItemInHand == null)
			{
				//Debug.Log("no need to set up boxes if nothing in hand");
				return;

			}
         
			BoxCollider HeldItemBox = ItemInHand.GetComponent<SimObjPhysics>().RotateAgentCollider.GetComponent<BoxCollider>();
         
            //rotate all pivots to 0, move all box colliders to the position of the box collider of item in hand
            //change each box collider's size and center
            //rotate all pivots to where they need to go

            //////////////Left/Right stuff first

            //zero out everything first
			for (int i = 0; i < RotateRLPivots.Length; i++)
			{
				RotateRLPivots[i].transform.localRotation = Quaternion.Euler(Vector3.zero);
			}

            //set the size of all RotateRL trigger boxes to the Rotation Collider's dimesnions
			for (int i = 0 ; i < RotateRLTriggerBoxes.Length; i++)
			{
				RotateRLTriggerBoxes[i].transform.position = HeldItemBox.transform.position;
				RotateRLTriggerBoxes[i].transform.rotation = HeldItemBox.transform.rotation;
				RotateRLTriggerBoxes[i].transform.localScale = HeldItemBox.transform.localScale;

				RotateRLTriggerBoxes[i].GetComponent<BoxCollider>().size = HeldItemBox.size;
				RotateRLTriggerBoxes[i].GetComponent<BoxCollider>().center = HeldItemBox.center;
			}

			int deg = -90;

			//set all pivots to their corresponding rotations
			for (int i = 0; i < RotateRLTriggerBoxes.Length; i++)
			{            
                if(deg == 0)
				{
					deg = 15;
				}

				RotateRLPivots[i].transform.localRotation = Quaternion.Euler(new Vector3(0, deg, 0));
				deg += 15;
			}

            //////////////////Up/Down stuff now
         
			//zero out everything first
			for (int i = 0; i < LookUDPivots.Length; i ++)
			{
				LookUDPivots[i].transform.localRotation = Quaternion.Euler(Vector3.zero);
			}

			for (int i = 0; i < LookUDTriggerBoxes.Length; i++)
			{
				LookUDTriggerBoxes[i].transform.position = HeldItemBox.transform.position;
				LookUDTriggerBoxes[i].transform.rotation = HeldItemBox.transform.rotation;
				LookUDTriggerBoxes[i].transform.localScale = HeldItemBox.transform.localScale;

				LookUDTriggerBoxes[i].GetComponent<BoxCollider>().size = HeldItemBox.size;
                LookUDTriggerBoxes[i].GetComponent<BoxCollider>().center = HeldItemBox.center;
			}

			int otherdeg = -30;

			for (int i = 0; i < LookUDPivots.Length; i++)
			{
				if(otherdeg == 0)
				{
					otherdeg = 10;
				}
				LookUDPivots[i].transform.localRotation = Quaternion.Euler(new Vector3(otherdeg, 0, 0)); //30 up
				otherdeg += 10;
				//print(otherdeg);
			}
			//LookUDPivots[0].transform.localRotation = Quaternion.Euler(new Vector3(-30, 0, 0)); //30 up
			//LookUDPivots[1].transform.localRotation = Quaternion.Euler(new Vector3(-20, 0, 0));//look forward
			//LookUDPivots[2].transform.localRotation = Quaternion.Euler(new Vector3(-10, 0, 0)); // 30 down
			//LookUDPivots[3].transform.localRotation = Quaternion.Euler(new Vector3(10, 0, 0)); // 60 down
			//LookUDPivots[4].transform.localRotation = Quaternion.Euler(new Vector3(20, 0, 0)); // 60 down         
			//LookUDPivots[5].transform.localRotation = Quaternion.Euler(new Vector3(30, 0, 0)); // 60 down

		}
        
       
		#if UNITY_EDITOR
        //used to show what's currently visible on the top left of the screen
        void OnGUI()
        {
            if (VisibleSimObjPhysics != null)
            {
				if (VisibleSimObjPhysics.Length > 10)
                {
                    int horzIndex = -1;
                    GUILayout.BeginHorizontal();
					foreach (SimObjPhysics o in VisibleSimObjPhysics)
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
					foreach (SimObjPhysics o in VisibleSimObjPhysics)
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
	}

}

