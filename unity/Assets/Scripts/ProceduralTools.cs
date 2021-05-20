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
using Thor.Procedural.Data;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
using UnityEditor;
#endif

namespace Thor.Procedural {
    [ExecuteInEditMode]
    public class AssetMap<T> {
        private Dictionary<string, T> assetMap;
        public AssetMap(Dictionary<string, T> assetMap) {
            this.assetMap = assetMap;
        }

        public T getAsset(string name) {
            return assetMap[name];
        }

        public bool ContainsKey(string key) {
            return assetMap.ContainsKey(key);
        }

        public int Count() {
            return assetMap.Count;
        }
    }

    // TODO: Turn caller of procedural tools into an instance that has certain
    // creation attributes like the material database
    [ExecuteInEditMode]
    public class RoomCreatorFactory {
        public RoomCreatorFactory(AssetMap<Material> materials, AssetMap<GameObject> prefabs) {

        }
        public static GameObject CreateProceduralRoomFromArray() {

            return null;
        }
    }

    public static class ProceduralTools {

        public static UnityEngine.Mesh GetRectangleFloorMesh(IEnumerable<RectangleRoom> rooms, float yOffset = 0.0f, bool generateBackFaces = false) {
            var mesh = new Mesh();

            var oppositeCorners = rooms.SelectMany(r => new Vector3[] {
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

            var vertices = new Vector3[] {
                minPoint,
                minPoint + new Vector3(0, 0, scale.z),
                maxPoint,
                minPoint + new Vector3(scale.x, 0, 0)
            };

            var uvs = new Vector2[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
            var triangles = new List<int>() { 3, 2, 0, 2, 1, 0, 0, 1, 2, 0, 2, 3 };
            var normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };

            // TODO: this is not working for some reason, although it works with the walls
            if (generateBackFaces) {
                // vertices = vertices.Concat(vertices).ToArray();
                // uvs = uvs.Concat(uvs).ToArray();
                //triangles = triangles.Concat(triangles.AsEnumerable().Reverse()).ToArray();
                triangles.AddRange(triangles.AsEnumerable().Reverse());
                //normals = normals.Concat(normals).ToArray();

            }
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles.ToArray();
            mesh.normals = normals;

            return mesh;
        }

        public static IEnumerable<UnityEngine.Mesh> GetMultipleRectangleFloorMeshes(IEnumerable<RectangleRoom> rooms, float yOffset = 0.0f, bool generateBackFaces = false) {
            return rooms.Select(
                r =>
                    GetRectangleFloorMesh(new List<RectangleRoom>() { r }, yOffset, generateBackFaces)
                );
        }

        // TODO call above for ceiling using yOffset

        public static UnityEngine.Mesh GetRectangleFloorMesh(RectangleRoom room) {
            return GetRectangleFloorMesh(new RectangleRoom[] { room });
        }

        public static GameObject CreateVisibilityPointsOnPlane(
            Vector3 start,
            Vector3 right,
            Vector3 top,
            float pointInterval = 1 / 3.0f,
            string postfixName = "VisibilityPoints"
        ) {
            var visibilityPoints = new GameObject($"VisibilityPoints{postfixName}");
            var width = right.magnitude;
            var height = top.magnitude;

            var step = pointInterval;
            var count = 0;

            var end = start + right + top;
            var stepVecRight = step * right.normalized;
            var stepVecTop = step * top.normalized;
            var delta = Vector3.zero;

            for (Vector3 rightDelta = Vector3.zero; (width * width) - rightDelta.sqrMagnitude > (step * step); rightDelta += stepVecRight) {
                for (Vector3 topDelta = Vector3.zero; (height * height) - topDelta.sqrMagnitude > (step * step); topDelta += stepVecTop) {
                    var vp = new GameObject($"VisibilityPoint{postfixName} ({count})");
                    vp.transform.position = start + rightDelta + topDelta;
                    vp.transform.parent = visibilityPoints.transform;
                    count++;
                }
            }
            return visibilityPoints;
        }

        public static GameObject CreateVisibilityPointsGameObject(RectangleRoom room, float pointInterval = 1 / 3.0f) {
            var visibilityPoints = new GameObject("VisibilityPoints");

            var step = pointInterval;
            var count = 0;
            var offset = new Vector3(room.width / 2.0f, 0, room.depth / 2.0f);
            for (float x = room.center.x - offset.x + room.marginWidth; x < room.center.x + offset.x - room.marginWidth + step; x += step) {
                for (float z = room.center.z - offset.z + room.marginDepth; z < room.center.z + offset.z - room.marginDepth + step; z += step) {
                    var vp = new GameObject($"VisibilityPoint ({count})");
                    vp.transform.position = new Vector3(x, room.center.y, z);
                    vp.transform.parent = visibilityPoints.transform;
                    count++;
                }
            }
            return visibilityPoints;
        }

        public static GameObject createWalls(Room room, AssetMap<Material> materialDb, string gameObjectId = "Structure") {
            var structure = new GameObject(gameObjectId);

            var zip3 = room.walls.Zip(
                room.walls.Skip(1).Concat(new Wall[] { room.walls.FirstOrDefault() }),
                (w0, w1) => (w0, w1)
            ).Zip(
                new Wall[] { room.walls.LastOrDefault() }.Concat(room.walls.Take(room.walls.Length - 1)),
                (wallPair, w2) => (wallPair.w0, w2, wallPair.w1)
            ).ToArray();

            var index = 0;
            foreach ((Wall w0, Wall w1, Wall w2) in zip3) {
                if (!w0.empty) {
                    var wallGO = createAndJoinWall(index, materialDb, w0, w1, w2);

                    wallGO.transform.parent = structure.transform;
                    index++;
                }
            }
            return structure;
        }

        private static Vector3? vectorsIntersectionXZ(Vector3 line1P0, Vector3 line1Dir, Vector3 line2P0, Vector3 line2Dir) {
            var denominator = (line2Dir.z + line1Dir.z * line2Dir.x);

            // denominator = (line1P0.x - line1Dir.x) * (line2.p1.x)
            // Lines do not intersect
            if (Mathf.Abs(denominator) < 1E-4) {
                Debug.Log(" !!! den 0");
                return null;
            }
            var u = (line1P0.z + line1Dir.z * (line2P0.x - line1P0.x)) / denominator;
            return line2P0 + u * line2Dir;
        }

        private static Vector3? wallsIntersectionXZ(Wall wall1, Wall wall2) {
            var denominator = -(wall1.p1.x - wall2.p1.x) * (wall2.p1.z - wall2.p0.z) + wall1.p1.z - wall1.p0.z;

            // denominator = (line1P0.x - line1Dir.x) * (line2.p1.x)
            // Lines do not intersect
            if (Mathf.Abs(denominator) < 1E-4) {
                Debug.Log(" !!! den 0");
                return null;
            }
            var t = ((wall1.p0.x - wall2.p0.x) * (wall2.p1.z - wall2.p0.z) + wall2.p1.z - wall1.p0.z) / denominator;
            return wall1.p0 + t * (wall1.p1 - wall1.p0);
        }

        private static Vector3? vectorIntersect(Wall wall1, Wall wall2) {
            var normal1 = Vector3.Cross((wall1.p1 - wall1.p0).normalized, Vector3.up);
            var normal2 = Vector3.Cross((wall2.p1 - wall2.p0).normalized, Vector3.up);

            var wall1p0 = wall1.p0 + normal1 * wall1.thickness;
            var wall1p1 = wall1.p1 + normal1 * wall1.thickness;

            var wall2p0 = wall2.p0 + normal2 * wall2.thickness;
            var wall2p1 = wall2.p1 + normal2 * wall2.thickness;

            Vector3 a = wall1p1 - wall1p0;
            Vector3 b = wall2p0 - wall2p1;

            var c = wall2p1 - wall1p0;

            var denominator = Vector3.Dot(a, b);

            // denominator = (line1P0.x - line1Dir.x) * (line2.p1.x)
            // Lines do not intersect
            if (Mathf.Abs(denominator) < 1E-4) {
                Debug.Log(" !!! den 0");
                // var pseduoCrossSign = Mathf.Sign(normal1.x * normal2.z - normal1.z * normal2.x);
                return wall1p0 + wall1.thickness * (wall1p1 - wall1p0).normalized;
            }
            var t = Vector3.Dot(c, b) / denominator;
            return wall1p0 + t * (wall1p1 - wall1p0);
        }

        private static float vectorIntersectThickness(Wall wall1, Wall wall2, bool perpendicularUseFirstThickness = false) {
            // TODO: Break up into line intersection utility and caller 
            var wall1p0p1 = (wall1.p1 - wall1.p0).normalized;
            var wall2p0p1 = (wall2.p1 - wall2.p0).normalized;
            //return wall1.thickness;
            var normal1 = Vector3.Cross(wall1p0p1, Vector3.up);
            var normal2 = Vector3.Cross(wall2p0p1, Vector3.up);

            var wall1p0 = wall1.p0 + normal1 * wall1.thickness;
            var wall1p1 = wall1.p1 + normal1 * wall1.thickness;

            var wall2p0 = wall2.p0 + normal2 * wall2.thickness;
            var wall2p1 = wall2.p1 + normal2 * wall2.thickness;

            Vector3 a = wall1p1 - wall1p0;
            Vector3 b = wall2p0 - wall2p1;

            var c = wall2p1 - wall1p0;

            var denominator = Vector3.Dot(a, b);

            // denominator = (line1P0.x - line1Dir.x) * (line2.p1.x)
            // Lines perpendicular
            if (Mathf.Abs(denominator) < 1E-4) {
                var pseduoCrossSign = Mathf.Sign(normal1.x * normal2.z - normal1.z * normal2.x);
                var thickness = perpendicularUseFirstThickness ? wall1.thickness : wall2.thickness;
                return wall2.thickness * (-pseduoCrossSign);
            }
            var t = Vector3.Dot(c, b) / denominator;
            return Vector3.Magnitude((wall1p0 + t * (wall1p1 - wall1p0)) - wall1p1);
        }


        private static Vector3? wallVectorIntersection(Wall wall1, Wall wall2) {
            var wall2p0p1 = wall2.p1 - wall2.p0;
            var wall1p0p1norm = (wall1.p1 - wall1.p0);

            // var normal2 = Vector3.Cross(wall1p0p1norm, wall2p0p1);
            var normal2 = Vector3.Cross(wall2p0p1, Vector3.up);
            var denominator = Vector3.Dot(wall1.p1 - wall1.p0, normal2);
            if (Mathf.Abs(denominator) < 1E-4) {


                var p0p1 = (wall1.p1 - wall1.p0).normalized;

                var normal = Vector3.Cross(p0p1, Vector3.up);
                var nextp0p1 = (wall2.p1 - wall2.p0).normalized;


                var nextNormal = Vector3.Cross(nextp0p1, Vector3.up);

                var sign = Mathf.Sign(nextNormal.x * normal.z - nextNormal.z * normal.x);

                // var sinAngle = nextNormal.x * normal.z - nextNormal.z * normal.x;

                return wall1.p1 + sign * wall2.thickness * nextp0p1;
            }
            var t = Vector3.Dot(wall2.p0 - wall1.p0, normal2) / denominator;

            return wall1.p0 + (wall1.p1 - wall1.p0) * t;
        }

        public static GameObject createAndJoinWall(
            int index,
            AssetMap<Material> materialDb,
            Wall toCreate,
            Wall previous = null,
            Wall next = null,
            float visibilityPointInterval = 1 / 3.0f,
            float minimumBoxColliderThickness = 0.1f,
            bool globalVertexPositions = false
        ) {
            var wallGO = new GameObject($"Wall_{index}");

            var meshF = wallGO.AddComponent<MeshFilter>();
            var boxC = wallGO.AddComponent<BoxCollider>();
            // boxC.convex = true;
            var generateBackFaces = true;
            const float zeroThicknessEpsilon = 1e-4f;
            var colliderThickness = toCreate.thickness < zeroThicknessEpsilon ? minimumBoxColliderThickness : toCreate.thickness;


            var p0p1 = toCreate.p1 - toCreate.p0;

            // var mid = p0p1 * 0.5f;
            // boxC.center = new Vector3(mid.x, )

            var mesh = meshF.mesh;

            var p0p1_norm = p0p1.normalized;

            var normal = Vector3.Cross(p0p1_norm, Vector3.up);

            var center = toCreate.p0 + p0p1 * 0.5f + Vector3.up * toCreate.height * 0.5f + normal * toCreate.thickness * 0.5f;
            var width = p0p1.magnitude;

            // List<Vector3> vertices;
            Vector3 p0;
            Vector3 p1;
            var theta = -Mathf.Sign(p0p1_norm.z) * Mathf.Acos(Vector3.Dot(p0p1_norm, Vector3.right));

            if (globalVertexPositions) {

                p0 = toCreate.p0;
                p1 = toCreate.p1;

                boxC.center = center;
            } else {
                p0 = -(width / 2.0f) * Vector3.right - new Vector3(0.0f, toCreate.height / 2.0f, toCreate.thickness / 2.0f);
                p1 = (width / 2.0f) * Vector3.right - new Vector3(0.0f, toCreate.height / 2.0f, toCreate.thickness / 2.0f);

                normal = Vector3.forward;
                p0p1_norm = Vector3.right;
                wallGO.transform.position = center;

                wallGO.transform.rotation = Quaternion.AngleAxis(theta * 180.0f / Mathf.PI, Vector3.up);
            }

            var colliderOffset = toCreate.thickness < zeroThicknessEpsilon ? normal * colliderThickness : Vector3.zero;

            boxC.center += colliderOffset;

            boxC.size = new Vector3(width, toCreate.height, colliderThickness);

            var vertices = new List<Vector3>() {
                        p0,
                        p0 + new Vector3(0.0f, toCreate.height, 0.0f),
                        p1 +  new Vector3(0.0f, toCreate.height, 0.0f),
                        p1
                    };

            var triangles = new List<int>() { 1, 2, 0, 2, 3, 0 };
            if (generateBackFaces) {
                triangles.AddRange(triangles.AsEnumerable().Reverse().ToList());
            }
            var uv = new List<Vector2>() {
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)
            };
            var normals = new List<Vector3>() { -normal, -normal, -normal, -normal };

            // if it is a double wall
            if (toCreate.thickness > zeroThicknessEpsilon) {


                // var nextp0p1 =  (next.p1 - next.p0).normalized;

                // var nextNormal = Vector3.Cross(nextp0p1, Vector3.up);

                // Debug.Log("tan " + 
                //     Mathf.Tan(Mathf.Asin( Vector3.Cross(normal, nextNormal).magnitude ) / 2.0f) +
                //      " norm " + normal + " nextNorm " + nextNormal + " 2d cross " + (nextNormal.x * normal.z - nextNormal.z * normal.x) + " revers 2d cross " + (normal.x * nextNormal.z - normal.z * nextNormal.x));

                // var thicknessAngleindependent = toCreate.thickness * Mathf.Tan(Mathf.Asin(nextNormal.x * normal.z - nextNormal.z * normal.x) / 2.0f);

                // var p0Thickness = vectorIntersectThickness2(previous, toCreate);
                // var p1Thickness = vectorIntersectThickness2(toCreate, next, false);

                var p0Thickness = p0 + normal * toCreate.thickness - p0p1_norm * vectorIntersectThickness(previous, toCreate, true);
                var p1Thickness = p1 + normal * toCreate.thickness + p0p1_norm * vectorIntersectThickness(toCreate, next);

                // var p0Thickness0 = wall.p0 + normal * thicknessAngleindependent - p0p1 * thicknessAngleindependent;
                // var p1Thickness1 = wall.p1 + normal * thicknessAngleindependent + p0p1 * thicknessAngleindependent;
                vertices.AddRange(
                    new Vector3[] {
                        p0Thickness, p0Thickness + new Vector3(0.0f, toCreate.height, 0.0f), p1Thickness +  new Vector3(0.0f, toCreate.height, 0.0f), p1Thickness
                    }
                );

                uv.AddRange(new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) });
                // mesh.uv = mesh.uv.Concat(new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) }).ToArray();
                normals.AddRange(new Vector3[] { normal, normal, normal, normal });
                var backWallTriangles = new int[] { 4, 6, 5, 4, 7, 6 };
                triangles.AddRange(backWallTriangles);
                if (generateBackFaces) {
                    triangles.AddRange(backWallTriangles.AsEnumerable().Reverse().ToList());
                }
            }

            mesh.vertices = vertices.ToArray();
            mesh.uv = uv.ToArray();
            mesh.normals = normals.ToArray();
            mesh.triangles = triangles.ToArray();
            var meshRenderer = wallGO.AddComponent<MeshRenderer>();
            // TODO use a material loader that has this dictionary
            //var mats = ProceduralTools.FindAssetsByType<Material>().ToDictionary(m => m.name, m => m);
            // var mats = ProceduralTools.FindAssetsByType<Material>().GroupBy(m => m.name).ToDictionary(m => m.Key, m => m.First());


            var visibilityPointsGO = CreateVisibilityPointsOnPlane(
                toCreate.p0,
                toCreate.p1 - toCreate.p0,
                (Vector3.up * toCreate.height),
                visibilityPointInterval
            );

            setWallSimObjPhysics(wallGO, $"wall_{index}", visibilityPointsGO, boxC);

            visibilityPointsGO.transform.parent = wallGO.transform;
            //if (mats.ContainsKey(wall.materialId)) {
            meshRenderer.material = materialDb.getAsset(toCreate.materialId);
            //}

            return wallGO;
        }

        public static GameObject createWall(Wall wall, Wall next = null) {

            var wallGO = new GameObject("Wall");

            var meshF = wallGO.AddComponent<MeshFilter>();
            var meshC = wallGO.AddComponent<MeshCollider>();

            var mesh = meshF.mesh;

            var p0p1 = (wall.p1 - wall.p0).normalized;

            var normal = Vector3.Cross(p0p1, Vector3.up);


            var vertices = new List<Vector3>() {
                    wall.p0, wall.p0 + new Vector3(0.0f, wall.height, 0.0f), wall.p1 +  new Vector3(0.0f, wall.height, 0.0f), wall.p1
                };

            var triangles = new List<int>() { 1, 2, 0, 2, 3, 0 };
            var uv = new List<Vector2>() {
                    new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1)
                };
            var normals = new List<Vector3>() { -normal, -normal, -normal, -normal };

            // if it is a double wall
            if (wall.thickness > 0.0001f) {


                var nextp0p1 = (next.p1 - next.p0).normalized;

                var nextNormal = Vector3.Cross(nextp0p1, Vector3.up);

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
                normals.AddRange(new Vector3[] { normal, normal, normal, normal });
                triangles.AddRange(new int[] { 4, 6, 5, 4, 7, 6 });
            }

            mesh.vertices = vertices.ToArray();
            mesh.uv = uv.ToArray();
            mesh.normals = normals.ToArray();
            mesh.triangles = triangles.ToArray();
            var meshRenderer = wallGO.AddComponent<MeshRenderer>();
            // TODO use a material loader that has this dictionary
            //var mats = ProceduralTools.FindAssetsByType<Material>().ToDictionary(m => m.name, m => m);

            //var mats = ProceduralTools.FindAssetsByType<Material>().GroupBy(m => m.name).ToDictionary(m => m.Key, m => m.First());

            //if (mats.ContainsKey(wall.materialId)) {
            //Debug.Log("MAT query " +  wall.materialId+ " " + string.Join(",", mats.Select(m => m.Value.name).ToArray()) + " len " + mats.Count);

            // TODO: Set material for asset database
            // meshRenderer.material = mats[wall.materialId];
            //}

            return wallGO;
        }

        public static GameObject createFloorCollider(GameObject floorGameObject, RectangleRoom room, float thickness) {
            var colliders = new GameObject("Colliders");

            var collider = new GameObject("Col");
            var box = collider.AddComponent<BoxCollider>();

            var size = new Vector3(room.width + room.marginWidth * 2.0f, thickness, room.depth + room.marginDepth * 2.0f);

            var center = room.center - new Vector3(0, thickness / 2.0f, 0);
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
        GameObject floorGameObject, RectangleRoom room, float height, string namePostfix = ""
    ) {
            var receptacleTriggerBox = new GameObject($"ReceptacleTriggerBox{namePostfix}");
            var receptacleCollider = receptacleTriggerBox.AddComponent<BoxCollider>();
            receptacleCollider.isTrigger = true;
            var widthMinusMargin = room.width - 2.0f * room.marginWidth;

            var depthMinusMargin = room.depth - 2.0f * room.marginDepth;

            receptacleCollider.size = new Vector3(widthMinusMargin, height, depthMinusMargin);
            receptacleCollider.center = room.center + new Vector3(0, height / 2.0f, 0);

            receptacleTriggerBox.transform.parent = floorGameObject.transform;
            return receptacleTriggerBox;
        }

        public static SimObjPhysics setWallSimObjPhysics(
            GameObject wallGameObject,
            string simObjId,
            GameObject visibilityPoints,
            Collider collider
        ) {
            var boundingBox = new GameObject("BoundingBox");
            var bbCollider = boundingBox.AddComponent<BoxCollider>();
            bbCollider.enabled = false;
            boundingBox.transform.parent = wallGameObject.transform;

            wallGameObject.tag = "SimObjPhysics";

            var simObjPhysics = wallGameObject.AddComponent<SimObjPhysics>();
            simObjPhysics.objectID = simObjId;
            simObjPhysics.ObjType = SimObjType.Wall;
            simObjPhysics.PrimaryProperty = SimObjPrimaryProperty.Wall;
            simObjPhysics.SecondaryProperties = new SimObjSecondaryProperty[] { };

            simObjPhysics.BoundingBox = boundingBox;

            simObjPhysics.VisibilityPoints = visibilityPoints.GetComponentsInChildren<Transform>();

            // simObjPhysics.ReceptacleTriggerBoxes = new GameObject[] { receptacleTriggerBox };
            simObjPhysics.MyColliders = new Collider[] { collider };

            simObjPhysics.transform.parent = wallGameObject.transform;

            // receptacleTriggerBox.AddComponent<Contains>();
            return simObjPhysics;
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
        public static GameObject createSimObjPhysicsGameObject(string name = "Floor", Vector3? position = null, string tag = "SimObjPhysics", int layer = 8) {

            var floorGameObject = new GameObject(name);
            floorGameObject.transform.position = position.GetValueOrDefault();

            floorGameObject.tag = tag;
            floorGameObject.layer = layer;

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
            var floorGameObject = createSimObjPhysicsGameObject(name, position);

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

            ProceduralTools.createWalls(room, materialDb, "Structure");
            return floorGameObject;
        }

        private static void setUpFloorMesh(GameObject floorGameObject, Mesh mesh, Material material) {
            floorGameObject.GetComponent<MeshFilter>().mesh = mesh;
            var meshRenderer = floorGameObject.GetComponent<MeshRenderer>();
            meshRenderer.material = material;
        }

        public static GameObject createMultiRoomFloorGameObject(
            string name,
            IEnumerable<RectangleRoom> rooms,
            AssetMap<Material> materialDb,
            string simObjId,
            float receptacleHeight = 0.7f,
            float floorColliderThickness = 1.0f,
            string ceilingMaterialId = "",
            bool individualRoomFloorMesh = false,
            Vector3? position = null
        ) {
            var walls = rooms.SelectMany(r => r.walls);
            var wallPoints = walls.SelectMany(w => new List<Vector3>() { w.p0, w.p1 });
            var wallsMinY = wallPoints.Min(p => p.y);
            var wallsMaxY = wallPoints.Max(p => p.y);
            var wallsMaxHeight = walls.Max(w => w.height);


            var floorGameObject = createSimObjPhysicsGameObject(name, position == null ? new Vector3(0, wallsMinY, 0) : position);

            var mesh = ProceduralTools.GetRectangleFloorMesh(rooms);
            if (individualRoomFloorMesh) {
                // TODO: solution for this, both the multi-object and multi-material approach lead to Z-fighting 
                // mesh.subMeshCount = rooms.Count();
                for (int i = 0; i < rooms.Count(); i++) {
                    var room = rooms.ElementAt(i);
                    var subFloorGO = createSimObjPhysicsGameObject($"Floor_{i}");
                    var meshes = ProceduralTools.GetMultipleRectangleFloorMeshes(rooms);
                    // mesh.subMeshCount
                    subFloorGO.GetComponent<MeshFilter>().mesh = meshes.ElementAt(i);
                    var meshRenderer = subFloorGO.GetComponent<MeshRenderer>();
                    meshRenderer.material = materialDb.getAsset(room.floor.materialId);
                    subFloorGO.transform.parent = floorGameObject.transform;
                }
            } else {
                floorGameObject.GetComponent<MeshFilter>().mesh = mesh;
                var meshRenderer = floorGameObject.GetComponent<MeshRenderer>();
                meshRenderer.material = materialDb.getAsset(rooms.ElementAt(0).floor.materialId);
            }


            var minPoint = mesh.vertices[0];
            var maxPoint = mesh.vertices[2];

            var dimension = maxPoint - minPoint;

            var floor = new RectangleFloor() {
                center = minPoint + dimension / 2.0f,
                width = dimension.x,
                depth = dimension.z,
                marginWidth = dimension.x * 0.05f,
                marginDepth = dimension.z * 0.05f,
                materialId = rooms.ElementAt(0).floor.materialId
            };
            var roomCluster = new RectangleRoom() {
                rectangleFloor = floor
            };



            var visibilityPoints = ProceduralTools.CreateVisibilityPointsGameObject(roomCluster);
            visibilityPoints.transform.parent = floorGameObject.transform;

            // rooms.Select((room, index) => ProceduralTools.createFloorReceptacle(floorGameObject, room, receptacleHeight, $"{index}"));

            var receptacleTriggerBox = ProceduralTools.createFloorReceptacle(floorGameObject, roomCluster, receptacleHeight);
            var collider = ProceduralTools.createFloorCollider(floorGameObject, roomCluster, floorColliderThickness);

            // generate ceiling
            if (ceilingMaterialId != "") {
                var ceilingGameObject = createSimObjPhysicsGameObject("Ceiling", new Vector3(0, wallsMaxY + wallsMaxHeight, 0), "Ceiling", 0);
                var ceilingMesh = ProceduralTools.GetRectangleFloorMesh(rooms, 0.0f, true);
                ceilingGameObject.GetComponent<MeshFilter>().mesh = mesh;
                ceilingGameObject.GetComponent<MeshRenderer>().material = materialDb.getAsset(ceilingMaterialId);
            }


            ProceduralTools.setRoomSimObjectPhysics(floorGameObject, simObjId, visibilityPoints, receptacleTriggerBox, collider.GetComponentInChildren<Collider>());

            receptacleTriggerBox.AddComponent<Contains>();

            // ProceduralTools.createWalls(room, "Structure");
            Debug.Log($"Structure creation count: {rooms.Count()}");
            var index = 0;
            //Debug.Log($" Room {rooms} roomlen {}");
            foreach (RectangleRoom room in rooms) {
                var wallGO = ProceduralTools.createWalls(room, materialDb, $"Structure_{index}");
                floorGameObject.transform.parent = wallGO.transform;
                index++;
            }
            return floorGameObject;
        }

#if UNITY_EDITOR
        public static List<T> FindAssetsByType<T>() where T : UnityEngine.Object {
            List<T> assets = new List<T>();
            string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof(T).ToString().Replace("UnityEngine.", "")));
            for (int i = 0; i < guids.Length; i++) {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset != null) {
                    assets.Add(asset);
                }
            }
            return assets;
        }

        public static List<GameObject> FindPrefabsInAssets() {
            var assets = new List<GameObject>();
            string[] guids = AssetDatabase.FindAssets("t:prefab");
            for (int i = 0; i < guids.Length; i++) {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (asset != null) {
                    assets.Add(asset);
                }
            }
            return assets;
        }
#endif
        public static GameObject spawnObjectAtReceptacle(AssetMap<GameObject> goDb, string objectId, SimObjPhysics receptacleSimObj, Vector3 position) {
            var spawnCoordinates = receptacleSimObj.FindMySpawnPointsFromTopOfTriggerBox();
            var go = goDb.getAsset(objectId);
            var pos = spawnCoordinates.Shuffle_().First();
            //GameObject.Instantiate(go, pos, Quaternion.identity);
            var fpsAgent = GameObject.FindObjectOfType<PhysicsRemoteFPSAgentController>();

            var sceneManager = GameObject.FindObjectOfType<PhysicsSceneManager>();
            var initialSpawnPosition = position;


            var spawned = GameObject.Instantiate(go, initialSpawnPosition, Quaternion.identity);
            var toSpawn = spawned.GetComponent<SimObjPhysics>();
            Rigidbody rb = spawned.GetComponent<Rigidbody>();


            var success = false;

            for (int i = 0; i < spawnCoordinates.Count; i++) {
                //place object at the given point, this also checks the spawn area to see if its clear
                //if not clear, it will return false
                if (fpsAgent.placeNewObjectAtPoint(toSpawn, spawnCoordinates[i])) {
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
                    foreach (Vector3 p in corners) {
                        if (!con.CheckIfPointIsAboveReceptacleTriggerBox(p)) {
                            cornerCheck = false;
                            //this position would cause object to fall off table
                            //double back and reset object to try again with another point
                            spawned.transform.position = initialSpawnPosition;
                            break;
                        }
                    }

                    if (!cornerCheck) {
                        success = false;
                        continue;
                    }
                }

                //if all corners were succesful, break out of this loop, don't keep trying
                if (success) {
                    rb.isKinematic = false;
                    //run scene setup to grab reference to object and give it objectId
                    sceneManager.SetupScene();
                    sceneManager.ResetObjectIdToSimObjPhysics();
                    break;
                }
            }

            return null;
        }

        private static List<Vector3> GetCorners(SimObjPhysics sop) {
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
            corners.Add(bb.transform.TransformPoint(bbCenter + new Vector3(bbcol.size.x, bbcol.size.y, -bbcol.size.z) * 0.5f));

            return corners;
        }

        private static bool isWall(int[][] roomArray, (int row, int column) index, int emptyValue = 1) {
            var rowNum = roomArray.Length;
            int colNum;
            if (index.row > rowNum || index.column > roomArray[index.row].Length) {
                return false;
            }
            var value = roomArray[index.row][index.column];
            if (emptyValue == value) {
                return false;
            }

            colNum = roomArray[index.row].Length;
            var neighbors = new (int row, int col)[]{
                    (index.row - 1, index.column),
                    (index.row, index.column - 1),
                    (index.row + 1, index.column),
                    (index.row, index.column + 1),

                    (index.row - 1, index.column - 1),
                    (index.row - 1, index.column + 1),
                    (index.row + 1, index.column - 1),
                    (index.row + 1, index.column + 1),
                };

            var isBoundary = neighbors.Select(
                tuple => withinArrayBoundary(tuple, rowNum, colNum) && roomArray[tuple.row][tuple.col] == value
            ).Any(sameAsSelf => !sameAsSelf);
            return isBoundary;
        }

        private static bool withinArrayBoundary((int row, int col) current, int rows, int columns) {
            return current.row >= 0 && current.row < rows && current.col >= 0 && current.col < columns;
        }

        // TODO: fix bugs for sharp one space corners
        private static IEnumerable<(int row, int col)> traverseBoundary(int[][] roomArray, (int row, int col) start, int target, int emptyValue) {
            var queue = new Queue<(int row, int col)>();
            var result = new List<(int row, int col)>();
            queue.Enqueue(start);
            var maxIter = 200;
            // Type d = (int row, int col);

            var taboo = new List<(int row, int col)>() { (-1, 0), (0, -1) };
            var neighborExplore = new (int row, int col)[] {
                        (0, 1),
                        (1, 0),
                        (0, -1),
                        (-1, 0)
                    };
            var iter = 0;
            while (queue.Count != 0 && iter < maxIter) {
                iter++;
                var dequeued = queue.Dequeue();
                // var begin = current;
                var colLenght = roomArray[dequeued.row].Length;
                var neighbors = neighborExplore.Where(n => !taboo.Contains(n));
                foreach (var neighborDelta in neighbors) {
                    (int row, int col)? previous = null; //= (row: current.row + neighborDelta.row, col: current.col + neighborDelta.col);
                                                         // var prevNeighbor = currentNeighbor;
                    var current = dequeued;
                    while (
                        withinArrayBoundary(current, roomArray.Length, colLenght) &&
                        target == roomArray[current.row][current.col] &&
                        isWall(roomArray, current, emptyValue)
                    ) {

                        result.Add((current.row, current.col));
                        previous = current;
                        current = (row: current.row + neighborDelta.row, col: current.col + neighborDelta.col);
                    }

                    if (previous != null && previous != start) {
                        if (previous != dequeued) {
                            Debug.Log($"Explored: {dequeued}, out: {string.Join(",", result.ToList())} prev: {previous} delta: {neighborDelta}, neighbors: {string.Join(",", neighbors)}");
                            queue.Enqueue(previous.GetValueOrDefault());
                            taboo = new List<(int row, int col)>() { neighborDelta, (-neighborDelta.row, -neighborDelta.col) };
                            break;
                        }
                        Debug.Log($"Explored No add: {dequeued}, out: {string.Join(",", result.ToList())} prev: {previous} delta: {neighborDelta}, neighbors: {string.Join(",", neighbors)}");
                        // continue;
                    }
                }
            }
            Debug.Log($" disctinct {string.Join(",", result.Distinct())}");
            return result.Distinct();
        }

        private static void traverseBoundaryRecursive(int[][] roomArray, (int row, int col) current, IEnumerable<(int row, int col)> taboo, (int row, int col) start, int target, List<(int, int)> output, int emptyValue) {

            if (start.col == current.col && start.row == current.row) {
                return;
            } else if (target != roomArray[current.row][current.col]) {
                return;
            } else {
                var colLenght = roomArray[current.row].Length;
                var neighbors = new (int row, int col)[] {
                    (0, 1),
                    (1, 0),
                    (0, -1),
                    (-1, 0)
                }.Where(n => !taboo.Contains(n));
                foreach (var neighborDelta in neighbors) {
                    var currentNeighbor = (row: current.row + neighborDelta.row, col: current.col + neighborDelta.col);
                    var prevNeighbor = currentNeighbor;
                    if (
                        withinArrayBoundary(currentNeighbor, roomArray.Length, colLenght) &&
                        target == roomArray[currentNeighbor.row][currentNeighbor.col] &&
                        isWall(roomArray, currentNeighbor, emptyValue)
                    ) {
                        while (
                        withinArrayBoundary(currentNeighbor, roomArray.Length, colLenght) &&
                        target == roomArray[currentNeighbor.row][currentNeighbor.col] &&
                        isWall(roomArray, currentNeighbor, emptyValue)
                        ) {
                            if (currentNeighbor != start) {
                                output.Add((currentNeighbor.row, currentNeighbor.col));
                            }
                            prevNeighbor = currentNeighbor;
                            currentNeighbor = (currentNeighbor.row + neighborDelta.row, currentNeighbor.col + neighborDelta.col);
                        }
                        traverseBoundaryRecursive(roomArray, prevNeighbor, new List<(int, int)>() { neighborDelta, (-neighborDelta.row, -neighborDelta.col) }, start, target, output, emptyValue);
                        break;
                    }
                }
            }
        }


        public static Dictionary<int, IEnumerable<(int row, int col)>> createRoomsFromGenerationArray(int[][] roomArray, int emptyValue = 1, Vector2? scale = null) {
            var distinct = roomArray.SelectMany(row => row.Distinct()).Distinct().Where(r => r != emptyValue);

            var valueToIndices = distinct.ToDictionary(
                val => val,
                val =>
                     roomArray.Select((row, rowIndex) => (row: rowIndex, col: Array.FindIndex(row, v => v == val)))
                    .First(
                        (index) => index.col != -1
                    )
                );
            var roomToSortedWalls = valueToIndices.ToDictionary(
                    c => c.Key,
                    c => traverseBoundary(roomArray, c.Value, c.Key, emptyValue)
            );
            return roomToSortedWalls;
        }

        // public static GameObject(Wall[] walls, string floorMaterialId, string ceilingMaterialId, float wallHeight) {

        // }

        public static GameObject roomFromWallIndexDictionary(
            string name,
            Dictionary<int, IEnumerable<(int row, int col)>> roomWallIndexMap,
            float rowNum,
            float colNum,
            float floorHeight,
            float wallHeight,
            float wallThickness,
            Dictionary<int, (string wallMaterial, string floorMaterial)> materialsPerRoom,
            AssetMap<Material> materialMap,
            Vector2? scale = null,
            float receptacleHeight = 0.7f,
            float floorColliderThickness = 1.0f
        ) {
            var scaleVec = scale.GetValueOrDefault(Vector2.one);

            var rooms = roomWallIndexMap.Select(entry => {
                (string wallMaterial, string floorMaterial) materialId;
                materialsPerRoom.TryGetValue(entry.Key, out materialId);
                var startOffsetX = entry.Value.Min(index => index.col);
                var startOffsetZ = entry.Value.Min(index => index.row);
                startOffsetX = 0;
                startOffsetZ = 0;

                var colNumPresent = colNum - startOffsetX;
                var rowNumPresent = rowNum - startOffsetZ;

                Debug.Log($"Room: {entry.Key} walls: {string.Join(",", entry.Value)}");

                return RectangleRoom.roomFromWallPoints(
                    entry.Value.Select(
                        // index => new Vector3(((index.col - startOffsetX) / colNum) * scaleVec.x - (scaleVec.x * ((colNum - startOffsetX) / colNum)) / 2.0f, floorHeight, -((index.row - startOffsetZ) / rowNum) * scaleVec.y + (scaleVec.y * ((rowNum - startOffsetZ) / rowNum)) / 2.0f)
                        index => new Vector3(((index.col - startOffsetX) / colNumPresent) * scaleVec.x - (scaleVec.x / 2.0f), floorHeight, -(((index.row - startOffsetZ) / rowNumPresent) * scaleVec.y - (scaleVec.y / 2.0f)))
                    ),
                        wallHeight,
                        wallThickness,
                        materialId.floorMaterial,
                        materialId.wallMaterial
                    );
            }
            );

            return ProceduralTools.createMultiRoomFloorGameObject(
                name,
                rooms,
                materialMap,
                $"room_{name}",
                receptacleHeight,
                floorColliderThickness
            );
        }

        public static AssetMap<Material> GetMaterials() {
            var assetDB = GameObject.FindObjectOfType<ProceduralAssetDatabase>();
            if (assetDB != null) {
                return new AssetMap<Material>(assetDB.materials.GroupBy(m => m.name).ToDictionary(m => m.Key, m => m.First()));
            }
            return null;
        }

        public static AssetMap<GameObject> GetPrefabs() {
            var assetDB = GameObject.FindObjectOfType<ProceduralAssetDatabase>();
            if (assetDB != null) {
                return new AssetMap<GameObject>(assetDB.prefabs.GroupBy(m => m.name).ToDictionary(m => m.Key, m => m.First()));
            }
            return null;
        }
    }

}