using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

public class GetAllScenesInstanceCount : MonoBehaviour
{
    [MenuItem("GameObject/Get All Scene Instances %j")]
    private static void GetInstanceCount()
    {
        int totalInstanceCount = 0;
        Dictionary<SimObjType, int> objectTypeCounts = new Dictionary<SimObjType, int>();

        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++)
        {
            //Be sure to have the scenes you want to check for instances (and ONLY those scenes) int the build settings!
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(SceneUtility.GetScenePathByBuildIndex(i), OpenSceneMode.Single);

            //We'll need to expand this if objects ever have children objects of their own (ex: fork inside drawer)
            var simObjects = FindObjectsOfType<SimObjPhysics>();

            //for every child object in "Objects" - simObjects
            totalInstanceCount = totalInstanceCount + simObjects.Length;

            foreach (SimObjPhysics currentSimObject in simObjects)
            {

                //if the child object's type is not in the dictionary, add to the ditionary and give count of 1
                //if it is in the dictionary, increment the count
                //print(currentSimObject.GetComponent<SimObjPhysics>().Type);
                if (objectTypeCounts.ContainsKey(currentSimObject.Type))
                {
                    objectTypeCounts[currentSimObject.Type]++;
                }

                else
                {
                    objectTypeCounts.Add(currentSimObject.Type, 1);
                }
            }
        }

        foreach (KeyValuePair<SimObjType, int> typeSet in objectTypeCounts)
        {
            print("Object Type: " + typeSet.Key + " | Total Instances: " + typeSet.Value);
        }

        print("The total number of objects is " + totalInstanceCount);
        //at the end, iterate through entire dictionary, and print every key and value (count)
    }
}
#endif