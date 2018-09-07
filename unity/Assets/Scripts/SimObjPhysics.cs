using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

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

	//[SerializeField]
	//public Transform[] InteractionPoints = null;

	[SerializeField]
	public Transform[] VisibilityPoints = null;

	[SerializeField]
	public GameObject[] MyColliders = null;

	[SerializeField]
	public GameObject[] MyTriggerColliders = null;

	[SerializeField]
	public GameObject[] ReceptacleTriggerBoxes = null;

	public bool isVisible = false;
	public bool isInteractable = false;
	public bool isColliding = false;

	public bool IsOpenable {
		get { return this.GetComponent<CanOpen>() || this.GetComponent<CanOpen_Object>(); }
	}

	public bool IsOpen {
		get {
			CanOpen co = this.GetComponent<CanOpen>();
			CanOpen_Object coo = this.GetComponent<CanOpen_Object>();
			if (co != null) {
				return co.isOpen;
			} else if (coo != null) {
				return coo.isOpen;
			} else {
				return false;
			}
		}
	}

	public bool IsReceptacle {
		get {
			return Array.IndexOf(SecondaryProperties, SimObjSecondaryProperty.Receptacle) > -1 &&
			 ReceptacleTriggerBoxes != null;
		}
	}

    //duplicate a non trigger collider, add a rigidbody to it and parant the duplicate to the original selection
    //for use with cabinet/fridge doors that need a secondary rigidbody to allow physics on the door while animating
	#if UNITY_EDITOR
    [UnityEditor.MenuItem("SimObjectPhysics/Create RB Collider")]
    public static void CreateRBCollider()
	{
		GameObject prefabRoot = Selection.activeGameObject;
		//print(prefabRoot.name);

		GameObject inst = Instantiate(prefabRoot, Selection.activeGameObject.transform, true);

		//inst.transform.SetParent(Selection.activeGameObject.transform);

		inst.name = "rbCol";
		inst.gameObject.AddComponent<Rigidbody>();
		inst.GetComponent<Rigidbody>().isKinematic = true;
		inst.GetComponent<Rigidbody>().useGravity = true;

        //default tag and layer so that nothing is raycast against this. The only thing this exists for is to make physics real
		inst.tag = "Untagged";
		inst.layer = 0;// default layer

		//EditorUtility.GetPrefabParent(Selection.activeGameObject);
        //PrefabUtility.InstantiatePrefab(prefabRoot);
	}
    #endif

	// Use this for initialization
	void Start()
	{
		//XXX For Debug setting up scene, comment out or delete when done settig up scenes
#if UNITY_EDITOR
		List<SimObjSecondaryProperty> temp = new List<SimObjSecondaryProperty>(SecondaryProperties);
		if (temp.Contains(SimObjSecondaryProperty.Receptacle))
		{
			if (ReceptacleTriggerBoxes.Length == 0)
			{
				Debug.LogError(this.name + " is missing ReceptacleTriggerBoxes please hook them up");
			}
		}
#endif
		//end debug setup stuff
	}
    
	public bool DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty prop)
	{
		bool result = false;
        List<SimObjSecondaryProperty> temp = new List<SimObjSecondaryProperty>(SecondaryProperties);

		if (temp.Contains(prop))
		{
			result = true;
		}

		return result;
	}
	// Update is called once per frame
	void Update()
	{
      
		//this is overriden by the Agent when doing the Visibility Sphere test
        if (isVisible) 
		{
            isVisible = false;
		}

        if (isInteractable) 
		{
            isInteractable = false;
		}

	}

	private void FixedUpdate()
	{
		isColliding = false;
	}

	//used for throwing the sim object, or anything that requires adding force for some reason
	public void ApplyForce(ServerAction action)
	{
		Vector3 dir = new Vector3(action.x, action.y, action.z);
		Rigidbody myrb = gameObject.GetComponent<Rigidbody>();
		myrb.AddForce(dir * action.moveMagnitude);
	}

    //returns a game object list of all sim objects contained by this object if it is a receptacle
    public List<GameObject> Contains_GameObject()
	{
		List<SimObjSecondaryProperty> sspList = new List<SimObjSecondaryProperty>(SecondaryProperties);

        List<GameObject> objs = new List<GameObject>();

        //is this object a receptacle?
        if (sspList.Contains(SimObjSecondaryProperty.Receptacle))
		{
			//this is a receptacle, now populate objs list of contained objets to return below
			if (ReceptacleTriggerBoxes != null)
			{
				//do this once per ReceptacleTriggerBox referenced by this object
				foreach (GameObject rtb in ReceptacleTriggerBoxes)
				{
					//now go through every object each ReceptacleTriggerBox is keeping track of and add their string UniqueID to objs
					foreach (SimObjPhysics sop in rtb.GetComponent<Contains>().CurrentlyContainedObjects())
					{
						//don't add repeats
						if (!objs.Contains(sop.gameObject))
							objs.Add(sop.gameObject);
					}
				}
			}
		}

		return objs;
	}

	//if this is a receptacle object, check what is inside the Receptacle
	//make sure to return array of strings so that this info can be put into MetaData
	public List<string> Contains()
	{
		//grab a list of all secondary properties of this object
		List<SimObjSecondaryProperty> sspList = new List<SimObjSecondaryProperty>(SecondaryProperties);

		List<string> objs = new List<string>();

		//is this object a receptacle?
		if (sspList.Contains(SimObjSecondaryProperty.Receptacle))
		{
			//this is a receptacle, now populate objs list of contained objets to return below
			if (ReceptacleTriggerBoxes != null)
			{
				//do this once per ReceptacleTriggerBox referenced by this object
				foreach (GameObject rtb in ReceptacleTriggerBoxes)
				{
					//now go through every object each ReceptacleTriggerBox is keeping track of and add their string UniqueID to objs
					foreach (string id in rtb.GetComponent<Contains>().CurrentlyContainedUniqueIDs())
					{
						//don't add repeats
						if (!objs.Contains(id))
							objs.Add(id);
					}
					//objs.Add(rtb.GetComponent<Contains>().CurrentlyContainedUniqueIDs()); 
				}

#if UNITY_EDITOR

				if(objs.Count != 0)
				{
					//print the objs for now just to check in editor
                    string result = UniqueID + " contains: ";

                    foreach (string s in objs)
                    {
                        result += s + ", ";
                    }

                    Debug.Log(result);
				}

#endif
				return objs;            
			}

			else
			{
				Debug.Log("No Receptacle Trigger Box!");
				return objs;
			}
		}

		else
		{
			Debug.Log(gameObject.name + " is not a Receptacle!");
			return objs;
		}
	}

	public void OnTriggerStay(Collider other)
	{

		//ignore collision of ghosted receptacle trigger boxes
        //because of this MAKE SURE ALL receptacle trigger boxes are tagged as "Receptacle," they should be by default
        //do this flag first so that the check against non Player objects overrides it in the right order
        if (other.tag == "Receptacle")
        {
            isColliding = false;
			return;
        }

		//make sure nothing is dropped while inside the agent (the agent will try to "push(?)" it out and it will fall in unpredictable ways
		else if (other.tag == "Player" && other.name == "FPSController")
		{
			isColliding = true;
			return;
		}

		//this is hitting something else so it must be colliding at this point!
		else if (other.tag != "Player")
		{
			isColliding = true;
			return;
		}
        
	}

#if UNITY_EDITOR
	void OnDrawGizmos()
	{
		Gizmos.color = Color.white;

		//if this object is in visibile range and not blocked by any other object, it is visible
        //visible drawn in yellow
		if (isVisible == true)
		{
			MeshFilter mf = gameObject.GetComponentInChildren<MeshFilter>(false);
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireMesh(mf.sharedMesh, -1, mf.transform.position, mf.transform.rotation, mf.transform.lossyScale);
		}

        //interactable drawn in magenta
		if (isInteractable == true)
		{
			MeshFilter mf = gameObject.GetComponentInChildren<MeshFilter>(false);
			Gizmos.color = Color.magenta;
			Gizmos.DrawWireMesh(mf.sharedMesh, -1, mf.transform.position, mf.transform.rotation, mf.transform.lossyScale);
		}

        //draw visibility points for editor
		Gizmos.color = Color.yellow;

		if (VisibilityPoints.Length > 0)
		{
			foreach (Transform t in VisibilityPoints)
			{
				Gizmos.DrawSphere(t.position, 0.01f);

			}
		}
              
        ////draw interaction points for editor
        //Gizmos.color = Color.magenta;

        //foreach (Transform t in InteractionPoints)
        //{
        //    Gizmos.DrawSphere(t.position, 0.01f);

        //}

	}

	//CONTEXT MENU STUFF FOR SETTING UP SIM OBJECTS
	//RIGHT CLICK this script in the inspector to reveal these options
	[ContextMenu("Cabinet")]
	void SetUpCabinet()
	{
		Type = SimObjType.Cabinet;
		PrimaryProperty = SimObjPrimaryProperty.Static;

		SecondaryProperties = new SimObjSecondaryProperty[2];
		SecondaryProperties[0] = SimObjSecondaryProperty.CanOpen;
		SecondaryProperties[1] = SimObjSecondaryProperty.Receptacle;

		if (!gameObject.GetComponent<Rigidbody>())
			gameObject.AddComponent<Rigidbody>();
        
		this.GetComponent<Rigidbody>().isKinematic = true;
        
		if (!gameObject.GetComponent<CanOpen_Object>())
		{
			gameObject.AddComponent<CanOpen_Object>();
			gameObject.GetComponent<CanOpen_Object>().SetMovementToRotate();
		}


		if (!gameObject.GetComponent<MovingPart>())
			gameObject.AddComponent<MovingPart>();
            
		List<GameObject> cols = new List<GameObject>();
		List<GameObject> tcols = new List<GameObject>();
		List<Transform> vpoints = new List<Transform>();
		List<GameObject> recepboxes = new List<GameObject>();

		List<GameObject> movparts = new List<GameObject>();

		List<Vector3> openPositions = new List<Vector3>();

		foreach(Transform child in gameObject.transform)
		{
			if(child.name == "StaticVisPoints")
			{
				foreach(Transform svp in child)
				{
					if(!vpoints.Contains(svp))
					vpoints.Add(svp);               
				}
			}
			
			if(child.name == "ReceptacleTriggerBox")
			{
				//print("check");
				if(!recepboxes.Contains(child.gameObject))
				recepboxes.Add(child.gameObject);
			}

            //found the cabinet door, go into it and populate triggerboxes, colliders, t colliders, and vis points
			if(child.name == "CabinetDoor")
			{
				if (child.GetComponent<Rigidbody>())
					DestroyImmediate(child.GetComponent<Rigidbody>(), true);

				if (child.GetComponent<SimObjPhysics>())
					DestroyImmediate(child.GetComponent<SimObjPhysics>(), true);

				if (child.GetComponent<CanOpen>())
				{
					openPositions.Add(child.GetComponent<CanOpen>().openPosition);

					child.transform.localEulerAngles = child.GetComponent<CanOpen>().openPosition;

					DestroyImmediate(child.GetComponent<CanOpen>(), true);

				}

				if(!movparts.Contains(child.gameObject))
				{
					movparts.Add(child.gameObject);
				}

				foreach (Transform c in child)
				{
					if(c.name == "Colliders")
					{
						foreach(Transform col in c)
						{
							if (!cols.Contains(col.gameObject))
								cols.Add(col.gameObject);

							if(col.childCount == 0)
							{
								GameObject prefabRoot = col.gameObject;

								GameObject inst = Instantiate(prefabRoot, col.gameObject.transform, true);

                                //inst.transform.SetParent(Selection.activeGameObject.transform);

                                inst.name = "rbCol";
                                inst.gameObject.AddComponent<Rigidbody>();
                                inst.GetComponent<Rigidbody>().isKinematic = true;
                                inst.GetComponent<Rigidbody>().useGravity = true;

                                //default tag and layer so that nothing is raycast against this. The only thing this exists for is to make physics real
                                inst.tag = "Untagged";
                                inst.layer = 0;// default layer
							}
						}
					}

                    if(c.name == "TriggerColliders")
					{
						foreach (Transform col in c)
                        {
                            if (!tcols.Contains(col.gameObject))
                                tcols.Add(col.gameObject);
                        }
					}
                    
                    if(c.name == "VisibilityPoints")
					{
						foreach (Transform col in c)
                        {
                            if (!vpoints.Contains(col.transform))
                               vpoints.Add(col.transform);
                        }
					}
				}
			}         
   		}

		VisibilityPoints = vpoints.ToArray();
		MyColliders = cols.ToArray();
		MyTriggerColliders = tcols.ToArray();
		ReceptacleTriggerBoxes = recepboxes.ToArray();


		gameObject.GetComponent<CanOpen_Object>().MovingParts = movparts.ToArray();
		gameObject.GetComponent<CanOpen_Object>().openPositions = new Vector3[movparts.Count];
		gameObject.GetComponent<CanOpen_Object>().closedPositions = new Vector3[movparts.Count];
        
		if(openPositions.Count != 0)
		gameObject.GetComponent<CanOpen_Object>().openPositions = openPositions.ToArray();
      

		//this.GetComponent<CanOpen>().SetMovementToRotate();
	}

	[ContextMenu("Drawer")]
	void SetUpDrawer()
	{
		//Type = SimObjType.Drawer;
		//PrimaryProperty = SimObjPrimaryProperty.Static;

		//SecondaryProperties = new SimObjSecondaryProperty[2];
		//SecondaryProperties[0] = SimObjSecondaryProperty.CanOpen;
		//SecondaryProperties[1] = SimObjSecondaryProperty.Receptacle;

		//if (!gameObject.GetComponent<Rigidbody>())
		//	gameObject.AddComponent<Rigidbody>();

		//this.GetComponent<Rigidbody>().isKinematic = true;

		//if (!gameObject.GetComponent<CanOpen>())
		//	gameObject.AddComponent<CanOpen>();

		//gameObject.GetComponent<CanOpen>().SetClosedPosition();

		if(!gameObject.GetComponent<CanOpen_Object>())
		{
			gameObject.AddComponent<CanOpen_Object>();         
		}

		if(!gameObject.GetComponent<MovingPart>())
		{
			gameObject.AddComponent<MovingPart>();

		}
		GameObject[] myobject = new GameObject[] { gameObject };
        gameObject.GetComponent<CanOpen_Object>().MovingParts = myobject;

		Vector3[] array = new Vector3[] {gameObject.GetComponent<CanOpen>().closedPosition};
		gameObject.GetComponent<CanOpen_Object>().closedPositions = array;

		Vector3[] array2 = new Vector3[] { gameObject.GetComponent<CanOpen>().openPosition };
		gameObject.GetComponent<CanOpen_Object>().openPositions = array2;


	}

    [ContextMenu("FrameCollider")]
    void ContextFrameColliderSetup()
	{
		//grab the FrameCollider prefab (find by component?)
		//then make a copy of the trigger collider array, add the triggers to it, and reassign
		//do the same for the visibility point array
		if (transform.Find("TriggerColliders"))
		{
			Transform tc = transform.Find("TriggerColliders");
         
			if(tc.Find("FrameCollider"))
			{
				Transform fc = tc.Find("FrameCollider");
				//print(fc.name);
				fc.localEulerAngles = new Vector3(0, -transform.localEulerAngles.y, 0);

				List<GameObject> frameColliders = new List<GameObject>(MyTriggerColliders);
				List<Transform> framevPoints = new List<Transform>(VisibilityPoints);
                
                //we are at the FrameCollider level here, adding in each fCol
				foreach (Transform child in fc)
				{
					//don't add duplicates
					if(!frameColliders.Contains(child.gameObject))
					frameColliders.Add(child.gameObject);

					//now for each fCol, add the vispoint to the visibility point list to update
				
					Transform vp = child.Find("vPoint");

                    //dont add duplicates
					if (!framevPoints.Contains(vp))
					framevPoints.Add(vp);
				}

				MyTriggerColliders = frameColliders.ToArray();
				VisibilityPoints = framevPoints.ToArray();
                
			}
		}

		//Transform Colliders = transform.Find("Colliders");

        //List<GameObject> listColliders = new List<GameObject>();

        //foreach (Transform child in Colliders)
        //{
        //    //list.toarray
        //    listColliders.Add(child.gameObject);

        //    //set correct tag and layer for each object
        //    //also ensure all colliders are NOT trigger
        //    child.gameObject.tag = "SimObjPhysics";
        //    child.gameObject.layer = 8;

        //    if (child.GetComponent<Collider>())
        //    {
        //        child.GetComponent<Collider>().enabled = true;
        //        child.GetComponent<Collider>().isTrigger = false;
        //    }

        //}

        //MyColliders = listColliders.ToArray();
	}
		

	[ContextMenu("Set Up SimObjPhysics")]
	void ContextSetUpSimObjPhysics()
	{
		if (this.Type == SimObjType.Undefined || this.PrimaryProperty == SimObjPrimaryProperty.Undefined)
		{
			Debug.Log("Type / Primary Property is missing");
			return;
		}
		//set up this object ot have the right tag and layer
		gameObject.tag = "SimObjPhysics";
		gameObject.layer = 8;

		if (!gameObject.GetComponent<Rigidbody>())
			gameObject.AddComponent<Rigidbody>();

		if (!gameObject.transform.Find("Colliders"))
		{
			GameObject c = new GameObject("Colliders");
			c.transform.position = gameObject.transform.position;
			c.transform.SetParent(gameObject.transform);
            
			GameObject cc = new GameObject("Col");
			cc.transform.position = c.transform.position;
			cc.transform.SetParent(c.transform);
		}

		if (!gameObject.transform.Find("TriggerColliders"))//static sim objets still need trigger colliders
		{
			//empty to hold all Trigger Colliders
			GameObject tc = new GameObject("TriggerColliders");
			tc.transform.position = gameObject.transform.position;
			tc.transform.SetParent(gameObject.transform);

			//create first trigger collider to work with
			GameObject tcc = new GameObject("tCol");
			tcc.transform.position = tc.transform.position;
			tcc.transform.SetParent(tc.transform);
		}

		if (!gameObject.transform.Find("VisibilityPoints"))
		{
			//empty to hold all visibility points
			GameObject vp = new GameObject("VisibilityPoints");
			vp.transform.position = gameObject.transform.position;
			vp.transform.SetParent(gameObject.transform);

			//create first Visibility Point to work with
			GameObject vpc = new GameObject("vPoint");
			vpc.transform.position = vp.transform.position;
			vpc.transform.SetParent(vp.transform);
		}

		//if (!gameObject.transform.Find("InteractionPoints"))
		//{
		//	//empty to hold all interaction points
		//	GameObject ip = new GameObject("InteractionPoints");
		//	ip.transform.position = gameObject.transform.position;
		//	ip.transform.SetParent(gameObject.transform);

		//	//create the first Interaction Point to work with
		//	GameObject ipc = new GameObject("iPoint");
		//	ipc.transform.position = ip.transform.position;
		//	ipc.transform.SetParent(ip.transform);
		//}

		if (!gameObject.transform.Find("RotateAgentCollider") && this.PrimaryProperty != SimObjPrimaryProperty.Static)
		{
			GameObject rac = new GameObject("RotateAgentCollider");
			rac.transform.position = gameObject.transform.position;
			rac.transform.SetParent(gameObject.transform);
		}

		ContextSetUpColliders();
		ContextSetUpTriggerColliders();
		ContextSetUpVisibilityPoints();
		//ContextSetUpInteractionPoints();
		ContextSetUpRotateAgentCollider();
	}

	//[ContextMenu("Set Up Colliders")]
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

				//set correct tag and layer for each object
				//also ensure all colliders are NOT trigger
				child.gameObject.tag = "SimObjPhysics";
				child.gameObject.layer = 8;

				if (child.GetComponent<Collider>())
				{
					child.GetComponent<Collider>().enabled = true;
					child.GetComponent<Collider>().isTrigger = false;
				}

			}

			MyColliders = listColliders.ToArray();
		}
	}

	//[ContextMenu("Set Up TriggerColliders")]
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

				//set correct tag and layer for each object
				//also ensure all colliders are set to trigger
				child.gameObject.tag = "SimObjPhysics";
				child.gameObject.layer = 8;

				if (child.GetComponent<Collider>())
				{
					child.GetComponent<Collider>().enabled = true;
					child.GetComponent<Collider>().isTrigger = true;
				}

			}

			MyTriggerColliders = listtc.ToArray();
		}
	}

	// [ContextMenu("Set Up VisibilityPoints")]
	void ContextSetUpVisibilityPoints()
	{
		if (transform.Find("VisibilityPoints"))
		{
			Transform vp = transform.Find("VisibilityPoints");

			List<Transform> vplist = new List<Transform>();

			foreach (Transform child in vp)
			{
				vplist.Add(child);

				//set correct tag and layer for each object
				child.gameObject.tag = "Untagged";
				child.gameObject.layer = 8;
			}

			VisibilityPoints = vplist.ToArray();
		}
	}

	////[ContextMenu("Set Up Interaction Points")]
	//void ContextSetUpInteractionPoints()
	//{
	//	if (transform.Find("InteractionPoints"))
	//	{
	//		Transform ip = transform.Find("InteractionPoints");

	//		List<Transform> iplist = new List<Transform>();

	//		foreach (Transform child in ip)
	//		{
	//			iplist.Add(child);

	//			//set correct tag and layer for each object
	//			child.gameObject.tag = "Untagged";
	//			child.gameObject.layer = 8;
	//		}

	//		InteractionPoints = iplist.ToArray();
	//	}
	//}

	//[ContextMenu("Set Up Rotate Agent Collider")]
	void ContextSetUpRotateAgentCollider()
	{
		if (transform.Find("RotateAgentCollider"))
		{
			RotateAgentCollider = transform.Find("RotateAgentCollider").gameObject;

			//This collider is used as a size reference for the Agent's Rotation checking boxes, so it does not need
			//to be enabled. To ensure this doesn't interact with anything else, set the Tag to Untagged, the layer to 
			//SimObjInvisible, and disable this component. Component values can still be accessed if the component itself
			//is not enabled.
			RotateAgentCollider.tag = "Untagged";
			RotateAgentCollider.layer = 9;//layer 9 - SimObjInvisible

			if (RotateAgentCollider.GetComponent<BoxCollider>())
				RotateAgentCollider.GetComponent<BoxCollider>().enabled = false;
		}
	}
	#endif

    
}
