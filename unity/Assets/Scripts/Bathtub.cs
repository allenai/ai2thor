// Copyright Allen Institute for Artificial Intelligence 2017
using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class Bathtub : MonoBehaviour {

	public SimObj ParentObj;
	public bool EditorFilled = false;
	public GameObject FilledObject;

	void OnEnable () {
		ParentObj = gameObject.GetComponent <SimObj> ();
		if (ParentObj == null) {
			ParentObj = gameObject.AddComponent <SimObj> ();
		}
		ParentObj.Type = SimObjType.Bathtub;

		if (!Application.isPlaying) {
			Animator a = ParentObj.gameObject.GetComponent<Animator> ();
			if (a == null) {
				a = ParentObj.gameObject.AddComponent<Animator> ();
				a.runtimeAnimatorController = Resources.Load ("ToggleableAnimController") as RuntimeAnimatorController;
			}
		}
	}
		
	void Update () {
		bool filled = EditorFilled;
		if (Application.isPlaying) {
			filled = ParentObj.Animator.GetBool ("AnimState1");
		}

		if (FilledObject == null) {
			Debug.LogError ("Filled object is null in bathtub");
			return;
		}

		FilledObject.SetActive (filled);
	}
}
