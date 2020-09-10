using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectSpecificReceptacle : MonoBehaviour 
{
	[Header("Only objects of these Types can be placed on this Receptacle")]
	public SimObjType[] SpecificTypes;

	[Header("Point where specified object(s) attach to this Receptacle")]
	public Transform attachPoint;

	[Header("Is this Receptacle already holding a valid object?")]
	public bool full = false;

	public bool HasSpecificType(SimObjType check)
	{
		bool result = false;

		foreach(SimObjType sot in SpecificTypes)
		{
			if(sot == check)
			{
				result = true;
			}
		}

		return result;
	}

	///REPLACE THIS CHECK WITH NEW CONTAINS CHECK
	public bool isFull()
	{
		print("isFull being called");
		SimObjPhysics sop = gameObject.GetComponent<SimObjPhysics>();

		foreach (GameObject rtb in sop.ReceptacleTriggerBoxes)
		{
			if(rtb.GetComponent<Contains>().isOccupied())
			{
				print("osr is occupied, return true");
				full = true;
				return true;
			}
		}

		print("osr is not occupied, return false");
		full = false;
		return false;
	}
	
	// Use this for initialization
	void Start () 
	{
		#if UNITY_EDITOR
		if(!gameObject.GetComponent<SimObjPhysics>().DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.ObjectSpecificReceptacle))
		{
			Debug.LogError(this.name + " is missing the Secondary Property ObjectSpecificReceptacle!");
		}
		#endif
	}
	
	// Update is called once per frame
	void Update () 
	{
		if(Input.GetKeyDown(KeyCode.Alpha7))
		{
			isFull();
		}
		//isFull();
	}

	void LateUpdate()
	{
		//isFull();
	}
}
