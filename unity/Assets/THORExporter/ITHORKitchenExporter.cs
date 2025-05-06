using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ITHORKitchenExporter : MonoBehaviour
{
    [Header("Path to iTHOR scene folder")]
    public string sceneDirectory = "Assets/Scenes";


    //[Header("FloorPlan Dataset Split. (empty)/Train/Val")]
    //public int sceneSplit = "train";


    [Header("FloorPlan index id. Empty cell will export all")]
    public int floorplanID = -1;


    private Scene currentScene;

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

        // for loop 
        foreach (var scenePath in scenePaths)
        {
            // LoadAndExportScene(scenePath);
            StartCoroutine(LoadAndExportSceneAsync(scenePath));
        }   
    }

    IEnumerator LoadAndExportSceneAsync(string sceneName)
    {
        Debug.Log($"Loading scene {sceneName}");
        
        var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        var newScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gameObject, newScene);
        UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(currentScene);
    }

    void LoadAndExportScene(string sceneName)
    {
        Debug.Log($"Loading scene {sceneName}");

        var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
        // UnityEngine.SceneManagement.SceneManager.UnloadScene(currentScene);
        var newScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(gameObject, newScene);
    }

    void Update() {
        var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        Debug.Log($"Current Scene Name: {currentScene.name}");
        // var gameObjects = currentScene.GetRootGameObjects();
        // Debug.Log("fuuuuuuuuu");
    }
}
