using UnityEngine.Serialization;

namespace UnityEngine.XR.Interaction.Toolkit
{
    /// <summary>
    /// Interprets feature values on a tracked input controller device into XR Interaction states, such as Select.
    /// Additionally, it applies the current pose value of a tracked device to the transform of the GameObject.
    /// </summary>
    /// <seealso cref="ActionBasedController"/>
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_Controllers)]
    [DisallowMultipleComponent]
    public abstract partial class XRBaseController : MonoBehaviour
    {
        /// <summary>
        /// The time within the frame that controller pose will be sampled.
        /// </summary>
        /// <seealso cref="updateTrackingType"/>
        public enum UpdateType
        {
            /// <summary>
            /// Sample input at both update and directly before rendering. For smooth controller pose tracking,
            /// we recommend using this value as it will provide the lowest input latency for the device.
            /// This is the default value for the UpdateType option.
            /// </summary>
            UpdateAndBeforeRender,

            /// <summary>
            /// Only sample input during the update phase of the frame.
            /// </summary>
            Update,

            /// <summary>
            /// Only sample input directly before rendering.
            /// </summary>
            BeforeRender,
        }

        [SerializeField]
        UpdateType m_UpdateTrackingType = UpdateType.UpdateAndBeforeRender;

        /// <summary>
        /// The time within the frame that the controller samples tracking input.
        /// </summary>
        /// <seealso cref="UpdateType"/>
        public UpdateType updateTrackingType
        {
            get => m_UpdateTrackingType;
            set => m_UpdateTrackingType = value;
        }

        [SerializeField]
        bool m_EnableInputTracking = true;

        /// <summary>
        /// Whether input pose tracking is enabled for this controller.
        /// When enabled, Unity reads the current tracking pose input of the controller device each frame.
        /// </summary>
        /// <remarks>
        /// You can disable this in order to drive the controller state manually instead of from reading current inputs,
        /// such as when playing back recorded pose inputs.
        /// </remarks>
        /// <seealso cref="enableInputActions"/>
        public bool enableInputTracking
        {
            get => m_EnableInputTracking;
            set => m_EnableInputTracking = value;
        }

        [SerializeField]
        bool m_EnableInputActions = true;

        /// <summary>
        /// Whether input for XR Interaction events is enabled for this controller.
        /// When enabled, Unity reads the current input of the controller device each frame.
        /// </summary>
        /// <remarks>
        /// You can disable this in order to drive the controller state manually instead of from reading current inputs,
        /// such as when playing back recorded inputs.
        /// </remarks>
        /// <seealso cref="enableInputTracking"/>
        public bool enableInputActions
        {
            get => m_EnableInputActions;
            set => m_EnableInputActions = value;
        }

        [SerializeField]
        Transform m_ModelPrefab;

        /// <summary>
        /// The prefab of a controller model to show for this controller that this behavior automatically instantiates.
        /// </summary>
        /// <remarks>
        /// This behavior automatically instantiates an instance of the prefab as a child
        /// of <see cref="modelParent"/> upon startup unless <see cref="model"/> is already set,
        /// in which case this value is ignored.
        /// </remarks>
        /// <seealso cref="model"/>
        public Transform modelPrefab
        {
            get => m_ModelPrefab;
            set => m_ModelPrefab = value;
        }

        [SerializeField, FormerlySerializedAs("m_ModelTransform")]
        Transform m_ModelParent;

        /// <summary>
        /// The transform that this behavior uses as the parent for the model prefab when it is instantiated.
        /// </summary>
        /// <remarks>
        /// Automatically instantiated and set in <see cref="Awake"/> if not already set.
        /// Setting this will not automatically destroy the previous object.
        /// </remarks>
        public Transform modelParent
        {
            get => m_ModelParent;
            set
            {
                m_ModelParent = value;

                if (m_Model != null)
                    m_Model.parent = m_ModelParent;
            }
        }

        [SerializeField]
        Transform m_Model;

        /// <summary>
        /// The instance of the controller model in the scene. You can set this to an existing object instead of using <see cref="modelPrefab"/>.
        /// </summary>
        /// <remarks>
        /// If set, it should reference a child GameObject of this behavior so it will update with the controller pose.
        /// </remarks>
        public Transform model
        {
            get => m_Model;
            set => m_Model = value;
        }

        [SerializeField]
        bool m_AnimateModel;

        /// <summary>
        /// Whether to animate the model in response to interaction events. When enabled, the animation trigger will be set for the corresponding
        /// animator component on the controller model when a select or deselect interaction events occurs.
        /// </summary>
        /// <seealso cref="modelSelectTransition"/>
        /// <seealso cref="modelDeSelectTransition"/>
        public bool animateModel
        {
            get => m_AnimateModel;
            set => m_AnimateModel = value;
        }

        [SerializeField]
        string m_ModelSelectTransition;

        /// <summary>
        /// The animation trigger name to activate upon selecting.
        /// </summary>
        /// <seealso cref="Animator.SetTrigger(string)"/>
        public string modelSelectTransition
        {
            get => m_ModelSelectTransition;
            set => m_ModelSelectTransition = value;
        }

        [SerializeField]
        string m_ModelDeSelectTransition;

        /// <summary>
        /// The animation trigger name to activate upon deselecting.
        /// </summary>
        /// <seealso cref="Animator.SetTrigger(string)"/>
        public string modelDeSelectTransition
        {
            get => m_ModelDeSelectTransition;
            set => m_ModelDeSelectTransition = value;
        }

        bool m_HideControllerModel;

        /// <summary>
        /// Whether to hide the controller model.
        /// </summary>
        /// <seealso cref="model"/>
        /// <seealso cref="XRBaseControllerInteractor.hideControllerOnSelect"/>
        public bool hideControllerModel
        {
            get => m_HideControllerModel;
            set
            {
                m_HideControllerModel = value;
                if (m_Model != null)
                    m_Model.gameObject.SetActive(!m_HideControllerModel);
            }
        }

        InteractionState m_SelectInteractionState;
        /// <summary>
        /// (Read Only) The current select interaction state.
        /// </summary>
        public InteractionState selectInteractionState => m_SelectInteractionState;

        InteractionState m_ActivateInteractionState;
        /// <summary>
        /// (Read Only) The current activate interaction state.
        /// </summary>
        public InteractionState activateInteractionState => m_ActivateInteractionState;

        InteractionState m_UIPressInteractionState;
        /// <summary>
        /// (Read Only) The current UI press interaction state.
        /// </summary>
        public InteractionState uiPressInteractionState => m_UIPressInteractionState;

        XRControllerState m_ControllerState;
        /// <summary>
        /// The current state of the controller.
        /// </summary>
        public XRControllerState currentControllerState
        {
            get
            {
                SetupControllerState();
                return m_ControllerState;
            }

            set
            {
                m_ControllerState = value;
                m_CreateControllerState = false;
            }
        }

        bool m_CreateControllerState = true;

#if ANIMATION_MODULE_PRESENT
        /// <summary>
        /// The <see cref="Animator"/> on <see cref="model"/>.
        /// </summary>
        Animator m_ModelAnimator;
#endif

        bool m_HasWarnedAnimatorMissing;

        /// <summary>
        /// A boolean value that indicates setup should be performed on Update.
        /// </summary>
        bool m_PerformSetup = true;

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void Awake()
        {
            // Create empty container transform for the model if none specified.
            // This is not strictly necessary to create since this GameObject could be used
            // as the parent for the instantiated prefab, but doing so anyway for backwards compatibility.
            if (m_ModelParent == null)
            {
                m_ModelParent = new GameObject($"[{gameObject.name}] Model Parent").transform;
                m_ModelParent.SetParent(transform);
                m_ModelParent.localPosition = Vector3.zero;
                m_ModelParent.localRotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void OnEnable()
        {
            Application.onBeforeRender += OnBeforeRender;
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void OnDisable()
        {
            Application.onBeforeRender -= OnBeforeRender;
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void Update()
        {
            UpdateController();
        }

        void SetupModel()
        {
            if (m_Model == null)
            {
                var prefab = GetModelPrefab();
                if (prefab != null)
                    m_Model = Instantiate(prefab, m_ModelParent).transform;
            }

            if (m_Model != null)
                m_Model.gameObject.SetActive(!m_HideControllerModel);
        }

        void SetupControllerState()
        {
            if (m_ControllerState == null && m_CreateControllerState)
                m_ControllerState = new XRControllerState();
        }

        /// <summary>
        /// Gets the prefab that should be instantiated upon startup.
        /// </summary>
        /// <returns>Returns the prefab that should be instantiated upon startup.</returns>
        protected virtual GameObject GetModelPrefab()
        {
            return m_ModelPrefab != null ? m_ModelPrefab.gameObject : null;
        }

        /// <summary>
        /// Updates the controller every frame.
        /// This is called automatically from <see cref="Update"/>.
        /// </summary>
        protected virtual void UpdateController()
        {
            if (m_PerformSetup)
            {
                SetupModel();
                SetupControllerState();
                m_PerformSetup = false;
            }

            if (m_EnableInputTracking &&
                (m_UpdateTrackingType == UpdateType.Update ||
                    m_UpdateTrackingType == UpdateType.UpdateAndBeforeRender))
            {
                UpdateTrackingInput(m_ControllerState);
            }

            if (m_EnableInputActions)
            {
                UpdateInput(m_ControllerState);
                UpdateControllerModelAnimation();
            }

            ApplyControllerState(XRInteractionUpdateOrder.UpdatePhase.Dynamic, m_ControllerState);
        }

        /// <summary>
        /// This method is automatically called for "Just Before Render" input updates for VR devices.
        /// </summary>
        /// <seealso cref="Application.onBeforeRender"/>
        protected virtual void OnBeforeRender()
        {
            if (m_EnableInputTracking &&
                (m_UpdateTrackingType == UpdateType.BeforeRender ||
                    m_UpdateTrackingType == UpdateType.UpdateAndBeforeRender))
            {
                UpdateTrackingInput(m_ControllerState);
            }

            ApplyControllerState(XRInteractionUpdateOrder.UpdatePhase.OnBeforeRender, m_ControllerState);
        }

        /// <summary>
        /// Applies the given controller state to this <see cref="XRBaseController"/>.
        /// Depending on the update phase, the XR Interaction states may be copied
        /// and/or the pose value may be applied to the transform of the GameObject.
        /// Unity calls this automatically from <see cref="OnBeforeRender"/> and <see cref="UpdateController"/>.
        /// </summary>
        /// <param name="updatePhase">The update phase during this call.</param>
        /// <param name="controllerState">The state of the controller to apply.</param>
        protected virtual void ApplyControllerState(XRInteractionUpdateOrder.UpdatePhase updatePhase, XRControllerState controllerState)
        {
            if (controllerState == null)
                return;

            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
            {
                // Sync the controller actions from the interaction state in the controller
                m_SelectInteractionState = controllerState.selectInteractionState;
                m_ActivateInteractionState = controllerState.activateInteractionState;
                m_UIPressInteractionState = controllerState.uiPressInteractionState;
            }

            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic ||
                updatePhase == XRInteractionUpdateOrder.UpdatePhase.OnBeforeRender)
            {
                if ((controllerState.inputTrackingState & InputTrackingState.Position) != 0)
                {
                    transform.localPosition = controllerState.position;
                }

                if ((controllerState.inputTrackingState & InputTrackingState.Rotation) != 0)
                {
                    transform.localRotation = controllerState.rotation;
                }
            }
        }

        /// <summary>
        /// Updates the pose values in the given controller state based on the current tracking input of the controller device.
        /// Unity calls this automatically from <see cref="OnBeforeRender"/> and <see cref="UpdateController"/> so explicit calls
        /// to this function are not required.
        /// </summary>
        /// <param name="controllerState">The state of the controller.</param>
        protected virtual void UpdateTrackingInput(XRControllerState controllerState)
        {
        }

        /// <summary>
        /// Updates the XR Interaction states in the given controller state based on the current inputs of the controller device.
        /// Unity calls this automatically during <see cref="UpdateController"/> so explicit calls to this function are not required.
        /// </summary>
        /// <param name="controllerState">The state of the controller.</param>
        protected virtual void UpdateInput(XRControllerState controllerState)
        {
        }

        /// <summary>
        /// Updates the animation on the model instance (if the model contains an <see cref="Animator"/>).
        /// Unity calls this automatically from <see cref="UpdateController"/>.
        /// </summary>
        /// <seealso cref="animateModel"/>
        /// <seealso cref="modelSelectTransition"/>
        /// <seealso cref="modelDeSelectTransition"/>
        protected virtual void UpdateControllerModelAnimation()
        {
#if ANIMATION_MODULE_PRESENT
            if (m_AnimateModel && m_Model != null)
            {
                // Update the Animator reference if necessary
                if (m_ModelAnimator == null || m_ModelAnimator.gameObject != m_Model.gameObject)
                {
                    if (!m_Model.TryGetComponent(out m_ModelAnimator))
                    {
                        if (!m_HasWarnedAnimatorMissing)
                        {
                            Debug.LogWarning("Animate Model is enabled, but there is no Animator component on the model." +
                                " Unable to activate named triggers to animate the model.", this);
                            m_HasWarnedAnimatorMissing = true;
                        }

                        return;
                    }
                }

                if (m_SelectInteractionState.activatedThisFrame)
                    m_ModelAnimator.SetTrigger(m_ModelSelectTransition);
                else if (m_SelectInteractionState.deactivatedThisFrame)
                    m_ModelAnimator.SetTrigger(m_ModelDeSelectTransition);
            }
#endif
        }

        /// <summary>
        /// Play a haptic impulse on the controller if one is available.
        /// </summary>
        /// <param name="amplitude">Amplitude (from 0.0 to 1.0) to play impulse at.</param>
        /// <param name="duration">Duration (in seconds) to play haptic impulse.</param>
        /// <returns>Returns <see langword="true"/> if successful. Otherwise, returns <see langword="false"/>.</returns>
        public virtual bool SendHapticImpulse(float amplitude, float duration) => false;
    }
}
