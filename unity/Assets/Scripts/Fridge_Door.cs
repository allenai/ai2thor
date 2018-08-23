using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fridge_Door : MonoBehaviour 
{
	public CanOpen_Fridge myFridge;

	// Use this for initialization
	void Start () 
	{
		myFridge = gameObject.GetComponentInParent<CanOpen_Fridge>();
	}
	
	// Update is called once per frame
	void Update () 
	{
		
	}
       
	public void OnTriggerEnter(Collider other)
    {

        //note: Normally rigidbodies set to Kinematic will never call the OnTriggerX events
        //when colliding with another rigidbody that is kinematic. For some reason, if the other object
        //has a trigger collider even though THIS object only has a kinematic rigidbody, this
        //function is still called so we'll use that here:

        //The Agent has a trigger Capsule collider, and other cabinets/drawers have
        //a trigger collider, so this is used to reset the position if the agent or another
        //cabinet or drawer is in the way of this object opening/closing

        //if hitting the Agent, reset position and report failed action
        if (other.name == "FPSController" && myFridge.canReset == true)
        {
            Debug.Log(gameObject.name + " hit " + other.name + " Resetting position");
            myFridge.canReset = false;
            myFridge.Reset();
        }

        //if hitting another cabinet/drawer, do some checks 
        if (other.GetComponentInParent<CanOpen>() && myFridge.canReset == true)
        {
            if (myFridge.IsInIgnoreArray(other, myFridge.IgnoreTheseObjects))
            {
                //don't reset, it's cool to ignore these since some cabinets literally clip into each other if they are double doors
                return;
            }

            //oh it was something else RESET! DO IT!
            else
            {
                //check the collider hit's parent for itween instances
                //if 0, then it is not actively animating so check against it. This is needed so CanOpen objects don't reset unless they are the active
                //object moving. Otherwise, an open cabinet hit by a drawer would cause the Drawer AND the cabinet to try and reset.
                //this should be fine since only one cabinet/drawer will be moving at a time given the Agent's action only opening on object at a time
                if (other.transform.GetComponentInParent<CanOpen>().GetiTweenCount() == 0)//iTween.Count(other.transform.GetComponentInParent<CanOpen>().transform.gameObject) == 0)
                {
                    //print(other.GetComponentInParent<CanOpen>().transform.name);
                    Debug.Log(gameObject.name + " hit " + other.name + " Resetting position");
                    myFridge.canReset = false;
                    myFridge.Reset();
                }

            }
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.name == "FPSController" || other.GetComponentInParent<CanOpen>())
        {
            //print("HAAAAA");
            myFridge.canReset = true;
        }
    }
}
