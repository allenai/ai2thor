// Copyright Allen Institute for Artificial Intelligence 2017
using UnityEngine;
using System.Collections;

public class Toaster : MonoBehaviour 
{

	private ObjectSpecificReceptacle osr;
	private CanToggleOnOff onOff;

	void Start()
	{
		osr = gameObject.GetComponent<ObjectSpecificReceptacle>();
		onOff = gameObject.GetComponent<CanToggleOnOff>();
	}
	void Update()
	{
		//on update.... maybe check if toaster is on? if so.... try and toast the object
		Toast();
	}

	public void Toast()
	{
		//check if attachpoint has a bread
		//if so, use the ToastObject.Toast() function
		SimObjPhysics target;

		if(osr.attachPoint.transform.GetComponentInChildren<SimObjPhysics>() && onOff.isTurnedOnOrOff())
		{
			target = osr.attachPoint.transform.GetComponentInChildren<SimObjPhysics>();
			ToastObject toast = target.GetComponent<ToastObject>();

			//if not already toasted, toast it!
			if(!toast.IsToasted())
			{
				toast.Toast();
			}
		}
	}
}
