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
		[SerializeField] protected float MaxViewDistancePhysics = 1.7f; //change MaxVisibleDistance of BaseAgent to this value to account for Physics
        
		[SerializeField] protected GameObject AgentHand = null;
		[SerializeField] protected GameObject DefaultHandPosition = null;
        [SerializeField] protected GameObject ItemInHand = null;

		//for turning and look Sweeptests
		[SerializeField] protected GameObject LookSweepPosition = null;
		[SerializeField] protected GameObject LookSweepTestPivot = null; //if the Camera position ever moves, make sure this is set to the same local position as FirstPersonCharacter
		[SerializeField] protected GameObject TurnSweepPosition = null;
        [SerializeField] protected GameObject TurnSweepTestPivot = null;
        
		[SerializeField] protected SimObjPhysics[] VisibleSimObjPhysics; //all SimObjPhysics that are within camera viewport and range dictated by MaxViewDistancePhysics

		[SerializeField] public bool IsHandDefault = true;

        // Use this for initialization
        protected override void Start()
        {
			base.Start();

			//enable all the GameObjects on the Agent that Physics Mode requires

            //physics requires max distance to be extended to be able to see objects on ground
			maxVisibleDistance = MaxViewDistancePhysics;

			AgentHand.SetActive(true);
			DefaultHandPosition.SetActive(true);

			LookSweepTestPivot.SetActive(true);
			LookSweepPosition.SetActive(true);

			TurnSweepTestPivot.SetActive(true);
			TurnSweepPosition.SetActive(true);
        }

        // Update is called once per frame
        void Update()
        {
			//VisibleSimObjPhysics = GetAllVisibleSimObjPhysics(m_Camera, maxVisibleDistance);
        }

		private void LateUpdate()
		{
			VisibleSimObjPhysics = GetAllVisibleSimObjPhysics(m_Camera, maxVisibleDistance);

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

                        //if the object does have compount trigger colliders, get the SimObjPhysics component from the parent
                        else
                        {
                            sop = item.GetComponentInParent<SimObjPhysics>();
                        }

                        if (sop.VisibilityPoints.Length > 0)
                        {
                            Transform[] visPoints = sop.VisibilityPoints;
                            int visPointCount = 0;

                            foreach (Transform point in visPoints)
                            {
                                //if this particular point is in view...
                                if (CheckIfVisibilityPointInViewport(point, agentCamera, maxDistance))
                                {
                                    visPointCount++;
                                }
                            }

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
                    }
                }
            }
            
            //populate array of visible items in order by distance
            currentlyVisibleItems.Sort((x, y) => Vector3.Distance(x.transform.position, agentCameraPos).CompareTo(Vector3.Distance(y.transform.position, agentCameraPos)));
            return currentlyVisibleItems.ToArray();
        }

        
		protected bool CheckIfVisibilityPointInViewport(Transform point, Camera agentCamera, float maxDistance)
        {
            bool result = false;

            Vector3 viewPoint = agentCamera.WorldToViewportPoint(point.position);

            float ViewPointRangeHigh = 1.0f;
            float ViewPointRangeLow = 0.0f;

            if (viewPoint.z > 0 && viewPoint.z < maxDistance //is in front of camera and within range of visibility sphere
                   && viewPoint.x < ViewPointRangeHigh && viewPoint.x > ViewPointRangeLow//within x bounds of viewport
                    && viewPoint.y < ViewPointRangeHigh && viewPoint.y > ViewPointRangeLow)//within y bounds of viewport
            {
                result = true;
                #if UNITY_EDITOR
                Debug.DrawLine(agentCamera.transform.position, point.position, Color.yellow);
                #endif
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

			if(CheckIfAgentCanLook(targetHorizon))            
			base.LookDown(response);
		}

		public override void LookUp(ServerAction controlCommand)
		{
			float targetHorizon = 0.0f;

			if (currentHorizonAngleIndex() < horizonAngles.Length - 1)
			{
				targetHorizon = horizonAngles[currentHorizonAngleIndex() + 1];
			}

			if(CheckIfAgentCanLook(targetHorizon))                        
			base.LookUp(controlCommand);
		}

		public bool CheckIfAgentCanLook(float targetAngle)
        {
            //print(targetAngle);
            if (ItemInHand == null)
            {
                Debug.Log("Look check passed: nothing in Agent Hand to prevent Angle change");
                return true;
            }

            //returns true if Rotation is allowed
            bool result = false;

            //zero out the pivot and default to hand's current position AND rotation
            LookSweepTestPivot.transform.localRotation = m_Camera.transform.localRotation;
            LookSweepPosition.transform.position = AgentHand.transform.position;

            //rotate pivot to target location, then sweep for obstacles
            LookSweepTestPivot.transform.localRotation = Quaternion.AngleAxis(targetAngle, Vector3.right);
            //RotationSweepTestPivot.transform.localRotation = Quaternion.Euler(new Vector3(targetAngle, 0, 0));
            
            RaycastHit hit;

            Rigidbody ItemRB = ItemInHand.GetComponent<Rigidbody>();

            //check if the sweep hits anything at all
            if (ItemRB.SweepTest(LookSweepPosition.transform.position - AgentHand.transform.position, out hit,
                                 Vector3.Distance(LookSweepPosition.transform.position, AgentHand.transform.position),
                                 QueryTriggerInteraction.Ignore))
            {
                //If the thing hit was anything except the object itself, the agent, or the agent's hand - it's blocking
                if (hit.transform != AgentHand.transform && hit.transform != ItemInHand.transform && hit.transform != gameObject.transform) //&& hit.transform != gameObject.transform
                {
                    Debug.Log("Can't change view to " + targetAngle + ", " + hit.transform.name + "is blocking the way");
                    result = false;
                }

                else
                {
                    //the sweep hit something that it is ok to hit (agent itself, agent's hand, object itself somehow)
                    result = true;

                }

            }

            else
            {
                //oh we didn't hit anything, good to go
                result = true;
            }
         
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
            bool result = false;

            if (ItemInHand == null)
            {
                Debug.Log("Rotation check passed: nothing in Agent Hand");
                return true;
            }

            if (direction != 90 && direction != -90)
            {
                Debug.Log("Please give -90(left) or 90(right) as direction parameter");
                return false;
            }

            //zero out the pivot and default to hand's current position
            TurnSweepTestPivot.transform.localRotation = Quaternion.Euler(Vector3.zero);
            TurnSweepPosition.transform.position = AgentHand.transform.position;


            TurnSweepTestPivot.transform.localRotation = Quaternion.Euler(new Vector3(0, direction, 0));

            RaycastHit hit;

            Rigidbody ItemRB = ItemInHand.GetComponent<Rigidbody>();

            //check if the sweep hits anything at all
            if (ItemRB.SweepTest(TurnSweepPosition.transform.position - AgentHand.transform.position, out hit,
                                 Vector3.Distance(TurnSweepPosition.transform.position, AgentHand.transform.position),
                                 QueryTriggerInteraction.Ignore))
            {
                //print(hit.transform.name);
                //If the thing hit was anything except the object itself, the agent, or the agent's hand - it's blocking
                if (hit.transform != AgentHand.transform && hit.transform != gameObject.transform && hit.transform != ItemInHand.transform)
                {
                    if (direction == 90)
                    {
                        Debug.Log(hit.transform.name + " is in Agent Hand's Path! Can't rotate Agent RIGHT");
                        result = false;
                    }

                    else
                    {
                        Debug.Log(hit.transform.name + " is in Agent Hand's Path! Can't rotate Agent LEFT");
                        result = false;
                    }
                }

                else
                {
                    //the sweep hit something that it is ok to hit (agent itself, agent's hand, object itself somehow)
                    result = true;
                }
            }

            else
            {
                //oh we didn't hit anything, good to go
                result = true;
            }

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
                Debug.Log("Agent has nothing in hand blocking movement");
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
						if (gameObject.transform == res.transform)
                        {
                            result = true;
                            break;
                        }

						else
						{
							result = false;
							Debug.Log(res.transform.name + " is blocking the Agent from moving " + orientation + " with " + ItemInHand.name);

						}
                                          
					}
				}

				//if the array is empty, nothing was hit by the sweeptest so we are clear to move
                else
                {
                    Debug.Log("Agent Body can move " + orientation);
                    result = true;
                }

                return result;
                //if (rb.SweepTest(dir, out hit, moveMagnitude, QueryTriggerInteraction.Ignore))
                //{
                //    Debug.Log(hit.transform.name + " is blocking Agent Hand holding " + ItemInHand.name + " from moving " + orientation);
                //    result = false;
                //}
                ////nothing was hit, we good
                //else
                //{
                //    Debug.Log("Agent hand holding " + ItemInHand.name + " can move " + orientation + " " + moveMagnitude + " units");
                //    result = true;
                //}
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

            //might need to sweep test all, check for static..... want to be able to try and move through sim objects that can pickup and move yes
            RaycastHit[] sweepResults = rb.SweepTestAll(dir, moveMagnitude, QueryTriggerInteraction.Ignore);

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
                        if (res.transform.GetComponent<SimObjPhysics>())
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
                Debug.Log("Agent Body can move " + orientation);
                result = true;
            }

            return result;
        }


        /////AGENT HAND STUFF////

		public void ResetAgentHandPosition()
        {
            AgentHand.transform.position = DefaultHandPosition.transform.position;
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

		//returns true if the Hand Movement was succesful
        //false if blocked by something or out of range
        public bool MoveHand(Vector3 targetPosition)
        {
            bool result = false;

            //can only move hand if there is an object in it.
            if (ItemInHand == null)
            {
                Debug.Log("Agent can only move hand if holding an item");
                result = false;
                return result;
            }
            //result if movement was succesful or not


            //first check if passed in targetPosition is in range or not           
			if (Vector3.Distance(gameObject.transform.position, targetPosition) > maxVisibleDistance)// + 0.3)
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
            if (ItemRB.SweepTest(targetPosition - AgentHand.transform.position, out hit, Vector3.Distance(targetPosition, AgentHand.transform.position)))
            {
                //return error if anything but the Agent Hand or the Agent are hit
                if (hit.transform != AgentHand.transform && hit.transform != gameObject.transform)
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
                IsHandDefault = false;
                result = true;
            }

            return result;

        }

		//used by RotateSimObjPhysicsInHand for compound collider object comparison
        private bool CheckForMatches(IEnumerable<Transform> objects, Transform toCompare)
        {
            foreach (Transform t in objects)
            {
                if (toCompare == t)
                {
                    return true;
                }
            }
            return false;
        }

        public bool RotateSimObjPhysicsInHand(Vector3 vec)
        {
            //based on the collider type of the item in the Agent's Hand, set the radius of the OverlapSphere to check if there is room for rotation
            if (ItemInHand != null)
            {
                //for items that use box colliders
                if (ItemInHand.GetComponent<BoxCollider>())
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

                    //for objects that might have compound colliders, make sure we track them here for comparison below
                    //NOTE: Make sure any objects with compound colliders have an "isTrigger" Collider on the highest object in the 
                    //Heirarchy. The check for "Box" or "Sphere" Collider will use that trigger collider for radius calculations, since
                    //getting the dimensions of a compound collider wouldn't make any sense due to irregular shapes
                    Transform[] anyChildren = ItemInHand.GetComponentsInChildren<Transform>();

                    foreach (Collider col in hitColliders)
                    {
                        //check if the thing collided with by the OverlapSphere is the agent, the hand, or the object itself
                        if (col.name != "TextInputModeler" && col.name != "TheHand" && col.name != ItemInHand.name)
                        {
                            //also check against any children the ItemInHand has for prefabs with compound colliders                     
                            //set to true if there is a match between this collider among ANY of the children of ItemInHand

                            if (CheckForMatches(anyChildren, col.transform) == false)
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

		public bool PickUpSimObjPhysics(Transform target)
        {
            if (target.GetComponent<SimObjPhysics>().PrimaryProperty != SimObjPrimaryProperty.CanPickup)
            {
                Debug.Log("Only SimObjPhysics that have the property CanPickup can be picked up");
                return false;
            }
            //make sure hand is empty, turn off the target object's collision and physics properties
            //and make the object kinematic
            if (ItemInHand == null)
            {
                if (IsHandDefault == false)
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
                //Text txt = Inventory_Text.GetComponent<Text>();
                //txt.text = "In Inventory: " + target.name + " " + target.GetComponent<SimObjPhysics>().UniqueID;

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

                if (ItemInHand.GetComponent<SimObjPhysics>().isColliding)
                {
                    Debug.Log(ItemInHand.transform.name + " can't be dropped. It must be clear of all other objects first");
                    return false;
                }

                else
                {
                    ItemInHand.GetComponent<Rigidbody>().isKinematic = false;
                    ItemInHand.transform.parent = null;
                    ItemInHand = null;

                    //take this out later when moving to BaseFPS agent controller
                    //Text txt = Inventory_Text.GetComponent<Text>();
                    //txt.text = "In Inventory: Nothing!";
                    ///////

                    return true;
                }

            }

            else
            {
                Debug.Log("nothing in hand to drop!");
                return false;
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

