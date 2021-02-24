// Copyright Allen Institute for Artificial Intelligence 2017
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;


#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using UnityStandardAssets.Characters.FirstPerson;

[ExecuteInEditMode]
public class SceneManager : MonoBehaviour {
	public static SceneManager Current {
		get {
			if (current == null) {
				current = GameObject.FindObjectOfType<SceneManager> ();
			}
			return current;
		}
	}
	private static SceneManager current;

	public ScenePhysicsMode LocalPhysicsMode = ScenePhysicsMode.Static;
	public SceneType LocalSceneType = SceneType.Kitchen;
	public SceneAnimationMode AnimationMode = SceneAnimationMode.Instant;

	public SimObjType[] RequiredTypesKitchen;
	public SimObjType[] RequiredTypesLivingRoom;
	public SimObjType[] RequiredTypesBedroom;
	public SimObjType[] RequiredTypesBathroom;

	public SimObjType[] RequiredObjectTypes = new SimObjType[0];
	public SimObj[] PlatonicPrefabs = new SimObj[0];
	public Object[] Scenes = new Object[0];

	public int SceneNumber = 0;

	public Transform ObjectsParent;
	public Transform ControlsParent;
	public Transform LightingParent;
	public Transform StructureParent;
	public Transform TargetsParent;

	public string ObjectsParentName = "Objects";
	public string ControlsParentName = "Controls";
	public string LightingParentName = "Lighting";
	public string StructureParentName = "Structure";
	public string TargetsParentName = "Targets";
	public GameObject FPSControllerPrefab;

	public List<SimObj> ObjectsInScene = new List<SimObj>();

	void OnEnable () {
		GatherSimObjsInScene ();

		#if UNITY_EDITOR
		if (!Application.isPlaying) {
			SetUpParents ();
			SetUpFPSController ();
			SetUpNavigation ();
			SetUpLighting();
			transform.SetAsFirstSibling ();
		}
		#endif
        this.AnimationMode = SceneAnimationMode.Instant;

	}
   
	//generates a object ID for a sim object
	public void AssignObjectID (SimObj obj) {
		//object ID is a string consisting of:
		//[SimObjType]_[X0.00]:[Y0.00]:[Z0.00]
		Vector3 pos = obj.transform.position;
		string xPos = (pos.x >= 0 ? "+" : "") + pos.x.ToString ("00.00");
		string yPos = (pos.y >= 0 ? "+" : "") + pos.y.ToString ("00.00");
		string zPos = (pos.z >= 0 ? "+" : "") + pos.z.ToString ("00.00");
		obj.ObjectID = obj.Type.ToString () + "|" + xPos + "|" + yPos + "|" + zPos;
	}

	public void GatherSimObjsInScene () {
		ObjectsInScene = new List<SimObj> ();
		ObjectsInScene.AddRange (GameObject.FindObjectsOfType<SimObj> ());
		ObjectsInScene.Sort ((x, y)=>(x.Type.ToString().CompareTo (y.Type.ToString())));
		foreach (SimObj o in ObjectsInScene) {
			AssignObjectID (o);
		}
	}

	#if UNITY_EDITOR
	//returns an array of required types NOT found in scene
	public SimObjType[] CheckSceneForRequiredTypes () {
		
		List<SimObjType> typesToCheck = null;
		switch (LocalSceneType) {
		case SceneType.Kitchen:
		default:
			typesToCheck = new List<SimObjType> (RequiredObjectTypes);
			break;

		case SceneType.LivingRoom:
			typesToCheck = new List<SimObjType> (RequiredTypesLivingRoom);
			break;

		case SceneType.Bedroom:
			typesToCheck = new List<SimObjType> (RequiredTypesBedroom);
			break;

		case SceneType.Bathroom:
			typesToCheck = new List<SimObjType> (RequiredTypesBathroom);
			break;
		}
		foreach (SimObj obj in ObjectsInScene) {
			typesToCheck.Remove (obj.Type);
		}
		return typesToCheck.ToArray ();
	}

	public void SetUpParents() {
		FindOrCreateParent (ref ObjectsParent, ObjectsParentName);
		FindOrCreateParent (ref ControlsParent, ControlsParentName);
		FindOrCreateParent (ref LightingParent, LightingParentName);
		FindOrCreateParent (ref StructureParent, StructureParentName);
		FindOrCreateParent (ref TargetsParent, TargetsParentName);
	}

	public void AutoStructureNavigation () {
		Transform[] transforms = StructureParent.GetComponentsInChildren <Transform> ();
		//take a wild guess that floor will be floor and ceiling will be ceiling
		foreach (Transform t in transforms) {
			if (t.name.Contains ("Ceiling")) {
				//set it to not walkable
				GameObjectUtility.SetNavMeshArea (t.gameObject, PlacementManager.NavemeshNoneArea);
			} else if (t.name.Contains ("Floor")) {
				//set it to floor
				GameObjectUtility.SetNavMeshArea (t.gameObject, PlacementManager.NavmeshFloorArea);
			} else {
				//if it's not already set to none (ie sittable objects) set it to 'shelves'
				if (GameObjectUtility.GetNavMeshArea (t.gameObject) != PlacementManager.NavemeshNoneArea) {
					GameObjectUtility.SetNavMeshArea (t.gameObject, PlacementManager.NavmeshShelfArea);
				}
			}
			GameObjectUtility.SetStaticEditorFlags (t.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.NavigationStatic);
		}
	}

	public void GatherObjectsUnderParents() {
		SetUpParents ();

		//move everything under structure by default
		//then move things to other folders based on their tags or type
		SimObj[] simObjs = GameObject.FindObjectsOfType<SimObj> ();
		foreach (SimObj o in simObjs) {
			Receptacle r = o.transform.GetComponentInParent<Receptacle> ();
			if (r == null || r.gameObject == o.gameObject) {
				o.transform.parent = ObjectsParent;
			}
		}

		GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
		foreach (GameObject rootObject in rootObjects) {
			//make sure it's not a parent
			if (rootObject != gameObject
				&& rootObject.transform != LightingParent
				&& rootObject.transform != ObjectsParent
				&& rootObject.transform != StructureParent
				&& rootObject.transform != TargetsParent
				&& rootObject.transform != ControlsParent
				&& rootObject.name != FPSControllerPrefab.name) {
				rootObject.transform.parent = StructureParent;
				EditorUtility.SetDirty(rootObject);
			}
		}

		ReflectionProbe[] probes = GameObject.FindObjectsOfType <ReflectionProbe> ();
		foreach (ReflectionProbe p in probes) {
			p.transform.parent = LightingParent;
		}
		Light[] lights = GameObject.FindObjectsOfType<Light> ();
		foreach (Light l in lights) {
			//TODO specify whether to gather prefab lights

			//if it's NOT in a prefab, move it
			if (PrefabUtility.GetCorrespondingObjectFromSource(l.gameObject) == null) {
				l.transform.parent = LightingParent;
			}
		}
		//tag all the structure colliders
		Collider[] cols = StructureParent.GetComponentsInChildren<Collider> ();
		foreach (Collider c in cols) {
			c.tag = "Structure";
			c.gameObject.layer = SimUtil.RaycastVisibleLayer;
		}
		//set all structure transforms to static
		Transform [] transforms = StructureParent.GetComponentsInChildren <Transform> ();
		foreach (Transform tr in transforms) {
			tr.gameObject.isStatic = true;
			GameObjectUtility.SetStaticEditorFlags (tr.gameObject, StaticEditorFlags.BatchingStatic);
		}
	}

	public void ReplaceGenerics() {
		SimObj[] simObjs = GameObject.FindObjectsOfType <SimObj> ();
		foreach (SimObj generic in simObjs) {
			foreach (SimObj platonic in PlatonicPrefabs) {
				if (generic.Type == platonic.Type) {
					//make sure one isn't a prefab of the other
					GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(generic.gameObject) as GameObject;
					if (prefab == null || prefab != platonic.gameObject) {
						Debug.Log ("Replacing " + generic.name + " with " + platonic.name);
						//as long as it's not a prefab, swap it out with the prefab
						GameObject newSimObj = PrefabUtility.InstantiatePrefab(platonic.gameObject) as GameObject;
						newSimObj.transform.position = generic.transform.position;
						newSimObj.transform.rotation = generic.transform.rotation;
						newSimObj.transform.parent = generic.transform.parent;
						newSimObj.name = newSimObj.name.Replace ("(Clone)", "");
						//destroy the old sim obj
						GameObject.DestroyImmediate (generic.gameObject);
						break;
					}
				}
			}
		}
	}

	public void SetUpLighting () {
		//thanks for not exposing these props, Unity :P
		//this may break in later versions
		var getLightmapSettingsMethod = typeof(LightmapEditorSettings).GetMethod("GetLightmapSettings", BindingFlags.Static | BindingFlags.NonPublic);
		var lightmapSettings = getLightmapSettingsMethod.Invoke(null, null) as Object;
		SerializedObject settingsObject = new SerializedObject(lightmapSettings);

		/*var iter = settingsObject.GetIterator();
		do {
			Debug.Log("member:  " + iter.name);
		} while (iter.Next(true));*/

		settingsObject.FindProperty ("m_GISettings.m_EnableBakedLightmaps").boolValue = false;
		settingsObject.FindProperty ("m_GISettings.m_EnableRealtimeLightmaps").boolValue = false;
		settingsObject.FindProperty ("m_LightmapEditorSettings.m_FinalGather").boolValue = false;
		settingsObject.FindProperty ("m_LightmapEditorSettings.m_LightmapsBakeMode").boolValue = false;

		settingsObject.ApplyModifiedProperties ();
	}

	public void SetUpNavigation () {
		//thanks for not exposing these props, Unity :P
		//this may break in later versions
		SerializedObject settingsObject = new SerializedObject (UnityEditor.AI.NavMeshBuilder.navMeshSettingsObject);

		/*var iter = settingsObject.GetIterator();
		do {
			Debug.Log("member:  " + iter.displayName);
		} while (iter.Next(true));*/

		settingsObject.FindProperty ("m_BuildSettings.agentRadius").floatValue = 0.05f;
		settingsObject.FindProperty ("m_BuildSettings.agentHeight").floatValue = .25f;
		settingsObject.FindProperty ("m_BuildSettings.agentSlope").floatValue = 10f;
		settingsObject.FindProperty ("m_BuildSettings.agentClimb").floatValue = 0f;
		settingsObject.FindProperty ("m_BuildSettings.ledgeDropHeight").floatValue = 0f;
		settingsObject.FindProperty ("m_BuildSettings.minRegionArea").floatValue = 0.05f;
		settingsObject.FindProperty ("m_BuildSettings.ledgeDropHeight").floatValue = 0.0f;
		settingsObject.FindProperty ("m_BuildSettings.maxJumpAcrossDistance").floatValue = 0.0f;
		settingsObject.FindProperty ("m_BuildSettings.cellSize").floatValue = 0.02f;
		settingsObject.FindProperty ("m_BuildSettings.manualCellSize").boolValue = true;

		settingsObject.ApplyModifiedProperties ();
	}

	public void SetUpFPSController () {
		if (FPSControllerPrefab == null) {
			Debug.LogError ("FPS controller prefab is not set in Scene Manager.");
			return;
		}
		GameObject fpsObj = GameObject.Find (FPSControllerPrefab.name);
		if (fpsObj == null) {
			fpsObj = PrefabUtility.InstantiatePrefab (FPSControllerPrefab) as GameObject;
			fpsObj.name = FPSControllerPrefab.name;
		} else {
			//re-attach to prefab
			GameObject prefabParent = PrefabUtility.GetCorrespondingObjectFromSource(fpsObj) as GameObject;
			if (prefabParent == null) {
				//if it's not attached to a prefab, delete and start over
				Vector3 pos = fpsObj.transform.position;
				GameObject.DestroyImmediate (fpsObj);
				fpsObj = PrefabUtility.InstantiatePrefab (FPSControllerPrefab) as GameObject;
				fpsObj.transform.position = pos;
			} else {
				PrefabUtility.RevertPrefabInstance(fpsObj, InteractionMode.AutomatedAction);
			}
		}
		fpsObj.transform.parent = null;
		fpsObj.transform.SetAsLastSibling ();

		//FirstPersonController fps = fpsObj.GetComponent <FirstPersonController> ();
		Rigidbody rb = fpsObj.GetComponent<Rigidbody> ();
		CharacterController cc = fpsObj.GetComponent <CharacterController> ();
		Camera cam = fpsObj.GetComponentInChildren<Camera> ();

		//values copied from scene doc
		cc.slopeLimit = 0f;
		cc.stepOffset = 0f;
		cc.skinWidth = 0.08f;
		cc.center = Vector3.zero;
		cc.radius = 0.2f;
		cc.height = 1.8f;

		rb.mass = 1f;
		rb.isKinematic = true;
		rb.interpolation = RigidbodyInterpolation.None;
		rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
		rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationY;

		cam.fieldOfView = 60f;
		cam.farClipPlane = 1000f;
		cam.nearClipPlane = 0.1f;

		//remove all other cameras in the scene
		//Camera[] cams = GameObject.FindObjectsOfType<Camera>();
		//foreach (Camera c in cams) {
		//	if (c != cam) {
		//		CameraControls controls = c.GetComponent <CameraControls> ();
		//		if (controls == null) {
		//			GameObject.DestroyImmediate (c.gameObject);
		//		}
		//	}
		//}
	}

	void FindOrCreateParent (ref Transform parentTransform, string parentName) {
		GameObject parentGo = GameObject.Find (parentName);
		if (parentGo == null) {
			parentGo = new GameObject (parentName);
		}
		//set to root just in case
		parentGo.transform.parent = null;
		parentTransform = parentGo.transform;
		parentTransform.SetAsLastSibling ();
	}
	#endif
}

public enum SceneAnimationMode {
	Instant,
	Smooth,
}

public enum ScenePhysicsMode {
	Static,
	Dynamic,
}

public enum SceneType {
	Kitchen,
	LivingRoom,
	Bathroom,
	Bedroom,
}
