using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityStandardAssets.Characters.FirstPerson;

//this script manages the spawning/placing of sim objects in the scene
public class InstantiatePrefabTest : MonoBehaviour
{

	public GameObject[] prefabs = null;
	private int spawnCount = 0;

	private bool m_Started = false;
	Vector3 gizmopos;
	Vector3 gizmoscale;
	Quaternion gizmoquaternion;

    public GameObject TestPlaceObject;
    public Contains Testreceptbox;

    private List<Vector3> SpawnCorners = new List<Vector3>();

    // //uses the PlaceIn action
    // //The object placed must have the entirety of it's object oriented bounding box (all 8 corners) enclosed within the Receptacle's Box
    // private List<SimObjType> InReceptacles = new List<SimObjType>() 
    // {SimObjType.Drawer, SimObjType.Cabinet, SimObjType.Closet, SimObjType.Fridge, SimObjType.Microwave};

    // //uses the PlaceOn action
    // //the object placed only needs the bottom most 4 corners within the Receptacle Box to be placed validly, this allows
    // //things like a tall cup to have the top half of it sticking out of the receptacle box when placed on a table
    // private List<SimObjType> OnReceptacles = new List <SimObjType>()
    // {SimObjType.TableTop, SimObjType.Dresser, SimObjType.CounterTop, SimObjType.Shelf, SimObjType.ArmChair,
    //  SimObjType.Sofa, SimObjType.Ottoman, SimObjType.StoveBurner};

    // //Uses the PlaceIn action
    // //while these receptacles have things placed "in" them, they use the logic of OnReceptacles - Only the bottom 4 corners must be within the
    // //receptacle box for the placement to be valid. This means we can have a Spoon placed IN a cup, but the top half of the spoon is still allowed to stick out
    // private List<SimObjType> InReceptaclesThatOnlyCheckBottomFourCorners = new List <SimObjType>()
    // { SimObjType.Cup, SimObjType.Bowl, SimObjType.GarbageCan, SimObjType.Box, SimObjType.Sink,};

	// Use this for initialization
	void Start()
	{
		//m_Started = true;
	}

	// Update is called once per frame
	void Update()
	{
        if(Input.GetKeyDown(KeyCode.Space))
        {
            
            // if(!TestPlaceObject)
            // return;
            // //PhysicsRemoteFPSAgentController agent = GameObject.Find("FPSController").GetComponent<PhysicsRemoteFPSAgentController>();

            // //string TargetReceptacle = agent.UniqueIDOfClosestReceptacleObject();

            // //Testreceptbox = agent.FindObjectInVisibleSimObjPhysics(TargetReceptacle);
            // GameObject agent = GameObject.Find("FPSController");
            // string receptID;

            // receptID = agent.GetComponent<PhysicsRemoteFPSAgentController>().UniqueIDOfClosestReceptacleObject();

            // if(Testreceptbox.validpointlist.Count > 0)
            // {
            //     //PlaceObject(TestPlaceObject.GetComponent<SimObjPhysics>(), Testreceptbox.validpointlist[0], true);
            //     PlaceObjectReceptacle(Testreceptbox.validpointlist,TestPlaceObject.GetComponent<SimObjPhysics>(), false);
            // }

            // else
            // Debug.Log("No valid points right now!");

        }
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

				so.UniqueID = objectId;
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

        //print(Enum.Parse(typeof(SimObjType), objectType));

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

        //Debug.Log(variation);
        Quaternion quat = Quaternion.Euler(rotation);

		if (ignoreChecks || CheckSpawnArea(candidates[variation].GetComponent<SimObjPhysics>(), position, quat, spawningInHand))
        {
            GameObject prefab = Instantiate(candidates[variation], position, quat) as GameObject;
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
            simObj.uniqueID = objectType + "|" + spawnCount.ToString();
            return simObj;
        }

        return null;
    }

    //call PlaceObject for all points in the passed in ReceptacleSpawnPoint list
    //The list should be sorted by distance to the Agent, so closer points will be checked first.
    public bool PlaceObjectReceptacle(List<ReceptacleSpawnPoint> rsp, SimObjPhysics sop, bool PlaceStationary)
    {
        if(rsp != null)
        {
            foreach (ReceptacleSpawnPoint p in rsp)
            {
                if(PlaceObject(sop, p, PlaceStationary))
                {
                    //found a place to spawn! neato, return success
                    return true;
                    //break;
                }
            }
            //couldn't find valid places to spawn
            #if UNITY_EDITOR
            Debug.Log("PlaceObject checks failed");
            #endif
            return false;
        }
        #if UNITY_EDITOR
        Debug.Log("Null list of points to check, please pass in populated list of <ReceptacleSpawnPoint>?");
        #endif
        //uh, there was nothing in the List for some reason, so failed to spawn
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

    public bool PlaceObject(SimObjPhysics sop, ReceptacleSpawnPoint rsp, bool PlaceStationary)
	{
        if(rsp.ParentSimObjPhys == sop)
        {
            #if UNITY_EDITOR
            Debug.Log("Can't place object inside itself!");
            #endif
            return false;
        }
        
        //remember the original rotation of the sim object if we need to reset it
        Quaternion originalRot = sop.transform.rotation;

        //get the bounding box of the sim object we are trying to place
        BoxCollider oabb = sop.BoundingBox.GetComponent<BoxCollider>();
        
        //zero out rotation to match the target receptacle's rotation
        sop.transform.rotation = rsp.ReceptacleBox.transform.rotation;
        sop.GetComponent<Rigidbody>().velocity = Vector3.zero;

        //degree increment the object will be checked on in each x, y, z local axis
        //we can probably add a thing to make this check more rotations later...
        int degreeIncrement = 45;

        int HowManyRotationsToCheck = 360/degreeIncrement;

        List<RotationAndDistanceValues> ToCheck = new List<RotationAndDistanceValues>(); //we'll check 8 rotations for now, replace the 45 later if we want to adjust the amount of checks

        for(int i = 0; i < HowManyRotationsToCheck; i++)
        {
            if(i > 0)
            {
                sop.transform.Rotate(new Vector3(0, degreeIncrement, 0), Space.Self);
                //ToCheck[i].rotation = sop.transform.rotation;
                
                Vector3 Offset = oabb.ClosestPoint(oabb.center + -Vector3.up * 10);
                Plane BoxBottom = new Plane(rsp.ReceptacleBox.transform.up, Offset);
                float DistanceFromBoxBottomTosop = Math.Abs(BoxBottom.GetDistanceToPoint(sop.transform.position));

                ToCheck.Add(new RotationAndDistanceValues(DistanceFromBoxBottomTosop, sop.transform.rotation));
                //ToCheck[i] = new RotationAndDistanceValues(DistanceFromBoxBottomTosop, sop.transform.rotation);

                Quaternion oldRotation = sop.transform.rotation;

                //now add more points by rotating the x axis at this given y axis
                for(int j = 0; j < HowManyRotationsToCheck; j++)
                {
                    sop.transform.Rotate(new Vector3(degreeIncrement, 0, 0), Space.Self);

                    Offset = oabb.ClosestPoint(oabb.center + -Vector3.up * 10);
                    BoxBottom = new Plane(rsp.ReceptacleBox.transform.up, Offset);
                    DistanceFromBoxBottomTosop = Math.Abs(BoxBottom.GetDistanceToPoint(sop.transform.position)+ 0.01f);

                    ToCheck.Add(new RotationAndDistanceValues(DistanceFromBoxBottomTosop, sop.transform.rotation));
                }

                sop.transform.rotation = oldRotation;

                //now add EVEN more points by rotating the z axis at this given y axis
                for(int j = 0; j < HowManyRotationsToCheck; j++)
                {
                    sop.transform.Rotate(new Vector3(0, 0, degreeIncrement), Space.Self);

                    Offset = oabb.ClosestPoint(oabb.center + -Vector3.up * 10);
                    BoxBottom = new Plane(rsp.ReceptacleBox.transform.up, Offset);
                    DistanceFromBoxBottomTosop = Math.Abs(BoxBottom.GetDistanceToPoint(sop.transform.position));

                    ToCheck.Add(new RotationAndDistanceValues(DistanceFromBoxBottomTosop, sop.transform.rotation));
                }
                         
               sop.transform.rotation = oldRotation;

            }

            else
            {
                //Starting orientation, default at prefab's 0, 0, 0

                Vector3 Offset = oabb.ClosestPoint(oabb.center + -Vector3.up * 10); //was using rsp.point
                Plane BoxBottom = new Plane(rsp.ReceptacleBox.transform.up, Offset);
                float DistanceFromBoxBottomTosop = Math.Abs(BoxBottom.GetDistanceToPoint(sop.transform.position));

                ToCheck.Add(new RotationAndDistanceValues(DistanceFromBoxBottomTosop, sop.transform.rotation));
                //ToCheck[i] = new RotationAndDistanceValues(DistanceFromBoxBottomTosop, sop.transform.rotation);

            }
        }

        foreach(RotationAndDistanceValues quat in ToCheck)
        {
            //if spawn area is clear, spawn it and return true that we spawned it
            //origin point we are checking + sim object's upward vector * distance from bottom of box to the transform will give the center of the CheckSpawnArea box
            if(CheckSpawnArea(sop, rsp.Point + rsp.ParentSimObjPhys.transform.up * (quat.distance + 0.01f), quat.rotation, false))
            {
                //print(quat.distance);
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
                //sort corners so that first four corners are the corners closest to the spawn point we are checking against
                SpawnCorners.Sort(delegate(Vector3 p1, Vector3 p2)
                {
                    //sort by making a plane where rsp.point is, find the four corners closest to that point
                    //return rspPlane.GetDistanceToPoint(p1).CompareTo(rspPlane.GetDistanceToPoint(p2));

                    return Vector3.Distance(p1, rsp.Point).CompareTo(Vector3.Distance(p2, rsp.Point));

                    // return Vector3.Distance(new Vector3(0, p1.y, 0), new Vector3(0, rsp.Point.y, 0)).CompareTo(
                    // Vector3.Distance(new Vector3(0, p2.y, 0), new Vector3(0, rsp.Point.y, 0)));

                });

                //now the SpawnCorners list is sorted with the four corners closest in y-position difference to the spawn point first
                for(int i = 0; i < HowManyCornersToCheck; i++)
                {
                    //print("Checking " + (i-1));
                    if(rsp.Script.CheckIfPointIsInsideReceptacleTriggerBox(SpawnCorners[i]))
                    {
                        CornerCount++;
                    }
                }

                if(CornerCount < HowManyCornersToCheck)
                {
                    sop.transform.rotation = originalRot;
                    //Destroy(placeholderPosition.gameObject);
                    // #if UNITY_EDITOR
                    // Debug.Log(sop.name + " cannot fit in target receptacle: " + rsp.ParentSimObjPhys.name + " at coordinate " + rsp.Point);
                    // #endif
                    
                    return false;
                }

                //we passed all the checks! Place the object now!
                GameObject topObject = GameObject.Find("Objects");
                //parent to the Objects transform
                sop.transform.SetParent(topObject.transform);
                //translate position of the target sim object to the rsp.Point and offset in local y up
                sop.transform.position = rsp.Point + rsp.ReceptacleBox.transform.up * (quat.distance + 0.01f);//rsp.Point + sop.transform.up * DistanceFromBottomOfBoxToTransform;
                sop.transform.rotation = quat.rotation;

                //set true if we want objects to be stationary when placed. (if placed on uneven surface, object remains stationary)
                //if falce, once placed the object will resolve with physics (if placed on uneven surface object might slide or roll)
                if(PlaceStationary == true)
                sop.GetComponent<Rigidbody>().isKinematic = true;

                else
                {
                    sop.GetComponent<Rigidbody>().isKinematic = false;
                }

                
                #if UNITY_EDITOR
                Debug.Log(sop.name + " succesfully spawned in " +rsp.ParentSimObjPhys.name + " at coordinate " + rsp.Point);
                #endif

                return true;
            }
        }
       
        //reset rotation if no valid spawns found
        sop.transform.rotation = originalRot;
        

        //oh now we couldn't spawn it, all the spawn areas were not clear
        //Debug.Log("Spawn Area not clear at" + rsp.Point + ", failed to spawn");
        return false;
	}

	//IMPORTANT INFO!//
    //The prefab MUST have a Bounding Box with zeroed out transform, rotation, and 1, 1, 1 scale
    //All adjustments to the Bounding Box must be done on the collider only using the
    //"Edit Collider" button if you need to change the size
    //this assumes that the BoundingBox transform is zeroed out according to the root transform of the prefab
    private bool CheckSpawnArea(SimObjPhysics simObj, Vector3 position, Quaternion rotation, bool spawningInHand)
    {
        //create a dummy gameobject that is instantiated then rotated to get the actual
        //location and orientation of the spawn area
        Transform placeholderPosition = new GameObject("placeholderPosition").transform;

        //this is now in the exact position the object will spawn at
        placeholderPosition.transform.position = position;

        //get how much and in what direction the bounding box might be offset from its parent sim object
        //Vector3 OffsetPos = simObj.BoundingBox.transform.position - simObj.transform.position;

        GameObject placeBox = Instantiate(simObj.BoundingBox, position /*+ OffsetPos*/, placeholderPosition.transform.rotation);
        placeBox.transform.SetParent(placeholderPosition);
        placeBox.transform.localPosition = simObj.BoundingBox.transform.localPosition;

        //rotate it after creating the offset so that the offset's local position is maintained
        placeholderPosition.transform.rotation = rotation;


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

        BoxCollider pbbc = placeBox.GetComponent<BoxCollider>();
        //print("extents:" + placeBox.GetComponent<BoxCollider>().size);

        Collider[] hitColliders = Physics.OverlapBox(placeBox.transform.TransformPoint(pbbc.center)/* placeBox.transform.position*/,
                                                     placeBox.GetComponent<BoxCollider>().size / 2.0f, placeholderPosition.transform.rotation,
                                                     layermask, QueryTriggerInteraction.Ignore);


        // Collider[] hitColliders = Physics.OverlapBox(inst.transform.position,
        //                                              instantbox.size / 2, rotation,
        //                                              layermask, QueryTriggerInteraction.Ignore);
        
        //keep track of all 8 corners of the OverlapBox
        List<Vector3> corners = new List<Vector3>();
        //bottom forward right
        corners.Add(placeBox.transform.TransformPoint(pbbc.center + new Vector3(pbbc.size.x, -pbbc.size.y, pbbc.size.z) * 0.5f));
        //bottom forward left
        corners.Add(placeBox.transform.TransformPoint(pbbc.center + new Vector3(-pbbc.size.x, -pbbc.size.y, pbbc.size.z) * 0.5f));
        //bottom back left
        corners.Add(placeBox.transform.TransformPoint(pbbc.center + new Vector3(-pbbc.size.x, -pbbc.size.y, -pbbc.size.z) * 0.5f));
        //bottom back right
        corners.Add(placeBox.transform.TransformPoint(pbbc.center+ new Vector3(pbbc.size.x, -pbbc.size.y, -pbbc.size.z) * 0.5f));

        //top forward right
        corners.Add(placeBox.transform.TransformPoint(pbbc.center + new Vector3(pbbc.size.x, pbbc.size.y, pbbc.size.z) * 0.5f));
        //top forward left
        corners.Add(placeBox.transform.TransformPoint(pbbc.center + new Vector3(-pbbc.size.x, pbbc.size.y, pbbc.size.z) * 0.5f));
        //top back left
        corners.Add(placeBox.transform.TransformPoint(pbbc.center + new Vector3(-pbbc.size.x, pbbc.size.y, -pbbc.size.z) * 0.5f));
        //top back right
        corners.Add(placeBox.transform.TransformPoint(pbbc.center+ new Vector3(pbbc.size.x, pbbc.size.y, -pbbc.size.z) * 0.5f));

        SpawnCorners = corners;

        #if UNITY_EDITOR
		m_Started = true;     
        gizmopos = placeBox.transform.TransformPoint(pbbc.center); 
        //gizmopos = inst.transform.position;
        gizmoscale = pbbc.size;
        //gizmoscale = simObj.BoundingBox.GetComponent<BoxCollider>().size;
        gizmoquaternion = placeholderPosition.transform.rotation;
        #endif

        //destroy the dummy object, we don't need it anymore
        Destroy(placeholderPosition.gameObject);

        //if a collider was hit, then the space is not clear to spawn
		if (hitColliders.Length > 0)
		{
            
            // // print(hitColliders.Length);
            // foreach(Collider c in hitColliders)
            // {
            //     print(c.name);
            //     print(c.transform.position);
            // }
			return false;
		}

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
