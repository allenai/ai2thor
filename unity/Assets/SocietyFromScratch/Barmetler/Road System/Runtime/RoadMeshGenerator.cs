using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Barmetler.RoadSystem
{
	[RequireComponent(typeof(Road)), RequireComponent(typeof(MeshFilter))]
	public class RoadMeshGenerator : MonoBehaviour
	{
		[System.Serializable]
		public class RoadMeshSettings
		{
			[Tooltip("Orientation of the Source Mesh")]
			public MeshConversion.MeshOrientation SourceOrientation = MeshConversion.MeshOrientation.Presets["BLENDER"];
			[Tooltip("By how much to displace uvs every time the mesh tiles")]
			public Vector2 uvOffset = Vector2.up;
		}
		[Tooltip("Settings regarding mesh generation")]
		public RoadMeshSettings settings;

		public bool AutoGenerate
		{
			get => autoGenerate;
			set
			{
				if (value)
					GenerateRoadMesh();
				autoGenerate = value;
			}
		}
		[SerializeField, HideInInspector]
		bool autoGenerate;

		[Tooltip("Drag the model to be used for mesh generation into this slot")]
		public MeshFilter SourceMesh;

		public bool Valid { private set; get; }

		private Road road;
		private MeshFilter mf;

		private void OnValidate()
		{
			road = GetComponent<Road>();
			mf = GetComponent<MeshFilter>();
		}

		/// <summary>
		/// Generate the mesh based on the curve described in the Road component.
		/// </summary>
		public void GenerateRoadMesh()
		{
			OnValidate();

			if (!road) road = GetComponent<Road>();
			if (!road) return;
			if (!SourceMesh) return;

			float stepSize = 1;

			var points = road.GetEvenlySpacedPoints(stepSize, 1);

			Mesh oldMesh = MeshConversion.CopyMesh(SourceMesh.sharedMesh);
			MeshConversion.TransformMesh(oldMesh, settings.SourceOrientation);
			Mesh newMesh = new Mesh();

			float meshLength = oldMesh.bounds.size.z;
			{
				float meshOffset = -oldMesh.bounds.min.z;
				oldMesh.SetVertices(oldMesh.vertices.Select(v => v + meshOffset * Vector3.forward).ToArray());
			}

			// The last point is repositioned to the end of the bezier
			float bezierLength = stepSize * (points.Length - 2) +
				(points[points.Length - 2].position - points[points.Length - 1].position).magnitude;

			int completeCopies = Mathf.FloorToInt(bezierLength / meshLength);

			int submeshCount = oldMesh.subMeshCount;

			var oldVertices = new List<Vector3>();
			oldMesh.GetVertices(oldVertices);
			var oldIndices = Enumerable.Range(0, submeshCount).Select(i => new List<int>(oldMesh.GetIndices(i))).ToArray();
			var oldUVs = Enumerable.Range(0, 8)
				.Select(channel =>
				{
					var x = new List<Vector2>();
					oldMesh.GetUVs(channel, x);
					return x;
				})
				.ToArray();

			var newVertices = new List<Vector3>();
			var newIndices = Enumerable.Range(0, submeshCount).Select(_ => new List<int>()).ToArray();
			var newUVs = Enumerable.Range(0, 8).Select(_ => new List<Vector2>()).ToArray();

			int vertexCount = oldVertices.Count;
			int[] indexCounts = oldIndices.Select(e => e.Count).ToArray();

			for (int z = 0; z < completeCopies; ++z)
			{
				float yOffset = z * meshLength;

				for (int v = 0; v < vertexCount; ++v)
				{
					Vector3 pos = oldVertices[v] + Vector3.forward * (yOffset);
					// transform from blender to unity coordinate system
					pos = new Vector3(pos.x, pos.y, pos.z);

					newVertices.Add(pos);
				}

				for (int submesh = 0; submesh < submeshCount; ++submesh)
				{
					for (int i = 0; i < indexCounts[submesh] / 3; ++i)
					{
						// transform from blender to unity coordinate system
						newIndices[submesh].Add(oldIndices[submesh][3 * i] + z * vertexCount);
						newIndices[submesh].Add(oldIndices[submesh][3 * i + 1] + z * vertexCount);
						newIndices[submesh].Add(oldIndices[submesh][3 * i + 2] + z * vertexCount);
					}
				}

				for (int channel = 0; channel < 8; ++channel)
					for (int uv = 0; uv < oldUVs[channel].Count; ++uv)
						newUVs[channel].Add(oldUVs[channel][uv] + Vector2.up * settings.uvOffset * z);
			}

			float remainder = bezierLength - completeCopies * meshLength;
			var remainderVertices = oldVertices.ToList();
			var remainderIndices = oldIndices.Select(e => e.ToList()).ToArray();
			var remainderUVs = oldUVs.Select(e => e.ToList()).ToArray();
			for (int i = 0; i < submeshCount; ++i)
				ClipMeshZ(ref remainderVertices, ref remainderIndices[i], ref remainderUVs, remainder);

			remainderVertices = remainderVertices.Select(p =>
			{
				Vector3 pos = p + Vector3.forward * (meshLength * completeCopies);
				pos = new Vector3(pos.x, pos.y, pos.z);
				return pos;
			}).ToList();

			remainderIndices = remainderIndices.Select(e => e.Select(i => i + newVertices.Count).ToList()).ToArray();

			remainderUVs = remainderUVs.Select(e => e.Select(uv => uv + settings.uvOffset * completeCopies).ToList()).ToArray();

			newVertices.AddRange(remainderVertices);
			for (int i = 0; i < submeshCount; ++i)
				newIndices[i].AddRange(remainderIndices[i]);
			for (int i = 0; i < 8; ++i)
				newUVs[i].AddRange(remainderUVs[i]);

			// bend along bezier
			for (int v = 0; v < newVertices.Count; ++v)
			{
				Vector3 pos = newVertices[v];

				int pointIndex = Mathf.Clamp(Mathf.FloorToInt(pos.z / stepSize), 0, points.Length - 2);
				float weight = pos.z / stepSize - pointIndex;
				if (pointIndex == points.Length - 2)
				{
					weight = (pos.z - stepSize * pointIndex) /
						(points[points.Length - 1].position - points[points.Length - 2].position).magnitude;
				}
				Vector3 centerPos;
				Vector3 forward;
				Vector3 normal;
				if (pointIndex < points.Length - 1)
				{
					centerPos = Vector3.Lerp(points[pointIndex].position, points[pointIndex + 1].position, weight);
					forward = Vector3.Lerp(points[pointIndex].forward, points[pointIndex + 1].forward, weight).normalized;
					if (weight < 1e-6)
						normal = points[pointIndex].normal;
					else if (weight > 1 - 1e-6)
						normal = points[pointIndex + 1].normal;
					else
						normal = Vector3.Lerp(points[pointIndex].normal, points[pointIndex + 1].normal, weight);
				}
				else // Should not happen, except if the z coordinate is EXACTLY at the end of the bezier
				{
					centerPos = points[pointIndex].position;
					forward = points[pointIndex].forward;
					normal = points[pointIndex].normal;
				}
				Vector3 right = Vector3.Cross(normal, forward).normalized;

				pos = centerPos + right * pos.x + normal * pos.y;

				newVertices[v] = pos;
			}

			newMesh.subMeshCount = submeshCount;
			newMesh.SetVertices(newVertices);
			for (int i = 0; i < submeshCount; ++i)
			{
				newMesh.SetIndices(newIndices[i].ToArray(), oldMesh.GetTopology(i), i);
				newMesh.SetUVs(i, newUVs[i].ToArray());
			}
			newMesh.RecalculateNormals();
			newMesh.RecalculateTangents();
			newMesh.RecalculateBounds();

			mf.mesh = newMesh;
			if (GetComponent<MeshCollider>() != null)
				GetComponent<MeshCollider>().sharedMesh = newMesh;

			Valid = true;
		}

		void ClipMeshZ(ref List<Vector3> verticesRef, ref List<int> indicesRef, ref List<Vector2>[] uvsRef, float maxZ)
		{
			var reuseVertices = true;

			var vertices = verticesRef;
			var indices = indicesRef;
			var uvs = uvsRef;

			var newVertices = vertices.ToList();
			var newIndices = new List<int>();
			var newUVs = uvs.Select(e => e.ToList()).ToArray();

			var intersectedIndices = new Dictionary<(int a, int b), int>();

			for (int tri = 0; tri + 3 <= indices.Count; tri += 3)
			{
				switch (new int[] { tri, tri + 1, tri + 2 }.Where(i => vertices[indices[i]].z <= maxZ).Count())
				{
					case 3:
						{
							newIndices.Add(indices[tri]);
							newIndices.Add(indices[tri + 1]);
							newIndices.Add(indices[tri + 2]);
							break;
						}
					case 2:
						{
							var a = indices[tri];
							var b = indices[tri + 1];
							var c = indices[tri + 2];
							// shuffle to make a and b inside
							if (vertices[a].z > maxZ)
							{
								var t = a;
								a = b;
								b = c;
								c = t;
							}
							else if (vertices[b].z > maxZ)
							{
								var t = b;
								b = a;
								a = c;
								c = t;
							}
							var ac = vertices[c] - vertices[a];
							var bc = vertices[c] - vertices[b];
							if ((vertices[c].z - vertices[a].z) < 1e-6 || (vertices[c].z - vertices[b].z) < 1e-6) break;
							var va = vertices[a] + ac * (maxZ - vertices[a].z) / (vertices[c].z - vertices[a].z);
							var vb = vertices[b] + bc * (maxZ - vertices[b].z) / (vertices[c].z - vertices[b].z);

							var insertedA = false;
							int ia;
							if (!reuseVertices || !intersectedIndices.ContainsKey((a, c)))
							{
								newVertices.Add(va);
								ia = newVertices.Count - 1;
								intersectedIndices[(a, c)] = ia;
								insertedA = true;
							}
							else ia = intersectedIndices[(a, c)];
							var insertedB = false;
							int ib;
							if (!reuseVertices || !intersectedIndices.ContainsKey((b, c)))
							{
								newVertices.Add(vb);
								ib = newVertices.Count - 1;
								intersectedIndices[(b, c)] = ib;
								insertedB = true;
							}
							else ib = intersectedIndices[(b, c)];

							var weightA = (va - vertices[c]).magnitude / ac.magnitude;
							var weightB = (vb - vertices[c]).magnitude / bc.magnitude;
							for (int channel = 0; channel < 8; ++channel)
							{
								if (newUVs[channel].Count > 0)
								{
									if (insertedA)
										newUVs[channel].Add(
											weightA * uvs[channel][a] + (1 - weightA) * uvs[channel][c]);
									if (insertedB)
										newUVs[channel].Add(
											weightB * uvs[channel][b] + (1 - weightB) * uvs[channel][c]);
								}
							}
							newIndices.AddRange(new[] {
								a, b, ib,
								a, ib, ia
							});
							break;
						}
					case 1:
						{
							var a = indices[tri];
							var b = indices[tri + 1];
							var c = indices[tri + 2];
							// shuffle to make a and b inside
							if (vertices[a].z <= maxZ)
							{
								var t = a;
								a = b;
								b = c;
								c = t;
							}
							else if (vertices[b].z <= maxZ)
							{
								var t = b;
								b = a;
								a = c;
								c = t;
							}
							var ca = vertices[a] - vertices[c];
							var cb = vertices[b] - vertices[c];
							if ((vertices[a].z - vertices[c].z) < 1e-6 || (vertices[b].z - vertices[c].z) < 1e-6) break;
							var va = vertices[c] + ca * (maxZ - vertices[c].z) / (vertices[a].z - vertices[c].z);
							var vb = vertices[c] + cb * (maxZ - vertices[c].z) / (vertices[b].z - vertices[c].z);

							var insertedA = false;
							int ia;
							if (!reuseVertices || !intersectedIndices.ContainsKey((c, a)))
							{
								newVertices.Add(va);
								ia = newVertices.Count - 1;
								intersectedIndices[(c, a)] = ia;
								insertedA = true;
							}
							else ia = intersectedIndices[(c, a)];
							var insertedB = false;
							int ib;
							if (!reuseVertices || !intersectedIndices.ContainsKey((c, b)))
							{
								newVertices.Add(vb);
								ib = newVertices.Count - 1;
								intersectedIndices[(c, b)] = ib;
								insertedB = true;
							}
							else ib = intersectedIndices[(c, b)];

							var weightA = (va - vertices[c]).magnitude / ca.magnitude;
							var weightB = (vb - vertices[c]).magnitude / cb.magnitude;
							for (int channel = 0; channel < 8; ++channel)
							{
								if (newUVs[channel].Count > 0)
								{
									if (insertedA)
										newUVs[channel].Add(
											weightA * uvs[channel][a] + (1 - weightA) * uvs[channel][c]);
									if (insertedB)
										newUVs[channel].Add(
											weightB * uvs[channel][b] + (1 - weightB) * uvs[channel][c]);
								}
							}
							newIndices.AddRange(new[] {
								ia, ib, c
							});
							break;
						}
				}
			}

			verticesRef = newVertices;
			indicesRef = newIndices;
			uvsRef = newUVs;
		}

		/// <summary>
		/// To be called whenever the road shape changes. Will regenerate the mesh if AutoGenerate is true.
		/// </summary>
		/// <param name="update">- whether to regenerate the mesh at all.</param>
		public void Invalidate(bool update = true)
		{
			Valid = false;
			if (AutoGenerate && update) GenerateRoadMesh();
		}
	}
}
