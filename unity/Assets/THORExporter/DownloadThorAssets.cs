using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;
using System;
using Thor.Procedural;
using System.Linq;
using System.Reflection;
using UnityEditorInternal.Profiling.Memory.Experimental;

public class DownloadThorAssets : MonoBehaviour
{
    public string savePath = "Assets/ExportedThorAssets";
    public string assetPath = "Assets/Physics/SimObjsPhysics";
    public string materialPath = "Assets/Resources/QuickMaterials";
    //public string doorAssetPath = "Assets/Physics/SimObjsPhysics/ManipulaTHOR Objects/Doorways/Prefabs";
    bool applyBoundingBox = false; // TODO: should always false


    [Header("generate obj and mtl")]
    public bool saveSubMeshes = true;

    [Header("include transform key and value for all submeshes in json")]
    public bool saveSubMeshTransform = true;

    [Header("Save all submeshes as one OBJ file")]
    public bool saveCombinedSubmeshes = true;

    [Header("Save time and skip mesh export step")]
    public bool skipMeshExport = false;

    [Header("Save time and skip material export step")]
    public bool skipMaterialExport = false;

    [Header("Just do the first one because I'm testing things")]
    public bool justDoTheFirstOneBecauseImTestingThings = false;

    Dictionary<string, Material> allMaterials = new Dictionary<string, Material>();
    Dictionary<string, Dictionary<string, string>> Mat2Texture = new Dictionary<string, Dictionary<string, string>>();

    [System.Serializable]
    public class SerializableKeyValuePair
    {
        public string outerKey;
        public string innerKey;
        public string value;

        public SerializableKeyValuePair(string outerKey, string innerKey, string value)
        {
            this.outerKey = outerKey;
            this.innerKey = innerKey;
            this.value = value;
        }
    }

    [System.Serializable]
    public class SerializableDictionary
    {
        public List<SerializableKeyValuePair> keyValuePairs;

        public SerializableDictionary(Dictionary<string, Dictionary<string, string>> dictionary)
        {
            keyValuePairs = new List<SerializableKeyValuePair>();

            foreach (var outerPair in dictionary)
            {
                foreach (var innerPair in outerPair.Value)
                {
                    keyValuePairs.Add(new SerializableKeyValuePair(outerPair.Key, innerPair.Key, innerPair.Value));
                }
            }
        }
    }

    [System.Serializable]
    public class ExportedAssetInfo {
        //the bounding box center for this entire asset
        public bbox_center bbox_center = new bbox_center();
        //mesh heirarchy info for this entire asset
        public List<MeshData> meshes = new List<MeshData>();
    }

    [System.Serializable]
    public class bbox_center {
        public string position = "";
    }

    [System.Serializable]
    public class MeshData {
        public Vector3 parentRelativePosition;
        public Vector3 parentRelativeScale;
        public Quaternion parentRelativeRotation;
        public string parentName;
        public string meshName;
        public AllMyPrimitiveColliders primitiveColliders = new AllMyPrimitiveColliders();
        public AllMyPlaceableZones placeableZoneColliders = new AllMyPlaceableZones();
        public JointInfo jointInfo = new JointInfo();
    }

    [System.Serializable]
    public class JointInfo {
        public string jointType = "none"; //rotate, slide, (scale????)
        //what is the position of this joint relative to whatever this joint's mesh's parent is?
        public Vector3 meshParentRelativePosition;
        //this joint's range of movement in local space
        //if jointType == rotate, this is a change in rotation in euler angles
        //if jointType == slide, this is a change in position
        public Vector3 lowRange;
        public Vector3 highRange;
    }

    [System.Serializable]
    public class AllMyPrimitiveColliders {
        public List<ColliderInfo> myPrimitiveColliders = new List<ColliderInfo>();
    }

    [System.Serializable]
    public class AllMyPlaceableZones {
        public List<ColliderInfo> myPlaceableZones = new List<ColliderInfo>();
    }

    [System.Serializable]
    public class ColliderInfo {
        public string type;
        public Vector3 size;
        public Vector3 position;
        public Quaternion rotation;
        public float radius;
        public float height;
        public int direction;
    }

    public List<string> meshNamesToClearColliders = new List<string> { 
        "fridge_drawer1_b1", 
        "fridge_drawer2_b1", 
        "fridge_drawer1_c1", 
        "fridge_drawer2_c1", 
        "fridge_drawer3_c1",
        "fridge_drawer4_c1",
        "fridge_freezerdoor_c1",
        "fridge_drawer1_d1",
        "fridge_drawer2_d1",
        "fridge_drawer1_e1",
        "fridge_drawer2_e1",
        "fridge_drawer1_b2",
        "fridge_drawer2_b2",
        "fridge_drawer1_c2",
        "fridge_drawer2_c2",
        "fridge_drawer3_c2",
        "fridge_drawer4_c2",
        "fridge_freezerdoor_c2",
        "fridge_drawer1_d2",
        "fridge_drawer2_d2",
        "fridge_drawer1_e2",
        "fridge_drawer2_e2",
        "fridge_drawer1_b3",
        "fridge_drawer2_b3",
        "fridge_drawer1_c3",
        "fridge_drawer2_c3",
        "fridge_freezerdoor_c3",
        "fridge_drawer1_d3",
        "fridge_drawer1_e3",
        "fridge_drawer2_e3",
        "fridge_drawer1_b4",
        "fridge_drawer2_b4",
        "fridge_drawer1_c4",
        "fridge_drawer2_c4",
        "fridge_drawer3_c4",
        "fridge_drawer4_c4",
        "fridge_freezerdoor_c4",
        "fridge_drawer1_d4",
        "fridge_drawer2_d4",
        "fridge_drawer1_e4",
        "fridge_drawer2_e4",
        "fridge_drawer1_b5",
        "fridge_drawer2_b5",
        "fridge_drawer1_c5",
        "fridge_drawer2_c5",
        "fridge_drawer3_c5",
        "fridge_drawer4_c5",
        "fridge_freezerdoor_c5",
        "fridge_drawer1_d5",
        "fridge_drawer2_d5",
        "fridge_drawer1_e5",
        "fridge_drawer2_e5",
        "fridge_drawer3_e5",
        "fridge_drawer1_b6",
        "fridge_drawer2_b6",
        "fridge_drawer1_c6",
        "fridge_drawer2_c6",
        "fridge_drawer3_c6",
        "fridge_drawer4_c6",
        "fridge_freezerdoor_c6",
        "fridge_drawer1_d6",
        "fridge_drawer2_d6",
        "fridge_drawer1_e6",
        "fridge_drawer2_e6",
    };

    // Start is called before the first frame update
    void Start()
    {
        // get all assets and export obj
        GatherGameObjectsFromPrefabsAndSave(assetPath, applyBoundingBox, saveSubMeshes, saveSubMeshTransform);
        //GatherGameObjectsFromPrefabsAndSave(doorAssetPath, false, true);

        if(!skipMaterialExport)
        {
            GetAllMaterials(materialPath);

            // save material dictionary to json
            //Debug.Log(Mat2Texture.Count);
            string json = JsonUtility.ToJson(new SerializableDictionary(Mat2Texture), true);
            File.WriteAllText(Path.Combine(savePath, "quick_material_to_textures.json"), json);
            Debug.Log("Saved material to textures dictionary to: " + Path.Combine(savePath, "material_to_textures.json"));
        }
    }


    void GetAllMaterials(string folderPath)
    {
        if (Directory.Exists(folderPath))
        {
            // Get all .mat files in the folder
            string[] matFiles = Directory.GetFiles(folderPath, "*.mat", SearchOption.AllDirectories);

            // Loop through each .mat file
            foreach (string matFile in matFiles)
            {
                // Load the Material from the .mat file
                //Debug.Log("Loading material: " + matFile);
                Material m = AssetDatabase.LoadAssetAtPath<Material>(matFile);
                if (m != null)
                {
                    if (!Mat2Texture.ContainsKey(m.name))
                    {
                        Dictionary<string, string> matdict = new Dictionary<string, string>();
                        string _MainTex = TryExportTexture("_MainTex", m);
                        matdict.Add("_MainTex", _MainTex);
                        string _MetallicGlossMap = TryExportTexture("_MetallicGlossMap", m);
                        matdict.Add("_MetallicGlossMap", _MetallicGlossMap);
                        string _BumpMap = TryExportTexture("_BumpMap", m);
                        matdict.Add("_BumpMap", _BumpMap);

                        matdict.Add("emission_rgba", m.GetColor("_EmissionColor").r.ToString() + " " + m.GetColor("_EmissionColor").g.ToString() + " " + m.GetColor("_EmissionColor").b.ToString() + " " + m.GetColor("_EmissionColor").a.ToString());
                        //matdict.Add("specular_rgba", m.GetColor("_SpecColor").r.ToString() + " " + m.GetColor("_SpecColor").g.ToString() + " " + m.GetColor("_SpecColor").b.ToString());
                        matdict.Add("specular", m.GetFloat("_SpecularHighlights").ToString()); // reflectance ?
                        matdict.Add("smoothness", m.GetFloat("_Glossiness").ToString()); // reflectance ? _Glossiness (Smothness)
                        matdict.Add("metallic", m.GetFloat("_Metallic").ToString());  // shininess ? _GlossyReflectons (Glossy Reflections) or _Metallic
                        //matdict.Add("reflection", m.GetFloat("_GlossyReflectons").ToString());  // shininess ? _GlossyReflectons (Glossy Reflections) or _Metallic
                        matdict.Add("albedo_rgba", m.color.r.ToString() + " " + m.color.g.ToString() + " " + m.color.b.ToString() + " " + m.color.a.ToString());
                        //Debug.Log(m.color.r.ToString() + " " + m.color.g.ToString() + " " + m.color.b.ToString() + " " + m.color.a.ToString());

                        Mat2Texture.Add(m.name, matdict);
                        //Debug.Log("Adding " + m.name);
                        //Debug.Log("Adding " + m.name + " to Mat2Texture" + Mat2Texture[m.name]["_MainTex"]);
                    }
                }
            }
        }
        else
        {
            Debug.LogError("Folder does not exist: " + folderPath);
        }
    }

    string GetRelativePath(string rootDirectory, string fullPath)
    {
        string[] splitArray =  fullPath.Split(char.Parse("/"));

        string relativePath = "";
        for (int i = 3; i < splitArray.Length; i++)
        {
            relativePath += splitArray[i];
            if (i<splitArray.Length-1)
                relativePath += "/";
        }

        return relativePath;
    }

    string[] GetFilesExcludingDirectories(string rootPath, string searchPattern, List<string> excludeDirectoryNames)
    {
        List<string> files = new List<string>();
        GetFilesRecursively(rootPath, searchPattern, excludeDirectoryNames, files);
        return files.ToArray();
    }

    void GetFilesRecursively(string currentPath, string searchPattern, List<string> excludeDirectoryNames, List<string> files)
    {
        try
        {
            foreach (string file in Directory.GetFiles(currentPath, searchPattern))
            {
                files.Add(file);
            }

            foreach (string directory in Directory.GetDirectories(currentPath))
            {
                if (excludeDirectoryNames.Contains(Path.GetFileName(directory), StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                GetFilesRecursively(directory, searchPattern, excludeDirectoryNames, files);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Error while searching files: " + ex.Message);
        }
    }

    void GatherGameObjectsFromPrefabsAndSave(string directoryPath, bool applyBoundingBox = false, bool saveSubMeshes = false, bool saveSubMeshTransform = false)
    {
        if (!Directory.Exists(directoryPath))
        {
            Debug.LogError("Directory does not exist: " + directoryPath);
            return;
        }

        Directory.CreateDirectory(Path.Combine(savePath, "Textures")); 
        
        // Get all prefab files, excluding specified directories
        List<string> excludeDirectories = new List<string> { "Custom Project Objects", "Entryway Objects" };
        string[] prefabFiles = GetFilesExcludingDirectories(directoryPath, "*.prefab", excludeDirectories);

        // Filter out prefabs that contain "Sliced" in their names
        prefabFiles = prefabFiles.Where(prefabPath => !Path.GetFileNameWithoutExtension(prefabPath).Contains("Sliced")).ToArray();

        int assetsProcessed = 0;
        foreach (string prefabPath in prefabFiles)
        {
            // skip if already exist
            //if (File.Exists(Path.Combine(savePath, GetRelativePath(assetPath, prefabPath).Replace(".prefab", ".obj"))))
            //{
            //    Debug.Log("Skipping " + prefabPath);
            //    continue;
            //}

            string relativePrefabPath = GetRelativePath(assetPath, prefabPath);
            Debug.Log("Prefab path: " + relativePrefabPath);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null)
            {
                GameObject instantiatedPrefab = Instantiate(prefab);
                //remove the "(Clone)" from the name on instantiation
                instantiatedPrefab.name = prefab.name + "_root";

                // Check if the instantiated prefab has a SimObjPhysics component on its topmost game object
                if (instantiatedPrefab.GetComponentInChildren<SimObjPhysics>() == null)
                {
                    Debug.LogWarning("No SimObjPhysics component found on topmost game object or any of its children of prefab: " + prefabPath);
                    Destroy(instantiatedPrefab);
                    continue;
                }

                SaveEachAsset(instantiatedPrefab, relativePrefabPath, applyBoundingBox, saveSubMeshes, saveSubMeshTransform);
                Destroy(instantiatedPrefab);

                assetsProcessed++;
            }
            else
            {
                Debug.LogWarning("Failed to load prefab at path: " + prefabPath);
            }

            if(justDoTheFirstOneBecauseImTestingThings) // just do the first one
            {
                break;
            }
        }

        Debug.Log($"finished exporting {assetsProcessed} prefabs from {assetPath}");
    }

    
    void SaveEachAsset(GameObject go, string relativeExportPath, bool applyBoundingBox = true, bool saveSubMeshes = false, bool saveSubMeshTransform = false)
    {    
        Directory.CreateDirectory(Path.Combine(savePath, Path.GetDirectoryName(relativeExportPath)));
        
        // grab reference to all mesh filters in this prefab's heirarchy
        MeshFilter[] meshFilters = go.transform.GetComponentsInChildren<MeshFilter>();

        //remove all mesh filters that have an associated mesh renderer that is not active, these meshes
        //are meant to be invisible so should not be included in the export
        List<MeshFilter> activeMeshFilters = new List<MeshFilter>();
        foreach (MeshFilter mf in meshFilters)
        {
            MeshRenderer mr = mf.gameObject.GetComponent<MeshRenderer>();
            if (mr != null && mr.enabled && mf.transform.gameObject.activeSelf)
            {
                //get rid of anything using the Placeable_Surface_Mat
                bool containsMaterialWeDontWant = false;
                foreach (Material mat in mr.sharedMaterials)
                {
                    if (mat != null && (mat.name == "Placeable_Surface_Mat") || (mat.name == "Water_Volume_Surface_Mat"))
                    {
                        containsMaterialWeDontWant = true;
                        break;
                    }
                }

                if (!containsMaterialWeDontWant)
                {
                    activeMeshFilters.Add(mf);
                }
            }
        }

        Vector3 center = Vector3.zero;
        SimObjPhysics parent = go.transform.GetComponent<SimObjPhysics>();

        if(parent != null)  
        {
            AxisAlignedBoundingBox box = parent.AxisAlignedBoundingBox;
            center = box.center;
            //Debug.Log("center" + center.ToString());
        }       
        else
        {
            Debug.Log("No bounding box found for " + go.name);
        } 
    
        //Debug.Log("saving mesh1" + center.ToString());

        //SaveMeshes(relativeExportPath, meshFilters, center, applyBoundingBox, saveSubMeshes, saveSubMeshTransform, false);    
        if(saveCombinedSubmeshes)
        {
            SaveMeshes(
                relativeExportPath, 
                activeMeshFilters.ToArray(), 
                center, 
                applyBoundingBox, 
                saveSubMeshes, 
                saveSubMeshTransform, 
                saveCombinedSubmeshes,
                parent
            );
        }
    

        //Debug.Log("saving mesh2");

        if (!skipMaterialExport)
        {
            Debug.Log("saving material");

            SaveMaterials(relativeExportPath);
            allMaterials.Clear();
        }

    }

    void SaveMaterials(string relativeExportPath)
    {
        string baseFileName = Path.GetFileNameWithoutExtension(relativeExportPath);

        StringBuilder sbMaterials = new StringBuilder();
        foreach (KeyValuePair<string, Material> entry in allMaterials)
        {
            Debug.Log("saving material 1");

            sbMaterials.Append(MaterialToString(entry.Value));
            sbMaterials.AppendLine();
        }
    
        //write to disk
        System.IO.File.WriteAllText( Path.Combine(Path.Combine(savePath, Path.GetDirectoryName(relativeExportPath)), baseFileName + ".mtl"),  sbMaterials.ToString());
        print("material saved");
    }

    //take one mesh filter, and get all the information about it ready to go
public MeshData FillMeshData(MeshFilter meshfilter, string meshName, SimObjPhysics topmostSimObjPhysics)
{
    var go = meshfilter.gameObject;

    //recursive setup for where mesh's parent is the top level of the hierarchy
    var meshData = new MeshData
    {
        //default with this mesh's current local pos, rot, and scale
        parentRelativePosition = go.transform.localPosition,
        parentRelativeRotation = go.transform.localRotation,
        parentRelativeScale = go.transform.localScale,
        meshName = meshName,
        parentName = ""
    };

    //keep track of what transforms we have traversed upward so we can compare them to associated joints later.....
    List<Transform> transformsTraversed = new List<Transform>();
    //include THIS MESH's transform because that can sometimes be a joint
    transformsTraversed.Add(go.transform);

    // Traverse the parent hierarchy
    Transform parent = go.transform.parent;

    while (parent != null)
    {
        //track what transforms we have traversed so far
        transformsTraversed.Add(parent);

        // Adjust transform relative to this parent
        meshData.parentRelativePosition = parent.InverseTransformPoint(meshData.parentRelativePosition);
        meshData.parentRelativeRotation = Quaternion.Inverse(parent.rotation) * meshData.parentRelativeRotation;
        meshData.parentRelativeScale = Vector3.Scale(meshData.parentRelativeScale, parent.localScale);

        // If this parent has a MeshFilter, stop here
        if (parent.GetComponent<MeshFilter>() != null)
        {
            meshData.parentName = parent.name;
            break;  //Stop searching further
        }

        // If this parent has a SimObjPhysics component and it's not the topmost one, continue searching
        if (parent.GetComponent<SimObjPhysics>() != null && parent != topmostSimObjPhysics.transform)
        {
            // Move up the hierarchy
            parent = parent.parent;
            continue;
        }

        // Move up the hierarchy
        parent = parent.parent;
    }

    // If no parent with MeshFilter or SimObjPhysics was found, use the root transform
    if (parent == null)
    {
        parent = go.transform.root;
        meshData.parentName = parent.name;
        Debug.LogWarning("No parent with MeshFilter or SimObjPhysics found, using root transform as fallback.");
    }

    CollectValidColliders(meshfilter, ref meshData);

    /////////////// Joint setup ///////////
    //check if this mesh has a joint associated with it 

    meshData.jointInfo = CollectValidJoints(meshfilter, ref meshData, transformsTraversed, parent, topmostSimObjPhysics.transform);

    ////////////////meshData cleanup ///////////
    //this is jank but oh welllllll
    foreach (string name in meshNamesToClearColliders)
    {
        if (meshData.meshName.Contains(name))
        {
            meshData.primitiveColliders.myPrimitiveColliders.Clear();
            meshData.primitiveColliders.myPrimitiveColliders = new List<ColliderInfo>();
            break;
        }
    }

    // foreach (ColliderInfo cInfo in meshData.primitiveColliders.myPrimitiveColliders) 
    // {
    //     if (cInfo.type == "mesh")
    //     {
    //         Debug.Log("Mesh Collider found on " + meshData.meshName + " clearing all colliders");
    //         meshData.primitiveColliders.myPrimitiveColliders.Remove(cInfo);
    //         break;
    //     }
    // }

    return meshData;
}

private JointInfo CollectValidJoints(MeshFilter meshfilter, ref MeshData meshData, List<Transform> transformsTraversed, Transform parent, Transform topmostSimObjPhysics)
{
    Debug.Log("CollectValidJoints called for mesh: " + meshData.meshName);

    JointInfo jointInfo = null;

    // Traverse up the hierarchy to find the first SimObjPhysics component
    SimObjPhysics firstSimObjPhysics = null;
    Transform current = meshfilter.transform;

    while (current != null)
    {
        if (current.GetComponent<SimObjPhysics>() != null)
        {
            firstSimObjPhysics = current.GetComponent<SimObjPhysics>();
            break;
        }
        current = current.parent;
    }

    if (firstSimObjPhysics != null)
    {
        Debug.Log("First SimObjPhysics component found: " + firstSimObjPhysics.name);

        // Check if the first SimObjPhysics component has a CanOpen_Object component
        var canOpenObject = firstSimObjPhysics.GetComponent<CanOpen_Object>();
        if (canOpenObject != null)
        {
            Debug.Log("CanOpen_Object component found on first SimObjPhysics: " + firstSimObjPhysics.name);

            // Traverse up the hierarchy to find the topmost SimObjPhysics component
            SimObjPhysics topmostSimObjPhysicsComponent = firstSimObjPhysics;
            current = firstSimObjPhysics.transform.parent;

            while (current != null)
            {
                if (current.GetComponent<SimObjPhysics>() != null)
                {
                    topmostSimObjPhysicsComponent = current.GetComponent<SimObjPhysics>();
                }
                current = current.parent;
            }

            Debug.Log("Topmost SimObjPhysics component found: " + topmostSimObjPhysicsComponent.name);

            // Get the movementType from CanOpen_Object
            jointInfo = new JointInfo
            {
                jointType = canOpenObject.movementType.ToString()
            };
            Debug.Log("MovementType: " + jointInfo.jointType);

            // Get the openPositions and closedPositions arrays from CanOpen_Object
            Vector3[] openPositions = canOpenObject.openPositions;
            Vector3[] closedPositions = canOpenObject.closedPositions;
            GameObject[] movingParts = canOpenObject.MovingParts;

            Debug.Log("MovingParts length: " + movingParts.Length);
            Debug.Log("OpenPositions length: " + openPositions.Length);
            Debug.Log("ClosedPositions length: " + closedPositions.Length);

            // Find the associated moving part
            bool foundAssociatedMovingPart = false;
            for (int i = 0; i < movingParts.Length; i++)
            {
                Debug.Log("Checking MovingPart: " + movingParts[i].name);

                // Compare each transform traversed against the moving parts
                foreach (var transform in transformsTraversed)
                {
                    Debug.Log("Transform encountered: " + transform.name);
                    Debug.Log("MovingPart InstanceID: " + movingParts[i].GetInstanceID());
                    Debug.Log("Transform InstanceID: " + transform.gameObject.GetInstanceID());

                    if (movingParts[i] == transform.gameObject)
                    {
                        Debug.Log("Associated MovingPart found: " + movingParts[i].name);

                        // Calculate meshParentRelativePosition
                        Debug.Log("Calculating meshParentRelativePosition...");
                        jointInfo.meshParentRelativePosition = topmostSimObjPhysicsComponent.transform.InverseTransformPoint(movingParts[i].transform.position);

                        Debug.Log("meshParentRelativePosition: " + jointInfo.meshParentRelativePosition);

                        // Calculate lowRange and highRange based on movementType
                        if (canOpenObject.movementType == CanOpen_Object.MovementType.Slide)
                        {
                            Debug.Log("Calculating lowRange and highRange for Slide...");
                            jointInfo.lowRange = topmostSimObjPhysicsComponent.transform.InverseTransformPoint(closedPositions[i]);
                            jointInfo.highRange = topmostSimObjPhysicsComponent.transform.InverseTransformPoint(openPositions[i]);
                        }
                        else if (canOpenObject.movementType == CanOpen_Object.MovementType.Rotate)
                        {
                            Debug.Log("Calculating lowRange and highRange for Rotate...");
                            jointInfo.lowRange = (Quaternion.Inverse(topmostSimObjPhysicsComponent.transform.rotation) * Quaternion.Euler(closedPositions[i])).eulerAngles;
                            jointInfo.highRange = (Quaternion.Inverse(topmostSimObjPhysicsComponent.transform.rotation) * Quaternion.Euler(openPositions[i])).eulerAngles;
                        }

                        Debug.Log("lowRange: " + jointInfo.lowRange);
                        Debug.Log("highRange: " + jointInfo.highRange);

                        foundAssociatedMovingPart = true;
                        break; // Stop searching further
                    }
                }

                if (foundAssociatedMovingPart)
                {
                    break; // Stop searching further
                }
            }

            if (!foundAssociatedMovingPart)
            {
                jointInfo = null; // No associated moving part found, so set jointInfo to null
            }
        }
        else
        {
            Debug.Log("CanOpen_Object component not found on first SimObjPhysics: " + firstSimObjPhysics.name);
        }

        // Check if the first SimObjPhysics component has a CanToggleOnOff component
        var canToggleOnOff = firstSimObjPhysics.GetComponent<CanToggleOnOff>();
        if (canToggleOnOff != null)
        {
            Debug.Log("CanToggleOnOff component found on first SimObjPhysics: " + firstSimObjPhysics.name);

            // Traverse up the hierarchy to find the topmost SimObjPhysics component
            SimObjPhysics topmostSimObjPhysicsComponent = firstSimObjPhysics;
            current = firstSimObjPhysics.transform.parent;

            while (current != null)
            {
                if (current.GetComponent<SimObjPhysics>() != null)
                {
                    topmostSimObjPhysicsComponent = current.GetComponent<SimObjPhysics>();
                }
                current = current.parent;
            }

            Debug.Log("Topmost SimObjPhysics component found: " + topmostSimObjPhysicsComponent.name);

            // Get the movementType from CanToggleOnOff
            jointInfo = new JointInfo
            {
                jointType = canToggleOnOff.movementType.ToString()
            };
            Debug.Log("MovementType: " + jointInfo.jointType);

            // Get the OnPositions and OffPositions arrays from CanToggleOnOff
            Vector3[] onPositions = canToggleOnOff.OnPositions;
            Vector3[] offPositions = canToggleOnOff.OffPositions;
            GameObject[] movingParts = canToggleOnOff.MovingParts;

            Debug.Log("MovingParts length: " + movingParts.Length);
            Debug.Log("OnPositions length: " + onPositions.Length);
            Debug.Log("OffPositions length: " + offPositions.Length);

            // Find the associated moving part
            bool foundAssociatedMovingPart = false;
            for (int i = 0; i < movingParts.Length; i++)
            {
                Debug.Log("Checking MovingPart: " + movingParts[i].name);

                // Compare each transform traversed against the moving parts
                foreach (var transform in transformsTraversed)
                {
                    Debug.Log("Transform encountered: " + transform.name);
                    Debug.Log("MovingPart InstanceID: " + movingParts[i].GetInstanceID());
                    Debug.Log("Transform InstanceID: " + transform.gameObject.GetInstanceID());

                    if (movingParts[i] == transform.gameObject)
                    {
                        Debug.Log("Associated MovingPart found: " + movingParts[i].name);

                        // Calculate meshParentRelativePosition
                        Debug.Log("Calculating meshParentRelativePosition...");
                        jointInfo.meshParentRelativePosition = topmostSimObjPhysicsComponent.transform.InverseTransformPoint(movingParts[i].transform.position);

                        Debug.Log("meshParentRelativePosition: " + jointInfo.meshParentRelativePosition);

                        // Calculate lowRange and highRange based on movementType
                        if (canToggleOnOff.movementType == CanToggleOnOff.MovementType.Slide)
                        {
                            Debug.Log("Calculating lowRange and highRange for Slide...");
                            jointInfo.lowRange = topmostSimObjPhysicsComponent.transform.InverseTransformPoint(offPositions[i]);
                            jointInfo.highRange = topmostSimObjPhysicsComponent.transform.InverseTransformPoint(onPositions[i]);
                        }
                        else if (canToggleOnOff.movementType == CanToggleOnOff.MovementType.Rotate)
                        {
                            Debug.Log("Calculating lowRange and highRange for Rotate...");
                            jointInfo.lowRange = (Quaternion.Inverse(topmostSimObjPhysicsComponent.transform.rotation) * Quaternion.Euler(offPositions[i])).eulerAngles;
                            jointInfo.highRange = (Quaternion.Inverse(topmostSimObjPhysicsComponent.transform.rotation) * Quaternion.Euler(onPositions[i])).eulerAngles;
                        }

                        Debug.Log("lowRange: " + jointInfo.lowRange);
                        Debug.Log("highRange: " + jointInfo.highRange);

                        foundAssociatedMovingPart = true;
                        break; // Stop searching further
                    }
                }

                if (foundAssociatedMovingPart)
                {
                    break; // Stop searching further
                }
            }

            if (!foundAssociatedMovingPart)
            {
                jointInfo = null; // No associated moving part found, so set jointInfo to null
            }
        }
        else
        {
            Debug.Log("CanToggleOnOff component not found on first SimObjPhysics: " + firstSimObjPhysics.name);
        }
    }
    else
    {
        Debug.Log("SimObjPhysics component not found on root: " + meshfilter.name);
    }

    return jointInfo;
}

private void CollectValidColliders(MeshFilter meshfilter, ref MeshData meshData)
{
    Debug.Log("call CollectValidColliders");
    var go = meshfilter.gameObject;

    Transform parent = go.transform.parent;
    if (parent != null)
    {
        foreach (Transform sibling in parent)
        {
            if (sibling == go.transform) continue;

            // Include sibling's colliders and descendants recursively
            AddCollidersRecursive(sibling, ref meshData, go);
        }
    }
}

private void AddCollidersRecursive(Transform sibling, ref MeshData meshData, GameObject meshFiltersGameObject)
{
    // Check if SimObjPhysics is found in the target or its descendants
    SimObjPhysics simObjPhysics = sibling.GetComponent<SimObjPhysics>();
    if (simObjPhysics != null)
    {
        // Check if the SimObjPhysics type is BathtubBasin or SinkBasin
        if (simObjPhysics.Type == SimObjType.BathtubBasin || simObjPhysics.Type == SimObjType.SinkBasin)
        {
            // Continue the search and include colliders with the Contains component
            foreach (var collider in sibling.GetComponents<Collider>())
            {
                if (!collider.enabled || !collider.gameObject.activeInHierarchy)
                    continue;

                var colliderInfo = GetColliderInfo(collider);
                if (colliderInfo != null)
                {
                    if (collider.GetComponent("Contains") != null)
                    {
                        meshData.placeableZoneColliders.myPlaceableZones.Add(colliderInfo);
                    }
                }
            }
        }
        else
        {
            // Stop searching if SimObjPhysics is found and it's not BathtubBasin or SinkBasin
            return;
        }
    }

    // Collect colliders at this level
    foreach (var collider in sibling.GetComponents<Collider>())
    {
        if (!collider.enabled || !collider.gameObject.activeInHierarchy)
            continue;

        var colliderInfo = GetColliderInfo(collider);
        if (colliderInfo != null)
        {
            if (!collider.isTrigger)
            {
                meshData.primitiveColliders.myPrimitiveColliders.Add(colliderInfo);
            }
            else if (collider.GetComponent("Contains") != null)
            {
                meshData.placeableZoneColliders.myPlaceableZones.Add(colliderInfo);
            }
        }
    }

    // Recursively check all children
    foreach (Transform child in sibling)
    {
        AddCollidersRecursive(child, ref meshData, meshFiltersGameObject);
    }
}

// private bool IsSimObjPhysicsFound(Transform target, GameObject reference)
// {
//     // Return true if SimObjPhysics is found on the target or any descendant
//     if (target.GetComponent("SimObjPhysics") != null) return true;

//     foreach (Transform child in target)
//     {
//         if (IsSimObjPhysicsFound(child, reference)) return true;
//     }

//     return false;
// }

    public ColliderInfo GetColliderInfo(Collider collider)
    {
        string colliderType = collider.GetType().Name.ToLower().Replace("collider", "");
        ColliderInfo info = new ColliderInfo();

        // mesh colliders
        if (colliderType == "mesh")
        {
            return null; // Skip mesh colliders
        }

        info.type = colliderType;

        // Get nearest parent with MeshFilter or root for relative transforms
        Transform referenceTransform = GetNearestMeshOrRoot(collider.transform);

        Vector3 combinedScale = GetCombinedScale(collider.transform, referenceTransform);
        Vector3 relativePosition = referenceTransform.InverseTransformPoint(collider.transform.position);
        Quaternion relativeRotation = Quaternion.Inverse(referenceTransform.rotation) * collider.transform.rotation;

        // BoxCollider
        if (collider is BoxCollider box)
        {
            info.size = Vector3.Scale(box.size, combinedScale) * 0.5f; // Half extents with combined scale
            info.position = referenceTransform.InverseTransformPoint(box.transform.TransformPoint(box.center));
            info.rotation = relativeRotation;
        }
        // SphereCollider
        else if (collider is SphereCollider sphere)
        {
            float maxScale = Mathf.Max(combinedScale.x, combinedScale.y, combinedScale.z);
            info.radius = sphere.radius * maxScale;
            info.position = referenceTransform.InverseTransformPoint(sphere.transform.TransformPoint(sphere.center));
            info.rotation = relativeRotation;
        }
        // CapsuleCollider
        else if (collider is CapsuleCollider capsule)
        {
            float horizontalScale = Mathf.Max(combinedScale.x, combinedScale.z); // Radius uses X/Z
            info.radius = capsule.radius * horizontalScale;
            info.height = capsule.height * combinedScale.y; // Height uses Y
            info.direction = capsule.direction;
            info.position = referenceTransform.InverseTransformPoint(capsule.transform.TransformPoint(capsule.center));
            info.rotation = relativeRotation;
        }

        return info;
    }

    private Transform GetNearestMeshOrRoot(Transform target)
    {
        Transform current = target;
        while (current.parent != null)
        {
            if (current.parent.GetComponent<MeshFilter>() != null)
                return current.parent;

            current = current.parent;
        }
        return target.root;
    }

    private Vector3 GetCombinedScale(Transform target, Transform reference)
    {
        Vector3 scale = target.localScale;
        Transform parent = target.parent;

        while (parent != null && parent != reference)
        {
            scale = Vector3.Scale(scale, parent.localScale);
            parent = parent.parent;
        }

        return scale;
    }

    void SaveMeshes(string relativeExportPath, MeshFilter[] meshFilters, Vector3 center, bool applyBoundingBox = true, bool saveSubMeshes = false, bool saveSubMeshTransform = false, bool saveCombinedSubmeshes = false, SimObjPhysics topmostSimObjPhysics = null)
    {
        Debug.Log("saving mesh");

        ExportedAssetInfo exportedAssetInfo = new ExportedAssetInfo();
        exportedAssetInfo.bbox_center.position = center.ToString("0.00000");

        string baseFileName = Path.GetFileNameWithoutExtension(relativeExportPath);

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("mtllib " + baseFileName + ".mtl");
        int lastIndex = 0;

        // START GOING THROUGH ALL MESH FILTERS HERE
        for(int i = 0; i < meshFilters.Length; i++)
        {
            MeshFilter mf = meshFilters[i];
            //ensure mesh name is unique because SOMETIMES THEY ARE NAMED THE SAME IM SORRY
            //string meshName = mf.gameObject.name + "_" + i.ToString();
            string meshName = mf.sharedMesh.name;

            MeshData meshData = FillMeshData(meshFilters[i], meshName, topmostSimObjPhysics);
            exportedAssetInfo.meshes.Add(meshData);

            if(!saveCombinedSubmeshes & saveSubMeshes)
            {
                sb = new StringBuilder();
                sb.AppendLine("mtllib " + baseFileName + ".mtl");
                lastIndex = 0;
            }

            Mesh msh = mf.sharedMesh;
            if (msh == null)
            {
                Debug.LogError("No mesh found for " + mf.gameObject.name);
                continue;
            }

            MeshRenderer mr = mf.gameObject.GetComponent<MeshRenderer>();
            {
                string exportName = meshName;
                // if (true)
                // {
                //     exportName += "_" + i;
                // }
                sb.AppendLine("g " + exportName);
            }

            if(mr != null)
            {
                Material[] mats = mr.sharedMaterials;

                for(int j=0; j < mats.Length; j++)
                {
                    Material m = mats[j];
                    if (m != null)
                    {
                        if (!allMaterials.ContainsKey(m.name))
                        {
                            allMaterials[m.name] = m;
                        }
                    }
                    else
                        Debug.LogWarning("No material found for " + meshName);
                }
            }
            else
            {
                Debug.LogWarning("No mesh renderer found for " + meshName);
            }

            if (skipMeshExport)
                continue;

            int faceOrder = (int)Mathf.Clamp((mf.gameObject.transform.lossyScale.x * mf.gameObject.transform.lossyScale.z), -1, 1);

            //export vector data (FUN :D)!
            foreach (Vector3 vx in msh.vertices)
            {
                Vector3 v = vx;
                if (false) // TODO: applyScale,if true, must apply it too all children object
                {
                    v = MultiplyVec3s(v, mf.gameObject.transform.lossyScale);
                }
                
                if (!saveSubMeshes) //true) //applyRotation)
                {
  
                    v = RotateAroundPoint(v, Vector3.zero, mf.gameObject.transform.rotation);
                    //v = RotateAroundPoint(v, Vector3.zero, mf.gameObject.transform.localRotation);

                }

                if (!saveSubMeshes) //true) //applyPosition)
                {
                    v += mf.gameObject.transform.position;
                    //v += mf.gameObject.transform.localPosition;
                }

                if (false) //applyBoundingBox) //true)// move to bouning box center
                    v -= center;                

                v.x *= -1;
                sb.AppendLine("v " + v.x + " " + v.y + " " + v.z);
            }

            foreach (Vector3 vx in msh.normals)
            {
                Vector3 v = vx;
                
                if (false) //applyScale)
                {
                    v = MultiplyVec3s(v, mf.gameObject.transform.lossyScale.normalized);
                }
                if (!saveSubMeshes) //applyRotation)
                {
                    v = RotateAroundPoint(v, Vector3.zero, mf.gameObject.transform.rotation);
                    //v = RotateAroundPoint(v, Vector3.zero, mf.gameObject.transform.localRotation);
                }
                if (!saveSubMeshes) //true) //applyPosition)
                {
                    v += mf.gameObject.transform.position;
                    //v += mf.gameObject.transform.localPosition;
                }

                if (false) //true)// move to bouning box center
                    v -= center;    

                v.x *= -1;
                sb.AppendLine("vn " + v.x + " " + v.y + " " + v.z);

            }

            foreach (Vector2 v in msh.uv)
            {
                sb.AppendLine("vt " + v.x + " " + v.y);
            }

            for (int j=0; j < msh.subMeshCount; j++)
            {
                if(mr != null && j < mr.sharedMaterials.Length)
                {
                    if(mr.sharedMaterials[j] != null)
                    {
                        string matName = mr.sharedMaterials[j].name;
                        sb.AppendLine("usemtl " + matName);
                    }
                    else
                    {
                        sb.AppendLine("usemtl " + meshName + "_sm" + j);
                    }
                }
                else
                {
                    sb.AppendLine("usemtl " + meshName + "_sm" + j);
                }

                int[] tris = msh.GetTriangles(j);
                for(int t = 0; t < tris.Length; t+= 3)
                {
                    int idx2 = tris[t] + 1 + lastIndex;
                    int idx1 = tris[t + 1] + 1 + lastIndex;
                    int idx0 = tris[t + 2] + 1 + lastIndex;
                    if(faceOrder < 0)
                    {
                        sb.AppendLine("f " + ConstructOBJString(idx2) + " " + ConstructOBJString(idx1) + " " + ConstructOBJString(idx0));
                    }
                    else
                    {
                        sb.AppendLine("f " + ConstructOBJString(idx0) + " " + ConstructOBJString(idx1) + " " + ConstructOBJString(idx2));
                    }
                }
            }

            if(saveSubMeshes & !saveCombinedSubmeshes)
            {
                //write to disk
                Directory.CreateDirectory(Path.Combine(savePath, Path.GetDirectoryName(relativeExportPath), baseFileName));
                Debug.Log("writing to disk: " + Path.Combine(savePath, Path.Combine(Path.GetDirectoryName(relativeExportPath), baseFileName, baseFileName + "_" + i.ToString() + ".obj")));
                System.IO.File.WriteAllText( Path.Combine(savePath,  Path.Combine(Path.GetDirectoryName(relativeExportPath), baseFileName, baseFileName + "_" + i.ToString() + ".obj")), sb.ToString());
                Debug.Log("Write to disk done");
            }

            lastIndex += msh.vertices.Length;
        }


        if (skipMeshExport)
            return;

        if(true) //!saveSubMeshes)
        {
            //write to disk
            Debug.Log("writing obj to disk: " + Path.Combine(savePath, Path.Combine(Path.GetDirectoryName(relativeExportPath), baseFileName + ".obj")));
            System.IO.File.WriteAllText( Path.Combine(savePath,  Path.Combine(Path.GetDirectoryName(relativeExportPath), baseFileName + ".obj")), sb.ToString());
            Debug.Log("Write obj to disk done");
        }

        if (saveSubMeshTransform)  
        {
            //old  json export stuff is here
            // string json = JsonUtility.ToJson(new SerializableDictionary(mesh_transforms), true);
            // File.WriteAllText(Path.Combine(savePath, Path.Combine(Path.GetDirectoryName(relativeExportPath), baseFileName + ".json")), json);

            string json = JsonUtility.ToJson(exportedAssetInfo, true);
            File.WriteAllText(Path.Combine(savePath, Path.Combine(Path.GetDirectoryName(relativeExportPath), baseFileName + ".json")), json);
            Debug.Log("Saved mesh serializable dictionaries to json.");
        }    
    }

    Vector3 RotateAroundPoint(Vector3 point, Vector3 pivot, Quaternion angle)
    {
        return angle * (point - pivot) + pivot;
    }

    Vector3 MultiplyVec3s(Vector3 v1, Vector3 v2)
    {
        return new Vector3(v1.x * v2.x, v1.y * v2.y, v1.z * v2.z);
    }

    private string ConstructOBJString(int index)
    {
        string idxString = index.ToString();
        return idxString + "/" + idxString + "/" + idxString;
    }

    string MaterialToString(Material m)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("newmtl " + m.name);


        //add properties
        if (m.HasProperty("_Color"))
        {
            sb.AppendLine("Kd " + m.color.r.ToString() + " " + m.color.g.ToString() + " " + m.color.b.ToString());
            if (m.color.a < 1.0f)
            {
                //use both implementations of OBJ transparency
                sb.AppendLine("Tr " + (1f - m.color.a).ToString());
                sb.AppendLine("d " + m.color.a.ToString());
            }
        }
        if (m.HasProperty("_SpecColor"))
        {
            Color sc = m.GetColor("_SpecColor");
            sb.AppendLine("Ks " + sc.r.ToString() + " " + sc.g.ToString() + " " + sc.b.ToString());
        }
        if (true) 
        {
            //diffuse
            string _MainTex = TryExportTexture("_MainTex", m);
            Vector2 _mainTextureScale = m.GetTextureScale("_MainTex");
            if (_MainTex != "false")
            {
                sb.AppendLine("map_Kd " + _MainTex);
            }
            
            Debug.Log("Checking SecondaryTexture");
            string _SecondaryTex = TryExportTexture("_DetailAlbedoMap", m);
            Vector2 _secondaryTextureScale = m.GetTextureScale("_DetailAlbedoMap");
            if (_SecondaryTex != "false")
            {
                sb.AppendLine("map_Kd " + _SecondaryTex);
            }
            
            //spec map
            string _MetallicGlossMap = TryExportTexture("_MetallicGlossMap", m);
            if (_MetallicGlossMap != "false")
            {
                sb.AppendLine("map_Ks " + _MetallicGlossMap);
            }
            //bump map
            string _BumpMap = TryExportTexture("_BumpMap", m);
            if (_BumpMap != "false")
            {
                sb.AppendLine("map_Bump " + _BumpMap);
            }

            if (!Mat2Texture.ContainsKey(m.name))
            {
                Dictionary<string, string> matdict = new Dictionary<string, string>();
                matdict.Add("_MainTex", _MainTex);
                matdict.Add("main_texture_scale", _mainTextureScale.x.ToString() + " " + _mainTextureScale.y.ToString());
                matdict.Add("_DetailAlbedoMap", _SecondaryTex);
                matdict.Add("detail_texture_scale", _secondaryTextureScale.x.ToString() + " " + _secondaryTextureScale.y.ToString());

                matdict.Add("_MetallicGlossMap", _MetallicGlossMap);
                matdict.Add("_BumpMap", _BumpMap);

                matdict.Add("emission_rgba", m.GetColor("_EmissionColor").r.ToString() + " " + m.GetColor("_EmissionColor").g.ToString() + " " + m.GetColor("_EmissionColor").b.ToString() + " " + m.GetColor("_EmissionColor").a.ToString());
                //matdict.Add("specular_rgba", m.GetColor("_SpecColor").r.ToString() + " " + m.GetColor("_SpecColor").g.ToString() + " " + m.GetColor("_SpecColor").b.ToString());
                matdict.Add("specular", m.GetFloat("_SpecularHighlights").ToString()); // reflectance ?
                matdict.Add("smoothness", m.GetFloat("_Glossiness").ToString()); // reflectance ? _Glossiness (Smothness)
                matdict.Add("metallic", m.GetFloat("_Metallic").ToString());  // shininess ? _GlossyReflectons (Glossy Reflections) or _Metallic
                //matdict.Add("reflection", m.GetFloat("_GlossyReflectons").ToString());  // shininess ? _GlossyReflectons (Glossy Reflections) or _Metallic
                matdict.Add("albedo_rgba", m.color.r.ToString() + " " + m.color.g.ToString() + " " + m.color.b.ToString() + " " + m.color.a.ToString());
                Debug.Log(m.color.r.ToString() + " " + m.color.g.ToString() + " " + m.color.b.ToString() + " " + m.color.a.ToString());

                Mat2Texture.Add(m.name, matdict);
                Debug.Log("Adding " + m.name);
                //Debug.Log("Adding " + m.name + " to Mat2Texture" + Mat2Texture[m.name]["_MainTex"]);
            }

        }
        sb.AppendLine("illum 2");
        return sb.ToString();
    }

    string TryExportTexture(string propertyName, Material m)
    {
        if (m.HasProperty(propertyName))
        {
            Texture t = m.GetTexture(propertyName);
            if(t != null)
            {
                return ExportTexture((Texture2D)t);
            }
        }
        return "false";
    }

    string ExportTexture(Texture2D t)
    {
        string assetPath = AssetDatabase.GetAssetPath(t);
        //Debug.Log(assetPath);

        if(File.Exists(assetPath))
        {
            string textureName = Path.GetFileName(assetPath); // with extension
            string copyPath = Path.Combine(Path.Combine(savePath, "Textures"), textureName);
            //Debug.Log(copyPath);

            File.Copy(assetPath, copyPath, true);
            return copyPath;
        }
        else
            return "false";
        /*
        try
        {
            if (autoMarkTexReadable)
            {
                string assetPath = AssetDatabase.GetAssetPath(t);
                Debug.Log(assetPath);

                var tImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (tImporter != null)
                {
                    tImporter.textureType = TextureImporterType.Advanced;

                    if (!tImporter.isReadable)
                    {
                        tImporter.isReadable = true;

                        AssetDatabase.ImportAsset(assetPath);
                        AssetDatabase.Refresh();
                    }
                }
            }
            string exportName = lastExportFolder + "\\" + t.name + ".png";
            Texture2D exTexture = new Texture2D(t.width, t.height, TextureFormat.ARGB32, false);
            exTexture.SetPixels(t.GetPixels());
            System.IO.File.WriteAllBytes(exportName, exTexture.EncodeToPNG());
            return exportName;
        }
        catch (System.Exception ex)
        {
            Debug.Log("Could not export texture : " + t.name + ". is it readable?");
            return "null";
        }
        */

    }

    private static Dictionary<string, object> getJsonTransorm(Transform transform) {
        return new Dictionary<string, object>() {
                    {"position", transform.position},
                    {"rotationEuler", transform.rotation.eulerAngles},
                    {"rotation", transform.rotation},
                    {"scale", transform.localScale}
                };
    }

    public static Dictionary<string, object> getCollider(Collider c) {
        Dictionary<string, object> co = null;
        if (c != null) {
        if (c.GetType() == typeof(CapsuleCollider)) {
            var ct = c as CapsuleCollider;
            co = new Dictionary<string, object>(){
                {"type", "capsule"},
                {"center", ct.center},
                {"transformedCenter", ct.transform.TransformPoint(ct.center)},
                {"radius", ct.radius},
                {"transform",  getJsonTransorm(ct.transform)}
            };
        }
        else if (c.GetType() == typeof(BoxCollider)) {
            var ct = c as BoxCollider;
            co = new Dictionary<string, object>(){
                {"type", "box"},
                {"center", ct.center},
                {"transformedCenter", ct.transform.TransformPoint(ct.center)},
                {"size", ct.size},
               {"transform",  getJsonTransorm(ct.transform)}
            };
        }
        else if (c.GetType() == typeof(SphereCollider)) {
            var ct = c as SphereCollider;
            co = new Dictionary<string, object>(){
                {"type", "sphere"},
                {"center", ct.center},
                {"transformedCenter", ct.transform.TransformPoint(ct.center)},
                {"radius", ct.radius},
                {"transform",  getJsonTransorm(ct.transform)}
            };
        }
        else {
            co = new Dictionary<string, object>(){
                {"unsupported", true},
                {"type", c.GetType().ToString()}
            };
        }
        }
        else {
            co = new Dictionary<string, object>(){
                {"error", "Null collider"}
            };
        }
        // else if (c.GetType() == typeof(MeshRenderer)) {

        // }
        return co;
    }

    // [UnityEditor.MenuItem("Procedural/Get Primitive Colliders from PDB")]
    public static void ExportProcthorPrimitiveColliders() {
        var assetDb = GameObject.FindObjectOfType<ProceduralAssetDatabase>();
        var m = assetDb.prefabs.Select(
                (p, i) => (sop: p.GetComponent<SimObjPhysics>(), i))
            .Where(x => x.sop != null);// && x.assetID == "Fertility_Statue_1");

        var jsonResolver = new ShouldSerializeContractResolver();
        
        var colliderDict =  new Dictionary<string, object>();
        foreach (var (sop, i) in m) {
            
            var assetId = sop.gameObject.name;
            Debug.Log($"assetID {assetId}");

            // var meshColliders = sop.GetComponentsInChildren<MeshCollider>();

            // var colliders = sop.MyColliders.Count() > 0 ? sop.MyColliders.Select(getCollider) : new List<Dictionary<string, object>>() { getCollider(meshCollider) };
            var colliders = sop.MyColliders.Select(getCollider);
            Debug.Log("collider count: " + colliders.Count());
            
            
            if (!colliderDict.ContainsKey(assetId)) { 
                colliderDict.Add(assetId, new Dictionary<string, object>() { {"colliders", colliders}, {"assetId", assetId} });
            }
            else {
                Debug.Log($"----- Error duplicate key {sop.assetID} object name: {sop.objectID}, GO name: {sop.gameObject.name}, index: {i}" );
            }
            // var jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(
            // new Dictionary<string, object>() {
            //     {"colliders", colliders},
            //     {"assetId", sop.assetID}
            // },
            // Newtonsoft.Json.Formatting.None,
            // new Newtonsoft.Json.JsonSerializerSettings() {
            //     ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
            //     ContractResolver = jsonResolver
            // }
            // );

            // Debug.Log($"st {jsonStr}");

        }

        var jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(
            colliderDict,
            Newtonsoft.Json.Formatting.None,
            new Newtonsoft.Json.JsonSerializerSettings() {
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                ContractResolver = jsonResolver
            }
            );

            Debug.Log($"st {jsonStr}");
            var fileName = $"{Application.dataPath}/test.json";
            Debug.Log($"Save as: {fileName}");

            System.IO.StreamWriter file = new System.IO.StreamWriter($"{Application.dataPath}/test.json");
            // file.WriteLine(jsonStr);
            file.Write(jsonStr);

            file.Close();

//             using(StreamWriter writetext = new StreamWriter("write.txt"))
// {
//     writetext.writeLine
//     writetext.WriteLine("writing in text file");
// }

        // var colliderDict = m.ToDictionary(sop => (sop.assetID, new Dictionary<string, object>() {
        //         {"colliders", sop.MyColliders.Select(getCollider)},
        //         {"assetId", sop.assetID}
        // }));

        //     var jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(
        //     colliderDict,
        //     Newtonsoft.Json.Formatting.None,
        //     new Newtonsoft.Json.JsonSerializerSettings() {
        //         ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
        //         ContractResolver = jsonResolver
        //     }
        //     );

        //     Debug.Log($"st {jsonStr}");

    }
}
