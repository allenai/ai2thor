using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Contains : MonoBehaviour 
{
	//used to not add the door/drawer/etc of the object itself to the list of currently contained objects
    [SerializeField] protected GameObject MyObject = null; 

	[SerializeField] protected List<SimObjPhysics> CurrentlyContains = new List<SimObjPhysics>();
   
	// Use this for initialization
	void Start () 
	{
		
	}
	
	// Update is called once per frame
	void Update () 
	{
		
	}

	private void FixedUpdate()
    {
      
    }

	public void OnTriggerStay(Collider other)
    {
		//from the collider, see if the thing hit is a sim object physics
		if(other.GetComponentInParent<SimObjPhysics>())
		{
			SimObjPhysics sop = other.GetComponentInParent<SimObjPhysics>();

			if (!CurrentlyContains.Contains(sop) && sop.transform != MyObject.transform)
            {
				CurrentlyContains.Add(sop);
            }         
		}      
    }

	public void OnTriggerExit(Collider other)
	{
		if(other.GetComponentInParent<SimObjPhysics>())
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



}
