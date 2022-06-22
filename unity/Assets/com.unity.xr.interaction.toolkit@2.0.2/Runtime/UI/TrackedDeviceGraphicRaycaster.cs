using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UnityEngine.XR.Interaction.Toolkit.UI
{
    /// <summary>
    /// Custom implementation of <see cref="GraphicRaycaster"/> for XR Interaction Toolkit.
    /// This behavior is used to ray cast against a <see cref="Canvas"/>. The Raycaster looks
    /// at all Graphics on the canvas and determines if any of them have been hit by a ray
    /// from a tracked device.
    /// </summary>
    [AddComponentMenu("Event/Tracked Device Graphic Raycaster", 11)]
    [HelpURL(XRHelpURLConstants.k_TrackedDeviceGraphicRaycaster)]
    public class TrackedDeviceGraphicRaycaster : BaseRaycaster
    {
        const int k_MaxRaycastHits = 10;

        readonly struct RaycastHitData
        {
            public RaycastHitData(Graphic graphic, Vector3 worldHitPosition, Vector2 screenPosition, float distance, int displayIndex)
            {
                this.graphic = graphic;
                this.worldHitPosition = worldHitPosition;
                this.screenPosition = screenPosition;
                this.distance = distance;
                this.displayIndex = displayIndex;
            }

            public Graphic graphic { get; }
            public Vector3 worldHitPosition { get; }
            public Vector2 screenPosition { get; }
            public float distance { get; }
            public int displayIndex { get; }
        }

        /// <summary>
        /// Compares ray cast hits by graphic depth, to sort in descending order.
        /// </summary>
        sealed class RaycastHitComparer : IComparer<RaycastHitData>
        {
            public int Compare(RaycastHitData a, RaycastHitData b)
                => b.graphic.depth.CompareTo(a.graphic.depth);
        }

        [SerializeField]
        [Tooltip("Whether Graphics facing away from the ray caster are checked for ray casts. Enable this to ignore backfacing Graphics.")]
        bool m_IgnoreReversedGraphics;

        /// <summary>
        /// Whether Graphics facing away from the ray caster are checked for ray casts.
        /// Enable this to ignore backfacing Graphics.
        /// </summary>
        public bool ignoreReversedGraphics
        {
            get => m_IgnoreReversedGraphics;
            set => m_IgnoreReversedGraphics = value;
        }

        [SerializeField]
        [Tooltip("Whether or not 2D occlusion is checked when performing ray casts. Enable to make Graphics be blocked by 2D objects that exist in front of it.")]
        bool m_CheckFor2DOcclusion;

        /// <summary>
        /// Whether or not 2D occlusion is checked when performing ray casts.
        /// Enable to make Graphics be blocked by 2D objects that exist in front of it.
        /// </summary>
        /// <remarks>
        /// This property has no effect when the project does not include the Physics 2D module.
        /// </remarks>
        public bool checkFor2DOcclusion
        {
            get => m_CheckFor2DOcclusion;
            set => m_CheckFor2DOcclusion = value;
        }

        [SerializeField]
        [Tooltip("Whether or not 3D occlusion is checked when performing ray casts. Enable to make Graphics be blocked by 3D objects that exist in front of it.")]
        bool m_CheckFor3DOcclusion;

        /// <summary>
        /// Whether or not 3D occlusion is checked when performing ray casts.
        /// Enable to make Graphics be blocked by 3D objects that exist in front of it.
        /// </summary>
        public bool checkFor3DOcclusion
        {
            get => m_CheckFor3DOcclusion;
            set => m_CheckFor3DOcclusion = value;
        }

        [SerializeField]
        [Tooltip("The layers of objects that are checked to determine if they block Graphic ray casts when checking for 2D or 3D occlusion.")]
        LayerMask m_BlockingMask = -1;

        /// <summary>
        /// The layers of objects that are checked to determine if they block Graphic ray casts
        /// when checking for 2D or 3D occlusion.
        /// </summary>
        public LayerMask blockingMask
        {
            get => m_BlockingMask;
            set => m_BlockingMask = value;
        }

        [SerializeField]
        [Tooltip("Specifies whether the ray cast should hit Triggers when checking for 3D occlusion.")]
        QueryTriggerInteraction m_RaycastTriggerInteraction = QueryTriggerInteraction.Ignore;

        /// <summary>
        /// Specifies whether the ray cast should hit Triggers when checking for 3D occlusion.
        /// </summary>
        public QueryTriggerInteraction raycastTriggerInteraction
        {
            get => m_RaycastTriggerInteraction;
            set => m_RaycastTriggerInteraction = value;
        }

        /// <summary>
        /// See [BaseRaycaster.eventCamera](xref:UnityEngine.EventSystems.BaseRaycaster.eventCamera).
        /// </summary>
        public override Camera eventCamera => canvas != null && canvas.worldCamera != null ? canvas.worldCamera : Camera.main;

        /// <summary>
        /// Performs a ray cast against objects within this Raycaster's domain.
        /// </summary>
        /// <param name="eventData">Data containing where and how to ray cast.</param>
        /// <param name="resultAppendList">The resultant hits from the ray cast.</param>
        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            if (eventData is TrackedDeviceEventData trackedEventData)
            {
                PerformRaycasts(trackedEventData, resultAppendList);
            }
        }

        Canvas m_Canvas;

        Canvas canvas
        {
            get
            {
                if (m_Canvas != null)
                    return m_Canvas;

                m_Canvas = GetComponent<Canvas>();
                return m_Canvas;
            }
        }

        bool m_HasWarnedEventCameraNull;

        readonly RaycastHit[] m_OcclusionHits3D = new RaycastHit[k_MaxRaycastHits];
#if PHYSICS2D_MODULE_PRESENT
        readonly RaycastHit2D[] m_OcclusionHits2D = new RaycastHit2D[k_MaxRaycastHits];
#endif
        static readonly RaycastHitComparer s_RaycastHitComparer = new RaycastHitComparer();

        static readonly Vector3[] s_Corners = new Vector3[4];

        // Use this list on each ray cast to avoid continually allocating.
        readonly List<RaycastHitData> m_RaycastResultsCache = new List<RaycastHitData>();

        [NonSerialized]
        static readonly List<RaycastHitData> s_SortedGraphics = new List<RaycastHitData>();

        static RaycastHit FindClosestHit(RaycastHit[] hits, int count)
        {
            var index = 0;
            var distance = float.MaxValue;
            for (var i = 0; i < count; i++)
            {
                if (hits[i].distance < distance)
                {
                    distance = hits[i].distance;
                    index = i;
                }
            }

            return hits[index];
        }

#if PHYSICS2D_MODULE_PRESENT
        static RaycastHit2D FindClosestHit(RaycastHit2D[] hits, int count)
        {
            var index = 0;
            var distance = float.MaxValue;
            for (var i = 0; i < count; i++)
            {
                if (hits[i].distance < distance)
                {
                    distance = hits[i].distance;
                    index = i;
                }
            }

            return hits[index];
        }
#endif

        void PerformRaycasts(TrackedDeviceEventData eventData, List<RaycastResult> resultAppendList)
        {
            if (canvas == null)
                return;

            // Property can call Camera.main, so cache the reference
            var currentEventCamera = eventCamera;
            if (currentEventCamera == null)
            {
                if (!m_HasWarnedEventCameraNull)
                {
                    Debug.LogWarning("Event Camera must be set on World Space Canvas to perform ray casts with tracked device." +
                        " UI events will not function correctly until it is set.",
                        this);
                    m_HasWarnedEventCameraNull = true;
                }

                return;
            }

            var rayPoints = eventData.rayPoints;
            var layerMask = eventData.layerMask;
            for (var i = 1; i < rayPoints.Count; i++)
            {
                var from = rayPoints[i - 1];
                var to = rayPoints[i];
                if (PerformRaycast(from, to, layerMask, currentEventCamera, resultAppendList))
                {
                    eventData.rayHitIndex = i;
                    break;
                }
            }
        }

        bool PerformRaycast(Vector3 from, Vector3 to, LayerMask layerMask, Camera currentEventCamera, List<RaycastResult> resultAppendList)
        {
            var hitSomething = false;

            var rayDistance = Vector3.Distance(to, from);
            var ray = new Ray(from, (to - from).normalized * rayDistance);

            var hitDistance = rayDistance;
            if (m_CheckFor3DOcclusion)
            {
                var hitCount = Physics.RaycastNonAlloc(ray, m_OcclusionHits3D, hitDistance, m_BlockingMask, m_RaycastTriggerInteraction);

                if (hitCount > 0)
                {
                    var hit = FindClosestHit(m_OcclusionHits3D, hitCount);
                    hitDistance = hit.distance;
                    hitSomething = true;
                }
            }

            if (m_CheckFor2DOcclusion)
            {
#if PHYSICS2D_MODULE_PRESENT
                var hitCount = Physics2D.RaycastNonAlloc(ray.origin, ray.direction, m_OcclusionHits2D, hitDistance, m_BlockingMask);

                if (hitCount > 0)
                {
                    var hit = FindClosestHit(m_OcclusionHits2D, hitCount);
                    hitDistance = hit.distance > hitDistance ? hitDistance : hit.distance;
                    hitSomething = true;
                }
#endif
            }

            m_RaycastResultsCache.Clear();
            SortedRaycastGraphics(canvas, ray, hitDistance, layerMask, currentEventCamera, m_RaycastResultsCache);

            // Now that we have a list of sorted hits, process any extra settings and filters.
            foreach (var hitData in m_RaycastResultsCache)
            {
                var validHit = true;

                var go = hitData.graphic.gameObject;
                if (m_IgnoreReversedGraphics)
                {
                    var forward = ray.direction;
                    var goDirection = go.transform.rotation * Vector3.forward;
                    validHit = Vector3.Dot(forward, goDirection) > 0;
                }

                validHit &= hitData.distance < hitDistance;

                if (validHit)
                {
                    var trans = go.transform;
                    var transForward = trans.forward;
                    var castResult = new RaycastResult
                    {
                        gameObject = go,
                        module = this,
                        distance = hitData.distance,
                        index = resultAppendList.Count,
                        depth = hitData.graphic.depth,
                        sortingLayer = canvas.sortingLayerID,
                        sortingOrder = canvas.sortingOrder,
                        worldPosition = hitData.worldHitPosition,
                        worldNormal = -transForward,
                        screenPosition = hitData.screenPosition,
                        displayIndex = hitData.displayIndex,
                    };
                    resultAppendList.Add(castResult);

                    hitSomething = true;
                }
            }

            return hitSomething;
        }

        static void SortedRaycastGraphics(Canvas canvas, Ray ray, float maxDistance, LayerMask layerMask, Camera eventCamera, List<RaycastHitData> results)
        {
            var graphics = GraphicRegistry.GetGraphicsForCanvas(canvas);

            s_SortedGraphics.Clear();
            for (int i = 0; i < graphics.Count; ++i)
            {
                var graphic = graphics[i];

                // -1 means it hasn't been processed by the canvas, which means it isn't actually drawn
                if (graphic.depth == -1 || !graphic.raycastTarget || graphic.canvasRenderer.cull)
                    continue;

                if (((1 << graphic.gameObject.layer) & layerMask) == 0)
                    continue;

#if UNITY_2020_1_OR_NEWER
                var raycastPadding = graphic.raycastPadding;
#else
                var raycastPadding = Vector4.zero;
#endif

                if (RayIntersectsRectTransform(graphic.rectTransform, raycastPadding, ray, out var worldPos, out var distance))
                {
                    if (distance <= maxDistance)
                    {
                        Vector2 screenPos = eventCamera.WorldToScreenPoint(worldPos);
                        // mask/image intersection - See Unity docs on eventAlphaThreshold for when this does anything
                        if (graphic.Raycast(screenPos, eventCamera))
                        {
                            s_SortedGraphics.Add(new RaycastHitData(graphic, worldPos, screenPos, distance, eventCamera.targetDisplay));
                        }
                    }
                }
            }

            SortingHelpers.Sort(s_SortedGraphics, s_RaycastHitComparer);
            results.AddRange(s_SortedGraphics);
        }

        static bool RayIntersectsRectTransform(RectTransform transform, Vector4 raycastPadding, Ray ray, out Vector3 worldPosition, out float distance)
        {
            GetRectTransformWorldCorners(transform, raycastPadding, s_Corners);
            var plane = new Plane(s_Corners[0], s_Corners[1], s_Corners[2]);

            if (plane.Raycast(ray, out var enter))
            {
                var intersection = ray.GetPoint(enter);

                var bottomEdge = s_Corners[3] - s_Corners[0];
                var leftEdge = s_Corners[1] - s_Corners[0];
                var bottomDot = Vector3.Dot(intersection - s_Corners[0], bottomEdge);
                var leftDot = Vector3.Dot(intersection - s_Corners[0], leftEdge);

                // If the intersection is right of the left edge and above the bottom edge.
                if (leftDot >= 0f && bottomDot >= 0f)
                {
                    var topEdge = s_Corners[1] - s_Corners[2];
                    var rightEdge = s_Corners[3] - s_Corners[2];
                    var topDot = Vector3.Dot(intersection - s_Corners[2], topEdge);
                    var rightDot = Vector3.Dot(intersection - s_Corners[2], rightEdge);

                    // If the intersection is left of the right edge, and below the top edge
                    if (topDot >= 0f && rightDot >= 0f)
                    {
                        worldPosition = intersection;
                        distance = enter;
                        return true;
                    }
                }
            }

            worldPosition = Vector3.zero;
            distance = 0f;
            return false;
        }

        // This method is similar to RecTransform.GetWorldCorners, but with support for the raycastPadding offset.
        static void GetRectTransformWorldCorners(RectTransform transform, Vector4 offset, Vector3[] fourCornersArray)
        {
            if (fourCornersArray == null || fourCornersArray.Length < 4)
            {
                Debug.LogError("Calling GetRectTransformWorldCorners with an array that is null or has less than 4 elements.");
                return;
            }

            // GraphicRaycaster.Raycast uses RectTransformUtility.RectangleContainsScreenPoint instead,
            // which redirects to PointInRectangle defined in RectTransformUtil.cpp. However, that method
            // uses the Camera to convert from the given screen point to a ray, but this class uses
            // the ray from the Ray Interactor that feeds the event data.
            // Offset calculation for raycastPadding from PointInRectangle method, which replaces RectTransform.GetLocalCorners.
            var rect = transform.rect;
            var x0 = rect.x + offset.x;
            var y0 = rect.y + offset.y;
            var x1 = rect.xMax - offset.z;
            var y1 = rect.yMax - offset.w;
            fourCornersArray[0] = new Vector3(x0, y0, 0f);
            fourCornersArray[1] = new Vector3(x0, y1, 0f);
            fourCornersArray[2] = new Vector3(x1, y1, 0f);
            fourCornersArray[3] = new Vector3(x1, y0, 0f);

            // Transform the local corners to world space, which is from RectTransform.GetWorldCorners.
            var localToWorldMatrix = transform.localToWorldMatrix;
            for (var index = 0; index < 4; ++index)
                fourCornersArray[index] = localToWorldMatrix.MultiplyPoint(fourCornersArray[index]);
        }
    }
}
