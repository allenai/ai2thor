using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Contains : MonoBehaviour
{
	//used to not add the door/drawer/etc of the object itself to the list of currently contained objects
	//XXX Can probably delete this, the new PropertiesToIgnore should catch these edge cases, leaving here for now just in case
	//[SerializeField] protected List<GameObject> MyObjects = null; //use this for any colliders that should be ignored (each cabinet should ignore it's own door, etc)

	[SerializeField] protected List<SimObjPhysics> CurrentlyContains = new List<SimObjPhysics>();

    //this is an object reference to the sim object that is linked to this receptacle box
	//public GameObject myObject = null;

	//if the sim object is one of these properties, do not add it to the Currently Contains list.
	private List<SimObjPrimaryProperty> PropertiesToIgnore = new List<SimObjPrimaryProperty>(new SimObjPrimaryProperty[] {SimObjPrimaryProperty.Wall,
		SimObjPrimaryProperty.Floor, SimObjPrimaryProperty.Static, SimObjPrimaryProperty.Ceiling, SimObjPrimaryProperty.Moveable});

	// Use this for initialization
	void Start()
	{
		//XXX debug for setting up scenes, delete or comment out when done setting up scenes
		//if(MyObjects == null)
		//{
		//	Debug.Log(this.name + " Missing MyObjects List");
		//}
	}

	// Update is called once per frame
	void Update()
	{

	}

	private void FixedUpdate()
	{

	}

	public void OnTriggerStay(Collider other)
	{
		//from the collider, see if the thing hit is a sim object physics
		//don't detect other trigger colliders to prevent nested objects from containing each other
		if (other.GetComponentInParent<SimObjPhysics>() && !other.isTrigger)
		{
			SimObjPhysics sop = other.GetComponentInParent<SimObjPhysics>();

			//ignore any sim objects that shouldn't be added to the CurrentlyContains list
			if (PropertiesToIgnore.Contains(sop.PrimaryProperty))
			{
				return;
			}

			//check each "other" object, see if it is currently in the CurrentlyContains list, and make sure it is NOT one of this object's doors/drawer
			if (!CurrentlyContains.Contains(sop))//&& !MyObjects.Contains(sop.transform.gameObject))
			{
				CurrentlyContains.Add(sop);
			}
		}
	}

	public void OnTriggerExit(Collider other)
	{
		//remove objects if they leave the ReceptacleTriggerBox
		if (other.GetComponentInParent<SimObjPhysics>())
		{
			CurrentlyContains.Remove(other.GetComponentInParent<SimObjPhysics>());
		}
	}

	//report back what is currently inside this receptacle
	public List<SimObjPhysics> CurrentlyContainedObjects()
	{
		return CurrentlyContains;
	}

	//report back a list of unique id of objects currently inside this receptacle
	public List<string> CurrentlyContainedUniqueIDs()
	{
		List<string> ids = new List<string>();

		foreach (SimObjPhysics sop in CurrentlyContains)
		{
			ids.Add(sop.UniqueID);
		}

		return ids;
	}

	//tag - receptacle
	//tag - SimObjInvisible
	//BoxCollider, Trigger

    #if UNITY_EDITOR
	void OnDrawGizmos()
	{
		BoxCollider b = GetComponent<BoxCollider>();
        
		//these are the 8 points making up the corner of the box. If ANY parents of this object have non uniform scales,
        //these values will be off. Make sure that all parents in the heirarchy are at 1,1,1 scale and we can use these values
        //as a "valid area" for spawning objects inside of receptacles.

		//Gizmos.color = Color.green;
		//Gizmos.DrawSphere(transform.TransformPoint(b.center + new Vector3(b.size.x, -b.size.y, b.size.z) * 0.5f), 0.01f);
		//Gizmos.DrawSphere(transform.TransformPoint(b.center + new Vector3(-b.size.x, -b.size.y, b.size.z) * 0.5f), 0.01f);
		//Gizmos.DrawSphere(transform.TransformPoint(b.center + new Vector3(-b.size.x, -b.size.y, -b.size.z) * 0.5f), 0.01f);
		//Gizmos.DrawSphere(transform.TransformPoint(b.center + new Vector3(b.size.x, -b.size.y, -b.size.z) * 0.5f), 0.01f);

		//Gizmos.DrawSphere(transform.TransformPoint(b.center + new Vector3(b.size.x, b.size.y, b.size.z) * 0.5f), 0.01f);
		//Gizmos.DrawSphere(transform.TransformPoint(b.center + new Vector3(-b.size.x, b.size.y, b.size.z) * 0.5f), 0.01f);
		//Gizmos.DrawSphere(transform.TransformPoint(b.center + new Vector3(-b.size.x, b.size.y, -b.size.z) * 0.5f), 0.01f);
		//Gizmos.DrawSphere(transform.TransformPoint(b.center + new Vector3(b.size.x, b.size.y, -b.size.z) * 0.5f), 0.01f);
        
        //ok i made them cubes because they look more better as cubes
		Gizmos.color = Color.green;
        Gizmos.DrawCube(transform.TransformPoint(b.center + new Vector3(b.size.x, -b.size.y, b.size.z) * 0.5f), new Vector3(0.01f, 0.01f, 0.01f));
		Gizmos.DrawCube(transform.TransformPoint(b.center + new Vector3(-b.size.x, -b.size.y, b.size.z) * 0.5f), new Vector3(0.01f, 0.01f, 0.01f));
		Gizmos.DrawCube(transform.TransformPoint(b.center + new Vector3(-b.size.x, -b.size.y, -b.size.z) * 0.5f), new Vector3(0.01f, 0.01f, 0.01f));
		Gizmos.DrawCube(transform.TransformPoint(b.center + new Vector3(b.size.x, -b.size.y, -b.size.z) * 0.5f), new Vector3(0.01f, 0.01f, 0.01f));

		Gizmos.DrawCube(transform.TransformPoint(b.center + new Vector3(b.size.x, b.size.y, b.size.z) * 0.5f), new Vector3(0.01f, 0.01f, 0.01f));
		Gizmos.DrawCube(transform.TransformPoint(b.center + new Vector3(-b.size.x, b.size.y, b.size.z) * 0.5f), new Vector3(0.01f, 0.01f, 0.01f));
		Gizmos.DrawCube(transform.TransformPoint(b.center + new Vector3(-b.size.x, b.size.y, -b.size.z) * 0.5f), new Vector3(0.01f, 0.01f, 0.01f));
		Gizmos.DrawCube(transform.TransformPoint(b.center + new Vector3(b.size.x, b.size.y, -b.size.z) * 0.5f), new Vector3(0.01f, 0.01f, 0.01f));
	}
    #endif
}
