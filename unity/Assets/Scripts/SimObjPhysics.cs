using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimObjPhysics : MonoBehaviour
{
    
	[SerializeField]
	public string UniqueID = string.Empty;

	[SerializeField]
    public SimObjType Type = SimObjType.Undefined;

	[SerializeField]
	public SimObjPrimaryProperty PrimaryProperty;
    
	[SerializeField]
	public SimObjSecondaryProperty[] SecondaryProperties;

	public GameObject RotateAgentCollider = null;

	//public GameObject RotateAgentHandCollider = null;

	[SerializeField]
	public Transform[] InteractionPoints = null;

	[SerializeField]
	public Transform[] VisibilityPoints = null;

	[SerializeField]
	public GameObject[] MyColliders = null;

	[SerializeField]
    public GameObject[] MyTriggerColliders = null;
   
	public bool isVisible = false;
	public bool isInteractable = false;
	public bool isColliding = false;


	//initial position object spawned in in case we want to reset the scene
	//private Vector3 startPosition;   

	// Use this for initialization
	void Start()
	{
		//Generate_UniqueID();
		//startPosition = transform.position;

        //maybe we can set these up more efficiently here....


	}
   
	// Update is called once per frame
	void Update()
	{
  		//this is overriden by the Agent when doing the Visibility Sphere test
		isVisible = false;
		isInteractable = false;
	}

	private void FixedUpdate()
	{
		isColliding = false;
        
	}
    
	public void ApplyForce(ServerAction action)
	{
		Vector3 dir = new Vector3(action.x, action.y, action.z);
		Rigidbody myrb = gameObject.GetComponent<Rigidbody>();
		myrb.AddForce(dir * action.moveMagnitude);
	}

	//private void Generate_UniqueID()
	//{
	//	Vector3 pos = this.transform.position;
	//	string xPos = (pos.x >= 0 ? "+" : "") + pos.x.ToString("00.00");
	//	string yPos = (pos.y >= 0 ? "+" : "") + pos.y.ToString("00.00");
	//	string zPos = (pos.z >= 0 ? "+" : "") + pos.z.ToString("00.00");
	//	this.UniqueID = this.Type.ToString() + "|" + xPos + "|" + yPos + "|" + zPos;
	//}

	public void OnTriggerStay(Collider other)
	{
		//make sure nothing is dropped while inside the agent (the agent will try to "push(?)" it out and it will fall in unpredictable ways
		if(other.tag == "Player" && other.name == "FPSController")
		{
			isColliding = true;
		}

        //ignore the trigger boxes the agent is using to check rotation, otherwise the object is colliding
		if (other.tag != "Player")
		{
			isColliding = true;
			//print(this.name +" is touching " + other.name);
		}
		//print(transform.name + "aaaah");
	}

    #if UNITY_EDITOR
	void OnDrawGizmos()
	{
		Gizmos.color = Color.white;

		//if this object is in visibile range and not blocked by any other object, it is visible
		if (isVisible == true)
		{
			MeshFilter mf = gameObject.GetComponentInChildren<MeshFilter>(false);
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireMesh(mf.sharedMesh, -1, mf.transform.position, mf.transform.rotation, mf.transform.lossyScale);
		}

		if (isInteractable == true)
		{
			MeshFilter mf = gameObject.GetComponentInChildren<MeshFilter>(false);
			Gizmos.color = Color.magenta;
			Gizmos.DrawWireMesh(mf.sharedMesh, -1, mf.transform.position, mf.transform.rotation, mf.transform.lossyScale);
		}
      
	}
    #endif

    //CONTEXT MENU STUFF FOR SETTING UP SIM OBJECTS
    //RIGHT CLICK this script in the inspector to reveal these options

	[ContextMenu("Set Up Colliders")]
    void ContextSetUpColliders()
    {
        if (transform.Find("Colliders"))
        {
            Transform Colliders = transform.Find("Colliders");

            List<GameObject> listColliders = new List<GameObject>();

            foreach (Transform child in Colliders)
            {
                //list.toarray
                listColliders.Add(child.gameObject);
            }

            MyColliders = listColliders.ToArray();
        }
    }

    [ContextMenu("Set Up TriggerColliders")]
    void ContextSetUpTriggerColliders()
    {
        if (transform.Find("TriggerColliders"))
        {
            Transform tc = transform.Find("TriggerColliders");

            List<GameObject> listtc = new List<GameObject>();

            foreach (Transform child in tc)
            {
                //list.toarray
                listtc.Add(child.gameObject);
            }

            MyTriggerColliders = listtc.ToArray();
        }
    }

    [ContextMenu("Set Up VisibilityPoints")]
    void ContextSetUpVisibilityPoints()
    {
        if (transform.Find("VisibilityPoints"))
        {
            Transform vp = transform.Find("VisibilityPoints");

            List<Transform> vplist = new List<Transform>();

            foreach (Transform child in vp)
            {
                vplist.Add(child);
            }

            VisibilityPoints = vplist.ToArray();
        }
    }

    [ContextMenu("Set Up Interaction Points")]
    void ContextSetUpInteractionPoints()
    {
        if (transform.Find("InteractionPoints"))
        {
            Transform ip = transform.Find("InteractionPoints");

            List<Transform> iplist = new List<Transform>();

            foreach (Transform child in ip)
            {
                iplist.Add(child);
            }

            InteractionPoints = iplist.ToArray();
        }
    }

    [ContextMenu("Set Up Rotate Agent Collider")]
    void ContextSetUpRotateAgentCollider()
    {
        if (transform.Find("RotateAgentCollider"))
        {
            RotateAgentCollider = transform.Find("RotateAgentCollider").gameObject;
        }
    }



}
