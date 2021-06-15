using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityStandardAssets.Characters.FirstPerson;
using System;
using MessagePack.Resolvers;
using MessagePack.Formatters;
using MessagePack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Thor.Procedural.Data {
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class AssetMetadata {
        public string id;

        public string type;

        public string primaryProperty;

        public List<string> secondaryProperties;

        public BoundingBox boundingBox;
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class LightParameters {
        public string id { get; set; }
        public string room_id { get; set; }
        public string type { get; set; }
        public Vector3 position { get; set; }
        public float intensity { get; set; }
        public Color rgb { get; set; }
        public string asset_id { get; set; }
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class Margin {
        public float bottom { get; set; }
        public float top { get; set; }
        public float left { get; set; }
        public float right { get; set; }
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class Door {
        public string id { get; set; }
        public Margin margin { get; set; }
        public string room_0 { get; set; }
        public string room_1 { get; set; }
        public BoundingBox bounding_box { get; set; }
        public string wall_id { get; set; }
        public List<VectorXZ> axes_xz { get; set; }
        public string type { get; set; }
        public bool openable { get; set; }
        public string asset_id { get; set; }
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class ProceduralParameters {
        public float floor_collider_thickness { get; set; }
        public float receptacle_height { get; set; }
        public string skybox_id { get; set; }
        public string datetime { get; set; }
        public List<LightParameters> lights { get; set; }

        public string ceiling_material { get; set; }
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class Ceiling {
        public string id { get; set; }
        public List<Vector3> polygon { get; set; }
        public string material { get; set; }
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class RoomHierarchy {
        public string id { get; set; }
        public string type { get; set; }
        public string floor_material { get; set; }
        // public float y { get; set; }
        public List<Vector3> floor_polygon { get; set; }
        public List<Ceiling> ceilings { get; set; }
        public List<RoomHierarchy> rooms = null;
    }

    // public class m {
    //     Vector3 
    // }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class PolygonWall {
        public string id { get; set; }
        public List<Vector3> polygon { get; set; }
        public string room_id { get; set; }
        public float thickness { get; set; }
        public string material { get; set; }
        public bool empty { get; set; }
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class BoundingBox {
        public Vector3 min { get; set; }
        public Vector3 max { get; set; }
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class VectorXZ {
        public int x;
        public int z;
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class Window {
        public string id { get; set; }
        public string room_0 { get; set; }
        public string room_1 { get; set; }
        public BoundingBox bounding_box { get; set; }
        public string wall_id { get; set; }
        public bool openable { get; set; }
        public List<VectorXZ> axes_xz { get; set; }
        public string type { get; set; }
        public string asset_id { get; set; }
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class AxisAngleRotation {
        public Vector3 axis;
        public float degrees;
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class Child {
        public string type { get; set; }
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class Taxonomy {
        public string name;
        public List<Taxonomy> children = null;
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class HouseObject {
        public string id { get; set; } //set to SimObjPhysics.objectId
        public Vector3 position { get; set; }
        public AxisAngleRotation rotation { get; set; }
        public bool kinematic { get; set; } //should the rigidbody be kinematic or not
        public BoundingBox bounding_box { get; set; }
        public string room { get; set; }
        public List<object> children { get; set; }
        public List<Taxonomy> types { get; set; }
        public string asset_id { get; set; } //name of prefab asset from asset database
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class Roof {
        public float thickness { get; set; }
        public string material { get; set; }
        public string asset_id { get; set; }
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class ProceduralHouse {
        public ProceduralParameters procedural_parameters { get; set; }
        public string id { get; set; }
        public List<RoomHierarchy> rooms { get; set; }
        public List<PolygonWall> walls { get; set; }
        public List<Door> doors { get; set; }
        public List<Window> windows { get; set; }
        public List<HouseObject> objects { get; set; }
        public Roof roof { get; set; }
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class House {
        public RectangleRoom[] rooms;
        public string ceilingMaterialId;
        public string id;
    }

    // TODO more general
    // public class House<T> where T : Room
    // {

    // }


    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
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

    [Serializable]

    [MessagePackObject(keyAsPropertyName: true)]
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

        [NonSerialized()]
        private Vector3[] _corners;

        public static Wall[] createSurroundingWalls(Floor floor, string wallMaterialId, float wallHeight, float wallThickness = 0.0f) {
            return floor.cornersClockwise.Zip(
                floor.cornersClockwise.Skip(1).Concat(new Vector3[] { floor.cornersClockwise.FirstOrDefault() }),
                (p0, p1) => new Wall() { height = wallHeight, p0 = p0, p1 = p1, thickness = wallThickness, materialId = wallMaterialId }
            ).ToArray();
        }

    }

    public interface Room {


        Wall[] walls { get; }
        Floor floor { get; }

        Vector3 center { get; }

        string type { get; }

        string id { get; }

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

    // public class House<T> where T : Room
    // {

    // }

    [Serializable]

    [MessagePackObject(keyAsPropertyName: true)]
    // TODO move to this interface
    public class RectangleRoom : Room {
        public RectangleFloor rectangleFloor { get; set; }

        public Wall[] walls { get; set; }

        public string type { get; set; }

        public string id { get; set; }
        public Floor floor { get { return rectangleFloor; } }

        public Vector3 center { get { return rectangleFloor.center; } }

        public float width { get { return rectangleFloor.width; } }
        public float depth { get { return rectangleFloor.depth; } }


        // TODO decide if needed here
        // public float floorThickness {get;}

        public float marginWidth { get { return rectangleFloor.marginWidth; } }

        public float marginDepth { get { return rectangleFloor.marginDepth; } }

        public Vector3[] cornersClockWise { get { return rectangleFloor.cornersClockwise; } }


        public static RectangleRoom roomFromWallPoints(IEnumerable<Vector3> cornersClockWise, float wallHeight, float wallThickness, string floorMaterialId, string wallMaterialId, float marginWidth = 0.0f, float marginDepth = 0.0f) {

            var centroid = cornersClockWise.Aggregate(Vector3.zero, (accumulator, c) => accumulator + c) / cornersClockWise.Count();


            // var cornersClockWise = corners
            //     .OrderBy(p => p.z)
            //     .ThenBy(p => p, Comparer<Vector3>.Create((a, b) => System.Math.Sign( (a.x - center.x) * (b.y - center.y) - (b.x - center.x) * (a.y - center.y)))).ToArray();
            // var cornersClockWise = corners;

            // TODO what to do when different heights?
            var minY = cornersClockWise.Min(p => p.y);

            var minPoint = new Vector3(cornersClockWise.Min(c => c.x), minY, cornersClockWise.Min(c => c.z));
            var maxPoint = new Vector3(cornersClockWise.Max(c => c.x), minY, cornersClockWise.Max(c => c.z));

            var dimensions = maxPoint - minPoint;

            var floor = new RectangleFloor() { center = minPoint + dimensions / 2.0f, width = dimensions.x, depth = dimensions.z, marginWidth = marginWidth, marginDepth = marginDepth, materialId = floorMaterialId };

            var walls = cornersClockWise.Zip(
                cornersClockWise.Skip(1).Concat(new Vector3[] { cornersClockWise.FirstOrDefault() }),
                (p0, p1) => new Wall() { height = wallHeight, p0 = p0, p1 = p1, thickness = wallThickness, materialId = wallMaterialId }
            ).ToArray();

            return new RectangleRoom() { walls = walls, rectangleFloor = floor };
            // var m = new Vector3[]{};
            // var orderByResult = from s in m
            //            orderby s.x, s.y 
            //            select new { s };
            //initRoom(origin, width, height, walls);
        }

        public static RectangleRoom roomFromWalls(Wall[] walls, string floorMaterialId, float marginWidth = 0.0f, float marginDepth = 0.0f) {

            //     public RectangleRoom(Wall[] walls) {

            var wallPoints = walls.SelectMany(w => new Vector3[] { w.p0, w.p1 }).Distinct();

            // TODO check all y are the same?
            var minY = walls.SelectMany(w => new Vector3[] { w.p0, w.p1 }).Min(p => p.y);


            // var centroid = wallPoints.Aggregate(Vector3.zero, (accumulator, c) => accumulator + c) / wallPoints.Count();

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
                depth = dimensions.z, marginWidth = marginWidth, marginDepth = marginDepth, materialId = floorMaterialId
            };


            return new RectangleRoom() { walls = walls, rectangleFloor = floor };

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
                cornersClockWise.Skip(1).Concat(new Vector3[] { cornersClockWise.FirstOrDefault() }),
                (p0, p1) => new Wall() { height = wallHeight, p0 = p0, p1 = p1, thickness = wallThickness, materialId = wallMaterialId }
            ).ToArray();

        }

        // private void initRoom(Vector3 origin, float width, float height, Wall[] walls) {
        //     this.width = width;
        //     this.depth = height;
        //     this.origin = origin;
        //     this.walls = walls;
        // }
    }

}