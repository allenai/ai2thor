// Copyright Allen Institute for Artificial Intelligence 2017
using UnityEngine;
using System.Collections;

public class Fridge : MonoBehaviour {

	public SimObj ParentObj;

	public Transform[] Doors;
	public Vector3[] OpenRotations;
	public Vector3[] ClosedRotations;
	public Transform[] Drawers;
	public Vector3[] OpenPositions;
	public Vector3[] ClosedPositions;

	Vector3 doorTargetRotation;
	Vector3 drawerTargetPosition;
	float distanceToTarget;

	void Awake() {
		ParentObj = gameObject.GetComponent<SimObj> ();
	}

	void Update () {
		bool open = false;
		if (!ParentObj.IsAnimated)
			return;

		open = ParentObj.Animator.GetBool ("AnimState1");

		switch (SceneManager.Current.AnimationMode) {
		case SceneAnimationMode.Instant:
		default:
			for (int i = 0; i < Doors.Length; i++) {
				Doors [i].localEulerAngles = open ? OpenRotations [i] : ClosedRotations [i];
			}
			for (int i = 0; i < Drawers.Length; i++) {
				Drawers [i].localPosition = open ? OpenPositions [i] : ClosedPositions [i];
			}
			break;

		case SceneAnimationMode.Smooth:
			distanceToTarget = 0f;
			for (int i = 0; i < Doors.Length; i++) {
				doorTargetRotation = open ? OpenRotations [i] : ClosedRotations [i];
				Quaternion doorStartRotation = Doors [i].rotation;
				Doors [i].localEulerAngles = doorTargetRotation;
				doorTargetRotation = Doors [i].localEulerAngles;
				Doors [i].rotation = Quaternion.RotateTowards (doorStartRotation, Doors [i].rotation, Time.deltaTime * SimUtil.SmoothAnimationSpeed * 25);
				distanceToTarget = Mathf.Max (distanceToTarget, Vector3.Distance (Doors [i].localEulerAngles, doorTargetRotation));
			}
			for (int i = 0; i < Drawers.Length; i++) {
				drawerTargetPosition = open ? OpenPositions [i] : ClosedPositions [i];
				Drawers [i].localPosition = Vector3.Lerp (Drawers [i].localPosition, drawerTargetPosition, Time.deltaTime * SimUtil.SmoothAnimationSpeed);
				distanceToTarget = Mathf.Max (distanceToTarget, Vector3.Distance (Doors [i].localPosition, drawerTargetPosition));
			}

			if (distanceToTarget >= 360f)
				distanceToTarget -= 360f;

			ParentObj.IsAnimating = distanceToTarget > 0.0025f;
			break;
		}
	}
}
