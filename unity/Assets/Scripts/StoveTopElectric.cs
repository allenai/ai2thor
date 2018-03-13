// Copyright Allen Institute for Artificial Intelligence 2017
using UnityEngine;
using System.Collections;

public class StoveTopElectric : MonoBehaviour {

	public SimObj ParentObj;
	public Material BurnerOnMat;
	public int MatIndex;
	public Renderer BurnerRenderer;

	Material[] matArrayOn;
	Material[] matArrayOff;

	void Awake () {
		ParentObj = gameObject.GetComponent<SimObj> ();
		matArrayOn = BurnerRenderer.sharedMaterials;
		matArrayOff = BurnerRenderer.sharedMaterials;
		matArrayOn [MatIndex] = BurnerOnMat;
	}

	void Update() {
		BurnerRenderer.sharedMaterials = ParentObj.Animator.GetBool ("AnimState1") ? matArrayOn : matArrayOff;
	}
}
