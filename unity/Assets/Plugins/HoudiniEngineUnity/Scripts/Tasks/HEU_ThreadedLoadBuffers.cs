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


	public class HEU_LoadBufferBase
	{
		public HAPI_NodeId _id;
		public string _name;
		public bool _bInstanced;
		public bool _bInstancer;

		public HEU_GeneratedOutput _generatedOutput;

		public void InitializeBuffer(HAPI_NodeId id, string name, bool bInstanced, bool bInstancer)
		{
			_id = id;
			_name = name;
			_bInstanced = bInstanced;
			_bInstancer = bInstancer;
		}
	}

	public class HEU_LoadBufferMesh : HEU_LoadBufferBase
	{
		public HEU_GenerateGeoCache _geoCache;
		public List<HEU_GeoGroup> _LODGroupMeshes;

		public int _defaultMaterialKey;

		public bool _bGenerateUVs;
		public bool _bGenerateTangents;
		public bool _bGenerateNormals;
		public bool _bPartInstanced;
	}

	public class HEU_LoadBufferVolume : HEU_LoadBufferBase
	{
		public int _tileIndex;
		public List<HEU_LoadBufferVolumeLayer> _splatLayers = new List<HEU_LoadBufferVolumeLayer>();

		public int _heightMapWidth;
		public int _heightMapHeight;
		public float[,] _heightMap;
		public float[,,] _splatMaps;

		public float _terrainSizeX;
		public float _terrainSizeY;
		public float _heightRange;

		public Vector3 _position;

		public string _terrainDataPath;
		public string _terrainDataExportPath;

		public HEU_VolumeScatterTrees _scatterTrees;

		// Detail Layers
		public List<HEU_DetailPrototype> _detailPrototypes = new List<HEU_DetailPrototype>();
		public List<int[,]> _detailMaps = new List<int[,]>();
		public HEU_DetailProperties _detailProperties;

		// Specified terrain material
		public string _specifiedTerrainMaterialName;
	}

	public class HEU_LoadBufferVolumeLayer
	{
		public string _layerName;
		public HAPI_PartId _partID;
		public int _heightMapWidth;
		public int _heightMapHeight;
		public float _strength = 1.0f;

		public string _diffuseTexturePath;
		public string _maskTexturePath;
		public float _metallic = 0f;
		public string _normalTexturePath;
		public float _normalScale = 0.5f;
		public float _smoothness = 0f;
		public Color _specularColor = Color.gray;
		public Vector2 _tileSize = Vector2.zero;
		public Vector2 _tileOffset = Vector2.zero;

		public bool _uiExpanded;
		public int _tile = 0;

		public float[] _normalizedHeights;
		public float _minHeight;
		public float _maxHeight;
		public float _heightRange;

		public float _terrainSizeX;
		public float _terrainSizeY;

		public Vector3 _position;
		public Vector3 _minBounds;
		public Vector3 _maxBounds;
		public Vector3 _center;

		public string _layerPath;

		public bool _hasLayerAttributes;

		public HFLayerType _layerType;
	}

	public class HEU_LoadBufferInstancer : HEU_LoadBufferBase
	{
		public HAPI_Transform[] _instanceTransforms;
		public string[] _instancePrefixes;

		// Instancing with parts as source
		public HAPI_NodeId[] _instanceNodeIDs;

		// Instancing with asset path as source (single or multi)
		public string[] _assetPaths;

		// Override collision asset paths
		public string[] _collisionAssetPaths;
	}

}   // HoudiniEngineUnity