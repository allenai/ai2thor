/*
* Copyright (c) <2019> Side Effects Software Inc.
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
using UnityEditor;

namespace HoudiniEngineUnity
{
	/// <summary>
	/// Display the Instance Input UI on the asset UI.
	/// To support large number of instances, this caches all the inputs initially,
	/// then draws a subset per page.
	/// </summary>
	public class HEU_InstanceInputUI
	{
		// Serialized object of HEU_InstanceInputUIState
		private SerializedObject _serializedObject;

		// Serialized properties of HEU_InstanceInputUIState
		private SerializedProperty _showInstanceInputsProperty;
		private SerializedProperty _numInputsToShowProperty;
		private SerializedProperty _inputsPageIndexProperty;

		// List of all input instance serialized properties
		private List<HEU_InstanceInputObjectCache> _instanceObjects = new List<HEU_InstanceInputObjectCache>();

		// Whether this has cached all necessary data for display
		private bool _populated;

		/// <summary>
		/// Contains cached serialized data of a HEU_ObjectInstanceInfo
		/// </summary>
		private class HEU_InstanceInputObjectCache
		{
			public string _inputName;

			public SerializedObject _objInstanceSerialized;

			public SerializedProperty _instancedInputsProperty;
		}

		/// <summary>
		/// Initialially populates the instance input cache.
		/// Should be called after every cook, or when selection changes.
		/// </summary>
		/// <param name="asset">HEU_HoudiniAsset object</param>
		/// <param name="assetObject">Serialized HEU_HoudiniAsset</param>
		private void PopulateInstanceInputCache(HEU_HoudiniAsset asset, SerializedObject assetObject)
		{
			//Debug.Log("PopulateInstanceInputCache");

			if (asset.InstanceInputUIState == null)
			{
				// This could be null due to upgrade of the plugin with an asset that was
				// instatiated from an older version of the plugin. Just create it now.
				asset.InstanceInputUIState = ScriptableObject.CreateInstance<HEU_InstanceInputUIState>();
			}

			// Get list of object instance infos in asset
			List<HEU_ObjectInstanceInfo> objInstanceInfos = new List<HEU_ObjectInstanceInfo>();
			asset.PopulateObjectInstanceInfos(objInstanceInfos);

			_populated = true;

			int numObjInstances = objInstanceInfos.Count;
			if (numObjInstances <= 0)
			{
				// No instances means nothing to show, so no need to create cache
				return;
			}

			_serializedObject = new SerializedObject(asset.InstanceInputUIState);

			_showInstanceInputsProperty = _serializedObject.FindProperty("_showInstanceInputs");

			_numInputsToShowProperty = _serializedObject.FindProperty("_numInputsToShowUI");

			_inputsPageIndexProperty = _serializedObject.FindProperty("_inputsPageIndexUI");

			// Create cache for each object instance info
			for (int i = 0; i < numObjInstances; ++i)
			{
				HEU_InstanceInputObjectCache objCache = new HEU_InstanceInputObjectCache();

				objCache._inputName = objInstanceInfos[i]._partTarget.PartName + "_" + i;
				objCache._objInstanceSerialized = new SerializedObject(objInstanceInfos[i]);
				objCache._instancedInputsProperty = HEU_EditorUtility.GetSerializedProperty(objCache._objInstanceSerialized, "_instancedInputs");

				_instanceObjects.Add(objCache);
			}

			//Debug.Log("Created instance input cache!");
		}

		/// <summary>
		/// Draw the given asset's input instances.
		/// Caches initially, then draws in paginized format.
		/// </summary>
		/// <param name="asset">Asset's input instances to draw</param>
		/// <param name="assetObject">Serialized object of assset</param>
		public void DrawInstanceInputs(HEU_HoudiniAsset asset, SerializedObject assetObject)
		{
			if (!_populated)
			{
				// Create cache
				PopulateInstanceInputCache(asset, assetObject);
			}

			if (_instanceObjects.Count == 0)
			{
				//Debug.Log("No instance objects");
				return;
			}

			HEU_EditorUI.DrawSeparator();

			int numObjInstances = _instanceObjects.Count;

			// Display input section if at least have 1 input field
			if (numObjInstances > 0)
			{
				HEU_EditorUI.BeginSection();

				_showInstanceInputsProperty.boolValue = HEU_EditorUI.DrawFoldOut
					(_showInstanceInputsProperty.boolValue, "INSTANCE INPUTS");
				if (_showInstanceInputsProperty.boolValue)
				{
					EditorGUI.BeginChangeCheck();

					// Number to display per page
					EditorGUILayout.DelayedIntField(_numInputsToShowProperty, new GUIContent("Display Per Page"));

					int numInputsToShow = Mathf.Clamp(_numInputsToShowProperty.intValue, 1, numObjInstances);
					_numInputsToShowProperty.intValue = numInputsToShow;

					int inputsPageIndex = _inputsPageIndexProperty.intValue;

					int totalPages = numObjInstances / numInputsToShow;

					using (new GUILayout.HorizontalScope())
					{
						EditorGUILayout.DelayedIntField(_inputsPageIndexProperty, new GUIContent("Page"));

						// Current page
						inputsPageIndex = Mathf.Clamp(_inputsPageIndexProperty.intValue, 0, totalPages - 1);

						// Previous page
						EditorGUI.BeginDisabledGroup(inputsPageIndex <= 0);
						if (GUILayout.Button("<<"))
						{
							inputsPageIndex--;
						}
						EditorGUI.EndDisabledGroup();

						// Next page
						EditorGUI.BeginDisabledGroup(inputsPageIndex >= (totalPages - 1));
						if (GUILayout.Button(">>"))
						{
							inputsPageIndex++;
						}
						EditorGUI.EndDisabledGroup();

						_inputsPageIndexProperty.intValue = inputsPageIndex;
					}

					inputsPageIndex = inputsPageIndex < totalPages ? inputsPageIndex : totalPages - 1;

					int startIndex = inputsPageIndex * numInputsToShow;
					int validNumInputs = numInputsToShow <= numObjInstances ? numInputsToShow : numObjInstances;

					if (EditorGUI.EndChangeCheck())
					{
						// Only apply change to the UI object so don't need to cook entire asset
						_serializedObject.ApplyModifiedProperties();
					}

					EditorGUI.BeginChangeCheck();

					// Draw instanced input info for current page
					for (int i = 0; i < validNumInputs; ++i)
					{
						EditorGUILayout.BeginVertical();

						int currentIndex = startIndex + i;

						if (_instanceObjects[currentIndex]._instancedInputsProperty != null)
						{
							int inputCount = _instanceObjects[currentIndex]._instancedInputsProperty.arraySize;
							EditorGUILayout.PropertyField(_instanceObjects[currentIndex]._instancedInputsProperty, 
								new GUIContent(_instanceObjects[currentIndex]._inputName), true);

							// When input size increases, Unity creates default values for HEU_InstancedInput which results in
							// zero value for scale offset. This fixes it up.
							int newInputCount = _instanceObjects[currentIndex]._instancedInputsProperty.arraySize;
							if (inputCount < newInputCount)
							{
								for (int inputIndex = inputCount; inputIndex < newInputCount; ++inputIndex)
								{
									SerializedProperty scaleProperty = _instanceObjects[currentIndex]._instancedInputsProperty.GetArrayElementAtIndex(inputIndex).FindPropertyRelative("_scaleOffset");
									scaleProperty.vector3Value = Vector3.one;
								}
							}
						}

						_instanceObjects[currentIndex]._objInstanceSerialized.ApplyModifiedProperties();

						EditorGUILayout.EndVertical();
					}

					if (EditorGUI.EndChangeCheck())
					{
						asset.RequestCook(bCheckParametersChanged: true, bAsync: true, bSkipCookCheck: false, bUploadParameters: true);
					}
				}

				HEU_EditorUI.EndSection();
			}
		}
	}

}   // HoudiniEngineUnity