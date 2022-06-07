using UnityEngine;
using System.Collections;

public class ZRNGUIController : MonoBehaviour {

	private float hSliderValue = 0.0f;
	private bool menuVisible = false;
	private int operateCameraNumber;
	private bool shadowOn;

	private const string ZENRIN_URL = "http://www.zenrin.co.jp/";
	private const string PQ_URL = "http://www.pocket-queries.co.jp/";

	[SerializeField]
	GameObject[] QueryObjects;

	int previousCameraNumber;
	
	string playModeString;

	// Use this for initialization
	void Start () {

		this.GetComponent<CameraController>().ChangeCamera(0);
		operateCameraNumber = 0;
		previousCameraNumber = 0;

		this.GetComponent<AmbientController>().changeShadow(true);
		shadowOn = true;

		changePlayMode(0);
		SetQueryChan(0);
	}
	
	// Update is called once per frame
	void Update () {
		
	}


	void OnGUI () {

		if (menuVisible == true)
		{
			GUI.BeginGroup (new Rect (50, 50, Screen.width - 100, 270));

			GUI.Box (new Rect (0, 0, Screen.width - 100, 270), "Control Menu");

			if (GUI.Button (new Rect (Screen.width - 100 - 50 , 10, 40, 40), "X"))
			{
				menuVisible = false;
			}

			// ---------- Sky Control ----------
			GUI.Label (new Rect (20, 40, 100, 30), "Sky Control");
			if (GUI.Button (new Rect (20, 60, 80, 40), "Sunny"))
			{
				this.GetComponent<AmbientController>().changeSkybox(AmbientController.AmbientType.AMBIENT_SKYBOX_SUNNY);
			}
			if (GUI.Button (new Rect (110, 60, 80, 40), "Cloud"))
			{
				this.GetComponent<AmbientController>().changeSkybox(AmbientController.AmbientType.AMBIENT_SKYBOX_CLOUD);
			}
			if (GUI.Button (new Rect (200, 60, 80, 40), "Night"))
			{
				this.GetComponent<AmbientController>().changeSkybox(AmbientController.AmbientType.AMBIENT_SKYBOX_NIGHT);
			}

			// ---------- Shadow Control ----------
			GUI.Label (new Rect (20, 110, 100, 30), "Shadow Control");
			if (GUI.Button (new Rect (20, 130, 80, 40), "On / Off"))
			{
				if (shadowOn == false)
				{
					this.GetComponent<AmbientController>().changeShadow(true);
					shadowOn = true;
				}
				else
				{
					this.GetComponent<AmbientController>().changeShadow(false);
					shadowOn = false;
				}
			}
			GUI.Label (new Rect (120, 130, 100, 30), "TIme");
			hSliderValue = GUI.HorizontalSlider (new Rect (120, 155, 150, 30), hSliderValue, 0.0f, 100.0f);
			this.GetComponent<AmbientController>().rotateAmbientLight(hSliderValue);

			// ---------- Effect Control ----------
			GUI.Label (new Rect (20, 180, 100, 30), "Effect Control");
			if (GUI.Button (new Rect (20, 200, 80, 40), "None"))
			{
				this.GetComponent<AmbientController>().changeParticle(AmbientController.ParticleType.PARTICLE_NONE);
			}
			if (GUI.Button (new Rect (110, 200, 80, 40), "Wind"))
			{
				this.GetComponent<AmbientController>().changeParticle(AmbientController.ParticleType.PARTICLE_WIND);
			}
			if (GUI.Button (new Rect (200, 200, 80, 40), "Rain"))
			{
				this.GetComponent<AmbientController>().changeParticle(AmbientController.ParticleType.PARTICLE_RAIN);
			}

			// ---------- Camera Control ----------
			if (operateCameraNumber < 100)
			{
				GUI.Label (new Rect (400, 40, 100, 30), "Camera Control");
				if (GUI.Button (new Rect (400, 60, 50, 40), "<---"))
				{
					operateCameraNumber--;
					if (operateCameraNumber < 0)
					{
						operateCameraNumber = this.GetComponent<CameraController>().targetCameraNames.Count -1;
						previousCameraNumber = operateCameraNumber;
					}
				}
				if (GUI.Button (new Rect (600, 60, 50, 40), "--->"))
				{
					operateCameraNumber++;
					if (operateCameraNumber > this.GetComponent<CameraController>().targetCameraNames.Count -1)
					{
						operateCameraNumber = 0;
						previousCameraNumber = operateCameraNumber;
					}
				}
				GUI.Label (new Rect (460, 60, 140, 20), this.GetComponent<CameraController>().targetCameraNames[operateCameraNumber]);
				if (GUI.Button (new Rect (450, 80, 150, 20), "Change"))
				{
					this.GetComponent<CameraController>().ChangeCamera(operateCameraNumber);
					previousCameraNumber = operateCameraNumber;
					SetQueryChan(0);
				}
			}

			// ---------- Move Control ----------
			GUI.Label (new Rect (400, 110, 100, 30), "Move Control");
			if (GUI.Button (new Rect (400, 130, 80, 40), "Normal"))
			{
				operateCameraNumber = previousCameraNumber;
				this.GetComponent<CameraController>().ChangeCamera(operateCameraNumber);
				SetQueryChan(0);
				changePlayMode(0);
			}
			if (GUI.Button (new Rect (490, 130, 80, 40), "FlyThrough"))
			{
				SetQueryChan(1);
				changePlayMode(1);
			}
			if (GUI.Button (new Rect (580, 130, 80, 40), "Driving"))
			{
				InitAICars();
				changePlayMode(2);
			}

			// ---------- Info Control ----------
			GUI.Label (new Rect (400, 180, 100, 30), "Information");
			if (GUI.Button (new Rect (400, 200, 120, 40), "ZENRIN"))
			{
				Application.OpenURL(ZENRIN_URL);
			}
			if (GUI.Button (new Rect (530, 200, 120, 40), "Pocket Queries"))
			{
				Application.OpenURL(PQ_URL);	
			}
			
			GUI.EndGroup ();
		}
		else
		{
			// ---------- Menu Button ----------
			if (GUI.Button (new Rect (Screen.width - 120 , 20, 100, 40), "Menu"))
			{
				menuVisible = true;
			}
		}

		// Display PlayMode 
		GUI.Box( new Rect(30 , Screen.height - 60, 250, 50), "Mode = " + playModeString);

	}


	void SetQueryChan (int QueryNumber) {

		foreach (GameObject targetQueryChan in QueryObjects)
		{
			targetQueryChan.SetActive(false);
		}
		QueryObjects[QueryNumber].SetActive(true);
		if (QueryNumber == 1)
		{
			QueryObjects[QueryNumber].GetComponent<FlyThroughController>().InitQuery();
			operateCameraNumber = 100;
			this.GetComponent<CameraController>().ChangeCamera(operateCameraNumber);
		}

	}


	void changePlayMode (int modeNumber) {

		switch (modeNumber)
		{
		case 0:
			playModeString = "Normal";
			break;
		case 1:
			playModeString = "FlyThrough\nkey: z = decelerate,  x = accelerate\n arrow key:  up , down, left, right";
			break;
		case 2:
			playModeString = "Driving";
			break;
		}
	}


	void InitAICars () {

		GameObject[] targetAICars = GameObject.FindGameObjectsWithTag("AICars");
		foreach (GameObject targetAICar in targetAICars)
		{
			targetAICar.GetComponent<AICarMove>().InitAICar();
			operateCameraNumber = 200;
			this.GetComponent<CameraController>().ChangeCamera(operateCameraNumber);
		}



	}

}
