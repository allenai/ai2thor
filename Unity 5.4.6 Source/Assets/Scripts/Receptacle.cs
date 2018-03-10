// Copyright Allen Institute for Artificial Intelligence 2017
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SimObj))]
[ExecuteInEditMode]
public class Receptacle : MonoBehaviour {
	public Collider VisibilityCollider;
	public Transform [] Pivots;
	public GameObject MessItem;
	public bool IsClean {
		get {
			if (MessItem != null) {
				return !MessItem.activeSelf;
			}
			return true;
		} set {
			if (MessItem != null) {
				MessItem.SetActive (!value);
			}
		}
	}

	private SimObj[] startupItems;
	private Vector3 lastPosition;

	protected virtual void OnEnable() {
		if (VisibilityCollider == null) {
			Debug.LogError ("Visibility collider is not set on receptacle " + name + " - this should not happen");
			return;
		}
		//this is guaranteed to run before sim objs
		//set the visibility collider to receptacle
		//leave other colliders alone
		VisibilityCollider.tag = SimUtil.ReceptacleTag;

		if (Application.isPlaying) {
			//un-parent all parented sim objs temporarily
			//this way any sim objs we're holding can grab their colliders
			startupItems = new SimObj[Pivots.Length];
			for (int i = 0; i < Pivots.Length; i++) {
				if (Pivots [i].childCount > 0) {
					startupItems [i] = Pivots [i].GetChild (0).GetComponent<SimObj> ();
					if (startupItems [i] == null) {
						Debug.LogError ("Found a non-SimObj child in a receptacle " + name + " pivot - this should not happen");
					} else {
						startupItems [i].transform.parent = null;
					}
				}
			}

			//see if we have more than 1 collider - if we do, make the visibility collider a trigger
			Collider[] colliders = gameObject.GetComponentsInChildren<Collider>();
			if (colliders.Length > 1) {
				VisibilityCollider.isTrigger = true;
			}
		}
	}

	protected void Start () {
		if (Application.isPlaying) {
			//now that all sim objs have updated themselves
			//re-parent all children of pivots
			//and set any sim objs to invisible
			for (int i = 0; i < Pivots.Length; i++) {
				if (startupItems [i] != null) {
					startupItems [i].transform.parent = Pivots [i];
					startupItems [i].transform.localPosition = Vector3.zero;
					startupItems [i].transform.localRotation = Quaternion.identity;
					startupItems [i].VisibleToRaycasts = false;
					//if the item starts in a receptacle, it has no 'startup position'
					//so destroy its startup transform
					if (startupItems [i].StartupTransform != null) {
						GameObject.Destroy (startupItems [i].StartupTransform.gameObject);
					}
				}
			}
		}
	}

	void OnDrawGizmos () {
		if (Pivots != null && Pivots.Length > 0) {
			foreach (Transform pivot in Pivots) {
				Gizmos.color = Color.Lerp (Color.cyan, Color.clear, 0.5f);
				Gizmos.DrawCube (pivot.position, Vector3.one * 0.05f);
				Gizmos.color = Color.green;
				Gizmos.DrawLine (pivot.position, pivot.position + (pivot.up * 0.15f));
				Gizmos.color = Color.blue;
				Gizmos.DrawLine (pivot.position, pivot.position + (pivot.forward * 0.15f));
				Gizmos.color = Color.red;
				Gizmos.DrawLine (pivot.position, pivot.position + (pivot.right * 0.15f));
			}
		}
	}
}
