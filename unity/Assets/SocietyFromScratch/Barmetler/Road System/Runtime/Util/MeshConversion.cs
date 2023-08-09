using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Barmetler
{
	public static class MeshConversion
	{
		/// <summary>
		/// Describes the coordinate space of a mesh.
		/// </summary>
		[System.Serializable]
		public struct MeshOrientation
		{
			public enum AxisDirection
			{
				X_POSITIVE, X_NEGATIVE, Y_POSITIVE, Y_NEGATIVE, Z_POSITIVE, Z_NEGATIVE
			}

			[Tooltip("Which axis represents the forward vector?")]
			public AxisDirection forward;
			[Tooltip("Which axis represents the up vector?")]
			public AxisDirection up;
			[Tooltip("False for left-handed (like in Unity), true for right-handed (like in Blender)")]
			public bool isRightHanded;

			public string Preset
			{
				get
				{
					if (PresetNames.TryGetValue(this, out var ret))
						return ret;
					foreach (var kv in Presets)
					{
						if (Equals(kv.Value))
						{
							PresetNames[this] = kv.Key;
							return kv.Key;
						}
					}
					return "CUSTOM";
				}
				set
				{
					if (Presets.TryGetValue(value, out var v))
					{
						this = v;
						PresetNames[v] = value;
					}
				}
			}

			public readonly static Dictionary<string, MeshOrientation> Presets = new Dictionary<string, MeshOrientation>
			{
				["BLENDER"] = new MeshOrientation
				{
					forward = AxisDirection.Y_POSITIVE,
					up = AxisDirection.Z_POSITIVE,
					isRightHanded = true
				},
				["UNITY"] = new MeshOrientation
				{
					forward = AxisDirection.Z_POSITIVE,
					up = AxisDirection.Y_POSITIVE,
					isRightHanded = false
				},
			};
			readonly static Dictionary<MeshOrientation, string> PresetNames = new Dictionary<MeshOrientation, string>();
		}

		/// <summary>
		/// Perform a deep copy of a Mesh.
		/// </summary>
		/// <param name="m">- the Mesh to copy.</param>
		/// <returns>new Mesh instance.</returns>
		public static Mesh CopyMesh(Mesh m)
		{
			var ret = new Mesh
			{
				vertices = m.vertices.ToArray(),
				uv = m.uv.ToArray(),
				uv2 = m.uv2.ToArray(),
				tangents = m.tangents.ToArray(),
				normals = m.normals.ToArray(),
				colors32 = m.colors32.ToArray()
			};

			int[][] t = new int[m.subMeshCount][];
			for (int i = 0; i < t.Length; i++)
				t[i] = m.GetTriangles(i);

			ret.name = m.name;
			ret.subMeshCount = m.subMeshCount;

			for (int i = 0; i < t.Length; i++)
				ret.SetTriangles(t[i], i);

			return ret;
		}

		public static Vector3 ToVector(this MeshOrientation.AxisDirection axis) => axis switch
		{
			MeshOrientation.AxisDirection.X_POSITIVE => Vector3.right,
			MeshOrientation.AxisDirection.X_NEGATIVE => -Vector3.right,
			MeshOrientation.AxisDirection.Y_POSITIVE => Vector3.up,
			MeshOrientation.AxisDirection.Y_NEGATIVE => -Vector3.up,
			MeshOrientation.AxisDirection.Z_POSITIVE => Vector3.forward,
			MeshOrientation.AxisDirection.Z_NEGATIVE => -Vector3.forward,
			_ => Vector3.zero, // can't happen
		};

		/// <summary>
		/// Transform a Mesh from the supplied space to Unity's space.
		/// </summary>
		/// <param name="mesh">- the Mesh to transform</param>
		/// <param name="from">- the space the Mesh is currently in</param>
		public static void TransformMesh(Mesh mesh, MeshOrientation from) =>
			TransformMesh(mesh, from, MeshOrientation.Presets["UNITY"]);

		/// <summary>
		/// Transform a Mesh from the one space to another
		/// </summary>
		/// <param name="mesh">- the Mesh to transform</param>
		/// <param name="from">- the space the Mesh is currently in</param>
		/// <param name="to">- the space the Mesh will be transformed to</param>
		public static void TransformMesh(Mesh mesh, MeshOrientation from, MeshOrientation to)
		{
			var from_forward = from.forward.ToVector();
			var from_up = from.up.ToVector();
			var from_right = from.isRightHanded ? Vector3.Cross(from_forward, from_up) : Vector3.Cross(from_up, from_forward);
			var to_forward = to.forward.ToVector();
			var to_up = to.up.ToVector();
			var to_right = to.isRightHanded ? Vector3.Cross(to_forward, to_up) : Vector3.Cross(to_up, to_forward);

			var vertices = new Vector3[mesh.vertexCount];
			for (int i = 0; i < mesh.vertexCount; ++i)
			{
				var v = mesh.vertices[i];
				vertices[i] =
					to_right * Vector3.Dot(from_right, v) +
					to_forward * Vector3.Dot(from_forward, v) +
					to_up * Vector3.Dot(from_up, v);
			}
			mesh.SetVertices(vertices);

			for (int submesh = 0; submesh < mesh.subMeshCount; ++submesh)
			{
				var oldTriangles = mesh.GetTriangles(submesh);
				var triangles = new int[oldTriangles.Length];
				if (from.isRightHanded != to.isRightHanded)
				{
					for (int tri = 0; tri + 3 <= triangles.Length; tri += 3)
					{
						triangles[tri] = oldTriangles[tri];
						triangles[tri + 1] = oldTriangles[tri + 2];
						triangles[tri + 2] = oldTriangles[tri + 1];
					}
				}
				mesh.SetTriangles(triangles, submesh);
			}

			mesh.RecalculateNormals();
			mesh.RecalculateTangents();
			mesh.RecalculateBounds();
		}
	}
}
