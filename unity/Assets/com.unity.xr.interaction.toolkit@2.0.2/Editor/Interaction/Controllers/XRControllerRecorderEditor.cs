using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.XR.Interaction.Toolkit;

namespace UnityEditor.XR.Interaction.Toolkit
{
    /// <summary>
    /// Custom editor for an <see cref="XRControllerRecorder"/>.
    /// </summary>
    [CustomEditor(typeof(XRControllerRecorder), true), CanEditMultipleObjects]
    [MovedFrom("UnityEngine.XR.Interaction.Toolkit")]
    public class XRControllerRecorderEditor : Editor
    {
        List<XRControllerRecorder> m_ControllerRecorders;

        /// <summary>
        /// Contents of GUI elements used by this editor.
        /// </summary>
        protected static class Contents
        {
            /// <summary><see cref="GUIContent"/> for the button to Stop Recording.</summary>
            public static readonly GUIContent stopRecording = EditorGUIUtility.TrTextContent("Stop Recording");
            /// <summary><see cref="GUIContent"/> for the button to Record Input.</summary>
            public static readonly GUIContent recordInput = EditorGUIUtility.TrTextContent("Record Input");
            /// <summary><see cref="GUIContent"/> for the button to Stop.</summary>
            public static readonly GUIContent stop = EditorGUIUtility.TrTextContent("Stop");
            /// <summary><see cref="GUIContent"/> for the button to Play.</summary>
            public static readonly GUIContent play = EditorGUIUtility.TrTextContent("Play");
            /// <summary><see cref="GUIContent"/> for disabled playback control buttons that can't be multi-object edited.</summary>
            public static readonly GUIContent mixedValues = EditorGUIUtility.TrTextContent("\u2014", "Mixed Values");
        }

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        /// <seealso cref="MonoBehaviour"/>
        protected virtual void OnEnable()
        {
            m_ControllerRecorders = targets.Cast<XRControllerRecorder>().ToList();
        }

        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            DrawPlaybackControls();
            DrawTimeline();
        }

        /// <summary>
        /// Draw the playback controls while the application is playing.
        /// </summary>
        protected virtual void DrawPlaybackControls()
        {
            if (!Application.isPlaying)
                return;

            using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
            {
                if (m_ControllerRecorders.All(controllerRecorder => controllerRecorder.isRecording))
                {
                    if (GUILayout.Button(Contents.stopRecording))
                        m_ControllerRecorders.ForEach(controllerRecorder => controllerRecorder.isRecording = false);
                }
                else if (m_ControllerRecorders.All(controllerRecorder => !controllerRecorder.isRecording))
                {
                    if (GUILayout.Button(Contents.recordInput))
                        m_ControllerRecorders.ForEach(controllerRecorder => controllerRecorder.isRecording = true);
                }
                else
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        GUILayout.Button(Contents.mixedValues);
                    }
                }

                if (m_ControllerRecorders.All(controllerRecorder => controllerRecorder.isPlaying))
                {
                    if (GUILayout.Button(Contents.stop))
                        m_ControllerRecorders.ForEach(controllerRecorder => controllerRecorder.isPlaying = false);
                }
                else if (m_ControllerRecorders.All(controllerRecorder => !controllerRecorder.isPlaying))
                {
                    if (GUILayout.Button(Contents.play))
                        m_ControllerRecorders.ForEach(controllerRecorder => controllerRecorder.isPlaying = true);
                }
                else
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        GUILayout.Button(Contents.mixedValues);
                    }
                }
            }
        }

        /// <summary>
        /// Draw the time progress bar while the application is playing.
        /// </summary>
        protected virtual void DrawTimeline()
        {
            if (!Application.isPlaying)
                return;

            var currentTime = (float)((XRControllerRecorder)target).currentTime;
            var duration = (float)((XRControllerRecorder)target).duration;
            if (!serializedObject.isEditingMultipleObjects ||
                m_ControllerRecorders.All(controllerRecorder => Mathf.Approximately((float)controllerRecorder.currentTime, currentTime)) &&
                m_ControllerRecorders.All(controllerRecorder => Mathf.Approximately((float)controllerRecorder.duration, duration)))
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.Slider(currentTime, 0f, duration);
                }
            }
        }
    }
}
