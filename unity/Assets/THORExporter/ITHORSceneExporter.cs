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
        public List<string> children;
    }

    [System.Serializable]
    class IThorMetadata
    {
        public Agent agent;
    }

    [System.Serializable]
    class Agent
    {
        public Vector3 position;
        public Vector3 rotation;
    }

    [System.Serializable]
    class iTHORLight
    {
        public string id;
        public string type;
        public float range;
        public Color rgb;
        public float intensity;
        //public float indirectMultiplier;
        public Vector3 position;
        public Vector3 direction;
        //public string shadow_type;
      //  public float shadow_strength;   
    }

    [System.Serializable]
    class IThorProceduralParams
    {
        public List<iTHORLight> lights;
        public string skyboxId;
    }

    [System.Serializable]
    class IThorScene
    {
        public List<IThorObject> objects = new List<IThorObject>();
        public List<IThorObject> structuralObjects = new List<IThorObject>();
        public IThorMetadata metadata = new IThorMetadata();
        public IThorProceduralParams proceduralParameters = new IThorProceduralParams();
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


        // FIRST PASS. GET ALL OBJECTS
        GameObject root = GameObject.Find("Objects");
        if (root == null)
        {
            Debug.LogError("Objects root not found");
            return;
        }
        
        // Get all objects in the root
        List<GameObject> objects = new List<GameObject>();
        List<GameObject> structuralObjects = new List<GameObject>();
        foreach (Transform child in root.transform)
        {
            if (child.gameObject.activeSelf)
            {
                if (child.gameObject.tag == "Structure")
                {
                    structuralObjects.Add(child.gameObject);

                    // go through all children and add them to the scene data
                    foreach (Transform grandChild in child)
                    {
                        if (grandChild.gameObject.activeSelf && grandChild.gameObject.tag == "SimObjPhysics")
                            objects.Add(grandChild.gameObject);
                        if (grandChild.gameObject.activeSelf && grandChild.gameObject.tag == "Structure")
                            structuralObjects.Insert(0, grandChild.gameObject);
                    }
                    
                    
                }
                else if (child.gameObject.tag == "SimObjPhysics")
                    objects.Add(child.gameObject);
            }
        }

        // SECOND PASS. Get INFO for all objects
        sceneData.objects = GetObjects(floorPlanName, objects);
        sceneData.structuralObjects = GetStructuralObjects(floorPlanName, structuralObjects);

        // Get the agent position and rotation
        GameObject agentObject = GameObject.Find("FPSController");
        if (agentObject != null)
        {
            sceneData.metadata.agent = new Agent
            {
                position = agentObject.transform.position,
                rotation = agentObject.transform.rotation.eulerAngles
            };
        }
        else
        {
            sceneData.metadata.agent = new Agent
            {
                position = Vector3.zero,
                rotation = Vector3.zero
            };
        }
        sceneData.metadata.agent.position.y = 0.0f; // For some reason, thor robot is lifted up


        // Get the procedural params
        // -- Get the lights 
        GameObject lightingObject = GameObject.Find("Lighting");
        if (lightingObject != null)
        {
            sceneData.proceduralParameters.lights = GetLights(lightingObject);
        }
        else
        {
            Debug.LogError("Lighting object not found");
        }


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

    private string ExportAsset(GameObject go, string tagType, string objectId, string objectType, string floorPlanName)
    {
        if (!objectCount.ContainsKey(objectId))
            objectCount[objectId] = 0;

        string assetId = objectId; //(objectId + "_" + objectCount[objectId])
            //.Replace(" ", "")
            //.Replace("(Instance)", "")
            //.Replace("Instance", "")
            //.Replace(".", "_");

        string exportPath = Path.Combine("Assets/iTHOR", floorPlanName);
        
        // Create all necessary directories
        Directory.CreateDirectory(Path.Combine("Assets/iTHOR", "Textures"));  // Textures directory moved up one level
        Directory.CreateDirectory(exportPath);  // Base directory
        Directory.CreateDirectory(Path.Combine(exportPath, objectType.Replace(" ", "")));  // Object type directory
        
        Debug.Log($"Created directories in: {exportPath}");

        // Get active mesh filters
        List<MeshFilter> activeMeshFilters = new List<MeshFilter>();
        if (false) //tagType == "Structure")
        {
            var meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
            {
                activeMeshFilters.Add(meshFilter);
            }
        }
        else
        {
            var meshFilters = go.GetComponentsInChildren<MeshFilter>();
            foreach (MeshFilter mf in meshFilters)
            {
                if (tagType == "Structure" && mf.gameObject.tag != tagType)
                    continue;

                
                // Skip if mesh filter or renderer is invalid
                if (mf == null || mf.mesh == null)
                    continue;
                
                Debug.Log($"Processing mesh: {mf.sharedMesh.name}");

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
                if (mf.mesh.name.Contains("Quad") && go.tag != "Structure")
                    continue;
                if (mf.gameObject.name.Contains("Decal") && go.tag != "Structure")
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

        }

        // Initialize DownloadThorAssets if not already initialized
        if (downloadThorAssets == null)
        {
            downloadThorAssets = new DownloadThorAssets();
        }
        // Handle bounding box and center
        Vector3 center = Vector3.zero;
        bool applyBoundingBox = false;
        bool saveSubMeshes = false;
        bool saveSubMeshTransform = false;
        bool saveCombinedSubmeshes = true;
        downloadThorAssets.applyScale = true; 

        // Get the SimObjPhysics component
        if (go.tag == "SimObjPhysics")
        {
            SimObjPhysics simObjPhysics = go.GetComponent<SimObjPhysics>();
            //AxisAlignedBoundingBox box = simObjPhysics.AxisAlignedBoundingBox;
            //center = box.center;
            applyBoundingBox = false;//true; 
            saveSubMeshes = true;
            saveSubMeshTransform = true;
            saveCombinedSubmeshes = true;
            downloadThorAssets.applyScale = false;
        }
        

        
        downloadThorAssets.savePath = exportPath;

        // Export asset obj
        string relativeExportPath = Path.Combine(objectType.Replace(" ", ""), assetId);
        
        try
        {
            var simObjPhysics = go.GetComponent<SimObjPhysics>();
            downloadThorAssets.SaveMeshes(
                relativeExportPath, 
                activeMeshFilters.ToArray(), 
                center, 
                applyBoundingBox, 
                saveSubMeshes, 
                saveSubMeshTransform, 
                saveCombinedSubmeshes,
                simObjPhysics
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

    private List<IThorObject> GetObjects(string floorPlanName, List<GameObject> allObjects)
    {        
        List<IThorObject> objects = new List<IThorObject>();
        
        // All Relative Positive from here
        var rootParent = GameObject.Find("Objects");


        // Find all objects with SimObjPhysics tag in the scene
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
                    .Replace("Instance", "")
                    .Replace(".", "_");
                
                var position = obj.transform.position;

                string assetId = simObjPhysics.assetID?.Replace(" ", "");
                // Export asset if it doesn't have an assetID
                if (string.IsNullOrEmpty(assetId))
                {
                    //GameObject clone = Instantiate(obj);
                    //clone.name = objectId;
                    Debug.Log($"No asset ID for {objectId}, exporting asset");
                    var tagType = obj.tag;
                    assetId = ExportAsset(obj,  tagType, objectId, objectType, floorPlanName);
                    bbox_center = Vector3.zero;
                    //Destroy(clone);
                }
                else
                {
                    
                    //Transform bbox = obj.transform.Find("BoundingBox");
                    AxisAlignedBoundingBox bbox = simObjPhysics.AxisAlignedBoundingBox;
                    if (bbox == null) 
                    {
                        // find componet from children 
                        BoxCollider bbox1 = obj.GetComponentInChildren<BoxCollider>();
                        if (bbox1 != null)
                        {
                            if (obj.name.Contains("Laptop"))
                            {
                                break;
                            }
                            bbox_center = bbox1.center;
                            position.y = bbox_center.y;
                        }
                    }
                    if (bbox != null)
                    {
                        //bbox_center.y = bbox.GetComponent<BoxCollider>().center.y; // For some reason, THOR assets need this
                        //bbox_center.y = bbox.GetComponent<BoxCollider>().size.y/2.0f; // For some reason, THOR assets need this
                        bbox_center.y = bbox.center.y;
                        position.y = bbox_center.y;
                    }
                }                

                var children = new List<string>();
                //var position = rootParent.transform.InverseTransformPoint(obj.transform.position);
                IThorObject thorObj = new IThorObject
                {
                    objectType = objectType,
                    assetId = assetId,
                    position = position, //obj.transform.position + bbox_center, // position,
                    rotation = WrapEulerAngles(obj.transform.rotation.eulerAngles), // Wrap the angles
                    kinematic = false, //simObjPhysics.isStatic,
                    children = children,
                };
                objects.Add(thorObj);
                Debug.Log($"Added object: {obj.name} of type {thorObj.objectType}");
            }
        }

        return objects;
    }

    private List<IThorObject> GetStructuralObjects(string floorPlanName, List<GameObject> allStructural)
    {
        List<IThorObject> structuralObjects = new List<IThorObject>();
        
        // Find all objects with Structural tag in the scene
        foreach (GameObject obj in allStructural)
        {
            if (!obj.activeSelf)
                continue;

            //SimObjPhysics simObjPhysics = obj.GetComponent<SimObjPhysics>();
            string objectId = obj.name
                .Replace(" ", "")
                .Replace("(Instance)", "")
                .Replace("Instance", "")
                .Replace(".", "_");
            string objectType = "Structural";
            string assetId = objectId;

            //GameObject clone = Instantiate(obj);
            //clone.name = objectId;
            var tagType = obj.tag;
            assetId = ExportAsset(obj, tagType, objectId, objectType, floorPlanName);
            Debug.Log($"Exported structural object: {obj.name} with assetId: {assetId}");
            //Destroy(clone);
        

            var children = new List<string>();
            foreach (Transform child in obj.transform)
            {
                if (child.gameObject.activeSelf && (child.gameObject.tag == "SimObjPhysics" )|| child.gameObject.tag == "Structure")
                    // Add child names to the list
                {
                    children.Add(child.gameObject.name);
                }
            }

            IThorObject thorObj = new IThorObject
            {
                objectType = objectId,  // Use the object name as type for structural objects
                assetId = assetId,
                position = Vector3.zero,//obj.transform.localPosition,
                rotation = Vector3.zero,//obj.transform.localEulerAngles,
                kinematic = true,
                children = children,
            };
            structuralObjects.Add(thorObj);
            Debug.Log($"Added structural object: {obj.name} with assetId: {assetId}");
        }

        return structuralObjects;
    }

    // Add these utility methods to wrap angles between -180 and 180 degrees
    private Vector3 WrapEulerAngles(Vector3 angles)
    {
        // Wrap each component between -180 and 180 degrees
        angles.x = WrapAngle(angles.x);
        angles.y = WrapAngle(angles.y);
        angles.z = WrapAngle(angles.z);
        return angles;
    }

    private float WrapAngle(float angle)
    {
        // Wrap the angle between -180 and 180 degrees
        angle %= 360f;
        if (angle > 180f)
            angle -= 360f;
        else if (angle < -180f)
            angle += 360f;
        return angle;
    }


    private List<iTHORLight> GetLights(GameObject lightingObject)
    {

        var children = lightingObject.GetComponentsInChildren<Light>();
        Debug.Log($"Found {children.Length} lights");

       // Get all lights in the scene
        List<iTHORLight> lights = new List<iTHORLight>();
        foreach (Light light in children)
        {
            iTHORLight iThorLight = new iTHORLight
                    {
                        id = light.name,
                        type = light.type.ToString(),
                        range = light.range,
                        rgb = light.color,
                        intensity = light.intensity,
                        //indirectMultiplier = light.indirectMultiplier,
                        position = light.transform.position,
                        direction = light.transform.forward, //.eulerAngles, 
                        //shadow_type = light.shadowType,
                        //shadow_strength = light.shadowStrength
                    };
            lights.Add(iThorLight);
            
        }
        return lights;
    }
}
