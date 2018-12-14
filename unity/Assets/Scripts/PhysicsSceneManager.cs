using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityStandardAssets.Characters.FirstPerson;

[ExecuteInEditMode]
public class PhysicsSceneManager : MonoBehaviour 
{
	public List<GameObject> RequiredObjects = new List<GameObject>();
	public List<SimObjPhysics> PhysObjectsInScene = new List<SimObjPhysics>();

	public List<string> UniqueIDsInScene = new List<string>();

	public List<SimObjPhysics> ReceptaclesInScene = new List<SimObjPhysics>();

	public List<SimObjPhysics> LookAtThisList = new List<SimObjPhysics>();

	private void OnEnable()
	{
		//clear this on start so that the CheckForDuplicates function doesn't check pre-existing lists
        UniqueIDsInScene.Clear();
		ReceptaclesInScene.Clear();

		GatherSimObjPhysInScene();

		GatherAllReceptaclesInScene();

	}
	// Use this for initialization
	void Start () 
	{
		//RandomSpawnRequiredSceneObjects();
	}
	
	// Update is called once per frame
	void Update () 
	{
		if(Input.GetKeyDown(KeyCode.K))
		RandomSpawnRequiredSceneObjects(0);
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

				foreach (GameObject go in sop.ReceptacleTriggerBoxes)
				{
					if(go.GetComponent<Contains>().myParent == null)
					go.GetComponent<Contains>().myParent = sop.transform.gameObject;
				}
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
		List<SimObjPhysics> AllowedToSpawnInAndExistsInScene = new List<SimObjPhysics>();

		//for each object in RequiredObjects, start a list of what objects it's allowed 
		//to spawn in by checking the PlacementRestrictions dictionary
		foreach(GameObject go in RequiredObjects)
		{
			bool typefoundindictionary = false;
			foreach(KeyValuePair<SimObjType, List<SimObjType>> res in ReceptacleRestrictions.PlacementRestrictions)
			{
				//find the game object's type in the ReceptacleRestrictions dictionary
				if(go.GetComponent<SimObjPhysics>().ObjType == res.Key)
				{
					//copy the list of receptacles this object is allowed to spawn in for further use below
					TypesOfObjectsPrefabIsAllowedToSpawnIn = res.Value;

					foreach(SimObjType sot in TypesOfObjectsPrefabIsAllowedToSpawnIn)
					{
						foreach(SimObjPhysics sop in ReceptaclesInScene)
						{
							//if the potential valid object type matches one of the ReceptacleinScene's object types
							if(sot == sop.ObjType)
							{
								//print("here");
								typefoundindictionary = true;
								AllowedToSpawnInAndExistsInScene.Add(sop);
							}
						}
					}
				}
			}

			if(!typefoundindictionary)
			{
				Debug.Log(go.name +"'s Type is not in the ReceptacleRestrictions dictionary!");
				break;
			}

			LookAtThisList = AllowedToSpawnInAndExistsInScene;
			//print("also here?");

			// //now we have an updated list of SimObjPhys of receptacles in the scene that are also in the list
			// //of valid receptacles for this given game object "go" that we are currently checking this loop
			if(AllowedToSpawnInAndExistsInScene.Count > 0)
			{
				SimObjPhysics targetReceptacle;
				InstantiatePrefabTest spawner = gameObject.GetComponent<InstantiatePrefabTest>();
				List<ReceptacleSpawnPoint> targetReceptacleSpawnPoints;

				//RAAANDOM!
				AllowedToSpawnInAndExistsInScene = ShuffleSimObjPhysicsList(AllowedToSpawnInAndExistsInScene, seed);
				bool diditspawn = false;

				foreach(SimObjPhysics sop in AllowedToSpawnInAndExistsInScene)
				{
					targetReceptacle = sop;
					targetReceptacleSpawnPoints = targetReceptacle.ReturnMySpawnPoints(false);

					GameObject temp = PrefabUtility.InstantiatePrefab(go as GameObject) as GameObject;
					temp.GetComponent<Rigidbody>().isKinematic = true;
					//spawn it waaaay outside of the scene and then we will try and move it in a moment here, hold your horses
					temp.transform.position = new Vector3(0, 100, 0);//GameObject.Find("FPSController").GetComponent<PhysicsRemoteFPSAgentController>().AgentHandLocation();

					//first shuffle the list so it's raaaandom
					targetReceptacleSpawnPoints = ShuffleReceptacleSpawnPointList(targetReceptacleSpawnPoints, seed);
					
					//try to spawn it, and if it succeeds great! if not uhhh...
					if(spawner.PlaceObjectReceptacle(targetReceptacleSpawnPoints, temp.GetComponent<SimObjPhysics>(), true))
					{
						Debug.Log(go.name + " succesfully spawned");
						diditspawn = true;
						break;
					}
				}

				if(!diditspawn)
				{
					Debug.Log("None of the receptacles in the scene could spawn " + go.name);
				}
			}
		}

		//now that we have a list of valid object types, start a list of
		//any Receptacles in the scene that match those object types

		//ok now we have an updated list of valid receptacles, now randomly pick one of those

		//ok now get all valid spawn points on that receptacle, randomly pick one of them and
		//see if it's a valid spawn, do this until you spawn succesfully, if not, go back out and
		//find another valid receptacle
	}

	public List<ReceptacleSpawnPoint> ShuffleReceptacleSpawnPointList (List<ReceptacleSpawnPoint> list, int seed)
	{
		Random.InitState(seed);
		System.Random rand = new System.Random(seed);

		ReceptacleSpawnPoint receptacleSpawn;

		int n = list.Count;

		for(int i = 0; i < n; i++)
		{
			int r = i + (int)rand.NextDouble() * (n - i);
			receptacleSpawn = list[r];
			list[r] = list[i];
			list[i] = receptacleSpawn;
		}

		return list;
	}

	public List<SimObjPhysics> ShuffleSimObjPhysicsList (List<SimObjPhysics> list, int seed)
	{
	    Random.InitState(seed);
		System.Random rand = new System.Random(seed);

		SimObjPhysics sop;

		int n = list.Count;

		for(int i = 0; i < n; i++)
		{
			int r = i + (int)rand.NextDouble() * (n - i);
			sop = list[r];
			list[r] = list[i];
			list[i] = sop;
		}

		return list;
	}


		
}
