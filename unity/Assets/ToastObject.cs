using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToastObject : MonoBehaviour
{
    //Meshes that require different materials when in on/off state
	[Header("Objects that need Mat Swaps")]
	[SerializeField]
	public SwapObjList[] MaterialSwapObjects;

    private bool isToasted = false;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        #if UNITY_EDITOR
		if (Input.GetKeyDown(KeyCode.Minus))
        {
            Toast();
        }
        #endif
    }

    //use this to return info on if this object is toasted or not
    public bool IsToasted()
    {
        return isToasted;
    }

    //this will swap the material of this object to toasted. There is no
    //un-toast function because uh... you can't un toast bread?
    public void Toast()
    {
        if(MaterialSwapObjects.Length > 0)
        {
            for(int i = 0; i < MaterialSwapObjects.Length; i++)
            {
                MaterialSwapObjects[i].MyObject.GetComponent<MeshRenderer>().materials =
                MaterialSwapObjects[i].OnMaterials;
            }

            isToasted = true;
        }
    }
}
