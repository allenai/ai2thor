using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//serialized class so it can show up in Inspector Window
[System.Serializable]
public class SwapObjList
{
	//reference to game object that needs to have materials changed
	[Header("Object That Needs Mat Swaps")]
	[SerializeField]
	public GameObject MyObject;

	//copy the Materials array on MyObject's Renderer component here
	[Header("Materials for On state")]
	[SerializeField]
	public Material[] OnMaterials;

	//swap to this array of materials when off, usually just one or two materials will change
	[Header("Materials for Off state")]
	[SerializeField]
	public Material[] OffMaterials;

}

public class CanToggleOnOff : MonoBehaviour 
{
	//the array of moving parts and lightsources will correspond with each other based on their 
	//position in the array
	[Header("Moving Parts for this Object")]
	[SerializeField]
	public GameObject[] MovingParts;

	[Header("Objects that need Mat Swaps")]
	[SerializeField]
	public SwapObjList[] MaterialSwapObjects;

	//toggle these on and off based on isOn
	[Header("Light Source Objects")]
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
    
#endif

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
		if (Input.GetKeyDown(KeyCode.Minus))
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
							"position", OffPositions[i],
							"islocal", true,
							"time", animationTime,
							"easetype", "linear", "onComplete", "setisOn", "onCompleteTarget", gameObject));
						}

						else
						iTween.MoveTo(MovingParts[i], iTween.Hash(
						"position", OffPositions[i],
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
							"position", OnPositions[i],
							"islocal", true,
							"time", animationTime,
							"easetype", "linear", "onComplete", "setisOn", "onCompleteTarget", gameObject));
						}

						else
						iTween.MoveTo(MovingParts[i], iTween.Hash(
						"position", OnPositions[i],
						"islocal", true,
						"time", animationTime,
						"easetype", "linear")); 
					}
				}
			}
		}
	}

	//toggle isOn variable, swap Materials and enable/disable Light sources
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

			if(MaterialSwapObjects.Length > 0)
			{
				for(int i = 0; i < MaterialSwapObjects.Length; i++)
				{
					MaterialSwapObjects[i].MyObject.GetComponent<MeshRenderer>().materials =
					MaterialSwapObjects[i].OffMaterials;
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
				}
			}

			if(MaterialSwapObjects.Length > 0)
			{
				for(int i = 0; i < MaterialSwapObjects.Length; i++)
				{
					MaterialSwapObjects[i].MyObject.GetComponent<MeshRenderer>().materials =
					MaterialSwapObjects[i].OnMaterials;
				}
			}

			isOn = true;
		}
	}

	// [ContextMenu("Get On-Off Materials")]
	// void ContextOnOffMaterials()
	// {
	// 	foreach (SwapObjList swap in MaterialSwapObjects)
	// 	{
	// 		List<Material> list = 
	// 		new List<Material>(swap.MyObject.GetComponent<MeshRenderer>().sharedMaterials);

	// 		//print(swap.MyObject.name);
	// 		Material[] objectMats = list.ToArray();//swap.MyObject.GetComponent<MeshRenderer>().sharedMaterials;

	// 		// foreach (Material m in objectMats)
	// 		// {
	// 		// 	//print(m.name);
	// 		// }

	// 		swap.OnMaterials = objectMats;
	// 		swap.OffMaterials = objectMats;
	// 	}
	// }
}
