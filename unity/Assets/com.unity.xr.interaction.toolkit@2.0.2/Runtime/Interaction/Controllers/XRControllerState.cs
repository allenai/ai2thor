using System;

namespace UnityEngine.XR.Interaction.Toolkit
{
    /// <summary>
    /// <see cref="InteractionState"/> type to hold current state for a given interaction.
    /// </summary>
    [Serializable]
    public partial struct InteractionState
    {
        [Range(0f, 1f)]
        [SerializeField]
        float m_Value;

        /// <summary>
        /// The value of the interaction in this frame.
        /// </summary>
        public float value
        {
            get => m_Value;
            set => m_Value = value;
        }
        
        [SerializeField]
        bool m_Active;

        /// <summary>
        /// Whether it is currently on.
        /// </summary>
        public bool active
        {
            get => m_Active;
            set => m_Active = value;
        }

        bool m_ActivatedThisFrame;

        /// <summary>
        /// Whether the interaction state activated this frame.
        /// </summary>
        public bool activatedThisFrame
        {
            get => m_ActivatedThisFrame;
            set => m_ActivatedThisFrame = value;
        }

        bool m_DeactivatedThisFrame;

        /// <summary>
        /// Whether the interaction state deactivated this frame.
        /// </summary>
        public bool deactivatedThisFrame
        {
            get => m_DeactivatedThisFrame;
            set => m_DeactivatedThisFrame = value;
        }

        /// <summary>
        /// Sets the interaction state for this frame. This method should only be called once per frame.
        /// </summary>
        /// <param name="isActive">Whether the state is active (in other words, pressed).</param>
        public void SetFrameState(bool isActive)
        {
            SetFrameState(isActive, isActive ? 1f : 0f);
        }

        /// <summary>
        /// Sets the interaction state for this frame. This method should only be called once per frame.
        /// </summary>
        /// <param name="isActive">Whether the state is active (in other words, pressed).</param>
        /// <param name="newValue">The interaction value.</param>
        public void SetFrameState(bool isActive, float newValue)
        {
            value = newValue;
            activatedThisFrame = !active && isActive;
            deactivatedThisFrame = active && !isActive;
            active = isActive;
        }

        /// <summary>
        /// Sets the interaction state that are based on whether they occurred "this frame".
        /// </summary>
        /// <param name="wasActive">Whether the previous state is active (in other words, pressed).</param>
        public void SetFrameDependent(bool wasActive)
        {
            activatedThisFrame = !wasActive && active;
            deactivatedThisFrame = wasActive && !active;
        }

        /// <summary>
        /// Resets the interaction states that are based on whether they occurred "this frame".
        /// </summary>
        /// <seealso cref="activatedThisFrame"/>
        /// <seealso cref="deactivatedThisFrame"/>
        public void ResetFrameDependent()
        {
            activatedThisFrame = false;
            deactivatedThisFrame = false;
        }
    }

    /// <summary>
    /// Represents the current state of the <see cref="XRBaseController"/>.
    /// </summary>
    [Serializable]
    public partial class XRControllerState
    {
        /// <summary>
        /// The time value for this controller.
        /// </summary>
        public double time;

        /// <summary>
        /// The input tracking state of the controller.
        /// </summary>
        public InputTrackingState inputTrackingState;

        /// <summary>
        /// The position of the controller.
        /// </summary>
        public Vector3 position;

        /// <summary>
        /// The rotation of the controller.
        /// </summary>
        public Quaternion rotation;

        /// <summary>
        /// The selection interaction state.
        /// </summary>
        public InteractionState selectInteractionState;

        /// <summary>
        /// The activate interaction state.
        /// </summary>
        public InteractionState activateInteractionState;

        /// <summary>
        /// The UI press interaction state.
        /// </summary>
        public InteractionState uiPressInteractionState;

        /// <summary>
        /// Initializes and returns an instance of <see cref="XRControllerState"/>.
        /// </summary>
        /// <param name="time">The time value for this controller.</param>
        /// <param name="position">The position for this controller.</param>
        /// <param name="rotation">The rotation for this controller.</param>
        /// <param name="inputTrackingState">The inputTrackingState for this controller.</param>
        protected XRControllerState(double time, Vector3 position, Quaternion rotation, InputTrackingState inputTrackingState)
        {
            this.time = time;
            this.position = position;
            this.rotation = rotation;
            this.inputTrackingState = inputTrackingState;
        }
        
        /// <summary>
        /// Initializes and returns an instance of <see cref="XRControllerState"/>.
        /// </summary>
        public XRControllerState() : this (0d, Vector3.zero, Quaternion.identity, InputTrackingState.None)
        {
        }

        /// <summary>
        /// Initializes and returns an instance of <see cref="XRControllerState"/>.
        /// </summary>
        /// <param name="value"> The <see cref="XRControllerState"/> object used to create this object.</param>
        public XRControllerState(XRControllerState value)
        {
            this.time = value.time;
            this.position = value.position;
            this.rotation = value.rotation;
            this.inputTrackingState = value.inputTrackingState;
            this.selectInteractionState = value.selectInteractionState;
            this.activateInteractionState = value.activateInteractionState;
            this.uiPressInteractionState = value.uiPressInteractionState;
        }

        /// <summary>
        /// Initializes and returns an instance of <see cref="XRControllerState"/>.
        /// </summary>
        /// <param name="time">The time value for this controller.</param>
        /// <param name="position">The position for this controller.</param>
        /// <param name="rotation">The rotation for this controller.</param>
        /// <param name="inputTrackingState">The inputTrackingState for this controller.</param>
        /// <param name="selectActive">Whether select is active or not.</param>
        /// <param name="activateActive">Whether activate is active or not.</param>
        /// <param name="pressActive">Whether UI press is active or not.</param>
        public XRControllerState(double time, Vector3 position, Quaternion rotation, InputTrackingState inputTrackingState, 
            bool selectActive, bool activateActive, bool pressActive) 
            : this (time, position, rotation, inputTrackingState) 
        {
            this.selectInteractionState.SetFrameState(selectActive);
            this.activateInteractionState.SetFrameState(activateActive);
            this.uiPressInteractionState.SetFrameState(pressActive);
        }
        
        /// <summary>
        /// Initializes and returns an instance of <see cref="XRControllerState"/>.
        /// </summary>
        /// <param name="time">The time value for this controller.</param>
        /// <param name="position">The position for this controller.</param>
        /// <param name="rotation">The rotation for this controller.</param>
        /// <param name="inputTrackingState">The inputTrackingState for this controller.</param>
        /// <param name="selectActive">Whether select is active or not.</param>
        /// <param name="activateActive">Whether activate is active or not.</param>
        /// <param name="pressActive">Whether UI press is active or not.</param>
        /// <param name="selectValue">The select value.</param>
        /// <param name="activateValue">The activate value.</param>
        /// <param name="pressValue">The UI press value.</param>
        public XRControllerState(double time, Vector3 position, Quaternion rotation, InputTrackingState inputTrackingState, 
            bool selectActive, bool activateActive, bool pressActive,
            float selectValue, float activateValue, float pressValue)
            : this(time, position, rotation, inputTrackingState)
        {
            this.selectInteractionState.SetFrameState(selectActive, selectValue);
            this.activateInteractionState.SetFrameState(activateActive, activateValue);
            this.uiPressInteractionState.SetFrameState(pressActive, pressValue);
        }

        /// <summary>
        /// Resets all the interaction states that are based on whether they occurred "this frame".
        /// </summary>
        /// <seealso cref="InteractionState.ResetFrameDependent"/>
        public void ResetFrameDependentStates()
        {
            selectInteractionState.ResetFrameDependent();
            activateInteractionState.ResetFrameDependent();
            uiPressInteractionState.ResetFrameDependent();
        }

        /// <summary>
        /// Converts state data to a string.
        /// </summary>
        /// <returns>A string representation.</returns>
        public override string ToString() => $"time: {time}, position: {position}, rotation: {rotation}, selectActive: {selectInteractionState.active}, activateActive: {activateInteractionState.active}, pressActive: {uiPressInteractionState.active}";
    }
}
