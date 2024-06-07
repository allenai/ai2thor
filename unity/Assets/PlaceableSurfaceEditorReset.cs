using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
[ExecuteInEditMode]
public class PlaceableSurfaceEditorReset : MonoBehaviour
{
    public Dictionary<GameObject, string> assetToAssetPath = new Dictionary<GameObject, string>();

    public void GetAllSimObjPrefabs()
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
            if (
                assetPath.Contains("Scene Setup Prefabs")
                || assetPath.Contains("Entryway Objects")
                || assetPath.Contains("Custom Project Objects")
            )
            {
                continue;
            }

            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (asset != null && asset.GetComponent<SimObjPhysics>())
            {
                SimObjPhysics sop = asset.GetComponent<SimObjPhysics>();
                assetToAssetPath.Add(asset, assetPath);
            }
        }
    }

    public void ToggleOffPlaceableSurface()
    {
        GetAllSimObjPrefabs();

        foreach (KeyValuePair<GameObject, string> go in assetToAssetPath)
        {
            GameObject assetRoot = go.Key;
            string assetPath = go.Value;

            GameObject contentRoot = PrefabUtility.LoadPrefabContents(assetPath);

            //search all child objects and look for mesh renderers
            MeshRenderer[] renderers;
            renderers = contentRoot.GetComponentsInChildren<MeshRenderer>();

            bool shouldSave = false;

            //just in case something doesn't have a renderer?
            if (renderers.Length > 0)
            {
                foreach (MeshRenderer mr in renderers)
                {
                    if (mr.sharedMaterial != null)
                    {
                        if (
                            mr.sharedMaterial.ToString()
                            == "Placeable_Surface_Mat (UnityEngine.Material)"
                        )
                        {
                            mr.enabled = false;
                            shouldSave = true;
                            continue;
                        }
                    }
                }

                if (shouldSave)
                    PrefabUtility.SaveAsPrefabAsset(contentRoot, assetPath);
            }

            PrefabUtility.UnloadPrefabContents(contentRoot);
        }
    }

    public void ToggleOnPlaceableSurface()
    {
        GetAllSimObjPrefabs();

        foreach (KeyValuePair<GameObject, string> go in assetToAssetPath)
        {
            GameObject assetRoot = go.Key;
            string assetPath = go.Value;

            GameObject contentRoot = PrefabUtility.LoadPrefabContents(assetPath);

            //search all child objects and look for mesh renderers
            MeshRenderer[] renderers;
            renderers = contentRoot.GetComponentsInChildren<MeshRenderer>();

            bool shouldSave = false;

            //just in case something doesn't have a renderer?
            if (renderers.Length > 0)
            {
                foreach (MeshRenderer mr in renderers)
                {
                    if (mr.sharedMaterial != null)
                    {
                        if (
                            mr.sharedMaterial.ToString()
                            == "Placeable_Surface_Mat (UnityEngine.Material)"
                        )
                        {
                            mr.enabled = true;
                            shouldSave = true;
                            continue;
                        }
                    }
                }

                if (shouldSave)
                    PrefabUtility.SaveAsPrefabAsset(contentRoot, assetPath);
            }

            PrefabUtility.UnloadPrefabContents(contentRoot);
        }
    }

    [CustomEditor(typeof(PlaceableSurfaceEditorReset))]
    public class PlaceableSurfaceEditorThing : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            PlaceableSurfaceEditorReset myScript = (PlaceableSurfaceEditorReset)target;

            if (GUILayout.Button("Toggle Off Placeable Surface in Prefab Assets"))
            {
                myScript.ToggleOffPlaceableSurface();
            }

            if (GUILayout.Button("Toggle ON Placeable Surface in Prefab Assets"))
            {
                myScript.ToggleOnPlaceableSurface();
            }
        }
    }
}
#endif
