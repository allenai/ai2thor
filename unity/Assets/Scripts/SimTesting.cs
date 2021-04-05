// Copyright Allen Institute for Artificial Intelligence 2017
using UnityEngine;
using System.Collections;
using System;

//class for testing SimUtil functions
public class SimTesting : MonoBehaviour {

    public enum TestMethod
    {
        SpherecastAll,
        CheckVisibility,
        Raycast,
    }

    public TestMethod Method = TestMethod.CheckVisibility;

    public const int MaxHits = 100;

    public bool FoundSimObjs = false;
	public int NumItems = 0;
	public SimObj[] SimObjsInView = new SimObj[0];
	public float MaxDistance;
	public float MaxPlaceDistance;
	public float ReachDistance;
    public float SpherecastRadius;
    public Camera Cam;
    RaycastHit hit;
    //RaycastHit[] hits = new RaycastHit[MaxHits];
	public Vector3 placementPoint;
	public bool foundPlacementPoint;
	public SimObj inventoryObject;

	void Start() {
		if (inventoryObject != null) {
			inventoryObject.gameObject.SetActive (false);
		}
	}

	#if UNITY_EDITOR
	//used to show what's currently visible
	void OnGUI () {
		if (SimObjsInView != null) {
			if (SimObjsInView.Length > 10) {
				int horzIndex = -1;
				GUILayout.BeginHorizontal ();
				foreach (SimObj o in SimObjsInView) {
					horzIndex++;
					if (horzIndex >= 3) {
						GUILayout.EndHorizontal ();
						GUILayout.BeginHorizontal ();
						horzIndex = 0;
					}
					GUILayout.Button (o.ObjectID, UnityEditor.EditorStyles.miniButton, GUILayout.MaxWidth (200f));
				}
				GUILayout.EndHorizontal ();
			} else {
				foreach (SimObj o in SimObjsInView) {
					GUILayout.Button (o.ObjectID, UnityEditor.EditorStyles.miniButton, GUILayout.MinWidth (100f));
				}
			}
		}
	}
	#endif

	#if UNITY_EDITOR
	void OnDisable() {
		//make all sim objs invisible
		SimObj[] simObjs = GameObject.FindObjectsOfType<SimObj> ();
		foreach (SimObj o in simObjs) {
			o.VisibleNow = false;
		}
	}
	#endif

    void Update() {

		//check for a navmesh hit
		foundPlacementPoint = PlacementManager.GetPlacementPoint (transform.position, Cam.transform.forward, Cam, ReachDistance, MaxPlaceDistance, ref placementPoint);

		if (inventoryObject != null && Input.GetKeyDown (KeyCode.P)) {
			if (inventoryObject.gameObject.activeSelf) {
				SimUtil.TakeItem (inventoryObject);
			} else if (foundPlacementPoint) {
				PlacementManager.PlaceObjectAtPoint (inventoryObject, placementPoint);
			}
		}

		switch (Method) {
		case TestMethod.CheckVisibility:
		default:
			SimObjsInView = SimUtil.GetAllVisibleSimObjs (Cam, MaxDistance);
			FoundSimObjs = SimObjsInView.Length > 0;
			NumItems = SimObjsInView.Length;
			break;
		}

		
        //resize the array to avoid confusion in the test
		if (SimObjsInView.Length != NumItems) {
			Array.Resize<SimObj> (ref SimObjsInView, NumItems);
		}
    }

	void OnDrawGizmos () {
		Gizmos.color = Color.cyan;
		Gizmos.DrawSphere (Cam.transform.position, 0.1f);
        Gizmos.color = Color.grey;
		Gizmos.DrawWireSphere(Cam.transform.position, SpherecastRadius);
		Gizmos.DrawWireSphere(Cam.transform.position + (Cam.transform.forward * MaxDistance), SpherecastRadius);
		Gizmos.DrawLine(Cam.transform.position, Cam.transform.position + (Cam.transform.forward * MaxDistance));

		Gizmos.color = foundPlacementPoint ? Color.green : Color.gray;
		Gizmos.DrawSphere (transform.position + (Cam.transform.forward * ReachDistance), 0.05f);
		if (foundPlacementPoint) {
			Gizmos.DrawLine (transform.position + (Cam.transform.forward * ReachDistance), placementPoint);
			Gizmos.DrawWireCube (placementPoint, Vector3.one * 0.05f);
		}
	}
}
