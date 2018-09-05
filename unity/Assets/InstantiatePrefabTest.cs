using UnityEngine;
using System.Collections;

public class InstantiatePrefabTest : MonoBehaviour {

	public GameObject[] prefabs = null;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public SimObjPhysics Spawn(string prefabType, string objectId, Vector3 position) {
		GameObject topObject = GameObject.Find("Objects");
		foreach(GameObject prefab in prefabs) {
			if (prefab.name.Contains(prefabType)) {
				GameObject go = Instantiate(prefab, position, Quaternion.identity) as GameObject;
				go.transform.SetParent(topObject.transform);
				SimObjPhysics so = go.GetComponentInChildren<SimObjPhysics>();
				if (so == null) {
					go.AddComponent<SimObjPhysics>();
					so = go.GetComponentInChildren<SimObjPhysics>();
				}
				so.UniqueID = objectId;
				return so;
			}
		}
		return null;
	}
}
