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

		//[SerializeField] protected float MaxViewDistancePhysics = 1.7f; //change MaxVisibleDistance of BaseAgent to this value to account for Physics
		[SerializeField] protected float PhysicsAgentSkinWidth = 0.04f; //change agent's skin width so that it collides directly with ground - otherwise sweeptests will fail for flat objects on floor

		[SerializeField] protected GameObject AgentHand = null;
		[SerializeField] protected GameObject DefaultHandPosition = null;
        [SerializeField] protected GameObject ItemInHand = null;//current object in inventory
              
        [SerializeField] protected GameObject[] RotateRLPivots = null;
		[SerializeField] protected GameObject[] RotateRLTriggerBoxes = null;

		[SerializeField] protected GameObject[] LookUDPivots = null;
		[SerializeField] protected GameObject[] LookUDTriggerBoxes = null;
        
		[SerializeField] protected SimObjPhysics[] VisibleSimObjPhysics; //all SimObjPhysics that are within camera viewport and range dictated by MaxViewDistancePhysics

		[SerializeField] protected bool IsHandDefault = true;


        //change visibility check to use this distance when looking down
		protected float DownwardViewDistance = 2.0f;
    
        // Use this for initialization
        protected override void Start()
        {
			base.Start();

			//ServerAction action = new ServerAction();
			//Initialize(action);

			//below, enable all the GameObjects on the Agent that Physics Mode requires

            //physics requires max distance to be extended to be able to see objects on ground
			//maxVisibleDistance = MaxViewDistancePhysics;//default maxVisibleDistance is 1.0f
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
				if(o.isInteractable == true && o.PrimaryProperty == SimObjPrimaryProperty.CanPickup)
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

			//Vector3 itemDirection = Vector3.zero;
			//         //do a raycast in the direction of the item
			//         itemDirection = (itemTargetPoint - agentCameraPos).normalized;
			//Vector3 agentForward = agentCamera.transform.forward;
			//agentForward.y = 0f;
			//agentForward.Normalize();
			////clap the angle so we can't wrap around
			//float maxDistanceLerp = 0f;
			//float lookAngle = Mathf.Clamp(Vector3.Angle(agentForward, itemDirection), 0f, MaxDownwardLookAngle) - MinDownwardLooKangle;
			//maxDistanceLerp = lookAngle / MaxDownwardLookAngle;
			//maxDistance = Mathf.Lerp(maxDistance, maxDistance * DownwardRangeExtension, maxDistanceLerp);

            //get all sim objects in range around us
			Collider[] colliders_in_view = Physics.OverlapSphere(agentCameraPos, maxDistance * DownwardViewDistance,
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
						if (viewPoint.z > 0 //&& viewPoint.z < maxDistance * DownwardViewDistance //is in front of camera and within range of visibility sphere
                            && viewPoint.x < ViewPointRangeHigh && viewPoint.x > ViewPointRangeLow//within x bounds of viewport
                            && viewPoint.y < ViewPointRangeHigh && viewPoint.y > ViewPointRangeLow)//within y bounds of viewport
						{   
                            ////down extension stuff
							float MaxDownwardLookAngle = 60f;
                            float MinDownwardLookAngle = 15f;
                            
                            Vector3 itemDirection = Vector3.zero;
                            //do a raycast in the direction of the item
                            itemDirection = (ip.position - agentCamera.transform.position).normalized;
                            Vector3 agentForward = agentCamera.transform.forward;
                            agentForward.y = 0f;
                            agentForward.Normalize();
                            //clap the angle so we can't wrap around
                            float maxDistanceLerp = 0f;
                            float lookAngle = Mathf.Clamp(Vector3.Angle(agentForward, itemDirection), 0f, MaxDownwardLookAngle) - MinDownwardLookAngle;
                            maxDistanceLerp = lookAngle / MaxDownwardLookAngle;
                            maxDistance = Mathf.Lerp(maxDistance, maxDistance * DownwardViewDistance, maxDistanceLerp);


                            //down extension stuff ends

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
            
			if (viewPoint.z > 0 //&& viewPoint.z < maxDistance * DownwardViewDistance //is in front of camera and within range of visibility sphere
                && viewPoint.x < ViewPointRangeHigh && viewPoint.x > ViewPointRangeLow//within x bounds of viewport
                && viewPoint.y < ViewPointRangeHigh && viewPoint.y > ViewPointRangeLow)//within y bounds of viewport
            {

                ///////downard max distance extension here
				float MaxDownwardLookAngle = 60f;
				float MinDownwardLookAngle = 15f;

				Vector3 itemDirection = Vector3.zero;
                         //do a raycast in the direction of the item
				itemDirection = (point.position - agentCamera.transform.position).normalized;
                Vector3 agentForward = agentCamera.transform.forward;
                agentForward.y = 0f;
                agentForward.Normalize();
                //clap the angle so we can't wrap around
                float maxDistanceLerp = 0f;
                float lookAngle = Mathf.Clamp(Vector3.Angle(agentForward, itemDirection), 0f, MaxDownwardLookAngle) - MinDownwardLookAngle;
                maxDistanceLerp = lookAngle / MaxDownwardLookAngle;
				maxDistance = Mathf.Lerp(maxDistance, maxDistance * DownwardViewDistance, maxDistanceLerp);

                ///////end downward max distance stuff
                
				//now cast a ray out toward the point, if anything occludes this point, that point is not visible
				RaycastHit hit;
				if(Physics.Raycast(agentCamera.transform.position, point.position - agentCamera.transform.position, out hit, 
                   maxDistance /*Vector3.Distance(point.position,agentCamera.transform.position)*/, 1<<8))//layer mask automatically excludes Agent from this check
				{
                    if(hit.transform != sop.transform)
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
      
		public override void LookDown(ServerAction response)
		{
			float targetHorizon = 0.0f;

			if (currentHorizonAngleIndex() > 0)
			{
				targetHorizon = horizonAngles[currentHorizonAngleIndex() - 1];
			}

			int down = -1;
            
			if(CheckIfAgentCanLook(targetHorizon, down)) 
			{
				DefaultAgentHand(response);
				base.LookDown(response);
			}

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
			{
				DefaultAgentHand(controlCommand);
				base.LookUp(controlCommand);
			}

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
         
            return result;
        }
        
        //

		public override void RotateRight(ServerAction controlCommand)
		{
			if(CheckIfAgentCanTurn(90))
			{
				DefaultAgentHand(controlCommand);
				base.RotateRight(controlCommand);
			}

		}

		public override void RotateLeft(ServerAction controlCommand)
		{
			if(CheckIfAgentCanTurn(-90))
			{
				DefaultAgentHand(controlCommand);
				base.RotateLeft(controlCommand);

			}
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

            return result;
        }

        //for all translational movement, check if the item the player is holding will hit anything, or if the agent will hit anything
		public override void MoveLeft(ServerAction action)
		{
			if(CheckIfItemBlocksAgentMovement(action.moveMagnitude, 270) && CheckIfAgentCanMove(action.moveMagnitude, 270))
			{
				DefaultAgentHand(action);
				base.MoveLeft(action);
			}
		}

		public override void MoveRight(ServerAction action)
		{
			if (CheckIfItemBlocksAgentMovement(action.moveMagnitude, 90) && CheckIfAgentCanMove(action.moveMagnitude, 90)) 
			{
				DefaultAgentHand(action);
				base.MoveRight(action);
			}
		}

		public override void MoveAhead(ServerAction action)
		{
			if (CheckIfItemBlocksAgentMovement(action.moveMagnitude, 0) && CheckIfAgentCanMove(action.moveMagnitude, 0))
			{
				DefaultAgentHand(action);
				base.MoveAhead(action);            
			}
		}
        
		public override void MoveBack(ServerAction action)
		{
			if (CheckIfItemBlocksAgentMovement(action.moveMagnitude, 180) && CheckIfAgentCanMove(action.moveMagnitude, 180))
			{
				DefaultAgentHand(action);
				base.MoveBack(action);
    		}
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

            //XXX might need to extend this range to reach down into low drawers/cabinets?
			//print(Vector3.Distance(gameObject.transform.position, targetPosition));
			//now check if the target position is within bounds of the Agent's forward (z) view
			if (Vector3.Distance(m_Camera.transform.position, targetPosition) > maxVisibleDistance)// + 0.3)
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

  //      //isOpen is true if trying to Close object, False if trying to open object
  //      public void OpenOrCloseObject(ServerAction action, bool open)
		//{
		//	//pass name of object in from action.objectID
  //          //check if that object is in the viewport
  //          //also check to make sure that target object is interactable
		//	if(action.objectId == null)
		//	{
		//		Debug.Log("Hey, actually give me an object ID to pick up, yeah?");
		//		return;
		//	}
				
		//	SimObjPhysics target = null;

  //          foreach (SimObjPhysics sop in VisibleSimObjPhysics)
  //          {
		//		//print("why not?");
		//		//check for object in current visible objects, and also check that it's interactable
		//		if (action.objectId == sop.UniqueID && sop.GetComponent<CanOpen>())
		//		{
		//			//print("wobbuffet");
		//			target = sop;
		//		}
	
  //          }

  //          if(target)
		//	{
		//		CanOpen co = target.GetComponent<CanOpen>();

  //              //trying to close object
		//		if(open == true)
		//		{
		//			if (co.isOpen == true)
		//			{                  
		//				co.Interact();
		//			}
                  
		//			else
		//				Debug.Log("can't close object if it's already closed");
		//		}

  //              //trying to open object
  //              else if(open == false)
		//		{
		//			if (co.isOpen == false)
		//			{
		//				if (action.moveMagnitude > 0.0f)
  //                      {
  //                          co.SetOpenPercent(action.moveMagnitude);
  //                      }

		//				co.Interact();		
		//			}

		//			else
  //                      Debug.Log("can't open object if it's already open");
		//		}
		//		//print("i have a target");
		//		//target.GetComponent<CanOpen>().Interact();
		//	}
            
		//}

        public void CloseObject(ServerAction action)
		{
			//pass name of object in from action.objectID
            //check if that object is in the viewport
            //also check to make sure that target object is interactable
            if (action.objectId == null)
            {
                Debug.Log("Hey, actually give me an object ID to pick up, yeah?");
                errorMessage = "objectId required for OpenObject";
                actionFinished(false);
                Debug.Log("Hey, actually give me an object ID to pick up, yeah?");
                return;
            }

            SimObjPhysics target = null;

            foreach (SimObjPhysics sop in VisibleSimObjs(action))
            {
                //print("why not?");
                //check for object in current visible objects, and also check that it's interactable
				if (!sop.isInteractable)
				{
					Debug.Log(sop.UniqueID + " is not Interactable");
					return;
				}
 
				
                if (sop.GetComponent<CanOpen>())
                {
                    //print("wobbuffet");
                    target = sop;
                }

            }
            
            if (target)
            {
                CanOpen co = target.GetComponent<CanOpen>();
                
                //if object is open, close it
                if (co.isOpen)
                {
                    co.Interact();
                    actionFinished(true);
                }

                else {
                    Debug.Log("can't close object if it's already closed");
                    actionFinished(false);
                    errorMessage = "object already open: " + action.objectId;
                }
            } else {
                actionFinished(false);
                errorMessage = "object not found: " + action.objectId;
            }
		}

        public void OpenObject(ServerAction action)
		{
			//pass name of object in from action.objectID
            //check if that object is in the viewport
            //also check to make sure that target object is interactable
            if (action.objectId == null)
            {
                Debug.Log("Hey, actually give me an object ID to pick up, yeah?");
                errorMessage = "objectId required for OpenObject";
                actionFinished(false);
                return;
            }

            SimObjPhysics target = null;

            foreach (SimObjPhysics sop in VisibleSimObjs(action))
            {
				//print("why not?");
				//check for object in current visible objects, and also check that it's interactable
				if (!sop.isInteractable)
                {
                    Debug.Log(sop.UniqueID + " is not Interactable");
                    return;
                }
				
                if (sop.GetComponent<CanOpen>())
                {
                    //print("wobbuffet");
                    target = sop;
                }

            }

			if (target)
			{
				CanOpen co = target.GetComponent<CanOpen>();

                //check to make sure object is closed
				if (co.isOpen)
                {
                    Debug.Log("can't open object if it's already open");
                    errorMessage = "object already open";
                    actionFinished(false);
                } else {
					//pass in percentage open if desired
                    // XXX should switch this to 
                    if (action.moveMagnitude > 0.0f)
                    {
                        co.SetOpenPercent(action.moveMagnitude);
                    }

                    co.Interact();
                    // XXX need to add checkOpenAction to determine if agent got moved
                    actionFinished(true);
                }
			}
		}

        public void Contains(ServerAction action)
		{
			if (action.objectId == null)
            {
                Debug.Log("Hey, actually give me an object ID to pick up, yeah?");
                return;
            }

			SimObjPhysics target = null;

            foreach (SimObjPhysics sop in VisibleSimObjPhysics)
            {
                //check for object in current visible objects, and also check that it's interactable
                if (action.objectId == sop.UniqueID)
                {
                    //print("wobbuffet");
                    target = sop;
                }

            }

			if (target)
			{
				//the sim object receptacle target returns list of unique sim object IDs as strings
                //XXX It looks like this goes right into the MetaData, so I'll need help figuring out how to pass whatever
				//is handling Metadata exporting this info. For now uh....the Contains() function on the sim object will print it in the log
                //if in editor
				target.Contains();
			}

			else
			{
				Debug.Log("Target object not in sight");
                errorMessage = "object not found: " + action.objectId;
                actionFinished(false);
            }
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

            //set the size of all RotateRL trigger boxes to the Rotate Agent Collider's dimesnions
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
		}
		public SimObjPhysics[] VisibleSimObjs(bool forceVisible)
		{
			if (forceVisible)
			{
				return GameObject.FindObjectsOfType(typeof(SimObjPhysics)) as SimObjPhysics[];
			}
			else
			{
				return GetAllVisibleSimObjPhysics(m_Camera, maxVisibleDistance);
			}
		}
        
		public SimObjPhysics[] VisibleSimObjs(ServerAction action) 
		{
			List<SimObjPhysics> simObjs = new List<SimObjPhysics> ();

			foreach (SimObjPhysics so in VisibleSimObjs (action.forceVisible)) 
			{

				if (!string.IsNullOrEmpty(action.objectId) && action.objectId != so.UniqueID) 
				{
					continue;
				}

				if (!string.IsNullOrEmpty(action.objectType) && action.GetSimObjType() != so.Type) 
				{
					continue;
				}

				simObjs.Add (so);
			}	


			return simObjs.ToArray ();
            
		}

		public void MassInRightScale(ServerAction action)
		{
			if (action.objectId == null)
			{
				Debug.Log("Please give me a MassScale's UniqueID");
				return;
			}

			SimObjPhysics target = null;

			foreach (SimObjPhysics sop in VisibleSimObjPhysics)
			{
				//check for object in current visible objects, and also check that it's interactable
				if (action.objectId == sop.UniqueID)
				{
					//print("wobbuffet");
					target = sop;
				}

			}

			if (target)
			{
				//XXX this is where the metadata would be exported, this info right here
				Debug.Log("The Right Scale has:" + target.GetComponent<MassScale>().RightScale_TotalMass() + " kg in it");
				//return target.GetComponent<MassScale>().RightScale_TotalMass();
			}
		}

		public void MassInLeftScale(ServerAction action)
        {
            if (action.objectId == null)
            {
                Debug.Log("Please give me a MassScale's UniqueID");
                return;
            }

            SimObjPhysics target = null;

            foreach (SimObjPhysics sop in VisibleSimObjPhysics)
            {
                //check for object in current visible objects, and also check that it's interactable
                if (action.objectId == sop.UniqueID)
                {
                    //print("wobbuffet");
                    target = sop;
                }
                
            }

            if (target)
            {
                //XXX this is where the metadata would be exported, this info right here
                Debug.Log("The Left Scale has:" + target.GetComponent<MassScale>().LeftScale_TotalMass() + " kg in it");
                //return target.GetComponent<MassScale>().RightScale_TotalMass();
            }
        }
        
		public void CountInRightScale(ServerAction action)
        {
            if (action.objectId == null)
            {
                Debug.Log("Please give me a MassScale's UniqueID");
                return;
            }

            SimObjPhysics target = null;

            foreach (SimObjPhysics sop in VisibleSimObjPhysics)
            {
                //check for object in current visible objects, and also check that it's interactable
                if (action.objectId == sop.UniqueID)
                {
                    //print("wobbuffet");
                    target = sop;
                }

            }

            if (target)
            {
                //XXX this is where the metadata would be exported, this info right here
				Debug.Log("The Right Scale has: " + target.GetComponent<MassScale>().RightScaleObjectCount() + " objects in it");
                //return target.GetComponent<MassScale>().RightScale_TotalMass();
            }
        }

		public void CountInLeftScale(ServerAction action)
        {
            if (action.objectId == null)
            {
                Debug.Log("Please give me a MassScale's UniqueID");
                return;
            }

            SimObjPhysics target = null;

            foreach (SimObjPhysics sop in VisibleSimObjPhysics)
            {
                //check for object in current visible objects, and also check that it's interactable
                if (action.objectId == sop.UniqueID)
                {
                    //print("wobbuffet");
                    target = sop;
                }

            }
            
            if (target)
            {
                //XXX this is where the metadata would be exported, this info right here
                Debug.Log("The Left Scale has :" + target.GetComponent<MassScale>().LeftScaleObjectCount() + " objects in it");
                //return target.GetComponent<MassScale>().RightScale_TotalMass();
            }
        }

		public void ObjectsInRightScale(ServerAction action)
        {
            if (action.objectId == null)
            {
                Debug.Log("Please give me a MassScale's UniqueID");
                return;
            }

            SimObjPhysics target = null;

            foreach (SimObjPhysics sop in VisibleSimObjPhysics)
            {
                //check for object in current visible objects, and also check that it's interactable
                if (action.objectId == sop.UniqueID)
                {
                    //print("wobbuffet");
                    target = sop;
                }

            }

            if (target)
            {
                //XXX this is where the metadata would be exported, this info right here
				List<SimObjPhysics> ObjectsOnScale = new List<SimObjPhysics>(target.GetComponent<MassScale>().ObjectsInRightScale());

				string result = "Right Scale Contains: ";

				foreach(SimObjPhysics sop in ObjectsOnScale)
				{
					result += sop.name + ", ";
				}

				Debug.Log(result);
            
            }
        }

		public void ObjectsInLeftScale(ServerAction action)
        {
            if (action.objectId == null)
            {
                Debug.Log("Please give me a MassScale's UniqueID");
                return;
            }

            SimObjPhysics target = null;

            foreach (SimObjPhysics sop in VisibleSimObjPhysics)
            {
                //check for object in current visible objects, and also check that it's interactable
                if (action.objectId == sop.UniqueID)
                {
                    //print("wobbuffet");
                    target = sop;
                }

            }

            if (target)
            {
                //XXX this is where the metadata would be exported, this info right here
                List<SimObjPhysics> ObjectsOnScale = new List<SimObjPhysics>(target.GetComponent<MassScale>().ObjectsInLeftScale());

                string result = "Left Scale Contains: ";

                foreach (SimObjPhysics sop in ObjectsOnScale)
                {
                    result += sop.name + ", ";
                }

                Debug.Log(result);

            }
        }

        //spawn a single object of a single type
		public void SpawnerSS(ServerAction action)
		{
			//need string of object to spawn
			if (action.objectId == null)
            {
                Debug.Log("Please give me a an Mass Object Spawner's UniqueID");
                return;
            }

            SimObjPhysics target = null;

            foreach (SimObjPhysics sop in VisibleSimObjPhysics)
            {
                //check for object in current visible objects, and also check that it's interactable
                if (action.objectId == sop.UniqueID)
                {
                    target = sop;
                }

            }

            if (target)
            {
				target.GetComponent<MassComparisonObjectSpawner>().SpawnSingle_SingleObjectType(action.objectType);            
            }
		}

        //spawn a single object of a random type
		public void SpawnerSOR(ServerAction action)
        {
            //need string of object to spawn
            if (action.objectId == null)
            {
                Debug.Log("Please give me a an Mass Object Spawner's UniqueID");
                return;
            }

            SimObjPhysics target = null;

            foreach (SimObjPhysics sop in VisibleSimObjPhysics)
            {
                //check for object in current visible objects, and also check that it's interactable
                if (action.objectId == sop.UniqueID)
                {
                    target = sop;
                }

            }

            if (target)
            {
				target.GetComponent<MassComparisonObjectSpawner>().SpawnSingle_One_RandomObjectType();
            }
        }

		//spawn multiple objects, all of a single type
        public void SpawnerMS(ServerAction action)
        {
            //need string of object to spawn
            if (action.objectId == null)
            {
                Debug.Log("Please give me a an Mass Object Spawner's UniqueID");
                return;
            }

            SimObjPhysics target = null;

            foreach (SimObjPhysics sop in VisibleSimObjPhysics)
            {
                //check for object in current visible objects, and also check that it's interactable
                if (action.objectId == sop.UniqueID)
                {
                    target = sop;
                }

            }

            if (target)
            {
				target.GetComponent<MassComparisonObjectSpawner>().
				      SpawnMultiple_SingleObjectType(action.maxNumRepeats, action.objectType, action.moveMagnitude);
            }
        }

		//spawn multiple objects, all of one random type
        public void SpawnerMOR(ServerAction action)
        {
            //need string of object to spawn
            if (action.objectId == null)
            {
                Debug.Log("Please give me a an Mass Object Spawner's UniqueID");
                return;
            }

            SimObjPhysics target = null;

            foreach (SimObjPhysics sop in VisibleSimObjPhysics)
            {
                //check for object in current visible objects, and also check that it's interactable
                if (action.objectId == sop.UniqueID)
                {
                    target = sop;
                }

            }

            if (target)
            {
				target.GetComponent<MassComparisonObjectSpawner>().
				      SpawnMultiple_One_RandomObjectType(action.maxNumRepeats, action.moveMagnitude);
            }
        }

		//spawn multiple objects, each of a random type
        public void SpawnerMER(ServerAction action)
        {
            //need string of object to spawn
            if (action.objectId == null)
            {
                Debug.Log("Please give me a an Mass Object Spawner's UniqueID");
                return;
            }

            SimObjPhysics target = null;

            foreach (SimObjPhysics sop in VisibleSimObjPhysics)
            {
                //check for object in current visible objects, and also check that it's interactable
                if (action.objectId == sop.UniqueID)
                {
                    target = sop;
                }

            }

            if (target)
            {
				target.GetComponent<MassComparisonObjectSpawner>().
				      SpawnMultiple_Each_RandomObjectType(action.maxNumRepeats, action.moveMagnitude);
            }
        }

		//spawn a random number (given a range) of objects, all of a single defined type
		public void SpawnerRS(ServerAction action)
        {
            //need string of object to spawn
            if (action.objectId == null)
            {
                Debug.Log("Please give me a an Mass Object Spawner's UniqueID");
                return;
            }

            SimObjPhysics target = null;

            foreach (SimObjPhysics sop in VisibleSimObjPhysics)
            {
                //check for object in current visible objects, and also check that it's interactable
                if (action.objectId == sop.UniqueID)
                {
                    target = sop;
                }

            }

            if (target)
            {
				target.GetComponent<MassComparisonObjectSpawner>().
				      SpawnRandRange_SingleObjectType(action.agentCount, action.maxNumRepeats, action.objectType, action.moveMagnitude);
            }
        }

		//spawn a random number (given a range) of objects, all of one random type
        public void SpawnerROR(ServerAction action)
        {
            //need string of object to spawn
            if (action.objectId == null)
            {
                Debug.Log("Please give me a an Mass Object Spawner's UniqueID");
                return;
            }

            SimObjPhysics target = null;

            foreach (SimObjPhysics sop in VisibleSimObjPhysics)
            {
                //check for object in current visible objects, and also check that it's interactable
                if (action.objectId == sop.UniqueID)
                {
                    target = sop;
                }

            }

            if (target)
            {
                target.GetComponent<MassComparisonObjectSpawner>().
				      SpawnRandRange_One_RandomObjectType(action.agentCount, action.maxNumRepeats, action.moveMagnitude);
            }
        }

		//spawn a random number (given a range) of objects, each of a random type
        public void SpawnerRER(ServerAction action)
        {
            //need string of object to spawn
            if (action.objectId == null)
            {
                Debug.Log("Please give me a an Mass Object Spawner's UniqueID");
                return;
            }

            SimObjPhysics target = null;

            foreach (SimObjPhysics sop in VisibleSimObjPhysics)
            {
                //check for object in current visible objects, and also check that it's interactable
                if (action.objectId == sop.UniqueID)
                {
                    target = sop;
                }

            }

            if (target)
            {
                target.GetComponent<MassComparisonObjectSpawner>().
				      SpawnRandRange_Each_RandomObjectType(action.agentCount, action.maxNumRepeats, action.moveMagnitude);
            }
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

