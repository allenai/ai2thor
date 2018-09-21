using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class InstantiatePrefabTest : MonoBehaviour
{

	public GameObject[] prefabs = null;
	private int spawnCount = 0;

	private bool m_Started = false;
	Vector3 gizmopos;
	Vector3 gizmoscale;


	// Use this for initialization
	void Start()
	{
		//m_Started = true;
	}

	// Update is called once per frame
	void Update()
	{

	}

	public SimObjPhysics Spawn(string prefabType, string objectId, Vector3 position)
	{
		GameObject topObject = GameObject.Find("Objects");

		foreach (GameObject prefab in prefabs)
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
	public SimObjPhysics SpawnObject(string objectType, bool randomize, int variation, Vector3 position){
		return SpawnObject(objectType, randomize, variation, position, false);
	}

	//object type - from SimObjType which object to spawn
	//randomize - should the spawner randomly pick an object to spawn
	//variation - which specific version of the object (1, 2, 3), set to 0 if no specific variation is wanted
	//position - where spawn?
	public SimObjPhysics SpawnObject(string objectType, bool randomize, int variation, Vector3 position, bool ignoreChecks)
	{

		//print(Enum.Parse(typeof(SimObjType), objectType));

		GameObject topObject = GameObject.Find("Objects");

		List<GameObject> candidates = new List<GameObject>();

		foreach (GameObject go in prefabs)
		{
			//does a prefab of objectType exist in the current array of prefabs to spawn?
			if (go.GetComponent<SimObjPhysics>().Type == (SimObjType)Enum.Parse(typeof(SimObjType), objectType))
			{
				candidates.Add(go);
			}
		}

		//ok time to spawn a sim object!
		SimObjPhysics simObj = null;

		// Figure out which variation to use
		if (randomize)
		{
			variation = UnityEngine.Random.Range(1, candidates.Count);
		}
		
		if (variation != 0) {
			variation -= 1;
		}
		Debug.Log(variation);

		if (ignoreChecks || CheckSpawnArea(candidates[variation].GetComponent<SimObjPhysics>(), position))
		{
			GameObject prefab = Instantiate(candidates[variation], position, Quaternion.identity) as GameObject;
			prefab.transform.SetParent(topObject.transform);
			simObj = prefab.GetComponent<SimObjPhysics>();
			spawnCount++;
		}
		else {
			return null;
		}

		//ok make sure we did actually spawn something now, and give it an Id number
		if (simObj)
		{
			//gizmopos = simObj.RotateAgentCollider.transform.position + simObj.RotateAgentCollider.GetComponent<BoxCollider>().center;
			//gizmoscale = simObj.RotateAgentCollider.GetComponent<BoxCollider>().size;
			//m_Started = true;

			simObj.uniqueID = objectType + "|" + spawnCount.ToString();
			return simObj;         
		}

		return null;
	}

	//IMPORTANT INFO!//
	//The prefab MUST have a rotate agent collider with zeroed out transform, rotation, and 1, 1, 1 scale
	//All adjustments to the Rotate agent collider box must be done on the collider only using the
	//"Edit Collider" button
	//this assumes that the RotateAgentCollider transform is zeroed out according to the root transform of the prefab
	private bool CheckSpawnArea(SimObjPhysics simObj, Vector3 center)
	{
		//first do a check to see if the area is clear
		Collider[] hitColliders = Physics.OverlapBox(center + simObj.RotateAgentCollider.GetComponent<BoxCollider>().center,
					  simObj.RotateAgentCollider.GetComponent<BoxCollider>().size / 2, Quaternion.identity,
					  1 << 8, QueryTriggerInteraction.Ignore);
        
#if UNITY_EDITOR
		m_Started = true;
		gizmopos = center + simObj.RotateAgentCollider.GetComponent<BoxCollider>().center;
		gizmoscale = simObj.RotateAgentCollider.GetComponent<BoxCollider>().size;
#endif

		if (hitColliders.Length > 0)
		{

#if UNITY_EDITOR
			int i = 0;
			//Check when there is a new collider coming into contact with the box
			while (i < hitColliders.Length)
			{
				//Output all of the collider names
				Debug.Log("Hit : " + hitColliders[i].transform.root.name + i);
				//Increase the number of Colliders in the array
				i++;
			}
#endif

			return false;
		}

		return true;
	}

#if UNITY_EDITOR
	void OnDrawGizmos()
	{
		Gizmos.color = Color.red;
		if (m_Started)
			//Draw a cube where the OverlapBox is (positioned where your GameObject is as well as a size)
			Gizmos.DrawWireCube(gizmopos, gizmoscale);
	}
#endif

}
