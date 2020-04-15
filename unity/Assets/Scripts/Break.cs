using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class Break : MonoBehaviour
{

    [SerializeField]
    private GameObject PrefabToSwapTo;
    [SerializeField]
    private GameObject DirtyPrefabToSwapTo;

    [SerializeField]
    protected float ImpulseThreshold = 3.6f; //set this to lower if this object should be easier to break. Higher if the object requires more force to break

    [SerializeField]
    protected float HighFrictionImpulseOffset = 2.0f;//if the object is colliding with a "soft" high friction zone, offset the ImpulseThreshold to be harder to break

    protected float CurrentImpulseThreshold;//modify this with ImpulseThreshold and HighFrictionImpulseOffset based on trigger callback functions
    [SerializeField]
    protected bool readytobreak = true;

    [SerializeField]
    protected bool broken;

    //if set to true, all breakable objects cannot be broken automatically. Instaed, only the Break() action targeting specific objects will allow them to be broken.
    public bool Unbreakable = false;

    //what does this object need to do when it is in the broken state? 
    //Some need a decal to show a cracked screen on the surface, others need a prefab swap to shattered pieces
    protected enum BreakType {PrefabSwap, MaterialSwap, Decal};

    [SerializeField]
    protected BreakType breakType; //please select how this object should be broken here

    [SerializeField]
    protected SwapObjList[] MaterialSwapObjects;//swap screen/surface with cracked version

    //if these soft objects hit this breakable object, ignore the breakobject check because it's soft so yeah why would it break this object?
    private List<SimObjType> TooSmalOrSoftToBreakOtherObjects = new List<SimObjType>()
    {SimObjType.TeddyBear, SimObjType.Pillow, SimObjType.Cloth, SimObjType.Bread, SimObjType.BreadSliced, SimObjType.Egg, SimObjType.EggShell, SimObjType.Omelette,
    SimObjType.EggCracked, SimObjType.LettuceSliced, SimObjType.TissueBox, SimObjType.Newspaper, SimObjType.TissueBoxEmpty, SimObjType.TissueBoxEmpty,
    SimObjType.CreditCard, SimObjType.ToiletPaper, SimObjType.ToiletPaperRoll, SimObjType.SoapBar, SimObjType.Pen, SimObjType.Pencil, SimObjType.Towel, 
    SimObjType.Watch, SimObjType.DishSponge, SimObjType.Tissue, SimObjType.CD, SimObjType.HandTowel};

    public bool isBroken()
    {
        return broken;
    }
    // Start is called before the first frame update
    void Start()
    {
        #if UNITY_EDITOR
        if(gameObject.GetComponentInParent<SimObjPhysics>() != null && !gameObject.GetComponentInParent<SimObjPhysics>().DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanBreak))
        {
            Debug.LogError(gameObject.name + " is missing the CanBreak secondary property!");
        }

        if(gameObject.GetComponent<Dirty>())
        {
            if(DirtyPrefabToSwapTo == null)
            Debug.LogError(gameObject.name + " is missing a DirtyPrefabToSpawnTo!");
        }
        #endif

        CurrentImpulseThreshold = ImpulseThreshold;
    }

    public void BreakObject(Collision collision)
    {
        //prefab swap will switch the entire object out with a new prefab object entirely
        if(breakType == BreakType.PrefabSwap)
        {
            //Disable this game object and spawn in the broken pieces
            Rigidbody rb = gameObject.GetComponent<Rigidbody>();

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
            if(gameObject.GetComponent<Dirty>())
            {
                //if the object is not clean, swap to the dirty prefab
                if(gameObject.GetComponent<Dirty>().IsDirty())
                {
                    PrefabToSwapTo = DirtyPrefabToSwapTo;
                }
            }

            GameObject resultObject = Instantiate(PrefabToSwapTo, transform.position, transform.rotation);
            broken = true;

            // ContactPoint cp = collision.GetContact(0);
            foreach (Rigidbody subRb in resultObject.GetComponentsInChildren<Rigidbody>()) {
                subRb.velocity = rb.velocity * 0.4f;
                subRb.angularVelocity = rb.angularVelocity * 0.4f;
            }

            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rb.isKinematic = true;

            //if this object breaking is an egg, set rotation for the EggCracked object
            //quick if the result object is an egg hard set it's rotation because EGGS ARE WEIRD and are not the same form as their shelled version
            if(resultObject.GetComponent<SimObjPhysics>())
            {
                if(resultObject.GetComponent<SimObjPhysics>().Type == SimObjType.EggCracked)
                {
                    resultObject.transform.rotation = Quaternion.Euler(Vector3.zero);
                    PhysicsSceneManager psm = GameObject.Find("PhysicsSceneManager").GetComponent<PhysicsSceneManager>();
                    psm.Generate_InheritedObjectID(gameObject.GetComponent<SimObjPhysics>(), resultObject.GetComponent<SimObjPhysics>(), 0);

                    Rigidbody resultrb = resultObject.GetComponent<Rigidbody>();
                    resultrb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                    resultrb.isKinematic = false;
                }
            }

            //it's broken, make sure that it cant trigger this call again
            readytobreak = false;
        }

        //if decal type, do not switch out the object but instead swap materials to show cracked/broken parts
        if(breakType == BreakType.MaterialSwap)
        {
            //decal logic here
            if(MaterialSwapObjects.Length > 0)
            {
                for(int i = 0; i < MaterialSwapObjects.Length; i++)
                {
                    MaterialSwapObjects[i].MyObject.GetComponent<MeshRenderer>().materials = MaterialSwapObjects[i].OnMaterials;
                }
            }

            //if the object can be toggled on/off, if it is on, turn it off since it is now broken
            if(gameObject.GetComponent<CanToggleOnOff>())
            {
                gameObject.GetComponent<CanToggleOnOff>().isOn = false;
            }

            broken = true;
            //it's broken, make sure that it cant trigger this call again
            readytobreak = false;
        }

        if(breakType == BreakType.Decal)
        {
            //move shattered decal to location of the collision, or if there was no collision and this is being called
            //directly from the Break() action, create a default decal i guess?
            BreakForDecalType(collision);
        }

        BaseFPSAgentController primaryAgent = GameObject.Find("PhysicsSceneManager").GetComponent<AgentManager>().ReturnPrimaryAgent();
        if(primaryAgent.imageSynthesis)
        {
            if(primaryAgent.imageSynthesis.enabled)
            primaryAgent.imageSynthesis.OnSceneChange();
        }
    }

    // Override for Decal behavior
    protected virtual void BreakForDecalType(Collision collision) {

    }

    void OnCollisionEnter(Collision col)
    {

        //do nothing if this specific breakable sim objects has been set to unbreakable
        if(Unbreakable)
        {
            return;
        }
        
        //first see if the object (col) or this object is in the list of objects that are too small or too soft
        // if(TooSmalOrSoftToBreakOtherObjects.Contains(gameObject.GetComponent<SimObjPhysics>().Type))
        // {
        //     return;
        // }

        //if the other collider hit is on the list of things that shouldn't cause this object to break, return and do nothing
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
                BreakObject(col);
            }
        }
    }

    //change the ImpulseThreshold to higher if we are in a high friction zone, to simulate throwing an object at a "soft" object requiring
    //more force to break - ie: dropping mug on floor vs on a rug
    public void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("HighFriction"))
        {
            CurrentImpulseThreshold = ImpulseThreshold + HighFrictionImpulseOffset;
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if(other.CompareTag("HighFriction"))
        {
            CurrentImpulseThreshold = ImpulseThreshold;
        }
    }
}