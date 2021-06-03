using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class VisibilityPointGenerator : MonoBehaviour
{
    public int voxelCountX = 3;
    public int voxelCountY = 3;
    public int voxelCountZ = 3;

    public class Triangle
    {
        public int index;
        public Vector3[] triangleVerts = new Vector3[4];
        public MeshCollider triangleCollider = new MeshCollider();
    }

    public class Voxel
    {
        public int index;
        public Vector3 min, max, center = new Vector3();
        public GameObject voxelCollider;
        public List<Triangle> trianglesInVoxel = new List<Triangle>();
        public Vector3 idealVisPoint, realVisPoint = Vector3.zero;
    }

    void Start()
    {
        // Grab key metadata from object mesh
        Vector3 centerOfBounds = transform.GetComponent<MeshFilter>().mesh.bounds.center;
        Vector3[] meshVerts = transform.GetComponent<MeshFilter>().mesh.vertices;
        int[] meshTriangleIndices = transform.GetComponent<MeshFilter>().mesh.triangles;

        // Create paper-thin tetrahedral mesh collider for each triangle of GameObject mesh, since convex mesh colliders are needed for Physics.ClosestPoint to work
        Triangle[] meshTriangles = new Triangle[meshTriangleIndices.Length / 3];
        int[] surrogateTriangleIndices = new int[] { 0, 1, 2, 0, 3, 1, 0, 2, 3, 1, 3, 2 };
        Vector3 triangleNormal = new Vector3();
        Mesh surrogateMesh = new Mesh();
        Color anyColor = new Color(1, 0, 1, 1);

        for (int i = 0; i < meshTriangleIndices.Length; i += 3)
        {
            meshTriangles[i / 3] = new Triangle();
            meshTriangles[i / 3].index = i / 3;
            // meshTriangles[i / 3].triangleVerts = new Vector3[3];
            // Define triangle for mesh (as tetrahedron, in order to be classifiable as a convex mesh collider)
            meshTriangles[i / 3].triangleVerts[0] = meshVerts[meshTriangleIndices[i]];
            meshTriangles[i / 3].triangleVerts[1] = meshVerts[meshTriangleIndices[i + 1]];
            meshTriangles[i / 3].triangleVerts[2] = meshVerts[meshTriangleIndices[i + 2]];
            // Each point vector is multiplied by 10 because Unity has trouble creating planes from three points so close together, and the normal stays the same anyway
            triangleNormal = new Plane(10 * meshTriangles[i / 3].triangleVerts[0], 10 * meshTriangles[i / 3].triangleVerts[1], 10 * meshTriangles[i / 3].triangleVerts[2]).normal;
            meshTriangles[i / 3].triangleVerts[3] = ((meshTriangles[i / 3].triangleVerts[0] + meshTriangles[i / 3].triangleVerts[1] + meshTriangles[i / 3].triangleVerts[2]) / 3) - (0.001f * triangleNormal);

            // Define mesh
            surrogateMesh = new Mesh();

            surrogateMesh.vertices = meshTriangles[i / 3].triangleVerts;
            surrogateMesh.triangles = surrogateTriangleIndices;

            GameObject gameObject = new GameObject("Mesh_" + (i / 3).ToString(), /*typeof(MeshFilter), typeof(MeshRenderer),*/ typeof(MeshCollider));
            gameObject.name = (i / 3).ToString();
            // gameObject.GetComponent<MeshFilter>().mesh = surrogateMesh;
            gameObject.GetComponent<MeshCollider>().sharedMesh = surrogateMesh;
            gameObject.GetComponent<MeshCollider>().convex = true;
            // gameObject.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Diffuse"));
            // gameObject.GetComponent<MeshRenderer>().material.color = anyColor;

            meshTriangles[i / 3].triangleCollider = gameObject.GetComponent<MeshCollider>();
        }

        // Organize triangles into voxelized sets of spatial overlap (of non-distinct contents, since triangles can overlap with multiple voxels)
        Vector3 voxelStartPoint = gameObject.transform.GetComponent<MeshFilter>().mesh.bounds.min;

        float voxelLengthX = gameObject.transform.GetComponent<MeshFilter>().mesh.bounds.size.x / voxelCountX;
        float voxelLengthY = gameObject.transform.GetComponent<MeshFilter>().mesh.bounds.size.y / voxelCountY;
        float voxelLengthZ = gameObject.transform.GetComponent<MeshFilter>().mesh.bounds.size.z / voxelCountZ;

        // Debug.Log("Voxel count is X: " + voxelCountX + ", Y: " + voxelCountY + ", Z: " + voxelCountZ + ".");
        // Debug.Log("Voxel length is X: " + voxelLengthX + ", Y: " + voxelLengthY + ", Z: " + voxelLengthZ + ".");

        Voxel[] voxels = new Voxel[voxelCountX * voxelCountY * voxelCountZ];

        List<Collider> intersectionPoints = new List<Collider>();

        int currentVoxelIndex = 0;
        List<Collider> currentTriangles = new List<Collider>();
        Vector3 currentClosestPointToIdeal = new Vector3();

        for (int i = 0; i < voxelCountX; i++)
        {
            for (int j = 0; j < voxelCountY; j++)
            {
                for (int k = 0; k < voxelCountZ; k++)
                {
                    // Create voxel metadata
                    voxels[currentVoxelIndex] = new Voxel();
                    voxels[currentVoxelIndex].index = currentVoxelIndex;
                    voxels[currentVoxelIndex].min = new Vector3(voxelStartPoint.x + i * voxelLengthX, voxelStartPoint.y + j * voxelLengthY, voxelStartPoint.z + k * voxelLengthZ);
                    voxels[currentVoxelIndex].max = new Vector3(voxels[currentVoxelIndex].min.x + voxelLengthX, voxels[currentVoxelIndex].min.y + voxelLengthY, voxels[currentVoxelIndex].min.z + voxelLengthZ);
                    voxels[currentVoxelIndex].center = (voxels[currentVoxelIndex].max + voxels[currentVoxelIndex].min) / 2;

                    // Find intersection point of raycast from bounds-center to midpoint against back of voxel (this will be the ideal vispoint location)
                    voxels[currentVoxelIndex].idealVisPoint = FindIdealVoxelVisPoint(voxels[currentVoxelIndex], centerOfBounds);

                    // if (voxels[currentVoxelIndex].index == 13) { Debug.Log("Min and max are " + voxels[currentVoxelIndex].min + " and " + voxels[currentVoxelIndex].max + ". Center is (" + voxels[currentVoxelIndex].center.x + "," + voxels[currentVoxelIndex].center.y + ", " + voxels[currentVoxelIndex].center.z + ")."); }

                    // Find all triangles whose borders overlap with voxel-planes

                    // if (voxels[currentVoxelIndex].index == 9)
                    // {
                        anyColor = UnityEngine.Random.ColorHSV();
                        foreach (Triangle currentTriangle in meshTriangles)
                        {
                            if (DoesTriangleOverlapWithVoxel(currentTriangle, voxels[currentVoxelIndex]))
                            {
                                voxels[currentVoxelIndex].trianglesInVoxel.Add(currentTriangle);
                                // Add triangles to voxel's triangle-index
                                // currentTriangle.triangleCollider.transform.GetComponent<MeshRenderer>().material.color = anyColor;
                            }
                        }
                    // }

                    // Find closest point among all triangles in voxel to ideal vispoint
                    
                    for (int l = 0; l < voxels[currentVoxelIndex].trianglesInVoxel.Count; l++)
                    {
                        currentClosestPointToIdeal = Physics.ClosestPoint(voxels[currentVoxelIndex].idealVisPoint, voxels[currentVoxelIndex].trianglesInVoxel[l].triangleCollider, Vector3.zero, Quaternion.identity);

                        if ((voxels[currentVoxelIndex].idealVisPoint - currentClosestPointToIdeal).sqrMagnitude < (voxels[currentVoxelIndex].idealVisPoint - voxels[currentVoxelIndex].realVisPoint).sqrMagnitude)
                        {
                            voxels[currentVoxelIndex].realVisPoint = currentClosestPointToIdeal;
                        }
                    }

                    currentVoxelIndex++;
                }
            }
        }

        foreach (Voxel voxel in voxels)
        {
            /*
            GameObject cube0 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube0.name = "Cube_" + voxel.index + "_center";
            cube0.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            cube0.GetComponent<Renderer>().material.color = new Color(1.0f, 0, 1.0f, 1.0f);
            // Debug.Log("Voxel " + voxel.center + " has center of " + (voxel.max + voxel.min) / 2 + ".");
            cube0.transform.position = transform.TransformPoint((voxel.max + voxel.min) / 2);

            GameObject cube2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube2.name = "Cube_" + voxel.index + "_idealVisPoint";
            cube2.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            cube2.GetComponent<Renderer>().material.color = new Color(1.0f, 0, 0, 1.0f);
            cube2.transform.position = transform.TransformPoint(voxel.idealVisPoint);
            */

            GameObject cube1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube1.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
            cube1.GetComponent<Renderer>().material.color = new Color(1.0f, 1.0f, 0, 1.0f);
            cube1.transform.position = transform.TransformPoint(voxel.realVisPoint);
            
        }

        // Generate sphere at center of mesh

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
        sphere.GetComponent<Renderer>().material.color = new Color(1.0f, 1.0f, 0, 1.0f);
        sphere.transform.position = transform.TransformPoint(centerOfBounds);
    }

    Vector3 AverageVerts(Vector3[] meshVerts)
    {
        Vector3 averagePosition = Vector3.zero;

        for (int i = 0; i < meshVerts.Length; i++)
        {
            averagePosition += meshVerts[i];
        }

        averagePosition /= meshVerts.Length;
        return averagePosition;
    }

    Vector3 FindIdealVoxelVisPoint(Voxel voxel, Vector3 origin)
    {
        Vector3 voxelExtremePoint = Vector3.zero;
        Ray centerCast = new Ray();
        float planeIntersectionMag = 0;
        Plane[] voxelFarPlanes = new Plane[3];
        // Plane closestFarPlane = new Plane();
        Vector3 rayPlaneIntersection = Vector3.zero;

        // Find furthest point in voxel from origin
        voxelExtremePoint.x = voxel.max.x;
        if (Math.Abs(voxel.min.x - origin.x) > Math.Abs(voxel.max.x - origin.x))
        {
            voxelExtremePoint.x = voxel.min.x;
        }

        voxelExtremePoint.y = voxel.max.y;
        if (Math.Abs(voxel.min.y - origin.y) > Math.Abs(voxel.max.y - origin.y))
        {
            voxelExtremePoint.y = voxel.min.y;
        }

        voxelExtremePoint.z = voxel.max.z;
        if (Math.Abs(voxel.min.z - origin.z) > Math.Abs(voxel.max.z - origin.z))
        {
            voxelExtremePoint.z = voxel.min.z;
        }

        // if (voxel.index == 9) { Debug.Log("Extreme point for voxel " + voxel.index + " is (" + voxelExtremePoint.x + ", " + voxelExtremePoint.y + ", " + voxelExtremePoint.z + "."); }

        // Define the three voxel planes furthest from origin
        voxelFarPlanes[0] = new Plane(new Vector3(voxelExtremePoint.x, 0, 0), voxelExtremePoint);
        voxelFarPlanes[1] = new Plane(new Vector3(0, voxelExtremePoint.y, 0), voxelExtremePoint);
        voxelFarPlanes[2] = new Plane(new Vector3(0, 0, voxelExtremePoint.z), voxelExtremePoint);

        // Cast a ray from the origin, through the voxel center, to each far plane
        centerCast = new Ray(origin, voxel.center - origin);

        foreach (Plane currentPlane in voxelFarPlanes)
        {
            // if (voxel.index == 9) { Debug.Log("Now testing if " + currentPlane + " passes through cube_" + voxel.index + "'s vector. Verdict: " + currentPlane.Raycast(centerCast, out float whatever)); }

            if (currentPlane.GetDistanceToPoint(voxel.center) != 0 && currentPlane.Raycast(centerCast, out planeIntersectionMag))
            {
                // if (voxel.index == 9) { Debug.Log("Intersection distance of cube_" + voxel.index + " to " + currentPlane + " is presently " + planeIntersectionMag); }

                if (rayPlaneIntersection == Vector3.zero || planeIntersectionMag < (rayPlaneIntersection - origin).magnitude)
                {
                    // if (voxel.index == 9) { Debug.Log("Plane " + currentPlane + " has a plane intersectionMag of " + planeIntersectionMag + ", which, if you're reading this, must be less than " + (rayPlaneIntersection - origin).magnitude + "."); }
                    rayPlaneIntersection = centerCast.GetPoint(planeIntersectionMag);
                    // if (voxel.index == 9) { Debug.Log("Closest intersection plane of cube_" + voxel.index + " is plane " + currentPlane + "."); }
                }
            }
        }

        return rayPlaneIntersection;
    }

    bool DoesTriangleOverlapWithVoxel(Triangle triangle, Voxel voxel/*, float voxelLengthX, float voxelLengthY, float voxelLengthZ*/)
    {
        // CASE 1: A POINT OF THE TRIANGLE OVERLAPS WITH THE VOXEL
        foreach (Vector3 trianglePoint in triangle.triangleVerts)
        {
            if (IsPointInsideVoxel(trianglePoint, voxel) == true)
            {
                return true;
            }
        }

        // CASE 2: AN EDGE OF THE TRIANGLE OVERLAPS WITH THE VOXEL
        Ray[] triangleLines = new Ray[3];
        triangleLines[0] = new Ray(triangle.triangleVerts[0], triangle.triangleVerts[1] - triangle.triangleVerts[0]);
        triangleLines[1] = new Ray(triangle.triangleVerts[1], triangle.triangleVerts[2] - triangle.triangleVerts[1]);
        triangleLines[2] = new Ray(triangle.triangleVerts[2], triangle.triangleVerts[0] - triangle.triangleVerts[2]);

        // This needs to be created because, unfortunately, the ray direction is normalized, but its magnitude does need to be retained
        float[] triangleLineMagnitudes = new float[3];
        triangleLineMagnitudes[0] = (triangle.triangleVerts[1] - triangle.triangleVerts[0]).magnitude;
        triangleLineMagnitudes[1] = (triangle.triangleVerts[2] - triangle.triangleVerts[1]).magnitude;
        triangleLineMagnitudes[2] = (triangle.triangleVerts[0] - triangle.triangleVerts[2]).magnitude;

        Plane[] voxelPlanes = new Plane[6];
        voxelPlanes[0] = new Plane(new Vector3(voxel.min.x, 0, 0), new Vector3(voxel.min.x, 0, 0));
        voxelPlanes[1] = new Plane(new Vector3(0, voxel.min.y, 0), new Vector3(0, voxel.min.y, 0));
        voxelPlanes[2] = new Plane(new Vector3(0, 0, voxel.min.z), new Vector3(0, 0, voxel.min.z));
        voxelPlanes[3] = new Plane(new Vector3(voxel.max.x, 0, 0), new Vector3(voxel.max.x, 0, 0));
        voxelPlanes[4] = new Plane(new Vector3(0, voxel.max.y, 0), new Vector3(0, voxel.max.y, 0));
        voxelPlanes[5] = new Plane(new Vector3(0, 0, voxel.max.z), new Vector3(0, 0, voxel.max.z));

        // Vector3[] linePlaneIntersections = new Vector3[18];
        // Debug.Log("Min and max are " + voxel.min + " and " + voxel.max + ".");

        for (int i = 0; i < triangleLines.Length; i++)
        {
            // Debug.Log("Now testing ray " + i + ", which is " + triangleLines[i] + ".");

            for (int j = 0; j < voxelPlanes.Length; j++)
            {
                // Debug.Log("Now testing plane " + j + ", which is " + voxelPlanes[j] + ".");

                if (voxelPlanes[j].Raycast(triangleLines[i], out float enter) == true)
                {
                    // DrawLine(triangle, triangle.triangleVerts[0], triangle.triangleVerts[1] + 1f * (triangle.triangleVerts[1] - triangle.triangleVerts[0]), new Color(0, 0, 0), new Color(0, 0, 1));
                    // Debug.Log("Is " + triangleLines[0].direction.magnitude + " equal to " + (triangle.triangleVerts[1] - triangle.triangleVerts[0]).magnitude + ", or does Unity normalize it somehow?");
                    // Debug.Log("Intersection point of triangle " + triangle.index + " with " + voxelPlanes[j] + " is " + triangleLines[i].GetPoint(enter) + ", and its magnitude from the starting point is " + enter + ", which should be compared to " + triangleLineMagnitudes[i] + ".");
                    // DrawLine(triangle.triangleVerts[0], 1.1f * triangle.triangleVerts[1], new Color(0, 0.5f, 1, 1));
                    if (enter <= triangleLineMagnitudes[i] && IsPointInsideVoxel(triangleLines[i].GetPoint(enter), voxel) == true)
                    {
                        return true;
                    }
                }
            }
        }

        // CASE 3: PART OF THE FACE OF THE TRIANGLE OVERLAPS WITH THE VOXEL
        // Very tricky, and edge-casey enough to not be necessary right now

        // CASE 4: TRIANGLE DOES NOT OVERLAP WITH THE VOXEL
        return false;
    }

    bool IsPointInsideVoxel(Vector3 point, Voxel voxel)
    {
        if (voxel.min.x <= point.x && voxel.min.y <= point.y && voxel.min.z <= point.z)
        {
            if (point.x <= voxel.max.x && point.y <= voxel.max.y && point.z <= voxel.max.z)
            {
                // Debug.Log("Triangle " + triangle.index + " DOES overlap with (" + voxel.center.x + ", " + voxel.center.y + ", " + voxel.center.z + "), (" + voxelLengthX + ", " + voxelLengthY + ", " + voxelLengthZ + ").");
                return true;
            }
        }

        return false;
    }

    /*
    void DrawLine(Triangle triangle, Vector3 start, Vector3 end, Color startColor, Color endColor)
    {
        // GameObject myLine = new GameObject();
        // myLine.transform.position = start;
        GameObject myLine = GameObject.Find(triangle.index.ToString());
        myLine.AddComponent<LineRenderer>();
        LineRenderer lr = myLine.GetComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Particles/Standard Unlit"));
        lr.SetColors(startColor, endColor);
        lr.SetWidth(0.0015f, 0.0015f);
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }
    */
}