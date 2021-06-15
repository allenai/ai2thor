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
    [Serializable]
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

            return GetRectangleMesh(new BoundingBox() { min = minPoint, max = maxPoint });
        }

        public static UnityEngine.Mesh GetRectangleMesh(BoundingBox box, bool generateBackFaces = false) {
            var mesh = new Mesh();

            //TODO check they have same y?
            // var l = oppositeCorners.Select(c => c.y).Distinct();
            // var currentY = l.First();
            // foreach (var y in l) {
            // }
            var minY = box.min.y;

            var minPoint = box.min;
            var maxPoint = box.max;

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

        // public static IEnumerable<UnityEngine.Mesh> GetMultipleRectangleFloorMeshes(IEnumerable<RectangleRoom> rooms, float yOffset = 0.0f, bool generateBackFaces = false) {
        //     return rooms.Select(
        //         r =>
        //             GetRectangleFloorMesh(new List<RectangleRoom>() { r }, yOffset, generateBackFaces)
        //         );
        // }

        public static Mesh GenerateFloorMesh(IEnumerable<Vector3> floorPolygon, float yOffset = 0.0f) {

            // Get indices for creating triangles
            var m_points = floorPolygon.Select(p => new Vector2(p.x, p.z)).ToArray();

            var triangleIndices = TriangulateVertices();
            //Debug.Log("TriangleIndices has length of " + triangleIndices.Length);

            // Get array of vertices for floor
            var floorVertices = m_points.Select(p => new Vector3(p.x, yOffset, p.y)).ToArray();

            // Create the mesh
            var floor = new Mesh();
            floor.vertices = floorVertices;
            floor.triangles = triangleIndices;
            floor.RecalculateNormals();
            floor.RecalculateBounds();

            //Get UVs for mesh's vertices
            floor.uv = GenerateUVs();
            return floor;

            int[] TriangulateVertices() {
                List<int> indices = new List<int>();

                int n = m_points.Length;
                if (n < 3) {
                    return indices.ToArray();
                }

                int[] V = new int[n];
                if (Area() > 0) {
                    for (int v = 0; v < n; v++) {
                        V[v] = v;
                    }
                } else {
                    for (int v = 0; v < n; v++) {
                        V[v] = (n - 1) - v;
                    }
                }

                int nv = n;
                int count = 2 * nv;
                for (int v = nv - 1; nv > 2;) {
                    if ((count--) <= 0) {
                        return indices.ToArray();
                    }

                    int u = v;
                    if (nv <= u) {
                        u = 0;
                    }

                    v = u + 1;
                    if (nv <= v) {
                        v = 0;
                    }

                    int w = v + 1;
                    if (nv <= w) {
                        w = 0;
                    }

                    if (Snip(u, v, w, nv, V)) {
                        int a, b, c, s, t;
                        a = V[u];
                        b = V[v];
                        c = V[w];
                        indices.Add(a);
                        indices.Add(b);
                        indices.Add(c);
                        for (s = v, t = v + 1; t < nv; s++, t++) {
                            V[s] = V[t];
                        }

                        nv--;
                        count = 2 * nv;
                    }
                }

                indices.Reverse();
                return indices.ToArray();
            }

            float Area() {
                int n = m_points.Length;
                float A = 0.0f;
                for (int p = n - 1, q = 0; q < n; p = q++) {
                    Vector2 pval = m_points[p];
                    Vector2 qval = m_points[q];
                    A += pval.x * qval.y - qval.x * pval.y;
                }
                return (A * 0.5f);
            }

            bool Snip(int u, int v, int w, int n, int[] V) {
                int p;
                Vector2 A = m_points[V[u]];
                Vector2 B = m_points[V[v]];
                Vector2 C = m_points[V[w]];
                if (Mathf.Epsilon > (((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x)))) {
                    return false;
                }

                for (p = 0; p < n; p++) {
                    if ((p == u) || (p == v) || (p == w)) {
                        continue;
                    }

                    Vector2 P = m_points[V[p]];
                    if (InsideTriangle(A, B, C, P)) {
                        return false;
                    }
                }

                return true;
            }

            bool InsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P) {
                float ax, ay, bx, by, cx, cy, apx, apy, bpx, bpy, cpx, cpy;
                float cCROSSap, bCROSScp, aCROSSbp;

                ax = C.x - B.x; ay = C.y - B.y;
                bx = A.x - C.x; by = A.y - C.y;
                cx = B.x - A.x; cy = B.y - A.y;
                apx = P.x - A.x; apy = P.y - A.y;
                bpx = P.x - B.x; bpy = P.y - B.y;
                cpx = P.x - C.x; cpy = P.y - C.y;

                aCROSSbp = ax * bpy - ay * bpx;
                cCROSSap = cx * apy - cy * apx;
                bCROSScp = bx * cpy - by * cpx;

                return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
            }

            Vector2[] GenerateUVs() {
                Vector2[] uvArray = new Vector2[m_points.Length];
                float texelDensity = 5f;

                for (int i = 0; i < m_points.Length; i++) {
                    uvArray[i] = (m_points[i] - m_points[0]) / texelDensity;
                }

                return uvArray;
            }
        }

        // TODO triangulation code here
        public static UnityEngine.Mesh GetPolygonMesh(RoomHierarchy room, float yOffset = 0.0f, bool generateBackFaces = false) {
            return new Mesh();
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

        public static GameObject createWalls(IEnumerable<Wall> walls, AssetMap<Material> materialDb, string gameObjectId = "Structure") {
            var structure = new GameObject(gameObjectId);

            var zip3 = walls.Zip(
                walls.Skip(1).Concat(new Wall[] { walls.FirstOrDefault() }),
                (w0, w1) => (w0, w1)
            ).Zip(
                new Wall[] { walls.LastOrDefault() }.Concat(walls.Take(walls.Count() - 1)),
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

        public static GameObject createWalls(Room room, AssetMap<Material> materialDb, string gameObjectId = "Structure") {
            return createWalls(room.walls, materialDb, gameObjectId);
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
            bool globalVertexPositions = false,
            int layer = 8
        ) {
            var wallGO = new GameObject($"Wall_{index}");

            wallGO.layer = layer;

            var meshF = wallGO.AddComponent<MeshFilter>();
            var boxC = wallGO.AddComponent<BoxCollider>();
            // boxC.convex = true;
            var generateBackFaces = false;
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

            var colliderOffset = Vector3.zero;//toCreate.thickness < zeroThicknessEpsilon ? normal * colliderThickness : Vector3.zero;

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
            // SimObjVisible
            collider.layer = 8;
            collider.tag = "SimObjPhysics";
            var box = collider.AddComponent<BoxCollider>();

            var size = new Vector3(room.width + room.marginWidth * 2.0f, thickness, room.depth + room.marginDepth * 2.0f);

            var center = room.center - new Vector3(0, thickness / 2.0f, 0);
            box.size = size;
            box.center = center;
            collider.transform.parent = colliders.transform;

            colliders.transform.parent = floorGameObject.transform;

            var triggerColliders = new GameObject("TriggerColliders");
            var triggerCollider = new GameObject("Col");

            triggerCollider.layer = 8;
            triggerCollider.tag = "SimObjPhysics";
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
            // SimObjInvisible
            receptacleTriggerBox.layer = 9;
            receptacleTriggerBox.tag = "Receptacle";
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
            // SimObjInvisible
            boundingBox.layer = 9;
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

        private static BoundingBox getRoomRectangle(IEnumerable<Vector3> polygonPoints) {
            var minY = polygonPoints.Min(p => p.y);

            var minPoint = new Vector3(polygonPoints.Min(c => c.x), minY, polygonPoints.Min(c => c.z));
            var maxPoint = new Vector3(polygonPoints.Max(c => c.x), minY, polygonPoints.Max(c => c.z));
            // Debug.Log(" min " + minPoint + " max " + maxPoint);
            return new BoundingBox() { min = minPoint, max = maxPoint };
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

            // If you want to generate an individual floor for each room
            if (individualRoomFloorMesh) {
                // TODO: solution for this, both the multi-object and multi-material approach lead to Z-fighting 
                // mesh.subMeshCount = rooms.Count();

                // For each room, generate floor mesh
                for (int i = 0; i < rooms.Count(); i++) {

                    // Select single RectangleRoom
                    var room = rooms.ElementAt(i);

                    // Create GameObject
                    var subFloorGO = createSimObjPhysicsGameObject($"Floor_{i}");

                    // Create current floor's mesh and set up meshFilter and MeshRenderer material
                    var currentFloorMesh = ProceduralTools.GenerateFloorMesh(rooms.ElementAt(i).walls.Select(w => w.p0));
                    subFloorGO.GetComponent<MeshFilter>().mesh = currentFloorMesh;
                    var meshRenderer = subFloorGO.GetComponent<MeshRenderer>();
                    meshRenderer.material = materialDb.getAsset(room.floor.materialId);

                    // Parent GameObject to floor-master gameObject
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
                var ceilingGameObject = createSimObjPhysicsGameObject("Ceiling", new Vector3(0, wallsMaxY + wallsMaxHeight, 0), "Structure", 0);
                var ceilingMesh = ProceduralTools.GetRectangleFloorMesh(rooms, 0.0f, true);
                ceilingGameObject.GetComponent<MeshFilter>().mesh = mesh;
                ceilingGameObject.GetComponent<MeshRenderer>().material = materialDb.getAsset(ceilingMaterialId);
            }

            ProceduralTools.setRoomSimObjectPhysics(floorGameObject, simObjId, visibilityPoints, receptacleTriggerBox, collider.GetComponentInChildren<Collider>());

            var index = 0;
            foreach (RectangleRoom room in rooms) {
                var wallGO = ProceduralTools.createWalls(room, materialDb, $"Structure_{index}");
                floorGameObject.transform.parent = wallGO.transform;
                index++;
            }

            //generate objectId for newly created wall/floor objects
            //also add them to objectIdToSimObjPhysics dict so they can be found via
            //getTargetObject() and other things that use that dict
            //also add their rigidbodies to the list of all rigid body objects in scene
            var sceneManager = GameObject.FindObjectOfType<PhysicsSceneManager>();
            sceneManager.SetupScene();

            return floorGameObject;
        }

        private static Wall polygonWallToSimpleWall(PolygonWall wall) {
            //wall.polygon.
            var polygons = wall.polygon.OrderBy(p => p.y);
            var maxY = wall.polygon.Max(p => p.y);
            var p0 = polygons.ElementAt(0);
            return new Wall() {
                p0 = polygons.ElementAt(0),
                p1 = polygons.ElementAt(1),
                height = maxY - p0.y,
                materialId = wall.material,
                empty = wall.empty
            };
        }

        public static GameObject creatPolygonFloorHouse(
           string name,
           ProceduralHouse house,
           AssetMap<Material> materialDb,
           Vector3? position = null
       ) {
            string simObjId = house.id;
            float receptacleHeight = house.procedural_parameters.receptacle_height;
            float floorColliderThickness = house.procedural_parameters.floor_collider_thickness;
            string ceilingMaterialId = house.procedural_parameters.ceiling_material;

            var walls = house.walls.Select(w => polygonWallToSimpleWall(w));
            var wallPoints = walls.SelectMany(w => new List<Vector3>() { w.p0, w.p1 });
            var wallsMinY = wallPoints.Min(p => p.y);
            var wallsMaxY = wallPoints.Max(p => p.y);
            var wallsMaxHeight = walls.Max(w => w.height);

            var floorGameObject = createSimObjPhysicsGameObject(name, position == null ? new Vector3(0, wallsMinY, 0) : position);

            for (int i = 0; i < house.rooms.Count(); i++) {
                var room = house.rooms.ElementAt(i);
                var subFloorGO = createSimObjPhysicsGameObject($"Floor_{i}");
                var mesh = ProceduralTools.GenerateFloorMesh(room.floor_polygon);//ProceduralTools.GetPolygonMesh(room);
                // mesh.subMeshCount
                subFloorGO.GetComponent<MeshFilter>().mesh = mesh;
                var meshRenderer = subFloorGO.GetComponent<MeshRenderer>();

                meshRenderer.material = materialDb.getAsset(room.floor_material);

                Debug.Log("After room " + room.id);

                GameObject.Destroy(subFloorGO.GetComponent<Rigidbody>()); //these meshes dont need a rigidbody, only colliders

                //set up mesh collider to allow raycasts against only the floor inside the room
                subFloorGO.AddComponent<MeshCollider>();
                var meshCollider = subFloorGO.GetComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh;
                subFloorGO.layer = 12; //raycast to layer 12 so it doesn't interact with any other layer

                subFloorGO.transform.parent = floorGameObject.transform;
            }

            // var minPoint = mesh.vertices[0];
            // var maxPoint = mesh.vertices[2];

            var boundingBox = getRoomRectangle(house.rooms.SelectMany(r => r.floor_polygon));
            var dimension = boundingBox.max - boundingBox.min;

            var floor = new RectangleFloor() {
                center = boundingBox.min + dimension / 2.0f,
                width = dimension.x,
                depth = dimension.z,
                // marginWidth = dimension.x * 0.05f,
                // marginDepth = dimension.z * 0.05f
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
                var ceilingGameObject = createSimObjPhysicsGameObject("Ceiling", new Vector3(0, wallsMaxY + wallsMaxHeight, 0), "Structure", 0);
                var ceilingMesh = ProceduralTools.GetRectangleFloorMesh(new List<RectangleRoom> { roomCluster }, 0.0f, true);
                ceilingGameObject.GetComponent<MeshFilter>().mesh = ceilingMesh;
                ceilingGameObject.GetComponent<MeshRenderer>().material = materialDb.getAsset(ceilingMaterialId);
            }

            ProceduralTools.setRoomSimObjectPhysics(floorGameObject, simObjId, visibilityPoints, receptacleTriggerBox, collider.GetComponentInChildren<Collider>());

            var index = 0;

            var wallGO = ProceduralTools.createWalls(walls, materialDb, $"Structure_{index}");

            foreach (var obj in house.objects) {
                var k = ProceduralTools.spawnObject(
                    ProceduralTools.getAssetMap(), obj);
            }

            //generate objectId for newly created wall/floor objects
            //also add them to objectIdToSimObjPhysics dict so they can be found via
            //getTargetObject() and other things that use that dict
            //also add their rigidbodies to the list of all rigid body objects in scene
            var sceneManager = GameObject.FindObjectOfType<PhysicsSceneManager>();
            sceneManager.SetupScene();

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

        //not sure if this is needed, a helper function like this might exist somewhere already?
        public static AssetMap<GameObject> getAssetMap() {
            var assetDB = GameObject.FindObjectOfType<ProceduralAssetDatabase>();
            return new AssetMap<GameObject>(assetDB.prefabs.GroupBy(p => p.name).ToDictionary(p => p.Key, p => p.First()));
        }

        //generic function to spawn object in scene. No bounds or collision checks done
        public static GameObject spawnObject(
            AssetMap<GameObject> goDb,
            HouseObject ho) {

            var go = goDb.getAsset(ho.asset_id);
            var spawned = GameObject.Instantiate(go, ho.position, Quaternion.identity);
            Vector3 toRot = ho.rotation.axis * ho.rotation.degrees;
            spawned.transform.Rotate(toRot.x, toRot.y, toRot.z);

            var toSpawn = spawned.GetComponent<SimObjPhysics>();
            Rigidbody rb = spawned.GetComponent<Rigidbody>();
            rb.isKinematic = ho.kinematic;

            toSpawn.objectID = ho.id;
            toSpawn.name = ho.id;

            var sceneManager = GameObject.FindObjectOfType<PhysicsSceneManager>();
            sceneManager.AddToObjectsInScene(toSpawn);
            toSpawn.transform.SetParent(GameObject.Find("Objects").transform);

            return toSpawn.transform.gameObject;
        }

        public static GameObject spawnObjectInReceptacle(
            AssetMap<GameObject> goDb, 
            string prefabName,
            string objectId,
            SimObjPhysics receptacleSimObj, 
            Vector3 position, 
            AxisAngleRotation rotation = null) {
            var go = goDb.getAsset(prefabName);
            //var fpsAgent = GameObject.FindObjectOfType<PhysicsRemoteFPSAgentController>();
            //to potentially support multiagent down the line, reference fpsAgent via agentManager's array of active agents
            var agentManager = GameObject.Find("PhysicsSceneManager").GetComponentInChildren<AgentManager>();
            var fpsAgent = agentManager.agents[0].GetComponent<PhysicsRemoteFPSAgentController>();

            var sceneManager = GameObject.FindObjectOfType<PhysicsSceneManager>();
            var initialSpawnPosition = new Vector3(receptacleSimObj.transform.position.x, receptacleSimObj.transform.position.y + 100f, receptacleSimObj.transform.position.z); ;

            var spawned = GameObject.Instantiate(go, initialSpawnPosition, Quaternion.identity);
            if (rotation != null) {
                Vector3 toRot = rotation.axis * rotation.degrees;
                spawned.transform.Rotate(toRot.x, toRot.y, toRot.z);
            }

            var toSpawn = spawned.GetComponent<SimObjPhysics>();
            Rigidbody rb = spawned.GetComponent<Rigidbody>();
            rb.isKinematic = true;

            //ensure bounding boxes for spawned object are defaulted correctly so placeNewObjectAtPoint doesn't FREAK OUT
            toSpawn.RegenerateBoundingBoxes();

            var success = false;

            if (fpsAgent.placeNewObjectAtPoint(toSpawn, position)) {
                success = true;
                List<Vector3> corners = GetCorners(toSpawn);
                //this only attempts to check the first ReceptacleTriggerBox of the receptacle, does not handle multiple receptacle boxes
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

                bool floorCheck = true;
                //raycast down from the object's position to see if it hits something on the NonInteractive layer (floor mesh collider)
                if (!Physics.Raycast(toSpawn.transform.position, -Vector3.up, Mathf.Infinity, 1 << 12))
                    floorCheck = false;

                if (!cornerCheck || !floorCheck) {
                    success = false;
                }

                //if all corners were succesful, break out of this loop, don't keep trying
                if (success) {
                    rb.isKinematic = false;
                    toSpawn.objectID = objectId;
                    toSpawn.name = objectId;
                    sceneManager.AddToObjectsInScene(toSpawn);
                    toSpawn.transform.SetParent(GameObject.Find("Objects").transform);
                }
            }

            //if after trying all spawn points, it failed to position, delete object from scene and return null
            if (!success) {
                UnityEngine.Object.Destroy(toSpawn.transform.gameObject);
                return null;
            }
            return toSpawn.transform.gameObject;
        }

        //will attempt to spawn prefabName at random free position in receptacle
        public static GameObject spawnObjectInReceptacleRandomly(
            AssetMap<GameObject> goDb, 
            string prefabName,
            string objectId, 
            SimObjPhysics receptacleSimObj,
            AxisAngleRotation rotation = null) {
            var spawnCoordinates = receptacleSimObj.FindMySpawnPointsFromTopOfTriggerBox();
            var go = goDb.getAsset(prefabName);
            var pos = spawnCoordinates.Shuffle_().First();
            //var fpsAgent = GameObject.FindObjectOfType<PhysicsRemoteFPSAgentController>();
            //to potentially support multiagent down the line, reference fpsAgent via agentManager's array of active agents
            var agentManager = GameObject.Find("PhysicsSceneManager").GetComponentInChildren<AgentManager>();
            var fpsAgent = agentManager.agents[0].GetComponent<PhysicsRemoteFPSAgentController>();

            var sceneManager = GameObject.FindObjectOfType<PhysicsSceneManager>();
            var initialSpawnPosition = new Vector3(receptacleSimObj.transform.position.x, receptacleSimObj.transform.position.y + 100f, receptacleSimObj.transform.position.z); ;

            var spawned = GameObject.Instantiate(go, initialSpawnPosition, Quaternion.identity);
            if (rotation != null) {
                Vector3 toRot = rotation.axis * rotation.degrees;
                spawned.transform.Rotate(toRot.x, toRot.y, toRot.z);
            }

            var toSpawn = spawned.GetComponent<SimObjPhysics>();
            Rigidbody rb = spawned.GetComponent<Rigidbody>();
            rb.isKinematic = true;

            //ensure bounding boxes for spawned object are defaulted correctly so placeNewObjectAtPoint doesn't FREAK OUT
            toSpawn.RegenerateBoundingBoxes();

            var success = false;

            for (int i = 0; i < spawnCoordinates.Count; i++) {
                //place object at the given point, this also checks the spawn area to see if its clear
                //if not clear, it will return false
                if (fpsAgent.placeNewObjectAtPoint(toSpawn, spawnCoordinates[i])) {
                    success = true;
                    List<Vector3> corners = GetCorners(toSpawn);
                    //this only attempts to check the first ReceptacleTriggerBox of the receptacle, does not handle multiple receptacle boxes
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

                    bool floorCheck = true;
                    //raycast down from the object's position to see if it hits something on the NonInteractive layer (floor mesh collider)
                    if (!Physics.Raycast(toSpawn.transform.position, -Vector3.up, Mathf.Infinity, 1 << 12))
                        floorCheck = false;

                    if (!cornerCheck || !floorCheck) {
                        success = false;
                        continue;
                    }
                }

                //if all corners were succesful, break out of this loop, don't keep trying
                if (success) {
                    rb.isKinematic = false;
                    toSpawn.objectID = objectId;
                    toSpawn.name = objectId;
                    sceneManager.AddToObjectsInScene(toSpawn);
                    toSpawn.transform.SetParent(GameObject.Find("Objects").transform);
                    break;
                }
            }

            //if after trying all spawn points, it failed to position, delete object from scene and return null
            if (!success) {
                UnityEngine.Object.Destroy(toSpawn.transform.gameObject);
                return null;
            }

            return toSpawn.transform.gameObject;
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