using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//THIS IS WINSON EXPERIMENTING WITH SETUP FUNCTIONS< DO NOT USE RIGHT NOW
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
		//SimObjSecondaryProperty[] MirrorSecondary = new SimObjSecondaryProperty[]{SimObjSecondaryProperty.CanBeCleanedGlass};
		//Setup(SimObjType.Mirror, SimObjPrimaryProperty.Static, MirrorSecondary);
	}

	public void Setup(SimObjType type, SimObjPrimaryProperty primaryProperty, SimObjSecondaryProperty[] secondaryProperties, string tag, int layer)
	{
		// SimObjPhysics sop = gameObject.GetComponent<SimObjPhysics>();

		// sop.Type = type;
		// sop.PrimaryProperty = primaryProperty;
		// sop.SecondaryProperties = secondaryProperties;
		// sop.gameObject.tag = tag;
		//sop.layer = layer;
	}
}
