using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using Siccity.GLTFUtility;

public class PlaceGLB : MonoBehaviour {

    public string basePath = null;

    // Use this for initialization
    void Start () {
        var annotationsStr = System.IO.File.ReadAllText(basePath + "annotations.json");
        JObject annotations = JObject.Parse(annotationsStr);

        // load the glb model
        GameObject result = Importer.LoadFromFile(basePath + "model.glb");
        result.name = "mesh";
        result.transform.parent = this.transform;

        SimObjPhysics simObj = this.GetComponent<SimObjPhysics>();
        GameObject meshColliders = new GameObject("colliders");
        meshColliders.layer = LayerMask.NameToLayer("SimObjVisible");
        meshColliders.transform.parent = this.transform;
        // reset the scale of the meshColliders to be 1, 1, 1

        List<Collider> colliders = new List<Collider>();
        for (int i = 0; i < annotations["colliders"].Count(); i++) {
            // skip meta files
            MeshCollider meshCollider = meshColliders.AddComponent<MeshCollider>();

            Mesh colliderMesh = new Mesh();
            // cast annotations["colliders"][i].vertices to Vector3[]
            Vector3[] vertices = new Vector3[annotations["colliders"][i]["vertices"].Count()];
            for (int j = 0; j < annotations["colliders"][i]["vertices"].Count(); j++) {
            vertices[j] = new Vector3(
                (float)annotations["colliders"][i]["vertices"][j]["x"],
                (float)annotations["colliders"][i]["vertices"][j]["y"],
                (float)annotations["colliders"][i]["vertices"][j]["z"]
            );
            }
            colliderMesh.vertices = vertices;
            colliderMesh.triangles = annotations["colliders"][i]["triangles"].ToObject<int[]>();

            meshCollider.sharedMesh = colliderMesh;
            meshCollider.convex = true;
            colliders.Add(meshCollider);
        }
        simObj.MyColliders = colliders.ToArray();

        // find the ReceptacleTriggerBoxes child
        
        // set the scale of the ReceptacleTriggerBoxes to be the mesh scale
        // 
    }
}
