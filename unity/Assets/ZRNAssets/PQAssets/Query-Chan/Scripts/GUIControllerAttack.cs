using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GUIControllerAttack : MonoBehaviour {

	Vector3 PosDefault;
	[SerializeField]
	GameObject CameraObj;
	private bool cameraUp;
	[SerializeField]
	GameObject queryChan;
	private int querySoundNumber;
	private int targetNum;
	List<string> targetSounds = new List<string>();
	
	
	void Start() {
		
		PosDefault = CameraObj.transform.localPosition;
		cameraUp = false;
		querySoundNumber = 0;
		
		foreach (AudioClip targetAudio in queryChan.GetComponent<QuerySoundController>().soundData)
		{
			targetSounds.Add(targetAudio.name);
		}
		targetNum = targetSounds.Count - 1;
		
		ChangeAnimation(QueryAnimationController.QueryChanAnimationType.STAND);
		
	}
	
	void OnGUI(){
		
		//AnimationChange ------------------------------------------------
		
		if(GUI.Button(new Rect(0,0,100,100),"Stand"))
		{
			ChangeAnimation(QueryAnimationController.QueryChanAnimationType.STAND);
		}
		if(GUI.Button(new Rect(0,100,100,100),"Idle"))
		{
			ChangeAnimation(QueryAnimationController.QueryChanAnimationType.AOQ_Idle);
		}
		if(GUI.Button(new Rect(0,200,100,100),"Rest_A"))
		{
			ChangeAnimation(QueryAnimationController.QueryChanAnimationType.AOQ_REST_A);
		}
		if(GUI.Button(new Rect(0,300,100,100),"Rest_B"))
		{
			ChangeAnimation(QueryAnimationController.QueryChanAnimationType.AOQ_REST_B);
		}
		if(GUI.Button(new Rect(0,400,100,100),"Walk"))
		{
			ChangeAnimation(QueryAnimationController.QueryChanAnimationType.AOQ_WALK);
		}
		if(GUI.Button(new Rect(0,500,100,100),"Hit"))
		{
			ChangeAnimation(QueryAnimationController.QueryChanAnimationType.AOQ_HIT);
		}
		if(GUI.Button(new Rect(0,600,100,100),"Glad"))
		{
			ChangeAnimation(QueryAnimationController.QueryChanAnimationType.AOQ_GLAD);
		}
		if(GUI.Button(new Rect(0,700,100,100),"Warp"))
		{
			ChangeAnimation(QueryAnimationController.QueryChanAnimationType.AOQ_WARP);
		}
		
		
		//FaceChange ------------------------------------------------
		
		if(GUI.Button(new Rect(Screen.width -100 ,0,100,100),"Normal"))
		{
			ChnageFace(QueryEmotionalController.QueryChanEmotionalType.NORMAL_EYEOPEN_MOUTHCLOSE);
		}
		if(GUI.Button(new Rect(Screen.width -100,100,100,100),"Mabataki"))
		{
			ChnageFace(QueryEmotionalController.QueryChanEmotionalType.NORMAL_EYECLOSE_MOUTHCLOSE);
		}
		if(GUI.Button(new Rect(Screen.width -100,200,100,100),"Anger"))
		{
			ChnageFace(QueryEmotionalController.QueryChanEmotionalType.ANGER_EYEOPEN_MOUTHCLOSE);
		}
		if(GUI.Button(new Rect(Screen.width -100,300,100,100),"Sad"))
		{
			ChnageFace(QueryEmotionalController.QueryChanEmotionalType.SAD_EYEOPEN_MOUTHCLOSE);
		}
		if(GUI.Button(new Rect(Screen.width -100,400,100,100),"Fun"))
		{
			ChnageFace(QueryEmotionalController.QueryChanEmotionalType.FUN_EYEOPEN_MOUTHCLOSE);
		}
		if(GUI.Button(new Rect(Screen.width -100,500,100,100),"Surprised"))
		{
			ChnageFace(QueryEmotionalController.QueryChanEmotionalType.SURPRISED_EYEOPEN_MOUTHOPEN);
		}
		
		
		//CameraChange --------------------------------------------
		
		if (GUI.Button (new Rect (Screen.width / 2 -75, 0, 150, 80), "Camera"))
		{
			if (cameraUp == true)
			{
				CameraObj.GetComponent<Camera>().fieldOfView = 60;
				CameraObj.transform.localPosition = new Vector3(PosDefault.x, PosDefault.y, PosDefault.z);
				cameraUp = false;
			}
			else
			{
				CameraObj.GetComponent<Camera>().fieldOfView = 25;
				CameraObj.transform.localPosition = new Vector3(PosDefault.x, PosDefault.y + 0.5f, PosDefault.z);
				cameraUp = true;
			}
		}
		
		
		//Sound ---------------------------------------------------------
		
		if(GUI.Button(new Rect(Screen.width / 2 - 150, Screen.height - 100, 50 ,100), "<---"))
		{
			querySoundNumber--;
			if (querySoundNumber < 0)
			{
				querySoundNumber = targetNum;
			}
		}
		if(GUI.Button(new Rect(Screen.width / 2 + 100, Screen.height - 100, 50 ,100), "--->"))
		{
			querySoundNumber++;
			if (querySoundNumber > targetNum)
			{
				querySoundNumber = 0;
			}
			
		}
		if(GUI.Button(new Rect(Screen.width / 2 - 100, Screen.height - 70, 200 ,70), "Play"))
		{
			queryChan.GetComponent<QuerySoundController>().PlaySoundByNumber(querySoundNumber);
		}
		
		GUI.Label (new Rect(Screen.width / 2 - 100, Screen.height - 100, 200, 30), (querySoundNumber+1) + " / " + (targetNum+1) + "  :  " + targetSounds[querySoundNumber]);
		
		
		//SceneChange --------------------------------------------
		
		if (GUI.Button (new Rect (Screen.width -100,700,100,100), "to Stand Mode"))
		{
			Application.LoadLevel("01_OperateQuery_Standing");
		}
		
	}
	
	
	void ChnageFace (QueryEmotionalController.QueryChanEmotionalType faceNumber) {
		
		queryChan.GetComponent<QueryEmotionalController>().ChangeEmotion(faceNumber);
		
	}
	
	
	void ChangeAnimation (QueryAnimationController.QueryChanAnimationType animNumber) {
		
		queryChan.GetComponent<QueryAnimationController>().ChangeAnimation(animNumber);
		
	}

}
