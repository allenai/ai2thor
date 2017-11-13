using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

[CustomEditor(typeof(Receptacle))]
public class ReceptacleEditor : Editor 
{
	public override void OnInspectorGUI()
	{
		Receptacle r = (Receptacle)target;

		GUI.color = Color.grey;
		EditorGUILayout.BeginVertical (EditorStyles.helpBox);
		GUI.color = Color.white;
		EditorGUILayout.LabelField ("Visibility Collider:", EditorStyles.miniLabel);
		if (r.VisibilityCollider == null) {
			GUI.color = Color.Lerp (Color.red, Color.white, 0.5f);
			EditorGUILayout.TextArea ("You must define a visibility collider.\n" +
				"If the item does not close, this should be the main collider.\n" +
				"If the item closes, it should be a collider that is obscured when it is closed.", EditorStyles.miniLabel);
			
			CheckForDragDropColliders (r);
		} else {
			r.VisibilityCollider = (Collider)EditorGUILayout.ObjectField (r.VisibilityCollider, typeof(Collider), false);
		}
		EditorGUILayout.EndVertical ();

		//if the visibility collider is null, don't do anything else
		if (r.VisibilityCollider == null)
			return;

		GUI.color = Color.grey;
		EditorGUILayout.BeginVertical (EditorStyles.helpBox);
		GUI.color = Color.white;
		EditorGUILayout.LabelField ("Pivots:", EditorStyles.miniLabel);
		if (r.Pivots == null) {
			r.Pivots = new Transform[0];
		}
		if (r.Pivots.Length == 0) {
			GUI.color = Color.Lerp (Color.red, Color.white, 0.5f);
			EditorGUILayout.LabelField ("Receptacle has no pivots!");
		} else {
			int deletedPivotIndex = -1;
			//check for null objects in the pivot list
			for (int i = 0; i < r.Pivots.Length; i++) {
				if (r.Pivots [i] == null) {
					deletedPivotIndex = i;
					break;
				}
			}

			if (deletedPivotIndex < 0) {
				EditorGUILayout.BeginHorizontal ();
				for (int i = 0; i < r.Pivots.Length; i++) {
					GUI.color = Color.white;
					string pivotDesc = r.Pivots [i].name;
					string itemDesc = string.Empty;
					if (r.Pivots [i].childCount > 0) {
						GUI.color = Color.Lerp (Color.green, Color.white, 0.5f);
						if (r.Pivots [i].childCount > 1) {
							GUI.color = Color.Lerp (Color.red, Color.white, 0.5f);
							itemDesc = "Too many items under pivot!";
						} else {
							SimObj o = r.Pivots [i].GetChild (0).GetComponent<SimObj> ();
							if (o == null) {
								GUI.color = Color.Lerp (Color.red, Color.white, 0.5f);
								itemDesc = "Contained item is NOT a SimObj!";
							} else {
								itemDesc = r.Pivots [i].GetChild (0).name;
							}
						}
					} else {
						GUI.color = Color.white;
						itemDesc = "Empty\n(Drag-drop SimObj here to add to pivot)";
					}
					CheckForDragDropItems (r, r.Pivots [i], pivotDesc, itemDesc);
				}
				EditorGUILayout.EndHorizontal ();

				EditorGUILayout.BeginHorizontal ();
				for (int i = 0; i < r.Pivots.Length; i++) {
					CheckForClearAndDelete (r, i, ref deletedPivotIndex);
				}
				EditorGUILayout.EndHorizontal ();
			}

			if (deletedPivotIndex >= 0) {
				//remove the pivot from the array
				List<Transform> newPivotList = new List<Transform> (r.Pivots);
				newPivotList.RemoveAt (deletedPivotIndex);
				r.Pivots = newPivotList.ToArray ();
			}
		}
		if (GUILayout.Button ("Set pivots to UP")) {
			foreach (Transform pivot in r.Pivots) {
				Transform tempParent = pivot.parent;
				pivot.parent = null;
				pivot.up = Vector3.up;
				pivot.parent = tempParent;
			}
		}
		GUI.color = Color.white;
		CheckForDragDropPivots (r);
		EditorGUILayout.EndVertical ();

		GUI.color = Color.grey;
		EditorGUILayout.BeginVertical (EditorStyles.helpBox);
		GUI.color = Color.white;
		EditorGUILayout.LabelField ("Mess item:", EditorStyles.miniLabel);
		r.MessItem = (GameObject)EditorGUILayout.ObjectField (r.MessItem, typeof(GameObject), false);
		if (r.MessItem != null) {
			if (!r.MessItem.transform.IsChildOf (r.transform)) {
				r.MessItem = null;
			} else {
				r.IsClean = GUILayout.Toggle (r.IsClean, "Is clean");
			}
		}
		EditorGUILayout.EndVertical ();
	}

	void CheckForClearAndDelete(Receptacle r, int index, ref int deletedPivotIndex) {
		EditorGUILayout.BeginVertical ();
		Rect deleteArea = GUILayoutUtility.GetRect (
			                  100.0f,
			                  25.0f);
		GUI.color = Color.Lerp (Color.grey, Color.yellow, 0.5f);
		if (GUI.Button (deleteArea, "Delete pivot")) {
			if (EditorUtility.DisplayDialog ("Delete pivot", "Are you sure you want to delete pivot?", "Yes", "Cancel")) {
				deletedPivotIndex = index;
			}
		}
		Rect removeArea = GUILayoutUtility.GetRect (
			                  100.0f,
			                  25.0f);
		if (r.Pivots [index].childCount > 0) {
			GUI.color = Color.white;
		} else {
			GUI.color = Color.grey;
		}
		if (GUI.Button (removeArea, "Clear pivot") && r.Pivots[index].transform.childCount > 0) {
			Transform item = r.Pivots [index].GetChild (0);
			SimUtil.TakeItem (item.GetComponent<SimObj> ());
		}
		EditorGUILayout.EndVertical ();

	}

	void CheckForDragDropColliders(Receptacle r) {
		Event evt = Event.current;
		Rect dropArea = GUILayoutUtility.GetRect (
			100.0f,
			100.0f);
		GUI.Box (dropArea, "Drag-drop visibility collider here", EditorStyles.helpBox);

		switch (evt.type) {
		case EventType.DragUpdated:
		case EventType.DragPerform:
			if (!dropArea.Contains (evt.mousePosition))
				return;

			DragAndDrop.visualMode = DragAndDropVisualMode.Generic;

			if (evt.type == EventType.DragPerform) {
				DragAndDrop.AcceptDrag ();

				foreach (UnityEngine.Object draggedItem in DragAndDrop.objectReferences) {
					TryToSetVisibilityCollider (r, draggedItem);
				}
			}
			break;
		}
	}

	void TryToSetVisibilityCollider(Receptacle r, UnityEngine.Object potentialCollider) {
		GameObject coGo = (GameObject)potentialCollider;
		if (coGo == null) {
			Debug.Log ("Couldn't cast to gameobject, not adding");
			return;
		}
		Collider c = coGo.GetComponent <Collider> ();
		if (c == null) {
			Debug.Log ("No collider attached, not adding");
			return;
		}

		if (!c.transform.IsChildOf (r.transform) && coGo != r.gameObject) {
			Debug.Log ("Visibility collider must be the receptacle or a child of the receptacle, not adding");
		}

		r.VisibilityCollider = c;
	}

	void CheckForDragDropItems(Receptacle r, Transform pivot, string pivotDesc, string itemDesc) {
		Event evt = Event.current;

		EditorGUILayout.BeginVertical ();
		Rect labelArea = GUILayoutUtility.GetRect (
			                 100.0f,
			                 20.0f);
		GUI.Button (labelArea, pivotDesc, EditorStyles.toolbarButton);
		Rect dropArea = GUILayoutUtility.GetRect (
			                100.0f,
			                100.0f);
		GUI.Box (dropArea, itemDesc, EditorStyles.helpBox);
		EditorGUILayout.EndVertical ();

		switch (evt.type) {
		case EventType.DragUpdated:
		case EventType.DragPerform:
			if (!dropArea.Contains (evt.mousePosition))
				return;

			DragAndDrop.visualMode = DragAndDropVisualMode.Generic;

			if (evt.type == EventType.DragPerform) {
				DragAndDrop.AcceptDrag ();

				foreach (UnityEngine.Object draggedItem in DragAndDrop.objectReferences) {
					TryToAddItem (draggedItem, pivot, r);
				}
			}
			break;
		}
	}

	void TryToAddItem(UnityEngine.Object item, Transform pivot, Receptacle r) {
		if (pivot.childCount > 0) {
			Debug.Log ("Pivot is already filled, not adding item");
			return;
		}

		if (EditorUtility.IsPersistent (r)) {
			Debug.Log ("Can't add items to a recepticle when not in scene, not adding");
			return;
		}

		GameObject itemGo = null;
		try {
			itemGo = (GameObject)item;
		} catch (Exception e) {
			Debug.Log (e);
			return;
		}

		if (item == null) {
			Debug.Log ("Item was null, not adding");
			return;
		}

		if (itemGo == r.gameObject || itemGo.transform.IsChildOf (r.transform)) {
			Debug.Log ("Can't add item to itself, not adding");
			return;
		}

		if (EditorUtility.IsPersistent (itemGo)) {
			//instantiate the object
			Debug.Log ("Instantiating " + itemGo.name + " and placing in the scene");
			itemGo = GameObject.Instantiate (itemGo) as GameObject;
			return;
		}

		SimObj o = itemGo.GetComponent<SimObj> ();
		if (o == null) {
			Debug.Log ("Item was not a SimObj, not adding");
			return;
		}

		for (int i = 0; i < r.Pivots.Length; i++) {
			if (r.Pivots [i].childCount > 0) {
				foreach (Transform c in r.Pivots[i]) {
					if (c == itemGo.transform) {
						Debug.Log ("Item is already in a pivot, not adding");
						return;
					}
				}
			}
		}

		//if we've made it this far the item is OK
		//parent it under the receptacle
		SimUtil.AddItemToReceptacle (o, r);
		//don't scale the item
	}

	void CheckForDragDropPivots(Receptacle r) {
		Event evt = Event.current;
		EditorGUILayout.LabelField ("");
		Rect dropArea = GUILayoutUtility.GetRect (0.0f, 35.0f, GUILayout.ExpandWidth (true));
		GUI.Box (dropArea, "Drag-drop transforms here to add pivots", EditorStyles.helpBox);

		switch (evt.type) {
		case EventType.DragUpdated:
		case EventType.DragPerform:
			if (!dropArea.Contains (evt.mousePosition))
				return;

			DragAndDrop.visualMode = DragAndDropVisualMode.Generic;

			if (evt.type == EventType.DragPerform) {
				DragAndDrop.AcceptDrag ();

				foreach (UnityEngine.Object draggedTransform in DragAndDrop.objectReferences) {
					TryToAddPivot (draggedTransform, r);
				}
			}
			break;
		}
	}

	void TryToAddPivot (UnityEngine.Object potentialPivot, Receptacle r) {
		GameObject pivot = null;
		try {
			pivot = (GameObject)potentialPivot;
		} catch (Exception e) {
			//if we can't cast, forget it
			Debug.Log (e);
			return;
		}

		if (pivot == null) {
			Debug.Log ("Pivot is null, not accepting");
			return;
		}

		if (!pivot.transform.IsChildOf (r.transform)) {
			Debug.Log ("Pivot is not a child of receptcale, not accepting");
			return;
		}

		for (int i = 0; i < r.Pivots.Length; i++) {
			if (r.Pivots [i] == pivot.transform) {
				Debug.Log ("Pivot is already in list, not accepting");
				return;
			}
		}

		//we made it this far, the pivot must be legit
		Array.Resize(ref r.Pivots, r.Pivots.Length + 1);
		r.Pivots [r.Pivots.Length - 1] = pivot.transform;
	}
}