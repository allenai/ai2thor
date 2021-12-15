// Save as Assets/Editor/ToggleGpuInstancing.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
 
class ToggleGpuInstancing
{
    [MenuItem("GPU Instancing/Enable")]
    static void EnableGpuInstancing()
    {
        SetGpuInstancing(true);
    }
 
    [MenuItem("GPU Instancing/Disable")]
    static void DisableGpuInstancing()
    {
        SetGpuInstancing(false);
    }

    [MenuItem("GPU Instancing/Check")]
    static void CheckInstancingIsEnabled() {
        var assetGuids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
        int enabledCount = 0;
        int disabledCount = 0;
        foreach (var guid in assetGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) {
                if (material.enableInstancing) enabledCount++;
                if (material.enableInstancing == false) disabledCount++;
                Debug.Log("Material doesn't have instancing enabled: " + material.name, material);
            }
        }
        
        Debug.Log("Final Report: " + enabledCount + "/" + assetGuids.Length + " enabled.");
    }
 
    static void SetGpuInstancing(bool value)
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                material.enableInstancing = value;
                EditorUtility.SetDirty(material);
            }
        }
        
        Debug.Log("Setting GPU Instancing on Materials to " + value.ToString() + " finished");
    }
}