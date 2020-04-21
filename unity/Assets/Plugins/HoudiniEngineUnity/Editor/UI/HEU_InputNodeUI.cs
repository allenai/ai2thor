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
	/// Editor drawing logic for input nodes.
	/// </summary>
	public static class HEU_InputNodeUI
	{
		/// <summary>
		/// Populate the UI cache for the given input node
		/// </summary>
		/// <param name="inputNode"></param>
		public static void PopulateCache(HEU_InputNode inputNode)
		{
			if (inputNode._uiCache == null)
			{
				inputNode._uiCache = new HEU_InputNodeUICache();

				inputNode._uiCache._inputNodeSerializedObject = new SerializedObject(inputNode);

				inputNode._uiCache._inputObjectTypeProperty = HEU_EditorUtility.GetSerializedProperty(inputNode._uiCache._inputNodeSerializedObject, "_inputObjectType");
				inputNode._uiCache._keepWorldTransformProperty = HEU_EditorUtility.GetSerializedProperty(inputNode._uiCache._inputNodeSerializedObject, "_keepWorldTransform");
				inputNode._uiCache._packBeforeMergeProperty = HEU_EditorUtility.GetSerializedProperty(inputNode._uiCache._inputNodeSerializedObject, "_packGeometryBeforeMerging");

				inputNode._uiCache._inputObjectsProperty = HEU_EditorUtility.GetSerializedProperty(inputNode._uiCache._inputNodeSerializedObject, "_inputObjects");

				int inputCount = inputNode._uiCache._inputObjectsProperty.arraySize;
				for (int i = 0; i < inputCount; ++i)
				{
					SerializedProperty inputObjectProperty = inputNode._uiCache._inputObjectsProperty.GetArrayElementAtIndex(i);

					HEU_InputNodeUICache.HEU_InputObjectUICache objectCache = new HEU_InputNodeUICache.HEU_InputObjectUICache();

					objectCache._gameObjectProperty = inputObjectProperty.FindPropertyRelative("_gameObject");

					objectCache._transformOffsetProperty = inputObjectProperty.FindPropertyRelative("_useTransformOffset");

					objectCache._translateProperty = inputObjectProperty.FindPropertyRelative("_translateOffset");
					objectCache._rotateProperty = inputObjectProperty.FindPropertyRelative("_rotateOffset");
					objectCache._scaleProperty = inputObjectProperty.FindPropertyRelative("_scaleOffset");

					inputNode._uiCache._inputObjectCache.Add(objectCache);
				}

				inputNode._uiCache._inputAssetsProperty = HEU_EditorUtility.GetSerializedProperty(inputNode._uiCache._inputNodeSerializedObject, "_inputAssetInfos");

				inputCount = inputNode._uiCache._inputAssetsProperty.arraySize;
				for (int i = 0; i < inputCount; ++i)
				{
					SerializedProperty inputAssetProperty = inputNode._uiCache._inputAssetsProperty.GetArrayElementAtIndex(i);

					HEU_InputNodeUICache.HEU_InputAssetUICache assetInfoCache = new HEU_InputNodeUICache.HEU_InputAssetUICache();

					assetInfoCache._gameObjectProperty = inputAssetProperty.FindPropertyRelative("_pendingGO");

					inputNode._uiCache._inputAssetCache.Add(assetInfoCache);
				}
			}
		}

		/// <summary>
		/// Draw the UI for the given input node
		/// </summary>
		/// <param name="inputNode"></param>
		public static void EditorDrawInputNode(HEU_InputNode inputNode)
		{
			int plusButtonWidth = 20;

			//GUIStyle boldLabelStyle = new GUIStyle(EditorStyles.boldLabel);
			//boldLabelStyle.alignment = TextAnchor.UpperLeft;

			GUIContent inputTypeLabel = new GUIContent("Input Type");

			GUIContent translateLabel = new GUIContent("    Translate");
			GUIContent rotateLabel = new GUIContent("    Rotate");
			GUIContent scaleLabel = new GUIContent("    Scale");

			PopulateCache(inputNode);

			EditorGUI.BeginChangeCheck();

			EditorGUILayout.BeginVertical(EditorStyles.helpBox);

			string labelName = inputNode.LabelName;
			if (!string.IsNullOrEmpty(labelName))
			{
				EditorGUILayout.LabelField(labelName);
			}

			EditorGUI.indentLevel++;

			HEU_InputNode.InputObjectType inputObjectType = (HEU_InputNode.InputObjectType)inputNode._uiCache._inputObjectTypeProperty.intValue;
			HEU_InputNode.InputObjectType userSelectedInputObjectType = (HEU_InputNode.InputObjectType)EditorGUILayout.EnumPopup(inputTypeLabel, inputObjectType);
			if (userSelectedInputObjectType != inputObjectType)
			{
				SerializedProperty pendingInputObjectTypeProperty = HEU_EditorUtility.GetSerializedProperty(inputNode._uiCache._inputNodeSerializedObject, "_pendingInputObjectType");
				if(pendingInputObjectTypeProperty != null)
				{
					pendingInputObjectTypeProperty.intValue = (int)userSelectedInputObjectType;
				}
			}
			else
			{
				EditorGUILayout.PropertyField(inputNode._uiCache._keepWorldTransformProperty);
				EditorGUILayout.PropertyField(inputNode._uiCache._packBeforeMergeProperty);

				if (inputObjectType == HEU_InputNode.InputObjectType.HDA)
				{
					SerializedProperty inputAssetsProperty = inputNode._uiCache._inputAssetsProperty;
					if (inputAssetsProperty != null)
					{
						int inputCount = inputAssetsProperty.arraySize;
						bool bSkipElements = false;

						HEU_EditorUI.DrawSeparator();

						EditorGUILayout.LabelField(string.Format("{0} input objects", inputCount));

						using (var hs1 = new EditorGUILayout.HorizontalScope())
						{
							if (GUILayout.Button("Add Slot"))
							{
								inputAssetsProperty.InsertArrayElementAtIndex(inputCount);

								bSkipElements = true;
							}

							if (GUILayout.Button("Add Selection"))
							{
								HEU_SelectionWindow.ShowWindow(inputNode.HandleSelectedObjectsForInputHDAs, typeof(HEU_HoudiniAssetRoot));
							}

							if (GUILayout.Button("Clear"))
							{
								inputAssetsProperty.ClearArray();
								bSkipElements = true;
							}
						}

						if (!bSkipElements)
						{
							using (var vs1 = new EditorGUILayout.VerticalScope())
							{
								for (int i = 0; i < inputCount; ++i)
								{
									using (var hs2 = new EditorGUILayout.HorizontalScope())
									{
										EditorGUILayout.LabelField("Input " + (i + 1));

										if (GUILayout.Button("+", GUILayout.Width(plusButtonWidth)))
										{
											inputAssetsProperty.InsertArrayElementAtIndex(i);
											break;
										}

										if (GUILayout.Button("-", GUILayout.Width(plusButtonWidth)))
										{
											inputAssetsProperty.DeleteArrayElementAtIndex(i);
											break;
										}
									}

									EditorGUI.indentLevel++;
									using (var vs4 = new EditorGUILayout.VerticalScope())
									{
										HEU_InputNodeUICache.HEU_InputAssetUICache assetCache = inputNode._uiCache._inputAssetCache[i];

										UnityEngine.Object setObject = EditorGUILayout.ObjectField(assetCache._gameObjectProperty.objectReferenceValue, typeof(HEU_HoudiniAssetRoot), true);
										if (setObject != assetCache._gameObjectProperty.objectReferenceValue)
										{
											GameObject inputGO = setObject != null ? (setObject as HEU_HoudiniAssetRoot).gameObject : null;
											// Check not setting same asset as self
											if (inputGO == null || inputGO != inputNode.ParentAsset.RootGameObject)
											{
												assetCache._gameObjectProperty.objectReferenceValue = inputGO;
											}
										}
									}
									EditorGUI.indentLevel--;
								}
							}
						}

					}

				}
				//else if (inputObjectType == HEU_InputNode.InputObjectType.CURVE)
				//{
				//	TODO INPUT CURVE
				//}
				else if (inputObjectType == HEU_InputNode.InputObjectType.UNITY_MESH)
				{
					SerializedProperty inputObjectsProperty = inputNode._uiCache._inputObjectsProperty;
					if (inputObjectsProperty != null)
					{
						bool bSkipElements = false;

						HEU_EditorUI.DrawSeparator();

						EditorGUILayout.LabelField(string.Format("{0} input objects", inputObjectsProperty.arraySize));

						using (var hs1 = new EditorGUILayout.HorizontalScope())
						{
							if (GUILayout.Button("Add Slot"))
							{
								inputObjectsProperty.arraySize++;
								FixUpScaleProperty(inputObjectsProperty, inputObjectsProperty.arraySize - 1);

								bSkipElements = true;
							}

							if (GUILayout.Button("Add Selection"))
							{
								HEU_SelectionWindow.ShowWindow(inputNode.HandleSelectedObjectsForInputObjects, typeof(GameObject));
							}

							if (GUILayout.Button("Clear"))
							{
								inputObjectsProperty.ClearArray();
								bSkipElements = true;
							}
						}

						if (!bSkipElements)
						{
							using (var vs1 = new EditorGUILayout.VerticalScope())
							{
								int inputCount = inputObjectsProperty.arraySize;
								for (int i = 0; i < inputCount; ++i)
								{
									using (var hs2 = new EditorGUILayout.HorizontalScope())
									{
										EditorGUILayout.LabelField("Input " + (i + 1));

										//using (var vs3 = new EditorGUILayout.VerticalScope())
										{
											if (GUILayout.Button("+", GUILayout.Width(plusButtonWidth)))
											{
												inputObjectsProperty.InsertArrayElementAtIndex(i);
												FixUpScaleProperty(inputObjectsProperty, i);
												break;
											}

											if (GUILayout.Button("-", GUILayout.Width(plusButtonWidth)))
											{
												inputObjectsProperty.DeleteArrayElementAtIndex(i);
												break;
											}
										}
									}

									EditorGUI.indentLevel++;
									using (var vs4 = new EditorGUILayout.VerticalScope())
									{
										HEU_InputNodeUICache.HEU_InputObjectUICache objectCache = inputNode._uiCache._inputObjectCache[i];

										EditorGUILayout.PropertyField(objectCache._gameObjectProperty, GUIContent.none);

										using (new EditorGUI.DisabledScope(!inputNode._uiCache._keepWorldTransformProperty.boolValue))
										{
											objectCache._transformOffsetProperty.boolValue = HEU_EditorUI.DrawToggleLeft(objectCache._transformOffsetProperty.boolValue, "Transform Offset");
											if (objectCache._transformOffsetProperty.boolValue)
											{
												objectCache._translateProperty.vector3Value = EditorGUILayout.Vector3Field(translateLabel, objectCache._translateProperty.vector3Value);
												objectCache._rotateProperty.vector3Value = EditorGUILayout.Vector3Field(rotateLabel, objectCache._rotateProperty.vector3Value);
												objectCache._scaleProperty.vector3Value = EditorGUILayout.Vector3Field(scaleLabel, objectCache._scaleProperty.vector3Value);
											}
										}
									}
									EditorGUI.indentLevel--;
								}
							}
						}
					}
				}
			}

			EditorGUI.indentLevel--;

			EditorGUILayout.EndVertical();

			if (EditorGUI.EndChangeCheck())
			{
				inputNode._uiCache._inputNodeSerializedObject.ApplyModifiedProperties();

				// When cooking, this will force input data to be uploaded
				inputNode.RequiresUpload = true;
			}
		}

		public static void FixUpScaleProperty(SerializedProperty inputObjectsProperty, int index)
		{
			SerializedProperty newInputProperty = inputObjectsProperty.GetArrayElementAtIndex(index);
			if (newInputProperty != null)
			{
				SerializedProperty scaleOverrideProperty = newInputProperty.FindPropertyRelative("_scaleOffset");
				if (scaleOverrideProperty != null)
				{
					scaleOverrideProperty.vector3Value = Vector3.one;
				}
			}
		}
	}

}	// HoudiniEngineUnity