#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class PrefabNameRevert {
    [MenuItem("Prefab/RevertName")]
    static void RevertPrefabNames() {
        RevertAllNames(Selection.GetFiltered(typeof(GameObject), SelectionMode.Editable));
    }

    public static string GetPrefabAssetName(GameObject prefab, string name = "") {
        // Debug.Log("Object " + name + " object: " + prefab.name);
        var original = PrefabUtility.GetCorrespondingObjectFromOriginalSource(prefab);
        if (original != null) {
            return original.name;
        } else {
            Debug.LogError($"No prefab could be found for object '{prefab.name}'");
            return null;
        }
    }

    public static void RemoveNameModification(UnityEngine.Object aObj) {
        var mods = new List<PropertyModification>(PrefabUtility.GetPropertyModifications(aObj));
        for (int i = 0; i < mods.Count; i++) {
            if (mods[i].propertyPath == "m_Name") {
                mods.RemoveAt(i);
            }
        }
        PrefabUtility.SetPropertyModifications(aObj, mods.ToArray());
    }
    public static void RevertAllNames(Object[] aObjects) {
        var items = new List<Object>();
        foreach (var item in aObjects) {
            var prefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(item);
            if (prefab != null) {
                Undo.RecordObject(item, "Revert perfab name");
                item.name = prefab.name;
                items.Add(item);
            }
        }
        Undo.FlushUndoRecordObjects();
        foreach (var item in items) {
            RemoveNameModification(item);
        }
    }

}
#endif