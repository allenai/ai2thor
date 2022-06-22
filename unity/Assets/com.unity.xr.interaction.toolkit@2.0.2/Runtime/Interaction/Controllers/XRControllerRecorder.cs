namespace UnityEngine.XR.Interaction.Toolkit
{
    /// <summary>
    /// <see cref="MonoBehaviour"/> that controls interaction recording and playback (via <see cref="XRControllerRecording"/> assets).
    /// </summary>
    [AddComponentMenu("XR/Debug/XR Controller Recorder", 11)]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(XRInteractionUpdateOrder.k_ControllerRecorder)]
    [HelpURL(XRHelpURLConstants.k_XRControllerRecorder)]
    public class XRControllerRecorder : MonoBehaviour
    {
        [Header("Input Recording/Playback")]

        [SerializeField, Tooltip("Controls whether this recording will start playing when the component's Awake() method is called.")]
        bool m_PlayOnStart;

        /// <summary>
        /// Controls whether this recording will start playing when the component's <see cref="Awake"/> method is called.
        /// </summary>
        public bool playOnStart
        {
            get => m_PlayOnStart;
            set => m_PlayOnStart = value;
        }

        [SerializeField, Tooltip("Controller Recording asset for recording and playback of controller events.")]
        XRControllerRecording m_Recording;

        /// <summary>
        /// Controller Recording asset for recording and playback of controller events.
        /// </summary>
        internal XRControllerRecording recording
        {
            get => m_Recording;
            set => m_Recording = value;
        }

        [SerializeField, Tooltip("XR Controller who's output will be recorded and played back")]
        XRBaseController m_XRController;

        /// <summary>
        /// The controller that this recording uses for recording and playback.
        /// </summary>
        public XRBaseController xrController
        {
            get => m_XRController;
            set => m_XRController = value;
        }

        /// <summary>
        /// Whether the <see cref="XRControllerRecorder"/> is currently recording interaction state.
        /// </summary>
        public bool isRecording
        {
            get => m_IsRecording;
            set
            {
                if (m_IsRecording != value)
                {
                    recordingStartTime = Time.time;
                    isPlaying = false;
                    m_CurrentTime = 0d;
                    if (m_Recording)
                    {
                        if (value)
                            m_Recording.InitRecording();
                        else
                            m_Recording.SaveRecording();
                    }
                    m_IsRecording = value;
                }
            }
        }

        /// <summary>
        /// Whether the XRControllerRecorder is currently playing back interaction state.
        /// </summary>
        public bool isPlaying
        {
            get => m_IsPlaying;
            set
            {
                if (m_IsPlaying != value)
                {
                    isRecording = false;
                    if (m_Recording)
                        ResetPlayback();
                    m_CurrentTime = 0d;
                    m_IsPlaying = value;

                    // Cache the previous state of the XRController, or put it back
                    if (value && m_XRController != null)
                    {
                        m_PrevEnableInputActions = m_XRController.enableInputActions;
                        m_PrevEnableInputTracking = m_XRController.enableInputTracking;
                        m_XRController.enableInputActions = false;
                        m_XRController.enableInputTracking = false;
                    }
                    else if (m_XRController != null)
                    {
                        m_XRController.enableInputActions = m_PrevEnableInputActions;
                        m_XRController.enableInputTracking = m_PrevEnableInputTracking;
                    }
                }
            }
        }

        double m_CurrentTime;

        /// <summary>
        /// (Read Only) The current recording/playback time.
        /// </summary>
        public double currentTime => m_CurrentTime;

        /// <summary>
        /// (Read Only) The total playback time (or 0 if no recording).
        /// </summary>
        public double duration => m_Recording != null ? m_Recording.duration : 0d;

        /// <summary>
        /// The <see cref="Time.time"/> when recording was last started.
        /// </summary>
        protected float recordingStartTime { get; set; }

        bool m_IsRecording;
        bool m_IsPlaying;
        double m_LastPlaybackTime;
        int m_LastFrameIdx;

        bool m_PrevEnableInputActions;
        bool m_PrevEnableInputTracking;

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void Awake()
        {
            if (m_XRController == null)
                m_XRController = GetComponent<XRBaseController>();

            m_CurrentTime = 0d;

            if (m_PlayOnStart)
                isPlaying = true;
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void Update()
        {
            if (isRecording && m_XRController != null)
            {
                var state = m_XRController.currentControllerState;

                state.time = Time.time - recordingStartTime;

                m_Recording.AddRecordingFrame(state);
            }
            else if (isPlaying)
            {
                UpdatePlaybackTime(m_CurrentTime);
            }

            if (isRecording || isPlaying)
                m_CurrentTime += Time.deltaTime;
            if (isPlaying && m_CurrentTime > m_Recording.duration)
                isPlaying = false;
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected void OnDestroy()
        {
            isRecording = false;
            isPlaying = false;
        }

        /// <summary>
        /// Resets the recorder to the start of the clip.
        /// </summary>
        public void ResetPlayback()
        {
            m_LastPlaybackTime = 0d;
            m_LastFrameIdx = 0;
        }

        void UpdatePlaybackTime(double playbackTime)
        {
            if (!m_Recording || m_Recording == null || m_Recording.frames.Count == 0 || m_LastFrameIdx >= m_Recording.frames.Count  )
                return;

            // Look for next frame in order (binary search would be faster but we are only searching from last cached frame index)
            var prevFrame = m_Recording.frames[m_LastFrameIdx];
            var frameIdx = m_LastFrameIdx;
            if (prevFrame.time < playbackTime)
            {
                for (; frameIdx < m_Recording.frames.Count &&
                    m_Recording.frames[frameIdx].time >= m_LastPlaybackTime &&
                    m_Recording.frames[frameIdx].time <= playbackTime;
                ++frameIdx) { }
            }

            // Past last frame or on the same frame, don't do anything
            if (frameIdx >= m_Recording.frames.Count)
                return;

            if (m_XRController != null)
            {
                var recordingFrame = m_Recording.frames[frameIdx];
                m_XRController.currentControllerState = recordingFrame;
            }

            m_LastFrameIdx = frameIdx;
            m_LastPlaybackTime = playbackTime;
        }

        /// <summary>
        /// Gets the state of the controller.
        /// </summary>
        /// <param name="controllerState">When this method returns, contains the <see cref="XRControllerState"/> object representing the state of the controller.</param>
        /// <returns>Returns <see langword="true"/> when playing or recording. Otherwise, returns <see langword="false"/>.</returns>
        public virtual bool GetControllerState(out XRControllerState controllerState)
        {
            if (isPlaying)
            {
                // Return the current frame we're playing back
                if (m_Recording.frames.Count > m_LastFrameIdx)
                {
                    controllerState = m_Recording.frames[m_LastFrameIdx];
                    return true;
                }
            }
            else if (isRecording)
            {
                // Relay the last frame we got
                if (m_Recording.frames.Count > 0)
                {
                    controllerState = m_Recording.frames[m_Recording.frames.Count - 1];
                    return true;
                }
            }
            else if (m_XRController != null)
            {
                // Pass through as we're not recording or playing
                controllerState = m_XRController.currentControllerState;
                return false;
            }

            controllerState = new XRControllerState();
            return false;
        }
    }
}
