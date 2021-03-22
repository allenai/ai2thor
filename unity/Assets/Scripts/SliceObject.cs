using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
using UnityEngine.Rendering;
using System.IO;

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
        #if UNITY_EDITOR
		//debug check for missing property
        if (!gameObject.GetComponent<SimObjPhysics>().DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanBeSliced))
        {
            Debug.LogError(gameObject.transform.name + " is missing the Secondary Property CanBeSliced!");
        }

        if(ObjectToChangeTo == null)
        {
            Debug.LogError(gameObject.transform.name + " is missing Object To Change To!");
        }

        // //if the object can be cooked, check if CookedObjectToChangeTo is missing
        // if(gameObject.GetComponent<SimObjPhysics>().DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanBeCooked))
        // {
        //     if(CookedObjectToChangeTo == null)
        //     {
        //         Debug.LogError(gameObject.transform.name + " is missing Cooked Object To Change To!");
        //     }
        // }
        #endif
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
        //if this is already sliced, we can't slice again so yeah stop that
        if(isSliced == true)
        {
            return;
        }

        //Disable this game object and spawn in the broken pieces
        Rigidbody rb = gameObject.GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        rb.isKinematic = true;

        //turn off everything except the top object, so we can continue to report back isSliced meta info without the object being "active"
        foreach(Transform t in gameObject.transform)
        {
            t.gameObject.SetActive(false);
        }

        GameObject resultObject;

        if(!gameObject.GetComponent<SimObjPhysics>().DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.CanBeCooked))
        {
            //instantiate the normal object if this object is not cooked, otherwise....
            resultObject = Instantiate(ObjectToChangeTo, transform.position, transform.rotation);
            isSliced = true;
        }

        //if the object can be cooked, check if it is cooked and then spawn the cooked object to change to, otherwise spawn the normal object
        else
        {
            //instantiate the normal object if this object is not cooked, otherwise....
            resultObject = Instantiate(ObjectToChangeTo, transform.position, transform.rotation);
            isSliced = true;

            if(gameObject.GetComponent<CookObject>().IsCooked())
            {
                //cook all objects under the resultObject
                foreach(Transform t in resultObject.transform)
                {
                    t.GetComponent<CookObject>().Cook();
                }
            }

        }


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
                    psm.Generate_InheritedObjectID(gameObject.GetComponent<SimObjPhysics>(), tsop, count);
                    count++;

                    //also turn on the kinematics of this object
                    Rigidbody trb = t.GetComponent<Rigidbody>();
                    trb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                    trb.isKinematic = false;

                    //also add each child object's rb to the cache of all rigidbodies in scene
                    psm.AddToRBSInScene(trb);
                }
            }

            //the spawned object is a sim object itself, so make an ID for it
            else
            {
                //quick if the result object is an egg hard set it's rotation because EGGS ARE WEIRD and are not the same form as their shelled version
                if(resultObject.GetComponent<SimObjPhysics>().Type == SimObjType.EggCracked)
                {
                    resultObject.transform.rotation = Quaternion.Euler(Vector3.zero);
                }

                SimObjPhysics resultsop = resultObject.GetComponent<SimObjPhysics>();
                psm.Generate_InheritedObjectID(gameObject.GetComponent<SimObjPhysics>(), resultsop, 0);

                Rigidbody resultrb = resultsop.GetComponent<Rigidbody>();
                resultrb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                resultrb.isKinematic = false;

                //also add the spawned object's RB to the cache of all rigidbodies in scene
                psm.AddToRBSInScene(resultrb);
            }

        }

        else
        {
            Debug.LogError("Physics Scene Manager object is missing from scene!");
        }

        //if image synthesis is active, make sure to update the renderers for image synthesis since now there are new objects with renderes in the scene
        BaseFPSAgentController primaryAgent = GameObject.Find("PhysicsSceneManager").GetComponent<AgentManager>().ReturnPrimaryAgent();
        if(primaryAgent.imageSynthesis)
        {
            if(primaryAgent.imageSynthesis.enabled)
            primaryAgent.imageSynthesis.OnSceneChange();
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
