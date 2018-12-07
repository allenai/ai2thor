using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class InstantiatePrefabTest : MonoBehaviour
{

	public GameObject[] prefabs = null;
	private int spawnCount = 0;

	private bool m_Started = false;
	Vector3 gizmopos;
	Vector3 gizmoscale;
	Quaternion gizmoquaternion;

    public GameObject TestPlaceObject;
    public Vector3 TestPosition;
    public Contains Testreceptbox;

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
            if(Testreceptbox.validpointlist.Count >0)
            {
                TestPosition = Testreceptbox.validpointlist[0];
                PlaceObject(TestPlaceObject.GetComponent<SimObjPhysics>(), TestPosition, Testreceptbox);
            }

            else
            Debug.Log("No valid points right now!");

        }
	}

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
        GameObject topObject = GameObject.Find("Objects");
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

	//object type - from SimObjType which object to spawn
    //randomize - should the spawner randomly pick an object to spawn
    //variation - which specific version of the object (1, 2, 3), set to 0 if no specific variation is wanted
    //position - where spawn?
    //rotation - orientation when spawned?
    //spawningInHand - adjusts layermask depending on if the object is going to spawn directly in the agent's hand vs spawning in the environment
    //ignoreChecks - bool to ignore checks and spawn anyway
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


    //Place a Game Object on/inside a Receptacle box. This is to replicate pivot behavior
	public void PlaceObject(SimObjPhysics sop, Vector3 position, Contains receptbox)
	{
        //zero out rotation to match the target receptacle's rotation
        sop.transform.rotation = receptbox.transform.rotation;

        BoxCollider oabb = sop.BoundingBox.GetComponent<BoxCollider>();

        //get position of the sim object's transform.
        Vector3 p1 = sop.transform.position;
        //get the vector going down (local space) starting at p1 and going down toward the bottom of the bounding box
        Vector3 p1downvector = p1 + -sop.transform.up;
        //now get the point directly below p1 that is on the bottom of the bounding box.

            #if UNITY_EDITOR
			Debug.DrawLine(p1, p1downvector, Color.magenta, 100f);
			#endif

        //Vector3 BottomOfBoxPoint = oabb.transform.TransformPoint(oabb.center + new Vector3(oabb.center.x, -oabb.size.y * 0.5f, oabb.center.z));

        Plane BottomOfBox = new Plane(sop.transform.up, oabb.transform.TransformPoint(oabb.center + new Vector3(oabb.center.x, -oabb.size.y * 0.5f, oabb.center.z)));

        float DistanceFromBottomOfBoxToTransform = BottomOfBox.GetDistanceToPoint(p1) + 0.001f; //adding .01 buffer cause physics be damned
        //Debug.DrawLine(Vector3.zero, BottomOfBoxPoint, Color.blue, 100f);

        if(CheckSpawnArea(sop, position + sop.transform.up * DistanceFromBottomOfBoxToTransform, sop.transform.rotation, false))
        {
            GameObject topObject = GameObject.Find("Objects");
            sop.transform.SetParent(topObject.transform);
            sop.transform.position = position + sop.transform.up * DistanceFromBottomOfBoxToTransform;
        }
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

        Collider[] hitColliders = Physics.OverlapBox(inst.transform.position,
                                                     simObj.BoundingBox.GetComponent<BoxCollider>().size / 2, rotation,
                                                     layermask, QueryTriggerInteraction.Ignore);
        
#if UNITY_EDITOR
		m_Started = true;      
        gizmopos = inst.transform.position;
        gizmoscale = simObj.BoundingBox.GetComponent<BoxCollider>().size;
        gizmoquaternion = rotation;
#endif
        //destroy the dummy object, we don't need it anymore
        Destroy(placeholderPosition.gameObject);

		if (hitColliders.Length > 0)
		{

#if UNITY_EDITOR
			int i = 0;
			//Check when there is a new collider coming into contact with the box
			while (i < hitColliders.Length)
			{
				//Output all of the collider names
				Debug.Log("Hit : " + hitColliders[i].transform.root.name + i);
				//Increase the number of Colliders in the array
				i++;
			}
#endif

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
    }
#endif

}
