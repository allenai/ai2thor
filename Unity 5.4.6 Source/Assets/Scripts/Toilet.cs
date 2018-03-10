// Copyright Allen Institute for Artificial Intelligence 2017
using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class Toilet : MonoBehaviour {

	public SimObj ParentSimObj;
	public Transform Lid;
	public Vector3 OpenRotation;
	public Vector3 ClosedRotation;
	public GameObject DirtObject;
	[Range(0,2)]
	public int EditorState = 0;

	void OnEnable() {
		ParentSimObj = gameObject.GetComponent <SimObj> ();
		if (ParentSimObj == null) {
			ParentSimObj = gameObject.AddComponent <SimObj> ();
		}
		ParentSimObj.Type = SimObjType.Toilet;

		if (!Application.isPlaying) {
			Animator a = ParentSimObj.gameObject.GetComponent<Animator> ();
			if (a == null) {
				a = ParentSimObj.gameObject.AddComponent<Animator> ();
				a.runtimeAnimatorController = Resources.Load ("StateAnimController") as RuntimeAnimatorController;
			}
		}
	}
	// Update is called once per frame
	void Update () {
		int state = EditorState;
		if (Application.isPlaying) {
			state = ParentSimObj.Animator.GetInteger ("AnimState1");
		}

		//0 - closed
		//1 - open, dirty
		//2 - open, clean

		switch (state) {
		case 0:
		default:
			Lid.localEulerAngles = ClosedRotation;
			DirtObject.SetActive (false);
			break;

		case 1:
			Lid.localEulerAngles = OpenRotation;
			DirtObject.SetActive (true);
			break;

		case 2:
			Lid.localEulerAngles = OpenRotation;
			DirtObject.SetActive (false);
			break;
		}
	}
}
