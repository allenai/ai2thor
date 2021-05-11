using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;

/// <summary>
/// 
/// </summary>
public class AddressablesEditor
{
    [MenuItem("Tools/Addressables/Refresh Addressables Folder")]
    public static void RefreshAddressables()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var group = settings.DefaultGroup;
        var guids = AssetDatabase.FindAssets("", new[] { "Assets/Addressables" });

        var entriesAdded = new List<AddressableAssetEntry>();
        for (int i = 0; i < guids.Length; i++)
        {
            var entry = settings.CreateOrMoveEntry(guids[i], group, readOnly: false, postEvent: false);
            entry.address = AssetDatabase.GUIDToAssetPath(guids[i]);
            entry.labels.Add("MyLabel");
            entriesAdded.Add(entry);
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entriesAdded, true);
    }

    [MenuItem("Tools/Addressables/Add Addressables From Selected Folder")]
    public static void SetAddressablesAtFolder()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        var group = settings.DefaultGroup;
        var path = GetSelectedFolder();

        path = string.IsNullOrEmpty(path) ? "Assets/AddressableAssets" : path;
        var guids = AssetDatabase.FindAssets("", new[] { path });

        var entriesAdded = new List<AddressableAssetEntry>();
        for (int i = 0; i < guids.Length; i++)
        {
            var entry = settings.CreateOrMoveEntry(guids[i], group, readOnly: false, postEvent: false);
            entry.address = AssetDatabase.GUIDToAssetPath(guids[i]);
            entry.labels.Add("MyLabel");
            entriesAdded.Add(entry);
        }

        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entriesAdded, true);
    }

    private static string GetSelectedFolder()
    {
        var path = "";
        var obj = Selection.activeObject;
        if (obj == null) path = "Assets";
        else path = AssetDatabase.GetAssetPath(obj.GetInstanceID());
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