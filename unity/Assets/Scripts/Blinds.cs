// Copyright Allen Institute for Artificial Intelligence 2017
using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class Blinds : MonoBehaviour {

	public SimObj ParentObj;
	public bool EditorOpen = false;
	public bool OpenByDefault = true;
	public GameObject OpenObject;
	public GameObject ClosedObject;

	void OnEnable () {
		EditorOpen = OpenByDefault;
		ParentObj = gameObject.GetComponent <SimObj> ();
		if (ParentObj == null) {
			ParentObj = gameObject.AddComponent <SimObj> ();
		}
		ParentObj.Type = SimObjType.Blinds;

		if (!Application.isPlaying) {
			Animator a = ParentObj.gameObject.GetComponent<Animator> ();
			if (a == null) {
				a = ParentObj.gameObject.AddComponent<Animator> ();
				a.runtimeAnimatorController = Resources.Load ("ToggleableAnimController") as RuntimeAnimatorController;
			}
		} else {
			if (OpenByDefault) {
				ParentObj.Animator.SetBool ("AnimState1", true);
			}
		}
	}

	void Update () {
		bool open = EditorOpen;
		if (Application.isPlaying) {
			open = ParentObj.Animator.GetBool ("AnimState1");
		}

		if (OpenObject == null || ClosedObject == null) {
			Debug.LogError ("Open or closed object is null in blinds");
			return;
		}

		OpenObject.SetActive (open);
		ClosedObject.SetActive (!open);
	}
}
