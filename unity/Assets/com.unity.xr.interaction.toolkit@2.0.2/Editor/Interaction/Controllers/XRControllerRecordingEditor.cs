using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.XR.Interaction.Toolkit;

namespace UnityEditor.XR.Interaction.Toolkit
{
    /// <summary>
    /// Custom editor for an <see cref="XRControllerRecording"/>.
    /// </summary>
    [CustomEditor(typeof(XRControllerRecording), true)]
    [MovedFrom("UnityEngine.XR.Interaction.Toolkit")]
    public class XRControllerRecordingEditor : BaseInteractionEditor
    {
        /// <summary>String format used to display the interaction values.</summary>
        protected const string k_ValueFormat = "0.#";

        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> <see cref="XRControllerRecording"/><c>.m_SelectActivatedInFirstFrame</c>.</summary>
        SerializedProperty m_SelectActivatedInFirstFrame;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> <see cref="XRControllerRecording"/><c>.m_ActivateActivatedInFirstFrame</c>.</summary>
        SerializedProperty m_ActivateActivatedInFirstFrame;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> <see cref="XRControllerRecording"/><c>.m_FirstUIPressActivatedInFirstFrame</c>.</summary>
        SerializedProperty m_FirstUIPressActivatedInFirstFrame;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRControllerRecording.frames"/>.</summary>
        SerializedProperty m_Frames;

        XRControllerRecording m_ControllerRecording;

        /// <summary>
        /// Contents of GUI elements used by this editor.
        /// </summary>
        protected static class Contents
        {
            /// <summary><see cref="GUIContent"/> for <see cref="XRControllerRecording.frames"/>.</summary>
            public static readonly GUIContent frames = EditorGUIUtility.TrTextContent("Frames", "Frames stored in this recording.");

            /// <summary><see cref="GUIContent"/> for the button to remove all <see cref="XRControllerRecording.frames"/>.</summary>
            public static readonly GUIContent clearRecording = EditorGUIUtility.TrTextContent("Clear Recording");
        }

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        /// <seealso cref="MonoBehaviour"/>
        protected virtual void OnEnable()
        {
            m_SelectActivatedInFirstFrame = serializedObject.FindProperty("m_SelectActivatedInFirstFrame");
            m_ActivateActivatedInFirstFrame = serializedObject.FindProperty("m_ActivateActivatedInFirstFrame");
            m_FirstUIPressActivatedInFirstFrame = serializedObject.FindProperty("m_FirstUIPressActivatedInFirstFrame");
            m_Frames = serializedObject.FindProperty("m_Frames");
            m_ControllerRecording = (XRControllerRecording)target;
        }

        /// <inheritdoc />
        /// <seealso cref="DrawBeforeProperties"/>
        /// <seealso cref="DrawProperties"/>
        /// <seealso cref="BaseInteractionEditor.DrawDerivedProperties"/>
        protected override void DrawInspector()
        {
            DrawBeforeProperties();
            DrawProperties();
            DrawDerivedProperties();
        }

        /// <summary>
        /// This method is automatically called by <see cref="DrawInspector"/> to
        /// draw the section of the custom inspector before <see cref="DrawProperties"/>.
        /// By default, this draws the read-only Script property.
        /// </summary>
        protected virtual void DrawBeforeProperties()
        {
            DrawScript();
        }

        /// <summary>
        /// This method is automatically called by <see cref="DrawInspector"/> to
        /// draw the property fields. Override this method to customize the
        /// properties shown in the Inspector. This is typically the method overridden
        /// when a derived behavior adds additional serialized properties
        /// that should be displayed in the Inspector.
        /// </summary>
        protected virtual void DrawProperties()
        {
            if (GUILayout.Button(Contents.clearRecording))
            {
                m_SelectActivatedInFirstFrame.boolValue = false;
                m_ActivateActivatedInFirstFrame.boolValue = false;
                m_FirstUIPressActivatedInFirstFrame.boolValue = false;
                m_Frames.ClearArray();
            }

            EditorGUILayout.LabelField(Contents.frames);
            using (new EditorGUILayout.VerticalScope())
            {
                DrawRecordingFrames();
                GUILayout.Space(5);
            }
        }

        /// <summary>
        /// Draw the frames stored in the recording.
        /// </summary>
        protected virtual void DrawRecordingFrames()
        {
            foreach (var frame in m_ControllerRecording.frames)
            {
                DrawRecordingFrame(frame);
            }
        }

        /// <summary>
        /// Draw the <paramref name="frame"/> stored in the recording.
        /// </summary>
        /// <param name="frame">The controller frame to draw.</param>
        protected virtual void DrawRecordingFrame(XRControllerState frame)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.FloatField((float)frame.time, GUILayout.ExpandWidth(true));
            EditorGUILayout.TextField(frame.position.ToString(), GUILayout.Width(120));
            EditorGUILayout.TextField(frame.rotation.ToString(), GUILayout.Width(160));
            EditorGUILayout.Toggle(frame.selectInteractionState.active, GUILayout.MaxWidth(14));
            EditorGUILayout.Toggle(frame.activateInteractionState.active, GUILayout.MaxWidth(14));
            EditorGUILayout.Toggle(frame.uiPressInteractionState.active, GUILayout.MaxWidth(14));
            EditorGUILayout.TextField(frame.selectInteractionState.value.ToString(k_ValueFormat), GUILayout.MaxWidth(28f));
            EditorGUILayout.TextField(frame.activateInteractionState.value.ToString(k_ValueFormat), GUILayout.MaxWidth(28f));
            EditorGUILayout.TextField(frame.uiPressInteractionState.value.ToString(k_ValueFormat), GUILayout.MaxWidth(28f));
            EditorGUILayout.EndHorizontal();
        }
    }
}
