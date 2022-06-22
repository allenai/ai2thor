#if !AR_FOUNDATION_PRESENT && !PACKAGE_DOCS_GENERATION

// Stub class definition used to fool version defines that this MonoScript exists (fixed in 19.3)
namespace UnityEngine.XR.Interaction.Toolkit.AR
{
    /// <summary>
    /// Controls displaying one or more annotations when hovering over the <see cref="GameObject"/> this component is attached to.
    /// </summary>
    public class ARAnnotationInteractable {}
}

#else

using System;
using System.Collections.Generic;

namespace UnityEngine.XR.Interaction.Toolkit.AR
{
    /// <summary>
    /// An annotation that appears when the user hovers over the <see cref="GameObject"/>
    /// that the <see cref="ARAnnotationInteractable"/> component governing this annotation is attached to.
    /// </summary>
    [Serializable]
    public class ARAnnotation
    {
        [SerializeField]
        [Tooltip("The visualization GameObject that will become active when the object is hovered over.")]
        GameObject m_AnnotationVisualization;

        /// <summary>
        /// The visualization <see cref="GameObject"/> that will become active when the user hovers over this object.
        /// </summary>
        public GameObject annotationVisualization
        {
            get => m_AnnotationVisualization;
            set => m_AnnotationVisualization = value;
        }

        [SerializeField]
        [Tooltip("Maximum angle (in radians) off of FOV horizontal center to show annotation.")]
        float m_MaxFOVCenterOffsetAngle = 0.25f;

        /// <summary>
        /// Maximum angle (in radians) off of FOV horizontal center to show annotation.
        /// </summary>
        public float maxFOVCenterOffsetAngle
        {
            get => m_MaxFOVCenterOffsetAngle;
            set => m_MaxFOVCenterOffsetAngle = value;
        }

        [SerializeField]
        [Tooltip("Minimum range to show annotation at.")]
        float m_MinAnnotationRange;

        /// <summary>
        /// Minimum range to show annotation at.
        /// </summary>
        public float minAnnotationRange
        {
            get => m_MinAnnotationRange;
            set => m_MinAnnotationRange = value;
        }

        [SerializeField]
        [Tooltip("Maximum range to show annotation at.")]
        float m_MaxAnnotationRange = 10f;

        /// <summary>
        /// Maximum range to show annotation at.
        /// </summary>
        public float maxAnnotationRange
        {
            get => m_MaxAnnotationRange;
            set => m_MaxAnnotationRange = value;
        }
    }

    /// <summary>
    /// Controls displaying one or more annotations when hovering over the <see cref="GameObject"/> this component is attached to.
    /// </summary>
    [AddComponentMenu("XR/AR Annotation Interactable", 22)]
    [HelpURL(XRHelpURLConstants.k_ARAnnotationInteractable)]
    public class ARAnnotationInteractable : ARBaseGestureInteractable
    {
        [SerializeField]
        List<ARAnnotation> m_Annotations = new List<ARAnnotation>();

        /// <summary>
        /// The list of annotations.
        /// </summary>
        public List<ARAnnotation> annotations
        {
            get => m_Annotations;
            set => m_Annotations = value;
        }

        /// <inheritdoc />
        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractable(updatePhase);

            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
                UpdateVisualizations();
        }

        void UpdateVisualizations()
        {
            // Disable all annotations if not hovered.
            if (!isHovered)
            {
                foreach (var annotation in m_Annotations)
                {
                    annotation.annotationVisualization.SetActive(false);
                }
            }
            else
            {
                // ReSharper disable once LocalVariableHidesMember -- hide deprecated camera property
                var camera = xrOrigin != null
                    ? xrOrigin.Camera
#pragma warning disable 618 // Calling deprecated property to help with backwards compatibility.
                    : (arSessionOrigin != null ? arSessionOrigin.camera : Camera.main);
#pragma warning restore 618
                if (camera == null)
                    return;

                var cameraTransform = camera.transform;
                var fromCamera = transform.position - cameraTransform.position;
                var distSquare = fromCamera.sqrMagnitude;
                fromCamera.y = 0f;
                fromCamera.Normalize();
                var dotProd = Vector3.Dot(fromCamera, cameraTransform.forward);

                foreach (var annotation in m_Annotations)
                {
                    var enableThisFrame =
                        (Mathf.Acos(dotProd) < annotation.maxFOVCenterOffsetAngle &&
                        distSquare >= Mathf.Pow(annotation.minAnnotationRange, 2f) &&
                        distSquare < Mathf.Pow(annotation.maxAnnotationRange, 2f));
                    if (annotation.annotationVisualization != null)
                    {
                        if (enableThisFrame && !annotation.annotationVisualization.activeSelf)
                            annotation.annotationVisualization.SetActive(true);
                        else if (!enableThisFrame && annotation.annotationVisualization.activeSelf)
                            annotation.annotationVisualization.SetActive(false);

                        // If enabled, align to camera
                        if (annotation.annotationVisualization.activeSelf)
                        {
                            annotation.annotationVisualization.transform.rotation =
                                Quaternion.LookRotation(fromCamera, transform.up);
                        }
                    }
                }
            }
        }
    }
}

#endif
