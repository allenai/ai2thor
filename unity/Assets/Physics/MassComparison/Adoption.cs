using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//because it's not full parenting, eh? eh? it's a pun!

public class Adoption : MonoBehaviour 
{
	public GameObject adoptedParent;


	// Use this for initialization
	void Start () 
	{
		
	}
	
	// Update is called once per frame
	void Update () 
	{
		gameObject.transform.position = adoptedParent.transform.position;
	}
}
