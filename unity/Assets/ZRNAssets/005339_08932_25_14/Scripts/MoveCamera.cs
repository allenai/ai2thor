using UnityEngine;
using System.Collections;

public class MoveCamera : MonoBehaviour {

	[SerializeField]
	GameObject targetCamera;

	[SerializeField]
	Vector3[] cameraPositions;
	[SerializeField]
	Vector3[] cameraRotaions;

	bool streetAnimIsPlaying;
	bool rotaryAnimIsPlaying;

	const float speedMoveStreet = 1.0f;
	const float speedRotateRotary =  5.0f;

	// Use this for initialization
	void Start () {

		streetAnimIsPlaying = false;
		rotaryAnimIsPlaying = false;
		ChangeCameraAnimation(0);
	
	}


	void Update () {

		if (streetAnimIsPlaying == true)
		{
			targetCamera.transform.Translate(Vector3.forward * Time.deltaTime * speedMoveStreet);
			if (targetCamera.transform.localPosition.z < -52.0f)
			{
				ChangeCameraAnimation(0);
			}
		}
		else if (rotaryAnimIsPlaying == true)
		{
			targetCamera.transform.Rotate(Vector3.up * Time.deltaTime * speedRotateRotary);
		}

	}


	void OnGUI () {
		
		if (GUI.Button(new Rect(30, 30, 150, 80), "Hiroba"))
		{
			ChangeCameraAnimation(0);
		}
		if (GUI.Button(new Rect(30, 110, 150, 80), "Street"))
		{
			ChangeCameraAnimation(1);
		}
		if (GUI.Button(new Rect(30, 190, 150, 80), "Rotary"))
		{
			ChangeCameraAnimation(2);
		}
	
	}



	void ChangeCameraAnimation (int cameraNum) {

		streetAnimIsPlaying = false;
		rotaryAnimIsPlaying = false;

		targetCamera.transform.position = cameraPositions[cameraNum];
		targetCamera.transform.eulerAngles = cameraRotaions[cameraNum];

		switch (cameraNum)
		{
		case 0:

			break;

		case 1:
			streetAnimIsPlaying = true;
			break;

		case 2:
			rotaryAnimIsPlaying = true;
			break;

		default:

			break;
		}
		
	}
}
