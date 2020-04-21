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

namespace HoudiniEngineUnity
{
	/// <summary>
	/// Custom UI class for locking out the HEU_HoudiniAsset UI.
	/// By design, HEU_HoudiniAssetUI draws the custom UI for the entire HDA.
	/// HEU_HoudiniAsset is the meta data container but Unity automatically
	/// draws all its serialized properties as default behaviour.
	/// In reduce user modification of the meta data, this classes provides
	/// an override UI to lock it out. Users can unlock manually to still
	/// debug their asset if needed.
	/// </summary>
	[CustomEditor(typeof(HEU_HoudiniAsset))]
	public class HEU_HoudiniAssetDebugUI : Editor
	{

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			SerializedProperty uiLockedProperty = HEU_EditorUtility.GetSerializedProperty(serializedObject, "_uiLocked");
			if (uiLockedProperty != null)
			{
				EditorGUI.BeginChangeCheck();

				HEU_EditorUI.DrawSeparator();

				GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
				labelStyle.fontStyle = FontStyle.Bold;
				labelStyle.wordWrap = true;
				labelStyle.normal.textColor = HEU_EditorUI.IsEditorDarkSkin() ? Color.yellow : Color.blue;

				string lockMsg = "This contains the meta data for the HDA."
								+ "\nIt is locked out because it is not recommended to edit it.";

				EditorGUILayout.LabelField(lockMsg, labelStyle);

				HEU_EditorUI.DrawSeparator();

				uiLockedProperty.boolValue = EditorGUILayout.Toggle("UI Locked", uiLockedProperty.boolValue);

				if (EditorGUI.EndChangeCheck())
				{
					serializedObject.ApplyModifiedProperties();
				}

				HEU_EditorUI.DrawHorizontalLine();

				using (new EditorGUI.DisabledScope(uiLockedProperty.boolValue))
				{
					// Only draw the default if user has unlocked asset UI.
					DrawDefaultInspector();
				}
			}
		}

	}

}   // HoudiniEngineUnity