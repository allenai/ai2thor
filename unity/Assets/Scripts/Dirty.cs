using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dirty : MonoBehaviour
{

    [SerializeField]
    public SwapObjList[] MaterialSwapObjects; //put objects that need amterial swaps here, use OnMaterials for Dirty, OffMaterials for Clean

    [SerializeField]
    public GameObject[] ObjectsToEnableIfClean; //for things like bed sheets, decals etc. that need to toggle on and off the entire game object
    [SerializeField]
    public GameObject[] ObjectsToEnableIfDirty; 
    [SerializeField]
    protected bool isDirty = false;

    public bool IsDirty()
    {
        return isDirty;
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
        //if clean make dirty
        if(!isDirty)
        {
            //swap all material swap objects to OnMaterials
            if(MaterialSwapObjects.Length > 0)
            {
                for(int i = 0; i < MaterialSwapObjects.Length; i++)
                {
                    MaterialSwapObjects[i].MyObject.GetComponent<MeshRenderer>().materials = MaterialSwapObjects[i].OnMaterials;
                }
            }

            //disable disable all clean objects
            if(ObjectsToEnableIfClean.Length > 0)
            {
                for(int i = 0; i < ObjectsToEnableIfClean.Length; i++)
                {
                    ObjectsToEnableIfClean[i].SetActive(false);
                }
            }

            //enable all dirty objects
            if(ObjectsToEnableIfDirty.Length > 0)
            {
                for(int i = 0; i < ObjectsToEnableIfDirty.Length; i++)
                {
                    ObjectsToEnableIfDirty[i].SetActive(true);
                }
            }

            isDirty = true;
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

            //enable all clean objects
            if(ObjectsToEnableIfClean.Length > 0)
            {
                for(int i = 0; i < ObjectsToEnableIfClean.Length; i++)
                {
                    ObjectsToEnableIfClean[i].SetActive(true);
                }
            }

            //disable all dirty objects
            if(ObjectsToEnableIfDirty.Length > 0)
            {
                for(int i = 0; i < ObjectsToEnableIfDirty.Length; i++)
                {
                    ObjectsToEnableIfDirty[i].SetActive(false);
                }
            }

            isDirty = false;
        }
    }

    //similar to Fire and Candles, if touching water and this object is dirty, auto toggle to clean
    public void OnTriggerStay(Collider other)
    {
        //only clean the object if touching a running water zone (tagged Liquid). Object will not be cleaned if touching standing, still water.
        if(other.CompareTag("Liquid"))
        {
            if(isDirty)
            {
                ToggleCleanOrDirty();
            }
        }
    }
}
