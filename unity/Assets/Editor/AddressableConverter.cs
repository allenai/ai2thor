#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using System.Collections.Generic;
using Thor.Procedural;
using System;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections;

public class AddressableConverter : MonoBehaviour
{

    [MenuItem("Procedural/Convert Procedural Assets to Addressables")]
    static void ConvertAssetsToAddressables()
    {
        
        var db = FindObjectOfType<ProceduralAssetDatabase>();
        if (db == null)
        {
            Debug.LogError("ProceduralAssetDatabase not found in the scene!");
            return;
        }

        var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
        if (settings == null)
        {
            Debug.LogError("Addressable Asset Settings not found. Please set up Addressables first.");
            return;
        }

        AddressableAssetGroup group = settings.FindGroup("ProceduralAssets");
        if (group == null)
        {
            group = settings.CreateGroup("ProceduralAssets", false, false, false, null, typeof(BundledAssetGroupSchema));
        }

        HashSet<string> processed = new HashSet<string>();

        HashSet<string> failed = new HashSet<string>();

        void AddToAddressables(UnityEngine.Object asset)
        {
            if (asset == null) {
                return;
            }

            string path = AssetDatabase.GetAssetPath(asset);

            Debug.Log($"-- Asset ${asset.name} path ${path}");
            if (string.IsNullOrEmpty(path) || processed.Contains(path)) {
                failed.Add(asset.name);
                return;
            }

            processed.Add(path);

            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group);
            entry.address = asset.name;
            Debug.Log($"Added {asset.name} to Addressables.");
        }

        foreach (var prefab in db.prefabs) {
            AddToAddressables(prefab);
        }

        foreach (var mat in db.materials) {
            AddToAddressables(mat);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Finished converting assets to Addressables.");
        Debug.Log($"-- Failed ${string.Join(",", failed)}");
    }

     [MenuItem("Procedural/Test Convert to Addressables")]
    static void TestConverAssetsToAddressables() {
        var db = FindObjectOfType<ProceduralAssetDatabase>();
        db.BuildAssetMap();
        var map = db.GetPrefabMap();
        
        var key = "Sink_20";
        Debug.Log($"--------- ${map.Count()} ${map.ContainsKey(key)}");
        var asset = map.getAsset(key);
        
        Debug.Log($"-------- ${asset.name}");

        string path = AssetDatabase.GetAssetPath(asset);
        Debug.Log($"-- Asset ${asset.name} path ${path}");

        var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
        if (settings == null)
        {
            Debug.LogError("Addressable Asset Settings not found. Please set up Addressables first.");
            return;
        }

        AddressableAssetGroup group = settings.FindGroup("ProceduralAssets");
        if (group == null)
        {
            group = settings.CreateGroup("ProceduralAssets", false, false, false, null, typeof(BundledAssetGroupSchema));
        }
        
        var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group);
        entry.address = asset.name;
        AssetDatabase.SaveAssets();

        

    }

    private static IEnumerator loadAsset(string asset) {
        var handle = Addressables.LoadAssetAsync<GameObject>(asset);
        yield return handle;
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            GameObject obj = Instantiate(handle.Result);
            // loadedObjects.Add(obj);
           
        }
         Debug.Log("====== Finish loadAsset Coroutine");
    }

    [MenuItem("Procedural/Test Load to Addressables")]
    static void TestLoadAddressable() { 
        var db = FindObjectOfType<ProceduralAssetDatabase>();
        var key = "Sink_20";
        db.StartCoroutine(loadAsset(key));
        Debug.Log("====== Finish TestLoadAddressable");
    }


}
#endif
