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
using System.Text;
using UnityEngine;

namespace HoudiniEngineUnity
{
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Typedefs (copy these from HEU_Common.cs)
	using HAPI_NodeId = System.Int32;
	using HAPI_PartId = System.Int32;
	using HAPI_NodeTypeBits = System.Int32;
	using HAPI_NodeFlagsBits = System.Int32;


	/// <summary>
	/// Represents the Houdini Object node.
	/// Holds and manages geo nodes.
	/// </summary>
	public class HEU_ObjectNode : ScriptableObject
	{
		//  DATA ------------------------------------------------------------------------------------------------------

		public HAPI_NodeId ObjectID { get { return _objectInfo.nodeId; } }

		[SerializeField]
		private string _objName;
		public string ObjectName { get { return _objName; } }

		[SerializeField]
		private HEU_HoudiniAsset _parentAsset;
		public HEU_HoudiniAsset ParentAsset { get { return _parentAsset; } }

		[SerializeField]
		private HAPI_ObjectInfo _objectInfo;

		[SerializeField]
		private List<HEU_GeoNode> _geoNodes;

		[SerializeField]
		public HAPI_Transform _objectTransform;

		public bool IsInstanced() { return _objectInfo.isInstanced; }

		public bool IsVisible() { return _objectInfo.isVisible; }


		//  LOGIC -----------------------------------------------------------------------------------------------------

		public HEU_ObjectNode()
		{
			Reset();
		}

		public void Reset()
		{
			_objName = "";

			_parentAsset = null;
			_objectInfo = new HAPI_ObjectInfo();
			_geoNodes = new List<HEU_GeoNode>();
			_objectTransform = new HAPI_Transform(true);
		}

		private void SyncWithObjectInfo(HEU_SessionBase session)
		{
			_objName = HEU_SessionManager.GetString(_objectInfo.nameSH, session);
		}

		public void Initialize(HEU_SessionBase session, HAPI_ObjectInfo objectInfo, HAPI_Transform objectTranform, HEU_HoudiniAsset parentAsset)
		{
			_objectInfo = objectInfo;
			_objectTransform = objectTranform;
			_parentAsset = parentAsset;

			SyncWithObjectInfo(session);

			// Translate transform to Unity (TODO)

			List<HAPI_GeoInfo> geoInfos = new List<HAPI_GeoInfo>();

			// Get display geo info
			HAPI_GeoInfo displayGeoInfo = new HAPI_GeoInfo();
			if(!session.GetDisplayGeoInfo(_objectInfo.nodeId, ref displayGeoInfo))
			{
				return;
			}
			//Debug.LogFormat("Found geoinfo with name {0} and id {1}", HEU_SessionManager.GetString(displayGeoInfo.nameSH, session), displayGeoInfo.nodeId);
			geoInfos.Add(displayGeoInfo);
			
			// Get editable nodes, cook em, then create geo nodes for them
			HAPI_NodeId[] editableNodes = null;
			HEU_SessionManager.GetComposedChildNodeList(session, _objectInfo.nodeId, (int)HAPI_NodeType.HAPI_NODETYPE_SOP, (int)HAPI_NodeFlags.HAPI_NODEFLAGS_EDITABLE, true, out editableNodes); 
			if(editableNodes != null)
			{
				foreach(HAPI_NodeId editNodeID in editableNodes)
				{
					if (editNodeID != displayGeoInfo.nodeId)
					{
						session.CookNode(editNodeID, HEU_PluginSettings.CookTemplatedGeos);

						HAPI_GeoInfo editGeoInfo = new HAPI_GeoInfo();
						if (session.GetGeoInfo(editNodeID, ref editGeoInfo))
						{
							geoInfos.Add(editGeoInfo);
						}
					}
				}
			}
			
			//Debug.LogFormat("Object id={5}, name={0}, isInstancer={1}, isInstanced={2}, instancePath={3}, instanceId={4}", 
			//	HEU_SessionManager.GetString(objectInfo.nameSH, session), objectInfo.isInstancer, objectInfo.isInstanced, 
			//	HEU_SessionManager.GetString(objectInfo.objectInstancePathSH, session), objectInfo.objectToInstanceId, objectInfo.nodeId);

			// Go through geo infos to create geometry
			int numGeoInfos = geoInfos.Count;
			for(int i = 0; i < numGeoInfos; ++i)
			{
				// Create GeoNode for each
				_geoNodes.Add(CreateGeoNode(session, geoInfos[i]));
			}

			// This has been moved to GenerateGeometry but kept here just in case.
			//ApplyObjectTransformToGeoNodes();
		}

		/// <summary>
		/// Destroy all data.
		/// </summary>
		public void DestroyAllData()
		{
			if(_geoNodes != null)
			{
				for(int i = 0; i < _geoNodes.Count; ++i)
				{
					_geoNodes[i].DestroyAllData();
					HEU_GeneralUtility.DestroyImmediate(_geoNodes[i]);
				}
				_geoNodes.Clear();
			}
		}

		private HEU_GeoNode CreateGeoNode(HEU_SessionBase session, HAPI_GeoInfo geoInfo)
		{
			HEU_GeoNode geoNode = ScriptableObject.CreateInstance<HEU_GeoNode>();
			geoNode.Initialize(session, geoInfo, this);
			geoNode.UpdateGeo(session);
			return geoNode;
		}

		/// <summary>
		/// Get debug info for this object
		/// </summary>
		public void GetDebugInfo(StringBuilder sb)
		{
			int numGeos = _geoNodes != null ? _geoNodes.Count : 0;

			sb.AppendFormat("ObjectID: {0}, Name: {1}, Geos: {2}, Parent: {3}\n", ObjectID, ObjectName, numGeos, _parentAsset);

			if (_geoNodes != null)
			{
				foreach (HEU_GeoNode geo in _geoNodes)
				{
					geo.GetDebugInfo(sb);
				}
			}
		}

		public void SetObjectInfo(HAPI_ObjectInfo newObjectInfo)
		{
			_objectInfo = newObjectInfo;
		}

		/// <summary>
		/// Retrieves object info from Houdini session and updates internal state.
		/// New geo nodes are created, unused geo nodes are destroyed.
		/// Geo nodes are then refreshed to be in sync with Houdini session.
		/// </summary>
		/// <returns>True if internal state has changed (including geometry).</returns>
		public void UpdateObject(HEU_SessionBase session, bool bForceUpdate)
		{
			// Update the geo info
			if (!session.GetObjectInfo(ObjectID, ref _objectInfo))
			{
				return;
			}
			SyncWithObjectInfo(session);

			// Update the object transform
			_objectTransform = ParentAsset.GetObjectTransform(session, ObjectID);

			// Container for existing geo nodes that are still in use
			List<HEU_GeoNode> geoNodesToKeep = new List<HEU_GeoNode>();
			
			// Container for new geo infos that need to be created
			List<HAPI_GeoInfo> newGeoInfosToCreate = new List<HAPI_GeoInfo>();

			if (_objectInfo.haveGeosChanged || bForceUpdate)
			{
				// Indicates that the geometry nodes have changed
				//Debug.Log("Geos have changed!");

				// Form a list of geo infos that are now present after cooking
				List<HAPI_GeoInfo> postCookGeoInfos = new List<HAPI_GeoInfo>();

				// Get the display geo info
				HAPI_GeoInfo displayGeoInfo = new HAPI_GeoInfo();
				if (session.GetDisplayGeoInfo(_objectInfo.nodeId, ref displayGeoInfo, false))
				{
					postCookGeoInfos.Add(displayGeoInfo);
				}
				else
				{
					displayGeoInfo.nodeId = HEU_Defines.HEU_INVALID_NODE_ID;
				}

				// Get editable nodes, cook em, then create geo nodes for them
				HAPI_NodeId[] editableNodes = null;
				HEU_SessionManager.GetComposedChildNodeList(session, _objectInfo.nodeId, (int)HAPI_NodeType.HAPI_NODETYPE_SOP, (int)HAPI_NodeFlags.HAPI_NODEFLAGS_EDITABLE, true, out editableNodes);
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
								postCookGeoInfos.Add(editGeoInfo);
							}
						}
					}
				}
				
				// Now for each geo node that are present after cooking, we check if its
				// new or whether we already have it prior to cooking.
				int numPostCookGeoInfos = postCookGeoInfos.Count;
				for (int i = 0; i < numPostCookGeoInfos; i++)
				{
					bool bFound = false;
					for (int j = 0; j < _geoNodes.Count; j++)
					{
						string geoName = HEU_SessionManager.GetString(postCookGeoInfos[i].nameSH, session);
						if(geoName.Equals(_geoNodes[j].GeoName))
						{
							_geoNodes[j].SetGeoInfo(postCookGeoInfos[i]);

							geoNodesToKeep.Add(_geoNodes[j]);
							_geoNodes.RemoveAt(j);

							bFound = true;
							break;
						}
					}

					if (!bFound)
					{
						newGeoInfosToCreate.Add(postCookGeoInfos[i]);
					}
				}

				// Whatever is left in _geoNodes is no longer needed so clean up
				int numCurrentGeos = _geoNodes.Count;
				for(int i = 0; i < numCurrentGeos; ++i)
				{
					_geoNodes[i].DestroyAllData();
				}
			}
			else
			{
				Debug.Assert(_objectInfo.geoCount == _geoNodes.Count, "Expected same number of geometry nodes.");
			}
			
			// Go through the old geo nodes that are still in use and update if necessary.
			foreach (HEU_GeoNode geoNode in geoNodesToKeep)
			{
				// Get geo info and check if geo changed
				bool bGeoChanged = bForceUpdate || geoNode.HasGeoNodeChanged(session);
				if(bGeoChanged)
				{
					geoNode.UpdateGeo(session);
				}
				else
				{
					if (_objectInfo.haveGeosChanged)
					{
						// Clear object instances since the object info has changed.
						// Without this, the object instances were never getting updated
						// if only the inputs changed but not outputs (of instancers).
						geoNode.ClearObjectInstances();
					}

					// Visiblity might have changed, so update that
					geoNode.CalculateVisiblity(IsVisible());
					geoNode.CalculateColliderState();
				}
			}

			// Create the new geo infos and add to our keep list
			foreach (HAPI_GeoInfo newGeoInfo in newGeoInfosToCreate)
			{
				geoNodesToKeep.Add(CreateGeoNode(session, newGeoInfo));
			}

			// Overwrite the old list with new
			_geoNodes = geoNodesToKeep;

			// Updating the trasform is done in GenerateGeometry
		}

		public void GenerateGeometry(HEU_SessionBase session, bool bRebuild)
		{
			// Volumes could come in as a geonode + part for each heightfield layer.
			// Otherwise the other geo types can be done individually.

			bool bResult = false;

			List<HEU_PartData> meshParts = new List<HEU_PartData>();
			List<HEU_PartData> volumeParts = new List<HEU_PartData>();

			List<HEU_PartData> partsToDestroy = new List<HEU_PartData>();

			HEU_HoudiniAsset parentAsset = ParentAsset;

			foreach (HEU_GeoNode geoNode in _geoNodes)
			{
				geoNode.GetPartsByOutputType(meshParts, volumeParts);

				if(volumeParts.Count > 0)
				{
					// Volumes
					// Each layer in the volume is retrieved as a volume part, in the display geo node. 
					// But we need to handle all layers as 1 terrain output in Unity, with 1 height layer and 
					// other layers as alphamaps.
					geoNode.ProcessVolumeParts(session, volumeParts, bRebuild);

					// Clear the volume parts after processing since we are done with this set
					volumeParts.Clear();
				}
			}

			// Meshes
			foreach (HEU_PartData part in meshParts)
			{
				// This returns false when there is no valid geometry or is not instancing. Should remove it as otherwise
				// stale data sticks around on recook
				bResult = part.GenerateMesh(session, parentAsset.GenerateUVs, parentAsset.GenerateTangents, parentAsset.GenerateNormals, parentAsset.UseLODGroups);
				if (!bResult)
				{
					partsToDestroy.Add(part);
				}
			}

			int numPartsToDestroy = partsToDestroy.Count;
			for(int i = 0; i < numPartsToDestroy; ++i)
			{
				HEU_GeoNode parentNode = partsToDestroy[i].ParentGeoNode;
				if (parentNode != null)
				{
					parentNode.RemoveAndDestroyPart(partsToDestroy[i]);
				}
				else
				{
					HEU_PartData.DestroyPart(partsToDestroy[i]);
				}
			}
			partsToDestroy.Clear();

			ApplyObjectTransformToGeoNodes();

			// Set visibility and attribute-based tag, layer, and scripts
			bool bIsVisible = IsVisible();
			foreach (HEU_GeoNode geoNode in _geoNodes)
			{
				geoNode.CalculateVisiblity(bIsVisible);
				geoNode.CalculateColliderState();

				geoNode.SetAttributeModifiersOnPartOutputs(session);
			}

			// Create editable attributes.
			// This should happen after visibility has been calculated above
			// since we need to show/hide the intermediate geometry during painting.
			foreach (HEU_PartData part in meshParts)
			{
				if (part.ParentGeoNode.IsIntermediateOrEditable())
				{
					part.SetupAttributeGeometry(session);
				}
			}
		}

		public void GeneratePartInstances(HEU_SessionBase session)
		{
			foreach (HEU_GeoNode geoNode in _geoNodes)
			{
				geoNode.GeneratePartInstances(session);
			}
		}

		/// <summary>
		/// Apply this object's transform to all its geo nodes.
		/// </summary>
		public void ApplyObjectTransformToGeoNodes()
		{
			foreach(HEU_GeoNode geoNode in _geoNodes)
			{
				geoNode.ApplyHAPITransform(ref _objectTransform);
			}
		}

		/// <summary>
		/// Returns true if this object is using the given material.
		/// </summary>
		/// <param name="materialData">Material data containing the material to check</param>
		/// <returns>True if this object is using the given material</returns>
		public bool IsUsingMaterial(HEU_MaterialData materialData)
		{
			foreach (HEU_GeoNode geoNode in _geoNodes)
			{
				if(geoNode.IsUsingMaterial(materialData))
				{
					return true;
				}
			}
			return false;
		}

		public void GetClonableParts(List<HEU_PartData> clonableParts)
		{
			foreach (HEU_GeoNode geoNode in _geoNodes)
			{
				if (geoNode.Displayable)
				{
					geoNode.GetClonableParts(clonableParts);
				}
			}
		}

		/// <summary>
		/// Adds gameobjects that were output from this object.
		/// </summary>
		/// <param name="outputObjects">List to add to</param>
		public void GetOutputGameObjects(List<GameObject> outputObjects)
		{
			foreach(HEU_GeoNode geoNode in _geoNodes)
			{
				// TODO: check if geoNode.Displayable? elmininates editable nodes
				geoNode.GetOutputGameObjects(outputObjects);
			}
		}

		/// <summary>
		/// Adds this node's HEU_GeneratedOutput to given outputs list.
		/// </summary>
		/// <param name="outputs">List to add to</param>
		public void GetOutput(List<HEU_GeneratedOutput> outputs)
		{
			foreach (HEU_GeoNode geoNode in _geoNodes)
			{
				geoNode.GetOutput(outputs);
			}
		}

		/// <summary>
		/// Returns the HEU_PartData with the given output gameobject.
		/// </summary>
		/// <param name="outputGameObject">The output gameobject to check</param>
		/// <returns>Valid HEU_PartData or null if no match</returns>
		public HEU_PartData GetHDAPartWithGameObject(GameObject outputGameObject)
		{
			HEU_PartData foundPart = null;
			foreach (HEU_GeoNode geoNode in _geoNodes)
			{
				foundPart = geoNode.GetHDAPartWithGameObject(outputGameObject);
				if (foundPart != null)
				{
					return foundPart;
				}
			}
			
			return null;
		}

		public HEU_GeoNode GetGeoNode(string geoName)
		{
			foreach (HEU_GeoNode geoNode in _geoNodes)
			{
				if (geoNode.GeoName.Equals(geoName))
				{
					return geoNode;
				}
			}
			return null;
		}

		public void GetCurves(List<HEU_Curve> curves, bool bEditableOnly)
		{
			foreach (HEU_GeoNode geoNode in _geoNodes)
			{
				geoNode.GetCurves(curves, bEditableOnly);
			}
		}

		public void GetOutputGeoNodes(List<HEU_GeoNode> outGeoNodes)
		{
			foreach (HEU_GeoNode geoNode in _geoNodes)
			{
				if (geoNode.Displayable)
				{
					outGeoNodes.Add(geoNode);
				}
			}
		}

		/// <summary>
		/// Generates object instances.
		/// Skips parts that already have their instances generated.
		/// </summary>
		/// <param name="session">Active session to use</param>
		public void GenerateObjectInstances(HEU_SessionBase session)
		{
			if (!IsInstancer())
			{
				Debug.LogErrorFormat("Generate object instances called on a non-instancer object {0} for asset {1}!", ObjectName, ParentAsset.AssetName);
				return;
			}

			//Debug.LogFormat("Generate Object Instances:: id={5}, name={0}, isInstancer={1}, isInstanced={2}, instancePath={3}, instanceId={4}", HEU_SessionManager.GetString(_objectInfo.nameSH, session), 
			//	_objectInfo.isInstancer, _objectInfo.isInstanced, HEU_SessionManager.GetString(_objectInfo.objectInstancePathSH, session), _objectInfo.objectToInstanceId, _objectInfo.nodeId);

			// Is this a Houdini attribute instancer?
			string instanceAttrName = HEU_PluginSettings.InstanceAttr;
			string unityInstanceAttrName = HEU_PluginSettings.UnityInstanceAttr;
			string instancePrefixAttrName = HEU_Defines.DEFAULT_INSTANCE_PREFIX_ATTR;

			HAPI_AttributeInfo instanceAttrInfo = new HAPI_AttributeInfo();
			HAPI_AttributeInfo unityInstanceAttrInfo = new HAPI_AttributeInfo();
			HAPI_AttributeInfo instancePrefixAttrInfo = new HAPI_AttributeInfo();

			int numGeos = _geoNodes.Count;
			for(int i = 0; i < numGeos; ++i)
			{
				if(_geoNodes[i].Displayable)
				{
					List<HEU_PartData> parts = _geoNodes[i].GetParts();
					int numParts = parts.Count;
					for(int j = 0; j < numParts; ++j)
					{
						if(parts[j].ObjectInstancesBeenGenerated || parts[j].IsPartVolume())
						{
							// This prevents instances being created unnecessarily (e.g. part hasn't changed since last cook).
							// Or for volumes that might have instance attributes.
							continue;
						}

						HEU_GeneralUtility.GetAttributeInfo(session, _geoNodes[i].GeoID, parts[j].PartID, instanceAttrName, ref instanceAttrInfo);
						HEU_GeneralUtility.GetAttributeInfo(session, _geoNodes[i].GeoID, parts[j].PartID, unityInstanceAttrName, ref unityInstanceAttrInfo);

						string[] instancePrefixes = null;
						HEU_GeneralUtility.GetAttributeInfo(session, _geoNodes[i].GeoID, parts[j].PartID, instancePrefixAttrName, ref instancePrefixAttrInfo);
						if(instancePrefixAttrInfo.exists)
						{
							instancePrefixes = HEU_GeneralUtility.GetAttributeStringData(session, _geoNodes[i].GeoID, parts[j].PartID, instancePrefixAttrName, ref instancePrefixAttrInfo);
						}

						// Must clear out instances, as otherwise we get duplicates
						parts[j].ClearInstances();

						// Clear out invalid object instance infos that no longer have any valid parts
						parts[j].ClearInvalidObjectInstanceInfos();

						if (instanceAttrInfo.exists)
						{
							// Object instancing via Houdini instance attribute

							parts[j].GenerateInstancesFromObjectIds(session, instancePrefixes);
						}
						else if (unityInstanceAttrInfo.exists)
						{
							// Object instancing via existing Unity object (path from point attribute)

							// Attribute owner type determines whether to use single instanced object (detail) or multiple (point)
							if (unityInstanceAttrInfo.owner == HAPI_AttributeOwner.HAPI_ATTROWNER_POINT)
							{
								parts[j].GenerateInstancesFromUnityAssetPathAttribute(session, unityInstanceAttrName);
							}
							else if(unityInstanceAttrInfo.owner == HAPI_AttributeOwner.HAPI_ATTROWNER_DETAIL)
							{
								bool bInstanced = false;
								int[] scriptAttr = new int[unityInstanceAttrInfo.count];
								HEU_GeneralUtility.GetAttribute(session, _geoNodes[i].GeoID, parts[j].PartID, unityInstanceAttrName, ref unityInstanceAttrInfo, ref scriptAttr, session.GetAttributeStringData);
								if (unityInstanceAttrInfo.exists)
								{
									string assetPath = HEU_SessionManager.GetString(scriptAttr[0]);
									if (!string.IsNullOrEmpty(assetPath))
									{
										parts[j].GenerateInstancesFromUnityAssetPath(session, assetPath, instancePrefixes);
										bInstanced = true;
									}
								}

								if (!bInstanced)
								{
									Debug.LogWarningFormat("Unable to get instanced object path from detail instance attribute!");
								}
							}
							else
							{
								// Other attribute owned types are unsupported.
								// Originally had a warning here, but unnecessary as in some cases (e.g. heightfield attrbiutes) the
								// attribute owner could be changed in HAPI.
							}
						}
						else
						{
							// Standard object instancing via single Houdini object

							if (_objectInfo.objectToInstanceId == HEU_Defines.HEU_INVALID_NODE_ID)
							{
								Debug.LogAssertionFormat("Invalid object ID {0} used for object instancing. " 
									+ "Make sure to turn on Full point instancing and set the correct Instance Object.", _objectInfo.objectToInstanceId);
								continue;
							}

							parts[j].GenerateInstancesFromObjectID(session, _objectInfo.objectToInstanceId, instancePrefixes);
						}
					}
				}
			}
		}

		/// <summary>
		/// Fill in the objInstanceInfos list with the HEU_ObjectInstanceInfos used by this object.
		/// </summary>
		/// <param name="objInstanceInfos">List to fill in</param>
		public void PopulateObjectInstanceInfos(List<HEU_ObjectInstanceInfo> objInstanceInfos)
		{
			if(IsInstancer())
			{
				int numGeos = _geoNodes.Count;
				for (int i = 0; i < numGeos; ++i)
				{
					if (_geoNodes[i].Displayable)
					{
						List<HEU_PartData> parts = _geoNodes[i].GetParts();
						int numParts = parts.Count;
						for (int j = 0; j < numParts; ++j)
						{
							parts[i].PopulateObjectInstanceInfos(objInstanceInfos);
						}
					}
				}
			}
		}

		/// <summary>
		/// Process custom attribute with Unity script name, and attach any scripts found.
		/// </summary>
		/// <param name="session">Session to use</param>
		public void ProcessUnityScriptAttributes(HEU_SessionBase session)
		{
			foreach (HEU_GeoNode geoNode in _geoNodes)
			{
				geoNode.ProcessUnityScriptAttribute(session);
			}
		}

		/// <summary>
		/// Hide all geometry contained within
		/// </summary>
		public void HideAllGeometry()
		{
			foreach (HEU_GeoNode geoNode in _geoNodes)
			{
				geoNode.HideAllGeometry();
			}
		}

		/// <summary>
		/// Calculate visiblity of all geometry within
		/// </summary>
		public void CalculateVisibility()
		{
			foreach (HEU_GeoNode geoNode in _geoNodes)
			{
				geoNode.CalculateVisiblity(IsVisible());
			}
		}

		public void CalculateColliderState()
		{
			foreach (HEU_GeoNode geoNode in _geoNodes)
			{
				geoNode.CalculateColliderState();
			}
		}

		public void DisableAllColliders()
		{
			foreach (HEU_GeoNode geoNode in _geoNodes)
			{
				geoNode.DisableAllColliders();
			}
		}

		/// <summary>
		/// Returns true if this is an object instancer, or if it has point (attribute) instancer parts.
		/// </summary>
		/// <returns></returns>
		public bool IsInstancer()
		{
			if (_objectInfo.isInstancer)
			{
				return true;
			}
			else
			{
				// Check parts for atrrib instancing
				foreach (HEU_GeoNode geoNode in _geoNodes)
				{
					if (geoNode.HasAttribInstancer())
					{
						return true;
					}
				}
			}
			return false;
		}

		public override string ToString()
		{
			return (!string.IsNullOrEmpty(_objName) ? ("ObjectNode: " + _objName) : base.ToString());
		}
	}

}   // HoudiniEngineUnity