// Copyright Allen Institute for Artificial Intelligence 2017
using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class Television : MonoBehaviour {

	public SimObj ParentSimObj;
	public Renderer[] ScreenRenderers;
	public int[] ScreenMatIndexes;
	public Material OffScreenMat;
	public Material OnScreenMat;
	public Material SyncedScreenMat;
	public bool EditorOn = false;
	public bool EditorConnected = false;

	//1 - Off
	//2 - On, Unsynced
	//3 - On, Synced

	public void Update() {
		if (ParentSimObj == null)
			return;

		if (OffScreenMat == null || OnScreenMat == null)
			return;

		Material mat = OffScreenMat;

		if (!Application.isPlaying) {
			if (EditorOn) {
				if (EditorConnected) {
					mat = SyncedScreenMat;
				} else {
					mat = OnScreenMat;
				}
			}
		} else {
			switch (ParentSimObj.Animator.GetInteger ("AnimState1")) {
			case 1:
			default:
				break;

			case 2:
				mat = OnScreenMat;
				break;

			case 3:
				mat = SyncedScreenMat;
				break;
			}
		}

		for (int i = 0; i < ScreenRenderers.Length; i++) {
			Material[] sharedMats = ScreenRenderers [i].sharedMaterials;
			sharedMats [ScreenMatIndexes [i]] = mat;
			ScreenRenderers [i].sharedMaterials = sharedMats;
		}
	}
}
