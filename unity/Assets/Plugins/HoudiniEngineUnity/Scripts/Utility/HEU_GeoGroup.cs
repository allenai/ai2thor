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

using System;
using System.Collections.Generic;
using UnityEngine;

namespace HoudiniEngineUnity
{
	/// <summary>
	/// Container for geometry group data.
	/// </summary>
	public class HEU_GeoGroup : IComparable<HEU_GeoGroup>
	{
		public string _groupName;

		// The submeshes that are part of this group, with their mesh ID (material)
		public Dictionary<int, HEU_MeshData> _subMeshesMap = new Dictionary<int, HEU_MeshData>();

		// We'll be generating the normals if they're not provided by Houdini
		// For every vertex, we'll hold list of other vertices that it shares faces with (ie. connected to)
		public List<HEU_VertexEntry>[] _sharedNormalIndices;

		public int CompareTo(HEU_GeoGroup other)
		{
			// Null means this object is greater
			if (other == null)
			{
				return 1;
			}

			return this._groupName.CompareTo(other._groupName);
		}

		public void SetupNormalIndices(int indicesCount)
		{
			_sharedNormalIndices = new List<HEU_VertexEntry>[indicesCount];
			for (int i = 0; i < indicesCount; ++i)
			{
				_sharedNormalIndices[i] = new List<HEU_VertexEntry>();
			}
		}
	}

	/// <summary>
	/// Helper used for storing vertex connections for normal generation
	/// </summary>
	public class HEU_VertexEntry
	{
		public int _meshKey;
		public int _vertexIndex;
		public int _normalIndex;

		public HEU_VertexEntry(int meshKey, int vertexIndex, int normalIndex)
		{
			_meshKey = meshKey;
			_vertexIndex = vertexIndex;
			_normalIndex = normalIndex;
		}
	}

}   // HoudiniEngineUnity