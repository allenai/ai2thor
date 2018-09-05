using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//this handles collision checks for the fridge door, resetting position to original open or closed state if it hits something in the way
public class MovingPart : MonoBehaviour 
{
	public CanOpen_Object myObject;

	// Use this for initialization
	void Start () 
	{
		myObject = gameObject.GetComponentInParent<CanOpen_Object>();
	}
	
	// Update is called once per frame
	void Update () 
	{
		
	}

    private bool hasAncestor(GameObject child, GameObject potentialAncestor) {
        if (child == potentialAncestor) {
            return true;
        } else if (child.transform.parent != null) {
            return hasAncestor(child.transform.parent.gameObject, potentialAncestor);
        } else {
            return false;
        }
    }
       
	public void OnTriggerEnter(Collider other)
    {
		//print(other.name);
        //note: Normally rigidbodies set to Kinematic will never call the OnTriggerX events
        //when colliding with another rigidbody that is kinematic. For some reason, if the other object
        //has a trigger collider even though THIS object only has a kinematic rigidbody, this
        //function is still called so we'll use that here:

        //The Agent has a trigger Capsule collider, and other cabinets/drawers have
        //a trigger collider, so this is used to reset the position if the agent or another
        //cabinet or drawer is in the way of this object opening/closing

        //if hitting the Agent, reset position and report failed action
        if (other.name == "FPSController" && myObject.canReset == true)
        {
            Debug.Log(gameObject.name + " hit " + other.name + " Resetting position");
            myObject.canReset = false;
            myObject.Reset();
        }

        // If the thing your colliding with is one of your (grand)-children then don't worry about it
        if (hasAncestor(other.transform.gameObject, gameObject)) {
            return;
        }
              
        //if hitting another object that has double doors, do some checks 
		if (other.GetComponentInParent<CanOpen_Object>() && myObject.canReset == true)
        {
            if (myObject.IsInIgnoreArray(other, myObject.IgnoreTheseObjects))
            {
                //don't reset, it's cool to ignore these since some cabinets literally clip into each other if they are double doors
                return;
            }

            //oh it was something else RESET! DO IT!
            else
            {
                //check the collider hit's parent for itween instances
                //if 0, then it is not actively animating so check against it. This is needed so openable objects don't reset unless they are the active
                //object moving. Otherwise, an open cabinet hit by a drawer would cause the Drawer AND the cabinet to try and reset.
                //this should be fine since only one cabinet/drawer will be moving at a time given the Agent's action only opening on object at a time
				if (other.transform.GetComponentInParent<CanOpen_Object>().GetiTweenCount() == 0)//iTween.Count(other.transform.GetComponentInParent<CanOpen>().transform.gameObject) == 0)
                {
                    //print(other.GetComponentInParent<CanOpen>().transform.name);
                    Debug.Log(gameObject.name + " hit " + other.name + " Resetting position");
                    myObject.canReset = false;
                    myObject.Reset();
                }

            }
        }

  //      //if we hit another sim object that is NOT inside of the receptacle, then we need to reset position and not shove it.
		//if(other.GetComponentInParent<SimObjPhysics>())
		//{

		//	if(myObject.GetComponent<SimObjPhysics>().Type == SimObjType.Cabinet)
		//	{
		//		myObject.canReset = false;
  //              myObject.Reset();
		//	}
			
		//	//if(other.GetComponentInParent<SimObjPhysics>() && myObject.canReset == true)
		//	//{
		//	//	List<GameObject> contained = new List<GameObject>(myObject.GetComponent<SimObjPhysics>().Contains_GameObject());
		//	//	//get list of game objects that this object contains
		//	//	//compare if this other object his is in that list, if not then reset, if it is ignore

		//	//	if (!contained.Contains(other.GetComponentInParent<SimObjPhysics>().gameObject))
		//	//	{

		//	//		myObject.canReset = false;
		//	//		myObject.Reset();
		//	//	}

		//	//	else
		//	//		return;
		//	//}

		//}
    }

    public void OnTriggerExit(Collider other)
    {
		if (other.name == "FPSController" || other.GetComponentInParent<CanOpen_Object>() || other.GetComponentInParent<SimObjPhysics>())
        {
            myObject.canReset = true;
        }
    }
}
