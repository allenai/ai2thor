using UnityEngine;
using UnityEditor;
using System.Collections;

[CustomEditor(typeof(ConcaveCollider))]
public class ConcaveColliderEditor : Editor
{
    SerializedProperty PropAlgorithm;
    SerializedProperty PropMaxHullVertices;
    SerializedProperty PropMaxHulls;
    SerializedProperty PropInternalScale;
    SerializedProperty PropPrecision;
    SerializedProperty PropCreateMeshAssets;
    SerializedProperty PropCreateHullMesh;
    SerializedProperty PropDebugLog;
    SerializedProperty PropLegacyDepth;
    SerializedProperty PropShowAdvancedOptions;
    SerializedProperty PropMinHullVolume;
    SerializedProperty PropBackFaceDistanceFactor;
    SerializedProperty PropNormalizeInputMesh;
    SerializedProperty PropForceNoMultithreading;
    SerializedProperty PropIsTrigger;
    SerializedProperty PropMaterial;

    ConcaveCollider m_concaveCollider = null;

    void OnEnable()
    {
        PropAlgorithm              = serializedObject.FindProperty("Algorithm");
        PropMaxHullVertices        = serializedObject.FindProperty("MaxHullVertices");
        PropMaxHulls               = serializedObject.FindProperty("MaxHulls");
        PropInternalScale          = serializedObject.FindProperty("InternalScale");
        PropPrecision              = serializedObject.FindProperty("Precision");
        PropDebugLog               = serializedObject.FindProperty("DebugLog");
        PropLegacyDepth            = serializedObject.FindProperty("LegacyDepth");
        PropShowAdvancedOptions    = serializedObject.FindProperty("ShowAdvancedOptions");
        PropMinHullVolume          = serializedObject.FindProperty("MinHullVolume");
        PropBackFaceDistanceFactor = serializedObject.FindProperty("BackFaceDistanceFactor");
        PropNormalizeInputMesh     = serializedObject.FindProperty("NormalizeInputMesh");
        PropCreateHullMesh         = serializedObject.FindProperty("CreateHullMesh");
        PropCreateMeshAssets       = serializedObject.FindProperty("CreateMeshAssets");
        PropForceNoMultithreading  = serializedObject.FindProperty("ForceNoMultithreading");
        PropIsTrigger              = serializedObject.FindProperty("IsTrigger");
        PropMaterial               = serializedObject.FindProperty("PhysMaterial");
    }

    void Log(string message)
    {
        Debug.Log(message);
        Repaint();
    }

    void Progress(string message, float fPercent)
    {
        Repaint();

        if(EditorUtility.DisplayCancelableProgressBar("Computing hulls", message, fPercent / 100.0f))
        {
            if(m_concaveCollider)
            {
                m_concaveCollider.CancelComputation();
                m_concaveCollider = null;
            }
        }
    }
    
    public override void OnInspectorGUI()
    {
        // Update the serializedProperty - always do this in the beginning of OnInspectorGUI.

        serializedObject.Update();

        // Show the custom GUI controls

        m_concaveCollider = serializedObject.targetObject as ConcaveCollider;

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(PropAlgorithm, new GUIContent("Algorithm", "Chooses which convex decomposition algorithm to use"));
        EditorGUILayout.IntSlider(PropMaxHullVertices,  3,    255,    new GUIContent("Max Hull Vertices", "Limits the number of vertices each collider will have"));
        EditorGUILayout.IntSlider(PropMaxHulls,         1,    255,    new GUIContent("Max Hulls", "Limits the number of colliders created"));
        EditorGUILayout.Slider   (PropInternalScale,    0.0f, 200.0f, new GUIContent("Internal Scale", "Mesh will internally be processed at this size for convex decomposition. Varying this value may get better results."));
        EditorGUILayout.Slider   (PropPrecision,        0.0f, 1.0f,   new GUIContent("Precision", "The more the value, the more precision but also more hulls are created"));

        if(PropAlgorithm.enumNames[PropAlgorithm.enumValueIndex] == ConcaveCollider.EAlgorithm.Legacy.ToString())
        {
            EditorGUILayout.IntSlider(PropLegacyDepth, 0, 20, new GUIContent("Legacy Steps", "How many iterations to compute. More steps = more hulls and more computing time"));
        }

        PropCreateMeshAssets.boolValue      = EditorGUILayout.Toggle(new GUIContent("Enable Prefab Usage", "Will generate mesh assets for all hulls. This enables prefab instancing at both editing and runtime"), PropCreateMeshAssets.boolValue);  
        PropCreateHullMesh.boolValue        = EditorGUILayout.Toggle(new GUIContent("Add Hull Meshfilter", "Besides the collider components, also adds a MeshFilter to each hull that can be used for other things"), PropCreateHullMesh.boolValue);
        PropDebugLog.boolValue              = EditorGUILayout.Toggle(new GUIContent("Output debug messages", "Shows additional information in the log window after processing"),   PropDebugLog.boolValue);

        PropShowAdvancedOptions.boolValue = EditorGUILayout.Foldout(PropShowAdvancedOptions.boolValue, new GUIContent("Advanced Options"));

        if(PropShowAdvancedOptions.boolValue)
        {
            PropMinHullVolume.floatValue = EditorGUILayout.FloatField(new GUIContent("    Min Hull Volume", "Hulls created with less than this volume will be approximated using boxes"), PropMinHullVolume.floatValue);
            EditorGUILayout.Slider(PropBackFaceDistanceFactor, 0.0001f, 1.0f, new GUIContent("    Back Face Distance Factor", "Set to larger values for hollow objects"));
            PropNormalizeInputMesh.boolValue    = EditorGUILayout.Toggle(new GUIContent("    Normalize Input Mesh",    "Normalizes the input mesh for convex decomposition (overrides the Space Proportions parameter)"), PropNormalizeInputMesh.boolValue);
            PropForceNoMultithreading.boolValue = EditorGUILayout.Toggle(new GUIContent("    Force No Multithreading", "Disables multithreading on the collider computation. Use only if the process hangs"), PropForceNoMultithreading.boolValue);
        }

        EditorGUILayout.Separator();

        bool bNoHulls = false;

        if(m_concaveCollider)
        {
            if(m_concaveCollider.m_aGoHulls == null)
            {
                bNoHulls = true;
            }
            else if(m_concaveCollider.m_aGoHulls.Length == 0)
            {
                bNoHulls = true;
            }

            if(bNoHulls)
            {
                EditorGUILayout.LabelField(new GUIContent("No hulls computed"));
            }
            else
            {
                EditorGUILayout.LabelField(new GUIContent("Hulls computed: "              + m_concaveCollider.m_aGoHulls.Length));
                EditorGUILayout.LabelField(new GUIContent("Max hull vertices computed: "  + m_concaveCollider.GetLargestHullVertices()));
                EditorGUILayout.LabelField(new GUIContent("Max hull triangles computed: " + m_concaveCollider.GetLargestHullFaces()));
            }

            EditorGUILayout.Space();
        }

        EditorGUILayout.BeginHorizontal();

        if(GUILayout.Button(new GUIContent("Compute hull(s)")))
        {
            if(m_concaveCollider)
            {
                m_concaveCollider.ComputeHulls(new ConcaveCollider.LogDelegate(Log), new ConcaveCollider.ProgressDelegate(Progress));
                EditorUtility.ClearProgressBar();
            }
        }

        GUI.enabled = bNoHulls == false;

        if(GUILayout.Button(new GUIContent("Delete hull(s)")))
        {
            if(m_concaveCollider)
            {
                m_concaveCollider.DestroyHulls();
                EditorUtility.SetDirty(m_concaveCollider);
            }
        }

        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        
        PropIsTrigger.boolValue              = EditorGUILayout.Toggle     (new GUIContent("Is Trigger"),               PropIsTrigger.boolValue);
        PropMaterial.objectReferenceValue    = EditorGUILayout.ObjectField(new GUIContent("Material"),                 PropMaterial.objectReferenceValue, typeof(PhysicMaterial), false);

        // Apply changes to the serializedProperty - always do this in the end of OnInspectorGUI.

        serializedObject.ApplyModifiedProperties();
    }
}
