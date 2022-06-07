using UnityEngine;
using System.Collections;

public class AmbientController : MonoBehaviour {

	[SerializeField]
	GameObject cameraObject;
	[SerializeField]
	GameObject lightObject;
	[SerializeField]
	GameObject spotLightObject;
	Vector3 lightBaseRotation;
	[SerializeField]
	Material[] skyboxMaterials;
	[SerializeField]
	GameObject[] particleObjects;


	public enum AmbientType
	{
		AMBIENT_SKYBOX_SUNNY = 0,
		AMBIENT_SKYBOX_CLOUD = 1,
		AMBIENT_SKYBOX_NIGHT = 2
	}

	public enum ParticleType
	{
		PARTICLE_NONE = -1,
		PARTICLE_WIND = 0,
		PARTICLE_RAIN = 1
	}


	// Use this for initialization
	void Start () {
		lightBaseRotation = lightObject.transform.localEulerAngles;
	}
	
	// Update is called once per frame
	void Update () {
	
	}


	public void rotateAmbientLight (float angleAddRotation) {

		lightObject.transform.localEulerAngles = new Vector3 (lightBaseRotation.x + angleAddRotation, lightBaseRotation.y, lightBaseRotation.z);
		
	}

	public void changeSkybox (AmbientType skyNumber) {

		cameraObject.GetComponent<Skybox>().material = skyboxMaterials[(int)skyNumber];

		if (skyNumber == AmbientType.AMBIENT_SKYBOX_SUNNY)
		{
			lightObject.GetComponent<Light>().intensity = 0.5f;
			spotLightObject.SetActive(false);
			changeShadow(true);
		}
		else if (skyNumber == AmbientType.AMBIENT_SKYBOX_CLOUD)
		{
			lightObject.GetComponent<Light>().intensity = 0.3f;
			spotLightObject.SetActive(false);
			changeShadow(true);
		}
		else if (skyNumber == AmbientType.AMBIENT_SKYBOX_NIGHT)
		{
			lightObject.GetComponent<Light>().intensity = 0.1f;
			spotLightObject.SetActive(true);
			changeShadow(false);
		}

	}

	public void changeShadow (bool shadowOn) {

		if (shadowOn == true)
		{
			lightObject.GetComponent<Light>().shadows = LightShadows.Soft;
		}
		else
		{
			lightObject.GetComponent<Light>().shadows = LightShadows.None;
		}

	}

	public void changeParticle (ParticleType particleNumber) {

		foreach (GameObject targetParticle in particleObjects)
		{
			targetParticle.SetActive(false);
		}
		if (particleNumber != ParticleType.PARTICLE_NONE)
		{
			particleObjects[(int)particleNumber].SetActive(true);
		}

	}

}
