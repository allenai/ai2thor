// Copyright Allen Institute for Artificial Intelligence 2017
using UnityEngine;
using System.Collections;

//[ExecuteInEditMode]
public class Microwave : MonoBehaviour {
	public Transform Door;
	public Vector3 DoorOpenRot;
	public Vector3 DoorClosedRot;
	public SimObj SimObjParent;
	public Material OnGlassMat;
	public Material OffGlassMat;
	public Renderer GlassRenderer;
	public int MatIndex;

	Vector3 targetDoorRotation;

	public bool EditorOpen = false;
	public bool EditorOn = false;
	
	bool displayedError = false;

	public void Update() {
		if (!Application.isPlaying) {
			Door.localEulerAngles = EditorOpen ? DoorOpenRot : DoorClosedRot;
			Material[] sharedMats = GlassRenderer.sharedMaterials;
			sharedMats [MatIndex] = EditorOn ? OnGlassMat : OffGlassMat;
			GlassRenderer.sharedMaterials = sharedMats;
		} else {
			if (SimObjParent == null || GlassRenderer == null || Door == null) {
				if (!displayedError) {
					Debug.LogError ("Component null in microwave " + name);
					displayedError = true;
				}
				return;
			}
		
			int animState = SimObjParent.Animator.GetInteger ("AnimState1");
			//1 - Closed, Off
			//2 - Open, Off
			//3 - Closed, On
			Material[] sharedMats = GlassRenderer.sharedMaterials;
			bool waitForDoorToClose = false;
			switch (animState) {
			case 1:
			default:
				targetDoorRotation = DoorClosedRot;
				sharedMats [MatIndex] = OffGlassMat;
				break;

			case 2:
				targetDoorRotation = DoorOpenRot;
				sharedMats [MatIndex] = OffGlassMat;
				break;

			case 3:
				targetDoorRotation = DoorClosedRot;
				sharedMats [MatIndex] = OnGlassMat;
				waitForDoorToClose = true;
				break;
			}

			switch (SceneManager.Current.AnimationMode) {
			case SceneAnimationMode.Smooth:
				Quaternion doorStartRotation = Quaternion.identity;

				doorStartRotation = Door.rotation;

				Door.localEulerAngles = targetDoorRotation;
				targetDoorRotation = Door.localEulerAngles;

				Door.rotation = Quaternion.RotateTowards (doorStartRotation, Door.rotation, Time.deltaTime * SimUtil.SmoothAnimationSpeed * 25);

				float distanceToTarget = Vector3.Distance (Door.localEulerAngles, targetDoorRotation);
				if (distanceToTarget >= 360f)
					distanceToTarget -= 360f;

				if (!waitForDoorToClose || distanceToTarget < 0.005f) {
					GlassRenderer.sharedMaterials = sharedMats;
				}

				SimObjParent.IsAnimating = distanceToTarget > 0.0025f;
				break;

			case SceneAnimationMode.Instant:
			default:
				Door.localEulerAngles = targetDoorRotation;
				GlassRenderer.sharedMaterials = sharedMats;
				break;
			}
		}
	}
}
