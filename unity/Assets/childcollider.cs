using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class childcollider : MonoBehaviour 
{

	// Use this for initialization
	void Start () 
	{
		
	}
	
	// Update is called once per frame
	void Update () 
	{
		
	}

	void OnCollisionEnter(Collision collision)
    {
		print("collision enteR!");
        this.GetComponent<Collider>().attachedRigidbody.SendMessage("OnCollisionEnter", collision);
    }
}
