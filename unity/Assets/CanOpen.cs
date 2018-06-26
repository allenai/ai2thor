using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanOpen : MonoBehaviour 
{
	[SerializeField] protected Vector3 openPosition; 	[SerializeField] protected Vector3 closedPosition;      	[SerializeField] protected float animationTime = 1.0f;

	[SerializeField] protected float openPercentage = 1.0f; //0.0 to 1.0 - percent of openPosition the object opens. 
     // drawer starts open?     public bool isOpen = false;     private Hashtable iTweenArgs;

	public bool canReset = true;

	//[SerializeField] TriggerColliderCheck CabinetTrigger = null;      private enum MovementType { Slide, Rotate } ;      [SerializeField] private MovementType movementType;      void Start ()      {
		iTween.Init(gameObject);//init itween cuase the documentation said so?
         iTweenArgs = iTween.Hash();         iTweenArgs.Add("position", openPosition);         iTweenArgs.Add("time", animationTime);         iTweenArgs.Add("islocal", true);         
        //if we want to start in open position, initialize here?         if(isOpen)         {             iTweenArgs["position"] = openPosition;             iTweenArgs["time"] = 0f;             iTween.MoveTo(gameObject, iTweenArgs);             iTweenArgs["time"] = animationTime;          }     }      // Update is called once per frame     void Update ()      {         if(Input.GetKeyDown(KeyCode.E))         { 			Interact();         }     }

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
                     
			Debug.Log("the " + gameObject.GetComponent<SimObjPhysics>().UniqueID + " is open " + openPercentage * 100 + "%");
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

    //trigger enter/exit functions reset the animation if the Agent is hit by the object opening
	public void OnTriggerEnter(Collider other)
	{
		if (other.name == "FPSController" && canReset == true)
		{
			//print("AAAAAH");
			canReset = false;
			Reset();
		}
	}

	public void OnTriggerExit(Collider other)
	{
		if(other.name == "FPSController")
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
