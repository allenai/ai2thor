using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanToggleOnOff : MonoBehaviour 
{
	//the array of moving parts and lightsources will correspond with each other based on their 
	//position in the array
	[Header("Moving Parts for this Object")]
	[SerializeField]
	public GameObject[] MovingParts;

	[Header("Light Sources")]
	[SerializeField]
	public GameObject[] LightSources;

	[Header("Animation Parameters")]
	[SerializeField]
    public Vector3[] OnPositions;

    [SerializeField]
    public Vector3[] OffPositions;

    //[SerializeField]
    public float animationTime = 0.05f;

	//use this to set the default state of this object. Lightswitches should be default on, things like
	//microwaves should be default off etc.
	[SerializeField]
    public bool isOn = true;

	protected enum MovementType { Slide, Rotate };

	[SerializeField]
    protected MovementType movementType;

	public bool isTurnedOnOrOff()
	{
		return isOn;
	}

	// Use this for initialization
	void Start () 
	{
		if(MovingParts != null)
		{
			foreach (GameObject go in MovingParts)
            {
                iTween.Init(go);
            }
		}
	}
	
	// Update is called once per frame
	void Update () 
	{
		//test if it can open without Agent Command - Debug Purposes
        #if UNITY_EDITOR
		if (Input.GetKeyDown(KeyCode.Equals))
        {
            Toggle();
        }
        #endif
	}

	public void Toggle()
	{
		//check if there are moving parts
		//check if there are lights/materials etc to swap out
		if(isOn)
		{
			if(MovingParts.Length >0)
			{
				for(int i = 0; i < MovingParts.Length; i++)
				{
					if(movementType == MovementType.Rotate)
					{
						if(i == MovingParts.Length -1)
						{
							iTween.RotateTo(MovingParts[i], iTween.Hash(
							"rotation", OffPositions[i],
							"islocal", true,
							"time", animationTime,
							"easetype", "linear", "onComplete", "setisOn", "onCompleteTarget", gameObject));
						}

						else
						iTween.RotateTo(MovingParts[i], iTween.Hash(
						"rotation", OffPositions[i],
						"islocal", true,
						"time", animationTime,
						"easetype", "linear")); 
					}

					if(movementType == MovementType.Slide)
					{
						if(i == MovingParts.Length -1)
						{
							iTween.MoveTo(MovingParts[i], iTween.Hash(
							"rotation", OffPositions[i],
							"islocal", true,
							"time", animationTime,
							"easetype", "linear", "onComplete", "setisOn", "onCompleteTarget", gameObject));
						}

						else
						iTween.MoveTo(MovingParts[i], iTween.Hash(
						"rotation", OffPositions[i],
						"islocal", true,
						"time", animationTime,
						"easetype", "linear")); 
					}
				}
			}
		}

		else
		{
			if(MovingParts.Length >0)
			{
				for(int i = 0; i < MovingParts.Length; i++)
				{
					if(movementType == MovementType.Rotate)
					{
						if(i == MovingParts.Length -1)
						{
							iTween.RotateTo(MovingParts[i], iTween.Hash(
							"rotation", OnPositions[i],
							"islocal", true,
							"time", animationTime,
							"easetype", "linear", "onComplete", "setisOn", "onCompleteTarget", gameObject));
						}

						else
						iTween.RotateTo(MovingParts[i], iTween.Hash(
						"rotation", OnPositions[i],
						"islocal", true,
						"time", animationTime,
						"easetype", "linear")); 
					}

					if(movementType == MovementType.Slide)
					{
						if(i == MovingParts.Length -1)
						{
							iTween.MoveTo(MovingParts[i], iTween.Hash(
							"rotation", OnPositions[i],
							"islocal", true,
							"time", animationTime,
							"easetype", "linear", "onComplete", "setisOn", "onCompleteTarget", gameObject));
						}

						else
						iTween.MoveTo(MovingParts[i], iTween.Hash(
						"rotation", OnPositions[i],
						"islocal", true,
						"time", animationTime,
						"easetype", "linear")); 
					}
				}
			}
		}

		
	}

	private void setisOn()
	{
		//if isOn true, set it to false and also turn off all lights/deactivate materials
		if(isOn)
		{
			if(LightSources.Length>0)
			{
				for(int i = 0; i < LightSources.Length; i++)
				{
					LightSources[i].SetActive(false);
				}
			}
			isOn = false;
		}

		//if isOn false, set to true and then turn ON all lights and activate material swaps
		else
		{
			if(LightSources.Length>0)
			{
				for(int i = 0; i < LightSources.Length; i++)
				{
					LightSources[i].SetActive(true);
					print("turn the lights on?");
				}
			}
			isOn = true;
		}
	}
}
