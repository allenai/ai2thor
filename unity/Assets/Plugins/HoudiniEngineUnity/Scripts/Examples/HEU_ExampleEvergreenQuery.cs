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

using System.Collections.Generic;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

// Specify HoudiniEngineUnity namespace to access the plugin API
using HoudiniEngineUnity;

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Typedefs (copy these from HEU_Common.cs)
using HAPI_NodeId = System.Int32;
using HAPI_PartId = System.Int32;


/// <summary>
/// This provides a couple of code examples of working with Houdini Digital Assets 
/// via the Houdini Engine for Unity API.
/// Examples provided include:
///		-loading and cooking HDA
///		-query and change parameters
///		-query the internal HDA hierarchy when loaded into a Houdini Engine session (objects, geometry, parts)
///		-query attributes
/// </summary>
public class HEU_ExampleEvergreenQuery
{
#if UNITY_EDITOR
	[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/Examples/Query Evergreen Asset", false, 100)]
	public static void QueryExampleEverygreen()
	{
		StartQuery();
	}
#endif


	// Start is called before the first frame update
	public static void StartQuery()
    {
		string evergreenAssetPath = "Assets/Plugins/HoudiniEngineUnity/HDAs/EverGreen.otl";
		string evergreenFullPath = HEU_AssetDatabase.GetAssetFullPath(evergreenAssetPath);
		if (string.IsNullOrEmpty(evergreenFullPath))
		{
			Debug.LogErrorFormat("Unable to load Evergreen asset at path: {0}", evergreenAssetPath);
			return;
		}

		// Always need a Houdini Engine session in order to use the APIs.
		// This call will create a new session if one does not exist, or continue using
		// an existing session.
		HEU_SessionBase session = HEU_SessionManager.GetOrCreateDefaultSession();

		// Load the Evergreen HDA into the Houdini Engine session, as well as the Unity scene.
		// This gives back the root gameobject of the generated HDA hiearchy in Unity.
		GameObject rootGO = HEU_HAPIUtility.InstantiateHDA(evergreenFullPath, Vector3.zero, session, true);
		if (rootGO != null)
		{
			HEU_EditorUtility.SelectObject(rootGO);
		}

		// Get reference to the Houdini script component on the asset.
		// This is the main container of the HDA's loaded data, and will be used in all
		// APIs to query and manipulate the asset.
		HEU_HoudiniAsset houdiniAsset = QueryHoudiniAsset(rootGO);
		if (houdiniAsset == null)
		{
			return;
		}

		// Make sure the HDA is cooked before querying or changing its properties.
		CookAsset(houdiniAsset);

		// Example of querying and changing parms.
		ChangeParmsAndCook(houdiniAsset);

		// This will query objects, geometry, parts, and attributes in the asset.
		QueryObjects(houdiniAsset);

		// This will query the gravity attribute.
		QueryAttribute(houdiniAsset, "EvergreenGenerator", "EvergreenGenerator1", 0, "Cd");
	}

	/// <summary>
	/// Shows how to get the HEU_HoudiniAsset component from a HDA root gameobject.
	/// </summary>
	public static HEU_HoudiniAsset QueryHoudiniAsset(GameObject rootGO)
	{
		// First get the HEU_HoudiniAssetRoot which is the script at the root gameobject
		HEU_HoudiniAssetRoot heuRoot = rootGO.GetComponent<HEU_HoudiniAssetRoot>();
		if (heuRoot == null)
		{
			Debug.LogWarningFormat("Unable to get the HEU_HoudiniAssetRoot from gameobject: {0}. Not a valid HDA.", rootGO.name);
			return null;
		}

		// The HEU_HoudiniAssetRoot should have a reference to HEU_HoudiniAsset which is the main HEU asset script.
		if (heuRoot._houdiniAsset == null)
		{
			Debug.LogWarningFormat("Unable to get the HEU_HoudiniAsset in root gameobject: {0}. Not a valid HDA.", rootGO.name);
			return null;
		}

		return heuRoot._houdiniAsset;
	}

	/// <summary>
	/// Start a cook and wait for it to finish.
	/// <param name="houdiniAsset">The HEU_HoudiniAsset of the loaded asset</param>
	/// </summary>
	public static void CookAsset(HEU_HoudiniAsset houdiniAsset)
	{
		// This starts a cook of the HDA, and waits until includes generation of output geomtry when cook has finished.
		// See function comment for the various parms.
		houdiniAsset.RequestCook(bCheckParametersChanged: true, bAsync: false, bSkipCookCheck: true, bUploadParameters: true);
	}

	/// <summary>
	/// Query the parameters in the HDA, and change some values.
	/// </summary>
	/// <param name="houdiniAsset">The HEU_HoudiniAsset of the loaded asset</param>
	public static void ChangeParmsAndCook(HEU_HoudiniAsset houdiniAsset)
	{
		// Always get the latest parms after each cook
		List<HEU_ParameterData> parms = houdiniAsset.Parameters.GetParameters();
		if (parms == null || parms.Count == 0)
		{
			Debug.LogFormat("No parms found");
			return;
		}

		// --------------------------------------------------------------------
		// Example to loop over each parm, checking its type and name. Then setting value.
		StringBuilder sb = new StringBuilder();
		foreach (HEU_ParameterData parmData in parms)
		{
			sb.AppendLine(string.Format("Parm: name={0}, type={1}", parmData._labelName, parmData._parmInfo.type));

			if (parmData._parmInfo.type == HAPI_ParmType.HAPI_PARMTYPE_BUTTON)
			{
				// Display a button: parmData._intValues[0];

			}
			else if (parmData._parmInfo.type == HAPI_ParmType.HAPI_PARMTYPE_FLOAT)
			{
				// Display a float: parmData._floatValues[0];

				// You can set a float this way
				HEU_ParameterUtility.SetFloat(houdiniAsset, parmData._name, 1f);

				// Or this way (the index is 0, unless its for array of floats)
				parmData._floatValues[0] = 1;
			}
		}
		Debug.Log("Parameters: \n" + sb.ToString());

		// --------------------------------------------------------------------
		// Examples to look up a parm via name, and set it.

		// Use helper to set float parameter with name
		HEU_ParameterUtility.SetFloat(houdiniAsset, "gravity", 5f);

		// Use helper to set random color
		HEU_ParameterUtility.SetColor(houdiniAsset, "branch_vtx_color_color", Random.ColorHSV());

		// Make sure to cook after changing parms
		CookAsset(houdiniAsset);
	}

	/// <summary>
	/// Query object nodes in the Asset. An object node represents a Houdini transform node.
	/// Each object might have any number of SOP geometry containers and a transform.
	/// <param name="houdiniAsset">The HEU_HoudiniAsset of the loaded asset</param>
	/// </summary>
	public static void QueryObjects(HEU_HoudiniAsset houdiniAsset)
	{
		// Get access to the Houdini Engine session used by this asset.
		// This gives access to call Houdini Engine APIs directly.
		HEU_SessionBase session = houdiniAsset.GetAssetSession(true);
		if (session == null || !session.IsSessionValid())
		{
			Debug.LogWarningFormat("Invalid Houdini Engine session! Try restarting session.");
			return;
		}

		HAPI_ObjectInfo[] objectInfos = null;
		HAPI_Transform[] objectTransforms = null;
		HAPI_NodeInfo assetNodeInfo = houdiniAsset.NodeInfo;

		// Fill in object infos and transforms based on node type and number of child objects.
		// This the hiearchy of the HDA when loaded in Houdini Engine. It can contain subnets with 
		// multiple objects containing multiple geometry, or a single object containting any number of geometry.
		// This automatically handles object-level HDAs and geometry (SOP) HDAs.
		if (!HEU_HAPIUtility.GetObjectInfos(session, houdiniAsset.AssetID, ref assetNodeInfo, out objectInfos, out objectTransforms))
		{
			return;
		}

		// For each object, get the display and editable geometries contained inside.
		for(int i = 0; i < objectInfos.Length; ++i)
		{
			// Get display SOP geo info
			HAPI_GeoInfo displayGeoInfo = new HAPI_GeoInfo();
			if (!session.GetDisplayGeoInfo(objectInfos[i].nodeId, ref displayGeoInfo))
			{
				return;
			}

			QueryGeoParts(session, ref displayGeoInfo);

			// Optional: Get editable nodes, cook em, then create geo nodes for them
			HAPI_NodeId[] editableNodes = null;
			HEU_SessionManager.GetComposedChildNodeList(session, objectInfos[i].nodeId, (int)HAPI_NodeType.HAPI_NODETYPE_SOP, (int)HAPI_NodeFlags.HAPI_NODEFLAGS_EDITABLE, true, out editableNodes);
			if (editableNodes != null)
			{
				foreach (HAPI_NodeId editNodeID in editableNodes)
				{
					if (editNodeID != displayGeoInfo.nodeId)
					{
						session.CookNode(editNodeID, HEU_PluginSettings.CookTemplatedGeos);

						HAPI_GeoInfo editGeoInfo = new HAPI_GeoInfo();
						if (session.GetGeoInfo(editNodeID, ref editGeoInfo))
						{
							QueryGeoParts(session, ref editGeoInfo);
						}
					}
				}
			}
		}
	}

	/// <summary>
	/// Query each geometry container's parts to get the actual geometry data.
	/// A HAPI_GeoInfo represents a SOP geometry container that might have one or more
	/// HAPI_PartInfos.A geometry containing more than one part could mean different 
	/// geometry types merged together, or different layers in a heightfield volume.
	/// </summary>
	/// <param name="session">Houdini Engine session</param>
	/// <param name="geoInfo">The HEU_GeoInfo pertaining to the geometry to query</param>
	public static void QueryGeoParts(HEU_SessionBase session, ref HAPI_GeoInfo geoInfo)
	{
		int numParts = geoInfo.partCount;
		for(int i = 0; i < numParts; ++i)
		{
			HAPI_PartInfo partInfo = new HAPI_PartInfo();
			if (!session.GetPartInfo(geoInfo.nodeId, 0, ref partInfo))
			{
				continue;
			}

			StringBuilder sb = new StringBuilder();

			// Process each geometry by its type
			if (partInfo.type == HAPI_PartType.HAPI_PARTTYPE_MESH)
			{
				// Meshes
				sb.AppendLine(string.Format("Mesh part at {0} with vertex count {1}, point count {2}, and primitive count {3}", 
					i, partInfo.vertexCount, partInfo.pointCount, partInfo.faceCount));
			}
			else if (partInfo.type == HAPI_PartType.HAPI_PARTTYPE_VOLUME)
			{
				// Heightfield / terrain
				sb.AppendLine(string.Format("Volume part at {0}", i));
			}
			else if (partInfo.type == HAPI_PartType.HAPI_PARTTYPE_CURVE)
			{
				// Curves
				sb.AppendLine(string.Format("Curve part at {0}", i));
			}
			else if (partInfo.type == HAPI_PartType.HAPI_PARTTYPE_INSTANCER)
			{
				// Instancer
				sb.AppendLine(string.Format("Instancer part at {0}", i));
			}
			else if (partInfo.type == HAPI_PartType.HAPI_PARTTYPE_INVALID)
			{
				// Not valid Houdini Engine type - ignore
				sb.AppendLine(string.Format("Invalid part at {0}", i));
			}

			// Query attributes for each part
			QueryPartAttributeByOwner(session, geoInfo.nodeId, i, HAPI_AttributeOwner.HAPI_ATTROWNER_DETAIL, partInfo.detailAttributeCount, sb);
			QueryPartAttributeByOwner(session, geoInfo.nodeId, i, HAPI_AttributeOwner.HAPI_ATTROWNER_PRIM, partInfo.primitiveAttributeCount, sb);
			QueryPartAttributeByOwner(session, geoInfo.nodeId, i, HAPI_AttributeOwner.HAPI_ATTROWNER_POINT, partInfo.pointAttributeCount, sb);
			QueryPartAttributeByOwner(session, geoInfo.nodeId, i, HAPI_AttributeOwner.HAPI_ATTROWNER_VERTEX, partInfo.vertexAttributeCount, sb);

			Debug.Log("Part: \n" + sb.ToString());
		}
	}

	/// <summary>
	/// Query all attributes of a specific part and a specific owner (detail, primitive, point, vertex).
	/// </summary>
	/// <param name="session">Houdini Engine session</param>
	/// <param name="geoID">The geometry object ID</param>
	/// <param name="partID">The part ID</param>
	/// <param name="owner">The attribute owner</param>
	/// <param name="count">The number of expected attributes for this owner</param>
	public static void QueryPartAttributeByOwner(HEU_SessionBase session, HAPI_NodeId geoID, 
		HAPI_PartId partID, HAPI_AttributeOwner owner, int count, StringBuilder sb)
	{
		if (count == 0)
		{
			Debug.LogFormat("No attributes with owner {0}", owner);
			return;
		}

		string[] attrNames = new string[count];
		if (session.GetAttributeNames(geoID, partID, owner, ref attrNames, count))
		{
			for (int i = 0; i < attrNames.Length; ++i)
			{
				HAPI_AttributeInfo attrInfo = new HAPI_AttributeInfo();
				if (HEU_GeneralUtility.GetAttributeInfo(session, geoID, partID, attrNames[i], ref attrInfo) && attrInfo.exists)
				{
					sb.AppendLine(string.Format("Attribute {0} has storage: {1}", attrNames[i], attrInfo.storage));

					// Query the actual values with helper for each type
					QueryAttributeByStorageType(session, geoID, partID, ref attrInfo, attrNames[i]);
				}
			}
		}
	}

	/// <summary>
	/// Query the attribute data by storage type.
	/// </summary>
	/// <param name="session">Houdini Engine session</param>
	/// <param name="geoID">The geometry object ID</param>
	/// <param name="partID">The part ID</param>
	/// <param name="attrInfo">A valid HAPI_AttributeInfo represendting the attribute</param>
	/// <param name="attrName">Name of the attribute</param>
	public static void QueryAttributeByStorageType(HEU_SessionBase session, HAPI_NodeId geoID, HAPI_PartId partID, 
		ref HAPI_AttributeInfo attrInfo, string attrName)
	{
		// Attribute values are usually accessed as arrays by their data type.

#pragma warning disable 0219   // Ignore unused warning

		if (attrInfo.storage == HAPI_StorageType.HAPI_STORAGETYPE_INT)
		{
			int[] data = new int[attrInfo.count];
			HEU_GeneralUtility.GetAttribute(session, geoID, partID, attrName, ref attrInfo, ref data, session.GetAttributeIntData);
		}
		else if (attrInfo.storage == HAPI_StorageType.HAPI_STORAGETYPE_FLOAT)
		{
			float[] data = new float[attrInfo.count];
			HEU_GeneralUtility.GetAttribute(session, geoID, partID, attrName, ref attrInfo, ref data, session.GetAttributeFloatData);
		}
		else if (attrInfo.storage == HAPI_StorageType.HAPI_STORAGETYPE_STRING)
		{
			string[] data = HEU_GeneralUtility.GetAttributeStringData(session, geoID, partID, attrName, ref attrInfo);
		}

#pragma warning restore 0219
	}

	/// <summary>
	/// Query a specific attribute on an asset, within its geometry.
	/// </summary>
	/// <param name="objName">The object name</param>
	/// <param name="geoName">The SOP geometry name</param>
	/// <param name="partID">The part ID</param>
	/// <param name="attrName">The attribute name</param>
	public static void QueryAttribute(HEU_HoudiniAsset houdiniAsset, string objName, string geoName, HAPI_PartId partID, string attrName)
	{
		// Get access to the Houdini Engine session used by this asset.
		// This gives access to call Houdini Engine APIs directly.
		HEU_SessionBase session = houdiniAsset.GetAssetSession(true);
		if (session == null || !session.IsSessionValid())
		{
			Debug.LogWarningFormat("Invalid Houdini Engine session! Try restarting session.");
			return;
		}

		// First get the object (transform) node, then the geometry container, then the part.
		// Finally, get the attribute on the part.

		HEU_ObjectNode objNode = houdiniAsset.GetObjectNodeByName(objName);
		if (objNode == null)
		{
			Debug.LogWarningFormat("Object with name {0} not found in asset {1}!", objName, houdiniAsset.AssetName);
			return;
		}

		HEU_GeoNode geoNode = objNode.GetGeoNode(geoName);
		if (geoNode == null)
		{
			Debug.LogWarningFormat("Geometry with name {0} not found in object {1} in asset {2}!", geoNode.GeoName, objName, houdiniAsset.AssetName);
		}

		HAPI_AttributeInfo attrInfo = new HAPI_AttributeInfo();
		if (!HEU_GeneralUtility.GetAttributeInfo(session, geoNode.GeoID, partID, attrName, ref attrInfo) && attrInfo.exists)
		{
			Debug.LogWarningFormat("Attribute {0} not found in asset.", attrName);
		}

		Debug.LogFormat("Found attribute {0} on geo {1}", attrName, geoName);

		// Now query the actual values on this attribute
		QueryAttributeByStorageType(session, geoNode.GeoID, partID, ref attrInfo, attrName);
	}
}
