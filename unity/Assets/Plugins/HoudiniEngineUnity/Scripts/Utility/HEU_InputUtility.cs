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

namespace HoudiniEngineUnity
{
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Typedefs (copy these from HEU_Common.cs)
	using HAPI_NodeId = System.Int32;

	/// <summary>
	/// Utility class for managing input interfaces and uploading input data.
	/// HEU_InputInterface-derived classes should register with this class
	/// so that they will be considered for uploading input gameobjects.
	/// When an input object needs to be uploaded, this uses the registered interface
	/// with the highest prioerity that can upload that object.
	/// The plugins set of input interfaces (Mesh, Terrain) are registered with this class
	/// and can be overriden by adding input interfaces that can also upload those object types.
	/// </summary>
	public static class HEU_InputUtility
	{
		// List of registered interfaces oredered by priority (low to high)
		private static List<HEU_InputInterface> _inputInterfaces = new List<HEU_InputInterface>();

		/// <summary>
		/// Return the highest priority in the registered interface list.
		/// Use the returned value + 1 to set a higher priority interface.
		/// </summary>
		/// <returns>Integer representing highest priority in the registered interface list</returns>
		public static int GetHighestPriority()
		{
			return _inputInterfaces.Count > 0 ? _inputInterfaces[_inputInterfaces.Count - 1].Priority : 0;
		}

		/// <summary>
		/// Add the given inputInterface to the registered interface list based on its priority.
		/// </summary>
		/// <param name="inputInterface">Interface to register</param>
		public static void RegisterInputInterface(HEU_InputInterface inputInterface)
		{
			System.Type inputType = inputInterface.GetType();

			if (GetInputInterfaceByType(inputType) != null)
			{
				return;
			}

			if (!_inputInterfaces.Contains(inputInterface))
			{
				// Add to ordered list based on priority
				int numInterfaces = _inputInterfaces.Count;
				if (numInterfaces > 0)
				{
					for (int i = numInterfaces - 1; i >= 0; i--)
					{
						if (_inputInterfaces[i] != null && _inputInterfaces[i].Priority <= inputInterface.Priority)
						{
							_inputInterfaces.Add(inputInterface);
							//Debug.LogFormat("Registered {0} at {1}. Total of {2}", inputInterface.GetType(), i, _inputInterfaces.Count);
							break;
						}
					}
				}
				else
				{
					_inputInterfaces.Add(inputInterface);
					//Debug.LogFormat("Registered {0} at {1}. Total of {2}", inputInterface.GetType(), _inputInterfaces.Count - 1, _inputInterfaces.Count);
				}
			}
		}

		/// <summary>
		/// Remove the given input interface from list of registered interfaces.
		/// </summary>
		/// <param name="inputInterface">The input interface to unregister</param>
		public static void UnregisterInputInterface(HEU_InputInterface inputInterface)
		{
			_inputInterfaces.Remove(inputInterface);
		}

		/// <summary>
		/// Return the input interface with the given type.
		/// Searches from highest priority to lowest.
		/// </summary>
		/// <param name="type">Type of interface to find</param>
		/// <returns>Found input interface or null</returns>
		public static HEU_InputInterface GetInputInterfaceByType(System.Type type)
		{
			int numInterfaces = _inputInterfaces.Count;
			for (int i = numInterfaces - 1; i >= 0; i--)
			{
				if (_inputInterfaces[i].GetType() == type)
				{
					return _inputInterfaces[i];
				}
			}

			return null;
		}

		/// <summary>
		/// Returns the input interface that can upload the given inputObject
		/// with the highest priority.
		/// </summary>
		/// <param name="inputObject">The gameobject to check if the interfaces can upload it</param>
		/// <returns>Compatible input interface or null</returns>
		public static HEU_InputInterface GetInputInterface(GameObject inputObject)
		{
			// List is in order of increasing priority so traverse backwards to get
			// the highest priority inteface for this object
			int numInterfaces = _inputInterfaces.Count;
			for (int i = numInterfaces - 1; i >= 0; i--)
			{
				if (_inputInterfaces[i].IsThisInputObjectSupported(inputObject))
				{
					return _inputInterfaces[i];
				}
			}
			return null;
		}

		/// <summary>
		/// Return the input interface that can upload the given inputObjectInfo's data.
		/// It checks the inputObjectInfo._inputInterfaceType to see if it was previously
		/// uploaded, and if so, uses the same interface.
		/// </summary>
		/// <param name="inputObjectInfo">Input object info used to find the interface</param>
		/// <returns>Compatible input interface or null</returns>
		public static HEU_InputInterface GetInputInterface(HEU_InputObjectInfo inputObjectInfo)
		{
			HEU_InputInterface inputInterface = null;
			if (inputObjectInfo._inputInterfaceType == null)
			{
				inputInterface = GetInputInterfaceByType(inputObjectInfo._inputInterfaceType);
			}
			if (inputInterface == null)
			{
				inputInterface = GetInputInterface(inputObjectInfo._gameObject);
				if(inputInterface != null)
				{
					inputObjectInfo._inputInterfaceType = inputInterface.GetType();
				}
			}
			return inputInterface;
		}

		/// <summary>
		/// Create an input node network and upload the given set of input objects.
		/// This creates a SOP/merge node, and input nodes for each object in inputObjects
		/// which are then connected to the merge node.
		/// It finds the input interface that supports each object in inputObjects for creating
		/// the input node and uploading the data based on the type of data.
		/// </summary>
		/// <param name="session">Session to create the input node in</param>
		/// <param name="assetID">Main asset ID</param>
		/// <param name="connectMergeID">Created SOP/merge node ID</param>
		/// <param name="inputObjects">List of input objects to upload</param>
		/// <param name="inputObjectsConnectedAssetIDs">List of input node IDs for the input nodes created</param>
		/// <param name="bKeepWorldTransform">Whether to use world transform for the input nodes</param>
		/// <returns>True if successfully uploading input nodes</returns>
		public static bool CreateInputNodeWithMultiObjects(HEU_SessionBase session, HAPI_NodeId assetID,
			ref HAPI_NodeId connectMergeID, ref List<HEU_InputObjectInfo> inputObjects, ref List<HAPI_NodeId> inputObjectsConnectedAssetIDs, bool bKeepWorldTransform)
		{
			// Create the merge SOP node that the input nodes are going to connect to.
			if (!session.CreateNode(-1, "SOP/merge", null, true, out connectMergeID))
			{
				Debug.LogErrorFormat("Unable to create merge SOP node for connecting input assets.");
				return false;
			}

			int numObjects = inputObjects.Count;
			for (int i = 0; i < numObjects; ++i)
			{
				HAPI_NodeId newConnectInputID = HEU_Defines.HEU_INVALID_NODE_ID;
				inputObjectsConnectedAssetIDs.Add(newConnectInputID);

				// Skipping null gameobjects. Though if this causes issues, can always let it continue
				// to create input node, but not upload mesh data
				if (inputObjects[i]._gameObject == null)
				{
					continue;
				}

				HEU_InputInterface inputInterface = GetInputInterface(inputObjects[i]);
				if (inputInterface == null)
				{
					Debug.LogWarningFormat("No input interface found for gameobject: {0}. Skipping upload!", inputObjects[i]._gameObject.name);
					continue;
				}

				bool bResult = inputInterface.CreateInputNodeWithDataUpload(session, connectMergeID, inputObjects[i]._gameObject, out newConnectInputID);
				if (!bResult || newConnectInputID == HEU_Defines.HEU_INVALID_NODE_ID)
				{
					Debug.LogError("Failed to upload input.");
					continue;
				}

				inputObjectsConnectedAssetIDs[i] = newConnectInputID;

				if (!session.ConnectNodeInput(connectMergeID, i, newConnectInputID))
				{
					Debug.LogErrorFormat("Unable to connect input nodes!");
					return false;
				}

				UploadInputObjectTransform(session, inputObjects[i], newConnectInputID, bKeepWorldTransform);
			}

			return true;
		}

		public static bool CreateInputNodeWithMultiAssets(HEU_SessionBase session, HEU_HoudiniAsset parentAsset,
			ref HAPI_NodeId connectMergeID, ref List<HEU_InputHDAInfo> inputAssetInfos,
			 bool bKeepWorldTransform, HAPI_NodeId mergeParentID = -1)
		{
			// Create the merge SOP node that the input nodes are going to connect to.
			if (!session.CreateNode(mergeParentID, "SOP/merge", null, true, out connectMergeID))
			{
				Debug.LogErrorFormat("Unable to create merge SOP node for connecting input assets.");
				return false;
			}

			int numInputs = inputAssetInfos.Count;
			for (int i = 0; i < numInputs; ++i)
			{
				inputAssetInfos[i]._connectedInputNodeID = HEU_Defines.HEU_INVALID_NODE_ID;

				if (inputAssetInfos[i]._pendingGO == null)
				{
					continue;
				}

				// ID of the asset that will be connected
				HAPI_NodeId inputAssetID = HEU_Defines.HEU_INVALID_NODE_ID;

				HEU_HoudiniAssetRoot inputAssetRoot = inputAssetInfos[i]._pendingGO.GetComponent<HEU_HoudiniAssetRoot>();
				if (inputAssetRoot != null && inputAssetRoot._houdiniAsset != null)
				{
					if (!inputAssetRoot._houdiniAsset.IsAssetValidInHoudini(session))
					{
						// Force a recook if its not valid (in case it hasn't been loaded into the session)
						inputAssetRoot._houdiniAsset.RequestCook(true, false, true, true);
					}

					inputAssetID = inputAssetRoot._houdiniAsset.AssetID;
				}

				if (inputAssetID == HEU_Defines.HEU_INVALID_NODE_ID)
				{	
					continue;
				}

				if (!session.ConnectNodeInput(connectMergeID, i, inputAssetID))
				{
					Debug.LogErrorFormat("Unable to connect input nodes!");
					return false;
				}

				inputAssetInfos[i]._connectedInputNodeID = inputAssetID;
				inputAssetInfos[i]._connectedGO = inputAssetInfos[i]._pendingGO;

				parentAsset.ConnectToUpstream(inputAssetRoot._houdiniAsset);
			}

			return true;
		}

		/// <summary>
		/// Set the input node's transform.
		/// </summary>
		/// <param name="session">Session that the input node exists in</param>
		/// <param name="inputObject">The input object info containing data about the input</param>
		/// <param name="inputNodeID">The input node ID</param>
		/// <param name="bKeepWorldTransform">Whether to use world transform or not</param>
		/// <returns></returns>
		public static bool UploadInputObjectTransform(HEU_SessionBase session, HEU_InputObjectInfo inputObject, HAPI_NodeId inputNodeID, bool bKeepWorldTransform)
		{
			Matrix4x4 inputTransform = Matrix4x4.identity;
			if (inputObject._useTransformOffset)
			{
				if (bKeepWorldTransform)
				{
					// Add offset tranform to world transform
					Transform inputObjTransform = inputObject._gameObject.transform;
					Vector3 position = inputObjTransform.position + inputObject._translateOffset;
					Quaternion rotation = inputObjTransform.rotation * Quaternion.Euler(inputObject._rotateOffset);
					Vector3 scale = Vector3.Scale(inputObjTransform.localScale, inputObject._scaleOffset);

					Vector3 rotVector = rotation.eulerAngles;
					inputTransform = HEU_HAPIUtility.GetMatrix4x4(ref position, ref rotVector, ref scale);
				}
				else
				{
					// Offset from origin.
					inputTransform = HEU_HAPIUtility.GetMatrix4x4(ref inputObject._translateOffset, ref inputObject._rotateOffset, ref inputObject._scaleOffset);
				}
			}
			else
			{
				inputTransform = inputObject._gameObject.transform.localToWorldMatrix;
			}

			HAPI_TransformEuler transformEuler = HEU_HAPIUtility.GetHAPITransformFromMatrix(ref inputTransform);

			HAPI_NodeInfo inputNodeInfo = new HAPI_NodeInfo();
			if (!session.GetNodeInfo(inputNodeID, ref inputNodeInfo))
			{
				return false;
			}

			if (session.SetObjectTransform(inputNodeInfo.parentId, ref transformEuler))
			{
				inputObject._syncdTransform = inputTransform;
			}

			return true;
		}
	}

}   // namespace HoudiniEngineUnity