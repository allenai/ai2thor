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

/// <summary>
/// A <see cref="OVRSceneAnchor"/> that has a 2D bounds associated with it.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(OVRSceneAnchor))]
public class OVRScenePlane : MonoBehaviour, IOVRSceneComponent
{
	/// <summary>
	/// The plane's width (in the local X-direction), in meters.
	/// </summary>
	public float Width { get; private set; }

	/// <summary>
	/// The plane's height (in the local Y-direction), in meters.
	/// </summary>
	public float Height { get; private set; }

	/// <summary>
	/// The dimensions of the plane.
	/// </summary>
	/// <remarks>
	/// This property corresponds to a Vector whose components are
	/// (<see cref="Width"/>, <see cref="Height"/>).
	/// </remarks>
	public Vector2 Dimensions => new Vector2(Width, Height);

	private static void SetChildScale(Transform parentTransform, float width, float height)
	{
		for (var i = 0; i < parentTransform.childCount; i++)
		{
			var child = parentTransform.GetChild(i);
			var scale = new Vector3(width, height, child.localScale.z);
			child.localScale = scale;
		}
	}

	private void Awake()
	{
		if (GetComponent<OVRSceneAnchor>().Space.Valid)
		{
			((IOVRSceneComponent)this).Initialize();
		}
	}

	void IOVRSceneComponent.Initialize()
	{
		if (OVRPlugin.GetSpaceBoundingBox2D(GetComponent<OVRSceneAnchor>().Space, out var rect))
		{
			Width = rect.Size.w;
			Height = rect.Size.h;

			// The volume component will also set the scale
			if (!GetComponent<OVRSceneVolume>())
			{
				SetChildScale(transform, Width, Height);
			}
		}
		else
		{
			OVRSceneManager.Development.LogError(nameof(OVRScenePlane),
				$"[{GetComponent<OVRSceneAnchor>().Uuid}] Could not obtain 2D bounds.");
		}
	}
}
