using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;
using System;

public class RoboThorJson : MonoBehaviour
{        
    [System.Serializable]
    class RoboThorObject
    {
        public string objectType;
        public string assetId;
        public Vector3 position;
        public Vector3 rotation;
        public bool kinematic;
    }

    [System.Serializable]
    class RoboThorRoom
    {
        public List<RoboThorObject> objects;
        public List<RoboThorObject> rooms; //TODO
        public List<RoboThorObject> walls;
        public List<RoboThorObject> doors;
        public List<RoboThorObject> windows;
        public RoboThorObject proceduralParameters;
        public RoboThorObject metadata;
       
    }

    public string save_filename = "RoboTHOR_objects.json";

    void Start()
    {
        string savePath = "Assets";
        
        // Create a new JSON object
        RoboThorRoom room = new RoboThorRoom();
        room.objects = GetObjects();


        // Save the JSON object to a file
        string json = JsonUtility.ToJson(room, true);
        File.WriteAllText(Path.Combine(savePath, save_filename), json);
        Debug.Log("saved to " + Path.Combine(savePath, save_filename));
    }


    void GetRooms()
    {
        // floor polygons
    }

    List<RoboThorObject> GetObjects()
    {
        // objects
        List<RoboThorObject> list_objects = new List<RoboThorObject>(); 

        // wall panels
        GameObject structure = GameObject.Find("Structure");
        foreach (Transform child in structure.transform)
        {
            if (child.tag =="Structure")
            {
            RoboThorObject obj = new RoboThorObject();

                // get object name
                obj.objectType = "WallPanel";
                obj.assetId = "RoboTHOR_wall_panel_32_5";

                // position
                Vector3 position = child.localPosition;
                obj.position = position;

                // rotation
                Vector3 rotation = child.localEulerAngles;
                obj.rotation = rotation;
                
                // kinematic
                bool isStatic = true;
                obj.kinematic = isStatic;

                list_objects.Add(obj);
            }
        }


        Transform parent = GameObject.Find("Objects").transform;
        for(int i=0; i<parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            RoboThorObject obj = new RoboThorObject();

            // get object name
            string objectType = Enum.GetName(typeof(SimObjType), child.GetComponent<SimObjPhysics>().Type); //folder name
            obj.objectType = objectType;

            string assetId = child.GetComponent<SimObjPhysics>().assetID; //filename
            obj.assetId = assetId;

            // position
            Vector3 position = child.localPosition;
            Transform bbox = child.Find("BoundingBox");
            if (bbox != null)
            {
                Vector3 bbox_center = bbox.GetComponent<BoxCollider>().center;
                obj.position = position + bbox_center;
            }
            else
                obj.position = position;

            // rotation
            Vector3 rotation = child.localEulerAngles;
            obj.rotation = rotation;
            
            // kinematic
            bool isStatic = child.GetComponent<SimObjPhysics>().isStatic;
            obj.kinematic = isStatic;

            list_objects.Add(obj);
        }
        return list_objects;
    }

    void GetDoors()
    {

    }

    void GetWindows()
    {

    }

    void GetWalls()
    {

    }

    void GetProceduralParams()
    {
        // floorColliderThickness

        // lights

        // reflections 

        // ceilingMaterial

        // skyboXId
    }

    void GetLights()
    {
        // id

        // position

        // rotation

        // shadow

        // type

        // intensity

        // indirectMultiplier

        // rgb
    }
    void GetMetadata()
    {
        /*From the menu bar, click Window > Rendering > Lighting Settings.
        In the window that appears, click the Environment tab.
        Assign the skybox Material to the Skybox Material property.
        */
    }
}