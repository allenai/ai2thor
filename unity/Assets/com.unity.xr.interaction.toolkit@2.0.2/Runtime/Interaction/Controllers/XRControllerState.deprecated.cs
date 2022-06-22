using System;
using UnityEngine.SpatialTracking;

namespace UnityEngine.XR.Interaction.Toolkit
{
    public partial class XRControllerState
    {
#pragma warning disable 618
        /// <summary>
        /// (Deprecated) The pose data flags of the controller.
        /// </summary>
        /// <seealso cref="inputTrackingState"/>
        [Obsolete("poseDataFlags has been deprecated. Use inputTrackingState instead.")]
        public PoseDataFlags poseDataFlags
        {
            get
            {
                var value = PoseDataFlags.NoData;
                
                if ((inputTrackingState & InputTrackingState.Position) != 0)
                    value |= PoseDataFlags.Position;
                if ((inputTrackingState & InputTrackingState.Rotation) != 0)
                    value |= PoseDataFlags.Rotation;
                
                return value;
            }

            set
            {
                inputTrackingState = InputTrackingState.None;

                if ((value & PoseDataFlags.Position) != 0)
                    inputTrackingState |= InputTrackingState.Position;
                if ((value & PoseDataFlags.Rotation) != 0)
                    inputTrackingState |= InputTrackingState.Rotation;
            }
        }
        
        /// <summary>
        /// (Deprecated) Initializes and returns an instance of <see cref="XRControllerState"/>.
        /// </summary>
        /// <param name="time">The time value for this controller.</param>
        /// <param name="position">The position for this controller.</param>
        /// <param name="rotation">The rotation for this controller.</param>
        /// <param name="selectActive">Whether select is active or not.</param>
        /// <param name="activateActive">Whether activate is active or not.</param>
        /// <param name="pressActive">Whether UI press is active or not.</param>
        [Obsolete("This constructor has been deprecated. Use the constructors with the inputTrackingState parameter.")]
        public XRControllerState(double time, Vector3 position, Quaternion rotation, bool selectActive, bool activateActive, bool pressActive)
            : this(time, position, rotation, InputTrackingState.Rotation | InputTrackingState.Position, selectActive, activateActive, pressActive)
        {
        }
#pragma warning restore 618

        /// <summary>
        /// (Deprecated) Resets all the interaction states that are based on whether they occurred "this frame".
        /// </summary>
        /// <remarks>
        /// <c>ResetInputs</c> has been renamed. Use <see cref="ResetFrameDependentStates"/> instead.
        /// </remarks>
        [Obsolete("ResetInputs has been renamed. Use ResetFrameDependentStates instead. (UnityUpgradable) -> ResetFrameDependentStates()")]
        public void ResetInputs() => ResetFrameDependentStates();
    }

    public partial struct InteractionState
    {
        /// <summary>
        /// (Deprecated) Whether the interaction state was deactivated this frame.
        /// </summary>
        /// <remarks>
        /// <c>deActivatedThisFrame</c> has been deprecated. Use <see cref="deactivatedThisFrame"/> instead.
        /// </remarks>
#pragma warning disable IDE1006 // Naming Styles
        [Obsolete("deActivatedThisFrame has been deprecated. Use deactivatedThisFrame instead. (UnityUpgradable) -> deactivatedThisFrame")]
        public bool deActivatedThisFrame
        {
            get => deactivatedThisFrame;
            set => deactivatedThisFrame = value;
        }
#pragma warning restore IDE1006

        /// <summary>
        /// (Deprecated) Resets the interaction states that are based on whether they occurred "this frame".
        /// </summary>
        /// <remarks>
        /// <c>Reset</c> has been deprecated. Use <see cref="ResetFrameDependent"/> instead.
        /// </remarks>
        [Obsolete("Reset has been renamed. Use ResetFrameDependent instead. (UnityUpgradable) -> ResetFrameDependent()")]
        public void Reset() => ResetFrameDependent();
    }
}
