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

public static class PathExtensions
{
    public static string GetRelativePath(string relativeTo, string path)
    {
        Uri fromUri = new Uri(Path.GetFullPath(relativeTo));
        Uri toUri = new Uri(Path.GetFullPath(path));

        if (fromUri.Scheme != toUri.Scheme) { return path; } // path can't be made relative.

        Uri relativeUri = fromUri.MakeRelativeUri(toUri);
        string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

        if (toUri.Scheme.Equals("file", StringComparison.InvariantCultureIgnoreCase))
        {
            relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
        }

        return relativePath;
    }
}

public class DownloadThorAssets : MonoBehaviour
{
    public string savePath = "Assets/ExportedThorAssets";
    public string assetPath = "Assets/Physics/SimObjsPhysics";
    public string materialPath = "Assets/Resources/QuickMaterials";
    //public string doorAssetPath = "Assets/Physics/SimObjsPhysics/ManipulaTHOR Objects/Doorways/Prefabs";
    bool applyBoundingBox = false; // TODO: should always false

    [Header("Apply scale to all mesh")]
    public bool applyScale = false;

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
        public Vector3 meshRelativePosition;
        //this joint's range of movement in local space
        //if jointType == rotate, this is a change in rotation in euler angles
        //if jointType == slide, this is a change in position
        public Vector3 lowRange;
        public Vector3 highRange;

        public GameObject jointGO;
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

    //BAD BAD BAD IGNORE IGNORE IGNORE PLEASE SEND HELP
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
                if (m.name.Contains("Grunge"))
                    continue;
                if (m.name.Contains("Glass"))
                    continue;

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

        // Move Textures directory up one level
        Directory.CreateDirectory(Path.Combine("Assets/iTHOR", "Textures")); 
        
        // Get all prefab files, excluding specified directories
        List<string> excludeDirectories = new List<string> { 
            "Custom Project Objects", 
            "Entryway Objects", 
            "RoboTHOR_Assets_Environment" 
        };

        string[] prefabFiles = GetFilesExcludingDirectories(directoryPath, "*.prefab", excludeDirectories);

        // Filter out prefabs that contain "Sliced" in their names
        prefabFiles = prefabFiles.Where(prefabPath => !Path.GetFileNameWithoutExtension(prefabPath).Contains("Sliced")).ToArray();

        int assetsProcessed = 0;
        foreach (string prefabPath in prefabFiles)
        {

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
                    if (mat != null && (mat.name == "Placeable_Surface_Mat") || (mat.name == "Water_Volume_Surface_Mat") || mat.name == "Material.002 Stove")
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
            Debug.Log(parent.name + " center" + center.ToString());
        }       
        else
        {
            Debug.Log("No bounding box found for " + go.name);
        } 
    
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
    
        if (!skipMaterialExport)
        {
            Debug.Log("saving material");

            SaveMaterials(relativeExportPath);
            allMaterials.Clear();
        }
    }

    public void SaveMaterials(string relativeExportPath)
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

    // MAKE SURE THIS WORKS FOR 'STRUCTURE" TAG.
    public MeshData FillStructureMeshData(MeshFilter meshfilter, string meshName, SimObjPhysics topmostSimObjPhysics)
    {
        var go = meshfilter.gameObject;

        var meshData = new MeshData
        {
            // Default with this mesh's current local pos, rot, and scale
            parentRelativePosition = Vector3.zero,
            parentRelativeRotation = Quaternion.identity,
            parentRelativeScale = Vector3.one, // go.transform.localScale,
            meshName = meshName,
            parentName = ""
        };

        // FILL IN THE REST primitives and 
        //public AllMyPrimitiveColliders primitiveColliders = new AllMyPrimitiveColliders();
        //public AllMyPlaceableZones placeableZoneColliders = new AllMyPlaceableZones();
        int i = 0;
        var meshFiltersGameObject = go;
        var mesh_parent = GameObject.Find("Objects");
        //var child = go.transform.Find("Colliders"); 
        var colliders = go.GetComponentsInChildren<Collider>();
        Debug.Log("---------------------------------go: " + go.name + " " + colliders.Length);
    
        var topmostparent = go.transform.parent;
        
        while (colliders.Length == 0)
        {
            Debug.LogWarning("No colliders found for " + go.name);
            colliders = topmostparent.GetComponentsInChildren<Collider>();
            if(topmostparent.transform.parent == mesh_parent)
                break;
            topmostparent = topmostparent.transform.parent;
        }

        if (colliders.Length > 0)
        {
            //foreach (var collider in child.GetComponents<Collider>())
            foreach (var collider in colliders)
            {
                if (!collider.enabled || !collider.gameObject.activeInHierarchy)
                    continue;

                // skip if the collider is a SimObjPhysics -> hand towels assets are
                //if (collider.transform != null && collider.transform.gameObject.tag == "SimObjPhysics")
                //{
                //    Debug.Log("skipping SimObjPhysics: " + collider.gameObject.name);
                //    continue;
                //}


                Debug.Log("ColliderInfo: " + collider.gameObject.name);
                Debug.Log("mesh_parent: " + mesh_parent.name);
                Debug.Log("meshFiltersGameObject: " + meshFiltersGameObject.name);
                var colliderInfo = GetColliderInfo(collider, meshFiltersGameObject, mesh_parent);
                if (colliderInfo != null)
                {
                    if (!collider.isTrigger)
                    {

                        if (collider.transform.parent != null && collider.transform.parent.gameObject.tag == "SimObjPhysics")
                        {
                            Debug.Log("skipping SimObjPhysics parent: " + collider.gameObject.name);
                            continue;
                        }

                        if (collider.transform.parent != null && collider.transform.parent.parent != null && collider.transform.parent.parent.gameObject.tag == "SimObjPhysics")
                        {
                            Debug.Log("skipping SimObjPhysics parent parent: " + collider.gameObject.name);
                            continue;
                        }
                        
                        if (collider.transform.parent != null && collider.transform.parent.parent != null && collider.transform.parent.parent.parent != null && collider.transform.parent.parent.parent.gameObject.tag == "SimObjPhysics")
                        {
                            Debug.Log("skipping SimObjPhysics parent parent parent: " + collider.gameObject.name);
                            continue;
                        }
                        
                        meshData.primitiveColliders.myPrimitiveColliders.Add(colliderInfo);
                        i++;
                    }         
                    else if (collider.GetComponent("Contains") != null)
                    {
                        var Name = collider.transform.parent.gameObject.name;
                        if (Name.Contains("Cabinet"))
                        {
                            meshData.placeableZoneColliders.myPlaceableZones.Add(colliderInfo);
                        }
                        else{
                            if (collider.transform.parent != null && collider.transform.parent.gameObject.tag == "SimObjPhysics")
                            {
                                Debug.Log("skipping SimObjPhysics parent: " + collider.gameObject.name);
                                continue;
                            }

                            if (collider.transform.parent != null && collider.transform.parent.parent != null && collider.transform.parent.parent.gameObject.tag == "SimObjPhysics")
                            {
                                Debug.Log("skipping SimObjPhysics parent parent: " + collider.gameObject.name);
                                continue;
                            }
                            
                            if (collider.transform.parent != null && collider.transform.parent.parent != null && collider.transform.parent.parent.parent != null && collider.transform.parent.parent.parent.gameObject.tag == "SimObjPhysics")
                            {
                                Debug.Log("skipping SimObjPhysics parent parent parent: " + collider.gameObject.name);
                                continue;
                            }
                            meshData.placeableZoneColliders.myPlaceableZones.Add(colliderInfo);
                        }
                    }
                    
                    Debug.Log("not null: " + collider.gameObject.name);
                }
            }
        }
  
        return meshData;
    }


    //take one mesh filter, and get all the information about it ready to go
    public MeshData FillMeshData(MeshFilter meshfilter, string meshName, SimObjPhysics topmostSimObjPhysics)
    {
        Debug.Log("FillMeshData called for mesh: " + meshName);
        var go = meshfilter.gameObject;

        // root 
        var root = topmostSimObjPhysics.transform.gameObject;
        var all_meshes = root.GetComponentsInChildren<MeshFilter>();

        // Filter out meshes with null shared meshes or names starting with "PS" (particle system)
        all_meshes = all_meshes.Where(mesh => 
            mesh.sharedMesh != null && 
            !mesh.sharedMesh.name.StartsWith("PS") && 
            !mesh.name.Contains("PS_") &&
            !mesh.name.Contains("Placeable")
        ).ToArray();

        // Build mesh hierarchy
        var meshHierarchy = BuildMeshHierarchy(all_meshes, root.transform);

        // Find parent for this specific mesh
        MeshFilter parentMeshFilter = null;
        GameObject mesh_parent;
        
        if (meshHierarchy.TryGetValue(meshfilter, out parentMeshFilter))
        {
            // Found a parent mesh
            mesh_parent = parentMeshFilter.gameObject;
        }
        else
        {
            // No parent found, use the mesh itself as parent
            mesh_parent = root;
        }
        
        // Calculate transforms relative to the parent mesh
        var scale = GetCombinedScale(go.transform, mesh_parent.transform);
        var position = mesh_parent.transform.InverseTransformPoint(go.transform.position);
        var rotation = Quaternion.Inverse(mesh_parent.transform.rotation) * go.transform.rotation;
        var meshData = new MeshData
        {
            parentRelativePosition = position,
            parentRelativeRotation = rotation,
            parentRelativeScale = scale,
            meshName = meshName,
            parentName = mesh_parent.name.Replace(" ", "_").Replace(".", "_")
        };
        /*
        var meshData = new MeshData
        {
            parentRelativePosition = mesh_parent.transform.InverseTransformPoint(go.transform.position),
            parentRelativeRotation = Quaternion.Inverse(mesh_parent.transform.rotation) * go.transform.rotation,
            parentRelativeScale = meshHierarchy.TryGetValue(meshfilter, out _) ? 
                new Vector3(
                    go.transform.localScale.x / mesh_parent.transform.localScale.x,
                    go.transform.localScale.y / mesh_parent.transform.localScale.y,
                    go.transform.localScale.z / mesh_parent.transform.localScale.z
                ) : 
                Vector3.Scale(go.transform.localScale, mesh_parent.transform.localScale),
            meshName = meshName,
            parentName = mesh_parent.name.Replace(" ", "_").Replace(".", "_")
        };
        */
        // Keep track of what transforms we have traversed upward so we can compare them to associated joints later
        List<Transform> transformsTraversed = new List<Transform>();
        transformsTraversed.Add(go.transform);
        transformsTraversed.Add(mesh_parent.transform);
        
        // Get joint info -- parent . mesh_parent or itself
        var meshFiltersGameObject = meshfilter.gameObject;
        meshData.jointInfo = CollectValidJoints(meshfilter, ref meshData, transformsTraversed, meshFiltersGameObject.transform.parent.gameObject.transform, topmostSimObjPhysics.transform);
        Debug.Log($"Joint info: {meshData.jointInfo}");
        
        GameObject collider_parent = null;
        if (meshData.jointInfo != null)
        {
            Debug.Log("Joint info found for mesh: " + meshData.meshName);
            collider_parent = meshData.jointInfo.jointGO;
        }

        // Get colliders
        //collider_parent = meshFiltersGameObject.transform.parent.gameObject; // was okay for iTHOR and most of THOR assets 
        //Debug.Log("collider_parent: " + collider_parent.name);
        //CollectValidColliders(collider_parent, meshfilter, ref meshData, mesh_parent);
        
        // Get colliders
        //collider_parent = mesh_parent;
        //collider_parent = meshFiltersGameObject.transform.parent.gameObject; // was okay for iTHOR and most of THOR assets 
        // for assets with handles. simobj that have nested meshfilters with colliders. 
        
        var collider_parent_transform =  meshFiltersGameObject.transform.parent.gameObject.transform.Find("Colliders");
        if (collider_parent_transform == null)
        {
            // specifically for handles 
            collider_parent_transform =  meshFiltersGameObject.transform.Find("Colliders");
            if (collider_parent_transform == null)
            {
                Debug.LogWarning("No collider found for " + mesh_parent.name);
            }
        }
        Debug.Log("collider_parent_transform: " + collider_parent_transform + " " + meshFiltersGameObject.transform.parent.gameObject.name);

        if (collider_parent_transform != null)
        {
            collider_parent = collider_parent_transform.gameObject;
            Debug.Log("-------------------mesh_parent: " + collider_parent.name);
            Debug.Log("-------------------mesh: " + meshfilter.gameObject.name);
            
            //collider_parent = meshFiltersGameObject.transform.parent.gameObject;
            var colliders = collider_parent.GetComponentsInChildren<Collider>();
            // collider_parent 

            if (colliders.Length > 0)
            {
                
                // Process colliders
                foreach (var collider in colliders)
                {
                    if (!collider.enabled || !collider.gameObject.activeInHierarchy)
                        continue;
                    
                    if (collider.gameObject.name.Contains("rb"))
                        continue;

                    
                    
                    Debug.Log("ColliderInfo: " + collider.gameObject.name);
                    Debug.Log("mesh_parent: " + mesh_parent.name);
                    Debug.Log("meshFiltersGameObject: " + meshFiltersGameObject.name);
                    
                    var colliderInfo = GetColliderInfo(collider, meshFiltersGameObject, meshFiltersGameObject);
                    if (colliderInfo != null)
                    {
                        if (!collider.isTrigger)
                        {
                            meshData.primitiveColliders.myPrimitiveColliders.Add(colliderInfo);
                        }
                        
                        Debug.Log("not null: " + collider.gameObject.name);
                    }
                }
            }
        }
        else{
            Debug.LogWarning("1 No collider found for " + mesh_parent.name);
        }
 

        // for receptacles
        //collider_parent
        collider_parent = meshFiltersGameObject.transform.parent.gameObject; // was okay for iTHOR and most of THOR assets 
        //collider_parent = topmostSimObjPhysics.transform.gameObject;
        //var trigger_colliders = collider_parent.GetComponentsInChildren<Collider>();
        var trigger_colliders = new List<Collider>();
        foreach (Transform t in collider_parent.transform)
        {
            if (t.GetComponent<Collider>() != null)
                trigger_colliders.Add(t.GetComponent<Collider>());
        }
        // Process colliders
        foreach (var collider in trigger_colliders)
        {
            if (!collider.enabled || !collider.gameObject.activeInHierarchy)
                continue;
            
            if (collider.gameObject.name.Contains("rb"))
                continue;


            var colliderInfo = GetColliderInfo(collider, meshFiltersGameObject, meshFiltersGameObject);
            if (colliderInfo != null)
            {
                if (collider.isTrigger && collider.GetComponent("Contains") != null)
                {
                    meshData.placeableZoneColliders.myPlaceableZones.Add(colliderInfo);
                }                
            }
        }

        
        return meshData;
        
    }

    private JointInfo CollectValidJoints(MeshFilter meshfilter, ref MeshData meshData, List<Transform> transformsTraversed, Transform parent, Transform topmostSimObjPhysics)
    {
        Debug.Log("CollectValidJoints called for mesh: " + meshData.meshName);

        JointInfo jointInfo = new JointInfo();
        jointInfo.jointType = "none";

        // Find the SimObjPhysics component that contains this mesh
        SimObjPhysics simObjPhysics = null;
        Transform current = meshfilter.transform;

        while (current != null)
        {
            simObjPhysics = current.GetComponent<SimObjPhysics>();
            if (simObjPhysics != null)
                break;
            current = current.parent;
        }

        if (simObjPhysics == null)
        {
            Debug.Log("No SimObjPhysics found for mesh: " + meshData.meshName);
            return jointInfo;
        }

        // Check if this object has a CanOpen_Object component
        CanOpen_Object canOpen = simObjPhysics.GetComponent<CanOpen_Object>();
        CanToggleOnOff canToggle = simObjPhysics.GetComponent<CanToggleOnOff>();
        // make sure it is active 
        if (canOpen != null && !canOpen.gameObject.activeInHierarchy)
            canOpen = null;
        if (canToggle != null && !canToggle.gameObject.activeInHierarchy)
            canToggle = null;

        
        if (canOpen == null && canToggle==null)
        {
            Debug.Log("No CanToggle_Object or CanToggleOnOff component found for: " + simObjPhysics.name);
            return jointInfo;
        }
        var allMovingParts = new List<GameObject>();
        if (canOpen != null && canOpen.MovingParts != null)
            allMovingParts.AddRange(canOpen.MovingParts);
        if (canToggle != null && canToggle.MovingParts != null)
            allMovingParts.AddRange(canToggle.MovingParts);

        // Check if this mesh is part of a moving part
        foreach (GameObject movingPart in allMovingParts)
        {
            if (movingPart == null)
                continue;
            
            Debug.Log("---------movingPart: " + movingPart.name);
            Debug.Log("---------meshData.meshName: " + meshData.meshName);
            Debug.Log("---------meshfilter.gameObject.name: " + meshfilter.gameObject.transform.parent.name);
            // Check if this mesh is part of this moving part
            if (IsChildOf(meshfilter.gameObject, movingPart) || meshfilter.gameObject == movingPart)
            {
                // but if parent has meshfilter and parent is child of movingpart, then skip (example. handle door and door are both child of movingpart)
                var meshParent = meshfilter.gameObject.transform.parent.gameObject;
                if (meshParent.GetComponent<MeshFilter>() != null && IsChildOf(meshParent, movingPart))
                    continue;
                if (meshParent.GetComponent<MeshFilter>() != null && meshParent == movingPart)
                    continue;


                Debug.Log("Found moving part for mesh: " + meshData.meshName + " - " + movingPart.name);
                
                // Determine joint type based on the movement type
                if (canOpen != null && canOpen.MovingParts.Contains(movingPart))
                {
                    // Handle CanOpen_Object joints
                    if (canOpen.GetMovementType() == CanOpen_Object.MovementType.Rotate)
                    {
                        jointInfo.jointType = "rotate";
                        
                        // Set rotation limits based on the current openness
                        Vector3 lowRange = Vector3.zero;
                        Vector3 highRange = Vector3.zero;
                        
                        // Get the rotation difference between open and closed positions
                        for (int i = 0; i < canOpen.MovingParts.Length; i++)
                        {
                            if (canOpen.MovingParts[i] == movingPart)
                            {
                                // Use the openPositions and closedPositions arrays to determine rotation axis and limits
                                Vector3 closedPos = canOpen.closedPositions[i];
                                Vector3 worldClosedPos = movingPart.transform.TransformPoint(closedPos);
                                Vector3 localClosedPos = meshfilter.transform.InverseTransformPoint(worldClosedPos);

                                Vector3 openPos = canOpen.openPositions[i];
                                Vector3 worldOpenPos = movingPart.transform.TransformPoint(openPos);
                                Vector3 localOpenPos = meshfilter.transform.InverseTransformPoint(worldOpenPos);

                                Vector3 rotDiff = localOpenPos - localClosedPos; // MeshFilter coordiante. is assumption.

                              
                                // Determine which axis has the largest rotation
                                if (Mathf.Abs(rotDiff.x) > Mathf.Abs(rotDiff.y) && Mathf.Abs(rotDiff.x) > Mathf.Abs(rotDiff.z))
                                {
                                    // X-axis rotation
                                    lowRange.x = 0;
                                    highRange.x = rotDiff.x;
                                }
                                else if (Mathf.Abs(rotDiff.y) > Mathf.Abs(rotDiff.x) && Mathf.Abs(rotDiff.y) > Mathf.Abs(rotDiff.z))
                                {
                                    // Y-axis rotation
                                    lowRange.y = 0;
                                    highRange.y = rotDiff.y;
                                }
                                else
                                {
                                    // Z-axis rotation
                                    lowRange.z = 0;
                                    highRange.z = rotDiff.z;
                                }
                            
                                
                                break;
                            }
                        }
                        
                        jointInfo.lowRange = lowRange;
                        jointInfo.highRange = highRange;
                    }
                    else if (canOpen.GetMovementType() == CanOpen_Object.MovementType.Slide)
                    {
                        jointInfo.jointType = "slide";
                        
                        // Set slide limits based on the current openness
                        for (int i = 0; i < canOpen.MovingParts.Length; i++)
                        {
                            if (canOpen.MovingParts[i] == movingPart)
                            {
                                // Use the openPositions and closedPositions arrays to determine slide direction and distance
                                Vector3 closedPos = canOpen.closedPositions[i];
                                Vector3 worldClosedPos = movingPart.transform.TransformPoint(closedPos);
                                Vector3 localClosedPos = meshfilter.transform.InverseTransformPoint(worldClosedPos);

                                Vector3 openPos = canOpen.openPositions[i];
                                Vector3 worldOpenPos = movingPart.transform.TransformPoint(openPos);
                                Vector3 localOpenPos = meshfilter.transform.InverseTransformPoint(worldOpenPos);

                                Vector3 slideVector = localOpenPos - localClosedPos; // MeshFilter coordiante. is assumption.

                                // transform slide vector to local meshfilter space
                                //slideVector = meshfilter.transform.InverseTransformDirection(slideVector);
                                
                                jointInfo.lowRange = Vector3.zero;
                                jointInfo.highRange = Quaternion.Inverse(movingPart.transform.localRotation) * slideVector;
                                //jointInfo.highRange = movingPart.transform.localRotation * slideVector;

                                break;
                            }
                        }
                    }
                }
                else if (canToggle != null && canToggle.MovingParts.Contains(movingPart))
                {
                    // Handle CanToggleOnOff joints
                    // Check if this is a sliding toggle (if the component has a movement type property)
                    bool isSliding = false;
                    
                    // Try to access the movement type using reflection since not all versions may have this
                    try {
                        var movementTypeProperty = canToggle.GetType().GetProperty("MovementType");
                        if (movementTypeProperty != null) {
                            var movementType = movementTypeProperty.GetValue(canToggle);
                            // Check if it's a slide type (assuming similar enum to CanOpen_Object)
                            isSliding = movementType.ToString().Contains("Slide");
                        }
                    } catch {
                        // If we can't access it, default to rotate
                        isSliding = false;
                    }
                    
                    if (isSliding) {
                        jointInfo.jointType = "slide";
                        
                        // Set slide limits based on the toggle positions
                        for (int i = 0; i < canToggle.MovingParts.Length; i++)
                        {
                            if (canToggle.MovingParts[i] == movingPart)
                            {
                                // Use the OnPositions and OffPositions arrays to determine slide direction and distance
                                Vector3 offPos = canToggle.OffPositions[i];
                                Vector3 onPos = canToggle.OnPositions[i];
                                Vector3 slideVector = onPos - offPos;
                                // Transform slide vector to local meshfilter space
                                slideVector = meshfilter.transform.InverseTransformDirection(slideVector);
                                jointInfo.lowRange = Vector3.zero;
                                jointInfo.highRange = slideVector;
                                break;
                            }
                        }
                    } else {
                        jointInfo.jointType = "rotate"; // Most toggle objects rotate
                        
                        // Set rotation limits based on the toggle positions
                        Vector3 lowRange = Vector3.zero;
                        Vector3 highRange = Vector3.zero;
                        
                        // Get the rotation difference between on and off positions
                        for (int i = 0; i < canToggle.MovingParts.Length; i++)
                        {
                            if (canToggle.MovingParts[i] == movingPart)
                            {
                                // Use the onPositions and offPositions arrays to determine rotation axis and limits
                                Vector3 offRot = canToggle.OffPositions[i];
                                Vector3 onRot = canToggle.OnPositions[i];
                                Vector3 rotDiff = onRot - offRot;
                                
                                // Determine which axis has the largest rotation
                                if (Mathf.Abs(rotDiff.x) > Mathf.Abs(rotDiff.y) && Mathf.Abs(rotDiff.x) > Mathf.Abs(rotDiff.z))
                                {
                                    // X-axis rotation
                                    lowRange.x = 0;
                                    highRange.x = rotDiff.x;
                                }
                                else if (Mathf.Abs(rotDiff.y) > Mathf.Abs(rotDiff.x) && Mathf.Abs(rotDiff.y) > Mathf.Abs(rotDiff.z))
                                {
                                    // Y-axis rotation
                                    lowRange.y = 0;
                                    highRange.y = rotDiff.y;
                                }
                                else
                                {
                                    // Z-axis rotation
                                    lowRange.z = 0;
                                    highRange.z = rotDiff.z;
                                }
                                
                                break;
                            }
                        }
                        
                        jointInfo.lowRange = lowRange;
                        jointInfo.highRange = highRange;
                    }
                }
                
                // Set joint position relative to parent
                // apply onPositions to jointInfo.meshRelativePosition  
                if (movingPart.gameObject.GetComponent<MeshFilter>() != null)
                {
                    jointInfo.meshRelativePosition = new Vector3(0, 0, 0);
                }                    
                else
                {
                    // position of the moving part relative to the meshFilter
                    //jointInfo.meshRelativePosition = parent.InverseTransformPoint(movingPart.transform.position);
                    jointInfo.meshRelativePosition = meshfilter.transform.InverseTransformPoint(movingPart.transform.position);

                }


                // We can keep the quaternion-based rotation calculation for debugging purposes only
                if (jointInfo.jointType == "rotate") {
                    Vector3 rotRange = jointInfo.highRange;
                    Vector3 worldAxis = Vector3.zero;
                    float angle = 0f;
                    
                    // First, determine which component we're working with (CanOpen or CanToggle)
                    if (canOpen != null && canOpen.MovingParts.Contains(movingPart)) {
                        // Find the index of this moving part
                        for (int i = 0; i < canOpen.MovingParts.Length; i++) {
                            if (canOpen.MovingParts[i] == movingPart) {
                                // Get the closed and open rotations
                                Vector3 closedRot = canOpen.closedPositions[i];
                                Vector3 openRot = canOpen.openPositions[i];
                                
                                // Store original rotation
                                Quaternion originalRotation = movingPart.transform.rotation;
                                
                                // Temporarily set to closed position to get base orientation
                                movingPart.transform.eulerAngles = closedRot;
                                Quaternion closedOrientation = movingPart.transform.rotation;
                                
                                // Set to open position to get target orientation
                                movingPart.transform.eulerAngles = openRot;
                                Quaternion openOrientation = movingPart.transform.rotation;
                                
                                // Calculate the rotation delta in world space
                                Quaternion rotationDelta = openOrientation * Quaternion.Inverse(closedOrientation);
                                
                                // Extract the rotation axis and angle
                                rotationDelta.ToAngleAxis(out angle, out Vector3 axis);
                                worldAxis = axis.normalized;
                                
                                // Restore original rotation
                                movingPart.transform.rotation = originalRotation;
                                
                                break;
                            }
                        }
                    } else if (canToggle != null && canToggle.MovingParts.Contains(movingPart)) {
                        // Similar implementation for toggle objects
                        for (int i = 0; i < canToggle.MovingParts.Length; i++) {
                            if (canToggle.MovingParts[i] == movingPart) {
                                Vector3 offRot = canToggle.OffPositions[i];
                                Vector3 onRot = canToggle.OnPositions[i];
                                
                                // Store original rotation
                                Quaternion originalRotation = movingPart.transform.rotation;
                                
                                // Temporarily set to off position to get base orientation
                                movingPart.transform.eulerAngles = offRot;
                                Quaternion offOrientation = movingPart.transform.rotation;
                                
                                // Set to on position to get target orientation
                                movingPart.transform.eulerAngles = onRot;
                                Quaternion onOrientation = movingPart.transform.rotation;
                                
                                // Calculate the rotation delta in world space
                                Quaternion rotationDelta = onOrientation * Quaternion.Inverse(offOrientation);
                                
                                // Extract the rotation axis and angle
                                rotationDelta.ToAngleAxis(out angle, out Vector3 axis);
                                worldAxis = axis.normalized;
                                
                                // Restore original rotation
                                movingPart.transform.rotation = originalRotation;
                                
                                break;
                            }
                        }
                    }
                    
                    // Transform the world axis to meshfilter's local space for debugging
                    Vector3 localAxisRelativeToMovingPart = meshfilter.transform.InverseTransformDirection(worldAxis).normalized;
                    
                    // Debug log only
                    Debug.Log($"Rotation joint - World axis: {worldAxis}, Local axis: {localAxisRelativeToMovingPart}, Angle: {angle}");
                }
                else if (jointInfo.jointType == "slide") {
                    // For slide joints, we need to handle the slide vector similarly
                    Vector3 slideVector = jointInfo.highRange;
                    
                    // If meshfilter is the moving part itself, just use the slide vector directly
                    Vector3 localSlideVectorRelativeToMovingPart;
                    
                    if (meshfilter.gameObject == movingPart) {
                        localSlideVectorRelativeToMovingPart = slideVector;
                    } else {
                        // First convert the slide vector to world space from the current local space
                        Vector3 worldSlideVector = meshfilter.transform.TransformDirection(slideVector);
                        
                        // Then convert it to the meshfilter's local space relative to the moving part
                        localSlideVectorRelativeToMovingPart = meshfilter.transform.InverseTransformDirection(worldSlideVector);
                    }
                    
                    // Debug log only
                    Debug.Log($"Slide joint - Local vector relative to moving part: {localSlideVectorRelativeToMovingPart}");
                }
                
                jointInfo.jointGO = movingPart;
                
                return jointInfo;
            }
        }

        Debug.Log("Mesh is not part of any moving part: " + meshData.meshName);
        return jointInfo;
    }

    // Helper method to check if an object is a child of another
    private bool IsChildOf(GameObject child, GameObject parent)
    {
        Transform current = child.transform.parent;
        while (current != null)
        {
            if (current.gameObject == parent)
                return true;
            current = current.parent;
        }
        return false;
    }

    // THIS IS WRONG, and the only reason it's been allowed to exist is because static objects don't even use this logic,
    // since they have no joints
    private void CollectValidColliders(GameObject collider_parent, MeshFilter meshfilter, ref MeshData meshData, GameObject mesh_parent)
    {
        Debug.Log("call CollectValidColliders " + meshfilter.sharedMesh.name);
        var go = meshfilter.gameObject;

        Transform parent = go.transform.parent;

        //if (collider_parent != null)
        //{
        //    parent = collider_parent.transform;
        //}
        if (parent != null)
        {
            foreach (Transform child in parent)
            {
                //actually no we want to check ourself too
                //if (sibling == go.transform) continue ;
                bool skip = false;
                Debug.Log("I am at: " + go.name + " and checking " + parent.name + " children");
                MeshFilter[] all_mf = child.GetComponentsInChildren<MeshFilter>();
                if (all_mf.Count() > 0)
                    skip = true; // this is skipping receptacle for cabinet door. which btw should not be rotating with the door. for now. cabinet receptacle can be child of root next to dresser mesh
                
                Debug.Log(go.name  + " all_mf: " + all_mf.Count());
                /*
                for (int i = 0; i< all_mf.Count(); i++)
                {
                    if (all_mf[i] != meshfilter)
                    {
                        // if the meshfilter, is a child of my sibling. than i just skip collider associate with it but keep everything else
                        Transform mf_parent = meshfilter.gameObject.transform.parent;
                        if (mf_parent == child)
                            skip=true;
                        else
                            skip = false;
                    }
                }
                */
                // Include sibling's colliders and descendants recursively
                // skip condition is wrong for some candle objects. still skipping so just changed the hierarhy in prefab.
                if (!skip)
                {
                    Debug.Log("Transform CHild " + child.name);
                    AddCollidersRecursive(child, ref meshData, go, mesh_parent);
                }
                
            }
        }
    }
    // Done with redundant stuff

    private void AddCollidersRecursive(Transform child, ref MeshData meshData, GameObject meshFiltersGameObject, GameObject mesh_parent)
    {
        // Check if SimObjPhysics is found in the target or its descendants
        SimObjPhysics simObjPhysics = child.GetComponent<SimObjPhysics>();
        if (simObjPhysics != null)
        {
            // Check if the SimObjPhysics type is BathtubBasin, Shelf, or SinkBasin
            if (simObjPhysics.Type == SimObjType.BathtubBasin || simObjPhysics.Type == SimObjType.Shelf || simObjPhysics.Type == SimObjType.SinkBasin || simObjPhysics.Type == SimObjType.Cabinet)
            {
                // Continue the search and include colliders with the Contains component
                foreach (var collider in child.GetComponents<Collider>())
                {
                    //only look at enabled colliders, active colliders
                    if (!collider.enabled || !collider.gameObject.activeInHierarchy)
                        continue;

                    Debug.Log("ColliderInfo: " + collider.gameObject.name);
                    
                    var colliderInfo = GetColliderInfo(collider, meshFiltersGameObject, mesh_parent);
                    if (colliderInfo != null)
                    {
                        if (collider.GetComponent("Contains") != null)
                        {
                            meshData.placeableZoneColliders.myPlaceableZones.Add(colliderInfo);
                        }
                        
                        //dont include trigger colliders to PrimitiveColliders
                        else if(!collider.isTrigger)
                        {
                            meshData.primitiveColliders.myPrimitiveColliders.Add(colliderInfo);
                        }
                        Debug.Log("not null: " + collider.gameObject.name);

                    }
                }
            }
            else
            {
                // Stop searching if SimObjPhysics is found and it's not BathtubBasin, Shelf, or SinkBasin
                return;
            }
        }
        // if child's sibling has meshrenderer comppnent but is not what we care about
        // then we skipp 
        

        // Collect colliders at this level if this level is not a SimObjPhysics
        int i = 0;
        //foreach (var collider in child.GetComponents<Collider>())
        foreach (var collider in child.GetComponentsInChildren<Collider>())
        {
            if (!collider.enabled || !collider.gameObject.activeInHierarchy)
                continue;
            
            Debug.Log("ColliderInfo: " + collider.gameObject.name);

            var colliderInfo = GetColliderInfo(collider, meshFiltersGameObject, mesh_parent);
            if (colliderInfo != null)
            {
                if (!collider.isTrigger)
                {
                    meshData.primitiveColliders.myPrimitiveColliders.Add(colliderInfo);
                    i++;
                }
                else if (collider.GetComponent("Contains") != null)
                {
                    meshData.placeableZoneColliders.myPlaceableZones.Add(colliderInfo);
                }
                
                Debug.Log("not null: " + collider.gameObject.name);
            }
        }
        //Debug.Log("Collecting colliders at this level: " + child.parent.name + " " + mesh_parent.GetComponent<MeshFilter>().sharedMesh.name + " " + i);

        // Recursively check all children - NOTE this is wrong for doorway bc it's not stopping at the meshfilter
        //foreach (Transform childOfchild in child)
        //{
        //    AddCollidersRecursive(childOfchild, ref meshData, meshFiltersGameObject, mesh_parent);
        //}
    }

    // REDUNDANT CRAP //
    // Find all colliders associated with this mesh, and convert it to the correct coordinates
    private ColliderInfo GetColliderInfo(Collider collider, GameObject meshObject, GameObject reference)
    {
        // Skip MeshColliders entirely
        if (collider is MeshCollider)
            return null;
        
        ColliderInfo colliderInfo = new ColliderInfo();
        
        // Set basic properties - use simple type names without "Collider" suffix
        if (collider is BoxCollider)
            colliderInfo.type = "box";
        else if (collider is SphereCollider)
            colliderInfo.type = "sphere";
        else if (collider is CapsuleCollider)
            colliderInfo.type = "capsule";
        else
            colliderInfo.type = collider.GetType().Name.ToLower().Replace("collider", "");
        
        // Debug logging to see what scales we're working with
        Debug.Log($"Collider {collider.name}: localScale = {collider.transform.localScale}, lossyScale = {collider.transform.lossyScale}");
        Debug.Log($"Reference {reference.name}: localScale = {reference.transform.localScale}, lossyScale = {reference.transform.lossyScale}");
        
        if (collider is BoxCollider boxCollider)
        {
            // Calculate the box's center in world space, accounting for scale
            Vector3 localCenter = boxCollider.center;
            Vector3 worldCenter = collider.transform.TransformPoint(boxCollider.center);
            
            // Get reference world position and calculate offset in world units
            Vector3 referenceWorldPos = reference.transform.position;
            Vector3 worldOffset = worldCenter - referenceWorldPos;
            
            // Transform the world offset direction to reference's local space (rotation only, no scale)
            Vector3 referenceSpaceCenter = Quaternion.Inverse(reference.transform.rotation) * worldOffset;
            colliderInfo.position = referenceSpaceCenter;
            
            // Calculate rotation relative to reference
            Quaternion relativeRotation = Quaternion.Inverse(reference.transform.rotation) * collider.transform.rotation;
            colliderInfo.rotation = relativeRotation;
            
            // Apply scale to the box size - use lossyScale for world-space scaling
            Vector3 originalSize = boxCollider.size;
            Vector3 scaledSize = Vector3.Scale(boxCollider.size, collider.transform.lossyScale);
            // Convert to half-extents for Mujoco
            colliderInfo.size = scaledSize * 0.5f;
            
            Debug.Log($"Box collider: {collider.name}");
            Debug.Log($"  Original center: {localCenter}, World center: {worldCenter}, Reference space center: {referenceSpaceCenter}");
            Debug.Log($"  Original size: {originalSize}, Scaled size: {scaledSize}, Half size: {colliderInfo.size}");
            Debug.Log($"  Scale applied: {collider.transform.lossyScale}");
        }
        else if (collider is SphereCollider sphereCollider)
        {
            // Calculate the sphere's center in world space
            Vector3 localCenter = sphereCollider.center;
            Vector3 worldCenter = collider.transform.TransformPoint(sphereCollider.center);
            
            // Get reference world position and calculate offset in world units
            Vector3 referenceWorldPos = reference.transform.position;
            Vector3 worldOffset = worldCenter - referenceWorldPos;
            
            // Transform the world offset direction to reference's local space (rotation only, no scale)
            Vector3 referenceSpaceCenter = Quaternion.Inverse(reference.transform.rotation) * worldOffset;
            colliderInfo.position = referenceSpaceCenter;
            
            // Calculate rotation relative to reference
            Quaternion relativeRotation = Quaternion.Inverse(reference.transform.rotation) * collider.transform.rotation;
            colliderInfo.rotation = relativeRotation;
            
            // Apply scale to radius - use the maximum scale component for spheres
            float maxScale = Mathf.Max(collider.transform.lossyScale.x, collider.transform.lossyScale.y, collider.transform.lossyScale.z);
            float originalRadius = sphereCollider.radius;
            colliderInfo.radius = sphereCollider.radius * maxScale;
            
            Debug.Log($"Sphere collider: {collider.name}");
            Debug.Log($"  Original center: {localCenter}, World center: {worldCenter}, Reference space center: {referenceSpaceCenter}");
            Debug.Log($"  Original radius: {originalRadius}, Scaled radius: {colliderInfo.radius}");
            Debug.Log($"  Scale applied: {maxScale} (from {collider.transform.lossyScale})");
        }
        else if (collider is CapsuleCollider capsuleCollider)
        {
            // Calculate the capsule's center in world space
            Vector3 localCenter = capsuleCollider.center;
            Vector3 worldCenter = collider.transform.TransformPoint(capsuleCollider.center);
            
            // Get reference world position and calculate offset in world units
            Vector3 referenceWorldPos = reference.transform.position;
            Vector3 worldOffset = worldCenter - referenceWorldPos;
            
            // Transform the world offset direction to reference's local space (rotation only, no scale)
            Vector3 referenceSpaceCenter = Quaternion.Inverse(reference.transform.rotation) * worldOffset;
            colliderInfo.position = referenceSpaceCenter;
            
            // Calculate rotation relative to reference
            Quaternion relativeRotation = Quaternion.Inverse(reference.transform.rotation) * collider.transform.rotation;
            colliderInfo.rotation = relativeRotation;
            
            // Apply scale to radius and height based on capsule direction
            Vector3 scale = collider.transform.lossyScale;
            float originalRadius = capsuleCollider.radius;
            float originalHeight = capsuleCollider.height;
            
            // Capsule direction: 0 = X-axis, 1 = Y-axis, 2 = Z-axis
            if (capsuleCollider.direction == 0) // X-axis
            {
                colliderInfo.radius = capsuleCollider.radius * Mathf.Max(scale.y, scale.z);
                colliderInfo.height = capsuleCollider.height * scale.x;
            }
            else if (capsuleCollider.direction == 1) // Y-axis (default)
            {
                colliderInfo.radius = capsuleCollider.radius * Mathf.Max(scale.x, scale.z);
                colliderInfo.height = capsuleCollider.height * scale.y;
            }
            else // Z-axis
            {
                colliderInfo.radius = capsuleCollider.radius * Mathf.Max(scale.x, scale.y);
                colliderInfo.height = capsuleCollider.height * scale.z;
            }
            
            colliderInfo.direction = capsuleCollider.direction;
            
            Debug.Log($"Capsule collider: {collider.name}");
            Debug.Log($"  Original center: {localCenter}, World center: {worldCenter}, Reference space center: {referenceSpaceCenter}");
            Debug.Log($"  Original radius: {originalRadius}, height: {originalHeight}");
            Debug.Log($"  Scaled radius: {colliderInfo.radius}, height: {colliderInfo.height}");
            Debug.Log($"  Scale applied: {scale}, Direction: {capsuleCollider.direction}");
        }
        
        return colliderInfo;
    }

    private Vector3 GetCombinedScale(Transform target, Transform reference)
    {
        Vector3 scale = Vector3.one;
        Transform current = target;

        // Accumulate scale from target up to (but not including) reference
        while (current != null && current != reference)
        {
            scale = Vector3.Scale(scale, current.localScale);
            current = current.parent;
        }

        return scale;
    }

    // private Transform GetNearestMeshOrRoot(Transform target)
    // {
    //     Transform current = target;
    //     while (current.parent != null)
    //     {
    //         if (current.parent.GetComponent<MeshFilter>() != null)
    //             return current.parent;

    //         current = current.parent;
    //     }
    //     return target.root;
    // }

    public void SaveMeshes(string relativeExportPath, MeshFilter[] meshFilters, Vector3 center, bool applyBoundingBox = true, bool saveSubMeshes = false, bool saveSubMeshTransform = false, bool saveCombinedSubmeshes = false, SimObjPhysics topmostSimObjPhysics = null)
    {
        Debug.Log("saving mesh");

        ExportedAssetInfo exportedAssetInfo = new ExportedAssetInfo();
        exportedAssetInfo.bbox_center.position = center.ToString("0.00000");

        string baseFileName = Path.GetFileNameWithoutExtension(relativeExportPath)
            .Replace(" ", "")
            .Replace("(Instance)", "")
            .Replace("Instance", "")
            .Replace(".", "_");

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("mtllib " + baseFileName + ".mtl");
        int lastIndex = 0;

        // START GOING THROUGH ALL MESH FILTERS HERE
        for(int i = 0; i < meshFilters.Length; i++)
        {
            MeshFilter mf = meshFilters[i];
            string meshName = mf.gameObject.name.Replace(" ", "_");

            Debug.Log("FillMeshData called for mesh: " + meshName);            
            if (mf.gameObject.tag == "Structure") // temporary set  to false for THOR assets...
            {
                MeshData meshData = FillStructureMeshData(meshFilters[i], meshName, topmostSimObjPhysics);  
                exportedAssetInfo.meshes.Add(meshData);
            }
            else
            {
                MeshData meshData = FillMeshData(meshFilters[i], meshName, topmostSimObjPhysics);
                exportedAssetInfo.meshes.Add(meshData);
            }

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
                if (applyScale) // TODO: applyScale,if true, must apply it too all children object
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

                if (applyBoundingBox) //true)// move to bouning box center
                    v -= center;                

                v.x *= -1;
                sb.AppendLine("v " + Math.Ceiling(v.x * 100000) / 100000.0f + " " + 
                             Math.Ceiling(v.y * 100000) / 100000.0f + " " + 
                             Math.Ceiling(v.z * 100000) / 100000.0f);
            }

            foreach (Vector3 vx in msh.normals)
            {
                Vector3 v = vx;
                
                if (applyScale) //applyScale)
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

                if (applyBoundingBox) //true)// move to bouning box center
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

        if (true) //saveSubMeshTransform)  
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

        // handle a material with different shaders
        if (m.shader.name.StartsWith("Custom"))
        {
            return "";
        }

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
                sb.AppendLine("map_Kd " + PathExtensions.GetRelativePath(savePath, _MainTex));// relative to savePath 
            }
            
            Debug.Log("Checking SecondaryTexture");
            string _SecondaryTex = TryExportTexture("_DetailAlbedoMap", m);
            Vector2 _secondaryTextureScale = m.GetTextureScale("_DetailAlbedoMap");
            if (_SecondaryTex != "false")
            {
                sb.AppendLine("map_Kd " + PathExtensions.GetRelativePath(savePath, _SecondaryTex));
            }
            
            //spec map
            string _MetallicGlossMap = TryExportTexture("_MetallicGlossMap", m);
            if (_MetallicGlossMap != "false")
            {
                sb.AppendLine("map_Ks " + PathExtensions.GetRelativePath(savePath, _MetallicGlossMap));
            }
            //bump map
            string _BumpMap = TryExportTexture("_BumpMap", m);
            if (_BumpMap != "false")
            {
                sb.AppendLine("map_Bump " + PathExtensions.GetRelativePath(savePath, _BumpMap));
            }

            
            Debug.Log(m.shader.name);
            if (!Mat2Texture.ContainsKey(m.name) & m.shader.name != "Custom/EmissiveDeferredDecal")
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

        if(File.Exists(assetPath))
        {
            string textureName = Path.GetFileName(assetPath); // with extension
            // Change texture save location to be one level up
            string copyPath = Path.Combine(Path.Combine("Assets/iTHOR", "Textures"), textureName);

            File.Copy(assetPath, copyPath, true);
            return copyPath;
        }
        else
            return "false";
    }

    //////////////// Alvaro Collider serialization Reference Code Below /////////////////////

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

    }

    // Find the nearest parent MeshFilter for a given MeshFilter
    private MeshFilter FindNearestParentMeshFilter(MeshFilter meshFilter, MeshFilter[] allMeshFilters, Transform rootTransform)
    {
        // Skip if this is already at the root level
        if (meshFilter.transform.parent == rootTransform || meshFilter.transform.parent == null)
        {
            return null; // No parent
        }
        
        // Start from the immediate parent and work our way up
        Transform currentTransform = meshFilter.transform.parent;
        
        while (currentTransform != null && currentTransform != rootTransform)
        {
            // Check if this transform has a MeshFilter
            MeshFilter parentFilter = currentTransform.GetComponent<MeshFilter>();
            if (parentFilter != null && parentFilter != meshFilter && IsSuitableParentMesh(parentFilter))
            {
                // Found a parent with a MeshFilter
                return parentFilter;
            }
            
            // Check if any of the siblings at this level have a MeshFilter
            foreach (Transform sibling in currentTransform.parent)
            {
                if (sibling != currentTransform)
                {
                    MeshFilter siblingFilter = sibling.GetComponent<MeshFilter>();
                    if (siblingFilter != null && siblingFilter != meshFilter && IsSuitableParentMesh(siblingFilter))
                    {
                        // Found a sibling with a MeshFilter
                        return siblingFilter;
                    }
                }
            }
            
            // Move up to the next parent
            currentTransform = currentTransform.parent;
        }
        
        // No parent MeshFilter found
        return null;
    }

    // Check if a mesh is suitable to be a parent
    private bool IsSuitableParentMesh(MeshFilter meshFilter)
    {
        // Check mesh name for exclusion patterns
        if (meshFilter.name.Contains("PS_") || 
            meshFilter.name.Contains("Particle") || 
            meshFilter.name.Contains("Placeable"))
        {
            return false;
        }
        
        // Check if mesh has a shared mesh
        if (meshFilter.sharedMesh == null || 
            meshFilter.sharedMesh.name.StartsWith("PS"))
        {
            return false;
        }
        
        // Check if mesh has a renderer with placeable surface material
        MeshRenderer renderer = meshFilter.GetComponent<MeshRenderer>();
        if (renderer != null && renderer.sharedMaterials != null)
        {
            foreach (Material mat in renderer.sharedMaterials)
            {
                if (mat != null && (
                    mat.name == "Placeable_Surface_Mat" || 
                    mat.name == "Water_Volume_Surface_Mat" || 
                    mat.name.Contains("Placeable")))
                {
                    return false;
                }
            }
        }
        
        return true;
    }

    // Use this function to build a hierarchy of MeshFilters
    private Dictionary<MeshFilter, MeshFilter> BuildMeshHierarchy(MeshFilter[] meshFilters, Transform rootTransform)
    {
        Dictionary<MeshFilter, MeshFilter> meshParents = new Dictionary<MeshFilter, MeshFilter>();
        
        // First pass: find direct parent relationships
        foreach (MeshFilter mf in meshFilters)
        {
            // Skip meshes that aren't suitable as children
            if (!IsSuitableParentMesh(mf))
            {
                continue;
            }
            
            MeshFilter parentMf = FindNearestParentMeshFilter(mf, meshFilters, rootTransform);
            if (parentMf != null)
            {
                meshParents[mf] = parentMf;
            }
        }
        
        return meshParents;
    }
}
