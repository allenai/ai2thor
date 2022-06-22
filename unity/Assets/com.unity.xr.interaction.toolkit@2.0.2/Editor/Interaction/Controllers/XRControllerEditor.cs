using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.XR.Interaction.Toolkit;

namespace UnityEditor.XR.Interaction.Toolkit
{
    /// <summary>
    /// Custom editor for an <see cref="XRController"/>.
    /// </summary>
    [CustomEditor(typeof(XRController), true), CanEditMultipleObjects]
    [MovedFrom("UnityEngine.XR.Interaction.Toolkit")]
    public class XRControllerEditor : XRBaseControllerEditor
    {
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRController.controllerNode"/>.</summary>
        SerializedProperty m_ControllerNode;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRController.selectUsage"/>.</summary>
        SerializedProperty m_SelectUsage;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRController.activateUsage"/>.</summary>
        SerializedProperty m_ActivateUsage;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRController.uiPressUsage"/>.</summary>
        SerializedProperty m_UIPressUsage;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRController.axisToPressThreshold"/>.</summary>
        SerializedProperty m_AxisToPressThreshold;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRController.rotateObjectLeft"/>.</summary>
        SerializedProperty m_RotateAnchorLeft;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRController.rotateObjectRight"/>.</summary>
        SerializedProperty m_RotateAnchorRight;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRController.moveObjectIn"/>.</summary>
        SerializedProperty m_MoveObjectIn;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRController.moveObjectOut"/>.</summary>
        SerializedProperty m_MoveObjectOut;

#if LIH_PRESENT
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRController.poseProvider"/>.</summary>
        SerializedProperty m_PoseProvider;
#endif

        /// <summary>
        /// Contents of GUI elements used by this editor.
        /// </summary>
        protected static class Contents
        {
            /// <summary><see cref="GUIContent"/> for <see cref="XRController.controllerNode"/>.</summary>
            public static GUIContent controllerNode = EditorGUIUtility.TrTextContent("Controller Node", "The XR Node for this controller.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRController.selectUsage"/>.</summary>
            public static GUIContent selectUsage = EditorGUIUtility.TrTextContent("Select Usage", "The input to use for detecting a select.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRController.activateUsage"/>.</summary>
            public static GUIContent activateUsage = EditorGUIUtility.TrTextContent("Activate Usage", "The input to use for detecting activation.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRController.uiPressUsage"/>.</summary>
            public static GUIContent uiPressUsage = EditorGUIUtility.TrTextContent("UI Press Usage", "The input to use for detecting a UI press.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRController.axisToPressThreshold"/>.</summary>
            public static GUIContent axisToPressThreshold = EditorGUIUtility.TrTextContent("Axis To Press Threshold", "The amount an axis needs to be pressed to trigger an interaction event.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRController.rotateObjectLeft"/>.</summary>
            public static GUIContent rotateAnchorLeft = EditorGUIUtility.TrTextContent("Rotate Object Left", "The input to use to rotate an anchor to the left.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRController.rotateObjectRight"/>.</summary>
            public static GUIContent rotateAnchorRight = EditorGUIUtility.TrTextContent("Rotate Object Right", "The input to use to rotate an anchor to the right.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRController.moveObjectIn"/>.</summary>
            public static GUIContent moveObjectIn = EditorGUIUtility.TrTextContent("Move Object In", "The input that will be used to translate the anchor away from the interactor.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRController.moveObjectOut"/>.</summary>
            public static GUIContent moveObjectOut = EditorGUIUtility.TrTextContent("Move Object Out", "The input that will be used to translate the anchor towards the interactor.");

#if LIH_PRESENT
            /// <summary><see cref="GUIContent"/> for <see cref="XRController.poseProvider"/>.</summary>
            public static GUIContent poseProvider = EditorGUIUtility.TrTextContent("Pose Provider", "Pose provider used to provide tracking data separate from the XR Node.");
            /// <summary>The help box message when Pose Provider is being used.</summary>
            public static GUIContent poseProviderWarning = EditorGUIUtility.TrTextContent("This XR Controller is using an external pose provider for tracking.  This takes priority over the Controller Node setting.");
#endif
        }

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();

            m_ControllerNode = serializedObject.FindProperty("m_ControllerNode");
            m_SelectUsage = serializedObject.FindProperty("m_SelectUsage");
            m_ActivateUsage = serializedObject.FindProperty("m_ActivateUsage");
            m_UIPressUsage = serializedObject.FindProperty("m_UIPressUsage");
            m_AxisToPressThreshold = serializedObject.FindProperty("m_AxisToPressThreshold");
            m_RotateAnchorLeft = serializedObject.FindProperty("m_RotateAnchorLeft");
            m_RotateAnchorRight = serializedObject.FindProperty("m_RotateAnchorRight");
            m_MoveObjectIn = serializedObject.FindProperty("m_MoveObjectIn");
            m_MoveObjectOut = serializedObject.FindProperty("m_MoveObjectOut");

#if LIH_PRESENT
            m_PoseProvider = serializedObject.FindProperty("m_PoseProvider");
#endif
        }

        /// <inheritdoc />
        protected override void DrawInputConfiguration()
        {
            base.DrawInputConfiguration();
#if LIH_PRESENT
            EditorGUILayout.PropertyField(m_PoseProvider, Contents.poseProvider);
            if (m_PoseProvider.objectReferenceValue != null)
                EditorGUILayout.HelpBox(Contents.poseProviderWarning.text, MessageType.Info, true);
#endif

            EditorGUILayout.PropertyField(m_ControllerNode, Contents.controllerNode);
            EditorGUILayout.PropertyField(m_SelectUsage, Contents.selectUsage);
            EditorGUILayout.PropertyField(m_ActivateUsage, Contents.activateUsage);
            EditorGUILayout.PropertyField(m_UIPressUsage, Contents.uiPressUsage);
            EditorGUILayout.PropertyField(m_AxisToPressThreshold, Contents.axisToPressThreshold);
        }

        /// <inheritdoc />
        protected override void DrawOtherActions()
        {
            base.DrawOtherActions();
            EditorGUILayout.PropertyField(m_RotateAnchorLeft, Contents.rotateAnchorLeft);
            EditorGUILayout.PropertyField(m_RotateAnchorRight, Contents.rotateAnchorRight);
            EditorGUILayout.PropertyField(m_MoveObjectIn, Contents.moveObjectIn);
            EditorGUILayout.PropertyField(m_MoveObjectOut, Contents.moveObjectOut);
        }
    }
}
