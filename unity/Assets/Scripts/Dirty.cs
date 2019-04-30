using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dirty : MonoBehaviour
{

    [SerializeField]
    public SwapObjList[] MaterialSwapObjects; //put objects that need amterial swaps here, use OnMaterials for Dirty, OffMaterials for Clean

    [SerializeField]
    public GameObject[] ObjectsToEnableOrDisable; //for things like bed sheets, decals etc. that need to toggle on and off the entire game object

    protected bool isClean = true;//default to clean i guess?

    public bool IsClean()
    {
        return isClean;
    }
    // Start is called before the first frame update
    void Start()
    {
        #if UNITY_EDITOR
        if(!gameObject.GetComponent<SimObjPhysics>().DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanBeDirty))
        {
            Debug.LogError(gameObject.name + " is missing the CanBeDirty secondary property!");
        }
        #endif
    }

    // Update is called once per frame
    void Update()
    {
        // if(Input.GetKeyDown(KeyCode.G))
        // {
        //     ToggleCleanOrDirty();
        // }
    }

    public void ToggleCleanOrDirty()
    {
        //if clean, make dirt
        if(isClean)
        {
            //swap all material swap objects to OnMaterials
            if(MaterialSwapObjects.Length > 0)
            {
                for(int i = 0; i < MaterialSwapObjects.Length; i++)
                {
                    MaterialSwapObjects[i].MyObject.GetComponent<MeshRenderer>().materials = MaterialSwapObjects[i].OnMaterials;
                }
            }

            //disable all things in ObjectsToEnableOrDisable
            if(ObjectsToEnableOrDisable.Length > 0)
            {
                for(int i = 0; i < ObjectsToEnableOrDisable.Length; i++)
                {
                    ObjectsToEnableOrDisable[i].SetActive(false);
                }
            }

            isClean = false;
        }

        //if dirt, make clean
        else
        {
            //swap all material swap object to OffMaterials
            if(MaterialSwapObjects.Length > 0)
            {
                for(int i = 0; i < MaterialSwapObjects.Length; i++)
                {
                    MaterialSwapObjects[i].MyObject.GetComponent<MeshRenderer>().materials = MaterialSwapObjects[i].OffMaterials;
                }
            }
            //enable all things in ObjectsToEnableOrDisable
            if(ObjectsToEnableOrDisable.Length > 0)
            {
                for(int i = 0; i < ObjectsToEnableOrDisable.Length; i++)
                {
                    ObjectsToEnableOrDisable[i].SetActive(true);
                }
            }

            isClean = true;
        }
    }

    //similar to Fire and Candles, if touching water and this object is dirty, auto toggle to clean
    public void OnTriggerStay(Collider other)
    {
        //only clean the object if touching a running water zone (tagged Liquid). Object will not be cleaned if touching standing, still water.
        if(other.tag == "Liquid")
        {
            if(!isClean)
            {
                ToggleCleanOrDirty();
            }
        }
    }
}
