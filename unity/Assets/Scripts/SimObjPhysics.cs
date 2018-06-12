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

	public GameObject RotateCollider = null;

	[SerializeField]
	public Transform[] InteractionPoints = null;

	[SerializeField]
	public Transform[] VisibilityPoints = null;
   
	public bool isVisible = false;
	public bool isInteractable = false;
	public bool isColliding = false;


	//initial position object spawned in in case we want to reset the scene
	//private Vector3 startPosition;   

	// Use this for initialization
	void Start()
	{
		Generate_UniqueID();
		//startPosition = transform.position;
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

	private void Generate_UniqueID()
	{
		Vector3 pos = this.transform.position;
		string xPos = (pos.x >= 0 ? "+" : "") + pos.x.ToString("00.00");
		string yPos = (pos.y >= 0 ? "+" : "") + pos.y.ToString("00.00");
		string zPos = (pos.z >= 0 ? "+" : "") + pos.z.ToString("00.00");
		this.UniqueID = this.Type.ToString() + "|" + xPos + "|" + yPos + "|" + zPos;
	}

	public void OnTriggerStay(Collider other)
	{
		if (other.transform.name != "TheHand")
			isColliding = true;
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
	//private void OnTriggerEnter(Collider other)
	//{
	//       print("AAAAH");
	//}


}
