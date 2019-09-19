using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(SimObj))]
public class SimObjectEditor : Editor 
{
	void OnSceneGUI () {
		SimObj simObj = (SimObj)target;

		if (SimUtil.ShowIDs) {
			Handles.color = Color.white;
			Handles.Label ((simObj.transform.position + Vector3.up * 0.1f), simObj.Type.ToString() + " : " + simObj.ObjectID.ToString (), EditorStyles.miniButton);
		}
		if (SimUtil.ShowBasePivots) {
			Handles.color = Color.white;
            //this was originally Handles.CircleCap, which is obsolete, had to add the EventType.Repaint parameter in upgrading to new CircleHandleCap implementation
            //not sure if the event type should be EventType.Repaint or EventType.Layout, but if Repaint isn't working change it to Layout
			Handles.CircleHandleCap (0, simObj.transform.position, Quaternion.Euler (-90f, 0f, 0f), 1f,EventType.Repaint); 
		}
	}

	public override void OnInspectorGUI()
	{
		SimObj simObj = (SimObj)target;

		GUI.color = Color.grey;
		EditorGUILayout.BeginVertical (EditorStyles.helpBox);
		GUI.color = Color.white;
		EditorGUILayout.LabelField ("Properties:", EditorStyles.miniLabel);
		if (!EditorUtility.IsPersistent (simObj)) {
			if (!SimUtil.IsObjectIDValid (simObj.ObjectID)) {
				GUI.color = Color.Lerp (Color.yellow, Color.white, 0.5f);
				EditorGUILayout.LabelField ("This object has no Object ID. Use SceneManager to gather / generate SimObj IDs.");
			} else {
				GUI.color = Color.white;
				EditorGUILayout.TextField ("Object ID:", simObj.ObjectID.ToString ());
			}
		}
		GUI.color = Color.white;
		simObj.Type = (SimObjType)EditorGUILayout.EnumPopup ("Object type:", simObj.Type);
		simObj.Manipulation = (SimObjManipType)EditorGUILayout.EnumPopup ("Manipulation type:", simObj.Manipulation);
		simObj.UseCustomBounds = EditorGUILayout.Toggle ("Use custom bounds", simObj.UseCustomBounds);
		if (!EditorUtility.IsPersistent (simObj)) {
			EditorGUILayout.Toggle ("Is a receptacle", simObj.IsReceptacle);
			EditorGUILayout.Toggle ("Is animated", simObj.IsAnimated);
			EditorGUILayout.Toggle ("Is animating now", simObj.IsAnimating);
		}
		if (simObj.UseCustomBounds) {
			GUI.color = Color.grey;
			EditorGUILayout.BeginVertical (EditorStyles.helpBox);
			GUI.color = Color.white;
			EditorGUILayout.LabelField ("Bounds dimensions:", EditorStyles.miniLabel);
			simObj.BoundsTransform = (Transform) EditorGUILayout.ObjectField (simObj.BoundsTransform, typeof(Transform), true);
			EditorGUILayout.EndVertical ();
		}
		if (Application.isPlaying) {
			if (GUILayout.Button ("Put back in original position")) {
				SimUtil.PutItemBackInStartupPosition (simObj);
			}
		}
		EditorGUILayout.EndVertical ();

		if (!string.IsNullOrEmpty (simObj.Error)) {
			GUI.color = Color.Lerp (Color.white, Color.red, 0.5f);
			EditorGUILayout.BeginVertical (EditorStyles.helpBox);
			EditorGUILayout.LabelField ("Error: " + simObj.Error);
			EditorGUILayout.EndVertical ();
		}

		if (!Application.isPlaying && !EditorUtility.IsPersistent(simObj)) {
			GUI.color = Color.grey;
			EditorGUILayout.BeginVertical (EditorStyles.helpBox);
			GUI.color = Color.white;
			EditorGUILayout.LabelField ("Utilities:", EditorStyles.miniLabel);
			bool showBasePivots = GUILayout.Toggle (SimUtil.ShowBasePivots, "Show base pivots");
			bool showIDs = GUILayout.Toggle (SimUtil.ShowIDs, "Show ID labels");
			bool showCustomBounds = GUILayout.Toggle (SimUtil.ShowCustomBounds, "Show custom bounds");
			bool showObjectVisibility = GUILayout.Toggle (SimUtil.ShowObjectVisibility, "Show object visibility");

			if (GUILayout.Button ("Set up base transform")) {
				GameObject newBaseObject = new GameObject ("Base");
				newBaseObject.transform.position = simObj.transform.position;
				newBaseObject.transform.rotation = simObj.transform.rotation;
				newBaseObject.transform.localScale = simObj.transform.localScale;
				MeshRenderer r = simObj.GetComponent<MeshRenderer> ();
				if (r != null) {
					MeshRenderer rc = newBaseObject.AddComponent<MeshRenderer> ();
					rc.sharedMaterials = r.sharedMaterials;
					MeshFilter mc = newBaseObject.AddComponent<MeshFilter> ();
					MeshFilter m = simObj.GetComponent<MeshFilter> ();
					mc.sharedMesh = m.sharedMesh;

					GameObject.DestroyImmediate (r);
					GameObject.DestroyImmediate (m);

					BoxCollider c = simObj.GetComponent<BoxCollider> ();
					if (c != null) {
						GameObject.DestroyImmediate (c);
						newBaseObject.AddComponent<BoxCollider> ();
					}

					//create a base object and parent everything under it
					List<Transform> children = new List<Transform>();
					foreach (Transform t in simObj.transform) {
						children.Add (t);
					}
					foreach (Transform t in children) {
						t.parent = newBaseObject.transform;
					}

					//reset the scale and rotation of the main object
					simObj.transform.localScale = Vector3.one;
					simObj.transform.localRotation = Quaternion.identity;

					newBaseObject.transform.parent = simObj.transform;
					newBaseObject.layer = simObj.gameObject.layer;
					newBaseObject.tag = SimUtil.SimObjTag;

					UnityEditor.Selection.activeGameObject = newBaseObject;

					simObj.RefreshColliders ();
				} else {
					//create a base object and parent everything under it
					List<Transform> children = new List<Transform>();
					foreach (Transform t in simObj.transform) {
						children.Add (t);
					}
					foreach (Transform t in children) {
						t.parent = newBaseObject.transform;
					}
					simObj.transform.localScale = Vector3.one;
					simObj.transform.localRotation = Quaternion.identity;
					newBaseObject.transform.parent = simObj.transform;
					newBaseObject.layer = simObj.gameObject.layer;
					newBaseObject.tag = SimUtil.SimObjTag;
				}
			}
			if (GUILayout.Button ("Fix base rotation/scale")) {
				Transform baseObj = simObj.transform.Find ("Base");
				if (baseObj == null) {
					Debug.LogError ("No base object found, not adjusting");
				} else {
					baseObj.transform.parent = null;
					simObj.transform.localScale = Vector3.one;
					simObj.transform.localRotation = Quaternion.identity;
					baseObj.transform.parent = simObj.transform;
				}
			}
			EditorGUILayout.EndVertical ();

			UnityEditor.EditorUtility.SetDirty (simObj);

			if (showBasePivots != SimUtil.ShowBasePivots
				|| showIDs != SimUtil.ShowIDs
				|| showCustomBounds != SimUtil.ShowCustomBounds
				|| showObjectVisibility != SimUtil.ShowObjectVisibility) {
				SimUtil.ShowBasePivots = showBasePivots;
				SimUtil.ShowIDs = showIDs;
				SimUtil.ShowCustomBounds = showCustomBounds;
				SimUtil.ShowObjectVisibility = showObjectVisibility;
				//force an editor repaint
				Repaint();
				SceneView.RepaintAll ();
			}
		}
	}
}