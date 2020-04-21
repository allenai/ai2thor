/*
* Copyright (c) <2018> Side Effects Software Inc.
* All rights reserved.
*
* Redistribution and use in source and binary forms, with or without
* modification, are permitted provided that the following conditions are met:
*
* 1. Redistributions of source code must retain the above copyright notice,
*    this list of conditions and the following disclaimer.
*
* 2. The name of Side Effects Software may not be used to endorse or
*    promote products derived from this software without specific prior
*    written permission.
*
* THIS SOFTWARE IS PROVIDED BY SIDE EFFECTS SOFTWARE "AS IS" AND ANY EXPRESS
* OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
* OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.  IN
* NO EVENT SHALL SIDE EFFECTS SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT,
* INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
* LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA,
* OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
* LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
* NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
* EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

#if (UNITY_EDITOR_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_LINUX)
#define HOUDINIENGINEUNITY_ENABLED
#endif

using System.Text;
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace HoudiniEngineUnity
{
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Typedefs (copy these from HEU_Common.cs)
	using HAPI_NodeId = System.Int32;
	using HAPI_PartId = System.Int32;

	/// <summary>
	/// Geometry-specific utility functions.
	/// </summary>
	public static class HEU_GeometryUtility
	{

		public static Vector2[] GeneratePerTriangle(Mesh meshSrc)
		{
#if UNITY_EDITOR
			return Unwrapping.GeneratePerTriangleUV(meshSrc);
#else
			Debug.LogWarning("GeneratePerTriangle is unavailable at runtime!");
			return null;
#endif
		}

		public static void GenerateSecondaryUVSet(Mesh meshsrc)
		{
#if UNITY_EDITOR
			UnwrapParam param;
			UnwrapParam.SetDefaults(out param);
			Unwrapping.GenerateSecondaryUVSet(meshsrc, param);
#else
			Debug.LogWarning("GenerateSecondaryUVSet is unavailable at runtime!");
#endif
		}


		/// <summary>
		/// Calculate the tangents for the given mesh.
		/// Does nothing if the mesh has no geometry, UVs, or normals.
		/// </summary>
		/// <param name="mesh">Source mesh to calculate tangents for.</param>
		public static void CalculateMeshTangents(Mesh mesh)
		{
			// Copy to local arrays
			int[] triangles = mesh.triangles;
			Vector3[] vertices = mesh.vertices;
			Vector2[] uv = mesh.uv;
			Vector3[] normals = mesh.normals;

			if (triangles == null || vertices == null || uv == null || normals == null 
				|| triangles.Length == 0 || vertices.Length == 0 || uv.Length == 0 || normals.Length == 0)
			{
				return;
			}

			int triangleCount = triangles.Length;
			int vertexCount = vertices.Length;

			Vector3[] tan1 = new Vector3[vertexCount];
			Vector3[] tan2 = new Vector3[vertexCount];
			Vector4[] tangents = new Vector4[vertexCount];

			for (long a = 0; a < triangleCount; a += 3)
			{
				long i1 = triangles[a + 0];
				long i2 = triangles[a + 1];
				long i3 = triangles[a + 2];

				Vector3 v1 = vertices[i1];
				Vector3 v2 = vertices[i2];
				Vector3 v3 = vertices[i3];

				Vector2 w1 = uv[i1];
				Vector2 w2 = uv[i2];
				Vector2 w3 = uv[i3];

				float x1 = v2.x - v1.x;
				float x2 = v3.x - v1.x;
				float y1 = v2.y - v1.y;
				float y2 = v3.y - v1.y;
				float z1 = v2.z - v1.z;
				float z2 = v3.z - v1.z;

				float s1 = w2.x - w1.x;
				float s2 = w3.x - w1.x;
				float t1 = w2.y - w1.y;
				float t2 = w3.y - w1.y;

				float div = s1 * t2 - s2 * t1;
				float r = div == 0.0f ? 0.0f : 1.0f / div;

				Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r,
											(t2 * y1 - t1 * y2) * r,
											(t2 * z1 - t1 * z2) * r);
				Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r,
											(s1 * y2 - s2 * y1) * r,
											(s1 * z2 - s2 * z1) * r);
				tan1[i1] += sdir;
				tan1[i2] += sdir;
				tan1[i3] += sdir;

				tan2[i1] += tdir;
				tan2[i2] += tdir;
				tan2[i3] += tdir;
			}

			for (long a = 0; a < vertexCount; ++a)
			{
				Vector3 n = normals[a];
				Vector3 t = tan1[a];

				Vector3.OrthoNormalize(ref n, ref t);
				tangents[a].x = t.x;
				tangents[a].y = t.y;
				tangents[a].z = t.z;

				tangents[a].w = (Vector3.Dot(Vector3.Cross(n, t), tan2[a]) < 0.0f) ? -1.0f : 1.0f;
			}

			mesh.tangents = tangents;
		}

		/// <summary>
		/// Generates a cube mesh using quad faces from given points, with vertex colours on selected and non-selected points.
		/// </summary>
		/// <param name="points">A cube will be created for each point in this list</param>
		/// <param name="selectedPtsFlag">Indices of selected points</param>
		/// <param name="defaultColor">Non-selected vertex color of cubes</param>
		/// <param name="selectedColor">Selected vertex color of cubes</param>
		/// <param name="size">Length of one side of cube</param>
		/// <returns>The generated mesh</returns>
		public static Mesh GenerateCubeMeshFromPoints(Vector3[] points, Color[] pointsColor, float size = 1f)
		{
			float halfSize = size * 0.5f;

			int totalPoints = points.Length;

			// Each cube face will get unique vertices due to splitting the normals
			int totalVertices = 24 * totalPoints;

			Vector3[] vertices = new Vector3[totalVertices];
			Color[] colors = new Color[totalVertices];
			Vector3[] normals = new Vector3[totalVertices];
			int[] indices = new int[totalVertices];

			int ptIndex = 0;
			int vertsPerPt = 24;

			foreach (Vector3 pt in points)
			{
				Vector3 v0 = new Vector3(pt.x - halfSize, pt.y + halfSize, pt.z + halfSize);
				Vector3 v1 = new Vector3(pt.x - halfSize, pt.y + halfSize, pt.z - halfSize);
				Vector3 v2 = new Vector3(pt.x + halfSize, pt.y + halfSize, pt.z - halfSize);
				Vector3 v3 = new Vector3(pt.x + halfSize, pt.y + halfSize, pt.z + halfSize);

				Vector3 v4 = new Vector3(pt.x - halfSize, pt.y - halfSize, pt.z + halfSize);
				Vector3 v5 = new Vector3(pt.x - halfSize, pt.y - halfSize, pt.z - halfSize);
				Vector3 v6 = new Vector3(pt.x + halfSize, pt.y - halfSize, pt.z - halfSize);
				Vector3 v7 = new Vector3(pt.x + halfSize, pt.y - halfSize, pt.z + halfSize);

				int vertIndex = ptIndex * vertsPerPt;

				// Top
				vertices[vertIndex + 0] = v0;
				vertices[vertIndex + 1] = v3;
				vertices[vertIndex + 2] = v2;
				vertices[vertIndex + 3] = v1;

				normals[vertIndex + 0] = Vector3.up;
				normals[vertIndex + 1] = Vector3.up;
				normals[vertIndex + 2] = Vector3.up;
				normals[vertIndex + 3] = Vector3.up;

				// Bottom
				vertices[vertIndex + 4] = v4;
				vertices[vertIndex + 5] = v5;
				vertices[vertIndex + 6] = v6;
				vertices[vertIndex + 7] = v7;

				normals[vertIndex + 4] = Vector3.down;
				normals[vertIndex + 5] = Vector3.down;
				normals[vertIndex + 6] = Vector3.down;
				normals[vertIndex + 7] = Vector3.down;

				// Front
				vertices[vertIndex + 8] = v0;
				vertices[vertIndex + 9] = v4;
				vertices[vertIndex + 10] = v7;
				vertices[vertIndex + 11] = v3;

				normals[vertIndex + 8] = Vector3.forward;
				normals[vertIndex + 9] = Vector3.forward;
				normals[vertIndex + 10] = Vector3.forward;
				normals[vertIndex + 11] = Vector3.forward;

				// Back
				vertices[vertIndex + 12] = v1;
				vertices[vertIndex + 13] = v2;
				vertices[vertIndex + 14] = v6;
				vertices[vertIndex + 15] = v5;

				normals[vertIndex + 12] = Vector3.back;
				normals[vertIndex + 13] = Vector3.back;
				normals[vertIndex + 14] = Vector3.back;
				normals[vertIndex + 15] = Vector3.back;

				// Left
				vertices[vertIndex + 16] = v0;
				vertices[vertIndex + 17] = v1;
				vertices[vertIndex + 18] = v5;
				vertices[vertIndex + 19] = v4;

				normals[vertIndex + 16] = Vector3.left;
				normals[vertIndex + 17] = Vector3.left;
				normals[vertIndex + 18] = Vector3.left;
				normals[vertIndex + 19] = Vector3.left;

				// Right
				vertices[vertIndex + 20] = v2;
				vertices[vertIndex + 21] = v3;
				vertices[vertIndex + 22] = v7;
				vertices[vertIndex + 23] = v6;

				normals[vertIndex + 20] = Vector3.right;
				normals[vertIndex + 21] = Vector3.right;
				normals[vertIndex + 22] = Vector3.right;
				normals[vertIndex + 23] = Vector3.right;

				// Vertex colors
				for (int i = 0; i < vertsPerPt; ++i)
				{
					colors[ptIndex * vertsPerPt + i] = pointsColor[ptIndex];
				}

				// Indices
				for (int i = 0; i < vertsPerPt; ++i)
				{
					indices[ptIndex * vertsPerPt + i] = ptIndex * vertsPerPt + i;
				}

				ptIndex++;
			}

			Mesh mesh = new Mesh();

			if (indices.Length > ushort.MaxValue)
			{
#if UNITY_2017_3_OR_NEWER
				mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
#else
				Debug.LogErrorFormat("Unable to generate mesh from points due to larger than supported geometry (> {0} vertices). Use Unity 2017.3+ for large geometry.", ushort.MaxValue);
				return mesh;
#endif
			}

			mesh.vertices = vertices;
			mesh.colors = colors;
			mesh.normals = normals;
			mesh.SetIndices(indices, MeshTopology.Quads, 0);

			return mesh;
		}

		/// <summary>
		/// Returns the output instance's name for given instance index. 
		/// The instance name convention is: PartName_Instance1
		/// User could override the prefix (PartName) with their own via given instancePrefixes array.
		/// </summary>
		/// <param name="partName"></param>
		/// <param name="userPrefix"></param>
		/// <param name="index"></param>
		/// <returns></returns>
		public static string GetInstanceOutputName(string partName, string[] userPrefix, int index)
		{
			string prefix = null;
			if (userPrefix == null || userPrefix.Length == 0)
			{
				prefix = partName;
			}
			else if (userPrefix.Length == 1)
			{
				prefix = userPrefix[0];
			}
			else if (index >= 0 && (index <= userPrefix.Length))
			{
				prefix = userPrefix[index - 1];
			}
			return prefix + HEU_Defines.HEU_INSTANCE + index;
		}
	}


}   // HoudiniEngineUnity