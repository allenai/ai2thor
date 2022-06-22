namespace UnityEngine.XR.Interaction.Toolkit
{
    /// <summary>
    /// Interactor helper object that draws a targeting <see cref="reticlePrefab"/> over a ray casted point in front of the Interactor.
    /// </summary>
    [AddComponentMenu("XR/Visual/XR Interactor Reticle Visual", 11)]
    [DisallowMultipleComponent]
    [HelpURL(XRHelpURLConstants.k_XRInteractorReticleVisual)]
    public class XRInteractorReticleVisual : MonoBehaviour
    {
        const int k_MaxRaycastHits = 10;

        [SerializeField, Tooltip("The max distance to Raycast from this Interactor.")]
        float m_MaxRaycastDistance = 10f;
        /// <summary>
        /// The max distance to Raycast from this Interactor.
        /// </summary>
        public float maxRaycastDistance
        {
            get => m_MaxRaycastDistance;
            set => m_MaxRaycastDistance = value;
        }

        [SerializeField, Tooltip("Prefab to draw over Raycast destination.")]
        GameObject m_ReticlePrefab;
        /// <summary>
        /// Prefab which Unity draws over Raycast destination.
        /// </summary>
        public GameObject reticlePrefab
        {
            get => m_ReticlePrefab;
            set
            {
                m_ReticlePrefab = value;
                SetupReticlePrefab();
            }
        }

        [SerializeField, Tooltip("Amount to scale prefab (before applying distance scaling).")]
        float m_PrefabScalingFactor = 1f;
        /// <summary>
        /// Amount to scale prefab (before applying distance scaling).
        /// </summary>
        public float prefabScalingFactor
        {
            get => m_PrefabScalingFactor;
            set => m_PrefabScalingFactor = value;
        }

        [SerializeField, Tooltip("Whether to undo the apparent scale of the prefab by distance.")]
        bool m_UndoDistanceScaling = true;
        /// <summary>
        /// Whether Unity undoes the apparent scale of the prefab by distance.
        /// </summary>
        public bool undoDistanceScaling
        {
            get => m_UndoDistanceScaling;
            set => m_UndoDistanceScaling = value;
        }

        [SerializeField, Tooltip("Whether to align the prefab to the ray casted surface normal.")]
        bool m_AlignPrefabWithSurfaceNormal = true;
        /// <summary>
        /// Whether Unity aligns the prefab to the ray casted surface normal.
        /// </summary>
        public bool alignPrefabWithSurfaceNormal
        {
            get => m_AlignPrefabWithSurfaceNormal;
            set => m_AlignPrefabWithSurfaceNormal = value;
        }

        [SerializeField, Tooltip("Smoothing time for endpoint.")]
        float m_EndpointSmoothingTime = 0.02f;
        /// <summary>
        /// Smoothing time for endpoint.
        /// </summary>
        public float endpointSmoothingTime
        {
            get => m_EndpointSmoothingTime;
            set => m_EndpointSmoothingTime = value;
        }

        [SerializeField, Tooltip("Draw the Reticle Prefab while selecting an Interactable.")]
        bool m_DrawWhileSelecting;
        /// <summary>
        /// Whether Unity draws the <see cref="reticlePrefab"/> while selecting an Interactable.
        /// </summary>
        public bool drawWhileSelecting
        {
            get => m_DrawWhileSelecting;
            set => m_DrawWhileSelecting = value;
        }

        [SerializeField, Tooltip("Layer mask for ray cast.")]
        LayerMask m_RaycastMask = -1;
        /// <summary>
        /// Layer mask for ray cast.
        /// </summary>
        public LayerMask raycastMask
        {
            get => m_RaycastMask;
            set => m_RaycastMask = value;
        }

        bool m_ReticleActive;
        /// <summary>
        /// Whether the reticle is currently active.
        /// </summary>
        public bool reticleActive
        {
            get => m_ReticleActive;
            set
            {
                m_ReticleActive = value;
                if (m_ReticleInstance != null)
                    m_ReticleInstance.SetActive(value);
            }
        }

        GameObject m_ReticleInstance;
        XRBaseInteractor m_Interactor;
        Vector3 m_TargetEndPoint;
        Vector3 m_TargetEndNormal;

        /// <summary>
        /// Reusable array of ray cast hits.
        /// </summary>
        readonly RaycastHit[] m_RaycastHits = new RaycastHit[k_MaxRaycastHits];

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void Awake()
        {
            m_Interactor = GetComponent<XRBaseInteractor>();
            if (m_Interactor != null)
            {
                m_Interactor.selectEntered.AddListener(OnSelectEntered);
            }
            SetupReticlePrefab();
            reticleActive = false;
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void Update()
        {
            if (m_Interactor != null && UpdateReticleTarget())
                ActivateReticleAtTarget();
            else
                reticleActive = false;
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void OnDestroy()
        {
            if (m_Interactor != null)
            {
                m_Interactor.selectEntered.RemoveListener(OnSelectEntered);
            }
        }

        void SetupReticlePrefab()
        {
            if (m_ReticleInstance != null)
                Destroy(m_ReticleInstance);

            if (m_ReticlePrefab != null)
                m_ReticleInstance = Instantiate(m_ReticlePrefab);
        }

        static RaycastHit FindClosestHit(RaycastHit[] hits, int hitCount)
        {
            var index = 0;
            var distance = float.MaxValue;
            for (var i = 0; i < hitCount; ++i)
            {
                if (hits[i].distance < distance)
                {
                    distance = hits[i].distance;
                    index = i;
                }
            }

            return hits[index];
        }

        bool TryGetRaycastPoint(ref Vector3 raycastPos, ref Vector3 raycastNormal)
        {
            var raycastHit = false;

            // Raycast against physics
            var hitCount = Physics.RaycastNonAlloc(m_Interactor.attachTransform.position, m_Interactor.attachTransform.forward,
                m_RaycastHits, m_MaxRaycastDistance, m_RaycastMask);
            if (hitCount != 0)
            {
                var closestHit = FindClosestHit(m_RaycastHits, hitCount);
                raycastPos = closestHit.point;
                raycastNormal = closestHit.normal;
                raycastHit = true;
            }

            return raycastHit;
        }

        bool UpdateReticleTarget()
        {
            if (!m_DrawWhileSelecting && m_Interactor.hasSelection)
                return false;

            var raycastPos = Vector3.zero;
            var raycastNormal = Vector3.zero;
            if (TryGetRaycastPoint(ref raycastPos, ref raycastNormal))
            {
                // Smooth target
                var velocity = Vector3.zero;
                m_TargetEndPoint = Vector3.SmoothDamp(m_TargetEndPoint, raycastPos, ref velocity, m_EndpointSmoothingTime);
                m_TargetEndNormal = Vector3.SmoothDamp(m_TargetEndNormal, raycastNormal, ref velocity, m_EndpointSmoothingTime);
                return true;
            }
            return false;
        }

        void ActivateReticleAtTarget()
        {
            if (m_ReticleInstance != null)
            {
                m_ReticleInstance.transform.position = m_TargetEndPoint;
                if (m_AlignPrefabWithSurfaceNormal)
                    m_ReticleInstance.transform.rotation = Quaternion.FromToRotation(Vector3.up, m_TargetEndNormal);
                else
                    m_ReticleInstance.transform.rotation = Quaternion.FromToRotation(Vector3.up, (m_Interactor.attachTransform.position - m_TargetEndPoint).normalized);
                var scaleFactor = m_PrefabScalingFactor;
                if (m_UndoDistanceScaling)
                    scaleFactor *= Vector3.Distance(m_Interactor.attachTransform.position, m_TargetEndPoint);
                m_ReticleInstance.transform.localScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
                reticleActive = true;
            }
        }

        void OnSelectEntered(SelectEnterEventArgs args)
        {
            reticleActive = false;
        }
    }
}
