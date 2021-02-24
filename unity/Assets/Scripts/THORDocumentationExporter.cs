using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

public class THORDocumentationExporter : MonoBehaviour
{

    //export the placement restrictions for each pickupable object
    [MenuItem("SimObjectPhysics/Generate Placement Restrictions to Text File")]
    private static void ExportPlacementRestrictionsToTextFile()
    {
        var file = "PlacementRestrictions.txt";

        var create = File.CreateText("Assets/DebugTextFiles/" + file);
        
        foreach (KeyValuePair<SimObjType, List<SimObjType>> kvp in ReceptacleRestrictions.PlacementRestrictions)
        {
            //create.WriteLine("/////////////////////////////");
            create.WriteLine("Receptacle Restrictions for: " + kvp.Key.ToString());
            foreach(SimObjType sop in kvp.Value)
            {
                create.Write(sop.ToString() + ", ");
            }
            create.WriteLine("\n");
        }

        create.Close();
    }

    public class TotalSimObjectsInScene
    {
        //count of total number of sim objects in this scene
        public int TotalSimObjCountInScene;
        //track the count of each object type that exists in this scene
        public Dictionary<SimObjType, int> ObjectType_to_Count = new Dictionary<SimObjType, int>();
    }

    public class UniqueSimObjectsInScene
    {
        //name of the asset if it is a prefab- via PrefabUtility.GetCorrsepondingObjectFromOriginalSource()
        public string assetName = "n/a";//default to n/a in the case this isn't a prefab

        //int count of the number of times this unique sim object appears across all scenes.
        public int count;
        //dict of scenes that this unique sim object appears in and what that object's scene name is within the scene
        //key is the scene name, value is the name of the sim object in heirarchy
        public Dictionary<String, String> Scenes_To_hName;
    }

    //print("original source: " + PrefabUtility.GetCorrespondingObjectFromOriginalSource(gameObject));

    [MenuItem("SimObjectPhysics/Generate Sim Obj Instance Count Text Files")]
    private static void GetInstanceCount()
    {
        //keep track of total number of sim objects across all scenes
        int totalInstanceCount = 0;

        //keep track of the count of each object type across all scenes
        Dictionary<SimObjType, int> ObjectTypeInAllScenes_to_Count = new Dictionary<SimObjType, int>();

        //keep track of which object types exist in which scene
        Dictionary<SimObjType, List<String>> ObjectType_To_Scenes = new Dictionary<SimObjType, List<String>>(); 

        //Keep track of the total instance count and oobject type: count in individual scenes
        Dictionary<String, TotalSimObjectsInScene> SceneName_to_Counts = new Dictionary<String, TotalSimObjectsInScene>();

        //track the number of times a Unique prefab shows up across all scenes.
        //ie: Pillow_1 might show up in scene 1, scene 2, scene 3, so total 3 duplicates of this instance
        Dictionary<GameObject, UniqueSimObjectsInScene> UniquePrefab_to_Count = new Dictionary <GameObject, UniqueSimObjectsInScene>();

        //Be sure to have the scenes you want to check for instances (and ONLY those scenes) int the build settings!
        //for each scene in the build do these things
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++)
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(SceneUtility.GetScenePathByBuildIndex(i), OpenSceneMode.Single);
            var simObjects = FindObjectsOfType<SimObjPhysics>();

            //for every child object in "Objects" - simObjects
            totalInstanceCount = totalInstanceCount + simObjects.Length;

            //keep track of the count of each object type in this specific
            Dictionary<SimObjType, int> sceneObjectTypeCounts = new Dictionary<SimObjType, int>();


            string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if(!SceneName_to_Counts.ContainsKey(currentSceneName))
            {
                TotalSimObjectsInScene tsois = new TotalSimObjectsInScene();
                tsois.TotalSimObjCountInScene = simObjects.Length;
                tsois.ObjectType_to_Count = sceneObjectTypeCounts;
                SceneName_to_Counts.Add(currentSceneName, tsois);
            }

            foreach (SimObjPhysics currentSimObject in simObjects)
            {

                //keep track of total object type count
                if (ObjectTypeInAllScenes_to_Count.ContainsKey(currentSimObject.Type))
                {
                    ObjectTypeInAllScenes_to_Count[currentSimObject.Type]++;
                }
                else
                {
                    ObjectTypeInAllScenes_to_Count.Add(currentSimObject.Type, 1);
                }

                //keep track of object type count for this scene only
                if (sceneObjectTypeCounts.ContainsKey(currentSimObject.Type))
                {
                    sceneObjectTypeCounts[currentSimObject.Type]++;
                }
                else
                {
                    sceneObjectTypeCounts.Add(currentSimObject.Type, 1);
                }

                //keep track of which scenes contain this object type
                //key already exists, don't worry about creating new list
                if(ObjectType_To_Scenes.ContainsKey(currentSimObject.Type))
                {
                    if(!ObjectType_To_Scenes[currentSimObject.Type].Contains(currentSceneName))
                    ObjectType_To_Scenes[currentSimObject.Type].Add(currentSceneName);
                }
                else
                {
                    List<String> listOfScenes = new List<String>();
                    listOfScenes.Add(currentSceneName);
                    ObjectType_To_Scenes.Add(currentSimObject.Type, listOfScenes);
                }

                //keep track of unique prefab instances across all scenes
                
                //check if this object is a prefab
                //if so, store that prefab as one we have encountered already
                //if this is not a prefab, assume that it is unique to this scene
                GameObject testObj = PrefabUtility.GetCorrespondingObjectFromOriginalSource(currentSimObject.gameObject);

                if(testObj != null)
                {
                    //this prefab already exists in our dictionary
                    if(UniquePrefab_to_Count.ContainsKey(testObj))
                    {
                        //increment how many times this prefab has shown up
                        UniquePrefab_to_Count[testObj].count++;
                        if(!UniquePrefab_to_Count[testObj].Scenes_To_hName.ContainsKey(currentSceneName))
                        {
                            //add any scenes where this prefab shows up
                            UniquePrefab_to_Count[testObj].Scenes_To_hName.Add(currentSceneName, currentSimObject.gameObject.name);
                        }
                    }
                    else
                    {
                        UniqueSimObjectsInScene usois = new UniqueSimObjectsInScene();
                        usois.count = 1;
                        usois.Scenes_To_hName = new Dictionary<String, String>();
                        usois.Scenes_To_hName.Add(currentSceneName, currentSimObject.gameObject.name);
                        usois.assetName = "" + testObj;//this allows me to conver the testObj gameobject into a string so yeah i guess?
                        UniquePrefab_to_Count.Add(testObj, usois);
                    }
                }

                //this sim object is not a prefab so assume it is unique to this scene only
                else
                {
                    UniqueSimObjectsInScene usois = new UniqueSimObjectsInScene();
                    usois.count = 1;
                    usois.Scenes_To_hName = new Dictionary<String, String>();
                    usois.Scenes_To_hName.Add(currentSceneName, currentSimObject.gameObject.name);
                    //use default usois.assetName since this isn't a prefab
                    UniquePrefab_to_Count.Add(currentSimObject.gameObject, usois);
                }

            }

        }

        //generate text file for counts across ALL SCENES
        var file = "ObjectTypesInAllScenes.txt";
        var create = File.CreateText("Assets/DebugTextFiles/" + file);
        create.WriteLine("Total number of OBJECT TYPES that apear across ALL Scenes: " + ObjectTypeInAllScenes_to_Count.Count + "\n");
        create.WriteLine("The total number of OBJECT INSTANCES across ALL scenes: " + totalInstanceCount + "\n");
        create.WriteLine("The following is the number of INSTANCES of each OBJECT TYPE that appears across ALL scenes:");
        foreach (KeyValuePair<SimObjType, int> typeSet in ObjectTypeInAllScenes_to_Count)
        {
            create.WriteLine(typeSet.Key + ": " + typeSet.Value);
        }
        create.Close();

        //generate text file for counts divided up by INDIVIDUAL SCENES
        var file2 = "ObjectTypesPerScene.txt";
        var create2 = File.CreateText("Assets/DebugTextFiles/" + file2);
        create2.WriteLine("The following is the Total count of Sim Objects by Scene");
        foreach(KeyValuePair<String, TotalSimObjectsInScene> entry in SceneName_to_Counts)
        {
            create2.WriteLine("\n" + "Scene Name: " + entry.Key);
            //key: scene, value: object with total count of instances in scene, count of each object by type in scene
            create2.WriteLine("Total Number of Sim Objects in " + entry.Key + ": " + entry.Value.TotalSimObjCountInScene);
            foreach(KeyValuePair<SimObjType, int> pair in entry.Value.ObjectType_to_Count)
            {
                create2.WriteLine(pair.Key + " | Total Instances In " + entry.Key + ": " + pair.Value);
            }
        }
        create2.Close();

        //generate a text file for which scenes each object type can be found in
        //object type: scenes where there is at least 1 instance of this object type
        var file3 = "ScenesThatContainObjectType.txt";
        var create3 = File.CreateText("Assets/DebugTextFiles/" + file3);
        create3.WriteLine("This contains a list of all Object Types and the Scenes which have at least one instance of the Object Type.");
        foreach(KeyValuePair<SimObjType, List<String>> typeToScene in ObjectType_To_Scenes)
        {
            create3.WriteLine("\n" + typeToScene.Key + ":");
            foreach(string s in typeToScene.Value)
            {
                create3.WriteLine(s);
            }
        }
        create3.Close();

        //generate a text file for the total count of unique sim object instances across all scenes
        //this file includes a breakdown of each object instance and how many times it is duplicated across all scenes
        var file4 = "UniqueObjectInstances.txt";
        var create4 = File.CreateText("Assets/DebugTextFiles/" + file4);
        create4.WriteLine("This includes a count of the total number of unique sim objects that exist across all scenes");
        create4.WriteLine("Afterwards is a breakdown of how many times a unique sim object is re-used across all scenes");
        create4.WriteLine("NOTE: if prefab name is n/a, it is not a prefab but is instead a game object unique to some scene)");

        create4.WriteLine("\nTotal number of UNIQUE Sim Object instances: " + UniquePrefab_to_Count.Count);
        foreach(KeyValuePair <GameObject, UniqueSimObjectsInScene> p in UniquePrefab_to_Count)
        {
            create4.WriteLine("\nbase prefab name (in assets): " + p.Value.assetName);
            create4.WriteLine("number of times this object shows up across all Scenes: " + p.Value.count);
            create4.WriteLine("list of scenes that this unique object shows up in: ");
            foreach(KeyValuePair<String, String> s in p.Value.Scenes_To_hName)
            {
                create4.WriteLine(s.Key + " | name of instance of this object in scene: " + s.Value);
            }
        }
        create4.Close();
    }
}
#endif
