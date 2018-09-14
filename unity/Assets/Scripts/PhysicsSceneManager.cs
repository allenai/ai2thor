using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class PhysicsSceneManager : MonoBehaviour 
{
	public List<SimObjPhysics> PhysObjectsInScene = new List<SimObjPhysics>();

	public List<string> UniqueIDsInScene = new List<string>();

	private void OnEnable()
	{
		//clear this on start so that the CheckForDuplicates function doesn't check pre-existing lists
        UniqueIDsInScene.Clear();

		GatherSimObjPhysInScene();
	}
	// Use this for initialization
	void Start () 
	{

	}
	
	// Update is called once per frame
	void Update () 
	{
		
	}

    public void GatherSimObjPhysInScene()
	{
		PhysObjectsInScene = new List<SimObjPhysics>();

		PhysObjectsInScene.AddRange(FindObjectsOfType<SimObjPhysics>());
		PhysObjectsInScene.Sort((x, y) => (x.Type.ToString().CompareTo(y.Type.ToString())));

		foreach(SimObjPhysics o in PhysObjectsInScene)
		{
			Generate_UniqueID(o);

			//check against any Unique IDs currently tracked in list if there is a duplicate
			if (CheckForDuplicateUniqueIDs(o))
				Debug.Log("Yo there are duplicate UniqueIDs! Check" + o.UniqueID);

			else
				UniqueIDsInScene.Add(o.UniqueID);

		}
	}
    
	private void Generate_UniqueID(SimObjPhysics o)
    {
        Vector3 pos = o.transform.position;
        string xPos = (pos.x >= 0 ? "+" : "") + pos.x.ToString("00.00");
        string yPos = (pos.y >= 0 ? "+" : "") + pos.y.ToString("00.00");
        string zPos = (pos.z >= 0 ? "+" : "") + pos.z.ToString("00.00");
        o.UniqueID = o.Type.ToString() + "|" + xPos + "|" + yPos + "|" + zPos;
    }
    
	private bool CheckForDuplicateUniqueIDs(SimObjPhysics sop)
	{
		if (UniqueIDsInScene.Contains(sop.UniqueID))
			return true;

		else
			return false;
	}

	public void AddToObjectsInScene(SimObjPhysics sop)
	{
		PhysObjectsInScene.Add(sop);
	}

	public void AddToIDsInScene(SimObjPhysics sop)
	{
		UniqueIDsInScene.Add(sop.uniqueID);
	}
		
}
