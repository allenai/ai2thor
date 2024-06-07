using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

public class ResourceAssetReference<T>
    where T : UnityEngine.Object {
    public readonly string Name;
    public readonly string ResourcePath;
    private T _asset;

    public ResourceAssetReference(string resourcePath, string name) {
        this.Name = name;
        this.ResourcePath = resourcePath;
    }

    public T Load() {
        if (this._asset == null) {
            this._asset = Resources.Load<T>(ResourcePath);
        }
        return this._asset;
    }
}

public class ResourceAssetManager {
    private class ResourceAsset {
        // must have empty constructor for JSON deserialization
        public ResourceAsset() { }

#if UNITY_EDITOR
        public ResourceAsset(string assetPath) {
            var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            this.path = assetPath;
            this.labels = AssetDatabase.GetLabels(asset);
            this.assetType = asset.GetType().FullName;
            this.name = asset.name;
        }
#endif

        public string[] labels;
        public string path;
        public string assetType;
        public string name;
    }

    private class ResourceAssetCatalog {
        public ResourceAssetCatalog() {
            this.assets = new List<ResourceAsset>();
            this.timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
        }

        public long timestamp;
        public List<ResourceAsset> assets;
    }

    private static readonly string ResourcesPath = Application.dataPath + "/Resources/";
    private static readonly string CatalogPath = ResourcesPath + "ResourceAssetCatalog.json";
    private static readonly string ProjectFolder = Path.GetDirectoryName(Application.dataPath);
    private ResourceAssetCatalog catalog;
    private Dictionary<string, List<ResourceAsset>> labelIndex =
        new Dictionary<string, List<ResourceAsset>>();

    public ResourceAssetManager() {
#if UNITY_EDITOR
        this.RefreshCatalog();
#else
        this.catalog = readCatalog();
        if (this.catalog == null)
        {
            this.catalog = new ResourceAssetCatalog();
        }
        this.generateLabelIndex();
#endif
    }

#if UNITY_EDITOR

    private string[] findResourceGuids() {
        return AssetDatabase.FindAssets("t:material", new[] { "Assets/Resources" });
    }

    public void RefreshCatalog() {
        this.catalog = new ResourceAssetCatalog();
        foreach (string guid in this.findResourceGuids()) {
            // GUIDToAssetPath returns a relative path e.g "Assets/Resources/QuickMaterials/Blue.mat"
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            // ignore FBX files with multiple sub-materials
            if (
                assetPath.ToLower().EndsWith(".fbx")
                && AssetDatabase.LoadAllAssetsAtPath(assetPath).Length > 1
            ) {
                continue;
            }

            var labeledAsset = new ResourceAsset(assetPath);
            this.catalog.assets.Add(labeledAsset);
        }
        this.generateLabelIndex();
    }

    public void BuildCatalog() {
        this.RefreshCatalog();
        this.writeCatalog();
    }
#endif

    private void generateLabelIndex() {
        foreach (var labeledAsset in this.catalog.assets) {
            foreach (var label in labeledAsset.labels) {
                if (!this.labelIndex.ContainsKey(label)) {
                    this.labelIndex[label] = new List<ResourceAsset>();
                }
                this.labelIndex[label].Add(labeledAsset);
            }
        }
    }

    private ResourceAssetCatalog readCatalog() {
        string catalogResourcePath = relativePath(ResourcesPath, CatalogPath);
        if (Path.HasExtension(catalogResourcePath)) {
            string ext = Path.GetExtension(catalogResourcePath);
            catalogResourcePath = catalogResourcePath.Remove(
                catalogResourcePath.Length - ext.Length,
                ext.Length
            );
        }
        var jsonResource = Resources.Load<TextAsset>(catalogResourcePath);

        if (jsonResource == null) {
            return null;
        }

        return JsonConvert.DeserializeObject<ResourceAssetCatalog>(jsonResource.text);
    }

    // just handles case where relativeTo is the prefix of fullPath
    private string relativePath(string relativeTo, string fullPath) {
        return fullPath.Remove(0, relativeTo.Length);
    }

    private void writeCatalog() {
        string json = serialize(this.catalog);

        // .tmp files are ignored by Unity under Assets/
        string tmpCatalogPath = CatalogPath + ".tmp";
        if (File.Exists(tmpCatalogPath)) {
            File.Delete(tmpCatalogPath);
        }

        File.WriteAllText(tmpCatalogPath, json);
        if (File.Exists(CatalogPath)) {
            File.Delete(CatalogPath);
        }
        // Doing a delete + move to make the write atomic and avoid writing a possibly invalid JSON
        // document
        File.Move(tmpCatalogPath, CatalogPath);
    }

    public List<ResourceAssetReference<T>> FindResourceAssetReferences<T>(string label)
        where T : UnityEngine.Object {
        string typeName = typeof(T).FullName;
        List<ResourceAssetReference<T>> assetRefs = new List<ResourceAssetReference<T>>();
        if (this.labelIndex.ContainsKey(label)) {
            foreach (var res in this.labelIndex[label]) {
                if (res.assetType == typeName) {
                    string resourcePath = relativePath("Assets/Resources/", res.path);
                    resourcePath = Path.Combine(
                        Path.GetDirectoryName(resourcePath),
                        Path.GetFileNameWithoutExtension(resourcePath)
                    );
                    ResourceAssetReference<T> assetRef = new ResourceAssetReference<T>(
                        resourcePath: resourcePath,
                        name: res.name
                    );
                    assetRefs.Add(assetRef);
                }
            }

            return assetRefs;
        } else {
            return assetRefs;
        }
    }

    private string serialize(ResourceAssetCatalog catalog) {
        var jsonResolver = new ShouldSerializeContractResolver();
        return Newtonsoft.Json.JsonConvert.SerializeObject(
            catalog,
            Newtonsoft.Json.Formatting.None,
            new Newtonsoft.Json.JsonSerializerSettings() {
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                ContractResolver = jsonResolver
            }
        );
    }
}
