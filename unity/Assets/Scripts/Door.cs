// Copyright Allen Institute for Artificial Intelligence 2017
using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class Door : MonoBehaviour {

	public SimObj ParentObj;
	public Vector3 OpenRotation;
	public Vector3 ClosedRotation;
	public Transform Pivot;
	public bool EditorOpen;

	void OnEnable () {
		ParentObj = gameObject.GetComponent <SimObj> ();
		if (ParentObj == null) {
			ParentObj = gameObject.AddComponent <SimObj> ();
		}

		if (!Application.isPlaying) {
			Animator a = ParentObj.gameObject.GetComponent<Animator> ();
			if (a == null) {
				a = ParentObj.gameObject.AddComponent<Animator> ();
				a.runtimeAnimatorController = Resources.Load ("ToggleableAnimController") as RuntimeAnimatorController;
			}
		}
	}

	void Update () {
		bool open = EditorOpen;
		if (Application.isPlaying) {
			open = ParentObj.Animator.GetBool ("AnimState1");
		}
		Pivot.localEulerAngles = open ? OpenRotation : ClosedRotation;
	}
}
