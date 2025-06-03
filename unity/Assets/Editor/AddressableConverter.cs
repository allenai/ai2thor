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
using System.Collections.Generic;
using System.Linq;

public class AddressableConverter : MonoBehaviour
{

     [MenuItem("Procedural/(Safe)Convert Procedural Assets to Addressables")]
    static void ConvertAssetsToAddressablesSafe()
    {
        var db = FindObjectOfType<ProceduralAssetDatabase>();
        if (db == null)
        {
            Debug.LogError("❌ ProceduralAssetDatabase not found in the scene!");
            return;
        }

        var settings = AddressableAssetSettingsDefaultObject.GetSettings(false);
        if (settings == null)
        {
            Debug.LogError("❌ Addressable Asset Settings not found. Please set up Addressables first.");
            return;
        }

        AddressableAssetGroup prefabGroup = GetOrCreateConfiguredGroup(settings, "ProceduralPrefabs");
        AddressableAssetGroup materialGroup = GetOrCreateConfiguredGroup(settings, "ProceduralMaterials");

        HashSet<string> processed = new HashSet<string>();
        List<string> failed = new List<string>();
        int successCount = 0;

        void AddToAddressables(UnityEngine.Object asset, AddressableAssetGroup group)
        {
            if (asset == null)
            {
                failed.Add("Null asset in list");
                return;
            }

            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path) || processed.Contains(path))
            {
                failed.Add($"{asset.name} — invalid or duplicate path");
                return;
            }

            if (asset is GameObject go)
            {
                if (!SanityCheckPrefab(go))
                {
                    failed.Add($"Prefab failed check: {asset.name}");
                    return;
                }
            }
            else if (asset is Material mat)
            {
                if (!SanityCheckMaterial(mat))
                {
                    failed.Add($"Material failed check: {asset.name}");
                    return;
                }
            }

            processed.Add(path);
            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group);
            entry.address = asset.name;
            successCount++;
            Debug.Log($"✔️ Added {asset.name} to Addressables group '{group.Name}'.");
        }

        foreach (var prefab in db.prefabs)
            AddToAddressables(prefab, prefabGroup);

        foreach (var mat in db.materials)
            AddToAddressables(mat, materialGroup);

        AssetDatabase.SaveAssets();

        Debug.Log($"✅ Finished: {successCount} assets added to Addressables.");

        if (failed.Count > 0)
        {
            Debug.LogWarning($"⚠️ {failed.Count} assets failed to convert:");
            foreach (var fail in failed)
                Debug.LogWarning($" - {fail}");
        }
    }

    static AddressableAssetGroup GetOrCreateConfiguredGroup(AddressableAssetSettings settings, string groupName)
    {
        var group = settings.FindGroup(groupName);
        if (group == null)
        {
            group = settings.CreateGroup(groupName, false, false, false, null, typeof(BundledAssetGroupSchema));
            Debug.Log($"📦 Created Addressables group: {groupName}");
        }

        var schema = group.GetSchema<BundledAssetGroupSchema>();
        if (schema == null)
        {
            schema = group.AddSchema<BundledAssetGroupSchema>();
        }

        // ✅ WebGL-friendly settings
        schema.Compression = BundledAssetGroupSchema.BundleCompressionMode.LZ4;
        schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
        schema.UseAssetBundleCache = false;
        schema.UseAssetBundleCrc = true;
        schema.UseUnityWebRequestForLocalBundles = true; // ✅ Ensures checkbox is checked
        schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
        schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);

        return group;
    }

    static bool SanityCheckPrefab(GameObject prefab)
    {
        try
        {
            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null) return false;
            GameObject.DestroyImmediate(instance);
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"❌ Sanity check failed for prefab '{prefab.name}': {ex.Message}");
            return false;
        }
    }

    static bool SanityCheckMaterial(Material mat)
    {
        if (mat.shader == null)
        {
            Debug.LogWarning($"❌ Material '{mat.name}' has a missing shader.");
            return false;
        }

        try
        {
            Shader shader = mat.shader;
            for (int i = 0; i < ShaderUtil.GetPropertyCount(shader); i++)
            {
                if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                {
                    string propName = ShaderUtil.GetPropertyName(shader, i);
                    Texture tex = mat.GetTexture(propName);
                    if (tex != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(tex)))
                    {
                        Debug.LogWarning($"❌ Material '{mat.name}' uses an invalid texture in '{propName}'.");
                        return false;
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"❌ Material check failed: {mat.name}: {ex.Message}");
            return false;
        }

        return true;
    }


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



        // mater
        // foreach (var prefab in db.prefabs.Where(p => p.name == "Doorway_1")) {
        //     AddToAddressables(prefab);
        // }


        // foreach (var prefab in db.prefabs.Where(p => includeOnly.Contains(p.name))) {
        //     AddToAddressables(prefab);
        // }

        // foreach (var mat in db.materials.Where(p => includeOnlyMats.Contains(p.name))) {
        //     AddToAddressables(mat);
        // }

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

    [MenuItem("Assets/Build Addressibles Content for WebGL")]
    static void BuildAddressables()
    {
        // For  manual Asset bundles
        // string assetBundleDirectory = "Assets/StreamingAssets/aa/WebGL";
        // if (!System.IO.Directory.Exists(assetBundleDirectory))
        // {
        //     System.IO.Directory.CreateDirectory(assetBundleDirectory);
        // }

        // BuildPipeline.BuildAssetBundles(
        //     assetBundleDirectory,
        //     BuildAssetBundleOptions.ChunkBasedCompression,
        //     BuildTarget.WebGL
        // );

        // Debug.Log("Finished building AssetBundles for WebGL.");

        AddressableAssetSettings.BuildPlayerContent();
        Debug.Log("Finished building Addressables for WebGL.");

        // AddressableAssetSettings.BuildPlayerContent();
    }


}
#endif
