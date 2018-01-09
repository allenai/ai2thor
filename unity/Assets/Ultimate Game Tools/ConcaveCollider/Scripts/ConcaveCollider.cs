using UnityEngine;
using System;
using System.Collections;
using System.Runtime.InteropServices;

[ExecuteInEditMode]
[AddComponentMenu("Ultimate Game Tools/Colliders/Concave Collider")]
public class ConcaveCollider : MonoBehaviour
{
    public enum EAlgorithm
    {
        Normal,
        Fast,
        Legacy
    }
                       public EAlgorithm       Algorithm                   = EAlgorithm.Fast;
                       public int              MaxHullVertices             = 64;
                       public int              MaxHulls                    = 128;
                       public float            InternalScale               = 10.0f;
                       public float            Precision                   = 0.8f;
                       public bool             CreateMeshAssets            = false;
                       public bool             CreateHullMesh              = false;
                       public bool             DebugLog                    = false;
                       public int              LegacyDepth                 = 6;
                       public bool             ShowAdvancedOptions         = false;
                       public float            MinHullVolume               = 0.00001f;
                       public float            BackFaceDistanceFactor      = 0.2f;
                       public bool             NormalizeInputMesh          = false;
                       public bool             ForceNoMultithreading       = false;

                       public PhysicMaterial   PhysMaterial                = null;
                       public bool             IsTrigger                   = false;

                       public GameObject[]     m_aGoHulls                  = null;

    [SerializeField]   private PhysicMaterial  LastMaterial                = null;
    [SerializeField]   private bool            LastIsTrigger               = false;

    [SerializeField]   private int             LargestHullVertices         = 0;
    [SerializeField]   private int             LargestHullFaces            = 0;

    public delegate void LogDelegate     ([MarshalAs(UnmanagedType.LPStr)]string message);
    public delegate void ProgressDelegate([MarshalAs(UnmanagedType.LPStr)]string message, float fPercent);

    void OnDestroy()
    {
        DestroyHulls();
    }

    void Reset()
    {
        DestroyHulls();
    }

    void Update()
    {
        if(PhysMaterial != LastMaterial)
        {
            foreach(GameObject hull in m_aGoHulls)
            {
                if(hull)
                {
                    Collider collider = hull.GetComponent<Collider>();

                    if(collider)
                    {
                        collider.material = PhysMaterial;
                        LastMaterial = PhysMaterial;
                    }
                }
            }
        }

        if(IsTrigger != LastIsTrigger)
        {
            foreach(GameObject hull in m_aGoHulls)
            {
                if(hull)
                {
                    Collider collider = hull.GetComponent<Collider>();

                    if(collider)
                    {
                        collider.isTrigger = IsTrigger;
                        LastIsTrigger = IsTrigger;
                    }
                }
            }
        }
    }

    public void DestroyHulls()
    {
        LargestHullVertices = 0;
        LargestHullFaces    = 0;

        if(m_aGoHulls == null)
        {
            return;
        }

        if(Application.isEditor && Application.isPlaying == false)
        {
            foreach(GameObject hull in m_aGoHulls)
            {
                if(hull) DestroyImmediate(hull);
            }
        }
        else
        {
            foreach(GameObject hull in m_aGoHulls)
            {
                if(hull) Destroy(hull);
            }
        }

        m_aGoHulls = null;
    }

    public void CancelComputation()
    {
        CancelConvexDecomposition();
    }

#if UNITY_EDITOR

    public void ComputeHulls(LogDelegate log, ProgressDelegate progress)
    {
        string strMeshAssetPath = "";

        if(CreateMeshAssets)
        {
            strMeshAssetPath = UnityEditor.EditorUtility.SaveFilePanelInProject("Save mesh asset", "mesh_" + gameObject.name + this.GetInstanceID().ToString() + ".asset", "asset", "Please enter a file name to save the mesh asset to");

            if(strMeshAssetPath.Length == 0)
            {
                return;
            }
        }

        MeshFilter theMesh = (MeshFilter)gameObject.GetComponent<MeshFilter>();
        
        bool bForceNoMultithreading = ForceNoMultithreading;

        if(Algorithm == EAlgorithm.Legacy)
        {
            // Force no multithreading for the legacy method since sometimes it hangs when merging hulls
            bForceNoMultithreading = true;
        }

        DllInit(!bForceNoMultithreading);

        if(log != null)
        {
            SetLogFunctionPointer(Marshal.GetFunctionPointerForDelegate(log));
        }
        else
        {
            SetLogFunctionPointer(IntPtr.Zero);
        }

        if(progress != null)
        {
            SetProgressFunctionPointer(Marshal.GetFunctionPointerForDelegate(progress));
        }
        else
        {
            SetProgressFunctionPointer(IntPtr.Zero);
        }

        SConvexDecompositionInfoInOut info = new SConvexDecompositionInfoInOut();

        int nMeshCount = 0;

        if(theMesh)
        {
            if(theMesh.sharedMesh)
            {
                info.uMaxHullVertices        = (uint)(Mathf.Max(3, MaxHullVertices));
                info.uMaxHulls               = (uint)(Mathf.Max(1, MaxHulls));
                info.fPrecision              = 1.0f - Mathf.Clamp01(Precision);
                info.fBackFaceDistanceFactor = BackFaceDistanceFactor;
                info.uLegacyDepth            = Algorithm == EAlgorithm.Legacy ? (uint)(Mathf.Max(1, LegacyDepth)) : 0;
                info.uNormalizeInputMesh     = NormalizeInputMesh == true ? (uint)1 : (uint)0;
                info.uUseFastVersion         = Algorithm == EAlgorithm.Fast ? (uint)1 : (uint)0;

                info.uTriangleCount          = (uint)theMesh.sharedMesh.triangles.Length / 3;
                info.uVertexCount            = (uint)theMesh.sharedMesh.vertexCount;

                if(DebugLog)
                {
                    Debug.Log(string.Format("Processing mesh: {0} triangles, {1} vertices.", info.uTriangleCount, info.uVertexCount));
                }

                Vector3[] av3Vertices = theMesh.sharedMesh.vertices;
                float fMeshRescale    = 1.0f;

                if(NormalizeInputMesh == false && InternalScale > 0.0f)
                {
                    av3Vertices = new Vector3[theMesh.sharedMesh.vertexCount];
                    float fMaxDistSquared = 0.0f;

                    for(int nVertex = 0; nVertex < theMesh.sharedMesh.vertexCount; nVertex++)
                    {
                        float fDistSquared = theMesh.sharedMesh.vertices[nVertex].sqrMagnitude;

                        if(fDistSquared > fMaxDistSquared)
                        {
                            fMaxDistSquared = fDistSquared;
                        }
                    }

                    fMeshRescale = InternalScale / Mathf.Sqrt(fMaxDistSquared);

                    if(DebugLog)
                    {
                        Debug.Log("Max vertex distance = " + Mathf.Sqrt(fMaxDistSquared) + ". Rescaling mesh by a factor of " + fMeshRescale);
                    }

                    for(int nVertex = 0; nVertex < theMesh.sharedMesh.vertexCount; nVertex++)
                    {
                        av3Vertices[nVertex] = theMesh.sharedMesh.vertices[nVertex] * fMeshRescale;
                    }
                }

                if(DoConvexDecomposition(ref info, av3Vertices, theMesh.sharedMesh.triangles))
                {
                    if(info.nHullsOut > 0)
                    {
                        if(DebugLog)
                        {
                            Debug.Log(string.Format("Created {0} hulls", info.nHullsOut));
                        }

                        DestroyHulls();

                        foreach(Collider collider in GetComponents<Collider>())
                        {
                            collider.enabled = false;
                        }

                        m_aGoHulls = new GameObject[info.nHullsOut];
                    }
                    else if(info.nHullsOut == 0)
                    {
                        if(log != null) log("Error: No hulls were generated");
                    }
                    else
                    {
                        // -1 User cancelled
                    }

                    for(int nHull = 0; nHull < info.nHullsOut; nHull++)
                    {
                        SConvexDecompositionHullInfo hullInfo = new SConvexDecompositionHullInfo();
                        GetHullInfo((uint)nHull, ref hullInfo);

                        if(hullInfo.nTriangleCount > 0)
                        {
                            m_aGoHulls[nHull] = new GameObject("Hull " + nHull);
                            m_aGoHulls[nHull].transform.position = this.transform.position;
                            m_aGoHulls[nHull].transform.rotation = this.transform.rotation;
                            m_aGoHulls[nHull].transform.parent   = this.transform;
                            m_aGoHulls[nHull].layer              = this.gameObject.layer;

                            Vector3[] hullVertices = new Vector3[hullInfo.nVertexCount];
                            int[]     hullIndices  = new int[hullInfo.nTriangleCount * 3];
                            
                            float fHullVolume     = -1.0f;
                            float fInvMeshRescale = 1.0f / fMeshRescale;

                            FillHullMeshData((uint)nHull, ref fHullVolume, hullIndices, hullVertices);

                            if(NormalizeInputMesh == false && InternalScale > 0.0f)
                            {
                                fInvMeshRescale = 1.0f / fMeshRescale;

                                for(int nVertex = 0; nVertex < hullVertices.Length; nVertex++)
                                {
                                    hullVertices[nVertex] *= fInvMeshRescale;
                                    hullVertices[nVertex]  = Vector3.Scale(hullVertices[nVertex], transform.localScale);
                                }
                            }
                            else
                            {
                                for(int nVertex = 0; nVertex < hullVertices.Length; nVertex++)
                                {
                                    hullVertices[nVertex] = Vector3.Scale(hullVertices[nVertex], transform.localScale);
                                }
                            }

                            Mesh hullMesh = new Mesh();
                            hullMesh.vertices  = hullVertices;
                            hullMesh.triangles = hullIndices;
                            
                            Collider hullCollider = null;

                            fHullVolume *= Mathf.Pow(fInvMeshRescale, 3.0f);
                            
                            if(fHullVolume < MinHullVolume)
                            {
                                if(DebugLog)
                                {
                                    Debug.Log(string.Format("Hull {0} will be approximated as a box collider (volume is {1:F2})", nHull, fHullVolume));
                                }
                                
                                MeshFilter meshf = m_aGoHulls[nHull].AddComponent<MeshFilter>();
                                meshf.sharedMesh = hullMesh;

                                // Let Unity3D compute the best fitting box (it will use the meshfilter)
                                hullCollider = m_aGoHulls[nHull].AddComponent<BoxCollider>() as BoxCollider;
                                BoxCollider boxCollider = hullCollider as BoxCollider;
                                boxCollider.center = hullMesh.bounds.center;
                                boxCollider.size = hullMesh.bounds.size;

                                if(CreateHullMesh == false)
                                {
                                    if(Application.isEditor && Application.isPlaying == false)
                                    {
                                        DestroyImmediate(meshf);
                                        DestroyImmediate(hullMesh);
                                    }
                                    else
                                    {
                                        Destroy(meshf);
                                        Destroy(hullMesh);
                                    }
                                }
                                else
                                {
                                    meshf.sharedMesh.RecalculateNormals();
                                    meshf.sharedMesh.uv = new Vector2[hullVertices.Length];
                                }
                            }                           
                            else
                            {
                                if(DebugLog)
                                {
                                    Debug.Log(string.Format("Hull {0} collider: {1} vertices and {2} triangles. Volume = {3}", nHull, hullMesh.vertexCount, hullMesh.triangles.Length / 3, fHullVolume));
                                }

                                MeshCollider meshCollider = m_aGoHulls[nHull].AddComponent<MeshCollider>() as MeshCollider;

                                meshCollider.sharedMesh = hullMesh;
                                meshCollider.convex     = true;
                                
                                hullCollider = meshCollider;

                                if(CreateHullMesh)
                                {
                                    MeshFilter meshf = m_aGoHulls[nHull].AddComponent<MeshFilter>();
                                    meshf.sharedMesh = hullMesh;
                                }
                                
                                if(CreateMeshAssets)
                                {
                                    if(nMeshCount == 0)
                                    {
                                        if(progress != null)
                                        {
                                            progress("Creating mesh assets", 0.0f);
                                        }

                                        // Avoid some shader warnings
                                        hullMesh.RecalculateNormals();
                                        hullMesh.uv = new Vector2[hullVertices.Length];

                                        UnityEditor.AssetDatabase.CreateAsset(hullMesh, strMeshAssetPath);
                                    }
                                    else
                                    {
                                        if(progress != null)
                                        {
                                            progress("Creating mesh assets", info.nHullsOut > 1 ? (nHull / (info.nHullsOut - 1.0f)) * 100.0f : 100.0f);
                                        }

                                        // Avoid some shader warnings
                                        hullMesh.RecalculateNormals();
                                        hullMesh.uv = new Vector2[hullVertices.Length];

                                        UnityEditor.AssetDatabase.AddObjectToAsset(hullMesh, strMeshAssetPath);
                                        UnityEditor.AssetDatabase.ImportAsset(UnityEditor.AssetDatabase.GetAssetPath(hullMesh));
                                    }
                                }
                                
                                nMeshCount++;
                            }
                            
                            if(hullCollider)
                            {
                                hullCollider.material  = PhysMaterial;
                                hullCollider.isTrigger = IsTrigger;

                                if(hullInfo.nTriangleCount > LargestHullFaces)    LargestHullFaces    = hullInfo.nTriangleCount;
                                if(hullInfo.nVertexCount   > LargestHullVertices) LargestHullVertices = hullInfo.nVertexCount;
                            }
                        }
                    }

                    if(CreateMeshAssets)
                    {
                        UnityEditor.AssetDatabase.Refresh();
                    }
                }
                else
                {
                    if(log != null) log("Error: convex decomposition could not be completed due to errors");
                }
            }
            else
            {
                if(log != null) log("Error: " + this.name + " has no mesh");
            }
        }
        else
        {
            if(log != null) log("Error: " + this.name + " has no mesh");
        }

        DllClose();
    }

#endif // UNITY_EDITOR

    public int GetLargestHullVertices()
    {
        return LargestHullVertices;
    }

    public int GetLargestHullFaces()
    {
        return LargestHullFaces;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct SConvexDecompositionInfoInOut
    {
        public uint     uMaxHullVertices;
        public uint     uMaxHulls;
        public float    fPrecision;
        public float    fBackFaceDistanceFactor;
        public uint     uLegacyDepth;
        public uint     uNormalizeInputMesh;
        public uint     uUseFastVersion;

        public uint     uTriangleCount;
        public uint     uVertexCount;

        // Out parameters

        public int      nHullsOut;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct SConvexDecompositionHullInfo
    {
        public int      nVertexCount;
        public int      nTriangleCount;
    };

    [DllImport("ConvexDecompositionDll")]
    private static extern void DllInit(bool bUseMultithreading);

    [DllImport("ConvexDecompositionDll")]
    private static extern void DllClose();

    [DllImport("ConvexDecompositionDll")]
    private static extern void SetLogFunctionPointer(IntPtr pfnUnity3DLog);

    [DllImport("ConvexDecompositionDll")]
    private static extern void SetProgressFunctionPointer(IntPtr pfnUnity3DProgress);

    [DllImport("ConvexDecompositionDll")]
    private static extern void CancelConvexDecomposition();

    [DllImport("ConvexDecompositionDll")]
    private static extern bool DoConvexDecomposition(ref SConvexDecompositionInfoInOut infoInOut, Vector3[] pfVertices, int[] puIndices);

    [DllImport("ConvexDecompositionDll")]
    private static extern bool GetHullInfo(uint uHullIndex, ref SConvexDecompositionHullInfo infoOut);

    [DllImport("ConvexDecompositionDll")]
    private static extern bool FillHullMeshData(uint uHullIndex, ref float pfVolumeOut, int[] pnIndicesOut, Vector3[] pfVerticesOut);
}
