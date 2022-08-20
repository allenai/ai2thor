/************************************************************************************
Filename    :   ONSPReflectionCustomGUI.cs
Content     :   GUI for Oculus Spatializer mixer effect
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
using UnityEditor;
using UnityEngine;
using System.Runtime.InteropServices;

public class OculusSpatializerReflectionCustomGUI : IAudioEffectPluginGUI
{
    public override string Name
    {
        get { return "OculusSpatializerReflection"; }
    }

    public override string Description
    {
        get { return "Reflection parameters for Oculus Spatializer"; }
    }

    public override string Vendor
    {
        get { return "Oculus"; }
    }
	
    public override bool OnGUI(IAudioEffectPlugin plugin)
    {
		float fval = 0.0f;
        bool bval = false;

        Separator();
        Label("Voice limit (1 - 1024)");
        ONSPSettings.Instance.voiceLimit = EditorGUILayout.IntField(" ", ONSPSettings.Instance.voiceLimit);
        if (GUI.changed)
        {
            GUI.changed = false;
            EditorUtility.SetDirty(ONSPSettings.Instance);
        }

		Separator();
		Label ("GLOBAL SCALE (0.00001 - 10000.0)");
		plugin.GetFloatParameter("GScale", out fval);
		plugin.SetFloatParameter("GScale", EditorGUILayout.FloatField(" ", Mathf.Clamp (fval, 0.00001f, 10000.0f)));

		Separator();

		Label ("REFLECTION ENGINE");
		Label("");
		// Treat these floats as bools in the inspector
		plugin.GetFloatParameter("E.Rflt On", out fval);
		bval = (fval == 0.0f) ? false : true;
		bval = EditorGUILayout.Toggle("Enable Early Reflections", bval);
		plugin.SetFloatParameter("E.Rflt On", (bval == false) ? 0.0f : 1.0f);

		plugin.GetFloatParameter("E.Rflt Rev On", out fval);
		bval = (fval == 0.0f) ? false : true;
		bval = EditorGUILayout.Toggle("Enable Reverberation", bval);
		plugin.SetFloatParameter("E.Rflt Rev On", (bval == false) ? 0.0f : 1.0f);

		Separator();
		Label("ROOM DIMENSIONS (meters)");
		Label("");
		plugin.GetFloatParameter("Room X", out fval);
		plugin.SetFloatParameter("Room X", EditorGUILayout.Slider("Width", fval, 1.0f, 200.0f));
		plugin.GetFloatParameter("Room Y", out fval);
		plugin.SetFloatParameter("Room Y", EditorGUILayout.Slider("Height", fval, 1.0f, 200.0f));
		plugin.GetFloatParameter("Room Z", out fval);
		plugin.SetFloatParameter("Room Z", EditorGUILayout.Slider("Length", fval, 1.0f, 200.0f));

		Separator();
		Label("WALL REFLECTION COEFFICIENTS (0.0 - 0.97)");
		Label("");

		plugin.GetFloatParameter("Left", out fval);
		plugin.SetFloatParameter("Left", EditorGUILayout.Slider("Left", fval, 0.0f, 0.97f));
		plugin.GetFloatParameter("Right", out fval);
		plugin.SetFloatParameter("Right", EditorGUILayout.Slider("Right", fval, 0.0f, 0.97f));
		plugin.GetFloatParameter("Up", out fval);
		plugin.SetFloatParameter("Up", EditorGUILayout.Slider("Up", fval, 0.0f, 0.97f));
		plugin.GetFloatParameter("Down", out fval);
		plugin.SetFloatParameter("Down", EditorGUILayout.Slider("Down", fval, 0.0f, 0.97f));
		plugin.GetFloatParameter("Behind", out fval);
		plugin.SetFloatParameter("Behind", EditorGUILayout.Slider("Back", fval, 0.0f, 0.97f));
		plugin.GetFloatParameter("Front", out fval);
		plugin.SetFloatParameter("Front", EditorGUILayout.Slider("Front", fval, 0.0f, 0.97f));

        Separator();
        Label("SHARED REVERB ATTENUATION RANGE (1.0 - 10000.0 meters)");
        Label("");
        plugin.GetFloatParameter("Shared Rev Min", out fval);
        plugin.SetFloatParameter("Shared Rev Min", EditorGUILayout.Slider("Minimum", fval, 1.0f, 10000.0f));
        plugin.GetFloatParameter("Shared Rev Max", out fval);
        plugin.SetFloatParameter("Shared Rev Max", EditorGUILayout.Slider("Maximum", fval, 1.0f, 10000.0f));

        Separator();
        Label("SHARED REVERB WET MIX (-60.0 - 20.0 dB)");
        Label("");
        plugin.GetFloatParameter("Shared Rev Wet", out fval);
        plugin.SetFloatParameter("Shared Rev Wet", EditorGUILayout.Slider(" ", fval, -60.0f, 20.0f));

		Separator();
        Label("PROPAGATION QUALITY LEVEL (0.0 - 200.0%)");
        Label("");
        plugin.GetFloatParameter("Prop Quality", out fval);
        plugin.SetFloatParameter("Prop Quality", EditorGUILayout.Slider(" ", fval, 0.0f, 200.0f));
		
		Separator();

        // We will override the controls with our own, so return false
        return false;
    }

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
