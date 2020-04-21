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
	/// This is the UI class for HEU_PDGAssetLink, and allows to manage its state.
	/// This "links" to an instanced HDA in the scene, and manage the TOP networks within.
	/// Shows TOP networks and TOP nodes within the HDA, show the PDG graph status, work item status, 
	/// allows to cook and dirty TOP networks, and nodes, and load / unload generated results.
	/// </summary>
	[CustomEditor(typeof(HEU_PDGAssetLink))]
	public class HEU_PDGAssetLinkUI : Editor
	{
		private void OnEnable()
		{
			// The HEU_PDGAssetLink contains the state and cached data of the linked asset
			_assetLink = target as HEU_PDGAssetLink;
		}

		public override void OnInspectorGUI()
		{
			if (_assetLink == null)
			{
				DrawNoAssetLink();
				return;
			}

			// Always hook into asset UI callback. This could have got reset on code refresh.
			_assetLink._repaintUIDelegate = RefreshUI;

			serializedObject.Update();

			SetupUI();

			DrawPDGStatus();

			DrawAssetLink();
		}

		/// <summary>
		/// Display message when no asset is linked.
		/// </summary>
		private void DrawNoAssetLink()
		{
			HEU_EditorUI.DrawSeparator();

			GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
			labelStyle.fontStyle = FontStyle.Bold;
			labelStyle.normal.textColor = HEU_EditorUI.IsEditorDarkSkin() ? Color.yellow : Color.red;
			EditorGUILayout.LabelField("Houdini Engine Asset - no HEU_PDGAssetLink found!", labelStyle);

			HEU_EditorUI.DrawSeparator();
		}

		/// <summary>
		/// Main function to display linked asset's info, and functions.
		/// </summary>
		private void DrawAssetLink()
		{
			HEU_PDGAssetLink.LinkState validState = _assetLink.AssetLinkState;

			using (new EditorGUILayout.VerticalScope(_backgroundStyle))
			{
				EditorGUILayout.Space();

				// Linked asset
				SerializedProperty assetGOProp = HEU_EditorUtility.GetSerializedProperty(serializedObject, "_assetGO");
				if (assetGOProp != null)
				{
					EditorGUILayout.PropertyField(assetGOProp, _assetGOLabel, false);
				}

				EditorGUILayout.Space();

				using (new EditorGUILayout.HorizontalScope())
				{
					// Refresh button re-poplates the UI data from linked asset
					if (GUILayout.Button(_refreshContent, GUILayout.MaxHeight(_largButtonHeight)))
					{
						_assetLink.Refresh();
					}

					// Reset button resets and recreates the HEU_PDGAssetLink
					if (GUILayout.Button(_resetContent, GUILayout.MaxHeight(_largButtonHeight)))
					{
						_assetLink.Reset();
					}
				}

				// Autocook allows to automatically cook the TOP network when input assets are cooked
				_assetLink._autoCook = EditorGUILayout.Toggle(_autocookContent, _assetLink._autoCook);

				// Whether to use HEngine meta data to filter TOP networks and nodes
				_assetLink._useHEngineData = EditorGUILayout.Toggle(_useHEngineDataContent, _assetLink._useHEngineData);

				EditorGUILayout.Space();

				// Asset status
				using (new EditorGUILayout.VerticalScope(HEU_EditorUI.GetSectionStyle()))
				{
					EditorGUILayout.LabelField("Asset is " + validState);

					if (validState == HEU_PDGAssetLink.LinkState.ERROR_NOT_LINKED)
					{
						EditorGUILayout.LabelField("Failed to link with HDA. Unable to proceed. Try rebuilding asset.");
					}
					else if (validState == HEU_PDGAssetLink.LinkState.LINKED)
					{
						EditorGUILayout.Space();

						EditorGUILayout.LabelField(_assetStatusLabel);

						DrawWorkItemTally(_assetLink._workItemTally);

						EditorGUILayout.Space();
					}
				}
			}

			if (validState == HEU_PDGAssetLink.LinkState.INACTIVE)
			{
				_assetLink.Refresh();
			}
			else if (validState == HEU_PDGAssetLink.LinkState.LINKED)
			{
				using (new EditorGUILayout.VerticalScope(_backgroundStyle))
				{
					EditorGUILayout.Space();

					DrawSelectedTOPNetwork();

					EditorGUILayout.Space();

					DrawSelectedTOPNode();
				}
			}

			// Display cook event messages
			string eventMsgs = "<color=#c0c0c0ff>Cook event messages and errors will be displayed here...</color>";
			HEU_PDGSession pdgSession = HEU_PDGSession.GetPDGSession();
			if (pdgSession != null)
			{
				string actualMsgs = pdgSession.GetEventMessages();
				if (!string.IsNullOrEmpty(actualMsgs))
				{
					eventMsgs = string.Format("{0}", actualMsgs);
				}
			}

			using (new EditorGUILayout.VerticalScope(_backgroundStyle))
			{
				EditorGUILayout.Space();

				_eventMessageScrollPos = EditorGUILayout.BeginScrollView(_eventMessageScrollPos, false, false);
				Vector2 textSize = _eventMessageStyle.CalcSize(new GUIContent(eventMsgs));
				EditorGUILayout.PrefixLabel(_eventMessageContent);
				EditorGUILayout.SelectableLabel(eventMsgs, _eventMessageStyle, GUILayout.ExpandHeight(true), 
					GUILayout.ExpandWidth(true), GUILayout.MinWidth(textSize.x), GUILayout.MinHeight(textSize.y));
				EditorGUILayout.EndScrollView();
			}
		}

		/// <summary>
		/// Displays a dropdown list of TOP network names, and shows the selected TOP network info
		/// </summary>
		private void DrawSelectedTOPNetwork()
		{
			HEU_EditorUI.DrawHeadingLabel("Internal TOP Networks");

			int numTopNodes = _assetLink._topNetworkNames.Length;
			if (numTopNodes > 0)
			{
				using (new EditorGUILayout.HorizontalScope())
				{
					EditorGUILayout.PrefixLabel(_topNetworkChooseLabel);

					int numTOPs = _assetLink._topNetworkNames.Length;

					int selectedIndex = Mathf.Clamp(_assetLink.SelectedTOPNetwork, 0, numTopNodes - 1);
					int newSelectedIndex = EditorGUILayout.Popup(selectedIndex, _assetLink._topNetworkNames);
					if (newSelectedIndex != selectedIndex)
					{
						_assetLink.SelectTOPNetwork(newSelectedIndex);
					}
				}

				EditorGUILayout.Space();

				using (new EditorGUILayout.HorizontalScope())
				{
					if (GUILayout.Button(_buttonDirtyAllContent, GUILayout.MaxHeight(_largButtonHeight)))
					{
						_assetLink.DirtyAll();
					}

					if (GUILayout.Button(_buttonCookAllContent, GUILayout.MaxHeight(_largButtonHeight)))
					{
						_assetLink.CookOutput();
					}
				}

				EditorGUILayout.Space();

				using (new EditorGUILayout.HorizontalScope())
				{
					if (GUILayout.Button(_buttonPauseCookContent))
					{
						_assetLink.PauseCook();
					}

					if (GUILayout.Button(_buttonCancelCookContent))
					{
						_assetLink.CancelCook();
					}
				}
			}
			else
			{
				EditorGUILayout.PrefixLabel(_topNetworkNoneLabel);
			}
		}

		/// <summary>
		/// Displays a dropdown list of TOP nodes, and shows the selected TOP node info
		/// </summary>
		private void DrawSelectedTOPNode()
		{
			HEU_TOPNetworkData topNetworkData = _assetLink.GetSelectedTOPNetwork();
			if (topNetworkData == null)
			{
				return;
			}

			using(new EditorGUILayout.VerticalScope(_backgroundStyle))
			{
				int numTopNodes = topNetworkData._topNodeNames.Length;
				if (numTopNodes > 0)
				{
					using (new EditorGUILayout.HorizontalScope())
					{
						EditorGUILayout.PrefixLabel(_topNodeChooseLabel);

						int selectedIndex = Mathf.Clamp(topNetworkData._selectedTOPIndex, 0, numTopNodes);
						int newSelectedIndex = EditorGUILayout.Popup(selectedIndex, topNetworkData._topNodeNames);
						if (newSelectedIndex != selectedIndex)
						{
							_assetLink.SelectTOPNode(topNetworkData, newSelectedIndex);
						}
					}
				}
				else
				{
					EditorGUILayout.PrefixLabel(_topNodeNoneLabel);
				}

				EditorGUILayout.Space();

				HEU_TOPNodeData topNode = _assetLink.GetSelectedTOPNode();
				if (topNode != null)
				{
					topNode._tags._autoload = EditorGUILayout.Toggle(_autoloadContent, topNode._tags._autoload);

					bool showResults = topNode._showResults;
					showResults = EditorGUILayout.Toggle(_showHideResultsContent, showResults);
					if (showResults != topNode._showResults)
					{
						topNode._showResults = showResults;
						_assetLink.UpdateTOPNodeResultsVisibility(topNode);
					}

					EditorGUILayout.Space();

					using (new EditorGUILayout.HorizontalScope())
					{
						if (GUILayout.Button(_buttonDirtyContent))
						{
							_assetLink.DirtyTOPNode(topNode);
						}

						if (GUILayout.Button(_buttonCookContent))
						{
							_assetLink.CookTOPNode(topNode);
						}
					}

					EditorGUILayout.Space();

					using (new EditorGUILayout.VerticalScope(HEU_EditorUI.GetSectionStyle()))
					{
						EditorGUILayout.LabelField("TOP Node State: " + _assetLink.GetTOPNodeStatus(topNode));

						EditorGUILayout.Space();

						EditorGUILayout.LabelField(_topNodeStatusLabel);
						DrawWorkItemTally(topNode._workItemTally);
					}
				}
			}
		}

		/// <summary>
		/// Displays global PDG status
		/// </summary>
		private void DrawPDGStatus()
		{
			string pdgState = "PDG is NOT READY";
			Color stateColor = Color.red;

			HEU_PDGSession pdgSession = HEU_PDGSession.GetPDGSession();
			if (pdgSession != null)
			{
				if (pdgSession._pdgState == HAPI_PDG_State.HAPI_PDG_STATE_COOKING)
				{
					pdgState = "PDG is COOKING";
					stateColor = Color.yellow;
				}
				else if (pdgSession._pdgState == HAPI_PDG_State.HAPI_PDG_STATE_READY)
				{
					pdgState = "PDG is READY";
					stateColor = Color.green;
				}
			}

			EditorGUILayout.Space();

			_boxStyleStatus.normal.textColor = stateColor;
			GUILayout.Box(pdgState, _boxStyleStatus);
		}

		/// <summary>
		/// Displays the given work item tally
		/// </summary>
		/// <param name="tally"></param>
		private void DrawWorkItemTally(HEU_WorkItemTally tally)
		{
			float totalWidth = EditorGUIUtility.currentViewWidth;
			float cellWidth = totalWidth / 5f;

			float titleCellHeight = 26;
			float cellHeight = 24;

			using (new GUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();

				//_boxStyleTitle.normal.textColor = Color.black;
				//DrawGridBoxTitle("TOTAL", cellWidth, titleCellHeight);

				_boxStyleTitle.normal.textColor = (tally._waitingWorkItems > 0) ? Color.cyan : Color.black;
				DrawGridBoxTitle("WAITING", cellWidth, titleCellHeight);

				//_boxStyleTitle.normal.textColor = (tally._scheduledWorkItems > 0) ? Color.yellow : Color.black;
				//DrawGridBoxTitle("SCHEDULED", cellWidth, titleCellHeight);

				_boxStyleTitle.normal.textColor = ((tally._scheduledWorkItems + tally._cookingWorkItems) > 0) ? Color.yellow : Color.black;
				DrawGridBoxTitle("COOKING", cellWidth, titleCellHeight);

				_boxStyleTitle.normal.textColor = (tally._cookedWorkItems > 0) ? _cookedColor : Color.black;
				DrawGridBoxTitle("COOKED", cellWidth, titleCellHeight);
				
				_boxStyleTitle.normal.textColor = (tally._erroredWorkItems > 0) ? Color.red : Color.black;
				DrawGridBoxTitle("FAILED", cellWidth, titleCellHeight);

				GUILayout.FlexibleSpace();
			}

			using (new GUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();

				//DrawGridBoxValue(string.Format("{0}", tally._totalWorkItems), cellWidth, cellHeight);
				DrawGridBoxValue(string.Format("{0}", tally._waitingWorkItems), cellWidth, cellHeight);
				//DrawGridBoxValue(string.Format("{0}", tally._scheduledWorkItems), cellWidth, cellHeight);
				DrawGridBoxValue(string.Format("{0}", (tally._scheduledWorkItems + tally._cookingWorkItems)), cellWidth, cellHeight);
				DrawGridBoxValue(string.Format("{0}", tally._cookedWorkItems), cellWidth, cellHeight);
				DrawGridBoxValue(string.Format("{0}", tally._erroredWorkItems), cellWidth, cellHeight);

				GUILayout.FlexibleSpace();
			}
		}

		private void DrawGridBoxTitle(string text, float width, float height)
		{
			GUILayout.Box(text, _boxStyleTitle, GUILayout.Width(width), GUILayout.Height(height));
		}

		private void DrawGridBoxValue(string text, float width, float height)
		{
			GUILayout.Box(text, _boxStyleValue, GUILayout.Width(width), GUILayout.Height(height));
		}

        private void SetupUI()
		{
			_cookedColor = new Color(0.1f, 0.9f, 0.0f, 1f);

			_assetGOLabel = new GUIContent("Linked Asset", "The HDA containing TOP networks to link with.");
			_assetStatusLabel = new GUIContent("Asset Work Items Status:");

			_resetContent = new GUIContent("Reset", "Reset the state and generated items. Updates from linked HDA.");
			_refreshContent = new GUIContent("Refresh", "Refresh the state and UI.");
			_autocookContent = new GUIContent("Autocook", "Automatically cook the output node when the linked asset is cooked.");
			_useHEngineDataContent = new GUIContent("Use HEngine Data", "Whether to use henginedata parm values for displaying and loading node resuls.");

			_topNetworkChooseLabel = new GUIContent("TOP Network");
			_topNetworkNoneLabel = new GUIContent("TOP Network: None");

			_topNodeChooseLabel = new GUIContent("TOP Node");
			_topNodeNoneLabel = new GUIContent("TOP Node: None");
			_topNodeStatusLabel = new GUIContent("TOP Node Work Items Status:");

			_buttonDirtyContent = new GUIContent("Dirty Node", "Remove current TOP node's work items.");
			_buttonCookContent = new GUIContent("Cook Node", "Generates and cooks current TOP node's work items.");

			_autoloadContent = new GUIContent("Autoload Results", "Automatically load into Unity the generated geometry from work item results.");
			_showHideResultsContent = new GUIContent("Show Results", "Show or Hide Results.");

			_buttonDirtyAllContent = new GUIContent("Dirty All", "Removes all work items.");
			_buttonCookAllContent = new GUIContent("Cook Output", "Generates and cooks all work items.");

			_buttonCancelCookContent = new GUIContent("Cancel Cook", "Cancel PDG cook.");
			_buttonPauseCookContent = new GUIContent("Pause Cook", "Pause PDG cook.");

			_eventMessageContent = new GUIContent("PDG Event Messages", "Messages from events generated during cooking the PDG graph.");

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

			_boxStyleTitle = new GUIStyle(GUI.skin.box);
			float c = 0.35f;
			_boxStyleTitle.normal.background = HEU_GeneralUtility.MakeTexture(1, 1, new Color(c, c, c, 1f));
			_boxStyleTitle.normal.textColor = Color.black;
			_boxStyleTitle.fontStyle = FontStyle.Bold;
			_boxStyleTitle.alignment = TextAnchor.MiddleCenter;
			_boxStyleTitle.fontSize = 10;

			_boxStyleValue = new GUIStyle(GUI.skin.box);
			c = 0.7f;
			_boxStyleValue.normal.background = HEU_GeneralUtility.MakeTexture(1, 1, new Color(c, c, c, 1f));
			_boxStyleValue.normal.textColor = Color.black;
			_boxStyleValue.fontStyle = FontStyle.Bold;
			_boxStyleValue.fontSize = 14;

			_boxStyleStatus = new GUIStyle(GUI.skin.box);
			c = 0.3f;
			_boxStyleStatus.normal.background = HEU_GeneralUtility.MakeTexture(1, 1, new Color(c, c, c, 1f));
			_boxStyleStatus.normal.textColor = Color.black;
			_boxStyleStatus.fontStyle = FontStyle.Bold;
			_boxStyleStatus.alignment = TextAnchor.MiddleCenter;
			_boxStyleStatus.fontSize = 14;
			_boxStyleStatus.stretchWidth = true;

			_eventMessageStyle = new GUIStyle(EditorStyles.textArea);
			_eventMessageStyle.richText = true;
			_eventMessageStyle.normal.background = HEU_GeneralUtility.MakeTexture(1, 1, new Color(0, 0, 0, 1f));
		}

		public void RefreshUI()
		{
			if (_assetLink != null)
			{
				_assetLink.UpdateWorkItemTally();
			}

			Repaint();
		}

		//	MENU ----------------------------------------------------------------------------------------------------
		/// <summary>
		/// Menu entry to create the PDG Asset Link object with link to selected HDA.
		/// </summary>
#if UNITY_EDITOR
		[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/PDG/Create PDG Asset Link", false, 100)]
		public static void CreatePDGAssetLink()
		{
			GameObject selectedGO = Selection.activeGameObject;
			if (selectedGO != null)
			{
				HEU_HoudiniAssetRoot assetRoot = selectedGO.GetComponent<HEU_HoudiniAssetRoot>();
				if (assetRoot != null)
				{
					if (assetRoot._houdiniAsset != null)
					{
						string name = string.Format("{0}_PDGLink", assetRoot._houdiniAsset.AssetName);

						GameObject go = new GameObject(name);
						HEU_PDGAssetLink assetLink = go.AddComponent<HEU_PDGAssetLink>();
						assetLink.Setup(assetRoot._houdiniAsset);

						Selection.activeGameObject = go;
					}
					else
					{
						Debug.LogError("Selected gameobject is not an instantiated HDA. Failed to create PDG Asset Link.");
					}
				}
				else
				{
					Debug.LogError("Selected gameobject is not an instantiated HDA. Failed to create PDG Asset Link.");
				}
			}
			else
			{
				//Debug.LogError("Nothing selected. Select an instantiated HDA first.");
				HEU_EditorUtility.DisplayErrorDialog("PDG Asset Link", "No HDA selected. You must select an instantiated HDA first.", "OK");
			}
		}
#endif

		//	DATA ------------------------------------------------------------------------------------------------------

		public HEU_PDGAssetLink _assetLink;

		private GUIStyle _backgroundStyle;

		private GUIContent _assetGOLabel;
		private GUIContent _assetStatusLabel;

		private GUIContent _resetContent;
		private GUIContent _refreshContent;
		private GUIContent _autocookContent;
		private GUIContent _useHEngineDataContent;

		private GUIContent _topNetworkChooseLabel;
		private GUIContent _topNetworkNoneLabel;

		private GUIContent _topNodeChooseLabel;
		private GUIContent _topNodeNoneLabel;
		private GUIContent _topNodeStatusLabel;

		private GUIContent _buttonDirtyContent;
		private GUIContent _buttonCookContent;

		private GUIContent _autoloadContent;
		private GUIContent _showHideResultsContent;

		private GUIContent _buttonDirtyAllContent;
		private GUIContent _buttonCookAllContent;
		private GUIContent _buttonCancelCookContent;
		private GUIContent _buttonPauseCookContent;

		private GUIStyle _boxStyleTitle;
		private GUIStyle _boxStyleValue;
		private GUIStyle _boxStyleStatus;

		private GUIContent _eventMessageContent;
		private GUIStyle _eventMessageStyle;
		private Vector2 _eventMessageScrollPos = new Vector2();

		private Texture2D _boxTitleTexture;

		private Color _cookedColor;

		private float _largButtonHeight = 26;
	}

}   // HoudiniEngineUnity