using System;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.XR.Interaction.Toolkit
{
    /// <summary>
    /// The <see cref="XRControllerRecording"/> <see cref="ScriptableObject"/> stores position, rotation,
    /// and Interaction state changes from the XR Controller for playback.
    /// </summary>
    [CreateAssetMenu(menuName = "XR/XR Controller Recording")]
    [Serializable, PreferBinarySerialization]
    [HelpURL(XRHelpURLConstants.k_XRControllerRecording)]
    public partial class XRControllerRecording : ScriptableObject, ISerializationCallbackReceiver
    {
        /// <summary>
        /// Whether the selection interaction was activated in the first frame.
        /// Used to proper deserialize the first frame.
        /// </summary>
        [SerializeField]
        bool m_SelectActivatedInFirstFrame;
        
        /// <summary>
        /// Whether the activate interaction was activated in the first frame.
        /// Used to proper deserialize the first frame.
        /// </summary>
        [SerializeField]
        bool m_ActivateActivatedInFirstFrame;
        
        /// <summary>
        /// Whether the UI press interaction was activated in the first frame.
        /// Used to proper deserialize the first frame.
        /// </summary>
        [SerializeField]
        bool m_FirstUIPressActivatedInFirstFrame;
        
        [SerializeField]
#pragma warning disable IDE0044 // Add readonly modifier -- readonly fields cannot be serialized by Unity
        List<XRControllerState> m_Frames = new List<XRControllerState>();
#pragma warning restore IDE0044

        /// <summary>
        /// (Read Only) Frames stored in this recording.
        /// </summary>
        public List<XRControllerState> frames => m_Frames;

        /// <summary>
        /// (Read Only) Total playback time for this recording.
        /// </summary>
        public double duration => m_Frames.Count == 0 ? 0d : m_Frames[m_Frames.Count - 1].time;

        /// <summary>
        /// See <see cref="ISerializationCallbackReceiver.OnBeforeSerialize"/>.
        /// </summary>
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (m_Frames == null || m_Frames.Count <= 0)
                return;

            var firstFrame = m_Frames[0];
            m_SelectActivatedInFirstFrame = firstFrame.selectInteractionState.activatedThisFrame;
            m_ActivateActivatedInFirstFrame = firstFrame.activateInteractionState.activatedThisFrame;
            m_FirstUIPressActivatedInFirstFrame = firstFrame.uiPressInteractionState.activatedThisFrame;
        }

        /// <summary>
        /// See <see cref="ISerializationCallbackReceiver.OnAfterDeserialize"/>.
        /// </summary>
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (m_Frames == null || m_Frames.Count <= 0)
                return;

            var firstFrame = m_Frames[0];
            firstFrame.selectInteractionState.SetFrameDependent(!m_SelectActivatedInFirstFrame && firstFrame.selectInteractionState.active);
            firstFrame.activateInteractionState.SetFrameDependent(!m_ActivateActivatedInFirstFrame && firstFrame.activateInteractionState.active);
            firstFrame.uiPressInteractionState.SetFrameDependent(!m_FirstUIPressActivatedInFirstFrame && firstFrame.uiPressInteractionState.active);

            for (var i = 1; i < m_Frames.Count; i++)
            {
                var frame = m_Frames[i];
                var previousFrame = m_Frames[i - 1];
                frame.selectInteractionState.SetFrameDependent(previousFrame.selectInteractionState.active);
                frame.activateInteractionState.SetFrameDependent(previousFrame.activateInteractionState.active);
                frame.uiPressInteractionState.SetFrameDependent(previousFrame.uiPressInteractionState.active);
            }
        }

        /// <summary>
        /// Adds a recording of a frame.
        /// Duplicates the supplied <paramref name="state"/> object and adds the copy as a frame recording.
        /// </summary>
        /// <param name="state"> The <seealso cref="XRControllerState"/> to be recorded.</param>
        /// <seealso cref="AddRecordingFrameNonAlloc"/>
        public void AddRecordingFrame(XRControllerState state)
        {
            frames.Add(new XRControllerState(state));
        }
        
        /// <summary>
        /// Adds a recording of a frame.
        /// Adds the supplied <paramref name="state"/> object as a frame recording; does not allocate new memory.
        /// </summary>
        /// <param name="state"> The <seealso cref="XRControllerState"/> to be recorded.</param>
        /// <seealso cref="AddRecordingFrame(XRControllerState)"/>
        public void AddRecordingFrameNonAlloc(XRControllerState state)
        {
            frames.Add(state);
        }

        /// <summary>
        /// Initializes this recording by clearing all frames stored.
        /// </summary>
        public void InitRecording()
        {
            m_SelectActivatedInFirstFrame = false;
            m_ActivateActivatedInFirstFrame = false;
            m_FirstUIPressActivatedInFirstFrame = false;
            m_Frames.Clear();
#if UNITY_EDITOR
            Undo.RecordObject(this, "Recording XR Controller");
#endif
        }

        /// <summary>
        /// Saves this recording and writes to disk.
        /// </summary>
        public void SaveRecording()
        {
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
#endif
        }
    }
}
