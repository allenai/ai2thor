using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CookObject : MonoBehaviour
{
    //Meshes that require different materials when in on/off state
	[Header("Objects that need Mat Swaps")]
	[SerializeField]
	public SwapObjList[] MaterialSwapObjects;

    [SerializeField]
    private bool isCooked = false;

    void Start()
    {
        #if UNITY_EDITOR
        if(!gameObject.GetComponent<SimObjPhysics>().DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanBeCooked))
        {
            Debug.LogError(gameObject.name + " is missing the CanBeCooked secondary property!");
        }
        #endif
    }

    // Update is called once per frame
    void Update()
    {
        #if UNITY_EDITOR
		if (Input.GetKeyDown(KeyCode.Minus))
        {
            Cook();
        }
        #endif
    }

    //use this to return info on if this object is toasted or not
    public bool IsCooked()
    {
        return isCooked;
    }

    //this will swap the material of this object to toasted. There is no
    //un-toast function because uh... you can't un toast bread?
    public void Cook()
    {
        if(MaterialSwapObjects.Length > 0)
        {
            for(int i = 0; i < MaterialSwapObjects.Length; i++)
            {
                MaterialSwapObjects[i].MyObject.GetComponent<MeshRenderer>().materials =
                MaterialSwapObjects[i].OnMaterials;
            }

            isCooked = true;
        }
    }
}
