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

using UnityEngine.Rendering;
using UnityStandardAssets.ImageEffects;


namespace UnityStandardAssets.Characters.FirstPerson
{
	[RequireComponent(typeof (CharacterController))]   
	public class PhysicsRemoteFPSAgentController : BaseFPSAgentController
    {
		[SerializeField] protected GameObject[] ToSetActive = null;
        
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

        // Extra stuff
        private Dictionary<string, SimObjPhysics> uniqueIdToSimObjPhysics = new Dictionary<string, SimObjPhysics>();
        [SerializeField] public string[] objectIdsInBox = new string[0];
        [SerializeField] protected bool inTopLevelView = false;
        [SerializeField] protected Vector3 lastLocalCameraPosition;
        [SerializeField] protected Quaternion lastLocalCameraRotation;
        [SerializeField] protected float cameraOrthSize;
        protected Dictionary<string, Dictionary<int, Material[]>> maskedObjects = new Dictionary<string, Dictionary<int, Material[]>>();
        protected float[,,] flatSurfacesOnGrid = new float[0,0,0];
		protected float[,] distances = new float[0,0];
		protected float[,,] normals = new float[0,0,0];
		protected bool[,] isOpenableGrid = new bool[0,0];
		protected string[] segmentedObjectIds = new string[0];
        [SerializeField] protected Vector3 standingLocalCameraPosition;
        protected HashSet<int> initiallyDisabledRenderers = new HashSet<int>();
        protected Vector3[] reachablePositions = new Vector3[0];

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

            standingLocalCameraPosition = m_Camera.transform.localPosition;

            foreach (SimObjPhysics so in GameObject.FindObjectsOfType<SimObjPhysics>()) {
                uniqueIdToSimObjPhysics[so.UniqueID] = so;
            }

            foreach (Renderer r in GameObject.FindObjectsOfType<Renderer>()) {
                if (!r.enabled) {
                    initiallyDisabledRenderers.Add(r.GetInstanceID());
                }
            }

            actionFinished(true);
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

        private ObjectMetadata ObjectMetadataFromSimObjPhysics(SimObjPhysics simObj) {
            ObjectMetadata objMeta = new ObjectMetadata();

            GameObject o = simObj.gameObject;
            objMeta.name = o.name;
            objMeta.position = o.transform.position;
            objMeta.rotation = o.transform.eulerAngles;

            objMeta.objectType = Enum.GetName(typeof(SimObjType), simObj.Type);
            objMeta.receptacle = simObj.ReceptacleTriggerBoxes != null && simObj.ReceptacleTriggerBoxes.Length != 0;
            objMeta.openable = simObj.IsOpenable;
            if (objMeta.openable) {
                objMeta.isopen = simObj.IsOpen;
            }
            objMeta.pickupable = simObj.PrimaryProperty == SimObjPrimaryProperty.CanPickup;
            objMeta.objectId = simObj.UniqueID;
            objMeta.visible = simObj.isVisible;

            // TODO: bounds necessary?
            // Bounds bounds = simObj.Bounds;
            // this.bounds3D = new [] {
            //     bounds.min.x,
            //     bounds.min.y,
            //     bounds.min.z,
            //     bounds.max.x,
            //     bounds.max.y,
            //     bounds.max.z,
            // };

            return objMeta;
        }

        private ObjectMetadata[] generateObjectMetadata()
		{
			// Encode these in a json string and send it to the server
			SimObjPhysics[] simObjects = GameObject.FindObjectsOfType<SimObjPhysics>();

			int numObj = simObjects.Length;
			List<ObjectMetadata> metadata = new List<ObjectMetadata>();
			Dictionary<string, List<string>> parentReceptacles = new Dictionary<string, List<string>> ();

			for (int k = 0; k < numObj; k++) {
				SimObjPhysics simObj = simObjects[k];
				if (this.excludeObject(simObj.UniqueID)) {
					continue;
				}
				ObjectMetadata meta = ObjectMetadataFromSimObjPhysics(simObj);

				if (meta.receptacle)
				{
					List<string> receptacleObjectIds = simObj.Contains();
					foreach (string oid in receptacleObjectIds)
					{
                        if (!parentReceptacles.ContainsKey(oid)) {
                            parentReceptacles[oid] = new List<string>();
                        }
                        parentReceptacles[oid].Add(simObj.UniqueID);
					}

					meta.receptacleObjectIds = receptacleObjectIds.ToArray();
					meta.receptacleCount = meta.receptacleObjectIds.Length;
				}
				meta.distance = Vector3.Distance(transform.position, simObj.gameObject.transform.position);
				metadata.Add(meta);
			}

			foreach (ObjectMetadata meta in metadata) {
				if (parentReceptacles.ContainsKey (meta.objectId)) {
					meta.parentReceptacles = parentReceptacles[meta.objectId].ToArray();
				}
			}
			return metadata.ToArray();
		}

		private T[] flatten2DimArray<T>(T[,] array) {
			int nrow = array.GetLength(0);
			int ncol = array.GetLength(1);
			T[] flat = new T[nrow * ncol];
			for (int i = 0; i < nrow; i++) {
				for (int j = 0; j < ncol; j++) {
					flat[i * ncol + j] = array[i, j];
				}
			}
			return flat;
		}

		private T[] flatten3DimArray<T>(T[,,] array) {
			int n0 = array.GetLength(0);
			int n1 = array.GetLength(1);
			int n2 = array.GetLength(2);
			T[] flat = new T[n0 * n1 * n2];
			for (int i = 0; i < n0; i++) {
				for (int j = 0; j < n1; j++) {
					for (int k = 0; k < n2; k++) {
						flat[i * n1 * n2 + j * n2 + k] = array[i, j, k];
					}
				}
			}
			return flat;
		}

        public override MetadataWrapper generateMetadataWrapper() 
        {
            // AGENT METADATA
            ObjectMetadata agentMeta = new ObjectMetadata();
			agentMeta.name = "agent";
			agentMeta.position = transform.position;
			agentMeta.rotation = transform.eulerAngles;
			agentMeta.cameraHorizon = m_Camera.transform.rotation.eulerAngles.x;
			if (agentMeta.cameraHorizon > 180) {
				agentMeta.cameraHorizon -= 360;
			}

            // OTHER METADATA
            MetadataWrapper metaMessage = new MetadataWrapper();
			metaMessage.agent = agentMeta;
			metaMessage.sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
			metaMessage.objects = generateObjectMetadata();
			metaMessage.collided = collidedObjects.Length > 0;
			metaMessage.collidedObjects = collidedObjects;
			metaMessage.screenWidth = Screen.width;
			metaMessage.screenHeight = Screen.height;
            metaMessage.cameraPosition = m_Camera.transform.position;
			metaMessage.cameraOrthSize = cameraOrthSize;
			cameraOrthSize = -1f;
            metaMessage.fov = m_Camera.fieldOfView;
			metaMessage.isStanding = (m_Camera.transform.localPosition - standingLocalCameraPosition).magnitude < 0.1f;

			metaMessage.lastAction = lastAction;
			metaMessage.lastActionSuccess = lastActionSuccess;
			metaMessage.errorMessage = errorMessage;

			if (errorCode != ServerActionErrorCode.Undefined) {
				metaMessage.errorCode = Enum.GetName(typeof(ServerActionErrorCode), errorCode);
			}

			List<InventoryObject> ios = new List<InventoryObject>();

            if (ItemInHand != null) {
                SimObjPhysics so = ItemInHand.GetComponent<SimObjPhysics>();
                InventoryObject io = new InventoryObject();
				io.objectId = so.UniqueID;
				io.objectType = Enum.GetName (typeof(SimObjType), so.Type);
				ios.Add(io);
            }

			metaMessage.inventoryObjects = ios.ToArray();

            // HAND
            metaMessage.hand = new HandMetadata();
            metaMessage.hand.position = AgentHand.transform.position;
			metaMessage.hand.localPosition = AgentHand.transform.localPosition;
			metaMessage.hand.rotation = AgentHand.transform.eulerAngles;
			metaMessage.hand.localRotation = AgentHand.transform.localEulerAngles;

            // EXTRAS
            metaMessage.reachablePositions = reachablePositions;
            metaMessage.flatSurfacesOnGrid = flatten3DimArray(flatSurfacesOnGrid);
			metaMessage.distances = flatten2DimArray(distances);
			metaMessage.normals = flatten3DimArray(normals);
			metaMessage.isOpenableGrid = flatten2DimArray(isOpenableGrid);
            metaMessage.segmentedObjectIds = segmentedObjectIds;
            metaMessage.objectIdsInBox = objectIdsInBox;
            // Resetting things
            reachablePositions = new Vector3[0];
			flatSurfacesOnGrid = new float[0,0,0];
			distances = new float[0,0];
			normals = new float[0,0,0];
			isOpenableGrid = new bool[0,0];
            segmentedObjectIds = new string[0];
			objectIdsInBox = new string[0];
			
			return metaMessage;
		}

        //return ID of closest CanPickup object by distance
        public string UniqueIDOfClosestVisibleObject()
		{
			  string objectID = null;

              foreach (SimObjPhysics o in VisibleSimObjPhysics)
              {
                  if(o.PrimaryProperty == SimObjPrimaryProperty.CanPickup)
                  {
                      objectID = o.UniqueID;
                  //  print(objectID);
                      break;
                  }
              }

              return objectID;
		}

        //return ID of closest CanOpen or CanOpen_Fridge object by distance
        public string UniqueIDOfClosestVisibleOpenableObject()
		{
			string objectID = null;
            
            foreach (SimObjPhysics o in VisibleSimObjPhysics)
            {
				if(o.GetComponent<CanOpen>())
				{
					objectID = o.UniqueID;
					break;
				}

				else if(o.GetComponent<CanOpen_Object>())
				{
					objectID = o.UniqueID;
					break;
				}
            }

            return objectID;
		}

		protected SimObjPhysics[] GetAllVisibleSimObjPhysics(Camera agentCamera, float maxDistance)
        {
            List<SimObjPhysics> currentlyVisibleItems = new List<SimObjPhysics>();

            Vector3 agentCameraPos = agentCamera.transform.position;

			//get all sim objects in range around us that have colliders in layer 8 (visible), ignoring objects in the SimObjInvisible layer
            //this will make it so the receptacle trigger boxes don't occlude the objects within them.
			Collider[] colliders_in_view = Physics.OverlapSphere(agentCameraPos, maxDistance * DownwardViewDistance,
                                                         1 << 8, QueryTriggerInteraction.Collide); //layermask is 8, ignores layer 9 which is SimObjInvisible

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

                        //now we have a reference to our sim object 
                        if(sop)
                        {

                            //check against all visibility points, accumulate count. If at least one point is visible, set object to visible
                            if (sop.VisibilityPoints.Length > 0)
                            {
                                Transform[] visPoints = sop.VisibilityPoints;
                                int visPointCount = 0;

                                foreach (Transform point in visPoints)
                                {

                                    //if this particular point is in view...
                                    if (CheckIfVisibilityPointInViewport(sop, point, agentCamera, maxDistance, false))
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
            }
            
            //check against anything in the invisible layers that we actually want to have occlude things in this round.
            //normally receptacle trigger boxes must be ignored from the visibility check otherwise objects inside them will be occluded, but
            //this additional check will allow us to see inside of receptacle objects like cabinets/fridges by checking for that interior
            //receptacle trigger box. Oh boy!
			Collider[] invisible_colliders_in_view = Physics.OverlapSphere(agentCameraPos, maxDistance * DownwardViewDistance,
                                                         1 << 9, QueryTriggerInteraction.Collide);
			if (invisible_colliders_in_view != null)
            {
                foreach (Collider item in invisible_colliders_in_view)
                {
                    if (item.tag == "Receptacle")
                    {
                        SimObjPhysics sop;

						sop = item.GetComponentInParent<SimObjPhysics>();

                        //now we have a reference to our sim object 
                        if (sop)
                        {
                            //check against all visibility points, accumulate count. If at least one point is visible, set object to visible
                            if (sop.VisibilityPoints.Length > 0)
                            {
                                Transform[] visPoints = sop.VisibilityPoints;
                                int visPointCount = 0;

                                foreach (Transform point in visPoints)
                                {

                                    //if this particular point is in view...
                                    if (CheckIfVisibilityPointInViewport(sop, point, agentCamera, maxDistance, true))
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
            }
            
            //populate array of visible items in order by distance
            currentlyVisibleItems.Sort((x, y) => Vector3.Distance(x.transform.position, agentCameraPos).CompareTo(Vector3.Distance(y.transform.position, agentCameraPos)));
            return currentlyVisibleItems.ToArray();
        }
        
		protected bool CheckIfVisibilityPointInViewport(SimObjPhysics sop, Transform point, Camera agentCamera, float maxDistance, bool includeInvisible)
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

                //check raycast against both visible and invisible layers, to check against ReceptacleTriggerBoxes which are normally
                //ignored by the other raycast
				if(includeInvisible)
				{
					if(Physics.Raycast(agentCamera.transform.position, point.position - agentCamera.transform.position, out hit, 
					                   maxDistance, (1 << 8 )| (1 << 9)))//layer mask automatically excludes Agent from this check. bit shifts are weird
                    {
                        if(hit.transform != sop.transform)
                        {
							result = false;
                        }

                        //if this line is drawn, then this visibility point is in camera frame and not occluded
                        //might want to use this for a targeting check as well at some point....
                        else
                        {
                            result = true;
							sop.isInteractable = true;

                            #if UNITY_EDITOR
                            Debug.DrawLine(agentCamera.transform.position, point.position, Color.cyan);
                            #endif
                        }               
                    }  
				}

				//only check against the visible layer, ignore the invisible layer
                //so if an object ONLY has colliders on it that are not on layer 8, this raycast will go through them 
				else
				{
        			if(Physics.Raycast(agentCamera.transform.position, point.position - agentCamera.transform.position, out hit, 
        				                   maxDistance , 1<<8 ))//layer mask automatically excludes Agent from this check
                    {
                        
                        //we didnt' directly hit the sop we are checking for with this cast, 
						//check if it's because we hit something see-through
                        if(hit.transform != sop.transform)
                        {
							if(hit.transform.GetComponent<SimObjPhysics>())
							{
								if (hit.transform.GetComponent<SimObjPhysics>().
                                    DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanSeeThrough))
                                {
                                    Transform firstseethroughhit = hit.transform;

                                    //we hit something see through, so now find all objects in the path between
                                    //the sop and the camera
                                    RaycastHit[] hits;
                                    hits = Physics.RaycastAll(agentCamera.transform.position, point.position - agentCamera.transform.position,
                                                              maxDistance, (1 << 8), QueryTriggerInteraction.Ignore);

                                    //now we need to check every object hit to see if it is the object we are looking for
                                    foreach (RaycastHit h in hits)
                                    {
										//found the object we are looking for, great!
                                        if (h.transform == sop.transform)
                                        {
											//do a raycast originating from the found object to the camera
                                            if (Physics.Raycast(point.position, agentCamera.transform.position - point.position, out hit,
                                                                maxDistance, 1 << 8))
                                            {
            								    //see if you hit the see through object between this object and the camera
												//if you didn't hit the see-through object, it means something else must be occluding
												//the object we are checking for, so don't change the result it's still false
												if (hit.transform.GetComponent<SimObjPhysics>())
												{
													if (hit.transform.GetComponent<SimObjPhysics>().
                                                    DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanSeeThrough))
                                                    {
                                                        if (hit.transform == firstseethroughhit.transform)
                                                            result = true;

														///XXX THE ELSE BELOW SHOULD BE REPLACED BY A RECURSIVE FUNCTION THAT ENDS WHEN THE OUTERMOST
														/// TRANSLUCENT OBJECT IS HIT. I'M BAD AT RECURSION SO THIS IS SUPER TEMPORARY
														/// right now this check only goes a few layers deep so it might leave some edge cases.

														//else
                                                        //result = TranslucentCheck(hit.point, sop, agentCamera, maxDistance, firstseethroughhit);

                                                        else
                                                        {
                                                            //result = TranslucentCheck(hit, sop, agentCamera, maxDistance, firstseethroughhit);

                                                            //do another raycast from hit.point to agent camera,
                                                            if (Physics.Raycast(hit.point, agentCamera.transform.position - hit.point, out hit,
                                                                                maxDistance, 1 << 8))
                                                            {
																if (hit.transform.GetComponent<SimObjPhysics>())
																{
																	if (hit.transform.GetComponent<SimObjPhysics>().
                                                                    DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanSeeThrough))
                                                                    {
                                                                        if (hit.transform == firstseethroughhit.transform)
                                                                            result = true;

                                                                        else
                                                                        {                                                         
                                                                            //do another raycast from hit.point to agent camera,
                                                                            if (Physics.Raycast(hit.point, agentCamera.transform.position - hit.point, out hit,
                                                                                                maxDistance, 1 << 8))
                                                                            {
																				if (hit.transform.GetComponent<SimObjPhysics>())
																				{
																					if (hit.transform.GetComponent<SimObjPhysics>().
                                                                                    DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanSeeThrough))
                                                                                    {
                                                                                        if (hit.transform == firstseethroughhit.transform)
                                                                                            result = true;                                                                  
                                                                                    }
																				}

                                                                            }
                                                                        }
                                                                    }
																}                                                
                                                            }
                                                        }
                                                    }
												}
                                            }
                                        }
                                    }
                                }
							}                     
						}
                        //i'm so sorry the above is so ugly we'll fix it later i promise

						//if this line is drawn, then this visibility point is in camera frame and not occluded
                        //might want to use this for a targeting check as well at some point....
                        else
                        {
                            result = true;
							sop.isInteractable = true;
                        }                   
                    }
                }  
			}
         
            //the point is not even in the viewport so it's
            else {
                result = false;
            }

			#if UNITY_EDITOR
            if(result==true)
			{            
                Debug.DrawLine(agentCamera.transform.position, point.position, Color.cyan);
			}
			#endif

            return result;

        }
        
        //XXX please help me i'm bad at recursion - Winson
		public bool TranslucentCheck(Vector3 hitpoint, SimObjPhysics sop, Camera agentCamera, float maxDistance, Transform firstseethroughhit)
		{
			RaycastHit hit;
			//bool result = false;
			//Transform lastThingHit = hit.transform;
			//do another raycast from hit.point to agent camera,
            if (Physics.Raycast(hitpoint, agentCamera.transform.position - hitpoint, out hit,
                                maxDistance, 1 << 8))
            {
				if(hit.transform.GetComponent<SimObjPhysics>())
				{
					if (hit.transform.GetComponent<SimObjPhysics>().
                    DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanSeeThrough))
                    {
                        if (hit.transform == firstseethroughhit.transform)
						{
							return true;
                        }                  
                    }
				}            
            }

            return TranslucentCheck(hit.point, sop, agentCamera, maxDistance, firstseethroughhit);;
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
                        //also ignore Untagged because the Transparent_RB of transparent objects need to be ignored for movement
                        //the actual rigidbody of the SimObjPhysics parent object of the transparent_rb should block correctly by having the
						//checkMoveAction() in the BaseFPSAgentController fail when the agent collides and gets shoved back
						if (res.transform.tag == "Player" || res.transform.tag == "Untagged")
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
						//including "Untagged" tag here so that the agent can't move through objects that are transparent
						if (res.transform.GetComponent<SimObjPhysics>() || res.transform.tag == "Structure" || res.transform.tag == "Untagged")
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
        public bool CheckIfAgentCanMoveHand(Vector3 targetPosition)
		{
			bool result = false;

            //first check if we have anything in our hand, if not then no reason to move hand
			if (ItemInHand == null)
            {
                Debug.Log("Agent can only move hand if holding an item");
                result = false;
                return result;
            }

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

            //XXX this does not check whether the object will still be visible when moving, so this will allow the agent to
            //move an object behind a door, causing the object to no longer be visible. Not sure if we should have a check
            //to restrict this yet, but about here is where that should go
            if (vp.z < 0 || vp.x > 1.0f || vp.x < 0.0f || vp.y > 1.0f || vp.y < 0.0f)
            {
                Debug.Log("The target position is not in the Are of the Agent's Viewport!");
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
        protected bool moveHandToXYZ(float x, float y, float z)
        {
            Vector3 targetPosition = new Vector3(x, y, z);
			if (CheckIfAgentCanMoveHand(targetPosition))
			{
				//Debug.Log("Movement of Agent Hand holding " + ItemInHand.name + " succesful!");
                AgentHand.transform.position = targetPosition;
				SetUpRotationBoxChecks();
                IsHandDefault = false;
                return true;
			} else {
                return false;
            }
        }

        // Moves hand relative the agent (but not relative the camera, i.e. up is up)
        // x, y, z coordinates should specify how far to move in that direction, so
        // x=.1, y=.1, z=0 will move the hand .1 in both the x and y coordinates.
        public void MoveHand(ServerAction action)
		{
			//get new direction relative to Agent forward facing direction (not the camera)
            Vector3 newPos = AgentHand.transform.position + 
                                transform.forward * action.z + 
			                    transform.right * action.x + 
								transform.up * action.y;

            actionFinished(moveHandToXYZ(newPos.x, newPos.y, newPos.z));
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

            actionFinished(moveHandToXYZ(newPos.x, newPos.y, newPos.z));
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
                    errorMessage = "Reset Hand to default position before attempting to Pick Up objects";
                    Debug.Log(errorMessage);
                    actionFinished(false);
                    //return false;
                }

                SimObjPhysics target = null;

                if (action.forceAction) {
                    action.forceVisible = true;
                }

                SimObjPhysics[] simObjPhysicsArray = VisibleSimObjs(action);
                // if (action.forceVisible) {
                //     simObjPhysicsArray = VisibleSimObjs(action);
                // }
                // TODO: Seems smart to reuse computation here but doing
                // so actually makes it impossible to see if an object is
                // interactable or not.
                // else {
                //     simObjPhysicsArray = VisibleSimObjPhysics;
                // }
                
                foreach (SimObjPhysics sop in simObjPhysicsArray)
                {
                    if (action.objectId == sop.UniqueID)
                    {
                        target = sop;
                    }
                }

                //GameObject target = GameObject.Find(action.objectId);
                if (target == null)
                {
                    errorMessage = "No valid target to pickup";
                    Debug.Log(errorMessage);
                    actionFinished(false);
                    return;
                }

                if (!target.GetComponent<SimObjPhysics>())
                {
                    errorMessage = "Target must be SimObjPhysics to pickup";
                    Debug.Log(errorMessage);
                    actionFinished(false);
                    return;
                }

                if (target.PrimaryProperty != SimObjPrimaryProperty.CanPickup)
                {
                    errorMessage = "Only SimObjPhysics that have the property CanPickup can be picked up";
                    Debug.Log(errorMessage);
                    actionFinished(false);
                    return;
                    //return false;
                }

				if(!action.forceAction && target.isInteractable == false)
				{
                    errorMessage = "Target is not interactable and is probably occluded by something!";
					Debug.Log(errorMessage);
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
                // TODO: The bellow check doesn't work because not all receptacle's are tagged as such
                // and thus objects are reporting that they are colliding when they aren't. It doesn't
                // seem clear to me that this check is really necessary though.
                // if (!action.forceAction && ItemInHand.GetComponent<SimObjPhysics>().isColliding)
                // {
                //     errorMessage = ItemInHand.transform.name + " can't be dropped. It must be clear of all other objects first";
                //     Debug.Log(errorMessage);
				// 	actionFinished(false);
				// 	return;
                // } else 
                // {
                ItemInHand.GetComponent<Rigidbody>().isKinematic = false;
                ItemInHand.transform.parent = null;
                ItemInHand = null;

                ServerAction a = new ServerAction();
                DefaultAgentHand(a);

                actionFinished(true);
                return;
                // }
            }

            else
            {
                errorMessage = "nothing in hand to drop!";
                Debug.Log(errorMessage);
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

        public void ObjectsInBox(ServerAction action) {
			HashSet<string> objectIds = new HashSet<string>();

			Collider[] colliders = Physics.OverlapBox(
				new Vector3(action.x, 0f, action.z),
				new Vector3(0.125f, 10f, 0.125f),
				Quaternion.identity
			);
			foreach (Collider c in colliders) {
				SimObjPhysics so = ancestorSimObjPhysics(c.transform.gameObject);
				if (so != null) {
					objectIds.Add(so.UniqueID);
				}
			}
			objectIdsInBox = new string[objectIds.Count];
			objectIds.CopyTo(objectIdsInBox);
			actionFinished(true);
		}

        private void UpdateDisplayGameObject(GameObject go, bool display) {
			if (go != null) {
				foreach (MeshRenderer mr in go.GetComponentsInChildren<MeshRenderer> () as MeshRenderer[]) {
                    if (!initiallyDisabledRenderers.Contains(mr.GetInstanceID())) {
                        mr.enabled = display;
                    }
				}
			}
		}

		public void ToggleMapView(ServerAction action) {
			if (inTopLevelView) {
				inTopLevelView = false;
				m_Camera.orthographic = false;
				m_Camera.transform.localPosition = lastLocalCameraPosition;
				m_Camera.transform.localRotation = lastLocalCameraRotation;
				UpdateDisplayGameObject(GameObject.Find("Ceiling"), true);
			} else {
				inTopLevelView = true;
				lastLocalCameraPosition = m_Camera.transform.localPosition;
				lastLocalCameraRotation = m_Camera.transform.localRotation;
				
				Bounds b = new Bounds(Vector3.zero, Vector3.zero);
				foreach (Renderer r in GameObject.FindObjectsOfType<Renderer>()) {
					b.Encapsulate(r.bounds);
				}
				float midX = (b.max.x + b.min.x) / 2.0f;
				float midZ = (b.max.z + b.min.z) / 2.0f;
				m_Camera.transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
				m_Camera.transform.position = new Vector3(midX, b.max.y, midZ);
				m_Camera.orthographic = true;

				m_Camera.orthographicSize = Math.Max((b.max.x - b.min.x) / 2f, (b.max.z - b.min.z) / 2f);
				
				cameraOrthSize = m_Camera.orthographicSize;
				UpdateDisplayGameObject(GameObject.Find("Ceiling"), false);
			}
			actionFinished(true);
		}

        private bool closeObject(SimObjPhysics target) {
            CanOpen co = target.GetComponent<CanOpen>();
            CanOpen_Object codd = target.GetComponent<CanOpen_Object>();
            if (co) {
                //if object is open, close it
                if (co.isOpen) {
                    co.Interact();
                    return true;
                }
			} else if (codd) {
                //if object is open, close it
                if (codd.isOpen) {
                    codd.Interact();
                    return true;
                }
			}
            return false;
        }

        public void CloseVisibleObjects(ServerAction action) {
            List<CanOpen> cos = new List<CanOpen>();
            List<CanOpen_Object> coos = new List<CanOpen_Object>();
			foreach (SimObjPhysics so in GetAllVisibleSimObjPhysics(m_Camera, 10f)) {
                CanOpen co = so.GetComponent<CanOpen>();
                CanOpen_Object coo = so.GetComponent<CanOpen_Object>();
                if (co) {
                    //if object is open, add it to be closed.
                    if (co.isOpen) {
                        cos.Add(co);
                    }
                } else if (coo) {
                    //if object is open, add it to be closed.
                    if (coo.isOpen) {
                        coos.Add(coo);
                    }
                }
            }
			StartCoroutine(InteractAndWait(cos, coos));
		}

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

            if (action.forceAction) {
                action.forceVisible = true;
            }

            foreach (SimObjPhysics sop in VisibleSimObjs(action))
            {
    //            //print("why not?");
    //            //check for object in current visible objects, and also check that it's interactable
				//if(!IgnoreInteractableFlag)
				//{
				//	if (!sop.isInteractable)
    //                {
    //                    Debug.Log(sop.UniqueID + " is not Interactable");
    //                    return;
    //                }
				//}

				if (sop.GetComponent<CanOpen>()|| sop.GetComponent<CanOpen_Object>())
                {
                    //print("wobbuffet");
                    target = sop;
                }

            }
            
            if (target)
            {
				if(!action.forceAction && target.isInteractable == false)
				{
                    errorMessage = "object is visible but occluded by something: " + action.objectId;
					Debug.Log(errorMessage);
                    actionFinished(false);
				}

				if(target.GetComponent<CanOpen>())
				{
					CanOpen co = target.GetComponent<CanOpen>();

                    //if object is open, close it
                    if (co.isOpen)
                    {
                        // co.Interact();
                        // actionFinished(true);
                        StartCoroutine(InteractAndWait(co, null));
                    }

                    else
                    {
                        Debug.Log("can't close object if it's already closed");
                        actionFinished(false);
                        errorMessage = "object already open: " + action.objectId;
                    }
				}

				else if(target.GetComponent<CanOpen_Object>())
				{
					CanOpen_Object codd = target.GetComponent<CanOpen_Object>();

                    //if object is open, close it
                    if (codd.isOpen)
                    {
                        // codd.Interact();
                        // actionFinished(true);
                        StartCoroutine(InteractAndWait(null, codd));
                    }

                    else
                    {
                        Debug.Log("can't close object if it's already closed");
                        actionFinished(false);
                        errorMessage = "object already open: " + action.objectId;
                    }
				}
              
            } 

			else 
			{
				Debug.Log("Target object not in sight");
                actionFinished(false);
                errorMessage = "object not found: " + action.objectId;
            }
		}

        private SimObjPhysics ancestorSimObjPhysics(GameObject go) {
			if (go == null) {
				return null;
			}
			SimObjPhysics so = go.GetComponent<SimObjPhysics>();
			if (so != null) {
				return so;
			} else if (go.transform.parent != null) {
				return ancestorSimObjPhysics(go.transform.parent.gameObject);
			} else {
				return null;
			}
		}

        private void OpenOrCloseObjectAtLocation(bool open, ServerAction action) {
            float x = action.x;
			float y = 1.0f - action.y;
            Ray ray = m_Camera.ViewportPointToRay(new Vector3(x, y, 0.0f));
            RaycastHit hit;
            int layerMask = 3 << 8;
            bool raycastDidHit = Physics.Raycast(ray, out hit, 10f, layerMask);
            if (!raycastDidHit) {
                Debug.Log("There don't seem to be any objects in that area.");
                errorMessage = "No openable object at location.";
                actionFinished(false);
                return;
            } 
            SimObjPhysics so = ancestorSimObjPhysics(hit.transform.gameObject);
            if (so != null && hit.distance < maxVisibleDistance) {
                action.objectId = so.UniqueID;
                action.forceAction = true;
                if (open) {
                    OpenObject(action);
                } else {
                    CloseObject(action);
                }
            } else if (so == null) {
                Debug.Log("Object at location is not interactable.");
                errorMessage = "Object at location is not interactable.";
                actionFinished(false);
            } else {
                Debug.Log("Object at location is too far away.");
                errorMessage = "Object at location is too far away.";
                actionFinished(false);
            }
        }

        public void OpenObjectAtLocation(ServerAction action) {
            OpenOrCloseObjectAtLocation(true, action);
            return;
        }

        public void CloseObjectAtLocation(ServerAction action) {
            OpenOrCloseObjectAtLocation(false, action);
            return;
        }

        protected IEnumerator InteractAndWait(CanOpen co, CanOpen_Object coo) {
            bool ignoreAgentInTransition = true;

            List<Collider> collidersDisabled = new List<Collider>();
            if (ignoreAgentInTransition) {    
                foreach(Collider c in this.GetComponentsInChildren<Collider>()) {
                    if (c.enabled) {
                        collidersDisabled.Add(c);
                        c.enabled = false;
                    }
                }
            }

            bool success = false;
            if (co != null) {
                co.Interact();
            }
            if (coo != null) {
                coo.Interact();
            }
            for (int i = 0; i < 1000; i++) {
                if ((co != null && co.GetiTweenCount() == 0) || 
                    (coo != null && coo.GetiTweenCount() == 0)) {
                    success = true;
                    break;
                }
                yield return null;
            }

            if (ignoreAgentInTransition) {
                foreach(Collider c in collidersDisabled) {
                    c.enabled = true;
                }
                for (int i = 0; i < 5; i++) {
                    yield return null;
                }

                if ((co != null && co.GetiTweenCount() != 0) || 
                    (coo != null && coo.GetiTweenCount() != 0)) {
                    success = false;
                    for (int i = 0; i < 1000; i++) {
                        if ((co != null && co.GetiTweenCount() == 0) || 
                            (coo != null && coo.GetiTweenCount() == 0)) {
                            break;
                        }
                    }
                    yield return null;
                }
            }

            if (!success) {
                errorMessage = "Object failed to open/close successfully.";
                Debug.Log(errorMessage);
            }
            
            actionFinished(success);
        }


        protected bool anyInteractionsStillRunning(List<CanOpen> cos, List<CanOpen_Object> coos) {
            bool anyStillRunning = false;
            foreach (CanOpen co in cos) {
                if (co.GetiTweenCount() != 0) {
                    anyStillRunning = true;
                    break;
                }
            }
            if (!anyStillRunning) {
                foreach (CanOpen_Object coo in coos) {
                    if (coo.GetiTweenCount() != 0) {
                        anyStillRunning = true;
                        break;
                    }
                }
            }
            return anyStillRunning;
        }

        protected IEnumerator InteractAndWait(List<CanOpen> cos, List<CanOpen_Object> coos) {
            bool ignoreAgentInTransition = true;

            List<Collider> collidersDisabled = new List<Collider>();
            if (ignoreAgentInTransition) {    
                foreach(Collider c in this.GetComponentsInChildren<Collider>()) {
                    if (c.enabled) {
                        collidersDisabled.Add(c);
                        c.enabled = false;
                    }
                }
            }

            foreach (CanOpen co in cos) {
                co.Interact();
            }
            foreach (CanOpen_Object coo in coos) {
                coo.Interact();
            }

            for (int i = 0; anyInteractionsStillRunning(cos, coos) && i < 1000; i++) {
                yield return null;
            }

            if (ignoreAgentInTransition) {
                foreach (Collider c in collidersDisabled) {
                    c.enabled = true;
                }
                yield return null;

                for (int i = 0; anyInteractionsStillRunning(cos, coos) && i < 1000; i++) {
                    yield return null;
                }
            }

            actionFinished(true);
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

            if (action.forceAction) {
                action.forceVisible = true;
            }

            foreach (SimObjPhysics sop in VisibleSimObjs(action))
            {
                //check for CanOpen drawers, cabinets or CanOpen_Fridge fridge objects
				if (sop.GetComponent<CanOpen>() || sop.GetComponent<CanOpen_Object>())
                {
                    target = sop;
                }

            }

			if (target)
			{
				if (!action.forceAction && target.isInteractable == false)
                {
                    Debug.Log("can't close object if it's already closed");
                    actionFinished(false);
                    errorMessage = "object is visible but occluded by something: " + action.objectId;
                    return;
                }

				if(target.GetComponent<CanOpen>())
				{
					CanOpen co = target.GetComponent<CanOpen>();

                    //check to make sure object is closed
                    if (co.isOpen)
                    {
                        Debug.Log("can't open object if it's already open");
                        errorMessage = "object already open";
                        actionFinished(false);
                    }

                    else
                    {
                        //pass in percentage open if desired
                        if (action.moveMagnitude > 0.0f)
                        {
                            co.SetOpenPercent(action.moveMagnitude);
                        }

                        // co.Interact();
                        StartCoroutine(InteractAndWait(co, null));
                    }
				}
                
				else if(target.GetComponent<CanOpen_Object>())
				{
					CanOpen_Object codd = target.GetComponent<CanOpen_Object>();

                    //check to make sure object is closed
                    if (codd.isOpen)
                    {
                        Debug.Log("can't open object if it's already open");
                        errorMessage = "object already open";
                        actionFinished(false);
                    }

                    else
                    {
                        //pass in percentage open if desired
                        if (action.moveMagnitude > 0.0f)
                        {
                            codd.SetOpenPercent(action.moveMagnitude);
                        }

                        // codd.Interact();
                        StartCoroutine(InteractAndWait(null, codd));
                    }
				}

                print("Happens");
			}

            //target not found in currently visible objects, report not found
			else
            {
				Debug.Log("Target object not in sight");
                actionFinished(false);
                errorMessage = "object not found: " + action.objectId;
            }
		}

        //
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
                //XXX It looks like this goes right into the MetaData, so basically this just returns a list of strings
				//that are the unique ID's of every object that is contained by the target object
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

        ////////////////////////////////////////
        ////// HIDING AND MASKING OBJECTS //////
        ////////////////////////////////////////

        private void HideAll() {
			foreach (GameObject go in GameObject.FindObjectsOfType<GameObject>()) {
				UpdateDisplayGameObject(go, false);
			}
		}

		private void UnhideAll() {
			foreach (GameObject go in GameObject.FindObjectsOfType<GameObject>()) {
				UpdateDisplayGameObject(go, true);
			}
		}

		protected void HideAllObjectsExcept(ServerAction action) {
			foreach (GameObject go in UnityEngine.Object.FindObjectsOfType<GameObject>()) {
				UpdateDisplayGameObject(go, false);
			}
            if (uniqueIdToSimObjPhysics.ContainsKey(action.objectId)) {
                UpdateDisplayGameObject(uniqueIdToSimObjPhysics[action.objectId].gameObject, true);
            }
			actionFinished(true);
		}

		public void HideObject(ServerAction action) {
			if (uniqueIdToSimObjPhysics.ContainsKey(action.objectId)) {
				UpdateDisplayGameObject(uniqueIdToSimObjPhysics[action.objectId].gameObject, false);
				actionFinished(true);
			} else {
				errorMessage = "No object with given id could be found to hide.";
				actionFinished(false);
			}
		}
		
		public void UnhideObject(ServerAction action) {
			if (uniqueIdToSimObjPhysics.ContainsKey(action.objectId)) {
				UpdateDisplayGameObject(uniqueIdToSimObjPhysics[action.objectId].gameObject, true);
				actionFinished(true);
			} else {
				errorMessage = "No object with given id could be found to unhide.";
				actionFinished(false);
			}
		}

		public void HideAllObjects(ServerAction action) {
			HideAll();
			actionFinished(true);
		}

		public void UnhideAllObjects(ServerAction action) {
			UnhideAll();
			actionFinished(true);
		}

        protected void MaskSimObj(SimObjPhysics so, Material mat) {
			if (!maskedObjects.ContainsKey(so.UniqueID)) {
				HashSet<MeshRenderer> renderersToSkip = new HashSet<MeshRenderer>();
				foreach (SimObjPhysics childSo in so.GetComponentsInChildren<SimObjPhysics>()) {
					if (so.UniqueID != childSo.UniqueID) {
						foreach (MeshRenderer mr in childSo.GetComponentsInChildren<MeshRenderer>()) {
							renderersToSkip.Add(mr);
						}
					}
				}
				Dictionary<int, Material[]> dict = new Dictionary<int, Material[]>();
				foreach (MeshRenderer r in so.gameObject.GetComponentsInChildren<MeshRenderer>() as MeshRenderer[]) {
					if (!renderersToSkip.Contains(r)) {
						dict[r.GetInstanceID()] = r.materials;
						Material[] newMaterials = new Material[r.materials.Length];
						for (int i = 0; i < newMaterials.Length; i++) {
							newMaterials[i] = new Material(mat);
						}
						r.materials = newMaterials;
					}
				}
				maskedObjects[so.UniqueID] = dict;
			}
		}

		protected void MaskSimObj(SimObjPhysics so, Color color) {
			if (!maskedObjects.ContainsKey(so.UniqueID)) {
				Material material = new Material(Shader.Find("Unlit/Color"));
				material.color = color;
				MaskSimObj(so, material);
			}
		}

		protected void UnmaskSimObj(SimObjPhysics so) {
			if (maskedObjects.ContainsKey(so.UniqueID)) {
				foreach (MeshRenderer r in so.gameObject.GetComponentsInChildren<MeshRenderer> () as MeshRenderer[]) {
					if (r != null) {
						if (maskedObjects[so.UniqueID].ContainsKey(r.GetInstanceID())) {
							r.materials = maskedObjects[so.UniqueID][r.GetInstanceID()];
						}
					}
				}
				maskedObjects.Remove(so.UniqueID);
			}
		}

		public void EmphasizeObject(ServerAction action) {
            foreach(KeyValuePair<string, SimObjPhysics> entry in uniqueIdToSimObjPhysics)
            {
                Debug.Log(entry.Key);
                Debug.Log(entry.Key == action.objectId);
            }

            if (uniqueIdToSimObjPhysics.ContainsKey(action.objectId)) {
                HideAll();
                UpdateDisplayGameObject(uniqueIdToSimObjPhysics[action.objectId].gameObject, true);
                MaskSimObj(uniqueIdToSimObjPhysics[action.objectId], Color.magenta);
                actionFinished(true);
            } else {
                errorMessage = "No object with id: " + action.objectId;
                Debug.Log(errorMessage);
                actionFinished(false);
            }
		}

		public void UnemphasizeAll(ServerAction action) {
			UnhideAll();
			foreach (SimObjPhysics so in GameObject.FindObjectsOfType<SimObjPhysics>()) {
				UnmaskSimObj(so);
            }
			actionFinished(true);
		}

		public void MaskObject(ServerAction action) {
            if (uniqueIdToSimObjPhysics.ContainsKey(action.objectId)) {
                MaskSimObj(uniqueIdToSimObjPhysics[action.objectId], Color.magenta);
                actionFinished(true);
            } else {    
                Debug.Log("No such object with id: " + action.objectId);
                errorMessage = "No such object with id: " + action.objectId;
                actionFinished(false);
            }
		}

		public void UnmaskObject(ServerAction action) {
            if (uniqueIdToSimObjPhysics.ContainsKey(action.objectId)) {
                UnmaskSimObj(uniqueIdToSimObjPhysics[action.objectId]);
                actionFinished(true);
            } else {    
                Debug.Log("No such object with id: " + action.objectId);
                errorMessage = "No such object with id: " + action.objectId;
                actionFinished(false);
            }
		}

        ///////////////////////////////////////////
        ///// GETTING DISTANCES, NORMALS, ETC /////
        ///////////////////////////////////////////

        private bool NormalIsApproximatelyUp(Vector3 normal) {
			return (Math.Abs(normal.x) < .01) && 
					(Math.Abs(normal.y - 1) < .01) &&
					(Math.Abs(normal.z) < .01);
		}

		private bool AnythingAbovePosition(Vector3 position, float distance) {
			Vector3 up = new Vector3(0.0f, 1.0f, 0.0f);
			RaycastHit hit;
			return Physics.Raycast(position, up, out hit, distance);
		}

        private bool AnythingAbovePositionIgnoreObject(
            Vector3 position, 
            float distance, 
            int layerMask,
            GameObject toIgnore) {
			Vector3 up = new Vector3(0.0f, 1.0f, 0.0f);
			RaycastHit[] hits = Physics.RaycastAll(position, up, distance, layerMask);
            // Debug.Log("");
            // Debug.Log(toIgnore);
            foreach (RaycastHit hit in hits) {
                // Debug.Log(hit.collider);
                if (hit.collider.transform.gameObject != toIgnore) {
                    // Debug.Log("happens");
                    return true;
                }
            }
			return false;
		}

		private float[,,] initializeFlatSurfacesOnGrid(int yGridSize, int xGridSize) {
			float[,,] flatSurfacesOnGrid = new float[2, yGridSize, xGridSize];
			for (int i = 0; i < 2; i++) {
				for (int j = 0; j < yGridSize; j++) {
					for (int k = 0; k < xGridSize; k++) {
						flatSurfacesOnGrid[i,j,k] = float.PositiveInfinity;
					}
				}
			}
			return flatSurfacesOnGrid;
		}

		private void toggleColliders(IEnumerable<Collider> colliders) {
			foreach (Collider c in colliders) {
				c.enabled = !c.enabled;
			}
		}

		public void FlatSurfacesOnGrid(ServerAction action) {
			int xGridSize = (int) Math.Round(action.x, 0);
			int yGridSize = (int) Math.Round(action.y, 0);
			flatSurfacesOnGrid = initializeFlatSurfacesOnGrid(yGridSize, xGridSize);
			
			if (ItemInHand != null) {
				toggleColliders(ItemInHand.GetComponentsInChildren<Collider>());
			}

            int layerMask = 1 << 8;
			for (int i = 0; i < yGridSize; i++) {
				for (int j = 0; j < xGridSize; j++) {
					float x = j * (1.0f / xGridSize) + (0.5f / xGridSize);
					float y = (1.0f - (0.5f / yGridSize)) - i * (1.0f / yGridSize);
					Ray ray = m_Camera.ViewportPointToRay(new Vector3(x, y, 0));
					RaycastHit[] hits = Physics.RaycastAll(ray, 10f, layerMask);
					float minHitDistance = float.PositiveInfinity;
					foreach (RaycastHit hit in hits) {
						if (hit.distance < minHitDistance) {
							minHitDistance = hit.distance;
						}
					}
					foreach (RaycastHit hit in hits) {
						if (NormalIsApproximatelyUp(hit.normal) &&
					 		!AnythingAbovePosition(hit.point, 0.1f)) {
							if (hit.distance == minHitDistance) {
								flatSurfacesOnGrid[0, i, j] = minHitDistance;
							} else {
								flatSurfacesOnGrid[1, i, j] = Math.Min(
									flatSurfacesOnGrid[1, i, j], hit.distance
								);
							}
						}
					}
				}
			}
			if (ItemInHand != null) {
				toggleColliders(ItemInHand.GetComponentsInChildren<Collider>());
			}
			actionFinished(true);
		}

		public void GetMetadataOnGrid(ServerAction action) {
			int xGridSize = (int) Math.Round(action.x, 0);
			int yGridSize = (int) Math.Round(action.y, 0);
			distances = new float[yGridSize, xGridSize];
			normals = new float[3, yGridSize, xGridSize];
			isOpenableGrid = new bool[yGridSize, xGridSize];
			
			if (ItemInHand != null) {
				toggleColliders(ItemInHand.GetComponentsInChildren<Collider>());
			}

            int layerMask = 1 << 8;
			for (int i = 0; i < yGridSize; i++) {
				for (int j = 0; j < xGridSize; j++) {
					float x = j * (1.0f / xGridSize) + (0.5f / xGridSize);
					float y = (1.0f - (0.5f / yGridSize)) - i * (1.0f / yGridSize);
					Ray ray = m_Camera.ViewportPointToRay(new Vector3(x, y, 0));
					RaycastHit hit;
					if (Physics.Raycast(ray, out hit, 10f, layerMask)) {
						distances[i, j] = hit.distance;
						normals[0, i, j] = Vector3.Dot(transform.right, hit.normal);
						normals[1, i, j] = Vector3.Dot(transform.up, hit.normal);
						normals[2, i, j] = Vector3.Dot(transform.forward, hit.normal);
						SimObjPhysics so = hit.transform.gameObject.GetComponent<SimObjPhysics>();
						isOpenableGrid[i, j] = so != null && (
                            so.GetComponent<CanOpen>() || so.GetComponent<CanOpen_Object>()
                        );
					} else {
						distances[i, j] = float.PositiveInfinity;
						normals[0, i, j] = float.NaN;
						normals[1, i, j] = float.NaN;
						normals[2, i, j] = float.NaN;
						isOpenableGrid[i, j] = false;
					}
				}
			}

			if (ItemInHand != null) {
				toggleColliders(ItemInHand.GetComponentsInChildren<Collider>());
			}
			actionFinished(true);
		}

		public void SegmentVisibleObjects(ServerAction action) {
			if (ItemInHand != null) {
				toggleColliders(ItemInHand.GetComponentsInChildren<Collider>());
			}
			
			int k = 0;
			List<string> uniqueIds = new List<string>();
			foreach (SimObjPhysics so in GetAllVisibleSimObjPhysics(m_Camera, 100f)) {
				int i = (10 * k) / 256;
				int j = (10 * k) % 256;
				MaskSimObj(so, new Color32(Convert.ToByte(i), Convert.ToByte(j), 255, 255));
				uniqueIds.Add(so.UniqueID);
				k++;
			}
			segmentedObjectIds = uniqueIds.ToArray();

			if (ItemInHand != null) {
				toggleColliders(ItemInHand.GetComponentsInChildren<Collider>());
			}
			actionFinished(true);
		}


        ////////////////////////////
        ///// Crouch and Stand /////
        ////////////////////////////

        public void Crouch(ServerAction action) {
			if (m_Camera.transform.localPosition.y == 0.0f) {
				errorMessage = "Already crouching.";
				actionFinished(false);
			} else {
				m_Camera.transform.localPosition = new Vector3(
                    standingLocalCameraPosition.x, 
                    0.0f,
                    standingLocalCameraPosition.z
                );
				actionFinished(true);
			}
		}

		public void Stand(ServerAction action) {
			if ((m_Camera.transform.localPosition - standingLocalCameraPosition).magnitude < 0.1f) {
				errorMessage = "Already standing.";
				actionFinished(false);
			} else {
				m_Camera.transform.localPosition = standingLocalCameraPosition;
				actionFinished(true);
			}
		}
        
        ////////////////
        ///// MISC /////
        ////////////////

        public void ChangeFOV(ServerAction action) {
			m_Camera.fieldOfView = action.fov;
			actionFinished(true);
		}

        // public IEnumerator WaitOnResolutionChange(int width, int height) {
		// 	while (Screen.width != width || Screen.height != height) {
		// 		yield return null;
		// 	}
		// 	tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        //     readPixelsRect = new Rect(0, 0, Screen.width, Screen.height);
		// 	// yield return new WaitForSeconds(2.0F);
		// 	actionFinished(true);
		// }

        public IEnumerator WaitOnResolutionChange(int width, int height) {
			while (Screen.width != width || Screen.height != height) {
				yield return null;
			}
			actionFinished(true);
		}

        public void ChangeResolution(ServerAction action) {
			int height = Convert.ToInt32(action.y);
			int width = Convert.ToInt32(action.x);
			Screen.SetResolution(width, height, false);
			StartCoroutine(WaitOnResolutionChange(width, height));
		}

		public void ChangeQuality(ServerAction action) {
			string[] names = QualitySettings.names;
			for(int i = 0; i < names.Length; i++) {
				if (names[i] == action.quality) {
					QualitySettings.SetQualityLevel(i, true);
					break;
				}
			}
			actionFinished(true);
		}

        public void ChangeTimeScale(ServerAction action) {
            if (action.timeScale > 0) {
                Time.timeScale = action.timeScale;
                actionFinished(true);
            } else {
                errorMessage = "Time scale must be >0";
                Debug.Log(errorMessage);
                actionFinished(false);
            }
		}

        ///////////////////////////////////
        ///// DATA GENERATION HELPERS /////
        ///////////////////////////////////

        public void GetReachablePositions(ServerAction action) {
            CapsuleCollider cc = GetComponent<CapsuleCollider>();

            Vector3 center = transform.position;
            float fudgeFactor = 0.05f;
            float radius = cc.radius;
            float innerHeight = center.y - radius;

            Queue<Vector3> pointsQueue = new Queue<Vector3>();
            pointsQueue.Enqueue(center);

            Vector3[] directions = {
                new Vector3(1.0f, 0.0f, 0.0f),
                new Vector3(0.0f, 0.0f, -1.0f),
                new Vector3(-1.0f, 0.0f, 0.0f),
                new Vector3(0.0f, 0.0f, 1.0f)
            };

            HashSet<Vector3> goodPoints = new HashSet<Vector3>();
            int layerMask = 1 << 8;
            while (pointsQueue.Count != 0) {
                Vector3 p = pointsQueue.Dequeue();
                if (!goodPoints.Contains(p)) {
                    goodPoints.Add(p);
                    Vector3 point1 = new Vector3(p.x, center.y + innerHeight, p.z);
                    Vector3 point2 = new Vector3(p.x, center.y - innerHeight, p.z);
                    foreach (Vector3 d in directions) {    
                        RaycastHit[] hits = Physics.CapsuleCastAll(
                            point1,
                            point2,
                            radius,
                            d,
                            gridSize + fudgeFactor,
                            layerMask,
                            QueryTriggerInteraction.Ignore
                        );
                        bool shouldEnqueue = true;
                        foreach (RaycastHit hit in hits) {
                            if (!ancestorHasName(hit.transform.gameObject, "FPSController")) {
                                shouldEnqueue = false;
                                break;
                            }
                        }
                        if (shouldEnqueue) {
                            pointsQueue.Enqueue(p + d * gridSize);
                        }
                    }
                }
            }

            reachablePositions = new Vector3[goodPoints.Count];
            goodPoints.CopyTo(reachablePositions);

            actionFinished(true);
        }

        private bool ancestorHasName(GameObject go, string name) {
			if (go.name == name) {
				return true;
			} else if (go.transform.parent != null) {
				return ancestorHasName(go.transform.parent.gameObject, name);
			} else {
				return false;
			}
		}

        public void HideObscuringObjects(ServerAction action) {
			string objType = "";
			if (action.objectId != null && action.objectId != "") {
				string[] split = action.objectId.Split('|');
				if (split.Length != 0) {
					objType = action.objectId.Split('|')[0];
				}
			}
			int xGridSize = 100;
			int yGridSize = 100;
            int layerMask = 1 << 8;
			for (int i = 0; i < yGridSize; i++) {
				for (int j = 0; j < xGridSize; j++) {
					float x = j * (1.0f / xGridSize) + (0.5f / xGridSize);
					float y = (1.0f - (0.5f / yGridSize)) - i * (1.0f / yGridSize);
					Ray ray = m_Camera.ViewportPointToRay(new Vector3(x, y, 0));
					RaycastHit hit;
					while (true) {
						if (Physics.Raycast(ray, out hit, 10f, layerMask)) {
							UpdateDisplayGameObject(hit.transform.gameObject, false);
							SimObjPhysics hitObj = hit.transform.gameObject.GetComponentInChildren<SimObjPhysics>();
							if (hitObj != null && objType != "" && hitObj.UniqueID.Contains(objType)) {
								ray.origin = hit.point + ray.direction / 100f;
							} else {
								break;
							}
						} else {
							break;
						}
					}
				}
			}
			actionFinished(true);
		}

        private IEnumerator CoverSurfacesWithHelper(int n, List<SimObjPhysics> newObjects) {
			Vector3[] initialPositions = new Vector3[newObjects.Count];
			int k = 0;
			bool[] deleted = new bool[newObjects.Count];
			foreach (SimObjPhysics so in newObjects) {
				initialPositions[k] = so.transform.position;
				deleted[k] = false;
				k++;
			}
			for (int i = 0; i < n; i++) {
				k = 0;
				foreach (SimObjPhysics so in newObjects) {
					if (!deleted[k]) {
						float dist = Vector3.Distance(initialPositions[k], so.transform.position);
						if (dist > 0.5f) {
							deleted[k] = true;
                            so.gameObject.SetActive(false);
						}
					}
					k++;
				}
				yield return null;
			}

			Collider[] fpsControllerColliders = GameObject.Find("FPSController").GetComponentsInChildren<Collider>();
			foreach (SimObjPhysics so in newObjects) {
				so.GetComponentInChildren<Rigidbody>().isKinematic = true;
				foreach(Collider c1 in so.GetComponentsInChildren<Collider>()) {
					foreach(Collider c in fpsControllerColliders) {
						Physics.IgnoreCollision(c, c1);
					}
				}
                uniqueIdToSimObjPhysics[so.UniqueID] = so;
			}

			actionFinished(true);
		}

		private void createCubeSurrounding(Bounds bounds) {
			Vector3 center = bounds.center;
			Vector3 max = bounds.max;
			Vector3 min = bounds.min;
			float size = 0.001f;
			float offset = 0.0f;
			min.y = Math.Max(-1.0f, min.y);
			center.y = (max.y + min.y) / 2;
			float xLen = max.x - min.x;
			float yLen = max.y - min.y;
			float zLen = max.z - min.z;

			// Top
			GameObject cube = Instantiate(
				Resources.Load("BlueCube") as GameObject,
				new Vector3(center.x, max.y + offset + size / 2, center.z),
				Quaternion.identity
			) as GameObject;
			cube.transform.localScale = new Vector3(xLen + 2 * (size + offset), size, zLen + 2 * (size + offset));

			// Bottom
			cube = Instantiate(
				Resources.Load("BlueCube") as GameObject,
				new Vector3(center.x, min.y - offset - size / 2, center.z),
				Quaternion.identity
			) as GameObject;
			cube.transform.localScale = new Vector3(xLen + 2 * (size + offset), size, zLen + 2 * (size + offset));

			// z min
			cube = Instantiate(
				Resources.Load("BlueCube") as GameObject,
				new Vector3(center.x, center.y, min.z - offset - size / 2),
				Quaternion.identity
			) as GameObject;
			cube.transform.localScale = new Vector3(xLen + 2 * (size + offset), yLen + 2 * offset, size);

			// z max
			cube = Instantiate(
				Resources.Load("BlueCube") as GameObject,
				new Vector3(center.x, center.y, max.z + offset + size / 2),
				Quaternion.identity
			) as GameObject;
			cube.transform.localScale = new Vector3(xLen + 2 * (size + offset), yLen + 2 * offset, size);

			// x min
			cube = Instantiate(
				Resources.Load("BlueCube") as GameObject,
				new Vector3(min.x - offset - size / 2, center.y, center.z),
				Quaternion.identity
			) as GameObject;
			cube.transform.localScale = new Vector3(size, yLen + 2 * offset, zLen + 2 * offset);

			// x max
			cube = Instantiate(
				Resources.Load("BlueCube") as GameObject,
				new Vector3(max.x + offset + size / 2, center.y, center.z),
				Quaternion.identity
			) as GameObject;
			cube.transform.localScale = new Vector3(size, yLen + 2 * offset, zLen + 2 * offset);
		}

        private List<RaycastHit> RaycastWithRepeatHits(
            Vector3 origin, Vector3 direction, float maxDistance, int layerMask
            ) {
			List<RaycastHit> hits = new List<RaycastHit>();
			RaycastHit hit;
			bool didHit = Physics.Raycast(origin, direction, out hit, maxDistance, layerMask);
			while (didHit) {
				hits.Add(hit);
				origin = hit.point + direction / 100f;
				hit = new RaycastHit();
				didHit = Physics.Raycast(origin, direction, out hit, maxDistance, layerMask);
			}
			return hits;
		}

        public void SetAllObjectsToBlue(ServerAction action) {
            foreach(Renderer r in GameObject.FindObjectsOfType<Renderer>()) {
				Material newMaterial = (Material) Resources.Load("BLUE", typeof(Material));
				Material[] newMaterials = new Material[r.materials.Length];
				for (int i = 0; i < newMaterials.Length; i++) {
					newMaterials[i] = newMaterial;
				}
				r.materials = newMaterials;
			}
			foreach (Light l in GameObject.FindObjectsOfType<Light>()) {
				l.enabled = false;
			}
			RenderSettings.ambientMode = AmbientMode.Flat;
			RenderSettings.ambientLight = Color.white;
            actionFinished(true);
        }

        public void EnableFog(ServerAction action) {
            GlobalFog gf = m_Camera.GetComponent<GlobalFog>();
			gf.enabled = true;
            gf.heightFog = false;
			RenderSettings.fog = true;
			RenderSettings.fogMode = FogMode.Linear;
			RenderSettings.fogStartDistance = 0.0f;
            RenderSettings.fogEndDistance = action.z;
			RenderSettings.fogColor = Color.white;
			actionFinished(true);
		}

		public void DisableFog(ServerAction action) {
			m_Camera.GetComponent<GlobalFog>().enabled = false;
			RenderSettings.fog = false;
			actionFinished(true);
		}

		public void CoverSurfacesWith(ServerAction action) {
			string prefab = action.objectId.Split('|')[0];
			
			Bounds b = new Bounds(
                new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity),
                new Vector3(-float.PositiveInfinity, -float.PositiveInfinity, -float.PositiveInfinity)
            );
			foreach (Renderer r in GameObject.FindObjectsOfType<Renderer>()) {
				b.Encapsulate(r.bounds);
			}
			b.min = new Vector3(
				Math.Max(b.min.x, transform.position.x - 7),
				Math.Max(b.min.y, transform.position.y - 1.3f),
				Math.Max(b.min.z, transform.position.z - 7)
			);
			b.max = new Vector3(
				Math.Min(b.max.x, transform.position.x + 7),
				Math.Min(b.max.y, transform.position.y + 3),
				Math.Min(b.max.z, transform.position.z + 7)
			);
			createCubeSurrounding(b);

			float yMax = b.max.y - 0.2f;
			float xRoomSize = b.max.x - b.min.x;
			float zRoomSize = b.max.z - b.min.z;
			InstantiatePrefabTest script = GameObject.Find("PhysicsSceneManager").GetComponent<InstantiatePrefabTest>();
			SimObjPhysics objForBounds = script.Spawn(prefab, prefab + "|ToDestroy", new Vector3(0.0f, b.max.y + 10.0f, 0.0f));

			Bounds objBounds = new Bounds(
                new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity),
                new Vector3(-float.PositiveInfinity, -float.PositiveInfinity, -float.PositiveInfinity)
            );
			foreach (Renderer r in objForBounds.GetComponentsInChildren<Renderer>()) {
                objBounds.Encapsulate(r.bounds);
			}
			Vector3 objCenterRelPos = objBounds.center - objForBounds.transform.position;
            Vector3 yOffset = new Vector3(
                0f, 
                0.01f + objForBounds.transform.position.y - objBounds.min.y, 
                0f
            );
            objForBounds.gameObject.SetActive(false);

			float xExtent = objBounds.max.x - objBounds.min.x;
			float yExtent = objBounds.max.y - objBounds.min.y;
			float zExtent = objBounds.max.z - objBounds.min.z;
			float xStepSize = Math.Max(Math.Max(xExtent, 0.1f), action.x);
			float zStepSize = Math.Max(Math.Max(zExtent, 0.1f), action.z);
			int numXSteps = (int) (xRoomSize / xStepSize);
			int numZSteps = (int) (zRoomSize / zStepSize);
            // float xTmp = -0.153f;
            // float zTmp = -3f;
			Material redMaterial = (Material) Resources.Load("RED", typeof(Material));
			List<SimObjPhysics> newObjects = new List<SimObjPhysics>();

            var xsToTry = new List<float>();
            var zsToTry = new List<float>();
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>()) {
                if (go.name == "ReceptacleTriggerBox") {
                Vector3 receptCenter = go.transform.position;
                    xsToTry.Add(receptCenter.x);
                    zsToTry.Add(receptCenter.z);
                }
            }
            for (int i = 0; i < numXSteps; i++) {
                float x = b.min.x + (0.5f + i) * xStepSize;
                for (int j = 0; j < numZSteps; j++) {
                    float z = b.min.z + (0.5f + j) * zStepSize;
                    xsToTry.Add(x);
                    zsToTry.Add(z);
                }
            }
            var xsToTryArray = xsToTry.ToArray();
            var zsToTryArray = zsToTry.ToArray();

            int layerMask = 1 << 8;
            for (int i = 0; i < xsToTryArray.Length; i++) {
                float xPos = xsToTryArray[i];
                float zPos = zsToTryArray[i];
					
                List<RaycastHit> hits = RaycastWithRepeatHits(
                    new Vector3(xPos, yMax, zPos),
                    new Vector3(0.0f, -1.0f, 0.0f),
                    10f,
                    layerMask
                );
                int k = -1;
                foreach (RaycastHit hit in hits) {
                    if (b.Contains(hit.point) && 
                        hit.point.y < transform.position.y + 1.2f &&
                        hit.point.y >= transform.position.y - 1.1f &&
                        !AnythingAbovePositionIgnoreObject(
                            hit.point + new Vector3(0f, -0.01f, 0f), 
                            0.02f,
                            layerMask,
                            hit.collider.transform.gameObject)
                        ) {
                        Vector3 halfExtents = new Vector3(xExtent / 2.1f, yExtent / 2.1f, zExtent / 2.1f);
                        Vector3 center = hit.point + objCenterRelPos + yOffset;
                        Collider[] colliders = Physics.OverlapBox(center, halfExtents, Quaternion.identity, layerMask);
                        if (colliders.Length == 0) {
                            k++;
                            string id = Convert.ToString(i) + "|" + Convert.ToString(k);
                            SimObjPhysics newObj = script.Spawn(prefab, action.objectId + "|" + id, center - objCenterRelPos);
                            MaskSimObj(newObj, redMaterial);
                            newObjects.Add(newObj);
                        } 
                        // else {
                        //     Debug.Log("Intersects collider:");
                        //     Debug.Log(colliders[0]);
                        // }
                    }
                }
			}
            actionFinished(true);
			StartCoroutine(CoverSurfacesWithHelper(100, newObjects));
		}

		//////MASS SCALE AND SPAWNER FUNCTIONS///

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
                if (action.objectId == sop.UniqueID)
                {
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
                if (action.objectId == sop.UniqueID)
                {
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
                if (action.objectId == sop.UniqueID)
                {
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
                if (action.objectId == sop.UniqueID)
                {
                    target = sop;
                }

            }

            if (target)
            {
                //XXX this is where the metadata would be exported, this info right here
                List<SimObjPhysics> ObjectsOnScale = new List<SimObjPhysics>(target.GetComponent<MassScale>().ObjectsInRightScale());

                string result = "Right Scale Contains: ";

                foreach (SimObjPhysics sop in ObjectsOnScale)
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
                if (action.objectId == sop.UniqueID)
                {
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
							//if(!IgnoreInteractableFlag)
							//{
								if (o.isInteractable == true)
                                {
                                    suffix += " INTERACTABLE";
                                }
							//}

                        }
                  
                        GUILayout.Button(o.UniqueID + suffix, UnityEditor.EditorStyles.miniButton, GUILayout.MinWidth(100f));
                    }
                }
            }
        }
        #endif
	}

}

