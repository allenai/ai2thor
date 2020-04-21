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
using UnityEngine.Serialization;


namespace HoudiniEngineUnity
{
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Typedefs (copy these from HEU_Common.cs)
	using HAPI_NodeId = System.Int32;


	// <summary>
	/// Represents a general node for sending data upstream to Houdini.
	/// Currently only supports sending geometry upstream.
	/// Specify input data as file (eg. bgeo), HDA, and Unity gameobjects.
	/// </summary>
	public class HEU_InputNode : ScriptableObject
	{
		// DATA -------------------------------------------------------------------------------------------------------

		// The type of input node based on how it was specified in the HDA
		public enum InputNodeType
		{
			CONNECTION,     // As an asset connection
			NODE,           // Pure input asset node
			PARAMETER,      // As an input parameter
		}

		[SerializeField]
		private InputNodeType _inputNodeType;

		public InputNodeType InputType { get { return _inputNodeType; } }

		// The type of input data set by user
		public enum InputObjectType
		{
			HDA,
			UNITY_MESH,
			//CURVE
		}

		[SerializeField]
		private InputObjectType _inputObjectType = InputObjectType.UNITY_MESH;

		public InputObjectType ThisInputObjectType { get { return _inputObjectType; } }

		[SerializeField]
		private InputObjectType _pendingInputObjectType = InputObjectType.UNITY_MESH;

		public InputObjectType PendingInputObjectType { get { return _pendingInputObjectType; } set { _pendingInputObjectType = value; } }

		// The IDs of the object merge created for the input objects
		[SerializeField]
		private List<HEU_InputObjectInfo> _inputObjects = new List<HEU_InputObjectInfo>();

		// This holds node IDs of input nodes that are created for uploading mesh data
		[SerializeField]
		private List<HAPI_NodeId> _inputObjectsConnectedAssetIDs = new List<HAPI_NodeId>();

#pragma warning disable 0414
		// [DEPRECATED: replaced with _inputAssetInfos]
		// Asset input: external reference used for UI
		[SerializeField]
		private GameObject _inputAsset;

		// [DEPRECATED: replaced with _inputAssetInfos]
		// Asset input: internal reference to the connected asset (valid if connected)
		[SerializeField]
		private GameObject _connectedInputAsset;
#pragma warning restore 0414

		// List of input HDAs
		[SerializeField]
		private List<HEU_InputHDAInfo> _inputAssetInfos = new List<HEU_InputHDAInfo>();

		[SerializeField]
		private HAPI_NodeId _nodeID;

		public HAPI_NodeId InputNodeID { get { return _nodeID; } }

		[SerializeField]
		private int _inputIndex;

		[SerializeField]
		private bool _requiresCook;

		public bool RequiresCook { get { return _requiresCook; } set { _requiresCook = value; } }

		[SerializeField]
		private bool _requiresUpload;

		public bool RequiresUpload { get { return _requiresUpload; } set { _requiresUpload = value; } }

		[SerializeField]
		private string _inputName;

		public string InputName { get { return _inputName; } }

		[SerializeField]
		private string _labelName;

		public string LabelName { get { return _labelName; } }

		[SerializeField]
		private string _paramName;

		public string ParamName { get { return _paramName; } set { _paramName = value; } }

		[SerializeField]
		private HAPI_NodeId _connectedNodeID = HEU_Defines.HEU_INVALID_NODE_ID;

		[SerializeField]
		// Enabling Keep World Transform by default to keep consistent with other plugins
		private bool _keepWorldTransform = true;

		// If true, sets the SOP/merge (object merge) node to use INTO_THIS_OBJECT transform type. Otherwise NONE.
		public bool KeepWorldTransform { get { return _keepWorldTransform; } set { _keepWorldTransform = value; } }

		[SerializeField]
		private bool _packGeometryBeforeMerging;

		// Acts same as SOP/merge (object merge) Pack Geometry Before Merging parameter value.
		public bool PackGeometryBeforeMerging { get { return _packGeometryBeforeMerging; } set { _packGeometryBeforeMerging = value; } }

		[SerializeField]
		private HEU_HoudiniAsset _parentAsset;

		public HEU_HoudiniAsset ParentAsset { get { return _parentAsset; } }

		public enum InputActions
		{
			ACTION,
			DELETE,
			INSERT
		}

		public bool IsAssetInput() { return _inputNodeType == InputNodeType.CONNECTION; }


		// LOGIC ------------------------------------------------------------------------------------------------------

		public static HEU_InputNode CreateSetupInput(HAPI_NodeId nodeID, int inputIndex, string inputName, string labelName, InputNodeType inputNodeType, HEU_HoudiniAsset parentAsset)
		{
			HEU_InputNode newInput = ScriptableObject.CreateInstance<HEU_InputNode>();
			newInput._nodeID = nodeID;
			newInput._inputIndex = inputIndex;
			newInput._inputName = inputName;
			newInput._labelName = labelName;
			newInput._inputNodeType = inputNodeType;
			newInput._parentAsset = parentAsset;

			newInput._requiresUpload = false;
			newInput._requiresCook = false;

			return newInput;
		}

		public void SetInputNodeID(HAPI_NodeId nodeID)
		{
			_nodeID = nodeID;
		}

		public void DestroyAllData(HEU_SessionBase session)
		{
			ClearUICache();

			DisconnectAndDestroyInputs(session);
			RemoveAllInputEntries();
		}

		private void ResetInputObjectTransforms()
		{
			for (int i = 0; i < _inputObjects.Count; ++i)
			{
				_inputObjects[i]._syncdTransform = Matrix4x4.identity;
			}
		}

		public void ResetInputNode(HEU_SessionBase session)
		{
			ResetConnectionForForceUpdate(session);
			RemoveAllInputEntries();
			ClearUICache();

			ChangeInputType(session, InputObjectType.UNITY_MESH);
		}

		public void InsertInputEntry(int index, GameObject newInputGameObject)
		{
			if (_inputObjectType == InputObjectType.UNITY_MESH)
			{
				if (index >= 0 && index < _inputObjects.Count)
				{
					_inputObjects.Insert(index, CreateInputObjectInfo(newInputGameObject));
				}
				else
				{
					Debug.LogErrorFormat("Insert index {0} out of range (number of items is {1})", index, _inputObjects.Count);
				}
			}
			else if (_inputObjectType == InputObjectType.HDA)
			{
				if (index >= 0 && index < _inputAssetInfos.Count)
				{
					_inputAssetInfos.Insert(index, CreateInputHDAInfo(newInputGameObject));
				}
				else
				{
					Debug.LogErrorFormat("Insert index {0} out of range (number of items is {1})", index, _inputAssetInfos.Count);
				}
			}
		}

		public GameObject GetInputEntryGameObject(int index)
		{
			if (_inputObjectType == InputObjectType.UNITY_MESH)
			{
				if (index >= 0 && index < _inputObjects.Count)
				{
					return _inputObjects[index]._gameObject;
				}
				else
				{
					Debug.LogErrorFormat("Get index {0} out of range (number of items is {1})", index, _inputObjects.Count);
				}
			}
			else if (_inputObjectType == InputObjectType.HDA)
			{
				if (index >= 0 && index < _inputAssetInfos.Count)
				{
					return _inputAssetInfos[index]._pendingGO;
				}
				else
				{
					Debug.LogErrorFormat("Get index {0} out of range (number of items is {1})", index, _inputAssetInfos.Count);
				}
			}
			return null;
		}

		public HEU_InputObjectInfo AddInputEntryAtEnd(GameObject newEntryGameObject)
		{
			if (_inputObjectType == InputObjectType.UNITY_MESH)
			{
				InternalAddInputObjectAtEnd(newEntryGameObject);
			}
			else if (_inputObjectType == InputObjectType.HDA)
			{
				InternalAddInputHDAAtEnd(newEntryGameObject);
			}
			return null;
		}

		public void RemoveAllInputEntries()
		{
			_inputObjects.Clear();
			_inputAssetInfos.Clear();
		}

		public int NumInputEntries()
		{
			if (_inputObjectType == InputObjectType.UNITY_MESH)
			{
				return _inputObjects.Count;
			}
			else if (_inputObjectType == InputObjectType.HDA)
			{
				return _inputAssetInfos.Count;
			}
			return 0;
		}

		public void ChangeInputType(HEU_SessionBase session, InputObjectType newType)
		{
			if (newType == _inputObjectType)
			{
				return;
			}

			DisconnectAndDestroyInputs(session);

			_inputObjectType = newType;
			_pendingInputObjectType = _inputObjectType;
		}

		/// <summary>
		/// Reset the connected state so that any previous connection will be remade
		/// </summary>
		public void ResetConnectionForForceUpdate(HEU_SessionBase session)
		{
			if (_inputObjectType == InputObjectType.HDA)
			{
				if (AreAnyInputHDAsConnected())
				{
					// By disconnecting here, we can then properly reconnect again.
					// This is needed when loading a saved scene and recooking.
					DisconnectConnectedMergeNode(session);

					// Clear out input HDA hooks (upstream callback)
					ClearConnectedInputHDAs();
				}
			}
		}

		public void UploadInput(HEU_SessionBase session)
		{
			if (_nodeID == HEU_Defines.HEU_INVALID_NODE_ID)
			{
				Debug.LogErrorFormat("Input Node ID is invalid. Unable to upload input. Try recooking.");
				return;
			}

			if (_pendingInputObjectType != _inputObjectType)
			{
				ChangeInputType(session, _pendingInputObjectType);
			}

			if (_inputObjectType == InputObjectType.UNITY_MESH)
			{
				// Connect regular gameobjects

				if (_inputObjects == null || _inputObjects.Count == 0)
				{
					DisconnectAndDestroyInputs(session);
				}
				else
				{
					DisconnectAndDestroyInputs(session);

					// Create merge object, and input nodes with data, then connect them to the merge object
					bool bResult = HEU_InputUtility.CreateInputNodeWithMultiObjects(session, _nodeID, ref _connectedNodeID, ref _inputObjects, ref _inputObjectsConnectedAssetIDs, _keepWorldTransform);
					if (!bResult)
					{
						DisconnectAndDestroyInputs(session);
						return;
					}

					// Now connect from this asset to the merge object
					ConnectToMergeObject(session);

					if (!UploadObjectMergeTransformType(session))
					{
						Debug.LogErrorFormat("Failed to upload object merge transform type!");
						return;
					}

					if (!UploadObjectMergePackGeometry(session))
					{
						Debug.LogErrorFormat("Failed to upload object merge pack geometry value!");
						return;
					}
				}
			}
			else if (_inputObjectType == InputObjectType.HDA)
			{
				// Connect HDAs

				// First clear all previous input connections
				DisconnectAndDestroyInputs(session);

				// Create merge object, and connect all input HDAs
				bool bResult = HEU_InputUtility.CreateInputNodeWithMultiAssets(session, _parentAsset, ref _connectedNodeID, ref _inputAssetInfos, _keepWorldTransform, -1);
				if (!bResult)
				{
					DisconnectAndDestroyInputs(session);
					return;
				}

				// Now connect from this asset to the merge object
				ConnectToMergeObject(session);

				if (!UploadObjectMergeTransformType(session))
				{
					Debug.LogErrorFormat("Failed to upload object merge transform type!");
					return;
				}

				if (!UploadObjectMergePackGeometry(session))
				{
					Debug.LogErrorFormat("Failed to upload object merge pack geometry value!");
					return;
				}
			}
			//else if (_inputObjectType == InputObjectType.CURVE)
			//{
			// TODO INPUT NODE - create new Curve SOP (add HEU_Curve here?)
			//}
			else
			{
				Debug.LogErrorFormat("Unsupported input type {0}. Unable to upload input.", _inputObjectType);
			}

			RequiresUpload = false;
			RequiresCook = true;

			ClearUICache();
		}

		public bool AreAnyInputHDAsConnected()
		{
			foreach (HEU_InputHDAInfo asset in _inputAssetInfos)
			{
				if (asset._connectedGO != null)
				{
					return true;
				}
			}
			return false;
		}

		public void ReconnectToUpstreamAsset()
		{
			if (_inputObjectType == InputObjectType.HDA && AreAnyInputHDAsConnected())
			{
				foreach (HEU_InputHDAInfo hdaInfo in _inputAssetInfos)
				{
					HEU_HoudiniAssetRoot inputAssetRoot = hdaInfo._connectedGO != null ? hdaInfo._connectedGO.GetComponent<HEU_HoudiniAssetRoot>() : null;
					if (inputAssetRoot != null && inputAssetRoot._houdiniAsset != null)
					{
						_parentAsset.ConnectToUpstream(inputAssetRoot._houdiniAsset);
					}
				}
			}
		}

		private HEU_InputObjectInfo CreateInputObjectInfo(GameObject inputGameObject)
		{
			HEU_InputObjectInfo newObjectInfo = new HEU_InputObjectInfo();
			newObjectInfo._gameObject = inputGameObject;

			return newObjectInfo;
		}

		private HEU_InputHDAInfo CreateInputHDAInfo(GameObject inputGameObject)
		{
			HEU_InputHDAInfo newInputInfo = new HEU_InputHDAInfo();
			newInputInfo._pendingGO = inputGameObject;
			newInputInfo._connectedInputNodeID = HEU_Defines.HEU_INVALID_NODE_ID;

			return newInputInfo;
		}

		private HEU_InputObjectInfo InternalAddInputObjectAtEnd(GameObject newInputGameObject)
		{
			HEU_InputObjectInfo inputObject = CreateInputObjectInfo(newInputGameObject);
			_inputObjects.Add(inputObject);
			return inputObject;
		}

		private HEU_InputHDAInfo InternalAddInputHDAAtEnd(GameObject newInputHDA)
		{
			HEU_InputHDAInfo inputInfo = CreateInputHDAInfo(newInputHDA);
			_inputAssetInfos.Add(inputInfo);
			return inputInfo;
		}

		private void DisconnectConnectedMergeNode(HEU_SessionBase session)
		{
			if (session != null)
			{
				//Debug.LogWarningFormat("Disconnecting Node Input for _nodeID={0} with type={1}", _nodeID, _inputNodeType);

				if (_inputNodeType == InputNodeType.PARAMETER)
				{
					HEU_ParameterData paramData = _parentAsset.Parameters.GetParameter(_paramName);
					if (paramData == null)
					{
						Debug.LogErrorFormat("Unable to find parameter with name {0}!", _paramName);
					}
					else if (!session.SetParamStringValue(_nodeID, "", paramData.ParmID, 0))
					{
						Debug.LogErrorFormat("Unable to clear object path parameter for input node!");
					}
				}
				else if (_nodeID != HEU_Defines.HEU_INVALID_NODE_ID)
				{
					session.DisconnectNodeInput(_nodeID, _inputIndex, false);
				}
			}
		}

		private void ClearConnectedInputHDAs()
		{
			int numInputs = _inputAssetInfos.Count;
			for (int i = 0; i < numInputs; ++i)
			{
				if (_inputAssetInfos[i] == null)
				{
					continue;
				}

				HEU_HoudiniAssetRoot inputAssetRoot = _inputAssetInfos[i]._connectedGO != null ? _inputAssetInfos[i]._connectedGO.GetComponent<HEU_HoudiniAssetRoot>() : null;
				if (inputAssetRoot != null)
				{
					_parentAsset.DisconnectFromUpstream(inputAssetRoot._houdiniAsset);
				}

				_inputAssetInfos[i]._connectedGO = null;
				_inputAssetInfos[i]._connectedInputNodeID = HEU_Defines.HEU_INVALID_NODE_ID;
			}
		}

		/// <summary>
		/// Connect the input to the merge object node
		/// </summary>
		/// <param name="session"></param>
		private void ConnectToMergeObject(HEU_SessionBase session)
		{
			if (_inputNodeType == InputNodeType.PARAMETER)
			{
				if (string.IsNullOrEmpty(_paramName))
				{
					Debug.LogErrorFormat("Invalid parameter name for input node of parameter type!");
					return;
				}

				if (!session.SetParamNodeValue(_nodeID, _paramName, _connectedNodeID))
				{
					Debug.LogErrorFormat("Unable to connect to input node!");
					return;
				}

				//Debug.LogFormat("Setting input connection for parameter {0} with {1} connecting to {2}", _paramName, _nodeID, _connectedNodeID);
			}
			else
			{
				if (!session.ConnectNodeInput(_nodeID, _inputIndex, _connectedNodeID))
				{
					Debug.LogErrorFormat("Unable to connect to input node!");
					return;
				}
			}
		}

		private void DisconnectAndDestroyInputs(HEU_SessionBase session)
		{
			// First disconnect the merge node from its connections
			DisconnectConnectedMergeNode(session);

			// Clear out input HDA hooks (upstream callback)
			ClearConnectedInputHDAs();

			if (session != null)
			{
				// Delete the input nodes that were created
				foreach (HAPI_NodeId nodeID in _inputObjectsConnectedAssetIDs)
				{
					if (nodeID != HEU_Defines.HEU_INVALID_NODE_ID)
					{
						session.DeleteNode(nodeID);
					}
				}

				// Delete the SOP/merge we created
				if (_connectedNodeID != HEU_Defines.HEU_INVALID_NODE_ID && HEU_HAPIUtility.IsNodeValidInHoudini(session, _connectedNodeID))
				{
					// We'll delete the parent Object because we presume to have created the SOP/merge ourselves.
					// If the parent Object doesn't get deleted, it sticks around unused.
					HAPI_NodeInfo parentNodeInfo = new HAPI_NodeInfo();
					if (session.GetNodeInfo(_connectedNodeID, ref parentNodeInfo))
					{
						if (parentNodeInfo.parentId != HEU_Defines.HEU_INVALID_NODE_ID)
						{
							session.DeleteNode(parentNodeInfo.parentId);
						}
					}
				}
			}

			_inputObjectsConnectedAssetIDs.Clear();
			_connectedNodeID = HEU_Defines.HEU_INVALID_NODE_ID;
		}

		public int GetConnectedInputCount()
		{
			if (_inputObjectType == InputObjectType.UNITY_MESH)
			{
				return _inputObjectsConnectedAssetIDs.Count;
			}
			else if (_inputObjectType == InputObjectType.HDA)
			{
				return _inputAssetInfos.Count;
			}
			return 0;
		}

		public HAPI_NodeId GetConnectedNodeID(int index)
		{
			if (_inputObjectType == InputObjectType.UNITY_MESH)
			{
				if (index >=0 && index < _inputObjectsConnectedAssetIDs.Count)
				{
					return _inputObjectsConnectedAssetIDs[index];
				}
			}
			else if (_inputObjectType == InputObjectType.HDA)
			{
				return _inputAssetInfos[index]._connectedInputNodeID;
			}
			return HEU_Defines.HEU_INVALID_NODE_ID;
		}

		public bool UploadObjectMergeTransformType(HEU_SessionBase session)
		{
			if (_connectedNodeID == HEU_Defines.HEU_INVALID_NODE_ID)
			{
				return false;
			}

			int transformType = _keepWorldTransform ? 1 : 0;

			HAPI_NodeId inputNodeID = HEU_Defines.HEU_INVALID_NODE_ID;

			// Use _connectedNodeID to find its connections, which should be
			// the object merge nodes. We set the pack parameter on those.
			// Presume that the number of connections to  _connectedNodeID is equal to 
			// size of GetConnectedInputCount() (i.e. the number of inputs)
			int numConnected = GetConnectedInputCount();
			for (int i = 0; i < numConnected; ++i)
			{
				if (GetConnectedNodeID(i) == HEU_Defines.HEU_INVALID_NODE_ID)
				{
					continue;
				}

				inputNodeID = HEU_Defines.HEU_INVALID_NODE_ID;
				if (session.QueryNodeInput(_connectedNodeID, i, out inputNodeID, false))
				{
					session.SetParamIntValue(inputNodeID, HEU_Defines.HAPI_OBJMERGE_TRANSFORM_PARAM, 0, transformType);
				}
			}

			return true;
		}

		private bool UploadObjectMergePackGeometry(HEU_SessionBase session)
		{
			if (_connectedNodeID == HEU_Defines.HAPI_INVALID_PARM_ID)
			{
				return false;
			}

			int packEnabled = _packGeometryBeforeMerging ? 1 : 0;

			HAPI_NodeId inputNodeID = HEU_Defines.HEU_INVALID_NODE_ID;

			// Use _connectedNodeID to find its connections, which should be
			// the object merge nodes. We set the pack parameter on those.
			// Presume that the number of connections to  _connectedNodeID is equal to 
			// size of GetConnectedInputCount() (i.e. the number of inputs)
			int numConnected = GetConnectedInputCount();
			for (int i = 0; i < numConnected; ++i)
			{
				if (GetConnectedNodeID(i) == HEU_Defines.HEU_INVALID_NODE_ID)
				{
					continue;
				}

				inputNodeID = HEU_Defines.HEU_INVALID_NODE_ID;
				if (session.QueryNodeInput(_connectedNodeID, i, out inputNodeID, false))
				{
					session.SetParamIntValue(inputNodeID, HEU_Defines.HAPI_OBJMERGE_PACK_GEOMETRY, 0, packEnabled);
				}
			}

			return true;
		}

		public bool HasInputNodeTransformChanged()
		{
			// Only need to check Mesh inputs, since HDA inputs don't upload transform
			if (_inputObjectType == InputObjectType.UNITY_MESH)
			{
				for (int i = 0; i < _inputObjects.Count; ++i)
				{
					if (_inputObjects[i]._gameObject != null)
					{
						if (_inputObjects[i]._useTransformOffset)
						{
							if (!HEU_HAPIUtility.IsSameTransform(ref _inputObjects[i]._syncdTransform, ref _inputObjects[i]._translateOffset, ref _inputObjects[i]._rotateOffset, ref _inputObjects[i]._scaleOffset))
							{
								return true;
							}
						}
						else if (_inputObjects[i]._gameObject.transform.localToWorldMatrix != _inputObjects[i]._syncdTransform)
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		public void UploadInputObjectTransforms(HEU_SessionBase session)
		{
			// Only need to upload Mesh inputs, since HDA inputs don't upload transform
			if (_nodeID == HEU_Defines.HAPI_INVALID_PARM_ID || _inputObjectType != InputObjectType.UNITY_MESH)
			{
				return;
			}

			int numInputs = GetConnectedInputCount();
			for (int i = 0; i < numInputs; ++i)
			{
				HAPI_NodeId connectedNodeID = GetConnectedNodeID(i);
				if (connectedNodeID != HEU_Defines.HEU_INVALID_NODE_ID && _inputObjects[i]._gameObject != null)
				{
					HEU_InputUtility.UploadInputObjectTransform(session, _inputObjects[i], connectedNodeID, _keepWorldTransform);
				}
			}
		}

		/// <summary>
		/// Update the input connection based on the fact that the owner asset was recreated
		/// in the given session.
		/// All connections will be invalidated without cleaning up because the IDs can't be trusted.
		/// </summary>
		/// <param name="session"></param>
		public void UpdateOnAssetRecreation(HEU_SessionBase session)
		{
			if (_inputObjectType == InputObjectType.HDA)
			{
				// For HDA inputs, need to recreate the merge node, cook the HDAs, and connect the HDAs to the merge nodes

				// For backwards compatiblity, copy the previous single input asset reference into the new input asset list
				if (_inputAsset != null && _inputAssetInfos.Count == 0)
				{
					InternalAddInputHDAAtEnd(_inputAsset);

					// Clear out these deprecated references for forever
					_inputAsset = null;
					_connectedInputAsset = null;
				}

				// Don't delete the merge node ID as its most likely not valid
				_connectedNodeID = HEU_Defines.HEU_INVALID_NODE_ID;

				int numInputs = _inputAssetInfos.Count;
				for (int i = 0; i < numInputs; ++i)
				{
					_inputAssetInfos[i]._connectedGO = null;
					_inputAssetInfos[i]._connectedInputNodeID = HEU_Defines.HEU_INVALID_NODE_ID;
				}
			}
			else if (_inputObjectType == InputObjectType.UNITY_MESH)
			{
				// For mesh input, invalidate _inputObjectsConnectedAssetIDs and _connectedNodeID as their
				// nodes most likely don't exist, and the IDs will not be correct since this asset got recreated
				// Note that _inputObjects don't need to be cleared as they will be used when recreating the connections.
				_inputObjectsConnectedAssetIDs.Clear();
				_connectedNodeID = HEU_Defines.HEU_INVALID_NODE_ID;
			}
		}

		public void CopyInputValuesTo(HEU_SessionBase session, HEU_InputNode destInputNode)
		{
			destInputNode._pendingInputObjectType = _inputObjectType;

			if (destInputNode._inputObjectType == InputObjectType.HDA)
			{
				destInputNode.ResetConnectionForForceUpdate(session);
			}

			destInputNode.RemoveAllInputEntries();

			foreach (HEU_InputObjectInfo srcInputObject in _inputObjects)
			{
				HEU_InputObjectInfo newInputObject = new HEU_InputObjectInfo();
				srcInputObject.CopyTo(newInputObject);

				destInputNode._inputObjects.Add(newInputObject);
			}

			foreach (HEU_InputHDAInfo srcInputInfo in _inputAssetInfos)
			{
				HEU_InputHDAInfo newInputInfo = new HEU_InputHDAInfo();
				srcInputInfo.CopyTo(newInputInfo);

				destInputNode._inputAssetInfos.Add(newInputInfo);
			}

			destInputNode._keepWorldTransform = _keepWorldTransform;
			destInputNode._packGeometryBeforeMerging = _packGeometryBeforeMerging;
		}

		public void PopulateInputPreset(HEU_InputPreset inputPreset)
		{
			inputPreset._inputObjectType = _inputObjectType;

			// Deprecated and replaced with _inputAssetPresets. Leaving it in for backwards compatibility.
			//inputPreset._inputAssetName = _inputAsset != null ? _inputAsset.name : "";

			inputPreset._inputIndex = _inputIndex;
			inputPreset._inputName = _inputName;

			inputPreset._keepWorldTransform = _keepWorldTransform;
			inputPreset._packGeometryBeforeMerging = _packGeometryBeforeMerging;

			foreach (HEU_InputObjectInfo inputObject in _inputObjects)
			{
				HEU_InputObjectPreset inputObjectPreset = new HEU_InputObjectPreset();

				if (inputObject._gameObject != null)
				{
					inputObjectPreset._gameObjectName = inputObject._gameObject.name;

					// Tag whether scene or project input object
					inputObjectPreset._isSceneObject = !HEU_GeneralUtility.IsGameObjectInProject(inputObject._gameObject);
					if (!inputObjectPreset._isSceneObject)
					{
						// For inputs in project, use the project path as name
						inputObjectPreset._gameObjectName = HEU_AssetDatabase.GetAssetOrScenePath(inputObject._gameObject);
					}
				}
				else
				{
					inputObjectPreset._gameObjectName = "";
				}

				inputObjectPreset._useTransformOffset = inputObject._useTransformOffset;
				inputObjectPreset._translateOffset = inputObject._translateOffset;
				inputObjectPreset._rotateOffset = inputObject._rotateOffset;
				inputObjectPreset._scaleOffset = inputObject._scaleOffset;

				inputPreset._inputObjectPresets.Add(inputObjectPreset);
			}

			foreach (HEU_InputHDAInfo hdaInfo in _inputAssetInfos)
			{
				HEU_InputAssetPreset inputAssetPreset = new HEU_InputAssetPreset();

				if (hdaInfo._connectedGO != null)
				{
					if (!HEU_GeneralUtility.IsGameObjectInProject(hdaInfo._connectedGO))
					{
						inputAssetPreset._gameObjectName = hdaInfo._connectedGO.name; 
					}
					else
					{
						inputAssetPreset._gameObjectName = "";
					}

					inputPreset._inputAssetPresets.Add(inputAssetPreset);
				}
			}
		}

		public void LoadPreset(HEU_SessionBase session, HEU_InputPreset inputPreset)
		{
			ResetInputNode(session);

			ChangeInputType(session, inputPreset._inputObjectType);

			if (inputPreset._inputObjectType == InputObjectType.UNITY_MESH)
			{
				bool bSet = false;
				int numObjects = inputPreset._inputObjectPresets.Count;
				for (int i = 0; i < numObjects; ++i)
				{
					bSet = false;

					if (!string.IsNullOrEmpty(inputPreset._inputObjectPresets[i]._gameObjectName))
					{
						GameObject inputGO = null;
						if (inputPreset._inputObjectPresets[i]._isSceneObject)
						{
							inputGO = HEU_GeneralUtility.GetGameObjectByNameInScene(inputPreset._inputObjectPresets[i]._gameObjectName);
						}
						else
						{
							// Use the _gameObjectName as path to find in scene
							inputGO = HEU_AssetDatabase.LoadAssetAtPath(inputPreset._inputObjectPresets[i]._gameObjectName, typeof(GameObject)) as GameObject;
							if (inputGO == null)
							{
								Debug.LogErrorFormat("Unable to find input at {0}", inputPreset._inputObjectPresets[i]._gameObjectName);
							}
						}

						if (inputGO != null)
						{
							HEU_InputObjectInfo inputObject = InternalAddInputObjectAtEnd(inputGO);
							bSet = true;

							inputObject._useTransformOffset = inputPreset._inputObjectPresets[i]._useTransformOffset;
							inputObject._translateOffset = inputPreset._inputObjectPresets[i]._translateOffset;
							inputObject._rotateOffset = inputPreset._inputObjectPresets[i]._rotateOffset;
							inputObject._scaleOffset = inputPreset._inputObjectPresets[i]._scaleOffset;
						}
						else
						{
							Debug.LogWarningFormat("Gameobject with name {0} not found. Unable to set input object.", inputPreset._inputAssetName);
						}
					}

					if (!bSet)
					{
						// Add dummy spot (user can replace it manually)
						InternalAddInputObjectAtEnd(null);
					}
				}
			}
			else if (inputPreset._inputObjectType == HEU_InputNode.InputObjectType.HDA)
			{
				bool bSet = false;
				int numInptus = inputPreset._inputAssetPresets.Count;
				for (int i = 0; i < numInptus; ++i)
				{
					bSet = false;
					if (!string.IsNullOrEmpty(inputPreset._inputAssetPresets[i]._gameObjectName))
					{
						bSet = FindAddToInputHDA(inputPreset._inputAssetPresets[i]._gameObjectName);
					}

					if (!bSet)
					{
						// Couldn't add for some reason, so just add dummy spot (user can replace it manually)
						InternalAddInputHDAAtEnd(null);
					}
				}

				if (numInptus == 0 && !string.IsNullOrEmpty(inputPreset._inputAssetName))
				{
					// Old preset. Add it to input
					FindAddToInputHDA(inputPreset._inputAssetName);
				}
			}

			KeepWorldTransform = inputPreset._keepWorldTransform;
			PackGeometryBeforeMerging = inputPreset._packGeometryBeforeMerging;

			RequiresUpload = true;

			ClearUICache();
		}

		private bool FindAddToInputHDA(string gameObjectName)
		{
			HEU_HoudiniAssetRoot inputAssetRoot = HEU_GeneralUtility.GetHDAByGameObjectNameInScene(gameObjectName);
			if (inputAssetRoot != null && inputAssetRoot._houdiniAsset != null)
			{
				// Adding to list will take care of reconnecting
				InternalAddInputHDAAtEnd(inputAssetRoot.gameObject);
				return true;
			}
			else
			{
				Debug.LogWarningFormat("HDA with gameobject name {0} not found. Unable to set input asset.", gameObjectName);
			}

			return false;
		}

		public void NotifyParentRemovedInput()
		{
			if (_parentAsset != null)
			{
				_parentAsset.RemoveInputNode(this);
			}
		}

		// UI CACHE ---------------------------------------------------------------------------------------------------

		public HEU_InputNodeUICache _uiCache;

		public void ClearUICache()
		{
			_uiCache = null;
		}

		/// <summary>
		/// Appends given selectedObjects to the input field.
		/// </summary>
		/// <param name="selectedObjects">Array of GameObjects that should be appended into new input entries</param>
		public void HandleSelectedObjectsForInputObjects(GameObject[] selectedObjects)
		{
			if (selectedObjects != null && selectedObjects.Length > 0)
			{
				GameObject rootGO = ParentAsset.RootGameObject;

				foreach (GameObject selected in selectedObjects)
				{
					if (selected == rootGO)
					{
						continue;
					}

					InternalAddInputObjectAtEnd(selected);
				}

				RequiresUpload = true;

				if (HEU_PluginSettings.CookingEnabled && ParentAsset.AutoCookOnParameterChange)
				{
					ParentAsset.RequestCook(bCheckParametersChanged: true, bAsync: true, bSkipCookCheck: false, bUploadParameters: true);
				}
			}
		}

		/// <summary>
		///  Appends given selectedObjects to the input field.
		/// </summary>
		/// <param name="selectedObjects">Array of HDAs that should be appended into new input entries</param>
		public void HandleSelectedObjectsForInputHDAs(GameObject[] selectedObjects)
		{
			if (selectedObjects != null && selectedObjects.Length > 0)
			{
				GameObject rootGO = ParentAsset.RootGameObject;

				foreach (GameObject selected in selectedObjects)
				{
					if (selected == rootGO)
					{
						continue;
					}

					InternalAddInputHDAAtEnd(selected);
				}

				RequiresUpload = true;

				if (HEU_PluginSettings.CookingEnabled && ParentAsset.AutoCookOnParameterChange)
				{
					ParentAsset.RequestCook(bCheckParametersChanged: true, bAsync: true, bSkipCookCheck: false, bUploadParameters: true);
				}
			}
		}
	}

	// Container for each input object in this node
	[System.Serializable]
	public class HEU_InputObjectInfo
	{
		// Gameobject containing mesh
		public GameObject _gameObject;

		// The last upload transform, for diff checks
		public Matrix4x4 _syncdTransform = Matrix4x4.identity;

		// Whether to use the transform offset
		[FormerlySerializedAs("_useTransformOverride")]
		public bool _useTransformOffset = false;

		// Transform offset
		[FormerlySerializedAs("_translateOverride")]
		public Vector3 _translateOffset = Vector3.zero;

		[FormerlySerializedAs("_rotateOverride")]
		public Vector3 _rotateOffset = Vector3.zero;

		[FormerlySerializedAs("_scaleOverride")]
		public Vector3 _scaleOffset = Vector3.one;

		public System.Type _inputInterfaceType;

		public void CopyTo(HEU_InputObjectInfo destObject)
		{
			destObject._gameObject = _gameObject;
			destObject._syncdTransform = _syncdTransform;
			destObject._useTransformOffset = _useTransformOffset;
			destObject._translateOffset = _translateOffset;
			destObject._rotateOffset = _rotateOffset;
			destObject._scaleOffset = _scaleOffset;
			destObject._inputInterfaceType = _inputInterfaceType;
		}
	}

	[System.Serializable]
	public class HEU_InputHDAInfo
	{
		// The HDA gameobject that needs to be connected
		public GameObject _pendingGO;

		// The HDA gameobject that has been connected
		public GameObject _connectedGO;

		// The ID of the connected HDA
		public HAPI_NodeId _connectedInputNodeID = HEU_Defines.HEU_INVALID_NODE_ID;

		public void CopyTo(HEU_InputHDAInfo destInfo)
		{
			destInfo._pendingGO = _pendingGO;
			destInfo._connectedGO = _connectedGO;

			destInfo._connectedInputNodeID = HEU_Defines.HEU_INVALID_NODE_ID;
		}
	}

	// UI cache container
	public class HEU_InputNodeUICache
	{
#if UNITY_EDITOR
		public UnityEditor.SerializedObject _inputNodeSerializedObject;

		public UnityEditor.SerializedProperty _inputObjectTypeProperty;

		public UnityEditor.SerializedProperty _keepWorldTransformProperty;
		public UnityEditor.SerializedProperty _packBeforeMergeProperty;

		public UnityEditor.SerializedProperty _inputObjectsProperty;

		public UnityEditor.SerializedProperty _inputAssetsProperty;
#endif

		public class HEU_InputObjectUICache
		{
#if UNITY_EDITOR
			public UnityEditor.SerializedProperty _gameObjectProperty;
			public UnityEditor.SerializedProperty _transformOffsetProperty;
			public UnityEditor.SerializedProperty _translateProperty;
			public UnityEditor.SerializedProperty _rotateProperty;
			public UnityEditor.SerializedProperty _scaleProperty;
#endif
		}

		public List<HEU_InputObjectUICache> _inputObjectCache = new List<HEU_InputObjectUICache>();

		public class HEU_InputAssetUICache
		{
#if UNITY_EDITOR
			public UnityEditor.SerializedProperty _gameObjectProperty;
#endif
		}

		public List<HEU_InputAssetUICache> _inputAssetCache = new List<HEU_InputAssetUICache>();
	}

}   // HoudiniEngineUnity