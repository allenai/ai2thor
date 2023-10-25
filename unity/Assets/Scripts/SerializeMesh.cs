#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
#endif
using UnityEngine;
 
namespace Thor.Utils
{
    [System.Serializable]
    public class SerializableMesh {
        // [SerializeField] public Vector2[] uv;
        // [SerializeField] public Vector3[] verticies;
        // [SerializeField] public Vector3[] normals;

        // [SerializeField] public int[] triangles;

        public Vector2[] uv;
        public Vector3[] verticies;
        public Vector3[] normals;
        public int[] triangles;
    }
    
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshFilter))]
    public class SerializeMesh : MonoBehaviour
    {
        // [HideInInspector] [SerializeField] Vector2[] uv;
        // [HideInInspector] [SerializeField] Vector3[] verticies;
        // [HideInInspector] [SerializeField] Vector3[] normals;

        // [HideInInspector] [SerializeField] int[] triangles;
        [SerializeField] public SerializableMesh model;
        [SerializeField] SerializableMesh[] collisionMeshes;
        [HideInInspector] [SerializeField] bool serialized = false;
        

        private static int materialCount = 0;
 
        void Awake()
        {
            if (serialized)
            {
                GetComponent<MeshFilter>().mesh = Rebuild();
            }
            // else {
            //     this.model = new SerializableMesh();
            // }
        }
 
        void Start()
        {
            if (serialized) 
            { 
                return;
            }
 
            Serialize();
        }

        private SerializableMesh serializeMesh(Mesh mesh) {
            var outMesh = new SerializableMesh();
            outMesh.uv = mesh.uv;
            outMesh.verticies = mesh.vertices;
            outMesh.triangles = mesh.triangles;
            outMesh.normals = mesh.normals;
            return outMesh;
        }

        private Mesh deSerializeMesh(SerializableMesh serializedMesh) {
            Mesh mesh = new Mesh();
            mesh.vertices = serializedMesh.verticies;
            mesh.triangles = serializedMesh.triangles;
            mesh.normals = serializedMesh.normals;
            mesh.uv = serializedMesh.uv;
            return mesh;
        }
 
        public void Serialize()
        {
            var mesh = GetComponent<MeshFilter>().mesh;
 
            // model.uv = mesh.uv;
            // model.verticies = mesh.vertices;
            // model.triangles = mesh.triangles;
            // model.normals = mesh.normals;

            model = serializeMesh(mesh);

            var colliders = transform.parent.Find("Colliders").GetComponentsInChildren<MeshCollider>();
            // if (this.collisionMeshes == null || colliders.Length != this.collisionMeshes.Length) {
                this.collisionMeshes = new SerializableMesh[colliders.Length];
            // }

            Debug.Log($"----- Serializing collider meshes {colliders.Length}");
            for (var i = 0; i < colliders.Length; i++) {
                var collisionMesh = colliders[i].sharedMesh;
                this.collisionMeshes[i] = this.serializeMesh(collisionMesh);
            }
 
            serialized = true;
            var matName = transform.parent.gameObject.name;

            

            // UnityEditor.AssetDatabase.CreateAsset(
            //         GetComponent<MeshRenderer>().sharedMaterial, $"{serializeMaterialsPath}/{matName}.mat"
            //     );


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
            Mesh mesh = this.deSerializeMesh(model);

            // Mesh mesh = new Mesh();
            // mesh.vertices = model.verticies;
            // mesh.triangles = model.triangles;
            // mesh.normals = model.normals;
            // mesh.uv = model.uv;

            
           
            // mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var colliders = transform.parent.Find("Colliders").GetComponentsInChildren<MeshCollider>();

            for (var i = 0; i < colliders.Length; i++) {
                colliders[i].sharedMesh = deSerializeMesh(this.collisionMeshes[i]);
            }

            colliders = transform.parent.Find("TriggerColliders").GetComponentsInChildren<MeshCollider>();

            for (var i = 0; i < colliders.Length; i++) {
                colliders[i].sharedMesh = deSerializeMesh(this.collisionMeshes[i]);
            }
 
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