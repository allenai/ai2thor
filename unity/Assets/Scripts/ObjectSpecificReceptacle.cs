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

	public bool isFull()
	{
		SimObjPhysics sop = gameObject.GetComponent<SimObjPhysics>();

		foreach (GameObject rtb in sop.ReceptacleTriggerBoxes)
		{
			if(rtb.GetComponent<Contains>().isOccupied())
			{
				full = true;
				return true;
			}
		}

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
	
}
