using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Utilities;

namespace UnityEngine.XR.Interaction.Toolkit
{
    /// <summary>
    /// Interactor used for holding interactables via a socket. This component is not designed to be attached to a controller
    /// (thus does not derive from <see cref="XRBaseControllerInteractor"/>) and instead will always attempt to select an interactable that it is
    /// hovering over.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("XR/XR Socket Interactor", 11)]
    [HelpURL(XRHelpURLConstants.k_XRSocketInteractor)]
    public partial class XRSocketInteractor : XRBaseInteractor
    {
        [SerializeField]
        bool m_ShowInteractableHoverMeshes = true;
        /// <summary>
        /// Whether this socket should show a mesh at socket's attach point for Interactables that it is hovering over.
        /// </summary>
        /// <remarks>
        /// The interactable's attach transform must not change parent Transform while selected
        /// for the position and rotation of the hover mesh to be correctly calculated.
        /// </remarks>
        public bool showInteractableHoverMeshes
        {
            get => m_ShowInteractableHoverMeshes;
            set => m_ShowInteractableHoverMeshes = value;
        }

        [SerializeField]
        Material m_InteractableHoverMeshMaterial;
        /// <summary>
        /// Material used for rendering interactable meshes on hover
        /// (a default material will be created if none is supplied).
        /// </summary>
        public Material interactableHoverMeshMaterial
        {
            get => m_InteractableHoverMeshMaterial;
            set => m_InteractableHoverMeshMaterial = value;
        }

        [SerializeField]
        Material m_InteractableCantHoverMeshMaterial;
        /// <summary>
        /// Material used for rendering interactable meshes on hover when there is already a selected object in the socket
        /// (a default material will be created if none is supplied).
        /// </summary>
        public Material interactableCantHoverMeshMaterial
        {
            get => m_InteractableCantHoverMeshMaterial;
            set => m_InteractableCantHoverMeshMaterial = value;
        }

        [SerializeField]
        bool m_SocketActive = true;
        /// <summary>
        /// Whether socket interaction is enabled.
        /// </summary>
        public bool socketActive
        {
            get => m_SocketActive;
            set => m_SocketActive = value;
        }

        [SerializeField]
        float m_InteractableHoverScale = 1f;
        /// <summary>
        /// Scale at which to render hovered Interactable.
        /// </summary>
        public float interactableHoverScale
        {
            get => m_InteractableHoverScale;
            set => m_InteractableHoverScale = value;
        }

        [SerializeField]
        float m_RecycleDelayTime = 1f;
        /// <summary>
        /// Sets the amount of time the socket will refuse hovers after an object is removed.
        /// </summary>
        public float recycleDelayTime
        {
            get => m_RecycleDelayTime;
            set => m_RecycleDelayTime = value;
        }

        float m_LastRemoveTime = -1f;

        /// <summary>
        /// The set of Interactables that this Interactor could possibly interact with this frame.
        /// This list is not sorted by priority.
        /// </summary>
        /// <seealso cref="IXRInteractor.GetValidTargets"/>
        protected List<IXRInteractable> unsortedValidTargets { get; } = new List<IXRInteractable>();

        /// <summary>
        /// The set of Colliders that stayed in touch with this Interactor on fixed updated.
        /// This list will be populated by colliders in OnTriggerStay.
        /// </summary>
        readonly List<Collider> m_StayedColliders = new List<Collider>();

        readonly TriggerContactMonitor m_TriggerContactMonitor = new TriggerContactMonitor();

        readonly Dictionary<IXRInteractable, ValueTuple<MeshFilter, Renderer>[]> m_MeshFilterCache = new Dictionary<IXRInteractable, ValueTuple<MeshFilter, Renderer>[]>();

        /// <summary>
        /// Reusable list of type <see cref="MeshFilter"/> to reduce allocations.
        /// </summary>
        static readonly List<MeshFilter> s_MeshFilters = new List<MeshFilter>();

        /// <summary>
        /// Reusable value of <see cref="WaitForFixedUpdate"/> to reduce allocations.
        /// </summary>
        static readonly WaitForFixedUpdate s_WaitForFixedUpdate = new WaitForFixedUpdate();

        /// <inheritdoc />
        protected override void Awake()
        {
            base.Awake();

            m_TriggerContactMonitor.interactionManager = interactionManager;
            m_TriggerContactMonitor.contactAdded += OnContactAdded;
            m_TriggerContactMonitor.contactRemoved += OnContactRemoved;

            CreateDefaultHoverMaterials();
        }

        /// <inheritdoc />
        protected override void Start()
        {
            base.Start();

            StartCoroutine(UpdateCollidersAfterOnTriggerStay());
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        /// <param name="other">The other <see cref="Collider"/> involved in this collision.</param>
        protected void OnTriggerEnter(Collider other)
        {
            m_TriggerContactMonitor.AddCollider(other);
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        /// <param name="other">The other <see cref="Collider"/> involved in this collision.</param>
        protected void OnTriggerStay(Collider other)
        {
            m_StayedColliders.Add(other);
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        /// <param name="other">The other <see cref="Collider"/> involved in this collision.</param>
        protected void OnTriggerExit(Collider other)
        {
            m_TriggerContactMonitor.RemoveCollider(other);
        }

        /// <summary>
        /// This coroutine functions like a LateFixedUpdate method that executes after OnTriggerXXX.
        /// </summary>
        /// <returns>Returns enumerator for coroutine.</returns>
        IEnumerator UpdateCollidersAfterOnTriggerStay()
        {
            while (true)
            {
                // Wait until the end of the physics cycle so that OnTriggerXXX can get called.
                // See https://docs.unity3d.com/Manual/ExecutionOrder.html
                yield return s_WaitForFixedUpdate;

                m_TriggerContactMonitor.UpdateStayedColliders(m_StayedColliders);
            }
            // ReSharper disable once IteratorNeverReturns -- stopped when behavior is destroyed.
        }

        /// <inheritdoc />
        public override void ProcessInteractor(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractor(updatePhase);

            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Fixed)
            {
                // Clear stayed Colliders at the beginning of the physics cycle before
                // the OnTriggerStay method populates this list.
                // Then the UpdateCollidersAfterOnTriggerStay coroutine will use this list to remove Colliders
                // that no longer stay in this frame after previously entered.
                m_StayedColliders.Clear();
            }
            else if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                // An explicit check for isHoverRecycleAllowed is done since an interactable may have been deselected
                // after this socket was updated by the manager, such as when a later Interactor takes the selection
                // from this socket. The recycle delay time could cause the hover to be effectively disabled.
                if (m_ShowInteractableHoverMeshes && hasHover && isHoverRecycleAllowed)
                    DrawHoveredInteractables();
            }
        }

        /// <summary>
        /// Creates the default hover materials
        /// for <see cref="interactableHoverMeshMaterial"/> and <see cref="interactableCantHoverMeshMaterial"/> if necessary.
        /// </summary>
        protected virtual void CreateDefaultHoverMaterials()
        {
            if (m_InteractableHoverMeshMaterial != null && m_InteractableCantHoverMeshMaterial != null)
                return;

            var shaderName = GraphicsSettings.currentRenderPipeline ? "Universal Render Pipeline/Lit" : "Standard";
            var defaultShader = Shader.Find(shaderName);

            if (defaultShader == null)
            {
                Debug.LogWarning("Failed to create default materials for Socket Interactor," +
                    $" was unable to find \"{shaderName}\" Shader. Make sure the shader is included into the game build.", this);
                return;
            }

            if (m_InteractableHoverMeshMaterial == null)
            {
                m_InteractableHoverMeshMaterial = new Material(defaultShader);
                SetMaterialFade(m_InteractableHoverMeshMaterial, new Color(0f, 0f, 1f, 0.6f));
            }

            if (m_InteractableCantHoverMeshMaterial == null)
            {
                m_InteractableCantHoverMeshMaterial = new Material(defaultShader);
                SetMaterialFade(m_InteractableCantHoverMeshMaterial, new Color(1f, 0f, 0f, 0.6f));
            }
        }

        /// <summary>
        /// Sets Standard <paramref name="material"/> with Fade rendering mode
        /// and set <paramref name="color"/> as the main color.
        /// </summary>
        /// <param name="material">The <see cref="Material"/> whose properties will be set.</param>
        /// <param name="color">The main color to set.</param>
        static void SetMaterialFade(Material material, Color color)
        {
            material.SetOverrideTag("RenderType", "Transparent");
            material.SetFloat(ShaderPropertyLookup.mode, 2f);
            material.SetInt(ShaderPropertyLookup.srcBlend, (int)BlendMode.SrcAlpha);
            material.SetInt(ShaderPropertyLookup.dstBlend, (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt(ShaderPropertyLookup.zWrite, 0);
            // ReSharper disable StringLiteralTypo
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            // ReSharper restore StringLiteralTypo
            material.renderQueue = (int)RenderQueue.Transparent;
            material.SetColor(GraphicsSettings.currentRenderPipeline ? ShaderPropertyLookup.baseColor : ShaderPropertyLookup.color, color);
        }

        /// <inheritdoc />
        protected override void OnHoverEntering(HoverEnterEventArgs args)
        {
            base.OnHoverEntering(args);

            s_MeshFilters.Clear();
            args.interactableObject.transform.GetComponentsInChildren(true, s_MeshFilters);
            if (s_MeshFilters.Count == 0)
                return;

            var interactableTuples = new ValueTuple<MeshFilter, Renderer>[s_MeshFilters.Count];
            for (var i = 0; i < s_MeshFilters.Count; ++i)
            {
                var meshFilter = s_MeshFilters[i];
                interactableTuples[i] = (meshFilter, meshFilter.GetComponent<Renderer>());
            }
            m_MeshFilterCache.Add(args.interactableObject, interactableTuples);
        }

        /// <inheritdoc />
        protected override void OnHoverExiting(HoverExitEventArgs args)
        {
            base.OnHoverExiting(args);
            m_MeshFilterCache.Remove(args.interactableObject);
        }

        /// <inheritdoc />
        protected override void OnSelectExiting(SelectExitEventArgs args)
        {
            base.OnSelectExiting(args);
            m_LastRemoveTime = Time.time;
        }

        Matrix4x4 GetHoverMeshMatrix(IXRInteractable interactable, MeshFilter meshFilter, float hoverScale)
        {
            var interactableAttachTransform = interactable.GetAttachTransform(this);

            var grabInteractable = interactable as XRGrabInteractable;

            // Get the "static" pose of the attach transform in world space.
            // While the interactable is selected, it may have a different pose than when released,
            // so this assumes it will be restored back to the original pose before a selection was made.
            Pose interactableAttachPose;
            if (interactable is IXRSelectInteractable selectable && selectable.isSelected &&
                interactableAttachTransform != interactable.transform &&
                interactableAttachTransform.IsChildOf(interactable.transform))
            {
                // The interactable's attach transform must not change parent Transform while selected
                // for the pose to be calculated correctly. This transforms the captured pose in local space
                // into the current pose in world space. If the pose of the attach transform was not modified
                // after being selected, this will be the same value as calculated in the else statement.
                var localAttachPose = selectable.GetLocalAttachPoseOnSelect(selectable.firstInteractorSelecting);
                var attachTransformParent = interactableAttachTransform.parent;
                interactableAttachPose =
                    new Pose(attachTransformParent.TransformPoint(localAttachPose.position),
                        attachTransformParent.rotation * localAttachPose.rotation);
            }
            else
            {
                interactableAttachPose = new Pose(interactableAttachTransform.position, interactableAttachTransform.rotation);
            }

            var attachOffset = meshFilter.transform.position - interactableAttachPose.position;
            var interactableLocalPosition = InverseTransformDirection(interactableAttachPose, attachOffset) * hoverScale;
            var interactableLocalRotation = Quaternion.Inverse(Quaternion.Inverse(meshFilter.transform.rotation) * interactableAttachPose.rotation);

            Vector3 position;
            Quaternion rotation;

            var interactorAttachTransform = GetAttachTransform(interactable);
            var interactorAttachPose = new Pose(interactorAttachTransform.position, interactorAttachTransform.rotation);
            if (grabInteractable == null || grabInteractable.trackRotation)
            {
                position = interactorAttachPose.rotation * interactableLocalPosition + interactorAttachPose.position;
                rotation = interactorAttachPose.rotation * interactableLocalRotation;
            }
            else
            {
                position = interactableAttachPose.rotation * interactableLocalPosition + interactorAttachPose.position;
                rotation = meshFilter.transform.rotation;
            }

            // Rare case that Track Position is disabled
            if (grabInteractable != null && !grabInteractable.trackPosition)
                position = meshFilter.transform.position;

            var scale = meshFilter.transform.lossyScale * hoverScale;

            return Matrix4x4.TRS(position, rotation, scale);
        }

        /// <summary>
        /// Transforms a direction from world space to local space. The opposite of <c>Transform.TransformDirection</c>,
        /// but using a world Pose instead of a Transform.
        /// </summary>
        /// <param name="pose">The world space position and rotation of the Transform.</param>
        /// <param name="direction">The direction to transform.</param>
        /// <returns>Returns the transformed direction.</returns>
        /// <remarks>
        /// This operation is unaffected by scale.
        /// <br/>
        /// You should use <c>Transform.InverseTransformPoint</c> equivalent if the vector represents a position in space rather than a direction.
        /// </remarks>
        static Vector3 InverseTransformDirection(Pose pose, Vector3 direction)
        {
            return Quaternion.Inverse(pose.rotation) * direction;
        }

        /// <summary>
        /// Unity calls this method automatically in order to draw the Interactables that are currently being hovered over.
        /// </summary>
        protected virtual void DrawHoveredInteractables()
        {
            if (m_InteractableHoverScale <= 0f)
                return;

            var materialToDrawWith = hasSelection ? m_InteractableCantHoverMeshMaterial : m_InteractableHoverMeshMaterial;
            if (materialToDrawWith == null)
                return;

            var mainCamera = Camera.main;
            if (mainCamera == null)
                return;

            foreach (var interactable in interactablesHovered)
            {
                if (interactable == null)
                    continue;

                if (IsSelecting(interactable))
                    continue;

                if (!m_MeshFilterCache.TryGetValue(interactable, out var interactableTuples))
                    continue;

                if (interactableTuples == null || interactableTuples.Length == 0)
                    continue;

                foreach (var tuple in interactableTuples)
                {
                    var meshFilter = tuple.Item1;
                    var meshRenderer = tuple.Item2;
                    if (!ShouldDrawHoverMesh(meshFilter, meshRenderer, mainCamera))
                        continue;

                    var matrix = GetHoverMeshMatrix(interactable, meshFilter, m_InteractableHoverScale);
                    var sharedMesh = meshFilter.sharedMesh;
                    for (var submeshIndex = 0; submeshIndex < sharedMesh.subMeshCount; ++submeshIndex)
                    {
                        Graphics.DrawMesh(
                            sharedMesh,
                            matrix,
                            materialToDrawWith,
                            gameObject.layer,
                            null, // Draw mesh in all cameras (default value)
                            submeshIndex);
                    }
                }
            }
        }

        /// <inheritdoc />
        public override void GetValidTargets(List<IXRInteractable> targets)
        {
            SortingHelpers.SortByDistanceToInteractor(this, unsortedValidTargets, targets);
        }

        /// <inheritdoc />
        public override bool isHoverActive => base.isHoverActive && m_SocketActive;

        /// <inheritdoc />
        public override bool isSelectActive => base.isSelectActive && m_SocketActive;

        /// <inheritdoc />
        public override XRBaseInteractable.MovementType? selectedInteractableMovementTypeOverride => XRBaseInteractable.MovementType.Instantaneous;

        /// <inheritdoc />
        public override bool CanHover(IXRHoverInteractable interactable)
        {
            return base.CanHover(interactable) && isHoverRecycleAllowed;
        }

        bool isHoverRecycleAllowed => m_LastRemoveTime < 0f || m_RecycleDelayTime <= 0f || (Time.time > m_LastRemoveTime + m_RecycleDelayTime);

        /// <inheritdoc />
        public override bool CanSelect(IXRSelectInteractable interactable)
        {
            return base.CanSelect(interactable) &&
                ((!hasSelection && !interactable.isSelected) ||
                    (IsSelecting(interactable) && interactable.interactorsSelecting.Count == 1));
        }

        /// <summary>
        /// Unity calls this method automatically in order to determine whether the mesh should be drawn.
        /// </summary>
        /// <param name="meshFilter">The <see cref="MeshFilter"/> which will be drawn when returning <see langword="true"/>.</param>
        /// <param name="meshRenderer">The <see cref="Renderer"/> on the same <see cref="GameObject"/> as the <paramref name="meshFilter"/>.</param>
        /// <param name="mainCamera">The Main Camera.</param>
        /// <returns>Returns <see langword="true"/> if the mesh should be drawn. Otherwise, returns <see langword="false"/>.</returns>
        /// <seealso cref="DrawHoveredInteractables"/>
        protected virtual bool ShouldDrawHoverMesh(MeshFilter meshFilter, Renderer meshRenderer, Camera mainCamera)
        {
            // Graphics.DrawMesh will automatically handle camera culling of the hover mesh using
            // the GameObject layer of this socket that we pass as the argument value.
            // However, we also check here to skip drawing the hover mesh if the mesh of the interactable
            // itself isn't also drawn by the main camera. For the typical scene with one camera,
            // this means that for the hover mesh to be rendered, the camera should have a culling mask
            // which overlaps with both the GameObject layer of this socket and the GameObject layer of the interactable.
            var cullingMask = mainCamera.cullingMask;
            return meshFilter != null && (cullingMask & (1 << meshFilter.gameObject.layer)) != 0 && meshRenderer != null && meshRenderer.enabled;
        }

        /// <inheritdoc />
        protected override void OnRegistered(InteractorRegisteredEventArgs args)
        {
            base.OnRegistered(args);
            args.manager.interactableRegistered += OnInteractableRegistered;
            args.manager.interactableUnregistered += OnInteractableUnregistered;

            // Attempt to resolve any colliders that entered this trigger while this was not subscribed,
            // and filter out any targets that were unregistered while this was not subscribed.
            m_TriggerContactMonitor.interactionManager = args.manager;
            m_TriggerContactMonitor.ResolveUnassociatedColliders();
            XRInteractionManager.RemoveAllUnregistered(args.manager, unsortedValidTargets);
        }

        /// <inheritdoc />
        protected override void OnUnregistered(InteractorUnregisteredEventArgs args)
        {
            base.OnUnregistered(args);
            args.manager.interactableRegistered -= OnInteractableRegistered;
            args.manager.interactableUnregistered -= OnInteractableUnregistered;
        }

        void OnInteractableRegistered(InteractableRegisteredEventArgs args)
        {
            m_TriggerContactMonitor.ResolveUnassociatedColliders(args.interactableObject);
            if (m_TriggerContactMonitor.IsContacting(args.interactableObject) && !unsortedValidTargets.Contains(args.interactableObject))
                unsortedValidTargets.Add(args.interactableObject);
        }

        void OnInteractableUnregistered(InteractableUnregisteredEventArgs args)
        {
            unsortedValidTargets.Remove(args.interactableObject);
        }

        void OnContactAdded(IXRInteractable interactable)
        {
            if (!unsortedValidTargets.Contains(interactable))
                unsortedValidTargets.Add(interactable);
        }

        void OnContactRemoved(IXRInteractable interactable)
        {
            unsortedValidTargets.Remove(interactable);
        }

        struct ShaderPropertyLookup
        {
            public static readonly int mode = Shader.PropertyToID("_Mode");
            public static readonly int srcBlend = Shader.PropertyToID("_SrcBlend");
            public static readonly int dstBlend = Shader.PropertyToID("_DstBlend");
            public static readonly int zWrite = Shader.PropertyToID("_ZWrite");
            public static readonly int baseColor = Shader.PropertyToID("_BaseColor");
            public static readonly int color = Shader.PropertyToID("_Color"); // Legacy
        }
    }
}