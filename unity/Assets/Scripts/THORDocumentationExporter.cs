using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Microsoft.Win32;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

public class THORDocumentationExporter : MonoBehaviour {
    // export the placement restrictions for each pickupable object
    [MenuItem("SimObjectPhysics/Generate Placement Restrictions to Text File")]
    private static void ExportPlacementRestrictionsToTextFile() {
        var file = "PlacementRestrictions.txt";

        var create = File.CreateText("Assets/DebugTextFiles/" + file);

        foreach (
            KeyValuePair<
                SimObjType,
                List<SimObjType>
            > kvp in ReceptacleRestrictions.PlacementRestrictions
        ) {
            // create.WriteLine("/////////////////////////////");
            create.WriteLine("Receptacle Restrictions for: " + kvp.Key.ToString());
            foreach (SimObjType sop in kvp.Value) {
                create.Write(sop.ToString() + ", ");
            }
            create.WriteLine("\n");
        }

        create.Close();
    }

    public class TotalSimObjectsInScene {
        // count of total number of sim objects in this scene
        public int TotalSimObjCountInScene;

        // track the count of each object type that exists in this scene
        public Dictionary<SimObjType, int> ObjectType_to_Count = new Dictionary<SimObjType, int>();
    }

    public class UniqueSimObjectsInScene {
        // name of the asset if it is a prefab- via PrefabUtility.GetCorrsepondingObjectFromOriginalSource()
        public string assetName = "n/a"; // default to n/a in the case this isn't a prefab

        // int count of the number of times this unique sim object appears across all scenes.
        public int count;

        // dict of scenes that this unique sim object appears in and what that object's scene name is within the scene
        // key is the scene name, value is the name of the sim object in heirarchy
        public Dictionary<String, String> Scenes_To_hName;
    }

    // print("original source: " + PrefabUtility.GetCorrespondingObjectFromOriginalSource(gameObject));

    public static readonly List<string> ArticulatedTypes = new List<string>
    {
        "BathroomSinkFaucet",
        "LightSwitch",
        "Toilet",
        "Book",
        "Dresser",
        "Safe",
        "ShelvingUnit",
        "SideTable",
        "Fridge",
        "Microwave",
        "Toaster",
        "CoffeeTable",
        "Desk",
        "Laptop",
        "Doorways",
        "LaundryHamper",
        "Cabinet",
        "Drawer",
        "Oven",
        "Dishwasher",
        "StoveKnob"
    };

    public static readonly List<string> PickupTypes = new List<string>
    {
        "AlarmClock",
        "AluminumFoil",
        "Apple",
        "AppleSliced",
        // "BaseballBat",
        "Book",
        "Boots",
        "Bottle",
        "Bowl",
        "Box",
        "Bread",
        "BreadSliced",
        "ButterKnife",
        "Candle",
        "CD",
        "CellPhone",
        "Cloth",
        "CreditCard",
        "Cup",
        "DishSponge",
        "Dumbbell",
        "Egg",
        "EggCracked",
        "Fork",
        "HandTowel",
        "Kettle",
        "KeyChain",
        "Knife",
        "Ladle",
        "Laptop",
        "Lettuce",
        "LettuceSliced",
        "Mug",
        "Newspaper",
        "Pan",
        "PaperTowelRoll",
        "Pen",
        "Pencil",
        "PepperShaker",
        "Pillow",
        "Plate",
        "Plunger",
        "Pot",
        "Potato",
        "PotatoSliced",
        "RemoteControl",
        "SaltShaker",
        // "Sandwich", #rose: i think this should be pickupable
        "ScrubBrush",
        "SoapBar",
        "SoapBottle",
        "Spatula",
        "Spoon",
        "SprayBottle",
        "Statue",
        "TableTopDecor",
        "TeddyBear",
        "TennisRacket",
        "TissueBox",
        "ToiletPaper",
        "Tomato",
        "TomatoSliced",
        "Towel",
        "Vase",
        "Watch",
        "WateringCan",
        "WineBottle",
    };

    [MenuItem("SimObjectPhysics/Generate Sim Obj Instance Count Text Files")]
    private static void GetInstanceCount() {
        // keep track of total number of sim objects across all scenes
        int totalInstanceCount = 0;

        // keep track of the count of each object type across all scenes
        Dictionary<SimObjType, int> ObjectTypeInAllScenes_to_Count =
            new Dictionary<SimObjType, int>();

        // keep track of which object types exist in which scene
        Dictionary<SimObjType, List<String>> ObjectType_To_Scenes =
            new Dictionary<SimObjType, List<String>>();

        // Keep track of the total instance count and object type: count in individual scenes
        Dictionary<String, TotalSimObjectsInScene> SceneName_to_Counts =
            new Dictionary<String, TotalSimObjectsInScene>();

        // keep track of articulation-type objects for each scene
        Dictionary<String, int> ArticulationObjectsByScene =
            new Dictionary<String, int>();

        // Keep track of pickup-type objects for this scene only
        Dictionary<String, int> PickupObjectsByScene =
            new Dictionary<String, int>();

        // Keep track of "reachable" pickup-type objects
        Dictionary<String, int> ReachablePickupObjectsByScene =
            new Dictionary<String, int>();

        // Keep track of total number of articulated components
        Dictionary<String, int> ArticulatedComponentsByScene =
            new Dictionary<String, int>();

        // Keep track of "reachable" articulated components
        Dictionary<String, int> ReachableArticulationObjectsByScene =
            new Dictionary<String, int>();

        // track the number of times a Unique prefab shows up across all scenes.
        // ie: Pillow_1 might show up in scene 1, scene 2, scene 3, so total 3 duplicates of this instance
        Dictionary<GameObject, UniqueSimObjectsInScene> UniquePrefab_to_Count =
            new Dictionary<GameObject, UniqueSimObjectsInScene>();

        // Be sure to have the scenes you want to check for instances (and ONLY those scenes) in the build settings!
        // for each scene in the build list, run the following
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++) {
            // open up individual scene
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
                SceneUtility.GetScenePathByBuildIndex(i),
                OpenSceneMode.Single
            );

            // Creates a dictionary 
            var simObjects = FindObjectsOfType<SimObjPhysics>();

            // So...do I go through each simobject now and run the test on it, checking what type and reachability it is???
            // for every child object in "Objects" - simObjects, add them to the count
            totalInstanceCount = totalInstanceCount + simObjects.Length;

            // keep track of the count of each object-type in this specific scene
            Dictionary<SimObjType, int> sceneObjectTypeCounts = new Dictionary<SimObjType, int>();

            // Get current scene name
            string currentSceneName = UnityEngine
                .SceneManagement.SceneManager.GetActiveScene()
                .name;

            // If the dictionary does NOT already have an entry keyed by currentSceneName...
            if (!SceneName_to_Counts.ContainsKey(currentSceneName)) {
                // Declare a new statistics container, which stores both the total number and number-by-type
                TotalSimObjectsInScene tsois = new TotalSimObjectsInScene();
                // Store raw number of SimObjects in TotalSimObjCountInScene
                tsois.TotalSimObjCountInScene = simObjects.Length;
                // Set tsois.ObjectTypes_to_Count to inherit all imminent changes to sceneObjectTypeCounts
                tsois.ObjectType_to_Count = sceneObjectTypeCounts;
                // Add current scene as key to SceneName_to_Counts, and to include new values coming imminently
                SceneName_to_Counts.Add(currentSceneName, tsois);
            }

            // Add current scene as key to each object-type counter dictionary
            if (!ArticulationObjectsByScene.ContainsKey(currentSceneName)) {
                ArticulationObjectsByScene.Add(currentSceneName, 0);
            }
            if (!PickupObjectsByScene.ContainsKey(currentSceneName)) {
                PickupObjectsByScene.Add(currentSceneName, 0);
            }
            if (!ArticulatedComponentsByScene.ContainsKey(currentSceneName)) {
                ArticulatedComponentsByScene.Add(currentSceneName, 0);
            }

            // For every single SimObject in the current scene (THIS IS WHERE YOU CAN ACTUALL ADD YOUR EXTRA CHECKS!!!)...
            foreach (SimObjPhysics currentSimObject in simObjects) {
                // keep track of object-count by type, across ALL scenes
                if (ObjectTypeInAllScenes_to_Count.ContainsKey(currentSimObject.Type)) {
                    ObjectTypeInAllScenes_to_Count[currentSimObject.Type]++;
                } else {
                    ObjectTypeInAllScenes_to_Count.Add(currentSimObject.Type, 1);
                }

                // keep track of object-count by type, for THIS scene only
                if (sceneObjectTypeCounts.ContainsKey(currentSimObject.Type)) {
                    sceneObjectTypeCounts[currentSimObject.Type]++;
                } else {
                    sceneObjectTypeCounts.Add(currentSimObject.Type, 1);
                }

                // keep track of which scenes contain the current object-type

                // key already exists, don't worry about creating new list
                if (ObjectType_To_Scenes.ContainsKey(currentSimObject.Type)) {
                    // Check if scene is already in list of included scenes for this SimObject-type
                    // If not, add it. If so, ignore it.
                    if (!ObjectType_To_Scenes[currentSimObject.Type].Contains(currentSceneName)) {
                        ObjectType_To_Scenes[currentSimObject.Type].Add(currentSceneName);
                    }
                    // key does NOT exist (ideally because this is the first time the SimObject
                    // has been encountered), make a new one
                } else {
                    // Create new list, populate it with current scene, and assign it as initial value
                    // for SimObjectType-key
                    List<String> listOfScenes = new List<String>();
                    listOfScenes.Add(currentSceneName);
                    ObjectType_To_Scenes.Add(currentSimObject.Type, listOfScenes);
                }

                // keep track of unique prefab instances across all scenes

                // check if this object is a prefab
                // if so, store that prefab as one we have encountered already
                // if this is not a prefab, assume that it is unique to this scene
                GameObject testObj = PrefabUtility.GetCorrespondingObjectFromOriginalSource(
                    currentSimObject.gameObject
                );

                if (testObj != null) {
                    // this prefab already exists in our dictionary
                    if (UniquePrefab_to_Count.ContainsKey(testObj)) {
                        // increment how many times this prefab has shown up
                        UniquePrefab_to_Count[testObj].count++;
                        if (
                            !UniquePrefab_to_Count[testObj]
                                .Scenes_To_hName.ContainsKey(currentSceneName)
                        ) {
                            // add any scenes where this prefab shows up
                            UniquePrefab_to_Count[testObj]
                                .Scenes_To_hName.Add(
                                    currentSceneName,
                                    currentSimObject.gameObject.name
                                );
                        }
                    } else {
                        UniqueSimObjectsInScene usois = new UniqueSimObjectsInScene();
                        usois.count = 1;
                        usois.Scenes_To_hName = new Dictionary<String, String>();
                        usois.Scenes_To_hName.Add(
                            currentSceneName,
                            currentSimObject.gameObject.name
                        );
                        usois.assetName = "" + testObj; // this allows me to conver the testObj gameobject into a string so yeah i guess?
                        UniquePrefab_to_Count.Add(testObj, usois);
                    }
                }
                // this sim object is not a prefab so assume it is unique to this scene only
                else {
                    UniqueSimObjectsInScene usois = new UniqueSimObjectsInScene();
                    usois.count = 1;
                    usois.Scenes_To_hName = new Dictionary<String, String>();
                    usois.Scenes_To_hName.Add(currentSceneName, currentSimObject.gameObject.name);
                    // use default usois.assetName since this isn't a prefab
                    UniquePrefab_to_Count.Add(currentSimObject.gameObject, usois);
                }

                // // Single-scene debug output test
                // string testScene = "FloorPlan1_physics";
                // if (currentSceneName.ToString() == testScene) {
                //     Debug.Log("Checking " + currentSimObject);
                // }

                // keep track of articulation-type objects for each scene
                // IF objects' SimObject-type match ANY of the terms in the ArticulatedTypes list, we're in busines
                if (ArticulatedTypes.Contains(currentSimObject.Type.ToString())) {
                    // Add to articulatedtype object-count for this scene's value
                    ArticulationObjectsByScene[currentSceneName.ToString()]++;

                    // Count up the number of articulated parts
                    int articulatedComponetnsCount = 0;
                    if (currentSimObject.gameObject.GetComponent<CanOpen_Object>() != null) {
                        articulatedComponetnsCount = currentSimObject.gameObject.GetComponent<CanOpen_Object>().MovingParts?.Length ?? 0;
                        ArticulatedComponentsByScene[currentSceneName.ToString()] += articulatedComponetnsCount;
                        // if (currentSceneName.ToString() == testScene) {
                        //     Debug.Log(currentSimObject.gameObject.name + " HAS CAN_OPEN_OBJECT, AND I'VE ADDED THIS MANY ACs: " + articulatedComponetnsCount);
                        // }
                    } else if (currentSimObject.gameObject.GetComponent<CanToggleOnOff>() != null) {
                        articulatedComponetnsCount = currentSimObject.gameObject.GetComponent<CanToggleOnOff>().MovingParts?.Length ?? 0;
                        ArticulatedComponentsByScene[currentSceneName.ToString()] += articulatedComponetnsCount;
                        // if (currentSceneName.ToString() == testScene) {
                        //     Debug.Log(currentSimObject.gameObject.name + " HAS CAN_TOGGLE_ON_OFF, AND I'VE ADDED THIS MANY ACs: " + articulatedComponetnsCount);
                        // }
                    } else {
                        // if (currentSceneName.ToString() == testScene) {
                        //     Debug.Log(currentSimObject.gameObject.name + " HAS NOTING!!!!!!!");
                        // }
                    }
                } else if (PickupTypes.Contains(currentSimObject.Type.ToString())) {
                    // Add to pickuptype object-count for this scene's value
                    PickupObjectsByScene[currentSceneName.ToString()]++;
                }
            }
        }

        // generate text file for counts across ALL SCENES
        var file = "ObjectTypesInAllScenes.txt";
        var create = File.CreateText("Assets/DebugTextFiles/" + file);
        create.WriteLine(
            "Total number of OBJECT TYPES that apear across ALL Scenes: "
                + ObjectTypeInAllScenes_to_Count.Count
                + "\n"
        );
        create.WriteLine(
            "The total number of OBJECT INSTANCES across ALL scenes: " + totalInstanceCount + "\n"
        );
        create.WriteLine(
            "The following is the number of INSTANCES of each OBJECT TYPE that appears across ALL scenes:"
        );
        foreach (KeyValuePair<SimObjType, int> typeSet in ObjectTypeInAllScenes_to_Count) {
            create.WriteLine(typeSet.Key + ": " + typeSet.Value);
        }
        create.Close();

        // generate text file for counts divided up by INDIVIDUAL SCENES
        var file2 = "ObjectTypesPerScene.txt";
        var create2 = File.CreateText("Assets/DebugTextFiles/" + file2);
        create2.WriteLine("The following is the Total count of Sim Objects by Scene");
        foreach (KeyValuePair<String, TotalSimObjectsInScene> entry in SceneName_to_Counts) {
            create2.WriteLine("\n" + "Scene Name: " + entry.Key);
            // key: scene, value: object with total count of instances in scene, count of each object by type in scene
            create2.WriteLine(
                "Total Number of Sim Objects in "
                    + entry.Key
                    + ": "
                    + entry.Value.TotalSimObjCountInScene
            );
            foreach (KeyValuePair<SimObjType, int> pair in entry.Value.ObjectType_to_Count) {
                create2.WriteLine(
                    pair.Key + " | Total Instances In " + entry.Key + ": " + pair.Value
                );
            }
        }
        create2.Close();

        // generate a text file for which scenes each object type can be found in
        // object type: scenes where there is at least 1 instance of this object type
        var file3 = "ScenesThatContainObjectType.txt";
        var create3 = File.CreateText("Assets/DebugTextFiles/" + file3);
        create3.WriteLine(
            "This contains a list of all Object Types and the Scenes which have at least one instance of the Object Type."
        );
        foreach (KeyValuePair<SimObjType, List<String>> typeToScene in ObjectType_To_Scenes) {
            create3.WriteLine("\n" + typeToScene.Key + ":");
            foreach (string s in typeToScene.Value) {
                create3.WriteLine(s);
            }
        }
        create3.Close();

        // generate a text file for the total count of unique sim object instances across all scenes
        // this file includes a breakdown of each object instance and how many times it is duplicated across all scenes
        var file4 = "UniqueObjectInstances.txt";
        var create4 = File.CreateText("Assets/DebugTextFiles/" + file4);
        create4.WriteLine(
            "This includes a count of the total number of unique sim objects that exist across all scenes"
        );
        create4.WriteLine(
            "Afterwards is a breakdown of how many times a unique sim object is re-used across all scenes"
        );
        create4.WriteLine(
            "NOTE: if prefab name is n/a, it is not a prefab but is instead a game object unique to some scene)"
        );

        create4.WriteLine(
            "\nTotal number of UNIQUE Sim Object instances: " + UniquePrefab_to_Count.Count
        );
        foreach (KeyValuePair<GameObject, UniqueSimObjectsInScene> p in UniquePrefab_to_Count) {
            create4.WriteLine("\nbase prefab name (in assets): " + p.Value.assetName);
            create4.WriteLine(
                "number of times this object shows up across all Scenes: " + p.Value.count
            );
            create4.WriteLine("list of scenes that this unique object shows up in: ");
            foreach (KeyValuePair<String, String> s in p.Value.Scenes_To_hName) {
                create4.WriteLine(
                    s.Key + " | name of instance of this object in scene: " + s.Value
                );
            }
        }
        create4.Close();

        // Add up totals for articulatedObjects, pickupObjects, and articulatedComponents
        int totalArticulationObjects = 0;
        foreach (int val in ArticulationObjectsByScene.Values) {
            totalArticulationObjects += val;
        }
        int totalPickupObjects = 0;
        foreach (int val in PickupObjectsByScene.Values) {
            totalPickupObjects += val;
        }
        int totalArticulatedComponents = 0;
        foreach (int val in ArticulatedComponentsByScene.Values) {
            totalArticulatedComponents += val;
        }

        var file5 = "ArticulationAndPickupTypeObjectCounts.txt";
        var create5 = File.CreateText("Assets/DebugTextFiles/" + file5);

        // articulation-type objects
        create5.WriteLine(
            "Articulation-type Objects: " +
            "\n" +
            "TOTAL: " + totalArticulationObjects
        );
        foreach (KeyValuePair<string, int> typeSet in ArticulationObjectsByScene) {
            create5.WriteLine(typeSet.Key + ": " + typeSet.Value);
        }

        // pickupable-type objects
        create5.WriteLine(
            "\n" +
            "Pickupable-type Objects" +
            "\n" +
            "TOTAL: " + totalArticulationObjects
        );
        foreach (KeyValuePair<string, int> typeSet in PickupObjectsByScene) {
            create5.WriteLine(typeSet.Key + ": " + typeSet.Value);
        }

        // articulated components
        create5.WriteLine(
            "\n" +
            "Articulated Components" +
            "\n" +
            "TOTAL: " + totalArticulatedComponents
        );
        foreach (KeyValuePair<string, int> typeSet in ArticulatedComponentsByScene) {
            create5.WriteLine(typeSet.Key + ": " + typeSet.Value);
        }

        create5.Close();
    }
}
#endif
