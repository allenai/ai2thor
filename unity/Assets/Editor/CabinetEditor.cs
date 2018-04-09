using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

[CustomEditor(typeof(Cabinet))]
public class CabinetEditor : Editor 
{
	public override void OnInspectorGUI()
	{
		Cabinet c = (Cabinet)target;

		if (!Application.isPlaying) {
			c.Animate = EditorGUILayout.Toggle ("Animate", c.Animate);
			bool open = EditorGUILayout.Toggle ("Open", c.Open);
			if (open != c.Open) {
				c.Open = open;
				EditorUtility.SetDirty (c);
			}
		}

		if (c.ParentObj == null) {
			GUI.color = Color.red;
		} else {
			GUI.color = Color.grey;
		}
		EditorGUILayout.BeginVertical (EditorStyles.helpBox);
		GUI.color = Color.white;
		EditorGUILayout.LabelField ("Parent SimObj:", EditorStyles.miniLabel);
		c.ParentObj = (SimObj)EditorGUILayout.ObjectField (c.ParentObj, typeof(SimObj), true);
		EditorGUILayout.EndVertical ();
	

		GUI.color = Color.grey;
		EditorGUILayout.BeginVertical (EditorStyles.helpBox);
		GUI.color = Color.white;
		EditorGUILayout.LabelField ("Transforms:", EditorStyles.miniLabel);
		c.OpenStyle = (CabinetOpenStyle)EditorGUILayout.EnumPopup ("Open Style", c.OpenStyle);
		switch (c.OpenStyle) {
		case CabinetOpenStyle.DoubleDoors:
			c.LeftDoor = (Transform)EditorGUILayout.ObjectField ("Left door", c.LeftDoor, typeof(Transform), true);
			c.RightDoor = (Transform)EditorGUILayout.ObjectField ("Right door", c.RightDoor, typeof(Transform), true);
			c.OpenAngleLeft = EditorGUILayout.Vector3Field ("Open Angle (Left)", c.OpenAngleLeft);
			c.ClosedAngleLeft = EditorGUILayout.Vector3Field ("Closed Angle (Left)", c.ClosedAngleLeft);
			c.OpenAngleRight = EditorGUILayout.Vector3Field ("Open Angle (Right)", c.OpenAngleRight);
			c.ClosedAngleRight = EditorGUILayout.Vector3Field ("Closed Angle (Right)", c.ClosedAngleRight);
			break;

		case CabinetOpenStyle.SingleDoorLeft:
			c.LeftDoor = (Transform)EditorGUILayout.ObjectField ("Left door", c.LeftDoor, typeof(Transform), true);
			//c.RightDoor = null;
			c.OpenAngleLeft = EditorGUILayout.Vector3Field ("Open Angle", c.OpenAngleLeft);
			c.ClosedAngleLeft = EditorGUILayout.Vector3Field ("Closed Angle", c.ClosedAngleLeft);
			break;

		case CabinetOpenStyle.SingleDoorRight:
			c.RightDoor = (Transform)EditorGUILayout.ObjectField ("Right door", c.RightDoor, typeof(Transform), true);
			//c.LeftDoor = null;
			c.OpenAngleRight = EditorGUILayout.Vector3Field ("Open Angle", c.OpenAngleRight);
			c.ClosedAngleRight = EditorGUILayout.Vector3Field ("Closed Angle", c.ClosedAngleRight);
			break;

		case CabinetOpenStyle.Drawer:
			c.DrawerDoor = (Transform)EditorGUILayout.ObjectField ("Drawer", c.DrawerDoor, typeof(Transform), true);
			c.OpenLocalPosition = EditorGUILayout.Vector3Field ("Open Position (local)", c.OpenLocalPosition);
			c.ClosedLocalPosition = EditorGUILayout.Vector3Field ("Closed Position (local)", c.ClosedLocalPosition);

            //visibility collider needs to change scale and position if drawer is open or closed
            c.VisCollider = (Transform)EditorGUILayout.ObjectField("Visibility Collider", c.VisCollider, typeof(Transform), true);

                //open position and scale for the visibility collider, set this manually in editor
                c.OpenVisColPosition = EditorGUILayout.Vector3Field("Visibility Collider Open Position (local)", c.OpenVisColPosition);
                c.OpenVisColScale = EditorGUILayout.Vector3Field("Visibility Collider Open Scale (local)", c.OpenVisColScale);
                //closed position and scale for visibility collider, set this manually in editor
                c.ClosedVisColPosition = EditorGUILayout.Vector3Field("Visibility Collider Closed Position (local)", c.ClosedVisColPosition);
                c.ClosedVisColScale = EditorGUILayout.Vector3Field("Visibility Collider Closed Scale (local)", c.ClosedVisColScale);
			break;
		}

		EditorGUILayout.EndVertical ();

		GUI.color = Color.grey;
		EditorGUILayout.BeginVertical (EditorStyles.helpBox);
		GUI.color = Color.white;
		EditorGUILayout.LabelField ("Utilities:", EditorStyles.miniLabel);

		if (c.OpenStyle == CabinetOpenStyle.Drawer) 
        {
			if (GUILayout.Button ("Set drawer open position")) 
            {
				c.OpenLocalPosition = c.DrawerDoor.localPosition;
			}
			if (GUILayout.Button ("Set drawer closed position")) 
            {
				c.ClosedLocalPosition = c.DrawerDoor.localPosition;
			}

            /*
             * if(GUILayout.Button ("Set vis collider open position"))
            {
                c.OpenVisColPosition = c.VisCollider.localPosition;
            }

            if(GUILayout.Button ("Set vis collider closed position"))
            {
                c.ClosedVisColPosition = c.VisCollider.localPosition;
            }

            if(GUILayout.Button ("Set vis collider open scale"))
            {
                c.OpenVisColScale = c.VisCollider.localScale;
            }

            if(GUILayout.Button ("Set vis collider closed scale"))
            {
                c.ClosedVisColScale = c.VisCollider.localScale;
            }
            Because of where the visibility collider is parented, setting the positions with buttons doesn't work!
            */
		}

		if (c.ParentObj != null && c.ParentObj.Animator == null) 
        {
			if (GUILayout.Button ("Add animation controller to parent SimObj")) 
            {
				Animator a = c.ParentObj.GetComponent <Animator> ();
				if (a == null) 
                {
					a = c.ParentObj.gameObject.AddComponent<Animator> ();
				}

				a.runtimeAnimatorController = Resources.Load ("ToggleableAnimController") as RuntimeAnimatorController;
			}
		}

		if (GUILayout.Button ("Set ALL pivots to UP")) {
			SetAllPivotsToUp ();
		}

		if (GUILayout.Button ("Set ALL cabinet pivots to bottom-center")) {
			SetAllPivotsBottomCenter ();
		}

		if (GUILayout.Button ("Create cabinet from top-level door")) {
			CreateCabinetFromBase (c);
		}
		if (GUILayout.Button ("Create ALL cabinets from top-level door")) {
			Cabinet[] cabinets = GameObject.FindObjectsOfType<Cabinet> ();
			foreach (Cabinet cabinet in cabinets) {
				CreateCabinetFromBase(cabinet);
			}
		}

		EditorGUILayout.EndVertical ();
	}

	public void SetAllPivotsBottomCenter () {
		Cabinet[] cabinets = GameObject.FindObjectsOfType <Cabinet>();
		foreach (Cabinet cabinet in cabinets) {
			//if (cabinet.OpenStyle != CabinetOpenStyle.Drawer) {
			try {
				if (cabinet.ParentObj != null) {
					Receptacle r = cabinet.ParentObj.GetComponent<Receptacle> ();
					Transform pivot = r.Pivots [0];
					Collider vCollider = r.VisibilityCollider;
					pivot.transform.position = vCollider.bounds.center + (Vector3.down * vCollider.bounds.extents.y);
				}
			//}
			}catch (Exception e) {
				Debug.Log ("Error in cabinet " + cabinet.name + ": " + e.ToString ());
			}
		}
	}

	public void SetAllPivotsToUp () {
		Cabinet[] cabinets = GameObject.FindObjectsOfType <Cabinet>();
		foreach (Cabinet cabinet in cabinets) {
			if (cabinet.ParentObj != null) {
				Receptacle r = cabinet.ParentObj.GetComponent<Receptacle> ();
				Transform pivot = r.Pivots [0];
				Transform tempParent = pivot.parent;
				Transform child = null;
				if (pivot.childCount > 0) {
					child = pivot.GetChild (0);
					child.parent = null;

				}
				pivot.parent = null;
				pivot.up = Vector3.up;
				pivot.localScale = Vector3.one;
				if (child != null) {
					child.parent = pivot;
				}
				pivot.parent = tempParent;
			}
		}
	}

	public void CreateCabinetFromBase (Cabinet c) {
		SimObj parentObj = c.GetComponent <SimObj> ();
		if (parentObj == null) {
			parentObj = c.gameObject.AddComponent<SimObj> ();
		}
		parentObj.Type = SimObjType.Cabinet;

		GameObject baseObj = new GameObject ("Base");
		baseObj.transform.parent = c.transform;
		baseObj.transform.localPosition = Vector3.zero;
		baseObj.transform.localRotation = Quaternion.identity;
		baseObj.transform.localScale = Vector3.one;
		GameObject cabineObj = null;
		BoxCollider doorBc = null;

		//drawers and cabinets are very different so handle drawers here
		if (c.OpenStyle == CabinetOpenStyle.Drawer) {
			Transform drawerObj = null;
			if (c.DrawerDoor == null) {
				c.DrawerDoor = c.transform;
			}
			drawerObj = CopyDoor (c.DrawerDoor, baseObj.transform);
			cabineObj = drawerObj.gameObject;
			doorBc = drawerObj.GetComponent<BoxCollider> ();

			//set up the new cabinet script, then destroy the old one
			Cabinet newC = cabineObj.AddComponent<Cabinet> ();
			newC.OpenStyle = CabinetOpenStyle.Drawer;
			newC.OpenLocalPosition = c.OpenLocalPosition;
			newC.ClosedLocalPosition = c.ClosedLocalPosition;
			newC.DrawerDoor = drawerObj;

			//create a new pivot and visibility obj under the DRAWER object
			GameObject visibilityObj = new GameObject ("VisibilityCollider");
			visibilityObj.transform.parent = drawerObj.transform;
			visibilityObj.transform.localPosition = Vector3.zero;
			visibilityObj.transform.localRotation = Quaternion.identity;
			BoxCollider visBc = visibilityObj.AddComponent<BoxCollider> ();
			//copy box collider props from door
			visBc.center = doorBc.center;
			visBc.size = doorBc.size;
			//scale visBc to pull it back from front
			visBc.transform.localScale = new Vector3 (0.95f, 0.95f, 15f);

			GameObject pivotObj = new GameObject ("Pivot");
			pivotObj.transform.parent = drawerObj.transform;
			pivotObj.transform.position = visBc.bounds.center;
			pivotObj.transform.localRotation = Quaternion.identity;
			//add pivot to receptacle
			Receptacle r = c.GetComponent<Receptacle> ();
			if (r == null) {
				r = c.gameObject.AddComponent<Receptacle> ();
			}
			r.Pivots = new Transform[] { pivotObj.transform };
			r.VisibilityCollider = visBc;
			//destroy the old script
			UnityEditor.Selection.activeGameObject = newC.gameObject;
			c.name = "Drawer";
			newC.ParentObj = c.GetComponent<SimObj> ();
			GameObject.DestroyImmediate (c);
			newC.transform.localScale = Vector3.one;

			Animator a = newC.ParentObj.GetComponent <Animator> ();
			if (a == null) {
				a = newC.ParentObj.gameObject.AddComponent<Animator> ();
			}
			a.runtimeAnimatorController = Resources.Load ("ToggleableAnimController") as RuntimeAnimatorController;

		} else {
			//create a door object under the base that's a copy of the top-level object
			Transform leftDoorObj = null;
			Transform rightDoorObj = null;

			switch (c.OpenStyle) {
			case CabinetOpenStyle.SingleDoorLeft:
				if (c.LeftDoor == null) {
					c.LeftDoor = c.transform;
				}
				leftDoorObj = CopyDoor (c.LeftDoor, baseObj.transform);
				doorBc = leftDoorObj.GetComponent<BoxCollider> ();
				cabineObj = leftDoorObj.gameObject;
				break;

			case CabinetOpenStyle.SingleDoorRight:
				if (c.RightDoor == null) {
					c.RightDoor = c.transform;
				}
				rightDoorObj = CopyDoor (c.RightDoor, baseObj.transform);
				doorBc = rightDoorObj.GetComponent<BoxCollider> ();
				cabineObj = rightDoorObj.gameObject;
				break;

			case CabinetOpenStyle.DoubleDoors:
				//one of these objects is going to be a copy, the other will be the real thing
				if (c.LeftDoor == c.transform) {
					//copy the left door, parent the right door
					leftDoorObj = CopyDoor (c.LeftDoor, baseObj.transform);
					doorBc = leftDoorObj.GetComponent<BoxCollider> ();
					rightDoorObj = c.RightDoor;
					rightDoorObj.transform.parent = baseObj.transform;
					cabineObj = leftDoorObj.gameObject;
				} else {
					//copy the right door, parent the left door
					rightDoorObj = CopyDoor (c.RightDoor, baseObj.transform);
					doorBc = rightDoorObj.GetComponent<BoxCollider> ();
					leftDoorObj = c.LeftDoor;
					leftDoorObj.transform.parent = baseObj.transform;
					cabineObj = rightDoorObj.gameObject;
				}
				break;
			}
			//set up the new cabinet script, then destroy the old one
			Cabinet newC = cabineObj.AddComponent<Cabinet> ();
			newC.OpenStyle = c.OpenStyle;
			newC.OpenAngleLeft = c.OpenAngleLeft;
			newC.OpenAngleRight = c.OpenAngleRight;
			newC.ClosedAngleLeft = c.ClosedAngleLeft;
			newC.ClosedAngleRight = c.ClosedAngleRight;
			newC.LeftDoor = leftDoorObj;
			newC.RightDoor = rightDoorObj;
			//create a new pivot and visibility obj under the base object
			GameObject visibilityObj = new GameObject ("VisibilityCollider");
			visibilityObj.transform.parent = baseObj.transform;
			visibilityObj.transform.localPosition = Vector3.zero;
			visibilityObj.transform.localRotation = Quaternion.identity;
			BoxCollider visBc = visibilityObj.AddComponent<BoxCollider> ();
			//copy box collider props from door
			visBc.center = doorBc.center;
			visBc.size = doorBc.size;

			GameObject pivotObj = new GameObject ("Pivot");
			pivotObj.transform.parent = baseObj.transform;
			pivotObj.transform.position = visBc.bounds.center;
			pivotObj.transform.localRotation = Quaternion.identity;
			//add pivot to receptacle
			Receptacle r = c.GetComponent<Receptacle> ();
			if (r == null) {
				r = c.gameObject.AddComponent<Receptacle> ();
			}
			r.Pivots = new Transform[] { pivotObj.transform };
			r.VisibilityCollider = visBc;
			//destroy the old script
			UnityEditor.Selection.activeGameObject = newC.gameObject;
			c.name = "Cabinet";
			newC.ParentObj = c.GetComponent<SimObj> ();
			GameObject.DestroyImmediate (c);
			newC.transform.localScale = Vector3.one;

			Animator a = newC.ParentObj.GetComponent <Animator> ();
			if (a == null) {
				a = newC.ParentObj.gameObject.AddComponent<Animator> ();
			}
			a.runtimeAnimatorController = Resources.Load ("ToggleableAnimController") as RuntimeAnimatorController;
		}
	}

	public Transform CopyDoor (Transform doorTransform, Transform baseObj) {
		GameObject doorCopy = new GameObject ("Door");
		doorCopy.transform.parent = baseObj;
		doorCopy.transform.position = doorTransform.position;
		doorCopy.transform.rotation = doorTransform.rotation;
		doorCopy.transform.localScale = doorTransform.localScale;
		MeshFilter cmf = doorTransform.GetComponent<MeshFilter> ();
		MeshFilter dmf = doorCopy.AddComponent<MeshFilter> ();
		MeshRenderer cmr = doorTransform.GetComponent<MeshRenderer> ();
		MeshRenderer dmr = doorCopy.AddComponent<MeshRenderer> ();
		dmf.sharedMesh = cmf.sharedMesh;
		dmr.sharedMaterials = cmr.sharedMaterials;
		GameObject.DestroyImmediate (cmf);
		GameObject.DestroyImmediate (cmr);
		BoxCollider bc = doorTransform.GetComponent<BoxCollider> ();
		if (bc != null) {
			GameObject.DestroyImmediate (bc);
		}
		doorCopy.AddComponent<BoxCollider> ();

		//copy all child transforms (handles)
		List<Transform> childTransforms = new List<Transform>();
		foreach (Transform child in doorTransform) {
			childTransforms.Add (child);
		}
		foreach (Transform child in childTransforms) {
			child.parent = doorCopy.transform;
		}
		return doorCopy.transform;
	}
}