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

// Uncomment to profile
//#define HEU_PROFILER_ON

using UnityEngine;
using System.Text;
using System.Collections.Generic;


namespace HoudiniEngineUnity
{
	using HAPI_NodeId = System.Int32;
	using HAPI_PartId = System.Int32;
	using HAPI_StringHandle = System.Int32;


	/// <summary>
	/// Stores geometry and material info for a part that is then used to generate Unity geometry.
	/// </summary>
	public class HEU_GenerateGeoCache
	{
		//	DATA ------------------------------------------------------------------------------------------------------

		public HAPI_NodeId GeoID { get { return _geoInfo.nodeId; } }
		public HAPI_PartId PartID { get { return _partInfo.id; } }

		public HAPI_NodeId AssetID { get; set; }

		public HAPI_GeoInfo _geoInfo;
		public HAPI_PartInfo _partInfo;

		public string _partName;

		public int[] _vertexList;

		public int[] _faceCounts;

		public HAPI_NodeId[] _houdiniMaterialIDs;

		public bool _singleFaceUnityMaterial;
		public bool _singleFaceHoudiniMaterial;

		public Dictionary<int, HEU_UnityMaterialInfo> _unityMaterialInfos;
		public HAPI_AttributeInfo _unityMaterialAttrInfo;
		public HAPI_StringHandle[] _unityMaterialAttrName;
		public Dictionary<HAPI_StringHandle, string> _unityMaterialAttrStringsMap = new Dictionary<HAPI_StringHandle, string>();

		public HAPI_AttributeInfo _substanceMaterialAttrNameInfo;
		public HAPI_StringHandle[] _substanceMaterialAttrName;
		public Dictionary<HAPI_StringHandle, string> _substanceMaterialAttrStringsMap = new Dictionary<HAPI_StringHandle, string>();

		public HAPI_AttributeInfo _substanceMaterialAttrIndexInfo;
		public int[] _substanceMaterialAttrIndex;

		public List<HEU_MaterialData> _inUseMaterials = new List<HEU_MaterialData>();

		public HAPI_AttributeInfo _posAttrInfo;
		public HAPI_AttributeInfo[] _uvsAttrInfo;
		public HAPI_AttributeInfo _normalAttrInfo;
		public HAPI_AttributeInfo _colorAttrInfo;
		public HAPI_AttributeInfo _alphaAttrInfo;
		public HAPI_AttributeInfo _tangentAttrInfo;

		public float[] _posAttr;
		public float[][] _uvsAttr;
		public float[] _normalAttr;
		public float[] _colorAttr;
		public float[] _alphaAttr;
		public float[] _tangentAttr;

		public string[] _groups;
		public bool _hasGroupGeometry;

		public Dictionary<string, int[]> _groupSplitVertexIndices = new Dictionary<string, int[]>();
		public Dictionary<string, List<int>> _groupSplitFaceIndices = new Dictionary<string, List<int>>();
		public Dictionary<string, List<int>> _groupVertexOffsets = new Dictionary<string, List<int>>();

		public int[] _allCollisionVertexList;
		public int[] _allCollisionFaceIndices;

		public float _normalCosineThreshold;

		public bool _hasLODGroups;
		public float[] _LODTransitionValues;

		public bool _isMeshReadWrite = false;

		// Colliders
		public class HEU_ColliderInfo
		{
			public enum ColliderType
			{
				NONE,
				BOX,
				SPHERE,
				MESH,
				SIMPLE_BOX,
				SIMPLE_SPHERE,
				SIMPLE_CAPSULE
			}

			public ColliderType _colliderType;
			public Vector3 _colliderCenter;
			public Vector3 _colliderSize;
			public float _colliderRadius;
			public bool _convexCollider;

			public string _collisionGroupName;
			public Vector3[] _collisionVertices;
			public int[] _collisionIndices;

			public MeshTopology _meshTopology = MeshTopology.Triangles;

			public bool _isTrigger = false;
		}
		public List<HEU_ColliderInfo> _colliderInfos = new List<HEU_ColliderInfo>();

		public List<HEU_MaterialData> _materialCache;
		public Dictionary<int, HEU_MaterialData> _materialIDToDataMap;

		public string _assetCacheFolderPath;

		[SerializeField]
		public HEU_MeshIndexFormat _meshIndexFormat = new HEU_MeshIndexFormat();


		//	LOGIC -----------------------------------------------------------------------------------------------------

		/// <summary>
		/// Creates a new HEU_GenerateGeoCache with geometry and material data for given part.
		/// </summary>
		/// <param name="bUseLODGroups">Whether to split group by LOD name</param>
		/// <returns>New HEU_GenerateGeoCache populated with geometry and material data.</returns>
		public static HEU_GenerateGeoCache GetPopulatedGeoCache(HEU_SessionBase session, HAPI_NodeId assetID, HAPI_NodeId geoID, HAPI_PartId partID, bool bUseLODGroups,
			List<HEU_MaterialData> materialCache, string assetCacheFolderPath)
		{
#if HEU_PROFILER_ON
			float generateGeoCacheStartTime = Time.realtimeSinceStartup;
#endif

			HEU_GenerateGeoCache geoCache = new HEU_GenerateGeoCache();

			geoCache.AssetID = assetID;

			Debug.Assert(geoID != HEU_Defines.HEU_INVALID_NODE_ID, "Invalid Geo ID! Unable to update materials!");
			Debug.Assert(partID != HEU_Defines.HEU_INVALID_NODE_ID, "Invalid Part ID! Unable to update materials!");

			geoCache._geoInfo = new HAPI_GeoInfo();
			if (!session.GetGeoInfo(geoID, ref geoCache._geoInfo))
			{
				return null;
			}

			geoCache._partInfo = new HAPI_PartInfo();
			if (!session.GetPartInfo(geoID, partID, ref geoCache._partInfo))
			{
				return null;
			}

			geoCache._faceCounts = new int[geoCache._partInfo.faceCount];
			if (!session.GetFaceCounts(geoID, partID, geoCache._faceCounts, 0, geoCache._partInfo.faceCount))
			{
				return null;
			}

			geoCache._partName = HEU_SessionManager.GetString(geoCache._partInfo.nameSH, session);

			geoCache._meshIndexFormat.CalculateIndexFormat(geoCache._partInfo.vertexCount);

			geoCache._vertexList = new int[geoCache._partInfo.vertexCount];
			if (!HEU_GeneralUtility.GetArray2Arg(geoID, partID, session.GetVertexList, geoCache._vertexList, 0, geoCache._partInfo.vertexCount))
			{
				return null;
			}

			geoCache._houdiniMaterialIDs = new HAPI_NodeId[geoCache._partInfo.faceCount];
			if (!session.GetMaterialNodeIDsOnFaces(geoID, partID, ref geoCache._singleFaceHoudiniMaterial, geoCache._houdiniMaterialIDs, geoCache._partInfo.faceCount))
			{
				return null;
			}

			geoCache.PopulateUnityMaterialData(session);

			geoCache._materialCache = materialCache;
			geoCache._materialIDToDataMap = HEU_MaterialFactory.GetMaterialDataMapFromCache(materialCache);
			geoCache._assetCacheFolderPath = assetCacheFolderPath;

			int meshReadWrite = 0;
			if (HEU_GeneralUtility.GetAttributeIntSingle(session, geoID, partID, HEU_Defines.DEFAULT_UNITY_MESH_READABLE, out meshReadWrite))
			{
				geoCache._isMeshReadWrite = meshReadWrite != 0;
			}

			if (!geoCache.PopulateGeometryData(session, bUseLODGroups))
			{
				return null;
			}

#if HEU_PROFILER_ON
			Debug.LogFormat("GENERATE GEO CACHE TIME:: {0}", (Time.realtimeSinceStartup - generateGeoCacheStartTime));
#endif

			return geoCache;
		}

		/// <summary>
		/// Parse and populate materials in use by part.
		/// </summary>
		public void PopulateUnityMaterialData(HEU_SessionBase session)
		{
			// First we look for Unity and Substance material attributes on faces.
			// We fill up the following dictionary with unique Unity + Substance material information
			_unityMaterialInfos = new Dictionary<int, HEU_UnityMaterialInfo>();

			_unityMaterialAttrInfo = new HAPI_AttributeInfo();
			_unityMaterialAttrName = new HAPI_StringHandle[0];
			HEU_GeneralUtility.GetAttribute(session, GeoID, PartID, HEU_PluginSettings.UnityMaterialAttribName, ref _unityMaterialAttrInfo, ref _unityMaterialAttrName, session.GetAttributeStringData);

			// Store a local copy of the actual string values since the indices get overwritten by the next call to session.GetAttributeStringData.
			// Using a dictionary to only query the unique strings, as doing all of them is very slow and unnecessary.
			_unityMaterialAttrStringsMap = new Dictionary<HAPI_StringHandle, string>();
			foreach (HAPI_StringHandle strHandle in _unityMaterialAttrName)
			{
				if (!_unityMaterialAttrStringsMap.ContainsKey(strHandle))
				{
					string materialName = HEU_SessionManager.GetString(strHandle, session);
					if (string.IsNullOrEmpty(materialName))
					{
						// Warn user of empty string, but add it anyway to our map so we don't keep trying to parse it
						Debug.LogWarningFormat("Found empty material attribute value for part {0}.", _partName);
					}
					_unityMaterialAttrStringsMap.Add(strHandle, materialName);
					//Debug.LogFormat("Added Unity material: " + materialName);
				}
			}

			_substanceMaterialAttrNameInfo = new HAPI_AttributeInfo();
			_substanceMaterialAttrName = new HAPI_StringHandle[0];
			HEU_GeneralUtility.GetAttribute(session, GeoID, PartID, HEU_PluginSettings.UnitySubMaterialAttribName, ref _substanceMaterialAttrNameInfo, ref _substanceMaterialAttrName, session.GetAttributeStringData);

			_substanceMaterialAttrStringsMap = new Dictionary<HAPI_StringHandle, string>();
			foreach (HAPI_StringHandle strHandle in _substanceMaterialAttrName)
			{
				if (!_substanceMaterialAttrStringsMap.ContainsKey(strHandle))
				{
					string substanceName = HEU_SessionManager.GetString(strHandle, session);
					if (string.IsNullOrEmpty(substanceName))
					{
						// Warn user of empty string, but add it anyway to our map so we don't keep trying to parse it
						Debug.LogWarningFormat("Found invalid substance material attribute value ({0}) for part {1}.",
							_partName, substanceName);
					}
					_substanceMaterialAttrStringsMap.Add(strHandle, substanceName);
					//Debug.LogFormat("Added Substance material: " + substanceName);
				}
			}

			_substanceMaterialAttrIndexInfo = new HAPI_AttributeInfo();
			_substanceMaterialAttrIndex = new int[0];
			HEU_GeneralUtility.GetAttribute(session, GeoID, PartID, HEU_PluginSettings.UnitySubMaterialIndexAttribName, ref _substanceMaterialAttrIndexInfo, ref _substanceMaterialAttrIndex, session.GetAttributeIntData);


			if (_unityMaterialAttrInfo.exists)
			{
				if (_unityMaterialAttrInfo.owner == HAPI_AttributeOwner.HAPI_ATTROWNER_DETAIL && _unityMaterialAttrName.Length > 0)
				{
					CreateMaterialInfoEntryFromAttributeIndex(this, 0);

					// Detail unity material attribute means we can treat it as single material
					_singleFaceUnityMaterial = true;
				}
				else
				{
					for(HAPI_StringHandle i = 0; i < _unityMaterialAttrName.Length; ++i)
					{
						CreateMaterialInfoEntryFromAttributeIndex(this, i);
					}
				}
			}
		}

		public static int GetMaterialKeyFromAttributeIndex(HEU_GenerateGeoCache geoCache, int attributeIndex, out string unityMaterialName, out string substanceName, out int substanceIndex)
		{
			unityMaterialName = null;
			substanceName = null;
			substanceIndex = -1;
			if (attributeIndex < geoCache._unityMaterialAttrName.Length && geoCache._unityMaterialAttrStringsMap.TryGetValue(geoCache._unityMaterialAttrName[attributeIndex], out unityMaterialName))
			{
				if (geoCache._substanceMaterialAttrNameInfo.exists && geoCache._substanceMaterialAttrName.Length > 0)
				{
					geoCache._substanceMaterialAttrStringsMap.TryGetValue(geoCache._substanceMaterialAttrName[attributeIndex], out substanceName);
				}

				if (geoCache._substanceMaterialAttrIndexInfo.exists && string.IsNullOrEmpty(substanceName) && geoCache._substanceMaterialAttrIndex[attributeIndex] >= 0)
				{
					substanceIndex = geoCache._substanceMaterialAttrIndex[attributeIndex];
				}

				return HEU_MaterialFactory.GetUnitySubstanceMaterialKey(unityMaterialName, substanceName, substanceIndex);
			}
			return HEU_Defines.HEU_INVALID_MATERIAL;
		}

		public static void CreateMaterialInfoEntryFromAttributeIndex(HEU_GenerateGeoCache geoCache, int materialAttributeIndex)
		{
			string unityMaterialName = null;
			string substanceName = null;
			int substanceIndex = -1;
			int materialKey = GetMaterialKeyFromAttributeIndex(geoCache, materialAttributeIndex, out unityMaterialName, out substanceName, out substanceIndex);
			if (!geoCache._unityMaterialInfos.ContainsKey(materialKey))
			{
				geoCache._unityMaterialInfos.Add(materialKey, new HEU_UnityMaterialInfo(unityMaterialName, substanceName, substanceIndex));
			}
		}

		/// <summary>
		/// Populate geometry data such as positions, UVs, normals, colors, tangents, vertices, indices by group from part.
		/// Splits by collider and/or LOD groups. All other groups are combined to a single main group.
		/// </summary>
		/// <param name="bUseLODGroups">Split geometry by LOD group if true. Otherwise store all non-collision groups into main group.</param>
		/// <returns>True if successfull</returns>
		public bool PopulateGeometryData(HEU_SessionBase session, bool bUseLODGroups)
		{
			// Get vertex position
			HAPI_AttributeInfo posAttrInfo = new HAPI_AttributeInfo();
			_posAttr = new float[0];
			HEU_GeneralUtility.GetAttribute(session, GeoID, PartID, HEU_Defines.HAPI_ATTRIB_POSITION, ref posAttrInfo, ref _posAttr, session.GetAttributeFloatData);
			if (!posAttrInfo.exists)
			{
				return false;
			}
			else if (posAttrInfo.owner != HAPI_AttributeOwner.HAPI_ATTROWNER_POINT)
			{
				Debug.LogErrorFormat("{0} only supports position as POINT attribute. Position attribute of {1} type not supported!", HEU_Defines.HEU_PRODUCT_NAME, posAttrInfo.owner);
				return false;
			}

			// Get all UV attributes
			_uvsAttrInfo = new HAPI_AttributeInfo[HEU_Defines.HAPI_MAX_UVS];
			_uvsAttr = new float[HEU_Defines.HAPI_MAX_UVS][];
			for (int i = 0; i < HEU_Defines.HAPI_MAX_UVS; ++i)
			{
				_uvsAttrInfo[i] = new HAPI_AttributeInfo();
				_uvsAttr[i] = new float[0];
				string uvName = i == 0 ? HEU_Defines.HAPI_ATTRIB_UV : HEU_Defines.HAPI_ATTRIB_UV + (i + 1);
				HEU_GeneralUtility.GetAttribute(session, GeoID, PartID, uvName, ref _uvsAttrInfo[i], ref _uvsAttr[i], session.GetAttributeFloatData);

				if (_uvsAttrInfo[i].exists && (_uvsAttrInfo[i].tupleSize < 2 || _uvsAttrInfo[i].tupleSize > 4))
				{
					Debug.LogWarningFormat("UV attribute '{0}' has size {1} which is unsupported. Size must be either 2, 3, or 4.", uvName, _uvsAttrInfo[i].tupleSize);
					_uvsAttrInfo[i].exists = false;
				}
			}

			// Get normal attributes
			_normalAttrInfo = new HAPI_AttributeInfo();
			_normalAttr = new float[0];
			HEU_GeneralUtility.GetAttribute(session, GeoID, PartID, HEU_Defines.HAPI_ATTRIB_NORMAL, ref _normalAttrInfo, ref _normalAttr, session.GetAttributeFloatData);

			// Get colour attributes
			_colorAttrInfo = new HAPI_AttributeInfo();
			_colorAttr = new float[0];
			HEU_GeneralUtility.GetAttribute(session, GeoID, PartID, HEU_Defines.HAPI_ATTRIB_COLOR, ref _colorAttrInfo, ref _colorAttr, session.GetAttributeFloatData);

			// Get alpha attributes
			_alphaAttrInfo = new HAPI_AttributeInfo();
			_alphaAttr = new float[0];
			HEU_GeneralUtility.GetAttribute(session, GeoID, PartID, HEU_Defines.HAPI_ATTRIB_ALPHA, ref _alphaAttrInfo, ref _alphaAttr, session.GetAttributeFloatData);

			// Get tangent attributes
			_tangentAttrInfo = new HAPI_AttributeInfo();
			_tangentAttr = new float[0];
			HEU_GeneralUtility.GetAttribute(session, GeoID, PartID, HEU_Defines.HAPI_ATTRIB_TANGENT, ref _tangentAttrInfo, ref _tangentAttr, session.GetAttributeFloatData);

			// Warn user since we are splitting points by attributes, might prevent some attrributes
			// to be transferred over properly
			if (_normalAttrInfo.exists && _normalAttrInfo.owner != HAPI_AttributeOwner.HAPI_ATTROWNER_POINT
									&& _normalAttrInfo.owner != HAPI_AttributeOwner.HAPI_ATTROWNER_VERTEX)
			{
				Debug.LogWarningFormat("{0}: Normals are not declared as point or vertex attributes.\nSet them as per point or vertices in HDA.", _partName);
			}

			if (_tangentAttrInfo.exists && _tangentAttrInfo.owner != HAPI_AttributeOwner.HAPI_ATTROWNER_POINT
									&& _tangentAttrInfo.owner != HAPI_AttributeOwner.HAPI_ATTROWNER_VERTEX)
			{
				Debug.LogWarningFormat("{0}: Tangents are not declared as point or vertex attributes.\nSet them as per point or vertices in HDA.", _partName);
			}

			if (_colorAttrInfo.exists && _colorAttrInfo.owner != HAPI_AttributeOwner.HAPI_ATTROWNER_POINT
									&& _colorAttrInfo.owner != HAPI_AttributeOwner.HAPI_ATTROWNER_VERTEX)
			{
				Debug.LogWarningFormat("{0}: Colours are not declared as point or vertex attributes."
					+ "\nCurrently set as owner type {1}. Set them as per point or vertices in HDA.", _partName, _colorAttrInfo.owner);
			}

			_groups = HEU_SessionManager.GetGroupNames(GeoID, _partInfo.id, HAPI_GroupType.HAPI_GROUPTYPE_PRIM, _partInfo.isInstanced);

			_allCollisionVertexList = new int[_vertexList.Length];
			_allCollisionFaceIndices = new int[_partInfo.faceCount];

			_hasGroupGeometry = false;
			_hasLODGroups = false;

			if (_groups != null)
			{
				// We go through each group, building up a list of vertices and indices that belong to it
				// For strictly colliders (ie. non-rendering), we only create geometry colliders 
				for (int g = 0; g < _groups.Length; ++g)
				{
					string groupName = _groups[g];

					// Query HAPI to get the group membership. 
					// This is returned as an array of 1s for vertices that belong to this group.
					int[] membership = null;
					HEU_SessionManager.GetGroupMembership(session, GeoID, PartID, HAPI_GroupType.HAPI_GROUPTYPE_PRIM, groupName, ref membership, _partInfo.isInstanced);

					bool bIsCollidable = groupName.Contains(HEU_PluginSettings.CollisionGroupName);
					bool bIsRenderCollidable = groupName.Contains(HEU_PluginSettings.RenderedCollisionGroupName);
					bool bIsLODGroup = bUseLODGroups && groupName.StartsWith(HEU_Defines.HEU_DEFAULT_LOD_NAME);
					_hasLODGroups |= bIsLODGroup;

					if (bIsCollidable || bIsRenderCollidable || bIsLODGroup)
					{
						// Extract vertex indices for this group

						int[] groupVertexList = new int[_vertexList.Length];
						groupVertexList.Init<int>(-1);

						int groupVertexListCount = 0;

						List<int> groupFaceList = new List<int>();
						List<int> groupVertexOffsetList = new List<int>();
						int numFaces = _faceCounts.Length;
						int vertIndex = 0;
						for( int f = 0; f < numFaces; ++f)
						{
							int numVerts = _faceCounts[f];
							if (membership[f] > 0)
							{
								// This face is a member of the specified group

								groupFaceList.Add(f);
								groupVertexOffsetList.Add(vertIndex);

								for (int v = 0; v < numVerts; v++)
								{
									groupVertexList[vertIndex + v] = _vertexList[vertIndex + v];

									// Mark vertices as used
									_allCollisionVertexList[vertIndex + v] = 1;
								}

								// Mark face as used
								_allCollisionFaceIndices[f] = 1;

								groupVertexListCount += numVerts;
							}

							vertIndex += numVerts;
						}

						if (groupVertexListCount > 0)
						{
							_groupSplitVertexIndices.Add(groupName, groupVertexList);
							_groupSplitFaceIndices.Add(groupName, groupFaceList);
							_groupVertexOffsets.Add(groupName, groupVertexOffsetList);

							_hasGroupGeometry = true;

							//Debug.Log("Adding collision group: " + groupName + " with index count: " + _groupVertexList.Length);
						}
					}
				}
			}

			if (_hasGroupGeometry)
			{
				// Construct vertex list for all other vertices that are not part of any group
				int[] remainingGroupSplitFaces = new int[_vertexList.Length];
				remainingGroupSplitFaces.Init<int>(-1);
				bool bMainSplitGroup = false;

				List<int> remainingGroupSplitFaceIndices = new List<int>();
				List<int> remainingGroupVertexOffsets = new List<int>();

				for (int cv = 0; cv < _allCollisionVertexList.Length; ++cv)
				{
					if (_allCollisionVertexList[cv] == 0)
					{
						// Unused index, so add it to unused vertex list
						remainingGroupSplitFaces[cv] = _vertexList[cv];
						bMainSplitGroup = true;
					}
				}

				int vertIndex = 0;
				for (int cf = 0; cf < _allCollisionFaceIndices.Length; ++cf)
				{
					if (_allCollisionFaceIndices[cf] == 0)
					{
						remainingGroupSplitFaceIndices.Add(cf);
						remainingGroupVertexOffsets.Add(vertIndex);
					}

					vertIndex += _faceCounts[cf];
				}

				if (bMainSplitGroup)
				{
					_groupSplitVertexIndices.Add(HEU_Defines.HEU_DEFAULT_GEO_GROUP_NAME, remainingGroupSplitFaces);
					_groupSplitFaceIndices.Add(HEU_Defines.HEU_DEFAULT_GEO_GROUP_NAME, remainingGroupSplitFaceIndices);
					_groupVertexOffsets.Add(HEU_Defines.HEU_DEFAULT_GEO_GROUP_NAME, remainingGroupVertexOffsets);

					//Debug.Log("Adding remaining group with index count: " + remainingGroupSplitFaces.Length);
				}
			}
			else
			{
				_groupSplitVertexIndices.Add(HEU_Defines.HEU_DEFAULT_GEO_GROUP_NAME, _vertexList);

				int vertIndex = 0;
				List<int> allFaces = new List<int>();
				List<int> groupVertexOffsets = new List<int>();
				for (int f = 0; f < _partInfo.faceCount; ++f)
				{
					allFaces.Add(f);
					groupVertexOffsets.Add(vertIndex);
					vertIndex += _faceCounts[f];
				}
				_groupSplitFaceIndices.Add(HEU_Defines.HEU_DEFAULT_GEO_GROUP_NAME, allFaces);
				_groupVertexOffsets.Add(HEU_Defines.HEU_DEFAULT_GEO_GROUP_NAME, groupVertexOffsets);

				//Debug.Log("Adding single non-group with index count: " + _vertexList.Length);
			}

			if (!_normalAttrInfo.exists)
			{
				_normalCosineThreshold = Mathf.Cos(HEU_PluginSettings.NormalGenerationThresholdAngle * Mathf.Deg2Rad);
			}
			else
			{
				_normalCosineThreshold = 0f;
			}

			if (_hasLODGroups)
			{
				// Get the LOD transition attribute values
				ParseLODTransitionAttribute(session, GeoID, PartID, ref _LODTransitionValues);
			}

			return true;
		}

		/// <summary>
		/// Get the LOD transition attribute values from the given part.
		/// Expects it to be detail attribute with float type.
		/// </summary>
		/// <param name="LODTransitionValues">Output float array of LOD transition values</param>
		public static void ParseLODTransitionAttribute(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID, ref float[] LODTransitionValues)
		{
			LODTransitionValues = null;

			// Get LOD detail float attribute specifying screen transition values.
			HAPI_AttributeInfo lodTransitionAttributeInfo = new HAPI_AttributeInfo();
			float[] lodAttr = new float[0];

			HEU_GeneralUtility.GetAttribute(session, geoID, partID, HEU_Defines.HEU_UNITY_LOD_TRANSITION_ATTR, ref lodTransitionAttributeInfo, ref lodAttr, session.GetAttributeFloatData);
			if (lodTransitionAttributeInfo.exists)
			{
				int numLODValues = lodAttr.Length;

				if (lodTransitionAttributeInfo.owner != HAPI_AttributeOwner.HAPI_ATTROWNER_DETAIL)
				{
					Debug.LogWarningFormat("Houdini Engine for Unity only supports {0} as detail attributes!", HEU_Defines.HEU_UNITY_LOD_TRANSITION_ATTR);
				}
				else
				{
					LODTransitionValues = lodAttr;
				}
			}
		}

		public static void UpdateColliders(HEU_GenerateGeoCache geoCache, HEU_GeneratedOutputData outputData)
		{
			// Remove previously generated colliders
			HEU_GeneratedOutput.DestroyAllGeneratedColliders(outputData);

			foreach (HEU_ColliderInfo colliderInfo in geoCache._colliderInfos)
			{
				UpdateCollider(geoCache, outputData, colliderInfo);
			}
		}

		public static void UpdateCollider(HEU_GenerateGeoCache geoCache, HEU_GeneratedOutputData outputData, HEU_ColliderInfo colliderInfo)
		{
			GameObject outputGameObject = outputData._gameObject;

			if (colliderInfo._colliderType == HEU_ColliderInfo.ColliderType.BOX)
			{
				BoxCollider collider = HEU_GeneralUtility.GetOrCreateComponent<BoxCollider>(outputGameObject);
				collider.center = colliderInfo._colliderCenter;
				collider.size = colliderInfo._colliderSize;
				collider.isTrigger = colliderInfo._isTrigger;

				outputData._colliders.Add(collider);
			}
			else if(colliderInfo._colliderType == HEU_ColliderInfo.ColliderType.SPHERE)
			{
				SphereCollider collider = HEU_GeneralUtility.GetOrCreateComponent<SphereCollider>(outputGameObject);
				collider.center = colliderInfo._colliderCenter;
				collider.radius = colliderInfo._colliderRadius;
				collider.isTrigger = colliderInfo._isTrigger;

				outputData._colliders.Add(collider);
			}
			else if (colliderInfo._colliderType == HEU_ColliderInfo.ColliderType.SIMPLE_BOX)
			{
				BoxCollider collider = HEU_GeneralUtility.GetOrCreateComponent<BoxCollider>(outputGameObject);

				Vector3 firstPt = colliderInfo._collisionVertices[0];
				Bounds bounds = new Bounds(firstPt, Vector3.zero);

				for (int i = 1; i < colliderInfo._collisionVertices.Length; ++i)
				{
					bounds.Encapsulate(colliderInfo._collisionVertices[i]);
				}

				collider.center = bounds.center;
				collider.size = bounds.size;
				collider.isTrigger = colliderInfo._isTrigger;

				outputData._colliders.Add(collider);
			}
			else if (colliderInfo._colliderType == HEU_ColliderInfo.ColliderType.SIMPLE_SPHERE)
			{
				SphereCollider collider = HEU_GeneralUtility.GetOrCreateComponent<SphereCollider>(outputGameObject);

				Vector3 firstPt = colliderInfo._collisionVertices[0];
				Bounds bounds = new Bounds(firstPt, Vector3.zero);

				for (int i = 1; i < colliderInfo._collisionVertices.Length; ++i)
				{
					bounds.Encapsulate(colliderInfo._collisionVertices[i]);
				}

				Vector3 extents = bounds.extents;
				float max_extent = Mathf.Max(new float[] { extents.x, extents.y, extents.z });

				collider.center = bounds.center;
				collider.radius = max_extent;
				collider.isTrigger = colliderInfo._isTrigger;

				outputData._colliders.Add(collider);
			}
			else if (colliderInfo._colliderType == HEU_ColliderInfo.ColliderType.SIMPLE_CAPSULE)
			{
				CapsuleCollider collider = HEU_GeneralUtility.GetOrCreateComponent<CapsuleCollider>(outputGameObject);

				Vector3 firstPt = colliderInfo._collisionVertices[0];
				Bounds bounds = new Bounds(firstPt, Vector3.zero);

				for (int i = 1; i < colliderInfo._collisionVertices.Length; ++i)
				{
					bounds.Encapsulate(colliderInfo._collisionVertices[i]);
				}

				collider.center = bounds.center;
				collider.direction = 1;
				collider.height = bounds.size.y;
				collider.radius = bounds.extents.x;
				collider.isTrigger = colliderInfo._isTrigger;

				outputData._colliders.Add(collider);
			}
			else if(colliderInfo._colliderType == HEU_ColliderInfo.ColliderType.MESH)
			{
				MeshCollider meshCollider = HEU_GeneralUtility.GetOrCreateComponent<MeshCollider>(outputGameObject);

				Mesh collisionMesh = new Mesh();

				geoCache._meshIndexFormat.SetFormatForMesh(collisionMesh);

				collisionMesh.name = colliderInfo._collisionGroupName;
				collisionMesh.vertices = colliderInfo._collisionVertices;
				collisionMesh.SetIndices(colliderInfo._collisionIndices, colliderInfo._meshTopology, 0);
				collisionMesh.RecalculateBounds();

				meshCollider.sharedMesh = collisionMesh;
				meshCollider.convex = colliderInfo._convexCollider;
				meshCollider.isTrigger = colliderInfo._isTrigger;

				outputData._colliders.Add(meshCollider);
			}
		}

		private static void GetFinalMaterialsFromComparingNewWithPrevious(GameObject gameObject, Material[] previousMaterials, Material[] newMaterials, ref Material[] finalMaterials)
		{
			MeshRenderer meshRenderer = HEU_GeneralUtility.GetOrCreateComponent<MeshRenderer>(gameObject);

			Material[] currentMaterials = meshRenderer.sharedMaterials;
			int numCurrentMaterials = currentMaterials.Length;

			int numNewMaterials = newMaterials != null ? newMaterials.Length : 0;

			int numPreviousMaterials = previousMaterials != null ? previousMaterials.Length : 0;

			// Final material set is the superset of new materials and current materials
			int newTotalMaterials = numNewMaterials > numCurrentMaterials ? numNewMaterials : numCurrentMaterials;
			finalMaterials = new Material[newTotalMaterials];

			for (int i = 0; i < newTotalMaterials; ++i)
			{
				if (i < numCurrentMaterials)
				{
					// Current material exists. Check if it has been overriden.
					if (i < numPreviousMaterials)
					{
						if (currentMaterials[i] != previousMaterials[i])
						{
							// Material has been overriden by user. Keep it.
							finalMaterials[i] = currentMaterials[i];
						}
						else if(i < numNewMaterials)
						{
							// Material is same as previously generated, so update to new
							finalMaterials[i] = newMaterials[i];
						}
					}
					else if (currentMaterials[i] == null && i < numNewMaterials)
					{
						finalMaterials[i] = newMaterials[i];
					}
					else
					{
						// User must have added this material, so keep it
						finalMaterials[i] = currentMaterials[i];
					}
				}
				else
				{
					// Current material does not exist. So set new material.
					finalMaterials[i] = newMaterials[i];
				}
			}
		}

		/// <summary>
		/// Generates single mesh from given GeoGroup.
		/// </summary>
		/// <param name="GeoGroup">Contains submehs data</param>
		/// <param name="geoCache">Contains geometry data</param>
		/// <param name="outputGameObject">GameObject to attach generated mesh</param>
		/// <param name="defaultMaterialKey">The material key for default material</param>
		/// <returns></returns>
		public static bool GenerateMeshFromSingleGroup(HEU_SessionBase session, HEU_GeoGroup GeoGroup, HEU_GenerateGeoCache geoCache,
			HEU_GeneratedOutput generatedOutput, int defaultMaterialKey, bool bGenerateUVs, bool bGenerateTangents, bool bGenerateNormals, bool bPartInstanced)
		{
			Material[] finalMaterials = null;

			Mesh newMesh = null;
			Material[] newMaterials = null;
			bool bGeneratedMesh = GenerateMeshFromGeoGroup(session, GeoGroup, geoCache, defaultMaterialKey, bGenerateUVs, bGenerateTangents, bGenerateNormals,
				bPartInstanced, out newMesh, out newMaterials);

			if (bGeneratedMesh)
			{
				// In order to keep user overriden materials, need to check against existing and newly generated materials.
				GetFinalMaterialsFromComparingNewWithPrevious(generatedOutput._outputData._gameObject, generatedOutput._outputData._renderMaterials, newMaterials, ref finalMaterials);

				// Clear generated materials no longer in use
				HEU_GeneratedOutput.ClearMaterialsNoLongerUsed(generatedOutput._outputData._renderMaterials, newMaterials);

				// Destroy children (components, materials, gameobjects)
				HEU_GeneratedOutput.DestroyGeneratedOutputChildren(generatedOutput);

				// Update cached generated materials
				generatedOutput._outputData._renderMaterials = newMaterials;

				MeshFilter meshFilter = HEU_GeneralUtility.GetOrCreateComponent<MeshFilter>(generatedOutput._outputData._gameObject);
				meshFilter.sharedMesh = newMesh;
				meshFilter.sharedMesh.RecalculateBounds();
				meshFilter.sharedMesh.UploadMeshData(!geoCache._isMeshReadWrite);

				MeshRenderer meshRenderer = HEU_GeneralUtility.GetOrCreateComponent<MeshRenderer>(generatedOutput._outputData._gameObject);
				meshRenderer.sharedMaterials = finalMaterials;
			}

			return bGeneratedMesh;
		}

		/// <summary>
		/// Generates LOD meshes from given GeoGroupMeshes.
		/// The outputGameObject will have a LODGroup component setup with each of the LOD mesh data.
		/// </summary>
		/// <param name="GeoGroupMeshes">List of LOD groups containing submesh data</param>
		/// <param name="geoCache">Contains geometry data</param>
		/// <param name="outputGameObject">GameObject to attach LODGroup to and child LOD meshes</param>
		/// <param name="defaultMaterialKey">The material key for default material</param>
		/// <returns>True if successfully generated meshes</returns>
		public static bool GenerateLODMeshesFromGeoGroups(HEU_SessionBase session, List<HEU_GeoGroup> GeoGroupMeshes, HEU_GenerateGeoCache geoCache,
			HEU_GeneratedOutput generatedOutput, int defaultMaterialKey, bool bGenerateUVs, bool bGenerateTangents, bool bGenerateNormals, bool bPartInstanced)
		{
			int numLODs = GeoGroupMeshes.Count;
			if(numLODs == 0)
			{
				return false;
			}

			// Sort the LOD groups alphabetically by group names
			GeoGroupMeshes.Sort();

			// Use default transition if user hasn't specified them. Sort by decreasing transition value (1 to 0)
			if(geoCache._LODTransitionValues == null || geoCache._LODTransitionValues.Length == 0)
			{
				geoCache._LODTransitionValues = new float[numLODs];
				for(int i = 0; i < numLODs; ++i)
				{
					geoCache._LODTransitionValues[i] = (float)(numLODs - (i + 1)) / (float)(numLODs + 1);
				}
			}
			else
			{
				if(geoCache._LODTransitionValues.Length < numLODs)
				{
					Debug.LogWarningFormat("Expected {0} values for LOD transition {1} attribute. Got {2} instead.", numLODs, HEU_Defines.HEU_UNITY_LOD_TRANSITION_ATTR, geoCache._LODTransitionValues.Length);
					System.Array.Resize(ref geoCache._LODTransitionValues, numLODs);
				}

				// Normalize to 0 to 1 if above 1. Presume that the user was using 0 to 100 range.
				for(int i = 0; i < numLODs; ++i)
				{
					geoCache._LODTransitionValues[i] = geoCache._LODTransitionValues[i] > 1f ? geoCache._LODTransitionValues[i] / 100f : geoCache._LODTransitionValues[i];
				}

				System.Array.Sort(geoCache._LODTransitionValues, (a, b) => b.CompareTo(a));
			}

			List<HEU_GeneratedOutputData> newGeneratedChildOutputs = new List<HEU_GeneratedOutputData>();

			// For each LOD, generate its mesh, then create a new child GameObject, add mesh, material, and renderer.
			LOD[] lods = new LOD[numLODs];
			for(int l = 0; l < numLODs; ++l)
			{
				Mesh newMesh = null;
				Material[] newMaterials = null;
				bool bGenerated = GenerateMeshFromGeoGroup(session, GeoGroupMeshes[l], geoCache, defaultMaterialKey, bGenerateUVs, bGenerateTangents, bGenerateNormals,
					bPartInstanced, out newMesh, out newMaterials);

				if(bGenerated)
				{
					HEU_GeneratedOutputData childOutput = null;

					// Get final materials after comparing previously genereated, newly generated, and user override (currently set on MeshRenderer).
					Material[] finalMaterials = null;
					if (l < generatedOutput._childOutputs.Count)
					{
						childOutput = generatedOutput._childOutputs[l];
						newGeneratedChildOutputs.Add(childOutput);

						GetFinalMaterialsFromComparingNewWithPrevious(childOutput._gameObject, childOutput._renderMaterials, newMaterials, ref finalMaterials);

						// Clear generated materials no longer in use
						HEU_GeneratedOutput.ClearMaterialsNoLongerUsed(childOutput._renderMaterials, newMaterials);
					}
					else
					{
						// No child output found, so setup new child output

						childOutput = new HEU_GeneratedOutputData();
						childOutput._gameObject = new GameObject(GeoGroupMeshes[l]._groupName);
						newGeneratedChildOutputs.Add(childOutput);

						finalMaterials = newMaterials;

						Transform childTransform = childOutput._gameObject.transform;
						childTransform.parent = generatedOutput._outputData._gameObject.transform;
						childTransform.localPosition = Vector3.zero;
						childTransform.localRotation = Quaternion.identity;
						childTransform.localScale = Vector3.one;
					}

					childOutput._renderMaterials = newMaterials;

					MeshFilter meshFilter = HEU_GeneralUtility.GetOrCreateComponent<MeshFilter>(childOutput._gameObject);
					meshFilter.sharedMesh = newMesh;

					if (!geoCache._tangentAttrInfo.exists && bGenerateTangents)
					{
						HEU_GeometryUtility.CalculateMeshTangents(meshFilter.sharedMesh);
					}

					meshFilter.sharedMesh.UploadMeshData(!geoCache._isMeshReadWrite);

					MeshRenderer meshRenderer = HEU_GeneralUtility.GetOrCreateComponent<MeshRenderer>(childOutput._gameObject);
					meshRenderer.sharedMaterials = finalMaterials;

					float screenThreshold = geoCache._LODTransitionValues[l];
					//Debug.Log("Threshold: " + screenThreshold + " for " + GeoGroupMeshes[l]._groupName);
					lods[l] = new LOD(screenThreshold, new MeshRenderer[] { meshRenderer });
				}
				else
				{
					Debug.LogError("Failed to create LOD mesh with group name: " + GeoGroupMeshes[l]._groupName);
					return false;
				}
			}

			// Destroy and remove extra LOD children previously generated
			int numExistingChildren = generatedOutput._childOutputs.Count;
			if (numLODs < numExistingChildren)
			{
				for (int i = numLODs; i < numExistingChildren; ++i)
				{
					HEU_GeneratedOutput.DestroyGeneratedOutputData(generatedOutput._childOutputs[i], true);
					generatedOutput._childOutputs[i] = null;
				}
			}

			// Update generated output children list
			generatedOutput._childOutputs = newGeneratedChildOutputs;

			// Apply the LOD Group with its LOD meshes to the output gameobject
			LODGroup lodGroup = generatedOutput._outputData._gameObject.GetComponent<LODGroup>();
			if (lodGroup == null)
			{
				// First clean up generated components since this doesn't have a LOD Group.
				// The assumption here is that this might have been previously a normal mesh output, not an LOD Group
				// so we need to remove the extra components.
				HEU_GeneratedOutput.ClearGeneratedMaterialReferences(generatedOutput._outputData);
				HEU_GeneratedOutput.DestroyAllGeneratedColliders(generatedOutput._outputData);
				HEU_GeneralUtility.DestroyGeneratedMeshMaterialsLODGroups(generatedOutput._outputData._gameObject, true);
				HEU_GeneralUtility.DestroyGeneratedComponents(generatedOutput._outputData._gameObject);

				lodGroup = HEU_GeneralUtility.GetOrCreateComponent<LODGroup>(generatedOutput._outputData._gameObject);
			}
			
			lodGroup.SetLODs(lods);
			lodGroup.RecalculateBounds();

			return true;
		}

		/// <summary>
		/// Generate mesh from given GeoGroup containing submesh data.
		/// Combines submeshes to form a single mesh, along with materials for it.
		/// </summary>
		/// <param name="GeoGroup">Contains submesh data</param>
		/// <param name="geoCache">Contains geometry data</param>
		/// <param name="newMesh">Single mesh to generate from submeshes</param>
		/// <param name="newMaterials">Array of materials for the generated mesh</param>
		/// <returns>True if successfully created the mesh</returns>
		public static bool GenerateMeshFromGeoGroup(HEU_SessionBase session, HEU_GeoGroup GeoGroup, HEU_GenerateGeoCache geoCache,
			int defaultMaterialKey, bool bGenerateUVs, bool bGenerateTangents, bool bGenerateNormals, bool bPartInstanced,
			out Mesh newMesh, out Material[] newMaterials)
		{
			newMesh = null;
			newMaterials = null;
			int numSubMeshes = GeoGroup._subMeshesMap.Keys.Count;

			bool bGenerated = false;
			if (numSubMeshes > 0)
			{
				if (!geoCache._normalAttrInfo.exists && bGenerateNormals)
				{
					// Normal calculation
					// Go throuch each vertex for the entire geometry and calculate the normal vector based on connected
					// vertices. This includes vertex connections between submeshes so we should get smooth transitions across submeshes.

					int numSharedNormals = GeoGroup._sharedNormalIndices.Length;
					for (int a = 0; a < numSharedNormals; ++a)
					{
						for (int b = 0; b < GeoGroup._sharedNormalIndices[a].Count; ++b)
						{
							Vector3 sumNormal = new Vector3();
							HEU_VertexEntry leftEntry = GeoGroup._sharedNormalIndices[a][b];
							HEU_MeshData leftSubMesh = GeoGroup._subMeshesMap[leftEntry._meshKey];

							List<HEU_VertexEntry> rightList = GeoGroup._sharedNormalIndices[a];
							for (int c = 0; c < rightList.Count; ++c)
							{
								HEU_VertexEntry rightEntry = rightList[c];
								HEU_MeshData rightSubMesh = GeoGroup._subMeshesMap[rightEntry._meshKey];

								if (leftEntry._vertexIndex == rightEntry._vertexIndex)
								{
									sumNormal += rightSubMesh._triangleNormals[rightEntry._normalIndex];
								}
								else
								{
									float dot = Vector3.Dot(leftSubMesh._triangleNormals[leftEntry._normalIndex],
										rightSubMesh._triangleNormals[rightEntry._normalIndex]);
									if (dot >= geoCache._normalCosineThreshold)
									{
										sumNormal += rightSubMesh._triangleNormals[rightEntry._normalIndex];
									}
								}
							}

							leftSubMesh._normals[leftEntry._vertexIndex] = sumNormal.normalized;
						}
					}
				}


				// Go through each valid submesh data and upload into a CombineInstance for combining.
				// Each CombineInstance represents a submesh in the final mesh.
				// And each submesh in that final mesh corresponds to a material.

				// Filter out only the submeshes with valid geometry
				List<Material> validMaterials = new List<Material>();
				List<int> validSubmeshes = new List<int>();

				foreach (KeyValuePair<int, HEU_MeshData> meshPair in GeoGroup._subMeshesMap)
				{
					HEU_MeshData meshData = meshPair.Value;
					if (meshData._indices.Count > 0)
					{
						int materialKey = meshPair.Key;

						// Find the material or create it
						HEU_MaterialData materialData = null;

						HEU_UnityMaterialInfo unityMaterialInfo = null;
						if (geoCache._unityMaterialInfos.TryGetValue(materialKey, out unityMaterialInfo))
						{
							if (!geoCache._materialIDToDataMap.TryGetValue(materialKey, out materialData))
							{
								// Create the material
								materialData = HEU_MaterialFactory.CreateUnitySubstanceMaterialData(materialKey, unityMaterialInfo._unityMaterialPath, unityMaterialInfo._substancePath, unityMaterialInfo._substanceIndex, geoCache._materialCache, geoCache._assetCacheFolderPath);
								geoCache._materialIDToDataMap.Add(materialData._materialKey, materialData);
							}
						}
						else if (!geoCache._materialIDToDataMap.TryGetValue(materialKey, out materialData))
						{
							if (materialKey == defaultMaterialKey)
							{
								materialData = HEU_MaterialFactory.GetOrCreateDefaultMaterialInCache(session, geoCache.GeoID, geoCache.PartID, false, geoCache._materialCache, geoCache._assetCacheFolderPath);
							}
							else
							{
								materialData = HEU_MaterialFactory.CreateHoudiniMaterialData(session, geoCache.AssetID, materialKey, geoCache.GeoID, geoCache.PartID, geoCache._materialCache, geoCache._assetCacheFolderPath);
							}
						}

						if (materialData != null)
						{
							validSubmeshes.Add(meshPair.Key);
							validMaterials.Add(materialData._material);

							if (materialData != null && bPartInstanced)
							{
								// Handle GPU instancing on material for instanced meshes

								if (materialData._materialSource != HEU_MaterialData.Source.UNITY && materialData._materialSource != HEU_MaterialData.Source.SUBSTANCE)
								{
									// Always enable GPU instancing for material generated from Houdini
									HEU_MaterialFactory.EnableGPUInstancing(materialData._material);
								}
							}
						}
					}
				}

				int validNumSubmeshes = validSubmeshes.Count;
				if (validNumSubmeshes == 1)
				{
					// Single mesh creation. Not using combiner path below due to
					// it always creating triangles (i.e. quads are never created).
					newMesh = CreateMeshFromMeshData(GeoGroup._subMeshesMap[validSubmeshes[0]],
						bGenerateUVs, bGenerateNormals, geoCache._meshIndexFormat);
				}
				else if (validNumSubmeshes > 1)
				{
					// Use CombineInstance for multiple submeshes.

					bool bHasQuads = false;
					for (int submeshIndex = 0; submeshIndex < validNumSubmeshes; ++submeshIndex)
					{
						if (GeoGroup._subMeshesMap[validSubmeshes[submeshIndex]]._meshTopology == MeshTopology.Quads)
						{
							bHasQuads = true;
							break;
						}
					}

					if (bHasQuads)
					{
						// Quads need to be handled specially due to crash in Unity when using CombineInstace with
						// quad topology submeshes.
						newMesh = CombineQuadMeshes(GeoGroup._subMeshesMap, validSubmeshes, bGenerateNormals);
					}
					else
					{
						// Otherwise regular CombineInstance path for triangles
						newMesh = CombineMeshes(GeoGroup._subMeshesMap, validSubmeshes, bGenerateUVs, bGenerateNormals, geoCache._meshIndexFormat);
					}
				}

				newMesh.name = geoCache._partName + "_mesh";

				if (!geoCache._tangentAttrInfo.exists && bGenerateTangents)
				{
					HEU_GeometryUtility.CalculateMeshTangents(newMesh);
				}

				newMaterials = validMaterials.ToArray();

				bGenerated = true;
			}

			return bGenerated;
		}

		/// <summary>
		/// Returns a new mesh that is created from combining the meshes given in the subMeshesMap
		/// specified by submeshIndices, specifically for quad topology meshes.
		/// Unity crashes when using CombineInstance with quad topology submeshes,
		/// so this works around the issue, though slower.
		/// Note that this does not generate UVs.
		/// </summary>
		/// <param name="subMeshesMap">Map of submeshes to indices</param>
		/// <param name="subMeshIndices">List of indices of submeshes to combine</param>
		/// <param name="bGenerateNormals">True if want to manually generate normals</param>
		/// <returns>New combined mesh with submeshes</returns>
		public static Mesh CombineQuadMeshes(Dictionary<int, HEU_MeshData> subMeshesMap, List<int> subMeshIndices,
			bool bGenerateNormals)
		{
			int numSubmeshes = subMeshIndices.Count;

			Mesh mesh = new Mesh();
			mesh.subMeshCount = numSubmeshes;

			List<Vector3> vertices = new List<Vector3>();
			List<Color32> colors = new List<Color32>();
			List<Vector3> normals = new List<Vector3>();
			List<Vector4> tangents = new List<Vector4>();

			List<Vector4>[] uvs = new List<Vector4>[HEU_Defines.HAPI_MAX_UVS];
			for (int u = 0; u < HEU_Defines.HAPI_MAX_UVS; ++u)
			{
				uvs[u] = new List<Vector4>();
			}

			int offset = 0;

			// Go through all submeshes, and combine all vertex attributes 
			// (position, colors, normals, and tangents).
			// Reindex the indices with offsets.
			for (int submeshIndex = 0; submeshIndex < numSubmeshes; ++submeshIndex)
			{
				HEU_MeshData meshData = subMeshesMap[subMeshIndices[submeshIndex]];

				vertices.AddRange(meshData._vertices);
				colors.AddRange(meshData._colors);
				normals.AddRange(meshData._normals);
				tangents.AddRange(meshData._tangents);

				for (int u = 0; u < HEU_Defines.HAPI_MAX_UVS; ++u)
				{
					if (submeshIndex == 0)
					{
						uvs[u] = new List<Vector4>();
					}

					if (meshData._uvs[u].Count > 0)
					{
						uvs[u].AddRange(meshData._uvs[u]);
					}
				}

				// Reindex indices with offset
				if (offset > 0)
				{
					int numIndices = meshData._indices.Count;
					for (int i = 0; i < numIndices; i++)
					{
						meshData._indices[i] += offset;
					}
				}

				// Reindex indices offset
				offset = vertices.Count;
				//Debug.LogFormat("Adding vertices: {0}. Total: {1}", vertices.Count, offset);
			}

			mesh.SetVertices(vertices);
			mesh.SetColors(colors);
			mesh.SetNormals(normals);
			mesh.SetNormals(normals);

			for (int u = 0; u < HEU_Defines.HAPI_MAX_UVS; ++u)
			{
				if (uvs[u].Count > 0)
				{
					mesh.SetUVs(u, uvs[u]);
				}
			}

			for (int submeshIndex = 0; submeshIndex < numSubmeshes; ++submeshIndex)
			{
				HEU_MeshData meshData = subMeshesMap[subMeshIndices[submeshIndex]];
				mesh.SetIndices(meshData._indices.ToArray(), meshData._meshTopology, submeshIndex);
			}

			if (bGenerateNormals && normals.Count == 0)
			{
				// Calculate normals since they weren't provided Houdini
				mesh.RecalculateNormals();
			}

			mesh.RecalculateBounds();

			return mesh;
		}

		/// <summary>
		/// Returns a new mesh that is created from combining the meshes given in the subMeshesMap
		/// specified by submeshIndices. Uses Unity's CombineInstance.
		/// Quad topology meshes should not be called here due to Unity crash,
		/// rather should use CombineQuadMeshes.
		/// </summary>
		/// <param name="subMeshesMap">Map of submeshes to indices</param>
		/// <param name="submeshIndices">Indices of submeshes to combine</param>
		/// <param name="bGenerateUVs">True to generate UVs manually</param>
		/// <param name="bGenerateNormals">True to generate normals manually</param>
		/// <param name="meshIndexFormat">The mesh IndexFormat to set</param>
		/// <returns>New mesh with combined submeshes</returns>
		public static Mesh CombineMeshes(Dictionary<int, HEU_MeshData> subMeshesMap, 
			List<int> submeshIndices,
			bool bGenerateUVs, bool bGenerateNormals, 
			HEU_MeshIndexFormat meshIndexFormat)
		{
			int numSubmeshes = submeshIndices.Count;

			CombineInstance[] meshCombiner = new CombineInstance[numSubmeshes];
			for (int submeshIndex = 0; submeshIndex < numSubmeshes; ++submeshIndex)
			{
				CombineInstance combine = new CombineInstance();

				HEU_MeshData meshData = subMeshesMap[submeshIndices[submeshIndex]];
				if (meshData._meshTopology == MeshTopology.Quads)
				{
					Debug.LogErrorFormat("Quad topology meshes should use CombineQuadMeshes!");
				}

				combine.mesh = CreateMeshFromMeshData(meshData, bGenerateUVs, bGenerateNormals, meshIndexFormat);

				combine.transform = Matrix4x4.identity;
				combine.mesh.RecalculateBounds();
				combine.subMeshIndex = 0;

				//Debug.LogFormat("Number of submeshes {0}", combine.mesh.subMeshCount);

				meshCombiner[submeshIndex] = combine;
			}

			Mesh newMesh = new Mesh();
			meshIndexFormat.SetFormatForMesh(newMesh);
			newMesh.CombineMeshes(meshCombiner, false, false);

			return newMesh;
		}

		/// <summary>
		/// Returns a new Mesh created by data from submesh.
		/// </summary>
		/// <param name="submesh">Contains geometry data for mesh</param>
		/// <param name="bGenerateUVs">True to generate UVs manually</param>
		/// <param name="bGenerateNormals">True to generate normals manually</param>
		/// <param name="meshIndexFormat">The mesh IndexFormat to set</param>
		/// <returns>New mesh</returns>
		public static Mesh CreateMeshFromMeshData(HEU_MeshData submesh,
			bool bGenerateUVs, bool bGenerateNormals, 
			HEU_MeshIndexFormat meshIndexFormat)
		{
			Mesh mesh = new Mesh();

			meshIndexFormat.SetFormatForMesh(mesh);

			mesh.SetVertices(submesh._vertices);

			mesh.SetIndices(submesh._indices.ToArray(), submesh._meshTopology, 0);

			if (submesh._colors.Count > 0)
			{
				mesh.SetColors(submesh._colors);
			}

			if (submesh._normals.Count > 0)
			{
				mesh.SetNormals(submesh._normals);
			}

			if (submesh._tangents.Count > 0)
			{
				mesh.SetTangents(submesh._tangents);
			}

			if (bGenerateUVs)
			{
				// TODO: revisit to test this out

				if (submesh._meshTopology == MeshTopology.Triangles)
				{
					Vector2[] generatedUVs = HEU_GeometryUtility.GeneratePerTriangle(mesh);
					if (generatedUVs != null)
					{
						mesh.uv = generatedUVs;
					}
				}
				else
				{
					Debug.LogWarningFormat("Generating UVs for Quad topology mesh is not supported!");
				}
			}
			else if (submesh._uvs[0].Count > 0)
			{
				mesh.SetUVs(0, submesh._uvs[0]);
			}

			for (int u = 1; u < HEU_Defines.HAPI_MAX_UVS; ++u)
			{
				if (submesh._uvs[u].Count > 0)
				{
					mesh.SetUVs(u, submesh._uvs[u]);
				}
			}

			if (bGenerateNormals && submesh._normals.Count == 0)
			{
				// Calculate normals since they weren't provided Houdini
				mesh.RecalculateNormals();
			}

			mesh.RecalculateBounds();

			return mesh;
		}

		/// <summary>
		/// Transfer given attribute values, based on owner type, into vertex attribute values
		/// for the given group of vertices.
		/// </summary>
		/// <param name="groupVertexList">Vertex indices in group to transfer attributes for</param>
		/// <param name="allFaceCounts">Face counts of faces in entire mesh</param>
		/// <param name="groupFaces">Face indices in the group</param>
		/// <param name="groupVertexOffset">Offsets of the vertex indices in the group</param>
		/// <param name="attribInfo">Attribute to parse</param>
		/// <param name="inData">Given attribute's values</param>
		/// <param name="outData">Converted vertex attribute values</param>
		public static void TransferRegularAttributesToVertices(int[] groupVertexList, int[] allFaceCounts, 
			List<int> groupFaces, List<int> groupVertexOffset,
			ref HAPI_AttributeInfo attribInfo, float[] inData, ref float[] outData)
		{
			if (attribInfo.exists && attribInfo.tupleSize > 0)
			{
				int wedgeCount = groupVertexList.Length;

				// Re-indexed wedges
				outData = new float[wedgeCount * attribInfo.tupleSize];

				int numFaces = groupFaces.Count;
				int groupFace = 0;
				int faceCount = 0;
				int primIndex = 0;
				int positionIndex = 0;
				for (int faceIndex = 0; faceIndex < numFaces; faceIndex++)
				{
					groupFace = groupFaces[faceIndex];
					faceCount = allFaceCounts[groupFace];

					for (int v = 0; v < faceCount; v++)
					{
						// Use the group's vertex offset for this face
						int vertexFaceIndex = groupVertexOffset[faceIndex] + v;

						// groupVertexList contains -1 for unused indices, and > 0 for used
						if (groupVertexList[vertexFaceIndex] == -1)
						{
							continue;
						}

						float value = 0;
						for (int attribIndex = 0; attribIndex < attribInfo.tupleSize; ++attribIndex)
						{
							switch (attribInfo.owner)
							{
								case HAPI_AttributeOwner.HAPI_ATTROWNER_POINT:
								{
									positionIndex = groupVertexList[vertexFaceIndex];
									value = inData[positionIndex * attribInfo.tupleSize + attribIndex];
									break;
								}
								case HAPI_AttributeOwner.HAPI_ATTROWNER_PRIM:
								{
									primIndex = vertexFaceIndex / faceCount;
									value = inData[primIndex * attribInfo.tupleSize + attribIndex];
									break;
								}
								case HAPI_AttributeOwner.HAPI_ATTROWNER_DETAIL:
								{
									value = inData[attribIndex];
									break;
								}
								case HAPI_AttributeOwner.HAPI_ATTROWNER_VERTEX:
								{
									value = inData[vertexFaceIndex * attribInfo.tupleSize + attribIndex];
									break;
								}
								default:
								{
									Debug.LogAssertion("Unsupported attribute owner " + attribInfo.owner);
									continue;
								}
							}

							int outIndex = vertexFaceIndex * attribInfo.tupleSize + attribIndex;
							outData[outIndex] = value;
						}
					}
				}
			}
		}


		/// <summary>
		/// Generate mesh for the given gameObject with the populated geoCache data.
		/// Splits vertices so that each triangle will have unique (non-shared) vertices.
		/// Can be invoked on non-main thread as it doesn't use any Unity main thread-only APIs.
		/// </summary>
		/// <returns>True if successfully generated mesh for gameObject</returns>
		public static bool GenerateGeoGroupUsingGeoCacheVertices(HEU_SessionBase session, HEU_GenerateGeoCache geoCache,
			bool bGenerateUVs, bool bGenerateTangents, bool bGenerateNormals, bool bUseLODGroups, bool bPartInstanced,
			out List<HEU_GeoGroup> LODGroupMeshes, out int defaultMaterialKey)
		{
#if HEU_PROFILER_ON
			float generateMeshTime = Time.realtimeSinceStartup;
#endif

			string collisionGroupName = HEU_PluginSettings.CollisionGroupName;
			string renderCollisionGroupName = HEU_PluginSettings.RenderedCollisionGroupName;

			string lodName = HEU_Defines.HEU_DEFAULT_LOD_NAME;

			// Stores submesh data based on material key (ie. a submesh for each unique material)

			// Unity requires that if using multiple materials in the same GameObject, then we
			// need to create corresponding number of submeshes as materials.
			// So we'll create a submesh for each material in use. 
			// Each submesh will have a list of vertices and their attributes which
			// we'll collect in a helper class (HEU_MeshData).
			// Once we collected all the submesh data, we create a CombineInstance for each
			// submesh, then combine it while perserving the submeshes.

			LODGroupMeshes = new List<HEU_GeoGroup>();
			HEU_GeoGroup defaultMainLODGroup = null;

			string defaultMaterialName = HEU_MaterialFactory.GenerateDefaultMaterialName(geoCache.GeoID, geoCache.PartID);
			defaultMaterialKey = HEU_MaterialFactory.MaterialNameToKey(defaultMaterialName);

			int singleFaceUnityMaterialKey = HEU_Defines.HEU_INVALID_MATERIAL;
			int singleFaceHoudiniMaterialKey = HEU_Defines.HEU_INVALID_MATERIAL;

			// Now go through each group data and acquire the vertex data.
			// We'll create the collider mesh rightaway and assign to the gameobject.
			int numCollisionMeshes = 0;
			foreach (KeyValuePair<string, int[]> groupSplitFacesPair in geoCache._groupSplitVertexIndices)
			{
				string groupName = groupSplitFacesPair.Key;
				int[] groupVertexList = groupSplitFacesPair.Value;

				List<int> groupFaces = geoCache._groupSplitFaceIndices[groupName];
				List<int> groupVertexOffset = geoCache._groupVertexOffsets[groupName];

				bool bIsCollidable = groupName.Contains(collisionGroupName);
				bool bIsRenderCollidable = groupName.Contains(renderCollisionGroupName);
				if (bIsCollidable || bIsRenderCollidable)
				{
					if (numCollisionMeshes > 0)
					{
						Debug.LogWarningFormat("More than 1 collision mesh detected for part {0}.\nOnly a single collision mesh is supported per part.", geoCache._partName);
					}

					HEU_ColliderInfo colliderInfo = new HEU_ColliderInfo();

					colliderInfo._isTrigger = groupName.Contains(HEU_Defines.DEFAULT_COLLISION_TRIGGER);

					if (geoCache._partInfo.type == HAPI_PartType.HAPI_PARTTYPE_BOX)
					{
						// Box collider

						HAPI_BoxInfo boxInfo = new HAPI_BoxInfo();
						if (session.GetBoxInfo(geoCache.GeoID, geoCache.PartID, ref boxInfo))
						{
							colliderInfo._colliderType = HEU_ColliderInfo.ColliderType.BOX;
							colliderInfo._colliderCenter = new Vector3(-boxInfo.center[0], boxInfo.center[1], boxInfo.center[2]);
							colliderInfo._colliderSize = new Vector3(boxInfo.size[0] * 2f, boxInfo.size[1] * 2f, boxInfo.size[2] * 2f);
							// TODO: Should we apply the box info rotation here to the box collider?
							//		 If so, it should be in its own gameobject?
						}
					}
					else if (geoCache._partInfo.type == HAPI_PartType.HAPI_PARTTYPE_SPHERE)
					{
						// Sphere collider

						HAPI_SphereInfo sphereInfo = new HAPI_SphereInfo();
						if (session.GetSphereInfo(geoCache.GeoID, geoCache.PartID, ref sphereInfo))
						{
							colliderInfo._colliderType = HEU_ColliderInfo.ColliderType.SPHERE;
							colliderInfo._colliderCenter = new Vector3(-sphereInfo.center[0], sphereInfo.center[1], sphereInfo.center[2]);
							colliderInfo._colliderRadius = sphereInfo.radius;
						}
					}
					else
					{
						// Mesh collider

						List<Vector3> collisionVertices = new List<Vector3>();
						for (int v = 0; v < groupVertexList.Length; ++v)
						{
							int index = groupVertexList[v];
							if (index >= 0 && index < geoCache._posAttr.Length)
							{
								collisionVertices.Add(new Vector3(-geoCache._posAttr[index * 3], geoCache._posAttr[index * 3 + 1], geoCache._posAttr[index * 3 + 2]));
								// TODO: also add bounds entry here
							}
						}

						int[] collisionIndices = new int[collisionVertices.Count];
						for (int i = 0; i < collisionIndices.Length; ++i)
						{
							collisionIndices[i] = i;
						}

						// Defer the mesh creation as this function can be invoked from non-main thread (e.g. HEU_GeoSync)
						colliderInfo._collisionGroupName = groupName;
						colliderInfo._collisionVertices = collisionVertices.ToArray();
						colliderInfo._collisionIndices = collisionIndices;
						colliderInfo._convexCollider = groupName.Contains(HEU_Defines.DEFAULT_CONVEX_COLLISION_GEO);
						colliderInfo._meshTopology = CalculateGroupMeshTopology(groupFaces, geoCache._faceCounts);

						HEU_ColliderInfo.ColliderType colliderType = HEU_ColliderInfo.ColliderType.MESH;

						if (groupName.StartsWith(HEU_Defines.DEFAULT_SIMPLE_COLLISION_GEO) 
							|| groupName.StartsWith(HEU_Defines.DEFAULT_SIMPLE_RENDERED_COLLISION_GEO))
						{
							if (groupName.EndsWith("box"))
							{
								colliderType = HEU_ColliderInfo.ColliderType.SIMPLE_BOX;
							}
							else if (groupName.EndsWith("sphere"))
							{
								colliderType = HEU_ColliderInfo.ColliderType.SIMPLE_SPHERE;
							}
							else if (groupName.EndsWith("capsule"))
							{
								colliderType = HEU_ColliderInfo.ColliderType.SIMPLE_CAPSULE;
							}
						}

						colliderInfo._colliderType = colliderType;
					}

					geoCache._colliderInfos.Add(colliderInfo);

					numCollisionMeshes++;
				}


				if (bIsCollidable && !bIsRenderCollidable)
				{
					continue;
				}

				// After this point, we'll be only processing renderable geometry

				HEU_GeoGroup currentLODGroup = null;

				// Add mesh data under LOD group if group name is a valid LOD name
				if (bUseLODGroups && groupName.StartsWith(lodName))
				{
					currentLODGroup = new HEU_GeoGroup();
					currentLODGroup._groupName = groupName;
					LODGroupMeshes.Add(currentLODGroup);

					if (!geoCache._normalAttrInfo.exists && bGenerateNormals)
					{
						currentLODGroup.SetupNormalIndices(groupVertexList.Length);
					}
				}
				else
				{
					// Any other group is added under the default group name
					if (defaultMainLODGroup == null)
					{
						defaultMainLODGroup = new HEU_GeoGroup();
						defaultMainLODGroup._groupName = HEU_Defines.HEU_DEFAULT_GEO_GROUP_NAME;
						LODGroupMeshes.Add(defaultMainLODGroup);

						if (!geoCache._normalAttrInfo.exists && bGenerateNormals)
						{
							defaultMainLODGroup.SetupNormalIndices(groupVertexList.Length);
						}
					}
					currentLODGroup = defaultMainLODGroup;
				}

				// Transfer indices for each attribute from the single large list into group lists

				float[] groupColorAttr = new float[0];
				HEU_GenerateGeoCache.TransferRegularAttributesToVertices(groupVertexList, 
					geoCache._faceCounts, groupFaces, groupVertexOffset, 
					ref geoCache._colorAttrInfo, geoCache._colorAttr, ref groupColorAttr);

				float[] groupAlphaAttr = new float[0];
				HEU_GenerateGeoCache.TransferRegularAttributesToVertices(groupVertexList, 
					geoCache._faceCounts, groupFaces, groupVertexOffset,
					ref geoCache._alphaAttrInfo, geoCache._alphaAttr, ref groupAlphaAttr);

				float[] groupNormalAttr = new float[0];
				HEU_GenerateGeoCache.TransferRegularAttributesToVertices(groupVertexList, 
					geoCache._faceCounts, groupFaces, groupVertexOffset, 
					ref geoCache._normalAttrInfo, geoCache._normalAttr, ref groupNormalAttr);

				float[] groupTangentsAttr = new float[0];
				HEU_GenerateGeoCache.TransferRegularAttributesToVertices(groupVertexList, 
					geoCache._faceCounts, groupFaces, groupVertexOffset, 
					ref geoCache._tangentAttrInfo, geoCache._tangentAttr, ref groupTangentsAttr);

				// Get maximum of 8 UV sets that Unity supports
				float[][] groupUVsAttr = new float[HEU_Defines.HAPI_MAX_UVS][];
				for(int u = 0; u < HEU_Defines.HAPI_MAX_UVS; ++u)
				{
					if (geoCache._uvsAttrInfo[u].exists)
					{
						groupUVsAttr[u] = new float[0];
						HEU_GenerateGeoCache.TransferRegularAttributesToVertices(groupVertexList, 
							geoCache._faceCounts, groupFaces, groupVertexOffset, 
							ref geoCache._uvsAttrInfo[u], geoCache._uvsAttr[u], ref groupUVsAttr[u]);
					}
				}

				// Unity mesh creation requires # of vertices must equal # of attributes (color, normal, uvs).
				// HAPI gives us point indices. Since our attributes are via vertex, we need to therefore
				// create new indices of vertices that correspond to our attributes.

				// To reindex, we go through each index, add each attribute corresponding to that index to respective lists.
				// Then we set the index of where we added those attributes as the new index.

				int numIndices = groupVertexList.Length;
				int numFaces = groupFaces.Count;
				int faceCount = 0;
				int groupFace = 0;
				int faceMaterialID = 0;
				int submeshID = HEU_Defines.HEU_INVALID_MATERIAL;
				HEU_MeshData subMeshData = null;

				bool bMixedTopolgoyError = false;

				for (int faceIndex = 0; faceIndex < numFaces; faceIndex++)
				{
					groupFace = groupFaces[faceIndex];
					faceCount = geoCache._faceCounts[groupFace];

					faceMaterialID = geoCache._houdiniMaterialIDs[groupFace];

					for (int v = 0; v < faceCount; v++)
					{
						// Use the group's vertex offset for this face
						int vertexFaceIndex = groupVertexOffset[faceIndex] + v;

						// groupVertexList contains -1 for unused indices, and > 0 for used
						if (groupVertexList[vertexFaceIndex] == -1)
						{
							continue;
						}

						// Get the submesh ID for this face. Depends on whether it is a Houdini or Unity material.
						// Using default material as failsafe
						submeshID = HEU_Defines.HEU_INVALID_MATERIAL;

						if (geoCache._unityMaterialAttrInfo.exists)
						{
							// This face might have a Unity or Substance material attribute. 
							// Formulate the submesh ID by combining the material attributes.

							if (geoCache._singleFaceUnityMaterial)
							{
								if (singleFaceUnityMaterialKey == HEU_Defines.HEU_INVALID_MATERIAL && geoCache._unityMaterialInfos.Count > 0)
								{
									// Use first material
									var unityMaterialMapEnumerator = geoCache._unityMaterialInfos.GetEnumerator();
									if (unityMaterialMapEnumerator.MoveNext())
									{
										singleFaceUnityMaterialKey = unityMaterialMapEnumerator.Current.Key;
									}
								}
								submeshID = singleFaceUnityMaterialKey;
							}
							else
							{
								int attrIndex = groupFace;
								if (geoCache._unityMaterialAttrInfo.owner == HAPI_AttributeOwner.HAPI_ATTROWNER_PRIM || geoCache._unityMaterialAttrInfo.owner == HAPI_AttributeOwner.HAPI_ATTROWNER_POINT)
								{
									if (geoCache._unityMaterialAttrInfo.owner == HAPI_AttributeOwner.HAPI_ATTROWNER_POINT)
									{
										attrIndex = groupVertexList[vertexFaceIndex];
									}

									string unityMaterialName = "";
									string substanceName = "";
									int substanceIndex = -1;
									submeshID = HEU_GenerateGeoCache.GetMaterialKeyFromAttributeIndex(geoCache, attrIndex, out unityMaterialName, out substanceName, out substanceIndex);
								}
								else
								{
									// (geoCache._unityMaterialAttrInfo.owner == HAPI_AttributeOwner.HAPI_ATTROWNER_DETAIL) should have been handled as geoCache._singleFaceMaterial above

									Debug.LogErrorFormat("Unity material attribute not supported for attribute type {0}!", geoCache._unityMaterialAttrInfo.owner);
								}
							}
						}

						if (submeshID == HEU_Defines.HEU_INVALID_MATERIAL)
						{
							// Check if has Houdini material assignment

							if (geoCache._houdiniMaterialIDs.Length > 0)
							{
								if (geoCache._singleFaceHoudiniMaterial)
								{
									if (singleFaceHoudiniMaterialKey == HEU_Defines.HEU_INVALID_MATERIAL)
									{
										singleFaceHoudiniMaterialKey = geoCache._houdiniMaterialIDs[0];
									}
									submeshID = singleFaceHoudiniMaterialKey;
								}
								else if (faceMaterialID > 0)
								{
									submeshID = faceMaterialID;
								}
							}

							if (submeshID == HEU_Defines.HEU_INVALID_MATERIAL)
							{
								// Use default material
								submeshID = defaultMaterialKey;
							}
						}

						// Find existing submesh for this vertex index or create new
						subMeshData = null;
						if (!currentLODGroup._subMeshesMap.TryGetValue(submeshID, out subMeshData))
						{
							subMeshData = new HEU_MeshData();

							if (faceCount == 3)
							{
								subMeshData._meshTopology = MeshTopology.Triangles;
							}
							else if (faceCount == 4)
							{
								subMeshData._meshTopology = MeshTopology.Quads;
							}
							else
							{
								Debug.LogErrorFormat("Unsupported number of vertices per face: {0}. Unable to set MeshTopology.", faceCount);
							}

							currentLODGroup._subMeshesMap.Add(submeshID, subMeshData);

							for (int u = 0; u < HEU_Defines.HAPI_MAX_UVS; ++u)
							{
								subMeshData._uvs[u] = new List<Vector4>();
							}
						}

						if (!bMixedTopolgoyError)
						{
							if (subMeshData._meshTopology == MeshTopology.Triangles && faceCount != 3)
							{
								bMixedTopolgoyError = true;
							}
							else if (subMeshData._meshTopology == MeshTopology.Quads && faceCount != 4)
							{
								bMixedTopolgoyError = true;
							}
						}

						int positionIndex = groupVertexList[vertexFaceIndex];

						// Position
						Vector3 position = new Vector3(-geoCache._posAttr[positionIndex * 3 + 0], geoCache._posAttr[positionIndex * 3 + 1], geoCache._posAttr[positionIndex * 3 + 2]);
						subMeshData._vertices.Add(position);

						// Color
						if (geoCache._colorAttrInfo.exists)
						{
							Color tempColor = new Color();
							tempColor.r = Mathf.Clamp01(groupColorAttr[vertexFaceIndex * geoCache._colorAttrInfo.tupleSize + 0]);
							tempColor.g = Mathf.Clamp01(groupColorAttr[vertexFaceIndex * geoCache._colorAttrInfo.tupleSize + 1]);
							tempColor.b = Mathf.Clamp01(groupColorAttr[vertexFaceIndex * geoCache._colorAttrInfo.tupleSize + 2]);

							if (geoCache._alphaAttrInfo.exists)
							{
								tempColor.a = Mathf.Clamp01(groupAlphaAttr[vertexFaceIndex]);
							}
							else if (geoCache._colorAttrInfo.tupleSize == 4)
							{
								tempColor.a = Mathf.Clamp01(groupColorAttr[vertexFaceIndex * geoCache._colorAttrInfo.tupleSize + 3]);
							}
							else
							{
								tempColor.a = 1f;
							}
							subMeshData._colors.Add(tempColor);
						}
						else
						{
							subMeshData._colors.Add(Color.white);
						}

						// Normal
						if (vertexFaceIndex < groupNormalAttr.Length)
						{
							// Flip the x
							Vector3 normal = new Vector3(-groupNormalAttr[vertexFaceIndex * 3 + 0], groupNormalAttr[vertexFaceIndex * 3 + 1], groupNormalAttr[vertexFaceIndex * 3 + 2]);
							subMeshData._normals.Add(normal);
						}
						else if (bGenerateNormals)
						{
							// We'll be calculating normals later
							subMeshData._normals.Add(Vector3.zero);
						}

						// Convert all UVs to vector format then add to submesh UVs array
						for (int u = 0; u < HEU_Defines.HAPI_MAX_UVS; ++u)
						{
							if (geoCache._uvsAttrInfo[u].exists && (vertexFaceIndex < groupUVsAttr[u].Length))
							{
								int uvSize = geoCache._uvsAttrInfo[u].tupleSize;
								switch (uvSize)
								{
									case 2: { subMeshData._uvs[u].Add(new Vector2(groupUVsAttr[u][vertexFaceIndex * 2 + 0], groupUVsAttr[u][vertexFaceIndex * 2 + 1])); break; }
									case 3: { subMeshData._uvs[u].Add(new Vector3(groupUVsAttr[u][vertexFaceIndex * 3 + 0], groupUVsAttr[u][vertexFaceIndex * 3 + 1], groupUVsAttr[u][vertexFaceIndex * 3 + 2])); break; }
									case 4: { subMeshData._uvs[u].Add(new Vector4(groupUVsAttr[u][vertexFaceIndex * 4 + 0], groupUVsAttr[u][vertexFaceIndex * 4 + 1], groupUVsAttr[u][vertexFaceIndex * 4 + 2], groupUVsAttr[u][vertexFaceIndex * 4 + 3])); break; }
								}
							}
						}

						// Tangents
						if (bGenerateTangents && vertexFaceIndex < groupTangentsAttr.Length)
						{
							Vector4 tangent = Vector4.zero;
							if (geoCache._tangentAttrInfo.tupleSize == 4)
							{
								tangent = new Vector4(-groupTangentsAttr[vertexFaceIndex * 4 + 0], groupTangentsAttr[vertexFaceIndex * 4 + 1], groupTangentsAttr[vertexFaceIndex * 4 + 2], groupTangentsAttr[vertexFaceIndex * 4 + 3]);
							}
							else if (geoCache._tangentAttrInfo.tupleSize == 3)
							{
								tangent = new Vector4(-groupTangentsAttr[vertexFaceIndex * 3 + 0], groupTangentsAttr[vertexFaceIndex * 3 + 1], groupTangentsAttr[vertexFaceIndex * 3 + 2], 1);
							}

							subMeshData._tangents.Add(tangent);
						}

						subMeshData._indices.Add(subMeshData._vertices.Count - 1);
						//Debug.LogFormat("Submesh index mat {0} count {1}", faceMaterialID, subMeshData._indices.Count);
					}

					if (!geoCache._normalAttrInfo.exists && bGenerateNormals 
						&& subMeshData != null && subMeshData._indices.Count >= 3)
					{
						// To generate normals after all the submeshes have been defined, we
						// calculate and store each triangle normal, along with the list
						// of connected vertices for each vertex

						int vertexFaceIndex = groupVertexOffset[faceIndex];

						int v0 = groupVertexList[vertexFaceIndex + 0];
						int v1 = groupVertexList[vertexFaceIndex + 1];
						int v2 = groupVertexList[vertexFaceIndex + 2];
						if (v0 >= 0 && v1 >= 0 && v2 >= 0)
						{
							int triIndex = subMeshData._indices.Count - faceCount;
							int i1 = subMeshData._indices[triIndex + 0];
							int i2 = subMeshData._indices[triIndex + 1];
							int i3 = subMeshData._indices[triIndex + 2];

							// Triangle normal
							Vector3 p1 = subMeshData._vertices[i2] - subMeshData._vertices[i1];
							Vector3 p2 = subMeshData._vertices[i3] - subMeshData._vertices[i1];
							Vector3 normal = Vector3.Cross(p1, p2).normalized;
							subMeshData._triangleNormals.Add(normal);
							int normalIndex = subMeshData._triangleNormals.Count - 1;

							// Connected vertices
							currentLODGroup._sharedNormalIndices[v0].Add(new HEU_VertexEntry(submeshID, i1, normalIndex));
							currentLODGroup._sharedNormalIndices[v1].Add(new HEU_VertexEntry(submeshID, i2, normalIndex));
							currentLODGroup._sharedNormalIndices[v2].Add(new HEU_VertexEntry(submeshID, i3, normalIndex));

							if (faceCount == 4 && groupVertexList[vertexFaceIndex + 3] >= 0)
							{
								// Add 4th vertex for quad
								int i4 = subMeshData._indices[triIndex + 3];
								currentLODGroup._sharedNormalIndices[groupVertexList[vertexFaceIndex + 3]].Add(new HEU_VertexEntry(submeshID, i4, normalIndex));
							}
						}
					}
				}

				if (bMixedTopolgoyError)
				{
					Debug.LogErrorFormat("Single mesh with group name {0} has mixed topology (triangles and quads) which is not supported. " +
						"Recommending splitting up the mesh or change Plugin Settings' Max Vertices Per Face to 3 to force triangles.",
						groupName);
				}
			}

#if HEU_PROFILER_ON
			Debug.LogFormat("GENERATE GEO GROUP TIME:: {0}", (Time.realtimeSinceStartup - generateMeshTime));
#endif

			return true;
		}

		/// <summary>
		/// Generate mesh for the given gameObject with the populated geoCache data.
		/// Only uses the points to generate the mesh, so vertices might be shared.
		/// Note that only point attributes are used (all other attributes ignored).
		/// Can be invoked on non-main thread as it doesn't use any Unity main thread-only APIs.
		/// </summary>
		/// <returns>True if successfully generated mesh for gameObject</returns>
		public static bool GenerateGeoGroupUsingGeoCachePoints(HEU_SessionBase session, HEU_GenerateGeoCache geoCache,
			 bool bGenerateUVs, bool bGenerateTangents, bool bGenerateNormals, bool bUseLODGroups, bool bPartInstanced,
			 out List<HEU_GeoGroup> LODGroupMeshes, out int defaultMaterialKey)
		{
#if HEU_PROFILER_ON
			float generateMeshTime = Time.realtimeSinceStartup;
#endif

			string collisionGroupName = HEU_PluginSettings.CollisionGroupName;
			string renderCollisionGroupName = HEU_PluginSettings.RenderedCollisionGroupName;

			string lodName = HEU_Defines.HEU_DEFAULT_LOD_NAME;

			// Stores submesh data based on material key (ie. a submesh for each unique material)

			// Unity requires that if using multiple materials in the same GameObject, then we
			// need to create corresponding number of submeshes as materials.
			// So we'll create a submesh for each material in use. 
			// Each submesh will have a list of vertices and their attributes which
			// we'll collect in a helper class (HEU_MeshData).
			// Once we collected all the submesh data, we create a CombineInstance for each
			// submesh, then combine it while perserving the submeshes.

			LODGroupMeshes = new List<HEU_GeoGroup>();
			HEU_GeoGroup defaultMainLODGroup = null;

			string defaultMaterialName = HEU_MaterialFactory.GenerateDefaultMaterialName(geoCache.GeoID, geoCache.PartID);
			defaultMaterialKey = HEU_MaterialFactory.MaterialNameToKey(defaultMaterialName);

			int singleFaceUnityMaterialKey = HEU_Defines.HEU_INVALID_MATERIAL;
			int singleFaceHoudiniMaterialKey = HEU_Defines.HEU_INVALID_MATERIAL;

			// Now go through each group data and acquire the vertex data.
			// We'll create the collider mesh rightaway and assign to the gameobject.
			int numCollisionMeshes = 0;
			foreach (KeyValuePair<string, int[]> groupSplitFacesPair in geoCache._groupSplitVertexIndices)
			{
				string groupName = groupSplitFacesPair.Key;
				int[] groupVertexList = groupSplitFacesPair.Value;

				List<int> groupFaces = geoCache._groupSplitFaceIndices[groupName];
				List<int> groupVertexOffset = geoCache._groupVertexOffsets[groupName];

				bool bIsCollidable = groupName.Contains(collisionGroupName);
				bool bIsRenderCollidable = groupName.Contains(renderCollisionGroupName);
				if (bIsCollidable || bIsRenderCollidable)
				{
					if (numCollisionMeshes > 0)
					{
						Debug.LogWarningFormat("More than 1 collision mesh detected for part {0}.\nOnly a single collision mesh is supported per part.", geoCache._partName);
					}

					HEU_ColliderInfo colliderInfo = new HEU_ColliderInfo();

					colliderInfo._isTrigger = groupName.Contains(HEU_Defines.DEFAULT_COLLISION_TRIGGER);

					if (geoCache._partInfo.type == HAPI_PartType.HAPI_PARTTYPE_BOX)
					{
						// Box collider

						HAPI_BoxInfo boxInfo = new HAPI_BoxInfo();
						if (session.GetBoxInfo(geoCache.GeoID, geoCache.PartID, ref boxInfo))
						{
							colliderInfo._colliderType = HEU_ColliderInfo.ColliderType.BOX;
							colliderInfo._colliderCenter = new Vector3(-boxInfo.center[0], boxInfo.center[1], boxInfo.center[2]);
							colliderInfo._colliderSize = new Vector3(boxInfo.size[0] * 2f, boxInfo.size[1] * 2f, boxInfo.size[2] * 2f);
							// TODO: Should we apply the box info rotation here to the box collider?
							//		 If so, it should be in its own gameobject?
						}
					}
					else if (geoCache._partInfo.type == HAPI_PartType.HAPI_PARTTYPE_SPHERE)
					{
						// Sphere collider

						HAPI_SphereInfo sphereInfo = new HAPI_SphereInfo();
						if (session.GetSphereInfo(geoCache.GeoID, geoCache.PartID, ref sphereInfo))
						{
							colliderInfo._colliderType = HEU_ColliderInfo.ColliderType.SPHERE;
							colliderInfo._colliderCenter = new Vector3(-sphereInfo.center[0], sphereInfo.center[1], sphereInfo.center[2]);
							colliderInfo._colliderRadius = sphereInfo.radius;
						}
					}
					else
					{
						// Mesh collider

						Dictionary<int, int> vertexIndextoMeshIndexMap = new Dictionary<HAPI_NodeId, HAPI_NodeId>();

						List<Vector3> collisionVertices = new List<Vector3>();
						List<int> collisionIndices = new List<int>();

						for (int v = 0; v < groupVertexList.Length; ++v)
						{
							int index = groupVertexList[v];
							if (index >= 0 && index < geoCache._posAttr.Length)
							{
								int meshIndex = -1;
								if (!vertexIndextoMeshIndexMap.TryGetValue(index, out meshIndex))
								{
									collisionVertices.Add(new Vector3(-geoCache._posAttr[index * 3], geoCache._posAttr[index * 3 + 1], geoCache._posAttr[index * 3 + 2]));

									meshIndex = collisionVertices.Count - 1;
									vertexIndextoMeshIndexMap[index] = meshIndex;
								}

								collisionIndices.Add(meshIndex);
							}
						}

						// Defer the mesh creation as this function can be invoked from non-main thread (e.g. HEU_GeoSync)
						colliderInfo._collisionGroupName = groupName;
						colliderInfo._collisionVertices = collisionVertices.ToArray();
						colliderInfo._collisionIndices = collisionIndices.ToArray();
						colliderInfo._colliderType = HEU_ColliderInfo.ColliderType.MESH;
						colliderInfo._convexCollider = groupName.Contains(HEU_Defines.DEFAULT_CONVEX_COLLISION_GEO);
						colliderInfo._meshTopology = CalculateGroupMeshTopology(groupFaces, geoCache._faceCounts);
					}

					geoCache._colliderInfos.Add(colliderInfo);

					numCollisionMeshes++;
				}


				if (bIsCollidable && !bIsRenderCollidable)
				{
					continue;
				}

				// After this point, we'll be only processing renderable geometry

				HEU_GeoGroup currentLODGroup = null;

				// Add mesh data under LOD group if group name is a valid LOD name
				if (bUseLODGroups && groupName.StartsWith(lodName))
				{
					currentLODGroup = new HEU_GeoGroup();
					currentLODGroup._groupName = groupName;
					LODGroupMeshes.Add(currentLODGroup);

					if (!geoCache._normalAttrInfo.exists && bGenerateNormals)
					{
						currentLODGroup.SetupNormalIndices(groupVertexList.Length);
					}
				}
				else
				{
					// Any other group is added under the default group name
					if (defaultMainLODGroup == null)
					{
						defaultMainLODGroup = new HEU_GeoGroup();
						defaultMainLODGroup._groupName = HEU_Defines.HEU_DEFAULT_GEO_GROUP_NAME;
						LODGroupMeshes.Add(defaultMainLODGroup);

						if (!geoCache._normalAttrInfo.exists && bGenerateNormals)
						{
							defaultMainLODGroup.SetupNormalIndices(groupVertexList.Length);
						}
					}
					currentLODGroup = defaultMainLODGroup;
				}

				// Unity mesh creation requires # of vertices must equal # of attributes (color, normal, uvs).
				// HAPI gives us point indices. Since our attributes are via vertex, we need to therefore
				// create new indices of vertices that correspond to our attributes.

				// To reindex, we go through each index, add each attribute corresponding to that index to respective lists.
				// Then we set the index of where we added those attributes as the new index.

				int numIndices = groupVertexList.Length;
				int numFaces = groupFaces.Count;
				int faceCount = 0;
				int groupFace = 0;
				int faceMaterialID = 0;
				int submeshID = HEU_Defines.HEU_INVALID_MATERIAL;
				HEU_MeshData subMeshData = null;

				bool bMixedTopolgoyError = false;

				for (int faceIndex = 0; faceIndex < numFaces; faceIndex++)
				{
					groupFace = groupFaces[faceIndex];
					faceCount = geoCache._faceCounts[groupFace];

					faceMaterialID = geoCache._houdiniMaterialIDs[faceIndex];

					for (int v = 0; v < faceCount; v++)
					{
						// Use the group's vertex offset for this face
						int vertexFaceIndex = groupVertexOffset[faceIndex] + v;

						// groupVertexList contains -1 for unused indices, and > 0 for used
						if (groupVertexList[vertexFaceIndex] == -1)
						{
							continue;
						}

						// Get the submesh ID for this face. Depends on whether it is a Houdini or Unity material.
						// Using default material as failsafe
						submeshID = HEU_Defines.HEU_INVALID_MATERIAL;

						if (geoCache._unityMaterialAttrInfo.exists)
						{
							// This face might have a Unity or Substance material attribute. 
							// Formulate the submesh ID by combining the material attributes.

							if (geoCache._singleFaceUnityMaterial)
							{
								if (singleFaceUnityMaterialKey == HEU_Defines.HEU_INVALID_MATERIAL && geoCache._unityMaterialInfos.Count > 0)
								{
									// Use first material
									var unityMaterialMapEnumerator = geoCache._unityMaterialInfos.GetEnumerator();
									if (unityMaterialMapEnumerator.MoveNext())
									{
										singleFaceUnityMaterialKey = unityMaterialMapEnumerator.Current.Key;
									}
								}
								submeshID = singleFaceUnityMaterialKey;
							}
							else
							{
								int attrIndex = faceIndex;
								if (geoCache._unityMaterialAttrInfo.owner == HAPI_AttributeOwner.HAPI_ATTROWNER_PRIM || geoCache._unityMaterialAttrInfo.owner == HAPI_AttributeOwner.HAPI_ATTROWNER_POINT)
								{
									if (geoCache._unityMaterialAttrInfo.owner == HAPI_AttributeOwner.HAPI_ATTROWNER_POINT)
									{
										attrIndex = groupVertexList[vertexFaceIndex];
									}

									string unityMaterialName = "";
									string substanceName = "";
									int substanceIndex = -1;
									submeshID = HEU_GenerateGeoCache.GetMaterialKeyFromAttributeIndex(geoCache, attrIndex, out unityMaterialName, out substanceName, out substanceIndex);
								}
								else
								{
									// (geoCache._unityMaterialAttrInfo.owner == HAPI_AttributeOwner.HAPI_ATTROWNER_DETAIL) should have been handled as geoCache._singleFaceMaterial above

									Debug.LogErrorFormat("Unity material attribute not supported for attribute type {0}!", geoCache._unityMaterialAttrInfo.owner);
								}
							}
						}

						if (submeshID == HEU_Defines.HEU_INVALID_MATERIAL)
						{
							// Check if has Houdini material assignment

							if (geoCache._houdiniMaterialIDs.Length > 0)
							{
								if (geoCache._singleFaceHoudiniMaterial)
								{
									if (singleFaceHoudiniMaterialKey == HEU_Defines.HEU_INVALID_MATERIAL)
									{
										singleFaceHoudiniMaterialKey = geoCache._houdiniMaterialIDs[0];
									}
									submeshID = singleFaceHoudiniMaterialKey;
								}
								else if (faceMaterialID > 0)
								{
									submeshID = faceMaterialID;
								}
							}

							if (submeshID == HEU_Defines.HEU_INVALID_MATERIAL)
							{
								// Use default material
								submeshID = defaultMaterialKey;
							}
						}

						// Find existing submesh for this vertex index or create new
						subMeshData = null;
						if (!currentLODGroup._subMeshesMap.TryGetValue(submeshID, out subMeshData))
						{
							subMeshData = new HEU_MeshData();

							if (faceCount == 3)
							{
								subMeshData._meshTopology = MeshTopology.Triangles;
							}
							else if (faceCount == 4)
							{
								subMeshData._meshTopology = MeshTopology.Quads;
							}
							else
							{
								Debug.LogErrorFormat("Unsupported number of vertices per face: {0}. Unable to set MeshTopology.", faceCount);
							}

							currentLODGroup._subMeshesMap.Add(submeshID, subMeshData);

							for (int u = 0; u < HEU_Defines.HAPI_MAX_UVS; ++u)
							{
								subMeshData._uvs[u] = new List<Vector4>();
							}
						}

						if (!bMixedTopolgoyError)
						{
							if (subMeshData._meshTopology == MeshTopology.Triangles && faceCount != 3)
							{
								bMixedTopolgoyError = true;
							}
							else if (subMeshData._meshTopology == MeshTopology.Quads && faceCount != 4)
							{
								bMixedTopolgoyError = true;
							}
						}

						int positionIndex = groupVertexList[vertexFaceIndex];

						int meshIndex = -1;
						if (!subMeshData._pointIndexToMeshIndexMap.TryGetValue(positionIndex, out meshIndex))
						{
							// Position
							Vector3 position = new Vector3(-geoCache._posAttr[positionIndex * 3 + 0], geoCache._posAttr[positionIndex * 3 + 1], geoCache._posAttr[positionIndex * 3 + 2]);
							subMeshData._vertices.Add(position);

							meshIndex = subMeshData._vertices.Count - 1;
							subMeshData._pointIndexToMeshIndexMap[positionIndex] = meshIndex;

							// Color
							if (geoCache._colorAttrInfo.exists && geoCache._colorAttrInfo.owner == HAPI_AttributeOwner.HAPI_ATTROWNER_POINT)
							{
								Color tempColor = new Color();

								tempColor.r = Mathf.Clamp01(geoCache._colorAttr[positionIndex * geoCache._colorAttrInfo.tupleSize + 0]);
								tempColor.g = Mathf.Clamp01(geoCache._colorAttr[positionIndex * geoCache._colorAttrInfo.tupleSize + 1]);
								tempColor.b = Mathf.Clamp01(geoCache._colorAttr[positionIndex * geoCache._colorAttrInfo.tupleSize + 2]);

								if (geoCache._alphaAttrInfo.exists)
								{
									tempColor.a = Mathf.Clamp01(geoCache._alphaAttr[positionIndex]);
								}
								else if (geoCache._colorAttrInfo.tupleSize == 4)
								{
									tempColor.a = Mathf.Clamp01(geoCache._colorAttr[positionIndex * geoCache._colorAttrInfo.tupleSize + 3]);
								}
								else
								{
									tempColor.a = 1f;
								}
								subMeshData._colors.Add(tempColor);
							}
							else
							{
								subMeshData._colors.Add(Color.white);
							}

							// Normal
							if (geoCache._normalAttrInfo.exists && geoCache._normalAttrInfo.owner == HAPI_AttributeOwner.HAPI_ATTROWNER_POINT && positionIndex < geoCache._normalAttr.Length)
							{
								// Flip the x
								Vector3 normal = new Vector3(-geoCache._normalAttr[positionIndex * 3 + 0], geoCache._normalAttr[positionIndex * 3 + 1], geoCache._normalAttr[positionIndex * 3 + 2]);
								subMeshData._normals.Add(normal);
							}

							// Convert all UVs to vector format

							for (int u = 0; u < HEU_Defines.HAPI_MAX_UVS; ++u)
							{
								if (geoCache._uvsAttrInfo[u].exists && geoCache._uvsAttrInfo[u].owner == HAPI_AttributeOwner.HAPI_ATTROWNER_POINT)
								{
									int uvSize = geoCache._uvsAttrInfo[u].tupleSize;
									switch (uvSize)
									{
										case 2: { subMeshData._uvs[u].Add(new Vector2(geoCache._uvsAttr[u][positionIndex * 2 + 0], geoCache._uvsAttr[u][positionIndex * 2 + 1])); break; }
										case 3: { subMeshData._uvs[u].Add(new Vector3(geoCache._uvsAttr[u][positionIndex * 3 + 0], geoCache._uvsAttr[u][positionIndex * 3 + 1], geoCache._uvsAttr[u][positionIndex * 3 + 2])); break; }
										case 4: { subMeshData._uvs[u].Add(new Vector4(geoCache._uvsAttr[u][positionIndex * 4 + 0], geoCache._uvsAttr[u][positionIndex * 4 + 1], geoCache._uvsAttr[u][positionIndex * 4 + 2], geoCache._uvsAttr[u][positionIndex * 4 + 3])); break; }
									}
								}
							}

							// Tangents
							if (bGenerateTangents && geoCache._tangentAttrInfo.exists && geoCache._tangentAttrInfo.owner == HAPI_AttributeOwner.HAPI_ATTROWNER_POINT && positionIndex < geoCache._tangentAttr.Length)
							{
								Vector4 tangent = Vector4.zero;
								if (geoCache._tangentAttrInfo.tupleSize == 4)
								{
									tangent = new Vector4(-geoCache._tangentAttr[positionIndex * 4 + 0], geoCache._tangentAttr[positionIndex * 4 + 1], geoCache._tangentAttr[positionIndex * 4 + 2], geoCache._tangentAttr[positionIndex * 4 + 3]);
								}
								else if (geoCache._tangentAttrInfo.tupleSize == 3)
								{
									tangent = new Vector4(-geoCache._tangentAttr[positionIndex * 3 + 0], geoCache._tangentAttr[positionIndex * 3 + 1], geoCache._tangentAttr[positionIndex * 3 + 2], 1);
								}

								subMeshData._tangents.Add(tangent);
							}
						}

						subMeshData._indices.Add(meshIndex);
						//Debug.LogFormat("Submesh index mat {0} count {1}", faceMaterialID, subMeshData._indices.Count);
					}
				}

				if (bMixedTopolgoyError)
				{
					Debug.LogErrorFormat("Single mesh with group name {0} has mixed topology (triangles and quads) which is not supported. " +
						"Recommending splitting up the mesh or change Plugin Settings' Max Vertices Per Face to 3 to force triangles.",
						groupName);
				}
			}

#if HEU_PROFILER_ON
			Debug.LogFormat("GENERATE GEO GROUP TIME:: {0}", (Time.realtimeSinceStartup - generateMeshTime));
#endif

			return true;
		}

		public static MeshTopology CalculateGroupMeshTopology(List<int> groupFaces, int[] allFaceCounts)
		{
			// Set collider mesh topology based on face counts in the group
			int faceIndex = 0;
			int groupFaceCount = 0;
			bool bMixedTopologyError = false;
			for (int i = 0; i < groupFaces.Count; ++i)
			{
				faceIndex = groupFaces[i];
				if (groupFaceCount == 0)
				{
					// First count
					groupFaceCount = allFaceCounts[faceIndex];
				}
				else if (groupFaceCount != allFaceCounts[faceIndex])
				{
					bMixedTopologyError = true;
					break;
				}
			}

			if (bMixedTopologyError)
			{
				Debug.LogErrorFormat("Group mesh has mixed mesh topology (triangles and quads) which is not supported.");
			}

			MeshTopology meshTopology = MeshTopology.Triangles;
			if (groupFaceCount == 4)
			{
				meshTopology = MeshTopology.Quads;
			}
			else if (groupFaceCount != 3)
			{
				Debug.LogErrorFormat("Unsupported mesh topology for collider mesh.");
			}

			return meshTopology;
		}
	}

}   // HoudiniEngineUnity
						 