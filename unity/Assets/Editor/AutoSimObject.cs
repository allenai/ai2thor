using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using Siccity.GLTFUtility;
using Thor.Procedural;


public class AutoSimObject : EditorWindow {
  [MenuItem("AI2-THOR/Draw 3D-BB")]
  static void Draw3DBoundingBoxes() {
    // find the "Objects" in the hierarchy
    GameObject[] objects = GameObject.FindGameObjectsWithTag("SimObjPhysics");

    foreach (GameObject obj in objects) {
      // get the SimObjPhysics component
      SimObjPhysics simObj = obj.GetComponent<SimObjPhysics>();
      if (simObj == null) {
        continue;
      }

      // skip if it's disabled
      if (!simObj.enabled) {
        continue;
      }

      Debug.Log(simObj.AxisAlignedBoundingBox.size);
      Debug.Log(simObj.AxisAlignedBoundingBox.center);
      float xDiff = simObj.AxisAlignedBoundingBox.size.x / 2;
      float yDiff = simObj.AxisAlignedBoundingBox.size.y / 2;
      float zDiff = simObj.AxisAlignedBoundingBox.size.z / 2;

      float xMean = simObj.AxisAlignedBoundingBox.center.x;
      float yMean = simObj.AxisAlignedBoundingBox.center.y;
      float zMean = simObj.AxisAlignedBoundingBox.center.z;

      // get all of the lines from the 3D bounding box
      Vector3[] verts = new Vector3[12];
      verts[0] = new Vector3(xMean - xDiff, yMean - yDiff, zMean - zDiff);
      verts[1] = new Vector3(xMean + xDiff, yMean - yDiff, zMean - zDiff);
      verts[2] = new Vector3(xMean + xDiff, yMean + yDiff, zMean - zDiff);
      verts[3] = new Vector3(xMean - xDiff, yMean + yDiff, zMean - zDiff);
      verts[4] = new Vector3(xMean - xDiff, yMean - yDiff, zMean + zDiff);
      verts[5] = new Vector3(xMean + xDiff, yMean - yDiff, zMean + zDiff);
      verts[6] = new Vector3(xMean + xDiff, yMean + yDiff, zMean + zDiff);
      verts[7] = new Vector3(xMean - xDiff, yMean + yDiff, zMean + zDiff);
      verts[8] = new Vector3(xMean - xDiff, yMean - yDiff, zMean - zDiff);
      verts[9] = new Vector3(xMean - xDiff, yMean - yDiff, zMean + zDiff);
      verts[10] = new Vector3(xMean - xDiff, yMean + yDiff, zMean - zDiff);
      verts[11] = new Vector3(xMean - xDiff, yMean + yDiff, zMean + zDiff);

      float size = 0.035f;

      // green color
      Color color = Color.green;

      // draw the line by instantiating a cube and making it red
      GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
      cube.transform.position = (verts[0] + verts[1]) / 2;
      cube.transform.localScale = new Vector3(Vector3.Distance(verts[0], verts[1]) + size, size, size);
      cube.GetComponent<Renderer>().material.color = color;

      cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
      cube.transform.position = (verts[1] + verts[2]) / 2;
      cube.transform.localScale = new Vector3(Vector3.Distance(verts[1], verts[2]) + size, size, size);
      cube.transform.Rotate(new Vector3(0, 0, 90));
      cube.GetComponent<Renderer>().material.color = color;

      cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
      cube.transform.position = (verts[2] + verts[3]) / 2;
      cube.transform.localScale = new Vector3(Vector3.Distance(verts[2], verts[3]) + size, size, size);
      cube.GetComponent<Renderer>().material.color = color;

      cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
      cube.transform.position = (verts[3] + verts[0]) / 2;
      cube.transform.localScale = new Vector3(Vector3.Distance(verts[3], verts[0]) + size, size, size);
      cube.transform.Rotate(new Vector3(0, 0, 90));
      cube.GetComponent<Renderer>().material.color = color;

      cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
      cube.transform.position = (verts[4] + verts[5]) / 2;
      cube.transform.localScale = new Vector3(Vector3.Distance(verts[4], verts[5]) + size, size, size);
      cube.GetComponent<Renderer>().material.color = color;

      cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
      cube.transform.position = (verts[5] + verts[6]) / 2;
      cube.transform.localScale = new Vector3(Vector3.Distance(verts[5], verts[6]) + size, size, size);
      cube.transform.Rotate(new Vector3(0, 0, 90));
      cube.GetComponent<Renderer>().material.color = color;

      cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
      cube.transform.position = (verts[6] + verts[7]) / 2;
      cube.transform.localScale = new Vector3(Vector3.Distance(verts[6], verts[7]) + size, size, size);
      cube.GetComponent<Renderer>().material.color = color;

      cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
      cube.transform.position = (verts[7] + verts[4]) / 2;
      cube.transform.localScale = new Vector3(Vector3.Distance(verts[7], verts[4]) + size, size, size);
      cube.transform.Rotate(new Vector3(0, 0, 90));
      cube.GetComponent<Renderer>().material.color = color;

      cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
      cube.transform.position = (verts[8] + verts[9]) / 2;
      cube.transform.localScale = new Vector3(Vector3.Distance(verts[8], verts[9]) + size, size, size);
      cube.transform.Rotate(new Vector3(90, 0, 90));
      cube.GetComponent<Renderer>().material.color = color;

      cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
      cube.transform.position = (verts[10] + verts[11]) / 2;
      cube.transform.localScale = new Vector3(Vector3.Distance(verts[10], verts[11]) + size, size, size);
      cube.transform.Rotate(new Vector3(90, 0, 90));
      cube.GetComponent<Renderer>().material.color = color;

      cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
      cube.transform.position = (verts[1] + verts[5]) / 2;
      cube.transform.localScale = new Vector3(Vector3.Distance(verts[1], verts[5]) + size, size, size);
      cube.transform.Rotate(new Vector3(90, 0, 90));
      cube.GetComponent<Renderer>().material.color = color;

      cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
      cube.transform.position = (verts[2] + verts[6]) / 2;
      cube.transform.localScale = new Vector3(Vector3.Distance(verts[2], verts[6]) + size, size, size);
      cube.transform.Rotate(new Vector3(90, 0, 90));
      cube.GetComponent<Renderer>().material.color = color;
    }
  }

  [MenuItem("AI2-THOR/Load GLB Prefab")]
  public static void LoadGLB() {
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
  public static void MakeSimObject() {
    string basePath = "Assets/Prefabs/debug/";

    // get all folders in the basePath
    string[] folders = System.IO.Directory.GetDirectories(basePath);

    // create a prefab of each model in the folders
    foreach (string folder in folders) {
      string modelId = folder.Substring(folder.LastIndexOf("/") + 1);

      // load the annotations
      var annotationsStr = System.IO.File.ReadAllText(basePath + modelId + "/annotations.json");
      JObject annotations = JObject.Parse(annotationsStr);

      GameObject obj = new GameObject(modelId);
      GameObject mesh;
      if (System.IO.File.Exists(basePath + modelId + "/model.glb")) {
        // load the glb file -- used with abo
        mesh = Importer.LoadFromFile(basePath + modelId + "/model.glb");
      } else {
        // instantiate the obj file -- used with google scanned objects
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePath + modelId + "/model.obj");
        mesh = GameObject.Instantiate(prefab);
      }
      // mesh.transform.rotation = Quaternion.identity;
      // Debug.Log("position:");
      // Debug.Log(obj.transform.position);
      // Debug.Log("rotation:");
      // Debug.Log(obj.transform.position);
      // Debug.Log("scale:");
      // Debug.Log(obj.transform.localScale);
      mesh.transform.parent = obj.transform;
      // set the name
      mesh.name = "mesh";

      // add the name
      obj.name = modelId;
      obj.layer = LayerMask.NameToLayer("SimObjVisible");
      obj.tag = "SimObjPhysics";

      // add a SimObjPhysics component
      SimObjPhysics simObj = obj.AddComponent<SimObjPhysics>();
      simObj.assetID = modelId;
      // string primaryProperty = annotations["primaryProperty"].ToString();
      // if (primaryProperty != "") {
      //   simObj.PrimaryProperty = (SimObjPrimaryProperty)Enum.Parse(
      //     typeof(SimObjPrimaryProperty), annotations["primaryProperty"].ToString()
      //   );
      // }

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

      // GameObject meshColliders = new GameObject("colliders");
      // meshColliders.layer = LayerMask.NameToLayer("SimObjVisible");
      // meshColliders.transform.parent = obj.transform;

      // List<Collider> colliders = new List<Collider>();
      // for (int i = 0; i < annotations["colliders"].Count(); i++) {
      //   // skip meta files
      //   MeshCollider meshCollider = meshColliders.AddComponent<MeshCollider>();

      //   Mesh colliderMesh = new Mesh();
      //   // cast annotations["colliders"][i].vertices to Vector3[]
      //   Vector3[] vertices = new Vector3[annotations["colliders"][i]["vertices"].Count()];
      //   for (int j = 0; j < annotations["colliders"][i]["vertices"].Count(); j++) {
      //     vertices[j] = new Vector3(
      //       (float)annotations["colliders"][i]["vertices"][j]["x"],
      //       (float)annotations["colliders"][i]["vertices"][j]["y"],
      //       (float)annotations["colliders"][i]["vertices"][j]["z"]
      //     );
      //   }
      //   colliderMesh.vertices = vertices;
      //   colliderMesh.triangles = annotations["colliders"][i]["triangles"].ToObject<int[]>();

      //   meshCollider.sharedMesh = colliderMesh;
      //   meshCollider.convex = true;
      //   colliders.Add(meshCollider);
      // }
      // simObj.MyColliders = colliders.ToArray();

      // visPoints.transform.localScale = new Vector3(1, 1, 1);
      // visPoints.transform.localPosition = new Vector3(0, 0, 0);

      // get all the colliders
      // string collidersPath = basePath + modelId + "/colliders/";
      // string[] colliderPaths = System.IO.Directory.GetFiles(collidersPath);

      // add the colliders
      // List<Collider> colliders = new List<Collider>();
      // for (int i = 0; i < colliderPaths.Length; i++) {
      //   // skip meta files
      //   if (!colliderPaths[i].EndsWith(".obj")) {
      //     continue;
      //   }
      //   MeshCollider meshCollider = meshColliders.AddComponent<MeshCollider>();
      //   meshCollider.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(colliderPaths[i]);
      //   meshCollider.convex = true;
      //   colliders.Add(meshCollider);
      // }
      // simObj.MyColliders = colliders.ToArray();

      // add a RigidBody component
      Rigidbody rigidBody = obj.AddComponent<Rigidbody>();

      // rotate the internal components of the model. Note that you do not want to do this
      // at `obj` level as it should be at its cannonical orientation when at rotation 0,0,0.
      // Quaternion rot = Quaternion.Euler(
      //   (float)annotations["transform"]["rotation"]["x"],
      //   (float)annotations["transform"]["rotation"]["y"],
      //   (float)annotations["transform"]["rotation"]["z"]
      // );
      // TODO: This is buggy - Find("default") doesn't work for some objects!
      // simObj.transform.Find("default").rotation = rot;
      // visPoints.transform.rotation = rot;
      // meshColliders.transform.rotation = rot;

      // set the transform scale
      // obj.transform.localScale = new Vector3(
      //   (float)annotations["transform"]["scale"]["x"],
      //   (float)annotations["transform"]["scale"]["y"],
      //   (float)annotations["transform"]["scale"]["z"]
      // );

      // Generate receptacle trigger boxes
      if ((bool) annotations["receptacleCandidate"]) {
        ReceptacleTriggerBoxEditor.TryToAddReceptacleTriggerBox(sop: simObj);
        GameObject receptacleTriggerBoxes = obj.transform.Find("ReceptacleTriggerBoxes").gameObject;
        if (receptacleTriggerBoxes.transform.childCount > 0) {
          receptacleTriggerBoxes.transform.localScale = mesh.transform.localScale;
          simObj.SecondaryProperties = new SimObjSecondaryProperty[] { SimObjSecondaryProperty.Receptacle };
        }
      }

      var placeGLB = obj.AddComponent<PlaceGLB>();
      placeGLB.basePath = basePath + modelId + "/";

      // remove the mesh object from the scene
      GameObject.DestroyImmediate(mesh);

      // save obj as a prefab
      PrefabUtility.SaveAsPrefabAsset(obj, basePath + modelId + "/" + modelId + ".prefab");

      // delete the obj
      GameObject.DestroyImmediate(obj);
      Debug.Log("Saved " + modelId + " as a prefab");
    }
  }


  public static void BuildAssetDBForProcedural() {
            // /EditorSceneManager.LoadS
            var scenePath = "Assets/Scenes/Procedural/Procedural.unity";
            // var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();

            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
            var proceduralADB = GameObject.FindObjectOfType<ProceduralAssetDatabase>();
            // proceduralADB.prefabs = new AssetMap<GameObject>(ProceduralTools.FindPrefabsInAssets().GroupBy(m => m.name).ToDictionary(m => m.Key, m => m.First()));
            // proceduralADB.materials = new AssetMap<Material>(ProceduralTools.FindAssetsByType<Material>().GroupBy(m => m.name).ToDictionary(m => m.Key, m => m.First()));

            proceduralADB.prefabs = ProceduralTools.FindPrefabsInAssets();
            proceduralADB.materials = ProceduralTools.FindAssetsByType<Material>();
            proceduralADB.totalMats = proceduralADB.materials.Count();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
}
