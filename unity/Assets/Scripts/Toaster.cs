using UnityEngine;
using System.Collections;

//[ExecuteInEditMode]
public class Toaster : MonoBehaviour {

	public SimObj SimObjParent;
	public GameObject BreadOn;
	public GameObject BreadOff;
	public GameObject LeverOn;
	public GameObject LeverOff;

	public bool EditorOn;
	public bool EditorFilled;
	
	bool displayedError = false;

	void Update() {
		int animState = 0;
		//1 - Off, Empty
		//2 - On, Empty
		//3 - Off, Full
		//4 - On, Full
		if (!Application.isPlaying) {
			if (EditorOn) {
				if (EditorFilled) {
					animState = 4;
				} else {
					animState = 2;
				}
			} else {
				if (EditorFilled) {
					animState = 3;
				} else {
					animState = 1;
				}
			}
		} else {
			if (SimObjParent == null) {
				if (!displayedError) {
					Debug.LogError ("SimObjParent null in toaster " + gameObject.name);
					displayedError = true;
				}
				return;
			}
			animState = SimObjParent.Animator.GetInteger ("AnimState1");
		}

		if (BreadOn == null || BreadOff == null || LeverOn == null || LeverOff == null) {
			if (!displayedError) {
				Debug.LogError ("Object null in toaster " + name);
				displayedError = true;
			}
			return;
		}
		
		switch (animState) {
		default:
		case 1:
			BreadOn.SetActive (false);
			BreadOff.SetActive (false);
			LeverOn.SetActive (false);
			LeverOff.SetActive (true);
			break;

		case 2:
			BreadOn.SetActive (false);
			BreadOff.SetActive (false);
			LeverOn.SetActive (true);
			LeverOff.SetActive (false);
			break;

		case 3:
			BreadOn.SetActive (false);
			BreadOff.SetActive (true);
			LeverOn.SetActive (false);
			LeverOff.SetActive (true);
			break;

		case 4:
			BreadOn.SetActive (true);
			BreadOff.SetActive (false);
			LeverOn.SetActive (true);
			LeverOff.SetActive (false);
			break;
		}
	}
}
