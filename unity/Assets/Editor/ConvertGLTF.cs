using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityGLTF;
using Newtonsoft.Json.Linq;

public class ComponentMetadata
{
    public string name;
    public string tag;
    public int layer;
    public bool isActive;
    public Transform transform;
}

namespace ExtensionMethods
{
    public static class MyExtensions
    {
    }
}

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

    // public static string DictToString<TKey, TValue> (this IDictionary<TKey, TValue> dictionary)
    // {
    //     return "{" + string.Join(",", dictionary.Select(kv => kv.Key + "=" + kv.Value).ToArray()) + "}";
    // }

#if UNITY_EDITOR

    [MenuItem("ConvertGLTF/Convert prefab models to glb format")]
    public static void ConvertToGLB() {
        // Find all assets labelled with 'architecture' :
        string[] prefab_guids = AssetDatabase.FindAssets("t:Prefab", new string[] {"Assets/Physics/SimObjsPhysics"});

        var path = EditorUtility.SaveFolderPanel("glTF Export Path", GLTFSceneExporter.SaveFolderPath, "");

        GLTFSceneExporter.SaveFolderPath = path;
        Debug.Log(GLTFSceneExporter.SaveFolderPath);

        foreach (string prefab_guid in prefab_guids)
        {
            var prefab_path = AssetDatabase.GUIDToAssetPath(prefab_guid);
            GameObject game_object = AssetDatabase.LoadAssetAtPath<GameObject>(prefab_path);
            Debug.Log(game_object.name);

            // export to glb file
            // MethodInfo export_method = typeof(GLTFExportMenu).GetMethod("Export", BindingFlags.NonPublic | BindingFlags.Static);
            // Transform[] transforms = new Transform[] {game_object.transform};
            // export_method.Invoke(null, new object[] {transforms, true, game_object.name});

            // export to metadata json file
            Transform[] children_objects;
            children_objects = game_object.GetComponentsInChildren<Transform>(true);
            JObject asset_metadata = new JObject();
            foreach (Transform child in children_objects) {
                GameObject child_object = child.gameObject;
                ComponentMetadata component = new ComponentMetadata();
                component.name = child_object.name;
                component.layer = child_object.layer;
                component.tag = child_object.tag;
                component.isActive = child_object.activeSelf;
                component.transform = child_object.transform;
                string component_json = JsonUtility.ToJson(component);
                asset_metadata[child_object.GetInstanceID().ToString()] = JObject.Parse(component_json);

                MonoBehaviour[] scripts;
                scripts = child_object.GetComponents<MonoBehaviour>();
                JObject script_metadata = new JObject();
                foreach (MonoBehaviour script in scripts) {
                    string script_name = script.GetType().Name;
                    string script_json = JsonUtility.ToJson(script);
                    script_metadata[script_name] = JObject.Parse(script_json);
                }
                if (script_metadata.Count > 0) {
                    asset_metadata[child_object.GetInstanceID().ToString()][typeof(MonoBehaviour).Name] = script_metadata;
                }
            }
            File.WriteAllText(path + "/" + game_object.name + ".json", asset_metadata.ToString());
        }
    }

# endif

}
