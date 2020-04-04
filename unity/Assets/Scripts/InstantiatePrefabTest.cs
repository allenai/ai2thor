using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityStandardAssets.Characters.FirstPerson;
using System.Linq;

//this script manages the spawning/placing of sim objects in the scene
public class InstantiatePrefabTest : MonoBehaviour
{
	public GameObject[] prefabs = null;
	private int spawnCount = 0;
    
    #if UNITY_EDITOR
	private bool m_Started = false;
    #endif

	Vector3 gizmopos;
	Vector3 gizmoscale;
	Quaternion gizmoquaternion;
    private float yoffset = 0.005f; //y axis offset of placing objects, useful to allow objects to fall just a tiny bit to allow physics to resolve consistently

    private List<Vector3> SpawnCorners = new List<Vector3>();

	// Use this for initialization
	void Start()
	{
        //m_Started = true;
    }

    // Update is called once per frame
    void Update()
	{

	}

    //spawn an object from the Array of prefabs. Used to spawn from a specific set of Prefabs
    //used for Hide and Seek stuff
	public SimObjPhysics Spawn(string prefabType, string objectId, Vector3 position)
	{
		GameObject topObject = GameObject.Find("Objects");

		foreach (GameObject prefab in prefabs)
		{
			if (prefab.name.Contains(prefabType))
			{
				GameObject go = Instantiate(prefab, position, Quaternion.identity) as GameObject;
				go.transform.SetParent(topObject.transform);
				SimObjPhysics so = go.GetComponentInChildren<SimObjPhysics>();
				if (so == null)
				{
					go.AddComponent<SimObjPhysics>();
					so = go.GetComponentInChildren<SimObjPhysics>();
				}

				so.ObjectID = objectId;
				return so;
			}
		}

		return null;
	}

	public SimObjPhysics SpawnObject(string objectType, bool randomize, int variation, Vector3 position, Vector3 rotation, bool spawningInHand)
    {
        return SpawnObject(objectType, randomize, variation, position, rotation, spawningInHand, false);
    }

    public Bounds BoundsOfObject(string objectType, int variation)
    {
        //GameObject topObject = GameObject.Find("Objects");
        List<GameObject> candidates = new List<GameObject>();
        foreach (GameObject go in prefabs) {
            if (go.GetComponent<SimObjPhysics>().Type == (SimObjType)Enum.Parse(typeof(SimObjType), objectType)) {
                candidates.Add(go);
            }
        }

        Bounds objBounds = new Bounds(
            new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity),
            new Vector3(-float.PositiveInfinity, -float.PositiveInfinity, -float.PositiveInfinity)
        );
        foreach(Renderer r in candidates[variation - 1].GetComponentsInChildren<Renderer>()) {
            if (r.enabled) {
                objBounds.Encapsulate(r.bounds);
            }
        }
        return objBounds;
    }

    //used to spawn an object at a position or in the Agent's hand, used for Hide and Seek
    //--
	//object type - from SimObjType which object to spawn
    //randomize - should the spawner randomly pick an object to spawn
    //variation - which specific version of the object (1, 2, 3), set to 0 if no specific variation is wanted
    //position - where spawn?
    //rotation - orientation when spawned?
    //spawningInHand - adjusts layermask depending on if the object is going to spawn directly in the agent's hand vs spawning in the environment
    //ignoreChecks - bool to ignore checks and spawn anyway
    //--
    public SimObjPhysics SpawnObject(string objectType, bool randomize, int variation, Vector3 position, Vector3 rotation, bool spawningInHand, bool ignoreChecks)
    {
        GameObject topObject = GameObject.Find("Objects");

        List<GameObject> candidates = new List<GameObject>();

        foreach (GameObject go in prefabs)
        {
            //does a prefab of objectType exist in the current array of prefabs to spawn?
            if (go.GetComponent<SimObjPhysics>().Type == (SimObjType)Enum.Parse(typeof(SimObjType), objectType))
            {
                candidates.Add(go);
            }
        }

        //ok time to spawn a sim object!
        SimObjPhysics simObj = null;

        // Figure out which variation to use, if no variation use first candidate found
        if (randomize)
		{
			variation = UnityEngine.Random.Range(1, candidates.Count);
		}
		if (variation != 0) {
			variation -= 1;
		}

        Quaternion quat = Quaternion.Euler(rotation);

		if (ignoreChecks || CheckSpawnArea(candidates[variation].GetComponent<SimObjPhysics>(), position, quat, spawningInHand))
        {
            GameObject prefab = Instantiate(candidates[variation], position, quat) as GameObject;
            if (!ignoreChecks) {
                if (UtilityFunctions.isObjectColliding(
                    prefab, 
                    new List<GameObject>(from agent in GameObject.FindObjectsOfType<BaseFPSAgentController>() select agent.gameObject))
                ) {
                    Debug.Log("On spawning object the area was not clear despite CheckSpawnArea saying it was.");
                    prefab.SetActive(false);
                    return null;
                }
            }
            prefab.transform.SetParent(topObject.transform);
            simObj = prefab.GetComponent<SimObjPhysics>();
            spawnCount++;
        }
        else
        {
            return null;
        }

        //ok make sure we did actually spawn something now, and give it an Id number
        if (simObj)
        {
            simObj.objectID = objectType + "|" + spawnCount.ToString();
            return simObj;
        }

        return null;
    }

    //call PlaceObject for all points in the passed in ReceptacleSpawnPoint list
    //The ReceptacleSpawnPoint list should be sorted based on what we are doing. If placing from the agent's hand, the list
    //should be sorted by distance to agent so the closest points are checked first. If used for Random Initial Spawn, it should
    //be randomized so that the random spawn is... random
    public bool PlaceObjectReceptacle(List<ReceptacleSpawnPoint> rsps, SimObjPhysics sop, bool PlaceStationary, int maxPlacementAttempts, int degreeIncrement, bool AlwaysPlaceUpright)
    {
        
        if(rsps == null)
        {
            #if UNITY_EDITOR
            Debug.Log("Null list of points to check, please pass in populated list of <ReceptacleSpawnPoint>?");
            #endif
            return false; //uh, there was nothing in the List for some reason, so failed to spawn
        }
        if (rsps.Count == 0)
        {
            return false;
        }

        List<ReceptacleSpawnPoint> goodRsps = new List<ReceptacleSpawnPoint>();

        //only add spawn points to try if the point's parent is not an object specific receptacle, that is handled in RandomSpawnRequiredSceneObjects
        foreach (ReceptacleSpawnPoint p in rsps) {
            if(!p.ParentSimObjPhys.GetComponent<SimObjPhysics>().DoesThisObjectHaveThisSecondaryProperty
                (SimObjSecondaryProperty.ObjectSpecificReceptacle)) {
                goodRsps.Add(p);
            }
        }

        //try a number of spawnpoints in this specific receptacle up to the maxPlacementAttempts
        int tries = 0;
        foreach (ReceptacleSpawnPoint p in goodRsps)
        {
            if (PlaceObject(sop, p, PlaceStationary, degreeIncrement, AlwaysPlaceUpright))
            {
                return true;
            }
            tries += 1;
            if (maxPlacementAttempts > 0 && tries > maxPlacementAttempts)
            {
                break;
            }
        }

        //couldn't find valid places to spawn
        return false;
    }

    //same as PlaceObjectReceptacle but instead only succeeds if final placed object is within viewport

    public bool PlaceObjectReceptacleInViewport(List<ReceptacleSpawnPoint> rsps, SimObjPhysics sop, bool PlaceStationary, int maxPlacementAttempts, int degreeIncrement, bool AlwaysPlaceUpright)
    {
        
        if(rsps == null)
        {
            #if UNITY_EDITOR
            Debug.Log("Null list of points to check, please pass in populated list of <ReceptacleSpawnPoint>?");
            #endif
            return false; //uh, there was nothing in the List for some reason, so failed to spawn
        }

        if (rsps.Count == 0)
        {
            return false;
        }

        List<ReceptacleSpawnPoint> goodRsps = new List<ReceptacleSpawnPoint>();
        foreach (ReceptacleSpawnPoint p in rsps) {
            if(!p.ParentSimObjPhys.GetComponent<SimObjPhysics>().DoesThisObjectHaveThisSecondaryProperty
                (SimObjSecondaryProperty.ObjectSpecificReceptacle)) {
                goodRsps.Add(p);
            }
        }

        int tries = 0;
        foreach (ReceptacleSpawnPoint p in goodRsps)
        {
            //if this is an Object Specific Receptacle, stop this check right now! I mean it!
            //Placing objects in/on an Object Specific Receptacle uses different logic to place the
            //object at the Attachemnet point rather than in the spawn area, so stop this right now!

            if (PlaceObject(sop, p, PlaceStationary, degreeIncrement, AlwaysPlaceUpright))
            {
                //check to make sure the placed object is within the viewport
                BaseFPSAgentController primaryAgent = GameObject.Find("PhysicsSceneManager").GetComponent<AgentManager>().ReturnPrimaryAgent();
                if(primaryAgent.GetComponent<PhysicsRemoteFPSAgentController>().objectIsOnScreen(sop))
                {
                    return true;
                }
            }

            tries += 1;
            if (maxPlacementAttempts > 0 && tries > maxPlacementAttempts)
            {
                break;
            }
        }

        //couldn't find valid places to spawn
        return false;
    }


    //use this to keep track of a Rotation and Distance for use in PlaceObject
    public class RotationAndDistanceValues
    {
        public float distance;
        public Quaternion rotation;

        public RotationAndDistanceValues(float d, Quaternion r)
        {
            distance = d;
            rotation = r;
        }
    }

    public bool PlaceObject(SimObjPhysics sop, ReceptacleSpawnPoint rsp, bool PlaceStationary, int degreeIncrement, bool AlwaysPlaceUpright)
	{
        if(rsp.ParentSimObjPhys == sop)
        {
            #if UNITY_EDITOR
            Debug.Log("Can't place object inside itself!");
            #endif
            return false;
        }

        //remember the original rotation of the sim object if we need to reset it
        //Quaternion originalRot = sop.transform.rotation;
        Vector3 originalPos = sop.transform.position;
        Quaternion originalRot = sop.transform.rotation;

        //get the bounding box of the sim object we are trying to place
        BoxCollider oabb = sop.BoundingBox.GetComponent<BoxCollider>();
        
        //zero out rotation and velocity/angular velocity, then match the target receptacle's rotation
        sop.transform.rotation = rsp.ReceptacleBox.transform.rotation;
        Rigidbody sopRB = sop.GetComponent<Rigidbody>();
        sopRB.velocity = Vector3.zero;
        sopRB.angularVelocity = Vector3.zero;


        //set 360 degree increment to only check one angle, set smaller increments to check more angles when trying to place (warning THIS WILL GET SLOWER)
        int HowManyRotationsToCheck = 360/degreeIncrement;
        Plane BoxBottom;
        float DistanceFromBoxBottomTosop;

        List<RotationAndDistanceValues> ToCheck = new List<RotationAndDistanceValues>(); //we'll check 8 rotations for now, replace the 45 later if we want to adjust the amount of checks

        //get rotations and distance values for 360/increment number of rotations around just the Y axis
        //we want to check all of these first so that the object is prioritized to be placed "upright"
        for(int i = 0; i < HowManyRotationsToCheck; i++)
        {
            oabb.enabled = true;

            if(i > 0)
            {
                sop.transform.Rotate(new Vector3(0, degreeIncrement, 0), Space.Self);
                //ToCheck[i].rotation = sop.transform.rotation;
                
                Vector3 Offset = oabb.ClosestPoint(oabb.transform.TransformPoint(oabb.center) + -rsp.ReceptacleBox.transform.up * 10);
                BoxBottom = new Plane(rsp.ReceptacleBox.transform.up, Offset);
                DistanceFromBoxBottomTosop = Math.Abs(BoxBottom.GetDistanceToPoint(sop.transform.position));

                ToCheck.Add(new RotationAndDistanceValues(DistanceFromBoxBottomTosop, sop.transform.rotation));
            }

            else
            {
                //no rotate change just yet, check the first position

                Vector3 Offset = oabb.ClosestPoint(oabb.transform.TransformPoint(oabb.center) + -rsp.ReceptacleBox.transform.up * 10); //was using rsp.point
                BoxBottom = new Plane(rsp.ReceptacleBox.transform.up, Offset);
                DistanceFromBoxBottomTosop = BoxBottom.GetDistanceToPoint(sop.transform.position);

                ToCheck.Add(new RotationAndDistanceValues(DistanceFromBoxBottomTosop, sop.transform.rotation));
            }

            oabb.enabled = false;
        }

        //continue to check rotations about the X and Z axes if the object doesn't have to be placed upright
        if(!AlwaysPlaceUpright)
        {
            //ok now try if the X and Z local axis are rotated if it'll fit
            //these values can cause the object to be placed at crazy angles, so we'll check these last
            for(int i = 0; i < HowManyRotationsToCheck; i++)
            {
                oabb.enabled = true;

                if(i > 0)
                {
                    sop.transform.Rotate(new Vector3(0, degreeIncrement, 0), Space.Self);
                    Quaternion oldRotation = sop.transform.rotation;

                    //now add more points by rotating the x axis at this current y rotation
                    for(int j = 0; j < HowManyRotationsToCheck; j++)
                    {
                        sop.transform.Rotate(new Vector3(degreeIncrement, 0, 0), Space.Self);

                        Vector3 Offset = oabb.ClosestPoint(oabb.transform.TransformPoint(oabb.center) + -rsp.ReceptacleBox.transform.up * 10);
                        BoxBottom = new Plane(rsp.ReceptacleBox.transform.up, Offset);
                        DistanceFromBoxBottomTosop = Math.Abs(BoxBottom.GetDistanceToPoint(sop.transform.position));

                        ToCheck.Add(new RotationAndDistanceValues(DistanceFromBoxBottomTosop, sop.transform.rotation));
                    }

                    sop.transform.rotation = oldRotation;

                    //now add EVEN more points by rotating the z axis at this current y rotation
                    for(int j = 0; j < HowManyRotationsToCheck; j++)
                    {
                        sop.transform.Rotate(new Vector3(0, 0, degreeIncrement), Space.Self);

                        Vector3 Offset = oabb.ClosestPoint(oabb.transform.TransformPoint(oabb.center) + -rsp.ReceptacleBox.transform.up * 10);
                        BoxBottom = new Plane(rsp.ReceptacleBox.transform.up, Offset);
                        DistanceFromBoxBottomTosop = Math.Abs(BoxBottom.GetDistanceToPoint(sop.transform.position));

                        ToCheck.Add(new RotationAndDistanceValues(DistanceFromBoxBottomTosop, sop.transform.rotation));
                    }
                            
                sop.transform.rotation = oldRotation;

                }

                oabb.enabled = false;
            }
        }


        foreach(RotationAndDistanceValues quat in ToCheck)
        {
            //if spawn area is clear, spawn it and return true that we spawned it
            if(CheckSpawnArea(sop, rsp.Point + rsp.ParentSimObjPhys.transform.up * (quat.distance + yoffset), quat.rotation, false))
            {

                //translate position of the target sim object to the rsp.Point and offset in local y up
                sop.transform.position = rsp.Point + rsp.ReceptacleBox.transform.up * (quat.distance + yoffset);//rsp.Point + sop.transform.up * DistanceFromBottomOfBoxToTransform;
                sop.transform.rotation = quat.rotation;
                
                //now to do a check to make sure the sim object is contained within the Receptacle box, and doesn't have
                //bits of it hanging out

                //Check the ReceptacleBox's Sim Object component to see what Type it is. Then check to
                //see if the type is the kind where the Object placed must be completely contained or just the bottom 4 corners contained
                int HowManyCornersToCheck = 0;
                if(ReceptacleRestrictions.OnReceptacles.Contains(rsp.ParentSimObjPhys.ObjType))
                {
                    //check that only the bottom 4 corners are in bounds
                    HowManyCornersToCheck = 4;
                }

                if(ReceptacleRestrictions.InReceptacles.Contains(rsp.ParentSimObjPhys.ObjType))
                {
                    //check that all 8 corners are within bounds
                    HowManyCornersToCheck = 8;
                }

                if(ReceptacleRestrictions.InReceptaclesThatOnlyCheckBottomFourCorners.Contains(rsp.ParentSimObjPhys.ObjType))
                {
                    //only check bottom 4 corners even though the action is PlaceIn
                    HowManyCornersToCheck = 4;
                }

                int CornerCount = 0;

                //Plane rspPlane = new Plane(rsp.Point, rsp.ParentSimObjPhys.transform.up);

                //now check the corner count for either the 4 lowest corners, or all 8 corners depending on Corner Count
                //attmpt to sort corners so that first four corners are the corners closest to the spawn point we are checking against
                SpawnCorners.Sort(delegate(Vector3 p1, Vector3 p2)
                {
                    //sort by making a plane where rsp.point is, find the four corners closest to that point
                    //return rspPlane.GetDistanceToPoint(p1).CompareTo(rspPlane.GetDistanceToPoint(p2));
                    //^ this ended up not working because if something is placed at an angle this no longer makes sense...

                    return Vector3.Distance(p1, rsp.Point).CompareTo(Vector3.Distance(p2, rsp.Point));

                    // return Vector3.Distance(new Vector3(0, p1.y, 0), new Vector3(0, rsp.Point.y, 0)).CompareTo(
                    // Vector3.Distance(new Vector3(0, p2.y, 0), new Vector3(0, rsp.Point.y, 0)));

                });

                //ok so this is just checking if there are enough corners in the Receptacle Zone to consider it placed correctly.
                //originally this looped up to i < HowManyCornersToCheck, but if we just check all the corners, regardless of
                //sort order, it seems to bypass the issue above of how to sort the corners to find the "bottom" 4 corners, so uh
                // i guess this might just work without fancy sorting to determine the bottom 4 corners... especially since the "bottom corners" starts to lose meaning as objects are rotated 
                for(int i = 0; i < 8; i++)
                {
                    if(rsp.Script.CheckIfPointIsInsideReceptacleTriggerBox(SpawnCorners[i]))
                    {
                        CornerCount++;
                    }
                }

                //if not enough corners are inside the receptacle, abort
                if(CornerCount < HowManyCornersToCheck)
                {
                    sop.transform.rotation = originalRot;
                    sop.transform.position = originalPos;
                    return false;
                }

                //one final check, make sure all corners of object are "above" the receptacle box in question, so we
                //dont spawn stuff half on a table and it falls over
                foreach (Vector3 v in SpawnCorners)
                {
                    if(!rsp.Script.CheckIfPointIsAboveReceptacleTriggerBox(v))
                    {
                        sop.transform.rotation = originalRot;
                        sop.transform.position = originalPos;
                        return false;
                    }
                }

                //set true if we want objects to be stationary when placed. (if placed on uneven surface, object remains stationary)
                //if false, once placed the object will resolve with physics (if placed on uneven surface object might slide or roll)
                if(PlaceStationary == true)
                {
                    //make object being placed kinematic true
                    sop.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Discrete;
                    sop.GetComponent<Rigidbody>().isKinematic = true;

                    //check if the parent sim object is one that moves like a drawer - and would require this to be parented
                    //if(rsp.ParentSimObjPhys.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanOpen))
                    sop.transform.SetParent(rsp.ParentSimObjPhys.transform);

                    //if this object is a receptacle and it has other objects inside it, drop them all together
                    if(sop.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.Receptacle))
                    {
                        PhysicsRemoteFPSAgentController agent = GameObject.Find("FPSController").GetComponent<PhysicsRemoteFPSAgentController>();
                        agent.DropContainedObjectsStationary(sop);//use stationary version so that colliders are turned back on, but kinematics remain true
                    }

                    //if the target receptacle is a pickupable receptacle, set it to kinematic true as will sence we are placing stationary
                    if(rsp.ParentSimObjPhys.PrimaryProperty == SimObjPrimaryProperty.CanPickup)
                    {
                        rsp.ParentSimObjPhys.GetComponent<Rigidbody>().isKinematic = true;
                    }

                }

                //place stationary false, let physics drop everything too
                else
                {
                    //if not placing stationary, put all objects under Objects game object
                    GameObject topObject = GameObject.Find("Objects");
                    //parent to the Objects transform
                    sop.transform.SetParent(topObject.transform);

                    Rigidbody rb = sop.GetComponent<Rigidbody>();
                    rb.isKinematic = false;
                    rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                    //if this object is a receptacle and it has other objects inside it, drop them all together
                    if(sop.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.Receptacle))
                    {
                        PhysicsRemoteFPSAgentController agent = GameObject.Find("FPSController").GetComponent<PhysicsRemoteFPSAgentController>();
                        agent.DropContainedObjects(sop);
                    }
                }
                sop.isInAgentHand = false;//set agent hand flag

                // #if UNITY_EDITOR
                // Debug.Log(sop.name + " succesfully spawned in " +rsp.ParentSimObjPhys.name + " at coordinate " + rsp.Point);
                // #endif

                return true;
            }
        }
       
        //reset rotation if no valid spawns found
        //oh now we couldn't spawn it, all the spawn areas were not clear
        sop.transform.rotation = originalRot;
        sop.transform.position = originalPos;
        return false;
	}

	//IMPORTANT INFO!//
    //The prefab MUST have a Bounding Box with zeroed out transform, rotation, and 1, 1, 1 scale
    //All adjustments to the Bounding Box must be done on the collider only using the
    //"Edit Collider" button if you need to change the size
    //this assumes that the BoundingBox transform is zeroed out according to the root transform of the prefab
    public bool CheckSpawnArea(SimObjPhysics simObj, Vector3 position, Quaternion rotation, bool spawningInHand)
    {
		int layermask;

		//first do a check to see if the area is clear

        //if spawning in the agent's hand, ignore collisions with the Agent
		if(spawningInHand)
		{
			layermask = 1 << 8;
		}

        //oh we are spawning it somehwere in the environment, we do need to make sure not to spawn inside the agent or the environment
		else
		{
			layermask = (1 << 8) | (1 << 10);
		}


        //track original position and rotation in case we need to reset
        Vector3 originalPos = simObj.transform.position;
        Quaternion originalRot = simObj.transform.rotation;

        //move it into place so the bouding box is in the right spot to generate the overlap box later
        simObj.transform.position = position;
        simObj.transform.rotation = rotation;

        //now let's get the BoundingBox of the simObj as reference cause we need it to create the overlapbox
        GameObject bb = simObj.BoundingBox.transform.gameObject;
        BoxCollider bbcol = bb.GetComponent<BoxCollider>();
        Vector3 bbCenter = bbcol.center;
        Vector3 bbCenterTransformPoint = bb.transform.TransformPoint(bbCenter);
        //keep track of all 8 corners of the OverlapBox
        List<Vector3> corners = new List<Vector3>();
        //bottom forward right
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(bbcol.size.x, -bbcol.size.y, bbcol.size.z) * 0.5f));
        //bottom forward left
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(-bbcol.size.x, -bbcol.size.y, bbcol.size.z) * 0.5f));
        //bottom back left
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(-bbcol.size.x, -bbcol.size.y, -bbcol.size.z) * 0.5f));
        //bottom back right
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(bbcol.size.x, -bbcol.size.y, -bbcol.size.z) * 0.5f));

        //top forward right
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(bbcol.size.x, bbcol.size.y, bbcol.size.z) * 0.5f));
        //top forward left
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(-bbcol.size.x, bbcol.size.y, bbcol.size.z) * 0.5f));
        //top back left
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(-bbcol.size.x, bbcol.size.y, -bbcol.size.z) * 0.5f));
        //top back right
        corners.Add(bb.transform.TransformPoint(bbCenter+ new Vector3(bbcol.size.x, bbcol.size.y, -bbcol.size.z) * 0.5f));

        SpawnCorners = corners;

        #if UNITY_EDITOR
		m_Started = true;     
        gizmopos = bb.transform.TransformPoint(bbCenter); 
        //gizmopos = inst.transform.position;
        gizmoscale = bbcol.size;
        //gizmoscale = simObj.BoundingBox.GetComponent<BoxCollider>().size;
        gizmoquaternion = rotation;
        #endif

        //move sim object back to it's original spot back so the overlap box doesn't hit it
        simObj.transform.position = originalPos;
        simObj.transform.rotation = originalRot;

        //spawn overlap box
        Collider[] hitColliders = Physics.OverlapBox(bbCenterTransformPoint,
                                                     bbcol.size / 2.0f, rotation, 
                                                     layermask, QueryTriggerInteraction.Collide);

        int colliderCount = 0;
        //if a collider was hit, then the space is not clear to spawn
		if (hitColliders.Length > 0)
		{
            //filter out any AgentTriggerBoxes because those should be ignored now
            foreach(Collider c in hitColliders)
            {
                if(c.isTrigger && c.GetComponentInParent<PhysicsRemoteFPSAgentController>())
                continue;

                else
                colliderCount++;
            }

            if(colliderCount > 0)
            return false;
		}

        //nothing was hit, we are good!
		return true;
	}

#if UNITY_EDITOR
	void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        if (m_Started)
        {
            Matrix4x4 cubeTransform = Matrix4x4.TRS(gizmopos, gizmoquaternion, gizmoscale);
            Matrix4x4 oldGizmosMatrix = Gizmos.matrix;

            Gizmos.matrix *= cubeTransform;

            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

            Gizmos.matrix = oldGizmosMatrix;
        }

        if(SpawnCorners!= null)
        {
            int count = 0;
            foreach (Vector3 point in SpawnCorners)
            {
                if(count > 3 )
                Gizmos.color = Color.cyan;

                Gizmos.DrawSphere(point, 0.005f);
                count++;
            }
        }
    }
#endif

}
