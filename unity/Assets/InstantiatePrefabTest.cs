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

    private Vector3[] SpawnCorners;

    //Receptacle Objects that must have objects completely inside the receptacletriggerbox if placed/spawned in
    private List<SimObjType> InReceptacles = new List<SimObjType>() 
    {SimObjType.Drawer, SimObjType.Cabinet, SimObjType.Closet, SimObjType.Fridge, SimObjType.Box, SimObjType.Bowl, SimObjType.GarbageCan,
    };

    //Receptacle Objects that can have objects placed on top of the receptacletriggerbox if placed/spawned on
    private List<SimObjType> OnReceptacles = new List <SimObjType>()
    {SimObjType.TableTop, SimObjType.Dresser, SimObjType.CounterTop, SimObjType.Shelf, SimObjType.ArmChair,
     SimObjType.Sofa, SimObjType.Sink, SimObjType.ButterKnife, SimObjType.Ottoman, SimObjType.Cup};

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

    public void RandomPlaceObjects()
    {

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
            return false;
        }
        //uh, there was nothing in the List for some reason, so failed to spawn
        return false;
    }

    //Place Sim Object (sop) at the given (position) inside/on the receptbox relative to the rotation of the receptacle
    //this is used for initial spawning/randomization of objects in a scene, and for Placing an Object onto a valid
    //receptacle without using just physics to resolve
	public bool PlaceObject(SimObjPhysics sop, ReceptacleSpawnPoint rsp, bool PlaceStationary)
	{
        if(rsp.ParentSimObjPhys == sop)
        {
            Debug.Log("Can't place object inside itself!");
            return false;
        }
        //zero out rotation to match the target receptacle's rotation
        sop.transform.rotation = rsp.ReceptacleBox.transform.rotation;
        sop.GetComponent<Rigidbody>().velocity = Vector3.zero;

        BoxCollider oabb = sop.BoundingBox.GetComponent<BoxCollider>();

        //get position of the sim object's transform.
        Vector3 p1 = sop.transform.position;

        //ceate a plane centered at the created oabb, set it's up normal to the same as the target receptacle box
        Plane BottomOfBox = new Plane(rsp.ReceptacleBox.transform.up, oabb.transform.TransformPoint(oabb.center + new Vector3(oabb.center.x, -oabb.size.y * 0.5f, oabb.center.z)));

        //distance from created plate
        float DistanceFromBottomOfBoxToTransform = BottomOfBox.GetDistanceToPoint(p1) + 0.01f; //adding .01 buffer cause physics be damned
        //might have to adjust this offset as we test against uneven surfaces like sinks, other meshess
        //Debug.DrawLine(Vector3.zero, BottomOfBoxPoint, Color.blue, 100f);

        //if spawn area is clear, spawn it and return true that we spawned it
        if(CheckSpawnArea(sop, rsp.Point + sop.transform.up * DistanceFromBottomOfBoxToTransform, sop.transform.rotation, false))
        {
            //now to do a check to make sure the sim object is contained within the Receptacle box, and doesn't have
            //bits of it hanging out

            //Check the ReceptacleBox's Sim Object component to see what Type it is. Then check to
            //see if the type is the kind where the Object placed must be completely contained or just the bottom 4 corners contained
            int HowManyCornersToCheck = 0;
            if(OnReceptacles.Contains(rsp.ParentSimObjPhys.ObjType))
            {
                //check that only the bottom 4 corners are in bounds
                HowManyCornersToCheck = 4;
            }

            if(InReceptacles.Contains(rsp.ParentSimObjPhys.ObjType))
            {
                //check that all 8 corners are within bounds
                HowManyCornersToCheck = 8;
            }

            for(int i = 1; i < HowManyCornersToCheck; i++)
            {
                //print("Checking " + (i-1));
                if(!rsp.Script.CheckIfPointIsInsideReceptacleTriggerBox(SpawnCorners[i - 1]))
                return false;
            }

            GameObject topObject = GameObject.Find("Objects");
            sop.transform.SetParent(topObject.transform);
            sop.transform.position = rsp.Point + sop.transform.up * DistanceFromBottomOfBoxToTransform;

            //set true if we want objects to be stationary when placed. (if placed on uneven surface, object remains stationary)
            //if falce, once placed the object will resolve with physics (if placed on uneven surface object might slide or roll)
            if(PlaceStationary)
            sop.GetComponent<Rigidbody>().isKinematic = true;

            return true;
        }

        //oh now we couldn't spawn it
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

        placeholderPosition.transform.position = position;

        GameObject inst = Instantiate(placeholderPosition.gameObject, placeholderPosition, false);
        inst.transform.localPosition = simObj.BoundingBox.GetComponent<BoxCollider>().center;

        BoxCollider instantbox = inst.AddComponent<BoxCollider>();
        instantbox.isTrigger = true;
        instantbox.size = simObj.BoundingBox.GetComponent<BoxCollider>().size;

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

        //BoxCollider sobb = simObj.BoundingBox.GetComponent<BoxCollider>();

        Collider[] hitColliders = Physics.OverlapBox(inst.transform.position,
                                                     instantbox.size / 2, rotation,
                                                     layermask, QueryTriggerInteraction.Ignore);
        
        //keep track of all 8 corners of the OverlapBox
        List<Vector3> corners = new List<Vector3>();
        //bottom forward right
        corners.Add(inst.transform.TransformPoint(instantbox.center + new Vector3(instantbox.size.x, -instantbox.size.y, instantbox.size.z) * 0.5f));
        //bottom forward left
        corners.Add(inst.transform.TransformPoint(instantbox.center + new Vector3(-instantbox.size.x, -instantbox.size.y, instantbox.size.z) * 0.5f));
        //bottom back left
        corners.Add(inst.transform.TransformPoint(instantbox.center + new Vector3(-instantbox.size.x, -instantbox.size.y, -instantbox.size.z) * 0.5f));
        //bottom back right
        corners.Add(inst.transform.TransformPoint(instantbox.center+ new Vector3(instantbox.size.x, -instantbox.size.y, -instantbox.size.z) * 0.5f));

        //top forward right
        corners.Add(inst.transform.TransformPoint(instantbox.center + new Vector3(instantbox.size.x, instantbox.size.y, instantbox.size.z) * 0.5f));
        //top forward left
        corners.Add(inst.transform.TransformPoint(instantbox.center + new Vector3(-instantbox.size.x, instantbox.size.y, instantbox.size.z) * 0.5f));
        //top back left
        corners.Add(inst.transform.TransformPoint(instantbox.center + new Vector3(-instantbox.size.x, instantbox.size.y, -instantbox.size.z) * 0.5f));
        //top back right
        corners.Add(inst.transform.TransformPoint(instantbox.center+ new Vector3(instantbox.size.x, instantbox.size.y, -instantbox.size.z) * 0.5f));

        // //top forward right
        // corners.Add(inst.transform.position + new Vector3(sobb.size.x, sobb.size.y, sobb.size.z) * 0.5f);
        // //top forward left
        // corners.Add(inst.transform.position + new Vector3(-sobb.size.x, sobb.size.y, sobb.size.z) * 0.5f);
        // //top back left
        // corners.Add(inst.transform.position + new Vector3(-sobb.size.x, sobb.size.y, -sobb.size.z) * 0.5f);
        // //top back right
        // corners.Add(inst.transform.position + new Vector3(sobb.size.x, sobb.size.y, -sobb.size.z) * 0.5f);
        
        SpawnCorners = corners.ToArray();

        #if UNITY_EDITOR
		m_Started = true;      
        gizmopos = inst.transform.position;
        gizmoscale = simObj.BoundingBox.GetComponent<BoxCollider>().size;
        gizmoquaternion = rotation;
        #endif
        //destroy the dummy object, we don't need it anymore
        Destroy(placeholderPosition.gameObject);

        //if a collider was hit, then the space is not clear to spawn
		if (hitColliders.Length > 0)
		{
        // #if UNITY_EDITOR
        // 			int i = 0;
        // 			//Check when there is a new collider coming into contact with the box
        // 			while (i < hitColliders.Length)
        // 			{
        // 				//Output all of the collider names
        // 				Debug.Log("Hit : " + hitColliders[i].transform.root.name + i);
        // 				//Increase the number of Colliders in the array
        // 				i++;
        // 			}
        // #endif
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
            foreach (Vector3 point in SpawnCorners)
            {
                Gizmos.DrawSphere(point, 0.005f);
            }
        }
    }
#endif

}
