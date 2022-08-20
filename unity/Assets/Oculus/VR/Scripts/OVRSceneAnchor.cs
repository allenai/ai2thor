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

/// <summary>
/// Represents a scene anchor.
/// </summary>
/// <remarks>
/// A scene anchor is a type of anchor that is provided by the system. It represents an item in the physical
/// environment, such as a plane or volume. Scene anchors are created by the <see cref="OVRSceneManager"/>.
/// </remarks>
/// <seealso cref="OVRScenePlane"/>
/// <seealso cref="OVRSceneVolume"/>
/// <seealso cref="OVRSemanticClassification"/>
[DisallowMultipleComponent]
public sealed class OVRSceneAnchor : MonoBehaviour
{
	/// <summary>
	/// The runtime handle of this scene anchor.
	/// </summary>
	public OVRSpace Space { get; private set; }

	/// <summary>
	/// The universally unique identifier for this scene anchor.
	/// </summary>
	public Guid Uuid { get; private set; }

	private static readonly Quaternion RotateY180 = Quaternion.Euler(0, 180, 0);

	private bool IsComponentEnabled(OVRPlugin.SpaceComponentType spaceComponentType) =>
		OVRPlugin.GetSpaceComponentStatus(Space, spaceComponentType, out var componentEnabled, out _)
		&& componentEnabled;

	private void SyncComponent<T>(OVRPlugin.SpaceComponentType spaceComponentType)
		where T : MonoBehaviour, IOVRSceneComponent
	{
		if (!IsComponentEnabled(spaceComponentType)) return;

		var component = GetComponent<T>();
		if (component)
		{
			// If the component already exists, then it means it was added before this component was valid, so we need
			// to initialize it.
			component.Initialize();
		}
		else
		{
			gameObject.AddComponent<T>();
		}
	}

	internal void Initialize(OVRSpace space, Guid uuid)
	{
		if (Space.Valid)
			throw new InvalidOperationException($"[{uuid}] {nameof(OVRSceneAnchor)} has already been initialized.");

		if (!space.Valid)
			throw new ArgumentException($"[{uuid}] {nameof(space)} must be valid.", nameof(space));

		Space = space;
		Uuid = uuid;

		if (!IsComponentEnabled(OVRPlugin.SpaceComponentType.Locatable))
		{
			OVRSceneManager.Development.LogError(nameof(OVRSceneAnchor),
				$"[{uuid}] Is missing the {nameof(OVRPlugin.SpaceComponentType.Locatable)} component.");
		}

		// Generally, we want to set the transform as soon as possible, but there is a valid use case where we want to
		// disable this component as soon as its added to override the transform.
		if (enabled)
		{
			var updateTransformSucceeded = TryUpdateTransform();

			// This should work; so add some development-only logs so we know if something is wrong here.
			if (updateTransformSucceeded)
			{
				OVRSceneManager.Development.Log(nameof(OVRSceneAnchor), $"[{uuid}] Initial transform set.");
			}
			else
			{
				OVRSceneManager.Development.LogWarning(nameof(OVRSceneAnchor),
					$"[{uuid}] {nameof(OVRPlugin.TryLocateSpace)} failed. The entity may have the wrong initial transform.");
			}
		}

		SyncComponent<OVRSemanticClassification>(OVRPlugin.SpaceComponentType.SemanticLabels);
		SyncComponent<OVRSceneVolume>(OVRPlugin.SpaceComponentType.Bounded3D);
		SyncComponent<OVRScenePlane>(OVRPlugin.SpaceComponentType.Bounded2D);
	}

	/// <summary>
	/// Initializes this scene anchor from an existing scene anchor.
	/// </summary>
	/// <param name="other">An existing <see cref="OVRSceneAnchor"/> from which to initialize this scene anchor.</param>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="other"/> is `null`.</exception>
	/// <exception cref="InvalidOperationException">Thrown if this <see cref="OVRSceneAnchor"/> is already associated with a scene anchor.</exception>
	public void InitializeFrom(OVRSceneAnchor other)
	{
		if (other == null)
			throw new ArgumentNullException(nameof(other));

		Initialize(other.Space, other.Uuid);
	}

	private bool TryUpdateTransform()
	{
		if (!Space.Valid) return false;

		if (!OVRPlugin.TryLocateSpace(Space, OVRPlugin.GetTrackingOriginType(), out var pose)) return false;

		// NOTE: This transformation performs the following steps:
		// 1. Flip Z to convert from OpenXR's right-handed to Unity's left-handed coordinate system.
		//    OpenXR             Unity
		//       | y          y |  / z
		//       |              | /
		//       +----> x       +----> x
		//      /
		//    z/ (normal)
		//
		// 2. (1) means that Z now points in the opposite direction from OpenXR. However, the design is such that a
		//    plane's normal should coincide with +Z, so we rotate 180 degrees around the +Y axis to make Z now point
		//    in the intended direction.
		//    OpenXR           Unity
		//       | y           y |
		//       |               |
		//       +---->  x  <----+
		//      /               /
		//    z/             z / (normal)
		//
		// 3. Convert from tracking space to world space.
		var worldSpacePose = new OVRPose
		{
			position = pose.Position.FromFlippedZVector3f(),
			orientation = pose.Orientation.FromFlippedZQuatf() * RotateY180
		}.ToWorldSpacePose(Camera.main);
		transform.SetPositionAndRotation(worldSpacePose.position, worldSpacePose.orientation);
		return true;
	}

	private void Update() => TryUpdateTransform();
}

internal interface IOVRSceneComponent
{
	void Initialize();
}
