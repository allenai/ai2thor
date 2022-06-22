using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace UnityEditor.XR.Interaction.Toolkit
{
    /// <summary>
    /// Custom editor for an <see cref="XRSocketInteractor"/>.
    /// </summary>
    [CustomEditor(typeof(XRSocketInteractor), true), CanEditMultipleObjects]
    public class XRSocketInteractorEditor : XRBaseInteractorEditor
    {
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRSocketInteractor.showInteractableHoverMeshes"/>.</summary>
        protected SerializedProperty m_ShowInteractableHoverMeshes;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRSocketInteractor.interactableHoverMeshMaterial"/>.</summary>
        protected SerializedProperty m_InteractableHoverMeshMaterial;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRSocketInteractor.interactableCantHoverMeshMaterial"/>.</summary>
        protected SerializedProperty m_InteractableCantHoverMeshMaterial;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRSocketInteractor.socketActive"/>.</summary>
        protected SerializedProperty m_SocketActive;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRSocketInteractor.interactableHoverScale"/>.</summary>
        protected SerializedProperty m_InteractableHoverScale;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRSocketInteractor.recycleDelayTime"/>.</summary>
        protected SerializedProperty m_RecycleDelayTime;

        /// <summary>
        /// Contents of GUI elements used by this editor.
        /// </summary>
        protected static class Contents
        {
            /// <summary><see cref="GUIContent"/> for <see cref="XRSocketInteractor.showInteractableHoverMeshes"/>.</summary>
            public static readonly GUIContent showInteractableHoverMeshes = EditorGUIUtility.TrTextContent("Show Interactable Hover Meshes", "Show interactable's meshes at socket's attach point on hover.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRSocketInteractor.interactableHoverMeshMaterial"/>.</summary>
            public static readonly GUIContent interactableHoverMeshMaterial = EditorGUIUtility.TrTextContent("Hover Mesh Material", "Material used for rendering interactable meshes on hover (a default material will be created if none is supplied).");
            /// <summary><see cref="GUIContent"/> for <see cref="XRSocketInteractor.interactableCantHoverMeshMaterial"/>.</summary>
            public static readonly GUIContent interactableCantHoverMeshMaterial = EditorGUIUtility.TrTextContent("Can't Hover Mesh Material", "Material used for rendering interactable meshes on hover when there is already a selected object in the socket (a default material will be created if none is supplied).");
            /// <summary><see cref="GUIContent"/> for <see cref="XRSocketInteractor.socketActive"/>.</summary>
            public static readonly GUIContent socketActive = EditorGUIUtility.TrTextContent("Socket Active", "Whether socket interaction is enabled.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRSocketInteractor.interactableHoverScale"/>.</summary>
            public static readonly GUIContent interactableHoverScale = EditorGUIUtility.TrTextContent("Hover Scale", "Scale at which to render hovered interactable.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRSocketInteractor.recycleDelayTime"/>.</summary>
            public static readonly GUIContent recycleDelayTime = EditorGUIUtility.TrTextContent("Recycle Delay Time", "Amount of time the socket will refuse hovers after an object is removed.");
        }

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();

            m_ShowInteractableHoverMeshes = serializedObject.FindProperty("m_ShowInteractableHoverMeshes");
            m_InteractableHoverMeshMaterial = serializedObject.FindProperty("m_InteractableHoverMeshMaterial");
            m_InteractableCantHoverMeshMaterial = serializedObject.FindProperty("m_InteractableCantHoverMeshMaterial");
            m_SocketActive = serializedObject.FindProperty("m_SocketActive");
            m_InteractableHoverScale = serializedObject.FindProperty("m_InteractableHoverScale");
            m_RecycleDelayTime = serializedObject.FindProperty("m_RecycleDelayTime");
        }

        /// <inheritdoc />
        protected override void DrawProperties()
        {
            base.DrawProperties();

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(m_ShowInteractableHoverMeshes, Contents.showInteractableHoverMeshes);
            if (m_ShowInteractableHoverMeshes.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(m_InteractableHoverMeshMaterial, Contents.interactableHoverMeshMaterial);
                    EditorGUILayout.PropertyField(m_InteractableCantHoverMeshMaterial, Contents.interactableCantHoverMeshMaterial);
                    EditorGUILayout.PropertyField(m_InteractableHoverScale, Contents.interactableHoverScale);
                }
            }

            EditorGUILayout.PropertyField(m_SocketActive, Contents.socketActive);
            EditorGUILayout.PropertyField(m_RecycleDelayTime, Contents.recycleDelayTime);
        }
    }
}
