using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneObjectLauncher : MonoBehaviour 
{
	[SerializeField] GameObject prefabToLaunch;

	// public Vector3 direction;
	// public float magnitude;
	public List<SimObjPhysics> launch_object = new List<SimObjPhysics>();

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
        if (launch_object.Count > 0)
        {   
            foreach(SimObjPhysics go in launch_object)
            {   
                if (go == obj)
                {   
                    return true;
                }
            }
            return false;
        }
        else
        {   
            return false;
        }
    }

	public void Launch(float magnitude, Vector3 direction, string objectName, bool randomize)
        {

        InstantiatePrefabTest script = GameObject.Find("PhysicsSceneManager").GetComponent<InstantiatePrefabTest>();
        prefabToLaunch = script.GetGameObject(objectName, randomize, 0);
                GameObject fireaway = Instantiate(prefabToLaunch, this.transform.position, this.transform.rotation);
        GameObject topObject = GameObject.Find("Objects");
        fireaway.transform.SetParent(topObject.transform);
        fireaway.transform.position = this.transform.position;
        fireaway.transform.rotation = this.transform.rotation;
        Rigidbody rb = fireaway.GetComponent<Rigidbody>();

        //rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        //rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.isKinematic = false;
                rb.AddForce(direction * magnitude);

        launch_object.Add(fireaway.GetComponent<SimObjPhysics>());

    }
}
