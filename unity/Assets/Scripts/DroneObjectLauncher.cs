using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DroneObjectLauncher : MonoBehaviour 
{
	[SerializeField] GameObject[] prefabsToLaunch;

    //keep track of what objects were launched already
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

    public GameObject GetGameObject(string objectType, bool randomize, int variation)
    {   
        List<GameObject> candidates = new List<GameObject>();

        SimObjType target = (SimObjType)Enum.Parse(typeof(SimObjType), objectType);
        //Debug.Log(target);
        foreach (GameObject go in prefabsToLaunch)
        {   
            //Debug.Log(go.GetComponent<SimObjPhysics>().Type);
            //does a prefab of objectType exist in the current array of prefabs to spawn?
            if (go.GetComponent<SimObjPhysics>().Type == target)
            {   
                candidates.Add(go);
            }
        }

        // Figure out which variation to use, if no variation use first candidate found
        if (randomize)
        {
                variation = UnityEngine.Random.Range(1, candidates.Count);
        }
        if (variation != 0) {
                variation -= 1;
        }

        return candidates[variation];
    }

	public void Launch(float magnitude, Vector3 direction, string objectName, bool randomize)
    {

        GameObject toLaunch = GetGameObject(objectName, randomize, 0);
        GameObject fireaway = Instantiate(toLaunch, this.transform.position, this.transform.rotation);
        
        GameObject topObject = GameObject.Find("Objects");
        fireaway.transform.SetParent(topObject.transform);
        fireaway.transform.position = this.transform.position;
        fireaway.transform.rotation = this.transform.rotation;
        Rigidbody rb = fireaway.GetComponent<Rigidbody>();

        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.isKinematic = false;
        rb.AddForce(direction * magnitude);

        launch_object.Add(fireaway.GetComponent<SimObjPhysics>());
    }
}
