// Copyright Allen Institute for Artificial Intelligence 2017
using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class Box : MonoBehaviour {

	public SimObj ParentSimObj;
	public GameObject[] Lids;
	public bool EditorClosed = true;

	void OnEnable() {
		if (Application.isPlaying) {
			ParentSimObj.Animator.SetBool ("AnimState1", false);
			foreach (GameObject lid in Lids) {
				Renderer r = lid.GetComponent <MeshRenderer> ();
				if (r != null) {
					bool lighten = SceneManager.Current.SceneNumber % 2 == 0;
					Material darkerMat = r.material;
					darkerMat.color = Color.Lerp (darkerMat.color, (lighten ? Color.white : Color.black), 0.15f);
				}
			}
		}
	}

	void Update () {
		bool closed = false;
		if (Application.isPlaying) {
			closed = !ParentSimObj.Animator.GetBool ("AnimState1");
		} else {
			closed = EditorClosed;
		}

		foreach (GameObject lid in Lids) {
			lid.SetActive (closed);
		}
	}
}
