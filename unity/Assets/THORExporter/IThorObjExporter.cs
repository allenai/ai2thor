using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public class ObjAssetTexture
{
    public string name = string.Empty;
    public string path = string.Empty;
    public Texture texture = null;
}

public class ObjExporter
{
    private int m_StartIndex = 0;
    private string m_ExportDirectory = string.Empty;
    private List<ObjAssetTexture> m_Assets;

    public void Start(string exportDirectory)
    {
        m_StartIndex = 0;
        m_ExportDirectory = exportDirectory;
        m_Assets = new List<ObjAssetTexture>();
    }

    public void End()
    {
        m_StartIndex = 0;
        foreach (var asset in m_Assets)
        {
            if (asset.texture != null)
            {
                var texture = asset.texture as Texture2D;
                string assetPath = AssetDatabase.GetAssetPath(texture);
                if (File.Exists(assetPath))
                {
                    string textureName = Path.GetFileName(assetPath);
                    string copyPath = Path.Combine(m_ExportDirectory, "textures", textureName);
                    if (!Directory.Exists(Path.GetDirectoryName(copyPath)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(copyPath));
                    }
                    File.Copy(assetPath, copyPath, true);
                }
            }
        }
        m_Assets.Clear();
    }

    public string MeshToString(MeshFilter mf, Transform tf)
    {
        Vector3 scale = tf.localScale;
        Vector3 pos = tf.localPosition;
        Quaternion rot = tf.localRotation;
        GameObject go = tf.gameObject;

        var meshRenderer = go.GetComponent<MeshRenderer>();

        int numVertices = 0;
        Mesh mesh = mf.sharedMesh;
        if (!mesh)
        {
            return "####Error####";
        }

        var strBuilder = new StringBuilder();
        foreach (var vv in mesh.vertices)
        {
            var v = tf.TransformPoint(vv);
            numVertices++;
            strBuilder.AppendLine($"v {v.x} {v.y} {v.z}");
        }
        strBuilder.Append("\n");
        foreach (var nn in mesh.normals)
        {
            var v = rot * nn;
            strBuilder.AppendLine($"vn {v.x} {v.y} {v.z}");
        }
        strBuilder.Append("\n");
        foreach (var uv in mesh.uv)
        {
            strBuilder.AppendLine($"vt {uv.x} {uv.y}");
        }
        for (var idx = 0; idx < mesh.subMeshCount; idx++)
        {
            // TODO(wilbert): Handle case with multiple materials. Currently we
            // are assuming that all objects have only one material at most

            if (meshRenderer != null && idx < meshRenderer.sharedMaterials.Length)
            {
                var matName = meshRenderer.sharedMaterials[idx].name;
                strBuilder.AppendLine($"usemtl {matName}");
            }

            int[] triangles = mesh.GetTriangles(idx);
            for (var i = 0; i < triangles.Length; i += 3)
            {
                strBuilder.AppendLine(
                    string.Format(
                        "f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}",
                        triangles[i] + 1 + m_StartIndex,
                        triangles[i + 1] + 1 + m_StartIndex,
                        triangles[i + 2] + 1 + m_StartIndex
                    )
                );
            }
        }

        m_StartIndex += numVertices;
        return strBuilder.ToString();
    }

    public string MaterialToString(Material mat)
    {
        var strBuilder = new StringBuilder();

        strBuilder.AppendLine($"newmtl {mat.name}");
        if (mat.HasProperty("_Color"))
        {
            strBuilder.AppendLine($"Ka {mat.color.r} {mat.color.g} {mat.color.b}");
            strBuilder.AppendLine($"Kd {mat.color.r} {mat.color.g} {mat.color.b}");
            if (mat.color.a < 1.0f)
            {
                strBuilder.AppendLine($"Tr {1 - mat.color.a}");
                strBuilder.AppendLine($"d {mat.color.a}");
            }
        }
        if (mat.HasProperty("_SpecColor"))
        {
            var specColor = mat.GetColor("_SpecColor");
            strBuilder.AppendLine($"Ks {specColor.r} {specColor.g} {specColor.b}");
        }
        if (mat.HasProperty("_MainTex"))
        {
            var texture = mat.GetTexture("_MainTex") as Texture2D;
            if (texture != null)
            {
                strBuilder.AppendLine($"map_Kd textures/{texture.name}.png");
                var asset = new ObjAssetTexture
                {
                    name = texture.name,
                    path = Path.Combine(m_ExportDirectory, "textures", texture.name + ".png"),
                    texture = texture,
                };
                m_Assets.Add(asset);
            }
        }

        strBuilder.AppendLine("illum 2");

        return strBuilder.ToString();
    }

    public (string, string) ProcessTransform(Transform tf, bool makeSubmeshes)
    {
        var strMesh = new StringBuilder();
        var strMaterial = new StringBuilder();

        if (tf.TryGetComponent<MeshFilter>(out var meshFilter))
        {
            strMesh.AppendLine($"# {tf.name}");
            strMesh.AppendLine($"#---------------");

            if (makeSubmeshes)
            {
                strMesh.AppendLine($"g {tf.name}");
            }

            strMesh.Append(MeshToString(meshFilter, tf));
        }

        if (tf.TryGetComponent<MeshRenderer>(out var meshRenderer))
        {
            strMaterial.AppendLine($"# {tf.name}");
            strMaterial.AppendLine($"#---------------");

            foreach (var mat in meshRenderer.sharedMaterials)
            {
                strMaterial.Append(MaterialToString(mat));
            }
        }

        for (var i = 0; i < tf.childCount; i++)
        {
            var (substrMesh, substrMaterial) = ProcessTransform(tf.GetChild(i), makeSubmeshes);
            strMesh.Append(substrMesh);
            strMaterial.Append(substrMaterial);
        }

        return (strMesh.ToString(), strMaterial.ToString());
    }

    public (string, string) Export(
        GameObject go,
        string fileName,
        string fileDirectory,
        bool makeSubmeshes
    )
    {
        Start(fileDirectory);

        var strMesh = new StringBuilder();
        var strMaterial = new StringBuilder();

        strMesh.AppendLine("----------------------------------------------");
        strMesh.AppendLine($"# {fileName}.obj");
        strMesh.AppendLine($"# Exported using iThor OBJ exporter");
        strMesh.AppendLine($"# {System.DateTime.Now.ToLongDateString()}");
        strMesh.AppendLine($"# {System.DateTime.Now.ToLongTimeString()}");
        strMesh.AppendLine("----------------------------------------------");

        strMaterial.AppendLine("---------------------------------------------");
        strMaterial.AppendLine($"# {fileName}.mtl");
        strMaterial.AppendLine($"# Exported using iThor OBJ exporter");
        strMaterial.AppendLine($"# {System.DateTime.Now.ToLongDateString()}");
        strMaterial.AppendLine($"# {System.DateTime.Now.ToLongTimeString()}");
        strMaterial.AppendLine("---------------------------------------------");

        var tf = go.transform;
        var originalPosition = tf.position;
        tf.position = Vector3.zero;

        if (!makeSubmeshes)
        {
            strMesh.AppendLine($"g {tf.name}");
        }

        var (substrMesh, substrMaterial) = ProcessTransform(tf, makeSubmeshes);
        strMesh.Append(substrMesh);
        strMaterial.Append(substrMaterial);

        tf.position = originalPosition;

        End();

        return (strMesh.ToString(), strMaterial.ToString());
    }
}

public class ObjExporterTools : MonoBehaviour
{
    [MenuItem("Tools/Export Selection to OBJ")]
    static void ExportSelectionWithSubmeshes()
    {
        ExportSelectionToObj(true);
    }

    [MenuItem("Tools/Export Selection to OBJ (No Submeshes)")]
    static void ExportSelectionWithoutSubmeshes()
    {
        ExportSelectionToObj(false);
    }

    static void ExportSelectionToObj(bool makeSubmeshes)
    {
        if (Selection.activeGameObject == null)
        {
            Debug.LogWarning("No GameObject selected to export");
            return;
        }

        string meshName = Selection.activeGameObject.name;
        string fileNameObj = EditorUtility.SaveFilePanel("Export .obj file", "", meshName, "obj");
        string fileNameMtl = Path.ChangeExtension(fileNameObj, "mtl");
        string fileNameNoExt = Path.GetFileNameWithoutExtension(fileNameObj);
        string fileDirectory = Path.GetDirectoryName(fileNameObj);

        var objExporter = new ObjExporter();
        var (objMeshStr, objMaterialStr) = objExporter.Export(
            Selection.activeGameObject,
            fileNameNoExt,
            fileDirectory,
            makeSubmeshes
        );

        WriteToFile(fileNameObj, objMeshStr);
        WriteToFile(fileNameMtl, objMaterialStr);
        Debug.Log($"Exported Mesh: {fileNameObj}");
        Debug.Log($"Exported Material: {fileNameMtl}");
    }

    static void WriteToFile(string fileName, string content)
    {
        using (var sw = new StreamWriter(fileName))
        {
            sw.Write(content);
        }
    }
}
