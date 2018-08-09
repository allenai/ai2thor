using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MassComparisonObjectSpawner : MonoBehaviour 
{
	public SimObjPhysics[] ObjectsToSpawn;
	public GameObject SpawnPosition;
	///private List<SimObjPhysics> ObjectsToSpawn_List;

	// Use this for initialization
	void Start () 
	{
		//ObjectsToSpawn_List = new List<SimObjPhysics>(ObjectsToSpawn);
	}

	// Update is called once per frame
	void Update () 
	{
		if(Input.GetKeyDown(KeyCode.Alpha7))
		{
			SpawnObject("bread");
		}

		if (Input.GetKeyDown(KeyCode.Alpha2))
        {
			SpawnObject("tomato");

        }

		if (Input.GetKeyDown(KeyCode.Alpha3))
        {
			SpawnObject("egg");

        }

		if (Input.GetKeyDown(KeyCode.Alpha4))
        {
			SpawnObject("potato");

        }
        
		if (Input.GetKeyDown(KeyCode.Alpha5))
        {
			SpawnObject("lettuce");

        }

		if (Input.GetKeyDown(KeyCode.Alpha6))
        {
			SpawnObject("apple");

        }
	}

	public void SpawnObject(string whichobject)
	{
		switch(whichobject)
		{
			case "bread":
				Instantiate(ObjectsToSpawn[1], SpawnPosition.transform.position,
				            Quaternion.Euler(Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f)));
				break;

			case "tomato":
				Instantiate(ObjectsToSpawn[5], SpawnPosition.transform.position,
                            Quaternion.Euler(Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f)));
				break;

			case "egg":
				Instantiate(ObjectsToSpawn[2], SpawnPosition.transform.position,
                            Quaternion.Euler(Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f)));
				break;
                
			case "potato":
				Instantiate(ObjectsToSpawn[4], SpawnPosition.transform.position,
                            Quaternion.Euler(Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f)));
				break;
                
			case "lettuce":
				Instantiate(ObjectsToSpawn[3], SpawnPosition.transform.position,
                            Quaternion.Euler(Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f)));
				break;

			case "apple":
				Instantiate(ObjectsToSpawn[0], SpawnPosition.transform.position,
                            Quaternion.Euler(Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f), Random.Range(0.0f, 360.0f)));
				break;
                            
		}
	}
}
