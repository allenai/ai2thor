using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

public class TextureLogger : MonoBehaviour {

    [MenuItem("Extras/Create Log of Material Uses Across Scenes")]
    static void LogMaterials() {
        // keep track of count of each object type across all objects
        Dictionary<Material, int> materialUsages = new Dictionary<Material, int>(); 

        // keep track of which materials exist in which objects
        Dictionary<Material, List<String>> MaterialsInTHESEBEESObjects = new Dictionary<Material, List<String>>();

        string[] materialGUIDs = AssetDatabase.FindAssets("t:Material");
        string[] materialPaths = new string[materialGUIDs.Length];


        // CAN YOU MAKE THIS DICTIONARY???????????
        Material[] materials = new Material[materialGUIDs.Length];

        for (int i = 0; i < materialGUIDs.Length; i++) {
            // create list of materials from project hierarchy
            materialPaths[i] = AssetDatabase.GUIDToAssetPath(materialGUIDs[i]);
            materials[i] = (Material)AssetDatabase.LoadAssetAtPath(materialPaths[i], typeof(Material));
            //Debug.Log(materialPaths[i]);
            Debug.Log(materials[i]);

            for (int j = 0; j < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; j++) {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(SceneUtility.GetScenePathByBuildIndex(i), OpenSceneMode.Single);
                
                foreach (var renderer in FindObjectsOfType<MeshRenderer>())
                {
                    if (renderer.sharedMaterials.Contains(materials[i]))
                        material
                }

            }

        }

        // check number of uses of each material across every scene

    }

    [MenuItem("Extras/Create Log of Texture Uses Across Materials")]
    static void LogTextures() {
        // keep track of which textures exist in which materials
        Dictionary<Texture, int> TexturesInAllMaterials = new Dictionary<Texture, int>(); 
    }
}
#endif