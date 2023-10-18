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

        public bool copyTexturesWhenLoading = false;

        // public bool savePrefab = true;


        private const string prefabsRelativePath = "Prefabs";
        // #if UNITY_EDITOR
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
        public void LoadObject(string objectId) {
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
                    returnObject: true
                );
            var go = result["gameObject"] as GameObject;

            go.transform.parent.gameObject.SetActive(true);

            if (copyTexturesWhenLoading) {
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
        public void SaveObjectTextures(string objectId) { 
            var transform = gameObject.transform.root.FirstChildOrDefault(g => g.name == objectId);
            if (transform != null) {
                SaveTextures(transform.gameObject);
            }
            else {
                Debug.LogError($"Invalid object {objectId} not present in scene.");
            }
        }
        // #endif
    }
}