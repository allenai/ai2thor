using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimObjPhysics : MonoBehaviour 
{

    [SerializeField]
    public string UniqueID = string.Empty;

    [SerializeField]
    public SimObjManipTypePhysics[] ManipTypes; //is this static, moveable, CanPickup, or a receptacle? Can be multiple manip types, like Pots = CanPIckup and Receptacle

    [SerializeField]
    public SimObjTypePhysics Type = SimObjTypePhysics.Undefined; //set the type of the prefab in editor



    //raycast to this point on the object to check if it is visible
    //if the raycast hits only this object's hitbox, then it is visible
    //if the raycast is blocked by another object's hitbox, it is not visible
    [SerializeField]
    public Transform[] InteractionPoints = null;



    public bool isVisible = false;
    public bool isInteractable = false;
    //public bool Receptacle = false;
    //public bool Pickupable = false;
    //public bool Actionable = false;
    //public bool Openable = false;

    //center point of the object based on it's Visibility Collider. This is NOT the same collider as the one used for
    //rigidbody physics, the Visibility Collider is set to isTrigger and is only for the visibility range check.
    private Vector3 centerPoint;

    //initial position object spawned in in case we want to reset the scene
    private Vector3 startPosition;




	// Use this for initialization
	void Start () 
	{
        Generate_UniqueID();
	}
	
	// Update is called once per frame
	void Update () 
	{
		
	}

    private void Generate_UniqueID()
    {
        Vector3 pos = this.transform.position;
        string xPos = (pos.x >= 0 ? "+" : "") + pos.x.ToString("00.00");
        string yPos = (pos.y >= 0 ? "+" : "") + pos.y.ToString("00.00");
        string zPos = (pos.z >= 0 ? "+" : "") + pos.z.ToString("00.00");
        this.UniqueID = this.Type.ToString() + "|" + xPos + "|" + yPos + "|" + zPos;
    }

    //public Vector3 VisibilityPointLocation()
    //{
    //    return VisibilityPoint.transform.position;
    //}

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

        if(isInteractable == true)
        {
            MeshFilter mf = gameObject.GetComponentInChildren<MeshFilter>(false);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireMesh(mf.sharedMesh, -1, mf.transform.position, mf.transform.rotation, mf.transform.lossyScale);
        }

        if(isVisible == false)
        {
            isInteractable = false;
        }

    }
}
