using System.Collections;
using System.Collections.Generic;
using UnityEngine;


//Maintains rotation based on the myObject object, which is the door of the sim object. This ensures that for any given
//cabinet shape, the cabinet can remain "visible" even when the door is out of frame. 

//to use: Put this prefab in the "TriggerColliders" section of the cabinet 
//rotate the CabinetDoor main sim object to an open position, and then rotate this object to that same position but opposite sign (ie:90, -90)

//Move the four fCol objects so that the camera of the agent can raycast to them when the door is open and out of frame. These fCol objects should
//frame the interior of the cabinet and since they are indivdual and isolated can be moved around to suite any shape or form interior space

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
