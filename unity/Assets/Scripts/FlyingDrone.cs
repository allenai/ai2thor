using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingDrone : MonoBehaviour 
{

	[SerializeField] GameObject basket;
	[SerializeField] GameObject basketTrigger;

	[SerializeField] DroneObjectLauncher DroneObjectLauncher;
	
	public bool caught = false;


	// Use this for initialization
	void Start () 
	{
		
	}
	
	// Update is called once per frame
	void Update () 
	{
		
	}

	public bool isCaught()
	{
		return caught;
	}

	public void Launch(ServerAction action)
	{
		DroneObjectLauncher.Launch(action.moveMagnitude, action.rotation);
	}
}
