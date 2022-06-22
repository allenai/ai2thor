using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.AR;

namespace UnityEditor.XR.Interaction.Toolkit.AR
{
    /// <summary>
    /// Custom editor for an <see cref="ARBaseGestureInteractable"/>.
    /// </summary>
    [CustomEditor(typeof(ARBaseGestureInteractable), true), CanEditMultipleObjects]
    public class ARBaseGestureInteractableEditor : XRBaseInteractableEditor
    {
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="ARBaseGestureInteractable.xrOrigin"/>.</summary>
        protected SerializedProperty m_XROrigin;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="ARBaseGestureInteractable.arSessionOrigin"/>.</summary>
        protected SerializedProperty m_ARSessionOrigin;

        /// <summary>
        /// Contents of GUI elements used by this editor.
        /// </summary>
        protected static class BaseGestureContents
        {
            /// <summary><see cref="GUIContent"/> for <see cref="ARBaseGestureInteractable.xrOrigin"/>.</summary>
            public static readonly GUIContent xrOrigin = EditorGUIUtility.TrTextContent("XR Origin", "The XR Origin that this Interactable will use (such as to get the Camera or to transform from Session space). Will find one if None.");
            /// <summary><see cref="GUIContent"/> for <see cref="ARBaseGestureInteractable.arSessionOrigin"/>.</summary>
            public static readonly GUIContent arSessionOrigin = EditorGUIUtility.TrTextContent("AR Session Origin", "(Deprecated) The AR Session Origin that this Interactable will use (such as to get the Camera or to transform from Session space). Will find one if None.");

            /// <summary>The help box message when AR Session Origin is used.</summary>
            public static readonly GUIContent arSessionOriginDeprecated = EditorGUIUtility.TrTextContent("AR Session Origin has been deprecated. Use the XR Origin component instead.");
        }

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();

            m_XROrigin = serializedObject.FindProperty("m_XROrigin");
            m_ARSessionOrigin = serializedObject.FindProperty("m_ARSessionOrigin");
        }

        /// <inheritdoc />
        protected override void DrawProperties()
        {
            base.DrawProperties();
            EditorGUILayout.PropertyField(m_XROrigin, BaseGestureContents.xrOrigin);
            using (new EditorGUI.IndentLevelScope())
            {
                if (m_ARSessionOrigin.objectReferenceValue != null)
                    EditorGUILayout.HelpBox(BaseGestureContents.arSessionOriginDeprecated.text, MessageType.Warning);
                EditorGUILayout.PropertyField(m_ARSessionOrigin, BaseGestureContents.arSessionOrigin);
            }
        }
    }
}
