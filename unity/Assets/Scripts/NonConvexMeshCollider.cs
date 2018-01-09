
using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Debug = UnityEngine.Debug;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class NonConvexMeshCollider : MonoBehaviour
{
    private bool mergeBoxesToReduceNumber = true;
    private int spatialTreeLevelDepth = 9;
    public bool outputTimeMeasurements = false;

    [Tooltip("Will create a child game object called 'Colliders' to store the generated colliders in. \n\rThis leads to a cleaner and more organized structure. \n\rPlease note that collisions will then report the child game object. So you may want to check for transform.parent.gameObject on your collision check.")]
    public bool createChildGameObject = true;
    [Tooltip("Takes a bit more time to compute, but leads to more performance optimized colliders (less boxes).")]
    public bool avoidGapsInside = false;
    [Tooltip("Makes sure all box colliders are generated completely on the inside of the mesh. More expensive to compute, but desireable if you need to avoid false collisions of objects very close to another, like rings of a chain for example.")]
    public bool avoidExceedingMesh = false;
    [Tooltip("The number of boxes your mesh will be segmented into, on each axis (x, y and z). \n\rHigher values lead to more accurate colliders but on the other hand makes computation and collision checks more expensive.")]
    public int boxesPerEdge = 20;
    [Tooltip("The physics material to apply to the generated compound colliders.")]
    public PhysicMaterial physicsMaterialForColliders;

    public const bool DebugOutput = false;

    public void Calculate()
    {
        var sw = Stopwatch.StartNew();

        if (boxesPerEdge > 100)
            boxesPerEdge = 100;
        if (avoidExceedingMesh && boxesPerEdge > 50)
            boxesPerEdge = 50;
        if (boxesPerEdge < 1)
            boxesPerEdge = 3;

        var go = gameObject;
        var meshFilter = go.GetComponent<MeshFilter>();
        if (meshFilter == null) return;
        if (meshFilter.sharedMesh == null) return;
        var rbdy = go.GetComponent<Rigidbody>();
        var hadNonKinematicRigidbody = false;
        if (rbdy != null && !rbdy.isKinematic)
        {
            hadNonKinematicRigidbody = true;
            rbdy.isKinematic = true;
        }
        var hadRigidbodyWithGravityUse = false;
        if (rbdy != null && rbdy.useGravity)
        {
            hadRigidbodyWithGravityUse = true;
            rbdy.useGravity = false;
        }
        if (!createChildGameObject)
        {
            foreach (var bc in go.GetComponents<BoxCollider>())
                DestroyImmediate(bc);
        }
        var originalLayer = go.layer;
        var collisionLayer = GetFirstEmptyLayer();
        go.layer = collisionLayer;

        var parentGo = go.transform.parent;

        var localPos = go.transform.localPosition;
        var localRot = go.transform.localRotation;
        var localScale = go.transform.localScale;
        var tempParent = new GameObject("Temp_CompoundColliderParent");
        go.transform.parent = tempParent.transform;
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.Euler(Vector3.zero);
        go.transform.localScale = Vector3.one;

        try
        {
            var collidersGo = CreateColliderChildGameObject(go, meshFilter);

            //create compound colliders
            var boxes = CreateMeshIntersectingBoxes(collidersGo).ToArray();

            //merge boxes to create bigger and less ones
            var mergedBoxes =
                mergeBoxesToReduceNumber
                    ? MergeBoxes(boxes.ToArray())
                    : boxes;

            foreach (var b in mergedBoxes)
            {
                var bc = (createChildGameObject ? collidersGo : go).AddComponent<BoxCollider>();
                bc.size = b.Size;
                bc.center = b.Center;
                if (physicsMaterialForColliders != null)
                    bc.material = physicsMaterialForColliders;
            }
            Debug.Log("NonConvexMeshCollider: " + mergedBoxes.Length + " box colliders created");

            //cleanup stuff not needed anymore on collider child obj
            DestroyImmediate(collidersGo.GetComponent<MeshFilter>());
            DestroyImmediate(collidersGo.GetComponent<MeshCollider>());
            DestroyImmediate(collidersGo.GetComponent<Rigidbody>());
            if (!createChildGameObject)
                DestroyImmediate(collidersGo);
            else if (collidersGo)
                collidersGo.layer = originalLayer;
        }
        finally
        {
            //reset original state of root object
            go.transform.parent = parentGo;
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;
            go.transform.localScale = localScale;
            go.layer = originalLayer;
            if (hadNonKinematicRigidbody)
                rbdy.isKinematic = false;
            if (hadRigidbodyWithGravityUse)
                rbdy.useGravity = true;
            DestroyImmediate(tempParent);
        }

        sw.Stop();
        if (outputTimeMeasurements)
            Debug.Log("Total duration: " + sw.Elapsed);
    }

    public class BoundingBox
    {
        public BoundingBox(params Interval[] intervalsXyz) : this(intervalsXyz[0], intervalsXyz[1], intervalsXyz[2])
        {

        }
        public BoundingBox(Interval intervalX, Interval intervalY, Interval intervalZ)
        {
            IntervalX = intervalX;
            IntervalY = intervalY;
            IntervalZ = intervalZ;
        }

        public Interval IntervalX { get; private set; }
        public Interval IntervalY { get; private set; }
        public Interval IntervalZ { get; private set; }

        public bool IntersectsRayToPositiveX(Vector3 origin)
        {
            var tx1 = IntervalX.Min - origin.x;
            var tx2 = IntervalX.Max - origin.x;

            var tmin = Math.Min(tx1, tx2);
            var tmax = Math.Max(tx1, tx2);

            tmin = Math.Max(tmin, 0);
            tmax = Math.Min(tmax, 0);

            return tmax >= tmin;
        }

        public bool Intersects(Ray r)
        {
            // r.dir is unit direction vector of ray
            var dirfracx = 1.0f / r.direction.x;
            var dirfracy = 1.0f / r.direction.y;
            var dirfracz = 1.0f / r.direction.z;
            // lb is the corner of AABB with minimal coordinates - left bottom, rt is maximal corner
            // r.org is origin of ray
            var t1 = (IntervalX.Min - r.origin.x) * dirfracx;
            var t2 = (IntervalX.Max - r.origin.x) * dirfracx;
            var t3 = (IntervalY.Min - r.origin.y) * dirfracy;
            var t4 = (IntervalY.Max - r.origin.y) * dirfracy;
            var t5 = (IntervalZ.Min - r.origin.z) * dirfracz;
            var t6 = (IntervalZ.Max - r.origin.z) * dirfracz;

            var tmin = Math.Max(Math.Max(Math.Min(t1, t2), Math.Min(t3, t4)), Math.Min(t5, t6));
            var tmax = Math.Min(Math.Min(Math.Max(t1, t2), Math.Max(t3, t4)), Math.Max(t5, t6));

            float t;

            // if tmax < 0, ray (line) is intersecting AABB, but whole AABB is behing us
            if (tmax < 0)
            {
                t = tmax;
                return false;
            }

            // if tmin > tmax, ray doesn't intersect AABB
            if (tmin > tmax)
            {
                t = tmax;
                return false;
            }

            t = tmin;
            return true;
        }

        public bool Intersects(BoundingBox other)
        {
            var result =
                IntervalX.Intersects(other.IntervalX) &&
                IntervalY.Intersects(other.IntervalY) &&
                IntervalZ.Intersects(other.IntervalZ);
            return result;
        }

        public override string ToString()
        {
            return string.Format("X: {0:N4}-{1:N4}, Y: {2:N4}-{3:N4}, Z: {4:N4}-{5:N4}",
                IntervalX.Min, IntervalX.Max,
                IntervalY.Min, IntervalY.Max,
                IntervalZ.Min, IntervalZ.Max);
        }
    }

    private Box[] MergeBoxes(Box[] boxes)
    {
        var sw = Stopwatch.StartNew();

        var mergeDirections = new[] {
            new Vector3Int(1,0,0),
            new Vector3Int(0,1,0),
            new Vector3Int(0,0,1),
            new Vector3Int(-1,0,0),
            new Vector3Int(0,-1,0),
            new Vector3Int(0,0,-1),
        };
        var foundSomethingToMerge = false;
        do
        {
            foreach (var mergeDirection in mergeDirections)
            {
                foundSomethingToMerge = false;
                foreach (var box in boxes)
                {
                    var merged = box.TryMerge(mergeDirection);
                    if (merged)
                        foundSomethingToMerge = true;
                }
                boxes = boxes.Select(b => b.Root).Distinct().ToArray();
            }
        } while (foundSomethingToMerge);
        var result = boxes.Select(b => b.Root).Distinct().ToArray();


        sw.Stop();
        if (outputTimeMeasurements)
            Debug.Log("Merged in " + sw.Elapsed);

        return result;
    }

    private static GameObject CreateColliderChildGameObject(GameObject go, MeshFilter meshFilter)
    {
        //ensure collider child gameobject exists
        var collidersTransform = go.transform.FindChild("Colliders");
        GameObject collidersGo;
        if (collidersTransform != null)
            collidersGo = collidersTransform.gameObject;
        else
        {
            collidersGo = new GameObject("Colliders");
            collidersGo.transform.parent = go.transform;
            collidersGo.transform.localRotation = Quaternion.Euler(Vector3.zero);
            collidersGo.transform.localPosition = Vector3.zero;
        }
        collidersGo.layer = go.layer;

        //reset collider child gameobject
        foreach (var bc in collidersGo.GetComponents<BoxCollider>())
            DestroyImmediate(bc);
        var mf = collidersGo.GetComponent<MeshFilter>();
        if (mf != null) DestroyImmediate(mf);
        var mc = collidersGo.GetComponent<MeshCollider>();
        if (mc != null) DestroyImmediate(mc);
        var rd = collidersGo.GetComponent<Rigidbody>();
        if (rd != null) DestroyImmediate(rd);
        rd = collidersGo.AddComponent<Rigidbody>();
        rd.isKinematic = true;
        rd.useGravity = false;

        //setup collider child gameobject
        mf = collidersGo.AddComponent<MeshFilter>();
        mf.sharedMesh = meshFilter.sharedMesh;
        //MakeMeshDoubleSided(mf.mesh);
        mc = collidersGo.AddComponent<MeshCollider>();
        mc.convex = false;
        return collidersGo;
    }

    private IEnumerable<Box> CreateMeshIntersectingBoxes(GameObject colliderGo)
    {
        var go = colliderGo.transform.parent.gameObject;
        var colliderLayer = colliderGo.layer;
        LayerMask colliderLayerMask = 1 << colliderLayer;
        var bounds = CalculateLocalBounds(go);
        var mesh = colliderGo.GetComponent<MeshFilter>().sharedMesh;
        var swTree = Stopwatch.StartNew();
        var tree = new SpatialBinaryTree(mesh, spatialTreeLevelDepth);
        swTree.Stop();
        if (outputTimeMeasurements)
            Debug.Log("SpatialTree Built in " + swTree.Elapsed);

        var boxes = new Box[boxesPerEdge, boxesPerEdge, boxesPerEdge];
        var boxColliderPositions = new bool[boxesPerEdge, boxesPerEdge, boxesPerEdge];
        var s = bounds.size / boxesPerEdge;
        var halfExtent = s / 2f;

        var directionsFromBoxCenterToCorners = new[]
        {
            new Vector3(1,1,1),
            new Vector3(1,1,-1),
            new Vector3(1,-1,1),
            new Vector3(1,-1,-1),
            new Vector3(-1,1,1),
            new Vector3(-1,1,-1),
            new Vector3(-1,-1,1),
            new Vector3(-1,-1,-1),
        };

        var pointInsideMeshCache = new Dictionary<Vector3, bool>();

        var sw = Stopwatch.StartNew();

        var colliders = new Collider[1000];
        for (var x = 0; x < boxesPerEdge; x++)
        {
            for (var y = 0; y < boxesPerEdge; y++)
            {
                for (var z = 0; z < boxesPerEdge; z++)
                {
                    var center = new Vector3(
                        bounds.center.x - bounds.size.x / 2 + s.x * x + halfExtent.x,
                        bounds.center.y - bounds.size.y / 2 + s.y * y + halfExtent.y,
                        bounds.center.z - bounds.size.z / 2 + s.z * z + halfExtent.z);

                    if (!avoidExceedingMesh)
                    {
                        if (avoidGapsInside)
                        {
                            var isInsideSurface = IsInsideMesh(center, tree, pointInsideMeshCache);
                            boxColliderPositions[x, y, z] = isInsideSurface;
                        }
                        else
                        {
                            var overlapsWithMeshSurface = Physics.OverlapBoxNonAlloc(center, halfExtent, colliders, Quaternion.identity, colliderLayerMask) > 0;
                            boxColliderPositions[x, y, z] = overlapsWithMeshSurface;
                        }
                        continue;
                    }

                    var allCornersInsideMesh =
                        (from d in directionsFromBoxCenterToCorners
                         select new Vector3(center.x + halfExtent.x * d.x, center.y + halfExtent.y * d.y, center.z + halfExtent.z * d.z))
                        .All(cornerPoint => IsInsideMesh(cornerPoint, tree, pointInsideMeshCache));
                    boxColliderPositions[x, y, z] = allCornersInsideMesh;
                }
            }
        }

        sw.Stop();
        if (outputTimeMeasurements)
            Debug.Log("Boxes analyzed in " + sw.Elapsed);

        for (var x = 0; x < boxesPerEdge; x++)
        {
            for (var y = 0; y < boxesPerEdge; y++)
            {
                for (var z = 0; z < boxesPerEdge; z++)
                {
                    if (!boxColliderPositions[x, y, z]) continue;
                    var center = new Vector3(
                        bounds.center.x - bounds.size.x / 2 + s.x * x + s.x / 2,
                        bounds.center.y - bounds.size.y / 2 + s.y * y + s.y / 2,
                        bounds.center.z - bounds.size.z / 2 + s.z * z + s.z / 2);
                    var b = new Box(boxes, center, s, new Vector3Int(x, y, z));
                    boxes[x, y, z] = b;
                    yield return b;
                }
            }
        }
    }


    private bool IsInsideMesh(Vector3 p, SpatialBinaryTree tree, Dictionary<Vector3, bool> pointInsideMeshCache)
    {
        bool isInsideMesh;
        if (pointInsideMeshCache.TryGetValue(Vector3.one, out isInsideMesh))
            return isInsideMesh;

        var r = new Ray(p, new Vector3(1, 0, 0));
        var intersectionCount = tree.GetTris(r).Count(t => t.Intersect(r));
        var isInside = intersectionCount % 2 != 0;

        pointInsideMeshCache[p] = isInside;
        return isInside;
    }

    private int GetFirstEmptyLayer()
    {
        for (int i = 8; i <= 31; i++) //user defined layers start with layer 8 and unity supports 31 layers
        {
            var layerN = LayerMask.LayerToName(i); //get the name of the layer
            if (layerN.Length == 0)
                return i;
        }
        throw new Exception("Didn't find unused layer for temporary assignment");
    }

    private static Bounds CalculateLocalBounds(GameObject go)
    {
        var bounds = new Bounds(go.transform.position, Vector3.zero);
        foreach (var renderer in go.GetComponentsInChildren<Renderer>())
            bounds.Encapsulate(renderer.bounds);
        var localCenter = bounds.center - go.transform.position;
        bounds.center = localCenter;
        return bounds;
    }

    public class Tri
    {
        private BoundingBox bounds;

        public Tri(Vector3 a, Vector3 b, Vector3 c)
        {
            A = a;
            B = b;
            C = c;
        }

        public Vector3 A { get; set; }
        public Vector3 B { get; set; }
        public Vector3 C { get; set; }

        public BoundingBox Bounds
        {
            get
            {
                if (bounds == null)
                {
                    var b = new BoundingBox(
                        Interval.From(A.x, B.x, C.x),
                        Interval.From(A.y, B.y, C.y),
                        Interval.From(A.z, B.z, C.z)
                    );
                    bounds = b;
                }
                return bounds;
            }
        }

        public bool Intersect(Ray ray)
        {
            // Vectors from p1 to p2/p3 (edges)
            //Find vectors for two edges sharing vertex/point p1
            var e1 = B - A;
            var e2 = C - A;

            // calculating determinant 
            var p = Vector3.Cross(ray.direction, e2);

            //Calculate determinat
            var det = Vector3.Dot(e1, p);

            //if determinant is near zero, ray lies in plane of triangle otherwise not
            if (det > -0.000001f && det < 0.000001f) return false;
            var invDet = 1.0f / det;

            //calculate distance from p1 to ray origin
            var t = ray.origin - A;

            //Calculate u parameter
            var u = Vector3.Dot(t, p) * invDet;

            //Check for ray hit
            if (u < 0 || u > 1) { return false; }

            //Prepare to test v parameter
            var q = Vector3.Cross(t, e1);

            //Calculate v parameter
            var v = Vector3.Dot(ray.direction, q) * invDet;

            //Check for ray hit
            if (v < 0 || u + v > 1) { return false; }

            if (Vector3.Dot(e2, q) * invDet > 0.000001f)
            {
                //ray does intersect
                return true;
            }

            // No hit at all
            return false;

        }


        /// <summary>
        /// Checks if the specified ray hits the trianglge descibed by p1, p2 and p3.
        /// Möller–Trumbore ray-triangle intersection algorithm implementation.
        /// </summary>
        /// <param name="p1">Vertex 1 of the triangle.</param>
        /// <param name="p2">Vertex 2 of the triangle.</param>
        /// <param name="p3">Vertex 3 of the triangle.</param>
        /// <param name="ray">The ray to test hit for.</param>
        /// <returns><c>true</c> when the ray hits the triangle, otherwise <c>false</c></returns>
        public static bool Intersect(Vector3 p1, Vector3 p2, Vector3 p3, Ray ray)
        {
            // Vectors from p1 to p2/p3 (edges)
            //Find vectors for two edges sharing vertex/point p1
            var e1 = p2 - p1;
            var e2 = p3 - p1;

            // calculating determinant 
            var p = Vector3.Cross(ray.direction, e2);

            //Calculate determinat
            var det = Vector3.Dot(e1, p);

            //if determinant is near zero, ray lies in plane of triangle otherwise not
            if (det > -0.000001f && det < 0.000001f) return false;
            var invDet = 1.0f / det;

            //calculate distance from p1 to ray origin
            var t = ray.origin - p1;

            //Calculate u parameter
            var u = Vector3.Dot(t, p) * invDet;

            //Check for ray hit
            if (u < 0 || u > 1) { return false; }

            //Prepare to test v parameter
            var q = Vector3.Cross(t, e1);

            //Calculate v parameter
            var v = Vector3.Dot(ray.direction, q) * invDet;

            //Check for ray hit
            if (v < 0 || u + v > 1) { return false; }

            if (Vector3.Dot(e2, q) * invDet > 0.000001f)
            {
                //ray does intersect
                return true;
            }

            // No hit at all
            return false;
        }
    }

    public class SpatialBinaryTree
    {
        public SpatialBinaryTree(Mesh m, int maxLevels)
        {
            var boundingBox = new BoundingBox(
                new Interval(m.vertices.Min(v => v.x), m.vertices.Max(v => v.x)),
                new Interval(m.vertices.Min(v => v.y), m.vertices.Max(v => v.y)),
                new Interval(m.vertices.Min(v => v.z), m.vertices.Max(v => v.z)));
            root = new SpatialBinaryTreeNode(0, maxLevels, boundingBox);

            var triCount = m.triangles.Length / 3;
            for (var i = 0; i < triCount; i++)
            {
                var v1 = m.vertices[m.triangles[i * 3]];
                var v2 = m.vertices[m.triangles[i * 3 + 1]];
                var v3 = m.vertices[m.triangles[i * 3 + 2]];
                var t = new Tri(v1, v2, v3);
                Add(t);
            }
        }

        public void Add(Tri t)
        {
            root.Add(t);
        }

        public IEnumerable<Tri> GetTris(Ray r)
        {
            return new HashSet<Tri>(root.GetTris(r));
        }

        private readonly SpatialBinaryTreeNode root;
    }

    public class SpatialBinaryTreeNode
    {
        private readonly int level;
        private readonly int maxLevels;
        private SpatialBinaryTreeNode childA;
        private SpatialBinaryTreeNode childB;
        private readonly List<Tri> tris;
        private readonly BoundingBox bounds;
        private readonly BoundingBox boundsChildA;
        private readonly BoundingBox boundsChildB;

        public SpatialBinaryTreeNode(int level, int maxLevels, BoundingBox bounds)
        {
            this.level = level;
            this.maxLevels = maxLevels;
            this.bounds = bounds;

            if (level >= maxLevels)
                tris = new List<Tri>();
            else
            {
                var lvlMod3 = level % 3;
                boundsChildA = new BoundingBox(
                    lvlMod3 == 0 ? bounds.IntervalX.LowerHalf : bounds.IntervalX,  //x
                    lvlMod3 == 1 ? bounds.IntervalY.LowerHalf : bounds.IntervalY,  //y
                    lvlMod3 == 2 ? bounds.IntervalZ.LowerHalf : bounds.IntervalZ); //z
                boundsChildB = new BoundingBox(
                    lvlMod3 == 0 ? bounds.IntervalX.UpperHalf : bounds.IntervalX,  //x
                    lvlMod3 == 1 ? bounds.IntervalY.UpperHalf : bounds.IntervalY,  //y
                    lvlMod3 == 2 ? bounds.IntervalZ.UpperHalf : bounds.IntervalZ); //z
            }
        }

        public void Add(Tri t)
        {
            if (tris != null)
            {
                tris.Add(t);
            }
            else
            {
                if (boundsChildA.Intersects(t.Bounds))
                {
                    if (childA == null)
                        childA = new SpatialBinaryTreeNode(level + 1, maxLevels, boundsChildA);
                    childA.Add(t);
                }
                if (boundsChildB.Intersects(t.Bounds))
                {
                    if (childB == null)
                        childB = new SpatialBinaryTreeNode(level + 1, maxLevels, boundsChildB);
                    childB.Add(t);
                }
            }
        }

        public IEnumerable<Tri> GetTris(Ray r)
        {
            if (!bounds.Intersects(r))
                yield break;

            if (tris != null)
            {
                foreach (var t in tris)
                    yield return t;
            }
            else
            {
                if (childA != null)
                    foreach (var t in childA.GetTris(r))
                        yield return t;

                if (childB != null)
                    foreach (var t in childB.GetTris(r))
                        yield return t;
            }
        }

        public override string ToString()
        {
            if (tris != null)
                return "Leaf node: " + tris.Count + " tris";
            return bounds.ToString();
        }
    }

    public class Interval
    {
        public Interval(float min, float max)
        {
            Min = min;
            Max = max;
            Center = (min + max) / 2f;
        }

        public float Min { get; set; }
        public float Max { get; set; }
        public float Center { get; private set; }
        public float Size { get { return Max - Min; } }

        public Interval LowerHalf { get { return new Interval(Min, Center); } }
        public Interval UpperHalf { get { return new Interval(Center, Max); } }

        public bool Contains(float v)
        {
            if (v < Min) return false;
            if (v >= Max) return false;
            return true;
        }

        public bool IsInLeftHalf(float v)
        {
            return v >= Min && v < Center;
        }

        public bool IsInRightHalf(float v)
        {
            return v > Center && v < Max;
        }

        public bool Intersects(Interval other)
        {
            return Min <= other.Max && other.Min <= Max;
        }

        public static Interval From(float a, float b, float c)
        {
            return new Interval(Math.Min(Math.Min(a, b), c), Math.Max(Math.Max(a, b), c));
        }

        public static Interval From(float a, float b)
        {
            return new Interval(Math.Min(a, b), Math.Max(a, b));
        }
    }

    public class Box
    {
        private readonly Box[,,] boxes;
        private readonly Vector3Int lastLevelGridPos;

        public Box(Box[,,] boxes, Vector3? center = null, Vector3? size = null, Vector3Int lastLevelGridPos = null)
        {
            this.boxes = boxes;
            this.lastLevelGridPos = lastLevelGridPos;
            this.center = center;
            this.size = size;
        }

        public Vector3 Center
        {
            get
            {
                if (center == null)
                {
                    if (Children == null) throw new Exception("Last level child box needs a center position");
                    var v = Vector3.zero;
                    foreach (var b in LastLevelBoxes)
                        v += b.Center;
                    v = v / LastLevelBoxes.Length;
                    center = v;
                }
                return center.Value;
            }
        }

        public Vector3 Size
        {
            get
            {
                if (size == null)
                {
                    if (Children == null) throw new Exception("Last level child box needs a size");
                    var singleBoxSize = LastLevelBoxes[0].Size;
                    size = new Vector3(GridSize.X * singleBoxSize.x, GridSize.Y * singleBoxSize.y, GridSize.Z * singleBoxSize.z);
                }
                return size.Value;
            }
        }

        private void MergeWith(Box other)
        {
            var b = new Box(boxes);
            foreach (var child in new[] { this, other })
                child.Parent = b;
            b.Children = new[] { this, other };
            Box temp = b;
        }

        public Box Parent { get; set; }
        public Box[] Children { get; set; }

        public IEnumerable<Box> Parents
        {
            get
            {
                var b = this;
                while (b.Parent != null)
                {
                    yield return b.Parent;
                    b = b.Parent;
                }
            }
        }

        public IEnumerable<Box> SelfAndParents
        {
            get
            {
                yield return this;
                foreach (var parent in Parents)
                    yield return parent;
            }
        }

        public Box Root
        {
            get
            {
                return Parent == null ? this : Parent.Root;
            }
        }

        public bool TryMerge(Vector3Int direction)
        {
            if (Parent != null) return false;
            foreach (var p in CoveredGridPositions)
            {
                var pos = new Vector3Int(p.X + direction.X, p.Y + direction.Y, p.Z + direction.Z);
                if (pos.X < 0 || pos.Y < 0 || pos.Z < 0)
                    continue;
                if (pos.X >= boxes.GetLength(0) || pos.Y >= boxes.GetLength(1) || pos.Z >= boxes.GetLength(2))
                    continue;
                var b = boxes[pos.X, pos.Y, pos.Z];
                if (b == null)
                    continue;
                b = b.Root;
                if (b == this)
                    continue;
                if (direction.X == 0 && b.GridSize.X != GridSize.X)
                    continue;
                if (direction.Y == 0 && b.GridSize.Y != GridSize.Y)
                    continue;
                if (direction.Z == 0 && b.GridSize.Z != GridSize.Z)
                    continue;
                if (direction.X == 0 && MinGridPos.X != b.MinGridPos.X)
                    continue;
                if (direction.Y == 0 && MinGridPos.Y != b.MinGridPos.Y)
                    continue;
                if (direction.Z == 0 && MinGridPos.Z != b.MinGridPos.Z)
                    continue;
                MergeWith(b);
                return true;
            }
            return false;
        }

        public IEnumerable<Box> ChildrenRecursive
        {
            get
            {
                if (Children == null) yield break;
                foreach (var c in Children)
                {
                    yield return c;
                    foreach (var cc in c.ChildrenRecursive)
                        yield return cc;
                }
            }
        }

        public IEnumerable<Box> SelfAndChildrenRecursive
        {
            get
            {
                yield return this;
                foreach (var c in ChildrenRecursive)
                    yield return c;
            }
        }

        private Box[] lastLevelBoxes;
        public Box[] LastLevelBoxes
        {
            get
            {
                if (lastLevelBoxes == null)
                    lastLevelBoxes = SelfAndChildrenRecursive.Where(c => c.Children == null).ToArray();
                return lastLevelBoxes;
            }
        }

        private IEnumerable<Vector3Int> CoveredGridPositions
        {
            get { return LastLevelBoxes.Select(c => c.lastLevelGridPos); }
        }

        private int MinGridPosX
        {
            get { return Children == null ? lastLevelGridPos.X : CoveredGridPositions.Min(p => p.X); }
        }

        private int MinGridPosY
        {
            get { return Children == null ? lastLevelGridPos.Y : CoveredGridPositions.Min(p => p.Y); }
        }

        private int MinGridPosZ
        {
            get { return Children == null ? lastLevelGridPos.Z : CoveredGridPositions.Min(p => p.Z); }
        }

        private int MaxGridPosX
        {
            get { return Children == null ? lastLevelGridPos.X : CoveredGridPositions.Max(p => p.X); }
        }

        private int MaxGridPosY
        {
            get { return Children == null ? lastLevelGridPos.Y : CoveredGridPositions.Max(p => p.Y); }
        }

        private int MaxGridPosZ
        {
            get { return Children == null ? lastLevelGridPos.Z : CoveredGridPositions.Max(p => p.Z); }
        }

        private Vector3Int minGridPos;
        private Vector3Int MinGridPos
        {
            get { return minGridPos ?? (minGridPos = new Vector3Int(MinGridPosX, MinGridPosY, MinGridPosZ)); }
        }

        private Vector3Int maxGridPos;
        private Vector3Int MaxGridPos
        {
            get { return maxGridPos ?? (maxGridPos = new Vector3Int(MaxGridPosX, MaxGridPosY, MaxGridPosZ)); }
        }

        private Vector3Int gridSize;
        private Vector3? center;
        private Vector3? size;

        private Vector3Int GridSize
        {
            get
            {
                if (gridSize == null)
                    gridSize = Children == null
                        ? Vector3Int.One
                        : new Vector3Int(
                            MaxGridPos.X - MinGridPos.X + 1,
                            MaxGridPos.Y - MinGridPos.Y + 1,
                            MaxGridPos.Z - MinGridPos.Z + 1);
                return gridSize;
            }
        }
    }

    public class Vector3Int
    {
        public Vector3Int(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
        public static readonly Vector3Int One = new Vector3Int(1, 1, 1);

        protected bool Equals(Vector3Int other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Vector3Int)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = X;
                hashCode = (hashCode * 397) ^ Y;
                hashCode = (hashCode * 397) ^ Z;
                return hashCode;
            }
        }

        public override string ToString()
        {
            return string.Format("X: {0}, Y: {1}, Z: {2}", X, Y, Z);
        }
    }

}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(NonConvexMeshCollider))]
public class NonConvexMeshColliderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var scripts = targets.OfType<NonConvexMeshCollider>();
        if (GUILayout.Button("Calculate"))
            foreach (var script in scripts)
                script.Calculate();
    }
}
#endif