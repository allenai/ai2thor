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
using System.Collections.Generic;
using System.Text;

namespace HoudiniEngineUnity
{
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Typedefs (copy these from HEU_Common.cs)
	using HAPI_NodeId = System.Int32;
	using HAPI_PartId = System.Int32;
	using HAPI_ParmId = System.Int32;
	using HAPI_StringHandle = System.Int32;


	/// <summary>
	/// Contains data and logic for curve node drawing and editing.
	/// </summary>
	public class HEU_Curve : ScriptableObject
	{
		// DATA -------------------------------------------------------------------------------------------------------

		[SerializeField]
		private HAPI_NodeId _geoID;

		public HAPI_NodeId GeoID { get { return _geoID; } }

		[SerializeField]
		private List<Vector3> _points = new List<Vector3>();

		[SerializeField]
		private Vector3[] _vertices;

		[SerializeField]
		private bool _isEditable;

		public bool IsEditable() { return _isEditable; }

		[SerializeField]
		private HEU_Parameters _parameters;

		public HEU_Parameters Parameters { get { return _parameters; } }

		[SerializeField]
		private bool _bUploadParameterPreset;

		public void SetUploadParameterPreset(bool bValue) { _bUploadParameterPreset = bValue; }

		[SerializeField]
		private string _curveName;

		public string CurveName { get { return _curveName; } }

		public GameObject _targetGameObject;

		[SerializeField]
		private bool _isGeoCurve;

		public bool IsGeoCurve() { return _isGeoCurve; }


		public enum CurveEditState
		{
			INVALID,
			GENERATED,
			EDITING,
			REQUIRES_GENERATION
		}
		[SerializeField]
		private CurveEditState _editState;

		public CurveEditState EditState { get { return _editState; } }

		// Types of interaction with this curve. Used by Editor.
		public enum Interaction
		{
			VIEW,
			ADD,
			EDIT
		}

		// Preferred interaction mode when this a curve selected. Allows for quick access for curve editing.
		public static Interaction PreferredNextInteractionMode = Interaction.VIEW;

		public enum CurveDrawCollision
		{
			COLLIDERS,
			LAYERMASK
		}


		// LOGIC ------------------------------------------------------------------------------------------------------

		public static HEU_Curve CreateSetupCurve(HEU_HoudiniAsset parentAsset, bool isEditable, string curveName, HAPI_NodeId geoID, bool bGeoCurve)
		{
			HEU_Curve newCurve = ScriptableObject.CreateInstance<HEU_Curve>();
			newCurve._isEditable = isEditable;
			newCurve._curveName = curveName;
			newCurve._geoID = geoID;
			newCurve.SetEditState(CurveEditState.INVALID);
			newCurve._isGeoCurve = bGeoCurve;
			parentAsset.AddCurve(newCurve);
			return newCurve;
		}

		public void DestroyAllData()
		{
			if (_parameters != null)
			{
				_parameters.CleanUp();
				_parameters = null;
			}

			if(_isGeoCurve && _targetGameObject != null)
			{
				HEU_HAPIUtility.DestroyGameObject(_targetGameObject);
				_targetGameObject = null;
			}
		}

		public void SetCurveName(string name)
		{
			_curveName = name;
			if(_targetGameObject != null)
			{
				_targetGameObject.name = name;
			}
		}

		public void UploadParameterPreset(HEU_SessionBase session, HAPI_NodeId geoID, HEU_HoudiniAsset parentAsset)
		{
			// TODO FIXME
			// This fixes up the geo IDs for curves, and upload parameter values to Houdini.
			// This is required for curves in saved scenes, as its parameter data is not part of the parent asset's
			// parameter preset. Also the _geoID and parameters._nodeID could be different so uploading the
			// parameter values before cooking would not be valid for those IDs. This waits until after cooking
			// to then upload and cook just the curve.
			// Admittedly this is a temporary solution until a proper workaround is in place. Ideally for an asset reload
			// the object node and geo node names can be used to match up the IDs and then parameter upload can happen
			// before cooking.

			_geoID = geoID;

			if(_parameters != null)
			{
				_parameters._nodeID = geoID;

				if(_bUploadParameterPreset)
				{
					_parameters.UploadPresetData(session);
					_parameters.UploadValuesToHoudini(session, parentAsset);

					HEU_HAPIUtility.CookNodeInHoudini(session, geoID, false, _curveName);

					_bUploadParameterPreset = false;
				}
			}
		}

		public void ResetCurveParameters(HEU_SessionBase session, HEU_HoudiniAsset parentAsset)
		{
			if(_parameters != null)
			{
				_parameters.ResetAllToDefault(session);

				// Force an upload here so that when the parent asset recooks, it will have updated parameter values.
				_parameters.UploadPresetData(session);
				_parameters.UploadValuesToHoudini(session, parentAsset);
			}
		}

		public void SetCurveParameterPreset(HEU_SessionBase session, HEU_HoudiniAsset parentAsset, byte[] parameterPreset)
		{
			if (_parameters != null)
			{
				_parameters.SetPresetData(parameterPreset);

				// Force an upload here so that when the parent asset recooks, it will have updated parameter values.
				_parameters.UploadPresetData(session);
				_parameters.UploadValuesToHoudini(session, parentAsset);
			}
		}

		public void UpdateCurve(HEU_SessionBase session, HAPI_PartId partID)
		{
			int vertexCount = 0;
			float[] posAttr = new float[0];

			if (partID != HEU_Defines.HEU_INVALID_NODE_ID)
			{
				// Get position attributes.
				// Note that for an empty curve (ie. no position attributes) this query will fail, 
				// but the curve is still valid, so we simply set to null vertices. This allows 
				// user to add points later on.
				HAPI_AttributeInfo posAttrInfo = new HAPI_AttributeInfo();
				HEU_GeneralUtility.GetAttribute(session, _geoID, partID, HEU_Defines.HAPI_ATTRIB_POSITION, ref posAttrInfo, ref posAttr, session.GetAttributeFloatData);
				if (posAttrInfo.exists)
				{
					vertexCount = posAttrInfo.count;
				}
			}

			// Curve guides from position attributes
			_vertices = new Vector3[vertexCount];
			for(int i = 0; i < vertexCount; ++i)
			{
				_vertices[i][0] = -posAttr[i * 3 + 0];
				_vertices[i][1] =  posAttr[i * 3 + 1];
				_vertices[i][2] =  posAttr[i * 3 + 2];
			}
		}

		public void GenerateMesh(GameObject inGameObject)
		{
			_targetGameObject = inGameObject;

			MeshFilter meshFilter = _targetGameObject.GetComponent<MeshFilter>();
			if(meshFilter == null)
			{
				meshFilter = _targetGameObject.AddComponent<MeshFilter>();
			}

			MeshRenderer meshRenderer = _targetGameObject.GetComponent<MeshRenderer>();
			if(meshRenderer == null)
			{
				meshRenderer = _targetGameObject.AddComponent<MeshRenderer>();

				Shader shader = HEU_MaterialFactory.FindPluginShader(HEU_PluginSettings.DefaultCurveShader);
				meshRenderer.sharedMaterial = new Material(shader);
				meshRenderer.sharedMaterial.SetColor("_Color", HEU_PluginSettings.LineColor);
			}

			Mesh mesh = meshFilter.sharedMesh;

			if(_points.Count <= 1)
			{
				if (mesh != null)
				{
					mesh.Clear();
					mesh = null;
				}
			}
			else
			{
				if (mesh == null)
				{
					mesh = new Mesh();
					mesh.name = "Curve";
				}

				int[] indices = new int[_vertices.Length];
				for(int i = 0; i < _vertices.Length; ++i)
				{
					indices[i] = i;
				}

				mesh.Clear();
				mesh.vertices = _vertices;
				mesh.SetIndices(indices, MeshTopology.LineStrip, 0);
				mesh.RecalculateBounds();

				mesh.UploadMeshData(false);
			}

			meshFilter.sharedMesh = mesh;
			meshRenderer.enabled = HEU_PluginSettings.Curves_ShowInSceneView;

			SetEditState(CurveEditState.GENERATED);
		}

		public void SyncFromParameters(HEU_SessionBase session, HEU_HoudiniAsset parentAsset)
		{
			HAPI_NodeInfo geoNodeInfo = new HAPI_NodeInfo();
			if (!session.GetNodeInfo(_geoID, ref geoNodeInfo))
			{
				return;
			}

			if (_parameters != null)
			{
				_parameters.CleanUp();
			}
			else
			{
				_parameters = ScriptableObject.CreateInstance<HEU_Parameters>();
			}

			string geoNodeName = HEU_SessionManager.GetString(geoNodeInfo.nameSH, session);
			_parameters._uiLabel = geoNodeName.ToUpper() + " PARAMETERS";

			bool bResult = _parameters.Initialize(session, _geoID, ref geoNodeInfo, null, null, parentAsset);
			if (!bResult)
			{
				Debug.LogWarningFormat("Parameter generate failed for geo node {0}.", geoNodeInfo.id);
				_parameters.CleanUp();
				return;
			}

			_points.Clear();
			string pointList = _parameters.GetStringFromParameter(HEU_Defines.CURVE_COORDS_PARAM);
			if(!string.IsNullOrEmpty(pointList))
			{
				string[] pointSplit = pointList.Split(' ');
				foreach (string str in pointSplit)
				{
					string[] vecSplit = str.Split(',');
					if (vecSplit.Length == 3)
					{
						_points.Add(new Vector3(-System.Convert.ToSingle(vecSplit[0], System.Globalization.CultureInfo.InvariantCulture),
							System.Convert.ToSingle(vecSplit[1], System.Globalization.CultureInfo.InvariantCulture),
							System.Convert.ToSingle(vecSplit[2], System.Globalization.CultureInfo.InvariantCulture)));
					}
				}
			}

			// Since we just reset / created new our parameters and sync'd, we also need to 
			// get the preset from Houdini session
			if (!HEU_EditorUtility.IsEditorPlaying() && IsEditable())
			{
				DownloadPresetData(session);
			}
		}

		/// <summary>
		/// Project curve points onto collider or layer.
		/// </summary>
		/// <param name="parentAsset">Parent asset of the curve</param>
		/// <param name="rayDirection">Direction to cast ray</param>
		/// <param name="rayDistance">Maximum ray cast distance</param>
		public void ProjectToColliders(HEU_HoudiniAsset parentAsset, Vector3 rayDirection, float rayDistance)
		{
			bool bRequiresUpload = false;

			LayerMask layerMask = Physics.DefaultRaycastLayers;

			HEU_Curve.CurveDrawCollision collisionType = parentAsset.CurveDrawCollision;
			if(collisionType == CurveDrawCollision.COLLIDERS)
			{
				List<Collider> colliders = parentAsset.GetCurveDrawColliders();

				bool bFoundHit = false;
				int numPoints = _points.Count;
				for(int i = 0; i < numPoints; ++i)
				{
					bFoundHit = false;
					RaycastHit[] rayHits = Physics.RaycastAll(_points[i], rayDirection, rayDistance, layerMask, QueryTriggerInteraction.Ignore);
					foreach(RaycastHit hit in rayHits)
					{
						foreach(Collider collider in colliders)
						{
							if(hit.collider == collider)
							{
								_points[i] = hit.point;
								bFoundHit = true;
								bRequiresUpload = true;
								break;
							}
						}

						if(bFoundHit)
						{
							break;
						}
					}
				}


			}
			else if(collisionType == CurveDrawCollision.LAYERMASK)
			{
				layerMask = parentAsset.GetCurveDrawLayerMask();

				int numPoints = _points.Count;
				for (int i = 0; i < numPoints; ++i)
				{
					RaycastHit hitInfo;
					if (Physics.Raycast(_points[i], rayDirection, out hitInfo, rayDistance, layerMask, QueryTriggerInteraction.Ignore))
					{
						_points[i] = hitInfo.point;
						bRequiresUpload = true;
					}
				}
			}

			if(bRequiresUpload)
			{
				HEU_ParameterData paramData = _parameters.GetParameter(HEU_Defines.CURVE_COORDS_PARAM);
				if (paramData != null)
				{
					paramData._stringValues[0] = GetPointsString(_points);
				}

				SetEditState(CurveEditState.REQUIRES_GENERATION);
			}
		}

		/// <summary>
		/// Returns points array as string
		/// </summary>
		/// <param name="points">List of points to stringify</param>
		/// <returns></returns>
		public static string GetPointsString(List<Vector3> points)
		{
			StringBuilder sb = new StringBuilder();
			foreach (Vector3 pt in points)
			{
				sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0},{1},{2} ", -pt[0], pt[1], pt[2]);
			}
			return sb.ToString();
		}

		public void SetEditState(CurveEditState editState)
		{
			_editState = editState;
		}

		public void SetCurvePoint(int pointIndex, Vector3 newPosition)
		{
			if(pointIndex >= 0 && pointIndex < _points.Count)
			{
				_points[pointIndex] = newPosition;
			}
		}

		public Vector3 GetCurvePoint(int pointIndex)
		{
			if(pointIndex >= 0 && pointIndex < _points.Count)
			{
				return _points[pointIndex];
			}
			return Vector3.zero;
		}

		public List<Vector3> GetAllPoints()
		{
			return _points;
		}

		public int GetNumPoints()
		{
			return _points.Count;
		}

		public Vector3 GetTransformedPoint(int pointIndex)
		{
			if(pointIndex >= 0 && pointIndex < _points.Count)
			{
				return GetTransformedPosition(_points[pointIndex]);
			}
			return Vector3.zero;
		}

		public Vector3 GetTransformedPosition(Vector3 inPosition)
		{
			return this._targetGameObject.transform.TransformPoint(inPosition);
		}

		public Vector3 GetInvertedTransformedPosition(Vector3 inPosition)
		{
			return this._targetGameObject.transform.InverseTransformPoint(inPosition);
		}

		public Vector3 GetInvertedTransformedDirection(Vector3 inPosition)
		{
			return this._targetGameObject.transform.InverseTransformVector(inPosition);
		}

		public Vector3[] GetVertices()
		{
			return _vertices;
		}

		public void SetCurveGeometryVisibility(bool bVisible)
		{
			if(_targetGameObject != null)
			{
				MeshRenderer renderer = _targetGameObject.GetComponent<MeshRenderer>();
				if(renderer != null)
				{
					renderer.enabled = bVisible;
				}
			}
		}

		public void DownloadPresetData(HEU_SessionBase session)
		{
			if(_parameters != null)
			{
				_parameters.DownloadPresetData(session);
			}
		}

		public void UploadPresetData(HEU_SessionBase session)
		{
			if (_parameters != null)
			{
				_parameters.UploadPresetData(session);
			}
		}

		public void DownloadAsDefaultPresetData(HEU_SessionBase session)
		{
			if (_parameters != null)
			{
				_parameters.DownloadAsDefaultPresetData(session);
			}
		}
	}

}   // HoudiniEngineUnity