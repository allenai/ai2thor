
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;
using System;
using UnityEngine.SceneManagement;



public class RoboThorJson : MonoBehaviour
{        
    [System.Serializable]
    class RoboThorObject
    {
        public string objectType;
        public string assetId;
        public Vector3 position;
        public Vector3 rotation;
        public bool kinematic;
    }


    [System.Serializable]
    class ProceduralParams
    {
        public float floorColliderThickness;
        public List<Light> lights;
        public float reflections;
        public string ceilingMaterial;
        public string skyboxId;
    }

    [System.Serializable]
    class RoboThorRoom
    {
        public List<RoboThorObject> objects;
        public List<RoboThorObject> rooms; //TODO
        public List<RoboThorObject> walls;
        public List<RoboThorObject> doors;
        public List<RoboThorObject> windows;
        public ProceduralParams proceduralParameters;
        public RoboThorObject metadata;
       
    }

    [System.Serializable]
    class Light
    {
        public string id;
        public string type;
        public float range;
        public float[] color;
        public float intensity;
        public float indirectMultiplier;
        public string position;
        public string rotation;
        public string shadow_type;
        public float shadow_strength;   
    }

    public string save_filename = "RoboTHOR_objects.json";
    Dictionary<string, int> object_count = new Dictionary<string, int>();
    DownloadThorAssets downloadThorAssets = new DownloadThorAssets();

    void Start()
    {
        string savePath = "Assets";
        
        // Create a new JSON object
        RoboThorRoom room = new RoboThorRoom();
    
        room.objects = GetObjects();


        // Save the JSON object to a file
        string json = JsonUtility.ToJson(room, true);
        File.WriteAllText(Path.Combine(savePath, save_filename), json);
        Debug.Log("saved to " + Path.Combine(savePath, save_filename));
    }


    void GetRooms()
    {
        // floor polygons. usually skip in RoboTHOR and iTHOR as they have mesh renderers
    }

    string _exportAsset(GameObject go, string objectId, string objectType)
    {
        if (!object_count.ContainsKey(objectId))
            object_count[objectId] = 0;

        string assetId = objectId + "_" + object_count[objectId];
        string ExportPath = Path.Combine("Assets", save_filename.Replace(".json", "")); //, sceneName);
        Debug.Log(Directory.Exists(ExportPath));
        Directory.CreateDirectory(ExportPath);

        MeshFilter[] meshFilters = go.GetComponentsInChildren<MeshFilter>();
        List<MeshFilter> activeMeshFilters = new List<MeshFilter>();
        foreach (MeshFilter mf in meshFilters)
        {
            Debug.Log(mf.mesh.name);
            if (mf.mesh.name.Contains("Surface"))
                continue;

            if (mf.gameObject.activeSelf) //mf.gameObject.GetComponent<MeshRenderer>().enabled)
            {
                activeMeshFilters.Add(mf);
            }
        }
        MeshFilter goMeshFilter = go.GetComponent<MeshFilter>();
        if (goMeshFilter != null)
            activeMeshFilters.Add(goMeshFilter);
        Debug.Log(activeMeshFilters.Count);

        // for structual
        Vector3 center = Vector3.zero;
        bool applyBoundingBox = false;
        bool saveSubMeshes = false;
        bool saveSubMeshTransform = false;
        bool saveCombinedSubmeshes = false;
    
        if (go.tag == "SimObjPhysics")
        {
            SimObjPhysics parent = go.transform.GetComponent<SimObjPhysics>();
            AxisAlignedBoundingBox box = parent.AxisAlignedBoundingBox;
            center = box.center;
            applyBoundingBox = true; 
            saveSubMeshes = true;
            saveSubMeshTransform = true;
            saveCombinedSubmeshes = true;
        }
        downloadThorAssets.savePath = ExportPath;

        // export asset obj 
        string relativeExportPath = Path.Combine(objectType, assetId);
        Directory.CreateDirectory(Path.Combine(ExportPath, objectType));
        downloadThorAssets.SaveMeshes(relativeExportPath, activeMeshFilters.ToArray(), center, applyBoundingBox, saveSubMeshes, saveSubMeshTransform, saveCombinedSubmeshes);

        // export asset mtl
        downloadThorAssets.SaveMaterials(relativeExportPath);   

        object_count[objectId]++;

        return assetId;
    }

    List<RoboThorObject> _GetStructuralObjects()
    {
        // static structural objects
        List<RoboThorObject> list_objects = new List<RoboThorObject>(); 

        // wall panels, etc
        GameObject structure = GameObject.Find("Structure");

        List<Transform> children = new List<Transform>();
        foreach (Transform top_child in structure.transform) // This worked for RoboTHOR
        {
            int i = 0;
            string objectType = "Structural"; 
            foreach (Transform child in top_child.GetComponentsInChildren<Transform>()) // This worked for iTHOR
            {
                string objectId = child.name.Replace(" ", "_"); // + "_" + i;
                Debug.Log("child: " + objectId);
                if (child.tag =="Structure")
                {
                    if (!child.gameObject.activeSelf)
                        continue;
                    
                    SimObjPhysics simObjPhysics = child.GetComponentInChildren<SimObjPhysics>() ?? child.GetComponent<SimObjPhysics>();
                    string assetId = "";
                    if (simObjPhysics == null || simObjPhysics.assetID == "" || simObjPhysics.assetID == null)
                    {
                        Debug.Log("no asset id for " + objectId);
                        assetId = _exportAsset(child.gameObject, objectId, objectType);              
                    }
                    else
                    {
                        assetId = simObjPhysics.assetID;
                    }

                    RoboThorObject obj = new RoboThorObject();

                    // get object name
                    // This was fixed and only true for RoboTHOR
                    //obj.objectType = "WallPanel";
                    //obj.assetId = "RoboTHOR_wall_panel_32_5";
                    obj.objectType = objectId;
                    obj.assetId = assetId;

                    // position
                    obj.position = Vector3.zero;

                    // rotation
                    Vector3 rotation = Vector3.zero;
                    obj.rotation = rotation;
                    
                    // kinematic
                    bool isStatic = true;
                    obj.kinematic = isStatic;

                    list_objects.Add(obj);
                }
            }
        }
        return list_objects;
    }

    List<RoboThorObject> GetObjects()
    {
        string sceneName = gameObject.scene.name;
        List<RoboThorObject> list_structural_objects  = _GetStructuralObjects();
        Debug.Log("list_structural_objects: " + list_structural_objects.Count);

        // objects
        List<RoboThorObject> list_objects = new List<RoboThorObject>(); 
        list_objects.AddRange(list_structural_objects);

        Transform parent = GameObject.Find("Objects").transform;
        for(int i=0; i<parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (!child.gameObject.activeSelf)
                continue;

            Debug.Log(child.name);

            RoboThorObject obj = new RoboThorObject();

            // get object name
            string objectId = child.name.Replace(" ", "_");
            SimObjPhysics simObjPhysics = child.GetComponentInChildren<SimObjPhysics>() ?? child.GetComponent<SimObjPhysics>();
            Transform bbox = child.Find("BoundingBox");
            Vector3 bbox_center = Vector3.zero;
            if (bbox != null)
            {
                bbox_center = bbox.GetComponent<BoxCollider>().center;
            }
            //AxisAlignedBoundingBox bbox = simObjPhysics.AxisAlignedBoundingBox;
            
            string objectType = Enum.GetName(typeof(SimObjType), simObjPhysics.Type); //folder name
            obj.objectType = objectType;

            string assetId = simObjPhysics.assetID; //filename
            Debug.Log("assetId: " + assetId);
            if (assetId == "")
            {
                // Were they custom designed? 
                // Drawers, Cabinet, Sink, Countertop, StoveBurner, StoveKnob, Floor                
                // export asset obj and mtl 

                // Clone the game object at the root
                GameObject clone = Instantiate(child.gameObject);
                clone.name = objectId;

                Debug.Log("no asset id for " + objectId);
                assetId = _exportAsset(clone, objectId, objectType);
                bbox_center = Vector3.zero;
            }

            obj.assetId = assetId;

            // position
            Vector3 position = child.localPosition;
            //obj.position = position;
            obj.position = position + bbox_center; // bbox center should be nonzero only for THOR objects
    
            
            // rotation
            Vector3 rotation = child.localEulerAngles;
            obj.rotation = rotation;
            
            // kinematic
            bool isStatic = simObjPhysics.isStatic;
            obj.kinematic = isStatic;

            list_objects.Add(obj);
        }
        return list_objects;
    }

    void GetDoors()
    {
        // usually skip in RoboTHOR and iTHOR as they have mesh renderers

    }

    void GetWindows()
    {
        // usually skip in RoboTHOR and iTHOR as they have mesh renderers

    }

    void GetWalls()
    {
        // fusually skip in RoboTHOR and iTHOR as they have mesh renderers

    }

    void GetProceduralParams()
    {
        // floorColliderThickness

        // lights

        // reflections 

        // ceilingMaterial

        // skyboXId
    }

    void GetLights()
    {
        // id

        // position

        // rotation

        // shadow

        // type

        // intensity

        // indirectMultiplier

        // rgb
    }
    void GetMetadata()
    {
        /*From the menu bar, click Window > Rendering > Lighting Settings.
        In the window that appears, click the Environment tab.
        Assign the skybox Material to the Skybox Material property.
        */
    }
}