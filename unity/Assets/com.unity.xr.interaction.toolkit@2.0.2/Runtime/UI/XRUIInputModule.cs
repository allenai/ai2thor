using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace UnityEngine.XR.Interaction.Toolkit.UI
{
    /// <summary>
    /// Matches the UI Model to the state of the Interactor.
    /// </summary>
    public interface IUIInteractor
    {
        /// <summary>
        /// Updates the current UI Model to match the state of the Interactor.
        /// </summary>
        /// <param name="model">The returned model that will match this Interactor.</param>
        void UpdateUIModel(ref TrackedDeviceModel model);

        /// <summary>
        /// Attempts to retrieve the current UI Model.
        /// </summary>
        /// <param name="model">The returned model that reflects the UI state of this Interactor.</param>
        /// <returns>Returns <see langword="true"/> if the model was able to retrieved. Otherwise, returns <see langword="false"/>.</returns>
        bool TryGetUIModel(out TrackedDeviceModel model);
    }

    /// <summary>
    /// Custom class for input modules that send UI input in XR.
    /// </summary>
    [AddComponentMenu("Event/XR UI Input Module", 11)]
    [HelpURL(XRHelpURLConstants.k_XRUIInputModule)]
    public partial class XRUIInputModule : UIInputModule
    {
        struct RegisteredInteractor
        {
            public IUIInteractor interactor;
            public TrackedDeviceModel model;

            public RegisteredInteractor(IUIInteractor interactor, int deviceIndex)
            {
                this.interactor = interactor;
                model = new TrackedDeviceModel(deviceIndex);
            }
        }

        struct RegisteredTouch
        {
            public bool isValid;
            public int touchId;
            public TouchModel model;

            public RegisteredTouch(Touch touch, int deviceIndex)
            {
                touchId = touch.fingerId;
                model = new TouchModel(deviceIndex);
                isValid = true;
            }
        }

        [SerializeField, HideInInspector]
        [Tooltip("The maximum distance to ray cast with tracked devices to find hit objects.")]
        float m_MaxTrackedDeviceRaycastDistance = 1000f;

        [Header("Input Devices")]
        [SerializeField]
        [Tooltip("If true, will forward 3D tracked device data to UI elements.")]
        bool m_EnableXRInput = true;

        /// <summary>
        /// If <see langword="true"/>, will forward 3D tracked device data to UI elements.
        /// </summary>
        public bool enableXRInput
        {
            get => m_EnableXRInput;
            set => m_EnableXRInput = value;
        }

        [SerializeField]
        [Tooltip("If true, will forward 2D mouse data to UI elements.")]
        bool m_EnableMouseInput = true;

        /// <summary>
        /// If <see langword="true"/>, will forward 2D mouse data to UI elements.
        /// </summary>
        public bool enableMouseInput
        {
            get => m_EnableMouseInput;
            set => m_EnableMouseInput = value;
        }

        [SerializeField]
        [Tooltip("If true, will forward 2D touch data to UI elements.")]
        bool m_EnableTouchInput = true;

        /// <summary>
        /// If <see langword="true"/>, will forward 2D touch data to UI elements.
        /// </summary>
        public bool enableTouchInput
        {
            get => m_EnableTouchInput;
            set => m_EnableTouchInput = value;
        }

        [SerializeField]
        [Tooltip("If true, will forward gamepad data to UI elements.")]
        bool m_EnableGamepadInput = true;

        /// <summary>
        /// If <see langword="true"/>, will forward gamepad data to UI elements.
        /// </summary>
        public bool enableGamepadInput
        {
            get => m_EnableGamepadInput;
            set => m_EnableGamepadInput = value;
        }

        [SerializeField]
        [Tooltip("If true, will forward joystick data to UI elements.")]
        bool m_EnableJoystickInput = true;

        /// <summary>
        /// If <see langword="true"/>, will forward joystick data to UI elements.
        /// </summary>
        public bool enableJoystickInput
        {
            get => m_EnableJoystickInput;
            set => m_EnableJoystickInput = value;
        }

        [Header("Input Manager (Old) Gamepad/Joystick Bindings")]
        [SerializeField]
        [Tooltip("Name of the horizontal axis for gamepad/joystick UI navigation when using the old Input Manager.")]
        string m_HorizontalAxis = "Horizontal";

        /// <summary>
        /// Name of the horizontal axis for UI navigation when using the old Input Manager.
        /// </summary>
        public string horizontalAxis
        {
            get => m_HorizontalAxis;
            set => m_HorizontalAxis = value;
        }

        [SerializeField]
        [Tooltip("Name of the vertical axis for gamepad/joystick UI navigation when using the old Input Manager.")]
        string m_VerticalAxis = "Vertical";

        /// <summary>
        /// Name of the vertical axis for UI navigation when using the old Input Manager.
        /// </summary>
        public string verticalAxis
        {
            get => m_VerticalAxis;
            set => m_VerticalAxis = value;
        }

        [SerializeField]
        [Tooltip("Name of the gamepad/joystick button to use for UI selection or submission when using the old Input Manager.")]
        string m_SubmitButton = "Submit";

        /// <summary>
        /// Name of the gamepad/joystick button to use for UI selection or submission when using the old Input Manager.
        /// </summary>
        public string submitButton
        {
            get => m_SubmitButton;
            set => m_SubmitButton = value;
        }

        [SerializeField]
        [Tooltip("Name of the gamepad/joystick button to use for UI cancel or back commands when using the old Input Manager.")]
        string m_CancelButton = "Cancel";

        /// <summary>
        /// Name of the gamepad/joystick button to use for UI cancel or back commands when using the old Input Manager.
        /// </summary>
        public string cancelButton
        {
            get => m_CancelButton;
            set => m_CancelButton = value;
        }

        int m_RollingPointerId;

        MouseModel m_Mouse;
        GamepadModel m_Gamepad;
        JoystickModel m_Joystick;

        readonly List<RegisteredTouch> m_RegisteredTouches = new List<RegisteredTouch>();

        readonly List<RegisteredInteractor> m_RegisteredInteractors = new List<RegisteredInteractor>();

        /// <summary>
        /// See <a href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnEnable.html">MonoBehavior.OnEnable</a>.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();
            m_Mouse = new MouseModel(m_RollingPointerId++);
            m_Gamepad = new GamepadModel();
            m_Joystick = new JoystickModel();
        }

        /// <summary>
        /// See <a href="https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnDisable.html">MonoBehavior.OnDisable</a>.
        /// </summary>
        protected override void OnDisable()
        {
            RemovePointerEventData(m_Mouse.pointerId);
            base.OnDisable();
        }

        /// <summary>
        /// Register an <see cref="IUIInteractor"/> with the UI system.
        /// Calling this will enable it to start interacting with UI.
        /// </summary>
        /// <param name="interactor">The <see cref="IUIInteractor"/> to use.</param>
        public void RegisterInteractor(IUIInteractor interactor)
        {
            for (var i = 0; i < m_RegisteredInteractors.Count; i++)
            {
                if (m_RegisteredInteractors[i].interactor == interactor)
                    return;
            }

            m_RegisteredInteractors.Add(new RegisteredInteractor(interactor, m_RollingPointerId++));
        }

        /// <summary>
        /// Unregisters an <see cref="IUIInteractor"/> with the UI system.
        /// This cancels all UI Interaction and makes the <see cref="IUIInteractor"/> no longer able to affect UI.
        /// </summary>
        /// <param name="interactor">The <see cref="IUIInteractor"/> to stop using.</param>
        public void UnregisterInteractor(IUIInteractor interactor)
        {
            for (var i = 0; i < m_RegisteredInteractors.Count; i++)
            {
                if (m_RegisteredInteractors[i].interactor == interactor)
                {
                    var registeredInteractor = m_RegisteredInteractors[i];
                    registeredInteractor.interactor = null;
                    m_RegisteredInteractors[i] = registeredInteractor;
                    return;
                }
            }
        }

        /// <summary>
        /// Gets an <see cref="IUIInteractor"/> from its corresponding Unity UI Pointer Id.
        /// This can be used to identify individual Interactors from the underlying UI Events.
        /// </summary>
        /// <param name="pointerId">A unique integer representing an object that can point at UI.</param>
        /// <returns>Returns the interactor associated with <paramref name="pointerId"/>.
        /// Returns <see langword="null"/> if no Interactor is associated (e.g. if it's a mouse event).</returns>
        public IUIInteractor GetInteractor(int pointerId)
        {
            for (var i = 0; i < m_RegisteredInteractors.Count; i++)
            {
                if (m_RegisteredInteractors[i].model.pointerId == pointerId)
                {
                    return m_RegisteredInteractors[i].interactor;
                }
            }

            return null;
        }

        /// <summary>Retrieves the UI Model for a selected <see cref="IUIInteractor"/>.</summary>
        /// <param name="interactor">The <see cref="IUIInteractor"/> you want the model for.</param>
        /// <param name="model">The returned model that reflects the UI state of the <paramref name="interactor"/>.</param>
        /// <returns>Returns <see langword="true"/> if the model was able to retrieved. Otherwise, returns <see langword="false"/>.</returns>
        public bool GetTrackedDeviceModel(IUIInteractor interactor, out TrackedDeviceModel model)
        {
            for (var i = 0; i < m_RegisteredInteractors.Count; i++)
            {
                if (m_RegisteredInteractors[i].interactor == interactor)
                {
                    model = m_RegisteredInteractors[i].model;
                    return true;
                }
            }

            model = new TrackedDeviceModel(-1);
            return false;
        }

        /// <inheritdoc />
        protected override void DoProcess()
        {
            base.DoProcess();

            if (m_EnableXRInput)
            {
                for (var i = 0; i < m_RegisteredInteractors.Count; i++)
                {
                    var registeredInteractor = m_RegisteredInteractors[i];

                    // If device is removed, we send a default state to unclick any UI
                    if (registeredInteractor.interactor == null)
                    {
                        registeredInteractor.model.Reset(false);
                        ProcessTrackedDevice(ref registeredInteractor.model, true);
                        RemovePointerEventData(registeredInteractor.model.pointerId);
                        m_RegisteredInteractors.RemoveAt(i--);
                    }
                    else
                    {
                        registeredInteractor.interactor.UpdateUIModel(ref registeredInteractor.model);
                        ProcessTrackedDevice(ref registeredInteractor.model);
                        m_RegisteredInteractors[i] = registeredInteractor;
                    }
                }
            }

            // Touch needs to take precedence because of the mouse emulation layer
            var hasTouches = false;
            if (m_EnableTouchInput)
                hasTouches = ProcessTouches();

            // Process mouse input before gamepad and joystick per StandaloneInputModule (case 1004066)
            if (!hasTouches && m_EnableMouseInput)
                ProcessMouse();

            if (m_EnableGamepadInput)
                ProcessGamepad();

            if (m_EnableJoystickInput)
                ProcessJoystick();
        }

        void ProcessMouse()
        {
            if (Mouse.current != null)
            {
                // The Input System reports scroll in pixels, whereas the old Input class reported in lines.
                // Example, scrolling down by one notch of a mouse wheel for Input would be (0, -1),
                // but would be (0, -120) from Input System.
                // For consistency between the two Active Input Handling modes and with StandaloneInputModule,
                // scale the scroll value to the range expected by UI.
                const float kPixelsPerLine = 120f;
                m_Mouse.position = Mouse.current.position.ReadValue();
                m_Mouse.scrollDelta = Mouse.current.scroll.ReadValue() * (1 / kPixelsPerLine);
                m_Mouse.leftButtonPressed = Mouse.current.leftButton.isPressed;
                m_Mouse.rightButtonPressed = Mouse.current.rightButton.isPressed;
                m_Mouse.middleButtonPressed = Mouse.current.middleButton.isPressed;

                ProcessMouse(ref m_Mouse);
            }
#if ENABLE_LEGACY_INPUT_MANAGER
            else if (Input.mousePresent)
            {
                m_Mouse.position = Input.mousePosition;
                m_Mouse.scrollDelta = Input.mouseScrollDelta;
                m_Mouse.leftButtonPressed = Input.GetMouseButton(0);
                m_Mouse.rightButtonPressed = Input.GetMouseButton(1);
                m_Mouse.middleButtonPressed = Input.GetMouseButton(2);

                ProcessMouse(ref m_Mouse);
            }
#endif
        }

        bool ProcessTouches()
        {
            var hasTouches = Input.touchCount > 0;
            if (!hasTouches)
                return false;

            var touchCount = Input.touchCount;
            for (var touchIndex = 0; touchIndex < touchCount; ++touchIndex)
            {
                var touch = Input.GetTouch(touchIndex);
                var registeredTouchIndex = -1;

                // Find if touch already exists
                for (var j = 0; j < m_RegisteredTouches.Count; j++)
                {
                    if (touch.fingerId == m_RegisteredTouches[j].touchId)
                    {
                        registeredTouchIndex = j;
                        break;
                    }
                }

                if (registeredTouchIndex < 0)
                {
                    // Not found, search empty pool
                    for (var j = 0; j < m_RegisteredTouches.Count; j++)
                    {
                        if (!m_RegisteredTouches[j].isValid)
                        {
                            // Re-use the Id
                            var pointerId = m_RegisteredTouches[j].model.pointerId;
                            m_RegisteredTouches[j] = new RegisteredTouch(touch, pointerId);
                            registeredTouchIndex = j;
                            break;
                        }
                    }

                    if (registeredTouchIndex < 0)
                    {
                        // No Empty slots, add one
                        registeredTouchIndex = m_RegisteredTouches.Count;
                        m_RegisteredTouches.Add(new RegisteredTouch(touch, m_RollingPointerId++));
                    }
                }

                var registeredTouch = m_RegisteredTouches[registeredTouchIndex];
                registeredTouch.model.selectPhase = touch.phase;
                registeredTouch.model.position = touch.position;
                m_RegisteredTouches[registeredTouchIndex] = registeredTouch;
            }

            for (var i = 0; i < m_RegisteredTouches.Count; i++)
            {
                var registeredTouch = m_RegisteredTouches[i];
                ProcessTouch(ref registeredTouch.model);
                if (registeredTouch.model.selectPhase == TouchPhase.Ended || registeredTouch.model.selectPhase == TouchPhase.Canceled)
                    registeredTouch.isValid = false;
                m_RegisteredTouches[i] = registeredTouch;
            }

            return true;
        }

        void ProcessGamepad()
        {
            if (Gamepad.current != null)
            {
                m_Gamepad.leftStick = Gamepad.current.leftStick.ReadValue();
                m_Gamepad.dpad = Gamepad.current.dpad.ReadValue();
                m_Gamepad.submitButtonDown = Gamepad.current.buttonSouth.isPressed;
                m_Gamepad.cancelButtonDown = Gamepad.current.buttonEast.isPressed;

                ProcessGamepad(ref m_Gamepad);
            }
#if ENABLE_LEGACY_INPUT_MANAGER
            else if (Input.GetJoystickNames().Length > 0)
            {
                m_Gamepad.leftStick = new Vector2(Input.GetAxis(m_HorizontalAxis), Input.GetAxis(m_VerticalAxis));
                // TODO: Find best way to set dPad vector from old Input Manager
                m_Gamepad.submitButtonDown = Input.GetButton(m_SubmitButton);
                m_Gamepad.cancelButtonDown = Input.GetButton(m_CancelButton);

                ProcessGamepad(ref m_Gamepad);
            }
#endif
        }

        void ProcessJoystick()
        {
            if (Joystick.current != null)
            {
                m_Joystick.move = Joystick.current.stick.ReadValue();
                m_Joystick.hat = Joystick.current.hatswitch != null ? Joystick.current.hatswitch.ReadValue() : Vector2.zero;
                m_Joystick.submitButtonDown = Joystick.current.trigger.isPressed;
                // This will always be false until we can rely on a secondary button from the joystick
                m_Joystick.cancelButtonDown = false;

                ProcessJoystick(ref m_Joystick);
            }
#if ENABLE_LEGACY_INPUT_MANAGER
            // When using the legacy input manager, gamepad and joystick input are technically the same
            // so we need to stop processing if both checkboxes are enabled.
            else if (!m_EnableGamepadInput && Input.GetJoystickNames().Length > 0)
            {
                m_Joystick.move = new Vector2(Input.GetAxis(m_HorizontalAxis), Input.GetAxis(m_VerticalAxis));
                // TODO: Find best way to set hat vector from old Input Manager
                m_Joystick.submitButtonDown = Input.GetButton(m_SubmitButton);
                m_Joystick.cancelButtonDown = Input.GetButton(m_CancelButton);

                ProcessJoystick(ref m_Joystick);
            }
#endif
        }
    }
}
