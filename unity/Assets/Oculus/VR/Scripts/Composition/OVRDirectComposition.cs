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
using UnityEngine;
using System.Collections;
using Object = UnityEngine.Object;

#if USING_URP
using UnityEngine.Rendering.Universal;
#endif

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN

public class OVRDirectComposition : OVRCameraComposition
{
	private GameObject previousMainCameraObject = null;
	public GameObject directCompositionCameraGameObject = null;
	public Camera directCompositionCamera = null;
	public RenderTexture boundaryMeshMaskTexture = null;

	public override OVRManager.CompositionMethod CompositionMethod() { return OVRManager.CompositionMethod.Direct; }

	public OVRDirectComposition(GameObject parentObject, Camera mainCamera, OVRMixedRealityCaptureConfiguration configuration)
		: base(parentObject, mainCamera, configuration)
	{
		RefreshCameraObjects(parentObject, mainCamera, configuration);
	}

	private void RefreshCameraObjects(GameObject parentObject, Camera mainCamera, OVRMixedRealityCaptureConfiguration configuration)
	{
		if (!hasCameraDeviceOpened)
		{
			Debug.LogWarning("[OVRDirectComposition] RefreshCameraObjects(): Unable to open camera device " + cameraDevice);
			return;
		}

		if (mainCamera.gameObject != previousMainCameraObject)
		{
			Debug.LogFormat("[OVRDirectComposition] Camera refreshed. Rebind camera to {0}", mainCamera.gameObject.name);

			OVRCompositionUtil.SafeDestroy(ref directCompositionCameraGameObject);
			directCompositionCamera = null;

			RefreshCameraRig(parentObject, mainCamera);

			Debug.Assert(directCompositionCameraGameObject == null);
			if (configuration.instantiateMixedRealityCameraGameObject != null)
			{
				directCompositionCameraGameObject = configuration.instantiateMixedRealityCameraGameObject(mainCamera.gameObject, OVRManager.MrcCameraType.Normal);
			}
			else
			{
				directCompositionCameraGameObject = Object.Instantiate(mainCamera.gameObject);
			}
			directCompositionCameraGameObject.name = "OculusMRC_DirectCompositionCamera";
			directCompositionCameraGameObject.transform.parent = cameraInTrackingSpace ? cameraRig.trackingSpace : parentObject.transform;
			if (directCompositionCameraGameObject.GetComponent<AudioListener>())
			{
				Object.Destroy(directCompositionCameraGameObject.GetComponent<AudioListener>());
			}
			if (directCompositionCameraGameObject.GetComponent<OVRManager>())
			{
				Object.Destroy(directCompositionCameraGameObject.GetComponent<OVRManager>());
			}
			directCompositionCamera = directCompositionCameraGameObject.GetComponent<Camera>();
#if USING_MRC_COMPATIBLE_URP_VERSION
			var directCamData = directCompositionCamera.GetUniversalAdditionalCameraData();
			if (directCamData != null)
			{
				directCamData.allowXRRendering = false;
			}
#elif USING_URP
			Debug.LogError("Using URP with MRC is only supported with URP version 10.0.0 or higher. Consider using Unity 2020 or higher.");
#else
			directCompositionCamera.stereoTargetEye = StereoTargetEyeMask.None;
#endif
			directCompositionCamera.depth = float.MaxValue;
			directCompositionCamera.rect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
			directCompositionCamera.cullingMask = (directCompositionCamera.cullingMask & ~configuration.extraHiddenLayers) | configuration.extraVisibleLayers;


			Debug.Log("DirectComposition activated : useDynamicLighting " + (configuration.useDynamicLighting ? "ON" : "OFF"));
			RefreshCameraFramePlaneObject(parentObject, directCompositionCamera, configuration);

			previousMainCameraObject = mainCamera.gameObject;
		}
	}

	public override void Update(GameObject gameObject, Camera mainCamera, OVRMixedRealityCaptureConfiguration configuration, OVRManager.TrackingOrigin trackingOrigin)
	{
		if (!hasCameraDeviceOpened)
		{
			return;
		}

		RefreshCameraObjects(gameObject, mainCamera, configuration);

		if (!OVRPlugin.SetHandNodePoseStateLatency(configuration.handPoseStateLatency))
		{
			Debug.LogWarning("HandPoseStateLatency is invalid. Expect a value between 0.0 to 0.5, get " + configuration.handPoseStateLatency);
		}

		directCompositionCamera.clearFlags = mainCamera.clearFlags;
		directCompositionCamera.backgroundColor = mainCamera.backgroundColor;
		if (configuration.dynamicCullingMask)
		{
			directCompositionCamera.cullingMask = (mainCamera.cullingMask & ~configuration.extraHiddenLayers) | configuration.extraVisibleLayers;
		}

		directCompositionCamera.nearClipPlane = mainCamera.nearClipPlane;
		directCompositionCamera.farClipPlane = mainCamera.farClipPlane;

		if (OVRMixedReality.useFakeExternalCamera || OVRPlugin.GetExternalCameraCount() == 0)
		{
			OVRPose trackingSpacePose = new OVRPose();
			trackingSpacePose.position = trackingOrigin == OVRManager.TrackingOrigin.EyeLevel ?
				OVRMixedReality.fakeCameraEyeLevelPosition :
				OVRMixedReality.fakeCameraFloorLevelPosition;
			trackingSpacePose.orientation = OVRMixedReality.fakeCameraRotation;
			directCompositionCamera.fieldOfView = OVRMixedReality.fakeCameraFov;
			directCompositionCamera.aspect = OVRMixedReality.fakeCameraAspect;
			if (cameraInTrackingSpace)
			{
				directCompositionCamera.transform.FromOVRPose(trackingSpacePose, true);
			}
			else
			{
				OVRPose worldSpacePose = new OVRPose();
				worldSpacePose = OVRExtensions.ToWorldSpacePose(trackingSpacePose, mainCamera);
				directCompositionCamera.transform.FromOVRPose(worldSpacePose);
			}
		}
		else
		{
			OVRPlugin.CameraExtrinsics extrinsics;
			OVRPlugin.CameraIntrinsics intrinsics;

			// So far, only support 1 camera for MR and always use camera index 0
			if (OVRPlugin.GetMixedRealityCameraInfo(0, out extrinsics, out intrinsics))
			{
				float fovY = Mathf.Atan(intrinsics.FOVPort.UpTan) * Mathf.Rad2Deg * 2;
				float aspect = intrinsics.FOVPort.LeftTan / intrinsics.FOVPort.UpTan;
				directCompositionCamera.fieldOfView = fovY;
				directCompositionCamera.aspect = aspect;
				if (cameraInTrackingSpace)
				{
					OVRPose trackingSpacePose = ComputeCameraTrackingSpacePose(extrinsics);
					directCompositionCamera.transform.FromOVRPose(trackingSpacePose, true);
				}
				else
				{
					OVRPose worldSpacePose = ComputeCameraWorldSpacePose(extrinsics, mainCamera);
					directCompositionCamera.transform.FromOVRPose(worldSpacePose);
				}
			}
			else
			{
				Debug.LogWarning("Failed to get external camera information");
			}
		}

		if (hasCameraDeviceOpened)
		{
			if (boundaryMeshMaskTexture == null || boundaryMeshMaskTexture.width != Screen.width || boundaryMeshMaskTexture.height != Screen.height)
			{
				boundaryMeshMaskTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.R8);
				boundaryMeshMaskTexture.Create();
			}
			UpdateCameraFramePlaneObject(mainCamera, directCompositionCamera, configuration, boundaryMeshMaskTexture);
			directCompositionCamera.GetComponent<OVRCameraFrameCompositionManager>().boundaryMeshMaskTexture = boundaryMeshMaskTexture;
		}
	}

	public override void Cleanup()
	{
		base.Cleanup();

		OVRCompositionUtil.SafeDestroy(ref directCompositionCameraGameObject);
		directCompositionCamera = null;

		Debug.Log("DirectComposition deactivated");
	}
}

#endif
