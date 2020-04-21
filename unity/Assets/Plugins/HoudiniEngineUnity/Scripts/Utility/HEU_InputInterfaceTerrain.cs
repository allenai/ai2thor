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
	/// This class provides functionality for uploading Unity terrain data from a gameobject
	/// into Houdini through a heightfield node network.
	/// It derives from the HEU_InputInterface and registers with HEU_InputUtility so that it
	/// can be used automatically when uploading terrain data.
	/// </summary>
	public class HEU_InputInterfaceTerrain : HEU_InputInterface
	{
#if UNITY_EDITOR
		/// <summary>
		/// Registers this input inteface for Unity meshes on
		/// the callback after scripts are reloaded in Unity.
		/// </summary>
		[InitializeOnLoadMethod]
		[UnityEditor.Callbacks.DidReloadScripts]
		private static void OnScriptsReloaded()
		{
			HEU_InputInterfaceTerrain inputInterface = new HEU_InputInterfaceTerrain();
			HEU_InputUtility.RegisterInputInterface(inputInterface);
		}
#endif

		public HEU_InputInterfaceTerrain() : base(priority: DEFAULT_PRIORITY)
		{

		}

		/// <summary>
		/// Creates a heightfield network inside the same object as connectNodeID.
		/// Uploads the terrain data from inputObject into the new heightfield network, incuding
		/// all terrain layers/alphamaps.
		/// </summary>
		/// <param name="session">Session that connectNodeID exists in</param>
		/// <param name="connectNodeID">The node to connect the network to. Most likely a SOP/merge node</param>
		/// <param name="inputObject">The gameobject containing the Terrain components</param>
		/// <param name="inputNodeID">The created heightfield network node ID</param>
		/// <returns>True if created network and uploaded heightfield data.</returns>
		public override bool CreateInputNodeWithDataUpload(HEU_SessionBase session, HAPI_NodeId connectNodeID, GameObject inputObject, out HAPI_NodeId inputNodeID)
		{
			inputNodeID = HEU_Defines.HEU_INVALID_NODE_ID;

			// Create input node, cook it, then upload the geometry data

			if (!HEU_HAPIUtility.IsNodeValidInHoudini(session, connectNodeID))
			{
				Debug.LogError("Connection node is invalid.");
				return false;
			}

			HEU_InputDataTerrain idt = GenerateTerrainDataFromGameObject(inputObject);
			if (idt == null)
			{
				return false;
			}

			HAPI_NodeId parentNodeID = HEU_HAPIUtility.GetParentNodeID(session, connectNodeID);
			idt._parentNodeID = parentNodeID;

			if (!CreateHeightFieldInputNode(session, idt))
			{
				return false;
			}

			HAPI_VolumeInfo volumeInfo = new HAPI_VolumeInfo();
			if (!UploadHeightValuesWithTransform(session, idt, ref volumeInfo))
			{
				return false;
			}

			inputNodeID = idt._heightfieldNodeID;

			bool bMaskSet = false;
			if (!UploadAlphaMaps(session, idt, ref volumeInfo, out bMaskSet))
			{
				return false;
			}

			if (!bMaskSet)
			{
				// While the default HF created by the input node also creates a default mask layer,
				// we still need to set the mask layer's transform. So this uses the base VolumeInfo
				// to do just that.
				if (!SetMaskLayer(session, idt, ref volumeInfo))
				{
					return false;
				}
			}

			if (!session.CookNode(inputNodeID, false))
			{
				Debug.LogError("New input node failed to cook!");
				return false;
			}

			return true;
		}

		private bool SetMaskLayer(HEU_SessionBase session, HEU_InputDataTerrain idt, ref HAPI_VolumeInfo baseVolumeInfo)
		{
			int sizeX = idt._terrainData.alphamapWidth;
			int sizeY = idt._terrainData.alphamapHeight;
			int totalSize = sizeX * sizeY;

			float[] maskValues = new float[totalSize];
			if (!SetHeightFieldData(session, idt._maskNodeID, 0, maskValues, HEU_Defines.HAPI_HEIGHTFIELD_LAYERNAME_MASK, ref baseVolumeInfo))
			{
				return false;
			}

			if (!session.CommitGeo(idt._maskNodeID))
			{
				Debug.LogError("Failed to commit volume layer 'mask'");
				return false;
			}

			return true;
		}

		public override bool IsThisInputObjectSupported(GameObject inputObject)
		{
			if (inputObject != null)
			{
				if (inputObject.GetComponentInChildren<Terrain>(true) != null)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Create the main heightfield network for input.
		/// </summary>
		/// <param name="session"></param>
		/// <param name="idt"></param>
		/// <returns>True if successfully created the network</returns>
		public bool CreateHeightFieldInputNode(HEU_SessionBase session, HEU_InputDataTerrain idt)
		{
			idt._heightfieldNodeID = HEU_Defines.HEU_INVALID_NODE_ID;
			idt._heightNodeID = HEU_Defines.HEU_INVALID_NODE_ID;
			idt._maskNodeID = HEU_Defines.HEU_INVALID_NODE_ID;
			idt._mergeNodeID = HEU_Defines.HEU_INVALID_NODE_ID;

			// Create the HeightField node network
			bool bResult = session.CreateHeightfieldInputNode(idt._parentNodeID, idt._heightFieldName, idt._numPointsX, idt._numPointsY, idt._voxelSize,
				out idt._heightfieldNodeID, out idt._heightNodeID, out idt._maskNodeID, out idt._mergeNodeID);
			if (!bResult 
				|| idt._heightfieldNodeID == HEU_Defines.HEU_INVALID_NODE_ID 
				|| idt._heightNodeID == HEU_Defines.HEU_INVALID_NODE_ID
				|| idt._maskNodeID == HEU_Defines.HEU_INVALID_NODE_ID 
				|| idt._mergeNodeID == HEU_Defines.HEU_INVALID_NODE_ID)
			{
				Debug.LogError("Failed to create new heightfield node in Houdini session!");
				return false;
			}

			if (!session.CookNode(idt._heightNodeID, false))
			{
				Debug.LogError("New input node failed to cook!");
				return false;
			}

			return true;
		}

		/// <summary>
		/// Upload the base height layer into heightfield network.
		/// </summary>
		/// <param name="session"></param>
		/// <param name="idt"></param>
		/// <returns></returns>
		public bool UploadHeightValuesWithTransform(HEU_SessionBase session, HEU_InputDataTerrain idt, ref HAPI_VolumeInfo volumeInfo)
		{
			// Get Geo, Part, and Volume infos
			HAPI_GeoInfo geoInfo = new HAPI_GeoInfo();
			if (!session.GetGeoInfo(idt._heightNodeID, ref geoInfo))
			{
				Debug.LogError("Unable to get geo info from heightfield node!");
				return false;
			}

			HAPI_PartInfo partInfo = new HAPI_PartInfo();
			if (!session.GetPartInfo(geoInfo.nodeId, 0, ref partInfo))
			{
				Debug.LogError("Unable to get part info from heightfield node!");
				return false;
			}

			volumeInfo = new HAPI_VolumeInfo();
			if (!session.GetVolumeInfo(idt._heightNodeID, 0, ref volumeInfo))
			{
				Debug.LogError("Unable to get volume info from heightfield node!");
				return false;
			}

			if (volumeInfo.xLength != Mathf.RoundToInt(idt._numPointsX / idt._voxelSize)
				|| volumeInfo.yLength != Mathf.RoundToInt(idt._numPointsY / idt._voxelSize)
				|| idt._terrainData.heightmapResolution != volumeInfo.xLength
				|| idt._terrainData.heightmapResolution != volumeInfo.yLength)
			{
				Debug.LogError("Created heightfield in Houdini differs in voxel size from input terrain!");
				return false;
			}

			// Update volume infos, and set it. This is required.
			volumeInfo.tileSize = 1;
			volumeInfo.type = HAPI_VolumeType.HAPI_VOLUMETYPE_HOUDINI;
			volumeInfo.storage = HAPI_StorageType.HAPI_STORAGETYPE_FLOAT;
			volumeInfo.transform = idt._transform;

			volumeInfo.minX = 0;
			volumeInfo.minY = 0;
			volumeInfo.minZ = 0;

			volumeInfo.tupleSize = 1;
			volumeInfo.tileSize = 1;

			volumeInfo.hasTaper = false;
			volumeInfo.xTaper = 0f;
			volumeInfo.yTaper = 0f;

			if (!session.SetVolumeInfo(idt._heightNodeID, partInfo.id, ref volumeInfo))
			{
				Debug.LogError("Unable to set volume info on input heightfield node!");
				return false;
			}

			// Now set the height data
			float[,] heights = idt._terrainData.GetHeights(0, 0, volumeInfo.xLength, volumeInfo.yLength);
			int sizeX = heights.GetLength(0);
			int sizeY = heights.GetLength(1);
			int totalSize = sizeX * sizeY;

			// Convert to single array
			float[] heightsArr = new float[totalSize];
			for (int j = 0; j < sizeY; j++)
			{
				for (int i = 0; i < sizeX; i++)
				{
					// Flip for coordinate system change
					float h = heights[i, (sizeY - j - 1)];

					heightsArr[i + j * sizeX] = h * idt._heightScale;
				}
			}

			// Set the base height layer
			if (!session.SetHeightFieldData(idt._heightNodeID, 0, HEU_Defines.HAPI_HEIGHTFIELD_LAYERNAME_HEIGHT, heightsArr, 0, totalSize))
			{
				Debug.LogError("Unable to set height values on input heightfield node!");
				return false;
			}

			SetTerrainDataAttributesToHeightField(session, geoInfo.nodeId, 0, idt._terrainData);

			SetTreePrototypes(session, geoInfo.nodeId, 0, idt._terrainData);

			if (!session.CommitGeo(idt._heightNodeID))
			{
				Debug.LogError("Unable to commit geo on input heightfield node!");
				return false;
			}

			return true;
		}

		/// <summary>
		/// Upload the alphamaps (TerrainLayers) into heightfield network.
		/// </summary>
		/// <param name="session"></param>
		/// <param name="idt"></param>
		/// <param name="baseVolumeInfo">The valid base height HAPI_VolumeInfo</param>
		/// <param name="bMaskSet">This is set to true if a mask layer was uploaded</param>
		/// <returns>True if successfully uploaded all layers</returns>
		public bool UploadAlphaMaps(HEU_SessionBase session, HEU_InputDataTerrain idt, ref HAPI_VolumeInfo baseVolumeInfo, out bool bMaskSet)
		{
			bool bResult = true;
			bMaskSet = false;

			int alphaLayers = idt._terrainData.alphamapLayers;
			if (alphaLayers < 1)
			{
				return bResult;
			}

			int sizeX = idt._terrainData.alphamapWidth;
			int sizeY = idt._terrainData.alphamapHeight;
			int totalSize = sizeX * sizeY;

			float[,,] alphaMaps = idt._terrainData.GetAlphamaps(0, 0, sizeX, sizeY);

			float[][] alphaMapsConverted = new float[alphaLayers][];

			// Convert the alphamap layers to double arrays.
			for (int m = 0; m < alphaLayers; ++m)
			{
				alphaMapsConverted[m] = new float[totalSize];
				for (int j = 0; j < sizeY; j++)
				{
					for (int i = 0; i < sizeX; i++)
					{
						// Flip for coordinate system change
						float h = alphaMaps[i, (sizeY - j - 1), m];

						alphaMapsConverted[m][i + j * sizeX] = h;
					}
				}
			}

			// Create volume layers for all alpha maps and upload values.
			bool bMaskLayer = false;
			int inputLayerIndex = 1;
			for (int m = 0; m < alphaLayers; ++m)
			{
#if UNITY_2018_3_OR_NEWER
				string layerName = idt._terrainData.terrainLayers[m].name;
#else
				string layerName = "unity_alphamap_" + m + 1;
#endif

				// The Unity layer name could contain '.terrainlayer' and spaces. Remove them because Houdini doesn't allow
				// spaces, and the extension isn't necessary.
				layerName = layerName.Replace(" ", "_");
				int extIndex = layerName.LastIndexOf(HEU_Defines.HEU_EXT_TERRAINLAYER);
				if (extIndex > 0)
				{
					layerName = layerName.Remove(extIndex);
				}
				//Debug.Log("Processing terrain layer: " + layerName);

				HAPI_NodeId alphaLayerID = HEU_Defines.HEU_INVALID_NODE_ID;

				if (layerName.Equals(HEU_Defines.HAPI_HEIGHTFIELD_LAYERNAME_HEIGHT))
				{
					// Skip height (base) layer (since it has been uploaded already)
					continue;
				}
				else if (layerName.Equals(HEU_Defines.HAPI_HEIGHTFIELD_LAYERNAME_MASK))
				{
					//Debug.Log("Mask layer found! Skipping creating the HF.");
					bMaskSet = true;
					bMaskLayer = true;
					alphaLayerID = idt._maskNodeID;
				}
				else
				{
					bMaskLayer = false;

					if (!session.CreateHeightfieldInputVolumeNode(idt._heightfieldNodeID, out alphaLayerID, layerName,
						Mathf.RoundToInt(sizeX * idt._voxelSize), Mathf.RoundToInt(sizeY * idt._voxelSize), idt._voxelSize))
					{
						bResult = false;
						Debug.LogError("Failed to create input volume node for layer " + layerName);
						break;
					}
				}

				//Debug.Log("Uploading terrain layer: " + layerName);

				if (!SetHeightFieldData(session, alphaLayerID, 0, alphaMapsConverted[m], layerName, ref baseVolumeInfo))
				{
					bResult = false;
					break;
				}

#if UNITY_2018_3_OR_NEWER
				SetTerrainLayerAttributesToHeightField(session, alphaLayerID, 0, idt._terrainData.terrainLayers[m]);
#endif

				if (!session.CommitGeo(alphaLayerID))
				{
					bResult = false;
					Debug.LogError("Failed to commit volume layer " + layerName);
					break;
				}

				if (!bMaskLayer)
				{
					// Connect to the merge node but starting from index 1 since index 0 is height layer
					if (!session.ConnectNodeInput(idt._mergeNodeID, inputLayerIndex + 1, alphaLayerID, 0))
					{
						bResult = false;
						Debug.LogError("Unable to connect new volume node for layer " + layerName);
						break;
					}

					inputLayerIndex++;
				}
			}

			return bResult;
		}

		/// <summary>
		/// Helper to set heightfield data for a specific volume node.
		/// Used for a specific terrain layer.
		/// </summary>
		/// <param name="session">Session that the volume node resides in.</param>
		/// <param name="volumeNodeID">ID of the target volume node</param>
		/// <param name="partID">Part ID</param>
		/// <param name="heightValues">Array of height or alpha values</param>
		/// <param name="heightFieldName">Name of the layer</param>
		/// <returns>True if successfully uploaded heightfield values</returns>
		public bool SetHeightFieldData(HEU_SessionBase session, HAPI_NodeId volumeNodeID, HAPI_PartId partID, float[] heightValues, string heightFieldName, ref HAPI_VolumeInfo baseVolumeInfo)
		{
			// Cook the node to get infos below
			if (!session.CookNode(volumeNodeID, false))
			{
				return false;
			}

			// Get Geo, Part, and Volume infos
			HAPI_GeoInfo geoInfo = new HAPI_GeoInfo();
			if (!session.GetGeoInfo(volumeNodeID, ref geoInfo))
			{
				return false;
			}

			HAPI_PartInfo partInfo = new HAPI_PartInfo();
			if (!session.GetPartInfo(geoInfo.nodeId, partID, ref partInfo))
			{
				return false;
			}

			HAPI_VolumeInfo volumeInfo = new HAPI_VolumeInfo();
			if (!session.GetVolumeInfo(volumeNodeID, partInfo.id, ref volumeInfo))
			{
				return false;
			}

			volumeInfo.tileSize = 1;
			// Use same transform as base layer
			volumeInfo.transform = baseVolumeInfo.transform;

			if (!session.SetVolumeInfo(volumeNodeID, partInfo.id, ref volumeInfo))
			{
				Debug.LogError("Unable to set volume info on input heightfield node!");
				return false;
			}

			// Now set the height data
			if (!session.SetHeightFieldData(geoInfo.nodeId, partInfo.id, heightFieldName, heightValues, 0, heightValues.Length))
			{
				Debug.LogError("Unable to set height values on input heightfield node!");
				return false;
			}

			return true;
		}

		/// <summary>
		/// Writes out the TerrainData file path as a string attribute (primitive-owned) for the specified heightfield volume.
		/// </summary>
		/// <param name="session">Current Houdini session</param>
		/// <param name="geoNodeID">Geometry object ID</param>
		/// <param name="partID">Part ID (volume)</param>
		/// <param name="terrainData">The TerrainData's file path is set as attribute</param>
		/// <returns>True if successfully added the attribute.</returns>
		public bool SetTerrainDataAttributesToHeightField(HEU_SessionBase session, HAPI_NodeId geoNodeID, HAPI_PartId partID, TerrainData terrainData)
		{
			string assetPath = HEU_AssetDatabase.GetAssetPath(terrainData);
			if (string.IsNullOrEmpty(assetPath))
			{
				return false;
			}

			HAPI_AttributeInfo attrInfo = new HAPI_AttributeInfo();
			attrInfo.exists = true;
			attrInfo.owner = HAPI_AttributeOwner.HAPI_ATTROWNER_PRIM;
			attrInfo.storage = HAPI_StorageType.HAPI_STORAGETYPE_STRING;
			attrInfo.count = 1;
			attrInfo.tupleSize = 1;
			attrInfo.originalOwner = HAPI_AttributeOwner.HAPI_ATTROWNER_INVALID;

			if (!session.AddAttribute(geoNodeID, partID, HEU_Defines.DEFAULT_UNITY_HEIGHTFIELD_TERRAINDATA_FILE_ATTR, ref attrInfo))
			{
				Debug.LogError("Failed to add TerrainData file attribute to input heightfield.");
				return false;
			}

			string[] pathData = new string[] { assetPath };
			if (!session.SetAttributeStringData(geoNodeID, partID, HEU_Defines.DEFAULT_UNITY_HEIGHTFIELD_TERRAINDATA_FILE_ATTR, ref attrInfo, pathData, 0, 1))
			{
				Debug.LogError("Failed to set TerrainData file name to input heightfield.");
				return false;
			}

			return true;
		}

#if UNITY_2018_3_OR_NEWER
		/// <summary>
		/// Writes out the TerrainLayer file path as a string attribute (primitive-owned) for the specified heightfield volume.
		/// </summary>
		/// <param name="session">Current Houdini session</param>
		/// <param name="geoNodeID">Geometry object ID</param>
		/// <param name="partID">Part ID (volume)</param>
		/// <param name="terrainLayer">The TerrainLayer's file path is set as attribute</param>
		/// <returns>True if successfully added the attribute.</returns>
		public bool SetTerrainLayerAttributesToHeightField(HEU_SessionBase session, HAPI_NodeId geoNodeID, HAPI_PartId partID, TerrainLayer terrainLayer)
		{
			string assetPath = HEU_AssetDatabase.GetAssetPath(terrainLayer);
			if (string.IsNullOrEmpty(assetPath))
			{
				return false;
			}

			HAPI_AttributeInfo attrInfo = new HAPI_AttributeInfo();
			attrInfo.exists = true;
			attrInfo.owner = HAPI_AttributeOwner.HAPI_ATTROWNER_PRIM;
			attrInfo.storage = HAPI_StorageType.HAPI_STORAGETYPE_STRING;
			attrInfo.count = 1;
			attrInfo.tupleSize = 1;
			attrInfo.originalOwner = HAPI_AttributeOwner.HAPI_ATTROWNER_INVALID;

			if (!session.AddAttribute(geoNodeID, partID, HEU_Defines.DEFAULT_UNITY_HEIGHTFIELD_TERRAINLAYER_FILE_ATTR, ref attrInfo))
			{
				Debug.LogError("Failed to add TerrainLayer file attribute to input heightfield.");
				return false;
			}

			string[] pathData = new string[] { assetPath };
			if (!session.SetAttributeStringData(geoNodeID, partID, HEU_Defines.DEFAULT_UNITY_HEIGHTFIELD_TERRAINLAYER_FILE_ATTR, ref attrInfo, pathData, 0, 1))
			{
				Debug.LogError("Failed to set TerrainLayer file name to input heightfield");
				return false;
			}

			return true;
		}
#endif

		/// <summary>
		/// Set the given TerrainData's TreePrototyes as attributes on the given part.
		/// The TreePrototypes as stored a string attributes where the name is HEU_Defines.HEIGHTFIELD_TREEPROTOTYPE + index.
		/// The string value is the tree prefab's file path comme-separated with the bend factor:
		/// e.g: Assets/Trees/redtree.prefab,0.9
		/// This does nothing if the given TerrainData doesn't have TreePrototype.
		/// </summary>
		/// <param name="session">Houdini Engine session</param>
		/// <param name="geoNodeID">Geometry object ID</param>
		/// <param name="partID">Part ID</param>
		/// <param name="terrainData">The TerrainData containing TreePrototypes.</param>
		public void SetTreePrototypes(HEU_SessionBase session, HAPI_NodeId geoNodeID, HAPI_PartId partID, TerrainData terrainData)
		{
			TreePrototype[] treePrototypes = terrainData.treePrototypes;
			if (treePrototypes == null || treePrototypes.Length == 0)
			{
				return;
			}

			// For each prototype, fill up a string attribute owned by primtive.
			// The string format is: tree_prefab_path,bend_factor
			string prefabPath;
			float bendFactor;
			for(int i = 0; i < treePrototypes.Length; ++i)
			{
				if (treePrototypes[i] == null)
				{
					continue;
				}

				prefabPath = HEU_AssetDatabase.GetAssetPath(treePrototypes[i].prefab);
				if (prefabPath == null)
				{
					continue;
				}

				bendFactor = treePrototypes[i].bendFactor;

				HAPI_AttributeInfo attrInfo = new HAPI_AttributeInfo();
				attrInfo.exists = true;
				attrInfo.owner = HAPI_AttributeOwner.HAPI_ATTROWNER_PRIM;
				attrInfo.storage = HAPI_StorageType.HAPI_STORAGETYPE_STRING;
				attrInfo.count = 1;
				attrInfo.tupleSize = 1;
				attrInfo.originalOwner = HAPI_AttributeOwner.HAPI_ATTROWNER_INVALID;

				string attrName = HEU_Defines.HEIGHTFIELD_TREEPROTOTYPE + i.ToString();
				if (!session.AddAttribute(geoNodeID, partID, attrName, ref attrInfo))
				{
					Debug.LogError("Failed to add TreePrototype string attribute to input heightfield.");
					return;
				}

				string[] pathData = new string[] { string.Format("{0},{1}", prefabPath, bendFactor) };
				if (!session.SetAttributeStringData(geoNodeID, partID, attrName, ref attrInfo, pathData, 0, 1))
				{
					Debug.LogError("Failed to set TreePrototype string value to input heightfield.");
					return;
				}
			}
		}

		public void SetTreeInstances(HEU_SessionBase session, HAPI_NodeId geoNodeID, HAPI_PartId partID, TerrainData terrainData)
		{
			TreeInstance[] treeInstances = terrainData.treeInstances;
			if (treeInstances == null || treeInstances.Length == 0)
			{
				return;
			}

			for(int i = 0; i < treeInstances.Length; ++i)
			{
				// Upload:
				// treeInstances[i].color
				// treeInstances[i].lightmapColor
				// treeInstances[i].heightScale
				// treeInstances[i].widthScale
				// treeInstances[i].position
				// treeInstances[i].prototypeIndex

				// Upload position as UVs?
			}
		}

		/// <summary>
		/// Holds terrain data for uploading as heightfields
		/// </summary>
		public class HEU_InputDataTerrain : HEU_InputData
		{
			// Default values
			public string _heightFieldName = "input";
			public HAPI_NodeId _parentNodeID = -1;
			public float _voxelSize = 2;

			// Acquired from input object
			public Terrain _terrain;
			public TerrainData _terrainData;

			public int _numPointsX;
			public int _numPointsY;

			public HAPI_Transform _transform = new HAPI_Transform();

			public float _heightScale;

			// Retrieved from Houdini
			public HAPI_NodeId _heightfieldNodeID = HEU_Defines.HEU_INVALID_NODE_ID;
			public HAPI_NodeId _heightNodeID = HEU_Defines.HEU_INVALID_NODE_ID;
			public HAPI_NodeId _maskNodeID = HEU_Defines.HEU_INVALID_NODE_ID;
			public HAPI_NodeId _mergeNodeID = HEU_Defines.HEU_INVALID_NODE_ID;
		}

		/// <summary>
		/// Generates heightfield/terrain data from the given object relevant for uploading to Houdini.
		/// </summary>
		/// <param name="inputObject"></param>
		/// <returns>Valid input object or null if given object is not supported</returns>
		public HEU_InputDataTerrain GenerateTerrainDataFromGameObject(GameObject inputObject)
		{
			HEU_InputDataTerrain inputData = null;

			Terrain terrain = inputObject.GetComponent<Terrain>();
			if (terrain != null)
			{
				TerrainData terrainData = terrain.terrainData;

				Vector3 terrainSize = terrainData.size;
				if (terrainSize.x != terrainSize.z)
				{
					Debug.LogError("Only square sized terrains are supported for input! Change to square size and try again.");
					return null;
				}

				inputData = new HEU_InputDataTerrain();
				inputData._inputObject = inputObject;
				inputData._terrain = terrain;
				inputData._terrainData = terrainData;

				// Height values in Unity are normalized between 0 and 1, so this height scale
				// will multiply them before uploading to Houdini.
				inputData._heightScale = terrainSize.y;

				// Terrain heightMapResolution is the pixel resolution, which we set to the number of voxels
				// by dividing the terrain size with it. In Houdini, this is the Grid Spacing.
				inputData._voxelSize = terrainSize.x / inputData._terrainData.heightmapResolution;

				// This is the number of heightfield voxels on each dimension.
				inputData._numPointsX = Mathf.RoundToInt(inputData._terrainData.heightmapResolution * inputData._voxelSize);
				inputData._numPointsY = Mathf.RoundToInt(inputData._terrainData.heightmapResolution * inputData._voxelSize);

				Matrix4x4 transformMatrix = inputObject.transform.localToWorldMatrix;
				//HAPI_TransformEuler transformEuler = HEU_HAPIUtility.GetHAPITransformFromMatrix(ref transformMatrix);

				// Volume transform used for all heightfield layers
				inputData._transform = new HAPI_Transform(false);

				// Unity terrain pivots are at bottom left, but Houdini uses centered heightfields so
				// apply local position offset by half sizes and account for coordinate change
				inputData._transform.position[0] = terrainSize.z * 0.5f;
				inputData._transform.position[1] = -terrainSize.x * 0.5f;
				inputData._transform.position[2] = 0;

				// Volume scale controls final size, but requires to be divided by 2
				inputData._transform.scale[0] = terrainSize.x * 0.5f;
				inputData._transform.scale[1] = terrainSize.z * 0.5f;
				inputData._transform.scale[2] = 0.5f;

				inputData._transform.rotationQuaternion[0] = 0f;
				inputData._transform.rotationQuaternion[1] = 0f;
				inputData._transform.rotationQuaternion[2] = 0f;
				inputData._transform.rotationQuaternion[3] = 1f;
			}

			return inputData;
		}
	}

}   // HoudiniEngineUnity
						 