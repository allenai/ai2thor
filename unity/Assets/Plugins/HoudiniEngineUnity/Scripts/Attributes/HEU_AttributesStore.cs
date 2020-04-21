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

using UnityEngine;
using System.Collections.Generic;


namespace HoudiniEngineUnity
{
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Typedefs (copy these from HEU_Common.cs)
	using HAPI_NodeId = System.Int32;
	using HAPI_PartId = System.Int32;
	using HAPI_StringHandle = System.Int32;

	/// <summary>
	/// Contains all the attributes for an editable node (part).
	/// Addtionally contains attribute-editing tools data such
	/// as temporary mesh and collider.
	/// </summary>
	public class HEU_AttributesStore : ScriptableObject
	{
		//	DATA ------------------------------------------------------------------------------------------------------

		[SerializeField]
		private HAPI_NodeId _geoID;

		public HAPI_NodeId GeoID { get { return _geoID; } }

		[SerializeField]
		private HAPI_PartId _partID;

		public HAPI_PartId PartID { get { return _partID; } }

		[SerializeField]
		private string _geoName;

		public string GeoName { get { return _geoName; } }

		[SerializeField]
		private List<HEU_AttributeData> _attributeDatas = new List<HEU_AttributeData>();

		[SerializeField]
		private bool _hasColorAttribute;

		public bool HasColorAttribute() { return _hasColorAttribute; }

		[SerializeField]
		private Material _localMaterial;

		[SerializeField]
		private Transform _outputTransform;

		public Transform OutputTransform { get { return _outputTransform; } }

		private Vector3[] _positionAttributeValues = new Vector3[0];

		private int[] _vertexIndices = new int[0];

		[SerializeField]
		private GameObject _outputGameObject;

		[SerializeField]
		private Mesh _outputMesh;

		public Mesh OutputMesh { get { return _outputMesh; } }

		[SerializeField]
		private Material[] _outputMaterials;

		[SerializeField]
		private MeshCollider _outputCollider;

		[SerializeField]
		private Mesh _outputColliderMesh;

		[SerializeField]
		private MeshCollider _outputMeshCollider;

		[SerializeField]
		private MeshCollider _localMeshCollider;

		[SerializeField]
		private bool _outputMeshRendererInitiallyEnabled;

		[SerializeField]
		private bool _outputMeshColliderInitiallyEnabled;

		//	LOGIC -----------------------------------------------------------------------------------------------------

		public void DestroyAllData(HEU_HoudiniAsset asset)
		{
			_attributeDatas.Clear();

			_positionAttributeValues = null;
			_vertexIndices = null;

			if (_localMaterial != null)
			{
				HEU_MaterialFactory.DestroyNonAssetMaterial(_localMaterial, false);
				_localMaterial = null;
			}

			_outputGameObject = null;
			_outputMaterials = null;
			_localMeshCollider = null;
			_outputMeshCollider = null;
			_outputColliderMesh = null;
			_outputMesh = null;
		}

		public void SyncAllAttributesFrom(HEU_SessionBase session, HEU_HoudiniAsset asset, HAPI_NodeId geoID, ref HAPI_PartInfo partInfo, GameObject outputGameObject)
		{
			_geoID = geoID;
			_partID = partInfo.id;

			HAPI_GeoInfo geoInfo = new HAPI_GeoInfo();
			if(session.GetGeoInfo(_geoID, ref geoInfo))
			{
				_geoName = HEU_SessionManager.GetString(geoInfo.nameSH, session);
			}

			if (outputGameObject != null)
			{
				_outputTransform = outputGameObject.transform;
			}

			// Need the vertex list of indices to map the positions to vertex colors
			_vertexIndices = new int[partInfo.vertexCount];
			if (!HEU_GeneralUtility.GetArray2Arg(geoID, partInfo.id, session.GetVertexList, _vertexIndices, 0, partInfo.vertexCount))
			{
				return;
			}

			// Note that this currently only supports point attributes
			int attributePointCount = partInfo.attributeCounts[(int)HAPI_AttributeOwner.HAPI_ATTROWNER_POINT];
			string[] pointAttributeNames = new string[attributePointCount];
			if(!session.GetAttributeNames(geoID, partInfo.id, HAPI_AttributeOwner.HAPI_ATTROWNER_POINT, ref pointAttributeNames, attributePointCount))
			{
				Debug.LogErrorFormat("Failed to sync attributes. Unable to retrieve attribute names.");
				return;
			}

			// Create new list of attributes. We'll move existing attributes that are still in use as we find them.
			List<HEU_AttributeData> newAttributeDatas = new List<HEU_AttributeData>();

			foreach (string pointAttributeName in pointAttributeNames)
			{
				if(string.IsNullOrEmpty(pointAttributeName))
				{
					continue;
				}

				// Get position attribute values separately. Used for painting and editing points in 3D scene.
				HAPI_AttributeInfo pointAttributeInfo = new HAPI_AttributeInfo();
				if(session.GetAttributeInfo(geoID, partInfo.id, pointAttributeName, HAPI_AttributeOwner.HAPI_ATTROWNER_POINT, ref pointAttributeInfo))
				{
					if (pointAttributeName.Equals(HEU_Defines.HAPI_ATTRIB_POSITION))
					{
						if (pointAttributeInfo.storage != HAPI_StorageType.HAPI_STORAGETYPE_FLOAT)
						{
							Debug.LogErrorFormat("Expected float type for position attribute, but got {0}", pointAttributeInfo.storage);
							return;
						}

						_positionAttributeValues = new Vector3[pointAttributeInfo.count];
						float[] data = new float[0];
						HEU_GeneralUtility.GetAttribute(session, geoID, partInfo.id, pointAttributeName, ref pointAttributeInfo, ref data, session.GetAttributeFloatData);
						for (int i = 0; i < pointAttributeInfo.count; ++i)
						{
							_positionAttributeValues[i] = new Vector3(-data[i * pointAttributeInfo.tupleSize + 0], data[i * pointAttributeInfo.tupleSize + 1], data[i * pointAttributeInfo.tupleSize + 2]);
						}

						// We don't let position attributes be editted (for now anyway)
						continue;
					}


					HEU_AttributeData attrData = GetAttributeData(pointAttributeName);
					if (attrData == null)
					{
						// Attribute data not found. Create it.

						attrData = CreateAttribute(pointAttributeName, ref pointAttributeInfo);
						//Debug.LogFormat("Created attribute data: {0}", pointAttributeName);
					}

					// Add to new list.
					newAttributeDatas.Add(attrData);

					// Sync the attribute info to data.
					PopulateAttributeData(session, geoID, partInfo.id, attrData, ref pointAttributeInfo);

					if(pointAttributeName.Equals(HEU_Defines.HAPI_ATTRIB_COLOR) || pointAttributeInfo.typeInfo == HAPI_AttributeTypeInfo.HAPI_ATTRIBUTE_TYPE_COLOR)
					{
						_hasColorAttribute = true;
					}
				}
				else
				{
					// Failed to get point attribute info!
				}
			}

			// Overwriting the old list with the new should automatically remove unused attribute datas.
			_attributeDatas = newAttributeDatas;
		}

		public void SetupMeshAndMaterials(HEU_HoudiniAsset asset, HAPI_PartType partType, GameObject outputGameObject)
		{
			_outputMesh = null;
			_outputGameObject = null;

			if (HEU_HAPIUtility.IsSupportedPolygonType(partType))
			{
				// Get the generated mesh. If mesh is missing, nothing we can do.
				MeshFilter meshFilter = outputGameObject.GetComponent<MeshFilter>();
				if (meshFilter != null && meshFilter.sharedMesh != null)
				{
					_outputMesh = meshFilter.sharedMesh;
				}
				else
				{
					// Without a valid mesh, we won't be able to paint so nothing else to do
					return;
				}

				_outputGameObject = outputGameObject;

				if (_localMaterial == null)
				{
					MeshRenderer meshRenderer = _outputGameObject.GetComponent<MeshRenderer>();
					if(meshRenderer != null)
					{
						_localMaterial = HEU_MaterialFactory.GetNewMaterialWithShader(null, HEU_PluginSettings.DefaultVertexColorShader, HEU_Defines.EDITABLE_MATERIAL, false);
					}
				}
			}
		}

		public bool HasDirtyAttributes()
		{
			foreach (HEU_AttributeData attrData in _attributeDatas)
			{
				if (!string.IsNullOrEmpty(attrData._name) && attrData._attributeState == HEU_AttributeData.AttributeState.LOCAL_DIRTY)
				{
					return true;
				}
			}
			return false;
		}

		public void SyncDirtyAttributesToHoudini(HEU_SessionBase session)
		{
			if (!UploadAttributeViaMeshInput(session, _geoID, _partID))
			{
				Debug.LogError("Unable to upload custom attribute edits!");
			}
		}


		private void PopulateAttributeData(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID, HEU_AttributeData attributeData, ref HAPI_AttributeInfo attributeInfo)
		{
			attributeData._attributeInfo = attributeInfo;

			int tupleSize = attributeInfo.tupleSize;
			int attributeCount = attributeInfo.count;
			int arraySize = attributeCount * tupleSize;

			// First reset arrays if the type had changed since last sync
			if ((attributeInfo.storage == HAPI_StorageType.HAPI_STORAGETYPE_INT && attributeData._attributeType != HEU_AttributeData.AttributeType.INT) ||
				(attributeInfo.storage == HAPI_StorageType.HAPI_STORAGETYPE_FLOAT && attributeData._attributeType != HEU_AttributeData.AttributeType.FLOAT) ||
				(attributeInfo.storage == HAPI_StorageType.HAPI_STORAGETYPE_STRING && attributeData._attributeType != HEU_AttributeData.AttributeType.STRING))
			{
				// Reset arrays if type is different
				attributeData._floatValues = null;
				attributeData._stringValues = null;
				attributeData._intValues = null;

				attributeData._attributeState = HEU_AttributeData.AttributeState.INVALID;

				if(attributeInfo.storage == HAPI_StorageType.HAPI_STORAGETYPE_INT)
				{
					attributeData._attributeType = HEU_AttributeData.AttributeType.INT;
				}
				else if (attributeInfo.storage == HAPI_StorageType.HAPI_STORAGETYPE_FLOAT)
				{
					attributeData._attributeType = HEU_AttributeData.AttributeType.FLOAT;
				}
				else if (attributeInfo.storage == HAPI_StorageType.HAPI_STORAGETYPE_STRING)
				{
					attributeData._attributeType = HEU_AttributeData.AttributeType.STRING;
				}
			}

			// Make sure the internal array is correctly sized for syncing.
			if (attributeData._attributeType == HEU_AttributeData.AttributeType.INT)
			{
				if (attributeData._intValues == null)
				{
					attributeData._intValues = new int[arraySize];
					attributeData._attributeState = HEU_AttributeData.AttributeState.INVALID;
				}
				else if (attributeData._intValues.Length != arraySize)
				{
					System.Array.Resize<int>(ref attributeData._intValues, arraySize);
					attributeData._attributeState = HEU_AttributeData.AttributeState.INVALID;
				}
				attributeData._floatValues = null;
				attributeData._stringValues = null;

				if (attributeData._attributeState == HEU_AttributeData.AttributeState.INVALID)
				{
					int[] data = new int[0];
					HEU_GeneralUtility.GetAttribute(session, geoID, partID, attributeData._name, ref attributeInfo, ref data, session.GetAttributeIntData);
					for (int i = 0; i < attributeCount; ++i)
					{
						for (int tuple = 0; tuple < tupleSize; ++tuple)
						{
							attributeData._intValues[i * tupleSize + tuple] = data[i * tupleSize + tuple];
						}
					}
				}
			}
			else if (attributeData._attributeType == HEU_AttributeData.AttributeType.FLOAT)
			{
				if (attributeData._floatValues == null)
				{
					attributeData._floatValues = new float[arraySize];
					attributeData._attributeState = HEU_AttributeData.AttributeState.INVALID;
				}
				else if (attributeData._floatValues.Length != arraySize)
				{
					System.Array.Resize<float>(ref attributeData._floatValues, arraySize);
					attributeData._attributeState = HEU_AttributeData.AttributeState.INVALID;
				}
				attributeData._intValues = null;
				attributeData._stringValues = null;

				if (attributeData._attributeState == HEU_AttributeData.AttributeState.INVALID)
				{
					float[] data = new float[0];
					HEU_GeneralUtility.GetAttribute(session, geoID, partID, attributeData._name, ref attributeInfo, ref data, session.GetAttributeFloatData);
					for (int i = 0; i < attributeCount; ++i)
					{
						for (int tuple = 0; tuple < tupleSize; ++tuple)
						{
							attributeData._floatValues[i * tupleSize + tuple] = data[i * tupleSize + tuple];
						}
					}
				}
			}
			else if (attributeData._attributeType == HEU_AttributeData.AttributeType.STRING)
			{
				if (attributeData._stringValues == null)
				{
					attributeData._stringValues = new string[arraySize];
					attributeData._attributeState = HEU_AttributeData.AttributeState.INVALID;
				}
				else if (attributeData._stringValues.Length != arraySize)
				{
					System.Array.Resize<string>(ref attributeData._stringValues, arraySize);
					attributeData._attributeState = HEU_AttributeData.AttributeState.INVALID;
				}
				attributeData._intValues = null;
				attributeData._floatValues = null;

				if (attributeData._attributeState == HEU_AttributeData.AttributeState.INVALID)
				{
					HAPI_StringHandle[] data = new HAPI_StringHandle[0];
					HEU_GeneralUtility.GetAttribute(session, geoID, partID, attributeData._name, ref attributeInfo, ref data, session.GetAttributeStringData);
					for (int i = 0; i < attributeCount; ++i)
					{
						for (int tuple = 0; tuple < tupleSize; ++tuple)
						{
							HAPI_StringHandle stringHandle = data[i * tupleSize + tuple];
							attributeData._stringValues[i * tupleSize + tuple] = HEU_SessionManager.GetString(stringHandle, session);
						}
					}
				}
			}

			SetAttributeDataSyncd(attributeData);
		}

		private void GetAttributesList(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID, List<HEU_AttributeData> attributesList, HAPI_AttributeOwner ownerType, int attributeCount)
		{
			if (attributeCount > 0)
			{
				string[] attributeNames = new string[attributeCount];
				if (session.GetAttributeNames(geoID, partID, ownerType, ref attributeNames, attributeCount))
				{
					for (int i = 0; i < attributeNames.Length; ++i)
					{
						HEU_AttributeData attrData = GetAttributeData(attributeNames[i]);
						if (attrData == null)
						{
							// New attribute, so create and populate

							HAPI_AttributeInfo attributeInfo = new HAPI_AttributeInfo();
							if (!session.GetAttributeInfo(geoID, partID, attributeNames[i], ownerType, ref attributeInfo) || !attributeInfo.exists)
							{
								continue;
							}

							attrData = CreateAttribute(attributeNames[i], ref attributeInfo);

							PopulateAttributeData(session, geoID, partID, attrData, ref attributeInfo);

							attributesList.Add(attrData);
						}
						else
						{
							// Existing, so just add it
							attributesList.Add(attrData);
						}
					}
				}
			}
		}

		private void UpdateAttribute(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID, HEU_AttributeData attributeData)
		{
			int attrCount = attributeData._attributeInfo.count;

			// Presuming we are working with point attributes
			HAPI_AttributeInfo newAttrInfo = new HAPI_AttributeInfo();
			newAttrInfo.exists = true;
			newAttrInfo.owner = attributeData._attributeInfo.owner;
			newAttrInfo.storage = attributeData._attributeInfo.storage;
			newAttrInfo.count = attributeData._attributeInfo.count;
			newAttrInfo.tupleSize = attributeData._attributeInfo.tupleSize;
			newAttrInfo.originalOwner = attributeData._attributeInfo.originalOwner;

			if (!session.AddAttribute(geoID, partID, attributeData._name, ref newAttrInfo))
			{
				Debug.LogErrorFormat("Failed to add attribute: {0}", attributeData._name);
				return;
			}

			if (newAttrInfo.storage == HAPI_StorageType.HAPI_STORAGETYPE_INT)
			{
				int[] pointData = new int[attrCount * newAttrInfo.tupleSize];
				for (int j = 0; j < attrCount; ++j)
				{
					for (int tuple = 0; tuple < newAttrInfo.tupleSize; ++tuple)
					{
						pointData[j * newAttrInfo.tupleSize + tuple] = attributeData._intValues[j * newAttrInfo.tupleSize + tuple];
					}
				}
				HEU_GeneralUtility.SetAttributeArray(geoID, partID, attributeData._name, ref newAttrInfo, pointData, session.SetAttributeIntData, attrCount);
			}
			else if (newAttrInfo.storage == HAPI_StorageType.HAPI_STORAGETYPE_FLOAT)
			{
				float[] pointData = new float[attrCount * newAttrInfo.tupleSize];
				for (int j = 0; j < attrCount; ++j)
				{
					for (int tuple = 0; tuple < newAttrInfo.tupleSize; ++tuple)
					{
						pointData[j * newAttrInfo.tupleSize + tuple] = attributeData._floatValues[j * newAttrInfo.tupleSize + tuple];
					}
				}
				HEU_GeneralUtility.SetAttributeArray(geoID, partID, attributeData._name, ref newAttrInfo, pointData, session.SetAttributeFloatData, attrCount);
			}
			else if (newAttrInfo.storage == HAPI_StorageType.HAPI_STORAGETYPE_STRING)
			{
				string[] pointData = new string[attrCount * newAttrInfo.tupleSize];
				for (int j = 0; j < attrCount; ++j)
				{
					for (int tuple = 0; tuple < newAttrInfo.tupleSize; ++tuple)
					{
						pointData[j * newAttrInfo.tupleSize + tuple] = attributeData._stringValues[j * newAttrInfo.tupleSize + tuple];
					}
				}
				HEU_GeneralUtility.SetAttributeArray(geoID, partID, attributeData._name, ref newAttrInfo, pointData, session.SetAttributeStringData, attrCount);
			}
		}

		private void UpdateAttributeList(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID, List<HEU_AttributeData> attributeDataList)
		{
			foreach (HEU_AttributeData attrData in attributeDataList)
			{
				UpdateAttribute(session, geoID, partID, attrData);
			}
		}

		/// <summary>
		/// This reverts local modifcations, refresh upstream inputs, and cooks the editable node.
		/// This ensures that the node will use the latest upstream input data before applying its own changes.
		/// </summary>
		/// <param name="session"></param>
		public void RefreshUpstreamInputs(HEU_SessionBase session)
		{
			session.RevertGeo(_geoID);

			HEU_HAPIUtility.CookNodeInHoudini(session, _geoID, false, "");
		}

		public bool UploadAttributeViaMeshInput(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID)
		{
			// Need to upload same geometry as in the original Editable node, and with custom attribute values.

			// First get the current geometry info and data.

			HAPI_GeoInfo geoInfo = new HAPI_GeoInfo();
			if(!session.GetGeoInfo(geoID, ref geoInfo))
			{
				return false;
			}

			HAPI_PartInfo oldPartInfo = new HAPI_PartInfo();
			if(!session.GetPartInfo(geoID, partID, ref oldPartInfo))
			{
				return false;
			}

			int pointCount = oldPartInfo.pointCount;
			int vertexCount = oldPartInfo.vertexCount;
			int faceCount = oldPartInfo.faceCount;

			// Get facecounts
			int[] faceCountData = new int[faceCount];
			if(!HEU_GeneralUtility.GetArray2Arg(geoID, partID, session.GetFaceCounts, faceCountData, 0, faceCount))
			{
				return false;
			}

			// Get indices
			int[] vertexList = new int[vertexCount];
			if (!HEU_GeneralUtility.GetArray2Arg(geoID, partID, session.GetVertexList, vertexList, 0, vertexCount))
			{
				return false;
			}

			List<HEU_AttributeData> pointAttributeDatas = new List<HEU_AttributeData>();
			List<HEU_AttributeData> vertexAttributeDatas = new List<HEU_AttributeData>();
			List<HEU_AttributeData> primitiveAttributeDatas = new List<HEU_AttributeData>();
			List<HEU_AttributeData> detailAttributeDatas = new List<HEU_AttributeData>();

			// Get all attributes, including editted
			GetAttributesList(session, geoID, partID, pointAttributeDatas, HAPI_AttributeOwner.HAPI_ATTROWNER_POINT, oldPartInfo.pointAttributeCount);
			GetAttributesList(session, geoID, partID, vertexAttributeDatas, HAPI_AttributeOwner.HAPI_ATTROWNER_VERTEX, oldPartInfo.vertexAttributeCount);
			GetAttributesList(session, geoID, partID, primitiveAttributeDatas, HAPI_AttributeOwner.HAPI_ATTROWNER_PRIM, oldPartInfo.primitiveAttributeCount);
			GetAttributesList(session, geoID, partID, detailAttributeDatas, HAPI_AttributeOwner.HAPI_ATTROWNER_DETAIL, oldPartInfo.detailAttributeCount);

			// Now create new geometry info and populate it

			HAPI_PartInfo newPartInfo = new HAPI_PartInfo();
			newPartInfo.faceCount = faceCount;
			newPartInfo.vertexCount = vertexCount;
			newPartInfo.pointCount = pointCount;
			newPartInfo.pointAttributeCount = pointAttributeDatas.Count;
			newPartInfo.vertexAttributeCount = vertexAttributeDatas.Count;
			newPartInfo.primitiveAttributeCount = primitiveAttributeDatas.Count;
			newPartInfo.detailAttributeCount = detailAttributeDatas.Count;

			int newPartID = partID;

			if (!session.SetPartInfo(geoID, newPartID, ref newPartInfo))
			{
				return false;
			}

			if (!HEU_GeneralUtility.SetArray2Arg(geoID, newPartID, session.SetFaceCount, faceCountData, 0, newPartInfo.faceCount))
			{
				return false;
			}

			if (!HEU_GeneralUtility.SetArray2Arg(geoID, newPartID, session.SetVertexList, vertexList, 0, newPartInfo.vertexCount))
			{
				return false;
			}

			// Upload all attributes, include editted
			UpdateAttributeList(session, geoID, partID, pointAttributeDatas);
			UpdateAttributeList(session, geoID, partID, vertexAttributeDatas);
			UpdateAttributeList(session, geoID, partID, primitiveAttributeDatas);
			UpdateAttributeList(session, geoID, partID, detailAttributeDatas);

			return session.CommitGeo(geoID);
		}

		private static void SetAttributeDataSyncd(HEU_AttributeData attributeData)
		{
			attributeData._attributeState = HEU_AttributeData.AttributeState.SYNCED;
		}

		public static void SetAttributeDataDirty(HEU_AttributeData attributeData)
		{
			attributeData._attributeState = HEU_AttributeData.AttributeState.LOCAL_DIRTY;
		}

		public HEU_AttributeData CreateAttribute(string attributeName, ref HAPI_AttributeInfo attributeInfo)
		{
			HEU_AttributeData.AttributeType attributeType = HEU_AttributeData.AttributeType.UNDEFINED;
			if (attributeInfo.storage == HAPI_StorageType.HAPI_STORAGETYPE_INT)
			{
				attributeType = HEU_AttributeData.AttributeType.INT;
			}
			else if (attributeInfo.storage == HAPI_StorageType.HAPI_STORAGETYPE_FLOAT)
			{
				attributeType = HEU_AttributeData.AttributeType.FLOAT;
			}
			else if (attributeInfo.storage == HAPI_StorageType.HAPI_STORAGETYPE_STRING)
			{
				attributeType = HEU_AttributeData.AttributeType.STRING;
			}

			HEU_AttributeData attributeData = new HEU_AttributeData();
			attributeData._name = attributeName;
			attributeData._attributeType = attributeType;
			attributeData._attributeInfo = attributeInfo;
			attributeData._attributeState = HEU_AttributeData.AttributeState.INVALID;

			return attributeData;
		}

		public HEU_AttributeData GetAttributeData(string name)
		{
			foreach(HEU_AttributeData attr in _attributeDatas)
			{
				if(attr._name.Equals(name))
				{
					return attr;
				}
			}
			return null;
		}

		public HEU_AttributeData GetAttributeData(int index)
		{
			if(index >= 0 && index < _attributeDatas.Count)
			{
				return _attributeDatas[index];
			}
			return null;
		}

		public List<string> GetAttributeNames()
		{
			List<string> attributeNames = new List<string>();
			foreach(HEU_AttributeData data in _attributeDatas)
			{
				attributeNames.Add(data._name);
			}
			return attributeNames;
		}

		public void EnablePaintCollider()
		{
			if(_outputGameObject == null || _outputMesh == null)
			{
				return;
			}

			MeshCollider meshCollider = _outputGameObject.GetComponent<MeshCollider>();
			if (meshCollider == null)
			{
				// Use a new local collider
				_localMeshCollider = _outputGameObject.AddComponent<MeshCollider>();
				_localMeshCollider.sharedMesh = _outputMesh;
				_localMeshCollider.enabled = true;
			}
			else
			{
				_outputCollider = meshCollider;
				_outputColliderMesh = meshCollider.sharedMesh;
				_outputCollider.sharedMesh = _outputMesh;

				_outputMeshColliderInitiallyEnabled = _outputCollider.enabled;

				// Force disable then enable to properly activate
				_outputCollider.enabled = false;
				_outputCollider.enabled = true;
			}
		}

		public void DisablePaintCollider()
		{
			if (_localMeshCollider != null && _outputGameObject != null)
			{
				HEU_GeneralUtility.DestroyComponent<MeshCollider>(_outputGameObject);
				_localMeshCollider = null;
			}

			if (_outputCollider != null)
			{
				_outputCollider.sharedMesh = _outputColliderMesh;
				_outputCollider.enabled = _outputMeshColliderInitiallyEnabled;

				_outputCollider = null;
				_outputColliderMesh = null;
			}
		}

		public void ShowPaintMesh()
		{
			// For painting, switch out material, and enable collider on the output gameobject.
			// Store references to the current material and collider mesh so that we can restore
			// when finished painting.
			// Create local material and collider if output gameobject doesn't have its own.

			if(_outputGameObject != null && _outputMesh != null)
			{
				MeshRenderer meshRenderer = _outputGameObject.GetComponent<MeshRenderer>();
				if(meshRenderer != null)
				{
					_outputMaterials = meshRenderer.sharedMaterials;

					meshRenderer.sharedMaterial = _localMaterial;

					_outputMeshRendererInitiallyEnabled = meshRenderer.enabled;
					meshRenderer.enabled = true;
				}
				else
				{
					_outputMaterials = null;
				}
			}
		}

		public void HidePaintMesh()
		{
			if (_outputGameObject != null)
			{
				MeshRenderer meshRenderer = _outputGameObject.GetComponent<MeshRenderer>();
				if (meshRenderer != null && _outputMaterials != null)
				{
					meshRenderer.sharedMaterials = _outputMaterials;
					meshRenderer.enabled = _outputMeshRendererInitiallyEnabled;

					_outputMaterials = null;
				}
			}
		}

		public bool HasMeshForPainting()
		{
			return _outputMesh != null;
		}

		public MeshCollider GetPaintMeshCollider()
		{
			if (_outputMeshCollider != null)
			{
				return _outputMeshCollider;
			}
			else return _localMeshCollider;
		}

		public void PaintAttribute(HEU_AttributeData attributeData, HEU_ToolsInfo sourceTools, int attributeIndex, float paintFactor, SetAttributeValueFunc setAttrFunc)
		{
			if(attributeData._attributeState == HEU_AttributeData.AttributeState.INVALID)
			{
				return;
			}

			int targetIndex = attributeIndex * attributeData._attributeInfo.tupleSize;
			setAttrFunc(attributeData, targetIndex, sourceTools, 0, paintFactor);

			SetAttributeDataDirty(attributeData);
		}

		public static void SetAttributeEditValueInt(HEU_AttributeData attributeData, int startIndex, int[] values)
		{
			int numValues = values.Length;
			for (int i = 0; i < numValues; ++i)
			{
				attributeData._intValues[startIndex + i] = values[i];
			}
		}

		public static void SetAttributeEditValueFloat(HEU_AttributeData attributeData, int startIndex, float[] values)
		{
			int numValues = values.Length;
			for (int i = 0; i < numValues; ++i)
			{
				attributeData._floatValues[startIndex + i] = values[i];
			}
		}

		public static void SetAttributeEditValueString(HEU_AttributeData attributeData, int startIndex, string[] values)
		{
			int numValues = values.Length;
			for (int i = 0; i < numValues; ++i)
			{
				attributeData._stringValues[startIndex + i] = values[i];
			}
		}

		// Delegate to set attribute value based on value type, and merge mode (for painting)
		public delegate void SetAttributeValueFunc(HEU_AttributeData attributeData, int targetIndex, HEU_ToolsInfo sourceTools, int sourceIndex, float factor);

		public static void ReplaceAttributeValueInt(HEU_AttributeData attributeData, int targetIndex, HEU_ToolsInfo sourceTools, int sourceIndex, float factor)
		{
			int numValues = sourceTools._paintIntValue.Length;
			for (int i = 0; i < numValues; ++i)
			{
				attributeData._intValues[targetIndex + i] = Mathf.RoundToInt(((float)attributeData._intValues[targetIndex + i]) * (1f - factor) + ((float)sourceTools._paintIntValue[sourceIndex + i]) * factor);
			}
		}

		public static void AddAttributeValueInt(HEU_AttributeData attributeData, int targetIndex, HEU_ToolsInfo sourceTools, int sourceIndex, float factor)
		{
			int numValues = sourceTools._paintIntValue.Length;
			for (int i = 0; i < numValues; ++i)
			{
				attributeData._intValues[targetIndex + i] += Mathf.RoundToInt((float)sourceTools._paintIntValue[sourceIndex + i] * factor);
			}
		}

		public static void SubtractAttributeValueInt(HEU_AttributeData attributeData, int targetIndex, HEU_ToolsInfo sourceTools, int sourceIndex, float factor)
		{
			int numValues = sourceTools._paintIntValue.Length;
			for (int i = 0; i < numValues; ++i)
			{
				attributeData._intValues[targetIndex + i] -= Mathf.RoundToInt((float)sourceTools._paintIntValue[sourceIndex + i] * factor);
			}
		}

		public static void MultiplyAttributeValueInt(HEU_AttributeData attributeData, int targetIndex, HEU_ToolsInfo sourceTools, int sourceIndex, float factor)
		{
			int numValues = sourceTools._paintIntValue.Length;
			for (int i = 0; i < numValues; ++i)
			{
				attributeData._intValues[targetIndex + i] = Mathf.RoundToInt(Mathf.Lerp((float)attributeData._intValues[targetIndex + i], (float)attributeData._intValues[targetIndex + i] * (float)sourceTools._paintIntValue[sourceIndex + i], factor));
			}
		}

		public static void ReplaceAttributeValueFloat(HEU_AttributeData attributeData, int targetIndex, HEU_ToolsInfo sourceTools, int sourceIndex, float factor)
		{
			int numValues = sourceTools._paintFloatValue.Length;
			for (int i = 0; i < numValues; ++i)
			{
				attributeData._floatValues[targetIndex + i] = attributeData._floatValues[targetIndex + i] * (1f - factor) + sourceTools._paintFloatValue[sourceIndex + i] * factor;
			}
		}

		public static void AddAttributeValueFloat(HEU_AttributeData attributeData, int targetIndex, HEU_ToolsInfo sourceTools, int sourceIndex, float factor)
		{
			int numValues = sourceTools._paintFloatValue.Length;
			for (int i = 0; i < numValues; ++i)
			{
				attributeData._floatValues[targetIndex + i] += sourceTools._paintFloatValue[sourceIndex + i] * factor;
			}
		}

		public static void SubtractAttributeValueFloat(HEU_AttributeData attributeData, int targetIndex, HEU_ToolsInfo sourceTools, int sourceIndex, float factor)
		{
			int numValues = sourceTools._paintFloatValue.Length;
			for (int i = 0; i < numValues; ++i)
			{
				attributeData._floatValues[targetIndex + i] -= sourceTools._paintFloatValue[sourceIndex + i] * factor;
			}
		}

		public static void MultiplyAttributeValueFloat(HEU_AttributeData attributeData, int targetIndex, HEU_ToolsInfo sourceTools, int sourceIndex, float factor)
		{
			int numValues = sourceTools._paintFloatValue.Length;
			for (int i = 0; i < numValues; ++i)
			{
				attributeData._floatValues[targetIndex + i] = Mathf.Lerp(attributeData._floatValues[targetIndex + i], attributeData._floatValues[targetIndex + i] * sourceTools._paintFloatValue[sourceIndex + i], factor);
			}
		}

		public static void SetAttributeValueString(HEU_AttributeData attributeData, int targetIndex, HEU_ToolsInfo sourceTools, int sourceIndex, float factor)
		{
			int numValues = sourceTools._paintStringValue.Length;
			for (int i = 0; i < numValues; ++i)
			{
				attributeData._stringValues[targetIndex + i] = sourceTools._paintStringValue[sourceIndex + i];
			}
		}


		public void FillAttribute(HEU_AttributeData attributeData, HEU_ToolsInfo sourceTools)
		{
			if(attributeData._attributeState == HEU_AttributeData.AttributeState.INVALID)
			{
				return;
			}

			HEU_AttributesStore.SetAttributeValueFunc setAttrFunc = HEU_AttributesStore.GetAttributeSetValueFunction(attributeData._attributeType, sourceTools._paintMergeMode);
			if (setAttrFunc == null)
			{
				return;
			}

			int tupleSize = attributeData._attributeInfo.tupleSize;
			int count = attributeData._attributeInfo.count;
			for (int pt = 0; pt < count; ++pt)
			{
				setAttrFunc(attributeData, pt * tupleSize, sourceTools, 0, sourceTools._paintBrushOpacity);
			}

			SetAttributeDataDirty(attributeData);
		}

		public bool AreAttributesDirty()
		{
			foreach(HEU_AttributeData attrData in _attributeDatas)
			{
				if(attrData._attributeState == HEU_AttributeData.AttributeState.LOCAL_DIRTY)
				{
					return true;
				}
			}
			return false;
		}

		public void GetPositionAttributeValues(out Vector3[] positionArray)
		{
			positionArray = _positionAttributeValues;
		}

		public void GetVertexIndices(out int[] indices)
		{
			indices = _vertexIndices;
		}

		public static SetAttributeValueFunc GetAttributeSetValueFunction(HEU_AttributeData.AttributeType attrType, HEU_ToolsInfo.PaintMergeMode paintMergeMode)
		{
			SetAttributeValueFunc setAttrFunc = null;
			if (attrType == HEU_AttributeData.AttributeType.INT)
			{
				if (paintMergeMode == HEU_ToolsInfo.PaintMergeMode.REPLACE)
				{
					setAttrFunc = HEU_AttributesStore.ReplaceAttributeValueInt;
				}
				else if (paintMergeMode == HEU_ToolsInfo.PaintMergeMode.ADD)
				{
					setAttrFunc = HEU_AttributesStore.AddAttributeValueInt;
				}
				else if (paintMergeMode == HEU_ToolsInfo.PaintMergeMode.SUBTRACT)
				{
					setAttrFunc = HEU_AttributesStore.SubtractAttributeValueInt;
				}
				else if (paintMergeMode == HEU_ToolsInfo.PaintMergeMode.MULTIPLY)
				{
					setAttrFunc = HEU_AttributesStore.MultiplyAttributeValueInt;
				}
			}
			else if (attrType == HEU_AttributeData.AttributeType.FLOAT)
			{
				if (paintMergeMode == HEU_ToolsInfo.PaintMergeMode.REPLACE)
				{
					setAttrFunc = HEU_AttributesStore.ReplaceAttributeValueFloat;
				}
				else if (paintMergeMode == HEU_ToolsInfo.PaintMergeMode.ADD)
				{
					setAttrFunc = HEU_AttributesStore.AddAttributeValueFloat;
				}
				else if (paintMergeMode == HEU_ToolsInfo.PaintMergeMode.SUBTRACT)
				{
					setAttrFunc = HEU_AttributesStore.SubtractAttributeValueFloat;
				}
				else if (paintMergeMode == HEU_ToolsInfo.PaintMergeMode.MULTIPLY)
				{
					setAttrFunc = HEU_AttributesStore.MultiplyAttributeValueFloat;
				}
			}
			else if (attrType == HEU_AttributeData.AttributeType.STRING)
			{
				setAttrFunc = HEU_AttributesStore.SetAttributeValueString;
			}

			return setAttrFunc;
		}

		public void CopyAttributeValuesTo(HEU_AttributesStore destAttrStore)
		{
			foreach(HEU_AttributeData attrData in _attributeDatas)
			{
				HEU_AttributeData destAttrData = destAttrStore.GetAttributeData(attrData._name);
				if(destAttrStore != null)
				{
					attrData.CopyValuesTo(destAttrData);
					SetAttributeDataDirty(destAttrData);
				}
			}
		}
	}

}   // HoudiniEngineUnity