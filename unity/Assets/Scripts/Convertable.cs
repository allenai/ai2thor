// Copyright Allen Institute for Artificial Intelligence 2017
using UnityEngine;
using System.Collections;
using System;

[Serializable]
public class SimObjState {
	public GameObject Obj;
	public SimObjType Type;
}

[ExecuteInEditMode]
public class Convertable : MonoBehaviour {
	public SimObjState[] States;
	public int DefaultState = 0;
	public int CurrentState {
		get {
			return currentState;
		}
	}
	#if UNITY_EDITOR
	public int EditorState;
	#endif

	int currentState = -1;
	SimObj parentObj;

	void OnEnable (){
		if (Application.isPlaying) {
			currentState = DefaultState;
		}
	}

	void Update () {
		if (parentObj == null)
			parentObj = gameObject.GetComponent<SimObj> ();

		//anim state is 1-4
		int animState = -1;
		if (Application.isPlaying) {
			animState = parentObj.Animator.GetInteger ("AnimState1");
		}
		#if UNITY_EDITOR
		else {
			animState = EditorState + 1;
		}
		#endif
		if (animState > States.Length) {
			animState = -1;
		}

		//stateIndex is 0-3
		int stateIndex = animState - 1;
		if (currentState != stateIndex && stateIndex >= 0) {
			currentState = stateIndex;
			for (int i = 0; i < States.Length; i++) {
				if (i == currentState) {
					parentObj.Type = States [i].Type;
					if (States [i].Obj != null) {
						States [i].Obj.SetActive (true);
					}
				} else {
					if (States [i].Obj != null) {
						States [i].Obj.SetActive (false);
					}
				}
			}
		}
	}
}
