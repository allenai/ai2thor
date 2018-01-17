// Copyright Allen Institute for Artificial Intelligence 2017
using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class Mirror : MonoBehaviour {

	public SimObj ParentObj;
	public bool EditorDirty = false;
	public GameObject DirtObject;

	void OnEnable () {
		ParentObj = gameObject.GetComponent <SimObj> ();
		if (ParentObj == null) {
			ParentObj = gameObject.AddComponent <SimObj> ();
		}
		ParentObj.Type = SimObjType.Mirror;

		if (!Application.isPlaying) {
			Animator a = ParentObj.gameObject.GetComponent<Animator> ();
			if (a == null) {
				a = ParentObj.gameObject.AddComponent<Animator> ();
				a.runtimeAnimatorController = Resources.Load ("ToggleableAnimController") as RuntimeAnimatorController;
			}
		}
	}
		
	void Update () {
		bool dirty = EditorDirty;
		if (Application.isPlaying) {
			dirty = !ParentObj.Animator.GetBool ("AnimState1");
		}

		if (DirtObject == null) {
			Debug.LogError ("Dirt object is null in mirror");
			return;
		}

		DirtObject.SetActive (dirty);
	}
}
