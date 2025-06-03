using UnityEngine;
using UnityEngine.Networking;
using System;
using System.IO;
using System.IO.Compression;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MessagePack;
using Thor.Procedural.Data;
using System.Linq;
using Newtonsoft.Json;

using MessagePack;
using MessagePack.Formatters;
using MessagePack.Internal;
using MessagePack.Resolvers;
using MessagePack.Unity;
using UnityEditor.Experimental;

namespace Thor.Procedural {

public interface ProgressReporter {
    void OnProgress(float progress);
}

public class ProceduralAssetDownloader {

    public static float progress;
    static List<string> supportedExtensions() {
         return new List<string>()
        {
            ".msgpack.gz",
            ".msgpack",
            ".json",
            ".gz", 
        };
    }

    private static List<string> _supportedExtensions;

    public static List<string> SupportedExtensions {
        get {
            if (_supportedExtensions != null) {
                return _supportedExtensions;
            }
            else {
                _supportedExtensions = supportedExtensions();
                return _supportedExtensions;
            }
        }
    }


    public static IEnumerator DownloadAndCreateAssets(string baseUrl, List<string> assetIds, string extension = null, bool saveMaterialToAssetDB = true, ProgressReporter progressReporter = null) {
        var actionF = ActionFinished.Success;
        progress = 0;
        MessagePack.Resolvers.MessagePackInit.Register();
        var results = new List<Dictionary<string, object>>();
        var i = 0;
        foreach (string assetId in assetIds) {
            if (!actionF.success) {
                break;
            }
            yield return DownloadAndCreateAsset(
                baseUrl, 
                assetId,
                actionF,
                results,
                extension: extension,
                saveMaterialToAssetDB: saveMaterialToAssetDB
            );
            i++;
            progress = i / (assetIds.Count + 0.0f);
            if (progressReporter != null) {
                progressReporter.OnProgress(progress);
            }
            // Debug.Log($"=== progress {progress}");
        }

        // Debug.Log($" ========= Downloaded assets {string.Join(",\n",results.Select(x => x["assetId"]))}");
        // onComplete?.Invoke(results);
        yield return new ActionFinished(
            success: actionF.success,
            errorMessage: actionF.errorMessage,
            actionReturn: results
        );
    }

    private static IEnumerator DownloadAndCreateAsset(
        string baseUrl, 
        string assetId, 
        ActionFinished runningActionFinished, 
        List<Dictionary<string, object>> results, 
        string extension = null,
        bool saveMaterialToAssetDB = true
    ) {
        //  var m = ("s", )
         string url = $"{baseUrl}/{assetId}.tar";
            UnityWebRequest request = UnityWebRequest.Get(url);
            request.downloadHandler = new DownloadHandlerBuffer();

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success) {
                Debug.LogError($"Download failed for {url}: {request.error}");
                yield return new ActionFinished(
                    success: false,
                    errorMessage: $"Download failed for {url}: {request.error}"
                );
            }

            byte[] tarBytes = request.downloadHandler.data;
            var fileMap = ExtractTarArchive(new MemoryStream(tarBytes));
            string folder = assetId + "/";
            

            // Not working
            // List<string> testExtensions = null;
            // if (extension != null && !SupportedExtensions.Contains(extension))  {
            //     yield return new ActionFinished(
            //         success: false,
            //         errorMessage: $"Unsupported extension {extension} only supported ''{string.Join(",", SupportedExtensions)}"
            //     );
            // }
            // else if (extension == null) {
            //     testExtensions = SupportedExtensions;
            // }
            // else {
            //     testExtensions = new List<string>(){
            //         extension
            //     };
            // }

            // byte[] compressed = new byte[0];
            // bool gotFile = false;
            // foreach (var testExtension in testExtensions) {
            //     string assetPath = folder + assetId + extension;
            //     if (fileMap.TryGetValue(assetPath, out compressed)) {
            //         gotFile = true;
            //         break;
            //     }
            // }

            // if (gotFile) {
            //     var testedPaths = testExtensions.Select(ext => folder + assetId + ext);
            //     var msg = $"Missing asset, tested paths: {string.Join(",", testedPaths)}. "+
            //             $"Either pass the correct `extension`, passed: `{extension}`. " +
            //             $"And make sure the tar file contains under directory {assetId} " +
            //             $"an asset of the supported formats {string.Join(",", SupportedExtensions)}";
            //     Debug.LogWarning(msg);
            //     yield return new ActionFinished(
            //         success: false,
            //         errorMessage: msg
            //     );
            // }

            string assetPath = folder + assetId + ".msgpack.gz";
            var testedPaths = new List<string>(){assetPath};

            if (!fileMap.TryGetValue(assetPath, out byte[] compressed)) {
                Debug.LogWarning($"Missing asset: {assetPath}");
                    var msg = $"Missing asset, tested paths: {string.Join(",", testedPaths)}. "+
                        $"Either pass the correct `extension`, passed: `{extension}`. " +
                        $"And make sure the tar file contains under directory {assetId} " +
                        $"an asset of the supported formats {string.Join(",", SupportedExtensions)}";
                yield return new ActionFinished(
                    success: false,
                    errorMessage: msg
                );
            }

            byte[] decompressed;
            using (var gz = new GZipStream(new MemoryStream(compressed), CompressionMode.Decompress))
            using (var ms = new MemoryStream()) {
                gz.CopyTo(ms);
                decompressed = ms.ToArray();
            }

            // MessagePack.Resolvers.MessagePackInit.Register();

            // MessagePackSerializerOptions options = MessagePackSerializerOptions.Standard
            //     .WithResolver(CompositeResolver.Create(
            //         MessagePack.Resolvers.ThorWebGLSafeStandardResolver.Instance,
            //         BuiltinResolver.Instance,
            //         UnityResolver.Instance,
            //         StandardResolver.Instance
            //     ));
            // Fails in webgl
            // var asset = MessagePack.MessagePackSerializer.Deserialize<ProceduralAsset>(
            //     decompressed,
            //     MessagePack.Resolvers.ThorContractlessStandardResolver.Options
            // );

            var asset = MessagePack.MessagePackSerializer.Deserialize<ProceduralAsset>(
                decompressed,
                MessagePack.Resolvers.ThorWebGLSafeStandardResolver.Options
            );

            // var dict = MessagePack.MessagePackSerializer.Deserialize<Dictionary<string, object>>(
            //     decompressed,
            //     MessagePack.Resolvers.ContractlessStandardResolver.Options.WithSecurity(MessagePackSecurity.UntrustedData)
            // );
            // string json = JsonConvert.SerializeObject(dict);
            //  ProceduralAsset asset = JsonConvert.DeserializeObject<ProceduralAsset>(json);

            // var options = MessagePack.MessagePackSerializerOptions.Standard
            //     .WithResolver(MessagePack.Resolvers.CompositeResolver.Create(
            //         MessagePack.Unity.UnityResolver.Instance,
            //         MessagePack.Resolvers.StandardResolver.Instance
            //     )).WithCompression(MessagePack.MessagePackCompression.Lz4Block);

            // var asset = MessagePack.MessagePackSerializer.Deserialize<ProceduralAsset>(
            //     decompressed
            // );
        
            // Deserialize JSON string to target object
           

            

            asset.rawTextures = new ProceduralTextures {
                albedoBase64JPG = GetBase64(fileMap, folder + "albedo.jpg"),
                metallicSmoothnessBase64JPG = GetBase64(fileMap, folder + "metallic_smoothness.jpg"),
                normalBase64JPG = GetBase64(fileMap, folder + "normal.jpg"),
                emissionBase64JPG = GetBase64(fileMap, folder + "emission.jpg")
            };

            asset.albedoTexturePath = null;
            asset.metallicSmoothnessTexturePath = null;
            asset.normalTexturePath = null;
            asset.emissionTexturePath = null;

            asset.saveMaterialToAssetDB = saveMaterialToAssetDB;
            yield return asset;

            // Debug.Log($"===== Downloaded asset {assetId}");

            yield return ProceduralTools.CreateAsset(
                asset, 
                data => {
                    results.Add(data); 
                 }, 
                 failMessage => {
                    runningActionFinished.errorMessage += $". {failMessage}";
                    runningActionFinished.success = false;
                 }
            );

            // Debug.Log($"===== Created asset {assetId}");
            
    }

    public static IEnumerator DownloadAndProcessAssets(string baseUrl, List<string> assetIds, Action<List<ProceduralAsset>> onComplete) {
        var results = new List<ProceduralAsset>();

        foreach (string assetId in assetIds) {
            string url = $"{baseUrl}/{assetId}.tar";
            UnityWebRequest request = UnityWebRequest.Get(url);
            request.downloadHandler = new DownloadHandlerBuffer();

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success) {
                Debug.LogError($"Download failed for {url}: {request.error}");
                continue;
            }

            byte[] tarBytes = request.downloadHandler.data;
            var fileMap = ExtractTarArchive(new MemoryStream(tarBytes));
            string folder = assetId + "/";
            string assetPath = folder + assetId + ".msgpack.gz";

            if (!fileMap.TryGetValue(assetPath, out byte[] compressed)) {
                Debug.LogWarning($"Missing asset: {assetPath}");
                continue;
            }

            byte[] decompressed;
            using (var gz = new GZipStream(new MemoryStream(compressed), CompressionMode.Decompress))
            using (var ms = new MemoryStream()) {
                gz.CopyTo(ms);
                decompressed = ms.ToArray();
            }


            var asset = MessagePack.MessagePackSerializer.Deserialize<ProceduralAsset>(
                decompressed,
                MessagePack.Resolvers.ThorContractlessStandardResolver.Options
            );

            asset.rawTextures = new ProceduralTextures {
                albedoBase64JPG = GetBase64(fileMap, folder + "albedo.jpg"),
                metallicSmoothnessBase64JPG = GetBase64(fileMap, folder + "metallic_smoothness.jpg"),
                normalBase64JPG = GetBase64(fileMap, folder + "normal.jpg"),
                emissionBase64JPG = GetBase64(fileMap, folder + "emission.jpg")
            };

            asset.albedoTexturePath = null;
            asset.metallicSmoothnessTexturePath = null;
            asset.normalTexturePath = null;
            asset.emissionTexturePath = null;

            results.Add(asset);
        }

        onComplete?.Invoke(results);
    }

    private static string GetBase64(Dictionary<string, byte[]> map, string path) {
        return map.TryGetValue(path, out var bytes)
            ? $"data:image/jpeg;base64,{Convert.ToBase64String(bytes)}"
            : null;
    }

    private static Dictionary<string, byte[]> ExtractTarArchive(Stream stream) {
        var files = new Dictionary<string, byte[]>();
        using var reader = new BinaryReader(stream);

        while (true) {
            byte[] header = reader.ReadBytes(512);
            if (header.Length < 512 || IsEndOfArchive(header)) break;

            string name = Encoding.ASCII.GetString(header, 0, 100).Trim('\0');
            if (string.IsNullOrEmpty(name)) break;

            string sizeStr = Encoding.ASCII.GetString(header, 124, 12).Trim('\0').Trim();
            long size = Convert.ToInt64(sizeStr, 8);

            byte[] content = reader.ReadBytes((int)size);
            files[name] = content;

            long pad = (512 - (size % 512)) % 512;
            if (pad > 0) reader.ReadBytes((int)pad);
        }

        return files;
    }

    private static bool IsEndOfArchive(byte[] block) {
        for (int i = 0; i < block.Length; i++) {
            if (block[i] != 0) return false;
        }
        return true;
    }
}
}