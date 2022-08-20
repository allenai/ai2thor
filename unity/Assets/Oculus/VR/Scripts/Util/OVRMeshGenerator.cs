using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates mesh from a list of vertices.
/// </summary>
internal class OVRMeshGenerator
{
	/// <summary>
	/// Winding order for triangles vertices.
	/// </summary>
	public enum WindingOrderMode
	{
		Clockwise,
		CounterClockwise
	}

	/// <summary>
	/// Returns a mesh generated from a array of vertices.
	/// </summary>
	/// <param name="vertices">Source boundary vertices</param>
	/// <param name="requireTransform">If true, source vertices position will
	/// be transformed from OpenXR to Unity coordinate space.</param>
	/// <returns>Generate Unity mesh object.</returns>
	public static void GenerateMesh(Vector2[] vertices, Mesh mesh)
	{
		TransformVertices(vertices,
			out var verticesV3,
			out var uvCoords,
			out var normals);		

		var triangles = GenerateTrianglesFromBoundaryVertices(vertices);

		mesh.Clear();
		mesh.vertices = verticesV3;
		mesh.triangles = triangles;
		mesh.normals = normals;
		mesh.uv = uvCoords;
		mesh.RecalculateBounds();
	}

	/// <summary>
	/// Transform the position of vertices from OpenXR space to Unity space.
	/// Create normals and uv arrays from source vertices.
	/// </summary>
	/// <param name="vertices">Source vertices</param>
	/// <param name="verticesV3">Transformed vertices</param>
	/// <param name="uvCoords">UV coords</param>
	/// <param name="normals">Vertices normals</param>
	/// <param name="requireTransform">If true, transform vertices position from OpenXR to Unity space.</param>
	public static void TransformVertices(Vector2[] vertices, out Vector3[] verticesV3, out Vector2[] uvCoords,
		out Vector3[] normals)
	{
		uvCoords = new Vector2[vertices.Length];
		verticesV3 = new Vector3[vertices.Length];
		normals = new Vector3[vertices.Length];
		
		for (int i = 0; i < vertices.Length; i++)
		{
			verticesV3[i] = vertices[i];
			verticesV3[i].x *= -1;
			
			uvCoords[i] = verticesV3[i];
			
			normals[i] = verticesV3[i];
			normals[i].z = 1;
		}
	}

	/// <summary>
	/// This method takes care the triangulation process.
	/// </summary>
	/// <param name="vertices">List of input vertices</param>
	/// <returns>A list of triangles to use as a indices in a mesh.</returns>
	/// <exception cref="ArgumentNullException">
	/// Throws when vertices array is null.
	/// </exception>
	/// <exception cref="ArgumentException">
	/// Throws when invalid number of vertices are passed. Minimum required vertices are 3.
	/// </exception>
	public static int[] GenerateTrianglesFromBoundaryVertices(Vector2[] vertices)
	{

		if (vertices == null)
		{
			throw new ArgumentNullException();
		}

		if (vertices.Length < 3)
		{
			throw new ArgumentException("Vertices count cannot be less than 3.");
		}

		int totalTriangleCount = (vertices.Length - 2);
		int[] triangles = new int[totalTriangleCount * 3];
		
		List<int> indexList = new List<int>();
		for (int i = 0; i < vertices.Length; i++)
		{
			indexList.Add(i);
		}

		bool indexListChanged = true;

		// Find a valid triangle.
		// Checks:
		// 1. Connected edges do not form a co-linear or reflex angle.
		// 2. There's no vertices inside the selected triangle area.
		int triangleCount = 0;
		while (indexList.Count > 3)
		{
			if (!indexListChanged)
			{
				Debug.LogWarning("Infinite loop in vertices.");
				break;
			}

			indexListChanged = false;

			for (int i = 0; i < indexList.Count; i++)
			{
				int a = indexList[i];
				int b = Get(i - 1, indexList);
				int c = Get(i + 1, indexList);

				Vector2 va = vertices[a];
				Vector2 vb = vertices[b];
				Vector2 vc = vertices[c];

				Vector2 atob = vb - va;
				Vector2 atoc = vc - va;

				// reflex angle check
				if (Cross(atob, atoc) >= 0)
				{
					continue;
				}

				bool validTriangle = true;
				for (int j = 0; j < vertices.Length; j++)
				{
					if (j == a || j == b || j == c)
					{
						continue;
					}

					if (PointInTriangle(vertices[j], va, vb, vc))
					{
						validTriangle = false;
						break;
					}
				}

				// add indices to triangle list
				if (validTriangle)
				{
					triangles[triangleCount++] = c;
					triangles[triangleCount++] = a;
					triangles[triangleCount++] = b;

					indexList.RemoveAt(i);
					indexListChanged = true;
					break;
				}
			}
		}

		triangles[triangleCount++] = indexList[2];
		triangles[triangleCount++] = indexList[1];
		triangles[triangleCount++] = indexList[0];

		return triangles;
	}

	/// <summary>
	/// Detect the order of vertices.
	/// </summary>
	/// <remarks>
	/// SUM((x[n] - x[n-1]) * (y[n] + y[n-1])); n is total number of vertices.
	/// Positive sum refers to the clockwise winding order.
	/// </remarks>
	/// <param name="vertices">Source vertices</param>
	/// <returns>Clockwise or Counter-Clockwise vertices order.</returns>
	public static WindingOrderMode GetWindingOrder(Vector2[] vertices)
	{
		
		float total = 0;
		for(int i = 1; i < vertices.Length; i++)
		{
			total += ((vertices[i].x - vertices[i - 1].x) * (vertices[i].y + vertices[i - 1].y));
		}

		return total < 0 ? WindingOrderMode.CounterClockwise : WindingOrderMode.Clockwise;
	}

	/// <summary>
	/// Checks if point p is always on the right side of the vectors a, b, c
	/// </summary>
	private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
	{
		return Cross(b - a, p - a) < 0 &&
		       Cross(c - b, p - b) < 0 &&
		       Cross(a - c, p - c) < 0;
	}

	/// <summary>
	/// Cross product for 2d vectors.
	/// </summary>
	private static float Cross(Vector2 a, Vector2 b)
	{
		return a.x * b.y - a.y * b.x;
	}

	/// <summary>
	/// Get item in circular order from an array
	/// </summary>
	private static int Get(int index, List<int> array)
	{
		if (index >= array.Count)
		{
			return array[index % array.Count];
		}
		else if (index < 0)
		{
			return array[index % array.Count + array.Count];
		}
		else
		{
			return array[index];
		}
	}
}
