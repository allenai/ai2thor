using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MeshFilter))]
public class NormalsVisualizer : Editor {

    private const string     EDITOR_PREF_KEY = "_normals_length";
    private const string     EDITOR_PREF_KEY_SHOW = "_normals_show";
    private       Mesh       mesh;
    private       MeshFilter mf;
    private       Vector3[]  verts;
    private       Vector3[]  normals;
    private       float      normalsLength = 1f;

    private bool showNormals = false;

    private void OnEnable() {
        mf   = target as MeshFilter;
        if (mf != null) {
            mesh = mf.sharedMesh;
        }
        normalsLength = EditorPrefs.GetFloat(EDITOR_PREF_KEY);
        showNormals = EditorPrefs.GetBool(EDITOR_PREF_KEY_SHOW);
    }

    private void OnSceneGUI() {
        if (mesh == null || !showNormals) {
            return;
        }

        Handles.matrix = mf.transform.localToWorldMatrix;
        Handles.color = Color.yellow;
        verts = mesh.vertices;
        normals = mesh.normals;
        int len = mesh.vertexCount;
        
        for (int i = 0; i < len; i++) {
            Handles.DrawLine(verts[i], verts[i] + normals[i] * normalsLength, 4.0f);
        }
    }

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();
        EditorGUI.BeginChangeCheck();
        normalsLength = EditorGUILayout.FloatField("Normals length", normalsLength);
        showNormals = EditorGUILayout.Toggle("Show normals", showNormals);
        if (EditorGUI.EndChangeCheck()) {
            EditorPrefs.SetFloat(EDITOR_PREF_KEY, normalsLength);
            EditorPrefs.SetBool(EDITOR_PREF_KEY_SHOW, showNormals);
        }
    }
}