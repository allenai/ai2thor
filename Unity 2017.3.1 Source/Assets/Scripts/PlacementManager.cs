// Copyright Allen Institute for Artificial Intelligence 2017
using UnityEngine;
using System.Collections;

public class PlacementManager : MonoBehaviour {

	public static PlacementManager Current {
		get {
			if (current == null) {
				current = GameObject.FindObjectOfType<PlacementManager> ();
			}
			return current;
		}
	}
	private static PlacementManager current;

	public const int NavmeshFloorArea = 0;
	public const int NavemeshNoneArea = 1;
	public const int NavmeshShelfArea = 3;
	public const float DefaultDropDistance = 0.05f;
	public const float MaxRaycastCheckDistance = 0.25f;

	public static bool GetPlacementPoint (Vector3 origin, Vector3 direction, Camera agentCamera, float reach, float maxDistance, ref Vector3 point) {
		UnityEngine.AI.NavMeshHit hit;
		if (UnityEngine.AI.NavMesh.SamplePosition (origin + (direction.normalized * reach), out hit, maxDistance, 1 << NavmeshShelfArea)) {
			//check whether we can see this point
			Vector3 viewPoint = agentCamera.WorldToViewportPoint(hit.position);
			Vector3 pointDirection = Vector3.zero;
			Vector3 agentCameraPos = agentCamera.transform.position;
			if (viewPoint.z > 0//in front of camera
				&& viewPoint.x < SimUtil.ViewPointRangeHigh && viewPoint.x > SimUtil.ViewPointRangeLow//within x bounds
				&& viewPoint.y < SimUtil.ViewPointRangeHigh && viewPoint.y > SimUtil.ViewPointRangeLow) { //within y bounds
				//do a raycast in the direction of the item
				pointDirection = (hit.position - agentCameraPos).normalized;
				RaycastHit pointHit;
				if (Physics.Raycast (
					    agentCameraPos,
					    pointDirection,
						out pointHit,
					    maxDistance * 2,
					    SimUtil.RaycastVisibleLayerMask,
						QueryTriggerInteraction.Ignore)) {
					//if it's within reasonable distance of the original point, we'll know we're fine
					if (Vector3.Distance (pointHit.point, hit.position) < MaxRaycastCheckDistance) {
						point = hit.position;
						return true;
					}
				}
			}
		}
		return false;
	}

	public static void PlaceObjectAtPoint (SimObj simObj, Vector3 point) {
		simObj.transform.position = point + Vector3.up * DefaultDropDistance;
		simObj.gameObject.SetActive (true);
		Current.StartCoroutine (current.EnableSimObjPhysics(simObj));
	}

	public IEnumerator EnableSimObjPhysics (SimObj simObj) {
		//always wait for 1 frame to allow sim object to wake itself up
		yield return null;
		//move the simObj to the object root to ensure it's not parented under another rigidbody
		simObj.transform.parent = SceneManager.Current.ObjectsParent;
		//make the object non-kinematic
		simObj.GetComponent <Rigidbody> ().isKinematic = false;
		//pray it doesn't explode
	}
}
