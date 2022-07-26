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
using UnityEngine.AI;

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


        public IEnumerable<string> Keys() {
            return assetMap.Keys;
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

            return GetRectangleMesh(new BoundingBox() { min = minPoint, max = maxPoint }, generateBackFaces);
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
            var triangles = new List<int>() { 3, 2, 0, 2, 1, 0 };
            var normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };

            // TODO: this is not working for some reason, although it works with the walls
            if (generateBackFaces) {
                triangles = triangles.Concat(triangles.AsEnumerable().Reverse()).ToList();

                // vertices = vertices.Concat(vertices).ToArray();
                // uvs = uvs.Concat(uvs).ToArray();
                //triangles = triangles.Concat(triangles.AsEnumerable().Reverse()).ToArray();
                // triangles.AddRange(triangles.AsEnumerable().Reverse());
                //normals = normals.Concat(normals).ToArray();

            }
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles.ToArray();
            mesh.normals = normals;

            return mesh;
        }

        private static IEnumerable<Vector3> GenerateTriangleVisibilityPoints(
            Vector3 p0,
            Vector3 p1,
            Vector3 p2,
            float interval = 1 / 3.0f
        ) {
            // Create Vector2 array from Vector3s, since y-axis is redundant
            Vector2[] tPoints = new Vector2[] { new Vector2(p0.x, p0.z), new Vector2(p1.x, p1.z), new Vector2(p2.x, p2.z) };
            Vector2 vPointLocalOrigin;
            List<Vector2> trianglevPoints2D = new List<Vector2>();

            // Find triangle's largest angle, which we will either use as the local vPoint origin if it's exactly 90 degrees, or to find it  
            Vector2 widestAngledPoint = tPoints[0];
            float longestSideSquare = (tPoints[2] - tPoints[1]).sqrMagnitude;
            float largestAngle = Vector2.Angle(tPoints[0] - tPoints[2], tPoints[1] - tPoints[0]);

            // Check tPoints[1]
            if ((tPoints[0] - tPoints[2]).sqrMagnitude >= longestSideSquare) {
                widestAngledPoint = tPoints[1];
                largestAngle = Vector2.Angle(tPoints[1] - tPoints[0], tPoints[2] - tPoints[1]);
            }

            // Check tPoints[2]
            if ((tPoints[1] - tPoints[0]).sqrMagnitude >= longestSideSquare) {
                widestAngledPoint = tPoints[2];
                largestAngle = Vector2.Angle(tPoints[2] - tPoints[1], tPoints[0] - tPoints[2]);
            }

            // Check if triangle is already right-angled, and if so, use it as the v-point origin
            if (Mathf.Approximately(largestAngle, 90)) {
                Vector2[] rightTriangle1 = new Vector2[3];
                vPointLocalOrigin = widestAngledPoint;

                if (Vector2.Equals(widestAngledPoint, tPoints[0])) {
                    rightTriangle1 = new Vector2[] { tPoints[2], tPoints[0], tPoints[1] }; ;
                } else if (Vector2.Equals(widestAngledPoint, tPoints[1])) {
                    rightTriangle1 = new Vector2[] { tPoints[0], tPoints[1], tPoints[2] };
                } else if (Vector2.Equals(widestAngledPoint, tPoints[2])) {
                    rightTriangle1 = new Vector2[] { tPoints[1], tPoints[2], tPoints[0] };
                }

                trianglevPoints2D = FindVPointsOnTriangle(rightTriangle1, false);
            }

            // If triangle is not right-angled, find v-point origin with trigonometry
            else {
                Vector2[] rightTriangle2 = new Vector2[3];
                // float t;

                // Enters here!!!
                if (Vector2.Equals(widestAngledPoint, tPoints[0])) {
                    trianglevPoints2D = FindVPointLocalOrigin(tPoints[0], tPoints[1], tPoints[2]);
                } else if (Vector2.Equals(widestAngledPoint, tPoints[1])) {
                    trianglevPoints2D = FindVPointLocalOrigin(tPoints[1], tPoints[2], tPoints[0]);
                } else if (Vector2.Equals(widestAngledPoint, tPoints[2])) {
                    trianglevPoints2D = FindVPointLocalOrigin(tPoints[2], tPoints[0], tPoints[1]);
                }
            }

            // Convert Vector2 vPoints to Vector3 vPoints
            List<Vector3> trianglevPoints = new List<Vector3>();
            foreach (Vector2 vPoint2D in trianglevPoints2D) {
                trianglevPoints.Add(new Vector3(vPoint2D.x, p0.y, vPoint2D.y));
            }

            return trianglevPoints;

            List<Vector2> FindVPointLocalOrigin(Vector2 a, Vector2 b, Vector2 c) {
                List<Vector2> rightTriangleVPoints = new List<Vector2>();
                float sideLength = (a - c).magnitude * Mathf.Sin(Mathf.Deg2Rad * (90 - Vector2.Angle(a - c, b - c)));
                vPointLocalOrigin = c + sideLength * (b - c).normalized;
                Vector2[] rightTriangle1 = new Vector2[] { a, vPointLocalOrigin, c };
                Vector2[] rightTriangle2 = new Vector2[] { b, vPointLocalOrigin, a };
                rightTriangleVPoints = FindVPointsOnTriangle(rightTriangle1, false);
                rightTriangleVPoints.AddRange(FindVPointsOnTriangle(rightTriangle2, true));
                return rightTriangleVPoints;
            }

            // Find all valid v-points along local grid
            List<Vector2> FindVPointsOnTriangle(Vector2[] rightTriangle, bool triangle2) {
                int startingX = triangle2 ? 1 : 0;
                float xMax = (rightTriangle[0] - rightTriangle[1]).magnitude;
                float yMax = (rightTriangle[2] - rightTriangle[1]).magnitude;
                Vector2 xIncrement = interval * (rightTriangle[0] - rightTriangle[1]).normalized;
                Vector2 yIncrement = interval * (rightTriangle[2] - rightTriangle[1]).normalized;
                List<Vector2> rightTriangleVPoints = new List<Vector2>();
                Vector2 currentPoint;

                // Check if each v-point is inside right triangle
                for (int i = startingX; i * interval < xMax; i++) {
                    for (int j = 0; j * interval < yMax; j++) {
                        currentPoint = rightTriangle[1] + i * xIncrement + j * yIncrement;
                        if (i == 0 || j == 0 || 360 - 1e-3 <=
                             Vector2.Angle(rightTriangle[0] - currentPoint, rightTriangle[1] - currentPoint) +
                             Vector2.Angle(rightTriangle[1] - currentPoint, rightTriangle[2] - currentPoint) +
                             Vector2.Angle(rightTriangle[2] - currentPoint, rightTriangle[0] - currentPoint)) {
                            rightTriangleVPoints.Add(currentPoint);
                        }
                    }
                }

                return rightTriangleVPoints;
            }
        }

        private static Mesh GenerateFloorMesh(IEnumerable<Vector3> floorPolygon, float yOffset = 0.0f, bool clockWise = false) {

            // Get indices for creating triangles
            var m_points = floorPolygon.Select(p => new Vector2(p.x, p.z)).ToArray();

            var triangleIndices = TriangulateVertices();

            // Get array of vertices for floor
            var floorVertices = m_points.Select(p => new Vector3(p.x, yOffset, p.y)).ToArray();

            // Create the mesh
            var floor = new Mesh();
            floor.vertices = floorVertices;
            floor.triangles = triangleIndices;
            floor.RecalculateNormals();
            floor.RecalculateBounds();

            // Get UVs for mesh's vertices
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

                        if (!clockWise) { 
                            indices.Add(a);
                            indices.Add(b);
                            indices.Add(c);
                        }
                        else {
                             indices.Add(c);
                             indices.Add(b);
                            indices.Add(a);
                        }
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
            Transform transform = null,
            WallRectangularHole hole = null,
            string postfixName = ""
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

            Bounds bounds = new Bounds();

            var rightNorm = right.normalized;
            var topNorm = top.normalized;

            if (hole != null && transform != null) {
                // var wallTransform = new Matrix4x4(rightNorm, topNorm, Vector3.Cross(rightNorm, topNorm), start);
                // var holeMaxWorld = wallTransform.MultiplyPoint(hole.boundingBox.max);
                // var holeMinWorld = wallTransform.MultiplyPoint(hole.boundingBox.min);

                var holeMaxWorld = transform.TransformPoint(hole.boundingBox.max) - (right + top) / 2.0f;// - transform.TransformPoint((right + top) / 2.0f);
                var holeMinWorld = transform.TransformPoint(hole.boundingBox.min - Vector3.forward * 0.1f) - (right + top) / 2.0f;// - transform.TransformPoint((right + top) / 2.0f);

                var dims = holeMaxWorld - holeMinWorld + transform.TransformVector(Vector3.forward) * 0.2f;
                var originalDims = dims;
                var center = holeMinWorld + (dims/ 2.0f);// transform.TransformVector((hole.boundingBox.max + Vector3.forward * 0.2f) - (hole.boundingBox.min - Vector3.forward * 0.1f));

                // Bounds.Contains does not work properly with negative sizes
                dims.z = Math.Sign(dims.z) == 1 ? dims.z : -dims.z;
                dims.x = Math.Sign(dims.x) == 1 ? dims.x : -dims.x;
               
                bounds = new Bounds(center, dims);

                // Debug Visualize
                /*
                var corners = new List<List<Vector3>> {
                    new List<Vector3>(){ 
                        Vector3.zero,
                        new Vector3(dims.x, 0, 0)
                    },
                    new List<Vector3>(){ 
                        Vector3.zero,
                        new Vector3(0, dims.y, 0)
                    },
                    new List<Vector3>(){ 
                        Vector3.zero,
                        new Vector3(0, 0, dims.z)
                    }
                }.CartesianProduct().Select(x => x.Aggregate(center-(dims/2.0f), (acc, v) => acc + v)).ToList();

                var tmp = corners[3];
                corners[3] = corners[2];
                corners[2] = tmp;

                tmp = corners[7];
                corners[7] = corners[6];
                corners[6] = tmp;

                var sides = new List<List<Vector3>>() { corners.Take(4).ToList(), corners.Skip(4).ToList() };
                if (transform.gameObject.name == "wall|6|4.8|3.2|4.8|4.8") {
                    Debug.Log("Hole min world " + holeMinWorld.ToString("F8") + "Hole max world " + holeMaxWorld.ToString("F8") + " dims " + dims.ToString("F8") + " center " + center.ToString("F8") + " hole dims " + (hole.boundingBox.max - hole.boundingBox.min).ToString("F8"));
                    foreach (var q in sides) {

                        foreach (var (first, second) in q.Zip(q.Skip(3).Concat(q.Take(3)), (first, second) => (first, second)).Concat(sides[0].Zip(sides[1], (first, second)=> (first, second)))) {
                            Debug.DrawLine(first, second, Color.magenta, 1000);
                        }
                    }
                }
                */ 
            }

            for (Vector3 rightDelta = Vector3.zero; (width * width) - rightDelta.sqrMagnitude > (step * step); rightDelta += stepVecRight) {
                for (Vector3 topDelta = Vector3.zero; (height * height) - topDelta.sqrMagnitude > (step * step); topDelta += stepVecTop) {
                    var pos = start + rightDelta + topDelta;

                    if (hole != null && bounds.Contains(pos)) {
                        continue;
                    }

                    var vp = new GameObject($"VisibilityPoint{postfixName} ({count})");
                    vp.transform.position = pos;
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

        public static GameObject CreateVisibilityPointsGameObject(IEnumerable<Vector3> visibilityPoints) {
            var visibilityPointsGO = new GameObject("VisibilityPoints");
            var count = 0;
            foreach (var point in visibilityPoints) {
                var vp = new GameObject($"VisibilityPoint ({count})");
                vp.transform.position = point;
                vp.transform.parent = visibilityPointsGO.transform;
                count++;
            }
            return visibilityPointsGO;
        }

        public static GameObject createWalls(IEnumerable<Wall> walls, AssetMap<Material> materialDb, ProceduralParameters proceduralParameters, string gameObjectId = "Structure") {
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
                    var wallGO = createAndJoinWall(
                        index,
                        materialDb,
                        w0,
                        w1, 
                        w2,
                        squareTiling: proceduralParameters.squareTiling,
                        minimumBoxColliderThickness: proceduralParameters.minWallColliderThickness,
                        layer: (
                            String.IsNullOrEmpty(w0.layer)
                            ? LayerMask.NameToLayer("SimObjVisible")
                            : LayerMask.NameToLayer(w0.layer)
                        )
                    );

                    wallGO.transform.parent = structure.transform;
                    index++;
                }
            }
            return structure;
        }

        public static GameObject createWalls(Room room, AssetMap<Material> materialDb, ProceduralParameters proceduralParameters, string gameObjectId = "Structure") {
            return createWalls(room.walls, materialDb, proceduralParameters, gameObjectId);
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

        private static float getWallDegreesRotation(Wall wall) {
            var p0p1 = wall.p1 - wall.p0;

            var p0p1_norm = p0p1.normalized;

            // var normal = Vector3.Cross(p0p1_norm, Vector3.up);
            var theta = -Mathf.Sign(p0p1_norm.z) * Mathf.Acos(Vector3.Dot(p0p1_norm, Vector3.right));
            return theta * 180.0f / Mathf.PI;
        }

        private static float TriangleArea(List<Vector3> vertices, int index0, int index1, int index2) {
            Vector3 a = vertices[index0];
            Vector3 b = vertices[index1];
            Vector3 c = vertices[index2];
            Vector3 cross = Vector3.Cross(a-b, a-c);
            float area = cross.magnitude * 0.5f;
            Debug.Log($"Area between {index0}, {index1}, {index2} = {area}");
            return area;
        }

        private static float GetBBXYArea(BoundingBox bb) {
            var diff = bb.max - bb.min;
            return diff.x * diff.y;
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
            int layer = 8,
            bool squareTiling = false
        ) {
            var wallGO = new GameObject(toCreate.id);

            SetLayer<Transform>(wallGO, layer);

            var meshF = wallGO.AddComponent<MeshFilter>();
            //var boxC = wallGO.AddComponent<BoxCollider>();
            //var boxC = new BoxCollider();
            // BoundingBox b;

            Vector3 boxCenter = Vector3.zero;
            Vector3 boxSize = Vector3.zero;

            // boxC.convex = true;
            var generateBackFaces = false;
            const float zeroThicknessEpsilon = 1e-4f;
            var colliderThickness = toCreate.thickness < zeroThicknessEpsilon ? minimumBoxColliderThickness : toCreate.thickness;


            var p0p1 = toCreate.p1 - toCreate.p0;

            // var mid = p0p1 * 0.5f;
            // boxC.center = new Vector3(mid.x, )

            var mesh = new Mesh();

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

                boxCenter = center;
            } else {
                p0 = -(width / 2.0f) * Vector3.right - new Vector3(0.0f, toCreate.height / 2.0f, toCreate.thickness / 2.0f);
                p1 = (width / 2.0f) * Vector3.right - new Vector3(0.0f, toCreate.height / 2.0f, toCreate.thickness / 2.0f);

                normal = Vector3.forward;
                p0p1_norm = Vector3.right;
                wallGO.transform.position = center;

                wallGO.transform.rotation = Quaternion.AngleAxis(theta * 180.0f / Mathf.PI, Vector3.up);
            }

            var colliderOffset = Vector3.zero;//toCreate.thickness < zeroThicknessEpsilon ? normal * colliderThickness : Vector3.zero;

            boxCenter += colliderOffset;

            boxSize = new Vector3(width, toCreate.height, colliderThickness);

            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uv = new List<Vector2>();
            var normals = new List<Vector3>();

            var min = p0;
            var max = p1 + new Vector3(0.0f, toCreate.height, 0.0f);

            // BoundingBox box0 = new BoundingBox();
            // BoundingBox box1 = new BoundingBox();
            // BoundingBox box2 = new BoundingBox();
            // BoundingBox box3 = new BoundingBox();

            IEnumerable<BoundingBox> colliderBoundingBoxes = new List<BoundingBox>();

            if (toCreate.hole != null) {
                var dims = toCreate.hole.boundingBox.max - toCreate.hole.boundingBox.min;
                var offset = new Vector2(
                    toCreate.hole.boundingBox.min.x, toCreate.hole.boundingBox.min.y
                );
                Debug.Log("offset " + offset + " dims " + dims);

                if (toCreate.hole.wall1 == toCreate.id) {
                    offset = new Vector2(
                        width - toCreate.hole.boundingBox.max.x, toCreate.hole.boundingBox.min.y
                    );
                }

                colliderBoundingBoxes = new List<BoundingBox>() {
                    new BoundingBox() {min = p0, max =  p0
                           + p0p1_norm * offset.x
                           + Vector3.up * (toCreate.height)},
                     new BoundingBox() {
                        min = p0
                            + p0p1_norm * offset.x
                            + Vector3.up * (offset.y + dims.y),
                        max = p0
                            + p0p1_norm * (offset.x + dims.x)
                            + Vector3.up * (toCreate.height)},
                    new BoundingBox() {
                        min = p0
                            + p0p1_norm * (offset.x + dims.x),
                        max = p1 + Vector3.up * (toCreate.height)},
                    new BoundingBox() {
                        min = p0
                            + p0p1_norm * offset.x,
                        max = p0
                            + p0p1_norm * (offset.x + dims.x)
                            + Vector3.up * (offset.y)
                    }
                };
                const float areaEps =0.0001f;
                colliderBoundingBoxes = colliderBoundingBoxes.Where(bb => Math.Abs(GetBBXYArea(bb)) > areaEps).ToList();


                // var box0 = new BoundingBox() {min = p0, max =  p0
                //            + p0p1_norm * offset.x
                //            + Vector3.up * (toCreate.height)};
                // var box1 = new BoundingBox() {
                //         min = p0
                //             + p0p1_norm * offset.x
                //             + Vector3.up * (offset.y + dims.y),
                //         max = p0
                //             + p0p1_norm * (offset.x + dims.x)
                //             + Vector3.up * (toCreate.height)};
            
                // var box2 = new BoundingBox() {
                //         min = p0
                //             + p0p1_norm * (offset.x + dims.x),
                //         max = p1 + Vector3.up * (toCreate.height)};

                // var box3 = new BoundingBox() {
                //         min = p0
                //             + p0p1_norm * offset.x,
                //         max = p0
                //             + p0p1_norm * (offset.x + dims.x)
                //             + Vector3.up * (offset.y)
                // };

                
                vertices = new List<Vector3>() {
                        p0,
                        p0 + new Vector3(0.0f, toCreate.height, 0.0f),
                        p0 + p0p1_norm * offset.x
                           + Vector3.up * offset.y,
                        p0
                           + p0p1_norm * offset.x
                           + Vector3.up * (offset.y + dims.y),

                        p1 +  new Vector3(0.0f, toCreate.height, 0.0f),

                        p0
                           + p0p1_norm * (offset.x + dims.x)
                           + Vector3.up * (offset.y + dims.y),

                        p1,

                        p0
                        + p0p1_norm * (offset.x + dims.x)
                        + Vector3.up * offset.y

                    };
                //
                Debug.Log($"-------- Cut holes vertices for wall {toCreate.id} center {center} transformed {String.Join(", ", vertices.Select(v => wallGO.transform.TransformPoint(v).ToString("F8")))}");

                // triangles = new List<int>() {
                //      1, 0, 2, 1, 2, 3, 1, 3, 4, 4, 5, 3, 4, 5, 6, 0, 6, 7, 0, 7, 2 };

                

                triangles = new List<int>() {
                     0, 1, 2, 1, 3, 2, 1, 4, 3, 3, 4, 5, 4, 6, 5, 5, 6, 7, 7, 6, 0, 0, 2, 7};

                var toRemove = new List<int>();
                // const float areaEps = 1e-4f;
                for (int i = 0; i < triangles.Count/3; i++) {
                    var i0 = triangles[i*3];
                    var i1 = triangles[ i*3 + 1];
                    var i2 = triangles[ i*3 + 2];
                    var area = TriangleArea(vertices, i0, i1, i2);
                    
                    if (area <= areaEps) {
                        toRemove.AddRange(new List<int>() { i*3, i*3 + 1, i*3 + 2 });
                    }
                }
                var toRemoveSet = new HashSet<int>(toRemove);
                Debug.Log($"ToRemove for wall {toCreate.id} {string.Join(",", toRemove)}");
                
                triangles = triangles.Where((t, i) => !toRemoveSet.Contains(i)).ToList();

            } else {

                vertices = new List<Vector3>() {
                        p0,
                        p0 + new Vector3(0.0f, toCreate.height, 0.0f),
                        p1 +  new Vector3(0.0f, toCreate.height, 0.0f),
                        p1
                    };

                triangles = new List<int>() { 1, 2, 0, 2, 3, 0 };
                if (generateBackFaces) {
                    triangles.AddRange(triangles.AsEnumerable().Reverse().ToList());
                }
                // uv = new List<Vector2>() {
                //     new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0)
                // };
                // normals = new List<Vector3>() { -normal, -normal, -normal, -normal };
            }

            normals = Enumerable.Repeat(-normal, vertices.Count).ToList();

            uv = vertices.Select(v =>
                new Vector2(Vector3.Dot(p0p1_norm, v - p0) / width, v.y / toCreate.height))
            .ToList();


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
            meshF.sharedMesh = mesh;
            var meshRenderer = wallGO.AddComponent<MeshRenderer>();

            if (toCreate.hole != null) {
                // var meshCollider  = wallGO.AddComponent<MeshCollider>();
                // meshCollider.sharedMesh = mesh;
                
                var holeColliders = new GameObject($"Colliders");

                
                
                holeColliders.transform.parent = wallGO.transform;
                holeColliders.transform.localPosition = Vector3.zero;
                holeColliders.transform.localRotation = Quaternion.identity;

                var i = 0;
                foreach (var boundingBox in colliderBoundingBoxes) {

                    var colliderObj = new GameObject($"Collider_{i}");
                    colliderObj.transform.parent = holeColliders.transform;
                    colliderObj.transform.localPosition = Vector3.zero;
                    colliderObj.transform.localRotation = Quaternion.identity;
                    colliderObj.tag = "SimObjPhysics";
                    colliderObj.layer = 8;
                    var boxCollider = colliderObj.AddComponent<BoxCollider>();
                    boxCollider.center = boundingBox.center();
                    boxCollider.size = boundingBox.size() + Vector3.forward * colliderThickness;

                }

                // var colliderObj0 = new GameObject($"Collider_0");
                // colliderObj0.transform.parent = holeColliders.transform;
                // colliderObj0.transform.localPosition = Vector3.zero;
                // colliderObj0.transform.localRotation = Quaternion.identity;
                // var collider0 = colliderObj0.AddComponent<BoxCollider>();
                // collider0.center = box0.center();
                // collider0.size = box0.size();

                // var colliderObj1 = new GameObject($"Collider_1");
                // colliderObj1.transform.parent = holeColliders.transform;
                // colliderObj1.transform.localPosition = Vector3.zero;
                // colliderObj1.transform.localRotation = Quaternion.identity;
                // var collider1 = colliderObj1.AddComponent<BoxCollider>();
                // collider1.center = box1.center();
                // collider1.size = box1.size();

                // var colliderObj2 = new GameObject($"Collider_2");
                // colliderObj2.transform.parent = holeColliders.transform;
                // colliderObj2.transform.localPosition = Vector3.zero;
                // colliderObj2.transform.localRotation = Quaternion.identity;
                // var collider2 = colliderObj2.AddComponent<BoxCollider>();
                // collider2.center = box2.center();
                // collider2.size = box2.size();
                
                // if (Math.Abs(box3.max.y - box3.min.y) > 1e-4) {
                //     var colliderObj3 = new GameObject($"Collider_3");
                //     colliderObj3.transform.parent = holeColliders.transform;
                //     colliderObj3.transform.localPosition = Vector3.zero;
                //     colliderObj3.transform.localRotation = Quaternion.identity;
                //     var collider3 = colliderObj3.AddComponent<BoxCollider>();
                //     collider3.center = box3.center();
                //     collider3.size = box3.size();
                // }
                
            }
            else {
                var boxC = wallGO.AddComponent<BoxCollider>();
                boxC.center = boxCenter;
                boxC.size = boxSize;
            }

            // TODO use a material loader that has this dictionary
            //var mats = ProceduralTools.FindAssetsByType<Material>().ToDictionary(m => m.name, m => m);
            // var mats = ProceduralTools.FindAssetsByType<Material>().GroupBy(m => m.name).ToDictionary(m => m.Key, m => m.First());

            
            var visibilityPointsGO = CreateVisibilityPointsOnPlane(
                toCreate.p0,
                toCreate.p1 - toCreate.p0,
                (Vector3.up * toCreate.height),
                visibilityPointInterval,
                wallGO.transform,
                toCreate.hole
            );

            setWallSimObjPhysics(wallGO, toCreate.id, visibilityPointsGO, boxCenter, boxSize);
            ProceduralTools.setFloorProperties(wallGO, toCreate);

            visibilityPointsGO.transform.parent = wallGO.transform;
            //if (mats.ContainsKey(wall.materialId)) {
            // meshRenderer.sharedMaterial = materialDb.getAsset(toCreate.materialId);
            var dimensions = new Vector2(p0p1.magnitude, toCreate.height);
            var prev_p0p1 = previous.p1 - previous.p0;


            var prevOffset = getWallMaterialOffset(previous.id).GetValueOrDefault(Vector2.zero);
            var offsetX = (prev_p0p1.magnitude / previous.materialTilingXDivisor) - Mathf.Floor(prev_p0p1.magnitude / previous.materialTilingXDivisor) + prevOffset.x;

            // TODO Offset Y would require to get joining walls from above and below 
            var mat = string.IsNullOrEmpty(toCreate.materialId) ? new Material(Shader.Find("Standard")) : materialDb.getAsset(toCreate.materialId);
            meshRenderer.material = generatePolygonMaterial(mat, toCreate.color, dimensions, toCreate.materialTilingXDivisor, toCreate.materialTilingYDivisor, offsetX, 0.0f, toCreate.unlit, 
                        squareTiling: squareTiling, materialProperties: toCreate.materialProperties);

            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;

            meshF.sharedMesh.RecalculateBounds();

            // var materialCopy = new Material(materialDb.getAsset(toCreate.materialId));
            // materialCopy.mainTextureScale = new Vector2(p0p1.magnitude / toCreate.materialTilingXDivisor, toCreate.height / toCreate.materialTilingYDivisor);

            // materialCopy.mainTextureOffset = new Vector2((prev_p0p1.magnitude / previous.materialTilingXDivisor) - Mathf.Floor(prev_p0p1.magnitude / previous.materialTilingXDivisor), 0);//previous.height - Mathf.Floor(previous.height));
            // if (toCreate.color != null) {
            //     materialCopy.color =  new Color(toCreate.color.r, toCreate.color.g, toCreate.color.b, toCreate.color.a);
            // }


            // meshRenderer.material = materialCopy;
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
            Vector3 boxCenter,
            Vector3 boxSize
        ) {
            var wallrb = wallGameObject.AddComponent<Rigidbody>();
            wallrb.isKinematic = true;

            var boundingBox = new GameObject("BoundingBox");
            // SimObjInvisible
            boundingBox.layer = 9;
            var bbCollider = boundingBox.AddComponent<BoxCollider>();
            bbCollider.enabled = false;
            boundingBox.transform.parent = wallGameObject.transform;
            boundingBox.transform.localPosition = Vector3.zero;
            boundingBox.transform.localRotation = Quaternion.identity;

            bbCollider.center = boxCenter;
            bbCollider.size = boxSize;

            wallGameObject.tag = "SimObjPhysics";

            var simObjPhysics = wallGameObject.AddComponent<SimObjPhysics>();
            simObjPhysics.objectID = simObjId;
            simObjPhysics.ObjType = SimObjType.Wall;
            simObjPhysics.PrimaryProperty = SimObjPrimaryProperty.Static;
            simObjPhysics.SecondaryProperties = new SimObjSecondaryProperty[] { };

            simObjPhysics.BoundingBox = boundingBox;

            simObjPhysics.VisibilityPoints = visibilityPoints.GetComponentsInChildren<Transform>();

            var PotentialColliders = wallGameObject.GetComponentsInChildren<Collider>(false);
            List<Collider> actuallyMyColliders = new List<Collider>();
            foreach (Collider c in PotentialColliders) {
                if(c.enabled)
                actuallyMyColliders.Add(c);
            }

            simObjPhysics.MyColliders =  actuallyMyColliders.ToArray();

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
            return setRoomSimObjectPhysics(
                floorGameObject,
                simObjId,
                visibilityPoints,
                new GameObject[] { receptacleTriggerBox },
                new Collider[] { collider }
            );
        }

        public static RoomProperties setRoomProperties(GameObject gameObject, RoomHierarchy room) {
            var roomProps = gameObject.AddComponent<RoomProperties>();
            roomProps.RoomType = room.roomType;
            return roomProps;
        }

        public static WallProperties setFloorProperties(GameObject gameObject, Wall wall) {
            var wallProps = gameObject.AddComponent<WallProperties>();
            wallProps.RoomId = wall.roomId;
            return wallProps;
        }

        public static ConnectionProperties setConnectionProperties(GameObject gameObject, WallRectangularHole hole) {
            var holeProps = gameObject.AddComponent<ConnectionProperties>();
            holeProps.OpenFromRoomId = hole.room0;
            holeProps.OpenToRoomId = hole.room1;
            return holeProps;
        }

        public static SimObjPhysics setRoomSimObjectPhysics(
            GameObject floorGameObject,
            string simObjId,
            GameObject visibilityPoints,
            GameObject[] receptacleTriggerBoxes = null,
            Collider[] colliders = null
        ) {
            var boundingBox = new GameObject("BoundingBox");
            var bbCollider = boundingBox.AddComponent<BoxCollider>();
            bbCollider.enabled = false;
            boundingBox.transform.parent = floorGameObject.transform;

            var simObjPhysics = floorGameObject.AddComponent<SimObjPhysics>();
            simObjPhysics.objectID = simObjId;
            simObjPhysics.ObjType = SimObjType.Floor;
            simObjPhysics.PrimaryProperty = SimObjPrimaryProperty.Static;
            var secondaryProperties = new SimObjSecondaryProperty[] { };

            simObjPhysics.BoundingBox = boundingBox;

            simObjPhysics.VisibilityPoints = visibilityPoints.GetComponentsInChildren<Transform>();
            visibilityPoints.transform.parent = floorGameObject.transform;

            simObjPhysics.ReceptacleTriggerBoxes = receptacleTriggerBoxes ?? Array.Empty<GameObject>();
            simObjPhysics.MyColliders = colliders ?? Array.Empty<Collider>();

            simObjPhysics.transform.parent = floorGameObject.transform;

            if (receptacleTriggerBoxes != null) {
                secondaryProperties = new SimObjSecondaryProperty[] { SimObjSecondaryProperty.Receptacle };
                foreach (var receptacleTriggerBox in receptacleTriggerBoxes) {
                    receptacleTriggerBox.AddComponent<Contains>();
                }
            }
            simObjPhysics.SecondaryProperties = secondaryProperties;

            return simObjPhysics;
        }
        public static GameObject createSimObjPhysicsGameObject(string name = "Floor", Vector3? position = null, string tag = "SimObjPhysics", int layer = 8, bool withRigidBody = true) {

            var floorGameObject = new GameObject(name);
            floorGameObject.transform.position = position.GetValueOrDefault();

            floorGameObject.tag = tag;
            floorGameObject.layer = layer;

            if (withRigidBody) {
                var rb = floorGameObject.AddComponent<Rigidbody>();
                rb.mass = 1.0f;
                rb.angularDrag = 0.05f;
                rb.useGravity = true;
                rb.isKinematic = true;
            }

            floorGameObject.AddComponent<MeshFilter>();

            floorGameObject.AddComponent<MeshRenderer>();

            return floorGameObject;
        }

        public static GameObject createFloorGameObject(string name, RectangleRoom room, AssetMap<Material> materialDb, ProceduralParameters proceduralParameters, string simObjId, Vector3? position = null) {
            var floorGameObject = createSimObjPhysicsGameObject(name, position);

            floorGameObject.GetComponent<MeshFilter>().mesh = ProceduralTools.GetRectangleFloorMesh(room);
            // TODO generate ceiling

            var meshRenderer = floorGameObject.GetComponent<MeshRenderer>();
            meshRenderer.material = materialDb.getAsset(room.rectangleFloor.materialId);

            var visibilityPoints = ProceduralTools.CreateVisibilityPointsGameObject(room);
            visibilityPoints.transform.parent = floorGameObject.transform;

            var receptacleTriggerBox = ProceduralTools.createFloorReceptacle(floorGameObject, room, proceduralParameters.receptacleHeight);
            var collider = ProceduralTools.createFloorCollider(floorGameObject, room, proceduralParameters.floorColliderThickness);

            ProceduralTools.setRoomSimObjectPhysics(floorGameObject, simObjId, visibilityPoints, receptacleTriggerBox, collider.GetComponentInChildren<Collider>());

            receptacleTriggerBox.AddComponent<Contains>();

            ProceduralTools.createWalls(room, materialDb, proceduralParameters, "Structure");
            return floorGameObject;
        }

        private static void setUpFloorMesh(GameObject floorGameObject, Mesh mesh, Material material) {
            floorGameObject.GetComponent<MeshFilter>().mesh = mesh;
            var meshRenderer = floorGameObject.GetComponent<MeshRenderer>();
            meshRenderer.material = material;
        }

        private static BoundingBox getRoomRectangle(IEnumerable<Vector3> polygonPoints) {
            
            var minY = polygonPoints.Count() > 0 ? polygonPoints.Min(p => p.y) : 0.0f;

            var minPoint = new Vector3(polygonPoints.Count() > 0 ? polygonPoints.Min(c => c.x) : 0.0f, minY, polygonPoints.Count() > 0 ? polygonPoints.Min(c => c.z) : 0.0f);
            var maxPoint = new Vector3(polygonPoints.Count() > 0 ? polygonPoints.Max(c => c.x) :0.0f, minY, polygonPoints.Count() > 0 ? polygonPoints.Max(c => c.z):0.0f);
            // Debug.Log(" min " + minPoint + " max " + maxPoint);
            return new BoundingBox() { min = minPoint, max = maxPoint };
        }

        private static Wall polygonWallToSimpleWall(PolygonWall wall, Dictionary<string, WallRectangularHole> holes) {
            //wall.polygon.
            var polygons = wall.polygon.OrderBy(p => p.y);
            var maxY = wall.polygon.Max(p => p.y);
            WallRectangularHole val;
            var hole = holes.TryGetValue(wall.id, out val) ? val : null;
            var p0 = polygons.ElementAt(0);
            return new Wall() {
                id = wall.id,
                p0 = polygons.ElementAt(0),
                p1 = polygons.ElementAt(1),
                height = maxY - p0.y,
                materialId = wall.material,
                materialProperties = wall.materialProperties,
                empty = wall.empty,
                roomId = wall.roomId,
                thickness = wall.thickness,
                hole = hole,
                materialTilingXDivisor = wall.materialTilingXDivisor,
                materialTilingYDivisor = wall.materialTilingYDivisor,
                color = wall.color,
                unlit = wall.unlit,
                layer = wall.layer,
            };
        }

        private static Vector2 getAxisAlignedWidthDepth(IEnumerable<Vector3> polygon) {
             // TODO: include rotation in json for floor and ceiling to compute the real scale not axis aligned scale

            if (polygon.Count() > 1) {
                var maxX = polygon.Max(p => p.x);
                var maxZ = polygon.Max(p => p.z);

                var minX = polygon.Min(p => p.x);
                var minZ = polygon.Min(p => p.z);
                

                var width =  maxX - minX;
                var depth = maxZ - minZ;
                return new Vector2(width, depth);
            }
            return Vector2.zero;
        }

        private static Vector2? getWallMaterialOffset(string wallId) {
            var wallGO = GameObject.Find(wallId);
            if (wallGO == null) {
                return null;
            }
                var renderer = wallGO.GetComponent<MeshRenderer>();
                if (renderer == null) {
                    return null;
                }
            return renderer.material.mainTextureOffset;
        }

        private static Material generatePolygonMaterial(Material sharedMaterial, SerializableColor color, Vector2 dimensions, float? tilingDivisorX = null, float? tilingDivisorY = null, float offsetX = 0.0f, float offsetY = 0.0f, bool useUnlitShader = false, bool squareTiling = false, MaterialProperties materialProperties = null) {
            // optimization do not copy when not needed
            if (color == null && !tilingDivisorX.HasValue && !tilingDivisorY.HasValue && offsetX == 0.0f && offsetY == 0.0f && !useUnlitShader && materialProperties == null) {
                return sharedMaterial;
            }

            var materialCopy = new Material(sharedMaterial);
        

            if (color != null) {
                materialCopy.color = color.toUnityColor();
            }
            
            // if (polygon.Count() > 1) {
            //         var maxX = polygon.Max(p => p.x);
            //         var maxZ = polygon.Max(p => p.z);

            //         var minX = polygon.Min(p => p.x);
            //         var minZ = polygon.Min(p => p.z);

            //         // TODO: include rotation in json for floor and ceiling to compute the real scale not axis aligned scale

            //         var width =  maxX - minX;
            //         var depth = maxZ - minZ;

                var tilingX = dimensions.x / tilingDivisorX.GetValueOrDefault(1.0f);
                var tilingY = dimensions.y / tilingDivisorY.GetValueOrDefault(1.0f);
                if (squareTiling) {
                    tilingX = Math.Max(tilingX, tilingY);
                    tilingY = tilingX;
                }
                    materialCopy.mainTextureScale = new Vector2(tilingX, tilingY);
                    materialCopy.mainTextureOffset = new Vector2(offsetX, offsetY);
                    
                // }

            if (useUnlitShader) {
                var shader = Shader.Find("Unlit/Color");
                materialCopy.shader = shader;
            }

            if (materialProperties != null) {
                materialCopy.SetFloat("_Metallic", materialProperties.metallic);
                materialCopy.SetFloat("_Glossiness", materialProperties.smoothness);
            }

            return materialCopy;
        }

        public static string DefaultHouseRootObjectName => "Floor";
        public static string DefaultRootStructureObjectName => "Structure";

        public static string DefaultRootWallsObjectName => "Walls";

        public static string DefaultCeilingRootObjectName => "Ceiling";

        public static string DefaultLightingRootName => "ProceduralLighting";
        public static string DefaultObjectsRootName => "Objects";

        public static void SetLayer<T>(GameObject go, int layer) where T : Component {
            if (go.GetComponent<T>() != null) {
                go.layer = layer;
            }
            foreach (Transform child in go.transform) {
                SetLayer<T>(child.gameObject, layer);
            }
        }

        public static GameObject CreateHouse(
           ProceduralHouse house,
           AssetMap<Material> materialDb,
           Vector3? position = null
       ) {
            string simObjId = !String.IsNullOrEmpty(house.id) ? house.id : ProceduralTools.DefaultHouseRootObjectName;
            float receptacleHeight = house.proceduralParameters.receptacleHeight;
            float floorColliderThickness = house.proceduralParameters.floorColliderThickness;
            string ceilingMaterialId = house.proceduralParameters.ceilingMaterial;

            var windowsAndDoors = house.doors.Select(d => d as WallRectangularHole).Concat(house.windows);
            // This is incorrect was leading to collision issues assetOffset should not affect the hole cut,
            // it's just an offset for the asset
            // foreach (var obj in windowsAndDoors) {
            //     // NOTE: this is currently necessary to make min=0 correctly on the
            //     // edge of the wall.
            //     obj.boundingBox.min -= obj.assetOffset;
            //     obj.boundingBox.max -= obj.assetOffset;
            // }
            var holes = windowsAndDoors
                .SelectMany(hole => new List<(string, WallRectangularHole)> { (hole.wall0, hole), (hole.wall1, hole) })
                .Where(pair => !String.IsNullOrEmpty(pair.Item1))
                .ToDictionary(pair => pair.Item1, pair => pair.Item2);
            //house.doors.SelectMany(door => new List<string>() { door.wall0, door.wall1 });

            var roomMap = house.rooms.ToDictionary(r => r.id, r => r.floorPolygon.Select((p, i) => (p, i)));

            //var m = house.rooms.Select(r => r.floorPolygon.Select((p, i) => (p, i)));

            // var wallsByRoom = house.walls
            // .GroupBy(w => w.roomId)
            // .ToDictionary(g => g.Key, g => g.Select(w => polygonWallToSimpleWall(w, holes)))
            // .Select(
            //     pair => {
            //         var roomWalls = pair.Value.ToList();
            //         var result = new List<Wall>(roomWalls.Count);

            //         foreach (var (p, i) in roomMap[pair.Key]) {

            //             var min = Mathf.Infinity;
            //             var selected = 0;
            //             var wallIndex = 0;
            //             foreach (var w in roomWalls) {
            //                 var sqrMag = Vector3.SqrMagnitude(p - w.p0);
            //                 if (sqrMag < min) {
            //                     min = sqrMag;
            //                     selected = wallIndex;
            //                 }
            //                 wallIndex++;
            //             }
            //             Debug.Log("Res " + i + " count " + result.Count + " id " + roomWalls[selected].id);
            //             // if (i < result.Count) {
            //             result.Add(roomWalls[selected]);

            //             roomWalls.RemoveAt(selected);
            //             // }
            //         }
            //         return (pair.Key, result);
            //     }
            // );

            //.Select(pair => roomMap[pair.Key]);

            var walls = house.walls.Select(w => polygonWallToSimpleWall(w, holes));

            var wallPoints = walls.SelectMany(w => new List<Vector3>() { w.p0, w.p1 });
            var wallsMinY = wallPoints.Count() > 0? wallPoints.Min(p => p.y) : 0.0f;
            var wallsMaxY =  wallPoints.Count() > 0? wallPoints.Max(p => p.y) : 0.0f;
            var wallsMaxHeight =  walls.Count() > 0? walls.Max(w => w.height) : 0.0f;

            var floorGameObject = createSimObjPhysicsGameObject(
                simObjId,
                position == null ? new Vector3(0, wallsMinY, 0) : position,
                withRigidBody: false
            );

            for (int i = 0; i < house.rooms.Count(); i++) {
                var room = house.rooms.ElementAt(i);
                var subFloorGO = createSimObjPhysicsGameObject(room.id);
                var mesh = ProceduralTools.GenerateFloorMesh(room.floorPolygon);

                // TODO: generate visibility points
                var visibilityPointInterval = 1 / 3.0f;
                // for (int j = 0; j < mesh.triangles.Length; j = j + 3)
                // {
                //     Debug.Log(mesh.vertices[mesh.triangles[j]] + ", " + mesh.vertices[mesh.triangles[j+1]] + ", " + mesh.vertices[mesh.triangles[j+2]]);
                // }

                // floorVisPoints is equal to, for the range of numbers equal to triangle-count...
                var floorVisibilityPoints = Enumerable.Range(0, mesh.triangles.Length / 3)
                // Ex: "For triangle "0", skip "0" * 3 indices in "mesh.triangle" array to get the correct 3 elements, and use those to select the respective indices from the mesh
                .Select(triangleIndex => mesh.triangles.Skip(triangleIndex * 3).Take(3).Select(vertexIndex => mesh.vertices[vertexIndex]))
                // With the selected 3 vertices, select all of the relevant vPoints, using the visibilityPointInvterval as an increment for their spacing
                .SelectMany(vertices => GenerateTriangleVisibilityPoints(vertices.ElementAt(0), vertices.ElementAt(1), vertices.ElementAt(2), visibilityPointInterval));

                var visibilityPointsGO = CreateVisibilityPointsGameObject(floorVisibilityPoints);

                // mesh.subMeshCount
                subFloorGO.GetComponent<MeshFilter>().mesh = mesh;
                var meshRenderer = subFloorGO.GetComponent<MeshRenderer>();

                var dimensions = getAxisAlignedWidthDepth(room.floorPolygon);
                meshRenderer.material = generatePolygonMaterial(
                    materialDb.getAsset(room.floorMaterial),
                    room.floorColor, 
                    dimensions, 
                    room.floorMaterialTilingXDivisor, 
                    room.floorMaterialTilingYDivisor, 
                    squareTiling: house.proceduralParameters.squareTiling, 
                    materialProperties: room.materialProperties
                );

                //set up mesh collider to allow raycasts against only the floor inside the room
                subFloorGO.AddComponent<MeshCollider>();
                var meshCollider = subFloorGO.GetComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh;
                subFloorGO.layer = 12; //raycast to layer 12 so it doesn't interact with any other layer

                // allow layer to be overwritten
                // will break support for spawnObjectInReceptacle
                if (!String.IsNullOrEmpty(room.layer)) {
                    SetLayer<MeshRenderer>(subFloorGO, LayerMask.NameToLayer(room.layer));
                }

                subFloorGO.transform.parent = floorGameObject.transform;

                ProceduralTools.setRoomSimObjectPhysics(subFloorGO, room.id, visibilityPointsGO);
                ProceduralTools.setRoomProperties(subFloorGO, room);

                Collider[] RoomMeshCollider = new Collider[] {meshCollider};
                subFloorGO.GetComponent<SimObjPhysics>().MyColliders = RoomMeshCollider;
            }

            // var minPoint = mesh.vertices[0];
            // var maxPoint = mesh.vertices[2];

            var boundingBox = getRoomRectangle(house.rooms.SelectMany(r => r.floorPolygon));
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

            // TODO Eli, comment this line below
            // var visibilityPoints = ProceduralTools.CreateVisibilityPointsGameObject(roomCluster);

            // TODO Eli, uncomment this
            var visibilityPoints = new GameObject("VisibilityPoints");

            visibilityPoints.transform.parent = floorGameObject.transform;

            // rooms.Select((room, index) => ProceduralTools.createFloorReceptacle(floorGameObject, room, receptacleHeight, $"{index}"));

            var receptacleTriggerBox = ProceduralTools.createFloorReceptacle(floorGameObject, roomCluster, receptacleHeight);
            var collider = ProceduralTools.createFloorCollider(floorGameObject, roomCluster, floorColliderThickness);




            ProceduralTools.setRoomSimObjectPhysics(floorGameObject, simObjId, visibilityPoints, receptacleTriggerBox, collider.GetComponentInChildren<Collider>());



            // foreach (var (id, wallsInRooms) in wallsByRoom) {
            //     ProceduralTools.createWalls(wallsInRooms, materialDb, $"Structure_{index}");
            //     index++;
            // }

            var structureGO = new GameObject(DefaultRootStructureObjectName);

            var wallsGO = ProceduralTools.createWalls(walls, materialDb, house.proceduralParameters, DefaultRootWallsObjectName);

            floorGameObject.transform.parent = structureGO.transform;
            wallsGO.transform.parent = structureGO.transform;

            // generate ceiling
            if (ceilingMaterialId != "") {
                var ceilingParent = new GameObject(DefaultCeilingRootObjectName);

                // OLD rectangular ceiling, may be usefull to have as a feature, much faster
                // var ceilingGameObject = createSimObjPhysicsGameObject(DefaultCeilingRootObjectName, new Vector3(0, wallsMaxY + wallsMaxHeight, 0), "Structure", 0);
                // var ceilingMesh = ProceduralTools.GetRectangleFloorMesh(new List<RectangleRoom> { roomCluster }, 0.0f, house.proceduralParameters.ceilingBackFaces);

                // var ceilingMesh = ProceduralTools.GenerateFloorMesh()


                // var k = house.rooms.SelectMany(r =>  r.floorPolygon.Select(p => new Vector3(p.x, p.y + wallsMaxY + wallsMaxHeight, p.z)).ToList()).ToList();


                var ceilingMeshes = house.rooms.Select(r => ProceduralTools.GenerateFloorMesh(r.floorPolygon, yOffset:  0.0f, clockWise: true)).ToArray();
                // var ceilingMesh = house.rooms.Select(r => ProceduralTools.GenerateFloorMesh(r.floorPolygon, yOffset:  0.0f, clockWise: true)).Aggregate(new Mesh(), (acc, mesh) => {
                //     acc.vertices = acc.vertices.Concat(mesh.vertices).ToArray();
                //     acc.triangles = acc.triangles.Concat(mesh.triangles).ToArray();
                //     acc.uv = acc.uv.Concat(mesh.uv).ToArray();
                //     return acc;
                // });
                // ceilingMesh.RecalculateBounds();
                // ceilingMesh.RecalculateNormals();

                // var ceilingMesh = ProceduralTools.GenerateFloorMesh(house.rooms[0].floorPolygon, yOffset:  0.0f, clockWise: true);
                //  for (int i = 0; i < house.rooms.Count(); i++) {
                //     var room = house.rooms.ElementAt(i);
                //     var subFloorGO = createSimObjPhysicsGameObject(room.id);

                   
                //     var mesh = ProceduralTools.GenerateFloorMesh(room.floorPolygon);
                //  }

                

                for (int i = 0; i < house.rooms.Count(); i++) {
                    var ceilingMesh = ceilingMeshes[i];
                    var room = house.rooms[i];
                    var floorName = house.rooms[i].id;

                    var ceilingGameObject = createSimObjPhysicsGameObject($"{DefaultCeilingRootObjectName}_{floorName}", new Vector3(0, wallsMaxY + wallsMaxHeight, 0), "Structure", 0);

                    StructureObject so = ceilingGameObject.AddComponent<StructureObject>();
                    so.WhatIsMyStructureObjectTag = StructureObjectTag.Ceiling;               

                    ceilingGameObject.GetComponent<MeshFilter>().mesh = ceilingMesh;
                    var ceilingMeshRenderer = ceilingGameObject.GetComponent<MeshRenderer>();

                    // var materialCopy = new Material(materialDb.getAsset(ceilingMaterialId));
                    
                    var dimensions = getAxisAlignedWidthDepth(ceilingMesh.vertices);

                    var roomCeilingMaterialId = ceilingMaterialId;
                    var ceilingTilingXDivisor =  house.proceduralParameters.ceilingMaterialTilingXDivisor;
                    var ceilingTilingYDivisor =  house.proceduralParameters.ceilingMaterialTilingYDivisor;
                    MaterialProperties ceilingMaterialProperties = null;
                    if (room.ceilings.Count > 0) {
                        ceilingTilingXDivisor = room.ceilings[0].tilingDivisorX;
                        ceilingTilingYDivisor = room.ceilings[0].tilingDivisorY;
                        ceilingMaterialProperties = room.ceilings[0].materialProperties;
                        if (!string.IsNullOrEmpty(room.ceilings[0].material)) {
                            roomCeilingMaterialId = room.ceilings[0].material;
                            
                        }
                    }
                    ceilingMeshRenderer.material = generatePolygonMaterial(
                        materialDb.getAsset(roomCeilingMaterialId),
                        house.proceduralParameters.ceilingColor,
                        dimensions,
                        ceilingTilingXDivisor,
                        ceilingTilingYDivisor,
                        0.0f,
                        0.0f,
                        house.proceduralParameters.unlitCeiling,
                        squareTiling: house.proceduralParameters.squareTiling,
                        materialProperties: ceilingMaterialProperties
                    );
                    ceilingMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;

                    tagObjectNavmesh(ceilingGameObject, "Not Walkable");

                    ceilingGameObject.transform.parent = ceilingParent.transform;

                }

                ceilingParent.transform.parent = structureGO.transform;

            }

            foreach (var obj in house.objects) {
                // var go = ProceduralTools.spawnObject(ProceduralTools.getAssetMap(), obj);
                // tagObjectNavmesh(go, "Not Walkable");
                spawnObjectHierarchy(obj);
            }

            


            var assetMap = ProceduralTools.getAssetMap();
            var doorsToWalls = windowsAndDoors.Select(
                door => (
                    door,
                    wall0: walls.First(w => w.id == door.wall0),
                    wall1: walls.FirstOrDefault(w => w.id == door.wall1)
                )
            ).ToDictionary(d => d.door.id, d => (d.wall0, d.wall1));
            var count = 0;
            foreach (WallRectangularHole holeCover in windowsAndDoors) {
                var coverPrefab = assetMap.getAsset(holeCover.assetId);
                (Wall wall0, Wall wall1) wall;
                var wallExists = doorsToWalls.TryGetValue(holeCover.id, out wall);

                if (wallExists) {

                    // TODO Hack for inconsistent doors and windows
                    // if (holeCover.GetType().IsAssignableFrom(typeof(Thor.Procedural.Data.Door))) {
                    //     var tmp = wall.wall0;    
                    //     wall.wall0 = wall.wall1;
                    //     wall.wall1 = tmp;
                    // }
                    var p0p1 = wall.wall0.p1 - wall.wall0.p0;


                    var p0p1_norm = p0p1.normalized;
                    var normal = Vector3.Cross(Vector3.up, p0p1_norm);
                    var pos = wall.wall0.p0 + (p0p1_norm * (holeCover.boundingBox.min.x + holeCover.assetOffset.x)) + Vector3.up * (holeCover.boundingBox.min.y + holeCover.assetOffset.y); //- normal * holeCover.boundingBox.min.z/2.0f;
                    Debug.Log($" ********* Spawn connection at {pos.ToString("F8")}");
                    var rotY = getWallDegreesRotation(new Wall { p0 = wall.wall0.p1, p1 = wall.wall0.p0 });
                    //var rotY = getWallDegreesRotation(wall.wall0);
                    var rotation = Quaternion.AngleAxis(rotY, Vector3.up);



                    var go = spawnSimObjPrefab(
                        prefab: coverPrefab,
                        id: holeCover.id,
                        assetId: holeCover.assetId,
                        position: pos,
                        // new FlexibleRotation() { axis = Vector3.up,  degrees = rotY },
                        rotation: rotation,
                        kinematic: true,
                        color: holeCover.color,
                        positionBoundingBoxCenter: false
                    );

                    setConnectionProperties(go, holeCover);

                    // if (holeCover.open) {
                        var canOpen = go.GetComponentInChildren<CanOpen_Object>();
                        if (canOpen != null) {
                            Debug.Log("OPENNESS --- " + holeCover.openness);
                            canOpen.SetOpennessImmediate(holeCover.openness);
                        }
                    // }

                    count++;
                    tagObjectNavmesh(go, "Not Walkable");
                }
            }

            var lightingRoot = new GameObject(DefaultLightingRootName);
            if (house.proceduralParameters.lights != null) {
                foreach (var lightParams in house.proceduralParameters.lights) {
                    var go = new GameObject(lightParams.id);
                    go.transform.position = lightParams.position;
                    if (lightParams.rotation != null) {
                        go.transform.rotation = lightParams.rotation.toQuaternion();
                    }
                    var light = go.AddComponent<Light>();
                    //light.lightmapBakeType = LightmapBakeType.Realtime; //removed because this is editor only, and probably not needed since the light should default to Realtime Light Mode anyway?
                    light.type = (LightType)Enum.Parse(typeof(LightType), lightParams.type, ignoreCase: true);
                    light.color = new Color(lightParams.rgb.r, lightParams.rgb.g, lightParams.rgb.b, lightParams.rgb.a);
                    light.intensity = lightParams.intensity;
                    light.bounceIntensity = lightParams.indirectMultiplier;
                    light.range = lightParams.range;
                    if (lightParams.cullingMaskOff != null) {
                        foreach (var layer in lightParams.cullingMaskOff) {
                            light.cullingMask &= ~(1 << LayerMask.NameToLayer(layer));
                        }
                    }

                    if (lightParams.shadow != null) {
                        light.shadowStrength = lightParams.shadow.strength;
                        light.shadows = (LightShadows)Enum.Parse(typeof(LightShadows), lightParams.shadow.type, ignoreCase: true);
                        light.shadowBias = lightParams.shadow.bias;
                        light.shadowNormalBias = lightParams.shadow.normalBias;
                        light.shadowNearPlane = lightParams.shadow.nearPlane;
                        light.shadowResolution = (UnityEngine.Rendering.LightShadowResolution)Enum.Parse(typeof(UnityEngine.Rendering.LightShadowResolution), lightParams.shadow.resolution, ignoreCase: true);
                    }
                    go.transform.parent = lightingRoot.transform;

                }
            }

            if (house.proceduralParameters.reflections != null) {
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

                    go.transform.parent = lightingRoot.transform;
                }
            }

            buildNavMesh(floorGameObject, house.proceduralParameters.navmeshVoxelSize);

            RenderSettings.skybox = materialDb.getAsset(house.proceduralParameters.skyboxId);
            DynamicGI.UpdateEnvironment();
            GameObject.FindObjectOfType<ReflectionProbe>().GetComponent<ReflectionProbe>().RenderProbe();

            //generate objectId for newly created wall/floor objects
            //also add them to objectIdToSimObjPhysics dict so they can be found via
            //getTargetObject() and other things that use that dict
            //also add their rigidbodies to the list of all rigid body objects in scene
            var sceneManager = GameObject.FindObjectOfType<PhysicsSceneManager>();
            sceneManager.SetupScene(false);
            var agentManager = GameObject.Find("PhysicsSceneManager").GetComponentInChildren<AgentManager>();
            agentManager.ResetSceneBounds();

            setAgentPose(house: house, agentManager: agentManager);

            return floorGameObject;
        }

        private static void setAgentPose(ProceduralHouse house, AgentManager agentManager) {
            // teleport the agent into the scene
            if (
                house.metadata != null
                && house.metadata.agentPoses != null
                && house.metadata.agentPoses.ContainsKey(agentManager.agentMode)
            ) {
                BaseFPSAgentController bfps = agentManager.primaryAgent;
                Vector3 newPosition = house.metadata.agentPoses[agentManager.agentMode].position;
                Vector3 newRotation = house.metadata.agentPoses[agentManager.agentMode].rotation;
                float newHorizon = house.metadata.agentPoses[agentManager.agentMode].horizon;
                bool? newStanding = house.metadata.agentPoses[agentManager.agentMode].standing;
                if (newPosition != null) {
                    bfps.transform.position = newPosition;
                    bfps.autoSyncTransforms();//make sure to sync transforms after teleporting to ensure rigidbody/transforms are all updated even if a frame hasn't passed

                    Vector3 target = new Vector3(
                        newPosition.x,
                        bfps.transform.position.y,
                        newPosition.z
                    );
                    Vector3 dir = target - bfps.transform.position;
                    Vector3 movement = dir.normalized * 100.0f;
                    if (movement.magnitude > dir.magnitude) {
                        movement = dir;
                    }
                    movement.y = Physics.gravity.y * bfps.m_GravityMultiplier;
                    bfps.GetComponent<CharacterController>().Move(movement);
                }
                if (newRotation != null) {
                    bfps.transform.rotation = Quaternion.Euler(newRotation);
                }
                if (newHorizon != null) {
                    bfps.m_Camera.transform.localEulerAngles = new Vector3(newHorizon, 0, 0);
                }
                if (agentManager.agentMode != "locobot" && newStanding != null) {
                    PhysicsRemoteFPSAgentController pfps = bfps as PhysicsRemoteFPSAgentController;
                    if (newStanding == true) {
                        pfps.stand();
                    } else {
                        pfps.crouch();
                    }
                }
            }
        }

        public static void spawnObjectHierarchy(HouseObject houseObject) {
            if (houseObject == null) {
                return;
            }
            var go = ProceduralTools.spawnHouseObject(ProceduralTools.getAssetMap(), houseObject);
            // Debug.Log("navmesh area for obj " + houseObject.assetId + " area " + houseObject.navmeshArea + " bool " + (houseObject.navmeshArea != ""));
            if (go != null) {
                tagObjectNavmesh(go, "Not Walkable");
            }

            if (houseObject.children != null) {
                foreach (var child in houseObject.children) {
                    spawnObjectHierarchy(child);
                }
            }

        }

        public static void tagObjectNavmesh(GameObject gameObject, string navMeshAreaName = "Walkable") {
            var modifier = gameObject.GetComponent<NavMeshModifier>();
            if (modifier == null) {
                modifier = gameObject.AddComponent<NavMeshModifier>();
            }
            // var modifier = gameObject.AddComponent<NavMeshModifier>();
            modifier.overrideArea = true;
            // Debug.Log("navmesh area " + navMeshAreaName);
            modifier.area = NavMesh.GetAreaFromName(navMeshAreaName);
        }


        public static void buildNavMesh(GameObject floorGameObject, float? voxelSize = null) {

            var navMesh = floorGameObject.AddComponent<NavMeshSurface>();
            // TODO multiple agents
            var navMeshAgent = GameObject.FindObjectOfType<NavMeshAgent>();

            navMesh.agentTypeID = navMeshAgent.agentTypeID;
            var settings = navMesh.GetBuildSettings();
            Debug.Log("Navmesh Agent radius: " + settings.agentRadius + ", Agent height " + settings.agentHeight);

            navMesh.overrideVoxelSize = voxelSize != null;
            navMesh.voxelSize = voxelSize.GetValueOrDefault(0.0f);

            navMesh.BuildNavMesh();

            //     new NavMeshBuildSettings() {
            //     agentTypeID = navmeshAgent.agentTypeID,
            //     agentRadius = 0.2f,
            //     agentHeight = 1.8f,
            //     agentSlope = 10,
            //     agentClimb = 0.5f,
            //     minRegionArea = 0.05f,
            //     overrideVoxelSize = false,
            //     overrideTileSize = false
            // };
            // NavMeshSetup.SetNavMeshNotWalkable(GameObject.Find("Objects"));

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
                string assetName = assetPath.Substring(
                    assetPath.LastIndexOf("/") + 1,
                    assetPath.Length - (assetPath.LastIndexOf("/") + 1) - ".prefab".Length
                );

                // skip all these folders and prefabs
                if (
                    assetPath.Contains("SceneSetupPrefabs")
                    || assetPath.Contains("Entryway Objects")
                    || assetPath.Contains("Custom Project Objects")
                    || assetPath.Contains("Assets/Resources")
                ) {
                    continue;
                }

                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (asset != null && asset.GetComponent<SimObjPhysics>()) {
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
        public static GameObject spawnHouseObject(
            AssetMap<GameObject> goDb,
            HouseObject ho
        ) {
            if (goDb.ContainsKey(ho.assetId)) {

                var go = goDb.getAsset(ho.assetId);
                return spawnSimObjPrefab(
                    prefab: go,
                    id: ho.id,
                    assetId: ho.assetId,
                    position: ho.position,
                    // ho.rotation,
                    rotation: Quaternion.AngleAxis(ho.rotation.degrees, ho.rotation.axis),
                    kinematic: ho.kinematic,
                    color: ho.color,
                    positionBoundingBoxCenter: true,
                    unlit: ho.unlit,
                    materialProperties: ho.materialProperties,
                    openness: ho.openness,
                    isOn: ho.isOn,
                    isDirty: ho.isDirty,
                    layer: ho.layer
                );
            } else {

                Debug.LogError("Asset not in Database " + ho.assetId);
                return null;
            }
        }

        public static GameObject spawnSimObjPrefab(
            GameObject prefab,
            string id,
            string assetId,
            Vector3 position,
            Quaternion rotation,
            // FlexibleRotation rotation,
            bool kinematic = false,
            SerializableColor color = null,
            bool positionBoundingBoxCenter = false,
            bool unlit = false,
            MaterialProperties materialProperties = null,
            float? openness = null,
            bool? isOn = null,
            bool? isDirty = null,
            string layer = null
        ) {
            var go = prefab;

            var spawned = GameObject.Instantiate(original: go); //, position, Quaternion.identity); //, position, rotation);

            if (!String.IsNullOrEmpty(layer)) {
                SetLayer<MeshRenderer>(spawned, LayerMask.NameToLayer(layer));
            }

            if (openness.HasValue) {
                var canOpen = spawned.GetComponentInChildren<CanOpen_Object>();
                if (canOpen != null) {
                    canOpen.SetOpennessImmediate(openness.Value);
                }
            }

            if (isOn.HasValue) {
                var canToggle = spawned.GetComponentInChildren<CanToggleOnOff>();
                if (canToggle != null) {
                    if (isOn.Value != canToggle.isOn) {
                        canToggle.Toggle();
                    }
                }
            }

            if (isDirty.HasValue) {
                var dirt = spawned.GetComponentInChildren<Dirty>();
                if (dirt != null) {
                    if (isDirty.Value != dirt.IsDirty()) {
                        dirt.ToggleCleanOrDirty();
                    }
                }
            }

            spawned.transform.parent = GameObject.Find("Objects").transform;
            // var rotaiton = Quaternion.AngleAxis(rotation.degrees, rotation.axis);
            if (positionBoundingBoxCenter) {
                var simObj = spawned.GetComponent<SimObjPhysics>();
                var box = simObj.AxisAlignedBoundingBox;
                // box.enabled = true;
                var centerObjectSpace = prefab.transform.TransformPoint(box.center);

                spawned.transform.position = rotation * (spawned.transform.localPosition - box.center) + position;
                spawned.transform.rotation = rotation;
            } else {
                spawned.transform.position = position;
                spawned.transform.rotation = rotation;
            }

            var toSpawn = spawned.GetComponent<SimObjPhysics>();
            Rigidbody rb = spawned.GetComponent<Rigidbody>();
            rb.isKinematic = kinematic;

            toSpawn.objectID = id;
            toSpawn.name = id;
            toSpawn.assetID = assetId;

            Shader unlitShader = null;
            if (unlit) {
                unlitShader = Shader.Find("Unlit/Color");
            }

            if (color != null) {
                var materials = toSpawn.GetComponentsInChildren<MeshRenderer>().Select(
                    mr => mr.material
                );
                foreach (var mat in materials) {
                    mat.color = new Color(color.r, color.g, color.b, color.a);
                    if (unlit) {
                        mat.shader = unlitShader;
                    }
                    if (materialProperties != null) { 
                        mat.SetFloat("_Metallic", materialProperties.metallic);
                        mat.SetFloat("_Glossiness", materialProperties.smoothness);
                    }
                }
            }

            // TODO (speed up): move to room creator class
            var sceneManager = GameObject.FindObjectOfType<PhysicsSceneManager>();
            sceneManager.AddToObjectsInScene(toSpawn);
            toSpawn.transform.SetParent(GameObject.Find("Objects").transform);

            SimObjPhysics[] childSimObjects = toSpawn.transform.gameObject.GetComponentsInChildren<SimObjPhysics>();
            int childNumber = 0;
            for (int i = 0; i < childSimObjects.Length; i++) {
                if (childSimObjects[i].objectID == id) {
                    // skip the parent object that's ID has already been assigned
                    continue;
                }
                childSimObjects[i].objectID = $"{id}___{childNumber++}";
            }

            return toSpawn.transform.gameObject;
        }

        public static GameObject spawnObjectInReceptacle(
            PhysicsRemoteFPSAgentController agent,
            AssetMap<GameObject> goDb,
            string prefabName,
            string objectId,
            SimObjPhysics receptacleSimObj,
            Vector3 position,
            FlexibleRotation rotation = null
        ) {
            var go = goDb.getAsset(prefabName);
            //var fpsAgent = GameObject.FindObjectOfType<PhysicsRemoteFPSAgentController>();
            //to potentially support multiagent down the line, reference fpsAgent via agentManager's array of active agents

            var sceneManager = GameObject.FindObjectOfType<PhysicsSceneManager>();
            var initialSpawnPosition = new Vector3(receptacleSimObj.transform.position.x, receptacleSimObj.transform.position.y + 100f, receptacleSimObj.transform.position.z); ;

            var spawned = GameObject.Instantiate(
                original: go,
                position: initialSpawnPosition,
                rotation: Quaternion.identity
            );
            spawned.transform.parent = GameObject.Find("Objects").transform;
            if (rotation != null) {
                Vector3 toRot = rotation.axis * rotation.degrees;
                spawned.transform.Rotate(toRot.x, toRot.y, toRot.z);
            }

            var toSpawn = spawned.GetComponent<SimObjPhysics>();
            Rigidbody rb = spawned.GetComponent<Rigidbody>();
            rb.isKinematic = true;

            //ensure bounding boxes for spawned object are defaulted correctly so placeNewObjectAtPoint doesn't FREAK OUT
            toSpawn.syncBoundingBoxes(true);

            var success = false;

            Debug.Log("---- placeNewObjectAtPoint check");
            if (agent.placeNewObjectAtPoint(toSpawn, position)) {
                success = true;
                List<Vector3> corners = GetCorners(toSpawn);
                //this only attempts to check the first ReceptacleTriggerBox of the receptacle, does not handle multiple receptacle boxes
                Contains con = receptacleSimObj.ReceptacleTriggerBoxes[0].GetComponent<Contains>();
                bool cornerCheck = true;
                foreach (Vector3 p in corners) {
                    if (!con.CheckIfPointIsAboveReceptacleTriggerBox(p)) {
                        Debug.Log("Corner check false");
                        cornerCheck = false;
                        //this position would cause object to fall off table
                        //double back and reset object to try again with another point
                        spawned.transform.position = initialSpawnPosition;
                        break;
                    }
                }

                bool floorCheck = true;
                //raycast down from the object's position to see if it hits something on the NonInteractive layer (floor mesh collider)
                if (
                    !Physics.Raycast(
                        toSpawn.transform.position,
                        -Vector3.up,
                        Mathf.Infinity,
                        LayerMask.GetMask("NonInteractive")
                    )
                ) {
                    Debug.Log("FloorCheck");
                    floorCheck = false;
                }

                if (!cornerCheck || !floorCheck) {
                    Debug.Log("corner || floor");
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
            PhysicsRemoteFPSAgentController agent,
            AssetMap<GameObject> goDb,
            string prefabName,
            string objectId,
            SimObjPhysics receptacleSimObj,
            FlexibleRotation rotation = null
        ) {
            var spawnCoordinates = receptacleSimObj.FindMySpawnPointsFromTopOfTriggerBox();
            var go = goDb.getAsset(prefabName);
            var pos = spawnCoordinates.Shuffle_().First();
            //var fpsAgent = GameObject.FindObjectOfType<PhysicsRemoteFPSAgentController>();
            //to potentially support multiagent down the line, reference fpsAgent via agentManager's array of active agents

            var sceneManager = GameObject.FindObjectOfType<PhysicsSceneManager>();
            var initialSpawnPosition = new Vector3(receptacleSimObj.transform.position.x, receptacleSimObj.transform.position.y + 100f, receptacleSimObj.transform.position.z); ;

            var spawned = GameObject.Instantiate(
                original: go,
                position: initialSpawnPosition,
                rotation: Quaternion.identity
            );
            spawned.transform.parent = GameObject.Find("Objects").transform;
            if (rotation != null) {
                Vector3 toRot = rotation.axis * rotation.degrees;
                spawned.transform.Rotate(toRot.x, toRot.y, toRot.z);
            }

            var toSpawn = spawned.GetComponent<SimObjPhysics>();
            Rigidbody rb = spawned.GetComponent<Rigidbody>();
            rb.isKinematic = true;

            //ensure bounding boxes for spawned object are defaulted correctly so placeNewObjectAtPoint doesn't FREAK OUT
            toSpawn.syncBoundingBoxes(true);

            var success = false;

            for (int i = 0; i < spawnCoordinates.Count; i++) {
                //place object at the given point, this also checks the spawn area to see if its clear
                //if not clear, it will return false
                var canPlace = agent.placeNewObjectAtPoint(toSpawn, spawnCoordinates[i]);
                if (canPlace) {
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
                    if (
                        !Physics.Raycast(
                            toSpawn.transform.position,
                            -Vector3.up,
                            Mathf.Infinity,
                            LayerMask.GetMask("NonInteractive")
                        )
                    ) {
                        floorCheck = false;
                    }

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

        public static bool withinArrayBoundary((int row, int col) current, int rows, int columns) {
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

        public static BoundingBoxWithOffset getHoleAssetBoundingBox(string holeAssetId) {
            var assetMap = ProceduralTools.getAssetMap();

            if (!assetMap.ContainsKey(holeAssetId)) {
                return null;
            }

            GameObject asset = assetMap.getAsset(holeAssetId);

            var holeMetadata = asset.GetComponentInChildren<HoleMetadata>();
            if (holeMetadata == null) {
                return null;
            
            }
            else {
                var diff = holeMetadata.Max - holeMetadata.Min;

                diff = new Vector3(Math.Abs(diff.x), Math.Abs(diff.y), Math.Abs(diff.z));// - holeMetadata.Margin;
                // inverse offset for the asset
                var min = new Vector3(holeMetadata.Min.x, -holeMetadata.Min.y, -holeMetadata.Min.z);
                // var max = new Vector3(-holeMetadata.Max.x, holeMetadata.Max.y, holeMetadata.Max.z);
                return  new BoundingBoxWithOffset() { min=Vector3.zero, max=diff, offset=min};
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
            return new GameObject();
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