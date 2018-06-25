using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerColliderCheck : MonoBehaviour 
{
	public bool isColliding = false;
	public GameObject MyObject = null;

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
		isColliding = true;
  //      //make sure nothing is dropped while inside the agent (the agent will try to "push(?)" it out and it will fall in unpredictable ways
  //      if (other.tag == "Player" && other.name == "FPSController")
  //      {
  //          isColliding = true;
  //      }

  //      //ignore the trigger boxes the agent is using to check rotation, otherwise the object is colliding
  //      if (other.tag != "Player")
  //      {

  //          isColliding = true;
  //          //print(this.name +" is touching " + other.name);
  //      }

  //      if(other.transform == MyObject.transform)
		//{
		//	//print(other.name);

		//	isColliding = false;
		//}
        ////print(transform.name + "aaaah");
    }

}
