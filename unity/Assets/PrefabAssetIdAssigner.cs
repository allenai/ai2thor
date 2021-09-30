using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[ExecuteInEditMode]
public class PrefabAssetIdAssigner : MonoBehaviour
{
    public string SimObjectType;
    public List<GameObject> assetsOfSimObjectType = new List<GameObject>();

    public void GetAllPrefabsOfType() 
    {
        //var assetsOfSimObjectType = new List<GameObject>();
        string[] guids = AssetDatabase.FindAssets("t:prefab");

            for (int i = 0; i < guids.Length; i++) 
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                string assetName = assetPath.Substring(
                    assetPath.LastIndexOf("/") + 1,
                    assetPath.Length - (assetPath.LastIndexOf("/") + 1) - ".prefab".Length
                );

                // skip all these prefabs
                if (assetPath.Contains("Scene Setup Prefabs") || assetPath.Contains("Entryway Objects")) 
                {
                    continue;
                }

                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (asset != null && asset.GetComponent<SimObjPhysics>()) {

                    SimObjPhysics sop = asset.GetComponent<SimObjPhysics>();
                    if(sop.Type.ToString() == SimObjectType)
                    assetsOfSimObjectType.Add(asset);
                }
            }
    }

    public void AssignIds()
    {
        if (UnityEditor.EditorApplication.isPlaying)
            return;
        UnityEditor.Undo.RecordObject(gameObject, "descriptive name of this operation");
        /// make changes here
        UnityEditor.EditorUtility.SetDirty(gameObject);
        UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }

}

[CustomEditor (typeof(PrefabAssetIdAssigner))]
public class AssetIdAssigner : Editor
{
    public override void OnInspectorGUI () 
    {
        DrawDefaultInspector();
        PrefabAssetIdAssigner myScript = (PrefabAssetIdAssigner)target;
        if(GUILayout.Button("Get All Prefabs Of Type <something>"))
        {
            myScript.GetAllPrefabsOfType();
        }

        if(GUILayout.Button("Assign Prefab Name as assetID to All Prefabs Gotten Of Type"))
        {
            myScript.AssignIds();
        }
    }
}

#endif