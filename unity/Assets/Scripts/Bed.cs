// Copyright Allen Institute for Artificial Intelligence 2017
using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class Bed : MonoBehaviour {

	public SimObj ParentObj;
	//public GameObject FittedSheet;
	public GameObject TidyBlanket;
	public GameObject MessyBlanket;
	[Range(0,2)]
	public int EditorState = 0;

	void OnEnable() {
		ParentObj = gameObject.GetComponent <SimObj> ();
		if (ParentObj == null) {
			ParentObj = gameObject.AddComponent <SimObj> ();
		}
		ParentObj.Type = SimObjType.Bed;

		if (!Application.isPlaying) {
			Animator a = ParentObj.gameObject.GetComponent<Animator> ();
			if (a == null) {
				a = ParentObj.gameObject.AddComponent<Animator> ();
				a.runtimeAnimatorController = Resources.Load ("StateAnimController") as RuntimeAnimatorController;
			}
		}
	}

	void Update () {
		
		int state = EditorState;
		if (Application.isPlaying) {
			state = ParentObj.Animator.GetInteger ("AnimState1");
		}

		//0 - messy, no sheet
		//1 - clean, no sheet
		//2 - clean, sheet

		switch (state) {
		case 0:
		default:
			MessyBlanket.SetActive (true);
			TidyBlanket.SetActive (false);
			//FittedSheet.SetActive (false);
			break;

		case 1:
			MessyBlanket.SetActive (false);
			TidyBlanket.SetActive (true);
			//FittedSheet.SetActive (false);
			break;

		case 2:
			MessyBlanket.SetActive (false);
			TidyBlanket.SetActive (true);
			//FittedSheet.SetActive (true);
			break;
		}
	}
}
