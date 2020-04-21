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
using UnityEditor;

namespace HoudiniEngineUnity
{

	/// <summary>
	/// Custom editor to draw HDA parameters.
	/// This will handle all the drawing for all parameters in an HDA.
	/// Requires an HEU_Parameters that has been properly setup with parameter hierarchy.
	/// </summary>
	[CustomEditor(typeof(HEU_Parameters))]
	public class HEU_ParameterUI : Editor
	{
		// Cache reference to the currently selected HDA's parameter object
		private HEU_Parameters _parameters;

		// Cached list of parameter objects property
		private SerializedProperty _rootParametersProperty;
		private SerializedProperty _parameterListProperty;

		private SerializedProperty _showParametersProperty;

		// Layout constants
		private const float _multiparmPlusMinusWidth = 40f;


		// Store cached SerializedProperties of a HEU_ParameterData class, and store as list
		private class HEU_ParameterUICache
		{
			public HEU_ParameterData _parameterData;

			public SerializedProperty _paramType;
			public SerializedProperty _primaryValue;
			public SerializedProperty _secondaryValue;

			// For multiparm instance, this would be its index into the multiparm list
			public int _instanceIndex = -1;
			// For multiparm instance, this is the index of the parameter for this instance
			public int _paramIndexForInstance = -1;

			public List<HEU_ParameterUICache> _childrenCache;

			public string[] _tabLabels;

			// Need to wrap up the gradient in order to figure out what had changed in the gradient window
			public SerializedObject _gradientSerializedObject;

			// List of objects for string-based asset paths
			public List<UnityEngine.Object> _assetObjects;
		}
		private List<HEU_ParameterUICache> _parameterCache;

		private SerializedProperty _parameterModifiersProperty;

		private static GUIStyle _sliderStyle;
		private static GUIStyle _sliderThumbStyle;

		public class HEU_GradientContainer : ScriptableObject
		{
			public Gradient _gradient;
		}



		private void OnEnable()
		{
			_parameters = target as HEU_Parameters;
			//Debug.Assert(_parameters != null, "Target is not HEU_Parameters");
		}

		private void OnDisable()
		{
			_rootParametersProperty = null;
			_parameterListProperty = null;
			_showParametersProperty = null;
			_parameterModifiersProperty = null;

			_parameterCache = null;
		}

		public override void OnInspectorGUI()
		{
			// First check if serialized properties need to be cached
			if (_parameterCache == null || _parameters.RecacheUI)
			{
				CacheProperties();
			}

			InitializeGUIStyles();

			float previousFieldWidth = EditorGUIUtility.fieldWidth;
			EditorGUIUtility.fieldWidth = 100;

			// Now draw the cached properties

			EditorGUI.BeginChangeCheck();

			HEU_EditorUI.BeginSection();

			// Draw all the parameters. Start at root parameters, and draw their children recursively.
			_showParametersProperty.boolValue = HEU_EditorUI.DrawFoldOut(_showParametersProperty.boolValue, _parameters._uiLabel);
			if (_showParametersProperty.boolValue)
			{
				if (!_parameters.AreParametersValid())
				{
					EditorGUILayout.LabelField("Parameters haven't been generated or failed to generate.\nPlease Recook or Rebuild to regenerate them!", GUILayout.MinHeight(35));
				}
				else if (_parameters.RequiresRegeneration || _parameters.HasModifiersPending())
				{
					// Skip drawing parameters if there pending changes. 
					// Cooking needs to happen before we let the user manipulate UI further.
					EditorGUILayout.LabelField("Parameters have changed.\nPlease Recook to upload them to Houdini!", GUILayout.MinHeight(35));
				}
				else
				{
					foreach (HEU_ParameterUICache paramCache in _parameterCache)
					{
						DrawParamUICache(paramCache);
					}
				}
			}

			HEU_EditorUI.EndSection();

			if (EditorGUI.EndChangeCheck())
			{
				serializedObject.ApplyModifiedProperties();
			}

			EditorGUIUtility.fieldWidth = previousFieldWidth;
		}

		private static void InitializeGUIStyles()
		{
			if (_sliderStyle == null)
			{
				_sliderStyle = new GUIStyle(GUI.skin.horizontalSlider);
			}
			if (_sliderThumbStyle == null)
			{
				_sliderThumbStyle = new GUIStyle(GUI.skin.horizontalSliderThumb);
			}
		}

		private void CacheProperties()
		{
			//Debug.Log("Cacheing Properties");
			serializedObject.Update();

			// Flag that we've cached
			_parameters.RecacheUI = false;

			_rootParametersProperty = HEU_EditorUtility.GetSerializedProperty(serializedObject, "_rootParameters");
			_parameterListProperty = HEU_EditorUtility.GetSerializedProperty(serializedObject, "_parameterList");

			_showParametersProperty = HEU_EditorUtility.GetSerializedProperty(serializedObject, "_showParameters");

			_parameterModifiersProperty = HEU_EditorUtility.GetSerializedProperty(serializedObject, "_parameterModifiers");

			// Cache each parameter based on its type
			_parameterCache = new List<HEU_ParameterUICache>();
			int paramCount = _rootParametersProperty.arraySize;
			for (int i = 0; i < paramCount; ++i)
			{
				// Get each root level index
				SerializedProperty elementProperty = _rootParametersProperty.GetArrayElementAtIndex(i);
				int childListIndex = elementProperty.intValue;

				// Find the parameter data associated with the index
				SerializedProperty childParameterProperty = _parameterListProperty.GetArrayElementAtIndex(childListIndex);

				HEU_ParameterUICache newParamUI = ProcessParamUICache(childListIndex, childParameterProperty);
				if(newParamUI != null)
				{
					_parameterCache.Add(newParamUI);
				}
			}
		}

		private HEU_ParameterUICache ProcessParamUICache(int parameterIndex, SerializedProperty parameterProperty)
		{
			// Get the actual parameter data associated with this parameter
			HEU_ParameterData parameterData = _parameters.GetParameter(parameterIndex);

			HEU_ParameterUICache newParamUICache = null;
			if (parameterData.IsMultiParam())
			{
				newParamUICache = ProcessMultiParamUICache(parameterData, parameterProperty);
			}
			else if (parameterData.IsContainer())
			{
				newParamUICache = ProcessContainerParamUICache(parameterData, parameterProperty);
			}
			else
			{
				newParamUICache = ProcessLeafParameterCache(parameterData, parameterProperty);
			}

			return newParamUICache;
		}

		private HEU_ParameterUICache ProcessMultiParamUICache(HEU_ParameterData parameterData, SerializedProperty parameterProperty)
		{
			HEU_ParameterUICache paramUICache = null;

			if (parameterData._parmInfo.rampType == HAPI_RampType.HAPI_RAMPTYPE_COLOR)
			{
				paramUICache = new HEU_ParameterUICache();
				paramUICache._parameterData = parameterData;
				paramUICache._childrenCache = new List<HEU_ParameterUICache>();

				paramUICache._primaryValue = parameterProperty.FindPropertyRelative("_childParameterIDs");
				Debug.Assert(paramUICache._primaryValue != null && paramUICache._primaryValue.arraySize > 0, "Multiparams should have at least 1 child");

				// This is ugly but can't figure out a better way due to Gradient not being easily accessible from a SerializedProperty.
				// Instead, have to wrap it up into a new container, serialize the container, and use the SerializedProperty within.
				// This allows to draw the gradient, then de-serialize it by using ApplyModifierProperties on the container.
				// The other way would be to just use the serialized Parameters object, but that will apply changes to all parameters
				// which isn't ideal as we wouldn't want to update just yet.
				HEU_GradientContainer gc = ScriptableObject.CreateInstance<HEU_GradientContainer>();
				gc._gradient = parameterData._gradient;
				SerializedObject gcSO = new SerializedObject(gc);
				paramUICache._gradientSerializedObject = gcSO;
				paramUICache._secondaryValue = gcSO.FindProperty("_gradient");

				// For ramps, the number of instances is the number of points in the ramp
				// Each point can then have a number of parameters.
				int numPoints = parameterData._parmInfo.instanceCount;
				int numParamsPerPoint = parameterData._parmInfo.instanceLength;
				int childIndex = 0;

				for (int pointIndex = 0; pointIndex < numPoints; ++pointIndex)
				{
					for (int paramIndex = 0; paramIndex < numParamsPerPoint; ++paramIndex)
					{
						SerializedProperty childIndexProperty = paramUICache._primaryValue.GetArrayElementAtIndex(childIndex);
						SerializedProperty childParameterProperty = _parameterListProperty.GetArrayElementAtIndex(childIndexProperty.intValue);

						HEU_ParameterUICache newChildParamUI = ProcessParamUICache(childIndexProperty.intValue, childParameterProperty);
						if(newChildParamUI != null)
						{
							paramUICache._childrenCache.Add(newChildParamUI);

							newChildParamUI._paramIndexForInstance = newChildParamUI._parameterData._parmInfo.childIndex;
							
							// Using instance index to store the int param index
							newChildParamUI._instanceIndex = newChildParamUI._parameterData._parmInfo.instanceNum;
						}

						childIndex++;
					}
				}
			}
			else if (parameterData._parmInfo.rampType == HAPI_RampType.HAPI_RAMPTYPE_FLOAT)
			{
				paramUICache = new HEU_ParameterUICache();
				paramUICache._parameterData = parameterData;
				paramUICache._primaryValue = parameterProperty.FindPropertyRelative("_childParameterIDs");
				paramUICache._secondaryValue = parameterProperty.FindPropertyRelative("_animCurve");
				paramUICache._childrenCache = new List<HEU_ParameterUICache>();

				int numPoints = parameterData._parmInfo.instanceCount;
				int numParamsPerPoint = parameterData._parmInfo.instanceLength;
				int childIndex = 0;

				for (int pointIndex = 0; pointIndex < numPoints; ++pointIndex)
				{
					for (int paramIndex = 0; paramIndex < numParamsPerPoint; ++paramIndex)
					{
						SerializedProperty childIndexProperty = paramUICache._primaryValue.GetArrayElementAtIndex(childIndex);
						SerializedProperty childParameterProperty = _parameterListProperty.GetArrayElementAtIndex(childIndexProperty.intValue);

						HEU_ParameterUICache newChildParamUI = ProcessParamUICache(childIndexProperty.intValue, childParameterProperty);
						if (newChildParamUI != null)
						{
							paramUICache._childrenCache.Add(newChildParamUI);

							newChildParamUI._paramIndexForInstance = newChildParamUI._parameterData._parmInfo.childIndex;

							// Using instance index to store the int param index
							newChildParamUI._instanceIndex = newChildParamUI._parameterData._parmInfo.instanceNum;
						}

						childIndex++;
					}
				}
				
			}
			else
			{
				paramUICache = new HEU_ParameterUICache();
				paramUICache._parameterData = parameterData;

				paramUICache._primaryValue = parameterProperty.FindPropertyRelative("_childParameterIDs");

				if (paramUICache._primaryValue != null)
				{
					paramUICache._childrenCache = new List<HEU_ParameterUICache>();

					for (int i = 0; i < paramUICache._primaryValue.arraySize; ++i)
					{
						SerializedProperty childIndexProperty = paramUICache._primaryValue.GetArrayElementAtIndex(i);
						int childIndex = childIndexProperty.intValue;
						SerializedProperty childParameterProperty = _parameterListProperty.GetArrayElementAtIndex(childIndex);
						if (childParameterProperty != null)
						{
							HEU_ParameterUICache newChildParamUI = ProcessParamUICache(childIndex, childParameterProperty);

							if (newChildParamUI != null)
							{
								paramUICache._childrenCache.Add(newChildParamUI);

								newChildParamUI._instanceIndex = newChildParamUI._parameterData._parmInfo.instanceNum;
								newChildParamUI._paramIndexForInstance = newChildParamUI._parameterData._parmInfo.childIndex;
							}
						}
					}
				}
			}

			return paramUICache;
		}

		private HEU_ParameterUICache ProcessContainerParamUICache(HEU_ParameterData parameterData, SerializedProperty parameterProperty)
		{
			HEU_ParameterUICache paramUICache = new HEU_ParameterUICache();
			paramUICache._parameterData = parameterData;

			//Debug.LogFormat("Container: name={0}, type={1}, size={2}, chidlren={3}", parameterData._name, parameterData._parmInfo.type, parameterData._parmInfo.size, parameterData._childParameterIDs.Count);

			paramUICache._primaryValue = parameterProperty.FindPropertyRelative("_childParameterIDs");
			if (paramUICache._primaryValue != null && paramUICache._primaryValue.arraySize > 0)
			{
				paramUICache._secondaryValue = parameterProperty.FindPropertyRelative("_showChildren");
				paramUICache._childrenCache = new List<HEU_ParameterUICache>();

				// Process child parameters
				for (int i = 0; i < paramUICache._primaryValue.arraySize; ++i)
				{
					SerializedProperty childIndexProperty = paramUICache._primaryValue.GetArrayElementAtIndex(i);
					int childIndex = childIndexProperty.intValue;
					SerializedProperty childParameterProperty = _parameterListProperty.GetArrayElementAtIndex(childIndex);
					if (childParameterProperty != null)
					{
						HEU_ParameterUICache newChildUICache = ProcessParamUICache(childIndex, childParameterProperty);
						if(newChildUICache != null)
						{
							paramUICache._childrenCache.Add(newChildUICache);
						}
					}
				}

				// Fill in children (folder) labels for folder list
				if (paramUICache._parameterData._parmInfo.type == HAPI_ParmType.HAPI_PARMTYPE_FOLDERLIST)
				{
					paramUICache._tabLabels = new string[paramUICache._childrenCache.Count];
					for (int i = 0; i < paramUICache._childrenCache.Count; ++i)
					{
						paramUICache._tabLabels[i] = paramUICache._childrenCache[i]._parameterData._labelName;
					}

					paramUICache._secondaryValue = parameterProperty.FindPropertyRelative("_tabSelectedIndex");
				}
			}

			return paramUICache;
		}

		private HEU_ParameterUICache ProcessLeafParameterCache(HEU_ParameterData parameterData, SerializedProperty parameterProperty)
		{
			HEU_ParameterUICache paramUICache = new HEU_ParameterUICache();
			paramUICache._parameterData = parameterData;

			paramUICache._paramType = parameterProperty.FindPropertyRelative("_parmInfo.type");
			HAPI_ParmType parmType = (HAPI_ParmType)paramUICache._paramType.intValue;

			if (parmType == HAPI_ParmType.HAPI_PARMTYPE_INT)
			{
				paramUICache._primaryValue = parameterProperty.FindPropertyRelative("_intValues");

				if (parameterData._parmInfo.choiceCount > 0)
				{
					paramUICache._secondaryValue = parameterProperty.FindPropertyRelative("_choiceValue");
				}
			}
			else if (parmType == HAPI_ParmType.HAPI_PARMTYPE_FLOAT)
			{
				paramUICache._primaryValue = parameterProperty.FindPropertyRelative("_floatValues");
			}
			else if (parmType == HAPI_ParmType.HAPI_PARMTYPE_STRING)
			{
				paramUICache._primaryValue = parameterProperty.FindPropertyRelative("_stringValues");

				if (parameterData.IsAssetPath())
				{
					// For asset paths, load and cache the assets if we have valid paths.
					int numItems = paramUICache._primaryValue.arraySize;
					paramUICache._assetObjects = new List<Object>(numItems);
					for(int i = 0; i < numItems; ++i)
					{
						SerializedProperty parmProperty = paramUICache._primaryValue.GetArrayElementAtIndex(i);
						if (!string.IsNullOrEmpty(parmProperty.stringValue))
						{
							paramUICache._assetObjects.Add(HEU_AssetDatabase.LoadAssetAtPath(parmProperty.stringValue, typeof(UnityEngine.Object)));
						}
						else
						{
							paramUICache._assetObjects.Add(null);
						}
					}
				}
				else if (parameterData._parmInfo.choiceCount > 0)
				{
					paramUICache._secondaryValue = parameterProperty.FindPropertyRelative("_choiceValue");
				}
			}
			else if (parmType == HAPI_ParmType.HAPI_PARMTYPE_TOGGLE)
			{
				paramUICache._primaryValue = parameterProperty.FindPropertyRelative("_toggle");
			}
			else if (parmType == HAPI_ParmType.HAPI_PARMTYPE_COLOR)
			{
				paramUICache._primaryValue = parameterProperty.FindPropertyRelative("_color");
			}
			else if (parmType == HAPI_ParmType.HAPI_PARMTYPE_BUTTON)
			{
				paramUICache._primaryValue = parameterProperty.FindPropertyRelative("_intValues");
			}
			else if (parmType == HAPI_ParmType.HAPI_PARMTYPE_PATH_FILE || parmType == HAPI_ParmType.HAPI_PARMTYPE_PATH_FILE_GEO
				|| parmType == HAPI_ParmType.HAPI_PARMTYPE_PATH_FILE_DIR || parmType == HAPI_ParmType.HAPI_PARMTYPE_PATH_FILE_IMAGE)
			{
				paramUICache._primaryValue = parameterProperty.FindPropertyRelative("_stringValues");
			}
			else if (parmType == HAPI_ParmType.HAPI_PARMTYPE_NODE)
			{
				paramUICache._primaryValue = parameterProperty.FindPropertyRelative("_paramInputNode");
			}
			else if (parmType == HAPI_ParmType.HAPI_PARMTYPE_LABEL)
			{
				paramUICache._primaryValue = parameterProperty.FindPropertyRelative("_stringValues");
			}
			else if(parmType == HAPI_ParmType.HAPI_PARMTYPE_SEPARATOR)
			{
				// Allow these
			}
			else
			{
				// Ignore all others (null out the cache)
				paramUICache = null;
			}

			return paramUICache;
		}

		private void DrawParamUICache(HEU_ParameterUICache paramCache, bool bDrawFoldout = true)
		{
			using (var disabledGroup = new EditorGUI.DisabledGroupScope(paramCache._parameterData._parmInfo.disabled))
			{
				if (paramCache._parameterData.IsMultiParam())
				{
					DrawMultiParamUICache(paramCache);
				}
				else if (paramCache._parameterData.IsContainer())
				{
					DrawContainerParamUICache(paramCache, bDrawFoldout);
				}
				else
				{
					DrawLeafParamUICache(paramCache);
				}
			}
		}

		private void DrawArrayPropertyStringPath(string labelString, SerializedProperty arrayProperty, List<UnityEngine.Object> assetObjects)
		{
			// Arrays are drawn with a label, and rows of object paths.

			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.PrefixLabel(labelString);

				using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
				{
					int numElements = arrayProperty.arraySize;
					int maxElementsPerRow = 4;

					GUILayout.BeginHorizontal();
					{
						for (int i = 0; i < numElements; ++i)
						{
							if (i > 0 && i % maxElementsPerRow == 0)
							{
								GUILayout.EndHorizontal();
								GUILayout.BeginHorizontal();
							}

							UnityEngine.Object newAssetObject = null;
							if (i < assetObjects.Count)
							{
								newAssetObject = EditorGUILayout.ObjectField(assetObjects[i], typeof(UnityEngine.Object), false);
								if (newAssetObject != assetObjects[i])
								{
									// Since its just a string parm, we only need to store the path to asset
									arrayProperty.GetArrayElementAtIndex(i).stringValue = HEU_AssetDatabase.GetAssetPath(newAssetObject);
									assetObjects[i] = newAssetObject;
								}
							}
							else
							{
								arrayProperty.GetArrayElementAtIndex(i).stringValue = null;
							}
						}
					}
					GUILayout.EndHorizontal();
				}
			}
		}

		private void DrawArrayPropertyString(string labelString, SerializedProperty arrayProperty)
		{
			// Arrays are drawn with a label, and rows of values.

			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.PrefixLabel(labelString);

				using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
				{
					int numElements = arrayProperty.arraySize;
					int maxElementsPerRow = 4;

					GUILayout.BeginHorizontal();
					{
						for (int i = 0; i < numElements; ++i)
						{
							if (i > 0 && i % maxElementsPerRow == 0)
							{
								GUILayout.EndHorizontal();
								GUILayout.BeginHorizontal();
							}

							EditorGUILayout.DelayedTextField(arrayProperty.GetArrayElementAtIndex(i), GUIContent.none);
						}
					}
					GUILayout.EndHorizontal();
				}
			}
		}

		private void DrawArrayPropertyInt(string labelString, SerializedProperty arrayProperty)
		{
			// Arrays are drawn with a label, and rows of values.

			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.PrefixLabel(labelString);

				using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
				{
					int numElements = arrayProperty.arraySize;
					int maxElementsPerRow = 4;

					GUILayout.BeginHorizontal();
					{
						for (int i = 0; i < numElements; ++i)
						{
							if (i > 0 && i % maxElementsPerRow == 0)
							{
								GUILayout.EndHorizontal();
								GUILayout.BeginHorizontal();
							}

							EditorGUILayout.DelayedIntField(arrayProperty.GetArrayElementAtIndex(i), GUIContent.none);
						}
					}
					GUILayout.EndHorizontal();
				}
			}
		}

		private void DrawArrayPropertyFloat(string labelString, SerializedProperty arrayProperty)
		{
			// Arrays are drawn with a label, and rows of values.

			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.PrefixLabel(labelString);

				using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
				{
					int numElements = arrayProperty.arraySize;
					int maxElementsPerRow = 4;

					GUILayout.BeginHorizontal();
					{
						for (int i = 0; i < numElements; ++i)
						{
							if (i > 0 && i % maxElementsPerRow == 0)
							{
								GUILayout.EndHorizontal();
								GUILayout.BeginHorizontal();
							}

							EditorGUILayout.DelayedFloatField(arrayProperty.GetArrayElementAtIndex(i), GUIContent.none);
						}
					}
					GUILayout.EndHorizontal();
				}
			}
		}

		private void DrawContainerParamUICache(HEU_ParameterUICache paramUICache, bool bDrawFoldout = true)
		{
			HEU_ParameterData parameterData = paramUICache._parameterData;

			if (paramUICache._childrenCache != null)
			{
				if (paramUICache._parameterData._parmInfo.type == HAPI_ParmType.HAPI_PARMTYPE_FOLDERLIST && paramUICache._childrenCache.Count > 1)
				{
					// For folder list with multiple children, draw as tabs

					EditorGUILayout.BeginVertical(EditorStyles.helpBox);
					{
						int tabChoice = GUILayout.Toolbar(paramUICache._secondaryValue.intValue, paramUICache._tabLabels);
						if (tabChoice >= 0 && tabChoice < paramUICache._childrenCache.Count)
						{
							paramUICache._secondaryValue.intValue = tabChoice;
							DrawParamUICache(paramUICache._childrenCache[paramUICache._secondaryValue.intValue], false);
						}
					}
					EditorGUILayout.EndVertical();
				}
				else
				{
					// Otherwise draw as foldout, unless requested not to (latter case when drawing folder under a folder list)

					// If folder list, then only have 1 child, so don't draw as foldout. Also due to using paramUICache._secondaryValue
					// as tab index rather than foldout show/hide state.
					if (paramUICache._parameterData._parmInfo.type == HAPI_ParmType.HAPI_PARMTYPE_FOLDERLIST || string.IsNullOrEmpty(parameterData._labelName))
					{
						bDrawFoldout = false;
					}

					if (bDrawFoldout)
					{
						EditorGUILayout.BeginVertical(EditorStyles.helpBox);
						paramUICache._secondaryValue.boolValue = EditorGUILayout.Foldout(paramUICache._secondaryValue.boolValue, parameterData._labelName, EditorStyles.foldout);
					}

					if (!bDrawFoldout || paramUICache._secondaryValue.boolValue)
					{
						foreach (HEU_ParameterUICache paramCache in paramUICache._childrenCache)
						{
							DrawParamUICache(paramCache);
						}
					}

					if (bDrawFoldout)
					{
						EditorGUILayout.EndVertical();
					}
				}
			}
		}

		private void DrawLeafParamUICache(HEU_ParameterUICache paramUICache)
		{
			// This is a leaf parameter. So draw based on type.

			HEU_ParameterData parameterData = paramUICache._parameterData;

			HAPI_ParmType parmType = (HAPI_ParmType)paramUICache._paramType.intValue;
			if (parmType == HAPI_ParmType.HAPI_PARMTYPE_INT)
			{
				SerializedProperty intsProperty = paramUICache._primaryValue;

				if (parameterData._parmInfo.choiceCount > 0)
				{
					// Drop-down choice list for INTs

					// Get the current choice value
					SerializedProperty choiceProperty = paramUICache._secondaryValue;

					// Draw it as an int popup, passing in the user options (_choiceLabels), and the corresponding Houdini values (_choiceIntValues)
					EditorGUILayout.IntPopup(choiceProperty, parameterData._choiceLabels, parameterData._choiceIntValues, new GUIContent(parameterData._labelName));

					// No need to check, just always updated with latest choiceProperty
					if (intsProperty.GetArrayElementAtIndex(0).intValue != parameterData._choiceIntValues[choiceProperty.intValue])
					{
						//Debug.LogFormat("Setting int property {0} from {1} to {2}", parameterData._labelName, intsProperty.GetArrayElementAtIndex(0).intValue, parameterData._choiceIntValues[choiceProperty.intValue]);
						intsProperty.GetArrayElementAtIndex(0).intValue = parameterData._choiceIntValues[choiceProperty.intValue];
					}
				}
				else
				{
					if (intsProperty.arraySize == 1)
					{
						bool bHasUIMinMax = (parameterData.HasUIMin() && parameterData.HasUIMax());

						int value = intsProperty.GetArrayElementAtIndex(0).intValue;

						EditorGUILayout.BeginHorizontal();
						{
							value = EditorGUILayout.DelayedIntField(new GUIContent(parameterData._labelName), value, GUILayout.ExpandWidth(!bHasUIMinMax));
							if (bHasUIMinMax)
							{
								value = Mathf.RoundToInt(GUILayout.HorizontalSlider(value, (int)Mathf.Min(value, parameterData.IntUIMin), (int)Mathf.Max(value, parameterData.IntUIMax), _sliderStyle, _sliderThumbStyle));
							}
						}
						EditorGUILayout.EndHorizontal();

						if (parameterData.HasMin())
						{
							value = Mathf.Max(parameterData.IntMin, value);
						}
						if (parameterData.HasMax())
						{
							value = Mathf.Min(parameterData.IntMax, value);
						}
						intsProperty.GetArrayElementAtIndex(0).intValue = value;
					}
					else
					{
						// Multiple ints. Display label, then each element.

						DrawArrayPropertyInt(parameterData._labelName, intsProperty);
					}
				}
			}
			else if (parmType == HAPI_ParmType.HAPI_PARMTYPE_FLOAT)
			{
				SerializedProperty floatsProperty = paramUICache._primaryValue;

				if (floatsProperty.arraySize == 1)
				{
					// Draw single float as either a slider (if min & max are available, or simply as a field)

					bool bHasUIMinMax = (parameterData.HasUIMin() && parameterData.HasUIMax());

					float value = floatsProperty.GetArrayElementAtIndex(0).floatValue;

					EditorGUILayout.BeginHorizontal();
					{
						value = EditorGUILayout.DelayedFloatField(new GUIContent(parameterData._labelName), value, GUILayout.ExpandWidth(!bHasUIMinMax));
						if (bHasUIMinMax)
						{
							value = GUILayout.HorizontalSlider(value, Mathf.Min(value, parameterData.FloatUIMin), Mathf.Max(value, parameterData.FloatUIMax), _sliderStyle, _sliderThumbStyle);
						}
					}
					EditorGUILayout.EndHorizontal();

					if (parameterData.HasMin())
					{
						value = Mathf.Max(parameterData.FloatMin, value);
					}
					if (parameterData.HasMax())
					{
						value = Mathf.Min(parameterData.FloatMax, value);
					}
					floatsProperty.GetArrayElementAtIndex(0).floatValue = value;
				}
				else
				{
					// Multiple floats. Display label, then each element.

					DrawArrayPropertyFloat(parameterData._labelName, floatsProperty);
				}

			}
			else if (parmType == HAPI_ParmType.HAPI_PARMTYPE_STRING)
			{
				SerializedProperty stringsProperty = paramUICache._primaryValue;

				if (parameterData._parmInfo.choiceCount > 0)
				{
					// Dropdown choice list for STRINGS

					// Get the current choice value
					SerializedProperty choiceProperty = paramUICache._secondaryValue;

					// Draw it as an int popup, passing in the user options (_choiceLabels), and the corresponding Houdini values (_choiceIntValues)
					EditorGUILayout.IntPopup(choiceProperty, parameterData._choiceLabels, parameterData._choiceIntValues, new GUIContent(parameterData._labelName));

					// choiceProperty.intValue now holds the user's choice, so just update it
					stringsProperty.GetArrayElementAtIndex(0).stringValue = parameterData._choiceStringValues[choiceProperty.intValue];
				}
				else if(parameterData.IsAssetPath())
				{
					DrawArrayPropertyStringPath(parameterData._labelName, stringsProperty, paramUICache._assetObjects);
				}
				else
				{
					// Draw strings as list or singularly, or as asset path
					DrawArrayPropertyString(parameterData._labelName, stringsProperty);
				}
			}
			else if (parmType == HAPI_ParmType.HAPI_PARMTYPE_TOGGLE)
			{
				EditorGUILayout.PropertyField(paramUICache._primaryValue, new GUIContent(parameterData._labelName));
			}
			else if (parmType == HAPI_ParmType.HAPI_PARMTYPE_COLOR)
			{
				EditorGUILayout.PropertyField(paramUICache._primaryValue, new GUIContent(parameterData._labelName));
			}
			else if (parmType == HAPI_ParmType.HAPI_PARMTYPE_BUTTON)
			{
				SerializedProperty intsProperty = paramUICache._primaryValue;
				Debug.Assert(intsProperty.arraySize == 1, "Button parameter property should have only a single value!");

				if (GUILayout.Button(parameterData._labelName))
				{
					intsProperty.GetArrayElementAtIndex(0).intValue = intsProperty.GetArrayElementAtIndex(0).intValue == 0 ? 1 : 0;
				}
			}
			else if (parmType == HAPI_ParmType.HAPI_PARMTYPE_PATH_FILE || parmType == HAPI_ParmType.HAPI_PARMTYPE_PATH_FILE_GEO
			         || parmType == HAPI_ParmType.HAPI_PARMTYPE_PATH_FILE_DIR || parmType == HAPI_ParmType.HAPI_PARMTYPE_PATH_FILE_IMAGE)
			{
				GUIStyle boxStyle = HEU_EditorUI.GetGUIStyle("Groupbox", 4, 0);
				using (var vs = new EditorGUILayout.VerticalScope(boxStyle))
				{
					GUIContent labelContent = new GUIContent(parameterData._labelName);
					EditorGUILayout.LabelField(labelContent);

					EditorGUILayout.BeginHorizontal();

					SerializedProperty stringsProperty = paramUICache._primaryValue;
					Debug.Assert(stringsProperty.arraySize == 1, "File path parameter property should only have a single value!");
					EditorGUILayout.DelayedTextField(stringsProperty.GetArrayElementAtIndex(0), GUIContent.none);

					GUIStyle buttonStyle = HEU_EditorUI.GetNewButtonStyle_MarginPadding(0, 0);
					if (GUILayout.Button("...", buttonStyle, GUILayout.Width(30), GUILayout.Height(18)))
					{
						string filePattern = parameterData._fileTypeInfo;
						if (string.IsNullOrEmpty(filePattern))
						{
							filePattern = "*";
						}
						else
						{
							filePattern.Replace(" ", ";");
							if (filePattern.StartsWith("*."))
							{
								filePattern = filePattern.Substring(2);
							}
							else if (filePattern.StartsWith("*"))
							{
								filePattern = filePattern.Substring(1);
							}
						}

						string userFilePath = null;
						if (parameterData._parmInfo.permissions == HAPI_Permissions.HAPI_PERMISSIONS_WRITE_ONLY)
						{
							if (parmType == HAPI_ParmType.HAPI_PARMTYPE_PATH_FILE_DIR)
							{
								userFilePath = EditorUtility.SaveFolderPanel("Select Folder", stringsProperty.GetArrayElementAtIndex(0).stringValue, "");
							}
							else
							{
								userFilePath = EditorUtility.SaveFilePanel("Select File", stringsProperty.GetArrayElementAtIndex(0).stringValue, "", filePattern);
							}
						}
						else
						{
							if (parmType == HAPI_ParmType.HAPI_PARMTYPE_PATH_FILE_DIR)
							{
								userFilePath = EditorUtility.OpenFolderPanel("Select Folder", stringsProperty.GetArrayElementAtIndex(0).stringValue, "");
							}
							else
							{
								userFilePath = EditorUtility.OpenFilePanel("Select File", stringsProperty.GetArrayElementAtIndex(0).stringValue, filePattern);
							}
						}
						if (!string.IsNullOrEmpty(userFilePath))
						{
							stringsProperty.GetArrayElementAtIndex(0).stringValue = userFilePath;
						}
					}

					EditorGUILayout.EndHorizontal();
				}
			}
			else if (parmType == HAPI_ParmType.HAPI_PARMTYPE_SEPARATOR)
			{
				// Drawing twice give Unity's version more vertical space (similar to Houdini separator size)
				HEU_EditorUI.DrawSeparator();
				HEU_EditorUI.DrawSeparator();
			}
			else if (parmType == HAPI_ParmType.HAPI_PARMTYPE_LABEL)
			{
				//SerializedProperty stringsProperty = paramUICache._primaryValue;
				//HEU_EditorUI.DrawHeadingLabel(stringsProperty.GetArrayElementAtIndex(0).stringValue);
				// Replaced above with this as it seems to be the correct label value
				HEU_EditorUI.DrawHeadingLabel(parameterData._labelName);
			}
			else if (parmType == HAPI_ParmType.HAPI_PARMTYPE_NODE)
			{
				HEU_InputNode inputNode = paramUICache._primaryValue.objectReferenceValue as HEU_InputNode;
				if (inputNode != null)
				{
					HEU_InputNodeUI.EditorDrawInputNode(inputNode);
				}
			}
			else
			{
				//Debug.LogFormat("Param name={0}, type={1}, folderType={2}", parameterData._labelName, parameterData._parmInfo.type, parameterData._parmInfo.fo);
			}
		}

		private void DrawMultiParamUICache(HEU_ParameterUICache paramUICache)
		{
			// Multiparams are drawn in its own section. Not doing foldout for it.

			HEU_ParameterData parameterData = paramUICache._parameterData;

			if (parameterData._parmInfo.rampType == HAPI_RampType.HAPI_RAMPTYPE_COLOR)
			{
				DrawColorRampParamUICache(paramUICache);
			}
			else if (parameterData._parmInfo.rampType == HAPI_RampType.HAPI_RAMPTYPE_FLOAT)
			{
				DrawFloatRampParamUICache(paramUICache);
			}
			else
			{
				// Non-ramp multiparams

				int numInstances = parameterData._parmInfo.instanceCount;
				int oldNumInstances = numInstances;

				int instanceStartOfset = parameterData._parmInfo.instanceStartOffset;

				GUILayout.BeginVertical(EditorStyles.helpBox);
				{
					// Show the top level information for a MultiParam:
					///	MultiParam Label [number of instances]  +  -  Clear
					GUILayout.BeginHorizontal();
					{
						EditorGUILayout.PrefixLabel(parameterData._labelName);
						numInstances = EditorGUILayout.DelayedIntField(numInstances);

						if (GUILayout.Button("+"))
						{
							numInstances++;
						}
						else if (numInstances > 0 && GUILayout.Button("-"))
						{
							numInstances--;
						}
						else if (numInstances > 0 && GUILayout.Button("Clear"))
						{
							numInstances = 0;
						}
					}
					GUILayout.EndHorizontal();

					// If number of instances have changed, add the modifier to handle it after UI is drawn
					if (numInstances == 0 && oldNumInstances > 0)
					{
						// Notify to clear all instances
						AddMultiParmModifierProperty(HEU_ParameterModifier.ModifierAction.MULTIPARM_CLEAR, parameterData._unityIndex, 0, 0);
					}
					else if (oldNumInstances > numInstances)
					{
						// Notify to remove last set of instances
						int removeNum = (oldNumInstances - numInstances);
						AddMultiParmModifierProperty(HEU_ParameterModifier.ModifierAction.MULTIPARM_REMOVE, parameterData._unityIndex, instanceStartOfset + numInstances, removeNum);
					}
					else if (oldNumInstances < numInstances)
					{
						// Notify to insert new instances at end
						AddMultiParmModifierProperty(HEU_ParameterModifier.ModifierAction.MULTIPARM_INSERT, parameterData._unityIndex, instanceStartOfset + oldNumInstances, (numInstances - oldNumInstances));
					}
					else if (numInstances == parameterData._parmInfo.instanceCount)
					{
						// As long as number of instances haven't changed, we can draw them

						for (int i = 0; i < paramUICache._childrenCache.Count; ++i)
						{
							HEU_ParameterUICache childUICache = paramUICache._childrenCache[i];
							GUILayout.BeginHorizontal();
							{
								// TODO: Note bug #85329 regarding nested multiparm add/remove
								if(childUICache._paramIndexForInstance == 0)
								{
									if (GUILayout.Button("x", GUILayout.Width(_multiparmPlusMinusWidth)))
									{
										// Notify to remove this instance
										AddMultiParmModifierProperty(HEU_ParameterModifier.ModifierAction.MULTIPARM_REMOVE, parameterData._unityIndex, childUICache._instanceIndex, 1);

										break;
									}
									else if (GUILayout.Button("+", GUILayout.Width(_multiparmPlusMinusWidth)))
									{
										// Notify to insert new instance before this one
										AddMultiParmModifierProperty(HEU_ParameterModifier.ModifierAction.MULTIPARM_INSERT, parameterData._unityIndex, childUICache._instanceIndex, 1);

										break;
									}
								}
								
							}
							DrawParamUICache(childUICache);

							GUILayout.EndHorizontal();
						}
					}
				}
				GUILayout.EndVertical();
			}
		}

		private void DrawColorRampParamUICache(HEU_ParameterUICache paramUICache)
		{
			// For a color ramp:
			//	multiparm # instances is the points on ramp (don't show that, but use as # of points)
			//	x , + is on the left side, with ramp color on right
			//	Point No.	: current point ID
			//	Position	: current point's position
			//	Color		: color value
			//	Interpolation: current point interpolation

			SerializedProperty childParameterIDsProperty = paramUICache._primaryValue;
			Debug.Assert(childParameterIDsProperty != null && childParameterIDsProperty.arraySize > 0, "Multiparams should have at least 1 child");

			SerializedProperty gradientProperty = paramUICache._secondaryValue;

			HEU_ParameterData parameterData = paramUICache._parameterData;

			// For ramps, the number of instances is the number of points in the ramp
			// Each point can then have a number of parameters.

			using (var vs = new GUILayout.VerticalScope(EditorStyles.helpBox))
			{
				EditorGUILayout.PrefixLabel(parameterData._labelName);

				// Draw the Gradient and handle changes. Points are drawn after the Gradient.
				// Note that individual points and their properties are only drawn if the Gradient didn't
				// change in this draw call. If it changed, we skip drawing points because the Gradient changes
				// will be overriden by drawing the points.

				string rampInterpolationInfo = "This Gradient only supports Blend (Linear) and Fixed (Constant) interpolation modes!"
					+ "\nChanging the Gradient's interpolation mode will affect all points."
					+ "\nUse the drop-down for each point below to set interpolation individually.";

				GradientMode previousGradientMode = parameterData._gradient.mode;
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(gradientProperty, GUIContent.none, GUILayout.Height(40));
				EditorGUILayout.LabelField(rampInterpolationInfo, GUILayout.Height(50));
				if (EditorGUI.EndChangeCheck())
				{
					gradientProperty.serializedObject.ApplyModifiedProperties();
					//paramUICache._gradientSerializedObject.ApplyModifiedProperties();

					Gradient gradient = parameterData._gradient;
					UpdateParameterFromGradient(gradient, parameterData, paramUICache, (previousGradientMode != gradient.mode));
				}
				else
				{
					int numParamsPerPoint = paramUICache._parameterData._parmInfo.instanceLength;

					HEU_EditorUI.DrawSeparator();

					// Display the points as a vertical list. Each point is stored in flattened child list.
					for (int childIndex = 0; childIndex < paramUICache._childrenCache.Count; ++childIndex)
					{
						HEU_ParameterUICache childUICache = paramUICache._childrenCache[childIndex];

						if (childUICache._paramIndexForInstance == 0 && (childIndex > 0))
						{
							HEU_EditorUI.DrawHorizontalLine();
						}

						GUILayout.BeginHorizontal();
						if (childUICache._paramIndexForInstance == 0)
						{
							// This is the start of a point

							GUILayout.Space(10);

							// Draw the add and remove for each point
							if (GUILayout.Button("x", GUILayout.Width(25), GUILayout.Height(18)))
							{
								AddMultiParmModifierProperty(HEU_ParameterModifier.ModifierAction.MULTIPARM_REMOVE, parameterData._unityIndex, childUICache._instanceIndex, 1);
								break;
							}
							else if (GUILayout.Button("+", GUILayout.Width(25), GUILayout.Height(18)))
							{
								// When adding new point, we add command to modifier list, then set the half-way values for the new point

								float curPos = 0;
								float nextPos = 0;
								Color curVal = Color.white;
								Color nextVal = Color.white;
								int interp = 0;

								// New point will be added just after current
								int newPointInstanceIndex = childUICache._instanceIndex + 1;

								// Get the 2 points that neighbour the new point and use half their values for the new point
								int pointIndex = childIndex / numParamsPerPoint;
								if ((pointIndex + 1) * numParamsPerPoint < paramUICache._childrenCache.Count)
								{
									GetColorRampPointData(paramUICache, pointIndex, ref curPos, ref curVal, ref interp);
									GetColorRampPointData(paramUICache, pointIndex + 1, ref nextPos, ref nextVal, ref interp);
								}
								else if (childIndex + numParamsPerPoint >= paramUICache._childrenCache.Count)
								{
									// The new point will be added before the last point
									newPointInstanceIndex -= 1;

									GetColorRampPointData(paramUICache, pointIndex - 1, ref curPos, ref curVal, ref interp);
									GetColorRampPointData(paramUICache, pointIndex, ref nextPos, ref nextVal, ref interp);
								}

								float newPos = (nextPos + curPos) * 0.5f;
								Color newVal = Color.Lerp(curVal, nextVal, 0.5f);

								AddMultiParmModifierProperty(HEU_ParameterModifier.ModifierAction.MULTIPARM_INSERT, parameterData._unityIndex, newPointInstanceIndex, 1);

								// Add modifier to update the new point that was just added with position, value, and interp settings
								// Color ramps have 4 floats (position, color3)
								int numFloatsPerPoint = 4;
								int newParamIndex = (newPointInstanceIndex - paramUICache._parameterData._parmInfo.instanceStartOffset) * numFloatsPerPoint;
								AddMultiParmModifierPropertyFloat(parameterData._unityIndex, newParamIndex, 1, newPos);

								AddMultiParmModifierPropertyFloat(parameterData._unityIndex, newParamIndex + 1, 1, newVal[0]);
								AddMultiParmModifierPropertyFloat(parameterData._unityIndex, newParamIndex + 2, 1, newVal[1]);
								AddMultiParmModifierPropertyFloat(parameterData._unityIndex, newParamIndex + 3, 1, newVal[2]);

								int numIntsPerPoint = 1;
								int newParamIntIndex = (newPointInstanceIndex - paramUICache._parameterData._parmInfo.instanceStartOffset) * numIntsPerPoint;
								int gradientMode = HEU_HAPIUtility.GradientModeToHoudiniColorRampInterpolation(previousGradientMode);
								AddMultiParmModifierPropertyInt(parameterData._unityIndex, newParamIntIndex + 1, 1, gradientMode);

								break;
							}

							DrawParamUICache(childUICache);
						}
						else
						{
							GUILayout.Space(68);

							DrawParamUICache(childUICache);
						}
						GUILayout.EndHorizontal();
					}
				}
			}
		}

		private void DrawFloatRampParamUICache(HEU_ParameterUICache paramUICache)
		{
			// For a float ramp:
			//	multiparm # instances is the points on ramp (don't show that, but thats # of points)
			//	x , + is on the left side, with ramp color on right
			//	Point No.	: current point ID (with slider)
			//	Position	: current point's position (with slider)
			//	Value		: float value (with slider 0 to 1)
			//	Interpolation: current point interpolation

			SerializedProperty animCurveProperty = paramUICache._secondaryValue;

			HEU_ParameterData parameterData = paramUICache._parameterData;

			using (var vs = new GUILayout.VerticalScope(EditorStyles.helpBox))
			{
				EditorGUILayout.PrefixLabel(parameterData._labelName);

				string rampInterpolationInfo = "Ramp only supports Constant, Linear, and Free (Catmull-Rom) tangent modes!"
					+ "\nOnly the Right Tangent mode for a point is supported."
					+ "\nUse the drop-down for each point below to set other smooth interpolation modes.";

				// Draw the Animation Curve and handle changes
				EditorGUI.BeginChangeCheck();
				animCurveProperty.animationCurveValue = EditorGUILayout.CurveField(animCurveProperty.animationCurveValue, GUILayout.Height(50));
				EditorGUILayout.LabelField(rampInterpolationInfo, GUILayout.Height(50));
				if (EditorGUI.EndChangeCheck())
				{
					AnimationCurve animCurve = animCurveProperty.animationCurveValue;
					UpdateParameterFromAnimationCurve(animCurve, parameterData, paramUICache);
					return;
				}
				else
				{
					int numParamsPerPoint = paramUICache._parameterData._parmInfo.instanceLength;

					HEU_EditorUI.DrawSeparator();

					// Display the points as a vertical list. Each point is stored in flattened child list.
					for (int childIndex = 0; childIndex < paramUICache._childrenCache.Count; ++childIndex)
					{
						HEU_ParameterUICache childUICache = paramUICache._childrenCache[childIndex];

						if (childUICache._paramIndexForInstance == 0 && (childIndex > 0))
						{
							HEU_EditorUI.DrawHorizontalLine();
						}

						GUILayout.BeginHorizontal();
						if (childUICache._paramIndexForInstance == 0)
						{
							// This is the start of a point

							GUILayout.Space(10);

							// Draw the add and remove for each point
							if (GUILayout.Button("x", GUILayout.Width(25), GUILayout.Height(18)))
							{
								AddMultiParmModifierProperty(HEU_ParameterModifier.ModifierAction.MULTIPARM_REMOVE, parameterData._unityIndex, childUICache._instanceIndex, 1);
								break;
							}
							else if (GUILayout.Button("+", GUILayout.Width(25), GUILayout.Height(18)))
							{
								// When adding new point, we add command to modifier list, then set the half-way values for the new point

								float curPos = 0;
								float nextPos = 0;
								float curVal = 0;
								float nextVal = 0;
								int interp = 0;

								// New point will be added just after current
								int newPointInstanceIndex = childUICache._instanceIndex + 1;

								// Get the 2 points that neighbour the new point and use half their values for the new point
								int pointIndex = childIndex / numParamsPerPoint;
								if ((pointIndex + 1) * numParamsPerPoint < paramUICache._childrenCache.Count)
								{
									GetFloatRampPointData(paramUICache, pointIndex, ref curPos, ref curVal, ref interp);
									GetFloatRampPointData(paramUICache, pointIndex + 1, ref nextPos, ref nextVal, ref interp);
								}
								else if (childIndex + numParamsPerPoint >= paramUICache._childrenCache.Count)
								{
									// The new point will be added before the last point
									newPointInstanceIndex -= 1;

									GetFloatRampPointData(paramUICache, pointIndex - 1, ref curPos, ref curVal, ref interp);
									GetFloatRampPointData(paramUICache, pointIndex, ref nextPos, ref nextVal, ref interp);
								}

								float newPos = (nextPos + curPos) * 0.5f;
								float newVal = (nextVal + curVal) * 0.5f;

								AddMultiParmModifierProperty(HEU_ParameterModifier.ModifierAction.MULTIPARM_INSERT, parameterData._unityIndex, newPointInstanceIndex, 1);

								int newParamIndexOffset = (newPointInstanceIndex - paramUICache._parameterData._parmInfo.instanceStartOffset);

								// Add modifier to update the new point that was just added with position, value, and interp settings
								// Ramps have 2 floats (position, value)
								int numFloatsPerPoint = 2;
								int newParamFloatIndex = newParamIndexOffset * numFloatsPerPoint;
								AddMultiParmModifierPropertyFloat(parameterData._unityIndex, newParamFloatIndex, 1, newPos);
								AddMultiParmModifierPropertyFloat(parameterData._unityIndex, newParamFloatIndex + 1, 1, newVal);

								break;
							}

							DrawParamUICache(childUICache);
						}
						else
						{
							GUILayout.Space(68);

							DrawParamUICache(childUICache);
						}
						GUILayout.EndHorizontal();
					}
				}
			}
		}

		private void UpdateParameterFromAnimationCurve(AnimationCurve animCurve,  HEU_ParameterData parameterData, HEU_ParameterUICache paramUICache)
		{
			// The animation curve has changed, but there isn't a direct way to see what exactly has changed.
			// So compare points to figure out whether points were added, removed, or values changed.

			Keyframe[] keys = animCurve.keys;
			int numKeys = keys.Length;

			int numPoints = parameterData._parmInfo.instanceCount;
			int numParamsPerPoint = parameterData._parmInfo.instanceLength;

			if (numKeys == numPoints && numPoints > 0)
			{
				// Same number of points, but the values might have changed, so update all values

				if (numKeys != (paramUICache._childrenCache.Count / numParamsPerPoint))
				{
					Debug.LogErrorFormat("Ramp parameter has mismatched number of points with UI (got {0}, expected {1}). Unable to update ramp.", (paramUICache._childrenCache.Count / numParamsPerPoint), numKeys);
					return;
				}

				for (int i = 0; i < numKeys; ++i)
				{
					int childIndex = i * numParamsPerPoint;

					paramUICache._childrenCache[childIndex]._primaryValue.GetArrayElementAtIndex(0).floatValue = keys[i].time;
					paramUICache._childrenCache[childIndex + 1]._primaryValue.GetArrayElementAtIndex(0).floatValue = keys[i].value;

					// Only supporting the following tangement modes, with corresponding Houdini equivalent of:
					//	Linear		-> Linear
					//	Constant	-> Constant
					//	Free		-> Catmull-Rom
					AnimationUtility.TangentMode rightTangentMode = AnimationUtility.GetKeyRightTangentMode(animCurve, i);
					int interpolateMode = HEU_HAPIUtility.TangentModeToHoudiniRampInterpolation(rightTangentMode);
					paramUICache._childrenCache[childIndex + 2]._primaryValue.GetArrayElementAtIndex(0).intValue = interpolateMode;
				}
			}
			else if (numKeys > numPoints)
			{
				// Add point

				// Check for different position/time, and add the first point that differs.
				// Only supporting 1 point being added at a time as that is what the UI does.
				int newPointInstanceIndex = -1;
				int keyIndex = -1;
				for (int i = 0; i < numKeys; ++i)
				{
					int childIndex = i * numParamsPerPoint;

					if (childIndex < paramUICache._childrenCache.Count)
					{
						if (paramUICache._childrenCache[childIndex]._primaryValue.GetArrayElementAtIndex(0).floatValue != keys[i].time)
						{
							newPointInstanceIndex = paramUICache._childrenCache[childIndex]._instanceIndex;
							keyIndex = i;

							break;
						}
					}
					else
					{
						newPointInstanceIndex = childIndex > 0 ? paramUICache._childrenCache[childIndex - 1]._instanceIndex + 1 : 0;
						keyIndex = i;
						break;
					}
				}

				if (newPointInstanceIndex > -1 && keyIndex > -1)
				{
					AddMultiParmModifierProperty(HEU_ParameterModifier.ModifierAction.MULTIPARM_INSERT, parameterData._unityIndex, newPointInstanceIndex, 1);

					int pointIndexOffset = (newPointInstanceIndex - paramUICache._parameterData._parmInfo.instanceStartOffset);

					// Add modifier to update the new point that was just added with position, value, and interp settings
					// Ramps have 2 floats (position, value)
					int numFloatsPerPoint = 2;
					int newParamFloatIndex = pointIndexOffset * numFloatsPerPoint;
					AddMultiParmModifierPropertyFloat(parameterData._unityIndex, newParamFloatIndex, 1, keys[keyIndex].time);
					AddMultiParmModifierPropertyFloat(parameterData._unityIndex, newParamFloatIndex + 1, 1, keys[keyIndex].value);

					int numIntsPerPoint = 1;
					int newParamIntIndex = pointIndexOffset * numIntsPerPoint;
					AnimationUtility.TangentMode rightTangentMode = AnimationUtility.GetKeyRightTangentMode(animCurve, keyIndex);
					int interpolateMode = HEU_HAPIUtility.TangentModeToHoudiniRampInterpolation(rightTangentMode);
					AddMultiParmModifierPropertyInt(parameterData._unityIndex, newParamIntIndex + 1, 1, interpolateMode);
				}
			}
			else if(numKeys < numPoints)
			{
				// Remove point

				// Check for different position/time value and remove the first point that differs.
				int keyIndex = 0;
				int numRemoved = 0;
				for(int i = 0; i < numPoints; ++i)
				{
					int childIndex = i * numParamsPerPoint;

					if(keyIndex < numKeys)
					{
						if (paramUICache._childrenCache[childIndex]._primaryValue.GetArrayElementAtIndex(0).floatValue != keys[keyIndex].time)
						{
							// As we remove items, the indices will shift, so need to account for it by subtracting num items removed
							int correctedIndex = paramUICache._childrenCache[childIndex]._instanceIndex - numRemoved;
							numRemoved++;
							AddMultiParmModifierProperty(HEU_ParameterModifier.ModifierAction.MULTIPARM_REMOVE, parameterData._unityIndex, correctedIndex, 1);
						}
						else
						{
							keyIndex++;
						}
					}
					else
					{
						// Went past the number of keys on curve, so any point from here on should be removed
						int correctedIndex = paramUICache._childrenCache[childIndex]._instanceIndex - numRemoved;
						numRemoved++;
						AddMultiParmModifierProperty(HEU_ParameterModifier.ModifierAction.MULTIPARM_REMOVE, parameterData._unityIndex, correctedIndex, 1);
					}
				}
			}
		}

		private void UpdateParameterFromGradient(Gradient gradient, HEU_ParameterData parameterData, HEU_ParameterUICache paramUICache, bool bUpdateGradientMode)
		{
			// The gradient has changed, but there isn't a direct way to see which keys have changed.
			// So compare keys to figure out whether keys were added, removed, or values changed.

			GradientColorKey[] colorKeys = gradient.colorKeys;
			int numKeys = colorKeys.Length;

			int numPoints = parameterData._parmInfo.instanceCount;
			int numParamsPerPoint = parameterData._parmInfo.instanceLength;

			int gradientValue = (gradient.mode == GradientMode.Blend) ? 1 : 0;

			if (numKeys == numPoints && numPoints > 0)
			{
				// Same number of points, but the values might have changed, so update all values

				if (numKeys != (paramUICache._childrenCache.Count / numParamsPerPoint))
				{
					Debug.LogErrorFormat("Ramp parameter has mismatched number of points with UI (got {0}, expected {1}). Unable to update ramp.", (paramUICache._childrenCache.Count / numParamsPerPoint), numKeys);
					return;
				}

				for (int i = 0; i < numKeys; ++i)
				{
					int childIndex = i * numParamsPerPoint;

					paramUICache._childrenCache[childIndex]._primaryValue.GetArrayElementAtIndex(0).floatValue = colorKeys[i].time;
					paramUICache._childrenCache[childIndex + 1]._primaryValue.colorValue = colorKeys[i].color;

					// Unity has a single blend mode for the entire gradient. Houdini supports each color having its own blend mode.
					// The compromise is then only change blend mode in Houdini (for all points) if it was changed in Unity.
					if (bUpdateGradientMode)
					{
						// Update the choice array which will then update 
						paramUICache._childrenCache[childIndex + 2]._primaryValue.GetArrayElementAtIndex(0).intValue = gradientValue;
					}
				}
			}
			else if (numKeys > numPoints)
			{
				// Add point

				// Check for different position/time, and add the first point that differs.
				// Only supporting 1 point being added at a time as that is what the UI does.
				int newPointInstanceIndex = -1;
				int keyIndex = -1;
				for (int i = 0; i < numKeys; ++i)
				{
					int childIndex = i * numParamsPerPoint;

					if (childIndex < paramUICache._childrenCache.Count)
					{
						if (paramUICache._childrenCache[childIndex]._primaryValue.GetArrayElementAtIndex(0).floatValue != colorKeys[i].time)
						{
							newPointInstanceIndex = paramUICache._childrenCache[childIndex]._instanceIndex;
							keyIndex = i;

							break;
						}
					}
					else
					{
						newPointInstanceIndex = childIndex > 0 ? paramUICache._childrenCache[childIndex - 1]._instanceIndex + 1 : 0;
						keyIndex = i;
						break;
					}
				}

				if (newPointInstanceIndex > -1 && keyIndex > -1)
				{
					AddMultiParmModifierProperty(HEU_ParameterModifier.ModifierAction.MULTIPARM_INSERT, parameterData._unityIndex, newPointInstanceIndex, 1);

					// Add modifier to update the new point that was just added with position, value, and interp settings
					// Ramps have 4 floats (position, color3)
					int numFloatsPerPoint = 4;
					int newParamFloatIndex = (newPointInstanceIndex - paramUICache._parameterData._parmInfo.instanceStartOffset) * numFloatsPerPoint;
					AddMultiParmModifierPropertyFloat(parameterData._unityIndex, newParamFloatIndex, 1, colorKeys[keyIndex].time);

					AddMultiParmModifierPropertyFloat(parameterData._unityIndex, newParamFloatIndex + 1, 1, colorKeys[keyIndex].color[0]);
					AddMultiParmModifierPropertyFloat(parameterData._unityIndex, newParamFloatIndex + 2, 1, colorKeys[keyIndex].color[1]);
					AddMultiParmModifierPropertyFloat(parameterData._unityIndex, newParamFloatIndex + 3, 1, colorKeys[keyIndex].color[2]);

					// Unity has a single blend mode for the entire gradient. Houdini supports each color having its own blend mode.
					// For the new point, we use the same gradient mode as rest of gradient
					int numIntsPerPoint = 1;
					int newParamIntIndex = (newPointInstanceIndex - paramUICache._parameterData._parmInfo.instanceStartOffset) * numIntsPerPoint;
					int gradientMode = HEU_HAPIUtility.GradientModeToHoudiniColorRampInterpolation(gradient.mode);
					AddMultiParmModifierPropertyInt(parameterData._unityIndex, newParamIntIndex + 1, 1, gradientMode);
				}
			}
			else if (numKeys < numPoints)
			{
				// Remove point

				// Check for different position/time value and remove the first point that differs.
				int keyIndex = 0;
				int numRemoved = 0;
				for (int i = 0; i < numPoints; ++i)
				{
					int childIndex = i * numParamsPerPoint;

					if (keyIndex < numKeys)
					{
						if (paramUICache._childrenCache[childIndex]._primaryValue.GetArrayElementAtIndex(0).floatValue != colorKeys[keyIndex].time)
						{
							// As we remove items, the indices will shift, so need to account for it by subtracting num items removed
							int correctedIndex = paramUICache._childrenCache[childIndex]._instanceIndex - numRemoved;
							numRemoved++;
							AddMultiParmModifierProperty(HEU_ParameterModifier.ModifierAction.MULTIPARM_REMOVE, parameterData._unityIndex, correctedIndex, 1);
						}
						else
						{
							keyIndex++;
						}
					}
					else
					{
						// Went past the number of keys on curve, so any point from here on should be removed
						int correctedIndex = paramUICache._childrenCache[childIndex]._instanceIndex - numRemoved;
						numRemoved++;
						AddMultiParmModifierProperty(HEU_ParameterModifier.ModifierAction.MULTIPARM_REMOVE, parameterData._unityIndex, correctedIndex, 1);
					}
				}
			}
		}

		private SerializedProperty AddMultiParmModifierProperty(HEU_ParameterModifier.ModifierAction action, int unityParamIndex, int instanceIndex, int numInstancesToAdd)
		{
			int newIndex = _parameterModifiersProperty.arraySize;
			_parameterModifiersProperty.InsertArrayElementAtIndex(newIndex);

			SerializedProperty newModifierProperty = _parameterModifiersProperty.GetArrayElementAtIndex(newIndex);
			newModifierProperty.FindPropertyRelative("_action").intValue = (int)action;
			newModifierProperty.FindPropertyRelative("_parameterIndex").intValue = unityParamIndex;
			newModifierProperty.FindPropertyRelative("_instanceIndex").intValue = instanceIndex;
			newModifierProperty.FindPropertyRelative("_modifierValue").intValue = numInstancesToAdd;

			return newModifierProperty;
		}

		private void AddMultiParmModifierPropertyFloat(int unityParamIndex, int instanceIndex, int numInstancesToAdd, float floatValue)
		{
			SerializedProperty newModifierProperty = AddMultiParmModifierProperty(HEU_ParameterModifier.ModifierAction.SET_FLOAT, unityParamIndex, instanceIndex, numInstancesToAdd);
			newModifierProperty.FindPropertyRelative("_floatValue").floatValue = floatValue;
		}

		private void AddMultiParmModifierPropertyInt(int unityParamIndex, int instanceIndex, int numInstancesToAdd, int intValue)
		{
			SerializedProperty newModifierProperty = AddMultiParmModifierProperty(HEU_ParameterModifier.ModifierAction.SET_INT, unityParamIndex, instanceIndex, numInstancesToAdd);
			newModifierProperty.FindPropertyRelative("_intValue").intValue = intValue;
		}

		private void GetFloatRampPointData(HEU_ParameterUICache paramUICache, int pointIndex, ref float position, ref float value, ref int interp)
		{
			int numParamsPerPoint = paramUICache._parameterData._parmInfo.instanceLength;

			Debug.AssertFormat(numParamsPerPoint == 3, "Ramp does not have expected (3) number of parameters per point!");

			int childIndex = pointIndex * numParamsPerPoint;
			if (childIndex >= 0 && childIndex < paramUICache._childrenCache.Count)
			{
				position = paramUICache._childrenCache[childIndex]._parameterData._floatValues[0];
				value = paramUICache._childrenCache[childIndex + 1]._parameterData._floatValues[0];
				interp = paramUICache._childrenCache[childIndex + 2]._parameterData._intValues[0];
			}
		}

		private void GetColorRampPointData(HEU_ParameterUICache paramUICache, int pointIndex, ref float position, ref Color color, ref int interp)
		{
			int numParamsPerPoint = paramUICache._parameterData._parmInfo.instanceLength;

			Debug.AssertFormat(numParamsPerPoint == 3, "Ramp does not have expected (3) number of parameters per point!");

			int childIndex = pointIndex * numParamsPerPoint;
			if (childIndex >= 0 && childIndex < paramUICache._childrenCache.Count)
			{
				position = paramUICache._childrenCache[childIndex]._parameterData._floatValues[0];
				color = paramUICache._childrenCache[childIndex + 1]._parameterData._color;
				interp = paramUICache._childrenCache[childIndex + 2]._parameterData._intValues[0];
			}
		}

		
	}

}   // HoudiniEngineUnity