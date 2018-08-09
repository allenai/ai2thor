using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MassScale : MonoBehaviour 
{
	[Header("Scale Parts that Move")]
	public GameObject Needle; 
	public GameObject BaseArm;

	public GameObject RightScale;
	public GameObject LeftScale;

	[Header("Lists of Objects in Each Scale")]
	public List<SimObjPhysics> RightScaleObjects = new List<SimObjPhysics>();
	private List<SimObjPhysics> RightScaleObjects_old = new List<SimObjPhysics>();

	public List<SimObjPhysics> LeftScaleObjects = new List<SimObjPhysics>();
	private List<SimObjPhysics> LeftScaleObjects_old = new List<SimObjPhysics>();

	[Header("Number of Objects in Each Scale")]
	public int RightCount = 0;
	public int LeftCount = 0;
    
	[Header("Total Mass of objects in Each Scale")]
	public float RightTotalMass = 0.0f;
	public float LeftTotalMass = 0.0f;
    
    //maximum amount the arm of the scale can rotate in either the positive or negative axis direction
    //this is different thanthe cap for the needle, which by default is + or - 90 degrees
	private float MaxAngleChangeAmount_BaseArm = 15;
	private float MaxAngleChangeAmount_Needle = 15;  

	//private Hashtable iTweenArgs;

	// Use this for initialization
	void Start () 
	{
		iTween.Init(Needle);//init itween cuase the documentation said so
		iTween.Init(BaseArm);

        //iTweenArgs = iTween.Hash();
        //iTweenArgs.Add("islocal", true);
	}

    //*****************************************
    //METADATA RETURN FUNCTIONS I GUESS
    //*****************************************

    //count of total number of sim objects in the Right scale
    public int RightScaleObjectCount()
	{
		return RightCount;
	}

    //count of total number of sim objects in the Left scale
    public int LeftScaleObjectCount()
	{
		return LeftCount;
	}

    //list of all sim objects in right scale
	public List<SimObjPhysics> ObjectsInRightScale()
	{
		return RightScaleObjects;
	}

    //list of all sim objects in the left scale
	public List<SimObjPhysics> ObjectsInLeftScale()
	{
		return LeftScaleObjects;
	}

    //list of all sim objects' unique ids in the right scale
    public List<string> ObjectsInRightScale_UniqueIDs()
	{
		List<string> Right_UniqueIDs = new List<string>();

		foreach(SimObjPhysics sop in RightScaleObjects)
		{
			Right_UniqueIDs.Add(sop.UniqueID);         
		}

		return Right_UniqueIDs;
	}
    
    //list of all sim objects' unique ids in the left scale
    public List<string> ObjectsInLeftScale_UniqueIDs()
	{
		List<string> Left_UniqueIDs = new List<string>();

        foreach (SimObjPhysics sop in LeftScaleObjects)
        {
            Left_UniqueIDs.Add(sop.UniqueID);
        }

        return Left_UniqueIDs;
	}

    //returns total mass of all objects in the Right scale combined
    public float RightScale_TotalMass()
	{
		return RightTotalMass;
	}

    //returns the total mass of all objects in the left scale combined
    public float LeftScale_TotalMass()
	{
		return LeftTotalMass;
	}

    //**********************************
    //END METADATA RETURN FUNCTIONS
    //**********************************

	// Update is called once per frame
	void Update () 
	{
		GetObjectsInScales();

		SetNeedle();

		SetArm();

		WAKEMEUPINSIDE();
	}

    public void WAKEMEUPINSIDE()
	{
		foreach(SimObjPhysics sop in RightScaleObjects)
		{
			sop.GetComponent<Rigidbody>().WakeUp();
		}

		foreach(SimObjPhysics sop in LeftScaleObjects)
		{
			sop.GetComponent<Rigidbody>().WakeUp();

		}
	}
    
	public void SetNeedle()
	{
		iTween.RotateTo(Needle, iTween.Hash("rotation", new Vector3(0, 0, NeedleAngleChange()), "islocal", true));
		//iTween.RotateTo(Needle, new Vector3(0, 0, NeedleAngleChange()), 10.0f);
	}

	//the amount of change to the needle's angle (-90 to +90)
	public float NeedleAngleChange()
	{
		float ratio = LeftTotalMass / RightTotalMass;

        //if ratio > 1, The Left side is more massive
        //if ratio < 1, the Right Side is more massive
        //if ratio is 1 exactly, both sides of scale are exactly equal! oh boy
        
        if(ratio < 1 || ratio > 1)
		{
			//angle to change the dial to
			float angleChange = 0;

			//move dial to the Right, right side is more massive
			if(ratio < 1)
			{
				//ok how much do we move it to the right? easy
				angleChange = ratio * MaxAngleChangeAmount_Needle;
				angleChange = MaxAngleChangeAmount_Needle - angleChange;
			}
            
            //move dial to the left, Left side is more massiv
            if(ratio > 1)
			{
				angleChange = -(RightTotalMass / LeftTotalMass) * MaxAngleChangeAmount_Needle;
    			angleChange = -MaxAngleChangeAmount_Needle - angleChange;
			}

			return angleChange;
		}
        
        //scale is perfectly balanced! good job!
		else
		{
			return 0;
		}
	}
    
    public void SetArm()
	{
		//BaseArm.GetComponent<Transform>().eulerAngles = new Vector3(0, 0, BaseArmAngleChange());
		iTween.RotateTo(BaseArm, iTween.Hash("rotation", new Vector3(0, 0, BaseArmAngleChange()), "islocal", true));
		//iTween.RotateTo(BaseArm, new Vector3(0,0, BaseArmAngleChange()), 10.0f);
	}

	public float BaseArmAngleChange()
	{
		float ratio = LeftTotalMass / RightTotalMass;

		if (ratio < 1 || ratio > 1)
		{
			float angleChange = 0;

            if (ratio < 1)
            {
				angleChange = ratio * MaxAngleChangeAmount_BaseArm;
                angleChange = MaxAngleChangeAmount_BaseArm - angleChange;
            }

            if (ratio > 1)
            {
                angleChange = -(RightTotalMass / LeftTotalMass) * MaxAngleChangeAmount_BaseArm;
                angleChange = -MaxAngleChangeAmount_BaseArm- angleChange;            
            }

            return angleChange;
		}
      
        else
        {
            return 0;
        }
	}
    
    //update list of objects in scales, count of objects in scales, and total mass on each scale
    public void GetObjectsInScales()
	{
		RightScaleObjects = RightScale.GetComponent<Contains>().CurrentlyContainedObjects();
		LeftScaleObjects = LeftScale.GetComponent<Contains>().CurrentlyContainedObjects();
      
		if(AreTheseTwoListsTheSame(RightScaleObjects, RightScaleObjects_old) == false)
		{
			RightTotalMass = 0;

            //add up mass of all objects currently on scale
            foreach (SimObjPhysics sop in RightScaleObjects)
            {
                RightTotalMass += sop.GetComponent<Rigidbody>().mass;
            }

            //update count of objects
			RightScaleObjects_old = new List<SimObjPhysics>(RightScaleObjects);
			RightCount = RightScaleObjects.Count;
		}

  //      //if the count of objects on this scale has changed, recalculate total mass
		//if(RightScaleObjects.Count != RightCount)
		//{
		//	//zero out the total mass
		//	RightTotalMass = 0;

  //          //add up mass of all objects currently on scale
		//	foreach (SimObjPhysics sop in RightScaleObjects)
  //          {
  //              RightTotalMass += sop.GetComponent<Rigidbody>().mass;
  //          }

  //          //update count of objects
		//	RightCount = RightScaleObjects.Count;
		//}
      
        //if the count of objects on this scale has changed, recalculate total mass
		if(AreTheseTwoListsTheSame(LeftScaleObjects, LeftScaleObjects_old) == false)
		{
			//zero out the total mass
			LeftTotalMass = 0;

            //add up mass of all objects currently on scale
			foreach (SimObjPhysics sop in LeftScaleObjects)
            {
                LeftTotalMass += sop.GetComponent<Rigidbody>().mass;
            }

			//update count of objectsz
			LeftScaleObjects_old = new List<SimObjPhysics>(LeftScaleObjects);
			LeftCount = LeftScaleObjects.Count;
		}

	}

	public bool AreTheseTwoListsTheSame(List<SimObjPhysics> l1, List<SimObjPhysics>l2)
	{
		if(l1.Count != l2.Count)
		{
			return false;
		}

		for (int i = 0; i < l1.Count; i++)
		{
			if (l1[i] != l2[i])            
				return false;
		}

		return true;

	}
}
