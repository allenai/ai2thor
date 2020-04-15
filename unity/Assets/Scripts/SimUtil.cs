// Copyright Allen Institute for Artificial Intelligence 2017
using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public static class SimUtil {

	//how fast smooth-animated s / cabinets / etc animate
	public const float SmoothAnimationSpeed = 0.5f;

	public const int RaycastVisibleLayer = 8;
	public const int RaycastHiddenLayer = 9;
	public const int RaycastVisibleLayerMask = 1 << RaycastVisibleLayer;
	public const int RaycastHiddenLayerMask = 1 << RaycastHiddenLayer;
	public const string ReceptacleTag = "Receptacle";
	public const string SimObjTag = "SimObj";
	public const string StructureTag = "Structure";// what is this used for?
	public const float ViewPointRangeHigh = 1f;
	public const float ViewPointRangeLow = 0f;
	public const int VisibilityCheckSteps = 10;

    public const float DownwardRangeExtension = 2.0f;//1.45f; changed this so the agent can see low enough to open all drawers
	public const float MinDownwardLooKangle = 15f;
	public const float MaxDownwardLookAngle = 60f;

	public const string DefaultBuildDirectory = "";

	public static bool ShowBasePivots = true;

	public static bool ShowIDs = true;
	public static bool ShowCustomBounds = true;
	public static bool ShowObjectVisibility = true;

	#region SimObj utility functions

    public static SimObj[] GetAllVisibleSimObjs (Camera agentCamera, float maxDistance) 
    {
		#if UNITY_EDITOR
		if (ShowObjectVisibility) {
			//set all objects to invisible before starting - THIS IS EXPENSIVE
			if (SceneManager.Current != null) {
				foreach (SimObj obj in SceneManager.Current.ObjectsInScene) {
					if (obj != null) {
						obj.VisibleNow = false;
					}
				}
			}
		}


		#endif
        //the final list of visible items
        List<SimObj> items = new List<SimObj>();

        //a temporary hashset to prevent duplicates

        HashSet<SimObj> uniqueItems = new HashSet<SimObj>();


        //get a list of all the colliders we intersect with in a sphere
		Vector3 agentCameraPos = agentCamera.transform.position;
		Collider[] colliders = Physics.OverlapSphere(agentCameraPos, maxDistance * DownwardRangeExtension, SimUtil.RaycastVisibleLayerMask, QueryTriggerInteraction.Collide);
        //go through them one by one and determine if they're actually simObjs
        for (int i = 0; i < colliders.Length; i++) {
			if (colliders [i].CompareTag (SimUtil.SimObjTag) || colliders[i].CompareTag (SimUtil.ReceptacleTag)) {
				SimObj o = null;
				if (GetSimObjFromCollider (colliders [i], out o)) {
					//this may result in duplicates because of 'open' receptacles
					//but using a hashset cancels that out
					//don't worry about items contained in receptacles until visibility
					uniqueItems.Add (o);
				}
			}
        }
        //now check to see if they're actually visible
        RaycastHit hit = new RaycastHit();
        foreach (SimObj item in uniqueItems) {

			if (!CheckItemBounds (item, agentCameraPos)) {
				//if the camera isn't in bounds, skip this item
				continue;
			}
			//check whether we can see the point in a sweep from top to bottom
			bool canSeeItem = false;

			//if it's a receptacle
			//raycast against every pivot in the receptacle just to make sure we don't miss anything
			if (item.IsReceptacle) {
				foreach (Transform pivot in item.Receptacle.Pivots) {
					canSeeItem = CheckPointVisibility (
						item,
						pivot.position,
						agentCamera,
						agentCameraPos,
						SimUtil.RaycastVisibleLayerMask,
						true,
						maxDistance,
						out hit);
					//if we can see it no need for more checks!
					if (canSeeItem)
						break;
				}
			}

			if (!canSeeItem) {
				for (int i = 0; i < VisibilityCheckSteps; i++) {
					canSeeItem = CheckPointVisibility (
						item,
						Vector3.Lerp (item.TopPoint, item.BottomPoint, (float)i / VisibilityCheckSteps),
						agentCamera,
						agentCameraPos,
						SimUtil.RaycastVisibleLayerMask,
						true,
						maxDistance,
						out hit);

					//if we can see it no need for more checks!
					if (canSeeItem)
						break;
				}
			}

			if (canSeeItem) {
				//if it's the same object, add it to the list
				#if UNITY_EDITOR
				item.VisibleNow = ShowObjectVisibility & true;
				#endif
				items.Add (item);
				//now check to see if it's a receptacle interior
				if (item.IsReceptacle && hit.collider.CompareTag (SimUtil.ReceptacleTag)) {
					//if it is, add the items in the receptacle as well
					items.AddRange (GetVisibleItemsFromReceptacle (item.Receptacle, agentCamera, agentCameraPos, maxDistance));
				}
			}
        }
        //now sort the items by distance to camera
        items.Sort((x, y) => Vector3.Distance(x.transform.position, agentCameraPos).CompareTo(Vector3.Distance(y.transform.position, agentCameraPos)));
        //we're done!
        return items.ToArray();
    }

	//checks whether a point is in view of the camera
	public static bool CheckPointVisibility(Vector3 itemTargetPoint, Camera agentCamera) {
		Vector3 viewPoint = agentCamera.WorldToViewportPoint(itemTargetPoint);
		if (viewPoint.z > 0//in front of camera
			&& viewPoint.x < ViewPointRangeHigh && viewPoint.x > ViewPointRangeLow//within x bounds
			&& viewPoint.y < ViewPointRangeHigh && viewPoint.y > ViewPointRangeLow) { //within y bounds
			return true;
		}
		return false;
	}

	//checks whether a point is in view of the camera
	//and whether it can be seen via raycast
	static bool CheckPointVisibility (SimObj item, Vector3 itemTargetPoint, Camera agentCamera, Vector3 agentCameraPos, int raycastLayerMask, bool checkTrigger, float maxDistance, out RaycastHit hit) {
		hit = new RaycastHit ();
		Vector3 viewPoint = agentCamera.WorldToViewportPoint(itemTargetPoint);

		if (viewPoint.z > 0//in front of camera
		    && viewPoint.x < ViewPointRangeHigh && viewPoint.x > ViewPointRangeLow//within x bounds
		    && viewPoint.y < ViewPointRangeHigh && viewPoint.y > ViewPointRangeLow) { //within y bounds
			Vector3 itemDirection = Vector3.zero;
			//do a raycast in the direction of the item
			itemDirection = (itemTargetPoint - agentCameraPos).normalized;
			//extend the range depending on how much we're looking downward
			//base this on the angle of the RAYCAST direction not the camera direction
			Vector3 agentForward = agentCamera.transform.forward;
			agentForward.y = 0f;
			agentForward.Normalize ();
			//clap the angle so we can't wrap around
			float maxDistanceLerp = 0f;
			float lookAngle = Mathf.Clamp (Vector3.Angle (agentForward, itemDirection), 0f, MaxDownwardLookAngle) - MinDownwardLooKangle;
			maxDistanceLerp = lookAngle / MaxDownwardLookAngle;
			maxDistance = Mathf.Lerp (maxDistance, maxDistance * DownwardRangeExtension, maxDistanceLerp);
			//try to raycast for the object
			if (Physics.Raycast (
				    agentCameraPos,
				    itemDirection,
				    out hit,
				    maxDistance,
				    raycastLayerMask,
				    (checkTrigger ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore))) {
				//check to see if we hit the item we're after
				if (hit.collider.attachedRigidbody != null && hit.collider.attachedRigidbody.gameObject == item.gameObject) {
					#if UNITY_EDITOR
					Debug.DrawLine (agentCameraPos, hit.point, Color.Lerp (Color.green, Color.blue, maxDistanceLerp));
					#endif
					return true;
				}
				#if UNITY_EDITOR
				//Debug.DrawRay (agentCameraPos, itemDirection, Color.Lerp (Color.red, Color.clear, 0.75f));
				#endif
			}
		}
		return false;
	}

	//Puts an item back where you originally found it
	//returns true if successful, false if unsuccessful
	//if successful, object is placed into the world and activated
	//this only works on objects of SimObjManipulation type 'inventory'
	//if an object was spawned in a Receptacle, this function will also return false
	public static bool PutItemBackInStartupPosition (SimObj item) {
		if (item == null) {
			Debug.LogError ("Item is null, not putting item back");
			return false;
		}

		bool result = false;

		switch (item.Manipulation) {
		case SimObjManipType.Inventory:
			if (item.StartupTransform != null) {
				item.transform.position = item.StartupTransform.position;
				item.transform.rotation = item.StartupTransform.rotation;
				item.transform.localScale = item.StartupTransform.localScale;
				item.gameObject.SetActive (true);
				result = true;
			} else {
				Debug.LogWarning ("Item had no startup transform. This probably means it was spawned in a receptacle.");
			}
			break;

		default:
			break;
		}
		return result;
	}

	//TAKES an item
	//removes it from any receptacles, then disables it entirely
	//this works whether the item is standalone or in a receptacle
	public static void TakeItem (SimObj item) {
		//make item visible to raycasts
		//unparent in case it's in a receptacle
		item.VisibleToRaycasts = true;
		item.transform.parent = null;
		//disable the item entirely
		item.gameObject.SetActive (false);
		//set the position to 'up' so it's rotated correctly when it comes back
		item.transform.up = Vector3.up;
		//reset the scale (to prevent floating point weirdness)
		item.ResetScale ();
	}
    
	//checks whether the item
	public static bool CheckItemBounds (SimObj item, Vector3 agentPosition) {
		//if the item doesn't use custom bounds this is an automatic pass
		if (!item.UseCustomBounds)
			return true;

		//use the item's bounds transform as a bounding box
		//this is NOT axis-aligned so objects rotated strangely may return unexpected results
		//but it should work for 99% of cases
		Bounds itemBounds = new Bounds (item.BoundsTransform.position, item.BoundsTransform.lossyScale);
		return itemBounds.Contains (agentPosition);
	}

	//searches for a SimObj item under a receptacle by ID
	//this does not TAKE the item, it just searches for it
	public static bool FindItemFromReceptacleByID (string itemID, Receptacle r, out SimObj item) {
		item = null;
		//make sure we're not doing something insane
		if (r == null) {
			Debug.LogError ("Receptacle was null, not searching for item");
			return false;
		}

		if (!IsObjectIDValid (itemID)) {
			Debug.LogError ("itemID " + itemID.ToString() + " is NOT valid, not searching for item");
			return false;
		}
		SimObj checkItem = null;
		for (int i = 0; i < r.Pivots.Length; i++) {
			if (r.Pivots [i].childCount > 0) {
				checkItem = r.Pivots [i].GetChild (0).GetComponent <SimObj> ();
				if (checkItem != null && checkItem.ObjectID == itemID) {
					//if the item under the pivot is a SimObj with the right id
					//we've found what we're after
					item = checkItem;
					return true;
				}
			}
		}
		//couldn't find it!
		return false;
	}

	//searches for a SimObj item under a receptacle by ID
	//this does not TAKE the item, it just searches for it
	public static bool FindItemFromReceptacleByType (SimObjType itemType, Receptacle r, out SimObj item) {
		item = null;
		//make sure we're not doing something insane
		if (r == null) {
			Debug.LogError ("Receptacle was null, not searching for item");
			return false;
		}
		if (itemType == SimObjType.Undefined) {
			Debug.LogError ("Can't search for type UNDEFINED, not searching for item");
			return false;
		}
		SimObj checkItem = null;
		for (int i = 0; i < r.Pivots.Length; i++) {
			if (r.Pivots [i].childCount > 0) {
				checkItem = r.Pivots [i].GetChild (0).GetComponent <SimObj> ();
				if (checkItem != null && checkItem.Type == itemType) {
					//if the item under the pivot is a SimObj of the right type
					//we've found what we're after
					item = checkItem;
					return true;
				}
			}
		}
		//couldn't find it!
		return false;
	}

	//adds the item to a receptacle
	//enabled the object, parents it under an empty pivot, then makes it invisible to raycasts
	//returns false if there are no available pivots in the receptacle
	public static bool AddItemToVisibleReceptacle (SimObj item, Receptacle r, Camera camera) {
		//make sure we're not doing something insane
		if (item == null) {
			Debug.LogError ("Can't add null item to receptacle");
			return false;
		}
		if (r == null) {
			Debug.LogError ("Receptacle was null, not adding item");
			return false;
		}
		if (item.gameObject == r.gameObject) {
			Debug.LogError ("Receptacle and item were same object, can't add item to itself");
			return false;
		}
		//make sure there's room in the recepticle
		Transform emptyPivot = null;
		if (!GetFirstEmptyVisibleReceptaclePivot (r, camera, out emptyPivot)) {
			Debug.Log ("No visible Pivots found");
			return false;
		}
		return AddItemToReceptaclePivot (item, emptyPivot);
	}


	//adds the item to a receptacle
	//enabled the object, parents it under an empty pivot, then makes it invisible to raycasts
	//returns false if there are no available pivots in the receptacle
	public static bool AddItemToReceptacle (SimObj item, Receptacle r) {
		//make sure we're not doing something insane
		if (item == null) {
			Debug.LogError ("Can't add null item to receptacle");
			return false;
		}
		if (r == null) {
			Debug.LogError ("Receptacle was null, not adding item");
			return false;
		}
		if (item.gameObject == r.gameObject) {
			Debug.LogError ("Receptacle and item were same object, can't add item to itself");
			return false;
		}
		//make sure there's room in the recepticle
		Transform emptyPivot = null;
		if (!GetFirstEmptyReceptaclePivot (r, out emptyPivot)) {
			//Debug.Log ("Receptacle is full");
			return false;
		}
		return AddItemToReceptaclePivot (item, emptyPivot);
	}

	//adds the item to a receptacle
	//enabled the object, parents it under an empty pivot, then makes it invisible to raycasts
	//returns false if there are no available pivots in the receptacle
	public static bool AddItemToReceptaclePivot (SimObj item,  Transform pivot) {

		if (item == null) {
			Debug.LogError ("SimObj item was null in AddItemToReceptaclePivot, not adding");
			return false;
		}

		if (pivot == null) {
			Debug.LogError ("Pivot was null when attempting to add item " + item.name + " to Receptacle pivot, not adding");
			return false;
		}

		//if there's room, parent it under the empty pivot
		item.transform.parent = pivot;
		item.transform.localPosition = Vector3.zero;
		item.transform.localRotation = Quaternion.identity;
		//don't scale the item

		//make sure the item is active (in case it's been in inventory)
		item.gameObject.SetActive (true);
		item.RecalculatePoints ();

		//disable the item's colliders (since we're no longer raycasting it directly)
		item.VisibleToRaycasts = false;

		//we're done!
		return true;
	}

	public static bool GetFirstEmptyReceptaclePivot (Receptacle r, out Transform emptyPivot) {
		emptyPivot = null;
		for (int i = 0; i < r.Pivots.Length; i++) {
			if (r.Pivots [i].childCount == 0) {
				emptyPivot = r.Pivots [i];
				break;
			}
		}
		return emptyPivot != null;
	}


	public static bool GetFirstEmptyVisibleReceptaclePivot (Receptacle r, Camera camera, out Transform emptyPivot) {
		Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);

		emptyPivot = null;
		for (int i = 0; i < r.Pivots.Length; i++) {
			Transform t = r.Pivots [i];
			Bounds bounds = new Bounds (t.position, new Vector3 (0.05f, 0.05f, 0.05f));

			if (t.childCount == 0 && GeometryUtility.TestPlanesAABB (planes, bounds)) {
				emptyPivot = r.Pivots [i];
				break;
			}
		}
		return emptyPivot != null;
	}

	//tries to get a SimObj from a collider, returns false if none found
	public static bool GetSimObjFromCollider (Collider c, out SimObj o) 
    {
		o = null;
		if (c.attachedRigidbody == null) {
			//all sim objs must have a kinematic rigidbody
			return false;
		}
		o = c.attachedRigidbody.GetComponent <SimObj> ();
		return o != null;
	}

	//tries to get a receptacle from a collider, returns false if none found
	public static bool GetReceptacleFromCollider (Collider c, out Receptacle r) {
		r = null;
		if (c.attachedRigidbody == null) {
			//all sim objs must have a kinematic rigidbody
			return false;
		}
		r = c.attachedRigidbody.GetComponent <Receptacle> ();
		return r != null;
	}

    //gets all SimObjs contained in a receptacle
    //this does not TAKE any of these items
    public static SimObj[] GetItemsFromReceptacle (Receptacle r) {
        List<SimObj> items = new List<SimObj>();
        foreach (Transform t in r.Pivots) {
            if (t.childCount > 0) {
                items.Add(t.GetChild(0).GetComponent<SimObj>());
            }
        }
        return items.ToArray();
    }

	//gets all VISIBILE SimObjs contained in a receptacle
	//this does not TAKE any of these items
	public static SimObj[] GetVisibleItemsFromReceptacle (Receptacle r, Camera agentCamera, Vector3 agentCameraPos, float maxDistance) {
		List<SimObj> items = new List<SimObj>();
		//RaycastHit hit = new RaycastHit();
		foreach (Transform t in r.Pivots) {
			if (t.childCount > 0) {
				SimObj item = t.GetChild (0).GetComponent <SimObj> ();
				//check whether the item is visible (center point only)
				//since it's inside a receptacle, it will be on the invisible layer
				if (CheckPointVisibility (item.CenterPoint, agentCamera)) {
					item.VisibleNow = true;
					items.Add (item);
				} else {
					item.VisibleNow = false;
				}
			}
		}
		return items.ToArray();
	}

	public static bool IsObjectIDValid (string objectID) {
		return !string.IsNullOrEmpty (objectID);
	}

	#endregion

	#region editor commands

	#if UNITY_EDITOR
	[UnityEditor.MenuItem ("Thor/Set Up Base Objects")]
	public static void SetUpBaseObjects () {
		foreach (GameObject go in UnityEditor.Selection.gameObjects) {
			if (go.transform.childCount > 0 && go.transform.GetChild (0).name == "Base") {
				continue;
			}
			GameObject newGo = new GameObject (go.name);
			Vector3 newPos = go.transform.position;
			newPos.y = 0f;
			newGo.transform.position = newPos;
			go.transform.parent = newGo.transform;
			go.name = "Base";
			BoxCollider bc = go.GetComponent<BoxCollider> ();
			if (bc == null) {
				go.AddComponent<BoxCollider> ();
			}
		}
	}

	public static void BuildScene (string scenePath, string buildName, string outputPath, UnityEditor.BuildTarget buildTarget, bool launchOnBuild) {
		Debug.Log ("Building scene '" + scenePath + "' to '" + outputPath + "'");
		//set up the player correctly
		UnityEditor.PlayerSettings.companyName = "Allen Institute for Artificial Intelligence";
		UnityEditor.PlayerSettings.productName = "RoboSims Platform - " + buildName;
		UnityEditor.PlayerSettings.defaultScreenWidth = 400;
		UnityEditor.PlayerSettings.defaultScreenWidth = 300;
		UnityEditor.PlayerSettings.runInBackground = true;
		UnityEditor.PlayerSettings.captureSingleScreen = false;
		UnityEditor.PlayerSettings.displayResolutionDialog = UnityEditor.ResolutionDialogSetting.Disabled;
		UnityEditor.PlayerSettings.usePlayerLog = true;
		UnityEditor.PlayerSettings.resizableWindow = false;
        UnityEditor.PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
		UnityEditor.PlayerSettings.visibleInBackground = false;
		UnityEditor.PlayerSettings.allowFullscreenSwitch = true;

		UnityEditor.BuildPipeline.BuildPlayer (
			new UnityEditor.EditorBuildSettingsScene [] {
				new UnityEditor.EditorBuildSettingsScene (scenePath, true)
			},
			System.IO.Path.Combine (outputPath, buildName),
			buildTarget,
			launchOnBuild ? UnityEditor.BuildOptions.AutoRunPlayer : UnityEditor.BuildOptions.None);
	}

	[UnityEditor.MenuItem ("Thor/Replace Generic Prefabs in All Scenes")]
	static void ReplacePrefabsInAllScenes () {
		UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes ();
		for (int i = 1; i <= 30; i++) {
			string scenePath = "Assets/Scenes/FloorPlan" + i.ToString () + ".unity";
			UnityEditor.EditorUtility.DisplayProgressBar ("Replacing generics...", scenePath, (1f / 30) * i);
			UnityEngine.SceneManagement.Scene openScene = new UnityEngine.SceneManagement.Scene ();
			try {
				openScene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene (scenePath);
			} catch (Exception e) {
				Debug.LogException (e);
				continue;
			}
			//find the scene manager and tell it to replace all prefabs
			SceneManager sm = GameObject.FindObjectOfType<SceneManager> ();
			sm.ReplaceGenerics ();
			//save the scene and close it
			UnityEditor.SceneManagement.EditorSceneManager.SaveScene(openScene);
			if (UnityEditor.SceneManagement.EditorSceneManager.loadedSceneCount > 1) {
				UnityEditor.SceneManagement.EditorSceneManager.CloseScene(openScene, true);
			}
		}
		UnityEditor.EditorUtility.ClearProgressBar ();
	}

	[UnityEditor.MenuItem ("Thor/Set SceneManager Scene Number")]
	static void SetSceneManagerSceneNumber () {
		UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes ();
		for (int i = 1; i <= 30; i++) {
			string scenePath = "Assets/Scenes/FloorPlan" + (i + 200).ToString () + ".unity";
			UnityEditor.EditorUtility.DisplayProgressBar ("Setting scene numbers...", scenePath, (1f / 30) * i);
			UnityEngine.SceneManagement.Scene openScene = new UnityEngine.SceneManagement.Scene ();
			try {
				openScene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene (scenePath);
			} catch (Exception e) {
				Debug.LogException (e);
				continue;
			}
			//find the scene manager and tell it to replace all prefabs
			SceneManager sm = GameObject.FindObjectOfType<SceneManager> ();
			sm.SceneNumber = i;
			//save the scene and close it
			UnityEditor.SceneManagement.EditorSceneManager.SaveScene(openScene);
			if (UnityEditor.SceneManagement.EditorSceneManager.loadedSceneCount > 1) {
				UnityEditor.SceneManagement.EditorSceneManager.CloseScene(openScene, true);
			}
		}
		UnityEditor.EditorUtility.ClearProgressBar ();
	}

	[UnityEditor.MenuItem ("Thor/Set Pivot Scales to 1")]
	static void SetPivotScalesToOne ()
	{
		Transform[] transforms = GameObject.FindObjectsOfType<Transform> ();
		foreach (Transform t in transforms) {
			if (t.name.Contains ("Pivot")) {
				GameObject prefabParent = null;
				if (UnityEditor.EditorUtility.IsPersistent (t)) {
					prefabParent = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject) as GameObject;
				}
				Transform pivot = t;
				Transform tempParent = pivot.parent;
				Transform child = null;
				if (pivot.childCount > 0) {
					child = pivot.GetChild (0);
					child.parent = null;
				}
				pivot.parent = null;
				if (pivot.localScale != Vector3.one) {
					Debug.Log ("Found pivot with non-uniform scale (" + pivot.localScale.ToString() + ") " + pivot.name);
				}
				pivot.localScale = Vector3.one;
				if (child != null) {
					child.parent = pivot;
				}
				pivot.parent = tempParent;
				if (prefabParent != null) {
					Debug.Log ("Reconnecting to " + prefabParent.name);
                    UnityEditor.PrefabUtility.RevertPrefabInstance(t.gameObject, UnityEditor.InteractionMode.AutomatedAction);
                }
			}
		}
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty (UnityEngine.SceneManagement.SceneManager.GetActiveScene());//(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene ());

	}

	#endif

	#endregion
}
