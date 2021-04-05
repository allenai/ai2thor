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

	//can use this list to filter out certain objects with properties, currently unused
	// private List<SimObjPrimaryProperty> PropertiesToIgnore = new List<SimObjPrimaryProperty>(new SimObjPrimaryProperty[] {SimObjPrimaryProperty.Wall,
	// 	SimObjPrimaryProperty.Floor, SimObjPrimaryProperty.Ceiling, SimObjPrimaryProperty.Static}); //should we ignore SimObjPrimaryProperty.Static?

	public bool occupied = false;

    private bool expanded = false;

	private PhysicsSceneManager physScene;

	private InstantiatePrefabTest iPrefabTest;

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

        // TODO MCS-267 Remove this code hack as well as the expanded variable
        SimObjPhysics simObj = this.gameObject.GetComponentInParent<SimObjPhysics>();
        BoxCollider triggerBoxCollider = this.GetComponent<BoxCollider>();
        if (!this.expanded && simObj != null && (simObj.shape.Equals("bowl") || simObj.shape.Equals("box") || simObj.shape.Equals("plate"))) {
            float multiplier = 10f / triggerBoxCollider.size.y;
            triggerBoxCollider.size = new Vector3(triggerBoxCollider.size.x, triggerBoxCollider.size.y * multiplier,
                triggerBoxCollider.size.z);
            triggerBoxCollider.center = new Vector3(triggerBoxCollider.center.x, triggerBoxCollider.center.y +
                (0.5f * triggerBoxCollider.size.y), triggerBoxCollider.center.z);
            this.expanded = true;
        }
	}
	void Start()
	{
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

	protected bool hasAncestor(GameObject child, GameObject potentialAncestor) {
		if (child == potentialAncestor) {
			return true;
		} else if (child.transform.parent != null) {
			return hasAncestor(child.transform.parent.gameObject, potentialAncestor);
		} else {
			return false;
		}
	}

	//public BoxCollider coll;

	//return a list of sim objects currently inside this trigger box as GameObjects
	public List<GameObject> CurrentlyContainedGameObjects()
	{
        List<GameObject> objs = new List<GameObject>();

		BoxCollider b = this.GetComponent<BoxCollider>();

		//debug
		//coll = b;

		//get center of this box in world space
		Vector3 worldCenter = b.transform.TransformPoint(b.center);

        //DEBUG: Check if colliders are at correct spots
        //Debug.Log(transform.parent.name + "'s collider is now at (" + b.transform.position.x + ", " + b.transform.position.y + ", " + b.transform.position.z + ").");

		//size of this receptacle box, but we need to scale by transform
		Vector3 worldHalfExtents = b.transform.TransformVector(b.transform.InverseTransformDirection(b.size * 0.5f));

		//ok now create an overlap box using these values and return all contained objects
		foreach (Collider col in Physics.OverlapBox(worldCenter, worldHalfExtents, b.transform.rotation))
		{
            //ignore triggers
            if (col.GetComponentInParent<SimObjPhysics>() && !col.isTrigger)
			{
				//grab reference to game object this collider is part of
				SimObjPhysics sop = col.GetComponentInParent<SimObjPhysics>();

                //don't add any colliders from our parent object, so things like a 
				//shelf or drawer nested inside another shelving unit or dresser sim object
				//don't contain the object they are nested inside
                if (!hasAncestor(this.transform.gameObject, sop.transform.gameObject))
				{
					//don't add repeat objects in case there were multiple
					//colliders from the same object
					if(!objs.Contains(sop.transform.gameObject))
					{
						objs.Add(sop.transform.gameObject);
					}
				}
			}
		}

		return objs;
	}

	//return if this container object is occupied by any object(s) inside the trigger collider
	//used by ObjectSpecificReceptacle, so objects like stove burners, coffee machine, etc.
	public bool isOccupied()
	{
		bool result = false;
		if(CurrentlyContainedGameObjects().Count > 0)
		result = true;

		return result;
	}

	//report back what is currently inside this receptacle as list of SimObjPhysics
	public List<SimObjPhysics> CurrentlyContainedObjects()
	{
        List<SimObjPhysics> toSimObjPhysics = new List<SimObjPhysics>();

        foreach(GameObject g in CurrentlyContainedGameObjects())
        {
            toSimObjPhysics.Add(g.GetComponent<SimObjPhysics>());
        }

     	return toSimObjPhysics;
	}

    // TODO MCS-77 We can take this out when we no longer deactivate held objects
    public void RemoveFromCurrentlyContains(SimObjPhysics sop) {
		occupied = false;
		CurrentlyContains.Remove(sop);
    }

	//report back a list of object ids of objects currently inside this receptacle
	public List<string> CurrentlyContainedObjectIDs()
	{
        List<string> ids = new List<string>();

        foreach(GameObject g in CurrentlyContainedGameObjects())
        {
            ids.Add(g.GetComponent<SimObjPhysics>().ObjectID);
        }

		return ids;
	}

	//returns a grid of points above the target receptacle
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

        // Find the true center and size (in global environment coordinates) of the receptacle trigger box collider.
        BoxCollider triggerBoxCollider = GetComponent<BoxCollider>();
        Vector3 center = gameObject.transform.TransformPoint(triggerBoxCollider.center);
        Vector3 size = this.FindReceptacleTriggerBoxSize();

        // Define the true sides (in global environment coordinates) of the box collider.
        Vector3 objectSideLeft = center + (new Vector3(size.x, 0, 0) * 0.5f);
        Vector3 objectSideRight = center - (new Vector3(size.x, 0, 0) * 0.5f);
        Vector3 objectSideTop = center + (new Vector3(0, size.y, 0) * 0.5f);
        Vector3 objectSideBottom = center - (new Vector3(0, size.y, 0) * 0.5f);
        Vector3 objectSideForward = center + (new Vector3(0, 0, size.z) * 0.5f);
        Vector3 objectSideBack = center - (new Vector3(0, 0, size.z) * 0.5f);

		Quaternion parentRotation = Quaternion.Euler(myParent.transform.eulerAngles);
		physScene = GameObject.Find("PhysicsSceneManager").GetComponent<PhysicsSceneManager>();
		//This is for setting the object being placed box collider checks to be the rotation of this receptacle
		iPrefabTest = physScene.GetComponent<InstantiatePrefabTest>();
		iPrefabTest.receptacleRotation = parentRotation;

        // Identify the direction corresponding to the global UP based on this receptacle object's current local rotation.
        // Since gravity is always global DOWN, we want this receptacle object between the spawn object and the ground.
        Vector3 objectUpVector = objectSideTop.y > objectSideBottom.y ? Vector3.up : Vector3.down;
        float xDiff = Mathf.Abs(objectSideTop.x - objectSideBottom.x);
        float yDiff = Mathf.Abs(objectSideTop.y - objectSideBottom.y);
        float zDiff = Mathf.Abs(objectSideTop.z - objectSideBottom.z);

        float verticalDistance = yDiff;
        if (xDiff > yDiff && xDiff > zDiff) {
            objectUpVector = objectSideTop.x > objectSideBottom.x ? Vector3.left : Vector3.right;
            verticalDistance = xDiff;
        }
        else if (zDiff > yDiff && zDiff > xDiff) {
            objectUpVector = objectSideTop.z > objectSideBottom.z ? Vector3.forward : Vector3.back;
            verticalDistance = zDiff;
        }

        // Position each spawn point slightly above this object in order to avoid possible
        // collisions (we assume that the spawned object will fall gently toward the ground).
        verticalDistance -= 0.05f;

		//so lets make a grid, we can parametize the gridsize value later, for now we'll adjust it here
		int gridsize = 4; //number of grid boxes we want, reduce this to SPEED THINGS UP but also GET WAY MORE INACCURATE
		float xBoundsMin = triggerBoxCollider.bounds.min.x;
		float xBoundsMax = triggerBoxCollider.bounds.max.x;
		float zBoundsMin = triggerBoxCollider.bounds.min.z;
		float zBoundsMax = triggerBoxCollider.bounds.max.z;

		float xBoundsRange = Mathf.Abs(xBoundsMax - xBoundsMin);
		float zBoundsRange = Mathf.Abs(zBoundsMax - zBoundsMin);
		float counterForGridSize = 0.1f;

		//this counteForGridSize amount can be changed but seems to be an apropriate size for increasing the number of points
		//on a varitey of receptacle sizes. Affects both x and z axis of the receptacle
		gridsize += (int) (Mathf.Ceil(((Mathf.Max(xBoundsRange,zBoundsRange) / counterForGridSize))));

		int linepoints = gridsize + 1; //number of points on the line we need to make the number of grid boxes
		float lineincrement =  1.0f / gridsize; //increment on the line to distribute the gridpoints

		//these are all the points on the grid on the top of the receptacle box in local space
		List<Vector3> gridpoints = new List<Vector3>();

		//The first if creates only one spawn point directly in the center of a receptacle for stacking receptacles
		SimObjPhysics simObj = gameObject.GetComponentInParent<SimObjPhysics>();
        if (simObj.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.Stacking)) {
			Transform renderer = simObj.GetComponent<Transform>();
			Renderer rend = renderer.transform.GetComponentInChildren<Renderer>();
			float yMaxOfMesh = rend.bounds.max.y; //for cubes
			float yMinOfRecBox = triggerBoxCollider.bounds.min.y;
			float yPlacementPosition = (simObj.shape.Contains("cube")) ? yMaxOfMesh : yMinOfRecBox;
			Vector3 centerOfReceptacleForStackingObjects = new Vector3(center.x, yPlacementPosition, center.z);
			if (NarrowDownValidSpawnPoints(centerOfReceptacleForStackingObjects)) {
				gridpoints.Add(centerOfReceptacleForStackingObjects);
			}
		
		} else {
			if (objectUpVector.Equals(Vector3.up) || objectUpVector.Equals(Vector3.down)) {
				for(int i = 0; i < linepoints; i++) {
					float x = objectSideLeft.x + ((objectSideRight.x - objectSideLeft.x) * (lineincrement * i));
					float y = objectUpVector.Equals(Vector3.up) ? objectSideTop.y : objectSideBottom.y;
					for(int j = 0; j < linepoints; j++) {
						float z = objectSideBack.z + ((objectSideForward.z - objectSideBack.z) * (lineincrement * j));
						///Rotate the point around the y-axis of the parent GameObject
						Vector3 returnVector = new Vector3(x,y,z);
						returnVector = RotateAroundPivot(returnVector, center, parentRotation);
						gridpoints.Add(returnVector);
					}
				}
			}

			else if (objectUpVector.Equals(Vector3.left) || objectUpVector.Equals(Vector3.right)) {
				for(int i = 0; i < linepoints; i++) {
					float x = objectUpVector.Equals(Vector3.left) ? objectSideLeft.x : objectSideRight.x;
					float y = objectSideBottom.y + ((objectSideTop.y - objectSideBottom.y) * (lineincrement * i));
					for(int j = 0; j < linepoints; j++) {
						float z = objectSideBack.z + ((objectSideForward.z - objectSideBack.z) * (lineincrement * j));
						Vector3 returnVector = new Vector3(x,y,z);
						returnVector = RotateAroundPivot(returnVector, center, parentRotation);
						gridpoints.Add(returnVector);
					}
				}
			}

			else if (objectUpVector.Equals(Vector3.forward) || objectUpVector.Equals(Vector3.back)) {
				for(int i = 0; i < linepoints; i++) {
					float x = objectSideLeft.x + ((objectSideRight.x - objectSideLeft.x) * (lineincrement * i));
					float z = objectUpVector.Equals(Vector3.forward) ? objectSideForward.z : objectSideBack.z;
					for(int j = 0; j < linepoints; j++) {
						float y = objectSideBottom.y + ((objectSideTop.y - objectSideBottom.y) * (lineincrement * j));
						Vector3 returnVector = new Vector3(x,y,z);
						returnVector = RotateAroundPivot(returnVector, center, parentRotation);
						gridpoints.Add(returnVector);
					}
				}
			}
		}

		foreach(Vector3 point in gridpoints)
		{
			// //quick test to see if this point on the grid is blocked by anything by raycasting down
			// //toward it

			//debug draw the gridpoints if you wanna see em
			#if UNITY_EDITOR
			Debug.DrawLine(point, point + (Vector3.down * verticalDistance), Color.red, 100f);
			#endif

			RaycastHit hit;
			if(Physics.Raycast(point, Vector3.down, out hit, verticalDistance, 1 << 8, QueryTriggerInteraction.Ignore))
			{

				//IMPORTANT NOTE: For objects like Sinks and Bathtubs where the interior simobject (SinkBasin, BathtubBasin) are children, make sure the interior Contains scripts have their 'myParent' field
				//set to the PARENT object of the sim object, not the sim object itself ie: SinkBasin's myParent = Sink
				if(hit.transform == myParent.transform)
				{
                    //print("raycast hit: " + hit.transform.name);
					if(!ReturnPointsCloseToAgent)
					{
						if(simObj.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.Stacking))
							PossibleSpawnPoints.Add(new ReceptacleSpawnPoint(point, triggerBoxCollider, this, myParent.GetComponent<SimObjPhysics>()));
						else 
							PossibleSpawnPoints.Add(new ReceptacleSpawnPoint(hit.point, triggerBoxCollider, this, myParent.GetComponent<SimObjPhysics>()));
					}

					else if(NarrowDownValidSpawnPoints(hit.point))
					{
						PossibleSpawnPoints.Add(new ReceptacleSpawnPoint(hit.point, triggerBoxCollider, this, myParent.GetComponent<SimObjPhysics>()));
					}
				}
			}

			Vector3 BottomPoint = point + (Vector3.down * verticalDistance);
			//didn't hit anything that could obstruct, so this point is good to go
			if(!ReturnPointsCloseToAgent)
			{
				if(simObj.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.Stacking))
					PossibleSpawnPoints.Add(new ReceptacleSpawnPoint(point, triggerBoxCollider, this, myParent.GetComponent<SimObjPhysics>()));
				else 
					PossibleSpawnPoints.Add(new ReceptacleSpawnPoint(BottomPoint, triggerBoxCollider, this, myParent.GetComponent<SimObjPhysics>()));	
			}

			else if(NarrowDownValidSpawnPoints(BottomPoint))
				PossibleSpawnPoints.Add(new ReceptacleSpawnPoint(BottomPoint, triggerBoxCollider, this, myParent.GetComponent<SimObjPhysics>()));
			//}
		}

		//****** */debug draw the spawn points as well
		// #if UNITY_EDITOR
		// validpointlist = PossibleSpawnPoints;
		// #endif

		//sort the possible spawn points by distance to the center of the Receptacle before returning
		PossibleSpawnPoints.Sort(delegate(ReceptacleSpawnPoint one, ReceptacleSpawnPoint two)
		{
			return Vector3.Distance(center, one.Point).CompareTo(Vector3.Distance(center, two.Point));
		});

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

    public Vector3 FindReceptacleTriggerBoxSize() {
        GameObject simObj = gameObject.GetComponentInParent<SimObjPhysics>().gameObject;
        // Sometimes the receptacle trigger box has a component between itself and the SimObjPhysics object.
        GameObject parentObject = (gameObject.transform.parent.gameObject.GetInstanceID() == simObj.GetInstanceID()) ?
            null : gameObject.transform.parent.gameObject;
        BoxCollider triggerBoxCollider = GetComponent<BoxCollider>();

        Vector3 simObjScale = simObj.transform.localScale;
        Vector3 parentObjectScale = parentObject != null ? parentObject.transform.localScale : Vector3.one;
        Vector3 triggerBoxObjectScale = gameObject.transform.localScale;

        return new Vector3(
            simObjScale.x * parentObjectScale.x * triggerBoxObjectScale.x * triggerBoxCollider.size.x,
            simObjScale.y * parentObjectScale.y * triggerBoxObjectScale.y * triggerBoxCollider.size.y,
            simObjScale.z * parentObjectScale.z * triggerBoxObjectScale.z * triggerBoxCollider.size.z
        );
    }

	//used to check if a given Vector3 is inside this receptacle box in world space
	//use this to check if a SimObjectPhysics's corners are contained within this receptacle, if not then it doesn't fit
	public bool CheckIfPointIsInsideReceptacleTriggerBox(Vector3 point)
	{
        BoxCollider triggerBoxCollider = GetComponent<BoxCollider>();
        Vector3 center = gameObject.transform.TransformPoint(triggerBoxCollider.center);
        Vector3 size = this.FindReceptacleTriggerBoxSize();

        SimObjPhysics simObj = gameObject.GetComponentInParent<SimObjPhysics>();
        if (simObj.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.Stacking)) {
            return true;
        }

        return point.x <= (center.x + (size.x * 0.5f)) && point.x >= (center.x - (size.x * 0.5f)) &&
            point.y <= (center.y + (size.y * 0.5f)) && point.y >= (center.y - (size.y * 0.5f)) &&
            point.z <= (center.z + (size.z * 0.5f)) && point.z >= (center.z - (size.z * 0.5f));
	}

	public bool CheckIfPointIsAboveReceptacleTriggerBox(Vector3 point)
	{
		BoxCollider myBox = gameObject.GetComponent<BoxCollider>();

		point = myBox.transform.InverseTransformPoint(point) - myBox.center;

		float halfX = (myBox.size.x * 0.5f);
		float BIGY = (myBox.size.y * 10.0f);
		float halfZ = (myBox.size.z * 0.5f);
		if (point.x < halfX && point.x > -halfX &&
			point.y < BIGY && point.y > -BIGY &&
			point.z < halfZ && point.z > -halfZ)
			return true;
		else
			return false;
	}

#if UNITY_EDITOR
	void OnDrawGizmos()
	{
        BoxCollider triggerBoxCollider = GetComponent<BoxCollider>();
        Vector3 center = gameObject.transform.TransformPoint(triggerBoxCollider.center);
        Vector3 size = this.FindReceptacleTriggerBoxSize();

		//these are the 8 points making up the corner of the box. If ANY parents of this object have non uniform scales,
        //these values will be off. Make sure that all parents in the heirarchy are at 1,1,1 scale and we can use these values
        //as a "valid area" for spawning objects inside of receptacles.
		Gizmos.color = Color.green;
        Gizmos.DrawCube(center + (new Vector3(size.x, -size.y, size.z) * 0.5f), new Vector3(0.01f, 0.01f, 0.01f));
		Gizmos.DrawCube(center + (new Vector3(-size.x, -size.y, size.z) * 0.5f), new Vector3(0.01f, 0.01f, 0.01f));
		Gizmos.DrawCube(center + (new Vector3(-size.x, -size.y, -size.z) * 0.5f), new Vector3(0.01f, 0.01f, 0.01f));
		Gizmos.DrawCube(center + (new Vector3(size.x, -size.y, -size.z) * 0.5f), new Vector3(0.01f, 0.01f, 0.01f));

		Gizmos.DrawCube(center + (new Vector3(size.x, size.y, size.z) * 0.5f), new Vector3(0.01f, 0.01f, 0.01f));
		Gizmos.DrawCube(center + (new Vector3(-size.x, size.y, size.z) * 0.5f), new Vector3(0.01f, 0.01f, 0.01f));
		Gizmos.DrawCube(center + (new Vector3(-size.x, size.y, -size.z) * 0.5f), new Vector3(0.01f, 0.01f, 0.01f));
		Gizmos.DrawCube(center + (new Vector3(size.x, size.y, -size.z) * 0.5f), new Vector3(0.01f, 0.01f, 0.01f));

		// foreach(Vector3 v in Corners)
		// {
		// 	Gizmos.DrawCube(v, new Vector3(0.01f, 0.01f, 0.01f));
		// }

		// Gizmos.color = Color.blue;
		// //Gizmos.DrawCube(boxCollider.ClosestPoint(GameObject.Find("FPSController").transform.position), new Vector3 (0.1f, 0.1f, 0.1f));
		
		// Gizmos.color = Color.magenta;
		// if(validpointlist.Count > 0)
		// {
		// 	foreach(Vector3 yes in validpointlist)
		// 	{
		// 		Gizmos.DrawCube(yes, new Vector3(0.01f, 0.01f, 0.01f));
		// 	}
		// }

		// if(coll != null)
		// {
		// 	Color prevColor = Gizmos.color;
		// 	Matrix4x4 prevMatrix = Gizmos.matrix;

		// 	Gizmos.color = Color.red;
		// 	//Gizmos.matrix = transform.localToWorldMatrix;
		// 	Gizmos.matrix = coll.transform.localToWorldMatrix;

		// 	Vector3 boxPosition = coll.transform.position;
		// 	//Vector3 boxPosition = coll.transform.TransformPoint(coll.center);

		// 	// convert from world position to local position 
		// 	boxPosition = transform.InverseTransformPoint(boxPosition) + coll.center;

		// 	Gizmos.DrawWireCube(boxPosition, coll.size);

		// 	// restore previous Gizmos settings
		// 	Gizmos.color = prevColor;
		// 	Gizmos.matrix = prevMatrix;
		// }

	}
    #endif

	public Vector3 RotateAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation) {
    	return rotation * (point - pivot) + pivot;
    }
}
