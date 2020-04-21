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

using System.IO;

namespace HoudiniEngineUnity
{
	/// <summary>
	/// Unity Editor menu functions
	/// </summary>
	public class HEU_EditorMenu : MonoBehaviour
	{
		// SESSIONS ---------------------------------------------------------------------------------------------------

#if (UNITY_EDITOR_64 || UNITY_64)
		/* Commenting out In-Process sessions as its not recommended usage
		// In-Process session is only available in 64-bit Houdini Engine library
		[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/Session/Create/" + HEU_EditorStrings.INPROCESS_SESSION, false, 0)]
		public static void CreateInProcessSession()
		{
			bool bResult = HEU_SessionManager.CreateInProcessSession();
			if (!bResult)
			{
				HEU_EditorUtility.DisplayErrorDialog("Create Session", HEU_SessionManager.GetLastSessionError(), "OK");
			}
		}
		*/
#endif

		[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/Session/Create/" + HEU_EditorStrings.RPC_PIPE_SESSION, false, 0)]
		public static void CreatePipeSession()
		{
			bool bResult = HEU_SessionManager.CreateThriftPipeSession(HEU_PluginSettings.Session_PipeName, HEU_PluginSettings.Session_AutoClose, HEU_PluginSettings.Session_Timeout, true);
			if (!bResult)
			{
				HEU_EditorUtility.DisplayErrorDialog("Create Session", HEU_SessionManager.GetLastSessionError(), "OK");
			}
		}

		[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/Session/Create/" + HEU_EditorStrings.RPC_SOCKET_SESSION, false, 0)]
		public static void CreateSocketSession()
		{
			bool bResult = HEU_SessionManager.CreateThriftSocketSession(HEU_PluginSettings.Session_Localhost, HEU_PluginSettings.Session_Port, HEU_PluginSettings.Session_AutoClose, HEU_PluginSettings.Session_Timeout, true);
			if (!bResult)
			{
				HEU_EditorUtility.DisplayErrorDialog("Create Session", HEU_SessionManager.GetLastSessionError(), "OK");
			}
		}

		[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/Session/Connect To Debugger/" + HEU_EditorStrings.RPC_PIPE_SESSION, false, 0)]
		public static void DebugConnectPipeSession()
		{
			bool bResult = HEU_SessionManager.ConnectThriftPipeSession(HEU_PluginSettings.Session_PipeName, HEU_PluginSettings.Session_AutoClose, HEU_PluginSettings.Session_Timeout);
			if (!bResult)
			{
				HEU_EditorUtility.DisplayErrorDialog("Debug Session", HEU_SessionManager.GetLastSessionError(), "OK");
			}
		}

		[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/Session/Connect To Debugger/" + HEU_EditorStrings.RPC_SOCKET_SESSION, false, 0)]
		public static void DebugConnectSocketSession()
		{
			bool bResult = HEU_SessionManager.ConnectThriftSocketSession(HEU_PluginSettings.Session_Localhost, HEU_PluginSettings.Session_Port, HEU_PluginSettings.Session_AutoClose, HEU_PluginSettings.Session_Port);
			if (!bResult)
			{
				HEU_EditorUtility.DisplayErrorDialog("Debug Session", HEU_SessionManager.GetLastSessionError(), "OK");
			}
		}

		[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/Session/" + HEU_EditorStrings.GET_SESSION_INFO, false, 20)]
		public static void GetSessionInfo()
		{
			HEU_EditorUtility.DisplayDialog("Houdini Engine", HEU_SessionManager.GetSessionInfo(), "OK");
		}

		[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/Session/" + HEU_EditorStrings.CLOSE_DEFAULT_SESSION, false, 40)]
		public static void CloseDefaultSession()
		{
			bool bResult = HEU_SessionManager.CloseDefaultSession();
			if (!bResult)
			{
				HEU_EditorUtility.DisplayErrorDialog("Closing Default Session", HEU_SessionManager.GetLastSessionError(), "OK");
			}
			else
			{
				Debug.Log("Houdini Engine Session closed!");
			}
		}

		[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/Session/" + HEU_EditorStrings.CLOSE_ALL_SESSIONS, false, 40)]
		public static void CloseAllSessions()
		{
			HEU_SessionManager.CloseAllSessions();
			Debug.Log("Houdini Engine Sessions closed!");
		}

		[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/Session/" + HEU_EditorStrings.RECONNECT_TO_SESSION, false, 60)]
		public static void ReconnectToSession()
		{
			bool bResult = HEU_SessionManager.LoadStoredDefaultSession();
			if (!bResult)
			{
				HEU_EditorUtility.DisplayDialog("Reconnecting to Session", HEU_SessionManager.GetLastSessionError(), "OK");
			}
			else
			{
				Debug.Log("Houdini Engine Session reconnected.");
			}
		}

		[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/Session/" + HEU_EditorStrings.RESTART_SESSION, false, 60)]
		public static void ReinitializeSession()
		{
			// Force to find engine path again if not found.
			if (!HEU_Platform.IsPathSet)
			{
				HEU_Platform.SetHoudiniEnginePath();
			}

			bool bResult = HEU_SessionManager.RestartSession();
			if(!bResult)
			{
				HEU_EditorUtility.DisplayDialog("Reinitializing Session", HEU_SessionManager.GetLastSessionError(), "OK");
			}
			else
			{
				Debug.Log("Houdini Engine Session restarted.");
			}
		}


		// INSTALLATION -----------------------------------------------------------------------------------------------

		[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/" + "Installation Info", false, 0)]
		public static void ShowInstallationInfo()
		{
			HEU_EditorUtility.DisplayDialog(HEU_Defines.HEU_INSTALL_INFO, HEU_HAPIUtility.GetHoudiniEngineInstallationInfo(), "OK");
		}

		[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/" + "Plugin Settings", false, 0)]
		public static void ShowSettingsWindow()
		{
			HEU_SettingsWindow.ShowWindow();
		}

		// DEBUG ---------------------------------------------------------------------------------------------------

		/* COMMENTED OUT FOR NOW UNTIL FULL IMPLEMENTATION IS IN
		[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/Debug/" + HEU_EditorStrings.LOAD_SESSION_FROM_HIP, false, 40)]
		public static void LoadSessionFromHIP()
		{
			bool bResult = HEU_SessionManager.LoadSessionFromHIP(true);
			if (!bResult)
			{
				HEU_EditorUtility.DisplayDialog("Loading Session From HIP", HEU_SessionManager.GetLastSessionError(), "OK");
			}
		}
		*/

		[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/Debug/" + HEU_EditorStrings.OPEN_SCENE_IN_HOUDINI, false, 20)]
		public static void OpenSceneHoudini()
		{
			bool bResult = HEU_SessionManager.OpenSessionInHoudini();
			if (!bResult)
			{
				HEU_EditorUtility.DisplayDialog("Opening Session in Houdini", HEU_SessionManager.GetLastSessionError(), "OK");
			}
		}

		[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/Debug/" + HEU_EditorStrings.SAVE_SESSION_TO_HIP, false, 20)]
		public static void SaveSessionToHIP()
		{
			bool bResult = HEU_SessionManager.SaveSessionToHIP(false);
			if (!bResult)
			{
				HEU_EditorUtility.DisplayDialog("Saving Session to HIP", HEU_SessionManager.GetLastSessionError(), "OK");
			}
		}

		[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/Help/" + HEU_EditorStrings.HELP_DOCUMENTATION, false, 20)]
		public static void OpenHelpDocumentation()
		{
			Application.OpenURL(HEU_EditorStrings.HELP_DOCUMENTATION_URL);
		}

		[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/Help/" + HEU_EditorStrings.HELP_FORUM, false, 20)]
		public static void OpenHelpForum()
		{
			Application.OpenURL(HEU_EditorStrings.HELP_FORUM_URL);
		}

		// GENERATE ---------------------------------------------------------------------------------------------------

		[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/Load HDA File", false, 40)]
		public static void LoadHoudiniAssetWindow()
		{
			if (HEU_SessionManager.ValidatePluginSession())
			{
				string[] extensions = { "HDAs", "otl,hda,otllc,hdalc,otlnc,hdanc" };
				string hdaPath = EditorUtility.OpenFilePanelWithFilters("Load Houdini Digital Asset", HEU_PluginSettings.LastLoadHDAPath, extensions);
				LoadHoudiniAssetFromPath(hdaPath);
			}
		}

		[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/Load HDA Expanded", false, 40)]
		public static void LoadHoudiniAssetExpandedWindow()
		{
			if (HEU_SessionManager.ValidatePluginSession())
			{
				string hdaPath = EditorUtility.OpenFolderPanel("Load Houdini Digital Asset (Expanded)", HEU_PluginSettings.LastLoadHDAPath, "");
				LoadHoudiniAssetFromPath(hdaPath);
			}
		}

		[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/Load Geo File", false, 40)]
		public static void LoadGeoFile()
		{
			GameObject newGO = HEU_HAPIUtility.LoadGeoWithNewGeoSync();
			if (newGO != null)
			{
				HEU_EditorUtility.SelectObject(newGO);
			}
		}

		private static void LoadHoudiniAssetFromPath(string hdaPath)
		{
			if (!string.IsNullOrEmpty(hdaPath))
			{
				// Store HDA path for next time
				HEU_PluginSettings.LastLoadHDAPath = Path.GetDirectoryName(hdaPath);

				GameObject go = HEU_HAPIUtility.InstantiateHDA(hdaPath, Vector3.zero, HEU_SessionManager.GetOrCreateDefaultSession(), true);
				if (go != null)
				{
					HEU_EditorUtility.SelectObject(go);
				}
			}
		}

		[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/Houdini Engine Tools", false, 40)]
		public static void ShowHEngineTools()
		{
			HEU_ShelfToolsWindow.ShowWindow();
		}

		[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/New Curve Asset", false, 60)]
		public static void CreateNewCurveAsset()
		{
			GameObject newCurveGO = HEU_HAPIUtility.CreateNewCurveAsset();
			if(newCurveGO != null)
			{
				HEU_Curve.PreferredNextInteractionMode = HEU_Curve.Interaction.ADD;
				HEU_EditorUtility.SelectObject(newCurveGO);
			}
		}

		[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/New Input Asset", false, 60)]
		public static void CreateNewInputAsset()
		{
			GameObject newCurveGO = HEU_HAPIUtility.CreateNewInputAsset();
			if (newCurveGO != null)
			{
				HEU_EditorUtility.SelectObject(newCurveGO);
			}
		}

		// BATCH ACTIONS ----------------------------------------------------------------------------------------------

		[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/Cook/Selected Houdini Assets", false, 80)]
		public static void CookSelected()
		{
			HEU_EditorUtility.CookSelected();
		}

		[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/Cook/All Houdini Assets", false, 80)]
		public static void CookAll()
		{
			HEU_EditorUtility.CookAll();
		}

		[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/Rebuild/Selected Houdini Assets", false, 80)]
		public static void RebuildSelected()
		{
			HEU_EditorUtility.RebuildSelected();
		}

		[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/Rebuild/All Houdini Assets", false, 80)]
		public static void RebuildAll()
		{
			HEU_EditorUtility.RebuildAll();
		}

		[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/Bake And Replace/Selected Houdini Assets", false, 80)]
		public static void BakeAndReplaceSelected()
		{
			HEU_EditorUtility.BakeAndReplaceSelectedInScene();
		}

		[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/Bake And Replace/All Houdini Assets", false, 80)]
		public static void BakeAndReplaceAll()
		{
			HEU_EditorUtility.BakeAndReplaceAllInScene();
		}

		[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/Export Output Geo/Selected Houdini Assets", false, 80)]
		public static void ExportSelecedAssetsGeo()
		{
			HEU_EditorUtility.ExportSelectedAssetsToGeoFiles();
		}

		[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/Export Output Geo/All Houdini Assets", false, 80)]
		public static void ExportAllAssetsGeo()
		{
			HEU_EditorUtility.ExportAllAssetsToGeoFiles();
		}

		// UTILITY ----------------------------------------------------------------------------------------------

		[MenuItem(HEU_Defines.HEU_PRODUCT_NAME + "/Utility/Query Mesh Topology", false, 100)]
		public static void QueryMeshTopology()
		{
			HEU_EditorUtility.QuerySelectedMeshTopology();
		}
	}


}   // HoudiniEngineUnity