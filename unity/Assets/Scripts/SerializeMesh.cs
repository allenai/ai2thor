using System;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

using System.IO;
 
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

         private static string getAssetRelativePath(string absPath) {
            return string.Join("/", absPath.Split('/').SkipWhile(x=> x != "Assets"));
        }

        public static string MeshToObj(string name, Mesh mesh) {
            var objString = $"o {name}";
            var verts = string.Join("\n", mesh.vertices.Select(v => $"v {v.x:F6} {v.y:F6} {v.z:F6}"));
            var uvs = string.Join("\n", mesh.uv.Select(v => $"vt {v.x:F6} {v.y:F6}"));
            var normals = string.Join("\n", mesh.normals.Select(v => $"vn {v.x:F6} {v.y:F6} {v.z:F6}"));
            var faces =  string.Join("\n", Enumerable.Range(0, mesh.triangles.Length / 3)
                .Select(i => 
                    ( i0: mesh.triangles[i * 3]+1, i1: mesh.triangles[i * 3 + 1]+1, i2: mesh.triangles[i * 3 + 2]+1))
                .Select(indx => $"f {indx.i0}/{indx.i0}/{indx.i0} {indx.i1}/{indx.i1}/{indx.i1} {indx.i2}/{indx.i2}/{indx.i2}")
            );
            return $"{objString}\n{verts}\n{uvs}\n{normals}\ns 1\n{faces}";
        }

        private static string SaveAsObj( Mesh mesh, string assetId, string outPath, string prefix = "") {

            var obj = MeshToObj(assetId, mesh);

            if (!Directory.Exists(outPath)) {
                Directory.CreateDirectory(outPath);
            }

            // var f = File.Create($"{outModelsBasePath}/{assetId}.obj");
            var fileObj = $"{outPath}/{prefix}_{assetId}.obj";
            Debug.Log($"Writing obj to `{fileObj}`");
            File.WriteAllText(fileObj, obj);

            return fileObj;
        }

#if UNITY_EDITOR
        public static void SaveMeshesAsObjAndReplaceReferences(GameObject go, string assetId, string modelsOutPath, string collidersOutPath) {
            var meshGo = go.transform.Find("mesh");

            var mf = meshGo.GetComponentInChildren<MeshFilter>();
            var objPath = SaveAsObj(mf.sharedMesh, assetId, modelsOutPath);

            var colliders = go.transform.Find("Colliders").GetComponentsInChildren<MeshCollider>();
            var colliderObjPaths = colliders.Select((c, i) => SaveAsObj(c.sharedMesh, assetId, collidersOutPath, prefix: $"col_{i}")).ToArray();
            
            AssetDatabase.Refresh();
            
            // is this necessary?
            if (mf.sharedMesh.indexFormat == UnityEngine.Rendering.IndexFormat.UInt32) {
                var mi = AssetImporter.GetAtPath(getAssetRelativePath(objPath)) as ModelImporter;
                mi.indexFormat = ModelImporterIndexFormat.UInt32;
            }
            

            var mesh = (Mesh)AssetDatabase.LoadAssetAtPath(getAssetRelativePath(objPath),typeof(Mesh));

            mf.sharedMesh = mesh;

             var collisionMeshes = colliderObjPaths.Select(path => (Mesh)AssetDatabase.LoadAssetAtPath(getAssetRelativePath(path), typeof(Mesh))).ToArray();
            for (var i = 0; i < colliders.Length; i++) {
                
                // is this necessary?
                if (colliders[i].sharedMesh.indexFormat == UnityEngine.Rendering.IndexFormat.UInt32) {
                    var mi = AssetImporter.GetAtPath(getAssetRelativePath(colliderObjPaths[i])) as ModelImporter;
                    mi.indexFormat = ModelImporterIndexFormat.UInt32;
                }
                // collisionMeshes[i].RecalculateNormals();
                colliders[i].sharedMesh = collisionMeshes[i];
            }
            colliders = go.transform.Find("TriggerColliders").GetComponentsInChildren<MeshCollider>();
            for (var i = 0; i < colliders.Length; i++) {
                colliders[i].sharedMesh  = collisionMeshes[i];
            }

        }
#endif
 
        void Awake()
        {
            Debug.Log("--- Awake called on object " + transform.parent.gameObject.name);
            if (serialized)
            {
                GetComponent<MeshFilter>().sharedMesh = Rebuild();
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
            Debug.Log("--- Serialize called  " + transform.parent.gameObject.name);
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