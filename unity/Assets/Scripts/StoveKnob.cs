// Copyright Allen Institute for Artificial Intelligence 2017
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]
[RequireComponent(typeof(SimObj))]
public class StoveKnob : MonoBehaviour {
    public SimObj StoveRange;
	public bool On = false;

	public Transform KnobTransform;
	public Vector3 OnRotation;
	public Vector3 OffRotation;

    SimObj simObj;
	bool displayedError = false;

    void Awake() {
        simObj = gameObject.GetComponent<SimObj>();
    }

    void Update() {
		if (KnobTransform == null || StoveRange == null) {
			if (!displayedError) {
				displayedError = true;
				Debug.LogError ("Knob transform or stove range null in Stove Knob " + name);
			}
			return;
		}
		if (!Application.isPlaying) {
			KnobTransform.localEulerAngles = On ? OnRotation : OffRotation;
			return;
		}

		if (!simObj.IsAnimated)
			return;
        //set stove range anim state to knob's anim state
		On = simObj.Animator.GetBool("AnimState1");
		StoveRange.Animator.SetBool ("AnimState1", On);
		KnobTransform.localEulerAngles = On ? OnRotation : OffRotation;
    }
}
