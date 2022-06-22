//-----------------------------------------------------------------------
// <copyright file="TransformationUtility.cs" company="Google">
//
// Copyright 2018 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

// Modifications copyright © 2020 Unity Technologies ApS

#if AR_FOUNDATION_PRESENT || PACKAGE_DOCS_GENERATION

using System;
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace UnityEngine.XR.Interaction.Toolkit.AR
{
    public static partial class GestureTransformationUtility
    {
        /// <summary>
        /// Cast a ray from a point in screen space against trackables, i.e., detected features such as planes.
        /// Can optionally fallback to hit test against Colliders in the loaded Scenes when no trackables were hit.
        /// </summary>
        /// <param name="screenPoint">The point, in device screen pixels, from which to cast.</param>
        /// <param name="hitResults">Contents are replaced with the ray cast results, if successful.</param>
        /// <param name="sessionOrigin">The <see cref="ARSessionOrigin"/> used for ray casting.</param>
        /// <param name="trackableTypes">(Optional) The types of trackables to cast against.</param>
        /// <param name="fallbackLayerMask">(Optional) The <see cref="LayerMask"/> that Unity uses during an additional ray cast when no trackables are hit.
        /// Defaults to Nothing which skips the fallback ray cast.</param>
        /// <returns>Returns <see langword="true"/> if the ray cast hit a trackable in the <paramref name="trackableTypes"/> or if the fallback ray cast hit.
        /// Otherwise, returns <see langword="false"/>.</returns>
        [Obsolete("Raycast with the ARSessionOrigin parameter has been deprecated. Use Raycast with the XROrigin parameter instead.")]
        public static bool Raycast(
            Vector2 screenPoint,
            List<ARRaycastHit> hitResults,
            ARSessionOrigin sessionOrigin,
            TrackableType trackableTypes = TrackableType.All,
            int fallbackLayerMask = 0)
        {
            if ((sessionOrigin != null || TryGetSessionOrigin(out sessionOrigin)) &&
                TryGetTrackableManager(sessionOrigin, out ARRaycastManager raycastManager) &&
                raycastManager.Raycast(screenPoint, hitResults, trackableTypes))
            {
                return true;
            }

            // No hits on trackables, try debug planes
            hitResults.Clear();
            const TrackableType hitType = TrackableType.PlaneWithinPolygon;
            if (fallbackLayerMask == 0 || (trackableTypes & hitType) == 0)
                return false;

            var camera = sessionOrigin != null ? sessionOrigin.camera : Camera.main;
            if (camera == null)
                return false;

            var ray = camera.ScreenPointToRay(screenPoint);
            if (Physics.Raycast(ray, out var hit, Mathf.Infinity, fallbackLayerMask))
            {
                hitResults.Add(new ARRaycastHit(
                    new XRRaycastHit(
                        TrackableId.invalidId,
                        new Pose(hit.point, Quaternion.LookRotation(Vector3.forward, hit.normal)),
                        hit.distance,
                        hitType),
                    hit.distance,
                    sessionOrigin != null ? sessionOrigin.transform : hit.collider.transform));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Calculates the best position to place an object in AR based on screen position.
        /// Could be used for tapping a location on the screen, dragging an object, or using a fixed
        /// cursor in the center of the screen for placing and moving objects.
        /// </summary>
        /// <returns>Returns the best placement position.</returns>
        /// <param name="currentAnchorPosition">Position of the parent anchor, i.e., where the
        /// object is before translation starts.</param>
        /// <param name="screenPosition">Location on the screen in pixels to place the object at.</param>
        /// <param name="groundingPlaneHeight">The starting height of the plane to place the object on.</param>
        /// <param name="hoverOffset">How much should the object hover above the groundingPlane
        /// before it has been placed.</param>
        /// <param name="maxTranslationDistance">The maximum distance allowed to translate the object.</param>
        /// <param name="gestureTranslationMode">The translation mode, indicating the plane types allowed.
        /// </param>
        /// <param name="sessionOrigin">The <see cref="ARSessionOrigin"/> used for ray casting.</param>
        /// <param name="trackableTypes">(Optional) The types of trackables to cast against.</param>
        /// <param name="fallbackLayerMask">(Optional) The <see cref="LayerMask"/> that Unity uses during
        /// an additional ray cast when no trackables are hit. Defaults to Nothing which skips the fallback ray cast.</param>
        /// <remarks>
        /// Unity places objects along the x/z of the grounding plane. When placed on an AR plane
        /// below the grounding plane, the object will drop straight down onto it in world space.
        /// This prevents the object from being pushed deeper into the scene when moving from a
        /// higher plane to a lower plane. When moving from a lower plane to a higher plane, this
        /// function returns a new groundingPlane to replace the old one.
        /// </remarks>
        [Obsolete("GetBestPlacementPosition with the ARSessionOrigin parameter has been deprecated. Use GetBestPlacementPosition with the XROrigin parameter instead.")]
        public static Placement GetBestPlacementPosition(
            Vector3 currentAnchorPosition,
            Vector2 screenPosition,
            float groundingPlaneHeight,
            float hoverOffset,
            float maxTranslationDistance,
            GestureTranslationMode gestureTranslationMode,
            ARSessionOrigin sessionOrigin,
            TrackableType trackableTypes = TrackableType.PlaneWithinPolygon,
            int fallbackLayerMask = 0)
        {
            var result = new Placement();

            if (sessionOrigin == null)
                TryGetSessionOrigin(out sessionOrigin);

            var camera = sessionOrigin != null ? sessionOrigin.camera : Camera.main;
            if (camera == null)
                return result;

            var cameraTransform = camera.transform;

            result.updatedGroundingPlaneHeight = groundingPlaneHeight;

            // Get the angle between the camera and the object's down direction.
            var angle = 90f - Vector3.Angle(cameraTransform.forward, Vector3.down);

            var touchOffsetRatio = Mathf.Clamp01(angle / 90f);
            var screenTouchOffset = touchOffsetRatio * k_MaxScreenTouchOffset;
            screenPosition.y += GestureTouchesUtility.InchesToPixels(screenTouchOffset);

            var hoverRatio = Mathf.Clamp01(angle / 45f);
            hoverOffset *= hoverRatio;

            var distance = (cameraTransform.position - currentAnchorPosition).magnitude;
            var distanceHoverRatio = Mathf.Clamp01(distance / k_HoverDistanceThreshold);
            hoverOffset *= distanceHoverRatio;

            // The best estimate of the point in the plane where the object will be placed:
            Vector3 groundingPoint;

            // Get the ray to cast into the scene from the perspective of the camera.
            if (Raycast(screenPosition, s_Hits, sessionOrigin, trackableTypes, fallbackLayerMask))
            {
                if (!TryGetTrackableManager(sessionOrigin, out ARPlaneManager planeManager))
                    return result;

                var firstHit = s_Hits[0];
                var plane = planeManager.GetPlane(firstHit.trackableId);
                if (plane == null || IsPlaneTypeAllowed(gestureTranslationMode, plane.alignment))
                {
                    // Avoid detecting the back of existing planes.
                    if (Vector3.Dot(cameraTransform.position - firstHit.pose.position,
                                    firstHit.pose.rotation * Vector3.up) < 0f)
                        return result;

                    // Don't allow hovering for vertical or horizontal downward facing planes.
                    if (plane == null ||
                        plane.alignment == PlaneAlignment.Vertical ||
                        plane.alignment == PlaneAlignment.HorizontalDown ||
                        plane.alignment == PlaneAlignment.HorizontalUp)
                    {
                        groundingPoint = LimitTranslation(
                            firstHit.pose.position, currentAnchorPosition, maxTranslationDistance);

                        if (plane != null)
                        {
                            result.placementPlane = plane;
                            result.hasPlane = true;
                        }

                        result.hasPlacementPosition = true;
                        result.placementPosition = groundingPoint;
                        result.hasHoveringPosition = true;
                        result.hoveringPosition = groundingPoint;
                        result.updatedGroundingPlaneHeight = groundingPoint.y;
                        result.placementRotation = firstHit.pose.rotation;
                        return result;
                    }
                }
                else
                {
                    // Plane type not allowed.
                    return result;
                }
            }

            // Return early if the camera is pointing upwards.
            if (angle < 0f)
            {
                return result;
            }

            // If the grounding point is lower than the current grounding plane height, or if the
            // ray cast did not return a hit, then we extend the grounding plane to infinity, and do
            // a new ray cast into the scene from the perspective of the camera.
            var cameraRay = camera.ScreenPointToRay(screenPosition);
            var groundingPlane =
                new Plane(Vector3.up, new Vector3(0f, groundingPlaneHeight, 0f));

            // Find the hovering position by casting from the camera onto the grounding plane
            // and offsetting the result by the hover offset.
            if (groundingPlane.Raycast(cameraRay, out var enter))
            {
                groundingPoint = LimitTranslation(
                    cameraRay.GetPoint(enter), currentAnchorPosition, maxTranslationDistance);

                result.hasHoveringPosition = true;
                result.hoveringPosition = groundingPoint + (Vector3.up * hoverOffset);
            }
            else
            {
                // If we can't successfully cast onto the groundingPlane, just return early.
                return result;
            }

            return result;
        }

        /// <summary>
        /// Cast a ray from a point in screen space against trackables, i.e., detected features such as planes.
        /// </summary>
        /// <param name="screenPoint">The point, in device screen pixels, from which to cast.</param>
        /// <param name="hitResults">Contents are replaced with the ray cast results, if successful.</param>
        /// <param name="trackableTypes">(Optional) The types of trackables to cast against.</param>
        /// <returns>Returns <see langword="true"/> if the ray cast hit a trackable in the <paramref name="trackableTypes"/>.
        /// Otherwise, returns <see langword="false"/>.</returns>
        [Obsolete("Raycast has been deprecated. Use Raycast with updated signature instead.")]
        public static bool Raycast(Vector2 screenPoint, List<ARRaycastHit> hitResults, TrackableType trackableTypes = TrackableType.All)
        {
            // For backwards compatibility, use the TestPlanes layer value from the AR examples project.
            const int fallbackLayerMask = 1 << 9;
            return Raycast(screenPoint, hitResults, (ARSessionOrigin)null, trackableTypes, fallbackLayerMask);
        }

        /// <summary>
        /// Calculates the best position to place an object in AR based on screen position.
        /// Could be used for tapping a location on the screen, dragging an object, or using a fixed
        /// cursor in the center of the screen for placing and moving objects.
        /// </summary>
        /// <returns>Returns the best placement position.</returns>
        /// <param name="currentAnchorPosition">Position of the parent anchor, i.e., where the
        /// object is before translation starts.</param>
        /// <param name="screenPos">Location on the screen in pixels to place the object at.</param>
        /// <param name="groundingPlaneHeight">The starting height of the plane to place the object on.</param>
        /// <param name="hoverOffset">How much should the object hover above the groundingPlane
        /// before it has been placed.</param>
        /// <param name="maxTranslationDistance">The maximum distance allowed to translate the object.</param>
        /// <param name="gestureTranslationMode">The translation mode, indicating the plane types allowed.
        /// </param>
        /// <remarks>
        /// Unity places objects along the x/z of the grounding plane. When placed on an AR plane
        /// below the grounding plane, the object will drop straight down onto it in world space.
        /// This prevents the object from being pushed deeper into the scene when moving from a
        /// higher plane to a lower plane. When moving from a lower plane to a higher plane, this
        /// function returns a new groundingPlane to replace the old one.
        /// </remarks>
        [Obsolete("GetBestPlacementPosition has been deprecated. Use GetBestPlacementPosition with updated signature instead.")]
        public static Placement GetBestPlacementPosition(
            Vector3 currentAnchorPosition,
            Vector2 screenPos,
            float groundingPlaneHeight,
            float hoverOffset,
            float maxTranslationDistance,
            GestureTranslationMode gestureTranslationMode)
        {
            // For backwards compatibility, use the TestPlanes layer value from the AR examples project.
            const int fallbackLayerMask = 1 << 9;
            return GetBestPlacementPosition(currentAnchorPosition,
                screenPos,
                groundingPlaneHeight,
                hoverOffset,
                maxTranslationDistance,
                gestureTranslationMode,
                (ARSessionOrigin)null,
                fallbackLayerMask: fallbackLayerMask);
        }

        public partial struct Placement
        {
#pragma warning disable IDE1006 // Naming Styles
            /// <inheritdoc cref="hasHoveringPosition"/>
            [Obsolete("HasHoveringPosition has been deprecated. Use hasHoveringPosition instead. (UnityUpgradable) -> hasHoveringPosition")]
            public bool HasHoveringPosition
            {
                get => hasHoveringPosition;
                set => hasHoveringPosition = value;
            }

            /// <inheritdoc cref="hoveringPosition"/>
            [Obsolete("HoveringPosition has been deprecated. Use hoveringPosition instead. (UnityUpgradable) -> hoveringPosition")]
            public Vector3 HoveringPosition
            {
                get => hoveringPosition;
                set => hoveringPosition = value;
            }

            /// <inheritdoc cref="hasPlacementPosition"/>
            [Obsolete("HasPlacementPosition has been deprecated. Use hasPlacementPosition instead. (UnityUpgradable) -> hasPlacementPosition")]
            public bool HasPlacementPosition
            {
                get => hasPlacementPosition;
                set => hasPlacementPosition = value;
            }

            /// <inheritdoc cref="placementPosition"/>
            [Obsolete("PlacementPosition has been deprecated. Use placementPosition instead. (UnityUpgradable) -> placementPosition")]
            public Vector3 PlacementPosition
            {
                get => placementPosition;
                set => placementPosition = value;
            }

            /// <inheritdoc cref="placementRotation"/>
            [Obsolete("PlacementRotation has been deprecated. Use placementRotation instead. (UnityUpgradable) -> placementRotation")]
            public Quaternion PlacementRotation
            {
                get => placementRotation;
                set => placementRotation = value;
            }

            /// <inheritdoc cref="hasPlane"/>
            [Obsolete("HasPlane has been deprecated. Use hasPlane instead. (UnityUpgradable) -> hasPlane")]
            public bool HasPlane
            {
                get => hasPlane;
                set => hasPlane = value;
            }

            /// <inheritdoc cref="placementPlane"/>
            [Obsolete("PlacementPlane has been deprecated. Use placementPlane instead. (UnityUpgradable) -> placementPlane")]
            public ARPlane PlacementPlane
            {
                get => placementPlane;
                set => placementPlane = value;
            }

            /// <inheritdoc cref="updatedGroundingPlaneHeight"/>
            [Obsolete("UpdatedGroundingPlaneHeight has been deprecated. Use updatedGroundingPlaneHeight instead. (UnityUpgradable) -> updatedGroundingPlaneHeight")]
            public float UpdatedGroundingPlaneHeight
            {
                get => updatedGroundingPlaneHeight;
                set => updatedGroundingPlaneHeight = value;
            }
#pragma warning restore IDE1006 // Naming Styles
        }
    }
}

#endif
