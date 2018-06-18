using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationTriggerCheck : MonoBehaviour 
{
	public bool isColliding = false;

	// Use this for initialization
	void Start () 
	{
		
	}
	
	// Update is called once per frame
	void Update () 
	{
		
	}

	private void FixedUpdate()
    {
        isColliding = false;

    }

	public void OnTriggerStay(Collider other)
    {
	    //this is in the Agent layer, so is the rest of the agent, so it won't collide with itself
        isColliding = true;
    }
}
