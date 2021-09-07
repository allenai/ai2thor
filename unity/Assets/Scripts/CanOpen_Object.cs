﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

//controls opening doors on a fridge. Because the fridge base body and door should be considered a single
//sim object, this has mimicked functionality from CanOpen.cs but specialized for a Fridge.
using System.Linq;
using System;
public class CanOpen_Object : MonoBehaviour 
{
	[Header("Moving Parts for this Object")]
	[SerializeField]
	public GameObject[] MovingParts;
    
	[Header("Animation Parameters")]
	[SerializeField]
    public Vector3[] openPositions;

    [SerializeField]
    public Vector3[] closedPositions;

    //[SerializeField]
    public float animationTime = 0.2f;

    [SerializeField]
    public float currentOpenPercentage = 0f; //0.0 to 1.0 - percent of openPosition the object opens. 

	[Header("Objects To Ignore Collision With - For Cabinets/Drawers with hinges too close together")]
    //these are objects to ignore collision with. This is in case the fridge doors touch each other or something that might
    //prevent them from closing all the way. Probably not needed but it's here if there is an edge case
    [SerializeField]
    public GameObject[] IgnoreTheseObjects;

	[Header("State information bools")]
	[SerializeField]
    public bool isOpen = false;

	[SerializeField]
    public bool canReset = true;

	protected enum MovementType { Slide, Rotate, ScaleX, ScaleY, ScaleZ};

	[SerializeField]
    protected MovementType movementType;

	//keep a list of all objects that, if able to turn on/off, must be in the Off state before opening (no opening microwave unless it's off!);
	private List<SimObjType> MustBeOffToOpen = new List<SimObjType>()
	{SimObjType.Microwave};

    //these objects, when hitting another sim object, should reset their state because it would cause clipping. Specifically used
    //for things like Laptops or Books that can open but are also pickupable and moveable. This should not include static
    //things in the scene like cabinets or drawers that have fixed positions
    // private List<SimObjType> ResetPositionIfPickupableAndOpenable = new List<SimObjType>()
    // {SimObjType.Book, SimObjType.Laptop};


    [Header("References for the Open or Closed bounding box for openable and pickupable objects")]
    //the bounding box to use when this object is in the open state
    [SerializeField]
    protected GameObject OpenBoundingBox;

    //the bounding box to use when this object is in the closed state
    [SerializeField]
    protected GameObject ClosedBoundingBox;

    public float isOpenByPercentage = 0;

    #if UNITY_EDITOR
    void OnEnable ()
    {
		//debug check for missing CanOpen property
        if (!gameObject.GetComponent<SimObjPhysics>().DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanOpen))
        {
            Debug.LogError(gameObject.transform.name + " is missing the Secondary Property CanOpen!");
        }
    }
    #endif

    public List<SimObjType> WhatReceptaclesMustBeOffToOpen()
    {
        return MustBeOffToOpen;
    }

	// Use this for initialization
	void Awake () 
	{
		//init Itween in all doors to prep for animation
		if(MovingParts != null)
		{
			foreach (GameObject go in MovingParts)
            {
                iTween.Init(go);

				//check to make sure all doors have a Fridge_Door.cs script on them, if not throw a warning
				//if (!go.GetComponent<Fridge_Door>())
					//Debug.Log("Fridge Door is missing Fridge_Door.cs component! OH NO!");
            }
		}

		#if UNITY_EDITOR
		if(!this.GetComponent<SimObjPhysics>().DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanOpen))
		{
			Debug.LogError(this.name + "is missing the CanOpen Secondary Property! Please set it!");
		}
		#endif

        if(!isOpen)
        currentOpenPercentage = 0.0f;
	}

	// Update is called once per frame
	void Update () 
	{
		//test if it can open without Agent Command - Debug Purposes
        #if UNITY_EDITOR
		if (Input.GetKeyDown(KeyCode.Equals))
        {
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

    public void SetMovementToScaleX()
    {
        movementType = MovementType.ScaleX;
    }

    public void SetMovementToScaleY()
    {
        movementType = MovementType.ScaleY;
    }

    public void SetMovementToScaleZ()
    {
        movementType = MovementType.ScaleZ;
    }   
#endif

    public bool SetOpenPercent(float val)
    {
        if (val >= 0.0 && val <= 1.0)
        {
            //print(val);
            currentOpenPercentage = val;
            return true;
        }
        else
        {
            return false;
        }
    }

    private float CalculateValueByPercentage(float previous, float next, float percentage) {
        return (previous < next ? (previous + ((next - previous) * percentage)) :
            (previous - ((previous - next) * percentage)));
    }

    public bool IsMovementTypeSlide() {
        return movementType == MovementType.Slide;
    }

    public void Interact()
    {

        //if this object is pickupable AND it's trying to open (book, box, laptop, etc)
        //before trying to open or close, these objects must have kinematic = false otherwise it might clip through other objects
        SimObjPhysics sop = gameObject.GetComponent<SimObjPhysics>();
        if(sop.PrimaryProperty == SimObjPrimaryProperty.CanPickup && sop.isInAgentHand == false)
        {
            gameObject.GetComponent<Rigidbody>().isKinematic = false;
        }

        bool isOpening = (this.currentOpenPercentage > this.isOpenByPercentage);
        float percentage = isOpening ? this.currentOpenPercentage : (1 - this.currentOpenPercentage);

        // Update the global variables BEFORE running the loop.
        this.isOpenByPercentage = this.currentOpenPercentage;
        this.isOpen = (this.isOpenByPercentage > 0);

		for (int i = 0; i < MovingParts.Length; i++)
		{
            Vector3 previous = isOpening ? closedPositions[i] : openPositions[i];
            Vector3 next = isOpening ? openPositions[i] : closedPositions[i];

            if (animationTime == 0) {
                if (movementType == MovementType.Rotate) {
                    MovingParts[i].transform.localRotation = Quaternion.Euler(
                        this.CalculateValueByPercentage(previous.x, next.x, percentage),
                        this.CalculateValueByPercentage(previous.y, next.y, percentage),
                        this.CalculateValueByPercentage(previous.z, next.z, percentage)
                    );
                }

                else if (movementType == MovementType.Slide) {
                    MovingParts[i].transform.localPosition = new Vector3(
                        this.CalculateValueByPercentage(previous.x, next.x, percentage),
                        this.CalculateValueByPercentage(previous.y, next.y, percentage),
                        this.CalculateValueByPercentage(previous.z, next.z, percentage)
                    );
                }

                else if (movementType == MovementType.ScaleX || movementType == MovementType.ScaleY ||
                    movementType == MovementType.ScaleZ) {

                    MovingParts[i].transform.localScale = new Vector3(
                        movementType != MovementType.ScaleX ? next.x :
                            this.CalculateValueByPercentage(previous.x, next.x, percentage),
                        movementType != MovementType.ScaleY ? next.y :
                            this.CalculateValueByPercentage(previous.y, next.y, percentage),
                        movementType != MovementType.ScaleZ ? next.z :
                            this.CalculateValueByPercentage(previous.z, next.z, percentage)
                    );
                }

                if (i == MovingParts.Length - 1) {
                    UpdateOpenOrCloseBoundingBox();
                }

                continue;
            }

            System.Object[] args = new System.Object[] {
                "islocal",
                true,
                "time",
                animationTime,
                "easetype",
                "linear"
            };

            if (i == MovingParts.Length - 1) {
                args = args.Concat(new System.Object[] {
                    "onComplete",
                    "UpdateOpenOrCloseBoundingBox",
                    "onCompleteTarget",
                    gameObject
                }).ToArray();
            }

			if(movementType == MovementType.Rotate)
			{
                iTween.RotateTo(MovingParts[i], iTween.Hash(new System.Object[] {
                    Quaternion.Euler(
                        this.CalculateValueByPercentage(previous.x, next.x, percentage),
                        this.CalculateValueByPercentage(previous.y, next.y, percentage),
                        this.CalculateValueByPercentage(previous.z, next.z, percentage)
                    ),
                    "rotation"
                }.Concat(args).ToArray()));
			}

			else if(movementType == MovementType.Slide)
			{
                iTween.MoveTo(MovingParts[i], iTween.Hash(new System.Object[] {
                    new Vector3(
                        this.CalculateValueByPercentage(previous.x, next.x, percentage),
                        this.CalculateValueByPercentage(previous.y, next.y, percentage),
                        this.CalculateValueByPercentage(previous.z, next.z, percentage)
                    ),
                    "position"
                }.Concat(args).ToArray()));
			}

            else if (movementType == MovementType.ScaleX || movementType == MovementType.ScaleY ||
                movementType == MovementType.ScaleZ) {

                iTween.ScaleTo(MovingParts[i], iTween.Hash(new System.Object[] {
                    new Vector3(
                        movementType != MovementType.ScaleX ? next.x :
                            this.CalculateValueByPercentage(previous.x, next.x, percentage),
                        movementType != MovementType.ScaleY ? next.y :
                            this.CalculateValueByPercentage(previous.y, next.y, percentage),
                        movementType != MovementType.ScaleZ ? next.z :
                            this.CalculateValueByPercentage(previous.z, next.z, percentage)
                    ),
                    "scale"
                }.Concat(args).ToArray()));
            }
        }
    }

    private void UpdateOpenOrCloseBoundingBox()
    {
        //some things that open and close don't need to switch bounding boxes- drawers for example, only things like
        //cabinets that are not self contained need to switch between open/close bounding box references (ie: books, cabinets, microwave, etc)
        if(OpenBoundingBox == null || ClosedBoundingBox == null)
        {
            return;
        }

        SimObjPhysics sop = gameObject.GetComponent<SimObjPhysics>();

        if(isOpen)
        {
            sop.BoundingBox = OpenBoundingBox;
        }

        else
        {
            sop.BoundingBox = ClosedBoundingBox;
        }
    }

    // public float GetOpenPercent()
    // {
    //     //if open, return the percent it is open
    //     if (isOpen)
    //     {
    //         return openPercentage;
    //     }

    //     //we are closed, so I guess it's 0% open?
    //     else
    //         return 0.0f;
    // }

    public bool GetisOpen()
    {
        return isOpen;
    }

    //for use in OnTriggerEnter ignore check
    //return true if it should ignore the object hit. Return false to cause this object to reset to the original position
    public bool IsInIgnoreArray(Collider other, GameObject[] arrayOfCol)
    {
        for (int i = 0; i < arrayOfCol.Length; i++)
        {
            if(other.GetComponentInParent<CanOpen_Object>().transform)
            {
                if (other.GetComponentInParent<CanOpen_Object>().transform ==
                    arrayOfCol[i].transform)
                    return true;
            }

            else
            return true;
        }
        return false;
    }

    public int GetiTweenCount()
    {
		//the number of iTween instances running on all doors managed by this fridge
		int count = 0;

		foreach (GameObject go in MovingParts)
        {
			count += iTween.Count(go);
        }
		return count;//iTween.Count(this.transform.gameObject);
    }

    //note: reset can interrupt the Interact() itween call because
    //it will start a new set of tweens before onComplete is called from Interact()... it seems
    public void Reset()
    {
        //print("inside reset");
        if (!canReset)
		{
			//Interact();

            //print("we are calling Reset() now");
            //we are still open, trying to close, but hit something - reset to open
			if(isOpen)
			{
				for (int i = 0; i < MovingParts.Length; i++)
                {
                    if (movementType == MovementType.Rotate)
                    {
						iTween.RotateTo(MovingParts[i], iTween.Hash(
                        "rotation", openPositions[i] * currentOpenPercentage,
                        "islocal", true,
                        "time", animationTime,
                        "easetype", "linear"));

                    }


                    else if (movementType == MovementType.Slide)
                    {
						iTween.MoveTo(MovingParts[i], iTween.Hash(
                        "position", openPositions[i] * currentOpenPercentage,
                        "islocal", true,
                        "time", animationTime,
                        "easetype", "linear"));

                    }

                    else if (movementType == MovementType.ScaleY)
                    {
                        iTween.ScaleTo(MovingParts[i], iTween.Hash(
                        "scale", new Vector3(openPositions[i].x, closedPositions[i].y + (openPositions[i].y - closedPositions[i].y) * currentOpenPercentage, openPositions[i].z),
                        "islocal", true,
                        "time", animationTime,
                        "easetype", "linear"));
                    }

                }
			}

			else
			{
				for (int i = 0; i < MovingParts.Length; i++)
                {
                    if (movementType == MovementType.Rotate)
                    {
						iTween.RotateTo(MovingParts[i], iTween.Hash(
                        "rotation", closedPositions[i],
                        "islocal", true,
                        "time", animationTime,
                        "easetype", "linear"));

                    }


                    else if (movementType == MovementType.Slide)
                    {
						iTween.MoveTo(MovingParts[i], iTween.Hash(
                        "position", closedPositions[i],
                        "islocal", true,
                        "time", animationTime,
                        "easetype", "linear"));
                    }


                    else if(movementType == MovementType.ScaleY)
                    {
                        iTween.ScaleTo(MovingParts[i], iTween.Hash(
                        "scale", closedPositions[i],
                        "islocal", true,
                        "time", animationTime,
                        "easetype", "linear"));
                    }
                }
			}

            StartCoroutine("CanResetToggle");
		}
    }

	private bool hasAncestor(GameObject child, GameObject potentialAncestor)
    {
        if (child == potentialAncestor)
        {
            return true;
        }
        else if (child.transform.parent != null)
        {
            return hasAncestor(child.transform.parent.gameObject, potentialAncestor);
        }
        else
        {
            return false;
        }
    }

	public void OnTriggerEnter(Collider other)
	{
		if(other.CompareTag("Receptacle"))
        {
            return;
        }
		//note: Normally rigidbodies set to Kinematic will never call the OnTriggerX events
		//when colliding with another rigidbody that is kinematic. For some reason, if the other object
		//has a trigger collider even though THIS object only has a kinematic rigidbody, this
		//function is still called so we'll use that here:

		//The Agent has a trigger Capsule collider, and other cabinets/drawers have
		//a trigger collider, so this is used to reset the position if the agent or another
		//cabinet or drawer is in the way of this object opening/closing

		//if hitting the Agent AND not being currently held by the Agent(so things like Laptops don't constantly reset if the agent is holding them)
        //..., reset position and report failed action
        //NOTE: hitting the agent and resetting is now handled by the InteractAndWait coroutine in PhysicsRemote
		// if (other.name == "FPSController" && canReset == true && !gameObject.GetComponentInParent<PhysicsRemoteFPSAgentController>())
		// {
        //     #if UNITY_EDITOR
		// 	Debug.Log(gameObject.name + " hit " + other.name + " Resetting position");
        //     #endif
		// 	canReset = false;
		// 	Reset();
		// }

		//// If the thing your colliding with is one of your (grand)-children then don't worry about it
		if (hasAncestor(other.transform.gameObject, gameObject))
		{
			return;
		}

		//if hitting another object that has double doors, do some checks 
		if (other.GetComponentInParent<CanOpen_Object>() && canReset == true)
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
				//if 0, then it is not actively animating so check against it. This is needed so openable objects don't reset unless they are the active
				//object moving. Otherwise, an open cabinet hit by a drawer would cause the Drawer AND the cabinet to try and reset.
				//this should be fine since only one cabinet/drawer will be moving at a time given the Agent's action only opening on object at a time
				if (other.transform.GetComponentInParent<CanOpen_Object>().GetiTweenCount() == 0 
                    && other.GetComponentInParent<SimObjPhysics>().PrimaryProperty == SimObjPrimaryProperty.Static)//check this so that objects that are openable & pickupable don't prevent drawers/cabinets from animating
				{
					//print(other.GetComponentInParent<CanOpen>().transform.name);
                    #if UNITY_EDITOR
					Debug.Log(gameObject.name + " hit " + other.name + " on "+ other.GetComponentInParent<SimObjPhysics>().transform.name + " Resetting position");
                    #endif
					canReset = false;
					Reset();
				}

			}
		}
	}

    // resets the CanReset boolean once the reset tween is done. This checks for iTween instanes, once there are none this object can be used again
    IEnumerator CanResetToggle()
    {
        while(true)
        {
            if(GetiTweenCount() != 0)
            yield return new WaitForEndOfFrame();

            else
            {
                canReset = true;
                yield break;
            }
        }
    }


}
