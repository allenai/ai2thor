using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace UnityEditor.XR.Interaction.Toolkit
{
    /// <summary>
    /// Custom editor for an <see cref="XRBaseInteractor"/>.
    /// </summary>
    [CustomEditor(typeof(XRBaseInteractor), true), CanEditMultipleObjects]
    public partial class XRBaseInteractorEditor : BaseInteractionEditor
    {
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractor.interactionManager"/>.</summary>
        protected SerializedProperty m_InteractionManager;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractor.interactionLayerMask"/>.</summary>
        protected SerializedProperty m_InteractionLayerMask;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractor.interactionLayers"/>.</summary>
        protected SerializedProperty m_InteractionLayers;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractor.attachTransform"/>.</summary>
        protected SerializedProperty m_AttachTransform;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractor.keepSelectedTargetValid"/>.</summary>
        protected SerializedProperty m_KeepSelectedTargetValid;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractor.startingSelectedInteractable"/>.</summary>
        protected SerializedProperty m_StartingSelectedInteractable;

        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractor.hoverEntered"/>.</summary>
        protected SerializedProperty m_HoverEntered;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractor.hoverExited"/>.</summary>
        protected SerializedProperty m_HoverExited;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractor.selectEntered"/>.</summary>
        protected SerializedProperty m_SelectEntered;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractor.selectExited"/>.</summary>
        protected SerializedProperty m_SelectExited;

        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractor.onHoverEntered"/>.</summary>
        protected SerializedProperty m_OnHoverEntered;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractor.onHoverExited"/>.</summary>
        protected SerializedProperty m_OnHoverExited;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractor.onSelectEntered"/>.</summary>
        protected SerializedProperty m_OnSelectEntered;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractor.onSelectExited"/>.</summary>
        protected SerializedProperty m_OnSelectExited;

        /// <summary><see cref="SerializedProperty"/> of the persistent calls backing <see cref="XRBaseInteractor.onHoverEntered"/>.</summary>
        protected SerializedProperty m_OnHoverEnteredCalls;
        /// <summary><see cref="SerializedProperty"/> of the persistent calls backing <see cref="XRBaseInteractor.onHoverExited"/>.</summary>
        protected SerializedProperty m_OnHoverExitedCalls;
        /// <summary><see cref="SerializedProperty"/> of the persistent calls backing <see cref="XRBaseInteractor.onSelectEntered"/>.</summary>
        protected SerializedProperty m_OnSelectEnteredCalls;
        /// <summary><see cref="SerializedProperty"/> of the persistent calls backing <see cref="XRBaseInteractor.onSelectExited"/>.</summary>
        protected SerializedProperty m_OnSelectExitedCalls;

        /// <summary>
        /// Contents of GUI elements used by this editor.
        /// </summary>
        protected static class BaseContents
        {
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseInteractor.interactionManager"/>.</summary>
            public static readonly GUIContent interactionManager = EditorGUIUtility.TrTextContent("Interaction Manager", "The XR Interaction Manager that this Interactor will communicate with (will find one if None).");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseInteractor.interactionLayerMask"/>.</summary>
            public static readonly GUIContent interactionLayerMask = EditorGUIUtility.TrTextContent("Deprecated Interaction Layer Mask", "Deprecated Interaction Layer Mask that uses the Unity physics Layers. Hide this property by disabling \'Show Old Interaction Layer Mask In Inspector\' in the XR Interaction Toolkit project settings.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseInteractor.interactionLayerMask"/>.</summary>
            public static readonly GUIContent interactionLayers = EditorGUIUtility.TrTextContent("Interaction Layer Mask", "Allows interaction with Interactables whose Interaction Layer Mask overlaps with any Layer in this Interaction Layer Mask.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseInteractor.attachTransform"/>.</summary>
            public static readonly GUIContent attachTransform = EditorGUIUtility.TrTextContent("Attach Transform", "The Transform that is used as the attach point for Interactables. Will create an empty GameObject if None.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseInteractor.keepSelectedTargetValid"/>.</summary>
            public static readonly GUIContent keepSelectedTargetValid = EditorGUIUtility.TrTextContent("Keep Selected Target Valid", "Keep selecting the target when not touching or pointing to it after initially selecting it. It is recommended to set this value to true for grabbing objects, false for teleportation.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseInteractor.startingSelectedInteractable"/>.</summary>
            public static readonly GUIContent startingSelectedInteractable = EditorGUIUtility.TrTextContent("Starting Selected Interactable", "The Interactable that this Interactor will automatically select at startup (optional, may be None).");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseInteractor.onHoverEntered"/>.</summary>
            public static readonly GUIContent onHoverEntered = EditorGUIUtility.TrTextContent("(Deprecated) On Hover Entered");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseInteractor.onHoverExited"/>.</summary>
            public static readonly GUIContent onHoverExited = EditorGUIUtility.TrTextContent("(Deprecated) On Hover Exited");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseInteractor.onSelectEntered"/>.</summary>
            public static readonly GUIContent onSelectEntered = EditorGUIUtility.TrTextContent("(Deprecated) On Select Entered");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseInteractor.onSelectExited"/>.</summary>
            public static readonly GUIContent onSelectExited = EditorGUIUtility.TrTextContent("(Deprecated) On Select Exited");
            /// <summary><see cref="GUIContent"/> for the header label of Hover events.</summary>
            public static readonly GUIContent hoverEventsHeader = EditorGUIUtility.TrTextContent("Hover", "Called when this Interactor begins hovering over an Interactable (Entered), or ends hovering (Exited).");
            /// <summary><see cref="GUIContent"/> for the header label of Select events.</summary>
            public static readonly GUIContent selectEventsHeader = EditorGUIUtility.TrTextContent("Select", "Called when this Interactor begins selecting an Interactable (Entered), or ends selecting (Exited).");

            /// <summary>The help box message when deprecated Interactor Events are being used.</summary>
            public static readonly GUIContent deprecatedEventsInUse = EditorGUIUtility.TrTextContent("Some deprecated Interactor Events are being used. These deprecated events will be removed in a future version. Please convert these to use the newer events, and update script method signatures for Dynamic listeners.");
        }

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        /// <seealso cref="MonoBehaviour"/>
        protected virtual void OnEnable()
        {
            m_InteractionManager = serializedObject.FindProperty("m_InteractionManager");
            m_InteractionLayerMask = serializedObject.FindProperty("m_InteractionLayerMask");
            m_InteractionLayers = serializedObject.FindProperty("m_InteractionLayers");
            m_AttachTransform = serializedObject.FindProperty("m_AttachTransform");
            m_KeepSelectedTargetValid = serializedObject.FindProperty("m_KeepSelectedTargetValid");
            m_StartingSelectedInteractable = serializedObject.FindProperty("m_StartingSelectedInteractable");

            m_HoverEntered = serializedObject.FindProperty("m_HoverEntered");
            m_HoverExited = serializedObject.FindProperty("m_HoverExited");
            m_SelectEntered = serializedObject.FindProperty("m_SelectEntered");
            m_SelectExited = serializedObject.FindProperty("m_SelectExited");

            m_OnHoverEntered = serializedObject.FindProperty("m_OnHoverEntered");
            m_OnHoverExited = serializedObject.FindProperty("m_OnHoverExited");
            m_OnSelectEntered = serializedObject.FindProperty("m_OnSelectEntered");
            m_OnSelectExited = serializedObject.FindProperty("m_OnSelectExited");

            m_OnHoverEnteredCalls = m_OnHoverEntered.FindPropertyRelative("m_PersistentCalls.m_Calls");
            m_OnHoverExitedCalls = m_OnHoverExited.FindPropertyRelative("m_PersistentCalls.m_Calls");
            m_OnSelectEnteredCalls = m_OnSelectEntered.FindPropertyRelative("m_PersistentCalls.m_Calls");
            m_OnSelectExitedCalls = m_OnSelectExited.FindPropertyRelative("m_PersistentCalls.m_Calls");
        }

        /// <inheritdoc />
        /// <seealso cref="DrawBeforeProperties"/>
        /// <seealso cref="DrawProperties"/>
        /// <seealso cref="BaseInteractionEditor.DrawDerivedProperties"/>
        /// <seealso cref="DrawEvents"/>
        protected override void DrawInspector()
        {
            DrawBeforeProperties();
            DrawProperties();
            DrawDerivedProperties();

            EditorGUILayout.Space();

            DrawEvents();
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
        /// when a derived behavior adds additional serialized properties that should
        /// be displayed in the Inspector.
        /// </summary>
        protected virtual void DrawProperties()
        {
            DrawCoreConfiguration();
            EditorGUILayout.PropertyField(m_KeepSelectedTargetValid, BaseContents.keepSelectedTargetValid);
        }

        /// <summary>
        /// This method is automatically called by <see cref="DrawInspector"/> to
        /// draw the event properties. Override this method to customize the
        /// events shown in the Inspector. This is typically the method overridden
        /// when a derived behavior adds additional serialized event properties
        /// that should be displayed in the Inspector.
        /// </summary>
        protected virtual void DrawEvents()
        {
            DrawInteractorEvents();
        }

        /// <summary>
        /// Draw the core group of property fields. These are the main properties
        /// that appear before any other spaced section in the inspector.
        /// </summary>
        protected virtual void DrawCoreConfiguration()
        {
            DrawInteractionManagement();
            EditorGUILayout.PropertyField(m_AttachTransform, BaseContents.attachTransform);
            EditorGUILayout.PropertyField(m_StartingSelectedInteractable, BaseContents.startingSelectedInteractable);
        }

        /// <summary>
        /// Draw the property fields related to interaction management.
        /// </summary>
        protected virtual void DrawInteractionManagement()
        {
            EditorGUILayout.PropertyField(m_InteractionManager, BaseContents.interactionManager);
            EditorGUILayout.PropertyField(m_InteractionLayers, BaseContents.interactionLayers);
            if (XRInteractionEditorSettings.instance.showOldInteractionLayerMaskInInspector)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(m_InteractionLayerMask, BaseContents.interactionLayerMask);
                }
            }
        }

        /// <summary>
        /// Draw the Interactor Events foldout.
        /// </summary>
        /// <seealso cref="DrawInteractorEventsNested"/>
        protected virtual void DrawInteractorEvents()
        {
#pragma warning disable 618 // One-time migration of deprecated events.
            if (IsDeprecatedEventsInUse())
            {
                EditorGUILayout.HelpBox(BaseContents.deprecatedEventsInUse.text, MessageType.Warning);
                if (GUILayout.Button("Migrate Events"))
                {
                    serializedObject.ApplyModifiedProperties();
                    MigrateEvents(targets);
                    serializedObject.SetIsDifferentCacheDirty();
                    serializedObject.Update();
                }
            }
#pragma warning restore 618

            m_HoverEntered.isExpanded = EditorGUILayout.Foldout(m_HoverEntered.isExpanded, EditorGUIUtility.TrTempContent("Interactor Events"), true);
            if (m_HoverEntered.isExpanded)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    DrawInteractorEventsNested();
                }
            }
        }

        /// <summary>
        /// Draw the nested contents of the Interactor Events foldout.
        /// </summary>
        /// <seealso cref="DrawInteractorEvents"/>
        protected virtual void DrawInteractorEventsNested()
        {
            EditorGUILayout.LabelField(BaseContents.hoverEventsHeader, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_HoverEntered);
            if (m_OnHoverEnteredCalls.arraySize > 0 || m_OnHoverEnteredCalls.hasMultipleDifferentValues)
                EditorGUILayout.PropertyField(m_OnHoverEntered, BaseContents.onHoverEntered);
            EditorGUILayout.PropertyField(m_HoverExited);
            if (m_OnHoverExitedCalls.arraySize > 0 || m_OnHoverExitedCalls.hasMultipleDifferentValues)
                EditorGUILayout.PropertyField(m_OnHoverExited, BaseContents.onHoverExited);

            EditorGUILayout.LabelField(BaseContents.selectEventsHeader, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_SelectEntered);
            if (m_OnSelectEnteredCalls.arraySize > 0 || m_OnSelectEnteredCalls.hasMultipleDifferentValues)
                EditorGUILayout.PropertyField(m_OnSelectEntered, BaseContents.onSelectEntered);
            EditorGUILayout.PropertyField(m_SelectExited);
            if (m_OnSelectExitedCalls.arraySize > 0 || m_OnSelectExitedCalls.hasMultipleDifferentValues)
                EditorGUILayout.PropertyField(m_OnSelectExited, BaseContents.onSelectExited);
        }
    }
}
