using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrameCollider : MonoBehaviour 
{
	public Transform myObject;
	// Use this for initialization
	void Start () 
	{
		myObject = gameObject.GetComponentInParent<SimObjPhysics>().transform;

	}
	
	// Update is called once per frame
	void Update () 
	{

		//so we need to constantly update the rotation of this object so that the colliders stay in the right place.
		//unforunately we couldnt' just unparent them from the door because the door has the rigidbody, and if they were unparented then the 
		//compound box collider would not propogate up to the rigidbody for visibility detection, so here we are this seems to work!
		gameObject.transform.localEulerAngles = new Vector3(0, -myObject.transform.localEulerAngles.y, 0);

	}
}
