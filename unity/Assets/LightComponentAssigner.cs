using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

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

    //editor function to add "WhatControlsThis" to all light child objects in prefabs
    public void AddWhatControlsThisComponent() {

        GetAllPrefabs();
        foreach (KeyValuePair<GameObject, string> go in assetToAssetPath) {
            GameObject asetRoot = go.Key;
            string assetPath = go.Value;

            GameObject contentRoot = PrefabUtility.LoadPrefabContents(assetPath);
            Debug.Log($"loading prefab: {contentRoot.name}");

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
                    WhatControlsThis wct;
                    if(!l.gameObject.GetComponent<WhatControlsThis>()) {
                        wct = l.gameObject.AddComponent<WhatControlsThis>();

                    }
                    else {
                        wct = l.gameObject.GetComponent<WhatControlsThis>();
                        Array.Resize(ref wct.SimObjsThatControlsMe, wct.SimObjsThatControlsMe.Length + 1);
                        wct.SimObjsThatControlsMe[wct.SimObjsThatControlsMe.Length - 1] = rootSOP;
                    }
                }
            }

            //save edits
            if(didIMakeEdits)
            PrefabUtility.SaveAsPrefabAsset(contentRoot, assetPath);

            Debug.Log("about to unload prefab");
            PrefabUtility.UnloadPrefabContents(contentRoot);
        }
    }

    public void AddWhatControlsThisComponentToSceneLights() {
        //open every scene
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++) {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(SceneUtility.GetScenePathByBuildIndex(i), OpenSceneMode.Single);
            SimObjPhysics[] objects = GameObject.FindObjectsOfType<SimObjPhysics>(true);

            foreach (SimObjPhysics sop in objects) {
                //find all sim objs that are toggleable with light sources
                if(sop.GetComponent<CanToggleOnOff>() && sop.GetComponent<CanToggleOnOff>().LightSources.Length >0) {
                    //check if the `WhatControlsThis component is already here or not
                    foreach(Light l in sop.GetComponent<CanToggleOnOff>().LightSources) {
                        WhatControlsThis wct;
                        if(!l.gameObject.GetComponent<WhatControlsThis>()) {
                            wct = l.gameObject.AddComponent<WhatControlsThis>();

                        }
                        else {
                            wct = l.gameObject.GetComponent<WhatControlsThis>();
                            Array.Resize(ref wct.SimObjsThatControlsMe, wct.SimObjsThatControlsMe.Length + 1);
                            wct.SimObjsThatControlsMe[wct.SimObjsThatControlsMe.Length - 1] = sop;
                        }
                    }
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
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

        if(GUILayout.Button("Assign WhatControlsThis component to Scene Lights"))
        {
            myScript.AddWhatControlsThisComponentToSceneLights();
        }
    }

}

#endif
