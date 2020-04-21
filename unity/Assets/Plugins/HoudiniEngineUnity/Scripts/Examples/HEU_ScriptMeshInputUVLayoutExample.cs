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

using HoudiniEngineUnity;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Typedefs (copy these from HEU_Common.cs)
using HAPI_NodeId = System.Int32;

/// <summary>
/// This is an example for the Houini Engine for Unity plugin.
/// It shows how to programmetically import input meshes from the scene
/// into Houdini, connect them to a UVLayout SOP, and generate the output
/// either as copy or replace the input mesh.
/// </summary>
public class HEU_ScriptMeshInputUVLayoutExample
{
	/// <summary>
	/// Specifies how output should be generated
	/// </summary>
	public enum OutputMode
	{
		COPY,		// As a copy of original
		REPLACE		// Replace the original gameobject's data (mesh and materials)
	}

#if UNITY_EDITOR
	[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/Examples/Apply UVLayout On Copy", false, 100)]
	public static void ExampleApplyUVLayoutOnCopy()
	{
		ApplyUVLayoutTo(Selection.gameObjects, HEU_ScriptMeshInputUVLayoutExample.OutputMode.COPY, "_uvlayout_copy");
	}

	[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/Examples/Apply UVLayout Replace", false, 100)]
	public static void ExampleApplyUVLayoutReplace()
	{
		ApplyUVLayoutTo(Selection.gameObjects, HEU_ScriptMeshInputUVLayoutExample.OutputMode.REPLACE, "");
	}
#endif

	/// <summary>
	/// Applies a Houdini's UVLayout node to each given gameobject's mesh data, and generates the output.
	/// The output could be a copy gameobject, or replace the mesh and materials on the original.
	/// </summary>
	/// <param name="gameObjects">Array of gameobjects containing meshes</param>
	/// <param name="outputMode">How the outputs should be generated</param>
	/// <param name="output_name_suffix">Name to append at end of each generated gameobject if outputMode == COPY</param>
	public static void ApplyUVLayoutTo(GameObject[] gameObjects, OutputMode outputMode, string output_name_suffix)
	{
		// A Houdini Engine session is always required. This should catch any licensing and installation issues.
		HEU_SessionBase session = HEU_SessionManager.GetOrCreateDefaultSession();
		if (session == null || !session.IsSessionValid())
		{
			Debug.LogError("Failed to get Houdini Engine session. Unable to apply UV layout.");
			return;
		}

		if (gameObjects == null || gameObjects.Length == 0)
		{
			Debug.LogError("No input objects found to apply UV layout.");
			return;
		}

		// For each gameobject with mesh:
		//	-create an input node in the Houdini session
		//	-import the mesh data into the input node
		//	-connect input node to a new UVLayout node
		//	-cook the UVLayout node
		//	-generate the output mesh
		foreach (GameObject currentGO in gameObjects)
		{
			// Process the current gameobject to get the potential list of input mesh data.
			// HEU_InputUtility contains helper functions for uploading mesh data into Houdini.
			// Also handles LOD meshes.
			bool bHasLODGroup = false;

			HEU_InputInterfaceMesh inputMeshInterface = HEU_InputUtility.GetInputInterfaceByType(typeof(HEU_InputInterfaceMesh)) as HEU_InputInterfaceMesh;

			HEU_InputInterfaceMesh.HEU_InputDataMeshes inputMeshes = inputMeshInterface.GenerateMeshDatasFromGameObject(currentGO);
			if (inputMeshes == null || inputMeshes._inputMeshes.Count == 0)
			{
				Debug.LogWarningFormat("Failed to generate input mesh data for: {0}", currentGO.name);
				continue;
			}

			
			// Create the input node in Houdini.
			// Houdini Engine automatically creates a new object to contain the input node.
			string inputName = null;
			HAPI_NodeId inputNodeID = HEU_Defines.HEU_INVALID_NODE_ID;
			session.CreateInputNode(out inputNodeID, inputName);
			if (inputNodeID == HEU_Defines.HEU_INVALID_NODE_ID || !HEU_HAPIUtility.IsNodeValidInHoudini(session, inputNodeID))
			{
				Debug.LogErrorFormat("Failed to create new input node in Houdini session!");
				break;
			}

			// Need the HAPI_NodeInfo of the new input node to get its details, such as parent object ID.
			HAPI_NodeInfo nodeInfo = new HAPI_NodeInfo();
			if (!session.GetNodeInfo(inputNodeID, ref nodeInfo))
			{
				break;
			}

			// Cook the node to make sure everything is alright.
			if (!session.CookNode(inputNodeID, false))
			{
				session.DeleteNode(nodeInfo.parentId);
				Debug.LogErrorFormat("New input node failed to cook!");
				break;
			}

			// Now upload the mesh data into the input node.
			if (!inputMeshInterface.UploadData(session, inputNodeID, inputMeshes))
			{
				session.DeleteNode(nodeInfo.parentId);
				Debug.LogErrorFormat("Failed to upload input mesh data");
				break;
			}

			// Create UVLayout node in Houdini. Passing in the input node's parent node's ID will
			// create it within the same object as the input node..
			HAPI_NodeId uvlayoutID = -1;
			if (!session.CreateNode(nodeInfo.parentId, "uvlayout", "UVLayout", true, out uvlayoutID))
			{
				session.DeleteNode(nodeInfo.parentId);
				break;
			}

			// Example showing how to set the parameter on the new UVLayout node.
			session.SetParamIntValue(uvlayoutID, "correctareas", 0, 1);

			// Connect the input node to the UVLayout node.
			// Important bit here is the node IDs being passed in.
			if (!session.ConnectNodeInput(uvlayoutID, 0, inputNodeID, 0))
			{
				session.DeleteNode(nodeInfo.parentId);
				break;
			}

			// Force cook the UVLayout node in Houdini.
			if (!HEU_HAPIUtility.CookNodeInHoudini(session, uvlayoutID, true, "uvlayout"))
			{
				session.DeleteNode(nodeInfo.parentId);
				break;
			}

			// Now its time to generate the actual output in Unity. A couple of utlity classes will help here.

			// materialCache will contain the list of materials generated..
			List<HEU_MaterialData> materialCache = new List<HEU_MaterialData>();

			// Suggested name of the folder within this project where output files might be written out to (eg. materials).
			string assetCachePathName = "uvlayoutcache";

			// First create a HEU_GenerateGeoCache which will contain the geometry data from Houdiini.
			// This will get all the geometry data buffers from Houdini from the UVLayout node, along with the materials (new or existing).
			HEU_GenerateGeoCache geoCache = HEU_GenerateGeoCache.GetPopulatedGeoCache(session, inputNodeID, uvlayoutID, 0, bHasLODGroup, materialCache, assetCachePathName);
			if (geoCache == null)
			{
				session.DeleteNode(nodeInfo.parentId);
				break;
			}

			// Store reorganized data buffers into mesh groups. Groups are created if its a LOD mesh.
			List<HEU_GeoGroup> LODGroupMeshes = null;
			
			// The default material identifier (used when no material is supplied initially in Unity).
			int defaultMaterialKey = 0;

			// Flag whether to generate UVs, tangents, normals in Unity (in case they weren't created in Houdini).
			bool bGenerateUVs = false;
			bool bGenerateTangents = false;
			bool bGenerateNormals = false;
			bool bPartInstanced =false;

			// Now reorganize the data buffers into Unity mesh friendly format.
			// This handles point splitting into vertices, collider groups, submeshes based on multiple materials, LOD groups.
			// Can instead use HEU_GenerateGeoCache.GenerateGeoGroupUsingGeoCachePoints to keep as points instead.
			bool bResult = HEU_GenerateGeoCache.GenerateGeoGroupUsingGeoCacheVertices(session, geoCache, bGenerateUVs, bGenerateTangents, bGenerateNormals, bHasLODGroup, bPartInstanced,
				out LODGroupMeshes, out defaultMaterialKey);
			if (!bResult)
			{
				session.DeleteNode(nodeInfo.parentId);
				break;
			}

			// This will hold the output gameobject, along with any children and materials.
			HEU_GeneratedOutput generatedOutput = new HEU_GeneratedOutput();

			if (outputMode == OutputMode.COPY)
			{
				// For copy mode, create and set new gameobject as output
				generatedOutput._outputData._gameObject = new GameObject(currentGO.name + "_HEU_modified");
			}
			else if (outputMode == OutputMode.REPLACE)
			{
				// For replace, just use current input gameobject
				generatedOutput._outputData._gameObject = currentGO;
			}

			// Now generate the Unity meshes with material assignment. Handle LOD groups.
			int numLODs = LODGroupMeshes != null ? LODGroupMeshes.Count : 0;
			if (numLODs > 1)
			{
				bResult = HEU_GenerateGeoCache.GenerateLODMeshesFromGeoGroups(session, LODGroupMeshes, geoCache, generatedOutput, defaultMaterialKey, bGenerateUVs, bGenerateTangents, bGenerateNormals, bPartInstanced);
			}
			else if(numLODs == 1)
			{
				bResult = HEU_GenerateGeoCache.GenerateMeshFromSingleGroup(session, LODGroupMeshes[0], geoCache, generatedOutput, defaultMaterialKey, bGenerateUVs, bGenerateTangents, bGenerateNormals, bPartInstanced);
			}

			// Clean up by deleting the object node containing the input and uvlayout node.
			session.DeleteNode(nodeInfo.parentId);
		}
	}

	

}
