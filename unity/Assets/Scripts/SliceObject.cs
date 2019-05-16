using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

public class SliceObject : MonoBehaviour
{
    //prefab that this object should change to when "sliced"
    [Header("Object To Change To")]
	[SerializeField]
	public GameObject ObjectToChangeTo;

    //private bool quit = false; //used to track when application is quitting

    [SerializeField]
    protected bool isSliced = false;

    public bool IsSliced()
    {
        return isSliced;
    }

    void OnEnable ()
    {
		//debug check for missing property
        if (!gameObject.GetComponent<SimObjPhysics>().DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanBeSliced))
        {
            Debug.LogError(gameObject.transform.name + " is missing the Secondary Property CanBeSliced!");
        }

        if(ObjectToChangeTo == null)
        {
            Debug.LogError(gameObject.transform.name + " is missing Object To Change To!");
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //action to be called from PhysicsRemoteFPSAgentController
    public void Slice()
    {
        //Destroy(gameObject);

        //Disable this game object and spawn in the broken pieces
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.isKinematic = true;

        //turn off everything except the top object, so we can continue to report back isSliced meta info without the object being "active"
        foreach(Transform t in gameObject.transform)
        {
            t.gameObject.SetActive(false);
        }

        GameObject resultObject = Instantiate(ObjectToChangeTo, transform.position, transform.rotation);
        isSliced = true;

        PhysicsSceneManager psm = GameObject.Find("PhysicsSceneManager").GetComponent<PhysicsSceneManager>();
        if (psm != null) 
        {
            //if the spawned object is not a sim object itself, but if it's holding a ton of sim objects let's go
            if(!resultObject.transform.GetComponent<SimObjPhysics>())
            {
                //each instantiated sliced version of the object is a bunch of sim objects held by a master parent transform, so go into each one and assign the id to each based on the parent's id so 
                //there is an association with the original source object
                int count = 0;
                foreach (Transform t in resultObject.transform)
                {
                    SimObjPhysics tsop = t.GetComponent<SimObjPhysics>();
                    psm.Generate_InheritedUniqueID(gameObject.GetComponent<SimObjPhysics>(), tsop, count);
                    count++;

                    //also turn on the kinematics of this object
                    Rigidbody trb = t.GetComponent<Rigidbody>();
                    trb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                    trb.isKinematic = false;
                }
            }

            else
            {
                //quick if the result object is an egg hard set it's rotation because EGGS ARE WEIRD and are not the same form as their shelled version
                if(resultObject.GetComponent<SimObjPhysics>().Type == SimObjType.EggCracked)
                {
                    resultObject.transform.rotation = Quaternion.Euler(Vector3.zero);
                }

                SimObjPhysics resultsop = resultObject.GetComponent<SimObjPhysics>();
                psm.Generate_InheritedUniqueID(gameObject.GetComponent<SimObjPhysics>(), resultsop, 0);

                Rigidbody resultrb = resultsop.GetComponent<Rigidbody>();
                resultrb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                resultrb.isKinematic = false;
            }

        }
    }

    // void OnApplicationQuit()
    // {
    //     //quit = true;
    // }

    // void OnDestroy()
    // {
    //     // //don't do this when the application is quitting, because it throws null reference errors looking for the PhysicsSceneManager
    //     // if(!quit)
    //     // {
    //     //     PhysicsSceneManager psm = GameObject.Find("PhysicsSceneManager").GetComponent<PhysicsSceneManager>();
    //     //     if (psm != null) {
    //     //         psm.SetupScene();
    //     //         psm.RemoveFromSpawnedObjects(gameObject.GetComponent<SimObjPhysics>());
    //     //         psm.RemoveFromRequiredObjects(gameObject.GetComponent<SimObjPhysics>());
    //     //     }
    //     // }
    // }
}
