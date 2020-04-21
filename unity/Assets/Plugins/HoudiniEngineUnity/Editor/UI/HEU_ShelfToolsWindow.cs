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

	public class HEU_ShelfToolsWindow : EditorWindow
	{
		private const float _windowWidth = 400;
		private const float _windowHeight = 400;

		private const int _toolGridXElements = 3;
		private const float _toolGridFixedCellWidth = 140;

		private const float _buttonWidth = 110;
		private const float _buttonHeight = 25;

		private GUIContent[] _guiContents;

		private GUIContent _addButton = new GUIContent("Add Shelf", "Add a new shelf folder (containing json files) to load.");
		private GUIContent _removeButton = new GUIContent("Remove Shelf", "Remove active shelf.");
		private GUIContent _applyButton = new GUIContent("Create Tool", "Create and apply the selected tool on the selected gameobjects.");

		private bool _folderFoldout;

		private Vector2 _toolButtonScrollPos = Vector2.zero;

		private GUIStyle _toolGridStyle;
		private GUIStyle _buttonStyle;
		private GUIStyle _popupStyle;

		private bool _initializedUI;

		private string[] _shelfNames;

		private int _selectedToolIndex;


		public static void ShowWindow()
		{
			bool bUtility = false;
			bool bFocus = true;
			string title = "Houdini Engine Tools";

			HEU_ShelfToolsWindow window = EditorWindow.GetWindow<HEU_ShelfToolsWindow>(bUtility, title, bFocus);
			window.autoRepaintOnSceneChange = true;
			window.minSize = new Vector2(_windowWidth, _windowHeight);
		}

		private void InitializeUIElements()
		{
			_toolGridStyle = new GUIStyle(GUI.skin.button);
			//_toolGridStyle.fixedWidth = _toolGridFixedCellWidth;
			_toolGridStyle.imagePosition = ImagePosition.ImageAbove;

			_buttonStyle = new GUIStyle(GUI.skin.button);

			_popupStyle = new GUIStyle(EditorStyles.popup);

			_initializedUI = true;
		}

		public void OnEnable()
		{
			_initializedUI = false;

			// Always reload the tools data when window is reopened
			// since the GUIContents are not kept around when closed.
			LoadShelves();

			Selection.selectionChanged += SelectionChangedCallback;
		}

		public void OnDisable()
		{
			Selection.selectionChanged -= SelectionChangedCallback;
		}

		public void OnGUI()
		{
			if (!_initializedUI)
			{
				// Creating of UI elements must happen in OnGUI
				InitializeUIElements();
			}

			bool bChanged = false;

			Color originalBGColor = GUI.backgroundColor;

			bool bRequiresLoad = !HEU_ShelfTools.AreShelvesLoaded();
			if(!bRequiresLoad)
			{
				// Sanity check that textures are still valid. When scene changes, these get invalidated.
				if (_guiContents != null && _guiContents.Length > 0)
				{
					bRequiresLoad = (_guiContents[0].image == null);
				}
			}
			
			if(bRequiresLoad)
			{
				LoadShelves();
			}

			int numTools = 0;

			using (new EditorGUILayout.VerticalScope())
			{
				using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
				{
					if (HEU_ShelfTools.AreShelvesLoaded())
					{
						int currentShelfIndex = HEU_ShelfTools.GetCurrentShelfIndex();

						HEU_Shelf shelf = null;

						using (new EditorGUILayout.HorizontalScope())
						{
							GUILayout.FlexibleSpace();

							if (GUILayout.Button(_addButton, _buttonStyle, GUILayout.MaxWidth(_buttonWidth), GUILayout.MaxHeight(_buttonHeight)))
							{
								string newShelfPath = UnityEditor.EditorUtility.OpenFolderPanel("Add Shelf Folder", "", "");
								if (!string.IsNullOrEmpty(newShelfPath) && HEU_Platform.DoesDirectoryExist(newShelfPath))
								{
									AddNewShelfWindow(newShelfPath);
									bChanged = true;
								}
							}
						}

						using (new EditorGUILayout.HorizontalScope())
						{
							GUILayout.Label("Active Shelf");

							int newShelfIndex = EditorGUILayout.Popup(currentShelfIndex, _shelfNames, _popupStyle);
							if (currentShelfIndex != newShelfIndex)
							{
								// Change shelf
								currentShelfIndex = newShelfIndex;
								HEU_ShelfTools.SetCurrentShelf(currentShelfIndex);
								SelectShelf(currentShelfIndex);
							}

							shelf = HEU_ShelfTools.GetShelf(currentShelfIndex);
							numTools = shelf._tools.Count;

							using (new EditorGUI.DisabledGroupScope(shelf._defaultShelf))
							{
								if (GUILayout.Button(_removeButton, _buttonStyle, GUILayout.MaxWidth(_buttonWidth)))
								{
									HEU_ShelfTools.RemoveShelf(currentShelfIndex);

									HEU_ShelfTools.SaveShelf();
									HEU_ShelfTools.SetReloadShelves();
									bChanged = true;
								}
							}
						}


						HEU_EditorUI.DrawSeparator();

						if (!bChanged)
						{
							using (EditorGUILayout.ScrollViewScope scroll = new EditorGUILayout.ScrollViewScope(_toolButtonScrollPos))
							{
								if (numTools > 0)
								{
									int numXElements = numTools < _toolGridXElements ? numTools : _toolGridXElements;

									_selectedToolIndex = GUILayout.SelectionGrid(_selectedToolIndex, _guiContents, numXElements, _toolGridStyle);
								}
								else
								{
									EditorGUILayout.LabelField("No tools found!");
								}

								_toolButtonScrollPos = scroll.scrollPosition;
							}
						}
					}
				}
				
				bool bValidSelection = (_selectedToolIndex >= 0 && _selectedToolIndex < numTools);
				using (new EditorGUI.DisabledGroupScope(!bValidSelection))
				{
					if(!bValidSelection)
					{
						_applyButton.text = "Select a Tool!";
					}
					else
					{
						GameObject[] selectedObjects = HEU_EditorUtility.GetSelectedObjects();
						if(selectedObjects.Length == 0)
						{
							_applyButton.text = "Create Tool (no input selected)!";
						}
						else
						{
							_applyButton.text = "Create Tool (selected objects as inputs)!";
						}
					}

					if (GUILayout.Button(_applyButton, _buttonStyle, GUILayout.MaxHeight(_buttonHeight)))
					{
						ProcessUserSelection(_selectedToolIndex);
					}
				}
			}
		}

		private void SelectionChangedCallback()
		{
			Repaint();
		}

		private void LoadShelves()
		{
			HEU_ShelfTools.ClearShelves();
			HEU_ShelfTools.LoadShelves();

			int numShelves = HEU_ShelfTools.GetNumShelves();
			_shelfNames = new string[numShelves];
			for(int i = 0; i < numShelves; ++i)
			{
				_shelfNames[i] = HEU_ShelfTools.GetShelf(i)._shelfName;
			}

			SelectShelf(HEU_ShelfTools.GetCurrentShelfIndex());

			_selectedToolIndex = -1;
		}

		private void SelectShelf(int index)
		{
			int numShelves = HEU_ShelfTools.GetNumShelves();
			if(index >= 0 && index < numShelves)
			{
				HEU_Shelf shelf = HEU_ShelfTools.GetShelf(index);
				if (shelf != null)
				{
					int numTools = shelf._tools.Count;
					_guiContents = new GUIContent[numTools];

					for (int i = 0; i < numTools; ++i)
					{
						_guiContents[i] = new GUIContent();
						_guiContents[i].text = shelf._tools[i]._name;

						if (HEU_HAPIUtility.DoesMappedPathExist(shelf._tools[i]._iconPath))
						{
							string realPath = HEU_PluginStorage.Instance.ConvertEnvKeyedPathToReal(shelf._tools[i]._iconPath);
							_guiContents[i].image = HEU_GeneralUtility.LoadTextureFromFile(realPath);
						}
						
						_guiContents[i].tooltip = shelf._tools[i]._toolTip;
					}
				}
			}
		}

		private void ProcessUserSelection(int selectedIndex)
		{
			HEU_ShelfTools.ExecuteTool(selectedIndex);
		}

		private void AddNewShelfWindow(string newShelfPath)
		{
			HEU_ShowAddShelfWindow window = ScriptableObject.CreateInstance<HEU_ShowAddShelfWindow>() as HEU_ShowAddShelfWindow;
			window.titleContent = new GUIContent("Add Shelf");
			window.position = new Rect(Screen.width / 2, Screen.height / 2, 350, 100);
			window._shelfPath = newShelfPath;
			window.ShowUtility();
		}
	}

	public class HEU_ShowAddShelfWindow : EditorWindow
	{
		public string _shelfName;
		public string _shelfPath;

		public void OnGUI()
		{
			using (new GUILayout.VerticalScope())
			{
				_shelfName = EditorGUILayout.TextField("New Shelf Name", _shelfName);

				using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_shelfName)))
				{
					if (GUILayout.Button("Create"))
					{
						//Debug.Log("Shelf name: " + _shelfName);
						HEU_ShelfTools.AddShelf(_shelfName, _shelfPath);
						HEU_ShelfTools.SaveShelf();
						HEU_ShelfTools.SetReloadShelves();

						this.Close();
					}
				}

				if (GUILayout.Button("Cancel"))
				{
					this.Close();
				}
			}
		}
	}

}   // HoudiniEngineUnity