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

using Priority_Queue;

using System.Linq;

namespace UnityStandardAssets.Characters.FirstPerson
{
	[RequireComponent(typeof (CharacterController))]   
	public class PhysicsRemoteFPSAgentController : BaseFPSAgentController
    {
		[SerializeField] protected GameObject[] ToSetActive = null;
        
		[SerializeField] protected float PhysicsAgentSkinWidth = -1f; //change agent's skin width so that it collides directly with ground - otherwise sweeptests will fail for flat objects on floor

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
        protected int actionIntReturn;
        protected float actionFloatReturn;
        protected bool actionBoolReturn;
        protected float[] actionFloatsReturn;

        protected Vector3[] actionVector3sReturn;
        protected string[] actionStringsReturn;
        [SerializeField] protected Vector3 standingLocalCameraPosition;
        [SerializeField] protected Vector3 crouchingLocalCameraPosition;
        protected HashSet<int> initiallyDisabledRenderers = new HashSet<int>();
        public Vector3[] reachablePositions = new Vector3[0];
        public Bounds sceneBounds = new Bounds(
            new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity),
            new Vector3(-float.PositiveInfinity, -float.PositiveInfinity, -float.PositiveInfinity)
        );

        //change visibility check to use this distance when looking down
		//protected float DownwardViewDistance = 2.0f;
    
        // Use this for initialization
        protected override void Start()
        {
			base.Start();

			//ServerAction action = new ServerAction();
			//Initialize(action);

			//below, enable all the GameObjects on the Agent that Physics Mode requires

            //physics requires max distance to be extended to be able to see objects on ground
			//maxVisibleDistance = MaxViewDistancePhysics;//default maxVisibleDistance is 1.0f

            if (PhysicsAgentSkinWidth < 0.0f) {
                Debug.LogError("Agent skin width must be > 0.0f, please set it in the editor. Forcing it to equal 0.01f for now.");
                PhysicsAgentSkinWidth = 0.01f;
            }
			m_CharacterController.skinWidth = PhysicsAgentSkinWidth;

			foreach (GameObject go in ToSetActive)
			{
				go.SetActive(true);
			}

			//On start, activate gravity
            Vector3 movement = Vector3.zero;
            movement.y = Physics.gravity.y * m_GravityMultiplier;
            m_CharacterController.Move(movement);

            standingLocalCameraPosition = m_Camera.transform.localPosition;
            crouchingLocalCameraPosition = m_Camera.transform.localPosition;
            crouchingLocalCameraPosition.y = 0.0f;

            foreach (SimObjPhysics so in GameObject.FindObjectsOfType<SimObjPhysics>()) {
                uniqueIdToSimObjPhysics[so.UniqueID] = so;
            }

            // Recordining initially disabled renderers and scene bounds 
            foreach (Renderer r in GameObject.FindObjectsOfType<Renderer>()) {
                if (!r.enabled) {
                    initiallyDisabledRenderers.Add(r.GetInstanceID());
                } else {
                    sceneBounds.Encapsulate(r.bounds);
                }
            }

            base.actionComplete = true;
        }

        public float WhatIsAgentsMaxVisibleDistance()
        {
            return maxVisibleDistance;
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
			//this is also mostly for in editor, the array of visible sim objects is found via server actions
			//using VisibleSimObjs(action), so be aware of that

            #if UNITY_EDITOR
            if (this.actionComplete) {
                ServerAction action = new ServerAction();
                VisibleSimObjPhysics = VisibleSimObjs(action);//GetAllVisibleSimObjPhysics(m_Camera, maxVisibleDistance);
            }
            #endif
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

        private ObjectMetadata ObjectMetadataFromSimObjPhysics(SimObjPhysics simObj, bool isVisible) {
            ObjectMetadata objMeta = new ObjectMetadata();
            GameObject o = simObj.gameObject;
            objMeta.name = o.name;
            objMeta.position = o.transform.position;
            objMeta.rotation = o.transform.eulerAngles;
            objMeta.objectType = Enum.GetName(typeof(SimObjType), simObj.Type);
            objMeta.receptacle = simObj.IsReceptacle;
            objMeta.openable = simObj.IsOpenable;
            if (objMeta.openable) {
                objMeta.isopen = simObj.IsOpen;
            }
            objMeta.pickupable = simObj.PrimaryProperty == SimObjPrimaryProperty.CanPickup;
            objMeta.objectId = simObj.UniqueID;

            // TODO: using the isVisible flag on the object causes weird problems
            // in the multiagent setting, explicitly giving this information for now.
            objMeta.visible = isVisible; //simObj.isVisible;

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
            SimObjPhysics[] visibleSimObjs = VisibleSimObjs(false); // Update visibility for all sim objects for this agent
            HashSet<SimObjPhysics> visibleSimObjsHash = new HashSet<SimObjPhysics>();
            foreach (SimObjPhysics sop in visibleSimObjs) {
                visibleSimObjsHash.Add(sop);
            }

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
				ObjectMetadata meta = ObjectMetadataFromSimObjPhysics(simObj, visibleSimObjsHash.Contains(simObj));
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
			metaMessage.objects = this.generateObjectMetadata();
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
            metaMessage.actionIntReturn = actionIntReturn;
            metaMessage.actionFloatReturn = actionFloatReturn;
            metaMessage.actionBoolReturn = actionBoolReturn;
            metaMessage.actionFloatsReturn = actionFloatsReturn;
            metaMessage.actionStringsReturn = actionStringsReturn;
            metaMessage.actionVector3sReturn = actionVector3sReturn;

            // Resetting things
            reachablePositions = new Vector3[0];
			flatSurfacesOnGrid = new float[0,0,0];
			distances = new float[0,0];
			normals = new float[0,0,0];
			isOpenableGrid = new bool[0,0];
            segmentedObjectIds = new string[0];
			objectIdsInBox = new string[0];
            actionIntReturn = 0;
            actionFloatReturn = 0.0f;
            actionBoolReturn = false;
            actionFloatsReturn = new float[0];
            actionStringsReturn = new string[0];
            actionVector3sReturn = new Vector3[0];
			
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
                if(o.GetComponent<CanOpen_Object>())
				{
					objectID = o.UniqueID;
					break;
				}
            }

            return objectID;
		}

        //return ID of closes toggleable object by distance
        public string UniqueIDOfClosestToggleObject()
        {
            string objectID = null;
            
            foreach (SimObjPhysics o in VisibleSimObjPhysics)
            {
                if(o.GetComponent<CanToggleOnOff>())
				{
					objectID = o.UniqueID;
					break;
				}
            }

            return objectID;
        }

        public string UniqueIDOfClosestReceptacleObject()
        {
            string objectID = null;

            foreach(SimObjPhysics o in VisibleSimObjPhysics)
            {
                if(o.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.Receptacle))
                {
                    objectID = o.UniqueID;
                    break;
                }
            }
            return objectID;
        }

        //return a reference to a SimObj that is Visible (in the VisibleSimObjPhysics array) and
        //matches the passe din objectID
        public GameObject FindObjectInVisibleSimObjPhysics(string objectID)
        {
            GameObject target = null;

            foreach(SimObjPhysics o in VisibleSimObjPhysics)
            {
                if(o.uniqueID == objectID)
                {
                    target = o.gameObject;
                }
            }

            return target;
        }


        protected Collider[] collidersWithinCapsuleCastOfAgent(float maxDistance) {
            CapsuleCollider agentCapsuleCollider = GetComponent<CapsuleCollider>();
            Vector3 point0, point1;
            float radius;
            agentCapsuleCollider.ToWorldSpaceCapsule(out point0, out point1, out radius);
            if (point0.y <= point1.y) {
                point1.y += maxDistance;
            } else {
                point0.y += maxDistance;
            }
			return Physics.OverlapCapsule(point0, point1, maxDistance, 1 << 8, QueryTriggerInteraction.Collide);
        }

        //*** Maybe make this better */
        // This function should be called before and after doing a visibility check (before with 
        // enableColliders == false and after with it equaling true). It, in particular, will
        // turn off/on all the colliders on agents which should not block visibility for the current agent
        // (invisible agents for example). 
        protected void updateAllAgentCollidersForVisibilityCheck(bool enableColliders) {
            foreach (BaseFPSAgentController agent in Agents) {
                PhysicsRemoteFPSAgentController phyAgent = (PhysicsRemoteFPSAgentController) agent;
                bool overlapping = (transform.position - phyAgent.transform.position).magnitude < 0.001f;
                if (overlapping || phyAgent.AgentId == AgentId || !phyAgent.IsVisible) {
                    foreach (Collider c in phyAgent.GetComponentsInChildren<Collider>()) {
                        c.enabled = enableColliders;
                    }
                    foreach (Collider c in phyAgent.AgentHand.GetComponentsInChildren<Collider>()) {
                        c.enabled = true;
                    }
                }
            }
        }

		protected SimObjPhysics[] GetAllVisibleSimObjPhysics(Camera agentCamera, float maxDistance)
        {
            foreach (KeyValuePair<string, SimObjPhysics> pair in uniqueIdToSimObjPhysics) {
                // Set all objects to not be visible
                pair.Value.isVisible = false;
            }
            List<SimObjPhysics> currentlyVisibleItems = new List<SimObjPhysics>();

            Vector3 agentCameraPos = agentCamera.transform.position;

			//get all sim objects in range around us that have colliders in layer 8 (visible), ignoring objects in the SimObjInvisible layer
            //this will make it so the receptacle trigger boxes don't occlude the objects within them.
            CapsuleCollider agentCapsuleCollider = GetComponent<CapsuleCollider>();
            Vector3 point0, point1;
            float radius;
            agentCapsuleCollider.ToWorldSpaceCapsule(out point0, out point1, out radius);
            if (point0.y <= point1.y) {
                point1.y += maxDistance;
            } else {
                point0.y += maxDistance;
            }

            // Turn off the colliders corresponding to this agent
            // and any invisible agents.
            updateAllAgentCollidersForVisibilityCheck(false);
            
			Collider[] colliders_in_view = Physics.OverlapCapsule(point0, point1, maxDistance, 1 << 8, QueryTriggerInteraction.Collide);
            
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
                            if (sop.VisibilityPoints == null || sop.VisibilityPoints.Length > 0)
                            {
                                Transform[] visPoints = sop.VisibilityPoints;
                                int visPointCount = 0;

                                foreach (Transform point in visPoints)
                                {

                                    //if this particular point is in view...
                                    if (CheckIfVisibilityPointInViewport(sop, point, agentCamera, false))
                                    {
                                        visPointCount++;
                                        #if !UNITY_EDITOR
                                        // If we're in the unity editor then don't break on finding a visible
                                        // point as we want to draw lines to each visible point.
                                        break;
                                        #endif
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
                            else {
                                Debug.Log("Error! Set at least 1 visibility point on SimObjPhysics " + sop + ".");
                            }
                                 
                        }
                    }               
                }
            }
            
            //check against anything in the invisible layers that we actually want to have occlude things in this round.
            //normally receptacle trigger boxes must be ignored from the visibility check otherwise objects inside them will be occluded, but
            //this additional check will allow us to see inside of receptacle objects like cabinets/fridges by checking for that interior
            //receptacle trigger box. Oh boy!
            Collider[] invisible_colliders_in_view = Physics.OverlapCapsule(point0, point1, maxDistance, 1 << 9, QueryTriggerInteraction.Collide);
			// Collider[] invisible_colliders_in_view = Physics.OverlapSphere(agentCameraPos, maxDistance * DownwardViewDistance,
            //                                              1 << 9, QueryTriggerInteraction.Collide);
            
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
                                    if (CheckIfVisibilityPointInViewport(sop, point, agentCamera, true))
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

            // Turn back on the colliders corresponding to this agent
            // and any invisible agents.
            updateAllAgentCollidersForVisibilityCheck(true);
            
            //populate array of visible items in order by distance
            currentlyVisibleItems.Sort((x, y) => Vector3.Distance(x.transform.position, agentCameraPos).CompareTo(Vector3.Distance(y.transform.position, agentCameraPos)));
            return currentlyVisibleItems.ToArray();
        }

        //use this to check if any given Vector3 coordinate is within the agent's viewport and also not obstructed
        public bool CheckIfPointIsInViewport(Vector3 point)
        {
            Vector3 viewPoint = m_Camera.WorldToViewportPoint(point);

            float ViewPointRangeHigh = 1.0f;
            float ViewPointRangeLow = 0.0f;

            if (viewPoint.z > 0 //&& viewPoint.z < maxDistance * DownwardViewDistance //is in front of camera and within range of visibility sphere
            && viewPoint.x < ViewPointRangeHigh && viewPoint.x > ViewPointRangeLow//within x bounds of viewport
            && viewPoint.y < ViewPointRangeHigh && viewPoint.y > ViewPointRangeLow)//within y bounds of viewport
            {
                RaycastHit hit;

                updateAllAgentCollidersForVisibilityCheck(false);

                if(Physics.Raycast(m_Camera.transform.position, point - m_Camera.transform.position, out hit,
                Vector3.Distance(m_Camera.transform.position, point) - 0.01f, (1 << 8 )|(1 << 10))) //reduce distance by slight offset
                {
                    updateAllAgentCollidersForVisibilityCheck(true);
                    return false;
                }

                else
                {
                    updateAllAgentCollidersForVisibilityCheck(true);
                    return true;
                }
            }

            return false;
        }
        
        //check if the visibility point on a sim object, sop, is within the viewport
        //has a inclueInvisible bool to check against triggerboxes as well, to check for visibility with things like Cabinets/Drawers
		protected bool CheckIfVisibilityPointInViewport(SimObjPhysics sop, Transform point, Camera agentCamera, bool includeInvisible)
        {
            bool result = false;

            Vector3 viewPoint = agentCamera.WorldToViewportPoint(point.position);

            float ViewPointRangeHigh = 1.0f;
            float ViewPointRangeLow = 0.0f;
            
			if (viewPoint.z > 0 //&& viewPoint.z < maxDistance * DownwardViewDistance //is in front of camera and within range of visibility sphere
                && viewPoint.x < ViewPointRangeHigh && viewPoint.x > ViewPointRangeLow//within x bounds of viewport
                && viewPoint.y < ViewPointRangeHigh && viewPoint.y > ViewPointRangeLow)//within y bounds of viewport
            {
				//now cast a ray out toward the point, if anything occludes this point, that point is not visible
				RaycastHit hit;

                float raycastDistance = Vector3.Distance(point.position, m_Camera.transform.position) + 1.0f;

                //check raycast against both visible and invisible layers, to check against ReceptacleTriggerBoxes which are normally
                //ignored by the other raycast
				if(includeInvisible)
				{
					if(Physics.Raycast(agentCamera.transform.position, point.position - agentCamera.transform.position, out hit, 
					                   raycastDistance, (1 << 8 )| (1 << 9) | (1 << 10)))
                    {
                        //print(hit.transform.name);
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
        			if (Physics.Raycast(agentCamera.transform.position, point.position - agentCamera.transform.position, out hit, 
        				                   raycastDistance, (1 << 8) | (1 << 10)))
					{ //layer mask automatically excludes Agent from this check
                        if (hit.transform != sop.transform) 
						{
                            //we didn't directly hit the sop we are checking for with this cast, 
						    //check if it's because we hit something see-through
                            SimObjPhysics hitSop = hit.transform.GetComponent<SimObjPhysics>();
                            if (hitSop != null && hitSop.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanSeeThrough)) 
							{
                                //we hit something see through, so now find all objects in the path between
                                //the sop and the camera
                                RaycastHit[] hits;
                                hits = Physics.RaycastAll(agentCamera.transform.position, point.position - agentCamera.transform.position,
                                                            raycastDistance, (1 << 8), QueryTriggerInteraction.Ignore);

                                float[] hitDistances = new float[hits.Length];
                                for (int i = 0; i < hitDistances.Length; i++) 
								{
									hitDistances[i] = hits[i].distance;//Vector3.Distance(hits[i].transform.position, m_Camera.transform.position);
                                }

                                Array.Sort(hitDistances, hits);

                                foreach (RaycastHit h in hits) 
								{

                                    if (h.transform == sop.transform) 
									{
                                        //found the object we are looking for, great!
                                        result = true;
                                        break;
                                    } 
									else 
									{
										// Didn't find it, continue on only if the hit object was translucent
										SimObjPhysics sopHitOnPath = null;
                                        sopHitOnPath = h.transform.GetComponentInParent<SimObjPhysics>();
                                        if (sopHitOnPath == null || 
                                            !sopHitOnPath.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanSeeThrough)) 
										{
											//print("this is blocking: " + sopHitOnPath.name);
                                            break;
                                        }
                                    }
                                }
                            }              
						}

						else 
						{
                            result = true;
							sop.isInteractable = true;
                        }                
                    }
                }
			}

			#if UNITY_EDITOR
            if (result == true)
			{            
                Debug.DrawLine(agentCamera.transform.position, point.position, Color.cyan);
			}
			#endif

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

			else
			{
				actionFinished(false);
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
			else
			{
				actionFinished(false);
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

		public override void RotateRight(ServerAction controlCommand)
		{
			if(CheckIfAgentCanTurn(90))
			{
				DefaultAgentHand(controlCommand);
				base.RotateRight(controlCommand);
			}

			else
			{
				actionFinished(false);
			}

		}

		public override void RotateLeft(ServerAction controlCommand)
		{
			if(CheckIfAgentCanTurn(-90))
			{
				DefaultAgentHand(controlCommand);
				base.RotateLeft(controlCommand);

			}

			else
			{
				actionFinished(false);
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

        public void TeleportObject(ServerAction action) {
            if (!uniqueIdToSimObjPhysics.ContainsKey(action.objectId)) {
                errorMessage = "Cannot find object with id " + action.objectId;
                Debug.Log(errorMessage);
                actionFinished(false);
                return;
            } else {
                SimObjPhysics sop = uniqueIdToSimObjPhysics[action.objectId];
                if (ItemInHand != null && sop == ItemInHand.GetComponent<SimObjPhysics>()) {
                    errorMessage = "Cannot teleport object in hand.";
                    Debug.Log(errorMessage);
                    actionFinished(false);
                    return;
                }
                sop.transform.position = new Vector3(action.x, action.y, action.z);
                sop.transform.rotation = Quaternion.Euler(action.rotation);
                actionFinished(true);
            }
        }

        public void TeleportObjectToFloor(ServerAction action) 
		{
            if (!uniqueIdToSimObjPhysics.ContainsKey(action.objectId)) {
                errorMessage = "Cannot find object with id " + action.objectId;
                Debug.Log(errorMessage);
                actionFinished(false);
                return;
            } else {
                SimObjPhysics sop = uniqueIdToSimObjPhysics[action.objectId];
                if (ItemInHand != null && sop == ItemInHand.GetComponent<SimObjPhysics>()) {
                    errorMessage = "Cannot teleport object in hand.";
                    Debug.Log(errorMessage);
                    actionFinished(false);
                    return;
                }
                Bounds objBounds = new Bounds(
                    new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity),
                    new Vector3(-float.PositiveInfinity, -float.PositiveInfinity, -float.PositiveInfinity)
                );
                foreach (Renderer r in sop.GetComponentsInChildren<Renderer>()) {
                    if (r.enabled) {
                        objBounds.Encapsulate(r.bounds);
                    }
                }
                if (objBounds.min.x == float.PositiveInfinity) {
                    errorMessage = "Could not get bounds of " + action.objectId + ".";
                    actionFinished(false);
                    return;
                }
                float y = getFloorY(action.x, action.z);
                if (errorMessage != "") {
                    actionFinished(false);
                    return;
                }
                sop.transform.position = new Vector3(
                    action.x, 
                    objBounds.extents.y + y + 0.1f, 
                    action.z
                );
                sop.transform.rotation = Quaternion.Euler(action.rotation);
                actionFinished(true);
            }
		}

        public void TeleportFull(ServerAction action) {
			targetTeleport = new Vector3 (action.x, action.y, action.z);

            if (action.forceAction) {
                DefaultAgentHand(action);
                transform.position = targetTeleport;
                transform.rotation = Quaternion.Euler(new Vector3(0.0f, action.rotation.y, 0.0f));
                if (action.standing) {
                    m_Camera.transform.localPosition = standingLocalCameraPosition;
                } else {
                    m_Camera.transform.localPosition = crouchingLocalCameraPosition;
                }
                m_Camera.transform.localEulerAngles = new Vector3 (action.horizon, 0.0f, 0.0f);
            } else {
                if (!sceneBounds.Contains(targetTeleport)) {
                    errorMessage = "Teleport target out of scene bounds.";
                    actionFinished(false);
                    return;
                }

                Vector3 oldPosition = transform.position;
                Quaternion oldRotation = transform.rotation;
                Vector3 oldLocalHandPosition = new Vector3();
                Quaternion oldLocalHandRotation = new Quaternion();
                if (ItemInHand != null) {
                    oldLocalHandPosition = ItemInHand.transform.localPosition;
                    oldLocalHandRotation = ItemInHand.transform.localRotation;
                }
                Vector3 oldCameraLocalEulerAngle = m_Camera.transform.localEulerAngles;
                Vector3 oldCameraLocalPosition = m_Camera.transform.localPosition;

                DefaultAgentHand(action);
                transform.position = targetTeleport;
                transform.rotation = Quaternion.Euler(new Vector3(0.0f, action.rotation.y, 0.0f));
                if (action.standing) {
                    m_Camera.transform.localPosition = standingLocalCameraPosition;
                } else {
                    m_Camera.transform.localPosition = crouchingLocalCameraPosition;
                }
                m_Camera.transform.localEulerAngles = new Vector3 (action.horizon, 0.0f, 0.0f);

                bool agentCollides = isAgentCapsuleColliding();
                bool handObjectCollides = isHandObjectColliding();

                if (agentCollides) {
                    errorMessage = "Cannot teleport due to agent collision.";
                    Debug.Log(errorMessage);
                } else if (handObjectCollides) {
                    errorMessage = "Cannot teleport due to hand object collision.";
                    Debug.Log(errorMessage);
                }

                if (agentCollides || handObjectCollides) {
                    if (ItemInHand != null) {
                        ItemInHand.transform.localPosition = oldLocalHandPosition;
                        ItemInHand.transform.localRotation = oldLocalHandRotation;
                    }
                    transform.position = oldPosition;
                    transform.rotation = oldRotation;
                    m_Camera.transform.localPosition = oldCameraLocalPosition;
                    m_Camera.transform.localEulerAngles = oldCameraLocalEulerAngle;
                    actionFinished(false);
                    return;
                }
            }
            snapToGrid();
            actionFinished(true);
		}

		public void Teleport(ServerAction action) {
            action.horizon = Convert.ToInt32(m_Camera.transform.localEulerAngles.x);
            action.standing = isStanding();
			if (!action.rotateOnTeleport) {
				action.rotation = transform.eulerAngles;
			}
			TeleportFull(action);
		}

        protected bool checkIfSceneBoundsContainTargetPosition(Vector3 position) {
            if (!sceneBounds.Contains(position)) {
                errorMessage = "Scene bounds do not contain target position.";
                return false;
            } else {
                return true;
            }
        }

        //for all translational movement, check if the item the player is holding will hit anything, or if the agent will hit anything
		public override void MoveLeft(ServerAction action)
		{
            action.moveMagnitude = action.moveMagnitude > 0 ? action.moveMagnitude : gridSize;
            Vector3 targetPosition = transform.position + -1 * transform.right * action.moveMagnitude;
			if(checkIfSceneBoundsContainTargetPosition(targetPosition) &&
                CheckIfItemBlocksAgentMovement(action.moveMagnitude, 270) &&
                CheckIfAgentCanMove(action.moveMagnitude, 270)) {
				DefaultAgentHand(action);
                transform.position = targetPosition;
                this.snapToGrid();
                actionFinished(true);
				// base.MoveLeft(action);
			} else {
                actionFinished(false);
            }
		}

		public override void MoveRight(ServerAction action)
		{
            action.moveMagnitude = action.moveMagnitude > 0 ? action.moveMagnitude : gridSize;
            Vector3 targetPosition = transform.position + transform.right * action.moveMagnitude;
			if(checkIfSceneBoundsContainTargetPosition(targetPosition) &&
                CheckIfItemBlocksAgentMovement(action.moveMagnitude, 90) &&
                CheckIfAgentCanMove(action.moveMagnitude, 90))  {
				DefaultAgentHand(action);
                transform.position = targetPosition;
                this.snapToGrid();
                actionFinished(true);
				// base.MoveRight(action);
			} else {
                actionFinished(false);
            }
		}

		public override void MoveAhead(ServerAction action)
		{
            action.moveMagnitude = action.moveMagnitude > 0 ? action.moveMagnitude : gridSize;
            Vector3 targetPosition = transform.position + transform.forward * action.moveMagnitude;
            if (checkIfSceneBoundsContainTargetPosition(targetPosition) &&
                CheckIfItemBlocksAgentMovement(action.moveMagnitude, 0) && 
                CheckIfAgentCanMove(action.moveMagnitude, 0)) {
				DefaultAgentHand(action);
                transform.position = targetPosition;
                this.snapToGrid();
                actionFinished(true);
				// base.MoveAhead(action);            
			} else {
                actionFinished(false);
            }
		}
        
		public override void MoveBack(ServerAction action)
		{
            action.moveMagnitude = action.moveMagnitude > 0 ? action.moveMagnitude : gridSize;
			Vector3 targetPosition = transform.position + -1 * transform.forward * action.moveMagnitude;
            if (checkIfSceneBoundsContainTargetPosition(targetPosition) &&
                CheckIfItemBlocksAgentMovement(action.moveMagnitude, 180) &&
                CheckIfAgentCanMove(action.moveMagnitude, 180)) {
				DefaultAgentHand(action);
                transform.position = targetPosition;
                this.snapToGrid();
                actionFinished(true);
				// base.MoveBack(action);
    		} else {
                actionFinished(false);
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
                            errorMessage = res.transform.name + " is blocking the Agent from moving " + orientation + " with " + ItemInHand.name;
							result = false;
							Debug.Log(errorMessage);
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

        //Sweeptest to see if the object Agent is holding will prohibit movement
        public bool CheckIfItemBlocksAgentStandOrCrouch()
        {
            bool result = false;

            //if there is nothing in our hand, we are good, return!
            if (ItemInHand == null)
            {
                result = true;
                return result;
            }

            //otherwise we are holding an object and need to do a sweep using that object's rb
            else
            {
                Vector3 dir = new Vector3();

                if (isStanding()) {
                    dir = new Vector3(0.0f, -1f, 0.0f);
                } else {
                    dir = new Vector3(0.0f, 1f, 0.0f);
                }

                Rigidbody rb = ItemInHand.GetComponent<Rigidbody>();

				RaycastHit[] sweepResults = rb.SweepTestAll(dir, standingLocalCameraPosition.y, QueryTriggerInteraction.Ignore);
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
                            errorMessage = res.transform.name + " is blocking the Agent from moving " + dir + " with " + ItemInHand.name;
							result = false;
							Debug.Log(errorMessage);
							return result;
						}
                                          
					}
				}
				//if the array is empty, nothing was hit by the sweeptest so we are clear to move
                else
                {
                    result = true;
                }

                return result;
            }
        }

        //
        public bool CheckIfAgentCanMove(float moveMagnitude, int orientation)
        {
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
            //sweep a bit further to account for thickness of agent skin. This should create more accurate movement without having the CheckMoveAction coroutine 
            //being required to reset positions in edge cases where collision happens literally by skin width differences
            float SkinOffsetMagnitude = moveMagnitude + m_CharacterController.skinWidth;

            RaycastHit[] sweepResults = rb.SweepTestAll(dir, SkinOffsetMagnitude, QueryTriggerInteraction.Ignore);
			//print(sweepResults[0]);
            //check if we hit an environmental structure or a sim object that we aren't actively holding. If so we can't move
            if (sweepResults.Length > 0)
            {
                foreach (RaycastHit res in sweepResults)
                {
                    // Don't worry if we hit something thats in our hand.
                    if (ItemInHand != null && ItemInHand.transform == res.transform) {
                        continue;
                    }
               
                    //including "Untagged" tag here so that the agent can't move through objects that are transparent
                    if (res.transform.GetComponent<SimObjPhysics>() || res.transform.tag == "Structure" || res.transform.tag == "Untagged")
                    {
                        errorMessage = res.transform.name + " is blocking the Agent from moving " + orientation;
                        Debug.Log(errorMessage);
                        //the moment we find a result that is blocking, return false here
                        return false;
                    } 
                }
            }
            return true;
        }
      
        /////AGENT HAND STUFF////
        protected IEnumerator moveHandToTowardsXYZWithForce(float x, float y, float z, float maxDistance)
        {
            if (ItemInHand == null)
            {
                Debug.Log("Agent can only move hand if holding an item");
                actionFinished(false);
                yield break;
            }
            SimObjPhysics simObjInHand = ItemInHand.GetComponent<SimObjPhysics>();
            simObjInHand.ResetContactPointsDictionary();
            Rigidbody rb = ItemInHand.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = false;

            Vector3 targetPosition = new Vector3(x, y, z);
            
            Vector3 initialPosition = rb.transform.position;
            Quaternion initialRotation = rb.transform.rotation;
            Vector3 forceDirection = targetPosition - rb.transform.position;
            forceDirection.Normalize();

            Vector3 lastPosition = initialPosition;
            Quaternion lastRotation = initialRotation;
            bool hitMaxDistance = false;
            bool beyondVisibleDistance = false;
            bool leavingViewport = false;
            float oldTimeScale = Time.timeScale;
            Time.timeScale = 5.0f;
            rb.AddForce(forceDirection, ForceMode.VelocityChange);
            float oldAngularDrag = rb.angularDrag;
            rb.angularDrag = 0.999f;
            List<Vector3> positions = new List<Vector3>();
            List<Quaternion> rotations = new List<Quaternion>();
            positions.Add(initialPosition);
            rotations.Add(initialRotation);
            for (int i = 0; i < 20; i++) {
                yield return null;
                float a = Vector3.Dot(rb.velocity, forceDirection);
                float aPos = Math.Max(0.0f, a);
                float aNeg = Math.Min(0.0f, a);
                rb.velocity = (aPos + 0.5f * aNeg) * forceDirection + 0.5f * (rb.velocity - a * forceDirection);

                if (rb.velocity.magnitude <= 0.001f &&
                    rb.angularVelocity.magnitude < 0.001f) {
                    break;
                }
                
                hitMaxDistance = beyondVisibleDistance = leavingViewport = false;

                Vector3 newPosition = rb.transform.position;
                Vector3 delta = newPosition - initialPosition;
                Vector3 forceDir = Vector3.Project(newPosition - initialPosition, forceDirection);
                Vector3 perpDir = delta - forceDir;
                float perpNorm = perpDir.magnitude;
                if (perpNorm > 0.1f * maxDistance) {
                    newPosition = initialPosition + forceDir + (0.1f * maxDistance) * perpDir / perpNorm;
                    rb.transform.position = newPosition;
                }

                Vector3 tmpForCamera = newPosition;
                tmpForCamera.y = m_Camera.transform.position.y;

                hitMaxDistance = Vector3.Distance(initialPosition, newPosition) > maxDistance;
                beyondVisibleDistance = Vector3.Distance(m_Camera.transform.position, tmpForCamera) > maxVisibleDistance;
                leavingViewport = !objectIsCurrentlyVisible(simObjInHand, 1000f);

                if (hitMaxDistance || beyondVisibleDistance || leavingViewport) {
                    break;
                } else {
                    positions.Add(rb.transform.position);
                    rotations.Add(rb.transform.rotation);
                    lastPosition = rb.transform.position;
                    lastRotation = rb.transform.rotation;
                }
            }
            Time.timeScale = oldTimeScale;

            Vector3 normalSum = new Vector3(0.0f, 0.0f, 0.0f);
            Vector3 aveCollisionsNormal = new Vector3(0.0f, 0.0f, 0.0f);
            int count = 0;
            foreach (KeyValuePair<Collider, ContactPoint[]> pair in simObjInHand.contactPointsDictionary) {
                foreach (ContactPoint cp in pair.Value) {
                    normalSum += cp.normal;
                    count += 1;
                }
            }
            if (count != 0) {
                aveCollisionsNormal = normalSum / count;
                aveCollisionsNormal.Normalize();
            }

            rb.angularDrag = oldAngularDrag;
            AgentHand.transform.position = lastPosition;
            rb.transform.localPosition = new Vector3(0f, 0f, 0f);
            rb.transform.rotation = lastRotation;

            SetUpRotationBoxChecks();
            IsHandDefault = false;

            bool handObjectIsColliding = count != 0;
            if (handObjectIsColliding) {
                AgentHand.transform.position = AgentHand.transform.position + 0.05f * aveCollisionsNormal;
                yield return null;
                handObjectIsColliding = simObjInHand.contactPointsDictionary.Count != 0;
            }
            // This has to be after the above as the contactPointsDictionary is only
            // updated while rb is not kinematic.
            rb.isKinematic = true;

            if (handObjectIsColliding) {
                AgentHand.transform.position = initialPosition;
                rb.transform.rotation = initialRotation;
                errorMessage = "Hand object was colliding with another object after movement.";
                Debug.Log(errorMessage);
                actionFinished(false);
            } else if (Vector3.Distance(initialPosition, lastPosition) < 0.001f &&
                Quaternion.Angle(initialRotation, lastRotation) < 0.001f) {
                if (beyondVisibleDistance) {
                    errorMessage = "Hand already at max distance.";
                } else if (leavingViewport) {
                    errorMessage = "Hand at viewport constraints.";
                } else {
                    errorMessage = "Hand object did not move, perhaps its being blocked.";
                }
                Debug.Log(errorMessage);
                actionFinished(false);
            } else {
                actionFinished(true);
            }
        }

        public void MoveHandForce(ServerAction action) {
            Vector3 direction = transform.forward * action.z + 
			                    transform.right * action.x + 
								transform.up * action.y;
            Vector3 target = AgentHand.transform.position + 
                                direction;
            if (ItemInHand == null) {
                Debug.Log("Agent can only move hand if holding an item");
                actionFinished(false);
            } else if (moveHandToXYZ(target.x, target.y, target.z)) {
                actionFinished(true);
            } else {
                errorMessage = "";
                StartCoroutine(
                    moveHandToTowardsXYZWithForce(target.x, target.y, target.z, direction.magnitude)
                );
            }
        }

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
            Vector3 tmp = m_Camera.transform.position;
            tmp.y = targetPosition.y;
            // TODO: What's the best way to determine the reach here?
			if (Vector3.Distance(tmp, targetPosition) > maxVisibleDistance)// + 0.3)
            {
                errorMessage = "The target position is out of range.";
                Debug.Log(errorMessage);
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
            Vector3 lastPosition = AgentHand.transform.position;
            AgentHand.transform.position = targetPosition;
            if (!objectIsCurrentlyVisible(ItemInHand.GetComponent<SimObjPhysics>(), 1000f))
            {
                Debug.Log("The target position is not in the Are of the Agent's Viewport!");
                result = false;
                AgentHand.transform.position = lastPosition;
                return result;
            }
            AgentHand.transform.position = lastPosition;

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

        // protected bool moveHandInDirection(Vector3 dir, float minDistance, float maxDistance)
		// {
		// 	bool result = false;

        //     //first check if we have anything in our hand, if not then no reason to move hand
		// 	if (ItemInHand == null)
        //     {
        //         errorMessage = "Agent can only move hand if holding an item";
        //         Debug.Log(errorMessage);
        //         result = false;
        //         return result;
        //     }

        //     // Check if the Agent Hand holding ItemInHand can move to the target position without
        //     // being obstructed by anything
		// 	Rigidbody ItemRB = ItemInHand.GetComponent<Rigidbody>();
        //     RaycastHit hit;
		// 	ItemRB.SweepTest(
        //         targetPosition - AgentHand.transform.position, 
        //         out hit,
        //         maxDistance,
        //         QueryTriggerInteraction.Ignore
        //     );

        //     Vector3 tmp = m_Camera.transform.position;
        //     tmp.y = targetPosition.y;
		// 	if (Vector3.Distance(tmp, targetPosition) > maxVisibleDistance)
        //     {
        //         errorMessage = "The target position is out of range.";
        //         Debug.Log(errorMessage);
        //         result = false;
        //         return result;
        //     }

        //     //now make sure that the targetPosition is within the Agent's x/y view, restricted by camera
		// 	Vector3 vp = m_Camera.WorldToViewportPoint(targetPosition);

        //     //Note: Viewport normalizes to (0,0) bottom left, (1, 0) top right of screen
        //     //now make sure the targetPosition is actually within the Camera Bounds 

        //     //XXX this does not check whether the object will still be visible when moving, so this will allow the agent to
        //     //move an object behind a door, causing the object to no longer be visible. Not sure if we should have a check
        //     //to restrict this yet, but about here is where that should go
        //     if (vp.z < 0 || vp.x > 1.0f || vp.x < 0.0f || vp.y > 1.0f || vp.y < 0.0f)
        //     {
        //         Debug.Log("The target position is not in the Are of the Agent's Viewport!");
        //         result = false;
        //         return result;
        //     }         


        //     //did we hit anything?
		// 	if (sweepResults.Length > 0)
		// 	{

		// 		foreach (RaycastHit hit in sweepResults)
		// 		{
		// 			//hit the player? it's cool, no problem
		// 			if (hit.transform.tag == "Player")
        //             {
        //                 result = true;
		// 				break;
        //             }

        //             //oh we hit something else? oh boy, that's blocking!
        //             else
        //             {
        //                 //  print("sweep didn't hit anything?");
        //                 Debug.Log(hit.transform.name + " is in Object In Hand's Path! Can't Move Hand holding " + ItemInHand.name);
        //                 result = false;
		// 				return result;
        //             }
		// 		}

		// 	}

        //     //didnt hit anything in sweep, we are good to go
		// 	else
		// 	{
		// 		result = true;
		// 	}

		// 	return result;
		// }

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

        protected IEnumerator waitForNFramesAndReturn(int n, bool actionSuccess) {
            for (int i = 0; i < n; i++) {
                yield return null;
            }
            actionFinished(actionSuccess);
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
            StartCoroutine(waitForNFramesAndReturn(1, moveHandToXYZ(newPos.x, newPos.y, newPos.z)));
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
			if (ItemInHand.GetComponent<SimObjPhysics>().BoundingBox.GetComponent<BoxCollider>())
			{
				//print("yes yes yes");
				Vector3 sizeOfBox = ItemInHand.GetComponent<SimObjPhysics>().BoundingBox.GetComponent<BoxCollider>().size;
				float overlapRadius = Math.Max(Math.Max(sizeOfBox.x, sizeOfBox.y), sizeOfBox.z);

                //all colliders hit by overlapsphere
                Collider[] hitColliders = Physics.OverlapSphere(AgentHand.transform.position,
				                                                overlapRadius, 1<< 8, QueryTriggerInteraction.Ignore);

                //did we even hit enything?
				if(hitColliders.Length > 0)
				{               
					foreach (Collider col in hitColliders)
                    {
						//is this a sim object?
						if(col.GetComponentInParent<SimObjPhysics>())
						{
							//is it not the item we are holding? then it's blocking
							if (col.GetComponentInParent<SimObjPhysics>().transform != ItemInHand.transform)
							{
								result = false;
								return result;
							}

                            //oh it is the item we are holding, it's fine
							else
								result = true;
						}

                        //ok it's not a sim obj and it's not the player, so it must be a structure or something else that would block
                        else if(col.tag != "Player")
						{
							result = false;
							return result;
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

			else
			{
				Debug.Log("Something is blocking the object from rotating freely");

				actionFinished(false);
			}
        }

        //if you are holding an object, place it on a valid Receptacle 
        //used for placing objects on receptacles without enclosed restrictions (drawers, cabinets, etc)
        //only checks if the object can be placed on top of the target receptacle
        public void PlaceHeldObject(ServerAction action)
        {
            //check if we are even holding anything
            if(ItemInHand == null)
            {
                errorMessage = "Can't place an object if Agent isn't holding anything";
                Debug.Log(errorMessage);
                actionFinished(false);
                return;
            }
            
            //get the target receptacle based on the action object ID
            SimObjPhysics targetReceptacle = null;

            SimObjPhysics[] simObjPhysicsArray = VisibleSimObjs(action);
            
            foreach (SimObjPhysics sop in simObjPhysicsArray)
            {
                if (action.objectId == sop.UniqueID)
                {
                    targetReceptacle = sop;
                }
            }

            if (targetReceptacle == null)
            {
                errorMessage = "No valid Receptacle found";
                Debug.Log(errorMessage);
                actionFinished(false);
                return;
            }

            //ok we are holding something, time to try and place it
            InstantiatePrefabTest script = GameObject.Find("PhysicsSceneManager").GetComponent<InstantiatePrefabTest>();
            if(script.PlaceObjectReceptacle(targetReceptacle.ReturnMySpawnPoints(), ItemInHand.GetComponent<SimObjPhysics>(), action.placeStationary))
            {
                ItemInHand = null;
                actionFinished(true);
            }

            else
            {
                errorMessage = "No valid points to place object found";
                Debug.Log(errorMessage);
                actionFinished(false);
            }
        }

        //if you are holding an object, place it INSIDE of a valid receptacle.
        //checks that the entirety of the object is enclosed within the receptacle's bounds
        // public void PlaceHeldObjectIn(ServerAction action)
        // {
        //     //check if we are even holding anything
        //     if(ItemInHand == null)
        //     {
        //         errorMessage = "Can't place an object if Agent isn't holding anything";
        //         Debug.Log(errorMessage);
        //         actionFinished(false);
        //         return;
        //     }
            
        //     //get the target receptacle based on the action object ID
        //     SimObjPhysics targetReceptacle = null;

        //     SimObjPhysics[] simObjPhysicsArray = VisibleSimObjs(action);
            
        //     foreach (SimObjPhysics sop in simObjPhysicsArray)
        //     {
        //         if (action.objectId == sop.UniqueID)
        //         {
        //             targetReceptacle = sop;
        //         }
        //     }

        //     if (targetReceptacle == null)
        //     {
        //         errorMessage = "No valid Receptacle found";
        //         Debug.Log(errorMessage);
        //         actionFinished(false);
        //         return;
        //     }

        //     //ok we are holding something, time to try and place it
        //     InstantiatePrefabTest script = GameObject.Find("PhysicsSceneManager").GetComponent<InstantiatePrefabTest>();
        //     if(script.PlaceObjectReceptacle(targetReceptacle.ReturnMySpawnPoints(), ItemInHand.GetComponent<SimObjPhysics>(), true))
        //     {
        //         ItemInHand = null;
        //         actionFinished(true);
        //     }
        // }
        
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
                
                foreach (SimObjPhysics sop in simObjPhysicsArray)
                {
                    if (action.objectId == sop.UniqueID)
                    {
                        target = sop;
                    }
                }

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
                
                //move the object to the hand's default position. Make it Kinematic
                //then set parant and ItemInHand
                
                Vector3 savedPos = target.transform.position;
                Quaternion savedRot = target.transform.rotation;
                Transform savedParent = target.transform.parent;
                bool wasKinematic = target.GetComponent<Rigidbody>().isKinematic;

                target.GetComponent<Rigidbody>().isKinematic = true;
                target.transform.position = AgentHand.transform.position;
				// target.transform.rotation = AgentHand.transform.rotation; - keep this line if we ever want to change the pickup position to be constant relative to the Agent Hand and Agent Camera rather than aligned by world axis
				target.transform.rotation = transform.rotation;
                target.transform.SetParent(AgentHand.transform);
                ItemInHand = target.gameObject;

                if (!action.forceAction && isHandObjectColliding()) {
                    // Undo picking up the object if the object is colliding with something after picking it up
                    target.GetComponent<Rigidbody>().isKinematic = wasKinematic;
                    target.transform.position = savedPos;
                    target.transform.rotation = savedRot;
                    target.transform.SetParent(savedParent);
                    ItemInHand = null;
                    errorMessage = "Picking up object would cause it to collide.";
                    Debug.Log(errorMessage);
                    actionFinished(true);
                    return;
                }

				SetUpRotationBoxChecks();

                //return true;
                actionFinished(true);
                return;
			}      
        }

        private IEnumerator checkDropHandObjectAction(SimObjPhysics currentHandSimObj) {
			yield return null; // wait for two frames to pass
			yield return null;
			for (int i = 0; i < 50; i++) {
				Rigidbody rb = currentHandSimObj.GetComponentInChildren<Rigidbody>();
				if (Math.Abs (rb.angularVelocity.sqrMagnitude + rb.velocity.sqrMagnitude) < 0.00001) {
					// Debug.Log ("object is now at rest");
					break;
				} else {
					// Debug.Log ("object is still moving");
					yield return null;
				}
			}

            DefaultAgentHand(new ServerAction());
			actionFinished (true);
		}

		public void DropHandObject(ServerAction action)
        {
            //make sure something is actually in our hands
            if (ItemInHand != null)
            {            
                //we do need this to check if the item is currently colliding with the agent, otherwise
                //dropping an object while it is inside the agent will cause it to shoot out weirdly
                if (!action.forceAction && ItemInHand.GetComponent<SimObjPhysics>().isColliding)
                {
                    errorMessage = ItemInHand.transform.name + " can't be dropped. It must be clear of all other objects first";
                    Debug.Log(errorMessage);
				 	actionFinished(false);
				 	return;
                } 

				else 
                {
                    Rigidbody rb = ItemInHand.GetComponent<Rigidbody>();
                    rb.isKinematic = false;
                    rb.constraints = RigidbodyConstraints.None;
				    rb.useGravity = true;

                    //change collision detection mode while falling so that obejcts don't phase through colliders.
                    //this is reset to discrete on SimObjPhysics.cs's update 
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

                    GameObject topObject = GameObject.Find("Objects");
                    if (topObject != null) {
                        ItemInHand.transform.parent = topObject.transform;
                    } else {
                        ItemInHand.transform.parent = null;
                    }

                    // Add some random rotational momentum to the dropped object to make things
                    // less deterministic.
                    // TODO: Need a parameter to control how much randomness we introduce.
                    rb.angularVelocity = UnityEngine.Random.insideUnitSphere;

                    StartCoroutine (checkDropHandObjectAction (ItemInHand.GetComponent<SimObjPhysics>()));
                    ItemInHand = null;
                    return;
                }
            }

            else
            {
                errorMessage = "nothing in hand to drop!";
                Debug.Log(errorMessage);
				actionFinished(false);
				return;
            }         
        }  

        //by default will throw in the forward direction relative to the Agent's Camera
        //moveMagnitude, strength of throw, good values for an average throw are around 150-250
        public void ThrowObject(ServerAction action)
		{
			if(ItemInHand == null)
			{
                errorMessage = "Nothing in Hand to Throw!";
				Debug.Log(errorMessage);
                actionFinished(false);            
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

        protected HashSet<SimObjPhysics> objectsInBox(float x, float z) {
            Collider[] colliders = Physics.OverlapBox(
				new Vector3(x, 0f, z),
				new Vector3(0.125f, 10f, 0.125f),
				Quaternion.identity
			);
            HashSet<SimObjPhysics> toReturn = new HashSet<SimObjPhysics>();
			foreach (Collider c in colliders) {
				SimObjPhysics so = ancestorSimObjPhysics(c.transform.gameObject);
				if (so != null) {
					toReturn.Add(so);
				}
			}
            return toReturn;
        }
      
        public void ObjectsInBox(ServerAction action) {
            HashSet<SimObjPhysics> objects = objectsInBox(action.x, action.z);
			objectIdsInBox = new string[objects.Count];
            int i = 0;
            foreach (SimObjPhysics so in objects) {
                objectIdsInBox[i] = so.UniqueID;
                i++;
            }
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
				
				Bounds b = sceneBounds;
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
            CanOpen_Object codd = target.GetComponent<CanOpen_Object>();

			if (codd) {
                //if object is open, close it
                if (codd.isOpen) {
                    codd.Interact();
                    return true;
                }
			}
            return false;
        }

        private bool openObject(SimObjPhysics target) {
            CanOpen_Object codd = target.GetComponent<CanOpen_Object>();

			if (codd) {
                //if object is open, close it
                if (!codd.isOpen) {
                    codd.Interact();
                    return true;
                }
			}
            return false;
        }

        public void CloseVisibleObjects(ServerAction action) {
            List<CanOpen_Object> coos = new List<CanOpen_Object>();
			foreach (SimObjPhysics so in GetAllVisibleSimObjPhysics(m_Camera, maxVisibleDistance)) {
                CanOpen_Object coo = so.GetComponent<CanOpen_Object>();
                    if (coo) {
                    //if object is open, add it to be closed.
                    if (coo.isOpen) {
                        coos.Add(coo);
                    }
                }
            }
			StartCoroutine(InteractAndWait(coos));
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
				if (sop.GetComponent<CanOpen_Object>())
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
                

				if(target.GetComponent<CanOpen_Object>())
				{
					CanOpen_Object codd = target.GetComponent<CanOpen_Object>();

                    //if object is open, close it
                    if (codd.isOpen)
                    {
                        // codd.Interact();
                        // actionFinished(true);
                        StartCoroutine(InteractAndWait(codd));
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
            if (ItemInHand != null) {
                foreach (Collider c in ItemInHand.GetComponentsInChildren<Collider>()) {
                    c.enabled = false;
                }
            }
            bool raycastDidHit = Physics.Raycast(ray, out hit, 10f, layerMask);
            if (ItemInHand != null) {
                foreach (Collider c in ItemInHand.GetComponentsInChildren<Collider>()) {
                    c.enabled = true;
                }
            }
            if (!raycastDidHit) {
                Debug.Log("There don't seem to be any objects in that area.");
                errorMessage = "No openable object at location.";
                actionFinished(false);
                return;
            } 
            SimObjPhysics so = ancestorSimObjPhysics(hit.transform.gameObject);
            Vector3 tmp = hit.transform.position;
            tmp.y = m_Camera.transform.position.y;
            if (so != null && (
                action.forceAction || Vector3.Distance(tmp, m_Camera.transform.position) < maxVisibleDistance
            )) {
                action.objectId = so.UniqueID;
                action.forceAction = true;
                if (open) {
                    OpenObject(action);
                } else {
                    CloseObject(action);
                }
            } else if (so == null) {
                errorMessage = "Object at location is not interactable.";
                Debug.Log(errorMessage);
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

        protected IEnumerator InteractAndWait(CanOpen_Object coo) {
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
            if (coo != null) {
                coo.Interact();
            }
            for (int i = 0; i < 1000; i++) {
                if (coo != null && coo.GetiTweenCount() == 0) {
                    success = true;
                    break;
                }
                yield return null;
            }

            if (ignoreAgentInTransition) {
                GameObject openedObject = null;
                openedObject = coo.GetComponentInParent<SimObjPhysics>().gameObject;
                
                if (isAgentCapsuleCollidingWith(openedObject) || isHandObjectCollidingWith(openedObject)) {
                    success = false;

                    if (coo != null) {
                        coo.Interact();
                    }
                    for (int i = 0; i < 1000; i++) {
                        if (coo != null && coo.GetiTweenCount() == 0) {
                            break;
                        }
                        yield return null;
                    }
                }
                foreach(Collider c in collidersDisabled) {
                    c.enabled = true;
                }
            }

            if (!success) {
                errorMessage = "Object failed to open/close successfully.";
                Debug.Log(errorMessage);
            }
            
            actionFinished(success);
        }


        protected bool anyInteractionsStillRunning(List<CanOpen_Object> coos) {
            bool anyStillRunning = false;
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

        protected IEnumerator InteractAndWait(List<CanOpen_Object> coos) {
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
            foreach (CanOpen_Object coo in coos) {
                coo.Interact();
            }

            for (int i = 0; anyInteractionsStillRunning(coos) && i < 1000; i++) {
                yield return null;
            }

            if (ignoreAgentInTransition) {
                foreach (CanOpen_Object coo in coos) {
                    GameObject openedObject = coo.GetComponentInParent<SimObjPhysics>().gameObject;
                    if (isAgentCapsuleCollidingWith(openedObject) || isHandObjectCollidingWith(openedObject)) {
                        coo.Interact();
                    }
                }

                for (int i = 0; anyInteractionsStillRunning(coos) && i < 1000; i++) {
                    yield return null;
                }

                foreach (Collider c in collidersDisabled) {
                    c.enabled = true;
                }
            }

            actionFinished(true);
        }

        public void ToggleObjectOn(ServerAction action)
        {
            if (action.objectId == null)
            {
                Debug.Log("Hey, actually give me an object ID to Toggle, yeah?");
                errorMessage = "objectId required for ToggleObject";
                actionFinished(false);
                return;
            }

            SimObjPhysics target = null;
            foreach (SimObjPhysics sop in VisibleSimObjs(action))
            {
                //check for CanOpen drawers, cabinets or CanOpen_Fridge fridge objects
				if (sop.GetComponent<CanToggleOnOff>())
                {
                    target = sop;
                }
            }

            if (target)
            {
                if (!action.forceAction && target.isInteractable == false)
                {
                    //Debug.Log("can't close object if it's already closed");
                    actionFinished(false);
                    errorMessage = "object is visible but occluded by something: " + action.objectId;
                    return;
                }

                if(target.GetComponent<CanToggleOnOff>())
                {
                    CanToggleOnOff ctof = target.GetComponent<CanToggleOnOff>();

                    //check to make sure object is off
                    if (ctof.isOn)
                    {
                        Debug.Log("can't toggle object on if it's already on!");
                        errorMessage = "can't toggle object on if it's already on!";
                        actionFinished(false);
                    }

                    else
                    {
                        ctof.Toggle();
                        actionFinished(true);
                    }
                }
            }

            //target not found in currently visible objects, report not found
            else
            {
                actionFinished(false);
                errorMessage = "object not found: " + action.objectId;
                Debug.Log(errorMessage);
            }
        }

        public void ToggleObjectOff(ServerAction action)
        {
            if (action.objectId == null)
            {
                Debug.Log("Hey, actually give me an object ID to Toggle, yeah?");
                errorMessage = "objectId required for ToggleObject";
                actionFinished(false);
                return;
            }

            SimObjPhysics target = null;
            foreach (SimObjPhysics sop in VisibleSimObjs(action))
            {
                //check for CanOpen drawers, cabinets or CanOpen_Fridge fridge objects
                if (sop.GetComponent<CanToggleOnOff>())
                {
                    target = sop;
                }
            }

            if (target)
            {
                if (!action.forceAction && target.isInteractable == false)
                {
                    //Debug.Log("can't close object if it's already closed");
                    actionFinished(false);
                    errorMessage = "object is visible but occluded by something: " + action.objectId;
                    return;
                }

                if(target.GetComponent<CanToggleOnOff>())
                {
                    CanToggleOnOff ctof = target.GetComponent<CanToggleOnOff>();

                    //check to make sure object is on
                    if (!ctof.isOn)
                    {
                        Debug.Log("can't toggle object off if it's already off!");
                        errorMessage = "can't toggle object off if it's already off!";
                        actionFinished(false);
                    }

                    else
                    {
                        ctof.Toggle();
                        actionFinished(true);
                    }
                }
            }

            //target not found in currently visible objects, report not found
            else
            {
                actionFinished(false);
                errorMessage = "object not found: " + action.objectId;
                Debug.Log(errorMessage);
            }
        }
        public void OpenObject(ServerAction action)
		{
			//pass name of object in from action.objectID
            //check if that object is in the viewport
            //also check to make sure that target object is interactable
            if (action.objectId == null)
            {
                Debug.Log("Hey, actually give me an object ID to open, yeah?");
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
				if (sop.GetComponent<CanOpen_Object>())
                {
                    target = sop;
                }
            }

			if (target)
			{
				if (!action.forceAction && target.isInteractable == false)
                {
                    //Debug.Log("can't close object if it's already closed");
                    actionFinished(false);
                    errorMessage = "object is visible but occluded by something: " + action.objectId;
                    return;
                }

                if(target.GetComponent<CanOpen_Object>())
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
                        StartCoroutine(InteractAndWait(codd));
                    }
				}

			}

            //target not found in currently visible objects, report not found
			else
            {
				
                actionFinished(false);
                errorMessage = "object not found: " + action.objectId;
				Debug.Log(errorMessage);
            }
		}

        //
        public void Contains(ServerAction action)
		{
			if (action.objectId == null)
            {
                errorMessage = "Hey, actually give me an object ID check containment for, yeah?";
                Debug.Log(errorMessage);
                actionFinished(false);
                return;
            }

			SimObjPhysics target = null;

			foreach (SimObjPhysics sop in VisibleSimObjs(action))
            {
                //check for object in current visible objects, and also check that it's interactable
                if (action.objectId == sop.UniqueID)
                {
                    target = sop;
                }

            }

			if (target)
			{
				//the sim object receptacle target returns list of unique sim object IDs as strings
                //XXX It looks like this goes right into the MetaData, so basically this just returns a list of strings
				//that are the unique ID's of every object that is contained by the target object
                target.Contains();
				// foreach(string s in target.Contains()) {
                //     Debug.Log(s);
                // }
                actionFinished(true);
			}

			else
			{
                errorMessage = "object not found: " + action.objectId;
				Debug.Log(errorMessage);
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
         
			BoxCollider HeldItemBox = ItemInHand.GetComponent<SimObjPhysics>().BoundingBox.GetComponent<BoxCollider>();
         
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
        

        public override SimpleSimObj[] VisibleSimObjs()
        {
            return GetAllVisibleSimObjPhysics(m_Camera, maxVisibleDistance);
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

        public void HideTranslucentObjects(ServerAction action) {
            foreach (SimObjPhysics sop in GameObject.FindObjectsOfType<SimObjPhysics>()) {
                if (sop.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanSeeThrough)) {
                    UpdateDisplayGameObject(sop.gameObject, false);
                }
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
            foreach (RaycastHit hit in hits) {
                if (hit.collider.transform.gameObject != toIgnore) {
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
						isOpenableGrid[i, j] = so != null && (so.GetComponent<CanOpen_Object>()
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

        protected bool isStanding() {
            return standingLocalCameraPosition == m_Camera.transform.localPosition;
        }

        protected void crouch() {
            m_Camera.transform.localPosition = new Vector3(
                standingLocalCameraPosition.x, 
                0.0f,
                standingLocalCameraPosition.z
            );
            SetUpRotationBoxChecks();
        }

        protected void stand() {
            m_Camera.transform.localPosition = standingLocalCameraPosition;
            SetUpRotationBoxChecks();
        }

        public void Crouch(ServerAction action) {
			if (!isStanding()) {
				errorMessage = "Already crouching.";
				actionFinished(false);
			} else if (!CheckIfItemBlocksAgentStandOrCrouch()) {
                actionFinished(false);
            } else {
				m_Camera.transform.localPosition = new Vector3(
                    standingLocalCameraPosition.x, 
                    0.0f,
                    standingLocalCameraPosition.z
                );
                SetUpRotationBoxChecks();
				actionFinished(true);
			}
		}

		public void Stand(ServerAction action) {
			if (isStanding()) {
				errorMessage = "Already standing.";
				actionFinished(false);
			} else if (!CheckIfItemBlocksAgentStandOrCrouch()) {
                actionFinished(false);
            } else {
				m_Camera.transform.localPosition = standingLocalCameraPosition;
                SetUpRotationBoxChecks();
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

        public void Pass(ServerAction action) {
            actionFinished(true);
        }
        protected bool objectIsCurrentlyVisible(SimObjPhysics sop, float maxDistance) {
            if (sop.VisibilityPoints.Length > 0) {
                Transform[] visPoints = sop.VisibilityPoints;
                updateAllAgentCollidersForVisibilityCheck(false);
                foreach (Transform point in visPoints) {
                    Vector3 tmp = point.position;
                    tmp.y = transform.position.y;
                    // Debug.Log(Vector3.Distance(tmp, transform.position));
                    if (Vector3.Distance(tmp, transform.position) < maxDistance) {
                        //if this particular point is in view...
                        if (CheckIfVisibilityPointInViewport(sop, point, m_Camera, false)) {
                            updateAllAgentCollidersForVisibilityCheck(true);
                            return true;
                        }
                    }
                }
            } else {
                Debug.Log("Error! Set at least 1 visibility point on SimObjPhysics prefab!");
            }
            updateAllAgentCollidersForVisibilityCheck(true);
            return false;
        }
        

        protected static void Shuffle<T> (System.Random rng, T[] array)
        {
            // Taken from https://stackoverflow.com/questions/108819/best-way-to-randomize-an-array-with-net
            int n = array.Length;
            while (n > 1) 
            {
                int k = rng.Next(n--);
                T temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
        }

        protected int xzManhattanDistance(Vector3 p0, Vector3 p1, float gridSize) {
            return (Math.Abs(Convert.ToInt32((p0.x - p1.x) / gridSize)) +
                Math.Abs(Convert.ToInt32((p0.z - p1.z) / gridSize)));
        }

        public void ExhaustiveSearchForItem(ServerAction action) {
            if (!uniqueIdToSimObjPhysics.ContainsKey(action.objectId)) {
                errorMessage = "Object ID appears to be invalid.";
                Debug.Log(errorMessage);
                actionFinished(false);
                return;
            }
            SimObjPhysics theObject = uniqueIdToSimObjPhysics[action.objectId];

            Vector3[] positions = getReachablePositions();
            bool wasStanding = isStanding();
            Vector3 oldPosition = transform.position;
            Quaternion oldRotation = transform.rotation;
            if (ItemInHand != null) {
                ItemInHand.gameObject.SetActive(true);
            }

            Shuffle(new System.Random(action.randomSeed), positions);

            SimplePriorityQueue<Vector3> pq = new SimplePriorityQueue<Vector3>();
            Vector3 agentPos = transform.position;
            foreach (Vector3 p in positions) {
                pq.Enqueue(p, xzManhattanDistance(p, agentPos, gridSize));
            }

            Vector3 visiblePosition = new Vector3(0.0f, 0.0f, 0.0f);
            bool objectSeen = false;
            int positionsTried = 0;
            while (pq.Count != 0 && !objectSeen) {
                positionsTried += 1;
                Vector3 p = pq.Dequeue();
                transform.position = p;
                Collider[] colliders = collidersWithinCapsuleCastOfAgent(maxVisibleDistance);

                HashSet<SimObjPhysics> openableObjectsNearby = new HashSet<SimObjPhysics>();
                foreach (Collider c in colliders) {
                    SimObjPhysics sop = ancestorSimObjPhysics(c.gameObject);
                    if (sop != null && sop.GetComponent<CanOpen_Object>() != null) {
                        openableObjectsNearby.Add(sop);
                    }
                }

                foreach (SimObjPhysics openable in openableObjectsNearby) {
                    foreach (GameObject go in openable.GetComponent<CanOpen_Object>().MovingParts) {
                        go.SetActive(false);
                    }
                }

                for (int j = 0; j < 2; j++) { // Standing / Crouching
                    if (j == 0) {
                        stand();
                    } else {
                        crouch();
                    }
                    for (int i = 0; i < 4; i++) { // 4 rotations
                        transform.rotation = Quaternion.Euler(new Vector3(0.0f, 90.0f * i, 0.0f));
                        if (objectIsCurrentlyVisible(theObject, 1000f)) {
                            // for (int k = 0; k < 100; k++) {
                            //     yield return null;
                            // }
                            objectSeen = true;
                            visiblePosition = p;
                            break;
                        }
                    }
                    if (objectSeen) {
                        break;
                    }
                }

                foreach (SimObjPhysics openable in openableObjectsNearby) {
                    foreach (GameObject go in openable.GetComponent<CanOpen_Object>().MovingParts) {
                        go.SetActive(true);
                    }
                }
            }

            #if UNITY_EDITOR
            if (objectSeen) {
                Debug.Log("Object found.");
                Debug.Log("Manhattan distance:");
                Debug.Log(xzManhattanDistance(visiblePosition, oldPosition, gridSize));
            } else {
                Debug.Log("Object not found.");
            }
            Debug.Log("BFS steps taken:");
            Debug.Log(positionsTried);
            #endif

            actionBoolReturn = objectSeen;
            actionIntReturn = positionsTried;

            if (wasStanding) {
                stand();
            } else {
                crouch();
            }
            transform.position = oldPosition;
            transform.rotation = oldRotation;
            if (ItemInHand != null) {
                ItemInHand.gameObject.SetActive(true);
            }
            actionFinished(true);
        }
        
        public void NumberOfPositionsFromWhichItemIsVisible(ServerAction action) {
            Vector3[] positions = getReachablePositions();
            bool wasStanding = isStanding();
            Vector3 oldPosition = transform.position;
            Quaternion oldRotation = transform.rotation;
            if (ItemInHand != null) {
                ItemInHand.gameObject.SetActive(true);
            }

            if (!uniqueIdToSimObjPhysics.ContainsKey(action.objectId)) {
                errorMessage = "Object ID appears to be invalid.";
                Debug.Log(errorMessage);
                actionFinished(false);
                return;
            }
            SimObjPhysics theObject = uniqueIdToSimObjPhysics[action.objectId];

            int numTimesVisible = 0;
            for (int j = 0; j < 2; j++) { // Standing / Crouching
                if (j == 0) {
                    stand();
                } else {
                    crouch();
                }
                for (int i = 0; i < 4; i++) { // 4 rotations
                    transform.rotation = Quaternion.Euler(new Vector3(0.0f, 90.0f * i, 0.0f));
                    foreach (Vector3 p in positions) {
                        transform.position = p;

                        if (objectIsCurrentlyVisible(theObject, 1000f)) {
                            numTimesVisible += 1;
                        }
                    }
                }
            }          

            if (wasStanding) {
                stand();
            } else {
                crouch();
            }
            transform.position = oldPosition;
            transform.rotation = oldRotation;
            if (ItemInHand != null) {
                ItemInHand.gameObject.SetActive(true);
            }

            #if UNITY_EDITOR
            Debug.Log(4 * 2 * positions.Length);
            Debug.Log(numTimesVisible);
            #endif

            actionIntReturn = numTimesVisible;
            actionFinished(true);
        }

        public void TogglePhysics(ServerAction action) {
            Physics.autoSimulation = !Physics.autoSimulation;
            actionFinished(true);
        }

        public void ChangeOpenSpeed(ServerAction action) {
            foreach (CanOpen_Object coo in GameObject.FindObjectsOfType<CanOpen_Object>()) {
                coo.animationTime = action.x;
            }
            actionFinished(true);
        }

        public void GetSceneBounds(ServerAction action) {
            reachablePositions = new Vector3[2];
            reachablePositions[0] = sceneBounds.min;
            reachablePositions[1] = sceneBounds.max;
            #if UNITY_EDITOR
            Debug.Log(reachablePositions[0]);
            Debug.Log(reachablePositions[1]);
            #endif
            actionFinished(true);
        }

        protected Collider[] overlapCollider(BoxCollider box, Vector3 newCenter, float rotateBy, int layerMask) {
            Vector3 center, halfExtents;
            Quaternion orientation;
            box.ToWorldSpaceBox(out center, out halfExtents, out orientation);
            orientation = Quaternion.Euler(0f, rotateBy, 0f) * orientation;

            return Physics.OverlapBox(newCenter, halfExtents, orientation, layerMask, QueryTriggerInteraction.Ignore);
        }
        protected Collider[] overlapCollider(SphereCollider sphere, Vector3 newCenter, int layerMask) {
            Vector3 center;
            float radius;
            sphere.ToWorldSpaceSphere(out center, out radius);
            return Physics.OverlapSphere(newCenter, radius, layerMask, QueryTriggerInteraction.Ignore);
        }
        protected Collider[] overlapCollider(CapsuleCollider capsule, Vector3 newCenter, float rotateBy, int layerMask) {
            Vector3 point0, point1;
            float radius;
            capsule.ToWorldSpaceCapsule(out point0, out point1, out radius);

            // Normalizing
            Vector3 oldCenter = (point0 + point1) / 2.0f;
            point0 = point0 - oldCenter;
            point1 = point1 - oldCenter;
            
            // Rotating and recentering
            var rotator = Quaternion.Euler(0f, rotateBy, 0f);
            point0 = rotator * point0 + newCenter;
            point1 = rotator * point1 + newCenter;

            return Physics.OverlapCapsule(point0, point1, radius, layerMask, QueryTriggerInteraction.Ignore);
        }

        protected Collider[] objectsCollidingWithAgent() {
            int layerMask = 1 << 8;
            return PhysicsExtensions.OverlapCapsule(GetComponent<CapsuleCollider>(), layerMask, QueryTriggerInteraction.Ignore);
        }

        protected bool isAgentCapsuleColliding() {
            int layerMask = 1 << 8;
            foreach (Collider c in PhysicsExtensions.OverlapCapsule(GetComponent<CapsuleCollider>(), layerMask, QueryTriggerInteraction.Ignore)) {
                if (!hasAncestor(c.transform.gameObject, gameObject)) {
                    #if UNITY_EDITOR
                    Debug.Log("Collided with: ");
                    Debug.Log(c);
                    #endif
                    return true;
                }
            }
            return false;
        }

        protected bool isHandObjectColliding() {
            if (ItemInHand == null) {
                return false;
            }
            int layerMask = 1 << 8;
            foreach (CapsuleCollider cc in ItemInHand.GetComponentsInChildren<CapsuleCollider>()) {
                foreach (Collider c in PhysicsExtensions.OverlapCapsule(cc, layerMask, QueryTriggerInteraction.Ignore)) {
                    if (!hasAncestor(c.transform.gameObject, gameObject)) {
                        return true;
                    }
                }
            }
            foreach (BoxCollider bc in ItemInHand.GetComponentsInChildren<BoxCollider>()) {
                foreach (Collider c in PhysicsExtensions.OverlapBox(bc, layerMask, QueryTriggerInteraction.Ignore)) {
                    if (!hasAncestor(c.transform.gameObject, gameObject)) {
                        return true;
                    }
                }
            }
            foreach (SphereCollider sc in ItemInHand.GetComponentsInChildren<SphereCollider>()) {
                foreach (Collider c in PhysicsExtensions.OverlapSphere(sc, layerMask, QueryTriggerInteraction.Ignore)) {
                    if (!hasAncestor(c.transform.gameObject, gameObject)) {
                        return true;
                    }
                }
            }
            return false;
        }

        protected bool isAgentCapsuleCollidingWith(GameObject otherGameObject) {
            int layerMask = 1 << 8;
            foreach (Collider c in PhysicsExtensions.OverlapCapsule(GetComponent<CapsuleCollider>(), layerMask, QueryTriggerInteraction.Ignore)) {
                if (hasAncestor(c.transform.gameObject, otherGameObject)) {
                    return true;
                }
            }
            return false;
        }

        protected bool isHandObjectCollidingWith(GameObject otherGameObject) {
            if (ItemInHand == null) {
                return false;
            }
            int layerMask = 1 << 8;
            foreach (CapsuleCollider cc in ItemInHand.GetComponentsInChildren<CapsuleCollider>()) {
                foreach (Collider c in PhysicsExtensions.OverlapCapsule(cc, layerMask, QueryTriggerInteraction.Ignore)) {
                    if (hasAncestor(c.transform.gameObject, otherGameObject)) {
                        return true;
                    }
                }
            }
            foreach (BoxCollider bc in ItemInHand.GetComponentsInChildren<BoxCollider>()) {
                foreach (Collider c in PhysicsExtensions.OverlapBox(bc, layerMask, QueryTriggerInteraction.Ignore)) {
                    if (!hasAncestor(c.transform.gameObject, otherGameObject)) {
                        return true;
                    }
                }
            }
            foreach (SphereCollider sc in ItemInHand.GetComponentsInChildren<SphereCollider>()) {
                foreach (Collider c in PhysicsExtensions.OverlapSphere(sc, layerMask, QueryTriggerInteraction.Ignore)) {
                    if (!hasAncestor(c.transform.gameObject, otherGameObject)) {
                        return true;
                    }
                }
            }
            return false;
        }

        private bool hasAncestor(GameObject child, GameObject potentialAncestor) {
            if (child == potentialAncestor) {
                return true;
            } else if (child.transform.parent != null) {
                return hasAncestor(child.transform.parent.gameObject, potentialAncestor);
            } else {
                return false;
            }
        }

        protected bool handObjectCanFitInPosition(Vector3 newAgentPosition, float rotation) {
            if (ItemInHand == null) {
                return true;
            }

            SimObjPhysics soInHand = ItemInHand.GetComponent<SimObjPhysics>();

            Vector3 handObjPosRelAgent = 
                Quaternion.Euler(0, rotation - transform.rotation.y, 0) * 
                (transform.position - ItemInHand.transform.position);
            
            Vector3 newHandPosition = handObjPosRelAgent + newAgentPosition;

            int layerMask = 1 << 8;
            foreach (CapsuleCollider cc in soInHand.GetComponentsInChildren<CapsuleCollider>()) {
                foreach (Collider c in overlapCollider(cc, newHandPosition, rotation, layerMask)) {
                    if (!hasAncestor(c.transform.gameObject, gameObject)) {
                        return false;
                    }
                }
            }
            foreach (BoxCollider bc in soInHand.GetComponentsInChildren<BoxCollider>()) {
                foreach (Collider c in overlapCollider(bc, newHandPosition, rotation, layerMask)) {
                    if (!hasAncestor(c.transform.gameObject, gameObject)) {
                        return false;
                    }
                }
            }
            foreach (SphereCollider sc in soInHand.GetComponentsInChildren<SphereCollider>()) {
                foreach (Collider c in overlapCollider(sc, newHandPosition, layerMask)) {
                    if (!hasAncestor(c.transform.gameObject, gameObject)) {
                        return false;
                    }
                }
            }

            return true;
        }

        override public Vector3[] getReachablePositions() {
            CapsuleCollider cc = GetComponent<CapsuleCollider>();

            Vector3 center = transform.position;
            float sw = m_CharacterController.skinWidth;
            float floorFudgeFactor = sw; // Small constant added to make sure the capsule
                                                                      // cast below doesn't collide with the ground.
            float radius = cc.radius + sw;
            float innerHeight = cc.height / 2.0f - radius;

            Queue<Vector3> pointsQueue = new Queue<Vector3>();
            pointsQueue.Enqueue(center);

            //float dirSkinWidthMultiplier = 1.0f + sw;
            Vector3[] directions = {
                new Vector3(1.0f, 0.0f, 0.0f),
                new Vector3(0.0f, 0.0f, 1.0f),
                new Vector3(-1.0f, 0.0f, 0.0f),
                new Vector3(0.0f, 0.0f, -1.0f)
            };

            HashSet<Vector3> goodPoints = new HashSet<Vector3>();
            int layerMask = 1 << 8;
            int stepsTaken = 0;
            while (pointsQueue.Count != 0) {
                stepsTaken += 1;
                Vector3 p = pointsQueue.Dequeue();
                if (!goodPoints.Contains(p)) {
                    goodPoints.Add(p);
                    Vector3 point1 = new Vector3(p.x, center.y + innerHeight, p.z);
                    Vector3 point2 = new Vector3(p.x, center.y - innerHeight + floorFudgeFactor, p.z);
                    HashSet<Collider> objectsAlreadyColliding = new HashSet<Collider>(objectsCollidingWithAgent());
                    foreach (Vector3 d in directions) {
                        RaycastHit[] hits = Physics.CapsuleCastAll(
                            point1,
                            point2,
                            radius,
                            d,// * dirSkinWidthMultiplier, - multiplying direction doesn't increase distance cast
                            gridSize + sw, //offset should be here, this mimics the additional distance check of MoveAgent()
                            layerMask,
                            QueryTriggerInteraction.Ignore
                        );
                        bool shouldEnqueue = true;
                        foreach (RaycastHit hit in hits) {
                            if (hit.transform.gameObject.name != "Floor" &&
                                !ancestorHasName(hit.transform.gameObject, "FPSController") &&
                                !objectsAlreadyColliding.Contains(hit.collider)
                                ) {
                                shouldEnqueue = false;
                                break;
                            }
                        }
                        Vector3 newPosition = p + d * gridSize;
                        bool inBounds = sceneBounds.Contains(newPosition);
                        if (errorMessage == "" && !inBounds) {
                            errorMessage = "In " + 
                            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + 
                            ", position " + newPosition.ToString() +
                             " can be reached via capsule cast but is beyond the scene bounds.";
                             Debug.Log(errorMessage);
                        }                        

                        shouldEnqueue = shouldEnqueue && inBounds && (
                                        handObjectCanFitInPosition(newPosition, 0.0f) ||
                                        handObjectCanFitInPosition(newPosition, 90.0f) ||
                                        handObjectCanFitInPosition(newPosition, 180.0f) || 
                                        handObjectCanFitInPosition(newPosition, 270.0f)
                        );
                        if (shouldEnqueue) {
                            pointsQueue.Enqueue(newPosition);
                            #if UNITY_EDITOR
                            Debug.DrawLine(p, newPosition, Color.cyan, 100000f);
                            #endif
                        }
                    }
                }
                if (stepsTaken > 10000) {
                    errorMessage = "Too many steps taken in getReachablePoints.";
                    Debug.Log(errorMessage);
                    break;
                }
            }

            Vector3[] reachablePos = new Vector3[goodPoints.Count];
            goodPoints.CopyTo(reachablePos);
            #if UNITY_EDITOR
            Debug.Log(reachablePos.Length);
            #endif
            return reachablePos;
        }

        public void GetReachablePositions(ServerAction action) {
            reachablePositions = getReachablePositions();
            if (errorMessage != "") {
                actionFinished(false);
            } else {
                actionFinished(true);
            }
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

        //spawns object in agent's hand with the same orientation as the agent's hand
        public void CreateObject(ServerAction action) 
		{
            if (ItemInHand != null) 
			{
                errorMessage = "Already have an object in hand, can't create a new one to put there.";
                Debug.Log(errorMessage);
                actionFinished(false);
				return;
            }
            
			if(action.objectType == null)
			{
				errorMessage = "Please give valid Object Type from SimObjType enum list";
                Debug.Log(errorMessage);
                actionFinished(false);
                return;
			}

            //spawn the object at the agent's hand position
			InstantiatePrefabTest script = GameObject.Find("PhysicsSceneManager").GetComponent<InstantiatePrefabTest>();
			SimObjPhysics so = script.SpawnObject(action.objectType, action.randomizeObjectAppearance, action.objectVariation, 
			                                      AgentHand.transform.position, AgentHand.transform.rotation.eulerAngles, true);
            
			if (so == null) 
			{
                errorMessage = "Failed to create object, are you sure it can be spawned?";
                Debug.Log(errorMessage);
				actionFinished(false);
				return;
			} 

			else 
			{
				//put new object created in dictionary and assign its uniqueID to the action
				uniqueIdToSimObjPhysics[so.UniqueID] = so;
				action.objectId = so.uniqueID;

                //also update the PHysics Scene Manager with this new object
				PhysicsSceneManager physicsSceneManager = GameObject.Find("PhysicsSceneManager").GetComponent<PhysicsSceneManager>();
				physicsSceneManager.AddToIDsInScene(so);
				physicsSceneManager.AddToObjectsInScene(so);
			}

            action.forceAction = true;
			PickupObject(action);
		}

		public void CreateObjectAtLocation(ServerAction action) 
		{
			Vector3 targetPosition = action.position;
			Vector3 targetRotation = action.rotation;

			if(!sceneBounds.Contains(targetPosition))
			{
				errorMessage = "Target position is out of bounds!";
                Debug.Log(errorMessage);
                actionFinished(false);
                return;
			}

			if (action.objectType == null)
            {
                errorMessage = "Please give valid Object Type from SimObjType enum list";
                Debug.Log(errorMessage);
                actionFinished(false);
                return;
            }
         
			//spawn the object at the agent's hand position
            InstantiatePrefabTest script = GameObject.Find("PhysicsSceneManager").GetComponent<InstantiatePrefabTest>();
            SimObjPhysics so = script.SpawnObject(action.objectType, action.randomizeObjectAppearance, action.objectVariation,
			                                      targetPosition, targetRotation, false);
			
			if (so == null) 
			{
				errorMessage = "Failed to create object, are you sure it can be spawned?";
                Debug.Log(errorMessage);
				actionFinished(false);
				return;
			} 

			else 
			{
				uniqueIdToSimObjPhysics[so.UniqueID] = so;
				//also update the PHysics Scene Manager with this new object
                PhysicsSceneManager physicsSceneManager = GameObject.Find("PhysicsSceneManager").GetComponent<PhysicsSceneManager>();
                physicsSceneManager.AddToIDsInScene(so);
                physicsSceneManager.AddToObjectsInScene(so);
			}

			actionFinished(true);
		}

        public bool createObjectAtLocation(string objectType, Vector3 targetPosition, Vector3 targetRotation, int objectVariation = 1) 
		{
			if(!sceneBounds.Contains(targetPosition))
			{
				errorMessage = "Target position is out of bounds!";
                Debug.Log(errorMessage);
                return false;
			}

			if (objectType == null)
            {
                errorMessage = "Please give valid Object Type from SimObjType enum list";
                return false;
            }
         
			//spawn the object at the agent's hand position
            InstantiatePrefabTest script = GameObject.Find("PhysicsSceneManager").GetComponent<InstantiatePrefabTest>();
            SimObjPhysics so = script.SpawnObject(objectType, false, objectVariation,
			                                      targetPosition, targetRotation, false);
			
			if (so == null) 
			{
				errorMessage = "Failed to create object, are you sure it can be spawned?";
                return false;
			} 

			else 
			{
				uniqueIdToSimObjPhysics[so.UniqueID] = so;
				//also update the PHysics Scene Manager with this new object
                PhysicsSceneManager physicsSceneManager = GameObject.Find("PhysicsSceneManager").GetComponent<PhysicsSceneManager>();
                physicsSceneManager.AddToIDsInScene(so);
                physicsSceneManager.AddToObjectsInScene(so);
			}

			return true;
		}

        protected float getFloorY(float x, float z) {
			int layerMask = ~(1 << 10);
			
            Ray ray = new Ray(transform.position, -transform.up);
			RaycastHit hit;
			if (!Physics.Raycast(ray, out hit, 10f, layerMask)) {
                errorMessage = "Could not find the floor";
				return float.NegativeInfinity;
			}

            float y = hit.point.y;

            ray = new Ray(new Vector3(x, y + 0.5f, z), -transform.up);
			if (!Physics.Raycast(ray, out hit, 10f, layerMask)) {
                errorMessage = "Could not find the floor";
				return float.NegativeInfinity;
			}

			return hit.point.y;
		}

        public void CreateObjectOnFloor(ServerAction action) 
		{
            InstantiatePrefabTest script = GameObject.Find("PhysicsSceneManager").GetComponent<InstantiatePrefabTest>();
			Bounds b = script.BoundsOfObject(action.objectType, 1);
            if (b.min.x == float.PositiveInfinity) {
                errorMessage = "Could not get bounds for the object to be created on the floor";
                Debug.Log(errorMessage);
                actionFinished(false);
            } else {
                action.y = b.extents.y + getFloorY(action.x, action.z) + 0.1f;
                action.position = new Vector3(action.x, action.y, action.z);
                CreateObjectAtLocation(action);
            }
		}

        public void RandomlyCreateAndPlaceObjectOnFloor(ServerAction action) {
            InstantiatePrefabTest script = GameObject.Find("PhysicsSceneManager").GetComponent<InstantiatePrefabTest>();
			Bounds b = script.BoundsOfObject(action.objectType, 1);
            if (b.min.x != float.PositiveInfinity) {
                errorMessage = "Could not get bounds of object with type " + action.objectType;
            }

            System.Random rnd = new System.Random();
            Vector3[] shuffledCurrentlyReachable = getReachablePositions().OrderBy(x => rnd.Next()).ToArray(); 
            float[] rotations = {0f, 90f, 180f, 270f};
            float[] shuffledRotations = rotations.OrderBy(x => rnd.Next()).ToArray(); 
            bool objectCreated = false;
            foreach (Vector3 position in shuffledCurrentlyReachable) {
                float y = b.extents.y + getFloorY(position.x, position.z) + 0.1f;
                foreach (float r in shuffledRotations) {
                    objectCreated = createObjectAtLocation(
                        action.objectType, 
                        new Vector3(position.x, y, position.z), 
                        new Vector3(0.0f, r, 0.0f),
                        action.objectVariation);
                    if (objectCreated) {
                        break;
                    }
                }
                if (objectCreated) {
                    break;
                }
            }
            if (!objectCreated) {
                errorMessage = "Failed to randomly create object";
                actionFinished(false);
            } else {
                errorMessage = "";
                actionFinished(true);
            }
        }

        public void GetPositionsObjectVisibleFrom(ServerAction action) {
            if (!uniqueIdToSimObjPhysics.ContainsKey(action.objectId)) {
                errorMessage = "Object " + action.objectId + " does not seem to exist.";
                actionFinished(false);
                return;
            }

            SimObjPhysics sop = uniqueIdToSimObjPhysics[action.objectId];

            GameObject[] visibilityCapsules = Resources.FindObjectsOfTypeAll<GameObject>().Where(
                obj => obj.name == "VisibilityCapsule"
            ).ToArray();
            foreach (GameObject go in visibilityCapsules) {
                go.SetActive(false);
            }

            Vector3 savedPosition = transform.position;
            Quaternion savedRotation = transform.rotation;
            float[] rotations = {0f, 90f, 180f, 270f};

            List<Vector3> goodPositions = new List<Vector3>();
            List<float> goodRotations = new List<float>();

            foreach (Vector3 position in getReachablePositions()) {
                Vector3 tmp = position;
                tmp.y = sop.transform.position.y;
                if (Vector3.Distance(tmp, sop.transform.position) <= 2 * maxVisibleDistance) {
                    foreach (float r in rotations) {
                        transform.position = position;
                        transform.rotation = Quaternion.Euler(new Vector3(0f, r, 0f));
                        if (objectIsCurrentlyVisible(sop, maxVisibleDistance)) {
                            #if UNITY_EDITOR
                            Debug.Log(position);
                            Debug.Log(r);
                            #endif
                            goodPositions.Add(position);
                            goodRotations.Add(r);
                        }
                    }
                }
            }

            actionVector3sReturn = goodPositions.ToArray();
            actionFloatsReturn = goodRotations.ToArray();

            foreach (GameObject go in visibilityCapsules) {
                go.SetActive(true);
            }

            transform.position = savedPosition;
            transform.rotation = savedRotation;

            actionFinished(true);
        }

		public void DisableAllObjectsOfType(ServerAction action) {
			string type = action.objectId;
			foreach (SimObjPhysics so in GameObject.FindObjectsOfType<SimObjPhysics>()) {
				if (Enum.GetName(typeof(SimObjType), so.Type) == type) {
					so.gameObject.SetActive(false);
				}
			}
			actionFinished(true);
		}

        public void DisableObject(ServerAction action) {
			string objectId = action.objectId;
			if (uniqueIdToSimObjPhysics.ContainsKey(objectId)) {
                uniqueIdToSimObjPhysics[objectId].gameObject.SetActive(false);
                actionFinished(true);
            } else {
                actionFinished(false);
            }
		}

        public void EnableObject(ServerAction action) {
			string objectId = action.objectId;
			if (uniqueIdToSimObjPhysics.ContainsKey(objectId)) {
                uniqueIdToSimObjPhysics[objectId].gameObject.SetActive(true);
                actionFinished(true);
            } else {
                actionFinished(false);
            }
		}

        public void StackBooks(ServerAction action) {
            GameObject topLevelObject = GameObject.Find("HideAndSeek");
            SimObjPhysics[] hideSeekObjects = topLevelObject.GetComponentsInChildren<SimObjPhysics>();

            HashSet<string> seenBooks = new HashSet<string>();
            List<HashSet<SimObjPhysics>> groups = new List<HashSet<SimObjPhysics>>();
            foreach (SimObjPhysics sop in hideSeekObjects) {
                HashSet<SimObjPhysics> group = new HashSet<SimObjPhysics>();
                if (sop.UniqueID.StartsWith("Book|")) {
                    if (!seenBooks.Contains(sop.UniqueID)) {
                        HashSet<SimObjPhysics> objectsNearBook = objectsInBox(
                            sop.transform.position.x, sop.transform.position.z);
                        group.Add(sop);
                        seenBooks.Add(sop.UniqueID);
                        foreach (SimObjPhysics possibleBook in objectsNearBook) {
                            if (possibleBook.UniqueID.StartsWith("Book|") && 
                                !seenBooks.Contains(possibleBook.UniqueID)) {
                                group.Add(possibleBook);
                                seenBooks.Add(possibleBook.UniqueID);
                            }
                        }
                        groups.Add(group);
                    }
                }
            }

            foreach (HashSet<SimObjPhysics> group in groups) {
                SimObjPhysics topBook = null;
                GameObject topMesh = null;
                GameObject topColliders = null;
                GameObject topTrigColliders = null;
                GameObject topVisPoints = null;
                foreach(SimObjPhysics so in group) {
                    if (topBook == null) {
                        topBook = so;  
                        topMesh = so.gameObject.transform.Find("mesh").gameObject;
                        topColliders = so.gameObject.transform.Find("Colliders").gameObject;
                        topTrigColliders = so.gameObject.transform.Find("TriggerColliders").gameObject;
                        topVisPoints = so.gameObject.transform.Find("VisibilityPoints").gameObject;
                    } else {
                        GameObject mesh = so.gameObject.transform.Find("mesh").gameObject;
                        mesh.transform.parent = topMesh.transform;

                        GameObject colliders = so.gameObject.transform.Find("Colliders").gameObject;
                        foreach (Transform t in colliders.GetComponentsInChildren<Transform>()) {
                            if (t != colliders.transform) {
                                t.parent = topColliders.transform;
                            }
                        }

                        GameObject trigColliders = so.gameObject.transform.Find("TriggerColliders").gameObject;
                        foreach (Transform t in trigColliders.GetComponentsInChildren<Transform>()) {
                            if (t != colliders.transform) {
                                t.parent = topTrigColliders.transform;
                            }
                        }

                        GameObject visPoints = so.gameObject.transform.Find("VisibilityPoints").gameObject;
                        foreach (Transform t in visPoints.GetComponentsInChildren<Transform>()) {
                            if (t != visPoints.transform) {
                                t.parent = topVisPoints.transform;
                            }
                        }

                        uniqueIdToSimObjPhysics.Remove(so.UniqueID);
                        so.gameObject.SetActive(false);
                    }
                }
            }
            actionFinished(true);
        }

        public void RandomizeHideSeekObjects(ServerAction action) {
            GameObject topLevelObject = GameObject.Find("HideAndSeek");
            SimObjPhysics[] hideSeekObjects = topLevelObject.GetComponentsInChildren<SimObjPhysics>();

            System.Random rnd = new System.Random(action.randomSeed);
            HashSet<string> seenBooks = new HashSet<string>();
            foreach (SimObjPhysics sop in hideSeekObjects) {
                HashSet<SimObjPhysics> group = new HashSet<SimObjPhysics>();
                if (sop.UniqueID.StartsWith("Book|")) {
                    if (!seenBooks.Contains(sop.UniqueID)) {
                        HashSet<SimObjPhysics> objectsNearBook = objectsInBox(
                            sop.transform.position.x, sop.transform.position.z);
                        group.Add(sop);
                        seenBooks.Add(sop.UniqueID);
                        foreach (SimObjPhysics possibleBook in objectsNearBook) {
                            if (possibleBook.UniqueID.StartsWith("Book|") && 
                                !seenBooks.Contains(possibleBook.UniqueID)) {
                                group.Add(possibleBook);
                                seenBooks.Add(possibleBook.UniqueID);
                            }
                        }
                    }
                } else {
                    group.Add(sop);
                }

                if (group.Count != 0 && rnd.NextDouble() <= action.removeProb) {
                    foreach (SimObjPhysics toRemove in group) {
                        toRemove.gameObject.SetActive(false);
                    }
                }
            }
            snapToGrid(); // This snapping seems necessary for some reason, really doesn't make any sense.
            actionFinished(true);
        }

        // Following code for calculating the volume of a mesh taken from
        // https://answers.unity.com/questions/52664/how-would-one-calculate-a-3d-mesh-volume-in-unity.html
        // and https://answers.unity.com/questions/52664/how-would-one-calculate-a-3d-mesh-volume-in-unity.html
        //  protected float SignedVolumeOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3) {
        //     float v321 = p3.x * p2.y * p1.z;
        //     float v231 = p2.x * p3.y * p1.z;
        //     float v312 = p3.x * p1.y * p2.z;
        //     float v132 = p1.x * p3.y * p2.z;
        //     float v213 = p2.x * p1.y * p3.z;
        //     float v123 = p1.x * p2.y * p3.z;
        //     return (1.0f / 6.0f) * (-v321 + v231 + v312 - v132 - v213 + v123);
        // }
        // protected float VolumeOfMesh(Mesh mesh) {
        //     float volume = 0;
        //     Vector3[] vertices = mesh.vertices;
        //     int[] triangles = mesh.triangles;
        //     Debug.Log(vertices);
        //     Debug.Log(triangles);
        //     for (int i = 0; i < mesh.triangles.Length; i += 3)
        //     {
        //         Vector3 p1 = vertices[triangles[i + 0]];
        //         Vector3 p2 = vertices[triangles[i + 1]];
        //         Vector3 p3 = vertices[triangles[i + 2]];
        //         volume += SignedVolumeOfTriangle(p1, p2, p3);
        //     }
        //     return Mathf.Abs(volume);
        // }

        // public void VolumeOfObject(ServerAction action) {
        //     SimObjPhysics so = uniqueIdToSimObjPhysics[action.objectId];
        //     foreach (MeshFilter meshFilter in so.GetComponentsInChildren<MeshFilter>()) {
        //         Mesh mesh = meshFilter.sharedMesh;
        //         float volume = VolumeOfMesh(mesh);
        //         string msg = "The volume of the mesh is " + volume + " cube units.";
        //         Debug.Log(msg);
        //     }
        // }
        
        // End code for calculating the volume of a mesh

		public void RandomlyOpenCloseObjects(ServerAction action) {
			System.Random rnd = new System.Random (action.randomSeed);
			List<CanOpen_Object> toInteractWith = new List<CanOpen_Object>();
			foreach (SimObjPhysics so in GameObject.FindObjectsOfType<SimObjPhysics>()) {
                CanOpen_Object coo = so.GetComponent<CanOpen_Object>();
                if (coo != null) {
                    if (rnd.NextDouble() < 0.5) {
                        if (!coo.isOpen) {
                            toInteractWith.Add(coo);
                        }
                        
                    } else if (coo.isOpen) {
                        toInteractWith.Add(coo);
                    }
                }
			}
			StartCoroutine (InteractAndWait (toInteractWith));
		}

        public void GetApproximateVolume(ServerAction action) {
            if (uniqueIdToSimObjPhysics.ContainsKey(action.objectId)) {
                SimObjPhysics so = uniqueIdToSimObjPhysics[action.objectId];
                Quaternion oldRotation = so.transform.rotation;
                so.transform.rotation = Quaternion.identity;
                Bounds objBounds = new Bounds(
                    new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity),
                    new Vector3(-float.PositiveInfinity, -float.PositiveInfinity, -float.PositiveInfinity)
                );
                bool hasActiveRenderer = false;
                foreach (Renderer r in so.GetComponentsInChildren<Renderer>()) {
                    if (r.enabled) {
                        hasActiveRenderer = true;
                        objBounds.Encapsulate(r.bounds);
                    }
                }
                if (!hasActiveRenderer) {
                    errorMessage = "Cannot get bounds for " + action.objectId + " as it has no attached (and active) renderers.";
                    Debug.Log(errorMessage);
                    actionFinished(false);
                    return;
                }
                so.transform.rotation = oldRotation;
                Vector3 diffs = objBounds.max - objBounds.min;
                actionFloatReturn = diffs.x * diffs.y * diffs.z;
                #if UNITY_EDITOR
                Debug.Log("Volume is " + actionFloatReturn);
                #endif
                actionFinished(true);
            } else {
                errorMessage = "Invalid objectId " + action.objectId;
                Debug.Log(errorMessage);
                actionFinished(false);
            }
        }

        public void GetVolumeOfAllObjects(ServerAction action) {
            List<string> objectIds = new List<string>();
            List<float> volumes = new List<float>();
            foreach (SimObjPhysics so in FindObjectsOfType<SimObjPhysics>()) {
                Quaternion oldRotation = so.transform.rotation;
                so.transform.rotation = Quaternion.identity;
                Bounds objBounds = new Bounds(
                    new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity),
                    new Vector3(-float.PositiveInfinity, -float.PositiveInfinity, -float.PositiveInfinity)
                );
                bool hasActiveRenderer = false;
                foreach (Renderer r in so.GetComponentsInChildren<Renderer>()) {
                    if (r.enabled) {
                        hasActiveRenderer = true;
                        objBounds.Encapsulate(r.bounds);
                    }
                }
                if (!hasActiveRenderer) {
                    continue;
                }
                so.transform.rotation = oldRotation;
                Vector3 diffs = objBounds.max - objBounds.min;
                
                objectIds.Add(so.UniqueID);
                volumes.Add(diffs.x * diffs.y * diffs.z);
            }
            actionStringsReturn = objectIds.ToArray();
            actionFloatsReturn = volumes.ToArray();
            actionFinished(true);
        }

        protected void changeObjectBlendMode(SimObjPhysics so, StandardShaderUtils.BlendMode bm, float alpha) {
            HashSet<MeshRenderer> renderersToSkip = new HashSet<MeshRenderer>();
            foreach (SimObjPhysics childSo in so.GetComponentsInChildren<SimObjPhysics>()) {
                if (!childSo.UniqueID.StartsWith("Drawer") && 
                    !childSo.UniqueID.Split('|')[0].EndsWith("Door") && 
                    so.UniqueID != childSo.UniqueID) {
                    foreach (MeshRenderer mr in childSo.GetComponentsInChildren<MeshRenderer>()) {
                        renderersToSkip.Add(mr);
                    }
                }
            }
            
            foreach (MeshRenderer r in so.gameObject.GetComponentsInChildren<MeshRenderer>() as MeshRenderer[]) {
                if (!renderersToSkip.Contains(r)) {
                    Material[] newMaterials = new Material[r.materials.Length];
                    for (int i = 0; i < newMaterials.Length; i++) {
                        newMaterials[i] = new Material(r.materials[i]);
                        StandardShaderUtils.ChangeRenderMode(newMaterials[i], bm);
                        Color color = newMaterials[i].color;
                        color.a = alpha;
                        newMaterials[i].color = color;
                    }
                    r.materials = newMaterials;
                }
            }
        }

        public void MakeObjectTransparent(ServerAction action) {
            if (uniqueIdToSimObjPhysics.ContainsKey(action.objectId)) {
                changeObjectBlendMode(
                    uniqueIdToSimObjPhysics[action.objectId], 
                    StandardShaderUtils.BlendMode.Fade, 
                    0.4f
                );
                actionFinished(true);
            } else {
                errorMessage = "Invalid objectId " + action.objectId;
                Debug.Log(errorMessage);
                actionFinished(false);
            }
        }

        public void MakeObjectOpaque(ServerAction action) {
            if (uniqueIdToSimObjPhysics.ContainsKey(action.objectId)) {
                changeObjectBlendMode(
                    uniqueIdToSimObjPhysics[action.objectId], 
                    StandardShaderUtils.BlendMode.Opaque, 
                    1.0f
                );
                actionFinished(true);
            } else {
                errorMessage = "Invalid objectId " + action.objectId;
                Debug.Log(errorMessage);
                actionFinished(false);
            }
        }

        public void ToggleColorWalkable(ServerAction action) {
            GameObject go = GameObject.Find("WalkablePlanes");
            if (go != null) {
                foreach (Renderer r in go.GetComponentsInChildren<Renderer>()) {
                    r.enabled = !r.enabled;
                }
                actionFinished(true);
                return;
            }
            Vector3[] reachablePositions = getReachablePositions();
            GameObject walkableParent = new GameObject();
            walkableParent.name = "WalkablePlanes";
            GameObject topLevelObject = GameObject.Find("Objects");
            if (topLevelObject != null) {
                walkableParent.transform.parent = topLevelObject.transform;
            }

            int layerMask = 1 << 8;
            foreach (Vector3 p in reachablePositions) {
                RaycastHit hit;
                bool somethingHit = false;
                float y = 0f;
                for (int i = -1; i <= 1; i++) {
                    for (int j = -1; j <= 1; j++) {
                        Vector3 offset = new Vector3(i * 0.41f * gridSize, 0f, i * 0.41f * gridSize);
                        if (Physics.Raycast(p + offset, -transform.up, out hit, 10f, layerMask)) {
                            if (!somethingHit) {
                                y = hit.point.y;
                            } else {
                                y = Math.Max(y, hit.point.y);
                            }
                            somethingHit = true;
                        }
                    }
                }
                if (somethingHit) {
                    y += 0.01f;
                    GameObject plane = Instantiate(
                        Resources.Load("BluePlane") as GameObject,
                        new Vector3(p.x, y, p.z),
                        Quaternion.identity
                    ) as GameObject;
                    plane.name = "WalkablePlane";
                    plane.transform.parent = walkableParent.transform;
                    plane.transform.localScale = new Vector3(gridSize * 0.1f, 0.1f, gridSize * 0.1f);
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

            HashSet<string> objectIdsContained = new HashSet<string>();
            foreach (SimObjPhysics so in uniqueIdToSimObjPhysics.Values) {
                if (so.ReceptacleTriggerBoxes != null && 
                    so.ReceptacleTriggerBoxes.Length != 0 &&
                    !so.UniqueID.Contains("Top") && // Don't include table tops, counter tops, etc.
                    !so.UniqueID.Contains("Burner") &&
                    !so.UniqueID.Contains("Chair") &&
                    !so.UniqueID.Contains("Sofa") &&
                    !so.UniqueID.Contains("Shelf") &&
                    !so.UniqueID.Contains("Ottoman")) {
                    foreach (string id in so.Contains()) {
                        objectIdsContained.Add(id);
                    }
                }
            }

            Material redMaterial = (Material) Resources.Load("RED", typeof(Material));
            Material greenMaterial = (Material) Resources.Load("GREEN", typeof(Material));
			Collider[] fpsControllerColliders = GameObject.Find("FPSController").GetComponentsInChildren<Collider>();
            k = 0;
			foreach (SimObjPhysics so in newObjects) {
                if (!deleted[k]) {
                    so.GetComponentInChildren<Rigidbody>().isKinematic = true;
                    foreach(Collider c1 in so.GetComponentsInChildren<Collider>()) {
                        foreach(Collider c in fpsControllerColliders) {
                            Physics.IgnoreCollision(c, c1);
                        }
                    }
                    if (objectIdsContained.Contains(so.UniqueID)) {
                        MaskSimObj(so, greenMaterial);
                    } else {
                        MaskSimObj(so, redMaterial);
                    }
                    uniqueIdToSimObjPhysics[so.UniqueID] = so;
                }
                k++;
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
            GameObject go = GameObject.Find("Lighting");
            if (go != null) {
                go.SetActive(false);
            }
            foreach(Renderer r in GameObject.FindObjectsOfType<Renderer>()) {
                bool disableRenderer = false;
                foreach (Material m in r.materials) {
                    if (m.name.Contains("LightRay")) {
                        disableRenderer = true;
                        break;
                    }
                }
                if (disableRenderer) {
                    r.enabled = false;
                } else {
                    Material newMaterial = (Material) Resources.Load("BLUE", typeof(Material));
                    Material[] newMaterials = new Material[r.materials.Length];
                    for (int i = 0; i < newMaterials.Length; i++) {
                        newMaterials[i] = newMaterial;
                    }
                    r.materials = newMaterials;
                }
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
			string prefab = action.objectType;
            int objectVariation = action.objectVariation;
			
			Bounds b = sceneBounds;
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
			SimObjPhysics objForBounds = script.SpawnObject(prefab, false, objectVariation, new Vector3(0.0f, b.max.y + 10.0f, 0.0f), transform.eulerAngles, false, true);

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
			List<SimObjPhysics> newObjects = new List<SimObjPhysics>();

            var xsToTry = new List<float>();
            var zsToTry = new List<float>();
            // xsToTry.Add(-0.1253266f);
            // zsToTry.Add(1.159979f);
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
                        SimObjPhysics hitSimObj = hit.transform.gameObject.GetComponent<SimObjPhysics>();
                        if (hitSimObj == null || hitSimObj.UniqueID.Split('|')[0] != prefab) {
                            Vector3 halfExtents = new Vector3(xExtent / 2.1f, yExtent / 2.1f, zExtent / 2.1f);
                            Vector3 center = hit.point + objCenterRelPos + yOffset;
                            Collider[] colliders = Physics.OverlapBox(center, halfExtents, Quaternion.identity, layerMask);
                            if (colliders.Length == 0) {
                                k++;
                                SimObjPhysics newObj = script.SpawnObject(prefab, false, objectVariation, center - objCenterRelPos, transform.eulerAngles, false, true);
                                if (prefab == "Cup") {
                                    foreach (Collider c in newObj.GetComponentsInChildren<Collider>()) {
                                        c.enabled = false;
                                    }
                                    newObj.GetComponentInChildren<Renderer>().gameObject.AddComponent<BoxCollider>();
                                }
                                newObjects.Add(newObj);
                            } 
                        }
                        // else {
                        //     Debug.Log("Intersects collider:");
                        //     Debug.Log(colliders[0]);
                        // }
                    }
                }
			}
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

        // Taken from https://answers.unity.com/questions/1144378/copy-to-clipboard-with-a-button-unity-53-solution.html
        public static void CopyToClipboard(string s)
        {
            TextEditor te = new TextEditor();
            te.text = s;
            te.SelectAll();
            te.Copy();
        }
        
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
                        var b = GUILayout.Button(o.UniqueID, UnityEditor.EditorStyles.miniButton, GUILayout.MaxWidth(200f));
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
								// if (o.isInteractable == true)
                                // {
                                //     suffix += " INTERACTABLE";
                                // }
							//}

                        }
                  
                        if (GUILayout.Button(o.UniqueID + suffix, UnityEditor.EditorStyles.miniButton, GUILayout.MinWidth(100f))) {
                            CopyToClipboard(o.UniqueID);
                        }
                    }
                }
            }
        }
        #endif
	}

}

