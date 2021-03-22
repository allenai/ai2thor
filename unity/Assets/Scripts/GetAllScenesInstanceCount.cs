using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

// [Serializable]
// public class TotalSimObjectsInScene
// {
//     //count of total number of sim objects in this scene
//     public int TotalSimObjCountInScene;
//     //track the count of each object type that exists in this scene
//     public Dictionary<SimObjType, int> objectTypeCountInScene = new Dictionary<SimObjType, int>();
// }

public class GetAllScenesInstanceCount : MonoBehaviour
{
    // [MenuItem("GameObject/Test")]
    // private static void TestGetPrefabInstanceHandle()
    // {
    //     // Keep track of the currently selected GameObject(s)
    //     GameObject[] objectArray = Selection.gameObjects;

    //     // Loop through every GameObject in the array above
    //     foreach (GameObject gameObject in objectArray)
    //     {
    //         print("prefab instance handle: " + PrefabUtility.GetPrefabInstanceHandle(gameObject));
    //         print("prefab asset type: " + PrefabUtility.GetPrefabAssetType(gameObject));
    //         print("original source: " + PrefabUtility.GetCorrespondingObjectFromOriginalSource(gameObject));
    //     }
    // }

    // [MenuItem("SimObjectPhysics/Get All Scene Instances %j")]
    // private static void GetInstanceCount()
    // {
    //     //keep track of total number of sim objects across all scenes
    //     int totalInstanceCount = 0;
    //     //keep track of the count of each object type across all scenes
    //     Dictionary<SimObjType, int> objectTypeCounts = new Dictionary<SimObjType, int>();

    //     //Keep track of the total instance count and objectTypecount in individual scenes
    //     Dictionary<String, TotalSimObjectsInScene> objectTypeCountsByScene = new Dictionary<String, TotalSimObjectsInScene>();

    //     //Be sure to have the scenes you want to check for instances (and ONLY those scenes) int the build settings!
    //     //for each scene in the build do these things
    //     for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++)
    //     {
    //         UnityEditor.SceneManagement.EditorSceneManager.OpenScene(SceneUtility.GetScenePathByBuildIndex(i), OpenSceneMode.Single);
    //         var simObjects = FindObjectsOfType<SimObjPhysics>();

    //         //for every child object in "Objects" - simObjects
    //         totalInstanceCount = totalInstanceCount + simObjects.Length;

    //         //keep track of the count of each object type in this specific
    //         Dictionary<SimObjType, int> sceneObjectTypeCounts = new Dictionary<SimObjType, int>();

    //         foreach (SimObjPhysics currentSimObject in simObjects)
    //         {

    //             //keep track of total object type count
    //             if (objectTypeCounts.ContainsKey(currentSimObject.Type))
    //             {
    //                 objectTypeCounts[currentSimObject.Type]++;
    //             }

    //             else
    //             {
    //                 objectTypeCounts.Add(currentSimObject.Type, 1);
    //             }

    //             //keep track of object type count for this scene only
    //             if (sceneObjectTypeCounts.ContainsKey(currentSimObject.Type))
    //             {
    //                 sceneObjectTypeCounts[currentSimObject.Type]++;
    //             }

    //             else
    //             {
    //                 sceneObjectTypeCounts.Add(currentSimObject.Type, 1);
    //             }

    //         }

    //         string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
    //         if(!objectTypeCountsByScene.ContainsKey(currentSceneName))
    //         {
    //             TotalSimObjectsInScene tsois = new TotalSimObjectsInScene();
    //             tsois.TotalSimObjCountInScene = simObjects.Length;
    //             tsois.objectTypeCountInScene = sceneObjectTypeCounts;
    //             objectTypeCountsByScene.Add(currentSceneName, tsois);
    //         }

    //     }

    //     //this is how many types of objects appear across all scenes. This does not track the number of instances of each object type, only if the type itself exists.
    //     print("Total number of OBJECT TYPES that appear across ALL scenes: " + objectTypeCounts.Count);
    //     //this is the total number of sim objects across all scenes. this includes duplicate appearances of the same prefab instance.
    //     print("The total number of OBJECT INSTANCES across ALL scenes: " + totalInstanceCount);

    //     print("The following is the number of INSTANCES of each OBJECT TYPE that appears in ALL scenes:");
    //     //this tracks the total number of instances of each object type across all scenes
    //     foreach (KeyValuePair<SimObjType, int> typeSet in objectTypeCounts)
    //     {
    //         print(typeSet.Key + " | Total Count of Instances Across ALL Scenes: " + typeSet.Value);
    //     }

    //     print("/////////////////////////////////////////////////////////////////");

    //     //output per scene
    //     foreach(KeyValuePair<String, TotalSimObjectsInScene> entry in objectTypeCountsByScene)
    //     {
    //         print("/////////////////////////////////////////////////////////////////");
    //         print("Scene Name: " + entry.Key);
    //         print("Total Sim Objects in " + entry.Key + ": " + entry.Value.TotalSimObjCountInScene);
    //         foreach(KeyValuePair<SimObjType, int> pair in entry.Value.objectTypeCountInScene)
    //         {
    //             print(pair.Key + " | Total Instances In " + entry.Key + ": " + pair.Value);
    //         }
    //     }
    // }
}
#endif