// Copyright Allen Institute for Artificial Intelligence 2017
using UnityEngine;
using System.Collections;
using System.Linq;
public class RoomCreator : MonoBehaviour {

[SerializeField]
private Mesh floorMesh;
[SerializeField]
private Material floorMaterial;
private Material wallMaterial;

private GameObject floorGameObject;
public void Start() {

    var position = new Vector3(0.0f, 0.0f, 0.0f);
    floorGameObject = new GameObject("GenFloor");

    floorGameObject.tag = "SimObjPhysics";
    floorGameObject.layer = 8;

    var rb = floorGameObject.AddComponent<Rigidbody>();
    rb.mass = 1.0f;
    rb.angularDrag = 0.05f;
    rb.useGravity = true;
    rb.isKinematic = true;

    //  position, Quaternion.identity);
    var meshFilter = floorGameObject.AddComponent<MeshFilter>();
    var width = 10.0f;
    var depth = 5.0f;

    var colliderThickness = 1.0f;

    var receptacleHeight = 0.7f;

    var wallHeight = 3.0f;

    var offsetX = width / 2.0f;
    var offsetZ = depth / 2.0f;
    var height = 0.0f;
    var simObjId = "FloorGen|+00.00|+00.00|+00.00";

    var mesh = meshFilter.mesh;
    mesh.vertices = new Vector3[] { new Vector3(0 - offsetX, height, 0 - offsetZ), new Vector3(width - offsetX, height, 0 - offsetZ), new Vector3(width - offsetX, height, depth - offsetZ), new Vector3(0-offsetX, height, depth - offsetZ)};
    mesh.uv = new Vector2[] {new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)};
    mesh.triangles = new int[] {0, 2, 1, 0, 3, 2};
    mesh.normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
    var meshRenderer = floorGameObject.AddComponent<MeshRenderer>();
    meshRenderer.material = floorMaterial;
    var visibilityPoints = new GameObject("VisibilityPoints");
    visibilityPoints.transform.parent = floorGameObject.transform;
    
    var marginX = 0.5f;
    var marginZ = 0.5f;
    
    var step = 1/3.0f;
    var count = 0;
    for (float x =  -offsetX + marginX; x < (width/2.0f)-marginX + step; x+=step) {
        for (float z = -offsetZ + marginZ; z < (depth/2.0f)-marginZ + step; z+=step) {
            var vp = new GameObject($"VisibilityPoint ({count})");
            vp.transform.position = new Vector3(x, height, z);
            vp.transform.parent = visibilityPoints.transform;
            count++;
        }
    }

    var boundingBox = new GameObject("BoundingBox");
    var bbCollider = boundingBox.AddComponent<BoxCollider>();
    bbCollider.enabled = false;
    boundingBox.transform.parent = floorGameObject.transform;

    var receptacleTriggerBox = setReceptacle(floorGameObject, width, depth, receptacleHeight, marginX, marginZ, offsetX, offsetZ);

    var collider = setCollider(floorGameObject, width, depth, offsetX, offsetZ, colliderThickness);

    var simObjPhysics = floorGameObject.AddComponent<SimObjPhysics>();
    simObjPhysics.objectID = simObjId;
    simObjPhysics.ObjType = SimObjType.Floor;
    simObjPhysics.PrimaryProperty = SimObjPrimaryProperty.Static;
    simObjPhysics.SecondaryProperties = new SimObjSecondaryProperty[] { SimObjSecondaryProperty.Receptacle };

    simObjPhysics.BoundingBox = boundingBox;

    simObjPhysics.VisibilityPoints = visibilityPoints.GetComponentsInChildren<Transform>();

    simObjPhysics.ReceptacleTriggerBoxes = new GameObject[] { receptacleTriggerBox };
    simObjPhysics.MyColliders = new Collider[] { collider.GetComponent<Collider>() };

    simObjPhysics.transform.parent = floorGameObject.transform;

    receptacleTriggerBox.AddComponent<Contains>();


     var structure = new GameObject("GenStructure");
     createWall(structure, wallHeight, mesh.vertices[0], mesh.vertices[1]);

     createWall(structure, wallHeight, mesh.vertices[1], mesh.vertices[2]);
     createWall(structure, wallHeight, mesh.vertices[2], mesh.vertices[3]);
     createWall(structure, wallHeight, mesh.vertices[3], mesh.vertices[0]);

    
}

private GameObject setReceptacle(
    GameObject floorGameObject, float width, float depth, float height, float marginX, float marginZ, float offsetX, float offsetZ
) {
    var receptacleTriggerBox = new GameObject("ReceptacleTriggerBox");
    var receptacleCollider = receptacleTriggerBox.AddComponent<BoxCollider>();
    receptacleCollider.isTrigger = true;
    var widthMinusMargin = width - 2.0f * marginX;

    var depthMinusMargin = depth - 2.0f * marginZ;

    receptacleCollider.size = new Vector3(widthMinusMargin, height, depthMinusMargin);
    //var offsetRatioX= offsetX / width;
    //var offsetRatioZ= offsetZ / depth;
    receptacleCollider.center = new Vector3(width / 2.0f - offsetX, height/2.0f, depth / 2.0f - offsetZ);
    
    receptacleTriggerBox.transform.parent = floorGameObject.transform;
    return receptacleTriggerBox;
} 

private GameObject setCollider(GameObject floorGameObject, float width, float depth, float offsetX, float offsetZ, float thickness) {
    var colliders = new GameObject("Colliders");

    var collider = new GameObject("Col");
    var box = collider.AddComponent<BoxCollider>();

    var center = new Vector3(width / 2.0f - offsetX, -thickness/2.0f, depth / 2.0f - offsetZ);
    var size = new Vector3(width, thickness, depth);
    // TODO set collider params
    box.size = new Vector3(width, thickness, depth);
    box.center = center;
    collider.transform.parent = colliders.transform;

    colliders.transform.parent = floorGameObject.transform;

    var triggerColliders = new GameObject("TriggerColliders");
    var triggerCollider = new GameObject("Col");
    var triggerBox = triggerCollider.AddComponent<BoxCollider>();

    triggerBox.center = center;
    triggerBox.size = size;
    triggerBox.isTrigger = true;

    triggerCollider.transform.parent = triggerColliders.transform;
    triggerColliders.transform.parent = floorGameObject.transform;

    return collider;
} 


private GameObject createWall(GameObject structure, float height, Vector3 p1, Vector3 p2) {

    var wall = new GameObject("Wall");

    var meshF = wall.AddComponent<MeshFilter>();
    var meshC = wall.AddComponent<MeshCollider>();

    var mesh = meshF.mesh;

    var p1p2 =  (p2 - p2);
   
    var normal = Vector3.Cross(p1p2, Vector3.up);

    mesh.vertices = new Vector3[] { p1, p1 + new Vector3(0.0f, height, 0.0f), p2 +  new Vector3(0.0f, height, 0.0f), p2 };
    mesh.uv = new Vector2[] {new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)};
    mesh.triangles = new int[] {0, 2, 1, 0, 3, 2};
    mesh.normals = new Vector3[] { normal, normal, normal, normal};

    //mesh.vertices 

    var meshRenderer = wall.AddComponent<MeshRenderer>();
    meshRenderer.material = wallMaterial;

    wall.transform.parent = structure.transform;


    return wall;
}

}