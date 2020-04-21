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

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace HoudiniEngineUnity
{
	class HEU_SelectionWindow : EditorWindow
	{
		public static void ShowWindow(SelectionResultHandler selectionHandler, System.Type selectionType)
		{
			bool bUtility = false;
			bool bFocus = true;
			string title = "Input Selection";

			HEU_SelectionWindow window = EditorWindow.GetWindow<HEU_SelectionWindow>(bUtility, title, bFocus);
			window.autoRepaintOnSceneChange = true;
			window._selectionHandler = selectionHandler;
			window._selectionType = selectionType;
		}

		private void OnEnable()
		{
			_requiresSetupUI = true;
			_requiresPopulation = true;

			_filterLocation = (FilterLocationType) HEU_PluginSettings.InputSelectionFilterLocation;
			_filterActive = (FilterActiveType)HEU_PluginSettings.InputSelectionFilterState;
			_filterRoots = HEU_PluginSettings.InputSelectionFilterRoots;
			_filterName = HEU_PluginSettings.InputSelectionFilterName;
		}

		private void OnDisable()
		{
			HEU_PluginSettings.InputSelectionFilterLocation = (int) _filterLocation;
			HEU_PluginSettings.InputSelectionFilterState = (int) _filterActive;
			HEU_PluginSettings.InputSelectionFilterRoots = _filterRoots;
			HEU_PluginSettings.InputSelectionFilterName = _filterName;
		}

		public void OnGUI()
		{
			if (_requiresSetupUI)
			{
				SetupUI();
				_requiresSetupUI = false;
			}

			if(_requiresPopulation)
			{
				PopulateFromScene();
				_requiresPopulation = false;
			}

			using (new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button(_refreshLabel, GUILayout.MaxHeight(_buttonHeight)))
				{
					PopulateFromScene();
				}

				if (GUILayout.Button(_clearLabel, GUILayout.MaxHeight(_buttonHeight)))
				{
					_sceneObjectState.Clear();
					PopulateFromScene();
				}

				if (GUILayout.Button(_selectFound, GUILayout.MaxHeight(_buttonHeight)))
				{
					foreach (GameObject go in _sortedObjects)
					{
						_sceneObjectState[go] = true;
					}
				}
			}

			using (new EditorGUILayout.VerticalScope(_backgroundStyle))
			{
				EditorGUILayout.PrefixLabel(_filterLabel);

				FilterLocationType updatedLocation = (FilterLocationType)EditorGUILayout.EnumPopup(_filterLocationLabel, _filterLocation);
				FilterActiveType updatedActive = _filterActive;

				using (new EditorGUI.DisabledScope(updatedLocation == FilterLocationType.Project))
				{
					updatedActive = (FilterActiveType)EditorGUILayout.EnumPopup(_filterActiveLabel, _filterActive);
				}

				bool updateRoots = EditorGUILayout.Toggle(_filterRootLabel, _filterRoots);

				string updatedName = EditorGUILayout.TextField(_filterNameLabel, _filterName);

				if (updatedLocation != _filterLocation || updatedActive != _filterActive || !updatedName.Equals(_filterName) || updateRoots != _filterRoots)
				{
					_filterLocation = updatedLocation;
					_filterActive = updatedActive;
					_filterName = updatedName;
					_filterRoots = updateRoots;
					PopulateFromScene();
				}
			}

			using (new EditorGUILayout.VerticalScope(_backgroundStyle))
			{
				EditorGUILayout.PrefixLabel(_selectionObjectsLabel);

				using (var scrollView = new EditorGUILayout.ScrollViewScope(_scrollViewPos))
				{
					_scrollViewPos = scrollView.scrollPosition;

					// Display each gameobject with a toggle. Save the changed gameobject in a list.
					List<GameObject> changedStateGOs = new List<GameObject>();
					bool bSelected = false;

					foreach (GameObject go in _sortedObjects)
					{
						if (!_sceneObjectState.TryGetValue(go, out bSelected))
						{
							continue;
						}

						using (new EditorGUILayout.HorizontalScope())
						{
							bool bUpdated = EditorGUILayout.ToggleLeft("", bSelected, GUILayout.MaxWidth(14), GUILayout.MaxHeight(15));
							if (bUpdated != bSelected)
							{
								changedStateGOs.Add(go);
								EditorGUIUtility.PingObject(go);
							}

							if (GUILayout.Button(go.name, _textButtonStyle, GUILayout.MaxHeight(18)))
							{
								EditorGUIUtility.PingObject(go);
							}

							GUILayout.FlexibleSpace();
						}
					}

					// Apply changed states after, since we can't modify the dict while drawing.
					foreach (var go in changedStateGOs)
					{
						_sceneObjectState[go] = !_sceneObjectState[go];
					}
				}
			}

			EditorGUILayout.Space();

			using (new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button(_acceptLabel, GUILayout.MaxHeight(_buttonHeight)))
				{
					if (_selectionHandler != null)
					{
						List<GameObject> selectedGOs = new List<GameObject>();
						foreach (KeyValuePair<GameObject, bool> stateEntry in _sceneObjectState)
						{
							if (stateEntry.Value)
							{
								selectedGOs.Add(stateEntry.Key);
							}
						}

						if (selectedGOs.Count > 0)
						{
							_selectionHandler(selectedGOs.ToArray());
						}
					}

					this.Close();
				}

				if (GUILayout.Button(_cancelLabel, GUILayout.MaxHeight(_buttonHeight)))
				{
					this.Close();
				}
			}
		}

		private GameObject GetGameObjectFromType(Object obj, System.Type type)
		{
			if (type == typeof(GameObject))
			{
				return obj as GameObject;
			}
			else if(type == typeof(HEU_HoudiniAssetRoot))
			{
				HEU_HoudiniAssetRoot heuRoot = obj as HEU_HoudiniAssetRoot;
				return heuRoot.gameObject;
			}
			else
			{
				Debug.LogErrorFormat("Unsupported type {0} for Selection Window.", type);
				return null;
			}
		}

		private void PopulateFromScene()
		{
			// Populate selection state for specified objects (scene, project, filtered, etc).
			// Copy over selection state from previous population if we have them.

			Object[] foundObjects = Resources.FindObjectsOfTypeAll(_selectionType);

			if (foundObjects == null || foundObjects.Length == 0)
			{
				_sceneObjectState.Clear();
			}
			else
			{
				Dictionary<GameObject, bool> newState = new Dictionary<GameObject, bool>();
				_sortedObjects.Clear();

				System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.CurrentCulture;

				bool bInScene = false;
				bool bActive = false;
				bool bSelected = false;
				bool bUseName = _filterName.Length > 0;
				foreach (Object obj in foundObjects)
				{
					GameObject go = GetGameObjectFromType(obj, _selectionType);
					if (go != null)
					{
						if (bUseName && culture.CompareInfo.IndexOf(go.name, _filterName, System.Globalization.CompareOptions.IgnoreCase) < 0)
						{
							continue;
						}

						bInScene = !HEU_GeneralUtility.IsGameObjectInProject(go);
						bActive = go.activeInHierarchy;

						if (_filterRoots && go.transform.parent != null)
						{
							continue;
						}

						if (bInScene)
						{
							if (_filterLocation == FilterLocationType.Project)
							{
								continue;
							}

							// Filter out scene objects that are hidden or internal
							if ((go.hideFlags & HideFlags.NotEditable) == HideFlags.NotEditable
							|| (go.hideFlags & HideFlags.HideAndDontSave) == HideFlags.HideAndDontSave
							|| (go.hideFlags & HideFlags.HideInInspector) == HideFlags.HideInInspector)
							{
								continue;
							}

							// Filter out by active state
							if ((_filterActive == FilterActiveType.Active && !bActive)
								|| (_filterActive == FilterActiveType.Disabled && bActive))
							{
								continue;
							}
						}
						else if (_filterLocation == FilterLocationType.Scene)
						{
							continue;
						}

						if (!_sceneObjectState.TryGetValue(go, out bSelected))
						{
							bSelected = false;
						}

						newState.Add(go, bSelected);
						_sortedObjects.Add(go);
					}
				}

				_sceneObjectState = newState;

				_sortedObjects.Sort(delegate (GameObject a, GameObject b)
				{
					return a.name.CompareTo(b.name);
				});
			}
		}

		private void SetupUI()
		{
			_refreshLabel = new GUIContent("Refresh", "Refresh objects from project or scene, based on filters.");
			_clearLabel = new GUIContent("Clear", "Clear all selections.");
			_acceptLabel = new GUIContent("Accept", "Accept current selections.");
			_cancelLabel = new GUIContent("Cancel", "Cancel this selection.");

			_selectFound = new GUIContent("Select Found", "Select all of the found objects.");

			_selectionObjectsLabel = new GUIContent("FOUND OBJECTS:");

			_filterLabel = new GUIContent("FILTER OPTIONS:");

			_filterLocationLabel = new GUIContent("Location", "Include scene, project, or all objects.");
			_filterActiveLabel = new GUIContent("State", "Include active, disabled, or all objects.");
			_filterRootLabel = new GUIContent("Roots Only", "Include only root objects.");
			_filterNameLabel = new GUIContent("Name", "Filter by name.");

			_textButtonStyle = new GUIStyle(GUI.skin.label);
			_textButtonStyle.alignment = TextAnchor.MiddleLeft;

			_backgroundStyle = new GUIStyle(GUI.skin.box);
			RectOffset br = _backgroundStyle.margin;
			br.top = 10;
			br.bottom = 6;
			br.left = 4;
			br.right = 4;
			_backgroundStyle.margin = br;

			br = _backgroundStyle.padding;
			br.top = 8;
			br.bottom = 8;
			br.left = 8;
			br.right = 8;
			_backgroundStyle.padding = br;
		}


		//	DATA ------------------------------------------------------------------------------------------------------

		private bool _requiresPopulation = true;

		private Dictionary<GameObject, bool> _sceneObjectState = new Dictionary<GameObject, bool>();
		private List<GameObject> _sortedObjects = new List<GameObject>();

		private bool _requiresSetupUI = true;

		private GUIContent _refreshLabel;
		private GUIContent _clearLabel;
		private GUIContent _acceptLabel;
		private GUIContent _cancelLabel;

		private GUIContent _selectFound;

		private GUIStyle _textButtonStyle;

		private GUIContent _selectionObjectsLabel;
		private GUIContent _filterLabel;

		private GUIContent _filterLocationLabel;
		private GUIContent _filterActiveLabel;
		private GUIContent _filterRootLabel;
		private GUIContent _filterNameLabel;

		private GUIStyle _backgroundStyle;

		private float _buttonHeight = 22;

		private Vector2 _scrollViewPos = Vector2.zero;

		public delegate void SelectionResultHandler(GameObject[] selectedObjects);

		private SelectionResultHandler _selectionHandler;

		private System.Type _selectionType = typeof(GameObject);

		private enum FilterLocationType
		{
			All,
			Scene,
			Project
		}

		private FilterLocationType _filterLocation = FilterLocationType.Scene;

		private enum FilterActiveType
		{
			All,
			Active,
			Disabled
		}

		private FilterActiveType _filterActive = FilterActiveType.Active;

		private string _filterName = "";
		private bool _filterRoots = false;
	}
}
