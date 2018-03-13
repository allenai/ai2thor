// Copyright Allen Institute for Artificial Intelligence 2017
using UnityEngine;
using System.Collections;

public enum FurniturePosition {
	Undesirable,
	Desireable,
}

[ExecuteInEditMode]
public class Rearrangeable : MonoBehaviour {
	public bool EditorPos = false;
	public Transform StartPosition;
	public Transform EndPosition;
	public SimObj ParentSimObj;
	public FurniturePosition Position {
		get {
			return ParentSimObj.Animator.GetBool ("AnimState1") ? FurniturePosition.Undesirable : FurniturePosition.Desireable;
		}
	}

	bool reportedError = false;

	public void MoveTo (FurniturePosition state) {
		ParentSimObj.Animator.SetBool ("AnimState1", state == FurniturePosition.Undesirable);
	}

	void OnEnable() {
		if (!Application.isPlaying) {
			if (StartPosition == null) {
				StartPosition = new GameObject (name + "_StartPosition").transform;
				StartPosition.parent = transform.parent;
				StartPosition.localPosition = transform.localPosition;
				StartPosition.localRotation = transform.localRotation;
			}
			if (EndPosition == null) {
				EndPosition = new GameObject (name + "_EndPosition").transform;
				EndPosition.parent = transform.parent;
				EndPosition.localPosition = transform.localPosition;
				EndPosition.localRotation = transform.localRotation;
			}
			if (ParentSimObj == null) {
				ParentSimObj = gameObject.GetComponent<SimObj> ();
			}
			if (ParentSimObj != null) {
				if (!ParentSimObj.IsAnimated) {
					Animator a = ParentSimObj.gameObject.AddComponent <Animator> ();
					a.runtimeAnimatorController = Resources.Load ("ToggleableAnimController") as RuntimeAnimatorController;
					gameObject.SetActive (false);
					gameObject.SetActive (true);
				}
			}
		} else {
			MoveTo (FurniturePosition.Undesirable);
		}
	}

	void Update () {
		if (ParentSimObj == null) {
			if (!reportedError) {
				reportedError = true;
				Debug.LogError ("Parent sim obj null in rearrangeable " + name);
			}
			return;
		}

		if (!ParentSimObj.IsAnimated) {
			if (!reportedError) {
				reportedError = true;
				Debug.LogError ("Parent sim obj is not animated in rearrangeable " + name);
			}
			return;
		}

		if (StartPosition == null || EndPosition == null) {
			if (!reportedError) {
				reportedError = true;
				Debug.LogError ("Start or end positions is null in rearrangeable " + name);
			}
			return;
		}

		bool startPos = false;
		if (Application.isPlaying) {
			startPos = ParentSimObj.Animator.GetBool ("AnimState1");
		} else {
			startPos = EditorPos;
		}
		ParentSimObj.transform.position = startPos ? StartPosition.position : EndPosition.position;
		ParentSimObj.transform.rotation = startPos ? StartPosition.rotation : EndPosition.rotation;
	}

	void OnDrawGizmos () {
		if (Application.isPlaying)
			return;

		if (ParentSimObj == null || StartPosition == null || EndPosition == null)
			return;

		MeshFilter mf = ParentSimObj.GetComponentInChildren <MeshFilter> ();
		if (mf != null) {
			Gizmos.color = EditorPos ? Color.Lerp (Color.red, Color.clear, 0.5f) : Color.Lerp (Color.green, Color.clear, 0.5f);
			Gizmos.matrix = EditorPos ? StartPosition.localToWorldMatrix : EndPosition.localToWorldMatrix;
			Gizmos.DrawWireMesh (mf.sharedMesh);
		}
	}
}
