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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace HoudiniEngineUnity
{
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Typedefs (copy these from HEU_Common.cs)
	using HAPI_NodeId = System.Int32;


	/// <summary>
	/// This class provides functionality for uploading Unity mesh data from gameobjects
	/// into Houdini through an input node.
	/// It derives from the HEU_InputInterface and registers with HEU_InputUtility so that it
	/// can be used automatically when uploading mesh data.
	/// </summary>
	public class HEU_InputInterfaceMesh : HEU_InputInterface
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
			HEU_InputInterfaceMesh inputInterface = new HEU_InputInterfaceMesh();
			HEU_InputUtility.RegisterInputInterface(inputInterface);
		}
#endif

		private HEU_InputInterfaceMesh() : base(priority: DEFAULT_PRIORITY)
		{
			
		}

		/// <summary>
		/// Creates a mesh input node and uploads the mesh data from inputObject.
		/// </summary>
		/// <param name="session">Session that connectNodeID exists in</param>
		/// <param name="connectNodeID">The node to connect the network to. Most likely a SOP/merge node</param>
		/// <param name="inputObject">The gameobject containing the mesh components</param>
		/// <param name="inputNodeID">The created input node ID</param>
		/// <returns>True if created network and uploaded mesh data.</returns>
		public override bool CreateInputNodeWithDataUpload(HEU_SessionBase session, HAPI_NodeId connectNodeID, GameObject inputObject, out HAPI_NodeId inputNodeID)
		{
			inputNodeID = HEU_Defines.HEU_INVALID_NODE_ID;

			// Create input node, cook it, then upload the geometry data

			if (!HEU_HAPIUtility.IsNodeValidInHoudini(session, connectNodeID))
			{
				Debug.LogError("Connection node is invalid.");
				return false;
			}

			// Get upload meshes from input object
			HEU_InputDataMeshes inputMeshes = GenerateMeshDatasFromGameObject(inputObject);
			if (inputMeshes == null || inputMeshes._inputMeshes == null || inputMeshes._inputMeshes.Count == 0)
			{
				Debug.LogError("No valid meshes found on input objects.");
				return false;
			}

			string inputName = null;
			HAPI_NodeId newNodeID = HEU_Defines.HEU_INVALID_NODE_ID;
			session.CreateInputNode(out newNodeID, inputName);
			if (newNodeID == HEU_Defines.HEU_INVALID_NODE_ID || !HEU_HAPIUtility.IsNodeValidInHoudini(session, newNodeID))
			{
				Debug.LogError("Failed to create new input node in Houdini session!");
				return false;
			}

			inputNodeID = newNodeID;

			if (!session.CookNode(inputNodeID, false))
			{
				Debug.LogError("New input node failed to cook!");
				return false;
			}

			return UploadData(session, inputNodeID, inputMeshes);
		}

		public override bool IsThisInputObjectSupported(GameObject inputObject)
		{
			if (inputObject != null)
			{
				if (inputObject.GetComponent<LODGroup>() != null)
				{
					return true;
				}
				else if (inputObject.GetComponentInChildren<MeshFilter>(true) != null)
				{
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Upload the inputData (mesh geometry) into the input node with inputNodeID.
		/// </summary>
		/// <param name="session">Session that the input node exists in</param>
		/// <param name="inputNodeID">ID of the input node</param>
		/// <param name="inputData">Container of the mesh geometry</param>
		/// <returns>True if successfully uploaded data</returns>
		public bool UploadData(HEU_SessionBase session, HAPI_NodeId inputNodeID, HEU_InputData inputData)
		{
			HEU_InputDataMeshes inputDataMeshes = inputData as HEU_InputDataMeshes;
			if (inputDataMeshes == null)
			{
				Debug.LogError("Expected HEU_InputDataMeshes type for inputData, but received unsupported type.");
				return false;
			}

			List<Vector3> vertices = new List<Vector3>();
			List<Vector3> normals = new List<Vector3>();
			List<Vector2> uvs = new List<Vector2>();
			List<Color> colors = new List<Color>();

			List<int> pointIndexList = new List<int>();
			List<int> vertIndexList = new List<int>();

			int numMaterials = 0;

			int numMeshes = inputDataMeshes._inputMeshes.Count;

			// Get the parent's world transform, so when there are multiple child meshes,
			// can merge and apply their local transform after subtracting their parent's world transform
			Matrix4x4 rootInvertTransformMatrix = Matrix4x4.identity;
			if (numMeshes > 1)
			{
				rootInvertTransformMatrix = inputDataMeshes._inputObject.transform.worldToLocalMatrix;
			}

			// Always using the first submesh topology. This doesn't support mixed topology (triangles and quads).
			MeshTopology meshTopology = inputDataMeshes._inputMeshes[0]._mesh.GetTopology(0);

			int numVertsPerFace = 3;
			if (meshTopology == MeshTopology.Quads)
			{
				numVertsPerFace = 4;
			}

			// For all meshes:
			// Accumulate vertices, normals, uvs, colors, and indices.
			// Keep track of indices start and count for each mesh for later when uploading material assignments and groups.
			// Find shared vertices, and use unique set of vertices to use as point positions.
			// Need to reindex indices for both unique vertices, as well as vertex attributes.
			for (int i = 0; i < numMeshes; ++i)
			{
				Vector3[] meshVertices = inputDataMeshes._inputMeshes[i]._mesh.vertices;
				Matrix4x4 localToWorld = rootInvertTransformMatrix * inputDataMeshes._inputMeshes[i]._transform.localToWorldMatrix;

				List<Vector3> uniqueVertices = new List<Vector3>();

				// Keep track of old vertex positions (old vertex slot points to new unique vertex slot)
				int[] reindexVertices = new int[meshVertices.Length];
				Dictionary<Vector3, int> reindexMap = new Dictionary<Vector3, int>();

				// For each vertex, check against subsequent vertices for shared positions.
				for (int a = 0; a < meshVertices.Length; ++a)
				{
					Vector3 va = meshVertices[a];

					if (!reindexMap.ContainsKey(va))
					{
						if (numMeshes > 1 && !inputDataMeshes._hasLOD)
						{
							// For multiple meshes that are not LODs, apply local transform on vertices to get the merged mesh.
							uniqueVertices.Add(localToWorld.MultiplyPoint(va));
						}
						else
						{
							uniqueVertices.Add(va);
						}

						// Reindex to point to unique vertex slot
						reindexVertices[a] = uniqueVertices.Count - 1;
						reindexMap[va] = uniqueVertices.Count - 1;
					}
					else
					{
						reindexVertices[a] = reindexMap[va];
					}
				}

				int vertexOffset = vertices.Count;
				vertices.AddRange(uniqueVertices);

				Vector3[] meshNormals = inputDataMeshes._inputMeshes[i]._mesh.normals;
				Vector2[] meshUVs = inputDataMeshes._inputMeshes[i]._mesh.uv;
				Color[] meshColors = inputDataMeshes._inputMeshes[i]._mesh.colors;

				inputDataMeshes._inputMeshes[i]._indexStart = new uint[inputDataMeshes._inputMeshes[i]._numSubMeshes];
				inputDataMeshes._inputMeshes[i]._indexCount = new uint[inputDataMeshes._inputMeshes[i]._numSubMeshes];

				// For each submesh:
				// Generate face to point index -> pointIndexList
				// Generate face to vertex attribute index -> vertIndexList
				for (int j = 0; j < inputDataMeshes._inputMeshes[i]._numSubMeshes; ++j)
				{
					int indexStart = pointIndexList.Count;
					int vertIndexStart = vertIndexList.Count;

					// Indices have to be re-indexed with our own offset 
					// (using GetIndices to generalize triangles and quad indices)
					int[] meshIndices = inputDataMeshes._inputMeshes[i]._mesh.GetIndices(j);
					int numIndices = meshIndices.Length;
					for (int k = 0; k < numIndices; ++k)
					{
						int originalIndex = meshIndices[k];
						meshIndices[k] = reindexVertices[originalIndex];

						pointIndexList.Add(vertexOffset + meshIndices[k]);
						vertIndexList.Add(vertIndexStart + k);

						if (meshNormals != null && (originalIndex < meshNormals.Length))
						{
							normals.Add(meshNormals[originalIndex]);
						}

						if (meshUVs != null && (originalIndex < meshUVs.Length))
						{
							uvs.Add(meshUVs[originalIndex]);
						}

						if (meshColors != null && (originalIndex < meshColors.Length))
						{
							colors.Add(meshColors[originalIndex]);
						}
					}

					inputDataMeshes._inputMeshes[i]._indexStart[j] = (uint)indexStart;
					inputDataMeshes._inputMeshes[i]._indexCount[j] = (uint)(pointIndexList.Count) - inputDataMeshes._inputMeshes[i]._indexStart[j];
				}

				numMaterials += inputDataMeshes._inputMeshes[i]._materials != null ? inputDataMeshes._inputMeshes[i]._materials.Length : 0;
			}

			// It is possible for some meshes to not have normals/uvs/colors while others do.
			// In the case where an attribute is missing on some meshes, we clear out those attributes so we don't upload
			// partial attribute data.
			int totalAllVertexCount = vertIndexList.Count;
			if (normals.Count != totalAllVertexCount)
			{
				normals = null;
			}

			if (uvs.Count != totalAllVertexCount)
			{
				uvs = null;
			}

			if (colors.Count != totalAllVertexCount)
			{
				colors = null;
			}


			HAPI_PartInfo partInfo = new HAPI_PartInfo();
			partInfo.faceCount = vertIndexList.Count / numVertsPerFace;
			partInfo.vertexCount = vertIndexList.Count;
			partInfo.pointCount = vertices.Count;
			partInfo.pointAttributeCount = 1;
			partInfo.vertexAttributeCount = 0;
			partInfo.primitiveAttributeCount = 0;
			partInfo.detailAttributeCount = 0;

			//Debug.LogFormat("Faces: {0}; Vertices: {1}; Verts/Face: {2}", partInfo.faceCount, partInfo.vertexCount, numVertsPerFace);

			if (normals != null && normals.Count > 0)
			{
				partInfo.vertexAttributeCount++;
			}

			if (uvs != null && uvs.Count > 0)
			{
				partInfo.vertexAttributeCount++;
			}

			if (colors != null && colors.Count > 0)
			{
				partInfo.vertexAttributeCount++;
			}

			if (numMaterials > 0)
			{
				partInfo.primitiveAttributeCount++;
			}

			if (numMeshes > 0)
			{
				partInfo.primitiveAttributeCount++;
			}

			if (inputDataMeshes._hasLOD)
			{
				partInfo.primitiveAttributeCount++;
				partInfo.detailAttributeCount++;
			}

			HAPI_GeoInfo displayGeoInfo = new HAPI_GeoInfo();
			if (!session.GetDisplayGeoInfo(inputNodeID, ref displayGeoInfo))
			{
				return false;
			}

			HAPI_NodeId displayNodeID = displayGeoInfo.nodeId;

			if (!session.SetPartInfo(displayNodeID, 0, ref partInfo))
			{
				Debug.LogError("Failed to set input part info. ");
				return false;
			}

			int[] faceCounts = new int[partInfo.faceCount];
			for (int i = 0; i < partInfo.faceCount; ++i)
			{
				faceCounts[i] = numVertsPerFace;
			}

			int[] faceIndices = pointIndexList.ToArray();

			if (!HEU_GeneralUtility.SetArray2Arg(displayNodeID, 0, session.SetFaceCount, faceCounts, 0, partInfo.faceCount))
			{
				Debug.LogError("Failed to set input geometry face counts.");
				return false;
			}

			if (!HEU_GeneralUtility.SetArray2Arg(displayNodeID, 0, session.SetVertexList, faceIndices, 0, partInfo.vertexCount))
			{
				Debug.LogError("Failed to set input geometry indices.");
				return false;
			}

			if (!HEU_InputMeshUtility.SetMeshPointAttribute(session, displayNodeID, 0, HEU_Defines.HAPI_ATTRIB_POSITION, 3, vertices.ToArray(), ref partInfo, true))
			{
				Debug.LogError("Failed to set input geometry position.");
				return false;
			}

			int[] vertIndices = vertIndexList.ToArray();

			//if(normals != null && !SetMeshPointAttribute(session, displayNodeID, 0, HEU_Defines.HAPI_ATTRIB_NORMAL, 3, normals.ToArray(), ref partInfo, true))
			if (normals != null && !HEU_InputMeshUtility.SetMeshVertexAttribute(session, displayNodeID, 0, HEU_Defines.HAPI_ATTRIB_NORMAL, 3, normals.ToArray(), vertIndices, ref partInfo, true))
			{
				Debug.LogError("Failed to set input geometry normals.");
				return false;
			}

			if (uvs != null && uvs.Count > 0)
			{
				Vector3[] uvs3 = new Vector3[uvs.Count];
				for (int i = 0; i < uvs.Count; ++i)
				{
					uvs3[i][0] = uvs[i][0];
					uvs3[i][1] = uvs[i][1];
					uvs3[i][2] = 0;
				}
				//if(!SetMeshPointAttribute(session, displayNodeID, 0, HEU_Defines.HAPI_ATTRIB_UV, 3, uvs3, ref partInfo, false))
				if (!HEU_InputMeshUtility.SetMeshVertexAttribute(session, displayNodeID, 0, HEU_Defines.HAPI_ATTRIB_UV, 3, uvs3, vertIndices, ref partInfo, false))
				{
					Debug.LogError("Failed to set input geometry UVs.");
					return false;
				}
			}

			if (colors != null && colors.Count > 0)
			{
				Vector3[] rgb = new Vector3[colors.Count];
				float[] alpha = new float[colors.Count];
				for (int i = 0; i < colors.Count; ++i)
				{
					rgb[i][0] = colors[i].r;
					rgb[i][1] = colors[i].g;
					rgb[i][2] = colors[i].b;

					alpha[i] = colors[i].a;
				}

				//if(!SetMeshPointAttribute(session, displayNodeID, 0, HEU_Defines.HAPI_ATTRIB_COLOR, 3, rgb, ref partInfo, false))
				if (!HEU_InputMeshUtility.SetMeshVertexAttribute(session, displayNodeID, 0, HEU_Defines.HAPI_ATTRIB_COLOR, 3, rgb, vertIndices, ref partInfo, false))
				{
					Debug.LogError("Failed to set input geometry colors.");
					return false;
				}

				//if(!SetMeshPointAttribute(session, displayNodeID, 0, HEU_Defines.HAPI_ATTRIB_ALPHA, 1, alpha, ref partInfo, false))
				if (!HEU_InputMeshUtility.SetMeshVertexFloatAttribute(session, displayNodeID, 0, HEU_Defines.HAPI_ATTRIB_ALPHA, 1, alpha, vertIndices, ref partInfo))
				{
					Debug.LogError("Failed to set input geometry color alpha.");
					return false;
				}
			}

			// Set material names for round-trip perservation of material assignment
			// Each HEU_UploadMeshData might have a list of submeshes and materials
			// These are all combined into a single mesh, with group names
			if (numMaterials > 0)
			{
				bool bFoundAtleastOneValidMaterial = false;

				string[] materialIDs = new string[partInfo.faceCount];
				for (int g = 0; g < inputDataMeshes._inputMeshes.Count; ++g)
				{
					if (inputDataMeshes._inputMeshes[g]._numSubMeshes != inputDataMeshes._inputMeshes[g]._materials.Length)
					{
						// Number of submeshes should equal number of materials since materials determine submeshes
						continue;
					}

					for (int i = 0; i < inputDataMeshes._inputMeshes[g]._materials.Length; ++i)
					{
						string materialName = HEU_AssetDatabase.GetAssetPathWithSubAssetSupport(inputDataMeshes._inputMeshes[g]._materials[i]);
						if (materialName == null)
						{
							materialName = "";
						}
						else if (materialName.StartsWith(HEU_Defines.DEFAULT_UNITY_BUILTIN_RESOURCES))
						{
							materialName = HEU_AssetDatabase.GetUniqueAssetPathForUnityAsset(inputDataMeshes._inputMeshes[g]._materials[i]);
						}

						bFoundAtleastOneValidMaterial |= !string.IsNullOrEmpty(materialName);

						int faceStart = (int)inputDataMeshes._inputMeshes[g]._indexStart[i] / numVertsPerFace;
						int faceEnd = faceStart + ((int)inputDataMeshes._inputMeshes[g]._indexCount[i] / numVertsPerFace);
						for (int m = faceStart; m < faceEnd; ++m)
						{
							materialIDs[m] = materialName;
						}
					}
				}

				if (bFoundAtleastOneValidMaterial)
				{
					HAPI_AttributeInfo materialIDAttrInfo = new HAPI_AttributeInfo();
					materialIDAttrInfo.exists = true;
					materialIDAttrInfo.owner = HAPI_AttributeOwner.HAPI_ATTROWNER_PRIM;
					materialIDAttrInfo.storage = HAPI_StorageType.HAPI_STORAGETYPE_STRING;
					materialIDAttrInfo.count = partInfo.faceCount;
					materialIDAttrInfo.tupleSize = 1;
					materialIDAttrInfo.originalOwner = HAPI_AttributeOwner.HAPI_ATTROWNER_INVALID;

					if (!session.AddAttribute(displayNodeID, 0, HEU_PluginSettings.UnityMaterialAttribName, ref materialIDAttrInfo))
					{
						Debug.LogError("Failed to add input geometry unity material name attribute.");
						return false;
					}

					if (!HEU_GeneralUtility.SetAttributeArray(displayNodeID, 0, HEU_PluginSettings.UnityMaterialAttribName, ref materialIDAttrInfo, materialIDs, session.SetAttributeStringData, partInfo.faceCount))
					{
						Debug.LogError("Failed to set input geometry unity material name.");
						return false;
					}
				}
			}

			// Set mesh name attribute
			HAPI_AttributeInfo attrInfo = new HAPI_AttributeInfo();
			attrInfo.exists = true;
			attrInfo.owner = HAPI_AttributeOwner.HAPI_ATTROWNER_PRIM;
			attrInfo.storage = HAPI_StorageType.HAPI_STORAGETYPE_STRING;
			attrInfo.count = partInfo.faceCount;
			attrInfo.tupleSize = 1;
			attrInfo.originalOwner = HAPI_AttributeOwner.HAPI_ATTROWNER_INVALID;

			if (session.AddAttribute(displayNodeID, 0, HEU_PluginSettings.UnityInputMeshAttr, ref attrInfo))
			{
				string[] primitiveNameAttr = new string[partInfo.faceCount];

				for (int g = 0; g < inputDataMeshes._inputMeshes.Count; ++g)
				{
					for (int i = 0; i < inputDataMeshes._inputMeshes[g]._numSubMeshes; ++i)
					{
						int faceStart = (int)inputDataMeshes._inputMeshes[g]._indexStart[i] / numVertsPerFace;
						int faceEnd = faceStart + ((int)inputDataMeshes._inputMeshes[g]._indexCount[i] / numVertsPerFace);
						for (int m = faceStart; m < faceEnd; ++m)
						{
							primitiveNameAttr[m] = inputDataMeshes._inputMeshes[g]._meshPath;
						}
					}
				}

				if (!HEU_GeneralUtility.SetAttributeArray(displayNodeID, 0, HEU_PluginSettings.UnityInputMeshAttr, ref attrInfo, primitiveNameAttr, session.SetAttributeStringData, partInfo.faceCount))
				{
					Debug.LogError("Failed to set input geometry unity mesh name.");
					return false;
				}
			}
			else
			{
				return false;
			}

			// Set LOD group membership
			if (inputDataMeshes._hasLOD)
			{
				int[] membership = new int[partInfo.faceCount];

				for (int g = 0; g < inputDataMeshes._inputMeshes.Count; ++g)
				{
					if (g > 0)
					{
						// Clear array
						for (int m = 0; m < partInfo.faceCount; ++m)
						{
							membership[m] = 0;
						}
					}

					// Set 1 for faces belonging to this group
					for (int s = 0; s < inputDataMeshes._inputMeshes[g]._numSubMeshes; ++s)
					{
						int faceStart = (int)inputDataMeshes._inputMeshes[g]._indexStart[s] / numVertsPerFace;
						int faceEnd = faceStart + ((int)inputDataMeshes._inputMeshes[g]._indexCount[s] / numVertsPerFace);
						for (int m = faceStart; m < faceEnd; ++m)
						{
							membership[m] = 1;
						}
					}

					if (!session.AddGroup(displayNodeID, 0, HAPI_GroupType.HAPI_GROUPTYPE_PRIM, inputDataMeshes._inputMeshes[g]._meshName))
					{
						Debug.LogError("Failed to add input geometry LOD group name.");
						return false;
					}

					if (!session.SetGroupMembership(displayNodeID, 0, HAPI_GroupType.HAPI_GROUPTYPE_PRIM, inputDataMeshes._inputMeshes[g]._meshName, membership, 0, partInfo.faceCount))
					{
						Debug.LogError("Failed to set input geometry LOD group name.");
						return false;
					}
				}
			}

			return session.CommitGeo(displayNodeID);
		}

		/// <summary>
		/// Contains input geometry for multiple meshes.
		/// </summary>
		public class HEU_InputDataMeshes : HEU_InputData
		{
			public List<HEU_InputDataMesh> _inputMeshes = new List<HEU_InputDataMesh>();

			public bool _hasLOD;
		}

		/// <summary>
		/// Contains input geometry for a single mesh.
		/// </summary>
		public class HEU_InputDataMesh
		{
			public Mesh _mesh;
			public Material[] _materials;

			public string _meshPath;
			public string _meshName;

			public int _numVertices;
			public int _numSubMeshes;

			// This keeps track of indices start and length for each submesh
			public uint[] _indexStart;
			public uint[] _indexCount;

			public float _LODScreenTransition;

			public Transform _transform;
		}

		/// <summary>
		/// Return an input data structure containing mesh data that needs to be
		/// uploaded from the given inputObject.
		/// Supports child gameobjects with meshes from the given inputObject.
		/// </summary>
		/// <param name="inputObject">GameObject containing mesh components</param>
		/// <returns>A valid input data strcuture containing mesh data</returns>
		public HEU_InputDataMeshes GenerateMeshDatasFromGameObject(GameObject inputObject)
		{
			HEU_InputDataMeshes inputMeshes = new HEU_InputDataMeshes();
			inputMeshes._inputObject = inputObject;

			LODGroup lodGroup = inputObject.GetComponent<LODGroup>();
			if (lodGroup != null)
			{
				inputMeshes._hasLOD = true;

				LOD[] lods = lodGroup.GetLODs();
				for (int i = 0; i < lods.Length; ++i)
				{
					if (lods[i].renderers != null && lods[i].renderers.Length > 0)
					{
						GameObject childGO = lods[i].renderers[0].gameObject;
						HEU_InputDataMesh meshData = CreateSingleMeshData(childGO);
						if (meshData != null)
						{
							meshData._LODScreenTransition = lods[i].screenRelativeTransitionHeight;
							inputMeshes._inputMeshes.Add(meshData);
						}
					}
				}
			}
			else
			{
				inputMeshes._hasLOD = false;

				// Create a HEU_InputDataMesh for each gameobject with a MeshFilter (including children)
				MeshFilter[] meshFilters = inputObject.GetComponentsInChildren<MeshFilter>();
				foreach(MeshFilter filter in meshFilters)
				{
					HEU_InputDataMesh meshData = CreateSingleMeshData(filter.gameObject);
					if (meshData != null)
					{
						inputMeshes._inputMeshes.Add(meshData);
					}
				}
			}

			return inputMeshes;
		}

		/// <summary>
		/// Returns HEU_UploadMeshData with mesh data found on meshGameObject.
		/// </summary>
		/// <param name="meshGameObject">The GameObject to query mesh data from</param>
		/// <returns>A valid HEU_UploadMeshData if mesh data found or null</returns>
		public static HEU_InputDataMesh CreateSingleMeshData(GameObject meshGameObject)
		{
			HEU_InputDataMesh meshData = new HEU_InputDataMesh();

			MeshFilter meshfilter = meshGameObject.GetComponent<MeshFilter>();
			if (meshfilter == null)
			{
				return null;
			}

			if (meshfilter.sharedMesh == null)
			{
				return null;
			}
			meshData._mesh = meshfilter.sharedMesh;
			meshData._numVertices = meshData._mesh.vertexCount;
			meshData._numSubMeshes = meshData._mesh.subMeshCount;

			meshData._meshName = meshGameObject.name;

			// Use project path is not saved in scene, otherwise just use name
			if (HEU_GeneralUtility.IsGameObjectInProject(meshGameObject))
			{
				meshData._meshPath = HEU_AssetDatabase.GetAssetOrScenePath(meshGameObject);
				if (string.IsNullOrEmpty(meshData._meshPath))
				{
					meshData._meshPath = meshGameObject.name;
				}
			}
			else
			{
				meshData._meshPath = meshGameObject.name;
			}
			//Debug.Log("Mesh Path: " + meshData._meshPath);

			MeshRenderer meshRenderer = meshGameObject.GetComponent<MeshRenderer>();
			if (meshRenderer != null)
			{
				meshData._materials = meshRenderer.sharedMaterials;
			}

			meshData._transform = meshGameObject.transform;

			return meshData;
		}
	}

}   // HoudiniEngineUnity