using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;
using UnityEngine;

/// <summary>
/// 
/// </summary>
public class AddressablesEditor
{
    private static string LINUX_CACHED_DIR = "CachedAddressableLink/Linux/aa";
    private static string LINUX_STREAMING_DIR = "StreamingAssets/aa";

    private static string OSX_CACHED_DIR = "CachedAddressableLink/OSX/aa";
    private static string OSX_STREAMING_DIR = "Contents/Resources/Data/StreamingAssets/aa";

    /// <summary>
    /// Configures all assets within Addressables folder to addressables group
    /// </summary>
    public static void RefreshAddressables()
    {
        string path = "Assets/Addressables";
        string[] guids = AssetDatabase.FindAssets("", new[] { path });

        List<AddressableAssetEntry> entriesAdded = new List<AddressableAssetEntry>();
        for (int i = 0; i < guids.Length; i++)
        {
            entriesAdded.Add(AddToAddressablesDefaultGroup(guids[i]));
        }

        AddressableAssetSettingsDefaultObject.Settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entriesAdded, true);
    }

    /// <summary>
    /// Adds all content within folder recursively to addressables
    /// </summary>
    [MenuItem("Tools/Addressables/Add To Addressables From Selected Folder")]
    public static void SetAddressablesAtFolder()
    {
        string path = GetSelectedFolder();

        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning("No path selected for marking as addressables!");
            return;
        }

        string[] guids = AssetDatabase.FindAssets("", new[] { path });

        var entriesAdded = new List<AddressableAssetEntry>();
        for (int i = 0; i < guids.Length; i++)
        {
            entriesAdded.Add(AddToAddressablesDefaultGroup(guids[i]));
        }

        AddressableAssetSettingsDefaultObject.Settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entriesAdded, true);
    }

    private static AddressableAssetEntry AddToAddressablesDefaultGroup(string guid)
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        AddressableAssetGroup group = settings.DefaultGroup;

        AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
        entry.address = AssetDatabase.GUIDToAssetPath(guid);
        entry.labels.Add("MyLabel");

        Debug.Log(AssetDatabase.GUIDToAssetPath(guid) + " was added to Addressables default group!");

        return entry;
    }

    private static string GetSelectedFolder()
    {
        var path = "";
        var obj = Selection.activeObject;
        if (obj == null)
        {
            return string.Empty;
        }
        else
        {
            path = AssetDatabase.GetAssetPath(obj.GetInstanceID());
        }
        if (path.Length > 0)
        {
            if (Directory.Exists(Path.GetDirectoryName(path)))
            {
                return Path.GetDirectoryName(path);
            }
        }
        return string.Empty;
    }

    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        var (sourceDir, targetDir) = GetBuildAssetsDirectories(target, pathToBuiltProject);

        #if UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
            // If no addressable catalog is found, bring it from cache. Otherwise bring it from S3
            if (!Directory.Exists(targetDir))
            {
                if(Directory.Exists(sourceDir))
                    CopyFilesRecursively(sourceDir, targetDir);
                else 
                    CopyAddressableFilesFromS3(targetDir);
            }
           
        #endif
    }

    public static (string sourceDir, string targetDir) GetBuildAssetsDirectories(BuildTarget target, string pathToBuiltProject) {
        string sourceDir = null;
        string targetDir = null;

        if (target == BuildTarget.StandaloneLinux64) {
            sourceDir = Path.Combine(Path.GetDirectoryName(Application.dataPath), LINUX_CACHED_DIR);
            string dataDir = Path.ChangeExtension(pathToBuiltProject, null) + "_Data/";
            targetDir = Path.Combine(dataDir, LINUX_STREAMING_DIR);
        }
        else if (target == BuildTarget.StandaloneOSX) {
            sourceDir = Path.Combine(Path.GetDirectoryName(Application.dataPath), OSX_CACHED_DIR);
            targetDir = Path.Combine(pathToBuiltProject, OSX_STREAMING_DIR);
        }
        
        return (sourceDir, targetDir);
    }

    private static void CopyFilesRecursively(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir))
            File.Copy(file, Path.Combine(targetDir, Path.GetFileName(file)));

        foreach (var directory in Directory.GetDirectories(sourceDir))
            CopyFilesRecursively(directory, Path.Combine(targetDir, Path.GetFileName(directory)));
    }

    private static void CopyAddressableFilesFromS3(string targetDir) {
        string bucketUrl = "https://ai2thor-mcs-addressables.s3.amazonaws.com/";
        string catalogUrl = bucketUrl + "StandaloneLinux64/catalog.json";
        string settingsUrl = bucketUrl + "StandaloneLinux64/settings.json";
        string linkUrl = bucketUrl + "StandaloneLinux64/link.xml";

        //Download files
        using (var client = new WebClient())
        {
            client.DownloadFile(catalogUrl, targetDir + "/catalog.json");
            client.DownloadFile(settingsUrl, targetDir + "/settings.json");
            Directory.CreateDirectory(targetDir + "AddressablesLink/");
            client.DownloadFile(linkUrl, targetDir + "/AddressablesLink/link.xml");
        }
    }
}
