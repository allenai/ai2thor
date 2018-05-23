using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugInputField : MonoBehaviour 
{

	// Use this for initialization
	void Start () 
	{
		//UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(gameObject);
	}
	
	// Update is called once per frame
	void Update () 
	{
		
	}

    public void Execute(string command)
	{
		print(command);

	}
}
