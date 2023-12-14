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

using diagnostics = System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Data.Common;

namespace Thor.Procedural {

    [System.Serializable]
    public class AssetEditorPaths {
        public string serializeBasePath;
        public string materialsRelativePath;
        public string texturesRelativePath;
        public string prefabsRelativePath;
        public string modelsRelativePath;
         public string collidersInModelsPath;
        public string repoRootObjaverseDir;
    }

    [System.Serializable]
    public class ObjaversePipelinseSettings {
        public string pythonExecutablePath;
        public string vidaRepo; 
        public int timeoutSeconds;
        
    }

    public class ProceduralAssetEditor : MonoBehaviour {
        
        public AssetEditorPaths paths = new AssetEditorPaths() {
            serializeBasePath = "Assets/Resources/ai2thor-objaverse/NoveltyTHOR_Assets",
            materialsRelativePath = "Materials/objaverse",
            texturesRelativePath = "Textures",
            prefabsRelativePath = "Prefabs",
            repoRootObjaverseDir = "objaverse",
            modelsRelativePath = "Models/objaverse",
            collidersInModelsPath =  "Colliders",

        };

        public ObjaversePipelinseSettings objaversePipelineConfig = new ObjaversePipelinseSettings{
            pythonExecutablePath = "/Users/alvaroh/anaconda3/envs/vida/bin/python",
            vidaRepo = "/Users/alvaroh/ai2/vida",
            timeoutSeconds = 800
        };

        public bool savePrefabsOnLoad = false;
        public bool forceCreateAnnotationComponent = false;

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
            
            var mr = go.GetComponentInChildren<MeshRenderer>();
            var sharedMaterial = mr.sharedMaterial;

            // AssetDatabase.CreateAsset(this.relatedMaterial, MATERIALS_PATH + "_material.mat");

            // newAlbedo = newAlbedo.Substring(0,newAlbedo.LastIndexOf('.'));
            AssetDatabase.Refresh();

            var normalImporter = AssetImporter.GetAtPath(getAssetRelativePath(newNormal)) as TextureImporter;

            normalImporter.textureType = TextureImporterType.NormalMap;
            
            sharedMaterial.SetTexture("_MainTex", loadTexture(newAlbedo));//Resources.Load<Texture2D>(newAlbedo));
            sharedMaterial.SetTexture("_BumpMap",  loadTexture(newNormal));
            sharedMaterial.SetTexture("_EmissionMap",  loadTexture(newEmission));

            sharedMaterial.SetColor("_EmissionColor", Color.white);

            DestroyImmediate(runtimeP);

            SerializeMesh.SaveMeshesAsObjAndReplaceReferences(
                go,
                assetId, 
                $"{Application.dataPath}/{dir}/{paths.modelsRelativePath}/{assetId}",
                $"{Application.dataPath}/{dir}/{paths.modelsRelativePath}/{assetId}/{paths.collidersInModelsPath}"
            );

            // var mf = go.GetComponentInChildren<MeshFilter>();
            // var mesh =mf.sharedMesh;
            // var obj = SerializeMesh.MeshToObj(assetId, mesh);

            // var outModelsBasePath = $"{Application.dataPath}/{dir}/{paths.modelsRelativePath}/{assetId}";

            // if (!Directory.Exists(outModelsBasePath)) {
            //     Directory.CreateDirectory(outModelsBasePath);
            // }

            // // var f = File.Create($"{outModelsBasePath}/{assetId}.obj");
            // var fileObj = $"{outModelsBasePath}/{assetId}.obj";
            // Debug.Log($"---- Writing to {fileObj}");
            // File.WriteAllText(fileObj, obj);
            // AssetDatabase.Refresh();

            //  var mi = AssetImporter.GetAtPath(getAssetRelativePath(fileObj)) as ModelImporter;

            //  mesh = (Mesh)AssetDatabase.LoadAssetAtPath(getAssetRelativePath(fileObj),typeof(Mesh));
            //  Debug.Log($"---- model imp {mi} null? {mi==null}");
            //  Debug.Log($"---- mesh imp {mesh} null? {mesh==null}");

            // mf.sharedMesh = mesh;

            
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

        private GameObject importAsset(string objectPath, bool addAnotationComponent = false) {
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
                    parent: transform,
                    addAnotationComponent: addAnotationComponent
                );
            var go = result["gameObject"] as GameObject;

            var intermediate = result["intermediateGameObject"] as GameObject;
            DestroyImmediate(intermediate);

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
            return go;
        }

        private List<Coroutine> coroutines = new List<Coroutine>();
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

            var objaverseRoot = $"{string.Join("/", repoRoot)}/{paths.repoRootObjaverseDir}";
            var objectDir = $"{objaverseRoot}/{objectId}";
            var objectPath = $"{objectDir}/{file}";
            //var objectPath = $"{string.Join("/", repoRoot)}/{this.objectsDirectory}/{objectId}/{file}";
            Debug.Log(objectPath);
            // this.loadedHouse = readHouseFromJson(objectPath);

            if (!File.Exists(objectPath)) {
                cancellAll = false;
                Debug.Log("Starting objaverse pipeline background process...");
                coroutines.Add(
                    StartCoroutine(runAssetPipelineAsync(objectId, objaverseRoot, () => {                        
                        if (!cancellAll) { importAsset(objectPath, addAnotationComponent: true); }
                    }))
                );
            }
            else {
                importAsset(objectPath, forceCreateAnnotationComponent);
            }

            

            

            // if (savePrefab) {
            //     if (!saveTextures) {
            //         SaveTextures(go); 
            //     }
            //      SavePrefab(go);
            
            // }
        }

        [Button(Expanded = true)]
        public void CancelAll() { 
            EditorUtility.ClearProgressBar();
            cancellAll = true;
           
            foreach (var ap in this.processingIds.Values) {
                 Debug.Log($"Cancelled conversion process for '{ap.id}'");
                 ap.process.Kill();
                 ap.process.CancelOutputRead();
                 ap.process.CancelErrorRead();
                
            }
            
            foreach (var coroutine in coroutines) {
                StopCoroutine(coroutine);
            }
            
            processingIds.Clear();
            coroutines.Clear();
        }
        private string commaSepIds;

        // [Button(Expanded = false)]
        public void LoadMulti() {

            var ids = commaSepIds.Split(',');
            Debug.Log($"ids comma {string.Join(" | ", ids)} count {ids.Count()}");
            foreach (var id in ids) {
            var file = id.Trim();
            if (!file.EndsWith(".json")) {
                file += ".json";
            }
            
            
            

            var objaverseRoot = getObjaverseRootPath();
            Debug.Log(string.Join("/", objaverseRoot));
            var objectDir = $"{objaverseRoot}/{id}";
            var objectPath = $"{objectDir}/{file}";
            //var objectPath = $"{string.Join("/", repoRoot)}/{this.objectsDirectory}/{id}/{file}";
            Debug.Log(objectPath);
            // this.loadedHouse = readHouseFromJson(objectPath);

            if (!File.Exists(objectPath)) {
                cancellAll = false;
                Debug.Log("Starting objaverse pipeline background process...");
                coroutines.Add(
                    StartCoroutine(runAssetPipelineAsync(id, objaverseRoot, () => {                        
                        if (!cancellAll) { importAsset(objectPath, addAnotationComponent: true); }
                    }))
                );
            }
            else {
                importAsset(objectPath, forceCreateAnnotationComponent);
            }
            }


        }

        private string getObjaverseRootPath() {
            var pathSplit = Application.dataPath.Split('/');

            var repoRoot = pathSplit.Reverse().Skip(2).Reverse().ToList();
            return  $"{string.Join("/", repoRoot)}/{paths.repoRootObjaverseDir}";
        }

        // private IEnumerator loadAndFixAsync(string[] ids) {

        //     foreach (var id in ids) {

        //     }

        // }

        public bool useLoadedMesh = false;

        // [UnityEditor.MenuItem("Procedural/Fix Prefabs")]
         [Button(Expanded = false)]
        public void FixPrefabs() {
            var procAssets = GameObject.FindObjectsOfType(typeof(SimObjPhysics))  as SimObjPhysics[];

            //var gos = procAssets.GroupBy(so => so.assetID).Select(k => (IdentifierCase: k.Key, go: k.First().gameObject)).Where(p => p.go.GetComponentInChildren<SerializeMesh>() != null);
            //var dict = new Dictionary<string, 
            var gos = procAssets.
            Select(k => (id: k.assetID, go: k.gameObject))
            .Where( p => p.id != "4b3ae3b8744d429a8a4aa1b5b5cb4f7c" && p.id != "")
            .Where(p => p.id == "46bb9561bf23477aaad941e143a52803");
             //Selection.activeGameObject=gos.Last().go;
            //"4b3ae3b8744d429a8a4aa1b5b5cb4f7c"
            Debug.Log($"running for {gos.Count()} last {gos.Last().id} eq {gos.Last().id == ""}");

            
            Debug.Log($"running for {string.Join(",", gos.Select(g => g.id).Distinct())}");
            //return;

            var loadedObjs = new Dictionary<string, GameObject>();

            if (useLoadedMesh) {
            loadedObjs = gos.Select(m => m.id).Distinct().ToDictionary(id => id, assetId => {

                 var pathSplit = Application.dataPath.Split('/');

                    var repoRoot = pathSplit.Reverse().Skip(2).Reverse().ToList();
                     var objaverseRoot = $"{string.Join("/", repoRoot)}/{paths.repoRootObjaverseDir}";
                    var objectDir = $"{objaverseRoot}/{assetId}";
                    var objectPath = $"{objectDir}/{assetId}.json";

                    var jsonStr = System.IO.File.ReadAllText(objectPath);

            JObject obj = JObject.Parse(jsonStr);

            var procAsset = obj.ToObject<Procedural.Data.ProceduralAsset>();

            var result = Procedural.ProceduralTools.CreateAsset(
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
                DestroyImmediate(result["intermediateGameObject"] as GameObject);
                return result["gameObject"] as GameObject;
            });
            }


            foreach (var (assetId, go) in gos) {
                var dir =string.Join("/", paths.serializeBasePath.Split('/').Skip(1));
                  if (PrefabUtility.IsPartOfPrefabInstance(go)) {
                    var loaded = useLoadedMesh ? loadedObjs[assetId] : null;
                    SerializeMesh.SaveMeshesAsObjAndReplaceReferences(
                        go,
                        assetId,
                        $"{Application.dataPath}/{dir}/{paths.modelsRelativePath}/{assetId}",
                        $"{Application.dataPath}/{dir}/{paths.modelsRelativePath}/{assetId}/{paths.collidersInModelsPath}",
                        overwrite: true
                        ,sourceGo: loaded
                    );
                    var meshGo = go.transform.Find("mesh");
                    // if (meshGo != null) 
                    // {
                    //     var negRot = -meshGo.transform.localEulerAngles;
                    //     meshGo.transform.localEulerAngles = negRot;
                    // }
                    Debug.Log($"Ran for {go.name}");
                    if (go.GetComponentInChildren<SerializeMesh>() != null) {
                        DestroyImmediate(go.GetComponentInChildren<SerializeMesh>());
                    }
                    
                    PrefabUtility.ApplyPrefabInstance(go, InteractionMode.UserAction);
                    Selection.activeGameObject=go;
                  }
            }
            if (useLoadedMesh) { 
                // foreach (var go in loadedObjs.Values) {
                //     DestroyImmediate(go);
                // }
            }
        } 

         [MenuItem("Procedural/Revert Prefabs")]
          public void RevertPrefabs() {
            var simObjs = GameObject.FindObjectsOfType(typeof(SimObjPhysics))  as SimObjPhysics[];
            
            foreach (var so in simObjs) {
                if (PrefabUtility.IsPartOfPrefabInstance(so.gameObject)) {
                    Debug.Log($"Reverting {so.gameObject}");
                    //PrefabUtility.RevertPropertyOverride()
                    PrefabUtility.RevertPrefabInstance(so.gameObject, InteractionMode.UserAction);
                }
            }
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

         [UnityEditor.MenuItem("Procedural/Reload Procedural Asset Textures _s")]
        public static void ReloadTextures() {
             var procAssets = GameObject.FindObjectsOfType(typeof(RuntimePrefab))  as RuntimePrefab[];
             foreach (var asset in procAssets) { 
                asset.RealoadTextures();
             }
        }

        class AsyncProcess {
            public float progress;
            public diagnostics.Process process;
            public string id;

            public GameObject goResultß;
        }

        private ConcurrentDictionary<string, AsyncProcess> processingIds = new ConcurrentDictionary<string, AsyncProcess>();

        private bool cancellAll = false;

        private IEnumerator waitForProcess(diagnostics.Process p, string id, int sleepSeconds, int debugIntervalSeconds, int timeoutSeconds) {

            var secs = 0;
            var start = Time.realtimeSinceStartup;
            var prevCount = processingIds.Count;
            var updateProgressBar = false;
            Debug.Log("Running objaverse conversion pipeline...");
            while (!p.HasExited) {
                
               
                // var currentCount = processingIds.Count;
                // p.StandardOutput.ReadAsync()
                yield return new WaitForSeconds(sleepSeconds);
               

                // yield return true;
                var currentCount = processingIds.Count;
                
                var deltaSeconds = Time.realtimeSinceStartup - start;
                int roundedSeconds = (int)Mathf.Round(deltaSeconds);
                if (roundedSeconds % debugIntervalSeconds == 0) {
                    Debug.Log($"Ran pipeline for ({roundedSeconds}) for '{id}'.");
                    
                    // Debug.Log($"Output: {p.StandardOutput.ReadToEnd()}");
                    // Debug.Log($"Error Out: {p.StandardError.ReadToEnd()}");
                }

                 
                if (currentCount < prevCount) {
                    //  prevCount = currentCount;
                    EditorUtility.DisplayProgressBar("Objaverse import", $"Still running glb conversion pipeline for {id}...", 0.1f);
                    // prevCount = currentCount;
                }
                // if (updateProgressBar && currentCount < prevCount) {
                //     prevCount = currentCount;
                //     EditorUtility.DisplayProgressBar("Objaverse import", $"'{id}' Still running glb conversion pipeline...", 0.1f);
                // }


                if (deltaSeconds >= timeoutSeconds) {
                    Debug.Log($"Timeout reached, possible issue with object '{id}', or increase components 'objaversePipelineConfig.timeoutSeconds'.");
                    p.Kill();
                    break;
                }
            }
        }

        private diagnostics.Process runPythonCommand(string id, string saveDir) {
            diagnostics.Process p = new diagnostics.Process ();
            var pythonFilename = $"{objaversePipelineConfig.vidaRepo}/data_generation/objaverse/object_consolidater_blender_direct.py";
            Debug.Log($"Running conversion script: `{objaversePipelineConfig.pythonExecutablePath} {pythonFilename} {objectId} {saveDir}`");
            p.StartInfo = new diagnostics.ProcessStartInfo(objaversePipelineConfig.pythonExecutablePath, $"{pythonFilename} {objectId} {saveDir}")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };
            Console.InputEncoding = Encoding.UTF8;
            
            p.OutputDataReceived += (sender, args) => Debug.Log(args.Data);
            p.ErrorDataReceived += (sender, args) => Debug.LogError(args.Data);
            // processes.Enqueue(p);

            processingIds.GetOrAdd(id, new AsyncProcess() {
                id = id,
                process = p,
                progress = 0.0f

            });

            p.Start();
            
            // cant mix async and non async output read
             p.BeginOutputReadLine();
             p.BeginErrorReadLine();

            return p;

        }

        // private float getPipelineAverageProgress() {
        //     return processingIds.Values.Sum() / processingIds.Count;
        // }

        private IEnumerator runAssetPipelineAsync(string id, string saveDir, Action callback = null) {
            
            // processingIds.TryUpdate(id)
            EditorUtility.DisplayProgressBar("Objaverse import", $"'{id}' Running import pipeline...", 0.0f);
            var p = runPythonCommand(id, saveDir);

             EditorUtility.DisplayProgressBar("Objaverse import", $"'{id}' Running glb conversion...", 0.1f);
            yield return waitForProcess(p, id, 1, 5, objaversePipelineConfig.timeoutSeconds);

             EditorUtility.DisplayProgressBar("Objaverse import", $"'{id}' Finished glb conversion.", 0.8f);


            // var outputStr = p.StandardOutput.ReadToEnd();
            // var errorStr = p.StandardError.ReadToEnd();
            // Debug.Log($"Pipeline Output: {outputStr}");

            // Debug.LogError($"Pipeline Error Output: {errorStr}");

            // var split = $"{outputStr}\n{errorStr}".Split('\n');
            // foreach (var line in split) {
            //     Debug.Log (line);
            // }
            
            p.WaitForExit();
            
            Debug.Log($"Exit: {p.ExitCode}");
            Debug.Log("Running callback...");
            EditorUtility.DisplayProgressBar("Objaverse import", $"'{id}' Importing to Unity", 0.9f);
            callback?.Invoke();
            EditorUtility.DisplayProgressBar("Objaverse import", $"'{id}' Finished!", 1f);

            processingIds.TryRemove(id, out AsyncProcess val);
            if (processingIds.IsEmpty) {
                EditorUtility.ClearProgressBar();
            }

        }

        // [Button(Expanded = true)]
        // public void Download() {
        //     var p = runPythonCommand(objectId);

        //     var outputStr = p.StandardOutput.ReadToEnd();
        //     var errorStr = p.StandardError.ReadToEnd();
        //     Debug.Log(outputStr);
        //     Debug.Log(errorStr);
            
            
        //     p.WaitForExit();
            
        //     Debug.Log($"Exit: {p.ExitCode}");
            
        // }

    
#endif

    }
}