//-----------------------------------------------------------------------
// <copyright file="RotationManipulator.cs" company="Google">
//
// Copyright 2018 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

// Modifications copyright © 2020 Unity Technologies ApS

#if !AR_FOUNDATION_PRESENT && !PACKAGE_DOCS_GENERATION

// Stub class definition used to fool version defines that this MonoScript exists (fixed in 19.3)
namespace UnityEngine.XR.Interaction.Toolkit.AR
{
    /// <summary>
    /// Manipulates the rotation of an object via a drag or a twist gesture.
    /// If an object is selected, then dragging along the horizontal axis
    /// or performing a twist gesture will rotate along the y-axis of the item.
    /// </summary>
    public class ARRotationInteractable {}
}

#else

using UnityEngine;

namespace UnityEngine.XR.Interaction.Toolkit.AR
{
    /// <summary>
    /// Manipulates the rotation of an object via a drag or a twist gesture.
    /// If an object is selected, then dragging along the horizontal axis
    /// or performing a twist gesture will rotate along the y-axis of the item.
    /// </summary>
    [AddComponentMenu("XR/AR Rotation Interactable", 22)]
    [HelpURL(XRHelpURLConstants.k_ARRotationInteractable)]
    public class ARRotationInteractable : ARBaseGestureInteractable
    {
        [SerializeField, Tooltip("The rate at which Unity rotates the attached object with a drag gesture.")]
        float m_RotationRateDegreesDrag = 100f;

        /// <summary>
        /// The rate at which Unity rotates the attached object with a drag gesture.
        /// </summary>
        public float rotationRateDegreesDrag
        {
            get => m_RotationRateDegreesDrag;
            set => m_RotationRateDegreesDrag = value;
        }

        [SerializeField, Tooltip("The rate at which Unity rotates the attached object with a twist gesture.")]
        float m_RotationRateDegreesTwist = 2.5f;

        /// <summary>
        /// The rate at which Unity rotates the attached object with a twist gesture.
        /// </summary>
        public float rotationRateDegreesTwist
        {
            get => m_RotationRateDegreesTwist;
            set => m_RotationRateDegreesTwist = value;
        }

        /// <inheritdoc />
        protected override bool CanStartManipulationForGesture(DragGesture gesture)
        {
            return IsGameObjectSelected() && gesture.targetObject == null;
        }

        /// <inheritdoc />
        protected override bool CanStartManipulationForGesture(TwistGesture gesture)
        {
            return IsGameObjectSelected() && gesture.targetObject == null;
        }

        /// <summary>
        /// Rotates the object around the y-axis via a Drag gesture.
        /// </summary>
        /// <param name="gesture">The current drag gesture.</param>
        /// <inheritdoc />
        protected override void OnContinueManipulation(DragGesture gesture)
        {
            // ReSharper disable once LocalVariableHidesMember -- hide deprecated camera property
            var camera = xrOrigin != null
                ? xrOrigin.Camera
#pragma warning disable 618 // Calling deprecated property to help with backwards compatibility.
                : (arSessionOrigin != null ? arSessionOrigin.camera : Camera.main);
#pragma warning restore 618
            if (camera == null)
                return;

            var forward = camera.transform.forward;
            var worldToVerticalOrientedDevice = Quaternion.Inverse(Quaternion.LookRotation(forward, Vector3.up));
            var deviceToWorld = camera.transform.rotation;
            var rotatedDelta = worldToVerticalOrientedDevice * deviceToWorld * gesture.delta;

            var rotationAmount = -1f * (rotatedDelta.x / Screen.dpi) * m_RotationRateDegreesDrag;
            transform.Rotate(0f, rotationAmount, 0f);
        }

        /// <summary>
        /// Rotates the object around the y-axis via a Twist gesture.
        /// </summary>
        /// <param name="gesture">The current twist gesture.</param>
        /// <inheritdoc />
        protected override void OnContinueManipulation(TwistGesture gesture)
        {
            var rotationAmount = -gesture.deltaRotation * m_RotationRateDegreesTwist;
            transform.Rotate(0f, rotationAmount, 0f);
        }
    }
}

#endif
