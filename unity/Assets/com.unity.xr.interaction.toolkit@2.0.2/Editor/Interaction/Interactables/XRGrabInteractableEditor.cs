using UnityEditor.XR.Interaction.Toolkit.Utilities;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace UnityEditor.XR.Interaction.Toolkit
{
    /// <summary>
    /// Custom editor for an <see cref="XRGrabInteractable"/>.
    /// </summary>
    [CustomEditor(typeof(XRGrabInteractable), true), CanEditMultipleObjects]
    public class XRGrabInteractableEditor : XRBaseInteractableEditor
    {
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRGrabInteractable.attachTransform"/>.</summary>
        protected SerializedProperty m_AttachTransform;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRGrabInteractable.attachEaseInTime"/>.</summary>
        protected SerializedProperty m_AttachEaseInTime;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRGrabInteractable.movementType"/>.</summary>
        protected SerializedProperty m_MovementType;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRGrabInteractable.velocityDamping"/>.</summary>
        protected SerializedProperty m_VelocityDamping;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRGrabInteractable.velocityScale"/>.</summary>
        protected SerializedProperty m_VelocityScale;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRGrabInteractable.angularVelocityDamping"/>.</summary>
        protected SerializedProperty m_AngularVelocityDamping;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRGrabInteractable.angularVelocityScale"/>.</summary>
        protected SerializedProperty m_AngularVelocityScale;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRGrabInteractable.trackPosition"/>.</summary>
        protected SerializedProperty m_TrackPosition;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRGrabInteractable.smoothPosition"/>.</summary>
        protected SerializedProperty m_SmoothPosition;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRGrabInteractable.smoothPositionAmount"/>.</summary>
        protected SerializedProperty m_SmoothPositionAmount;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRGrabInteractable.tightenPosition"/>.</summary>
        protected SerializedProperty m_TightenPosition;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRGrabInteractable.trackRotation"/>.</summary>
        protected SerializedProperty m_TrackRotation;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRGrabInteractable.smoothRotation"/>.</summary>
        protected SerializedProperty m_SmoothRotation;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRGrabInteractable.smoothRotationAmount"/>.</summary>
        protected SerializedProperty m_SmoothRotationAmount;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRGrabInteractable.tightenRotation"/>.</summary>
        protected SerializedProperty m_TightenRotation;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRGrabInteractable.throwOnDetach"/>.</summary>
        protected SerializedProperty m_ThrowOnDetach;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRGrabInteractable.throwSmoothingDuration"/>.</summary>
        protected SerializedProperty m_ThrowSmoothingDuration;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRGrabInteractable.throwSmoothingCurve"/>.</summary>
        protected SerializedProperty m_ThrowSmoothingCurve;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRGrabInteractable.throwVelocityScale"/>.</summary>
        protected SerializedProperty m_ThrowVelocityScale;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRGrabInteractable.throwAngularVelocityScale"/>.</summary>
        protected SerializedProperty m_ThrowAngularVelocityScale;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRGrabInteractable.forceGravityOnDetach"/>.</summary>
        protected SerializedProperty m_ForceGravityOnDetach;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRGrabInteractable.retainTransformParent"/>.</summary>
        protected SerializedProperty m_RetainTransformParent;
        /// <summary><see cref="SerializedProperty"/> of the <see cref="SerializeField"/> backing <see cref="XRGrabInteractable.attachPointCompatibilityMode"/>.</summary>
        protected SerializedProperty m_AttachPointCompatibilityMode;

        /// <summary>Value to be checked before recalculate if the inspected object has a non-uniformly scaled parent.</summary>
        bool m_RecalculateHasNonUniformScale = true;
        /// <summary>Caches if the inspected object has a non-uniformly scaled parent.</summary>
        bool m_HasNonUniformScale;

        /// <summary>
        /// Contents of GUI elements used by this editor.
        /// </summary>
        protected static class Contents
        {
            /// <summary><see cref="GUIContent"/> for <see cref="XRGrabInteractable.attachTransform"/>.</summary>
            public static readonly GUIContent attachTransform = EditorGUIUtility.TrTextContent("Attach Transform", "The attachment point to use on this Interactable (will use this object's position if none set).");
            /// <summary><see cref="GUIContent"/> for <see cref="XRGrabInteractable.attachEaseInTime"/>.</summary>
            public static readonly GUIContent attachEaseInTime = EditorGUIUtility.TrTextContent("Attach Ease In Time", "Time in seconds to ease in the attach when selected (a value of 0 indicates no easing).");
            /// <summary><see cref="GUIContent"/> for <see cref="XRGrabInteractable.movementType"/>.</summary>
            public static readonly GUIContent movementType = EditorGUIUtility.TrTextContent("Movement Type", "Specifies how this object is moved when selected, either through setting the velocity of the Rigidbody, moving the kinematic Rigidbody during Fixed Update, or by directly updating the Transform each frame.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRGrabInteractable.velocityDamping"/>.</summary>
            public static readonly GUIContent velocityDamping = EditorGUIUtility.TrTextContent("Velocity Damping", "Scale factor of how much to dampen the existing velocity when tracking the position of the Interactor. The smaller the value, the longer it takes for the velocity to decay.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRGrabInteractable.velocityScale"/>.</summary>
            public static readonly GUIContent velocityScale = EditorGUIUtility.TrTextContent("Velocity Scale", "Scale factor applied to the tracked velocity while updating the Rigidbody when tracking the position of the Interactor.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRGrabInteractable.angularVelocityDamping"/>.</summary>
            public static readonly GUIContent angularVelocityDamping = EditorGUIUtility.TrTextContent("Angular Velocity Damping", "Scale factor of how much to dampen the existing angular velocity when tracking the rotation of the Interactor. The smaller the value, the longer it takes for the angular velocity to decay.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRGrabInteractable.angularVelocityScale"/>.</summary>
            public static readonly GUIContent angularVelocityScale = EditorGUIUtility.TrTextContent("Angular Velocity Scale", "Scale factor applied to the tracked angular velocity while updating the Rigidbody when tracking the rotation of the Interactor.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRGrabInteractable.trackPosition"/>.</summary>
            public static readonly GUIContent trackPosition = EditorGUIUtility.TrTextContent("Track Position", "Whether this object should follow the position of the Interactor when selected.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRGrabInteractable.smoothPosition"/>.</summary>
            public static readonly GUIContent smoothPosition = EditorGUIUtility.TrTextContent("Smooth Position", "Apply smoothing while following the position of the Interactor when selected.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRGrabInteractable.smoothPositionAmount"/>.</summary>
            public static readonly GUIContent smoothPositionAmount = EditorGUIUtility.TrTextContent("Smooth Position Amount", "Scale factor for how much smoothing is applied while following the position of the Interactor when selected. The larger the value, the closer this object will remain to the position of the Interactor.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRGrabInteractable.tightenPosition"/>.</summary>
            public static readonly GUIContent tightenPosition = EditorGUIUtility.TrTextContent("Tighten Position", "Reduces the maximum follow position difference when using smoothing. The value ranges from 0 meaning no bias in the smoothed follow distance, to 1 meaning effectively no smoothing at all.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRGrabInteractable.trackRotation"/>.</summary>
            public static readonly GUIContent trackRotation = EditorGUIUtility.TrTextContent("Track Rotation", "Whether this object should follow the rotation of the Interactor when selected.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRGrabInteractable.smoothRotation"/>.</summary>
            public static readonly GUIContent smoothRotation = EditorGUIUtility.TrTextContent("Smooth Rotation", "Apply smoothing while following the rotation of the Interactor when selected.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRGrabInteractable.smoothRotationAmount"/>.</summary>
            public static readonly GUIContent smoothRotationAmount = EditorGUIUtility.TrTextContent("Smooth Rotation Amount", "Scale factor for how much smoothing is applied while following the rotation of the Interactor when selected. The larger the value, the closer this object will remain to the rotation of the Interactor.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRGrabInteractable.tightenRotation"/>.</summary>
            public static readonly GUIContent tightenRotation = EditorGUIUtility.TrTextContent("Tighten Rotation", "Reduces the maximum follow rotation difference when using smoothing. The value ranges from 0 meaning no bias in the smoothed follow rotation, to 1 meaning effectively no smoothing at all.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRGrabInteractable.throwOnDetach"/>.</summary>
            public static readonly GUIContent throwOnDetach = EditorGUIUtility.TrTextContent("Throw On Detach", "Whether this object inherits the velocity of the Interactor when released.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRGrabInteractable.throwSmoothingDuration"/>.</summary>
            public static readonly GUIContent throwSmoothingDuration = EditorGUIUtility.TrTextContent("Throw Smoothing Duration", "Time period to average thrown velocity over.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRGrabInteractable.throwSmoothingCurve"/>.</summary>
            public static readonly GUIContent throwSmoothingCurve = EditorGUIUtility.TrTextContent("Throw Smoothing Curve", "The curve to use to weight thrown velocity smoothing (most recent frames to the right).");
            /// <summary><see cref="GUIContent"/> for <see cref="XRGrabInteractable.throwVelocityScale"/>.</summary>
            public static readonly GUIContent throwVelocityScale = EditorGUIUtility.TrTextContent("Throw Velocity Scale", "Scale factor applied to this object's inherited velocity of the Interactor when released.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRGrabInteractable.throwAngularVelocityScale"/>.</summary>
            public static readonly GUIContent throwAngularVelocityScale = EditorGUIUtility.TrTextContent("Throw Angular Velocity Scale", "Scale factor applied to this object's inherited angular velocity of the Interactor when released.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRGrabInteractable.forceGravityOnDetach"/>.</summary>
            public static readonly GUIContent forceGravityOnDetach = EditorGUIUtility.TrTextContent("Force Gravity On Detach", "Force this object to have gravity when released (will still use pre-grab value if this is false).");
            /// <summary><see cref="GUIContent"/> for <see cref="XRGrabInteractable.retainTransformParent"/>.</summary>
            public static readonly GUIContent retainTransformParent = EditorGUIUtility.TrTextContent("Retain Transform Parent", "Whether to set the parent of this object back to its original parent this object was a child of after this object is dropped.");
            /// <summary><see cref="GUIContent"/> for <see cref="XRGrabInteractable.attachPointCompatibilityMode"/>.</summary>
            public static readonly GUIContent attachPointCompatibilityMode = EditorGUIUtility.TrTextContent("Attach Point Compatibility Mode", "Use Default for consistent attach points between all Movement Type values. Use Legacy for older projects that want to maintain the incorrect method which was partially based on center of mass.");

            /// <summary>Message for non-uniformly scaled parent.</summary>
            public static readonly string nonUniformScaledParentWarning = "When a child object has a non-uniformly scaled parent and is rotated relative to that parent, it may appear skewed. To avoid this, use uniform scale in all parents' Transform of this object.";
            
            /// <summary>Array of type <see cref="GUIContent"/> for the options shown in the popup for <see cref="XRGrabInteractable.attachPointCompatibilityMode"/>.</summary>
            public static readonly GUIContent[] attachPointCompatibilityModeOptions =
            {
                EditorGUIUtility.TrTextContent("Default (Recommended)"),
                EditorGUIUtility.TrTextContent("Legacy (Obsolete)")
            };
        }

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();

            m_AttachTransform = serializedObject.FindProperty("m_AttachTransform");
            m_AttachEaseInTime = serializedObject.FindProperty("m_AttachEaseInTime");
            m_MovementType = serializedObject.FindProperty("m_MovementType");
            m_VelocityDamping = serializedObject.FindProperty("m_VelocityDamping");
            m_VelocityScale = serializedObject.FindProperty("m_VelocityScale");
            m_AngularVelocityDamping = serializedObject.FindProperty("m_AngularVelocityDamping");
            m_AngularVelocityScale = serializedObject.FindProperty("m_AngularVelocityScale");
            m_TrackPosition = serializedObject.FindProperty("m_TrackPosition");
            m_SmoothPosition = serializedObject.FindProperty("m_SmoothPosition");
            m_SmoothPositionAmount = serializedObject.FindProperty("m_SmoothPositionAmount");
            m_TightenPosition = serializedObject.FindProperty("m_TightenPosition");
            m_TrackRotation = serializedObject.FindProperty("m_TrackRotation");
            m_SmoothRotation = serializedObject.FindProperty("m_SmoothRotation");
            m_SmoothRotationAmount = serializedObject.FindProperty("m_SmoothRotationAmount");
            m_TightenRotation = serializedObject.FindProperty("m_TightenRotation");
            m_ThrowOnDetach = serializedObject.FindProperty("m_ThrowOnDetach");
            m_ThrowSmoothingDuration = serializedObject.FindProperty("m_ThrowSmoothingDuration");
            m_ThrowSmoothingCurve = serializedObject.FindProperty("m_ThrowSmoothingCurve");
            m_ThrowVelocityScale = serializedObject.FindProperty("m_ThrowVelocityScale");
            m_ThrowAngularVelocityScale = serializedObject.FindProperty("m_ThrowAngularVelocityScale");
            m_ForceGravityOnDetach = serializedObject.FindProperty("m_ForceGravityOnDetach");
            m_RetainTransformParent = serializedObject.FindProperty("m_RetainTransformParent");
            m_AttachPointCompatibilityMode = serializedObject.FindProperty("m_AttachPointCompatibilityMode");

            Undo.postprocessModifications += OnPostprocessModifications;
        }

        /// <summary>
        /// This function is called when the object becomes disabled.
        /// </summary>
        /// <seealso cref="MonoBehaviour"/>
        protected virtual void OnDisable()
        {
            Undo.postprocessModifications -= OnPostprocessModifications;
        }

        /// <inheritdoc />
        protected override void DrawProperties()
        {
            base.DrawProperties();

            EditorGUILayout.Space();

            DrawGrabConfiguration();
            DrawTrackConfiguration();
            DrawDetachConfiguration();
            DrawAttachConfiguration();
        }

        /// <summary>
        /// Draw the property fields related to grab configuration.
        /// </summary>
        protected virtual void DrawGrabConfiguration()
        {
            EditorGUILayout.PropertyField(m_MovementType, Contents.movementType);
            EditorGUILayout.PropertyField(m_RetainTransformParent, Contents.retainTransformParent);
            DrawNonUniformScaleMessage();
        }

        /// <summary>
        /// Draw the property fields related to tracking configuration.
        /// </summary>
        protected virtual void DrawTrackConfiguration()
        {
            EditorGUILayout.PropertyField(m_TrackPosition, Contents.trackPosition);
            if (m_TrackPosition.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(m_SmoothPosition, Contents.smoothPosition);
                    if (m_SmoothPosition.boolValue)
                    {
                        using (new EditorGUI.IndentLevelScope())
                        {
                            EditorGUILayout.PropertyField(m_SmoothPositionAmount, Contents.smoothPositionAmount);
                            EditorGUILayout.PropertyField(m_TightenPosition, Contents.tightenPosition);
                        }
                    }

                    if (m_MovementType.intValue == (int)XRBaseInteractable.MovementType.VelocityTracking)
                    {
                        EditorGUILayout.PropertyField(m_VelocityDamping, Contents.velocityDamping);
                        EditorGUILayout.PropertyField(m_VelocityScale, Contents.velocityScale);
                    }
                }
            }

            EditorGUILayout.PropertyField(m_TrackRotation, Contents.trackRotation);
            if (m_TrackRotation.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(m_SmoothRotation, Contents.smoothRotation);
                    if (m_SmoothRotation.boolValue)
                    {
                        using (new EditorGUI.IndentLevelScope())
                        {
                            EditorGUILayout.PropertyField(m_SmoothRotationAmount, Contents.smoothRotationAmount);
                            EditorGUILayout.PropertyField(m_TightenRotation, Contents.tightenRotation);
                        }
                    }

                    if (m_MovementType.intValue == (int)XRBaseInteractable.MovementType.VelocityTracking)
                    {
                        EditorGUILayout.PropertyField(m_AngularVelocityDamping, Contents.angularVelocityDamping);
                        EditorGUILayout.PropertyField(m_AngularVelocityScale, Contents.angularVelocityScale);
                    }
                }
            }
        }

        /// <summary>
        /// Draw property fields related to detach configuration.
        /// </summary>
        protected virtual void DrawDetachConfiguration()
        {
            EditorGUILayout.PropertyField(m_ThrowOnDetach, Contents.throwOnDetach);
            if (m_ThrowOnDetach.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(m_ThrowSmoothingDuration, Contents.throwSmoothingDuration);
                    EditorGUILayout.PropertyField(m_ThrowSmoothingCurve, Contents.throwSmoothingCurve);
                    EditorGUILayout.PropertyField(m_ThrowVelocityScale, Contents.throwVelocityScale);
                    EditorGUILayout.PropertyField(m_ThrowAngularVelocityScale, Contents.throwAngularVelocityScale);
                }
            }

            EditorGUILayout.PropertyField(m_ForceGravityOnDetach, Contents.forceGravityOnDetach);
        }

        /// <summary>
        /// Draw property fields related to attach configuration.
        /// </summary>
        protected virtual void DrawAttachConfiguration()
        {
            EditorGUILayout.PropertyField(m_AttachTransform, Contents.attachTransform);
            EditorGUILayout.PropertyField(m_AttachEaseInTime, Contents.attachEaseInTime);
            XRInteractionEditorGUI.EnumPropertyField(m_AttachPointCompatibilityMode, Contents.attachPointCompatibilityMode, Contents.attachPointCompatibilityModeOptions);
        }

        /// <summary>
        /// Checks if the object has a non-uniformly scaled parent and draws a message if necessary.
        /// </summary>
        protected virtual void DrawNonUniformScaleMessage()
        {
            if (m_RetainTransformParent == null || !m_RetainTransformParent.boolValue)
                return;

            if (m_RecalculateHasNonUniformScale)
            {
                var monoBehaviour = target as MonoBehaviour;
                if (monoBehaviour == null)
                    return;

                var transform = monoBehaviour.transform;
                if (transform == null)
                    return;

                m_HasNonUniformScale = false;
                for (var parent = transform.parent; parent != null; parent = parent.parent)
                {
                    var localScale = parent.localScale;
                    if (!Mathf.Approximately(localScale.x, localScale.y) ||
                        !Mathf.Approximately(localScale.x, localScale.z))
                    {
                        m_HasNonUniformScale = true;
                        break;
                    }
                }

                m_RecalculateHasNonUniformScale = false;
            }

            if (m_HasNonUniformScale)
                EditorGUILayout.HelpBox(Contents.nonUniformScaledParentWarning, MessageType.Warning);
        }

        /// <summary>
        /// Callback registered to be triggered whenever a new set of property modifications is created.
        /// </summary>
        /// <seealso cref="Undo.postprocessModifications"/>
        protected virtual UndoPropertyModification[] OnPostprocessModifications(UndoPropertyModification[] modifications)
        {
            m_RecalculateHasNonUniformScale = true;
            return modifications;
        }
    }
}
