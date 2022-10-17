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
        public Vector3 localPosition { get; set; }
        public string[] cullingMaskOff { get; set;}
        public FlexibleRotation rotation;
        public float intensity { get; set; }
        public float indirectMultiplier { get; set; }
        public float range { get; set; }
        public float spotAngle { get; set; } //only used for spot lights, [1-179] valid range 
        public SerializableColor rgb { get; set; }
        public ShadowParameters shadow = null;
        /*
        linked objects are one of two cases:
        this is a scene light and it is controlled by some light switch sim object, and is linked to that light switch
        this is a light that is a child of some sim object (ie: lamp) and it is controlled by that sim object
        notably, lights that are children of sim objects will have that sim object's name in the light's name (LightParameters.id) as an additional identifier
        */
        public string linkedSimObj { get; set; } //explicit reference to what Sim Object controls if this light is enabled/disabled when using ToggleOnOff
        public bool enabled { get; set; }
        public string parentSimObjId { get; set; } //explicit reference to the objectID of a parent Sim Object this Light is a child of
        public string parentSimObjName { get; set;} //explicit reference to the game object name of the parent Sim Object this Light is a child of
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
        public string room0 { get; set; }
        public string room1 { get; set; }
        public string wall0 { get; set; }
        public string wall1 { get; set; }
        public List<VectorXZ> axesXZ { get; set; }
        public List<Vector3> holePolygon { get; set; }
        public Vector3 assetPosition { get; set; }
        public string type { get; set; }
        public bool openable { get; set; }
        public float openness { get; set; } = 0.0f;
        public string assetId { get; set; }
        public MaterialProperties material { get; set; }
        public Vector3? scale { get; set; } = null;
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

        public MaterialProperties? ceilingMaterial = new MaterialProperties() {
            tilingDivisorX = 1.0f,
            tilingDivisorY = 1.0f
        };

        public float navmeshVoxelSize { get; set; }
        public bool ceilingBackFaces { get; set; }

        public bool unlitCeiling { get; set; }

        public bool squareTiling = false;
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class SerializableCollider {
        public Vector3[] vertices;
        public int[] triangles;
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class PhysicalProperties {
        public float mass = 1;
        public float drag = 0;
        public float angularDrag = 0.05f;
        public bool useGravity = true;
        public bool isKinematic = false;
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class ObjectAnnotations {
        public string objectType = "Undefined";
        public string primaryProperty = "Undefined";
        public string[]? secondaryProperties = null;
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class Ceiling {
        public string id { get; set; }
        public List<Vector3> polygon { get; set; }
        public MaterialProperties material = new MaterialProperties() {
            tilingDivisorX = null,
            tilingDivisorY = null
        };
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class RoomHierarchy {
        public string id { get; set; }
        public string roomType { get; set; }
        public MaterialProperties floorMaterial = new MaterialProperties() {
            tilingDivisorX = 1.0f,
            tilingDivisorY = 1.0f
        };

        public string layer { get; set; }
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
        public string layer { get; set; }
        public MaterialProperties material { get; set; }
        public bool empty { get; set; } = false;
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
        public List<Vector3> holePolygon { get; set; }
        public Vector3 assetPosition { get; set; }
        public string wall0 { get; set; }
        public string wall1 { get; set; }
        public bool openable { get; set; }
        public float openness { get; set; } = 0.0f;
        public List<VectorXZ> axesXZ { get; set; }
        public string type { get; set; }
        public string assetId { get; set; }
        public MaterialProperties material { get; set; }
        public Vector3? scale { get; set; } = null;
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
        public MaterialProperties material;
    }

    [Serializable]
    [MessagePackObject(keyAsPropertyName: true)]
    public class Roof {
        public MaterialProperties material { get; set; }
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
        public string schema { get; set; } = null;
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
    public class MaterialProperties {
        // TODO: move material id, color (as albedo) and tiling divisors 
        public string name { get; set; }
        public SerializableColor color { get; set; }
        public string shader { get; set; } = "Standard";
        public bool unlit { get; set; }
        public float? tilingDivisorX = 1.0f;
        public float? tilingDivisorY = 1.0f;
        public float? metallic;
        public float? smoothness;
    }
    
    public interface WallRectangularHole {
        string id { get; set; }
        string assetId { get; set; }
        string room0 { get; set; }
        string room1 { get; set; }
        string wall0 { get; set; }
        string wall1 { get; set; }
        public List<Vector3> holePolygon { get; set; }
        public Vector3 assetPosition { get; set; }

        float openness { get; set; }

        Vector3? scale { get; set; }
        MaterialProperties material { get; set; }
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
        public MaterialProperties material;
        public string roomId;

        public string layer { get; set; }

        public SerializableColor color { get; set; } = null;

        public bool unlit;
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
