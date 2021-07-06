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
        public List<Collider> trianglesInVoxel = new List<Collider>();
        public Vector3 idealVisPoint, realVisPoint = Vector3.zero;
    }

    [ContextMenu("Regenerate VisibilityPoints")]
    public void GenerateVisibilityPoints() {
        // Grab key metadata from object mesh
        Vector3 centerOfBounds = transform.GetComponent<MeshFilter>().sharedMesh.bounds.center;
        Vector3[] meshVerts = transform.GetComponent<MeshFilter>().sharedMesh.vertices;
        int[] meshTriangleIndices = transform.GetComponent<MeshFilter>().sharedMesh.triangles;

        // Create mesh collider for each face, for overlap calculations
        Triangle[] meshTriangles = new Triangle[meshTriangleIndices.Length / 3];
        int[] surrogateTriangleIndices = new int[] { 0, 1, 2, 0, 3, 1, 0, 2, 3, 1, 3, 2 };
        Vector3 triangleNormal = new Vector3();
        Mesh surrogateMesh = new Mesh();
        Color anyColor = new Color(1, 0, 1, 1);

        for (int i = 0; i < meshTriangleIndices.Length; i += 3) {
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

            GameObject gameObject = new GameObject("Mesh_" + (i / 3).ToString(), typeof(MeshCollider));
            gameObject.layer = 12;
            gameObject.GetComponent<MeshCollider>().sharedMesh = surrogateMesh;
            gameObject.GetComponent<MeshCollider>().convex = true;
            // gameObject.GetComponent<MeshRenderer>().material = new Material(Shader.Find("Diffuse"));
            // gameObject.GetComponent<MeshRenderer>().material.color = anyColor;

            meshTriangles[i / 3].triangleCollider = gameObject.GetComponent<MeshCollider>();
        }

        // Organize triangles into voxelized sets of spatial overlap (of non-distinct contents, since triangles can overlap with multiple voxels)
        Vector3 voxelStartPoint = gameObject.transform.GetComponent<MeshFilter>().sharedMesh.bounds.min;

        float voxelLengthX = gameObject.transform.GetComponent<MeshFilter>().sharedMesh.bounds.size.x / voxelCountX;
        float voxelLengthY = gameObject.transform.GetComponent<MeshFilter>().sharedMesh.bounds.size.y / voxelCountY;
        float voxelLengthZ = gameObject.transform.GetComponent<MeshFilter>().sharedMesh.bounds.size.z / voxelCountZ;

        // Debug.Log("Voxel count is X: " + voxelCountX + ", Y: " + voxelCountY + ", Z: " + voxelCountZ + ".");
        // Debug.Log("Voxel length is X: " + voxelLengthX + ", Y: " + voxelLengthY + ", Z: " + voxelLengthZ + ".");

        Voxel[] voxels = new Voxel[voxelCountX * voxelCountY * voxelCountZ];

        //List<Collider> intersectionPoints = new List<Collider>();

        int currentVoxelIndex = 0;
        Vector3 currentClosestPointToIdeal = new Vector3();

        for (int i = 0; i < voxelCountX; i++) {
            for (int j = 0; j < voxelCountY; j++) {
                for (int k = 0; k < voxelCountZ; k++) {
                    // Create voxel metadata
                    voxels[currentVoxelIndex] = new Voxel();
                    voxels[currentVoxelIndex].index = currentVoxelIndex;
                    voxels[currentVoxelIndex].min = new Vector3(voxelStartPoint.x + i * voxelLengthX, voxelStartPoint.y + j * voxelLengthY, voxelStartPoint.z + k * voxelLengthZ);
                    voxels[currentVoxelIndex].max = new Vector3(voxels[currentVoxelIndex].min.x + voxelLengthX, voxels[currentVoxelIndex].min.y + voxelLengthY, voxels[currentVoxelIndex].min.z + voxelLengthZ);
                    voxels[currentVoxelIndex].center = (voxels[currentVoxelIndex].max + voxels[currentVoxelIndex].min) / 2;

                    // Find intersection point of raycast from bounds-center to midpoint against back of voxel (this will be the ideal vispoint location)
                    voxels[currentVoxelIndex].idealVisPoint = FindIdealVoxelVisPoint(voxels[currentVoxelIndex], centerOfBounds);

                    // if (voxels[currentVoxelIndex].index == 13) { Debug.Log("Min and max are " + voxels[currentVoxelIndex].min + " and " + voxels[currentVoxelIndex].max + ". Center is (" + voxels[currentVoxelIndex].center.x + "," + voxels[currentVoxelIndex].center.y + ", " + voxels[currentVoxelIndex].center.z + ")."); }

                    // Find all triangles that overlap with voxel
                    voxels[currentVoxelIndex].trianglesInVoxel = Physics.OverlapBox(voxels[currentVoxelIndex].center, new Vector3(voxelLengthX / 2, voxelLengthY / 2, voxelLengthZ / 2), Quaternion.identity, 1 << 12).ToList();

                    // Find closest point among all triangles in voxels to ideal vispoint
                    for (int l = 0; l < voxels[currentVoxelIndex].trianglesInVoxel.Count; l++) {
                        currentClosestPointToIdeal = voxels[currentVoxelIndex].trianglesInVoxel[l].ClosestPoint(voxels[currentVoxelIndex].idealVisPoint);

                        if ((voxels[currentVoxelIndex].idealVisPoint - currentClosestPointToIdeal).sqrMagnitude < (voxels[currentVoxelIndex].idealVisPoint - voxels[currentVoxelIndex].realVisPoint).sqrMagnitude) {
                            voxels[currentVoxelIndex].realVisPoint = currentClosestPointToIdeal;
                        }
                    }

                    currentVoxelIndex++;
                }
            }
        }

        // Delete old VisPoints from SimObject that were created by this SimObject

        // Find top of SimObject
        Transform simObjMaster = transform;
        while (!simObjMaster.GetComponent<SimObjPhysics>()) {
            simObjMaster = simObjMaster.parent;
        }

        // Reset VisibilityPoint array on SimObj
        simObjMaster.GetComponent<SimObjPhysics>().VisibilityPoints = null;

        // Remove relevant VisibilityPoints from SimObject's group
        GameObject[] currentVisPoints = new GameObject[simObjMaster.Find("VisibilityPoints").childCount];
        for (int i = 0; i < currentVisPoints.Length; i++) {
            currentVisPoints[i] = simObjMaster.Find("VisibilityPoints").GetChild(i).gameObject;
        }

        foreach (GameObject visPoint in currentVisPoints) {
            if (visPoint.name.Contains(transform.parent.name) || visPoint.name == "vPoint") {
                DestroyImmediate(visPoint);
            }
        }

        // Generate new VisPoint at every relevant voxel
        foreach (Voxel voxel in voxels) {
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

            if (voxel.realVisPoint != Vector3.zero) {
                GameObject visPoint = new GameObject(transform.parent.name + "_vPoint");
                //cube1.transform.localScale = new Vector3(0.03f, 0.03f, 0.03f);
                //cube1.GetComponent<Renderer>().sharedMaterial.color = new Color(1.0f, 1.0f, 0, 1.0f);
                visPoint.transform.position = transform.TransformPoint(voxel.realVisPoint);
                visPoint.transform.parent = simObjMaster.Find("VisibilityPoints");
                simObjMaster.GetComponent<SimObjPhysics>().ContextSetUpVisibilityPoints();
            }


            //Delete surrogate mesh collider gameobjects
            foreach (GameObject gameObject in FindObjectsOfType(typeof(GameObject))) {
                if (gameObject.name.Contains("Mesh_")) {
                    DestroyImmediate(gameObject);
                }
            }
        }
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

        //if (voxel.index == 9) { Debug.Log("Extreme point for voxel " + voxel.index + " is (" + voxelExtremePoint.x + ", " + voxelExtremePoint.y + ", " + voxelExtremePoint.z + "."); }

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