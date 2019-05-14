// Copyright Allen Institute for Artificial Intelligence 2017
using UnityEngine;
using System.Collections;

public class Toaster : MonoBehaviour 
{

	//private ObjectSpecificReceptacle osr;
	//private CanToggleOnOff onOff;

	void Start()
	{
		//osr = gameObject.GetComponent<ObjectSpecificReceptacle>();
		//onOff = gameObject.GetComponent<CanToggleOnOff>();
	}
	void Update()
	{
		//Note: Moved This call to the HeatZone that is turned on when the toaster is turned on

		
		// //on update.... maybe check if toaster is on? if so.... try and toast the object
		// if(osr.attachPoint.transform.GetComponentInChildren<SimObjPhysics>() && onOff.isTurnedOnOrOff())
		// {
		// 	Toast();
		// }

	}

	// public void Toast()
	// {
	// 	//check if attachpoint has a bread
	// 	//if so, use the ToastObject.Toast() function
	// 	SimObjPhysics target;
	// 	target = osr.attachPoint.transform.GetComponentInChildren<SimObjPhysics>();
	// 	CookObject toast = target.GetComponent<CookObject>();

	// 	//if not already toasted, toast it!
	// 	if(!toast.IsCooked())
	// 	{
	// 		toast.Cook();
	// 	}
	// }
	
}
