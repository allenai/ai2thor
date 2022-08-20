/************************************************************************************
Filename    :   ONSPAudioSourceEditor.cs
Content     :   This script adds editor functionality to OculusSpatializerUserParams script.
Copyright   :   Copyright (c) Facebook Technologies, LLC and its affiliates. All rights reserved.

Licensed under the Oculus SDK Version 3.5 (the "License"); 
you may not use the Oculus SDK except in compliance with the License, 
which is provided at the time of installation or download, or which 
otherwise accompanies this software in either electronic or hard copy form.

You may obtain a copy of the License at

https://developer.oculus.com/licenses/sdk-3.5/

Unless required by applicable law or agreed to in writing, the Oculus SDK 
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
************************************************************************************/
#define CUSTOM_LAYOUT

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(ONSPAudioSource))]

public class OculusSpatializerUserParamsEditor : Editor
{
	// target component
	private ONSPAudioSource m_Component;

	// OnEnable
	void OnEnable()
	{
		m_Component = (ONSPAudioSource)target;
	}
	
	// OnInspectorGUI
	public override void OnInspectorGUI()
	{
		GUI.color = Color.white;
		Undo.RecordObject(m_Component, "OculusSpatializerUserParams");
		
		{
			#if CUSTOM_LAYOUT
			m_Component.EnableSpatialization = EditorGUILayout.Toggle("Spatialization Enabled", m_Component.EnableSpatialization);
			m_Component.EnableRfl  = EditorGUILayout.Toggle("Reflections Enabled", m_Component.EnableRfl);
			m_Component.Gain  = EditorGUILayout.FloatField("Gain", m_Component.Gain);

			Separator();

			Label ("OCULUS ATTENUATION");
			m_Component.UseInvSqr = EditorGUILayout.Toggle("Enabled", m_Component.UseInvSqr);
			Label ("");
			Label("RANGE (0.0 - 1000000.0 meters)");
			m_Component.Near  = EditorGUILayout.FloatField("Minimum", m_Component.Near);
			m_Component.Far   = EditorGUILayout.FloatField("Maximum", m_Component.Far);

            Label("");
            Label("VOLUMETRIC RADIUS (0.0 - 1000.0 meters)");
            m_Component.VolumetricRadius = EditorGUILayout.FloatField("Radius", m_Component.VolumetricRadius);

			Separator();

			Label("REVERB SEND LEVEL (-60.0 - 20.0 decibels)");
			m_Component.ReverbSend  = EditorGUILayout.FloatField(" ", m_Component.ReverbSend);

			Separator();

			#else			 
			DrawDefaultInspector ();
			#endif
		}
		
		if (GUI.changed)
		{
			EditorUtility.SetDirty(m_Component);
		}
	}	
	
	// Utilities, move out of here (or copy over to other editor script)
	
	// Separator
	void Separator()
	{
		GUI.color = new Color(1, 1, 1, 0.25f);
		GUILayout.Box("", "HorizontalSlider", GUILayout.Height(16));
		GUI.color = Color.white;
	}
	
	// Label
	void Label(string label)
	{
		EditorGUILayout.LabelField (label);
	}
	
}

