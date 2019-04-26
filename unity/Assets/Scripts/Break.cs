using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class Break : MonoBehaviour
{

    [SerializeField]
    private GameObject PrefabToSwapTo;
    protected bool readytobreak = true;

    public bool broken;//return this to metadata to report state of this object.

    //what does this object need to do when it is in the broken state? 
    //Some need a decal to show a cracked screen on the surface, others need a prefab swap to shattered pieces
    protected enum BreakType {PrefabSwap, Decal};

    [SerializeField]
    protected BreakType breakType; //please select how this object should be broken here

    // Start is called before the first frame update
    void Start()
    {
        #if UNITY_EDITOR
        if(!gameObject.GetComponent<SimObjPhysics>().DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanBreak))
        {
            Debug.LogError(gameObject.name + " is missing the CanBreak secondary property!");
        }
        #endif
    }

    // Update is called once per frame
    void Update()
    {
        // if(Input.GetKey(KeyCode.C))
        // {
        //     BreakObject();
        // }
    }

    public void BreakObject()
    {
        if(breakType == BreakType.PrefabSwap)
        {
            //Disable this game object and spawn in the broken pieces
            gameObject.GetComponent<Rigidbody>().isKinematic = true;
            foreach(Transform t in gameObject.transform)
            {
                t.gameObject.SetActive(false);
            }

            //spawn in correct prefab to swap to at object's last location and rotation
            //make sure to change to the correct variant of Prefab if the object isDirty
            
            //if gameObject.GetComponent<Dirty>() - first check to make sure if this object can become dirty
            //if object is dirty - probably get this from the "Dirty" component to keep everything nice and self contained
            //PrefabToSwapTo = DirtyPrefabToSwapTo

            Instantiate(PrefabToSwapTo, transform.position, transform.rotation);
        }

        if(breakType == BreakType.Decal)
        {
            //decal logic here
        }

    }

    void OnCollisionEnter(Collision col)
    {

        //ImpulseForce.Add(col.impulse.magnitude);
        if(col.impulse.magnitude > 4.0f && !col.transform.GetComponentInParent<PhysicsRemoteFPSAgentController>())
        {
            if(readytobreak)
            {
                readytobreak = false;
                broken = true;
                BreakObject();
            }
        }
    }
}
