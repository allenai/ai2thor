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
	[CustomEditor(typeof(HEU_GeoSync))]
	public class HEU_GeoSyncUI : Editor
	{
		private HEU_GeoSync _geoSync;

		private GUIContent _fileLabelContent = new GUIContent("File Path", "File to load.");

		private GUIContent _syncContent = new GUIContent("Sync", "Load the file.");

		private GUIContent _stopContent = new GUIContent("Stop", "Stop the loading.");

		private GUIContent _statusIdleContent = new GUIContent("Idle");

		private GUIContent _statusSyncContent = new GUIContent("Syncing");

		private GUIContent _unloadContent = new GUIContent("Unload", "Delete the file node and clean up all generated content.");



		private void OnEnable()
		{
			AcquireTarget();
		}

		private void AcquireTarget()
		{
			_geoSync = target as HEU_GeoSync;
		}

		public override void OnInspectorGUI()
		{
			if (_geoSync == null)
			{
				AcquireTarget();
			}

			using (new EditorGUILayout.VerticalScope())
			{
				bool bSyncing = _geoSync.IsSyncing;

				EditorGUILayout.LabelField(_fileLabelContent);

				using (new EditorGUILayout.HorizontalScope())
				{
					_geoSync._filePath = EditorGUILayout.DelayedTextField(_geoSync._filePath);

					// TODO: add field for output cache directory

					GUIStyle buttonStyle = HEU_EditorUI.GetNewButtonStyle_MarginPadding(0, 0);
					if (GUILayout.Button("...", buttonStyle, GUILayout.Width(30), GUILayout.Height(18)))
					{
						string filePattern = "*.bgeo;*.bgeo.sc";
						_geoSync._filePath = EditorUtility.OpenFilePanel("Select File", _geoSync._filePath, filePattern);
					}
				}

				HEU_EditorUI.DrawSeparator();

				if (bSyncing)
				{
					EditorGUILayout.LabelField(_statusSyncContent);

					if (GUILayout.Button(_stopContent))
					{
						_geoSync.StopSync();
					}
				}
				else
				{
					EditorGUILayout.LabelField(_statusIdleContent);

					using (new EditorGUILayout.HorizontalScope())
					{
						bool bLoaded = _geoSync.IsLoaded();

						if (GUILayout.Button(_syncContent))
						{
							_geoSync.StartSync();
						}

						//GUILayout.FlexibleSpace();

						using (new EditorGUI.DisabledScope(!bLoaded))
						{
							if (GUILayout.Button(_unloadContent))
							{
								_geoSync.Unload();
							}
						}
					}
				}

				if (!string.IsNullOrEmpty(_geoSync._logStr))
				{
					EditorGUILayout.LabelField("Log: " + _geoSync._logStr);
				}
			}
		}

	}

}   // HoudiniEngineUnity