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

	//these are any switches, dials, levers, etc that should change position or orientation when toggle on or off
	[Header("Moving Parts (switches, dials, etc")]
	[SerializeField]
	public GameObject[] MovingParts;

	//Meshes that require different materials when in on/off state
	[Header("Objects that need Mat Swaps")]
	[SerializeField]
	public SwapObjList[] MaterialSwapObjects;

	//Light emitting objects that must be toggled enabled/disabled. Can also be used for non-Light objects
	[Header("Light Source Objects/Objects to Enable or Disable")]
	[SerializeField]
	public GameObject[] LightSources;

	[Header("Animation Parameters")]
	
	//rotations or translations for the MovingParts when On
	[SerializeField]
    public Vector3[] OnPositions;

	//rotations or translations for the MovingParts when off
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

	//keep a list of all objects that can turn on, but must be in the closed state before turning on (ie: can't microwave an object with the door open!)
	protected List<SimObjType> MustBeClosedToTurnOn = new List<SimObjType>()
	{SimObjType.Microwave};

	//if this object controls the on/off state of ONLY itself, set to true (lamps, laptops, etc.)
	//if this object's on/off state is not controlled by itself, but instead controlled by another sim object (ex: stove burner is controlled by the stove knob) set this to false
	[SerializeField]
	protected bool SelfControlled = true;

	//reference to any sim objects that this object will turn on/off by proxy (ie: stove burner knob will toggle on/off state of its stove burner)
	[SerializeField]
	protected SimObjPhysics[] ControlledSimObjects;

	//return this for metadata check to see if this object is Toggleable or not
	//specifically, stove burners should not be Toggleable, but they can return 'isToggled' because they can be Toggled on by
	//another sim object, the stove knob.
	//stove knob: returns toggleable, returns istoggled
	//stove burner: only returns istoggled
	public bool ReturnSelfControlled()
	{
		return SelfControlled;
	}

	//returns references to all sim objects this object toggles the on/off state of. For example all stove knobs can
	//return which burner they control with this
	public SimObjPhysics[] ReturnControlledSimObjects()
	{
		return ControlledSimObjects;
	}

	public List<SimObjType> ReturnMustBeClosedToTurnOn()
	{
		return MustBeClosedToTurnOn;
	}

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

		#if UNITY_EDITOR
		if(!this.GetComponent<SimObjPhysics>().DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanToggleOnOff))
		{
			Debug.LogError(this.name + "is missing the CanToggleOnOff Secondary Property! Please set it!");
		}
		#endif
	}
	
	// Update is called once per frame
	void Update () 
	{
		// //test if it can open without Agent Command - Debug Purposes
        // #if UNITY_EDITOR
		// if (Input.GetKeyDown(KeyCode.Minus))
        // {
        //     Toggle();
        // }
        // #endif
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
    
	public void Toggle()
	{
		//if this object is controlled by another object, do nothing and report failure?
		if(!SelfControlled)
		return;

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

			//if no moving parts, then only materials and lights need to be swapped
			else
			{
				setisOn();
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

			//if no moving parts, then only materials and lights need to be toggled
			else
			{
				setisOn();
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

			//also set any objects this object controlls to the off state
			if(ControlledSimObjects.Length >0)
			{
				foreach (SimObjPhysics sop in ControlledSimObjects)
				{
					sop.GetComponent<CanToggleOnOff>().isOn = false;
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

			//also set any objects this object controlls to the on state
			if(ControlledSimObjects.Length >0)
			{
				foreach (SimObjPhysics sop in ControlledSimObjects)
				{
					sop.GetComponent<CanToggleOnOff>().isOn = true;
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
