using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WhatIsInsideMagnetSphere : MonoBehaviour
{

	[SerializeField] protected List<SimObjPhysics> CurrentlyContains = new List<SimObjPhysics>();
	private List<SimObjPrimaryProperty> PropertiesToIgnore = new List<SimObjPrimaryProperty>(new SimObjPrimaryProperty[] {SimObjPrimaryProperty.Wall,
		SimObjPrimaryProperty.Floor, SimObjPrimaryProperty.Ceiling, SimObjPrimaryProperty.Static}); //should we ignore SimObjPrimaryProperty.Static?

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
        //this is a bit jank but you know
        CurrentlyContains.Clear();
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
				if(sop.PrimaryProperty == SimObjPrimaryProperty.CanPickup)
				CurrentlyContains.Add(sop);
			}
		}
	}

	//report back what is currently inside this receptacle
	public List<SimObjPhysics> CurrentlyContainedObjects()
	{

        //do a sphere cast to grab all sim objects 
        //Collider[] hitColliders = Physics.OverlapSphere()

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

}
