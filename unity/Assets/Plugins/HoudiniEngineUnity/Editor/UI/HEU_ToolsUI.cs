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
	/// Editor UI class for asset custom tools like Paint and Edit.
	/// </summary>
	[CanEditMultipleObjects]
	[CustomEditor(typeof(HEU_AttributesStore))]
	public class HEU_ToolsUI : Editor
	{
		// CONSTANTS --------------------------------------------------------------------------------------------------

		private const string _toolsLabel = "HOUDINI ENGINE - EDITABLE NODES TOOLS";

		private const string _paintValuesLabel = "Paint Values";
		private const string _paintColorLabel = "Paint Color";
		private const string _paintFillLabel = "Fill";
		private const string _selectedPtsLabel = "# of Selected Pts: ";
		private const string _editValuesLabel = "Selected Pts Values";
		private const string _selectAllPtsLabel = "Select All";

		private const string _cookOnMouseReleaseLabel = "Cook On Mouse Release";
		private const string _brushSizeLabel = "Brush Size";
		private const string _brushOpacityLabel = "Brush Opacity";
		private const string _brushMergeMode = "Merge Mode";

		private const string _brushHandleColor = "Handle Color";
		private const string _affectedAreaColorLabel = "Affected Area Color";

		private const string _editableNodeLabel = "Editable Node";
		private const string _attributeLabel = "Attribute";

		private const string _pointSizeLabel = "Pt Size";
		private const string _unselectedPtColorLabel = "Unselected Pt Color";
		private const string _selectedPtColorLabel = "Selected Pt Color";

		private const string _showOnlyEditGeoLabel = "Show Only Edit Geometry";

		private const string _noMeshForPainting = "Selected node does not have a mesh so painting is unavailable.";

		private GUIContent[] _interactionModeLabels = new GUIContent[]
		{
			new GUIContent(ToolInteractionMode.VIEW.ToString()),
			new GUIContent(ToolInteractionMode.PAINT.ToString()),
			new GUIContent(ToolInteractionMode.EDIT.ToString())
		};

		private const float _mouseWheelBrushSizeMultiplier = 0.05f;
		private const float _intersectionRayLength = 5000f;

		private Color _selectionBoxFillColor = new Color(0.5f, 0.8f, 1f, 0.05f);
		private Color _selectionBoxOutlineColor = new Color(0.9f, 0.9f, 0.9f, 0.8f);

		private const KeyCode _brushResizeKey = KeyCode.LeftShift;

		private const float _mouseSelectDistanceThreshold = 15f;

		private const float _infoPanelHeight = 134;

		private const float _buttonHeight = 22;

		private const float _infoPanelSettingsWidth = 0.6f;


		// CACHE ------------------------------------------------------------------------------------------------------

		private Material _editPointBoxMaterial;

		// Mesh containing selectable points for editing attribute
		private Mesh _editPointBoxMesh;

		// Indices of selected vertices
		private List<int> _editPointsSelectedIndices = new List<int>();

		// Number of vertices in _editPointBoxMesh. Used as a simple check if need to reset selected vertices.
		private int _previousEditMeshPointCount;

		// Drag selection
		private bool _dragMouseDown;
		private Vector3 _dragMouseStart;

		// List of editable nodes and their attributes
		public List<HEU_AttributesStore> _attributesStores = new List<HEU_AttributesStore>();

		// Map of attributes and their serialized data
		private Dictionary<HEU_AttributesStore, SerializedObject> _serializedAttributesStoresCache = new Dictionary<HEU_AttributesStore, SerializedObject>();

		private Camera _currentCamera;

		public enum ToolInteractionMode
		{
			VIEW,
			PAINT,
			EDIT
		}

		private ToolInteractionMode _interactionMode;

		private Rect _editorUIRect;

		private SerializedObject _toolsInfoSerializedObject;

		private HEU_ToolsInfo _toolsInfo;

		// Selected editable node
		private HEU_AttributesStore _selectedAttributesStore;

		// Selected attribute to edit
		private HEU_AttributeData _selectedAttributeData;

		private bool _GUIChanged;

		private HEU_HoudiniAsset _asset;

		private Vector2 _editNodeScrollPos;

		private int _controlID;

		private Event _currentEvent;
		private Vector3 _mousePosition;
		private bool _mouseWithinSceneView;
		private bool _mouseOverInfoPanel;

		private float _screenWidth;
		private float _screenHeight;

		private GUIStyle _toolsBGStyle;

		// LOGIC ------------------------------------------------------------------------------------------------------


		private void OnEnable()
		{
			// This forces UI cache to be regenerated
			_toolsInfo = null;
			_toolsInfoSerializedObject = null;

			GUISkin heuSkin = HEU_EditorUI.LoadHEUSkin();
			_toolsBGStyle = heuSkin.GetStyle("toolsbg"); 

			// Callback will be used to disable this tool and reset state
			Selection.selectionChanged += SelectionChangedCallback;
		}

		/// <summary>
		/// Callback when selection has changed.
		/// </summary>
		private void SelectionChangedCallback()
		{
			Selection.selectionChanged -= SelectionChangedCallback;

			ShowAssetGeometry();

			ClearCache();

			ShowUnityTools();
		}

		public bool DoesCacheNeedRegeneration()
		{
			return _toolsInfo == null || _toolsInfo._recacheRequired || _toolsInfoSerializedObject == null;
		}

		public void GenerateCache()
		{
			_toolsInfo = _asset.ToolsInfo;
			_toolsInfo._recacheRequired = false;
			_toolsInfoSerializedObject = new SerializedObject(_toolsInfo);

			CacheAttributesStores();

			if (_interactionMode == ToolInteractionMode.EDIT)
			{
				UpdateShowOnlyEditGeometry();
			}
		}

		private void ClearCache()
		{
			if (_toolsInfoSerializedObject != null)
			{
				SerializedProperty isPaintingProperty = HEU_EditorUtility.GetSerializedProperty(_toolsInfoSerializedObject, "_isPainting");
				PaintingFinished(isPaintingProperty);
			}

			_selectedAttributesStore = null;
			_selectedAttributeData = null;

			_attributesStores.Clear();
			_serializedAttributesStoresCache.Clear();

			if(_editPointBoxMaterial != null)
			{
				HEU_MaterialFactory.DestroyNonAssetMaterial(_editPointBoxMaterial, false);
				_editPointBoxMaterial = null;
			}

			DestroyEditPointBoxMesh();

			_dragMouseDown = false;

			_asset = null;

			_toolsInfo = null;
		}

		private void DestroyEditPointBoxMesh()
		{
			if(_editPointBoxMesh != null)
			{
				HEU_GeneralUtility.DestroyImmediate(_editPointBoxMesh);
				_editPointBoxMesh = null;
			}
		}

		public void CacheAttributesStores()
		{
			_attributesStores.Clear();
			foreach (Object targetObject in targets)
			{
				HEU_AttributesStore attributeStore = targetObject as HEU_AttributesStore;
				if (attributeStore != null)
				{
					_attributesStores.Add(attributeStore);
				}
			}
		}

		private SerializedObject GetOrCreateSerializedAttributesStore(HEU_AttributesStore attributesStore)
		{
			SerializedObject serializedAttributesStore = null;
			if (!_serializedAttributesStoresCache.TryGetValue(attributesStore, out serializedAttributesStore))
			{
				serializedAttributesStore = new SerializedObject(attributesStore);
				_serializedAttributesStoresCache.Add(attributesStore, serializedAttributesStore);
			}
			return serializedAttributesStore;
		}

		public void DrawToolsEditor(HEU_HoudiniAsset asset)
		{
			_currentCamera = Camera.current;

			_controlID = GUIUtility.GetControlID(FocusType.Passive);

			_currentEvent = Event.current;
			_mousePosition = HEU_EditorUI.GetMousePosition(ref _currentEvent, _currentCamera);
			_mouseWithinSceneView = HEU_GeneralUtility.IsMouseWithinSceneView(_currentCamera, _mousePosition);

			float pixelsPerPoint = HEU_EditorUI.GetPixelsPerPoint();
			_screenWidth = Screen.width / pixelsPerPoint;
			_screenHeight = Screen.height / pixelsPerPoint;

			_editorUIRect = new Rect(10, _screenHeight - (_infoPanelHeight + 45), _screenWidth - 25, _infoPanelHeight);

			_mouseOverInfoPanel = HEU_GeneralUtility.IsMouseOverRect(_currentCamera, _mousePosition, ref _editorUIRect);

			if (_asset == null || (_asset != asset) || DoesCacheNeedRegeneration())
			{
				ClearCache();

				_asset = asset;

				GenerateCache();
			}

			_GUIChanged = false;

			EditorGUI.BeginChangeCheck();

			// Draw the info panel first. This gets the user selected node and attribute.
			DrawInfoPanel();

			// Now update the scene with tool handles (e.g. paint brush) and geo (edit box mesh)
			if (_interactionMode == ToolInteractionMode.VIEW)
			{
				DrawViewModeScene();
			}
			else if (_interactionMode == ToolInteractionMode.PAINT)
			{
				DrawPaintModeScene();
			}
			else if (_interactionMode == ToolInteractionMode.EDIT)
			{
				DrawEditModeScene();
			}

			if (EditorGUI.EndChangeCheck() || _GUIChanged)
			{
				// Presume that if a serialized object was cached, then most likely it was modified
				foreach (var attributeStoresPair in _serializedAttributesStoresCache)
				{
					attributeStoresPair.Value.ApplyModifiedProperties();
				}

				_toolsInfoSerializedObject.ApplyModifiedProperties();
			}

			
			// Ignoe deselect if over info panel
			if(_mouseOverInfoPanel && _currentEvent.type == EventType.MouseDown && (!_currentEvent.control && !_currentEvent.alt && !_currentEvent.shift))
			{
				GUIUtility.hotControl = _controlID;
				_currentEvent.Use();
			}
		}

		private void DrawInfoPanel()
		{
			Handles.BeginGUI();
			{
				Rect labelRect = new Rect(10, _editorUIRect.y - 30, _screenWidth - 25, _buttonHeight + 4);
				GUIStyle toolbarStyle = new GUIStyle(EditorStyles.toolbar);
				toolbarStyle.fixedHeight = _buttonHeight + 8;
				toolbarStyle.fixedWidth = labelRect.width;

				GUILayout.BeginArea(labelRect, toolbarStyle);
				{
					GUIStyle boldLabelStyle = new GUIStyle(EditorStyles.boldLabel);
					boldLabelStyle.alignment = TextAnchor.UpperLeft;

					using (var hl = new GUILayout.HorizontalScope())
					{
						GUILayout.Label(_toolsLabel, boldLabelStyle);

						int selectedMode = GUILayout.Toolbar((int)_interactionMode, _interactionModeLabels, GUILayout.Height(_buttonHeight));
						if (selectedMode != (int)_interactionMode)
						{
							SwitchToMode((ToolInteractionMode)selectedMode);
						}
					}
				}
				GUILayout.EndArea();

				GUILayout.BeginArea(_editorUIRect, _toolsBGStyle);
				{
					using (var horizontalLayout = new GUILayout.HorizontalScope())
					{
						switch (_interactionMode)
						{
							case ToolInteractionMode.VIEW:
							{
								DrawViewModeInfo();
								break;
							}
							case ToolInteractionMode.PAINT:
							{
								DrawPaintModeInfo();
								break;
							}
							case ToolInteractionMode.EDIT:
							{
								DrawEditModeInfo();
								break;
							}
						}
					}
				}
				GUILayout.EndArea();
			}
			Handles.EndGUI();
		}

		private void SwitchToMode(ToolInteractionMode newMode)
		{
			_interactionMode = newMode;

			if(_interactionMode == ToolInteractionMode.VIEW)
			{
				// Destroying the edit mesh removes it from the viewport if it has been drawn
				DestroyEditPointBoxMesh();

				ShowUnityTools();

				if (_selectedAttributesStore != null)
				{
					_selectedAttributesStore.DisablePaintCollider();
				}

				ShowAssetGeometry();
			}
			else if (_interactionMode == ToolInteractionMode.PAINT)
			{
				// Destroying the edit mesh removes it from the viewport if it has been drawn
				DestroyEditPointBoxMesh();

				HideUnityTools();

				ShowAssetGeometry();

				if (_selectedAttributesStore != null)
				{
					_selectedAttributesStore.EnablePaintCollider();
				}
			}
			else if (_interactionMode == ToolInteractionMode.EDIT)
			{
				HideUnityTools();

				if (_selectedAttributesStore != null)
				{
					_selectedAttributesStore.DisablePaintCollider();
				}

				UpdateShowOnlyEditGeometry();
			}

			SceneView.RepaintAll();
		}

		private void DrawViewModeInfo()
		{
			float uiWidth = _editorUIRect.width * _infoPanelSettingsWidth;

			char upArrow = '\u25B2';
			char downArrow = '\u25BC';
			float arrayWidth = 25;

			GUIStyle editNodesBoxStyle = new GUIStyle(GUI.skin.textArea);
			GUIStyle entryStyle = new GUIStyle(GUI.skin.box);

			using (var hs = new EditorGUILayout.HorizontalScope())
			{
				using (var vs = new EditorGUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.MaxWidth(uiWidth)))
				{
					EditorGUILayout.LabelField("This tool allows to Paint & Edit POINT attributes of Editable nodes.");
					EditorGUILayout.LabelField("Painting vertex colors is directly supported if a Paint SOP is made editable.");
					EditorGUILayout.LabelField("Use node list on the right to re-order editable nodes by order of edit operations.");
					EditorGUILayout.LabelField("Nodes that take inputs from other editable nodes should come after them in the list.");
				}

				using (var vs = new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
				{
					// Draw each editable node (attribute store), along with buttons to re-order the node.

					using (var hs2 = new EditorGUILayout.HorizontalScope())
					{
						HEU_EditorUI.DrawHeadingLabel("EDIT ORDER");

						SerializedProperty cookUpstreamSerializedProperty = HEU_EditorUtility.GetSerializedProperty(_toolsInfoSerializedObject, "_alwaysCookUpstream");
						if (cookUpstreamSerializedProperty != null)
						{
							EditorGUILayout.PropertyField(cookUpstreamSerializedProperty,
								new GUIContent("Always Cook Inputs", "For multiple editable nodes, this forces each one to always cook upstream inputs first before applying edits."),
								GUILayout.Width(168));
						}
					}

					using (var vs2 = new EditorGUILayout.VerticalScope(editNodesBoxStyle))
					{
						using (var scrollScope = new EditorGUILayout.ScrollViewScope(_editNodeScrollPos))
						{
							_editNodeScrollPos = scrollScope.scrollPosition;

							int numStores = _attributesStores.Count;
							for (int i = 0; i < numStores; ++i)
							{
								using (var hsi = new EditorGUILayout.HorizontalScope(entryStyle))
								{
									if (GUILayout.Button(upArrow.ToString(), GUILayout.MaxWidth(arrayWidth)))
									{
										if (i > 0)
										{
											_asset.ReorderAttributeStore(i, i - 1);
										}
									}

									if (GUILayout.Button(downArrow.ToString(), GUILayout.MaxWidth(arrayWidth)))
									{
										if (i < numStores - 1)
										{
											_asset.ReorderAttributeStore(i, i + 1);
										}
									}

									EditorGUILayout.LabelField(_attributesStores[i].GeoName);
								}
							}

						}
					}
				}
			}
		}

		private void DrawPaintModeInfo()
		{
			bool bFillInvoked = false;

			float uiWidth = _editorUIRect.width * _infoPanelSettingsWidth;

			using (var verticalSpace = new GUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.MaxWidth(uiWidth)))
			{
				// Attribute Selection, and paint values

				DrawAttributeSelection();

				DrawPaintAttributeValues();
			}

			using (var verticalSpace = new GUILayout.VerticalScope(EditorStyles.helpBox))
			{
				// Tool Settings

				HEU_EditorUtility.EditorDrawBoolProperty(_toolsInfoSerializedObject, "_liveUpdate", _cookOnMouseReleaseLabel, "Auto-cook on mouse release when painting.");

				HEU_EditorUtility.EditorDrawFloatProperty(_toolsInfoSerializedObject, "_paintBrushSize", _brushSizeLabel, "Change brush size via Shift + drag or Shift + mouse scroll.");
				HEU_EditorUtility.EditorDrawFloatProperty(_toolsInfoSerializedObject, "_paintBrushOpacity", _brushOpacityLabel, "Blending factor when merging source and destination colors.");

				HEU_EditorUtility.EditorDrawSerializedProperty(_toolsInfoSerializedObject, "_paintMergeMode", _brushMergeMode, "How paint color is applied to surface.");

				HEU_EditorUtility.EditorDrawSerializedProperty(_toolsInfoSerializedObject, "_brushHandleColor", _brushHandleColor, "Color of the brush handle in Scene.");

				bFillInvoked = GUILayout.Button(_paintFillLabel, GUILayout.Height(_buttonHeight));
			}

			if (_selectedAttributesStore != null)
			{
				if (bFillInvoked)
				{
					HEU_ToolsInfo toolsInfo = _toolsInfoSerializedObject.targetObject as HEU_ToolsInfo;
					_selectedAttributesStore.FillAttribute(_selectedAttributeData, toolsInfo);

					_GUIChanged = true;
				}
			}
		}

		private void DrawAttributeSelection()
		{
			// Try to re-use the last selected node
			string lastSelectedNodeName = null;
			SerializedProperty lastSelectedNodeNameProperty = HEU_EditorUtility.GetSerializedProperty(_toolsInfoSerializedObject, "_lastAttributeNodeName");
			if (lastSelectedNodeNameProperty != null)
			{
				lastSelectedNodeName = lastSelectedNodeNameProperty.stringValue;
			}

			HEU_AttributesStore foundAttributeStore = null;

			// Get the names of the all editable nodes having an attribute store for this asset
			// While doing that, we'll find the last selected attribute store
			int lastSelectedIndex = 0;
			List<string> nodeNames = new List<string>();
			foreach (HEU_AttributesStore attributeStore in _attributesStores)
			{
				string geoName = attributeStore.GeoName;
				if (!nodeNames.Contains(geoName))
				{
					nodeNames.Add(geoName);

					// Either re-select last selected node, or select the first one found
					if (string.IsNullOrEmpty(lastSelectedNodeName) || lastSelectedNodeName.Equals(geoName))
					{
						lastSelectedNodeName = geoName;
						lastSelectedIndex = nodeNames.Count - 1;
						foundAttributeStore = attributeStore;
					}
				}
			}

			// Try to re-use the last selected attribute
			string lastSelectedAttributeName = null;
			SerializedProperty lastSelectedAttributeProperty = HEU_EditorUtility.GetSerializedProperty(_toolsInfoSerializedObject, "_lastAttributeName");
			if (lastSelectedAttributeProperty != null)
			{
				lastSelectedAttributeName = lastSelectedAttributeProperty.stringValue;
			}

			// Display a dropdown list of editable nodes with attribute stores
			int currentSelectedIndex = EditorGUILayout.Popup(_editableNodeLabel, lastSelectedIndex, nodeNames.ToArray());
			if (currentSelectedIndex != lastSelectedIndex)
			{
				// User changed node selection, so update it
				lastSelectedNodeName = nodeNames[currentSelectedIndex];

				foundAttributeStore = null;

				foreach (HEU_AttributesStore attributeStore in _attributesStores)
				{
					string geoName = attributeStore.GeoName;
					if (geoName.Equals(lastSelectedNodeName))
					{
						foundAttributeStore = attributeStore;
						break;
					}
				}

				SetSelectedAttributeStore(foundAttributeStore);

				// Reset selected attribute to first attribute
				SetSelectedAttributeData(_selectedAttributesStore.GetAttributeData(0));
				lastSelectedAttributeName = _selectedAttributeData._name;
				lastSelectedAttributeProperty.stringValue = lastSelectedAttributeName;

				lastSelectedNodeNameProperty.stringValue = lastSelectedNodeName;

				_GUIChanged = true;
			}
			else
			{
				// Since selected node hasn't changed, re-use the last selected attribute

				SetSelectedAttributeStore(foundAttributeStore);

				SetSelectedAttributeData(_selectedAttributesStore.GetAttributeData(lastSelectedAttributeName));
			}

			// Get attribute names for selected node
			List<string> attributeNames = _selectedAttributesStore.GetAttributeNames();

			// Find the last selected attribute index
			int lastAttributeIndex = -1;
			if (!string.IsNullOrEmpty(lastSelectedAttributeName))
			{
				for(int i = 0; i < attributeNames.Count; ++i)
				{
					if (lastSelectedAttributeName.Equals(attributeNames[i]))
					{
						lastAttributeIndex = i;
						break;
					}
				}
			}

			// Use first attribute as default if none selected last time
			if(lastAttributeIndex == -1)
			{
				lastAttributeIndex = 0;
				HEU_AttributeData data = _selectedAttributesStore.GetAttributeData(0);
				if (data != null)
				{
					lastSelectedAttributeProperty.stringValue = data._name;
				}
			}

			// Display attributes as dropdown
			if (attributeNames.Count > 0)
			{
				int currentAttributeIndex = EditorGUILayout.Popup(_attributeLabel, lastAttributeIndex, attributeNames.ToArray());
				if (currentAttributeIndex != lastAttributeIndex)
				{
					// User changed attribute selection, so update it
					SetSelectedAttributeData(_selectedAttributesStore.GetAttributeData(attributeNames[currentAttributeIndex]));
					lastSelectedAttributeProperty.stringValue = _selectedAttributeData._name;
				}
			}
		}

		private void DrawPaintAttributeValues()
		{
			// Display the values as editable fields

			if (_selectedAttributeData == null)
			{
				return;
			}

			if(!_selectedAttributesStore.HasMeshForPainting())
			{
				HEU_EditorUI.DrawWarningLabel(_noMeshForPainting);
				return;
			}

			SerializedProperty selectedToolsValuesProperty = null;

			if (_selectedAttributeData._attributeType == HEU_AttributeData.AttributeType.INT)
			{
				selectedToolsValuesProperty = HEU_EditorUtility.GetSerializedProperty(_toolsInfoSerializedObject, "_paintIntValue");
				if (selectedToolsValuesProperty != null)
				{
					ResizeSerializedPropertyArray(selectedToolsValuesProperty, _selectedAttributeData._attributeInfo.tupleSize);
					HEU_EditorUtility.EditorDrawArrayProperty(selectedToolsValuesProperty, HEU_EditorUtility.EditorDrawIntProperty, _paintValuesLabel);
				}
			}
			else if (_selectedAttributeData._attributeType == HEU_AttributeData.AttributeType.FLOAT)
			{
				selectedToolsValuesProperty = HEU_EditorUtility.GetSerializedProperty(_toolsInfoSerializedObject, "_paintFloatValue");
				if (selectedToolsValuesProperty != null)
				{
					ResizeSerializedPropertyArray(selectedToolsValuesProperty, _selectedAttributeData._attributeInfo.tupleSize);
					HEU_EditorUtility.EditorDrawArrayProperty(selectedToolsValuesProperty, HEU_EditorUtility.EditorDrawFloatProperty, _paintValuesLabel);
				}

				// Display paint color selector if this is a color attribute
				if (_selectedAttributeData.IsColorAttribute())
				{
					Color color = Color.white;

					if (selectedToolsValuesProperty.arraySize >= 3)
					{
						color.r = selectedToolsValuesProperty.GetArrayElementAtIndex(0).floatValue;
						color.g = selectedToolsValuesProperty.GetArrayElementAtIndex(1).floatValue;
						color.b = selectedToolsValuesProperty.GetArrayElementAtIndex(2).floatValue;

						if (selectedToolsValuesProperty.arraySize >= 4)
						{
							color.a = selectedToolsValuesProperty.GetArrayElementAtIndex(3).floatValue;
						}
					}

					Color newColor = EditorGUILayout.ColorField(_paintColorLabel, color);
					if (color != newColor)
					{
						if (selectedToolsValuesProperty.arraySize >= 3)
						{
							selectedToolsValuesProperty.GetArrayElementAtIndex(0).floatValue = newColor.r;
							selectedToolsValuesProperty.GetArrayElementAtIndex(1).floatValue = newColor.g;
							selectedToolsValuesProperty.GetArrayElementAtIndex(2).floatValue = newColor.b;

							if (selectedToolsValuesProperty.arraySize >= 4)
							{
								selectedToolsValuesProperty.GetArrayElementAtIndex(3).floatValue = newColor.a;
							}
						}
					}
				}

			}
			else if (_selectedAttributeData._attributeType == HEU_AttributeData.AttributeType.STRING)
			{
				selectedToolsValuesProperty = HEU_EditorUtility.GetSerializedProperty(_toolsInfoSerializedObject, "_paintStringValue");
				if (selectedToolsValuesProperty != null)
				{
					ResizeSerializedPropertyArray(selectedToolsValuesProperty, _selectedAttributeData._attributeInfo.tupleSize);
					HEU_EditorUtility.EditorDrawArrayProperty(selectedToolsValuesProperty, HEU_EditorUtility.EditorDrawTextProperty, _paintValuesLabel);
				}
			}

			if (!_selectedAttributeData.IsColorAttribute())
			{
				HEU_EditorUtility.EditorDrawSerializedProperty(_toolsInfoSerializedObject, "_affectedAreaPaintColor", _affectedAreaColorLabel, "Color to show painted area.");
			}
		}

		private void DrawEditAttributeValues()
		{
			if(_selectedAttributeData == null)
			{
				return;
			}

			if(_editPointsSelectedIndices.Count == 0)
			{
				return;
			}

			int arraySize = _selectedAttributeData._attributeInfo.tupleSize;

			// Use first values found as current values
			int ptIndex = _editPointsSelectedIndices[0] * arraySize;

			// Display values of selected pts which can be edited
			if (_selectedAttributeData._attributeType == HEU_AttributeData.AttributeType.INT)
			{
				// Copy values to display
				int[] values = new int[arraySize];
				for (int i = 0; i < arraySize; ++i)
				{
					values[i] = _selectedAttributeData._intValues[ptIndex + i];
				}

				// Display values and update attribute if changed
				bool bChanged = HEU_EditorUI.DrawArray<int>(_editValuesLabel, ref values, HEU_EditorUI.DrawFieldInt);
				if (bChanged)
				{
					int numPts = _editPointsSelectedIndices.Count;
					for (int i = 0; i < numPts; ++i)
					{
						ptIndex = _editPointsSelectedIndices[i] * arraySize;

						HEU_AttributesStore.SetAttributeEditValueInt(_selectedAttributeData, ptIndex, values);
					}

					_GUIChanged = true;

					HEU_AttributesStore.SetAttributeDataDirty(_selectedAttributeData);
				}
			}
			else if (_selectedAttributeData._attributeType == HEU_AttributeData.AttributeType.FLOAT)
			{
				// Copy values to display
				float[] values = new float[arraySize];
				for(int i = 0; i < arraySize; ++i)
				{
					values[i] = _selectedAttributeData._floatValues[ptIndex + i];
				}

				// Display values and update attribute if changed
				bool bChanged = HEU_EditorUI.DrawArray<float>(_editValuesLabel, ref values, HEU_EditorUI.DrawFieldFloat);
				if (bChanged)
				{
					int numPts = _editPointsSelectedIndices.Count;
					for (int i = 0; i < numPts; ++i)
					{
						ptIndex = _editPointsSelectedIndices[i] * arraySize;

						HEU_AttributesStore.SetAttributeEditValueFloat(_selectedAttributeData, ptIndex, values);
					}

					_GUIChanged = true;

					HEU_AttributesStore.SetAttributeDataDirty(_selectedAttributeData);
				}
			}
			else if (_selectedAttributeData._attributeType == HEU_AttributeData.AttributeType.STRING)
			{
				// Copy values to display
				string[] values = new string[arraySize];
				for (int i = 0; i < arraySize; ++i)
				{
					values[i] = _selectedAttributeData._stringValues[ptIndex + i];
				}

				// Display values and update attribute if changed
				bool bChanged = HEU_EditorUI.DrawArray<string>(_editValuesLabel, ref values, HEU_EditorUI.DrawFieldString);
				if (bChanged)
				{
					int numPts = _editPointsSelectedIndices.Count;
					for (int i = 0; i < numPts; ++i)
					{
						ptIndex = _editPointsSelectedIndices[i] * arraySize;

						HEU_AttributesStore.SetAttributeEditValueString(_selectedAttributeData, ptIndex, values);
					}

					_GUIChanged = true;

					HEU_AttributesStore.SetAttributeDataDirty(_selectedAttributeData);
				}
			}
		}

		private void ResizeSerializedPropertyArray(SerializedProperty arrayProperty, int newSize)
		{
			if(arrayProperty.arraySize != newSize)
			{
				arrayProperty.arraySize = newSize;
				
				_GUIChanged = true;
			}
		}

		private void SetSelectedAttributeStore(HEU_AttributesStore newStore)
		{
			if (_selectedAttributesStore == newStore)
			{
				return;
			}

			if (_selectedAttributesStore != null)
			{
				_selectedAttributesStore.DisablePaintCollider();
			}

			_selectedAttributesStore = null;
			DestroyEditPointBoxMesh();

			if (newStore != null)
			{
				SerializedObject serializedObject = GetOrCreateSerializedAttributesStore(newStore);
				_selectedAttributesStore = serializedObject.targetObject as HEU_AttributesStore;
			}

			if (_interactionMode == ToolInteractionMode.EDIT)
			{
				UpdateShowOnlyEditGeometry();
			}
			else if (_interactionMode == ToolInteractionMode.PAINT)
			{
				if (_selectedAttributesStore != null)
				{
					_selectedAttributesStore.EnablePaintCollider();
				}
			}
		}

		private void SetSelectedAttributeData(HEU_AttributeData newAttr)
		{
			_selectedAttributeData = newAttr;
		}

		private void DrawEditModeInfo()
		{
			float uiWidth = _editorUIRect.width * _infoPanelSettingsWidth;

			using (var verticalSpace = new GUILayout.VerticalScope(EditorStyles.helpBox, GUILayout.MaxWidth(uiWidth)))
			{
				// Attribute Selection and editing values

				DrawAttributeSelection();

				DrawEditAttributeValues();
			}

			using (var verticalSpace = new GUILayout.VerticalScope(EditorStyles.helpBox))
			{
				// Tool Settings

				EditorGUI.BeginChangeCheck();

				SerializedProperty showGeoProperty = HEU_EditorUtility.GetSerializedProperty(_toolsInfoSerializedObject, "_showOnlyEditGeometry");
				if (showGeoProperty != null)
				{
					bool bOldValue = showGeoProperty.boolValue;
					EditorGUILayout.PropertyField(showGeoProperty, new GUIContent(_showOnlyEditGeoLabel, "Show only this edit node's geometry."));
					if (bOldValue != showGeoProperty.boolValue)
					{
						UpdateShowOnlyEditGeometry();
					}
				}

				HEU_EditorUtility.EditorDrawFloatProperty(_toolsInfoSerializedObject, "_editPointBoxSize", _pointSizeLabel, "Change size of the point box visualization.");

				HEU_EditorUtility.EditorDrawSerializedProperty(_toolsInfoSerializedObject, "_editPointBoxUnselectedColor", _unselectedPtColorLabel);

				HEU_EditorUtility.EditorDrawSerializedProperty(_toolsInfoSerializedObject, "_editPointBoxSelectedColor", _selectedPtColorLabel);

				EditorGUILayout.LabelField(_selectedPtsLabel + _editPointsSelectedIndices.Count);

				if (GUILayout.Button(_selectAllPtsLabel, GUILayout.Height(_buttonHeight)))
				{
					SelectAllPoints();
				}

				if (EditorGUI.EndChangeCheck())
				{
					GenerateEditPointBoxNewMesh();
				}
			}
		}

		// SCENE DRAWING ----------------------------------------------------------------------------------------------

		private void DrawViewModeScene()
		{

		}

		private void DrawPaintModeScene()
		{
			if(_selectedAttributesStore == null || _selectedAttributeData == null)
			{
				// Nothing to do if no attribute selected
				return;
			}

			if(!_selectedAttributesStore.HasMeshForPainting())
			{
				// Painting requires a mesh so nothing to do
				return;
			}

			if (!_mouseWithinSceneView)
			{
				return;
			}

			float brushRadius = GetBrushRadius();

			// TODO: use Handles.DrawingScope
			Color originalHandleColor = Handles.color;
			Color newHandleColor = originalHandleColor;
			
			SerializedProperty handleColorProperty = HEU_EditorUtility.GetSerializedProperty(_toolsInfoSerializedObject, "_brushHandleColor");
			if(handleColorProperty != null)
			{
				newHandleColor = handleColorProperty.colorValue;
			}

			HEU_ToolsInfo.PaintMergeMode paintMergeMode = HEU_ToolsInfo.PaintMergeMode.REPLACE;
			SerializedProperty paintMergeProperty = HEU_EditorUtility.GetSerializedProperty(_toolsInfoSerializedObject, "_paintMergeMode");
			if (paintMergeProperty != null)
			{
				paintMergeMode = (HEU_ToolsInfo.PaintMergeMode)paintMergeProperty.intValue;
			}

			SerializedProperty isPaintingProperty = HEU_EditorUtility.GetSerializedProperty(_toolsInfoSerializedObject, "_isPainting");

			// Enable the mesh collider so that we can raycast to paint using brush
			MeshCollider collider = _selectedAttributesStore.GetPaintMeshCollider();
			if (collider != null)
			{
				Ray ray = _currentCamera.ScreenPointToRay(_mousePosition);
				ray.origin = _currentCamera.transform.position;

				RaycastHit hit;
				if (collider.Raycast(ray, out hit, _intersectionRayLength))
				{
					if (_currentEvent.type == EventType.ScrollWheel && _currentEvent.shift)
					{
						// Brush resize
						brushRadius -= _currentEvent.delta.y * _mouseWheelBrushSizeMultiplier;
						brushRadius = UpdateBrushSize(brushRadius);

						_GUIChanged = true;
						_currentEvent.Use();
					}
					else if (_currentEvent.type == EventType.MouseDrag && _currentEvent.shift)
					{
						// Brush resize
						brushRadius += _currentEvent.delta.x * _mouseWheelBrushSizeMultiplier;
						brushRadius = UpdateBrushSize(brushRadius);

						_GUIChanged = true;
						_currentEvent.Use();
					}
					else if (_currentEvent.button == 0 && !_currentEvent.shift && !_currentEvent.alt && !_currentEvent.control && !_mouseOverInfoPanel)
					{
						// Painting

						if (_currentEvent.type == EventType.MouseDown && !isPaintingProperty.boolValue)
						{
							PaintingStarted(isPaintingProperty);
						}

						if (isPaintingProperty.boolValue)
						{
							if(_currentEvent.type == EventType.MouseDown || _currentEvent.type == EventType.MouseDrag)
							{
								HandlePaintEvent(hit, brushRadius, paintMergeMode);

								_currentEvent.Use();
							}
						}
					}

					if (!_mouseOverInfoPanel)
					{
						Handles.color = newHandleColor;
						Vector3 endPt = hit.point + (Vector3.Normalize(hit.normal) * brushRadius);
						Handles.DrawAAPolyLine(2f, new Vector3[] { hit.point, endPt });

						HEU_EditorUI.DrawCircleCap(_controlID, hit.point, Quaternion.FromToRotation(Vector3.forward, hit.normal), brushRadius);
					}
				}
			}

			switch (_currentEvent.type)
			{
				case EventType.MouseDown:
				{
					// Don't use event here as it will ignore mouse camera controls
					break;
				}
				case EventType.MouseUp:
				{
					if (_currentEvent.button == 0)
					{
						if(!_currentEvent.shift && !_currentEvent.alt && !_currentEvent.control)
						{
							if (isPaintingProperty != null && isPaintingProperty.boolValue)
							{
								_currentEvent.Use();
							}

							PaintingFinished(isPaintingProperty);
						}
					}
					break;
				}
				case EventType.MouseMove:
				{
					// Use the mouse move event will force a repaint allowing for much more responsive UI
					_currentEvent.Use();
					break;
				}
				case EventType.Layout:
				{
					// This disables deselection on asset while in Edit mode
					HandleUtility.AddDefaultControl(_controlID);

					break;
				}
				case EventType.Repaint:
				{
					
					break;
				}
			}

			Handles.color = originalHandleColor;
		}

		private void DrawEditModeScene()
		{
			if (_selectedAttributesStore == null || _selectedAttributeData == null)
			{
				// Nothing to do if no attribute selected
				return;
			}

			// Get edit mesh and draw it if exists. Create it if doesn't.
			if (_editPointBoxMesh != null && _editPointBoxMaterial != null)
			{
				Vector3 meshPosition = _selectedAttributesStore.OutputTransform.position;
				Graphics.DrawMesh(_editPointBoxMesh, meshPosition, Quaternion.identity, _editPointBoxMaterial, 0);
			}
			else if (_editPointBoxMesh == null)
			{
				GenerateEditPointBoxNewMesh();
			}

			EventType eventType = _currentEvent.GetTypeForControl(_controlID);
			switch (eventType)
			{
				case EventType.MouseDown:
				{
					// Don't use event here as it will ignore mouse camera controls

					if (_mouseWithinSceneView && !_dragMouseDown && !_currentEvent.alt && !_currentEvent.control && _currentEvent.button == 0)
					{
						_dragMouseStart = _mousePosition;
						_dragMouseDown = true;

						_currentEvent.Use();
					}

					break;
				}
				case EventType.MouseUp:
				{
					if (_currentEvent.button == 0)
					{
						if(_dragMouseDown)
						{
							// Note that as user was dragging, the points were auto-selected, so we shouldn't
							// need to do anything here other than stop dragging.
							_dragMouseDown = false;

							_currentEvent.Use();

							GenerateEditPointBoxNewMesh();
						}
						else
						{
							// Select point or deselect if none selected

							if (_mouseWithinSceneView && !_mouseOverInfoPanel && !_currentEvent.alt && !_currentEvent.control)
							{
								SelectPointsWithMouseClick(_mousePosition);

								if (_editPointsSelectedIndices.Count == 0)
								{
									DeselectAllSelectedPoints();
								}

								GenerateEditPointBoxNewMesh();
							}
						}
					}
					break;
				}
				case EventType.MouseMove:
				{
					// Use the mouse move event will force a repaint allowing for much more responsive UI
					_currentEvent.Use();
					break;
				}
				case EventType.MouseDrag:
				{
					if (_dragMouseDown)
					{
						_currentEvent.Use();
					}

					break;
				}
				case EventType.Layout:
				{
					// This disables deselection on asset while in Add mode
					HandleUtility.AddDefaultControl(_controlID);

					break;
				}
				case EventType.Repaint:
				{
					if (_dragMouseDown)
					{
						DrawSelectionBox(_mousePosition, true);
					}

					break;
				}
			}
		}

		private void PaintingStarted(SerializedProperty isPaintingProperty)
		{
			if (isPaintingProperty != null && !isPaintingProperty.boolValue)
			{
				isPaintingProperty.boolValue = true;
				_GUIChanged = true;

				// Hide asset geometry in order to show the paint object
				_asset.HideAllGeometry();

				if (_selectedAttributesStore != null)
				{
					_selectedAttributesStore.ShowPaintMesh();
				}
			}
		}

		private void PaintingFinished(SerializedProperty isPaintingProperty)
		{
			if (isPaintingProperty != null && isPaintingProperty.boolValue)
			{
				isPaintingProperty.boolValue = false;
				_GUIChanged = true;

				if (_selectedAttributesStore != null)
				{
					_selectedAttributesStore.HidePaintMesh();
				}

				// Show asset geometry once done painting
				_asset.CalculateVisibility();
			}
		}

		private float GetBrushRadius()
		{
			SerializedProperty property = HEU_EditorUtility.GetSerializedProperty(_toolsInfoSerializedObject, "_paintBrushSize");
			return (property != null) ? property.floatValue : 1f;
		}

		private float UpdateBrushSize(float radius)
		{
			if(radius < 0.01f)
			{
				radius = 0.01f;
			}

			SerializedProperty property = HEU_EditorUtility.GetSerializedProperty(_toolsInfoSerializedObject, "_paintBrushSize");
			if(property != null && (Mathf.Abs(property.floatValue - radius) >= float.Epsilon))
			{
				property.floatValue = radius;
			}

			return radius;
		}

		private void HandlePaintEvent(RaycastHit hit, float brushRadius, HEU_ToolsInfo.PaintMergeMode paintMergeMode)
		{
			if(_selectedAttributesStore == null || _selectedAttributeData == null)
			{
				return;
			}

			HEU_AttributesStore.SetAttributeValueFunc setAttrFunc = HEU_AttributesStore.GetAttributeSetValueFunction(_selectedAttributeData._attributeType, paintMergeMode);
			if(setAttrFunc == null)
			{
				return;
			}

			Vector3[] positionArray = new Vector3[0];
			_selectedAttributesStore.GetPositionAttributeValues(out positionArray);

			int[] indices = new int[0];
			_selectedAttributesStore.GetVertexIndices(out indices);

			int numPositions = positionArray.Length;
			if (positionArray != null && numPositions > 0 && indices != null && indices.Length > 0)
			{
				Vector3 localHitPoint = _selectedAttributesStore.OutputTransform.InverseTransformPoint(hit.point);

				Mesh mesh = _selectedAttributesStore.OutputMesh;
				Color[] colors = mesh.colors;

				for (int posIndex = 0; posIndex < numPositions; ++posIndex)
				{
					float distance = Vector3.Distance(localHitPoint, positionArray[posIndex]);
					if (distance <= brushRadius)
					{
						float paintFactor = Mathf.Abs((brushRadius - distance) / brushRadius) * _toolsInfo._paintBrushOpacity;

						// Update the attribute
						_selectedAttributesStore.PaintAttribute(_selectedAttributeData, _toolsInfo, posIndex, paintFactor, setAttrFunc);

						// Get the paint color
						Color paintColor = _toolsInfo._affectedAreaPaintColor;
						if (_selectedAttributeData.IsColorAttribute())
						{
							if (_selectedAttributeData._attributeInfo.tupleSize >= 3 && _toolsInfo._paintFloatValue.Length >= 3)
							{
								paintColor.r = _toolsInfo._paintFloatValue[0];
								paintColor.g = _toolsInfo._paintFloatValue[1];
								paintColor.b = _toolsInfo._paintFloatValue[2];

								if (_selectedAttributeData._attributeInfo.tupleSize >= 4 && _toolsInfo._paintFloatValue.Length >= 4)
								{
									paintColor.a = _toolsInfo._paintFloatValue[3];
								}
							}
						}

						// Update local temporary mesh to show area of effect.
						// The position index, a point attribute, must be mapped to the vertex color,
						// which is a vertex attribute, via the vertex index
						int numIndices = indices.Length;
						for(int i = 0; i < numIndices; ++i)
						{
							if(indices[i] == posIndex && i < colors.Length)
							{
								colors[i] = Color.Lerp(colors[i], paintColor, paintFactor);
							}
						}
					}
				}

				mesh.colors = colors;
			}
		}

		private void GenerateEditPointBoxNewMesh()
		{
			if (_selectedAttributesStore == null)
			{
				return;
			}

			Vector3[] positionArray = new Vector3[0];
			_selectedAttributesStore.GetPositionAttributeValues(out positionArray);

			int numPoints = positionArray.Length;
			if (numPoints != _previousEditMeshPointCount)
			{
				_editPointsSelectedIndices.Clear();
				_previousEditMeshPointCount = numPoints;
			}

			if (numPoints > 0)
			{
				float boxSize = HEU_EditorUtility.GetSerializedProperty(_toolsInfoSerializedObject, "_editPointBoxSize").floatValue;
				Color unselectedColor = HEU_EditorUtility.GetSerializedProperty(_toolsInfoSerializedObject, "_editPointBoxUnselectedColor").colorValue;
				Color selectedColor = HEU_EditorUtility.GetSerializedProperty(_toolsInfoSerializedObject, "_editPointBoxSelectedColor").colorValue;

				if (_editPointBoxMaterial == null)
				{
					_editPointBoxMaterial = HEU_MaterialFactory.CreateNewHoudiniStandardMaterial("", "EditPointMaterial", false);
				}

				Color[] pointColors = new Color[numPoints];
				for (int i = 0; i < numPoints; ++i)
				{
					pointColors[i] = unselectedColor;
				}

				int numSelected = _editPointsSelectedIndices.Count;
				for (int i = 0; i < numSelected; ++i)
				{
					pointColors[_editPointsSelectedIndices[i]] = selectedColor;
				}

				_editPointBoxMesh = HEU_GeometryUtility.GenerateCubeMeshFromPoints(positionArray, pointColors, boxSize);

				_GUIChanged = true;
			}
		}

		private void DrawSelectionBox(Vector3 mousePosition, bool bAutoSelectPoints)
		{
			// First draw the selection box from drag start to current mouse position.

			// Calculating the bounding box in screenspace then converting to world seems to
			// produce the best lines in the Scene view regardless of camera orientation.
			Vector3 xVec = new Vector3((mousePosition.x - _dragMouseStart.x), 0, 0);
			Vector3 yVec = new Vector3(0, (mousePosition.y - _dragMouseStart.y), 0);

			Vector3 s00 = _dragMouseStart;
			Vector3 s01 = _dragMouseStart + xVec;
			Vector3 s10 = _dragMouseStart + yVec;
			Vector3 s11 = _dragMouseStart + xVec + yVec;

			Vector3 camFwd = _currentCamera.transform.forward;
			float depth = Mathf.Abs((_currentCamera.transform.position + camFwd * 2f).z);
			Vector3 w00 = _currentCamera.ScreenToWorldPoint(new Vector3(s00.x, s00.y, depth));
			Vector3 w01 = _currentCamera.ScreenToWorldPoint(new Vector3(s01.x, s01.y, depth));
			Vector3 w10 = _currentCamera.ScreenToWorldPoint(new Vector3(s10.x, s10.y, depth));
			Vector3 w11 = _currentCamera.ScreenToWorldPoint(new Vector3(s11.x, s11.y, depth));

			Color defaultColor = Handles.color;
			Handles.color = Color.white;
			Vector3[] lines = new Vector3[]
			{
				w00, w01, w11, w10, w00
			};
			Handles.DrawSolidRectangleWithOutline(lines, _selectionBoxFillColor, _selectionBoxOutlineColor);
			Handles.color = defaultColor;
			
			if (bAutoSelectPoints)
			{
				// Now we select points withing the selection box
				DeselectAllSelectedPoints();

				// We'll use a rect to test against each point
				Rect selectionRect = new Rect(_dragMouseStart.x, _dragMouseStart.y, (mousePosition.x - _dragMouseStart.x), (mousePosition.y - _dragMouseStart.y));

				Transform outputTransform =_selectedAttributesStore.OutputTransform;

				Vector3[] positionArray = new Vector3[0];
				_selectedAttributesStore.GetPositionAttributeValues(out positionArray);

				int numPoints = positionArray.Length;
				for (int i = 0; i < numPoints; ++i)
				{
					// Convert vertices to screenspace
					// _editPointBoxMeshIndices contains the indices of the drawn points (which could be a subset of all mesh points)
					Vector3 pointPosition = outputTransform.localToWorldMatrix * positionArray[i];
					Vector3 pointScreenPosition = HEU_EditorUI.GetHandleWorldToScreenPosition(pointPosition, _currentCamera);

					if (selectionRect.Contains(pointScreenPosition, true))
					{
						_editPointsSelectedIndices.Add(i);
					}
				}
			}
		}
		
		private void DeselectAllSelectedPoints()
		{
			_editPointsSelectedIndices.Clear();
		}

		/// <summary>
		/// Select points under the given mouse position. Multiple points can be selected.
		/// </summary>
		/// <param name="mousePosition"></param>
		private void SelectPointsWithMouseClick(Vector3 mousePosition)
		{
			Transform outputTransform = _selectedAttributesStore.OutputTransform;

			Vector3[] positionArray = new Vector3[0];
			_selectedAttributesStore.GetPositionAttributeValues(out positionArray);

			mousePosition.z = 0f;

			float closestDistance = _mouseSelectDistanceThreshold;

			_editPointsSelectedIndices.Clear();

			int numPoints = positionArray.Length;
			for(int i = 0; i < numPoints; ++i)
			{
				// Convert vertices to screenspace
				// _editPointBoxMeshIndices contains the indices of the drawn points (which could be a subset of all mesh points)
				Vector3 pointPosition = outputTransform.localToWorldMatrix * positionArray[i];
				Vector3 pointScreenPosition = HEU_EditorUI.GetHandleWorldToScreenPosition(pointPosition, _currentCamera);
				pointScreenPosition.z = 0f;

				float deltaMag = Vector3.Distance(pointScreenPosition, mousePosition);
				if(deltaMag < closestDistance)
				{
					closestDistance = deltaMag;

					_editPointsSelectedIndices.Clear();
					_editPointsSelectedIndices.Add(i);
				}
				else if(deltaMag == closestDistance)
				{
					// Could have multiple points at this location
					_editPointsSelectedIndices.Add(i);
				}
			}
		}

		private void SelectAllPoints()
		{
			_editPointsSelectedIndices.Clear();

			if (_selectedAttributesStore != null)
			{
				Vector3[] positionArray = new Vector3[0];
				_selectedAttributesStore.GetPositionAttributeValues(out positionArray);
				int numPoints = positionArray.Length;
				for (int i = 0; i < numPoints; ++i)
				{
					_editPointsSelectedIndices.Add(i);
				}
			}

			GenerateEditPointBoxNewMesh();
		}

		// This shows Unity's transform handle on selected object.
		private void ShowUnityTools()
		{
			Tools.hidden = false;
		}

		// This hides Unity's transform handle on selected object.
		private void HideUnityTools()
		{
			Tools.hidden = true;
		}

		// Show all geometry for the asset.
		private void ShowAssetGeometry()
		{
			if (_selectedAttributesStore != null)
			{
				_selectedAttributesStore.HidePaintMesh();
				_selectedAttributesStore.DisablePaintCollider();
			}

			if (_asset != null)
			{
				_asset.CalculateVisibility();
			}
		}

		// If enabled, show only the edit node's geometry.
		private void UpdateShowOnlyEditGeometry()
		{
			if (_asset != null && _toolsInfoSerializedObject != null)
			{
				SerializedProperty showProperty = HEU_EditorUtility.GetSerializedProperty(_toolsInfoSerializedObject, "_showOnlyEditGeometry");
				if(showProperty != null)
				{
					if(showProperty.boolValue)
					{
						if (_selectedAttributesStore != null)
						{
							// Hide the asset geo and its colliders
							_asset.HideAllGeometry();
							_asset.DisableAllColliders();
						}
					}
					else
					{
						// Show asset geo based on its internal state
						_asset.CalculateVisibility();
						_asset.CalculateColliderState();
					}
				}
			}
		}
	}

}   // HoudiniEngineUnity