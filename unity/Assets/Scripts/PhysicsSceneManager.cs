using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
public class PhysicsSceneManager : MonoBehaviour 
{
	public List<GameObject> RequiredObjects = new List<GameObject>();
	public List<SimObjPhysics> PhysObjectsInScene = new List<SimObjPhysics>();

	public List<string> UniqueIDsInScene = new List<string>();

	public List<SimObjPhysics> ReceptaclesInScene = new List<SimObjPhysics>();

	private void OnEnable()
	{
		//clear this on start so that the CheckForDuplicates function doesn't check pre-existing lists
        UniqueIDsInScene.Clear();

		GatherSimObjPhysInScene();

		GatherAllReceptaclesInScene();
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

	public void GatherAllReceptaclesInScene()
	{
		foreach(SimObjPhysics sop in PhysObjectsInScene)
		{
			if(sop.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.Receptacle))
			{
				ReceptaclesInScene.Add(sop);
			}
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

	//default random seed to 0 if not using anything?
	public void RandomSpawnRequiredSceneObjects()
	{
		RandomSpawnRequiredSceneObjects(0);
	}

	//place each object in the array of objects that should appear in this scene randomly in valid receptacles
	//a seed of 0 is the default positions placed by hand(?)
	public void RandomSpawnRequiredSceneObjects(int seed)
	{
		Random.InitState(seed);


		List<SimObjType> TypesOfObjectsPrefabIsAllowedToSpawnIn = new List<SimObjType>();

		//for each object in RequiredObjects, start a list of what objects it's allowed 
		//to spawn in by checking the PlacementRestrictions dictionary
		foreach(GameObject go in RequiredObjects)
		{
			foreach(KeyValuePair<SimObjType, List<SimObjType>> res in ReceptacleRestrictions.PlacementRestrictions)
			{
				//find the game object's type in the ReceptacleRestrictions dictionary
				if(go.GetComponent<SimObjPhysics>().ObjType == res.Key)
				{
					//copy the list of receptacles this object is allowed to spawn in for further use below
					TypesOfObjectsPrefabIsAllowedToSpawnIn = res.Value;

					List<SimObjPhysics> AllowedToSpawnInAndExistsInScene = new List<SimObjPhysics>();

					foreach(SimObjType sot in TypesOfObjectsPrefabIsAllowedToSpawnIn)
					{
						foreach(SimObjPhysics sop in ReceptaclesInScene)
						{
							//if the potential valid object type matches one of the ReceptacleinScene's object types
							if(sot == sop.ObjType)
							{
								AllowedToSpawnInAndExistsInScene.Add(sop);
							}
						}
					}

					//now we have an updated list of SimObjPhys of receptacles in the scene that are also in the list
					//of valid receptacles for this given game object
					if(AllowedToSpawnInAndExistsInScene.Count > 0)
					{
						int tryThisReceptacle = Random.Range(0, AllowedToSpawnInAndExistsInScene.Count);

						SimObjPhysics targetReceptacle = AllowedToSpawnInAndExistsInScene[tryThisReceptacle];


					}

					//none of the receptacles in the scene match any of the receptacles that the object can spawn in
					else
					{
						Debug.Log("none of the receptacles in scene can have a " + go + " spawn in/on it");
						return;
					}




				}

				else
				{
					//the key was not found in the dictionary, so it's missing!
					Debug.Log(go.GetComponent<SimObjPhysics>().ObjType + " was not found in PlacementRestrictions Dictionary!");
					return;
				}
			}


			//if(go.GetComponent<SimObjPhysics>().ObjType == )
		}

		//now that we have a list of valid object types, start a list of
		//any Receptacles in the scene that match those object types

		//ok now we have an updated list of valid receptacles, now randomly pick one of those

		//ok now get all valid spawn points on that receptacle, randomly pick one of them and
		//see if it's a valid spawn, do this until you spawn succesfully, if not, go back out and
		//find another valid receptacle
	}

	//take all Required objects in this scene and scramble their positions
	public void RandomScrambleRequiredSceneObjects(int seed)
	{

	}
		
}
