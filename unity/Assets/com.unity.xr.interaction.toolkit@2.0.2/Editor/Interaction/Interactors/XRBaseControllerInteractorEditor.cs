using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace UnityEditor.XR.Interaction.Toolkit
{
    /// <summary>
    /// Custom editor for an <see cref="XRBaseControllerInteractor"/>.
    /// </summary>
    [CustomEditor(typeof(XRBaseControllerInteractor), true), CanEditMultipleObjects]
    public class XRBaseControllerInteractorEditor : XRBaseInteractorEditor
    {
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseControllerInteractor.selectActionTrigger"/>.</summary>
        protected SerializedProperty m_SelectActionTrigger;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseControllerInteractor.hideControllerOnSelect"/>.</summary>
        protected SerializedProperty m_HideControllerOnSelect;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseControllerInteractor.allowHoveredActivate"/>.</summary>
        protected SerializedProperty m_AllowHoveredActivate;

        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseControllerInteractor.playAudioClipOnSelectEntered"/>.</summary>
        protected SerializedProperty m_PlayAudioClipOnSelectEntered;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseControllerInteractor.audioClipForOnSelectEntered"/>.</summary>
        protected SerializedProperty m_AudioClipForOnSelectEntered;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseControllerInteractor.playAudioClipOnSelectExited"/>.</summary>
        protected SerializedProperty m_PlayAudioClipOnSelectExited;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseControllerInteractor.audioClipForOnSelectExited"/>.</summary>
        protected SerializedProperty m_AudioClipForOnSelectExited;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseControllerInteractor.playAudioClipOnSelectCanceled"/>.</summary>
        protected SerializedProperty m_PlayAudioClipOnSelectCanceled;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseControllerInteractor.audioClipForOnSelectCanceled"/>.</summary>
        protected SerializedProperty m_AudioClipForOnSelectCanceled;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseControllerInteractor.playAudioClipOnHoverEntered"/>.</summary>
        protected SerializedProperty m_PlayAudioClipOnHoverEntered;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseControllerInteractor.audioClipForOnHoverEntered"/>.</summary>
        protected SerializedProperty m_AudioClipForOnHoverEntered;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseControllerInteractor.playAudioClipOnHoverExited"/>.</summary>
        protected SerializedProperty m_PlayAudioClipOnHoverExited;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseControllerInteractor.audioClipForOnHoverExited"/>.</summary>
        protected SerializedProperty m_AudioClipForOnHoverExited;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseControllerInteractor.playAudioClipOnHoverCanceled"/>.</summary>
        protected SerializedProperty m_PlayAudioClipOnHoverCanceled;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseControllerInteractor.audioClipForOnHoverCanceled"/>.</summary>
        protected SerializedProperty m_AudioClipForOnHoverCanceled;

        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseControllerInteractor.playHapticsOnSelectEntered"/>.</summary>
        protected SerializedProperty m_PlayHapticsOnSelectEntered;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseControllerInteractor.hapticSelectEnterIntensity"/>.</summary>
        protected SerializedProperty m_HapticSelectEnterIntensity;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseControllerInteractor.hapticSelectEnterDuration"/>.</summary>
        protected SerializedProperty m_HapticSelectEnterDuration;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseControllerInteractor.playHapticsOnHoverEntered"/>.</summary>
        protected SerializedProperty m_PlayHapticsOnHoverEntered;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseControllerInteractor.hapticHoverEnterIntensity"/>.</summary>
        protected SerializedProperty m_HapticHoverEnterIntensity;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseControllerInteractor.hapticHoverEnterDuration"/>.</summary>
        protected SerializedProperty m_HapticHoverEnterDuration;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseControllerInteractor.playHapticsOnSelectExited"/>.</summary>
        protected SerializedProperty m_PlayHapticsOnSelectExited;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseControllerInteractor.hapticSelectExitIntensity"/>.</summary>
        protected SerializedProperty m_HapticSelectExitIntensity;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseControllerInteractor.hapticSelectExitDuration"/>.</summary>
        protected SerializedProperty m_HapticSelectExitDuration;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseControllerInteractor.playHapticsOnSelectCanceled"/>.</summary>
        protected SerializedProperty m_PlayHapticsOnSelectCanceled;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseControllerInteractor.hapticSelectCancelIntensity"/>.</summary>
        protected SerializedProperty m_HapticSelectCancelIntensity;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseControllerInteractor.hapticSelectCancelDuration"/>.</summary>
        protected SerializedProperty m_HapticSelectCancelDuration;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseControllerInteractor.playHapticsOnHoverExited"/>.</summary>
        protected SerializedProperty m_PlayHapticsOnHoverExited;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseControllerInteractor.hapticHoverExitIntensity"/>.</summary>
        protected SerializedProperty m_HapticHoverExitIntensity;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseControllerInteractor.hapticHoverExitDuration"/>.</summary>
        protected SerializedProperty m_HapticHoverExitDuration;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseControllerInteractor.playHapticsOnHoverCanceled"/>.</summary>
        protected SerializedProperty m_PlayHapticsOnHoverCanceled;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseControllerInteractor.hapticHoverCancelIntensity"/>.</summary>
        protected SerializedProperty m_HapticHoverCancelIntensity;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRBaseControllerInteractor.hapticHoverCancelDuration"/>.</summary>
        protected SerializedProperty m_HapticHoverCancelDuration;

        /// <summary>
        /// Contents of GUI elements used by this editor.
        /// </summary>
        protected static class BaseControllerContents
        {
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseControllerInteractor.selectActionTrigger"/>.</summary>
            public static readonly GUIContent selectActionTrigger = EditorGUIUtility.TrTextContent("Select Action Trigger", "Choose how the select action is triggered, either by current state, state transition, toggle when the select button is pressed, or sticky toggle when the select button is pressed and deselect the second time the select button is depressed.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseControllerInteractor.hideControllerOnSelect"/>.</summary>
            public static readonly GUIContent hideControllerOnSelect = EditorGUIUtility.TrTextContent("Hide Controller On Select", "Hide the controller model on select.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseControllerInteractor.allowHoveredActivate"/>.</summary>
            public static readonly GUIContent allowHoveredActivate = EditorGUIUtility.TrTextContent("Allow Hovered Activate", "Send activate and deactivate events to interactables that this interactor is hovered over but not selected when there is no current selection.");

            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseControllerInteractor.playAudioClipOnSelectEntered"/>.</summary>
            public static readonly GUIContent playAudioClipOnSelectEntered = EditorGUIUtility.TrTextContent("On Select Entered", "Play an audio clip when the Select state is entered.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseControllerInteractor.audioClipForOnSelectEntered"/>.</summary>
            public static readonly GUIContent audioClipForOnSelectEntered = EditorGUIUtility.TrTextContent("AudioClip To Play", "The audio clip to play when the Select state is entered.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseControllerInteractor.playAudioClipOnSelectExited"/>.</summary>
            public static readonly GUIContent playAudioClipOnSelectExited = EditorGUIUtility.TrTextContent("On Select Exited", "Play an audio clip when the Select state is exited without being canceled.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseControllerInteractor.audioClipForOnSelectExited"/>.</summary>
            public static readonly GUIContent audioClipForOnSelectExited = EditorGUIUtility.TrTextContent("AudioClip To Play", "The audio clip to play when the Select state is exited without being canceled.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseControllerInteractor.playAudioClipOnSelectCanceled"/>.</summary>
            public static readonly GUIContent playAudioClipOnSelectCanceled = EditorGUIUtility.TrTextContent("On Select Canceled", "Play an audio clip when the Select state is exited due to being canceled.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseControllerInteractor.audioClipForOnSelectCanceled"/>.</summary>
            public static readonly GUIContent audioClipForOnSelectCanceled = EditorGUIUtility.TrTextContent("AudioClip To Play", "The audio clip to play when the Select state is exited due to being canceled.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseControllerInteractor.playAudioClipOnHoverEntered"/>.</summary>
            public static readonly GUIContent playAudioClipOnHoverEntered = EditorGUIUtility.TrTextContent("On Hover Entered", "Play an audio clip when the Hover state is entered.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseControllerInteractor.audioClipForOnHoverEntered"/>.</summary>
            public static readonly GUIContent audioClipForOnHoverEntered = EditorGUIUtility.TrTextContent("AudioClip To Play", "The audio clip to play when the Hover state is entered.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseControllerInteractor.playAudioClipOnHoverExited"/>.</summary>
            public static readonly GUIContent playAudioClipOnHoverExited = EditorGUIUtility.TrTextContent("On Hover Exited", "Play an audio clip when the Hover state is exited without being canceled.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseControllerInteractor.audioClipForOnHoverExited"/>.</summary>
            public static readonly GUIContent audioClipForOnHoverExited = EditorGUIUtility.TrTextContent("AudioClip To Play", "The audio clip to play when the Hover state is exited without being canceled.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseControllerInteractor.playAudioClipOnHoverCanceled"/>.</summary>
            public static readonly GUIContent playAudioClipOnHoverCanceled = EditorGUIUtility.TrTextContent("On Hover Canceled", "Play an audio clip when the Hover state is exited due to being canceled.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseControllerInteractor.audioClipForOnHoverCanceled"/>.</summary>
            public static readonly GUIContent audioClipForOnHoverCanceled = EditorGUIUtility.TrTextContent("AudioClip To Play", "The audio clip to play when the Hover state is exited due to being canceled.");

            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseControllerInteractor.playHapticsOnSelectEntered"/>.</summary>
            public static readonly GUIContent playHapticsOnSelectEntered = EditorGUIUtility.TrTextContent("On Select Entered", "Play haptics when the Select state is entered.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseControllerInteractor.hapticSelectEnterIntensity"/>.</summary>
            public static readonly GUIContent hapticSelectEnterIntensity = EditorGUIUtility.TrTextContent("Haptic Intensity", "Haptics intensity to play when the Select state is entered.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseControllerInteractor.hapticSelectEnterDuration"/>.</summary>
            public static readonly GUIContent hapticSelectEnterDuration = EditorGUIUtility.TrTextContent("Duration", "Haptics duration (in seconds) to play when the Select state is entered.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseControllerInteractor.playHapticsOnHoverEntered"/>.</summary>
            public static readonly GUIContent playHapticsOnHoverEntered = EditorGUIUtility.TrTextContent("On Hover Entered", "Play haptics when the Hover State is entered.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseControllerInteractor.hapticHoverEnterIntensity"/>.</summary>
            public static readonly GUIContent hapticHoverEnterIntensity = EditorGUIUtility.TrTextContent("Haptic Intensity", "Haptics intensity to play when the Hover state is entered.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseControllerInteractor.hapticHoverEnterDuration"/>.</summary>
            public static readonly GUIContent hapticHoverEnterDuration = EditorGUIUtility.TrTextContent("Duration", "Haptics duration (in seconds) to play when the Hover state is entered.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseControllerInteractor.playHapticsOnSelectExited"/>.</summary>
            public static readonly GUIContent playHapticsOnSelectExited = EditorGUIUtility.TrTextContent("On Select Exited", "Play haptics when the Select state is exited without being canceled.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseControllerInteractor.hapticSelectExitIntensity"/>.</summary>
            public static readonly GUIContent hapticSelectExitIntensity = EditorGUIUtility.TrTextContent("Haptic Intensity", "Haptics intensity to play when the Select state is exited without being canceled.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseControllerInteractor.hapticSelectExitDuration"/>.</summary>
            public static readonly GUIContent hapticSelectExitDuration = EditorGUIUtility.TrTextContent("Duration", "Haptics duration (in seconds) to play when the Select state is exited without being canceled.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseControllerInteractor.playHapticsOnSelectCanceled"/>.</summary>
            public static readonly GUIContent playHapticsOnSelectCanceled = EditorGUIUtility.TrTextContent("On Select Canceled", "Play haptics when the Select state is exited due to being canceled.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseControllerInteractor.hapticSelectCancelIntensity"/>.</summary>
            public static readonly GUIContent hapticSelectCancelIntensity = EditorGUIUtility.TrTextContent("Haptic Intensity", "Haptics intensity to play when the Select state is exited due to being canceled.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseControllerInteractor.hapticSelectCancelDuration"/>.</summary>
            public static readonly GUIContent hapticSelectCancelDuration = EditorGUIUtility.TrTextContent("Duration", "Haptics duration (in seconds) to play when the Select state is exited due to being canceled.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseControllerInteractor.playHapticsOnHoverExited"/>.</summary>
            public static readonly GUIContent playHapticsOnHoverExited = EditorGUIUtility.TrTextContent("On Hover Exited", "Play haptics when the Hover state is exited without being canceled.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseControllerInteractor.hapticHoverExitIntensity"/>.</summary>
            public static readonly GUIContent hapticHoverExitIntensity = EditorGUIUtility.TrTextContent("Haptic Intensity", "Haptics intensity to play when the Hover state is exited without being canceled.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseControllerInteractor.hapticHoverExitDuration"/>.</summary>
            public static readonly GUIContent hapticHoverExitDuration = EditorGUIUtility.TrTextContent("Duration", "Haptics duration (in seconds) to play when the Hover state is exited without being canceled.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseControllerInteractor.playHapticsOnHoverCanceled"/>.</summary>
            public static readonly GUIContent playHapticsOnHoverCanceled = EditorGUIUtility.TrTextContent("On Hover Canceled", "Play haptics when the Hover state is exited due to being canceled.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseControllerInteractor.hapticHoverCancelIntensity"/>.</summary>
            public static readonly GUIContent hapticHoverCancelIntensity = EditorGUIUtility.TrTextContent("Haptic Intensity", "Haptics intensity to play when the Hover state is exited due to being canceled.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRBaseControllerInteractor.hapticHoverCancelDuration"/>.</summary>
            public static readonly GUIContent hapticHoverCancelDuration = EditorGUIUtility.TrTextContent("Duration", "Haptics duration (in seconds) to play when the Hover state is exited due to being canceled.");

            /// <summary>The help box message when <see cref="XRBaseController"/> is missing.</summary>
            public static readonly string missingRequiredController = "This component requires the GameObject to have an XR Controller component. Add one to ensure this component can respond to user input.";
            /// <summary>The help box message when the <see cref="XRBaseInteractor.startingSelectedInteractable"/> will be instantly deselected due to the value of <see cref="XRBaseControllerInteractor.selectActionTrigger"/>.</summary>
            public static readonly string selectActionTriggerWarning = "A Starting Selected Interactable will be instantly deselected unless Select Action Trigger is set to Toggle or Sticky.";
        }

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();

            m_SelectActionTrigger = serializedObject.FindProperty("m_SelectActionTrigger");
            m_HideControllerOnSelect = serializedObject.FindProperty("m_HideControllerOnSelect");
            m_AllowHoveredActivate = serializedObject.FindProperty("m_AllowHoveredActivate");

            m_PlayAudioClipOnSelectEntered = serializedObject.FindProperty("m_PlayAudioClipOnSelectEntered");
            m_AudioClipForOnSelectEntered = serializedObject.FindProperty("m_AudioClipForOnSelectEntered");
            m_PlayAudioClipOnSelectExited = serializedObject.FindProperty("m_PlayAudioClipOnSelectExited");
            m_AudioClipForOnSelectExited = serializedObject.FindProperty("m_AudioClipForOnSelectExited");
            m_PlayAudioClipOnSelectCanceled = serializedObject.FindProperty("m_PlayAudioClipOnSelectCanceled");
            m_AudioClipForOnSelectCanceled = serializedObject.FindProperty("m_AudioClipForOnSelectCanceled");
            m_PlayAudioClipOnHoverEntered = serializedObject.FindProperty("m_PlayAudioClipOnHoverEntered");
            m_AudioClipForOnHoverEntered = serializedObject.FindProperty("m_AudioClipForOnHoverEntered");
            m_PlayAudioClipOnHoverExited = serializedObject.FindProperty("m_PlayAudioClipOnHoverExited");
            m_AudioClipForOnHoverExited = serializedObject.FindProperty("m_AudioClipForOnHoverExited");
            m_PlayAudioClipOnHoverCanceled = serializedObject.FindProperty("m_PlayAudioClipOnHoverCanceled");
            m_AudioClipForOnHoverCanceled = serializedObject.FindProperty("m_AudioClipForOnHoverCanceled");

            m_PlayHapticsOnSelectEntered = serializedObject.FindProperty("m_PlayHapticsOnSelectEntered");
            m_HapticSelectEnterIntensity = serializedObject.FindProperty("m_HapticSelectEnterIntensity");
            m_HapticSelectEnterDuration = serializedObject.FindProperty("m_HapticSelectEnterDuration");
            m_PlayHapticsOnHoverEntered = serializedObject.FindProperty("m_PlayHapticsOnHoverEntered");
            m_HapticHoverEnterIntensity = serializedObject.FindProperty("m_HapticHoverEnterIntensity");
            m_HapticHoverEnterDuration = serializedObject.FindProperty("m_HapticHoverEnterDuration");
            m_PlayHapticsOnSelectExited = serializedObject.FindProperty("m_PlayHapticsOnSelectExited");
            m_HapticSelectExitIntensity = serializedObject.FindProperty("m_HapticSelectExitIntensity");
            m_HapticSelectExitDuration = serializedObject.FindProperty("m_HapticSelectExitDuration");
            m_PlayHapticsOnSelectCanceled = serializedObject.FindProperty("m_PlayHapticsOnSelectCanceled");
            m_HapticSelectCancelIntensity = serializedObject.FindProperty("m_HapticSelectCancelIntensity");
            m_HapticSelectCancelDuration = serializedObject.FindProperty("m_HapticSelectCancelDuration");
            m_PlayHapticsOnHoverExited = serializedObject.FindProperty("m_PlayHapticsOnHoverExited");
            m_HapticHoverExitIntensity = serializedObject.FindProperty("m_HapticHoverExitIntensity");
            m_HapticHoverExitDuration = serializedObject.FindProperty("m_HapticHoverExitDuration");
            m_PlayHapticsOnHoverCanceled = serializedObject.FindProperty("m_PlayHapticsOnHoverCanceled");
            m_HapticHoverCancelIntensity = serializedObject.FindProperty("m_HapticHoverCancelIntensity");
            m_HapticHoverCancelDuration = serializedObject.FindProperty("m_HapticHoverCancelDuration");
        }

        /// <inheritdoc />
        protected override void DrawBeforeProperties()
        {
            base.DrawBeforeProperties();
            VerifyControllerPresent();
        }

        /// <inheritdoc />
        protected override void DrawProperties()
        {
            // Not calling base method to completely override drawn properties

            DrawCoreConfiguration();
            DrawSelectActionTrigger();
            EditorGUILayout.PropertyField(m_KeepSelectedTargetValid, BaseContents.keepSelectedTargetValid);
            EditorGUILayout.PropertyField(m_HideControllerOnSelect, BaseControllerContents.hideControllerOnSelect);
            EditorGUILayout.PropertyField(m_AllowHoveredActivate, BaseControllerContents.allowHoveredActivate);
        }

        /// <inheritdoc />
        protected override void DrawEvents()
        {
            DrawAudioEvents();

            EditorGUILayout.Space();

            DrawHapticEvents();

            EditorGUILayout.Space();

            base.DrawEvents();
        }

        /// <summary>
        /// Verify that the required <see cref="XRBaseController"/> component is present
        /// and display a warning box if missing.
        /// </summary>
        protected virtual void VerifyControllerPresent()
        {
            foreach (var targetObject in serializedObject.targetObjects)
            {
                var interactor = (XRBaseControllerInteractor)targetObject;
                if (interactor.GetComponentInParent<XRBaseController>() == null)
                {
                    EditorGUILayout.HelpBox(BaseControllerContents.missingRequiredController, MessageType.Warning, true);
                    break;
                }
            }
        }

        /// <summary>
        /// Draw the Select Action Trigger property and display a warning box if misconfigured.
        /// </summary>
        protected virtual void DrawSelectActionTrigger()
        {
            EditorGUILayout.PropertyField(m_SelectActionTrigger, BaseControllerContents.selectActionTrigger);
            if (m_StartingSelectedInteractable.objectReferenceValue != null &&
                m_SelectActionTrigger.intValue != (int)XRBaseControllerInteractor.InputTriggerType.Toggle &&
                m_SelectActionTrigger.intValue != (int)XRBaseControllerInteractor.InputTriggerType.Sticky)
            {
                EditorGUILayout.HelpBox(BaseControllerContents.selectActionTriggerWarning, MessageType.Warning, true);
            }
        }

        /// <summary>
        /// Draw the Audio Events foldout.
        /// </summary>
        /// <seealso cref="DrawAudioEventsNested"/>
        protected virtual void DrawAudioEvents()
        {
            m_PlayAudioClipOnSelectEntered.isExpanded = EditorGUILayout.Foldout(m_PlayAudioClipOnSelectEntered.isExpanded, EditorGUIUtility.TrTempContent("Audio Events"), true);
            if (m_PlayAudioClipOnSelectEntered.isExpanded)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    DrawAudioEventsNested();
                }
            }
        }

        /// <summary>
        /// Draw the nested contents of the Audio Events foldout.
        /// </summary>
        /// <seealso cref="DrawAudioEvents"/>
        protected virtual void DrawAudioEventsNested()
        {
            EditorGUILayout.PropertyField(m_PlayAudioClipOnSelectEntered, BaseControllerContents.playAudioClipOnSelectEntered);
            if (m_PlayAudioClipOnSelectEntered.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(m_AudioClipForOnSelectEntered, BaseControllerContents.audioClipForOnSelectEntered);
                }
            }

            EditorGUILayout.PropertyField(m_PlayAudioClipOnSelectExited, BaseControllerContents.playAudioClipOnSelectExited);
            if (m_PlayAudioClipOnSelectExited.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(m_AudioClipForOnSelectExited, BaseControllerContents.audioClipForOnSelectExited);
                }
            }

            EditorGUILayout.PropertyField(m_PlayAudioClipOnSelectCanceled, BaseControllerContents.playAudioClipOnSelectCanceled);
            if (m_PlayAudioClipOnSelectCanceled.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(m_AudioClipForOnSelectCanceled, BaseControllerContents.audioClipForOnSelectCanceled);
                }
            }

            EditorGUILayout.PropertyField(m_PlayAudioClipOnHoverEntered, BaseControllerContents.playAudioClipOnHoverEntered);
            if (m_PlayAudioClipOnHoverEntered.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(m_AudioClipForOnHoverEntered, BaseControllerContents.audioClipForOnHoverEntered);
                }
            }

            EditorGUILayout.PropertyField(m_PlayAudioClipOnHoverExited, BaseControllerContents.playAudioClipOnHoverExited);
            if (m_PlayAudioClipOnHoverExited.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(m_AudioClipForOnHoverExited, BaseControllerContents.audioClipForOnHoverExited);
                }
            }

            EditorGUILayout.PropertyField(m_PlayAudioClipOnHoverCanceled, BaseControllerContents.playAudioClipOnHoverCanceled);
            if (m_PlayAudioClipOnHoverCanceled.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(m_AudioClipForOnHoverCanceled, BaseControllerContents.audioClipForOnHoverCanceled);
                }
            }
        }

        /// <summary>
        /// Draw the Haptic Events foldout.
        /// </summary>
        /// <seealso cref="DrawHapticEventsNested"/>
        protected virtual void DrawHapticEvents()
        {
            m_PlayHapticsOnSelectEntered.isExpanded = EditorGUILayout.Foldout(m_PlayHapticsOnSelectEntered.isExpanded, EditorGUIUtility.TrTempContent("Haptic Events"), true);
            if (m_PlayHapticsOnSelectEntered.isExpanded)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    DrawHapticEventsNested();
                }
            }
        }

        /// <summary>
        /// Draw the nested contents of the Haptic Events foldout.
        /// </summary>
        /// <seealso cref="DrawHapticEvents"/>
        protected virtual void DrawHapticEventsNested()
        {
            EditorGUILayout.PropertyField(m_PlayHapticsOnSelectEntered, BaseControllerContents.playHapticsOnSelectEntered);
            if (m_PlayHapticsOnSelectEntered.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(m_HapticSelectEnterIntensity, BaseControllerContents.hapticSelectEnterIntensity);
                    EditorGUILayout.PropertyField(m_HapticSelectEnterDuration, BaseControllerContents.hapticSelectEnterDuration);
                }
            }

            EditorGUILayout.PropertyField(m_PlayHapticsOnSelectExited, BaseControllerContents.playHapticsOnSelectExited);
            if (m_PlayHapticsOnSelectExited.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(m_HapticSelectExitIntensity, BaseControllerContents.hapticSelectExitIntensity);
                    EditorGUILayout.PropertyField(m_HapticSelectExitDuration, BaseControllerContents.hapticSelectExitDuration);
                }
            }

            EditorGUILayout.PropertyField(m_PlayHapticsOnSelectCanceled, BaseControllerContents.playHapticsOnSelectCanceled);
            if (m_PlayHapticsOnSelectCanceled.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(m_HapticSelectCancelIntensity, BaseControllerContents.hapticSelectCancelIntensity);
                    EditorGUILayout.PropertyField(m_HapticSelectCancelDuration, BaseControllerContents.hapticSelectCancelDuration);
                }
            }

            EditorGUILayout.PropertyField(m_PlayHapticsOnHoverEntered, BaseControllerContents.playHapticsOnHoverEntered);
            if (m_PlayHapticsOnHoverEntered.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(m_HapticHoverEnterIntensity, BaseControllerContents.hapticHoverEnterIntensity);
                    EditorGUILayout.PropertyField(m_HapticHoverEnterDuration, BaseControllerContents.hapticHoverEnterDuration);
                }
            }

            EditorGUILayout.PropertyField(m_PlayHapticsOnHoverExited, BaseControllerContents.playHapticsOnHoverExited);
            if (m_PlayHapticsOnHoverExited.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(m_HapticHoverExitIntensity, BaseControllerContents.hapticHoverExitIntensity);
                    EditorGUILayout.PropertyField(m_HapticHoverExitDuration, BaseControllerContents.hapticHoverExitDuration);
                }
            }

            EditorGUILayout.PropertyField(m_PlayHapticsOnHoverCanceled, BaseControllerContents.playHapticsOnHoverCanceled);
            if (m_PlayHapticsOnHoverCanceled.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(m_HapticHoverCancelIntensity, BaseControllerContents.hapticHoverCancelIntensity);
                    EditorGUILayout.PropertyField(m_HapticHoverCancelDuration, BaseControllerContents.hapticHoverCancelDuration);
                }
            }
        }
    }
}