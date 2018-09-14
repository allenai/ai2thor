using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class InstantiatePrefabTest : MonoBehaviour 
{

	public GameObject[] prefabs = null;
	private int spawnCount = 0;

	// Use this for initialization
	void Start () 
	{
	
	}
	
	// Update is called once per frame
	void Update () 
	{
		//if(Input.GetKeyDown(KeyCode.L))
		//{
		//	ServerAction action = new ServerAction();
            
		//	action.objectType = "Footstool";//what type of object?
		//	action.randomizeObjectAppearance = false;//pick randomly from available or not?
		//	action.x = 0;//spawn pos x
		//	action.y = 0;//spawn pos y
		//	action.z = 0;//spawn pos z
		//	action.sequenceId = 1;//if random false, which version of the object to spawn? (there are only 3 of each type atm)
            
  //          //objectId isn't being used yet... might be for something in a moment
            
		//	SpawnObject(action.objectType, action.randomizeObjectAppearance, action.sequenceId, new Vector3(action.x, action.y, action.z));	
		//}
	}

	public SimObjPhysics Spawn(string prefabType, string objectId, Vector3 position) 
	{
		GameObject topObject = GameObject.Find("Objects");

		foreach(GameObject prefab in prefabs) 
		{
			if (prefab.name.Contains(prefabType)) 
			{
				GameObject go = Instantiate(prefab, position, Quaternion.identity) as GameObject;
				go.transform.SetParent(topObject.transform);
				SimObjPhysics so = go.GetComponentInChildren<SimObjPhysics>();
				if (so == null) 
				{
					go.AddComponent<SimObjPhysics>();
					so = go.GetComponentInChildren<SimObjPhysics>();
				}
    
				so.UniqueID = objectId;
				return so;
			}
		}

		return null;
	}

    //object type - from SimObjType which object to spawn
    //randomize - should the spawner randomly pick an object to spawn
	//variation - which specific version of the object (1, 2, 3), set to 0 if no specific variation is wanted
    //position - where spawn?
	public SimObjPhysics SpawnObject(string objectType, bool randomize, int variation, Vector3 position)
	{
		//print(Enum.Parse(typeof(SimObjType), objectType));

		GameObject topObject = GameObject.Find("Objects");

		List<GameObject> candidates = new List<GameObject>();
        
		foreach(GameObject go in prefabs)
		{
			//does a prefab of objectType exist in the current array of prefabs to spawn?
			if(go.GetComponent<SimObjPhysics>().Type == (SimObjType) Enum.Parse(typeof(SimObjType), objectType))
			{
				candidates.Add(go);
			}
		}

		//ok time to spawn a sim object!
		SimObjPhysics simObj = null;

        //randomly pick from one of the eligible candidates to spawn
		if (randomize)
        {
			int whichone = UnityEngine.Random.Range(0, candidates.Count);

			GameObject prefab = Instantiate(candidates[whichone], position, Quaternion.identity) as GameObject;
			prefab.transform.SetParent(topObject.transform);

			simObj = prefab.GetComponent<SimObjPhysics>();
			spawnCount++;
        }

		else
		{
			if(variation < 0 || variation > candidates.Count)
			{
				Debug.LogError("There aren't that many varations of " + objectType + 
				               " there are only " + candidates.Count + " variations.");
				return null;
			}

			//specify which variation you would like to spawn
			if(variation != 0)
			{
				GameObject prefab = Instantiate(candidates[variation - 1], position, Quaternion.identity) as GameObject;
                prefab.transform.SetParent(topObject.transform);

                simObj = prefab.GetComponent<SimObjPhysics>();
				spawnCount++;
			}
            
			//or spawn default 1st found prefab of the objectType
			else
			{
				GameObject prefab = Instantiate(candidates[variation], position, Quaternion.identity) as GameObject;
                prefab.transform.SetParent(topObject.transform);

                simObj = prefab.GetComponent<SimObjPhysics>();
				spawnCount++;
			}
		}

        //ok make sure we did actually spawn something now, and give it an Id number
		if(simObj)
		{
			simObj.uniqueID = objectType + "|" + spawnCount.ToString();
			return simObj;
		}


        

		return null;
	}
}
