// Copyright Allen Institute for Artificial Intelligence 2017
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityStandardAssets.Characters.FirstPerson;

public static class IEnnumerableExtension {
    public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(this IEnumerable<IEnumerable<T>> sequences)
    {
    IEnumerable<IEnumerable<T>> emptyProduct = new[] { Enumerable.Empty<T>()};
    return sequences.Aggregate(
        emptyProduct,
        (accumulator, sequence) => 
            from accseq in accumulator 
            from item in sequence 
            select accseq.Concat(new[] {item})                       
        );
    }
}
public class RoomCreator : MonoBehaviour {

[SerializeField]
private Mesh floorMesh;
[SerializeField]
private Material floorMaterial;
private Material wallMaterial;

private GameObject floorGameObject;
public void Start() {

    var position = new Vector3(0.0f, 0.0f, 0.0f);
   //floorGameObject = ProceduralTools.createFloorGameObject();
   
    var width = 10.0f;
    var depth = 5.0f;

    var colliderThickness = 1.0f;

    var receptacleHeight = 0.7f;

    var wallHeight = 3.0f;
    var wallThickness = 0.25f;

    var offsetX = width / 2.0f;
    var offsetZ = depth / 2.0f;
    var height = 0.0f;
    var simObjId = "FloorGen|+00.00|+00.00|+00.00";

    // var floorMaterialId = "THORKEA_Carpet_Mat";
    // var wallMaterialId = "THORKEA_Wall_Panel_Fabric_Mat";
    var floorMaterialId = "DarkWoodFloors";
    var wallMaterialId = "DrywallOrange";

    var marginX = 0.1f;
    var marginZ = 0.1f;

    var floor = new RectangleFloor() { 
        center = Vector3.zero,
        width = width, 
        depth = depth, 
        marginWidth = marginX, 
        marginDepth = marginZ, 
        materialId = floorMaterialId 
    };
    var room = new RectangleRoom() { 
        rectangleFloor = floor, 
        walls = RectangleFloor.createSurroundingWalls(floor, wallMaterialId, wallHeight, wallThickness) 
    };

    // Old constructor style
     // var room = new RectangleRoom(Vector3.zero, width, depth, marginX, marginZ, wallHeight, wallThickness, "", "DrywallOrange");

     var room2 = RectangleRoom.roomFromWallPoints(new Vector3[]{
        new Vector3(-offsetX, height, -offsetZ),


        new Vector3(-offsetX, height, offsetZ),

        new Vector3(offsetX, height, offsetZ),
        new Vector3(offsetX, height, offsetZ / 2.0f),

          new Vector3(offsetX+2.0f, height, offsetZ / 2.0f),
          
         new Vector3(offsetX+2.0f, height, offsetZ / 2.0f - 3.0f),

        
        new Vector3(offsetX, height, offsetZ / 2.0f - 3.0f),


        // new Vector3(offsetX/2.0f, height, offsetZ - 0.1f),
        // new Vector3(offsetX/2.0f - 0.2f, height, offsetZ - 0.1f),

        // new Vector3(offsetX/2.0f - 0.2f, height, offsetZ),
        new Vector3(offsetX, height, -offsetZ)
     }, 
     wallHeight,  wallThickness, floorMaterialId, wallMaterialId);

     var mats =  new AssetMap<Material>(ProceduralTools.FindAssetsByType<Material>().GroupBy(m => m.name).ToDictionary(m => m.Key, m => m.First()));

    floorGameObject = ProceduralTools.createFloorGameObject("Floor", room2, mats, simObjId, receptacleHeight, colliderThickness);
    //var mats = ProceduralTools.FindPrefabsInAssets();
    //Debug.Log("Prefabs" + string.Join(",", mats.Select(m => m.name).ToArray()) + " len " + mats.Count);

    var db = new AssetMap<GameObject>(ProceduralTools.FindPrefabsInAssets().GroupBy(m => m.name).ToDictionary(m => m.Key, m => m.First()));
    ProceduralTools.spawnObjectAtReceptacle(db, "Coffee_Table_211_1", floorGameObject.GetComponentInChildren<SimObjPhysics>());
    // var prefabDb = new AssetDatabase<GameObject>(
    //     ProceduralTools.FindAssetsByType<Material>().GroupBy(m => m.name).ToDictionary(m => m.Key, m => m.First())
    // );



    // // mesh.vertices = new Vector3[] { new Vector3(0 - offsetX, height, 0 - offsetZ), new Vector3(width - offsetX, height, 0 - offsetZ), new Vector3(width - offsetX, height, depth - offsetZ), new Vector3(0-offsetX, height, depth - offsetZ)};
    // // mesh.uv = new Vector2[] {new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)};
    // // mesh.triangles = new int[] {0, 2, 1, 0, 3, 2};
    // // mesh.normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };

    // floorGameObject.GetComponent<MeshFilter>().mesh = ProceduralTools.GetRectangleFloorMesh(room);


    // var meshRenderer = floorGameObject.AddComponent<MeshRenderer>();
    // meshRenderer.material = floorMaterial;
    // //var visibilityPoints = new GameObject("VisibilityPoints");
    // var visibilityPoints = ProceduralTools.CreateVisibilityPointsGameObject(room);
    // visibilityPoints.transform.parent = floorGameObject.transform;
    
    // // var step = 1/3.0f;
    // // var count = 0;
    // // for (float x =  -offsetX + marginX; x < (width/2.0f)-marginX + step; x+=step) {
    // //     for (float z = -offsetZ + marginZ; z < (depth/2.0f)-marginZ + step; z+=step) {
    // //         var vp = new GameObject($"VisibilityPoint ({count})");
    // //         vp.transform.position = new Vector3(x, height, z);
    // //         vp.transform.parent = visibilityPoints.transform;
    // //         count++;
    // //     }
    // // }

    

    // // var receptacleTriggerBox = setReceptacle(floorGameObject, width, depth, receptacleHeight, marginX, marginZ, offsetX, offsetZ);
    // var receptacleTriggerBox = ProceduralTools.createFloorReceptacle(floorGameObject, room, receptacleHeight);

    // //var collider = setCollider(floorGameObject, width, depth, offsetX, offsetZ, colliderThickness);
    // var collider = ProceduralTools.createFloorCollider(floorGameObject, room, colliderThickness);

    // // var simObjPhysics = floorGameObject.AddComponent<SimObjPhysics>();
    // // simObjPhysics.objectID = simObjId;
    // // simObjPhysics.ObjType = SimObjType.Floor;
    // // simObjPhysics.PrimaryProperty = SimObjPrimaryProperty.Static;
    // // simObjPhysics.SecondaryProperties = new SimObjSecondaryProperty[] { SimObjSecondaryProperty.Receptacle };

    // // simObjPhysics.BoundingBox = boundingBox;

    // // simObjPhysics.VisibilityPoints = visibilityPoints.GetComponentsInChildren<Transform>();

    // // simObjPhysics.ReceptacleTriggerBoxes = new GameObject[] { receptacleTriggerBox };
    // // simObjPhysics.MyColliders = new Collider[] { collider.GetComponentInChildren<Collider>() };

    // // simObjPhysics.transform.parent = floorGameObject.transform;
    // ProceduralTools.setRoomSimObjectPhysics(floorGameObject, simObjId, visibilityPoints, receptacleTriggerBox, collider.GetComponentInChildren<Collider>());

    // ProceduralTools.createWalls(room, "Structure");

    // //  var structure = new GameObject("GenStructure");
    // //  createWall(structure, wallHeight, mesh.vertices[0], mesh.vertices[1]);

    // //  createWall(structure, wallHeight, mesh.vertices[1], mesh.vertices[2]);
    // //  createWall(structure, wallHeight, mesh.vertices[2], mesh.vertices[3]);
    // //  createWall(structure, wallHeight, mesh.vertices[3], mesh.vertices[0]);

    
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

//public enum WallType

public class Wall {
    public float height;
    public Vector3 p0;
    public Vector3 p1;
    public float thickness;
    public bool empty;
    public string materialId;
    
}

public interface Floor {
    Vector3[] cornersClockwise { get; }
    string materialId { get; }
}

// TODO unused for now, add this instead of Room properties
public class RectangleFloor : Floor {
    public Vector3 center;
    public float width;
    public float depth;

    // TODO decide if needed here
    // public float floorThickness {get;}

    public float marginWidth;
    public float marginDepth;

    public string materialId { get; set; }

    public Vector3[] cornersClockwise {
        get {
        if (_corners == null) {
            var offsetXMargin = new Vector3(width / 2.0f - marginWidth, 0, 0);
            var offsetZMargin = new Vector3(0, 0, depth / 2.0f - marginDepth);
            
            // Clockwise corner
            // (0,0), (0,1), (1,1), (1,0)
            _corners = new Vector3[]{
                center - offsetXMargin - offsetZMargin,
                center - offsetXMargin + offsetZMargin,
                center + offsetXMargin + offsetZMargin,
                center + offsetXMargin - offsetZMargin
            };
        }
        return _corners;
        }
    }

    private Vector3[] _corners;

    public static Wall[] createSurroundingWalls(Floor floor, string wallMaterialId, float wallHeight, float wallThickness = 0.0f) {
        return floor.cornersClockwise.Zip(
            floor.cornersClockwise.Skip(1).Concat(new Vector3[]{ floor.cornersClockwise.FirstOrDefault() }),
            (p0, p1) => new Wall(){ height = wallHeight, p0 = p0, p1 = p1, thickness = wallThickness, materialId = wallMaterialId }
        ).ToArray();
    }

}

public interface Room {


    Wall[] walls { get; }
    Floor floor { get; }

    Vector3 center { get; }

    // float width { get; }

    // float depth { get; }

    // public Floor floor;

    // For a convex floor implementation using the walls
    // public Room(Wall[] walls) {
    //     this.cornersClockWise = walls
    //         .SelectMany(w => new Vector3[]{w.p0, w.p1})
    //         .Distinct()
    //         .OrderBy(p => p.z)
    //         .ThenBy(p => p, Comparer<Vector3>.Create((a, b) => System.Math.Sign( (a.x - center.x) * (b.y - center.y) - (b.x - center.x) * (a.y - center.y)))).ToArray();
    // }

   

}

// TODO move to this interface
public class RectangleRoom : Room {
    public RectangleFloor rectangleFloor { get; set; }
    public Floor floor { get {return rectangleFloor;} }
    public Wall[] walls { get; set; }

    public Vector3 center { get {return rectangleFloor.center; } }

    public float width { get {return rectangleFloor.width; } }
    public float depth { get {return rectangleFloor.depth; } }


    // TODO decide if needed here
    // public float floorThickness {get;}

    public float marginWidth  { get {return rectangleFloor.marginWidth; } }

    public float marginDepth { get {return rectangleFloor.marginDepth; } }

    public Vector3[] cornersClockWise { get { return rectangleFloor.cornersClockwise;} }


    public static RectangleRoom roomFromWallPoints(Vector3[] corners, float wallHeight, float wallThickness, string floorMaterialId, string wallMaterialId, float marginWidth =0.0f, float marginDepth = 0.0f) {

        var centroid = corners.Aggregate(Vector3.zero, (accumulator, c) => accumulator + c) / corners.Length; 
        

        // var cornersClockWise = corners
        //     .OrderBy(p => p.z)
        //     .ThenBy(p => p, Comparer<Vector3>.Create((a, b) => System.Math.Sign( (a.x - center.x) * (b.y - center.y) - (b.x - center.x) * (a.y - center.y)))).ToArray();
        var cornersClockWise = corners;

        // TODO what to do when different heights?
        var minY = cornersClockWise.Min(p => p.y);

        var minPoint = new Vector3(cornersClockWise.Min(c => c.x), minY, cornersClockWise.Min(c => c.z));
        var maxPoint = new Vector3(cornersClockWise.Max(c => c.x), minY, cornersClockWise.Max(c => c.z));

        var dimensions = maxPoint - minPoint;

        var floor = new RectangleFloor() { center = minPoint + dimensions / 2.0f, width = dimensions.x, depth = dimensions.z, marginWidth = marginWidth, marginDepth = marginDepth, materialId = floorMaterialId};

        var walls =  cornersClockWise.Zip(
            cornersClockWise.Skip(1).Concat(new Vector3[]{ cornersClockWise.FirstOrDefault() }),
            (p0, p1) => new Wall(){ height = wallHeight, p0 = p0, p1 = p1, thickness = wallThickness, materialId = wallMaterialId }
        ).ToArray();

        return new RectangleRoom() { walls = walls, rectangleFloor = floor};
        // var m = new Vector3[]{};
        // var orderByResult = from s in m
        //            orderby s.x, s.y 
        //            select new { s };
        //initRoom(origin, width, height, walls);
    }

    public static RectangleRoom roomFromWalls(Wall[] walls, string floorMaterialId, float marginWidth =0.0f, float marginDepth = 0.0f) {

       //     public RectangleRoom(Wall[] walls) {

        var wallPoints = walls.SelectMany(w => new Vector3[]{w.p0, w.p1}).Distinct();

        // TODO check all y are the same?
        var minY = walls.SelectMany(w => new Vector3[]{w.p0, w.p1}).Min(p => p.y);


        var centroid = wallPoints.Aggregate(Vector3.zero, (accumulator, c) => accumulator + c) / wallPoints.Count();
         
        // Clockwise point sort with determinant (pseduo-cross), move to Room interface general for any convex room
        // var cornersClockWise = wallPoints
        //     .OrderBy(p => p.z)
        //     .ThenBy(p => p, Comparer<Vector3>.Create((a, b) => System.Math.Sign( (a.x - centroid.x) * (b.y - centroid.y) - (b.x - centroid.x) * (a.y - centroid.y)))).ToArray();

        
        var minPoint = new Vector3(wallPoints.Min(c => c.x), minY, wallPoints.Min(c => c.z));
        var maxPoint = new Vector3(wallPoints.Max(c => c.x), minY, wallPoints.Max(c => c.z));

        var dimensions = maxPoint - minPoint;

        var floor = new RectangleFloor() { 
            center = minPoint + dimensions / 2.0f,
            width = dimensions.x, 
            depth = dimensions.z, marginWidth = marginWidth, marginDepth = marginDepth, materialId = floorMaterialId};


        return new RectangleRoom() { walls = walls, rectangleFloor = floor};

        // var m = new Vector3[]{};
        // var orderByResult = from s in m
        //            orderby s.x, s.y 
        //            select new { s };
        //initRoom(origin, width, height, walls);
    }

    public static Wall[] wallsFromContiguousPoints(Vector3[] corners, float wallHeight, float wallThickness, string wallMaterialId) {
        var centroid = corners.Aggregate(Vector3.zero, (accumulator, c) => accumulator + c) / corners.Length; 
        

        // var cornersClockWise = corners
        //     .OrderBy(p => p.z)
        //     .ThenBy(p => p, Comparer<Vector3>.Create((a, b) => System.Math.Sign( (a.x - center.x) * (b.y - center.y) - (b.x - center.x) * (a.y - center.y)))).ToArray();
        var cornersClockWise = corners;

        // TODO what to do when different heights?
        var minY = cornersClockWise.Min(p => p.y);

        var minPoint = new Vector3(cornersClockWise.Min(c => c.x), minY, cornersClockWise.Min(c => c.z));
        var maxPoint = new Vector3(cornersClockWise.Max(c => c.x), minY, cornersClockWise.Max(c => c.z));

        var dimensions = maxPoint - minPoint;

        // var floor = new RectangleFloor() { center = minPoint + dimensions / 2.0f, width = dimensions.x, depth = dimensions.z, marginWidth = marginWidth, marginDepth = marginDepth, materialId = floorMaterialId};

        return cornersClockWise.Zip(
            cornersClockWise.Skip(1).Concat(new Vector3[]{ cornersClockWise.FirstOrDefault() }),
            (p0, p1) => new Wall(){ height = wallHeight, p0 = p0, p1 = p1, thickness = wallThickness, materialId = wallMaterialId }
        ).ToArray();

    }

    // private void initRoom(Vector3 origin, float width, float height, Wall[] walls) {
    //     this.width = width;
    //     this.depth = height;
    //     this.origin = origin;
    //     this.walls = walls;
    // }
}

// public class RectangleRoom : Room {
//     public float width { get; }
//     public float depth { get; }

//     // TODO decide if needed here
//     // public float floorThickness {get;}

//     public float marginWidth { get; }
//     public float marginDepth { get; }
//     public float? uniformWallHeight { get; }

//     public Vector3[] cornersClockWise { get; private set; }

//     public string materialId { get; }


//     public RectangleRoom(
//         Vector3 center, float width, float depth, float marginWidth = 0.0f, float marginDepth = 0.0f, float? uniformWallHeight = null, float wallThickness = 0.0f, string materialId = "", string wallMaterialId = "") {
        
//         this.width = width;
//         this.depth = depth;
//         this.center = center;
//         //this.floorThickness = floorThickness;
//         this.marginWidth = marginWidth;
//         this.marginDepth = marginDepth;
//         this.uniformWallHeight = uniformWallHeight;
//         this.materialId = materialId;

//         var offsetXMargin = new Vector3(width / 2.0f - marginWidth, 0, 0);
//         var offsetZMargin = new Vector3(0, 0, depth / 2.0f - marginDepth);
        
//         // Clockwise corner
//         // (0,0), (0,1), (1,1), (1,0)
//         this.cornersClockWise = new Vector3[]{
//             center - offsetXMargin - offsetZMargin,
//             center - offsetXMargin + offsetZMargin,
//             center + offsetXMargin + offsetZMargin,
//             center + offsetXMargin - offsetZMargin
//         };
        
//         // Cartesian product generates corners in x, z
//         // (0,0), (0,1), (1,0), (1,1)
//         // var corners = new float[][] {
//         //         new float[]{center.x - offsetX, center.x + offsetX }, 
//         //         new float[]{center.y },
//         //         new float[]{center.z - offsetZ, center.z + offsetZ }
//         // }.CartesianProduct()
//         // .Select(p => p.ToArray())
//         // .Select(p => new Vector3(p[0], p[1], p[2])).ToArray();

//         // // clockwise, swap 3 and 2
//         // var tmp = corners[2];
//         // corners[2] =  corners[3];
//         // corners[3] = tmp;

//         this.walls = cornersClockWise.Zip(
//                 cornersClockWise.Skip(1).Concat(new Vector3[]{ cornersClockWise.FirstOrDefault() }),
//                 (p0, p1) => new Wall(){ height = uniformWallHeight.GetValueOrDefault(0.0f), p0 = p0, p1 = p1, thickness = wallThickness, materialId = wallMaterialId }
//             ).ToArray();
//     }

//     public RectangleRoom(Wall[] walls) {

//         this.walls = walls;

//         // TODO check all y are the same?
//         var minY = walls.SelectMany(w => new Vector3[]{w.p0, w.p1}).Min(p => p.y);
         
//         // Clockwise point sort with determinant (pseduo-cross), move to Room interface general for any convex room
//         this.cornersClockWise = walls
//             .SelectMany(w => new Vector3[]{w.p0, w.p1})
//             .Distinct()
//             .OrderBy(p => p.z)
//             .ThenBy(p => p, Comparer<Vector3>.Create((a, b) => System.Math.Sign( (a.x - center.x) * (b.y - center.y) - (b.x - center.x) * (a.y - center.y)))).ToArray();
        
//         var minPoint = new Vector3(cornersClockWise.Min(c => c.x), minY, cornersClockWise.Min(c => c.z));
//         var maxPoint = new Vector3(cornersClockWise.Max(c => c.x), minY, cornersClockWise.Max(c => c.z));

//         var dimensions = maxPoint - minPoint;

//         this.width = dimensions.x;
//         this.depth = dimensions.z;
//         this.center = minPoint + dimensions / 2.0f;
//         // var m = new Vector3[]{};
//         // var orderByResult = from s in m
//         //            orderby s.x, s.y 
//         //            select new { s };
//         //initRoom(origin, width, height, walls);
//     }

//     // private void initRoom(Vector3 origin, float width, float height, Wall[] walls) {
//     //     this.width = width;
//     //     this.depth = height;
//     //     this.origin = origin;
//     //     this.walls = walls;
//     // }

//     // public Vector3[] clockwiseCornersMargin() {
//     //     return cornersClockWise.Take(2).Select(c => c + new Vector3())
//     // }

   
     
// }

public class AssetMap<T> {
    private Dictionary<string, T> assetMap;
    public AssetMap(Dictionary<string, T> assetMap) {
        this.assetMap = assetMap;
    }

    public T getAsset(string name) {
        return assetMap[name];
    }
}

public static class ProceduralTools {

   
     public static UnityEngine.Mesh GetRectangleFloorMesh(RectangleRoom[] rooms, float yOffset = 0.0f) {
        var mesh = new Mesh();

        var oppositeCorners =  rooms.SelectMany(r => new Vector3[] {
            r.center - new Vector3(r.width/2.0f + r.marginWidth, 0.0f, r.depth/2.0f + r.marginDepth), 
            r.center + new Vector3(r.width/2.0f + r.marginWidth, 0.0f, r.depth/2.0f + r.marginDepth), 
        });

        //TODO check they have same y?
        // var l = oppositeCorners.Select(c => c.y).Distinct();
        // var currentY = l.First();
        // foreach (var y in l) {

        // }
        var minY = oppositeCorners.Min(p => p.y);

        var minPoint = new Vector3(oppositeCorners.Min(c => c.x), minY + yOffset, oppositeCorners.Min(c => c.z));
        var maxPoint = new Vector3(oppositeCorners.Max(c => c.x), minY + yOffset, oppositeCorners.Max(c => c.z));

        var scale = maxPoint - minPoint;

        mesh.vertices = new Vector3[] { 
            minPoint, 
            minPoint + new Vector3(0, 0, scale.z),
            maxPoint,
            minPoint + new Vector3(scale.x, 0, 0)
        };
        mesh.uv = new Vector2[] {new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0)};
        mesh.triangles = new int[] {0, 1, 2, 0, 2, 3};
        mesh.normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };

        return mesh;
    }

    // TODO call above for ceiling using yOffset

     public static UnityEngine.Mesh GetRectangleFloorMesh(RectangleRoom room) {
        return GetRectangleFloorMesh(new RectangleRoom[]{ room });
     }

     public static GameObject CreateVisibilityPointsGameObject(RectangleRoom room, float pointInterval = 1/3.0f) {
        var visibilityPoints = new GameObject("VisibilityPoints");
    
        var step = pointInterval;
        var count = 0;
        var offset = new Vector3(room.width / 2.0f, 0, room.depth / 2.0f);
        for (float x =  room.center.x - offset.x + room.marginWidth; x < room.center.x + offset.x - room.marginWidth + step; x+=step) {
            for (float z = room.center.z - offset.z + room.marginDepth; z <  room.center.z + offset.z - room.marginDepth + step; z+=step) {
                var vp = new GameObject($"VisibilityPoint ({count})");
                vp.transform.position = new Vector3(x, room.center.y, z);
                vp.transform.parent = visibilityPoints.transform;
                count++;
            }
        }
        return visibilityPoints;
     }

     public static GameObject createWalls(Room room, string gameObjectId = "Structure") {
        var structure = new GameObject("GenStructure");
        //room.walls.Where(w => w.empty).


        var zip = room.walls.Zip(
            room.walls.Skip(1).Concat(new Wall[]{ room.walls.FirstOrDefault() }),
            (w0, w1) => (w0, w1)
        ).ToArray();


        
        var zip3 = room.walls.Zip(
            room.walls.Skip(1).Concat(new Wall[]{ room.walls.FirstOrDefault() }),
            (w0, w1) => (w0, w1)
        ).Zip(
            new Wall[]{ room.walls.LastOrDefault() }.Concat(room.walls.Take(room.walls.Length - 1)),
            (wallPair, w2) => (wallPair.w0, w2, wallPair.w1)
        ).ToArray();


        foreach ((Wall w0, Wall w1, Wall w2) in zip3) {
            // TODO query material
            var wallGO = createAndJoinWall(w0, w1, w2);
            wallGO.transform.parent = structure.transform;
        }
        
        // foreach ((Wall w0, Wall w1) in zip) {
        //     // TODO query material
        //     var wallGO = createWall(w0, w1);
        //     wallGO.transform.parent = structure.transform;
        // }

        // createWall(structure, wallHeight, mesh.vertices[0], mesh.vertices[1]);

        // createWall(structure, wallHeight, mesh.vertices[1], mesh.vertices[2]);
        // createWall(structure, wallHeight, mesh.vertices[2], mesh.vertices[3]);
        // createWall(structure, wallHeight, mesh.vertices[3], mesh.vertices[0]);
        return structure;
     }

     private static Vector3? vectorsIntersectionXZ(Vector3 line1P0, Vector3 line1Dir, Vector3 line2P0, Vector3 line2Dir) {
        var denominator = (line2Dir.z + line1Dir.z * line2Dir.x);

        // denominator = (line1P0.x - line1Dir.x) * (line2.p1.x)
                // Lines do not intersect
        if ( Mathf.Abs(denominator) < 1E-4) {
              Debug.Log( " !!! den 0" );
            return null;
        }
        var u = (line1P0.z + line1Dir.z * (line2P0.x - line1P0.x) ) / denominator;
        return line2P0 + u * line2Dir;
     }

     private static Vector3? wallsIntersectionXZ(Wall wall1, Wall wall2) {
        var denominator = -(wall1.p1.x - wall2.p1.x) * (wall2.p1.z - wall2.p0.z) + wall1.p1.z - wall1.p0.z;

        // denominator = (line1P0.x - line1Dir.x) * (line2.p1.x)
                // Lines do not intersect
        if ( Mathf.Abs(denominator) < 1E-4) {
              Debug.Log( " !!! den 0" );
            return null;
        }
        var t = ((wall1.p0.x - wall2.p0.x)*(wall2.p1.z - wall2.p0.z) + wall2.p1.z - wall1.p0.z) / denominator;
        return wall1.p0 + t * (wall1.p1 - wall1.p0);
     }

     private static Vector3? vectorIntersect(Wall wall1, Wall wall2) {
         Vector3 a = wall1.p1 - wall1.p0;
         Vector3 b = wall2.p0 - wall2.p1;
         var c = wall2.p1 - wall1.p0;
        var denominator = Vector3.Dot(a, b);


        // denominator = (line1P0.x - line1Dir.x) * (line2.p1.x)
                // Lines do not intersect
        if ( Mathf.Abs(denominator) < 1E-4) {
              Debug.Log( " !!! den 0" );
            return null;
        }
        var t =  Vector3.Dot(c, b) / denominator;
        return wall1.p0 + t * (wall1.p1 - wall1.p0);
     }


     private static Vector3? wallVectorIntersection(Wall wall1, Wall wall2) {
        var wall2p0p1 = wall2.p1 - wall2.p0;
        var wall1p0p1norm = (wall1.p1 - wall1.p0);
        
        // var normal2 = Vector3.Cross(wall1p0p1norm, wall2p0p1);
        var normal2 = Vector3.Cross(wall2p0p1, Vector3.up);
        var denominator = Vector3.Dot(wall1.p1 - wall1.p0, normal2);
         Debug.Log( " den " + denominator);
        if ( Mathf.Abs(denominator) < 1E-4) {


            var p0p1 =  (wall1.p1 - wall1.p0).normalized;
    
            var normal = Vector3.Cross(p0p1, Vector3.up);
            var nextp0p1 =  (wall2.p1 - wall2.p0).normalized;

        
            var nextNormal = Vector3.Cross(nextp0p1, Vector3.up);

            var sign = Mathf.Sign(nextNormal.x * normal.z - nextNormal.z * normal.x);

            // var sinAngle = nextNormal.x * normal.z - nextNormal.z * normal.x;
            
            Debug.Log( "!!!! 0 den ");
            return wall1.p1 + sign * wall2.thickness * nextp0p1;
        }
         var t = Vector3.Dot(wall2.p0 - wall1.p0, normal2) / denominator;
         
         return wall1.p0 +  (wall1.p1 - wall1.p0) * t;
     }

     public static GameObject createAndJoinWall(Wall toCreate, Wall previous = null, Wall next = null) { 
          var wallGO = new GameObject("Wall");

        var meshF = wallGO.AddComponent<MeshFilter>();
        var meshC = wallGO.AddComponent<MeshCollider>();

        var mesh = meshF.mesh;

        var p0p1 =  (toCreate.p1 - toCreate.p0).normalized;
    
        var normal = Vector3.Cross(p0p1, Vector3.up);
        

        var vertices = new List<Vector3>() { 
            toCreate.p0, toCreate.p0 + new Vector3(0.0f, toCreate.height, 0.0f), toCreate.p1 +  new Vector3(0.0f, toCreate.height, 0.0f), toCreate.p1
        };

        var triangles = new List<int>() {1, 2, 0, 2, 3, 0,  0, 2, 1, 0, 3, 2};
        var uv = new List<Vector2>() {
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)
        };
        var normals = new List<Vector3>() { -normal, -normal, -normal, -normal };

        // if it is a double wall
        if (toCreate.thickness > 0.0001f) {

            var prevThickness = 0.0f;
            Debug.Log(" prev != null "+ (previous != null));
            if (previous != null) {
                var prevp0p1 = (previous.p1 - previous.p0).normalized;
                var prevNormal = Vector3.Cross(prevp0p1, Vector3.up);

                // var p1p0 = -p0p1;

                // //var thicknessExtensionToPrevious = toCreate.thickness * Mathf.Tan(Mathf.Asin(prevNormal.x * normal.z - prevNormal.z * normal.x) / 2.0f);
                // var denominator = (p1p0.z + prevp0p1.z * p1p0.x);
                // // Lines do not intersect
                // if ( Mathf.Abs(denominator) < 1E-4) {

                // }
                // var t = (previous.p0.x + prevp0p1.z * (toCreate.p0.x - previous.p0.x) ) / (p1p0.z + prevp0p1.z * p1p0.x) / denominator;
                // var intersect = vectorsIntersectionXZ(previous.p0 + prevNormal * previous.thickness,
                
                //  prevp0p1,
                //  toCreate.p0 + normal * toCreate.thickness,
                //  -p0p1);
                var intersect = vectorIntersect(previous, toCreate);
                if (intersect != null) {
                    prevThickness = Vector3.Magnitude(intersect.GetValueOrDefault() - toCreate.p0);
                }
                //prevThickness = previous.thickness * Mathf.Tan(Mathf.Asin(prevNormal.x * normal.z - prevNormal.z * normal.x) / 2.0f);
            }

            var nextThickness = 0.0f;
            if (next != null) {
                var nextp0p1 = (next.p1 - next.p0).normalized;
                var nextNormal = Vector3.Cross(nextp0p1, Vector3.up);

                // var p1p0 = -p0p1;

                // //var thicknessExtensionToPrevious = toCreate.thickness * Mathf.Tan(Mathf.Asin(prevNormal.x * normal.z - prevNormal.z * normal.x) / 2.0f);
                // var denominator = (p1p0.z + prevp0p1.z * p1p0.x);
                // // Lines do not intersect
                // if ( Mathf.Abs(denominator) < 1E-4) {

                // }
                // var t = (previous.p0.x + prevp0p1.z * (toCreate.p0.x - previous.p0.x) ) / (p1p0.z + prevp0p1.z * p1p0.x) / denominator;
                // var intersect = vectorsIntersectionXZ(
                //     next.p0 + nextNormal * next.thickness,
                //     nextp0p1,
                //     toCreate.p1 + normal * toCreate.thickness,
                //     p0p1
                // );
                var intersect = vectorIntersect(toCreate, next);
                if (intersect != null) {
                    nextThickness = Vector3.Magnitude(intersect.GetValueOrDefault() - toCreate.p1);
                }

                //nextThickness = next.thickness * Mathf.Tan(Mathf.Asin(nextNormal.x * normal.z - nextNormal.z * normal.x) / 2.0f);
            }


            // var nextp0p1 =  (next.p1 - next.p0).normalized;
        
            // var nextNormal = Vector3.Cross(nextp0p1, Vector3.up);

            // Debug.Log("tan " + 
            //     Mathf.Tan(Mathf.Asin( Vector3.Cross(normal, nextNormal).magnitude ) / 2.0f) +
            //      " norm " + normal + " nextNorm " + nextNormal + " 2d cross " + (nextNormal.x * normal.z - nextNormal.z * normal.x) + " revers 2d cross " + (normal.x * nextNormal.z - normal.z * nextNormal.x));
            
            // var thicknessAngleindependent = toCreate.thickness * Mathf.Tan(Mathf.Asin(nextNormal.x * normal.z - nextNormal.z * normal.x) / 2.0f);


            var p0Thickness = toCreate.p0 + normal * toCreate.thickness - p0p1 * prevThickness;
            var p1Thickness = toCreate.p1 + normal * toCreate.thickness + p0p1 * nextThickness;
            // var p0Thickness0 = wall.p0 + normal * thicknessAngleindependent - p0p1 * thicknessAngleindependent;
            // var p1Thickness1 = wall.p1 + normal * thicknessAngleindependent + p0p1 * thicknessAngleindependent;
           vertices.AddRange(
                new Vector3[] { 
                    p0Thickness, p0Thickness + new Vector3(0.0f, toCreate.height, 0.0f), p1Thickness +  new Vector3(0.0f, toCreate.height, 0.0f), p1Thickness
                }
            );

            uv.AddRange(new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
            // mesh.uv = mesh.uv.Concat(new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) }).ToArray();
            normals.AddRange(new Vector3[] {normal, normal, normal, normal});
            triangles.AddRange(new int[] { 4, 6, 5, 4, 7, 6, 5, 6, 4, 6, 7, 4});
        }

        mesh.vertices = vertices.ToArray();
        mesh.uv = uv.ToArray();
        mesh.normals = normals.ToArray();
        mesh.triangles = triangles.ToArray();
                var meshRenderer = wallGO.AddComponent<MeshRenderer>();
        // TODO use a material loader that has this dictionary
        //var mats = ProceduralTools.FindAssetsByType<Material>().ToDictionary(m => m.name, m => m);
        var mats =  ProceduralTools.FindAssetsByType<Material>().GroupBy(m => m.name).ToDictionary(m => m.Key, m => m.First());
        //if (mats.ContainsKey(wall.materialId)) {
            //Debug.Log("MAT query " +  wall.materialId+ " " + string.Join(",", mats.Select(m => m.Value.name).ToArray()) + " len " + mats.Count);
            meshRenderer.material = mats[toCreate.materialId];
        //}
    
        return wallGO;
     }

     public static GameObject createWall(Wall wall, Wall next = null) {

        var wallGO = new GameObject("Wall");

        var meshF = wallGO.AddComponent<MeshFilter>();
        var meshC = wallGO.AddComponent<MeshCollider>();

        var mesh = meshF.mesh;

        var p0p1 =  (wall.p1 - wall.p0).normalized;
    
        var normal = Vector3.Cross(p0p1, Vector3.up);
        

        var vertices = new List<Vector3>() { 
            wall.p0, wall.p0 + new Vector3(0.0f, wall.height, 0.0f), wall.p1 +  new Vector3(0.0f, wall.height, 0.0f), wall.p1
        };

        var triangles = new List<int>() {1, 2, 0, 2, 3, 0};
        var uv = new List<Vector2>() {
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)
        };
        var normals = new List<Vector3>() { -normal, -normal, -normal, -normal };

        // if it is a double wall
        if (wall.thickness > 0.0001f) {


            var nextp0p1 =  (next.p1 - next.p0).normalized;
        
            var nextNormal = Vector3.Cross(nextp0p1, Vector3.up);

            Debug.Log("tan " + 
                Mathf.Tan(Mathf.Asin( Vector3.Cross(normal, nextNormal).magnitude ) / 2.0f) +
                 " norm " + normal + " nextNorm " + nextNormal + " 2d cross " + (nextNormal.x * normal.z - nextNormal.z * normal.x) + " revers 2d cross " + (normal.x * nextNormal.z - normal.z * nextNormal.x));
            
            var thicknessAngleindependent = wall.thickness * Mathf.Tan(Mathf.Asin(nextNormal.x * normal.z - nextNormal.z * normal.x) / 2.0f);


            var p0Thickness = wall.p0 + normal * wall.thickness - p0p1 * thicknessAngleindependent;
            var p1Thickness = wall.p1 + normal * wall.thickness + p0p1 * thicknessAngleindependent;
            // var p0Thickness0 = wall.p0 + normal * thicknessAngleindependent - p0p1 * thicknessAngleindependent;
            // var p1Thickness1 = wall.p1 + normal * thicknessAngleindependent + p0p1 * thicknessAngleindependent;
           vertices.AddRange(
                new Vector3[] { 
                    p0Thickness, p0Thickness + new Vector3(0.0f, wall.height, 0.0f), p1Thickness +  new Vector3(0.0f, wall.height, 0.0f), p1Thickness
                }
            );

            uv.AddRange(new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
            // mesh.uv = mesh.uv.Concat(new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) }).ToArray();
            normals.AddRange(new Vector3[] {normal, normal, normal, normal});
            triangles.AddRange(new int[] { 4, 6, 5, 4, 7, 6});
        }

        mesh.vertices = vertices.ToArray();
        mesh.uv = uv.ToArray();
        mesh.normals = normals.ToArray();
        mesh.triangles = triangles.ToArray();
                var meshRenderer = wallGO.AddComponent<MeshRenderer>();
        // TODO use a material loader that has this dictionary
        //var mats = ProceduralTools.FindAssetsByType<Material>().ToDictionary(m => m.name, m => m);
        var mats =  ProceduralTools.FindAssetsByType<Material>().GroupBy(m => m.name).ToDictionary(m => m.Key, m => m.First());
        //if (mats.ContainsKey(wall.materialId)) {
            //Debug.Log("MAT query " +  wall.materialId+ " " + string.Join(",", mats.Select(m => m.Value.name).ToArray()) + " len " + mats.Count);
            meshRenderer.material = mats[wall.materialId];
        //}
    
        return wallGO;
    }

    public static GameObject createFloorCollider(GameObject floorGameObject, RectangleRoom room, float thickness) {
        var colliders = new GameObject("Colliders");

        var collider = new GameObject("Col");
        var box = collider.AddComponent<BoxCollider>();

        var size = new Vector3(room.width + room.marginWidth * 2.0f, thickness, room.depth + room.marginDepth * 2.0f);

        var center = room.center - new Vector3(0, thickness/2.0f, 0);
        // TODO set collider params
        box.size = size;
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

        return colliders;
    } 

    public static GameObject createFloorReceptacle(
    GameObject floorGameObject, RectangleRoom room, float height
) {
    var receptacleTriggerBox = new GameObject("ReceptacleTriggerBox");
    var receptacleCollider = receptacleTriggerBox.AddComponent<BoxCollider>();
    receptacleCollider.isTrigger = true;
    var widthMinusMargin = room.width - 2.0f * room.marginWidth;

    var depthMinusMargin = room.depth - 2.0f * room.marginDepth;

    receptacleCollider.size = new Vector3(widthMinusMargin, height, depthMinusMargin);
    receptacleCollider.center = room.center + new Vector3(0, height/2.0f, 0);
    
    receptacleTriggerBox.transform.parent = floorGameObject.transform;
    return receptacleTriggerBox;
}

    public static SimObjPhysics setRoomSimObjectPhysics(
        GameObject floorGameObject, 
        string simObjId,
        GameObject visibilityPoints, 
        GameObject receptacleTriggerBox, 
        Collider collider
    ) {
            var boundingBox = new GameObject("BoundingBox");
            var bbCollider = boundingBox.AddComponent<BoxCollider>();
            bbCollider.enabled = false;
            boundingBox.transform.parent = floorGameObject.transform;

            var simObjPhysics = floorGameObject.AddComponent<SimObjPhysics>();
            simObjPhysics.objectID = simObjId;
            simObjPhysics.ObjType = SimObjType.Floor;
            simObjPhysics.PrimaryProperty = SimObjPrimaryProperty.Static;
            simObjPhysics.SecondaryProperties = new SimObjSecondaryProperty[] { SimObjSecondaryProperty.Receptacle };

            simObjPhysics.BoundingBox = boundingBox;

            simObjPhysics.VisibilityPoints = visibilityPoints.GetComponentsInChildren<Transform>();

            simObjPhysics.ReceptacleTriggerBoxes = new GameObject[] { receptacleTriggerBox };
            simObjPhysics.MyColliders = new Collider[] { collider };

            simObjPhysics.transform.parent = floorGameObject.transform;

            receptacleTriggerBox.AddComponent<Contains>();
            return simObjPhysics;
    }

    // 
    public static GameObject createFloorGameObject(string name = "Floor", Vector3? position = null) {
       
        var floorGameObject = new GameObject(name);
        floorGameObject.transform.position = position.GetValueOrDefault();

        floorGameObject.tag = "SimObjPhysics";
        floorGameObject.layer = 8;

        var rb = floorGameObject.AddComponent<Rigidbody>();
        rb.mass = 1.0f;
        rb.angularDrag = 0.05f;
        rb.useGravity = true;
        rb.isKinematic = true;
        
        floorGameObject.AddComponent<MeshFilter>();

        floorGameObject.AddComponent<MeshRenderer>();
        
        return floorGameObject;
    }
    public static GameObject createFloorGameObject(string name, RectangleRoom room, AssetMap<Material> materialDb, string simObjId, float receptacleHeight = 0.7f, float floorColliderThickness = 1.0f, Vector3? position = null) {
        var floorGameObject = createFloorGameObject(name, position);
        
        floorGameObject.GetComponent<MeshFilter>().mesh = ProceduralTools.GetRectangleFloorMesh(room);
        // TODO generate ceiling

        var meshRenderer = floorGameObject.GetComponent<MeshRenderer>();
        meshRenderer.material = materialDb.getAsset(room.rectangleFloor.materialId);

        var visibilityPoints = ProceduralTools.CreateVisibilityPointsGameObject(room);
        visibilityPoints.transform.parent = floorGameObject.transform;

        var receptacleTriggerBox = ProceduralTools.createFloorReceptacle(floorGameObject, room, receptacleHeight);
        var collider = ProceduralTools.createFloorCollider(floorGameObject, room, floorColliderThickness);

        ProceduralTools.setRoomSimObjectPhysics(floorGameObject, simObjId, visibilityPoints, receptacleTriggerBox, collider.GetComponentInChildren<Collider>());

        receptacleTriggerBox.AddComponent<Contains>();

        ProceduralTools.createWalls(room, "Structure");
        return floorGameObject;
    }

    public static List<T> FindAssetsByType<T>() where T : UnityEngine.Object
    {
        List<T> assets = new List<T>();
        string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof (T).ToString().Replace("UnityEngine.", "")));
        for( int i = 0; i < guids.Length; i++ )
        {
            string assetPath = AssetDatabase.GUIDToAssetPath( guids[i] );
            T asset = AssetDatabase.LoadAssetAtPath<T>( assetPath );
            if( asset != null )
            {
                assets.Add(asset);
            }
        }
        return assets;
    }

    public static List<GameObject> FindPrefabsInAssets() {
        var assets = new List<GameObject>();
        string[] guids = AssetDatabase.FindAssets("t:prefab");
        for( int i = 0; i < guids.Length; i++ )
        {
            string assetPath = AssetDatabase.GUIDToAssetPath( guids[i] );
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>( assetPath );
            if( asset != null )
            {
                assets.Add(asset);
            }
        }
        return assets;
    }
    
    public static GameObject spawnObjectAtReceptacle(AssetMap<GameObject> goDb, string objectId, SimObjPhysics receptacleSimObj) {
        var spawnCoordinates = receptacleSimObj.FindMySpawnPointsFromTopOfTriggerBox();
        var go = goDb.getAsset(objectId);
        var pos = spawnCoordinates.Shuffle_().First();
        //GameObject.Instantiate(go, pos, Quaternion.identity);
        var fpsAgent = GameObject.FindObjectOfType<PhysicsRemoteFPSAgentController>();

        var sceneManager =  GameObject.FindObjectOfType<PhysicsSceneManager>();
        var initialSpawnPosition = new Vector3(0, 3, 0);

        
        var spawned = GameObject.Instantiate(go, initialSpawnPosition, Quaternion.identity);
        var toSpawn = spawned.GetComponent<SimObjPhysics>();
        Rigidbody rb = spawned.GetComponent<Rigidbody>();

        
        var success = false;

        for(int i = 0; i < spawnCoordinates.Count; i++)
        {
            //place object at the given point, this also checks the spawn area to see if its clear
            //if not clear, it will return false
            if(fpsAgent.placeNewObjectAtPoint(toSpawn, spawnCoordinates[i]))
            {
                //we set success to true, if one of the corners doesn't fit on the table
                //this will be switched to false and will be returned at the end
                success = true;

                //double check if all corners of spawned object's bounding box are
                //above the targetReceptacle table
                //note this only accesses the very first receptacle trigger box, so
                //for EXPERIMENT ROOM TABLES make sure there is only one
                //receptacle trigger box on the square table
                List<Vector3> corners = GetCorners(toSpawn);

                Contains con = receptacleSimObj.ReceptacleTriggerBoxes[0].GetComponent<Contains>();
                bool cornerCheck = true;
                foreach(Vector3 p in corners)
                {
                    if(!con.CheckIfPointIsAboveReceptacleTriggerBox(p))
                    {
                        cornerCheck = false;
                        //this position would cause object to fall off table
                        //double back and reset object to try again with another point
                        spawned.transform.position = initialSpawnPosition;
                        break;
                    }
                }

                if(!cornerCheck)
                {
                    success = false;
                    continue;
                }
            }

            //if all corners were succesful, break out of this loop, don't keep trying
            if(success)
            {
                rb.isKinematic = false;
                //run scene setup to grab reference to object and give it objectId
                sceneManager.SetupScene();
                sceneManager.ResetObjectIdToSimObjPhysics();
                break;
            }
        }

        return null;
    }

    private static List<Vector3> GetCorners(SimObjPhysics sop)
    {
        //get corners of the bounding box of the object spawned in
        GameObject bb = sop.BoundingBox.transform.gameObject;
        BoxCollider bbcol = bb.GetComponent<BoxCollider>();
        Vector3 bbCenter = bbcol.center;
        Vector3 bbCenterTransformPoint = bb.transform.TransformPoint(bbCenter);
        //keep track of all 8 corners of the OverlapBox
        List<Vector3> corners = new List<Vector3>();
        //bottom forward right
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(bbcol.size.x, -bbcol.size.y, bbcol.size.z) * 0.5f));
        //bottom forward left
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(-bbcol.size.x, -bbcol.size.y, bbcol.size.z) * 0.5f));
        //bottom back left
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(-bbcol.size.x, -bbcol.size.y, -bbcol.size.z) * 0.5f));
        //bottom back right
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(bbcol.size.x, -bbcol.size.y, -bbcol.size.z) * 0.5f));
        //top forward right
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(bbcol.size.x, bbcol.size.y, bbcol.size.z) * 0.5f));
        //top forward left
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(-bbcol.size.x, bbcol.size.y, bbcol.size.z) * 0.5f));
        //top back left
        corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(-bbcol.size.x, bbcol.size.y, -bbcol.size.z) * 0.5f));
        //top back right
        corners.Add(bb.transform.TransformPoint(bbCenter+ new Vector3(bbcol.size.x, bbcol.size.y, -bbcol.size.z) * 0.5f));

        return corners;
    }
}

// public static GameObject createRooms(RectangleRoom[] rooms) {
    
// }

// public static GameObject createRoom(Vector3 origin, )

}