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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(OVROverlay))]
public class OVROverlayEditor : Editor
{

	/// <summary>
	/// Common Video Types, to ease source and dest rect creation
	/// </summary>
	public enum StereoType
	{
		Custom = 0,
		Mono = 1,
		Stereo = 2,
		StereoLeftRight = 3,
		StereoTopBottom = 4,
	}

	public enum DisplayType
	{
		Custom = 0,
		Full = 1,
		Half = 2,
	}

	private bool sourceRectsVisible = false;
	private bool destRectsVisible = false;

	private Material _SrcRectMaterial;
	protected Material SrcRectMaterial
	{
		get
		{
			if (_SrcRectMaterial == null)
			{
				string[] shaders = AssetDatabase.FindAssets("OVROverlaySrcRectEditor");

				if (shaders.Length > 0)
				{
					Shader shader = (Shader)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(shaders[0]), typeof(Shader));

					if (shader != null)
					{
						_SrcRectMaterial = new Material(shader);
					}
				}
			}
			return _SrcRectMaterial;
		}
	}

	private Material _DestRectMaterial;
	protected Material DestRectMaterial
	{
		get
		{
			if (_DestRectMaterial == null)
			{
				string[] shaders = AssetDatabase.FindAssets("OVROverlayDestRectEditor");

				if (shaders.Length > 0)
				{
					Shader shader = (Shader)AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(shaders[0]), typeof(Shader));

					if (shader != null)
					{
						_DestRectMaterial = new Material(shader);
					}
				}
			}
			return _DestRectMaterial;
		}
	}

	private TextureRect _DraggingRect;
	private Side _DraggingSide;

	enum TextureRect
	{
		None,
		SrcLeft,
		SrcRight,
		DestLeft,
		DestRight
	}

	enum Side
	{
		Left,
		Right,
		Top,
		Bottom
	}

	private GUIContent[] selectableShapeNames;
	private OVROverlay.OverlayShape[] selectableShapeValues;

	private void Awake()
	{
		List<GUIContent> selectableShapeNameList = new List<GUIContent>();
		List<OVROverlay.OverlayShape> selectableShapesValueList = new List<OVROverlay.OverlayShape>();
		foreach (OVROverlay.OverlayShape value in Enum.GetValues(typeof(OVROverlay.OverlayShape)))
		{
			if (!OVROverlay.IsPassthroughShape(value))
			{
				string name = Enum.GetName(typeof(OVROverlay.OverlayShape), value);
				selectableShapeNameList.Add(new GUIContent(name, name));
				selectableShapesValueList.Add(value);
			}
		}
		selectableShapeNames = selectableShapeNameList.ToArray();
		selectableShapeValues = selectableShapesValueList.ToArray();
	}

	public override void OnInspectorGUI()
	{
		OVROverlay overlay = (OVROverlay)target;
		if (overlay == null)
		{
			return;
		}

		EditorGUILayout.LabelField("Display Order", EditorStyles.boldLabel);
		overlay.currentOverlayType = (OVROverlay.OverlayType)EditorGUILayout.EnumPopup(new GUIContent("Current Overlay Type", "Whether this overlay should layer behind the scene or in front of it"), overlay.currentOverlayType);
		overlay.compositionDepth = EditorGUILayout.IntField(new GUIContent("Composition Depth", "Depth value used to sort OVROverlays in the scene, smaller value appears in front"), overlay.compositionDepth);
		overlay.noDepthBufferTesting = EditorGUILayout.Toggle(new GUIContent("No Depth Buffer Testing", "The noDepthBufferTesting will stop layer's depth buffer compositing even if the engine has \"Shared Depth Buffer\" enabled"), overlay.noDepthBufferTesting);
		EditorGUILayout.Space();

		EditorGUILayout.LabelField(new GUIContent("Overlay Shape", "The shape of this overlay"), EditorStyles.boldLabel);
		// If the overlay shape has been set to a passthrough shape (via scripting), do not allow to change it.
		if (!OVROverlay.IsPassthroughShape(overlay.currentOverlayShape))
		{
			int currentShapeIndex = Array.IndexOf(selectableShapeValues, overlay.currentOverlayShape);
			if (currentShapeIndex == -1)
			{
				Debug.LogError("Invalid shape encountered");
				currentShapeIndex = 0;
			}
			currentShapeIndex = EditorGUILayout.Popup(new GUIContent("Overlay Shape", "The shape of this overlay"), currentShapeIndex, selectableShapeNames);
			overlay.currentOverlayShape = selectableShapeValues[currentShapeIndex];
		}

		if (overlay.currentOverlayShape == OVROverlay.OverlayShape.Cubemap)
		{
			overlay.useLegacyCubemapRotation = EditorGUILayout.Toggle(new GUIContent("Use Legacy Cubemap Rotation",
				"Whether the cubemap should use the legacy rotation which was rotated 180 degrees around the Y axis comapred to Unity's definition of cubemaps. This setting will be deprecated in the near future, therefore it is recommended to fix the cubemap texture instead."), overlay.useLegacyCubemapRotation);
		}

		EditorGUILayout.Space();

		EditorGUILayout.LabelField("Layer Properties", EditorStyles.boldLabel);
		overlay.useBicubicFiltering = EditorGUILayout.Toggle(new GUIContent("Bicubic Filtering",
			"Whether this layer should use bicubic filtering. This can increase quality for small details on text and icons being viewed at farther distances."), overlay.useBicubicFiltering);
		overlay.useEfficientSupersample = EditorGUILayout.Toggle(new GUIContent("Super Sample",
			"Whether this layer should use an efficient super sample filter. This can help reduce flicker artifacts."), overlay.useEfficientSupersample);
		overlay.useEfficientSharpen = EditorGUILayout.Toggle(new GUIContent("Sharpen",
			"Whether this layer should use a sharpen filter. This amplifies contrast and fine details"), overlay.useEfficientSharpen);

		EditorGUILayout.Space();

		EditorGUILayout.Separator();
		EditorGUILayout.LabelField("Textures", EditorStyles.boldLabel);

#if UNITY_ANDROID
		bool lastIsExternalSurface = overlay.isExternalSurface;
		overlay.isExternalSurface = EditorGUILayout.Toggle(new GUIContent("Is External Surface", "On Android, retrieve an Android Surface object to render to (e.g., video playback)"), overlay.isExternalSurface);

		if (lastIsExternalSurface)
		{
			overlay.externalSurfaceWidth = EditorGUILayout.IntField("External Surface Width", overlay.externalSurfaceWidth);
			overlay.externalSurfaceHeight = EditorGUILayout.IntField("External Surface Height", overlay.externalSurfaceHeight);
			overlay.isProtectedContent = EditorGUILayout.Toggle(new GUIContent("Is Protected Content", "The external surface has L1 widevine protection."), overlay.isProtectedContent);
		}
		else
#endif
		{
			if (overlay.textures == null)
			{
				overlay.textures = new Texture[2];
			}
			if (overlay.textures.Length < 2)
			{
				Texture[] tmp = new Texture[2];
				for (int i = 0; i < overlay.textures.Length; i++)
				{
					tmp[i] = overlay.textures[i];
				}
				overlay.textures = tmp;
			}

			var labelControlRect = EditorGUILayout.GetControlRect();
			EditorGUI.LabelField(new Rect(labelControlRect.x, labelControlRect.y, labelControlRect.width / 2, labelControlRect.height), new GUIContent("Left Texture", "Texture used for the left eye"));
			EditorGUI.LabelField(new Rect(labelControlRect.x + labelControlRect.width / 2, labelControlRect.y, labelControlRect.width / 2, labelControlRect.height), new GUIContent("Right Texture", "Texture used for the right eye"));


			var textureControlRect = EditorGUILayout.GetControlRect(GUILayout.Height(64));

			overlay.textures[0] = (Texture)EditorGUI.ObjectField(new Rect(textureControlRect.x, textureControlRect.y, 64, textureControlRect.height), overlay.textures[0], typeof(Texture), true);
			Texture right = (Texture)EditorGUI.ObjectField(new Rect(textureControlRect.x + textureControlRect.width / 2, textureControlRect.y, 64, textureControlRect.height), overlay.textures[1] != null ? overlay.textures[1] : overlay.textures[0], typeof(Texture), true);
			if (right == overlay.textures[0])
			{
				overlay.textures[1] = null;
			}
			else
			{
				overlay.textures[1] = right;
			}

			overlay.isDynamic = EditorGUILayout.Toggle(new GUIContent("Dynamic Texture", "This texture will be updated dynamically at runtime (e.g., Video)"), overlay.isDynamic);
#if !UNITY_ANDROID
			overlay.isProtectedContent = EditorGUILayout.Toggle(new GUIContent("Is Protected Content", "The texture has copy protection, e.g., HDCP"), overlay.isProtectedContent);
#endif
		}
		if (overlay.currentOverlayShape == OVROverlay.OverlayShape.Cylinder || overlay.currentOverlayShape == OVROverlay.OverlayShape.Equirect || overlay.currentOverlayShape == OVROverlay.OverlayShape.Quad || overlay.currentOverlayShape == OVROverlay.OverlayShape.Fisheye)
		{

			EditorGUILayout.Separator();
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Texture Rects", EditorStyles.boldLabel);

			bool lastOverrideTextureRectMatrix = overlay.overrideTextureRectMatrix;
			overlay.overrideTextureRectMatrix = !EditorGUILayout.Toggle(new GUIContent("Use Default Rects", overlay.textures[1] == null ? "If you need to use a single texture as a stereo image, uncheck this box" : "Uncheck this box if you need to clip you textures or layer"), !overlay.overrideTextureRectMatrix);

			if (lastOverrideTextureRectMatrix)
			{
				sourceRectsVisible = EditorGUILayout.Foldout(sourceRectsVisible, new GUIContent("Source Rects", "What portion of the source texture will ultimately be shown in each eye."));

				if (sourceRectsVisible)
				{
					var mat = SrcRectMaterial;

					if (mat != null)
					{
						Rect drawRect = EditorGUILayout.GetControlRect(GUILayout.Height(128 + 8));
						Vector4 srcLeft = new Vector4(Mathf.Max(0.0f, overlay.srcRectLeft.x), Mathf.Max(0.0f, overlay.srcRectLeft.y), Mathf.Min(1.0f - overlay.srcRectLeft.x, overlay.srcRectLeft.width), Mathf.Min(1.0f - overlay.srcRectLeft.y, overlay.srcRectLeft.height));
						Vector4 srcRight = new Vector4(Mathf.Max(0.0f, overlay.srcRectRight.x), Mathf.Max(0.0f, overlay.srcRectRight.y), Mathf.Min(1.0f - overlay.srcRectRight.x, overlay.srcRectRight.width), Mathf.Min(1.0f - overlay.srcRectRight.y, overlay.srcRectRight.height));

						if (overlay.invertTextureRects)
						{
							srcLeft.y = 1 - srcLeft.y - srcLeft.w;
							srcRight.y = 1 - srcRight.y - srcRight.w;
						}
						mat.SetVector("_SrcRectLeft", srcLeft);
						mat.SetVector("_SrcRectRight", srcRight);
						// center our draw rect
						var drawRectCentered = new Rect(drawRect.x + drawRect.width / 2 - 128 - 4, drawRect.y, 256 + 8, drawRect.height);
						EditorGUI.DrawPreviewTexture(drawRectCentered, overlay.textures[0] ?? Texture2D.blackTexture, mat);

						var drawRectInset = new Rect(drawRectCentered.x + 4, drawRectCentered.y + 4, drawRectCentered.width - 8, drawRectCentered.height - 8);
						UpdateRectDragging(drawRectInset, drawRectInset, TextureRect.SrcLeft, TextureRect.SrcRight, overlay.invertTextureRects, ref overlay.srcRectLeft, ref overlay.srcRectRight);
						CreateCursorRects(drawRectInset, overlay.srcRectLeft, overlay.invertTextureRects);
						CreateCursorRects(drawRectInset, overlay.srcRectRight, overlay.invertTextureRects);
					}

					var labelControlRect = EditorGUILayout.GetControlRect();
					EditorGUI.LabelField(new Rect(labelControlRect.x, labelControlRect.y, labelControlRect.width / 2, labelControlRect.height), new GUIContent("Left Source Rect", "The rect in the source image that will be displayed on the left eye layer"));
					EditorGUI.LabelField(new Rect(labelControlRect.x + labelControlRect.width / 2, labelControlRect.y, labelControlRect.width / 2, labelControlRect.height), new GUIContent("Right Source Rect", "The rect in the source image that will be displayed on the right eye layer"));

					var rectControlRect = EditorGUILayout.GetControlRect(GUILayout.Height(34));

					overlay.srcRectLeft = Clamp01(EditorGUI.RectField(new Rect(rectControlRect.x, rectControlRect.y, rectControlRect.width / 2 - 20, rectControlRect.height), overlay.srcRectLeft));
					overlay.srcRectRight = Clamp01(EditorGUI.RectField(new Rect(rectControlRect.x + rectControlRect.width / 2, rectControlRect.y, rectControlRect.width / 2 - 20, rectControlRect.height), overlay.srcRectRight));


					EditorGUILayout.BeginHorizontal();
					if (overlay.textures[1] != null)
					{
						if (GUILayout.Button(new GUIContent("Reset To Default", "Reset Source Rects to default")))
						{
							SetRectsByVideoType(overlay, StereoType.Stereo, DisplayType.Custom);
						}
					}
					else
					{
						if (GUILayout.Button(new GUIContent("Monoscopic", "Display the full Texture in both eyes")))
						{
							SetRectsByVideoType(overlay, StereoType.Mono, DisplayType.Custom);
						}
						if (GUILayout.Button(new GUIContent("Stereo Left/Right", "The left half of the texture is displayed in the left eye, and the right half in the right eye")))
						{
							SetRectsByVideoType(overlay, StereoType.StereoLeftRight, DisplayType.Custom);
						}
						if (GUILayout.Button(new GUIContent("Stereo Top/Bottom", "The top half of the texture is displayed in the left eye, and the bottom half in the right eye")))
						{
							SetRectsByVideoType(overlay, StereoType.StereoTopBottom, DisplayType.Custom);
						}
					}
					EditorGUILayout.EndHorizontal();

				}
				destRectsVisible = EditorGUILayout.Foldout(destRectsVisible, new GUIContent("Destination Rects", "What portion of the destination texture that the source will be rendered into."));
				if (destRectsVisible)
				{

					var mat = DestRectMaterial;

					if (mat != null)
					{
						Rect drawRect = EditorGUILayout.GetControlRect(GUILayout.Height(128 + 8));

						Vector4 srcLeft = new Vector4(Mathf.Max(0.0f, overlay.srcRectLeft.x), Mathf.Max(0.0f, overlay.srcRectLeft.y), Mathf.Min(1.0f - overlay.srcRectLeft.x, overlay.srcRectLeft.width), Mathf.Min(1.0f - overlay.srcRectLeft.y, overlay.srcRectLeft.height));
						Vector4 srcRight = new Vector4(Mathf.Max(0.0f, overlay.srcRectRight.x), Mathf.Max(0.0f, overlay.srcRectRight.y), Mathf.Min(1.0f - overlay.srcRectRight.x, overlay.srcRectRight.width), Mathf.Min(1.0f - overlay.srcRectRight.y, overlay.srcRectRight.height));
						Vector4 destLeft = new Vector4(Mathf.Max(0.0f, overlay.destRectLeft.x), Mathf.Max(0.0f, overlay.destRectLeft.y), Mathf.Min(1.0f - overlay.destRectLeft.x, overlay.destRectLeft.width), Mathf.Min(1.0f - overlay.destRectLeft.y, overlay.destRectLeft.height));
						Vector4 destRight = new Vector4(Mathf.Max(0.0f, overlay.destRectRight.x), Mathf.Max(0.0f, overlay.destRectRight.y), Mathf.Min(1.0f - overlay.destRectRight.x, overlay.destRectRight.width), Mathf.Min(1.0f - overlay.destRectRight.y, overlay.destRectRight.height));

						if (overlay.invertTextureRects)
						{
							srcLeft.y = 1 - srcLeft.y - srcLeft.w;
							srcRight.y = 1 - srcRight.y - srcRight.w;
							destLeft.y = 1 - destLeft.y - destLeft.w;
							destRight.y = 1 - destRight.y - destRight.w;
						}
						mat.SetVector("_SrcRectLeft", srcLeft);
						mat.SetVector("_SrcRectRight", srcRight);
						mat.SetVector("_DestRectLeft", destLeft);
						mat.SetVector("_DestRectRight", destRight);
						mat.SetColor("_BackgroundColor", EditorGUIUtility.isProSkin ? (Color)new Color32(56, 56, 56, 255) : (Color)new Color32(194, 194, 194, 255));

						var drawRectCentered = new Rect(drawRect.x + drawRect.width / 2 - 128 - 16 - 4, drawRect.y, 256 + 32 + 8, drawRect.height);
						// center our draw rect
						EditorGUI.DrawPreviewTexture(drawRectCentered, overlay.textures[0] ?? Texture2D.blackTexture, mat);

						var drawRectInsetLeft = new Rect(drawRectCentered.x + 4, drawRectCentered.y + 4, drawRectCentered.width / 2 - 20, drawRectCentered.height - 8);
						var drawRectInsetRight = new Rect(drawRectCentered.x + drawRectCentered.width / 2 + 16, drawRectCentered.y + 4, drawRectCentered.width / 2 - 20, drawRectCentered.height - 8);
						UpdateRectDragging(drawRectInsetLeft, drawRectInsetRight, TextureRect.DestLeft, TextureRect.DestRight, overlay.invertTextureRects, ref overlay.destRectLeft, ref overlay.destRectRight);

						CreateCursorRects(drawRectInsetLeft, overlay.destRectLeft, overlay.invertTextureRects);
						CreateCursorRects(drawRectInsetRight, overlay.destRectRight, overlay.invertTextureRects);

					}

					var labelControlRect = EditorGUILayout.GetControlRect();
					EditorGUI.LabelField(new Rect(labelControlRect.x, labelControlRect.y, labelControlRect.width / 2, labelControlRect.height), new GUIContent("Left Destination Rect", "The rect in the destination layer the left eye will display to"));
					EditorGUI.LabelField(new Rect(labelControlRect.x + labelControlRect.width / 2, labelControlRect.y, labelControlRect.width / 2, labelControlRect.height), new GUIContent("Right Destination Rect", "The rect in the destination layer the right eye will display to"));

					var rectControlRect = EditorGUILayout.GetControlRect(GUILayout.Height(34));

					overlay.destRectLeft = Clamp01(EditorGUI.RectField(new Rect(rectControlRect.x, rectControlRect.y, rectControlRect.width / 2 - 20, rectControlRect.height), overlay.destRectLeft));
					overlay.destRectRight = Clamp01(EditorGUI.RectField(new Rect(rectControlRect.x + rectControlRect.width / 2, rectControlRect.y, rectControlRect.width / 2 - 20, rectControlRect.height), overlay.destRectRight));


					if (overlay.currentOverlayShape == OVROverlay.OverlayShape.Equirect)
					{
						EditorGUILayout.BeginHorizontal();
						if (GUILayout.Button(new GUIContent("360 Video", "Display the full 360 layer")))
						{
							SetRectsByVideoType(overlay, StereoType.Custom, DisplayType.Full);
						}
						if (GUILayout.Button(new GUIContent("180 Video", "Display the front 180 layer")))
						{
							SetRectsByVideoType(overlay, StereoType.Custom, DisplayType.Half);
						}
						EditorGUILayout.EndHorizontal();
					}
					else
					{
						if (GUILayout.Button(new GUIContent("Reset To Default", "Reset Source Rects to default")))
						{
							SetRectsByVideoType(overlay, StereoType.Custom, DisplayType.Full);
						}
					}
				}

				overlay.invertTextureRects = EditorGUILayout.Toggle(new GUIContent("Invert Rect Coordinates", "Check this box to use the top left corner of the texture as the origin"), overlay.invertTextureRects);
			}
		}

		EditorGUILayout.Separator();
		EditorGUILayout.LabelField("Color Scale", EditorStyles.boldLabel);
		EditorGUILayout.Space();
		overlay.overridePerLayerColorScaleAndOffset = EditorGUILayout.Toggle(new GUIContent("Override Color Scale", "Manually set color scale and offset of this layer, regardless of what the global values are from OVRManager.SetColorScaleAndOffset()."), overlay.overridePerLayerColorScaleAndOffset);
		if (overlay.overridePerLayerColorScaleAndOffset)
		{
			Vector4 colorScale = EditorGUILayout.Vector4Field(new GUIContent("Color Scale", "Scale that the color values for this overlay will be multiplied by."), overlay.colorScale);
			Vector4 colorOffset = EditorGUILayout.Vector4Field(new GUIContent("Color Offset", "Offset that the color values for this overlay will be added to."), overlay.colorOffset);
			overlay.SetPerLayerColorScaleAndOffset(colorScale, colorOffset);
		}

		EditorGUILayout.Separator();
		EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
		overlay.previewInEditor = EditorGUILayout.Toggle(new GUIContent("Preview in Editor (Experimental)", "Preview the overlay in the editor using a mesh renderer."), overlay.previewInEditor);



		EditorUtility.SetDirty(overlay);
	}

	private Rect Clamp01(Rect rect)
	{
		rect.x = Mathf.Clamp01(rect.x);
		rect.y = Mathf.Clamp01(rect.y);
		rect.width = Mathf.Clamp01(rect.width);
		rect.height = Mathf.Clamp01(rect.height);
		return rect;
	}

	private bool IsUnitRect(Rect rect)
	{
		return IsRect(rect, 0, 0, 1, 1);
	}

	private bool IsRect(Rect rect, float x, float y, float w, float h)
	{
		return rect.x == x && rect.y == y && rect.width == w && rect.height == h;
	}

	private StereoType GetStereoType(OVROverlay overlay)
	{
		if (overlay.textures[0] != null && overlay.textures[1] != null)
		{
			if (IsUnitRect(overlay.srcRectLeft) && IsUnitRect(overlay.srcRectRight))
			{
				return StereoType.Stereo;
			}
			else
			{
				return StereoType.Custom;
			}
		}
		else if (overlay.textures[0] != null)
		{
			if (IsUnitRect(overlay.srcRectLeft) && IsUnitRect(overlay.srcRectRight))
			{
				return StereoType.Mono;
			}
			else if (IsRect(overlay.srcRectLeft, 0, 0, 0.5f, 1f) && IsRect(overlay.srcRectRight, 0.5f, 0, 0.5f, 1f))
			{
				return StereoType.StereoLeftRight;
			}
			else if (overlay.invertTextureRects && IsRect(overlay.srcRectLeft, 0, 0.0f, 1f, 0.5f) && IsRect(overlay.srcRectRight, 0f, 0.5f, 1f, 0.5f))
			{
				return StereoType.StereoTopBottom;
			}
			else if (!overlay.invertTextureRects && IsRect(overlay.srcRectLeft, 0, 0.5f, 1f, 0.5f) && IsRect(overlay.srcRectRight, 0f, 0f, 1f, 0.5f))
			{
				return StereoType.StereoTopBottom;
			}
			else
			{
				return StereoType.Custom;
			}
		}
		else
		{
			return StereoType.Mono;
		}
	}

	private void SetRectsByVideoType(OVROverlay overlay, StereoType stereoType, DisplayType displayType)
	{
		Rect srcRectLeft, srcRectRight, destRectLeft, destRectRight;

		switch (displayType)
		{
			case DisplayType.Full:
				destRectLeft = destRectRight = new Rect(0, 0, 1, 1);
				break;

			case DisplayType.Half:
				destRectLeft = destRectRight = new Rect(0.25f, 0, 0.5f, 1);
				break;

			default:
				destRectLeft = overlay.destRectLeft;
				destRectRight = overlay.destRectRight;
				break;
		}

		switch (stereoType)
		{
			case StereoType.Mono:
			case StereoType.Stereo:
				srcRectLeft = srcRectRight = new Rect(0, 0, 1, 1);
				break;

			case StereoType.StereoTopBottom:
				if (overlay.invertTextureRects)
				{
					srcRectLeft = new Rect(0, 0.0f, 1, 0.5f);
					srcRectRight = new Rect(0, 0.5f, 1, 0.5f);
				}
				else
				{
					srcRectLeft = new Rect(0, 0.5f, 1, 0.5f);
					srcRectRight = new Rect(0, 0.0f, 1, 0.5f);
				}
				break;

			case StereoType.StereoLeftRight:
				srcRectLeft = new Rect(0, 0, 0.5f, 1);
				srcRectRight = new Rect(0.5f, 0, 0.5f, 1);
				break;

			default:
				srcRectLeft = overlay.srcRectLeft;
				srcRectRight = overlay.srcRectRight;
				break;
		}
		overlay.SetSrcDestRects(srcRectLeft, srcRectRight, destRectLeft, destRectRight);
	}

	private void GetCursorPoints(Rect drawRect, Rect selectRect, bool invertY, out Vector2 leftPos, out Vector2 rightPos, out Vector2 topPos, out Vector2 bottomPos)
	{
		if (invertY)
		{
			selectRect.y = 1 - selectRect.y - selectRect.height;
		}
		leftPos = new Vector2(drawRect.x + selectRect.x * drawRect.width, drawRect.y + (1 - selectRect.y - selectRect.height / 2) * drawRect.height);
		rightPos = new Vector2(drawRect.x + (selectRect.x + selectRect.width) * drawRect.width, drawRect.y + (1 - selectRect.y - selectRect.height / 2) * drawRect.height);
		topPos = new Vector2(drawRect.x + (selectRect.x + selectRect.width / 2) * drawRect.width, drawRect.y + (1 - selectRect.y - selectRect.height) * drawRect.height);
		bottomPos = new Vector2(drawRect.x + (selectRect.x + selectRect.width / 2) * drawRect.width, drawRect.y + (1 - selectRect.y) * drawRect.height);

		if (invertY)
		{
			// swap top and bottom
			var tmp = topPos;
			topPos = bottomPos;
			bottomPos = tmp;
		}
	}

	private void CreateCursorRects(Rect drawRect, Rect selectRect, bool invertY)
	{
		Vector2 leftPos, rightPos, topPos, bottomPos;
		GetCursorPoints(drawRect, selectRect, invertY, out leftPos, out rightPos, out topPos, out bottomPos);

		EditorGUIUtility.AddCursorRect(new Rect(leftPos - 5 * Vector2.one, 10 * Vector2.one), MouseCursor.ResizeHorizontal);
		EditorGUIUtility.AddCursorRect(new Rect(rightPos - 5 * Vector2.one, 10 * Vector2.one), MouseCursor.ResizeHorizontal);
		EditorGUIUtility.AddCursorRect(new Rect(topPos - 5 * Vector2.one, 10 * Vector2.one), MouseCursor.ResizeVertical);
		EditorGUIUtility.AddCursorRect(new Rect(bottomPos - 5 * Vector2.one, 10 * Vector2.one), MouseCursor.ResizeVertical);
	}

	private bool IsOverRectControls(Rect drawRect, Vector2 mousePos, Rect selectRect, bool invertY, ref Side side)
	{
		Vector2 leftPos, rightPos, topPos, bottomPos;
		GetCursorPoints(drawRect, selectRect, invertY, out leftPos, out rightPos, out topPos, out bottomPos);

		if ((leftPos - mousePos).sqrMagnitude <= 25)
		{
			side = Side.Left;
			return true;
		}
		if ((rightPos - mousePos).sqrMagnitude <= 25)
		{
			side = Side.Right;
			return true;
		}
		if ((topPos - mousePos).sqrMagnitude <= 25)
		{
			side = Side.Top;
			return true;
		}
		if ((bottomPos - mousePos).sqrMagnitude <= 25)
		{
			side = Side.Bottom;
			return true;
		}
		return false;
	}

	private void UpdateRectDragging(Rect drawingRectLeft, Rect drawingRectRight, TextureRect rectLeftType, TextureRect rectRightType, bool invertY, ref Rect rectLeft, ref Rect rectRight)
	{
		if (!Event.current.isMouse || Event.current.button != 0)
		{
			return;
		}

		if (Event.current.type == EventType.MouseUp)
		{
			_DraggingRect = TextureRect.None;
			return;
		}

		Vector2 mousePos = Event.current.mousePosition;
		if (_DraggingRect == TextureRect.None && Event.current.type == EventType.MouseDown)
		{
			if (IsOverRectControls(drawingRectLeft, mousePos, rectLeft, invertY, ref _DraggingSide))
			{
				_DraggingRect = rectLeftType;
			}
			if (_DraggingRect == TextureRect.None || Event.current.shift)
			{
				if (IsOverRectControls(drawingRectRight, mousePos, rectRight, invertY, ref _DraggingSide))
				{
					_DraggingRect = rectRightType;
				}
			}
		}

		if (_DraggingRect == rectLeftType)
		{
			SetRectSideValue(drawingRectLeft, mousePos, _DraggingSide, invertY, ref rectLeft);
		}
		if (_DraggingRect == rectRightType)
		{
			SetRectSideValue(drawingRectRight, mousePos, _DraggingSide, invertY, ref rectRight);
		}
	}

	private void SetRectSideValue(Rect drawingRect, Vector2 mousePos, Side side, bool invertY, ref Rect rect)
	{
		// quantize to 1/32
		float x = Mathf.Clamp01(Mathf.Round(((mousePos.x - drawingRect.x) / drawingRect.width) * 32) / 32.0f);
		float y = Mathf.Clamp01(Mathf.Round(((mousePos.y - drawingRect.y) / drawingRect.height) * 32) / 32.0f);
		if (!invertY)
		{
			y = 1 - y;
		}

		switch (side)
		{
			case Side.Left:
				float xMax = rect.xMax;
				rect.x = Mathf.Min(x, xMax);
				rect.width = xMax - rect.x;
				break;
			case Side.Right:
				rect.width = Mathf.Max(0, x - rect.x);
				break;
			case Side.Bottom:
				float yMax = rect.yMax;
				rect.y = Mathf.Min(y, yMax);
				rect.height = yMax - rect.y;
				break;
			case Side.Top:
				rect.height = Mathf.Max(0, y - rect.y);
				break;
		}
	}
}
