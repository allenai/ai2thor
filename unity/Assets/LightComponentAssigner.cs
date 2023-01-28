using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[ExecuteInEditMode]
public class LightComponentAssigner : MonoBehaviour
{
    public Dictionary<GameObject, string> assetToAssetPath = new Dictionary<GameObject, string>();

    public void GetAllPrefabs()
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
            if (assetPath.Contains("Scene Setup Prefabs") || 
            assetPath.Contains("Entryway Objects") || 
            assetPath.Contains("SceneSetupPrefabs") || 
            assetPath.Contains("EntrywayObjects")) 
            {
                continue;
            }

            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (asset != null && asset.GetComponent<SimObjPhysics>()) {

                SimObjPhysics sop = asset.GetComponent<SimObjPhysics>();
                assetToAssetPath.Add(asset, assetPath);
            }
        }
    }

    public void AddWhatControlsThisComponent() {

        GetAllPrefabs();

        Debug.Log("here!");

        foreach (KeyValuePair<GameObject, string> go in assetToAssetPath) {
            GameObject asetRoot = go.Key;
            string assetPath = go.Value;

            Debug.Log("loading prefab");
            GameObject contentRoot = PrefabUtility.LoadPrefabContents(assetPath);

            //check if this object is toggleable
            SimObjPhysics rootSOP = contentRoot.GetComponent<SimObjPhysics>();

            bool didIMakeEdits = false;

            if (rootSOP.GetComponent<CanToggleOnOff>() && rootSOP.GetComponent<CanToggleOnOff>().LightSources.Length > 0) {
                Debug.Log($"found a toggleable thing with lights: {rootSOP.name}");
                didIMakeEdits = true;
                //find any referenced light objects this controls
                //for each light object this sim object controls, add the WhatControlsThis component
                //update WhatcontrolsThis component to reference the sim object
                foreach (Light l in rootSOP.GetComponent<CanToggleOnOff>().LightSources) {
                    WhatControlsThis wct = l.gameObject.AddComponent<WhatControlsThis>();
                    wct.SimObjThatControlsMe = rootSOP;
                }
            }

            //save edits
            if(didIMakeEdits)
            PrefabUtility.SaveAsPrefabAsset(contentRoot, assetPath);

            Debug.Log("about to unload prefab");
            PrefabUtility.UnloadPrefabContents(contentRoot);
        }
    }
}

[CustomEditor (typeof(LightComponentAssigner))]
public class EditorLightComponentAssigner : Editor 
{

    public override void OnInspectorGUI() {
        DrawDefaultInspector();
        LightComponentAssigner myScript = (LightComponentAssigner)target;

        if(GUILayout.Button("Assign WhatControlsThis component to Prefabs"))
        {
            myScript.AddWhatControlsThisComponent();
        }
    }

}

#endif
