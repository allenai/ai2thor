using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityStandardAssets.Characters.FirstPerson;
 #if UNITY_EDITOR
    using UnityEditor.SceneManagement;
#endif
using System.Linq;

public class NavMeshSetup : MonoBehaviour
{
    public Transform goal;
    private UnityEngine.AI.NavMeshAgent navMeshAgent;
    private Transform hitPos;
    //private PhysicsRemoteFPSAgentController PhysicsController = null;

    void Start()
    {
         
        
    }

    #if UNITY_EDITOR

        [UnityEditor.MenuItem("NavMesh/Build NavMeshes for All Scenes")]
        public static void Build()
        {
            // var testSceneNames = GetSceneNames(3, 5, "Val");
            var trainSceneNames = GetSceneNames(12, 5, "Train");

            var selection = new List<string>();
            // selection.AddRange(testSceneNames);
            selection.AddRange(trainSceneNames);
            
            // selection.AddRange(trainSceneNames);

            
            // These scenes were mannually adjusted so the nav mesh variables should not be set automatically and should be build manually 
            var exclude = new List<string>() {
                "Assets/Scenes/FloorPlan_Train7_1.unity", // Radius of agent made smaller to fit between table small path where reachable positions exist
                "Assets/Scenes/FloorPlan_Train11_3.unity", // Unmade bed obstructs conectivity of navmesh
                "Assets/Scenes/FloorPlan_Val2_3.unity", // Unmade bed obstructs conectivity of navmesh
                };
            exclude.ForEach((x) => selection.Remove(x));
            Debug.Log("Scenes: " + string.Join(",", selection.ToArray()));
            selection.ToList().ForEach(sceneName => BuildNavmeshForScene(sceneName));
        }

        private static  List<string> GetSceneNames(int lastIndex, int lastSubIndex, string nameTemplate, string pathPrefix="Assets/Scenes") {
            var scenes = new List<string>();
            for (var i = 1; i <= lastIndex; i++) {
                for (var j = 1; j <= lastSubIndex; j++) {
                    var scene = pathPrefix + "/FloorPlan_" + nameTemplate + i + "_" + j + ".unity";
                    scenes.Add(scene);
                }
            }
            return scenes;
        }

        private static void BuildNavMeshesForScenes(IEnumerable<string> sceneNames) {
            foreach (var sceneName in sceneNames) {
                EditorSceneManager.OpenScene(sceneName);
                GameObject.Find("DebugCanvasPhysics/Object");
                // FindObjectsOfType<MeshRenderer>()
            }
        }

        private static void BuildNavmeshForScene(string sceneName) {
            EditorSceneManager.OpenScene(sceneName);
            SetNavMeshNotWalkable(GameObject.Find("Objects"));
            SetNavMeshNotWalkable(GameObject.Find("Structure"));
            var agentController = FindObjectOfType<PhysicsRemoteFPSAgentController>();
            //var capsuleCollider = agentController.GetComponent<CapsuleCollider>();
            var navmeshAgent = agentController.GetComponent<NavMeshAgent>();
            navmeshAgent.enabled = true;
            // The Editor bake interface does not take with parameters and could not be modified as of 2018.3
            //var buildSettings = 
            new NavMeshBuildSettings() {
                agentTypeID = navmeshAgent.agentTypeID,
                agentRadius = navmeshAgent.radius,
                agentHeight = navmeshAgent.height,
                agentSlope = 10,
                agentClimb = 0.5f,
                minRegionArea = 0.05f,
                overrideVoxelSize = false,
                overrideTileSize = false
            };
    
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static void SetNavMeshNotWalkable(GameObject hirerarchy) {
            
             var objectHierarchy = GameObject.Find("Objects");
             if (objectHierarchy == null) {
                 objectHierarchy = GameObject.Find("Object");
             }
             for (int i = 0; i < objectHierarchy.transform.childCount; i++) {
                 var child = objectHierarchy.transform.GetChild(i);
                 child.GetComponentsInChildren<MeshRenderer>().ToList().ForEach( meshRenderer => {
                     Debug.Log("Mesh Renderer " + meshRenderer.gameObject.name + " layer ");
                     UnityEditor.GameObjectUtility.SetStaticEditorFlags(meshRenderer.gameObject,  UnityEditor.StaticEditorFlags.NavigationStatic);
                     UnityEditor.GameObjectUtility.SetNavMeshArea(meshRenderer.gameObject, NavMesh.GetAreaFromName("Not Walkable"));
                 });
                 Debug.Log("Setting flag for " + child.gameObject.name + " layer " +  NavMesh.GetAreaFromName("Not Walkable"));
             }
        }
    #endif
}