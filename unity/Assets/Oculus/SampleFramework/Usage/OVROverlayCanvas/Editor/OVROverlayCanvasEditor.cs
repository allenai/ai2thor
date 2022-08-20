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

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(OVROverlayCanvas))]
public class OVROverlayCanvasEditor : Editor {

	public override void OnInspectorGUI()
	{
		OVROverlayCanvas canvas = target as OVROverlayCanvas;

		EditorGUI.BeginChangeCheck();

		float lastTextureSize = canvas.MaxTextureSize;
		canvas.MaxTextureSize = EditorGUILayout.IntField(new GUIContent("Max Texture Size", "Limits the maximum size of the texture used for this canvas"), canvas.MaxTextureSize);
		canvas.MinTextureSize = EditorGUILayout.IntField(new GUIContent("Min Texture Size", "Limits the minimum size this texture will be displayed at"), canvas.MinTextureSize);

		// Automatically adjust pixels per unit when texture size is adjusted to maintain the same density
		canvas.PixelsPerUnit *= lastTextureSize / (float)canvas.MaxTextureSize;
		canvas.PixelsPerUnit = EditorGUILayout.FloatField(new GUIContent("Pixels Per Unit", "Controls the density of the texture"), canvas.PixelsPerUnit);

		canvas.DrawRate = EditorGUILayout.IntField(new GUIContent("Draw Rate", "Controls how frequently this canvas updates. A value of 1 means every frame, 2 means every other, etc."), canvas.DrawRate);
		if (canvas.DrawRate > 1)
		{
			canvas.DrawFrameOffset = EditorGUILayout.IntField(new GUIContent("Draw Frame Offset", "Allows you to alternate which frame each canvas will draw on by specifying a frame offset."), canvas.DrawFrameOffset);
		}

		canvas.Expensive = EditorGUILayout.Toggle(new GUIContent("Expensive", "Improve the visual appearance at the cost of additional GPU time"), canvas.Expensive);
		canvas.Opacity = (OVROverlayCanvas.DrawMode)EditorGUILayout.EnumPopup(new GUIContent("Opacity", "Treat this canvas as opaque, which is a big performance improvement"), canvas.Opacity);

		if (canvas.Opacity == OVROverlayCanvas.DrawMode.TransparentDefaultAlpha)
		{
			var prevColor = GUI.contentColor;
			GUI.contentColor = Color.yellow;
			EditorGUILayout.LabelField("Transparent Default Alpha is not recommended with overlapping semitransparent graphics.");
			GUI.contentColor = prevColor;
		}

		if (canvas.Opacity == OVROverlayCanvas.DrawMode.TransparentCorrectAlpha)
		{
			var graphics = canvas.GetComponentsInChildren<UnityEngine.UI.Graphic>();
			bool usingDefaultMaterial = false;
			foreach(var graphic in graphics)
			{
				if (graphic.material == null || graphic.material == graphic.defaultMaterial)
				{
					usingDefaultMaterial = true;
					break;
				}
			}

			if (usingDefaultMaterial)
			{
				var prevColor = GUI.contentColor;
				GUI.contentColor = Color.yellow;
				EditorGUILayout.LabelField("Some graphics in this canvas are using the default UI material.");
				EditorGUILayout.LabelField("Would you like to replace all of them with the corrected UI Material?");
				GUI.contentColor = prevColor;

				if (GUILayout.Button("Replace Materials"))
				{
					var matList = AssetDatabase.FindAssets("t:Material UI Default Correct");
					if (matList.Length > 0)
					{
						var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(matList[0]));

						foreach(var graphic in graphics)
						{
							if (graphic.material == null || graphic.material == graphic.defaultMaterial)
							{
								graphic.material = mat;
							}
						}
					}
				}
			}
		}
		if (canvas.Opacity == OVROverlayCanvas.DrawMode.TransparentCorrectAlpha ||
			canvas.Opacity == OVROverlayCanvas.DrawMode.TransparentDefaultAlpha)
		{
			if (PlayerSettings.colorSpace == ColorSpace.Gamma)
			{
				var prevColor = GUI.contentColor;
				GUI.contentColor = Color.yellow;
				EditorGUILayout.LabelField("Alpha blending may not be correct with Gamma Color Space");
				GUI.contentColor = prevColor;
			}
		}


		canvas.Layer = EditorGUILayout.LayerField(new GUIContent("Layer", "The layer this overlay should be drawn on"), canvas.Layer);

		if (Camera.main != null)
		{
			if ((Camera.main.cullingMask & (1 << canvas.gameObject.layer)) != 0)
			{
				var prevColor = GUI.contentColor;
				GUI.contentColor = Color.red;
				EditorGUILayout.LabelField("Main Camera does not cull " + LayerMask.LayerToName(canvas.gameObject.layer)+". Make sure the layer of this object is not drawn by the main camera");
				GUI.contentColor = prevColor;
			}
			if ((Camera.main.cullingMask & (1 << canvas.Layer)) == 0)
			{
				var prevColor = GUI.contentColor;
				GUI.contentColor = Color.red;
				EditorGUILayout.LabelField("Layer should be assigned to a layer visible to your main camera.");
				GUI.contentColor = prevColor;
			}
		}
		else
		{
			var prevColor = GUI.contentColor;
			GUI.contentColor = Color.yellow;
			EditorGUILayout.LabelField("No Main Camera found. Make sure you camera does not draw layer "+LayerMask.LayerToName(canvas.gameObject.layer));
			GUI.contentColor = prevColor;
		}

		if (canvas.Layer == canvas.gameObject.layer)
		{
			var prevColor = GUI.contentColor;
			GUI.contentColor = Color.red;
			EditorGUILayout.LabelField("Layer is set to the same layer as this object (" + LayerMask.LayerToName(canvas.gameObject.layer) + ").");
			GUI.contentColor = prevColor;
		}

		if (Application.isPlaying)
		{
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Editor Debug", EditorStyles.boldLabel);
			canvas.overlayEnabled = EditorGUILayout.Toggle("Overlay Enabled", canvas.overlayEnabled);			
		}
	}
}
