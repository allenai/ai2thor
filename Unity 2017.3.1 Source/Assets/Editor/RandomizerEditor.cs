using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

[CustomEditor(typeof(Randomizer))]
public class RandomizerEditor : Editor 
{
	static Color newColor = Color.white;
	static bool showMatPreviews = false;

	public override void OnInspectorGUI()
	{
		if (Application.isPlaying) {
			return;
		}

		showMatPreviews = EditorGUILayout.Toggle ("Show mat previews", showMatPreviews);

		Randomizer r = (Randomizer)target;
		EditorGUILayout.BeginVertical (EditorStyles.helpBox);
		GUI.color = Color.white;
		r.Title = EditorGUILayout.TextField ("Title (for reference):", r.Title);
		r.Style = (RandomizerStyle) EditorGUILayout.EnumPopup ("Style", r.Style);
		r.UseLocalSceneNumber = EditorGUILayout.Toggle ("Use local scene number", r.UseLocalSceneNumber);
		int lastSceneNumber = r.SceneNumber;
		if (r.UseLocalSceneNumber) {
			r.SceneNumber = EditorGUILayout.IntSlider (r.SceneNumber, 0, 100);
		}
		if (r.SceneNumber != lastSceneNumber) {
			r.Randomize ();
		}
		EditorGUILayout.EndVertical ();

		GUI.color = Color.grey;
		EditorGUILayout.BeginVertical (EditorStyles.helpBox);
		GUI.color = Color.white;

		switch (r.Style) {
		case RandomizerStyle.GameObject:
			DisplayAddRemoveList (ref r.TargetGameObjects, "Target GameObjects:");
			break;

		case RandomizerStyle.MeshAndMat:
			DisplayAddRemoveList <MeshFilter> (ref r.TargetMeshes, "Target Meshes");
			DisplayAddRemoveList <Renderer> (ref r.TargetRenderers, "Target Renderers");
			DisplayMatIndexes (ref r.TargetMats, r.TargetRenderers);
			DisplayAddRemoveList <Material> (ref r.Mats, "Substitute Materials");
			break;

		case RandomizerStyle.MatColorFixed:
			DisplayAddRemoveList <Renderer> (ref r.TargetRenderers, "Target Renderers:");
			DisplayMatIndexes (ref r.TargetMats, r.TargetRenderers);
			DisplayColorList (ref r.Colors);
			break;

		case RandomizerStyle.MatColorRandom:
			DisplayAddRemoveList <Renderer> (ref r.TargetRenderers, "Target Renderers:");
			DisplayMatIndexes (ref r.TargetMats, r.TargetRenderers);
			r.ColorRangeLow = EditorGUILayout.ColorField ("Low Color Range", r.ColorRangeLow);
			r.ColorRangeHigh = EditorGUILayout.ColorField ("High Color Range", r.ColorRangeHigh);
			r.ColorSaturation = EditorGUILayout.Slider ("Saturation", r.ColorSaturation, 0f, 1f);
			EditorGUILayout.ColorField ("Random Color Preview:", Randomizer.GetRandomColor (r.SceneNumber, r.ColorRangeLow, r.ColorRangeHigh, r.ColorSaturation));
			break;
		}

		EditorGUILayout.EndVertical ();

		EditorUtility.SetDirty (r);
		EditorUtility.SetDirty (r.gameObject);
		if (!Application.isPlaying) {
			UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty (UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
		}
	}

	void DisplayColorList (ref Color[] colorArray) {
		EditorGUILayout.BeginVertical (EditorStyles.helpBox);
		EditorGUILayout.LabelField ("Colors:", EditorStyles.miniLabel);
		if (colorArray == null || colorArray.Length == 0) {
			colorArray = new Color[1];
			colorArray [0] = Color.white;
		}
		List<Color> itemList = new List<Color> (colorArray);
		int indexToRemove = -1;
		for (int i = 0; i < itemList.Count; i++) {
			EditorGUILayout.BeginHorizontal ();
			itemList [i] = EditorGUILayout.ColorField (itemList [i]);
			if (GUILayout.Button ("Remove")) {
				indexToRemove = i;
			}
			EditorGUILayout.EndHorizontal ();
		}
		if (indexToRemove >= 0) {
			itemList.RemoveAt (indexToRemove);
		}
		GUI.color = Color.white;
		EditorGUILayout.BeginHorizontal ();
		newColor = EditorGUILayout.ColorField ("Add color:", newColor);
		if (GUILayout.Button ("Click to add")) {
			itemList.Add (newColor);
		}
		EditorGUILayout.EndHorizontal ();
		colorArray = itemList.ToArray ();
		EditorGUILayout.EndVertical ();
	}

	void DisplayMatIndexes (ref int[] matIndexArray, Renderer[] renderers) {
		EditorGUILayout.BeginVertical (EditorStyles.helpBox);
		EditorGUILayout.LabelField ("Target Material Indexes:", EditorStyles.miniLabel);
		if (renderers == null || renderers.Length == 0) {
			EditorGUILayout.LabelField ("(No renderers)");
			return;
		}
		if (matIndexArray == null) {
			matIndexArray = new int[0];
		}
		if (matIndexArray.Length != renderers.Length) {
			Array.Resize <int> (ref matIndexArray, renderers.Length);
		}
		for (int i = 0; i < renderers.Length; i++) {
			Material[] sharedMats = renderers [i].sharedMaterials;
			string[] options = new string[sharedMats.Length];
			for (int j = 0; j < sharedMats.Length; j++) {
				string option = "(NULL)";
				if (sharedMats [j] != null) {
					option = sharedMats [j].name + "(" + j.ToString () + ")";
				}
				options [j] = option;
			}
			EditorGUILayout.BeginHorizontal ();
			int newMatIndex = EditorGUILayout.Popup (renderers[i].name, matIndexArray [i], options);
			if (showMatPreviews && sharedMats [newMatIndex] != null) {
				Editor matEditor = MaterialEditor.CreateEditor (sharedMats[newMatIndex]);
				matEditor.OnPreviewGUI (GUILayoutUtility.GetRect (25, 25), GUIStyle.none);
				GameObject.DestroyImmediate (matEditor);
			}
			matIndexArray [i] = newMatIndex;
			EditorGUILayout.EndHorizontal ();
		}
		EditorGUILayout.EndVertical ();
	}

	void DisplayAddRemoveList <T> (ref T[] itemArray, string header) where T : UnityEngine.Object {
		EditorGUILayout.BeginVertical (EditorStyles.helpBox);
		EditorGUILayout.LabelField (header, EditorStyles.miniLabel);
		if (itemArray == null) {
			itemArray = new T[0];
		}
		if (itemArray.Length == 0) {
			EditorGUILayout.LabelField ("(No items)");
		}
		List<T> itemList = new List<T> (itemArray);
		int indexToRemove = -1;
		bool showMatPreview = false;
		if (typeof(T) == typeof(Material)) {
			showMatPreview = true;
		}
		for (int i = 0; i < itemList.Count; i++) {
			if (showMatPreview) {
				EditorGUILayout.BeginHorizontal ();
			}
			if (GUILayout.Button (itemList [i].name)) {
				if (EditorUtility.DisplayDialog ("Confirm Remove", "Remove " + itemList [i].name + "?", "Yes", "Cancel")) {
					indexToRemove = i;
				}
			}
			if (showMatPreview) {
				Material mat = itemList [i] as Material;
				if (showMatPreviews && mat != null) {
					Editor matEditor = MaterialEditor.CreateEditor (mat);
					matEditor.OnPreviewGUI (GUILayoutUtility.GetRect (25, 25), GUIStyle.none);
					GameObject.DestroyImmediate (matEditor);
				}
				EditorGUILayout.EndHorizontal ();
			}
		}
		if (indexToRemove >= 0) {
			itemList.RemoveAt (indexToRemove);
		}
		GUI.color = Color.white;
		T newGameObject = (T)EditorGUILayout.ObjectField ("Add new:", null, typeof(T), true);
		if (newGameObject != null) {
			itemList.Add (newGameObject);
		}
		itemArray = itemList.ToArray ();
		EditorGUILayout.EndVertical ();
	}
}