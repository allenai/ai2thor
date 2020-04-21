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
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace HoudiniEngineUnity
{
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Typedefs (copy these from HEU_Common.cs)
	using HAPI_Int64 = System.Int64;
	using HAPI_StringHandle = System.Int32;
	using HAPI_ErrorCodeBits = System.Int32;
	using HAPI_AssetLibraryId = System.Int32;
	using HAPI_NodeId = System.Int32;
	using HAPI_NodeTypeBits = System.Int32;
	using HAPI_NodeFlagsBits = System.Int32;
	using HAPI_ParmId = System.Int32;
	using HAPI_PartId = System.Int32;
	using HAPI_PDG_WorkitemId = System.Int32;
	using HAPI_PDG_GraphContextId = System.Int32;


	/// <summary>
	/// Base class for a Houdini Engine session.
	/// Contains functionality to interface with the Houdini Engine for a particular session.
	/// </summary>
	public class HEU_SessionBase
	{
		// Session information
		protected HEU_SessionData _sessionData;

		// Whether user has been notified of this session is invalid (so we don't keep doing it)
		public bool UserNotifiedSessionInvalid { get; set; }

		public enum SessionConnectionState
		{
			NOT_CONNECTED,
			CONNECTED,
			FAILED_TO_CONNECT
		}
		public SessionConnectionState ConnectedState { get; set; }

		// The last error message for this session
		private string _sessionErrorMsg;

		public string GetSessionErrorMsg() { return _sessionErrorMsg; }

		// Override for logging session error
		public bool LogErrorOverride { get; set; }

		// Override for throwing session errors
		public bool ThrowErrorOverride { get; set; }

		// ASSET REGISTRATION -----------------------------------------------------------------------------------------------

		// The following asset registration mechanism keeps track of HEU_HoudiniAsset 
		// to asset ID mappings on the Unity side. This is required for 2 reasons:
		// 
		// 1. On scene load for a new Unity launch, a new Houdin session will
		// be created. The existing assets in the scene will also have to be
		// recreated in the new session, and therefore they will require new 
		// asset IDs (since IDs are session-specific). This mapping ensures
		// that when the asset is not found in the mapping, it will be guaranteed
		// to be recreated properly.
		// 
		// 2. On play mode change or script compilation, Unity destroys assets
		// and recreates them. But they still exist in the Houdini session,
		// so this mapping will help find the recreated assets, and remap
		// their IDs to the new asset references.

		/// <summary>
		/// Get the asset registered with the given ID.
		/// </summary>
		/// <param name="id">Asset ID to check</param>
		/// <returns>Asset or null if not found</returns>
		public virtual HEU_HoudiniAsset GetRegisteredAssetFromID(HAPI_NodeId id)
		{
			return null;
		}

		/// <summary>
		/// Returns true if given asset is registered (exists) with this session.
		/// </summary>
		/// <param name="asset">Asset to check</param>
		/// <returns>True if asset is registered</returns>
		public virtual bool IsAssetRegistered(HEU_HoudiniAsset asset)
		{
			return false;
		}

		/// <summary>
		/// Register this asset with this session.
		/// Should be called when the asset is created in a session.
		/// </summary>
		/// <param name="asset">Asset to register</param>
		public virtual void RegisterAsset(HEU_HoudiniAsset asset)
		{

		}

		/// <summary>
		/// Unregister the asset with given ID.
		/// Should be called when the asset is deleted from a session.
		/// </summary>
		/// <param name="id"></param>
		public virtual void UnregisterAsset(HAPI_NodeId id)
		{

		}

		/// <summary>
		/// Register this asset on its Awake. This ensures that either the asset
		/// is registered with the session it was created in, or if that session doesn't
		/// exist, then the asset is invalidated.
		/// </summary>
		/// <param name="asset">Asset to registered</param>
		public virtual void ReregisterOnAwake(HEU_HoudiniAsset asset)
		{

		}

		// SESSION ----------------------------------------------------------------------------------------------------

		public HEU_SessionBase()
		{
			// Have to initialize here as C# 6+ supports property initializer
			LogErrorOverride = true;
			ThrowErrorOverride = true;
		}

		/// <summary>
		/// Set the session error message
		/// </summary>
		/// <param name="msg">String message to set</param>
		/// <param name="bLogError">Set to true if want to log error on console</param>
		public virtual void SetSessionErrorMsg(string msg, bool bLogError = false)
		{
			_sessionErrorMsg = msg;
			if (bLogError && LogErrorOverride)
			{
				Debug.LogError(_sessionErrorMsg);
			}
		}

		/// <summary>
		/// Create new session data if specified.
		/// </summary>
		/// <param name="bOverwriteExisting">True if overwrite existing session data. Note it does not close existing.</param>
		/// <returns>True if created new session. False if session already exists.</returns>
		protected virtual bool CreateSessionData(bool bOverwriteExisting, bool bIsDefaultSession)
		{
			return false;
		}

		/// <summary>
		/// Create in-process Houdini Engine session.
		/// </summary>
		/// <returns>True if session creation succeeded.</returns>
		public virtual bool CreateInProcessSession(bool bIsDefaultSession)
		{
			return false;
		}

		public virtual bool CreateThriftSocketSession(bool bIsDefaultSession, string hostName = HEU_Defines.HEU_SESSION_LOCALHOST, int serverPort = HEU_Defines.HEU_SESSION_PORT, bool autoClose = HEU_Defines.HEU_SESSION_AUTOCLOSE, float timeout = HEU_Defines.HEU_SESSION_TIMEOUT, bool bLogError = true)
		{
			return false;
		}

		public virtual bool CreateThriftPipeSession(bool bIsDefaultSession, string pipeName = HEU_Defines.HEU_SESSION_PIPENAME, bool autoClose = HEU_Defines.HEU_SESSION_AUTOCLOSE, float timeout = HEU_Defines.HEU_SESSION_TIMEOUT, bool bLogError = true)
		{
			return false;
		}

		public virtual bool CreateCustomSession(bool bIsDefaultSession)
		{
			return false;
		}

		public virtual bool ConnectThriftSocketSession(bool bIsDefaultSession, string hostName = HEU_Defines.HEU_SESSION_LOCALHOST, int serverPort = HEU_Defines.HEU_SESSION_PORT, bool autoClose = HEU_Defines.HEU_SESSION_AUTOCLOSE, float timeout = HEU_Defines.HEU_SESSION_TIMEOUT)
		{
			return false;
		}

		public virtual bool ConnectThriftPipeSession(bool bIsDefaultSession, string pipeName = HEU_Defines.HEU_SESSION_PIPENAME, bool autoClose = HEU_Defines.HEU_SESSION_AUTOCLOSE, float timeout = HEU_Defines.HEU_SESSION_TIMEOUT)
		{
			return false;
		}

		/// <summary>
		/// Close the existing session.
		/// </summary>
		/// <returns>True if successfully closed session.</returns>
		public virtual bool CloseSession()
		{
			return false;
		}

		/// <summary>
		/// Closes session if one exists.
		/// </summary>
		/// <returns>Only returns false if closing existing session failed. Otherwise returns true.</returns>
		protected virtual bool CheckAndCloseExistingSession()
		{
			return false;
		}

		/// <summary>
		/// Clears the session info.
		/// </summary>
		protected virtual void ClearSessionInfo()
		{
			if (_sessionData != null)
			{
				_sessionData.SessionID = -1;
				_sessionData.ProcessID = -1;
				_sessionData = null;
			}
		}

		/// <summary>
		/// Set the session data for this session.
		/// </summary>
		/// <param name="sessionData">Session data to set</param>
		public void SetSessionData(HEU_SessionData sessionData)
		{
			_sessionData = sessionData;
		}

		/// <summary>
		/// Return the existing session data.
		/// </summary>
		/// <returns></returns>
		public HEU_SessionData GetSessionData()
		{
			return _sessionData;
		}

		/// <summary>
		/// Return the session info.
		/// </summary>
		/// <returns>The session information as a formatted string.</returns>
		public virtual string GetSessionInfo()
		{
			return HEU_Defines.NO_EXISTING_SESSION;
		}

		/// <summary>
		/// Checks that the Houdini Engine session is valid.
		/// </summary>
		/// <returns>True if this session is valid.</returns>
		public virtual bool IsSessionValid()
		{
			return false;
		}

		/// <summary>
		/// Close current (if valid) and open a new session.
		/// </summary>
		/// <returns>True if created a new session.</returns>
		public virtual bool RestartSession()
		{
			return false;
		}

		/// <summary>
		/// Returns last session error.
		/// </summary>
		/// <returns>The last session error.</returns>
		public string GetLastSessionError()
		{
			return _sessionErrorMsg;
		}

		public virtual bool CheckVersionMatch()
		{
			return false;
		}

		public virtual bool HandleStatusResult(HAPI_Result result, string prependMsg, bool bThrowError, bool bLogError)
		{
			return false;
		}

		// ENVIRONMENT ------------------------------------------------------------------------------------------------

		/// <summary>
		/// Set environment variable for the server process as a string.
		/// </summary>
		/// <param name="name">Name of variable.</param>
		/// <param name="value">String value.</param>
		public virtual void SetServerEnvString(string name, string value)
		{

		}

		public virtual bool GetServerEnvString(string name, out string value)
		{
			value = null;
			return false;
		}

		public virtual bool GetServerEnvVarCount(out int env_count)
		{
			env_count = 0;
			return false;
		}

		/// <summary>
		/// Gives back the status code for a specific status type
		/// </summary>
		/// <param name="statusType">Status type to query</param>
		/// <param name="statusCode">Result status code</param>
		/// <returns>True if successfully queried status</returns>
		public virtual bool GetStatus(HAPI_StatusType statusType, out HAPI_State statusCode)
		{
			statusCode = HAPI_State.HAPI_STATE_READY;
			return false;
		}

		/// <summary>
		/// Return the status string from HAPI session.
		/// </summary>
		/// <param name="statusType"></param>
		/// <param name="verbosity"></param>
		/// <returns></returns>
		public virtual string GetStatusString(HAPI_StatusType statusType, HAPI_StatusVerbosity verbosity)
		{
			return "Unsupported plugin configuration.";
		}

		/// <summary>
		/// Returns environment value in Houdini Engine.
		/// </summary>
		/// <param name="intType">Type of environment variable</param>
		/// <returns>Value of environment variable</returns>
		public virtual int GetEnvInt(HAPI_EnvIntType intType)
		{
			return 0;
		}

		/// <summary>
		/// Return the session environment variable.
		/// </summary>
		/// <param name="intType">Type of environment variable.</param>
		/// <returns>Value of environment variable.</returns>
		public virtual int GetSessionEnvInt(HAPI_SessionEnvIntType intType, bool bLogError)
		{
			return 0;
		}

		/// <summary>
		/// Get the string value for the associated string handle.
		/// </summary>
		/// <param name="stringHandle">Handle to look up.</param>
		/// <param name="resultString">Container for return value.</param>
		/// <param name="bufferLength">Length of return value</param>
		/// <returns>True if it has successfully populated the string value.</returns>
		public virtual bool GetString(HAPI_StringHandle stringHandle, ref string resultString, int bufferLength)
		{
			return false;
		}

		/// <summary>
		/// Returns the length of the string value for the given handle.
		/// </summary>
		/// <param name="stringHandle">Handle of the string to query</param>
		/// <returns>Buffer of the length of the queried string.</returns>
		public virtual int GetStringBufferLength(HAPI_StringHandle stringHandle)
		{
			return 0;
		}

		/// <summary>
		/// Checks for and returns specific errors on node.
		/// </summary>
		/// <param name="nodeID">Node to check</param>
		/// <param name="errorsToCheck">Specific errors to check for</param>
		/// <returns>Errors found on node</returns>
		public virtual HAPI_ErrorCodeBits CheckForSpecificErrors(HAPI_NodeId nodeID, HAPI_ErrorCodeBits errorsToCheck)
		{
			return 0;
		}

		// ASSETS -----------------------------------------------------------------------------------------------------

		/// <summary>
		/// Load given asset file in Houdini Engine.
		/// </summary>
		/// <param name="assetPath">Path to the asset</param>
		/// <param name="bAllowOverwrite">Whether to overwrite an existing matching asset definition</param>
		/// <param name="libraryID">ID of the asset in the library</param>
		/// <returns>True if successfully loaded the asset.</returns>
		public virtual bool LoadAssetLibraryFromFile(string assetPath, bool bAllowOverwrite, out HAPI_StringHandle libraryID)
		{
			libraryID = 0;
			return false;
		}

		/// <summary>
		/// Creates a node inside a node network.
		/// </summary>
		/// <param name="parentNodeID">Parent node network's node ID or -1 if at top level</param>
		/// /// <param name="operatorName">Name of the node operator type</param>
		/// <param name="nodeLabel">Label for newly created node</param>
		/// <param name="bCookOnCreation">Whether should cook on creation or not</param>
		/// <param name="newNodeID">New node's ID</param>
		/// <returns>True if successfully created a new node</returns>
		public virtual bool CreateNode(HAPI_StringHandle parentNodeID, string operatorName, string nodeLabel, bool bCookOnCreation, out HAPI_NodeId newNodeID)
		{
			newNodeID = -1;
			return false;
		}

		/// <summary>
		/// Delete specified Houdini Engine node.
		/// </summary>
		/// <param name="nodeID">Node to delete</param>
		public virtual void DeleteNode(HAPI_NodeId nodeID)
		{

		}

		/// <summary>
		/// Cook the given node. This may trigger cooks on other nodes if connected.
		/// </summary>
		/// <param name="nodeID">ID of the node to cook</param>
		/// <param name="bCookTemplatedGeos">Whether to recursively cook all templated geos or not</param>
		/// <param name="bSplitGeosByGroup">Whether to split the geometry by groups. Not recommended to use, but allowing in specific situations.</param>
		/// <returns>True if successfully cooked the node</returns>
		public virtual bool CookNode(HAPI_NodeId nodeID, bool bCookTemplatedGeos, bool bSplitGeosByGroup = false)
		{
			return false;
		}

		/// <summary>
		/// Rename an existing node.
		/// </summary>
		/// <param name="nodeID">ID of the node to rename</param>
		/// <param name="newName">New name</param>
		/// <returns>True if successful</returns>
		public virtual bool RenameNode(HAPI_NodeId nodeID, string newName)
		{
			return false;
		}

		/// <summary>
		/// Connect two nodes together
		/// </summary>
		/// <param name="nodeID">Node whom's input to connect to</param>
		/// <param name="inputIndex">The input index should be between 0 and nodeIDToConnect's input count</param>
		/// <param name="nodeIDToConnect">The ndoe to connect to nodeID's input</param>
		/// <param name="outputIndex">The output index should be between 0 and nodeIDToConnect's output count</param>
		/// <returns></returns>
		public virtual bool ConnectNodeInput(HAPI_NodeId nodeID, int inputIndex, HAPI_NodeId nodeIDToConnect, int outputIndex = 0)
		{
			return false;
		}

		/// <summary>
		/// Disconnect a node input
		/// </summary>
		/// <param name="nodeID">The node whom's input to disconnect</param>
		/// <param name="inputIndex">The input index should be between 0 and the node's input count</param>
		/// <param name="bLogError">Whether to log error</param>
		/// <returns>True if successful</returns>
		public virtual bool DisconnectNodeInput(HAPI_NodeId nodeID, int inputIndex, bool bLogError)
		{
			return false;
		}

		/// <summary>
		/// Query which node is connected to another node's input.
		/// </summary>
		/// <param name="nodeID">The node to query</param>
		/// <param name="inputIndex">The input index should be between 0 and the node's input count</param>
		/// <param name="connectedNodeID">The node ID of the connected node to this input. -1 if no connection.</param>
		/// <param name="bLogError">True if error should be logged</param>
		/// <returns>True if successfully queried the node.</returns>
		public virtual bool QueryNodeInput(HAPI_NodeId nodeID, int inputIndex, out HAPI_NodeId connectedNodeID, bool bLogError)
		{
			connectedNodeID = HEU_Defines.HEU_INVALID_NODE_ID;
			return false;
		}

		/// <summary>
		/// Get the name of the given node's input. This will return a string handle for the name
		/// which will persisst until the next call to this function.
		/// </summary>
		/// <param name="nodeID">Node's ID</param>
		/// <param name="inputIndex">Index of the input</param>
		/// <param name="nodeNameIndex">Input name string handle</param>
		/// <returns>True if successfully queried the node</returns>
		public virtual bool GetNodeInputName(HAPI_NodeId nodeID, int inputIndex, out HAPI_StringHandle nodeNameIndex)
		{
			nodeNameIndex = 0;
			return false;
		}

		/// <summary>
		/// Returns the number of assets contained in an asset library.
		/// Requires LoadAssetLibraryFromFile to be invokved before calling this.
		/// </summary>
		/// <param name="libraryID">ID of the asset in lbirary</param>
		/// <param name="assetCount">Number of assets contained in this asset library</param>
		/// <returns>True if successfully queried the asset count</returns>
		public virtual bool GetAvailableAssetCount(HAPI_AssetLibraryId libraryID, out int assetCount)
		{
			assetCount = 0;
			return false;
		}

		/// <summary>
		/// Returns the names of the assets contained in given asset library.
		/// </summary>
		/// <param name="libraryID">ID of the asset in the library</param>
		/// <param name="assetNames">Array to fill with names. Assumes array is initialized at least size of assetCount</param>
		/// <param name="assetCount">Should be same or less than returned by GetAvailableAssetCount</param>
		/// <returns>True if query was successful</returns>
		public virtual bool GetAvailableAssets(HAPI_AssetLibraryId libraryID, ref HAPI_StringHandle[] assetNames, int assetCount)
		{
			return false;
		}

		/// <summary>
		/// Returns the asset info for the given node
		/// </summary>
		/// <param name="nodeID">The node to retrieve the asset info for</param>
		/// <param name="assetInfo">The asset info structure to populate</param>
		/// <returns>True if successfully queried the asset info</returns>
		public virtual bool GetAssetInfo(HAPI_NodeId nodeID, ref HAPI_AssetInfo assetInfo)
		{
			return false;
		}

		/// <summary>
		/// Returns the node info for the given node.
		/// </summary>
		/// <param name="nodeID">The node to retrieve the node info for</param>
		/// <param name="nodeInfo">The node info structure to populate</param>
		/// <param name="bLogError">True to log any error</param>
		/// <returns>True if successfully queried the node info</returns>
		public virtual bool GetNodeInfo(HAPI_NodeId nodeID, ref HAPI_NodeInfo nodeInfo, bool bLogError = true)
		{
			return false;
		}

		/// <summary>
		/// Get the node absolute path or relative path in the Houdini node network.
		/// </summary>
		/// <param name="nodeID">The ID of the node to query</param>
		/// <param name="relativeNodeID">The relative node. Set to -1 to get absolute.</param>
		/// <param name="path">The returned path string</param>
		/// <returns>True if successfully queried the node path</returns>
		public virtual bool GetNodePath(HAPI_NodeId nodeID, HAPI_NodeId relativeNodeID, out string path)
		{
			path = null;
			return false;
		}

		/// <summary>
		/// Returns true if this node exists in the Houdini session.
		/// Allows host application to check if needed to repopulate in Houdini.
		/// </summary>
		/// <param name="nodeID">The node ID</param>
		/// <param name="uniqueNodeID">The unique node ID</param>
		/// <returns>True if the node exists</returns>
		public virtual bool IsNodeValid(HAPI_NodeId nodeID, int uniqueNodeID)
		{
			return false;
		}

		/// <summary>
		/// Compose a list of child nodes based on given filters.
		/// </summary>
		/// <param name="parentNodeID">Parent node ID</param>
		/// <param name="nodeTypeFilter">Node type by which to filter the children</param>
		/// <param name="nodeFlagFilter">Node flags by which to filter the children</param>
		/// <param name="bRecursive">Whether or not to compose the list recursively</param>
		/// <param name="count">Number of child nodes composed</param>
		/// <returns>True if successfully composed the child node list</returns>
		public virtual bool ComposeChildNodeList(HAPI_NodeId parentNodeID, HAPI_NodeTypeBits nodeTypeFilter, HAPI_NodeFlagsBits nodeFlagFilter, bool bRecursive, ref int count)
		{
			return false;
		}

		/// <summary>
		/// Get the composed list of child node IDs after calling ComposeChildNodeList.
		/// </summary>
		/// <param name="parentNodeID">Parent node ID</param>
		/// <param name="childNodeIDs">Array to store the child node IDs. If null, will create array of size count. If non-null, size must at least be count.</param>
		/// <param name="count">Number of children in the composed list. Must match the count returned by ComposeChildNodeList</param>
		/// <returns>True if successfully retrieved the child node list</returns>
		public virtual bool GetComposedChildNodeList(HAPI_NodeId parentNodeID, HAPI_NodeId[] childNodeIDs, int count)
		{
			return false;
		}

		// HIP FILES --------------------------------------------------------------------------------------------------

		/// <summary>
		/// Load a HIP file into current (or new if none existing) Houdini session.
		/// </summary>
		/// <param name="fileName">HIP file path to load</param>
		/// <param name="bCookOnLoad">True if want to cook on loading instead of manually cook each node</param>
		/// <returns>True if successfull</returns>
		public virtual bool LoadHIPFile(string fileName, bool bCookOnLoad)
		{
			return false;
		}

		/// <summary>
		/// Save current Houdini session into a HIP file.
		/// </summary>
		/// <param name="fileName">HIP file path to save to</param>
		/// <param name="bLockNodes">True if all SOP nodes should be locked to maintain state, instead of relying on the re-cook</param>
		/// <returns>True if successfull</returns>
		public virtual bool SaveHIPFile(string fileName, bool bLockNodes)
		{
			return false;
		}

		// OBJECTS ----------------------------------------------------------------------------------------------------

		/// <summary>
		/// Get the object info on an OBJ node.
		/// </summary>
		/// <param name="nodeID">The node ID</param>
		/// <param name="objectInfo">Object info to populate</param>
		/// <returns>True if successfully queried object info</returns>
		public virtual bool GetObjectInfo(HAPI_NodeId nodeID, ref HAPI_ObjectInfo objectInfo)
		{
			return false;
		}

		// <summary>
		/// Get the transform of an OBJ node.
		/// </summary>
		/// <param name="nodeID">The object node ID</param>
		/// <param name="relativeToNodeID">The object node ID of the object to which the returned transform will be relative to. -1 if want object's local transform</param>
		/// <param name="rstOrder">The transform order</param>
		/// <param name="hapiTransform">Transform info to populate</param>
		/// <returns>True if successfully queried transform info</returns>
		public virtual bool GetObjectTransform(HAPI_NodeId nodeID, HAPI_NodeId relativeToNodeID, HAPI_RSTOrder rstOrder, ref HAPI_Transform hapiTransform)
		{
			return false;
		}

		/// <summary>
		/// Set the transform of an OBJ node.
		/// </summary>
		/// <param name="nodeID">The object node ID</param>
		/// <param name="hapiTransform">The transform to set</param>
		/// <returns>True if successfully set the transform</returns>
		public virtual bool SetObjectTransform(HAPI_NodeId nodeID, ref HAPI_TransformEuler hapiTransform)
		{
			return false;
		}

		/// <summary>
		/// Compose a list of child object nodes given a parent node ID.
		/// </summary>
		/// <param name="nodeID">The parent node ID</param>
		/// <param name="objectCount">The number of object nodes currently under the parent</param>
		/// <returns>True if successfully composed the list</returns>
		public virtual bool ComposeObjectList(HAPI_NodeId nodeID, out int objectCount)
		{
			objectCount = 0;
			return false;
		}

		/// <summary>
		/// Fill an array of HAPI_ObjectInfo list.
		/// </summary>
		/// <param name="nodeID">The parent node ID</param>
		/// <param name="objectInfos">Array to fill. Should atleast be size of length</param>
		/// <param name="start">At least 0 and at most object count returned by ComposeObjectList</param>
		/// <param name="length">Object count returned by ComposeObjectList. Should be at least 0 and at most object count - start</param>
		/// <returns>True if successfully queuried the object list</returns>
		public virtual bool GetComposedObjectList(HAPI_NodeId nodeID, [Out] HAPI_ObjectInfo[] objectInfos, int start, int length)
		{
			return false;
		}

		/// <summary>
		/// Fill in array of HAPI_Transform list.
		/// </summary>
		/// <param name="nodeID">The parent node ID</param>
		/// <param name="rstOrder">Transform order</param>
		/// <param name="transforms">Array to fill. Should at least be size of length</param>
		/// <param name="start">At least 0 and at most object count returned by ComposeObjectList</param>
		/// <param name="length">Object count returned by ComposeObjectList. Should be at least 0 and at most object count - start</param>
		/// <returns>True if successfully queuried the transform list</returns>
		public virtual bool GetComposedObjectTransforms(HAPI_NodeId nodeID, HAPI_RSTOrder rstOrder, [Out] HAPI_Transform[] transforms, int start, int length)
		{
			return false;
		}


		// GEOMETRY GETTERS -------------------------------------------------------------------------------------------

		/// <summary>
		/// Get the display geo (SOP) node inside an Object node. If there are multiple display SOP nodes, only
		/// the first one is returned.
		/// </summary>
		/// <param name="nodeID">Object node ID</param>
		/// <param name="geoInfo">Geo info to populate</param>
		/// <returns>True if successfully queried the geo info</returns>
		public virtual bool GetDisplayGeoInfo(HAPI_NodeId nodeID, ref HAPI_GeoInfo geoInfo, bool bLogError = false)
		{
			return false;
		}

		/// <summary>
		/// Get the geometry info on a SOP node.
		/// </summary>
		/// <param name="nodeID">The SOP node ID</param>
		/// <param name="geoInfo">Geo info to populate</param>
		/// <returns>True if successfully queried the geo info</returns>
		public virtual bool GetGeoInfo(HAPI_NodeId nodeID, ref HAPI_GeoInfo geoInfo)
		{
			return false;
		}

		/// <summary>
		/// Get the part info on a SOP node.
		/// </summary>
		/// <param name="nodeID">The SOP node ID</param>
		/// <param name="partID">The part ID</param>
		/// <param name="geoInfo">Part info to populate</param>
		/// <returns>True if successfully queried the part info</returns>
		public virtual bool GetPartInfo(HAPI_NodeId nodeID, HAPI_PartId partID, ref HAPI_PartInfo partInfo)
		{
			return false;
		}

		/// <summary>
		/// Get the main geometry info struct.
		/// </summary>
		/// <param name="nodeID">The SOP node ID</param>
		/// <param name="partID">The part ID</param>
		/// <param name="name">Attribute name</param>
		/// <param name="owner">Attribute owner</param>
		/// <param name="attributeInfo">Info to populate</param>
		/// <returns>True if successfully queried the attribute info</returns>
		public virtual bool GetAttributeInfo(HAPI_NodeId nodeID, HAPI_PartId partID, string name, HAPI_AttributeOwner owner, ref HAPI_AttributeInfo attributeInfo)
		{
			return false;
		}

		/// <summary>
		/// Get the attribute names of all attributes having owner of the given part.
		/// </summary>
		/// <param name="nodeID">The SOP node ID</param>
		/// <param name="partID">The part ID</param>
		/// <param name="owner">Attributes must have this owner type</param>
		/// <param name="attributeNames">Result array of name strings. Must be atleast count size.</param>
		/// <param name="count">Expected number of attributes. Should be from HAPI_PartInfo.attributeCounts[owner].</param>
		/// <returns>True if successfully retrieved the names</returns>
		public virtual bool GetAttributeNames(HAPI_NodeId nodeID, HAPI_PartId partID, HAPI_AttributeOwner owner, ref string[] attributeNames, int count)
		{
			return false;
		}

		/// <summary>
		/// Get the attribute string data.
		/// </summary>
		/// <param name="nodeID">The node ID</param>
		/// <param name="partID">The part ID</param>
		/// <param name="name">Attribute name</param>
		/// <param name="attributeInfo">Atttribute info</param>
		/// <param name="dataArray">Array to populate. Must be at least the size of length * HAPI_AttributeInfo::tupleSize</param>
		/// <param name="start">First index of range. Must be at least 0 and at most HAPI_AttributeInfo::count - 1</param>
		/// <param name="length">Must be at least 0 and at most HAPI_AttributeInfo::count - start</param>
		/// <returns>True if successfully queried the atttribute string data</returns>
		public virtual bool GetAttributeStringData(HAPI_NodeId nodeID, HAPI_PartId partID, string name, ref HAPI_AttributeInfo attributeInfo, [Out] HAPI_StringHandle[] dataArray, int start, int length)
		{
			return false;
		}

		/// <summary>
		/// Get the attribute float data.
		/// </summary>
		/// <param name="nodeID">The node ID</param>
		/// <param name="partID">The part ID</param>
		/// <param name="name">Attribut ename</param>
		/// <param name="attributeInfo">Should be same struct returned by HAPI_GetAttributeInfo</param>
		/// <param name="data">A float array at least the size of length * HAPI_AttributeInfo::tupleSize</param>
		/// <param name="start">First index of range. Must be at least 0 and at most HAPI_AttributeInfo::count - 1</param>
		/// <param name="length">Must be at least 0 and at most HAPI_AttributeInfo::count - start.</param>
		/// <returns>True if successfully queried the atttribute float data</returns>
		public virtual bool GetAttributeFloatData(HAPI_NodeId nodeID, HAPI_PartId partID, string name, ref HAPI_AttributeInfo attributeInfo, [Out] float[] data, int start, int length)
		{
			return false;
		}

		public virtual bool GetAttributeFloat64Data(HAPI_NodeId nodeID, HAPI_PartId partID, string name, ref HAPI_AttributeInfo attributeInfo, [Out] double[] data, int start, int length)
		{
			return false;
		}

		/// <summary>
		/// Get the attribute int data.
		/// </summary>
		/// <param name="nodeID">The node ID</param>
		/// <param name="partID">The part ID</param>
		/// <param name="name">Attribut ename</param>
		/// <param name="attributeInfo">Should be same struct returned by HAPI_GetAttributeInfo</param>
		/// <param name="data">An int array at least the size of length * HAPI_AttributeInfo::tupleSize</param>
		/// <param name="start">First index of range. Must be at least 0 and at most HAPI_AttributeInfo::count - 1</param>
		/// <param name="length">Must be at least 0 and at most HAPI_AttributeInfo::count - start.</param>
		/// <returns>True if successfully queried the atttribute int data</returns>
		public virtual bool GetAttributeIntData(HAPI_NodeId nodeID, HAPI_PartId partID, string name, ref HAPI_AttributeInfo attributeInfo, [Out] int[] data, int start, int length)
		{
			return false;
		}

		public virtual bool GetAttributeInt64Data(HAPI_NodeId nodeID, HAPI_PartId partID, string name, ref HAPI_AttributeInfo attributeInfo, [Out] HAPI_Int64[] data, int start, int length)
		{
			return false;
		}

		/// <summary>
		/// Gets the group names for given group type.
		/// </summary>
		/// <param name="nodeID">The node ID</param>
		/// <param name="groupType">The group type</param>
		/// <param name="names">Array to populate. Must at least be of size count</param>
		/// <param name="count">Should be less than or equal to size of names</param>
		/// <returns>True if successfully queried the group names</returns>
		public virtual bool GetGroupNames(HAPI_NodeId nodeID, HAPI_GroupType groupType, ref HAPI_StringHandle[] names, int count)
		{
			return false;
		}

		public virtual bool GetGroupMembership(HAPI_NodeId nodeID, HAPI_PartId partID, HAPI_GroupType groupType, string groupName, ref bool membershipArrayAllEqual, [Out] int[] membershipArray, int start, int length)
		{
			return false;
		}

		public virtual bool GetGroupCountOnPackedInstancePart(HAPI_NodeId nodeID, HAPI_PartId partID, out int pointGroupCount, out int primitiveGroupCount)
		{
			pointGroupCount = 0;
			primitiveGroupCount = 0;
			return false;
		}

		public virtual bool GetGroupNamesOnPackedInstancePart(HAPI_NodeId nodeID, HAPI_PartId partID, HAPI_GroupType groupType, ref HAPI_StringHandle[] groupNamesArray, int groupCount)
		{
			return false;
		}

		public virtual bool GetGroupMembershipOnPackedInstancePart(HAPI_NodeId nodeID, HAPI_PartId partID, HAPI_GroupType groupType, string groupName, ref bool membershipArrayAllEqual, [Out] int[] membershipArray, int start, int length)
		{
			return false;
		}

		public virtual bool GetInstancedPartIds(HAPI_NodeId nodeID, HAPI_PartId partID, [Out] HAPI_PartId[] instancedPartsArray, int start, int length)
		{
			return false;
		}

		public virtual bool GetInstancerPartTransforms(HAPI_NodeId nodeID, HAPI_PartId partID, HAPI_RSTOrder rstOrder, [Out] HAPI_Transform[] transformsArray, int start, int length)
		{
			return false;
		}

		public virtual bool GetInstanceTransformsOnPart(HAPI_NodeId nodeID, HAPI_PartId partID, HAPI_RSTOrder rstOrder, [Out] HAPI_Transform[] transformsArray, int start, int length)
		{
			return false;
		}

		public virtual bool GetInstancedObjectIds(HAPI_NodeId nodeID, [Out] HAPI_NodeId[] instanced_node_id_array, int start, int length)
		{
			return false;
		}

		/// <summary>
		/// Get the array of faces where the nth integer in the array is the number of vertices
		/// the nth face has.
		/// </summary>
		/// <param name="nodeID">The node ID</param>
		/// <param name="partID">The part ID</param>
		/// <param name="faceCounts">An integer array at least the size of length</param>
		/// <param name="start">First index of range. Must be at least 0 and at most HAPI_PartInfo::faceCount - 1</param>
		/// <param name="length">Must be at least 0 and at most HAPI_PartInfo::faceCount - start</param>
		/// <returns>True if successfully queried the face counts</returns>
		public virtual bool GetFaceCounts(HAPI_NodeId nodeID, HAPI_PartId partID, [Out] int[] faceCounts, int start, int length)
		{
			return false;
		}

		/// <summary>
		/// Get the array containing the vertex-point associations where the ith element
		/// in the array is the point index that the ith vertex associates with.
		/// </summary>
		/// <param name="nodeID">The node ID</param>
		/// <param name="partID">The part ID</param>
		/// <param name="vertexList">An integer array at least the size of length</param>
		/// <param name="start">First index of range. Must be at least 0 and at most HAPI_PartInfo::vertexCount - 1</param>
		/// <param name="length">Must be at least 0 and at most HAPI_PartInfo::vertexCount - start</param>
		/// <returns>True if successfully queried the vertex list</returns>
		public virtual bool GetVertexList(HAPI_NodeId nodeID, HAPI_PartId partID, [Out] int[] vertexList, int start, int length)
		{
			return false;
		}

		/// <summary>
		/// Get the box info on a geo part.
		/// </summary>
		/// <param name="nodeID">The geo node ID</param>
		/// <param name="partID">The part ID</param>
		/// <param name="boxInfo">The info to fill</param>
		/// <returns>True if successfully queried the box info</returns>
		public virtual bool GetBoxInfo(HAPI_NodeId nodeID, HAPI_PartId partID, ref HAPI_BoxInfo boxInfo)
		{
			return false;
		}

		/// <summary>
		/// Get the sphere info on a geo part.
		/// </summary>
		/// <param name="nodeID">The geo node ID</param>
		/// <param name="partID">The part ID</param>
		/// <param name="sphereInfo">The info to fill</param>
		/// <returns>True if successfully queried the sphere info</returns>
		public virtual bool GetSphereInfo(HAPI_NodeId nodeID, HAPI_PartId partID, ref HAPI_SphereInfo sphereInfo)
		{
			return false;
		}

		public virtual bool GetCurveInfo(HAPI_NodeId nodeID, HAPI_PartId partID, ref HAPI_CurveInfo curveInfo)
		{
			return false;
		}

		public virtual bool GetCurveCounts(HAPI_NodeId nodeID, HAPI_PartId partID, [Out] int[] counts, int start, int length)
		{
			return false;
		}

		// GEOMETRY SETTERS -------------------------------------------------------------------------------------------

		public virtual bool SetPartInfo(HAPI_NodeId nodeID, HAPI_PartId partID, ref HAPI_PartInfo partInfo)
		{
			return false;
		}

		public virtual bool SetFaceCount(HAPI_NodeId nodeID, HAPI_PartId partID, int[] faceCounts, int start, int length)
		{
			return false;
		}

		public virtual bool SetVertexList(HAPI_NodeId nodeID, HAPI_PartId partID, int[] vertexList, int start, int length)
		{
			return false;
		}

		public virtual bool SetAttributeIntData(HAPI_NodeId nodeID, HAPI_PartId partID, string name, ref HAPI_AttributeInfo attrInfo,
			int[] data, int start, int length)
		{
			return false;
		}

		public virtual bool SetAttributeFloatData(HAPI_NodeId nodeID, HAPI_PartId partID, string name, ref HAPI_AttributeInfo attrInfo,
			float[] data, int start, int length)
		{
			return false;
		}

		public virtual bool SetAttributeStringData(HAPI_NodeId nodeID, HAPI_PartId partID, string name, ref HAPI_AttributeInfo attrInfo,
			string[] data, int start, int length)
		{
			return false;
		}

		public virtual bool AddAttribute(HAPI_NodeId nodeID, HAPI_PartId partID, string name, ref HAPI_AttributeInfo attrInfo)
		{
			return false;
		}

		public virtual bool AddGroup(HAPI_NodeId nodeID, HAPI_PartId partID, HAPI_GroupType groupType, string groupName)
		{
			return false;
		}

		public virtual bool DeleteGroup(HAPI_NodeId nodeID, HAPI_PartId partID, HAPI_GroupType groupType, string groupName)
		{
			return false;
		}

		public virtual bool SetGroupMembership(HAPI_NodeId nodeID, HAPI_PartId partID, HAPI_GroupType groupType, string groupName, [Out] int[] membershipArray, int start, int length)
		{
			return false;
		}

		public virtual bool CommitGeo(HAPI_NodeId nodeID)
		{
			return false;
		}

		public virtual bool RevertGeo(HAPI_NodeId nodeID)
		{
			return false;
		}

		// MATERIALS --------------------------------------------------------------------------------------------------

		/// <summary>
		/// Get the Material on the specified Part
		/// </summary>
		/// <param name="nodeID">The node ID</param>
		/// <param name="partID">The part ID</param>
		/// <param name="materialInfo">A valid material info to populate</param>
		/// <returns>True if successfully queried the material info</returns>
		public virtual bool GetMaterialOnPart(HAPI_NodeId nodeID, HAPI_PartId partID, ref HAPI_MaterialInfo materialInfo)
		{
			return false;
		}

		/// <summary>
		/// Get the material node IDs on faces for specified part.
		/// </summary>
		/// <param name="nodeID">The node ID</param>
		/// <param name="partID">The part to query</param>
		/// <param name="bSingleFaceMaterial">Whether same material on all faces</param>
		/// <param name="materialNodeIDs">The array to fill in with material node IDs. Must be at least size of faceCount</param>
		/// <param name="faceCount">Number of material IDs to query</param>
		/// <returns>True if successfully queried the materials</returns>
		public virtual bool GetMaterialNodeIDsOnFaces(HAPI_NodeId nodeID, HAPI_PartId partID, ref bool bSingleFaceMaterial, [Out] HAPI_NodeId[] materialNodeIDs, int faceCount)
		{
			return false;
		}

		/// <summary>
		/// Get the material info for the given material node ID.
		/// </summary>
		/// <param name="materialNodeID">The material node ID</param>
		/// <param name="materialInfo">Material info to populate</param>
		/// <returns>True if successfully returned the material info</returns>
		public virtual bool GetMaterialInfo(HAPI_NodeId materialNodeID, ref HAPI_MaterialInfo materialInfo, bool bLogError = true)
		{
			return false;
		}

		public virtual bool GetImageInfo(HAPI_NodeId materialNodeID, ref HAPI_ImageInfo imageInfo)
		{
			return false;
		}

		public virtual bool SetImageInfo(HAPI_NodeId materialNodeID, ref HAPI_ImageInfo imageInfo)
		{
			return false;
		}

		public virtual bool RenderTextureToImage(HAPI_NodeId materialNodeID, HAPI_ParmId parmID, bool bLogError = true)
		{
			return false;
		}

		public virtual bool RenderCOPToImage(HAPI_NodeId copNodeID)
		{
			return false;
		}

		public virtual bool ExtractImageToMemory(HAPI_NodeId nodeID, string fileFormat, string imagePlanes, out byte[] buffer)
		{
			buffer = new byte[0];
			return false;
		}

		public virtual bool GetImagePlanes(HAPI_NodeId nodeID, [Out] HAPI_StringHandle[] imagePlanes, int numImagePlanes)
		{
			imagePlanes = new HAPI_StringHandle[0];
			return false;
		}

		/// <summary>
		/// Extract image to given folder path, returning the path to the written image file.
		/// </summary>
		/// <param name="nodeID">Material node ID</param>
		/// <param name="fileFormat">The image's file format</param>
		/// <param name="imagePlanes">Image planes, see docs</param>
		/// <param name="destinationFolderPath">Path to folder where image needs to be written out to</param>
		/// <param name="destinationFilePath">Path to the written image file</param>
		/// <returns>Returns valid path to written image file, or null if failed</returns>
		public virtual bool ExtractImageToFile(HAPI_NodeId nodeID, string fileFormat, string imagePlanes, string destinationFolderPath, out string destinationFilePath)
		{
			destinationFilePath = null;
			return false;
		}


		// PARAMS -----------------------------------------------------------------------------------------------------

		/// <summary>
		/// Fill an array of HAPI_ParmInfo structs with parameter information from asset node.
		/// </summary>
		/// <param name="nodeID">The asset node ID</param>
		/// <param name="parmInfos">Array to fill. Must be at least size of length.</param>
		/// <param name="start">First index of range. Must be at least 0 and at most HAPI_NodeInfo::parmCount - 1</param>
		/// <param name="length">Must be at least 0 and at most HAPI_NodeInfo::parmCount - start</param>
		/// <returns>True if successfully retrieved the HAPI_ParmInfos</returns>
		public virtual bool GetParams(HAPI_NodeId nodeID, [Out] HAPI_ParmInfo[] parmInfos, int start, int length)
		{
			return false;
		}

		public virtual bool GetParmTagName(HAPI_NodeId nodeID, HAPI_ParmId parmID, int tagIndex, out HAPI_StringHandle tagName)
		{
			tagName = 0;
			return false;
		}

		public virtual bool GetParmTagValue(HAPI_NodeId nodeID, HAPI_ParmId parmID, string tagName, out HAPI_StringHandle tagValue)
		{
			tagValue = 0;
			return false;
		}

		public virtual bool ParmHasTag(HAPI_NodeId nodeID, HAPI_ParmId parmID, string tagName, ref bool hasTag)
		{
			return false;
		}

		public virtual bool GetParamIntValues(HAPI_NodeId nodeID, [Out] int[] values, int start, int length)
		{
			return false;
		}

		public virtual bool GetParamIntValue(HAPI_NodeId nodeID, string parmName, int index, out int value)
		{
			value = 0;
			return false;
		}

		public virtual bool GetParamFloatValues(HAPI_NodeId nodeID, [Out] float[] values, int start, int length)
		{
			return false;
		}

		public virtual bool GetParamFloatValue(HAPI_NodeId nodeID, string parmName, int index, out float value)
		{
			value = 0;
			return false;
		}

		public virtual bool GetParamStringValues(HAPI_NodeId nodeID, [Out] HAPI_StringHandle[] values, int start, int length)
		{
			return false;
		}

		public virtual bool GetParamStringValue(HAPI_NodeId nodeID, string parmName, int index, out HAPI_StringHandle value)
		{
			value = 0;
			return false;
		}

		public virtual bool GetParamNodeValue(HAPI_NodeId nodeID, string paramName, out int nodeValue)
		{
			nodeValue = HEU_Defines.HEU_INVALID_NODE_ID;
			return false;
		}

		public virtual bool GetParamChoiceValues(HAPI_NodeId nodeID, [Out] HAPI_ParmChoiceInfo[] values, int start, int length)
		{
			return false;
		}

		public virtual bool SetParamIntValues(HAPI_NodeId nodeID, ref int[] values, int start, int length)
		{
			return false;
		}

		public virtual bool SetParamIntValue(HAPI_NodeId nodeID, string paramName, int index, int value)
		{
			return false;
		}

		public virtual bool SetParamFloatValues(HAPI_NodeId nodeID, ref float[] values, int start, int length)
		{
			return false;
		}

		public virtual bool SetParamFloatValue(HAPI_NodeId nodeID, string paramName, int index, float value)
		{
			return false;
		}

		public virtual bool SetParamStringValue(HAPI_NodeId nodeID, string strValue, HAPI_ParmId parmID, int index)
		{
			return false;
		}

		public virtual bool SetParamStringValue(HAPI_NodeId nodeID, string parmName, string parmValue, int index)
		{
			return false;
		}

		public virtual bool SetParamNodeValue(HAPI_NodeId nodeID, string paramName, HAPI_NodeId nodeValueID)
		{
			return false;
		}

		public virtual bool InsertMultiparmInstance(HAPI_NodeId nodeID, HAPI_ParmId parmID, int instancePosition)
		{
			return false;
		}

		public virtual bool RemoveMultiParmInstance(HAPI_NodeId nodeID, HAPI_ParmId parmID, int instancePosition)
		{
			return false;
		}

		public virtual bool GetParmWithTag(HAPI_NodeId nodeID, string tagName, ref HAPI_ParmId parmID)
		{
			return false;
		}

		public virtual bool RevertParmToDefault(HAPI_NodeId nodeID, string parm_name, int index)
		{
			return false;
		}

		public virtual bool RevertParmToDefaults(HAPI_NodeId nodeID, string parm_name)
		{
			return false;
		}

		public virtual bool GetParmIDFromName(HAPI_NodeId nodeID, string parmName, out HAPI_ParmId parmID)
		{
			parmID = HEU_Defines.HAPI_INVALID_PARM_ID;
			return false;
		}

		public virtual bool GetParmStringValue(HAPI_NodeId nodeID, string parmName, int index, bool evaluate, out HAPI_StringHandle value)
		{
			value = 0;
			return false;
		}

		// INPUT NODES ------------------------------------------------------------------------------------------------

		public virtual bool CreateInputNode(out HAPI_NodeId nodeID, string name)
		{
			nodeID = HEU_Defines.HEU_INVALID_NODE_ID;
			return false;
		}

		public virtual bool CreateHeightfieldInputNode(HAPI_NodeId parentNodeID, string name, int xSize, int ySize, float voxelSize,
			out HAPI_NodeId heightfieldNodeID, out HAPI_NodeId heightNodeID, out HAPI_NodeId maskNodeID, out HAPI_NodeId mergeNodeID)
		{
			heightfieldNodeID = HEU_Defines.HEU_INVALID_NODE_ID;
			heightNodeID = HEU_Defines.HEU_INVALID_NODE_ID;
			maskNodeID = HEU_Defines.HEU_INVALID_NODE_ID;
			mergeNodeID = HEU_Defines.HEU_INVALID_NODE_ID;
			return false;
		}

		public virtual bool CreateHeightfieldInputVolumeNode(HAPI_NodeId parentNodeID, out HAPI_NodeId newNodeID, string name, int xSize, int ySize, float voxelSize)
		{
			newNodeID = HEU_Defines.HEU_INVALID_NODE_ID;
			return false;
		}

		// PRESETS ----------------------------------------------------------------------------------------------------

		/// <summary>
		/// Returns a preset blob of the current state of all parameter values.
		/// </summary>
		/// <param name="nodeID">The asset node ID</param>
		/// <param name="presetData">The acquired preset data (or empty if failed)</param>
		/// <returns>True if successfully acquired the preset data</returns>
		public virtual bool GetPreset(HAPI_NodeId nodeID, out byte[] presetData)
		{
			presetData = new byte[0];
			return false;
		}

		/// <summary>
		/// Sets an asset's preset data.
		/// </summary>
		/// <param name="nodeID">The asset node ID</param>
		/// <param name="presetData">The preset data buffer to set</param>
		/// <returns>True if successfully set the preset</returns>
		public virtual bool SetPreset(HAPI_NodeId nodeID, byte[] presetData)
		{
			return false;
		}


		// VOLUMES -----------------------------------------------------------------------------------------------------

		public virtual bool GetVolumeInfo(HAPI_NodeId nodeID, HAPI_PartId partID, ref HAPI_VolumeInfo volumeInfo)
		{
			return false;
		}

		public virtual bool GetHeightFieldData(HAPI_NodeId nodeID, HAPI_PartId partID, float[] valuesArray, int start, int length)
		{
			return false;
		}

		public virtual bool SetVolumeInfo(HAPI_NodeId nodeID, HAPI_PartId partID, ref HAPI_VolumeInfo volumeInfo)
		{
			return false;
		}

		public virtual bool SetVolumeTileFloatData(HAPI_NodeId nodeID, HAPI_PartId partID, ref HAPI_VolumeTileInfo tileInfo, float[] valuesArray, int length)
		{
			valuesArray = new float[0];
			return false;
		}

		public virtual bool GetVolumeBounds(HAPI_NodeId nodeID, HAPI_PartId partID, out float x_min, out float y_min, out float z_min,
			out float x_max, out float y_max, out float z_max, out float x_center, out float y_center, out float z_center)
		{
			x_min = y_min = z_min = x_max = y_max = z_max = x_center = y_center = z_center = 0;
			return false;
		}

		public virtual bool SetHeightFieldData(HAPI_NodeId nodeID, HAPI_PartId partID, string name, float[] valuesArray, int start, int length)
		{
			return false;
		}

		// CACHING ----------------------------------------------------------------------------------------------------

		public virtual bool GetActiveCacheCount(out int activeCacheCount)
		{
			activeCacheCount = 0;
			return false;
		}

		public virtual bool GetActiveCacheNames([Out] HAPI_StringHandle[] cacheNamesArray, int activeCacheCount)
		{
			return false;
		}

		public virtual bool GetCacheProperty(string cacheName, HAPI_CacheProperty cacheProperty, out int propertyValue)
		{
			propertyValue = 0;
			return false;
		}

		public virtual bool SetCacheProperty(string cacheName, HAPI_CacheProperty cacheProperty, int propertyValue)
		{
			return false;
		}

		public virtual bool SaveGeoToFile(HAPI_NodeId nodeID, string fileName)
		{
			return false;
		}

		public virtual bool LoadGeoFromFile(HAPI_NodeId nodeID, string file_name)
		{
			return false;
		}

		public virtual bool GetGeoSize(HAPI_NodeId nodeID, string format, out int size)
		{
			size = 0;
			return false;
		}

		// HANDLES ----------------------------------------------------------------------------------------------------

		public virtual bool GetHandleInfo(HAPI_NodeId nodeID, [Out] HAPI_HandleInfo[] handleInfos, int start, int length)
		{
			return false;
		}

		public virtual bool GetHandleBindingInfo(HAPI_NodeId nodeID, int handleIndex, [Out] HAPI_HandleBindingInfo[] handleBindingInfos, int start, int length)
		{
			return false;
		}

		public virtual bool ConvertTransform(ref HAPI_TransformEuler inTransform, HAPI_RSTOrder RSTOrder, HAPI_XYZOrder ROTOrder, out HAPI_TransformEuler outTransform)
		{
			outTransform = new HAPI_TransformEuler();
			return false;
		}
	}

}   // HoudiniEngineUnity