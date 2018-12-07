using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//we need to grab the FPSController for some checks
using UnityStandardAssets.Characters.FirstPerson;
public class Contains : MonoBehaviour
{
	//used to not add the door/drawer/etc of the object itself to the list of currently contained objects
	//XXX Can probably delete this, the new PropertiesToIgnore should catch these edge cases, leaving here for now just in case
	//[SerializeField] protected List<GameObject> MyObjects = null; //use this for any colliders that should be ignored (each cabinet should ignore it's own door, etc)

	[SerializeField] protected List<SimObjPhysics> CurrentlyContains = new List<SimObjPhysics>();

    //this is an object reference to the sim object that is linked to this receptacle box
	public GameObject myParent = null;

	//if the sim object is one of these properties, do not add it to the Currently Contains list.
	private List<SimObjPrimaryProperty> PropertiesToIgnore = new List<SimObjPrimaryProperty>(new SimObjPrimaryProperty[] {SimObjPrimaryProperty.Wall,
		SimObjPrimaryProperty.Floor, SimObjPrimaryProperty.Ceiling, SimObjPrimaryProperty.Moveable}); //should we ignore SimObjPrimaryProperty.Static?

	public Vector3[] gridVisual = new Vector3[0];
	public List<Vector3> validpointlist = new List<Vector3>();
	// Use this for initialization
	void Start()
	{
		//XXX debug for setting up scenes, delete or comment out when done setting up scenes
		//if(MyObjects == null)
		//{
		//	Debug.Log(this.name + " Missing MyObjects List");
		//}

		//check that all objects with receptacles components have the correct Receptacle secondary property
		#if UNITY_EDITOR
		SimObjPhysics go = gameObject.GetComponentInParent<SimObjPhysics>();
		if(!go.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.Receptacle))
		{
			Debug.LogError(go.transform.name + " is missing ReceptacleTriggerBoxes please hook them up");
		}
		#endif

		//if the parent of this object has a SimObjPhysics component, grab a reference to it
		if(gameObject.GetComponentInParent<SimObjPhysics>().transform.gameObject)
		myParent = gameObject.GetComponentInParent<SimObjPhysics>().transform.gameObject;

	}

	// Update is called once per frame
	void Update()
	{
		GetValidSpawnPoints();
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

			if(sop.transform == gameObject.GetComponentInParent<SimObjPhysics>().transform)
			{
				//don't add myself
				return;
			}

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

	public List<Vector3> GetValidSpawnPoints()
	{
		List<Vector3> PossibleSpawnPoints = new List<Vector3>();

		Vector3 p1, p2, p3, p4, p5, p6, p7, p8;

		BoxCollider b = GetComponent<BoxCollider>();

		//get all the corners of the box and convert to world coordinates
		//top forward right
		p1 = transform.TransformPoint(b.center + new Vector3(b.size.x, b.size.y, b.size.z) * 0.5f);
		//top forward left
		p2 = transform.TransformPoint(b.center + new Vector3(-b.size.x, b.size.y, b.size.z) * 0.5f);
		//top back left
		p3 = transform.TransformPoint(b.center + new Vector3(-b.size.x, b.size.y, -b.size.z) * 0.5f);
		//top back right
		p4 = transform.TransformPoint(b.center + new Vector3(b.size.x, b.size.y, -b.size.z) * 0.5f);

		//bottom forward right
		p5 = transform.TransformPoint(b.center + new Vector3(b.size.x, -b.size.y, b.size.z) * 0.5f);
		//bottom forward left
		p6 = transform.TransformPoint(b.center + new Vector3(-b.size.x, -b.size.y, b.size.z) * 0.5f);
		//bottom back left
		p7 = transform.TransformPoint(b.center + new Vector3(-b.size.x, -b.size.y, -b.size.z) * 0.5f);
		//bottom back right
		p8 = transform.TransformPoint(b.center + new Vector3(b.size.x, -b.size.y, -b.size.z) * 0.5f);

		//divide it up into a grid, put all grid points in a list i guess
		//when t = 0, we are at first point, when t = 1, we are at second point
		//so we need to figure out how to dividke up t to get the grid we want

		//so first divide up the distance from p1 to p2 into lets say 5 sections, so t increments by 0.2
		int gridsize = 5; //number of grid boxes we want
		int linepoints = gridsize + 1; //number of points on the line we need to make the number of grid boxes
		float lineincrement =  1.0f / gridsize; //increment on the line to distribute the gridpoints

		Vector3[] PointsOnLineXdir = new Vector3[linepoints];

		List<Vector3> gridpoints = new List<Vector3>();

		Vector3 zdir = (p4 - p1).normalized; //direction in the -z direction to finish drawing grid
		Vector3 xdir = (p2 - p1).normalized; //direction in the +x direction to finish drawing grid
		Vector3 ydir = (p1 - p5).normalized;
		float xdist = Vector3.Distance(p2, p1);
		float zdist = Vector3.Distance(p4, p1);
		float ydist = Vector3.Distance(p1, p5);

		for(int i = 0; i < linepoints; i++)
		{
			float x = p1.x + (p2.x - p1.x) * (lineincrement * i);
			float y = p1.y + (p2.y - p1.y) * (lineincrement * i);
			float z = p1.z + (p2.z - p1.z) * (lineincrement * i);

			PointsOnLineXdir[i] = new Vector3 (x, y, z);

			for(int j = 0; j < linepoints; j++)
			{
				gridpoints.Add(PointsOnLineXdir[i] + zdir * (zdist * (j*lineincrement)));
			}
		}
		
		//****** */debug draw the grid points as gizmos
		gridVisual = gridpoints.ToArray();

		foreach(Vector3 point in gridpoints)
		{
			//debug draw the gridpoints if you wanna see em
			// #if UNITY_EDITOR
			// Debug.DrawLine(point, point + -(ydir * ydist), Color.red, 100f);
			// #endif

			//quick test to see if this point on the grid is blocked by anything
			RaycastHit hit;
			if(Physics.Raycast(point, -ydir, out hit, ydist, 1 << 8, QueryTriggerInteraction.Ignore))
			{
				//if this hits anything except the parent object, this spot is blocked by something
				if(hit.transform == myParent.transform)
				{
					if(NarrowDownValidSpawnPoints(hit.point))
					PossibleSpawnPoints.Add(hit.point);
				}
			}
			
			//didn't hit anything that could obstruct, so this point is good to go
			//do additional checks here tos ee if the point is valid
			else
			{		
				if(NarrowDownValidSpawnPoints(point + -(ydir * ydist)))
				PossibleSpawnPoints.Add(point + -(ydir * ydist));
			}
		}

		//****** */debug draw the spawn points as well
		validpointlist = PossibleSpawnPoints;

		GameObject agent = GameObject.Find("FPSController");

		//sort the possible spawn points by distance to the Agent before returning
		PossibleSpawnPoints.Sort(delegate(Vector3 one, Vector3 two)
		{
			return Vector3.Distance(agent.transform.position, one).CompareTo(Vector3.Distance(agent.transform.position, two));
		});

		return PossibleSpawnPoints;
	}

	//additional checks if the point is valid. Return true if it's valid
	public bool NarrowDownValidSpawnPoints(Vector3 point)
	{
		//check if the point is in range of the agent at all
		GameObject agent = GameObject.Find("FPSController");
		PhysicsRemoteFPSAgentController agentController = agent.GetComponent<PhysicsRemoteFPSAgentController>();
		float maxvisdist = agentController.WhatIsAgentsMaxVisibleDistance();

		if(Vector3.Distance(point, agent.transform.position) >= maxvisdist)
		return false;

		//ok cool, it's within distance to the agent, now let's check 
		//if the point is within the viewport of the agent as well
		if(agentController.CheckIfPointIsInViewport(point))
		return true;

		return false;
	}

	//used to check if a given Vector3 is inside this receptacle box in world space
	//use this to check if a SimObjectPhysics's bottom four corners are contained within this receptacle, if not then it doesn't fit
	public bool CheckIfPointIsInsideReceptacleTriggerBox(Vector3 point)
	{
		BoxCollider myBox = gameObject.GetComponent<BoxCollider>();

		point = myBox.transform.InverseTransformPoint(point) - myBox.center;

		float halfX = (myBox.size.x * 0.5f);
        float halfY = (myBox.size.y * 0.5f);
        float halfZ = (myBox.size.z * 0.5f);
        if( point.x < halfX && point.x > -halfX && 
            point.y < halfY && point.y > -halfY && 
            point.z < halfZ && point.z > -halfZ )
            return true;
        else
            return false;	
	}

    #if UNITY_EDITOR
	void OnDrawGizmos()
	{
		BoxCollider b = GetComponent<BoxCollider>();
        
		//these are the 8 points making up the corner of the box. If ANY parents of this object have non uniform scales,
        //these values will be off. Make sure that all parents in the heirarchy are at 1,1,1 scale and we can use these values
        //as a "valid area" for spawning objects inside of receptacles.
		Gizmos.color = Color.green;
        Gizmos.DrawCube(transform.TransformPoint(b.center + new Vector3(b.size.x, -b.size.y, b.size.z) * 0.5f), new Vector3(0.01f, 0.01f, 0.01f));
		Gizmos.DrawCube(transform.TransformPoint(b.center + new Vector3(-b.size.x, -b.size.y, b.size.z) * 0.5f), new Vector3(0.01f, 0.01f, 0.01f));
		Gizmos.DrawCube(transform.TransformPoint(b.center + new Vector3(-b.size.x, -b.size.y, -b.size.z) * 0.5f), new Vector3(0.01f, 0.01f, 0.01f));
		Gizmos.DrawCube(transform.TransformPoint(b.center + new Vector3(b.size.x, -b.size.y, -b.size.z) * 0.5f), new Vector3(0.01f, 0.01f, 0.01f));

		Gizmos.DrawCube(transform.TransformPoint(b.center + new Vector3(b.size.x, b.size.y, b.size.z) * 0.5f), new Vector3(0.01f, 0.01f, 0.01f));
		Gizmos.DrawCube(transform.TransformPoint(b.center + new Vector3(-b.size.x, b.size.y, b.size.z) * 0.5f), new Vector3(0.01f, 0.01f, 0.01f));
		Gizmos.DrawCube(transform.TransformPoint(b.center + new Vector3(-b.size.x, b.size.y, -b.size.z) * 0.5f), new Vector3(0.01f, 0.01f, 0.01f));
		Gizmos.DrawCube(transform.TransformPoint(b.center + new Vector3(b.size.x, b.size.y, -b.size.z) * 0.5f), new Vector3(0.01f, 0.01f, 0.01f));

		// Gizmos.color = Color.blue;
		// //Gizmos.DrawCube(b.ClosestPoint(GameObject.Find("FPSController").transform.position), new Vector3 (0.1f, 0.1f, 0.1f));

		// if(gridVisual.Length > 0)
		// {
		// 	foreach (Vector3 yes in gridVisual)
		// 	{
		// 		Gizmos.DrawCube(yes, new Vector3(0.01f, 0.01f, 0.01f));
		// 	}
		// }

		Gizmos.color = Color.magenta;
		if(validpointlist.Count > 0)
		{
			foreach(Vector3 yes in validpointlist)
			{
				Gizmos.DrawCube(yes, new Vector3(0.01f, 0.01f, 0.01f));
			}
		}

	}
    #endif
}
