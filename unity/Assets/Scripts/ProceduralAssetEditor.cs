using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;
#if UNITY_EDITOR
using EasyButtons.Editor;
using UnityEditor.SceneManagement;
#endif
using EasyButtons;
using System;
using Thor.Procedural;
using Thor.Procedural.Data;
using System.Linq;
using System.IO;
using Thor.Utils;

namespace Thor.Procedural {

    [System.Serializable]
    public class AssetEditorPaths {
        public string serializeBasePath;
        public string materialsRelativePath;
        public string texturesRelativePath;
        public string prefabsRelativePath;
        public string repoRootObjaverseDir;
    }

    public class ProceduralAssetEditor : MonoBehaviour {
        
        public AssetEditorPaths paths = new AssetEditorPaths() {
            serializeBasePath = "Assets/Resources/ai2thor-objaverse/NoveltyTHOR_Assets",
            materialsRelativePath = "Materials/objaverse",
            texturesRelativePath = "Textures",
            prefabsRelativePath = "Prefabs",
            repoRootObjaverseDir = "objaverse"

        };

        public bool savePrefabsOnLoad = false;

        private bool savePrefab = true;

        public string objectId;

       
        #if UNITY_EDITOR
        private string copyTexture(string original, string destinationDir) {
            var outName = $"{destinationDir}/{Path.GetFileName(original)}";
            if (!File.Exists(outName)) {
                File.Copy(original, outName);
            }
            else {
                Debug.LogWarning($"Failed to save texture {outName}, as it already extists. To overwite delete them first and load object again.");
            }
            return outName;
        } 

        private string getAssetRelativePath(string absPath) {
            return string.Join("/", absPath.Split('/').SkipWhile(x=> x != "Assets"));
        }

        private Texture2D loadTexture(string textureAbsPath) {
            return (Texture2D)AssetDatabase.LoadAssetAtPath(getAssetRelativePath(textureAbsPath),typeof(Texture2D));//Resources.Load<Texture2D>(newAlbedo);
        } 

        // private Texture2D copyAndLoadTexture(string original, string destinationDir) {
        //     var outName = $"{destinationDir}/{Path.GetFileName(original)}";
        //     copyTexture(original, destinationDir);
        //     var relativeName =  string.Join("/", outName.Split('/').SkipWhile(x=> x != "Assets"));

        //     return (Texture2D)AssetDatabase.LoadAssetAtPath(relativeName,typeof(Texture2D));//Resources.Load<Texture2D>(newAlbedo);
            
        // } 

        private void SaveTextures(GameObject go, bool savePrefab) {
            var assetId = go.GetComponentInChildren<SimObjPhysics>().assetID;

            var matOutPath = $"{paths.serializeBasePath}/{paths.materialsRelativePath}/{assetId}.mat";

            if (File.Exists(matOutPath)) {
                Debug.LogWarning($"Failed to save material {matOutPath}, as it already extists. To overwite delete it first and load object again.");

                var savedMat = (Material)AssetDatabase.LoadAssetAtPath(getAssetRelativePath(matOutPath),typeof(Material));
                go.GetComponentInChildren<MeshRenderer>().sharedMaterial = savedMat;
            }
            else {
                UnityEditor.AssetDatabase.CreateAsset(
                    go.GetComponentInChildren<MeshRenderer>().sharedMaterial, matOutPath
                );
            }

            
            var runtimeP = go.GetComponent<RuntimePrefab>();
            var dir = string.Join("/", paths.serializeBasePath.Split('/').Skip(1));

            var outTextureBasePath = $"{Application.dataPath}/{dir}/{paths.texturesRelativePath}/{assetId}";

            if (!Directory.Exists(outTextureBasePath)) {
                Directory.CreateDirectory(outTextureBasePath);
            }

            Debug.Log($"---- SaveTextures {outTextureBasePath}");

            var newAlbedo = copyTexture(runtimeP.albedoTexturePath, outTextureBasePath);
            var newNormal = copyTexture(runtimeP.normalTexturePath, outTextureBasePath);
            var newEmission = copyTexture(runtimeP.emissionTexturePath, outTextureBasePath);

            var sharedMaterial = go.GetComponentInChildren<MeshRenderer>().sharedMaterial;

            // AssetDatabase.CreateAsset(this.relatedMaterial, MATERIALS_PATH + "_material.mat");

            // newAlbedo = newAlbedo.Substring(0,newAlbedo.LastIndexOf('.'));
            AssetDatabase.Refresh();
            
            
            sharedMaterial.SetTexture("_MainTex", loadTexture(newAlbedo));//Resources.Load<Texture2D>(newAlbedo));
            sharedMaterial.SetTexture("_BumpMap",  loadTexture(newNormal));
            sharedMaterial.SetTexture("_EmissionMap",  loadTexture(newEmission));

            sharedMaterial.SetColor("_EmissionColor", Color.white);

            DestroyImmediate(runtimeP);
            
            if (savePrefab) {
                PrefabUtility.SaveAsPrefabAssetAndConnect(go, $"{paths.serializeBasePath}/{paths.prefabsRelativePath}/{assetId}.prefab", InteractionMode.UserAction);
            }
        }

        // NOT WORKIGN drag prefab manually
        private void SavePrefab(GameObject go) {

            // var dir = string.Join("/", SerializeMesh.serializeBasePath.Split('/').Skip(1));
            var path = $"{paths.serializeBasePath}/{paths.prefabsRelativePath}/{go.name}.prefab";
            Debug.Log($"---- Savingprefabs {path}");
            //  PrefabUtility.SaveAsPrefabAsset(go, path);
            bool prefabSuccess;
                PrefabUtility.SaveAsPrefabAssetAndConnect(go, path, InteractionMode.UserAction, out prefabSuccess);
                Debug.Log($"---- prefab save {prefabSuccess}");
        }   
        // TODO: put in ifdef  block
        [Button(Expanded = true)]
        public void LoadObject() {
            var file = objectId.Trim();
            if (!file.EndsWith(".json")) {
                file += ".json";
            }
            
            var pathSplit = Application.dataPath.Split('/');

            var repoRoot = pathSplit.Reverse().Skip(2).Reverse().ToList();
            Debug.Log(string.Join("/", repoRoot));

            var objectPath = $"{string.Join("/", repoRoot)}/{paths.repoRootObjaverseDir}/{objectId}/{file}";
            //var objectPath = $"{string.Join("/", repoRoot)}/{this.objectsDirectory}/{objectId}/{file}";
            Debug.Log(objectPath);
            // this.loadedHouse = readHouseFromJson(objectPath);

            var jsonStr = System.IO.File.ReadAllText(objectPath);

            JObject obj = JObject.Parse(jsonStr);

            var procAsset = obj.ToObject<ProceduralAsset>();

            // var prefabParentTransform = transform.Find("ProceduralAssets");
            
            // if (prefabParentTransform == null) {
            //     Debug.Log($"ProceduralAssets container does not exist {prefabParentTransform == null}");
            //     var prefabParent = new GameObject("ProceduralAssets");
            //     prefabParent.transform.parent = transform;
            //     prefabParentTransform = prefabParent.transform;
            // }

            var result = ProceduralTools.CreateAsset(
                    procAsset.vertices,
                    procAsset.normals,
                    procAsset.name,
                    procAsset.triangles,
                    procAsset.uvs,
                    procAsset.albedoTexturePath ,
                    procAsset.normalTexturePath ,
                    procAsset.emissionTexturePath,
                    procAsset.colliders ,
                    procAsset.physicalProperties,
                    procAsset.visibilityPoints ,
                    procAsset.annotations ,
                    procAsset.receptacleCandidate ,
                    procAsset.yRotOffset ,
                    serializable: true,
                    returnObject: true,
                    parent: transform
                );
            var go = result["gameObject"] as GameObject;

            if (go.transform.parent != null && !go.transform.parent.gameObject.activeSelf) {
                go.transform.parent.gameObject.SetActive(true);
            }
            

            if (savePrefabsOnLoad) {
                SaveTextures(go, savePrefab);
            }
            else {
                var runtimeP = go.GetComponent<RuntimePrefab>();
                if (runtimeP != null) {
                    runtimeP.RealoadTextures();
                }
               
            }

            // if (savePrefab) {
            //     if (!saveTextures) {
            //         SaveTextures(go); 
            //     }
            //      SavePrefab(go);
            
            // }
        }

        [Button(Expanded = true)]
        public void SaveObjectPrefabAndTextures() { 
            var transformRoot = GameObject.Find(objectId);
            var transform = gameObject.transform.root.FirstChildOrDefault(g => g.name == objectId);

            if (transform == null) {
                transform = transformRoot.transform;
            }
            // Debug.Log($"Root: {gameObject.transform.root.name}");
            if (transform != null) {
                SaveTextures(transform.gameObject, savePrefab);
            }
            else {
                Debug.LogError($"Invalid object {objectId} not present in scene.");
            }
        }

        [Button(Expanded = true)]
        public void SaveAllPrefabsAndTextures() { 
            var procAssets = GameObject.FindObjectsOfType(typeof(RuntimePrefab))  as RuntimePrefab[];
            // var procAssets = gameObject.transform.root.GetComponentsInChildren<RuntimePrefab>();
            foreach (var asset in procAssets) {
                if (asset != null) {
                    SaveTextures(asset.gameObject, savePrefab);
                }
                else {
                    Debug.LogError($"Invalid object in scene.");
                }
            }
        }

         [UnityEditor.MenuItem("Procedural/Reload Procedural Asset Textures")]
        public static void ReloadTextures() {
             var procAssets = GameObject.FindObjectsOfType(typeof(RuntimePrefab))  as RuntimePrefab[];
             foreach (var asset in procAssets) { 
                asset.RealoadTextures();
             }
        }
        #endif

    }
}