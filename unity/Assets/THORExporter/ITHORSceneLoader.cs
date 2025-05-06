using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ITHORSceneLoader : MonoBehaviour
{
    [Header("Path to iTHOR scene folder")]
    public string sceneDirectory = "Assets/Scenes";


    //[Header("FloorPlan Dataset Split. (empty)/Train/Val")]
    //public int sceneSplit = "train";


    [Header("FloorPlan index id. Empty cell will export all")]
    public int floorplanID = -1;


    private Scene? previousScene = null;  // Add this field at class level

    private string scenePath = null;

    // Start is called before the first frame update
    void Start()
    {
        // var assetBundle = AssetBundle.LoadFromFile(sceneDirectory);
        // get a list of scenePath 
        var scenePaths = new List<string>();
        if (floorplanID == -1)
        {
            var scenePathCandidates = Directory.GetFiles(sceneDirectory);
            // var scenePathCandidates = assetBundle.GetAllScenePaths();
            string pattern = @".*FloorPlan[1-9][0-9]*_physics.*\.unity$";
            foreach (var pathCandidate in scenePathCandidates)
            {
                bool isMatch = Regex.IsMatch(pathCandidate, pattern);
                if (isMatch)
                {
                    scenePaths.Add(pathCandidate);
                }
            }
        }
        else{
            // scenePaths.Add($"{sceneDirectory}/FloorPlan{floorplanID}_physics");
            scenePaths.Add($"Scenes/FloorPlan{floorplanID}_physics");
        }

        StartCoroutine(ProcessAllKitchenScenes(scenePaths));
    }

    void Awake()
    {
        // Make sure this object persists when loading new scenes
        DontDestroyOnLoad(gameObject);
    }

    IEnumerator ProcessAllKitchenScenes(List<string> scenePaths)
    {
        foreach (string sceneName in scenePaths)
        {
            Debug.Log($"Processing kitchen scene: {sceneName}");
            
            // Load and process each kitchen scene
            yield return StartCoroutine(LoadAndExportSceneAsync(sceneName));
            
            Debug.Log($"Finished processing {sceneName}");
        }
        
        Debug.Log("Finished processing all kitchen scenes");
    }

    IEnumerator LoadAndExportSceneAsync(string sceneName)
    {
        Debug.Log($"Loading scene {sceneName}");
        
        // Load the new scene in Single mode (this automatically unloads current scene)
        var asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Get reference to newly loaded scene
        var newScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        Debug.Log($"Successfully loaded scene: {newScene.name}");

        // Create a new empty GameObject and add the IThorHouseExporter component
        var houseExporterObj = new GameObject("ITHORSceneExporter");
        var houseExporter = houseExporterObj.AddComponent<ITHORSceneExporter>();
        
        // Move the new GameObject to the loaded scene
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(houseExporterObj, newScene);
        
        Debug.Log($"Created new ITHORSceneExporter in scene: {newScene.name}");

        // Debug print the number of root GameObjects in the scene
        var rootObjects = newScene.GetRootGameObjects();
        Debug.Log($"Number of root GameObjects in {sceneName}: {rootObjects.Length}");

        // Print info about each root object
        foreach (var obj in rootObjects)
        {
            Debug.Log($"Root object: {obj.name}");
            Debug.Log($"Number of children in {obj.name}: {obj.transform.childCount}");
        }
    }

    // Add your scene processing methods here
    IEnumerator ProcessCurrentScene()
    {
        // Add your logic to process the current kitchen scene
        yield return null;
    }

    void Update() 
    {
        // Remove this method if you don't need continuous scene name printing
        // Or keep it if you find it useful for debugging
    }
}
