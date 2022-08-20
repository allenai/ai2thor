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
using System.Collections.Generic;
using System.Reflection;

[CustomEditor(typeof(OVRManager))]
public class OVRManagerEditor : Editor
{
	override public void OnInspectorGUI()
	{
		OVRRuntimeSettings runtimeSettings = OVRRuntimeSettings.GetRuntimeSettings();
		OVRProjectConfig projectConfig = OVRProjectConfig.GetProjectConfig();

#if UNITY_ANDROID
		OVRProjectConfigEditor.DrawTargetDeviceInspector(projectConfig);
		EditorGUILayout.Space();
#endif

		DrawDefaultInspector();

		bool modified = false;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_ANDROID
		OVRManager manager = (OVRManager)target;

		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Display", EditorStyles.boldLabel);

		OVRManager.ColorSpace colorGamut = runtimeSettings.colorSpace;
		OVREditorUtil.SetupEnumField(target, new GUIContent("Color Gamut",
			"The target color gamut when displayed on the HMD"), ref colorGamut, ref modified,
			"https://developer.oculus.com/documentation/unity/unity-color-space/");
		manager.colorGamut = colorGamut;

		if (modified)
		{
			runtimeSettings.colorSpace = colorGamut;
			OVRRuntimeSettings.CommitRuntimeSettings(runtimeSettings);
		}
#endif

		EditorGUILayout.Space();
        OVRProjectConfigEditor.DrawProjectConfigInspector(projectConfig);

#if UNITY_ANDROID
		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Mixed Reality Capture for Quest", EditorStyles.boldLabel);
		EditorGUI.indentLevel++;
		OVREditorUtil.SetupEnumField(target, "ActivationMode", ref manager.mrcActivationMode, ref modified);
		EditorGUI.indentLevel--;
#endif

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
		EditorGUILayout.Space();
		EditorGUILayout.BeginHorizontal();
		manager.expandMixedRealityCapturePropertySheet = EditorGUILayout.BeginFoldoutHeaderGroup(manager.expandMixedRealityCapturePropertySheet, "Mixed Reality Capture");
		OVREditorUtil.DisplayDocLink("https://developer.oculus.com/documentation/unity/unity-mrc/");
		EditorGUILayout.EndHorizontal();
		if (manager.expandMixedRealityCapturePropertySheet)
		{
			string[] layerMaskOptions = new string[32];
			for (int i=0; i<32; ++i)
			{
				layerMaskOptions[i] = LayerMask.LayerToName(i);
				if (layerMaskOptions[i].Length == 0)
				{
					layerMaskOptions[i] = "<Layer " + i.ToString() + ">";
				}
			}

			EditorGUI.indentLevel++;

			OVREditorUtil.SetupBoolField(target, "enableMixedReality", ref manager.enableMixedReality, ref modified);
			OVREditorUtil.SetupEnumField(target, "compositionMethod", ref manager.compositionMethod, ref modified);
			OVREditorUtil.SetupLayerMaskField(target, "extraHiddenLayers", ref manager.extraHiddenLayers, layerMaskOptions, ref modified);
			OVREditorUtil.SetupLayerMaskField(target, "extraVisibleLayers", ref manager.extraVisibleLayers, layerMaskOptions, ref modified);
			OVREditorUtil.SetupBoolField(target, "dynamicCullingMask", ref manager.dynamicCullingMask, ref modified);

			if (manager.compositionMethod == OVRManager.CompositionMethod.External)
			{
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("External Composition", EditorStyles.boldLabel);
				EditorGUI.indentLevel++;

				OVREditorUtil.SetupColorField(target, "backdropColor (target, Rift)", ref manager.externalCompositionBackdropColorRift, ref modified);
				OVREditorUtil.SetupColorField(target, "backdropColor (target, Quest)", ref manager.externalCompositionBackdropColorQuest, ref modified);
				EditorGUI.indentLevel--;
			}

			if (manager.compositionMethod == OVRManager.CompositionMethod.Direct)
			{
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Direct Composition", EditorStyles.boldLabel);
				EditorGUI.indentLevel++;

				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Camera", EditorStyles.boldLabel);
				OVREditorUtil.SetupEnumField(target, "capturingCameraDevice", ref manager.capturingCameraDevice, ref modified);
				OVREditorUtil.SetupBoolField(target, "flipCameraFrameHorizontally", ref manager.flipCameraFrameHorizontally, ref modified);
				OVREditorUtil.SetupBoolField(target, "flipCameraFrameVertically", ref manager.flipCameraFrameVertically, ref modified);

				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Chroma Key", EditorStyles.boldLabel);
				OVREditorUtil.SetupColorField(target, "chromaKeyColor", ref manager.chromaKeyColor, ref modified);
				OVREditorUtil.SetupFloatField(target, "chromaKeySimilarity", ref manager.chromaKeySimilarity, ref modified);
				OVREditorUtil.SetupFloatField(target, "chromaKeySmoothRange", ref manager.chromaKeySmoothRange, ref modified);
				OVREditorUtil.SetupFloatField(target, "chromaKeySpillRange", ref manager.chromaKeySpillRange, ref modified);

				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Dynamic Lighting", EditorStyles.boldLabel);
				OVREditorUtil.SetupBoolField(target, "useDynamicLighting", ref manager.useDynamicLighting, ref modified);
				OVREditorUtil.SetupEnumField(target, "depthQuality", ref manager.depthQuality, ref modified);
				OVREditorUtil.SetupFloatField(target, "dynamicLightingSmoothFactor", ref manager.dynamicLightingSmoothFactor, ref modified);
				OVREditorUtil.SetupFloatField(target, "dynamicLightingDepthVariationClampingValue", ref manager.dynamicLightingDepthVariationClampingValue, ref modified);

				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Virtual Green Screen", EditorStyles.boldLabel);
				OVREditorUtil.SetupEnumField(target, "virtualGreenScreenType", ref manager.virtualGreenScreenType, ref modified);
				OVREditorUtil.SetupFloatField(target, "virtualGreenScreenTopY", ref manager.virtualGreenScreenTopY, ref modified);
				OVREditorUtil.SetupFloatField(target, "virtualGreenScreenBottomY", ref manager.virtualGreenScreenBottomY, ref modified);
				OVREditorUtil.SetupBoolField(target, "virtualGreenScreenApplyDepthCulling", ref manager.virtualGreenScreenApplyDepthCulling, ref modified);
				OVREditorUtil.SetupFloatField(target, "virtualGreenScreenDepthTolerance", ref manager.virtualGreenScreenDepthTolerance, ref modified);

				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Latency Control", EditorStyles.boldLabel);
				OVREditorUtil.SetupFloatField(target, "handPoseStateLatency", ref manager.handPoseStateLatency, ref modified);
				EditorGUI.indentLevel--;
			}

			EditorGUI.indentLevel--;
		}
		EditorGUILayout.EndFoldoutHeaderGroup();
#endif

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_ANDROID
		// Insight Passthrough section
#if UNITY_ANDROID
		bool passthroughCapabilityEnabled = projectConfig.insightPassthroughEnabled;
		EditorGUI.BeginDisabledGroup(!passthroughCapabilityEnabled);
		GUIContent enablePassthroughContent = new GUIContent("Enable Passthrough", "Enables passthrough functionality for the scene. Can be toggled at runtime. Passthrough Capability must be enabled in the project settings.");
#else
		GUIContent enablePassthroughContent = new GUIContent("Enable Passthrough", "Enables passthrough functionality for the scene. Can be toggled at runtime.");
#endif
		EditorGUILayout.Space();
		EditorGUILayout.LabelField("Insight Passthrough", EditorStyles.boldLabel);
#if UNITY_ANDROID
		if (!passthroughCapabilityEnabled) {
			EditorGUILayout.LabelField("Requires Passthrough Capability to be enabled in the General section of the Quest features.", EditorStyles.wordWrappedLabel);
		}
#endif
		OVREditorUtil.SetupBoolField(target, enablePassthroughContent, ref manager.isInsightPassthroughEnabled, ref modified);
#if UNITY_ANDROID
		EditorGUI.EndDisabledGroup();
#endif
#endif


		if (modified)
		{
			EditorUtility.SetDirty(target);
		}
	}

}
