using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IgnoreCollision : MonoBehaviour
{
    public Collider[] myColliders;
    public GameObject objectToIgnoreCollisionsWith = null;

    // Start is called before the first frame update
    void Start()
    {
        SetupIgnoreCollision();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetupIgnoreCollision()
    {
        if(gameObject.GetComponentInParent<SimObjPhysics>() && myColliders.Length == 0)
        {
            myColliders = gameObject.GetComponentInParent<SimObjPhysics>().MyColliders;
        }

        Collider[] otherCollidersToIgnore = null;

        //if the object to ignore has been manually set already
        if(objectToIgnoreCollisionsWith != null)
        otherCollidersToIgnore = objectToIgnoreCollisionsWith.GetComponent<SimObjPhysics>().MyColliders;

        #if UNITY_EDITOR
        else
        Debug.LogError("IgnoreCollision on " + gameObject.transform.name + " is missing an objectToIgnoreCollisionsWith!");
        #endif
        // //otherwise, default to finding the SimObjPhysics component in the nearest parent to use as the object to ignore
        // else
        // otherCollidersToIgnore = gameObject.GetComponentInParent<SimObjPhysics>().MyColliders;

        foreach(Collider col in myColliders)
        {
            //Physics.IgnoreCollision(col 1, col2, true) with all combinations?
            foreach(Collider otherCol in otherCollidersToIgnore)
            {
                Physics.IgnoreCollision(col, otherCol);
            }
        }
    }
}
