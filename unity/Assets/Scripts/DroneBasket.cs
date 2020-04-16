using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

#if UNITY_EDITOR
using UnityEditor;
#endif
public class DroneBasket : MonoBehaviour 
{
	public GameObject myParent = null;
    private PhysicsSceneManager psManager;

	[SerializeField] protected List<SimObjPhysics> CurrentlyContains = new List<SimObjPhysics>();

	// Use this for initialization
	void Start () 
	{
        psManager = GameObject.Find("PhysicsSceneManager").GetComponent<PhysicsSceneManager>();
	}
	
	// Update is called once per frame
	void Update () 
	{
		
	}

	public void OnTriggerStay(Collider other)
	{
		//from the collider, see if the thing hit is a sim object physics
		//don't detect other trigger colliders to prevent nested objects from containing each other
		if (other.GetComponentInParent<SimObjPhysics>() && !other.isTrigger)
		{
			
			SimObjPhysics sop = other.GetComponentInParent<SimObjPhysics>();

			//don't add any parent objects in case this is a child sim object
			if(sop.transform == myParent.transform)
			{
				return;
			}

			//check each "other" object, see if it is currently in the CurrentlyContains list, and make sure it is NOT one of this object's doors/drawer
			if (!CurrentlyContains.Contains(sop))//&& !MyObjects.Contains(sop.transform.gameObject))
			{
				CurrentlyContains.Add(sop);

				sop.GetComponent<Transform>().SetParent(this.transform);
				sop.transform.Find("Colliders").gameObject.SetActive(false);
				sop.transform.Find("TriggerColliders").gameObject.SetActive(false);
				sop.transform.Find("BoundingBox").gameObject.SetActive(false);

				if(sop.GetComponent<Rigidbody>())
                {
                    Rigidbody rb = sop.GetComponent<Rigidbody>();

                    psManager.RemoveFromRBSInScene(rb);
				    Destroy(rb);

                }

				sop.enabled = false;

				myParent.GetComponent<DroneFPSAgentController>().caught_object.Add(sop);
			}
		}
	}

}
