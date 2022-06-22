using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace UnityEngine.XR.Interaction.Toolkit.UI
{
    /// <summary>
    /// Physics-based UI Raycaster for Tracked Devices (e.g. XR Controllers).
    /// Detects UI elements with physics colliders attached to their GameObjects.
    /// </summary>
    /// <remarks>
    /// Does not need to be attached to any canvas. UI elements are any element inheriting from <see cref="IEventSystemHandler"/>.
    /// Designed to work with <see cref="XRUIInputModule"/>, which configures the final screen position once all hits are tallied.
    /// </remarks>
    /// <seealso cref="PhysicsRaycaster"/>
    [AddComponentMenu("Event/Tracked Device Physics Raycaster", 11)]
    [HelpURL(XRHelpURLConstants.k_TrackedDevicePhysicsRaycaster)]
    public class TrackedDevicePhysicsRaycaster : BaseRaycaster
    {
        /// <summary>
        /// Default value of <see cref="eventMask"/> for all layers.
        /// </summary>
        const int k_EverythingLayerMask = -1;

        // Non allocating, non boxing ArraySegment variant
        class RaycastHitArraySegment : IEnumerable<RaycastHit>, IEnumerator<RaycastHit>
        {
            int m_Count;

            public int count
            {
                get => m_Count;
                set => m_Count = value;
            }

            public RaycastHit Current => m_Hits[m_CurrentIndex];

            object IEnumerator.Current => Current;

            readonly RaycastHit[] m_Hits;
            int m_CurrentIndex;

            public RaycastHitArraySegment(RaycastHit[] raycastHits, int count)
            {
                m_Hits = raycastHits;
                m_Count = count;
            }

            public bool MoveNext()
            {
                m_CurrentIndex++;
                return m_CurrentIndex < m_Count;
            }

            public void Reset()
            {
                m_CurrentIndex = -1;
            }

            public void Dispose()
            {
            }

            public IEnumerator<RaycastHit> GetEnumerator()
            {
                Reset();
                return this;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        /// <summary>
        /// Compares ray cast hits by distance, to sort in ascending order.
        /// </summary>
        sealed class RaycastHitComparer : IComparer<RaycastHit>
        {
            /// <summary>
            /// Compares ray cast hits by distance in ascending order.
            /// </summary>
            /// <param name="a">The first ray cast hit to compare.</param>
            /// <param name="b">The second ray cast hit to compare.</param>
            /// <returns>Returns less than 0 if a is closer than b. 0 if a and b are equal. Greater than 0 if b is closer than a.</returns>
            public int Compare(RaycastHit a, RaycastHit b)
                => a.distance.CompareTo(b.distance);
        }

        [SerializeField]
        [Tooltip("Specifies whether the ray cast should hit triggers.")]
        QueryTriggerInteraction m_RaycastTriggerInteraction = QueryTriggerInteraction.Ignore;

        /// <summary>
        /// Specifies whether the ray cast should hit triggers.
        /// </summary>
        public QueryTriggerInteraction raycastTriggerInteraction
        {
            get => m_RaycastTriggerInteraction;
            set => m_RaycastTriggerInteraction = value;
        }

        [SerializeField, Tooltip("Layer mask used to filter events. Always combined with the ray cast mask of the UI interactor.")]
        LayerMask m_EventMask = k_EverythingLayerMask;

        /// <summary>
        /// Layer mask used to filter events. Always combined with the ray cast mask of the <see cref="IUIInteractor"/>.
        /// </summary>
        public LayerMask eventMask
        {
            get => m_EventMask;
            set => m_EventMask = value;
        }

        [SerializeField, Tooltip("The max number of intersections allowed. Value will be clamped to greater than 0.")]
        int m_MaxRayIntersections = 10;

        /// <summary>
        /// Max number of ray intersections allowed to be found.
        /// </summary>
        /// <remarks>
        /// Value will be clamped to greater than 0.
        /// </remarks>
        public int maxRayIntersections
        {
            get => m_MaxRayIntersections;
            set => m_MaxRayIntersections = Math.Max(value, 1);
        }

        [SerializeField, Tooltip("The event camera for this ray caster. The event camera is used to determine the screen position and display of the ray cast results.")]
        Camera m_EventCamera;

        /// <summary>
        /// See [BaseRaycaster.eventCamera](xref:UnityEngine.EventSystems.BaseRaycaster.eventCamera).
        /// </summary>
        public override Camera eventCamera
        {
            get
            {
                if (m_EventCamera == null)
                    m_EventCamera = GetComponent<Camera>();
                return m_EventCamera != null ? m_EventCamera : Camera.main;
            }
        }

        /// <summary>
        /// Sets the event camera for this ray caster. The event camera is used to determine the screen position and display of the ray cast results.
        /// </summary>
        /// <param name="newEventCamera">The new <see cref="Camera"/> to set as this ray caster's <see cref="eventCamera"/>.</param>
        public void SetEventCamera(Camera newEventCamera)
        {
            m_EventCamera = newEventCamera;
        }

        /// <summary>
        /// Performs a ray cast against all physics objects using this event.
        /// </summary>
        /// <remarks>Will only process events of type <see cref="TrackedDeviceEventData"/>.</remarks>
        /// <param name="eventData">Data containing where and how to ray cast.</param>
        /// <param name="resultAppendList">The resultant hits from the ray cast.</param>
        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            if (eventData is TrackedDeviceEventData trackedEventData)
            {
                PerformRaycasts(trackedEventData, resultAppendList);
            }
        }

        bool m_HasWarnedEventCameraNull;

        RaycastHit[] m_RaycastHits;
        readonly RaycastHitComparer m_RaycastHitComparer = new RaycastHitComparer();
        RaycastHitArraySegment m_RaycastArrayWrapper;

        // Use this list on each ray cast to avoid continually allocating.
        readonly List<RaycastHit> m_RaycastResultsCache = new List<RaycastHit>();

        /// <summary>
        /// See <a href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.Awake.html">MonoBehaviour.Awake</a>.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            m_RaycastHits = new RaycastHit[m_MaxRayIntersections];
            m_RaycastArrayWrapper = new RaycastHitArraySegment(m_RaycastHits, 0);
        }

        void PerformRaycasts(TrackedDeviceEventData eventData, List<RaycastResult> resultAppendList)
        {
            // Property can call Camera.main, so cache the reference
            var currentEventCamera = eventCamera;
            if (currentEventCamera == null)
            {
                if (!m_HasWarnedEventCameraNull)
                {
                    Debug.LogWarning("Event Camera must be set on TrackedDevicePhysicsRaycaster to determine screen space coordinates." +
                        " UI events will not function correctly until it is set.",
                        this);
                    m_HasWarnedEventCameraNull = true;
                }

                return;
            }

            var rayPoints = eventData.rayPoints;
            var layerMask = eventData.layerMask & m_EventMask;
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

            m_MaxRayIntersections = Math.Max(m_MaxRayIntersections, 1);
            if (m_RaycastHits.Length != m_MaxRayIntersections)
                Array.Resize(ref m_RaycastHits, m_MaxRayIntersections);

            var hitCount = Physics.RaycastNonAlloc(ray, m_RaycastHits, hitDistance, layerMask, m_RaycastTriggerInteraction);
            m_RaycastArrayWrapper.count = hitCount;

            m_RaycastResultsCache.Clear();
            m_RaycastResultsCache.AddRange(m_RaycastArrayWrapper);
            SortingHelpers.Sort(m_RaycastResultsCache, m_RaycastHitComparer);

            // Now that we have a list of sorted hits, process any extra settings and filters.
            foreach (var hit in m_RaycastResultsCache)
            {
                var col = hit.collider;
                var go = col.gameObject;

                Vector2 screenPos = currentEventCamera.WorldToScreenPoint(hit.point);
                var relativeMousePosition = Display.RelativeMouseAt(screenPos);
                var displayIndex = (int)relativeMousePosition.z;

                var validHit = hit.distance < hitDistance;
                if (validHit)
                {
                    var result = new RaycastResult
                    {
                        gameObject = go,
                        module = this,
                        distance = hit.distance,
                        index = resultAppendList.Count,
                        depth = 0,
                        sortingLayer = 0,
                        sortingOrder = 0,
                        worldPosition = hit.point,
                        worldNormal = hit.normal,
                        screenPosition = screenPos,
                        displayIndex = displayIndex,
                    };

                    resultAppendList.Add(result);

                    hitSomething = true;
                }
            }

            return hitSomething;
        }
    }
}
