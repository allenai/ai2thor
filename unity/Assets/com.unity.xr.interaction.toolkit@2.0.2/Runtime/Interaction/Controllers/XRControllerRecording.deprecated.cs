using System;

namespace UnityEngine.XR.Interaction.Toolkit
{
    public partial class XRControllerRecording
    {
#pragma warning disable 618
        /// <summary>
        /// (Deprecated) Adds a recording of a frame.
        /// </summary>
        /// <param name="time">The time for this controller frame.</param>
        /// <param name="position">The position for this controller frame.</param>
        /// <param name="rotation">The rotation for this controller frame.</param>
        /// <param name="selectActive">Whether select is active or not.</param>
        /// <param name="activateActive">Whether activate is active or not.</param>
        /// <param name="pressActive">Whether press is active or not.</param>
        /// <seealso cref="AddRecordingFrame(XRControllerState)"/>
        /// <seealso cref="AddRecordingFrameNonAlloc"/>
        [Obsolete("AddRecordingFrame has been deprecated. Use the overload method with the XRControllerState parameter or the method AddRecordingFrameNonAlloc.")]
        public void AddRecordingFrame(
            double time, Vector3 position, Quaternion rotation, bool selectActive, bool activateActive, bool pressActive)
        {
            AddRecordingFrameNonAlloc(new XRControllerState(time, position, rotation, InputTrackingState.Position | InputTrackingState.Rotation, selectActive, activateActive, pressActive));
        }
#pragma warning restore 618
    }
}
