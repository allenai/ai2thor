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
	/// Contains utility functions for working with parameters
	/// </summary>
	public static class HEU_ParameterUtility
	{
		public static bool GetToggle(HEU_HoudiniAsset asset, string paramName, out bool outValue)
		{
			outValue = false;
			HEU_ParameterData paramData = asset.Parameters.GetParameter(paramName);
			if (paramData != null && paramData.IsToggle())
			{
				outValue = paramData._toggle;
				return true;
			}
			else
			{
				Debug.LogWarningFormat("{0}: Query failed. Asset [{0}]'s Parameter [{1}] is not a valid toggle!", asset.AssetName, paramName);
				return false;
			}
		}

		public static bool SetToggle(HEU_HoudiniAsset asset, string paramName, bool setValue)
		{
			HEU_ParameterData paramData = asset.Parameters.GetParameter(paramName);
			if (paramData != null && paramData.IsToggle())
			{
				paramData._toggle = setValue;
				return true;
			}
			else
			{
				Debug.LogWarningFormat("{0}: Set failed. Asset [{0}]'s Parameter [{1}] is not a valid toggle!", asset.AssetName, paramName);
				return false;
			}
		}

		public static bool GetInt(HEU_HoudiniAsset asset, string paramName, out int outValue)
		{
			outValue = 0;
			HEU_ParameterData paramData = asset.Parameters.GetParameter(paramName);
			if (paramData != null && paramData.IsInt())
			{
				outValue = paramData._intValues[0];
				return true;
			}
			else
			{
				Debug.LogWarningFormat("{0}: Query failed. Asset [{0}]'s Parameter [{1}] is not a valid int!", asset.AssetName, paramName);
				return false;
			}
		}

		public static bool SetInt(HEU_HoudiniAsset asset, string paramName, int setValue)
		{
			HEU_ParameterData paramData = asset.Parameters.GetParameter(paramName);
			if (paramData != null && paramData.IsInt())
			{
				paramData._intValues[0] = setValue;
				return true;
			}
			else
			{
				Debug.LogWarningFormat("{0}: Set failed. Asset [{0}]'s Parameter [{1}] is not a valid int!", asset.AssetName, paramName);
				return false;
			}
		}

		public static bool GetFloat(HEU_HoudiniAsset asset, string paramName, out float outValue)
		{
			outValue = 0;
			HEU_ParameterData paramData = asset.Parameters.GetParameter(paramName);
			if (paramData != null && paramData.IsFloat())
			{
				outValue = paramData._floatValues[0];
				return true;
			}
			else
			{
				Debug.LogWarningFormat("{0}: Query failed. Asset [{0}]'s Parameter [{1}] is not a valid float!", asset.AssetName, paramName);
				return false;
			}
		}

		public static bool GetFloats(HEU_HoudiniAsset asset, string paramName, out float[] outValues)
		{
			HEU_ParameterData paramData = asset.Parameters.GetParameter(paramName);
			if (paramData != null && paramData.IsFloat())
			{
				outValues = paramData._floatValues;
				return true;
			}
			else
			{
				outValues = new float[0];
				Debug.LogWarningFormat("{0}: Query failed. Asset [{0}]'s Parameter [{1}] is not a valid float!", asset.AssetName, paramName);
				return false;
			}
		}

		public static bool SetFloat(HEU_HoudiniAsset asset, string paramName, float setValue)
		{
			HEU_ParameterData paramData = asset.Parameters.GetParameter(paramName);
			if (paramData != null && paramData.IsFloat())
			{
				paramData._floatValues[0] = setValue;
				return true;
			}
			else
			{
				Debug.LogWarningFormat("{0}: Set failed. Asset [{0}]'s Parameter [{1}] is not a valid float!", asset.AssetName, paramName);
				return false;
			}
		}

		public static bool SetFloats(HEU_HoudiniAsset asset, string paramName, float[] setValues)
		{
			HEU_ParameterData paramData = asset.Parameters.GetParameter(paramName);
			if (paramData != null && paramData.IsFloat())
			{
				paramData._floatValues = setValues;
				return true;
			}
			else
			{
				Debug.LogWarningFormat("{0}: Set failed. Asset [{0}]'s Parameter [{1}] is not a valid float!", asset.AssetName, paramName);
				return false;
			}
		}

		public static bool GetString(HEU_HoudiniAsset asset, string paramName, out string outValue)
		{
			outValue = null;
			HEU_ParameterData paramData = asset.Parameters.GetParameter(paramName);
			if (paramData != null && (paramData.IsString() || paramData.IsPathFile()))
			{
				outValue = paramData._stringValues[0];
				return true;
			}
			else
			{
				Debug.LogWarningFormat("{0}: Query failed. Asset [{0}]'s Parameter [{1}] is not a valid string!", asset.AssetName, paramName);
				return false;
			}
		}

		public static bool SetString(HEU_HoudiniAsset asset, string paramName, string setValue)
		{
			HEU_ParameterData paramData = asset.Parameters.GetParameter(paramName);
			if (paramData != null && (paramData.IsString() || paramData.IsPathFile()))
			{
				paramData._stringValues[0] = setValue;
				return true;
			}
			else
			{
				Debug.LogWarningFormat("{0}: Set failed. Asset [{0}]'s Parameter [{1}] is not a valid string!", asset.AssetName, paramName);
				return false;
			}
		}

		public static bool SetChoice(HEU_HoudiniAsset asset, string paramName, int setValue)
		{
			HEU_ParameterData paramData = asset.Parameters.GetParameter(paramName);
			if (paramData != null && paramData._parmInfo.choiceCount > 0 && setValue >= 0 && setValue < paramData._choiceIntValues.Length)
			{
				paramData._intValues[0] = paramData._choiceIntValues[setValue];
				return true;
			}
			else
			{
				Debug.LogWarningFormat("{0}: Set failed. Asset [{0}]'s Parameter [{1}] is not a valid choice!", asset.AssetName, paramName);
				return false;
			}
		}

		public static bool GetChoice(HEU_HoudiniAsset asset, string paramName, out int outValue)
		{
			outValue = 0;
			HEU_ParameterData paramData = asset.Parameters.GetParameter(paramName);
			if (paramData != null && paramData._parmInfo.choiceCount > 0)
			{
				outValue = paramData._intValues[0];
				return true;
			}
			else
			{
				Debug.LogWarningFormat("{0}: Query failed. Asset [{0}]'s Parameter [{1}] is not a valid choice!", asset.AssetName, paramName);
				return false;
			}
		}

		public static bool SetInputNode(HEU_HoudiniAsset asset, string paramName, GameObject obj, int index)
		{
			HEU_ParameterData paramData = asset.Parameters.GetParameter(paramName);
			if (paramData != null && paramData._paramInputNode != null)
			{
				if (index < paramData._paramInputNode.NumInputEntries())
				{
					paramData._paramInputNode.InsertInputEntry(index, obj);
				}
				else
				{
					paramData._paramInputNode.AddInputEntryAtEnd(obj);
				}

				paramData._paramInputNode.RequiresUpload = true;

				return true;
			}
			else
			{
				Debug.LogWarningFormat("{0}: Set failed. Asset [{0}]'s Parameter [{1}] is not a valid input parameter!", asset.AssetName, paramName);
				return false;
			}
		}

		public static bool GetInputNode(HEU_HoudiniAsset asset, string paramName, int index, out GameObject obj)
		{
			obj = null;
			HEU_ParameterData paramData = asset.Parameters.GetParameter(paramName);
			if (paramData != null && paramData._paramInputNode != null)
			{
				obj = paramData._paramInputNode.GetInputEntryGameObject(index);
				return obj != null;
			}
			else
			{
				Debug.LogWarningFormat("{0}: Set failed. Asset [{0}]'s Parameter [{1}] is not a valid input parameter!", asset.AssetName, paramName);
				return false;
			}
		}

		public static bool GetColor(HEU_HoudiniAsset asset, string paramName, out Color getValue)
		{
			HEU_ParameterData paramData = asset.Parameters.GetParameter(paramName);
			if (paramData != null && paramData.IsColor())
			{
				getValue = paramData._color;
				return true;
			}
			else
			{
				getValue = Color.white;
				Debug.LogWarningFormat("{0}: Query failed. Asset [{0}]'s Parameter [{1}] is not a valid color!", asset.AssetName, paramName);
				return false;
			}
		}

		public static bool SetColor(HEU_HoudiniAsset asset, string paramName, Color setValue)
		{
			HEU_ParameterData paramData = asset.Parameters.GetParameter(paramName);
			if (paramData != null && paramData.IsColor())
			{
				paramData._color = setValue;
				return true;
			}
			else
			{
				Debug.LogWarningFormat("{0}: Set failed. Asset [{0}]'s Parameter [{1}] is not a valid color!", asset.AssetName, paramName);
				return false;
			}
		}

		public static int GetParameterIndexFromName(HEU_SessionBase session, HAPI_ParmInfo[] parameters, string parameterName)
		{
			if(parameters != null && parameters.Length > 0)
			{
				int numParameters = parameters.Length;
				for(int i = 0; i < numParameters; ++i)
				{
					if(HEU_SessionManager.GetString(parameters[i].nameSH, session).Equals(parameterName))
					{
						return i;
					}
				}
			}
			return -1;
		}

		public static int GetParameterIndexFromNameOrTag(HEU_SessionBase session, HAPI_NodeId nodeID, HAPI_ParmInfo[] parameters, string parameterName)
		{
			int parameterIndex = GetParameterIndexFromName(session, parameters, parameterName);
			if (parameterIndex < 0)
			{
				// Try to find tag instead
				parameterIndex = HEU_Defines.HEU_INVALID_NODE_ID;
				session.GetParmWithTag(nodeID, parameterName, ref parameterIndex);
			}
			return parameterIndex;
		}

		public static float GetParameterFloatValue(HEU_SessionBase session, HAPI_NodeId nodeID, HAPI_ParmInfo[] parameters, string parameterName, float defaultValue)
		{
			int parameterIndex = GetParameterIndexFromNameOrTag(session, nodeID, parameters, parameterName);
			if(parameterIndex < 0 || parameterIndex >= parameters.Length)
			{
				return defaultValue;
			}

			int valueIndex = parameters[parameterIndex].floatValuesIndex;
			float[] value = new float[1];

			if(session.GetParamFloatValues(nodeID, value, valueIndex, 1))
			{
				return value[0];
			}

			return defaultValue;
		}

		public static Color GetParameterColor3Value(HEU_SessionBase session, HAPI_NodeId nodeID, HAPI_ParmInfo[] parameters, string parameterName, Color defaultValue)
		{
			int parameterIndex = GetParameterIndexFromNameOrTag(session, nodeID, parameters, parameterName);
			if (parameterIndex < 0 || parameterIndex >= parameters.Length)
			{
				return defaultValue;
			}

			if(parameters[parameterIndex].size < 3)
			{
				Debug.LogError("Parameter size not large enough to be a Color3");
				return defaultValue;
			}

			int valueIndex = parameters[parameterIndex].floatValuesIndex;
			float[] value = new float[3];

			if (session.GetParamFloatValues(nodeID, value, valueIndex, 3))
			{
				return new Color(value[0], value[1], value[2], 1f);
			}
			return defaultValue;
		}
	}

}   // HoudiniEngineUnity