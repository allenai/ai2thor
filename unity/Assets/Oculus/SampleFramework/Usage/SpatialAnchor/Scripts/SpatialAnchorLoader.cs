// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Demonstrates loading existing spatial anchors from storage.
/// </summary>
/// <remarks>
/// Loading existing anchors involves two asynchronous methods:
/// 1. Call <see cref="OVRPlugin.QuerySpaces"/>
/// 2. Subscribe to the completion callback <see cref="OVRManager.SpaceQueryComplete"/>.
/// 3. For each result, attempt to make the space locatable (this means it has a pose in the world) by calling <see cref="OVRPlugin.SetSpaceComponentStatus"/>.
/// 4. Subscribe to the completion callback <see cref="OVRManager.SpaceSetComponentStatusComplete"/>.
/// 5. Once locatable, instantiate a GameObject or perform other logic specific to a spatial anchor.
/// </remarks>
public class SpatialAnchorLoader : MonoBehaviour
{
	[SerializeField]
	OVRSpatialAnchor _anchorPrefab;

	private readonly HashSet<ulong> _locateAnchorRequest = new HashSet<ulong>();

	private ulong _queryRequestId;

	private void OnEnable()
	{
		// Bind Spatial Anchor API callbacks
		OVRManager.SpaceQueryComplete += QueryComplete;
		OVRManager.SpaceSetComponentStatusComplete += SetComponentEnabled;
	}

	private void OnDisable()
	{
		// Unbind Spatial Anchor API callbacks
		OVRManager.SpaceQueryComplete -= QueryComplete;
		OVRManager.SpaceSetComponentStatusComplete -= SetComponentEnabled;
	}

	// ComponentEnabled callback
	private void SetComponentEnabled(UInt64 requestId, bool result, OVRSpace space, Guid uuid,
		OVRPlugin.SpaceComponentType componentType, bool enabled)
	{
		if (!_locateAnchorRequest.Remove(requestId)) return;

		if (!result)
		{
			Log($"[{requestId}] [{uuid}] Failed to make spatial anchor locatable.");
			return;
		}

		var spatialAnchor = Instantiate(_anchorPrefab);
		spatialAnchor.InitializeFromExisting(space, uuid);

		var anchor = spatialAnchor.GetComponent<Anchor>();
		if (anchor)
		{
			// We just loaded it, so we know it exists in persistent storage.
			anchor.ShowSaveIcon = true;
		}
	}

	// Invoked in response to OVRPlugin.QuerySpaces after the query completes.
	private void QueryComplete(UInt64 requestId, bool result)
	{
		if (requestId != _queryRequestId) return;

		_queryRequestId = 0;

		Log("QueryComplete requestId: " + requestId + " result: " + result);
		if (!result) return;

		if (!OVRPlugin.RetrieveSpaceQueryResults(requestId, out OVRPlugin.SpaceQueryResult[] results))
		{
			Log("RetrieveSpaceQueryResults failed for requestId: " + requestId);
			return;
		}

		foreach (OVRPlugin.SpaceQueryResult res in results)
		{
			Log("Setting components for retrieved Spatial Anchor with uuid: " + res.uuid.ToString("D"));
			TryEnableComponent(res.space, OVRPlugin.SpaceComponentType.Locatable);
			TryEnableComponent(res.space, OVRPlugin.SpaceComponentType.Storable);
		}
	}

	// Enables specified component if not already enabled
	private void TryEnableComponent(ulong anchorHandle, OVRPlugin.SpaceComponentType type)
	{
		if (!OVRPlugin.GetSpaceComponentStatus(anchorHandle, type, out var enabled, out _))
		{
			Log("WARNING GetSpaceComponentStatus did not complete successfully");
		}

		if (enabled)
		{
			// This means it is probably already loaded.
			Log("Component of type: " + type + " already enabled for anchorHandle: " + anchorHandle);
		}
		else
		{
			OVRPlugin.SetSpaceComponentStatus(anchorHandle, type, true, 0, out var requestId);
			Log("Enabling component for anchorHandle: " + anchorHandle + " type: " + type + " requestId " + requestId);

			if (type == OVRPlugin.SpaceComponentType.Locatable)
			{
				// Track it so that we know when to instantiate it
				_locateAnchorRequest.Add(requestId);
			}
		}
	}

	public void LoadAnchorsByUuid()
	{
		if (_queryRequestId != 0)
			throw new InvalidOperationException($"Existing request must complete before beginning a new one.");

		// Get number of saved anchor uuids
		if (!PlayerPrefs.HasKey(Anchor.NumUuidsPlayerPref))
		{
			PlayerPrefs.SetInt(Anchor.NumUuidsPlayerPref, 0);
		}
		int playerNumUuids = PlayerPrefs.GetInt("numUuids");
		Log("numUuids to query with: " + Anchor.NumUuidsPlayerPref + " uuids ");
		if (playerNumUuids == 0)
			return;

		var uuidArr = new Guid[playerNumUuids];
		for (int i = 0; i < playerNumUuids; ++i)
		{
			string uuidKey = "uuid" + i;
			string currentUuid = PlayerPrefs.GetString(uuidKey);
			Log("QueryAnchorByUuid: " + currentUuid);

			uuidArr[i] = new Guid(currentUuid);
		}

		var uuidInfo = new OVRPlugin.SpaceFilterInfoIds
		{
			NumIds = playerNumUuids,
			Ids = uuidArr
		};

		var queryInfo = new OVRPlugin.SpaceQueryInfo
		{
			QueryType = OVRPlugin.SpaceQueryType.Action,
			MaxQuerySpaces = 20,
			Timeout = 0,
			Location = OVRPlugin.SpaceStorageLocation.Local,
			ActionType = OVRPlugin.SpaceQueryActionType.Load,
			FilterType = OVRPlugin.SpaceQueryFilterType.Ids,
			IdInfo = uuidInfo
		};

		if (!OVRPlugin.QuerySpaces(queryInfo, out _queryRequestId))
		{
			Log("OVRPlugin.QuerySpaces failed");
		}
	}

	public void LoadAllLocalAnchors()
	{
		if (_queryRequestId != 0)
			throw new InvalidOperationException($"Existing request must complete before beginning a new one.");

		Log("QueryAllLocalAnchors called");
		var queryInfo = new OVRPlugin.SpaceQueryInfo()
		{
			QueryType = OVRPlugin.SpaceQueryType.Action,
			MaxQuerySpaces = 20,
			Timeout = 0,
			Location = OVRPlugin.SpaceStorageLocation.Local,
			ActionType = OVRPlugin.SpaceQueryActionType.Load,
			FilterType = OVRPlugin.SpaceQueryFilterType.None,
		};

		if (!OVRPlugin.QuerySpaces(queryInfo, out _queryRequestId))
		{
			Log("OVRPlugin.QuerySpaces failed");
		}
	}

	private static void Log(string message) => Debug.Log($"[SpatialAnchorsUnity]: {message}");
}
