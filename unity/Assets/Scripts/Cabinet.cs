// Copyright Allen Institute for Artificial Intelligence 2017
using UnityEngine;
using System.Collections;

public enum CabinetOpenStyle {
	SingleDoorLeft,
	SingleDoorRight,
	DoubleDoors,
	Drawer
}

[ExecuteInEditMode]
public class Cabinet : MonoBehaviour {

	public bool Animate = false;
	public bool Open;
	public SimObj ParentObj;

	public CabinetOpenStyle OpenStyle;
	public Transform LeftDoor;
	public Transform RightDoor;
	public Transform DrawerDoor;

	public Vector3 OpenAngleRight = new Vector3 (0f, -90f, 0f);
	public Vector3 OpenAngleLeft = new Vector3 (0f, 90f, 0f);
	public Vector3 ClosedAngleRight = new Vector3 (0f, 0f, 0f);
	public Vector3 ClosedAngleLeft = new Vector3 (0f, 0f, 0f);
	public Vector3 ClosedLocalPosition = new Vector3 (0f, 0f, 0f);
	public Vector3 OpenLocalPosition = new Vector3(0f, 0f, 0f);

	Vector3 rightDoorTargetRotation;
	Vector3 leftDoorTargetRotation;
	Vector3 drawerTargetPosition;
	//bool lastOpen = false;
	float animatingDistance;

	public void OnEnable() {
		ParentObj.Manipulation = SimObjManipType.StaticNoPlacement;
		if (!Application.isPlaying) {
			Animator a = ParentObj.gameObject.GetComponent<Animator> ();
			if (a == null) {
				a = ParentObj.gameObject.AddComponent<Animator> ();
				a.runtimeAnimatorController = Resources.Load ("ToggleableAnimController") as RuntimeAnimatorController;
			}
		}
	}


	public void Update () {
		bool open = Open;
		if (Application.isPlaying) {
			//get whether we're open from our animation state
			if (!ParentObj.IsAnimated) {
				return;
			}
			open = ParentObj.Animator.GetBool ("AnimState1");
		}

		if (!Application.isPlaying && !Animate)
			return;

		switch (SceneManager.Current.AnimationMode) {
		case SceneAnimationMode.Instant:
		default:
			UpdateAnimationInstant (open);
			break;			

		case SceneAnimationMode.Smooth:
			if (Application.isPlaying) {
				UpdateAnimationSmooth (open);
			}
			break;
		}
	}

	void UpdateAnimationSmooth (bool open) {

		Quaternion rightDoorStartRotation = Quaternion.identity;
		Quaternion leftDoorStartRotation = Quaternion.identity;

		switch (OpenStyle) {
		case CabinetOpenStyle.DoubleDoors:
			rightDoorStartRotation = RightDoor.rotation;
			leftDoorStartRotation = LeftDoor.rotation;

			rightDoorTargetRotation = open ? OpenAngleRight : ClosedAngleRight;
			leftDoorTargetRotation = open ? OpenAngleLeft : ClosedAngleLeft;

			RightDoor.localEulerAngles = rightDoorTargetRotation;
			LeftDoor.localEulerAngles = leftDoorTargetRotation;

			leftDoorTargetRotation = LeftDoor.localEulerAngles;
			rightDoorTargetRotation = RightDoor.localEulerAngles;

			RightDoor.rotation = Quaternion.RotateTowards (rightDoorStartRotation, RightDoor.rotation, Time.deltaTime * SimUtil.SmoothAnimationSpeed * 25);
			LeftDoor.rotation = Quaternion.RotateTowards (leftDoorStartRotation, LeftDoor.rotation, Time.deltaTime * SimUtil.SmoothAnimationSpeed * 25);
			animatingDistance = Vector3.Distance (RightDoor.localEulerAngles, rightDoorTargetRotation);
			break;

		case CabinetOpenStyle.SingleDoorLeft:
			leftDoorStartRotation = LeftDoor.rotation;
			leftDoorTargetRotation = open ? OpenAngleLeft : ClosedAngleLeft;
			LeftDoor.localEulerAngles = leftDoorTargetRotation;
			leftDoorTargetRotation = LeftDoor.localEulerAngles;
			LeftDoor.rotation = Quaternion.RotateTowards (leftDoorStartRotation, LeftDoor.rotation, Time.deltaTime * SimUtil.SmoothAnimationSpeed * 25);
			animatingDistance = Vector3.Distance (LeftDoor.localEulerAngles, leftDoorTargetRotation);
			break;

		case CabinetOpenStyle.SingleDoorRight:
			rightDoorStartRotation = RightDoor.rotation;
			rightDoorTargetRotation = open ? OpenAngleRight : ClosedAngleRight;
			RightDoor.localEulerAngles = rightDoorTargetRotation;
			rightDoorTargetRotation = RightDoor.localEulerAngles;
			RightDoor.rotation = Quaternion.RotateTowards (rightDoorStartRotation, RightDoor.rotation, Time.deltaTime * SimUtil.SmoothAnimationSpeed * 25);
			animatingDistance = Vector3.Distance (RightDoor.localEulerAngles, rightDoorTargetRotation);
			break;

		case CabinetOpenStyle.Drawer:
			drawerTargetPosition = open ? OpenLocalPosition : ClosedLocalPosition;
			DrawerDoor.localPosition = Vector3.Lerp (DrawerDoor.localPosition, drawerTargetPosition, Time.deltaTime * SimUtil.SmoothAnimationSpeed);
			animatingDistance = Vector3.Distance (DrawerDoor.localPosition, drawerTargetPosition);
			break;
		}

		if (animatingDistance >= 360f)
			animatingDistance -= 360f;

		ParentObj.IsAnimating = (animatingDistance > 0.0025f);
	}

	void UpdateAnimationInstant(bool open) {
		switch (OpenStyle) {
		case CabinetOpenStyle.DoubleDoors:
			RightDoor.transform.localEulerAngles = open ? OpenAngleRight : ClosedAngleRight;
			LeftDoor.transform.localEulerAngles = open ? OpenAngleLeft : ClosedAngleLeft;
			break;

		case CabinetOpenStyle.SingleDoorLeft:
			LeftDoor.transform.localEulerAngles = open ? OpenAngleLeft : ClosedAngleLeft;
			break;

		case CabinetOpenStyle.SingleDoorRight:
			RightDoor.transform.localEulerAngles = open ? OpenAngleRight : ClosedAngleRight;
			break;

		case CabinetOpenStyle.Drawer:
			DrawerDoor.transform.localPosition = open ? OpenLocalPosition : ClosedLocalPosition;
			break;
		}
	}
}
