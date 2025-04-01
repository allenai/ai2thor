using UnityEngine;
using UnityEditor;

public class BoxColliderCenterReset : MonoBehaviour
{
    [MenuItem("Tools/Fix BoxCollider Center and Normalize Scale &k")] // Ctrl+K
    static void FixBoxColliderCenterAndScale()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            Debug.LogWarning("No GameObjects selected.");
            return;
        }

        foreach (GameObject obj in selectedObjects)
        {
            if (obj.GetComponent<BoxCollider>() == null)
            {
                Debug.LogWarning($"Aborted: '{obj.name}' does not have a BoxCollider.");
                return;
            }
        }

        foreach (GameObject obj in selectedObjects)
        {
            BoxCollider boxCollider = obj.GetComponent<BoxCollider>();
            Transform transform = obj.transform;

            Undo.RecordObject(transform, "Adjust Transform");
            Undo.RecordObject(boxCollider, "Adjust BoxCollider");

            // 1. Apply center offset to position
            Vector3 worldOffset = transform.rotation * Vector3.Scale(boxCollider.center, transform.lossyScale);
            transform.position += worldOffset;
            boxCollider.center = Vector3.zero;

            // 2. Apply scale to size
            Vector3 scale = transform.localScale;
            boxCollider.size = Vector3.Scale(boxCollider.size, scale);
            transform.localScale = Vector3.one;

            Debug.Log($"✔ '{obj.name}': BoxCollider center & scale consolidated.");
        }
    }
}