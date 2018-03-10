// Copyright Allen Institute for Artificial Intelligence 2017
using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class Laptop : MonoBehaviour {

	public SimObj SimObjParent;
	public Transform[] PivotTransforms;
	public Renderer[] ScreenRenderers;
	public int[] ScreenMatIndexes;
	public Material OffScreenMat;
	public Material OnScreenMat;
	public Television Television;
	public Vector3 OpenRot;
	public Vector3 ClosedRot;

	public bool EditorOn;
	public bool EditorOpen;

	//1 - Closed
	//2 - Open, Off
	//3 - Open, On
	//4 - Open, Synced

	bool displayedError;

	void OnEnable() {
		Television = GameObject.FindObjectOfType<Television> ();
	}

	void Update() {
		if (Television != null) {
			Television.SyncedScreenMat = OnScreenMat;
		}

		Vector3 rot = ClosedRot;
		Material mat = OffScreenMat;

		if (!Application.isPlaying) {
			rot = EditorOpen ? OpenRot : ClosedRot;
			mat = EditorOn ? OnScreenMat : OffScreenMat;
		} else {
			if (SimObjParent == null || OffScreenMat == null || OnScreenMat == null) {
				if (!displayedError) {
					Debug.LogError ("Component null in latop " + name);
					displayedError = true;
				}
				return;
			}

			int animState = SimObjParent.Animator.GetInteger ("AnimState1");
			//1 - Closed, Off
			//2 - Open, Off
			//3 - Closed, On

			switch (animState) {
			case 1:
			default:
				rot = ClosedRot;
				mat = OffScreenMat;
				break;

			case 2:
				rot = OpenRot;
				mat = OffScreenMat;
				break;

			case 3:
				rot = ClosedRot;
				mat = OffScreenMat;
				break;
			}
		}

		for (int i = 0; i < ScreenRenderers.Length; i++) {
			Material[] sharedMats = ScreenRenderers [i].sharedMaterials;
			sharedMats [ScreenMatIndexes [i]] = mat;
			ScreenRenderers [i].sharedMaterials = sharedMats;
			PivotTransforms [i].localEulerAngles = rot;
		}
	}
}
