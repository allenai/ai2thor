using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//for use with Openable Objects that do not have the "body" mesh well defined.
//Cabinets that each individual cabinet door sharing a large cabinet body for example.
//see scripts like "CanOpen_Fridge" for opening objects that have a self contained base body mesh

//please use the "FrameCollider" prefab on the TriggerColliders on this object in order to make sure this
//object remains in view even when the door of the object is out of view. This allows isolating the sim object without
//pairing it to the base body mesh, which wouldn't make sense for a large row of cabinets all sharing the same body.

public class CanOpen : MonoBehaviour 
{
	[Header("Animation Parameters")]
	[SerializeField]
	public Vector3 openPosition;

	[SerializeField]
	public Vector3 closedPosition;

	[SerializeField] 
	protected float animationTime = 1.0f;

	[SerializeField]
	protected float openPercentage = 1.0f; //0.0 to 1.0 - percent of openPosition the object opens. 

	[Header("Objects To Ignore Collision With - For Cabinets/Drawers with hinges too close together")]
    //these are objects to ignore collision with. For example, two cabinets right next to each other
    //might clip into themselves, so ignore the "reset" event in that case by putting the object to ignore in the below array
	[SerializeField] 
	public GameObject[] IgnoreTheseObjects;

	[Header("State information bools")]
	public bool isOpen = false;
	private Hashtable iTweenArgs;
	public bool canReset = true;
	protected enum MovementType { Slide, Rotate } ;

	[SerializeField]
	protected MovementType movementType;

	void Start (){
		iTween.Init(gameObject);//init itween cuase the documentation said so
 		iTweenArgs = iTween.Hash();
		iTweenArgs.Add("position", openPosition);
		iTweenArgs.Add("time", animationTime);
		iTweenArgs.Add("islocal", true);
        //if we want to start in open position, initialize here         if(isOpen)         {             iTweenArgs["position"] = openPosition;             iTweenArgs["time"] = 0f;             iTween.MoveTo(gameObject, iTweenArgs);             iTweenArgs["time"] = animationTime;          }     }      // Update is called once per frame     void Update ()      {
		//test if it can open without Agent Command - Debug Purposes
		#if UNITY_EDITOR
		if(Input.GetKeyDown(KeyCode.Alpha1)){
			Interact();
		}
		#endif
	}
    
    //Helper functions for setting up scenes, only for use in Editor
	#if UNITY_EDITOR
	public void SetMovementToSlide()
	{
		movementType = MovementType.Slide;
	}

    public void SetMovementToRotate()
	{
		movementType = MovementType.Rotate;      
	}

    public void SetClosedPosition()
	{
		closedPosition = transform.position;
	}
	#endif
   
    public void SetOpenPercent(float val)
	{
		if (val >= 0.0 && val <= 1.0)
			openPercentage = val;

		else
			Debug.Log("Please give an open percentage between 0.0f and 1.0f");
	}

    public void Interact()
	{
		//it's open? close it
		if (isOpen)
        {
            iTweenArgs["position"] = closedPosition;
            iTweenArgs["rotation"] = closedPosition;            
        }

        //open it here
        else
        {
			iTweenArgs["position"] = openPosition * openPercentage;
			iTweenArgs["rotation"] = openPosition * openPercentage;
                     
			//Debug.Log("the " + gameObject.GetComponent<SimObjPhysics>().UniqueID + " is open " + openPercentage * 100 + "%");
        }
        

        switch(movementType)
        {
            case MovementType.Slide:
                iTween.MoveTo(gameObject, iTweenArgs);
                break;

            case MovementType.Rotate:
                iTween.RotateTo(gameObject, iTweenArgs);               
                break;
        }

		isOpen = !isOpen;
		//canReset = true;
	}

    public float GetOpenPercent()
	{
		//if open, return the percent it is open
		if (isOpen)
		{
			return openPercentage;
		}

        //we are closed, so I guess it's 0% open?
		else
			return 0.0f;
	}

    public bool GetisOpen()
	{
		return isOpen;
	}

    //for use in OnTriggerEnter ignore check
	public bool IsInIgnoreArray(Collider other, GameObject[] arrayOfCol)
    {
        for (int i = 0; i < arrayOfCol.Length; i++)
        {
			if (other.GetComponentInParent<CanOpen>().transform == 
			    arrayOfCol[i].GetComponentInParent<CanOpen>().transform)
                return true;
        }
        return false;
    }

    public int GetiTweenCount()
	{
		//the number of iTween instances running on this object
		return iTween.Count(this.transform.gameObject);
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

    //trigger enter/exit functions reset the animation if the Agent is hit by the object opening
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
		if (other.name == "FPSController" && canReset == true)
		{
			Debug.Log(gameObject.name + " hit " + other.name + " Resetting position");
			canReset = false;
			Reset();
		}

		// If the thing your colliding with is one of your (grand)-children then don't worry about it
        if (hasAncestor(other.transform.gameObject, gameObject)) {
            return;
        }

        //if hitting another object that can open, do some checks 
		if(other.GetComponentInParent<CanOpen>() && canReset == true)
		{
			if (IsInIgnoreArray(other, IgnoreTheseObjects))
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
				if(other.transform.GetComponentInParent<CanOpen>().GetiTweenCount() == 0)//iTween.Count(other.transform.GetComponentInParent<CanOpen>().transform.gameObject) == 0)
				{
					//print(other.GetComponentInParent<CanOpen>().transform.name);
                    Debug.Log(gameObject.name + " hit " + other.name + " Resetting position");
                    canReset = false;
                    Reset();
				}

			}
		}
      
	}

	public void OnTriggerExit(Collider other)
	{
		if(other.name == "FPSController" || other.GetComponentInParent<CanOpen>() || other.GetComponentInParent<CanOpen_Object>())
		{
			//print("HAAAAA");
			canReset = true;
		}
	}
       
	public void Reset()
	{
		if(!canReset)
		Interact();      
	}


}
