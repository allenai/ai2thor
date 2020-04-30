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
using UnityEditor;


namespace HoudiniEngineUnity
{
	/// <summary>
	/// Custom Inspector UI for Houdini Asset.
	/// It uses HEU_HoudiniAssetRoot as the target object in order to access
	/// the underlying HEU_HoudiniAsset object whih contains actual data and logic.
	/// This allows to both show custom UI (via HEU_HoudiniAssetRoot) and 
	/// exclude Houdini-specific data at runtime (via HEU_HoudiniAsset which is EditorOnly).
	/// </summary>
	[CustomEditor(typeof(HEU_HoudiniAssetRoot))]
	public class HEU_HoudiniAssetUI : Editor
	{
		//	DATA ------------------------------------------------------------------------------------------------------

		// The root gameobject for an HDA. Used to show this custom UI.
		private HEU_HoudiniAssetRoot _houdiniAssetRoot;

		// Actual HDA data and logic
		private HEU_HoudiniAsset _houdiniAsset;

		// Serialized asset object
		private SerializedObject _houdiniAssetSerializedObject;

		// Cache reference to the custom parameter editor
		private Editor _parameterEditor;

		// Cache reference to the custom curve editor
		private Editor _curveEditor;

		// Cache reference to the custom curve parameter editor
		private Editor _curveParameterEditor;

		// Cache reference to the custom Tools editor
		private Editor _toolsEditor;

		// Cache reference to the custom Handles editor
		private Editor _handlesEditor;

		// Draws UI for instance inputs
		private HEU_InstanceInputUI _instanceInputUI;

		//	GUI CONTENT -----------------------------------------------------------------------------------------------

		private static Texture2D _reloadhdaIcon;
		private static Texture2D _recookhdaIcon;
		private static Texture2D _bakegameobjectIcon;
		private static Texture2D _bakeprefabIcon;
		private static Texture2D _bakeandreplaceIcon;
		private static Texture2D _removeheIcon;
		private static Texture2D _duplicateAssetIcon;
		private static Texture2D _resetParamIcon;

		private static GUIContent _reloadhdaContent;
		private static GUIContent _recookhdaContent;
		private static GUIContent _bakegameobjectContent;
		private static GUIContent _bakeprefabContent;
		private static GUIContent _bakeandreplaceContent;
		private static GUIContent _removeheContent;
		private static GUIContent _duplicateContent;
		private static GUIContent _resetParamContent;

		private static GUIContent _dragAndDropField = new GUIContent("Drag & drop GameObjects / Prefabs:", "Place GameObjects and/or Prefabs here that were previously baked out and need to be updated, then click Bake Update.");

		private static GUIContent _resetMaterialOverridesButton = new GUIContent("Reset Material Overrides", "Remove overridden materials, and replace with generated materials for this asset's output.");

		private static GUIContent _projectCurvePointsButton = new GUIContent("Project Curve", "Project all points in curves to colliders or layers specified above.");

		private static GUIContent _savePresetButton = new GUIContent("Save HDA Preset", "Save the HDA's current preset to a file.");

		private static GUIContent _loadPresetButton = new GUIContent("Load HDA Preset", "Load a HDA preset file into this asset and cook it.");

		//	LOGIC -----------------------------------------------------------------------------------------------------

		private void OnEnable()
		{
			_reloadhdaIcon = Resources.Load("heu_reloadhdaIcon") as Texture2D;
			_recookhdaIcon = Resources.Load("heu_recookhdaIcon") as Texture2D;
			_bakegameobjectIcon = Resources.Load("heu_bakegameobjectIcon") as Texture2D;
			_bakeprefabIcon = Resources.Load("heu_bakeprefabIcon") as Texture2D;
			_bakeandreplaceIcon = Resources.Load("heu_bakeandreplaceIcon") as Texture2D;
			_removeheIcon = Resources.Load("heu_removeheIcon") as Texture2D;
			_duplicateAssetIcon = Resources.Load("heu_duplicateassetIcon") as Texture2D;
			_resetParamIcon = Resources.Load("heu_resetparametersIcon") as Texture2D;

			_reloadhdaContent = new GUIContent("  Rebuild Asset", _reloadhdaIcon, "Reload the asset in Houdini and cook it. Current parameter values and input objects will be re-applied. Material overrides will be removed.");
			_recookhdaContent = new GUIContent("  Recook Asset", _recookhdaIcon, "Force recook of the asset in Houdini with the current parameter values and specified input data. Updates asset if changed in Houdini.");
			_bakegameobjectContent = new GUIContent("  Bake GameObject", _bakegameobjectIcon, "Bakes the output to a new GameObject. Meshes and Materials are copied.");
			_bakeprefabContent = new GUIContent("  Bake Prefab", _bakeprefabIcon, "Bakes the output to a new Prefab. Meshes and Materials are copied.");
			_bakeandreplaceContent = new GUIContent("  Bake Update", _bakeandreplaceIcon, "Update existing GameObject(s) and Prefab(s). Generated components, meshes, and materials are updated.");
			_removeheContent = new GUIContent("  Keep Only Output", _removeheIcon, "Remove HDA_Data and the Houdini Asset Root object, and leave just the output GameObject.");
			_duplicateContent = new GUIContent("  Duplicate Asset", _duplicateAssetIcon, "Safe duplication of this asset to create an exact copy. The asset is duplicated in Houdini. All data is copied over.");
			_resetParamContent = new GUIContent("  Reset Parameters", _resetParamIcon, "Reset all parameters to their HDA default values.");

			// Get the root gameobject, and the HDA bound to it
			_houdiniAssetRoot = target as HEU_HoudiniAssetRoot;
			TryAcquiringAsset();
		}

		private void TryAcquiringAsset()
		{
			if (_houdiniAsset == null && _houdiniAssetRoot != null)
			{
				_houdiniAsset = _houdiniAssetRoot._houdiniAsset;
			}

			if(_houdiniAsset != null && _houdiniAssetSerializedObject == null)
			{
				_houdiniAssetSerializedObject = new SerializedObject(_houdiniAsset);
			}
		}

		public void RefreshUI()
		{
			// Clear out the instance input cache.
			// Needed after a cook.
			_instanceInputUI = null;

			Repaint();
		}

		public override void OnInspectorGUI()
		{
			// Try acquiring asset reference in here again due to Undo.
			// Eg. After a delete, Undo requires us to re-acquire references.
			TryAcquiringAsset();

			if (_houdiniAsset == null)
			{
				DrawNoHDAInfo();
				return;
			}

			// Always hook into asset UI callback. This could have got reset on code refresh.
			_houdiniAsset._refreshUIDelegate = RefreshUI;

			serializedObject.Update();
			_houdiniAssetSerializedObject.Update();

			bool guiEnabled = GUI.enabled;

			GUIStyle backgroundStyle = new GUIStyle(GUI.skin.GetStyle("box"));
			RectOffset br = backgroundStyle.margin;
			br.top = 10;
			br.bottom = 6;
			br.left = 4;
			br.right = 4;
			backgroundStyle.margin = br;

			br = backgroundStyle.padding;
			br.top = 8;
			br.bottom = 8;
            br.left = 8;
            br.right = 8;
			backgroundStyle.padding = br;

			using (var hs = new EditorGUILayout.VerticalScope(backgroundStyle))
			{
				HEU_EditorUI.DrawSeparator();

                DrawHeaderSection();

				DrawLicenseInfo();

				bool bSkipDraw = DrawGenerateSection(_houdiniAssetRoot, serializedObject, _houdiniAsset, _houdiniAssetSerializedObject); ;
				if (!bSkipDraw)
				{
					SerializedProperty assetCookStatusProperty = HEU_EditorUtility.GetSerializedProperty(_houdiniAssetSerializedObject, "_cookStatus");
					if (assetCookStatusProperty != null)
					{
						// Track changes to Houdini Asset gameobject
						EditorGUI.BeginChangeCheck();

						DrawEventsSection(_houdiniAsset, _houdiniAssetSerializedObject);

						DrawAssetOptions(_houdiniAsset, _houdiniAssetSerializedObject);

						DrawCurvesSection(_houdiniAsset, _houdiniAssetSerializedObject);

						DrawInputNodesSection(_houdiniAsset, _houdiniAssetSerializedObject);

						DrawTerrainSection(_houdiniAsset, _houdiniAssetSerializedObject);

						// If this is a Curve asset, we don't need to draw parameters as its redundant
						if (_houdiniAsset.AssetType != HEU_HoudiniAsset.HEU_AssetType.TYPE_CURVE)
						{
							DrawParameters(_houdiniAsset.Parameters, ref _parameterEditor);
						}

						DrawInstanceInputs(_houdiniAsset, _houdiniAssetSerializedObject);

						// Check if any changes occurred, and if so, trigger a recook
						if (EditorGUI.EndChangeCheck())
						{
							_houdiniAssetSerializedObject.ApplyModifiedProperties();
							serializedObject.ApplyModifiedProperties();

							// Do recook if values have changed
							if (HEU_PluginSettings.CookingEnabled && _houdiniAsset.AutoCookOnParameterChange && _houdiniAsset.DoesAssetRequireRecook())
							{
								_houdiniAsset.RequestCook(bCheckParametersChanged: true, bAsync: false, bSkipCookCheck: false, bUploadParameters: true);
							}
						}
					}
				}
			}

			GUI.enabled = guiEnabled;
		}

		/// <summary>
		/// Callback when Scene is updated
		/// </summary>
		public void OnSceneGUI()
		{
			if ((Event.current.type == EventType.ValidateCommand && Event.current.commandName.Equals("UndoRedoPerformed")))
			{
				Event.current.Use();
			}

			if ((Event.current.type == EventType.ExecuteCommand && Event.current.commandName.Equals("UndoRedoPerformed")))
			{
				if(_houdiniAsset != null)
				{
					// On Undo, need to check which parameters have changed in order to update and recook.
					_houdiniAsset.SyncInternalParametersForUndoCompare();

					_houdiniAsset.RequestCook(bCheckParametersChanged: true, bAsync: false, bSkipCookCheck: false, bUploadParameters: true);
				}

				// Force a repaint here to update the UI when Undo is invoked. Handles case where the Inspector window is
				// no longer the focus. Without this the Inspector window still shows old value until user selects it.
				Repaint();
			}

			// Draw custom scene elements. Should be called for any event, not just repaint.
			DrawSceneElements(_houdiniAsset);
		}

		/// <summary>
		/// Draw Houdini Engine license info.
		/// </summary>
		private void DrawLicenseInfo()
		{
			HAPI_License license = HEU_SessionManager.GetCurrentLicense(false);
			if (license == HAPI_License.HAPI_LICENSE_HOUDINI_ENGINE_INDIE)
			{
				HEU_EditorUI.DrawSeparator();

				GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
				labelStyle.fontStyle = FontStyle.Bold;
				labelStyle.normal.textColor = HEU_EditorUI.IsEditorDarkSkin() ? Color.yellow : Color.red;
				EditorGUILayout.LabelField("Houdini Engine Indie - For Limited Commercial Use Only", labelStyle);

				HEU_EditorUI.DrawSeparator();
			}
		}

		private void DrawNoHDAInfo()
		{
			HEU_EditorUI.DrawSeparator();

			GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
			labelStyle.fontStyle = FontStyle.Bold;
			labelStyle.normal.textColor = HEU_EditorUI.IsEditorDarkSkin() ? Color.yellow : Color.red;
			EditorGUILayout.LabelField("Houdini Engine Asset - no HEU_HoudiniAsset found!", labelStyle);

			HEU_EditorUI.DrawSeparator();
		}

		/// <summary>
		/// Draw the Object Instance Inputs section for given asset.
		/// </summary>
		/// <param name="asset">The HDA asset</param>
		/// <param name="assetObject">Serialized HDA asset object</param>
		private void DrawInstanceInputs(HEU_HoudiniAsset asset, SerializedObject assetObject)
		{
			if (_instanceInputUI == null)
			{
				_instanceInputUI = new HEU_InstanceInputUI();
			}
			_instanceInputUI.DrawInstanceInputs(asset, assetObject);
		}

		/// <summary>
		/// Draw asset options for given asset.
		/// </summary>
		/// <param name="asset">The HDA asset</param>
		/// <param name="assetObject">Serialized HDA asset object</param>
		private void DrawAssetOptions(HEU_HoudiniAsset asset, SerializedObject assetObject)
		{
			GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
			buttonStyle.fontSize = 11;
			buttonStyle.alignment = TextAnchor.MiddleCenter;
			buttonStyle.fixedHeight = 24;
			buttonStyle.margin.left = 34;

			HEU_EditorUI.BeginSection();
			{
				SerializedProperty showHDAOptionsProperty = assetObject.FindProperty("_showHDAOptions");

				showHDAOptionsProperty.boolValue = HEU_EditorUI.DrawFoldOut(showHDAOptionsProperty.boolValue, "ASSET OPTIONS");
				if (showHDAOptionsProperty.boolValue)
				{
					EditorGUI.indentLevel++;
					HEU_EditorUI.DrawPropertyField(assetObject, "_autoCookOnParameterChange", "Auto-Cook On Parameter Change", "Automatically cook when a parameter changes. If off, must use Recook to cook.");
					HEU_EditorUI.DrawPropertyField(assetObject, "_pushTransformToHoudini", "Push Transform To Houdini", "Send the asset's transform to Houdini and apply to object.");
					HEU_EditorUI.DrawPropertyField(assetObject, "_transformChangeTriggersCooks", "Transform Change Triggers Cooks", "Changing the transform (e.g. moving) the asset in Unity will invoke cook in Houdini.");
					HEU_EditorUI.DrawPropertyField(assetObject, "_cookingTriggersDownCooks", "Cooking Triggers Downstream Cooks", "Cooking this asset will trigger dependent assets' to also cook.");
					HEU_EditorUI.DrawPropertyField(assetObject, "_generateUVs", "Generate UVs", "Force Unity to generate UVs for output geometry.");
					HEU_EditorUI.DrawPropertyField(assetObject, "_generateTangents", "Generate Tangents", "Generate tangents in Unity for output geometry.");
					HEU_EditorUI.DrawPropertyField(assetObject, "_generateNormals", "Generate Normals", "Generate normals in Unity for output geometry.");
					HEU_EditorUI.DrawPropertyField(assetObject, "_generateMeshUsingPoints", "Generate Mesh Using Points", "Use point attributes instead of vertex attributes for geometry. Ignores vertex attributes.");
					HEU_EditorUI.DrawPropertyField(assetObject, "_useLODGroups", "Use LOD Groups", "Automatically create Unity LOD group if found.");
					HEU_EditorUI.DrawPropertyField(assetObject, "_ignoreNonDisplayNodes", "Ignore NonDisplay Nodes", "Only display node geometry will be created.");
					HEU_EditorUI.DrawPropertyField(assetObject, "_splitGeosByGroup", "Split Geos By Group", "Split geometry into separate gameobjects by group. Deprecated feature and only recommended for simple use cases.");

					if (asset.NumAttributeStores() > 0)
					{
						HEU_EditorUI.DrawPropertyField(assetObject, "_editableNodesToolsEnabled", "Enable Editable Node Tools", "Displays Editable Node Tools and generates the node's geometry, if asset has editable nodes.");
					}

					if (asset.NumHandles() > 0)
					{
						HEU_EditorUI.DrawPropertyField(assetObject, "_handlesEnabled", "Enable Handles", "Creates Houdini Handles if asset has them.");
					}

					EditorGUILayout.Space();

					using (var hs = new EditorGUILayout.HorizontalScope())
					{
						if (GUILayout.Button(_savePresetButton, buttonStyle, GUILayout.MaxWidth(160)))
						{
							string fileName = asset.AssetName;
							string filePattern = "preset";
							string newPath = EditorUtility.SaveFilePanel("Save HDA preset", "", fileName + "." + filePattern, filePattern);
							if (newPath != null && !string.IsNullOrEmpty(newPath))
							{
								HEU_AssetPresetUtility.SaveAssetPresetToFile(asset, newPath);
							}
						}

						if (GUILayout.Button(_loadPresetButton, buttonStyle, GUILayout.MaxWidth(160)))
						{
							string fileName = asset.AssetName;
							string filePattern = "preset";
							string newPath = EditorUtility.OpenFilePanel("Load HDA preset", "", filePattern);
							if (newPath != null && !string.IsNullOrEmpty(newPath))
							{
								HEU_AssetPresetUtility.LoadPresetFileIntoAssetAndCook(asset, newPath);
							}
						}
					}

					EditorGUILayout.Space();

					if(GUILayout.Button(_resetMaterialOverridesButton, buttonStyle, GUILayout.MaxWidth(160)))
					{
						asset.ResetMaterialOverrides();
					}

					EditorGUI.indentLevel--;
				}
			}
			HEU_EditorUI.EndSection();

			HEU_EditorUI.DrawSeparator();
		}

		private static HEU_HoudiniAsset.AssetCookStatus GetCookStatusFromSerializedAsset(SerializedObject assetObject)
		{
			HEU_HoudiniAsset.AssetCookStatus cookStatus = HEU_HoudiniAsset.AssetCookStatus.NONE;

			SerializedProperty cookStatusProperty = HEU_EditorUtility.GetSerializedProperty(assetObject, "_cookStatus");
			if (cookStatusProperty != null)
			{
				cookStatus = (HEU_HoudiniAsset.AssetCookStatus)cookStatusProperty.enumValueIndex;
			}

			return cookStatus;
		}


		/// <summary>
		/// Draw the Generate section.
		/// </summary>
		private static bool DrawGenerateSection(HEU_HoudiniAssetRoot assetRoot, SerializedObject assetRootSerializedObject, HEU_HoudiniAsset asset, SerializedObject assetObject)
		{
			bool bSkipDrawing = false;

			float separatorDistance = 5f;

			float screenWidth = EditorGUIUtility.currentViewWidth;

			float buttonHeight = 30f;
			float widthPadding = 55f;
			float doubleButtonWidth = Mathf.Round(screenWidth - widthPadding + separatorDistance);
			float singleButtonWidth = Mathf.Round((screenWidth - widthPadding) * 0.5f);

			GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
			buttonStyle.fontStyle = FontStyle.Bold;
			buttonStyle.fontSize = 11;
			buttonStyle.alignment = TextAnchor.MiddleLeft;
			buttonStyle.fixedHeight = buttonHeight;
			buttonStyle.padding.left = 6;
			buttonStyle.padding.right = 6;
			buttonStyle.margin.left = 0;
			buttonStyle.margin.right = 0;

			GUIStyle centredButtonStyle = new GUIStyle(buttonStyle);
			centredButtonStyle.alignment = TextAnchor.MiddleCenter;

			GUIStyle buttonSetStyle = new GUIStyle(GUI.skin.box);
			RectOffset br = buttonSetStyle.margin;
			br.left = 4;
			br.right = 4;
			buttonSetStyle.margin = br;

			GUIStyle boxStyle = new GUIStyle(GUI.skin.GetStyle("ColorPickerBackground"));
			br = boxStyle.margin;
			br.left = 4;
			br.right = 4;
			boxStyle.margin = br;
			boxStyle.padding = br;

			GUIStyle promptButtonStyle = new GUIStyle(GUI.skin.button);
			promptButtonStyle.fontSize = 11;
			promptButtonStyle.alignment = TextAnchor.MiddleCenter;
			promptButtonStyle.fixedHeight = 30;
			promptButtonStyle.margin.left = 34;
			promptButtonStyle.margin.right = 34;

			_recookhdaContent.text = "  Recook Asset";

			HEU_HoudiniAsset.AssetBuildAction pendingBuildAction = HEU_HoudiniAsset.AssetBuildAction.NONE;
			SerializedProperty pendingBuildProperty = HEU_EditorUtility.GetSerializedProperty(assetObject, "_requestBuildAction");
			if (pendingBuildProperty != null)
			{
				pendingBuildAction = (HEU_HoudiniAsset.AssetBuildAction)pendingBuildProperty.enumValueIndex;
			}

			// Track changes for the build and bake targets
			EditorGUI.BeginChangeCheck();

			HEU_HoudiniAsset.AssetCookStatus cookStatus = GetCookStatusFromSerializedAsset(assetObject);

			if (cookStatus == HEU_HoudiniAsset.AssetCookStatus.SELECT_SUBASSET)
			{
				// Prompt user to select subasset

				GUIStyle promptStyle = new GUIStyle(GUI.skin.label);
				promptStyle.fontStyle = FontStyle.Bold;
				promptStyle.normal.textColor = HEU_EditorUI.IsEditorDarkSkin() ? Color.green : Color.blue;
				EditorGUILayout.LabelField("SELECT AN ASSET TO INSTANTIATE:", promptStyle);

				EditorGUILayout.Separator();

				int selectedIndex = -1;
				string[] subassetNames = asset.SubassetNames;

				for (int i = 0; i < subassetNames.Length; ++i)
				{
					if (GUILayout.Button(subassetNames[i], promptButtonStyle))
					{
						selectedIndex = i;
						break;
					}

					EditorGUILayout.Separator();
				}

				if (selectedIndex >= 0)
				{
					SerializedProperty selectedIndexProperty = HEU_EditorUtility.GetSerializedProperty(assetObject, "_selectedSubassetIndex");
					if (selectedIndexProperty != null)
					{
						selectedIndexProperty.intValue = selectedIndex;
					}
				}

				bSkipDrawing = true;
			}
			else
			{
				HEU_EditorUI.BeginSection();
				{
					if (cookStatus == HEU_HoudiniAsset.AssetCookStatus.COOKING || cookStatus == HEU_HoudiniAsset.AssetCookStatus.POSTCOOK)
					{
						_recookhdaContent.text = "  Cooking Asset";
					}
					else if (cookStatus == HEU_HoudiniAsset.AssetCookStatus.LOADING || cookStatus == HEU_HoudiniAsset.AssetCookStatus.POSTLOAD)
					{
						_reloadhdaContent.text = "  Loading Asset";
					}

					SerializedProperty showGenerateProperty = assetObject.FindProperty("_showGenerateSection");

					showGenerateProperty.boolValue = HEU_EditorUI.DrawFoldOut(showGenerateProperty.boolValue, "GENERATE");
					if (showGenerateProperty.boolValue)
					{
						//bool bHasPendingAction = (pendingBuildAction != HEU_HoudiniAsset.AssetBuildAction.NONE) || (cookStatus != HEU_HoudiniAsset.AssetCookStatus.NONE);

						HEU_EditorUI.DrawSeparator();

						//EditorGUI.BeginDisabledGroup(bHasPendingAction);

						using (var hs = new EditorGUILayout.HorizontalScope(boxStyle))
						{
							if (GUILayout.Button(_reloadhdaContent, buttonStyle, GUILayout.Width(singleButtonWidth)))
							{
								pendingBuildAction = HEU_HoudiniAsset.AssetBuildAction.RELOAD;
								bSkipDrawing = true;
							}

							GUILayout.Space(separatorDistance);

							if (!bSkipDrawing && GUILayout.Button(_recookhdaContent, buttonStyle, GUILayout.Width(singleButtonWidth)))
							{
								pendingBuildAction = HEU_HoudiniAsset.AssetBuildAction.COOK;
								bSkipDrawing = true;
							}
						}

						using (var hs = new EditorGUILayout.HorizontalScope(boxStyle))
						{
							float tripleButtonWidth = Mathf.Round((screenWidth - widthPadding) * 0.33f);

							if (GUILayout.Button(_removeheContent, buttonStyle, GUILayout.Width(tripleButtonWidth)))
							{
								pendingBuildAction = HEU_HoudiniAsset.AssetBuildAction.STRIP_HEDATA;
								bSkipDrawing = true;
							}

							GUILayout.Space(separatorDistance);

							if (GUILayout.Button(_duplicateContent, buttonStyle, GUILayout.Width(tripleButtonWidth)))
							{
								pendingBuildAction = HEU_HoudiniAsset.AssetBuildAction.DUPLICATE;
								bSkipDrawing = true;
							}

							GUILayout.Space(separatorDistance);

							if (GUILayout.Button(_resetParamContent, buttonStyle, GUILayout.Width(tripleButtonWidth)))
							{
								pendingBuildAction = HEU_HoudiniAsset.AssetBuildAction.RESET_PARAMS;
								bSkipDrawing = true;
							}
						}

						//EditorGUI.EndDisabledGroup();

						HEU_EditorUI.DrawSeparator();
					}
				}

				HEU_EditorUI.EndSection();

				HEU_EditorUI.DrawSeparator();

				HEU_EditorUI.BeginSection();
				{
					SerializedProperty showBakeProperty = assetObject.FindProperty("_showBakeSection");

					showBakeProperty.boolValue = HEU_EditorUI.DrawFoldOut(showBakeProperty.boolValue, "BAKE");
					if (showBakeProperty.boolValue)
					{
						if (!bSkipDrawing)
						{
							// Bake -> New Instance, New Prefab, Existing instance or prefab

							using (var vs = new EditorGUILayout.HorizontalScope(boxStyle))
							{
								if (GUILayout.Button(_bakegameobjectContent, buttonStyle, GUILayout.Width(singleButtonWidth)))
								{
									asset.BakeToNewStandalone();
								}

								GUILayout.Space(separatorDistance);

								if (GUILayout.Button(_bakeprefabContent, buttonStyle, GUILayout.Width(singleButtonWidth)))
								{
									asset.BakeToNewPrefab();
								}
							}

							HEU_EditorUI.DrawSeparator();

							using (var hs2 = new EditorGUILayout.VerticalScope(boxStyle))
							{
								if (GUILayout.Button(_bakeandreplaceContent, centredButtonStyle, GUILayout.Width(doubleButtonWidth)))
								{
									if (assetRoot._bakeTargets == null || assetRoot._bakeTargets.Count == 0)
									{
										// No bake target means user probably forgot to set one. So complain!
										HEU_EditorUtility.DisplayDialog("No Bake Targets", "Bake Update requires atleast one valid GameObject.\n\nDrag a GameObject or Prefab onto the Drag and drop GameObjects / Prefabs field!", "OK");
									}
									else
									{
										int numTargets = assetRoot._bakeTargets.Count;
										for (int i = 0; i < numTargets; ++i)
										{
											GameObject bakeGO = assetRoot._bakeTargets[i];
											if (bakeGO != null)
											{
												if (HEU_EditorUtility.IsPrefabAsset(bakeGO))
												{
													// Prefab asset means its the source prefab, and not an instance of it
													asset.BakeToExistingPrefab(bakeGO);
												}
												else
												{
													// This is for all standalone (including prefab instances)
													asset.BakeToExistingStandalone(bakeGO);
												}
											}
											else
											{
												Debug.LogWarning("Unable to bake to null target at index " + i);
											}
										}
									}
								}

								using (var hs = new EditorGUILayout.VerticalScope(buttonSetStyle))
								{
									SerializedProperty bakeTargetsProp = assetRootSerializedObject.FindProperty("_bakeTargets");
									if (bakeTargetsProp != null)
									{
										EditorGUILayout.PropertyField(bakeTargetsProp, _dragAndDropField, true, GUILayout.Width(doubleButtonWidth - 9f));
									}
								}
							}
						}
					}
				}
				HEU_EditorUI.EndSection();

				HEU_EditorUI.DrawSeparator();

				if (pendingBuildAction != HEU_HoudiniAsset.AssetBuildAction.NONE)
				{
					// Sanity check to make sure the asset is part of the AssetUpater
					HEU_AssetUpdater.AddAssetForUpdate(asset);

					// Apply pending build action based on user UI interaction above
					pendingBuildProperty.enumValueIndex = (int)pendingBuildAction;

					if (pendingBuildAction == HEU_HoudiniAsset.AssetBuildAction.COOK)
					{
						// Recook should only update parameters that haven't changed. Otherwise if not checking and updating parameters,
						// then buttons will trigger callbacks on Recook which is not desired.
						SerializedProperty checkParameterChange = HEU_EditorUtility.GetSerializedProperty(assetObject, "_checkParameterChangeForCook");
						if (checkParameterChange != null)
						{
							checkParameterChange.boolValue = true;
						}

						// But we do want to always upload input geometry on user hitting Recook expliclity
						SerializedProperty forceUploadInputs = HEU_EditorUtility.GetSerializedProperty(assetObject, "_forceUploadInputs");
						if (forceUploadInputs != null)
						{
							forceUploadInputs.boolValue = true;
						}
					}
				}
			}
			
			if (EditorGUI.EndChangeCheck())
			{
				assetRootSerializedObject.ApplyModifiedProperties();
				assetObject.ApplyModifiedProperties();
			}

			return bSkipDrawing;
		}

        /// <summary>
        /// Draw the Houdini Engine header image
        /// </summary>
        void DrawHeaderSection()
        {
            GUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
            Texture2D headerImage = Resources.Load("heu_hengine") as Texture2D;

            HEU_EditorUI.BeginSection();
            GUILayout.Label(headerImage, GUILayout.MinWidth(100));
            HEU_EditorUI.EndSection();

            GUI.backgroundColor = Color.white;

            HEU_EditorUI.DrawSeparator();
        }

		/// <summary>
		/// Draw Asset Events section.
		/// </summary>
		/// <param name="asset"></param>
		/// <param name="assetObject"></param>
		private void DrawEventsSection(HEU_HoudiniAsset asset, SerializedObject assetObject)
		{
			HEU_EditorUI.BeginSection();
			{
				SerializedProperty showEventsProperty = assetObject.FindProperty("_showEventsSection");

				showEventsProperty.boolValue = HEU_EditorUI.DrawFoldOut(showEventsProperty.boolValue, "EVENTS");
				if (showEventsProperty.boolValue)
				{
					HEU_EditorUI.DrawSeparator();

					SerializedProperty reloadEvent = assetObject.FindProperty("_reloadEvent");
					EditorGUILayout.PropertyField(reloadEvent, new GUIContent("Rebuild Events"));

					HEU_EditorUI.DrawSeparator();

					SerializedProperty recookEvent = assetObject.FindProperty("_cookedEvent");
					EditorGUILayout.PropertyField(recookEvent, new GUIContent("Cooked Events"));

					HEU_EditorUI.DrawSeparator();

					SerializedProperty bakedEvent = assetObject.FindProperty("_bakedEvent");
					EditorGUILayout.PropertyField(bakedEvent, new GUIContent("Baked Events"));
				}
			}

			HEU_EditorUI.EndSection();

			HEU_EditorUI.DrawSeparator();
		}

		private void DrawParameters(HEU_Parameters parameters, ref Editor parameterEditor)
		{
			if (parameters != null)
			{
				SerializedObject paramObject = new SerializedObject(parameters);
				Editor.CreateCachedEditor(paramObject.targetObject, null, ref parameterEditor);
				parameterEditor.OnInspectorGUI();
			}
		}

		private void DrawCurvesSection(HEU_HoudiniAsset asset, SerializedObject assetObject)
		{
			if (asset.GetEditableCurveCount() <= 0)
			{
				return;
			}

			GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
			buttonStyle.fontSize = 11;
			buttonStyle.alignment = TextAnchor.MiddleCenter;
			buttonStyle.fixedHeight = 24;
			buttonStyle.margin.left = 34;

			HEU_EditorUI.BeginSection();
			{
				SerializedProperty showCurvesProperty = HEU_EditorUtility.GetSerializedProperty(assetObject, "_showCurvesSection");
				if (showCurvesProperty != null)
				{
					showCurvesProperty.boolValue = HEU_EditorUI.DrawFoldOut(showCurvesProperty.boolValue, "CURVES");
					if (showCurvesProperty.boolValue)
					{
						SerializedProperty curveEditorProperty = HEU_EditorUtility.GetSerializedProperty(assetObject, "_curveEditorEnabled");
						if (curveEditorProperty != null)
						{
							EditorGUILayout.PropertyField(curveEditorProperty);
						}

						HEU_EditorUI.DrawHeadingLabel("Collision Settings");
						EditorGUI.indentLevel++;

						string projectLabel = "Project Curves To ";
						List<HEU_Curve> curves = asset.GetCurves();

						SerializedProperty curveCollisionProperty = HEU_EditorUtility.GetSerializedProperty(assetObject, "_curveDrawCollision");
						if (curveCollisionProperty != null)
						{
							EditorGUILayout.PropertyField(curveCollisionProperty, new GUIContent("Collision Type"));
							if (curveCollisionProperty.enumValueIndex == (int)HEU_Curve.CurveDrawCollision.COLLIDERS)
							{
								HEU_EditorUtility.EditorDrawSerializedProperty(assetObject, "_curveDrawColliders", label: "Colliders");
								projectLabel += "Colliders";
							}
							else if (curveCollisionProperty.enumValueIndex == (int)HEU_Curve.CurveDrawCollision.LAYERMASK)
							{
								HEU_EditorUtility.EditorDrawSerializedProperty(assetObject, "_curveDrawLayerMask", label: "Layer Mask");
								projectLabel += "Layer";
							}

							HEU_EditorUI.DrawSeparator();

							EditorGUI.indentLevel--;
							HEU_EditorUI.DrawHeadingLabel("Projection Settings");
							EditorGUI.indentLevel++;

							HEU_EditorUtility.EditorDrawSerializedProperty(assetObject, "_curveProjectDirection", label: "Project Direction", tooltip: "The ray cast direction for projecting the curve points.");
							HEU_EditorUtility.EditorDrawFloatProperty(assetObject, "_curveProjectMaxDistance", label: "Project Max Distance", tooltip: "The maximum ray cast distance for projecting the curve points.");

							_projectCurvePointsButton.text = projectLabel;
							if (GUILayout.Button(_projectCurvePointsButton, buttonStyle, GUILayout.MaxWidth(180)))
							{
								SerializedProperty projectDirProperty = HEU_EditorUtility.GetSerializedProperty(assetObject, "_curveProjectDirection");
								SerializedProperty maxDistanceProperty = HEU_EditorUtility.GetSerializedProperty(assetObject, "_curveProjectMaxDistance");

								Vector3 projectDir = projectDirProperty != null ? projectDirProperty.vector3Value : Vector3.down;
								float maxDistance = maxDistanceProperty != null ? maxDistanceProperty.floatValue : 0;

								for (int i = 0; i < curves.Count; ++i)
								{
									curves[i].ProjectToColliders(asset, projectDir, maxDistance);
								}
							}
						}

						EditorGUI.indentLevel--;

						HEU_EditorUI.DrawSeparator();

						for (int i = 0; i < curves.Count; ++i)
						{
							if (curves[i].Parameters != null)
							{
								DrawParameters(curves[i].Parameters, ref _curveParameterEditor);
							}
						}
					}
				}
			}
			HEU_EditorUI.EndSection();

			HEU_EditorUI.DrawSeparator();
		}

		private void DrawInputNodesSection(HEU_HoudiniAsset asset, SerializedObject assetObject)
		{
			List<HEU_InputNode> inputNodes = asset.GetNonParameterInputNodes();
			if (inputNodes.Count > 0)
			{
				HEU_EditorUI.BeginSection();

				SerializedProperty showInputNodesProperty = HEU_EditorUtility.GetSerializedProperty(assetObject, "_showInputNodesSection");
				if (showInputNodesProperty != null)
				{
					showInputNodesProperty.boolValue = HEU_EditorUI.DrawFoldOut(showInputNodesProperty.boolValue, "INPUT NODES");
					if (showInputNodesProperty.boolValue)
					{
						foreach (HEU_InputNode inputNode in inputNodes)
						{
							HEU_InputNodeUI.EditorDrawInputNode(inputNode);

							if (inputNodes.Count > 1)
							{
								HEU_EditorUI.DrawSeparator();
							}
						}
					}

					HEU_EditorUI.DrawSeparator();
				}

				HEU_EditorUI.EndSection();

				HEU_EditorUI.DrawSeparator();
			}
		}

		private void DrawSceneElements(HEU_HoudiniAsset asset)
		{
			if(asset == null)
			{
				return;
			}

			// Curve Editor
			if (asset.CurveEditorEnabled)
			{
				if (asset.GetEditableCurveCount() > 0)
				{
					HEU_Curve[] curvesArray = asset.GetCurves().ToArray();
					Editor.CreateCachedEditor(curvesArray, null, ref _curveEditor);
					(_curveEditor as HEU_CurveUI).UpdateSceneCurves(asset);

					bool bRequiresCook = !System.Array.TrueForAll(curvesArray, c => c.EditState != HEU_Curve.CurveEditState.REQUIRES_GENERATION);
					if (bRequiresCook)
					{
						_houdiniAsset.RequestCook(bCheckParametersChanged: true, bAsync: false, bSkipCookCheck: false, bUploadParameters: true);
					}
				}
			}

			// Tools Editor
			if(asset.EditableNodesToolsEnabled)
			{
				List<HEU_AttributesStore> attributesStores = asset.GetAttributesStores();
				if(attributesStores.Count > 0)
				{
					HEU_AttributesStore[] attributesStoresArray = attributesStores.ToArray();
					Editor.CreateCachedEditor(attributesStoresArray, null, ref _toolsEditor);
					HEU_ToolsUI toolsUI = (_toolsEditor as HEU_ToolsUI);
					toolsUI.DrawToolsEditor(asset);

					if (asset.ToolsInfo._liveUpdate && !asset.ToolsInfo._isPainting)
					{
						bool bAttributesDirty = !System.Array.TrueForAll(attributesStoresArray, s => !s.AreAttributesDirty());
						if (bAttributesDirty)
						{
							//Debug.Log("Cook for attributes dirty!");
							_houdiniAsset.RequestCook(bCheckParametersChanged: true, bAsync: false, bSkipCookCheck: false, bUploadParameters: true);
						}
					}
				}
			}

			// Handles
			if(asset.HandlesEnabled)
			{
				List<HEU_Handle> handles = asset.GetHandles();
				if(handles.Count > 0)
				{
					HEU_Handle[] handlesArray = handles.ToArray();
					Editor.CreateCachedEditor(handlesArray, null, ref _handlesEditor);
					HEU_HandlesUI handlesUI = (_handlesEditor as HEU_HandlesUI);
					bool bHandlesChanged = handlesUI.DrawHandles(asset);

					if(bHandlesChanged)
					{
						_houdiniAsset.RequestCook(bCheckParametersChanged: true, bAsync: false, bSkipCookCheck: false, bUploadParameters: true);
					}
				}
			}
		}

		private void DrawTerrainSection(HEU_HoudiniAsset asset, SerializedObject assetObject)
		{
			int numVolumes = asset.GetVolumeCacheCount();
			if(numVolumes <= 0)
			{
				return;
			}

			HEU_EditorUI.BeginSection();
			{
				SerializedProperty showTerrainProperty = HEU_EditorUtility.GetSerializedProperty(assetObject, "_showTerrainSection");
				if (showTerrainProperty != null)
				{
					showTerrainProperty.boolValue = HEU_EditorUI.DrawFoldOut(showTerrainProperty.boolValue, "TERRAIN");
					if (showTerrainProperty.boolValue)
					{
						// Draw each volume layer
						List<HEU_VolumeCache> volumeCaches = asset.GetVolumeCaches();
						int numCaches = volumeCaches.Count;
						for (int i = 0; i < numCaches; ++i)
						{
							SerializedObject cacheObjectSerialized = new SerializedObject(volumeCaches[i]);
							bool bChanged = false;
							bool bStrengthChanged = false;

							SerializedProperty layersProperty = cacheObjectSerialized.FindProperty("_layers");
							if (layersProperty == null || layersProperty.arraySize == 0)
							{
								continue;
							}

							string heading = string.Format("{0}-{1}:", volumeCaches[i].ObjectName, volumeCaches[i].GeoName);

							if (HEU_EditorUI.DrawFoldOutSerializedProperty(HEU_EditorUtility.GetSerializedProperty(cacheObjectSerialized, "_uiExpanded"), heading, ref bChanged))
							{
								EditorGUI.indentLevel++;

								int numlayers = layersProperty.arraySize;
								for (int j = 0; j < numlayers; ++j)
								{
									SerializedProperty layerProperty = layersProperty.GetArrayElementAtIndex(j);
									if (layerProperty == null)
									{
										continue;
									}

									// Skipping "height" layer on UI since its treated as Houdini-specific layer
									string layerName = layerProperty.FindPropertyRelative("_layerName").stringValue;
									if (layerName.Equals(HEU_Defines.HAPI_HEIGHTFIELD_LAYERNAME_HEIGHT))
									{
										continue;
									}
									layerName = string.Format("Layer: {0}", layerName);

									SerializedProperty uiExpandedProperty = layerProperty.FindPropertyRelative("_uiExpanded");
									bool bExpanded = uiExpandedProperty != null ? uiExpandedProperty.boolValue : true;
									bool bNewExpanded = HEU_EditorUI.DrawFoldOut(bExpanded, layerName);
									if (uiExpandedProperty != null && bExpanded != bNewExpanded)
									{
										bChanged = true;
										uiExpandedProperty.boolValue = bNewExpanded;
									}

									if (!bNewExpanded)
									{
										continue;
									}

									if (HEU_EditorUtility.EditorDrawFloatSliderProperty(layerProperty, "_strength", "Strength", "Amount to multiply the layer values by on import."))
									{
										bStrengthChanged = true;
									}

									HEU_EditorUI.DrawSeparator();
								}

								EditorGUI.indentLevel--;
							}

							if (bStrengthChanged)
							{
								SerializedProperty dirtyProperty = cacheObjectSerialized.FindProperty("_isDirty");
								if (dirtyProperty != null)
								{
									dirtyProperty.boolValue = true;
									bChanged = true;
								}
							}

							if(bChanged)
							{
								cacheObjectSerialized.ApplyModifiedProperties();
							}
						}
					}
				}
			}
			HEU_EditorUI.EndSection();

			HEU_EditorUI.DrawSeparator();
		}

	}

}   // HoudiniEngineUnity