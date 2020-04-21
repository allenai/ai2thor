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

namespace HoudiniEngineUnity
{
	/////////////////////////////////////////////////////////////////////////////////////////////////////////////////
	// Typedefs (copy these from HEU_Common.cs)
	using HAPI_NodeId = System.Int32;
	using HAPI_ParmId = System.Int32;

	/// <summary>
	/// Represents a Handle in an asset.
	/// Currently only supports transform (xform) handle.
	/// </summary>
	[System.Serializable]
	public class HEU_Handle : ScriptableObject
	{
		public enum HEU_HandleType
		{
			XFORM,
			UNSUPPORTED
		}

		[SerializeField]
		private string _handleName;

		public string HandleName { get { return _handleName; } }

		[SerializeField]
		private HEU_HandleType _handleType;

		public HEU_HandleType HandleType { get { return _handleType; } }

		[SerializeField]
		private int _handleIndex;

		[SerializeField]
		private HEU_HandleParamBinding _handleParamTranslateBinding;

		[SerializeField]
		private HEU_HandleParamBinding _handleParamRotateBinding;

		[SerializeField]
		private HEU_HandleParamBinding _handleParamScaleBinding;

		[SerializeField]
		private Vector3 _handlePosition = Vector3.zero;

		[SerializeField]
		private Quaternion _handleRotation = Quaternion.identity;

		[SerializeField]
		private Vector3 _handleScale = Vector3.one;

		[SerializeField]
		private HAPI_RSTOrder _rstOrder = HAPI_RSTOrder.HAPI_SRT;

		public HAPI_RSTOrder RSTOrder { get { return _rstOrder; } }

		[SerializeField]
		private HAPI_XYZOrder _xyzOrder = HAPI_XYZOrder.HAPI_XYZ;

		public HAPI_XYZOrder XYZOrder { get { return _xyzOrder; } }

		[SerializeField]
		private HAPI_TransformEuler _convertedTransformEuler;

		public HAPI_TransformEuler ConvertedTransformEuler { get { return _convertedTransformEuler; } }

		public bool HasTranslateHandle() { return _handleParamTranslateBinding != null; }

		public bool HasRotateHandle() { return _handleParamRotateBinding != null; }

		public bool HasScaleHandle() { return _handleParamScaleBinding != null; }

		public bool IsTranslateHandleDisabled() { return (_handleParamTranslateBinding != null) ? _handleParamTranslateBinding._bDisabled : true; }

		public bool IsRotateHandleDisabled() { return (_handleParamRotateBinding != null) ? _handleParamRotateBinding._bDisabled : true; }

		public bool IsScaleHandleDisabled() { return (_handleParamScaleBinding != null) ? _handleParamScaleBinding._bDisabled : true; }

		public HEU_HandleParamBinding GetTranslateBinding() { return _handleParamTranslateBinding; }

		public HEU_HandleParamBinding GetRotateBinding() { return _handleParamRotateBinding; }

		public HEU_HandleParamBinding GetScaleBinding() { return _handleParamScaleBinding; }

		public Vector3 HandlePosition { get { return _handlePosition; } }

		public Quaternion HandleRotation { get { return _handleRotation; } }

		public Vector3 HandleScale { get { return _handleScale; } }


		//  LOGIC -----------------------------------------------------------------------------------------------------

		public bool SetupHandle(HEU_SessionBase session, HAPI_NodeId assetID, int handleIndex, string handleName, 
			HEU_HandleType handleType, ref HAPI_HandleInfo handleInfo, HEU_Parameters parameters)
		{
			_handleIndex = handleIndex;
			_handleName = handleName;
			_handleType = handleType;

			HAPI_HandleBindingInfo[] handleBindingInfos = new HAPI_HandleBindingInfo[handleInfo.bindingsCount];
			if (!session.GetHandleBindingInfo(assetID, _handleIndex, handleBindingInfos, 0, handleInfo.bindingsCount))
			{
				return false;
			}

			HAPI_ParmId translateParmID = -1;
			HAPI_ParmId rotateParmID = -1;
			HAPI_ParmId scaleParmID = -1;
			HAPI_ParmId rstOrderParmID = -1;
			HAPI_ParmId xyzOrderParmID = -1;

			_rstOrder = HAPI_RSTOrder.HAPI_SRT;
			_xyzOrder = HAPI_XYZOrder.HAPI_XYZ;

			_handleParamTranslateBinding = null;
			_handleParamRotateBinding = null;
			_handleParamScaleBinding = null;

			for (int i = 0; i < handleBindingInfos.Length; ++i)
			{
				string parmName = HEU_SessionManager.GetString(handleBindingInfos[i].handleParmNameSH, session);

				//string assetParmName = HEU_SessionManager.GetString(handleBindingInfos[i].assetParmNameSH, session);
				//Debug.LogFormat("Handle {0} has parm {1} with asset parm {2} with asset parm id {3}", handleName, parmName, assetParmName, handleBindingInfos[i].assetParmId);

				if (parmName.Equals("tx") || parmName.Equals("ty") || parmName.Equals("tz"))
				{
					translateParmID = handleBindingInfos[i].assetParmId;

					if(_handleParamTranslateBinding == null)
					{
						HEU_ParameterData parmData = parameters.GetParameterWithParmID(translateParmID);
						if (parmData != null && !parmData._parmInfo.invisible)
						{
							_handleParamTranslateBinding = new HEU_HandleParamBinding();
							_handleParamTranslateBinding._paramType = HEU_HandleParamBinding.HEU_HandleParamType.TRANSLATE;
							_handleParamTranslateBinding._parmID = parmData.ParmID;
							_handleParamTranslateBinding._paramName = parmData._name;
							_handleParamTranslateBinding._bDisabled = parmData._parmInfo.disabled;
						}
					}

					if(_handleParamTranslateBinding != null)
					{
						if(parmName.Equals("tx"))
						{
							_handleParamTranslateBinding._boundChannels[0] = true;
						}
						else if (parmName.Equals("ty"))
						{
							_handleParamTranslateBinding._boundChannels[1] = true;
						}
						else if (parmName.Equals("tz"))
						{
							_handleParamTranslateBinding._boundChannels[2] = true;
						}
					}
				}

				if (parmName.Equals("rx") || parmName.Equals("ry") || parmName.Equals("rz"))
				{
					rotateParmID = handleBindingInfos[i].assetParmId;

					if(_handleParamRotateBinding == null)
					{
						HEU_ParameterData parmData = parameters.GetParameterWithParmID(rotateParmID);
						if (parmData != null && !parmData._parmInfo.invisible)
						{
							_handleParamRotateBinding = new HEU_HandleParamBinding();
							_handleParamRotateBinding._paramType = HEU_HandleParamBinding.HEU_HandleParamType.ROTATE;
							_handleParamRotateBinding._parmID = parmData.ParmID;
							_handleParamRotateBinding._paramName = parmData._name;
							_handleParamRotateBinding._bDisabled = parmData._parmInfo.disabled;
						}
					}

					if (_handleParamRotateBinding != null)
					{
						if (parmName.Equals("rx"))
						{
							_handleParamRotateBinding._boundChannels[0] = true;
						}
						else if (parmName.Equals("ry"))
						{
							_handleParamRotateBinding._boundChannels[1] = true;
						}
						else if (parmName.Equals("rz"))
						{
							_handleParamRotateBinding._boundChannels[2] = true;
						}
					}
				}

				if (parmName.Equals("sx") || parmName.Equals("sy") || parmName.Equals("sz"))
				{
					scaleParmID = handleBindingInfos[i].assetParmId;

					if (_handleParamScaleBinding == null)
					{
						HEU_ParameterData parmData = parameters.GetParameterWithParmID(scaleParmID);
						if (parmData != null && !parmData._parmInfo.invisible)
						{
							_handleParamScaleBinding = new HEU_HandleParamBinding();
							_handleParamScaleBinding._paramType = HEU_HandleParamBinding.HEU_HandleParamType.SCALE;
							_handleParamScaleBinding._parmID = parmData.ParmID;
							_handleParamScaleBinding._paramName = parmData._name;
							_handleParamScaleBinding._bDisabled = parmData._parmInfo.disabled;
						}
					}

					if (_handleParamScaleBinding != null)
					{
						if (parmName.Equals("sx"))
						{
							_handleParamScaleBinding._boundChannels[0] = true;
						}
						else if (parmName.Equals("sy"))
						{
							_handleParamScaleBinding._boundChannels[1] = true;
						}
						else if (parmName.Equals("sz"))
						{
							_handleParamScaleBinding._boundChannels[2] = true;
						}
					}
				}

				if(parmName.Equals("trs_order"))
				{
					rstOrderParmID = handleBindingInfos[i].assetParmId;
				}

				if (parmName.Equals("xyz_order"))
				{
					xyzOrderParmID = handleBindingInfos[i].assetParmId;
				}
			}

			if (rstOrderParmID >= 0)
			{
				HEU_ParameterData parmData = parameters.GetParameter(rstOrderParmID);
				if (parmData != null)
				{
					_rstOrder = (HAPI_RSTOrder)parmData._intValues[0];
				}
			}

			if (xyzOrderParmID >= 0)
			{
				HEU_ParameterData parmData = parameters.GetParameter(xyzOrderParmID);
				if (parmData != null)
				{
					_xyzOrder = (HAPI_XYZOrder)parmData._intValues[0];
				}
			}

			GenerateTransform(session, parameters);

			return true;
		}

		public void CleanUp()
		{
			_handleParamTranslateBinding = null;
			_handleParamRotateBinding = null;
			_handleParamScaleBinding = null;
		}

		public void GenerateTransform(HEU_SessionBase session, HEU_Parameters parameters)
		{
			HAPI_TransformEuler transformEuler = new HAPI_TransformEuler(true);

			transformEuler.rstOrder = _rstOrder;
			transformEuler.rotationOrder = _xyzOrder;

			transformEuler.position[0] = 0;
			transformEuler.position[1] = 0;
			transformEuler.position[2] = 0;

			transformEuler.rotationEuler[0] = 0;
			transformEuler.rotationEuler[1] = 0;
			transformEuler.rotationEuler[2] = 0;

			transformEuler.scale[0] = 1;
			transformEuler.scale[1] = 1;
			transformEuler.scale[2] = 1;

			if (_handleParamTranslateBinding != null)
			{
				HEU_ParameterData parmData = parameters.GetParameterWithParmID(_handleParamTranslateBinding._parmID);
				if (parmData != null && !parmData._parmInfo.invisible)
				{
					transformEuler.position[0] = parmData._floatValues[0];
					transformEuler.position[1] = parmData._floatValues[1];
					transformEuler.position[2] = parmData._floatValues[2];
				}
			}

			if(_handleParamRotateBinding != null)
			{
				HEU_ParameterData parmData = parameters.GetParameterWithParmID(_handleParamRotateBinding._parmID);
				if (parmData != null && !parmData._parmInfo.invisible)
				{
					transformEuler.rotationEuler[0] = parmData._floatValues[0];
					transformEuler.rotationEuler[1] = parmData._floatValues[1];
					transformEuler.rotationEuler[2] = parmData._floatValues[2];
				}
			}

			if(_handleParamScaleBinding != null)
			{
				HEU_ParameterData parmData = parameters.GetParameterWithParmID(_handleParamScaleBinding._parmID);
				if (parmData != null && !parmData._parmInfo.invisible)
				{
					transformEuler.scale[0] = parmData._floatValues[0];
					transformEuler.scale[1] = parmData._floatValues[1];
					transformEuler.scale[2] = parmData._floatValues[2];
				}
			}

			if (!session.ConvertTransform(ref transformEuler, HAPI_RSTOrder.HAPI_SRT, HAPI_XYZOrder.HAPI_ZXY, out _convertedTransformEuler))
			{
				return;
			}

			// Convert to left-handed Unity
			_convertedTransformEuler.position[0] = -_convertedTransformEuler.position[0];
			_convertedTransformEuler.rotationEuler[1] = -_convertedTransformEuler.rotationEuler[1];
			_convertedTransformEuler.rotationEuler[2] = -_convertedTransformEuler.rotationEuler[2];

			if (IsSpecialRSTOrder(transformEuler.rstOrder))
			{
				_handlePosition = new Vector3(_convertedTransformEuler.position[0], _convertedTransformEuler.position[1], _convertedTransformEuler.position[2]);
			}
			else if (_handleParamTranslateBinding != null)
			{
				_handlePosition = new Vector3(-transformEuler.position[0], transformEuler.position[1], transformEuler.position[2]);
			}
			else
			{
				_handlePosition = Vector3.zero;
			}

			_handleRotation = Quaternion.Euler(_convertedTransformEuler.rotationEuler[0], _convertedTransformEuler.rotationEuler[1], _convertedTransformEuler.rotationEuler[2]);

			if (_handleParamScaleBinding != null)
			{
				_handleScale = new Vector3(transformEuler.scale[0], transformEuler.scale[1], transformEuler.scale[2]);
			}
			else
			{
				_handleScale = Vector3.one;
			}
		}

		public bool GetUpdatedPosition(HEU_HoudiniAsset asset, ref Vector3 inPosition)
		{
			if (_handleParamTranslateBinding == null || _handleParamTranslateBinding._bDisabled)
			{
				return false;
			}

			HEU_SessionBase session = asset.GetAssetSession(true);
			if(session == null)
			{
				return false;
			}

			if (IsSpecialRSTOrder(_rstOrder))
			{
				HAPI_TransformEuler transformEuler = _convertedTransformEuler;
				transformEuler.position[0] = inPosition[0];
				transformEuler.position[1] = inPosition[1];
				transformEuler.position[2] = inPosition[2];

				HAPI_TransformEuler newTransformEuler;
				if (!session.ConvertTransform(ref transformEuler, _rstOrder, _xyzOrder, out newTransformEuler))
				{
					return false;
				}

				inPosition[0] = newTransformEuler.position[0];
				inPosition[1] = newTransformEuler.position[1];
				inPosition[2] = newTransformEuler.position[2];
			}

			inPosition[0] = -inPosition[0];

			return true;
		}

		public bool GetUpdatedRotation(HEU_HoudiniAsset asset, ref Quaternion inRotation)
		{
			if (_handleParamTranslateBinding == null || _handleParamTranslateBinding._bDisabled)
			{
				return false;
			}

			HEU_SessionBase session = asset.GetAssetSession(true);
			if (session == null)
			{
				return false;
			}

			Vector3 newRotation = inRotation.eulerAngles;

			HAPI_TransformEuler transformEuler = _convertedTransformEuler;

			transformEuler.position[0] = 0;
			transformEuler.position[1] = 0;
			transformEuler.position[2] = 0;
			transformEuler.rotationEuler[0] = newRotation[0];
			transformEuler.rotationEuler[1] = newRotation[1];
			transformEuler.rotationEuler[2] = newRotation[2];
			transformEuler.scale[0] = 1;
			transformEuler.scale[1] = 1;
			transformEuler.scale[2] = 1;
			transformEuler.rotationOrder = HAPI_XYZOrder.HAPI_ZXY;
			transformEuler.rstOrder = HAPI_RSTOrder.HAPI_SRT;

			HAPI_TransformEuler newTransformEuler;
			if (!session.ConvertTransform(ref transformEuler, _rstOrder, _xyzOrder, out newTransformEuler))
			{
				return false;
			}

			inRotation[0] = newTransformEuler.rotationEuler[0];
			inRotation[1] = -newTransformEuler.rotationEuler[1];
			inRotation[2] = -newTransformEuler.rotationEuler[2];

			return true;
		}

		public static bool IsSpecialRSTOrder(HAPI_RSTOrder rstOrder)
		{
			return (rstOrder == HAPI_RSTOrder.HAPI_TSR || rstOrder == HAPI_RSTOrder.HAPI_STR || rstOrder == HAPI_RSTOrder.HAPI_SRT);
		}
	}

}   // HoudiniEngineUnity