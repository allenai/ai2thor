using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/////////HOW TO USE///////////
/*
put the sim object type in the `SimObjectType` string field in this component on 
`PrefabAssetIdAssigner` in the Asset_Id_Assign scene

Then hit the button to load all prefabs in the project that are sim objects of that type
These assets will then be modified to include the prefab name in the `assetID` field
of the prefab so it can be written out to object metadata
*/


#if UNITY_EDITOR
[ExecuteInEditMode]
public class PrefabAssetIdAssigner : MonoBehaviour
{
    public string SimObjectType;
    public Dictionary<GameObject, string> assetToAssetPath = new Dictionary<GameObject, string>();

    public void GetAllPrefabsOfType() 
    {
        assetToAssetPath.Clear();

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
                assetToAssetPath.Add(asset, assetPath);
            }
        }
    }

    public void AssignIds()
    {
        GetAllPrefabsOfType();
        foreach (KeyValuePair<GameObject, string> go in assetToAssetPath) 
        {
            GameObject assetRoot = go.Key;
            string assetPath = go.Value;

            GameObject contentRoot = PrefabUtility.LoadPrefabContents(assetPath);

            //modify
            contentRoot.GetComponent<SimObjPhysics>().assetID = assetRoot.name;

            PrefabUtility.SaveAsPrefabAsset(contentRoot, assetPath);
            PrefabUtility.UnloadPrefabContents(contentRoot);
        }
    }
}

[CustomEditor (typeof(PrefabAssetIdAssigner))]
public class AssetIdAssigner : Editor
{
    public override void OnInspectorGUI () 
    {
        DrawDefaultInspector();
        PrefabAssetIdAssigner myScript = (PrefabAssetIdAssigner)target;
        // if(GUILayout.Button("Get All Prefabs Of Type <something>"))
        // {
        //     myScript.GetAllPrefabsOfType();
        // }

        if(GUILayout.Button("Assign Prefab Name as assetID to All Prefabs Gotten Of Type"))
        {
            myScript.AssignIds();
        }
    }
}

#endif