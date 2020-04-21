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

using System.Collections.Generic;
using UnityEngine;

namespace HoudiniEngineUnity
{
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Typedefs (copy these from HEU_Common.cs)
	using HAPI_NodeId = System.Int32;
	using HAPI_PartId = System.Int32;

	/// <summary>
	/// Utility class for uploading input mesh data
	/// </summary>
	public static class HEU_InputMeshUtility
	{
		public static bool SetMeshPointAttribute(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID, string attrName,
			int tupleSize, Vector3[] data, ref HAPI_PartInfo partInfo, bool bConvertToHoudiniCoordinateSystem)
		{
			HAPI_AttributeInfo attrInfo = new HAPI_AttributeInfo();
			attrInfo.exists = true;
			attrInfo.owner = HAPI_AttributeOwner.HAPI_ATTROWNER_POINT;
			attrInfo.storage = HAPI_StorageType.HAPI_STORAGETYPE_FLOAT;
			attrInfo.count = partInfo.pointCount;
			attrInfo.tupleSize = tupleSize;
			attrInfo.originalOwner = HAPI_AttributeOwner.HAPI_ATTROWNER_INVALID;

			float[] attrValues = new float[partInfo.pointCount * tupleSize];

			if (session.AddAttribute(geoID, 0, attrName, ref attrInfo))
			{
				float conversionMultiplier = bConvertToHoudiniCoordinateSystem ? -1f : 1f;

				for (int i = 0; i < partInfo.pointCount; ++i)
				{
					attrValues[i * tupleSize + 0] = conversionMultiplier * data[i][0];

					for (int j = 1; j < tupleSize; ++j)
					{
						attrValues[i * tupleSize + j] = data[i][j];
					}
				}
			}

			return HEU_GeneralUtility.SetAttributeArray(geoID, partID, attrName, ref attrInfo, attrValues, session.SetAttributeFloatData, partInfo.pointCount);
		}

		public static bool SetMeshVertexAttribute(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID, string attrName,
			int tupleSize, Vector3[] data, int[] indices, ref HAPI_PartInfo partInfo, bool bConvertToHoudiniCoordinateSystem)
		{
			HAPI_AttributeInfo attrInfo = new HAPI_AttributeInfo();
			attrInfo.exists = true;
			attrInfo.owner = HAPI_AttributeOwner.HAPI_ATTROWNER_VERTEX;
			attrInfo.storage = HAPI_StorageType.HAPI_STORAGETYPE_FLOAT;
			attrInfo.count = partInfo.vertexCount;
			attrInfo.tupleSize = tupleSize;
			attrInfo.originalOwner = HAPI_AttributeOwner.HAPI_ATTROWNER_INVALID;

			float[] attrValues = new float[partInfo.vertexCount * tupleSize];

			if (session.AddAttribute(geoID, 0, attrName, ref attrInfo))
			{
				float conversionMultiplier = bConvertToHoudiniCoordinateSystem ? -1f : 1f;

				for (int i = 0; i < partInfo.vertexCount; ++i)
				{
					attrValues[i * tupleSize + 0] = conversionMultiplier * data[indices[i]][0];

					for (int j = 1; j < tupleSize; ++j)
					{
						attrValues[i * tupleSize + j] = data[indices[i]][j];
					}
				}
			}

			return HEU_GeneralUtility.SetAttributeArray(geoID, partID, attrName, ref attrInfo, attrValues, session.SetAttributeFloatData, partInfo.vertexCount);
		}

		public static bool SetMeshVertexFloatAttribute(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID, string attrName,
			int tupleSize, float[] data, int[] indices, ref HAPI_PartInfo partInfo)
		{
			HAPI_AttributeInfo attrInfo = new HAPI_AttributeInfo();
			attrInfo.exists = true;
			attrInfo.owner = HAPI_AttributeOwner.HAPI_ATTROWNER_VERTEX;
			attrInfo.storage = HAPI_StorageType.HAPI_STORAGETYPE_FLOAT;
			attrInfo.count = partInfo.vertexCount;
			attrInfo.tupleSize = tupleSize;
			attrInfo.originalOwner = HAPI_AttributeOwner.HAPI_ATTROWNER_INVALID;

			float[] attrValues = new float[partInfo.vertexCount * tupleSize];

			if (session.AddAttribute(geoID, 0, attrName, ref attrInfo))
			{
				for (int i = 0; i < partInfo.vertexCount; ++i)
				{
					for (int j = 0; j < tupleSize; ++j)
					{
						attrValues[i * tupleSize + j] = data[indices[i] * tupleSize + j];
					}
				}
			}

			return HEU_GeneralUtility.SetAttributeArray(geoID, partID, attrName, ref attrInfo, attrValues, session.SetAttributeFloatData, partInfo.vertexCount);
		}

		/// <summary>
		/// Uploads given mesh geometry into Houdini.
		/// Creates a new part for given geo node, and uploads vertices, indices, UVs, Normals, and Colors.
		/// </summary>
		/// <param name="session"></param>
		/// <param name="assetNodeID"></param>
		/// <param name="objectID"></param>
		/// <param name="geoID"></param>
		/// <param name="mesh"></param>
		/// <returns>True if successfully uploaded all required data.</returns>
		public static bool UploadMeshIntoHoudiniNode(HEU_SessionBase session, HAPI_NodeId assetNodeID, HAPI_NodeId objectID, HAPI_NodeId geoID, ref Mesh mesh)
		{
			bool bSuccess = false;

			Vector3[] vertices = mesh.vertices;
			int[] triIndices = mesh.triangles;
			Vector2[] uvs = mesh.uv;
			Vector3[] normals = mesh.normals;
			Color[] colors = mesh.colors;

			HAPI_PartInfo partInfo = new HAPI_PartInfo();
			partInfo.faceCount = triIndices.Length / 3;
			partInfo.vertexCount = triIndices.Length;
			partInfo.pointCount = vertices.Length;
			partInfo.pointAttributeCount = 1;
			partInfo.vertexAttributeCount = 0;
			partInfo.primitiveAttributeCount = 0;
			partInfo.detailAttributeCount = 0;

			if (uvs != null && uvs.Length > 0)
			{
				partInfo.pointAttributeCount++;
			}
			if (normals != null && normals.Length > 0)
			{
				partInfo.pointAttributeCount++;
			}
			if (colors != null && colors.Length > 0)
			{
				partInfo.pointAttributeCount++;
			}

			bSuccess = session.SetPartInfo(geoID, 0, ref partInfo);
			if (!bSuccess)
			{
				return false;
			}

			int[] faceCounts = new int[partInfo.faceCount];
			for (int i = 0; i < partInfo.faceCount; ++i)
			{
				faceCounts[i] = 3;
			}
			bSuccess = HEU_GeneralUtility.SetArray2Arg(geoID, 0, session.SetFaceCount, faceCounts, 0, partInfo.faceCount);
			if (!bSuccess)
			{
				return false;
			}

			int[] vertexList = new int[partInfo.vertexCount];
			for (int i = 0; i < partInfo.faceCount; ++i)
			{
				for (int j = 0; j < 3; ++j)
				{
					vertexList[i * 3 + j] = triIndices[i * 3 + j];
				}
			}
			bSuccess = HEU_GeneralUtility.SetArray2Arg(geoID, 0, session.SetVertexList, vertexList, 0, partInfo.vertexCount);
			if (!bSuccess)
			{
				return false;
			}

			bSuccess = HEU_InputMeshUtility.SetMeshPointAttribute(session, geoID, 0, HEU_Defines.HAPI_ATTRIB_POSITION, 3, vertices, ref partInfo, true);
			if (!bSuccess)
			{
				return false;
			}

			bSuccess = HEU_InputMeshUtility.SetMeshPointAttribute(session, geoID, 0, HEU_Defines.HAPI_ATTRIB_NORMAL, 3, normals, ref partInfo, true);
			if (!bSuccess)
			{
				return false;
			}

			if (uvs != null && uvs.Length > 0)
			{
				Vector3[] uvs3 = new Vector3[uvs.Length];
				for (int i = 0; i < uvs.Length; ++i)
				{
					uvs3[i][0] = uvs[i][0];
					uvs3[i][1] = uvs[i][1];
					uvs3[i][2] = 0;
				}
				bSuccess = HEU_InputMeshUtility.SetMeshPointAttribute(session, geoID, 0, HEU_Defines.HAPI_ATTRIB_UV, 3, uvs3, ref partInfo, false);
				if (!bSuccess)
				{
					return false;
				}
			}

			if (colors != null && colors.Length > 0)
			{
				Vector3[] rgb = new Vector3[colors.Length];
				Vector3[] alpha = new Vector3[colors.Length];
				for (int i = 0; i < colors.Length; ++i)
				{
					rgb[i][0] = colors[i].r;
					rgb[i][1] = colors[i].g;
					rgb[i][2] = colors[i].b;

					alpha[i][0] = colors[i].a;
				}

				bSuccess = HEU_InputMeshUtility.SetMeshPointAttribute(session, geoID, 0, HEU_Defines.HAPI_ATTRIB_COLOR, 3, rgb, ref partInfo, false);
				if (!bSuccess)
				{
					return false;
				}

				bSuccess = HEU_InputMeshUtility.SetMeshPointAttribute(session, geoID, 0, HEU_Defines.HAPI_ATTRIB_ALPHA, 1, alpha, ref partInfo, false);
				if (!bSuccess)
				{
					return false;
				}
			}

			// TODO: additional attributes (for painting)

			return session.CommitGeo(geoID);
		}
	}

}   // HoudiniEngineUnity