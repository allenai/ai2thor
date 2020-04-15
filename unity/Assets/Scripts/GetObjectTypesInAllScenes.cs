using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

public class GetObjectTypesInAllScenes : MonoBehaviour
{
    [MenuItem("SimObjectPhysics/Generate Sim Obj Instance Count By Scene")]
    private static void GetInstanceCount()
    {
        int totalInstanceCount = 0;

        //keep track of which types appear in which room category, and a count of each
        Dictionary<SimObjType, int> kitchenCount = new Dictionary<SimObjType, int>();
        Dictionary<SimObjType, int> livingRoomCount = new Dictionary<SimObjType, int>();
        Dictionary<SimObjType, int> bedroomCount = new Dictionary<SimObjType, int>();
        Dictionary<SimObjType, int> bathroomCount = new Dictionary<SimObjType, int>();


        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++)
        {
            //Be sure to have the scenes you want to check for instances (and ONLY those scenes) int the build settings!
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(SceneUtility.GetScenePathByBuildIndex(i), OpenSceneMode.Single);

            //We'll need to expand this if objects ever have children objects of their own (ex: fork inside drawer)
            var simObjects = FindObjectsOfType<SimObjPhysics>();

            //for every child object in "Objects" - simObjects
            totalInstanceCount = totalInstanceCount + simObjects.Length;

            Dictionary<SimObjType, int> whichOne = new Dictionary<SimObjType, int>();

            //check which scene category we are in since they are listed in the Build settings in order...
            if(i <= 29)
            {
                whichOne = kitchenCount;
            }

            if(i > 29 && i < 60)
            {
                whichOne = livingRoomCount;
            }

            if(i > 59 && i < 90)
            {
                whichOne = bedroomCount;
            }

            if(i > 89 && i < 120)
            {
                whichOne = bathroomCount;
            }
            
            foreach (SimObjPhysics currentSimObject in simObjects)
            {

                //if the child object's type is not in the dictionary, add to the ditionary and give count of 1
                //if it is in the dictionary, increment the count
                if (whichOne.ContainsKey(currentSimObject.Type))
                {
                    whichOne[currentSimObject.Type]++;
                }

                else
                {
                    whichOne.Add(currentSimObject.Type, 1);
                }
            }
        }

        var file = "ObjectTypesInAllScenes.txt";
                
        // if(File.Exists("Assets/DebugTextFiles/" + file))
        // {
        //     //return;
        //     File.Open("Assets/DebugTextFiles/" + file, FileMode.Create);
        // }

        var create = File.CreateText("Assets/DebugTextFiles/" + file);
        //write stuff
        create.WriteLine("Kitchens-----------------------------------" + "\n");
        foreach (KeyValuePair<SimObjType, int> typeSet in kitchenCount)
        {
            create.WriteLine(typeSet.Key + " | Total Instances: " + typeSet.Value + "\n");
        }

        create.WriteLine("Living Rooms-----------------------------------" + "\n");
        foreach (KeyValuePair<SimObjType, int> typeSet in livingRoomCount)
        {
            create.WriteLine(typeSet.Key + " | Total Instances: " + typeSet.Value + "\n");
        }

        create.WriteLine("Bedrooms-----------------------------------" + "\n");
        foreach (KeyValuePair<SimObjType, int> typeSet in bedroomCount)
        {
            create.WriteLine(typeSet.Key + " | Total Instances: " + typeSet.Value + "\n");
        }

        create.WriteLine("Bathrooms-----------------------------------" + "\n");
        foreach (KeyValuePair<SimObjType, int> typeSet in bathroomCount)
        {
            create.WriteLine(typeSet.Key + " | Total Instances: " + typeSet.Value + "\n");
        }

        create.WriteLine("------------------------------------------------------");

        create.WriteLine("The total number of sim objects in all scenes is " + totalInstanceCount);

        create.Close();
    }

    [MenuItem("SimObjectPhysics/Export Placement Restrictions to Text File")]
    private static void ExportPlacementRestrictionsToTextFile()
    {
        var file = "PlacementRestrictions.txt";

        var create = File.CreateText("Assets/DebugTextFiles/" + file);
        
        foreach (KeyValuePair<SimObjType, List<SimObjType>> kvp in ReceptacleRestrictions.PlacementRestrictions)
        {
            create.WriteLine("PickupableObject: " + kvp.Key.ToString() + "\n");
            foreach(SimObjType sop in kvp.Value)
            {
                create.Write(sop.ToString() + ", ");
            }
            create.WriteLine("\n");
        }

        create.Close();
    }
}
#endif
