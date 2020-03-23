using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyingDrone : MonoBehaviour 
{

	[SerializeField] GameObject basket;
	[SerializeField] GameObject basketTrigger;

	[SerializeField] DroneObjectLauncher DroneObjectLauncher;
	
	public List<SimObjPhysics> caught_object = new List<SimObjPhysics>();

	// Use this for initialization
	void Start () 
	{
		
	}
	
	// Update is called once per frame
	void Update () 
	{
		
	}

	public bool HasLaunch(SimObjPhysics obj)
    {   
        return DroneObjectLauncher.HasLaunch(obj);
    }

	public bool isObjectCaught(SimObjPhysics check_obj)
    {
        bool caught_object_bool = false;
        foreach (SimObjPhysics obj in caught_object)
        {
            if(obj.Type == check_obj.Type)
            {
                if(obj.name == check_obj.name)
                {
                    caught_object_bool = true;
                    //Debug.Log("catch!!!");
                    break;
                }
            }
        }
        return caught_object_bool;
    }

	public void Launch(ServerAction action)
	{
		Vector3 LaunchAngle = new Vector3(action.x, action.y, action.z);
		DroneObjectLauncher.Launch(action.moveMagnitude, LaunchAngle, action.objectName, action.objectRandom);
	}

	public void MoveLauncher(Vector3 position)
    {
        DroneObjectLauncher.transform.position = position;
    }

	public Vector3 GetLauncherPosition()
    {
        return DroneObjectLauncher.transform.position;
    }

    public void SpawnLauncher(Vector3 position)
    {
        Instantiate(DroneObjectLauncher, position, Quaternion.identity);
    }

}
