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

using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Unity.XR.CoreUtils;

namespace UnityEngine.XR.Interaction.Toolkit.AR
{
    /// <summary>
    /// Provides helper functions for common functionality to transform objects in AR.
    /// </summary>
    public static partial class GestureTransformationUtility
    {
        /// <summary>
        /// Represents the alignment of a plane where translation is allowed.
        /// </summary>
        /// <seealso cref="GetBestPlacementPosition(Vector3, Vector2, float, float, float, GestureTranslationMode, XROrigin, TrackableType, int)"/>
        /// <seealso cref="PlaneAlignment"/>
        public enum GestureTranslationMode
        {
            /// <summary>
            /// Allow translation when the plane is horizontal.
            /// </summary>
            Horizontal,

            /// <summary>
            /// Allow translation when the plane is vertical.
            /// </summary>
            Vertical,

            /// <summary>
            /// Allow translation on any plane.
            /// </summary>
            Any,
        }

        /// <summary>
        /// Max amount (inches) to offset the screen touch in <see cref="GetBestPlacementPosition(Vector3, Vector2, float, float, float, GestureTranslationMode, XROrigin, TrackableType, int)"/>.
        /// The actual amount depends on the angle of the camera relative.
        /// The further downward the camera is angled, the more the screen touch is offset.
        /// </summary>
        const float k_MaxScreenTouchOffset = 0.4f;

        /// <summary>
        /// In <see cref="GetBestPlacementPosition(Vector3, Vector2, float, float, float, GestureTranslationMode, XROrigin, TrackableType, int)"/>, when the camera is closer than this value to the object,
        /// reduce how much the object hovers.
        /// </summary>
        const float k_HoverDistanceThreshold = 1f;

        static XROrigin s_XROrigin;
        static ARSessionOrigin s_ARSessionOrigin;
        static ARRaycastManager s_ARRaycastManager;
        static ARPlaneManager s_ARPlaneManager;

        static readonly List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

        static bool TryGetTrackableManager([CanBeNull] XROrigin sessionOrigin, out ARRaycastManager raycastManager) =>
            TryGetTrackableManager(sessionOrigin, ref s_ARRaycastManager, out raycastManager);

        static bool TryGetTrackableManager([CanBeNull] XROrigin sessionOrigin, out ARPlaneManager planeManager) =>
            TryGetTrackableManager(sessionOrigin, ref s_ARPlaneManager, out planeManager);

        static bool TryGetTrackableManager<T>([CanBeNull] XROrigin sessionOrigin, ref T cachedManager, out T manager)
        {
            // This method is used to get the Trackable Manager component on the XROrigin GameObject
            // by caching the manager component from the most recently used XROrigin.
            // There is typically only one XROrigin, so this serves as a simple cache to avoid
            // doing a GetComponent call to get the manager component each time.
            if (sessionOrigin == null)
            {
                if (s_XROrigin == null)
                {
                    s_XROrigin = Object.FindObjectOfType<XROrigin>();
                    cachedManager = default;
                    if (s_XROrigin == null)
                    {
                        Debug.LogWarning($"Could not find {nameof(XROrigin)} in scene.");
                        manager = default;
                        return false;
                    }
                }
            }
            else if (sessionOrigin != s_XROrigin)
            {
                s_XROrigin = sessionOrigin;
                cachedManager = default;
            }

            // The cached Trackable Managers are associated with an XROrigin.
            // Update and use cached version
            var found = cachedManager != null || s_XROrigin.TryGetComponent(out cachedManager);
            manager = cachedManager;
            return found;
        }

        static bool TryGetSessionOrigin(out XROrigin sessionOrigin)
        {
            if (s_XROrigin == null)
            {
                s_XROrigin = Object.FindObjectOfType<XROrigin>();
                if (s_XROrigin == null)
                {
                    Debug.LogWarning($"Could not find {nameof(XROrigin)} in scene.");
                    sessionOrigin = s_XROrigin;
                    return false;
                }
            }

            sessionOrigin = s_XROrigin;
            return true;
        }

        #region Deprecated ARSessionOrigin overloads

        static bool TryGetTrackableManager([CanBeNull] ARSessionOrigin sessionOrigin, out ARRaycastManager raycastManager) =>
            TryGetTrackableManager(sessionOrigin, ref s_ARRaycastManager, out raycastManager);

        static bool TryGetTrackableManager([CanBeNull] ARSessionOrigin sessionOrigin, out ARPlaneManager planeManager) =>
            TryGetTrackableManager(sessionOrigin, ref s_ARPlaneManager, out planeManager);

        static bool TryGetTrackableManager<T>([CanBeNull] ARSessionOrigin sessionOrigin, ref T cachedManager, out T manager)
        {
            // This method is used to get the Trackable Manager component on the ARSessionOrigin GameObject
            // by caching the manager component from the most recently used ARSessionOrigin.
            // There is typically only one ARSessionOrigin, so this serves as a simple cache to avoid
            // doing a GetComponent call to get the manager component each time.
            if (sessionOrigin == null)
            {
                if (s_ARSessionOrigin == null)
                {
                    s_ARSessionOrigin = Object.FindObjectOfType<ARSessionOrigin>();
                    cachedManager = default;
                    if (s_ARSessionOrigin == null)
                    {
                        Debug.LogWarning($"Could not find {nameof(ARSessionOrigin)} in scene.");
                        manager = default;
                        return false;
                    }
                }
            }
            else if (sessionOrigin != s_ARSessionOrigin)
            {
                s_ARSessionOrigin = sessionOrigin;
                cachedManager = default;
            }

            // The cached Trackable Managers are associated with an ARSessionOrigin.
            // Update and use cached version
            var found = cachedManager != null || s_ARSessionOrigin.TryGetComponent(out cachedManager);
            manager = cachedManager;
            return found;
        }

        static bool TryGetSessionOrigin(out ARSessionOrigin sessionOrigin)
        {
            if (s_ARSessionOrigin == null)
            {
                s_ARSessionOrigin = Object.FindObjectOfType<ARSessionOrigin>();
                if (s_ARSessionOrigin == null)
                {
                    Debug.LogWarning($"Could not find {nameof(ARSessionOrigin)} in scene.");
                    sessionOrigin = s_ARSessionOrigin;
                    return false;
                }
            }

            sessionOrigin = s_ARSessionOrigin;
            return true;
        }

        #endregion

        /// <summary>
        /// Cast a ray from a point in screen space against trackables, i.e., detected features such as planes.
        /// Can optionally fallback to hit test against Colliders in the loaded Scenes when no trackables were hit.
        /// </summary>
        /// <param name="screenPoint">The point, in device screen pixels, from which to cast.</param>
        /// <param name="hitResults">Contents are replaced with the ray cast results, if successful.</param>
        /// <param name="sessionOrigin">The <see cref="XROrigin"/> used for ray casting.</param>
        /// <param name="trackableTypes">(Optional) The types of trackables to cast against.</param>
        /// <param name="fallbackLayerMask">(Optional) The <see cref="LayerMask"/> that Unity uses during an additional ray cast when no trackables are hit.
        /// Defaults to Nothing which skips the fallback ray cast.</param>
        /// <returns>Returns <see langword="true"/> if the ray cast hit a trackable in the <paramref name="trackableTypes"/> or if the fallback ray cast hit.
        /// Otherwise, returns <see langword="false"/>.</returns>
        public static bool Raycast(
            Vector2 screenPoint,
            List<ARRaycastHit> hitResults,
            XROrigin sessionOrigin,
#if AR_FOUNDATION_4_2_OR_NEWER
            TrackableType trackableTypes = TrackableType.AllTypes,
#else
            TrackableType trackableTypes = TrackableType.All,
#endif
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

            var camera = sessionOrigin != null ? sessionOrigin.Camera : Camera.main;
            if (camera == null)
                return false;

            var ray = camera.ScreenPointToRay(screenPoint);
            if (Physics.Raycast(ray, out var hit, Mathf.Infinity, fallbackLayerMask))
            {
                var transform = sessionOrigin != null ? sessionOrigin.transform : hit.collider.transform;
                hitResults.Add(new ARRaycastHit(
                    new XRRaycastHit(
                        TrackableId.invalidId,
                        new Pose(hit.point, Quaternion.LookRotation(Vector3.forward, hit.normal)),
                        hit.distance,
                        hitType),
                    hit.distance,
#if AR_FOUNDATION_4_1_OR_NEWER
                    transform, null));
#else
                    transform));
#endif
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
        /// <param name="sessionOrigin">The <see cref="XROrigin"/> used for ray casting.</param>
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
        public static Placement GetBestPlacementPosition(
            Vector3 currentAnchorPosition,
            Vector2 screenPosition,
            float groundingPlaneHeight,
            float hoverOffset,
            float maxTranslationDistance,
            GestureTranslationMode gestureTranslationMode,
            XROrigin sessionOrigin,
            TrackableType trackableTypes = TrackableType.PlaneWithinPolygon,
            int fallbackLayerMask = 0)
        {
            var result = new Placement();

            if (sessionOrigin == null)
                TryGetSessionOrigin(out sessionOrigin);

            var camera = sessionOrigin != null ? sessionOrigin.Camera : Camera.main;
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
        /// Limits the translation to the maximum distance allowed.
        /// </summary>
        /// <returns>Returns the new target position, limited so that the object does not translate more
        /// than the maximum allowed distance.</returns>
        /// <param name="desiredPosition">Desired position.</param>
        /// <param name="currentPosition">Current position.</param>
        /// <param name="maxTranslationDistance">Max translation distance.</param>
        static Vector3 LimitTranslation(Vector3 desiredPosition, Vector3 currentPosition, float maxTranslationDistance)
        {
            if ((desiredPosition - currentPosition).sqrMagnitude > Mathf.Pow(maxTranslationDistance, 2f))
            {
                return currentPosition + (
                    (desiredPosition - currentPosition).normalized * maxTranslationDistance);
            }

            return desiredPosition;
        }

        static bool IsPlaneTypeAllowed(GestureTranslationMode gestureTranslationMode, PlaneAlignment planeAlignment)
        {
            if (gestureTranslationMode == GestureTranslationMode.Any)
            {
                return true;
            }

            if (gestureTranslationMode == GestureTranslationMode.Horizontal &&
                (planeAlignment == PlaneAlignment.HorizontalDown ||
                    planeAlignment == PlaneAlignment.HorizontalUp))
            {
                return true;
            }

            if (gestureTranslationMode == GestureTranslationMode.Vertical &&
                planeAlignment == PlaneAlignment.Vertical)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Result of the function <see cref="GetBestPlacementPosition(Vector3, Vector2, float, float, float, GestureTranslationMode, XROrigin, TrackableType, int)"/>
        /// that indicates if a placement position
        /// was found and information about the placement position.
        /// </summary>
        public partial struct Placement
        {
            /// <summary>
            /// <see langword="true"/> if this Placement has a valid <see cref="hoveringPosition"/> value, otherwise <see langword="false"/>.
            /// </summary>
            /// <seealso cref="hoveringPosition"/>
            public bool hasHoveringPosition { get; set; }

            /// <summary>
            /// The position that the object should be displayed at before the placement has been
            /// confirmed.
            /// </summary>
            /// <seealso cref="hasHoveringPosition"/>
            public Vector3 hoveringPosition { get; set; }

            /// <summary>
            /// <see langword="true"/> if this Placement has a valid <see cref="placementPosition"/> value, otherwise <see langword="false"/>.
            /// </summary>
            /// <seealso cref="placementPosition"/>
            public bool hasPlacementPosition { get; set; }

            /// <summary>
            /// The resulting position that the object should be placed at.
            /// </summary>
            /// <seealso cref="hasPlacementPosition"/>
            public Vector3 placementPosition { get; set; }

            /// <summary>
            /// The resulting rotation that the object should have.
            /// </summary>
            public Quaternion placementRotation { get; set; }

            /// <summary>
            /// <see langword="true"/> if this Placement has a <see cref="placementPlane"/>, otherwise <see langword="false"/>.
            /// </summary>
            /// <seealso cref="placementPlane"/>
            public bool hasPlane { get; set; }

            /// <summary>
            /// The <a href="https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@4.1/api/UnityEngine.XR.ARFoundation.ARPlane.html">ARPlane</a>
            /// that the object is being placed on.
            /// </summary>
            /// <seealso cref="hasPlane"/>
            public ARPlane placementPlane { get; set; }

            /// <summary>
            /// The resulting starting height of the plane that the object is being placed along.
            /// </summary>
            public float updatedGroundingPlaneHeight { get; set; }
        }
    }
}

#endif
