using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace UnityEngine.XR.Interaction.Toolkit.UI
{
    /// <summary>
    /// Models a tracked device.
    /// </summary>
    public partial struct TrackedDeviceModel
    {
        const float k_DefaultMaxRaycastDistance = 1000f;

        internal struct ImplementationData
        {
            /// <summary>
            /// This tracks the current GUI targets being hovered over.
            /// </summary>
            /// <seealso cref="PointerEventData.hovered"/>
            public List<GameObject> hoverTargets { get; set; }

            /// <summary>
            /// Tracks the current enter/exit target being hovered over at any given moment.
            /// </summary>
            /// <seealso cref="PointerEventData.pointerEnter"/>
            public GameObject pointerTarget { get; set; }

            /// <summary>
            /// Whether or not the current mouse button is being dragged.
            /// </summary>
            /// <seealso cref="PointerEventData.dragging"/>
            public bool isDragging { get; set; }

            /// <summary>
            /// The last time this button was pressed.
            /// </summary>
            /// <seealso cref="PointerEventData.clickTime"/>
            public float pressedTime { get; set; }

            /// <summary>
            /// The position on the screen.
            /// </summary>
            /// <seealso cref="PointerEventData.position"/>
            public Vector2 position { get; set; }

            /// <summary>
            /// The position on the screen that this button was last pressed.
            /// In the same scale as <see cref="position"/>, and caches the same value as <see cref="PointerEventData.pressPosition"/>.
            /// </summary>
            /// <seealso cref="PointerEventData.pressPosition"/>
            public Vector2 pressedPosition { get; set; }

            /// <summary>
            /// The Raycast data from the time it was pressed.
            /// </summary>
            /// <seealso cref="PointerEventData.pointerPressRaycast"/>
            public RaycastResult pressedRaycast { get; set; }

            /// <summary>
            /// The last GameObject pressed on that can handle press or click events.
            /// </summary>
            /// <seealso cref="PointerEventData.pointerPress"/>
            public GameObject pressedGameObject { get; set; }

            /// <summary>
            /// The last GameObject pressed on regardless of whether it can handle events or not.
            /// </summary>
            /// <seealso cref="PointerEventData.rawPointerPress"/>
            public GameObject pressedGameObjectRaw { get; set; }

            /// <summary>
            /// The GameObject currently being dragged if any.
            /// </summary>
            /// <seealso cref="PointerEventData.pointerDrag"/>
            public GameObject draggedGameObject { get; set; }

            /// <summary>
            /// Resets this object to its default, unused state.
            /// </summary>
            public void Reset()
            {
                isDragging = false;
                pressedTime = 0f;
                position = Vector2.zero;
                pressedPosition = Vector2.zero;
                pressedRaycast = new RaycastResult();
                pressedGameObject = null;
                pressedGameObjectRaw = null;
                draggedGameObject = null;

                if (hoverTargets == null)
                    hoverTargets = new List<GameObject>();
                else
                    hoverTargets.Clear();
            }
        }

        ImplementationData m_ImplementationData;

        internal ImplementationData implementationData => m_ImplementationData;

        /// <summary>
        /// (Read Only) A unique Id to identify this model from others within the UI system.
        /// </summary>
        public int pointerId { get; }

        bool m_SelectDown;

        /// <summary>
        /// Whether or not the model should be selecting UI at this moment. This is the equivalent of left mouse down for a mouse.
        /// </summary>
        public bool select
        {
            get => m_SelectDown;
            set
            {
                if (m_SelectDown != value)
                {
                    m_SelectDown = value;
                    selectDelta |= value ? ButtonDeltaState.Pressed : ButtonDeltaState.Released;
                    changedThisFrame = true;
                }
            }
        }

        /// <summary>
        /// Whether the state of the select option has changed this frame.
        /// </summary>
        public ButtonDeltaState selectDelta { get; private set; }

        /// <summary>
        /// Checks whether this model has meaningfully changed this frame.
        /// This is used by the UI system to avoid excessive work. Use <see cref="OnFrameFinished"/> to reset.
        /// </summary>
        public bool changedThisFrame { get; private set; }

        Vector3 m_Position;

        /// <summary>
        /// The world starting position of the cast for the tracked device.
        /// </summary>
        public Vector3 position
        {
            get => m_Position;
            set
            {
                if (m_Position != value)
                {
                    m_Position = value;
                    changedThisFrame = true;
                }
            }
        }

        Quaternion m_Orientation;

        /// <summary>
        /// The world starting orientation of the cast for the tracked device.
        /// </summary>
        public Quaternion orientation
        {
            get => m_Orientation;
            set
            {
                if (m_Orientation != value)
                {
                    m_Orientation = value;
                    changedThisFrame = true;
                }
            }
        }

        List<Vector3> m_RaycastPoints;

        /// <summary>
        /// A series of Ray segments used to hit UI.
        /// </summary>
        /// <remarks>
        /// A polygonal chain represented by a list of endpoints which form line segments
        /// to approximate the curve. Each line segment is where the ray cast starts and ends.
        /// World space coordinates.
        /// </remarks>
        public List<Vector3> raycastPoints
        {
            get => m_RaycastPoints;
            set
            {
                changedThisFrame |= m_RaycastPoints.Count != value.Count;
                m_RaycastPoints = value;
            }
        }

        /// <summary>
        /// The last ray cast done for this model.
        /// </summary>
        /// <seealso cref="PointerEventData.pointerCurrentRaycast"/>
        public RaycastResult currentRaycast { get; private set; }

        /// <summary>
        /// The endpoint index within the list of ray cast points that the <see cref="currentRaycast"/> refers to when a hit occurred.
        /// Otherwise, a value of <c>0</c> if no hit occurred.
        /// </summary>
        /// <seealso cref="currentRaycast"/>
        /// <seealso cref="raycastPoints"/>
        /// <seealso cref="TrackedDeviceEventData.rayHitIndex"/>
        public int currentRaycastEndpointIndex { get; private set; }

        LayerMask m_RaycastLayerMask;

        /// <summary>
        /// Layer mask for ray casts.
        /// </summary>
        public LayerMask raycastLayerMask
        {
            get => m_RaycastLayerMask;
            set
            {
                if (m_RaycastLayerMask != value)
                {
                    changedThisFrame = true;
                    m_RaycastLayerMask = value;
                }
            }
        }

        Vector2 m_ScrollDelta;

        /// <summary>
        /// The amount of scroll since the last update.
        /// </summary>
        /// <seealso cref="PointerEventData.scrollDelta"/>
        public Vector2 scrollDelta
        {
            get => m_ScrollDelta;
            set
            {
                if (m_ScrollDelta != value)
                {
                    m_ScrollDelta = value;
                    changedThisFrame = true;
                }
            }
        }

        /// <summary>
        /// Initializes and returns an instance of <see cref="TrackedDeviceModel"/>.
        /// </summary>
        /// <param name="pointerId">The pointer id.</param>
        public TrackedDeviceModel(int pointerId) : this()
        {
            this.pointerId = pointerId;
#pragma warning disable 618 // Setting deprecated property, this will be removed once the property is removed
            maxRaycastDistance = k_DefaultMaxRaycastDistance;
#pragma warning restore 618
            m_RaycastPoints = new List<Vector3>();
            m_ImplementationData = new ImplementationData();

            Reset();
        }

        /// <summary>
        /// Resets this object back to defaults.
        /// </summary>
        /// <param name="resetImplementation">If <see langword="false"/>, will reset only the external state of the object, and not internal, UI-used variables. Defaults to <see langword="true"/>.</param>
        public void Reset(bool resetImplementation = true)
        {
            m_Orientation = Quaternion.identity;
            m_Position = Vector3.zero;
            changedThisFrame = false;
            m_SelectDown = false;
            selectDelta = ButtonDeltaState.NoChange;
            m_RaycastPoints.Clear();
            currentRaycastEndpointIndex = 0;
            m_RaycastLayerMask = Physics.DefaultRaycastLayers;
            m_ScrollDelta = Vector2.zero;

            if (resetImplementation)
                m_ImplementationData.Reset();
        }

        /// <summary>
        /// To be called at the end of each frame to reset any tracking of changes within the frame.
        /// </summary>
        /// <seealso cref="selectDelta"/>
        /// <seealso cref="changedThisFrame"/>
        public void OnFrameFinished()
        {
            selectDelta = ButtonDeltaState.NoChange;
            m_ScrollDelta = Vector2.zero;
            changedThisFrame = false;
        }

        /// <summary>
        /// Copies data from this model to the UI Event Data.
        /// </summary>
        /// <param name="eventData">The event that copies the data.</param>
        /// <seealso cref="CopyFrom"/>
        public void CopyTo(TrackedDeviceEventData eventData)
        {
            eventData.rayPoints = m_RaycastPoints;
            eventData.layerMask = m_RaycastLayerMask;
            eventData.pointerId = pointerId;
            eventData.scrollDelta = m_ScrollDelta;

            eventData.pointerEnter = m_ImplementationData.pointerTarget;
            eventData.dragging = m_ImplementationData.isDragging;
            eventData.clickTime = m_ImplementationData.pressedTime;
            eventData.position = m_ImplementationData.position;
            eventData.pressPosition = m_ImplementationData.pressedPosition;
            eventData.pointerPressRaycast = m_ImplementationData.pressedRaycast;
            eventData.pointerPress = m_ImplementationData.pressedGameObject;
            eventData.rawPointerPress = m_ImplementationData.pressedGameObjectRaw;
            eventData.pointerDrag = m_ImplementationData.draggedGameObject;
            eventData.hovered.Clear();
            eventData.hovered.AddRange(m_ImplementationData.hoverTargets);
        }

        /// <summary>
        /// Copies data from the UI Event Data to this model.
        /// </summary>
        /// <param name="eventData">The data to copy from.</param>
        /// <seealso cref="CopyTo"/>
        public void CopyFrom(TrackedDeviceEventData eventData)
        {
            m_ImplementationData.pointerTarget = eventData.pointerEnter;
            m_ImplementationData.isDragging = eventData.dragging;
            m_ImplementationData.pressedTime = eventData.clickTime;
            m_ImplementationData.position = eventData.position;
            m_ImplementationData.pressedPosition = eventData.pressPosition;
            m_ImplementationData.pressedRaycast = eventData.pointerPressRaycast;
            m_ImplementationData.pressedGameObject = eventData.pointerPress;
            m_ImplementationData.pressedGameObjectRaw = eventData.rawPointerPress;
            m_ImplementationData.draggedGameObject = eventData.pointerDrag;
            m_ImplementationData.hoverTargets.Clear();
            m_ImplementationData.hoverTargets.AddRange(eventData.hovered);

            currentRaycast = eventData.pointerCurrentRaycast;
            currentRaycastEndpointIndex = eventData.rayHitIndex;
        }

        // this only exists to clean up a warning in the .deprecated.cs file for this type - when removing that file, remove this field
        float m_MaxRaycastDistance;

        internal static TrackedDeviceModel invalid { get; } = new TrackedDeviceModel(-1);
    }
}
