using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using UnityEditor;
//using System.Linq;
using UnityStandardAssets.Characters.FirstPerson;


[ExecuteInEditMode]
public class PhysicsSceneManager : MonoBehaviour 
{
	//public Dictionary<SimObjPhysics, List<ReceptacleSpawnPoint>> SceneSpawnPoints =  new Dictionary<SimObjPhysics, List<ReceptacleSpawnPoint>>();

	public List<GameObject> RequiredObjects = new List<GameObject>();

	//get references to the spawned Required objects after spawning them for the first time.
	public List<GameObject> SpawnedObjects = new List<GameObject>();
	public List<SimObjPhysics> PhysObjectsInScene = new List<SimObjPhysics>();

	public List<string> UniqueIDsInScene = new List<string>();

	public List<SimObjPhysics> ReceptaclesInScene = new List<SimObjPhysics>();

	public GameObject HideAndSeek;

    //public List<SimObjPhysics> LookAtThisList = new List<SimObjPhysics>();

	private void OnEnable()
	{
		//clear this on start so that the CheckForDuplicates function doesn't check pre-existing lists
		SetupScene();

		if(GameObject.Find("HideAndSeek"))
		HideAndSeek = GameObject.Find("HideAndSeek");

		if(!GameObject.Find("Objects"))
		{
			GameObject c = new GameObject("Objects");
		}
	}

	public void SetupScene()
	{
        UniqueIDsInScene.Clear();
		ReceptaclesInScene.Clear();
		PhysObjectsInScene.Clear();
		GatherSimObjPhysInScene();
		GatherAllReceptaclesInScene();
	}
	// Use this for initialization
	void Start () 
	{

	}
	
	void Update () 
	{

	}
	public bool ToggleHideAndSeek(bool hide)
	{
		if(HideAndSeek)
		{
			HideAndSeek.SetActive(hide);
			SetupScene();
			return true;
		}

		else
		{
			#if UNITY_EDITOR
			Debug.Log("Hide and Seek object reference not set!");
			#endif

			return false;
		}


	}

    public void GatherSimObjPhysInScene()
	{
		//PhysObjectsInScene.Clear();
		PhysObjectsInScene = new List<SimObjPhysics>();

		PhysObjectsInScene.AddRange(FindObjectsOfType<SimObjPhysics>());
		//PhysObjectsInScene.Sort((x, y) => (x.Type.ToString().CompareTo(y.Type.ToString())));

		foreach(SimObjPhysics o in PhysObjectsInScene)
		{
			Generate_UniqueID(o);

			///debug in editor, make sure no two object share ids for some reason
			#if UNITY_EDITOR
			if (CheckForDuplicateUniqueIDs(o))
			{
				
				Debug.Log("Yo there are duplicate UniqueIDs! Check" + o.UniqueID);
				
			}


			else
			#endif
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
		//check if this object require's it's parent simObj's UniqueID as a prefix
		if(ReceptacleRestrictions.UseParentUniqueIDasPrefix.Contains(o.Type))
		{
			SimObjPhysics parent = o.transform.parent.GetComponent<SimObjPhysics>();
			if(parent.UniqueID == null)
			{
				Vector3 ppos = parent.transform.position;
				string xpPos = (ppos.x >= 0 ? "+" : "") + ppos.x.ToString("00.00");
				string ypPos = (ppos.y >= 0 ? "+" : "") + ppos.y.ToString("00.00");
				string zpPos = (ppos.z >= 0 ? "+" : "") + ppos.z.ToString("00.00");
				parent.UniqueID = parent.Type.ToString() + "|" + xpPos + "|" + ypPos + "|" + zpPos;
			}

			o.UniqueID = parent.UniqueID + "|" + o.Type.ToString();
			return;

		}

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
		
		if(RandomSpawnRequiredSceneObjects(action.randomSeed, action.forceVisible, action.maxNumRepeats, action.placeStationary))
		{
			return true;
		}
		
		else
		return false;
	}

	//if no values passed in, default to system random based on ticks
	public void RandomSpawnRequiredSceneObjects()
	{
		RandomSpawnRequiredSceneObjects(System.Environment.TickCount, false, 50, false);
	}

	//place each object in the array of objects that should appear in this scene randomly in valid receptacles
	//a seed of 0 is the default positions placed by hand(?)
	public bool RandomSpawnRequiredSceneObjects(int seed, bool SpawnOnlyOutside, int maxcount, bool StaticPlacement)
	{
		#if UNITY_EDITOR
		var Masterwatch = System.Diagnostics.Stopwatch.StartNew();
		#endif

		if(RequiredObjects.Count == 0)
		{
			#if UNITY_EDITOR
			Debug.Log("No objects in Required Objects array, please add them in editor");
			#endif
			
			return false;
		}

		Random.InitState(seed);

		List<SimObjType> TypesOfObjectsPrefabIsAllowedToSpawnIn = new List<SimObjType>();
		List<SimObjPhysics> AllowedToSpawnInAndExistsInScene = new List<SimObjPhysics>();

		List<GameObject> TargetList = new List<GameObject>();

		int HowManyCouldntSpawn = RequiredObjects.Count;

		//if we already spawned objects, lets just move them around
		if(SpawnedObjects.Count > 0)
		{
			HowManyCouldntSpawn = SpawnedObjects.Count;
			//bool diditspawn = false;

			//for each object in RequiredObjects, start a list of what objects it's allowed 
			//to spawn in by checking the PlacementRestrictions dictionary
			foreach(GameObject go in SpawnedObjects)
			{
				TypesOfObjectsPrefabIsAllowedToSpawnIn.Clear();
				AllowedToSpawnInAndExistsInScene.Clear();

				SimObjType goObjType = go.GetComponent<SimObjPhysics>().ObjType;

				bool typefoundindictionary = ReceptacleRestrictions.PlacementRestrictions.ContainsKey(goObjType);
				if(typefoundindictionary)
				{
					TypesOfObjectsPrefabIsAllowedToSpawnIn = new List<SimObjType>(ReceptacleRestrictions.PlacementRestrictions[goObjType]);

					//remove from list if receptacle isn't in this scene
					//compare to receptacles that exist in scene, get the ones that are the same
					
					foreach(SimObjPhysics sop in ReceptaclesInScene)
					{
						if(SpawnOnlyOutside)
						{
							if(ReceptacleRestrictions.SpawnOnlyOutsideReceptacles.Contains(sop.ObjType) && TypesOfObjectsPrefabIsAllowedToSpawnIn.Contains(sop.ObjType))
							{
								AllowedToSpawnInAndExistsInScene.Add(sop);
							}
						}

						else if(TypesOfObjectsPrefabIsAllowedToSpawnIn.Contains(sop.ObjType))
						{
							//updated list of valid receptacles in scene
							AllowedToSpawnInAndExistsInScene.Add(sop);
						}
					}
				}

				//not found indictionary!
				else
				{
					#if UNITY_EDITOR
					Debug.Log(go.name +"'s Type is not in the ReceptacleRestrictions dictionary!");
					#endif
					break;
				}

				// // //now we have an updated list of SimObjPhys of receptacles in the scene that are also in the list
				// // //of valid receptacles for this given game object "go" that we are currently checking this loop
				if(AllowedToSpawnInAndExistsInScene.Count > 0)
				{
					SimObjPhysics targetReceptacle;
					InstantiatePrefabTest spawner = gameObject.GetComponent<InstantiatePrefabTest>();
					List<ReceptacleSpawnPoint> targetReceptacleSpawnPoints;

					//RAAANDOM!
					ShuffleSimObjPhysicsList(AllowedToSpawnInAndExistsInScene);
					//bool diditspawn = false;
			
					foreach(SimObjPhysics sop in AllowedToSpawnInAndExistsInScene)
					{
						targetReceptacle = sop;

						//check if the target Receptacle is an ObjectSpecificReceptacle
						//if so, if this game object is compatible with the ObjectSpecific restrictions, place it!
						if(targetReceptacle.DoesThisObjectHaveThisSecondaryProperty(SimObjSecondaryProperty.ObjectSpecificReceptacle))
						{
							ObjectSpecificReceptacle osr = targetReceptacle.GetComponent<ObjectSpecificReceptacle>();
							if(osr.HasSpecificType(go.GetComponent<SimObjPhysics>().ObjType) && osr.isFull() == false)
							{
								go.transform.position = osr.attachPoint.position;
								go.transform.SetParent(osr.attachPoint.transform);
								go.transform.localRotation = Quaternion.identity;
								go.GetComponent<Rigidbody>().isKinematic = true;
								HowManyCouldntSpawn--;

								#if UNITY_EDITOR
                				Debug.Log(go.name + " succesfully placed in " +targetReceptacle.UniqueID);
                				#endif

								break;
							}
						}

						targetReceptacleSpawnPoints = targetReceptacle.ReturnMySpawnPoints(false);

						//first shuffle the list so it's raaaandom
						ShuffleReceptacleSpawnPointList(targetReceptacleSpawnPoints);
						
						//try to spawn it, and if it succeeds great! if not uhhh...

						#if UNITY_EDITOR
						var watch = System.Diagnostics.Stopwatch.StartNew();
						#endif

						if(spawner.PlaceObjectReceptacle(targetReceptacleSpawnPoints, go.GetComponent<SimObjPhysics>(), StaticPlacement, maxcount, 360, true)) //we spawn them stationary so things don't fall off of ledges
						{
							//Debug.Log(go.name + " succesfully spawned");
							//diditspawn = true;
							HowManyCouldntSpawn--;
							break;
						}

						#if UNITY_EDITOR
						watch.Stop();
						var elapsedMs = watch.ElapsedMilliseconds;
						print("time for PlacfeObject: " + elapsedMs);
						#endif

					}
				}
			}
		}

		//we have not spawned objects, so instantiate them here first
		else
		{
			//for each object in RequiredObjects, start a list of what objects it's allowed 
			//to spawn in by checking the PlacementRestrictions dictionary
			foreach(GameObject go in RequiredObjects)
			{
				TypesOfObjectsPrefabIsAllowedToSpawnIn.Clear();
				AllowedToSpawnInAndExistsInScene.Clear();

				SimObjType goObjType = go.GetComponent<SimObjPhysics>().ObjType;

				bool typefoundindictionary = ReceptacleRestrictions.PlacementRestrictions.ContainsKey(goObjType);
				if(typefoundindictionary)
				{
					TypesOfObjectsPrefabIsAllowedToSpawnIn = new List<SimObjType>(ReceptacleRestrictions.PlacementRestrictions[goObjType]);

					//remove from list if receptacle isn't in this scene
					//compare to receptacles that exist in scene, get the ones that are the same
					
					foreach(SimObjPhysics sop in ReceptaclesInScene)
					{
						if(SpawnOnlyOutside)
						{
							if(ReceptacleRestrictions.SpawnOnlyOutsideReceptacles.Contains(sop.ObjType) && TypesOfObjectsPrefabIsAllowedToSpawnIn.Contains(sop.ObjType))
							{
								AllowedToSpawnInAndExistsInScene.Add(sop);
							}
						}

						else if(TypesOfObjectsPrefabIsAllowedToSpawnIn.Contains(sop.ObjType))
						{
							//updated list of valid receptacles in scene
							AllowedToSpawnInAndExistsInScene.Add(sop);
						}
					}
				}

				else
				{
					#if UNITY_EDITOR
					Debug.Log(go.name +"'s Type is not in the ReceptacleRestrictions dictionary!");
					#endif

					break;
				}

				// // //now we have an updated list of SimObjPhys of receptacles in the scene that are also in the list
				// // //of valid receptacles for this given game object "go" that we are currently checking this loop
				if(AllowedToSpawnInAndExistsInScene.Count > 0)
				{
					SimObjPhysics targetReceptacle;
					InstantiatePrefabTest spawner = gameObject.GetComponent<InstantiatePrefabTest>();
					List<ReceptacleSpawnPoint> targetReceptacleSpawnPoints;

					//RAAANDOM!
					ShuffleSimObjPhysicsList(AllowedToSpawnInAndExistsInScene);
					//bool diditspawn = false;


					GameObject temp = Instantiate(go, new Vector3(0, 100, 0), Quaternion.identity);
					temp.transform.name = go.name;
					//print("create object");
					//GameObject temp = PrefabUtility.InstantiatePrefab(go as GameObject) as GameObject;
					temp.GetComponent<Rigidbody>().isKinematic = true;
					//spawn it waaaay outside of the scene and then we will try and move it in a moment here, hold your horses
					temp.transform.position = new Vector3(0, 100, 0);//GameObject.Find("FPSController").GetComponent<PhysicsRemoteFPSAgentController>().AgentHandLocation();

					foreach(SimObjPhysics sop in AllowedToSpawnInAndExistsInScene)
					{
						targetReceptacle = sop;

						targetReceptacleSpawnPoints = targetReceptacle.ReturnMySpawnPoints(false);

						//first shuffle the list so it's raaaandom
						ShuffleReceptacleSpawnPointList(targetReceptacleSpawnPoints);
						
						//try to spawn it, and if it succeeds great! if not uhhh...

						#if UNITY_EDITOR
						var watch = System.Diagnostics.Stopwatch.StartNew();
						#endif

						if(spawner.PlaceObjectReceptacle(targetReceptacleSpawnPoints, temp.GetComponent<SimObjPhysics>(), true, maxcount, 360, true)) //we spawn them stationary so things don't fall off of ledges
						{
							//Debug.Log(go.name + " succesfully spawned");
							//diditspawn = true;
							HowManyCouldntSpawn--;
							SpawnedObjects.Add(temp);
							break;
						}

						#if UNITY_EDITOR
						watch.Stop();
						var elapsedMs = watch.ElapsedMilliseconds;
						print("time for PlacfeObject: " + elapsedMs);
						#endif

					}
				}
			}			
		}

		//we can use this to report back any failed spawns if we want that info at some point ?

		#if UNITY_EDITOR
		if(HowManyCouldntSpawn > 0)
		{
			Debug.Log(HowManyCouldntSpawn + " object(s) could not be spawned into the scene!");
		}

		Masterwatch.Stop();
		var elapsed = Masterwatch.ElapsedMilliseconds;
		print("total time: " + elapsed);
		#endif

		//Debug.Log("Iteration through Required Objects finished");
		SetupScene();
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
