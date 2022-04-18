using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityStandardAssets.Characters.FirstPerson;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
#endif
using System.Linq;

public class NavMeshSetup : MonoBehaviour {
    public Transform goal;
    private UnityEngine.AI.NavMeshAgent navMeshAgent;
    private Transform hitPos;
    // private PhysicsRemoteFPSAgentController PhysicsController = null;

    void Start() {


    }

#if UNITY_EDITOR

    [UnityEditor.MenuItem("NavMesh/Save Full Scene Prefabs (all houdini scenes)")]
    public static void SaveHoudiniScenePrefabs() {
        var trainSceneNames = houdiniScenes();

        // These scenes were mannually adjusted so the nav mesh variables should not be set automatically and should be build manually 
        trainSceneNames.ForEach((x) => saveSceneAsPrefab(x));
    }

    private static void saveSceneAsPrefab(string sceneName) {
        EditorSceneManager.OpenScene(sceneName);
        GameObject sceneParent = new GameObject();
        sceneParent.name = "Scene";
        foreach (GameObject obj in Object.FindObjectsOfType(typeof(GameObject))) {
            if (obj.transform.parent == null && (obj.name == "Objects" || obj.name == "Structure" || obj.name == "Lighting")) {

                // create new object then destroy it
                GameObject copyObj = Instantiate(obj) as GameObject;
                copyObj.transform.parent = sceneParent.transform;
                // stroyImmediate(copyObj);
                copyObj.name = copyObj.name.Replace("(Clone)", "");
            }
        }

        sceneName = sceneName.Substring(sceneName.IndexOf("/") + 1);
        sceneName = sceneName.Substring(sceneName.IndexOf("/") + 1);
        PrefabUtility.SaveAsPrefabAsset(sceneParent, "Assets/Scenes/prefab_exports/" + sceneName.Substring(0, sceneName.Length - ".unity".Length) + ".prefab");
        DestroyImmediate(sceneParent);
    }

    private static List<string> houdiniScenes(string pathPrefix = "Assets/Scenes") {
        // list hand chosen from Winson
        // gets iTHOR scene names
        var scenes = new List<string>();

        // house 1
        scenes.Add(pathPrefix + "/FloorPlan508_physics.unity");
        scenes.Add(pathPrefix + "/FloorPlan1_physics.unity");
        scenes.Add(pathPrefix + "/FloorPlan210_physics.unity");
        scenes.Add(pathPrefix + "/FloorPlan301_physics.unity");
        scenes.Add(pathPrefix + "/FloorPlan416_physics.unity");

        // house 2
        scenes.Add(pathPrefix + "/FloorPlan507_physics.unity");
        scenes.Add(pathPrefix + "/FloorPlan10_physics.unity");
        scenes.Add(pathPrefix + "/FloorPlan202_physics.unity");
        scenes.Add(pathPrefix + "/FloorPlan304_physics.unity");
        scenes.Add(pathPrefix + "/FloorPlan404_physics.unity");

        // house 3
        scenes.Add(pathPrefix + "/FloorPlan514_physics.unity");
        scenes.Add(pathPrefix + "/FloorPlan14_physics.unity");
        scenes.Add(pathPrefix + "/FloorPlan202_physics.unity");
        scenes.Add(pathPrefix + "/FloorPlan304_physics.unity");
        scenes.Add(pathPrefix + "/FloorPlan404_physics.unity");

        // house 4
        scenes.Add(pathPrefix + "/FloorPlan522_physics.unity");
        scenes.Add(pathPrefix + "/FloorPlan23_physics.unity");
        scenes.Add(pathPrefix + "/FloorPlan225_physics.unity");
        scenes.Add(pathPrefix + "/FloorPlan321_physics.unity");
        scenes.Add(pathPrefix + "/FloorPlan424_physics.unity");

        // house 5
        scenes.Add(pathPrefix + "/FloorPlan529_physics.unity");
        scenes.Add(pathPrefix + "/FloorPlan29_physics.unity");
        scenes.Add(pathPrefix + "/FloorPlan228_physics.unity");
        scenes.Add(pathPrefix + "/FloorPlan329_physics.unity");
        scenes.Add(pathPrefix + "/FloorPlan428_physics.unity");

        return scenes;
    }

    [UnityEditor.MenuItem("NavMesh/Build NavMeshes for All Scenes")]
    public static void Build() {
        // var testSceneNames = GetRoboSceneNames(3, 5, "Val");
        // var trainSceneNames = GetRoboSceneNames(12, 5, "Train");

        // GetSceneNames(1, 30) + GetSceneNames(201, 230) + GetSceneNames(301, 330) + GetSceneNames(401, 430) + GetSceneNames(501, 530)

        var selection = new List<string>();
        // selection.AddRange(testSceneNames);
        selection.AddRange(GetSceneNames(1, 30));
        selection.AddRange(GetSceneNames(201, 230));
        selection.AddRange(GetSceneNames(301, 330));
        selection.AddRange(GetSceneNames(401, 430));
        selection.AddRange(GetSceneNames(501, 530));

        // selection.Add("Assets/Scenes/FloorPlan227_physics.unity");

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

    [UnityEditor.MenuItem("NavMesh/Build NavMesh for Active Scene")]
    public static void BuildForCurrentActiveScene() {
        BuildNavmeshForScene(EditorSceneManager.GetActiveScene().path);
    } 

    private static List<string> GetRoboSceneNames(int lastIndex, int lastSubIndex, string nameTemplate, string pathPrefix = "Assets/Scenes") {
        var scenes = new List<string>();
        for (var i = 1; i <= lastIndex; i++) {
            for (var j = 1; j <= lastSubIndex; j++) {
                var scene = pathPrefix + "/FloorPlan_" + nameTemplate + i + "_" + j + ".unity";
                scenes.Add(scene);
            }
        }
        return scenes;
    }

    private static List<string> GetSceneNames(int startIndex, int lastIndex, string nameTemplate = "", string pathPrefix = "Assets/Scenes") {
        var scenes = new List<string>();
        for (var i = startIndex; i <= lastIndex; i++) {

            var scene = pathPrefix + "/FloorPlan" + nameTemplate + i + "_physics.unity";
            scenes.Add(scene);

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
        //EditorSceneManager.OpenScene(sceneName);
        SetNavMeshNotWalkable(GameObject.Find("Objects"));
        SetNavMeshNotWalkable(GameObject.Find("Structure"));
        SetNavMeshWalkable(SearchForSimObjectType(SimObjType.Floor, GameObject.Find("Objects")));
        //SetNavMeshWalkable(GameObject.Find("Objects").transform.FirstChildOrDefault(x => x.name.Contains("Floor")).gameObject);

        // var floorStruct = GameObject.Find("Structure").transform.FirstChildOrDefault(x => x.name.Contains("Decals"));
        // if (floorStruct != null) {
        //     SetNavMeshWalkable(floorStruct.gameObject);
        // }

        var agentController = FindObjectOfType<BaseAgentComponent>();
        // var capsuleCollider = agentController.GetComponent<CapsuleCollider>();
        var navmeshAgent = agentController.GetComponentInChildren<NavMeshAgent>();
        navmeshAgent.enabled = true;
        // The Editor bake interface does not take with parameters and could not be modified as of 2018.3
        //var buildSettings = 
        new NavMeshBuildSettings() {
            agentTypeID = navmeshAgent.agentTypeID,
            agentRadius = 0.2f,
            agentHeight = 1.8f,
            agentSlope = 10,
            agentClimb = 0.5f,
            minRegionArea = 0.05f,
            overrideVoxelSize = false,
            overrideTileSize = false
        };

        UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    public static void SetNavMeshNotWalkable(GameObject hierarchy) {

        for (int i = 0; i < hierarchy.transform.childCount; i++) {
            var child = hierarchy.transform.GetChild(i);
            child.GetComponentsInChildren<MeshRenderer>().ToList().ForEach(meshRenderer => {
                Debug.Log("Mesh Renderer " + meshRenderer.gameObject.name + " layer ");
                UnityEditor.GameObjectUtility.SetStaticEditorFlags(meshRenderer.gameObject, UnityEditor.StaticEditorFlags.NavigationStatic);
                UnityEditor.GameObjectUtility.SetNavMeshArea(meshRenderer.gameObject, NavMesh.GetAreaFromName("Not Walkable"));
            });
            //Debug.Log("Setting flag for " + child.gameObject.name + " layer " + NavMesh.GetAreaFromName("Not Walkable"));
        }
    }

    private static void SetNavMeshWalkable(GameObject hierarchy) {

        //  var objectHierarchy = hirerarchy.transform.FirstChildOrDefault(x => x.name.Contains("Floor"));
        hierarchy.GetComponentsInChildren<MeshRenderer>().ToList().ForEach(meshRenderer => {
            Debug.Log("Mesh Renderer " + meshRenderer.gameObject.name + " layer ");
            UnityEditor.GameObjectUtility.SetStaticEditorFlags(meshRenderer.gameObject, UnityEditor.StaticEditorFlags.NavigationStatic);
            UnityEditor.GameObjectUtility.SetNavMeshArea(meshRenderer.gameObject, NavMesh.GetAreaFromName("Walkable"));
        });
    }

    private static GameObject SearchForSimObjectType(SimObjType sot, GameObject hierarchy) {
        GameObject go = null;

        hierarchy.GetComponentsInChildren<SimObjPhysics>().ToList().ForEach(sop => {
            if(sop.ObjType == sot)
            go = sop.gameObject;
        });

        return go;
    }
#endif
}