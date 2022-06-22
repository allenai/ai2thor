using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace UnityEditor.XR.Interaction.Toolkit
{
    /// <summary>
    /// Custom editor for an <see cref="XRInteractorLineVisual"/>.
    /// </summary>
    [CustomEditor(typeof(XRInteractorLineVisual), true), CanEditMultipleObjects]
    public class XRInteractorLineVisualEditor : BaseInteractionEditor
    {
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRInteractorLineVisual.lineWidth"/>.</summary>
        protected SerializedProperty m_LineWidth;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRInteractorLineVisual.widthCurve"/>.</summary>
        protected SerializedProperty m_WidthCurve;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRInteractorLineVisual.validColorGradient"/>.</summary>
        protected SerializedProperty m_ValidColorGradient;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRInteractorLineVisual.invalidColorGradient"/>.</summary>
        protected SerializedProperty m_InvalidColorGradient;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRInteractorLineVisual.smoothMovement"/>.</summary>
        protected SerializedProperty m_SmoothMovement;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRInteractorLineVisual.followTightness"/>.</summary>
        protected SerializedProperty m_FollowTightness;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRInteractorLineVisual.snapThresholdDistance"/>.</summary>
        protected SerializedProperty m_SnapThresholdDistance;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRInteractorLineVisual.reticle"/>.</summary>
        protected SerializedProperty m_Reticle;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRInteractorLineVisual.overrideInteractorLineLength"/>.</summary>
        protected SerializedProperty m_OverrideInteractorLineLength;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRInteractorLineVisual.lineLength"/>.</summary>
        protected SerializedProperty m_LineLength;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRInteractorLineVisual.stopLineAtFirstRaycastHit"/>.</summary>
        protected SerializedProperty m_StopLineAtFirstRaycastHit;

        /// <summary>
        /// Contents of GUI elements used by this editor.
        /// </summary>
        protected static class Contents
        {
            /// <summary><see cref="GUIContent"/> for <see cref="XRInteractorLineVisual.lineWidth"/>.</summary>
            public static readonly GUIContent lineWidth = EditorGUIUtility.TrTextContent("Line Width", "Controls the width of the line.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRInteractorLineVisual.widthCurve"/>.</summary>
            public static readonly GUIContent widthCurve = EditorGUIUtility.TrTextContent("Width Curve", "Controls the relative width of the line from start to end.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRInteractorLineVisual.validColorGradient"/>.</summary>
            public static readonly GUIContent validColorGradient = EditorGUIUtility.TrTextContent("Valid Color Gradient", "Controls the color of the line as a gradient from start to end to indicate a valid state.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRInteractorLineVisual.invalidColorGradient"/>.</summary>
            public static readonly GUIContent invalidColorGradient = EditorGUIUtility.TrTextContent("Invalid Color Gradient", "Controls the color of the line as a gradient from start to end to indicate an invalid state.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRInteractorLineVisual.smoothMovement"/>.</summary>
            public static readonly GUIContent smoothMovement = EditorGUIUtility.TrTextContent("Smooth Movement", "Controls whether the rendered segments will be delayed from and smoothly follow the target segments.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRInteractorLineVisual.followTightness"/>.</summary>
            public static readonly GUIContent followTightness = EditorGUIUtility.TrTextContent("Follow Tightness", "Controls the speed that the rendered segments will follow the target segments when Smooth Movement is enabled.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRInteractorLineVisual.snapThresholdDistance"/>.</summary>
            public static readonly GUIContent snapThresholdDistance = EditorGUIUtility.TrTextContent("Snap Threshold Distance", "Controls the threshold distance between line points at two consecutive frames to snap rendered segments to target segments when Smooth Movement is enabled.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRInteractorLineVisual.reticle"/>.</summary>
            public static readonly GUIContent reticle = EditorGUIUtility.TrTextContent("Reticle", "Stores the reticle that will appear at the end of the line when it is valid.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRInteractorLineVisual.overrideInteractorLineLength"/>.</summary>
            public static readonly GUIContent overrideInteractorLineLength = EditorGUIUtility.TrTextContent("Override Line Length", "Controls which source is used to determine the length of the line. Set to true to use the Line Length set by this behavior. Set to false have the length of the line determined by the interactor.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRInteractorLineVisual.lineLength"/>.</summary>
            public static readonly GUIContent lineLength = EditorGUIUtility.TrTextContent("Line Length", "Controls the length of the line when overriding.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRInteractorLineVisual.stopLineAtFirstRaycastHit"/>.</summary>
            public static readonly GUIContent stopLineAtFirstRaycastHit = EditorGUIUtility.TrTextContent("Stop Line At First Raycast Hit", "Controls whether the line will always be cut short by this behavior at the first ray cast hit, even when invalid.");
        }

        /// <summary>
        /// See <see cref="MonoBehaviour"/>.
        /// </summary>
        protected virtual void OnEnable()
        {
            m_LineWidth = serializedObject.FindProperty("m_LineWidth");
            m_WidthCurve = serializedObject.FindProperty("m_WidthCurve");
            m_ValidColorGradient = serializedObject.FindProperty("m_ValidColorGradient");
            m_InvalidColorGradient = serializedObject.FindProperty("m_InvalidColorGradient");
            m_SmoothMovement = serializedObject.FindProperty("m_SmoothMovement");
            m_FollowTightness = serializedObject.FindProperty("m_FollowTightness");
            m_SnapThresholdDistance = serializedObject.FindProperty("m_SnapThresholdDistance");
            m_Reticle = serializedObject.FindProperty("m_Reticle");
            m_OverrideInteractorLineLength = serializedObject.FindProperty("m_OverrideInteractorLineLength");
            m_LineLength = serializedObject.FindProperty("m_LineLength");
            m_StopLineAtFirstRaycastHit = serializedObject.FindProperty("m_StopLineAtFirstRaycastHit");
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
            DrawWidthConfiguration();
            DrawColorConfiguration();
            DrawLengthConfiguration();
            DrawSmoothMovement();
            DrawReticle();
        }

        /// <summary>
        /// Draw property fields related to the line width.
        /// </summary>
        protected virtual void DrawWidthConfiguration()
        {
            EditorGUILayout.PropertyField(m_LineWidth, Contents.lineWidth);
            EditorGUILayout.PropertyField(m_WidthCurve, Contents.widthCurve);
        }

        /// <summary>
        /// Draw property fields related to color gradients.
        /// </summary>
        protected virtual void DrawColorConfiguration()
        {
            EditorGUILayout.PropertyField(m_ValidColorGradient, Contents.validColorGradient);
            EditorGUILayout.PropertyField(m_InvalidColorGradient, Contents.invalidColorGradient);
        }

        /// <summary>
        /// Draw property fields related to the line length.
        /// </summary>
        protected virtual void DrawLengthConfiguration()
        {
            EditorGUILayout.PropertyField(m_OverrideInteractorLineLength, Contents.overrideInteractorLineLength);
            if (m_OverrideInteractorLineLength.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(m_LineLength, Contents.lineLength);
                }
            }

            EditorGUILayout.PropertyField(m_StopLineAtFirstRaycastHit, Contents.stopLineAtFirstRaycastHit);
        }

        /// <summary>
        /// Draw property fields related to smooth movement.
        /// </summary>
        protected virtual void DrawSmoothMovement()
        {
            EditorGUILayout.PropertyField(m_SmoothMovement, Contents.smoothMovement);

            if (m_SmoothMovement.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(m_FollowTightness, Contents.followTightness);
                    EditorGUILayout.PropertyField(m_SnapThresholdDistance, Contents.snapThresholdDistance);
                }
            }
        }

        /// <summary>
        /// Draw property fields related to the reticle.
        /// </summary>
        protected virtual void DrawReticle()
        {
            EditorGUILayout.PropertyField(m_Reticle, Contents.reticle);
        }
    }
}
