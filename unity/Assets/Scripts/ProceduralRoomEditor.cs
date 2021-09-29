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

[ExecuteInEditMode]
public class ProceduralRoomEditor : MonoBehaviour
{
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
    [Button(Expanded=true)]
    public void LoadLayout() {
        var path =  BuildLayoutPath(this.layoutJSONFilename);
        Debug.Log($"Loading: '{path}'");
        var jsonStr = System.IO.File.ReadAllText(path);
        Debug.Log($"json: {jsonStr}");

        JObject obj = JObject.Parse(jsonStr);

        this.loadedHouse = obj.ToObject<ProceduralHouse>();

        var houseObj = ProceduralTools.CreateHouse(
            this.loadedHouse,
            ProceduralTools.GetMaterials()
        );

    }


    [Button] 
    public void AssignIds() {
        if (namedSimObjects == null) {
            var root = GameObject.Find("Objects");
            //var counter = new Dictionary<SimObjType, int>();
            if (root != null) {
                var simobjs = root.transform.GetComponentsInChildren<SimObjPhysics>();
                
                this.namedSimObjects = simobjs
                    .Where(s => s.transform.parent.GetComponentInParent<SimObjPhysics>() == null)
                    .GroupBy(s => s.Type)
                    .SelectMany(objsOfType => objsOfType.Select((simObj, index) => new NamedSimObj {
                        assetId = PrefabNameRevert.GetPrefabAssetName(simObj.gameObject),
                        simObj = simObj,
                        id = $"{Enum.GetName(typeof(SimObjType), simObj.ObjType)}_{index}"
                    })).ToList();
                foreach (var namedObj in this.namedSimObjects) {
                    Debug.Log($" Renaming obj: {namedObj.simObj.gameObject.name} to {namedObj.id}, asset_id: {namedObj.assetId}" );
                    namedObj.simObj.objectID = namedObj.id;
                    namedObj.simObj.gameObject.name = namedObj.id;
                }
                // foreach (var namedObj in this.namedSimObjects) {
                //     Debug.Log($" Renamed obj: {namedObj.simObj.gameObject.name} to {namedObj.id}, asset_id: {namedObj.assetId}" );
                // }
            }
            Debug.Log("--- Ids assigned");
        }
        else {
            Debug.LogError("Ids already assigned!");
        }
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
            Debug.Log(" wall " + wall.GetComponentInParent<SimObjPhysics>().gameObject.name + " box " + box.center + " offset " + offset + " size: " + box.size);
        }
        var r = new List<Vector3>() { 
            wall.transform.TransformPoint(box.center - offset) ,
            wall.transform.TransformPoint(box.center + new Vector3(offset.x, -offset.y, 0.0f)),
            wall.transform.TransformPoint(box.center + new Vector3(offset.x, offset.y, 0.0f)),
            wall.transform.TransformPoint(box.center + new Vector3(-offset.x, offset.y, 0.0f)),
        };

        if (!reverse) {
            return r;
        }
        else {
            return new List<Vector3>() { 
                r[1],
                r[0],
                r[3],
                r[2]
            };
        }
    }

    public class ConnectionAndWalls{
        public WallRectangularHole connection;
        public List<PolygonWall> walls;

        public List<string> wallsToDelete;
    }

    private IEnumerable<ConnectionAndWalls> serializeConnections(IEnumerable<SimObjPhysics> connections, SimObjType filterType, string prefix) {
        var connectionsWithWalls = connections.Where(s => s.Type == filterType).Select( (d, i) => {
            var id = $"connection_{prefix}_{i}";
            Debug.Log($"----- {prefix} " + d.gameObject.name);
            var box = d.BoundingBox.GetComponent<BoxCollider>();
            box.enabled = true;
            var boxOffset = box.size / 2.0f;
            var poly = getPolygonFromWallObject(d.BoundingBox, false, true);
            var polyRev = getPolygonFromWallObject(d.BoundingBox, true);
            ConnectionProperties connectionProps = d.GetComponentInChildren<ConnectionProperties>();
            var materialId = "";

           
            // Doesnt work for some reason
    //         var colliders = Physics.OverlapBox(d.transform.TransformPoint(box.center), boxOffset * 4, 
    //   Quaternion.identity, 
    //     LayerMask.GetMask("SimObjVisible"), 
    //  QueryTriggerInteraction.UseGlobal);
    var colliders = GameObject.Find(
        $"/{ProceduralTools.DefaultRootStructureObjectName}/{ProceduralTools.DefaultRootWallsObjectName}"
        ).transform.GetComponentsInChildren<BoxCollider>();

        var wallColliders = colliders; //.Where(s => s.GetComponent<SimObjPhysics>()?.Type == SimObjType.Wall); //&& s.GetType().IsAssignableFrom(typeof(BoxCollider))).Select(c => c as BoxCollider);
        
        var p0 = poly[0];
        var p1 = poly[1];

        wallColliders = wallColliders.Where(s => s.GetComponent<SimObjPhysics>()?.Type == SimObjType.Wall).ToArray();
        Debug.Log("After filter "+ wallColliders.Length);

        var p0UpWorld =  d.transform.TransformPoint(poly[2] - poly[1]).normalized;

        // var p0World =  d.transform.worldToLocalMatrix.MultiplyPoint( p0);
        // var p1World = d.transform.worldToLocalMatrix.MultiplyPoint(p1);

        var p0World =  p0;
        var p1World =  p1;

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
                
                var localP0 =  collider.center - offset;
                // var localP0OnConnection =  
                
                var localP1 = collider.center + new Vector3(offset.x, 0, 0) - new Vector3(0, offset.y, 0);
                var topP0 = collider.transform.TransformPoint(collider.center + new Vector3(0, offset.y, 0));
                var cP0 = collider.transform.TransformPoint(localP0);

                
                var upVec =  (topP0 - cP0).normalized;
                var cP1 = collider.transform.TransformPoint(localP1);

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
        var dir  = (p1World - p0World);
        var dirNormalized  = dir.normalized;
        var eps = 0.1f;
        var tEps = 7e-2;

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

              if (name == "wall_0_16" || name == "wall_0_17" && d.gameObject.name == "Window_0") {
                  Debug.Log($" ^^^^^^^^^^ DIRECTION p0 {p0World.x},{p0World.y},{p0World.z} p1 {p1World.x},{p1World.y},{p1World.z} dir {dir.x},{dir.y},{dir.z}");
                Debug.Log($"************* wall {name}, wallp0 ({wallP0.x}, {wallP0.y}, {wallP0.z}),  wallp1 ({wallP1.x}, {wallP1.y}, {wallP1.z}), connP0 {p0Ref},  connP1 {p1Ref}, connP0ToWallP1 {connP0ToWallP1}, connP1ToWallP0 {connP1ToWallP0} dir {direction} connP1ToWallP0 {connP1ToWallP0} connP0ToWallP1 {connP0ToWallP1} t0.x {t0.x}, t0.y {t0.y}, t0.z {t0.z} t1.x {t1.x}, t1.y {t1.y}, t1.z {t1.z} onLine0 {onLine0}, onLine1 {onLine1} dot0 {dot0} dot1 {dot1}  num0.z {connP0ToWallP1.z} num1.z {connP1ToWallP0.z} dir.z {direction.z}" );
            }

            return  
             (onLine0 && ( (Math.Abs(direction.x) > dirEps && t0.x <= (1.0f+tEpsStrict) && t0.x >= (0.0f-tEpsStrict)) || ((Math.Abs(direction.z) > dirEps && t0.z <= (1.0f+tEpsStrict) && t0.z >= (0.0f-tEpsStrict))) ))
              || 
             (onLine1 && ( (Math.Abs(direction.x) > dirEps && t1.x <= (1.0f+tEpsStrict) && t1.x >= (0.0f-tEpsStrict)) || ((Math.Abs(direction.z) > dirEps && t1.z <= (1.0f+tEpsStrict) && t1.z >= (0.0f-tEpsStrict))) ));
        }

        // bool pointOnWallLine(Vector3 p, Vector3 direction, Vector3 origin, string name) {
        //     var num = (p - origin);
        //     var proyLength = Vector3.Dot(num, direction);
        //     num = direction * proyLength;
           

        //     var t = new Vector3(num.x / direction.x, num.y / direction.y, num.z / direction.z);
        //     var onLine = Math.Abs(t.x - t.z) < eps;

        //      if (name == "wall_1_7" || name == "wall_1_6" || name == "wall_1_5" && d.gameObject.name == "Window_1") {
        //         Debug.Log($"************* wall {name}, p {p}, orig {origin}, diff {(p - origin)} dir {direction} PROJ {proyLength} num {num} t.x {t.x}, t.y {t.y}, t.z {t.z} onLine {onLine}" );
        //     }
        //     return onLine && t.x <= (1.0f+tEps) && t.x >= (0.0f-tEps); 
        // }

        bool pointOnWallLine(Vector3 p, Vector3 direction, Vector3 origin, string name) {

            var originRef = new Vector3(origin.x, p.y, origin.z);
            var originToP = (p - originRef);

            var dot0 = Vector3.Dot(originToP.normalized, direction.normalized);

            var t = new Vector3(originToP.x / direction.x, originToP.y / direction.y, originToP.z / direction.z);
            // var proyLength = Vector3.Dot(num, direction);

            var onLine = Math.Abs(dot0) >= 1.0f - eps;

             if (name == "wall_1_7" || name == "wall_1_6" || name == "wall_1_5" && d.gameObject.name == "Window_1") {
                Debug.Log($"!!!!!!!!!!!!!!! wall {name}, p {p}, orig {originRef}, dir {direction} dot0 {dot0} originToP {originToP} t.x {t.x}, t.y {t.y}, t.z {t.z} onLine {onLine}" );
            }

            return onLine && ( (Math.Abs(direction.x) > dirEps && t.x <= (1.0f+tEps) && t.x >= (0.0f-tEps)) || ((Math.Abs(direction.z) > dirEps && t.z <= (1.0f+tEps) && t.z >= (0.0f-tEps))) );
           
        }

        // Func<Vector3, Vector3, Vector3, string, bool, bool> pointOnWallLine = (Vector3 p, Vector3 direction, Vector3 origin, string name, bool ignoreSign) => {
            
        // };
        var wallRight = colliderDistances.Aggregate( new {
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
                && Vector3.Dot(next.collider.transform.forward, d.transform.forward) >= normalEps
                && !pointOnWallLine(next.p0, dirNormalized, p0World, next.collider.gameObject.name) ? next: min 
        );

        // && Vector3.Dot(next.normal, normal) > 0
        var wallLeft = colliderDistances.Aggregate( new {
                collider = box,
                p0SqrDistance = float.MaxValue,
                p1SqrDistance = float.MaxValue,
                p0 = new Vector3(),
                p1 = new Vector3(),
                height = 0.0f,
                normal = new Vector3()
            }, 
            (min, next) =>  {
                var name = next.collider.gameObject.name;
                if (name == "wall_1_7" || name == "wall_1_6" || name == "wall_1_5" && d.gameObject.name == "Window_1") {
                Debug.Log($"########## wall Left {name} p1SqrDistance {next.p1SqrDistance}, normal {Vector3.Dot(next.collider.transform.forward, d.transform.forward)} !onLine {!pointOnWallLine(next.p1, -dirNormalized, p1World, next.collider.gameObject.name)}" );
            }
            
                return 
                min.p1SqrDistance > next.p1SqrDistance  
                && Vector3.Dot(next.collider.transform.forward, d.transform.forward) >= normalEps 
                // && wallPointOnConnectionLine(next.p0, next.p1, -dirNormalized)
                && !pointOnWallLine(next.p1, -dir, p1World, next.collider.gameObject.name)
                ? next: min;
            }
        );


        var backWallClosestLeft = colliderDistances.Aggregate( new {
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
                && Vector3.Dot(next.collider.transform.forward, -d.transform.forward) >= normalEps
                && !pointOnWallLine(next.p0, -dirNormalized, p1World, next.collider.gameObject.name)
                 ? next: min 
        );


        var backWallClosestRight = colliderDistances.Aggregate( new {
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
                Vector3.Dot(next.collider.transform.forward, -d.transform.forward) >= normalEps 
                && !pointOnWallLine(next.p1, dirNormalized, p0World, next.collider.gameObject.name)
                ? next: min 
        );


        var toDelete = colliderDistances.Where( 
            next => wallPointOnConnectionLine(next.p0, next.p1, dir, next.collider.transform.forward, next.collider.gameObject.name)
        );

        Debug.Log("&&&&&&&&&& TODELETE " + string.Join(", ", toDelete.Select(w => w.collider.gameObject.name)));

        //Debug.Log("Walls0 " + wall0.collider.name + " wall 1 " +wall1.collider.gameObject.name);
        // var debug = colliderDistances.ToList()[0];
        // // var debug = colliderDistances.ElementAt(0);
        // Debug.Log(" p0 CDist " + debug.p0SqrDistance + " p1 CDist " + debug.p1SqrDistance + " name " + debug.collider.GetComponentInParent<SimObjPhysics>().ObjectID);
        Debug.Log("walls_right " + wallRight.collider.gameObject.name +  " dist " + wallRight.p0SqrDistance + " wall_left " +wallLeft.collider.gameObject.name + " dist " + wallLeft.p1SqrDistance +
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
            //     room_id = connectionProps?.OpenFromRoomId,
            //     polygon = poly,
            //     // TODO get material somehow
            //     // material = connectionProps?.openFromWallMaterial?.name
            //     material = wallRight.collider.GetComponent<MeshRenderer>().sharedMaterial.name

            //     // material_tiling_x_divisor = box.size.x / material.mainTextureScale.x,
            //     // material_tiling_y_divisor = box.size.y / material.mainTextureScale.y};
            // };
            //  var wallRev = new PolygonWall {
            //     id = $"wall_{id}_back",
            //     room_id = connectionProps?.OpenToRoomId,
            //     polygon = polyRev,
            //     // TODO get material somehow
            //     // material = connectionProps?.openToWallMaterial?.name

            //     material = backWallClosestLeft.collider.GetComponent<MeshRenderer>().sharedMaterial.name

            //     // material_tiling_x_divisor = box.size.x / material.mainTextureScale.x,
            //     // material_tiling_y_divisor = box.size.y / material.mainTextureScale.y};
            // };

            var wall = createNewWall(
                    $"wall_{id}_front", 
                    connectionProps, 
                    wallLeft.p1, 
                    wallRight.p0, 
                    wallLeft.height, 
                    wallLeft.collider.GetComponent<MeshRenderer>().sharedMaterial
            );

            var wallRev = createNewWall(
                    $"wall_{id}_back", 
                    connectionProps, 
                    backWallClosestRight.p1, 
                    backWallClosestLeft.p0, 
                    backWallClosestRight.height, 
                    backWallClosestRight.collider.GetComponent<MeshRenderer>().sharedMaterial
            );

            WallRectangularHole connection = null;

            if (filterType == SimObjType.Doorway) {
                connection = new Thor.Procedural.Data.Door {

                    id = id,
        
                    room_0 = connectionProps?.OpenFromRoomId,
                    room_1 = connectionProps?.OpenToRoomId,
                    wall_0 = wall.id,
                    wall_1 = wallRev.id,
                    bounding_box = new Thor.Procedural.Data.BoundingBox { min = box.center - boxOffset, max = box.center + boxOffset },
                    type = connectionProps?.Type,
                    openable = d.SecondaryProperties.Contains(SimObjSecondaryProperty.CanOpen),
                    // TODO
                    open = false,
                    asset_id = PrefabNameRevert.GetPrefabAssetName(d.gameObject)

                };
            }
            else if (filterType == SimObjType.Window) {
                connection = new Thor.Procedural.Data.Window {

                    id = id,
        
                    room_0 = connectionProps?.OpenFromRoomId,
                    room_1 = connectionProps?.OpenToRoomId,
                    wall_0 = wall.id,
                    wall_1 = wallRev.id,
                    bounding_box = new Thor.Procedural.Data.BoundingBox { min = box.center - boxOffset, max = box.center + boxOffset },
                    type = connectionProps?.Type,
                    openable = d.SecondaryProperties.Contains(SimObjSecondaryProperty.CanOpen),
                    // TODO
                    open = false,
                    asset_id = PrefabNameRevert.GetPrefabAssetName(d.gameObject)

                };
            }

            // var connection = new Thor.Procedural.Data.Door {

            //      id = id,
       
            //     room_0 = connectionProps?.OpenFromRoomId,
            //     room_1 = connectionProps?.OpenToRoomId,
            //     wall_0 = wall.id,
            //     wall_1 = wallRev.id,
            //     bounding_box = new Thor.Procedural.Data.BoundingBox { min = box.center - boxOffset, max = box.center + boxOffset },
            //     type = "???",
            //     openable = d.SecondaryProperties.Contains(SimObjSecondaryProperty.CanOpen),
            //     // TODO
            //     open = false,
            //     asset_id = PrefabNameRevert.GetPrefabAssetName(d.gameObject)

            // };
            box.enabled = false;

            var wallsToCreate = new List<PolygonWall>() {}; 

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

             Debug.Log("Walls to create: " + string.Join(", ", wallsToCreate.Select(w => w.id)));
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
                walls = new List<PolygonWall>() { wall, wallRev },
                wallsToDelete = toDelete.Select(o => o.collider.name).ToList()
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
    //             room_id = d.GetComponentInChildren<ConnectionProperties>().OpenFromRoomId,
    //             polygon = poly,
    //             // TODO get material somehow
    //             material = ""

    //             // material_tiling_x_divisor = box.size.x / material.mainTextureScale.x,
    //             // material_tiling_y_divisor = box.size.y / material.mainTextureScale.y};
    //         };
    //          var wallRev = new PolygonWall {
    //             id = id,
    //             room_id = d.GetComponentInChildren<ConnectionProperties>().OpenFromRoomId,
    //             polygon = polyRev,
    //             // TODO get material somehow
    //             material = ""

    //             // material_tiling_x_divisor = box.size.x / material.mainTextureScale.x,
    //             // material_tiling_y_divisor = box.size.y / material.mainTextureScale.y};
    //         };
    //         var door = new ?? {

    //              id = $"wall_{id}_back",
       
    //             room_0 = d.GetComponentInChildren<ConnectionProperties>().OpenFromRoomId,
    //             room_1 = d.GetComponentInChildren<ConnectionProperties>().OpenToRoomId,
    //             wall_0 = wall.id,
    //             wall_1 = wallRev.id,
    //             bounding_box = new Thor.Procedural.Data.BoundingBox { min = box.center - boxOffset, max = box.center + boxOffset },
    //             type = "???",
    //             openable = d.SecondaryProperties.Contains(SimObjSecondaryProperty.CanOpen),
    //             // TODO
    //             open = false,
    //             asset_id = PrefabNameRevert.GetPrefabAssetName(d.gameObject)

    //         } as WallRectangularHole;
    //         return new ConnectionGameObjects<T>{
    //             door = door,
    //             walls = new List<PolygonWall>() { wall, wallRev}
    //         };
    //     });
    //     return doorWalls;
    // }

    private List<(Vector3, Color)> spheres = new List<(Vector3, Color)>();

    void OnDrawGizmosSelected()
    {
        
       foreach (var (c, color) in spheres) {
           Gizmos.color = color;
         Gizmos.DrawSphere(c, 0.2f);
       }
    }

    private PolygonWall createNewWall(string id, ConnectionProperties connectionProps, Vector3 p0, Vector3 p1, float height, Material material) {
        var len = (p1 - p0).magnitude;
         return new PolygonWall {
            id = id,
            room_id = connectionProps?.OpenToRoomId,
            polygon = getPolygonFromWallPoints(p0, p1, height),
            // TODO get material somehow
            // material = connectionProps?.openFromWallMaterial?.name
            material = material.name,

            material_tiling_x_divisor = len / material.mainTextureScale.x,
            material_tiling_y_divisor = height / material.mainTextureScale.y
        };
        
    }

    [Button]
     public void RegenerateWallsData() {
        var root = GameObject.Find(ProceduralTools.DefaultRootWallsObjectName);
        var wallsGOs = root.GetComponentsInChildren<SimObjPhysics>().Where(s => s.Type == SimObjType.Wall);
        var wallsDict = this.loadedHouse.walls.ToDictionary(w => w.id, w => w);
        var minimumWallSqrDistanceCreateEpsilon = 0.03f;

        var wallsJson = wallsGOs.Select((w, i) =>  {
            var material = w.GetComponent<MeshRenderer>().sharedMaterial;
            var poly = getPolygonFromWallObject(w.gameObject);
            var box = w.GetComponent<BoxCollider>();
    
            return new PolygonWall {
                id = $"wall_{i}",
                room_id = w.GetComponentInChildren<WallProperties>().RoomId,
                polygon = poly,
                material = material.name,
                material_tiling_x_divisor = box.size.x / material.mainTextureScale.x,
                material_tiling_y_divisor = box.size.y / material.mainTextureScale.y};
            }
        );


        // var doorWalls = GameObject.Find(ProceduralTools.DefaultObjectsRootName).GetComponentsInChildren<SimObjPhysics>().Where(
        //     s => s.Type == SimObjType.Doorway 
        // ).Select( (d, i) => {
        //     Debug.Log("----- Door " + d.gameObject.name);
        //     var box = d.BoundingBox.GetComponent<BoxCollider>();
        //     box.enabled = true;
        //     var boxOffset = box.size / 2.0f;
        //     var poly = getPolygonFromWallObject(d.BoundingBox, false, true);
        //     var polyRev = getPolygonFromWallObject(d.BoundingBox, true);
        //     var id = $"door_{i}";
        //     ConnectionProperties connectionProps = d.GetComponentInChildren<ConnectionProperties>();
        //     var materialId = "";

           
            // Doesnt work for some reason
    //         var colliders = Physics.OverlapBox(d.transform.TransformPoint(box.center), boxOffset * 4, 
    //   Quaternion.identity, 
    //     LayerMask.GetMask("SimObjVisible"), 
    //  QueryTriggerInteraction.UseGlobal);
    // var colliders = GameObject.Find(
    //     $"/{ProceduralTools.DefaultRootStructureObjectName}/{ProceduralTools.DefaultRootWallsObjectName}"
    //     ).transform.GetComponentsInChildren<BoxCollider>();

    //     var wallColliders = colliders; //.Where(s => s.GetComponent<SimObjPhysics>()?.Type == SimObjType.Wall); //&& s.GetType().IsAssignableFrom(typeof(BoxCollider))).Select(c => c as BoxCollider);
        
    //     var p0 = poly[0];
    //     var p1 = poly[1];

    //     wallColliders = wallColliders.Where(s => s.GetComponent<SimObjPhysics>()?.Type == SimObjType.Wall).ToArray();
    //     Debug.Log("After filter "+ wallColliders.Length);

    //     var p0UpWorld =  d.transform.TransformPoint(poly[2] - poly[1]).normalized;

    //     // var p0World =  d.transform.worldToLocalMatrix.MultiplyPoint( p0);
    //     // var p1World = d.transform.worldToLocalMatrix.MultiplyPoint(p1);

    //     var p0World =  p0;
    //     var p1World =  p1;

    //     spheres.Add((p0World, Color.cyan));
    //     spheres.Add((p1World, Color.green));
    //     Debug.Log("diff " + (p1 - p0) + " p0: " + p0 + " p1: " + p1 + " p0World: " + p0World + " p1World: " + p1World); 
    //     //     Gizmos.color = Color.yellow;
    //     //     Debug.Dr
    //     //  Gizmos.DrawSphere(p0World, 0.2f);
    //     //  Gizmos.DrawSphere(p1World, 0.2f);

    //     var normal = Vector3.Cross((p1World - p0World).normalized, p0UpWorld);

    //     var colliderDistances = wallColliders.Select(collider => {
    //             var offset = collider.size / 2.0f;
    //             var localP0 =  collider.center - offset;
    //             var localP1 = collider.center + new Vector3(offset.x, 0, 0) - new Vector3(0, offset.y, 0);
    //             var topP0 = collider.transform.TransformPoint(collider.center + new Vector3(0, offset.y, 0));
    //             var cP0 = collider.transform.TransformPoint(localP0);
    //             var upVec =  (topP0 - cP0).normalized;
    //             var cP1 = collider.transform.TransformPoint(localP1);
    //             return new {
    //                 collider = collider,
    //                 p0SqrDistance = (p0World - cP1).sqrMagnitude,
    //                 p1SqrDistance = (p1World - cP0).sqrMagnitude,
    //                 p0 = cP0,
    //                 p1 = cP1,
    //                 height = collider.size.y,
    //                 normal = Vector3.Cross((cP1 - cP0).normalized, upVec)
    //             };
    //     });

    //     // && Vector3.Dot(next.normal, normal) > 0

    //     Debug.Log("Colliders returned " + colliders.Count() + " collider distances " + colliderDistances.Count());
    //     var wallRight = colliderDistances.Aggregate( new {
    //             collider = box,
    //             p0SqrDistance = float.MaxValue,
    //             p1SqrDistance = float.MaxValue,
    //             p0 = new Vector3(),
    //             p1 = new Vector3(),
    //             height = 0.0f,
    //             normal = new Vector3()
    //         }, 
    //         (min, next) => 
    //             min.p0SqrDistance > next.p0SqrDistance && Vector3.Dot(next.collider.transform.forward, d.transform.forward) >= 0 ? next: min 
    //     );

    //     // && Vector3.Dot(next.normal, normal) > 0
    //     var wallLeft = colliderDistances.Aggregate( new {
    //             collider = box,
    //             p0SqrDistance = float.MaxValue,
    //             p1SqrDistance = float.MaxValue,
    //             p0 = new Vector3(),
    //             p1 = new Vector3(),
    //             height = 0.0f,
    //             normal = new Vector3()
    //         }, 
    //         (min, next) => 
    //             min.p1SqrDistance > next.p1SqrDistance  && Vector3.Dot(next.collider.transform.forward, d.transform.forward) >= 0 ? next: min 
    //     );


    //     var backWallClosestLeft = colliderDistances.Aggregate( new {
    //             collider = box,
    //             p0SqrDistance = float.MaxValue,
    //             p1SqrDistance = float.MaxValue,
    //             p0 = new Vector3(),
    //             p1 = new Vector3(),
    //             height = 0.0f,
    //             normal = new Vector3()
    //         }, 
    //         (min, next) => 
    //             min.p0SqrDistance > next.p0SqrDistance && Vector3.Dot(next.collider.transform.forward, -d.transform.forward) >= 0 ? next: min 
    //     );

    //     var backWallClosestRight = colliderDistances.Aggregate( new {
    //             collider = box,
    //             p0SqrDistance = float.MaxValue,
    //             p1SqrDistance = float.MaxValue,
    //             p0 = new Vector3(),
    //             p1 = new Vector3(),
    //             height = 0.0f,
    //             normal = new Vector3()
    //         }, 
    //         (min, next) => 
    //             min.p1SqrDistance > next.p1SqrDistance && Vector3.Dot(next.collider.transform.forward, -d.transform.forward) >= 0 ? next: min 
    //     );

    //     //Debug.Log("Walls0 " + wall0.collider.name + " wall 1 " +wall1.collider.gameObject.name);
    //     var debug = colliderDistances.ToList()[0];
    //     // var debug = colliderDistances.ElementAt(0);
    //     Debug.Log(" p0 CDist " + debug.p0SqrDistance + " p1 CDist " + debug.p1SqrDistance + " name " + debug.collider.GetComponentInParent<SimObjPhysics>().ObjectID);
    //     Debug.Log("Walls_0 " + wallRight.collider.gameObject.name +  " dist " + wallRight.p0SqrDistance + " wall_1 " +wallLeft.collider.gameObject.name + " dist " + wallLeft.p1SqrDistance +
    //     " backwallLeft " + backWallClosestLeft.collider.gameObject.name + " backwallRight " + backWallClosestRight.collider.name);



    //     //     var m_HitDetect = Physics.BoxCast(box.bounds.center, boxOffset * 4, transform.forward, out m_Hit, transform.rotation, m_MaxDistance);
    //     // if (m_HitDetect)
    //     // {
    //     //     //Output the name of the Collider your Box hit
    //     //     Debug.Log("Hit : " + m_Hit.collider.name);
    //     // }
    //         //  var wall = new PolygonWall {
    //         //     id = $"wall_{id}_front",
    //         //     room_id = connectionProps?.OpenFromRoomId,
    //         //     polygon = poly,
    //         //     // TODO get material somehow
    //         //     // material = connectionProps?.openFromWallMaterial?.name
    //         //     material = wallRight.collider.GetComponent<MeshRenderer>().sharedMaterial.name

    //         //     // material_tiling_x_divisor = box.size.x / material.mainTextureScale.x,
    //         //     // material_tiling_y_divisor = box.size.y / material.mainTextureScale.y};
    //         // };
    //         //  var wallRev = new PolygonWall {
    //         //     id = $"wall_{id}_back",
    //         //     room_id = connectionProps?.OpenToRoomId,
    //         //     polygon = polyRev,
    //         //     // TODO get material somehow
    //         //     // material = connectionProps?.openToWallMaterial?.name

    //         //     material = backWallClosestLeft.collider.GetComponent<MeshRenderer>().sharedMaterial.name

    //         //     // material_tiling_x_divisor = box.size.x / material.mainTextureScale.x,
    //         //     // material_tiling_y_divisor = box.size.y / material.mainTextureScale.y};
    //         // };

    //         var wall = createNewWall(
    //                 $"wall_{id}_front", 
    //                 connectionProps, 
    //                 wallLeft.p1, 
    //                 wallRight.p0, 
    //                 wallLeft.height, 
    //                 wallLeft.collider.GetComponent<MeshRenderer>().sharedMaterial
    //         );

    //         var wallRev = createNewWall(
    //                 $"wall_{id}_back", 
    //                 connectionProps, 
    //                 backWallClosestRight.p1, 
    //                 backWallClosestLeft.p0, 
    //                 backWallClosestRight.height, 
    //                 backWallClosestRight.collider.GetComponent<MeshRenderer>().sharedMaterial
    //         );

    //         var door = new Thor.Procedural.Data.Door {

    //              id = id,
       
    //             room_0 = connectionProps?.OpenFromRoomId,
    //             room_1 = connectionProps?.OpenToRoomId,
    //             wall_0 = wall.id,
    //             wall_1 = wallRev.id,
    //             bounding_box = new Thor.Procedural.Data.BoundingBox { min = box.center - boxOffset, max = box.center + boxOffset },
    //             type = "???",
    //             openable = d.SecondaryProperties.Contains(SimObjSecondaryProperty.CanOpen),
    //             // TODO
    //             open = false,
    //             asset_id = PrefabNameRevert.GetPrefabAssetName(d.gameObject)

    //         };
    //         box.enabled = false;

    //         var wallsToCreate = new List<PolygonWall>() {}; 

    //         // if ( wallLeft.p1SqrDistance > minimumWallSqrDistanceCreateEpsilon ) {
    //         //     wallsToCreate.Add(
    //         //         createNewWall(
    //         //             $"wall_{id}_left_front", connectionProps, wallLeft.p1, p0World, wallLeft.height, wallLeft.collider.GetComponent<MeshRenderer>().sharedMaterial)
    //         //     );
    //         // }

    //         // if ( (backWallClosestLeft.p0 - p0World).magnitude > minimumWallSqrDistanceCreateEpsilon ) {

    //         //     wallsToCreate.Add(
    //         //         createNewWall($"wall_{id}_right_back", connectionProps, backWallClosestLeft.p0, p0World, backWallClosestLeft.height, backWallClosestLeft.collider.GetComponent<MeshRenderer>().sharedMaterial)
    //         //     );
    //         // }

    //         // if ( wallRight.p0SqrDistance > minimumWallSqrDistanceCreateEpsilon ) {
    //         //     wallsToCreate.Add(
    //         //         createNewWall($"wall_{id}_right_front", connectionProps, wallRight.p0, p1World, wallRight.height, wallRight.collider.GetComponent<MeshRenderer>().sharedMaterial)
    //         //     );
    //         // }

    //         //  if ( (backWallClosestRight.p1 - p1World).magnitude > minimumWallSqrDistanceCreateEpsilon ) {

    //         //     //  Debug.Log(" Wall closest right dist " + )
    //         //      wallsToCreate.Add(
    //         //         createNewWall($"wall_{id}_right_back", connectionProps, backWallClosestRight.p1, p1World, backWallClosestLeft.height, backWallClosestLeft.collider.GetComponent<MeshRenderer>().sharedMaterial)
    //         //     );
    //         //  }

    //          Debug.Log("Walls to create: " + string.Join(", ", wallsToCreate.Select(w => w.id)));

    //         return new {
    //             door = door,
    //             walls = new List<PolygonWall>() { wall, wallRev }
    //         };
    //     });
        var simObjs = GameObject.Find(ProceduralTools.DefaultObjectsRootName).GetComponentsInChildren<SimObjPhysics>();
        var doorWalls = serializeConnections(simObjs, SimObjType.Doorway, "door");

        var windowsAndWalls = serializeConnections(simObjs, SimObjType.Window, "window");
        // var windowsAndWalls = new List<ConnectionAndWalls>(){};

        // var windowsAndWalls = GameObject.Find(ProceduralTools.DefaultObjectsRootName).GetComponentsInChildren<SimObjPhysics>().Where(
        //     s => s.Type == SimObjType.Window 
        // ).Select( (d, i) => {
        //     var box = d.BoundingBox.GetComponent<BoxCollider>();
        //     var boxOffset = box.size / 2.0f;
        //     var poly = getPolygonFromWallObject(d.BoundingBox);
        //     var polyRev = getPolygonFromWallObject(d.BoundingBox, true);
        //     var id = $"window_{i}";
            
        //     ConnectionProperties connectionProps = d.GetComponentInChildren<ConnectionProperties>();
        //     // if (connectionProps != null) {
        //     // }
        //     // connectionProps?.OpenFromRoomId
        //      var wall = new PolygonWall {
        //         id = $"wall_{id}_front",
        //         room_id = connectionProps?.OpenFromRoomId,
        //         polygon = poly,
        //         // TODO get material somehow
        //         material = connectionProps?.openFromWallMaterial?.name

        //         // material_tiling_x_divisor = box.size.x / material.mainTextureScale.x,
        //         // material_tiling_y_divisor = box.size.y / material.mainTextureScale.y};
        //     };
        //      var wallRev = new PolygonWall {
        //         id =  $"wall_{id}_back",
        //         room_id = connectionProps?.OpenFromRoomId,
        //         polygon = polyRev,
        //         // TODO get material somehow
        //         material = ""

        //         // material_tiling_x_divisor = box.size.x / material.mainTextureScale.x,
        //         // material_tiling_y_divisor = box.size.y / material.mainTextureScale.y};
        //     };
        //     var window = new Thor.Procedural.Data.Window {

        //          id = id,
       
        //         room_0 = connectionProps?.OpenFromRoomId,
        //         room_1 = connectionProps?.OpenToRoomId,
        //         wall_0 = wall.id,
        //         wall_1 = wallRev.id,
        //         bounding_box = new Thor.Procedural.Data.BoundingBox { min = box.center - boxOffset, max = box.center + boxOffset },
        //         type = "???",
        //         openable = d.SecondaryProperties.Contains(SimObjSecondaryProperty.CanOpen),
        //         // TODO
        //         open = false,
        //         asset_id = PrefabNameRevert.GetPrefabAssetName(d.gameObject)

        //     };
        //     return new {
        //         window = window,
        //         walls = new List<PolygonWall>() { wall, wallRev}
        //     };
        // });

     

        var allWalls = wallsJson.Concat(doorWalls.SelectMany(d => d.walls)).Concat(windowsAndWalls.SelectMany(d => d.walls));
        var doors = doorWalls.Select(d => d.connection as Thor.Procedural.Data.Door);
        var windows = windowsAndWalls.Select(d => d.connection as Thor.Procedural.Data.Window);

        this.loadedHouse.walls = allWalls.ToList();
        this.loadedHouse.doors = doors.ToList();
        this.loadedHouse.windows = windows.ToList();
    }

     [Button]
    public void ReloadScene() {
#if UNITY_EDITOR        
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
       
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scene.path);
#endif        
    }


    [Button(Expanded=true)] 
    public void SerializeScene(string outFilename) {
        // var path = BuildLayoutPath(layoutJSONFilename);
        // var jsonStr = System.IO.File.ReadAllText(path);
        // JObject jsonObj = JObject.Parse(jsonStr);
        // this.loadedHouse = jsonObj.ToObject<ProceduralHouse>();

        if (this.loadedHouse != null) {
            var outPath = BuildLayoutPath(outFilename);
            Debug.Log($"Serializing to: '{outFilename}'");
           

            // var house = jsonObj.ToObject<ProceduralHouse>();
            
            
            if (this.namedSimObjects != null) {
                var assetDb = ProceduralTools.getAssetMap();
                this.loadedHouse.objects = this.namedSimObjects.Select(obj => {
                    Vector3 axis;
                    float degrees; 
                    obj.simObj.transform.rotation.ToAngleAxis(out degrees, out axis);
                    var bb = obj.simObj.AxisAlignedBoundingBox;
                    RaycastHit hit;
                    var didHit = Physics.Raycast(obj.simObj.transform.position, -Vector3.up,out hit, Mathf.Infinity, 1 << 12);
                    string room = "";
                    if (didHit) {
                        room = hit.collider.transform.GetComponentInParent<SimObjPhysics>()?.ObjectID;
                    }
                    Debug.Log("Processing " + obj.assetId + " ...");
                    if (!assetDb.ContainsKey(obj.assetId)) {
                        Debug.LogError($"Asset '{obj.assetId}' not in AssetLibrary, so it won't be able to be loaded as part of a procedural scene. Save the asset and rebuild asset library.");
                    }
                    return new HouseObject(){
                        id = obj.id,
                        position = obj.simObj.transform.position,
                        rotation = new AxisAngleRotation() { axis = axis, degrees = degrees },
                        kinematic = (obj.simObj.GetComponentInChildren<Rigidbody>()?.isKinematic).GetValueOrDefault(),
                        bounding_box = new BoundingBox() { min =  bb.center - (bb.size / 2.0f), max = bb.center + (bb.size / 2.0f) },
                        room = room,
                        types = new List<Taxonomy>() { new Taxonomy() { name = Enum.GetName(typeof(SimObjType), obj.simObj.ObjType) } },
                        asset_id = obj.assetId
                    };
                }
                ).ToList();
            }

            GameObject floorRoot; 
            floorRoot = GameObject.Find(loadedHouse.id);
            if (floorRoot == null) {
                floorRoot = GameObject.Find(ProceduralTools.DefaultFloorRootObjectName);
            }

            var roomIdToProps = floorRoot.GetComponentsInChildren<RoomProperties>()
                .ToDictionary(
                    rp => rp.GetComponentInParent<SimObjPhysics>().ObjectID,
                    rp => new {
                        roomProps = rp,
                        simOb = rp.GetComponentInParent<SimObjPhysics>()
            });

            loadedHouse.rooms = loadedHouse.rooms.Select(r => {
                r.type = roomIdToProps[r.id].roomProps.RoomType;
                // TODO add more room annotations here
                return r;
            }).ToList();
            
            var sceneLights = GameObject.Find(ProceduralTools.DefaultLightingRootName).GetComponentsInChildren<Light>().Concat( 
                GameObject.Find(ProceduralTools.DefaultObjectsRootName).GetComponentsInChildren<Light>()
            );
            Debug.Log("Scene light count " + sceneLights.Count());

            var gatheredLights = new List<LightParameters>();

            //this.loadedHouse.procedural_parameters.lights = new List<LightParameters>();

            this.loadedHouse.procedural_parameters.lights = sceneLights.Select(l => {
                 RaycastHit hit;
                    var didHit = Physics.Raycast(l.transform.position, -Vector3.up,out hit, Mathf.Infinity, 1 << 12);
                    string room = "";
                    if (didHit) {
                        room = hit.collider.transform.GetComponentInParent<SimObjPhysics>()?.ObjectID;
                    }
                    // didHit = Physics.Raycast(l.transform.position, -Vector3.up,out hit, 1.0f, 1 << 8);
                    string objectLink = "";
                    var parentSim = l.GetComponentInParent<SimObjPhysics>();
                    //SimObjType.Lamp
                    if( parentSim != null) { //( parentSim?.ObjType).GetValueOrDefault() == SimObjType.FloorLamp )
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
                            normal_bias = l.shadowNormalBias,
                            bias = l.shadowBias,
                            near_plane = l.shadowNearPlane,
                            resolution = Enum.GetName(typeof(UnityEngine.Rendering.LightShadowResolution), l.shadowResolution)
                        };
                    }
                    return new LightParameters()  {
                        id = l.gameObject.name,
                        room_id = room,
                        type = LightType.GetName(typeof(LightType), l.type),

                        

                        position = l.transform.position,
                        //rotation = AxisAngleRotation.fromQuaternion(l.transform.rotation),
                        intensity = l.intensity,
                        indirect_multiplier = l.bounceIntensity,
                        range = l.range,
                        rgb = new SerializableColor() {r = l.color.r, g = l.color.g, b = l.color.b, a = l.color.a},
                        shadow = sp,
                        object_id = objectLink
                };
            }).ToList();
            
          
          
            

            // var m = sceneLights.Select(l => 
            //      new LightParameters()  {
            //             id = l.gameObject.name,
            //             room_id = "room",
            //             type = LightType.GetName(typeof(LightType), l.type),

                        

            //             position = l.transform.position,
            //             rotation = AxisAngleRotation.fromQuaternion(l.transform.rotation),
            //             intensity = l.intensity,
            //             range = l.range,
            //             rgb = l.color,
            //             shadow = null,
            //             object_id = ""

            //     }
            // ).ToList();
            //Debug.Log(gatheredLights.Count);

            //this.loadedHouse.procedural_parameters.lights = new List<LightParameters>() {gatheredLights[0]};
            //this.loadedHouse.procedural_parameters.lights.

            // this.loadedHouse.procedural_parameters.lights = new List<LightParameters>(gatheredLights.Count);
             //this.loadedHouse.procedural_parameters.lights.AddRange(Enumerable.Repeat(this.loadedHouse.procedural_parameters.lights[0], 12));

            //  this.loadedHouse.procedural_parameters = new ProceduralParameters() {
            //     lights = gatheredLights
            //  };


            // for (int i = 0; i < gatheredLights.Count; i++) {
            //     Debug.Log("Light copy: " + i);
            //     this.loadedHouse.procedural_parameters.lights[i] =gatheredLights[i];
            // }
            //loadedHouse.procedural_parameters.lights = gatheredLights;


            // loadedHouse.procedural_parameters.lights = sceneLights.Select(l => {
            //     RaycastHit hit;
            //     //var didHit = Physics.Raycast(l.transform.position, -Vector3.up,out hit, Mathf.Infinity, 1 << 12);
            //     string room = "";
            //     // if (didHit) {
            //     //     room = hit.collider.transform.GetComponentInParent<SimObjPhysics>()?.ObjectID;
            //     // }
            //     //didHit = Physics.Raycast(l.transform.position, -Vector3.up,out hit, Mathf.Infinity, 1 << 8);
            //     string objectLink = "";
            //     // if (didHit) {
            //     //     objectLink = hit.transform.GetComponentInParent<SimObjPhysics>()?.objectID;
            //     // }
            //     ShadowParameters sp = null;
            //     if (l.shadows != LightShadows.None) {
            //         sp = new ShadowParameters() {
            //             strength = l.shadowStrength
            //         };
            //     }
            //     return new LightParameters()  {
            //         id = l.gameObject.name,
            //         room_id = room,
            //         type = Enum.GetName(typeof(LightType), l.type),

                    

            //         position = l.transform.position,
            //         rotation = AxisAngleRotation.fromQuaternion(l.transform.rotation),
            //         intensity = l.intensity,
            //         range = l.range,
            //         rgb = l.color,
            //         shadow = sp,
            //         object_id = objectLink

            // };}).ToList();


            loadedHouse.procedural_parameters.skybox_id = RenderSettings.skybox.name;

            Debug.Log("Lights " + this.loadedHouse.procedural_parameters.lights.Count);

            var jsonResolver = new ShouldSerializeContractResolver();
                    var outJson = JObject.FromObject(this.loadedHouse,
                                new Newtonsoft.Json.JsonSerializer() {
                                    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                                    ContractResolver = jsonResolver
                                });
            
            Debug.Log($"output json: {outJson.ToString()}");
            System.IO.File.WriteAllText(outPath, outJson.ToString());
        }
        else {
            Debug.LogError("No loaded layout load a layout first");
        }

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
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
