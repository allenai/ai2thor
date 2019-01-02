using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorSetupSimObjPhysics : MonoBehaviour 
{
	public SimObjType Type;
	public SimObjPrimaryProperty Primary;
	public SimObjSecondaryProperty[] Secondary;


	// Use this for initialization
	void Start () 
	{
		
	}
	
	// Update is called once per frame
	void Update () 
	{
		
	}

	public void Mirror()
	{
		SimObjSecondaryProperty[] MirrorSecondary = new SimObjSecondaryProperty[]{SimObjSecondaryProperty.CanBeCleanedGlass};
		//Setup(SimObjType.Mirror, SimObjPrimaryProperty.Static, MirrorSecondary);
	}

	public void Setup(SimObjType type, SimObjPrimaryProperty primaryProperty, SimObjSecondaryProperty[] secondaryProperties, string tag, int layer)
	{
		//SimObjPhysics sop = gameObject.GetCompn
		//get SimObjPhysics component
		//setup type, primary and secondary properties, tag, and layer for the object
	}
}
