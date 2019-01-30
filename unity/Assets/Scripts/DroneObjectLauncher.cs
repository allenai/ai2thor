using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneObjectLauncher : MonoBehaviour 
{
	[SerializeField] GameObject prefabToLaunch;

	// public Vector3 direction;
	// public float magnitude;

	// Use this for initialization
	void Start () 
	{
		
	}
	
	// Update is called once per frame
	void Update () 
	{
		
	}

	public void Launch(float magnitude, Vector3 direction)
	{

		GameObject fireaway = Instantiate(prefabToLaunch, this.transform.position, this.transform.rotation);
		Rigidbody rb = fireaway.GetComponent<Rigidbody>();

		rb.isKinematic = false;
		rb.AddForce(direction * magnitude);

	}
}
