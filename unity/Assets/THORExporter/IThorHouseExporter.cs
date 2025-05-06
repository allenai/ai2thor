using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IThorHouseExporter : MonoBehaviour
{
    [System.Serializable]
    class IThorExportNode
    {
        public string objectName = string.Empty;
        public string objectType = string.Empty;
        public string parentName = string.Empty; // Structural, Lighting, etc.
        public string assetId = string.Empty;
        public Vector3 position;
        public Vector3 rotation;
        public bool kinematic;
    }

    [System.Serializable]
    class IThorExportLightNode
    {
        public string name = string.Empty;
        public string type = string.Empty;
        public string parentName = string.Empty; // Structural, Lighting, etc.
        public Vector3 position;
        public Vector3 rotation;
        public Color color;
        public float intensity;
        public bool castShadows;
    }

    [System.Serializable]
    class IThorExportRoomNode
    {
        public string name = string.Empty;
        // TODO(wilbert): Complete the definition for this node
    }

    [System.Serializable]
    class IThorExportDoorNode
    {
        public string name = string.Empty;
        // TODO(wilbert): Complete the definition for this node
    }

    [System.Serializable]
    class IThorExportWallNode
    {
        public string name = string.Empty;
        // TODO(wilbert): Complete the definition for this node
    }

    [System.Serializable]
    class IThorExportWindowNode
    {
        public string name = string.Empty;
        // TODO(wilbert): Complete the definition for this node
    }

    [System.Serializable]
    class IThorExportNodeList
    {
        public List<IThorExportNode> objects;
        public List<IThorExportLightNode> lights;
        public List<IThorExportRoomNode> rooms;
        public List<IThorExportDoorNode> doors;
        public List<IThorExportWallNode> walls;
        public List<IThorExportWindowNode> windows;
    }

    [MenuItem("Tools/IThor - Export JSON current selection")]
    static void ExportJSONSelection()
    {
        
    }

    [MenuItem("Tools/IThor - Export Current Scene")]
    static void ExportCurrentScene()
    {
        Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        GameObject[] objects = scene.GetRootGameObjects();

        var fileNameJSON = EditorUtility.SaveFilePanel("Export .json file", "", scene.name, "json");
        var folderPath = Path.GetDirectoryName(fileNameJSON);

        var objExporter = new ObjExporter();

        var nodes = new List<IThorExportNode>();
        var lightNodes = new List<IThorExportLightNode>();

        var stack = new Stack<(GameObject, GameObject)>();
        foreach (var obj in objects)
            stack.Push((obj, null));

        while (stack.Count > 0)
        {
            var (obj, parent) = stack.Pop();
            if (obj.activeSelf == false)
                continue;

            // TODO(wilbert): should check for other cases of no-export?

            // Do all exportable objects have a SimObjPhysics component?
            if (obj.GetComponent<SimObjPhysics>())
            {
                var simObjPhysics = obj.GetComponent<SimObjPhysics>();
                var node = new IThorExportNode
                {
                    objectName = obj.name,
                    objectType = Enum.GetName(typeof(SimObjType), simObjPhysics.Type),
                    parentName = parent != null ? parent.name : string.Empty,
                    assetId = simObjPhysics.assetID,
                    position = obj.transform.position,
                    rotation = obj.transform.rotation.eulerAngles,
                    kinematic = simObjPhysics.isStatic,
                };

                // No link to a prefab, so export all geometry to an .obj file
                if (simObjPhysics.assetID == "")
                {
                    var filePathObj = Path.Combine(folderPath, "models", $"{obj.name}.obj");
                    var filePathMtl = Path.Combine(folderPath, "models", $"{obj.name}.mtl");

                    var fileNameNoExt = Path.GetFileNameWithoutExtension(filePathObj);
                    var fileDirectory = Path.GetDirectoryName(filePathObj);

                    var (objMeshStr, objMaterialStr) = objExporter.Export(
                        obj,
                        fileNameNoExt,
                        fileDirectory,
                        true
                    );

                    using (var writer = new StreamWriter(filePathObj))
                        writer.Write(objMeshStr);
                    using (var writer = new StreamWriter(filePathMtl))
                        writer.Write(objMaterialStr);
                }

                nodes.Add(node);
            }
            else if (obj.GetComponent<Light>())
            {
                var light = obj.GetComponent<Light>();
                var lightNode = new IThorExportLightNode
                {
                    name = light.name,
                    type = Enum.GetName(typeof(LightType), light.type).ToLower(),
                    parentName = parent != null ? parent.name : string.Empty,
                    position = obj.transform.position,
                    rotation = obj.transform.rotation.eulerAngles,
                    color = light.color,
                    intensity = light.intensity,
                    castShadows = light.shadows != LightShadows.None,
                };

                lightNodes.Add(lightNode);
            }

            foreach (Transform childTf in obj.transform)
                stack.Push((childTf.gameObject, obj));
        }

        // Use the wrapper for JsonUtility to serialize the list
        var nodesList = new IThorExportNodeList { objects = nodes, lights = lightNodes };

        var json = JsonUtility.ToJson(nodesList, true);
        File.WriteAllText(fileNameJSON, json);
    }
}
