using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;

namespace UnityEngine.XR.Interaction.Toolkit.UI
{
    /// <summary>
    /// A series of flags to determine if a button has been pressed or released since the last time checked.
    /// Useful for identifying press/release events that occur in a single frame or sample.
    /// </summary>
    [Flags]
    public enum ButtonDeltaState
    {
        /// <summary>
        /// No change since last time checked.
        /// </summary>
        NoChange = 0,

        /// <summary>
        /// Button pressed since last time checked.
        /// </summary>
        Pressed = 1 << 0,

        /// <summary>
        /// Button released since last time checked.
        /// </summary>
        Released = 1 << 1,
    }

    /// <summary>
    /// Represents the state of a single mouse button within the Unity UI (UGUI) system. Keeps track of various book-keeping regarding clicks, drags, and presses.
    /// Can be converted to and from PointerEventData for sending into Unity UI (UGUI).
    /// </summary>
    public struct MouseButtonModel
    {
        internal struct ImplementationData
        {
            /// <summary>
            /// Used to cache whether or not the current mouse button is being dragged.
            /// </summary>
            /// <seealso cref="PointerEventData.dragging"/>
            public bool isDragging { get; set; }

            /// <summary>
            /// Used to cache the last time this button was pressed.
            /// </summary>
            /// <seealso cref="PointerEventData.clickTime"/>
            public float pressedTime { get; set; }

            /// <summary>
            /// The position on the screen that this button was last pressed.
            /// In the same scale as <see cref="MouseModel.position"/>, and caches the same value as <see cref="PointerEventData.pressPosition"/>.
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
                pressedPosition = Vector2.zero;
                pressedRaycast = new RaycastResult();
                pressedGameObject = pressedGameObjectRaw = draggedGameObject = null;
            }
        }

        /// <summary>
        /// Used to store the current binary state of the button. When set, will also track the changes between calls of <see cref="OnFrameFinished"/> in <see cref="lastFrameDelta"/>.
        /// </summary>
        public bool isDown
        {
            get => m_IsDown;
            set
            {
                if (m_IsDown != value)
                {
                    m_IsDown = value;
                    lastFrameDelta |= value ? ButtonDeltaState.Pressed : ButtonDeltaState.Released;
                }
            }
        }

        /// <summary>
        /// A set of flags to identify the changes that have occurred between calls of <see cref="OnFrameFinished"/>.
        /// </summary>
        internal ButtonDeltaState lastFrameDelta { get; private set; }

        /// <summary>
        /// Resets this object to it's default, unused state.
        /// </summary>
        public void Reset()
        {
            lastFrameDelta = ButtonDeltaState.NoChange;
            m_IsDown = false;

            m_ImplementationData.Reset();
        }

        /// <summary>
        /// Call this on each frame in order to reset properties that detect whether or not a certain condition was met this frame.
        /// </summary>
        public void OnFrameFinished() => lastFrameDelta = ButtonDeltaState.NoChange;

        /// <summary>
        /// Fills a <see cref="PointerEventData"/> with this mouse button's internally cached values.
        /// </summary>
        /// <param name="eventData">These objects are used to send data through the Unity UI (UGUI) system.</param>
        public void CopyTo(PointerEventData eventData)
        {
            eventData.dragging = m_ImplementationData.isDragging;
            eventData.clickTime = m_ImplementationData.pressedTime;
            eventData.pressPosition = m_ImplementationData.pressedPosition;
            eventData.pointerPressRaycast = m_ImplementationData.pressedRaycast;
            eventData.pointerPress = m_ImplementationData.pressedGameObject;
            eventData.rawPointerPress = m_ImplementationData.pressedGameObjectRaw;
            eventData.pointerDrag = m_ImplementationData.draggedGameObject;
        }

        /// <summary>
        /// Fills this object with the values from a <see cref="PointerEventData"/>.
        /// </summary>
        /// <param name="eventData">These objects are used to send data through the Unity UI (UGUI) system.</param>
        public void CopyFrom(PointerEventData eventData)
        {
            m_ImplementationData.isDragging = eventData.dragging;
            m_ImplementationData.pressedTime = eventData.clickTime;
            m_ImplementationData.pressedPosition = eventData.pressPosition;
            m_ImplementationData.pressedRaycast = eventData.pointerPressRaycast;
            m_ImplementationData.pressedGameObject = eventData.pointerPress;
            m_ImplementationData.pressedGameObjectRaw = eventData.rawPointerPress;
            m_ImplementationData.draggedGameObject = eventData.pointerDrag;
        }

        bool m_IsDown;
        ImplementationData m_ImplementationData;
    }

    struct MouseModel
    {
        internal struct InternalData
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

            public void Reset()
            {
                pointerTarget = null;

                if (hoverTargets == null)
                    hoverTargets = new List<GameObject>();
                else
                    hoverTargets.Clear();
            }
        }

        /// <summary>
        /// An Id representing a unique pointer.
        /// </summary>
        public int pointerId { get; }

        /// <summary>
        /// A boolean value representing whether any mouse data has changed this frame, meaning that events should be processed.
        /// </summary>
        /// <remarks>
        /// This only checks for changes in mouse state (<see cref="position"/>, <see cref="leftButton"/>, <see cref="rightButton"/>, <see cref="middleButton"/>, or <see cref="scrollDelta"/>).
        /// </remarks>
        public bool changedThisFrame { get; private set; }

        Vector2 m_Position;

        public Vector2 position
        {
            get => m_Position;
            set
            {
                if (m_Position != value)
                {
                    deltaPosition = value - m_Position;
                    m_Position = value;
                    changedThisFrame = true;
                }
            }
        }

        /// <summary>
        /// The pixel-space change in <see cref="position"/> since the last call to <see cref="OnFrameFinished"/>.
        /// </summary>
        public Vector2 deltaPosition { get; private set; }

        Vector2 m_ScrollDelta;

        /// <summary>
        /// The amount of scroll since the last call to <see cref="OnFrameFinished"/>.
        /// </summary>
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

        MouseButtonModel m_LeftButton;

        /// <summary>
        /// Cached data and button state representing a left mouse button on a mouse.
        /// Used by Unity UI (UGUI) to keep track of persistent click, press, and drag states.
        /// </summary>
        public MouseButtonModel leftButton
        {
            get => m_LeftButton;
            set
            {
                changedThisFrame |= (value.lastFrameDelta != ButtonDeltaState.NoChange);
                m_LeftButton = value;
            }
        }

        /// <summary>
        /// Sets the pressed state of the left mouse button.
        /// </summary>
        public bool leftButtonPressed
        {
            set
            {
                changedThisFrame |= m_LeftButton.isDown != value;
                m_LeftButton.isDown = value;
            }
        }

        MouseButtonModel m_RightButton;

        /// <summary>
        /// Cached data and button state representing a right mouse button on a mouse.
        /// Unity UI (UGUI) uses this to keep track of persistent click, press, and drag states.
        /// </summary>
        public MouseButtonModel rightButton
        {
            get => m_RightButton;
            set
            {
                changedThisFrame |= (value.lastFrameDelta != ButtonDeltaState.NoChange);
                m_RightButton = value;
            }
        }

        /// <summary>
        /// Sets the pressed state of the right mouse button.
        /// </summary>
        public bool rightButtonPressed
        {
            set
            {
                changedThisFrame |= m_RightButton.isDown != value;
                m_RightButton.isDown = value;
            }
        }

        MouseButtonModel m_MiddleButton;

        /// <summary>
        /// Cached data and button state representing a middle mouse button on a mouse.
        /// Used by Unity UI (UGUI) to keep track of persistent click, press, and drag states.
        /// </summary>
        public MouseButtonModel middleButton
        {
            get => m_MiddleButton;
            set
            {
                changedThisFrame |= (value.lastFrameDelta != ButtonDeltaState.NoChange);
                m_MiddleButton = value;
            }
        }

        /// <summary>
        /// Sets the pressed state of the middle mouse button.
        /// </summary>
        public bool middleButtonPressed
        {
            set
            {
                changedThisFrame |= m_MiddleButton.isDown != value;
                m_MiddleButton.isDown = value;
            }
        }

        InternalData m_InternalData;

        public MouseModel(int pointerId)
        {
            this.pointerId = pointerId;
            changedThisFrame = false;
            m_Position = Vector2.zero;
            deltaPosition = Vector2.zero;
            m_ScrollDelta = Vector2.zero;

            m_LeftButton = new MouseButtonModel();
            m_RightButton = new MouseButtonModel();
            m_MiddleButton = new MouseButtonModel();
            m_LeftButton.Reset();
            m_RightButton.Reset();
            m_MiddleButton.Reset();

            m_InternalData = new InternalData();
            m_InternalData.Reset();
        }

        /// <summary>
        /// Call this at the end of polling for per-frame changes.  This resets delta values, such as <see cref="deltaPosition"/>, <see cref="scrollDelta"/>, and <see cref="MouseButtonModel.lastFrameDelta"/>.
        /// </summary>
        public void OnFrameFinished()
        {
            changedThisFrame = false;
            deltaPosition = Vector2.zero;
            m_ScrollDelta = Vector2.zero;
            m_LeftButton.OnFrameFinished();
            m_RightButton.OnFrameFinished();
            m_MiddleButton.OnFrameFinished();
        }

        public void CopyTo(PointerEventData eventData)
        {
            eventData.pointerId = pointerId;
            eventData.position = position;
            eventData.delta = deltaPosition;
            eventData.scrollDelta = scrollDelta;

            eventData.pointerEnter = m_InternalData.pointerTarget;
            eventData.hovered.Clear();
            eventData.hovered.AddRange(m_InternalData.hoverTargets);
        }

        public void CopyFrom(PointerEventData eventData)
        {
            var hoverTargets = m_InternalData.hoverTargets;
            m_InternalData.hoverTargets.Clear();
            m_InternalData.hoverTargets.AddRange(eventData.hovered);
            m_InternalData.hoverTargets = hoverTargets;
            m_InternalData.pointerTarget = eventData.pointerEnter;
        }
    }
}
