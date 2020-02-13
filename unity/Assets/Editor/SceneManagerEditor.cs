using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System;
using UnityEngine.SceneManagement;

[CustomEditor(typeof(SceneManager))]
public class SceneManagerEditor : Editor 
{
	public BuildTarget[] Targets = new BuildTarget[] {
#if UNITY_2017_3_OR_NEWER
                BuildTarget.StandaloneOSX,
#else
                BuildTarget.StandaloneOSXIntel,
                BuildTarget.StandaloneOSXIntel64,
#endif
		BuildTarget.StandaloneLinux64,
		BuildTarget.StandaloneLinux,
		BuildTarget.StandaloneLinuxUniversal,
		BuildTarget.StandaloneWindows,
		BuildTarget.StandaloneWindows64,
	};
	public SimObjType SelectionType;
	public SimObjType[] TypesNotFound;
	public SimObj[] CurrentlySelected;
	public bool ShowSceneObjects = true;
	public bool ShowRequiredTypeCheckResult = false;

	static bool showBuildOptions = true;
	static int sceneSelection;
	static string outputPath;
	static bool launchOnBuild = false;

	public override void OnInspectorGUI()
	{
		SceneManager sm = (SceneManager)target;

		GUI.color = Color.white;
		EditorGUILayout.LabelField ("Scene Options", EditorStyles.miniLabel);
		GUI.color = Color.grey;
		EditorGUILayout.BeginVertical (EditorStyles.helpBox);
		GUI.color = Color.white;
		sm.SceneNumber = EditorGUILayout.IntField ("Scene Number", sm.SceneNumber);
		sm.LocalSceneType = (SceneType)EditorGUILayout.EnumPopup ("Scene Type", sm.LocalSceneType);
		sm.LocalPhysicsMode = (ScenePhysicsMode)EditorGUILayout.EnumPopup ("Physics Mode", sm.LocalPhysicsMode);
		sm.AnimationMode = (SceneAnimationMode)EditorGUILayout.EnumPopup ("Animation Mode", sm.AnimationMode);

		EditorUtility.SetDirty (sm.gameObject);
		EditorUtility.SetDirty (sm);
		if (!Application.isPlaying) {
			UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
		}

		sm.FPSControllerPrefab = (GameObject) EditorGUILayout.ObjectField ("FPS controller prefab:", sm.FPSControllerPrefab, typeof(GameObject), false);
		EditorGUILayout.LabelField ("Actions:", EditorStyles.miniLabel);
		if (GUILayout.Button ("Check for required object types", GUILayout.MaxWidth(300))) {
			TypesNotFound = sm.CheckSceneForRequiredTypes ();
			ShowRequiredTypeCheckResult = true;
		}
		if (ShowRequiredTypeCheckResult) {
			if (TypesNotFound != null && TypesNotFound.Length > 0) {
				GUI.color = Color.Lerp (Color.red, Color.white, 0.5f);
				EditorGUILayout.LabelField ("The following required types were NOT found:", EditorStyles.boldLabel);
				for (int i = 0; i < TypesNotFound.Length; i++) {
					EditorGUILayout.LabelField (" - " + TypesNotFound [i].ToString (), EditorStyles.miniLabel);
				}
			} else {
				GUI.color = Color.Lerp (Color.green, Color.white, 0.5f);
				EditorGUILayout.LabelField ("All required types were found!", EditorStyles.boldLabel);
			}
			if (GUILayout.Button ("OK", GUILayout.MaxWidth(125))) {
				ShowRequiredTypeCheckResult = false;
			}
		}
		GUI.color = Color.white;
		if (GUILayout.Button ("Gather SimObjs and assign object IDs", GUILayout.MaxWidth(300))) {
			sm.GatherSimObjsInScene ();
		}
		if (GUILayout.Button ("Gather objects under parent folders", GUILayout.MaxWidth(300))) {
			sm.GatherObjectsUnderParents ();
		}
		/*if (GUILayout.Button ("Set up FPS controller", GUILayout.MaxWidth(300))) {
			sm.SetUpFPSController ();
		}*/
		if (GUILayout.Button ("Replace generics with platonic objects", GUILayout.MaxWidth(300))) {
			if (EditorUtility.DisplayDialog ("Confirm replace", "Are you SURE you want to do this?", "Yes", "Cancel")) {
				sm.ReplaceGenerics ();
				sm.GatherSimObjsInScene ();
			}
		}
		EditorGUILayout.EndVertical ();

		GUI.color = Color.grey;
		EditorGUILayout.BeginVertical (EditorStyles.helpBox);
		GUI.color = Color.white;

		EditorGUILayout.BeginHorizontal ();
		EditorGUILayout.LabelField ("Objects in scene:", EditorStyles.miniLabel);
		if (GUILayout.Button ("Show / Hide", EditorStyles.toolbarButton, GUILayout.MaxWidth(125))) {
			ShowSceneObjects = !ShowSceneObjects;
		}
		EditorGUILayout.EndHorizontal ();

		if (ShowSceneObjects) {
			if (sm.ObjectsInScene.Count == 0) {
				GUI.color = Color.Lerp (Color.white, Color.red, 0.5f);
				EditorGUILayout.LabelField ("(None found)");
			} else {
				HashSet<string> objectIDsSoFar = new HashSet<string> ();
				for (int i = 0; i < sm.ObjectsInScene.Count; i++) {
					if (sm.ObjectsInScene [i] == null) {
						Debug.Log ("Found null items in sm objects list, returning");
						sm.ObjectsInScene.Clear ();
						break;
					}
					if (!string.IsNullOrEmpty (sm.ObjectsInScene [i].Error)) {
						GUI.color = Color.Lerp (Color.red, Color.white, 0.5f);
					} else {
						GUI.color = Color.white;
					}
					string buttonText = string.Empty;
					if (!SimUtil.IsObjectIDValid (sm.ObjectsInScene [i].ObjectID)) {
						buttonText = sm.ObjectsInScene [i].Type.ToString () + ": (No ID)";
					} else {
						if (!objectIDsSoFar.Add (sm.ObjectsInScene [i].ObjectID)) {
							GUI.color = Color.Lerp (Color.red, Color.white, 0.5f);
							buttonText = "ERROR: DUPLICATE ID! " + sm.ObjectsInScene [i].ObjectID;
						} else {
							buttonText = sm.ObjectsInScene [i].ObjectID;
						}
					}
					if (GUILayout.Button (buttonText, EditorStyles.miniButton)) {
						UnityEditor.Selection.activeGameObject = sm.ObjectsInScene [i].gameObject;
					}
				}
			}
		}
		EditorGUILayout.EndVertical ();

		GUI.color = Color.grey;
		EditorGUILayout.BeginVertical (EditorStyles.helpBox);
		GUI.color = Color.white;
		EditorGUILayout.LabelField ("Item placement:", EditorStyles.miniLabel);
		if (GUILayout.Button ("Auto-assign navmesh layers", GUILayout.MaxWidth(300))) {
			sm.AutoStructureNavigation ();
		}
		EditorGUILayout.EndVertical ();


		GUI.color = Color.white;
		EditorGUILayout.LabelField ("Build Options", EditorStyles.miniLabel);
		GUI.color = Color.grey;
		EditorGUILayout.BeginVertical (EditorStyles.helpBox);
		GUI.color = Color.white;
		/*if (!showBuildOptions) {
			if (GUILayout.Button ("Show", EditorStyles.miniButton)) {
				showBuildOptions = true;
			}
		} else if (GUILayout.Button ("Hide", EditorStyles.miniButton)) {
			showBuildOptions = false;
		}*/

		string scenePath = string.Empty;
		string buildName = string.Empty;
#if UNITY_2017_3_OR_NEWER
                BuildTarget buildTarget = BuildTarget.StandaloneOSX;
#else
                BuildTarget buildTarget = BuildTarget.StandaloneOSXIntel;
#endif

		if (showBuildOptions) {
			//figure out what scene we want to build
			List<string> dropdownOptions = new List<string> ();
			foreach (UnityEngine.Object scene in sm.Scenes) {
				dropdownOptions.Add (scene.name);
			}
			dropdownOptions.Sort(new NaturalStringComparer());
			sceneSelection = EditorGUILayout.Popup ("Scene to build", sceneSelection, dropdownOptions.ToArray ());
			string sceneToBuild = dropdownOptions [sceneSelection];

			EditorGUILayout.BeginHorizontal ();
			if (string.IsNullOrEmpty (outputPath)) {
				if (string.IsNullOrEmpty (SimUtil.DefaultBuildDirectory)) {
					outputPath = Environment.GetFolderPath (Environment.SpecialFolder.Desktop);
				} else {
					outputPath = SimUtil.DefaultBuildDirectory;
				}
			}
			outputPath = EditorGUILayout.TextField ("Output Path", outputPath);
			if (GUILayout.Button ("Choose...")) {
				outputPath = EditorUtility.OpenFolderPanel ("Scene output path", outputPath, string.Empty);
			}
			EditorGUILayout.EndHorizontal ();

			launchOnBuild = EditorGUILayout.Toggle ("Launch on build", launchOnBuild);

			GUI.color = Color.yellow;
			foreach (BuildTarget bt in Targets) {
				if (GUILayout.Button ("BUILD -> " + bt.ToString())) {
					if (string.IsNullOrEmpty (outputPath) || !System.IO.Directory.Exists (outputPath)) {
						EditorUtility.DisplayDialog ("Invalid output path!", "Select a valid scene output path", "OK");
					} else {
						foreach (UnityEngine.Object sceneObject in sm.Scenes) {
							if (sceneObject.name == sceneToBuild) {
								scenePath = AssetDatabase.GetAssetPath (sceneObject);
								buildName = sceneObject.name.Replace (".unity", "");
							}
						}
					}
				}
			}
		}

		EditorGUILayout.EndVertical ();

		if (!string.IsNullOrEmpty (scenePath)) {
			SimUtil.BuildScene (scenePath, buildName, outputPath, buildTarget, launchOnBuild);
			return;
		}
	}
}

public class NaturalStringComparer : IComparer<string>
{
	private static readonly Regex _re = new Regex(@"(?<=\D)(?=\d)|(?<=\d)(?=\D)", RegexOptions.Compiled);

	public int Compare(string x, string y)
	{
		x = x.ToLower();
		y = y.ToLower();
		if(string.Compare(x, 0, y, 0, Math.Min(x.Length, y.Length)) == 0)
		{
			if(x.Length == y.Length) return 0;
			return x.Length < y.Length ? -1 : 1;
		}
		var a = _re.Split(x);
		var b = _re.Split(y);
		int i = 0;
		while(true)
		{
			int r = PartCompare(a[i], b[i]);
			if(r != 0) return r;
			++i;
		}
	}

	private static int PartCompare(string x, string y)
	{
		int a, b;
		if(int.TryParse(x, out a) && int.TryParse(y, out b))
			return a.CompareTo(b);
		return x.CompareTo(y);
	}
}
