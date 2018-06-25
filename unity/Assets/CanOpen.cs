using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanOpen : MonoBehaviour 
{
    public Vector3 openPosition;     public Vector3 closedPosition;      public float animationTime;     // Use this for initialization     public bool isOpen = false;     private Hashtable iTweenArgs;

	private bool canReset = true;

	//[SerializeField] TriggerColliderCheck CabinetTrigger = null;      private enum MovementType { Slide, Rotate } ;      [SerializeField] private MovementType movementType;      void Start ()      {         iTweenArgs = iTween.Hash();         iTweenArgs.Add("position", openPosition);         iTweenArgs.Add("time", animationTime);         iTweenArgs.Add("islocal", true);                   if(isOpen)         {             iTweenArgs["position"] = openPosition;             iTweenArgs["time"] = 0f;             iTween.MoveTo(gameObject, iTweenArgs);             iTweenArgs["time"] = animationTime;          }     }      // Update is called once per frame     void Update ()      {         if(Input.GetKeyDown(KeyCode.E))         { 			Interact();         }     }

    public void Interact()
	{
		if (isOpen)
        {
            iTweenArgs["position"] = closedPosition;
            iTweenArgs["rotation"] = closedPosition;            
        }

        else
        {
            iTweenArgs["position"] = openPosition;
            iTweenArgs["rotation"] = openPosition;            
        }

        isOpen = !isOpen;

        switch(movementType)
        {
            case MovementType.Slide:
                iTween.MoveTo(gameObject, iTweenArgs);               
                break;

            case MovementType.Rotate:
                iTween.RotateTo(gameObject, iTweenArgs);               
                break;
        }   
	}

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
