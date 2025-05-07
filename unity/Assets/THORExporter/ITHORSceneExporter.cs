using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using UnityEngine.SceneManagement;
using System.Linq;

public class ITHORSceneExporter : MonoBehaviour
{
    [System.Serializable]
    class IThorObject
    {
        public string objectType;
        public string assetId;
        public Vector3 position;
        public Vector3 rotation;
        public bool kinematic;
    }

    [System.Serializable]
    class IThorScene
    {
        public List<IThorObject> objects = new List<IThorObject>();
        public List<IThorObject> structuralObjects = new List<IThorObject>();
    }

    public string saveFilename = "iTHOR_scene.json";

    private Dictionary<string, int> objectCount = new Dictionary<string, int>();
    private DownloadThorAssets downloadThorAssets = new DownloadThorAssets();

    void Start()
    {
        // Get the current active scene
        Scene currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        string floorPlanName = currentScene.name.Replace(" ", "");
        Debug.Log($"Exporting scene: {floorPlanName}");

        // Create scene data container
        IThorScene sceneData = new IThorScene();

        // Get all objects in scene
        sceneData.objects = GetObjects(floorPlanName);
        sceneData.structuralObjects = GetStructuralObjects(floorPlanName);

        // Save to JSON
        string savePath = Path.Combine("Assets/iTHOR", floorPlanName);
        Directory.CreateDirectory(savePath);
        
        string jsonPath = Path.Combine(savePath, $"{floorPlanName}.json");
        string json = JsonUtility.ToJson(sceneData, true);
        File.WriteAllText(jsonPath, json);
        Debug.Log($"Saved to {jsonPath}");

        // Log counts
        Debug.Log($"Exported {sceneData.objects.Count} objects");
        Debug.Log($"Exported {sceneData.structuralObjects.Count} structural objects");
    }

    private string ExportAsset(GameObject go, string objectId, string objectType, string floorPlanName)
    {
        if (!objectCount.ContainsKey(objectId))
            objectCount[objectId] = 0;

        string assetId = (objectId + "_" + objectCount[objectId])
            .Replace(" ", "")
            .Replace("(Instance)", "")
            .Replace("Instance", "");

        string exportPath = Path.Combine("Assets/iTHOR", floorPlanName);
        
        // Create all necessary directories
        Directory.CreateDirectory(Path.Combine("Assets/iTHOR", "Textures"));  // Textures directory moved up one level
        Directory.CreateDirectory(exportPath);  // Base directory
        Directory.CreateDirectory(Path.Combine(exportPath, objectType.Replace(" ", "")));  // Object type directory
        
        Debug.Log($"Created directories in: {exportPath}");

        // Get active mesh filters
        MeshFilter[] meshFilters = go.GetComponentsInChildren<MeshFilter>();
        List<MeshFilter> activeMeshFilters = new List<MeshFilter>();
        foreach (MeshFilter mf in meshFilters)
        {
            Debug.Log($"Processing mesh: {mf.mesh.name}");
            
            // Skip if mesh filter or renderer is invalid
            if (mf == null || mf.mesh == null)
                continue;
            
            MeshRenderer renderer = mf.GetComponent<MeshRenderer>();
            if (renderer == null || renderer.sharedMaterials == null || renderer.sharedMaterials.Length == 0)
                continue;
            
            // Skip if any materials are null
            bool hasNullMaterial = false;
            foreach (Material mat in renderer.sharedMaterials)
            {
                if (mat == null)
                {
                    hasNullMaterial = true;
                    break;
                }
            }
            if (hasNullMaterial)
                continue;

            if (mf.mesh.name.Contains("PS") && go.tag != "Structure")
                continue;

            if (mf.gameObject.activeSelf)
            {
                activeMeshFilters.Add(mf);
            }
        }

        MeshFilter goMeshFilter = go.GetComponent<MeshFilter>();
        if (goMeshFilter != null)
        {
            MeshRenderer renderer = goMeshFilter.GetComponent<MeshRenderer>();
            bool isValid = renderer != null && 
                          renderer.sharedMaterials != null && 
                          renderer.sharedMaterials.Length > 0 &&
                          !renderer.sharedMaterials.Any(m => m == null);
                          
            if (isValid)
            {
                activeMeshFilters.Add(goMeshFilter);
            }
        }
        Debug.Log($"Active mesh filters: {activeMeshFilters.Count}");

        // Handle bounding box and center
        Vector3 center = Vector3.zero;
        bool applyBoundingBox = false;
        bool saveSubMeshes = false;
        bool saveSubMeshTransform = false;
        bool saveCombinedSubmeshes = false;

        // Get the SimObjPhysics component
        if (go.tag == "SimObjPhysics")
        {
            SimObjPhysics simObjPhysics = go.GetComponent<SimObjPhysics>();
            AxisAlignedBoundingBox box = simObjPhysics.AxisAlignedBoundingBox;
            center = box.center;
            applyBoundingBox = true; 
            saveSubMeshes = true;
            saveSubMeshTransform = true;
            saveCombinedSubmeshes = true;
        }
        

        // Initialize DownloadThorAssets if not already initialized
        if (downloadThorAssets == null)
        {
            downloadThorAssets = new DownloadThorAssets();
        }
        downloadThorAssets.savePath = exportPath;

        // Export asset obj
        string relativeExportPath = Path.Combine(objectType.Replace(" ", ""), assetId);
        
        try
        {
            downloadThorAssets.SaveMeshes(
                relativeExportPath, 
                activeMeshFilters.ToArray(), 
                center, 
                applyBoundingBox, 
                saveSubMeshes, 
                saveSubMeshTransform, 
                saveCombinedSubmeshes//,
                //simObjPhysics
            );

            // Create materials directory if needed
            string materialsPath = Path.Combine(exportPath, "Materials");
            Directory.CreateDirectory(materialsPath);

            // Export asset mtl
            downloadThorAssets.SaveMaterials(relativeExportPath);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error exporting asset {objectId}: {e.Message}\n{e.StackTrace}");
        }

        objectCount[objectId]++;
        return assetId;
    }

    private List<IThorObject> GetObjects(string floorPlanName)
    {
        List<IThorObject> objects = new List<IThorObject>();
        
        // Find all objects with SimObjPhysics tag in the scene
        GameObject[] allObjects = GameObject.FindGameObjectsWithTag("SimObjPhysics");
        foreach (GameObject obj in allObjects)
        {
            if (!obj.activeSelf)
                continue;

            SimObjPhysics simObjPhysics = obj.GetComponent<SimObjPhysics>();
            Vector3 bbox_center = Vector3.zero;
            if (simObjPhysics != null)
            {
                string objectType = Enum.GetName(typeof(SimObjType), simObjPhysics.Type).Replace(" ", "");
                string objectId = obj.name
                    .Replace(" ", "")
                    .Replace("(Instance)", "")
                    .Replace("Instance", "");
                string assetId = simObjPhysics.assetID?.Replace(" ", "");
                Transform bbox = obj.transform.Find("BoundingBox");
                if (bbox != null)
                {
                    bbox_center = bbox.GetComponent<BoxCollider>().center;
                }
                // Export asset if it doesn't have an assetID
                if (string.IsNullOrEmpty(assetId))
                {
                    GameObject clone = Instantiate(obj);
                    clone.name = objectId;
                    Debug.Log($"No asset ID for {objectId}, exporting asset");
                    assetId = ExportAsset(clone, objectId, objectType, floorPlanName);
                    bbox_center = Vector3.zero;
                    Destroy(clone);
                }

                IThorObject thorObj = new IThorObject
                {
                    objectType = objectType,
                    assetId = assetId,
                    position = obj.transform.localPosition,
                    rotation = obj.transform.localEulerAngles,
                    kinematic = simObjPhysics.isStatic
                };
                objects.Add(thorObj);
                Debug.Log($"Added object: {obj.name} of type {thorObj.objectType}");
            }
        }

        return objects;
    }

    private List<IThorObject> GetStructuralObjects(string floorPlanName)
    {
        List<IThorObject> structuralObjects = new List<IThorObject>();
        
        // Find all objects with Structural tag in the scene
        GameObject[] allStructural = GameObject.FindGameObjectsWithTag("Structure");
        foreach (GameObject obj in allStructural)
        {
            if (!obj.activeSelf)
                continue;

            SimObjPhysics simObjPhysics = obj.GetComponent<SimObjPhysics>();
            string objectId = obj.name
                .Replace(" ", "")
                .Replace("(Instance)", "")
                .Replace("Instance", "");
            string objectType = "Structural";
            string assetId = objectId;

            GameObject clone = Instantiate(obj);
            clone.name = objectId;
            assetId = ExportAsset(clone, objectId, objectType, floorPlanName);
            Debug.Log($"Exported structural object: {obj.name} with assetId: {assetId}");
            Destroy(clone);
        

            IThorObject thorObj = new IThorObject
            {
                objectType = objectId,  // Use the object name as type for structural objects
                assetId = assetId,
                position = obj.transform.localPosition,
                rotation = obj.transform.localEulerAngles,
                kinematic = true
            };
            structuralObjects.Add(thorObj);
            Debug.Log($"Added structural object: {obj.name} with assetId: {assetId}");
        }

        return structuralObjects;
    }
}
