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
  [MenuItem("AI2-THOR/Load GLB Prefab")]
  static void LoadGLB() {
    string modelId = "B07B4MJZN1";
    string prefabPath = "Assets/Prefabs/abo/" + modelId + "/" + modelId + ".prefab";
    string glbModel = "Assets/Prefabs/abo/" + modelId + "/model.glb";

    // instantiate the prefab
    GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
    obj = Instantiate(obj);
    obj.name = modelId;

    // load the glb model
    GameObject result = Importer.LoadFromFile(glbModel);
    result.name = "mesh";
    result.transform.parent = obj.transform;
    result.transform.position = obj.transform.position;
    result.transform.rotation = obj.transform.rotation;

    // TODO: may have to mess with scale. Figure out why some glbs
    // aren't scaled to (1, 1, 1) by default.
    result.transform.localScale = new Vector3(1, 1, 1);
  }

  [MenuItem("AI2-THOR/Make Sim Object")]
  static void MakeSimObject() {
    string basePath = "Assets/Prefabs/abo/";

    // get all folders in the basePath
    string[] folders = System.IO.Directory.GetDirectories(basePath);

    // create a prefab of each model in the folders
    foreach (string folder in folders) {
      string modelId = folder.Substring(folder.LastIndexOf("/") + 1);

      // load the annotations
      var annotationsStr = System.IO.File.ReadAllText(basePath + modelId + "/annotations.json");
      JObject annotations = JObject.Parse(annotationsStr);

      GameObject obj;
      if (System.IO.File.Exists(basePath + modelId + "/model.glb")) {
        // load the glb file -- used with abo
        obj = Importer.LoadFromFile(basePath + modelId + "/model.glb");
      } else {
        // instantiate the obj file -- used with google scanned objects
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + modelId + "/model.obj");
        obj = GameObject.Instantiate(prefab);
      }

      // add the name
      obj.name = modelId;

      // add a SimObjPhysics component
      SimObjPhysics simObj = obj.AddComponent<SimObjPhysics>();
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
      }
      simObj.VisibilityPoints = visPointsTransforms;
      visPoints.transform.localScale = new Vector3(1, 1, 1);
      visPoints.transform.localPosition = new Vector3(0, 0, 0);

      GameObject meshColliders = new GameObject("colliders");
      meshColliders.transform.parent = obj.transform;

      // get all the colliders
      string collidersPath = basePath + modelId + "/colliders/";
      string[] colliderPaths = System.IO.Directory.GetFiles(collidersPath);

      // filter over colliders that end with obj
      List<string> colliderPathsFiltered = new List<string>();
      foreach (string colliderPath in colliderPaths) {
        if (colliderPath.EndsWith(".obj")) {
          colliderPathsFiltered.Add(colliderPath);
        }
      }

      // add the colliders
      Collider[] colliders = new Collider[colliderPathsFiltered.Count()];
      for (int i = 0; i < colliderPathsFiltered.Count(); i++) {
        // skip meta files
        MeshCollider meshCollider = meshColliders.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(colliderPaths[i]);
        meshCollider.convex = true;
        colliders[i] = meshCollider;
      }
      simObj.MyColliders = colliders;

      // add a RigidBody component
      Rigidbody rigidBody = obj.AddComponent<Rigidbody>();

      // set the transform rotation
      obj.transform.rotation = Quaternion.Euler(
        (float)annotations["transform"]["rotation"]["x"],
        (float)annotations["transform"]["rotation"]["y"],
        (float)annotations["transform"]["rotation"]["z"]
      );

      // set the transform scale
      obj.transform.localScale = new Vector3(
        (float)annotations["transform"]["scale"]["x"],
        (float)annotations["transform"]["scale"]["y"],
        (float)annotations["transform"]["scale"]["z"]
      );

      // set the layer to SimObjPhysics
      obj.layer = LayerMask.NameToLayer("SimObjVisible");

      // set the tag to SimObjPhysics
      obj.tag = "SimObjPhysics";

      // save obj as a prefab
      PrefabUtility.SaveAsPrefabAsset(obj, basePath + modelId + "/" + modelId + ".prefab");

      // delete the obj
      // GameObject.DestroyImmediate(obj);
      Debug.Log("Saved " + modelId + " as a prefab");
    }
  }
}
