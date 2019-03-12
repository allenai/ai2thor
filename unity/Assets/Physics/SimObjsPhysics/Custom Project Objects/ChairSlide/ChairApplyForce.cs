using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//DO NOT USE THIS FOR REAL: This script made for demo reel purposes
public class ChairApplyForce : MonoBehaviour 
{
	public float Force = 10f;

	// Use this for initialization
	void Start () 
	{
		
	}
	
	// Update is called once per frame
	void Update () 
	{
		if(Input.GetKeyDown(KeyCode.H))
		{
			GOGOGO();
		}
	}

	public void GOGOGO()
	{
		Rigidbody rb = GetComponent<Rigidbody>();

		rb.AddForce(transform.forward * Force);
	}
}
