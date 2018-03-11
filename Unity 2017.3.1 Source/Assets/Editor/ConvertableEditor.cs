using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

[CustomEditor(typeof(Convertable))]
public class ConvertableEditor : Editor 
{
	public override void OnInspectorGUI ()
	{
		Convertable c = (Convertable)target;

		GUI.color = Color.grey;

		if (c.CurrentState < 0) {
			GUI.color = Color.red;
		}

		Animator a = c.GetComponent<Animator> ();
		if (a == null) {
			a = c.gameObject.AddComponent<Animator> ();
			a.runtimeAnimatorController = Resources.Load ("StateAnimController") as RuntimeAnimatorController;
		}

		EditorGUILayout.BeginVertical (EditorStyles.helpBox);
		GUI.color = Color.white;
		EditorGUILayout.LabelField ("States:", EditorStyles.miniLabel);

		c.DefaultState = EditorGUILayout.IntSlider ("Default State", c.DefaultState + 1, 1, 4) - 1;
		c.EditorState = EditorGUILayout.IntSlider ("Editor State", c.EditorState + 1, 1, 4) - 1;
		EditorGUILayout.LabelField ("Current state: " + (c.CurrentState + 1).ToString ());

		if (c.States == null || c.States.Length < 2) {
			c.States = new SimObjState[2];
			c.States [0] = new SimObjState ();
			c.States [1] = new SimObjState ();
			c.States [0].Type = c.GetComponent<SimObj> ().Type;
		}

		//name all state objects for their state
		foreach (SimObjState state in c.States) {
			if (state.Obj != null) {
				//state.Obj.name = state.Type.ToString ();
			}
		}

		List<GameObject> availableItems = new List<GameObject> ();
		Transform baseTransform = c.transform.Find ("Base");
		if (baseTransform == null) {
			GUI.color = Color.red;
			EditorGUILayout.LabelField ("MUST HAVE BASE OBJECT!");
		} else {
			foreach (Transform child in baseTransform) {
				availableItems.Add (child.gameObject);
			}

			List<string> choicesList = new List<string> ();
			foreach (GameObject availableItem in availableItems) {
				choicesList.Add (availableItem.name);
			}
			string[] choices = choicesList.ToArray ();

			int stateIndex = 0;
			foreach (SimObjState state in c.States) {
				GUI.color = Color.white;
				if (stateIndex == c.CurrentState) {
					GUI.color = Color.Lerp (Color.white, Color.green, 0.5f);
				}
				EditorGUILayout.BeginVertical (EditorStyles.helpBox);
				EditorGUILayout.LabelField ("State " + (stateIndex + 1).ToString());
				state.Type = (SimObjType)EditorGUILayout.EnumPopup ("Type", state.Type);
				int stateObjIndex = 0;
				if (state.Obj != null) {
					for (int i = 0; i < choices.Length; i++) {
						if (state.Obj.name.Equals (choices [i])) {
							stateObjIndex = i;
							break;
						}
					}
				}
				stateObjIndex = EditorGUILayout.Popup ("Object", stateObjIndex, choices);
				if (availableItems.Count > 0) {
					state.Obj = availableItems [stateObjIndex];
				}
				EditorGUILayout.EndVertical ();
				stateIndex++;
			}
		}
		EditorGUILayout.EndVertical ();


		GUI.color = Color.grey;
		EditorGUILayout.BeginVertical (EditorStyles.helpBox);
		GUI.color = Color.white;
		EditorGUILayout.LabelField ("Utilities:", EditorStyles.miniLabel);
		if (baseTransform.childCount == 0 && GUILayout.Button ("Create sub-type objects from base")) {
			GameObject newBaseObj = new GameObject ("Base");
			newBaseObj.transform.parent = baseTransform.parent;
			newBaseObj.transform.position = baseTransform.position;
			newBaseObj.transform.rotation = baseTransform.rotation;
			newBaseObj.transform.localScale = baseTransform.localScale;
			baseTransform.parent = newBaseObj.transform;
			baseTransform.name = c.States [0].Type.ToString ();
			c.States [0].Obj = baseTransform.gameObject;
			GameObject newStateObj = new GameObject ("State");
			newStateObj.transform.parent = baseTransform.parent;
			newStateObj.transform.position = baseTransform.position;
			newStateObj.transform.rotation = baseTransform.rotation;
			newStateObj.transform.localScale = baseTransform.localScale;
			c.States [1].Obj = newStateObj;
		}
		EditorGUILayout.EndVertical ();

		EditorUtility.SetDirty (c.gameObject);
		EditorUtility.SetDirty (c);
	}
}