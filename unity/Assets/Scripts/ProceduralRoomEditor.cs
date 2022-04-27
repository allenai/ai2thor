using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using Newtonsoft.Json.Linq;
#if UNITY_EDITOR
using EasyButtons.Editor;
#endif
using EasyButtons;
using System;
using Thor.Procedural;
using Thor.Procedural.Data;
using System.Linq;
using System.IO;

#if UNITY_EDITOR
[ExecuteInEditMode]
public class ProceduralRoomEditor : MonoBehaviour {
    private IEnumerable<NamedSimObj> namedSimObjects;
    public ProceduralHouse loadedHouse;
    protected class NamedSimObj {
        public string assetId;
        public string id;
        public SimObjPhysics simObj;
    }

    [UnityEngine.Header("Loading")]
    public string LoadBasePath = "/Resources/rooms/";
    public string layoutJSONFilename;

    private ProceduralHouse readHouseFromJson(string fileName) {
        var path = BuildLayoutPath(fileName);
        Debug.Log($"Loading: '{path}'");
        var jsonStr = System.IO.File.ReadAllText(path);
        Debug.Log($"json: {jsonStr}");

        JObject obj = JObject.Parse(jsonStr);

        return obj.ToObject<ProceduralHouse>();

    }

    private List<NamedSimObj> assignObjectIds() {
        var root = GameObject.Find(ProceduralTools.DefaultObjectsRootName);
        //var counter = new Dictionary<SimObjType, int>();
        if (root != null) {
            var simobjs = root.transform.GetComponentsInChildren<SimObjPhysics>();

            var namedObjects = simobjs
                .Where(s => s.transform.parent.GetComponentInParent<SimObjPhysics>() == null)
                .GroupBy(s => s.Type)
                .SelectMany(objsOfType => objsOfType.Select((simObj, index) => new NamedSimObj {
                    assetId = !string.IsNullOrEmpty(simObj.assetID) ? simObj.assetID : PrefabNameRevert.GetPrefabAssetName(simObj.gameObject),
                    simObj = simObj,
                    id = $"{Enum.GetName(typeof(SimObjType), simObj.ObjType)}_{index}"
                })).ToList();
            foreach (var namedObj in namedObjects) {
                Debug.Log($" Renaming obj: {namedObj.simObj.gameObject.name} to {namedObj.id}, assetId: {namedObj.assetId}");
                namedObj.simObj.assetID = namedObj.assetId;
                namedObj.simObj.objectID = namedObj.id;
                namedObj.simObj.gameObject.name = namedObj.id;

            }
            return namedObjects;
            // foreach (var namedObj in this.namedSimObjects) {
            //     Debug.Log($" Renamed obj: {namedObj.simObj.gameObject.name} to {namedObj.id}, assetId: {namedObj.assetId}" );
            // }
        } else {
            Debug.LogError($"No root object '{ProceduralTools.DefaultObjectsRootName}'");
            return null;
        }
    }

    [Button(Expanded = true)]
    public void LoadLayout() {
        this.loadedHouse = readHouseFromJson(this.layoutJSONFilename);

        var houseObj = ProceduralTools.CreateHouse(
            this.loadedHouse,
            ProceduralTools.GetMaterials()
        );

    }


    private List<Vector3> getPolygonFromWallPoints(Vector3 p0, Vector3 p1, float height) {
        return new List<Vector3>() {
            p0,
            p1,
            p1 + Vector3.up * height,
            p0 + Vector3.up * height
        };

    }


    // private List<Vector3> getPolygonFromWallObject(GameObject wall, bool reverse = false, bool debug = false) {
    private List<Vector3> getPolygonFromWallObject(GameObject wall, bool reverse = false, bool debug = false) {
        var box = wall.GetComponent<BoxCollider>();
        var offset = box.size / 2.0f;
        offset.z = 0.0f;


        if (debug) {

            Debug.Log(" wall " + $"name: '{wall.gameObject.name}' " + wall.GetComponentInParent<SimObjPhysics>().gameObject.name + " box " + box.center + " offset " + offset + " size: " + box.size + " p0 " + wall.transform.TransformPoint(box.center - offset).ToString("F8") + " p1 " + wall.transform.TransformPoint(box.center + new Vector3(offset.x, -offset.y, 0.0f)).ToString("F8") + $"local p0: {(box.center + new Vector3(offset.x, -offset.y, 0.0f)).ToString("F8")} p1: {(box.center + new Vector3(offset.x, -offset.y, 0.0f)).ToString("F8")}");
        }
        var r = new List<Vector3>() {
            wall.transform.TransformPoint(box.center - offset) ,
            wall.transform.TransformPoint(box.center + new Vector3(offset.x, -offset.y, 0.0f)),
            wall.transform.TransformPoint(box.center + new Vector3(offset.x, offset.y, 0.0f)),
            wall.transform.TransformPoint(box.center + new Vector3(-offset.x, offset.y, 0.0f)),
        };

        if (!reverse) {
            return r;
        } else {
            return new List<Vector3>() {
                r[1],
                r[0],
                r[3],
                r[2]
            };
        }
    }

    public class ConnectionAndWalls {
        public WallRectangularHole connection;
        public List<(PolygonWall wall, string afterWallId)> walls;

        public List<string> wallIdsToDelete;
    }

    private IEnumerable<ConnectionAndWalls> serializeConnections(IEnumerable<SimObjPhysics> connections, SimObjType filterType, string prefix, Dictionary<string, PolygonWall> wallMap, Dictionary<string, WallRectangularHole> connectionMap = null) {
        var flippedForward = filterType == SimObjType.Window;
        var connectionsWithWalls = connections.Where(s => s.Type == filterType).Select((d, i) => {
            var id = d.gameObject.name;

            // Debug.Log($"----- {prefix} " + d.gameObject.name);
            var box = d.BoundingBox.GetComponent<BoxCollider>();
            box.enabled = true;
            var boxOffset = box.size / 2.0f;
            var poly = getPolygonFromWallObject(d.BoundingBox, flippedForward, true);

            Debug.Log($"----- i: {i},  {prefix} {d.gameObject.name} p0 {poly[0]} p1 {poly[1]}");
            var polyRev = getPolygonFromWallObject(d.BoundingBox, !flippedForward);
            ConnectionProperties connectionProps = d.GetComponentInChildren<ConnectionProperties>();
            var materialId = "";


            // Doesnt work for some reason
            //         var colliders = Physics.OverlapBox(d.transform.TransformPoint(box.center), boxOffset * 4, 
            //   Quaternion.identity, 
            //     LayerMask.GetMask("SimObjVisible", "Procedural1", "Procedural2", "Procedural3", "Procedural0"), 
            //  QueryTriggerInteraction.UseGlobal);
            var colliders = GameObject.Find(
                $"/{ProceduralTools.DefaultRootStructureObjectName}/{ProceduralTools.DefaultRootWallsObjectName}"
                ).transform.GetComponentsInChildren<BoxCollider>();

            var wallColliders = colliders; //.Where(s => s.GetComponent<SimObjPhysics>()?.Type == SimObjType.Wall); //&& s.GetType().IsAssignableFrom(typeof(BoxCollider))).Select(c => c as BoxCollider);

            var p0 = poly[0];
            var p1 = poly[1];

            wallColliders = wallColliders.Where(s => s.GetComponent<SimObjPhysics>()?.Type == SimObjType.Wall).ToArray();
            Debug.Log("After filter " + wallColliders.Length);

            var p0UpWorld = d.transform.TransformPoint(poly[2] - poly[1]).normalized;

            // var p0World =  d.transform.worldToLocalMatrix.MultiplyPoint( p0);
            // var p1World = d.transform.worldToLocalMatrix.MultiplyPoint(p1);

            var p0World = p0;
            var p1World = p1;

            spheres.Add((p0World, Color.cyan));
            spheres.Add((p1World, Color.green));
            Debug.Log("diff " + (p1 - p0) + " p0: " + p0 + " p1: " + p1 + " p0World: " + p0World + " p1World: " + p1World);
            //     Gizmos.color = Color.yellow;
            //     Debug.Dr
            //  Gizmos.DrawSphere(p0World, 0.2f);
            //  Gizmos.DrawSphere(p1World, 0.2f);

            var normal = Vector3.Cross((p1World - p0World).normalized, p0UpWorld);

            var colliderDistances = wallColliders.Select(collider => {
                var offset = collider.size / 2.0f;

                // Debug.Log("Getting collider: " + collider.gameObject.name + " in dic " + wallMap.ContainsKey(collider.gameObject.name));
                var wallM = wallMap[collider.gameObject.name];



                var localP0 = collider.center - offset;
                // var localP0OnConnection =  

                var localP1 = collider.center + new Vector3(offset.x, 0, 0) - new Vector3(0, offset.y, 0);
                // var topP0 = collider.transform.TransformPoint(collider.center + new Vector3(0, offset.y, 0));
                // var cP0 = collider.transform.TransformPoint(localP0);
                // var cP1 = collider.transform.TransformPoint(localP1);

                var topP0 = wallM.polygon[3];
                var cP0 = wallM.polygon[0];
                var cP1 = wallM.polygon[1];

                var upVec = (topP0 - cP0).normalized;


                return new {
                    collider = collider,
                    p0SqrDistance = (p1World - cP0).sqrMagnitude,
                    p1SqrDistance = (p0World - cP1).sqrMagnitude,
                    p0 = cP0,
                    p1 = cP1,
                    height = collider.size.y,
                    normal = Vector3.Cross((cP1 - cP0).normalized, upVec)
                };
            });

            // && Vector3.Dot(next.normal, normal) > 0

            Debug.Log("Colliders returned " + colliders.Count() + " collider distances " + colliderDistances.Count());
            var dir = (p1World - p0World);
            var dirNormalized = dir.normalized;
            var eps = 0.1f;
            var tEps = 1e-12;


            var tEpsStrict = 1e-5;
            var dirEps = 1e-5;
            var normalEps = -1e-4;

            bool wallPointOnConnectionLine(Vector3 wallP0, Vector3 wallP1, Vector3 direction, Vector3 wallNormal, string name) {
                var p0Ref = new Vector3(p0World.x, wallP0.y, p0World.z);
                var p1Ref = new Vector3(p1World.x, wallP1.y, p1World.z);

                var len = (p1Ref - p0Ref).magnitude;
                var connP0ToWallP1 = wallP1 - p0Ref;
                // var p0toP1 = direction;
                var connP1ToWallP0 = wallP0 - p1Ref;
                // var p1toP0 = -p0toP1;

                // var sign = Vector3.Dot(wallNormal, d.transform.forward) >= normalEps ? 1.0f : -1.0f;
                // direction *= sign;

                // var proy0 = Vector3.Dot(connP0ToWallP1, direction) * direction;
                var t0 = new Vector3(connP0ToWallP1.x / direction.x, connP0ToWallP1.y / direction.y, connP0ToWallP1.z / direction.z);
                // var proy1 = Vector3.Dot(connP1ToWallP0, -direction) * -direction;
                var t1 = new Vector3(connP1ToWallP0.x / -direction.x, connP1ToWallP0.y / -direction.y, connP1ToWallP0.z / -direction.z);
                // var onLine0 = Math.Abs(t0.x - t0.z) < eps || Math.Abs(t0.z) < eps || Math.Abs(t0.x) < eps;
                // var onLine1 = Math.Abs(t1.x - t1.z) < eps || Math.Abs(t1.z) < eps || Math.Abs(t1.x) < eps;

                var connP0toWallP0 = (wallP0 - p0Ref).normalized;
                var connP1toWallP1 = (wallP1 - p1Ref).normalized;

                var dot0 = Vector3.Dot(connP0toWallP0, direction.normalized);
                var dot1 = Vector3.Dot(connP1toWallP1, -direction.normalized);

                var onLine0 = Math.Abs(dot0) >= 1.0f - eps;
                var onLine1 = Math.Abs(dot1) >= 1.0f - eps;

                if (name == "wall_3_1" || name == "wall_3_2" && d.gameObject.name == "Window_3") {
                    Debug.Log($" ^^^^^^^^^^ DIRECTION p0 {p0World.x},{p0World.y},{p0World.z} p1 {p1World.x},{p1World.y},{p1World.z} dir {dir.x},{dir.y},{dir.z}");
                    Debug.Log($"************* wall {name}, wallp0 ({wallP0.x}, {wallP0.y}, {wallP0.z}),  wallp1 ({wallP1.x}, {wallP1.y}, {wallP1.z}), connP0 {p0Ref},  connP1 {p1Ref}, connP0ToWallP1 {connP0ToWallP1}, connP1ToWallP0 {connP1ToWallP0} dir {direction} connP1ToWallP0 {connP1ToWallP0} connP0ToWallP1 {connP0ToWallP1} t0.x {t0.x}, t0.y {t0.y}, t0.z {t0.z} t1.x {t1.x}, t1.y {t1.y}, t1.z {t1.z} onLine0 {onLine0}, onLine1 {onLine1} dot0 {dot0} dot1 {dot1}  num0.z {connP0ToWallP1.z} num1.z {connP1ToWallP0.z} dir.z {direction.z}");
                }

                return
                 (onLine0 && ((Math.Abs(direction.x) > dirEps && t0.x <= (1.0f + tEpsStrict) && t0.x >= (0.0f - tEpsStrict)) || ((Math.Abs(direction.z) > dirEps && t0.z <= (1.0f + tEpsStrict) && t0.z >= (0.0f - tEpsStrict)))))
                  ||
                 (onLine1 && ((Math.Abs(direction.x) > dirEps && t1.x <= (1.0f + tEpsStrict) && t1.x >= (0.0f - tEpsStrict)) || ((Math.Abs(direction.z) > dirEps && t1.z <= (1.0f + tEpsStrict) && t1.z >= (0.0f - tEpsStrict)))));
            }

            // bool pointOnWallLine(Vector3 p, Vector3 direction, Vector3 origin, string name) {
            //     var num = (p - origin);
            //     var proyLength = Vector3.Dot(num, direction);
            //     num = direction * proyLength;


            //     var t = new Vector3(num.x / direction.x, num.y / direction.y, num.z / direction.z);
            //     var onLine = Math.Abs(t.x - t.z) < eps;

            //      if (name == "wall1_7" || name == "wall1_6" || name == "wall1_5" && d.gameObject.name == "Window_1") {
            //         Debug.Log($"************* wall {name}, p {p}, orig {origin}, diff {(p - origin)} dir {direction} PROJ {proyLength} num {num} t.x {t.x}, t.y {t.y}, t.z {t.z} onLine {onLine}" );
            //     }
            //     return onLine && t.x <= (1.0f+tEps) && t.x >= (0.0f-tEps); 
            // }

            bool pointOnWallLine(Vector3 p, Vector3 direction, Vector3 origin, string name, string side = "") {

                var originRef = new Vector3(origin.x, p.y, origin.z);
                var originToP = (p - originRef);

                var dot0 = Vector3.Dot(originToP.normalized, direction.normalized);

                var t = new Vector3(originToP.x / direction.x, originToP.y / direction.y, originToP.z / direction.z);
                // var proyLength = Vector3.Dot(num, direction);

                var onLine = Math.Abs(dot0) >= 1.0f - eps;

                //  if (name == "wall1_7" || name == "wall1_6" || name == "wall1_5" && d.gameObject.name == "Window_1") {
                // if (name == "wall_2_8" && d.gameObject.name == "Window_Hung_48x44") {
                if (name == "wall_2_6" || name == "wall_2_7" || name == "wall_2_8" && d.gameObject.name == "Window_5") {

                    Debug.Log($"!!!!!!!!!!!!!!! Window_Hung_48x44 wall {name} - {side} walls, p {p}, orig {originRef}, dir {direction} dot0 {dot0} originToP {originToP} t.x {t.x}, t.y {t.y}, t.z {t.z} onLine {onLine} t.x <= (1.0f+tEps) {t.x <= (1.0f + tEps)} t.x >= (0.0f-tEps) {t.x >= (0.0f - tEps)} (Math.Abs(direction.x) > dirEps && t.x <= (1.0f+tEps) && t.x >= (0.0f-tEps))  {(Math.Abs(direction.x) > dirEps && t.x <= (1.0f + tEps) && t.x >= (0.0f - tEps))} ((Math.Abs(direction.z) > dirEps && t.z <= (1.0f+tEps) && t.z >= (0.0f-tEps))) {((Math.Abs(direction.z) > dirEps && t.z <= (1.0f + tEps) && t.z >= (0.0f - tEps)))} ");
                }

                return onLine && ((Math.Abs(direction.x) > dirEps && t.x <= (1.0f + tEps) && t.x >= (0.0f - tEps)) || ((Math.Abs(direction.z) > dirEps && t.z <= (1.0f + tEps) && t.z >= (0.0f - tEps))));

            }

            var connectionNormal = flippedForward ? -d.transform.forward : d.transform.forward;
            // if (filterType == SimObjType.Window) {
            //     connectionNormal = -connectionNormal;
            // }

            // Func<Vector3, Vector3, Vector3, string, bool, bool> pointOnWallLine = (Vector3 p, Vector3 direction, Vector3 origin, string name, bool ignoreSign) => {

            // };

            var wallRight = colliderDistances.Aggregate(new {
                collider = box,
                p0SqrDistance = float.MaxValue,
                p1SqrDistance = float.MaxValue,
                p0 = new Vector3(),
                p1 = new Vector3(),
                height = 0.0f,
                normal = new Vector3()
            },
                (min, next) =>
                    min.p0SqrDistance > next.p0SqrDistance
                    && Vector3.Dot(next.collider.transform.forward, connectionNormal) >= normalEps
                    && !pointOnWallLine(next.p0, dir, p0World, next.collider.gameObject.name, "right") ? next : min
            );

            // && Vector3.Dot(next.normal, normal) > 0
            var wallLeft = colliderDistances.Aggregate(new {
                collider = box,
                p0SqrDistance = float.MaxValue,
                p1SqrDistance = float.MaxValue,
                p0 = new Vector3(),
                p1 = new Vector3(),
                height = 0.0f,
                normal = new Vector3()
            },
                (min, next) => {
                    var name = next.collider.gameObject.name;
                // if (name == "wall1_7" || name == "wall1_6" || name == "wall1_5" && d.gameObject.name == "Window_1") {
                if (name == "wall_2_8" && d.gameObject.name == "Window_Hung_48x44") {
                        Debug.Log($"########## -- connection {d.gameObject.name} wall Left {name} p1SqrDistance {next.p1SqrDistance}, normal {Vector3.Dot(next.collider.transform.forward, connectionNormal)} !onLine {!pointOnWallLine(next.p1, -dirNormalized, p1World, next.collider.gameObject.name)}");
                    }

                    return
                    min.p1SqrDistance > next.p1SqrDistance
                    && Vector3.Dot(next.collider.transform.forward, connectionNormal) >= normalEps
                    // && wallPointOnConnectionLine(next.p0, next.p1, -dirNormalized)
                    && !pointOnWallLine(next.p1, -dir, p1World, next.collider.gameObject.name, "left")
                    ? next : min;
                }
            );

            Debug.Log($"^^^^^^^^^^^^ Wall left p0: {wallLeft.p0} p1: {wallLeft.p1}");


            var backWallClosestLeft = colliderDistances.Aggregate(new {
                collider = box,
                p0SqrDistance = float.MaxValue,
                p1SqrDistance = float.MaxValue,
                p0 = new Vector3(),
                p1 = new Vector3(),
                height = 0.0f,
                normal = new Vector3()
            },
                (min, next) =>
                    min.p0SqrDistance > next.p0SqrDistance
                    && Vector3.Dot(next.collider.transform.forward, -connectionNormal) >= normalEps
                    && !pointOnWallLine(next.p0, -dirNormalized, p1World, next.collider.gameObject.name, "backLeft")
                     ? next : min
            );


            var backWallClosestRight = colliderDistances.Aggregate(new {
                collider = box,
                p0SqrDistance = float.MaxValue,
                p1SqrDistance = float.MaxValue,
                p0 = new Vector3(),
                p1 = new Vector3(),
                height = 0.0f,
                normal = new Vector3()
            },
                (min, next) =>
                    min.p1SqrDistance > next.p1SqrDistance &&
                    Vector3.Dot(next.collider.transform.forward, -connectionNormal) >= normalEps
                    && !pointOnWallLine(next.p1, dirNormalized, p0World, next.collider.gameObject.name, "backRight")
                    ? next : min
            );


            var toDelete = colliderDistances.Where(
                next =>
                    (
                        Vector3.Dot(next.collider.transform.forward, connectionNormal) >= normalEps
                        && pointOnWallLine(next.p0, dir, p0World, next.collider.gameObject.name, "right")
                    )
                    ||
                    (
                        Vector3.Dot(next.collider.transform.forward, connectionNormal) >= normalEps
                        && pointOnWallLine(next.p1, -dir, p1World, next.collider.gameObject.name, "left")
                    )
                    ||
                    (
                        Vector3.Dot(next.collider.transform.forward, -connectionNormal) >= normalEps
                        && pointOnWallLine(next.p0, -dirNormalized, p1World, next.collider.gameObject.name, "backLeft")
                    )
                    ||
                    (
                        Vector3.Dot(next.collider.transform.forward, -connectionNormal) >= normalEps
                        && pointOnWallLine(next.p1, dirNormalized, p0World, next.collider.gameObject.name, "backRight")
                    )
            // next => wallPointOnConnectionLine(next.p0, next.p1, dir, next.collider.transform.forward, next.collider.gameObject.name)
            );


            Debug.Log($"&&&&&&&&&& TODELETE {d.gameObject.name} " + string.Join(", ", toDelete.Select(w => w.collider.gameObject.name)));

            //Debug.Log("Walls0 " + wall0.collider.name + " wall 1 " +wall1.collider.gameObject.name);
            // var debug = colliderDistances.ToList()[0];
            // // var debug = colliderDistances.ElementAt(0);
            // Debug.Log(" p0 CDist " + debug.p0SqrDistance + " p1 CDist " + debug.p1SqrDistance + " name " + debug.collider.GetComponentInParent<SimObjPhysics>().ObjectID);
            Debug.Log("walls_right " + wallRight.collider.gameObject.name + " dist " + wallRight.p0SqrDistance + " wall_left " + wallLeft.collider.gameObject.name + " dist " + wallLeft.p1SqrDistance +
            " backwallLeft " + backWallClosestLeft.collider.gameObject.name + " backwallRight " + backWallClosestRight.collider.name);


            spheres.Add((wallLeft.p1, Color.red));
            spheres.Add((wallRight.p0, Color.blue));
            //     var m_HitDetect = Physics.BoxCast(box.bounds.center, boxOffset * 4, transform.forward, out m_Hit, transform.rotation, m_MaxDistance);
            // if (m_HitDetect)
            // {
            //     //Output the name of the Collider your Box hit
            //     Debug.Log("Hit : " + m_Hit.collider.name);
            // }
            //  var wall = new PolygonWall {
            //     id = $"wall_{id}_front",
            //     roomId = connectionProps?.OpenFromRoomId,
            //     polygon = poly,
            //     // TODO get material somehow
            //     // material = connectionProps?.openFromWallMaterial?.name
            //     material = wallRight.collider.GetComponent<MeshRenderer>().sharedMaterial.name

            //     // materialTilingXDivisor = box.size.x / material.mainTextureScale.x,
            //     // materialTilingYDivisor = box.size.y / material.mainTextureScale.y};
            // };
            //  var wallRev = new PolygonWall {
            //     id = $"wall_{id}_back",
            //     roomId = connectionProps?.OpenToRoomId,
            //     polygon = polyRev,
            //     // TODO get material somehow
            //     // material = connectionProps?.openToWallMaterial?.name

            //     material = backWallClosestLeft.collider.GetComponent<MeshRenderer>().sharedMaterial.name

            //     // materialTilingXDivisor = box.size.x / material.mainTextureScale.x,
            //     // materialTilingYDivisor = box.size.y / material.mainTextureScale.y};
            // };

            var wall = createNewWall(
                    $"wall_{id}_front",
                    connectionProps,
                    wallLeft.p1,
                    wallRight.p0,
                    wallLeft.height,
                    wallLeft.collider.GetComponent<MeshRenderer>().sharedMaterial
            );

            var material = backWallClosestRight.collider.GetComponent<MeshRenderer>().sharedMaterial;
            var lenn = (wall.polygon[1] - wall.polygon[0]).magnitude;
            //   var height = 
            var wallRev = new PolygonWall {
                id = $"wall_{id}_back",
                roomId = connectionProps?.OpenToRoomId,
                polygon = new List<Vector3>() { wall.polygon[1], wall.polygon[0], wall.polygon[3], wall.polygon[2] },
                // polygon = getPolygonFromWallPoints(p0, p1, backWallClosestRight.height),
                // TODO get material somehow
                // material = connectionProps?.openFromWallMaterial?.name
                material = material.name,

                materialTilingXDivisor = lenn / material.mainTextureScale.x,
                materialTilingYDivisor = backWallClosestRight.height / material.mainTextureScale.y
            };

            // var wallRev = createNewWall(
            //         $"wall_{id}_back", 
            //         connectionProps, 
            //         backWallClosestLeft.p0, 
            //         backWallClosestRight.p1, 

            //         backWallClosestRight.height, 
            //         backWallClosestRight.collider.GetComponent<MeshRenderer>().sharedMaterial
            // );

            Debug.Log($"^^^^^^^^^^^^ Created wall p0: {wall.polygon[0].ToString("F8")} p1: {wall.polygon[1].ToString("F8")}");

            WallRectangularHole connection = null;

            var p0WallLevel = new Vector3(p0World.x, wallLeft.p1.y, p0World.z);
            var p0ToConnection = p0WallLevel - wallLeft.p1;
            var xLen = Vector3.Dot(p0ToConnection, p0ToConnection.normalized);

            Debug.Log($"............. Door {xLen} p0To {p0ToConnection} ");


            var assetId = !string.IsNullOrEmpty(d.assetID) ? d.assetID : PrefabNameRevert.GetPrefabAssetName(d.gameObject);

            // assetId = assetId == null ? d.assetID : assetId;

            if (filterType == SimObjType.Doorway) {



                connection = new Thor.Procedural.Data.Door {

                    id = id,

                    room0 = connectionProps?.OpenFromRoomId,
                    room1 = connectionProps?.OpenToRoomId,
                    wall0 = wall.id,
                    wall1 = wallRev.id,
                    boundingBox = new Thor.Procedural.Data.BoundingBox { min = new Vector3(xLen, 0.0f, box.size.z / 2.0f), max = new Vector3(xLen + box.size.x, box.size.y, box.size.z / 2.0f) },

                    //boundingBox = new Thor.Procedural.Data.BoundingBox { min = box.center - boxOffset, max = box.center + boxOffset },
                    // boundingBox = new Thor.Procedural.Data.BoundingBox { min = new Vector3(1f, 0.0f, 0.0f), max = new Vector3(3f, 2.0f, 2.0f) },
                    type = Enum.GetName(typeof(ConnectionType), (connectionProps?.Type).GetValueOrDefault()),

                    openable = d.SecondaryProperties.Contains(SimObjSecondaryProperty.CanOpen),
                    openness = (connectionProps?.IsOpen).GetValueOrDefault() ? 1.0f : 0.0f,
                    assetId = assetId

                };
            } else if (filterType == SimObjType.Window) {
                var yMin = p0World.y - wallLeft.p1.y;
                connection = new Thor.Procedural.Data.Window {

                    id = id,

                    room0 = connectionProps?.OpenFromRoomId,
                    room1 = connectionProps?.OpenToRoomId,
                    wall0 = wall.id,
                    wall1 = wallRev.id,
                    boundingBox = new Thor.Procedural.Data.BoundingBox {
                        min = new Vector3(xLen, yMin, box.size.z / 2.0f),
                        max = new Vector3(xLen + box.size.x, yMin + box.size.y, box.size.z / 2.0f)
                    },

                    // boundingBox = new Thor.Procedural.Data.BoundingBox { min = box.center - boxOffset, max = box.center + boxOffset },
                    type = Enum.GetName(typeof(ConnectionType), (connectionProps?.Type).GetValueOrDefault()),
                    openable = d.SecondaryProperties.Contains(SimObjSecondaryProperty.CanOpen),

                    openness = (connectionProps?.IsOpen).GetValueOrDefault() ? 1.0f : 0.0f,
                    assetId = assetId

                };
            }

            // var connection = new Thor.Procedural.Data.Door {

            //      id = id,

            //     room0 = connectionProps?.OpenFromRoomId,
            //     room1 = connectionProps?.OpenToRoomId,
            //     wall0 = wall.id,
            //     wall1 = wallRev.id,
            //     boundingBox = new Thor.Procedural.Data.BoundingBox { min = box.center - boxOffset, max = box.center + boxOffset },
            //     type = "???",
            //     openable = d.SecondaryProperties.Contains(SimObjSecondaryProperty.CanOpen),
            //     // TODO
            //     open = false,
            //     assetId = PrefabNameRevert.GetPrefabAssetName(d.gameObject)

            // };
            box.enabled = false;


            var wallsToCreate = new List<(PolygonWall wall, string afterWallId)>() { (wall, wallLeft.collider.name), (wallRev, backWallClosestLeft.collider.name) };

            // if ( wallLeft.p1SqrDistance > minimumWallSqrDistanceCreateEpsilon ) {
            //     wallsToCreate.Add(
            //         createNewWall(
            //             $"wall_{id}_left_front", connectionProps, wallLeft.p1, p0World, wallLeft.height, wallLeft.collider.GetComponent<MeshRenderer>().sharedMaterial)
            //     );
            // }

            // if ( (backWallClosestLeft.p0 - p0World).magnitude > minimumWallSqrDistanceCreateEpsilon ) {

            //     wallsToCreate.Add(
            //         createNewWall($"wall_{id}_right_back", connectionProps, backWallClosestLeft.p0, p0World, backWallClosestLeft.height, backWallClosestLeft.collider.GetComponent<MeshRenderer>().sharedMaterial)
            //     );
            // }

            // if ( wallRight.p0SqrDistance > minimumWallSqrDistanceCreateEpsilon ) {
            //     wallsToCreate.Add(
            //         createNewWall($"wall_{id}_right_front", connectionProps, wallRight.p0, p1World, wallRight.height, wallRight.collider.GetComponent<MeshRenderer>().sharedMaterial)
            //     );
            // }

            //  if ( (backWallClosestRight.p1 - p1World).magnitude > minimumWallSqrDistanceCreateEpsilon ) {

            //     //  Debug.Log(" Wall closest right dist " + )
            //      wallsToCreate.Add(
            //         createNewWall($"wall_{id}_right_back", connectionProps, backWallClosestRight.p1, p1World, backWallClosestLeft.height, backWallClosestLeft.collider.GetComponent<MeshRenderer>().sharedMaterial)
            //     );
            //  }

            // wallsToCreate.Add((wall, wallLeft.collider.name));

            // if (filterType != SimObjType.Window) {

            // wallsToCreate.Add((wallRev, backWallClosestLeft.collider.name));

            // wallsToCreate = wallsToCreate.(new List<(PolygonWall wall, string afterWallId)>() {(wallRev, backWallClosestLeft.collider.name)});
            // }

            Debug.Log("^^^^^^^^^^^^^^^^ SUPPOSED TO ADD PIECE OF SHIT " + string.Join(", ", wallsToCreate.Select(x => x.afterWallId)));



            var anchoredSet = new HashSet<string>() {
                 wallLeft.collider.name,
                 wallRight.collider.name,
                 backWallClosestLeft.collider.name,
                 backWallClosestRight.collider.name
             };
            toDelete = toDelete.Where(o => !anchoredSet.Contains(o.collider.name));
            Debug.Log($"~~~~~~~~~~ Walls to delete {string.Join(", ", toDelete.Select(o => o.collider.name))}");

            return new ConnectionAndWalls() {
                connection = connection,
                walls = wallsToCreate,
                wallIdsToDelete = toDelete.Select(o => o.collider.name).ToList()
            };
        });
        return connectionsWithWalls;
    }

    // No anonymous class with interface implementation C#?? lagging behind Java
    // private ConnectionGameObjects<WallRectangularHole> serializeConnections(IEnumerable<SimObjPhysics> connections, SimObjType filterType) {
    //     var doorWalls = connections.Where(
    //         s => s.Type == filterType
    //     ).Select( (d, i) => {
    //         var box = d.transform.Find("BoundingBox").GetComponentInChildren<BoxCollider>();
    //         var boxOffset = box.size / 2.0f;
    //         var poly = getPolygonFromWallObject(d.gameObject);
    //         var polyRev = getPolygonFromWallObject(d.gameObject, true);
    //         var id = $"door_{i}";
    //          var wall = new PolygonWall {
    //             id = $"wall_{id}_front",
    //             roomId = d.GetComponentInChildren<ConnectionProperties>().OpenFromRoomId,
    //             polygon = poly,
    //             // TODO get material somehow
    //             material = ""

    //             // materialTilingXDivisor = box.size.x / material.mainTextureScale.x,
    //             // materialTilingYDivisor = box.size.y / material.mainTextureScale.y};
    //         };
    //          var wallRev = new PolygonWall {
    //             id = id,
    //             roomId = d.GetComponentInChildren<ConnectionProperties>().OpenFromRoomId,
    //             polygon = polyRev,
    //             // TODO get material somehow
    //             material = ""

    //             // materialTilingXDivisor = box.size.x / material.mainTextureScale.x,
    //             // materialTilingYDivisor = box.size.y / material.mainTextureScale.y};
    //         };
    //         var door = new ?? {

    //              id = $"wall_{id}_back",

    //             room0 = d.GetComponentInChildren<ConnectionProperties>().OpenFromRoomId,
    //             room1 = d.GetComponentInChildren<ConnectionProperties>().OpenToRoomId,
    //             wall0 = wall.id,
    //             wall1 = wallRev.id,
    //             boundingBox = new Thor.Procedural.Data.BoundingBox { min = box.center - boxOffset, max = box.center + boxOffset },
    //             type = "???",
    //             openable = d.SecondaryProperties.Contains(SimObjSecondaryProperty.CanOpen),
    //             // TODO
    //             open = false,
    //             assetId = PrefabNameRevert.GetPrefabAssetName(d.gameObject)

    //         } as WallRectangularHole;
    //         return new ConnectionGameObjects<T>{
    //             door = door,
    //             walls = new List<PolygonWall>() { wall, wallRev}
    //         };
    //     });
    //     return doorWalls;
    // }

    private List<(Vector3, Color)> spheres = new List<(Vector3, Color)>();

    void OnDrawGizmosSelected() {

        foreach (var (c, color) in spheres) {
            Gizmos.color = color;
            Gizmos.DrawSphere(c, 0.2f);
        }
    }

    private PolygonWall createNewWall(string id, ConnectionProperties connectionProps, Vector3 p0, Vector3 p1, float height, Material material) {
        var len = (p1 - p0).magnitude;
        return new PolygonWall {
            id = id,
            roomId = connectionProps?.OpenFromRoomId,
            polygon = getPolygonFromWallPoints(p0, p1, height),
            // TODO get material somehow
            // material = connectionProps?.openFromWallMaterial?.name
            material = material.name,

            materialTilingXDivisor = len / material.mainTextureScale.x,
            materialTilingYDivisor = height / material.mainTextureScale.y
        };

    }

    private ProceduralHouse regenerateWallsData(ProceduralHouse house) {

        // if (this.loadedHouse == null) {
        //     this.loadedHouse = readHouseFromJson(this.layoutJSONFilename);
        // }
        // var house = readHouseFromJson(this.layoutJSONFilename);
        var root = GameObject.Find(ProceduralTools.DefaultRootWallsObjectName);
        var wallsGOs = root.GetComponentsInChildren<SimObjPhysics>().Where(s => s.Type == SimObjType.Wall);
        // var wallsDict = this.loadedHouse.walls.ToDictionary(w => w.id, w => w);
        var minimumWallSqrDistanceCreateEpsilon = 0.03f;

        var wallsJson = wallsGOs.Select((w, i) => {
            var material = w.GetComponent<MeshRenderer>().sharedMaterial;
            var poly = getPolygonFromWallObject(w.gameObject);
            var box = w.GetComponent<BoxCollider>();

            return new PolygonWall {
                id = w.gameObject.name,
                roomId = w.GetComponentInChildren<WallProperties>().RoomId,
                polygon = poly,
                material = material.name,
                materialTilingXDivisor = box.size.x / material.mainTextureScale.x,
                materialTilingYDivisor = box.size.y / material.mainTextureScale.y
            };
        }
        ).ToList();
        var simObjs = GameObject.Find(ProceduralTools.DefaultObjectsRootName).GetComponentsInChildren<SimObjPhysics>();

        var wallsDict = new Dictionary<string, PolygonWall>(house.walls.ToDictionary(w => w.id, w => w));

        var connectionsDict = new Dictionary<string, WallRectangularHole>(house.doors.Select(d => d as WallRectangularHole).Concat(house.windows).ToDictionary(w => w.id, w => w));
        // var wallsDict = new List<PolygonWall>(this.loadedHouse.walls).ToDictionary(w => w.id, w => w);
        // foreach (var m in wallsDict) {
        //     Debug.Log("Dict: " + m.Key);
        // }
        var doorWalls = serializeConnections(simObjs, SimObjType.Doorway, "door", wallsDict).ToList();
        //doorWalls = doorWalls.Take(1).ToList();
        // var doorWalls = new List<ConnectionAndWalls>();

        var windowWalls = serializeConnections(simObjs, SimObjType.Window, "window", wallsDict).ToList();

        Debug.Log("+++++++++++++++ Windows " + string.Join(", ", windowWalls.SelectMany(x => x.walls).Select(w => w.wall.id)));

        var allNewConnections = doorWalls.Concat(windowWalls);

        Debug.Log($"Walls: {string.Join(", ", wallsJson.Select(w => w.id))}");

        var wallWithInsertLocation = allNewConnections.SelectMany(s => s.walls).Select(wallIdPair => {

            //    Debug.Log($"All Walls: {string.Join(", ", wallsJson.Select(w => w.id))}");
            // Debug.Log("Wall " +  wallIdPair.wall.id + " search after: '" + wallIdPair.afterWallId + "' find: " + wallsJson.FirstOrDefault( w => string.Equals(w.id, wallIdPair.afterWallId,StringComparison.InvariantCultureIgnoreCase ) )+ " ." + $" find '{string.Join(", ", wallsJson.Where(w => string.Equals(w.id, "wall0_17")))}'");
            return (
                wall: wallIdPair.wall,
                index:
                    wallsJson.Select(
                        (w, i) => (wall: w, index: i)
                    ).First(w => string.Equals(w.wall.id, wallIdPair.afterWallId)).index
            );
        }

        );
        var toDeleteSet = new HashSet<string>(allNewConnections.SelectMany(w => w.wallIdsToDelete));
        //wallsJson = wallsJson.Where(w => !toDeleteSet.Contains(w.id)).ToList();
        foreach (var pair in wallWithInsertLocation) {
            wallsJson.Insert(pair.index + 1, pair.wall);
        }

        // wallsJson = wallsJson.Where(w => !toDeleteSet.Contains(w.id)).ToList();

        // var allWalls = wallsJson.Concat(doorWalls.SelectMany(d => d.walls)).Concat(windowWalls.SelectMany(d => d.walls));
        var doors = doorWalls.Select(d => d.connection as Thor.Procedural.Data.Door);
        var windows = windowWalls.Select(d => d.connection as Thor.Procedural.Data.Window);
        Debug.Log($"TO DELETE: {string.Join(", ", toDeleteSet)}");
        //  house.walls = wallsJson.Where(w => !toDeleteSet.Contains(w.id)).ToList();
        Debug.Log($"################# WALLS Before DELETE {string.Join(",", house.walls.Select(c => c.id))}");
        // house.doors = doors.ToList();
        // house.windows = windows.ToList();
        return new ProceduralHouse {
            proceduralParameters = house.proceduralParameters,
            id = house.id,
            rooms = house.rooms,
            walls = wallsJson.Where(w => !toDeleteSet.Contains(w.id)).ToList(),
            doors = doors.ToList(),
            windows = windows.ToList(),
            objects = house.objects,
            roof = house.roof
        };
    }

    // Debug
    // [Button]
    //  public void RegenerateWallsData() {

    //     this.regenerateWallsData(readHouseFromJson(this.layoutJSONFilename));


    // }
    [Button]
    public void AssigObjectIds() {
        this.assignObjectIds();
    }

    [Button]
    public void ReloadScene() {
#if UNITY_EDITOR        
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();

        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scene.path);
#endif        
    }

    [Button(Expanded = true)]
    public void SerializeSceneFromGameObjects(string outFilename) {
        // var path = BuildLayoutPath(layoutJSONFilename);
        // var jsonStr = System.IO.File.ReadAllText(path);
        // JObject jsonObj = JObject.Parse(jsonStr);
        // this.loadedHouse = jsonObj.ToObject<ProceduralHouse>();

        // if (this.loadedHouse != null) {
        if (String.IsNullOrWhiteSpace(this.layoutJSONFilename)) {
            Debug.LogError("No base layout filename provided, need this to get procedural params. Add a 'layoutJSONFilename'");
            return;
        }

        //var house = this.readHouseFromJson(this.layoutJSONFilename);

        // }

        var house = regenerateWallsData(this.readHouseFromJson(this.layoutJSONFilename));
        var outPath = BuildLayoutPath(outFilename);
        Debug.Log($"Serializing to: '{outFilename}', using procedural params and elements not in scene from: '{this.layoutJSONFilename}'");


        // var house = jsonObj.ToObject<ProceduralHouse>();


        // if (this.namedSimObjects == null) {
        var simObjects = assignObjectIds();
        // }

        //  RegenerateWallsData();

        var assetDb = ProceduralTools.getAssetMap();
        var skipObjects = new HashSet<SimObjType>() {
                SimObjType.Doorway,
                SimObjType.Window
            };
        house.objects = simObjects
        .Where(obj => !skipObjects.Contains(obj.simObj.Type))
        .Select(obj => {
            Vector3 axis;
            float degrees;
            obj.simObj.transform.rotation.ToAngleAxis(out degrees, out axis);

            // PrefabUtility.UnpackPrefabInstance(obj.simObj.gameObject, PrefabUnpackMode.Completely, InteractionMode.UserAction);

            var bb = obj.simObj.AxisAlignedBoundingBox;

            // PrefabUtility.Pack

            // PrefabUtility.UnpackPrefabInstance(obj.simObj.gameObject, PrefabUnpackMode.Completely, InteractionMode.UserAction);

            RaycastHit hit;
            var didHit = Physics.Raycast(
                obj.simObj.transform.position,
                -Vector3.up,
                out hit,
                Mathf.Infinity,
                LayerMask.GetMask("NonInteractive")
            );
            string room = "";
            if (didHit) {
                room = hit.collider.transform.GetComponentInParent<SimObjPhysics>()?.ObjectID;
            }
            Debug.Log("Processing " + obj.assetId + " ...");
            if (!assetDb.ContainsKey(obj.assetId)) {
                Debug.LogError($"Asset '{obj.assetId}' not in AssetLibrary, so it won't be able to be loaded as part of a procedural scene. Save the asset and rebuild asset library.");
            }
                // var box = obj.simObj.BoundingBox.GetComponent<BoxCollider>();
                // box.enabled = true;
                var serializedObj = new HouseObject() {
                id = obj.id,
                position = bb.center,
                rotation = new FlexibleRotation() { axis = axis, degrees = degrees },
                kinematic = (obj.simObj.GetComponentInChildren<Rigidbody>()?.isKinematic).GetValueOrDefault(),
                boundingBox = new BoundingBox() { min = bb.center - (bb.size / 2.0f), max = bb.center + (bb.size / 2.0f) },
                room = room,
                types = new List<Taxonomy>() { new Taxonomy() { name = Enum.GetName(typeof(SimObjType), obj.simObj.ObjType) } },
                assetId = obj.assetId
            };
                // box.enabled = false;
                return serializedObj;
        }
        ).ToList();


        GameObject floorRoot;
        floorRoot = GameObject.Find(house.id);
        if (floorRoot == null) {
            floorRoot = GameObject.Find(ProceduralTools.DefaultHouseRootObjectName);
        }

        var roomIdToProps = floorRoot.GetComponentsInChildren<RoomProperties>()
            .ToDictionary(
                rp => rp.GetComponentInParent<SimObjPhysics>().ObjectID,
                rp => new {
                    roomProps = rp,
                    simOb = rp.GetComponentInParent<SimObjPhysics>()
                });

        house.rooms = house.rooms.Select(r => {
            r.roomType = roomIdToProps[r.id].roomProps.RoomType;
            // TODO add more room annotations here
            return r;
        }).ToList();

        var sceneLights = GameObject.Find(ProceduralTools.DefaultLightingRootName).GetComponentsInChildren<Light>().Concat(
            GameObject.Find(ProceduralTools.DefaultObjectsRootName).GetComponentsInChildren<Light>()
        );
        Debug.Log("Scene light count " + sceneLights.Count());

        var gatheredLights = new List<LightParameters>();

        house.proceduralParameters.lights = sceneLights.Select(
            l => {
                RaycastHit hit;
                var didHit = Physics.Raycast(
                    l.transform.position,
                    -Vector3.up,
                    out hit,
                    Mathf.Infinity,
                    LayerMask.GetMask("NonInteractive")
                );
                string room = "";
                if (didHit) {
                    room = hit.collider.transform.GetComponentInParent<SimObjPhysics>()?.ObjectID;
                }
                // didHit = Physics.Raycast(l.transform.position, -Vector3.up,out hit, 1.0f, LayerMask.GetMask("SimObjVisible", "Procedural1", "Procedural2", "Procedural3", "Procedural0"));
                string objectLink = "";
                var parentSim = l.GetComponentInParent<SimObjPhysics>();
                //SimObjType.Lamp
                if (parentSim != null) { //( parentSim?.ObjType).GetValueOrDefault() == SimObjType.FloorLamp )
                    objectLink = parentSim.ObjectID;
                }
                // if (didHit) {
                //     objectLink = hit.transform.GetComponentInParent<SimObjPhysics>()?.objectID;
                // }
                ShadowParameters sp = null;
                if (l.shadows != LightShadows.None) {
                    sp = new ShadowParameters() {
                        strength = l.shadowStrength,
                        type = Enum.GetName(typeof(LightShadows), l.shadows),
                        normalBias = l.shadowNormalBias,
                        bias = l.shadowBias,
                        nearPlane = l.shadowNearPlane,
                        resolution = Enum.GetName(typeof(UnityEngine.Rendering.LightShadowResolution), l.shadowResolution)
                    };
                }
                return new LightParameters() {
                    id = l.gameObject.name,
                    type = LightType.GetName(typeof(LightType), l.type),
                    position = l.transform.position,
                    rotation = FlexibleRotation.fromQuaternion(l.transform.rotation),
                    //rotation = FlexibleRotation.fromQuaternion(l.transform.rotation),
                    intensity = l.intensity,
                    indirectMultiplier = l.bounceIntensity,
                    range = l.range,
                    rgb = new SerializableColor() { r = l.color.r, g = l.color.g, b = l.color.b, a = l.color.a },
                    shadow = sp,
                    linkedObjectId = objectLink
                };
            }
        ).ToList();
        var probes = GameObject.Find(ProceduralTools.DefaultLightingRootName).GetComponentsInChildren<ReflectionProbe>().Concat(
            GameObject.Find(ProceduralTools.DefaultObjectsRootName).GetComponentsInChildren<ReflectionProbe>()
        );
        house.proceduralParameters.reflections = probes.Select( probeComp => {
            return new ProbeParameters() {
                background = SerializableColor.fromUnityColor(probeComp.backgroundColor),
                intensity = probeComp.intensity,
                boxSize = probeComp.size,
                shadowDistance = probeComp.shadowDistance,
                boxOffset = probeComp.center,
                id = probeComp.gameObject.name,
                position = probeComp.transform.position
            };
        }).ToList();

        foreach (var probe in house.proceduralParameters.reflections) {
                var go = new GameObject(probe.id);
                go.transform.position = probe.position;
                
                var probeComp = go.AddComponent<ReflectionProbe>();
                probeComp.backgroundColor = (probe.background?.toUnityColor()).GetValueOrDefault();
                probeComp.center = probe.boxOffset;
                probeComp.intensity = probe.intensity;
                probeComp.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
                probeComp.size = probe.boxSize;
                probeComp.shadowDistance = probe.shadowDistance;
            }



        house.proceduralParameters.ceilingMaterial = GameObject.Find(ProceduralTools.DefaultCeilingRootObjectName).GetComponentInChildren<MeshRenderer>().sharedMaterial.name;
        house.proceduralParameters.skyboxId = RenderSettings.skybox.name;

        Debug.Log("Lights " + house.proceduralParameters.lights.Count);

        var jsonResolver = new ShouldSerializeContractResolver();
        var outJson = JObject.FromObject(
            house,
                    new Newtonsoft.Json.JsonSerializer() {
                        ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                        ContractResolver = jsonResolver
                    });

        Debug.Log($"output json: {outJson.ToString()}");
        System.IO.File.WriteAllText(outPath, outJson.ToString());


    }

    private string BuildLayoutPath(string layoutFilename) {
        layoutFilename = layoutFilename.Trim();
        if (!layoutFilename.EndsWith(".json")) {
            layoutFilename += ".json";
        }
        var path = Application.dataPath + LoadBasePath + layoutFilename;
        return path;
    }
    // Start is called before the first frame update
    void Start() {

    }

    // Update is called once per frame
    void Update() {

    }
}
#endif