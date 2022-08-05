using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityGLTF;

public class ConvertGLTF : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

#if UNITY_EDITOR

    [MenuItem("ConvertGLTF/Convert prefab models to glb format")]
    public static void ConvertToGLB() {
        // Find all assets labelled with 'architecture' :
        string[] prefab_guids = AssetDatabase.FindAssets("t:Prefab", new string[] {"Assets/Physics/SimObjsPhysics"});

        GLTFSceneExporter.SaveFolderPath = "/path/to/export/glb/folder/ai2thor_glb";
        Debug.Log(GLTFSceneExporter.SaveFolderPath);

        foreach (string prefab_guid in prefab_guids)
        {
            var prefab_path = AssetDatabase.GUIDToAssetPath(prefab_guid);
            GameObject game_object = AssetDatabase.LoadAssetAtPath<GameObject>(prefab_path);
            Debug.Log(game_object.name);

            MethodInfo export_method = typeof(GLTFExportMenu).GetMethod("Export", BindingFlags.NonPublic | BindingFlags.Static);
            Transform[] transforms = new Transform[] {game_object.transform};
            export_method.Invoke(null, new object[] {transforms, true, game_object.name});
        }
    }

# endif

}
