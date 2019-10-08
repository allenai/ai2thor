using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//we need to grab the FPSController for some checks
using UnityStandardAssets.Characters.FirstPerson;

//Class that holds info for a Vector3 object spawn point and the BoxCollider that the Point resides in
//this will be used for a comparison test later to make sure the spawned object is within bounds
//of the Receptacle Trigger Box, which gets confusing if an Object has multiple ReceptacleTriggerBoxes
//with multiple Contains.cs scripts
[System.Serializable]
public class ReceptacleSpawnPoint
{
	public BoxCollider ReceptacleBox; //the box the point is in
	public Vector3 Point; //Vector3 coordinate in world space, possible spawn location

	public Contains Script;
	public SimObjPhysics ParentSimObjPhys;

	public ReceptacleSpawnPoint(Vector3 p, BoxCollider box, Contains c, SimObjPhysics parentsop)
	{
		ReceptacleBox = box;
		Point = p;
		Script = c;
		ParentSimObjPhys = parentsop;
	}
}

public class Contains : MonoBehaviour
{
	[SerializeField] protected List<SimObjPhysics> CurrentlyContains = new List<SimObjPhysics>();

    //this is an object reference to the sim object that is linked to this receptacle box
	public GameObject myParent = null;

	//if the sim object is one of these properties, do not add it to the Currently Contains list.
	private List<SimObjPrimaryProperty> PropertiesToIgnore = new List<SimObjPrimaryProperty>(new SimObjPrimaryProperty[] {SimObjPrimaryProperty.Wall,
		SimObjPrimaryProperty.Floor, SimObjPrimaryProperty.Ceiling, SimObjPrimaryProperty.Static}); //should we ignore SimObjPrimaryProperty.Static?

	public bool occupied = false;

    //private List<Vector3> validpointlist = new List<Vector3>();

	//world coordinates of the Corners of this object's receptacles in case we need it for something
	//public List<Vector3> Corners = new List<Vector3>();

	// Use this for initialization

	void OnEnable()
	{
		//if the parent of this object has a SimObjPhysics component, grab a reference to it
		if(myParent == null)
		{
			if(gameObject.GetComponentInParent<SimObjPhysics>().transform.gameObject)
			myParent = gameObject.GetComponentInParent<SimObjPhysics>().transform.gameObject;
		}

	}
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
			Debug.LogError(go.transform.name + " is missing Receptacle Secondary Property! please hook them up");
		}
		#endif



	}

	// Update is called once per frame
	void Update()
	{
		//turn this on for debugging spawnpoints in editor
		//GetValidSpawnPoints(true);

		//set the bool if any objects are inside this
		// if(CurrentlyContains.Count > 0)
		// {
		// 	occupied = true;
		// }

		// else
		// {
		// 	occupied = false;
		// }
	}

	private void FixedUpdate()
	{
		//clear the currently contains list so that if things were Initial Random Spawned in, the receptacle
		//trigger boxes correctly re-populate with the current objects via OnTriggerStay. We need this here
		//because OnTriggerExit will miss correctly editing the list if objects are teleported around like with
		//the Initial Random Spawn Function! 
		//CurrentlyContains.Clear();
		//occupied = false;
	}

	public void OnTriggerEnter(Collider other)
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

			//don't add any parent objects in case this is a child sim object
			if(sop.transform == myParent.transform)
			{
				return;
			}

			//check each "other" object, see if it is currently in the CurrentlyContains list, and make sure it is NOT one of this object's doors/drawer
			if (!CurrentlyContains.Contains(sop))//&& !MyObjects.Contains(sop.transform.gameObject))
			{
				occupied = true;
				CurrentlyContains.Add(sop);
			}
		}
	}

	public void OnTriggerExit(Collider other)
	{
		//remove objects if they leave the ReceptacleTriggerBox
		if (other.GetComponentInParent<SimObjPhysics>())
		{
			SimObjPhysics sop = other.GetComponentInParent<SimObjPhysics>();

			//if the object was removed from the receptacle by anything other than the Agent picking it up
			if(!sop.transform.GetComponentInParent<PhysicsRemoteFPSAgentController>())
			{
				//make sure to only remove and unparent stuff that is actually contained - prevent errors like the SinkBasin being unparanted when a mug is removed from it
				if(CurrentlyContains.Contains(sop))
				//if(sop.Type != SimObjType.SinkBasin && sop.Type != SimObjType.BathtubBasin)
				{
					//check if initial random spawn is currently happening, if it is DO NOT DO THIS
					// GameObject topObject = GameObject.Find("Objects");
					// sop.transform.SetParent(topObject.transform);
				}

			}

			occupied = false;
			//print(other.GetComponentInParent<SimObjPhysics>().transform.name);
			CurrentlyContains.Remove(sop);
		}
	}

	//report back what is currently inside this receptacle
	public List<SimObjPhysics> CurrentlyContainedObjects()
	{
        List<SimObjPhysics> cleanedList = new List<SimObjPhysics>(CurrentlyContains);

        foreach(SimObjPhysics sop in CurrentlyContains)
        {
            if(sop.GetComponent<SliceObject>())
            {
                if(sop.GetComponent<SliceObject>().IsSliced())
                cleanedList.Remove(sop);
            }
        }

        CurrentlyContains = cleanedList;
		return CurrentlyContains;
	}

	//report back a list of object ids of objects currently inside this receptacle
	public List<string> CurrentlyContainedObjectIDs()
	{
        List<SimObjPhysics> cleanedList = new List<SimObjPhysics>(CurrentlyContains);

		List<string> ids = new List<string>();

		foreach (SimObjPhysics sop in CurrentlyContains)
		{
            if(sop.GetComponent<SliceObject>())
            {
                if(sop.GetComponent<SliceObject>().IsSliced())
                {
                    cleanedList.Remove(sop);
                }
            }
		}

        CurrentlyContains = cleanedList;

        foreach (SimObjPhysics sop in CurrentlyContains)
        {
            ids.Add(sop.ObjectID);
        }

		return ids;
	}

	//generate a grid of potential spawn points, set ReturnPointsClosestToAgent to true if
	//the list of points should be filtered closest to agent, if false
	//it will return all points on the receptacle regardless of agent proximity
	public List<Vector3> GetValidSpawnPointsFromTopOfTriggerBox()
	{
		Vector3 p1, p2, p4; //in case we need all the corners later for something...

		BoxCollider b = GetComponent<BoxCollider>();

		//get all the corners of the box and convert to world coordinates
		//top forward right
		p1 = transform.TransformPoint(b.center + new Vector3(b.size.x, b.size.y, b.size.z) * 0.5f);
		//top forward left
		p2 = transform.TransformPoint(b.center + new Vector3(-b.size.x, b.size.y, b.size.z) * 0.5f);
		//top back right
		p4 = transform.TransformPoint(b.center + new Vector3(b.size.x, b.size.y, -b.size.z) * 0.5f);

		//so lets make a grid, we can parametize the gridsize value later, for now we'll adjust it here
		int gridsize = 20; //number of grid boxes we want, reduce this to SPEED THINGS UP but also GET WAY MORE INACCURATE
		int linepoints = gridsize + 1; //number of points on the line we need to make the number of grid boxes
		float lineincrement =  1.0f / gridsize; //increment on the line to distribute the gridpoints

		Vector3[] PointsOnLineXdir = new Vector3[linepoints];

		//these are all the points on the grid on the top of the receptacle box in local space
		List<Vector3> gridpoints = new List<Vector3>();

		Vector3 zdir = (p4 - p1).normalized; //direction in the -z direction to finish drawing grid
		float zdist = Vector3.Distance(p4, p1);

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
		// //****** */debug draw the spawn points as well
		// #if UNITY_EDITOR
		// validpointlist = gridpoints;
		// #endif

        return gridpoints;
    }

	//generate a grid of potential spawn points, set ReturnPointsClosestToAgent to true if
	//the list of points should be filtered closest to agent, if false
	//it will return all points on the receptacle regardless of agent proximity
	public List<ReceptacleSpawnPoint> GetValidSpawnPoints(bool ReturnPointsCloseToAgent)
	{
		List<ReceptacleSpawnPoint> PossibleSpawnPoints = new List<ReceptacleSpawnPoint>();

		Vector3 p1, p2, /*p3,*/ p4, p5 /*p6, p7, p8*/; //in case we need all the corners later for something...

		BoxCollider b = GetComponent<BoxCollider>();

		//get all the corners of the box and convert to world coordinates
		//top forward right
		p1 = transform.TransformPoint(b.center + new Vector3(b.size.x, b.size.y, b.size.z) * 0.5f);
		//top forward left
		p2 = transform.TransformPoint(b.center + new Vector3(-b.size.x, b.size.y, b.size.z) * 0.5f);
		//top back right
		p4 = transform.TransformPoint(b.center + new Vector3(b.size.x, b.size.y, -b.size.z) * 0.5f);

		//bottom forward right
		p5 = transform.TransformPoint(b.center + new Vector3(b.size.x, -b.size.y, b.size.z) * 0.5f);

		//so lets make a grid, we can parametize the gridsize value later, for now we'll adjust it here
		int gridsize = 8; //number of grid boxes we want, reduce this to SPEED THINGS UP but also GET WAY MORE INACCURATE
		int linepoints = gridsize + 1; //number of points on the line we need to make the number of grid boxes
		float lineincrement =  1.0f / gridsize; //increment on the line to distribute the gridpoints

		Vector3[] PointsOnLineXdir = new Vector3[linepoints];

		//these are all the points on the grid on the top of the receptacle box in local space
		List<Vector3> gridpoints = new List<Vector3>();

		Vector3 zdir = (p4 - p1).normalized; //direction in the -z direction to finish drawing grid
		Vector3 xdir = (p2 - p1).normalized; //direction in the +x direction to finish drawing grid
		Vector3 ydir = (p1 - p5).normalized;
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

		foreach(Vector3 point in gridpoints)
		{
            // print("checking point in gridpoints on " + myParent.transform.name);

			// //quick test to see if this point on the grid is blocked by anything by raycasting down
			// //toward it
			RaycastHit hit;
			if(Physics.Raycast(point, -ydir, out hit, ydist, 1 << 8, QueryTriggerInteraction.Collide))//NOTE: QueryTriggerInteraction was previously Ignore
			{

				//IMPORTANT NOTE: For objects like Sinks and Bathtubs where the interior simobject (SinkBasin, BathtubBasin) are children, make sure the interior Contains scripts have their 'myParent' field
				//set to the PARENT object of the sim object, not the sim object itself ie: SinkBasin's myParent = Sink
				if(hit.transform == myParent.transform)
				{
                    //print("raycast hit: " + hit.transform.name);
					if(!ReturnPointsCloseToAgent)
					{
						PossibleSpawnPoints.Add(new ReceptacleSpawnPoint(hit.point, b, this, myParent.GetComponent<SimObjPhysics>()));
					}

					else if(NarrowDownValidSpawnPoints(hit.point))
					{
						PossibleSpawnPoints.Add(new ReceptacleSpawnPoint(hit.point, b, this, myParent.GetComponent<SimObjPhysics>()));
					}

                    // //debug draw the gridpoints if you wanna see em
                    // #if UNITY_EDITOR
                    // Debug.DrawLine(point, point + -(ydir * ydist), Color.red, 100f);
                    // #endif
				}
			}

			Vector3 BottomPoint = point + -(ydir * ydist);
			//didn't hit anything that could obstruct, so this point is good to go
			if(!ReturnPointsCloseToAgent)
			{
				PossibleSpawnPoints.Add(new ReceptacleSpawnPoint(BottomPoint, b, this, myParent.GetComponent<SimObjPhysics>()));
			}

			else if(NarrowDownValidSpawnPoints(BottomPoint))
				PossibleSpawnPoints.Add(new ReceptacleSpawnPoint(BottomPoint, b, this, myParent.GetComponent<SimObjPhysics>()));
		}

		//****** */debug draw the spawn points as well
		// #if UNITY_EDITOR
		// validpointlist = PossibleSpawnPoints;
		// #endif

		//sort the possible spawn points by distance to the Agent before returning
		// PossibleSpawnPoints.Sort(delegate(ReceptacleSpawnPoint one, ReceptacleSpawnPoint two)
		// {
		// 	return Vector3.Distance(agent.transform.position, one.Point).CompareTo(Vector3.Distance(agent.transform.position, two.Point));
		// });

		return PossibleSpawnPoints;
	}

	//additional checks if the point is valid. Return true if it's valid
	public bool NarrowDownValidSpawnPoints(Vector3 point)
	{
		//check if the point is in range of the agent at all
		GameObject agent = GameObject.Find("FPSController");
		PhysicsRemoteFPSAgentController agentController = agent.GetComponent<PhysicsRemoteFPSAgentController>();

		//get agent's camera point, get point to check, find the distance from agent camera point to point to check

		float maxvisdist = agentController.WhatIsAgentsMaxVisibleDistance();

		//set the distance so that it is within the radius maxvisdist from the agent
		Vector3 tmpForCamera =  agent.GetComponent<PhysicsRemoteFPSAgentController>().m_Camera.transform.position;
		tmpForCamera.y = point.y;

		//automatically rule out a point if it's beyond our max distance of visibility
		if(Vector3.Distance(point, tmpForCamera) >= maxvisdist)
		return false;

		//ok cool, it's within distance to the agent, now let's check 
		//if the point is within the viewport of the agent as well

		Camera agentCam = agent.GetComponent<PhysicsRemoteFPSAgentController>().m_Camera;

		//no offset if the object is below the camera position - a slight offset to account for objects equal in y distance to the camera
		if(point.y < agentCam.transform.position.y - 0.05f)
		{
			//do this check if the point's y value is below the camera's y value
			//this check will be a raycast vision check from the camera to the point exactly
			if(agentController.CheckIfPointIsInViewport(point))
			return true;
		}
		
		else
		{
			//do this check if the point's y value is above the agent camera. This means we are
			//trying to place an object on a shelf or something high up that we can't quite reach
			//in this case, modify the point that is checked for visibility by adding a little bit to the y

			//might want to adjust this offset amount, or even move this check to ensure object visibility after the
			//checkspawnarea corners are generated?
			if(agentController.CheckIfPointIsInViewport(point + new Vector3(0, 0.05f, 0)))
			return true;
		}

		return false;
		
	}

	//used to check if a given Vector3 is inside this receptacle box in world space
	//use this to check if a SimObjectPhysics's corners are contained within this receptacle, if not then it doesn't fit
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

	public bool CheckIfPointIsAboveReceptacleTriggerBox(Vector3 point)
	{
		BoxCollider myBox = gameObject.GetComponent<BoxCollider>();

		point = myBox.transform.InverseTransformPoint(point) - myBox.center;

		float halfX = (myBox.size.x * 0.5f);
        float BIGY = (myBox.size.y * 10.0f);
        float halfZ = (myBox.size.z * 0.5f);
        if( point.x < halfX && point.x > -halfX && 
            point.y < BIGY && point.y > -BIGY && 
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

		// foreach(Vector3 v in Corners)
		// {
		// 	Gizmos.DrawCube(v, new Vector3(0.01f, 0.01f, 0.01f));
		// }

		// Gizmos.color = Color.blue;
		// //Gizmos.DrawCube(b.ClosestPoint(GameObject.Find("FPSController").transform.position), new Vector3 (0.1f, 0.1f, 0.1f));
		
		// Gizmos.color = Color.magenta;
		// if(validpointlist.Count > 0)
		// {
		// 	foreach(Vector3 yes in validpointlist)
		// 	{
		// 		Gizmos.DrawCube(yes, new Vector3(0.01f, 0.01f, 0.01f));
		// 	}
		// }
	}
    #endif
}
