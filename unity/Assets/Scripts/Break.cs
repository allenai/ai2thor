using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class Break : MonoBehaviour
{

    [SerializeField]
    private GameObject PrefabToSwapTo;

    [SerializeField]
    protected float ImpulseThreshold = 3.6f; //set this to lower if this object should be easier to break. Higher if the object requires more force to break

    [SerializeField]
    protected float HighFrictionImpulseOffset = 2.0f;//if the object is colliding with a "soft" high friction zone, offset the ImpulseThreshold to be harder to break

    protected float CurrentImpulseThreshold;//modify this with ImpulseThreshold and HighFrictionImpulseOffset based on trigger callback functions
    protected bool readytobreak = true;

    public bool broken;//return this to metadata to report state of this object.

    //what does this object need to do when it is in the broken state? 
    //Some need a decal to show a cracked screen on the surface, others need a prefab swap to shattered pieces
    protected enum BreakType {PrefabSwap, Decal};

    [SerializeField]
    protected BreakType breakType; //please select how this object should be broken here

    //if these soft objects hit this breakable object, ignore the breakobject check because it's soft so yeah why would it break this object?
    private List<SimObjType> TooSmalOrSoftToBreakOtherObjects = new List<SimObjType>()
    {SimObjType.TeddyBear, SimObjType.Pillow, SimObjType.Cloth, SimObjType.Bread, SimObjType.BreadSliced, SimObjType.Egg, SimObjType.EggShell, SimObjType.Omelette,
    SimObjType.EggFried, SimObjType.LettuceSliced, SimObjType.TissueBox, SimObjType.Newspaper, SimObjType.TissueBoxEmpty, SimObjType.TissueBoxEmpty,
    SimObjType.CreditCard, SimObjType.ToiletPaper, SimObjType.ToiletPaperRoll, SimObjType.SoapBar, SimObjType.Pen, SimObjType.Pencil, SimObjType.Towel, 
    SimObjType.Watch, SimObjType.DishSponge, SimObjType.Tissue, SimObjType.CD,};

    // Start is called before the first frame update
    void Start()
    {
        #if UNITY_EDITOR
        if(!gameObject.GetComponent<SimObjPhysics>().DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanBreak))
        {
            Debug.LogError(gameObject.name + " is missing the CanBreak secondary property!");
        }
        #endif

        CurrentImpulseThreshold = ImpulseThreshold;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void BreakObject()
    {
        if(breakType == BreakType.PrefabSwap)
        {
            //Disable this game object and spawn in the broken pieces
            Rigidbody rb = gameObject.GetComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rb.isKinematic = true;

            //turn off everything except the top object
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
        //first see if the object (col) or this object is in the list of objects that are too small or too soft
        if(TooSmalOrSoftToBreakOtherObjects.Contains(gameObject.GetComponent<SimObjPhysics>().Type))
        {
            return;
        }

        if(col.transform.GetComponentInParent<SimObjPhysics>())
        {
            if(TooSmalOrSoftToBreakOtherObjects.Contains(col.transform.GetComponentInParent<SimObjPhysics>().Type))
            {
                return;
            }
        }

        //ImpulseForce.Add(col.impulse.magnitude);
        if(col.impulse.magnitude > CurrentImpulseThreshold && !col.transform.GetComponentInParent<PhysicsRemoteFPSAgentController>())
        {
            if(readytobreak)
            {
                readytobreak = false;
                broken = true;
                BreakObject();
            }
        }
    }

    //change the ImpulseThreshold to higher if we are in a high friction zone, to simulate throwing an object at a "soft" object requiring
    //more force to break - ie: dropping mug on floor vs on a rug
    public void OnTriggerStay(Collider other)
    {
        if(other.gameObject.tag == "HighFriction")
        {
            CurrentImpulseThreshold = ImpulseThreshold + HighFrictionImpulseOffset;
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if(other.gameObject.tag == "HighFriction")
        {
            CurrentImpulseThreshold = ImpulseThreshold;
        }
    }
}
