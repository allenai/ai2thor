using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class Prep_XML_Export : MonoBehaviour {
    [MenuItem("Tools/Update Scene For XML")]
    static void UpdateSceneForXML() {
        // Find all objects with SimObjPhysics components in the scene
        SimObjPhysics[] simObjPhysicsObjects = FindObjectsOfType<SimObjPhysics>();

        foreach (SimObjPhysics simObj in simObjPhysicsObjects) {
            // Check if the PrimaryProperty is set to "Static"
            if (simObj.PrimaryProperty == SimObjPrimaryProperty.Static) {
                // Check for CanToggleOnOff or CanOpenClose components
                var canToggleOnOff = simObj.GetComponent<CanToggleOnOff>();
                var canOpenClose = simObj.GetComponent<CanOpen_Object>();

                if (((canToggleOnOff != null && canToggleOnOff.MovingParts.Length > 0) ||
                    (canOpenClose != null && canOpenClose.MovingParts.Length > 0)) && simObj.Type != SimObjType.Blinds) {
                    //Debug.Log($"Skipping tag change for {simObj.gameObject.name} because it has populated MovingParts.");
                    continue; // Skip this object
                }

                Debug.Log($"Tagging {simObj.gameObject.name} and its children as 'Structure'");

                // Set the tag of the GameObject and all its children recursively
                SetTagRecursively(simObj.gameObject, "Structure", simObj);
            }
        }

        // Mark the scene as dirty to indicate changes
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    /// <summary>
    /// Recursively sets the tag of a GameObject and its children if they have a MeshFilter component
    /// or if they are the topmost SimObjPhysics component in the hierarchy.
    /// </summary>
    /// <param name="obj">The GameObject to tag.</param>
    /// <param name="tag">The tag to set.</param>
    /// <param name="topmostSimObj">The topmost SimObjPhysics component in the hierarchy.</param>
    static void SetTagRecursively(GameObject obj, string tag, SimObjPhysics topmostSimObj) {
        // Only update the tag if the GameObject has a MeshFilter component or is the topmost SimObjPhysics
        if (obj.GetComponent<MeshFilter>() != null || obj.GetComponent<SimObjPhysics>() == topmostSimObj) {
            // Record the object for undo and mark it as changed
            Undo.RecordObject(obj, "Change Tag");

            obj.tag = tag;
            Debug.Log($"Tag changed for GameObject: {obj.name} to '{tag}'");
        } else {
            //Debug.Log($"Skipping tag change for GameObject: {obj.name} (tag remains '{obj.tag}')");
        }

        // Recursively check children
        foreach (Transform child in obj.transform) {
            SetTagRecursively(child.gameObject, tag, topmostSimObj);
        }
    }
    
    [MenuItem("Tools/Delete All Placeable_Surface_Mat Meshes")]
    static void DeleteAllPlaceableSurfaceMatMeshes() {
        // Find all MeshRenderers in the scene
        MeshRenderer[] meshRenderers = FindObjectsOfType<MeshRenderer>(true);
        List<GameObject> objectsToDelete = new List<GameObject>();

        foreach (MeshRenderer mr in meshRenderers) {
            foreach (Material mat in mr.sharedMaterials) {
                if (mat != null && mat.name == "Placeable_Surface_Mat") {
                    objectsToDelete.Add(mr.gameObject);
                    break;
                }
            }
        }

        foreach (GameObject go in objectsToDelete) {
            Debug.Log($"Deleting GameObject '{go.name}' because it uses Placeable_Surface_Mat.");
            Undo.DestroyObjectImmediate(go);
        }

        // Mark the scene as dirty to indicate changes
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    // Add hotkey: %&r means Ctrl+Alt+R (Windows) or Cmd+Alt+R (Mac)
    [MenuItem("Tools/Reset Transform and Preserve Children %&r")]
    static void ResetTransformAndPreserveChildren() {
        if (Selection.activeGameObject == null) {
            Debug.LogWarning("No GameObject selected.");
            return;
        }

        GameObject selected = Selection.activeGameObject;
        Transform selectedTransform = selected.transform;

        // Debug log original rotation
        Debug.Log($"Original rotation of '{selected.name}': Quaternion={selectedTransform.rotation}, Euler={selectedTransform.eulerAngles}");

        // Store children in a list (no need to store world transforms if using worldPositionStays)
        List<Transform> children = new List<Transform>();
        foreach (Transform child in selectedTransform) {
            children.Add(child);
        }

        // Unparent all children
        foreach (Transform child in children) {
            Undo.SetTransformParent(child, null, "Unparent Child");
        }

        // Reset selected object's transform
        Undo.RecordObject(selectedTransform, "Reset Transform");
        selectedTransform.localScale = Vector3.one;
        selectedTransform.localRotation = Quaternion.identity; // Set local rotation to (0,0,0)

        // Debug log new rotation
        Debug.Log($"New rotation of '{selected.name}': Quaternion={selectedTransform.rotation}, Euler={selectedTransform.eulerAngles}");

        // Reparent all children using worldPositionStays = true to preserve world transforms
        foreach (Transform child in children) {
            Undo.SetTransformParent(child, selectedTransform, "Reparent Child");
            child.SetParent(selectedTransform, true); // true = worldPositionStays
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Transform reset and children preserved for: " + selected.name);
    }
}