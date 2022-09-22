using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityStandardAssets.Characters.FirstPerson;
using System;
 using System.Runtime.Serialization.Formatters.Binary;
 using System.IO;
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
    public class ProbeParameters {
        public string id;
        public Vector3 position;
        public float intensity;
        public Vector3 boxSize;
        public Vector3 boxOffset;
        public float shadowDistance;
        public SerializableColor background;
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class LightParameters {
        public string id { get; set; }
        public string type { get; set; }
        public Vector3 position { get; set; }

        public string[] cullingMaskOff { get; set;}

        public FlexibleRotation rotation;
        public float intensity { get; set; }
        public float indirectMultiplier { get; set; }
        public float range { get; set; }
        public SerializableColor rgb { get; set; }
        public ShadowParameters shadow = null;
        public string linkedObjectId { get; set; }
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class ShadowParameters {
        public string type { get; set; } = "Soft";
        public float strength { get; set; } = 1.0f;

        public float normalBias { get; set; } = 0.4f;
        public float bias { get; set; } = 0.05f;
        public float nearPlane { get; set; } = 0.2f;
        public string resolution { get; set; } = "FromQualitySettings";
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class SerializableColor {
        public float r { get; set; }
        public float g { get; set; }
        public float b { get; set; }
        public float a { get; set; } = 1.0f;

        public Color toUnityColor() {
            return new Color(r, g, b, a);
        }

        public static SerializableColor fromUnityColor(Color color) {
            return new SerializableColor() {r = color.r, g = color.g, b = color.b, a = color.a};
        }
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
    public class Door : WallRectangularHole {
        public string id { get; set; }
        public Vector3 assetOffset { get; set; }
        public string room0 { get; set; }
        public string room1 { get; set; }
        public string wall0 { get; set; }
        public string wall1 { get; set; }
        public BoundingBox boundingBox { get; set; }

        public List<VectorXZ> axesXZ { get; set; }
        public string type { get; set; }
        public bool openable { get; set; }
        public float openness { get; set; } = 0.0f;
        public string assetId { get; set; }

        public SerializableColor color { get; set; } = null;
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class ProceduralParameters {
        public float floorColliderThickness { get; set; }
        public float minWallColliderThickness { get; set; }
        public float receptacleHeight { get; set; }
        public string skyboxId { get; set; }
        public string datetime { get; set; }
        public List<LightParameters> lights { get; set; }

        public List<ProbeParameters> reflections;

        public string ceilingMaterial { get; set; }
        public float? ceilingMaterialTilingXDivisor = 1.0f;
        public float? ceilingMaterialTilingYDivisor = 1.0f;
        public SerializableColor ceilingColor { get; set; } = null;
        public float navmeshVoxelSize { get; set; }
        public bool ceilingBackFaces { get; set; }

        public bool unlitCeiling { get; set; }

        public bool squareTiling = false;
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class Ceiling {
        public string id { get; set; }
        public List<Vector3> polygon { get; set; }
        public string material { get; set; }
        public MaterialProperties materialProperties;
        public float? tilingDivisorX { get; set; }
        public float? tilingDivisorY { get; set; }
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class RoomHierarchy {
        public string id { get; set; }
        public string roomType { get; set; }
        public MaterialProperties materialProperties;
        public string floorMaterial { get; set; }
        public float? floorMaterialTilingXDivisor = 1.0f;
        public float? floorMaterialTilingYDivisor = 1.0f;

        public string layer { get; set; }

        public SerializableColor floorColor { get; set; } = null;
        // public float y { get; set; }
        public List<Vector3> floorPolygon { get; set; }
        public List<Ceiling> ceilings { get; set; }
        public List<RoomHierarchy> rooms = null;
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class PolygonWall {
        public string id { get; set; }
        public List<Vector3> polygon { get; set; }
        public string roomId { get; set; }
        public float thickness { get; set; }
        public string material { get; set; }

        public string layer { get; set; }

        public MaterialProperties materialProperties;

        public bool empty { get; set; } = false;

        public float materialTilingXDivisor = 1.0f;
        public float materialTilingYDivisor = 1.0f;

        public SerializableColor color { get; set; } = null;

        public bool unlit;
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class BoundingBox {
        public Vector3 min { get; set; }
        public Vector3 max { get; set; }

        public Vector3 center() {
            return this.min + (this.max - this.min) / 2.0f;
        }

        public Vector3 size() {
            return (this.max - this.min);
        }
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class BoundingBoxWithOffset {
        public Vector3 min { get; set; }
        public Vector3 max { get; set; }
        public Vector3 offset {get; set; }
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class VectorXZ {
        public int x;
        public int z;
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class Window : WallRectangularHole {
        public string id { get; set; }
        public string room0 { get; set; }
        public string room1 { get; set; }
        public BoundingBox boundingBox { get; set; }
        public Vector3 assetOffset { get; set; }
        public string wall0 { get; set; }
        public string wall1 { get; set; }
        public bool openable { get; set; }
        public float openness { get; set; } = 0.0f;
        public List<VectorXZ> axesXZ { get; set; }
        public string type { get; set; }
        public string assetId { get; set; }

        public SerializableColor color { get; set; } = null;
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class FlexibleRotation {
        // Support Angle-Axis rotation (axis, degrees)
        public Vector3 axis;
        public float degrees;

        // Support for Vector3 rotation
        private float? _x;
        private float? _y;
        private float? _z;
        public float? x {
            get { return _x; }
            set {
                _x = value;
                setAngleAxis();
            }
        }
        public float? y {
            get { return _y; }
            set {
                _y = value;
                setAngleAxis();
            }
        }
        public float? z {
            get { return _z; }
            set {
                _z = value;
                setAngleAxis();
            }
        }

        private void setAngleAxis() {
            if (x == null || y == null || z == null) {
                return;
            }
            Vector3 rot = new Vector3(x: x.Value, y: y.Value, z: z.Value);
            Quaternion.Euler(rot).ToAngleAxis(out degrees, out axis);
            if (
                (axis.x == Mathf.Infinity || axis.x == -Mathf.Infinity || float.IsNaN(axis.x))
                && (axis.y == Mathf.Infinity || axis.y == -Mathf.Infinity || float.IsNaN(axis.y))
                && (axis.z == Mathf.Infinity || axis.z == -Mathf.Infinity || float.IsNaN(axis.z))
            ) {
                axis = Vector3.up;
            }
            if (degrees == Mathf.Infinity || degrees == -Mathf.Infinity || float.IsNaN(degrees)) {
                degrees = 0;
            }
        }

        public static FlexibleRotation fromQuaternion(Quaternion quat) {
            var r = new FlexibleRotation();
            quat.ToAngleAxis(out r.degrees, out r.axis);
            return r;
        }

        public Quaternion toQuaternion() {
            return Quaternion.AngleAxis(degrees, axis);
        }
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
        public FlexibleRotation rotation { get; set; }
        public bool kinematic { get; set; } //should the rigidbody be kinematic or not
        public BoundingBox boundingBox { get; set; }
        public string room { get; set; }
        public List<HouseObject> children { get; set; }
        public List<Taxonomy> types { get; set; }
        public string assetId { get; set; } //name of prefab asset from asset database
        public string navmeshArea { get; set; }

        public string layer { get; set; }

        public float? openness { get; set; } = null;
        public bool? isOn { get; set; } = null;
        public bool? isDirty { get; set; } = null;
        
        public bool unlit;
        public SerializableColor color { get; set; } = null;
        public MaterialProperties materialProperties;
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class Roof {
        public float thickness { get; set; }
        public string material { get; set; }
        public string assetId { get; set; }
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class ProceduralHouse {
        public ProceduralParameters proceduralParameters { get; set; }
        public string id { get; set; }
        public List<RoomHierarchy> rooms { get; set; } = new List<RoomHierarchy>();
        public List<PolygonWall> walls { get; set; } = new List<PolygonWall>();
        public List<Door> doors { get; set; } = new List<Door>();
        public List<Window> windows { get; set; } = new List<Window>();
        public List<HouseObject> objects { get; set; } = new List<HouseObject>();
        public Roof roof { get; set; }
        public HouseMetadata metadata { get; set; }
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class HouseMetadata {
        public Dictionary<string, AgentPose> agentPoses { get; set; }
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class AgentPose {
        public float horizon;
        public Vector3 position;
        public Vector3 rotation;
        public bool? standing = null;
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class House {
        public RectangleRoom[] rooms;
        public string ceilingMaterialId;
        public string id;
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class MaterialProperties {
        // TODO: move material id, color (as albedo) and tiling divisors 
        public float metallic;
        public float smoothness;
    }

    // TODO more general
    // public class House<T> where T : Room
    // {

    // }
    public interface WallRectangularHole {
        string id { get; set; }
        string assetId { get; set; }
        string room0 { get; set; }
        string room1 { get; set; }
        string wall0 { get; set; }
        string wall1 { get; set; }

        BoundingBox boundingBox { get; set; }

        Vector3 assetOffset { get; set; }

        float openness { get; set; }

        SerializableColor color { get; set; }
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class Wall {
        public string id;
        public float height;
        public Vector3 p0;
        public Vector3 p1;
        public float thickness;
        public bool empty;
        public WallRectangularHole hole = null;
        public string materialId;
        public MaterialProperties materialProperties;

        public string roomId;

        public string layer { get; set; }

        public float materialTilingXDivisor = 1.0f;
        public float materialTilingYDivisor = 1.0f;

        public SerializableColor color { get; set; } = null;

        public bool unlit;
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

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    // TODO move to this interface
    public class RectangleRoom : Room {
        public RectangleFloor rectangleFloor { get; set; }

        public Wall[] walls { get; set; }

        public string type { get; set; }

        // public string layer { get; set; }

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


        public static RectangleRoom roomFromWallPoints(
            IEnumerable<Vector3> cornersClockWise,
            float wallHeight,
            float wallThickness,
            string floorMaterialId,
            string wallMaterialId,
            float marginWidth = 0.0f,
            float marginDepth = 0.0f
        ) {

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

        public static Wall[] wallsFromContiguousPoints(
            Vector3[] corners, float wallHeight, float wallThickness, string wallMaterialId
        ) {
            var centroid = corners.Aggregate(
                Vector3.zero, (accumulator, c) => accumulator + c
            ) / corners.Length;


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

    public static class ExtensionMethods {
        public static T DeepClone<T>(this T obj)
        {
            // Don't serialize a null object, simply return the default for that object
            if (ReferenceEquals(obj, null)) {
                return default;
            } 

            // initialize inner objects individually
            // for example in default constructor some list property initialized with some values,
            // but in 'source' these items are cleaned -
            // without ObjectCreationHandling.Replace default constructor values will be added to result
            var jsonResolver = new ShouldSerializeContractResolver();
            var str = Newtonsoft.Json.JsonConvert.SerializeObject(
                obj,
                Newtonsoft.Json.Formatting.None,
                new Newtonsoft.Json.JsonSerializerSettings() {
                    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                    ContractResolver = jsonResolver
                }
            );

            var jObj = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(str);

            return jObj.ToObject<T>();
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default(TValue))
        {
            TValue value;
            return dictionary.TryGetValue(key, out value) ? value : defaultValue;
        }

        public static int AddCount<TKey>(this Dictionary<TKey, int> dictionary, TKey key, int count = 1)
        {
            int value;
            dictionary.TryGetValue(key, out value);
            if (dictionary.ContainsKey(key)) {
                dictionary[key] = dictionary[key] + count;
            }
            else {
                dictionary[key] = count;
            }
            return dictionary[key];
        }
    }
    
}
