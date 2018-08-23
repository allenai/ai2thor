using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanOpen_Fridge : MonoBehaviour 
{
	[Header("Doors for this Fridge")]
	[SerializeField]
	GameObject[] Doors;
    
	[Header("Animation Parameters")]
	[SerializeField]
    protected Vector3[] openPositions;

    [SerializeField]
    protected Vector3[] closedPositions;

    [SerializeField]
    protected float animationTime = 1.0f;

    [SerializeField]
    protected float openPercentage = 1.0f; //0.0 to 1.0 - percent of openPosition the object opens. 

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
    
	// Use this for initialization
	void Start () 
	{
		//init Itween in all doors to prep for animation
		if(Doors != null)
		{
			foreach (GameObject go in Doors)
            {
                iTween.Init(go);

				//check to make sure all doors have a Fridge_Door.cs script on them, if not throw a warning
				//if (!go.GetComponent<Fridge_Door>())
					//Debug.Log("Fridge Door is missing Fridge_Door.cs component! OH NO!");
            }
		}

	}
	
	// Update is called once per frame
	void Update () 
	{
		//test if it can open without Agent Command - Debug Purposes
        #if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            Interact();
        }
        #endif
	}

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
			for (int i = 0; i < Doors.Length; i++)
			{
				iTween.RotateTo(Doors[i],iTween.Hash("rotation", closedPositions[i], "islocal", true));
			}
        }

        //oh it's closed? let's open it
        else
        {
			for (int i = 0; i < Doors.Length; i++)
            {
				iTween.RotateTo(Doors[i], iTween.Hash("rotation", openPositions[i] * openPercentage, "islocal", true));            
			}
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
		//the number of iTween instances running on all doors managed by this fridge
		int count = 0;

		foreach (GameObject go in Doors)
        {
			count += iTween.Count(go);
        }
		return count;//iTween.Count(this.transform.gameObject);
    }

    public void Reset()
    {
        if (!canReset)
            Interact();
    }
}
