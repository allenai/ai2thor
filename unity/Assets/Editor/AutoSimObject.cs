using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using Siccity.GLTFUtility;


public class AutoSimObject : EditorWindow {
  [MenuItem("AI2-THOR/Load GLB")]
  static void LoadGLB() {
    GameObject result = Importer.LoadFromFile("Assets/Prefabs/GoogleScannedObjects/obj2.glb");
    Debug.Log(result);
  }

  [MenuItem("AI2-THOR/Make Sim Object")]
  static void MakeSimObject() {
    // string basePath = "Assets/Prefabs/ycb/";
    string basePath = "Assets/Prefabs/GoogleScannedObjects/";

    // get all folders in the basePath
    string[] folders = System.IO.Directory.GetDirectories(basePath);

    // create a prefab of each model in the folders
    foreach (string folder in folders) {
      string modelId = folder.Substring(folder.LastIndexOf("/") + 1);

      // load the annotations
      var annotationsStr = System.IO.File.ReadAllText(basePath + modelId + "/annotations.json");
      JObject annotations = JObject.Parse(annotationsStr);

      // instantiate the prefab
      var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + modelId + "/model.obj");
      GameObject obj = GameObject.Instantiate(prefab);
      obj.transform.rotation = Quaternion.identity;
      obj.name = modelId;

      // add a SimObjPhysics component
      SimObjPhysics simObj = obj.AddComponent<SimObjPhysics>();
      simObj.gameObject.layer = LayerMask.NameToLayer("SimObjVisible");
      simObj.assetID = modelId;
      string primaryProperty = annotations["primaryProperty"].ToString();
      if (primaryProperty != "") {
        simObj.PrimaryProperty = (SimObjPrimaryProperty)Enum.Parse(
          typeof(SimObjPrimaryProperty), annotations["primaryProperty"].ToString()
        );
      }

      // add the visibility points
      GameObject visPoints = new GameObject("visibilityPoints");
      visPoints.transform.parent = obj.transform;
      Transform[] visPointsTransforms = new Transform[annotations["visibilityPoints"].Count()];
      for (int i = 0; i < annotations["visibilityPoints"].Count(); i++) {
        GameObject visPoint = new GameObject("visibilityPoint" + i);
        visPoint.transform.parent = visPoints.transform;
        visPoint.transform.localPosition = new Vector3(
          (float)annotations["visibilityPoints"][i]["x"],
          (float)annotations["visibilityPoints"][i]["y"],
          (float)annotations["visibilityPoints"][i]["z"]
        );
        visPointsTransforms[i] = visPoint.transform;
        visPoint.layer = LayerMask.NameToLayer("SimObjVisible");
      }
      simObj.VisibilityPoints = visPointsTransforms;

      GameObject meshColliders = new GameObject("colliders");
      meshColliders.layer = LayerMask.NameToLayer("SimObjVisible");
      meshColliders.transform.parent = obj.transform;

      // get all the colliders
      string collidersPath = basePath + modelId + "/colliders/";
      string[] colliderPaths = System.IO.Directory.GetFiles(collidersPath);

      // add the colliders
      List<Collider> colliders = new List<Collider>();
      for (int i = 0; i < colliderPaths.Length; i++) {
        // skip meta files
        if (!colliderPaths[i].EndsWith(".obj")) {
          continue;
        }
        MeshCollider meshCollider = meshColliders.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(colliderPaths[i]);
        meshCollider.convex = true;
        colliders.Add(meshCollider);
      }
      simObj.MyColliders = colliders.ToArray();

      // add a RigidBody component
      Rigidbody rigidBody = obj.AddComponent<Rigidbody>();

      // rotate the internal components of the model. Note that you do not want to do this
      // at `obj` level as it should be at its cannonical orientation when at rotation 0,0,0.
      Quaternion rot = Quaternion.Euler(
        (float)annotations["transform"]["rotation"]["x"],
        (float)annotations["transform"]["rotation"]["y"],
        (float)annotations["transform"]["rotation"]["z"]
      );
      simObj.transform.Find("default").rotation = rot;
      visPoints.transform.rotation = rot;
      meshColliders.transform.rotation = rot;

      // set the transform scale
      obj.transform.localScale = new Vector3(
        (float)annotations["transform"]["scale"]["x"],
        (float)annotations["transform"]["scale"]["y"],
        (float)annotations["transform"]["scale"]["z"]
      );

      // Generate receptacle trigger boxes
      ReceptacleTriggerBoxEditor.TryToAddReceptacleTriggerBox(sop: simObj);
      simObj.SecondaryProperties = new SimObjSecondaryProperty[] { SimObjSecondaryProperty.Receptacle };

      // save obj as a prefab
      PrefabUtility.SaveAsPrefabAsset(obj, basePath + modelId + "/" + modelId + ".prefab");

      // delete the obj
      GameObject.DestroyImmediate(obj);
      Debug.Log("Saved " + modelId + " as a prefab");
    }
  }
}
