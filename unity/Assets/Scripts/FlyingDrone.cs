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
		Vector3 LaunchAngle = new Vector3(action.x, action.y, action.z);
		DroneObjectLauncher.Launch(action.moveMagnitude, LaunchAngle);
	}

	public bool DidICatchTheThing(ServerAction action)
	{
		Debug.Log("Did The Drone catch something?- " + caught);
		return isCaught();
	}
}
