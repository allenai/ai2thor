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
    public class ProceduralAssetEditor : MonoBehaviour {

        public string objectsDirectory = "objaverse";

        public bool copyTexturesOnLoad = false;

        // public bool savePrefab = true;

        public string objectId;

        private const string prefabsRelativePath = "Prefabs";
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

        private void SaveTextures(GameObject go) {
            Debug.Log("---- SaveTextures");
            var runtimeP = go.GetComponent<RuntimePrefab>();
            var dir = string.Join("/", SerializeMesh.serializeBasePath.Split('/').Skip(1));

            var outTextureBasePath = $"{Application.dataPath}/{dir}/{SerializeMesh.texturesRelativePath}/{go.name}";

            if (!Directory.Exists(outTextureBasePath)) {
                Directory.CreateDirectory(outTextureBasePath);
            }

            Debug.Log($"---- SaveTextures {outTextureBasePath}");

            var newAlbedo = copyTexture(runtimeP.albedoTexturePath, outTextureBasePath);
            var newNormal = copyTexture(runtimeP.normalTexturePath, outTextureBasePath);
            var newEmission = copyTexture(runtimeP.emissionTexturePath, outTextureBasePath);
            runtimeP.albedoTexturePath = newAlbedo;
            runtimeP.normalTexturePath = newNormal;
            runtimeP.emissionTexturePath = newEmission;
        }

        // NOT WORKIGN drag prefab manually
        private void SavePrefab(GameObject go) {

            // var dir = string.Join("/", SerializeMesh.serializeBasePath.Split('/').Skip(1));
            var path = $"{SerializeMesh.serializeBasePath}/{prefabsRelativePath}/{go.name}.prefab";
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

            var objectPath = $"{string.Join("/", repoRoot)}/objaverse/{objectId}/{file}";
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
                    parent: null
                );
            var go = result["gameObject"] as GameObject;

            if (go.transform.parent != null && !go.transform.parent.gameObject.activeSelf) {
                go.transform.parent.gameObject.SetActive(true);
            }
            

            if (copyTexturesOnLoad) {
                SaveTextures(go);
            }

            // if (savePrefab) {
            //     if (!saveTextures) {
            //         SaveTextures(go); 
            //     }
            //      SavePrefab(go);
            
            // }
        }

        [Button(Expanded = true)]
        public void CopyObjectTextures() { 
            var transformRoot = GameObject.Find(objectId);
            var transform = gameObject.transform.root.FirstChildOrDefault(g => g.name == objectId);

            if (transform == null) {
                transform = transformRoot.transform;
            }
            // Debug.Log($"Root: {gameObject.transform.root.name}");
            if (transform != null) {
                SaveTextures(transform.gameObject);
            }
            else {
                Debug.LogError($"Invalid object {objectId} not present in scene.");
            }
        }

         [Button(Expanded = true)]
        public void CopyAllTextures() { 
            var procAssets = GameObject.FindObjectsOfType(typeof(RuntimePrefab))  as RuntimePrefab[];
            // var procAssets = gameObject.transform.root.GetComponentsInChildren<RuntimePrefab>();
            foreach (var asset in procAssets) {
                if (asset != null) {
                    SaveTextures(asset.gameObject);
                }
                else {
                    Debug.LogError($"Invalid object in scene.");
                }
            }
        }
        #endif
    }
}