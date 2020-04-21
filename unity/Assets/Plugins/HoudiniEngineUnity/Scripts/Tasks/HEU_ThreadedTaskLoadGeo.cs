/*
* Copyright (c) <2019> Side Effects Software Inc.
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HoudiniEngineUnity
{
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Typedefs (copy these from HEU_Common.cs)
	using HAPI_NodeId = System.Int32;
	using HAPI_PartId = System.Int32;
	using HAPI_ParmId = System.Int32;
	using HAPI_StringHandle = System.Int32;

	/// <summary>
	/// Threaded class for loading bgeo files into Unity.
	/// The threaded work involves loading the bgeo into a Houdini Engine session, then retrieving the geometry
	/// into local buffers. 
	/// Finally, back in the main thread, the buffers are passed off to HEU_GeoSync to continue loading in Unity.
	/// </summary>
	public class HEU_ThreadedTaskLoadGeo : HEU_ThreadedTask
	{
		//	LOGIC ------------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Initial setup for loading the bgeo file.
		/// </summary>
		/// <param name="filePath">Path to the bgeo file</param>
		/// <param name="geoSync">HEU_GeoSync object that will do the actual Unity geometry creation</param>
		/// <param name="session">Houdini Engine session</param>
		/// <param name="fileNodeID">The file node's ID that was created in Houdini</param>
		public void Setup(string filePath, HEU_GeoSync geoSync, HEU_SessionBase session, HAPI_NodeId fileNodeID)
		{
			_filePath = filePath;
			_geoSync = geoSync;
			_session = session;
			_name = filePath;

			_generateOptions = geoSync.GenerateOptions;

			// Work data
			_loadData = new HEU_LoadData();
			_loadData._fileNodeID = fileNodeID;
			_loadData._loadStatus = HEU_LoadData.LoadStatus.NONE;
			_loadData._logStr = "";
		}

		/// <summary>
		/// Do the geometry loading in Houdini in a thread.
		/// Creates a file node, loads the bgeo, then retrives the geometry into local buffers.
		/// </summary>
		protected override void DoWork()
		{
			_loadData._loadStatus = HEU_LoadData.LoadStatus.STARTED;

			//Debug.LogFormat("DoWork: Loading {0}", _filePath);

			if (_session == null || !_session.IsSessionValid())
			{
				SetLog(HEU_LoadData.LoadStatus.ERROR, "Invalid session!");
				return;
			}

			// Check file path
			if (!HEU_Platform.DoesPathExist(_filePath))
			{
				SetLog(HEU_LoadData.LoadStatus.ERROR, string.Format("File not found at {0}", _filePath));
				return;
			}

#if true
			// Make sure file type is supported
			//if (!_filePath.EndsWith(".bgeo") && !_filePath.EndsWith(".bgeo.sc"))
			//{
			//	SetLog(HEU_LoadData.LoadStatus.ERROR, string.Format("Only .bgeo or .bgeo.sc files are supported."));
			//	return;
			//}

			// Create file SOP
			if (_loadData._fileNodeID == HEU_Defines.HEU_INVALID_NODE_ID)
			{
				if (!CreateFileNode(out _loadData._fileNodeID))
				{
					SetLog(HEU_LoadData.LoadStatus.ERROR, string.Format("Unable to create file node in Houdini."));
					return;
				}
			}

            Sleep();

			HAPI_NodeId displayNodeID = GetDisplayNodeID(_loadData._fileNodeID);
			if (displayNodeID == HEU_Defines.HEU_INVALID_NODE_ID)
			{
				SetLog(HEU_LoadData.LoadStatus.ERROR, string.Format("Unable to get display node of file geo node."));
				return;
			}

			// Set the file parameter
			if (!SetFileParm(displayNodeID, _filePath))
			{
				SetLog(HEU_LoadData.LoadStatus.ERROR, string.Format("Unable to set file path parm."));
				return;
			}

			// Cooking it will load the bgeo
			if (!_session.CookNode(_loadData._fileNodeID, false))
			{
				SetLog(HEU_LoadData.LoadStatus.ERROR, string.Format("Unable to cook node."));
				return;
			}

			// Wait until cooking has finished
			bool bResult = true;
			HAPI_State statusCode = HAPI_State.HAPI_STATE_STARTING_LOAD;
			while (bResult && statusCode > HAPI_State.HAPI_STATE_MAX_READY_STATE)
			{
				bResult = _session.GetStatus(HAPI_StatusType.HAPI_STATUS_COOK_STATE, out statusCode);

				Sleep();
			}

			// Check cook results for any errors
			if (statusCode == HAPI_State.HAPI_STATE_READY_WITH_COOK_ERRORS || statusCode == HAPI_State.HAPI_STATE_READY_WITH_FATAL_ERRORS)
			{
				string statusString = _session.GetStatusString(HAPI_StatusType.HAPI_STATUS_COOK_RESULT, HAPI_StatusVerbosity.HAPI_STATUSVERBOSITY_ERRORS);
				SetLog(HEU_LoadData.LoadStatus.ERROR, string.Format("Cook failed: {0}.", statusString));
				return;
			}

#else

            // Load the file using HAPI_LoadGeoFromFile
            if (_loadData._fileNodeID == HEU_Defines.HEU_INVALID_NODE_ID)
            {
				Debug.Log("Creating file node with path: " + _filePath);
                if (!_session.CreateNode(-1, "SOP/file", "loadfile", true, out _loadData._fileNodeID))
                {
                    SetLog(HEU_LoadData.LoadStatus.ERROR, string.Format("Unable to create geo SOP in Houdini."));
                    return;
                }

                if (!_session.LoadGeoFromFile(_loadData._fileNodeID, _filePath))
                {
                    SetLog(HEU_LoadData.LoadStatus.ERROR, string.Format("Unable to create file node in Houdini."));
                    return;
                }
            }
			HAPI_NodeId displayNodeID = GetDisplayNodeID(_loadData._fileNodeID);

#endif

			// Note that object instancing is not supported. Instancers currently supported are
			// part and point instancing.

			// Get the various types of geometry (parts) from the display node
			List<HAPI_PartInfo> meshParts = new List<HAPI_PartInfo>();
			List<HAPI_PartInfo> volumeParts = new List<HAPI_PartInfo>();
			List<HAPI_PartInfo> instancerParts = new List<HAPI_PartInfo>();
			List<HAPI_PartInfo> curveParts = new List<HAPI_PartInfo>();
			List<HAPI_PartInfo> scatterInstancerParts = new List<HAPI_PartInfo>();
			if (!QueryParts(displayNodeID, ref meshParts, ref volumeParts, ref instancerParts, ref curveParts, ref scatterInstancerParts))
			{
				SetLog(HEU_LoadData.LoadStatus.ERROR, string.Format("Unable to query parts on node."));
				return;
			}

			Sleep();

			// Create Unity mesh buffers
            if (!GenerateMeshBuffers(_session, displayNodeID, meshParts, _generateOptions._splitPoints, _generateOptions._useLODGroups, 
				_generateOptions._generateUVs, _generateOptions._generateTangents, _generateOptions._generateNormals, 
				out _loadData._meshBuffers))
            {
				SetLog(HEU_LoadData.LoadStatus.ERROR, string.Format("Unable to generate mesh data from parts."));
				return;
			}

			// Create Unity terrain buffers
			if (!GenerateTerrainBuffers(_session, displayNodeID, volumeParts, scatterInstancerParts, out _loadData._terrainBuffers))
			{
				SetLog(HEU_LoadData.LoadStatus.ERROR, string.Format("Unable to generate terrain data from volume parts."));
				return;
			}

			// Create instancers (should come after normal geometry has been generated above)
			if (!GenerateInstancerBuffers(_session, displayNodeID, instancerParts, out _loadData._instancerBuffers))
			{
				SetLog(HEU_LoadData.LoadStatus.ERROR, string.Format("Unable to generate data from instancer parts."));
				return;
			}

			SetLog(HEU_LoadData.LoadStatus.SUCCESS, "Completed!");
		}

		/// <summary>
		/// Once geometry buffers have been retrieved, load into Unity
		/// </summary>
		protected override void OnComplete()
		{
			//Debug.LogFormat("OnCompete: Loaded {0}", _filePath);

			if (_geoSync != null)
			{
				_geoSync.OnLoadComplete(_loadData);
			}
		}

		protected override void OnStopped()
		{
			//Debug.LogFormat("OnStopped: Loaded {0}", _filePath);

			if (_geoSync != null)
			{
				_geoSync.OnStopped(_loadData);
			}
		}

		protected override void CleanUp()
		{
			_loadData = null;

			base.CleanUp();
		}

		private void SetLog(HEU_LoadData.LoadStatus status, string logStr)
		{
			_loadData._loadStatus = status;
			_loadData._logStr = string.Format("{0} : {1}", _loadData._loadStatus.ToString(), logStr);
		}

		private bool CreateFileNode(out HAPI_NodeId fileNodeID)
		{
			fileNodeID = HEU_Defines.HEU_INVALID_NODE_ID;

			if (!_session.CreateNode(-1, "SOP/file", "loadbgeo", true, out fileNodeID))
			{
				return false;
			}

			return true;
		}

		private HAPI_NodeId GetDisplayNodeID(HAPI_NodeId objNodeID)
		{
			HAPI_GeoInfo displayGeoInfo = new HAPI_GeoInfo();
			if (_session.GetDisplayGeoInfo(objNodeID, ref displayGeoInfo))
			{
				return displayGeoInfo.nodeId;
			}

			return HEU_Defines.HEU_INVALID_NODE_ID;
		}

		private bool SetFileParm(HAPI_NodeId fileNodeID, string filePath)
		{
			HAPI_ParmId parmID = -1;
			if (!_session.GetParmIDFromName(fileNodeID, "file", out parmID))
			{
				return false;
			}

			if (!_session.SetParamStringValue(fileNodeID, filePath, parmID, 0))
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Returns the various geometry types (parts) from the given node.
		/// Only part instancers and point instancers (via attributes) are returned.
		/// </summary>
		private bool QueryParts(HAPI_NodeId nodeID, ref List<HAPI_PartInfo> meshParts, ref List<HAPI_PartInfo> volumeParts,
			ref List<HAPI_PartInfo> instancerParts, ref List<HAPI_PartInfo> curveParts, ref List<HAPI_PartInfo> scatterInstancerParts)
		{
			// Get display geo info
			HAPI_GeoInfo geoInfo = new HAPI_GeoInfo();
			if (!_session.GetGeoInfo(nodeID, ref geoInfo))
			{
				return false;
			}

			//Debug.LogFormat("GeoNode name:{0}, type: {1}, isTemplated: {2}, isDisplayGeo: {3}, isEditable: {4}, parts: {5}",
			//	HEU_SessionManager.GetString(geoInfo.nameSH, _session),
			//	geoInfo.type, geoInfo.isTemplated,
			//	geoInfo.isDisplayGeo, geoInfo.isEditable, geoInfo.partCount);

			if (geoInfo.type == HAPI_GeoType.HAPI_GEOTYPE_DEFAULT)
			{
				int numParts = geoInfo.partCount;
				for(int i = 0; i < numParts; ++i)
				{
					HAPI_PartInfo partInfo = new HAPI_PartInfo();
					if (!_session.GetPartInfo(geoInfo.nodeId, i, ref partInfo))
					{
						return false;
					}

					//Debug.LogFormat("Part {0} with name {1} and type {2}", i, HEU_SessionManager.GetString(partInfo.nameSH), partInfo.type);

					bool isAttribInstancer = false;
					bool isScatterInstancer = false;
					// Preliminary check for attribute instancing (mesh type with no verts but has points with instances)
					if (HEU_HAPIUtility.IsSupportedPolygonType(partInfo.type) && partInfo.vertexCount == 0 && partInfo.pointCount > 0)
					{
						// Allowing both types of instancing

						if (HEU_GeneralUtility.HasValidInstanceAttribute(_session, nodeID, partInfo.id, HEU_PluginSettings.UnityInstanceAttr))
						{
							isAttribInstancer = true;
						}

						if (HEU_GeneralUtility.HasValidInstanceAttribute(_session, nodeID, partInfo.id, HEU_Defines.HEIGHTFIELD_TREEINSTANCE_PROTOTYPEINDEX))
						{
							isScatterInstancer = true;
						}
					}

					if (isScatterInstancer || isAttribInstancer || partInfo.type == HAPI_PartType.HAPI_PARTTYPE_INSTANCER)
					{
						if (isScatterInstancer)
						{
							scatterInstancerParts.Add(partInfo);
						}

						if (partInfo.type == HAPI_PartType.HAPI_PARTTYPE_INSTANCER || isAttribInstancer)
						{
							instancerParts.Add(partInfo);
						}
					}
					else if (partInfo.type == HAPI_PartType.HAPI_PARTTYPE_VOLUME)
					{
						volumeParts.Add(partInfo);
					}
					else if (partInfo.type == HAPI_PartType.HAPI_PARTTYPE_CURVE)
					{
						curveParts.Add(partInfo);
					}
					else if(HEU_HAPIUtility.IsSupportedPolygonType(partInfo.type))
					{
						meshParts.Add(partInfo);
					}
					else
					{
						string partName = HEU_SessionManager.GetString(partInfo.nameSH, _session);
						SetLog(HEU_LoadData.LoadStatus.ERROR, string.Format("Part {0} with type {1} is not supported for GeoSync.", partName, partInfo.type));
					}
				}
			}
			else if(geoInfo.type == HAPI_GeoType.HAPI_GEOTYPE_CURVE)
			{
				SetLog(HEU_LoadData.LoadStatus.ERROR, string.Format("Currently {0} geo type is not implemented for threaded geo loading!", geoInfo.type));
			}

			return true;
		}

		public bool GenerateTerrainBuffers(HEU_SessionBase session, HAPI_NodeId nodeID, List<HAPI_PartInfo> volumeParts,
			List<HAPI_PartInfo> scatterInstancerParts, out List<HEU_LoadBufferVolume> volumeBuffers)
		{
			volumeBuffers = null;
			if (volumeParts.Count == 0)
			{
				return true;
			}

			volumeBuffers = new List<HEU_LoadBufferVolume>();
			int detailResolution = 0;

			int numParts = volumeParts.Count;
			for (int i = 0; i < numParts; ++i)
			{
				HAPI_VolumeInfo volumeInfo = new HAPI_VolumeInfo();
				bool bResult = session.GetVolumeInfo(nodeID, volumeParts[i].id, ref volumeInfo);
				if (!bResult || volumeInfo.tupleSize != 1 || volumeInfo.zLength != 1 || volumeInfo.storage != HAPI_StorageType.HAPI_STORAGETYPE_FLOAT)
				{
					SetLog(HEU_LoadData.LoadStatus.ERROR, "This heightfield is not supported. Please check documentation.");
					return false;
				}

				if (volumeInfo.xLength != volumeInfo.yLength)
				{
					SetLog(HEU_LoadData.LoadStatus.ERROR, "Non-square sized terrain not supported.");
					return false;
				}

				string volumeName = HEU_SessionManager.GetString(volumeInfo.nameSH, session);

				HFLayerType layerType = HEU_TerrainUtility.GetHeightfieldLayerType(session, nodeID, volumeParts[i].id, volumeName);

				//Debug.LogFormat("Index: {0}, Part id: {1}, Part Name: {2}, Volume Name: {3}", i, volumeParts[i].id, HEU_SessionManager.GetString(volumeParts[i].nameSH), volumeName);

				// Ignoring mask layer because it is Houdini-specific (same behaviour as regular HDA terrain generation)
				if (layerType == HFLayerType.MASK)
				{
					continue;
				}

				HEU_LoadBufferVolumeLayer layer = new HEU_LoadBufferVolumeLayer();
				layer._layerName = volumeName;
				layer._partID = volumeParts[i].id;
				layer._heightMapWidth = volumeInfo.xLength;
				layer._heightMapHeight = volumeInfo.yLength;
				layer._layerType = layerType;

				Matrix4x4 volumeTransformMatrix = HEU_HAPIUtility.GetMatrixFromHAPITransform(ref volumeInfo.transform, false);
				layer._position = HEU_HAPIUtility.GetPosition(ref volumeTransformMatrix);
				Vector3 scale = HEU_HAPIUtility.GetScale(ref volumeTransformMatrix);

				// Calculate real terrain size in both Houdini and Unity.
				// The height values will be mapped over this terrain size.
				float gridSpacingX = scale.x * 2f;
				float gridSpacingY = scale.y * 2f;
				layer._terrainSizeX = Mathf.Round((volumeInfo.xLength - 1) * gridSpacingX);
				layer._terrainSizeY = Mathf.Round((volumeInfo.yLength - 1) * gridSpacingY);

				// Get volume bounds for calculating position offset
				session.GetVolumeBounds(nodeID, volumeParts[i].id, 
					out layer._minBounds.x, out layer._minBounds.y, out layer._minBounds.z, 
					out layer._maxBounds.x, out layer._maxBounds.y, out layer._maxBounds.z, 
					out layer._center.x, out layer._center.y, out layer._center.z);

				// Look up TerrainLayer file via attribute if user has set it
				layer._layerPath = HEU_GeneralUtility.GetAttributeStringValueSingle(session, nodeID, volumeParts[i].id,
					HEU_Defines.DEFAULT_UNITY_HEIGHTFIELD_TERRAINLAYER_FILE_ATTR, HAPI_AttributeOwner.HAPI_ATTROWNER_PRIM);

				if (layerType != HFLayerType.DETAIL)
				{
					layer._hasLayerAttributes = HEU_TerrainUtility.VolumeLayerHasAttributes(session, nodeID, volumeParts[i].id);

					if (layer._hasLayerAttributes)
					{
						LoadStringFromAttribute(session, nodeID, volumeParts[i].id, HEU_Defines.DEFAULT_UNITY_HEIGHTFIELD_TEXTURE_DIFFUSE_ATTR, ref layer._diffuseTexturePath);
						LoadStringFromAttribute(session, nodeID, volumeParts[i].id, HEU_Defines.DEFAULT_UNITY_HEIGHTFIELD_TEXTURE_MASK_ATTR, ref layer._maskTexturePath);
						LoadStringFromAttribute(session, nodeID, volumeParts[i].id, HEU_Defines.DEFAULT_UNITY_HEIGHTFIELD_TEXTURE_NORMAL_ATTR, ref layer._normalTexturePath);

						LoadFloatFromAttribute(session, nodeID, volumeParts[i].id, HEU_Defines.DEFAULT_UNITY_HEIGHTFIELD_NORMAL_SCALE_ATTR, ref layer._normalScale);
						LoadFloatFromAttribute(session, nodeID, volumeParts[i].id, HEU_Defines.DEFAULT_UNITY_HEIGHTFIELD_METALLIC_ATTR, ref layer._metallic);
						LoadFloatFromAttribute(session, nodeID, volumeParts[i].id, HEU_Defines.DEFAULT_UNITY_HEIGHTFIELD_SMOOTHNESS_ATTR, ref layer._smoothness);

						LoadLayerColorFromAttribute(session, nodeID, volumeParts[i].id, HEU_Defines.DEFAULT_UNITY_HEIGHTFIELD_SPECULAR_ATTR, ref layer._specularColor);
						LoadLayerVector2FromAttribute(session, nodeID, volumeParts[i].id, HEU_Defines.DEFAULT_UNITY_HEIGHTFIELD_TILE_OFFSET_ATTR, ref layer._tileOffset);
						LoadLayerVector2FromAttribute(session, nodeID, volumeParts[i].id, HEU_Defines.DEFAULT_UNITY_HEIGHTFIELD_TILE_SIZE_ATTR, ref layer._tileSize);
					}

					// Get the height values from Houdini along with the min and max height range.
					layer._normalizedHeights = HEU_TerrainUtility.GetNormalizedHeightmapFromPartWithMinMax(
						_session, nodeID, volumeParts[i].id, volumeInfo.xLength, volumeInfo.yLength,
						ref layer._minHeight, ref layer._maxHeight, ref layer._heightRange,
						(layerType == HFLayerType.HEIGHT));
				}

				// Get the tile index, if it exists, for this part
				HAPI_AttributeInfo tileAttrInfo = new HAPI_AttributeInfo();
				int[] tileAttrData = new int[0];
				HEU_GeneralUtility.GetAttribute(session, nodeID, volumeParts[i].id, HEU_Defines.HAPI_HEIGHTFIELD_TILE_ATTR, ref tileAttrInfo, ref tileAttrData, session.GetAttributeIntData);

				int tileIndex = 0;
				if (tileAttrInfo.exists && tileAttrData.Length == 1)
				{
					tileIndex = tileAttrData[0];
				}

				// Add layer based on tile index
				if (tileIndex >= 0)
				{
					HEU_LoadBufferVolume volumeBuffer = null;
					for(int j = 0; j < volumeBuffers.Count; ++j)
					{
						if (volumeBuffers[j]._tileIndex == tileIndex)
						{
							volumeBuffer = volumeBuffers[j];
							break;
						}
					}

					if (volumeBuffer == null)
					{
						volumeBuffer = new HEU_LoadBufferVolume();
						volumeBuffer.InitializeBuffer(volumeParts[i].id, volumeName, false, false);

						volumeBuffer._tileIndex = tileIndex;
						volumeBuffers.Add(volumeBuffer);
					}

					if (layerType == HFLayerType.HEIGHT)
					{
						// Height layer always first layer
						volumeBuffer._splatLayers.Insert(0, layer);

						volumeBuffer._heightMapWidth = layer._heightMapWidth;
						volumeBuffer._heightMapHeight = layer._heightMapHeight;
						volumeBuffer._terrainSizeX = layer._terrainSizeX;
						volumeBuffer._terrainSizeY = layer._terrainSizeY;
						volumeBuffer._heightRange = layer._heightRange;

						// The terrain heightfield position in y requires offset of min height
						layer._position.y += layer._minHeight;

						// Use y position from attribute if user has set it
						float userYPos;
						if (HEU_GeneralUtility.GetAttributeFloatSingle(session, nodeID, volumeParts[i].id,
							HEU_Defines.DEFAULT_UNITY_HEIGHTFIELD_YPOS, out userYPos))
						{
							layer._position.y = userYPos;
						}

						// Look up TerrainData file path via attribute if user has set it
						volumeBuffer._terrainDataPath = HEU_GeneralUtility.GetAttributeStringValueSingle(session, nodeID, volumeBuffer._id,
							HEU_Defines.DEFAULT_UNITY_HEIGHTFIELD_TERRAINDATA_FILE_ATTR, HAPI_AttributeOwner.HAPI_ATTROWNER_PRIM);

						// Look up TerrainData export file path via attribute if user has set it
						volumeBuffer._terrainDataExportPath = HEU_GeneralUtility.GetAttributeStringValueSingle(session, nodeID, volumeBuffer._id,
							HEU_Defines.DEFAULT_UNITY_HEIGHTFIELD_TERRAINDATA_EXPORT_FILE_ATTR, HAPI_AttributeOwner.HAPI_ATTROWNER_PRIM);

						// Load the TreePrototype buffers
						List<HEU_TreePrototypeInfo> treePrototypeInfos = HEU_TerrainUtility.GetTreePrototypeInfosFromPart(session, nodeID, volumeBuffer._id);
						if (treePrototypeInfos != null)
						{
							if (volumeBuffer._scatterTrees == null)
							{
								volumeBuffer._scatterTrees = new HEU_VolumeScatterTrees();
							}
							volumeBuffer._scatterTrees._treePrototypInfos = treePrototypeInfos;
						}

						HEU_TerrainUtility.PopulateDetailProperties(session, nodeID, 
							volumeBuffer._id, ref volumeBuffer._detailProperties);

						// Get specified material if any
						volumeBuffer._specifiedTerrainMaterialName = HEU_GeneralUtility.GetMaterialAttributeValueFromPart(session,
							nodeID, volumeBuffer._id);
					}
					else if(layer._layerType == HFLayerType.DETAIL)
					{
						// Get detail prototype
						HEU_DetailPrototype detailPrototype = null;
						HEU_TerrainUtility.PopulateDetailPrototype(session, nodeID, volumeParts[i].id, ref detailPrototype);

						int[,] detailMap = HEU_TerrainUtility.GetDetailMapFromPart(session, nodeID,
							volumeParts[i].id, out detailResolution);

						volumeBuffer._detailPrototypes.Add(detailPrototype);
						volumeBuffer._detailMaps.Add(detailMap);

						// Set the detail resolution which is formed from the detail layer
						if (volumeBuffer._detailProperties == null)
						{
							volumeBuffer._detailProperties = new HEU_DetailProperties();
						}
						volumeBuffer._detailProperties._detailResolution = detailResolution;
					}
					else
					{
						volumeBuffer._splatLayers.Add(layer);
					}
				}

				Sleep();
			}

			// Each volume buffer is a self contained terrain tile
			foreach(HEU_LoadBufferVolume volumeBuffer in volumeBuffers)
			{
				List<HEU_LoadBufferVolumeLayer> layers = volumeBuffer._splatLayers;
				//Debug.LogFormat("Heightfield: tile={0}, layers={1}", tile._tileIndex, layers.Count);

				int heightMapWidth = volumeBuffer._heightMapWidth;
				int heightMapHeight = volumeBuffer._heightMapHeight;

				int numLayers = layers.Count;
				if (numLayers > 0)
				{
					// Convert heightmap values from Houdini to Unity
					volumeBuffer._heightMap = HEU_TerrainUtility.ConvertHeightMapHoudiniToUnity(heightMapWidth, heightMapHeight, layers[0]._normalizedHeights);

					Sleep();

					// Convert splatmap values from Houdini to Unity.
					// Start at 2nd index since height is strictly for height values (not splatmap).
					List<float[]> heightFields = new List<float[]>();
					for(int m = 1; m < numLayers; ++m)
					{
						// Ignore Detail layers as they are handled differently
						if(layers[m]._layerType != HFLayerType.DETAIL)
						{
							heightFields.Add(layers[m]._normalizedHeights);
						}
					}

					// The number of maps are the number of splatmaps (ie. non height/mask layers)
					int numMaps = heightFields.Count;
					if (numMaps > 0)
					{
						// Using the first splatmap size for all splatmaps
						volumeBuffer._splatMaps = HEU_TerrainUtility.ConvertHeightFieldToAlphaMap(layers[1]._heightMapWidth, layers[1]._heightMapHeight, heightFields);
					}
					else
					{
						volumeBuffer._splatMaps = null;
					}

					// TODO: revisit how the position is calculated
					volumeBuffer._position = new Vector3(
						volumeBuffer._terrainSizeX + volumeBuffer._splatLayers[0]._minBounds.x, 
						volumeBuffer._splatLayers[0]._position.y, 
						volumeBuffer._splatLayers[0]._minBounds.z);
				}
			}

			// Process the scatter instancer parts to get the scatter data
			for (int i = 0; i < scatterInstancerParts.Count; ++i)
			{
				// Find the terrain tile (use primitive attr). Assume 0 tile if not set (i.e. not split into tiles)
				int terrainTile = 0;
				HAPI_AttributeInfo tileAttrInfo = new HAPI_AttributeInfo();
				int[] tileAttrData = new int[0];
				if (!HEU_GeneralUtility.GetAttribute(session, nodeID, scatterInstancerParts[i].id, HEU_Defines.HAPI_HEIGHTFIELD_TILE_ATTR, ref tileAttrInfo, ref tileAttrData, session.GetAttributeIntData))
				{
					// Try part 0 (the height layer) to get the tile index.
					// For scatter points merged with HF, in some cases the part ID doesn't have the tile attribute.
					HEU_GeneralUtility.GetAttribute(session, nodeID, 0, HEU_Defines.HAPI_HEIGHTFIELD_TILE_ATTR, ref tileAttrInfo, ref tileAttrData, session.GetAttributeIntData);
				}

				if (tileAttrData != null && tileAttrData.Length > 0)
				{
					terrainTile = tileAttrData[0];
				}

				// Find the volume layer associated with this part using the terrain tile index
				HEU_LoadBufferVolume volumeBuffer = GetLoadBufferVolumeFromTileIndex(terrainTile, volumeBuffers);
				if (volumeBuffer == null)
				{
					continue;
				}

				HEU_TerrainUtility.PopulateScatterTrees(session, nodeID, scatterInstancerParts[i].id, scatterInstancerParts[i].pointCount, ref volumeBuffer._scatterTrees);
			}

			return true;
		}

		private void LoadStringFromAttribute(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_NodeId partID, string attrName, ref string strValue)
		{
			HAPI_AttributeInfo attrInfo = new HAPI_AttributeInfo();
			string[] strAttr = HEU_GeneralUtility.GetAttributeStringData(session, geoID, partID, attrName, ref attrInfo);
			if (strAttr != null && strAttr.Length > 0 && !string.IsNullOrEmpty(strAttr[0]))
			{
				strValue = strAttr[0];
			}
		}

		private void LoadFloatFromAttribute(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_NodeId partID, string attrName, ref float floatValue)
		{
			HAPI_AttributeInfo attrInfo = new HAPI_AttributeInfo();
			float[] attrValues = new float[0];
			HEU_GeneralUtility.GetAttribute(session, geoID, partID, attrName, ref attrInfo, ref attrValues, session.GetAttributeFloatData);
			if (attrValues != null && attrValues.Length > 0)
			{
				floatValue = attrValues[0];
			}
		}

		private void LoadLayerColorFromAttribute(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_NodeId partID, string attrName, ref Color colorValue)
		{
			HAPI_AttributeInfo attrInfo = new HAPI_AttributeInfo();
			float[] attrValues = new float[0];
			HEU_GeneralUtility.GetAttribute(session, geoID, partID, attrName, ref attrInfo, ref attrValues, session.GetAttributeFloatData);
			if (attrValues != null && attrValues.Length >= 3)
			{
				if (attrInfo.tupleSize >= 3)
				{
					colorValue[0] = attrValues[0];
					colorValue[1] = attrValues[1];
					colorValue[2] = attrValues[2];

					if (attrInfo.tupleSize == 4 && attrValues.Length == 4)
					{
						colorValue[3] = attrValues[3];
					}
					else
					{
						colorValue[3] = 1f;
					}
				}
			}
		}

		private void LoadLayerVector2FromAttribute(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_NodeId partID, string attrName, ref Vector2 vectorValue)
		{
			HAPI_AttributeInfo attrInfo = new HAPI_AttributeInfo();
			float[] attrValues = new float[0];
			HEU_GeneralUtility.GetAttribute(session, geoID, partID, attrName, ref attrInfo, ref attrValues, session.GetAttributeFloatData);
			if (attrValues != null && attrValues.Length == 2)
			{
				if (attrInfo.tupleSize == 2)
				{
					vectorValue[0] = attrValues[0];
					vectorValue[1] = attrValues[1];
				}
			}
		}

		private void Sleep()
		{
			System.Threading.Thread.Sleep(0);
		}

		public bool GenerateMeshBuffers(HEU_SessionBase session, HAPI_NodeId nodeID, List<HAPI_PartInfo> meshParts, 
			bool bSplitPoints, bool bUseLODGroups, bool bGenerateUVs, bool bGenerateTangents, bool bGenerateNormals,
			out List<HEU_LoadBufferMesh> meshBuffers)
        {
			meshBuffers = null;
			if (meshParts.Count == 0)
            {
                return true;
            }

			bool bSuccess = true;
			string assetCacheFolderPath = "";

			meshBuffers = new List<HEU_LoadBufferMesh>();

			foreach(HAPI_PartInfo partInfo in meshParts)
			{
				HAPI_NodeId geoID = nodeID;
				int partID = partInfo.id;
				string partName = HEU_SessionManager.GetString(partInfo.nameSH, session);
				bool bPartInstanced = partInfo.isInstanced;

				if (partInfo.type == HAPI_PartType.HAPI_PARTTYPE_MESH)
				{
					List<HEU_MaterialData> materialCache = new List<HEU_MaterialData>();

					HEU_GenerateGeoCache geoCache = HEU_GenerateGeoCache.GetPopulatedGeoCache(session, -1, geoID, partID, bUseLODGroups,
						materialCache, assetCacheFolderPath);
					if (geoCache == null)
					{
						// Failed to get necessary info for generating geometry.
						SetLog(HEU_LoadData.LoadStatus.ERROR, string.Format("Failed to generate geometry cache for part: {0}", partName));
						continue;
					}

					geoCache._materialCache = materialCache;

					// Build the GeoGroup using points or vertices
					bool bResult = false;
					List<HEU_GeoGroup> LODGroupMeshes = null;
					int defaultMaterialKey = 0;
					if (bSplitPoints)
					{
						bResult = HEU_GenerateGeoCache.GenerateGeoGroupUsingGeoCachePoints(session, geoCache, bGenerateUVs, bGenerateTangents, bGenerateNormals, bUseLODGroups, bPartInstanced,
							out LODGroupMeshes, out defaultMaterialKey);
					}
					else
					{
						bResult = HEU_GenerateGeoCache.GenerateGeoGroupUsingGeoCacheVertices(session, geoCache, bGenerateUVs, bGenerateTangents, bGenerateNormals, bUseLODGroups, bPartInstanced,
							out LODGroupMeshes, out defaultMaterialKey);
					}

					if (bResult)
					{
						HEU_LoadBufferMesh meshBuffer = new HEU_LoadBufferMesh();
						meshBuffer.InitializeBuffer(partID, partName, partInfo.isInstanced, false);

						meshBuffer._geoCache = geoCache;
						meshBuffer._LODGroupMeshes = LODGroupMeshes;
						meshBuffer._defaultMaterialKey = defaultMaterialKey;

						meshBuffer._bGenerateUVs = bGenerateUVs;
						meshBuffer._bGenerateTangents = bGenerateTangents;
						meshBuffer._bGenerateNormals = bGenerateNormals;
						meshBuffer._bPartInstanced = partInfo.isInstanced;

						meshBuffers.Add(meshBuffer);
					}
					else
					{
						SetLog(HEU_LoadData.LoadStatus.ERROR, string.Format("Failed to generated geometry for part: {0}", partName));
					}
				}
			}

			return bSuccess;
        }


		public bool GenerateInstancerBuffers(HEU_SessionBase session, HAPI_NodeId nodeID, List<HAPI_PartInfo> instancerParts,
			out List<HEU_LoadBufferInstancer> instancerBuffers)
		{
			instancerBuffers = null;
			if (instancerParts.Count == 0)
			{
				return true;
			}

			instancerBuffers = new List<HEU_LoadBufferInstancer>();

			foreach (HAPI_PartInfo partInfo in instancerParts)
			{
				HAPI_NodeId geoID = nodeID;
				HAPI_PartId partID = partInfo.id;
				string partName = HEU_SessionManager.GetString(partInfo.nameSH, session);

				HEU_LoadBufferInstancer newBuffer = null;
				if (partInfo.instancedPartCount > 0)
				{
					// Part instancer
					newBuffer = GeneratePartsInstancerBuffer(session, geoID, partID, partName, partInfo);
				}
				else if (partInfo.vertexCount == 0 && partInfo.pointCount > 0)
				{
					// Point attribute instancer
					newBuffer = GeneratePointAttributeInstancerBuffer(session, geoID, partID, partName, partInfo);
				}
				else
				{
					SetLog(HEU_LoadData.LoadStatus.ERROR, string.Format("Invalid instanced part count: {0} for part {1}", partInfo.instancedPartCount, partName));
					continue;
				}

				if (newBuffer != null)
				{
					instancerBuffers.Add(newBuffer);
				}
			}

			return true;
		}

		private HEU_LoadBufferInstancer GeneratePartsInstancerBuffer(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID, string partName, HAPI_PartInfo partInfo)
		{
			// Get the instance node IDs to get the geometry to be instanced.
			// Get the instanced count to all the instances. These will end up being mesh references to the mesh from instance node IDs.

			// Get each instance's transform
			HAPI_Transform[] instanceTransforms = new HAPI_Transform[partInfo.instanceCount];
			if (!HEU_GeneralUtility.GetArray3Arg(geoID, partID, HAPI_RSTOrder.HAPI_SRT, session.GetInstancerPartTransforms, instanceTransforms, 0, partInfo.instanceCount))
			{
				SetLog(HEU_LoadData.LoadStatus.ERROR, string.Format("Unable to get instance transforms for part {0}", partName));
				return null;
			}

			// Get part IDs for the parts being instanced
			HAPI_NodeId[] instanceNodeIDs = new HAPI_NodeId[partInfo.instancedPartCount];
			if (!HEU_GeneralUtility.GetArray2Arg(geoID, partID, session.GetInstancedPartIds, instanceNodeIDs, 0, partInfo.instancedPartCount))
			{
				SetLog(HEU_LoadData.LoadStatus.ERROR, string.Format("Unable to get instance node IDs for part {0}", partName));
				return null;
			}

			// Get instance names if set
			string[] instancePrefixes = null;
			HAPI_AttributeInfo instancePrefixAttrInfo = new HAPI_AttributeInfo();
			HEU_GeneralUtility.GetAttributeInfo(session, geoID, partID, HEU_Defines.DEFAULT_INSTANCE_PREFIX_ATTR, ref instancePrefixAttrInfo);
			if (instancePrefixAttrInfo.exists)
			{
				instancePrefixes = HEU_GeneralUtility.GetAttributeStringData(session, geoID, partID, HEU_Defines.DEFAULT_INSTANCE_PREFIX_ATTR, ref instancePrefixAttrInfo);
			}

			HEU_LoadBufferInstancer instancerBuffer = new HEU_LoadBufferInstancer();
			instancerBuffer.InitializeBuffer(partID, partName, partInfo.isInstanced, true);

			instancerBuffer._instanceTransforms = instanceTransforms;
			instancerBuffer._instanceNodeIDs = instanceNodeIDs;
			instancerBuffer._instancePrefixes = instancePrefixes;

			return instancerBuffer;
		}

		private HEU_LoadBufferInstancer GeneratePointAttributeInstancerBuffer(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID, 
			string partName, HAPI_PartInfo partInfo)
		{
			int numInstances = partInfo.pointCount;
			if (numInstances <= 0)
			{
				return null;
			}

			// Find type of instancer
			string instanceAttrName = HEU_PluginSettings.InstanceAttr;
			string unityInstanceAttrName = HEU_PluginSettings.UnityInstanceAttr;
			string instancePrefixAttrName = HEU_Defines.DEFAULT_INSTANCE_PREFIX_ATTR;

			HAPI_AttributeInfo instanceAttrInfo = new HAPI_AttributeInfo();
			HAPI_AttributeInfo unityInstanceAttrInfo = new HAPI_AttributeInfo();
			HAPI_AttributeInfo instancePrefixAttrInfo = new HAPI_AttributeInfo();
			

			HEU_GeneralUtility.GetAttributeInfo(session, geoID, partID, instanceAttrName, ref instanceAttrInfo);
			HEU_GeneralUtility.GetAttributeInfo(session, geoID, partID, unityInstanceAttrName, ref unityInstanceAttrInfo);

			if (unityInstanceAttrInfo.exists)
			{
				// Object instancing via existing Unity object (path from point attribute)

				HAPI_Transform[] instanceTransforms = new HAPI_Transform[numInstances];
				if (!HEU_GeneralUtility.GetArray3Arg(geoID, partID, HAPI_RSTOrder.HAPI_SRT, session.GetInstanceTransformsOnPart, instanceTransforms, 0, numInstances))
				{
					return null;
				}

				string[] instancePrefixes = null;
				HEU_GeneralUtility.GetAttributeInfo(session, geoID, partID, instancePrefixAttrName, ref instancePrefixAttrInfo);
				if (instancePrefixAttrInfo.exists)
				{
					instancePrefixes = HEU_GeneralUtility.GetAttributeStringData(session, geoID, partID, instancePrefixAttrName, ref instancePrefixAttrInfo);
				}

				string[] assetPaths = null;

				// Attribute owner type determines whether to use single (detail) or multiple (point) asset(s) as source
				if (unityInstanceAttrInfo.owner == HAPI_AttributeOwner.HAPI_ATTROWNER_POINT || unityInstanceAttrInfo.owner == HAPI_AttributeOwner.HAPI_ATTROWNER_DETAIL)
				{
					assetPaths = HEU_GeneralUtility.GetAttributeStringData(session, geoID, partID, unityInstanceAttrName, ref unityInstanceAttrInfo);
				}
				else
				{
					// Other attribute owned types are unsupported
					SetLog(HEU_LoadData.LoadStatus.ERROR, string.Format("Unsupported attribute owner {0} for attribute {1}", 
						unityInstanceAttrInfo.owner, unityInstanceAttrName));
					return null;
				}

				if (assetPaths == null)
				{
					SetLog(HEU_LoadData.LoadStatus.ERROR, "Unable to get instanced asset path from attribute!");
					return null;
				}

				HEU_LoadBufferInstancer instancerBuffer = new HEU_LoadBufferInstancer();
				instancerBuffer.InitializeBuffer(partID, partName, partInfo.isInstanced, true);

				instancerBuffer._instanceTransforms = instanceTransforms;
				instancerBuffer._instancePrefixes = instancePrefixes;
				instancerBuffer._assetPaths = assetPaths;

				HAPI_AttributeInfo collisionGeoAttrInfo = new HAPI_AttributeInfo();
				HEU_GeneralUtility.GetAttributeInfo(session, geoID, partID, HEU_PluginSettings.CollisionGroupName, ref collisionGeoAttrInfo);
				if (collisionGeoAttrInfo.owner == HAPI_AttributeOwner.HAPI_ATTROWNER_POINT
					|| collisionGeoAttrInfo.owner == HAPI_AttributeOwner.HAPI_ATTROWNER_DETAIL)
				{
					instancerBuffer._collisionAssetPaths = HEU_GeneralUtility.GetAttributeStringData(session, geoID, partID, HEU_PluginSettings.CollisionGroupName, ref collisionGeoAttrInfo);
				}

				return instancerBuffer;
			}
			else if (instanceAttrInfo.exists)
			{
				// Object instancing via internal object path is not supported
				SetLog(HEU_LoadData.LoadStatus.ERROR, string.Format("Object instancing is not supported (part {0})!", partName));
			}
			else
			{
				// Standard object instancing via single Houdini object is not supported
				SetLog(HEU_LoadData.LoadStatus.ERROR, string.Format("Object instancing is not supported (part {0})!", partName));
			}

			return null;
		}


		public static HEU_LoadBufferVolume GetLoadBufferVolumeFromTileIndex(int tileIndex, List<HEU_LoadBufferVolume> buffers)
		{
			foreach(HEU_LoadBufferVolume buffer in buffers)
			{
				if (buffer._tileIndex == tileIndex)
				{
					return buffer;
				}
			}
			return null;
		}

		//	DATA ------------------------------------------------------------------------------------------------------

		// Setup
		private string _filePath;
		private HEU_GeoSync _geoSync;
		private HEU_SessionBase _session;

		private HEU_GenerateOptions _generateOptions;

		private HEU_LoadData _loadData;

		public class HEU_LoadData
		{
			public HAPI_NodeId _fileNodeID;

			public enum LoadStatus
			{
				NONE,
				STARTED,
				SUCCESS,
				ERROR,
			}
			public LoadStatus _loadStatus;

			public string _logStr;

			public HEU_GeoGroup _geoGroup;

			public HEU_SessionBase _session;

			public List<HEU_LoadBufferVolume> _terrainBuffers;

			public List<HEU_LoadBufferMesh> _meshBuffers;

			public List<HEU_LoadBufferInstancer> _instancerBuffers;
		}
	}

}   // namespace HoudiniEngineUnity