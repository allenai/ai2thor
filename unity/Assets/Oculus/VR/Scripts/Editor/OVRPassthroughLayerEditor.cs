/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using UnityEditor;
using UnityEngine;

using ColorMapEditorType = OVRPassthroughLayer.ColorMapEditorType;

[CustomEditor(typeof(OVRPassthroughLayer))]
public class OVRPassthroughLayerEditor : Editor {
	private readonly static string[] _selectableColorMapNames = {
		"None",
		"Color Adjustment",
		"Grayscale",
		"Grayscale To Color"
	};
	private readonly static string[] _colorMapNames = {
		"None",
		"Color Adjustment",
		"Grayscale",
		"Grayscale to color",
		"Custom"
	};
	private ColorMapEditorType[] _colorMapTypes = {
		ColorMapEditorType.None,
		ColorMapEditorType.ColorAdjustment,
		ColorMapEditorType.Grayscale,
		ColorMapEditorType.GrayscaleToColor,
		ColorMapEditorType.Custom
	};

	public override void OnInspectorGUI()
	{
		OVRPassthroughLayer layer = (OVRPassthroughLayer)target;

		layer.projectionSurfaceType = (OVRPassthroughLayer.ProjectionSurfaceType)EditorGUILayout.EnumPopup(
			new GUIContent("Projection Surface", "The type of projection surface for this Passthrough layer"),
			layer.projectionSurfaceType);

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Compositing", EditorStyles.boldLabel);
		layer.overlayType = (OVROverlay.OverlayType)EditorGUILayout.EnumPopup(new GUIContent("Placement", "Whether this overlay should layer behind the scene or in front of it"), layer.overlayType);
		layer.compositionDepth = EditorGUILayout.IntField(new GUIContent("Composition Depth", "Depth value used to sort layers in the scene, smaller value appears in front"), layer.compositionDepth);

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Style", EditorStyles.boldLabel);

		layer.textureOpacity = EditorGUILayout.Slider("Opacity", layer.textureOpacity, 0, 1);

		EditorGUILayout.Space();

		layer.edgeRenderingEnabled = EditorGUILayout.Toggle(
			new GUIContent("Edge Rendering", "Highlight salient edges in the camera images in a specific color"),
			layer.edgeRenderingEnabled);
		layer.edgeColor = EditorGUILayout.ColorField("Edge Color", layer.edgeColor);

		EditorGUILayout.Space();

		// Custom popup for color map type to control order, names, and visibility of types
		int colorMapTypeIndex = Array.IndexOf(_colorMapTypes, layer.colorMapEditorType);
		if (colorMapTypeIndex == -1)
		{
			Debug.LogWarning("Invalid color map type encountered");
			colorMapTypeIndex = 0;
		}
		// Dropdown list contains "Custom" only if it is currently selected.
		string[] colorMapNames = layer.colorMapEditorType == ColorMapEditorType.Custom ? _colorMapNames
			: _selectableColorMapNames;
		colorMapTypeIndex = EditorGUILayout.Popup(new GUIContent("Color Control", "The type of color controls applied to this layer"), colorMapTypeIndex, colorMapNames);
		layer.colorMapEditorType = _colorMapTypes[colorMapTypeIndex];

		if (layer.colorMapEditorType == ColorMapEditorType.Grayscale
			|| layer.colorMapEditorType == ColorMapEditorType.GrayscaleToColor
			|| layer.colorMapEditorType == ColorMapEditorType.ColorAdjustment
		) {
			layer.colorMapEditorContrast = EditorGUILayout.Slider("Contrast", layer.colorMapEditorContrast, -1, 1);
			layer.colorMapEditorBrightness = EditorGUILayout.Slider("Brightness", layer.colorMapEditorBrightness, -1, 1);
		}

		if (layer.colorMapEditorType == ColorMapEditorType.Grayscale
			|| layer.colorMapEditorType == ColorMapEditorType.GrayscaleToColor)
		{
			layer.colorMapEditorPosterize = EditorGUILayout.Slider("Posterize", layer.colorMapEditorPosterize, 0, 1);
		}


		if (layer.colorMapEditorType == ColorMapEditorType.GrayscaleToColor)
		{
			layer.colorMapEditorGradient = EditorGUILayout.GradientField("Colorize", layer.colorMapEditorGradient);
		}

		if (GUI.changed)
		{
			EditorUtility.SetDirty(layer);
		}
	}
}
