#if UNITY_EDITOR
using System;
using UnityEditor;
#endif
using UnityEngine;
 
namespace Thor.Utils
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    public class SerializeMesh : MonoBehaviour
    {
        [HideInInspector] [SerializeField] Vector2[] uv;
        [HideInInspector] [SerializeField] Vector3[] verticies;
        [HideInInspector] [SerializeField] Vector3[] normals;

        [HideInInspector] [SerializeField] int[] triangles;
        [HideInInspector] [SerializeField] bool serialized = false;
        

        private static int materialCount = 0;

        public const string serializeBasePath = "Assets/Resources/ai2thor-objaverse/NoveltyTHOR_Assets";

        private string serializeMaterialsPath = $"{serializeBasePath}/Materials";
        public const string texturesRelativePath = "Textures";
 
        void Awake()
        {
            if (serialized)
            {
                GetComponent<MeshFilter>().mesh = Rebuild();
            }
        }
 
        void Start()
        {
            if (serialized) 
            { 
                return;
            }
 
            Serialize();
        }
 
        public void Serialize()
        {
            var mesh = GetComponent<MeshFilter>().mesh;
 
            uv = mesh.uv;
            verticies = mesh.vertices;
            triangles = mesh.triangles;
            normals = mesh.normals;
 
            serialized = true;
            var matName = transform.parent.gameObject.name;

            

            UnityEditor.AssetDatabase.CreateAsset(
                    GetComponent<MeshRenderer>().sharedMaterial, $"{serializeMaterialsPath}/{matName}.mat"
                );
            // try {
                // UnityEditor.AssetDatabase.CreateAsset(
                //     GetComponentInChildren<MeshRenderer>().material, $"{serializeMaterialsPath}/{matName}.mat"
                // );
            // }
            // // There are some restricted material names so if it fails name it with scheme <scene>_<count>
            // catch (Exception e) {
               
            //     var sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            //     matName = $"{sceneName}_{materialCount}";
            //      UnityEditor.AssetDatabase.CreateAsset(
            //         GetComponent<MeshRenderer>().material, $"{serializeMaterialsPath}/{matName}.mat"
            //     );
            //     materialCount++;
            // }
        }
 
        public Mesh Rebuild()
        {
            Mesh mesh = new Mesh();
            mesh.vertices = verticies;
            mesh.triangles = triangles;
            mesh.normals = normals;
            mesh.uv = uv;
           
            // mesh.RecalculateNormals();
            mesh.RecalculateBounds();
 
            return mesh;
        }
    }
 
#if UNITY_EDITOR
    [CustomEditor(typeof(SerializeMesh))]
    class SerializeMeshEditor : Editor
    {
        SerializeMesh obj;
 
        void OnSceneGUI()
        {
            obj = (SerializeMesh)target;
        }
 
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
 
            if (GUILayout.Button("Rebuild"))
            {
                if (obj)
                {
                    obj.gameObject.GetComponent<MeshFilter>().mesh = obj.Rebuild();
                }
            }
 
            if (GUILayout.Button("Serialize"))
            {
                if (obj)
                {
                   obj.Serialize();
                }
            }
        }
    }
#endif
}