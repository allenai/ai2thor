using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

/// <summary>
/// 
/// </summary>
public class AddressablesEditor
{
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
            if (Directory.Exists(path))
            {
                return path;
            }
        }

        return string.Empty;
    }
}