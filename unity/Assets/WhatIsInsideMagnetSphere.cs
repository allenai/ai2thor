using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WhatIsInsideMagnetSphere : MonoBehaviour
{

	[SerializeField] protected List<string> CurrentlyContainedObjectIds = new List<string>();
	[SerializeField] protected List<SimObjPhysics> CurrentlyContainedSOP = new List<SimObjPhysics>();

	private List<SimObjPrimaryProperty> PropertiesToIgnore = new List<SimObjPrimaryProperty>(new SimObjPrimaryProperty[] {SimObjPrimaryProperty.Wall,
		SimObjPrimaryProperty.Floor, SimObjPrimaryProperty.Ceiling, SimObjPrimaryProperty.Static}); //should we ignore SimObjPrimaryProperty.Static?

	//check if the sphere is actively colliding with anything
	public bool isColliding;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void FixedUpdate()
    {
        //this is a bit jank but you know, reset objects inside sphere since onTriggerEnd don't work
        CurrentlyContainedObjectIds.Clear();
		CurrentlyContainedSOP.Clear();
		isColliding = false;
    }

	public void OnTriggerStay(Collider other)
	{
		//from the collider, see if the thing hit is a sim object physics
		//don't detect other trigger colliders to prevent nested objects from containing each other
		if (other.GetComponentInParent<SimObjPhysics>() && !other.isTrigger)
		{
			isColliding = true;
			
			SimObjPhysics sop = other.GetComponentInParent<SimObjPhysics>();

			//ignore any sim objects that shouldn't be added to the CurrentlyContains list
			if (PropertiesToIgnore.Contains(sop.PrimaryProperty))
			{
				return;
			}

			//populate list of sim objects inside sphere by objectID
			if (!CurrentlyContainedObjectIds.Contains(sop.objectID))
			{
				if(sop.PrimaryProperty == SimObjPrimaryProperty.CanPickup)
				CurrentlyContainedObjectIds.Add(sop.objectID);
			}

			//populate list of sim objects inside sphere by object reference
			if(!CurrentlyContainedSOP.Contains(sop))
			{
				if(sop.PrimaryProperty == SimObjPrimaryProperty.CanPickup)
				CurrentlyContainedSOP.Add(sop);
			}
		}
	}

	//report back what is currently inside this receptacle as a list of sim object references
	public List<SimObjPhysics> CurrentlyContainedSimObjects()
	{

        //do a sphere cast to grab all sim objects 
        //Collider[] hitColliders = Physics.OverlapSphere()

        // List<string> cleanedList = new List<string>(CurrentlyContains);

        // foreach(SimObjPhysics sop in CurrentlyContains)
        // {
		// 	//I don't remember why sliced objects were being filtered out?
		// 	//why is this here please help
        //     // if(sop.GetComponent<SliceObject>())
        //     // {
        //     //     if(sop.GetComponent<SliceObject>().IsSliced())
        //     //     cleanedList.Remove(sop);
        //     // }
        // }

        // CurrentlyContains = cleanedList;
		return CurrentlyContainedSOP;
	}

	//report back what is currently inside this receptacle as a list of object ids of sim objects
	public List<string> CurrentlyContainedSimObjectsByID()
	{
		return CurrentlyContainedObjectIds;
	}

}
