using System;
using UnityEngine;

namespace HoudiniEngineUnity
{
	/// <summary>
	/// Wraps UnityEngine.Rendering.IndexFormat which isn't available in older Unity versions.
	/// </summary>
	[System.Serializable]
	public class HEU_MeshIndexFormat
	{
#if UNITY_2017_3_OR_NEWER
		// Store the type of the index buffer size. By default use 16-bit, but will change to 32-bit if 
		// for large vertex count.
		public UnityEngine.Rendering.IndexFormat _indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
#endif

		/// <summary>
		/// Calculate the index format based on number of vertices.
		/// </summary>
		public void CalculateIndexFormat(int numVertices)
		{
			uint maxVertexCount = ushort.MaxValue;
			uint vertexCount = Convert.ToUInt32(numVertices);
			if (vertexCount > maxVertexCount)
			{
#if UNITY_2017_3_OR_NEWER
				// For vertex count larger than 16-bit, use 32-bit buffer
				_indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
#else
				Debug.LogErrorFormat("Vertex count {0} which is above Unity maximum of {1}.\nUse Unity 2017.3+ or reduce this in Houdini.",
					vertexCount, maxVertexCount);
#endif
			}
		}

		/// <summary>
		/// Set the given mesh's index format based on current index format setting.
		/// </summary>
		/// <param name="mesh"></param>
		public void SetFormatForMesh(Mesh mesh)
		{
#if UNITY_2017_3_OR_NEWER
			mesh.indexFormat = _indexFormat;
#endif
		}
	}

}