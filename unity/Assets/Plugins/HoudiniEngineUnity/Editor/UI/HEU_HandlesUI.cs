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
using UnityEditor;
using System.Collections.Generic;

namespace HoudiniEngineUnity
{
	/// <summary>
	/// Draws all Handles for an asset.
	/// </summary>
	[CanEditMultipleObjects]
	[CustomEditor(typeof(HEU_Handle))]
	public class HEU_HandlesUI : Editor
	{
		public enum HEU_HandleManipMode
		{
			MOVE,
			ROTATE,
			SCALE
		}

		// LOGIC ------------------------------------------------------------------------------------------------------

		public List<HEU_Handle> CacheHandles()
		{
			List<HEU_Handle> handles = new List<HEU_Handle>();
			foreach (Object targetObject in targets)
			{
				HEU_Handle handle = targetObject as HEU_Handle;
				if (handle != null)
				{
					handles.Add(handle);
				}
			}
			return handles;
		}

		public bool DrawHandles(HEU_HoudiniAsset asset)
		{
			List<HEU_Handle> handles = CacheHandles();

			HEU_HandleManipMode manipMode = GetCurrentGlobalManipMode();

			GUIStyle textStyle = new GUIStyle(EditorStyles.textField);
			textStyle.contentOffset = new Vector2(1.4f, 1.4f);

			HEU_Parameters assetParameters = asset.Parameters;
			SerializedObject serializedParametersObject = new SerializedObject(assetParameters);
			SerializedProperty parameterListProperty = HEU_EditorUtility.GetSerializedProperty(serializedParametersObject, "_parameterList");

			bool bChanged = false;

			Matrix4x4 defaultMatrix = Handles.matrix;

			foreach (HEU_Handle handle in handles)
			{
				if (handle.HandleType == HEU_Handle.HEU_HandleType.XFORM)
				{
					Handles.matrix = asset.transform.localToWorldMatrix;

					Vector3 handlePosition = handle.HandlePosition;
					Quaternion handleRotation = handle.HandleRotation;
					Vector3 handleScale = handle.HandleScale;

					string handleName = handle.HandleName;

					if (manipMode == HEU_HandleManipMode.MOVE)
					{
						if(!handle.HasTranslateHandle())
						{
							continue;
						}

						bool bDisabled = handle.IsTranslateHandleDisabled();
						if(bDisabled)
						{
							handleName += " (disabled)";
						}

						GUIContent labelContent = new GUIContent(handleName);
						labelContent.tooltip = handleName;

						Handles.Label(handlePosition, labelContent, textStyle);

						if(bDisabled)
						{
							bool bLighting = Handles.lighting;
							Handles.lighting = false;
							Handles.PositionHandle(handlePosition, handleRotation);
							Handles.lighting = bLighting;
							continue;
						}

						Vector3 updatedPosition = Handles.PositionHandle(handlePosition, handleRotation);
						if(updatedPosition != handlePosition)
						{
							if(!handle.GetUpdatedPosition(asset, ref updatedPosition))
							{
								continue;
							}

							HEU_HandleParamBinding translateBinding = handle.GetTranslateBinding();
							if(translateBinding != null)
							{
								HEU_ParameterData paramData = assetParameters.GetParameterWithParmID(translateBinding._parmID);
								if(paramData != null && paramData._unityIndex < parameterListProperty.arraySize)
								{
									SerializedProperty paramDataProperty = parameterListProperty.GetArrayElementAtIndex(paramData._unityIndex);
									float[] posFloats = new float[3];
									posFloats[0] = updatedPosition[0];
									posFloats[1] = updatedPosition[1];
									posFloats[2] = updatedPosition[2];
									bChanged |= UpdateFloatArrayProperty(paramDataProperty, posFloats, translateBinding);
								}
							}
						}
					}
					else if(manipMode == HEU_HandleManipMode.ROTATE)
					{
						if (!handle.HasRotateHandle())
						{
							continue;
						}

						bool bDisabled = handle.IsRotateHandleDisabled();
						if (bDisabled)
						{
							handleName += " (disabled)";
						}

						GUIContent labelContent = new GUIContent(handleName);
						labelContent.tooltip = handleName;

						Handles.Label(handlePosition, labelContent, textStyle);

						if (bDisabled)
						{
							bool bLighting = Handles.lighting;
							Handles.lighting = false;
							Handles.RotationHandle(handleRotation, handlePosition);
							Handles.lighting = bLighting;
							continue;
						}

						Quaternion updatedRotation = Handles.RotationHandle(handleRotation, handlePosition);
						if (updatedRotation != handleRotation)
						{
							if (!handle.GetUpdatedRotation(asset, ref updatedRotation))
							{
								continue;
							}

							HEU_HandleParamBinding rotateBinding = handle.GetRotateBinding();
							if (rotateBinding != null)
							{
								HEU_ParameterData paramData = assetParameters.GetParameterWithParmID(rotateBinding._parmID);
								if (paramData != null && paramData._unityIndex < parameterListProperty.arraySize)
								{
									SerializedProperty paramDataProperty = parameterListProperty.GetArrayElementAtIndex(paramData._unityIndex);
									float[] rotFloats = new float[3];
									rotFloats[0] = updatedRotation[0];
									rotFloats[1] = updatedRotation[1];
									rotFloats[2] = updatedRotation[2];
									bChanged |= UpdateFloatArrayProperty(paramDataProperty, rotFloats, rotateBinding);
								}
							}
						}
					}
					else if (manipMode == HEU_HandleManipMode.SCALE)
					{
						if (!handle.HasScaleHandle())
						{
							continue;
						}

						bool bDisabled = handle.IsScaleHandleDisabled();
						if (bDisabled)
						{
							handleName += " (disabled)";
						}

						GUIContent labelContent = new GUIContent(handleName);
						labelContent.tooltip = handleName;

						Handles.Label(handlePosition, labelContent, textStyle);

						if (bDisabled)
						{
							bool bLighting = Handles.lighting;
							Handles.lighting = false;
							Handles.ScaleHandle(handleScale, handlePosition, handleRotation, 1f);
							Handles.lighting = bLighting;
							continue;
						}

						Vector3 updatedScale = Handles.ScaleHandle(handleScale, handlePosition, handleRotation, 1f);
						if (updatedScale != handleScale)
						{
							HEU_HandleParamBinding scaleBinding = handle.GetScaleBinding();
							if (scaleBinding != null)
							{
								HEU_ParameterData paramData = assetParameters.GetParameterWithParmID(scaleBinding._parmID);
								if (paramData != null && paramData._unityIndex < parameterListProperty.arraySize)
								{
									SerializedProperty paramDataProperty = parameterListProperty.GetArrayElementAtIndex(paramData._unityIndex);
									float[] scaleFloats = new float[3];
									scaleFloats[0] = updatedScale[0];
									scaleFloats[1] = updatedScale[1];
									scaleFloats[2] = updatedScale[2];
									bChanged |= UpdateFloatArrayProperty(paramDataProperty, scaleFloats, scaleBinding);
								}
							}
						}
					}
				}
			}

			if (bChanged)
			{
				serializedParametersObject.ApplyModifiedProperties();
			}

			Handles.matrix = defaultMatrix;

			return bChanged;
		}

		public bool UpdateFloatArrayProperty(SerializedProperty paramDataProperty, float[] inValues, HEU_HandleParamBinding bindingParam)
		{
			bool bChanged = false;
			SerializedProperty floatValuesProperty = paramDataProperty.FindPropertyRelative("_floatValues");

			int numChannels = bindingParam._boundChannels.Length;
			for(int i = 0; i < numChannels; ++i)
			{
				if(bindingParam._boundChannels[i] && floatValuesProperty.GetArrayElementAtIndex(i).floatValue != inValues[i])
				{
					floatValuesProperty.GetArrayElementAtIndex(i).floatValue = inValues[i];
					bChanged = true;
				}
			}

			return bChanged;
		}

		public static HEU_HandleManipMode GetCurrentGlobalManipMode()
		{
			string manipTool = Tools.current.ToString();
			HEU_HandleManipMode manipMode = HEU_HandleManipMode.MOVE;
			if (manipTool.Equals("Move"))
			{
				manipMode = HEU_HandleManipMode.MOVE;
			}
			else if(manipTool.Equals("Rotate"))
			{
				manipMode = HEU_HandleManipMode.ROTATE;
			}
			else if(manipTool.Equals("Scale"))
			{
				manipMode = HEU_HandleManipMode.SCALE;
			}
			return manipMode;
		}
	}

}   // HoudiniEngineUnity