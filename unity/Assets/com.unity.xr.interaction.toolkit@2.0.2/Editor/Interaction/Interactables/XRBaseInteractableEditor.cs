using System;
using UnityEditor.XR.Interaction.Toolkit.Utilities;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace UnityEditor.XR.Interaction.Toolkit
{
    /// <summary>
    /// Custom editor for an <see cref="XRBaseInteractable"/>.
    /// </summary>
    [CustomEditor(typeof(XRBaseInteractable), true), CanEditMultipleObjects]
    public partial class XRBaseInteractableEditor : BaseInteractionEditor
    {
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractable.interactionManager"/>.</summary>
        protected SerializedProperty m_InteractionManager;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractable.colliders"/>.</summary>
        protected SerializedProperty m_Colliders;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractable.interactionLayerMask"/>.</summary>
        protected SerializedProperty m_InteractionLayerMask;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractable.interactionLayers"/>.</summary>
        protected SerializedProperty m_InteractionLayers;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractable.selectMode"/>.</summary>
        protected SerializedProperty m_SelectMode;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractable.customReticle"/>.</summary>
        protected SerializedProperty m_CustomReticle;

        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractable.firstHoverEntered"/>.</summary>
        protected SerializedProperty m_FirstHoverEntered;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractable.lastHoverExited"/>.</summary>
        protected SerializedProperty m_LastHoverExited;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractable.hoverEntered"/>.</summary>
        protected SerializedProperty m_HoverEntered;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractable.hoverExited"/>.</summary>
        protected SerializedProperty m_HoverExited;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractable.firstSelectEntered"/>.</summary>
        protected SerializedProperty m_FirstSelectEntered;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractable.lastSelectExited"/>.</summary>
        protected SerializedProperty m_LastSelectExited;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractable.selectEntered"/>.</summary>
        protected SerializedProperty m_SelectEntered;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractable.selectExited"/>.</summary>
        protected SerializedProperty m_SelectExited;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractable.activated"/>.</summary>
        protected SerializedProperty m_Activated;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractable.deactivated"/>.</summary>
        protected SerializedProperty m_Deactivated;

        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractable.onFirstHoverEntered"/>.</summary>
        protected SerializedProperty m_OnFirstHoverEntered;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractable.onLastHoverExited"/>.</summary>
        protected SerializedProperty m_OnLastHoverExited;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractable.onHoverEntered"/>.</summary>
        protected SerializedProperty m_OnHoverEntered;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractable.onHoverExited"/>.</summary>
        protected SerializedProperty m_OnHoverExited;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractable.onSelectEntered"/>.</summary>
        protected SerializedProperty m_OnSelectEntered;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractable.onSelectExited"/>.</summary>
        protected SerializedProperty m_OnSelectExited;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractable.onSelectCanceled"/>.</summary>
        protected SerializedProperty m_OnSelectCanceled;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractable.onActivate"/>.</summary>
        protected SerializedProperty m_OnActivate;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseInteractable.onDeactivate"/>.</summary>
        protected SerializedProperty m_OnDeactivate;

        /// <summary><see cref="SerializedProperty"/> of the persistent calls backing <see cref="XRBaseInteractable.onFirstHoverEntered"/>.</summary>
        protected SerializedProperty m_OnFirstHoverEnteredCalls;
        /// <summary><see cref="SerializedProperty"/> of the persistent calls backing <see cref="XRBaseInteractable.onLastHoverExited"/>.</summary>
        protected SerializedProperty m_OnLastHoverExitedCalls;
        /// <summary><see cref="SerializedProperty"/> of the persistent calls backing <see cref="XRBaseInteractable.onHoverEntered"/>.</summary>
        protected SerializedProperty m_OnHoverEnteredCalls;
        /// <summary><see cref="SerializedProperty"/> of the persistent calls backing <see cref="XRBaseInteractable.onHoverExited"/>.</summary>
        protected SerializedProperty m_OnHoverExitedCalls;
        /// <summary><see cref="SerializedProperty"/> of the persistent calls backing <see cref="XRBaseInteractable.onSelectEntered"/>.</summary>
        protected SerializedProperty m_OnSelectEnteredCalls;
        /// <summary><see cref="SerializedProperty"/> of the persistent calls backing <see cref="XRBaseInteractable.onSelectExited"/>.</summary>
        protected SerializedProperty m_OnSelectExitedCalls;
        /// <summary><see cref="SerializedProperty"/> of the persistent calls backing <see cref="XRBaseInteractable.onSelectCanceled"/>.</summary>
        protected SerializedProperty m_OnSelectCanceledCalls;
        /// <summary><see cref="SerializedProperty"/> of the persistent calls backing <see cref="XRBaseInteractable.onActivate"/>.</summary>
        protected SerializedProperty m_OnActivateCalls;
        /// <summary><see cref="SerializedProperty"/> of the persistent calls backing <see cref="XRBaseInteractable.onDeactivate"/>.</summary>
        protected SerializedProperty m_OnDeactivateCalls;

        /// <summary>
        /// Whether <see cref="InteractableSelectMode.Multiple"/> is allowed by the script of the object being inspected.
        /// </summary>
        protected bool selectMultipleAllowed { get; private set; }

        /// <summary>
        /// Contents of GUI elements used by this editor.
        /// </summary>
        protected static class BaseContents
        {
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseInteractable.interactionManager"/>.</summary>
            public static readonly GUIContent interactionManager = EditorGUIUtility.TrTextContent("Interaction Manager", "The XR Interaction Manager that this Interactable will communicate with (will find one if None).");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseInteractable.colliders"/>.</summary>
            public static readonly GUIContent colliders = EditorGUIUtility.TrTextContent("Colliders", "Colliders to include when selecting/interacting with this Interactable (if empty, will use any child Colliders).");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseInteractable.interactionLayerMask"/>.</summary>
            public static readonly GUIContent interactionLayerMask = EditorGUIUtility.TrTextContent("Deprecated Interaction Layer Mask", "Deprecated Interaction Layer Mask that uses the Unity physics Layers. Hide this property by disabling \'Show Old Interaction Layer Mask In Inspector\' in the XR Interaction Toolkit project settings.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseInteractable.interactionLayers"/>.</summary>
            public static readonly GUIContent interactionLayers = EditorGUIUtility.TrTextContent("Interaction Layer Mask", "Allows interaction with Interactors whose Interaction Layer Mask overlaps with any Layer in this Interaction Layer Mask.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseInteractable.selectMode"/>.</summary>
            public static readonly GUIContent selectMode = EditorGUIUtility.TrTextContent("Select Mode", "The selection policy, either Single selection with swapping allowed, Single Locked selection without swapping, or Multiple selection.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseInteractable.customReticle"/>.</summary>
            public static readonly GUIContent customReticle = EditorGUIUtility.TrTextContent("Custom Reticle", "The reticle that will appear at the end of the line when it is valid.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseInteractable.onFirstHoverEntered"/>.</summary>
            public static readonly GUIContent onFirstHoverEntered = EditorGUIUtility.TrTextContent("(Deprecated) On First Hover Entered");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseInteractable.onLastHoverExited"/>.</summary>
            public static readonly GUIContent onLastHoverExited = EditorGUIUtility.TrTextContent("(Deprecated) On Last Hover Exited");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseInteractable.onHoverEntered"/>.</summary>
            public static readonly GUIContent onHoverEntered = EditorGUIUtility.TrTextContent("(Deprecated) On Hover Entered");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseInteractable.onHoverExited"/>.</summary>
            public static readonly GUIContent onHoverExited = EditorGUIUtility.TrTextContent("(Deprecated) On Hover Exited");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseInteractable.onSelectEntered"/>.</summary>
            public static readonly GUIContent onSelectEntered = EditorGUIUtility.TrTextContent("(Deprecated) On Select Entered");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseInteractable.onSelectExited"/>.</summary>
            public static readonly GUIContent onSelectExited = EditorGUIUtility.TrTextContent("(Deprecated) On Select Exited");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseInteractable.onSelectCanceled"/>.</summary>
            public static readonly GUIContent onSelectCanceled = EditorGUIUtility.TrTextContent("(Deprecated) On Select Canceled");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseInteractable.onActivate"/>.</summary>
            public static readonly GUIContent onActivate = EditorGUIUtility.TrTextContent("(Deprecated) On Activate");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseInteractable.onDeactivate"/>.</summary>
            public static readonly GUIContent onDeactivate = EditorGUIUtility.TrTextContent("(Deprecated) On Deactivate");
            /// <summary><see cref="GUIContent"/> for the header label of First/Last Hover events.</summary>
            public static readonly GUIContent firstLastHoverEventsHeader = EditorGUIUtility.TrTextContent("First/Last Hover", "Similar to Hover except called only when the first Interactor begins hovering over this Interactable as the sole hovering Interactor, or when the last remaining hovering Interactor ends hovering.");
            /// <summary><see cref="GUIContent"/> for the header label of Hover events.</summary>
            public static readonly GUIContent hoverEventsHeader = EditorGUIUtility.TrTextContent("Hover", "Called when an Interactor begins hovering over this Interactable (Entered), or ends hovering (Exited).");
            /// <summary><see cref="GUIContent"/> for the header label of First/Last Hover events.</summary>
            public static readonly GUIContent firstLastSelectEventsHeader = EditorGUIUtility.TrTextContent("First/Last Select", "Similar to Select except called only when the first Interactor begins selecting this Interactable as the sole selecting Interactor, or when the last remaining selecting Interactor ends selecting.");
            /// <summary><see cref="GUIContent"/> for the header label of Select events.</summary>
            public static readonly GUIContent selectEventsHeader = EditorGUIUtility.TrTextContent("Select", "Called when an Interactor begins selecting this Interactable (Entered), or ends selecting (Exited).");
            /// <summary><see cref="GUIContent"/> for the header label of Activate events.</summary>
            public static readonly GUIContent activateEventsHeader = EditorGUIUtility.TrTextContent("Activate", "Called when the Interactor that is selecting this Interactable sends a command to activate (Activated), or deactivate (Deactivated). Not to be confused with the active state of a GameObject.");

            /// <summary>The help box message when Multiple is not supported by the script and the serialized field is Multiple.</summary>
            /// <seealso cref="selectMultipleAllowed"/>
            public static readonly GUIContent multipleNotSupported = EditorGUIUtility.TrTextContent("Multiple is not supported by this component script.");

            /// <summary>The help box message when deprecated Interactable Events are being used.</summary>
            public static readonly GUIContent deprecatedEventsInUse = EditorGUIUtility.TrTextContent("Some deprecated Interactable Events are being used. These deprecated events will be removed in a future version. Please convert these to use the newer events, and update script method signatures for Dynamic listeners.");
        }

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        /// <seealso cref="MonoBehaviour"/>
        protected virtual void OnEnable()
        {
            m_InteractionManager = serializedObject.FindProperty("m_InteractionManager");
            m_Colliders = serializedObject.FindProperty("m_Colliders");
            m_InteractionLayerMask = serializedObject.FindProperty("m_InteractionLayerMask");
            m_InteractionLayers = serializedObject.FindProperty("m_InteractionLayers");
            m_SelectMode = serializedObject.FindProperty("m_SelectMode");
            m_CustomReticle = serializedObject.FindProperty("m_CustomReticle");

            m_FirstHoverEntered = serializedObject.FindProperty("m_FirstHoverEntered");
            m_LastHoverExited = serializedObject.FindProperty("m_LastHoverExited");
            m_HoverEntered = serializedObject.FindProperty("m_HoverEntered");
            m_HoverExited = serializedObject.FindProperty("m_HoverExited");
            m_FirstSelectEntered = serializedObject.FindProperty("m_FirstSelectEntered");
            m_LastSelectExited = serializedObject.FindProperty("m_LastSelectExited");
            m_SelectEntered = serializedObject.FindProperty("m_SelectEntered");
            m_SelectExited = serializedObject.FindProperty("m_SelectExited");
            m_Activated = serializedObject.FindProperty("m_Activated");
            m_Deactivated = serializedObject.FindProperty("m_Deactivated");

            m_OnFirstHoverEntered = serializedObject.FindProperty("m_OnFirstHoverEntered");
            m_OnHoverEntered = serializedObject.FindProperty("m_OnHoverEntered");
            m_OnHoverExited = serializedObject.FindProperty("m_OnHoverExited");
            m_OnLastHoverExited = serializedObject.FindProperty("m_OnLastHoverExited");
            m_OnSelectEntered = serializedObject.FindProperty("m_OnSelectEntered");
            m_OnSelectExited = serializedObject.FindProperty("m_OnSelectExited");
            m_OnSelectCanceled = serializedObject.FindProperty("m_OnSelectCanceled");
            m_OnActivate = serializedObject.FindProperty("m_OnActivate");
            m_OnDeactivate = serializedObject.FindProperty("m_OnDeactivate");

            m_OnFirstHoverEnteredCalls = m_OnFirstHoverEntered.FindPropertyRelative("m_PersistentCalls.m_Calls");
            m_OnLastHoverExitedCalls = m_OnLastHoverExited.FindPropertyRelative("m_PersistentCalls.m_Calls");
            m_OnHoverEnteredCalls = m_OnHoverEntered.FindPropertyRelative("m_PersistentCalls.m_Calls");
            m_OnHoverExitedCalls = m_OnHoverExited.FindPropertyRelative("m_PersistentCalls.m_Calls");
            m_OnSelectEnteredCalls = m_OnSelectEntered.FindPropertyRelative("m_PersistentCalls.m_Calls");
            m_OnSelectExitedCalls = m_OnSelectExited.FindPropertyRelative("m_PersistentCalls.m_Calls");
            m_OnSelectCanceledCalls = m_OnSelectCanceled.FindPropertyRelative("m_PersistentCalls.m_Calls");
            m_OnActivateCalls = m_OnActivate.FindPropertyRelative("m_PersistentCalls.m_Calls");
            m_OnDeactivateCalls = m_OnDeactivate.FindPropertyRelative("m_PersistentCalls.m_Calls");

            var attribute = (CanSelectMultipleAttribute)Attribute.GetCustomAttribute(target.GetType(), typeof(CanSelectMultipleAttribute));
            selectMultipleAllowed = attribute?.allowMultiple ?? true;
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
        /// when a derived behavior adds additional serialized properties
        /// that should be displayed in the Inspector.
        /// </summary>
        protected virtual void DrawProperties()
        {
            DrawCoreConfiguration();
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
            DrawInteractableEvents();
        }

        /// <summary>
        /// Draw the core group of property fields. These are the main properties
        /// that appear before any other spaced section in the inspector.
        /// </summary>
        protected virtual void DrawCoreConfiguration()
        {
            DrawInteractionManagement();
            EditorGUILayout.PropertyField(m_CustomReticle, BaseContents.customReticle);
            DrawSelectionConfiguration();
        }

        /// <summary>
        /// Draw the property fields related to selection configuration.
        /// </summary>
        protected virtual void DrawSelectionConfiguration()
        {
            if (m_SelectMode.intValue == (int)InteractableSelectMode.Multiple && !selectMultipleAllowed)
                EditorGUILayout.HelpBox(BaseContents.multipleNotSupported.text, MessageType.Error);

            XRInteractionEditorGUI.EnumPropertyField<InteractableSelectMode>(m_SelectMode, BaseContents.selectMode, IsSelectModeOptionEnabled);
        }

        bool IsSelectModeOptionEnabled(Enum arg) => (InteractableSelectMode)arg != InteractableSelectMode.Multiple || selectMultipleAllowed;

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
            
            EditorGUILayout.PropertyField(m_Colliders, BaseContents.colliders, true);
        }

        /// <summary>
        /// Draw the Interactable Events foldout.
        /// </summary>
        /// <seealso cref="DrawInteractableEventsNested"/>
        protected virtual void DrawInteractableEvents()
        {
#pragma warning disable 618 // One-time migration of deprecated events.
            if (IsDeprecatedEventsInUse())
            {
                EditorGUILayout.HelpBox(BaseContents.deprecatedEventsInUse.text, MessageType.Warning);
                if (GUILayout.Button("Migrate Events"))
                {
                    if (m_OnSelectCanceledCalls.arraySize > 0 || m_OnSelectCanceledCalls.hasMultipleDifferentValues)
                        Debug.LogWarning("Unable to migrate the deprecated On Select Canceled event since there" +
                            " is no corresponding event as Select Exited will fire in both cases.", target);

                    serializedObject.ApplyModifiedProperties();
                    MigrateEvents(targets);
                    serializedObject.SetIsDifferentCacheDirty();
                    serializedObject.Update();
                }
            }
#pragma warning restore 618

            m_FirstHoverEntered.isExpanded = EditorGUILayout.Foldout(m_FirstHoverEntered.isExpanded, EditorGUIUtility.TrTempContent("Interactable Events"), true);
            if (m_FirstHoverEntered.isExpanded)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    DrawInteractableEventsNested();
                }
            }
        }

        /// <summary>
        /// Draw the nested contents of the Interactable Events foldout.
        /// </summary>
        /// <seealso cref="DrawInteractableEvents"/>
        protected virtual void DrawInteractableEventsNested()
        {
            EditorGUILayout.LabelField(BaseContents.firstLastHoverEventsHeader, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_FirstHoverEntered);
            if (m_OnFirstHoverEnteredCalls.arraySize > 0 || m_OnFirstHoverEnteredCalls.hasMultipleDifferentValues)
                EditorGUILayout.PropertyField(m_OnFirstHoverEntered, BaseContents.onFirstHoverEntered);
            EditorGUILayout.PropertyField(m_LastHoverExited);
            if (m_OnLastHoverExitedCalls.arraySize > 0 || m_OnLastHoverExitedCalls.hasMultipleDifferentValues)
                EditorGUILayout.PropertyField(m_OnLastHoverExited, BaseContents.onLastHoverExited);

            EditorGUILayout.LabelField(BaseContents.hoverEventsHeader, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_HoverEntered);
            if (m_OnHoverEnteredCalls.arraySize > 0 || m_OnHoverEnteredCalls.hasMultipleDifferentValues)
                EditorGUILayout.PropertyField(m_OnHoverEntered, BaseContents.onHoverEntered);
            EditorGUILayout.PropertyField(m_HoverExited);
            if (m_OnHoverExitedCalls.arraySize > 0 || m_OnHoverExitedCalls.hasMultipleDifferentValues)
                EditorGUILayout.PropertyField(m_OnHoverExited, BaseContents.onHoverExited);

            EditorGUILayout.LabelField(BaseContents.firstLastSelectEventsHeader, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_FirstSelectEntered);
            EditorGUILayout.PropertyField(m_LastSelectExited);

            EditorGUILayout.LabelField(BaseContents.selectEventsHeader, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_SelectEntered);
            if (m_OnSelectEnteredCalls.arraySize > 0 || m_OnSelectEnteredCalls.hasMultipleDifferentValues)
                EditorGUILayout.PropertyField(m_OnSelectEntered, BaseContents.onSelectEntered);
            EditorGUILayout.PropertyField(m_SelectExited);
            if (m_OnSelectExitedCalls.arraySize > 0 || m_OnSelectExitedCalls.hasMultipleDifferentValues)
                EditorGUILayout.PropertyField(m_OnSelectExited, BaseContents.onSelectExited);
            if (m_OnSelectCanceledCalls.arraySize > 0 || m_OnSelectCanceledCalls.hasMultipleDifferentValues)
                EditorGUILayout.PropertyField(m_OnSelectCanceled, BaseContents.onSelectCanceled);

            EditorGUILayout.LabelField(BaseContents.activateEventsHeader, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_Activated);
            if (m_OnActivateCalls.arraySize > 0 || m_OnActivateCalls.hasMultipleDifferentValues)
                EditorGUILayout.PropertyField(m_OnActivate, BaseContents.onActivate);
            EditorGUILayout.PropertyField(m_Deactivated);
            if (m_OnDeactivateCalls.arraySize > 0 || m_OnDeactivateCalls.hasMultipleDifferentValues)
                EditorGUILayout.PropertyField(m_OnDeactivate, BaseContents.onDeactivate);
        }
    }
}
