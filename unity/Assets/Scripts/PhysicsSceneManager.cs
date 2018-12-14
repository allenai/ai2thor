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
		// //test spawning only in visible spots outside, with seed
		// if(Input.GetKeyDown(KeyCode.K))
		// RandomSpawnRequiredSceneObjects(1, true);

		// //test spawning anywhere, with seed
		// if(Input.GetKeyDown(KeyCode.J))
		// RandomSpawnRequiredSceneObjects(1, false);

		// //test default, no seed
		// if(Input.GetKeyDown(KeyCode.L))
		// RandomSpawnRequiredSceneObjects();
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

	//use action.randomseed for seed, use action.forceVisible for if objects shoudld ONLY spawn outside and not inside anything
	//set forceVisible to true for if you want objects to only spawn in immediately visible receptacles.
	public bool RandomSpawnRequiredSceneObjects(ServerAction action)
	{
		
		if(RandomSpawnRequiredSceneObjects(action.randomSeed, action.forceVisible))
		{
			return true;
		}
		
		else
		return false;
	}

	//if no values passed in, default to system random based on ticks
	public void RandomSpawnRequiredSceneObjects()
	{
		RandomSpawnRequiredSceneObjects(System.Environment.TickCount, false);
	}

	//place each object in the array of objects that should appear in this scene randomly in valid receptacles
	//a seed of 0 is the default positions placed by hand(?)
	public bool RandomSpawnRequiredSceneObjects(int seed, bool SpawnOnlyOutside)
	{
		if(RequiredObjects.Count == 0)
		{
			Debug.Log("No objects in Required Objects array, please add them in editor");
			return false;
		}
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
								if(!SpawnOnlyOutside)
								{
									typefoundindictionary = true;
									AllowedToSpawnInAndExistsInScene.Add(sop);
								}

								else
								{
									if(ReceptacleRestrictions.SpawnOnlyOutsideReceptacles.Contains(sot))
									{
										typefoundindictionary = true;
										AllowedToSpawnInAndExistsInScene.Add(sop);
									}
								}
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

			ShuffleSimObjPhysicsList(AllowedToSpawnInAndExistsInScene);
			//print("also here?");

			// // //now we have an updated list of SimObjPhys of receptacles in the scene that are also in the list
			// // //of valid receptacles for this given game object "go" that we are currently checking this loop
			if(AllowedToSpawnInAndExistsInScene.Count > 0)
			{
				SimObjPhysics targetReceptacle;
				InstantiatePrefabTest spawner = gameObject.GetComponent<InstantiatePrefabTest>();
				List<ReceptacleSpawnPoint> targetReceptacleSpawnPoints;

				//RAAANDOM!
				ShuffleSimObjPhysicsList(AllowedToSpawnInAndExistsInScene);
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
					ShuffleReceptacleSpawnPointList(targetReceptacleSpawnPoints);
					
					//try to spawn it, and if it succeeds great! if not uhhh...
					if(spawner.PlaceObjectReceptacle(targetReceptacleSpawnPoints, temp.GetComponent<SimObjPhysics>(), true))
					{
						Debug.Log(go.name + " succesfully spawned");
						diditspawn = true;
						break;
					}

					//object failed to spawn, destroy it and try again 
					else
					{
						Destroy(temp);
					}
				}

				if(!diditspawn)
				{
					Debug.Log("None of the receptacles in the scene could spawn " + go.name);
					return false;
				}
			}
		}

		Debug.Log("Iteration through Required Objects finished");
		return true;
	}

	public void ShuffleReceptacleSpawnPointList (List<ReceptacleSpawnPoint> list)
	{
		for(int i = 0; i < list.Count; i++)
		{
			ReceptacleSpawnPoint rsp = list[i];
			int r = Random.Range(i,list.Count);
			list[i] = list[r];
			list[r] = rsp;
		}
	}

	public void ShuffleSimObjPhysicsList (List<SimObjPhysics> list)
	{
		for(int i = 0; i < list.Count; i++)
		{
			SimObjPhysics sop = list[i];
			int r = Random.Range(i,list.Count);
			list[i] = list[r];
			list[r] = sop;
		}
	}


		
}
